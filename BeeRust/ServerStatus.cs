using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core;
using System;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using System.Linq;
using ConVar;
using Oxide.Game.Rust.Libraries;

namespace Oxide.Plugins
{
    [Info("Server Status", "UNKN0WN", "1.3.0")]
    [Description("Server Status Check for discord Webhook")]
    class ServerStatus : RustPlugin
    {
        private Configuration _config;
        [PluginReference] Plugin SmoothRestart;
        private bool isQuit = false;
        private bool isSR = false;

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (null != arg)
            {
                string commandName = arg.cmd.Name;
                string[] args = arg.Args;

                if (null == commandName) return null;
                if(isSR == true)
                {
                    if("sr.restart".Equals(commandName))
                    {
                        string time = "300";
                        string reason = "Unknown";
                        if (null != args)
                        {
                            if("stop".Equals(args[0]))
                            {
                                SendMessage(Lang("Restart Cancel"), Lang("Restart Cancel Descriptions"));
                                Puts("Restart has been cancelled.");
                                return null;
                            }
                            else
                            {
                                if (2 <= args.Length)
                                {
                                    time = args[0];
                                    reason = "";
                                    for (int i = 1; i < args.Length; i++)
                                    {
                                        reason += args[i];
                                        if (i < args.Length - 1) reason += " ";
                                    }
                                }
                                else
                                {
                                    time = args[0];
                                }
                            }
                        }

                        var list = new Dictionary<string, string>
                        {
                            { "time", time },
                            { "reason", reason }
                        };
                        SendMessage(Lang("Restart"), Lang("Restart Descriptions", list));
                    }
                }
                if(isSR == false)
                {
                    if ("restart".Equals(commandName))
                    {
                        string time = "300";
                        string reason = "Unknown";
                        if (null != args)
                        {
                            if ("-1".Equals(args[0]))
                            {
                                SendMessage(Lang("Restart Cancel"), Lang("Restart Cancel Descriptions"));
                                Puts("Restart has been cancelled.");
                                return null;
                            }
                            else
                            {
                                if (2 <= args.Length)
                                {
                                    time = args[0];
                                    reason = "";
                                    for (int i = 1; i < args.Length; i++)
                                    {
                                        reason += args[i];
                                        if (i < args.Length - 1) reason += " ";
                                    }
                                }
                                else
                                {
                                    time = args[0];
                                }
                            }
                        }
                        var list = new Dictionary<string, string>
                        {
                            { "time", time },
                            { "reason", reason }
                        };
                        SendMessage(Lang("Restart"), Lang("Restart Descriptions", list));
                    }
                }
                
                if ("quit".Equals(commandName))
                {
                    timer.Once(3f, () =>
                    {
                        isQuit = true;
                        Server.Command("quit");
                    });

                    if (!isQuit)
                    {
                        SendMessage(Lang("Quit"), Lang("Quit Descriptions"));
                        return isQuit;
                    }
                }
            }

            return null;
        }

        private void OnServerInitialized()
        {
            if (_config.webhook == "webhookurl" || _config.webhook == null || _config.webhook == string.Empty)
            {
                PrintWarning("Change WebHook URL");
                return;
            }

            if(SmoothRestart != null)
            {
                isSR = true;
                PrintWarning("SmoothRestart Plugins Allowed");
            }

            SendMessage(Lang("Online"), Lang("Online Descriptions"));
        }

        #region Config
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(_config);

        private class Configuration
        {
            [JsonProperty("Discord WebHook")]
            public string webhook { get; set; } = "webhookurl";

            [JsonProperty("Embed Fields Time Format")]
            public string TimeFormat { get; set; } = "MM/dd/yyyy HH:mm:ss";

            [JsonProperty("Selection Mention (0 - none | 1 - @here | 2 - @everyone | 3 - @something)")]
            public int SelectionMention { get; set; } = 0;

