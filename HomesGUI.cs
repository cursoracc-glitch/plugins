using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HomesGUI", "TopPlugin.ru", "1.4.22")]

    class HomesGUI : RustPlugin
    {
        #region Declarations 
        [PluginReference] Plugin TeleportGUI;
        
        private static HomesGUI Instance;

        private Collider[] colBuffer = new Collider[1024];

        private const string permUse = "homesgui.use";
        private const string permBack = "homesgui.back";
        private const string permBackBypass = "homesgui.back.bypass";
        private const string permViewOthersHomes = "homesgui.viewothershomes";
        private const string permDeleteOthersHomes = "homesgui.deleteothershomes";
        private const int PREVENT_BUILDING_LAYER = 536870912;

        private GameObject CMObject;
        private bool DebuggingMode = false;

        private Dictionary<BasePlayer, bool> GUIOpen = new Dictionary<BasePlayer, bool>();
        private Dictionary<BasePlayer, Vector3> HomeBack = new Dictionary<BasePlayer, Vector3>();

        private List<Vector3> OilRigs = new List<Vector3>();

        [PluginReference] private Plugin Economics, ServerRewards, NoEscape, ZoneManager;
        #endregion

        #region Classes

        class GamePos
        {
            public float x;
            public float y;
            public float z;

            public GamePos(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public static explicit operator GamePos(Vector3 v) => new GamePos(v.x, v.y, v.z);

            public static implicit operator Vector3(GamePos p) => new Vector3(p.x, p.y, p.z);

            public Vector3 ToVector() => new Vector3(x, y, z);
        }

        class HomePoint
        {
            public string Name { get; private set; }
            public GamePos Position { get; private set; }
            public bool IsSleeping { get; private set; }
            public uint BedID { get; private set; }

            public HomePoint(string name, GamePos position, bool isSleeping = false, uint bedID = 0)
            {
                this.Name = name;
                this.Position = position;
                this.IsSleeping = isSleeping;
                this.BedID = bedID;
            }

            public void UpdateName(string newName)
            {
                this.Name = newName;
            }
        }

        class HomeTeleporter : MonoBehaviour
        {
            BasePlayer Player;
            int TimeUntilTeleport;
            public Vector3 Pos;
            public string HomeName;
            public bool PlayerIsPaying;
            public bool IsTPBack;

            void Awake()
            {
                Player = GetComponent<BasePlayer>();

                TimeUntilTeleport = Instance.ConfigFile.DefaultTimeUntilTeleport;

                foreach (var kvp in Instance.ConfigFile.TimeUntilTeleport)
                {
                    if (Instance.permission.UserHasPermission(Player.UserIDString, kvp.Key))
                    {
                        if (kvp.Value < TimeUntilTeleport)
                            TimeUntilTeleport = kvp.Value;
                    }
                }
            }

            void Start()
            {
                SendReply(Player, Instance.GetMessage("TeleportingTo").Replace("{0}", HomeName).Replace("{1}", TimeUntilTeleport.ToString()));
                InvokeHandler.InvokeRepeating(this, TimerTick, 0, 1.0f);
            }

            void TimerTick()
            {
                if (TimeUntilTeleport == 0)
                {
                    if (Instance.IsInFoundation(Pos))
                    {
                        Player.ChatMessage(Instance.GetMessage("HomeIsInFoundation"));
                        Destroy(this);
                    }
                    else Teleport();
                    return;
                }

                if (Player == null || !Player.IsConnected)
                {
                    CancelTeleport();
                    return;
                }

                TimeUntilTeleport--;
            }

            public void CancelTeleport()
            {
                if (Instance.EconomicsInstalled() &&
                    Instance.ConfigFile.UseEconomicsPlugin &&
                    this.PlayerIsPaying)
                {
                    Instance.RefundPlayerEconomics(this.Player);
                }

                if (Instance.ServerRewardsInstalled() &&
                    Instance.ConfigFile.UseServerRewardsPlugin &&
                    this.PlayerIsPaying)
                {
                    Instance.RefundPlayerEconomics(this.Player);
                }

                if (Instance.storedData.Cooldowns.ContainsKey(this.Player.userID))
                {
                    Instance.storedData.Cooldowns.Remove(this.Player.userID);
                }

                Destroy(this);
            }

            void Teleport()
            {
                var call = Interface.Oxide.CallHook("CanTeleport", this.Player);
                if (call != null)
                {
                    SendReply(this.Player, call.ToString());
                    CancelTeleport();
                    return;
                }

                bool escapeBlocked = Instance.NoEscape?.Call<bool>("IsBlocked", this.Player) ?? false;
                if (escapeBlocked)
                {
                    SendReply(this.Player, Instance.GetMessage("IsEscapeBlocked"));
                    CancelTeleport();
                    return;
                }

                if (!IsTPBack)
                    Instance.RecordHomeBack(Player);
                Instance.Teleport(Player, Pos);
                SendReply(Player, Instance.GetMessage("TeleportedTo").Replace("{0}", HomeName));

                Instance.AssignCooldown(Player);
                Instance.RecordUse(Player);

                int uses = Instance.storedData.Uses[Player.userID];
                int maxUses = Instance.GetDailyMaxUses(Player);
                if (maxUses != 0)
                {
                    int usesRemaining = maxUses - uses;

                    if (usesRemaining < 0)
                    {
                        SendReply(this.Player, Instance.GetMessage("TeleportsUsed").Replace("{0}", uses.ToString()));
                    }
                    else
                    {
                        SendReply(this.Player, Instance.GetMessage("MaxUsesRemaining").Replace("{0}", uses.ToString()).Replace("{1}", usesRemaining.ToString()));
                    }
                }

                if (Instance.ConfigFile.ForceWakeOnTp)
                {
                    Player.Invoke(() =>
                    {
                        Player.EndSleeping();
                        SendReply(Player, Instance.GetMessage("ForcedAwake"));
                    }, 1.5f);
                }
                Destroy(this);
            }

            void OnDestroy()
            {
                InvokeHandler.CancelInvoke(this, TimerTick);
            }
        }

        class CooldownManager : MonoBehaviour
        {
            void Awake()
            {
                InvokeHandler.InvokeRepeating(this, TimerTick, 0, 1.0f);
            }

            void TimerTick()
            {
                foreach (var kvp in new Dictionary<ulong, int>(Instance.storedData.Cooldowns))
                {
                    Instance.storedData.Cooldowns[kvp.Key]--;
                    if (kvp.Value == 0)
                        Instance.storedData.Cooldowns.Remove(kvp.Key);
                }
            }
        }

        class GUIManager
        {
            public static Dictionary<BasePlayer, GUIManager> Players = new Dictionary<BasePlayer, GUIManager>();

            public bool Delete = false;

            public static GUIManager Get(BasePlayer player)
            {
                if (!Players.ContainsKey(player))
                {
                    Players.Add(player, new GUIManager());
                }

                return Players[player];
            }
        }

        class StoredData
        {
            public Dictionary<ulong, List<HomePoint>> Homes = new Dictionary<ulong, List<HomePoint>>();
            public Dictionary<ulong, int> Cooldowns = new Dictionary<ulong, int>();
            public Dictionary<ulong, int> Uses = new Dictionary<ulong, int>();
        }
        StoredData storedData;

        #endregion

        #region Hooks

        void Init()
        {
            DebuggingMode = (ConVar.Server.hostname == "PsychoTea's Testing Server");

            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permBack, this);
            permission.RegisterPermission(permBackBypass, this);
            permission.RegisterPermission(permViewOthersHomes, this);
            permission.RegisterPermission(permDeleteOthersHomes, this);

            foreach (string perm in ConfigFile.Cooldowns.Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            foreach (string perm in ConfigFile.MaxHomes.Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            foreach (string perm in ConfigFile.TimeUntilTeleport.Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            foreach (string perm in ConfigFile.MaxUses.Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "NoPermission", "You do not have permission to use this command." },
                { "HomesTitle", "Homes" },
                { "SetHome-Usage", "Incorrect usage! /sethome {name}" },
                { "HomeAlreadyExists", "You already have a home called {0}." },
                { "HomeCreated", "Home {0} set." },
                { "DelHome-Usage", "Incorrect usage! /delhome {name}" },
                { "HomeDoesntExist", "The home {0} doesn't exist." },
                { "HomeDeleted", "Home {0} deleted." },
                { "HomeInBuildBlock", "You may not set a home whilst build blocked." },
                { "NoHomesSet", "You have no homes set." },
                { "HomesList", "{0}" },
                { "TeleportingTo", "Teleporting to home {0} in {1} seconds..." },
                { "TeleportedTo", "You have teleported to home {0}." },
                { "HomeLimitedReached", "You have reached the home limit." },
                { "TeleportWhilstBuildBlock", "You may not use teleport whilst building blocked." },
                { "TeleportWhilstBleeding", "You may not teleport whilst bleeding." },
                { "TeleportWhilstCrafting", "You may not teleport whilst crafting." },
                { "OnCooldown", "Your home teleport is on cooldown for {0} seconds." },
                { "YouTookDamage", "You took damage; home teleport cancelled." },
                { "CantAffordEconomics", "You can't afford this! Price: ${0}" },
                { "EconomicsYouSpent", "You spent ${0} on this home teleport." },
                { "EconomicsRefunded", "You were refunded ${0}." },
                { "CantAffordServerRewards", "You can't afford this! Price: {0}RP" },
                { "ServerRewardsYouSpent", "You spent {0}RP on this home teleport." },
                { "ServerRewardsRefunded", "You were refunded {0}RP." },
                { "MustBeOnFoundation", "You must be on a foundation to set a home." },
                { "MustBeOnFoundationOrFloor", "You must be on a foundation or a floor to set a home." },
                { "HomeBlockInsideFoundation", "This home position is inside a foundation" },
                { "HomeBuildBlockDestroyed", "The building your home was set on has been destroyed." },
                { "NoPreviousHomes", "You have no previous homes to return to." },
                { "TeleportedBack", "Teleported back to your previous location." },
                { "AlreadyTeleporting", "You are already teleporting somewhere." },
                { "NoTeleportsToCancel", "You don't have any teleports to cancel." },
                { "TeleportCancelled", "Teleport cancelled." },
                { "TeleportIntoBuildBlock", "You may not teleport into a building blocked zone." },
                { "NoSetHomeInZones", "You may not set home in this ZoneManager zone." },
                { "MaxUsesReached", "You have reached your max uses of {0} for today." },
                { "MaxUsesRemaining", "You have used {0} home teleports today, {1} remaining." },
                { "TeleportsUsed", "You have used {0} home teleports today." },
                { "CantTPWhilstWounded", "You may not TP whilst wounded." },
                { "BedHomeCreated", "Your new home {0} has been created." },
                { "BedHomeDestroyed", "Your home {0} has been destroyed." },
                { "BedHomeUpdated", "Your home {0} has been renamed to {1}." },
                { "SetHomeDisabled", "Manually setting new homes is disabled." },
                { "IsEscapeBlocked", "You are currently escape blocked and may not teleport." },
                { "HomeSetWithinRadius", "You already have a home set {0}m from your current location. You must be at least {1}m away." },
                { "PlayerIsOnCargoShip", "You may not teleport whilst on a cargoship." },
                { "PlayerIsOnHotAirBalloon", "You may not teleport whilst on a hot air balloon." },
                { "PlayerIsNearOilRig", "You may not teleport whilst within 100m of an oil rig." },
                { "PlayerIsInUnderwaterLab", "You may not teleport whilst in the underwater labs" },
                { "PlayerIsMounted", "You may not teleport whilst mounted" },
                { "DailyUsesRemainingGUI", "Daily Uses Remaining: {0}" },
                { "FailedToFindPlayer", "Unable to find player '{0}'." },
                { "FoundMultiplePlayers", "Found multiple players matching the name '{0}'." },
                { "TargetHasNoSetHomes", "The player {0} has no homes set." },
                { "ListingHomesForPlayer", "Listing homes for {0}:\n{1}" },
                { "TargetHomeDoesntExist", "Player {0} does not have a home named '{1}'." },
                { "TeleportFromMonument", "You cannot teleport from monuments." },
                { "TeleportFromSafeZone", "You cannot teleport from safezones." },
                { "ForcedAwake", "You were automatically woken up." },
                { "HomeIsInFoundation", "Invalid Home : Your home position is inside a foundation" }
            }, this, "en");

            foreach (string cmdAlias in ConfigFile.HomeCommandAliases)
                cmd.AddChatCommand(cmdAlias, this, "homeCommand");
        }

        void OnServerInitialized()
        {
            Instance = this;

            if (!ReadData())
                return;
            
            if (Economics == null &&
                ConfigFile.UseEconomicsPlugin)
            {
                Debug.LogError("[TeleportGUI] Error! Economics is enabled in the config but is not installed! Please install Economics or disable 'UseEconomicsPlugin' in the config!");
            }

            if (ServerRewards == null &&
                ConfigFile.UseServerRewardsPlugin)
            {
                Debug.LogError("[TeleportGUI] Error! ServerRewards is enabled in the config but is not installed! Please install ServerRewards or disable 'UseServerRewardsPlugin' in the config!");
            }

            if (!ConfigFile.UseEconomicsPlugin &&
                !ConfigFile.UseServerRewardsPlugin &&
                ConfigFile.PayAfterUsingDailyLimits)
            {
                Debug.LogError("[TeleportGUI] Error! PayAfterUsingDailyLimits is set in the config, but neither UseEconomicsPlugin or UseServerRewardsPlugin is set! Please fix this error before loading HomesGUI again. Unloading...");
                Interface.Oxide.UnloadPlugin(this.Title);
                return;
            }
            
            OilRigs = UnityEngine.Object.FindObjectsOfType<MonumentInfo>().Where(x => x.gameObject.name.ToLower().Contains("oilrig")).Select(x => x.transform.position).ToList();

            CMObject = new GameObject();
            CMObject.AddComponent<CooldownManager>();

            timer.Every(60f, () =>
            {
                if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
                {
                    storedData.Uses.Clear();
                }
            });

            if (DebuggingMode)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    ShowUI(player);
            }
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                HideUI(player);

            if (!Interface.Oxide.IsShuttingDown)
                SaveData();

            DestroyAllOfType<HomeTeleporter>(false);

            UnityEngine.Object.Destroy(CMObject);

            GUIManager.Players.Clear();

            Instance = null;
        }

        void OnServerSave()
        {
            SaveData();
        }

        void OnNewSave()
        {
            if (ConfigFile.WipeHomesOnNewServerSave)
            {
                Puts($"New save detected - wiping home data");

                ReadData();

                storedData.Cooldowns.Clear();
                storedData.Uses.Clear();
                storedData.Homes.Clear();

                SaveData();
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!ConfigFile.ShouldCancelOnDamage) return;
            if (!(entity is BasePlayer)) return;
            BasePlayer player = entity as BasePlayer;

            if (HasComponent<HomeTeleporter>(player))
            {
                player.GetComponent<HomeTeleporter>().CancelTeleport();
                SendReply(player, GetMessage("YouTookDamage"));
            }
        }

        void OnEntitySpawned(BaseEntity entity)
        {
            if (!ConfigFile.SleepingBags.CreateHomeOnBagPlacement && !ConfigFile.SleepingBags.CreateHomeOnBedPlacement && !ConfigFile.SleepingBags.CreateHomeOnBeachTowelPlacement)            
                return;            

            BasePlayer player = BasePlayer.FindByID(entity.OwnerID);
            if (player == null) return;

            SleepingBag bag = entity as SleepingBag;
            if (bag == null) return;

            if ((entity.ShortPrefabName == "sleepingbag_leather_deployed" && ConfigFile.SleepingBags.CreateHomeOnBagPlacement) ||
                (entity.ShortPrefabName == "bed_deployed" && ConfigFile.SleepingBags.CreateHomeOnBedPlacement) ||
                (entity.ShortPrefabName == "beachtowel.deployed" && ConfigFile.SleepingBags.CreateHomeOnBeachTowelPlacement))
            {
                if (ConfigFile.SleepingBags.OnlyCreateInBuilding && entity.IsOutside())
                    return;

                if (!CheckCanCreateHome(player))
                {
                    SendReply(player, GetMessage("HomeLimitReached"));
                    return;
                }

                var zmgrCall = ZoneManager?.CallHook("PlayerHasFlag", player, "notp");
                if (zmgrCall != null && zmgrCall is bool && (bool)zmgrCall)
                {
                    SendReply(player, GetMessage("NoSetHomeInZones"));
                    return;
                }

                if (!storedData.Homes.ContainsKey(player.userID))                
                    storedData.Homes.Add(player.userID, new List<HomePoint>());                

                var homes = storedData.Homes[player.userID];
                string homeName = bag.niceName;

                var bagCount = homes.Count(x => x.Name == homeName);
                if (bagCount > 0)
                {
                    homeName += $" {bagCount}";
                }

                var newHome = new HomePoint(homeName, (GamePos)bag.transform.position, true, bag.net.ID);
                storedData.Homes[player.userID].Add(newHome);
                SendReply(player, GetMessage("BedHomeCreated").Replace("{0}", newHome.Name));
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!ConfigFile.SleepingBags.CreateHomeOnBagPlacement &&
                !ConfigFile.SleepingBags.CreateHomeOnBedPlacement)
                return;

            BasePlayer player = info?.InitiatorPlayer;
            if (player == null) return;

            SleepingBag bag = entity as SleepingBag;
            if (bag == null) return;

            if (!storedData.Homes.ContainsKey(player.userID)) return;

            var matchingHome = storedData.Homes[player.userID].FirstOrDefault(x => x.IsSleeping && x.BedID == bag.net.ID);
            if (matchingHome == null) return;

            storedData.Homes[player.userID].Remove(matchingHome);
            SendReply(player, GetMessage("BedHomeDestroyed").Replace("{0}", matchingHome.Name));
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            SleepingBag bag = entity as SleepingBag;
            if (bag == null) return;

            if (storedData?.Homes == null)
            {
                PrintError("HomesGUI data file is corrupted. Unload the plugin and remove it");
                return;
            }

            foreach (var homes in storedData.Homes)
            {
                var foundHome = homes.Value?.FirstOrDefault(x => x != null && x.IsSleeping && x.BedID == bag.net.ID);
                if (foundHome == null)
                    continue;

                BasePlayer player = BasePlayer.FindByID(homes.Key);

                if (player != null && player.IsConnected)
                    SendReply(player, GetMessage("BedHomeDestroyed").Replace("{0}", foundHome.Name));
                homes.Value.Remove(foundHome);
                return;
            }
        }

        void CanRenameBed(BasePlayer player, SleepingBag bed, string bedName)
        {
            if (!storedData.Homes.ContainsKey(player.userID)) return;

            var homes = storedData.Homes[player.userID];

            var bagHome = homes.FirstOrDefault(x => x.IsSleeping && x.BedID == bed.net.ID);

            if (bagHome == null) return;

            if (string.IsNullOrEmpty(bedName))
            {
                bedName = "Unnamed Sleeping Bag";
            }

            if (bedName.Length > 24)
            {
                bedName = bedName.Substring(0, 22) + "..";
            }

            var bagCount = homes.Count(x => x.Name == bedName);
            if (bagCount > 0)
            {
                bedName += $" {bagCount}";
            }

            bagHome.UpdateName(bedName);

            SendReply(player, GetMessage("BedHomeUpdated").Replace("{0}", bed.niceName)
                                                          .Replace("{1}", bagHome.Name));
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (HasComponent<HomeTeleporter>(player))
                player.GetComponent<HomeTeleporter>().CancelTeleport();
        }

        #endregion

        #region Config

        ConfigData ConfigFile;

        class ConfigData
        {
            public class _SleepingBags
            {
                [JsonProperty("Create Home On Bag Placement")]
                public bool CreateHomeOnBagPlacement = false;

                [JsonProperty("Create Home On Bed Placement")]
                public bool CreateHomeOnBedPlacement = false;

                [JsonProperty("Create Home On Beach Towel Placement")]
                public bool CreateHomeOnBeachTowelPlacement = false;

                [JsonProperty("Only create a home on placement if it is inside a building")]
                public bool OnlyCreateInBuilding = true;

                [JsonProperty("Remove Home On Bag/Bed Removal")]
                public bool RemoveHomeOnRemoval = false;

                [JsonProperty("Disable SetHome Command")]
                public bool DisableSetHomeCommand = false;
            }

            public bool PrefixEnabled = true;
            public string PrefixText = "<color=orange>Homes: </color>";
            public int DefaultTimeUntilTeleport = 15;
            public Dictionary<string, int> TimeUntilTeleport = new Dictionary<string, int>()
            {
                { "homesgui.vip", 10 },
                { "homesgui.elite", 5 },
                { "homesgui.god", 3 },
                { "homesgui.none", 0 }
            };
            public List<string> HomeCommandAliases = new List<string>() { };
            public int DefaultCooldown = 180;
            public Dictionary<string, int> Cooldowns = new Dictionary<string, int>()
            {
                { "homesgui.vip", 60 },
                { "homesgui.elite", 30 },
                { "homesgui.god", 15 },
                { "homesgui.none", 0 }
            };
            public int DefaultMaxHomes = 3;
            public Dictionary<string, int> MaxHomes = new Dictionary<string, int>()
            {
                { "homesgui.vip", 4 },
                        { "homesgui.elite", 6 },
                        { "homesgui.god", 10 },
                        { "homesgui.unlimited", 0 }
            };
            public int DefaultMaxUses = 5;
            public Dictionary<string, int> MaxUses = new Dictionary<string, int>()
            {
                { "homesgui.vip", 10 },
                        { "homesgui.elite", 20 },
                        { "homesgui.god", 30 },
                        { "homesgui.unlimited", 0 }
            };
            public bool AllowTeleportWhilstBleeding = false;
            public bool AllowTeleportFromBuildBlock = false;
            public bool AllowTeleportToBuildBlock = false;
            public bool AllowTeleportFromCargoShip = false;
            public bool AllowTeleportFromHotAirBalloon = false;
            public bool AllowTeleportFromOilRig = false;
            public bool AllowTeleportFromUnderwaterLabs = false;
            public bool AllowTeleportFromMounted = false;
            public bool UseEconomicsPlugin = false;
            public double EconomicsPrice = 100;
            public bool UseServerRewardsPlugin = false;
            public double ServerRewardsPrice = 100;
            public bool PayAfterUsingDailyLimits = false;
            public bool BlockTPCrafting = true;
            public bool AdminInstaTP = false;
            public bool AllowSetHomeInBuildBlocked = false;
            public bool MustSetHomeOnBuilding = true;
            public bool CanSetHomeOnFloor = false;
            public bool ShouldCancelOnDamage = true;
            public bool MinimumHomeRadiusEnabled = false;
            public float MinimumHomeRadiusDistance = 20f;
            public bool CanTeleportFromMonuments = true;
            public bool CanTeleportFromSafeZones = true;
            public bool WipeHomesOnNewServerSave = false;
            public bool ForceWakeOnTp = false;
            public ulong ChatIconID = 0UL;
            public _SleepingBags SleepingBags = new _SleepingBags();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            ConfigFile = Config.ReadObject<ConfigData>();
            Config.WriteObject(ConfigFile, true);
        }

        protected override void LoadDefaultConfig() => ConfigFile = new ConfigData();

        protected override void SaveConfig() => Config.WriteObject(ConfigFile, true);

        #endregion

        #region Commands

        [ChatCommand("home")]
        void HomeCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length > 0)
            {
                string option = args[0].ToLower();

                /* Support for NTeleportation format: /home add/remove name */
                if (option == "add")
                {
                    SetHomeCommand(player, "sethome", args.Skip(1).ToArray());
                    return;
                }

                if (option == "remove")
                {
                    DelHomeCommand(player, "delhome", args.Skip(1).ToArray());
                    return;
                }

                if (!storedData.Homes.ContainsKey(player.userID))
                {
                    storedData.Homes.Add(player.userID, new List<HomePoint>());
                }

                string homeName = string.Join(" ", args);

                var targetHome = storedData.Homes[player.userID].SingleOrDefault(x => x.Name.ToLower() == homeName.ToLower());

                if (targetHome == null)
                {
                    SendReply(player, GetMessage("HomeDoesntExist").Replace("{0}", homeName));
                    return;
                }

                Vector3 homePos = targetHome.Position;

                bool playerIsPaying = false;
                object canTP = AllowedToTeleport(player, homePos, out playerIsPaying);
                if (canTP is string)
                {
                    SendReply(player, canTP.ToString());
                    return;
                }

                HomeTeleporter ht = player.gameObject.AddComponent<HomeTeleporter>();
                ht.Pos = homePos;
                ht.HomeName = homeName;
                ht.PlayerIsPaying = playerIsPaying;

                return;
            }

            ShowHomesUI(player);
        }

        [ChatCommand("sethome")]
        void SetHomeCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (ConfigFile.SleepingBags.DisableSetHomeCommand)
            {
                SendReply(player, GetMessage("SetHomeDisabled"));
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, GetMessage("SetHome-Usage"));
                return;
            }

            if (!storedData.Homes.ContainsKey(player.userID))
            {
                storedData.Homes.Add(player.userID, new List<HomePoint>());
            }

            string homeName = string.Join(" ", args);

            var search = storedData.Homes[player.userID].Where(x => x.Name.ToLower() == homeName.ToLower());
            if (search.Count() != 0)
            {
                SendReply(player, GetMessage("HomeAlreadyExists").Replace("{0}", homeName));
                return;
            }

            if (!CanSetHome(player))
                return;

            var newHome = new HomePoint(homeName, (GamePos)player.transform.position);
            storedData.Homes[player.userID].Add(newHome);
            SaveData();
            SendReply(player, GetMessage("HomeCreated").Replace("{0}", homeName));
        }

        [ChatCommand("delhome")]
        void DelHomeCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, GetMessage("DelHome-Usage"));
                return;
            }

            if (args.Length > 1 && HasPerm(player, permDeleteOthersHomes))
            {
                string targetName = args[0];

                bool multipleMatches = false;
                BasePlayer target = TryFindPlayer(targetName, out multipleMatches);

                if (target == null)
                {
                    SendReply(player, GetMessage("FailedToFindPlayer"), targetName);
                    return;
                }

                if (multipleMatches)
                {
                    SendReply(player, GetMessage("FoundMultiplePlayers"), targetName);
                    return;
                }

                if (!storedData.Homes.ContainsKey(target.userID))
                {
                    storedData.Homes.Add(target.userID, new List<HomePoint>());
                }

                var targetHomes = storedData.Homes[target.userID];

                string targetHomeName = string.Join(" ", args.Skip(1));

                var targetHome = targetHomes.SingleOrDefault(x => x.Name.ToLower() == targetHomeName.ToLower());

                if (targetHome == null)
                {
                    SendReply(player, GetMessage("TargetHomeDoesntExist").Replace("{0}", target.displayName)
                                                                         .Replace("{1}", targetHomeName));
                    return;
                }

                storedData.Homes[player.userID].Remove(targetHome);
                SaveData();
                SendReply(player, GetMessage("HomeDeleted").Replace("{0}", targetHomeName));
                return;
            }

            if (!storedData.Homes.ContainsKey(player.userID))
            {
                storedData.Homes.Add(player.userID, new List<HomePoint>());
            }

            string homeName = string.Join(" ", args);

            var search = storedData.Homes[player.userID].Where(x => x.Name.ToLower() == homeName.ToLower());
            if (!search.Any())
            {
                SendReply(player, GetMessage("HomeDoesntExist").Replace("{0}", homeName));
                return;
            }

            var home = search.FirstOrDefault();

            storedData.Homes[player.userID].Remove(home);
            SaveData();
            SendReply(player, GetMessage("HomeDeleted").Replace("{0}", homeName));
        }

        [ChatCommand("listhomes")]
        void ListHomesCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length > 0)
            {
                if (!HasPerm(player, permViewOthersHomes))
                {
                    SendReply(player, GetMessage("NoPermission"));
                    return;
                }

                string playerName = string.Join(" ", args);
                SendReply(player, $"Viewing homes for {playerName}:");

                bool multipleMatches = false;
                BasePlayer target = TryFindPlayer(playerName, out multipleMatches);

                if (target == null)
                {
                    SendReply(player, GetMessage("FailedToFindPlayer"), playerName);
                    return;
                }

                if (multipleMatches)
                {
                    SendReply(player, GetMessage("FoundMultiplePlayers"), playerName);
                    return;
                }

                if (!storedData.Homes.ContainsKey(target.userID))
                {
                    storedData.Homes.Add(target.userID, new List<HomePoint>());
                }

                var targetHomes = storedData.Homes[target.userID];

                if (targetHomes.Count() == 0)
                {
                    SendReply(player, GetMessage("TargetHasNoSetHomes"), target.displayName);
                    return;
                }

                string targetHomeNames = string.Join(", ", targetHomes.Select(x => x.Name).ToArray());
                SendReply(player, GetMessage("ListingHomesForPlayer"), target.displayName, targetHomeNames);

                return;
            }

            if (!storedData.Homes.ContainsKey(player.userID))
            {
                storedData.Homes.Add(player.userID, new List<HomePoint>());
            }

            var homes = storedData.Homes[player.userID];

            if (homes.Count() == 0)
            {
                SendReply(player, GetMessage("NoHomesSet"));
                return;
            }

            string homeNames = string.Join(", ", homes.Select(x => x.Name).ToArray());
            SendReply(player, GetMessage("HomesList").Replace("{0}", homeNames));
        }

        [ChatCommand("homec")]
        void HomeCancelCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            SendReply(player, TPCancel(player));
        }

        [ChatCommand("homeback")]
        void HomeBackCommand(BasePlayer player, string command, string[] args)
        {
            if (HasPerm(player, permBackBypass))
            {
                TPBack(player);
                return;
            }

            if (HasPerm(player, permBack))
            {
                TPBackLimited(player);                
                return;
            }

            SendReply(player, GetMessage("NoPermission"));
        }

        [ConsoleCommand("homegui")]
        void HomeGUICommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            BasePlayer player = arg.Player();

            string[] args = arg.Args ?? new string[] { };

            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length == 0)
            {
                ShowHomesUI(player);
                return;
            }

            string option = args[0].ToLower();

            if (option == "true" || option == "close")
            {
                ShowHomesUI(player);
                return;
            }

            if (option == "to")
            {
                if (!storedData.Homes.ContainsKey(player.userID))
                {
                    storedData.Homes.Add(player.userID, new List<HomePoint>());
                }

                string homeName = string.Join(" ", args.Skip(1).ToArray());

                var search = storedData.Homes[player.userID].Where(x => x.Name.ToLower() == homeName.ToLower());
                if (search.Count() == 0)
                {
                    SendReply(player, GetMessage("HomeDoesntExist").Replace("{0}", homeName));
                    return;
                }

                HomePoint home = search.FirstOrDefault();

                bool playerIsPaying = false;
                object canTP = AllowedToTeleport(player, home.Position, out playerIsPaying);
                if (canTP is string)
                {
                    SendReply(player, canTP.ToString());
                    ShowHomesUI(player);
                    return;
                }

                if (ConfigFile.AdminInstaTP && player.IsAdmin)
                {
                    Debug.Log("insta");
                    ShowHomesUI(player);
                    SendReply(player, GetMessage("TeleportedTo").Replace("{0}", homeName));
                    Teleport(player, home.Position);
                    return;
                }

                ShowHomesUI(player);

                HomeTeleporter ht = player.gameObject.AddComponent<HomeTeleporter>();
                ht.Pos = home.Position;
                ht.HomeName = homeName;
                ht.PlayerIsPaying = playerIsPaying;

                return;
            }

            if (option == "back")
            {
                if (HasPerm(player, permBackBypass))
                    TPBack(player);
                else if (HasPerm(player, permBack))                
                    TPBackLimited(player);  
               
                ShowHomesUI(player);
                return;
            }

            if (option == "delete")
            {
                if (args.Length == 1)
                {
                    GUIManager.Get(player).Delete = !GUIManager.Get(player).Delete;
                    ShowUI(player);
                    return;
                }

                string homeName = string.Join(" ", args.Skip(1));

                var search = storedData.Homes[player.userID].Where(x => x.Name.ToLower() == homeName.ToLower());
                if (!search.Any())
                {
                    SendReply(player, GetMessage("HomeDoesntExist").Replace("{0}", homeName));
                    ShowUI(player);
                    return;
                }

                var home = search.FirstOrDefault();

                storedData.Homes[player.userID].Remove(home);
                SaveData();
                SendReply(player, GetMessage("HomeDeleted").Replace("{0}", homeName));
                ShowUI(player);

                return;
            }

            if (option == "page")
            {
                int page = arg.GetInt(1, 0);
                ShowUI(player, page);
                return;
            }
        }

        [ConsoleCommand("homesgui.resetdatafile")]
        void ResetAllCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
            {
                return;
            }

            storedData.Cooldowns.Clear();
            storedData.Uses.Clear();
            SaveData();
            Puts("Cleared data file and saved.");
        }

        #endregion

        #region External Hooks

        string CancelAllTeleports(BasePlayer player)
        {
            if (HasComponent<HomeTeleporter>(player)) return TPCancel(player);
            return null;
        }

        #endregion

        #region GUIs

        void ShowHomesUI(BasePlayer player)
        {
            if (!GUIOpen.ContainsKey(player))
                GUIOpen.Add(player, false);

            if (!GUIOpen[player])
            {
                ShowUI(player);
                GUIOpen[player] = true;
                return;
            }

            if (GUIOpen[player])
            {
                HideUI(player);
                GUIOpen[player] = false;
                return;
            }
        }

        void ShowUI(BasePlayer player, int page = 0)
        {
            HideUI(player);

            var GUIElement = new CuiElementContainer();

            var wholePanel = GUIElement.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0.3 0.3",
                    AnchorMax = "0.7 0.75"
                },
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                CursorEnabled = true
            }, "Hud", "homesGUI");

            #region Title Bar

            var titleBar = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.9", //Left Bottom
                    AnchorMax = "0.998 0.999" // Right Top
                }
            }, wholePanel);

            #region Homes/Uses

            int homesCount = GetPlayerHomesCount(player);
            int maxHomesCount = GetPlayerMaxHomesCount(player);

            string homesText = $"Homes: {homesCount}";

            if (maxHomesCount > 0)
            {
                homesText += $"/{maxHomesCount}";
            }

            if (DailyMaxUsesEnabled())
            {
                int dailyUsesRemaining = GetDailyUsesRemaining(player);

                if (dailyUsesRemaining < 0)
                {
                    dailyUsesRemaining = 0;
                }

                homesText += $", Uses Remaining: {dailyUsesRemaining} ";

                if (dailyUsesRemaining == 0 &&
                    ConfigFile.PayAfterUsingDailyLimits)
                {
                    string costText = " (";

                    if (ConfigFile.UseEconomicsPlugin)
                    {
                        costText += $"${ConfigFile.EconomicsPrice}";
                    }

                    if (ConfigFile.UseEconomicsPlugin && ConfigFile.UseServerRewardsPlugin)
                    {
                        costText += " / ";
                    }

                    if (ConfigFile.UseServerRewardsPlugin)
                    {
                        costText += $"{ConfigFile.ServerRewardsPrice}RP";
                    }

                    costText += ")";

                    homesText += costText;
                }

            }

            GUIElement.Add(new CuiLabel
            {
                Text =
                {
                    Text = homesText,
                    FontSize = 14,
                    Align = TextAnchor.MiddleLeft
                },
                RectTransform =
                {
                    AnchorMin = "0.02 0",
                    AnchorMax = "1 1"
                }
            }, titleBar);

            #endregion

            #region Title

            GUIElement.Add(new CuiLabel
            {
                Text =
                {
                    Text = lang.GetMessage("HomesTitle", this),
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, titleBar);

            #endregion

            #region Delete Button

            string deleteColour = GUIManager.Get(player).Delete ? "1 0.15 0.15 1" : "0.5 0.5 0.5 1";

            GUIElement.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0.734 0",
                    AnchorMax = "0.834 0.97"
                },
                Text =
                {
                    Text = "Delete",
                    Color = "1 1 1 1",
                    Align = TextAnchor.MiddleCenter
                },
                Button =
                {
                    Command = "homegui delete",
                    Color = deleteColour
                }
            }, titleBar);

            #endregion

            #region Back Button

            if (HasPerm(player, permBack))
            {
                string backCommand = HasLastHome(player) ? "homegui back" : "";
                string backColour = HasLastHome(player) ? "0.15 0.15 1 1" : "0.5 0.5 0.5 1";

                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0.834 0",
                        AnchorMax = "0.934 0.97"
                    },
                    Text =
                    {
                        Text = "Back",
                        Color = "1 1 1 1",
                        Align = TextAnchor.MiddleCenter
                    },
                    Button =
                    {
                        Command = backCommand,
                        Color = backColour
                    }
                }, titleBar);
            }

            #endregion

            #region Close Button

            GUIElement.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0.935 0",
                    AnchorMax = "0.998 0.97"
                },
                Button =
                {
                    Command = "homegui close",
                    Color = "1 0 0 1"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, titleBar);

            #endregion

            #endregion

            #region Homes List

            var homesList = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 0.9"
                }
            }, wholePanel);

            const float columnWidth = 0.2f;
            const float rowWidth = 0.2f;

            bool deleteEnabled = GUIManager.Get(player).Delete;

            if (storedData.Homes.ContainsKey(player.userID))
            {
                var homes = storedData.Homes[player.userID].OrderBy(x => x.Name).ToList();

                int homeCount = page * 25;

                for (int i = 0; i < 5 && homeCount < homes.Count; i++)
                {
                    for (int j = 0; j < 5 && homeCount < homes.Count; j++)
                    {
                        var panel = GUIElement.Add(new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = (columnWidth * j).ToString() + " " + (1f - (rowWidth * i) - rowWidth).ToString(),
                                AnchorMax = ((columnWidth * j) + columnWidth).ToString() + " " + (1f - (rowWidth * i)).ToString()
                            },
                            Image =
                            {
                                Color = "0 0 0 0"
                            }
                        }, homesList);

                        string homeName = homes[homeCount].Name;

                        GUIElement.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            },
                            Text =
                            {
                                Text = homeName,
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 18,
                                Color = "1 1 1 1",
                                Font = "robotocondensed-regular.ttf"
                            },
                            Button =
                            {
                                Command = $"homegui to {homeName}",
                                Color = "0 0 0 0"
                            }
                        }, panel);

                        if (deleteEnabled)
                        {
                            GUIElement.Add(new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0.7 0.005",
                                    AnchorMax = "0.97 0.2"
                                },
                                Text =
                                {
                                    Text = "Delete",
                                    Color = "1 1 1 1",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 10
                                },
                                Button =
                                {
                                    Command = $"homegui delete {homeName}",
                                    Color = "1.00 0.15 0.15 0.5"
                                }
                            }, panel);
                        }

                        homeCount++;
                    }
                }

                if (page > 0)
                {
                    GUIElement.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.8 -0.07",
                            AnchorMax = "0.898 0"
                        },
                        Button =
                        {
                            Command = $"homegui page {page - 1}",
                            Color = "1 0 0 1"
                        },
                        Text =
                        {
                            Text = "<<<",
                            FontSize = 20,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        }
                    }, "homesGUI");
                }

                if ((page * 25) + 25 < homes.Count)
                {
                    GUIElement.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.9 -0.07",
                            AnchorMax = "0.998 0"
                        },
                        Button =
                        {
                            Command = $"homegui page {page + 1}",
                            Color = "1 0 0 1"
                        },
                        Text =
                        {
                            Text = ">>>",
                            FontSize = 20,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        }
                    }, "homesGUI");
                }
            }

            #endregion

            #region Empty List

            if (!storedData.Homes.ContainsKey(player.userID) ||
                (storedData.Homes.ContainsKey(player.userID) &&
                storedData.Homes[player.userID].Count() == 0))
            {
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("NoHomesSet", this),
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.1",
                        AnchorMax = "1 1"
                    }
                }, homesList);
            }

            #endregion

            CuiHelper.AddUi(player, GUIElement);
        }

        void HideUI(BasePlayer player) => CuiHelper.DestroyUi(player, "homesGUI");

        #endregion

        #region API

        Dictionary<string, Vector3> GetPlayerHomes(string steamID)
        {
            ulong userID;
            if (!ulong.TryParse(steamID, out userID))
            {
                return null;
            }

            if (!storedData.Homes.ContainsKey(userID))
            {
                return null;
            }

            // return storedData.Homes[userID].ToDictionary(k => k.Key, v => v.Value.ToVector());
            return storedData.Homes[userID].ToDictionary(k => k.Name, v => v.Position.ToVector());
        }

        int GetPlayerCooldown(string steamID)
        {
            ulong userID;
            if (!ulong.TryParse(steamID, out userID))
            {
                return -1;
            }

            if (!storedData.Cooldowns.ContainsKey(userID))
            {
                return -1;
            }

            return storedData.Cooldowns[userID];
        }

        int GetPlayerUses(string steamID)
        {
            ulong userID;
            if (!ulong.TryParse(steamID, out userID))
            {
                return -1;
            }

            if (!storedData.Uses.ContainsKey(userID))
            {
                return -1;
            }

            return storedData.Uses[userID];
        }

        #endregion

        #region Functions

        void TPBack(BasePlayer player)
        {
            if (!HomeBack.ContainsKey(player))
            {
                SendReply(player, GetMessage("NoPreviousHomes"));
                return;
            }

            Teleport(player, HomeBack[player]);
            SendReply(player, GetMessage("TeleportedBack"));
        }

        void TPBackLimited(BasePlayer player)
        {
            if (!HomeBack.ContainsKey(player))
            {
                SendReply(player, GetMessage("NoPreviousHomes"));
                return;
            }

            Vector3 homePos = HomeBack[player];

            bool playerIsPaying = false;
            object canTP = AllowedToTeleport(player, homePos, out playerIsPaying, true);
            if (canTP is string)
            {
                SendReply(player, canTP.ToString());
                return;
            }

            HomeTeleporter ht = player.gameObject.AddComponent<HomeTeleporter>();
            ht.Pos = homePos;
            ht.HomeName = "Previous Location";
            ht.PlayerIsPaying = playerIsPaying;
            ht.IsTPBack = true;
        }

        string TPCancel(BasePlayer player)
        {
            if (!HasComponent<HomeTeleporter>(player))
                return GetMessage("NoTeleportsToCancel");

            var ht = player.GetComponent<HomeTeleporter>();
            ht.CancelTeleport();
            return GetMessage("TeleportCancelled");
        }

        void RecordHomeBack(BasePlayer player)
        {
            if (HomeBack.ContainsKey(player))
                HomeBack.Remove(player);
            HomeBack.Add(player, player.transform.position);
        }

        bool CanSetHome(BasePlayer player)
        {
            if (!CheckCanCreateHome(player))
            {
                SendReply(player, GetMessage("HomeLimitedReached"));
                return false;
            }

            if (!ConfigFile.AllowSetHomeInBuildBlocked && !player.CanBuild())
            {
                SendReply(player, GetMessage("HomeInBuildBlock"));
                return false;
            }

            if (ConfigFile.MinimumHomeRadiusEnabled)
            {
                var homes = storedData.Homes.SingleOrDefault(x => x.Key == player.userID).Value;

                foreach (var home in homes)
                {
                    float distance = Vector3.Distance(player.transform.position, home.Position);

                    if (distance <= ConfigFile.MinimumHomeRadiusDistance)
                    {
                        SendReply(player, GetMessage("HomeSetWithinRadius")
                            .Replace("{0}", distance.ToString("N1"))
                            .Replace("{1}", ConfigFile.MinimumHomeRadiusDistance.ToString("N1")));
                        return false;
                    }
                }
            }
            
            if (ConfigFile.MustSetHomeOnBuilding)
            {
                if (!ConfigFile.CanSetHomeOnFloor)
                {
                    if (!CheckFoundation(player.transform.position))
                    {
                        SendReply(player, GetMessage("MustBeOnFoundation"));
                        return false;
                    }
                }
                else
                {
                    if (!CheckFoundation(player.transform.position) && !CheckFloor(player.transform.position))
                    {
                        SendReply(player, GetMessage("MustBeOnFoundationOrFloor"));
                        return false;
                    }
                }
            }
            
            var zmgrCall = ZoneManager?.CallHook("PlayerHasFlag", player, "notp");
            if (zmgrCall != null && zmgrCall is bool && (bool)zmgrCall)
            {
                SendReply(player, GetMessage("NoSetHomeInZones"));
                return false;
            }

            return true;
        }

        bool IsInsideFoundation(Vector3 pos)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Ray(pos + (Vector3.up * 2f), Vector3.down), 3f, 1 << 21);

            for (int i = 0; i < hits.Length; i++)           
            {
                BuildingBlock block = hits[i].GetEntity() as BuildingBlock;
                if (block != null && block.ShortPrefabName.Contains("foundation"))
                {
                    if (block.transform.position.y > pos.y)
                        return true;
                }
            }
            return false;
        }

        bool FindBuildBlock(Vector3 pos, string BlockName)
        {
            RaycastHit[] hits = Physics.RaycastAll(new Ray(pos + Vector3.up, Vector3.down), 2f);

            if (hits.Count() == 0)
                return false;

            foreach (RaycastHit hit in hits)
            {
                string buildBlockName = hit.GetEntity()?.GetComponent<BuildingBlock>()?.ShortPrefabName;
                if (buildBlockName != null && buildBlockName == BlockName)
                    return true;
            }
            return false;
        }

        bool IsUnderFoundation(Vector3 pos)
        {
            bool hitBackFaces = Physics.queriesHitBackfaces;

            try
            {
                RaycastHit raycastHit;

                const string FOUNDATION_TRIANGLE = "foundation.triangle";
                const string FOUNDATION = "foundation";

                Physics.queriesHitBackfaces = true;
                if (Physics.Raycast(pos + (Vector3.up * 0.25f), Vector3.up, out raycastHit, 50f, 1 << 21, QueryTriggerInteraction.Collide))
                {
                    string buildBlockName = raycastHit.GetEntity()?.GetComponent<BuildingBlock>()?.ShortPrefabName;
                    if (buildBlockName != null && (buildBlockName == FOUNDATION || buildBlockName == FOUNDATION_TRIANGLE))
                        return true;
                }
                return false;
            }
            finally
            {
                Physics.queriesHitBackfaces = hitBackFaces;
            }
        }

        int GetPlayerHomesCount(BasePlayer player)
        {
            if (!storedData.Homes.ContainsKey(player.userID))
            {
                storedData.Homes.Add(player.userID, new List<HomePoint>());
            }

            return storedData.Homes[player.userID].Count;
        }

        int GetPlayerMaxHomesCount(BasePlayer player)
        {
            int maxHomes = ConfigFile.DefaultMaxHomes;

            foreach (var kvp in ConfigFile.MaxHomes
                .Where(x => permission.UserHasPermission(player.UserIDString, x.Key)))
            {
                int homes = kvp.Value;

                if (homes <= 0)
                {
                    return 0;
                }

                if (homes > maxHomes)
                {
                    maxHomes = homes;
                }
            }

            return maxHomes;
        }

        bool CheckCanCreateHome(BasePlayer player)
        {
            int maxHomes = GetPlayerMaxHomesCount(player);
            if (maxHomes == 0)
            {
                return true;
            }

            int homesCount = GetPlayerHomesCount(player);

            return maxHomes > homesCount;
        }

        bool DailyMaxUsesEnabled() => ConfigFile.DefaultMaxUses > 0;

        bool CheckHighestUses(BasePlayer player, out string uses)
        {
            int highestUses = ConfigFile.DefaultMaxUses;
            uses = highestUses.ToString();

            if (highestUses == 0) return false;

            foreach (var kvp in ConfigFile.MaxUses.Where(x => permission.UserHasPermission(player.UserIDString, x.Key)))
            {
                int permUses = kvp.Value;
                if (permUses == 0)
                {
                    highestUses = 0;
                    return false;
                }

                if (permUses > highestUses)
                {
                    highestUses = permUses;
                }
            }

            uses = highestUses.ToString();
            return (highestUses > 0 && storedData.Uses.ContainsKey(player.userID) && storedData.Uses[player.userID] >= highestUses);
        }

        int GetDailyMaxUses(BasePlayer player)
        {
            int highestUses = ConfigFile.DefaultMaxUses;

            foreach (var kvp in ConfigFile.MaxUses.Where(x => permission.UserHasPermission(player.UserIDString, x.Key)))
            {
                int permUses = kvp.Value;
                if (permUses == 0)
                {
                    highestUses = 0;
                    return highestUses;
                }

                if (permUses > highestUses)
                {
                    highestUses = permUses;
                }
            }

            return highestUses;
        }

        int GetDailyUsesRemaining(BasePlayer player)
        {
            if (!DailyMaxUsesEnabled())
            {
                return -1;
            }

            int maxUses = GetDailyMaxUses(player);

            if (!storedData.Uses.ContainsKey(player.userID))
            {
                return maxUses;
            }

            return maxUses - storedData.Uses[player.userID];
        }

        object AllowedToTeleport(BasePlayer player, Vector3 homePos, out bool playerIsPaying, bool isTpBack = false)
        {
            playerIsPaying = false;

            if (storedData.Cooldowns.ContainsKey(player.userID))
                return GetMessage("OnCooldown").Replace("{0}", storedData.Cooldowns[player.userID].ToString());

            if (player.IsWounded())
                return GetMessage("CantTPWhilstWounded");

            if (!ConfigFile.AllowTeleportFromBuildBlock)
            {
                if (!player.CanBuild())
                    return GetMessage("TeleportWhilstBuildBlock");
            }

            if (!ConfigFile.AllowTeleportToBuildBlock)
            {
                if (IsBuildingBlocked(player, homePos))
                    return GetMessage("TeleportIntoBuildBlock");
            }

            if (!ConfigFile.AllowTeleportWhilstBleeding)
            {
                if (player.metabolism.bleeding.value > 0f)
                    return GetMessage("TeleportWhilstBleeding");
            }


            if (!ConfigFile.AllowTeleportFromCargoShip && player.GetParentEntity() is CargoShip)
            {
                return GetMessage("PlayerIsOnCargoShip");
            }

            if (!ConfigFile.AllowTeleportFromHotAirBalloon && player.GetParentEntity() is HotAirBalloon)
            {
                return GetMessage("PlayerIsOnHotAirBalloon");
            }

            if (!ConfigFile.AllowTeleportFromOilRig && IsNearOilRig(player.transform.position))
            {
                return GetMessage("PlayerIsNearOilRig");
            }

            if (!ConfigFile.AllowTeleportFromUnderwaterLabs && IsInUnderwaterLab(player.transform.position))
            {
                return GetMessage("PlayerIsInUnderwaterLab");
            }

            if (IsInsideFoundation(homePos))
            {
                return GetMessage("HomeIsInFoundation");
            }

            if (ConfigFile.BlockTPCrafting)
            {
                if (IsCrafting(player))
                    return GetMessage("TeleportWhilstCrafting");
            }

            if (!ConfigFile.AllowTeleportFromMounted && player.isMounted)
                return GetMessage("PlayerIsMounted");

            if (!ConfigFile.CanTeleportFromMonuments)
            {
                if (!ConfigFile.CanTeleportFromSafeZones || (ConfigFile.CanTeleportFromSafeZones && !player.InSafeZone()))
                {
                    if (IsOnMonument(player))
                        return GetMessage("TeleportFromMonument");
                }
            }
            else
            {
                if (!ConfigFile.CanTeleportFromSafeZones)
                {
                    if (player.InSafeZone())
                        return GetMessage("TeleportFromSafeZone");
                }
            }

            if (ConfigFile.UseEconomicsPlugin && !ConfigFile.PayAfterUsingDailyLimits)
            {
                if (EconomicsInstalled())
                {
                    if (!PayEconomics(player))
                    {
                        return GetMessage("CantAffordEconomics").Replace("{0}", ConfigFile.EconomicsPrice.ToString());
                    }

                    SendReply(player, GetMessage("EconomicsYouSpent").Replace("{0}", ConfigFile.EconomicsPrice.ToString()));
                    playerIsPaying = true;
                }
            }

            if (ConfigFile.UseServerRewardsPlugin && !ConfigFile.PayAfterUsingDailyLimits)
            {
                if (ServerRewardsInstalled())
                {
                    if (!PayServerRewards(player))
                    {
                        return GetMessage("CantAffordServerRewards").Replace("{0}", ConfigFile.ServerRewardsPrice.ToString("N1"));
                    }

                    SendReply(player, GetMessage("ServerRewardsYouSpent").Replace("{0}", ConfigFile.ServerRewardsPrice.ToString("N1")));
                    playerIsPaying = true;
                }
            }

            string uses;
            if (CheckHighestUses(player, out uses))
            {
                if (!ConfigFile.PayAfterUsingDailyLimits)
                {
                    return GetMessage("MaxUsesReached").Replace("{0}", uses);
                }

                if (ConfigFile.UseEconomicsPlugin)
                {
                    if (!PayEconomics(player))
                    {
                        return GetMessage("CantAffordEconomics").Replace("{0}", ConfigFile.EconomicsPrice.ToString("N1"));
                    }

                    SendReply(player, GetMessage("EconomicsYouSpent").Replace("{0}", ConfigFile.EconomicsPrice.ToString("N1")));
                    playerIsPaying = true;
                }

                if (ConfigFile.UseServerRewardsPlugin)
                {
                    if (!PayServerRewards(player))
                    {
                        return GetMessage("CantAffordServerRewards").Replace("{0}", ConfigFile.ServerRewardsPrice.ToString("N1"));
                    }

                    SendReply(player, GetMessage("ServerRewardsYouSpent").Replace("{0}", ConfigFile.ServerRewardsPrice.ToString("N1")));
                    playerIsPaying = true;
                }
            }

            var call = Interface.Oxide.CallHook("CanTeleport", player);
            if (call != null) return call.ToString();

            bool escapeBlocked = NoEscape?.Call<bool>("IsBlocked", player) ?? false;
            if (escapeBlocked)
            {
                return GetMessage("IsEscapeBlocked");
            }

            if (!isTpBack)
            {
                if (ConfigFile.MustSetHomeOnBuilding)
                {
                    if (ConfigFile.CanSetHomeOnFloor)
                    {
                        if (!CheckFloor(homePos) && !CheckFoundation(homePos))
                            return GetMessage("HomeBuildBlockDestroyed");
                    }
                    else if (!CheckFoundation(homePos))
                        return GetMessage("HomeBuildBlockDestroyed");
                }

                if (IsInsideFoundation(homePos))
                    return GetMessage("HomeBlockInsideFoundation");
            }

            if (HasComponent<HomeTeleporter>(player)) return GetMessage("AlreadyTeleporting");

            return true;
        }

        void AssignCooldown(BasePlayer player)
        {
            int cooldown = ConfigFile.DefaultCooldown;
            foreach (var kvp in ConfigFile.Cooldowns)
            {
                if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                {
                    int cd = kvp.Value;
                    if (cd < cooldown) cooldown = cd;
                }
            }
            if (storedData.Cooldowns.ContainsKey(player.userID))
                storedData.Cooldowns[player.userID] = cooldown;
            else storedData.Cooldowns.Add(player.userID, cooldown);
            SaveData();
        }

        void RecordUse(BasePlayer player)
        {
            if (!storedData.Uses.ContainsKey(player.userID))
                storedData.Uses.Add(player.userID, 0);
            storedData.Uses[player.userID]++;
        }

        bool IsBuildingBlocked(BasePlayer player, Vector3 pos)
        {
            var buildPriv = player.GetBuildingPrivilege(new OBB(pos, new Quaternion(), new Bounds()));

            if (buildPriv == null)
            {
                return false;
            }

            return !buildPriv.IsAuthed(player);
        }

        BasePlayer TryFindPlayer(string searchName, out bool multipleMatches)
        {
            multipleMatches = false;

            var matches = BasePlayer.activePlayerList.Where(x =>
                x.displayName.ToLower().Contains(searchName.ToLower()));

            if (matches.Count() == 0)
            {
                return null;
            }

            if (matches.Count() > 1)
            {
                multipleMatches = true;

                return null;
            }

            return matches.Single();
        }

        bool IsOnMonument(BasePlayer player) 
        {
            int num = Physics.OverlapSphereNonAlloc(player.transform.position, 2f, colBuffer, PREVENT_BUILDING_LAYER);

            if (num == 0)
                return false;

            bool flag = false;

            for (int i = 0; i < num; i++)
            {      
                if (!flag)
                {
                    Collider col = colBuffer[i];
                    if (col.gameObject != null && !col.gameObject.name.Contains("oilrig", System.Globalization.CompareOptions.IgnoreCase))
                    {
                        Bounds bounds = col.bounds;

                        bounds.size /= 2f;

                        float distance = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);

                        if (Vector3.Distance(col.transform.position, player.transform.position) <= distance)                        
                            flag = true;                        
                    }
                }

                colBuffer[i] = null;
            }

            return flag;
        }

        bool IsNearOilRig(Vector3 position)
        {
            for (int i = 0; i < OilRigs.Count; i++)
            {
                if (Vector3Ex.Distance2D(position, OilRigs[i]) <= 100f)
                    return true;
            }

            return false;
        }

        bool IsInUnderwaterLab(Vector3 position)
        {
            int hits = Physics.OverlapSphereNonAlloc(position, 1f, Vis.colBuffer, 1 << 18);

            for (int i = 0; i < hits; i++)
            {
                Collider col = Vis.colBuffer[i];
                EnvironmentVolume environmentVolume = col.gameObject.GetComponent<EnvironmentVolume>();
                if (environmentVolume != null && (environmentVolume.Type & EnvironmentType.UnderwaterLab) == EnvironmentType.UnderwaterLab)
                    return true;
            }
            return false;
        }

        #region Server Rewards/Economics

        bool EconomicsInstalled() => Economics != null;

        bool ServerRewardsInstalled() => ServerRewards != null;

        bool PayEconomics(BasePlayer player)
        {
            if (player == null)
                return false;

            if (Economics == null)
            {
                PrintError("Trying to pay with Economics but its not installed!");
                return true;
            }

            double price = ConfigFile.EconomicsPrice;
            double playerMoney = (double)Economics?.Call("Balance", player.UserIDString);

            if (playerMoney >= price)
            {
                Economics?.Call("Withdraw", player.userID, price);
                return true;
            }

            return false;
        }

        bool PayServerRewards(BasePlayer player)
        {
            if (player == null)
                return false;

            if (ServerRewards == null)
            {
                PrintError("Trying to pay with ServerRewards but its not installed!");
                return true;
            }

            double price = ConfigFile.ServerRewardsPrice;
            int currentPoints;
            var call = ServerRewards?.Call("CheckPoints", player.userID);
            if (call == null) currentPoints = 0;
            else currentPoints = (int)call;

            if (currentPoints - price >= 0)
            {
                ServerRewards?.Call("TakePoints", player.userID, price);
                return true;
            }
            return false;
        }

        void RefundPlayerEconomics(BasePlayer player)
        {
            if (player == null)
                return;

            if (Economics == null)
            {
                PrintError("Trying to refund with Economics but its not installed!");
                return;
            }

            double price = ConfigFile.EconomicsPrice;

            Economics?.Call("Deposit", player.userID, price);

            SendReply(player, GetMessage("EconomicsRefunded").Replace("{0}", price.ToString()));
        }

        void RefundServerRewards(BasePlayer player)
        {
            if (player == null)
                return;

            if (ServerRewards == null)
            {
                PrintError("Trying to refund with ServerRewards but its not installed!");
                return;
            }

            double price = ConfigFile.ServerRewardsPrice;
            ServerRewards?.Call("AddPoints", player.userID, price);
            SendReply(player, GetMessage("ServerRewardsRefunded").Replace("{0}", price.ToString()));
        }

        #endregion

        void Teleport(BasePlayer player, Vector3 position)
        {
            TeleportGUI?.Call("RecordLastTP", player);

            try
            {
                if (player.isMounted)
                    player.GetMounted().DismountPlayer(player, true);

                player.SetParent(null, true, true);

                player.StartSleeping();
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);

                player.EnablePlayerCollider();
                player.SetServerFall(true);

                player.MovePosition(position);
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);

                player.UpdateNetworkGroup();
                player.SendNetworkUpdateImmediate(false);
                player.ClearEntityQueue(null);
                player.SendFullSnapshot();
            }
            finally
            {                
                player.EnablePlayerCollider();
                player.SetServerFall(false);
            }
        }

        #endregion

        #region Helpers

        bool HasLastHome(BasePlayer player) => HomeBack.ContainsKey(player);

        bool CheckFoundation(Vector3 homePos) => FindBuildBlock(homePos, "foundation") || FindBuildBlock(homePos, "foundation.triangle");
        bool CheckFloor(Vector3 homePos) => FindBuildBlock(homePos, "floor") || FindBuildBlock(homePos, "floor.triangle");
        bool IsInFoundation(Vector3 homePos) => IsUnderFoundation(homePos) || IsInsideFoundation(homePos);

        bool HasPerm(BasePlayer player) => (permission.UserHasPermission(player.UserIDString, permUse) || player.IsAdmin);
        bool HasPerm(BasePlayer player, string perm) => (permission.UserHasPermission(player.UserIDString, perm) || player.IsAdmin);

        bool IsCrafting(BasePlayer player) => player.inventory.crafting.queue.Count() > 0;

        static void SendReply(BasePlayer target, string message) => target.SendConsoleCommand("chat.add", new object[] { 0, Instance.ConfigFile.ChatIconID, message });

        string GetMessage(string key)
        {
            string message = "";
            if (ConfigFile.PrefixEnabled)
                message += ConfigFile.PrefixText;
            message += lang.GetMessage(key, this);
            return message;
        }

        bool HasComponent<T>(BasePlayer player) => (player.GetComponent<T>() != null);

        void DestroyAllOfType<T>(bool includeGameObject) where T : Component
        {
            T[] t = UnityEngine.Object.FindObjectsOfType<T>();

            for (int i = 0; i < t?.Length; i++)
            {
                if (includeGameObject)
                    UnityEngine.Object.Destroy(t[i].gameObject);
                else UnityEngine.Object.Destroy(t[i]);
            }
        }

        void SaveData()
        {
            if (storedData == null) return;

            Interface.Oxide.DataFileSystem?.WriteObject<StoredData>(this.Title, storedData);
        }

        bool ReadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(this.Title);
            }
            catch (Exception ex)
            {
                PrintError($"There was an error reading your data file! It may be corrupt or outdated - please delete it and load this plugin again.");
                Interface.Oxide.UnloadPlugin(this.Title);
                return false;
            }

            return true;
        }

        #endregion
    }
}