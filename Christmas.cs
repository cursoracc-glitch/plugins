using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Christmas", "", "1.0.0")]
    [Description("")]

    public class Christmas : RustPlugin
    {
        private const string PLAYER_PERM = "christmas.use";
        private static Christmas ins { get; set; }
        #region Functions
        private void Init()
        {
            ins = this;
            permission.RegisterPermission(PLAYER_PERM, this);

            ConVar.XMas.enabled = true;
            ConVar.XMas.spawnRange = configData.Automation.playerDistance;
            ConVar.XMas.giftsPerPlayer = configData.Automation.giftsPerPlayer;

            timer.Every(configData.Automation.refillTime * 60, () =>
            {
                RefillPresents();
                if (configData.Automation.messagesEnabled)
                    PrintWarning("PresentPizdec");
            });
        }

        private void Unload()
        {
            Puts("Disabling the Christmas event...");
            ConVar.XMas.enabled = false;
            ins = null;
        }

        public void RefillPresents() => ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "xmas.refill");

        #endregion

        

        [ConsoleCommand("giftwqdwqwqdwqdwq")]
        private void GiftsConsole(ConsoleSystem.Arg arg)
        {
            RefillPresents();
        }

        private static ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Event Automation Settings")]
            public AutomationOptions Automation { get; set; }

            public class AutomationOptions
            {
                [JsonProperty(PropertyName = "Time in-between presents and stocking refills (minutes)")]
                public int refillTime { get; set; }
                [JsonProperty(PropertyName = "Distance a player in which to spawn")]
                public int playerDistance { get; set; }
                [JsonProperty(PropertyName = "Gifts per player")]
                public int giftsPerPlayer { get; set; }
                [JsonProperty(PropertyName = "Broadcast Message enabled to players when gifts sent (true/false)")]
                public bool messagesEnabled { get; set; }
            }


            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Automation = new ConfigData.AutomationOptions
                {
                    refillTime = 50,
                    playerDistance = 50,
                    giftsPerPlayer = 1,
                    messagesEnabled = true
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new Core.VersionNumber(2, 0, 0))
                configData = baseConfig;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

      
    }
}