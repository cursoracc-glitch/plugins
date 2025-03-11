using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine;
using Facepunch;
using System;
using Oxide.Core;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Deekay", "k1lly0u", "0.2.27")]
    class Deekay : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin ZoneManager;

        private static Deekay ins;

        private Hash<uint, DecayManager> dkEntities = new Hash<uint, DecayManager>();
        private bool isInitialized;
        #endregion

        #region Oxide Hooks        
        private void OnServerInitialized()
        {
            ins = this;
            InitializeConfigData();
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!isInitialized || entity == null)
                return;

            string prefabName = entity.ShortPrefabName;
            if (prefabName.Contains("autospawn") || prefabName.Contains("npc") || prefabName.Contains("static"))
                return;

            if (entity is BaseCombatEntity)
            {
                if (entity is BuildingPrivlidge)
                    return;

                ConfigData.DecayData decayData = null;
                if (entity is BuildingBlock)
                {
                    if (configData.Buildings.ContainsKey(prefabName))
                        configData.Buildings[prefabName].TryGetValue((entity as BuildingBlock).grade, out decayData);
                }
                else
                {
                    if (configData.Entities.ContainsKey(prefabName))
                        configData.Entities.TryGetValue(prefabName, out decayData);
                }
                if (decayData != null && decayData.IsEnabled)
                {
                    if (!dkEntities.ContainsKey(entity.net.ID))
                        dkEntities.Add(entity.net.ID, new DecayManager(entity as BaseCombatEntity, decayData));
                }
            }
        }
       
        private void OnDoorOpened(Door door, BasePlayer player)
        {
            if (!configData.MonitorActivity || door == null)
                return;           

            DecayTouch(door.transform.position);
        }

        private void OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            if (!configData.MonitorActivity || entity == null)
                return;

            DecayTouch(entity.transform.position);
        }

        private void OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (entity == null || entity.IsDestroyed || !entity.IsValid()) return;

            ConfigData.DecayData decayData = null;
            string prefabName = entity.ShortPrefabName;

            if (configData.Buildings.ContainsKey(prefabName))
            {
                if (configData.Buildings[prefabName].TryGetValue(grade, out decayData))
                {
                    if (!decayData.IsEnabled)
                    {
                        if (dkEntities.ContainsKey(entity.net.ID))
                        {
                            dkEntities[entity.net.ID].CancelInvokes();
                            dkEntities.Remove(entity.net.ID);
                        }
                        return;
                    }
                    if (dkEntities.ContainsKey(entity.net.ID))
                    {
                        dkEntities[entity.net.ID].CancelInvokes();
                        dkEntities[entity.net.ID] = new DecayManager(entity, decayData);
                    }
                    else dkEntities.Add(entity.net.ID, new DecayManager(entity, decayData));
                }
            }
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null)
                return;

            if (entity is BuildingPrivlidge)
            {
                BuildingManager.Building building = (entity as BuildingPrivlidge).GetBuilding();
                if (building != null)
                {
                    if (building.HasBuildingPrivileges())
                        return;

                    if (building.HasBuildingBlocks())
                    {
                        foreach (BuildingBlock block in building.buildingBlocks)
                        {
                            DecayManager manager;
                            if (dkEntities.TryGetValue(block.net.ID, out manager))
                                manager.ResetDecayOnDestroy();
                        }
                    }
                    if (building.HasDecayEntities())
                    {
                        foreach (DecayEntity decayEntity in building.decayEntities)
                        {
                            DecayManager manager;
                            if (dkEntities.TryGetValue(decayEntity.net.ID, out manager))
                                manager.ResetDecayOnDestroy();
                        }
                    }
                }
            }
            else
            {
                if (entity?.net?.ID == null)
                    return;

                if (dkEntities.ContainsKey(entity.net.ID))
                {
                    dkEntities[entity.net.ID].CancelInvokes();
                    dkEntities.Remove(entity.net.ID);
                }
            }                
        }

        private void Unload()
        {
            foreach(var entity in dkEntities)            
                entity.Value.CancelInvokes();            
            dkEntities.Clear();

            ins = null;
        }
        #endregion

        #region Functions        
        private void InitializeConfigData()
        {
            bool hasChanged = false;
            foreach (Construction construction in GetAllPrefabs<Construction>())
            {
                if (construction?.deployable == null && !string.IsNullOrEmpty(construction.hierachyName))
                {
                    if (construction.hierachyName.Contains("survey_crater"))
                        continue;

                    string hierachyName = construction.hierachyName.Replace("PrefabPreProcess - Server/", "");

                    if (!configData.Buildings.ContainsKey(hierachyName))
                    {
                        configData.Buildings.Add(hierachyName, new Dictionary<BuildingGrade.Enum, ConfigData.DecayData>
                            {
                                { BuildingGrade.Enum.Metal, configData.DefaultDecay },
                                { BuildingGrade.Enum.Stone, configData.DefaultDecay },
                                { BuildingGrade.Enum.TopTier, configData.DefaultDecay },
                                { BuildingGrade.Enum.Twigs, configData.DefaultDecay },
                                { BuildingGrade.Enum.Wood, configData.DefaultDecay }
                            });
                        hasChanged = true;
                    }
                }
            }
           
            foreach (ItemDefinition item in ItemManager.GetItemDefinitions())
            {
                ItemModDeployable deployable = item?.GetComponent<ItemModDeployable>();
                if (deployable == null)
                    continue;

                string fileName = deployable.entityPrefab.resourcePath;
                fileName = fileName.Substring(fileName.LastIndexOf("/") + 1);
                fileName = fileName.Substring(0, fileName.LastIndexOf("."));

                if (!configData.Entities.ContainsKey(fileName))
                {
                    configData.Entities.Add(fileName, configData.DefaultDecay);
                    hasChanged = true;
                }
            }

            if (hasChanged)
                SaveConfig();

            isInitialized = true;
            FindAllEntities();
        }

        private T[] GetAllPrefabs<T>()
        {
            
            Dictionary<uint, PrefabAttribute.AttributeCollection> prefabs = PrefabAttribute.server.prefabs;
            if (prefabs == null)
                return new T[0];

            List<T> results = new List<T>();
            foreach (PrefabAttribute.AttributeCollection prefab in prefabs.Values)
            {
                T[] arrayCache = prefab.Find<T>();
                if (arrayCache == null || !arrayCache.Any())
                    continue;

                results.AddRange(arrayCache);
            }

            return results.ToArray();
        }

        private void FindAllEntities()
        {
            PrintWarning("Finding all deekay-able entities");
            
            IEnumerable<BaseCombatEntity> entities = UnityEngine.Object.FindObjectsOfType<BaseCombatEntity>().Distinct();
            if (entities != null)
            {
                foreach (BaseCombatEntity entity in entities)
                {
                    if (entity == null || entity.IsDestroyed || !entity.IsValid())
                        continue;

                    if (entity is BuildingPrivlidge)
                        continue;

                    string prefabName = entity.ShortPrefabName;
                    if (string.IsNullOrEmpty(prefabName)) continue;

                    ConfigData.DecayData decayData;
                    if (!configData.Entities.TryGetValue(prefabName, out decayData))
                    {
                        if (!configData.Buildings.ContainsKey(prefabName))
                            continue;

                        BuildingGrade.Enum? grade = (entity as BuildingBlock)?.grade;
                        if (grade == null)
                            continue;

                        if (!configData.Buildings[prefabName].TryGetValue(grade.Value, out decayData))
                            continue;                        
                    }

                    if (decayData != null && decayData.IsEnabled && !dkEntities.ContainsKey(entity.net.ID))
                        dkEntities.Add(entity.net.ID, new DecayManager(entity, decayData));
                }
            }
            PrintWarning($"Initializing {dkEntities.Count} decay managers");
        }

        private void DecayTouch(Vector3 position)
        {
            List<BaseCombatEntity> list = Pool.GetList<BaseCombatEntity>();
            Vis.Entities<BaseCombatEntity>(position, configData.ActivityRadius, list, 2097408, QueryTriggerInteraction.Collide);
            for (int i = 0; i < list.Count; i++)
            {
                BaseCombatEntity entity = list[i];
                if (entity == null || entity.IsDestroyed || !entity.IsValid())
                    continue;

                DecayManager manager;
                if (dkEntities.TryGetValue(entity.net.ID, out manager))                
                    manager.ResetDecay();  
            }
            Pool.FreeList<BaseCombatEntity>(ref list);
        }        
        #endregion

        #region Decay Manager 
        private class DecayManager
        {
            public BaseCombatEntity Entity { get; private set; }

            private ConfigData.DecayData decayData;
            private bool isInPrivs = false;
            private bool isConstruction = false;
            private float decayRate;

            public DecayManager() { }
            public DecayManager(BaseCombatEntity entity, ConfigData.DecayData decayData)
            {
                this.Entity = entity;               
                this.decayData = decayData;
                isConstruction = entity is BuildingBlock;
                BeginDecay();
            }

            public void BeginDecay()
            {
                decayRate = IsInPrivilege(false) ? decayData.InsidePrivilege.DecayRate : decayData.OutsidePrivilege.DecayRate;

                if (decayRate == 0)
                    Entity.Invoke(BeginDecay, 300);
                else Entity.Invoke(SetInitialTimer, UnityEngine.Random.Range(1, 600));                
            }

            private void SetInitialTimer()
            {
                Entity.InvokeRepeating(RunDecay, decayRate, decayRate);
            }            

            private void RunDecay() => DealDamage();

            public bool DealDamage()
            {
                if (Entity == null || !Entity.IsValid())
                    return false;

                if (Entity.IsDestroyed)
                {
                    CancelInvokes();
                    ins.dkEntities.Remove(Entity.net.ID);
                    return false;
                }

                if (ins.configData.ZoneManager && ins.ZoneManager)
                {
                    object success = ins.ZoneManager.Call("EntityHasFlag", Entity, "nodecay");
                    if (success is bool && (bool)success)
                        return true;
                }               
                
                float decayAmount = IsInPrivilege(true) ? decayData.InsidePrivilege.DamageRate : decayData.OutsidePrivilege.DamageRate;
                if (decayAmount == 0)                
                    return false;

                if (isInPrivs && decayData.InsidePrivilege.UseUpkeep)
                {
                    if (Entity.GetBuildingPrivilege()?.GetProtectedMinutes() > 0)
                        return true;
                }

                float amount = Entity.MaxHealth() * (decayAmount / 100);
                if (Entity.health <= amount)
                {
                    CancelInvokes();
                    ins.dkEntities.Remove(Entity.net.ID);
                    Entity.Die();
                    return true;                   
                }
                else Entity.Hurt(amount, Rust.DamageType.Decay);

                return true;
            }            

            public bool IsInPrivilege(bool reset)
            {
                bool foundTC = false;

                if (isConstruction && ((Entity as BuildingBlock)?.GetBuilding()?.HasBuildingPrivileges() ?? false))
                    foundTC = true;
                else
                {
                    OBB obb = Entity.WorldSpaceBounds();
                    List<BuildingBlock> list = Pool.GetList<BuildingBlock>();
                    Vis.Entities<BuildingBlock>(obb.position, 16f + obb.extents.magnitude, list, 2097152, QueryTriggerInteraction.Collide);
                    for (int i = 0; i < list.Count; i++)
                    {
                        BuildingBlock item = list[i];

                        if (obb.Distance(item.WorldSpaceBounds()) <= 16f)
                        {
                            BuildingManager.Building building = item.GetBuilding();
                            if (building != null && building.HasBuildingPrivileges())
                            {
                                foundTC = true;
                                break;
                            }
                        }
                    }
                    Pool.FreeList<BuildingBlock>(ref list);
                }

                if (foundTC && !isInPrivs)
                {
                    isInPrivs = true;
                    if (reset)
                        ResetDecay();
                }
                else if (!foundTC && isInPrivs)
                {
                    isInPrivs = false;
                    if (reset)
                        ResetDecay();
                }
                return foundTC;
            } 

            public void ResetDecayOnDestroy()
            {
                decayRate = decayData.OutsidePrivilege.DecayRate;
                ResetDecay(true);
            }
            
            public void ResetDecay(bool addRandom = false)
            {
                if (Entity.IsInvoking(RunDecay))
                {
                    Entity.CancelInvoke(RunDecay);
                    Entity.InvokeRepeating(RunDecay, decayRate + (addRandom ? UnityEngine.Random.Range(1, 600) : 0), decayRate);
                }
            }

            public void CancelInvokes()
            {
                Entity.CancelInvoke(SetInitialTimer);
                Entity.CancelInvoke(BeginDecay);
                Entity.CancelInvoke(RunDecay);
            }

            public void UpdateDecayRates(ConfigData.DecayData decayData)
            {
                this.decayData = decayData;
                BeginDecay();
            }
        }
        #endregion        
       
        #region Commands
        [ConsoleCommand("dk.reset")]
        private void ccmdDKReset(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            configData.Buildings.Clear();
            configData.Entities.Clear();
            InitializeConfigData();
            SendReply(arg, "The config has been reset");
        }

        [ConsoleCommand("dk.rundecay")]
        private void ccmdDKRun(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            int decayCount = 0;
                      
            for (int i = dkEntities.Count - 1; i >= 0; i--)
            {
                var entry = dkEntities.ElementAt(i);
                if (entry.Value.DealDamage())
                {
                    entry.Value.ResetDecay();
                    ++decayCount;
                }
            }
            
            SendReply(arg, $"{decayCount} entities have been dealt decay damage!");
        }

        [ConsoleCommand("dk.setall")]
        private void ccmdDKSet(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            if (arg.Args == null || arg.Args.Length != 4)
            {
                SendReply(arg, "dk.setall <twigs/wood/stone/metal/toptier> <inside/outside> <damage amount (0 - 100 : percent of maximum health)> <decay timer (seconds)> - Set damage and time for all building blocks of the specified tier");
                return;
            }

            object grade = ParseType<BuildingGrade.Enum>(arg.Args[0]);
            if (grade == null)
            {
                SendReply(arg, $"The value \"{arg.Args[0]}\" is not a valid building grade. Valid types are: {Enum.GetNames(typeof(BuildingGrade.Enum)).ToSentence()}");
                return;
            }

            object inside = arg.Args[1].ToLower() == "inside" ? true : arg.Args[1].ToLower() == "outside" ? false : false;
            if (inside == null)
            {
                SendReply(arg, $"You must specify whether these values are \"inside\" or \"outside\" of privilege");
                return;
            }

            float damageRate = 0;
            if (!float.TryParse(arg.Args[2], out damageRate))
            {
                SendReply(arg, $"You must specify a numerical damage rate. {arg.Args[2]} is invalid");
                return;
            }            

            float decayTime = 0;
            if (!float.TryParse(arg.Args[3], out decayTime))
            {
                SendReply(arg, $"You must specify a numerical decay time. {arg.Args[3]} is invalid");
                return;
            }

            for (int i = configData.Buildings.Count - 1; i >= 0; i--)
            {
                var entry = configData.Buildings.ElementAt(i);
                ConfigData.DecayData.Rates rates = (bool)inside ? entry.Value[(BuildingGrade.Enum)grade].InsidePrivilege : entry.Value[(BuildingGrade.Enum)grade].OutsidePrivilege;
                rates.DamageRate = damageRate;
                rates.DecayRate = decayTime;
            }            
            
            SaveConfig();

            foreach (DecayManager decayManager in dkEntities.Values.Where(x => x.Entity is BuildingBlock && (x.Entity as BuildingBlock).grade == (BuildingGrade.Enum)grade))
            {
                if (decayManager.Entity == null || !decayManager.Entity.IsValid() || !configData.Buildings.ContainsKey(decayManager.Entity.ShortPrefabName)) continue;

                ConfigData.DecayData decayData;
                if (!configData.Buildings[decayManager.Entity.ShortPrefabName].TryGetValue((decayManager.Entity as BuildingBlock).grade, out decayData)) continue;

                timer.In(UnityEngine.Random.Range(0.1f, 10f), () => decayManager.UpdateDecayRates(decayData));
            }

            SendReply(arg, $"All building blocks of the grade \"{(BuildingGrade.Enum)grade}\" that are {((bool)inside ? "inside privilege" : "outside privilege")} now have a damage rate of {damageRate}% with a decay time of {decayTime} seconds");
        }

        private object ParseType<T>(string type)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), type, true);
            }
            catch
            {                
                return null;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Activity - Player activity will reset nearby decay timers")]
            public bool MonitorActivity { get; set; }
            [JsonProperty(PropertyName = "Activity - Radius of effect")]
            public float ActivityRadius { get; set; }
            [JsonProperty(PropertyName = "Decay - 1. Default decay settings for all entities when plugin first loads")]
            public DecayData DefaultDecay { get; set; }
            [JsonProperty(PropertyName = "Decay - 2. Decay settings for building blocks")]
            public Dictionary<string, Dictionary<BuildingGrade.Enum, DecayData>> Buildings { get; set; }
            [JsonProperty(PropertyName = "Decay - 3. Decay settings for all other entities")]
            public Dictionary<string, DecayData> Entities { get; set; }
            [JsonProperty(PropertyName = "ZoneManager - Ignore decay-able entities inside of 'nodecay' zones")]
            public bool ZoneManager { get; set; }

            public class DecayData
            {
                [JsonProperty(PropertyName = "Inside of privilege")]
                public InternalRates InsidePrivilege { get; set; }
                [JsonProperty(PropertyName = "Outside of privilege")]
                public Rates OutsidePrivilege { get; set; }
                [JsonProperty(PropertyName = "Decay is enabled for this entity")]
                public bool IsEnabled { get; set; }

                public class Rates
                {
                    [JsonProperty(PropertyName = "Damage per decay tick (% of max health)")]
                    public float DamageRate { get; set; }
                    [JsonProperty(PropertyName = "Time between decay passes")]
                    public float DecayRate { get; set; }
                }

                public class InternalRates : Rates
                {
                    [JsonProperty(PropertyName = "Use upkeep to handle decay when the TC has resources")]
                    public bool UseUpkeep { get; set; }
                }
            }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);            
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                ActivityRadius = 40f,
                DefaultDecay = new ConfigData.DecayData
                {
                    InsidePrivilege = new ConfigData.DecayData.InternalRates
                    {
                        DamageRate = 5f,
                        DecayRate = 3600,
                        UseUpkeep = false
                    },
                    OutsidePrivilege = new ConfigData.DecayData.Rates
                    {
                        DamageRate = 20f,
                        DecayRate = 3600
                    },
                    IsEnabled = true
                },
                MonitorActivity = true,
                Buildings = new Dictionary<string, Dictionary<BuildingGrade.Enum, ConfigData.DecayData>>(),
                Entities = new Dictionary<string, ConfigData.DecayData>(),
                Version = Version,
                ZoneManager = true
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 2, 20))
                configData = baseConfig;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }       
        #endregion               
    }
}
