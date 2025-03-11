using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("GatherPrefab", "King.", "1.0.0")]
    public class GatherPrefab : RustPlugin
    {
        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 0, 0))
                {
                    //
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("При уничтожении какого обьекта будет даваться предмет из конфига [ Entity - кол-во ]")]
            public Dictionary<string, int> GatherPrefab;

            [JsonProperty("ShortName выдаваемого предмета")]
            public string ShortName;

            [JsonProperty("Использовать рандом ?")]
            public bool UseRandom;

            [JsonProperty("С какой вероятностью будет падать предмет из конфига ( При использовании рандома )")]
            public int RandomValue;

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    GatherPrefab = new Dictionary<string, int>()
                    {
                        ["assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab"] = 30,
                        ["assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab"] = 50,
                        ["assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab"] = 40
                    },
                    ShortName = "hq.metal.ore",
                    RandomValue = 50,
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion

        #region [Rust-Api]
        private void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (info == null || entity?.net?.ID == null) return;

            var player = info?.InitiatorPlayer;
            if (player == null) return;
            
            if (config.GatherPrefab.ContainsKey(entity.PrefabName))
            {
                if (!config.GatherPrefab.ContainsKey(entity.PrefabName)) return;
                if (config.UseRandom)
                {
                    if (UnityEngine.Random.Range(0f, 100f) < config.RandomValue)
                    {
                        var item = ItemManager.CreateByName(config.ShortName, config.GatherPrefab[entity.PrefabName]);
                        player.GiveItem(item);
                    }
                }
                else
                {
                    var item = ItemManager.CreateByName(config.ShortName, config.GatherPrefab[entity.PrefabName]);
                    player.GiveItem(item);
                }
            }
        }
        #endregion
    }
}