using System;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Rust;

namespace Oxide.Plugins
{
    [Info("SpawnControl", "FuJiCuRa", "1.6.5", ResourceId = 18)]
    [Description("Provides complete control over the population of animals, ore's, tree's, junkpiles and all others")]
    internal class SpawnControl : RustPlugin
    {
        private bool Changed = false;
        private bool _loaded = false;
        private bool isAtStartup = true;
        private bool onTerrainCalled;
        private bool heartbeatOn = false;
        private double lastMinute;
        private bool newSave;
        private int versionMajor;
        private int versionMinor;
        private bool _newConfig = false;
        private Dictionary<string, object> spawnPopulations = new Dictionary<string, object>();
        private Dictionary<string, object> spawnGroups = new Dictionary<string, object>();
        private Dictionary<string, object> spawnDefaults = new Dictionary<string, object>();
        private Dictionary<string, object> spawnPrefabDefaults = new Dictionary<string, object>();
        private List<string> PopulationNames = new List<string>();

        private Dictionary<string, ConvarControlledSpawnPopulation> convarCommands =
            new Dictionary<string, ConvarControlledSpawnPopulation>();

        private Dictionary<string, string> populationConvars = new Dictionary<string, string>();
        private Dictionary<string, int> fillPopulations = new Dictionary<string, int>();
        private Dictionary<string, int> fillJobs = new Dictionary<string, int>();
        private DynamicConfigFile defaultSpawnPops;

        private DynamicConfigFile getFile(string file)
        {
            return Interface.Oxide.DataFileSystem.GetDatafile($"{file}");
        }

        private bool chkFile(string file)
        {
            return Interface.Oxide.DataFileSystem.ExistsDatafile($"{file}");
        }

        private FieldInfo _numToSpawn = typeof(SpawnPopulation).GetField("numToSpawn", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private Coroutine _groupKill;
        private Coroutine _groupFill;
        private Coroutine _spawnKill;
        private Coroutine _spawnFill;
        private Coroutine _enforceLimits;
        private Coroutine _spawnLowerDensity;
        private Coroutine _spawnRaiseDensity;
        private bool normalizeDefaultVariables;
        private bool reloadWithIncludedFill;
        private bool logJobsToConsole;
        private bool fixBarricadeStacking;
        private bool fillAtEveryStartup;
        private float tickInterval;
        private int minSpawnsPerTick;
        private int maxSpawnsPerTick;
        private SpawnPopulation[] AllSpawnPopulations = null;
        private SpawnDistribution[] SpawnDistributions = null;
        private List<string> killAllProtected;
        private bool currentTickStatus;

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            Dictionary<string, object> data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }

            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }

