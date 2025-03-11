using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Doors", "Wulf/lukespragg/Arainrr", "3.2.9", ResourceId = 1924)]
    [Description("Automatically closes doors behind players after X seconds")]
    public class AutoDoors : RustPlugin
    {
        #region Fields

        [PluginReference] private Plugin RustTranslationAPI;
        private const string PERMISSION_USE = "autodoors.use";
        private readonly Hash<uint, Timer> doorTimers = new Hash<uint, Timer>();
        private readonly Dictionary<string, string> supportedDoors = new Dictionary<string, string>();
        private HashSet<DoorManipulator> doorManipulators;

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            LoadData();
            Unsubscribe(nameof(OnEntitySpawned));
            permission.RegisterPermission(PERMISSION_USE, this);
            if (configData.chatS.commands.Length == 0)
            {
                configData.chatS.commands = new[] { "ad" };
            }
            foreach (var command in configData.chatS.commands)
            {
                cmd.AddChatCommand(command, this, nameof(CmdAutoDoor));
            }
        }

        private void OnServerInitialized()
        {
            UpdateConfig();
            if (configData.globalS.excludeDoorController)
            {
                doorManipulators = new HashSet<DoorManipulator>();
                Subscribe(nameof(OnEntitySpawned));
                foreach (var doorManipulator in BaseNetworkable.serverEntities.OfType<DoorManipulator>())
                {
                    OnEntitySpawned(doorManipulator);
                }
            }
        }

        private void OnEntitySpawned(DoorManipulator doorManipulator)
        {
            if (doorManipulator == null || doorManipulator.OwnerID == 0) return;
            doorManipulators.Add(doorManipulator);
        }

        private void OnEntityKill(DoorManipulator doorManipulator)
        {
            if (doorManipulator == null || doorManipulators == null) return;
            doorManipulators.RemoveWhere(x => x == doorManipulator);
        }

        private void OnEntityKill(Door door)
        {
            if (door == null || door.net == null) return;
            var doorID = door.net.ID;
            Timer value;
            if (doorTimers.TryGetValue(doorID, out value))
            {
                value?.Destroy();
                doorTimers.Remove(doorID);
            }
            foreach (var playerData in storedData.playerData.Values)
            {
                playerData.theDoorS.Remove(doorID);
            }
        }

        private void OnServerSave() => timer.Once(UnityEngine.Random.Range(0f, 60f), SaveData);

        private void Unload()
        {
            foreach (var value in doorTimers.Values)
            {
                value?.Destroy();
            }
            SaveData();
        }

        private void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || door.net == null || !door.IsOpen()) return;
            if (!supportedDoors.ContainsKey(door.ShortPrefabName)) return;
            if (!configData.globalS.useUnownedDoor && door.OwnerID == 0) return;
            if (configData.globalS.excludeDoorController && HasDoorController(door)) return;
            if (configData.usePermission && !permission.UserHasPermission(player.UserIDString, PERMISSION_USE)) return;

            var playerData = GetPlayerData(player.userID, true);
            if (!playerData.doorData.enabled) return;
            float autoCloseTime;
            var doorID = door.net.ID;
            StoredData.DoorData doorData;
            if (playerData.theDoorS.TryGetValue(doorID, out doorData))
            {
                if (!doorData.enabled) return;
                autoCloseTime = doorData.time;
            }
            else if (playerData.doorTypeS.TryGetValue(door.ShortPrefabName, out doorData))
            {
                if (!doorData.enabled) return;
                autoCloseTime = doorData.time;
            }
            else autoCloseTime = playerData.doorData.time;

            if (autoCloseTime <= 0) return;
            if (Interface.CallHook("OnDoorAutoClose", player, door) != null) return;

            Timer value;
            if (doorTimers.TryGetValue(doorID, out value))
            {
                value?.Destroy();
            }
            doorTimers[doorID] = timer.Once(autoCloseTime, () =>
            {
                doorTimers.Remove(doorID);
                if (door == null || !door.IsOpen()) return;
                if (configData.globalS.cancelOnKill && player != null && player.IsDead()) return;
                door.SetFlag(BaseEntity.Flags.Open, false);
                door.SendNetworkUpdateImmediate();
            });
        }

        private void OnDoorClosed(Door door, BasePlayer player)
        {
            if (door == null || door.net == null || door.IsOpen()) return;
            Timer value;
            if (doorTimers.TryGetValue(door.net.ID, out value))
            {
                value?.Destroy();
                doorTimers.Remove(door.net.ID);
            }
        }

        #endregion Oxide Hooks

        #region Methods

        private bool HasDoorController(Door door)
        {
            foreach (var doorManipulator in doorManipulators)
            {
                if (doorManipulator != null && doorManipulator.targetDoor == door)
                {
                    return true;
                }
            }
            return false;
        }

        private StoredData.PlayerData GetPlayerData(ulong playerID, bool readOnly = false)
        {
            StoredData.PlayerData playerData;
            if (!storedData.playerData.TryGetValue(playerID, out playerData))
            {
                playerData = new StoredData.PlayerData
                {
                    doorData = new StoredData.DoorData
                    {
                        enabled = configData.globalS.defaultEnabled,
                        time = configData.globalS.defaultDelay,
                    }
                };
                if (readOnly)
                {
                    return playerData;
                }
                storedData.playerData.Add(playerID, playerData);
            }

            return playerData;
        }

        private static Door GetLookingAtDoor(BasePlayer player)
        {
            RaycastHit rHit;
            if (Physics.Raycast(player.eyes.HeadRay(), out rHit, 10f, Rust.Layers.Mask.Construction))
            {
                return rHit.GetEntity() as Door;
            }
            return null;
        }

        private void UpdateConfig()
        {
            foreach (var itemDefinition in ItemManager.GetItemDefinitions())
            {
                var itemModDeployable = itemDefinition.GetComponent<ItemModDeployable>();
                if (itemModDeployable == null) continue;
                var door = GameManager.server.FindPrefab(itemModDeployable.entityPrefab.resourcePath)?.GetComponent<Door>();
                if (door == null || string.IsNullOrEmpty(door.ShortPrefabName)) continue;
                ConfigData.DoorSettings doorSettings;
                if (!configData.doorS.TryGetValue(itemDefinition.shortname, out doorSettings))
                {
                    doorSettings = new ConfigData.DoorSettings
                    {
                        enabled = true,
                        displayName = itemDefinition.displayName.english
                    };
                    configData.doorS.Add(itemDefinition.shortname, doorSettings);
                }
                if (doorSettings.enabled && !supportedDoors.ContainsKey(door.ShortPrefabName))
                {
                    supportedDoors.Add(door.ShortPrefabName, doorSettings.displayName);
                }
            }
            SaveConfig();
        }

        #region RustTranslationAPI

        private string GetDeployableTranslation(string language, string deployable) => (string)RustTranslationAPI.Call("GetDeployableTranslation", language, deployable);

        private string GetDeployableDisplayName(BasePlayer player, string deployable, string displayName)
        {
            if (RustTranslationAPI != null)
            {
                displayName = GetDeployableTranslation(lang.GetLanguage(player.UserIDString), deployable);
                if (!string.IsNullOrEmpty(displayName))
                {
                    return displayName;
                }
            }
            return displayName;
        }

        #endregion RustTranslationAPI

        #endregion Methods

        #region ChatCommand

        private void CmdAutoDoor(BasePlayer player, string command, string[] args)
        {
            if (configData.usePermission && !permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
            {
                Print(player, Lang("NotAllowed", player.UserIDString));
                return;
            }
            var playerData = GetPlayerData(player.userID);
            if (args == null || args.Length == 0)
            {
                playerData.doorData.enabled = !playerData.doorData.enabled;
                Print(player, Lang("AutoDoor", player.UserIDString, playerData.doorData.enabled ? Lang("Enabled", player.UserIDString) : Lang("Disabled", player.UserIDString)));
                return;
            }
            float time;
            if (float.TryParse(args[0], out time))
            {
                if (time <= configData.globalS.maximumDelay && time >= configData.globalS.minimumDelay)
                {
                    playerData.doorData.time = time;
                    if (!playerData.doorData.enabled) playerData.doorData.enabled = true;
                    Print(player, Lang("AutoDoorDelay", player.UserIDString, time));
                    return;
                }
                Print(player, Lang("AutoDoorDelayLimit", player.UserIDString, configData.globalS.minimumDelay, configData.globalS.maximumDelay));
                return;
            }
            switch (args[0].ToLower())
            {
                case "a":
                case "all":
                    {
                        if (args.Length > 1)
                        {
                            if (float.TryParse(args[1], out time))
                            {
                                if (time <= configData.globalS.maximumDelay && time >= configData.globalS.minimumDelay)
                                {
                                    playerData.doorData.time = time;
                                    playerData.doorTypeS.Clear();
                                    playerData.theDoorS.Clear();
                                    Print(player, Lang("AutoDoorDelayAll", player.UserIDString, time));
                                    return;
                                }

                                Print(player,
                                    Lang("AutoDoorDelayLimit", player.UserIDString, configData.globalS.minimumDelay,
                                        configData.globalS.maximumDelay));
                                return;
                            }
                        }

                        break;
                    }
                case "s":
                case "single":
                    {
                        var door = GetLookingAtDoor(player);
                        if (door == null || door.net == null)
                        {
                            Print(player, Lang("DoorNotFound", player.UserIDString));
                            return;
                        }

                        string doorDisplayName;
                        if (!supportedDoors.TryGetValue(door.ShortPrefabName, out doorDisplayName))
                        {
                            Print(player, Lang("DoorNotSupported", player.UserIDString));
                            return;
                        }

                        StoredData.DoorData doorData;
                        if (!playerData.theDoorS.TryGetValue(door.net.ID, out doorData))
                        {
                            doorData = new StoredData.DoorData
                            { enabled = true, time = configData.globalS.defaultDelay };
                            playerData.theDoorS.Add(door.net.ID, doorData);
                        }

                        if (args.Length <= 1)
                        {
                            doorData.enabled = !doorData.enabled;
                            Print(player,
                                Lang("AutoDoorSingle", player.UserIDString,
                                    GetDeployableDisplayName(player, door.ShortPrefabName, doorDisplayName),
                                    doorData.enabled
                                        ? Lang("Enabled", player.UserIDString)
                                        : Lang("Disabled", player.UserIDString)));
                            return;
                        }

                        if (float.TryParse(args[1], out time))
                        {
                            if (time <= configData.globalS.maximumDelay && time >= configData.globalS.minimumDelay)
                            {
                                doorData.time = time;
                                Print(player, Lang("AutoDoorSingleDelay", player.UserIDString,
                                    GetDeployableDisplayName(player, door.ShortPrefabName, doorDisplayName), time));
                                return;
                            }

                            Print(player,
                                Lang("AutoDoorDelayLimit", player.UserIDString, configData.globalS.minimumDelay,
                                    configData.globalS.maximumDelay));
                            return;
                        }

                        break;
                    }

                case "t":
                case "type":
                    {
                        var door = GetLookingAtDoor(player);
                        if (door == null || door.net == null)
                        {
                            Print(player, Lang("DoorNotFound", player.UserIDString));
                            return;
                        }

                        string doorDisplayName;
                        if (!supportedDoors.TryGetValue(door.ShortPrefabName, out doorDisplayName))
                        {
                            Print(player, Lang("DoorNotSupported", player.UserIDString));
                            return;
                        }

                        StoredData.DoorData doorData;
                        if (!playerData.doorTypeS.TryGetValue(door.ShortPrefabName, out doorData))
                        {
                            doorData = new StoredData.DoorData
                            { enabled = true, time = configData.globalS.defaultDelay };
                            playerData.doorTypeS.Add(door.ShortPrefabName, doorData);
                        }

                        if (args.Length <= 1)
                        {
                            doorData.enabled = !doorData.enabled;
                            Print(player,
                                Lang("AutoDoorType", player.UserIDString, GetDeployableDisplayName(player, door.ShortPrefabName, doorDisplayName),
                                    doorData.enabled
                                        ? Lang("Enabled", player.UserIDString)
                                        : Lang("Disabled", player.UserIDString)));
                            return;
                        }

                        if (float.TryParse(args[1], out time))
                        {
                            if (time <= configData.globalS.maximumDelay && time >= configData.globalS.minimumDelay)
                            {
                                doorData.time = time;
                                Print(player, Lang("AutoDoorTypeDelay", player.UserIDString,
                                    GetDeployableDisplayName(player, door.ShortPrefabName, doorDisplayName), time));
                                return;
                            }

                            Print(player,
                                Lang("AutoDoorDelayLimit", player.UserIDString, configData.globalS.minimumDelay,
                                    configData.globalS.maximumDelay));
                            return;
                        }

                        break;
                    }

                case "h":
                case "help":
                    {
                        StringBuilder stringBuilder = Pool.Get<StringBuilder>();
                        stringBuilder.AppendLine();
                        var firstCmd = configData.chatS.commands[0];
                        stringBuilder.AppendLine(Lang("AutoDoorSyntax", player.UserIDString, firstCmd));
                        stringBuilder.AppendLine(Lang("AutoDoorSyntax1", player.UserIDString, firstCmd,
                            configData.globalS.minimumDelay, configData.globalS.maximumDelay));
                        stringBuilder.AppendLine(Lang("AutoDoorSyntax2", player.UserIDString, firstCmd));
                        stringBuilder.AppendLine(Lang("AutoDoorSyntax3", player.UserIDString, firstCmd,
                            configData.globalS.minimumDelay, configData.globalS.maximumDelay));
                        stringBuilder.AppendLine(Lang("AutoDoorSyntax4", player.UserIDString, firstCmd));
                        stringBuilder.AppendLine(Lang("AutoDoorSyntax5", player.UserIDString, firstCmd,
                            configData.globalS.minimumDelay, configData.globalS.maximumDelay));
                        stringBuilder.AppendLine(Lang("AutoDoorSyntax6", player.UserIDString, firstCmd,
                            configData.globalS.minimumDelay, configData.globalS.maximumDelay));
                        Print(player, stringBuilder.ToString());
                        stringBuilder.Clear();
                        Pool.Free(ref stringBuilder);
                        return;
                    }
            }
            Print(player, Lang("SyntaxError", player.UserIDString, configData.chatS.commands[0]));
        }

        #endregion ChatCommand

        #region ConfigurationFile

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Use permissions")]
            public bool usePermission = false;

            [JsonProperty(PropertyName = "Clear data on map wipe")]
            public bool clearDataOnWipe = false;

            [JsonProperty(PropertyName = "Global settings")]
            public GlobalSettings globalS = new GlobalSettings();

            [JsonProperty(PropertyName = "Chat settings")]
            public ChatSettings chatS = new ChatSettings();

            [JsonProperty(PropertyName = "Door Settings")]
            public Dictionary<string, DoorSettings> doorS = new Dictionary<string, DoorSettings>();

            public class DoorSettings
            {
                public bool enabled;
                public string displayName;
            }

            public class GlobalSettings
            {
                [JsonProperty(PropertyName = "Allows automatic closing of unowned doors")]
                public bool useUnownedDoor = false;

                [JsonProperty(PropertyName = "Exclude door controller")]
                public bool excludeDoorController = true;

                [JsonProperty(PropertyName = "Cancel on player dead")]
                public bool cancelOnKill = false;

                [JsonProperty(PropertyName = "Default enabled")]
                public bool defaultEnabled = true;

                [JsonProperty(PropertyName = "Default delay")]
                public float defaultDelay = 5f;

                [JsonProperty(PropertyName = "Maximum delay")]
                public float maximumDelay = 10f;

                [JsonProperty(PropertyName = "Minimum delay")]
                public float minimumDelay = 5f;
            }

            public class ChatSettings
            {
                [JsonProperty(PropertyName = "Chat command")]
                public string[] commands = { "ad", "autodoor" };

                [JsonProperty(PropertyName = "Chat prefix")]
                public string prefix = "<color=#8e6874>STORM RUST - Автоматические двери</color>:\n ";

                [JsonProperty(PropertyName = "Chat steamID icon")]
                public ulong steamIDIcon = 0;
            }

            [JsonProperty(PropertyName = "Version")]
            public VersionNumber version;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    LoadDefaultConfig();
                }
                else
                {
                    UpdateConfigValues();
                }
            }
            catch (Exception ex)
            {
                PrintError($"The configuration file is corrupted. \n{ex}");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file");
            configData = new ConfigData();
            configData.version = Version;
        }

        protected override void SaveConfig() => Config.WriteObject(configData);

        private void UpdateConfigValues()
        {
            if (configData.version < Version)
            {
                if (configData.version <= default(VersionNumber))
                {
                    string prefix, prefixColor;
                    if (GetConfigValue(out prefix, "Chat Settings", "Chat Prefix") && GetConfigValue(out prefixColor, "Chat Settings", "Chat Prefix Color"))
                    {
                        configData.chatS.prefix = $"<color={prefixColor}>{prefix}</color> :";
                    }
                }
                configData.version = Version;
            }
        }

        private bool GetConfigValue<T>(out T value, params string[] path)
        {
            var configValue = Config.Get(path);
            if (configValue == null)
            {
                value = default(T);
                return false;
            }
            value = Config.ConvertValue<T>(configValue);
            return true;
        }

        #endregion ConfigurationFile

        #region DataFile

        private StoredData storedData;

        private class StoredData
        {
            public readonly Dictionary<ulong, PlayerData> playerData = new Dictionary<ulong, PlayerData>();

            public class PlayerData
            {
                public DoorData doorData = new DoorData();
                public readonly Dictionary<uint, DoorData> theDoorS = new Dictionary<uint, DoorData>();
                public readonly Dictionary<string, DoorData> doorTypeS = new Dictionary<string, DoorData>();
            }

            public class DoorData
            {
                public bool enabled;
                public float time;
            }
        }

        private void LoadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch
            {
                storedData = null;
            }
            finally
            {
                if (storedData == null)
                {
                    ClearData();
                }
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);

        private void ClearData()
        {
            storedData = new StoredData();
            SaveData();
        }

        private void OnNewSave(string filename)
        {
            if (configData.clearDataOnWipe)
            {
                ClearData();
            }
            else
            {
                foreach (var value in storedData.playerData.Values)
                {
                    value.theDoorS.Clear();
                }
                SaveData();
            }
        }

        #endregion DataFile

        #region LanguageFile

        private void Print(BasePlayer player, string message)
        {
            Player.Message(player, message, configData.chatS.prefix, configData.chatS.steamIDIcon);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "У вас не прав на использование данной команды",
                ["Enabled"] = "<color=#8ee700>ВКЛЮЧЕНО</color>",
                ["Disabled"] = "<color=#8e6874>ВЫКЛЮЧЕНО</color>",
                ["AutoDoor"] = "Автоматическое закрытие двери теперь {0}",
                ["AutoDoorDelay"] = "Задержка автоматического закрытия двери установлена на {0}с. (Двери, установленные по «одинарным» и «типу», не включены)",
                ["AutoDoorDelayAll"] = "Автоматическая задержка закрытия всех дверей установлена на {0}с",
                ["DoorNotFound"] = "Вам нужно посмотреть на дверь",
                ["DoorNotSupported"] = "Этот тип двери не поддерживается",
                ["AutoDoorDelayLimit"] = "Допустимая задержка автоматического закрытия двери составляет от {0}с до {1}с",
                ["AutoDoorSingle"] = "Автоматическое закрытие этого <color=#8e6874>{0}</color> является {1}",
                ["AutoDoorSingleDelay"] = "Автоматическая задержка закрытия этого<color=#8e6874>{0}</color> является {1}с",
                ["AutoDoorType"] = "Автоматическое закрытие <color=#8e6874>{0}</color> дверь {1}",
                ["AutoDoorTypeDelay"] = "Автоматическая задержка закрытия <color=#8e6874>{0}</color> дверь {1}с",
                ["SyntaxError"] = "Синтаксическая ошибка, тип '<color=#8e6874>/{0} <help | h></color>' просмотреть справку",

                ["AutoDoorSyntax"] = "<color=#8e6874>/{0} </color> - Включить/выключить автоматическое закрытие двери",
                ["AutoDoorSyntax1"] = "<color=#8e6874>/{0} [time (seconds)]</color> - Включить/выключить автоматическое закрытие двери {1}с до {2}с. (Двери, установленные по «одинарным» и «типу», не включены)",
                ["AutoDoorSyntax2"] = "<color=#8e6874>/{0} <single | s></color> - Включить/выключить автоматическое закрытие двери, на которую вы смотрите",
                ["AutoDoorSyntax3"] = "<color=#8e6874>/{0} <single | s> [time (seconds)]</color> - Установите задержку автоматического закрытия для двери, на которую вы смотрите, разрешенное время между {1}s and {2}s",
                ["AutoDoorSyntax4"] = "<color=#8e6874>/{0} <type | t></color> - Включите/отключите автоматическое закрытие двери для выбранного типа двери. («тип» — это просто слово, а не тип двери)",
                ["AutoDoorSyntax5"] = "<color=#8e6874>/{0} <type | t> [time (seconds)]</color> - Установите задержку автоматического закрытия для типа двери, на которую вы смотрите, допустимое время составляет от {1} с до {2} с. («тип» — это просто слово, а не тип двери)",
                ["AutoDoorSyntax6"] = "<color=#8e6874>/{0} <all | a> [time (seconds)]</color> - Установите задержку автоматического закрытия для всех дверей, допустимое время составляет от {1} с до {2} с..",
            }, this);
        }

        #endregion LanguageFile
    }
}