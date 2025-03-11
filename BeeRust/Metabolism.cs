using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Metabolism", "Menevt", "1.0.0")]
    [Description("Modify or disable player metabolism stats")]
    public class Metabolism : RustPlugin
    {
        #region Oxide Hooks
        
        private void Init()
        {
            foreach (var value in config.permissions.Keys)
            {
                permission.RegisterPermission(value, this);
            }
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            foreach (var pair in config.permissions)
            {
                if (permission.UserHasPermission(player.UserIDString, pair.Key))
                {
                    var data = pair.Value;

                    var health = data.health;
                    if (health > 100f)
                    {
                        player._maxHealth = health;
                    }

                    var hydration = data.hydration;
                    if (hydration > 250)
                    {
                        player.metabolism.hydration.max = hydration;
                    }

                    var calories = data.calories;
                    if (calories > 500)
                    {
                        player.metabolism.calories.max = calories;
                    }
                    
                    player.health = health;
                    player.metabolism.hydration.value = hydration;
                    player.metabolism.calories.value = calories;
                    player.SendNetworkUpdate();
                    break;
                }
            }
        }

        #endregion
        
        #region Configuration
        
        private static ConfigData config;
        
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Permission -> Settings")]
            public Dictionary<string, MetabolismSettings> permissions;
        }

        private class MetabolismSettings
        {
            [JsonProperty(PropertyName = "Water on respawn")]
            public float hydration;
            
            [JsonProperty(PropertyName = "Calories on respawn")]
            public float calories;
            
            [JsonProperty(PropertyName = "Health on respawn")]
            public float health;
        }
        
        private ConfigData GetDefaultConfig()
        {
            return new ConfigData 
            {
                permissions = new Dictionary<string, MetabolismSettings>
                {
                    ["metabolism.3"] = new MetabolismSettings
                    {
                        hydration = 5000,
                        calories = 5000,
                        health = 100
                    },
                    ["metabolism.2"] = new MetabolismSettings
                    {
                        hydration = 500,
                        calories = 500,
                        health = 100
                    },
                    ["metabolism.1"] = new MetabolismSettings
                    {
                        hydration = 250,
                        calories = 250,
                        health = 100
                    },
                }
            };
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
   
            try
            {
                config = Config.ReadObject<ConfigData>();
        
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Configuration file is corrupt(or not exists), creating new one!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        
        #endregion
    }
}
