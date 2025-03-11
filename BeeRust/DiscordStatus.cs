using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Activities;
using Oxide.Ext.Discord.Entities.Applications;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Commands;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Messages.Embeds;
using Oxide.Ext.Discord.Entities.Permissions;
using Oxide.Ext.Discord.Libraries.Linking;
using Oxide.Ext.Discord.Logging;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("Discord Status", "Gonzi", "4.0.1")]
    [Description("Shows server information as a discord bot status")]

    public class DiscordStatus : CovalencePlugin
    {
        private string seperatorText = string.Join("-", new string[25 + 1]);
        private bool enableChatSeparators;

        #region Fields

        [DiscordClient]
        private DiscordClient Client;

        private readonly DiscordSettings _settings = new DiscordSettings
        {
            Intents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.GuildMembers
        };
        
        private DiscordGuild _guild;
        
        private readonly DiscordLink _link = GetLibrary<DiscordLink>();

        Configuration config;
        private int statusIndex = -1;
        private string[] StatusTypes = new string[]
        {
            "Game",
            "Stream",
            "Listen",
            "Watch"
        };

        #endregion

        #region Config
        class Configuration
        {
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string BotToken = string.Empty;
            
            [JsonProperty(PropertyName = "Discord Server ID (Optional if bot only in 1 guild)")]
            public Snowflake GuildId { get; set; }

            [JsonProperty(PropertyName = "Prefix")]
            public string Prefix = "!";

            [JsonProperty(PropertyName = "Discord Group Id needed for Commands (null to disable)")]
            public Snowflake? GroupId;

            [JsonProperty(PropertyName = "Update Interval (Seconds)")]
            public int UpdateInterval = 5;

            [JsonProperty(PropertyName = "Randomize Status")]
            public bool Randomize = false;

            [JsonProperty(PropertyName = "Status Type (Game/Stream/Listen/Watch)")]
            public string StatusType = "Game";

            [JsonProperty(PropertyName = "Status", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Status = new List<string>
            {
                "{players.online} / {server.maxplayers} Online!",
                "{server.entities} Entities",
                "{players.sleepers} Sleepers!",
                "{players.authenticated} Linked Account(s)"
            };
            
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(DiscordLogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public DiscordLogLevel ExtensionDebugging { get; set; } = DiscordLogLevel.Info;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Title"] = "Players List",
                ["Players"] = "Online Players [{0}/{1}] 🎆\n {2}",
                ["IPAddress"] = "steam://connect/{0}:{1}"

            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Title"] = "플레이어 목록",
                ["Players"] = "접속중인 플레이어 [{0}/{1}] 🎆\n {2}",
                ["IPAddress"] = "steam://connect/{0}:{1}"
            }, this, "kr");
        }

        private string Lang(string key, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this), args);
        }

        #endregion

        #region Discord
        public DiscordEmbed ServerStats(string content)
        {
            DiscordEmbed embed = new DiscordEmbed
            {
                Title = Lang("Title", ConVar.Server.hostname),
                Description = content,
                Thumbnail = new EmbedThumbnail
                {
                    Url = $"{ConVar.Server.headerimage}"
                },
                Footer = new EmbedFooter
                {
                    Text = $"Gonzi V{Version}",
                    IconUrl = "https://cdn.discordapp.com/avatars/321373026488811520/08f996472c573473e7e30574e0e28da0.png"
                },

                Color = new DiscordColor(15158332)
            };
            return embed;
        }
        
        [HookMethod(DiscordHooks.OnDiscordGuildMessageCreated)]
        void OnDiscordGuildMessageCreated(DiscordMessage message)
        {
            if (message.Author.Bot == true) return;


            if (message.Content[0] == config.Prefix[0])
            {

                string cmd;
                try
                {
                    cmd = message.Content.Split(' ')[0].ToLower();
                    if (string.IsNullOrEmpty(cmd.Trim()))
                        cmd = message.Content.Trim().ToLower();
                }
                catch
                {
                    cmd = message.Content.Trim().ToLower();
                }

                cmd = cmd.Remove(0, 1);

                cmd = cmd.Trim();
                cmd = cmd.ToLower();

                DiscordCMD(cmd, message);
            }
        }

        private void DiscordCMD(string command, DiscordMessage message)
        {
            if (config.GroupId.HasValue && !message.Member.Roles.Contains(config.GroupId.Value)) return;

            switch (command)
            {
                case "players":
                    {
                        string maxplayers = Convert.ToString(ConVar.Server.maxplayers);
                        string onlineplayers = Convert.ToString(BasePlayer.activePlayerList.Count);
                        string list = string.Empty;
                        var playerList = BasePlayer.activePlayerList;
                        foreach (var player in playerList)
                        {
                            list += $"[{player.displayName}](https://steamcommunity.com/profiles/{player.UserIDString}/) \n";
                        }

                        DiscordChannel.GetChannel(Client, message.ChannelId, channel =>
                        {
                            channel.CreateMessage(Client, ServerStats(Lang("Players", BasePlayer.activePlayerList.Count, ConVar.Server.maxplayers, list)));
                        });
                        break;
                    }
                case "ip":
                    {
                        DiscordChannel.GetChannel(Client, message.ChannelId, channel =>
                        {
                            webrequest.Enqueue("http://icanhazip.com", "", (code, response) =>
                            {
                                string ip = response.Trim();
                                channel.CreateMessage(Client, Lang("IPAddress", ip, ConVar.Server.port));
                            }, this);
                        });
                    }
                    break;
            }
        }

        #endregion

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            lang.SetServerLanguage("en");

            if (config.BotToken == string.Empty)
                return;

            _settings.ApiToken = config.BotToken;
            _settings.LogLevel = config.ExtensionDebugging;
            Client.Connect(_settings);

            timer.Every(config.UpdateInterval, () => UpdateStatus());
        }
        
        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            if (ready.Guilds.Count == 0)
            {
                PrintError("Your bot was not found in any discord servers. Please invite it to a server and reload the plugin.");
                return;
            }

            DiscordGuild guild = null;
            if (ready.Guilds.Count == 1 && !config.GuildId.IsValid())
            {
                guild = ready.Guilds.Values.FirstOrDefault();
            }

            if (guild == null)
            {
                guild = ready.Guilds[config.GuildId];
            }

            if (guild == null)
            {
                PrintError("Failed to find a matching guild for the Discord Server Id. " +
                           "Please make sure your guild Id is correct and the bot is in the discord server.");
                return;
            }
                
            if (Client.Bot.Application.Flags.HasValue && !Client.Bot.Application.Flags.Value.HasFlag(ApplicationFlags.GatewayGuildMembersLimited))
            {
                PrintError($"You need to enable \"Server Members Intent\" for {Client.Bot.BotUser.Username} @ https://discord.com/developers/applications\n" +
                           $"{Name} will not function correctly until that is fixed. Once updated please reload {Name}.");
                return;
            }
            
            _guild = guild;
        }
        #endregion

        #region Discord Hooks

        #endregion

        #region Status Update
        private void UpdateStatus()
        {
            try
            {
                if (config.Status.Count == 0)
                    return;

                var index = GetStatusIndex();

                Client.Bot.UpdateStatus(new UpdatePresenceCommand
                {
                    Activities = new List<DiscordActivity>
                    {
                        new DiscordActivity
                        {
                            Name = Format(config.Status[index]),
                            Type = ActivityType.Game
                        }
                    }
                });

                statusIndex = index;
            }
            catch (Exception err)
            {
                LogToFile("DiscordStatus", $"{err}", this);
            }
        }
        #endregion

        #region Helper Methods
        private int GetStatusIndex()
        {
            if (!config.Randomize)
                return (statusIndex + 1) % config.Status.Count;

            var index = 0;
            do index = Random.Range(0, config.Status.Count - 1);
            while (index == statusIndex);

            return index;
        }

        private ActivityType GetStatusType()
        {
            if (!StatusTypes.Contains(config.StatusType))
                PrintError($"Unknown Status Type '{config.StatusType}'");

            switch (config.StatusType)
            {
                case "Game":
                    return ActivityType.Game;
                case "Stream":
                    return ActivityType.Streaming;
                case "Listen":
                    return ActivityType.Listening;
                case "Watch":
                    return ActivityType.Watching;
                default:
                    return default(ActivityType);
            }
        }

        private string Format(string message)
        {
            message = message
                .Replace("{guild.name}", _guild.Name ?? "{unknown}")
                .Replace("{members.total}", _guild.MemberCount?.ToString() ?? "{unknown}")
                .Replace("{channels.total}", _guild.Channels?.Count.ToString() ?? "{unknown}")
                .Replace("{server.hostname}", server.Name)
                .Replace("{server.maxplayers}", server.MaxPlayers.ToString())
                .Replace("{players.online}", players.Connected.Count().ToString())
                .Replace("{players.authenticated}", GetAuthCount().ToString());

#if RUST
        message = message
            .Replace("{server.ip}", ConVar.Server.ip)
            .Replace("{server.port}", ConVar.Server.port.ToString())
            .Replace("{server.entities}", BaseNetworkable.serverEntities.Count.ToString())
            .Replace("{server.worldsize}", ConVar.Server.worldsize.ToString())
            .Replace("{server.seed}", ConVar.Server.seed.ToString())
            .Replace("{server.fps}", Performance.current.frameRate.ToString())
            .Replace("{server.avgfps}", Convert.ToInt32(Performance.current.frameRateAverage).ToString())
            .Replace("{players.queued}", ConVar.Admin.ServerInfo().Queued.ToString())
            .Replace("{players.joining}", ConVar.Admin.ServerInfo().Joining.ToString())
            .Replace("{players.sleepers}", BasePlayer.sleepingPlayerList.Count.ToString())
            .Replace("{players.total}", (players.Connected.Count() + BasePlayer.sleepingPlayerList.Count).ToString());
#endif

            return message;
        }

        private int GetAuthCount() => _link.GetLinkedCount();

        #endregion
    }
}