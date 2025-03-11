using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Rust;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PilotEject", "k1lly0u", "3.1.2")]
    [Description("A mini event where a helicopter malfunctions and the pilot has to eject")]
    class PilotEject : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin CustomNPC, Kits, HeliRefuel;

        private List<ScientistNPC> scientistNPCs = new List<ScientistNPC>();

        private Hash<ulong, ItemContainer[]> scientistInventory = new Hash<ulong, ItemContainer[]>();

        public static PilotEject Instance { get; private set; }

        private const string ADMIN_PERM = "piloteject.admin";

        private const string HELICOPTER_PREFAB = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
        private const string PARACHUTE_PREFAB = "assets/prefabs/misc/parachute/parachute.prefab";
        private const string SMOKE_EFFECT = "assets/bundled/prefabs/fx/smoke_signal_full.prefab";

        private const int LAND_LAYERS = 1 << 4 | 1 << 8 | 1 << 16 | 1 << 21 | 1 << 23;
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            Instance = this;

            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission(ADMIN_PERM, this);
        }

        private void OnServerInitialized()
        {
            if (configData.NPC.LootType == "Default")
            {
                Unsubscribe(nameof(OnCorpsePopulate));
                Unsubscribe(nameof(CanPopulateLoot));
            }

            if (configData.Automation.AutoSpawn)
                RunAutomatedEvent();
        }

        private void OnEntitySpawned(BaseHelicopter baseHelicopter)
        {
            timer.In(1f, () =>
            {
                if (baseHelicopter == null || baseHelicopter.GetComponent<EjectionComponent>())
                    return;

                if (HeliRefuel && (bool)HeliRefuel.Call("IsRefuelHelicopter", baseHelicopter))
                    return;

                if (Mathf.Approximately(configData.Automation.Chance, 100) || Random.Range(0, 100) < configData.Automation.Chance)
                    baseHelicopter.gameObject.AddComponent<EjectionComponent>();
            });
        }

        private void OnEntityTakeDamage(BaseHelicopter baseHelicopter, HitInfo hitInfo)
        {
            if (baseHelicopter == null || hitInfo == null)
                return;

            EjectionComponent refuelHelicopter = baseHelicopter.GetComponent<EjectionComponent>();
            if (refuelHelicopter != null)
                refuelHelicopter.OnTakeDamage(hitInfo); 
        }

        private void OnEntityKill(BaseHelicopter baseHelicopter)
        {
            if (baseHelicopter == null)
                return;

            EjectionComponent ejectionHelictoper = baseHelicopter.GetComponent<EjectionComponent>();
            if (ejectionHelictoper != null)
            {
                if (!ejectionHelictoper.hasEjected)
                {
                    if (configData.Ejection.EjectOnDeath)
                        ejectionHelictoper.EjectPilot();
                    return;
                }

                UnityEngine.Object.Destroy(ejectionHelictoper);
            }

            for (int i = 0; i < ParachutePhysics._allParachutes.Count; i++)
            {
                ParachutePhysics parachutePhysics = ParachutePhysics._allParachutes[i];
                if (parachutePhysics != null && parachutePhysics.Helicopter == baseHelicopter)                
                    parachutePhysics.crashSite = baseHelicopter.transform.position;                
            }
        }

        private void OnEntityKill(ScientistNPC scientist) => scientistNPCs.Remove(scientist);


        private void OnHelicopterRetire(PatrolHelicopterAI patrolHelicopterAI) => UnityEngine.Object.Destroy(patrolHelicopterAI?.helicopterBase?.GetComponent<EjectionComponent>());

        private object CanBeTargeted(BaseCombatEntity player, MonoBehaviour behaviour)
        {
            ParachutePhysics npcParachute = player?.GetComponent<ParachutePhysics>();
            if (npcParachute != null)
            {
                if (((behaviour is AutoTurret) || (behaviour is GunTrap) || (behaviour is FlameTurret)) && configData.NPC.TargetedByTurrets)
                    return null;
                return false;
            }

            return null;
        }

        private void OnEntityDeath(ScientistNPC scientistNpc, HitInfo hitInfo)
        {
            if (scientistNpc == null || !scientistNPCs.Contains(scientistNpc))
                return;

            if (configData.NPC.LootType == "Inventory")
                StoreInventory(scientistNpc);

            if (hitInfo.InitiatorPlayer != null && configData.Notifications.NPCDeath)
                Broadcast("Notification.PilotKilled", hitInfo.InitiatorPlayer.displayName);

        }

        private object CanPopulateLoot(ScientistNPC scientistNpc, NPCPlayerCorpse corpse) => scientistNPCs.Contains(scientistNpc) ? (object)false : null;

        private object OnCorpsePopulate(ScientistNPC scientistNpc, NPCPlayerCorpse corpse)
        {
            if (scientistNpc == null || !scientistNPCs.Contains(scientistNpc))
                return null;

            if (configData.NPC.LootType == "Inventory")
            {
                MoveInventoryTo(scientistNpc.userID, corpse);
                return corpse;
            }

            PopulateLoot(corpse.containers[0], configData.NPC.RandomItems);
            return corpse;
        }

        private object CanLootPlayer(ScientistNPC scientistNpc, BasePlayer player)
        {
            if (scientistNPCs.Contains(scientistNpc) && scientistNpc.IsWounded())
                return false;
            return null;
        }

        private object OnPlayerAssist(ScientistNPC scientistNpc, BasePlayer player)
        {
            if (scientistNPCs.Contains(scientistNpc))
                return false;
            return null;
        }

        private void Unload()
        {
            for (int i = 0; i < EjectionComponent.allHelicopters.Count; i++)
                UnityEngine.Object.Destroy(EjectionComponent.allHelicopters[i]);

            for (int i = 0; i < ParachutePhysics._allParachutes.Count; i++)
                UnityEngine.Object.Destroy(ParachutePhysics._allParachutes[i]);

            configData = null;
            Instance = null;
        }
        #endregion

        #region Functions
        private void RunAutomatedEvent()
        {
            timer.In(Random.Range(configData.Automation.Min, configData.Automation.Max), () =>
            {
                if (BasePlayer.activePlayerList.Count >= configData.Automation.RequiredPlayers)
                    SpawnEntity();

                RunAutomatedEvent();
            });
        }

        private EjectionComponent SpawnEntity()
        {
            BaseHelicopter baseHelicopter = GameManager.server.CreateEntity(HELICOPTER_PREFAB) as BaseHelicopter;
            baseHelicopter.enableSaving = false;
            baseHelicopter.Spawn();

            return baseHelicopter.gameObject.AddComponent<EjectionComponent>(); 
        }

        private static string GetGridString(Vector3 position)
        {
            Vector2 adjPosition = new Vector2((World.Size / 2) + position.x, (World.Size / 2) - position.z);
            return $"{NumberToString((int)(adjPosition.x / 145))}{((int)(adjPosition.y / 145)) - 1}";
        }

        private static string NumberToString(int number)
        {
            bool a = number > 26;
            System.Char c = (System.Char)(65 + (a ? number - 26 : number));
            return a ? "A" + c : c.ToString();
        }

        private static void StripInventory(BasePlayer player)
        {
            Item[] allItems = player.inventory.AllItems();

            for (int i = allItems.Length - 1; i >= 0; i--)
            {
                Item item = allItems[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }

        private static void ClearContainer(ItemContainer container)
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

        private static void PopulateLoot(ItemContainer container, ConfigData.LootContainer loot)
        {
            if (container == null || loot == null)
                return;

            ClearContainer(container);

            int amount = Random.Range(loot.Minimum, loot.Maximum);

            List<ConfigData.LootItem> list = Pool.GetList<ConfigData.LootItem>();
            list.AddRange(loot.Items);

            int itemCount = 0;
            while (itemCount < amount)
            {
                int totalWeight = list.Sum((ConfigData.LootItem x) => Mathf.Max(1, x.Weight));
                int random = Random.Range(0, totalWeight);

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    ConfigData.LootItem lootItem = list[i];

                    totalWeight -= Mathf.Max(1, lootItem.Weight);

                    if (random >= totalWeight)
                    {
                        list.Remove(lootItem);

                        Item item = ItemManager.CreateByName(lootItem.Name, Random.Range(lootItem.Minimum, lootItem.Maximum), lootItem.Skin);
                        item?.MoveToContainer(container);

                        itemCount++;
                        break;
                    }
                }

                if (list.Count == 0)
                    list.AddRange(loot.Items);
            }

            Pool.FreeList(ref list);            
        }
        #endregion

        #region Inventory Copy
        private void StoreInventory(ScientistNPC scientistNpc)
        {
            ItemContainer[] source = new ItemContainer[] { scientistNpc.inventory.containerMain, scientistNpc.inventory.containerWear, scientistNpc.inventory.containerBelt };

            ItemContainer[] containers = new ItemContainer[3];

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
                        if (!item.MoveToContainer(containers[i], -1, true))
                            item.Remove(0f);
                    }
                }
            }

            scientistInventory[scientistNpc.userID] = containers;
        }

        private void MoveInventoryTo(ulong scientistId, LootableCorpse corpse)
        {
            ItemContainer[] containers;
            if (scientistInventory.TryGetValue(scientistId, out containers))
            {
                for (int i = 0; i < containers.Length; i++)
                {
                    Item[] array = containers[i].itemList.ToArray();
                    for (int j = 0; j < array.Length; j++)
                    {
                        Item item = array[j];
                        if (!item.MoveToContainer(corpse.containers[i], -1, true))
                        {
                            item.Remove(0f);
                        }
                    }

                    containers[i].Kill();
                }

                scientistInventory.Remove(scientistId);

                corpse.ResetRemovalTime();
            }
        }
        #endregion

        #region Component        
        private class EjectionComponent : MonoBehaviour
        {
            internal static List<EjectionComponent> allHelicopters = new List<EjectionComponent>();

            internal BaseHelicopter Helicopter { get; private set; }

            internal PatrolHelicopterAI AI { get; private set; }
            
            private float actualHealth;

            internal bool ejectOverride = false;

            internal bool hasEjected = false;

            private void Awake()
            {
                allHelicopters.Add(this);

                Helicopter = GetComponent<BaseHelicopter>();
                AI = Helicopter.myAI;

                if (configData.Helicopter.DamageEffects)
                {
                    BaseHelicopter.weakspot weakspot = Helicopter.weakspots[1];
                    weakspot.healthFractionOnDestroyed = 0f;
                    weakspot.Hurt(weakspot.health, null);
                }

                actualHealth = Helicopter.health = configData.Helicopter.Health;
                Helicopter.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                if (configData.Ejection.EjectRandom)
                    InvokeHandler.Invoke(this, TryAutoEjection, Random.Range(configData.Ejection.Min, configData.Ejection.Max));
            }

            private void OnDestroy() => allHelicopters.Remove(this);

            internal void OnTakeDamage(HitInfo hitInfo)
            {
                if (!configData.Ejection.EjectOnKilled || hasEjected)
                    return;

                actualHealth -= hitInfo.damageTypes.Total();

                if (actualHealth < 0f)
                    EjectPilot();                
            }

            private void TryAutoEjection()
            {
                if (TerrainMeta.HeightMap.GetHeight(transform.position) < 0 || BasePlayer.activePlayerList.Count < configData.Automation.RequiredPlayers)
                {
                    InvokeHandler.Invoke(this, TryAutoEjection, Random.Range(30, 60));
                    return;
                }

                EjectPilot();
            }

            internal void EjectPilot()
            {
                if ((BasePlayer.activePlayerList.Count < configData.Automation.RequiredPlayers && !ejectOverride) || hasEjected)
                {
                    Destroy(this);
                    return;
                }

                InvokeHandler.CancelInvoke(this, EjectPilot);

                if (Instance.CustomNPC)
                    ServerMgr.Instance.StartCoroutine(SpawnNPCs(Helicopter));
                else Debug.LogWarning($"[PilotEject] - PilotEject requires the 'CustomNPC' plugin available at https://chaoscode.io to spawn NPC's!");

                DropLoot();

                hasEjected = true;

                if (actualHealth < 0f)
                {
                    if (configData.Notifications.Death)
                        Broadcast("Notification.OnDeath", GetGridString(transform.position));
                }
                else
                {
                    if (configData.Notifications.Malfunction)
                        Broadcast("Notification.Malfunction", GetGridString(transform.position));
                }

                if (!AI.isDead)
                    AI.CriticalDamage();

                Destroy(this);
            }

            private IEnumerator SpawnNPCs(BaseHelicopter baseHelicopter)
            {                
                Vector3 position = transform.position + (transform.up + (transform.forward * 2f));
                Quaternion rotation = transform.rotation;

                for (int i = 0; i < configData.NPC.Amount; i++)
                {
                    JObject settings = new JObject()
                    {
                        ["DisplayName"] = configData.NPC.Names?.Length > 0 ? configData.NPC.Names.GetRandom() : "ScientistNPC",
                        ["Health"] = configData.NPC.Health,
                        ["NPCType"] = "Scientist",
                        ["SightRange"] = configData.NPC.SightRange,
                        ["RoamRange"] = 25f,
                        ["ChaseRange"] = 90f,
                        ["Kit"] = configData.NPC.Kits.Length > 0 ? configData.NPC.Kits.GetRandom() : string.Empty,
                        ["KillInSafeZone"] = true,
                        ["DespawnTime"] = configData.NPC.Lifetime,
                        ["StartWounded"] = Random.Range(0, 100) <= configData.NPC.WoundedChance,
                        ["WoundedDuration"] = Random.Range(300f, 600f),
                        ["WoundedRecoveryChance"] = configData.NPC.RecoveryChance,
                        ["EnableNavMesh"] = false,
                        ["EquipWeapon"] = false,
                        ["States"] = new JArray { "IdleState", "RoamState", "ChaseState", "CombatState", "FallingState" }
                    };

                    ScientistNPC scientistNPC = Instance.CustomNPC?.Call<ScientistNPC>("SpawnNPC", Instance, position, settings);
                    if (scientistNPC != null)
                    {
                        Instance.scientistNPCs.Add(scientistNPC);
                        scientistNPC.gameObject.AddComponent<ParachutePhysics>().Helicopter = baseHelicopter;
                    }
                    
                    yield return CoroutineEx.waitForSeconds(0.5f);
                }
            }

            private void DropLoot()
            {
                if (configData.LootBoxes.Amount <= 0)
                    return;

                for (int i = 0; i < configData.LootBoxes.Amount; i++)
                {
                    LootContainer container = GameManager.server.CreateEntity(Helicopter.crateToDrop.resourcePath, transform.position) as LootContainer;
                    container.enableSaving = false;
                    container.Spawn();

                    Vector3 velocity = Random.onUnitSphere;
                    velocity.y = 1;

                    Rigidbody rb = container.gameObject.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.mass = 2f;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.angularVelocity = Vector3Ex.Range(-1.75f, 1.75f);
                    rb.drag = 0.5f * (rb.mass / 5f);
                    rb.angularDrag = 0.2f * (rb.mass / 5f);

                    rb.AddForce(velocity * 5f, ForceMode.Impulse);

                    ClearContainer(container.inventory);
                    PopulateLoot(container.inventory, configData.LootBoxes.RandomItems);
                }
            }
        }

        internal class ParachutePhysics : MonoBehaviour
        {
            internal static List<ParachutePhysics> _allParachutes = new List<ParachutePhysics>();

            internal ScientistNPC Entity { get; private set; }

            internal BaseHelicopter Helicopter { get; set; }

            private Transform tr;

            private Rigidbody rb;

            private BaseEntity parachute;

            private Vector3 currentWindVector = Vector3.zero;

            internal Vector3 crashSite;

            private bool isFalling;

            private bool wasWounded = false;

            private Vector3 DirectionTowardsCrash2D
            {
                get
                {
                    if (Helicopter != null && !Helicopter.IsDestroyed)
                        return (Helicopter.transform.position.XZ3D() - tr.position.XZ3D()).normalized;
                    return (crashSite.XZ3D() - tr.position.XZ3D()).normalized;
                }
            }

            private void Awake()
            {
                Entity = GetComponent<ScientistNPC>();

                tr = Entity.transform;

                InitializeVelocity();

                _allParachutes.Add(this);

                wasWounded = Entity.HasPlayerFlag(BasePlayer.PlayerFlags.Wounded);
                Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
            }

            private void OnDestroy()
            {
                _allParachutes.Remove(this);

                if (parachute != null && !parachute.IsDestroyed)
                    parachute.Kill();
            }

            private void Update()
            {
                Entity.modelState.onground = false;
                Entity.modelState.flying = true;

                if (!isFalling)
                    return;
                else
                {
                    if (Physics.Raycast(tr.position, Vector3.down, 0.5f, LAND_LAYERS))
                    {
                        enabled = false;
                        isFalling = false;

                        rb.useGravity = false;
                        rb.isKinematic = true;
                        rb.drag = 0;

                        Entity.modelState.onground = true;
                        Entity.modelState.flying = false;

                        if (parachute != null && !parachute.IsDestroyed)
                            parachute.Kill();

                        if (TerrainMeta.WaterMap.GetDepth(tr.position) > 0.6f)
                        {
                            Entity.Die(new HitInfo(null, Entity, DamageType.Drowned, 1000f));
                            return;
                        }

                        OnParachuteLand();
                    }                    
                }
            }

            private void FixedUpdate()
            {
                if (!isFalling && rb.velocity.y < 0)
                    DeployParachute();

                if (isFalling)
                {
                    Vector3 windAtPosition = Vector3.Lerp(DirectionTowardsCrash2D, GetWindAtCurrentPos(), Helicopter != null && !Helicopter.IsDestroyed ? 0.25f : 0.75f);

                    float heightFromGround = Mathf.Max(TerrainMeta.HeightMap.GetHeight(tr.position), TerrainMeta.WaterMap.GetHeight(tr.position));
                    float force = Mathf.InverseLerp(heightFromGround + 20f, heightFromGround + 60f, tr.position.y);

                    Vector3 normalizedDir = (windAtPosition.normalized * force) * configData.Ejection.Wind;

                    currentWindVector = Vector3.Lerp(currentWindVector, normalizedDir, Time.fixedDeltaTime * 0.25f);

                    rb.AddForceAtPosition(normalizedDir * 0.1f, tr.position, ForceMode.Force);
                    rb.AddForce(normalizedDir * 0.9f, ForceMode.Force);

                    Quaternion rotation = Quaternion.LookRotation(rb.velocity);
                    tr.rotation = Entity.eyes.rotation = Entity.ServerRotation = rotation;
                    Entity.viewAngles = rotation.eulerAngles;

                    parachute.transform.localRotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
                    parachute.SendNetworkUpdate();
                }
            }

            private void InitializeVelocity()
            {
                rb = Entity.GetComponent<Rigidbody>();
                rb.useGravity = true;
                rb.isKinematic = false;
                rb.AddForce((Vector3.up * 15) + (Random.onUnitSphere.XZ3D() * 5), ForceMode.Impulse);
            }

            private void DeployParachute()
            {
                parachute = GameManager.server.CreateEntity(PARACHUTE_PREFAB, tr.position);
                parachute.enableSaving = false;
                parachute.Spawn();

                parachute.SetParent(Entity, false);
                parachute.transform.localPosition = Vector3.up * 2f;
                parachute.transform.localRotation = Quaternion.Euler(0f, Entity.viewAngles.y, 0f);

                rb.drag = configData.Ejection.Drag;

                if (configData.Ejection.ShowSmoke)
                    Effect.server.Run(SMOKE_EFFECT, parachute, 0, Vector3.zero, Vector3.zero, null, true);

                Entity.Invoke(() =>
                {
                    Entity.modelState.onground = false;
                    Entity.modelState.flying = true;
                    Entity.SendNetworkUpdate();
                }, 1f);

                isFalling = true;
            }

            private Vector3 GetWindAtCurrentPos()
            {
                float single = tr.position.y * 6f;
                Vector3 force = new Vector3(Mathf.Sin(single * 0.0174532924f), 0f, Mathf.Cos(single * 0.0174532924f));
                return force.normalized * 1f;
            }

            private void OnParachuteLand()
            {
                Instance.CustomNPC?.Call("SetRoamHomePosition", Entity, Helicopter != null && !Helicopter.IsDestroyed ? Helicopter.transform.position : crashSite);

                if (wasWounded)
                    Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, true);

                Destroy(this);
            }
        }        
        #endregion

        #region Commands
        [ChatCommand("pe")]
        private void cmdPilotEject(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, ADMIN_PERM) && !player.IsAdmin)
                return;

            if (args.Length == 0)
            {
                SendReply(player, "/pe call - Call a PilotEject helicopter");
                SendReply(player, "/pe eject - Force eject any active pilots");
                return;
            }

            switch (args[0].ToLower())
            {
                case "call":
                    SpawnEntity();
                    SendReply(player, "Spawned PilotEject helicopter");
                    return;
                case "c":
                    if (player.net.connection.authLevel == 2)
                    {
                        EjectionComponent ejectionComponent = SpawnEntity();
                        timer.In(1f, () => ejectionComponent.Helicopter.transform.position = player.transform.position);
                    }
                    return;
                case "s":
                    if (player.net.connection.authLevel == 2)
                    {
                        for (int i = 0; i < EjectionComponent.allHelicopters.Count; i++)
                        {
                            EjectionComponent ejectionComponent = EjectionComponent.allHelicopters[i];
                            ejectionComponent.Helicopter.Die(new HitInfo(ejectionComponent.Helicopter, ejectionComponent.Helicopter, Rust.DamageType.Explosion, 100000));
                        }
                    }
                    return;
                case "eject":
                    int count = 0;
                    for (int i = 0; i < EjectionComponent.allHelicopters.Count; i++)
                    {
                        EjectionComponent ejectionComponent = EjectionComponent.allHelicopters[i];
                        if (!ejectionComponent.hasEjected)
                        {
                            ejectionComponent.ejectOverride = true;
                            ejectionComponent.EjectPilot();
                            count++;
                        }
                    }
                    SendReply(player, $"Ejected {count * configData.NPC.Amount} pilots from {count} helicopters");
                    return;

                default:
                    SendReply(player, "Invalid syntax!");
                    break;
            }
        }

        [ConsoleCommand("pe")]
        private void ccmdPilotEject(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                if (!permission.UserHasPermission(arg.Connection.userid.ToString(), ADMIN_PERM))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args.Length == 0)
            {
                SendReply(arg, "pe call - Call a PilotEject helicopter");
                SendReply(arg, "pe eject - Force eject any active pilots");
                return;
            }

            switch (arg.Args[0].ToLower())
            {
                case "call":
                    SpawnEntity();
                    return;                
                case "eject":
                    int count = 0;
                    for (int i = 0; i < EjectionComponent.allHelicopters.Count; i++)
                    {
                        EjectionComponent ejectionComponent = EjectionComponent.allHelicopters[i];
                        if (!ejectionComponent.hasEjected)
                        {
                            ejectionComponent.ejectOverride = true;
                            ejectionComponent.EjectPilot();
                            count++;
                        }
                    }
                    SendReply(arg, $"Ejected {count * configData.NPC.Amount} pilots from {count} helicopters");
                    return;

                default:
                    SendReply(arg, "Invalid syntax!");
                    break;
            }
        }
        #endregion

        #region Config        
        private static ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Event Automation")]
            public AutomationOptions Automation { get; set; }

            [JsonProperty(PropertyName = "Ejection Options")]
            public EjectionOptions Ejection { get; set; }

            [JsonProperty(PropertyName = "Helicopter Options")]
            public HelicopterOptions Helicopter { get; set; }

            [JsonProperty(PropertyName = "Notification Options")]
            public NotificationOptions Notifications { get; set; }

            [JsonProperty(PropertyName = "NPC Options")]
            public NPCOptions NPC { get; set; }

            [JsonProperty(PropertyName = "Loot Container Options")]
            public Loot LootBoxes { get; set; }

            public class AutomationOptions
            {
                [JsonProperty(PropertyName = "Automatically spawn helicopters on a timer")]
                public bool AutoSpawn { get; set; }

                [JsonProperty(PropertyName = "Auto-spawn time minimum (seconds)")]
                public float Min { get; set; }

                [JsonProperty(PropertyName = "Auto-spawn time maximum (seconds)")]
                public float Max { get; set; }

                [JsonProperty(PropertyName = "Minimum amount of online players to trigger the event")]
                public int RequiredPlayers { get; set; }

                [JsonProperty(PropertyName = "Chance of game spawned helicopter becoming a PilotEject helicopter (x / 100)")]
                public float Chance { get; set; }
            }

            public class EjectionOptions
            {
                [JsonProperty(PropertyName = "Eject the pilot when the helicopter has been shot down")]
                public bool EjectOnKilled { get; set; }

                [JsonProperty(PropertyName = "Eject the pilot when the helicopter has been destroyed mid-air")]
                public bool EjectOnDeath { get; set; }

                [JsonProperty(PropertyName = "Eject the pilot randomly")]
                public bool EjectRandom { get; set; }

                [JsonProperty(PropertyName = "Show smoke when parachuting")]
                public bool ShowSmoke { get; set; }

                [JsonProperty(PropertyName = "Random ejection time minimum (seconds)")]
                public float Min { get; set; }

                [JsonProperty(PropertyName = "Random ejection time maximum (seconds)")]
                public float Max { get; set; }

                [JsonProperty(PropertyName = "Parachute drag force")]
                public float Drag { get; set; }

                [JsonProperty(PropertyName = "Wind force")]
                public float Wind { get; set; }
            }

            public class HelicopterOptions
            {
                [JsonProperty(PropertyName = "Helicopter spawns with tail rotor on fire")]
                public bool DamageEffects { get; set; }

                [JsonProperty(PropertyName = "Start health")]
                public float Health { get; set; }
            }

            public class NotificationOptions
            {
                [JsonProperty(PropertyName = "Show notification when helicopter has been shot down")]
                public bool Death { get; set; }

                [JsonProperty(PropertyName = "Show notification when helicopter malfunctions")]
                public bool Malfunction { get; set; }

                [JsonProperty(PropertyName = "Show notification when a NPC has been killed")]
                public bool NPCDeath { get; set; }
            }

            public class NPCOptions
            {
                [JsonProperty(PropertyName = "Amount of NPCs to spawn")]
                public int Amount { get; set; }

                [JsonProperty(PropertyName = "NPC display name (chosen at random)")]
                public string[] Names { get; set; }

                [JsonProperty(PropertyName = "NPC kit (chosen at random)")]
                public string[] Kits { get; set; }

                [JsonProperty(PropertyName = "NPC health")]
                public int Health { get; set; }

                [JsonProperty(PropertyName = "Chance of being wounded when landing (x / 100)")]
                public int WoundedChance { get; set; }

                [JsonProperty(PropertyName = "Chance of recovery from being wounded(x / 100)")]
                public int RecoveryChance { get; set; }

                [JsonProperty(PropertyName = "Amount of time the NPCs will be alive before suiciding (seconds) (0 = disabled)")]
                public int Lifetime { get; set; }

                [JsonProperty(PropertyName = "Roam distance from landing position")]
                public float RoamDistance { get; set; }

                [JsonProperty(PropertyName = "Sight range")]
                public float SightRange { get; set; }

                [JsonProperty(PropertyName = "Loot type (Default, Inventory, Random)")]
                public string LootType { get; set; }

                [JsonProperty(PropertyName = "Random loot items")]
                public LootContainer RandomItems { get; set; }

                [JsonProperty(PropertyName = "Can be targeted by turrets")]
                public bool TargetedByTurrets { get; set; }
            }

            public class Loot
            {
                [JsonProperty(PropertyName = "Amount of loot boxes to drop when pilot ejects")]
                public int Amount { get; set; }

                [JsonProperty(PropertyName = "Loot container items")]
                public LootContainer RandomItems { get; set; }
            }

            public class LootContainer
            {
                [JsonProperty(PropertyName = "Minimum amount of items")]
                public int Minimum { get; set; }

                [JsonProperty(PropertyName = "Maximum amount of items")]
                public int Maximum { get; set; }

                [JsonProperty(PropertyName = "Items")]
                public LootItem[] Items { get; set; }
            }

            public class LootItem
            {
                [JsonProperty(PropertyName = "Item shortname")]
                public string Name { get; set; }

                [JsonProperty(PropertyName = "Item skin ID")]
                public ulong Skin { get; set; }

                [JsonProperty(PropertyName = "Minimum amount of item")]
                public int Minimum { get; set; }

                [JsonProperty(PropertyName = "Maximum amount of item")]
                public int Maximum { get; set; }

                [JsonProperty(PropertyName = "Item weight (a larger number has more chance of being selected)")]
                public int Weight { get; set; } = 1;
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
                Automation = new ConfigData.AutomationOptions
                {
                    AutoSpawn = false,
                    Chance = 100,
                    Min = 3600,
                    Max = 5400,
                    RequiredPlayers = 1,
                },
                Ejection = new ConfigData.EjectionOptions
                {
                    Drag = 2f,
                    EjectOnDeath = false,
                    EjectOnKilled = true,
                    EjectRandom = false,
                    ShowSmoke = false,
                    Min = 300,
                    Max = 600,
                    Wind = 10f
                },
                Helicopter = new ConfigData.HelicopterOptions
                {
                    DamageEffects = false,
                    Health = 10000
                },
                Notifications = new ConfigData.NotificationOptions
                {
                    Death = true,
                    Malfunction = true,
                    NPCDeath = true
                },
                NPC = new ConfigData.NPCOptions
                {
                    Amount = 1,
                    Names = new string[0],
                    Kits = new string[0],
                    LootType = "Random",
                    Health = 150,
                    Lifetime = 0,
                    RandomItems = new ConfigData.LootContainer
                    {
                        Minimum = 3,
                        Maximum = 5,
                        Items = new ConfigData.LootItem[]
                        {
                            new ConfigData.LootItem {Name = "apple", Skin = 0, Maximum = 6, Minimum = 2 },
                            new ConfigData.LootItem {Name = "bearmeat.cooked", Skin = 0, Maximum = 4, Minimum = 2 },
                            new ConfigData.LootItem {Name = "blueberries", Skin = 0, Maximum = 8, Minimum = 4 },
                            new ConfigData.LootItem {Name = "corn", Skin = 0, Maximum = 8, Minimum = 4 },
                            new ConfigData.LootItem {Name = "fish.raw", Skin = 0, Maximum = 4, Minimum = 2 },
                            new ConfigData.LootItem {Name = "granolabar", Skin = 0, Maximum = 4, Minimum = 1 },
                            new ConfigData.LootItem {Name = "meat.pork.cooked", Skin = 0, Maximum = 8, Minimum = 4 },
                            new ConfigData.LootItem {Name = "candycane", Skin = 0, Maximum = 2, Minimum = 1 }
                        }
                    },
                    RecoveryChance = 80,
                    SightRange = 60f,
                    RoamDistance = 50,
                    WoundedChance = 15
                },
                LootBoxes = new ConfigData.Loot
                {
                    Amount = 2,
                    RandomItems = new ConfigData.LootContainer
                    {
                        Minimum = 3,
                        Maximum = 5,
                        Items = new ConfigData.LootItem[]
                        {
                            new ConfigData.LootItem {Name = "apple", Skin = 0, Maximum = 6, Minimum = 2 },
                            new ConfigData.LootItem {Name = "bearmeat.cooked", Skin = 0, Maximum = 4, Minimum = 2 },
                            new ConfigData.LootItem {Name = "blueberries", Skin = 0, Maximum = 8, Minimum = 4 },
                            new ConfigData.LootItem {Name = "corn", Skin = 0, Maximum = 8, Minimum = 4 },
                            new ConfigData.LootItem {Name = "fish.raw", Skin = 0, Maximum = 4, Minimum = 2 },
                            new ConfigData.LootItem {Name = "granolabar", Skin = 0, Maximum = 4, Minimum = 1 },
                            new ConfigData.LootItem {Name = "meat.pork.cooked", Skin = 0, Maximum = 8, Minimum = 4 },
                            new ConfigData.LootItem {Name = "candycane", Skin = 0, Maximum = 2, Minimum = 1 }
                        }
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new Core.VersionNumber(3, 0, 0))
                configData = baseConfig;

            if (configData.Version < new Core.VersionNumber(3, 0, 1))            
                configData.NPC.Health = baseConfig.NPC.Health;            

            if (configData.Version < new Core.VersionNumber(3, 0, 2))
                configData.Helicopter = baseConfig.Helicopter;

            if (configData.Version < new Core.VersionNumber(3, 0, 3))
                configData.Notifications = baseConfig.Notifications;

            if (configData.Version < new Core.VersionNumber(3, 0, 4))
            {
                configData.NPC.Lifetime = 0;
                configData.Ejection.ShowSmoke = false;
            }

            if (configData.Version < new Core.VersionNumber(3, 0, 10))
            {
                configData.NPC.SightRange = 60f;
            }

            if (configData.Version < new Core.VersionNumber(3, 1, 2))
            {
                for (int i = 0; i < configData.LootBoxes.RandomItems.Items.Length; i++)                
                    configData.LootBoxes.RandomItems.Items[i].Weight = 1;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Localization
        private static void Broadcast(string key, params object[] args)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                player.ChatMessage(args?.Length > 0 ? string.Format(Instance.Message(key, player.userID), args) : Instance.Message(key, player.userID));
        }

        private string Message(string key, ulong playerId = 0UL) => lang.GetMessage(key, this, playerId == 0UL ? null : playerId.ToString());

        private Dictionary<string, string> Messages = new Dictionary<string, string>
        {           
            ["Notification.OnDeath"] = "<color=#ce422b>[Pilot Eject]</color> A <color=#aaff55>Patrol Helicopter</color> has been shot down and the pilot had to eject. He was last spotted near <color=#aaff55>{0}</color>",
            ["Notification.Malfunction"] = "<color=#ce422b>[Pilot Eject]</color> A <color=#aaff55>Patrol Helicopter</color> has malfunctioned and the pilot had to eject. He was last spotted near <color=#aaff55>{0}</color>",
            ["Notification.PilotKilled"] = "<color=#ce422b>[Pilot Eject]</color> A <color=#aaff55>Patrol Helicopter</color> pilot has been killed by <color=#aaff55>{0}</color>",
        };
        #endregion
    }
}
