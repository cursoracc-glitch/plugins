using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("QuarryRefiner", "Vlad-00003", "1.1.1")]
    [Description("Automatically smelts ores gathers by quarries and excavator")]
    /*
     * Author info:
     *   E-mail: Vlad-00003@mail.ru
     *   Vk: vk.com/vlad_00003
     */
    class QuarryRefiner : RustPlugin
    {
        #region Vars ‌﻿‌‍‍​‍

        private PluginConfig _config;
        private PluginData _data;
        private readonly Dictionary<ItemDefinition, ItemModCookable> _itemToCookable = new Dictionary<ItemDefinition, ItemModCookable>();
        private ItemDefinition _coal;

        #endregion

        #region Configuration ‌﻿‌‍‍​‍

        private class BaseConfig
        {
            [JsonProperty("Переплавлять ресурсы")]
            public bool Use;
            [JsonProperty("Привилегия, необходимая для автоматической переплавки")]
            public string Permission;

            [JsonIgnore]
            private Permission _permission;

            #region Default Config ‌﻿‌‍‍​‍

            public static BaseConfig DefaultConfig => new BaseConfig
            {
                Use = true,
                Permission = nameof(QuarryRefiner) + ".Excavator"
            };

            #endregion

            public void Register(QuarryRefiner plugin)
            {
                _permission = plugin.permission;
                _permission.RegisterPermission(Permission, plugin);
            }

            public bool ShouldTransmute(string userId)
            {
                return Use && (string.IsNullOrEmpty(Permission) || _permission.UserHasPermission(userId, Permission));
            }
        }

        private class QuarryConfig : BaseConfig
        {
            [JsonProperty("Определять наличие привилегии по владельцу (false - последний запустивший)")]
            public bool DefineByOwner;

            #region Default Config ‌﻿‌‍‍​‍

            public new static QuarryConfig DefaultConfig => new QuarryConfig
            {
                Use = true,
                Permission = nameof(QuarryRefiner) + ".Quarry",
                DefineByOwner = false
            };

            public static QuarryConfig DefaultOil => new QuarryConfig
            {
                Use = false,
                Permission = nameof(QuarryRefiner) + ".Oil",
                DefineByOwner = false
            };

            #endregion
        }

        private class PluginConfig
        {
            [JsonProperty("Настройки экскаваторов")]
            public BaseConfig ExcavatorConfig;
            [JsonProperty("Настройки карьеров")]
            public QuarryConfig QuarryConfig;
            [JsonProperty("Настройка нефтекачек")]
            public QuarryConfig OilConfig;

            #region Default Config ‌﻿‌‍‍​‍

            public static PluginConfig DefaultConfig => new PluginConfig
            {
                ExcavatorConfig = BaseConfig.DefaultConfig,
                QuarryConfig = QuarryConfig.DefaultConfig,
                OilConfig = QuarryConfig.DefaultOil
            };

            #endregion

            public void Register(QuarryRefiner plugin)
            {
                ExcavatorConfig.Register(plugin);
                QuarryConfig.Register(plugin);
                OilConfig.Register(plugin);
            }

        }

        #endregion

        #region Data ‌﻿‌‍‍​‍

        private class PluginData
        {
            [JsonProperty("Последние запускавшие")]
            public readonly Dictionary<ulong, string> QuarryOwners = new Dictionary<ulong, string>();

            public void Cleanup()
            {
                foreach (var quarry in QuarryOwners.Keys.ToArray())
                {
                    if (BaseNetworkable.serverEntities.Find(new NetworkableId(quarry)) == null)
                        QuarryOwners.Remove(quarry);
                }
            }
        }

        #endregion

        #region Config and Data Initialization ‌﻿‌‍‍​‍

        #region Data ‌﻿‌‍‍​‍

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Title);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load data (is the file corrupt?) - no previously created recycles would work ({ex.Message})");
                _data = new PluginData();
            }
            _data.Cleanup();
        }
        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Title, _data);
        }

        #endregion

        #region Config ‌﻿‌‍‍​‍

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<PluginConfig>();
            }
            catch (Exception ex)
            {
                PrintError("Failed to load config file(is the config file corrupt ?)(" + ex.Message + ")");
            }
            if (ShouldUpdateConfig())
                SaveConfig();
            LoadData();
        }

        protected override void LoadDefaultConfig()
        {
            _config = PluginConfig.DefaultConfig;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        private bool ShouldUpdateConfig()
        {
            if (Config["Настройка нефтекачек"] == null)
            {
                _config.OilConfig = QuarryConfig.DefaultOil;
                PrintWarning("New option was added, check your config file");
                return true;
            }

            return false;
        }

        #endregion

        #endregion

        #region Initialization and quitting ‌﻿‌‍‍​‍

        private void Init()
        {
            _config.Register(this);
        }


        private void OnServerInitialized(bool initial)
        {
            foreach (var itemDefinition in ItemManager.GetItemDefinitions())
            {
                var cookable = itemDefinition.GetComponent<ItemModCookable>();
                if (cookable)
                    _itemToCookable[itemDefinition] = cookable;
            }

            _coal = ItemManager.FindItemDefinition("charcoal");
        }
        private void Unload() => OnServerSave();

        private void OnServerSave() => SaveData();

        private void Loaded()
        {
            if (!_config.QuarryConfig.Use && !_config.OilConfig.Use)
            {
                Unsubscribe(nameof(OnQuarryGather));
                Unsubscribe(nameof(OnQuarryToggled));
            }

            if (!_config.ExcavatorConfig.Use)
            {
                Unsubscribe(nameof(OnExcavatorGather));
                Unsubscribe(nameof(OnExcavatorResourceSet));
            }
        }

        #endregion

        #region Oxide Hooks ‌﻿‌‍‍​‍
        void OnQuarryToggled(MiningQuarry quarry, BasePlayer player)
        {
            if (!player || !quarry || !quarry.IsOn())
                return;
            _data.QuarryOwners[quarry.net.ID.Value] = player.UserIDString;
        }
        void OnExcavatorResourceSet(ExcavatorArm arm, string resource, BasePlayer player)
        {
            if (!player || !arm)
                return;
            _data.QuarryOwners[arm.net.ID.Value] = player.UserIDString;
        }
        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (!quarry.canExtractLiquid)
            {
                if (_config.QuarryConfig.ShouldTransmute(GetQuarryOwner(quarry)))
                    Transmute(item);
                return;
            }

            if (_config.OilConfig.ShouldTransmute(GetQuarryOwner(quarry)))
                Transmute(item);
        }

        void OnExcavatorGather(ExcavatorArm arm, Item item)
        {
            if (_config.ExcavatorConfig.ShouldTransmute(GetQuarryOwner(arm)))
                Transmute(item);
        }

        #endregion

        #region Helpers‌﻿‌‍‍​‍
        public string GetQuarryOwner(BaseEntity quarry)
        {
            string owner;
            if (quarry is MiningQuarry)
            {
                if (!_config.QuarryConfig.DefineByOwner)
                    return _data.QuarryOwners.TryGetValue(quarry.net.ID.Value, out owner) ? owner : "0";
                if (quarry.OwnerID != 0)
                    return quarry.OwnerID.ToString();
                return _data.QuarryOwners.TryGetValue(quarry.net.ID.Value, out owner) ? owner : "0";
            }

            if (quarry.OwnerID != 0 || !_data.QuarryOwners.TryGetValue(quarry.net.ID.Value, out owner))
                owner = quarry.OwnerID.ToString();
            return owner;
        }

        private void Transmute(Item item)
        {
            if (item.info.shortname == "wood")
            {
                item.info = _coal;
                return;
            }

            if (!_itemToCookable.ContainsKey(item.info))
                return;
            var cookable = _itemToCookable[item.info];
            item.info = cookable.becomeOnCooked;
            item.amount *= cookable.amountOfBecome;
        }
        #endregion
    }
}
