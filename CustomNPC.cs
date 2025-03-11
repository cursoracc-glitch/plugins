using Facepunch;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("CustomNPC", "k1lly0u", "1.2.2")]
    class CustomNPC : RustPlugin
    {
        #region Fields
        [PluginReference] private Plugin Kits;

        private Hash<Plugin, List<HumanAI>> _humanAIRef = new Hash<Plugin, List<HumanAI>>();

        private static CustomNPC Instance;
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            Instance = this;
        }

        private void OnEntityKill(ScientistNPC scientistNpc)
        {
            if (scientistNpc == null)
                return;

            HumanAI humanAI = scientistNpc.GetComponent<HumanAI>();
            if (humanAI == null)
                return;

            foreach (KeyValuePair<Plugin, List<HumanAI>> kvp in _humanAIRef)
                kvp.Value.Remove(humanAI);

            UnityEngine.Object.Destroy(humanAI);
        }

        private object OnNpcTarget(ScientistNPC scientistNpc, BasePlayer target)
        {
            if (scientistNpc == null)
                return null;

            HumanAI humanAI = scientistNpc.GetComponent<HumanAI>();
            if (humanAI == null)
                return null;

            if (target.IsSleeping() || target.IsFlying)
                return true;

            return null;
        }

        private void OnPluginUnloaded(Plugin plugin)
        {
            List<HumanAI> list;
            if (_humanAIRef.TryGetValue(plugin, out list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    HumanAI humanAI = list[i];
                    if (humanAI != null)
                        humanAI.Despawn();
                }

                _humanAIRef.Remove(plugin);
            }
        }

        private void Unload()
        {
            foreach (KeyValuePair<Plugin, List<HumanAI>> kvp in _humanAIRef)
            {
                for (int i = kvp.Value.Count - 1; i >= 0; i--)
                {
                    HumanAI humanAI = kvp.Value[i];
                    if (humanAI != null)
                        humanAI.Despawn();
                }
            }

            _humanAIRef.Clear();

            Instance = null;
        }
        #endregion

        #region Functions        
        private HumanAI CreateHumanAI(Vector3 position, Settings settings)
        {
            object point = settings.EnableNavMesh ? FindPointOnNavmesh(position, 60) : position;
            if (point is Vector3)
            {
                position = (Vector3)point;

                if (settings.EnableNavMesh && (position.y < -0.25f || (settings.KillInSafeZone && IsInSafeZone(position))))
                    return null;

                const string SCIENTIST_PREFAB = "assets/rust.ai/agents/npcplayer/humannpc/heavyscientist/heavyscientist.prefab";

                ScientistNPC npcPlayer = InstantiateEntity(SCIENTIST_PREFAB, position);
                npcPlayer.enableSaving = false;

                BaseAIBrain<global::HumanNPC> defaultBrain = npcPlayer.GetComponent<BaseAIBrain<global::HumanNPC>>();
                defaultBrain._baseEntity = npcPlayer;
                UnityEngine.Object.DestroyImmediate(defaultBrain);

                HumanAI humanAI = npcPlayer.gameObject.AddComponent<HumanAI>();
                npcPlayer._brain = humanAI.Brain = npcPlayer.gameObject.AddComponent<CustomAIBrain>();

                npcPlayer.Spawn();

                humanAI.Setup(settings);

                return humanAI;
            }

            return null;
        }

        private HumanAI ConvertHumanAI(ScientistNPC scientistNPC, Settings settings)
        {
            Vector3 position = scientistNPC.transform.position;
            object point = FindPointOnNavmesh(position, 60);
            if (point is Vector3)
            {
                position = (Vector3)point;

                if (position.y < -0.25f || (settings.KillInSafeZone && IsInSafeZone(position)))
                    return null;

                HumanAI humanAI = scientistNPC.gameObject.AddComponent<HumanAI>();
                humanAI.Setup(settings);

                return humanAI;
            }

            return null;
        }

        private static ScientistNPC InstantiateEntity(string type, Vector3 position)
        {
            GameObject gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(type), position, Quaternion.identity);
            gameObject.name = type;

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            ScientistNPC component = gameObject.GetComponent<ScientistNPC>();
            return component;
        }

        private static bool IsInSafeZone(Vector3 position)
        {
            int count = Physics.OverlapSphereNonAlloc(position, 1f, Vis.colBuffer, 1 << 18, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Collider collider = Vis.colBuffer[i];
                if (collider.GetComponent<TriggerSafeZone>())
                    return true;
            }

            return false;
        }

        private static void StripInventory(BasePlayer player, bool skipWear = false)
        {
            Item[] allItems = player.inventory.AllItems();

            for (int i = allItems.Length - 1; i >= 0; i--)
            {
                Item item = allItems[i];
                if (skipWear && item?.parent == player.inventory.containerWear)
                    continue;

                item.RemoveFromContainer();
                item.Remove();
            }
        }
        #endregion

        #region API
        /// <summary>
        /// Spawns a new ScientistNPC and add's a HumanAI component on top
        /// </summary>
        /// <param name="plugin">The plugin the NPC belongs to</param>
        /// <param name="position">The spawn position</param>
        /// <param name="settingsJson">The settings JObject</param>
        /// <returns></returns>
        private ScientistNPC SpawnNPC(Plugin plugin, Vector3 position, JObject settingsJson)
        {
            if (Rust.Ai.AiManager.nav_disable)
            {
                Debug.LogWarning($"[CustomNPC] - NPC's can not be spawned when the Navmesh is disabled!\nYou can turn it on with the 'aimanager.nav_disable false' convar");
                return null;
            }

            Settings settings = settingsJson.ToObject<Settings>();

            HumanAI npc = CreateHumanAI(position, settings);
            if (npc == null)
                return null;

            List<HumanAI> list;
            if (!_humanAIRef.TryGetValue(plugin, out list))
                list = _humanAIRef[plugin] = new List<HumanAI>();

            list.Add(npc);

            return npc.Entity;
        }

        /// <summary>
        /// Converts an already spawned ScientistNPC to a HumanAI
        /// </summary>
        /// <param name="plugin">The plugin the NPC belongs to</param>
        /// <param name="scientistNPC">The target NPC</param>
        /// <param name="settingsJson">The settings JObject</param>
        /// <returns></returns>
        private bool ConvertNPC(Plugin plugin, ScientistNPC scientistNPC, JObject settingsJson)
        {
            Settings settings = settingsJson.ToObject<Settings>();

            HumanAI npc = ConvertHumanAI(scientistNPC, settings);
            if (npc == null)
                return false;

            List<HumanAI> list;
            if (!_humanAIRef.TryGetValue(plugin, out list))
                list = _humanAIRef[plugin] = new List<HumanAI>();

            list.Add(npc);

            return true;
        }

        /// <summary>
        /// Allows plugins to add a custom AI state to the NPC
        /// </summary>
        /// <param name="scientistNPC">The target NPC</param>
        /// <param name="state">The custom AI state</param>
        /// <returns>true if successfull, false if not</returns>
        private bool AddCustomAIState(ScientistNPC scientistNPC, BaseAIBrain<global::HumanNPC>.BasicAIState state)
        {
            HumanAI humanAI = scientistNPC.GetComponent<HumanAI>();
            if (humanAI != null)
            {
                humanAI.Brain.AddState(state);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set the base position the NPC will roam around
        /// </summary>
        /// <param name="scientistNPC">The target NPC</param>
        /// <param name="position">The base roam position</param>
        private void SetRoamHomePosition(ScientistNPC scientistNPC, Vector3 position)
        {
            HumanAI humanAI = scientistNPC.GetComponent<HumanAI>();
            if (humanAI != null)
            {
                object point = FindPointOnNavmesh(position, 20);
                if (point is Vector3)
                    position = (Vector3)point;
                
                humanAI._roamBasePosition = position;
            }
        }

        /// <summary>
        /// Enables the NPC's NavMeshAgent
        /// </summary>
        /// <param name="scientistNPC">The target NPC</param>
        /// <returns></returns>
        private bool EnableNavAgent(ScientistNPC scientistNPC)
        {
            HumanAI humanAI = scientistNPC.GetComponent<HumanAI>();
            if (humanAI != null)
            {
                object point = FindPointOnNavmesh(scientistNPC.transform.position, 20);
                if (point is Vector3)
                {
                    Vector3 position = (Vector3)point;
                    scientistNPC.NavAgent.Warp(position);
                    scientistNPC.NavAgent.enabled = true;
                    scientistNPC.transform.position = position;

                    humanAI._NavMeshEnabled = true;
                    humanAI.NavMeshEnabled = true;

                    scientistNPC.NavAgent.isStopped = false;
                    humanAI.SetDestination(position);

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Force the NPC to equip their weapon
        /// </summary>
        /// <param name="scientistNPC">The target NPC</param>
        /// <returns></returns>
        private void EquipWeapon(ScientistNPC scientistNPC)
        {
            HumanAI humanAI = scientistNPC.GetComponent<HumanAI>();
            if (humanAI != null)
                humanAI.EquipWeapon();
        }

        /// <summary>
        /// Force the NPC to holster their weapon
        /// </summary>
        /// <param name="scientistNPC">The target NPC</param>
        /// <returns></returns>
        private void HolsterWeapon(ScientistNPC scientistNPC)
        {
            HumanAI humanAI = scientistNPC.GetComponent<HumanAI>();
            if (humanAI != null)
                humanAI.HolsterWeapon();
        }

        /// <summary>
        /// Send the NPC to the target destination (requires the MoveToDestination state)
        /// </summary>
        /// <param name="scientistNPC">The target NPC</param>
        /// <param name="destination">The destination</param>
        /// <param name="speed">The speed at which they will move towards the destination</param>
        /// <param name="onReachedDestination">A callback for once they have reached the destination</param>
        private void SetDestination(ScientistNPC scientistNPC, Vector3 destination, global::HumanNPC.SpeedType speed, Action onReachedDestination)
        {
            HumanAI humanAI = scientistNPC.GetComponent<HumanAI>();
            if (humanAI != null)
                humanAI.SetDestination(destination, speed, onReachedDestination);
        }
        #endregion

        #region Settings
        /// <summary>
        /// This the the settings class. The JObject passed through the hook will be converted to this class structure
        /// </summary>
        internal class Settings
        {
            public List<string> States = new List<string>(); // The logic states to apply to the NPC


            public string NPCType; // The type of NPC (Scientist, Murderer, HeavyScientist)

            public string DisplayName; // The name of the NPC

            public float Health; // The NPCs initial health


            public float SightRange; // The NPCs sight range

            public float RoamRange; // The maximum distance from it's spawn point the NPC will roam      

            public float ChaseRange; // The distance from it's spawn point the NPC will chase a target to      


            public string Kit; // The kit to give the NPC

            public bool StripCorpseLoot; // Removes the NPC corpse loot table


            public bool KillInSafeZone; // Kill the NPC if they enter a safe zone, prevent spawn if in a safe zone

            public float DespawnTime; // Despawn the NPC after this amount of time (seconds)


            public bool StartDead; // Spawn the NPC dead

            public bool StartWounded; // NPC spawns in a wounded state

            public float WoundedDuration; // The amount of time the NPC will be in the wounded state before attempting to recover

            public float WoundedRecoveryChance; // The chance the NPC will recover from the wounded state (0.0 - 100.0)


            public bool EnableNavMesh = true; // Enables the NavMeshAgent component on spawn

            public bool EquipWeapon = true; // Equips a weapon on spawn

            public bool CanUseWeaponMounted; // Can equip a weapon whilst mounted
        }
        #endregion

        #region NavMesh
        private static NavMeshHit navmeshHit;

        private static RaycastHit raycastHit;

        private static Collider[] _buffer = new Collider[256];

        private const int WORLD_LAYER = 65536;

        internal static object FindPointOnNavmesh(Vector3 targetPosition, float maxDistance = 4f)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 position = i == 0 ? targetPosition : targetPosition + (UnityEngine.Random.onUnitSphere * (maxDistance * 0.5f));
                if (NavMesh.SamplePosition(position, out navmeshHit, maxDistance * 0.5f, 1))
                {
                    if (IsInRockPrefab(navmeshHit.position))
                        continue;

                    if (IsNearWorldCollider(navmeshHit.position))
                        continue;

                    return navmeshHit.position;
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

            return removed != count;
        }

        private static string[] acceptedColliders = new string[] { "road", "rocket_factory", "train_track", "runway", "_grounds", "concrete_slabs", "office", "industrial", "junkyard" };

        private static string[] blockedColliders = new string[] { "rock", "junk", "range", "invisible" };
        #endregion

        #region NPC Component
        public class HumanAI : MonoBehaviour
        {
            internal ScientistNPC Entity { get; private set; }

            internal Transform Transform { get; private set; }

            internal CustomAIBrain Brain { get; set; }


            internal AttackEntity _attackEntity;

            internal Vector3 _roamBasePosition;

            internal Settings _settings;

            internal bool HasHomePosition { get; private set; }

            internal float DistanceFromBase => Vector3.Distance(Transform.position, _roamBasePosition);

            internal bool _NavMeshEnabled
            {
                get
                {
                    return (bool)typeof(global::HumanNPC).GetField("navmeshEnabled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).GetValue(Entity);
                }
                set
                {
                    typeof(global::HumanNPC).GetField("navmeshEnabled", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).SetValue(Entity, value);
                }
            }

            internal bool NavMeshEnabled { get; set; }

            private const int AREA_MASK = 1;

            private const int AGENT_TYPE_ID = -1372625422;

            private void Awake()
            {
                Entity = GetComponent<ScientistNPC>();
                Transform = Entity.transform;

                _roamBasePosition = Transform.position;
            }

            private void Start()
            {
                //Destroy(GetComponent<HumanBrain>());
                //Entity._brain = Brain = gameObject.AddComponent<CustomAIBrain>();
                //Brain.UseAIDesign = false;
            }

            internal void Setup(Settings settings)
            {
                this._settings = settings;

                Entity.CancelInvoke(Entity.EnableNavAgent);
                Entity.CancelInvoke(Entity.EquipTest);                

                if (settings.StripCorpseLoot)
                    Entity.LootSpawnSlots = new LootContainer.LootSpawnSlot[0];

                if (!string.IsNullOrEmpty(settings.DisplayName))
                    Entity.displayName = settings.DisplayName;

                if (settings.StartDead)
                {
                    UpdateGear();
                    Entity.Invoke(Die, 1f);
                    return;
                }

                if (settings.SightRange != 0f)
                {
                    Entity.sightRange = settings.SightRange;
                    Entity.sightRangeLarge = settings.SightRange * 3;
                }

                Entity.NavAgent.areaMask = AREA_MASK;
                Entity.NavAgent.agentTypeID = AGENT_TYPE_ID;

                if (settings.EnableNavMesh)
                    Entity.Invoke(EnableNavAgent, 0.25f);

                if (settings.Health != 0f)
                    Entity.InitializeHealth(settings.Health, settings.Health);

                if (settings.DespawnTime != 0f)
                    Entity.Invoke(Despawn, settings.DespawnTime);

                UpdateGear();

                if (settings.StartWounded)
                    Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, true);
                else
                {
                    if (settings.EquipWeapon)
                        Entity.Invoke(EquipWeapon, 0.25f);
                }

                if (_settings.NPCType.Equals("Murderer"))
                {
                    Entity.DeathEffects = ZombieDeathEffects;
                    Entity.RadioChatterEffects = ZombieChatterEffects;
                    Entity.IdleChatterRepeatRange = new Vector2(10f, 15f);
                }

                Entity.InvokeRandomized(UpdateTick, 1f, 4f, 1f);
            }

            internal void AddStates()
            {
                Brain.ClearStates();

                for (int i = 0; i < _settings.States.Count; i++)
                {
                    string state = _settings.States[i];

                    switch (state)
                    {
                        case "IdleState":
                            Brain.AddState(new CustomAIBrain.IdleState(this));
                            break;
                        case "RoamState":
                            Brain.AddState(new CustomAIBrain.RoamState(this, _settings.RoamRange));
                            break;
                        case "ChaseState":
                            Brain.AddState(new CustomAIBrain.ChaseState(this, _settings.ChaseRange));
                            break;
                        case "CombatState":
                            Brain.AddState(new CustomAIBrain.CombatState(this, _settings.ChaseRange));
                            break;
                        case "MountedState":
                            Brain.AddState(new CustomAIBrain.MountedState(this, _settings.CanUseWeaponMounted));
                            break;
                        case "MoveDestinationState":
                            Brain.AddState(new CustomAIBrain.MoveDestinationState(this));
                            break;
                        case "FallingState":
                            Brain.AddState(new CustomAIBrain.FallingState(this));
                            break;
                        default:
                            break;
                    }
                }

                if (_settings.StartWounded && _settings.WoundedDuration != 0f)
                    Brain.AddState(new CustomAIBrain.WoundedState(this, _settings.WoundedDuration, _settings.WoundedRecoveryChance));
            }

            private void UpdateGear()
            {
                if (!string.IsNullOrEmpty(_settings.Kit))
                {
                    StripInventory(Entity);
                    Instance.Kits?.Call("GiveKit", Entity, _settings.Kit);
                }
                else
                {
                    if (_settings.NPCType == "Murderer")
                    {
                        StripInventory(Entity);

                        ItemManager.CreateByName("halloween.surgeonsuit").MoveToContainer(Entity.inventory.containerWear);
                        ItemManager.CreateByName("gloweyes").MoveToContainer(Entity.inventory.containerWear);
                        ItemManager.CreateByName("machete").MoveToContainer(Entity.inventory.containerBelt);
                    }
                    else if (_settings.NPCType == "Scientist")
                    {
                        StripInventory(Entity);

                        ItemManager.CreateByName("hazmatsuit_scientist").MoveToContainer(Entity.inventory.containerWear);
                        switch (UnityEngine.Random.Range(0, 2))
                        {
                            case 0:
                                ItemManager.CreateByName("smg.mp5").MoveToContainer(Entity.inventory.containerBelt, 0);
                                break;
                            case 1:
                                ItemManager.CreateByName("shotgun.spas12").MoveToContainer(Entity.inventory.containerBelt, 0);
                                break;
                            default:
                                ItemManager.CreateByName("pistol.m92").MoveToContainer(Entity.inventory.containerBelt, 0);
                                break;
                        }
                    }
                }
            }

            internal void EnableNavAgent()
            {
                NavMeshHit navMeshHit;

                if (!NavMesh.SamplePosition(Transform.position + (Vector3.up * 1f), out navMeshHit, 20f, -1))
                {
                    Debug.Log("Failed to sample navmesh");
                    return;
                }

                Entity.NavAgent.Warp(navMeshHit.position);
                Transform.position = navMeshHit.position;

                _NavMeshEnabled = NavMeshEnabled = true;

                Entity.NavAgent.enabled = true;
                Entity.NavAgent.isStopped = false;
                SetDestination(Transform.position);
            }

            internal void DisableNavAgent()
            {
                if (!_NavMeshEnabled)
                    return;

                Entity.NavAgent.destination = Transform.position; 
                Entity.NavAgent.isStopped = true;
                Entity.NavAgent.enabled = false;

                _NavMeshEnabled = NavMeshEnabled = false;
            }

            internal void SetDestination(Vector3 destination)
            {
                if (NavMeshEnabled)
                {
                    if (!_NavMeshEnabled)
                        _NavMeshEnabled = true;

                    Entity.NavAgent.enabled = true;
                    Entity.NavAgent.isStopped = false;
                    Entity.SetDestination(destination);
                }
            }

            internal void SetDestination(Vector3 destination, global::HumanNPC.SpeedType speed, Action onReachedDestination)
            {
                CustomAIBrain.MoveDestinationState moveDestinationState = Brain.GetState<CustomAIBrain.MoveDestinationState>() as CustomAIBrain.MoveDestinationState;
                if (moveDestinationState != null)
                    moveDestinationState.SetDestination(destination, speed, onReachedDestination);                
            }

            internal void ForgetEntity(BaseEntity baseEntity)
            {
                Entity.myMemory.Players.Remove(baseEntity);
                Entity.myMemory.Targets.Remove(baseEntity);
                Entity.myMemory.Threats.Remove(baseEntity);
                Entity.myMemory.Friendlies.Remove(baseEntity);
                Entity.myMemory.LOS.Remove(baseEntity);
            }

            private void UpdateTick()
            {
                if (Entity == null || Entity.IsDestroyed)
                {
                    Destroy(this);
                    return;
                }

                if (_attackEntity == null)
                    _attackEntity = Entity.GetAttackEntity();
            }

            internal void EquipWeapon()
            {
                if (Entity == null || Entity.IsDestroyed || Entity.IsDead())
                    return;

                if (Entity.isMounted && !_settings.CanUseWeaponMounted)
                    return;

                for (int i = 0; i < Entity.inventory.containerBelt.itemList.Count; i++)
                {
                    Item slot = Entity.inventory.containerBelt.GetSlot(i);
                    if (slot != null)
                    {
                        Entity.UpdateActiveItem(Entity.inventory.containerBelt.GetSlot(i).uid);

                        BaseEntity heldEntity = slot.GetHeldEntity();
                        if (heldEntity != null)
                        {
                            _attackEntity = heldEntity.GetComponent<AttackEntity>();

                            if (_attackEntity != null)
                                _attackEntity.TopUpAmmo();

                            if (_attackEntity is BaseProjectile)
                                _attackEntity.effectiveRange *= 2f;
                        }

                        return;
                    }
                }
            }

            internal void HolsterWeapon()
            {
                Entity.svActiveItemID = 0;

                Item activeItem = Entity.GetActiveItem();
                if (activeItem != null)
                {
                    HeldEntity heldEntity = activeItem.GetHeldEntity() as HeldEntity;
                    if (heldEntity != null)
                    {
                        heldEntity.SetHeld(false);
                    }
                }

                Entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                Entity.inventory.UpdatedVisibleHolsteredItems();
            }

            internal void Die()
            {
                if (Entity == null || Entity.IsDestroyed || Entity.IsDead())
                    return;

                Entity.Die(new HitInfo(Entity, Entity, DamageType.Explosion, 1000f));
            }

            internal void Die(HitInfo hitInfo)
            {
                if (Entity == null || Entity.IsDestroyed || Entity.IsDead())
                    return;

                Entity.Die(hitInfo);
            }

            internal void Despawn()
            {
                if (Entity == null || Entity.IsDestroyed || Entity.IsDead())
                    return;

                StripInventory(Entity);
                Entity.Kill();
            }

            private void OnDestroy()
            {
                Facepunch.Pool.FreeList(ref friendlies);

                Destroy(Brain);

                if (Entity != null)
                {                    
                    Entity.CancelInvoke(EnableNavAgent);
                    Entity.CancelInvoke(EquipWeapon);
                    Entity.CancelInvoke(UpdateTick);
                    Entity.CancelInvoke(Despawn);
                    Entity.CancelInvoke(Die);
                }
            }

            private readonly static GameObjectRef[] ZombieDeathEffects = new GameObjectRef[]
            {
                new GameObjectRef(){ guid = GameManifest.guidToPath.FirstOrDefault(x => x.Value.Equals("assets/prefabs/npc/murderer/sound/death.prefab")).Key }
            };

            private readonly static GameObjectRef[] ZombieChatterEffects = new GameObjectRef[]
            {
                new GameObjectRef(){ guid = GameManifest.guidToPath.FirstOrDefault(x => x.Value.Equals("assets/prefabs/npc/murderer/sound/breathing.prefab")).Key }
            };

            #region Senses
            private float nextSenseUpdateTime;

            private float nextFriendlyUpdateTime;

            private const float FRIENDLY_UPDATE_INTERVAL = 5f;
           
            private const float SENSE_UPDATE_INTERVAL = 0.5f;

            private const float SENSE_RANGE = 40f;

            private const float VISION_CONE = -0.4f;

            private static readonly BaseEntity[] QueryResults = new BaseEntity[64];

            private static readonly BasePlayer[] PlayerQueryResults = new BasePlayer[64];

            private List<HumanAI> friendlies = Facepunch.Pool.GetList<HumanAI>();

            internal void UpdateSenses()
            {
                if (Entity == null)
                    return;

                if (Time.time < nextSenseUpdateTime)
                    return;

                nextSenseUpdateTime = Time.time + SENSE_UPDATE_INTERVAL;

                SenseBrains();
                SensePlayers();
            }

            internal void UpdateNearbyFriendlies()
            {
                if (Entity == null)
                    return;

                if (Time.time < nextFriendlyUpdateTime)
                    return;

                nextFriendlyUpdateTime = Time.time + FRIENDLY_UPDATE_INTERVAL;

                friendlies.Clear();

                BaseEntity.Query.Server.GetPlayersInSphere(Transform.position, 40f, PlayerQueryResults, new Func<BasePlayer, bool>(IsFriendlyPlayer));               
            }

            private void SenseBrains()
            {
                int brainsInSphere = BaseEntity.Query.Server.GetBrainsInSphere(Transform.position, _settings.SightRange, QueryResults, new Func<BaseEntity, bool>(CaresAbout));
                for (int i = 0; i < brainsInSphere; i++)                
                    Entity.myMemory.SetKnown(QueryResults[i], Entity, null);                
            }

            private void SensePlayers()
            {
                ScientistNPC scientistNPC = Entity;
                int playersInSphere = BaseEntity.Query.Server.GetPlayersInSphere(Transform.position, _settings.SightRange, PlayerQueryResults, new Func<BasePlayer, bool>(CaresAbout));
                for (int i = 0; i < playersInSphere; i++)
                {
                    BasePlayer result = PlayerQueryResults[i];

                    Entity.myMemory.SetKnown(result, Entity, null);

                    for (int y = 0; y < friendlies.Count; y++)
                    {
                        scientistNPC = friendlies[y]?.Entity;

                        if (scientistNPC != null)
                            scientistNPC.myMemory.SetKnown(result, scientistNPC, null);
                    }
                }
            }

            private bool IsFriendlyPlayer(BasePlayer player)
            {
                if (!player?.IsNpc ?? false)
                    return false;

                HumanAI humanAI = player.GetComponent<HumanAI>();
                if (humanAI != null)
                {
                    friendlies.Add(humanAI);
                    return true;
                }

                return false;
            }

            private bool CaresAbout(BaseEntity entity)
            {
                if (entity == null)                
                    return false;
                
                if (entity.EqualNetID(Entity))                
                    return false;
                
                if (entity.Health() <= 0f)                
                    return false;
                
                if (!CanSenseType(entity))                
                    return false;                

                if (entity is BasePlayer && (entity as BasePlayer).InSafeZone())                
                    return false;
                
                if (entity is BaseCombatEntity && (entity as BaseCombatEntity).TimeSinceLastNoise <= 1f && (entity as BaseCombatEntity).CanLastNoiseBeHeard(Transform.position, SENSE_RANGE))                
                    return true;
                
                if (!IsTargetInVision(entity))                
                    return false;

                bool hasLOS = CanSeeTarget(entity);
                Entity.myMemory.SetLOS(entity, hasLOS);

                if (!hasLOS)
                    return false;

                return true;                
            }

            private bool CanSenseType(BaseEntity ent)
            {
                BasePlayer basePlayer = ent as BasePlayer;
                if (basePlayer != null && !basePlayer.IsNpc)
                    return true;

                if (ent is BaseNpc)                
                    return true;
                                
                if (ent is TimedExplosive)                
                    return true;                
               
                return false;
            }

            private bool CanSeeTarget(BaseEntity entity)
            {
                BasePlayer basePlayer = entity as BasePlayer;
                if (basePlayer == null)                
                    return true;
                
                return Entity.IsPlayerVisibleToUs(basePlayer);
            }

            private bool IsTargetInVision(BaseEntity target)
            {
                Vector3 vector3 = Vector3Ex.Direction(target.transform.position, Transform.position);
                return Vector3.Dot((Entity != null ? Entity.eyes.BodyForward() : Transform.forward), vector3) >= VISION_CONE;
            }
            #endregion

        }
        #endregion

        #region States
        public class CustomAIBrain : BaseAIBrain<global::HumanNPC>
        {
            private HumanAI humanAI;

            private void Awake()
            {
                humanAI = GetComponent<HumanAI>();
            }

            public override void AddStates()
            {
                base.AddStates();
                humanAI.AddStates();
            }

            public override void InitializeAI()
            {                
                global::HumanNPC humanNpc = GetEntity();

                UseAIDesign = false;

                base.InitializeAI();

                ThinkMode = AIThinkMode.Interval;
                thinkRate = 0.25f;
                PathFinder = new HumanPathFinder();
                ((HumanPathFinder)PathFinder).Init(humanNpc);
            }

            public override void Think(float delta)
            {
                if (!ConVar.AI.think || states == null)
                    return;

                lastThinkTime = Time.time;
                if (sleeping)
                    return;

                humanAI.UpdateNearbyFriendlies();
                humanAI.UpdateSenses();

                if (CurrentState != null)                
                    CurrentState.StateThink(delta); 

                if (CurrentState == null || CurrentState.CanLeave())
                {
                    float highest = 0f;
                    BasicAIState state = null;

                    foreach (BasicAIState value in states.Values)
                    {
                        if (value == null || !value.CanEnter())
                            continue;

                        float weight = value.GetWeight();
                        if (weight <= highest)
                            continue;

                        highest = weight;
                        state = value;
                    }

                    if (state != CurrentState)
                        SwitchToState(state, -1);
                }
            }

            internal void ClearStates()
            {
                if (states != null)
                    states.Clear();
            }

            internal BasicAIState GetState<T>() where T : BasicAIState
            {
                foreach(BasicAIState basicAIState in states.Values)
                {
                    if (basicAIState is T)
                        return basicAIState;
                }

                return null;
            }

            public class IdleState : BaseAIBrain<global::HumanNPC>.BasicAIState
            {
                private readonly HumanAI humanAI;

                public IdleState(HumanAI humanAI) : base(AIState.Idle)
                {
                    this.humanAI = humanAI;
                }

                public override float GetWeight() => 0.1f;

                public override void StateEnter()
                {
                    humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.SlowWalk);
                    base.StateEnter();
                }
            }

            public class WoundedState : BaseAIBrain<global::HumanNPC>.BasicAIState
            {
                private readonly HumanAI humanAI;
                private readonly float woundedDuration;
                private readonly float woundedRecoveryChance;

                private bool isIncapacitated;
                private Vector3 destination;

                public WoundedState(HumanAI humanAI, float woundedDuration, float woundedRecoveryChance) : base(AIState.Orbit)
                {
                    this.humanAI = humanAI;
                    this.woundedDuration = woundedDuration;
                    this.woundedRecoveryChance = woundedRecoveryChance;
                }

                public override float GetWeight() => humanAI.Entity.HasPlayerFlag(BasePlayer.PlayerFlags.Wounded) ? 100f : 0f;

                public override void StateEnter()
                {
                    base.StateEnter();

                    if (UnityEngine.Random.value > 0.5f)
                    {
                        humanAI.Entity.health = (float)UnityEngine.Random.Range(ConVar.Server.crawlingminhealth, ConVar.Server.crawlingmaxhealth);
                        humanAI.Entity.metabolism.bleeding.@value = 0f;
                        humanAI.Entity.healingWhileCrawling = 0f;
                        
                        isIncapacitated = false;
                        destination = humanAI.Entity.ServerPosition;
                        humanAI.Entity.NavAgent.speed = BasePlayer.crawlSpeed * 0.75f;
                    }
                    else
                    {
                        humanAI.Entity.health = UnityEngine.Random.Range(2f, 6f);
                        humanAI.Entity.metabolism.bleeding.@value = 0f;
                        humanAI.Entity.healingWhileCrawling = 0f;
                        humanAI.Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Incapacitated, true);
                        isIncapacitated = true;
                    }

                    humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.Crouch);

                    humanAI.Entity.SetServerFall(true);
                    humanAI.Entity.SendNetworkUpdateImmediate(false);
                }

                public override void StateLeave()
                {
                    base.StateLeave();

                    humanAI.Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
                    humanAI.Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Incapacitated, false);
                    humanAI.Entity.SetServerFall(false);
                }

                public override StateStatus StateThink(float delta)
                {
                    base.StateThink(delta);

                    if (!humanAI.Entity.IsDead())
                    {
                        if (!isIncapacitated)
                        {
                            if (Vector3.Distance(humanAI.transform.position, destination) < 1f)
                            {
                                Vector2 random = UnityEngine.Random.insideUnitCircle.normalized * 25f;
                                object d = FindPointOnNavmesh(humanAI.transform.position + new Vector3(random.x, 0f, random.y), 4f);

                                if (d is Vector3)
                                {
                                    destination = (Vector3)d;
                                    humanAI.SetDestination(destination);
                                }
                            }

                            Vector3 dest = humanAI.Transform.position;
                            if (humanAI.Entity.HasPath)                            
                                dest = humanAI.Entity.NavAgent.nextPosition;

                            humanAI.Entity.ServerPosition = dest;
                            humanAI.Entity.SetAimDirection(humanAI.Entity.GetAimDirection());
                        }

                        if (TimeInState >= woundedDuration)
                        {
                            if (UnityEngine.Random.Range(0, 100) >= woundedRecoveryChance)
                                humanAI.Die();
                            else
                            {
                                humanAI.Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
                                humanAI.EquipWeapon();
                            }

                            return StateStatus.Finished;
                        }
                    }

                    return StateStatus.Running;
                }
            }

            public class ChaseState : BaseAIBrain<global::HumanNPC>.BasicAIState
            {
                private readonly HumanAI humanAI;
                private readonly float chaseRange;

                public ChaseState(HumanAI humanAI, float chaseRange) : base(AIState.Chase)
                {
                    this.humanAI = humanAI;
                    this.chaseRange = chaseRange;
                }

                public override float GetWeight()
                {
                    float weight = 0f;

                    if (!humanAI.Entity.HasTarget())
                        return 0f;

                    if (humanAI.HasHomePosition && humanAI.DistanceFromBase > chaseRange)
                        return 0f;

                    if ((humanAI.Entity.currentTarget as BasePlayer)?.IsFlying ?? false)
                    {
                        humanAI.ForgetEntity(humanAI.Entity.currentTarget);
                        humanAI.Entity.currentTarget = null;
                        humanAI.Entity.currentTargetLOS = false;
                        return 0f;
                    }

                    if (humanAI._attackEntity is BaseProjectile && (humanAI.Entity.AmmoFractionRemaining() < 0.3f || humanAI.Entity.IsReloading()))
                        weight -= 1f;

                    if (humanAI.Entity.HasTarget())
                        weight += 0.5f;

                    if (humanAI.Entity.CanSeeTarget())
                        weight -= 0.5f;
                    else weight += 1f;

                    if (!(humanAI._attackEntity is BaseProjectile) || humanAI.Entity.DistanceToTarget() > humanAI.Entity.GetIdealDistanceFromTarget())
                        weight += 1f;

                    return weight;
                }

                public override void StateEnter()
                {
                    base.StateEnter();
                    humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.Walk);
                }

                public override StateStatus StateThink(float delta)
                {
                    base.StateThink(delta);

                    if (!humanAI.Entity.HasTarget())
                        return StateStatus.Error;

                    bool hasProjectileWeapon = humanAI._attackEntity is BaseProjectile;

                    float distanceToTarget = Vector3.Distance(humanAI.Entity.currentTarget.transform.position, humanAI.Transform.position);

                    if (!hasProjectileWeapon)
                        humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.Sprint);

                    else humanAI.Entity.SetDesiredSpeed(distanceToTarget < 5f ? global::HumanNPC.SpeedType.SlowWalk :
                                                distanceToTarget >= 10f ? global::HumanNPC.SpeedType.Sprint :
                                                global::HumanNPC.SpeedType.Walk);

                    Vector3 position = humanAI.Transform.position;

                    if (!hasProjectileWeapon)
                        humanAI.SetDestination(humanAI.Entity.currentTarget.transform.position);
                    else
                    {
                        AIInformationZone aiInformationZone = humanAI.Entity.GetInformationZone(humanAI.Entity.currentTarget.transform.position);
                        if (aiInformationZone != null)
                        {
                            AIMovePoint bestMovePointNear = aiInformationZone.GetBestMovePointNear(humanAI.Entity.currentTarget.transform.position, humanAI.Transform.position, 0f, 35f, true, null);
                            if (!bestMovePointNear)
                            {
                                position = brain.PathFinder.GetRandomPositionAround(humanAI.Entity.currentTarget.transform.position, 1f, 2f);
                            }
                            else
                            {
                                bestMovePointNear.SetUsedBy(humanAI.Entity, 5f);
                                position = brain.PathFinder.GetRandomPositionAround(bestMovePointNear.transform.position, 0f, bestMovePointNear.radius - 0.3f);
                            }
                        }
                        else position = brain.PathFinder.GetRandomPositionAround(humanAI.Entity.currentTarget.transform.position, 10f, 20f);

                        humanAI.SetDestination(position);
                    }

                    return StateStatus.Running;
                }
            }

            public class CombatState : BaseAIBrain<global::HumanNPC>.BasicAIState
            {
                private readonly HumanAI humanAI;
                private readonly float chaseRange;

                private float nextStrafeTime;

                public CombatState(HumanAI humanAI, float chaseRange) : base(AIState.Combat)
                {
                    this.humanAI = humanAI;
                    this.chaseRange = chaseRange;
                }

                public override float GetWeight()
                {
                    if (!humanAI.Entity.HasTarget())
                        return 0f;

                    if (!humanAI.Entity.TargetInRange())
                        return 0f;

                    if (humanAI.HasHomePosition && humanAI.DistanceFromBase > chaseRange)
                        return 0f;

                    if (humanAI._attackEntity == null)
                        return 0f;

                    if ((humanAI.Entity.currentTarget as BasePlayer)?.IsFlying ?? false)
                    {
                        humanAI.ForgetEntity(humanAI.Entity.currentTarget);
                        humanAI.Entity.currentTarget = null;
                        humanAI.Entity.currentTargetLOS = false;
                        return 0f;
                    }

                    float weight = 0f;
                    if (humanAI._attackEntity is BaseProjectile)
                    {
                        float single = 1f - Mathf.InverseLerp(humanAI.Entity.GetIdealDistanceFromTarget(), humanAI.Entity.EngagementRange(), humanAI.Entity.DistanceToTarget());

                        weight = 0.5f * single;

                        if (humanAI.Entity.CanSeeTarget())
                            weight += 1f;
                    }
                    else
                    {
                        if (Vector3.Distance(humanAI.Transform.position, humanAI.Entity.currentTarget.transform.position) < humanAI._attackEntity.effectiveRange)
                            weight = 5f;
                    }

                    return weight;
                }

                public override void StateEnter()
                {
                    base.StateEnter();

                    brain.mainInterestPoint = humanAI.Transform.position;
                    humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.Walk);

                    if (humanAI._attackEntity is BaseMelee)
                        DoMeleeAttack();
                }

                public override void StateLeave()
                {
                    humanAI.Entity.SetDucked(false);
                    base.StateLeave();
                }

                public override StateStatus StateThink(float delta)
                {
                    base.StateThink(delta);

                    if (!humanAI.Entity.HasTarget())
                        return StateStatus.Error;

                    if (humanAI._attackEntity is BaseProjectile)
                    {
                        if (Time.time > nextStrafeTime)
                        {
                            if (UnityEngine.Random.Range(0, 3) == 1)
                            {
                                nextStrafeTime = Time.time + UnityEngine.Random.Range(2f, 3f);
                                humanAI.Entity.SetDucked(true);
                                humanAI.Entity.Stop();
                                return StateStatus.Running;
                            }

                            nextStrafeTime = Time.time + UnityEngine.Random.Range(3f, 4f);
                            humanAI.Entity.SetDucked(false);
                            humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.Walk);
                            humanAI.SetDestination(brain.PathFinder.GetRandomPositionAround(brain.mainInterestPoint, 1f, 2f));
                        }
                    }
                    else if (humanAI._attackEntity is BaseMelee)
                    {
                        humanAI.Entity.nextTriggerTime = Time.time + 30f;

                        if (Vector3.Distance(humanAI.Transform.position, humanAI.Entity.currentTarget.transform.position) < humanAI._attackEntity.effectiveRange)
                            DoMeleeAttack();
                        else
                        {
                            humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.Sprint);
                            humanAI.SetDestination(humanAI.Entity.currentTarget.transform.position);
                        }
                    }

                    return StateStatus.Running;
                }

                private void DoMeleeAttack() // Hackery to make ScientistNPC's do melee damage
                {
                    if (humanAI._attackEntity == null || !(humanAI._attackEntity is BaseMelee))
                        return;

                    BaseMelee baseMelee = humanAI._attackEntity as BaseMelee;
                    if (baseMelee.HasAttackCooldown())
                        return;

                    baseMelee.StartAttackCooldown(baseMelee.repeatDelay * 2f);
                    humanAI.Entity.SignalBroadcast(BaseEntity.Signal.Attack, string.Empty, null);

                    if (baseMelee.swingEffect.isValid)
                        Effect.server.Run(baseMelee.swingEffect.resourcePath, baseMelee.transform.position, Vector3.forward, humanAI.Entity.net.connection, false);

                    DoMeleeDamage(humanAI._attackEntity as BaseMelee);
                }

                private void DoMeleeDamage(BaseMelee baseMelee)
                {
                    Vector3 position = humanAI.Entity.eyes.position;
                    Vector3 forward = humanAI.Entity.eyes.BodyForward();

                    for (int i = 0; i < 2; i++)
                    {
                        List<RaycastHit> list = Pool.GetList<RaycastHit>();

                        GamePhysics.TraceAll(new Ray(position - (forward * (i == 0 ? 0f : 0.2f)), forward), (i == 0 ? 0f : baseMelee.attackRadius), list, baseMelee.effectiveRange + 0.2f, 1219701521, QueryTriggerInteraction.UseGlobal);

                        bool hasHit = false;
                        for (int j = 0; j < list.Count; j++)
                        {
                            RaycastHit raycastHit = list[j];
                            BaseEntity hitEntity = raycastHit.GetEntity();

                            if (hitEntity != null && hitEntity != humanAI.Entity && !hitEntity.EqualNetID(humanAI.Entity) && !(hitEntity is ScientistNPC))
                            {
                                float damageAmount = 0f;
                                foreach (DamageTypeEntry damageType in baseMelee.damageTypes)
                                    damageAmount += damageType.amount;

                                hitEntity.OnAttacked(new HitInfo(humanAI.Entity, hitEntity, DamageType.Slash, damageAmount * baseMelee.npcDamageScale));

                                HitInfo hitInfo = Pool.Get<HitInfo>();
                                hitInfo.HitEntity = hitEntity;
                                hitInfo.HitPositionWorld = raycastHit.point;
                                hitInfo.HitNormalWorld = -forward;

                                if (hitEntity is BaseNpc || hitEntity is BasePlayer)
                                    hitInfo.HitMaterial = StringPool.Get("Flesh");
                                else hitInfo.HitMaterial = StringPool.Get((raycastHit.GetCollider().sharedMaterial != null ? raycastHit.GetCollider().sharedMaterial.GetName() : "generic"));

                                Effect.server.ImpactEffect(hitInfo);
                                Pool.Free(ref hitInfo);

                                hasHit = true;

                                if (hitEntity.ShouldBlockProjectiles())
                                    break;
                            }
                        }

                        Pool.FreeList(ref list);
                        if (hasHit)
                            break;
                    }
                }
            }

            public class RoamState : BaseAIBrain<global::HumanNPC>.BasicAIState
            {
                private readonly HumanAI humanAI;

                private readonly float roamRange;

                private float nextSetDestinationTime;

                private float currentDestinationTime;

                private bool isAtDestination;

                private Vector3 roamDestination;

                public RoamState(HumanAI humanAI, float roamRange) : base (AIState.Roam)
                {
                    this.humanAI = humanAI;
                    this.roamRange = roamRange;
                }

                public override float GetWeight()
                {
                    if (!humanAI.Entity.HasTarget() && humanAI.Entity.SecondsSinceAttacked > 10f)
                        return 5f;

                    return 0f;
                }

                public override void StateEnter()
                {
                    humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.SlowWalk);
                    humanAI.Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Relaxed, true);

                    humanAI.Entity.IsDormant = false;

                    roamDestination = humanAI.Transform.position;
                    nextSetDestinationTime = Time.time;
                    isAtDestination = true;

                    base.StateEnter();
                }

                public override void StateLeave()
                {
                    humanAI.Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Relaxed, false);
                    base.StateLeave();
                }

                public override StateStatus StateThink(float delta)
                {
                    base.StateThink(delta);

                    if (humanAI.DistanceFromBase > roamRange)
                    {
                        roamDestination = humanAI._roamBasePosition;
                        humanAI.SetDestination(roamDestination);

                        currentDestinationTime = 0;
                        isAtDestination = false;

                        humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.Sprint);
                    }
                    else
                    {
                        if (Vector3.Distance(humanAI.Transform.position, roamDestination) < 3f)
                        {
                            if (!isAtDestination)
                            {
                                nextSetDestinationTime = Time.time + UnityEngine.Random.Range(2f, 4f);
                                isAtDestination = true;

                                humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.SlowWalk);
                                return StateStatus.Running;
                            }

                            if (Time.time > nextSetDestinationTime && isAtDestination)
                            {
                                Vector2 random = UnityEngine.Random.insideUnitCircle * (roamRange - 5f);

                                roamDestination = humanAI._roamBasePosition + new Vector3(random.x, 0f, random.y);
                                roamDestination.y = TerrainMeta.HeightMap.GetHeight(roamDestination);

                                if (NavMesh.SamplePosition(roamDestination, out navmeshHit, 5f, humanAI.Entity.NavAgent.areaMask))
                                    roamDestination = navmeshHit.position;

                                humanAI.SetDestination(roamDestination);

                                isAtDestination = false;
                                currentDestinationTime = 0;
                            }
                        }
                        else
                        {
                            currentDestinationTime += delta;
                            if (currentDestinationTime > 30f)
                            {
                                isAtDestination = true;
                                roamDestination = humanAI.transform.position;
                            }

                            humanAI.SetDestination(roamDestination);
                        }
                    }

                    return StateStatus.Running;
                }
            }

            public class MountedState : BaseAIBrain<global::HumanNPC>.BasicAIState
            {
                private readonly HumanAI humanAI;
                private readonly bool canUseWeapon;

                public MountedState(HumanAI humanAI, bool canUseWeapon) : base(AIState.Mounted)
                {
                    this.humanAI = humanAI;
                    this.canUseWeapon = canUseWeapon;
                }

                public override float GetWeight()
                {
                    if (humanAI.Entity.isMounted)
                        return 400f;
                    return 0f;
                }

                public override void StateEnter()
                {
                    humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.Walk);

                    if (!canUseWeapon)
                        humanAI.HolsterWeapon();

                    humanAI.DisableNavAgent();

                    base.StateEnter();
                }

                public override void StateLeave()
                {
                    humanAI.EnableNavAgent();

                    humanAI.EquipWeapon();

                    base.StateLeave();
                }
            }

            public class MoveDestinationState : BaseAIBrain<global::HumanNPC>.BasicAIState
            {
                private readonly HumanAI humanAI;

                private Vector3 destination;
                private global::HumanNPC.SpeedType speed;

                private Action onReachedDestination;

                private bool hasDestination;

                public MoveDestinationState(HumanAI humanAI) : base (AIState.MoveTowards)
                {
                    this.humanAI = humanAI;
                }

                public void SetDestination(Vector3 destination, global::HumanNPC.SpeedType speed, Action onReachedDestination)
                {
                    if (NavMesh.SamplePosition(destination, out navmeshHit, 5f, humanAI.Entity.NavAgent.areaMask))
                        destination = navmeshHit.position;

                    this.destination = destination;
                    this.speed = speed;
                    this.onReachedDestination = onReachedDestination;

                    hasDestination = true;
                }

                public override float GetWeight()
                {
                    if (hasDestination)
                        return 300f;
                    return 0f;
                }

                public override void StateEnter()
                {
                    humanAI.Entity.SetDesiredSpeed(speed);
                    humanAI.SetDestination(destination);

                    base.StateEnter();
                }

                public override void StateLeave()
                {
                    humanAI.EnableNavAgent();

                    humanAI.EquipWeapon();

                    base.StateLeave();
                }

                public override StateStatus StateThink(float delta)
                {
                    base.StateThink(delta);

                    if (Vector3.Distance(humanAI.Transform.position, destination) < 3f)
                    {
                        if (onReachedDestination != null)
                        {
                            onReachedDestination.Invoke();
                            onReachedDestination = null;
                        }

                        humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.Walk);

                        hasDestination = false;

                        return StateStatus.Finished;
                    }
                    else
                    {
                        humanAI.Entity.SetDesiredSpeed(speed);
                        humanAI.SetDestination(destination);

                        return StateStatus.Running;
                    }
                }
            }

            public class FallingState : BaseAIBrain<global::HumanNPC>.BasicAIState
            {
                private readonly HumanAI humanAI;

                public FallingState(HumanAI humanAI) : base (AIState.Land)
                {
                    this.humanAI = humanAI;
                }

                public override float GetWeight()
                {
                    if (humanAI.Entity.modelState.flying)
                        return 500f;

                    return 0f;
                }

                public override void StateEnter()
                {
                    humanAI.Entity.SetDesiredSpeed(global::HumanNPC.SpeedType.SlowWalk);

                    humanAI.DisableNavAgent();
                    humanAI.HolsterWeapon();

                    base.StateEnter();
                }

                public override void StateLeave()
                {
                    base.StateLeave();

                    humanAI.EnableNavAgent();
                    humanAI.EquipWeapon();
                }
            }
        }
        #endregion

        #region Testing
        [ChatCommand("spawnnpc")]
        private void cmdSpawnNPC(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            int amount = 3;
            if (args.Length > 0)
                int.TryParse(args[0], out amount);

            for (int i = 0; i < amount; i++)
            {
                Vector3 position = player.transform.position + (UnityEngine.Random.onUnitSphere * 20);

                JObject settings = new JObject()
                {
                    ["DisplayName"] = "ScientistNPC",
                    ["Health"] = 100,
                    ["NPCType"] = "HeavyScientist",
                    ["SightRange"] = 60,
                    ["RoamRange"] = 50f,
                    ["ChaseRange"] = 90f,
                    ["EnableNavMesh"] = true,
                    ["EquipWeapon"] = true,
                    ["Kit"] = string.Empty,
                    ["StripCorpseLoot"] = false,
                    ["KillInSafeZone"] = true,
                    ["DespawnTime"] = 0,
                    ["StartDead"] = false,
                    ["StartWounded"] = true,
                    ["WoundedDuration"] = 180,
                    ["WoundedRecoveryChance"] = 10,
                    ["States"] = new JArray { "IdleState", "RoamState", "ChaseState", "CombatState" }
                };

                SpawnNPC(this, position, settings);
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
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
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion       
    }
}
