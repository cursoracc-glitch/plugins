using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Always Day", "Orange", "1.0.1")]
    [Description("Stops time on your server at one time")]
    public class AlwaysDay : RustPlugin
    {
        #region Vars

        private TOD_Time time;

        #endregion
        
        #region Oxide Hooks

        private void OnServerInitialized()
        {
            time = UnityEngine.Object.FindObjectOfType<TOD_Time>();
            time.ProgressTime = false;
            Server.Command($"env.time {config.time}");
        }

        private void Unload()
        {
            time.ProgressTime = true;
        }

        #endregion
        
        #region Configuration 1.1.2

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Time")]
            public string time;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                time =  "12"
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
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                
                timer.Every(10f, () =>
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                });
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}