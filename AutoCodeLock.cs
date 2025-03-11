using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using ProtoBuf;
namespace Oxide.Plugins
{
    [Info("AutoCodeLock", "FuJiCuRa", "2.2.12", ResourceId = 15)]
    [Description("CodeLock & Door automation tools")]
    class AutoCodeLock : RustPlugin
    {
        [PluginReference] Plugin NoEscape;
        bool Changed = false;
        bool Initialized = false;
        static AutoCodeLock ACL;
        StoredData playerPrefs = new StoredData();
        List<ulong> usedConsoleInput = new List<ulong>();
        DateTime Epoch = new DateTime(1970, 1, 1);
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        class StoredData
        {
            public Dictionary<ulong,
            PlayerInfo> PlayerInfo = new Dictionary<ulong,
            PlayerInfo>();
            public Int32 saveStamp = 0;
            public string lastStorage = string.Empty;
            public StoredData() { }
        }
        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        class PlayerInfo
        {
            public bool AL = ACL.autoLock;
            public bool DLD = ACL.deployDoor;
            public bool DLB = ACL.deployBox;
            public bool DLL = ACL.deployLocker;
            public bool DLC = ACL.deployCupboard;
            public string PC;
            public string GC;
            public bool EGC = ACL.enableGuestCode;
            public bool DDC = ACL.deployDoorCloser;
            public float DCD = ACL.defaultCloseDelay;
            public float LHD = ACL.defaultHatchDelay;
            public PlayerInfo() { }
        }
        bool useProtostorageUserdata;
        bool autoUpdateChangedDelays;
        string codelockCommand;
        bool notifyAuthCodeLock;
        string permissionDeployDoor;
        string permissionDeployBox;
        string permissionDeployLocker;
        string permissionDeployCupboard;
        string permissionAutoLock;
        string permissionNoLockNeed;
        string permissionDoorCloser;
        string permissionAll;
        string pluginPrefix;
        string prefixColor;
        string prefixFormat;
        string colorTextMsg;
        string colorCmdUsage;
        string colorON;
        string colorOFF;
        bool adminAutoRights;
        bool autoLock;
        bool deployDoor;
        bool deployBox;
        bool deployLocker;
        bool deployCupboard;
        bool enableGuestCode;
        bool deployDoorCloser;
        float doorCloserMinDelay;
        float doorCloserMaxDelay;
        float defaultCloseDelay;
        float ladderHatchMinDelay;
        float ladderHatchMaxDelay;
        float defaultHatchDelay;
        bool checkPlayerForRaidBlocked;
        bool checkPlayerForCombatBlocked;
        bool denyCloserPickupByPlayer;
        bool denyCloserPickupByAdmin;
        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string,
            object>;
            if (data == null)
            {
                data = new Dictionary<string,
                object>();
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
        void LoadVariables()
        { 
            codelockCommand = Convert.ToString(GetConfig("Command", "codelockCommand", "codelock"));
            notifyAuthCodeLock = Convert.ToBoolean(GetConfig("Options", "notifyAuthCodeLock", true));
            autoUpdateChangedDelays = Convert.ToBoolean(GetConfig("Options", "autoUpdateChangedDelays", false));
            doorCloserMinDelay = Convert.ToSingle(GetConfig("Options", "doorCloserMinDelay", 2.0f));
            doorCloserMaxDelay = Convert.ToSingle(GetConfig("Options", "doorCloserMaxDelay", 15.0f));
            ladderHatchMinDelay = Convert.ToSingle(GetConfig("Options", "ladderHatchMinDelay", 3.0f));
            ladderHatchMaxDelay = Convert.ToSingle(GetConfig("Options", "ladderHatchMaxDelay", 15.0f));
            checkPlayerForRaidBlocked = Convert.ToBoolean(GetConfig("Options", "checkPlayerForRaidBlocked", true));
            checkPlayerForCombatBlocked = Convert.ToBoolean(GetConfig("Options", "checkPlayerForCombatBlocked", false));
            denyCloserPickupByPlayer = Convert.ToBoolean(GetConfig("Options", "denyCloserPickupByPlayer", true));
            denyCloserPickupByAdmin = Convert.ToBoolean(GetConfig("Options", "denyCloserPickupByAdmin", false));
            permissionDeployDoor = Convert.ToString(GetConfig("Permissions", "permissionDeployDoor", "autocodelock.deploydoor"));
            permissionDeployBox = Convert.ToString(GetConfig("Permissions", "permissionDeployBox", "autocodelock.deploybox"));
            permissionDeployLocker = Convert.ToString(GetConfig("Permissions", "permissionDeployLocker", "autocodelock.deploylocker"));
            permissionDeployCupboard = Convert.ToString(GetConfig("Permissions", "permissionDeployCupboard", "autocodelock.deploycup"));
            permissionAutoLock = Convert.ToString(GetConfig("Permissions", "permissionAutoLock", "autocodelock.autolock"));
            permissionNoLockNeed = Convert.ToString(GetConfig("Permissions", "permissionNoLockNeed", "autocodelock.nolockneed"));
            permissionDoorCloser = Convert.ToString(GetConfig("Permissions", "permissionDoorCloser", "autocodelock.doorcloser"));
            permissionAll = Convert.ToString(GetConfig("Permissions", "permissionAll", "autocodelock.all"));
            adminAutoRights = Convert.ToBoolean(GetConfig("Permissions", "adminAutoRights", true));
            pluginPrefix = Convert.ToString(GetConfig("Formatting", "pluginPrefix", "AutoCodeLock"));
            prefixColor = Convert.ToString(GetConfig("Formatting", "prefixColor", "#ffa500"));
            prefixFormat = Convert.ToString(GetConfig("Formatting", "prefixFormat", "<color={0}>{1}</color>: "));
            colorTextMsg = Convert.ToString(GetConfig("Formatting", "colorTextMsg", "#ffffff"));
            colorCmdUsage = Convert.ToString(GetConfig("Formatting", "colorCmdUsage", "#ffff00"));
            colorON = Convert.ToString(GetConfig("Formatting", "colorON", "#008000"));
            colorOFF = Convert.ToString(GetConfig("Formatting", "colorOFF", "#c0c0c0"));
            autoLock = Convert.ToBoolean(GetConfig("PlayerDefaults", "AutoLock", false));
            deployDoor = Convert.ToBoolean(GetConfig("PlayerDefaults", "DeployDoor", false));
            deployBox = Convert.ToBoolean(GetConfig("PlayerDefaults", "DeployBox", false));
            deployLocker = Convert.ToBoolean(GetConfig("PlayerDefaults", "DeployLocker", false));
            deployCupboard = Convert.ToBoolean(GetConfig("PlayerDefaults", "DeployCupboard", false));
            enableGuestCode = Convert.ToBoolean(GetConfig("PlayerDefaults", "UseGuestCode", false));
            deployDoorCloser = Convert.ToBoolean(GetConfig("PlayerDefaults", "DeployDoorCloser", false));
            defaultCloseDelay = Convert.ToSingle(GetConfig("PlayerDefaults", "DefaultCloseDelay", 3.0f));
            defaultHatchDelay = Convert.ToSingle(GetConfig("PlayerDefaults", "DefaultHatchdelay", 5.0f));
            useProtostorageUserdata = Convert.ToBoolean(GetConfig("Storage", "useProtostorageUserdata", false));
            var configRemoval = false;
            if ((Config.Get("Options") as Dictionary<string, object>).ContainsKey("closerPickupReplaceItem"))
            {
                (Config.Get("Options") as Dictionary<string, object>).Remove("closerPickupReplaceItem");
                configRemoval = true;
            }
            if (!Changed && !configRemoval) return;
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
            lang.RegisterMessages(new Dictionary<string, string> {
                {
                    "AutoLockEnabled",
                    "CodeLock automation (secure and lock) enabled"
                },
                {
                    "AutoLockDisabled",
                    "CodeLock automation disabled"
                },
                {
                    "DeployLockDoorEnabled",
                    "Doors will include codelocks on deploy"
                },
                {
                    "DeployLockDoorDisabled",
                    "Doors will not include codelocks on deploy"
                },
                {
                    "DeployDoorCloserEnabled",
                    "Doors will include doorclosers on deploy"
                },
                {
                    "DeployDoorCloserDisabled",
                    "Doors will not include doorclosers on deploy"
                },
                {
                    "DeployLockBoxEnabled",
                    "Boxes will include codelocks on deploy"
                },
                {
                    "DeployLockBoxDisabled",
                    "Boxes will not include codelocks on deploy"
                },
                {
                    "DeployLockLockerEnabled",
                    "Locker will include codelocks on deploy"
                },
                {
                    "DeployLockLockerDisabled",
                    "Locker will not include codelocks on deploy"
                },
                {
                    "DeployLockCupEnabled",
                    "Cupboards will include codelocks on deploy"
                },
                {
                    "DeployLockCupDisabled",
                    "Cupboards will not include codelocks on deploy"
                },
                {
                    "UseGuestCodeEnabled",
                    "Guest PIN will be set with codelocks"
                },
                {
                    "UseGuestCodeDisabled",
                    "Guest PIN gets not set with codelocks"
                },
                {
                    "CodeAuth",
                    "CodeLock secured and locked with '{0}'"
                },
                {
                    "CodeAuthBoth",
                    "CodeLock secured and locked with '{0}', guest set to '{1}'"
                },
                {
                    "NoAccess",
                    "You are not granted for this feature"
                },
                {
                    "NotLocked",
                    "Codelock not locked. You are not the object owner"
                },
                {
                    "NotLockedByBlock",
                    "Codelock not auto-locked. You are currently raidblocked"
                },
                {
                    "NotLockedByMsg",
                    "Codelock not auto-locked for reason: "
                },
                {
                    "NotSupported",
                    "The specific function '{0}' is not available"
                },
                {
                    "UpdatePin",
                    "Updates all doors with the current PIN"
                },
                {
                    "UpdateGuestPin",
                    "Updates all doors with the current guest PIN"
                },
                {
                    "UpdateCloseDelay",
                    "Updates all doorclosers with the current close & hatch delays"
                },
                {
                    "CommandUsage",
                    "Command usage:"
                },
                {
                    "CommandToggle",
                    "All switches toggle their setting (on<>off)"
                },
                {
                    "CommandAutolock",
                    "Autolock feature:"
                },
                {
                    "CommandDoorCloser",
                    "Doorcloser feature:"
                },
                {
                    "CommandCloseDelay",
                    "Your current close delay:"
                },
                {
                    "CommandHatchDelay",
                    "Your current hatch delay:"
                },
                {
                    "CommandPinCode",
                    "Your current PIN:"
                },
                {
                    "CommandGuestCode",
                    "Your current guest PIN:"
                },
                {
                    "CommandGuest",
                    "Guest PIN feature:"
                },
                {
                    "CommandPinCodeSetTo",
                    "Your PIN was successful set to:"
                },
                {
                    "CommandGuestCodeSetTo",
                    "Your guest PIN was succesful set to:"
                },
                {
                    "CommandCloseDelaySetTo",
                    "Your close delay was successful set to: {0}s"
                },
                {
                    "CommandHatchDelaySetTo",
                    "Your hatch delay was successful set to: {0}s"
                },
                {
                    "CommandPinCodeHelp",
                    "Set your PIN with <color={0}>/{1} pin|p <1234></color> (4-Digits)"
                },
                {
                    "CommandGuestCodeHelp",
                    "Set your guest PIN with <color={0}>/{1} guestpin | gp 1234</color> (4-Digits)"
                },
                {
                    "CommandCloseDelayHelp",
                    "Set your delay with <color={0}>/{1} closedelay | cd x</color> ({2}-{3}s)"
                },
                {
                    "CommandHatchDelayHelp",
                    "Set your delay with <color={0}>/{1} hatchdelay | hd x</color> ({2}-{3}s)"
                },
                {
                    "CommandDeployDoor",
                    "Deploy with Door:"
                },
                {
                    "CommandDeployBox",
                    "Deploy with Box:"
                },
                {
                    "CommandDeployLocker",
                    "Deploy with Locker:"
                },
                {
                    "CommandDeployCupboard",
                    "Deploy with Cupboard:"
                },
                {
                    "StreamerMode",
                    "<color=#ffa500>NOTE</color>: Active <color=#ffff00>streamermode</color> does cloak the PINs"
                },
                {
                    "UpdatedDoors",
                    "Updated '{0}' doors with your current setting"
                },
            },
            this);
        }
        void Init()
        {
            LoadVariables();
            LoadDefaultMessages();
            cmd.AddChatCommand(codelockCommand, this, "CodeLockCommand");
            cmd.AddConsoleCommand(codelockCommand, this, "cCodeLockCommand");
        }
        void Loaded()
        {
            ACL = this;
        }
        void LoadPlayerData()
        {
            StoredData protoStorage = new StoredData();
            if (ProtoStorage.Exists(new string[] { this.Title })) protoStorage = ProtoStorage.Load<StoredData>(new string[] { this.Title }) ?? new StoredData();
            StoredData jsonStorage = new StoredData();
            if (Interface.GetMod().DataFileSystem.ExistsDatafile(this.Title)) jsonStorage = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
            bool lastwasProto = (protoStorage.lastStorage == "proto" && protoStorage.saveStamp > jsonStorage.saveStamp);
            if (useProtostorageUserdata)
            {
                if (lastwasProto)
                {
                    playerPrefs = ProtoStorage.Load<StoredData>(new string[] { this.Title }) ?? new StoredData();
                    return;
                }
                else
                {
                    if (Interface.GetMod().DataFileSystem.ExistsDatafile(this.Title)) playerPrefs = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
                }
            }
            else
            {
                if (!lastwasProto)
                {
                    playerPrefs = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title);
                    return;
                }
                else
                {
                    if (ProtoStorage.Exists(new string[] { this.Title })) playerPrefs = ProtoStorage.Load<StoredData>(new string[] { this.Title }) ?? new StoredData();
                }
            }
        }
        void OnServerInitialized()
        {
            if (!permission.PermissionExists(permissionDeployDoor)) permission.RegisterPermission(permissionDeployDoor, this);
            if (!permission.PermissionExists(permissionAutoLock)) permission.RegisterPermission(permissionAutoLock, this);
            if (!permission.PermissionExists(permissionDeployBox)) permission.RegisterPermission(permissionDeployBox, this);
            if (!permission.PermissionExists(permissionDeployLocker)) permission.RegisterPermission(permissionDeployLocker, this);
            if (!permission.PermissionExists(permissionDeployCupboard)) permission.RegisterPermission(permissionDeployCupboard, this);
            if (!permission.PermissionExists(permissionAll)) permission.RegisterPermission(permissionAll, this);
            if (!permission.PermissionExists(permissionNoLockNeed)) permission.RegisterPermission(permissionNoLockNeed, this);
            if (!permission.PermissionExists(permissionDoorCloser)) permission.RegisterPermission(permissionDoorCloser, this);

            LoadPlayerData();

            foreach (var player in BasePlayer.activePlayerList) 
                SetPlayer(player);

            foreach (var player in BasePlayer.sleepingPlayerList) 
                SetPlayer(player);

            _refreshAllDoorCloserDelays = ServerMgr.Instance.StartCoroutine(RefreshAllDoorCloserDelays());

            Initialized = true;
        }
        void SetPlayer(BasePlayer player)
        {
            if (player == null) return;
            PlayerInfo p = null;
            if (!playerPrefs.PlayerInfo.TryGetValue(player.userID, out p))
            {
                var info = new PlayerInfo();
                info.AL = autoLock;
                info.DLD = deployDoor;
                info.DLB = deployBox;
                info.DLL = deployLocker;
                info.DLC = deployCupboard;
                info.PC = Convert.ToString(UnityEngine.Random.Range(1, 9999)).PadLeft(4, '0');
                info.GC = Convert.ToString(UnityEngine.Random.Range(1, 9999)).PadLeft(4, '0');
                info.EGC = enableGuestCode;
                info.DDC = deployDoorCloser;
                info.DCD = defaultCloseDelay;
                info.LHD = defaultHatchDelay;
                playerPrefs.PlayerInfo.Add(player.userID, info);
                return;
            }
            return;
        }
        void OnPlayerConnected(BasePlayer player) => SetPlayer(player);

