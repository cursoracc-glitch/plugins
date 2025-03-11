using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Linq;
using Network;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BanSystem", "RusskiIvan", "1.0.6")]
    [Description("BanSystem")]
    public class BanSystem : RustPlugin
    {
        [PluginReference] private Plugin DiscordMessages;

        #region Variables

        private static Oxide.Core.MySql.Libraries.MySql Sql = Interface.Oxide.GetLibrary<Oxide.Core.MySql.Libraries.MySql>();
        private static Core.Database.Connection Sql_conn;
        private List<ulong> CheckSumList = new List<ulong>();
        private bool DiscordNotification;
        private string _database = "ServerBans";
        private ConfigData _config;
        private string DiscordAlertWebhookURL;
        private string DiscordBanWebhookURL;
        private string DiscordDebugWebhookURL;
        private string _moderatorID = "70000000000000000";
        private string _moderatorName = "Server";
        private DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private const string PlayerStr = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={0}&steamids={1}";

        #endregion

        #region Configuration

        private class ConfigData
        {
            [JsonProperty("Привилегии")] public Permissions permissions { get; set; }
            [JsonProperty("Настройки")] public Settings settings { get; set; }
            [JsonProperty("База данных MySQL")] public MySQL mySQL { get; set; }
        }

        public class Permissions
        {
            [JsonProperty("Забанить игрока")] public string PermissionBan { get; set; }
            [JsonProperty("Разбанить игрока")] public string PermissionUnban { get; set; }
            [JsonProperty("Забанить модератора")] public string PermissionBanModerator { get; set; }
        }

        public class Settings
        {
            [JsonProperty("Название сервера")]
            public string Server { get; set; }

            [JsonProperty("SteamID модераторов и их ники для записи в базу (если модератора нет в списке пишется его имя в игре")]
            public Dictionary<string, string> moderatorList { get; set; }
            [JsonProperty("Звук бана игрока")] public string Ban_sound { get; set; }
            [JsonProperty("Причина бана")] public string BanDefaultReason { get; set; }

            [JsonProperty("Сообщение в чат при бане игрока")]
            public bool Ban_Broadcast { get; set; }

            [JsonProperty("Логировать бан игроков")]
            public bool Ban_Log { get; set; }

            [JsonProperty("Discord оповещение")] public bool Discord_use { get; set; }
            [JsonProperty("Discord hook")] public string Discord_Webhook { get; set; }
            [JsonProperty("Steam api key (для поиска имен игроков в оффлайне)")] public string SteamApi { get; set; }

            [JsonProperty("Список белых IP (другие игроки с этих адресов не будут блокироваться)")]
            public List<string> NoBlockIpList { get; set; }
        }

        public class MySQL
        {
            [JsonProperty("Адрес хоста")] public string MySQL_Host { get; set; }
            [JsonProperty("Порт")] public int MySQL_Port { get; set; }
            [JsonProperty("База данных")] public string MySQL_DB { get; set; }
            [JsonProperty("Пользователь")] public string MySQL_User { get; set; }
            [JsonProperty("Пароль")] public string MySQL_Pass { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            ConfigData config = new ConfigData
            {
                settings = new Settings()
                {
                    Server = ConVar.Server.hostname.Split('[')[0],
                    Ban_sound = "assets/bundled/prefabs/fx/player/howl.prefab",
                    SteamApi = "",
                    BanDefaultReason = "Banned",
                    Ban_Broadcast = true,
                    Ban_Log = true,
                    Discord_use = true,
                    Discord_Webhook = "",
                    moderatorList = new Dictionary<string, string>()
                    {
                        {"STEAMID", "Name"}
                    },
                    NoBlockIpList = new List<string>()
                    {
                        "ip"
                    }
                },

                permissions = new Permissions()
                {
                    PermissionBan = $"{Name}.ban".ToLower(),
                    PermissionBanModerator = $"{Name}.mainmoder".ToLower(),
                    PermissionUnban = Name + $"{Name}.unban".ToLower()
                },
                mySQL = new MySQL()
                {
                    MySQL_Host = "ip",
                    MySQL_Port = 0,
                    MySQL_DB = "rust",
                    MySQL_User = "rust",
                    MySQL_Pass = "passwd",
                }
            };
            SaveConfig(config);
            PrintWarning("Creating default a configuration file ...");
        }

        private void LoadConfigVariables() => _config = Config.ReadObject<ConfigData>();
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

        #endregion

        #region Commands

        [ConsoleCommand("player.ban")]
        private void ConsoleCmdBan(ConsoleSystem.Arg arg)
        {
            ulong targetID;
            int duration = 0;
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, _config.permissions.PermissionBan.ToLower()))
            {
                SendReply(player,
                    Msg("NoAccess", player.UserIDString));
                return;
            }
            var reason = "";
            if (!arg.HasArgs())
            {
                PrintWarning(Msg("BanSyntax"));
                return;
            }
            if (arg.Args.Length < 2)
            {
                PrintWarning(Msg("BanSyntax"));
                return;
            }
            if (ulong.TryParse(arg.Args[0], out targetID) && targetID < 76560000000000000)
            {
                PrintWarning(Msg("BanSyntax"));
                return;
            }
            if (player != null)
            {
                ServerUsers.User user = ServerUsers.Get(targetID);
                if (user != null && user.@group == ServerUsers.UserGroup.Owner)
                {
                    SendToPlayer(string.Format(Msg("CantBanOwner", player.UserIDString), targetID), player);
                    return;
                }
                if (user != null && user.@group == ServerUsers.UserGroup.Moderator && !permission.UserHasPermission(player.UserIDString, _config.permissions.PermissionBanModerator.ToLower()))
                {
                    SendToPlayer(string.Format(Msg("CantBanModerator", player.UserIDString), targetID), player);
                    return;
                }
            }
            if (arg.Args.Length == 2) reason = arg.Args[1];
            else
            {
                var length = arg.Args.Length;
                if (int.TryParse(arg.Args[arg.Args.Length - 1], out duration)) length = arg.Args.Length - 1;
                for (int i = 1; i < length; i++)
                {
                    if (!string.IsNullOrEmpty(reason)) reason += " ";
                    reason += arg.Args[i];
                }
            }
            var target = BasePlayer.FindByID(targetID);
            var moderatorName = _moderatorName;
            var moderatorID = _moderatorID;
            if (player != null)
            {
                moderatorName = ModeratorName(player.userID);
                moderatorID = player.UserIDString;
            }
            var message = duration == 0
                ? Msg("ExitMessagePermBan", targetID.ToString())
                : string.Format(Msg("ExitMessageTempBan", targetID.ToString()), "<color=#FFA500>" + duration + Msg("day", targetID.ToString()) + "</color>");
            if (target == null)
            {
                //Puts($"Offline BAN: {targetID.ToString()}, {player.userID}, {moderatorName}, {reason}, {duration}");
                OfflineBan(targetID.ToString(), moderatorID, moderatorName, message, reason, duration);
                return;
            }
            var ip = GetIPAddress(target.net.connection.ipaddress);
            //Puts($"online BAN: {target}, {player.userID}, {player.displayName}, {ip}, {message}, {reason}, {duration}");

            BanPlayer(target, moderatorID, moderatorName, ip, message, reason, duration, true);
        }

        [ConsoleCommand("player.unban")]
        private void ConsoleCmdUnban(ConsoleSystem.Arg arg)
        {
            ulong targetID;
            var player = arg.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, _config.permissions.PermissionUnban.ToLower()))
            {
                SendReply(player,
                    Msg("NoAccess", player.UserIDString));
                return;
            }
            if (!arg.HasArgs())
            {
                PrintWarning(Msg("UnbanSyntax"));
                return;
            }
            if (arg.Args.Length > 1)
            {
                PrintWarning(Msg("UnbanSyntax"));
                return;
            }
            if (ulong.TryParse(arg.Args[0], out targetID) && targetID < 76560000000000000)
            {
                PrintWarning(Msg("UnbanSyntax"));
                return;
            }

            if (player != null)
            {
                UnBanPlayer(targetID, player);
            }
            else
            {
                UnBanPlayer(targetID);
            }

        }

        [ChatCommand("ban")]
        private void ChatCmdBan(BasePlayer player, string command, string[] arg)
        {
            if (!permission.UserHasPermission(player.UserIDString, _config.permissions.PermissionBan.ToLower()))
            {
                SendReply(player,
                    Msg("NoAccess", player.UserIDString));
                return;
            }
            if (arg == null)
            {
                SendReply(player, Msg("BanSyntax", player.UserIDString));
                return;
            }
            if (arg.Length < 2)
            {
                SendReply(player, Msg("BanSyntax", player.UserIDString));
                return;
            }
            int duration = 0;
            var reason = "";
            ulong targetID;
            if (ulong.TryParse(arg[0], out targetID) && targetID < 76560000000000000)
            {
                SendReply(player, Msg("BanSyntax", player.UserIDString));
                return;
            }
            ServerUsers.User user = ServerUsers.Get(targetID);
            if (user != null && user.@group == ServerUsers.UserGroup.Owner)
            {
                SendToPlayer(string.Format(Msg("CantBanOwner", player.UserIDString), targetID), player);
                return;
            }
            if (user != null && user.@group == ServerUsers.UserGroup.Moderator && !permission.UserHasPermission(player.UserIDString, _config.permissions.PermissionBanModerator.ToLower()))
            {
                SendToPlayer(string.Format(Msg("CantBanModerator", player.UserIDString), targetID), player);
                return;
            }
            if (arg.Length == 2) reason = arg[1];
            else
            {
                var length = arg.Length;
                if (int.TryParse(arg[arg.Length - 1], out duration)) length = arg.Length - 1;
                for (int i = 1; i < length; i++)
                {
                    if (!string.IsNullOrEmpty(reason)) reason += " ";
                    reason += arg[i];
                }
            }
            var target = BasePlayer.FindByID(targetID);
            var moderatorName = ModeratorName(player.userID);
            var message = duration == 0
                ? Msg("ExitMessagePermBan", targetID.ToString())
                : string.Format(Msg("ExitMessageTempBan", targetID.ToString()), "<color=#FFA500>" + duration + Msg("day", targetID.ToString()) + "</color>");
            if (target == null)
            {
                //Puts($"Offline BAN: {targetID.ToString()}, {player.userID}, {moderatorName}, {reason}, {duration}");
                OfflineBan(targetID.ToString(), player.UserIDString, moderatorName, message, reason, duration);
                return;
            }
            var ip = GetIPAddress(target.net.connection.ipaddress);
            //Puts($"online BAN: {target}, {player.userID}, {player.displayName}, {ip}, {message}, {reason}, {duration}");
            BanPlayer(target, player.UserIDString, moderatorName, ip, message, reason, duration, true);
        }

        [ChatCommand("unban")]
        private void ChatCmdUnan(BasePlayer player, string command, string[] arg)
        {
            ulong targetID;
            if (!permission.UserHasPermission(player.UserIDString, _config.permissions.PermissionUnban.ToLower()))
            {
                SendReply(player,
                    Msg("NoAccess", player.UserIDString));
                return;
            }
            if (arg == null)
            {
                PrintWarning(Msg("UnbanSyntax"));
                return;
            }
            if (arg.Length > 1)
            {
                PrintWarning(Msg("UnbanSyntax"));
                return;
            }
            if (ulong.TryParse(arg[0], out targetID) && targetID < 76560000000000000)
            {
                PrintWarning(Msg("UnbanSyntax"));
                return;
            }
            UnBanPlayer(targetID, player);
        }

        #endregion

        #region Oxide

        private void Loaded() => LoadConfigVariables();
        private void OnClientAuth(Connection connection, object AddQueue) => CheckPlayer(connection);
        private void OnPlayerConnected(BasePlayer player) => CheckSharedAcc(player, player.net.connection.ownerid, GetIPAddress(player.net.connection.ipaddress));
        private void Init() => Unsubscribe("OnServerCommand");

        [HookMethod("OnServerCommand")]
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            string command = arg.cmd?.Name;
            if (command == null)
            {
                return null;
            }
            switch (command)
            {
                case "ban":
                    ConsoleCmdBan(arg);
                    return false;
                case "banid":
                    ConsoleCmdBan(arg);
                    return false;
                case "unban":
                    ConsoleCmdUnban(arg);
                    return false;
                default:
                    return null;
            }
        }

        private void OnServerInitialized()
        {
            RegisterPermissions();
            LoadDatabase();
            Subscribe("OnServerCommand");
            DiscordBanWebhookURL = _config.settings.Discord_Webhook;
            DiscordAlertWebhookURL = _config.settings.Discord_Webhook;
            DiscordDebugWebhookURL = _config.settings.Discord_Webhook;
            DiscordNotification = DiscordNotify();
        }

        #endregion

        #region Helpers

        private string SplitIP(string ip)
        {
            var ipSplit = ip.Split('.');
            return ipSplit[0] + "." + ipSplit[1] + "." + ipSplit[2];
        }
        private void KickPlayer(Connection connection, string reason)
        {
            Net.sv.Kick(connection, reason);
        }
        private bool DiscordNotify()
        {
            if (!DiscordMessages) return false;
            if (!_config.settings.Discord_use) return false;
            if (string.IsNullOrEmpty(_config.settings.Discord_Webhook)) return false;
            return true;
        }
        private enum ResponseCode
        {
            Valid = 200,
            InvalidKey = 403,
            Unavailable = 503,
        }
        private bool IsValidRequest(ResponseCode code)
        {
            switch (code)
            {
                case ResponseCode.Valid:
                    return true;

                case ResponseCode.InvalidKey:
                    Puts("ErrorInvalidKey");
                    return false;

                case ResponseCode.Unavailable:
                    Puts("ErrorServiceUnavailable");
                    return false;

                default:
                    Puts("ErrorUnknown");
                    return false;
            }
        }
        private void GetResponce<T>(string url, Action<T> callback)
        {
            webrequest.Enqueue(url, null, (code, response) =>
            {
                try
                {
                    T query = JsonConvert.DeserializeObject<T>(response, new JsonSerializerSettings());
                    if (query == null)
                    {
                        if (IsValidRequest((ResponseCode)code)) return;
                        Puts($"Ошибка соединения с сервером {url} ...[{IsValidRequest((ResponseCode)code)}]");
                    }
                    else
                    {
                        callback.Invoke(query);
                    }
                }
                catch { }
            }, this);
        }
        private void SendNotification(string targetID, string targetName, string moderatorID, string moderatorName, string reason, string ip, int duration = 0)
        {
            if (_config.settings.Ban_Broadcast)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    var serverMessage = duration == 0
                        ? string.Format(Msg("PlayerBanned", player.UserIDString), targetName, reason)
                        : string.Format(Msg("PlayerBannedTemp", player.UserIDString), targetName, duration + Msg("day", player.UserIDString), reason);
                    SendReply(player, $"{serverMessage}");
                    if (!string.IsNullOrEmpty(_config.settings.Ban_sound)) Effect.server.Run("assets/bundled/prefabs/fx/player/howl.prefab", player.transform.position);
                }
            }
            if (!DiscordNotification) return;
            var expire = "НИКОГДА";
            if (string.IsNullOrEmpty(ip)) ip = "не указан";
            if (duration != 0)
            {
                expire = DateTime.Now.AddDays(duration).ToString("dd.MM.yyyy HH:mm:ss");
            }
            var text = moderatorName == _moderatorName ? $"{_moderatorName}" : $"{moderatorName} [{moderatorID}]";
            var fields = new List<SendList>
            {
                new SendList()
                {
                    name = $":technologist: {targetName}",
                    inline = true,
                    value = $"[{targetID}]"
                },
                new SendList()
                {
                    name = ":file_cabinet: IP:",
                    inline = true,
                    value = ip
                },
                new SendList()
                {
                    name = ":clock8: Дата:",
                    inline = true,
                    value = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}]"
                },
                new SendList()
                {
                    name = ":link: Ссылки:",
                    inline = true,
                    value = $"\n[Профиль Steam](https://steamcommunity.com/profiles/{targetID})\n[База чекера](https://rustcheatcheck.ru/panel/player/{targetID})"
                },
                new SendList()
                {
                    name = ":scales: Причина:",
                    inline = true,
                    value = "``" + reason + "``"
                },
                new SendList()
                {
                    name = ":stopwatch: Бан истекает:",
                    inline = true,
                    value = $"[{expire}]"
                },
                new SendList()
                {
                    name = ":judge: Забанил:",
                    inline = false,
                    value = text
                },

            };
            DiscordMessages.Call("API_SendFancyMessage", DiscordBanWebhookURL, ":no_entry: БАН на " + _config.settings.Server, 11111199, JsonConvert.SerializeObject(fields.Cast<object>().ToArray()));
            fields.Clear();
        }
        private void SendToPlayer(string message, BasePlayer player = null)
        {
            if (player != null) SendReply(player, message);
            else PrintWarning(message);
        }
        private string GetSignature(string reason, ulong id, string signature = null)
        {
            if (reason == null) return _config.settings.BanDefaultReason;
            if (!string.IsNullOrEmpty(signature)) return $"{reason} | {signature}";
            foreach (var data in _config.settings.moderatorList.Where(data => data.Key == id.ToString()))
            {
                signature = data.Value;
            }
            return string.IsNullOrEmpty(signature) ? $"{reason} | AutoBan" : $"{reason} | by {signature}";
        }
        private string ModeratorName(ulong id)
        {
            return _config.settings.moderatorList.ContainsKey(id.ToString()) ? _config.settings.moderatorList[id.ToString()] : BasePlayer.FindByID(id).ToString();
        }
        private void IPValidate(Connection connection, string ip)
        {
            if (!DiscordNotification) return;
            if (string.IsNullOrEmpty(ip)) return;
            Sql.Query(
                    Core.Database.Sql.Builder.Append(
                        $"SELECT `steamid`, `date`, `name`, `ip`, `moderatorid`, `reason`, `expire`, `moderatorName` FROM {_database} WHERE `ip`LIKE '%{SplitIP(ip)}%'"), Sql_conn,
                    responseID =>
                    {
                        if (responseID.Count == 0) return;
                        var field = new List<SendList>();
                        field.Add(new SendList
                        {
                            name = $":technologist: {connection.username}\n[{connection.userid}]",
                            inline = true,
                            value = $"\n[Профиль Steam](https://steamcommunity.com/profiles/{connection.userid})\n[База чекера](https://rustcheatcheck.ru/panel/player/{connection.userid})"
                        });
                        field.Add(new SendList
                        {
                            name = "Возможно пользователь сменил IP адрес",
                            inline = true,
                            value = $":exclamation: :exclamation: :exclamation: :exclamation: :exclamation: :exclamation:"
                        });
                        field.Add(new SendList
                        {
                            name = "IP:",
                            inline = true,
                            value = $"{ip} [[Локация]](https://ip-api.com/#{ip})"
                        });
                        field.Add(new SendList
                        {
                            name = "АККАУНТ В БАНЕ:",
                            inline = false,
                            value = ":point_down: :point_down: :point_down: :point_down:"
                        });
                        foreach (var data in responseID)
                        {
                            var name = data["name"].ToString();
                            var id = data["steamid"].ToString();
                            var date = data["date"].ToString();
                            var text = data["moderatorName"].ToString() == _moderatorName
                                ? $"{_moderatorName}"
                                : $"{data["moderatorName"]} [{data["moderatorid"]}]";
                            var expire = "НИКОГДА";
                            var duration = int.Parse(data["expire"].ToString());
                            if (int.Parse(data["expire"].ToString()) != 0) expire = epoch.AddSeconds(duration).ToString();
                            field.Add(new SendList
                            {
                                name = $":technologist: {name}",
                                inline = true,
                                value = $"[{id}]"
                            });
                            field.Add(new SendList
                            {
                                name = ":file_cabinet: IP:",
                                inline = true,
                                value = $"{data["ip"]} [[Локация]](https://ip-api.com/#{data["ip"]})"
                            });
                            field.Add(new SendList
                            {
                                name = ":clock8: Дата:",
                                inline = true,
                                value = $"[{date}]"
                            });
                            field.Add(new SendList
                            {
                                name = ":link: Ссылки:",
                                inline = true,
                                value =
                                    $"\n[Профиль Steam](https://steamcommunity.com/profiles/{id})\n[База чекера](https://rustcheatcheck.ru/panel/player/{id})"
                            });
                            field.Add(new SendList
                            {
                                name = ":scales: Причина:",
                                inline = true,
                                value = "``" + data["reason"] + "``"
                            });
                            field.Add(new SendList
                            {
                                name = ":stopwatch: Бан истекает:",
                                inline = true,
                                value = $"[{expire}]"
                            });
                            field.Add(new SendList
                            {
                                name = ":judge: Забанил:",
                                inline = false,
                                value = text
                            });
                        }
                        DiscordMessages.Call("API_SendFancyMessage", DiscordAlertWebhookURL,
                                "!!!" + _config.settings.Server, 11111199,
                                JsonConvert.SerializeObject(field.Cast<object>().ToArray()));
                        field.Clear();
                    });
        }

        [HookMethod("GetIPAddress")]
        private string GetIPAddress(string ip)
        {
            var IP = ip.Split(':')[0];
            if (_config.settings.NoBlockIpList.Contains(IP)) IP = "";
            return IP;
        }
        private BasePlayer FindPlayerName(ulong userId)
        {
            BasePlayer player = BasePlayer.FindByID(userId);
            if (player) return player;
            player = BasePlayer.FindSleeping(userId);
            if (player) return player;
            BasePlayer target = covalence.Players.FindPlayer(userId.ToString()) as BasePlayer;
            return target != null ? target : null;
        }
        private string GetExpiredTime(string target, int timestamp)
        {
            if (timestamp == 0) return null;
            var timeLeft = (epoch.AddSeconds(timestamp) - DateTime.Now).ToString().Split('.');
            if (timeLeft.Length != 3 && timeLeft.Length != 2) return null;
            var days = timeLeft.Length == 3 ? timeLeft[0] + " " + Msg("day", target) : "";
            var time = timeLeft.Length == 3 ? timeLeft[1] : timeLeft[0];
            var text = "";
            text = time == "00:00:00"
                ? days
                : days + " " + time;
            return text;
        }

        #endregion


        #region Functions
        private void RegisterPermissions()
        {
            if (!permission.PermissionExists(_config.permissions.PermissionBan.ToLower()))
                permission.RegisterPermission(_config.permissions.PermissionBan.ToLower(), this);
            if (!permission.PermissionExists(_config.permissions.PermissionBanModerator.ToLower()))
                permission.RegisterPermission(_config.permissions.PermissionBanModerator.ToLower(), this);
            if (!permission.PermissionExists(_config.permissions.PermissionUnban.ToLower()))
                permission.RegisterPermission(_config.permissions.PermissionUnban.ToLower(), this);
        }
        private void CheckSharedAcc(BasePlayer target, ulong sharedID, string ip)
        {
            if (!CheckSumList.Contains(target.userID))
            {
                var field = new List<SendList>();
                field.Add(new SendList
                {
                    name = $":technologist: {target.displayName}\n[{target.userID}]",
                    inline = true,
                    value = $"\n[Профиль Steam](https://steamcommunity.com/profiles/{target.userID})\n[База чекера](https://rustcheatcheck.ru/panel/player/{target.userID})"
                });
                field.Add(new SendList
                {
                    name = "ИГРОК НЕ БЫЛ ПРОВЕРЕН В БАН ЛИСТЕ!",
                    inline = true,
                    value = $":exclamation: :exclamation: :exclamation: :exclamation: :exclamation: :exclamation:"
                });
                field.Add(new SendList
                {
                    name = "IP:",
                    inline = true,
                    value = $"{ip} [[Локация]](https://ip-api.com/#{ip})"
                });

                DiscordMessages.Call("API_SendFancyMessage", DiscordAlertWebhookURL,
                        "!!! ОШИБКА " + _config.settings.Server, 11111199,
                        JsonConvert.SerializeObject(field.Cast<object>().ToArray()));
                field.Clear();
            }
            else
                CheckSumList.Remove(target.userID);

            if (target.userID == sharedID) return;
            try
            {
                Sql.Query(Core.Database.Sql.Builder.Append($"SELECT `expire`, `reason`, `ip`, `moderatorid`, `moderatorName` FROM {_database} WHERE `steamid` = @0", sharedID), Sql_conn, responseID =>
                {
                    if (responseID.Count == 0) return;
                    var expireID = int.Parse(responseID[0]["expire"].ToString());
                    //Puts("++++Игрок есть в базе!");
                    //Puts("++++Забанить игрока на сервере");
                    var message = expireID == 0
                        ? Msg("ExitMessagePermBan", target.UserIDString)
                        : string.Format(Msg("ExitMessageTempBan", target.UserIDString), "<color=#FFA500>" + GetExpiredTime(target.UserIDString, expireID)) + "</color>";
                    BanPlayer(target, _moderatorID, _moderatorName, ip, message, $"Owner: {sharedID}", expireID, true);
                    if (expireID != 0) ChangeExpire(null, sharedID.ToString(), responseID[0]["reason"] + "| MultiAcc");
                });
            }
            catch (Exception e)
            {
                PrintError(e.Message);
            }
        }
        private void CheckPlayer(Connection connection, bool checkIp = true)
        {
            var moderatorid = _moderatorID;
            var moderatorName = _moderatorName;
            var currentIP = "";
            var listIP = new List<string>();
            var sqlAdd = false;
            if (checkIp) currentIP = GetIPAddress(connection.ipaddress);
            string message;
            try
            {
                Sql.Query(Core.Database.Sql.Builder.Append($"SELECT `expire`, `reason`, `ip`, `moderatorid`, `moderatorName` FROM {_database} WHERE `steamid` = @0", connection.userid), Sql_conn, responseID =>
                {
                    if (responseID.Count == 0)
                    {
                        //("++++ID игрока нету в базе"); 
                        if (string.IsNullOrEmpty(currentIP))
                        {
                            //("++++ip адрес в white листе");
                            //ShtirlitzTime(connection);
                            if (!CheckSumList.Contains(connection.userid)) CheckSumList.Add(connection.userid);
                            return;
                        }
                        Sql.Query(Core.Database.Sql.Builder.Append($"SELECT `expire`, `reason`, `steamid`, `moderatorid` FROM {_database} WHERE `ip` = @0", currentIP), Sql_conn,
                            responseIP =>
                            {
                                if (responseIP.Count == 0)
                                {
                                    //Puts("++++IP игрока нету в базе");
                                    //ShtirlitzTime(connection);
                                    if (!CheckSumList.Contains(connection.userid)) CheckSumList.Add(connection.userid);
                                    //Puts("++++Запущена проверка IP на совпадения");
                                    if (DiscordNotification) IPValidate(connection, currentIP);
                                    return;
                                }
                                var reasonIPDB = responseIP[0]["reason"].ToString();
                                var expireIPDB = int.Parse(responseIP[0]["expire"].ToString());
                                if (expireIPDB != 0 && DateTime.Now.Subtract(epoch).TotalSeconds < expireIPDB)
                                {
                                    Sql.Query(
                                        Core.Database.Sql.Builder.Append(
                                            $"SELECT `expire`, `reason`, `ip`, `moderatorid`, `moderatorName` FROM {_database} WHERE `steamid` = @0",
                                            responseIP[0]["steamid"].ToString()), Sql_conn,
                                        response =>
                                        {
                                            var reasonDB = response[0]["reason"];
                                            message = string.Format(Msg("ExitMessagePermBan", connection.userid.ToString()), reasonIPDB);
                                            KickPlayer(connection, message + "<color=#FFA500>" + GetSignature(reasonDB + " | " + Msg("BanEvade", connection.userid.ToString()), ulong.Parse(_moderatorID)) + ". </color>" + Msg("InformMessage", connection.userid.ToString()));
                                            ChangeExpire(moderatorid, responseIP[0]["steamid"].ToString(), reasonDB + " | " + Msg("BanEvade", connection.userid.ToString()));
                                            AddToDatabase(connection.userid.ToString(), connection.username, moderatorid,
                                                moderatorName, currentIP, reasonDB + " | " + Msg("BanEvade", connection.userid.ToString()));
                                            //Puts("++++Игрок есть в базе и зашел с другого аккаунта во время временной блокировки. Забанить все аккаунты игрока навсегда за попытку обхода. Сообщение попытка обхода бана");
                                        });
                                    return;
                                }
                                if (expireIPDB != 0 && DateTime.Now.Subtract(epoch).TotalSeconds >= expireIPDB)
                                {
                                    if (DiscordNotification) IPValidate(connection, currentIP);
                                    UnBanPlayer(connection.userid);
                                    //Puts("++++Время игровой блокировки вышло! Разбанить игрока");
                                    //ShtirlitzTime(connection);
                                    if (!CheckSumList.Contains(connection.userid)) CheckSumList.Add(connection.userid);
                                    return;
                                }
                                Sql.Query(
                                    Core.Database.Sql.Builder.Append(
                                        $"SELECT `expire`, `reason`, `ip`, `moderatorid`, `moderatorName` FROM {_database} WHERE `steamid` = @0",
                                        responseIP[0]["steamid"].ToString()), Sql_conn,
                                    response =>
                                    {
                                        message = string.Format(Msg("ExitMessagePermBan", connection.userid.ToString()), reasonIPDB);
                                        KickPlayer(connection, message + "<color=#FFA500>" + GetSignature(Msg("MultiAcc", connection.userid.ToString()), ulong.Parse(_moderatorID)) + ". </color>" + Msg("InformMessage", connection.userid.ToString()));

                                        foreach (var data in response.Where(data => !listIP.Contains(data["ip"].ToString())))
                                            listIP.Add(data["ip"].ToString());

                                        if (listIP.Contains(currentIP))
                                            AddToDatabase(connection.userid.ToString(), connection.username, moderatorid,
                                            moderatorName, currentIP, Msg("MultiAcc", connection.userid.ToString()), expireIPDB);
                                        //Puts("++++Игрок есть в базе и зашел с другого аккаунта. Забанить новый id игрока за попытку обхода блокировки. Сообщение мульти акк");
                                        listIP.Clear();
                                    });
                            });
                        return;
                    }
                    var expireIDDB = int.Parse(responseID[0]["expire"].ToString());
                    var reasonIDDB = responseID[0]["reason"].ToString();
                    var ipDB = responseID[0]["ip"].ToString();
                    var ModeratorIDDB = ulong.Parse(responseID[0]["moderatorid"].ToString());
                    //Puts("++++Игрок есть в базе!");
                    if (expireIDDB != 0 && DateTime.Now.Subtract(epoch).TotalSeconds >= expireIDDB)
                    {
                        if (DiscordNotification) IPValidate(connection, currentIP);
                        UnBanPlayer(connection.userid);
                        if (!CheckSumList.Contains(connection.userid)) CheckSumList.Add(connection.userid);
                        //Puts("++++Время игровой блокировки вышло! Разбанить игрока");
                        return;
                    }
                    //Puts("++++Забанить игрока на сервере");
                    message = expireIDDB == 0
                        ? Msg("ExitMessagePermBan", connection.userid.ToString())
                        : string.Format(Msg("ExitMessageTempBan", connection.userid.ToString()), "<color=#FFA500>" + GetExpiredTime(connection.userid.ToString(), expireIDDB)) + "</color>";
                    KickPlayer(connection, message + "<color=#FFA500>" + GetSignature(reasonIDDB, ModeratorIDDB) + ". </color>" + Msg("InformMessage", connection.userid.ToString()));
                    foreach (var data in responseID.Where(data => !listIP.Contains(data["ip"].ToString())))
                    {
                        listIP.Add(data["ip"].ToString());
                    }
                    //Если игрок зашел с ip, отличного от указанного в базе
                    if (!string.IsNullOrEmpty(currentIP) && !string.IsNullOrEmpty(ipDB) && currentIP != ipDB && !listIP.Contains(currentIP))
                        AddToDatabase(connection.userid.ToString(), connection.username, _moderatorID, _moderatorName,
                            currentIP, reasonIDDB, expireIDDB);

                    //Если в базе нету ip и игрок зашел с ip не из white листа
                    if (!string.IsNullOrEmpty(currentIP) && string.IsNullOrEmpty(ipDB))
                        ChangePlayerData(connection.userid.ToString(), ModeratorIDDB,
                            responseID[0]["moderatorName"].ToString(), reasonIDDB, currentIP, expireIDDB);
                    listIP.Clear();
                });
            }
            catch (Exception e)
            {
                PrintError(e.Message);
            }
        }

        private void ChangePlayerData(string targetID, ulong moderatorID, string moderatorName, string reason, string ip = null, int duration = 0)
        {
            Sql.Insert(Core.Database.Sql.Builder.Append($"UPDATE {_database} SET `moderatorName`=@0, `moderatorid`=@1, `expire`=@2, `reason`=@3, `ip`=@4 WHERE steamid=@5", moderatorName, moderatorID, duration, reason, ip, targetID), Sql_conn, i =>
            {
                Puts($"MySQL - изменено записей: [{i}]");
                if (_config.settings.Ban_Log) LogToFile(Name, $"[{DateTime.Now.ToString("HH:mm:ss")}] Изменение данных для {targetID}: Модератор: {moderatorName}[{moderatorID}], причина {reason}, ip {ip}, duration {duration}", this);
            });
        }

        private void ChangeExpire(string moderatorID, string targetID, string reason, int duration = 0)
        {
            Sql.Insert(Core.Database.Sql.Builder.Append($"UPDATE {_database} SET `expire`=@0, `reason`=@1 WHERE steamid=@2", duration, reason, targetID), Sql_conn,
                i =>
                {
                    ulong playerID;
                    var text = duration == 0 ? Msg("Permanent", moderatorID) : GetExpiredTime(targetID, duration);
                    if (!ulong.TryParse(moderatorID, out playerID)) return;
                    var player = BasePlayer.FindByID(playerID);
                    var textMsg = Msg("PlayerNotFound");
                    if (player != null)
                    {
                        textMsg = Msg("Changed", playerID.ToString());
                    }

                    SendToPlayer(string.Format(textMsg, targetID, text), player);
                    Puts($"MySQL - изменено записей: [{i}]");
                    if (_config.settings.Ban_Log) LogToFile(Name, $"[{DateTime.Now.ToString("HH:mm:ss")}] Изменение времени бана для {targetID}: Модератор: [{moderatorID}], причина {reason}, duration {duration}", this);
                });
        }
        private void UnBanPlayer(ulong target, BasePlayer player = null)
        {
            Sql.Query(Core.Database.Sql.Builder.Append($"SELECT `expire`, `ip` FROM {_database} WHERE `steamid` = @0", target), Sql_conn,
                responseID =>
                {

                    if (responseID.Count == 0)
                    {
                        var text = Msg("PlayerNotFound");
                        if (player != null)
                        {
                            text = Msg("PlayerNotFound", player.UserIDString);
                        }
                        SendToPlayer(string.Format(text, target), player);
                        return;
                    }
                    if (responseID.Count == 1)
                    {
                        DeleteFromDatabase(player, target);
                        return;
                    }
                    var ipList = new List<string>();
                    foreach (var data in responseID.Where(data => string.IsNullOrEmpty(data["ip"].ToString()) && !ipList.Contains(data["ip"].ToString())))
                    {
                        ipList.Add(data["ip"].ToString());
                    }
                    var idlist = new List<string> { target.ToString() };
                    foreach (var ip in ipList)
                    {
                        Sql.Query(
                            Core.Database.Sql.Builder.Append($"SELECT `steamid` FROM {_database} WHERE `ip` = @0",
                                ip), Sql_conn,
                            responseIP =>
                            {
                                foreach (var data in responseIP.Where(data => !idlist.Contains(data["steamid"].ToString())))
                                {
                                    idlist.Add(data["steamid"].ToString());
                                }
                            });
                    }
                    if (idlist.Count == 1)
                    {
                        DeleteFromDatabase(player, target);
                        return;
                    }
                    var message = "";
                    foreach (var id in idlist)
                    {
                        if (string.IsNullOrEmpty(message)) message += "\n";
                        message += id;
                    }
                    SendReply(player, string.Format(Msg("PlayerDefinition", player.UserIDString), message));
                });
        }
        private void DeleteFromDatabase(BasePlayer player, ulong target)
        {
            Sql.Insert(Core.Database.Sql.Builder.Append($"DELETE FROM {_database} WHERE steamid=@0", target), Sql_conn,
                responseID =>
                {
                    Puts($"MySQL - удалено записей: [{responseID}]");
                    if (_config.settings.Ban_Log) LogToFile(Name, $"[{DateTime.Now.ToString("HH:mm:ss")}] Игрок {target} разбанен: Инициатор: {player}", this);

                    if (player == null)
                        PrintWarning(string.Format(Msg("UnbanMessage"), target));
                    else
                        SendReply(player, string.Format(Msg("UnbanMessage", player.UserIDString), target));
                });

        }

        private void AddToDatabase(string targetID, string targetName, string moderatorID, string moderatorName, string ip, string reason, int duration = 0)
        {
            /*Sql.Query(
                Core.Database.Sql.Builder.Append($"SELECT `expire`, `ip` FROM {_database} WHERE `steamid` = @0",
                    targetID), Sql_conn,
                responseID =>
                {*/
            Sql.Insert(
                    Core.Database.Sql.Builder.Append(
                        $"INSERT INTO {_database} (`steamid`,`name`,`ip`,`reason`,`moderatorid`,`moderatorName`, `server`, `expire` ) VALUES ( @0, @1, @2, @3, @4, @5, @6, @7)",
                        targetID, targetName, ip, reason, moderatorID, moderatorName,
                        _config.settings.Server, duration), Sql_conn,
                    i => Puts($"MySQL - добавлено записей: [{i}]"));
            if (_config.settings.Ban_Log) LogToFile(Name, $"[{DateTime.Now.ToString("HH:mm:ss")}] Добавлен пользователь {targetName}[{targetID}]: Модератор: {moderatorName}[{moderatorID}], причина {reason}, ip {ip}, duration {duration}", this);
            // });
        }
        private void BanPlayer(BasePlayer target, string moderatorID, string moderatorName, string ip, string message, string reason, int duration = 0, bool sqlAdd = false, int chatDuration = 0)
        {
            var expire = duration == 0 ? 0 : (int)DateTime.Now.AddDays(duration).Subtract(epoch).TotalSeconds;
            var player = BasePlayer.FindByID(ulong.Parse(moderatorID));
            Sql.Query(Core.Database.Sql.Builder.Append($"SELECT `expire`, `ip` FROM {_database} WHERE `steamid` = @0", target.userID), Sql_conn,
                responseID =>
                {
                    if (responseID.Count != 0)
                    {
                        if (responseID[0]["expire"].ToString() == expire.ToString())
                        {
                            var text = Msg("PlayerAlreadyBanned");
                            if (player != null)
                            {
                                text = Msg("PlayerAlreadyBanned", player.UserIDString);
                            }
                            SendToPlayer(string.Format(text, target), player);
                        }
                        else
                        {
                            ChangeExpire(moderatorID, target.UserIDString, reason, expire);
                        }
                        KickPlayer(target.net.connection, message + "<color=#FFA500>" + GetSignature(reason, ulong.Parse(moderatorID)) + ". </color>" + Msg("InformMessage", target.UserIDString));
                        return;
                    }
                    var id = target.UserIDString;
                    var name = target.displayName;
                    if (sqlAdd)
                    {
                        AddToDatabase(target.UserIDString, target.displayName, moderatorID, moderatorName, ip, reason, expire);
                        if (target.net.connection.ownerid != target.userID) OfflineBan(target.net.connection.ownerid.ToString(), moderatorID, moderatorName, message, reason, expire);
                    }
                    KickPlayer(target.net.connection, message + "<color=#FFA500>" + GetSignature(reason, ulong.Parse(moderatorID)) + ". </color>" + Msg("InformMessage", target.UserIDString));
                    if (_config.settings.Ban_Log) LogToFile(Name, $"[{DateTime.Now.ToString("HH:mm:ss")}] Добавлен онлайн пользователь {target}: Модератор: {moderatorName}[{moderatorID}], причина {reason}, ip {ip}, duration {duration}", this);
                    SendNotification(id, name, moderatorID, moderatorName, reason + $"|by {moderatorName}", ip, duration);
                });

        }
        private void OfflineBan(string targetID, string moderatorID, string moderatorName, string message, string reason, int duration = 0)
        {
            var expire = duration == 0 ? 0 : (int)DateTime.Now.AddDays(duration).Subtract(epoch).TotalSeconds;
            var textReason = GetSignature(reason, ulong.Parse(moderatorID));
            var target = ulong.Parse(targetID);
            Sql.Query(
                Core.Database.Sql.Builder.Append(
                    $"SELECT `expire` FROM {_database} WHERE `steamid` = @0",
                    targetID), Sql_conn,
                responseID =>
                {
                    var player = BasePlayer.FindByID(ulong.Parse(moderatorID));
                    if (responseID.Count != 0)
                    {
                        if (responseID[0]["expire"].ToString() == duration.ToString())
                        {
                            var text = Msg("PlayerAlreadyBanned");
                            if (player != null)
                            {
                                text = Msg("PlayerAlreadyBanned", player.UserIDString);
                            }

                            SendToPlayer(string.Format(text, target), player);
                        }
                        else
                        {
                            ChangeExpire(moderatorID, targetID, reason, expire);
                        }
                        return;
                    }

                    if (string.IsNullOrEmpty(_config.settings.SteamApi))
                    {
                        //Puts("SteamApi не задан");
                        var targetName = "Негодяй" + DateTime.Now.Millisecond;
                        AddToDatabase(targetID, targetName, moderatorID, moderatorName, "", reason, expire);
                        if (_config.settings.Ban_Log) LogToFile(Name, $"[{DateTime.Now.ToString("HH:mm:ss")}] Добавлен оффлайн пользователь [{target}]: Модератор: {moderatorName}[{moderatorID}], причина {reason}, duration {duration}", this);
                        SendNotification(targetID, targetName, moderatorID, moderatorName, textReason, "---", duration);
                        return;
                    }
                    //Puts("Запрос имени игрока в Steam");
                    GetResponce(string.Format(PlayerStr, _config.settings.SteamApi, targetID), (PlayerInfo.Summaries _summaries) =>
                    {
                        var targetName = _summaries.response.Players[0].PersonaName ?? "HiddenName";
                        AddToDatabase(targetID, targetName, moderatorID, moderatorName, "", reason, expire);
                        if (_config.settings.Ban_Log) LogToFile(Name, $"[{DateTime.Now.ToString("HH:mm:ss")}] Добавлен оффлайн пользователь {targetName}[{targetID}]: Модератор: {moderatorName}[{moderatorID}], причина {reason}, duration {duration}", this);
                        SendNotification(targetID, targetName, moderatorID, moderatorName, textReason, "---", duration);
                    });
                });
        }
        #endregion


        #region <MySQL>
        private void LoadDatabase()
        {
            var fields = new List<SendList>();
            try
            {
                Sql_conn = Sql.OpenDb($"server={_config.mySQL.MySQL_Host}; port={_config.mySQL.MySQL_Port}; database={_config.mySQL.MySQL_DB}; user={_config.mySQL.MySQL_User}; password={_config.mySQL.MySQL_Pass}; charset= utf8", this);
                if (Sql_conn == null || Sql_conn.Con == null)
                {
                    fields.Add(new SendList
                    {
                        name = ":exclamation::exclamation::exclamation: Ошибка :exclamation::exclamation::exclamation:",
                        inline = true,
                        value = $"Couldn't open the MySQL PlayerDatabase: [{Sql_conn.Con.State}]"
                    });
                    PrintError("Couldn't open the MySQL PlayerDatabase: {0}", Sql_conn.Con.State.ToString());
                    return;
                }
                Sql.Insert(Core.Database.Sql.Builder.Append($"CREATE TABLE IF NOT EXISTS {_database} (`id` int(11) NOT NULL AUTO_INCREMENT, `steamid` BIGINT(17), `name` VARCHAR(32),`ip` VARCHAR(15),`reason` VARCHAR(32),`moderatorid` BIGINT(17),`moderatorName` VARCHAR(32), `server` VARCHAR(25), `expire` int(11) NOT NULL, `date` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (`id`));"), Sql_conn);
                Puts($"Database {_database} loaded");
                Sql.Query(Core.Database.Sql.Builder.Append($"SELECT `id` FROM {_database} WHERE 1"), Sql_conn, response =>
                {
                    fields.Add(new SendList
                    {
                        name = $":sunny: База банов загружена! :sunny:",
                        inline = true,
                        value = $":card_box: Количество записей: [{response.Count}]"
                    });
                    DiscordMessages.Call("API_SendFancyMessage", DiscordDebugWebhookURL, "BAN SYSTEM", 11111199, JsonConvert.SerializeObject(fields.Cast<object>().ToArray()));
                    fields.Clear();
                });
            }
            catch (Exception e)
            {
                fields.Add(new SendList
                {
                    name = ":exclamation::exclamation::exclamation: Ошибка :exclamation::exclamation::exclamation:",
                    inline = true,
                    value = $"[{e.Message}]"
                });
                PrintError(e.Message);
                DiscordMessages.Call("API_SendFancyMessage", DiscordDebugWebhookURL, "БАНЫ " + _config.settings.Server, 11111199, JsonConvert.SerializeObject(fields.Cast<object>().ToArray()));
                fields.Clear();
            }


        }
        #endregion


        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoAccess","<color=#FF0000>[LF]</color><color=#FFFFFF> You don't have the permission to use this command</color>" },
                {"UnbanMessage","Player [{0}] is unbanned!" },
                {"UnbanSyntax","Syntax: unban < SteamID >" },
                {"BanSyntax","Syntax: ban < SteamID > < reason > < time in days(optional) > " },
                { "PlayerNotFound", "Player {0} was not found in database.\n" },
                { "PlayerDefinition", "Игрока нельзя разбанить без согласования с админом!\nСвязанные SteamID: {0}" },
                { "PlayerAlreadyBanned", "Player {0} is already banned.\n" },
                { "Changed", "Ban duration for player {0} changed to {1}" },
                { "day", "d." },
                { "Permanent", "permanent" },
                { "BanEvade", "Ban evade" },
                { "CantBanOwner", "Нельзя забанить Админа\n" },
                { "CantBanModerator", "Нельзя забанить модератора\n" },
                { "PlayerBanned", "<color=#FF0000>[LF]</color><color=#FFFFFF> Player <color=#FF0000>{0}</color> was permanently banned! Reason: <color=#FF0000>{1}</color></color>" },
                { "PlayerBannedTemp", "<color=#FF0000>[LF]</color><color=#FFFFFF> Player <color=#FF0000>{0}</color> was banned for <color=#FF0000>{1}</color> Reason: <color=#FF0000>{2}</color></color>" },
                { "DatabaseError", "Database receiving error: list.Count != 1!\n" },
                { "InformMessage", "If you believe that the ban was received unfairly, you can apply for a unban <color=#FFA500>vk.com/lifefinerust</color>" },
                { "ExitMessagePermBan", "You are permanently banned!\nReason:" },
                { "ExitMessageTempBan", "The ban expires after {0} Reason: " }
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoAccess","<color=#FF0000>[LF]</color><color=#FFFFFF> У вас нету привилегии для использования команды</color>" },
                {"UnbanMessage","Игрок [{0}] разбанен!" },
                {"UnbanSyntax","Синтаксис: unban < SteamID >" },
                {"BanSyntax","Синтаксис: ban < SteamID > < причина > < дни бана (необязательно) > " },
                { "PlayerNotFound", "Игрок {0} не найден в базе банов\n" },
                { "PlayerDefinition", "Игрока нельзя разбанить без согласования с админом!\nСвязанные SteamID: {0}</color></color>" },
                { "PlayerAlreadyBanned", "Игрок {0} уже забанен.\n" },
                { "CantBanOwner", "Нельзя забанить Админа\n" },
                { "CantBanModerator", "Нельзя забанить модератора\n" },
                { "Changed", "Продолжительность бана для игрока {0} изменена на {1}</color></color>" },
                { "day", "дн." },
                { "Permanent", "пожизненное" },
                { "BanEvade", "Обход бана" },
                { "PlayerBanned", "<color=#FF0000>[LF]</color><color=#FFFFFF> Игрок <color=#FF0000>{0}</color> был навсегда забанен на сервере! Причина: <color=#FF0000>{1}</color></color>" },
                { "PlayerBannedTemp", "<color=#FF0000>[LF]</color><color=#FFFFFF> Игрок <color=#FF0000>{0}</color> был забанен на <color=#FF0000>{1}</color> Причина: <color=#FF0000>{2}</color></color>" },
                { "DatabaseError", "Database receiving error: list.Count != 1!\n" },
                { "InformMessage", "Незаслуженный бан? Подайте заявку на <color=#FFA500>vk.com/lifefinerust</color>" },
                { "ExitMessagePermBan", "Вы забанены НАВСЕГДА!\nПричина: " },
                { "ExitMessageTempBan", "Вы получили временный бан! Осталось: {0} Причина: " }
            }, this, "ru");
        }
        private string Msg(string key, string userID = null) =>
            lang.GetMessage(key, this, userID);
        #endregion


        #region Json

        private PlayerInfo.Summaries _summaries;

        private class SendList
        {
            public string name { get; set; }
            public bool inline { get; set; }
            public string value { get; set; }
        }

        public class PlayerInfo
        {
            public List<Summaries> summaries = new List<Summaries>();
            public class Summaries
            {
                [JsonProperty("response")] public Content response;

                public class Content
                {
                    [JsonProperty("players")] public Player[] Players;

                    public class Player
                    {
                        [JsonProperty("personaname")] public string PersonaName;

                    }
                }
            }
        }

        #endregion


    }
}