            [JsonProperty("Designated mention")]
            public string DesignatedMention = "@something";
        }
        #endregion
        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Title"] = "Server Status 💫",
                ["Online"] = "📡 Server is online | ✅",
                ["Quit"] = "📡 Server is offline | ❌",
                ["Restart"] = "📡 The server has started restarting | ⏳",
                ["Restart Cancel"] = "📡 The server has canceled the restart | ⏳",
                ["Time"] = "Time:",
                ["Descriptions"] = "Descriptions:",
                ["Online Descriptions"] = "🎈 Server is Online",
                ["Quit Descriptions"] = "🎈 Server is Offline",
                ["Restart Descriptions"] = "🎈 The server shuts down after {time} seconds.\n\n🎈 Reason: {reason}",
                ["Restart Cancel Descriptions"] = "🎈 Server is Cancel Restart",
                ["Unknown"] = "Unknown"

            }, this, "en");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Title"] = "서버 상태 💫",
                ["Online"] = "📡 서버 시작 | ✅",
                ["Quit"] = "📡 서버 중지. | ❌",
                ["Restart"] = "📡 서버 재시작 | ⏳",
                ["Restart Cancel"] = "📡 재시작 취소| ⏳",
                ["Time"] = "시간:",
                ["Descriptions"] = "설명:",
                ["Online Descriptions"] = "🎈 서버가 시작되었습니다.",
                ["Quit Descriptions"] = "🎈 서버가 중지되었습니다.",
                ["Restart Descriptions"] = "🎈 서버가  {time} 초 후에 재시작 됩니다.\n\n🎈 이유: {reason}",
                ["Restart Cancel Descriptions"] = "🎈 서버 재시작이 취소되었습니다.",
                ["Unknown"] = "알수없음"
            }, this, "kr");
        }

        private string Lang(string key, Dictionary<string, string> args = null)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            key = lang.GetMessage(key, this);

            if (args != null)
            {
                foreach (var lekey in args)
                {
                    if (key.Contains("{" + lekey.Key + "}"))
                        key = key.Replace("{" + lekey.Key + "}", lekey.Value);
                }
            }

            return key;
        }
        #endregion
        #region Discord
        private void SendMessage(string status, string reason)
        {
            var list = new Dictionary<string, string>
            {
                { "hostname", ConVar.Server.hostname },
                { "ip", ConVar.Server.ip },
                { "port", ConVar.Server.port.ToString() },
                { "online", BasePlayer.activePlayerList.Count.ToString() },
                { "maxplayers", ConVar.Server.maxplayers.ToString() }
            };
            var embed = new Embed()
                .AddField(Lang("Title", list), status, true)
                .AddField(Lang("Time"), $"{DateTime.Now.ToString(_config.TimeFormat)}", false)
                .AddField(Lang("Descriptions", list), reason, false);

            if(_config.SelectionMention == 0)
            {
                webrequest.Enqueue(_config.webhook, new DiscordMessage("", embed).ToJson(), (code, response) => {
                }, this, RequestMethod.POST, new Dictionary<string, string>() {
                { "Content-Type", "application/json" }
                });
            }
            else if (_config.SelectionMention == 1)
            {
                webrequest.Enqueue(_config.webhook, new DiscordMessage("@here", embed).ToJson(), (code, response) => {
                }, this, RequestMethod.POST, new Dictionary<string, string>() {
                { "Content-Type", "application/json" }
                });
            }
            else if (_config.SelectionMention == 2)
            {
                webrequest.Enqueue(_config.webhook, new DiscordMessage("@everyone", embed).ToJson(), (code, response) => {
                }, this, RequestMethod.POST, new Dictionary<string, string>() {
                { "Content-Type", "application/json" }
                });
            }
            else if (_config.SelectionMention == 3)
            {
                webrequest.Enqueue(_config.webhook, new DiscordMessage($"{_config.DesignatedMention}", embed).ToJson(), (code, response) => {
                }, this, RequestMethod.POST, new Dictionary<string, string>() {
                { "Content-Type", "application/json" }
                });
            }
            
        }

        private class DiscordMessage
        {
            
            public DiscordMessage(string content, params Embed[] embeds)
            {
                Content = "" + content;
                Embeds = embeds.ToList();
            }

            [JsonProperty("content")] public string Content { get; set; }
            [JsonProperty("embeds")] public List<Embed> Embeds { get; set; }

            public string ToJson() => JsonConvert.SerializeObject(this);
        }

        private class Embed
        {
            [JsonProperty("fields")] public List<Field> Fields { get; set; } = new List<Field>();

            public Embed AddField(string name, string value, bool inline)
            {
                Fields.Add(new Field(name, Regex.Replace(value, "<.*?>", string.Empty), inline));

                return this;
            }
        }

        private class Field
        {
            public Field(string name, string value, bool inline)
            {
                Name = name;
                Value = value;
                Inline = inline;
            }

            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("value")] public string Value { get; set; }
            [JsonProperty("inline")] public bool Inline { get; set; }
        }
        #endregion
    }
}
