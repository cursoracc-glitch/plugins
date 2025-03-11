// #define TESTING

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.VehicleBuyExtensionMethods;
using Rust;
using Rust.Modular;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Physics = UnityEngine.Physics;
using Random = UnityEngine.Random;
using Time = UnityEngine.Time;

#if CARBON
using Carbon.Base;
using Carbon.Modules;
#endif

namespace Oxide.Plugins
{
    [Info("Purchase Vehicles from the Shop", "M&B-Studios & Mevent", "2.1.4")]
    internal class VehicleBuy : RustPlugin
    {
        private class LockedVehicleTracker
        {
            public Dictionary<VehicleInfo, HashSet<BaseEntity>> VehiclesWithLocksByType { get; } = new();

            private readonly VehicleInfoManager _vehicleInfoManager;

            public LockedVehicleTracker(VehicleInfoManager vehicleInfoManager)
            {
                _vehicleInfoManager = vehicleInfoManager;
            }

            public void OnServerInitialized()
            {
                foreach (var entity in BaseNetworkable.serverEntities)
                {
                    var baseEntity = entity as BaseEntity;
                    if (baseEntity == null)
                        continue;

                    var vehicleInfo = _vehicleInfoManager.GetVehicleInfo(baseEntity);
                    if (vehicleInfo == null || GetVehicleLock(baseEntity) == null)
                        continue;

                    OnLockAdded(baseEntity);
                }
            }

            public void OnLockAdded(BaseEntity vehicle)
            {
                GetEntityListForVehicle(vehicle)?.Add(vehicle);
            }

            public void OnLockRemoved(BaseEntity vehicle)
            {
                GetEntityListForVehicle(vehicle)?.Remove(vehicle);
            }

            private HashSet<BaseEntity> EnsureEntityList(VehicleInfo vehicleInfo)
            {
                if (!VehiclesWithLocksByType.TryGetValue(vehicleInfo, out var vehicleList))
                {
                    vehicleList = new HashSet<BaseEntity>();
                    VehiclesWithLocksByType[vehicleInfo] = vehicleList;
                }

                return vehicleList;
            }

            private HashSet<BaseEntity> GetEntityListForVehicle(BaseEntity entity)
            {
                var vehicleInfo = _vehicleInfoManager.GetVehicleInfo(entity);
                if (vehicleInfo == null)
                    return null;

                return EnsureEntityList(vehicleInfo);
            }
        }

        private const float MaxDeployDistance = 3;

        private class VehicleInfo
        {
            public string VehicleType;
            public string[] PrefabPaths;
            public Vector3 LockPosition;
            public Quaternion LockRotation;
            public string ParentBone;

            public string CodeLockPermission { get; }
            public string KeyLockPermission { get; }
            public uint[] PrefabIds { get; private set; }

            public Func<BaseEntity, BaseEntity> DetermineLockParent = entity => entity;
            public Func<BaseEntity, float> TimeSinceLastUsed = entity => 0;

            public void OnServerInitialized()
            {
                if (!Instance.permission.PermissionExists(CodeLockPermission, Instance))
                    Instance.permission.RegisterPermission(CodeLockPermission, Instance);

                if (!Instance.permission.PermissionExists(KeyLockPermission, Instance))
                    Instance.permission.RegisterPermission(KeyLockPermission, Instance);

                ServerMgr.Instance.InvokeRepeating(() => GetNewEnts(), 0, 60);
                // Custom vehicles aren't currently allowed to specify prefabs since they reuse existing prefabs.
                if (PrefabPaths != null)
                {
                    var prefabIds = new List<uint>();
                    foreach (var prefabName in PrefabPaths)
                    {
                        var prefabId = StringPool.Get(prefabName);
                        if (prefabId != 0) prefabIds.Add(prefabId);
                    }

                    PrefabIds = prefabIds.ToArray();
                }
            }

            // In the future, custom vehicles may be able to pass in a method to override this.
            public bool IsMounted(BaseEntity entity)
            {
                var vehicle = entity as BaseVehicle;
                if (vehicle != null)
                    return vehicle.AnyMounted();

                var mountable = entity as BaseMountable;
                if (mountable != null)
                    return mountable.AnyMounted();

                return false;
            }
        }

        private class VehicleInfoManager
        {
            private readonly Dictionary<uint, VehicleInfo> _prefabIdToVehicleInfo = new();

            private readonly Dictionary<string, VehicleInfo>
                _customVehicleTypes = new();

            public void OnServerInitialized()
            {
                var allVehicles = new[]
                {
                    new VehicleInfo
                    {
                        VehicleType = "attackhelicopter",
                        PrefabPaths = new[] {"assets/content/vehicles/attackhelicopter/attackhelicopter.entity.prefab"},
                        LockPosition = new Vector3(-0.6f, 1.08f, 1.01f),
                        TimeSinceLastUsed = vehicle =>
                            Time.time - (vehicle as AttackHelicopter)?.lastEngineOnTime ?? Time.time
                    },
                    new VehicleInfo
                    {
                        VehicleType = "chinook",
                        PrefabPaths = new[] {"assets/prefabs/npc/ch47/ch47.entity.prefab"},
                        LockPosition = new Vector3(-1.175f, 2, 6.5f),
                        TimeSinceLastUsed = vehicle =>
                            Time.time - (vehicle as CH47Helicopter)?.lastPlayerInputTime ?? Time.time
                    },
                    new VehicleInfo
                    {
                        VehicleType = "duosub",
                        PrefabPaths = new[] {"assets/content/vehicles/submarine/submarineduo.entity.prefab"},
                        LockPosition = new Vector3(-0.455f, 1.29f, 0.75f),
                        LockRotation = Quaternion.Euler(0, 180, 10),
                        TimeSinceLastUsed = vehicle => (vehicle as SubmarineDuo)?.timeSinceLastUsed ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "hotairballoon",
                        PrefabPaths = new[] {"assets/prefabs/deployable/hot air balloon/hotairballoon.prefab"},
                        LockPosition = new Vector3(1.45f, 0.9f, 0),
                        TimeSinceLastUsed = vehicle =>
                            Time.time - (vehicle as HotAirBalloon)?.sinceLastBlast ?? Time.time
                    },
                    new VehicleInfo
                    {
                        VehicleType = "kayak",
                        PrefabPaths = new[] {"assets/content/vehicles/boats/kayak/kayak.prefab"},
                        LockPosition = new Vector3(-0.43f, 0.2f, 0.2f),
                        LockRotation = Quaternion.Euler(0, 90, 90),
                        TimeSinceLastUsed = vehicle => (vehicle as Kayak)?.timeSinceLastUsed ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "locomotive",
                        PrefabPaths = new[] {"assets/content/vehicles/trains/locomotive/locomotive.entity.prefab"},
                        LockPosition = new Vector3(-0.11f, 2.89f, 4.95f),
                        TimeSinceLastUsed = vehicle => (vehicle as TrainEngine)?.decayingFor ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "magnetcrane",
                        PrefabPaths = new[] {"assets/content/vehicles/crane_magnet/magnetcrane.entity.prefab"},
                        LockPosition = new Vector3(-1.735f, -1.445f, 0.79f),
                        LockRotation = Quaternion.Euler(0, 0, 90),
                        ParentBone = "Top",
                        TimeSinceLastUsed = vehicle =>
                            Time.realtimeSinceStartup - (vehicle as MagnetCrane)?.lastDrivenTime ??
                            Time.realtimeSinceStartup
                    },
                    new VehicleInfo
                    {
                        VehicleType = "minicopter",
                        PrefabPaths = new[] {"assets/content/vehicles/minicopter/minicopter.entity.prefab"},
                        LockPosition = new Vector3(-0.15f, 0.7f, -0.1f),
                        TimeSinceLastUsed = vehicle =>
                            Time.time - (vehicle as Minicopter)?.lastEngineOnTime ?? Time.time
                    },
                    new VehicleInfo
                    {
                        VehicleType = "modularcar",
                        // There are at least 37 valid Modular Car prefabs.
                        PrefabPaths = FindPrefabsOfType<ModularCar>(),
                        LockPosition = new Vector3(-0.9f, 0.35f, -0.5f),
                        DetermineLockParent = vehicle => FindFirstDriverModule((ModularCar) vehicle),
                        TimeSinceLastUsed = vehicle =>
                            Time.time - (vehicle as ModularCar)?.lastEngineOnTime ?? Time.time
                    },
                    new VehicleInfo
                    {
                        VehicleType = "rhib",
                        PrefabPaths = new[] {"assets/content/vehicles/boats/rhib/rhib.prefab"},
                        LockPosition = new Vector3(-0.68f, 2.00f, 0.7f),
                        TimeSinceLastUsed = vehicle => (vehicle as RHIB)?.timeSinceLastUsedFuel ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "ridablehorse",
                        PrefabPaths = new[] {"assets/content/vehicles/horse/ridablehorse2.prefab"},
                        LockPosition = new Vector3(-0.6f, 0.25f, -0.1f),
                        LockRotation = Quaternion.Euler(0, 95, 90),
                        ParentBone = "Horse_RootBone",
                        TimeSinceLastUsed = vehicle =>
                            Time.time - (vehicle as RidableHorse2)?.lastRiddenTime ?? Time.time
                    },
                    new VehicleInfo
                    {
                        VehicleType = "rowboat",
                        PrefabPaths = new[] {"assets/content/vehicles/boats/rowboat/rowboat.prefab"},
                        LockPosition = new Vector3(-0.83f, 0.51f, -0.57f),
                        TimeSinceLastUsed = vehicle => (vehicle as MotorRowboat)?.timeSinceLastUsedFuel ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "scraptransport",
                        PrefabPaths = new[]
                            {"assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab"},
                        LockPosition = new Vector3(-1.25f, 1.22f, 1.99f),
                        TimeSinceLastUsed = vehicle =>
                            Time.time - (vehicle as ScrapTransportHelicopter)?.lastEngineOnTime ?? Time.time
                    },
                    new VehicleInfo
                    {
                        VehicleType = "sedan",
                        PrefabPaths = new[] {"assets/content/vehicles/sedan_a/sedantest.entity.prefab"},
                        LockPosition = new Vector3(-1.09f, 0.79f, 0.5f)
                    },
                    new VehicleInfo
                    {
                        VehicleType = "sedanrail",
                        PrefabPaths = new[] {"assets/content/vehicles/sedan_a/sedanrail.entity.prefab"},
                        LockPosition = new Vector3(-1.09f, 1.025f, -0.26f),
                        TimeSinceLastUsed = vehicle => (vehicle as TrainEngine)?.decayingFor ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "snowmobile",
                        PrefabPaths = new[] {"assets/content/vehicles/snowmobiles/snowmobile.prefab"},
                        LockPosition = new Vector3(-0.205f, 0.59f, 0.4f),
                        TimeSinceLastUsed = vehicle => (vehicle as Snowmobile)?.timeSinceLastUsed ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "solosub",
                        PrefabPaths = new[] {"assets/content/vehicles/submarine/submarinesolo.entity.prefab"},
                        LockPosition = new Vector3(0f, 1.85f, 0f),
                        LockRotation = Quaternion.Euler(0, 90, 90),
                        TimeSinceLastUsed = vehicle => (vehicle as BaseSubmarine)?.timeSinceLastUsed ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "tomaha",
                        PrefabPaths = new[] {"assets/content/vehicles/snowmobiles/tomahasnowmobile.prefab"},
                        LockPosition = new Vector3(-0.37f, 0.4f, 0.125f),
                        TimeSinceLastUsed = vehicle => (vehicle as Snowmobile)?.timeSinceLastUsed ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "tugboat",
                        PrefabPaths = new[] {"assets/content/vehicles/boats/tugboat/tugboat.prefab"},
                        LockPosition = new Vector3(0.065f, 6.8f, 4.12f),
                        LockRotation = Quaternion.Euler(0, 90, 60),
                        TimeSinceLastUsed = vehicle => (vehicle as Tugboat)?.timeSinceLastUsedFuel ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "workcart",
                        PrefabPaths = new[] {"assets/content/vehicles/trains/workcart/workcart.entity.prefab"},
                        LockPosition = new Vector3(-0.2f, 2.35f, 2.7f),
                        TimeSinceLastUsed = vehicle => (vehicle as TrainEngine)?.decayingFor ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "workcartaboveground",
                        PrefabPaths = new[]
                            {"assets/content/vehicles/trains/workcart/workcart_aboveground.entity.prefab"},
                        LockPosition = new Vector3(-0.2f, 2.35f, 2.7f),
                        TimeSinceLastUsed = vehicle => (vehicle as TrainEngine)?.decayingFor ?? 0
                    },
                    new VehicleInfo
                    {
                        VehicleType = "workcartcovered",
                        PrefabPaths = new[]
                            {"assets/content/vehicles/trains/workcart/workcart_aboveground2.entity.prefab"},
                        LockPosition = new Vector3(-0.2f, 2.35f, 2.7f),
                        TimeSinceLastUsed = vehicle => (vehicle as TrainEngine)?.decayingFor ?? 0
                    }
                };

                foreach (var vehicleInfo in allVehicles)
                {
                    vehicleInfo.OnServerInitialized();
                    foreach (var prefabId in vehicleInfo.PrefabIds) _prefabIdToVehicleInfo[prefabId] = vehicleInfo;
                }
            }

            public void RegisterCustomVehicleType(VehicleInfo vehicleInfo)
            {
                vehicleInfo.OnServerInitialized();
                _customVehicleTypes[vehicleInfo.VehicleType] = vehicleInfo;
            }

            public VehicleInfo GetVehicleInfo(BaseEntity entity)
            {
                if (_prefabIdToVehicleInfo.TryGetValue(entity.prefabID, out var vehicleInfo))
                    return vehicleInfo;

                foreach (var customVehicleInfo in _customVehicleTypes.Values)
                    if (customVehicleInfo.DetermineLockParent(entity) != null)
                        return customVehicleInfo;

                return null;
            }

            public BaseEntity GetCustomVehicleParent(BaseEntity entity)
            {
                foreach (var vehicleInfo in _customVehicleTypes.Values)
                {
                    var lockParent = vehicleInfo.DetermineLockParent(entity);
                    if (lockParent != null)
                        return lockParent;
                }

                return null;
            }
        }

        internal struct VendingMachinePosition
        {
            public Vector3 Offset;
            public Transform transform;
            public Vector3 Rotation;

            public VendingMachinePosition(Transform position, Vector3 rotation, Vector3 offset)
            {
                transform = position;
                Rotation = rotation;
                Offset = offset;
            }
        }

        #region Fields

        [PluginReference] private Plugin ImageLibrary = null, Economics = null, ServerRewards = null, BankSystem = null;

        private static VehicleBuy Instance;

#if CARBON
		private ImageDatabaseModule imageDatabase;
#endif

        private bool _enabledImageLibrary;

        private Dictionary<ulong, ulong> PRM = new();

        private const bool LangRu = false;

        private const string
            Prefab_CodeLock_DeployedEffect = "assets/prefabs/locks/keypad/effects/lock-code-deploy.prefab",
            Prefab_CodeLock_DeniedEffect = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab",
            Prefab_CodeLock_UnlockEffect = "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab",
            VENDINGMACHINE_PREFAB = "assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab";

        private const string
            PERM_USE = "vehiclebuy.use",
            PERM_FREE = "vehiclebuy.free",
            PERM_PICKUP = "vehiclebuy.pickup",
            PERM_RECALL = "vehiclebuy.recall";

        private const string PURCHASE_EFFECT =
            "assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab";

        private VehicleInfoManager _vehicleInfoManager;
        private LockedVehicleTracker _lockedVehicleTracker;
        private List<BaseEntity> VendingMachines = new();

        private VendingMachinePosition BANDITCAMP_POSITION;
        private VendingMachinePosition OUTPOST_POSITION;
        private VendingMachinePosition fvA_POSITION;
        private VendingMachinePosition fvB_POSITION;
        private VendingMachinePosition fvC_POSITION;

        private MonumentInfo Outpost;
        private MonumentInfo BanditCamp;
        private List<MonumentInfo> FishingVillagesA = new();
        private List<MonumentInfo> FishingVillagesB = new();
        private List<MonumentInfo> FishingVillagesC = new();

        #endregion

        #region Data

        private Dictionary<ulong, PlayerData> _players = new();

        internal class PlayerData
        {
            public Dictionary<string, int> Cooldowns = new();

            public Dictionary<string, int> PurchasedVehicles = new Dictionary<string, int>();
        }

        #endregion

        #region Base Hooks

        private bool CanPlayerBypassLock(BasePlayer player, BaseLock baseLock, bool provideFeedback)
        {
            var hookResult = Interface.CallHook("CanUseLockedEntity", player, baseLock);
            if (hookResult is bool)
                return (bool)hookResult;

            var canAccessLock = IsPlayerAuthorizedToLock(player, baseLock);

            if (canAccessLock)
            {
                if (provideFeedback && !(baseLock is KeyLock))
                    Effect.server.Run(Prefab_CodeLock_UnlockEffect, baseLock, 0, Vector3.zero, Vector3.forward);

                return true;
            }

            if (provideFeedback)
                Effect.server.Run(Prefab_CodeLock_DeniedEffect, baseLock, 0, Vector3.zero, Vector3.forward);

            return false;
        }

        private static VehicleModuleSeating FindFirstDriverModule(ModularCar car)
        {
            for (var socketIndex = 0; socketIndex < car.TotalSockets; socketIndex++)
            {
                if (car.TryGetModuleAt(socketIndex, out var module))
                {
                    var seatingModule = module as VehicleModuleSeating;
                    if (seatingModule != null && seatingModule.HasADriverSeat())
                        return seatingModule;
                }
            }

            return null;
        }

        private void Init()
        {
            _vehicleInfoManager = new VehicleInfoManager();
            _lockedVehicleTracker = new LockedVehicleTracker(_vehicleInfoManager);

            if (!_config.DisableVehiclesDamage)
                Unsubscribe(nameof(OnEntityTakeDamage));
        }

