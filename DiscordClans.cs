using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Messages.Embeds;
using Oxide.Ext.Discord.Entities.Permissions;
using Oxide.Ext.Discord.Logging;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("DiscordClans", "k1lly0u", "0.1.2"), Description("Log ClansReborn events to Discord")]
    class DiscordClans : RustPlugin
    {
        #region Fields
        [DiscordClient]
        private DiscordClient Client;

        private DiscordGuild Guild;

        private bool ConnectionExists { get; set; } = false;
        #endregion

        #region Oxide Hooks     
        private void Unload()
        {
            if (Client != null)
            {
                Client.Disconnect();
                Client = null;
            }

            Guild = null;
        }
        #endregion

        #region Discord Hooks       
        private void OnDiscordClientCreated()
        {
            if (string.IsNullOrEmpty(configData.Discord.APIKey))
            {
                PrintError("No API token set in config... Unable to continue!");
                return;
            }

            if (string.IsNullOrEmpty(configData.Discord.BotID))
            {
                PrintError("No bot client ID set in config... Unable to continue!");
                return;
            }

            Puts("Establishing connection to your Discord server...");

            DiscordSettings settings = new DiscordSettings();
            settings.ApiToken = configData.Discord.APIKey;
            settings.LogLevel = configData.Discord.LogLevel;
            settings.Intents = GatewayIntents.Guilds;
           
            Client.Connect(settings);
        }

        private void OnDiscordGatewayReady(GatewayReadyEvent ready) 
        {
            if (ready.Guilds.Count == 0)
            {
                PrintError("Failed to find your bot in any guilds. Unable to continue...");
                return;
            }

            Guild = ready.Guilds.Values.FirstOrDefault();

            if (Guild == null)
            {
                PrintError("Failed to connect to guild. Unable to continue...");
                return;
            }

            Puts($"Connection to {Guild.Name} established! DiscordClans is now active");
            ConnectionExists = true;
        }
        #endregion
       
        #region API
        private enum MessageType { Create, Invite, InviteReject, InviteWithdrawn, Join, Leave, Kick, Promote, Demote, Disband, AllianceInvite, AllianceInviteReject, AllianceInviteWithdrawn, AllianceAccept, AllianceWithdrawn, TeamChat, ClanChat, AllyChat }

        private void LogMessage(string message, int messageType)
        {
            if (!ConnectionExists)
                return;

            ConfigData.LogSettings logSettings;

            if (!configData.Log.TryGetValue((MessageType)messageType, out logSettings) || !logSettings.Enabled)
                return;

            if (string.IsNullOrEmpty(logSettings.Channel))
                return;

            DiscordChannel channel = Guild.GetChannel(logSettings.Channel);
            if (channel == null)
                return;

            DiscordEmbed embed = new DiscordEmbed
            {
                Title = $"Clan Log - {(MessageType)messageType}",
                Description = message,
                Color = new DiscordColor(logSettings.Color),
                Footer = new EmbedFooter { Text = $"{DateTime.Now.ToLongDateString()}, {DateTime.Now.ToLongTimeString()}" }
            };

            channel.CreateMessage(Client, embed);            
        }
        #endregion

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Discord Settings")]
            public DiscordSettings Discord { get; set; }

            [JsonProperty(PropertyName = "Log Settings")]
            public Hash<MessageType, LogSettings> Log { get; set; }

            public class DiscordSettings
            {
                [JsonProperty(PropertyName = "Bot Token")]
                public string APIKey { get; set; }

                [JsonProperty(PropertyName = "Bot Client ID")]
                public string BotID { get; set; }

                [JsonConverter(typeof(StringEnumConverter))]
                [JsonProperty(PropertyName = "Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
                public DiscordLogLevel LogLevel { get; set; }
            }

            public class LogSettings
            {
                [JsonProperty(PropertyName = "Logs enabled for this message type")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Log Channel Name")]
                public string Channel { get; set; }

                [JsonProperty(PropertyName = "Embed Color (hex)")]
                public string Color { get; set; }
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
                Discord = new ConfigData.DiscordSettings
                {
                    APIKey = string.Empty,
                    BotID = string.Empty,
                    LogLevel = DiscordLogLevel.Info
                },
                Log = new Hash<MessageType, ConfigData.LogSettings>
                {
                    [MessageType.Create] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#e91e63",
                        Enabled = true
                    },
                    [MessageType.Invite] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#9b59b6",
                        Enabled = true
                    },
                    [MessageType.InviteReject] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#71368a",
                        Enabled = true
                    },
                    [MessageType.InviteWithdrawn] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#71368a",
                        Enabled = true
                    },
                    [MessageType.Join] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#3498db",
                        Enabled = true
                    },
                    [MessageType.Leave] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#206694",
                        Enabled = true
                    },
                    [MessageType.Kick] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#992d22",
                        Enabled = true
                    },
                    [MessageType.Promote] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#f1c40f",
                        Enabled = true
                    },
                    [MessageType.Demote] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#c27c0e",
                        Enabled = true
                    },
                    [MessageType.Disband] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#ad1457",
                        Enabled = true
                    },
                    [MessageType.AllianceInvite] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#95a5a6",
                        Enabled = true
                    },
                    [MessageType.AllianceInviteReject] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#95a5a6",
                        Enabled = true
                    },
                    [MessageType.AllianceInviteWithdrawn] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#95a5a6",
                        Enabled = true
                    },
                    [MessageType.AllianceAccept] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#95a5a6",
                        Enabled = true
                    },
                    [MessageType.AllianceWithdrawn] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#95a5a6",
                        Enabled = true
                    },
                    [MessageType.TeamChat] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#607d8b",
                        Enabled = true
                    },
                    [MessageType.ClanChat] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#607d8b",
                        Enabled = true
                    },
                    [MessageType.AllyChat] = new ConfigData.LogSettings
                    {
                        Channel = "clanlog",
                        Color = "#607d8b",
                        Enabled = true
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 1, 1))
            {
                configData.Log[MessageType.TeamChat] = baseConfig.Log[MessageType.TeamChat];
                configData.Log[MessageType.ClanChat] = baseConfig.Log[MessageType.ClanChat];
                configData.Log[MessageType.AllyChat] = baseConfig.Log[MessageType.AllyChat];
            }

            if (configData.Version < new VersionNumber(0, 1, 2))
            {
                configData.Discord.LogLevel = DiscordLogLevel.Info;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }
        #endregion
    }
}
