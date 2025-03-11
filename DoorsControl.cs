using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Text;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DoorsControl", "RusskiIvan", "1.0.4")]
    [Description("DoorsControl")]
    public class DoorsControl : RustPlugin
    {
        [PluginReference] private Plugin Clans;

        #region Variables

        private StoredData _data;
        private ConfigData _config;

        private readonly FieldInfo _serverInput = typeof(BasePlayer).GetField("serverInput",
            (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

        private readonly Vector3 _eyesAdjust = new Vector3(0f, 1.5f, 0f);
        private bool _dataLoaded;

        private readonly FieldInfo _hasCode = typeof(CodeLock).GetField("hasCode",
            (BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));

        #endregion

        #region Configuration

        private class ConfigData
        {
            [JsonProperty("Команда для замков")] public string codelockCommand { get; set; }
            [JsonProperty("Команда для дверей ")] public string doorsCommand { get; set; }
            [JsonProperty("Привилегии")] public Permissions permissions { get; set; }
            [JsonProperty("Настройки")] public Settings settings { get; set; }
        }

        private class Permissions
        {
            [JsonProperty("Привилегия для дверей")]
            public string permissionDeployDoor { get; set; }

            [JsonProperty("Привилегия для ящиков")]
            public string permissionDeployBox { get; set; }

            [JsonProperty("Привилегия для шкафов с одеждой")]
            public string permissionDeployLocker { get; set; }

            [JsonProperty("Привилегия для шкафа")] public string permissionDeployCupboard { get; set; }

            [JsonProperty("Привилегия для автозакрытия замка")]
            public string permissionAutoLock { get; set; }

            [JsonProperty("Привилегия для установки замка без замка :)")]
            public string permissionNoLockNeed { get; set; }

            [JsonProperty("Привилегия для автозакрывания двери")]
            public string permissionAutoCloseDoor { get; set; }

            [JsonProperty("Привилегия для умного дома")]
            public string permissionSmartHome { get; set; }
        }

        private class Settings
        {
            [JsonProperty("Автозакрытие замка")] public bool AutoLock { get; set; }

            [JsonProperty("Авто установка на двери")]
            public bool DeployDoor { get; set; }

            [JsonProperty("Авто установка на ящики")]
            public bool DeployBox { get; set; }

            [JsonProperty("Авто установка на шкафы с одеждой")]
            public bool DeployLocker { get; set; }

            [JsonProperty("Авто установка на шкаф")]
            public bool DeployCupboard { get; set; }

            [JsonProperty("Задержка закрытия двери")]
            public float defaultDelay { get; set; }

            [JsonProperty("Автозакрытие дверей")] public bool autoDoor { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                codelockCommand = "code",
                doorsCommand = "ad",
                permissions = new Permissions
                {
                    permissionDeployDoor = Name + ".deploydoor",
                    permissionDeployBox = Name + ".deploybox",
                    permissionDeployLocker = Name + ".deploylocker",
                    permissionDeployCupboard = Name + ".deploycup",
                    permissionAutoLock = Name + ".autolock",
                    permissionNoLockNeed = Name + ".nolockneed",
                    permissionAutoCloseDoor = Name + ".autoclose",
                    permissionSmartHome = Name + ".smarthome",

                },
                settings = new Settings
                {
                    AutoLock = true,
                    DeployDoor = true,
                    DeployBox = true,
                    DeployLocker = true,
                    DeployCupboard = true,
                    autoDoor = true,
                    defaultDelay = 5f
                }
            };
            SaveConfig(config);
            PrintWarning("Creating default a configuration file ...");
        }

        private void LoadConfigVariables() => _config = Config.ReadObject<ConfigData>();
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

        #endregion

        #region Oxide
        
        private void Loaded()
        {
            LoadConfigVariables();
            cmd.AddChatCommand(_config.codelockCommand, this, "CodeLockCommand");
            cmd.AddChatCommand(_config.doorsCommand, this, "AutoDoorCommand");
        }

        private void OnServerInitialized()
        {
            if (!_dataLoaded) LoadData();
            RegisterPermissions();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!_data.PlayerInfo.ContainsKey(player.userID)) AddNewPlayer(player);
            }
        }

        private void OnPlayerConnected(BasePlayer player)    
        {
            if (!_data.PlayerInfo.ContainsKey(player.userID)) AddNewPlayer(player);
        }

        private void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, _config.permissions.permissionAutoCloseDoor))
            {
                //Puts(permission.UserHasPermission(player.UserIDString, _config.permissions.permissionAutoCloseDoor).ToString());
                return;
            }
            if (door.GetComponentInChildren<DoorCloser>() != null) return;
            if (_data.DoorsList.Contains(door.net.ID)) return;
            var time = 0f;
            if (_data.PlayerInfo.ContainsKey(player.userID))
            {
                if (!_data.PlayerInfo[player.userID].AutoDoor) return;
                time = _data.PlayerInfo[player.userID].DefaultDelay;
            }
            else
            {
                if (!_config.settings.AutoLock) return;
                time = _config.settings.defaultDelay;
            }

            if (time == 0f) return;
            timer.Once(time, () => CloseDoor(door));

        }

        private void OnItemDeployed(Deployer deployer, BaseEntity entity)
        {
            if (deployer.GetOwnerPlayer() == null || deployer.GetModDeployable() == null ||
                deployer.GetModDeployable().name == "doorcloser.item" || !entity.HasSlot(BaseEntity.Slot.Lock) ||
                !(entity.GetSlot(BaseEntity.Slot.Lock) is CodeLock)) return;
            var owner = deployer.GetOwnerPlayer();
            if (!permission.UserHasPermission(owner.UserIDString, _config.permissions.permissionAutoLock) ||
                !_data.PlayerInfo[owner.userID].AutoLock) return;
            var codelock = entity.GetSlot(BaseEntity.Slot.Lock) as CodeLock;
            if (codelock == null) return;
            codelock.code = Convert.ToString(_data.PlayerInfo[owner.userID].Password != 0
                ? _data.PlayerInfo[owner.userID].Password
                : UnityEngine.Random.Range(1234, 9876));
            _hasCode.SetValue(codelock, true);
            codelock.whitelistPlayers.Add(owner.userID);
            codelock.SetFlag(BaseEntity.Flags.Locked, true);
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab",
                entity.transform.position);
            var code = codelock.code;
            if (owner.net.connection.info.GetBool("global.streamermode")) code = "****";
            SendReply(owner, string.Format(Msg("CodeAuth", owner), code)); //Изменено
        }

        private void OnEntityBuilt(Planner planner, GameObject obj)
        {
            if (planner == null || planner.GetOwnerPlayer() == null ||
                obj.GetComponent<BaseEntity>() == null || obj.GetComponent<BaseEntity>().OwnerID == 0) return;
            var entity = obj.GetComponent<BaseEntity>();
            var player = planner.GetOwnerPlayer();
            if (player == null || !_data.PlayerInfo.ContainsKey(player.userID)) return;
            if (entity is Door && (entity as Door).canTakeLock)
            {
                if (permission.UserHasPermission(player.UserIDString, _config.permissions.permissionDeployDoor) &&
                    _data.PlayerInfo[player.userID].DeployDoor) LockPlacing(player, entity);
            }
            else if (entity is BoxStorage && entity.HasSlot(BaseEntity.Slot.Lock))
            {
                if (permission.UserHasPermission(player.UserIDString, _config.permissions.permissionDeployBox) &&
                    _data.PlayerInfo[player.userID].DeployBox) LockPlacing(player, entity);
            }
            else if (entity is Locker && entity.HasSlot(BaseEntity.Slot.Lock))
            {
                if (permission.UserHasPermission(player.UserIDString, _config.permissions.permissionDeployLocker) &&
                    _data.PlayerInfo[player.userID].DeployLocker) LockPlacing(player, entity);
            }
            else if (entity is BuildingPrivlidge && entity.HasSlot(BaseEntity.Slot.Lock))
            {
                if (permission.UserHasPermission(player.UserIDString, _config.permissions.permissionDeployCupboard) &&
                    _data.PlayerInfo[player.userID].DeployCupboard) LockPlacing(player, entity);
            }
        }

        private void OnNewSave()
        {
            LoadData();
            _data.DoorsList.Clear();
            SaveData();
        }

        private void OnServerSave() => SaveData();

        private void Unload() => SaveData();

        #endregion

        #region Functions
        bool IsClanMember(ulong playerid = 294912, ulong targetID = 0) => (bool)(Clans?.Call("HasFriend", playerid, targetID) ?? false);

        private void CloseDoor(BaseEntity door)
        {
            if (door == null || !door.IsOpen()) return;
            door.SetFlag(BaseEntity.Flags.Open, false);
            door.SendNetworkUpdateImmediate();
        }

        private static BaseEntity DoRay(Vector3 pos, Vector3 aim)
        {
            var hits = Physics.RaycastAll(pos, aim);
            var distance = 3f;
            BaseEntity target = null;
            foreach (var hit in hits)
            {
                if (!(hit.distance < distance)) continue;
                distance = hit.distance;
                target = hit.GetEntity();
            }

            return target;
        }
    
        private void AutoDoorCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, _config.permissions.permissionAutoCloseDoor))
            {
                SendReply(player,
                    Msg("NoAccess", player));
                return;
            }

            SetDoor(player);
        }

        private void SetDoor(BasePlayer player)
        {
            var input = _serverInput.GetValue(player) as InputState;
            if (input == null) return;
            var currentRot = Quaternion.Euler(input.current.aimAngles) * Vector3.forward;
            var entity = DoRay(player.transform.position + _eyesAdjust, currentRot);
            var door = entity as Door;
            if (door == null)
            {
                if (!_data.PlayerInfo.ContainsKey(player.userID)) AddNewPlayer(player);
                _data.PlayerInfo[player.userID].AutoDoor = !_data.PlayerInfo[player.userID].AutoDoor;
                SaveData();
                SendReply(player,
                    _data.PlayerInfo[player.userID].AutoDoor
                        ? Msg("AllAutoCloseEnabled", player)
                        : Msg("AllAutoCloseDisabled", player));
                return;
            }

            SwitchDoor(player, door);
        }

        private void SwitchDoor(BasePlayer player, BaseNetworkable door)
        {
            if (door == null || door.net == null) return;
            if (!permission.UserHasPermission(player.UserIDString, _config.permissions.permissionAutoCloseDoor)) return;

            if (_data.DoorsList.Contains(door.net.ID))
            {
                _data.DoorsList.Remove(door.net.ID);
                SendReply(player,
                    Msg("AutoCloseEnabled", player));
            }
            else
            {
                _data.DoorsList.Add(door.net.ID);
                SendReply(player,
                    Msg("AutoCloseDisabled", player));
            }

            SaveData();
        }

        private void RegisterPermissions()
        {
            if (!permission.PermissionExists(_config.permissions.permissionDeployDoor))
                permission.RegisterPermission(_config.permissions.permissionDeployDoor, this);
            if (!permission.PermissionExists(_config.permissions.permissionAutoLock))
                permission.RegisterPermission(_config.permissions.permissionAutoLock, this);
            if (!permission.PermissionExists(_config.permissions.permissionDeployBox))
                permission.RegisterPermission(_config.permissions.permissionDeployBox, this);
            if (!permission.PermissionExists(_config.permissions.permissionDeployLocker))
                permission.RegisterPermission(_config.permissions.permissionDeployLocker, this);
            if (!permission.PermissionExists(_config.permissions.permissionDeployCupboard))
                permission.RegisterPermission(_config.permissions.permissionDeployCupboard, this);
            if (!permission.PermissionExists(_config.permissions.permissionAutoCloseDoor))
                permission.RegisterPermission(_config.permissions.permissionAutoCloseDoor, this);
            if (!permission.PermissionExists(_config.permissions.permissionNoLockNeed))
                permission.RegisterPermission(_config.permissions.permissionNoLockNeed, this);
            if (!permission.PermissionExists(_config.permissions.permissionSmartHome))
                permission.RegisterPermission(_config.permissions.permissionSmartHome, this);
        }

        private void AddNewPlayer(BasePlayer player)
        {
            if (_data.PlayerInfo.ContainsKey(player.userID)) return;

            var info = new PlayerInfo
            {
                AutoLock = _config.settings.AutoLock,
                DeployDoor = _config.settings.DeployDoor,
                DeployBox = _config.settings.DeployBox,
                DeployLocker = _config.settings.DeployLocker,
                DeployCupboard = _config.settings.DeployCupboard,
                AutoDoor = _config.settings.autoDoor,
                DefaultDelay = _config.settings.defaultDelay,
                Password = UnityEngine.Random.Range(1000, 9999)
            };
            _data.PlayerInfo.Add(player.userID, info);
            SaveData();
        }

        private void SetPlayerData(BasePlayer player, int Code, bool AutoLock = true, bool DeployDoor = true,
            bool DeployBox = true, bool DeployLocker = true, bool DeployCupboard = true, bool autoDoor = true,
            float defaultDelay = 5f)
        {
            if (player == null) return;
            if (_data.PlayerInfo.ContainsKey(player.userID)) _data.PlayerInfo.Remove(player.userID);
            var info = new PlayerInfo
            {
                AutoLock = AutoLock,
                DeployDoor = DeployDoor,
                DeployBox = DeployBox,
                DeployLocker = DeployLocker,
                DeployCupboard = DeployCupboard,
                AutoDoor = autoDoor,
                DefaultDelay = defaultDelay,
                Password = Code == 0 ? UnityEngine.Random.Range(1000, 9999) : Code
            };
            _data.PlayerInfo.Add(player.userID, info);
            SaveData();
        }

        private PlayerInfo GetPlayerData(BasePlayer player)
        {
            if (player == null) return null;
            if (!_data.PlayerInfo.ContainsKey(player.userID)) AddNewPlayer(player);
            return _data.PlayerInfo[player.userID];
        }

        private void LockPlacing(BasePlayer player, BaseEntity entity)
        {
            if (!permission.UserHasPermission(player.UserIDString, _config.permissions.permissionNoLockNeed) &&
                player.inventory.Take(null, 1159991980, 1) == 0)
            {
                return;
            }

            var codeLock = GameManager.server.CreateEntity("assets/prefabs/locks/keypad/lock.code.prefab",
                new Vector3(), new Quaternion(), true) as CodeLock;
            if (codeLock == null) return;
            codeLock.gameObject.Identity();
            codeLock.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.Lock));
            codeLock.OnDeployed(entity, player);
            codeLock.Spawn();
            entity.SetSlot(BaseEntity.Slot.Lock, codeLock);
            if (!permission.UserHasPermission(player.UserIDString, _config.permissions.permissionAutoLock) ||
                !_data.PlayerInfo[player.userID].AutoLock) return;
            codeLock.code = Convert.ToString(_data.PlayerInfo[player.userID].Password != 0
                ? _data.PlayerInfo[player.userID].Password
                : UnityEngine.Random.Range(1234, 9876));
            _hasCode.SetValue(codeLock, true);
            codeLock.whitelistPlayers.Add(player.userID);
            var clanList = Clans?.Call<List<string>>("GetClanMembers", player.userID);
            if (clanList != null && clanList.Count > 0)
            {
                foreach (var clanuser in clanList)
                {
                    codeLock.whitelistPlayers.Add(ulong.Parse(clanuser));
                } 
            }
            else
            {
                codeLock.whitelistPlayers.Add(player.userID);
            }
            if (RelationshipManager.ServerInstance.FindPlayersTeam(player.userID) != null)
            {
                foreach (var playerID in player.Team.members)
                {
                    codeLock.whitelistPlayers.Add(playerID);

                }
            }
            codeLock.SetFlag(BaseEntity.Flags.Locked, true);
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab",
                entity.transform.position);
            var code = codeLock.code;
            if (player.net.connection.info.GetBool("global.streamermode")) code = "****";
            SendReply(player, string.Format(Msg("CodeAuth", player), code));
        }

        private void CodeLockCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, _config.permissions.permissionAutoLock) &&
                !permission.UserHasPermission(player.UserIDString, _config.permissions.permissionDeployDoor) &&
                !permission.UserHasPermission(player.UserIDString, _config.permissions.permissionDeployBox) &&
                !permission.UserHasPermission(player.UserIDString, _config.permissions.permissionDeployLocker) &&
                !permission.UserHasPermission(player.UserIDString, _config.permissions.permissionDeployCupboard))
            {
                SendReply(player,
                    Msg("NoAccess", player));
                return;
            }

            if (args.Length == 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine(string.Format(Msg("CommandUsage", player), _config.codelockCommand));
                if (permission.UserHasPermission(player.UserIDString, _config.permissions.permissionAutoLock))
                    sb.AppendLine(String.Format(Msg("CommandAutolock", player), "auto", _data.PlayerInfo[player.userID].AutoLock
                                      ? "ON"
                                      : "OFF"));
                var pin = Convert.ToString(_data.PlayerInfo[player.userID].Password);
                if (player.net.connection.info.GetBool("global.streamermode")) pin = "****";
                sb.AppendLine(String.Format(Msg("CommandPinCode", player),"pin", _data.PlayerInfo[player.userID].Password > 0
                    ? pin
                    : Msg("CommandPinCodeNotSet", player)));
                SendReply(player, sb.ToString().TrimEnd());
                return;
            }

            switch (args[0].ToLower())
            {
                case "auto":
                    _data.PlayerInfo[player.userID].AutoLock = !_data.PlayerInfo[player.userID].AutoLock;
                        SendReply(player,
                            (_data.PlayerInfo[player.userID].AutoLock
                                ? Msg("AutoLockEnabled", player)
                                : Msg("AutoLockDisabled", player)));//Изменено
                    break;
               case "pin":
                    int pin;
                    if (args.Length != 2) goto case "noaccess";
                    //Puts(int.TryParse(args[1], out pin).ToString());
                    if (int.TryParse(args[1], out pin) == false || int.Parse(args[1]) > 9999 || int.Parse(args[1]) < 1000) goto case "badargument";
                    _data.PlayerInfo[player.userID].Password = pin;
                    SendReply(player, string.Format(Msg("CommandPinCodeSetTo", player), pin));
                    break;
                case "noaccess":
                    SendReply(player, string.Format(Msg("CommandPinCodeHelp", player), _config.codelockCommand));
                    break;
                 case "badargument":
                     SendReply(player, string.Format(Msg("BadFormatPin", player), args[1]));
                    break;
                    
                default:
                    SendReply(player, string.Format(Msg("NotSupported", player), args[0]));
                    break;
            }
        }
        
        #endregion

        #region Data
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _data);

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (_data == null) _data = new StoredData();
            _dataLoaded = true;
        }
        class StoredData
        {
            public Dictionary<ulong, PlayerInfo> PlayerInfo = new Dictionary<ulong, PlayerInfo>();
            public List<uint> DoorsList = new List<uint>();
        }
        
        class PlayerInfo
        {
            public bool AutoLock;
            public bool DeployDoor;
            public bool DeployBox;
            public bool DeployLocker;
            public bool DeployCupboard;
            public int Password;
            public bool AutoDoor;
            public float DefaultDelay;
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(
                new Dictionary<string, string>
                {
                    {"AllAutoCloseEnabled", "<color=#00FFFF></color><color=#FFFFFF> Automatic doors closing for you is enabled</color>"},
                    {"AllAutoCloseDisabled", "<color=#00FFFF></color><color=#FFFFFF> Automatic doors closing for you is disabled</color>"},
                    {"BadFormatPin", "<color=#00FFFF></color><color=#FFFFFF> Error syntax pin: <color=#FF0000>{0}</color></color>"},
                    {"AutoCloseEnabled", "<color=#00FFFF></color><color=#FFFFFF> Automatic closing of <color=#00FF00>THIS DOOR</color> for all authorized players is enabled</color>"},
                    {"AutoCloseDisabled", "<color=#00FFFF></color><color=#FFFFFF> Automatic closing of <color=#FF0000>THIS DOOR</color> for all authorized players is disabled</color>"},
                    {"AutoLockEnabled", "<color=#00FFFF></color><color=#FFFFFF> CodeLock automation (secure and lock) enabled</color>"},
                    {"AutoLockDisabled", "<color=#00FFFF></color><color=#FFFFFF> CodeLock automation disabled</color>"},
                    {"DeployLockDoorEnabled", "<color=#00FFFF></color><color=#FFFFFF> Doors will include codelocks on deploy</color>"},
                    {"DeployLockDoorDisabled", "<color=#00FFFF></color><color=#FFFFFF> Doors will not include codelocks on deploy</color>"},
                    {"DeployLockBoxEnabled", "<color=#00FFFF></color><color=#FFFFFF> Boxes will include codelocks on deploy</color>"},
                    {"DeployLockBoxDisabled", "<color=#00FFFF></color><color=#FFFFFF> Boxes will not include codelocks on deploy</color>"},
                    {"DeployLockLockerEnabled", "<color=#00FFFF></color><color=#FFFFFF> Locker will include codelocks on deploy</color>"},
                    {"DeployLockLockerDisabled", "<color=#00FFFF></color><color=#FFFFFF> Locker will not include codelocks on deploy</color>"},
                    {"DeployLockCupEnabled", "<color=#00FFFF></color><color=#FFFFFF> Cupboards will include codelocks on deploy</color>"},
                    {"DeployLockCupDisabled", "<color=#00FFFF></color><color=#FFFFFF> Cupboards will not include codelocks on deploy</color>"},
                    {"CodeAuth", "<color=#00FFFF></color><color=#FFFFFF> CodeLock secured and locked with <color=#00FF00>{0}</color></color>"},
                    {"NoAccess", "<color=#00FFFF></color><color=#FFFFFF> You are not granted for this feature</color>"},
                    {"NotSupported", "<color=#00FFFF></color><color=#FFFFFF> The specific function <color=#FF0000>{0}</color> is not available</color>"},
                    {"CommandUsage", "<color=#FFFFFF>Command usage: <color=#00FF00>{0}</color></color>"},
                    {"CommandToggle", "<color=#FFFFFF>All switches toggle their setting (on<>off)</color>"},
                    {"CommandAutolock", "<color=#FFFFFF><color=#00FF00>{0}</color> - Autolock feature: <color=#00FF00>{1}</color></color>"}, 
                    {"CommandPinCode", "<color=#FFFFFF><color=#00FF00>{0}</color> - Your current PIN: <color=#00FF00>{1}</color></color>"},
                    {"CommandPinCodeNotSet", "<color=#FFFFFF>Random 8-Digits</color>"},
                    {"CommandPinCodeSetTo", "<color=#00FFFF></color><color=#FFFFFF> Your Pin was succesful set to: <color=#00FF00>{0}</color></color>"},
                    {"CommandPinCodeHelp", "<color=#00FFFF></color><color=#FFFFFF> Set your PIN with <color=#00FF00>/{1} pin 1234</color> (4-Digits)"},
                    {"CommandDeployDoor", "<color=#FFFFFF> Deploy with Door:</color>"}, 
                    {"CommandDeployBox", "<color=#FFFFFF> Deploy with Box:</color>"},
                    {"CommandDeployLocker", "<color=#FFFFFF> Deploy with Locker:</color>"}, 
                    {"CommandDeployCupboard", "<color=#FFFFFF> Deploy with Cupboard:</color>"},
                }, this);
        lang.RegisterMessages(
                new Dictionary<string, string>
                {
                    {"AllAutoCloseEnabled", "<color=#00FFFF></color><color=#FFFFFF> Автоматическое закрытие всех дверей для Вас <color=#00FF00>ВКЛЮЧЕНО</color></color>"},
                    {"AllAutoCloseDisabled", "<color=#00FFFF></color><color=#FFFFFF> Автоматическое закрытие всех дверей для Вас <color=#FF0000>ВЫКЛЮЧЕНО</color></color>"},
                    {"BadFormatPin", "<color=#00FFFF></color><color=#FFFFFF> Неправильный формат пароля: <color=#FF0000>{0}</color></color>"},
                    {"AutoCloseEnabled", "<color=#00FFFF></color><color=#FFFFFF> Автоматическое закрытие <color=#00FF00>ЭТОЙ ДВЕРИ</color> для всех авторизованных игроков <color=#00FF00>ВКЛЮЧЕНО</color></color>"},
                    {"AutoCloseDisabled", "<color=#00FFFF></color><color=#FFFFFF> Автоматическое закрытие <color=#FF0000>ЭТОЙ ДВЕРИ</color> для всех авторизованных игроков <color=#FF0000>ВЫКЛЮЧЕНО</color></color>"},
                    {"AutoLockEnabled", "<color=#00FFFF></color><color=#FFFFFF> Автоматическая установка замка с паролем <color=#00FF00>ВКЛЮЧЕНА</color></color>"},
                    {"AutoLockDisabled", "<color=#00FFFF></color><color=#FFFFFF> Автоматическая установка замка с паролем <color=#FF0000>ВЫКЛЮЧЕНА</color></color>"},
                    {"DeployLockDoorEnabled", "<color=#00FFFF></color><color=#FFFFFF> Замки <color=#00FF00>будут</color> автоматически устанавливаться на двери</color>"},
                    {"DeployLockDoorDisabled", "<color=#00FFFF></color><color=#FFFFFF> Замки <color=#FF0000>не будут</color> автоматически устанавливаться на двери</color>"},
                    {"DeployLockBoxEnabled", "<color=#00FFFF></color><color=#FFFFFF> Замки <color=#00FF00>будут</color> автоматически устанавливаться на ящики</color>"},
                    {"DeployLockBoxDisabled", "<color=#00FFFF></color><color=#FFFFFF> Замки <color=#FF0000>не будут</color> автоматически устанавливаться на ящики</color>"},
                    {"DeployLockLockerEnabled", "<color=#00FFFF></color><color=#FFFFFF> Замки <color=#00FF00>будут</color> автоматически устанавливаться на шкафы с одеждой</color>"},
                    {"DeployLockLockerDisabled", "<color=#00FFFF></color><color=#FFFFFF> Замки <color=#FF0000>не будут</color> автоматически устанавливаться на шкафы с одеждой</color>"},
                    {"DeployLockCupEnabled", "<color=#00FFFF></color><color=#FFFFFF> Замки <color=#00FF00>будут</color> автоматически устанавливаться на шкафы</color>"},
                    {"DeployLockCupDisabled", "<color=#00FFFF></color><color=#FFFFFF> Замки <color=#FF0000>не будут</color> автоматически устанавливаться на шкафы</color>"},
                    {"CodeAuth", "<color=#00FFFF></color><color=#FFFFFF> Замок установлен! Пароль <color=#00FF00>{0}</color></color>"},
                    {"NoAccess", "<color=#00FFFF></color><color=#FFFFFF> У вас нету привилегии для использования команды</color>"},
                    {"NotSupported", "<color=#00FFFF></color><color=#FFFFFF> Функция <color=#FF0000>{0}</color> недоступна!</color>"},
                    {"CommandUsage", "<color=#FFFFFF>Команда: <color=#00FF00>{0}</color></color>"},
                    {"CommandToggle", "<color=#FFFFFF>Используется переключение <color=#00FF00>ON/OFF</color></color>"},
                    {"CommandAutolock", "<color=#FFFFFF><color=#00FF00>{0}</color> - значение: <color=#00FF00>{1}</color></color>"}, 
                    {"CommandPinCode", "<color=#FFFFFF><color=#00FF00>{0}</color> - Текущий пароль: <color=#00FF00>{1}</color></color>"},    
                    {"CommandPinCodeNotSet", "<color=#FFFFFF>Случайные 8 цифр</color>"},
                    {"CommandPinCodeSetTo", "<color=#00FFFF></color><color=#FFFFFF> Ваш пароль: <color=#00FF00>{0}</color>"},
                    {"CommandPinCodeHelp", "<color=#00FFFF></color><color=#FFFFFF> Установить пароль <color=##00FF00>/{1} pin 1234</color> (4 цифры)</color>"},
                    {"CommandDeployDoor", "<color=#FFFFFF> Установка на двери:</color>"}, 
                    {"CommandDeployBox", "<color=#FFFFFF> Установка на ящики:</color>"},
                    {"CommandDeployLocker", "<color=#FFFFFF> Установка на шкаф с одеждой:</color>"}, 
                    {"CommandDeployCupboard", "<color=#FFFFFF> Установка на шкаф:</color>"},
                }, this, "ru");
        }    
        private string Msg(string key, BasePlayer player = null) =>
            lang.GetMessage(key, this, player.UserIDString);
        #endregion    
        
    }
}