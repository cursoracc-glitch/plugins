using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Auto Decay", "Hougan/Arainrr", "1.3.1")]
    [Description("Auto damage to objects, that are not in building zone")]
    public class AutoDecay : RustPlugin
    {
        #region Fields

        private const string PERMISSION_IGNORE = "autodecay.ignore";
        private readonly Hash<ulong, float> notifyPlayers = new Hash<ulong, float>();
        private readonly Dictionary<ulong, DecayController> decayControllers = new Dictionary<ulong, DecayController>();

        private readonly List<string> defaultDisabled = new List<string>
        {
            "small_stash_deployed",
            "sleepingbag_leather_deployed",
        };

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            LoadData();
            Unsubscribe(nameof(OnEntitySpawned));
            permission.RegisterPermission(PERMISSION_IGNORE, this);
        }

        private void OnServerInitialized()
        {
            Subscribe(nameof(OnEntitySpawned));
            UpdateConfig(_config.entityS.Count <= 0);
            foreach (var baseNetworkable in BaseNetworkable.serverEntities.OfType<BaseCombatEntity>())
            {
                if (baseNetworkable != null)
                    CreateDecayController(baseNetworkable);
            }

            foreach (var entry in _config.entityS)
            {
                if (!storedData.entityShortPrefabNames.Contains(entry.Key))
                    PrintError($"\"{entry.Key}\" is an invalid combat entity short prefab name, Please get them in the data file");
            }
        }

        private void Unload()
        {
            foreach (var decayController in decayControllers.Values)
                decayController?.Destroy();

            _config = null;
        }

        private void OnEntitySpawned(BaseCombatEntity baseCombatEntity)
        {
            if (baseCombatEntity == null || baseCombatEntity.net == null) return;
            var buildingPrivlidge = baseCombatEntity as BuildingPrivlidge;
            if (buildingPrivlidge != null)
                HandleCupboard(buildingPrivlidge, true);
            var player = baseCombatEntity.OwnerID.IsSteamId() ? BasePlayer.FindByID(baseCombatEntity.OwnerID) : null;
            CreateDecayController(baseCombatEntity, player, false);
        }

        //private void OnEntityDeath(BaseCombatEntity baseCombatEntity, HitInfo info) => OnEntityKill(baseCombatEntity);

        private void OnEntityKill(BaseCombatEntity baseCombatEntity)
        {
            if (baseCombatEntity == null || baseCombatEntity.net == null) return;
            var buildingPrivlidge = baseCombatEntity as BuildingPrivlidge;
            if (buildingPrivlidge != null)
                HandleCupboard(buildingPrivlidge, false);

            DecayController decayController;
            if (decayControllers.TryGetValue(baseCombatEntity.net.ID.Value, out decayController))
            {
                decayController?.Destroy();
                decayControllers.Remove(baseCombatEntity.net.ID.Value);
            }
        }

        private void OnStructureUpgrade(BuildingBlock buildingBlock, BasePlayer player, BuildingGrade.Enum newGrade)
        {
            if (buildingBlock == null || buildingBlock.net == null) return;
            var grade = buildingBlock.grade;
            NextTick(() =>
            {
                if (buildingBlock == null || buildingBlock.net == null) return;
                if (grade == buildingBlock.grade) return;
                var settings = GetBuildingBlockSettings(buildingBlock);
                if (IsDecayEnabled(settings, buildingBlock))
                {
                    DecayController decayController;
                    if (!decayControllers.TryGetValue(buildingBlock.net.ID.Value, out decayController))
                    {
                        decayController = new DecayController(buildingBlock, settings, false);
                        decayControllers.Add(buildingBlock.net.ID.Value, decayController);
                    }
                    else
                        decayController?.OnBuildingUpgrade(settings);
                }
                else
                {
                    DecayController decayController;
                    if (decayControllers.TryGetValue(buildingBlock.net.ID.Value, out decayController))
                    {
                        decayController?.Destroy();
                        decayControllers.Remove(buildingBlock.net.ID.Value);
                    }
                }
            });
        }

        #endregion Oxide Hooks

        #region Methods

        private void HandleCupboard(BuildingPrivlidge buildingPrivlidge, bool spawned)
        {
            var decayEntities = buildingPrivlidge?.GetBuilding()?.decayEntities;
            if (decayEntities != null)
            {
                //Only check the same building
                foreach (var decayEntity in decayEntities)
                {
                    if (decayEntity == null || decayEntity.net == null) continue;
                    if (decayEntity == buildingPrivlidge) continue;
                    DecayController decayController;
                    if (decayControllers.TryGetValue(decayEntity.net.ID.Value, out decayController))
                    {
                        if (spawned) decayController?.OnCupboardPlaced();
                        else decayController?.OnCupboardDestroyed();
                    }
                }
            }
        }

        private void UpdateConfig(bool newConfig)
        {
            foreach (var itemDefinition in ItemManager.GetItemDefinitions())
            {
                var prefabName = itemDefinition.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
                if (string.IsNullOrEmpty(prefabName)) continue;
                var baseCombatEntity = GameManager.server.FindPrefab(prefabName)?.GetComponent<BaseCombatEntity>();
                if (baseCombatEntity == null || string.IsNullOrEmpty(baseCombatEntity.ShortPrefabName)) continue;
                if (_config.entityS.ContainsKey(baseCombatEntity.ShortPrefabName)) continue;

                _config.entityS.Add(baseCombatEntity.ShortPrefabName, new DecaySettings
                {
                    enabled = newConfig && !(itemDefinition.category == ItemCategory.Food || defaultDisabled.Contains(baseCombatEntity.ShortPrefabName))
                });
            }
            UpdateData(true, _config.buildingBlockS.Count <= 0);
            SaveConfig();
        }

        private void UpdateData(bool updateConfig = false, bool newConfig = false)
        {
            var grades = new[] { BuildingGrade.Enum.Twigs, BuildingGrade.Enum.Wood, BuildingGrade.Enum.Stone, BuildingGrade.Enum.Metal, BuildingGrade.Enum.TopTier };

            storedData.entityShortPrefabNames.Clear();
            foreach (var prefab in GameManifest.Current.entities)
            {
                var baseCombatEntity = GameManager.server.FindPrefab(prefab)?.GetComponent<BaseCombatEntity>();
                if (baseCombatEntity == null || string.IsNullOrEmpty(baseCombatEntity.ShortPrefabName)) continue;
                storedData.entityShortPrefabNames.Add(baseCombatEntity.ShortPrefabName);
                if (updateConfig)
                {
                    if (baseCombatEntity is BaseVehicle)
                    {
                        if (!_config.entityS.ContainsKey(baseCombatEntity.ShortPrefabName))
                        {
                            _config.entityS.Add(baseCombatEntity.ShortPrefabName, new DecaySettings { enabled = newConfig });
                        }
                    }

                    if (baseCombatEntity is BuildingBlock)
                    {
                        _config.entityS.Remove(baseCombatEntity.ShortPrefabName);
                        Dictionary<BuildingGrade.Enum, DecaySettings> settings;
                        if (!_config.buildingBlockS.TryGetValue(baseCombatEntity.ShortPrefabName, out settings))
                        {
                            settings = new Dictionary<BuildingGrade.Enum, DecaySettings>();
                            _config.buildingBlockS.Add(baseCombatEntity.ShortPrefabName, settings);
                        }

                        foreach (var grade in grades)
                        {
                            if (!settings.ContainsKey(grade))
                            {
                                settings.Add(grade, new DecaySettings { enabled = newConfig });
                            }
                        }
                    }
                }
            }
            SaveData();
        }

        private void CreateDecayController(BaseCombatEntity baseCombatEntity, BasePlayer player = null, bool init = true)
        {
            if (baseCombatEntity == null || baseCombatEntity.net == null) return;
            if (baseCombatEntity.OwnerID.IsSteamId() && permission.UserHasPermission(baseCombatEntity.OwnerID.ToString(), PERMISSION_IGNORE)) return;
            var decayEntityS = GetDecayEntitySettings(baseCombatEntity);
            if (IsDecayEnabled(decayEntityS, baseCombatEntity))
            {
                if (!decayControllers.ContainsKey(baseCombatEntity.net.ID.Value))
                {
                    decayControllers.Add(baseCombatEntity.net.ID.Value, new DecayController(baseCombatEntity, decayEntityS, init));
                    if (_config.notifyPlayer && player != null && baseCombatEntity.GetBuildingPrivilege() == null)
                        SendMessage(player, decayEntityS.delayTime + decayEntityS.destroyTime);
                }
            }
        }

        private void SendMessage(BasePlayer player, float time)
        {
            float value;
            if (notifyPlayers.TryGetValue(player.userID, out value) && Time.realtimeSinceStartup - value <= _config.notifyInterval) return;
            notifyPlayers[player.userID] = Time.realtimeSinceStartup;
            Print(player, Lang("DESTROY", player.UserIDString, TimeSpan.FromSeconds(time).ToShortString()));
        }

        private static DecaySettings GetDecayEntitySettings(BaseCombatEntity baseCombatEntity)
        {
            var buildingBlock = baseCombatEntity as BuildingBlock;
            if (buildingBlock != null)
            {
                return GetBuildingBlockSettings(buildingBlock);
            }
            DecaySettings decaySettings;
            return _config.entityS.TryGetValue(baseCombatEntity.ShortPrefabName, out decaySettings) ? decaySettings : null;
        }

        private static DecaySettings GetBuildingBlockSettings(BuildingBlock buildingBlock)
        {
            Dictionary<BuildingGrade.Enum, DecaySettings> buildingSettings;
            if (_config.buildingBlockS.TryGetValue(buildingBlock.ShortPrefabName, out buildingSettings))
            {
                DecaySettings settings;
                if (buildingSettings.TryGetValue(buildingBlock.grade, out settings))
                {
                    return settings;
                }
            }
            return null;
        }

        private static bool IsDecayEnabled(DecaySettings decaySettings, BaseEntity entity)
        {
            return decaySettings != null && decaySettings.enabled && (!decaySettings.onlyOwned || entity.OwnerID.IsSteamId());
        }

        #endregion Methods

        #region DestroyControl

        private class DecayController
        {
            private enum State
            {
                None,
                Delaying,
                Decaying,
            }

            private BaseCombatEntity entity;
            private DecaySettings decaySettings;

            private State state;
            private float tickDamage;
            private bool isCupboard;

            public DecayController(BaseCombatEntity entity, DecaySettings decaySettings, bool init)
            {
                this.entity = entity;
                this.decaySettings = decaySettings;
                isCupboard = entity is BuildingPrivlidge;
                entity.InvokeRepeating(CheckBuildingPrivilege, init ? UnityEngine.Random.Range(0f, 60f) : 1f, _config.cupboardCheckTime);
            }

            private void CheckBuildingPrivilege()
            {
                if (entity == null || entity.IsDestroyed)
                {
                    Destroy();
                    return;
                }

                if (isCupboard ? OnFoundation() : HasBuildingPrivilege())
                {
                    StopDamage();
                    return;
                }
                StartDelay();
            }

            private bool HasBuildingPrivilege()
            {
                var buildingPrivlidge = entity.GetBuildingPrivilege();
                if (buildingPrivlidge == null) return false;
                if (_config.checkEmptyCupboard)
                    return buildingPrivlidge.GetProtectedMinutes() > 0f;
   
                return true;

            }

            private bool OnFoundation()
            {
                RaycastHit raycastHit;
                return Physics.Raycast(entity.transform.position + Vector3.up * 0.1f, Vector3.down, out raycastHit, 0.11f, Rust.Layers.Mask.Construction) && raycastHit.GetEntity() is BuildingBlock;
            }

            private void StartDelay()
            {
                if (state != State.None) return;
                state = State.Delaying;
                entity.Invoke(StartDamage, decaySettings.delayTime);
            }

            private void StartDamage()
            {
                if (entity == null || entity.IsDestroyed)
                {
                    Destroy();
                    return;
                }

                state = State.Decaying;
                entity.InvokeRepeating(DoDamage, 600f, decaySettings.destroyTime / decaySettings.tickRate);
            }

            private void StopDamage()
            {
                switch (state)
                {
                    case State.Delaying:
                        state = State.None;
                        entity.CancelInvoke(StartDamage);
                        break;

                    case State.Decaying:
                        state = State.None;
                        entity.CancelInvoke(DoDamage);
                        break;
                }
            }

            private void ResetDamage()
            {
                StopDamage();
                StartDamage();
            }

            private void DoDamage()
            {
                if (entity == null || entity.IsDestroyed)
                {
                    Destroy();
                    return;
                }

                var currentTickDamage = entity.MaxHealth() / decaySettings.tickRate;
                if (tickDamage != currentTickDamage)
                    tickDamage = currentTickDamage;

                entity.Hurt(tickDamage, Rust.DamageType.Decay);
            }

            public void OnBuildingUpgrade(DecaySettings settings)
            {
                decaySettings = settings;
                StopDamage();
                CheckBuildingPrivilege();
            }

            public void OnCupboardPlaced()
            {
                StopDamage();
            }

            public void OnCupboardDestroyed()
            {
                StartDelay();
            }

            public void Destroy()
            {
                if (entity == null) return;
                entity.CancelInvoke(DoDamage);
                entity.CancelInvoke(StartDamage);
                entity.CancelInvoke(CheckBuildingPrivilege);
            }
        }

        #endregion DestroyControl

        #region ConfigurationFile

        private static ConfigData _config;

        private class ConfigData
        {
            [JsonProperty("Check Cupboard Interval (Seconds)")]
            public float cupboardCheckTime = 300f;

            [JsonProperty("Not Protected Cupboard = No Cupboard")]
            public bool checkEmptyCupboard;

            [JsonProperty("Notify Player That His Object Will Be Removed")]
            public bool notifyPlayer = true;

            [JsonProperty("Notify Interval")]
            public float notifyInterval = 10f;

            [JsonProperty("Chat Settings")]
            public ChatSettings chatS = new ChatSettings();

            [JsonProperty("Building Block Settings")]
            public Dictionary<string, Dictionary<BuildingGrade.Enum, DecaySettings>> buildingBlockS = new Dictionary<string, Dictionary<BuildingGrade.Enum, DecaySettings>>();

            [JsonProperty("Other Entity Settings")]
            public Dictionary<string, DecaySettings> entityS = new Dictionary<string, DecaySettings>();

            [JsonProperty("Version")]
            public VersionNumber version;
        }

        public class ChatSettings
        {
            [JsonProperty("Chat Prefix")]
            public string prefix = "<color=#00FFFF>[AutoDecay]</color>: ";

            [JsonProperty("Chat SteamID Icon")]
            public ulong steamIDIcon = 0;
        }

        private class DecaySettings
        {
            [JsonProperty("Enabled")]
            public bool enabled;

            [JsonProperty("Only Used For Player's Entity")]
            public bool onlyOwned = true;

            [JsonProperty("Delay Time (Seconds)")]
            public float delayTime = 600f;

            [JsonProperty("Destroy Time (Seconds)")]
            public float destroyTime = 3600f;

            [JsonProperty("Tick Rate (Damage Per Tick = Max Health / This)")]
            public float tickRate = 10f;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<ConfigData>();
                if (_config == null)
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
            _config = new ConfigData();
            _config.version = Version;
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private void UpdateConfigValues()
        {
            if (_config.version < Version)
            {
                if (_config.version <= default(VersionNumber))
                {
                    string prefix, prefixColor;
                    if (GetConfigValue(out prefix, "Chat prefix") && GetConfigValue(out prefixColor, "Chat prefix color"))
                    {
                        _config.chatS.prefix = $"<color={prefixColor}>{prefix}</color>: ";
                    }

                    ulong steamID;
                    if (GetConfigValue(out steamID, "Chat steamID icon"))
                    {
                        _config.chatS.steamIDIcon = steamID;
                    }
                }
                if (_config.version <= new VersionNumber(1, 2, 10))
                {
                    bool enabled;
                    if (GetConfigValue(out enabled, "Check empty cupboard"))
                    {
                        _config.checkEmptyCupboard = enabled;
                    }
                    if (GetConfigValue(out enabled, "Notify player, that his object will be removed"))
                    {
                        _config.notifyPlayer = enabled;
                    }

                    float time;
                    if (GetConfigValue(out time, "Check cupboard time (seconds)"))
                    {
                        _config.cupboardCheckTime = time;
                    }
                    if (GetConfigValue(out time, "Notify player interval"))
                    {
                        _config.notifyInterval = time;
                    }

                    Dictionary<string, object> decayList;
                    if (GetConfigValue(out decayList, "Decay entity list"))
                    {
                        foreach (var entry in decayList)
                        {
                            if (!_config.entityS.ContainsKey(entry.Key))
                            {
                                var jToken = JToken.FromObject(entry.Value);
                                _config.entityS.Add(entry.Key, new DecaySettings
                                {
                                    enabled = jToken["Enabled destroy"].ToObject<bool>(),
                                    onlyOwned = jToken["Check if it is a player's entity"].ToObject<bool>(),
                                    delayTime = jToken["Delay destroy time (seconds)"].ToObject<float>(),
                                    destroyTime = jToken["Tick rate (Damage per tick = max health / this)"].ToObject<float>(),
                                    tickRate = jToken["Destroy time (seconds)"].ToObject<float>(),
                                });
                            }
                        }
                    }
                }
                _config.version = Version;
            }
        }

        private bool GetConfigValue<T>(out T value, params string[] path)
        {
            var configValue = Config.Get(path);
            if (configValue != null)
            {
                if (configValue is T)
                {
                    value = (T)configValue;
                    return true;
                }
                try
                {
                    value = Config.ConvertValue<T>(configValue);
                    return true;
                }
                catch (Exception ex)
                {
                    PrintError($"GetConfigValue ERROR: path: {string.Join("\\", path)}\n{ex}");
                }
            }

            value = default(T);
            return false;
        }

        #endregion ConfigurationFile

        #region DataFile

        private StoredData storedData;

        private class StoredData
        {
            [JsonProperty("List of short prefab names for all combat entities")]
            public HashSet<string> entityShortPrefabNames = new HashSet<string>();
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
                    storedData = new StoredData();
                    UpdateData();
                }
            }
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);

        #endregion DataFile

        #region LanguageFile

        private void Print(BasePlayer player, string message) => Player.Message(player, message, _config.chatS.prefix, _config.chatS.steamIDIcon);

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["DESTROY"] = "If you do not install the cupboard, the object will <color=#F4D142>be deleted</color> after {0}."
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["DESTROY"] = "如果您一直不放置领地柜，该实体将在 {0} 后<color=#F4D142>被删除</color>"
            }, this, "zh-CN");
        }

        #endregion LanguageFile
    }
}