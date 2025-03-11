using Oxide.Core.Configuration;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Text;
using Oxide.Core.Database;
using System.Net;
using static Oxide.Plugins.BanSystem;

namespace Oxide.Plugins
{
    [Info("BanSystem", "MaltrzD", "1.0.8")]
    class BanSystem : RustPlugin
    {
        public static DateTime unixEpoch { get; private set; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static BanSystem _ins;
        Effect banEffect = new Effect();

		private Connection _mySqlConnection = null;
		private readonly Core.MySql.Libraries.MySql _mySql = new Core.MySql.Libraries.MySql();

		private const string PERM_BAN = "bansystem.ban";
        private const string PERM_KICK = "bansystem.kick";
        private const string PERM_UNBAN = "bansystem.unban";

        private const string DISCORD_BAN_MESSAGE = @"{
                    ""avatar_url"": ""https://sun9-41.userapi.com/impg/XG4lWeqt5KHP7Sss_58bLFzDGNAjbf3Gb3D-Tw/mLipoINPLJ8.jpg?size=96x96&quality=96&sign=97b16e2cb2729ebb02f39fbe0ac3620e&type=album"",
                    ""embeds"": [
                        {
                            ""title"": ""Игрок был забанен!"",
                            ""color"": 16728345,
                            ""url"": """",
                            ""fields"": [
                                { 
                                ""name"": ""Игрок"",
                                ""value"": ""[{0}](https://steamcommunity.com/profiles/{0}) / {1}"" 
                                },  
                                { 
                                ""name"": ""Забанил"",
                                ""value"": ""{2}"" 
                                },
                                { 
                                ""name"": ""Время"",
                                ""value"": ""{3}"" 
                                },
                                { 
                                ""name"": ""Причина"",
                                ""value"": ""{4}"" 
                                }
                            ],
                            ""footer"": {}
                        }
                    ]
                    }";
        private const string DISCORD_UNBAN_MESSAGE = @"{
                    ""avatar_url"": ""https://sun9-41.userapi.com/impg/XG4lWeqt5KHP7Sss_58bLFzDGNAjbf3Gb3D-Tw/mLipoINPLJ8.jpg?size=96x96&quality=96&sign=97b16e2cb2729ebb02f39fbe0ac3620e&type=album"",
                    ""embeds"": [
                        {
                            ""title"": ""Игрок был разбанен!"",
                            ""color"": 2227982,
                            ""url"": """",
                            ""fields"": [
                                { 
                                ""name"": ""Игрок"",
                                ""value"": ""[{0}](https://steamcommunity.com/profiles/{0}) / {1}"" 
                                },  
                                { 
                                ""name"": ""Разбанил"",
                                ""value"": ""{2}"" 
                                }
                            ],
                            ""footer"": { }
                        }
                    ]
                    }";

		#region [ OXIDE HOOK ]
		private void Loaded()
        {
            _ins = this;

            ReadConfig();
            LoadPlayerNickNames();

            PermissionService.RegisterPermissions(new List<string>() { PERM_BAN, PERM_KICK, PERM_UNBAN } );

            timer.Every(300f, () => Save());

			_mySqlConnection = _mySql.OpenDb($"Server={_config.DataBase.server};Port={_config.DataBase.port};Database={_config.DataBase.database};User={_config.DataBase.username};Password={_config.DataBase.password};Pooling=false;default command timeout=120;Allow Zero Datetime=true;charset=utf8;", this);

			_mySql.Insert(Sql.Builder.Append("CREATE TABLE IF NOT EXISTS banneds ("
                + "banID INT AUTO_INCREMENT PRIMARY KEY, "
                + "steamID BIGINT UNSIGNED, "
                + "name TEXT COLLATE utf8_general_ci, "
                + "ip TEXT COLLATE utf8_general_ci, "
                + "actor TEXT COLLATE utf8_general_ci, "
                + "reason TEXT COLLATE utf8_general_ci, "
                + "banTime DATETIME, "
                + "expiredTime BIGINT, "
                + "UNIQUE (steamID)"
                + ")"), _mySqlConnection,
                (_) =>
                {
                    Puts($"[BD] Подключение завершено!");
                });
        }
        private void Unload() => SavePlayerNickNames();

		private void CanUserLogin(string name, string id, string ipAddress)
		{
            timer.Once(1f, () =>
            {
                CheckUser(Convert.ToUInt64(id), ipAddress);
            });
        }
        private void OnPlayerConnected(BasePlayer player) => UpdatePlayerData(player);
		private void CheckUser(ulong id, string ipAddress)
        {
            ulong userId = Convert.ToUInt64(id);
            ipAddress = ipAddress.Split(':')[0];
            foreach (var con in Network.Net.sv.connections.ToList())
            {
                if(con.userid == id)
                {
					GetBanBySteamID(userId, (ban) =>
					{
						if (ban != null)
						{
							DateTime futureTime = ConvertUnixTimeToDateTime(ban.ExpiredTime);
							if (DateTime.Now > futureTime && ban.ExpiredTime != 0)
							{
								UnbanBySteamID(Convert.ToUInt64(userId), null);
							}
							else
							{
								string banReason = ban.ExpiredTime == 0 ? "навсегда" : futureTime.ToString();
								//player.Kick(_config.Lang.ReplaceArgs(_config.Lang.BannedMessage, ban.Reason, banReason));
                                Network.Net.sv.Kick(con, _config.Lang.ReplaceArgs(_config.Lang.BannedMessage, ban.Reason, banReason));
								return;
							}
						}
						else
						{
							if (_config.BanEvade.IsEnable == false) return;

							GetBanByIP(ipAddress, (ipBan) =>
							{
								if (ipBan != null)
								{
									long time = 0;
									if (string.IsNullOrEmpty(_config.BanEvade.BanTime) == false && IsTime(_config.BanEvade.BanTime))
										time = GetTimeFromString(DateTime.Now, _config.BanEvade.BanTime);


									Ban newBan = BanPlayer(userId, null, "SERVER", _config.BanEvade.Reason, time, ipAddress);
									string banReason = ban.ExpiredTime == 0 ? "навсегда" : ConvertUnixTimeToDateTime(ban.ExpiredTime).ToString("dd:MM:dd hh:mm:ss");

									Network.Net.sv.Kick(con, _config.Lang.ReplaceArgs(_config.Lang.BannedMessage, newBan.Reason, banReason));
									return;
								}
							});
						}
					});

                    return;
				}
            }
        }
        #endregion

        #region [ MAIN ]
        private void Save() => SavePlayerNickNames();
        #endregion

        #region [ CHAT COMMAND ]
        [ChatCommand("ban")]
        private void BanPlayer_ChatCommand(BasePlayer player, string cmd, string[] args)
        {
            if(PermissionService.HasPermission(player.UserIDString, PERM_BAN) == false)
            {
                player.ChatMessage("У вас нет прав чтобы использовать эту команду!");
                return;
            }

            if(args.Length == 0)
            {
                player.ChatMessage("Не все аргументы указаны!");
                return;
            }

            ulong targetUserId = 0;
            if (IsSteamID(args[0]) == false)
            {
                BasePlayer targetPlayer = FindPlayer(args[0]);
                if(targetPlayer == null)
                {
                    player.ChatMessage("Такого игрока нет на сервере!");
                    return;
                }
                else
                {
                    targetUserId = targetPlayer.userID;
                }
            }
            else
            {
                targetUserId = Convert.ToUInt64(args[0]);
            }

            long time = 0;
            if(args.Length >= 2)
            {
                if (IsTime(args[1]))
                {
                    time = GetTimeFromString(DateTime.Now, args[1]);
                }
            }

            string reason = "";
            if(args.Length >= 2)
            {
                if(time != 0)
                {
                    for (int i = 2; i < args.Length; i++)
                    {
                        reason += args[i] + " ";
                    }
                }
                else
                {
                    for (int i = 1; i < args.Length; i++)
                    {
                        reason += args[i] + " ";
                    }
                }
            }
            if(string.IsNullOrEmpty(reason)) { reason = _config.Main.defaultBanReason; }

            BanPlayer(targetUserId, player, player.displayName, reason, time, formattedTime: time != 0 ? GetFormattedTime(args[1]) : null);
        }
        [ChatCommand("unban")]
        private void UnbanPlayer_ChatCommand(BasePlayer player, string cmd, string[] args)
        {
            if (PermissionService.HasPermission(player.UserIDString, PERM_UNBAN) == false)
            {
                player.ChatMessage("У вас нет прав чтобы использовать эту команду!");
                return;
            }

            if (args.Length == 0) { player.ChatMessage("Недостаточно аргументов!\nСинтаксис: /unban <SteamID>"); return; }

            ulong uid = 0;
            try
            {
                uid = Convert.ToUInt64(args[0]);
            }
            catch
            {
                player.ChatMessage("Вы указали неверный SteamID");
                return;
            }

            UnbanBySteamID(uid, (unban) =>
            {
                if(unban) player.ChatMessage($"Запись успешно удалена!");
				else player.ChatMessage($"Запись не найдена!");
			}, player.displayName, true);
        }

        [ChatCommand("kick")]
        private void KickPlayer_ChatCommand(BasePlayer player, string cmd, string[] args)
        {
            if (PermissionService.HasPermission(player.UserIDString, PERM_KICK) == false)
            {
                player.ChatMessage("У вас нет прав чтобы использовать эту команду!");
                return;
            }

            if (args.Length == 0)
                player.ChatMessage(KickPlayer(null, null));
            else if (args.Length == 1)
                player.ChatMessage(KickPlayer(args[0], null));
            else
            {
                string reason = string.Empty;

                for (int i = 1; i < args.Length; i++)
                {
                    reason += args[i] + " ";
                }

                player.ChatMessage(KickPlayer(args[0], reason));
            }
        }
        #endregion

        #region [ CONSOLE COMMAND ]
        [ConsoleCommand("banp")]
        private void BanPlayer_ConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin == false) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                Debug.LogWarning("Не все аргументы указаны!");
                return;
            }
            ulong steamId = Convert.ToUInt64(arg.Args[0]);

