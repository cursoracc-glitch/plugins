using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;
using ProtoBuf;
using Facepunch;
using Facepunch.Extend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("DynamicCupShare", "FuJiCuRa", "2.7.2", ResourceId = 20)]
    [Description("Dynamic sharing of cupboards/doors/boxes/lockers/turrets/quarries")]
    internal class DynamicCupShare : RustPlugin
    {
        [PluginReference]
        private Plugin Clans, Friends;

        private static DynamicCupShare Instance { get; set; }

        private bool Changed = false;
        private bool Initialized = false;
        private bool clansEnabled = false;
        private bool friendsEnabled = false;
        private bool friendsAPIEnabled = false;
        private bool pluginDisabled = false;
        private List<ulong> usdCnslInpt = new List<ulong>();
        private Hash<ulong, bool> adminAccessEnabled = new Hash<ulong, bool>();
        private Hash<ulong, List<uint>> adminCupboards = new Hash<ulong, List<uint>>();
        private Hash<ulong, List<uint>> adminTurrets = new Hash<ulong, List<uint>>();
        private StoredData playerPrefs = new StoredData();
        private Dictionary<string, List<uint>> playerCupboards = new Dictionary<string, List<uint>>();
        private Dictionary<string, List<uint>> playerTurrets = new Dictionary<string, List<uint>>();
        private List<object> pseudoAdminPerms = new List<object>();
        private List<string> pseudoPerms = new List<string>();

        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        private class StoredData
        {
            public Dictionary<ulong, PlayerInfo> PlayerInfo = new Dictionary<ulong, PlayerInfo>();
            public int saveStamp = 0;
            public string lastStorage = string.Empty;

            public StoredData()
            {
            }
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        internal class PlayerInfo
        {
            public bool CS;
            public bool DS;
            public bool BS;
            public bool TS;
            public bool LS;
            public bool QS;
            public bool AA;
            public bool CCS;
            public bool CDS;
            public bool CBS;
            public bool CLS;
            public bool CQS;
            public bool CTS;
            [JsonIgnore] [ProtoIgnore] public bool CanAutoAuth;
            [JsonIgnore] [ProtoIgnore] public bool HasClanShare;
            [JsonIgnore] [ProtoIgnore] public bool HasFriendShare;
            [JsonIgnore] [ProtoIgnore] public BuildingPrivilegeHandler BPH;

            public PlayerInfo()
            {
                BPH = null;
            }
        }

        private int UnixTimeStampUTC()
        {
            int unixTimeStamp;
            DateTime currentTime = DateTime.Now;
            DateTime zuluTime = currentTime.ToUniversalTime();
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            unixTimeStamp = (int)zuluTime.Subtract(unixEpoch).TotalSeconds;
            return unixTimeStamp;
        }

        private string shareCommand;
        private bool useFriendsApi;
        private bool useClans;
        private bool useProtostorageUserdata;
        private bool blockCupClearClanMembers;
        private bool blockCupAccessNotSameClan;
        private bool blockCupClearFriends;
        private bool allowLaddersIntoBlocked;
        private bool allowTwigIntoBlocked;
        private bool notifyPlayersBeingBlocked;
        private bool allowIcebergBuilding;
        private bool allowIcesheetBuilding;
        private bool allowIcelakeBuilding;
        private string permGetClanShares;
        private bool usePermGetClanShares;
        private string permGetFriendShares;
        private bool usePermGetFriendShares;
        private string permAutoAuth;
        private bool usePermAutoAuth;
        private bool clanTurretShareOverride;
        private bool includeFlameTurrets;
        private bool includeGunTraps;
        private bool includeSamSites;
        private bool samSiteShootEmptyVehicles;
        private bool enableCupSharing;
        private bool enableDoorSharing;
        private bool enableBoxSharing;
        private bool enableLockerSharing;
        private bool enableTurretSharing;
        private bool enableAutoAuth;
        private bool enableQuarrySharing;
        private bool enableQuarrySwitchCheck;
        private bool notifyAuthCupboard;
        private bool notifyAuthTurret;
        private bool CupShare;
        private bool DoorShare;
        private bool TurretShare;
        private bool BoxShare;
        private bool LockerShare;
        private bool QuarryShare;
        private bool AutoAuth;
        private bool ClanCupShare;
        private bool ClanDoorShare;
        private bool ClanBoxShare;
        private bool ClanLockerShare;
        private bool ClanTurretShare;
        private bool ClanQuarryShare;
        private bool toggleCupShare;
        private bool toggleDoorShare;
        private bool toggleTurretShare;
        private bool toggleBoxShare;
        private bool toggleLockerShare;
        private bool toggleQuarryShare;
        private bool toggleAutoAuth;
        private bool toggleClanCupShare;
        private bool toggleClanDoorShare;
        private bool toggleClanBoxShare;
        private bool toggleClanLockerShare;
        private bool toggleClanQuarryShare;
        private bool toggleClanTurretShare;
        private bool adminsRemainCupAuthed;
        private bool adminsRemainTurretAuthed;
        private bool enableAdminmodeAtLogin;
        private int cupboardAuthMaxUsers;
        private string pluginPrefix;
        private string prefixColor;
        private string prefixFormat;
        private string colorTextMsg;
        private string colorCmdUsage;
        private string colorON;
        private string colorOFF;

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
            shareCommand = Convert.ToString(GetConfig("Command", "shareCommand", "share"));
            useFriendsApi = Convert.ToBoolean(GetConfig("Options", "useFriendsApi", true));
            useClans = Convert.ToBoolean(GetConfig("Options", "useClans", true));
            pseudoAdminPerms = (List<object>)GetConfig("Options", "pseudoAdminPerms",
                new List<object> { "fauxadmin.allowed", "fakeadmin.allow" });
            useProtostorageUserdata = Convert.ToBoolean(GetConfig("Storage", "useProtostorageUserdata", false));
            permGetClanShares =
                Convert.ToString(GetConfig("Permission", "permGetClanShares", "dynamiccupshare.getclanshares"));
            usePermGetClanShares = Convert.ToBoolean(GetConfig("Permission", "usePermGetClanShares", false));
            permGetFriendShares =
                Convert.ToString(GetConfig("Permission", "permGetFriendShares", "dynamiccupshare.getfriendshares"));
            usePermGetFriendShares = Convert.ToBoolean(GetConfig("Permission", "usePermGetFriendShares", false));
            permAutoAuth = Convert.ToString(GetConfig("Permission", "permGetShares", "dynamiccupshare.autoauth"));
            usePermAutoAuth = Convert.ToBoolean(GetConfig("Permission", "usePermAutoAuth", false));
            clanTurretShareOverride = Convert.ToBoolean(GetConfig("Security", "clanTurretShareOverride", false));
            blockCupClearClanMembers = Convert.ToBoolean(GetConfig("Security", "blockCupClearClanMembers", true));
            blockCupAccessNotSameClan = Convert.ToBoolean(GetConfig("Security", "blockCupAccessNotSameClan", false));
            blockCupClearFriends = Convert.ToBoolean(GetConfig("Security", "blockCupClearFriends", true));
            allowLaddersIntoBlocked = Convert.ToBoolean(GetConfig("Blocker", "allowLaddersIntoBlocked", true));
            allowTwigIntoBlocked = Convert.ToBoolean(GetConfig("Blocker", "allowTwigIntoBlocked", false));
            notifyPlayersBeingBlocked = Convert.ToBoolean(GetConfig("Blocker", "notifyPlayersBeingBlocked", true));
            allowIcebergBuilding = Convert.ToBoolean(GetConfig("Blocker", "allowIcebergBuilding", false));
            allowIcesheetBuilding = Convert.ToBoolean(GetConfig("Blocker", "allowIcesheetBuilding", true));
            allowIcelakeBuilding = Convert.ToBoolean(GetConfig("Blocker", "allowIcelakeBuilding", true));
            notifyAuthCupboard = Convert.ToBoolean(GetConfig("Notification", "notifyAuthCupboard", true));
            notifyAuthTurret = Convert.ToBoolean(GetConfig("Notification", "notifyAuthTurret", true));
            CupShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "CupShare", false));
            DoorShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "DoorShare", false));
            TurretShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "TurretShare", false));
            BoxShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "BoxShare", false));
            LockerShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "LockerShare", false));
            QuarryShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "QuarryShare", false));
            AutoAuth = Convert.ToBoolean(GetConfig("PlayerDefaults", "AutoAuth", true));
            ClanCupShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanCupShare", true));
            ClanDoorShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanDoorShare", true));
            ClanBoxShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanBoxShare", true));
            ClanLockerShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanLockerShare", true));
            ClanQuarryShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanQuarryShare", true));
            ClanTurretShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanTurretShare", true));
            toggleCupShare = Convert.ToBoolean(GetConfig("PlayerToggles", "CupShare", true));
            toggleDoorShare = Convert.ToBoolean(GetConfig("PlayerToggles", "DoorShare", true));
            toggleTurretShare = Convert.ToBoolean(GetConfig("PlayerToggles", "TurretShare", true));
            toggleBoxShare = Convert.ToBoolean(GetConfig("PlayerToggles", "BoxShare", true));
            toggleLockerShare = Convert.ToBoolean(GetConfig("PlayerToggles", "LockerShare", true));
            toggleQuarryShare = Convert.ToBoolean(GetConfig("PlayerToggles", "QuarryShare", true));
            toggleAutoAuth = Convert.ToBoolean(GetConfig("PlayerToggles", "AutoAuth", true));
            toggleClanCupShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanCupShare", true));
            toggleClanDoorShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanDoorShare", true));
            toggleClanBoxShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanBoxShare", true));
            toggleClanLockerShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanLockerShare", true));
            toggleClanQuarryShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanQuarryShare", true));
            toggleClanTurretShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanTurretShare", true));
            enableCupSharing = Convert.ToBoolean(GetConfig("Functions", "enableCupSharing", true));
            enableDoorSharing = Convert.ToBoolean(GetConfig("Functions", "enableDoorSharing", true));
            enableBoxSharing = Convert.ToBoolean(GetConfig("Functions", "enableBoxSharing", true));
            enableLockerSharing = Convert.ToBoolean(GetConfig("Functions", "enableLockerSharing", true));
            enableQuarrySharing = Convert.ToBoolean(GetConfig("Functions", "enableQuarrySharing", false));
            enableQuarrySwitchCheck = Convert.ToBoolean(GetConfig("Functions", "enableQuarrySwitchCheck", false));
            enableTurretSharing = Convert.ToBoolean(GetConfig("Functions", "enableTurretSharing", true));
            enableAutoAuth = Convert.ToBoolean(GetConfig("Functions", "enableAutoAuth", true));
            includeFlameTurrets = Convert.ToBoolean(GetConfig("Functions", "includeFlameTurrets", true));
            includeGunTraps = Convert.ToBoolean(GetConfig("Functions", "includeGunTraps", true));
            includeSamSites = Convert.ToBoolean(GetConfig("Functions", "includeSamSites", true));
            cupboardAuthMaxUsers = Convert.ToInt32(GetConfig("Functions", "cupboardAuthMaxUsers (0 is disabled)", 0));
            samSiteShootEmptyVehicles = Convert.ToBoolean(GetConfig("Functions", "samSiteShootEmptyVehicles", false));
            adminsRemainCupAuthed = Convert.ToBoolean(GetConfig("Adminmode", "adminsRemainCupAuthed", false));
            adminsRemainTurretAuthed = Convert.ToBoolean(GetConfig("Adminmode", "adminsRemainTurretAuthed", false));
            enableAdminmodeAtLogin = Convert.ToBoolean(GetConfig("Adminmode", "enableAdminmodeAtLogin", false));
            pluginPrefix = Convert.ToString(GetConfig("Formatting", "pluginPrefix", "DynaShare"));
            prefixColor = Convert.ToString(GetConfig("Formatting", "prefixColor", "#ffa500"));
            prefixFormat = Convert.ToString(GetConfig("Formatting", "prefixFormat", "<color={0}>{1}</color>: "));
            colorTextMsg = Convert.ToString(GetConfig("Formatting", "colorTextMsg", "#ffffff"));
            colorCmdUsage = Convert.ToString(GetConfig("Formatting", "colorCmdUsage", "#ffff00"));
            colorON = Convert.ToString(GetConfig("Formatting", "colorON", "#008000"));
            colorOFF = Convert.ToString(GetConfig("Formatting", "colorOFF", "#c0c0c0"));
            bool configremoval = false;
            if ((Config.Get("Security") as Dictionary<string, object>).ContainsKey("blockCupAuthClanMembers"))
            {
                (Config.Get("Security") as Dictionary<string, object>).Remove("blockCupAuthClanMembers");
                (Config.Get("Security") as Dictionary<string, object>).Remove("blockCupAuthFriends");
                configremoval = true;
            }

            if ((Config.Get("Functions") as Dictionary<string, object>).ContainsKey("addAdminsToCupboards"))
            {
                (Config.Get("Functions") as Dictionary<string, object>).Remove("addAdminsToCupboards");
                configremoval = true;
            }

            if ((Config.Get("Options") as Dictionary<string, object>).ContainsKey("pluginDelayOnFreshStart"))
            {
                (Config.Get("Options") as Dictionary<string, object>).Remove("pluginDelayOnFreshStart");
                configremoval = true;
            }

            if ((Config.Get("Blocker") as Dictionary<string, object>).ContainsKey("excludeAdminsFromBlocking"))
            {
                (Config.Get("Blocker") as Dictionary<string, object>).Remove("excludeAdminsFromBlocking");
                configremoval = true;
            }

            if (Config.Get("Blocking") != null)
            {
                Config.Remove("Blocking");
                configremoval = true;
            }

            if ((Config.Get("Options") as Dictionary<string, object>).ContainsKey("useFriendsIO"))
            {
                (Config.Get("Options") as Dictionary<string, object>).Remove("useFriendsIO");
                configremoval = true;
            }

            if (!Changed && !configremoval) return;
            SaveConfig();
            Changed = false;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string>
                {
                    {"ShareEnabled", "Cupboard sharing for friends enabled"},
                    {"ShareDisabled", "Cupboard sharing for friends disabled"},
                    {"CodesEnabled", "Door sharing for friends enabled"},
                    {"CodesDisabled", "Door sharing for friends disabled"},
                    {"BoxesEnabled", "Box sharing for friends enabled"},
                    {"BoxesDisabled", "Box sharing for friends disabled"},
                    {"LockersEnabled", "Locker sharing for friends enabled"},
                    {"LockersDisabled", "Locker sharing for friends disabled"},
                    {"QuarriesEnabled", "Quarry sharing for friends enabled"},
                    {"QuarriesDisabled", "Quarry sharing for friends disabled"},
                    {"TurretEnabled", "Turret sharing for friends enabled"},
                    {"TurretDisabled", "Turret sharing for friends disabled"},
                    {"ClanShareEnabled", "Cupboard sharing for clan enabled"},
                    {"ClanShareDisabled", "Cupboard sharing for clan disabled"},
                    {"ClanCodesEnabled", "Door sharing for clan enabled"},
                    {"ClanCodesDisabled", "Door sharing for clan disabled"},
                    {"ClanBoxesEnabled", "Box sharing for clan enabled"},
                    {"ClanBoxesDisabled", "Box sharing for clan disabled"},
                    {"ClanLockersEnabled", "Locker sharing for clan enabled"},
                    {"ClanLockersDisabled", "Locker sharing for clan disabled"},
                    {"ClanQuarriesEnabled", "Quarry sharing for clan enabled"},
                    {"ClanQuarriesDisabled", "Quarry sharing for clan disabled"},
                    {"ClanTurretEnabled", "Turret sharing for clan enabled"},
                    {"ClanTurretDisabled", "Turret sharing for clan disabled"},
                    {"AdminAccessEnabled", "Admin access mode enabled"},
                    {"AdminAccessDisabled", "Admin access mode disabled"},
                    {"AutoAuthEnabled", "Cupboard automatic authorization enabled"},
                    {"AutoAuthDisabled", "Cupboard automatic authorization disabled"},
                    {"CupAuth", "Cupboard authorized"}, {"TurretAuth", "Turret authorized"},
                    {"NoAccess", "You are not granted for this feature"},
                    {"NotEnabled", "The specific function '{0}' is currently not active"},
                    {"SwitchBlocked", "The admin blocked the '{0}' switch"},
                    {"SharedAll", "Enabled all available sharing functions"},
                    {"NotSupported", "The specific function '{0}' is not available"},
                    {"NotFound", "The player '{0}' was not found."}, {"NeedArgs", "Please define a target playername."},
                    {"CupAuthDisabledOwner", "Authorization denied. '{0}' has cup sharing deactivated"},
                    {"CupAuthDisabledSelf", "Authorization denied. You need to activate cup sharing"},
                    {"CupAuthClearBlocked", "Clear authorized list denied"},
                    {"CupAuthNotSameClanBlocked", "Authorization denied"},
                    {"CupAuthMaxUsers", "This cupboard already has the maximum amount of authorized players ({0})"},
                    {"BlockBuildIntoBlocked", "You can't build or place into blocked area!"},
                    {"BlockBuildOnIceBergs", "You can't build or place on icebergs"},
                    {"BlockBuildOnIceSheets", "You can't build or place on icesheets"},
                    {"BlockBuildOnIceLakes", "You can't build or place on icelakes"},
                    {"DoorClanNotShared", "Clan member '{0}' has door sharing deactivated"},
                    {"DoorClanNotSharedSelf", "Your clan door sharing is deactivated"},
                    {"BoxClanNotShared", "Clan member '{0}' has box sharing deactivated"},
                    {"BoxClanNotSharedSelf", "Your clan box sharing is deactivated"},
                    {"LockerClanNotShared", "Clan member '{0}' has locker sharing deactivated"},
                    {"LockerClanNotSharedSelf", "Your clan locker sharing is deactivated"},
                    {"CupClanNotShared", "Authorization denied. Clan member '{0}' has cup sharing deactivated"},
                    {"QuarryNoLootAccess", "You are not allowed access this storage."},
                    {"QuarryNoStartStop", "You are not allowed use this machine."},
                    {"AccessRights", "You can access these shares:"},
                    {"CommandPlgDisabled", "Plugin disabled! Please contact your server admin"},
                    {"CommandUsage", "Command usage:"},
                    {"CommandToggle", "All switches toggle their setting (on<>off)"},
                    {"CommandFriendCup", "Friends Cupboard:"}, {"CommandFriendDoor", "Friends Door:"},
                    {"CommandFriendBox", "Friends Box:"}, {"CommandFriendLocker", "Friends Locker:"},
                    {"CommandFriendQuarry", "Friends Quarry:"}, {"CommandFriendTurret", "Friends Turret:"},
                    {"CommandAutoAuth", "Cup/Turret authorization:"}, {"CommandClanCup", "Clan Cupboard:"},
                    {"CommandClanDoor", "Clan Door:"}, {"CommandClanBox", "Clan Box:"},
                    {"CommandClanLocker", "Clan Locker:"}, {"CommandClanQuarry", "Clan Quarry:"},
                    {"CommandClanTurret", "Clan Members not targeted by Turrets:"},
                    {"CommandClanTurretM", "Clan Turret:"}, {"CommandAdminAccess", "Admin access status"},
                    {"HelpCups", "Get the description for sharing of cupboards"},
                    {"HelpDoors", "Get the description for sharing of doors"},
                    {"HelpBoxes", "Get the description for sharing of boxes"},
                    {"HelpLockers", "Get the description for sharing of lockers"},
                    {"HelpQuarries", "Get the description for sharing of quarries"},
                    {"HelpTurrets", "Get the description for sharing of turrets"},
                    {"HelpAutoAuth", "Get the description for automatic authorization"},
                    {"HelpNotAvailable", "This help topics does'nt exist"},
                    {
                        "DescriptionCups",
                        "By enabling cup sharing for friends/members, then those players get build rights in every cupboard range, which you own and where yourself are authed. It does not share in case you are not selfauthed."
                    },
                    {
                        "DescriptionDoors",
                        "By enabling door sharing for friends/members, then those players can open each of your locked doors; Without any direct lock access."
                    },
                    {
                        "DescriptionBoxes",
                        "By enabling box sharing for friends/members, then those players can open each of your locked boxes; Without any direct lock access."
                    },
                    {
                        "DescriptionLockers",
                        "By enabling locker sharing for friends/members, then those players can open each of your lockers; Without any direct lock access."
                    },
                    {
                        "DescriptionQuarries",
                        "By enabling quarry sharing for friends/members, then those players can open each of your fuelstorages and hopperoutputs; Other players will be blocked."
                    },
                    {
                        "DescriptionTurrets",
                        "By enabling turret sharing for friends/members, then those players will not be targeted by your turrets/traps."
                    },
                    {
                        "DescriptionAutoAuth",
                        "By enabling automatic authorization for cups and turrets, you can skip the selfauth steps after placement by this automation."
                    },
                }, this);
        }

        private void Init()
        {
            StateDisabled();
            ClansDisabled();
            LoadVariables();
            LoadDefaultMessages();
            cmd.AddChatCommand(shareCommand, this, "ShareCommand");
            cmd.AddConsoleCommand(shareCommand, this, "cShareCommand");
            permission.RegisterPermission(permGetClanShares, this);
            permission.RegisterPermission(permGetFriendShares, this);
            permission.RegisterPermission(permAutoAuth, this);
            List<string> filter = RustExtension.Filter.ToList();
            filter.Add("Calling hook CanBeTargeted resulted in a conflict");
            RustExtension.Filter = filter.ToArray();
            LdPlyrDt();
        }

        private void LdPlyrDt()
        {
            StoredData protoStorage = new StoredData();
            if (ProtoStorage.Exists(new string[] { Title }))
                protoStorage = ProtoStorage.Load<StoredData>(new string[] { Title }) ?? new StoredData();

            StoredData jsonStorage = new StoredData();
            if (Interface.GetMod().DataFileSystem.ExistsDatafile(Title))
                jsonStorage = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title);

            bool lastwasProto = protoStorage.lastStorage == "proto" && protoStorage.saveStamp > jsonStorage.saveStamp;
            if (useProtostorageUserdata)
            {
                if (lastwasProto)
                {
                    playerPrefs = ProtoStorage.Load<StoredData>(new string[] { Title }) ?? new StoredData();
                    return;
                }
                else
                {
                    if (Interface.GetMod().DataFileSystem.ExistsDatafile(Title))
                        playerPrefs = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title);
                }
            }
            else
            {
                if (!lastwasProto)
                {
                    playerPrefs = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Title);
                    return;
                }
                else
                {
                    if (ProtoStorage.Exists(new string[] { Title }))
                        playerPrefs = ProtoStorage.Load<StoredData>(new string[] { Title }) ?? new StoredData();
                }
            }
        }

        private void SaveData()
        {
            if (pluginDisabled)
                return;

            playerPrefs.saveStamp = UnixTimeStampUTC();
            playerPrefs.lastStorage = useProtostorageUserdata ? "proto" : "json";

            if (useProtostorageUserdata)
                ProtoStorage.Save<StoredData>(playerPrefs, new string[] { Title });
            else Interface.Oxide.DataFileSystem.WriteObject(Title, playerPrefs);
        }

        private void Loaded()
        {
            Instance = this;
        }

        private void OnServerInitialized()
        {
            if (Initialized)
                return;

            foreach (string pseudoPerm in pseudoAdminPerms.ConvertAll(obj => Convert.ToString(obj)).ToList())
            {
                if (permission.PermissionExists(pseudoPerm))
                    pseudoPerms.Add(pseudoPerm.ToLower());
            }
            Initialize();
        }

        private void Initialize()
        {
            if (Clans && useClans)
            {
                clansEnabled = true;
                Puts("Plugin 'Clans' found - Clan support activated");
                ClansEnabled();
            }

            if (!Clans && useClans) PrintWarning("Plugin 'Clans' not found - Clan support not active");
            if (useFriendsApi)
                if (Friends && useFriendsApi && !friendsEnabled)
                {
                    friendsEnabled = true;
                    friendsAPIEnabled = true;
                    Puts("Plugin Friends found - Friends support activated");
                }

            if (useFriendsApi && !friendsEnabled) PrintWarning("No Friend Plugin found - Friend support not active");
            if (!clansEnabled && !friendsEnabled)
            {
                PrintWarning("No supported requirements found - Plugin unload!");
                Interface.Oxide.UnloadPlugin(Title);
                return;
            }

            adminAccessEnabled = new Hash<ulong, bool>();
            adminCupboards = new Hash<ulong, List<uint>>();
            adminTurrets = new Hash<ulong, List<uint>>();
            playerCupboards = new Dictionary<string, List<uint>>();
            playerTurrets = new Dictionary<string, List<uint>>();

            foreach (BasePlayer player in BasePlayer.activePlayerList.ToList())
                SetPlayer(player);

            foreach (BasePlayer player in BasePlayer.sleepingPlayerList.ToList())
                SetPlayer(player);

            List<BaseEntity> plyrObjcts = BaseNetworkable.serverEntities.Where(p => (p is BuildingPrivlidge || p is AutoTurret || p is GunTrap || p is FlameTurret || p is BaseLock)).Cast<BaseEntity>().ToList();
            foreach (BaseEntity plyrObjct in plyrObjcts)
            {
                if (plyrObjct is CodeLock && plyrObjct.GetParentEntity() && !playerPrefs.PlayerInfo.ContainsKey(plyrObjct.GetParentEntity().OwnerID))
                {
                    AddPlayerData(plyrObjct.GetParentEntity().OwnerID);
                    continue;
                }

                if (plyrObjct is CodeLock || plyrObjct.OwnerID == 0uL) continue;
                string owner = plyrObjct.OwnerID.ToString();
                if (plyrObjct is BuildingPrivlidge)
                {
                    List<uint> player;
                    if (!playerCupboards.TryGetValue(owner, out player)) playerCupboards.Add(owner, new List<uint>());
                    playerCupboards[owner].Add(plyrObjct.net.ID);
                    if (!playerPrefs.PlayerInfo.ContainsKey(plyrObjct.OwnerID)) AddPlayerData(plyrObjct.OwnerID);
                }
                else if (plyrObjct is AutoTurret)
                {
                    List<uint> player;
                    if (!playerTurrets.TryGetValue(owner, out player)) playerTurrets.Add(owner, new List<uint>());
                    playerTurrets[owner].Add(plyrObjct.net.ID);
                    if (!playerPrefs.PlayerInfo.ContainsKey(plyrObjct.OwnerID)) AddPlayerData(plyrObjct.OwnerID);
                }
                else if (!playerPrefs.PlayerInfo.ContainsKey(plyrObjct.OwnerID))
                {
                    AddPlayerData(plyrObjct.OwnerID);
                }
            }

            if (enableQuarrySharing && enableQuarrySwitchCheck)
            {
                foreach (MiningQuarry quarry in BaseNetworkable.serverEntities.Where(p => p is MiningQuarry && (p as BaseEntity).OwnerID != 0uL).Cast<MiningQuarry>().ToList())
                    quarry.engineSwitchPrefab.instance.gameObject.transform.GetOrAddComponent<QuarryHandler>();
            }
            StateEnabled();
            Initialized = true;
        }

        private bool GetAdmin(BasePlayer player)
        {
            return player.IsAdmin || IsPsdAdmn(player.UserIDString);
        }

        private bool AccessOn(ulong id)
        {
            bool obj;
            return adminAccessEnabled.TryGetValue(id, out obj) && (bool)obj == true;
        }

        private PlayerInfo SetPlayer(BasePlayer player)
        {
            if (player == null || player.userID < 76561197960265729uL)
                return null;

            if (GetAdmin(player))
            {
                adminAccessEnabled[player.userID] = enableAdminmodeAtLogin;
                adminCupboards[player.userID] = new List<uint>();
                adminTurrets[player.userID] = new List<uint>();
            }

            List<uint> entries;

            if (!playerCupboards.TryGetValue(player.UserIDString, out entries))
                playerCupboards.Add(player.UserIDString, new List<uint>());

            if (!playerTurrets.TryGetValue(player.UserIDString, out entries))
                playerTurrets.Add(player.UserIDString, new List<uint>());

            PlayerInfo p;
            if (!playerPrefs.PlayerInfo.TryGetValue(player.userID, out p))
            {
                PlayerInfo info = new PlayerInfo();
                info.CS = CupShare;
                info.DS = DoorShare;
                info.TS = TurretShare;
                info.BS = BoxShare;
                info.LS = LockerShare;
                info.QS = QuarryShare;
                info.AA = AutoAuth;
                info.CCS = ClanCupShare;
                info.CDS = ClanDoorShare;
                info.CBS = ClanBoxShare;
                info.CLS = ClanLockerShare;
                info.CQS = ClanQuarryShare;
                info.CTS = ClanTurretShare;
                info.HasClanShare = !usePermGetClanShares || usePermGetClanShares && permission.UserHasPermission(player.UserIDString, permGetClanShares);
                info.HasFriendShare = !usePermGetFriendShares || usePermGetFriendShares && permission.UserHasPermission(player.UserIDString, permGetFriendShares);
                info.CanAutoAuth = !usePermAutoAuth || usePermAutoAuth && permission.UserHasPermission(player.UserIDString, permAutoAuth);
                playerPrefs.PlayerInfo.Add(player.userID, info);
                return info;
            }
            else
            {
                p.HasClanShare = !usePermGetClanShares || usePermGetClanShares && permission.UserHasPermission(player.UserIDString, permGetClanShares);
                p.HasFriendShare = !usePermGetFriendShares || usePermGetFriendShares && permission.UserHasPermission(player.UserIDString, permGetFriendShares);
                p.CanAutoAuth = !usePermAutoAuth || usePermAutoAuth && permission.UserHasPermission(player.UserIDString, permAutoAuth);
            }

            return p;
        }

        private bool IsPsdAdmn(string id)
        {
            foreach (string perm in pseudoPerms)
                if (permission.UserHasPermission(id, perm))
                    return true;
            return false;
        }

        private void OnUserPermissionGranted(string id, string perm)
        {
            if (pseudoPerms.Contains(perm.ToLower()))
            {
                adminAccessEnabled[Convert.ToUInt64(id)] = enableAdminmodeAtLogin;
                adminCupboards[Convert.ToUInt64(id)] = new List<uint>();
                adminTurrets[Convert.ToUInt64(id)] = new List<uint>();
            }
        }

        private PlayerInfo AddPlayerData(ulong userID)
        {
            PlayerInfo info = new PlayerInfo();
            info.CS = CupShare;
            info.DS = DoorShare;
            info.TS = TurretShare;
            info.BS = BoxShare;
            info.LS = LockerShare;
            info.QS = QuarryShare;
            info.AA = AutoAuth;
            info.CCS = ClanCupShare;
            info.CDS = ClanDoorShare;
            info.CBS = ClanBoxShare;
            info.CLS = ClanLockerShare;
            info.CQS = ClanQuarryShare;
            info.CTS = ClanTurretShare;
            info.HasClanShare = !usePermGetClanShares || usePermGetClanShares &&
                            permission.UserHasPermission(userID.ToString(), permGetClanShares);
            info.HasFriendShare = !usePermGetFriendShares || usePermGetFriendShares &&
                             permission.UserHasPermission(userID.ToString(), permGetFriendShares);
            info.CanAutoAuth = !usePermAutoAuth ||
                           usePermAutoAuth && permission.UserHasPermission(userID.ToString(), permAutoAuth);
            playerPrefs.PlayerInfo.Add(userID, info);
            return info;
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!GetAdmin(player)) return;
            adminAccessEnabled.Remove(player.userID);
            List<uint> cups;
            if (adminCupboards.TryGetValue(player.userID, out cups))
            {
                foreach (uint cup in cups)
                {
                    BuildingPrivlidge ent = (BuildingPrivlidge)BaseNetworkable.serverEntities.Find(cup);
                    if (ent)
                    {
                        ent.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == player.userID);
                        ent.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    }
                }

                adminCupboards.Remove(player.userID);
            }

            List<uint> turrets;
            if (adminTurrets.TryGetValue(player.userID, out turrets))
            {
                foreach (uint turret in turrets)
                {
                    AutoTurret ent = (AutoTurret)BaseNetworkable.serverEntities.Find(turret);
                    if (ent)
                    {
                        ent.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == player.userID);
                        ent.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    }
                }

                adminTurrets.Remove(player.userID);
            }
        }

        private void Unload()
        {
            SaveData();
            bool isOn = !Interface.Oxide.IsShuttingDown;
            foreach (KeyValuePair<ulong, List<uint>> admin in adminCupboards.ToList())
                foreach (uint cup in adminCupboards[admin.Key].ToList())
                {
                    BuildingPrivlidge p = (BuildingPrivlidge)BaseNetworkable.serverEntities.Find(cup);
                    if (p)
                    {
                        p.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == admin.Key);
                        if (isOn) p.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    }
                }

            foreach (KeyValuePair<ulong, List<uint>> admin in adminTurrets.ToList())
                foreach (uint turret in adminTurrets[admin.Key].ToList())
                {
                    AutoTurret t = (AutoTurret)BaseNetworkable.serverEntities.Find(turret);
                    if (t)
                    {
                        t.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == admin.Key);
                        if (isOn) t.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    }
                }

            if (!isOn) return;
            List<BuildingPrivilegeHandler> bphandlers = UnityEngine.Object.FindObjectsOfType<BuildingPrivilegeHandler>().ToList();
            if (bphandlers.Count > 0)
                foreach (BuildingPrivilegeHandler handler in bphandlers)
                    UnityEngine.Object.Destroy(handler);
            List<QuarryHandler> qhandlers = UnityEngine.Object.FindObjectsOfType<QuarryHandler>().ToList();
            if (qhandlers.Count > 0)
                foreach (QuarryHandler handler in qhandlers)
                    UnityEngine.Object.Destroy(handler);
            List<string> filter = RustExtension.Filter.ToList();
            filter.Remove("Calling hook CanBeTargeted resulted in a conflict");
            RustExtension.Filter = filter.ToArray();
        }

        private void OnServerSave()
        {
            SaveData();
        }

        private void OnPluginUnloaded(Plugin name)
        {
            if (!Initialized || name.Name == Title) return;
            if (name.Name == "Clans" && useClans && clansEnabled)
            {
                clansEnabled = false;
                Puts("Clans support disabled");
                ClansDisabled();
            }

            if (name.Name == "Friends" && useFriendsApi && friendsAPIEnabled)
            {
                friendsAPIEnabled = false;
                friendsEnabled = false;
                Puts("Friends support disabled");
            }

            if (!clansEnabled && !friendsEnabled && !pluginDisabled)
            {
                pluginDisabled = true;
                PrintWarning("Sharing functions disabled - Plugin paused!");
                StateDisabled();
            }
        }

        private void OnPluginLoaded(Plugin name)
        {
            if (!Initialized || name.Name == Title) return;
            if (name.Name == "Clans" && useClans && !clansEnabled)
            {
                clansEnabled = true;
                Puts("Clans support enabled");
                ClansEnabled();
            }

            if (name.Name == "Friends" && useFriendsApi && !friendsAPIEnabled)
            {
                friendsAPIEnabled = true;
                friendsEnabled = true;
                Puts("Friends support enabled");
            }

            if ((clansEnabled || friendsEnabled) && pluginDisabled)
            {
                pluginDisabled = false;
                Puts("Sharing functions re-enabled - Plugin unpaused!");
                StateEnabled();
            }
        }

        private object CanBuild(Planner plan, Construction prefab, Construction.Target target)
        {
            if (plan == null || prefab == null ||
                allowLaddersIntoBlocked && prefab.hierachyName.Contains("ladder.wooden.wall") ||
                allowTwigIntoBlocked && prefab.hierachyName.Contains("floor")) return null;
            BasePlayer player = plan.GetOwnerPlayer();
            Construction.Placement placement = new Construction.Placement();
            if (target.socket != null)
            {
                List<Socket_Base> list = Pool.GetList<Socket_Base>();
                prefab.FindMaleSockets(target, list);
                foreach (Socket_Base current in list)
                    if (!(target.entity != null) || !(target.socket != null) ||
                        !target.entity.IsOccupied(target.socket))
                    {
                        placement = current.DoPlacement(target);
                        if (placement != null) break;
                    }

                Pool.FreeList<Socket_Base>(ref list);
                if (placement == null) return null;
            }
            else
            {
                placement.position = target.position;
                placement.rotation = Quaternion.Euler(target.rotation);
                if (placement.rotation == Quaternion.identity)
                    placement.rotation = Quaternion.Euler(0, plan.GetOwnerPlayer().transform.rotation.y, 0);
            }

            RaycastHit hit = default(RaycastHit);
            if ((!allowIcebergBuilding || !allowIcesheetBuilding || !allowIcelakeBuilding) &&
                Physics.Raycast(placement.position, Vector3.down, out hit, placement.position.y, 65536))
            {
                if (!allowIcebergBuilding && hit.collider.name.ToLower().StartsWith("iceberg"))
                {
                    if (notifyPlayersBeingBlocked)
                        PrintToChat(player,
                            $"<color={colorTextMsg}>" +
                            string.Format(lang.GetMessage("BlockBuildOnIceBergs", this, player.UserIDString)) +
                            $"</color>");
                    return false;
                }

                if (!allowIcelakeBuilding && hit.collider.name.ToLower().StartsWith("ice_lake"))
                {
                    if (notifyPlayersBeingBlocked)
                        PrintToChat(player,
                            $"<color={colorTextMsg}>" +
                            string.Format(lang.GetMessage("BlockBuildOnIceLakes", this, player.UserIDString)) +
                            $"</color>");
                    return false;
                }

                if (!allowIcesheetBuilding && hit.collider.name.ToLower().StartsWith("ice_sheet"))
                {
                    if (notifyPlayersBeingBlocked)
                        PrintToChat(player,
                            $"<color={colorTextMsg}>" +
                            string.Format(lang.GetMessage("BlockBuildOnIceSheets", this, player.UserIDString)) +
                            $"</color>");
                    return false;
                }
            }

            if (!allowLaddersIntoBlocked || !allowTwigIntoBlocked)
            {
                BuildingPrivlidge cup = player.GetBuildingPrivilege(new OBB(placement.position, placement.rotation, prefab.bounds));
                if (cup == null)
                    return null;

                if (!cup.IsAuthed(player))
                {
                    if (notifyPlayersBeingBlocked)
                        PrintToChat(player, $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("BlockBuildIntoBlocked", this, player.UserIDString)) + $"</color>");
                    return false;
                }
            }

            return null;
        }

        private void OnFriendAdded(string playerId, string friendId, bool isCup = true, bool isTurret = true)
        {
            if (playerId == null || playerId == string.Empty || friendId == null || friendId == string.Empty)
                return;

            ServerMgr.Instance.StartCoroutine(CoFrndAddd(playerId, friendId, isCup, isTurret));
        }

        private IEnumerator CoFrndAddd(string playerId, string friendId, bool isCup, bool isTurret)
        {
            ulong friendID = Convert.ToUInt64(friendId);

            IPlayer friend = covalence.Players.FindPlayerById(friendId);
            if (friend == null)
                yield break;

            List<uint> playercups;
            if (isCup && playerCupboards.TryGetValue(playerId, out playercups))
            {
                foreach (uint cup in playercups.ToList())
                {
                    BuildingPrivlidge priv = BaseNetworkable.serverEntities.Find(cup) as BuildingPrivlidge;
                    if (priv)
                    {
                        if (priv.AnyAuthed() &&
                            priv.authorizedPlayers.Any((PlayerNameID x) => x.userid == priv.OwnerID) &&
                            !priv.authorizedPlayers.Any((PlayerNameID x) => x.userid == friendID))
                        {
                            priv.authorizedPlayers.Add(new PlayerNameID { userid = friendID, username = friend.Name });
                            priv.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                        }
                    }
                    else
                    {
                        playerCupboards[playerId].Remove(cup);
                    }

                    yield return wait;
                }
            }

            List<uint> playerturr;
            if (isTurret && playerTurrets.TryGetValue(playerId, out playerturr))
            {
                foreach (uint turr in playerturr.ToList())
                {
                    AutoTurret turret = BaseNetworkable.serverEntities.Find(turr) as AutoTurret;
                    if (turret)
                    {
                        if (turret.AnyAuthed() &&
                            turret.authorizedPlayers.Any((PlayerNameID x) => x.userid == turret.OwnerID) &&
                            !turret.authorizedPlayers.Any((PlayerNameID x) => x.userid == friendID))
                        {
                            turret.authorizedPlayers.Add(new PlayerNameID { userid = friendID, username = friend.Name });
                            turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                        }
                    }
                    else
                    {
                        playerTurrets[playerId].Remove(turr);
                    }

                    yield return wait;
                }
            }
            yield return null;
        }

        private void OnFriendRemoved(string playerId, string friendId, bool isCup = true, bool isTurret = true)
        {
            if (playerId == null || playerId == string.Empty || friendId == null || friendId == string.Empty)
                return;

            ServerMgr.Instance.StartCoroutine(CoFrndRmvd(playerId, friendId, isCup, isTurret));
        }

        private IEnumerator CoFrndRmvd(string playerId, string friendId, bool isCup, bool isTurret)
        {
            ulong playerID = Convert.ToUInt64(playerId);
            ulong friendID = Convert.ToUInt64(friendId);
            bool areSameClan = false;

            if (clansEnabled && SameClan(playerID, friendID))
                areSameClan = true;

            IPlayer friend = covalence.Players.FindPlayerById(friendId);
            if (friend == null) yield break;
            List<uint> playercups;
            if (isCup && playerCupboards.TryGetValue(playerId, out playercups))
            {
                foreach (uint cup in playercups)
                {
                    if (areSameClan && playerPrefs.PlayerInfo[playerID].CCS)
                        continue;

                    BuildingPrivlidge priv = BaseNetworkable.serverEntities.Find(cup) as BuildingPrivlidge;
                    if (priv)
                    {
                        if (priv.AnyAuthed() && priv.authorizedPlayers.Any((PlayerNameID x) => x.userid == friendID))
                        {
                            priv.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == friendID);
                            priv.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                        }
                    }
                    else
                    {
                        playerCupboards[playerId].Remove(cup);
                    }

                    yield return wait;
                }
            }

            List<uint> playerturr;
            if (isTurret && playerTurrets.TryGetValue(playerId, out playerturr))
            {
                foreach (uint turr in playerturr)
                {
                    if (areSameClan && (playerPrefs.PlayerInfo[playerID].CTS || clanTurretShareOverride))
                        continue;

                    AutoTurret turret = BaseNetworkable.serverEntities.Find(turr) as AutoTurret;
                    if (turret)
                    {
                        if (turret.AnyAuthed() && turret.authorizedPlayers.Any((PlayerNameID x) => x.userid == friendID))
                        {
                            turret.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == friendID);
                            turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                        }
                    }
                    else
                    {
                        playerTurrets[playerId].Remove(turr);
                    }

                    yield return wait;
                }
            }
        }

        private WaitForEndOfFrame wait = new WaitForEndOfFrame();

        private IEnumerator ClnMmbrChng(string memberId, List<ulong> clanMembers, bool wasAdded, bool isCup = true, bool isTurret = true)
        {
            if (isCup)
            {
                List<uint> playercups;
                foreach (ulong clanMember in clanMembers)
                {
                    if (playerCupboards.TryGetValue(clanMember.ToString(), out playercups))
                    {
                        foreach (uint cup in playercups.ToList())
                        {
                            BuildingPrivlidge priv = BaseNetworkable.serverEntities.Find(cup) as BuildingPrivlidge;
                            if (priv)
                            {
                                foreach (ulong clanMember2 in clanMembers)
                                {
                                    if (priv.OwnerID != clanMember2)
                                    {
                                        if (wasAdded)
                                        {
                                            if (!playerPrefs.PlayerInfo[clanMember2].CCS || !playerPrefs.PlayerInfo[clanMember2].HasClanShare)
                                                continue;

                                            if (priv.AnyAuthed() && priv.authorizedPlayers.Any((PlayerNameID x) => x.userid == priv.OwnerID) && !priv.authorizedPlayers.Any((PlayerNameID x) => x.userid == clanMember2))
                                            {
                                                IPlayer iplayer = covalence.Players.FindPlayerById(clanMember2.ToString());
                                                if (iplayer != null)
                                                    priv.authorizedPlayers.Add(new PlayerNameID { userid = clanMember2, username = iplayer.Name });
                                            }
                                        }
                                        else
                                        {
                                            if (friendsEnabled && playerPrefs.PlayerInfo[priv.OwnerID].CS && HasFriend(priv.OwnerID, clanMember2))
                                                continue;
                                            if (priv.AnyAuthed() && priv.authorizedPlayers.Any((PlayerNameID x) => x.userid == clanMember2))
                                            {
                                                IPlayer iplayer = covalence.Players.FindPlayerById(clanMember2.ToString());
                                                if (iplayer != null)
                                                    priv.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == clanMember2);
                                            }
                                        }

                                        yield return wait;
                                    }
                                }

                                priv.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                                yield return wait;
                            }
                            else
                            {
                                playerCupboards[clanMember.ToString()].Remove(cup);
                            }

                            yield return wait;
                        }
                    }
                }
            }

            if (isTurret)
            {
                List<uint> playerturr;
                foreach (ulong clanMember in clanMembers)
                {
                    if (playerTurrets.TryGetValue(clanMember.ToString(), out playerturr))
                    {
                        foreach (uint turr in playerturr.ToList())
                        {
                            AutoTurret turret = BaseNetworkable.serverEntities.Find(turr) as AutoTurret;
                            if (turret)
                            {
                                foreach (ulong clanMember2 in clanMembers)
                                {
                                    if (turret.OwnerID != clanMember2)
                                    {
                                        if (wasAdded)
                                        {
                                            if (!playerPrefs.PlayerInfo[clanMember2].CTS || !playerPrefs.PlayerInfo[clanMember2].HasClanShare)
                                                continue;

                                            if (turret.AnyAuthed() && turret.authorizedPlayers.Any((PlayerNameID x) => x.userid == turret.OwnerID) && !turret.authorizedPlayers.Any((PlayerNameID x) => x.userid == clanMember2))
                                            {
                                                IPlayer iplayer = covalence.Players.FindPlayerById(clanMember2.ToString());
                                                if (iplayer != null)
                                                    turret.authorizedPlayers.Add(new PlayerNameID { userid = clanMember2, username = iplayer.Name });
                                            }
                                        }
                                        else
                                        {
                                            if (friendsEnabled && playerPrefs.PlayerInfo[turret.OwnerID].TS && HasFriend(turret.OwnerID, clanMember2))
                                                continue;

                                            if (turret.AnyAuthed() && turret.authorizedPlayers.Any((PlayerNameID x) => x.userid == clanMember2))
                                            {
                                                IPlayer iplayer = covalence.Players.FindPlayerById(clanMember2.ToString());
                                                if (iplayer != null)
                                                    turret.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == clanMember2);
                                            }
                                        }

                                        yield return wait;
                                    }
                                }

                                turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                                yield return wait;
                            }
                            else
                            {
                                playerTurrets[clanMember.ToString()].Remove(turr);
                            }

                            yield return wait;
                        }
                    }
                }
            }

            yield return null;
        }

        private void OnClanMemberJoined(string playerId, List<string> clanMembersCall)
        {
            List<string> clanMembers = new List<string>(clanMembersCall);

            if (!clanMembers.Contains(playerId))
                clanMembers.Add(playerId);

            List<ulong> members = clanMembers.ConvertAll(obj => Convert.ToUInt64(obj));
            PlayerInfo pInfo;
            foreach (ulong member in members)
            {
                if (!playerPrefs.PlayerInfo.TryGetValue(member, out pInfo))
                    AddPlayerData(member);
            }
            ServerMgr.Instance.StartCoroutine(ClnMmbrChng(playerId, members, true));
        }

        private void OnClanMemberGone(string playerId, List<string> clanMembersCall)
        {
            List<string> clanMembers = new List<string>(clanMembersCall);

            if (!clanMembers.Contains(playerId))
                clanMembers.Add(playerId);

            List<ulong> members = clanMembers.ConvertAll(obj => Convert.ToUInt64(obj));
            PlayerInfo pInfo;
            foreach (ulong member in members)
            {
                if (!playerPrefs.PlayerInfo.TryGetValue(member, out pInfo))
                    AddPlayerData(member);
            }
            ServerMgr.Instance.StartCoroutine(ClnMmbrChng(playerId, members, false));
        }

        private void OnClanDisbanded(List<string> clanMembersCall)
        {
            List<string> clanMembers = new List<string>(clanMembersCall);
            List<ulong> members = clanMembers.ConvertAll(obj => Convert.ToUInt64(obj));
            PlayerInfo pInfo;
            foreach (ulong member in members)
            {
                if (!playerPrefs.PlayerInfo.TryGetValue(member, out pInfo))
                    AddPlayerData(member);
            }

            foreach (string clanMember in clanMembers.ToList())
                ServerMgr.Instance.StartCoroutine(ClnMmbrChng(clanMember, members.ToList(), false));
        }

        private void CpSttsFrnd(BasePlayer player, bool IsOn)
        {
            object obj = Friends.Call("GetFriendsS", player.UserIDString);

            if (obj == null || obj as string[] == null || (obj as string[]).Length == 0)
                return;

            List<string> friendIds = (obj as string[]).ToList();
            if (friendIds == null || friendIds.Count == 0) return;
            if (IsOn)
            {
                foreach (string friendId in friendIds)
                    OnFriendAdded(player.UserIDString, friendId, true, false);
            }
            else
            {
                foreach (string friendId in friendIds)
                    OnFriendRemoved(player.UserIDString, friendId, true, false);
            }
        }

        private void TrrtSttsFrnd(BasePlayer player, bool IsOn)
        {
            object obj = Friends.Call("GetFriendsS", player.UserIDString);
            if (obj == null || obj as string[] == null || (obj as string[]).Length == 0) return;
            List<string> friendIds = (obj as string[]).ToList();
            if (friendIds == null || friendIds.Count == 0) return;
            if (IsOn)
                foreach (string friendId in friendIds)
                    OnFriendAdded(player.UserIDString, friendId, false, true);
            else
                foreach (string friendId in friendIds)
                    OnFriendRemoved(player.UserIDString, friendId, false, true);
        }

        private void CpSttsCln(BasePlayer player, bool IsOn)
        {
            string tag = Clans?.Call("GetClanOf", player.userID) as string;
            if (tag == null || tag == string.Empty) return;
            JObject Clan = Clans?.Call("GetClan", tag) as JObject;
            if (Clan == null) return;
            List<ulong> members = Clan["members"].ToObject<List<string>>().ConvertAll(obj => Convert.ToUInt64(obj));
            PlayerInfo pInfo;
            foreach (ulong member in members)
                if (!playerPrefs.PlayerInfo.TryGetValue(member, out pInfo))
                    AddPlayerData(member);
            if (IsOn) ServerMgr.Instance.StartCoroutine(ClnMmbrChng(player.UserIDString, members, true, true, false));
            else ServerMgr.Instance.StartCoroutine(ClnMmbrChng(player.UserIDString, members, false, true, false));
        }

        private void TrrtSttsCln(BasePlayer player, bool IsOn)
        {
            string tag = Clans?.Call("GetClanOf", player.userID) as string;
            if (tag == null || tag == string.Empty) return;
            JObject Clan = Clans?.Call("GetClan", tag) as JObject;
            if (Clan == null) return;
            List<ulong> members = Clan["members"].ToObject<List<string>>().ConvertAll(obj => Convert.ToUInt64(obj));
            PlayerInfo pInfo;
            foreach (ulong member in members)
            {
                pInfo = null;
                if (!playerPrefs.PlayerInfo.TryGetValue(member, out pInfo)) AddPlayerData(member);
            }

            if (IsOn) ServerMgr.Instance.StartCoroutine(ClnMmbrChng(player.UserIDString, members, true, false, true));
            else ServerMgr.Instance.StartCoroutine(ClnMmbrChng(player.UserIDString, members, false, false, true));
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || (entity as BaseEntity)?.OwnerID == 0uL) return;
            if (entity is BuildingPrivlidge)
            {
                List<uint> player = null;
                string owner = (entity as BaseEntity).OwnerID.ToString();
                if (!playerCupboards.TryGetValue(owner, out player)) return;
                playerCupboards[owner].Remove(entity.net.ID);
            }
            else if (entity is AutoTurret)
            {
                List<uint> player = null;
                string owner = (entity as BaseEntity).OwnerID.ToString();
                if (!playerTurrets.TryGetValue(owner, out player)) return;
                playerTurrets[owner].Remove(entity.net.ID);
            }
        }

        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            if (entity == null || !turret.IsValid() || turret is NPCAutoTurret || turret.OwnerID == 0uL)
                return null;

            BasePlayer player = entity as BasePlayer;
            if (player != null && player.IsConnected && !player.IsDead())
            {
                ulong userID = player.userID;
                string displayName = player.displayName;
                try
                {
                    if (AccessOn(userID))
                    {
                        if (adminTurrets[userID] == null)
                            adminTurrets[userID] = new List<uint>();

                        adminTurrets[userID].Add(turret.net.ID);

                        turret.authorizedPlayers.Add(new PlayerNameID { userid = userID, username = displayName });
                        turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                        return true;
                    }
                }
                catch
                {
                }

                if (!turret.AnyAuthed() || !turret.authorizedPlayers.Any((PlayerNameID x) => x.userid == turret.OwnerID))
                    return null;

                PlayerInfo playerInfo;
                if (playerPrefs.PlayerInfo.TryGetValue(userID, out playerInfo))
                {
                    if (playerInfo.BPH != null && playerInfo.BPH.SameTurret(turret.net.ID))
                    {
                        object result = playerInfo.BPH.LastResult();
                        return result;
                    }

                    PlayerInfo otherInfo;
                    if (playerPrefs.PlayerInfo.TryGetValue(turret.OwnerID, out otherInfo))
                    {
                        if (clansEnabled && (clanTurretShareOverride || otherInfo.CTS) && SameClan(turret.OwnerID, userID) && (usePermGetClanShares && permission.UserHasPermission(userID.ToString(), permGetClanShares) || !usePermGetClanShares || clanTurretShareOverride))
                        {
                            turret.authorizedPlayers.Add(new PlayerNameID { userid = userID, username = displayName });
                            turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            return true;
                        }

                        if (friendsEnabled && otherInfo.TS && HasFriend(turret.OwnerID, userID) && (usePermGetFriendShares && permission.UserHasPermission(userID.ToString(), permGetFriendShares) || !usePermGetFriendShares))
                        {
                            turret.authorizedPlayers.Add(new PlayerNameID { userid = userID, username = displayName });
                            turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                            return true;
                        }
                        if (playerInfo.BPH != null)
                            playerInfo.BPH.SetTurret(turret.net.ID, null);
                    }
                }
            }

            return null;
        }

        private object CanBeTargeted(BasePlayer player, FlameTurret turret)
        {
            if (!includeFlameTurrets || player == null || !turret.IsValid() || turret.OwnerID == 0uL)
                return null;

            if (player.userID == turret.OwnerID || AccessOn(player.userID))
                return false;

            return CanBeTurretTarget(player.userID, turret.net.ID, turret.OwnerID);
        }

        private object CanBeTargeted(BasePlayer player, GunTrap turret)
        {
            if (!includeGunTraps || player == null || !turret.IsValid() || turret.OwnerID == 0uL)
                return null;

            if (player.userID == turret.OwnerID || AccessOn(player.userID))
                return false;

            return CanBeTurretTarget(player.userID, turret.net.ID, turret.OwnerID);
        }

        private object CanBeTurretTarget(ulong userID, uint netID, ulong OwnerID)
        {
            PlayerInfo oInfo;
            if (playerPrefs.PlayerInfo.TryGetValue(OwnerID, out oInfo))
            {
                PlayerInfo pInfo;
                if (playerPrefs.PlayerInfo.TryGetValue(userID, out pInfo))
                {
                    if (pInfo.BPH != null && pInfo.BPH.SameTurret(netID))
                    {
                        object result = pInfo.BPH.LastResult();
                        return result;
                    }

                    if (clansEnabled && (clanTurretShareOverride || oInfo.CTS) && SameClan(OwnerID, userID) && pInfo.HasClanShare && (pInfo.CTS || clanTurretShareOverride) || friendsEnabled && oInfo.TS && HasFriend(OwnerID, userID) && pInfo.HasFriendShare)
                    {
                        pInfo.BPH.SetTurret(netID, false);
                        return false;
                    }

                    if (pInfo.BPH != null)
                        pInfo.BPH.SetTurret(netID, null);
                }
            }

            return null;
        }

        private object OnSamSiteTarget(SamSite sam, BaseCombatEntity target)
        {
            if (!includeSamSites || sam.OwnerID == 0)
                return null;

            BaseVehicle baseVehicle = target as BaseVehicle;
            if (baseVehicle != null)
            {
                if (!samSiteShootEmptyVehicles && !HasOccupants(baseVehicle))
                {
                    sam.CancelInvoke(sam.WeaponTick);
                    return false;
                }

                for (int i = 0; i < baseVehicle.mountPoints?.Length; i++)
                {
                    BasePlayer player = baseVehicle.mountPoints[i].mountable?.GetMounted();
                    if (player != null)
                    {
                        if (player.userID == sam.OwnerID || AccessOn(player.userID))
                        {
                            sam.CancelInvoke(sam.WeaponTick);
                            return false;
                        }

                        if (SamVerificationInternal(sam.OwnerID, player.userID, () => sam.CancelInvoke(sam.WeaponTick)))
                        {
                            return false;
                        }
                    }
                }
                return null;
            }

            HotAirBalloon hotAirBalloon = target as HotAirBalloon;
            if (hotAirBalloon != null)
            {
                if (!samSiteShootEmptyVehicles && hotAirBalloon.children?.Count == 0)
                {
                    sam.CancelInvoke(sam.WeaponTick);
                    return false;
                }

                for (int i = 0; i < hotAirBalloon.children?.Count; i++)
                {
                    BasePlayer player = hotAirBalloon.children[i] as BasePlayer;
                    if (player != null)
                    {
                        if (player.userID == sam.OwnerID || AccessOn(player.userID))
                        {
                            sam.CancelInvoke(sam.WeaponTick);
                            return false;
                        }

                        if (SamVerificationInternal(sam.OwnerID, player.userID, () => sam.CancelInvoke(sam.WeaponTick)))
                            return false;
                    }
                }
            }
            return null;
        }

        private bool HasOccupants(BaseVehicle baseVehicle)
        {
            for (int i = 0; i < baseVehicle?.mountPoints?.Length; i++)
            {
                if (baseVehicle.mountPoints[i].mountable.IsMounted())
                    return true;
            }
            return false;
        }

        private bool SamVerificationInternal(ulong ownerId, ulong playerId, Action action)
        {
            PlayerInfo oInfo;
            if (playerPrefs.PlayerInfo.TryGetValue(ownerId, out oInfo))
            {
                PlayerInfo pInfo;
                if (playerPrefs.PlayerInfo.TryGetValue(playerId, out pInfo))
                {
                    if (clansEnabled && (clanTurretShareOverride || oInfo.CTS) && SameClan(ownerId, playerId) && pInfo.HasClanShare && (pInfo.CTS || clanTurretShareOverride) || friendsEnabled && oInfo.TS && HasFriend(ownerId, playerId) && pInfo.HasFriendShare)
                    {
                        action?.Invoke();
                        return true;
                    }
                }
            }
            return false;
        }
        private void OnEntityBuilt(Planner planner, GameObject obj)
        {
            if (planner == null) return;
            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null) return;
            BaseEntity entity = obj.ToBaseEntity();
            if (entity == null || entity.net == null || entity.OwnerID == 0uL) return;
            if (entity is MiningQuarry && enableQuarrySharing && enableQuarrySwitchCheck)
            {
                (entity as MiningQuarry).engineSwitchPrefab.instance.gameObject.transform.GetOrAddComponent<QuarryHandler>();
                return;
            }

            PlayerInfo pInfo;
            if (!playerPrefs.PlayerInfo.TryGetValue(player.userID, out pInfo)) pInfo = SetPlayer(player);
            if (!pInfo.AA || !pInfo.CanAutoAuth) return;
            if (entity is BuildingPrivlidge)
            {
                (entity as BuildingPrivlidge).authorizedPlayers.Add(new PlayerNameID
                    {userid = player.userID, username = player.displayName});
                entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                if (notifyAuthCupboard)
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("CupAuth", this, player.UserIDString)) + $"</color>");
                playerCupboards[player.UserIDString].Add(entity.net.ID);
                if (enableCupSharing)
                {
                    if (clansEnabled && pInfo.CCS) CpSttsCln(player, true);
                    if (friendsEnabled && pInfo.CS) CpSttsFrnd(player, true);
                }
            }
            else if (entity is AutoTurret)
            {
                (entity as AutoTurret).authorizedPlayers.Add(new PlayerNameID
                    {userid = player.userID, username = player.displayName});
                entity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                if (notifyAuthTurret)
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("TurretAuth", this, player.UserIDString)) + $"</color>");
                playerTurrets[player.UserIDString].Add(entity.net.ID);
                if (enableTurretSharing)
                {
                    if (clansEnabled && (pInfo.CTS || clanTurretShareOverride)) TrrtSttsCln(player, true);
                    if (friendsEnabled && pInfo.TS) TrrtSttsFrnd(player, true);
                }
            }
        }

        private object OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege == null || player == null || privilege.OwnerID == player.userID ||
                GetAdmin(player) && AccessOn(player.userID) || !privilege.AnyAuthed() ||
                !privilege.authorizedPlayers.Any((PlayerNameID x) => x.userid == privilege.OwnerID)) return null;
            if (clansEnabled && blockCupClearClanMembers && SameClan(privilege.OwnerID, player.userID))
            {
                PrintToChat(player,
                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                    string.Format(lang.GetMessage("CupAuthClearBlocked", this, player.UserIDString)) + $"</color>");
                return true;
            }

            if (clansEnabled && blockCupAccessNotSameClan && !SameClan(privilege.OwnerID, player.userID))
            {
                PrintToChat(player,
                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                    string.Format(lang.GetMessage("CupAuthClearBlocked", this, player.UserIDString)) + $"</color>");
                return true;
            }

            if (friendsEnabled && blockCupClearFriends && HasFriend(privilege.OwnerID, player.userID))
            {
                PrintToChat(player,
                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                    string.Format(lang.GetMessage("CupAuthClearBlocked", this, player.UserIDString)) + $"</color>");
                return true;
            }

            return null;
        }

        private object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege == null || player == null)
                return null;

            if (cupboardAuthMaxUsers > 0 && privilege.authorizedPlayers.Count >= cupboardAuthMaxUsers)
            {
                PrintToChat(player,
                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                    string.Format(lang.GetMessage("CupAuthMaxUsers", this, player.UserIDString), cupboardAuthMaxUsers) + $"</color>");
                return false;
            }

            if (privilege.OwnerID == player.userID || !privilege.AnyAuthed() || !privilege.authorizedPlayers.Any((PlayerNameID x) => x.userid == privilege.OwnerID))
                return null;

            if (GetAdmin(player) && AccessOn(player.userID)) return null;
            
            bool sameClan = false;
            if (clansEnabled) sameClan = SameClan(privilege.OwnerID, player.userID);
            if (clansEnabled && blockCupAccessNotSameClan && !sameClan)
            {
                PrintToChat(player,
                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                    string.Format(lang.GetMessage("CupAuthNotSameClanBlocked", this, player.UserIDString)) +
                    $"</color>");
                return false;
            }

            if (clansEnabled && sameClan)
            {
                if (!playerPrefs.PlayerInfo[privilege.OwnerID].CCS)
                {
                    IPlayer oPlayer = covalence.Players.FindPlayerById(privilege.OwnerID.ToString());
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("CupAuthDisabledOwner", this, player.UserIDString),
                            oPlayer != null ? oPlayer.Name : "The owner") + $"</color>");
                    return false;
                }

                if (!playerPrefs.PlayerInfo[player.userID].CCS)
                {
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("CupAuthDisabledSelf", this, player.UserIDString)) + $"</color>");
                    return false;
                }
            }

            if (friendsEnabled && HasFriend(privilege.OwnerID, player.userID) &&
                !playerPrefs.PlayerInfo[privilege.OwnerID].CS && !sameClan)
            {
                IPlayer oPlayer = covalence.Players.FindPlayerById(privilege.OwnerID.ToString());
                PrintToChat(player,
                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                    string.Format(lang.GetMessage("CupAuthDisabledOwner", this, player.UserIDString),
                        oPlayer != null ? oPlayer.Name : "The owner") + $"</color>");
                return false;
            }

            return null;
        }

        private void CheckCupboardAccess(BasePlayer player, BuildingPrivlidge cup)
        {
            if (GetAdmin(player) && AccessOn(player.userID))
            {
                adminCupboards[player.userID].Add(cup.net.ID);
                cup.authorizedPlayers.Add(new PlayerNameID {userid = player.userID, username = player.displayName});
                cup.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                return;
            }

            if (!cup.AnyAuthed() || !cup.authorizedPlayers.Any((PlayerNameID x) => x.userid == cup.OwnerID)) return;
            PlayerInfo pInfo;
            if (!playerPrefs.PlayerInfo.TryGetValue(cup.OwnerID, out pInfo)) pInfo = AddPlayerData(cup.OwnerID);
            if (clansEnabled && pInfo.CCS && SameClan(cup.OwnerID, player.userID) &&
                playerPrefs.PlayerInfo[player.userID].CCS && playerPrefs.PlayerInfo[player.userID].HasClanShare || friendsEnabled &&
                pInfo.CS && HasFriend(cup.OwnerID, player.userID) && playerPrefs.PlayerInfo[player.userID].HasFriendShare)
            {
                cup.authorizedPlayers.Add(new PlayerNameID {userid = player.userID, username = player.displayName});
                cup.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
        }

        private void OnLootEntity(BasePlayer looter, BaseEntity entity)
        {
            if (looter == null || entity == null || !(entity is ResourceExtractorFuelStorage)) return;
            MiningQuarry quarry = entity.GetComponentInParent<MiningQuarry>();
            if (quarry == null || quarry.OwnerID == 0uL || quarry.OwnerID == looter.userID ||
                GetAdmin(looter) && AccessOn(looter.userID)) return;
            PlayerInfo pInfo;
            if (!playerPrefs.PlayerInfo.TryGetValue(quarry.OwnerID, out pInfo)) pInfo = AddPlayerData(quarry.OwnerID);
            if (clansEnabled && pInfo.CQS && SameClan(quarry.OwnerID, looter.userID) &&
                playerPrefs.PlayerInfo[looter.userID].CQS && playerPrefs.PlayerInfo[looter.userID].HasClanShare || friendsEnabled &&
                pInfo.QS && HasFriend(quarry.OwnerID, looter.userID) &&
                playerPrefs.PlayerInfo[looter.userID].HasFriendShare) return;
            NextTick(() =>
            {
                if (looter == null) return;
                looter.ClientRPCPlayer(null, looter, "OnDied");
                PrintToChat(looter,
                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                    string.Format(lang.GetMessage("QuarryNoLootAccess", this, looter.UserIDString)) + $"</color>");
            });
        }

        internal class QuarryHandler : FacepunchBehaviour
        {
            private EngineSwitch engine;
            private MiningQuarry quarry;
            private ItemContainer fuelStorage;
            private bool isOn;

            private void Awake()
            {
                engine = GetComponent<EngineSwitch>();
                quarry = engine.GetComponentInParent<MiningQuarry>();
                isOn = engine.HasFlag(BaseEntity.Flags.On);
                fuelStorage = quarry.fuelStoragePrefab.instance.GetComponent<StorageContainer>().inventory;

                quarry.InvokeRandomized(DoUpdate, 2f, 2f, 0.5f);
            }

            private void OnDestroy()
            {
                quarry?.CancelInvoke(DoUpdate);
            }

            private bool FuelCheck()
            {
                Item item = fuelStorage?.FindItemsByItemName("lowgradefuel");

                if (item != null && item.amount >= 1)
                    return true;

                return false;
            }

            private void DoUpdate()
            {
                if (engine.HasFlag(BaseEntity.Flags.On) != isOn)
                {
                    if (!FuelCheck())
                    {
                        isOn = engine.HasFlag(BaseEntity.Flags.On);
                        return;
                    }

                    List<BasePlayer> list = Pool.GetList<BasePlayer>();
                    Vis.Entities<BasePlayer>(engine.transform.position, 4f, list, 131072, QueryTriggerInteraction.Collide);

                    foreach (BasePlayer player in list.Where(d => engine.Distance(d.eyes.position) < 3f).OrderBy(p => engine.Distance(p.eyes.position)).ToList())
                    {
                        if (Instance.CheckForCQS(quarry.OwnerID, player) || player.userID == quarry.OwnerID || Instance.GetAdmin(player) && Instance.AccessOn(player.userID))
                        {
                            isOn = !isOn;
                            return;
                        }
                    }

                    quarry.SetFlag(BaseEntity.Flags.On, isOn, false);
                    engine.SetFlag(BaseEntity.Flags.On, isOn, false);

                    quarry.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    engine.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

                    Pool.FreeList(ref list);
                }
            }
        }

        private bool CheckForCQS(ulong ownerID, BasePlayer player)
        {
            if (clansEnabled && playerPrefs.PlayerInfo[ownerID].CQS && SameClan(ownerID, player.userID) &&
                playerPrefs.PlayerInfo[player.userID].CQS && playerPrefs.PlayerInfo[player.userID].HasClanShare || friendsEnabled &&
                playerPrefs.PlayerInfo[ownerID].QS && HasFriend(ownerID, player.userID) &&
                playerPrefs.PlayerInfo[player.userID].HasFriendShare) return true;
            return false;
        }

        private object CanUseLockedEntity(BasePlayer player, KeyLock code)
        {
            if (player == null || code == null || !code.IsLocked()) return null;
            if (GetAdmin(player) && AccessOn(player.userID)) return true;
            return null;
        }

        private object CanUseLockedEntity(BasePlayer player, CodeLock code)
        {
            if (player == null || code == null || !code.IsLocked()) return null;
            object result = UseLock(player, code);
            if (result is bool)
            {
                if ((bool) result)
                    Effect.server.Run(code.effectUnlocked.resourcePath, code, 0u, Vector3.zero, Vector3.forward, null,
                        false);
                else
                    Effect.server.Run(code.effectDenied.resourcePath, code, 0u, Vector3.zero, Vector3.forward, null,
                        false);
            }

            return result;
        }

        private object CanUnlock(CodeLock code, BasePlayer player)
        {
            if (player == null || code == null) return null;
            if (InAdmMode(player))
            {
                Effect.server.Run(code.effectUnlocked.resourcePath, code, 0u, Vector3.zero, Vector3.forward, null,
                    false);
                code.SetFlag(BaseEntity.Flags.Locked, false, false);
                code.SendNetworkUpdate();
                return true;
            }

            return null;
        }

        private object CanLock(CodeLock code, BasePlayer player)
        {
            if (player == null || code == null) return null;
            if (InAdmMode(player))
            {
                Effect.server.Run(code.effectLocked.resourcePath, code, 0u, Vector3.zero, Vector3.forward, null, false);
                code.SetFlag(BaseEntity.Flags.Locked, true, false);
                code.SendNetworkUpdate();
                return true;
            }

            return null;
        }

        private bool InAdmMode(BasePlayer player)
        {
            bool enabled;
            return adminAccessEnabled.TryGetValue(player.userID, out enabled) && enabled;
        }

        private object UseLock(BasePlayer player, CodeLock code)
        {
            if (InAdmMode(player)) return true;
            BaseEntity parent = code.GetParentEntity();
            if (parent == null || parent.OwnerID == 0uL) return null;
            ulong owner = parent.OwnerID;
            if (code.whitelistPlayers.Contains(player.userID) || code.guestPlayers.Contains(player.userID)) return true;
            if (code.whitelistPlayers.Count == 0 || !code.whitelistPlayers.Contains(owner)) return null;
            bool hasClanShare = playerPrefs.PlayerInfo[player.userID].HasClanShare;
            bool hasFriendShare = playerPrefs.PlayerInfo[player.userID].HasFriendShare;
            PlayerInfo oInfo;
            if (!playerPrefs.PlayerInfo.TryGetValue(owner, out oInfo)) oInfo = AddPlayerData(owner);
            if (clansEnabled && parent is Door && enableDoorSharing && playerPrefs.PlayerInfo[owner].CDS &&
                SameClan(owner, player.userID) && playerPrefs.PlayerInfo[player.userID].CDS && hasClanShare)
            {
                return true;
            }
            else if (clansEnabled && parent is BuildingPrivlidge && enableCupSharing && playerPrefs.PlayerInfo[owner].CCS &&
                     SameClan(owner, player.userID) && playerPrefs.PlayerInfo[player.userID].CCS && hasClanShare)
            {
                return true;
            }
            else if (clansEnabled &&
                     (parent is BoxStorage && enableBoxSharing && playerPrefs.PlayerInfo[owner].CBS ||
                      parent is Locker && enableLockerSharing && playerPrefs.PlayerInfo[owner].CBS) &&
                     SameClan(owner, player.userID) && playerPrefs.PlayerInfo[player.userID].CBS && hasClanShare)
            {
                return true;
            }
            else if (friendsEnabled && parent is Door && enableDoorSharing && playerPrefs.PlayerInfo[owner].DS &&
                     HasFriend(owner, player.userID) && hasFriendShare)
            {
                return true;
            }
            else if (friendsEnabled && parent is BuildingPrivlidge && enableCupSharing && playerPrefs.PlayerInfo[owner].CS &&
                     HasFriend(owner, player.userID) && hasFriendShare)
            {
                return true;
            }
            else if (friendsEnabled &&
                     (parent is BoxStorage && enableBoxSharing && playerPrefs.PlayerInfo[owner].BS ||
                      parent is Locker && enableLockerSharing && playerPrefs.PlayerInfo[owner].LS) &&
                     HasFriend(owner, player.userID) && hasFriendShare)
            {
                return true;
            }
            else if (clansEnabled && parent is Door && enableDoorSharing && SameClan(owner, player.userID) && hasClanShare)
            {
                if (parent.IsOpen()) return true;
                if (!playerPrefs.PlayerInfo[owner].CDS)
                {
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("DoorClanNotShared", this, player.UserIDString),
                            rust.FindPlayerById(owner)?.displayName ?? string.Empty) + $"</color>");
                    return false;
                }

                if (!playerPrefs.PlayerInfo[player.userID].CDS)
                {
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("DoorClanNotSharedSelf", this, player.UserIDString)) +
                        $"</color>");
                    return false;
                }
            }
            else if (clansEnabled && parent is BoxStorage && enableBoxSharing && SameClan(owner, player.userID) &&
                     hasClanShare)
            {
                if (!playerPrefs.PlayerInfo[owner].CBS)
                {
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("BoxClanNotShared", this, player.UserIDString),
                            rust.FindPlayerById(owner)?.displayName ?? string.Empty) + $"</color>");
                    return false;
                }

                if (!playerPrefs.PlayerInfo[player.userID].CBS)
                {
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("BoxClanNotSharedSelf", this, player.UserIDString)) +
                        $"</color>");
                    return false;
                }
            }
            else if (clansEnabled && parent is Locker && enableLockerSharing && SameClan(owner, player.userID) &&
                     hasClanShare)
            {
                if (parent.IsOpen()) return true;
                if (!playerPrefs.PlayerInfo[owner].CLS)
                {
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("LockerClanNotShared", this, player.UserIDString),
                            rust.FindPlayerById(owner)?.displayName ?? string.Empty) + $"</color>");
                    return false;
                }

                if (!playerPrefs.PlayerInfo[player.userID].CLS)
                {
                    PrintToChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("LockerClanNotSharedSelf", this, player.UserIDString)) +
                        $"</color>");
                    return false;
                }
            }

            return null;
        }

        [ConsoleCommand("dynashare.resetdata")]
        private void dataReset(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            SendReply(arg, $"Resetting {playerPrefs.PlayerInfo.Count} entries in userdata");
            foreach (KeyValuePair<ulong, PlayerInfo> info in playerPrefs.PlayerInfo)
            {
                info.Value.CS = CupShare;
                info.Value.DS = DoorShare;
                info.Value.TS = TurretShare;
                info.Value.BS = BoxShare;
                info.Value.LS = LockerShare;
                info.Value.AA = AutoAuth;
                info.Value.CCS = ClanCupShare;
                info.Value.CDS = ClanDoorShare;
                info.Value.CBS = ClanBoxShare;
                info.Value.CLS = ClanLockerShare;
                info.Value.CTS = ClanTurretShare;
            }

            SendReply(arg, "Saving userdata");
            SaveData();
        }

        private void cShareCommand(ConsoleSystem.Arg arg)
        {
            if (arg != null && arg.Connection != null && arg.Connection.player != null)
            {
                usdCnslInpt.Add(arg.Connection.userid);
                if (arg.Args != null) ShareCommand((BasePlayer) arg.Connection.player, shareCommand, arg.Args);
                else ShareCommand((BasePlayer) arg.Connection.player, shareCommand, new string[] { });
            }
        }

        private void ShareCommand(BasePlayer player, string command, string[] args)
        {
            ulong userID = player.userID;
            string UserIDString = player.UserIDString;
            if (pluginDisabled)
            {
                PrintChat(player,
                    string.Format(prefixFormat, prefixColor, pluginPrefix) +
                    $"<color={colorTextMsg}>Plugin out of service!</color>");
                return;
            }

            bool hasClan = false;
            if (clansEnabled && Clans?.Call("GetClanOf", player) != null) hasClan = true;
            PlayerInfo pInfo;
            if (!playerPrefs.PlayerInfo.TryGetValue(userID, out pInfo)) pInfo = SetPlayer(player);
            if (args.Length == 0)
            {
                StringBuilder sb = new StringBuilder();
                if (!usdCnslInpt.Contains(userID))
                    sb.AppendLine($"<size=16><color={prefixColor}>{pluginPrefix}</color></size>");
                sb.AppendLine(lang.GetMessage("CommandUsage", this, UserIDString) +
                              $"<color={colorCmdUsage}>/{shareCommand} option | all</color> OR <color={colorCmdUsage}>/{shareCommand} help | h</color>");
                if (usePermGetClanShares && clansEnabled || usePermGetFriendShares && friendsEnabled)
                {
                    string hasAccessTo;
                    if (pInfo.HasClanShare && pInfo.HasFriendShare)
                        hasAccessTo = $" <color={colorON}>Clan</color> | <color={colorON}>Friends</color>";
                    else if (pInfo.HasClanShare && !pInfo.HasFriendShare) hasAccessTo = $" <color={colorON}>Clan</color>";
                    else if (!pInfo.HasClanShare && pInfo.HasFriendShare) hasAccessTo = $" <color={colorON}>Friends</color>";
                    else hasAccessTo = $" <color={colorOFF}>None</color>";
                    sb.AppendLine(lang.GetMessage("AccessRights", this, UserIDString) + hasAccessTo);
                }
                else
                {
                    sb.AppendLine(lang.GetMessage("CommandToggle", this, UserIDString));
                }

                if (friendsEnabled)
                {
                    if (enableCupSharing)
                        sb.AppendLine($"<color={colorCmdUsage}>cup | c</color> - " +
                                      lang.GetMessage("CommandFriendCup", this, UserIDString) + " " +
                                      (pInfo.CS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (enableDoorSharing)
                        sb.AppendLine($"<color={colorCmdUsage}>door | d</color> - " +
                                      lang.GetMessage("CommandFriendDoor", this, UserIDString) + " " +
                                      (pInfo.DS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (enableBoxSharing)
                        sb.AppendLine($"<color={colorCmdUsage}>box | b</color> - " +
                                      lang.GetMessage("CommandFriendBox", this, UserIDString) + " " +
                                      (pInfo.BS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (enableLockerSharing)
                        sb.AppendLine($"<color={colorCmdUsage}>locker | l</color> - " +
                                      lang.GetMessage("CommandFriendLocker", this, UserIDString) + " " +
                                      (pInfo.LS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (enableQuarrySharing)
                        sb.AppendLine($"<color={colorCmdUsage}>quarry | q</color> - " +
                                      lang.GetMessage("CommandFriendQuarry", this, UserIDString) + " " +
                                      (pInfo.QS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (enableTurretSharing)
                        sb.AppendLine($"<color={colorCmdUsage}>turret | t</color> - " +
                                      lang.GetMessage("CommandFriendTurret", this, player.UserIDString) + " " +
                                      (pInfo.TS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                }

                if (usePermAutoAuth && permission.UserHasPermission(UserIDString, permAutoAuth) || !usePermAutoAuth)
                    sb.AppendLine($"<color={colorCmdUsage}>autoauth | a</color> - " +
                                  lang.GetMessage("CommandAutoAuth", this, UserIDString) + " " +
                                  (playerPrefs.PlayerInfo[userID].AA
                                      ? $"<color={colorON}>ON</color>"
                                      : $"<color={colorOFF}>OFF</color>"));
                if (hasClan)
                {
                    if (enableCupSharing)
                        sb.AppendLine($"<color={colorCmdUsage}>clancup | cc</color> - " +
                                      lang.GetMessage("CommandClanCup", this, UserIDString) + " " +
                                      (pInfo.CCS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (enableDoorSharing)
                        sb.AppendLine($"<color={colorCmdUsage}>clandoor | cd</color> - " +
                                      lang.GetMessage("CommandClanDoor", this, UserIDString) + " " +
                                      (pInfo.CDS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (enableBoxSharing)
                        sb.AppendLine($"<color={colorCmdUsage}>clanbox | cb</color> - " +
                                      lang.GetMessage("CommandClanBox", this, UserIDString) + " " +
                                      (pInfo.CBS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (enableLockerSharing)
                        sb.AppendLine($"<color={colorCmdUsage}>clanlocker | cl</color> - " +
                                      lang.GetMessage("CommandClanLocker", this, UserIDString) + " " +
                                      (pInfo.CLS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (enableQuarrySharing)
                        sb.AppendLine($"<color={colorCmdUsage}>clanquarry | cq</color> - " +
                                      lang.GetMessage("CommandClanQuarry", this, UserIDString) + " " +
                                      (pInfo.CQS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                    if (clanTurretShareOverride)
                        sb.AppendLine(lang.GetMessage("CommandClanTurret", this, UserIDString) + " " +
                                      (clanTurretShareOverride
                                          ? $"<color={colorON}>ON</color>"
                                          : $"<color={colorOFF}>OFF</color>"));
                    else
                        sb.AppendLine($"<color={colorCmdUsage}>clanturret | ct</color> - " +
                                      lang.GetMessage("CommandClanTurretM", this, UserIDString) + " " +
                                      (pInfo.CTS ? $"<color={colorON}>ON</color>" : $"<color={colorOFF}>OFF</color>"));
                }

                if (GetAdmin(player))
                    sb.AppendLine($"<color={prefixColor}>" + lang.GetMessage("CommandAdminAccess", this, UserIDString) +
                                  "</color>" + $" (<color={colorCmdUsage}>admin | adm</color>): " +
                                  (adminAccessEnabled[player.userID] == true
                                      ? $"<color={colorON}>ON</color>"
                                      : $"<color={colorOFF}>OFF</color>"));
                string openText = $"<color={colorTextMsg}>";
                string closeText = "</color>";
                string[] parts = sb.ToString().Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
                sb = new StringBuilder();
                foreach (string part in parts)
                {
                    if (sb.ToString().TrimEnd().Length + part.Length + openText.Length + closeText.Length > 1100)
                    {
                        PrintChat(player, openText + sb.ToString().TrimEnd() + closeText,
                            usdCnslInpt.Contains(player.userID) ? true : false);
                        sb.Clear();
                    }

                    sb.AppendLine(part);
                }

                PrintChat(player, openText + sb.ToString().TrimEnd() + closeText);
                return;
            }

            switch (args[0])
            {
                case "cup":
                case "c":
                    if (!friendsEnabled || !enableCupSharing) goto case "disabled";
                    if (!toggleCupShare) goto case "blocked";
                    pInfo.CS = !pInfo.CS;
                    CpSttsFrnd(player, pInfo.CS);
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.CS
                            ? lang.GetMessage("ShareEnabled", this, UserIDString)
                            : lang.GetMessage("ShareDisabled", this, UserIDString)) + "</color>");
                    break;
                case "door":
                case "d":
                    if (!friendsEnabled || !enableDoorSharing) goto case "disabled";
                    if (!toggleDoorShare) goto case "blocked";
                    pInfo.DS = !pInfo.DS;
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.DS
                            ? lang.GetMessage("CodesEnabled", this, UserIDString)
                            : lang.GetMessage("CodesDisabled", this, UserIDString)) + "</color>");
                    break;
                case "box":
                case "b":
                    if (!friendsEnabled || !enableBoxSharing) goto case "disabled";
                    if (!toggleBoxShare) goto case "blocked";
                    pInfo.BS = !pInfo.BS;
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.BS
                            ? lang.GetMessage("BoxesEnabled", this, UserIDString)
                            : lang.GetMessage("BoxesDisabled", this, UserIDString)) + "</color>");
                    break;
                case "locker":
                case "l":
                    if (!friendsEnabled || !enableLockerSharing) goto case "disabled";
                    if (!toggleLockerShare) goto case "blocked";
                    pInfo.LS = !pInfo.LS;
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.LS
                            ? lang.GetMessage("LockersEnabled", this, UserIDString)
                            : lang.GetMessage("LockersDisabled", this, UserIDString)) + "</color>");
                    break;
                case "quarry":
                case "q":
                    if (!friendsEnabled || !enableQuarrySharing) goto case "disabled";
                    if (!toggleQuarryShare) goto case "blocked";
                    pInfo.QS = !pInfo.QS;
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (playerPrefs.PlayerInfo[userID].QS
                            ? lang.GetMessage("QuarriesEnabled", this, UserIDString)
                            : lang.GetMessage("QuarriesDisabled", this, UserIDString)) + "</color>");
                    break;
                case "turret":
                case "t":
                    if (!friendsEnabled || !enableTurretSharing) goto case "disabled";
                    if (!toggleTurretShare) goto case "blocked";
                    pInfo.TS = !pInfo.TS;
                    TrrtSttsFrnd(player, pInfo.TS);
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.TS
                            ? lang.GetMessage("TurretEnabled", this, UserIDString)
                            : lang.GetMessage("TurretDisabled", this, UserIDString)) + "</color>");
                    break;
                case "autoauth":
                case "a":
                    if (usePermAutoAuth && !permission.UserHasPermission(UserIDString, permAutoAuth))
                        goto case "disabled";
                    if (!toggleAutoAuth) goto case "blocked";
                    pInfo.AA = !pInfo.AA;
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.AA
                            ? lang.GetMessage("AutoAuthEnabled", this, UserIDString)
                            : lang.GetMessage("AutoAuthDisabled", this, UserIDString)) + "</color>");
                    break;
                case "clancup":
                case "cc":
                    if (!hasClan || !enableCupSharing) goto case "disabled";
                    if (!toggleClanCupShare) goto case "blocked";
                    pInfo.CCS = !pInfo.CCS;
                    CpSttsCln(player, pInfo.CCS);
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.CCS
                            ? lang.GetMessage("ClanShareEnabled", this, UserIDString)
                            : lang.GetMessage("ClanShareDisabled", this, UserIDString)) + "</color>");
                    break;
                case "clandoor":
                case "cd":
                    if (!hasClan || !enableDoorSharing) goto case "disabled";
                    if (!toggleClanDoorShare) goto case "blocked";
                    pInfo.CDS = !pInfo.CDS;
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.CDS
                            ? lang.GetMessage("ClanCodesEnabled", this, UserIDString)
                            : lang.GetMessage("ClanCodesDisabled", this, UserIDString)) + "</color>");
                    break;
                case "clanbox":
                case "cb":
                    if (!hasClan || !enableBoxSharing) goto case "disabled";
                    if (!toggleClanBoxShare) goto case "blocked";
                    pInfo.CBS = !pInfo.CBS;
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.CBS
                            ? lang.GetMessage("ClanBoxesEnabled", this, UserIDString)
                            : lang.GetMessage("ClanBoxesDisabled", this, UserIDString)) + "</color>");
                    break;
                case "clanlocker":
                case "cl":
                    if (!hasClan || !enableLockerSharing) goto case "disabled";
                    if (!toggleClanLockerShare) goto case "blocked";
                    pInfo.CLS = !pInfo.CLS;
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.CLS
                            ? lang.GetMessage("ClanLockersEnabled", this, UserIDString)
                            : lang.GetMessage("ClanLockersDisabled", this, UserIDString)) + "</color>");
                    break;
                case "clanquarry":
                case "cq":
                    if (!hasClan || !enableQuarrySharing) goto case "disabled";
                    if (!toggleClanQuarryShare) goto case "blocked";
                    pInfo.CQS = !pInfo.CQS;
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.CQS
                            ? lang.GetMessage("ClanQuarriesEnabled", this, UserIDString)
                            : lang.GetMessage("ClanQuarriesDisabled", this, UserIDString)) + "</color>");
                    break;
                case "clanturret":
                case "ct":
                    if (!hasClan || clanTurretShareOverride || !enableTurretSharing) goto case "disabled";
                    if (!toggleClanTurretShare) goto case "blocked";
                    pInfo.CTS = !pInfo.CTS;
                    TrrtSttsCln(player, pInfo.CTS);
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        (pInfo.CTS
                            ? lang.GetMessage("ClanTurretEnabled", this, UserIDString)
                            : lang.GetMessage("ClanTurretDisabled", this, UserIDString)) + "</color>");
                    break;
                case "disabled":
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("NotEnabled", this, UserIDString), args[0]) + $"</color>");
                    break;
                case "blocked":
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("SwitchBlocked", this, UserIDString), args[0]) + $"</color>");
                    break;
                case "all":
                    if (friendsEnabled)
                    {
                        if (enableCupSharing && toggleCupShare && !pInfo.CS)
                        {
                            pInfo.CS = true;
                            CpSttsFrnd(player, true);
                        }

                        if (enableDoorSharing && toggleDoorShare) pInfo.DS = true;
                        if (enableBoxSharing && toggleBoxShare) pInfo.BS = true;
                        if (enableLockerSharing && toggleLockerShare) pInfo.LS = true;
                        if (enableTurretSharing && toggleTurretShare && !pInfo.TS)
                        {
                            pInfo.TS = true;
                            TrrtSttsFrnd(player, true);
                        }
                    }

                    if (usePermAutoAuth && !permission.UserHasPermission(UserIDString, permAutoAuth) ||
                        !usePermAutoAuth)
                        if (toggleAutoAuth)
                            pInfo.AA = true;
                    if (hasClan)
                    {
                        if (enableCupSharing && toggleClanCupShare && !pInfo.CCS)
                        {
                            pInfo.CCS = true;
                            CpSttsCln(player, true);
                        }

                        if (enableDoorSharing && toggleClanDoorShare) pInfo.CDS = true;
                        if (enableBoxSharing && toggleClanBoxShare) pInfo.CBS = true;
                        if (enableLockerSharing && toggleClanLockerShare) pInfo.CLS = true;
                        if (enableTurretSharing && !clanTurretShareOverride && toggleClanTurretShare && !pInfo.CTS)
                        {
                            pInfo.CTS = true;
                            TrrtSttsCln(player, true);
                        }
                    }

                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("SharedAll", this, UserIDString)) + $"</color>");
                    break;
                case "admin":
                case "adm":
                    if (!GetAdmin(player)) goto default;
                    adminAccessEnabled[player.userID] = !adminAccessEnabled[player.userID];
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        ((bool) adminAccessEnabled[player.userID]
                            ? lang.GetMessage("AdminAccessEnabled", this, UserIDString)
                            : lang.GetMessage("AdminAccessDisabled", this, UserIDString)) + "</color>");
                    if (!adminAccessEnabled[player.userID])
                    {
                        if (!adminsRemainCupAuthed)
                            foreach (uint cup in adminCupboards[player.userID].ToList())
                            {
                                BuildingPrivlidge p = (BuildingPrivlidge) BaseNetworkable.serverEntities.Find(cup);
                                if (p)
                                {
                                    p.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == player.userID);
                                    p.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                                }

                                adminCupboards[player.userID].Remove(cup);
                            }

                        if (!adminsRemainTurretAuthed)
                            foreach (uint turret in adminTurrets[player.userID].ToList())
                            {
                                AutoTurret t = (AutoTurret) BaseNetworkable.serverEntities.Find(turret);
                                if (t)
                                {
                                    t.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == player.userID);
                                    t.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                                }

                                adminTurrets[player.userID].Remove(turret);
                            }
                    }

                    break;
                case "help":
                case "h":
                    if (args.Length != 2)
                    {
                        StringBuilder sbhelp = new StringBuilder();
                        if (!usdCnslInpt.Contains(player.userID))
                            sbhelp.AppendLine($"<size=16><color={prefixColor}>{pluginPrefix}</color></size>");
                        sbhelp.AppendLine(lang.GetMessage("CommandUsage", this, UserIDString) +
                                          $"<color={colorCmdUsage}>/{shareCommand} help option</color>");
                        if (enableCupSharing)
                            sbhelp.AppendLine($"<color={colorCmdUsage}>cups | c</color> - " +
                                              lang.GetMessage("HelpCups", this, UserIDString));
                        if (enableDoorSharing)
                            sbhelp.AppendLine($"<color={colorCmdUsage}>doors | d</color> - " +
                                              lang.GetMessage("HelpDoors", this, UserIDString));
                        if (enableBoxSharing)
                            sbhelp.AppendLine($"<color={colorCmdUsage}>boxes | b</color> - " +
                                              lang.GetMessage("HelpBoxes", this, UserIDString));
                        if (enableLockerSharing)
                            sbhelp.AppendLine($"<color={colorCmdUsage}>lockers | l</color> - " +
                                              lang.GetMessage("HelpLockers", this, UserIDString));
                        if (enableQuarrySharing)
                            sbhelp.AppendLine($"<color={colorCmdUsage}>quarries | q</color> - " +
                                              lang.GetMessage("HelpQuarries", this, UserIDString));
                        if (enableTurretSharing)
                            sbhelp.AppendLine($"<color={colorCmdUsage}>turrets | t</color> - " +
                                              lang.GetMessage("HelpTurrets", this, UserIDString));
                        if (enableAutoAuth)
                            sbhelp.AppendLine($"<color={colorCmdUsage}>autoauth | a</color> - " +
                                              lang.GetMessage("HelpAutoAuth", this, UserIDString));
                        PrintChat(player, $"<color={colorTextMsg}>" + sbhelp.ToString() + "</color>");
                    }
                    else if (args.Length >= 2)
                    {
                        switch (args[1])
                        {
                            case "cups":
                            case "c":
                                PrintChat(player,
                                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                                    lang.GetMessage("DescriptionCups", this, UserIDString) + "</color>");
                                break;
                            case "doors":
                            case "d":
                                PrintChat(player,
                                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                                    lang.GetMessage("DescriptionDoors", this, UserIDString) + "</color>");
                                break;
                            case "boxes":
                            case "b":
                                PrintChat(player,
                                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                                    lang.GetMessage("DescriptionBoxes", this, UserIDString) + "</color>");
                                break;
                            case "lockers":
                            case "l":
                                PrintChat(player,
                                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                                    lang.GetMessage("DescriptionLockers", this, UserIDString) + "</color>");
                                break;
                            case "quarries":
                            case "q":
                                PrintChat(player,
                                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                                    lang.GetMessage("DescriptionQuarries", this, UserIDString) + "</color>");
                                break;
                            case "turrets":
                            case "t":
                                PrintChat(player,
                                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                                    lang.GetMessage("DescriptionTurrets", this, UserIDString) + "</color>");
                                break;
                            case "autoauth":
                            case "a":
                                PrintChat(player,
                                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                                    lang.GetMessage("DescriptionAutoAuth", this, UserIDString) + "</color>");
                                break;
                            default:
                                PrintChat(player,
                                    string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                                    lang.GetMessage("HelpNotAvailable", this, UserIDString) + "</color>");
                                break;
                        }
                    }

                    break;
                default:
                    PrintChat(player,
                        string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" +
                        string.Format(lang.GetMessage("NotSupported", this, UserIDString), args[0]) + $"</color>");
                    break;
            }

            pInfo.BPH?.Reset();
        }

        private void PrintChat(BasePlayer player, string message, bool keepConsole = false)
        {
            if (usdCnslInpt.Contains(player.userID)) player.ConsoleMessage(message);
            else player.ChatMessage(message);
            if (!keepConsole) usdCnslInpt.Remove(player.userID);
        }

        internal class BuildingPrivilegeHandler : FacepunchBehaviour
        {
            private BasePlayer player;
            private ulong userID;
            private BuildingPrivlidge lastPrivilege;
            private uint lastTurretID;
            private object lastTurretResult;

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                userID = player.userID;
                Instance.playerPrefs.PlayerInfo[userID].BPH = this;

                if (Instance.enableCupSharing)
                    InvokeRandomized(Repeater, 1f, 1.5f, 0.15f);

                lastTurretResult = null;
                lastTurretID = 0u;
            }

            public void Reset()
            {
                lastPrivilege = null;
                lastTurretID = 0u;
                lastTurretResult = null;
            }

            public bool SameTurret(uint currentID)
            {
                if (currentID == lastTurretID)
                    return true;
                return false;
            }

            public object LastResult()
            {
                return lastTurretResult;
            }

            public void SetTurret(uint currentID, object flag)
            {
                lastTurretID = currentID;
                lastTurretResult = flag;
            }

            private void Repeater()
            {
                if (player == null || !player.IsConnected || player.IsDead())
                {
                    DoDestroy();
                    return;
                }

                if (player.IsSleeping() || player.IsSpectating() || player.IsReceivingSnapshot || Instance.pluginDisabled)
                    return;

                BuildingPrivlidge buildingPrivilege = player.GetBuildingPrivilege();

                if (buildingPrivilege == null || lastPrivilege != null && lastPrivilege == buildingPrivilege)
                    return;

                if (!buildingPrivilege.IsAuthed(player))
                    Instance.CheckCupboardAccess(player, buildingPrivilege);

                lastPrivilege = buildingPrivilege;
            }

            public void DoDestroy()
            {
                CancelInvoke(Repeater);
                Instance.playerPrefs.PlayerInfo[userID].BPH = null;
                Destroy(this);
            }

            private void OnDestroy()
            {
                if (IsInvoking(Repeater))
                    CancelInvoke(Repeater);

                Instance.playerPrefs.PlayerInfo[userID].BPH = null;
            }
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player != null && (enableCupSharing || enableTurretSharing))
                playerPrefs.PlayerInfo[player.userID].BPH = player.transform.GetOrAddComponent<BuildingPrivilegeHandler>();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player != null)
            {
                SetPlayer(player);
                if ((enableCupSharing || enableTurretSharing) && player.IsConnected && !player.IsDead())
                    playerPrefs.PlayerInfo[player.userID].BPH = player.transform.GetOrAddComponent<BuildingPrivilegeHandler>();
            }
        }

        private bool HasFriend(ulong owner = 0uL, ulong friend = 0uL)
        {
            if (owner < 76561197960265729uL || friend < 76561197960265729uL) return false;
            if (friendsAPIEnabled)
                return Friends != null && Friends.Call("HasFriend", owner, friend) != null &&
                       (bool) Friends.Call("HasFriend", owner, friend);
            return false;
        }

        private bool HasFriendS(string owner, string friend)
        {
            if (friendsAPIEnabled)
                return Friends != null && Friends.Call("HasFriendS", owner, friend) != null &&
                       (bool) Friends.CallHook("HasFriendS", owner, friend);
            return false;
        }

        private bool SameClan(ulong owner, ulong member)
        {
            object o = Clans.Call("GetClanOf", owner);
            object m = Clans.Call("GetClanOf", member);
            if (o != null && m != null && (string) o == (string) m) return true;
            return false;
        }

        private bool SameClanS(string owner, string member)
        {
            object o = Clans.Call("GetClanOf", owner);
            object m = Clans.Call("GetClanOf", member);
            if (o != null && m != null && (string) o == (string) m) return true;
            return false;
        }

        private void StateDisabled()
        {
            Unsubscribe(nameof(OnEntityKill));
            Unsubscribe(nameof(OnTurretTarget));
            Unsubscribe(nameof(CanBeTargeted));
            Unsubscribe(nameof(OnEntityBuilt));
            Unsubscribe(nameof(OnCupboardClearList));
            Unsubscribe(nameof(OnCupboardAuthorize));
            Unsubscribe(nameof(OnLootEntity));
            Unsubscribe(nameof(CanUseLockedEntity));
            Unsubscribe(nameof(CanUnlock));
            Unsubscribe(nameof(CanLock));
        }

        private void StateEnabled()
        {
            Subscribe(nameof(OnEntityKill));
            if (enableTurretSharing) Subscribe(nameof(OnTurretTarget));
            if (enableTurretSharing) Subscribe(nameof(CanBeTargeted));
            if (enableAutoAuth) Subscribe(nameof(OnEntityBuilt));
            if (enableCupSharing) Subscribe(nameof(OnCupboardClearList));
            if (enableCupSharing) Subscribe(nameof(OnCupboardAuthorize));
            if (enableQuarrySharing) Subscribe(nameof(OnLootEntity));
            Subscribe(nameof(CanUseLockedEntity));
            Subscribe(nameof(CanUnlock));
            Subscribe(nameof(CanLock));
        }

        private void ClansDisabled()
        {
            Unsubscribe(nameof(OnClanMemberJoined));
            Unsubscribe(nameof(OnClanMemberGone));
            Unsubscribe(nameof(OnClanDisbanded));
        }

        private void ClansEnabled()
        {
            Subscribe(nameof(OnClanMemberJoined));
            Subscribe(nameof(OnClanMemberGone));
            Subscribe(nameof(OnClanDisbanded));
        }
    }
}