        Coroutine _updateCloserDelays;
        Coroutine _updateDoorCodes;
        Coroutine _updateGuestCodes;
        Coroutine _refreshDoorClosers;
        Coroutine _refreshAllDoorCloserDelays;

        void Unload()
        {
            SaveData();
            if (Interface.Oxide.IsShuttingDown) return;
            if (_updateCloserDelays != null) ServerMgr.Instance.StopCoroutine(_updateCloserDelays);
            if (_updateDoorCodes != null) ServerMgr.Instance.StopCoroutine(_updateDoorCodes);
            if (_updateGuestCodes != null) ServerMgr.Instance.StopCoroutine(_updateGuestCodes);
            if (_refreshDoorClosers != null) ServerMgr.Instance.StopCoroutine(_refreshDoorClosers);
            if (_refreshAllDoorCloserDelays != null) ServerMgr.Instance.StopCoroutine(_refreshAllDoorCloserDelays);
        }
        void OnServerSave() => SaveData();
        void SaveData()
        {
            if (!Initialized) return;
            playerPrefs.saveStamp = (int)DateTime.UtcNow.Subtract(Epoch).TotalSeconds;
            playerPrefs.lastStorage = useProtostorageUserdata ? "proto" : "json";
            if (useProtostorageUserdata) ProtoStorage.Save<StoredData>(playerPrefs, new string[] {
                this.Title
            });
            else Interface.Oxide.DataFileSystem.WriteObject(this.Title, playerPrefs);
        }
        void OnItemDeployed(Deployer deployer, BaseEntity entity)
        {
            if (!Initialized || deployer == null || entity == null || entity.OwnerID == 0uL || deployer.GetDeployable().slot != BaseEntity.Slot.Lock || !(entity.GetSlot(BaseEntity.Slot.Lock) is CodeLock)) return;
            var owner = deployer.GetOwnerPlayer();
            if (PlayerHasPerm(owner.UserIDString, permissionAutoLock, owner.IsAdmin) && playerPrefs.PlayerInfo[owner.userID].AL)
            {
                if (owner.userID != entity.OwnerID && !(owner.IsAdmin && adminAutoRights))
                {
                    PrintToChat(owner, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NotLocked", this, owner.UserIDString) + "</color>");
                    return;
                }
                var externalPlugins = Interface.CallHook("CanAutoLock", owner);
                if (externalPlugins != null && !(owner.IsAdmin && adminAutoRights))
                {
                    PrintToChat(owner, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NotLockedByMsg", this, owner.UserIDString) + $"{(externalPlugins is string ? (string)externalPlugins : string.Empty)}</color>");
                    return;
                }
                if (checkPlayerForRaidBlocked && NoEscape)
                {
                    bool isBlocked = (bool)NoEscape?.CallHook("IsRaidBlocked", owner);
                    if (isBlocked && !(owner.IsAdmin && adminAutoRights))
                    {
                        PrintToChat(owner, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NotLockedByBlock", this, owner.UserIDString) + "</color>");
                        return;
                    }
                }
                if (checkPlayerForCombatBlocked && NoEscape)
                {
                    bool isBlocked = (bool)NoEscape?.CallHook("IsCombatBlocked", owner);
                    if (isBlocked && !(owner.IsAdmin && adminAutoRights))
                    {
                        PrintToChat(owner, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NotLockedByBlock", this, owner.UserIDString) + "</color>");
                        return;
                    }
                }
                CodeLockPrepare(deployer, entity, owner);
            }
        }
        void CodeLockPrepare(Deployer deployer, BaseEntity entity, BasePlayer owner)
        {
            var codelock = entity.GetSlot(BaseEntity.Slot.Lock) as CodeLock;
            codelock.code = GetOrSetPin(owner.userID);
            codelock.hasCode = true;
            codelock.whitelistPlayers.Add(owner.userID);
            if (playerPrefs.PlayerInfo[owner.userID].EGC)
            {
                codelock.guestCode = GetOrSetGuest(owner.userID);
                codelock.hasGuestCode = true;
            } (codelock as BaseEntity).SetFlag(BaseEntity.Flags.Locked, true, false);
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", entity.transform.position);
            if (notifyAuthCodeLock)
            {
                var code = codelock.code;
                var guestCode = codelock.guestCode;
                if (owner.net.connection.info.GetBool("global.streamermode"))
                {
                    code = "****";
                    guestCode = "****";
                }
                if (playerPrefs.PlayerInfo[owner.userID].EGC) PrintToChat(owner, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("CodeAuthBoth", this, owner.UserIDString), code, guestCode + "</color>");
                else PrintToChat(owner, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("CodeAuth", this, owner.UserIDString), code + "</color>");
            }
        }
        string GetOrSetPin(ulong userID)
        {
            if (playerPrefs.PlayerInfo[userID].PC != string.Empty) return playerPrefs.PlayerInfo[userID].PC;
            var newCode = Convert.ToString(UnityEngine.Random.Range(1, 9999)).PadLeft(4, '0');
            playerPrefs.PlayerInfo[userID].PC = newCode;
            return newCode;
        }
        string GetOrSetGuest(ulong userID)
        {
            if (playerPrefs.PlayerInfo[userID].GC != string.Empty) return playerPrefs.PlayerInfo[userID].GC;
            var newCode = Convert.ToString(UnityEngine.Random.Range(1, 9999)).PadLeft(4, '0');
            playerPrefs.PlayerInfo[userID].GC = newCode;
            return newCode;
        }
        WaitForEndOfFrame waitF = new WaitForEndOfFrame();

        IEnumerator RefreshAllDoorCloserDelays()
        {
            var allDoorClosers = BaseNetworkable.serverEntities.Where(p => p is DoorCloser);

            Debug.Log($"found {allDoorClosers.Count()} door closers");
            foreach(BaseNetworkable networkable in allDoorClosers)
            {
                DoorCloser doorCloser = networkable as DoorCloser;
                if (doorCloser == null)                
                    continue;
                
                BaseEntity parent = doorCloser.GetParentEntity();

                PlayerInfo playerInfo;
                if (playerPrefs.PlayerInfo.TryGetValue(parent.OwnerID, out playerInfo))
                {                    
                    if (parent.ShortPrefabName == "floor.ladder.hatch" || parent.ShortPrefabName == "floor.triangle.ladder.hatch")
                        doorCloser.delay = playerInfo.LHD;
                    else doorCloser.delay = playerInfo.DCD;

                    doorCloser.SendNetworkUpdate();

                    yield return waitF;
                }
            }
        }

        IEnumerator RefreshDoorClosers(ConsoleSystem.Arg arg)
        {
            var playerObjects = BaseNetworkable.serverEntities.Where(p => p is DoorCloser);// BaseNetworkable.serverEntities.Where(p =>  p != null && p.GetComponent<BaseEntity>() != null).Cast<BaseEntity>().Where(k =>  k.HasSlot(BaseEntity.Slot.UpperModifier) && k.GetSlot(BaseEntity.Slot.UpperModifier) == null && (k.OwnerID > 0uL)).GroupBy(c =>  c.OwnerID).ToDictionary(c =>  c.Key, c =>  c);
            yield return waitF;

            int counter = 0;
            foreach (var networkable in playerObjects)
            {
                DoorCloser doorCloser = networkable as DoorCloser;
                if (doorCloser == null)
                    continue;

                BaseEntity parent = doorCloser.GetParentEntity();

                if (!PlayerHasPerm(parent.OwnerID.ToString(), permissionDoorCloser, Player.IsAdmin(parent.OwnerID))) 
                    continue;

                if (!playerPrefs.PlayerInfo.ContainsKey(parent.OwnerID)) playerPrefs.PlayerInfo.Add(parent.OwnerID, new PlayerInfo());
                yield return waitF;

                CloserPlacing(parent.OwnerID, parent);
                counter++;
                yield return waitF;
            }
            if (arg != null) SendReply(arg, $"Refeshed '{counter}' doors with new placed DoorClosers");
            yield return null;
        }
        [ConsoleCommand("acl.refreshclosers")]
        void refreshClosers(ConsoleSystem.Arg arg)
        {
            if (!Initialized) return;
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
             
            _refreshDoorClosers = ServerMgr.Instance.StartCoroutine(RefreshDoorClosers(arg));
        }
        IEnumerator UpdateCloserDelays(BasePlayer player)
        {
            var playerObjects = BaseNetworkable.serverEntities.Where(p => p is DoorCloser);// p != null && p.GetComponent<BaseEntity>() != null).Cast<BaseEntity>().Where(k =>  k.GetSlot(BaseEntity.Slot.UpperModifier) != null && (k.OwnerID == player.userID)).ToList();
            int counter = 0;
            foreach (var networkable in playerObjects)
            {
                DoorCloser doorCloser = networkable as DoorCloser;
                if (doorCloser == null)
                    continue;

                BaseEntity parent = doorCloser.GetParentEntity();
                if (parent == null || parent.OwnerID != player.userID)
                    continue;

                if (parent.ShortPrefabName == "floor.ladder.hatch" || parent.ShortPrefabName == "floor.triangle.ladder.hatch")
                    doorCloser.delay = playerPrefs.PlayerInfo[player.userID].LHD;
                else doorCloser.delay = playerPrefs.PlayerInfo[player.userID].DCD;

                doorCloser.SendNetworkUpdate();
                counter++;
                yield return waitF;
            }
            PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("UpdatedDoors", this, player.UserIDString), counter) + "</color>");
            yield return null;
        }
        IEnumerator UpdateDoorCodes(BasePlayer player)
        {
            var playerObjects = BaseNetworkable.serverEntities.Where(p => p is CodeLock);//BaseNetworkable.serverEntities.Where(p =>  p != null && p.GetComponent<BaseEntity>() != null).Cast<BaseEntity>().Where(k =>  k.GetSlot(BaseEntity.Slot.Lock) != null && (k.OwnerID == player.userID)).ToList();
            int counter = 0;
            foreach (var networkable in playerObjects)
            {
                CodeLock codelock = networkable as CodeLock;
                if (codelock == null) 
                    continue;

                BaseEntity parent = codelock.GetParentEntity();
                if (parent == null || parent.OwnerID != player.userID)
                    continue;

                codelock.code = GetOrSetPin(player.userID);
                codelock.hasCode = true;
                codelock.SendNetworkUpdate();
                counter++;
                yield return waitF;
            }
            PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("UpdatedDoors", this, player.UserIDString), counter) + "</color>");
            yield return null;
        }
        IEnumerator UpdateGuestCodes(BasePlayer player)
        {
            var playerObjects = BaseNetworkable.serverEntities.Where(p => p is CodeLock);//BaseNetworkable.serverEntities.Where(p =>  p != null && p.GetComponent<BaseEntity>() != null).Cast<BaseEntity>().Where(k =>  k.GetSlot(BaseEntity.Slot.Lock) != null && (k.OwnerID == player.userID)).ToList();
            int counter = 0;
            foreach (var networkable in playerObjects)
            {
                CodeLock codelock = networkable as CodeLock;
                if (codelock == null)
                    continue;

                BaseEntity parent = codelock.GetParentEntity();
                if (parent == null || parent.OwnerID != player.userID)
                    continue;

                codelock.guestCode = GetOrSetPin(player.userID);
                codelock.hasGuestCode = true;
                codelock.SendNetworkUpdate();
                counter++;
                yield return waitF;
            }
            PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("UpdatedDoors", this, player.UserIDString), counter) + "</color>");
            yield return null;
        }
        void LockPlacing(BasePlayer player, BaseEntity entity)
        {
            var externalPlugins = Interface.CallHook("CanAutoLock", player);
            if (externalPlugins != null && !(player.IsAdmin && adminAutoRights))
            {
                PrintToChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NotLockedByMsg", this, player.UserIDString) + $"{(externalPlugins is string ? (string)externalPlugins : string.Empty)}</color>");
                return;
            }
            if (checkPlayerForRaidBlocked && NoEscape)
            {
                bool isBlocked = (bool)NoEscape?.CallHook("IsRaidBlocked", player);
                if (isBlocked && !(player.IsAdmin && adminAutoRights))
                {
                    PrintToChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NotLockedByBlock", this, player.UserIDString) + "</color>");
                    return;
                }
            }
            if (checkPlayerForCombatBlocked && NoEscape)
            {
                bool isBlocked = (bool)NoEscape?.CallHook("IsCombatBlocked", player);
                if (isBlocked && !(player.IsAdmin && adminAutoRights))
                {
                    PrintToChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NotLockedByBlock", this, player.UserIDString) + "</color>");
                    return;
                }
            }
            if (PlayerHasPerm(player.UserIDString, permissionNoLockNeed, player.IsAdmin))
            {
                var codelock = (CodeLock)GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab", new Vector3(), new Quaternion(), true);
                if (codelock == null) return;
                codelock.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.Lock));
                codelock.Spawn();
                entity.SetSlot(BaseEntity.Slot.Lock, codelock);
                if (PlayerHasPerm(player.UserIDString, permissionAutoLock, player.IsAdmin) && playerPrefs.PlayerInfo[player.userID].AL)
                {
                    codelock.code = GetOrSetPin(player.userID);
                    codelock.hasCode = true;
                    codelock.whitelistPlayers.Add(player.userID);
                    if (playerPrefs.PlayerInfo[player.userID].EGC)
                    {
                        codelock.guestCode = GetOrSetGuest(player.userID);
                        codelock.hasGuestCode = true;
                    }
                    codelock.SetFlag(BaseEntity.Flags.Locked, true, false);
                    Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", entity.transform.position);
                    if (notifyAuthCodeLock)
                    {
                        var code = codelock.code;
                        var guestCode = codelock.guestCode;
                        if (player.net.connection.info.GetBool("global.streamermode"))
                        {
                            code = "****";
                            guestCode = "****";
                        }
                        if (playerPrefs.PlayerInfo[player.userID].EGC) PrintToChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("CodeAuthBoth", this, player.UserIDString), code, guestCode + "</color>");
                        else PrintToChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("CodeAuth", this, player.UserIDString), code + "</color>");
                    }
                }
                return;
            }
            else
            {
                Item codelock = player.inventory.FindItemID(1159991980);
                if (codelock == null) return;
                var deploy = codelock.GetHeldEntity() as Deployer;
                deploy.DoDeploy_Slot(deploy.GetDeployable(), player.eyes.HeadRay(), entity.net.ID);
            }
        }
        void CloserPlacing(ulong userID, BaseEntity entity)
        {
            DoorCloser doorcloser = GameManager.server.CreateEntity(StringPool.Get(1831641807), new Vector3(), new Quaternion(), true) as DoorCloser;
            if (doorcloser == null) return;
            doorcloser.gameObject.Identity();
            if (entity.ShortPrefabName == "floor.ladder.hatch" || entity.ShortPrefabName == "floor.triangle.ladder.hatch")
                doorcloser.delay = playerPrefs.PlayerInfo[userID].LHD;
            else doorcloser.delay = playerPrefs.PlayerInfo[userID].DCD;
            doorcloser.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.UpperModifier));
            doorcloser.OnDeployed(entity, null);
            if (entity.ShortPrefabName == "floor.ladder.hatch")
                doorcloser.transform.localPosition = new Vector3(0.7f, 0f, 0f);
            else if (entity.ShortPrefabName == "floor.triangle.ladder.hatch")
                doorcloser.transform.localPosition = new Vector3(-0.8f, 0f, 0f);
            else if (entity.ShortPrefabName.StartsWith("door.double.hinged")) doorcloser.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            else if (entity.ShortPrefabName == "wall.frame.garagedoor") doorcloser.transform.localPosition = new Vector3(0f, 2.85f, 0f);
            doorcloser.Spawn();
            entity.SetSlot(BaseEntity.Slot.UpperModifier, doorcloser);
        }
        void OnEntityBuilt(Planner planner, GameObject obj)
        {
            if (!Initialized || planner == null || planner.GetOwnerPlayer() == null) return;
            BaseEntity entity = obj.ToBaseEntity();
            if (entity == null || entity.OwnerID == 0uL) return;
            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null || !playerPrefs.PlayerInfo.ContainsKey(player.userID)) return;
            if (entity is Door)
            {
                if ((entity as Door).canTakeLock && PlayerHasPerm(player.UserIDString, permissionDeployDoor, player.IsAdmin) && playerPrefs.PlayerInfo[player.userID].DLD) LockPlacing(player, entity);
                if (((entity as Door).canTakeCloser || entity.HasSlot(BaseEntity.Slot.UpperModifier)) && PlayerHasPerm(player.UserIDString, permissionDoorCloser, player.IsAdmin) && playerPrefs.PlayerInfo[player.userID].DDC) CloserPlacing(player.userID, entity);
                return;
            }
            else if (entity is BoxStorage && entity.HasSlot(BaseEntity.Slot.Lock))
            {
                if (PlayerHasPerm(player.UserIDString, permissionDeployBox, player.IsAdmin) && playerPrefs.PlayerInfo[player.userID].DLB) LockPlacing(player, entity);
                return;
            }
            else if (entity is Locker && entity.HasSlot(BaseEntity.Slot.Lock))
            {
                if (PlayerHasPerm(player.UserIDString, permissionDeployLocker, player.IsAdmin) && playerPrefs.PlayerInfo[player.userID].DLL) LockPlacing(player, entity);
                return;
            }
            else if (entity is BuildingPrivlidge && entity.HasSlot(BaseEntity.Slot.Lock))
            {
                if (PlayerHasPerm(player.UserIDString, permissionDeployCupboard, player.IsAdmin) && playerPrefs.PlayerInfo[player.userID].DLC) LockPlacing(player, entity);
                return;
            }
        }
        object CanPickupEntity(BasePlayer player, DoorCloser closer)
        {
            if ((denyCloserPickupByPlayer && !player.IsAdmin) || (denyCloserPickupByAdmin && player.IsAdmin)) return true;
            return null;
        }
        Boolean PlayerHasPerm(string UserIDString, string permissionName, bool isAdmin = false)
        {
            if (permission.UserHasPermission(UserIDString, permissionName) || permission.UserHasPermission(UserIDString, permissionAll)) return true;
            if (isAdmin && adminAutoRights) return true;
            return false;
        }
        Boolean PlayerHasUpdatePerm(string UserIDString, bool isAdmin = false)
        {
            if (permission.UserHasPermission(UserIDString, permissionAutoLock) || permission.UserHasPermission(UserIDString, permissionDoorCloser) || permission.UserHasPermission(UserIDString, permissionAll)) return true;
            if (isAdmin && adminAutoRights) return true;
            return false;
        }
        void cCodeLockCommand(ConsoleSystem.Arg arg)
        {
            if (Initialized && arg != null && arg.Connection != null && arg.Connection.player != null)
            {
                usedConsoleInput.Add(arg.Connection.userid);
                if (arg.Args != null) CodeLockCommand((BasePlayer)arg.Connection.player, codelockCommand, arg.Args);
                else CodeLockCommand((BasePlayer)arg.Connection.player, codelockCommand, new string[] { });
            }
        }
        void CodeLockCommand(BasePlayer player, string command, string[] args)
        {
            if (!Initialized) return;
            if (!player.IsAdmin || (player.IsAdmin && !adminAutoRights))
            {
                if (!permission.UserHasPermission(player.UserIDString, permissionAutoLock) && !permission.UserHasPermission(player.UserIDString, permissionDeployDoor) && !permission.UserHasPermission(player.UserIDString, permissionDeployBox) && !permission.UserHasPermission(player.UserIDString, permissionDeployLocker) && !permission.UserHasPermission(player.UserIDString, permissionDeployCupboard) && !permission.UserHasPermission(player.UserIDString, permissionDoorCloser) && !permission.UserHasPermission(player.UserIDString, permissionAll))
                {
                    PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NoAccess", this, player.UserIDString) + "</color>");
                    return;
                }
            }
            if (args.Length == 0)
            {
                var sb = new StringBuilder();
                if (!usedConsoleInput.Contains(player.userID)) sb.AppendLine($"<size=16><color={prefixColor}>{pluginPrefix}</color></size>");
                sb.AppendLine(lang.GetMessage("CommandUsage", this, player.UserIDString) + $"<color={colorCmdUsage}>/{codelockCommand} <option></color>" + (PlayerHasUpdatePerm(player.UserIDString, player.IsAdmin) ? $" | <color={colorCmdUsage}>/{codelockCommand} update | u</color>": ""));
                sb.AppendLine(lang.GetMessage("CommandToggle", this, player.UserIDString));
                if (PlayerHasPerm(player.UserIDString, permissionAutoLock, player.IsAdmin))
                {
                    sb.AppendLine(lang.GetMessage("StreamerMode", this, player.UserIDString));
                    sb.AppendLine($"<color={colorCmdUsage}>auto | a</color> - " + lang.GetMessage("CommandAutolock", this, player.UserIDString) + " " + (playerPrefs.PlayerInfo[player.userID].AL ? $"<color={colorON}>ON</color>": $"<color={colorOFF}>OFF</color>"));
                    sb.AppendLine($"<color={colorCmdUsage}>pin | p</color> - " + lang.GetMessage("CommandPinCode", this, player.UserIDString) + $" <color={colorCmdUsage}>{(player.net.connection.info.GetBool("global.streamermode ") ? " * ***" : GetOrSetPin(player.userID))}</color>");
                    sb.AppendLine($"<color={colorCmdUsage}>guest | g</color> - " + lang.GetMessage("CommandGuest", this, player.UserIDString) + " " + (playerPrefs.PlayerInfo[player.userID].EGC ? $"<color={colorON}>ON</color>": $"<color={colorOFF}>OFF</color>"));
                    if (playerPrefs.PlayerInfo[player.userID].EGC) sb.AppendLine($"<color={colorCmdUsage}>guestpin | gp</color> - " + lang.GetMessage("CommandGuestCode", this, player.UserIDString) + $" <color={colorCmdUsage}>{(player.net.connection.info.GetBool("global.streamermode") ? " * ***" : GetOrSetGuest(player.userID))}</color>");
                }
                if (PlayerHasPerm(player.UserIDString, permissionDoorCloser, player.IsAdmin))
                {
                    sb.AppendLine($"<color={colorCmdUsage}>doorcloser | dc</color> - " + lang.GetMessage("CommandDoorCloser", this, player.UserIDString) + " " + (playerPrefs.PlayerInfo[player.userID].DDC ? $"<color={colorON}>ON</color>": $"<color={colorOFF}>OFF</color>"));
                    sb.AppendLine($"<color={colorCmdUsage}>closedelay | cd</color> - " + lang.GetMessage("CommandCloseDelay", this, player.UserIDString) + $" <color={colorCmdUsage}>{playerPrefs.PlayerInfo[player.userID].DCD}s</color>");
                    sb.AppendLine($"<color={colorCmdUsage}>hatchdelay | hd</color> - " + lang.GetMessage("CommandHatchDelay", this, player.UserIDString) + $" <color={colorCmdUsage}>{playerPrefs.PlayerInfo[player.userID].LHD}s</color>");
                }
                if (PlayerHasPerm(player.UserIDString, permissionDeployDoor, player.IsAdmin)) sb.AppendLine($"<color={colorCmdUsage}>door | d</color> - " + lang.GetMessage("CommandDeployDoor", this, player.UserIDString) + " " + (playerPrefs.PlayerInfo[player.userID].DLD ? $"<color={colorON}>ON</color>": $"<color={colorOFF}>OFF</color>"));
                if (PlayerHasPerm(player.UserIDString, permissionDeployBox, player.IsAdmin)) sb.AppendLine($"<color={colorCmdUsage}>box | b</color> - " + lang.GetMessage("CommandDeployBox", this, player.UserIDString) + " " + (playerPrefs.PlayerInfo[player.userID].DLB ? $"<color={colorON}>ON</color>": $"<color={colorOFF}>OFF</color>"));
                if (PlayerHasPerm(player.UserIDString, permissionDeployLocker, player.IsAdmin)) sb.AppendLine($"<color={colorCmdUsage}>locker | l</color> - " + lang.GetMessage("CommandDeployLocker", this, player.UserIDString) + " " + (playerPrefs.PlayerInfo[player.userID].DLL ? $"<color={colorON}>ON</color>": $"<color={colorOFF}>OFF</color>"));
                if (PlayerHasPerm(player.UserIDString, permissionDeployCupboard, player.IsAdmin)) sb.AppendLine($"<color={colorCmdUsage}>cup | c</color> - " + lang.GetMessage("CommandDeployCupboard", this, player.UserIDString) + " " + (playerPrefs.PlayerInfo[player.userID].DLC ? $"<color={colorON}>ON</color>": $"<color={colorOFF}>OFF</color>"));
                string openText = $"<color={colorTextMsg}>";
                string closeText = "</color>";
                string[] parts = sb.ToString().Split(new char[] {
                    '\n'
                },
                StringSplitOptions.RemoveEmptyEntries);
                sb = new StringBuilder();
                foreach (var part in parts)
                {
                    if ((sb.ToString().TrimEnd().Length + part.Length + openText.Length + closeText.Length) > 1050)
                    {
                        PrintChat(player, openText + sb.ToString().TrimEnd() + closeText, usedConsoleInput.Contains(player.userID) ? true : false);
                        sb.Clear();
                    }
                    sb.AppendLine(part);
                }
                PrintChat(player, openText + sb.ToString().TrimEnd() + closeText);
                return;
            }
            switch (args[0].ToLower())
            {
                case "auto":
                case "a":
                    if (PlayerHasPerm(player.UserIDString, permissionAutoLock, player.IsAdmin))
                    {
                        playerPrefs.PlayerInfo[player.userID].AL = !playerPrefs.PlayerInfo[player.userID].AL;
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + (playerPrefs.PlayerInfo[player.userID].AL ? lang.GetMessage("AutoLockEnabled", this, player.UserIDString) : lang.GetMessage("AutoLockDisabled", this, player.UserIDString)) + "</color>");
                    }
                    else goto
              case "noaccess";
                    break;
                case "door":
                case "d":
                    if (PlayerHasPerm(player.UserIDString, permissionDeployDoor, player.IsAdmin))
                    {
                        playerPrefs.PlayerInfo[player.userID].DLD = !playerPrefs.PlayerInfo[player.userID].DLD;
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + (playerPrefs.PlayerInfo[player.userID].DLD ? lang.GetMessage("DeployLockDoorEnabled", this, player.UserIDString) : lang.GetMessage("DeployLockDoorDisabled", this, player.UserIDString)) + "</color>");
                    }
                    else goto
              case "noaccess";
                    break;
                case "doorcloser":
                case "dc":
                    if (PlayerHasPerm(player.UserIDString, permissionDoorCloser, player.IsAdmin))
                    {
                        playerPrefs.PlayerInfo[player.userID].DDC = !playerPrefs.PlayerInfo[player.userID].DDC;
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + (playerPrefs.PlayerInfo[player.userID].DDC ? lang.GetMessage("DeployDoorCloserEnabled", this, player.UserIDString) : lang.GetMessage("DeployDoorCloserDisabled", this, player.UserIDString)) + "</color>");
                    }
                    else goto
              case "noaccess";
                    break;
                case "closedelay":
                case "cd":
                    if (PlayerHasPerm(player.UserIDString, permissionDoorCloser, player.IsAdmin))
                    {
                        float delay;
                        if (args.Length != 2 || float.TryParse(args[1], out delay) == false)
                        {
                            PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + string.Format(lang.GetMessage("CommandCloseDelayHelp", this, player.UserIDString), colorCmdUsage, codelockCommand, doorCloserMinDelay, doorCloserMaxDelay));
                            return;
                        }
                        if (delay < doorCloserMinDelay) delay = doorCloserMinDelay;
                        if (delay > doorCloserMaxDelay) delay = doorCloserMaxDelay;
                        playerPrefs.PlayerInfo[player.userID].DCD = delay;
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("CommandCloseDelaySetTo", this, player.UserIDString), delay) + "</color>");
                        if (autoUpdateChangedDelays)
                        {
                            _updateCloserDelays = ServerMgr.Instance.StartCoroutine(UpdateCloserDelays(player));
                        }
                    }
                    else goto
              case "noaccess";
                    break;
                case "hatchdelay":
                case "hd":
                    if (PlayerHasPerm(player.UserIDString, permissionDoorCloser, player.IsAdmin))
                    {
                        float delay;
                        if (args.Length != 2 || float.TryParse(args[1], out delay) == false)
                        {
                            PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + string.Format(lang.GetMessage("CommandHatchDelayHelp", this, player.UserIDString), colorCmdUsage, codelockCommand, ladderHatchMinDelay, ladderHatchMaxDelay));
                            return;
                        }
                        if (delay < ladderHatchMinDelay) delay = ladderHatchMinDelay;
                        if (delay > ladderHatchMaxDelay) delay = ladderHatchMaxDelay;
                        playerPrefs.PlayerInfo[player.userID].LHD = delay;
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("CommandHatchDelaySetTo", this, player.UserIDString), delay) + "</color>");
                        if (autoUpdateChangedDelays)
                        {
                            _updateCloserDelays = ServerMgr.Instance.StartCoroutine(UpdateCloserDelays(player));
                        }
                    }
                    else goto
              case "noaccess";
                    break;
                case "box":
                case "b":
                    if (PlayerHasPerm(player.UserIDString, permissionDeployBox, player.IsAdmin))
                    {
                        playerPrefs.PlayerInfo[player.userID].DLB = !playerPrefs.PlayerInfo[player.userID].DLB;
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + (playerPrefs.PlayerInfo[player.userID].DLB ? lang.GetMessage("DeployLockBoxEnabled", this, player.UserIDString) : lang.GetMessage("DeployLockBoxDisabled", this, player.UserIDString)) + "</color>");
                    }
                    else goto
              case "noaccess";
                    break;
                case "locker":
                case "l":
                    if (PlayerHasPerm(player.UserIDString, permissionDeployLocker, player.IsAdmin))
                    {
                        playerPrefs.PlayerInfo[player.userID].DLL = !playerPrefs.PlayerInfo[player.userID].DLL;
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + (playerPrefs.PlayerInfo[player.userID].DLL ? lang.GetMessage("DeployLockLockerEnabled", this, player.UserIDString) : lang.GetMessage("DeployLockLockerDisabled", this, player.UserIDString)) + "</color>");
                    }
                    else goto
              case "noaccess";
                    break;
                case "cup":
                case "c":
                    if (PlayerHasPerm(player.UserIDString, permissionDeployCupboard, player.IsAdmin))
                    {
                        playerPrefs.PlayerInfo[player.userID].DLC = !playerPrefs.PlayerInfo[player.userID].DLC;
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + (playerPrefs.PlayerInfo[player.userID].DLC ? lang.GetMessage("DeployLockCupEnabled", this, player.UserIDString) : lang.GetMessage("DeployLockCupDisabled", this, player.UserIDString)) + "</color>");
                    }
                    else goto
              case "noaccess";
                    break;
                case "pin":
                case "p":
                    if (PlayerHasPerm(player.UserIDString, permissionAutoLock, player.IsAdmin))
                    {
                        int pinChk;
                        if (args.Length != 2 || int.TryParse(args[1], out pinChk) == false || args[1].Length != 4 || args[1].Length < 4)
                        {
                            PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + string.Format(lang.GetMessage("CommandPinCodeHelp", this, player.UserIDString), colorCmdUsage, codelockCommand));
                            return;
                        }
                        playerPrefs.PlayerInfo[player.userID].PC = args[1].ToString().PadLeft(4, '0');
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("CommandPinCodeSetTo", this, player.UserIDString)) + $" <color={colorCmdUsage}>{args[1]}</color></color>");
                    }
                    else goto
              case "noaccess";
                    break;
                case "guest":
                case "g":
                    if (PlayerHasPerm(player.UserIDString, permissionAutoLock, player.IsAdmin))
                    {
                        playerPrefs.PlayerInfo[player.userID].EGC = !playerPrefs.PlayerInfo[player.userID].EGC;
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + (playerPrefs.PlayerInfo[player.userID].EGC ? lang.GetMessage("UseGuestCodeEnabled", this, player.UserIDString) : lang.GetMessage("UseGuestCodeDisabled", this, player.UserIDString)) + "</color>");
                    }
                    else goto
              case "noaccess";
                    break;
                case "guestpin":
                case "gp":
                    if (PlayerHasPerm(player.UserIDString, permissionAutoLock, player.IsAdmin))
                    {
                        int guestChk;
                        if (args.Length != 2 || int.TryParse(args[1], out guestChk) == false || args[1].Length != 4 || args[1].Length < 4)
                        {
                            PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + string.Format(lang.GetMessage("CommandGuestCodeHelp", this, player.UserIDString), colorCmdUsage, codelockCommand));
                            return;
                        }
                        playerPrefs.PlayerInfo[player.userID].GC = args[1].ToString().PadLeft(4, '0');
                        PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("CommandGuestCodeSetTo", this, player.UserIDString)) + $" <color={colorCmdUsage}>{args[1]}</color></color>");
                    }
                    else goto
              case "noaccess";
                    break;
                case "update":
                case "u":
                    if (args.Length != 2)
                    {
                        var sbhelp = new StringBuilder();
                        sbhelp.AppendLine(lang.GetMessage("CommandUsage", this, player.UserIDString) + $"<color={colorCmdUsage}>/{codelockCommand} update | u <option></color>");
                        if (PlayerHasPerm(player.UserIDString, permissionAutoLock, player.IsAdmin))
                        {
                            sbhelp.AppendLine($"<color={colorCmdUsage}>pin | p</color> - " + lang.GetMessage("UpdatePin", this, player.UserIDString));
                            sbhelp.AppendLine($"<color={colorCmdUsage}>guestpin | gp</color> - " + lang.GetMessage("UpdateGuestPin", this, player.UserIDString));
                        }
                        if (PlayerHasPerm(player.UserIDString, permissionDoorCloser, player.IsAdmin)) sbhelp.AppendLine($"<color={colorCmdUsage}>closedelay | cd</color> - " + lang.GetMessage("UpdateCloseDelay", this, player.UserIDString));
                        PrintChat(player, $"<color={colorTextMsg}>" + sbhelp.ToString() + "</color>");
                    }
                    else if (args.Length >= 2)
                    {
                        switch (args[1])
                        {
                            case "closedelay":
                            case "cd":
                                if (PlayerHasPerm(player.UserIDString, permissionDoorCloser, player.IsAdmin))
                                {                                     
                                    _updateCloserDelays = ServerMgr.Instance.StartCoroutine(UpdateCloserDelays(player));
                                }
                                else goto
                          case "noaccess";
                                break;
                            case "pin":
                            case "p":
                                if (PlayerHasPerm(player.UserIDString, permissionAutoLock, player.IsAdmin))
                                {
                                    _updateDoorCodes = ServerMgr.Instance.StartCoroutine(UpdateDoorCodes(player));
                                }
                                else goto
                          case "noaccess";
                                break;
                            case "guestpin":
                            case "gp":
                                if (PlayerHasPerm(player.UserIDString, permissionAutoLock, player.IsAdmin))
                                {
                                    _updateGuestCodes = ServerMgr.Instance.StartCoroutine(UpdateGuestCodes(player));
                                }
                                else goto
                          case "noaccess";
                                break;
                            case "noaccess":
                                PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NoAccess", this, player.UserIDString) + "</color>");
                                break;
                            default:
                                PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("NotSupported", this, player.UserIDString), args[0]) + "</color>");
                                break;
                        }
                    }
                    break;
                case "noaccess":
                    PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + lang.GetMessage("NoAccess", this, player.UserIDString) + "</color>");
                    break;
                default:
                    PrintChat(player, string.Format(prefixFormat, prefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + string.Format(lang.GetMessage("NotSupported", this, player.UserIDString), args[0]) + "</color>");
                    break;
            }
        }
        void PrintChat(BasePlayer player, string message, bool keepConsole = false)
        {
            if (usedConsoleInput.Contains(player.userID)) player.ConsoleMessage(message);
            else player.ChatMessage(message);
            if (!keepConsole) usedConsoleInput.Remove(player.userID);
        }
    }
}