            return value;
        }

        private void LoadVariables()
        {
            spawnPopulations =
                (Dictionary<string, object>)GetConfig("Spawn", "Population", new Dictionary<string, object>());
            spawnGroups = (Dictionary<string, object>)GetConfig("Spawn", "Groups", new Dictionary<string, object>());
            normalizeDefaultVariables = Convert.ToBoolean(GetConfig("Generic", "normalizeDefaultVariables", true));
            reloadWithIncludedFill = Convert.ToBoolean(GetConfig("Generic", "reloadWithIncludedFill", false));
            fillAtEveryStartup = Convert.ToBoolean(GetConfig("Generic", "fillAtEveryStartup", true));
            logJobsToConsole = Convert.ToBoolean(GetConfig("Generic", "logJobsToConsole", true));
            fixBarricadeStacking = Convert.ToBoolean(GetConfig("Generic", "fixBarricadeStacking", true));
            tickInterval = Convert.ToSingle(GetConfig("Handler", "tickInterval", 60f));
            minSpawnsPerTick = Convert.ToInt32(GetConfig("Handler", "minSpawnsPerTick", 100));
            maxSpawnsPerTick = Convert.ToInt32(GetConfig("Handler", "maxSpawnsPerTick", 200));
            bool configremoval = false;
            if (Config.Get("Animals") as Dictionary<string, object> != null)
            {
                Config.Remove("Animals");
                configremoval = true;
            }

            if ((Config.Get("Generic") as Dictionary<string, object>).ContainsKey("enablePopulationPrefabs"))
            {
                (Config.Get("Generic") as Dictionary<string, object>).Remove("enablePopulationPrefabs");
                configremoval = true;
            }

            if ((Config.Get("Generic") as Dictionary<string, object>).ContainsKey("enableGroupPrefabs"))
            {
                (Config.Get("Generic") as Dictionary<string, object>).Remove("enableGroupPrefabs");
                configremoval = true;
            }

            if ((Config.Get("Generic") as Dictionary<string, object>).ContainsKey("enableSpawnGroups"))
            {
                (Config.Get("Generic") as Dictionary<string, object>).Remove("enableSpawnGroups");
                configremoval = true;
            }

            if ((Config.Get("Generic") as Dictionary<string, object>).ContainsKey("enableGroupTickData"))
            {
                (Config.Get("Generic") as Dictionary<string, object>).Remove("enableGroupTickData");
                configremoval = true;
            }

            if (!Changed && !configremoval) return;
            SaveConfig();
            Changed = false;
            configremoval = false;
        }

        protected override void LoadDefaultConfig()
        {
            _newConfig = true;
            Config.Clear();
            LoadVariables();
        }

        private void Init()
        {
            LoadVariables();
            onTerrainCalled = false;
            if (normalizeDefaultVariables)
            {
                ConVar.Spawn.max_density = 1f;
                ConVar.Spawn.max_rate = 1f;
                ConVar.Spawn.min_density = 0.5f;
                ConVar.Spawn.min_rate = 0.5f;
                SetTickConvars(true);
            }

            newSave = false;
            lastMinute = DateTime.UtcNow.Minute;
        }

        private void OnNewSave(string strFilename)
        {
            newSave = true;
            if (!fillAtEveryStartup)
            {
                ConVar.Spawn.respawn_populations = false;
                ConVar.Spawn.respawn_groups = false;
            }
        }

        private void Unload()
        {
            if (Interface.Oxide.IsShuttingDown) return;
            UnloadCoRoutines();
        }

        private void UnloadCoRoutines()
        {
            if (_groupKill != null) Global.Runner.StopCoroutine(_groupKill);
            if (_groupFill != null) Global.Runner.StopCoroutine(_groupFill);
            if (_spawnKill != null) Global.Runner.StopCoroutine(_spawnKill);
            if (_spawnFill != null) Global.Runner.StopCoroutine(_spawnFill);
            if (_enforceLimits != null) Global.Runner.StopCoroutine(_enforceLimits);
            if (_spawnLowerDensity != null) Global.Runner.StopCoroutine(_spawnLowerDensity);
            if (_spawnRaiseDensity != null) Global.Runner.StopCoroutine(_spawnRaiseDensity);
        }

        private void OnTerrainInitialized()
        {
            _loaded = true;
            AllSpawnPopulations = SpawnHandler.Instance.AllSpawnPopulations;
            SpawnDistributions = SpawnHandler.Instance.SpawnDistributions;
            if (isAtStartup && !newSave)
            {
                onTerrainCalled = true;
                SaveDefaultsToFile();
            }

            SpawnHandler.Instance.MaxSpawnsPerTick = maxSpawnsPerTick;
            SpawnHandler.Instance.MinSpawnsPerTick = minSpawnsPerTick;
            if (spawnPopulations != null && spawnPopulations.Count > 0) LoadSpawnPopulations();
            else GetSpawnPopulationDefaults();
            if (spawnGroups != null && spawnGroups.Count > 0) LoadSpawnGroups();
            else GetSpawnGroupDefaults();
        }

        private void OnServerInitialized()
        {
            isAtStartup = false;
            AllSpawnPopulations = SpawnHandler.Instance.AllSpawnPopulations;
            SpawnDistributions = SpawnHandler.Instance.SpawnDistributions;
            GetPopulationNames();
            if (!_loaded) OnTerrainInitialized();
            if (fillJobs.Count > 0) Puts($"Planned {fillJobs.Count} job(s) for spawn population");
            LoadDefaultsFromFile();
            if (onTerrainCalled) ReloadAnimals();
            if (onTerrainCalled)
            {
                if (fillAtEveryStartup)
                    SpawnHandler.Instance.InitialSpawn();
                else if (!fillAtEveryStartup && newSave) SetTickConvars(true);
            }

            SpawnHandler.Instance.StartSpawnTick();
            currentTickStatus = true;
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!_loaded || !fixBarricadeStacking || entity == null ||
                !(entity as BaseEntity).ShortPrefabName.StartsWith("door_barricade")) return;
            NextTick(() =>
            {
                if (entity == null) return;
                BaseEntity[] stacked = null;
                try
                {
                    stacked = Physics.OverlapSphere(entity.transform.position, 1.0f)
                        .Where(entry =>
                            entry != null && entry.GetComponentInParent<BaseEntity>() != null && entry
                                .GetComponentInParent<BaseEntity>().ShortPrefabName.StartsWith("door_barricade"))
                        .Select(entry => entry.GetComponentInParent<BaseEntity>()).Distinct()
                        .OrderByDescending(c => c.net.ID).ToArray();
                }
                catch
                {
                }

                if (stacked != null && stacked.Length > 1)
                    for (int i = 0; i < stacked.Length - 1; i++)
                        stacked[i].Kill();
            });
        }

        private void OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null || arg.FullString == string.Empty ||
                arg.cmd.FullName == string.Empty) return;
            if (populationConvars.ContainsKey(arg.cmd.FullName))
            {
                float targetDensity = -2;
                float.TryParse(arg.Args[0], out targetDensity);
                (spawnPopulations[populationConvars[arg.cmd.FullName]] as Dictionary<string, object>)["targetDensity"] =
                    targetDensity;
                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
            }
        }

        private void OnTick()
        {
            if (!_loaded || !heartbeatOn || lastMinute == DateTime.UtcNow.Minute ||
                !(_spawnKill == null && _spawnFill == null && _enforceLimits == null && _spawnLowerDensity == null &&
                  _spawnRaiseDensity == null)) return;
            lastMinute = DateTime.UtcNow.Minute;
            foreach (KeyValuePair<string, int> job in fillJobs.ToList())
                if ((int)fillJobs[job.Key] < 1)
                {
                    timer.Once(2f,
                        () => ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "sc.fillpopulation",
                            new object[] { job.Key.ToString(), "auto" }));
                    fillJobs[job.Key] = fillPopulations[job.Key];
                }
                else
                {
                    fillJobs[job.Key] = (int)job.Value - 1;
                }
        }

        private void PauseTick()
        {
            if (currentTickStatus) SetTickConvars(false);
        }

        private void ResumeTick()
        {
            if (currentTickStatus) SetTickConvars(true);
        }

        private void ReloadAnimals()
        {
            Dictionary<string, object> data = new Dictionary<string, object>(spawnPopulations);
            if (data == null || data.Count() == 0)
            {
                GetSpawnPopulationDefaults();
                return;
            }

            for (int j = 0; j < AllSpawnPopulations.Length; j++)
                if (!(AllSpawnPopulations[j] == null))
                {
                    SpawnPopulation population = AllSpawnPopulations[j];
                    object spawndata;
                    if (!data.TryGetValue(population.name, out spawndata)) continue;
                    if (!convarCommands.ContainsKey(population.name)) continue;
                    Dictionary<string, object> spawnData = spawndata as Dictionary<string, object>;
                    population.EnforcePopulationLimits = Convert.ToBoolean(spawnData["enforceLimits"]);
                    population.SpawnRate = Convert.ToSingle(spawnData["spawnRate"]);
                    population._targetDensity = Convert.ToSingle(spawnData["targetDensity"]);
                    SetAnimal(population.name, Convert.ToSingle(spawnData["targetDensity"]));
                }
        }

        private void SetAnimal(string name, float value)
        {
            if (convarCommands.ContainsKey(name))
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(),
                    $"{convarCommands[name].PopulationConvar} {value.ToString()}");
        }

        private void SaveDefaultsToFile()
        {
            spawnDefaults = new Dictionary<string, object>();
            spawnPrefabDefaults = new Dictionary<string, object>();
            for (int i = 0; i < AllSpawnPopulations.Length; i++)
                if (!(AllSpawnPopulations[i] == null))
                {
                    SpawnPopulation population = AllSpawnPopulations[i];
                    if (!population.Initialize()) continue;
                    Dictionary<string, object> prefabdata = new Dictionary<string, object>();
                    Prefab<Spawnable>[] prefabs = population.Prefabs;
                    Dictionary<string, int> counts =
                        prefabs.GroupBy(x => x.Name).ToDictionary(g => g.Key.ToLower(), g => g.Count());
                    foreach (KeyValuePair<string, int> prefab in counts)
                        prefabdata.Add(prefab.Key.ToLower(), prefab.Value);
                    spawnPrefabDefaults.Add(population.name, prefabdata);
                    spawnDefaults.Add(population.name, population.TargetDensity);
                }

            defaultSpawnPops = getFile($"{Title}_defaults");
            defaultSpawnPops.Clear();
            defaultSpawnPops.Set("Backup", spawnDefaults);
            defaultSpawnPops.Set("PrefabBackup", spawnPrefabDefaults);
            defaultSpawnPops.Save();
        }

        private void LoadDefaultsFromFile()
        {
            if (spawnDefaults == null || spawnDefaults.Count == 0)
            {
                spawnDefaults = new Dictionary<string, object>();
                defaultSpawnPops = getFile($"{Title}_defaults");
                spawnDefaults = defaultSpawnPops["Backup"] as Dictionary<string, object>;
                spawnPrefabDefaults = defaultSpawnPops["PrefabBackup"] as Dictionary<string, object>;
            }
        }

        private void GetPopulationNames()
        {
            PopulationNames = new List<string>();
            foreach (SpawnPopulation pop in SpawnHandler.Instance.AllSpawnPopulations.Where(x => x != null).Distinct()
                .ToList()) PopulationNames.Add(pop.name);
            convarCommands = new Dictionary<string, ConvarControlledSpawnPopulation>();
            populationConvars = new Dictionary<string, string>();
            foreach (SpawnPopulation pop in SpawnHandler.Instance.ConvarSpawnPopulations.ToList().Where(x => x != null))
            {
                populationConvars.Add((pop as ConvarControlledSpawnPopulation).PopulationConvar, pop.name);
                convarCommands.Add(pop.name, pop as ConvarControlledSpawnPopulation);
            }
        }

        private void GetSpawnPopulationDefaults()
        {
            Dictionary<string, object> data = new Dictionary<string, object>();
            spawnPopulations.Clear();
            for (int i = 0; i < AllSpawnPopulations.Length; i++)
                if (!(AllSpawnPopulations[i] == null))
                {
                    SpawnPopulation population = AllSpawnPopulations[i];
                    if (!population.Initialize()) continue;
                    Dictionary<string, object> populationdata = new Dictionary<string, object>();
                    populationdata.Add("targetDensity", population.TargetDensity);
                    populationdata.Add("spawnRate", population.SpawnRate);
                    populationdata.Add("enforceLimits", population.EnforcePopulationLimits);
                    populationdata.Add("spawnFillHeartbeat", 0);
                    populationdata.Add("protectFromKillAll", false);
                    populationdata.Add("scaleWithServerPopulation", population.ScaleWithServerPopulation);
                    populationdata.Add("spawnFilter", GetSpawnFilter(population.Filter, population));
                    Dictionary<string, object> prefabdata = new Dictionary<string, object>();
                    Prefab<Spawnable>[] prefabs = population.Prefabs;
                    Dictionary<string, int> counts =
                        prefabs.GroupBy(x => x.Name).ToDictionary(g => g.Key.ToLower(), g => g.Count());
                    foreach (KeyValuePair<string, int> prefab in counts)
                        prefabdata.Add(prefab.Key.ToLower(), prefab.Value);
                    populationdata.Add("spawnWeights", prefabdata);
                    data.Add(population.name, populationdata);
                }

            spawnPopulations = new Dictionary<string, object>(data);
            Config["Spawn", "Population"] = spawnPopulations;
            Config.Save();
            Puts($"Created SpawnPopulation with '{data.Count}' populations.");
        }

        private object GetSpawnFilter(SpawnFilter filter, SpawnPopulation population)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add(nameof(filter.TopologyAny).ToString(), (int)filter.TopologyAny);
            dict.Add(nameof(filter.TopologyNot).ToString(), (int)filter.TopologyNot);
            dict.Add(nameof(filter.TopologyAll).ToString(), (int)filter.TopologyAll);
            return dict;
        }

        private SpawnFilter SetSpawnFilter(Dictionary<string, object> values, SpawnPopulation population)
        {
            SpawnFilter filter = new SpawnFilter();
            filter.BiomeType = population.Filter.BiomeType;
            filter.SplatType = population.Filter.SplatType;
            filter.TopologyAny = (TerrainTopology.Enum)(int)values[nameof(filter.TopologyAny).ToString()];
            filter.TopologyAll = (TerrainTopology.Enum)(int)values[nameof(filter.TopologyAll).ToString()];
            filter.TopologyNot = (TerrainTopology.Enum)(int)values[nameof(filter.TopologyNot).ToString()];
            return filter;
        }

        private List<string> GetTerrainTopologies(int value)
        {
            List<string> result = new List<string>();
            string[] names = Enum.GetNames(typeof(TerrainTopology.Enum));
            for (int i = 0; i < names.Length; i++)
                if ((value & (1 << i)) != 0)
                    result.Add(names[i]);
            return result;
        }

        [ConsoleCommand("sc.topologyget")]
        private void GetTopology(ConsoleSystem.Arg arg)
        {
            if (arg != null && !arg.IsAdmin) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to provide a spawnFilter Topology value");
                return;
            }

            List<string> allNames = Enum.GetNames(typeof(TerrainTopology.Enum)).ToList();
            List<string> names = null;
            if (arg.Args != null)
            {
                int id = -1;
                int.TryParse(arg.Args[0], out id);
                if (id != -0) names = GetTerrainTopologies(id);
                else return;
            }
            else
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (string name in names) sb.AppendLine($"{name} | {allNames.IndexOf(name)}");
            SendReply(arg, "\n\n" + sb.ToString());
        }

        [ConsoleCommand("sc.topologylist")]
        private void ListTopology(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin) return;
            List<string> names = Enum.GetNames(typeof(TerrainTopology.Enum)).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (string name in names)
                sb.AppendLine(
                    $"{name} | {names.IndexOf(name)} | Single: {(int)Enum.Parse(typeof(TerrainTopology.Enum), name)}");
            SendReply(arg, "\n\n" + sb.ToString());
        }

        [ConsoleCommand("sc.topologycreate")]
        private void SetTopology(ConsoleSystem.Arg arg)
        {
            if (arg != null && !arg.IsAdmin) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to provide a list of spacer separated numbers (f.e. by: sc.topologylist)");
                return;
            }

            List<string> allNames = Enum.GetNames(typeof(TerrainTopology.Enum)).ToList();
            int maskOut = 0;
            for (int i = 0; i < arg.Args.Length; i++)
            {
                int id = -1;
                int.TryParse(arg.Args[i], out id);
                if (id != -1) maskOut += (int)Enum.Parse(typeof(TerrainTopology.Enum), allNames[id]);
            }

            SendReply(arg, "\n\nnew Topology mask is: " + maskOut);
        }

        private void LoadSpawnPopulations()
        {
            killAllProtected = new List<string>();
            Dictionary<string, object> data = new Dictionary<string, object>(spawnPopulations);
            if (data == null || data.Count() == 0)
            {
                GetSpawnPopulationDefaults();
                return;
            }

            bool changed = false;
            if (PopulationNames == null || PopulationNames.Count == 0)
                foreach (SpawnPopulation pop in SpawnHandler.Instance.AllSpawnPopulations.ToList().Where(x => x != null)
                    .Distinct().ToList())
                    PopulationNames.Add(pop.name);
            foreach (KeyValuePair<string, object> population in data.ToList())
                if (!PopulationNames.Contains(population.Key.ToString()))
                {
                    data.Remove(population.Key);
                    Puts($"Removed old SpawnPopulation: {population.Key}");
                    changed = true;
                    continue;
                }

            for (int i = 0; i < AllSpawnPopulations.Length; i++)
                if (!(AllSpawnPopulations[i] == null))
                {
                    SpawnPopulation population = AllSpawnPopulations[i];
                    if (!population.Initialize()) continue;
                    string name = population.name;
                    if (name == null) continue;
                    if (!data.ContainsKey(name))
                    {
                        Dictionary<string, object> populationdata = new Dictionary<string, object>();
                        populationdata.Add("targetDensity", population.TargetDensity);
                        populationdata.Add("spawnRate", population.SpawnRate);
                        populationdata.Add("enforceLimits", population.EnforcePopulationLimits);
                        populationdata.Add("spawnFillHeartbeat", 0);
                        populationdata.Add("protectFromKillAll", false);
                        populationdata.Add("scaleWithServerPopulation", population.ScaleWithServerPopulation);
                        populationdata.Add("spawnFilter", GetSpawnFilter(population.Filter, population));
                        Prefab<Spawnable>[] prefabs = population.Prefabs;
                        Dictionary<string, int> counts =
                            prefabs.GroupBy(x => x.Name).ToDictionary(g => g.Key, g => g.Count());
                        Dictionary<string, object> prefabdata = new Dictionary<string, object>();
                        foreach (KeyValuePair<string, int> prefab in counts) prefabdata.Add(prefab.Key, prefab.Value);
                        populationdata.Add("spawnWeights", prefabdata);
                        data.Add(name, populationdata);
                        Puts($"Added new SpawnPopulation: {name}");
                        changed = true;
                    }

                    if (!(data[name] as Dictionary<string, object>).ContainsKey("protectFromKillAll"))
                    {
                        (data[name] as Dictionary<string, object>).Add("protectFromKillAll", false);
                        changed = true;
                    }

                    if (!(data[name] as Dictionary<string, object>).ContainsKey("spawnFilter"))
                    {
                        (data[name] as Dictionary<string, object>).Add("spawnFilter",
                            GetSpawnFilter(population.Filter, population));
                        changed = true;
                    }

                    if ((data[name] as Dictionary<string, object>).ContainsKey("spawnlimits"))
                    {
                        (data[name] as Dictionary<string, object>).Remove("spawnlimits");
                        (data[name] as Dictionary<string, object>).Add("enforceLimits", true);
                        changed = true;
                    }

                    if (!(data[name] as Dictionary<string, object>).ContainsKey("scaleWithServerPopulation"))
                    {
                        (data[name] as Dictionary<string, object>).Add("scaleWithServerPopulation",
                            population.ScaleWithServerPopulation);
                        changed = true;
                    }
                }

            spawnPopulations = new Dictionary<string, object>(data);
            if (changed)
            {
                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
            }

            bool addedPrefabs = false;
            for (int j = 0; j < AllSpawnPopulations.Length; j++)
                if (!(AllSpawnPopulations[j] == null))
                {
                    SpawnPopulation population = AllSpawnPopulations[j];
                    if (!population.Initialize()) continue;
                    object spawndata;
                    if (!data.TryGetValue(population.name, out spawndata)) continue;
                    Dictionary<string, object> spawnData = spawndata as Dictionary<string, object>;
                    if (!spawnData.ContainsKey("spawnWeights"))
                    {
                        Prefab<Spawnable>[] prefabs = population.Prefabs;
                        Dictionary<string, int> counts = prefabs.GroupBy(x => x.Name)
                            .ToDictionary(g => g.Key.ToLower(), g => g.Count());
                        Dictionary<string, object> prefabdata = new Dictionary<string, object>();
                        foreach (KeyValuePair<string, int> prefab in counts)
                            prefabdata.Add(prefab.Key.ToLower(), prefab.Value);
                        spawnData.Add("spawnWeights", prefabdata);
                        addedPrefabs = true;
                    }
                    else if (spawnData.ContainsKey("spawnWeights"))
                    {
                        Dictionary<string, object> prefabData = spawnData["spawnWeights"] as Dictionary<string, object>;
                        List<Prefab<Spawnable>> prefabs = new List<Prefab<Spawnable>>();
                        foreach (KeyValuePair<string, object> prefab in prefabData.ToList())
                        {
                            GameObject gameObject = GameManager.server.FindPrefab(prefab.Key);
                            if (gameObject == null)
                            {
                                Puts($"Removed invalid/removed prefab from '{population.name}': {prefab.Key}");
                                ((data[population.name] as Dictionary<string, object>)["spawnWeights"] as
                                    Dictionary<string, object>).Remove(prefab.Key);
                                addedPrefabs = true;
                                continue;
                            }

                            Spawnable component = gameObject.GetComponent<Spawnable>();
                            if (component)
                                for (int i = 0; i < Convert.ToInt32(prefab.Value); i++)
                                    prefabs.Add(new Prefab<Spawnable>(prefab.Key.ToLower(), gameObject, component,
                                        GameManager.server, PrefabAttribute.server));
                        }

                        population.Prefabs = prefabs.ToArray();
                        _numToSpawn.SetValue(population, new int[prefabs.ToArray().Length]);
                    }

                    population.EnforcePopulationLimits = Convert.ToBoolean(spawnData["enforceLimits"]);
                    population.SpawnRate = Convert.ToSingle(spawnData["spawnRate"]);
                    population._targetDensity = Convert.ToSingle(spawnData["targetDensity"]);
                    population.ScaleWithServerPopulation = Convert.ToBoolean(spawnData["scaleWithServerPopulation"]);
                    SetAnimal(population.name, Convert.ToSingle(spawnData["targetDensity"]));
                    if ((int)spawnData["spawnFillHeartbeat"] > 0)
                    {
                        fillPopulations.Add(population.name, (int)spawnData["spawnFillHeartbeat"]);
                        heartbeatOn = true;
                    }

                    if (Convert.ToBoolean(spawnData["protectFromKillAll"])) killAllProtected.Add(population.name);
                    if (spawnData.ContainsKey("spawnFilter"))
                        population.Filter = SetSpawnFilter((Dictionary<string, object>)spawnData["spawnFilter"],
                            population);
                }

            if (addedPrefabs)
            {
                spawnPopulations = new Dictionary<string, object>(data);
                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
            }

            if (heartbeatOn) fillJobs = new Dictionary<string, int>(fillPopulations);
            Puts($"Loaded '{data.Count}' SpawnPopulations");
        }

        private void GetSpawnGroupDefaults()
        {
            if (TerrainMeta.Path.Monuments.Count == 0)
                return;

            Dictionary<string, List<MonumentInfo>> monumentGroups = TerrainMeta.Path?.Monuments?.GroupBy(c => c.displayPhrase.english)?.ToDictionary(c => c.Key, c => c.ToList());

            if (monumentGroups == null)
                return;

            foreach (KeyValuePair<string, List<MonumentInfo>> current in monumentGroups)
            {
                MonumentInfo monument = current.Value.First();

                string displayPhrase = monument.displayPhrase.english;
                if (displayPhrase.Length == 0)
                    continue;

                SpawnGroup[] groups = monument.gameObject.GetComponentsInChildren<SpawnGroup>();
                if ((groups == null) | (groups.Length == 0))
                    continue;

                List<object> list = new List<object>();
                foreach (SpawnGroup group in groups.ToList())
                {
                    Dictionary<string, object> spawner = new Dictionary<string, object>();

                    spawner.Add("_refDisplayPhrase", group.name);
                    spawner.Add("_refMaxPopulation", group.maxPopulation);

                    Vector3 center = Vector3.zero;

                    foreach (BaseSpawnPoint spawnPoint in group.spawnPoints.ToList())
                        center += spawnPoint.transform.position;

                    center /= group.spawnPoints.Length;
                    spawner.Add("_refPositionCenter", center.ToString().Replace(" ", ""));

                    Dictionary<string, object> prefabdata = new Dictionary<string, object>();
                    foreach (SpawnGroup.SpawnEntry prefab in group.prefabs)
                    {
                        if (prefab.prefab.resourcePath.Contains("/npc/"))
                            continue;

                        prefabdata[prefab.prefab.resourcePath] = prefab.weight;
                    }

                    if (prefabdata.Count == 0)
                        continue;

                    spawner.Add("spawnWeights", prefabdata);
                    spawner.Add("respawnDelayMin", group.respawnDelayMin);
                    spawner.Add("respawnDelayMax", group.respawnDelayMax);
                    spawner.Add("numToSpawnPerTickMin", group.numToSpawnPerTickMin);
                    spawner.Add("numToSpawnPerTickMax", group.numToSpawnPerTickMax);
                    list.Add(spawner);
                }

                if (list.Count > 0) spawnGroups[displayPhrase] = list;
            }

            Config["Spawn", "Groups"] = spawnGroups;
            Config.Save();
            Puts($"Created '{spawnGroups.Count()}' spawnGroups");
        }

        private void LoadSpawnGroups()
        {
            if (TerrainMeta.Path.Monuments.Count == 0)
                return;

            if (spawnGroups == null || spawnGroups.Count() == 0)
            {
                GetSpawnGroupDefaults();
                return;
            }

            bool addedData = false;

            foreach (KeyValuePair<string, object> monument in spawnGroups)
            {
                foreach (object spawner in monument.Value as List<object>)
                {
                    Dictionary<string, object> group = spawner as Dictionary<string, object>;
                    if (group.ContainsKey("_refPositionCenter"))
                    {
                        group.Remove("_refPositionCenter");
                        addedData = true;
                    }
                }
            }

            Dictionary<string, List<MonumentInfo>> monumentGroups = TerrainMeta.Path?.Monuments?.GroupBy(c => c.displayPhrase.english)?.ToDictionary(c => c.Key, c => c.ToList());

            if (monumentGroups == null) return;
            foreach (KeyValuePair<string, List<MonumentInfo>> current in monumentGroups)
            {
                MonumentInfo monument = current.Value.First();
                string displayPhrase = monument.displayPhrase.english;
                if (displayPhrase.Length == 0)
                    continue;

                SpawnGroup[] groups = monument.gameObject.GetComponentsInChildren<SpawnGroup>();
                if ((groups == null) | (groups.Length == 0))
                    continue;

                List<object> loadedGroups = new List<object>();

                if (!spawnGroups.ContainsKey(displayPhrase))
                {
                    List<object> list = new List<object>();
                    foreach (SpawnGroup group in groups.ToList())
                    {
                        Dictionary<string, object> spawner = new Dictionary<string, object>();

                        spawner.Add("_refDisplayPhrase", group.name);
                        spawner.Add("_refMaxPopulation", group.maxPopulation);

                        Vector3 center = Vector3.zero;

                        foreach (BaseSpawnPoint spawnPoint in group.spawnPoints.ToList())
                            center += spawnPoint.transform.position;
                        center /= group.spawnPoints.Length;

                        spawner.Add("_refPositionCenter", center.ToString().Replace(" ", ""));

                        Dictionary<string, object> prefabdata = new Dictionary<string, object>();

                        foreach (SpawnGroup.SpawnEntry prefab in group.prefabs)
                        {
                            if (prefab.prefab.resourcePath.Contains("/npc/")) continue;
                            prefabdata[prefab.prefab.resourcePath] = prefab.weight;
                        }

                        if (prefabdata.Count == 0)
                            continue;

                        spawner.Add("spawnWeights", prefabdata);
                        spawner.Add("respawnDelayMin", group.respawnDelayMin);
                        spawner.Add("respawnDelayMax", group.respawnDelayMax);
                        spawner.Add("numToSpawnPerTickMin", group.numToSpawnPerTickMin);
                        spawner.Add("numToSpawnPerTickMax", group.numToSpawnPerTickMax);
                        list.Add(spawner);
                    }

                    if (list.Count > 0)
                    {
                        spawnGroups[displayPhrase] = list;
                        addedData = true;
                    }

                    continue;
                }

                loadedGroups = spawnGroups[displayPhrase] as List<object>;
                List<object> list2 = new List<object>();
                foreach (SpawnGroup group in groups.ToList())
                {
                    bool groupMatch = false;
                    foreach (object loadedGroup in loadedGroups)
                    {
                        Dictionary<string, object> checkGroup = loadedGroup as Dictionary<string, object>;

                        if (checkGroup.ContainsKey("_refDisplayPhrase") && checkGroup["_refDisplayPhrase"].Equals(group.name))
                        {
                            if (checkGroup.ContainsKey("_refMaxPopulation") && (int)checkGroup["_refMaxPopulation"] == @group.maxPopulation)
                            {
                                @group.respawnDelayMin = Convert.ToSingle(checkGroup["respawnDelayMin"]);
                                @group.respawnDelayMax = Convert.ToSingle(checkGroup["respawnDelayMax"]);
                                @group.numToSpawnPerTickMin = (int)checkGroup["numToSpawnPerTickMin"];
                                @group.numToSpawnPerTickMax = (int)checkGroup["numToSpawnPerTickMax"];
                                if (@group.WantsTimedSpawn())
                                {
                                    LocalClock spawnClock = new LocalClock();
                                    spawnClock.Add(@group.GetSpawnDelta(), @group.GetSpawnVariance(), new Action(@group.Spawn));
                                    spawnClock.Tick();
                                    @group.spawnClock = spawnClock;
                                }

                                Vector3 center = Vector3.zero;
                                foreach (BaseSpawnPoint spawnPoint in @group.spawnPoints.ToList())
                                    center += spawnPoint.transform.position;
                                center /= @group.spawnPoints.Length;
                                checkGroup["_refPositionCenter"] = center.ToString().Replace(" ", "");
                                addedData = true;
                                groupMatch = true;
                            }
                        }
                    }

                    if (!groupMatch)
                    {
                        foreach (SpawnGroup.SpawnEntry prefab in @group.prefabs)
                        {
                            if (!prefab.prefab.resourcePath.Contains("/npc/"))
                            {
                                Puts($"Adding '{@group.name}' to '{displayPhrase}'");
                                Dictionary<string, object> spawner = new Dictionary<string, object>();
                                spawner.Add("_refDisplayPhrase", @group.name);
                                spawner.Add("_refMaxPopulation", @group.maxPopulation);
                                Vector3 center = Vector3.zero;
                                foreach (BaseSpawnPoint spawnPoint in @group.spawnPoints.ToList())
                                    center += spawnPoint.transform.position;
                                center /= @group.spawnPoints.Length;
                                spawner.Add("_refPositionCenter", center.ToString().Replace(" ", ""));
                                Dictionary<string, object> prefabdata = new Dictionary<string, object>();
                                foreach (SpawnGroup.SpawnEntry prefab2 in @group.prefabs)
                                {
                                    if (prefab2.prefab.resourcePath.Contains("scientist")) continue;
                                    prefabdata[prefab2.prefab.resourcePath] = prefab2.weight;
                                }

                                if (prefabdata.Count == 0) continue;
                                spawner.Add("spawnWeights", prefabdata);
                                spawner.Add("respawnDelayMin", @group.respawnDelayMin);
                                spawner.Add("respawnDelayMax", @group.respawnDelayMax);
                                spawner.Add("numToSpawnPerTickMin", @group.numToSpawnPerTickMin);
                                spawner.Add("numToSpawnPerTickMax", @group.numToSpawnPerTickMax);
                                (spawnGroups[displayPhrase] as List<object>).Add(spawner);
                                addedData = true;
                            }
                        }
                    }
                }
            }

            if (addedData)
            {
                Config["Spawn", "Groups"] = spawnGroups;
                Config.Save();
            }

            Puts($"Loaded '{spawnGroups.Count()}' spawnGroups");
        }

        [ConsoleCommand("sc.cleardata")]
        private void ccmdClearData(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            string data = string.Empty;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to provide 'weights', 'population', 'groups' or 'all'");
                return;
            }

            data = arg.Args[0];
            if (data == "weights")
            {
                foreach (KeyValuePair<string, object> pop in spawnPopulations)
                    if ((spawnPopulations[pop.Key] as Dictionary<string, object>).ContainsKey("spawnWeights"))
                        (spawnPopulations[pop.Key] as Dictionary<string, object>).Remove("spawnWeights");
                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
                SendReply(arg, "Did reset weights data");
            }
            else if (data == "population")
            {
                spawnPopulations.Clear();
                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
                SendReply(arg, "Did reset population data.");
            }
            else if (data == "groups")
            {
                spawnGroups.Clear();
                Config["Spawn", "Groups"] = spawnGroups;
                Config.Save();
                SendReply(arg, "Did reset groups data");
            }
            else if (data == "all")
            {
                spawnGroups.Clear();
                spawnPopulations.Clear();
                Config["Spawn", "Groups"] = spawnGroups;
                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
                SendReply(arg, "Did reset all data. Next server boot loads the game defaults");
            }
            else
            {
                SendReply(arg, "You need to provide 'weights', 'population', 'groups' or 'all'");
                return;
            }

            SendReply(arg, "Next server boot writes the game defaults back into the config.");
        }

        [ConsoleCommand("sc.tickinterval")]
        private void ccmdTickInterval(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            float currTickInterval = SpawnHandler.Instance.TickInterval;
            if (arg.Args == null)
            {
                SendReply(arg, $"Current TickInterval is '{currTickInterval}' seconds, spawntick is {(currentTickStatus ? "active" : "inactive")}");
                return;
            }

            if (arg.Args != null && arg.Args.Length > 0)
            {
                float input = 0f;
                if (float.TryParse(arg.Args[0], out input))
                {
                    currTickInterval = input;
                    SendReply(arg, $"TickInterval set to '{currTickInterval}' seconds, spawntick is {(currentTickStatus ? "active" : "inactive")}");
                    SpawnHandler.Instance.TickInterval = currTickInterval;
                    if (currTickInterval != tickInterval)
                    {
                        tickInterval = currTickInterval;
                        Config["Handler", "tickInterval"] = tickInterval;
                        Config.Save();
                    }

                    return;
                }
                else
                {
                    SendReply(arg, "You need to provide a full number in seconds");
                    return;
                }
            }
        }

        private void SetTickConvars(bool status)
        {
            ConVar.Spawn.respawn_populations = status;
            ConVar.Spawn.respawn_groups = status;
            ConVar.Spawn.respawn_individuals = status;
        }

        [ConsoleCommand("sc.spawnticktoggle")]
        private void ccmdToggleSpawnTick(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            currentTickStatus = !currentTickStatus;
            SetTickConvars(currentTickStatus);
            SendReply(arg, $"Spawntick is now: {(currentTickStatus ? "active" : "inactive")}");
        }

        [ConsoleCommand("sc.fillpopulation")]
        private void ccmdSpawnFill(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            string populationname = string.Empty;
            bool single = false;
            if (arg.Args != null && arg.Args.Length > 0)
            {
                populationname = arg.Args[0];
                single = true;
            }

            if (_spawnFill != null)
            {
                SendReply(arg, "Action aborted: another task is still running!");
                return;
            }

            PauseTick();
            _spawnFill = Global.Runner.StartCoroutine(SpawnFill(populationname, single, arg));
        }

        private IEnumerator SpawnFill(string populationname, bool single, ConsoleSystem.Arg arg)
        {
            yield return new WaitWhile(() =>
                _spawnKill != null || _enforceLimits != null || _spawnLowerDensity != null ||
                _spawnRaiseDensity != null);
            bool findany = false;
            if (single)
            {
                bool fullMatch = false;
                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                        if ((AllSpawnPopulations[i].name.Equals(populationname, StringComparison.OrdinalIgnoreCase) ||
                             AllSpawnPopulations[i].name
                                 .StartsWith(populationname, StringComparison.OrdinalIgnoreCase) ||
                             AllSpawnPopulations[i].name
                                 .EndsWith(populationname, StringComparison.OrdinalIgnoreCase)) && !fullMatch)
                        {
                            if (AllSpawnPopulations[i].name == populationname) fullMatch = true;
                            int oldCount =
                                SpawnHandler.Instance.GetCurrentCount(AllSpawnPopulations[i], SpawnDistributions[i]);
                            yield return CoroutineEx.waitForEndOfFrame;
                            SpawnHandler.Instance.SpawnInitial(AllSpawnPopulations[i], SpawnDistributions[i]);
                            yield return CoroutineEx.waitForEndOfFrame;
                            if (arg.Args.Length > 1 && arg.Args[1] == "auto")
                                if (logJobsToConsole)
                                    SendReply(arg,
                                        $"Timed population of '{AllSpawnPopulations[i].name}' | Old: {oldCount} | New: {SpawnHandler.Instance.GetCurrentCount(AllSpawnPopulations[i], SpawnDistributions[i])}");
                            if (arg.Args.Length == 1)
                                SendReply(arg,
                                    $"Forced population of '{AllSpawnPopulations[i].name}' | Old: {oldCount} | New: {SpawnHandler.Instance.GetCurrentCount(AllSpawnPopulations[i], SpawnDistributions[i])}");
                            findany = true;
                        }

                if (!findany) SendReply(arg, $"SpawnPopulation '{populationname}' not found");
            }
            else
            {
                int oldCount = 0;
                int newCount = 0;
                SendReply(arg, "Filling All SpawnPopulations");
                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                    {
                        oldCount += SpawnHandler.Instance.GetCurrentCount(AllSpawnPopulations[i],
                            SpawnDistributions[i]);
                        SpawnHandler.Instance.SpawnInitial(AllSpawnPopulations[i], SpawnDistributions[i]);
                        yield return CoroutineEx.waitForEndOfFrame;
                        newCount += SpawnHandler.Instance.GetCurrentCount(AllSpawnPopulations[i],
                            SpawnDistributions[i]);
                        yield return CoroutineEx.waitForEndOfFrame;
                    }

                SendReply(arg, $"Filled SpawnPopulations | Old: {oldCount} | New: {newCount}");
            }

            _spawnFill = null;
            ResumeTick();
            yield break;
        }

        [ConsoleCommand("sc.raisedensity")]
        private void ccmdSpawnRaiseDensity(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            float percentage = 0f;
            string populationname = string.Empty;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "You need to provide a number as percantage and a valid SpawnPopulation name");
                return;
            }

            float percent = -1;
            if (float.TryParse(arg.Args[0], out percent))
            {
                percentage = percent;
            }
            else
            {
                SendReply(arg, "You need to provide a number as percentage");
                return;
            }

            if (percentage > 100f)
            {
                percentage = 100f;
                SendReply(arg, "Percentage was limited to 100% to avoid server freezes");
            }

            if (_spawnRaiseDensity != null)
            {
                SendReply(arg, "Action aborted: another task is still running!");
                return;
            }

            populationname = arg.Args[1];
            bool raiseAll = false;
            if (populationname == "all") raiseAll = true;
            SendReply(arg, $"Calling raise of density with '{percentage}' percent for '{populationname}'");
            PauseTick();
            _spawnRaiseDensity =
                Global.Runner.StartCoroutine(SpawnRaiseDensity(percentage, populationname, raiseAll, arg));
        }

        private IEnumerator SpawnRaiseDensity(float percentage, string populationname, bool raiseAll,
            ConsoleSystem.Arg arg)
        {
            yield return new WaitWhile(() =>
                _spawnKill != null || _spawnFill != null || _enforceLimits != null || _spawnLowerDensity != null);
            bool findany = false;
            if (!raiseAll)
            {
                bool fullMatch = false;
                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                        if ((AllSpawnPopulations[i].name.Equals(populationname, StringComparison.OrdinalIgnoreCase) ||
                             AllSpawnPopulations[i].name
                                 .StartsWith(populationname, StringComparison.OrdinalIgnoreCase) ||
                             AllSpawnPopulations[i].name
                                 .EndsWith(populationname, StringComparison.OrdinalIgnoreCase)) && !fullMatch)
                        {
                            if (AllSpawnPopulations[i].name == populationname) fullMatch = true;
                            float oldDensity = AllSpawnPopulations[i].TargetDensity;
                            float increaseDensity = 0f;
                            if (oldDensity > 0f)
                            {
                                increaseDensity = AllSpawnPopulations[i].TargetDensity / 100 * percentage;
                            }
                            else
                            {
                                object backupDensity = null;
                                if (spawnDefaults.TryGetValue(AllSpawnPopulations[i].name, out backupDensity))
                                    increaseDensity = Convert.ToSingle(backupDensity) / 100 * percentage;
                                else increaseDensity = AllSpawnPopulations[i].TargetDensity / 100 * percentage;
                            }

                            float newDensity = (float)Math.Round((double)(oldDensity + increaseDensity));
                            AllSpawnPopulations[i]._targetDensity = newDensity;
                            SetAnimal(AllSpawnPopulations[i].name, newDensity);
                            yield return CoroutineEx.waitForEndOfFrame;
                            (spawnPopulations[AllSpawnPopulations[i].name] as Dictionary<string, object>)
                                ["targetDensity"] = AllSpawnPopulations[i].TargetDensity;
                            SendReply(arg,
                                $"Raised density of '{AllSpawnPopulations[i].name}' | Old: {oldDensity} | New: {newDensity}");
                            findany = true;
                        }

                if (!findany)
                {
                    SendReply(arg, $"SpawnPopulation '{populationname}' not found");
                }
                else
                {
                    SendReply(arg, "New raised density's set and saved.");
                    Config["Spawn", "Population"] = spawnPopulations;
                    Config.Save();
                    string type = arg.Args[1];
                    arg.Args = new string[] { type };
                    ccmdSpawnFill(arg);
                }
            }
            else
            {
                SendReply(arg, $"Raising all SpawnPopulation density's by '{percentage}' percent");
                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                    {
                        float oldDensity = AllSpawnPopulations[i].TargetDensity;
                        float increaseDensity = AllSpawnPopulations[i].TargetDensity / 100 * percentage;
                        float newDensity = (float)Math.Round((double)(oldDensity + increaseDensity));
                        AllSpawnPopulations[i]._targetDensity = newDensity;
                        SetAnimal(AllSpawnPopulations[i].name, newDensity);
                        yield return CoroutineEx.waitForEndOfFrame;
                        (spawnPopulations[AllSpawnPopulations[i].name] as Dictionary<string, object>)["targetDensity"] =
                            AllSpawnPopulations[i].TargetDensity;
                    }

                SendReply(arg, "New raised density's set and saved.");
                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
                arg.Args = null;
                ccmdSpawnFill(arg);
            }

            _spawnRaiseDensity = null;
            ResumeTick();
            yield break;
        }

        [ConsoleCommand("sc.lowerdensity")]
        private void ccmdSpawnLowerDensity(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            float percentage = 0f;
            string populationname = string.Empty;
            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "You need to provide a percentage number and a valid SpawnPopulation name");
                return;
            }

            float percent = -1;
            if (float.TryParse(arg.Args[0], out percent))
            {
                percentage = percent;
            }
            else
            {
                SendReply(arg, "You need to provide a number as percentage");
                return;
            }

            populationname = arg.Args[1];
            bool lowerAll = false;
            if (populationname == "all") lowerAll = true;
            if (percentage > 100f)
            {
                percentage = 100f;
                SendReply(arg, "Percentage was limited to 100%");
            }

            bool force = false;
            if (arg.Args.Length >= 3 && arg.Args[2] == "force") force = true;
            if (lowerAll && percentage == 100f && !force)
            {
                SendReply(arg, "Canceled: Lowering all by 100% would set all to zero pop");
                SendReply(arg, "You need to add 'force' as additional argument!");
                return;
            }

            if (_spawnLowerDensity != null)
            {
                SendReply(arg, "Action aborted: another task is still running!");
                return;
            }

            SendReply(arg, $"Calling lower with '{percentage}' percent for '{populationname}'");
            PauseTick();
            _spawnLowerDensity =
                Global.Runner.StartCoroutine(SpawnLowerDensity(percentage, populationname, lowerAll, arg));
        }

        private IEnumerator SpawnLowerDensity(float percentage, string populationname, bool lowerAll,
            ConsoleSystem.Arg arg)
        {
            yield return new WaitWhile(() =>
                _spawnKill != null || _spawnFill != null || _enforceLimits != null || _spawnRaiseDensity != null);
            bool findany = false;
            if (!lowerAll)
            {
                bool fullMatch = false;
                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                        if ((AllSpawnPopulations[i].name.Equals(populationname, StringComparison.OrdinalIgnoreCase) ||
                             AllSpawnPopulations[i].name
                                 .StartsWith(populationname, StringComparison.OrdinalIgnoreCase) ||
                             AllSpawnPopulations[i].name
                                 .EndsWith(populationname, StringComparison.OrdinalIgnoreCase)) && !fullMatch)
                        {
                            if (AllSpawnPopulations[i].name == populationname) fullMatch = true;
                            float oldDensity = AllSpawnPopulations[i].TargetDensity;
                            float decreaseDensity = AllSpawnPopulations[i].TargetDensity / 100 * percentage;
                            float newDensity = -1f;
                            if (percentage == 100f) newDensity = 0f;
                            else newDensity = (float)Math.Round((double)(oldDensity - decreaseDensity));
                            AllSpawnPopulations[i]._targetDensity = newDensity;
                            SetAnimal(AllSpawnPopulations[i].name, newDensity);
                            yield return CoroutineEx.waitForEndOfFrame;
                            (spawnPopulations[AllSpawnPopulations[i].name] as Dictionary<string, object>)
                                ["targetDensity"] = AllSpawnPopulations[i].TargetDensity;
                            SendReply(arg,
                                $"Lowered density of '{AllSpawnPopulations[i].name}' | Old: {oldDensity} | New: {newDensity}");
                            findany = true;
                        }

                if (!findany)
                {
                    SendReply(arg, $"SpawnPopulation '{populationname}' not found");
                }
                else
                {
                    SendReply(arg, $"New lowered density's set and saved.");
                    Config["Spawn", "Population"] = spawnPopulations;
                    Config.Save();
                    ccmdEnforceLimits(arg);
                }
            }
            else
            {
                SendReply(arg, $"Lowering all SpawnPopulation density's by '{percentage}' percent");
                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                    {
                        float oldDensity = AllSpawnPopulations[i].TargetDensity;
                        float decreaseDensity = AllSpawnPopulations[i].TargetDensity / 100 * percentage;
                        float newDensity = -1f;
                        if (percentage == 100f) newDensity = 0f;
                        else newDensity = (float)Math.Round((double)(oldDensity - decreaseDensity));
                        AllSpawnPopulations[i]._targetDensity = newDensity;
                        SetAnimal(AllSpawnPopulations[i].name, newDensity);
                        yield return CoroutineEx.waitForEndOfFrame;
                        (spawnPopulations[AllSpawnPopulations[i].name] as Dictionary<string, object>)["targetDensity"] =
                            AllSpawnPopulations[i].TargetDensity;
                    }

                SendReply(arg, $"New lowered density's set and saved");
                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
                ccmdEnforceLimits(arg);
            }

            _spawnLowerDensity = null;
            ResumeTick();
            yield break;
        }

        [ConsoleCommand("sc.killpopulation")]
        private void ccmdSpawnKill(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            string populationname = string.Empty;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, $"You need to provide a valid SpawnPopulation name");
                return;
            }

            if (_spawnKill != null)
            {
                SendReply(arg, "Action aborted: another task is still running!");
                return;
            }

            SendReply(arg, $"Calling kill for '{arg.Args[0]}'");
            PauseTick();
            _spawnKill = Global.Runner.StartCoroutine(SpawnKill(arg.Args[0], arg.Args[0] == "all" ? true : false, arg));
        }

        private IEnumerator SpawnKill(string populationname, bool killAll, ConsoleSystem.Arg arg)
        {
            yield return new WaitWhile(() =>
                _spawnFill != null || _enforceLimits != null || _spawnLowerDensity != null ||
                _spawnRaiseDensity != null);
            bool findany = false;
            int countDeleted = 0;
            bool fullMatch = false;
            bool junkPiles = false;
            for (int i = 0; i < AllSpawnPopulations.Length; i++)
                if (!(AllSpawnPopulations[i] == null))
                {
                    if (killAll && killAllProtected.Contains(AllSpawnPopulations[i].name)) continue;
                    if (AllSpawnPopulations[i].name.Equals(populationname, StringComparison.OrdinalIgnoreCase) ||
                        AllSpawnPopulations[i].name.StartsWith(populationname, StringComparison.OrdinalIgnoreCase) ||
                        AllSpawnPopulations[i].name.EndsWith(populationname, StringComparison.OrdinalIgnoreCase) &&
                        !fullMatch || killAll)
                    {
                        if (AllSpawnPopulations[i].name == populationname) fullMatch = true;
                        Spawnable[] array = SpawnHandler.Instance.FindAll(AllSpawnPopulations[i]);
                        int count = array.Length;
                        foreach (Spawnable current in array.ToList())
                        {
                            BaseEntity baseEntity = current.gameObject.ToBaseEntity();
                            if (baseEntity.IsValid())
                            {
                                if (baseEntity is JunkPile)
                                {
                                    JunkPile junk = baseEntity as JunkPile;
                                    for (int j = 0; j < junk.spawngroups.Length; j++) junk.spawngroups[j].Clear();
                                    junk.CheckEmpty();
                                    junk.SinkAndDestroy();
                                    junkPiles = true;
                                    yield return CoroutineEx.waitForEndOfFrame;
                                }
                                else
                                {
                                    if (baseEntity is OreResourceEntity)
                                        (baseEntity as OreResourceEntity).CleanupBonus();
                                    baseEntity.Kill(BaseNetworkable.DestroyMode.None);
                                }

                                countDeleted++;
                            }
                            else
                            {
                                GameManager.Destroy(current.gameObject, 0f);
                                countDeleted++;
                            }

                            if (countDeleted % 25 == 0) yield return CoroutineEx.waitForEndOfFrame;
                        }

                        if (!killAll)
                        {
                            if (junkPiles)
                                SendReply(arg,
                                    $"Forced deletion of '{AllSpawnPopulations[i].name}' | Old: {count} | New: {SpawnHandler.Instance.GetCurrentCount(AllSpawnPopulations[i], SpawnDistributions[i])}(DELAYED)");
                            else
                                SendReply(arg,
                                    $"Forced deletion of '{AllSpawnPopulations[i].name}' | Old: {count} | New: {SpawnHandler.Instance.GetCurrentCount(AllSpawnPopulations[i], SpawnDistributions[i])}");
                        }

                        findany = true;
                        yield return CoroutineEx.waitForFixedUpdate;
                    }
                }

            if (!findany && !killAll) SendReply(arg, $"SpawnPopulation '{populationname}' not found");
            if (findany && killAll) SendReply(arg, $"SpawnPopulation > cleaned the map of '{countDeleted}' objects");
            _spawnKill = null;
            ResumeTick();
            yield break;
        }

        [ConsoleCommand("sc.killgroups")]
        private void ccmdGroupKill(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, $"You need to confirm the action with 'force'");
                return;
            }

            if (_groupKill != null)
            {
                SendReply(arg, "Action aborted: another task is still running!");
                return;
            }

            SendReply(arg, $"Calling kill for SpawnGroups");
            _groupKill = Global.Runner.StartCoroutine(GroupKill(arg));
        }

        private IEnumerator GroupKill(ConsoleSystem.Arg arg)
        {
            PauseTick();
            int countDeleted = 0;
            bool success;
            for (int i = 0; i < SpawnHandler.Instance.SpawnGroups.Count; i++)
            {
                success = false;
                try
                {
                    countDeleted += (SpawnHandler.Instance.SpawnGroups[i] as SpawnGroup).currentPopulation;
                    (SpawnHandler.Instance.SpawnGroups[i] as SpawnGroup).Clear();
                    success = true;
                }
                catch
                {
                }

                if (success) yield return CoroutineEx.waitForEndOfFrame;
            }

            if (arg != null) SendReply(arg, $"Killed '{countDeleted}' SpawnGroup objects");
            ResumeTick();
            _groupKill = null;
            yield break;
        }

        [ConsoleCommand("sc.fillgroups")]
        private void ccmdGroupFill(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (_groupFill != null)
            {
                SendReply(arg, "Action aborted: another task is still running!");
                return;
            }

            SendReply(arg, $"Calling fill for SpawnGroups");
            _groupFill = Global.Runner.StartCoroutine(GroupFill(arg));
        }

        private IEnumerator GroupFill(ConsoleSystem.Arg arg)
        {
            PauseTick();
            int countFilled = 0;
            bool success;
            for (int i = 0; i < SpawnHandler.Instance.SpawnGroups.Count; i++)
            {
                success = false;
                try
                {
                    SpawnGroup group = SpawnHandler.Instance.SpawnGroups[i] as SpawnGroup;
                    countFilled += @group.maxPopulation - @group.currentPopulation;
                    group.Fill();
                    success = true;
                }
                catch
                {
                }

                if (success) yield return CoroutineEx.waitForEndOfFrame;
            }

            if (arg != null) SendReply(arg, $"Filled '{countFilled}' SpawnGroup objects");
            ResumeTick();
            _groupFill = null;
            yield break;
        }

        [ConsoleCommand("sc.enforcelimits")]
        private void ccmdEnforceLimits(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            bool forceAll = false;
            if (arg.Args != null && arg.Args.Length > 0)
                if (arg.Args[0].ToLower() == "all")
                    forceAll = true;
            SendReply(arg, $"Enforcing population limits");
            if (_enforceLimits != null)
            {
                SendReply(arg, "Action aborted: another task is still running!");
                return;
            }

            PauseTick();
            _enforceLimits = Global.Runner.StartCoroutine(EnforceLimits(forceAll, arg));
        }

        private IEnumerator EnforceLimits(bool forceAll, ConsoleSystem.Arg arg)
        {
            yield return new WaitWhile(() =>
                _spawnKill != null || _spawnFill != null || _spawnLowerDensity != null || _spawnRaiseDensity != null);
            int countDeleted = 0;
            for (int i = 0; i < AllSpawnPopulations.Length; i++)
            {
                if (!(AllSpawnPopulations[i] == null))
                {
                    SpawnPopulation population = AllSpawnPopulations[i];
                    SpawnDistribution distribution = SpawnDistributions[i];
                    if (forceAll || population.EnforcePopulationLimits)
                    {
                        int targetCount = SpawnHandler.Instance.GetTargetCount(population, distribution);
                        Spawnable[] array = SpawnHandler.Instance.FindAll(population);
                        if (array.Length <= targetCount) continue;
                        int num = array.Length - targetCount;
                        foreach (Spawnable current in array.Take(num).ToList())
                        {
                            BaseEntity baseEntity = current.gameObject.ToBaseEntity();
                            if (baseEntity.IsValid())
                            {
                                if (baseEntity is JunkPile)
                                {
                                    (baseEntity as JunkPile).SinkAndDestroy();
                                    yield return CoroutineEx.waitForEndOfFrame;
                                }
                                else
                                {
                                    if (baseEntity is OreResourceEntity)
                                        (baseEntity as OreResourceEntity).CleanupBonus();
                                    baseEntity.Kill(BaseNetworkable.DestroyMode.None);
                                }

                                countDeleted++;
                            }
                            else
                            {
                                GameManager.Destroy(current.gameObject, 0f);
                                countDeleted++;
                            }

                            if (countDeleted % 25 == 0) yield return CoroutineEx.waitForEndOfFrame;
                        }
                    }
                }

                yield return CoroutineEx.waitForFixedUpdate;
            }

            if (arg != null) SendReply(arg, $"Enforced limits on '{countDeleted}' objects");
            _enforceLimits = null;
            ResumeTick();
            yield break;
        }

        [ConsoleCommand("sc.reloadconfig")]
        private void ccmdSpawnReload(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            UnloadCoRoutines();
            NextTick(() =>
            {
                LoadConfig();
                LoadVariables();
                fillPopulations.Clear();
                fillJobs.Clear();
                killAllProtected.Clear();
                heartbeatOn = false;
                GetPopulationNames();
                LoadSpawnPopulations();
                LoadSpawnGroups();
                SpawnHandler.Instance.StartSpawnTick();
                if (normalizeDefaultVariables) SetTickConvars(true);
                currentTickStatus = true;
                if (reloadWithIncludedFill)
                {
                    SpawnHandler.Instance.FillGroups();
                    SpawnHandler.Instance.FillPopulations();
                }

                if (fillJobs.Count > 0) SendReply(arg, $"Planned {fillJobs.Count} job(s) for spawn population");
                if (reloadWithIncludedFill)
                    SendReply(arg, $"Updated and initially filled SpawnPopulations and spawnGroups");
                else SendReply(arg, $"Updated SpawnPopulation and SpawnGroup settings");
            });
        }

        [ConsoleCommand("sc.populationreport")]
        private void ccmdGetPopulationReport(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            NextTick(() =>
            {
                PauseTick();
                TextTable textTable = new TextTable();
                textTable.AddColumn("SpawnPopulation");
                textTable.AddColumn("Current");
                textTable.AddColumn("Limits");
                int totalCurrent = 0;
                int totalLimits = 0;
                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                    {
                        SpawnPopulation population = AllSpawnPopulations[i];
                        SpawnDistribution spawnDistribution = SpawnDistributions[i];
                        if (population != null)
                            if (spawnDistribution != null)
                            {
                                int curr = SpawnHandler.Instance.GetCurrentCount(population, spawnDistribution);
                                int lmt = SpawnHandler.Instance.GetTargetCount(population, spawnDistribution);
                                textTable.AddRow(new string[]
                                    {population.name.ToString(), curr.ToString(), lmt.ToString()});
                                totalCurrent += curr;
                                totalLimits += lmt;
                            }
                    }

                textTable.AddRow(new string[] { "TOTAL:", totalCurrent.ToString(), totalLimits.ToString() });
                SendReply(arg, "\n\n>> Report:\n" + textTable.ToString());
                ResumeTick();
            });
        }

        [ConsoleCommand("sc.populationsettings")]
        private void ccmdGetPopulations(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            NextFrame(() =>
            {
                PauseTick();
                TextTable textTable = new TextTable();
                textTable.AddColumn("Name");
                textTable.AddColumn("Density");
                textTable.AddColumn("Rate");
                textTable.AddColumn("Limiting");
                textTable.AddColumn("Prefabs");
                textTable.AddColumn("Scale");
                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                    {
                        SpawnPopulation population = AllSpawnPopulations[i];
                        Prefab<Spawnable>[] prefabs = population.Prefabs;
                        textTable.AddRow(new string[]
                        {
                            population.name.ToString(), population.TargetDensity.ToString(),
                            population.SpawnRate.ToString(), population.EnforcePopulationLimits.ToString(),
                            prefabs.Length.ToString(), population.ScaleWithServerPopulation.ToString(),
                        });
                    }

                SendReply(arg, "\n\n>> SpawnPopulations:\n" + textTable.ToString());
                ResumeTick();
            });
        }

        [ConsoleCommand("sc.restorepopulationdensities")]
        private void ccmdRestoreDensities(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            NextTick(() =>
            {
                PauseTick();
                if (spawnDefaults == null || spawnDefaults.Count == 0)
                {
                    spawnDefaults = new Dictionary<string, object>();
                    defaultSpawnPops = getFile($"{Title}_defaults");
                    spawnDefaults = defaultSpawnPops["Backup"] as Dictionary<string, object>;
                }

                if (spawnDefaults == null || spawnDefaults.Count == 0)
                {
                    SendReply(arg, "Default SpawnPopulations not found or file missing.");
                    return;
                }

                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                    {
                        string name = AllSpawnPopulations[i].name;
                        float newDensity = Convert.ToSingle(spawnDefaults[name]);
                        AllSpawnPopulations[i]._targetDensity = newDensity;
                        SetAnimal(name, newDensity);
                        (spawnPopulations[AllSpawnPopulations[i].name] as Dictionary<string, object>)["targetDensity"] =
                            newDensity;
                    }

                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
                SendReply(arg, "Reverted SpawnPopulation density's to their defaults");
                _enforceLimits = Global.Runner.StartCoroutine(EnforceLimits(true, arg));
            });
        }

        [ConsoleCommand("sc.restorepopulationweights")]
        private void ccmdRestoreWeights(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            NextTick(() =>
            {
                PauseTick();
                if (spawnPrefabDefaults == null || spawnPrefabDefaults.Count == 0)
                {
                    spawnPrefabDefaults = new Dictionary<string, object>();
                    defaultSpawnPops = getFile($"{Title}_defaults");
                    spawnPrefabDefaults = defaultSpawnPops["PrefabBackup"] as Dictionary<string, object>;
                }

                if (spawnPrefabDefaults == null || spawnPrefabDefaults.Count == 0)
                {
                    SendReply(arg, "Default SpawnPopulationWeights not found or file missing");
                    return;
                }

                for (int i = 0; i < AllSpawnPopulations.Length; i++)
                    if (!(AllSpawnPopulations[i] == null))
                    {
                        Dictionary<string, object> prefabData =
                            spawnPrefabDefaults[AllSpawnPopulations[i].name] as Dictionary<string, object>;
                        List<Prefab<Spawnable>> prefabs = new List<Prefab<Spawnable>>();
                        foreach (KeyValuePair<string, object> prefab in prefabData)
                        {
                            GameObject gameObject = GameManager.server.FindPrefab(prefab.Key);
                            Spawnable component = gameObject?.GetComponent<Spawnable>();
                            if (component == null) continue;
                            for (int j = 0; j < Convert.ToInt32(prefab.Value); j++)
                                prefabs.Add(new Prefab<Spawnable>(prefab.Key.ToLower(), gameObject, component,
                                    GameManager.server, PrefabAttribute.server));
                        }

                        AllSpawnPopulations[i].Prefabs = prefabs.ToArray();
                        _numToSpawn.SetValue(AllSpawnPopulations[i], new int[prefabs.ToArray().Length]);
                        (spawnPopulations[AllSpawnPopulations[i].name] as Dictionary<string, object>)["spawnWeights"] =
                            prefabData;
                    }

                Config["Spawn", "Population"] = spawnPopulations;
                Config.Save();
                SendReply(arg, "Reverted SpawnPopulation weights's to their defaults");
                _enforceLimits = Global.Runner.StartCoroutine(EnforceLimits(true, arg));
            });
        }

        [ConsoleCommand("sc.groupsettings")]
        private void ccmdGetGroups(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2)
                return;

            Dictionary<string, List<MonumentInfo>> monumentGroups = TerrainMeta.Path?.Monuments?.GroupBy(c => c.displayPhrase.english)?.ToDictionary(c => c.Key, c => c.ToList());
            if (monumentGroups == null)
                return;

            PauseTick();
            TextTable textTable = new TextTable();
            textTable.AddColumn("Monument");
            textTable.AddColumn("Group");
            textTable.AddColumn("DelayMin");
            textTable.AddColumn("DelayMax");
            textTable.AddColumn("PerTick");
            textTable.AddColumn("CenterPosition");
            foreach (KeyValuePair<string, List<MonumentInfo>> current in monumentGroups)
            {
                MonumentInfo monument = current.Value.First();
                string displayPhrase = monument.displayPhrase.english;
                if (displayPhrase.Length == 0) continue;
                SpawnGroup[] groups = monument.gameObject.GetComponentsInChildren<SpawnGroup>();
                if ((groups == null) | (groups.Length == 0)) continue;
                bool firstLine = true;
                foreach (SpawnGroup group in groups.ToList())
                {
                    Vector3 center = Vector3.zero;
                    foreach (BaseSpawnPoint spawnPoint in group.spawnPoints.ToList())
                        center += spawnPoint.transform.position;
                    center /= group.spawnPoints.Length;
                    Dictionary<string, object> prefabdata = new Dictionary<string, object>();
                    foreach (SpawnGroup.SpawnEntry prefab in group.prefabs)
                    {
                        if (prefab.prefab.resourcePath.Contains("/npc/")) continue;
                        prefabdata[prefab.prefab.resourcePath] = prefab.weight;
                    }

                    if (prefabdata.Count == 0) continue;
                    if (firstLine) firstLine = false;
                    else displayPhrase = string.Empty;
                    textTable.AddRow(new string[]
                    {
                        displayPhrase, group.name, group.respawnDelayMin.ToString(), group.respawnDelayMax.ToString(),
                        group.numToSpawnPerTickMin.ToString() + " / " + group.numToSpawnPerTickMax.ToString(),
                        center.ToString().Replace(" ", "")
                    });
                }
            }

            SendReply(arg, "\n\n>> GroupSpawners:\n" + textTable.ToString());
            ResumeTick();
        }

        [ConsoleCommand("sc.dumpresources")]
        private void ccmdDumpResources(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            NextFrame(() =>
            {
                PauseTick();
                TextTable textTable = new TextTable();
                textTable.AddColumn("Name");
                textTable.AddColumn("Count");
                textTable.AddColumn("Type");
                foreach (KeyValuePair<string, int> t in GetCollectibles())
                    textTable.AddRow(new string[] { t.Key.ToString(), t.Value.ToString(), "Collectible" });
                foreach (KeyValuePair<string, IGrouping<string, BaseEntity>> t in GetOreNodes())
                    textTable.AddRow(new string[] { t.Key.ToString(), t.Value.Count().ToString(), "Ore" });
                foreach (KeyValuePair<string, IGrouping<string, BaseEntity>> t in GetAnimals())
                    textTable.AddRow(new string[] { t.Key.ToString(), t.Value.Count().ToString(), "Animals" });
                SendReply(arg, "\n>> Resources:\n" + textTable.ToString());
                ResumeTick();
            });
        }

        [ConsoleCommand("sc.dumploot")]
        private void ccmdDumpLoot(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            NextFrame(() =>
            {
                PauseTick();
                TextTable textTable = new TextTable();
                textTable.AddColumn("Name");
                textTable.AddColumn("Count");
                textTable.AddColumn("Type");
                foreach (KeyValuePair<string, IGrouping<string, BaseEntity>> t in GetLootContainers())
                    textTable.AddRow(new string[] { t.Key.ToString(), t.Value.Count().ToString(), "Loot" });
                SendReply(arg, "\n>> Loot:\n" + textTable.ToString());
                ResumeTick();
            });
        }

        [ConsoleCommand("sc.cmds")]
        private void ccmdComandsList(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n==== Command List ================\n");
            sb.AppendLine($"sc.cleardata".PadRight(30) +
                          "| Clear Population and/or Group data by 'all', 'population' or 'groups");
            sb.AppendLine($"sc.tickinterval".PadRight(30) + "| Show or set the Population tick interval in 'seconds'");
            sb.AppendLine($"sc.spawnticktoggle".PadRight(30) + "| Does pause or resume the spawnhandler ticks'");
            sb.AppendLine($"sc.fillpopulation".PadRight(30) + "| Fill all Populations or choosen one's by 'filter'");
            sb.AppendLine($"sc.killpopulation".PadRight(30) + "| Kill 'all' Populations or choosen one's by 'filter'");
            sb.AppendLine($"sc.fillgroups".PadRight(30) + "| Fill all SpawnGroups");
            sb.AppendLine($"sc.killgroups".PadRight(30) + "| Kill all SpawnGroups");
            sb.AppendLine($"sc.raisedensity".PadRight(30) +
                          "| Raise Populations in 'percent' for 'all' or choosen one's by 'filter'");
            sb.AppendLine($"sc.lowerdensity".PadRight(30) +
                          "| Lower Populations in 'percent' for 'all' or choosen one's by 'filter'");
            sb.AppendLine($"sc.enforcelimits".PadRight(30) + "| Enforce object limits on all Populations");
            sb.AppendLine($"sc.reloadconfig".PadRight(30) + "| Reload the plugin after config changes");
            sb.AppendLine(
                $"sc.populationreport".PadRight(30) + "| Display current Population objects with their limits");
            sb.AppendLine($"sc.populationsettings".PadRight(30) + "| Display current Population settings");
            sb.AppendLine($"sc.restorepopulationdensities".PadRight(30) +
                          "| Restore Population densities to their defaults");
            sb.AppendLine(
                $"sc.restorepopulationweights".PadRight(30) + "| Restore Population weights to their defaults");
            sb.AppendLine($"sc.groupsettings".PadRight(30) + "| Display SpawnGroup settings");
            sb.AppendLine($"sc.dumpresources".PadRight(30) +
                          "| Dump current server resource amount (for information only)");
            sb.AppendLine($"sc.dumploot".PadRight(30) + "| Dump current server loot amount (for information only)");
            sb.AppendLine($"sc.topologyget".PadRight(30) + "| List all entries of a Topology mask");
            sb.AppendLine($"sc.topologylist".PadRight(30) + "| List all possible Topology mask entries");
            sb.AppendLine($"sc.topologycreate".PadRight(30) + "| To create a new Topology mask");
            SendReply(arg, sb.ToString());
        }

        private Dictionary<string, int> GetCollectibles()
        {
            return UnityEngine.Object.FindObjectsOfType<CollectibleEntity>()
                .Where(c => !c.ShortPrefabName.Contains("mushroom")).GroupBy(c => c.ShortPrefabName)
                .ToDictionary(c => c.Key.Remove(c.Key.Length - 12), c => c.Count());
        }

        private Dictionary<string, IGrouping<string, BaseEntity>> GetOreNodes()
        {
            return UnityEngine.Object.FindObjectsOfType<ResourceEntity>().Where(c => c.name.Contains("-ore"))
                .Cast<BaseEntity>().GroupBy(c => c.ShortPrefabName).ToDictionary(c => c.Key, c => c);
        }

        private Dictionary<string, IGrouping<string, BaseEntity>> GetLootContainers()
        {
            return UnityEngine.Object.FindObjectsOfType<LootContainer>().Cast<BaseEntity>()
                .GroupBy(c => c.ShortPrefabName)
                .ToDictionary(c => c.Key, c => c);
        }

        private Dictionary<string, IGrouping<string, BaseEntity>> GetAnimals()
        {
            return UnityEngine.Object.FindObjectsOfType<BaseNpc>().Cast<BaseEntity>().GroupBy(c => c.ShortPrefabName)
                .ToDictionary(c => c.Key, c => c);
        }

        private string PrefabCut(string name)
        {
            return Oxide.Core.ExtensionMethods.Basename(name, ".prefab");
        }

        private void FixBarricades()
        {
            if (fixBarricadeStacking) return;
            PauseTick();
            List<BaseEntity> spawns = UnityEngine.Object.FindObjectsOfType<BaseEntity>()
                .Where(c => c.ShortPrefabName.StartsWith("door_barricade")).OrderBy(c => c.transform.position.x)
                .ThenBy(c => c.transform.position.z).ThenBy(c => c.transform.position.z).ToList();
            int count = spawns.Count();
            int racelimit = count * count;
            int antirace = 0;
            int deleted = 0;
            for (int i = 0; i < count; i++)
            {
                BaseEntity box = spawns[i];
                int next = i + 1;
                Vector2 pos = new Vector2(box.transform.position.x, box.transform.position.z);
                if (++antirace > racelimit) return;
                while (next < count)
                {
                    BaseEntity box2 = spawns[next];
                    Vector2 pos2 = new Vector2(box2.transform.position.x, box2.transform.position.z);
                    float distance = Vector2.Distance(pos, pos2);
                    if (++antirace > racelimit) return;
                    if (distance < 1.0f)
                    {
                        spawns.RemoveAt(next);
                        count--;
                        (box2 as BaseEntity).Kill();
                        deleted++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            ResumeTick();
            if (deleted > 0) Puts($"Removed {deleted} stacked DoorBarricades.");
        }
    }
}