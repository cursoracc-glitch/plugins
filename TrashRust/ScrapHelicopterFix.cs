using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("ScrapHelicopterFix", "", "0.1.1")]
    [Description("Fixes scrap copters, and their elements")]
    class ScrapHelicopterFix : RustPlugin
    {
        static PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Thank you for downloading the plugin from RustPlugin.ru. <3 OxideBro");
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
                if (config.PluginVersion < new VersionNumber(0, 1, 1))
                {
                    PrintWarning("Config update detected! Updating config values...");
                    config.configMinicopter = new ConfigMinicopter()
                    {
                        DisabledEffectsMinicopter = false,
                        DisableFireBallsMinicopter = false
                    };
                    PrintWarning("Config update completed!");
                }

                config.PluginVersion = Version;
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }


        public class ConfigMinicopter
        {
            [JsonProperty("Не создавать огонь при взрыве Миникоптера | Do not create fire when the Minicopters explodes")]
            public bool DisableFireBallsMinicopter = false;

            [JsonProperty("Убрать еффект врзыва Миникоптера при его разрушении | Remove the effect of the Minicopters burst when it is destroyed")]
            public bool DisabledEffectsMinicopter = false;
        }

        private class PluginConfig
        {
            [JsonProperty("Не создавать огонь при взрыве коптера | Do not create fire when the ScrapHelicopter explodes")]
            public bool DisableFireBalls = false;

            [JsonProperty("Исправление позиции огня при разрушении коптера | Correcting the position of FireBalls when destroying the ScrapHelicopter")]
            public bool EnabledFixesFireBalls = false;

            [JsonProperty("Количество созданных fireballs (Если включен фикс позиции, стандарт 12) | Count of fireballs created (If fixed position is enabled, default 12)")]
            public int FireBallsCount = 12;

            [JsonProperty("Убрать части коптера при его разрушении | Remove server gibs of the ScrapHelicopter when it is destroyed")]
            public bool DisableServerGibs = false;

            [JsonProperty("Убрать еффект врзыва коптера при его разрушении | Remove the effect of the ScrapHelicopter burst when it is destroyed")]
            public bool DisabledEffects = false;



            [JsonProperty("Настройки разрушения Миникоптера | Minicopter destruction settings")]
            public ConfigMinicopter configMinicopter;


            [JsonProperty("Configuration version")]
            public VersionNumber PluginVersion = new VersionNumber();
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    PluginVersion = new VersionNumber(),
                    configMinicopter = new ConfigMinicopter()
                };
            }
        }
        void OnEntityDeath(Minicopter entity, HitInfo info)
        {
            if (entity == null || entity?.net.ID == null || info == null) return;
            if (entity is ScrapTransportHelicopter)
            {
                if (entity.fireBall != null)
                {
                    if (config.DisableFireBalls || config.EnabledFixesFireBalls)
                    {
                        entity.fireBall.guid = null;
                        if (config.EnabledFixesFireBalls && config.FireBallsCount > 0)
                        {
                            for (int i = 0; i < config.FireBallsCount; i++)
                            {
                                BaseEntity fireballs = GameManager.server.CreateEntity("assets/bundled/prefabs/oilfireballsmall.prefab", entity.transform.position);
                                if (fireballs)
                                {
                                    fireballs.enableSaving = false;
                                    fireballs.transform.position += new Vector3(UnityEngine.Random.Range(-4f, 4f), 1.5f, UnityEngine.Random.Range(-4f, 4f));
                                    fireballs.Spawn();
                                }
                            }
                        }
                    }
                    if (config.DisableServerGibs && entity.serverGibs != null)
                        entity.serverGibs.guid = null;
                    if (entity.explosionEffect != null && config.DisabledEffects)
                        entity.explosionEffect.guid = null;
                }
                return;
            }

            if (entity.explosionEffect != null && config.configMinicopter.DisabledEffectsMinicopter || config.configMinicopter.DisableFireBallsMinicopter && entity.fireBall != null)
            {
                if (entity.explosionEffect != null && config.configMinicopter.DisabledEffectsMinicopter)
                    entity.explosionEffect.guid = null;
                if (entity.fireBall != null && config.configMinicopter.DisableFireBallsMinicopter)
                    entity.fireBall.guid = null;
            }
        }
    }
}