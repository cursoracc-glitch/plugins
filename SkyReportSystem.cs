using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries;
using ConVar;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("SkyReportSystem", "DezLife", "3.2.0")]
    [Description("Report system for rust")]
    public class SkyReportSystem : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin IQChat, ImageLibrary;
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);

        Dictionary<ulong, ulong> CheckPlayerModeration = new Dictionary<ulong, ulong>();
        #endregion

        #region Config
        private Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "VK:", Order = 0)]
            public VKontakte vKontakte;

            [JsonProperty(PropertyName = "Discord:", Order = 1)]
            public Discord discord;

            [JsonProperty(PropertyName = "Setting:", Order = 2)]
            public Setting setting;

            [JsonProperty(PropertyName = "Проверка на AFK:", Order = 3)]
            public IsAfk isafk;

            [JsonProperty(PropertyName = "Причины бана:", Order = 4)]
            public List<BanReasons> banReasons;

            [JsonProperty("Настройки плагина", Order = 5)]
            public List<string> Reasonsforcomplaint;
        }

        public class BanReasons
        {
            [JsonProperty(PropertyName = "Причина бана:", Order = 0)]
            public string BanReason;

            [JsonProperty(PropertyName = "Команда для бана:", Order = 1)]
            public string BanReasonCommand;
        }

        public class IsAfk
        {
            [JsonProperty(PropertyName = "Включить проверку на AFK ?", Order = 0)]
            public bool usecheckafk;

            [JsonProperty(PropertyName = "Время между проверками на AFK", Order = 2)]
            public float timecheckisafk;
        }


        public class Setting
        {
            [JsonProperty(PropertyName = "Аватар для сообщений(Для работы с IQChat )", Order = 0)]
            public ulong avatarid;

            [JsonProperty(PropertyName = "Префикс(Для работы с IQChat )", Order = 1)]
            public string prefix;

            [JsonProperty(PropertyName = "Колличевство репортов для вызова на проверку", Order = 3)]
            public int maxreportcall;

            [JsonProperty(PropertyName = "Отправлять сообщения модераторам в чат о том что игрок превысил количевство репортов (Требуется разрешения SkyReportSystem.moderator)", Order = 4)]
            public bool usemodercall;

            [JsonProperty(PropertyName = "Включить логирование ?", Order = 5)]
            public bool uselog;

            [JsonProperty(PropertyName = "Кд на отправку репортов", Order = 6)]
            public int Cooldown;

            [JsonProperty(PropertyName = "Названия сервера", Order = 7)]
            public string servername;

            [JsonProperty(PropertyName = "Команда для открытия репорт меню", Order = 8)]
            public string comandplayer;

            [JsonProperty(PropertyName = "Команда для открытия модер меню", Order = 9)]
            public string comandmoder;

            [JsonProperty(PropertyName = "Сообщения в титле", Order = 10)]
            public string teatletxt;
        }

        public class VKontakte
        {
            [JsonProperty(PropertyName = "Используем Вконтакте:", Order = 0)]
            public bool UseVK;

            [JsonProperty(PropertyName = "ID Беседы ВК для бота:", Order = 1)]
            public string VK_ChatID;

            [JsonProperty(PropertyName = "Token Группы ВК:", Order = 2)]
            public string VK_Token;
        }
        public class Discord
        {
            [JsonProperty(PropertyName = "Используем Discord:", Order = 0)]
            public bool UseDiscord;

            [JsonProperty(PropertyName = "Webhook Discrod:", Order = 1)]
            public string Discord_webHook;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration()
            {
                vKontakte = new VKontakte()
                {
                    UseVK = false,
                    VK_ChatID = "ChatID",
                    VK_Token = "VKToken",
                },
                discord = new Discord()
                {
                    Discord_webHook = "webHook",
                    UseDiscord = false,
                },
                setting = new Setting()
                {
                    avatarid = 76561198854646370,
                    prefix = "SkyReportSystem",
                    maxreportcall = 4,
                    usemodercall = true,
                    uselog = false,
                    Cooldown = 360,
                    servername = "Server Name",
                    comandplayer = "report",
                    comandmoder = "reportm",
                    teatletxt = "ReportSystem by DezLife"

                },
                isafk = new IsAfk()
                {
                    usecheckafk = false,
                    timecheckisafk = 15f,
                },
                banReasons = new List<BanReasons>()
                {
                    new BanReasons
                    {
                         BanReason = "Читы",
                         BanReasonCommand = "ban {0} 999d Soft",
                    },
                    new BanReasons
                    {
                         BanReason = "Макросы",
                         BanReasonCommand = "ban {0} 30d Macros",
                    },
                    new BanReasons
                    {
                         BanReason = "Превышение максимальной численности игроков в команде",
                         BanReasonCommand = "ban {0} 14d 3+",
                    },
                    new BanReasons
                    {
                         BanReason = "Отказ от проверки",
                         BanReasonCommand = "ban {0} 7d Otkaz",
                    },
                    new BanReasons
                    {
                         BanReason = "Другое",
                         BanReasonCommand = "ban {0} 999d 5",
                    },
                },
                Reasonsforcomplaint = new List<string>
                {
                    "3+",
                    "Подозрения в читах",
                    "Макросы",
                    "Реклама в нике",
                    "Спам в чат",
                    "Обман игроков",
                    "Багоюз",
                    "Токсик",
                }

            };
            SaveConfig(config);
        }

        void SaveConfig(Configuration config)
        {
            Config.WriteObject(config, true);
            SaveConfig();
        }

        public void LoadConfigVars()
        {
            config = Config.ReadObject<Configuration>();
            Config.WriteObject(config, true);
        }
        #endregion

        #region data

        public Dictionary<ulong, ReportUser> ReportData = new Dictionary<ulong, ReportUser>();

        public class ReportUser
        {
            public int ReportCount;
            public int CheckCount;
            public List<string> complaint = new List<string>();
        }
        #endregion

        #region Lang

        protected override void LoadDefaultMessages()
        {
            Dictionary<string, string> Lang = new Dictionary<string, string>
            {
                ["SKY_REPORT_REPORTING"] = "Вы успешно отправили репорт на игрока {0}",
                ["SKY_REPORT_REPORTING_VK_DISCORD"] = "=====================================================\nСервер : {0}\nИгрок : {1} пожаловался на {2} [{3}]\nПричина : {4}",
                ["SKY_REPORT_MAXIMUM_REPORT"] = "Игрок: {0} " +
                " Превысил максимальное количевство репортов!" +
                "Его колличевство {1}",
                ["SKY_COLDOWN"] = "Вы недавно отправляли жалобу,подождите еще немного",
                ["SKY_ERROR_PLAYER"] = "Вы жалуетесь сами на себя!",
                ["SKY_PLAYER_PERMISSION_IS_FOUND"] = "У вас недостаточно прав для использования данной команды!",
                ["SKY_NO_CHECK_MODERATION_OR_CHECKING"] = "Вы не можете вызвать игрока {0} на проверку,т.к его проверяет другой модератор",
                ["SKY_ACESS_CHECKING"] = "Вы вызвали игрока {0} на проверку",
                ["SKY_MODER_CHECK_PLAYER_GOING"] = "=====================================================\nМодератор {0} вызвал на проверку игрока {1}[{2}]",
                ["SKY_MODER_CHECK_PLAYER_GOING_CONSOLE"] = "=====================================================\nИгрок {1}[{2}] был вызван с помощью консоли",
                ["SKY_PLAYER_IS_AFK_VK_DISCORD_OK"] = "=====================================================\nИгрок: {0}  двигался с момента последней проверки на AFK",
                ["SKY_PLAYER_IS_AFK_VK_DISCORD_NO"] = "=====================================================\nИгрок: {0} не двигался с момента последней проверки на AFK",
                ["SKY_PLAYER_IS_AFK_OK"] = "Игрок: <color=orange>{0}</color> двигался с момента последней проверки на AFK",
                ["SKY_PLAYER_IS_AFK_NO"] = "Игрок: <color=orange>{0}</color> не двигался с момента последней проверки на AFK",
                ["SKY_MODERATOR_STOPID_CHEKING"] = "Вы успешно закончили проверку над игроком : {0} !",
                ["SKY_PLAYER_STOPID_CHEKING"] = "Проверка успешно окончена. \nПроверял модератор {0} !",
                ["SKY_PLAYER_CHECKING_MODER_MENU_MODERATION"] = "Игрок проверяется модератором",
                ["SKY_PLAYER_NOTHING_MESSAGE_SKYPE_DISCORD"] = "Вы ничего не ввели!",
                ["SKY_MODER_CHECK_P_SKYPE"] = "Вы предоставили свой Skype : {0}",
                ["SKY_MODER_CHECK_P_DISCORD"] = "Вы предоставили свой Discord : {0}",
                ["SKY_Menu_Info"] = "В данном окне вам нужно выбрать игрока из списка или ввести его ник в поле ниже что бы оставить на него жалобу",
                ["SKY_SKYPE_MESSAGE"] =
                                           "=====================================================\n" +
                                           "Сервер : {0}\n" +
                                           "Игровой ник: {1}\n" +
                                           "Steam ID: {2}\n" +
                                           "Предоставил Skype на проверку : {3}",
                ["SKY_DISCORD_MESSAGE"] =
                                           "=====================================================\n" +
                                           "Сервер : {0}\n" +
                                           "Игровой ник: {1}\n" +
                                           "Steam ID: {2}\n" +
                                           "Предоставил Discord на проверку : {3}",
                ["SKY_SKYPE_DISCORD_ERROR"] = "Вас должны вызвать на проверку,перед тем,как отправлять данные",
                ["SKY_REPORT_VK_DISCORD_MAXIMUM_BLACK"] =
                                           "=====================================================\n" +
                                           "Сервер : {0}\n" +
                                           "Достигнут предел жалоб на игрока!\n" +
                                           "Игровой ник: {1}\n" +
                                           "Steam ID: {2}\n" +
                                           "Информация о подозрительном игроке:\n" +
                                           "Игрок проверялся: {3} раз(-а)\n" +
                                           "Стим: https://steamcommunity.com/profiles/{4}",
                ["SKY_CHECK_STOP"] =
                                           "=====================================================\n" +
                                           "Сервер : {0}\n" +
                                           "Модератор {1} окончил проверку над игроком {2} [{3}]\n",
                ["SKY_CHECK_STOP_CONSOLE"] =
                                           "=====================================================\n" +
                                           "Сервер : {0}\n" +
                                           "Проверка окончена над игроком {2} [{3}]\n",
                ["BAN_USER_VK_DISCORD"] =
                                           "=====================================================\n" +
                                           "Игрок :{0}({1})\n" +
                                           "Был забанен на сервере({2}) по причине {3} Модератором {4}\n",
            };
            lang.RegisterMessages(Lang, this);
            PrintWarning("Языковой файл загружен успешно");

        }
        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            LoadConfigVars();
            if (!ImageLibrary)
            {
                PrintError("Не найден ImageLibrary, плагин не будет работать!");
                return;
            }
            #region Permission
            permission.RegisterPermission("SkyReportSystem.moderator", this);
            permission.RegisterPermission("SkyReportSystem.ban", this);
            #endregion

            #region DataLoad
            LoadData("ReportData", ref ReportData, true);

            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
                OnPlayerConnected(BasePlayer.activePlayerList[i]);
            #endregion


            cmd.AddChatCommand(config.setting.comandplayer, this, nameof(ReportMenu));
            cmd.AddChatCommand(config.setting.comandmoder, this, nameof(modermenu));

            if (config.setting.servername == "Server Name")
            {
                PrintWarning("Вы не указали названия сервера, плагин будет работать некорректно!");
            }

            PrintError($"-----------------------------------");
            PrintError($"           SkyReportSystem         ");
            PrintError($"          Author = DezLife         ");
            PrintError($"          Version = {Version}      ");
            PrintError($"-----------------------------------");
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (!ReportData.ContainsKey(player.userID))
            {
                ReportUser NewUser = new ReportUser()
                {
                    ReportCount = 0,
                    CheckCount = 0,
                    complaint = new List<string> { }
                };
                ReportData.Add(player.userID, NewUser);
            }
            if (CheckPlayerModeration.ContainsKey(player.userID))
            {
                BasePlayer players = BasePlayer.FindByID(CheckPlayerModeration[player.userID]);
                if (players != null)
                {
                    SendChat(players, "Вызываемый игрок покинул сервер!");
                }
                else
                {
                    PrintWarning("Вызываемый игрок покинул сервер!");
                }
                BanPlayer.Remove(player.userID);
            }
        }

        void Unload()
        {
            SaveData("ReportData", ReportData);
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                var p = BasePlayer.activePlayerList[i];
                CuiHelper.DestroyUi(p, mainskymenu);
                CuiHelper.DestroyUi(p, ModerMenuSky);
                CuiHelper.DestroyUi(p, ModerAlert);
            }
        }

        #endregion

        #region Metods

        private void ReportActivity(BasePlayer reportplayer, BasePlayer target, string reason)
        {
            if (ReportData[target.userID].ReportCount >= config.setting.maxreportcall && target.userID != reportplayer.userID)
            {
                SendChatMessage("SKY_REPORT_VK_DISCORD_MAXIMUM_BLACK", config.setting.servername, target.displayName, target.userID, ReportData[target.userID].CheckCount, target.userID);
            }
            if (config.discord.UseDiscord == true && ReportData[target.userID].ReportCount >= config.setting.maxreportcall && target.userID != reportplayer.userID)
            {
                SendDiscordMsg("SKY_REPORT_VK_DISCORD_MAXIMUM_BLACK", config.setting.servername, target.displayName, target.userID, ReportData[target.userID].CheckCount, target.userID);
            }
            if (config.setting.uselog == true && ReportData[target.userID].ReportCount >= config.setting.maxreportcall && target.userID != reportplayer.userID)
            {
                LogToFile("SkyReportSystemLOG", $"На игрока <color=#816AD0>{target.displayName}</color> пожаловался <color=#816AD0>{reportplayer.displayName}</color>\n" +
                            $"<size=12>Причина: {reason} </size>", this);
            }
        }

        #endregion

        #region Parent
        public static string mainskymenu = "MAIN_MENU_PLAYER";
        public static string ModerMenuSky = "MAIN_MENU_MODER";
        public static string PlayerAlert = "MAIN_MENU_alert";
        public static string ModerAlert = "MAIN_moder_alert";
        #endregion

        #region commands

        private void ReportMenu(BasePlayer player)
        {
            SkyReportPlayers(player);
        }

        [ConsoleCommand("reportplayer")]
        private void cmdreportplayer(ConsoleSystem.Arg args)
        {
            SkyReportPlayers(args.Player());
        }

        [ConsoleCommand("players")]
        private void cmdreportplayerConsole(ConsoleSystem.Arg args)
        {
            if (args.Args == null || args.Args.Length == 0 || args.Args.Length < 2)
            {
                PrintWarning($"Неверный синтаксис, используйте players check/uncheck StemId64");
                return;
            }

            BasePlayer player = BasePlayer.FindByID(ulong.Parse(args.Args[1]));
            if (player == null) { PrintWarning("Игрока нет на сервере!"); return; };

            switch (args.Args[0].ToLower())
            {
                case "check":
                    {
                        CheckPlayerModeration.Add(player.userID, player.userID);
                        PrintWarning($"Вы вызвали игрока {player.displayName} на проверку");
                        SendDiscordMsg("SKY_MODER_CHECK_PLAYER_GOING_CONSOLE", player.displayName, player.userID);
                        SendChatMessage("SKY_MODER_CHECK_PLAYER_GOING_CONSOLE", player.displayName, player.userID);


                        if (config.isafk.usecheckafk == true)
                        {
                            timer.Repeat(config.isafk.timecheckisafk, 3, () =>
                            {
                                if (!IsPlayerAfk(player))
                                {
                                    SendDiscordMsg("SKY_PLAYER_IS_AFK_VK_DISCORD_OK", player.displayName);

                                    SendChatMessage("SKY_PLAYER_IS_AFK_VK_DISCORD_OK", player.displayName);
                                    PrintWarning(String.Format(lang.GetMessage("SKY_PLAYER_IS_AFK_OK", this), player.displayName));
                                }
                                else
                                {
                                    SendDiscordMsg("SKY_PLAYER_IS_AFK_VK_DISCORD_NO", player.displayName);

                                    SendChatMessage("SKY_PLAYER_IS_AFK_VK_DISCORD_NO", player.displayName);
                                    PrintWarning(String.Format(lang.GetMessage("SKY_PLAYER_IS_AFK_NO", this), player.displayName));
                                }
                            });
                        }
                        AlertPlayerCheck(player, player);
                        break;
                    }
                case "uncheck":
                    {
                        if (CheckPlayerModeration.ContainsKey(player.userID))
                        {
                            CheckPlayerModeration.Remove(player.userID);

                            CuiHelper.DestroyUi(player, PlayerAlert);

                            PrintWarning(String.Format(lang.GetMessage("SKY_MODERATOR_STOPID_CHEKING", this), player.displayName));
                            SendDiscordMsg("SKY_CHECK_STOP_CONSOLE", config.setting.servername, player.displayName, player.userID);
                            SendChatMessage("SKY_CHECK_STOP_CONSOLE", config.setting.servername, player.displayName, player.userID);

                            ReportData[player.userID].ReportCount = 0;
                            ReportData[player.userID].CheckCount++;
                        }
                        else
                        {
                            PrintWarning("Этого игрока не вызывали на проверку!");
                        }
                        break;
                    }
            }

        }

        private void modermenu(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "SkyReportSystem.moderator") || player.IsAdmin)
            {
                SkyModerMenu(player);
            }
            else
            {
                SendChat(player, String.Format(lang.GetMessage("SKY_PLAYER_PERMISSION_IS_FOUND", this)));
            }
        }

        [ConsoleCommand("moderreport")]
        private void cmdreportmenu(ConsoleSystem.Arg args)
        {
            if (permission.UserHasPermission(args.Player().UserIDString, "SkyReportSystem.moderator") || args.Player().IsAdmin)
            {
                SkyModerMenu(args.Player());
            }
            else
            {
                SendChat(args.Player(), String.Format(lang.GetMessage("SKY_PLAYER_PERMISSION_IS_FOUND", this)));
            }
        }

        [ConsoleCommand("closeui")]
        void closeuimain(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            CuiHelper.DestroyUi(player, mainskymenu);
            CuiHelper.DestroyUi(player, "buttoncap");
            CuiHelper.DestroyUi(player, ModerMenuSky);
            CuiHelper.DestroyUi(player, PlayerAlert);
            CuiHelper.DestroyUi(player, ModerAlert);
        }

        [ChatCommand("discord")]
        void discordaccess(BasePlayer player, string cmd, string[] Args)
        {
            if (Args == null || Args.Length == 0)
            {
                SendChat(player, lang.GetMessage("SKY_PLAYER_NOTHING_MESSAGE_SKYPE_DISCORD", this));
                return;
            }

            if (CheckPlayerModeration.ContainsKey(player.userID))
            {
                string Discord = "";
                foreach (var arg in Args)
                {
                    Discord += " " + arg;
                }
                SendChatMessage("SKY_DISCORD_MESSAGE", config.setting.servername, player.displayName, player.UserIDString, Discord);
                SendDiscordMsg("SKY_DISCORD_MESSAGE", config.setting.servername, player.displayName, player.UserIDString, Discord);
                BasePlayer moderator = FindPlayer(CheckPlayerModeration[player.userID].ToString());
                if (player == moderator) { }
                else
                {
                    SendChat(moderator, String.Format(lang.GetMessage("SKY_DISCORD_MESSAGE", this), config.setting.servername, player.displayName, player.UserIDString, Discord));
                    SendChat(player, String.Format(lang.GetMessage("SKY_MODER_CHECK_P_DISCORD", this), Discord));
                }

            }
            else { SendChat(player, lang.GetMessage("SKY_SKYPE_DISCORD_ERROR", this)); }
        }

        [ConsoleCommand("openinfoplayer")]
        void consolego(ConsoleSystem.Arg args)
        {
            if (!args.HasArgs(2)) return;

            BasePlayer targetPlayer = BasePlayer.FindByID(ulong.Parse(args.Args[1]));

            BasePlayer player = args.Player();
            bool CanUse = permission.UserHasPermission(player.UserIDString, "SkyReportSystem.moderator") || player.IsAdmin;
            if (!CanUse) return;
            if (args.Args[0].ToLower() == "chooseplayers")
            {
                if (CanUse)
                {
                    playerinfomenu(player, args.Args[1], targetPlayer);
                }
                else
                {
                    SendChat(player, String.Format(lang.GetMessage("SKY_PLAYER_PERMISSION_IS_FOUND", this)));
                }
            }
            if (args.Args[0].ToLower() == "checkinplayer")
            {
                if (CheckPlayerModeration.ContainsKey(targetPlayer.userID))
                {
                    SendChat(player, String.Format(lang.GetMessage("SKY_NO_CHECK_MODERATION_OR_CHECKING", this), targetPlayer.displayName));
                }
                else
                {
                    if (CanUse)
                    {
                        CheckPlayerModeration.Add(targetPlayer.userID, args.Player().userID);
                        SendChat(player, String.Format(lang.GetMessage("SKY_ACESS_CHECKING", this), targetPlayer.displayName));
                        SendDiscordMsg("SKY_MODER_CHECK_PLAYER_GOING", player.displayName, targetPlayer.displayName, targetPlayer.userID);
                        SendChatMessage("SKY_MODER_CHECK_PLAYER_GOING", player.displayName, targetPlayer.displayName, targetPlayer.userID);


                        if (config.isafk.usecheckafk == true)
                        {
                            timer.Repeat(config.isafk.timecheckisafk, 3, () =>
                            {
                                if (!IsPlayerAfk(targetPlayer))
                                {
                                    SendDiscordMsg("SKY_PLAYER_IS_AFK_VK_DISCORD_OK", targetPlayer.displayName);

                                    SendChatMessage("SKY_PLAYER_IS_AFK_VK_DISCORD_OK", targetPlayer.displayName);
                                    SendChat(args.Player(), String.Format(lang.GetMessage("SKY_PLAYER_IS_AFK_OK", this), targetPlayer.displayName));
                                }
                                else
                                {
                                    SendDiscordMsg("SKY_PLAYER_IS_AFK_VK_DISCORD_NO", targetPlayer.displayName);

                                    SendChatMessage("SKY_PLAYER_IS_AFK_VK_DISCORD_NO", targetPlayer.displayName);
                                    SendChat(args.Player(), String.Format(lang.GetMessage("SKY_PLAYER_IS_AFK_NO", this), targetPlayer.displayName));
                                }
                            });
                        }
                        AlertPlayerCheck(BasePlayer.FindByID(targetPlayer.userID), player);
                        CuiHelper.DestroyUi(player, ModerMenuSky);
                    }
                    else
                    {
                        SendChat(player, String.Format(lang.GetMessage("SKY_PLAYER_PERMISSION_IS_FOUND", this)));
                    }
                }
            }

            if (args.Args[0] == "stopcheckingplayers")
            {
                if (CanUse)
                {
                    CheckPlayerModeration.Remove(targetPlayer.userID);

                    CuiHelper.DestroyUi(targetPlayer, PlayerAlert);
                    CuiHelper.DestroyUi(player, ModerMenuSky);
                    SendChat(player, String.Format(lang.GetMessage("SKY_MODERATOR_STOPID_CHEKING", this), targetPlayer.displayName));
                    SendChat(targetPlayer, String.Format(lang.GetMessage("SKY_PLAYER_STOPID_CHEKING", this), args.Player().displayName));
                    SendDiscordMsg("SKY_CHECK_STOP", config.setting.servername, args.Player().displayName, targetPlayer.displayName, targetPlayer.userID);
                    SendChatMessage("SKY_CHECK_STOP", config.setting.servername, args.Player().displayName, targetPlayer.displayName, targetPlayer.userID);

                    ReportData[targetPlayer.userID].ReportCount = 0;
                    ReportData[targetPlayer.userID].CheckCount++;
                }
                else
                {
                    SendChat(player, String.Format(lang.GetMessage("SKY_PLAYER_PERMISSION_IS_FOUND", this)));
                }
            }

            if (args.Args[0] == "moderatorcheckbanreason")
            {
                PlayerBanIPlayerModeration(player, BasePlayer.FindByID(targetPlayer.userID));
            }
        }

        [ConsoleCommand("Banplayers")]
        void ReportSystemBanReason(ConsoleSystem.Arg arg)
        {
            rust.RunClientCommand(arg.Player(), String.Format(config.banReasons[Convert.ToInt32(arg.Args[1])].BanReasonCommand, arg.Args[0]));
            CuiHelper.DestroyUi(arg.Player(), ModerMenuSky);
            BasePlayer player = BasePlayer.Find(arg.Args[0]);
            BasePlayer moderator = arg.Player();
            ReportData[player.userID].ReportCount = 0;
            string BanReason = $"{String.Format(config.banReasons[Convert.ToInt32(arg.Args[1])].BanReason, arg.Args[0])}";

            SendChatMessage("BAN_USER_VK_DISCORD", player.displayName, player.UserIDString, config.setting.servername, BanReason, moderator.displayName);
            SendDiscordMsg("BAN_USER_VK_DISCORD", player.displayName, player.UserIDString, config.setting.servername, BanReason, moderator.displayName);

            BanPlayer.Add(player.userID);

        }
        public List<ulong> BanPlayer = new List<ulong>();

        #region MetodsCooldown
        public Dictionary<ulong, int> CooldownPC = new Dictionary<ulong, int>();

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        void Metods_GiveCooldown(ulong ID, int cooldown)
        {
            CooldownPC[ID] = cooldown + (int)CurrentTime();
        }

        bool Metods_GetCooldown(ulong ID)
        {
            if (!CooldownPC.ContainsKey(ID) || Math.Max(0, CooldownPC[ID]) < 1 || CooldownPC[ID] <= (int)CurrentTime())
                return false;
            else return true;
        }

        #endregion

        [ConsoleCommand("SkyReportGo")]
        private void PlayerReportSystem(ConsoleSystem.Arg args)
        {
            string ReasonReport = "";
            if (args.Args.Length == 0)
            {
                SkyReportPlayers(args.Player());
                return;
            }
            if (args.Args[0] == "chooseplayers")
            {
                SkyReportPlayers(args.Player(), args.Args[1]);
                return;
            }
            if (args.Args[0].Length != 17)
            {
                for (int t = 0; t < 44; t++)
                {
                    CuiHelper.DestroyUi(args.Player(), "Button" + $".ChoosePlayer.{t}");
                    CuiHelper.DestroyUi(args.Player(), "Button" + $".ChoosePlayer.{t}.Text");
                }
                SkyReportPlayers(args.Player(), args.Args[0]);
                return;
            }
            if (args.Args.Length >= 2)
            {
                int ReasonIndex = int.Parse(args.Args[1]);
                ReasonReport = config.Reasonsforcomplaint[ReasonIndex];
                BasePlayer target = BasePlayer.Find(args.Args[0]);
                ReportActivity(args.Player(), target, ReasonReport);


                if (args.Player() != target)
                {
                    if (Metods_GetCooldown(args.Player().userID) == true)
                    {
                        SendChat(args.Player(), lang.GetMessage("SKY_COLDOWN", this));
                        return;
                    }
                    ReportData[target.userID].ReportCount++;


                    ReportData[target.userID].complaint.Insert(0, ReasonReport);
                    SendChat(args.Player(), String.Format(lang.GetMessage("SKY_REPORT_REPORTING", this), target.displayName));
                    SendDiscordMsg("SKY_REPORT_REPORTING_VK_DISCORD", config.setting.servername, args.Player().displayName, target.displayName, target.userID, ReasonReport);
                    SendChatMessage("SKY_REPORT_REPORTING_VK_DISCORD", config.setting.servername, args.Player().displayName, target.displayName, target.userID, ReasonReport);
                    Metods_GiveCooldown(args.Player().userID, config.setting.Cooldown);
                    if (ReportData[target.userID].ReportCount > config.setting.maxreportcall)
                    {
                        if (config.setting.usemodercall == true)
                        {
                            for (int u = 0; u < BasePlayer.activePlayerList.Count; u++)
                            {
                                BasePlayer player = BasePlayer.activePlayerList[u];
                                if (permission.UserHasPermission(player.UserIDString, "SkyReportSystem.moderator"))
                                {
                                    SendChat(player, String.Format(lang.GetMessage("SKY_REPORT_MAXIMUM_REPORT", this), target, ReportData[target.userID].ReportCount));
                                    ModerAlertCheck(player, target);
                                    timer.Once(5f, () => { CuiHelper.DestroyUi(player, ModerAlert); });
                                }
                            }
                        }
                        if (config.setting.uselog == true)
                        {
                            LogToFile("SkyReportSystemLOG", $"Игрок {target} превысил максимальное количество репортов! [{ReportData[target.userID].ReportCount}]", this);
                        }
                        return;
                    }

                }
                else
                {
                    SendChat(args.Player(), lang.GetMessage("SKY_ERROR_PLAYER", this));
                }
                return;
            }
        }
        #endregion

        #region cui

        private void PlayerBanIPlayerModeration(BasePlayer player, BasePlayer targetinfoban)
        {
            CuiHelper.DestroyUi(player, "BanMunuReason");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.168753 0.1509259", AnchorMax = "0.6302084 0.3185185" },
                Image = { FadeIn = 1f, Color = HexToCuiColor("#00000064") }
            }, "InfoPlayerSky", "BanMunuReason");

            #region BanReasonPanel


            for (int U = 0, x = 0, y = 0, i = 0; U < config.banReasons.Count; U++)
            {
                var reason = config.banReasons[U];
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0.005902235 + (x * 0.33)} {0.6906075 - (y * 0.31)}", AnchorMax = $"{0.3318241 + (x * 0.33)} {0.9613256 - (y * 0.31)}" },
                    Button = { Command = $"Banplayers {targetinfoban.userID} {i}", Color = HexToCuiColor("#000000B2") },
                    Text = { Text = reason.BanReason, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16 }
                }, "BanMunuReason");
                x++; i++;
                if (x == 3)
                {
                    x = 0;
                    y++;
                }

            }
            #endregion

            CuiHelper.AddUi(player, container);
        }

        private void ModerAlertCheck(BasePlayer player, BasePlayer target)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, ModerAlert);

            container.Add(new CuiPanel
            {
                FadeOut = 0.1f,
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "1 -150", OffsetMax = "230 -60" },
                Image = { Color = "0 0 0 0", FadeIn = 0.3f }
            }, "Overlay", ModerAlert);

            container.Add(new CuiElement
            {
                Parent = ModerAlert,
                Name = "AlerModer",
                FadeOut = 0.1f,
                Components =
                    {
                        new CuiImageComponent {  Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.3", Sprite = "assets/content/ui/ui.background.transparent.radial.psd", FadeIn = 0.3f  },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
            });

            container.Add(new CuiLabel
            {
                FadeOut = 0.1f,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = $"Игрок: {target.displayName}\nПревысил максимальное количевство жалоб! ", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "AlerModer");

            CuiHelper.AddUi(player, container);

        }

        private void AlertPlayerCheck(BasePlayer player, BasePlayer moderinformation)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PlayerAlert);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-200 150", OffsetMax = "180 300" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", PlayerAlert);

            container.Add(new CuiElement
            {
                Parent = PlayerAlert,
                Name = "MainAlert",
                Components =
                    {
                        new CuiImageComponent {  Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.5", Sprite = "assets/content/ui/ui.background.transparent.radial.psd"  },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.7644448", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = "Вас вызвали на проверку", FontSize = 19, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "MainAlert");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.6311106", AnchorMax = "1 0.7422219", OffsetMax = "0 0.764444" },
                Text = { Text = "Вы обязаны предоставить Skype или Discord!", FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "MainAlert");

            if (player.userID == moderinformation.userID)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.0684208 0.05777803", AnchorMax = "0.9087719 0.6266667", OffsetMax = "0 0" },
                    Text = { Text = $"Kоманды :\n<color=orange>/skype</color> \n<color=orange>/discord</color>\nВызвавший модератор : НЕИЗВЕСТНО", FontSize = 17, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, "MainAlert");
            }
            else
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.0684208 0.057778234", AnchorMax = "0.9087719 0.6266667", OffsetMax = "0 0" },
                    Text = { Text = $"Kоманды :\n<color=orange>/skype</color> \n<color=orange>/discord</color>\nВызвавший модератор : {moderinformation.displayName}", FontSize = 17, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, "MainAlert");
            }


            CuiHelper.AddUi(player, container);
        }

        private void SkyModerMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, ModerMenuSky);
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", ModerMenuSky);

            container.Add(new CuiElement
            {
                Parent = ModerMenuSky,
                Components =
                    {
                        new CuiImageComponent {   Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat",  Color = HexToCuiColor("#00000069")   },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Close = ModerMenuSky, Color = "0 0 0 0" },
                Text = { Text = "" }
            }, ModerMenuSky);

            #region title
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2796874 0.9157416", AnchorMax = "0.7052073 0.968505" },
                Image = { Color = HexToCuiColor("#00000064") }
            }, ModerMenuSky, "TitlePanel");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = "Меню Модератора!", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "TitlePanel");

            #endregion

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2302083 0.2972222", AnchorMax = "0.7557291 0.8268524" },
                Image = { Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Color = HexToCuiColor("#423A3898") }
            }, ModerMenuSky, "MainPanelModer");

            container.Add(new CuiElement
            {
                Name = "playerlisd",
                Parent = "MainPanelModer",
                Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
            });

            #region PlayerList

            for (int x = 0, y = 0, i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                var mplayer = BasePlayer.activePlayerList[i];
                if (mplayer == null || !ReportData.ContainsKey(mplayer.userID) || mplayer.userID == player.userID) continue;
                if (ReportData[mplayer.userID].ReportCount >= config.setting.maxreportcall)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"{0.01333263 + (x * 0.245)} {0.9033457 - (y * 0.090)}", AnchorMax = $"{0.252307 + (x * 0.245)} {0.9758366 - (y * 0.090)}" },
                        Button = { Command = $"openinfoplayer chooseplayers {mplayer.userID}", Color = HexToCuiColor("#000000B2") },
                        Text = { Text = $"({mplayer.displayName})" + " [" + ReportData[mplayer.userID].ReportCount + "]", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16 }
                    }, "playerlisd", "playerlisd" + $".Player.{i}.Text");

                    x++;
                    if (x == 4)
                    {
                        x = 0;
                        y++;

                        if (y == 11)
                        {
                            break;
                        }
                    }
                }
            }
            #endregion

            CuiHelper.AddUi(player, container);
        }

        private void playerinfomenu(BasePlayer player, string target, BasePlayer targetinfocheck)
        {
            #region SupportMetods
            string ImageAvatar = GetImage(target, 0);
            #endregion
            BasePlayer targets = BasePlayer.FindByID(ulong.Parse(target));
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "MainPanelModer");


            container.Add(new CuiPanel
            {
                FadeOut = 0.5f,
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.5f, Color = "0 0 0 0" },
            }, ModerMenuSky, "InfoPlayerSky");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Close = ModerMenuSky, Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "InfoPlayerSky");



            container.Add(new CuiElement
            {
                Parent = "InfoPlayerSky",
                Components = {
                    new CuiRawImageComponent {
                        Png = ImageAvatar,
                        Url = null ,
                        Sprite = "assets/content/textures/generic/fulltransparent.tga"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.16875 0.5842593",
                        AnchorMax = "0.3151042 0.8796296"
                    },
                }
            });


            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3229166 0.8203703", AnchorMax = "0.5739583 0.8740742" },
                Text = { Text = $"{targets.displayName} ({targets.UserIDString})", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "InfoPlayerSky");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3203125 0.7574074", AnchorMax = "0.5020834 0.8037037" },
                Text = { Text = $"Количевство репортов: {ReportData[targetinfocheck.userID].ReportCount}", FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "InfoPlayerSky");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3203126 0.7101846", AnchorMax = "0.4828126 0.7564811" },
                Text = { Text = $"Количевство проверок: {ReportData[targetinfocheck.userID].CheckCount}", FontSize = 15, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "InfoPlayerSky");

            container.Add(new CuiPanel
            {
                FadeOut = 0.5f,
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.6703124 0.462963", AnchorMax = "0.8505208 0.8925924" },
                Image = { FadeIn = 0.5f, Color = HexToCuiColor("#0000006E"), },
            }, "InfoPlayerSky", "captain");

            container.Add(new CuiElement
            {
                Parent = "captain",
                Components =
                    {
                        new CuiImageComponent { Color = HexToCuiColor("#FFFFFFFF") },
                        new CuiRectTransformComponent { AnchorMin = "0 0.886463", AnchorMax = "1 0.8886465" }
                    }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.03468214 0.8995634", AnchorMax = "0.9624277 0.9825329" },
                Text = { Text = "Последнии жалобы", FontSize = 16, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "captain");

            int i = 0;
            for (int j = 0; j < ReportData[targets.userID].complaint.Count; j++)
            {
                var captainlist = ReportData[targets.userID].complaint[j];

                if (i <= 7)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "captain",
                        Name = "listcap",
                        Components =
                        {
                            new CuiImageComponent { Color = HexToCuiColor("#FF7575BB"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                            new CuiRectTransformComponent { AnchorMin =  $"0.0231214 {0.7758622 - (i * 0.107)}", AnchorMax = $"0.9739881 {0.8668124 - (i * 0.107)}" }
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Text = { Text = captainlist, FontSize = 17, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                    }, "listcap");
                    i++;
                }
                else
                {
                    break;
                }
            }

            if (!CheckPlayerModeration.ContainsKey(targetinfocheck.userID))
            {
                CuiHelper.DestroyUi(player, "StopPlayerBtn");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.16875 0.5018519", AnchorMax = "0.315625 0.5722228" },
                    Button = { Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Color = HexToCuiColor("#33302F98"), Command = $"openinfoplayer checkinplayer {targetinfocheck.userID}" },
                    Text = { Text = "Bызвать на проверку", FontSize = 14, Align = TextAnchor.MiddleCenter }
                }, "InfoPlayerSky", "CheckPlayerBtn");
            }
            else if (CheckPlayerModeration.ContainsKey(targetinfocheck.userID) && CheckPlayerModeration.ContainsValue(player.userID) || player.IsAdmin)
            {
                CuiHelper.DestroyUi(player, "CheckPlayerBtn");
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.1686849 0.427109", AnchorMax = "0.315625 0.4972244" },
                    Button = { Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Color = HexToCuiColor("#33302F98"), Command = $"openinfoplayer stopcheckingplayers {targetinfocheck.userID}" },
                    Text = { Text = "Закончить проверку", FontSize = 14, Align = TextAnchor.MiddleCenter }
                }, "InfoPlayerSky", "StopPlayerBtn");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.1686849 0.351852", AnchorMax = "0.3156251 0.4224281" },
                    Button = { Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Color = HexToCuiColor("#33302F98"), Command = $"openinfoplayer moderatorcheckbanreason {targetinfocheck.userID}" },
                    Text = { Text = "Выдать блокировку", FontSize = 14, Align = TextAnchor.MiddleCenter }
                }, "InfoPlayerSky", "GoBanned");
            }
            else
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.325 0.6064815", AnchorMax = "0.6682292 0.6990741", OffsetMax = "0 0" },
                    Text = { Text = String.Format(lang.GetMessage("SKY_PLAYER_CHECKING_MODER_MENU_MODERATION", this)), FontSize = 20, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, "InfoPlayerSky");
            }

            CuiHelper.AddUi(player, container);
        }

        private void SkyReportPlayers(BasePlayer player, string target = "", string reason = "")
        {
            CuiElementContainer container = new CuiElementContainer();

            if (target == "" && reason == "")
            {

                CuiHelper.DestroyUi(player, mainskymenu);

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Image = { Color = "0 0 0 0" }
                }, "Hud", mainskymenu);

                container.Add(new CuiElement
                {
                    Parent = mainskymenu,
                    Components =
                    {
                        new CuiImageComponent { Color = HexToCuiColor("#000000B1") },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                    Button = { Close = mainskymenu, Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, mainskymenu);


                #region title
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.2796874 0.9157416", AnchorMax = "0.7052073 0.968505" },
                    Image = { Color = HexToCuiColor("#00000064") }
                }, mainskymenu, "TitlePanel");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = config.setting.teatletxt, FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, "TitlePanel");

                #endregion

                #region infotxt

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.2322917 0.8472127", AnchorMax = "0.7520834 0.9027551" },
                    Text = { Text = lang.GetMessage("SKY_Menu_Info", this), FontSize = 17, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, mainskymenu);

                #endregion

                #region input

                container.Add(new CuiElement
                {
                    Parent = mainskymenu,
                    Name = mainskymenu + ".Input",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToCuiColor("#423a38")},
                        new CuiRectTransformComponent { AnchorMin = "0.2385417 0.7777631", AnchorMax = "0.7458333 0.821279" },
                        new CuiOutlineComponent{Distance = "1.2 1.2", Color = HexToCuiColor("#FFFFFFFF"), UseGraphicAlpha = false}

                    }
                });

                container.Add(new CuiElement
                {
                    Parent = mainskymenu + ".Input",
                    Name = mainskymenu + ".Input.Current",
                    Components =
                    {
                        new CuiInputFieldComponent { FontSize = 16, Align = TextAnchor.MiddleCenter, Command = "SkyReportGo ", Text = "dsfsadf", Color = HexToCuiColor("#AFF9FFFF")},
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }
                });
                #endregion

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.238542 0.2740741", AnchorMax = "0.7463545 0.7722222" },
                    Image = { Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Color = HexToCuiColor("#423A3898") }
                }, mainskymenu, "playerslist");
            }

            if (target.Length != 17 && reason == "")
            {

                for (int i = 0; i < 44; i++)
                {
                    CuiHelper.DestroyUi(player, mainskymenu + $".ChoosePlayer.{i}");
                    CuiHelper.DestroyUi(player, mainskymenu + $".ChoosePlayer.{i}.Text");
                }

                for (int x = 0, y = 0, i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {

                    var check = BasePlayer.activePlayerList[i];
                    if (check.displayName.ToLower().Contains(target.ToLower()))
                    {
                        string command = $"SkyReportGo chooseplayers {check.userID}";

                        container.Add(new CuiButton
                        {
                            FadeOut = 0.1f,
                            RectTransform = { AnchorMin = $"{0.01333263 + (x * 0.244)} {0.9033457 - (y * 0.089)}", AnchorMax = $"{0.252307 + (x * 0.244)} {0.9758366 - (y * 0.089)}" },
                            Button = { Command = command, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Color = HexToCuiColor("#939391C8"), FadeIn = 0.4f },
                            Text = { Text = $"[{check.displayName}]", FontSize = 14, Align = TextAnchor.MiddleCenter },
                        }, "playerslist", "Button" + $".ChoosePlayer.{i}");

                        x++;
                        if (x == 4)
                        {
                            x = 0;
                            y++;

                            if (y == 11)
                            {
                                break;
                            }
                        }

                    }

                }
            }
            else
            {

                CuiHelper.DestroyUi(player, "buttoncap");
                CuiHelper.DestroyUi(player, "infotxt2");

                if (reason == "")
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0.2385417 0.1064812", AnchorMax = "0.74583544 0.2185182" },
                        Image = { Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Color = HexToCuiColor("#423A3898") }
                    }, mainskymenu, "buttoncap");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.2333325 0.2203703", AnchorMax = "0.75 0.2648149" },
                        Text = { Text = "Далее вам нужно выбрать причину жалобы на данного игрока, или ввести свою", FontSize = 17, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                    }, mainskymenu, "infotxt2");

                    for (int x = 0, y = 0, i = 0, t = 0; i < config.Reasonsforcomplaint.Count; i++)
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"{0.009240265 + (x * 0.245)} {0.5289283 - (y * 0.45)}", AnchorMax = $"{0.2505133 + (x * 0.245)} {0.9173553 - (y * 0.45)}" },
                            Button = { Command = $"SkyReportGo {target.Replace(" ", "").Replace(" ", "").Replace(" ", "")} {i}", Color = HexToCuiColor("#939391C8"), Close = "buttoncap" + "infotxt2", Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.4f },
                            Text = { Text = config.Reasonsforcomplaint[i], Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16 }
                        }, "buttoncap");

                        x++;
                        if (x == 4)
                        {
                            x = 0;
                            y++;
                        }
                        if (y == 2)
                        {
                            break;
                        }
                        t++;
                    }
                }
            }
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Help


        #region FindPlayer
        private BasePlayer FindPlayer(string nameOrId)
        {
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                var check = BasePlayer.activePlayerList[i];
                if (check.displayName.ToLower().Contains(nameOrId.ToLower()) || check.userID.ToString() == nameOrId)
                    return check;
            }
            return null;
        }

        #endregion

        #region IsAfk

        readonly Hash<ulong, Vector3> lastPosition = new Hash<ulong, Vector3>();
        public bool IsPlayerAfk(BasePlayer player)
        {
            if (player == null) return true;
            var last = lastPosition[player.userID];
            var current = player.transform.position;

            if (last.x.Equals(current.x)) return true;
            lastPosition[player.userID] = current;

            return false;
        }
        #endregion

        #region Hex
        private static string HexToCuiColor(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException(" Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        #endregion

        #region reply
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, config.setting.prefix, config.setting.avatarid.ToString());
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region VK
        private void SendChatMessage(string msg, params object[] args)
        {
            if (!config.vKontakte.UseVK) return;

            string vkchat = string.Format(lang.GetMessage(msg, this), args);
            int RandomID = UnityEngine.Random.Range(0, 99999);
            while (vkchat.Contains("#"))
                vkchat = vkchat.Replace("#", "%23");
            webrequest.Enqueue($"https://api.vk.com/method/messages.send?chat_id={config.vKontakte.VK_ChatID}&random_id={RandomID}&message={vkchat}&access_token={config.vKontakte.VK_Token}&v=5.90", null, (code, response) => { }, this);
        }

        #endregion

        #region discord

        #region FancyDiscord
        public class FancyMessage
        {
            public string content { get; set; }
            public bool tts { get; set; }
            public Embeds[] embeds { get; set; }

            public class Embeds
            {
                public string title { get; set; }
                public int color { get; set; }
                public List<Fields> fields { get; set; }
                public Footer footer { get; set; }
                public Authors author { get; set; }

                public Embeds(string title, int color, List<Fields> fields, Authors author, Footer footer)
                {
                    this.title = title;
                    this.color = color;
                    this.fields = fields;
                    this.author = author;
                    this.footer = footer;

                }
            }

            public FancyMessage(string content, bool tts, Embeds[] embeds)
            {
                this.content = content;
                this.tts = tts;
                this.embeds = embeds;
            }

            public string toJSON() => JsonConvert.SerializeObject(this);
        }

        public class Footer
        {
            public string text { get; set; }
            public string icon_url { get; set; }
            public string proxy_icon_url { get; set; }
            public Footer(string text, string icon_url, string proxy_icon_url)
            {
                this.text = text;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }

        public class Authors
        {
            public string name { get; set; }
            public string url { get; set; }
            public string icon_url { get; set; }
            public string proxy_icon_url { get; set; }
            public Authors(string name, string url, string icon_url, string proxy_icon_url)
            {
                this.name = name;
                this.url = url;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }

        public class Fields
        {
            public string name { get; set; }
            public string value { get; set; }
            public bool inline { get; set; }
            public Fields(string name, string value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }

        private void Request(string url, string payload, Action<int> callback = null)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header.Add("Content-Type", "application/json");
            webrequest.Enqueue(url, payload, (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    if (response != null)
                    {
                        try
                        {
                            JObject json = JObject.Parse(response);
                            if (code == 429)
                            {
                                float seconds = float.Parse(Math.Ceiling((double)(int)json["retry_after"] / 1000).ToString());
                            }
                            else
                            {
                                PrintWarning($" Discord rejected that payload! Responded with \"{json["message"].ToString()}\" Code: {code}");
                            }
                        }
                        catch
                        {
                            PrintWarning($"Failed to get a valid response from discord! Error: \"{response}\" Code: {code}");
                        }
                    }
                    else
                    {
                        PrintWarning($"Discord didn't respond (down?) Code: {code}");
                    }
                }
                try
                {
                    callback?.Invoke(code);
                }
                catch (Exception ex) { }

            }, this, RequestMethod.POST, header);
        }
        #endregion

        void SendDiscordMsg(string key, params object[] args)
        {
            if (!config.discord.UseDiscord) return;
            if (String.IsNullOrEmpty(config.discord.Discord_webHook)) return;

            string msg = string.Format(lang.GetMessage(key, this), args);

            List<Fields> fields = new List<Fields>
            {
                    new Fields("SkyReportSystem", msg, true),
            };

            FancyMessage newMessage = new FancyMessage(null, true, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 16775936, fields, new Authors("SkyReportSystem", "https://skyplugins.ru/", "https://i.imgur.com/ILk3uJc.png", null), new Footer("Author: DezLife", "https://i.imgur.com/ILk3uJc.png", null)) });
            Request($"{config.discord.Discord_webHook}", newMessage.toJSON());
        }

        #endregion

        #endregion

        #region DataWorkerModule

        private void LoadData<T>(string name, ref T data, bool enableSaving)
        {
            string resultName = this.Name + $"/{name}";

            if (Interface.Oxide.DataFileSystem.ExistsDatafile(resultName))
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<T>(resultName);
            }
            else
            {
                Interface.Oxide.DataFileSystem.WriteObject(resultName, data);
            }

            if (enableSaving)
            {
                SaveData(name, data);
            }
        }

        private void SaveData<T>(string name, T data)
        {
            string resultName = this.Name + $"/{name}";

            Interface.Oxide.DataFileSystem.WriteObject(resultName, data);
        }

        #endregion
    }
}