            long time = 0;
            if (arg.Args.Length >= 2)
            {
                if (IsTime(arg.Args[1]))
                {
                    time = GetTimeFromString(DateTime.Now, arg.Args[1]);
                }
            }

            string reason = "";
            if (arg.Args.Length >= 2)
            {
                if (time != 0)
                {
                    for (int i = 2; i < arg.Args.Length; i++)
                    {
                        reason += arg.Args[i] + " ";
                    }
                }
                else
                {
                    for (int i = 1; i < arg.Args.Length; i++)
                    {
                        reason += arg.Args[i] + " ";
                    }
                }
            }
            if (string.IsNullOrEmpty(reason)) { reason = _config.Main.defaultBanReason; }


            string ip = "";
            PlayerData data = GetPlayerData(steamId);
            if (data != null)
            {
                ip = data.Ip;
            }

            BanPlayer(steamId, null, "CONSOLE", reason, time, playerIp: ip, formattedTime: time != 0? GetFormattedTime(arg.Args[1]) : null);
        }
        [ConsoleCommand("unbanp")]
        private void Unban_ConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin == false) return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                Puts("Вы не указали SteamID!");
                return;
            }
            if (IsSteamID(arg.Args[0]) == false)
            {
                Puts($"Неверный SteamID!");
                return;
            }

            UnbanBySteamID(Convert.ToUInt64(arg.Args[0]), (unban) =>
            {
                if (unban) Puts("Запись успешно удалена!");
                else Puts("Запись не найдена!");
            }, "CONSOLE", true);
        }

        [ConsoleCommand("checkban")]
        private void CheckBanBySteamId_ConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin == false) return;

            ulong steamId = 0;

            try
            {
                steamId = Convert.ToUInt64(arg.Args[0]);
            }
            catch
            {
                Puts("Вы ввели неверный SteamID или не указали его!");
                return;
            }

            GetBanBySteamID(steamId, (ban) =>
            {
                if(ban == null)
                {
                    Puts($"Игрок [{steamId}] не забанен!");
                    return;
                }

                string unbanTime = ban.ExpiredTime == 0 ? "навсегда" : ConvertUnixTimeToDateTime(ban.ExpiredTime).ToString("dd/MM/dd hh:mm:ss");

				Puts(
                    $"\nИгрок: {ban.SteamID}/{ban.Name}\n" +
                    $"IP: {ban.Ip}\n" +
                    $"Причина: {ban.Reason}\n" +
                    $"Время блокировки: {ban.BanTime.ToString("dd/MM/dd hh:mm:ss")}\n" +
                    $"Время разблокировки: {unbanTime}"
                    );
            });
        }
		[ConsoleCommand("checkbanip")]
		private void CheckBansIp_ConsoleCommand(ConsoleSystem.Arg arg)
		{
			if (arg.IsAdmin == false) return;

			string ip = arg.Args[0];

            GetBansByIP(ip, (bans) =>
            {
                if(bans == null)
                {
                    Puts("Баны не найдены!");
                    return;
                }

                StringBuilder sb = new StringBuilder();
                sb.Append($"Баны по IP: {ip}\n");
				foreach (var ban in bans)
				{
				    string unbanTime = ban.ExpiredTime == 0 ? "навсегда" : ConvertUnixTimeToDateTime(ban.ExpiredTime).ToString("dd/MM/dd hh:mm:ss");

                    sb.AppendLine($"\nИгрок: {ban.SteamID}/{ban.Name}\n" +
                    $"IP: {ban.Ip}\n" +
                    $"Причина: {ban.Reason}\n" +
                    $"Время блокировки: {ban.BanTime.ToString("dd/MM/dd hh:mm:ss")}\n" +
                    $"Время разблокировки: {unbanTime}\n");

                    sb.AppendLine("-----");
                }

                Puts(sb.ToString());
            });
		}


		[ConsoleCommand("kick")]
        private void KickPlayer_ConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin == false) return;

            if (arg.Args?.Length == 0)
                Puts(KickPlayer(null, null));
            else if (arg.Args.Length == 1)
                Puts(KickPlayer(arg.Args[0], null));
            else
            {
                string reason = string.Empty;

                for (int i = 1; i < arg.Args.Length; i++)
                {
                    reason += arg.Args[i] + " ";
                }

                Puts(KickPlayer(arg.Args[0], reason));
            }
        }
        #endregion

        #region [ EXT ]
        private string GetFormattedTime(string time)
        {
            string result = "";
            if (time.EndsWith("d"))
            {
                result = time.Replace("d", "д");
            }
            else if (time.EndsWith("s"))
            {
                result = time.Replace("s", "с");
            }
            else if (time.EndsWith("m"))
            {
                result = time.Replace("m", "м");
            }
            else if (time.EndsWith("h"))
            {
                result = time.Replace("h", "ч");
            }
            else if (time.EndsWith("y"))
            {
                result = time.Replace("y", "г");
            }
            return result;
        }
        private DateTime ConvertUnixTimeToDateTime(long unixTime)
            => unixEpoch.AddSeconds(unixTime);
        private long GetTimeFromString(DateTime currentTime, string timeString)
        {
            int time = Convert.ToInt32(timeString.Substring(0, timeString.Length - 1));
            string timeType = timeString.Substring(timeString.Length - 1, 1);

            DateTime expiredTime = DateTime.Now;

            switch(timeType)
            {
                case "s":
                    expiredTime = expiredTime.AddSeconds(time);
                    break;
                case "m":
                    expiredTime = expiredTime.AddMinutes(time);
                    break;
                case "h":
                    expiredTime = expiredTime.AddHours(time);
                    break;
                case "d":
                    expiredTime = expiredTime.AddDays(time);
                    break;
                case "y":
                    expiredTime = expiredTime.AddYears(time);
                    break;
            }

            TimeSpan interval = expiredTime - unixEpoch;
            long expiredUnixTime = (long)interval.TotalSeconds;


            return expiredUnixTime;
        }
        private bool IsTime(string input)
        {
            string pattern = @"\b\d+[sdhmy]\b";
            return Regex.IsMatch(input, pattern);
        }
        private BasePlayer FindPlayer(string filter)
        {
            return BasePlayer.activePlayerList.Where(x => x.displayName.Contains(filter, System.Globalization.CompareOptions.IgnoreCase) || x.UserIDString == filter).FirstOrDefault();
        }
        private BasePlayer FindPlayer(ulong steamId) => BasePlayer.FindByID(steamId);


        public static class PermissionService
        {
            public static void RegisterPermissions(List<string> perms)
            {
                foreach (var perm in perms)
                    if (_ins.permission.PermissionExists(perm, _ins) == false)
                        _ins.permission.RegisterPermission(perm, _ins);
            }
            public static void RegisterPermission(string perm)
            {
                if (_ins.permission.PermissionExists(perm, _ins) == false)
                    _ins.permission.RegisterPermission(perm, _ins);
            }
            public static bool HasPermission(string uid, string perm)
                => _ins.permission.UserHasPermission(uid, perm);

        }

        private void Dlog(string discordWebHook, string message)
        {
            if(string.IsNullOrEmpty(discordWebHook) || discordWebHook.Contains("http") == false) { return; }

            try
            {
                webrequest.Enqueue(discordWebHook, message, (code, response) =>
                {
                    Puts($"{code} : {response}");
                }, this, Core.Libraries.RequestMethod.POST, new Dictionary<string, string>() { { "Content-Type", "application/json" } }, 5000);
            }
            catch(Exception ex) { Debug.LogError($"Ошибка при отправке сообщения [Discord]: {ex.Message}"); }
        }
        private string GetMessage(string key, params object[] args)
        {
            string msg = key;
            for (int i = 0; i < args.Length; i++)
            {
                msg = msg.Replace($"{{{i}}}", args[i].ToString());
            }
            return msg;
        }
        public bool IsSteamID(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }
            if (input.Length == 17)
            {
                foreach (char c in input)
                {
                    if (!char.IsDigit(c))
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }
        #endregion

        #region [ DATA ] 
        private DynamicConfigFile PlayerData_File = Interface.Oxide.DataFileSystem.GetFile("BanSystem/PlayerData");
        private Dictionary<ulong, PlayerData> PlayerDatas;
        private ConfigData _config;

        private PlayerData? GetPlayerData(ulong userId)
        {
            if(PlayerDatas.TryGetValue(userId, out PlayerData nickname))
            {
                return nickname;
            }

            return null;
        }
        private string? GetPlayerNameById(ulong userId)
        {
            PlayerData data = GetPlayerData(userId);
            if(data != null)
                return data.Name;
            

            return null;
        }
        private string? GetPlayerIpById(ulong userId)
        {
            PlayerData data = GetPlayerData(userId);
            if (data != null)
                return data.Ip;


            return null;
        }
        private void UpdatePlayerData(BasePlayer player)
        {
            PlayerData data = GetPlayerData(player.userID);
            if(data != null)
            {
                data.Ip = player.Connection.IPAddressWithoutPort();
                data.Name = player.displayName;
            }
            else
            {
                PlayerDatas.Add(player.userID, new PlayerData() { Ip = player.Connection.IPAddressWithoutPort(), Name = player.displayName });
            }
        }


        private void LoadPlayerNickNames()
        {
            if (PlayerData_File.Exists() == false)
                SavePlayerNickNames();


            PlayerDatas = PlayerData_File.ReadObject<Dictionary<ulong, PlayerData>>();

            if (PlayerDatas == null)
                PlayerDatas = new Dictionary<ulong, PlayerData>();
        }
        private void SavePlayerNickNames() => PlayerData_File.WriteObject(PlayerDatas);
        #endregion

        #region [ CONFIG ]
        public class ConfigData
        {
            [JsonProperty("Основные настройки")] public MainSettings Main;
            [JsonProperty("Настройка MySql")] public DataBaseSettings DataBase;
            [JsonProperty("Настройка дискорд")] public DiscordSettings Discord;
            [JsonProperty("Настройки обхода блокировки")] public BanEvadeSetting BanEvade;
            [JsonProperty("Настройка звука при блокировке")] public SoundSetting Sound = new SoundSetting();
            [JsonProperty("Сообщения")] public Language Lang;
            public class MainSettings
            {
                [JsonProperty("Дефолтная причина блокировки (если не будет указана)")] public string defaultBanReason = "причина не указана";
                [JsonProperty("Сообщение при бане в чат [true/да | false/нет]")] public bool alertOnBan = true;
                [JsonProperty("Сообщение при кике в чат [true/да | false/нет]")] public bool alertOnKick = true;
                [JsonProperty("Сообщение при разбане в чат [true/да | false/нет]")] public bool alertOnUnban = false;
            }
            public class DiscordSettings
            {
                [JsonProperty("Вебхук банов")] public string discordWebHook_Bans = "123";
                [JsonProperty("Вебхук разбанов")] public string discordWebHook_UnBans = "123";
            }
            public class DataBaseSettings
            {
                [JsonProperty("Хост")] public string server = "localhost";
                [JsonProperty("Порт")] public string port = "3306";
                [JsonProperty("Пользователь")] public string username = "root";
                [JsonProperty("Пароль")] public string password = "root";
                [JsonProperty("Название бд")] public string database = "rusttest";
            }
            public class BanEvadeSetting
            {
                [JsonProperty("Включить блокировку обхода бана")] public bool IsEnable = true;
                [JsonProperty("Время блокировки")] public string BanTime = "30d";
                [JsonProperty("Причина блокировки")] public string Reason = "обход";
            }
            public class Language
            {
                [JsonProperty("Сообщение при бане")] public string BanMessage = "Игрок {0} был заблокирован!\nПричина: {1}";
                [JsonProperty("Сообщение при кике")] public string KickMessage = "Игрок {0} был кикнут!\nПричина {1}";
                [JsonProperty("Сообщение при разбане")] public string UnbanMessage = "Игрок {0} был разбанен!";
                [JsonProperty("Сообщение при заходе на сервер")] public string BannedMessage = "Вы заблокированы, причина: {0}\nДата разблокировки: {1}";
                [JsonProperty("Сообщение игроку при бане")] public string KickbannedMessage = "Вы были заблокированы, причина: {0}\nДата разблокировки: {1}\nЗаявка на разбан: vk.com/rust";

                public string ReplaceArgs(string msg, params object[] args)
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        msg = msg.Replace($"{{{i}}}", args[i].ToString());
                    }

                    return msg;
                }
            }
            public class SoundSetting
            {
                [JsonProperty("Проигрывать звук всем игрокам при блокировке")] public bool Play = false;
                [JsonProperty("Путь до префаба звука")] public string Prefab = "assets/prefabs/missions/portal/proceduraldungeon/effects/disappear.prefab";
            }
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData();

            config.Main = new ConfigData.MainSettings();
            config.Discord = new ConfigData.DiscordSettings();
            config.DataBase = new ConfigData.DataBaseSettings();
            config.BanEvade = new ConfigData.BanEvadeSetting();
            config.Sound = new ConfigData.SoundSetting();
            config.Lang = new ConfigData.Language();

            SaveConfig(config);
        }
        void SaveConfig(object config)
        {
            Config.WriteObject(config, true);
        }
        void ReadConfig()
        {
            base.Config.Settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
            _config = Config.ReadObject<ConfigData>();
            SaveConfig(_config);
        }
        #endregion

        private Ban BanPlayer(ulong userId, BasePlayer actorPlayer, string actor, string reason, long time = 0, string playerIp = "", string formattedTime = null)
        {
            string nickname = "Undefiend";
            string ip = "";

            BasePlayer player = BasePlayer.FindByID(userId);
            if(player != null)
            {
                nickname = player.displayName;
                ip = player.Connection.IPAddressWithoutPort();
            }
            else
            {
                string nn = GetPlayerNameById(userId);
                if(string.IsNullOrEmpty(nn) == false) { nickname = nn; }

                if(string.IsNullOrEmpty(playerIp) == false) { ip = playerIp; }
            }

            if(nickname.Contains("'"))
                nickname = nickname.Replace("'", " ");

            Ban ban = new Ban(userId, nickname, ip, actor, reason, DateTime.Now, time);



            _mySql.Update(Sql.Builder.Append($@"UPDATE banneds 
                SET name = '{ban.Name}', ip = '{ban.Ip}', actor = '{ban.Actor}', reason = '{ban.Reason}', banTime = '{ban.BanTime.ToString("yyyy-MM-dd HH:mm:ss")}', expiredTime = '{ban.ExpiredTime}'
                WHERE steamID = {ban.SteamID}"), _mySqlConnection, (i) =>
                {
                    if(i == 0) 
                    {
						string insertQuery = $@"INSERT INTO banneds (steamID, name, ip, actor, reason, banTime, expiredTime)
                                VALUES ({ban.SteamID}, '{ban.Name}', '{ban.Ip}', '{ban.Actor}', '{ban.Reason}', '{ban.BanTime.ToString("yyyy-MM-dd HH:mm:ss")}', '{ban.ExpiredTime}')";

                        _mySql.Query(Sql.Builder.Append(insertQuery), _mySqlConnection, (_) => { });
					}
                });

            string stringTime = time == 0 ? "Навсегда" : formattedTime;
            string msg =
                $"Игрок {nickname} заблокирован!\n" +
                $"Время: {stringTime}\n" +
                $"Администратор: {actor}\n" +
                $"Причина: {reason}";

            if(_config.Main.alertOnBan)
                Server.Broadcast(_config.Lang.ReplaceArgs(_config.Lang.BanMessage, nickname, stringTime, reason));

            if (actorPlayer != null)
            {
                actorPlayer.ChatMessage(msg);
            }
            else
            {
                Debug.LogWarning(msg);
            }

            BasePlayer targetPlayer = BasePlayer.FindByID(userId);
            if(targetPlayer != null)
            {
                DateTime futureTime = ConvertUnixTimeToDateTime(time);
                string banReason = time == 0 ? "навсегда" : futureTime.ToString();
                targetPlayer.Kick(_config.Lang.ReplaceArgs(_config.Lang.KickbannedMessage, reason, banReason));
                //targetPlayer.Kick("Вы были заблокированы!");
            }

            Interface.Call("OnPlayerBanned", userId);
            Dlog(_config.Discord.discordWebHook_Bans, GetMessage(DISCORD_BAN_MESSAGE, userId, nickname, actor, stringTime, reason));

            if (_config.Sound.Play)
            {
                foreach (var item in BasePlayer.activePlayerList)
                {
                    if (item == null) continue;

                    banEffect.Init(Effect.Type.Generic, item, 0, Vector3.zero, Vector3.forward, item.limitNetworking ? item.Connection : null);
                    banEffect.pooledString = _config.Sound.Prefab;

                    if (item.limitNetworking)
                    {
                        EffectNetwork.Send(banEffect, item.Connection);
                    }
                    else EffectNetwork.Send(banEffect);
                }
            }

            return ban;
        }
        private string KickPlayer(string filter = null, string reason = null)
        {
            if (filter == null)
                return "Не все аргументы указаны!\n" +
                    "Синтаксис: kick <никнейм> <причина (необязательно)>";

            BasePlayer player = FindPlayer(filter);
            if(player == null) return "Игрок не найден!";

            if (reason == null) reason = _config.Main.defaultBanReason;


            player.Kick(reason);
            Server.Broadcast(_config.Lang.ReplaceArgs(_config.Lang.KickMessage, player.displayName, reason));

            return $"Игрок успешно кикнут по причине {reason}";
        }
        private void GetBanBySteamID(ulong steamID, Action<Ban> callback)
        {
            string selectQuery = $"SELECT * FROM banneds WHERE steamID = {steamID}";

            _mySql.Query(Sql.Builder.Append(selectQuery), _mySqlConnection, (row) => 
            {
                if (row.Count == 0) 
                {
					callback?.Invoke(null);
                    return;
				}
                else
                {
                    Dictionary<string, object> banRecord = row.ElementAt(0);
					Ban ban = new Ban
                    (
                    	Convert.ToUInt64(banRecord["steamID"]),
                    	banRecord["name"].ToString(),
                    	banRecord["ip"].ToString(),
                    	banRecord["actor"].ToString(),
                    	banRecord["reason"].ToString(),
                    	Convert.ToDateTime(banRecord["banTime"]),
                    	Convert.ToInt64(banRecord["expiredTime"])
                    );

                    callback?.Invoke(ban);
                    return;
				}
            });
        }

        private void GetBansByIP(string ip, Action<List<Ban>> callback)
        {
			if (string.IsNullOrEmpty(ip))
			{
				callback?.Invoke(null);
				return;
			}

			string selectQuery = $"SELECT * FROM banneds WHERE ip = '{ip}' AND ip IS NOT NULL AND ip <> ''";

			_mySql.Query(Sql.Builder.Append(selectQuery), _mySqlConnection, (row) =>
			{
				if (row.Count == 0)
				{
					callback?.Invoke(null);
				}
				else
				{

                    List<Ban> bans = new List<Ban>();
                    foreach (var banRows in row)
                    {
                        bans.Add(new Ban
                        (
                            Convert.ToUInt64(banRows["steamID"]),
                            banRows["name"].ToString(),
                            banRows["ip"].ToString(),
                            banRows["actor"].ToString(),
                            banRows["reason"].ToString(),
                            Convert.ToDateTime(banRows["banTime"]),
                            Convert.ToInt64(banRows["expiredTime"])
                        ));
                    }

                    if(bans.Count == 0)
                    {
                        callback.Invoke(null);
                        return;
                    }
                    else
                    {
					    callback.Invoke(bans);
                    }
				}
			});
		}
        private void GetBanByIP(string ip, Action<Ban> callback)
        {
            if (string.IsNullOrEmpty(ip))
            {
                callback?.Invoke(null);
                return;
            }

            string selectQuery = $"SELECT * FROM banneds WHERE ip = '{ip}' AND ip IS NOT NULL AND ip <> ''";

			_mySql.Query(Sql.Builder.Append(selectQuery), _mySqlConnection, (row) =>
			{
                if (row.Count == 0) 
                {
					callback?.Invoke(null);
				}
                else
                {
                    Dictionary<string, object> banRows = row.ElementAt(0);
					Ban ban = new Ban
                    (
                        Convert.ToUInt64(banRows["steamID"]),
						banRows["name"].ToString(),
						banRows["ip"].ToString(),
						banRows["actor"].ToString(),
						banRows["reason"].ToString(),
                        Convert.ToDateTime(banRows["banTime"]),
                        Convert.ToInt64(banRows["expiredTime"])
                    );

                    callback.Invoke(ban);
                    return;
                }
			});
        }
        private void UnbanBySteamID(ulong steamID, Action<bool> callback, string actor = "Undefiend", bool notify = false)
        {
            string deleteQuery = $"DELETE FROM banneds WHERE steamID = {steamID}";

			_mySql.Delete(Sql.Builder.Append(deleteQuery), _mySqlConnection, (i) =>
			{
                if (i == 0) callback?.Invoke(false);
                else
                {
                    callback?.Invoke(true);

					if (notify)
					{
						string nname = GetPlayerNameById(steamID);
						string name = nname != null ? nname : "Undefiend";
						Dlog(_config.Discord.discordWebHook_UnBans, GetMessage(DISCORD_UNBAN_MESSAGE, steamID, name, actor));
						if (_config.Main.alertOnUnban)
							Server.Broadcast(_config.Lang.ReplaceArgs(_config.Lang.UnbanMessage, name));
					}
				}
			});
        }


        public class Ban
        {
            public ulong SteamID = 0;
            public string Name = "Undefiend";
            public string Ip = "127.0.0.1";

            public string Actor = "Undefiend";
            public string Reason = "No given";

            public DateTime BanTime = DateTime.Now;
            public long ExpiredTime;
            public Ban(ulong steamId, string name, string ip, string actor, string reason, DateTime banTime, long expiredTime)
            {
                SteamID = steamId;
                Name = name;
                Ip = ip;
                    
                Actor = actor;
                Reason = reason;

                BanTime = banTime;
                ExpiredTime = expiredTime;
            }
        }
        public class PlayerData
        {
            public string Name;
            public string Ip;
        }
    }
}