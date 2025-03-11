using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("NoHeliAmmoFireWall", "Tryhard", "1.0.5")]
    [Description("Убирает огонь от коптера . огонь от зажигалок")]
    public class NoHeliAmmoFireWall : RustPlugin
    {

        private ConfigData configData = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Enable plugin")]
            public bool enable = true;
            [JsonProperty(PropertyName = "Disable minicopter gibs")]
            public bool mGibs = true;
            [JsonProperty(PropertyName = "Disable minicopter fire")]
            public bool mFire = true;
            [JsonProperty(PropertyName = "Disable minicopter explosion sound")]
            public bool mExplo = true;
            [JsonProperty(PropertyName = "Disable scraphelicopter gibs")]
            public bool sGibs = true;
            [JsonProperty(PropertyName = "Disable scraphelicopter explosion sound")]
            public bool sExplo = true;
            [JsonProperty(PropertyName = "Disable scraphelicopter fire ")]
            public bool sFire = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                configData = Config.ReadObject<ConfigData>();

                if (configData == null) LoadDefaultConfig();
            }

            catch
            {
                PrintError("Configuration file is corrupt, check your config file at https://jsonlint.com/!");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => configData = new ConfigData();

        protected override void SaveConfig() => Config.WriteObject(configData);


        private void OnEntitySpawned(ScrapTransportHelicopter entity)
        {
            if (configData.enable)
            {
                if (configData.sExplo) entity.explosionEffect.guid = null;
                {
                    if (configData.sFire) entity.fireBall.guid = null;
                    {
                        if (configData.sGibs) entity.serverGibs.guid = null;
                    }
                }
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.PrefabName == "assets/bundled/prefabs/fireball_small.prefab" || entity.PrefabName == "assets/bundled/prefabs/fireball_small_shotgun.prefab" || entity.PrefabName == "assets/bundled/prefabs/fireball.prefab")
                entity.Kill();
        }

        private void OnEntitySpawned(Minicopter entity)
        {
            if (configData.enable)
            {
                if (configData.mExplo) entity.explosionEffect.guid = null;
                {
                    if (configData.mFire) entity.fireBall.guid = null;
                    {
                        if (configData.mGibs) entity.serverGibs.guid = null;
                    }
                }
            }
        }
    }
}