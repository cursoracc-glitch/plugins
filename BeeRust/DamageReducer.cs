using Newtonsoft.Json;
using Rust;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DamageReducer", "Reheight", "1.0.0")]
    [Description("Allows you to change the damage of specific entities.")]
    public class DamageReducer : RustPlugin
    {
        PluginConfig _config;

        private void Init()
        {
            _config = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig() => _config = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                {
                    throw new JsonException();
                }

                if (!_config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintWarning($"PluginConfig file {Name}.json updated.");

                    SaveConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();

                PrintError("Config file contains an error and has been replaced with the default file.");
            }

        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Damage Scale", Order = 0)]
            public Dictionary<string, float> DamageScale { get; set; }

            [JsonProperty(PropertyName = "Debug Mode", Order = 1)]
            public bool debug { get; set; }

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                DamageScale = new Dictionary<string, float>()
                {
                    { "rocket_hv", 56f },
                    { "40mm_grenade_he", 70f },
                    { "grenade.f1.deployed", 60f }
                },
                debug = false
            };
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null || entity is BasePlayer == false) return;

            DamageType type = hitInfo?.damageTypes.GetMajorityDamageType() ?? DamageType.Generic;

            if (type == DamageType.Suicide) return;

            BasePlayer attacker = hitInfo?.Initiator as BasePlayer;
            if (attacker == null) return;

            if (hitInfo.WeaponPrefab)
            {
                if (_config.debug)
                {
                    Puts(type.ToString());
                    Puts(hitInfo.WeaponPrefab.ShortPrefabName);
                }

                float damagePercentage;

                if (!_config.DamageScale.TryGetValue(hitInfo.WeaponPrefab.ShortPrefabName, out damagePercentage))
                {
                    return;
                }

                hitInfo.damageTypes.ScaleAll(damagePercentage / 100);

                if (_config.debug)
                {
                    Puts((damagePercentage / 100).ToString());
                }
            }
        }
    }
}