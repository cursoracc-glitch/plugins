using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Rust.Ai.HTN;
using Rust.Ai.HTN.Sensors;
using Rust.Ai.HTN.Reasoning;
using Rust.Ai.HTN.Murderer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("ZombieHorde", "k1lly0u", "0.3.4")]
    class ZombieHorde : RustPlugin
    {
        [PluginReference] 
        private Plugin Kits, Spawns;

        private static ZombieHorde Instance { get; set; }   

        private const string SCARECROW_PREFAB = "assets/prefabs/npc/scarecrow/scarecrow.prefab";

        private const int WORLD_LAYER = 65536;
       

        #region Oxide Hooks       
        private void OnServerInitialized()
        {
            Instance = this;

            HordeThinkManager.Create();

            permission.RegisterPermission("zombiehorde.admin", this);
            permission.RegisterPermission("zombiehorde.ignore", this);

            _blueprintBase = ItemManager.FindItemDefinition("blueprintbase");
            _glowEyes = ItemManager.FindItemDefinition("gloweyes");

            if (!configData.Member.TargetedByPeaceKeeperTurrets)
                Unsubscribe(nameof(CanEntityBeHostile));

            ValidateLoadoutProfiles();
            ValidateSpawnSystem();
            CreateMonumentHordeOrders();
            NextTick(() => CreateRandomHordes());
        }
                
        private void OnEntityTakeDamage(BaseCombatEntity baseCombatEntity, HitInfo hitInfo)
        {
            if (hitInfo != null)
            {
                HordeMember hordeMember;

                if (hitInfo.InitiatorPlayer != null)
                {
                    hordeMember = hitInfo.InitiatorPlayer.GetComponent<HordeMember>();
                    if (hordeMember != null)
                    {
                        if (hordeMember.damageMultiplier != 1f)
                            hitInfo.damageTypes.ScaleAll(hordeMember.damageMultiplier);
                        return;
                    }
                }

                hordeMember = baseCombatEntity.GetComponent<HordeMember>();
                if (hordeMember != null)
                {
                    if (configData.Member.HeadshotKills && hitInfo.isHeadshot)
                        hitInfo.damageTypes.ScaleAll(1000);
                }
            }
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            if (player == null || hitInfo == null)
                return;

            HordeMember hordeMember = player.GetComponent<HordeMember>();
            if (hordeMember != null)
            {
                if (configData.Loot.DropInventory)
                    hordeMember.PrepareInventory();

                hordeMember.Manager.OnMemberDeath(hordeMember, hitInfo.Initiator as BaseCombatEntity);
                return;
            }

            if (configData.Horde.CreateOnDeath && hitInfo.InitiatorPlayer != null)
            {
                HordeMember attacker = hitInfo.InitiatorPlayer.GetComponent<HordeMember>();

                if (attacker != null && attacker.Manager != null)                
                    attacker.Manager.OnPlayerDeath(player, attacker);
            }
        }

        private void OnEntityKill(HTNPlayer htnPlayer)
        {
            HordeMember hordeMember = htnPlayer.GetComponent<HordeMember>();
            if (hordeMember != null && hordeMember.Manager != null)            
                hordeMember.Manager.OnMemberDeath(hordeMember, null);            
        }

        private object CanBeTargeted(BasePlayer player, MonoBehaviour behaviour)
        {
            HordeMember hordeMember = player.GetComponent<HordeMember>();
            if (hordeMember != null)
            {
                if (((behaviour is AutoTurret) || (behaviour is GunTrap) || (behaviour is FlameTurret)) && configData.Member.TargetedByTurrets)
                    return null;
                return false;
            }

            return null;
        }

        private object CanEntityBeHostile(HTNPlayer htnPlayer) => htnPlayer != null && htnPlayer.GetComponent<HordeMember>() != null ? (object)true : null;
        
        private object CanBradleyApcTarget(BradleyAPC bradleyAPC, HTNPlayer htnPlayer)
        {
            if (htnPlayer != null)
            {
                HordeMember hordeMember = htnPlayer.GetComponent<HordeMember>();
                if (hordeMember != null && !configData.Member.TargetedByAPC)
                    return false;
            }
            return null;
        }

        private object OnCorpsePopulate(HTNPlayer htnPlayer, NPCPlayerCorpse npcPlayerCorpse)
        {
            if (htnPlayer != null && npcPlayerCorpse != null)
            {
                HordeMember hordeMember = htnPlayer.GetComponent<HordeMember>();
                if (hordeMember == null)
                    return null;

                if (configData.Loot.DropInventory)
                {
                    hordeMember.MoveInventoryTo(npcPlayerCorpse);
                    return npcPlayerCorpse;
                }

                SpawnIntoContainer(npcPlayerCorpse);
                return npcPlayerCorpse;
            }
            return null;
        }

        private object CanPopulateLoot(HTNPlayer htnPlayer, NPCPlayerCorpse corpse) => htnPlayer != null && htnPlayer.GetComponent<HordeMember>() != null ? (object)false : null;

        private void Unload()
        {
            HordeManager.Order.OnUnload();

            _hordeThinkManager.Destroy();

            for (int i = HordeManager._allHordes.Count - 1; i >= 0; i--)
                HordeManager._allHordes[i].Destroy(true, true);

            HordeManager._allHordes.Clear();

            _spawnState = SpawnState.Spawn;

            configData = null;
            Instance = null;
        }
        #endregion

        #region Functions
        #region Horde Spawning
        private List<Vector3> _spawnPoints;

        private SpawnSystem _spawnSystem = SpawnSystem.None; 

        private bool ValidateSpawnSystem()
        {
            _spawnSystem = ParseType<SpawnSystem>(configData.Horde.SpawnType);

            if (_spawnSystem == SpawnSystem.None)
            {
                PrintError("You have set an invalid value in the config entry \"Spawn Type\". Unable to spawn hordes!");
                return false;
            }
            else if (_spawnSystem == SpawnSystem.SpawnsDatabase)
            {
                if (Spawns != null)
                {
                    if (string.IsNullOrEmpty(configData.Horde.SpawnFile))
                    {
                        PrintError("You have selected SpawnsDatabase as your method of spawning hordes, however you have not specified a spawn file. Unable to spawn hordes!");
                        return false;
                    }

                    object success = Spawns?.Call("LoadSpawnFile", configData.Horde.SpawnFile);
                    if (success is List<Vector3>)
                    {
                        _spawnPoints = success as List<Vector3>;
                        if (_spawnPoints.Count > 0)
                            return true;
                    }
                    PrintError("You have selected SpawnsDatabase as your method of spawning hordes, however the spawn file you have chosen is either invalid, or has no spawn points. Unable to spawn hordes!");
                    return false;
                }
                else PrintError("You have selected SpawnsDatabase as your method of spawning hordes, however SpawnsDatabase is not loaded on your server. Unable to spawn hordes!");
                return false;
            }
            
            return true;
        }

        private const int SPAWN_RAYCAST_MASK = 1 << 0 | 1 << 8 | 1 << 15 | 1 << 17 | 1 << 21 | 1 << 29;

        private const TerrainTopology.Enum SPAWN_TOPOLOGY_MASK = (TerrainTopology.Enum.Ocean | TerrainTopology.Enum.River | TerrainTopology.Enum.Lake | TerrainTopology.Enum.Cliff | TerrainTopology.Enum.Offshore | TerrainTopology.Enum.Summit);

        private Vector3 GetSpawnPoint()
        {
            switch (_spawnSystem)
            {
                case SpawnSystem.None:
                    break;

                case SpawnSystem.SpawnsDatabase:
                {
                    if (Spawns == null)
                    {
                        PrintError("Tried getting a spawn point but SpawnsDatabase is null. Make sure SpawnsDatabase is still loaded to continue using custom spawn points");
                        break;
                    }

                    if (_spawnPoints == null || _spawnPoints.Count == 0)
                    {
                        PrintError("No spawnpoints have been loaded from the designated spawnfile. Defaulting to Rust spawns");
                        break;
                    }

                    Vector3 spawnPoint = _spawnPoints.GetRandom();
                    _spawnPoints.Remove(spawnPoint);
                    if (_spawnPoints.Count == 0)
                        _spawnPoints = (List<Vector3>)Spawns.Call("LoadSpawnFile", configData.Horde.SpawnFile);

                    return spawnPoint;
                }
            }
            
            float size = (World.Size / 2f) * 0.75f;
            NavMeshHit navMeshHit;

            for (int i = 0; i < 10; i++)
            {
                Vector2 randomInCircle = UnityEngine.Random.insideUnitCircle * size;

                Vector3 position = new Vector3(randomInCircle.x, 0, randomInCircle.y);
                position.y = TerrainMeta.HeightMap.GetHeight(position);

                if (NavMesh.SamplePosition(position, out navMeshHit, 25f, 1))
                {                    
                    position = navMeshHit.position;

                    if (Physics.SphereCast(new Ray(position + (Vector3.up * 5f), Vector3.down), 10f, 10f, SPAWN_RAYCAST_MASK))
                        continue;

                    if (ContainsTopologyAtPoint(SPAWN_TOPOLOGY_MASK, position))
                        continue;

                    if (WaterLevel.GetWaterDepth(position, true, null) <= 0.01f)                    
                        return position;
                }
            }

            return ServerMgr.FindSpawnPoint().pos;
        }
        
        private void CreateMonumentHordeOrders()
        {
            int count = 0;
            GameObject[] allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (GameObject gobject in allobjects)
            {
                if (count >= configData.Horde.MaximumHordes)
                    break;

                if (gobject.name.Contains("autospawn/monument"))
                {
                    Transform tr = gobject.transform;
                    Vector3 position = tr.position;

                    if (position == Vector3.zero)
                        continue;

                    if (gobject.name.Contains("powerplant_1") && configData.Monument.Powerplant.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(-30.8f, 0.2f, -15.8f)), configData.Monument.Powerplant);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("military_tunnel_1") && configData.Monument.Tunnels.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(-7.4f, 13.4f, 53.8f)), configData.Monument.Tunnels);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("harbor_1") && configData.Monument.LargeHarbor.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(54.7f, 5.1f, -39.6f)), configData.Monument.LargeHarbor);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("harbor_2") && configData.Monument.SmallHarbor.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(-66.6f, 4.9f, 16.2f)), configData.Monument.SmallHarbor);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("airfield_1") && configData.Monument.Airfield.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(-12.4f, 0.2f, -28.9f)), configData.Monument.Airfield);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("trainyard_1") && configData.Monument.Trainyard.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(35.8f, 0.2f, -0.8f)), configData.Monument.Trainyard);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("water_treatment_plant_1") && configData.Monument.WaterTreatment.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(11.1f, 0.3f, -80.2f)), configData.Monument.WaterTreatment);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("warehouse") && configData.Monument.Warehouse.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(16.6f, 0.1f, -7.5f)), configData.Monument.Warehouse);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("satellite_dish") && configData.Monument.Satellite.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(18.6f, 6.0f, -7.5f)), configData.Monument.Satellite);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("sphere_tank") && configData.Monument.Dome.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(1518.7f, -11.4f, 175.4f)), configData.Monument.Dome);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("radtown_small_3") && configData.Monument.Radtown.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(-16.3f, -2.1f, -3.3f)), configData.Monument.Radtown);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("launch_site_1") && configData.Monument.LaunchSite.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(222.1f, 3.3f, 0.0f)), configData.Monument.LaunchSite);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("gas_station_1") && configData.Monument.GasStation.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(-9.8f, 3.0f, 7.2f)), configData.Monument.GasStation);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("supermarket_1") && configData.Monument.Supermarket.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(5.5f, 0.0f, -20.5f)), configData.Monument.Supermarket);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("mining_quarry_c") && configData.Monument.HQMQuarry.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(15.8f, 4.5f, -1.5f)), configData.Monument.HQMQuarry);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("mining_quarry_a") && configData.Monument.SulfurQuarry.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(-0.8f, 0.6f, 11.4f)), configData.Monument.SulfurQuarry);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("mining_quarry_b") && configData.Monument.StoneQuarry.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(-7.6f, 0.2f, 12.3f)), configData.Monument.StoneQuarry);
                        count++;
                        continue;
                    }

                    if (gobject.name.Contains("junkyard_1") && configData.Monument.Junkyard.Enabled)
                    {
                        HordeManager.Order.CreateOrder(tr.TransformPoint(new Vector3(12.8f, 10.8f, -58.4f)), configData.Monument.Junkyard);
                        count++;
                        continue;
                    }
                }
            }
        }

        private void CreateRandomHordes()
        {
            int amountToCreate = configData.Horde.MaximumHordes - HordeManager._allHordes.Count;
            for (int i = 0; i < amountToCreate; i++)
            {
                float roamDistance = configData.Horde.LocalRoam ? configData.Horde.RoamDistance : -1;
                string profile = configData.Horde.UseProfiles ? configData.HordeProfiles.Keys.ToArray().GetRandom() : string.Empty;

                HordeManager.Order.CreateOrder(GetSpawnPoint(), configData.Horde.InitialMemberCount, configData.Horde.MaximumMemberCount, roamDistance, profile);
            }
        }
        #endregion

        #region Inventory and Loot
        private ItemDefinition _blueprintBase;

        private ItemDefinition _glowEyes;

        private static MurdererDefinition _defaultDefinition;
        private static MurdererDefinition DefaultDefinition
        {
            get
            {
                if (_defaultDefinition == null)
                    _defaultDefinition = GameManager.server.FindPrefab(SCARECROW_PREFAB).GetComponent<HTNPlayer>().AiDefinition as MurdererDefinition;
                return _defaultDefinition;
            }
        }

        private void ValidateLoadoutProfiles()
        {
            Puts("Validating horde profiles...");

            bool hasChanged = false;

            for (int i = configData.HordeProfiles.Count - 1; i >= 0; i--)
            {
                string key = configData.HordeProfiles.ElementAt(i).Key;

                for (int y = configData.HordeProfiles[key].Count - 1; y >= 0; y--)
                {
                    string loadoutId = configData.HordeProfiles[key][y];

                    if (!configData.Member.Loadouts.Any(x => x.LoadoutID == loadoutId))
                    {
                        Puts($"Loadout profile {loadoutId} does not exist. Removing from config");
                        configData.HordeProfiles[key].Remove(loadoutId);
                        hasChanged = true;
                    }
                }

                if (configData.HordeProfiles[key].Count <= 0)
                {
                    Puts($"Horde profile {key} does not have any valid loadouts. Removing from config");
                    configData.HordeProfiles.Remove(key);
                    hasChanged = true;
                }
            }

            foreach (ConfigData.MemberOptions.Loadout loadout in configData.Member.Loadouts)
            {
                if (loadout.Vitals == null)
                {
                    loadout.Vitals = new ConfigData.MemberOptions.Loadout.VitalStats() { Health = DefaultDefinition.Vitals.HP };
                    hasChanged = true;
                }

                if (loadout.Movement == null)
                {
                    loadout.Movement = new ConfigData.MemberOptions.Loadout.MovementStats()
                    {
                        Acceleration = DefaultDefinition.Movement.Acceleration,
                        DuckSpeed = DefaultDefinition.Movement.DuckSpeed,
                        RunSpeed = DefaultDefinition.Movement.RunSpeed,
                        TurnSpeed = DefaultDefinition.Movement.TurnSpeed,
                        WalkSpeed = DefaultDefinition.Movement.WalkSpeed
                    };
                    hasChanged = true;
                }

                if (loadout.Sensory == null)
                {
                    loadout.Sensory = new ConfigData.MemberOptions.Loadout.SensoryStats()
                    {
                        FOV = DefaultDefinition.Sensory.FieldOfView,
                        HearingRange = DefaultDefinition.Sensory.HearingRange,
                        VisionRange = DefaultDefinition.Sensory.VisionRange
                    };
                    hasChanged = true;
                }
            }

            if (hasChanged)
                SaveConfig();
        }

        private void SpawnIntoContainer(LootableCorpse lootableCorpse)
        {
            int count = UnityEngine.Random.Range(configData.Loot.Random.Minimum, configData.Loot.Random.Maximum);

            int spawnedCount = 0;
            int loopCount = 0;

            while (true)
            {
                loopCount++;

                if (loopCount > 3)
                    return;

                float probability = UnityEngine.Random.Range(0f, 1f);

                List<ConfigData.LootTable.RandomLoot.LootDefinition> definitions = new List<ConfigData.LootTable.RandomLoot.LootDefinition>(configData.Loot.Random.List);

                for (int i = 0; i < configData.Loot.Random.List.Count; i++)
                {
                    ConfigData.LootTable.RandomLoot.LootDefinition lootDefinition = definitions.GetRandom();

                    definitions.Remove(lootDefinition);

                    if (lootDefinition.Probability >= probability)
                    {
                        CreateItem(lootDefinition, lootableCorpse.containers[0]);

                        spawnedCount++;

                        if (spawnedCount >= count)
                            return;
                    }
                }
            }
        }

        private void CreateItem(ConfigData.LootTable.RandomLoot.LootDefinition lootDefinition, ItemContainer container)
        {
            Item item;

            if (!lootDefinition.IsBlueprint)
                item = ItemManager.CreateByName(lootDefinition.Shortname, lootDefinition.GetAmount(), lootDefinition.SkinID);
            else
            {
                item = ItemManager.Create(_blueprintBase);
                item.blueprintTarget = ItemManager.FindItemDefinition(lootDefinition.Shortname).itemid;
            }

            if (item != null)
            {
                item.OnVirginSpawn();
                if (!item.MoveToContainer(container, -1, true))
                    item.Remove(0f);
            }

            if (lootDefinition.Required != null)
                CreateItem(lootDefinition.Required, container);
        }

        private static void StripInventory(BasePlayer player, bool skipWear = false)
        {
            List<Item> list = Pool.GetList<Item>();

            player.inventory.AllItemsNoAlloc(ref list);

            for (int i = list.Count - 1; i >= 0; i--)
            {
                Item item = list[i];

                if (skipWear && item?.parent == player.inventory.containerWear)
                    continue;

                item.RemoveFromContainer();
                item.Remove();
            }

            Pool.FreeList(ref list);
        }

        private static void ClearContainer(ItemContainer container, bool skipWear = false)
        {
            if (container == null || container.itemList == null)
                return;

            while (container.itemList.Count > 0)
            {
                Item item = container.itemList[0];
                item.RemoveFromContainer();
                item.Remove(0f);
            }
        }        
        #endregion

        #region Spawning
        private static HTNPlayer InstantiateEntity(Vector3 position)
        {
            GameObject gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(SCARECROW_PREFAB), position, Quaternion.identity);
            gameObject.name = SCARECROW_PREFAB;

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            HTNPlayer component = gameObject.GetComponent<HTNPlayer>();
            return component;
        }

        private static NavMeshHit navHit;
        private static RaycastHit raycastHit;
        private static Collider[] _buffer = new Collider[256];

        private static object FindPointOnNavmesh(Vector3 targetPosition, float maxDistance = 4f)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 position = i == 0 ? targetPosition : targetPosition + (UnityEngine.Random.onUnitSphere * maxDistance);
                if (NavMesh.SamplePosition(position, out navHit, maxDistance, 1))
                {
                    if (IsInRockPrefab(navHit.position))                    
                        continue;                    

                    if (IsNearWorldCollider(navHit.position))                    
                        continue;  

                    return navHit.position;
                }
            }
            return null;
        } 

        private static bool IsInRockPrefab(Vector3 position)
        {
            Physics.queriesHitBackfaces = true;

            bool isInRock = Physics.Raycast(position, Vector3.up, out raycastHit, 20f, WORLD_LAYER, QueryTriggerInteraction.Ignore) && 
                            blockedColliders.Any(s => raycastHit.collider?.gameObject?.name.Contains(s) ?? false);

            Physics.queriesHitBackfaces = false;

            return isInRock;
        }

        private static bool IsNearWorldCollider(Vector3 position)
        {
            Physics.queriesHitBackfaces = true;

            int count = Physics.OverlapSphereNonAlloc(position, 2f, _buffer, WORLD_LAYER, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = false;

            int removed = 0;
            for (int i = 0; i < count; i++)
            {                
                if (acceptedColliders.Any(s => _buffer[i].gameObject.name.Contains(s)))
                  removed++;
            }


            return count - removed > 0;
        }

        private static readonly string[] acceptedColliders = new string[] { "road", "rocket_factory", "range", "train_track", "runway", "_grounds", "concrete_slabs", "lighthouse", "cave", "office", "walkways", "sphere", "tunnel", "industrial", "junkyard" };

        private static readonly string[] blockedColliders = new string[] { "rock", "junk", "range", "invisible" };
        #endregion

        private T ParseType<T>(string type)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), type, true);
            }
            catch
            {
                return default(T);
            }
        }

        private static bool ContainsTopologyAtPoint(TerrainTopology.Enum mask, Vector3 position) => (TerrainMeta.TopologyMap.GetTopology(position) & (int)mask) != 0;
        #endregion

        #region Think Manager
        private HordeThinkManager _hordeThinkManager;

        private static SpawnState _spawnState = SpawnState.Spawn;


        private class HordeThinkManager : MonoBehaviour
        {
            internal static void Create() => Instance._hordeThinkManager = new GameObject("ZombieHorde-ThinkManager").AddComponent<HordeThinkManager>();

            private void Awake()
            {
                _spawnState = configData.TimedSpawns.Enabled ? (ShouldSpawn() ? SpawnState.Spawn : SpawnState.Despawn) : SpawnState.Spawn;

                if (configData.TimedSpawns.Enabled)
                    InvokeHandler.InvokeRepeating(this, CheckTimeTick, 0f, 1f);
            }

            internal void Update()
            {
                HordeManager._hordeTickQueue.RunQueue(0.5);
                HordeMember._memberTickQueue.RunQueue(0.5);
            }

            internal void Destroy()
            {
                if (configData.TimedSpawns.Enabled)
                    InvokeHandler.CancelInvoke(this, CheckTimeTick);

                Destroy(gameObject);
            }

            private bool ShouldSpawn()
            {
                float currentTime = TOD_Sky.Instance.Cycle.Hour;

                if (configData.TimedSpawns.Start > configData.TimedSpawns.End)
                    return currentTime > configData.TimedSpawns.Start || currentTime < configData.TimedSpawns.End;
                else return currentTime > configData.TimedSpawns.Start && currentTime < configData.TimedSpawns.End;
            }
                         
            private void CheckTimeTick()
            {
                if (ShouldSpawn())
                {
                    if (_spawnState == SpawnState.Despawn)
                    {
                        _spawnState = SpawnState.Spawn;
                        HordeManager.Order.StopDespawning();
                        HordeManager.Order.BeginSpawning();
                    }
                }
                else
                {
                    if (_spawnState == SpawnState.Spawn)
                    {
                        _spawnState = SpawnState.Despawn;

                        if (configData.TimedSpawns.Despawn)
                        {
                            HordeManager.Order.StopSpawning();
                            HordeManager.Order.BeginDespawning();
                        }
                    }
                }
            }
        }

        internal class HordeMemberTickQueue : ObjectWorkQueue<HordeMember>
        {
            protected override void RunJob(HordeMember hordeMember)
            {
                if (!ShouldAdd(hordeMember))
                    return;

                hordeMember.OnTick();
            }

            protected override bool ShouldAdd(HordeMember hordeMember)
            {
                if (!base.ShouldAdd(hordeMember))
                    return false;

                return hordeMember != null && hordeMember.Entity != null && hordeMember.Entity.IsValid();
            }
        }

        internal class HordeTickQueue : ObjectWorkQueue<HordeManager>
        {
            protected override void RunJob(HordeManager hordeManager)
            {
                if (!ShouldAdd(hordeManager))
                    return;

                hordeManager.HordeTick();
                
                Instance.timer.In(3f, ()=> HordeManager._hordeTickQueue.Add(hordeManager));
            }

            protected override bool ShouldAdd(HordeManager hordeManager)
            {
                if (!base.ShouldAdd(hordeManager))
                    return false;

                return hordeManager != null && !hordeManager.isDestroyed && hordeManager.members?.Count > 0;
            }
        }
        #endregion

        #region Horde Manager
        internal class HordeManager
        {
            internal static List<HordeManager> _allHordes = new List<HordeManager>();

            internal static HordeTickQueue _hordeTickQueue = new HordeTickQueue();

            internal List<HordeMember> members;

            internal Vector3 Destination;

            internal Vector3 AverageLocation;


            internal BaseCombatEntity PrimaryTarget;

            internal NpcPlayerInfo NpcPlayerInfo;

            internal AnimalInfo AnimalInfo;


            internal bool DebugMode { get; set; } = false;


            private bool isRoaming = true;

            private bool isRegrouping = false;

            internal bool isDestroyed = false;


            private Vector3 initialSpawnPosition;

            private int initialMemberCount;

            private bool isLocalHorde = false;

            private float maximumRoamDistance;

            internal string hordeProfile;


            private float nextGrowthTime = Time.time + configData.Horde.GrowthRate;

            private int maximumMemberCount;

            private float nextMergeTime = Time.time + MERGE_COOLDOWN;

            private float refreshRoamTime;

            private float verifyTargetTime;

            internal bool PathStateFailed { get; set; }


            private const float MERGE_COOLDOWN = 180f;

            private const float ROAM_REFRESH_RATE = 1f;

            private const float TARGET_VERIFY_RATE = 3f;


            internal static bool Create(Order order)
            {
                HordeManager manager = new HordeManager
                {
                    members = Pool.GetList<HordeMember>(),
                    initialSpawnPosition = order.position,
                    isLocalHorde = order.maximumRoamDistance > 0,
                    maximumRoamDistance = order.maximumRoamDistance,
                    initialMemberCount = order.initialMemberCount,
                    maximumMemberCount = order.maximumMemberCount,
                    hordeProfile = order.hordeProfile
                };                  

                for (int i = 0; i < order.initialMemberCount; i++)                
                    manager.SpawnMember(order.position, false);

                if (manager.members.Count == 0)
                {
                    manager.Destroy();
                    return false;
                }

                _allHordes.Add(manager);

                _hordeTickQueue.Add(manager);

                return true;
            }

            internal void Destroy(bool permanent = false, bool killNpcs = false)
            {
                isDestroyed = true;

                if (killNpcs)
                {
                    for (int i = members.Count - 1; i >= 0; i--)
                    {
                        HordeMember hordeMember = members[i];
                        if (hordeMember != null && hordeMember.Entity != null && !hordeMember.Entity.IsDestroyed)
                        {
                            StripInventory(hordeMember.Entity);
                            hordeMember.Entity.Kill();
                        }
                    }
                }

                members.Clear();
                Pool.FreeList(ref members);

                _allHordes.Remove(this);

                if (!permanent && _allHordes.Count <= configData.Horde.MaximumHordes)                
                    InvokeHandler.Invoke(Instance._hordeThinkManager, () => 
                    Order.CreateOrder(isLocalHorde ? initialSpawnPosition : Instance.GetSpawnPoint(), initialMemberCount, maximumMemberCount, isLocalHorde ? maximumRoamDistance : -1f, hordeProfile), configData.Horde.RespawnTime);                
            }

            internal void HordeTick()
            {
                if (members.Count == 0 || isDestroyed)                
                    return;

                if (DebugMode)
                {
                    foreach(BasePlayer player in BasePlayer.activePlayerList)
                    {
                        if (player.IsAdmin)
                            player.SendConsoleCommand("ddraw.text", 10f, Color.green, AverageLocation + new Vector3(0, 1.5f, 0), $"<size=20>Horde {_allHordes.IndexOf(this)}</size>");
                    }
                }
                
                bool hasSeentargetRecently = false;
                bool isDormant = true;

                for (int i = members.Count - 1; i >= 0; i--)
                {
                    HordeMember member = members[i];

                    if (!member.Entity.IsDormant)
                        isDormant = false;

                    if (!isRoaming && !hasSeentargetRecently)
                    {
                        if (Time.time - member.lastSeenTargetTime < configData.Horde.ForgetTime)
                        {
                            hasSeentargetRecently = true;

                            if (PrimaryTarget != null)                            
                                Destination = PrimaryTarget.ServerPosition;                            
                        }
                    }
                }

                if (isDormant)
                    return;

                AverageLocation = GetAverageVector();

                TryMergeHordes();

                TryGrowHorde();

                bool hasValidTarget = PrimaryTarget != null && IsValidTarget(PrimaryTarget);

                if (hasSeentargetRecently && Time.time > verifyTargetTime)
                {
                    verifyTargetTime = Time.time + TARGET_VERIFY_RATE;
                    if (!hasValidTarget)
                    {
                        if (DebugMode)
                            Debug.Log($"Target is invalid, set roaming");
                        hasSeentargetRecently = false;
                        SetPrimaryTarget(null);
                    }
                }

                if (!isRoaming && !hasSeentargetRecently)
                {
                    if (DebugMode)
                        Debug.Log($"No targets, set roaming");

                    isRoaming = true;
                }

                if (isRoaming)
                {
                    if (Time.time > refreshRoamTime)
                    {
                        refreshRoamTime = Time.time + ROAM_REFRESH_RATE;

                        if ((!isRegrouping && GetMaximumSeperation() > 15f) || (isRegrouping && GetMaximumSeperation() > 5))
                        {                            
                            isRegrouping = true;

                            Destination = members.GetRandom().Transform.position;

                            if (DebugMode)
                                Debug.Log($"Regroup at {Destination}");

                            goto UPDATE_ROAM;
                        }

                        if (Destination == Vector3.zero || Vector3.Distance(Destination, AverageLocation) < 10f || members.All(x => x.Domain.NavAgent.isPathStale))
                        {                            
                            isRegrouping = false;

                            Destination = GetRandomLocation(isLocalHorde ? initialSpawnPosition : AverageLocation);
                           
                            if (DebugMode)
                                Debug.Log($"Set new destination at {Destination}");

                            goto UPDATE_ROAM;
                        }

                        if (PathStateFailed)
                        {
                            Destination = GetRandomLocation(isLocalHorde ? initialSpawnPosition : AverageLocation);
                            PathStateFailed = false;

                            if (DebugMode)
                                Debug.Log($"Path has failed. New destination {Destination}");
                        }

                        UPDATE_ROAM:
                        if (DebugMode)
                            Debug.Log($"Update roam target | Current {AverageLocation} | Destination {Destination} | Distance {Vector3.Distance(AverageLocation, Destination)}");
                        UpdateRoamTarget();
                    }
                }
            }

            internal void EvaluateTarget(NpcPlayerInfo target)
            {
                if (target.Player == null || target.Player.IsDead() || target.Player.transform == null || PrimaryTarget == target.Player)
                    return;

                if (PrimaryTarget == null || PrimaryTarget.IsDead())
                {
                    SetPrimaryTarget(target.Player, target);
                    return;
                }

                if (PrimaryTarget.transform == null)
                    return;

                if (Vector3.Distance(AverageLocation, target.Player.transform.position) < Vector3.Distance(AverageLocation, PrimaryTarget.transform.position) || PrimaryTarget is BaseNpc)
                {
                    SetPrimaryTarget(target.Player, target);
                    return;
                }
            }

            internal void EvaluateTarget(AnimalInfo target)
            {
                if (target.Animal == null || target.Animal.IsDead() || target.Animal.transform == null || PrimaryTarget == target.Animal)
                    return;

                if (PrimaryTarget == null || PrimaryTarget.IsDead())
                {
                    SetPrimaryTarget(target.Animal, target);
                    return;
                }

                if (PrimaryTarget.transform == null || PrimaryTarget is BasePlayer)
                    return;

                if (Vector3.Distance(AverageLocation, target.Animal.transform.position) < Vector3.Distance(AverageLocation, PrimaryTarget.transform.position))
                {
                    SetPrimaryTarget(target.Animal, target);
                    return;
                }
            }

            internal void SetPrimaryTarget(BaseCombatEntity baseCombatEntity, NpcPlayerInfo info)
            {
                if (baseCombatEntity == null || baseCombatEntity.transform == null)
                    return;

                SetPrimaryTarget(baseCombatEntity);

                PrimaryTarget = baseCombatEntity;

                NpcPlayerInfo = info;

                AnimalInfo = default(AnimalInfo);

                for (int i = 0; i < members.Count; i++)
                {
                    members[i].OnTargetUpdated(info);
                }
            }

            internal void SetPrimaryTarget(BaseCombatEntity baseCombatEntity, AnimalInfo info)
            {
                if (baseCombatEntity == null || baseCombatEntity.transform == null)
                    return;

                SetPrimaryTarget(baseCombatEntity);

                PrimaryTarget = baseCombatEntity;

                NpcPlayerInfo = default(NpcPlayerInfo);

                AnimalInfo = info;

                for (int i = 0; i < members.Count; i++)
                {
                    members[i].OnTargetUpdated(info);
                }
            }

            internal void SetPrimaryTarget(BaseCombatEntity baseCombatEntity)
            {
                if (baseCombatEntity == null || baseCombatEntity.transform == null)
                {
                    NpcPlayerInfo = default(NpcPlayerInfo);
                    AnimalInfo = default(AnimalInfo);
                    isRoaming = true;
                    Destination = AverageLocation;
                }
                else
                {
                    isRoaming = false;
                    isRegrouping = false;
                    Destination = baseCombatEntity.transform.position;
                }
            }

            private void UpdateRoamTarget()
            {
                for (int i = 0; i < members.Count; i++)
                {
                    HordeMember hordeMember = members[i];
                    if (hordeMember != null)
                        hordeMember.SetRoamToDestination();
                }
            }

            internal Vector3 GetAverageVector()
            {
                Vector3 location = Vector3.zero;

                if (members.Count == 0)
                    return location;

                int count = 0;
                for (int i = 0; i < members.Count; i++)
                {
                    HordeMember hordeMember = members[i];

                    if (hordeMember == null || hordeMember.Entity == null)
                        continue;

                    location += hordeMember.Transform.position;
                    count++;
                }

                return location /= count;
            }

            
            private const TerrainTopology.Enum DESTINATION_TOPOLOGY_MASK = (TerrainTopology.Enum.Ocean | TerrainTopology.Enum.River | TerrainTopology.Enum.Lake | TerrainTopology.Enum.Offshore | TerrainTopology.Enum.Cliff);

            private Vector3 GetRandomLocation(Vector3 from)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 vector2 = UnityEngine.Random.insideUnitCircle * (isLocalHorde ? maximumRoamDistance : 100f);
                    
                    Vector3 destination = from + new Vector3(vector2.x, 0f, vector2.y);
                    if (TerrainMeta.HeightMap != null)                    
                        destination.y = TerrainMeta.HeightMap.GetHeight(destination);

                    NavMeshHit navMeshHit;
                    if (NavMesh.FindClosestEdge(destination, out navMeshHit, 1))
                    {
                        destination = navMeshHit.position;
                        if (WaterLevel.GetWaterDepth(destination, true, null) <= 0.01f && !ContainsTopologyAtPoint(DESTINATION_TOPOLOGY_MASK, destination))                                                    
                            return destination;                        
                    }
                    else if (NavMesh.SamplePosition(destination, out navMeshHit, 5f, 1) && !ContainsTopologyAtPoint(DESTINATION_TOPOLOGY_MASK, destination))
                    {                        
                        destination = navMeshHit.position;
                        if (WaterLevel.GetWaterDepth(destination, true, null) <= 0.01f)
                            return destination;                        
                    }
                }
                return AverageLocation;
            }

            private float GetMaximumSeperation()
            {
                float distance = 0;

                for (int i = 0; i < members.Count; i++)
                {
                    HordeMember hordeMember = members[i];
                    if (hordeMember != null && hordeMember.Entity != null)
                    {
                        float d = Vector3.Distance(hordeMember.Transform.position, AverageLocation);
                        if (d > distance)
                            distance = d;
                    }
                }

                return distance;
            }

            internal bool IsValidTarget(BaseCombatEntity baseCombatEntity)
            {
                if (baseCombatEntity == null || baseCombatEntity.IsDestroyed || baseCombatEntity.transform == null)
                    return false;

                if (baseCombatEntity.Health() <= 0f)
                    return false;

                if (baseCombatEntity is BasePlayer)
                {
                    BasePlayer player = baseCombatEntity as BasePlayer;

                    if (player.IsDead())
                        return false;

                    if (player._limitedNetworking)
                        return false;

                    if (player.IsFlying)
                        return false;

                    if (player is HTNPlayer && (player as HTNPlayer).AiDefinition?.Info?.Family == BaseNpcDefinition.Family.Murderer)
                        return false;

                    if (!configData.Member.TargetNPCs)
                    {
                        if (player.IsNpc)
                            return false;

                        if (player is NPCPlayer || player is HTNPlayer)
                            return false;
                    }

                    if (!configData.Member.TargetHumanNPCs && !player.userID.IsSteamId() && !player.IsNpc)
                        return false;

                    if (player.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
                        return false;

                    if (configData.Member.IgnoreSleepers && player.IsSleeping())
                        return false;

                    if (player.userID.IsSteamId() && Instance.permission.UserHasPermission(player.UserIDString, "zombiehorde.ignore"))
                        return false;
                }

                return true;
            }

            internal bool SpawnMember(Vector3 position, bool alreadyInitialized = true)
            {                
                HTNPlayer htnPlayer = InstantiateEntity(position);                
                htnPlayer.enableSaving = false;
                htnPlayer.Spawn();

                HordeMember member = htnPlayer.gameObject.AddComponent<HordeMember>();
                member.Manager = this;
                members.Add(member);

                if (alreadyInitialized)
                {
                    if (PrimaryTarget == null || PrimaryTarget is BasePlayer)
                        member.OnTargetUpdated(NpcPlayerInfo);
                    else if (PrimaryTarget is BaseNpc)
                        member.OnTargetUpdated(AnimalInfo);
                }

                return true;
            }

            internal void OnPlayerDeath(BasePlayer player, HordeMember hordeMember)
            {
                if (hordeMember == null || !members.Contains(hordeMember))
                    return;

                if (members.Count < maximumMemberCount)
                    SpawnMember(hordeMember.Transform.position);
            }

            internal void OnMemberDeath(HordeMember hordeMember, BaseCombatEntity initiator)
            {
                if (isDestroyed || members == null)
                    return;

                members.Remove(hordeMember);

                if (members.Count == 0)
                    Destroy();
                else
                {
                    if (PrimaryTarget == null && initiator is BasePlayer)
                    {
                        SetPrimaryTarget(initiator, new NpcPlayerInfo()
                        {
                            Player = initiator as BasePlayer,
                            Time = Time.time,
                            SqrDistance = (initiator.transform.position - AverageLocation).sqrMagnitude,
                            BodyVisible = true,
                            HeadVisible = true,
                        });
                    }
                }
            }

            private void TryGrowHorde()
            {
                if (nextGrowthTime < Time.time)
                {
                    if (members.Count < maximumMemberCount)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            if (SpawnMember(members.GetRandom().Transform.position))
                                break;
                        }
                    }

                    nextGrowthTime = Time.time + configData.Horde.GrowthRate;
                }
            }

            private void TryMergeHordes()
            {
                if (!configData.Horde.MergeHordes || nextMergeTime > Time.time)
                    return;

                for (int y = _allHordes.Count - 1; y >= 0; y--)
                {
                    HordeManager manager = _allHordes[y];

                    if (manager == this)
                        continue;

                    if (members.Count >= maximumMemberCount)
                        return;

                    if (Vector3.Distance(AverageLocation, manager.AverageLocation) < 20)
                    {
                        int amountToMerge = maximumMemberCount - members.Count;
                        if (amountToMerge >= manager.members.Count)
                        {
                            for (int i = 0; i < manager.members.Count; i++)
                            {
                                HordeMember member = manager.members[i];
                                members.Add(member);
                                member.Manager = this;
                            }

                            manager.members.Clear();
                            manager.Destroy();

                            nextMergeTime = Time.time + MERGE_COOLDOWN;
                        }
                        else
                        {
                            bool hasMerged = false;
                            for (int i = 0; i < amountToMerge; i++)
                            {
                                if (manager.members.Count > 0)
                                {
                                    HordeMember member = manager.members[0];

                                    members.Add(member);

                                    member.Manager = this;

                                    manager.members.Remove(member);

                                    hasMerged = true;
                                }
                            }

                            if (hasMerged)                            
                                nextMergeTime = Time.time + MERGE_COOLDOWN;                            
                        }
                    }
                }
            }

            public class Order
            {
                public Vector3 position;
                public int initialMemberCount;
                public int maximumMemberCount;
                public float maximumRoamDistance;
                public string hordeProfile;

                public Order(Vector3 position, int initialMemberCount, string hordeProfile)
                {
                    this.position = position;
                    this.initialMemberCount = initialMemberCount;
                    maximumMemberCount = configData.Horde.MaximumMemberCount;
                    maximumRoamDistance = -1f;
                    this.hordeProfile = hordeProfile;
                }

                public Order(Vector3 position, int initialMemberCount, int maximumMemberCount, float maximumRoamDistance, string hordeProfile)
                {
                    this.position = position;
                    this.initialMemberCount = initialMemberCount;
                    this.maximumMemberCount = maximumMemberCount;
                    this.maximumRoamDistance = maximumRoamDistance;
                    this.hordeProfile = hordeProfile;
                }

                //private static WaitUntil waitUntilFinishedSpawning = new WaitUntil(() => IsSpawning == false);

                //private static WaitUntil waitUntilFinishedDespawning = new WaitUntil(() => IsDespawning == false);


                private static Queue<Order> _queue = new Queue<Order>();

                private static bool IsSpawning { get; set; }

                private static bool IsDespawning { get; set; }

                private static Coroutine SpawnRoutine { get; set; }

                private static Coroutine DespawnRoutine { get; set; }

                internal static void CreateOrder(Vector3 position, int initialMemberCount, int maximumMemberCount, float maximumRoamDistance, string hordeProfile)
                {
                    object success = FindPointOnNavmesh(position, 10f);
                    if (success == null)
                        return;

                    _queue.Enqueue(new Order((Vector3)success, initialMemberCount, maximumMemberCount, maximumRoamDistance, hordeProfile));

                    if (!IsSpawning && _spawnState == SpawnState.Spawn)                    
                        BeginSpawning();                    
                }

                internal static void CreateOrder(Vector3 position, ConfigData.MonumentSpawn.MonumentSettings settings)
                {
                    object success = FindPointOnNavmesh(position, 10f);
                    if (success == null)                    
                        return;
                    
                    _queue.Enqueue(new Order((Vector3)success, configData.Horde.InitialMemberCount, settings.HordeSize, settings.RoamDistance, settings.Profile));

                    if (!IsSpawning && _spawnState == SpawnState.Spawn)
                        BeginSpawning();
                }

                internal static Coroutine BeginSpawning() => SpawnRoutine = ServerMgr.Instance.StartCoroutine(ProcessSpawnOrders());

                internal static Coroutine BeginDespawning() => DespawnRoutine = ServerMgr.Instance.StartCoroutine(ProcessDespawn());   
                
                internal static void StopSpawning()
                {
                    if (SpawnRoutine != null)
                        ServerMgr.Instance.StopCoroutine(SpawnRoutine);

                    IsSpawning = false;
                }

                internal static void StopDespawning()
                {
                    if (DespawnRoutine != null)
                        ServerMgr.Instance.StopCoroutine(DespawnRoutine);

                    IsDespawning = false;
                }

                private static IEnumerator ProcessSpawnOrders()
                {
                    if (_queue.Count == 0)
                        yield break;

                    IsSpawning = true;

                    RESTART:
                    if (IsDespawning)
                        StopDespawning();

                    while (_allHordes?.Count > configData.Horde.MaximumHordes)                    
                        yield return CoroutineEx.waitForSeconds(10f);
                    
                    Order order = _queue.Dequeue();

                    if (order != null)
                        Create(order);

                    if (_queue.Count > 0)
                    {
                        yield return CoroutineEx.waitForSeconds(3f);
                        goto RESTART;
                    }

                    IsSpawning = false;
                }

                private static IEnumerator ProcessDespawn()
                {
                    IsDespawning = true;

                    if (IsSpawning)
                        StopSpawning();

                    while (_allHordes?.Count > 0)
                    {
                        HordeManager manager = HordeManager._allHordes.GetRandom();
                        if (manager.PrimaryTarget == null)
                        {
                            Order.CreateOrder(manager.isLocalHorde ? manager.initialSpawnPosition : Instance.GetSpawnPoint(), manager.initialMemberCount,
                                              manager.maximumMemberCount, manager.isLocalHorde ? manager.maximumRoamDistance : -1f, manager.hordeProfile);

                            manager.Destroy(true, true);
                        }

                        yield return CoroutineEx.waitForSeconds(3f);
                    }

                    IsDespawning = false;
                }

                internal static void OnUnload()
                {
                    if (SpawnRoutine != null)
                        ServerMgr.Instance.StopCoroutine(SpawnRoutine);

                    if (DespawnRoutine != null)
                        ServerMgr.Instance.StopCoroutine(DespawnRoutine);

                    IsDespawning = false;
                    IsSpawning = false;

                   _queue.Clear();
                }
            }
        }
        #endregion

        #region Horde Member
        internal class HordeMember : MonoBehaviour
        {
            internal static HordeMemberTickQueue _memberTickQueue = new HordeMemberTickQueue();

            internal HTNPlayer Entity { get; private set; }

            internal MurdererDomain Domain { get; private set; }

            internal MurdererContext Context { get; private set; }

            internal MurdererMemory Memory { get; private set; }

            internal HordeManager Manager { get; set; }

            internal Transform Transform { get; set; }


            internal List<AnimalInfo> animalsLineOfSight = Pool.GetList<AnimalInfo>();

            internal float damageMultiplier;

            internal float lastSeenTargetTime;

            private bool lightsOn;

            private ItemContainer[] containers;

            private void Awake()
            {
                Entity = GetComponent<HTNPlayer>();

                Transform = Entity.transform;

                Domain = Entity.AiDomain as MurdererDomain;
                Context = Domain.MurdererContext;
                Memory = Context.Memory;

                InitializeSensorsAndReasoners();

                InitializeNpc();
            }

            private void OnDestroy()
            {
                Pool.Free(ref animalsLineOfSight);
            }

            private void InitializeSensorsAndReasoners()
            {
                int index = Domain.Reasoners.FindIndex(x => x is Rust.Ai.HTN.Murderer.Reasoners.ReturnHomeReasoner);
                Domain.Reasoners.RemoveAt(index);

                index = Domain.Sensors.FindIndex(x => x is Rust.Ai.HTN.Sensors.PlayersInRangeSensor);
                Domain.Sensors.RemoveAt(index);
                Domain.Sensors.Insert(index, new PlayersInRangeSensor(this));

                index = Domain.Sensors.FindIndex(x => x is Rust.Ai.HTN.Murderer.Sensors.AnimalsInRangeSensor);
                Domain.Sensors.RemoveAt(index);

                if (configData.Member.TargetAnimals)
                {
                    Domain.Sensors.Insert(index, new AnimalsInRangeSensor(this));

                    index = Domain.Reasoners.FindIndex(x => x is Rust.Ai.HTN.Murderer.Reasoners.EnemyRangeReasoner);
                    Domain.Reasoners.RemoveAt(index);
                    Domain.Reasoners.Add(new EnemyRangeReasoner(this));

                    index = Domain.Reasoners.FindIndex(x => x is Rust.Ai.HTN.Murderer.Reasoners.EnemyTargetReasoner);
                    Domain.Reasoners.RemoveAt(index);
                    Domain.Reasoners.Add(new EnemyTargetReasoner(this));

                    index = Domain.Reasoners.FindIndex(x => x is Rust.Ai.HTN.Murderer.Reasoners.EnemyPlayerLineOfSightReasoner);
                    Domain.Reasoners.RemoveAt(index);
                    Domain.Reasoners.Add(new EnemyPlayerLineOfSightReasoner(this));

                    index = Domain.Reasoners.FindIndex(x => x is Rust.Ai.HTN.Murderer.Reasoners.FireTacticReasoner);
                    Domain.Reasoners.RemoveAt(index);
                    Domain.Reasoners.Add(new FireTacticReasoner(this));

                    index = Domain.Reasoners.FindIndex(x => x is Rust.Ai.HTN.Murderer.Reasoners.PreferredFightingRangeReasoner);
                    Domain.Reasoners.RemoveAt(index);
                    Domain.Reasoners.Add(new PreferredFightingRangeReasoner(this));

                    index = Domain.Reasoners.FindIndex(x => x is Rust.Ai.HTN.Murderer.Reasoners.AtLastKnownEnemyPlayerLocationReasoner);
                    Domain.Reasoners.RemoveAt(index);
                    Domain.Reasoners.Add(new AtLastKnownEnemyPlayerLocationReasoner(this));
                }
            }

            private void InitializeNpc()
            {
                if (configData.Member.DisableDormantSystem)                
                    Rust.Ai.AiManager.Instance.HTNAgency.Remove(Entity);
                
                StripInventory(Entity); 
                Entity.Invoke(GiveLoadout, 0.1f);

                InvokeHandler.InvokeRepeating(this, ScheduleMemberUpdate, 1f, UnityEngine.Random.Range(0.1f, 0.15f));
            }

            private void GiveLoadout()
            {
                ConfigData.MemberOptions.Loadout loadout;
                if (!string.IsNullOrEmpty(Manager.hordeProfile) && configData.HordeProfiles.ContainsKey(Manager.hordeProfile))
                {
                    string loadoutId = configData.HordeProfiles[Manager.hordeProfile].GetRandom();
                    loadout = configData.Member.Loadouts.Find(x => x.LoadoutID == loadoutId);
                }
                else loadout = configData.Member.Loadouts.GetRandom();

                Entity.displayName = loadout.Names.Length > 0 ? loadout.Names.GetRandom() : "Zombie";

                Entity._aiDefinition = loadout.LoadoutDefintion;

                Entity.InitializeHealth(loadout.Vitals.Health, loadout.Vitals.Health);               

                damageMultiplier = loadout.DamageMultiplier;

                for (int i = 0; i < loadout.BeltItems.Count; i++)
                {
                    ConfigData.LootTable.InventoryItem loadoutItem = loadout.BeltItems[i];

                    Item item = ItemManager.CreateByName(loadoutItem.Shortname, loadoutItem.Amount, loadoutItem.SkinID);
                    item.MoveToContainer(Entity.inventory.containerBelt);

                    if (loadoutItem.SubSpawn != null && item.contents != null)
                    {
                        for (int y = 0; y < loadoutItem.SubSpawn.Length; y++)
                        {
                            ConfigData.LootTable.InventoryItem subspawnItem = loadoutItem.SubSpawn[y];

                            Item subItem = ItemManager.CreateByName(subspawnItem.Shortname, subspawnItem.Amount, subspawnItem.SkinID);
                            subItem.MoveToContainer(item.contents);
                        }                        
                    }
                }

                for (int i = 0; i < loadout.MainItems.Count; i++)
                {
                    ConfigData.LootTable.InventoryItem loadoutItem = loadout.MainItems[i];

                    Item item = ItemManager.CreateByName(loadoutItem.Shortname, loadoutItem.Amount, loadoutItem.SkinID);
                    item.MoveToContainer(Entity.inventory.containerMain);

                    if (loadoutItem.SubSpawn != null && item.contents != null)
                    {
                        for (int y = 0; y < loadoutItem.SubSpawn.Length; y++)
                        {
                            ConfigData.LootTable.InventoryItem subspawnItem = loadoutItem.SubSpawn[y];

                            Item subItem = ItemManager.CreateByName(subspawnItem.Shortname, subspawnItem.Amount, subspawnItem.SkinID);
                            subItem.MoveToContainer(item.contents);
                        }
                    }
                }

                for (int i = 0; i < loadout.WearItems.Count; i++)
                {
                    ConfigData.LootTable.InventoryItem loadoutItem = loadout.WearItems[i];

                    Item item = ItemManager.CreateByName(loadoutItem.Shortname, loadoutItem.Amount, loadoutItem.SkinID);
                    item.MoveToContainer(Entity.inventory.containerWear);

                    if (loadoutItem.SubSpawn != null && item.contents != null)
                    {
                        for (int y = 0; y < loadoutItem.SubSpawn.Length; y++)
                        {
                            ConfigData.LootTable.InventoryItem subspawnItem = loadoutItem.SubSpawn[y];

                            Item subItem = ItemManager.CreateByName(subspawnItem.Shortname, subspawnItem.Amount, subspawnItem.SkinID);
                            subItem.MoveToContainer(item.contents);
                        }
                    }
                }

                Entity.InvokeRandomized(LightCheck, 5f, 30f, 5f);

                if (configData.Member.GiveGlowEyes)
                    ItemManager.Create(Instance._glowEyes).MoveToContainer(Entity.inventory.containerWear);

                if (configData.Member.AimconeOverride != 0f)
                    InvokeHandler.Invoke(this, UpdateProjectileAccuracy, 3f);
            }

            private void LightCheck()
            {
                if ((TOD_Sky.Instance.Cycle.Hour > 18 || TOD_Sky.Instance.Cycle.Hour < 6) && !lightsOn)
                    LightToggle(true);
                else if ((TOD_Sky.Instance.Cycle.Hour < 18 && TOD_Sky.Instance.Cycle.Hour > 6) && lightsOn)
                    LightToggle(false);                
            }

            private void LightToggle(bool on)
            {
                Item activeItem = Entity.GetActiveItem();
                if (activeItem != null)
                {
                    BaseEntity heldEntity = activeItem.GetHeldEntity();
                    if (heldEntity != null)
                    {
                        HeldEntity component = heldEntity.GetComponent<HeldEntity>();
                        if (component)
                        {
                            component.SendMessage("SetLightsOn", on, SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }
                foreach (Item item in Entity.inventory.containerWear.itemList)
                {
                    ItemModWearable itemModWearable = item.info.GetComponent<ItemModWearable>();
                    if (!itemModWearable || !itemModWearable.emissive)                    
                        continue;
                    
                    item.SetFlag(global::Item.Flag.IsOn, on);
                    item.MarkDirty();
                }

                lightsOn = on;
            }

            private static Hash<string, float> _aimConeDefaults = new Hash<string, float>();

            private void UpdateProjectileAccuracy()
            {
                for (int i = 0; i < Entity.inventory.containerBelt.itemList.Count; i++)
                {
                    Item item = Entity.inventory.containerBelt.itemList[i];

                    BaseProjectile baseProjectile = item.GetHeldEntity() as BaseProjectile;
                    if (baseProjectile != null)
                    {
                        if (_aimConeDefaults.ContainsKey(item.info.shortname))
                            _aimConeDefaults[item.info.shortname] = baseProjectile.aimCone;

                        baseProjectile.aimCone = configData.Member.AimconeOverride;
                    }
                }
            }

            private void ScheduleMemberUpdate() => _memberTickQueue.Add(this);

            internal void OnTick()
            {          
                if (Entity.InSafeZone())
                {
                    KillInSafeZone();
                    return;
                }

                if (configData.Member.KillUnderWater && IsUnderWater())
                {
                    Manager.OnMemberDeath(this, null);
                    Entity.Kill();                    
                    return;
                }

                if (configData.Member.DisableDormantSystem)
                {
                    Entity.IsDormant = false;
                    Entity.Tick();
                }

                if (Entity.IsDormant)
                    return;

                if (Manager.PrimaryTarget == null)
                {
                    if (Memory.TargetDestination != Manager.Destination || Domain.NavAgent.isPathStale)
                    {
                        if (Manager.DebugMode)
                        {
                            if (Domain.NavAgent.isPathStale)
                                Debug.Log($"Member {Manager.members.IndexOf(this)} has stale path");
                            else Debug.Log($"Member {Manager.members.IndexOf(this)} has mismatched destination. Manager: {Manager.Destination} - Memory {Memory.TargetDestination}");
                        }

                        SetRoamToDestination();
                    }
                }

                if (Memory.PrimaryKnownEnemyPlayer.PlayerInfo.VisibilityScore > 0.5f)
                    lastSeenTargetTime = Time.time;

                BaseNpcTargetTick();
            }

            private void KillInSafeZone()
            {
                Manager.OnMemberDeath(this, null);

                NPCPlayerCorpse npcPlayerCorpse = Entity.DropCorpse("assets/prefabs/npc/murderer/murderer_corpse.prefab") as NPCPlayerCorpse;

                if (Entity.AiDomain != null && Entity.AiDomain.NavAgent != null && Entity.AiDomain.NavAgent.isOnNavMesh)                
                    npcPlayerCorpse.transform.position = npcPlayerCorpse.transform.position + (Vector3.down * Entity.AiDomain.NavAgent.baseOffset);
                
                npcPlayerCorpse.SetLootableIn(2f);
                npcPlayerCorpse.SetFlag(BaseEntity.Flags.Reserved2, true, false, true);

                int number = 0;
                while (number < Entity.inventory.containerWear.itemList.Count)
                {
                    Item item = Entity.inventory.containerWear.itemList[number];
                    if (item == null || !(item.info.shortname == "gloweyes"))                    
                        number++;                    
                    else
                    {
                        Entity.inventory.containerWear.Remove(item);
                        break;
                    }
                }

                npcPlayerCorpse.containers = new ItemContainer[3];

                for (int i = 0; i < npcPlayerCorpse.containers.Length; i++)
                {
                    npcPlayerCorpse.containers[i] = new ItemContainer();
                    npcPlayerCorpse.containers[i].ServerInitialize(null, i == 1 ? Entity.inventory.containerWear.capacity : 0);
                    npcPlayerCorpse.containers[i].GiveUID();
                    npcPlayerCorpse.containers[i].entityOwner = npcPlayerCorpse;

                    if (i == 1)
                    {
                        List<Item> list = Entity.inventory.containerWear.itemList;
                        for (int j = 0; j < list.Count; j++)
                        {
                            Item item = list[j];
                            if (!item.MoveToContainer(npcPlayerCorpse.containers[i], -1, true))
                            {
                                item.DropAndTossUpwards(Transform.position, 2f);
                            }
                        }
                    }
                }

                npcPlayerCorpse.Spawn();
                Entity.Kill();
            }

            private bool IsUnderWater()
            {
                return TerrainMeta.WaterMap.GetDepth(Transform.position) > 1.5f;
            }

            #region Attack behaviour for BaseNpc targets
            private float _lastAttackTime;
            private bool _isAttacking;

            private void BaseNpcTargetTick()
            {
                if (configData.Member.TargetAnimals && Manager.PrimaryTarget is BaseNpc)
                {
                    Domain.SetDestination(GetPreferredFightingPosition(Context));
                    TickWeapons();
                }
            }

            private void TickWeapons()
            {
                if (Context.GetFact(Facts.HasEnemyTarget) == 0 || _isAttacking || !Context.IsBodyAlive())
                {
                    Context.Body.modelState.aiming = _isAttacking;
                    return;
                }
                switch (Context.GetFact(Facts.FirearmOrder))
                {
                    case 1:
                        {
                            TickFirearm(0f);
                            return;
                        }
                    case 2:
                        {
                            TickFirearm(0.2f);
                            return;
                        }
                    case 3:
                        {
                            TickFirearm(0.5f);
                            return;
                        }
                    default:
                        {
                            if (Context.GetFact(Facts.HeldItemType) != 2)
                            {
                                return;
                            }
                            else
                            {
                                break;
                            }
                        }
                }
                Context.Body.modelState.aiming = true;
            }

            private void TickFirearm(float interval)
            {
                AttackEntity firearm = Domain.ReloadFirearmIfEmpty();
                if (firearm == null || !(firearm is BaseMelee) || Context.GetFact(Facts.HeldItemType) == 2)
                {
                    MurdererDomain.MurdererHoldItemOfType.SwitchToItem(Context, ItemType.MeleeWeapon);
                    firearm = Domain.GetFirearm();
                }
                if (firearm == null)                
                    return;
                
                BaseMelee baseMelee = firearm as BaseMelee;

                if (baseMelee == null || baseMelee.effectiveRange > 2f)                
                    Context.Body.modelState.aiming = false;                
                else Context.Body.modelState.aiming = true;
                
                float time = Time.time;

                if (time - _lastAttackTime < interval)                
                    return;
                
                if (Manager.PrimaryTarget == null)                
                    return;
                
                if (!CanUseFirearmAtRange((Manager.PrimaryTarget.transform.position - Context.BodyPosition).sqrMagnitude))                
                    return;
                
                BaseProjectile baseProjectile = firearm as BaseProjectile;
                if (baseProjectile && baseProjectile.NextAttackTime > time)                
                    return;
                
                switch (Context.GetFact(Facts.FireTactic))
                {
                    case 0:
                        {
                            FireBurst(baseProjectile, time);
                            return;
                        }
                    case 2:
                        {
                            FireFullAuto(baseProjectile, time);
                            return;
                        }
                    default:
                        {
                            FireSingle(firearm, time);
                            return;
                        }
                }
            }

            private bool CanUseFirearmAtRange(float sqrRange)
            {
                AttackEntity firearm = Domain.GetFirearm();
                if (firearm == null)                
                    return false;
                
                if (sqrRange <= Context.Body.AiDefinition.Engagement.SqrCloseRangeFirearm(firearm))               
                    return true;
                
                if (sqrRange <= Context.Body.AiDefinition.Engagement.SqrMediumRangeFirearm(firearm))                
                    return firearm.CanUseAtMediumRange;
               
                return firearm.CanUseAtLongRange;
            }

            private void FireBurst(BaseProjectile proj, float time)
            {
                if (proj == null)                
                    return;
                
                Entity.StartCoroutine(HoldTriggerLogic(proj, time, UnityEngine.Random.Range(proj.attackLengthMin, proj.attackLengthMax)));
            }

            private void FireFullAuto(BaseProjectile proj, float time)
            {
                if (proj == null)
                {
                    return;
                }
                Entity.StartCoroutine(HoldTriggerLogic(proj, time, 4f));
            }

            private void FireSingle(AttackEntity attackEnt, float time)
            {
                if (Context.EnemyPlayersInLineOfSight.Count > 3)
                {
                    attackEnt.ServerUse((1f + UnityEngine.Random.@value * 0.5f) * ConVar.AI.npc_htn_player_base_damage_modifier, null);
                }
                else if (!(Context.PrimaryEnemyPlayerInLineOfSight.Player != null) || Context.PrimaryEnemyPlayerInLineOfSight.Player.healthFraction >= 0.2f)
                {
                    attackEnt.ServerUse(ConVar.AI.npc_htn_player_base_damage_modifier, null);
                }
                else
                {
                    attackEnt.ServerUse((0.1f + UnityEngine.Random.@value * 0.5f) * ConVar.AI.npc_htn_player_base_damage_modifier, null);
                }
                _lastAttackTime = time + attackEnt.attackSpacing * (0.5f + UnityEngine.Random.@value * 0.5f);
                Context.IncrementFact(Facts.Vulnerability, 1, true, true, true);
            }

            private IEnumerator HoldTriggerLogic(BaseProjectile proj, float startTime, float triggerDownInterval)
            {
                _isAttacking = true;
                _lastAttackTime = startTime + triggerDownInterval + proj.attackSpacing;
                Context.IncrementFact(Facts.Vulnerability, 1, true, true, true);
                do
                {
                    if (Time.time - startTime >= triggerDownInterval || !Context.IsBodyAlive() || !Context.IsFact(Facts.CanSeeEnemy))
                    {
                        break;
                    }
                    if (Context.EnemyPlayersInLineOfSight.Count > 3)
                    {
                        proj.ServerUse((1f + UnityEngine.Random.@value * 0.5f) * ConVar.AI.npc_htn_player_base_damage_modifier, null);
                    }
                    else if (!(Context.PrimaryEnemyPlayerInLineOfSight.Player != null) || Context.PrimaryEnemyPlayerInLineOfSight.Player.healthFraction >= 0.2f)
                    {
                        proj.ServerUse(ConVar.AI.npc_htn_player_base_damage_modifier, null);
                    }
                    else
                    {
                        proj.ServerUse((0.1f + UnityEngine.Random.@value * 0.5f) * ConVar.AI.npc_htn_player_base_damage_modifier, null);
                    }
                    yield return CoroutineEx.waitForSeconds(proj.repeatDelay);
                }
                while (proj.primaryMagazine.contents > 0);
                _isAttacking = false;
            }

            #endregion

            internal void OnTargetUpdated(NpcPlayerInfo playerInfo)
            {
                Context.ResetState();

                if (WantsToRoam())
                    return;

                if (playerInfo.Player == null)
                {
                    Memory.ForgetPrimiaryEnemyPlayer();                    
                    return;
                }

                if (Manager.PrimaryTarget is BasePlayer)
                {
                    BasePlayer targetPlayer = Manager.PrimaryTarget as BasePlayer;
                    if (targetPlayer == null || targetPlayer.transform == null)
                    {
                        Manager.PrimaryTarget = null;
                        return;
                    }

                    Context.EnemyPlayersAudible.Add(playerInfo);
                    Context.EnemyPlayersInLineOfSight.Add(playerInfo);
                    Context.PlayersInRange.Add(playerInfo);

                    Context.PrimaryEnemyPlayerAudible = Context.PrimaryEnemyPlayerInLineOfSight = playerInfo;

                    BaseNpcMemory.EnemyPlayerInfo info = new BaseNpcMemory.EnemyPlayerInfo()
                    {
                        BodyVisibleWhenLastNoticed = true,
                        HeadVisibleWhenLastNoticed = true,
                        LastKnownLocalPosition = targetPlayer.transform.localPosition,
                        LastKnownLocalHeading = targetPlayer.transform.localPosition - Context.BodyPosition,
                        OurLastLocalPositionWhenLastSeen = Entity.transform.localPosition,
                        PlayerInfo = playerInfo,
                        Time = Time.time
                    };

                    Memory.KnownEnemyPlayers.Add(info);
                    Memory.PrimaryKnownEnemyPlayer = info;

                    if ((info.LastKnownPosition - Context.BodyPosition).sqrMagnitude > 1f)
                        Context.HasVisitedLastKnownEnemyPlayerLocation = false;
                }
                else if (Manager.PrimaryTarget is BaseNpc)
                {
                    Memory.RememberPrimaryAnimal(Manager.PrimaryTarget as BaseNpc);
                }
            }

            internal void OnTargetUpdated(AnimalInfo animalInfo)
            {
                Context.ResetState();

                if (WantsToRoam())
                    return;

                if (Manager.PrimaryTarget is BaseNpc)
                {                    
                    BaseNpc targetAnimal = Manager.PrimaryTarget as BaseNpc;
                    if (targetAnimal == null || targetAnimal.transform == null)
                    {
                        Manager.PrimaryTarget = null;
                        return;
                    }

                    Context.AnimalsInRange.Add(animalInfo);

                    Memory.PrimaryKnownAnimal = animalInfo;

                    Context.SetFact(Facts.HasEnemyTarget, true);
                }
                else if (Manager.PrimaryTarget is BasePlayer)
                {
                    Context.Memory.RememberPrimaryAnimal(Manager.PrimaryTarget as BaseNpc);
                }
            }

            private bool WantsToRoam()
            {
                if (Manager.PrimaryTarget == null)
                {
                    SetRoamToDestination();
                    return true;
                }

                return false;
            }

            internal void SetRoamToDestination()
            {                
                Memory.ForgetPrimiaryEnemyPlayer();
                
                if (Memory.TargetDestination != Manager.Destination || Memory.CachedRoamDestination != Manager.Destination || Domain.NavAgent.isPathStale || (Domain.NavAgent.isOnNavMesh && Domain.NavAgent.isStopped))
                {
                    if (Manager.DebugMode)
                    {
                        foreach (Vector3 v in Domain.NavAgent.path.corners)
                        {
                            foreach (BasePlayer player in BasePlayer.activePlayerList)
                            {
                                if (player.IsAdmin)
                                    player.SendConsoleCommand("ddraw.sphere", 10f, Color.blue, v, 0.5f);
                            }
                        }
                    }

                    if (!Domain.SetDestination(Manager.Destination, false))
                    {
                        if (Manager.DebugMode)
                            Debug.Log($"Member {Manager.members.IndexOf(this)} failed SetDestination. PathState failed");

                        Manager.PathStateFailed = true;
                        return;
                    }
                }
                
                Memory.CachedRoamDestination = Manager.Destination;
                Memory.CachedRoamDestinationTime = Time.time;
                Memory.HasTargetDestination = true;

                Context.PushFactChangeDuringPlanning(Facts.FirearmOrder, FirearmOrders.HoldYourFire, false);
                Context.SetFact(Facts.FireTactic, (byte)0, true, true, true);

                Context.SetFact(Facts.PathStatus, (byte)1, true, false, true);

                Context.SetFact(Facts.IsIdle, false, true, true, true);
                Context.SetFact(Facts.IsWaiting, false, true, true, true);

                Context.SetFact(Facts.IsNavigating, true, true, true, true);
                Context.SetFact(Facts.IsRoaming, (byte)1, true, true, true);
            }

            #region Loot
            internal void PrepareInventory()
            {
                ItemContainer[] source = new ItemContainer[] { Entity.inventory.containerMain, Entity.inventory.containerWear, Entity.inventory.containerBelt };

                containers = new ItemContainer[3];

                for (int i = 0; i < containers.Length; i++)
                {
                    containers[i] = new ItemContainer();
                    containers[i].ServerInitialize(null, source[i].capacity);
                    containers[i].GiveUID();
                    Item[] array = source[i].itemList.ToArray();
                    for (int j = 0; j < array.Length; j++)
                    {
                        Item item = array[j];
                        if (i == 1)
                        {
                            Item newItem = ItemManager.CreateByItemID(item.info.itemid, item.amount, item.skin);
                            if (!newItem.MoveToContainer(containers[i], -1, true))
                                newItem.Remove(0f);
                        }
                        else
                        {
                            if (i == 2 && configData.Member.AimconeOverride != 0f)
                            {
                                float aimCone;
                                if (_aimConeDefaults.TryGetValue(item.info.shortname, out aimCone))
                                {
                                    BaseProjectile baseProjectile = item.GetHeldEntity() as BaseProjectile;
                                    if (baseProjectile != null)
                                        baseProjectile.aimCone = aimCone;
                                }
                            }

                            if (!item.MoveToContainer(containers[i], -1, true))
                                item.Remove(0f);
                        }
                    }
                }
            }

            internal void MoveInventoryTo(LootableCorpse corpse)
            {
                for (int i = 0; i < containers.Length; i++)
                {
                    Item[] array = containers[i].itemList.ToArray();
                    corpse.containers[i].capacity = array.Length;

                    for (int j = 0; j < array.Length; j++)
                    {
                        Item item = array[j];

                        if (item == null)
                            continue;

                        if (item.info.shortname == Instance._glowEyes.shortname)
                        {
                            item.RemoveFromContainer();
                            item.Remove(0f);
                            continue;
                        }

                        if (!item.MoveToContainer(corpse.containers[i], -1, true))
                        {
                            item.Remove(0f);
                        }
                    }
                }

                corpse.ResetRemovalTime();
            }
            #endregion
        }
        #endregion

        #region Sensors and Reasoners
        public class PlayersInRangeSensor : INpcSensor
        {
            internal HordeMember HordeMember { get; private set; }

            public float TickFrequency { get; set; }

            public float LastTickTime { get; set; }


            public const int MaxPlayers = 128;

            public static BasePlayer[] PlayerQueryResults = new BasePlayer[128];

            public static int PlayerQueryResultCount = 0;


            public PlayersInRangeSensor(HordeMember hordeMember)
            {
                HordeMember = hordeMember;
                TickFrequency = 0.5f;
            }

            private Func<BasePlayer, bool> Query = (BasePlayer player) => player != null && player.isServer && !player.IsDestroyed && player.transform != null && !player.IsDead() && !player.IsWounded() && (!player.IsSleeping() || player.secondsSleeping >= NPCAutoTurret.sleeperhostiledelay);

            public void Tick(IHTNAgent htnAgent, float deltaTime, float time)
            {
                if (HordeMember == null || htnAgent == null || htnAgent.transform == null || htnAgent.IsDestroyed || htnAgent.AiDefinition == null)
                    return;

                PlayerQueryResultCount = BaseEntity.Query.Server.GetPlayersInSphere(htnAgent.transform.position, htnAgent.AiDefinition.Sensory.VisionRange, PlayerQueryResults, Query);

                List<NpcPlayerInfo> playersInRange = htnAgent.AiDomain.NpcContext.PlayersInRange;

                if (PlayerQueryResultCount > 0)
                {
                    for (int i = 0; i < PlayerQueryResultCount; i++)
                    {
                        BasePlayer potentialTarget = PlayerQueryResults[i];

                        if (potentialTarget != null && !potentialTarget.IsDead() && potentialTarget.transform != null && potentialTarget != HordeMember.Entity && (potentialTarget.transform.position - htnAgent.transform.position).sqrMagnitude <= htnAgent.AiDefinition.Sensory.SqrVisionRange)
                        {
                            bool flag = false;
                            for (int j = 0; j < playersInRange.Count; j++)
                            {
                                NpcPlayerInfo npcPlayerInfo = playersInRange[j];
                                if (npcPlayerInfo.Player == potentialTarget)
                                {
                                    npcPlayerInfo.Time = time;
                                    playersInRange[j] = npcPlayerInfo;
                                    flag = true;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                if (HordeMember.Manager.IsValidTarget(potentialTarget))
                                {
                                    playersInRange.Add(new NpcPlayerInfo
                                    {
                                        Player = potentialTarget,
                                        Time = time
                                    });
                                }
                            }
                        }
                    }
                }
                for (int k = 0; k < playersInRange.Count; k++)
                {
                    NpcPlayerInfo npcPlayerInfo = playersInRange[k];

                    if ((time - npcPlayerInfo.Time > htnAgent.AiDefinition.Memory.ForgetInRangeTime && htnAgent.AiDomain.NpcContext.BaseMemory.ShouldRemoveOnPlayerForgetTimeout(time, npcPlayerInfo)) || (npcPlayerInfo.Player?.IsDead() ?? false))
                    {
                        playersInRange.RemoveAt(k);
                        k--;
                    }
                    else HordeMember.Manager.EvaluateTarget(npcPlayerInfo);
                }
            }
        }

        public class AnimalsInRangeSensor : INpcSensor
        {
            internal HordeMember HordeMember { get; private set; }
            
            public float LastTickTime { get; set; }

            public float TickFrequency { get; set; }


            public const int MaxAnimals = 128;

            public static BaseNpc[] QueryResults = new BaseNpc[128];

            public static int QueryResultCount = 0;

            public AnimalsInRangeSensor(HordeMember hordeMember)
            {
                HordeMember = hordeMember;
                TickFrequency = 1f;
            }

            public void Tick(IHTNAgent htnAgent, float deltaTime, float time)
            {
                if (HordeMember == null || htnAgent == null || htnAgent.transform == null || htnAgent.IsDestroyed || htnAgent.AiDefinition == null)
                    return;

                MurdererDomain aiDomain = HordeMember.Domain;
                if (aiDomain == null || HordeMember.Context == null)                
                    return;
                
                QueryResultCount = BaseEntity.Query.Server.GetInSphere(htnAgent.transform.position, htnAgent.AiDefinition.Sensory.VisionRange / 2f, QueryResults, (BaseEntity entity) => 
                {
                    BaseNpc baseNpc = entity as BaseNpc;
                    if (baseNpc != null && !baseNpc.IsDestroyed && !baseNpc.IsDead())                    
                        return true;                    
                    return false;
                });

                List<AnimalInfo> animalsInRange = htnAgent.AiDomain.NpcContext.AnimalsInRange;
                if (QueryResultCount > 0)
                {
                    for (int i = 0; i < QueryResultCount; i++)
                    {
                        BaseNpc potentialTarget = QueryResults[i];
                        if (potentialTarget != null && !potentialTarget.IsDead() && potentialTarget.transform != null && potentialTarget != HordeMember.Entity)
                        {
                            float sqrDistance = (potentialTarget.transform.position - htnAgent.transform.position).sqrMagnitude;
                            if (sqrDistance <= htnAgent.AiDefinition.Sensory.SqrVisionRange)
                            {
                                bool flag = false;
                                int num = 0;
                                while (num < animalsInRange.Count)
                                {
                                    AnimalInfo info = animalsInRange[num];
                                    if (info.Animal != potentialTarget)
                                    {
                                        num++;
                                    }
                                    else
                                    {
                                        info.Time = time;
                                        info.SqrDistance = sqrDistance;
                                        animalsInRange[num] = info;
                                        flag = true;
                                        break;
                                    }
                                }
                                if (!flag)
                                {
                                    AnimalInfo animalInfo = new AnimalInfo()
                                    {
                                        Animal = potentialTarget,
                                        Time = time,
                                        SqrDistance = sqrDistance
                                    };
                                    animalsInRange.Add(animalInfo);
                                }
                            }
                        }
                    }
                }
                for (int j = 0; j < animalsInRange.Count; j++)
                {
                    AnimalInfo animalInfo = animalsInRange[j];

                    if ((time - animalInfo.Time > htnAgent.AiDefinition.Memory.ForgetAnimalInRangeTime) || (animalInfo.Animal?.IsDead() ?? false))
                    {
                        animalsInRange.RemoveAt(j);
                        j--;
                    }
                    else HordeMember.Manager.EvaluateTarget(animalInfo);
                }
            }
        }

        public class EnemyRangeReasoner : INpcReasoner
        {
            internal HordeMember HordeMember { get; private set; }

            public float LastTickTime { get; set; }

            public float TickFrequency { get; set; }

            public EnemyRangeReasoner(HordeMember hordeMember)
            {
                HordeMember = hordeMember;
                TickFrequency = 0.2f;
            }

            public void Tick(IHTNAgent htnAgent, float deltaTime, float time)
            {
                if (HordeMember == null || htnAgent == null || htnAgent.transform == null || htnAgent.IsDestroyed || htnAgent.AiDefinition == null)
                    return;

                MurdererContext npcContext = HordeMember.Context;
                if (npcContext == null)                
                    return;

                if (HordeMember.Manager.PrimaryTarget == null)
                {
                    npcContext.SetFact(Facts.EnemyRange, EnemyRange.OutOfRange, true, true, true);
                    return;
                }
               
                float sqrDistance = (HordeMember.Manager.Destination - npcContext.BodyPosition).sqrMagnitude;

                AttackEntity firearm = npcContext.Domain.GetFirearm();

                if (sqrDistance <= npcContext.Body.AiDefinition.Engagement.SqrCloseRangeFirearm(firearm))
                {
                    npcContext.SetFact(Facts.EnemyRange, EnemyRange.CloseRange, true, true, true);
                    return;
                }

                if (sqrDistance <= npcContext.Body.AiDefinition.Engagement.SqrMediumRangeFirearm(firearm))
                {
                    npcContext.SetFact(Facts.EnemyRange, EnemyRange.MediumRange, true, true, true);
                    return;
                }

                if (sqrDistance <= npcContext.Body.AiDefinition.Engagement.SqrLongRangeFirearm(firearm))
                {
                    npcContext.SetFact(Facts.EnemyRange, EnemyRange.LongRange, true, true, true);
                    return;
                }

                npcContext.SetFact(Facts.EnemyRange, EnemyRange.OutOfRange, true, true, true);
            }
        }

        public class EnemyTargetReasoner : INpcReasoner
        {
            internal HordeMember HordeMember { get; private set; }

            public float LastTickTime { get; set; }

            public float TickFrequency { get; set; }

            public EnemyTargetReasoner(HordeMember hordeMember)
            {
                HordeMember = hordeMember;
                TickFrequency = 0.2f;
            }

            public void Tick(IHTNAgent htnAgent, float deltaTime, float time)
            {
                if (HordeMember == null || htnAgent == null || htnAgent.transform == null || htnAgent.IsDestroyed)
                    return;

                MurdererContext npcContext = HordeMember.Context;
                if (npcContext == null)                
                    return;
                
                npcContext.SetFact(Facts.HasEnemyTarget, HordeMember.Manager.PrimaryTarget != null, true, true, true);
            }
        }

        public class FireTacticReasoner : INpcReasoner
        {
            internal HordeMember HordeMember { get; private set; }

            public float LastTickTime { get; set; }

            public float TickFrequency { get; set; }

            public FireTacticReasoner(HordeMember hordeMember)
            {
                HordeMember = hordeMember;
                TickFrequency = 0.2f;
            }

            public void Tick(IHTNAgent htnAgent, float deltaTime, float time)
            {
                if (HordeMember == null || htnAgent == null || htnAgent.transform == null || htnAgent.IsDestroyed)
                    return;

                MurdererContext npcContext = HordeMember.Context;
                if (npcContext == null)                
                    return;
                
                FireTactic fireTactic = FireTactic.Single;
                AttackEntity heldEntity = HordeMember.Entity.GetHeldEntity() as AttackEntity;
                if (heldEntity)
                {
                    BaseProjectile baseProjectile = heldEntity as BaseProjectile;
                    float sqrDistance = float.MaxValue;

                    if (HordeMember.Manager.PrimaryTarget != null)                    
                        sqrDistance = (HordeMember.Manager.Destination - npcContext.BodyPosition).sqrMagnitude;
                    
                    if (heldEntity.attackLengthMin < 0f || sqrDistance > npcContext.Body.AiDefinition.Engagement.SqrCloseRangeFirearm(baseProjectile))                    
                        fireTactic = (heldEntity.attackLengthMin < 0f || sqrDistance > npcContext.Body.AiDefinition.Engagement.SqrMediumRangeFirearm(baseProjectile) ? FireTactic.Single : FireTactic.Burst);
                    
                    else fireTactic = FireTactic.FullAuto;
                    
                }
                npcContext.SetFact(Facts.FireTactic, fireTactic, true, true, true);
            }
        }

        public class PreferredFightingRangeReasoner : INpcReasoner
        {
            internal HordeMember HordeMember { get; private set; }

            public float LastTickTime { get; set; }

            public float TickFrequency { get; set; }

            public PreferredFightingRangeReasoner(HordeMember hordeMember)
            {
                HordeMember = hordeMember;
                TickFrequency = 0.2f;
            }

            public static bool IsAtPreferredRange(MurdererContext context, float sqrDistance, AttackEntity firearm)
            {
                if (firearm == null)                
                    return false;
                
                switch (firearm.effectiveRangeType)
                {
                    case NPCPlayerApex.WeaponTypeEnum.CloseRange:
                        {
                            return sqrDistance <= context.Body.AiDefinition.Engagement.SqrCloseRangeFirearm(firearm);
                        }
                    case NPCPlayerApex.WeaponTypeEnum.MediumRange:
                        {
                            if (sqrDistance > context.Body.AiDefinition.Engagement.SqrMediumRangeFirearm(firearm))
                            {
                                return false;
                            }
                            return sqrDistance > context.Body.AiDefinition.Engagement.SqrCloseRangeFirearm(firearm);
                        }
                    case NPCPlayerApex.WeaponTypeEnum.LongRange:
                        {
                            if (sqrDistance >= context.Body.AiDefinition.Engagement.SqrLongRangeFirearm(firearm))
                            {
                                return false;
                            }
                            return sqrDistance > context.Body.AiDefinition.Engagement.SqrMediumRangeFirearm(firearm);
                        }
                }
                return false;
            }

            public void Tick(IHTNAgent htnAgent, float deltaTime, float time)
            {
                if (HordeMember == null || htnAgent == null || htnAgent.transform == null || htnAgent.IsDestroyed)
                    return;

                MurdererContext npcContext = HordeMember.Context;
                if (npcContext == null)                
                    return;
                
                if (HordeMember.Manager.PrimaryTarget != null)
                {
                    float sqrDistance = (HordeMember.Manager.Destination - npcContext.BodyPosition).sqrMagnitude;

                    if (IsAtPreferredRange(npcContext, sqrDistance, npcContext.Domain.GetFirearm()))
                    {
                        npcContext.SetFact(Facts.AtLocationPreferredFightingRange, 1, true, true, true);
                        return;
                    }
                    npcContext.SetFact(Facts.AtLocationPreferredFightingRange, 0, true, true, true);
                }
            }
        }

        public class AtLastKnownEnemyPlayerLocationReasoner : INpcReasoner
        {
            internal HordeMember HordeMember { get; private set; }

            public float LastTickTime { get; set; }

            public float TickFrequency { get; set; }


            private NavMeshHit navMeshHit;

            public AtLastKnownEnemyPlayerLocationReasoner(HordeMember hordeMember)
            {
                HordeMember = hordeMember;
                TickFrequency = 0.2f;
            }

            public void Tick(IHTNAgent htnAgent, float deltaTime, float time)
            {
                if (HordeMember == null || htnAgent == null || htnAgent.transform == null || htnAgent.IsDestroyed)
                    return;

                MurdererContext npcContext = HordeMember.Context;
                if (npcContext == null)                
                    return;
                
                if (HordeMember.Manager.PrimaryTarget != null && (GetDestination() - HordeMember.Context.BodyPosition).sqrMagnitude < 1f)                
                    npcContext.SetFact(Facts.AtLocationLastKnownLocationOfPrimaryEnemyPlayer, 1, true, true, true);
                else npcContext.SetFact(Facts.AtLocationLastKnownLocationOfPrimaryEnemyPlayer, 0, true, true, true);
            }

            private Vector3 GetDestination()
            {
                if (HordeMember.Manager.PrimaryTarget != null && !HordeMember.Context.HasVisitedLastKnownEnemyPlayerLocation && NavMesh.FindClosestEdge(HordeMember.Manager.Destination, out navMeshHit, HordeMember.Domain.NavAgent.areaMask))
                    return navMeshHit.position;
                
                return HordeMember.Context.Body.transform.position;
            }
        }

        public class EnemyPlayerLineOfSightReasoner : INpcReasoner
        {
            internal HordeMember HordeMember { get; private set; }

            public float LastTickTime { get; set; }

            public float TickFrequency { get; set; }

            public EnemyPlayerLineOfSightReasoner(HordeMember hordeMember)
            {
                HordeMember = hordeMember;
                TickFrequency = 0.2f;
            }

            public void Tick(IHTNAgent htnAgent, float deltaTime, float time)
            {
                if (HordeMember == null || htnAgent == null || htnAgent.transform == null || htnAgent.IsDestroyed)
                    return;

                MurdererContext npcContext = HordeMember.Context;
                if (npcContext == null)                
                    return;
                
                npcContext.SetFact(Facts.CanSeeEnemy, npcContext.EnemyPlayersInLineOfSight.Count > 0 || HordeMember.Manager.PrimaryTarget is BaseNpc, true, true, true);
                
                float distance = 0f;
                NpcPlayerInfo npcPlayerInfo = default(NpcPlayerInfo);

                foreach (NpcPlayerInfo enemyPlayersInLineOfSight in htnAgent.AiDomain.NpcContext.EnemyPlayersInLineOfSight)
                {
                    float forwardDotDir = (enemyPlayersInLineOfSight.ForwardDotDir + 1f) * 0.5f;
                    float sqrDistance = (1f - enemyPlayersInLineOfSight.SqrDistance / htnAgent.AiDefinition.Engagement.SqrAggroRange) * 2f + forwardDotDir;
                    if (sqrDistance > distance)
                    {
                        distance = sqrDistance;
                        npcPlayerInfo = enemyPlayersInLineOfSight;
                    }

                    NpcPlayerInfo npcPlayerInfo1 = enemyPlayersInLineOfSight;
                    npcPlayerInfo1.VisibilityScore = sqrDistance;
                    npcContext.Memory.RememberEnemyPlayer(htnAgent, ref npcPlayerInfo1, time, 0f, "SEE!");
                }
                npcContext.PrimaryEnemyPlayerInLineOfSight = npcPlayerInfo;
                if (npcPlayerInfo.Player != null && (npcContext.Memory.PrimaryKnownEnemyPlayer.PlayerInfo.Player == null || npcContext.Memory.PrimaryKnownEnemyPlayer.PlayerInfo.AudibleScore < distance))
                {
                    npcContext.Memory.RememberPrimaryEnemyPlayer(npcPlayerInfo.Player);
                    npcContext.IncrementFact(Facts.Alertness, 2, true, true, true);
                }
            }
        }

        public static Vector3 GetPreferredFightingPosition(MurdererContext context)
        {            
            if (Time.time - context.Memory.CachedPreferredDistanceDestinationTime < 0.01f)            
                return context.Memory.CachedPreferredDistanceDestination;

            Vector3 vector3;
            Vector3 body;
            NavMeshHit navMeshHit;

            AnimalInfo primaryAnimalTarget = context.Memory.PrimaryKnownAnimal;
            if (primaryAnimalTarget.Animal != null)
            {
                float single = 1.5f;
                AttackEntity firearm = context.Domain.GetFirearm();
                if (firearm != null)
                {
                    single = (firearm.effectiveRangeType != NPCPlayerApex.WeaponTypeEnum.CloseRange ? context.Body.AiDefinition.Engagement.CenterOfMediumRangeFirearm(firearm) : context.Body.AiDefinition.Engagement.CenterOfCloseRangeFirearm(firearm));
                }
                float single1 = single * single;
                if (primaryAnimalTarget.Animal.NavAgent.velocity.Magnitude2D() <= 5f)
                {
                    single -= 0.1f;
                    if (primaryAnimalTarget.SqrDistance > single1)
                    {
                        body = context.Body.transform.position - primaryAnimalTarget.Animal.transform.position;
                        vector3 = body.normalized;
                    }
                    else
                    {
                        body = primaryAnimalTarget.Animal.transform.position - context.Body.transform.position;
                        vector3 = body.normalized;
                    }
                }
                else
                {
                    single += 1.5f;
                    if (primaryAnimalTarget.SqrDistance > single1)
                    {
                        body = primaryAnimalTarget.Animal.transform.position - context.Body.transform.position;
                        vector3 = body.normalized;
                    }
                    else
                    {
                        body = context.Body.transform.position - primaryAnimalTarget.Animal.transform.position;
                        vector3 = body.normalized;
                    }
                    if (Vector3.Dot(primaryAnimalTarget.Animal.NavAgent.velocity, vector3) < 0f)
                    {
                        if (primaryAnimalTarget.SqrDistance > single1)
                        {
                            body = context.Body.transform.position - primaryAnimalTarget.Animal.transform.position;
                            vector3 = body.normalized;
                        }
                        else
                        {
                            body = primaryAnimalTarget.Animal.transform.position - context.Body.transform.position;
                            vector3 = body.normalized;
                        }
                    }
                }
                Vector3 player = primaryAnimalTarget.Animal.transform.position + (vector3 * single);
                if (!NavMesh.SamplePosition(player + (Vector3.up * 0.1f), out navMeshHit, 2f * context.Domain.NavAgent.height, -1))
                {
                    context.Memory.AddFailedDestination(player);
                }
                else
                {
                    Vector3 allowedMovementDestination = context.Domain.ToAllowedMovementDestination(navMeshHit.position);
                    if (context.Memory.IsValid(allowedMovementDestination))
                    {
                        context.Memory.CachedPreferredDistanceDestination = allowedMovementDestination;
                        context.Memory.CachedPreferredDistanceDestinationTime = Time.time;
                        return allowedMovementDestination;
                    }
                }
            }
            return context.Body.transform.position;
        }
        #endregion

        #region Commands        
        [ChatCommand("horde")]
        private void cmdHorde(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "zombiehorde.admin"))
            {
                SendReply(player, "You do not have permission to use this command");
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, "/horde info - Show position and information about active zombie hordes");
                SendReply(player, "/horde tpto <number> - Teleport to the specified zombie horde");
                SendReply(player, "/horde destroy <number> - Destroy the specified zombie horde");
                SendReply(player, "/horde create <opt:distance> <opt:profile> - Create a new zombie horde on your position, optionally specifying distance they can roam and the horde profile you want to use");
                SendReply(player, "/horde createloadout - Copy your current inventory to a new zombie loadout");
                SendReply(player, "/horde hordecount <number> - Set the maximum number of hordes allowed");
                SendReply(player, "/horde membercount <number> - Set the maximum number of members allowed per horde");
                return;
            }

            switch (args[0].ToLower())
            {                
                case "info":
                    int memberCount = 0;
                    int hordeNumber = 0;
                    foreach (HordeManager hordeManager in HordeManager._allHordes)
                    {
                        player.SendConsoleCommand("ddraw.text", 30, Color.green, hordeManager.AverageLocation + new Vector3(0, 1.5f, 0), $"<size=20>Zombie Horde {hordeNumber}</size>");
                        memberCount += hordeManager.members.Count;
                        hordeNumber++;
                    }

                    SendReply(player, $"There are {HordeManager._allHordes.Count} active zombie hordes with a total of {memberCount} zombies");
                    return;
                case "destroy":
                    {
                        int number;
                        if (args.Length != 2 || !int.TryParse(args[1], out number))
                        {
                            SendReply(player, "You must specify a horde number");
                            return;
                        }

                        if (number < 0 || number >= HordeManager._allHordes.Count)
                        {
                            SendReply(player, "An invalid horde number has been specified");
                            return;
                        }

                        HordeManager._allHordes[number].Destroy(true, true);
                        SendReply(player, $"You have destroyed zombie horde {number}");
                        return;
                    }
                case "tpto":
                    {
                        int number;
                        if (args.Length != 2 || !int.TryParse(args[1], out number))
                        {
                            SendReply(player, "You must specify a horde number");
                            return;
                        }

                        if (number < 0 || number >= HordeManager._allHordes.Count)
                        {
                            SendReply(player, "An invalid horde number has been specified");
                            return;
                        }

                        player.Teleport(HordeManager._allHordes[number].AverageLocation);
                        SendReply(player, $"You have teleported to zombie horde {number}");
                        return;
                    }
                case "debug":
                    {
                        int number;
                        if (args.Length != 2 || !int.TryParse(args[1], out number))
                        {
                            SendReply(player, "You must specify a horde number");
                            return;
                        }

                        if (number < 0 || number >= HordeManager._allHordes.Count)
                        {
                            SendReply(player, "An invalid horde number has been specified");
                            return;
                        }


                        HordeManager._allHordes[number].DebugMode = !HordeManager._allHordes[number].DebugMode;
                        SendReply(player, $"Debug mode horde {number} : {HordeManager._allHordes[number].DebugMode}");
                        return;
                    }
                case "create":
                    float distance = -1;
                    if (args.Length >= 2)
                    {
                        if (!float.TryParse(args[1], out distance))
                        {
                            SendReply(player, "Invalid Syntax!");
                            return;
                        }
                    }

                    string profile = string.Empty;
                    if (args.Length >= 3 && configData.HordeProfiles.ContainsKey(args[2]))
                        profile = args[2];

                    object success = FindPointOnNavmesh(player.transform.position, 5f);
                    if (success != null)
                    {
                        if (HordeManager.Create(new HordeManager.Order((Vector3)success, configData.Horde.InitialMemberCount, configData.Horde.MaximumMemberCount, distance, profile)))
                        {
                            if (distance > 0)
                                SendReply(player, $"You have created a zombie horde with a roam distance of {distance}");
                            else SendReply(player, "You have created a zombie horde");

                            return;
                        }
                    }

                    SendReply(player, "Invalid spawn position, move to another more open position. Unable to spawn horde");
                    return;

                case "createloadout":
                    ConfigData.MemberOptions.Loadout loadout = new ConfigData.MemberOptions.Loadout($"loadout-{configData.Member.Loadouts.Count}", DefaultDefinition);
                    
                    for (int i = 0; i < player.inventory.containerBelt.itemList.Count; i++)
                    {
                        Item item = player.inventory.containerBelt.itemList[i];
                        if (item == null || item.amount == 0)
                            continue;

                        loadout.BeltItems.Add(new ConfigData.LootTable.InventoryItem()
                        {
                            Amount = item.amount,
                            Shortname = item.info.shortname,
                            SkinID = item.skin
                        });
                    }

                    for (int i = 0; i < player.inventory.containerMain.itemList.Count; i++)
                    {
                        Item item = player.inventory.containerMain.itemList[i];
                        if (item == null || item.amount == 0)
                            continue;

                        loadout.MainItems.Add(new ConfigData.LootTable.InventoryItem()
                        {
                            Amount = item.amount,
                            Shortname = item.info.shortname,
                            SkinID = item.skin
                        });
                    }

                    for (int i = 0; i < player.inventory.containerWear.itemList.Count; i++)
                    {
                        Item item = player.inventory.containerWear.itemList[i];
                        if (item == null || item.amount == 0)
                            continue;

                        loadout.WearItems.Add(new ConfigData.LootTable.InventoryItem()
                        {
                            Amount = item.amount,
                            Shortname = item.info.shortname,
                            SkinID = item.skin
                        });
                    }

                    configData.Member.Loadouts.Add(loadout);
                    SaveConfig();

                    SendReply(player, "Saved your current inventory as a zombie loadout");
                    return;

                case "hordecount":
                    int hordes;
                    if (args.Length < 2 || !int.TryParse(args[1], out hordes))
                    {
                        SendReply(player, "You must enter a number");
                        return;
                    }

                    configData.Horde.MaximumHordes = hordes;

                    if (HordeManager._allHordes.Count < hordes)
                        CreateRandomHordes();
                    SaveConfig();
                    SendReply(player, $"Set maximum hordes to {hordes}");
                    return;

                case "membercount":
                    int members;
                    if (args.Length < 2 || !int.TryParse(args[1], out members))
                    {
                        SendReply(player, "You must enter a number");
                        return;
                    }

                    configData.Horde.MaximumMemberCount = members;
                    SaveConfig();
                    SendReply(player, $"Set maximum horde members to {members}");
                    return;
                default:
                    SendReply(player, "Invalid Syntax!");
                    break;
            }
        }

        [ConsoleCommand("horde")]
        private void ccmdHorde(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                if (!permission.UserHasPermission(arg.Connection.userid.ToString(), "zombiehorde.admin"))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "horde info - Show position and information about active zombie hordes");
                SendReply(arg, "horde destroy <number> - Destroy the specified zombie horde");
                SendReply(arg, "horde create <opt:distance> - Create a new zombie horde at a random position, optionally specifying distance they can roam from the initial spawn point");
                SendReply(arg, "horde addloadout <kitname> <opt:otherkitname> <opt:otherkitname> - Convert the specified kit(s) into loadout(s) (add as many as you want)");
                SendReply(arg, "horde hordecount <number> - Set the maximum number of hordes allowed");
                SendReply(arg, "horde membercount <number> - Set the maximum number of members allowed per horde");
                return;
            }

            switch (arg.Args[0].ToLower())
            {
                case "info":
                    int memberCount = 0;
                    int hordeNumber = 0;
                    foreach (HordeManager hordeManager in HordeManager._allHordes)
                    {
                        memberCount += hordeManager.members.Count;
                        hordeNumber++;
                    }

                    SendReply(arg, $"There are {HordeManager._allHordes.Count} active zombie hordes with a total of {memberCount} zombies");
                    return;
                case "destroy":
                    int number;
                    if (arg.Args.Length != 2 || !int.TryParse(arg.Args[1], out number))
                    {
                        SendReply(arg, "You must specify a horde number");
                        return;
                    }

                    if (number < 1 || number > HordeManager._allHordes.Count)
                    {
                        SendReply(arg, "An invalid horde number has been specified");
                        return;
                    }

                    HordeManager._allHordes[number - 1].Destroy(true, true);
                    SendReply(arg, $"You have destroyed zombie horde {number}");
                    return;                
                case "create":
                    float distance = -1;
                    if (arg.Args.Length >= 2)
                    {
                        if (!float.TryParse(arg.Args[1], out distance))
                        {
                            SendReply(arg, "Invalid Syntax!");
                            return;
                        }
                    }

                    string profile = string.Empty;
                    if (arg.Args.Length >= 3 && configData.HordeProfiles.ContainsKey(arg.Args[2]))
                        profile = arg.Args[2];

                    if (HordeManager.Create(new HordeManager.Order(GetSpawnPoint(), configData.Horde.InitialMemberCount, configData.Horde.MaximumMemberCount, distance, profile)))
                    {
                        if (distance > 0)
                            SendReply(arg, $"You have created a zombie horde with a roam distance of {distance}");
                        else SendReply(arg, "You have created a zombie horde");
                    }
                    else SendReply(arg, "Invalid spawn position. Unable to spawn horde. Try again for a new random position");

                    return;
                case "addloadout":
                    if (!Kits)
                    {
                        SendReply(arg, "Unable to find the kits plugin");
                        return;
                    }

                    if (arg.Args.Length < 2)
                    {
                        SendReply(arg, "horde addloadout <kitname> <opt:otherkitname> <opt:otherkitname> - Convert the specified kit(s) into loadout(s) (add as many as you want)");
                        return;
                    }

                    for (int i = 1; i < arg.Args.Length; i++)
                    {
                        string kitname = arg.Args[i];
                        object success = Kits.Call("GetKitInfo", kitname);
                        if (success == null)
                        {
                            SendReply(arg, $"Unable to find a kit with the name {kitname}");
                            continue;
                        }

                        JObject obj = success as JObject;
                        JArray items = obj["items"] as JArray;

                        ConfigData.MemberOptions.Loadout loadout = new ConfigData.MemberOptions.Loadout(kitname, DefaultDefinition);

                        for (int y = 0; y < items.Count; y++)
                        {
                            JObject item = items[y] as JObject;
                            string container = (string)item["container"];

                            List<ConfigData.LootTable.InventoryItem> list = container == "belt" ? loadout.BeltItems : container == "main" ? loadout.MainItems : loadout.WearItems;
                            list.Add(new ConfigData.LootTable.InventoryItem
                            {
                                Amount = (int)item["amount"],
                                Shortname = ItemManager.FindItemDefinition((int)item["itemid"])?.shortname,
                                SkinID = (ulong)item["skinid"]
                            });
                        }

                        configData.Member.Loadouts.Add(loadout);

                        SendReply(arg, $"Successfully converted the kit {kitname} to a zombie loadout");
                    }
                    
                    SaveConfig();                    
                    return;

                case "hordecount":
                    int hordes;
                    if (arg.Args.Length < 2 || !int.TryParse(arg.Args[1], out hordes))
                    {
                        SendReply(arg, "You must enter a number");
                        return;
                    }

                    configData.Horde.MaximumHordes = hordes;

                    if (HordeManager._allHordes.Count < hordes)
                        CreateRandomHordes();
                    SaveConfig();
                    SendReply(arg, $"Set maximum hordes to {hordes}");
                    return;

                case "membercount":
                    int members;
                    if (arg.Args.Length < 2 || !int.TryParse(arg.Args[1], out members))
                    {
                        SendReply(arg, "You must enter a number");
                        return;
                    }

                    configData.Horde.MaximumMemberCount = members;
                    SaveConfig();
                    SendReply(arg, $"Set maximum horde members to {members}");
                    return;
                default:
                    SendReply(arg, "Invalid Syntax!");
                    break;
            }
        }

        private float nextCountTime;
        private string cachedString = string.Empty;

        private string GetInfoString()
        {
            if (nextCountTime < Time.time || string.IsNullOrEmpty(cachedString))
            {
                int memberCount = 0;
                HordeManager._allHordes.ForEach(x => memberCount += x.members.Count);
                cachedString = $"There are currently <color=#ce422b>{HordeManager._allHordes.Count}</color> hordes with a total of <color=#ce422b>{memberCount}</color> zombies";
                nextCountTime = Time.time + 30f;
            }

            return cachedString;
        }

        [ChatCommand("hordeinfo")]
        private void cmdHordeInfo(BasePlayer player, string command, string[] args) => player.ChatMessage(GetInfoString());
        
        [ConsoleCommand("hordeinfo")]
        private void ccmdHordeInfo(ConsoleSystem.Arg arg)
        {            
            if (arg.Connection == null)
                PrintToChat(GetInfoString());
        }

        #endregion

        #region Config       
        public enum SpawnSystem { None, Random, SpawnsDatabase }

        public enum SpawnState { Spawn, Despawn }

        internal static ConfigData configData;

        internal class ConfigData
        {
            [JsonProperty(PropertyName = "Horde Options")]
            public HordeOptions Horde { get; set; }

            [JsonProperty(PropertyName = "Horde Member Options")]
            public MemberOptions Member { get; set; }

            [JsonProperty(PropertyName = "Loot Table")]
            public LootTable Loot { get; set; }

            [JsonProperty(PropertyName = "Monument Spawn Options")]
            public MonumentSpawn Monument { get; set; }

            [JsonProperty(PropertyName = "Timed Spawn Options")]
            public TimedSpawnOptions TimedSpawns { get; set; }

            [JsonProperty(PropertyName = "Horde Profiles (profile name, list of applicable loadouts)")]
            public Dictionary<string, List<string>> HordeProfiles { get; set; }

            public class TimedSpawnOptions
            {
                [JsonProperty(PropertyName = "Only allows spawns during the set time period")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Despawn hordes outside of the set time period")]
                public bool Despawn { get; set; }

                [JsonProperty(PropertyName = "Start time (0.0 - 24.0)")]
                public float Start { get; set; }

                [JsonProperty(PropertyName = "End time (0.0 - 24.0)")]
                public float End { get; set; }
            }

            public class HordeOptions
            {
                [JsonProperty(PropertyName = "Amount of zombies to spawn when a new horde is created")]
                public int InitialMemberCount { get; set; }

                [JsonProperty(PropertyName = "Maximum amount of spawned zombies per horde")]
                public int MaximumMemberCount { get; set; }

                [JsonProperty(PropertyName = "Maximum amount of hordes at any given time")]
                public int MaximumHordes { get; set; }

                [JsonProperty(PropertyName = "Amount of time from when a horde is destroyed until a new horde is created (seconds)")]
                public int RespawnTime { get; set; }

                [JsonProperty(PropertyName = "Amount of time before a horde grows in size")]
                public int GrowthRate { get; set; }

                [JsonProperty(PropertyName = "Add a zombie to the horde when a horde member kills a player")]
                public bool CreateOnDeath { get; set; }

                [JsonProperty(PropertyName = "Merge hordes together if they collide")]
                public bool MergeHordes { get; set; }

                [JsonProperty(PropertyName = "Spawn system (SpawnsDatabase, Random)")]
                public string SpawnType { get; set; }

                [JsonProperty(PropertyName = "Spawn file (only required when using SpawnsDatabase)")]
                public string SpawnFile { get; set; }

                [JsonProperty(PropertyName = "Amount of time a player needs to be outside of a zombies vision before it forgets about them")]
                public float ForgetTime { get; set; }

                [JsonProperty(PropertyName = "Force all hordes to roam locally")]
                public bool LocalRoam { get; set; }

                [JsonProperty(PropertyName = "Local roam distance")]
                public float RoamDistance { get; set; }

                [JsonProperty(PropertyName = "Use horde profiles for randomly spawned hordes")]
                public bool UseProfiles { get; set; }
            }

            public class MemberOptions
            {
                [JsonProperty(PropertyName = "Can target animals")]
                public bool TargetAnimals { get; set; }

                [JsonProperty(PropertyName = "Can be targeted by turrets")]
                public bool TargetedByTurrets { get; set; }

                [JsonProperty(PropertyName = "Can be targeted by turrets set to peacekeeper mode")]
                public bool TargetedByPeaceKeeperTurrets { get; set; }

                [JsonProperty(PropertyName = "Can be targeted by Bradley APC")]
                public bool TargetedByAPC { get; set; }

                [JsonProperty(PropertyName = "Can target other NPCs")]
                public bool TargetNPCs { get; set; }

                [JsonProperty(PropertyName = "Can target NPCs from HumanNPC")]
                public bool TargetHumanNPCs { get; set; }

                [JsonProperty(PropertyName = "Ignore sleeping players")]
                public bool IgnoreSleepers { get; set; }

                [JsonProperty(PropertyName = "Give all zombies glowing eyes")]
                public bool GiveGlowEyes { get; set; }

                [JsonProperty(PropertyName = "Headshots instantly kill zombie")]
                public bool HeadshotKills { get; set; }

                [JsonProperty(PropertyName = "Projectile weapon aimcone override (0 = disabled)")]
                public float AimconeOverride { get; set; }

                [JsonProperty(PropertyName = "Kill NPCs that are under water")]
                public bool KillUnderWater { get; set; }

                [JsonProperty(PropertyName = "Disable NPC dormant system. This will allow NPCs to move all the time, but at a cost to performance")]
                public bool DisableDormantSystem { get; set; }

                public List<Loadout> Loadouts { get; set; }

                public class Loadout
                {
                    public string LoadoutID { get; set; }

                    [JsonProperty(PropertyName = "Potential names for zombies using this loadout (chosen at random)")]
                    public string[] Names { get; set; }

                    [JsonProperty(PropertyName = "Damage multiplier")]
                    public float DamageMultiplier { get; set; }

                    public VitalStats Vitals { get; set; }

                    public MovementStats Movement { get; set; }

                    public SensoryStats Sensory { get; set; }

                    public List<LootTable.InventoryItem> BeltItems { get; set; }

                    public List<LootTable.InventoryItem> MainItems { get; set; }

                    public List<LootTable.InventoryItem> WearItems { get; set; }
                   
                    public class VitalStats
                    {
                        public float Health { get; set; }
                    }

                    public class MovementStats
                    {
                        [JsonProperty(PropertyName = "Movement speed (running)")]
                        public float RunSpeed { get; set; }

                        [JsonProperty(PropertyName = "Movement speed (walking)")]
                        public float WalkSpeed { get; set; }

                        [JsonProperty(PropertyName = "Turn speed")]
                        public float TurnSpeed { get; set; }

                        [JsonProperty(PropertyName = "Duck speed")]
                        public float DuckSpeed { get; set; }

                        public float Acceleration { get; set; }
                    }

                    public class SensoryStats
                    {
                        [JsonProperty(PropertyName = "Vision range")]
                        public float VisionRange { get; set; }

                        [JsonProperty(PropertyName = "Hearing range")]
                        public float HearingRange { get; set; }

                        [JsonProperty(PropertyName = "Field of view")]
                        public float FOV { get; set; }
                    }

                    [JsonIgnore]
                    private MurdererDefinition _loadoutDefinition;

                    [JsonIgnore]
                    public MurdererDefinition LoadoutDefintion
                    {
                        get
                        {
                            if (_loadoutDefinition == null)
                            {
                                _loadoutDefinition = UnityEngine.Object.Instantiate(DefaultDefinition);

                                _loadoutDefinition.Vitals.HP = Vitals.Health;

                                _loadoutDefinition.Sensory.VisionRange = Sensory.VisionRange;
                                _loadoutDefinition.Sensory.HearingRange = Sensory.HearingRange;
                                _loadoutDefinition.Sensory.FieldOfView = Sensory.FOV;

                                _loadoutDefinition.Movement.RunSpeed = Movement.RunSpeed;
                                _loadoutDefinition.Movement.WalkSpeed = Movement.WalkSpeed;
                                _loadoutDefinition.Movement.Acceleration = Movement.Acceleration;
                                _loadoutDefinition.Movement.DuckSpeed = Movement.DuckSpeed;
                                _loadoutDefinition.Movement.TurnSpeed = Movement.TurnSpeed;
                            }

                            return _loadoutDefinition;
                        }
                    }

                    public Loadout() { }

                    public Loadout(string loadoutID, MurdererDefinition definition)
                    {
                        LoadoutID = loadoutID;

                        Names = new string[] { "Zombie" };

                        DamageMultiplier = 1f;

                        Vitals = new VitalStats() { Health = definition.Vitals.HP };

                        Movement = new MovementStats()
                        {
                            Acceleration = definition.Movement.Acceleration,
                            DuckSpeed = definition.Movement.DuckSpeed,
                            RunSpeed = definition.Movement.RunSpeed,
                            TurnSpeed = definition.Movement.TurnSpeed,
                            WalkSpeed = definition.Movement.WalkSpeed
                        };

                        Sensory = new SensoryStats()
                        {
                            FOV = definition.Sensory.FieldOfView,
                            HearingRange = definition.Sensory.HearingRange,
                            VisionRange = definition.Sensory.VisionRange
                        };

                        BeltItems = new List<LootTable.InventoryItem>();
                        MainItems = new List<LootTable.InventoryItem>();
                        WearItems = new List<LootTable.InventoryItem>();
                    }
                }
            }

            public class LootTable
            {
                [JsonProperty(PropertyName = "Drop inventory on death instead of random loot")]
                public bool DropInventory { get; set; }

                [JsonProperty(PropertyName = "Random loot table")]
                public RandomLoot Random { get; set; }

                public class InventoryItem
                {
                    public string Shortname { get; set; }
                    public ulong SkinID { get; set; }
                    public int Amount { get; set; }

                    [JsonProperty(PropertyName = "Attachments", NullValueHandling = NullValueHandling.Ignore)]
                    public InventoryItem[] SubSpawn { get; set; }
                }

                public class RandomLoot
                {
                    [JsonProperty(PropertyName = "Minimum amount of items to spawn")]
                    public int Minimum { get; set; }

                    [JsonProperty(PropertyName = "Maximum amount of items to spawn")]
                    public int Maximum { get; set; }

                    public List<LootDefinition> List { get; set; }

                    public class LootDefinition
                    {
                        public string Shortname { get; set; }

                        public int Minimum { get; set; }

                        public int Maximum { get; set; }

                        public ulong SkinID { get; set; }

                        [JsonProperty(PropertyName = "Spawn as blueprint")]
                        public bool IsBlueprint { get; set; }

                        [JsonProperty(PropertyName = "Probability (0.0 - 1.0)")]
                        public float Probability { get; set; }

                        [JsonProperty(PropertyName = "Spawn with")]
                        public LootDefinition Required { get; set; }

                        public int GetAmount()
                        {
                            if (Maximum <= 0f || Maximum <= Minimum)
                                return Minimum;

                            return UnityEngine.Random.Range(Minimum, Maximum);
                        }
                    }
                }
            }

            public class MonumentSpawn
            {
                public MonumentSettings Airfield { get; set; }
                public MonumentSettings Dome { get; set; }
                public MonumentSettings Junkyard { get; set; }
                public MonumentSettings LargeHarbor { get; set; }
                public MonumentSettings GasStation { get; set; }
                public MonumentSettings Powerplant { get; set; }
                public MonumentSettings StoneQuarry { get; set; }
                public MonumentSettings SulfurQuarry { get; set; }
                public MonumentSettings HQMQuarry { get; set; }
                public MonumentSettings Radtown { get; set; }
                public MonumentSettings LaunchSite { get; set; }
                public MonumentSettings Satellite { get; set; }
                public MonumentSettings SmallHarbor { get; set; }
                public MonumentSettings Supermarket { get; set; }
                public MonumentSettings Trainyard { get; set; }
                public MonumentSettings Tunnels { get; set; }
                public MonumentSettings Warehouse { get; set; }
                public MonumentSettings WaterTreatment { get; set; }

                public class MonumentSettings : SpawnSettings
                {
                    [JsonProperty(PropertyName = "Enable spawns at this monument")]
                    public bool Enabled { get; set; }
                }
            }

            public class CustomSpawnPoints : SpawnSettings
            {
                public SerializedVector Location { get; set; }

                public class SerializedVector
                {
                    public float X { get; set; }
                    public float Y { get; set; }
                    public float Z { get; set; }

                    public SerializedVector() { }

                    public SerializedVector(float x, float y, float z)
                    {
                        this.X = x;
                        this.Y = y;
                        this.Z = z;
                    }

                    public static implicit operator Vector3(SerializedVector v)
                    {
                        return new Vector3(v.X, v.Y, v.Z);
                    }

                    public static implicit operator SerializedVector(Vector3 v)
                    {
                        return new SerializedVector(v.x, v.y, v.z);
                    }
                }
            }

            public class SpawnSettings
            {
                [JsonProperty(PropertyName = "Distance that this horde can roam from their initial spawn point")]
                public float RoamDistance { get; set; }

                [JsonProperty(PropertyName = "Maximum amount of members in this horde")]
                public int HordeSize { get; set; }

                [JsonProperty(PropertyName = "Horde profile")]
                public string Profile { get; set; }
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
                Horde = new ConfigData.HordeOptions
                {
                    InitialMemberCount = 3,
                    MaximumHordes = 5,
                    MaximumMemberCount = 10,
                    GrowthRate = 300,
                    CreateOnDeath = true,
                    ForgetTime = 10f,
                    MergeHordes = true,
                    RespawnTime = 900,                   
                    SpawnType = "Random",
                    SpawnFile = "",
                    LocalRoam = false,
                    RoamDistance = 150,
                    UseProfiles = false
                },
                Member = new ConfigData.MemberOptions
                {
                    IgnoreSleepers = false,
                    TargetAnimals = false,
                    TargetedByTurrets = false,
                    TargetedByAPC = false,                    
                    TargetNPCs = true,
                    TargetHumanNPCs = false,
                    GiveGlowEyes = true,
                    HeadshotKills = true,
                    Loadouts = BuildDefaultLoadouts(),
                    AimconeOverride = 0,
                    KillUnderWater = true,
                    TargetedByPeaceKeeperTurrets = true,
                    DisableDormantSystem = true
                },
                Loot = new ConfigData.LootTable
                {
                    DropInventory = false,
                    Random = BuildDefaultLootTable(),
                },
                TimedSpawns = new ConfigData.TimedSpawnOptions
                {
                    Enabled = false,
                    Despawn = true,
                    Start = 18f,
                    End = 6f
                },
                HordeProfiles = new Dictionary<string, List<string>>
                {
                    ["Profile1"] = new List<string> { "loadout-1", "loadout-2", "loadout-3" },
                    ["Profile2"] = new List<string> { "loadout-2", "loadout-3", "loadout-4" },
                },
                Monument = new ConfigData.MonumentSpawn
                {
                    Airfield = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 85,
                        HordeSize = 10,
                        Profile = "",
                    },
                    Dome = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 50,
                        HordeSize = 10,
                    },
                    Junkyard = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 100,
                        HordeSize = 10,
                        Profile = ""
                    },
                    GasStation = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 40,
                        HordeSize = 10,
                        Profile = ""
                    },
                    LargeHarbor = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 120,
                        HordeSize = 10,
                        Profile = ""
                    },
                    Powerplant = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 120,
                        HordeSize = 10,
                        Profile = ""
                    },
                    HQMQuarry = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 40,
                        HordeSize = 10,
                        Profile = ""
                    },
                    StoneQuarry = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 40,
                        HordeSize = 10,
                        Profile = ""
                    },
                    SulfurQuarry = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 40,
                        HordeSize = 10,
                        Profile = ""
                    },
                    Radtown = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 85,
                        HordeSize = 10,
                        Profile = ""
                    },
                    LaunchSite = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 140,
                        HordeSize = 10,
                        Profile = ""
                    },
                    Satellite = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 60,
                        HordeSize = 10,
                        Profile = ""
                    },
                    SmallHarbor = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 85,
                        HordeSize = 10,
                        Profile = ""
                    },
                    Supermarket = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 20,
                        HordeSize = 10,
                        Profile = ""
                    },
                    Trainyard = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 100,
                        HordeSize = 10,
                        Profile = ""
                    },
                    Tunnels = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 90,
                        HordeSize = 10,
                        Profile = ""
                    },
                    Warehouse = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 40,
                        HordeSize = 10,
                        Profile = ""
                    },
                    WaterTreatment = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 120,
                        HordeSize = 10,
                        Profile = ""
                    },
                },
                Version = Version
            };
        }

        private List<ConfigData.MemberOptions.Loadout> BuildDefaultLoadouts()
        {
            List<ConfigData.MemberOptions.Loadout> list = new List<ConfigData.MemberOptions.Loadout>();

            MurdererDefinition definition = DefaultDefinition;
            if (definition != null)
            {
                for (int i = 0; i < definition.loadouts.Length; i++)
                {
                    PlayerInventoryProperties inventoryProperties = definition.loadouts[i];

                    ConfigData.MemberOptions.Loadout loadout = new ConfigData.MemberOptions.Loadout($"loadout-{list.Count}", definition);

                    for (int belt = 0; belt < inventoryProperties.belt.Count; belt++)
                    {
                        PlayerInventoryProperties.ItemAmountSkinned item = inventoryProperties.belt[belt];

                        loadout.BeltItems.Add(new ConfigData.LootTable.InventoryItem() { Shortname = item.itemDef.shortname, SkinID = item.skinOverride, Amount = (int)item.amount });
                    }

                    for (int main = 0; main < inventoryProperties.main.Count; main++)
                    {
                        PlayerInventoryProperties.ItemAmountSkinned item = inventoryProperties.main[main];

                        loadout.MainItems.Add(new ConfigData.LootTable.InventoryItem() { Shortname = item.itemDef.shortname, SkinID = item.skinOverride, Amount = (int)item.amount });
                    }

                    for (int wear = 0; wear < inventoryProperties.wear.Count; wear++)
                    {
                        PlayerInventoryProperties.ItemAmountSkinned item = inventoryProperties.wear[wear];

                        loadout.WearItems.Add(new ConfigData.LootTable.InventoryItem() { Shortname = item.itemDef.shortname, SkinID = item.skinOverride, Amount = (int)item.amount });
                    }

                    list.Add(loadout);
                }
            }
            return list;
        }

        private ConfigData.LootTable.RandomLoot BuildDefaultLootTable()
        {
            ConfigData.LootTable.RandomLoot randomLoot = new ConfigData.LootTable.RandomLoot();

            randomLoot.Minimum = 3;
            randomLoot.Maximum = 9;
            randomLoot.List = new List<ConfigData.LootTable.RandomLoot.LootDefinition>();

            MurdererDefinition definition = DefaultDefinition;
            if (definition != null)
            {
                for (int i = 0; i < definition.Loot.Length; i++)
                {
                    LootContainer.LootSpawnSlot lootSpawn = definition.Loot[i];

                    for (int y = 0; y < lootSpawn.definition.subSpawn.Length; y++)
                    {
                        LootSpawn.Entry entry = lootSpawn.definition.subSpawn[y];                                               

                        for (int c = 0; c < entry.category.items.Length; c++)
                        {
                            ItemAmountRanged itemAmountRanged = entry.category.items[c];

                            ConfigData.LootTable.RandomLoot.LootDefinition lootDefinition = new ConfigData.LootTable.RandomLoot.LootDefinition();
                            lootDefinition.Probability = lootSpawn.probability;
                            lootDefinition.Shortname = itemAmountRanged.itemDef.shortname;
                            lootDefinition.Minimum = (int)itemAmountRanged.amount;
                            lootDefinition.Maximum = (int)itemAmountRanged.maxAmount;
                            lootDefinition.SkinID = 0;
                            lootDefinition.IsBlueprint = itemAmountRanged.itemDef.spawnAsBlueprint;
                            lootDefinition.Required = null;

                            randomLoot.List.Add(lootDefinition);
                        }
                    }
                }
            }
            return randomLoot;
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new Core.VersionNumber(0, 2, 0))
                configData = baseConfig;

            if (configData.Version < new Core.VersionNumber(0, 2, 1))
                configData.Loot.Random = baseConfig.Loot.Random;

            if (configData.Version < new Core.VersionNumber(0, 2, 2))
            {
                for (int i = 0; i < configData.Member.Loadouts.Count; i++)                
                    configData.Member.Loadouts[i].LoadoutID = $"loadout-{i}";

                configData.Horde.LocalRoam = false;
                configData.Horde.RoamDistance = 150;
                configData.Horde.UseProfiles = false;

                configData.HordeProfiles = baseConfig.HordeProfiles;

                configData.Monument.Airfield.Profile = string.Empty;
                configData.Monument.Dome.Profile = string.Empty;
                configData.Monument.GasStation.Profile = string.Empty;
                configData.Monument.HQMQuarry.Profile = string.Empty;
                configData.Monument.Junkyard.Profile = string.Empty;
                configData.Monument.LargeHarbor.Profile = string.Empty;
                configData.Monument.LaunchSite.Profile = string.Empty;
                configData.Monument.Powerplant.Profile = string.Empty;
                configData.Monument.Radtown.Profile = string.Empty;
                configData.Monument.Satellite.Profile = string.Empty;
                configData.Monument.SmallHarbor.Profile = string.Empty;
                configData.Monument.StoneQuarry.Profile = string.Empty;
                configData.Monument.SulfurQuarry.Profile = string.Empty;
                configData.Monument.Supermarket.Profile = string.Empty;
                configData.Monument.Trainyard.Profile = string.Empty;
                configData.Monument.Tunnels.Profile = string.Empty;
                configData.Monument.Warehouse.Profile = string.Empty;
                configData.Monument.WaterTreatment.Profile = string.Empty;
            }

            if (configData.Version < new Core.VersionNumber(0, 2, 5))
                configData.Member.AimconeOverride = 0f;

            if (configData.Version < new Core.VersionNumber(0, 2, 13))
                configData.TimedSpawns = baseConfig.TimedSpawns;

            if (configData.Version < new Core.VersionNumber(0, 2, 18))            
                configData.Member.TargetedByPeaceKeeperTurrets = configData.Member.TargetedByTurrets; 

            if (configData.Version < new Core.VersionNumber(0, 2, 30))
            {
                if (configData.Horde.SpawnType == "RandomSpawns" || configData.Horde.SpawnType == "Default")
                    configData.Horde.SpawnType = "Random";
            }

            if (configData.Version < new Core.VersionNumber(0, 2, 31))
            {
                if (string.IsNullOrEmpty(configData.Horde.SpawnType))
                    configData.Horde.SpawnType = "Random";

                configData.Member.DisableDormantSystem = true;
            }
            
            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion
    }
}
