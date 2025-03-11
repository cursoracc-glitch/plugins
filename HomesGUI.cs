using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HomesGUI", "PsychoTea", "1.2.9")]

    class HomesGUI : RustPlugin
    {
        #region Declarations 

        private static HomesGUI Instance;
        private const string permUse = "homesgui.use";
        private const string permBack = "homesgui.back";

        private GameObject CMObject;
        private bool DebuggingMode = false;

        private Dictionary<BasePlayer, bool> GUIOpen = new Dictionary<BasePlayer, bool>();
        private Dictionary<BasePlayer, Vector3> HomeBack = new Dictionary<BasePlayer, Vector3>();

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
            BasePlayer Player { get { return GetComponentInParent<BasePlayer>(); } }
            int TimeUntilTeleport;
            public Vector3 Pos;
            public string HomeName;
            public bool PlayerIsPaying;

            public void Go()
            {
                Instance.SendReply(Player, Instance.GetMessage("TeleportingTo").Replace("{0}", HomeName).Replace("{1}", TimeUntilTeleport.ToString()));
                InvokeRepeating("TimerTick", 0, 1.0f);
            }

            void Awake()
            {
                name = "HomeTeleporter";

                TimeUntilTeleport = Instance.ConfigFile.DefaultTimeUntilTeleport;
                foreach (var kvp in Instance.ConfigFile.TimeUntilTeleport)
                    if (Instance.permission.UserHasPermission(Player.UserIDString, kvp.Key))
                        if (kvp.Value < TimeUntilTeleport)
                            TimeUntilTeleport = kvp.Value;
            }

            void TimerTick()
            {
                if (TimeUntilTeleport == 0)
                {
                    Teleport();
                    GameObject.Destroy(this);
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

                GameObject.Destroy(this);
            }

            void Teleport()
            {
                Instance.RecordHomeBack(Player);
                Instance.Teleport(Player, Pos);
                Instance.SendReply(Player, Instance.GetMessage("TeleportedTo").Replace("{0}", HomeName));

                Instance.AssignCooldown(Player);
                Instance.RecordUse(Player);

                int uses = Instance.storedData.Uses[Player.userID];
                int maxUses = Instance.GetDailyMaxUses(Player);
                if (maxUses != 0)
                {
                    int usesRemaining = maxUses - uses;

                    if (usesRemaining < 0)
                    {
                        Instance.SendReply(this.Player, Instance.GetMessage("TeleportsUsed").Replace("{0}", uses.ToString()));
                    }
                    else
                    {
                        Instance.SendReply(this.Player, Instance.GetMessage("MaxUsesRemaining").Replace("{0}", uses.ToString()).Replace("{1}", usesRemaining.ToString()));
                    }
                }
            }
        }

        class CooldownManager : MonoBehaviour
        {
            void Awake()
            {
                name = "CooldownManager";
                InvokeRepeating("TimerTick", 0, 1.0f);
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
                { "IsEscapeBlocked", "You are currently escape blocked and may not teleport." }
            }, this, "en");

            foreach (string cmdAlias in ConfigFile.HomeCommandAliases)
                cmd.AddChatCommand(cmdAlias, this, "homeCommand");
        }

        void OnServerInitialized()
        {
            Instance = this;

            if (!ReadData()) return;

            CMObject = new GameObject();
            CMObject.AddComponent<CooldownManager>();

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

            timer.Every(60f, () =>
            {
                if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
                {
                    storedData.Uses.Clear();
                }
            });

            if (DebuggingMode) BasePlayer.activePlayerList.ForEach(x => ShowUI(x));
        }

        void Unload()
        {
            BasePlayer.activePlayerList.ForEach(x => HideUI(x));

            SaveData();

            GameObject.Destroy(CMObject);
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
            if (!ConfigFile.SleepingBags.CreateHomeOnBagPlacement &&
                !ConfigFile.SleepingBags.CreateHomeOnBedPlacement)
                return;

            BasePlayer player = BasePlayer.FindByID(entity.OwnerID);
            if (player == null) return;

            SleepingBag bag = entity as SleepingBag;
            if (bag == null) return;

            if ((entity.ShortPrefabName == "sleepingbag_leather_deployed" && ConfigFile.SleepingBags.CreateHomeOnBagPlacement) ||
                (entity.ShortPrefabName == "bed_deployed" && ConfigFile.SleepingBags.CreateHomeOnBedPlacement))
            {
                if (!storedData.Homes.ContainsKey(player.userID))
                {
                    storedData.Homes.Add(player.userID, new List<HomePoint>());
                }

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

            foreach (var homes in storedData.Homes)
            {
                var foundHome = homes.Value.FirstOrDefault(x => x.IsSleeping && x.BedID == bag.net.ID);
                if (foundHome == null) continue;

                BasePlayer player = BasePlayer.FindByID(homes.Key);

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

                [JsonProperty("Remove Home On Bag/Bed Removal")]
                public bool RemoveHomeOnRemoval = false;
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
            public bool CheckForBuildingOnHomeTP = true;
            public bool ShouldCancelOnDamage = true;
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

                Vector3 pos = search.FirstOrDefault().Position;

                bool playerIsPaying = false;
                object canTP = AllowedToTeleport(player, pos, out playerIsPaying);
                if (canTP is string)
                {
                    SendReply(player, canTP.ToString());
                    return;
                }

                HomeTeleporter ht = player.gameObject.AddComponent<HomeTeleporter>();
                ht.Pos = pos;
                ht.HomeName = homeName;
                ht.PlayerIsPaying = playerIsPaying;
                ht.Go();
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

            if (!CanSetHome(player)) return;

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
            if (!HasPerm(player, permBack))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TPBack(player);
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

            if (args.Length == 0 || args[0] == "True" || args[0] == "close")
            {
                ShowHomesUI(player);
                return;
            }

            if (args[0] == "to")
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

                if (ConfigFile.AdminInstaTP && 
                    player.IsAdmin)
                {
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
                ht.Go();

                return;
            }

            if (args[0] == "back")
            {
                TPBack(player);
                ShowHomesUI(player);
                return;
            }
        }

        [ConsoleCommand("homesgui.resetdatafile")]
        void ResetAllCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;

            if (!DebuggingMode)
            {
                Debug.LogError("[TeleportGUI] You may not use this command. Warning: It is highly untested and unsafe. Please to not bypass this warning.");
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

        void ShowUI(BasePlayer player)
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

            if (storedData.Homes.ContainsKey(player.userID))
            {
                var homes = storedData.Homes[player.userID].OrderBy(x => x.Name).ToArray();

                int homeCount = 0;
                for (int i = 0; i < 5 && homeCount < homes.Count(); i++)
                {
                    for (int j = 0; j < 5 && homeCount < homes.Count(); j++)
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
                        homeCount++;
                    }
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
            if (!CheckMaxHomes(player))
            {
                SendReply(player, GetMessage("HomeLimitedReached"));
                return false;
            }

            if (!ConfigFile.AllowSetHomeInBuildBlocked && !player.CanBuild())
            {
                SendReply(player, GetMessage("HomeInBuildBlock"));
                return false;
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

            var zmgrCall = ZoneManager?.CallHook("EntityHasFlag", player, "notp");
            if (zmgrCall != null && zmgrCall is bool && (bool)zmgrCall)
            {
                SendReply(player, GetMessage("NoSetHomeInZones"));
                return false;
            }

            return true;
        }

        bool FindBuildBlock(Vector3 pos, string BlockName)
        {
            pos += new Vector3(0, 1f, 0);
            RaycastHit[] hits = Physics.RaycastAll(new Ray(pos, Vector3.down), 2f);
            if (hits.Count() == 0) return false;
            foreach (var hit in hits)
            {
                var buildBlockName = hit.GetEntity()?.GetComponent<BuildingBlock>()?.ShortPrefabName;
                if (buildBlockName != null && buildBlockName == BlockName) return true;
            }
            return false;
        }

        bool CheckMaxHomes(BasePlayer player)
        {
            if (!storedData.Homes.ContainsKey(player.userID))
            {
                storedData.Homes.Add(player.userID, new List<HomePoint>());
            }

            int maxHomes = ConfigFile.DefaultMaxHomes;
            foreach (var kvp in ConfigFile.MaxHomes)
            {
                if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                {
                    int homes = kvp.Value;
                    if (homes == 0) return true;
                    if (homes > maxHomes) maxHomes = homes;
                }
            }

            int homesCount = storedData.Homes[player.userID].Count();
            return (homesCount < maxHomes);
        }

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
        
        object AllowedToTeleport(BasePlayer player, Vector3 homePos, out bool playerIsPaying)
        {
            playerIsPaying = false;

            if (storedData.Cooldowns.ContainsKey(player.userID))
                return GetMessage("OnCooldown").Replace("{0}", storedData.Cooldowns[player.userID].ToString());

            if (player.IsWounded())
                return GetMessage("CantTPWhilstWounded");

            if (!ConfigFile.AllowTeleportFromBuildBlock)
                if (!player.CanBuild())
                    return GetMessage("TeleportWhilstBuildBlock");

            if (!ConfigFile.AllowTeleportToBuildBlock)
                if (IsBuildingBlocked(player, homePos))
                    return GetMessage("TeleportIntoBuildBlock");

            if (!ConfigFile.AllowTeleportWhilstBleeding)
                if (player.metabolism.bleeding.value > 0f)
                    return GetMessage("TeleportWhilstBleeding");

            if (ConfigFile.BlockTPCrafting)
                if (IsCrafting(player))
                    return GetMessage("TeleportWhilstCrafting");

            if (ConfigFile.UseEconomicsPlugin &&
                !ConfigFile.PayAfterUsingDailyLimits)
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

            if (ConfigFile.UseServerRewardsPlugin &&
                !ConfigFile.PayAfterUsingDailyLimits)
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
            var colliders = Pool.GetList<Collider>();
            Vis.Colliders(pos, 0.1f, colliders, LayerMask.GetMask("Trigger"));
            var cupboard = colliders.Select(x => x.GetComponentInParent<BuildingPrivlidge>()).Where(x => x != null).FirstOrDefault();
            Pool.FreeList(ref colliders);
            if (cupboard == null) return false;
            return player.userID != cupboard.OwnerID && !cupboard.IsAuthed(player);
        }

        #region Server Rewards/Economics

        bool EconomicsInstalled() => Economics != null;

        bool ServerRewardsInstalled() => ServerRewards != null;

        bool PayEconomics(BasePlayer player)
        {
            double price = ConfigFile.EconomicsPrice;
            double playerMoney = (double)Economics.Call("Balance", player.UserIDString);

            double moneyRemaining = playerMoney - price;
            if (moneyRemaining >= 0)
            {
                Economics.Call("SetMoney", player.userID, moneyRemaining);
                return true;
            }

            return false;
        }

        bool PayServerRewards(BasePlayer player)
        {
            double price = ConfigFile.ServerRewardsPrice;
            int currentPoints;
            var call = ServerRewards?.Call("CheckPoints", player.userID);
            if (call == null) currentPoints = 0;
            else currentPoints = (int)call;

            if (currentPoints - price >= 0)
            {
                ServerRewards.Call("TakePoints", player.userID, price);
                return true;
            }
            return false;
        }

        void RefundPlayerEconomics(BasePlayer player)
        {
            double price = ConfigFile.EconomicsPrice;
            double playerMoney = (double)Economics.Call("Balance", player.UserIDString);

            Economics.Call("SetMoney", player.userID, playerMoney + price);

            SendReply(player, GetMessage("EconomicsRefunded").Replace("{0}", price.ToString()));
        }

        void RefundServerRewards(BasePlayer player)
        {
            double price = ConfigFile.ServerRewardsPrice;
            ServerRewards.Call("AddPoints", player.userID, price);
            SendReply(player, GetMessage("ServerRewardsRefunded").Replace("{0}", price.ToString()));
        }

        #endregion

        void Teleport(BasePlayer player, Vector3 pos)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
            StartSleeping(player);
            player.MovePosition(pos);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", pos);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

        void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        #endregion

        #region Helpers

        bool HasLastHome(BasePlayer player) => HomeBack.ContainsKey(player);

        bool CheckFoundation(Vector3 homePos) => FindBuildBlock(homePos, "foundation") || FindBuildBlock(homePos, "foundation.triangle");
        bool CheckFloor(Vector3 homePos) => FindBuildBlock(homePos, "floor") || FindBuildBlock(homePos, "floor.triangle");

        bool HasPerm(BasePlayer player) => (permission.UserHasPermission(player.UserIDString, permUse) || player.IsAdmin);
        bool HasPerm(BasePlayer player, string perm) => (permission.UserHasPermission(player.UserIDString, perm) || player.IsAdmin);

        bool IsCrafting(BasePlayer player) => player.inventory.crafting.queue.Count() > 0;

        string GetMessage(string key)
        {
            string message = "";
            if (ConfigFile.PrefixEnabled)
                message += ConfigFile.PrefixText;
            message += lang.GetMessage(key, this);
            return message;
        }

        bool HasComponent<T>(BasePlayer player) => (player.GetComponent<T>() != null);

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