        private void OnServerInitialized()
        {
            Outpost = TerrainMeta.Path.Monuments.Find(p =>
                p.name.ToLower().Contains("monument/medium/compound"));
            BanditCamp =
                TerrainMeta.Path.Monuments.Find(p =>
                    p.name.ToLower().Contains("monument/medium/bandit_town"));

            BANDITCAMP_POSITION = new VendingMachinePosition
            {
                Offset = new Vector3(-53.28f, 2f, 28.72f),
                transform = BanditCamp?.transform ?? null,
                Rotation = new Vector3(0, 51.4f, 0)
            };

            OUTPOST_POSITION = new VendingMachinePosition
            {
                Offset = new Vector3(32f, 1.51f, -15.34f),
                transform = Outpost?.transform ?? null,
                Rotation = new Vector3(0, 270f, 0)
            };

            fvA_POSITION = new VendingMachinePosition
            {
                Offset = new Vector3(1.2f, 2f, 4.48f),
                Rotation = new Vector3(0, 180f, 0)
            };
            fvB_POSITION = new VendingMachinePosition
            {
                Offset = new Vector3(-10.04f, 2f, 20.58f),
                Rotation = new Vector3(0, 270f, 0)
            };
            fvC_POSITION = new VendingMachinePosition
            {
                Offset = new Vector3(-8.72f, 2f, 12.23f),
                Rotation = new Vector3(0, 90f, 0)
            };

            var fvA = TerrainMeta.Path.Monuments.FindAll(p =>
                p.name.ToLower().Contains("monument/fishing_village/fishing_village_a"));
            var fvB = TerrainMeta.Path.Monuments.FindAll(p =>
                p.name.ToLower().Contains("monument/fishing_village/fishing_village_b"));
            var fvC = TerrainMeta.Path.Monuments.FindAll(p =>
                p.name.ToLower().Contains("monument/fishing_village/fishing_village_c"));

            if (fvA.Any())
                FishingVillagesA.AddRange(fvA);
            else
                PrintError("Fishing villages A not found at map");

            if (fvB.Any())
                FishingVillagesB.AddRange(fvB);
            else
                PrintError("Fishing villages B not found at map");

            if (fvC.Any())
                FishingVillagesC.AddRange(fvC);
            else
                PrintError("Fishing villages C not found at map");

            if (Outpost == null) PrintError("Outpost not found at the map!");

            if (BanditCamp == null) PrintError("Bandit Camp not found at the map!");

            RegisterPermissions();

            RegisterCommands();

            Instance = this;

            LoadSetupUI();

            LoadImages();

            _vehicleInfoManager.OnServerInitialized();
            _lockedVehicleTracker.OnServerInitialized();

            foreach (var entity in BaseNetworkable.serverEntities.OfType<BaseEntity>())
            {
                var vehicleInfo = _vehicleInfoManager.GetVehicleInfo(entity);
                if (vehicleInfo == null)
                    continue;

                var lockEntity = GetVehicleLock(entity);
                if (lockEntity == null)
                    continue;

                var transform = lockEntity.transform;
                transform.localPosition = vehicleInfo.LockPosition;
                transform.localRotation = vehicleInfo.LockRotation;
                lockEntity.SendNetworkUpdate_Position();
            }

            Subscribe(nameof(OnEntityKill));

            SpawnVendingMachines();
        }

        private void Unload()
        {
            try
            {
                DestroyVendingMachines();

                foreach (var player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, Layer);
                    CuiHelper.DestroyUi(player, "UI.Server.Panel.Content");
                }
            }
            finally
            {
                Instance = null;
            }
        }

        #endregion

        #region Functions

        [ChatCommand("getposfva")]
        private void cmdGetPosfva(BasePlayer player)
        {
            if (!player.IsAdmin)
                return;

            var pos = player.GetNetworkPosition();
            var fva = FishingVillagesA.FirstOrDefault();
            // player.Teleport(fva.transform.position);

            player.ChatMessage($"Monument pos = {fva.transform.position}");
            player.ChatMessage($"Local to mon = {fva.transform.InverseTransformPoint(player.GetNetworkPosition())}");
            player.ChatMessage($"Global = X - {pos.x}, Y - {pos.y}, Z - {pos.z}");
            player.ChatMessage($"qu - {player.GetNetworkRotation()}");
        }

        [ChatCommand("getposfvb")]
        private void cmdGetPosfvb(BasePlayer player)
        {
            if (!player.IsAdmin)
                return;

            var pos = player.GetNetworkPosition();
            var fva = FishingVillagesB.FirstOrDefault();
            // player.Teleport(fva.transform.position);

            player.ChatMessage($"Monument pos = {fva.transform.position}");
            player.ChatMessage($"Local to mon = {fva.transform.InverseTransformPoint(player.GetNetworkPosition())}");
            player.ChatMessage($"Global = X - {pos.x}, Y - {pos.y}, Z - {pos.z}");
            player.ChatMessage($"qu - {player.GetNetworkRotation()}");
        }

        [ChatCommand("getposfvc")]
        private void cmdGetPosfvc(BasePlayer player)
        {
            if (!player.IsAdmin)
                return;

            var pos = player.GetNetworkPosition();
            var fva = FishingVillagesC.FirstOrDefault();
            // player.Teleport(fva.transform.position);

            player.ChatMessage($"Monument pos = {fva.transform.position}");
            player.ChatMessage($"Local to mon = {fva.transform.InverseTransformPoint(player.GetNetworkPosition())}");
            player.ChatMessage($"Global = X - {pos.x}, Y - {pos.y}, Z - {pos.z}");
            player.ChatMessage($"qu - {player.GetNetworkRotation()}");
        }

        [ChatCommand("getposoutpost")]
        private void cmdGetPosOP(BasePlayer player)
        {
            if (!player.IsAdmin)
                return;

            var pos = player.GetNetworkPosition();

            player.ChatMessage($"Monument pos = {Outpost.transform.position}");
            player.ChatMessage(
                $"Local to mon = {Outpost.transform.InverseTransformPoint(player.GetNetworkPosition())}");
            player.ChatMessage($"Global = X - {pos.x}, Y - {pos.y}, Z - {pos.z}");
            player.ChatMessage($"qu - {player.GetNetworkRotation()}");
        }

        [ChatCommand("getposbandit")]
        private void cmdGetPosBC(BasePlayer player)
        {
            if (!player.IsAdmin)
                return;
            var pos = player.GetNetworkPosition();
            player.ChatMessage($"Monument pos = {BanditCamp.transform.position}");
            player.ChatMessage(
                $"Local to mon = {BanditCamp.transform.InverseTransformPoint(player.GetNetworkPosition())}");
            player.ChatMessage($"Global = X - {pos.x}, Y - {pos.y}, Z - {pos.z}");
            player.ChatMessage($"qu - {player.GetNetworkRotation()}");
        }

        private object OnGiveSoldItem(VendingMachine vending, Item soldItem, BasePlayer buyer)
        {
            if (!VMProducts.ContainsKey(vending.net.ID))
                return null;

            var vmopt = VMProducts[vending.net.ID];

            if (!vmopt.Any(x => x.RandomShortname == soldItem.info.shortname))
                return null;

            var item = vmopt.FirstOrDefault(x => x.RandomShortname == soldItem.info.shortname);

            var itemToGive = ItemManager.CreateByItemID(item.Get().DeployableItemId, soldItem.amount, item.Get().Skin);

            if (!string.IsNullOrEmpty(item.Get().Name))
                itemToGive.name = item.Get().Name;

            buyer.GiveItem(itemToGive);

            return false;
        }

        private object CanAdministerVending(BasePlayer player, VendingMachine machine)
        {
            if (VMProducts.ContainsKey(machine.net.ID))
                return false;
            return null;
        }

        private void SpawnVendingMachines()
        {
            if (_config.VendingMachines.BanditCampSpawnMachine && BanditCamp)
            {
                var machine = SpawnVendingMachine(BANDITCAMP_POSITION);
                if (!machine.TryGetComponent<VendingMachine>(out var comp))
                {
                    UnityEngine.Object.Destroy(machine.gameObject);
                    return;
                }

                SetupVendingMachine(machine, comp, _config.VendingMachines.BanditCampOrders);
            }

            if (_config.VendingMachines.OutpostSpawnMachine && Outpost)
            {
                var machine = SpawnVendingMachine(OUTPOST_POSITION);
                // BasePlayer.activePlayerList.First().Teleport(machine.transform.position);
                if (!machine.TryGetComponent<VendingMachine>(out var comp))
                {
                    UnityEngine.Object.Destroy(machine.gameObject);
                    return;
                }

                SetupVendingMachine(machine, comp, _config.VendingMachines.OutpostOrders);
            }


            if (_config.VendingMachines.FishingVillageASpawnMachine && FishingVillagesA.Any())
                foreach (var x in FishingVillagesA)
                {
                    var machine = SpawnVendingMachine(fvA_POSITION, x.transform);

                    if (!machine.TryGetComponent<VendingMachine>(out var comp))
                    {
                        UnityEngine.Object.Destroy(machine.gameObject);
                        return;
                    }

                    SetupVendingMachine(machine, comp, _config.VendingMachines.FishingVillageAOrders);
                }

            if (_config.VendingMachines.FishingVillageBSpawnMachine && FishingVillagesB.Any())
                foreach (var x in FishingVillagesB)
                {
                    var machine = SpawnVendingMachine(fvB_POSITION, x.transform);

                    if (!machine.TryGetComponent<VendingMachine>(out var comp))
                    {
                        UnityEngine.Object.Destroy(machine.gameObject);
                        return;
                    }

                    SetupVendingMachine(machine, comp, _config.VendingMachines.FishingVillageBOrders);
                }

            if (_config.VendingMachines.FishingVillageBSpawnMachine && FishingVillagesB.Any())
                foreach (var x in FishingVillagesC)
                {
                    var machine = SpawnVendingMachine(fvC_POSITION, x.transform);

                    if (!machine.TryGetComponent<VendingMachine>(out var comp))
                    {
                        UnityEngine.Object.Destroy(machine.gameObject);
                        return;
                    }

                    SetupVendingMachine(machine, comp, _config.VendingMachines.FishingVillageCOrders);
                }
        }

        private static void SendEffect(BasePlayer player, string effect)
        {
            if (player == null || string.IsNullOrEmpty(effect)) return;

            Effect.server.Run(effect, player, 0, Vector3.zero, Vector3.forward);
        }

        private void SetupVendingMachine(BaseEntity machine, VendingMachine comp, List<VMOrder> items)
        {
            comp.shopName = "VEHICLE SHOP";
            var usedShortnames = new Dictionary<string, string>();

            foreach (var item in items)
            {
                if (!_config.Vehicles.ContainsKey(item.VehicleKey))
                    continue;

                var pricedef = ItemManager.FindItemDefinition(item.Shortname);

                var randDef =
                    ItemManager.itemList.Find(x => !usedShortnames.ContainsKey(x.shortname));

                usedShortnames.Add(randDef.shortname, item.VehicleKey);
                comp.sellOrders.sellOrders.Add(new ProtoBuf.VendingMachine.SellOrder
                {
                    itemToSellID = randDef.itemid,
                    itemToSellAmount = 1,
                    currencyID = pricedef.itemid,
                    currencyAmountPerItem = item.Price,
                    inStock = 10000,
                    currencyIsBP = false,
                    itemToSellIsBP = false,
                    itemCondition = 100,
                    itemConditionMax = 100
                });
            }

            foreach (var (itemShortname, vehicle) in usedShortnames)
            {
                var item = ItemManager.CreateByName(itemShortname, 10000, _config.Vehicles[vehicle].Skin);

                item.MoveToContainer(comp.inventory);
            }

            VMProducts.Add(machine.net.ID, usedShortnames.Select(x => new VendingMachineCache
            {
                RandomShortname = x.Key,
                VehicleKey = x.Value
            }));
            VendingMachines.Add(machine);
        }

        internal class VendingMachineCache
        {
            public string RandomShortname;
            public string VehicleKey;

            public VehicleInfoConfig Get()
            {
                return Instance._config.Vehicles[VehicleKey];
            }
        }

        private Dictionary<NetworkableId, IEnumerable<VendingMachineCache>> VMProducts = new();

        private void DestroyVendingMachines()
        {
            VendingMachines?.ForEach(x => x.Kill());
        }

        private BaseEntity SpawnVendingMachine(VendingMachinePosition position)
        {
            return SpawnVendingMachine(position, position.transform);
        }

        private BaseEntity SpawnVendingMachine(VendingMachinePosition position, Transform transform)
        {
            var entity = GameManager.server.CreateEntity(VENDINGMACHINE_PREFAB,
                transform.TransformPoint(position.Offset));
            if (entity.TryGetComponent<StabilityEntity>(out var comp))
                comp.grounded = true;

            entity.OwnerID = 92929294944;
            entity.Spawn();
            entity.transform.rotation = transform.rotation * Quaternion.Euler(position.Rotation);

            return entity;
        }

        private static string[] FindPrefabsOfType<T>() where T : BaseEntity
        {
            var prefabList = Pool.Get<List<string>>();

            try
            {
                for (var i = 0; i < GameManifest.Current.entities.Length; i++)
                {
                    var entity = GameManager.server.FindPrefab(GameManifest.Current.entities[i])?.GetComponent<T>();
                    if (entity == null) continue;

                    prefabList.Add(entity.PrefabName);
                }

                return prefabList.ToArray();
            }
            finally
            {
                Pool.FreeUnmanaged(ref prefabList);
            }
        }

        private int GetVehiclePurchased(BasePlayer player, string key)
        {
            return _players.TryGetValue(player.userID, out var playerData) &&
                   playerData.PurchasedVehicles.TryGetValue(key, out var purchased)
                ? purchased
                : 0;
        }

        private void SetVehiclePurchased(BasePlayer player, string key)
        {
            _players.TryAdd(player.userID, new PlayerData());

            _players[player.userID].PurchasedVehicles.TryAdd(key, 0);

            _players[player.userID].PurchasedVehicles[key]++;
        }

        private void SetCooldown(BasePlayer player, string key, int cooldownTime)
        {
            _players.TryAdd(player.userID, new PlayerData());

            _players[player.userID].Cooldowns.TryAdd(key, 0);

            _players[player.userID].Cooldowns[key] = Facepunch.Math.Epoch.Current + cooldownTime;
        }

        private int GetCooldown(BasePlayer player, string key)
        {
            _players.TryAdd(player.userID, new PlayerData());

            if (_players[player.userID].Cooldowns.TryGetValue(key, out var cooldown))
                return cooldown - Facepunch.Math.Epoch.Current;

            return -1;
        }

        private bool IsCooldown(BasePlayer player, string key)
        {
            return GetCooldown(player, key) > 0;
        }

        private int ItemsCount(BasePlayer player, string shortname)
        {
            return player.inventory.GetAmount(ItemManager.FindItemDefinition(shortname).itemid);
        }

        private bool CanBuy(BasePlayer player, VehicleInfoConfig product, string key)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_USE))
                return false;

            if (product.MaxPurchases > 0 && GetVehiclePurchased(player, key) >= product.MaxPurchases)
            {
                player.ChatMessage(GetMessage(MAX_PURCHASES_EXCEEDED, player.UserIDString, product.Shortname));
                return false;
            }

            if (permission.UserHasPermission(player.UserIDString, PERM_FREE)) return true;

            return product.SellCurrency switch
            {
                0 => ItemsCount(player, product.Shortname) >= product.Price,
                1 => GetBalance(player, product) >= product.Price,
                2 => GetBalance(player, product) >= product.Price,
                3 => GetBalance(player, product) >= product.Price,
                _ => false
            };
        }

        private bool Collect(BasePlayer player, VehicleInfoConfig product, string key)
        {
            if (!CanBuy(player, product, key))
                return false;

            switch (product.SellCurrency)
            {
                case 0:
                    player.inventory.Take(null, ItemManager.FindItemDefinition(product.Shortname).itemid,
                        product.Price);
                    return true;
                case 1:
                case 2:
                case 3:
                    Withdraw(player, product.Price, product.SellCurrency, product.Shortname);
                    return true;
                default:
                    return false;
            }
        }

        private string GetRemainingCost(BasePlayer player, VehicleInfoConfig product)
        {
            return product.SellCurrency switch
            {
                0 => product.Price - ItemsCount(player, product.Shortname) + " " +
                     ItemManager.FindItemDefinition(product.Shortname).displayName.english.ToUpper(),
                1 => product.Price - GetBalance(player, product) + " " + _config.CurrencyName,
                2 => product.Price - GetBalance(player, product) + " " + _config.CurrencyNameSR,
                3 => product.Price - GetBalance(player, product) + " " + _config.CurrencyNameBS,
                _ => ""
            };
        }

        private int GetBalance(BasePlayer player, VehicleInfoConfig product)
        {
            if (product.SellCurrency == 1 && !Economics)
            {
                PrintError("Economics plugin is not available!");
                return -1;
            }

            if (product.SellCurrency == 2 && !ServerRewards)
            {
                PrintError("ServerRewards plugin is not available!");
                return -1;
            }

            if (product.SellCurrency == 3 && !BankSystem)
            {
                PrintError("BankSystem plugin is not available!");
                return -1;
            }

            return product.SellCurrency switch
            {
                0 => ItemsCount(player, product.Shortname),
                1 => Convert.ToInt32(Economics?.Call<double>("Balance", player.userID.Get())),
                2 => Convert.ToInt32(ServerRewards?.Call("CheckPoints", player.userID.Get())),
                3 => Convert.ToInt32(BankSystem?.Call("API_BankSystemBalance", player.userID.Get())),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void Withdraw(BasePlayer player, int price, int type, string shortname)
        {
            switch (type)
            {
                case 0:
                    if (string.IsNullOrEmpty(shortname))
                    {
                        PrintError("Shortname in 'Withdraw' is null!");
                        return;
                    }

                    player.inventory.Take(null, ItemManager.FindItemDefinition(shortname).itemid, price);
                    break;
                case 1:
                    Economics?.Call("Withdraw", player.userID.Get(), (double)price);
                    break;
                case 2:
                    ServerRewards?.Call("TakePoints", player.userID.Get(), price);
                    break;
                case 3:
                    BankSystem?.Call("API_BankSystemWithdraw", player.userID.Get(), price);
                    break;
            }
        }

        private string GetCost(string key)
        {
            if (!_config.Vehicles.ContainsKey(key))
                return "";

            var product = _config.Vehicles.FirstOrDefault(x => x.Key == key);

            switch (product.Value.SellCurrency)
            {
                case 0:
                    {
                        var itemDef = ItemManager.FindItemDefinition(product.Value.Shortname);

                        return itemDef == null
                            ? "UNKNOWN ITEM"
                            : $"{product.Value.Price} {itemDef.displayName.english.ToUpper()}";
                    }
                case 1:
                    return $"{product.Value.Price} {_config.CurrencyName}";
                case 2:
                    return $"{product.Value.Price} {_config.CurrencyNameSR}";
                case 3:
                    return $"{product.Value.Price} {_config.CurrencyNameBS}";
                default:
                    return "ERROR CHECK CFG";
            }
        }

        #region Working with Images

        private Dictionary<string, string> _loadedImages = new Dictionary<string, string>();

        private void AddImage(string url, string fileName, ulong imageId = 0)
        {
#if CARBON
			imageDatabase.Queue(true, new Dictionary<string, string>
			{
				[fileName] = url
			});
#else
            ImageLibrary?.Call("AddImage", url, fileName, imageId);
#endif
        }

        private string GetImage(string name)
        {
            if (_loadedImages.TryGetValue(name, out var imageID)) return imageID;

#if CARBON
			return imageDatabase.GetImageString(name);
#else
            return Convert.ToString(ImageLibrary?.Call("GetImage", name));
#endif
        }

        private bool HasImage(string name)
        {
#if CARBON
			return Convert.ToBoolean(imageDatabase.HasImage(name));
#else
            return Convert.ToBoolean(ImageLibrary?.Call("HasImage", name));
#endif
        }

        private void LoadImages()
        {
#if CARBON
			imageDatabase = BaseModule.GetModule<ImageDatabaseModule>();
#endif

            _enabledImageLibrary = true;

            var imagesList = new Dictionary<string, string>();

            foreach (var (prefab, vehicleSettings) in _config.Vehicles)
                RegisterImage(ref imagesList, prefab + ".image", vehicleSettings.Image);

            foreach (var (name, url) in imagesList.ToArray())
            {
                if (!url.StartsWith("MeventImages/")) continue;

                LoadImageFromFS(name, url);

                imagesList.Remove(name);
            }

            if (imagesList.Count <= 0) return;

#if CARBON
            imageDatabase.Queue(false, imagesList);
#else
            timer.In(1f, () =>
            {
                if (ImageLibrary is not { IsLoaded: true })
                {
                    _enabledImageLibrary = false;

                    BroadcastILNotInstalled();
                    return;
                }

                ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
            });
#endif
        }

        private void RegisterImage(ref Dictionary<string, string> images, string name, string image)
        {
            if (string.IsNullOrEmpty(image) || string.IsNullOrEmpty(name)) return;

            images.TryAdd(name, image);
        }

        private void BroadcastILNotInstalled()
        {
            for (var i = 0; i < 5; i++) PrintError("IMAGE LIBRARY IS NOT INSTALLED.");
        }

        private void LoadImageFromFS(string name, string path)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path)) return;

            ServerMgr.Instance.StartCoroutine(LoadImage(name, path));
        }

        private IEnumerator LoadImage(string name, string path)
        {
            var url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + path;
            using var www = UnityWebRequestTexture.GetTexture(url);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Image not found: {path}");
            }
            else
            {
                var texture = DownloadHandlerTexture.GetContent(www);
                try
                {
                    var image = texture.EncodeToPNG();

                    _loadedImages.TryAdd(name, FileStorage.server.Store(image, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        #endregion

        private void RegisterPermissions()
        {
            permission.RegisterPermission(PERM_USE, this);
            permission.RegisterPermission(PERM_FREE, this);
            permission.RegisterPermission(PERM_PICKUP, this);
            permission.RegisterPermission(PERM_RECALL, this);

            foreach (var vehicleInfo in _config.Vehicles.Values)
                if (!string.IsNullOrEmpty(vehicleInfo.Permission) && !permission.PermissionExists(vehicleInfo.Permission))
                    permission.RegisterPermission(vehicleInfo.Permission, this);
        }

        public void RegisterCommands()
        {
            foreach (var x in _config.Commands)
                cmd.AddChatCommand(x, this, cmdVehicleBuy);

            foreach (var vehicle in _config.Vehicles) AddCovalenceCommand(vehicle.Value.Command, nameof(CmdAddVehicle));
        }

        private bool GiveVehicle(ItemContainer container, VehicleInfoConfig vehicleSettings, bool needFuel = true)
        {
            var item = ItemManager.CreateByItemID(vehicleSettings.DeployableItemId, 1, vehicleSettings.Skin);
            item.name = vehicleSettings.Name;
            return item.MoveToContainer(container, -1, false);
        }

        private void GetOutParts(ModularCar car)
        {
            foreach (var child in car.children)
            {
                var engineModule = child as VehicleModuleEngine;
                if (engineModule == null)
                    continue;

                var engineStorage = engineModule.GetContainer() as EngineStorage;
                if (engineStorage == null)
                    continue;

                engineStorage.inventory.Clear();
                engineStorage.SendNetworkUpdate();
            }
        }

        private void AddEngineParts(ModularCar car, List<string> shortnames)
        {
            foreach (var child in car.children)
            {
                var engineModule = child as VehicleModuleEngine;
                if (engineModule == null)
                    continue;

                var engineStorage = engineModule.GetContainer() as EngineStorage;
                if (engineStorage == null || !engineStorage.inventory.IsEmpty())
                    continue;

                foreach (var x in shortnames) AddPartsToEngineStorage(engineStorage, x);

                engineModule.RefreshPerformanceStats(engineStorage);
            }
        }

        private void AddPartsToEngineStorage(EngineStorage engineStorage, string shortname)
        {
            if (engineStorage.inventory == null)
                return;

            var inventory = engineStorage.inventory;
            for (var i = 0; i < inventory.capacity; i++)
            {
                var item = inventory.GetSlot(i);
                if (item != null)
                    continue;

                // if (tier > 0)
                // {
                TryAddEngineItem(engineStorage, -1, shortname);
                // }
            }
        }

        private bool TryAddEngineItem(EngineStorage engineStorage, int slot, string shortname)
        {
            var component = ItemManager.FindItemDefinition(shortname);
            var item = ItemManager.Create(component);
            if (item == null)
                return false;

            item.MoveToContainer(engineStorage.inventory, slot, false);
            return true;
        }

        private bool GiveVehicle(BasePlayer player, VehicleInfoConfig vehicleSettings, bool needfuel = true)
        {
            var item = ItemManager.CreateByItemID(vehicleSettings.DeployableItemId, 1, vehicleSettings.Skin);
            item.name = vehicleSettings.Name + (needfuel ? "" : "   ");
            if (!player.inventory.GiveItem(item))
            {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity);
                return false;
            }

            return true;
        }

        private BaseEntity GetMinDistance(Vector3 position, IEnumerable<BaseEntity> entities)
        {
            BaseEntity result = null;
            var min = float.PositiveInfinity;

            var ents = entities.ToArray();

            foreach (var t in ents)
            {
                var dist = Vector3.Distance(position, t.transform.position);

                if (dist < min)
                {
                    result = t;
                    min = dist;
                }
            }

            return result;
        }

        private bool HasInConfig(BaseEntity entity, string key)
        {
            return _config.Vehicles.Any(x =>
                x.Key == key && x.Value.Skin == entity.skinID && x.Value.Prefab == entity.PrefabName &&
                x.Value.CanCallback);
        }

        #endregion

        #region UI

        private const string Layer = "ui.VehicleBuy.bg";

        #endregion

        #region Interface

        #region Data

        private void LoadSetupUI()
        {
            LoadFullscreenUI();

            LoadMenuUI();

            #region Adaptation

            if (UIFulScreen is not { Installed: true })
            {
                UIFulScreen = UserInterface.GenerateFullScreen();

                SaveFullscreenUI();
            }

            if (UIMenu is not { Installed: true })
            {
                UIMenu = UserInterface.GenerateUIMenuV2();

                SaveMenuUI();
            }

            #endregion
        }

        #region Fullscreen

        private FullscreenSetup UIFulScreen;

        private void LoadFullscreenUI()
        {
            LoadDataFromFile(ref UIFulScreen, $"{Name}/Template/Fullscreen");
        }

        private void SaveFullscreenUI()
        {
            SaveDataToFile(UIFulScreen, $"{Name}/Template/Fullscreen");
        }

        #endregion

        #region InMenu

        private UserInterface UIMenu;

        private void LoadMenuUI()
        {
            LoadDataFromFile(ref UIFulScreen, $"{Name}/Template/Menu");
        }

        private void SaveMenuUI()
        {
            SaveDataToFile(UIFulScreen, $"{Name}/Template/Menu");
        }

        #endregion

        #region Data.Helpers

        private void LoadDataFromFile<T>(ref T data, string filePath)
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<T>(filePath);
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            data ??= Activator.CreateInstance<T>();
        }

        private void SaveDataToFile<T>(T data, string filePath)
        {
            Interface.Oxide.DataFileSystem.WriteObject(filePath, data);
        }


        #endregion

        #endregion

        private const string
            MAX_PURCHASES_EXCEEDED = "MAX_PURCHASES_EXCEEDED",
            BUY_VEHICLE = "BUY_VEHICLE",
            BUTTON_BUY = "BUTTON_BUY";

        protected override void LoadDefaultMessages()
        {
            var en = new Dictionary<string, string>
            {
                [BUY_VEHICLE] = "BUY VEHICLE",
                [BUTTON_BUY] = "BUY",
                [MAX_PURCHASES_EXCEEDED] = "You have reached the maximum number of purchases for this vehicle.",
            };

            var ru = new Dictionary<string, string>
            {
                [BUY_VEHICLE] = "ПОКУПКА ТРАНСПОРТА",
                [BUTTON_BUY] = "КУПИТЬ",
                [MAX_PURCHASES_EXCEEDED] = "Вы достигли максимального количества покупок для этого транспорта.",
            };

            foreach (var (langKey, langSettings) in GetCustomLang())
                if (!string.IsNullOrEmpty(langKey) && !string.IsNullOrEmpty(langSettings.ru) &&
                    !string.IsNullOrEmpty(langSettings.en))
                {
                    if (ru.ContainsKey(langKey)) ru[langKey] = langSettings.ru;
                    else ru.TryAdd(langKey, langSettings.ru);

                    if (en.ContainsKey(langKey)) en[langKey] = langSettings.en;
                    else en.TryAdd(langKey, langSettings.en);
                }

            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        public Dictionary<string, LangCustom> GetCustomLang()
        {
            Dictionary<string, LangCustom> customLang = null;
            try
            {
                if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{nameof(VehicleBuy)}/CustomLang"))
                {
                    customLang =
                        Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, LangCustom>>(
                            $"{nameof(VehicleBuy)}/CustomLang");
                }
                else
                {
                    customLang = GetDefaultCustomLang();
                    Interface.Oxide.DataFileSystem.WriteObject($"{nameof(VehicleBuy)}/CustomLang", customLang);
                }
            }
            finally
            {
                if (customLang == null) customLang = GetDefaultCustomLang();
            }

            return customLang;
        }

        public Dictionary<string, LangCustom> GetDefaultCustomLang()
        {
            return new Dictionary<string, LangCustom>
            {
                ["scrap"] = new("металлолом", "scrap"),
                ["SRTEST"] = new("SRTEST", "SRTEST"),
                ["ECOTEST"] = new("ECOTEST", "ECOTEST"),
                ["Minicopter"] = new("Миникоптер", "Minicopter"),
                ["Scrap Transport Helicopter"] = new("Транспортный вертолет", "Scrap Transport Helicopter"),
                ["Attack Helicopter"] = new("Атакующий вертолет", "Attack Helicopter"),
                ["Car 2"] = new("Машина на 2 модуля", "Car for 2 modules"),
                ["Car 3"] = new("Машина на 3 модуля", "Car for 3 modules"),
                ["Car 4"] = new("Машина на 4 модуля", "Car for 4 modules"),
                ["TugBoat"] = new("Буксирное судно", "Tugboat"),
                ["RowBoat"] = new("Гребная лодка", "Rowboat"),
                ["RHIB"] = new("Катер", "Speedboat"),
                ["SoloSub"] = new("Одноместная подводная лодка", "Single-seat submarine"),
                ["DuoSub"] = new("Двухместная подводная лодка", "Two-person submarine"),
                ["Horse"] = new("Лошадь", "Horse"),
                ["SnowMobile"] = new("Снегоход", "SnowMobile"),
                ["Tomaha"] = new("Снегоход Tomaha", "SnowMobile Tomaha"),
                ["HotairBalloon"] = new("Воздушный шар на горячем воздухе", "HotairBalloon"),
                ["Recycler"] = new("Переработчик", "Recycler"),
                ["pedalbike"] = new("Велосипед с педалями", "Pedal bike"),
                ["motorbike"] = new("Мотоцикл", "Motorbike"),
                ["motorbike_sidecar"] = new("Мотоцикл с коляской", "Motorbike with sidecar")
            };
        }

        public static string GetMessage(string key, string userId, params object[] args)
        {
            return string.Format(Instance.lang.GetMessage(key, Instance, userId), args);
        }

        public void OpenVehicleBuy(BasePlayer player)
        {
            var container = new CuiElementContainer();

            #region Background

            container.Add(new CuiElement
            {
                Parent = "Overlay",
                Name = Layer,
                DestroyUi = Layer,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToCuiColor("#191919", 90),
                        Sprite = "assets/content/ui/UI.Background.TileTex.psd",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                        ImageType = Image.Type.Tiled
                    },
                    new CuiRectTransformComponent(),
                    new CuiNeedsCursorComponent(),
                    new CuiNeedsKeyboardComponent(),
                }
            });

            #endregion

            float x = UIFulScreen.Width / 2, y = UIFulScreen.Height / 2;
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Main",
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                        OffsetMin = $"-{x} -{y}", OffsetMax = $"{x} {y}"
                    },
                    new CuiNeedsCursorComponent()
                }
            });

            container.Add(UIFulScreen.Background.GetImage(Layer + ".Main", Layer + ".Background"));

            #region Button.Close

            container.Add(new CuiElement
            {
                Parent = Layer + ".Background",
                Name = Layer + ".ButtonClose.Background",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Close = Layer,
                        Color = UIFulScreen.ButtonClose.BackgroundClose.Color.Get(),
                        Sprite = UIFulScreen.ButtonClose.BackgroundClose.Sprite,
                        Material = UIFulScreen.ButtonClose.BackgroundClose.Material
                    },
                    UIFulScreen.ButtonClose.BackgroundClose.GetRectTransform(),
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer + ".ButtonClose.Background",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = UIFulScreen.ButtonClose.Color.Get(),
                        Sprite = UIFulScreen.ButtonClose.Sprite,
                        Material = UIFulScreen.ButtonClose.Material
                    },
                    UIFulScreen.ButtonClose.GetRectTransform(),
                }
            });

            #endregion

            AddContentVehicleBuy(player, ref container, UIFulScreen);

            CuiHelper.AddUi(player, container);
        }

        private CuiElementContainer API_OpenPlugin(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, "UI.Server.Panel.Content", "UI.Server.Panel.Content.Plugin", "UI.Server.Panel.Content.Plugin");

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, "UI.Server.Panel.Content.Plugin", Layer, Layer);

            container.Add(UIMenu.Background.GetImage(Layer, Layer + ".Background"));

            AddContentVehicleBuy(player, ref container, UIMenu);

            return container;
        }

        public void AddContentVehicleBuy(BasePlayer player, ref CuiElementContainer container, UserInterface setup)
        {
            var vehicles = _config.Vehicles.OrderBy(x => x.Value.Order)
                .Where(x => x.Value.Show && x.Value.CanPlayerBuy(player))
                .ToArray();

            #region Header

            container.Add(setup.HeaderPanel.Background.GetImage(Layer + ".Background", Layer + ".Title.Background"));
            container.Add(setup.HeaderPanel.Title.GetText(GetMessage(BUY_VEHICLE, player.UserIDString), Layer + ".Title.Background", Layer + ".Title"));
            if (setup.HeaderPanel.ShowLine)
                container.Add(setup.HeaderPanel.Line.GetImage(Layer + ".Title.Background", Layer + ".Title.Line"));

            #endregion

            #region ScrollView

            container.Add(
                setup.ContentPanel.Background.GetImage(Layer + ".Background", Layer + ".ScrollPanel.Background"));

            var count = vehicles.Length;
            var countLotsVertical = count / setup.LotsOnString;
            if (count > countLotsVertical * setup.LotsOnString) countLotsVertical++;
            var heightContent =
                countLotsVertical * setup.LotHeight + countLotsVertical * setup.YIndent + setup.YIndent;

            var contentTransform = new CuiRectTransformComponent
            {
                AnchorMin = "0 1",
                AnchorMax = "1 1",
                OffsetMin = $"0 -{heightContent}",
                OffsetMax = "0 0"
            };
            container.Add(new CuiElement
            {
                Parent = Layer + ".ScrollPanel.Background",
                Name = Layer + ".ScrollContent",
                Components =
                {
                    new CuiImageComponent(){Color = "0 0 0 0"},
                    setup.ContentPanel.Scroll.GetScrollView(contentTransform),
                    new CuiRectTransformComponent
                        {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"},
                }
            });

            #endregion

            #region List Vehicles

            var offsetX = 0f;
            var offsetY = 0f;

            for (var i = 0; i < count; i++)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer + ".ScrollContent",
                    Name = Layer + $".Lot.Background.{i}",
                    Components =
                    {
                        setup.LotPanel.Background.GetImageComponent(),
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1", AnchorMax = "0 1",
                            OffsetMin = $"{offsetX} {offsetY - setup.LotHeight}",
                            OffsetMax = $"{offsetX + setup.LotWidth} {offsetY}",
                        }
                    }
                });

                var product = vehicles[i];

                container.Add(new CuiElement
                {
                    Parent = Layer + $".Lot.Background.{i}",
                    Name = Layer + $".Icon.{i}",
                    Components =
                    {
                        new CuiRawImageComponent
                            {Color = new IColor("#FFFFFF").Get(), Png = GetImage(product.Key + ".image")},
                        setup.LotPanel.LotImage.GetRectTransform()
                    }
                });
                if (setup.LotPanel.ShowName)
                    container.Add(setup.LotPanel.LotName.GetText(GetMessage(product.Value.Name, player.UserIDString),
                        Layer + $".Lot.Background.{i}", Layer + $".Lot.Title.{i}"));

                container.AddRange(setup.LotPanel.ButtonBuy.GetButton(GetMessage(BUTTON_BUY, player.UserIDString),
                    $"vb_buy {product.Key}", Layer + $".Lot.Background.{i}", Layer + $".Button.Buy.{i}"));

                container.Add(setup.LotPanel.CurrencyName.GetText(GetMessage(GetNameCurrency(product.Value), player.UserIDString),
                    Layer + $".Lot.Background.{i}", Layer + $".Cost.Currency.{i}"));

                container.Add(setup.LotPanel.PriceText.GetText(product.Value.Price.ToString(),
                    Layer + $".Lot.Background.{i}", Layer + $"$Cost.Value.{i}"));

                #region Calculate Position

                if ((i + 1) % setup.LotsOnString == 0)
                {
                    offsetX = 0f;
                    offsetY = offsetY - setup.LotHeight - setup.YIndent;
                }
                else
                {
                    offsetX += setup.LotWidth + setup.XIndent;
                }

                #endregion
            }

            #endregion
        }

        public string GetNameCurrency(VehicleInfoConfig vehicleInfo)
        {
            switch (vehicleInfo.SellCurrency)
            {
                case 0:
                    {
                        var itemDef = ItemManager.FindItemDefinition(vehicleInfo.Shortname);

                        return itemDef == null ? "UNKNOWN" : itemDef.displayName.english.ToUpper();
                    }
                case 1: return _config.CurrencyName;
                case 2: return _config.CurrencyNameSR;
                case 3: return _config.CurrencyNameBS;
            }

            return "ERROR";
        }
        private static List<ulong> _customents = new List<ulong>();
        private static void GetNewEnts()
        {
            foreach (var ent in BaseNetworkable.serverEntities)
            {
                BaseEntity entity = ent as BaseEntity;
                if (ent is BaseVehicle baseVehicle)
                {
                    if (baseVehicle.ShortPrefabName.Contains("customVehicle"))
                        _customents.Add(baseVehicle.net.ID.Value);
                }
            }
            if (!ServerMgr.Instance.IsInvoking("NewEnts"))
                ServerMgr.Instance.InvokeRepeating(() => GetNewEnts(), 0, UnityEngine.Random.Range(0, 360f));
        }

        public struct LangCustom
        {
            public string ru;
            public string en;

            public LangCustom(string ru, string en)
            {
                this.ru = ru;
                this.en = en;
            }
        }

        public class FullscreenSetup : UserInterface
        {
            [JsonProperty(LangRu ? "Цвет фона экрана" : "Screen background color")]
            public IColor BackgroundScreen;

            [JsonProperty("Material")]
            public string Material;

            [JsonProperty("Sprite")]
            public string Sprite;

            [JsonProperty(LangRu ? "Кнопка закрытия" : "Button Close")]
            public ButtonCloseSetup ButtonClose = new();
        }

        public class ButtonCloseSetup : InterfacePosition
        {
            [JsonProperty(PropertyName = "Background Button Close")]
            public ImageSettings BackgroundClose = new();

            [JsonProperty(PropertyName = "Color Button Close")]
            public IColor Color = IColor.CreateTransparent();

            [JsonProperty(PropertyName = "Sprite Button Close")]
            public string Sprite = string.Empty;

            [JsonProperty(PropertyName = "Material Button Close")]
            public string Material = string.Empty;
        }

        public class UserInterface
        {
            #region Fields

            [JsonProperty(PropertyName = LangRu ? "Установлена?" : "Installed?")]
            public bool Installed = false;

            [JsonProperty(PropertyName = LangRu ? "Высота главной панели" : "Height of the main panel")]
            public float Height;

            [JsonProperty(PropertyName = LangRu ? "Ширина главной панели" : "Width of the main panel")]
            public float Width;

            [JsonProperty(PropertyName = LangRu ? "Высота лота" : "Lot height")]
            public float LotHeight;

            [JsonProperty(PropertyName = LangRu ? "Ширина лота" : "Lot width")]
            public float LotWidth;

            [JsonProperty(PropertyName = LangRu ? "Кол-во лотов на строке" : "Number of lots per line")]
            public int LotsOnString;

            [JsonProperty(PropertyName = LangRu ? "Отступы по горизонтали" : "Horizontal margins")]
            public float XIndent;

            [JsonProperty(PropertyName = LangRu ? "Отступы по вертикали" : "Vertical margins")]
            public float YIndent;

            [JsonProperty(PropertyName = "Background main panel")]
            public ImageSettings Background = new();

            [JsonProperty(PropertyName = LangRu ? "Настройки заголовка" : "Header Settings")]
            public PanelHeaderUI HeaderPanel;

            [JsonProperty(PropertyName = LangRu ? "Настройки панели лотов" : "Lot Panel Settings")]
            public PanelContentUI ContentPanel;

            [JsonProperty(PropertyName = LangRu ? "Настройки лота" : "Lot Settings")]
            public PanelLotUI LotPanel;

            #endregion

            #region Classes

            public class PanelHeaderUI
            {
                [JsonProperty(PropertyName = "Background")]
                public ImageSettings Background = new();

                [JsonProperty(PropertyName = "Title")] public TextSettings Title = new();

                [JsonProperty(PropertyName = "Show Line?")]
                public bool ShowLine;

                [JsonProperty(PropertyName = "Line")] public ImageSettings Line = new();
            }

            public class PanelContentUI
            {
                [JsonProperty(PropertyName = "Background")]
                public ImageSettings Background = new();

                [JsonProperty(PropertyName = "Scroll View")]
                public ScrollViewUI Scroll = new();
            }

            public class PanelLotUI
            {
                [JsonProperty(PropertyName = "Background")]
                public ImageSettings Background = new();

                [JsonProperty(PropertyName = "Lot Image")]
                public InterfacePosition LotImage = new();

                [JsonProperty(PropertyName = "Show Name?")]
                public bool ShowName;

                [JsonProperty(PropertyName = "Lot Name")]
                public TextSettings LotName = new();

                [JsonProperty(PropertyName = "Lot Currency")]
                public TextSettings CurrencyName = new();

                [JsonProperty(PropertyName = "Lot Price")]
                public TextSettings PriceText = new();

                [JsonProperty(PropertyName = "Lot Button Buy")]
                public ButtonSettings ButtonBuy = new();
            }

            #endregion

            #region Templates

            public static FullscreenSetup GenerateFullScreen()
            {
                return new FullscreenSetup
                {
                    BackgroundScreen = new IColor("#191919", 90),
                    Sprite = "assets/content/ui/UI.Background.TileTex.psd",
                    Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",
                    ButtonClose = new ButtonCloseSetup
                    {
                        BackgroundClose = new ImageSettings
                        {
                            AnchorMin = "1 1",
                            AnchorMax = "1 1",
                            OffsetMin = "-40 -40",
                            OffsetMax = "0 0",
                            Color = new IColor("#E44028", 100),
                            Sprite = "assets/content/ui/UI.Background.TileTex.psd"
                        },
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-9 -9",
                        OffsetMax = "9 9",
                        Color = new IColor("#FFFFFF", 100),
                        Sprite = "assets/icons/close.png",
                        Material = null
                    },
                    Height = 470,
                    Width = 850,
                    LotHeight = 205,
                    LotWidth = 145,
                    LotsOnString = 5,
                    XIndent = 15,
                    YIndent = 20,
                    Background = new ImageSettings
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1",
                        OffsetMin = "0 0",
                        OffsetMax = "0 0",
                        Color = new IColor("#191919", 50),
                        Material = "assets/content/ui/uibackgroundblur.mat"
                    },

                    HeaderPanel = new PanelHeaderUI
                    {
                        Background = new ImageSettings
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "1 1",
                            OffsetMin = "0 -40",
                            OffsetMax = "-40 0",
                            Color = new IColor("#494949", 100),
                            Sprite = "assets/content/ui/UI.Background.Transparent.LinearLTR.tga"
                        },
                        Title = new TextSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "20 0",
                            OffsetMax = "0 0",
                            Align = TextAnchor.MiddleLeft,
                            IsBold = true,
                            FontSize = 18,
                            Color = new IColor("#E2DBD3", 100)
                        },
                        ShowLine = false,
                        Line = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0",
                            OffsetMin = "0 -2",
                            OffsetMax = "0 0",
                            Color = new IColor("#373737", 50)
                        }
                    },
                    ContentPanel = new PanelContentUI
                    {
                        Background = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "20 50",
                            OffsetMax = "-20 -60",
                            Color = IColor.CreateTransparent()
                        },
                        Scroll = new ScrollViewUI
                        {
                            Scrollbar = new ScrollViewUI.ScrollBarSettings
                            {
                                Size = 3,
                                HandleColor = new IColor("#D74933", 100),
                                HighlightColor = new IColor("#D74933", 100),
                                PressedColor = new IColor("#D74933", 100),
                                HandleSprite = "assets/content/ui/UI.Background.TileTex.psd",
                                TrackColor = new IColor("#38393F", 40),
                                TrackSprite = "assets/content/ui/UI.Background.TileTex.psd"
                            },
                            ScrollType = ScrollType.Vertical,
                            MovementType = ScrollRect.MovementType.Elastic,
                            Elasticity = 0.1f,
                            DecelerationRate = 1,
                            ScrollSensitivity = 10
                        }
                    },
                    LotPanel = new PanelLotUI
                    {
                        Background = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0",
                            Color = new IColor("#696969", 15),
                            Material = "assets/content/ui/namefontmaterial.mat",
                            Sprite = "assets/content/ui/UI.Background.Tile.psd"
                        },
                        ShowName = true,
                        LotName = new TextSettings
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "1 1",
                            OffsetMin = "10 -160",
                            OffsetMax = "0 -136",
                            Align = TextAnchor.UpperLeft,
                            IsBold = true,
                            FontSize = 13,
                            Color = new IColor("#E2DBD3")
                        },
                        LotImage = new InterfacePosition
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = "-66 -135",
                            OffsetMax = "66 -11"
                        },
                        ButtonBuy = new ButtonSettings
                        {
                            AnchorMin = "1 0",
                            AnchorMax = "1 0",
                            OffsetMin = "-74 10",
                            OffsetMax = "-10 38",
                            Align = TextAnchor.MiddleCenter,
                            IsBold = true,
                            FontSize = 14,
                            Color = new IColor("#E2DBD3"),
                            ButtonColor = new IColor("#D74933"),
                            Material = "assets/content/ui/namefontmaterial.mat",
                            Sprite = "assets/content/ui/UI.Background.TileTex.psd"
                        },
                        CurrencyName = new TextSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "10 24",
                            OffsetMax = "86 38",
                            Align = TextAnchor.UpperLeft,
                            IsBold = true,
                            FontSize = 10,
                            Color = new IColor("#cccccc", 50)
                        },
                        PriceText = new TextSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "10 10",
                            OffsetMax = "86 32",
                            Align = TextAnchor.LowerLeft,
                            IsBold = true,
                            FontSize = 16,
                            Color = new IColor("#cccccc")
                        }
                    }
                };
            }

            public static UserInterface GenerateUIMenuV1()
            {
                return new UserInterface
                {
                    Height = 470,
                    Width = 850,
                    LotHeight = 205,
                    LotWidth = 155,
                    LotsOnString = 7,
                    XIndent = 15,
                    YIndent = 15,
                    Background = new ImageSettings
                    {
                        AnchorMin = "0.5 1",
                        AnchorMax = "0.5 1",
                        OffsetMin = "-600 -550",
                        OffsetMax = "600 0",
                        Color = new IColor("#ffffff", 0),
                        Sprite = "assets/content/ui/UI.Background.TileTex.psd"
                    },

                    HeaderPanel = new PanelHeaderUI
                    {
                        Background = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0",
                            Color = new IColor("#ffffff", 0)
                        },
                        Title = new TextSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0",
                            Align = TextAnchor.UpperLeft,
                            IsBold = true,
                            FontSize = 32,
                            Color = new IColor("#ce432d", 90)
                        },
                        ShowLine = false,
                        Line = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0",
                            OffsetMin = "0 -2",
                            OffsetMax = "0 0",
                            Color = new IColor("#373737", 50)
                        }
                    },
                    ContentPanel = new PanelContentUI
                    {
                        Background = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "0 5",
                            OffsetMax = "0 -5",
                            Color = IColor.CreateTransparent()
                        },
                        Scroll = new ScrollViewUI
                        {
                            Scrollbar = new ScrollViewUI.ScrollBarSettings
                            {
                                Size = 3,
                                HandleColor = new IColor("#D74933"),
                                HighlightColor = new IColor("#D74933"),
                                PressedColor = new IColor("#D74933"),
                                HandleSprite = "assets/content/ui/UI.Background.TileTex.psd",
                                TrackColor = new IColor("#38393F", 40),
                                TrackSprite = "assets/content/ui/UI.Background.TileTex.psd"
                            },
                            ScrollType = ScrollType.Vertical,
                            MovementType = ScrollRect.MovementType.Elastic,
                            Elasticity = 0.1f,
                            DecelerationRate = 1,
                            ScrollSensitivity = 10
                        }
                    },
                    LotPanel = new PanelLotUI
                    {
                        Background = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0",
                            Color = new IColor("#696969", 15),
                            Material = "assets/content/ui/namefontmaterial.mat",
                            Sprite = "assets/content/ui/UI.Background.Tile.psd"
                        },
                        ShowName = true,
                        LotName = new TextSettings
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "1 1",
                            OffsetMin = "10 -160",
                            OffsetMax = "0 -136",
                            Align = TextAnchor.UpperLeft,
                            IsBold = true,
                            FontSize = 13,
                            Color = new IColor("#E2DBD3")
                        },
                        LotImage = new InterfacePosition
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = "-66 -135",
                            OffsetMax = "66 -11"
                        },
                        ButtonBuy = new ButtonSettings
                        {
                            AnchorMin = "1 0",
                            AnchorMax = "1 0",
                            OffsetMin = "-74 10",
                            OffsetMax = "-10 38",
                            Align = TextAnchor.MiddleCenter,
                            IsBold = true,
                            FontSize = 14,
                            Color = new IColor("#E2DBD3"),
                            ButtonColor = new IColor("#D74933"),
                            Material = "assets/content/ui/namefontmaterial.mat",
                            Sprite = "assets/content/ui/UI.Background.TileTex.psd"
                        },
                        CurrencyName = new TextSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "10 24",
                            OffsetMax = "86 38",
                            Align = TextAnchor.UpperLeft,
                            IsBold = true,
                            FontSize = 10,
                            Color = new IColor("#cccccc", 50)
                        },
                        PriceText = new TextSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "10 10",
                            OffsetMax = "86 32",
                            Align = TextAnchor.LowerLeft,
                            IsBold = true,
                            FontSize = 16,
                            Color = new IColor("#cccccc")
                        }
                    }
                };
            }

            public static UserInterface GenerateUIMenuV2()
            {
                return new UserInterface
                {
                    Height = 470,
                    Width = 850,
                    LotHeight = 205,
                    LotWidth = 155,
                    LotsOnString = 5,
                    XIndent = 25,
                    YIndent = 20,
                    Background = new ImageSettings
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = "0 -600",
                        OffsetMax = "940 0",
                        Color = new IColor("#ffffff", 0),
                        Sprite = "assets/content/ui/UI.Background.TileTex.psd"
                    },

                    HeaderPanel = new PanelHeaderUI
                    {
                        Background = new ImageSettings
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "1 1",
                            OffsetMin = "40 -70",
                            OffsetMax = "-10 -20",
                            Color = new IColor("#ffffff", 0)
                        },
                        Title = new TextSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0",
                            Align = TextAnchor.UpperLeft,
                            IsBold = true,
                            FontSize = 32,
                            Color = new IColor("#ce432d", 90)
                        },
                        ShowLine = true,
                        Line = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0",
                            OffsetMin = "0 -2",
                            OffsetMax = "0 0",
                            Color = new IColor("#373737", 50)
                        }
                    },
                    ContentPanel = new PanelContentUI
                    {
                        Background = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "40 70",
                            OffsetMax = "-10 -90",
                            Color = IColor.CreateTransparent()
                        },
                        Scroll = new ScrollViewUI
                        {
                            Scrollbar = new ScrollViewUI.ScrollBarSettings
                            {
                                Size = 3,
                                HandleColor = new IColor("#D74933"),
                                HighlightColor = new IColor("#D74933"),
                                PressedColor = new IColor("#D74933"),
                                HandleSprite = "assets/content/ui/UI.Background.TileTex.psd",
                                TrackColor = new IColor("#38393F", 40),
                                TrackSprite = "assets/content/ui/UI.Background.TileTex.psd"
                            },
                            ScrollType = ScrollType.Vertical,
                            MovementType = ScrollRect.MovementType.Elastic,
                            Elasticity = 0.1f,
                            DecelerationRate = 1,
                            ScrollSensitivity = 10
                        }
                    },
                    LotPanel = new PanelLotUI
                    {
                        Background = new ImageSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "0 0",
                            OffsetMax = "0 0",
                            Color = new IColor("#696969", 15),
                            Material = "assets/content/ui/namefontmaterial.mat",
                            Sprite = "assets/content/ui/UI.Background.Tile.psd"
                        },
                        ShowName = true,
                        LotName = new TextSettings
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "1 1",
                            OffsetMin = "10 -160",
                            OffsetMax = "0 -136",
                            Align = TextAnchor.UpperLeft,
                            IsBold = true,
                            FontSize = 13,
                            Color = new IColor("#E2DBD3")
                        },
                        LotImage = new InterfacePosition
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = "-66 -135",
                            OffsetMax = "66 -11"
                        },
                        ButtonBuy = new ButtonSettings
                        {
                            AnchorMin = "1 0",
                            AnchorMax = "1 0",
                            OffsetMin = "-74 10",
                            OffsetMax = "-10 38",
                            Align = TextAnchor.MiddleCenter,
                            IsBold = true,
                            FontSize = 14,
                            Color = new IColor("#E2DBD3"),
                            ButtonColor = new IColor("#D74933"),
                            Material = "assets/content/ui/namefontmaterial.mat",
                            Sprite = "assets/content/ui/UI.Background.TileTex.psd"
                        },
                        CurrencyName = new TextSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "10 24",
                            OffsetMax = "86 38",
                            Align = TextAnchor.UpperLeft,
                            IsBold = true,
                            FontSize = 10,
                            Color = new IColor("#cccccc", 50)
                        },
                        PriceText = new TextSettings
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0 0",
                            OffsetMin = "10 10",
                            OffsetMax = "86 32",
                            Align = TextAnchor.LowerLeft,
                            IsBold = true,
                            FontSize = 16,
                            Color = new IColor("#cccccc")
                        }
                    }
                };
            }

            #endregion
        }

        #endregion

        #region Commands

        [ChatCommand("callback")]
        private void cmdCallback(BasePlayer player, string command, string[] args)
        {
            if (!player.IPlayer.HasPermission(PERM_RECALL))
                return;

            if (args.IsNullOrEmpty())
            {
                player.ChatMessage("Usage: /callback vehicleName");
                return;
            }

            var name = args[0];

            var playerEntities = BaseNetworkable.serverEntities.OfType<BaseEntity>()
                .Where(x => x.OwnerID == player.userID && HasInConfig(x, name));
            if (playerEntities == null || !playerEntities.Any())
            {
                player.ChatMessage("No vehicle to callback!");
                return;
            }

            var entity = GetMinDistance(player.transform.position, playerEntities);
            if (entity == null)
            {
                player.ChatMessage("No vehicle to callback!");
                return;
            }

            var product = _config.Vehicles
                .FirstOrDefault(x => x.Value.Prefab == entity.PrefabName && x.Value.Skin == entity.skinID).Value;

            var balance = GetBalance(player, product);

            if (product.RecallCostNeed)
            {
                if (balance < product.RecallCost)
                {
                    player.ChatMessage(
                        $"Not enough balance! Need - {product.RecallCost - GetBalance(player, product)}");
                    return;
                }

                Withdraw(player, product.RecallCost, product.SellCurrency, product.Shortname);
            }

            var newCarPosition = new Vector3(player.transform.position.x + Random.Range(3f, 5f), 0,
                player.transform.position.z + Random.Range(3f, 5f));
            newCarPosition.y = TerrainMeta.HeightMap.GetHeight(newCarPosition) + 1f;

            entity.transform.position = newCarPosition;
            entity.SendNetworkUpdate();
            player.ChatMessage($"Vehicle {product.Name} was recalled");
        }

        [ChatCommand("pickup")]
        private void cmdPickup(BasePlayer player)
        {
            if (!player.IPlayer.HasPermission(PERM_PICKUP))
                return;

            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, _config.PickupRadius, -1))
            {
                var rhEntity = hit.GetEntity();
                if (rhEntity != null && rhEntity.OwnerID == player.userID)
                {
                    var product = _config.Vehicles.FirstOrDefault(z =>
                        z.Value.Prefab == rhEntity.PrefabName && z.Value.Skin == rhEntity.skinID);
                    if (!product.Equals(default(KeyValuePair<string, VehicleInfoConfig>)) && product.Value.CanPickup)
                    {
                        if (player.inventory.containerMain.IsFull())
                        {
                            player.ChatMessage("Inventory is full. Cannot pick up the vehicle.");
                            return;
                        }

                        if (product.Value.PickupPrice > 0)
                        {
                            if (product.Value.PickupPrice > GetBalance(player, product.Value))
                            {
                                player.ChatMessage(
                                    $"Not enough balance! Need - {product.Value.PickupPrice - GetBalance(player, product.Value)}");
                                return;
                            }

                            Withdraw(player, product.Value.PickupPrice, product.Value.SellCurrency,
                                product.Value.Shortname);
                        }

                        if (!GiveVehicle(player, product.Value, false))
                        {
                            player.ChatMessage("Unable to give vehicle to player.");
                            return;
                        }

                        if (rhEntity.TryGetComponent<ModularCar>(out var component))
                            GetOutParts(component);

                        var baseVehicle = rhEntity.GetComponent<BaseVehicle>();
                        if (baseVehicle != null)
                        {
                            var fuelSystem = baseVehicle.GetFuelSystem();
                            if (fuelSystem != null)
                            {
                                var fuelAmount = fuelSystem.GetFuelAmount();
                                if (fuelAmount > 0)
                                {
                                    var fuelItem = ItemManager.CreateByName("lowgradefuel", fuelAmount);
                                    if (fuelItem != null && fuelItem.amount > 0)
                                        if (!player.inventory.GiveItem(fuelItem))
                                        {
                                            player.ChatMessage($"{fuelItem.amount} fuel was dropped at your feet");
                                            fuelItem.Drop(player.inventory.containerMain.dropPosition,
                                                player.inventory.containerMain.dropVelocity);
                                        }
                                }
                            }
                        }

                        rhEntity.Kill();
                        player.ChatMessage($"Vehicle {product.Value.Name} was picked up");
                    }
                }
                else
                {
                    player.ChatMessage("Vehicle not found or cannot be picked up");
                }
            }
            else
            {
                player.ChatMessage("Vehicle not found");
            }
        }

        [ConsoleCommand("vehiclebuy.template")]
        private void CmdConsoleTemplate(ConsoleSystem.Arg arg)
        {
            if (!arg.IsServerside) return;

            switch (arg.GetString(0))
            {
                case "fullscreen":
                    {
                        UIFulScreen = UserInterface.GenerateFullScreen();

                        SaveFullscreenUI();

                        SendReply(arg, "Fullscreen UI has been successfully generated.");
                        break;
                    }

                case "inmenu":
                    {
                        var template = arg.GetInt(1);
                        if (template != 1 && template != 2)
                        {
                            SendReply(arg, "Error syntax! Usage: vehiclebuy.template inmenu [1/2]");
                            return;
                        }

                        UIMenu = template switch
                        {
                            1 => UserInterface.GenerateUIMenuV1(),
                            2 => UserInterface.GenerateUIMenuV2(),
                            _ => UIMenu
                        };

                        SaveMenuUI();

                        SendReply(arg, $"Menu UI version {template} has been successfully generated.");
                        break;
                    }

                default:
                    {
                        var sb = Pool.Get<StringBuilder>();
                        try
                        {
                            sb.Append("VehicleBuy Template Settings: ");
                            sb.AppendLine();
                            sb.AppendLine("- vehiclebuy.template fullscreen - reset fullscreen template");
                            sb.AppendLine("- vehiclebuy.template inmenu 1 - set in-menu template for ServerPanel template V1");
                            sb.AppendLine("- vehiclebuy.template inmenu 2 - set in-menu template for ServerPanel template V2");

                            SendReply(arg, sb.ToString());
                        }
                        finally
                        {
                            Pool.FreeUnmanaged(ref sb);
                        }

                        return;
                    }
            }
        }

        [ConsoleCommand("vehiclebuy.images")]
        private void CmdConsoleImages(ConsoleSystem.Arg arg)
        {
            if (!arg.IsServerside) return;

            switch (arg.GetString(0))
            {
                case "use_offline":
                    {
                        foreach (var (prefab, vehicleSettings) in _config.Vehicles)
                        {
                            vehicleSettings.Image = vehicleSettings.Image.Replace("https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/", "MeventImages/");
                        }

                        SaveConfig();

                        SendReply(arg, "Images has been successfully updated to offline");
                        break;
                    }

                case "use_online":
                    {
                        foreach (var (prefab, vehicleSettings) in _config.Vehicles)
                        {
                            vehicleSettings.Image = vehicleSettings.Image.Replace("MeventImages/", "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/");
                        }

                        SaveConfig();

                        SendReply(arg, "Images has been successfully updated to offline");
                        break;
                    }

                default:
                    {
                        var sb = Pool.Get<StringBuilder>();
                        try
                        {
                            sb.Append("VehicleBuy Images Info: ");
                            sb.AppendLine("- vehiclebuy.images use_offline - update images to offline usage");
                            sb.AppendLine("- vehiclebuy.images use_online - update images to online usage");

                            SendReply(arg, sb.ToString());
                        }
                        finally
                        {
                            Pool.FreeUnmanaged(ref sb);
                        }

                        break;
                    }
            }
        }

        [ConsoleCommand("vb_buy")]
        private void cmdBuy(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || !arg.HasArgs())
                return;

            if (!permission.UserHasPermission(player.UserIDString, PERM_USE))
                return;

            var vehicle = arg.Args[0];
            if (!_config.Vehicles.TryGetValue(vehicle, out var product))
                return;

            if (!product.Show)
                return;

            if (!product.CanPlayerBuy(player))
                return;

            if (IsCooldown(player, vehicle))
            {
                var timespawn = TimeSpan.FromSeconds(GetCooldown(player, vehicle));

                player.ChatMessage($"Vehicle is on cooldown! Remaining - {timespawn:hh\\:mm\\:ss}");
                return;
            }

            if (Collect(player, product, vehicle))
            {
                SetVehiclePurchased(player, vehicle);
                SetCooldown(player, vehicle, product.Cooldown);
                if (product.UseSoundOnPurchase)
                    SendEffect(player, PURCHASE_EFFECT);
                GiveVehicle(player, product);
            }
            else
            {
                player
                    .ChatMessage($"Not enough money for buy vehicle! Need - {GetRemainingCost(player, product)}");
            }
        }

        private void cmdVehicleBuy(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERM_USE))
            {
                player.ChatMessage("[Error] You do not have access to this command!");
                return;
            }

            OpenVehicleBuy(player);
        }

        private void SendMessage(IPlayer player, string message)
        {
            if (player.IsServer)
            {
                PrintWarning("\n\n" + message);
                return;
            }

            player.Message(message);
        }

        private void CmdAddVehicle(IPlayer user, string command, string[] args)
        {
            if (!user.IsServer && !user.IsAdmin)
            {
                SendMessage(user, "[Error] You do not have access to this command!");
                return;
            }

            if (args.Length < 1)
            {
                SendMessage(user,
                    $"[Error] Enter {command} steamid/nickname\n[Example] {command} Jjj\n[Example] {command} 76561198311233564");
                // PrintError(
                // );
                return;
            }

            var player = BasePlayer.Find(args[0]);
            if (player == null)
            {
                SendMessage(user, $"[Error] Unable to find player {args[0]}");
                return;
            }

            var vehicleSettings = _config.Vehicles.FirstOrDefault(s => s.Value.Command == command);

            if (vehicleSettings.Value == null)
            {
                SendMessage(user, "Undefined vehicle!");
                return;
            }

            GiveVehicle(player, vehicleSettings.Value);
        }

        #endregion

        #region Hooks

        #region LockHelpers

        private static BaseLock GetVehicleLock(BaseEntity vehicle)
        {
            return vehicle.GetSlot(BaseEntity.Slot.Lock) as BaseLock;
        }

        private bool IsPlayerAuthorizedToCodeLock(ulong userID, CodeLock codeLock)
        {
            return codeLock.whitelistPlayers.Contains(userID)
                   || codeLock.guestPlayers.Contains(userID);
        }

        private bool IsPlayerAuthorizedToLock(BasePlayer player, BaseLock baseLock)
        {
            return (baseLock as KeyLock)?.HasLockPermission(player)
                   ?? IsPlayerAuthorizedToCodeLock(player.userID, baseLock as CodeLock);
        }

        private object CanPlayerInteractWithVehicle(BasePlayer player, BaseEntity vehicle, bool provideFeedback = true)
        {
            if (player == null || vehicle == null)
                return null;

            var baseLock = GetVehicleLock(vehicle);
            if (baseLock == null || !baseLock.IsLocked())
                return null;

            if (CanPlayerBypassLock(player, baseLock, provideFeedback))
                return null;

            return false;
        }

        private BaseEntity GetParentVehicle(BaseEntity entity)
        {
            var parent = entity.GetParentEntity();
            if (parent == null)
                return null;

            // Check for a vehicle module first since they are considered vehicles.
            var parentModule = parent as BaseVehicleModule;
            if (parentModule != null)
                return parentModule.Vehicle;

            if (parent is HotAirBalloon || parent is BaseVehicle)
                return parent;

            return _vehicleInfoManager.GetCustomVehicleParent(entity);
        }

        private object CanPlayerInteractWithParentVehicle(BasePlayer player, BaseEntity entity,
            bool provideFeedback = true)
        {
            return CanPlayerInteractWithVehicle(player, GetParentVehicle(entity), provideFeedback);
        }

        #endregion

        #region Lock Info

        private class LockInfo
        {
            public int ItemId;
            public string Prefab;
            public string PreHookName;

            public ItemDefinition ItemDefinition =>
                ItemManager.FindItemDefinition(ItemId);

            public ItemBlueprint Blueprint =>
                ItemManager.FindBlueprint(ItemDefinition);
        }

        private readonly LockInfo LockInfo_CodeLock = new()
        {
            ItemId = 1159991980,
            Prefab = "assets/prefabs/locks/keypad/lock.code.prefab",
            PreHookName = "CanDeployVehicleCodeLock"
        };

        private readonly LockInfo LockInfo_KeyLock = new()
        {
            ItemId = -850982208,
            Prefab = "assets/prefabs/locks/keylock/lock.key.prefab",
            PreHookName = "CanDeployVehicleKeyLock"
        };

        #endregion

        private object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            // Don't lock taxi modules
            if (!(entity as ModularCarSeat)?.associatedSeatingModule?.DoorsAreLockable ?? false)
                return null;

            return CanPlayerInteractWithParentVehicle(player, entity);
        }

        private object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            // Don't lock taxi module shop fronts
            if (container is ModularVehicleShopFront)
                return null;

            return CanPlayerInteractWithParentVehicle(player, container);
        }

        private object CanLootEntity(BasePlayer player, ContainerIOEntity container)
        {
            return CanPlayerInteractWithParentVehicle(player, container);
        }

        private object CanLootEntity(BasePlayer player, RidableHorse horse)
        {
            return CanPlayerInteractWithVehicle(player, horse);
        }

        private object CanLootEntity(BasePlayer player, ModularCarGarage carLift)
        {
            if (carLift == null
                || !carLift.PlatformIsOccupied)
                return null;

            return CanPlayerInteractWithVehicle(player, carLift.carOccupant);
        }

        private object OnHorseLead(RidableHorse horse, BasePlayer player)
        {
            return CanPlayerInteractWithVehicle(player, horse);
        }

        private object OnHotAirBalloonToggle(HotAirBalloon hab, BasePlayer player)
        {
            return CanPlayerInteractWithVehicle(player, hab);
        }

        private object OnSwitchToggle(ElectricSwitch electricSwitch, BasePlayer player)
        {
            if (electricSwitch == null)
                return null;

            var autoTurret = electricSwitch.GetParentEntity() as AutoTurret;
            if (autoTurret != null)
                return CanPlayerInteractWithParentVehicle(player, autoTurret);

            return null;
        }

        private object OnTurretAuthorize(AutoTurret entity, BasePlayer player)
        {
            return CanPlayerInteractWithParentVehicle(player, entity);
        }

        private object OnTurretTarget(AutoTurret autoTurret, BasePlayer player)
        {
            if (autoTurret == null || player == null || player.UserIDString == null)
                return null;

            var turretParent = autoTurret.GetParentEntity();
            var vehicle = turretParent as BaseVehicle ?? (turretParent as BaseVehicleModule)?.Vehicle;
            if (vehicle == null)
                return null;

            var baseLock = GetVehicleLock(vehicle);
            if (baseLock == null)
                return null;

            if (CanPlayerBypassLock(player, baseLock, false))
                return false;

            return null;
        }

        private object CanSwapToSeat(BasePlayer player, ModularCarSeat carSeat)
        {
            // Don't lock taxi modules
            if (!carSeat.associatedSeatingModule.DoorsAreLockable)
                return null;

            return CanPlayerInteractWithParentVehicle(player, carSeat, false);
        }

        private object OnVehiclePush(BaseVehicle vehicle, BasePlayer player)
        {
            return CanPlayerInteractWithVehicle(player, vehicle);
        }

        private void OnEntityKill(BaseLock baseLock)
        {
            var vehicle = GetParentVehicle(baseLock);
            if (vehicle == null)
                return;

            _lockedVehicleTracker.OnLockRemoved(vehicle);
        }

        private void OnEntityTakeDamage(BaseEntity entity, HitInfo info)
        {
            if (entity == null || info == null ||
                !info.damageTypes.Has(DamageType.Decay) ||
                !_config.Vehicles.Values.Any(vehicle => vehicle.Prefab == entity.PrefabName && vehicle.Skin == entity.skinID))
                return;

            info.damageTypes.ScaleAll(0f);
        }

        // Handle the case where a cockpit is removed but the car remains
        // If a lock is present, either move the lock to another cockpit or destroy it
        private void OnEntityKill(VehicleModuleSeating seatingModule)
        {
            if (seatingModule == null || !seatingModule.HasADriverSeat())
                return;

            var car = seatingModule.Vehicle as ModularCar;
            if (car == null)
                return;

            var baseLock = seatingModule.GetComponentInChildren<BaseLock>();
            if (baseLock == null)
                return;

            baseLock.SetParent(null);

            var car2 = car;
            var baseLock2 = baseLock;

            NextTick(() =>
            {
                if (car2 == null)
                {
                    _lockedVehicleTracker.OnLockRemoved(car2);
                    baseLock2.Kill();
                }
                else
                {
                    var driverModule = FindFirstDriverModule(car2);
                    if (driverModule == null)
                    {
                        _lockedVehicleTracker.OnLockRemoved(car2);
                        baseLock2.Kill();
                    }
                    else
                    {
                        baseLock2.SetParent(driverModule);
                    }
                }
            });
        }

        // Allow players to deploy locks directly without any commands.
        private object CanDeployItem(BasePlayer basePlayer, Deployer deployer, NetworkableId entityId)
        {
            if (basePlayer == null || deployer == null)
                return null;

            var deployable = deployer.GetDeployable();
            if (deployable == null)
                return null;

            var activeItem = basePlayer.GetActiveItem();
            if (activeItem == null)
                return null;

            var itemid = activeItem.info.itemid;

            LockInfo lockInfo;
            if (itemid == LockInfo_CodeLock.ItemId)
                lockInfo = LockInfo_CodeLock;
            else if (itemid == LockInfo_KeyLock.ItemId)
                lockInfo = LockInfo_KeyLock;
            else
                return null;

            var vehicle = GetVehicleFromEntity(BaseNetworkable.serverEntities.Find(entityId) as BaseEntity, basePlayer);
            if (vehicle == null)
                return null;

            var vehicleInfo = _vehicleInfoManager.GetVehicleInfo(vehicle);
            if (vehicleInfo == null)
                return null;

            var player = basePlayer.IPlayer;

            // Trick to make sure the replies are in chat instead of console.
            player.LastCommand = CommandType.Chat;

            if (!VerifyCanDeploy(player, vehicle, vehicleInfo, lockInfo)
                || !VerifyDeployDistance(player, vehicle))
                return false;

            activeItem.UseItem();
            DeployLockForPlayer(vehicle, vehicleInfo, lockInfo, basePlayer);
            return false;
        }

        private BaseLock DeployLock(BaseEntity vehicle, VehicleInfo vehicleInfo, LockInfo lockInfo, ulong ownerId = 0)
        {
            var parentToEntity = vehicleInfo.DetermineLockParent(vehicle);
            if (parentToEntity == null)
                return null;

            var baseLock =
                GameManager.server.CreateEntity(lockInfo.Prefab, vehicleInfo.LockPosition, vehicleInfo.LockRotation) as
                    BaseLock;
            if (baseLock == null)
                return null;

            var keyLock = baseLock as KeyLock;
            if (keyLock != null) keyLock.keyCode = Random.Range(1, 100000);

            // Assign lock ownership when the lock is being deployed by/for a player.
            if (ownerId != 0) baseLock.OwnerID = ownerId;

            baseLock.SetParent(parentToEntity, vehicleInfo.ParentBone);
            baseLock.Spawn();
            vehicle.SetSlot(BaseEntity.Slot.Lock, baseLock);

            // Auto lock key locks to be consistent with vanilla.
            if (ownerId != 0 && keyLock != null) keyLock.SetFlag(BaseEntity.Flags.Locked, true);

            Effect.server.Run(Prefab_CodeLock_DeployedEffect, baseLock.transform.position);
            Interface.CallHook("OnVehicleLockDeployed", vehicle, baseLock);
            _lockedVehicleTracker.OnLockAdded(vehicle);

            return baseLock;
        }

        private BaseLock DeployLockForPlayer(BaseEntity vehicle, VehicleInfo vehicleInfo, LockInfo lockInfo,
            BasePlayer player)
        {
            var originalVehicleOwnerId = vehicle.OwnerID;

            // Temporarily set the player as the owner of the vehicle, for compatibility with AutoCodeLock (OnItemDeployed).
            vehicle.OwnerID = player.userID;

            var baseLock = DeployLock(vehicle, vehicleInfo, lockInfo, player.userID);
            if (baseLock == null)
            {
                vehicle.OwnerID = originalVehicleOwnerId;
                return null;
            }

            // Allow other plugins to detect the code lock being deployed (e.g., to auto lock).
            var lockItem = GetPlayerLockItem(player, lockInfo);
            if (lockItem != null)
            {
                Interface.CallHook("OnItemDeployed", lockItem.GetHeldEntity(), vehicle, baseLock);
            }
            else
            {
                // Temporarily increase the player inventory capacity to ensure there is enough space.
                player.inventory.containerMain.capacity++;
                var temporaryLockItem = ItemManager.CreateByItemID(lockInfo.ItemId);
                if (player.inventory.GiveItem(temporaryLockItem))
                {
                    Interface.CallHook("OnItemDeployed", temporaryLockItem.GetHeldEntity(), vehicle, baseLock);
                    temporaryLockItem.RemoveFromContainer();
                }

                temporaryLockItem.Remove();
                player.inventory.containerMain.capacity--;
            }

            // Revert the vehicle owner to the original, after OnItemDeployed is called.
            vehicle.OwnerID = originalVehicleOwnerId;


            // Potentially assign vehicle ownership when the lock is being deployed by/for a player.
            ClaimVehicle(vehicle, player.userID);

            return baseLock;
        }

        private static void ClaimVehicle(BaseEntity vehicle, ulong ownerId)
        {
            vehicle.OwnerID = ownerId;
            Interface.CallHook("OnVehicleOwnershipChanged", vehicle);
        }

        private static Item GetPlayerLockItem(BasePlayer player, LockInfo lockInfo)
        {
            return player.inventory.FindItemByItemID(lockInfo.ItemId);
        }

        private bool VerifyDeployDistance(IPlayer player, BaseEntity vehicle)
        {
            if (vehicle.Distance(player.Object as BasePlayer) <= MaxDeployDistance)
                return true;

            return false;
        }

        private static bool IsDead(BaseEntity entity)
        {
            return (entity as BaseCombatEntity)?.IsDead() ?? false;
        }

        private bool VerifyVehicleIsNotDead(IPlayer player, BaseEntity vehicle)
        {
            if (!IsDead(vehicle))
                return true;

            return false;
        }

        private bool VerifyNotForSale(IPlayer player, BaseEntity vehicle)
        {
            var rideableAnimal = vehicle as BaseRidableAnimal;
            if (rideableAnimal == null || !rideableAnimal.IsForSale())
                return true;

            return false;
        }

        private bool AllowNoOwner(BaseEntity vehicle)
        {
            return vehicle.OwnerID != 0;
        }

        private bool AllowDifferentOwner(IPlayer player, BaseEntity vehicle)
        {
            return vehicle.OwnerID == 0
                   || vehicle.OwnerID.ToString() == player.Id;
        }

        private bool VerifyNoOwnershipRestriction(IPlayer player, BaseEntity vehicle)
        {
            if (!AllowNoOwner(vehicle)) return false;

            if (!AllowDifferentOwner(player, vehicle)) return false;

            return true;
        }

        private bool VerifyCanBuild(IPlayer player, BaseEntity vehicle)
        {
            var basePlayer = player.Object as BasePlayer;

            if (vehicle.OwnerID == 0)
            {
                if (!basePlayer.IsBuildingAuthed() || !basePlayer.IsBuildingAuthed(vehicle.WorldSpaceBounds()))
                    return false;
            }
            else if (basePlayer.IsBuildingBlocked() || basePlayer.IsBuildingBlocked(vehicle.WorldSpaceBounds()))
            {
                return false;
            }

            return true;
        }

        private bool VerifyVehicleHasNoLock(IPlayer player, BaseEntity vehicle)
        {
            if (GetVehicleLock(vehicle) == null)
                return true;

            return false;
        }

        private bool VerifyVehicleCanHaveALock(IPlayer player, BaseEntity vehicle)
        {
            if (CanVehicleHaveALock(vehicle))
                return true;

            return false;
        }

        private static bool CanCarHaveLock(ModularCar car)
        {
            return FindFirstDriverModule(car) != null;
        }

        private static bool CanVehicleHaveALock(BaseEntity vehicle)
        {
            // Only modular cars have restrictions
            var car = vehicle as ModularCar;
            return car == null || CanCarHaveLock(car);
        }

        private bool VerifyNotMounted(IPlayer player, BaseEntity vehicle, VehicleInfo vehicleInfo)
        {
            if (!vehicleInfo.IsMounted(vehicle))
                return true;

            return false;
        }

        private bool VerifyCanDeploy(IPlayer player, BaseEntity vehicle, VehicleInfo vehicleInfo, LockInfo lockInfo)
        {
            var basePlayer = player.Object as BasePlayer;

            return
                VerifyVehicleIsNotDead(player, vehicle)
                && VerifyNotForSale(player, vehicle)
                && VerifyNoOwnershipRestriction(player, vehicle)
                && VerifyCanBuild(player, vehicle)
                && VerifyVehicleHasNoLock(player, vehicle)
                && VerifyVehicleCanHaveALock(player, vehicle)
                && VerifyNotMounted(player, vehicle, vehicleInfo)
                && !DeployWasBlocked(vehicle, basePlayer, lockInfo);
        }

        private static bool DeployWasBlocked(BaseEntity vehicle, BasePlayer player, LockInfo lockInfo)
        {
            var hookResult = Interface.CallHook(lockInfo.PreHookName, vehicle, player);
            return hookResult is bool && (bool)hookResult == false;
        }

        private static RidableHorse2 GetClosestHorse(HitchTrough hitchTrough, BasePlayer player)
        {
            var closestDistance = 1000f;
            RidableHorse2 closestHorse = null;

            for (var i = 0; i < hitchTrough.hitchSpots.Length; i++)
            {
                var hitchSpot = hitchTrough.hitchSpots[i];
                if (!hitchSpot.IsOccupied())
                    continue;

                var distance = Vector3.Distance(player.transform.position, hitchSpot.tr.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestHorse = hitchSpot.hitchableEntRef.Get(true) as RidableHorse2;
                }
            }

            return closestHorse;
        }

        private static BaseEntity GetVehicleFromEntity(BaseEntity entity, BasePlayer basePlayer)
        {
            if (entity == null)
                return null;

            var module = entity as BaseVehicleModule;
            if (module != null)
                return module.Vehicle;

            var carLift = entity as ModularCarGarage;
            if ((object)carLift != null)
                return carLift.carOccupant;

            var hitchTrough = entity as HitchTrough;
            if ((object)hitchTrough != null)
                return GetClosestHorse(hitchTrough, basePlayer);

            return entity;
        }

        private object OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (info == null || info.HitEntity == null || player == null)
                return null;

            var rhEntity = info.HitEntity;
            if (rhEntity.name.Contains("module_entities"))
                rhEntity = rhEntity.parentEntity.Get(true);
            if (rhEntity == null || rhEntity.OwnerID != player.userID)
                return null;

            var product = _config.Vehicles.FirstOrDefault(z =>
                z.Value.Prefab == rhEntity.PrefabName && z.Value.Skin == rhEntity.skinID);
            if (product.Equals(default(KeyValuePair<string, VehicleInfoConfig>)) || !product.Value.CanPickup)
                return null;

            if (player.inventory.containerMain.IsFull())
            {
                player.ChatMessage("Inventory is full. Cannot pick up the vehicle.");
                return null;
            }

            if (!GiveVehicle(player, product.Value, false))
            {
                player.ChatMessage("Unable to give vehicle to player.");
                return null;
            }

            if (rhEntity.TryGetComponent<ModularCar>(out var component))
                GetOutParts(component);

            var baseVehicle = rhEntity.GetComponent<BaseVehicle>();
            if (baseVehicle != null)
            {
                var fuelSystem = baseVehicle.GetFuelSystem();
                if (fuelSystem != null)
                {
                    var fuelAmount = fuelSystem.GetFuelAmount();
                    if (fuelAmount > 0)
                    {
                        var fuelItem = ItemManager.CreateByName("lowgradefuel", fuelAmount);
                        if (fuelItem != null && fuelItem.amount > 0)
                            if (!player.inventory.GiveItem(fuelItem))
                            {
                                player.ChatMessage($"{fuelItem.amount} fuel was dropped at your feet");
                                fuelItem.Drop(player.inventory.containerMain.dropPosition,
                                    player.inventory.containerMain.dropVelocity);
                            }
                    }
                }
            }

            rhEntity.Kill();
            player.ChatMessage($"Vehicle {product.Value.Name} was picked up");

            return false;
        }

        private void SpawnRecycler(BaseEntity entity, BasePlayer player, Vector3 position, Quaternion rotation,
            string prefab, ulong skin)
        {
            var recycler =
                GameManager.server.CreateEntity("assets/bundled/prefabs/static/recycler_static.prefab", position,
                    rotation) as Recycler;
            recycler.OwnerID = player.userID.Get();
            recycler.skinID = skin;
            recycler.Spawn();
            recycler._maxHealth = 1000;
            recycler.health = recycler.MaxHealth();
            NextFrame(() => { entity?.Kill(); });
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            var entity = gameObject?.ToBaseEntity();
            if (entity == null || entity.skinID <= 0) return;

            var item = planner.GetItem();
            if (item == null)
                return;

            var player = planner.GetOwnerPlayer();
            if (player == null) return;

            var vehicleSettings = _config.Vehicles.FirstOrDefault(x =>
                    x.Value.Skin == entity.skinID &&
                    item.name.Contains(x.Value.Name, StringComparison.OrdinalIgnoreCase))
                .Value;

            if (vehicleSettings == null)
                return;

            var rot = entity.transform.rotation;
            var pos = entity.transform.position;
            var prefab = vehicleSettings.Prefab;

            if (prefab.Contains("recycler_static"))
            {
                if (CanPlaceRecycler(entity, player))
                {
                    SpawnRecycler(entity, player, pos, rot, prefab, vehicleSettings.Skin);
                    return;
                }

                GiveVehicle(player, vehicleSettings, false);
                entity.Kill();
                player
                    .ChatMessage("Recycler cannot be placed here, it has been returned to your inventory.");
                return;
            }

            NextFrame(() => { entity?.Kill(); });

            if (!CanPlaceVehicle(pos))
                pos = GetPositionFromPlayer(player, vehicleSettings.SpawnDistance);

            var newEntity = GameManager.server.CreateEntity(prefab, pos, rot);

            if (newEntity == null)
            {
                GiveVehicle(player, vehicleSettings);
                return;
            }

            newEntity.OwnerID = player.userID;
            newEntity.skinID = vehicleSettings.Skin;
            newEntity.Spawn();

            if (planner.GetItem() != null && !planner.GetItem().name.EndsWith("   "))
            {
                var baseVehicle = newEntity.GetComponent<BaseVehicle>();
                if (baseVehicle != null)
                {
                    var fuelSystem = baseVehicle.GetFuelSystem();
                    if (fuelSystem != null) fuelSystem.AddFuel(vehicleSettings.Fuel);
                }
            }

            if (vehicleSettings.NeedCarParts)
                if (newEntity.TryGetComponent<ModularCar>(out var carComponent))
                    AddEngineParts(carComponent, vehicleSettings.EngineParts);

            if (!vehicleSettings.EnableDecay)
                if (newEntity.TryGetComponent<DecayEntity>(out var decayEntity))
                    UnityEngine.Object.Destroy(decayEntity);
        }

        private bool CanPlaceVehicle(Vector3 pos, float radius = 1f)
        {
            var hits = Physics.OverlapSphereNonAlloc(pos, radius, Vis.colBuffer, 1 << 18);

            for (var i = 0; i < hits; i++)
            {
                var col = Vis.colBuffer[i];
                if (col.gameObject != null)
                    return false;
            }

            return true;
        }

        private bool CanPlaceRecycler(BaseEntity entity, BasePlayer player)
        {
            // Check if the recycler can be placed at the current location
            // For demonstration purposes, we assume that placing a recycler within a monument is not allowed
            return !IsInMonument(entity.transform.position);
        }

        private bool IsInMonument(Vector3 position)
        {
            // This is a simple placeholder for checking if a position is within a monument.
            // You would need to replace this with actual game logic to check for monuments.
            // For example, you might need to use TerrainMeta.Path.Monuments or similar.
            foreach (var monument in TerrainMeta.Path.Monuments)
                if (monument.Bounds.Contains(position))
                    return true;

            return false;
        }

        private object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (_config.Vehicles.Any(x => x.Value.Skin == item.GetItem().skin)) return false;

            return null;
        }

        private object CanStackItem(Item item, Item targetItem)
        {
            if (_config.Vehicles.Any(x => x.Value.Skin == item.skin)) return false;

            return null;
        }

        #endregion

        #region Utils

        private static string HexToCuiColor(string hex, float alpha = 100)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFF";

            var str = hex.Trim('#');
            if (str.Length != 6) throw new Exception(hex);
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

            return $"{(double)r / 255} {(double)g / 255} {(double)b / 255} {alpha / 100}";
        }

        private Vector3 GetPositionFromPlayer(BasePlayer player, float distance = 1f)
        {
            var rotation = player.GetNetworkRotation();

            var forward = rotation * Vector3.forward;
            var straight = Vector3.Cross(Vector3.Cross(Vector3.up, forward), Vector3.up).normalized;

            var buff = new Vector3(player.transform.position.x + straight.x * distance,
                TerrainMeta.HeightMap.GetHeight(player.transform.position + straight * distance),
                player.transform.position.z + straight.z * distance);

            return buff;
        }

        #endregion

        #region Config

        private ConfigData _config;

        internal class VehicleInfoConfig
        {
            [JsonProperty("Sound on purchase", Order = 0)]
            public bool UseSoundOnPurchase;

            [JsonProperty("Order", Order = -1)] public int Order;
            [JsonProperty("Show", Order = 0)] public bool Show;
            [JsonProperty("Name", Order = 1)] public string Name;
            [JsonProperty("Prefab", Order = 2)] public string Prefab;

            [JsonProperty("Image link", Order = 3)]
            public string Image;

            [JsonProperty("Spawn distance", Order = 4)]
            public float SpawnDistance;

            [JsonProperty("Fuel", Order = 5)] public int Fuel;

            [JsonProperty("Currency: 0 - item, 1 - Economics, 2 - Server Rewards", Order = 6)]
            public byte SellCurrency;

            [JsonProperty("If vehicle selling for item type him shortname", Order = 7)]
            public string Shortname;

            [JsonProperty("Price", Order = 8)] public int Price;
            [JsonProperty("Skin", Order = 9)] public ulong Skin;
            [JsonProperty("Command", Order = 10)] public string Command;

            [JsonProperty("DeployableItemId", Order = 11)]
            public int DeployableItemId;

            [JsonProperty("Need add engine parts if it possible?", Order = 12)]
            public bool NeedCarParts = true;

            [JsonProperty("Engine parts", Order = 13)]
            public List<string> EngineParts;

            [JsonProperty("Cooldown to buy (in seconds)", Order = -2)]
            public int Cooldown;

            [JsonProperty("Pickup type (0 - command, 1 - hammer)", Order = -3)]
            public int PickupType;

            [JsonProperty("Can pickup?", Order = -4)]
            public bool CanPickup;

            [JsonProperty("Can recall?", Order = -5)]
            public bool CanCallback;

            [JsonProperty("Recall price", Order = -6)]
            public int RecallCost;

            [JsonProperty("Recall cost need?", Order = -7)]
            public bool RecallCostNeed;

            [JsonProperty("Pickup price", Order = -8)]
            public int PickupPrice;

            [JsonProperty("Enable decay?", Order = -9)]
            public bool EnableDecay;

            [JsonProperty("Permission (still empty if not need) ex. vehiclebuy.YOURPERMISSIONNAME", Order = -10)]
            public string Permission;

            [JsonProperty("Maximum number of purchases of one vehicle by one player", Order = -11)]
            public int MaxPurchases;

            public bool CanPlayerBuy(BasePlayer player)
            {
                return string.IsNullOrEmpty(Permission) || player.IPlayer.HasPermission(Permission);
            }
        }

        internal class VendingMachinesConfig
        {
            [JsonProperty("Bandit Camp vending machine")]
            public bool BanditCampSpawnMachine;

            [JsonProperty("Outpost vending machine")]
            public bool OutpostSpawnMachine;

            [JsonProperty("Bandit Camp products")] public List<VMOrder> BanditCampOrders;
            [JsonProperty("Outpost products")] public List<VMOrder> OutpostOrders;

            [JsonProperty("Fishing village A vending machine")]
            public bool FishingVillageASpawnMachine;

            [JsonProperty("Fishing village B vending machine")]
            public bool FishingVillageBSpawnMachine;

            [JsonProperty("Fishing village C vending machine")]
            public bool FishingVillageCSpawnMachine;

            [JsonProperty("Fishing Village C products")]
            public List<VMOrder> FishingVillageCOrders;

            [JsonProperty("Fishing Village A products")]
            public List<VMOrder> FishingVillageAOrders;

            [JsonProperty("Fishing Village B products")]
            public List<VMOrder> FishingVillageBOrders;
        }

        internal class VMOrder
        {
            [JsonProperty("Vehicle key from config")]
            public string VehicleKey;

            [JsonProperty("Item (shortname)")] public string Shortname;
            [JsonProperty("Price")] public int Price;

            public VehicleInfoConfig GetVehicle()
            {
                return Instance._config.Vehicles[VehicleKey] ??
                       throw new ArgumentException(
                           $"Key {VehicleKey} not found in config");
            }
        }

        public class ConfigData
        {
            [JsonProperty("Commands", Order = -1)]
            public List<string> Commands;

            [JsonProperty("Currency name economics", Order = 0)]
            public string CurrencyName;

            [JsonProperty("Currency name Server Rewards", Order = 1)]
            public string CurrencyNameSR;

            [JsonProperty("Currency name Bank System", Order = 1)]
            public string CurrencyNameBS;

            [JsonProperty("Pickup distance", Order = 2)]
            public float PickupRadius;

            [JsonProperty("Disable vehicles damage?", Order = 2)]
            public bool DisableVehiclesDamage = false;

            [JsonProperty("Vending machines", Order = 2)]
            public VendingMachinesConfig VendingMachines;

            [JsonProperty("Vehicles", Order = 3)]
            public Dictionary<string, VehicleInfoConfig> Vehicles;

            [JsonProperty(Order = 200)]
            public VersionNumber Version;
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                VendingMachines = new VendingMachinesConfig
                {
                    FishingVillageASpawnMachine = true,
                    FishingVillageBSpawnMachine = true,
                    FishingVillageCSpawnMachine = true,
                    FishingVillageAOrders = new List<VMOrder>
                    {
                        new()
                        {
                            VehicleKey = "copter",
                            Shortname = "scrap",
                            Price = 1000
                        },
                        new()
                        {
                            VehicleKey = "scrapheli",
                            Shortname = "scrap",
                            Price = 999
                        },
                        new()
                        {
                            VehicleKey = "attackheli",
                            Shortname = "sulfur.ore",
                            Price = 200
                        }
                    },
                    FishingVillageBOrders = new List<VMOrder>
                    {
                        new()
                        {
                            VehicleKey = "copter",
                            Shortname = "scrap",
                            Price = 1000
                        },
                        new()
                        {
                            VehicleKey = "scrapheli",
                            Shortname = "scrap",
                            Price = 999
                        },
                        new()
                        {
                            VehicleKey = "attackheli",
                            Shortname = "sulfur.ore",
                            Price = 200
                        }
                    },
                    FishingVillageCOrders = new List<VMOrder>
                    {
                        new()
                        {
                            VehicleKey = "copter",
                            Shortname = "scrap",
                            Price = 1000
                        },
                        new()
                        {
                            VehicleKey = "scrapheli",
                            Shortname = "scrap",
                            Price = 999
                        },
                        new()
                        {
                            VehicleKey = "attackheli",
                            Shortname = "sulfur.ore",
                            Price = 200
                        }
                    },
                    BanditCampSpawnMachine = true,
                    BanditCampOrders = new List<VMOrder>
                    {
                        new()
                        {
                            VehicleKey = "copter",
                            Shortname = "scrap",
                            Price = 1000
                        },
                        new()
                        {
                            VehicleKey = "scrapheli",
                            Shortname = "scrap",
                            Price = 999
                        },
                        new()
                        {
                            VehicleKey = "attackheli",
                            Shortname = "sulfur.ore",
                            Price = 200
                        }
                    },
                    OutpostSpawnMachine = true,
                    OutpostOrders = new List<VMOrder>
                    {
                        new()
                        {
                            VehicleKey = "car2",
                            Shortname = "scrap",
                            Price = 1000
                        },
                        new()
                        {
                            VehicleKey = "car1",
                            Shortname = "scrap",
                            Price = 999
                        },
                        new()
                        {
                            VehicleKey = "tugboat",
                            Shortname = "sulfur.ore",
                            Price = 200
                        }
                    }
                },
                Commands = new List<string>
                {
                    "vehiclebuy",
                    "vb",
                    "vehicle"
                },
                PickupRadius = 5f,
                CurrencyNameBS = "BSTEST",
                CurrencyNameSR = "SRTEST",
                CurrencyName = "ECOTEST",
                Vehicles = new Dictionary<string, VehicleInfoConfig>
                {
                    ["copter"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 1,
                        Show = true,
                        Name = "Minicopter",
                        Prefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/minicopter.png",
                        SpawnDistance = 5f,
                        Fuel = 53,
                        SellCurrency = 2,
                        Shortname = "scrap",
                        Price = 550,
                        Skin = 3036041060,
                        Command = "copter.add",
                        DeployableItemId = 833533164,
                        Permission = ""
                    },
                    ["scrapheli"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 2,
                        Show = true,
                        Name = "Scrap Transport Helicopter",
                        Prefab = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/scrap-heli.png",
                        SpawnDistance = 10f,
                        Fuel = 522,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 850,
                        Skin = 3033922797,
                        Command = "scrapi.add",
                        DeployableItemId = 833533164,
                        Permission = ""
                    },
                    ["attackheli"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 3,
                        Show = true,
                        Name = "Attack Helicopter",
                        Prefab = "assets/content/vehicles/attackhelicopter/attackhelicopter.entity.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/attack-helicopter.png",
                        SpawnDistance = 10f,
                        Fuel = 522,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 1200,
                        Skin = 3036032642,
                        Command = "attack.add",
                        DeployableItemId = 833533164,
                        Permission = ""
                    },
                    ["car2"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 4,
                        Show = true,
                        Name = "Car 2",
                        Prefab = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/modular-vehicle-2.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 300,
                        Skin = 3051397208,
                        Command = "car2.add",
                        DeployableItemId = 833533164,
                        NeedCarParts = true,
                        EngineParts = new List<string>
                        {
                            "carburetor3",
                            "crankshaft3",
                            "piston3",
                            "valve3",
                            "sparkplug3"
                        },
                        Permission = ""
                    },
                    ["car3"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 5,
                        Show = true,
                        Name = "Car 3",
                        Prefab = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/modular-vehicle-3.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 600,
                        Skin = 3051397420,
                        Command = "car3.add",
                        DeployableItemId = 833533164,
                        NeedCarParts = true,
                        EngineParts = new List<string>
                        {
                            "carburetor3",
                            "crankshaft3",
                            "piston3",
                            "valve3",
                            "sparkplug3"
                        },
                        Permission = ""
                    },
                    ["car4"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 6,
                        Show = true,
                        Name = "Car 4",
                        Prefab = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/modular-vehicle-4.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 900,
                        Skin = 3051397599,
                        Command = "car4.add",
                        DeployableItemId = 833533164,
                        NeedCarParts = true,
                        EngineParts = new List<string>
                        {
                            "carburetor3",
                            "crankshaft3",
                            "piston3",
                            "valve3",
                            "sparkplug3"
                        },
                        Permission = ""
                    },
                    ["tugboat"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 7,
                        Show = true,
                        Name = "TugBoat",
                        Prefab = "assets/content/vehicles/boats/tugboat/tugboat.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/tugboat.png",
                        SpawnDistance = 15f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 1500,
                        Skin = 3036456691,
                        Command = "tugboat.add",
                        DeployableItemId = -697981032,
                        Permission = ""
                    },
                    ["rowboat"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 8,
                        Show = true,
                        Name = "RowBoat",
                        Prefab = "assets/content/vehicles/boats/rowboat/rowboat.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/rowboat.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 2,
                        Shortname = "scrap",
                        Price = 450,
                        Skin = 3036112261,
                        Command = "rowboat.add",
                        DeployableItemId = -697981032,
                        Permission = ""
                    },
                    ["rhib"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 9,
                        Show = true,
                        Name = "RHIB",
                        Prefab = "assets/content/vehicles/boats/rhib/rhib.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/rhib.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 585,
                        Skin = 3036112776,
                        Command = "rhib.add",
                        DeployableItemId = -697981032,
                        Permission = ""
                    },
                    ["solosub"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 10,
                        Show = true,
                        Name = "SoloSub",
                        Prefab = "assets/content/vehicles/submarine/submarinesolo.entity.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/submarine-solo.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 555,
                        Skin = 3036453289,
                        Command = "solosub.add",
                        DeployableItemId = -697981032,
                        Permission = ""
                    },
                    ["duosub"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 11,
                        Show = true,
                        Name = "DuoSub",
                        Prefab = "assets/content/vehicles/submarine/submarineduo.entity.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/submarine-duo.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 750,
                        Skin = 3036453387,
                        Command = "duosub.add",
                        DeployableItemId = -697981032,
                        Permission = ""
                    },
                    ["horse"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 12,
                        Show = true,
                        Name = "Horse",
                        Prefab = "assets/content/vehicles/horse/ridablehorse2.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/ridable-horse.png",
                        SpawnDistance = 5f,
                        Fuel = 0,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 150,
                        Skin = 3036456786,
                        Command = "horse.add",
                        DeployableItemId = 833533164,
                        Permission = ""
                    },
                    ["snowmobile"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 13,
                        Show = true,
                        Name = "SnowMobile",
                        Prefab = "assets/content/vehicles/snowmobiles/snowmobile.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/snowmobile.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 600,
                        Skin = 3036453555,
                        Command = "snowmobile.add",
                        DeployableItemId = 833533164,
                        Permission = ""
                    },
                    ["tomaha"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 14,
                        Show = true,
                        Name = "Tomaha",
                        Prefab = "assets/content/vehicles/snowmobiles/tomahasnowmobile.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/snowmobiletomaha.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 100,
                        Skin = 3036453663,
                        Command = "tomaha.add",
                        DeployableItemId = 833533164,
                        Permission = ""
                    },
                    ["hotairballoon"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 15,
                        Show = true,
                        Name = "HotairBalloon",
                        Prefab = "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/balloon.png",
                        SpawnDistance = 5f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 300,
                        Skin = 3036454299,
                        Command = "hotairballoon.add",
                        DeployableItemId = 833533164,
                        Permission = ""
                    },
                    ["recycler"] = new()
                    {
                        UseSoundOnPurchase = true,
                        Order = 16,
                        Show = true,
                        Name = "Recycler",
                        Prefab = "assets/bundled/prefabs/static/recycler_static.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/recycler.png",
                        SpawnDistance = 2f,
                        Fuel = 52,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 1000,
                        Skin = 3036111302,
                        Command = "recycler.add",
                        DeployableItemId = 833533164,
                        Permission = ""
                    },
                    ["pedalbike"] = new VehicleInfoConfig
                    {
                        UseSoundOnPurchase = true,
                        Order = 17,
                        Show = true,
                        Name = "pedalbike",
                        Prefab = "assets/content/vehicles/bikes/pedalbike.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/pedalbike.png",
                        SpawnDistance = 5,
                        Fuel = 100,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 500,
                        Skin = 3281191605,
                        Command = "pedalbike.add",
                        DeployableItemId = 833533164,
                        NeedCarParts = false,
                        EngineParts = null,
                        Cooldown = 0,
                        PickupType = 0,
                        CanPickup = false,
                        CanCallback = false,
                        RecallCost = 0,
                        RecallCostNeed = false,
                        PickupPrice = 0,
                        EnableDecay = false,
                        Permission = null
                    },
                    ["motorbike"] = new VehicleInfoConfig
                    {
                        UseSoundOnPurchase = true,
                        Order = 18,
                        Show = true,
                        Name = "motorbike",
                        Prefab = "assets/content/vehicles/bikes/motorbike.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/motorbike.png",
                        SpawnDistance = 5,
                        Fuel = 100,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 1000,
                        Skin = 3281191090,
                        Command = "motorbike.add",
                        DeployableItemId = 833533164,
                        NeedCarParts = false,
                        EngineParts = null,
                        Cooldown = 0,
                        PickupType = 0,
                        CanPickup = false,
                        CanCallback = false,
                        RecallCost = 0,
                        RecallCostNeed = false,
                        PickupPrice = 0,
                        EnableDecay = false,
                        Permission = null
                    },
                    ["motorbike_sidecar"] = new VehicleInfoConfig
                    {
                        UseSoundOnPurchase = true,
                        Order = 19,
                        Show = true,
                        Name = "motorbike_sidecar",
                        Prefab = "assets/content/vehicles/bikes/motorbike_sidecar.prefab",
                        Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/motorbike-sidecar.png",
                        SpawnDistance = 5,
                        Fuel = 100,
                        SellCurrency = 0,
                        Shortname = "scrap",
                        Price = 1500,
                        Skin = 3281192470,
                        Command = "motorbike_sidecar",
                        DeployableItemId = 833533164,
                        NeedCarParts = false,
                        EngineParts = null,
                        Cooldown = 0,
                        PickupType = 0,
                        CanPickup = false,
                        CanCallback = false,
                        RecallCost = 0,
                        RecallCostNeed = false,
                        PickupPrice = 0,
                        EnableDecay = false,
                        Permission = null
                    }
                }
            };
            SaveConfig(config);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigData>();

            if (_config.Version == null)
                _config.Version = new VersionNumber(1, 0, 0);

            if (_config.Version < new VersionNumber(1, 1, 4))
            {
                _config.Vehicles.TryAdd("pedalbike", new VehicleInfoConfig
                {
                    UseSoundOnPurchase = true,
                    Order = 17,
                    Show = true,
                    Name = "pedalbike",
                    Prefab = "assets/content/vehicles/bikes/pedalbike.prefab",
                    Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/pedalbike.png",
                    SpawnDistance = 5,
                    Fuel = 100,
                    SellCurrency = 0,
                    Shortname = "scrap",
                    Price = 500,
                    Skin = 3281191605,
                    Command = "pedalbike.add",
                    DeployableItemId = 833533164,
                    NeedCarParts = false,
                    EngineParts = null,
                    Cooldown = 0,
                    PickupType = 0,
                    CanPickup = false,
                    CanCallback = false,
                    RecallCost = 0,
                    RecallCostNeed = false,
                    PickupPrice = 0,
                    EnableDecay = false,
                    Permission = null
                });

                _config.Vehicles.TryAdd("motorbike", new VehicleInfoConfig
                {
                    UseSoundOnPurchase = true,
                    Order = 18,
                    Show = true,
                    Name = "motorbike",
                    Prefab = "assets/content/vehicles/bikes/motorbike.prefab",
                    Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/motorbike.png",
                    SpawnDistance = 5,
                    Fuel = 100,
                    SellCurrency = 0,
                    Shortname = "scrap",
                    Price = 1000,
                    Skin = 3281191090,
                    Command = "motorbike.add",
                    DeployableItemId = 833533164,
                    NeedCarParts = false,
                    EngineParts = null,
                    Cooldown = 0,
                    PickupType = 0,
                    CanPickup = false,
                    CanCallback = false,
                    RecallCost = 0,
                    RecallCostNeed = false,
                    PickupPrice = 0,
                    EnableDecay = false,
                    Permission = null
                });

                _config.Vehicles.TryAdd("motorbike_sidecar", new VehicleInfoConfig
                {
                    UseSoundOnPurchase = true,
                    Order = 19,
                    Show = true,
                    Name = "motorbike_sidecar",
                    Prefab = "assets/content/vehicles/bikes/motorbike_sidecar.prefab",
                    Image = "https://gitlab.com/TheMevent/PluginsStorage/raw/main/Images/VehicleBuy/motorbike-sidecar.png",
                    SpawnDistance = 5,
                    Fuel = 100,
                    SellCurrency = 0,
                    Shortname = "scrap",
                    Price = 1500,
                    Skin = 3281192470,
                    Command = "motorbike_sidecar",
                    DeployableItemId = 833533164,
                    NeedCarParts = false,
                    EngineParts = null,
                    Cooldown = 0,
                    PickupType = 0,
                    CanPickup = false,
                    CanCallback = false,
                    RecallCost = 0,
                    RecallCostNeed = false,
                    PickupPrice = 0,
                    EnableDecay = false,
                    Permission = null
                });

                _config.Version = Version;
                PrintWarning("Config was updated");
            }

            if (_config.Version < new VersionNumber(2, 1, 3))
            {
                foreach (var (_, vehicle) in _config.Vehicles)
                {
                    if (vehicle.Prefab.Contains("assets/rust.ai/nextai/testridablehorse.prefab"))
                        vehicle.Prefab = "assets/content/vehicles/horse/ridablehorse2.prefab";
                }

                _config.Version = Version;
                PrintWarning("Config was updated");
            }

            if (_config.Version < new VersionNumber(2, 1, 4))
            {
                if (string.IsNullOrEmpty(_config.CurrencyNameBS))
                    _config.CurrencyNameBS = "BSTEST";

                _config.Version = Version;
                PrintWarning("Config was updated");
            }

            SaveConfig(_config);
        }

        private void SaveConfig(object config)
        {
            Config.WriteObject(config, true);
        }

        #endregion

        #region Data

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Title}/cooldowns", _players);
        }

        private void LoadData()
        {
            _players = Interface.Oxide?.DataFileSystem?.ReadObject<Dictionary<ulong, PlayerData>>($"{Title}/cooldowns")
                       ?? new Dictionary<ulong, PlayerData>();
        }

        #endregion

        #region UI Configuration

        public enum ScrollType
        {
            Horizontal,
            Vertical
        }

        public class ScrollViewUI
        {
            #region Fields

            [JsonProperty(PropertyName = "Scroll Type (Horizontal, Vertical)")]
            [JsonConverter(typeof(StringEnumConverter))]
            public ScrollType ScrollType;

            [JsonProperty(PropertyName = "Movement Type (Unrestricted, Elastic, Clamped)")]
            [JsonConverter(typeof(StringEnumConverter))]
            public ScrollRect.MovementType MovementType;

            [JsonProperty(PropertyName = "Elasticity")]
            public float Elasticity;

            [JsonProperty(PropertyName = "Deceleration Rate")]
            public float DecelerationRate;

            [JsonProperty(PropertyName = "Scroll Sensitivity")]
            public float ScrollSensitivity;

            [JsonProperty(PropertyName = "Minimal Height")]
            public float MinHeight;

            [JsonProperty(PropertyName = "Additional Height")]
            public float AdditionalHeight;

            [JsonProperty(PropertyName = "Scrollbar Settings")]
            public ScrollBarSettings Scrollbar = new();

            #endregion

            #region Public Methods

            public CuiScrollViewComponent GetScrollView(float totalWidth)
            {
                return GetScrollView(CalculateContentRectTransform(totalWidth));
            }

            public CuiScrollViewComponent GetScrollView(CuiRectTransform contentTransform)
            {
                var cuiScrollView = new CuiScrollViewComponent
                {
                    MovementType = MovementType,
                    Elasticity = Elasticity,
                    DecelerationRate = DecelerationRate,
                    ScrollSensitivity = ScrollSensitivity,
                    ContentTransform = contentTransform,
                    Inertia = true
                };

                switch (ScrollType)
                {
                    case ScrollType.Vertical:
                        {
                            cuiScrollView.Vertical = true;
                            cuiScrollView.Horizontal = false;

                            cuiScrollView.VerticalScrollbar = Scrollbar.Get();
                            break;
                        }

                    case ScrollType.Horizontal:
                        {
                            cuiScrollView.Horizontal = true;
                            cuiScrollView.Vertical = false;

                            cuiScrollView.HorizontalScrollbar = Scrollbar.Get();
                            break;
                        }
                }

                return cuiScrollView;
            }

            public CuiRectTransform CalculateContentRectTransform(float totalWidth)
            {
                CuiRectTransform contentRect;
                if (ScrollType == ScrollType.Horizontal)
                    contentRect = new CuiRectTransform
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0 1",
                        OffsetMin = "0 0",
                        OffsetMax = $"{totalWidth} 0"
                    };
                else
                    contentRect = new CuiRectTransform
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "1 1",
                        OffsetMin = $"0 -{totalWidth}",
                        OffsetMax = "0 0"
                    };

                return contentRect;
            }

            #endregion

            #region Classes

            public class ScrollBarSettings
            {
                #region Fields

                [JsonProperty(PropertyName = "Invert")]
                public bool Invert;

                [JsonProperty(PropertyName = "Auto Hide")]
                public bool AutoHide;

                [JsonProperty(PropertyName = "Handle Sprite")]
                public string HandleSprite = string.Empty;

                [JsonProperty(PropertyName = "Size")] public float Size;

                [JsonProperty(PropertyName = "Handle Color")]
                public IColor HandleColor = IColor.CreateWhite();

                [JsonProperty(PropertyName = "Highlight Color")]
                public IColor HighlightColor = IColor.CreateWhite();

                [JsonProperty(PropertyName = "Pressed Color")]
                public IColor PressedColor = IColor.CreateWhite();

                [JsonProperty(PropertyName = "Track Sprite")]
                public string TrackSprite = string.Empty;

                [JsonProperty(PropertyName = "Track Color")]
                public IColor TrackColor = IColor.CreateWhite();

                #endregion

                #region Public Methods

                public CuiScrollbar Get()
                {
                    var cuiScrollbar = new CuiScrollbar
                    {
                        Size = Size
                    };

                    if (Invert) cuiScrollbar.Invert = Invert;
                    if (AutoHide) cuiScrollbar.AutoHide = AutoHide;
                    if (!string.IsNullOrEmpty(HandleSprite)) cuiScrollbar.HandleSprite = HandleSprite;
                    if (!string.IsNullOrEmpty(TrackSprite)) cuiScrollbar.TrackSprite = TrackSprite;

                    if (HandleColor != null) cuiScrollbar.HandleColor = HandleColor.Get();
                    if (HighlightColor != null) cuiScrollbar.HighlightColor = HighlightColor.Get();
                    if (PressedColor != null) cuiScrollbar.PressedColor = PressedColor.Get();
                    if (TrackColor != null) cuiScrollbar.TrackColor = TrackColor.Get();

                    return cuiScrollbar;
                }

                #endregion
            }

            #endregion
        }

        public class ImageSettings : InterfacePosition
        {
            #region Fields

            [JsonProperty(PropertyName = "Sprite")]
            public string Sprite = string.Empty;

            [JsonProperty(PropertyName = "Material")]
            public string Material = string.Empty;

            [JsonProperty(PropertyName = "Image")] public string Image = string.Empty;

            [JsonProperty(PropertyName = "Color")] public IColor Color = IColor.CreateTransparent();

            [JsonProperty(PropertyName = "Cursor Enabled")]
            public bool CursorEnabled = false;

            [JsonProperty(PropertyName = "Keyboard Enabled")]
            public bool KeyboardEnabled = false;

            #endregion

            #region Private Methods

            [JsonIgnore] private ICuiComponent _imageComponent;

            public ICuiComponent GetImageComponent()
            {
                if (_imageComponent != null) return _imageComponent;

                if (!string.IsNullOrEmpty(Image))
                {
                    var rawImage = new CuiRawImageComponent
                    {
                        Png = Instance.GetImage(Image),
                        Color = Color.Get()
                    };

                    if (!string.IsNullOrEmpty(Sprite))
                        rawImage.Sprite = Sprite;

                    if (!string.IsNullOrEmpty(Material))
                        rawImage.Material = Material;

                    _imageComponent = rawImage;
                }
                else
                {
                    var image = new CuiImageComponent
                    {
                        Color = Color.Get()
                    };

                    if (!string.IsNullOrEmpty(Sprite))
                        image.Sprite = Sprite;

                    if (!string.IsNullOrEmpty(Material))
                        image.Material = Material;

                    _imageComponent = image;
                }

                return _imageComponent;
            }

            #endregion

            #region Public Methods

            public bool TryGetImageURL(out string url)
            {
                if (!string.IsNullOrWhiteSpace(Image) && Image.IsURL())
                {
                    url = Image;
                    return true;
                }

                url = null;
                return false;
            }

            public CuiElement GetImage(string parent,
                string name = null,
                string destroyUI = null)
            {
                if (string.IsNullOrEmpty(name))
                    name = CuiHelper.GetGuid();

                var element = new CuiElement
                {
                    Name = name,
                    Parent = parent,
                    DestroyUi = destroyUI,
                    Components =
                    {
                        GetImageComponent(),
                        GetRectTransform()
                    }
                };

                if (CursorEnabled)
                    element.Components.Add(new CuiNeedsCursorComponent());

                if (KeyboardEnabled)
                    element.Components.Add(new CuiNeedsKeyboardComponent());

                return element;
            }

            #endregion

            #region Constructors

            public ImageSettings()
            {
            }

            public ImageSettings(string imageURL, IColor color, InterfacePosition position) : base(position)
            {
                Image = imageURL;
                Color = color;
            }

            #endregion
        }

        public class ButtonSettings : TextSettings
        {
            #region Fields

            [JsonProperty(PropertyName = "Button Color")]
            public IColor ButtonColor = IColor.CreateWhite();

            [JsonProperty(PropertyName = "Sprite")]
            public string Sprite = string.Empty;

            [JsonProperty(PropertyName = "Material")]
            public string Material = string.Empty;

            [JsonProperty(PropertyName = "Image")] public string Image = string.Empty;

            [JsonProperty(PropertyName = "Image Color")]
            public IColor ImageColor = IColor.CreateWhite();

            [JsonProperty(PropertyName = "Use custom image position settings?")]
            public bool UseCustomPositionImage = false;

            [JsonProperty(PropertyName = "Custom image position settings")]
            public InterfacePosition ImagePosition = CreateFullStretch();

            #endregion

            #region Public Methods

            public bool TryGetImageURL(out string url)
            {
                if (!string.IsNullOrWhiteSpace(Image) && Image.IsURL())
                {
                    url = Image;
                    return true;
                }

                url = null;
                return false;
            }

            public List<CuiElement> GetButton(
                string msg,
                string cmd,
                string parent,
                string name = null,
                string destroyUI = null,
                string close = null)
            {
                if (string.IsNullOrEmpty(name))
                    name = CuiHelper.GetGuid();

                var list = new List<CuiElement>();

                var btn = new CuiButtonComponent
                {
                    Color = ButtonColor.Get()
                };

                if (!string.IsNullOrEmpty(cmd))
                    btn.Command = cmd;

                if (!string.IsNullOrEmpty(close))
                    btn.Close = close;

                if (!string.IsNullOrEmpty(Sprite))
                    btn.Sprite = Sprite;

                if (!string.IsNullOrEmpty(Material))
                    btn.Material = Material;

                list.Add(new CuiElement
                {
                    Name = name,
                    Parent = parent,
                    DestroyUi = destroyUI,
                    Components =
                    {
                        btn,
                        GetRectTransform()
                    }
                });

                if (!string.IsNullOrEmpty(Image))
                {
                    list.Add(new CuiElement
                    {
                        Parent = name,
                        Components =
                        {
                            Image.StartsWith("assets/")
                                ? new CuiImageComponent {Color = ImageColor.Get(), Sprite = Image}
                                : new CuiRawImageComponent {Color = ImageColor.Get(), Png = Instance.GetImage(Image)},

                            UseCustomPositionImage && ImagePosition != null
                                ? ImagePosition?.GetRectTransform()
                                : new CuiRectTransformComponent()
                        }
                    });
                }
                else
                {
                    if (!string.IsNullOrEmpty(msg))
                        list.Add(new CuiElement
                        {
                            Parent = name,
                            Components =
                            {
                                GetTextComponent(msg),
                                new CuiRectTransformComponent()
                            }
                        });
                }

                return list;
            }

            #endregion
        }

        public class TextSettings : InterfacePosition
        {
            #region Fields

            [JsonProperty(PropertyName = "Font Size")]
            public int FontSize = 12;

            [JsonProperty(PropertyName = "Is Bold?")]
            public bool IsBold;

            [JsonProperty(PropertyName = "Align")]
            [JsonConverter(typeof(StringEnumConverter))]
            public TextAnchor Align = TextAnchor.UpperLeft;

            [JsonProperty(PropertyName = "Color")] public IColor Color = IColor.CreateWhite();

            #endregion Fields

            #region Public Methods

            public CuiTextComponent GetTextComponent(string msg)
            {
                return new CuiTextComponent
                {
                    Text = msg ?? string.Empty,
                    FontSize = FontSize,
                    Font = IsBold ? "robotocondensed-bold.ttf" : "robotocondensed-regular.ttf",
                    Align = Align,
                    Color = Color.Get(),
                    VerticalOverflow = VerticalWrapMode.Overflow
                };
            }

            public CuiElement GetText(string msg,
                string parent,
                string name = null,
                string destroyUI = null)
            {
                if (string.IsNullOrEmpty(name))
                    name = CuiHelper.GetGuid();

                return new CuiElement
                {
                    Name = name,
                    Parent = parent,
                    DestroyUi = destroyUI,
                    Components =
                    {
                        GetTextComponent(msg),
                        GetRectTransform()
                    }
                };
            }

            #endregion
        }

        public class InterfacePosition
        {
            #region Fields

            [JsonProperty(PropertyName = "AnchorMin")]
            public string AnchorMin = "0 0";

            [JsonProperty(PropertyName = "AnchorMax")]
            public string AnchorMax = "1 1";

            [JsonProperty(PropertyName = "OffsetMin")]
            public string OffsetMin = "0 0";

            [JsonProperty(PropertyName = "OffsetMax")]
            public string OffsetMax = "0 0";

            #endregion

            #region Cache

            [JsonIgnore] private CuiRectTransformComponent _position;

            #endregion

            #region Public Methods

            public CuiRectTransformComponent GetRectTransform()
            {
                if (_position != null) return _position;

                var rect = new CuiRectTransformComponent();

                if (!string.IsNullOrEmpty(AnchorMin))
                    rect.AnchorMin = AnchorMin;

                if (!string.IsNullOrEmpty(AnchorMax))
                    rect.AnchorMax = AnchorMax;

                if (!string.IsNullOrEmpty(OffsetMin))
                    rect.OffsetMin = OffsetMin;

                if (!string.IsNullOrEmpty(OffsetMax))
                    rect.OffsetMax = OffsetMax;

                _position = rect;

                return _position;
            }

            #endregion

            #region Constructors

            public InterfacePosition()
            {
            }

            public InterfacePosition(InterfacePosition other)
            {
                AnchorMin = other.AnchorMin;
                AnchorMax = other.AnchorMin;
                OffsetMin = other.AnchorMin;
                OffsetMax = other.AnchorMin;
            }

            public static InterfacePosition CreatePosition(float aMinX, float aMinY, float aMaxX, float aMaxY,
                float oMinX, float oMinY, float oMaxX, float oMaxY)
            {
                return new InterfacePosition
                {
                    AnchorMin = $"{aMinX} {aMinY}",
                    AnchorMax = $"{aMaxX} {aMaxY}",
                    OffsetMin = $"{oMinX} {oMinY}",
                    OffsetMax = $"{oMaxX} {oMaxY}"
                };
            }

            public static InterfacePosition CreatePosition(
                string anchorMin = "0 0",
                string anchorMax = "1 1",
                string offsetMin = "0 0",
                string offsetMax = "0 0")
            {
                return new InterfacePosition
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax,
                    OffsetMin = offsetMin,
                    OffsetMax = offsetMax
                };
            }

            public static InterfacePosition CreatePosition(CuiRectTransform rectTransform)
            {
                return new InterfacePosition
                {
                    AnchorMin = rectTransform.AnchorMin,
                    AnchorMax = rectTransform.AnchorMax,
                    OffsetMin = rectTransform.OffsetMin,
                    OffsetMax = rectTransform.OffsetMax
                };
            }

            public static InterfacePosition CreateFullStretch()
            {
                return new InterfacePosition
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                    OffsetMin = "0 0",
                    OffsetMax = "0 0"
                };
            }

            public static InterfacePosition CreateCenter()
            {
                return new InterfacePosition
                {
                    AnchorMin = "0.5 0.5",
                    AnchorMax = "0.5 0.5",
                    OffsetMin = "0 0",
                    OffsetMax = "0 0"
                };
            }

            #endregion Constructors
        }

        public class IColor
        {
            #region Fields

            [JsonProperty(PropertyName = "HEX")] public string HEX;

            [JsonProperty(PropertyName = LangRu ? "Непрозрачность (0 - 100)" : "Opacity (0 - 100)")]
            public float Alpha;

            #endregion

            #region Public Methods

            [JsonIgnore] private string _cachedResult;

            [JsonIgnore] private bool _isCached;

            public string Get()
            {
                if (_isCached)
                    return _cachedResult;

                if (string.IsNullOrEmpty(HEX)) HEX = "#FFFFFF";

                var str = HEX.Trim('#');
                if (str.Length != 6)
                    throw new Exception(HEX);

                var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
                var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
                var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

                _cachedResult = $"{(double)r / 255} {(double)g / 255} {(double)b / 255} {Alpha / 100}";
                _isCached = true;

                return _cachedResult;
            }

            #endregion

            #region Constructors

            public IColor()
            {
            }

            public IColor(string hex, float alpha = 100)
            {
                HEX = hex;
                Alpha = alpha;
            }

            public static IColor Create(string hex, float alpha = 100)
            {
                return new IColor(hex, alpha);
            }

            public static IColor CreateTransparent()
            {
                return new IColor("#000000", 0);
            }

            public static IColor CreateWhite()
            {
                return new IColor("#FFFFFF");
            }

            public static IColor CreateBlack()
            {
                return new IColor("#000000");
            }

            #endregion
        }

        #endregion

        #region Testing Funtions

#if TESTING
        [ConsoleCommand("vb.debug.start.decay")]
        private void DebugStartDecay(ConsoleSystem.Arg arg)
        {
            var entity = GetLookEntity<Minicopter>(arg.Player());
            if (entity == null)
            {
                SendReply(arg, "No entity found");
                return;
            }
            
            float num = (float) (1.0 / (entity.IsOutside() ? (double) PlayerHelicopter.outsidedecayminutes : (double) PlayerHelicopter.insidedecayminutes));
            entity.Hurt(entity.MaxHealth() * num, DamageType.Decay, (BaseEntity) entity, false);

            Puts($"start decay: {entity} | amt: {entity.MaxHealth() * num}");
            
            T GetLookEntity<T>(BasePlayer player)
            {
                RaycastHit hit;
                if (!Physics.Raycast(player.eyes.HeadRay(), out hit)) return default(T);

                var entity = hit.GetEntity();
                return entity == null ? default(T) : entity.GetComponent<T>();
            }
        }
#endif

        #endregion
    }
}

namespace Oxide.Plugins.VehicleBuyExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool IsURL(this string uriName)
        {
            return Uri.TryCreate(uriName, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}