using System;
using System.Collections.Generic;
using ConVar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("IQPersonal", "Mercury", "1.0.0")]
    [Description("Властвуй по пионерски. Система контроля персонала")]
    class IQPersonal : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin IQChat;
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.MoreSetting;
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar, Chat.CustomHex);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region Vars
        public static string PermissionUse = "iqpersonal.use";
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Основная настройка плагина")]
            public ControllerSettings ControllerSetting = new ControllerSettings();
            [JsonProperty("Настройка дневной нормы для персонала")]
            public Dictionary<string, ControllerDay> Settings = new Dictionary<string, ControllerDay>();
            [JsonProperty("Использовать логирование в беседу ВК")]
            public bool UseLogged;
            [JsonProperty("Использовать логирование в Discord канал")]
            public bool UseDiscordLogged;
            [JsonProperty("Настройка ВК для логирования в беседу(Если включено)")]
            public VKInformation VKSetting = new VKInformation();
            [JsonProperty("Настройка Discord для логирования в канал(Если включено)")]
            public DiscordInformation DiscordSetting = new DiscordInformation();
            [JsonProperty("Настройка магазинов")]
            public StoreSettings StoreSetting = new StoreSettings();
            [JsonProperty("Дополнительные настройки")]
            public MoreSettings MoreSetting = new MoreSettings();
            internal class ControllerDay
            {
                [JsonProperty("Учитывать время в суточной норме")]
                public bool DayTimeUse;
                [JsonProperty("Суточная норма отыгранного времени на сервере[секунды]")]
                public int DayTime;
                [JsonProperty("Учитывать муты в суточной норме(IQChat)")]
                public bool DayMuteUse;
                [JsonProperty("Суточная норма выданных мутов(IQChat)")]
                public int DayMute;
                [JsonProperty("Учитывать проверки в суточной норме(IQReportSystem)")]
                public bool DayCheckUse;
                [JsonProperty("Суточная норма проверок(IQReportSystem)")]
                public int DayCheck;
            }
            internal class ControllerSettings
            {
                [JsonProperty("Учитывать время")]
                public bool ControllerTime;
                [JsonProperty("Учитывать муты(IQChat)")]
                public bool ControllerMute;
                [JsonProperty("Учитывать проверки(IQReportSystem)")]
                public bool ControllerCheck;
                [JsonProperty("Учитывать блокировки(IQReportSystem)")]
                public bool ControllerBans;
                [JsonProperty("Учитывать ругань персонала(IQChat)")]
                public bool ControllerBadWords;
                [JsonProperty("Сколько очков репутации снимать за 1 запрещенное слово(IQChat)")]
                public int ScoreRemoveBadWords;
                [JsonProperty("Сколько очков репутации нужно для вывода 1 рубля")]
                public int ScoreChanged;
                [JsonProperty("Включить суточную норму")]
                public bool ControllerDays;
                [JsonProperty("Снимать репутацию в случае не выполнения суточной нормы")]
                public bool UseControllerDaysFailed;
                [JsonProperty("Включить автоматическое снятие,при низкой репутации")]
                public bool ControllerAutoKick;
                [JsonProperty("При каком отрицательном показателе репутации снимать персонал")]
                public int AutoKickMinimalReputation;
                [JsonProperty("Сколько очков репутации начислять за выполнение дневной нормы")]
                public int ScoreAddDayNormal;
                [JsonProperty("Сколько репутации начислять за мут")]
                public int ScoreMute;
                [JsonProperty("Сколько репутации начислять за проверку игрока")]
                public int ScoreCheck;
                [JsonProperty("Сколько репутации начислять за блокировку игрока")]
                public int ScoreBans;
                [JsonProperty("Сколько репутации снимать за не выполнение суточной нормы")]
                public int ScoreFailedControllerDays;
            }
            internal class VKInformation
            {
                [JsonProperty("Токен от группы ВК(От группы будут идти сообщения в беседу.Вам нужно добавить свою группу в беседу!)")]
                public string VKToken;
                [JsonProperty("ID беседы для отправки логов(Отчет о времени,статистика)")]
                public string VKChatID;
                [JsonProperty("ID беседы с персоналом(если имеется. Туда будет приходить информация о добавлении персонала и удалять тех,кого кикнули(автоматически))")]
                public string VKChatIDPersonal;
            }
            internal class DiscordInformation
            {
                [JsonProperty("Webhooks для дискорда")]
                public string WebHook;
            }
            internal class MoreSettings
            {
                [JsonProperty("Префикс для чата(IQChat)")]
                public string CustomPrefix;
                [JsonProperty("Аватарка для чата(IQChat)")]
                public string CustomAvatar;
                [JsonProperty("Цвет для сообщения в чате(IQChat)")]
                public string CustomHex;
                [JsonProperty("Выдавать дополнительные права,при добавлении в персонал(при удалении будут сниматься)")]
                public bool GiveMorePermission;
                [JsonProperty("Лист дополительных привилегий(Если включено)(Выдадутся все сразу,при добавлении в персонал.При удалении снимутся)(Пример : iqchat.vip)")]
                public List<string> Permissions = new List<string>();
            }
            internal class StoreSettings
            {
                [JsonProperty("Использовать магазин MoscowOVH")]
                public bool MoscowOVH;
                [JsonProperty("Использовать магазин GameStores")]
                public bool GameStores;
                [JsonProperty("Настройки для магазина GameStores")]
                public GameStoresInformation InfoGameStores = new GameStoresInformation();
                internal class GameStoresInformation
                {
                    [JsonProperty("API Магазина(GameStores)")]
                    public string GameStoresAPIStore;
                    [JsonProperty("ID Магазина(GameStores)")]
                    public string GameStoresIDStore;
                    [JsonProperty("Сообщение в магазин при выдаче баланса(GameStores)")]
                    public string GameStoresMessage;
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    UseLogged = true,
                    UseDiscordLogged = true,
                    ControllerSetting = new ControllerSettings
                    {
                        ControllerBadWords = true,
                        ControllerBans = true,
                        ControllerCheck = true,
                        ControllerDays = true,
                        ControllerMute = true,
                        ControllerTime = true,
                        UseControllerDaysFailed = true,
                        ScoreChanged = 5,
                        ScoreAddDayNormal = 10,
                        ScoreRemoveBadWords = 1,
                        ScoreBans = 5,
                        ScoreCheck = 3,
                        ControllerAutoKick = true,
                        AutoKickMinimalReputation = -100,
                        ScoreMute = 1,
                        ScoreFailedControllerDays = 5,
                    },
                    Settings = new Dictionary<string, ControllerDay>
                    {
                        ["iqpersonal.helper"] = new ControllerDay
                        {
                            DayTimeUse = true,
                            DayCheckUse = false,
                            DayMuteUse = true,
                            DayMute = 10,
                            DayCheck = 0,
                            DayTime = 500
                        },
                        ["iqpersonal.moderator"] = new ControllerDay
                        {
                            DayTimeUse = true,
                            DayCheckUse = true,
                            DayMuteUse = true,
                            DayMute = 10,
                            DayCheck = 5,
                            DayTime = 1000
                        },
                    },
                    StoreSetting = new StoreSettings
                    {
                        GameStores = false,
                        MoscowOVH = true,
                        InfoGameStores = new StoreSettings.GameStoresInformation
                        {
                            GameStoresAPIStore = "",
                            GameStoresIDStore = "",
                            GameStoresMessage = "Вам зачислен баланс! Хорошего дня"
                        }
                    },
                    MoreSetting = new MoreSettings
                    {
                        CustomAvatar = "",
                        CustomHex = "",
                        CustomPrefix = "",
                        GiveMorePermission = false,
                        Permissions = new List<string>
                        {
                            "iqchat.personal",
                            "iqchat.vip",
                        }
                    },
                    VKSetting = new VKInformation
                    {
                        VKChatID = "",
                        VKToken = "",
                        VKChatIDPersonal = ""
                    },
                    DiscordSetting = new DiscordInformation
                    {
                        WebHook = ""
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #87" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию! #45");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        [JsonProperty("Информация о персонале")] public Dictionary<ulong, InformationUser> DataInformation = new Dictionary<ulong, InformationUser>();
        [JsonProperty("Хранение времени")] public int TimeDay;
        public class InformationUser
        {
            [JsonProperty("Ник")]
            public string DisplayName;
            [JsonProperty("Ссылка на ВК")]
            public string VKLink;
            [JsonProperty("Дневное отыгранное время на сервере")]
            public int DayTime;
            [JsonProperty("Дневное количество выданных мутов")]
            public int DayMute;
            [JsonProperty("Дневное количество проверок")]
            public int DayCheck;
            [JsonProperty("Дневное количество выданных блокировок")]
            public int DayBans;
            [JsonProperty("Очки репутации игрока")]
            public int ScorePlayer;
            [JsonProperty("Статус дневной нормы(true - выполнил/false - нет)")]
            public bool StatusDayNormal;
        }
        void ReadData() {
            DataInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, InformationUser>>("IQPersonal/Information");
            TimeDay = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<int>("IQPersonal/Time");
        }
        void WriteData() => timer.Every(60f, () => {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQPersonal/Information", DataInformation);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQPersonal/Time", TimeDay);
        });
        void RegisteredDataUser(ulong player, string VKLink = "")
        {
            if (!DataInformation.ContainsKey(player))
                DataInformation.Add(player, new InformationUser
                {
                    DisplayName = covalence.Players.FindPlayerById(player.ToString()).Name,
                    StatusDayNormal = false,
                    DayBans = 0,
                    DayCheck = 0,
                    DayMute = 0,
                    DayTime = 0,
                    ScorePlayer = 0,
                    VKLink = VKLink,
                });
        }

        #endregion

        #region Metods

        void NormalDaySet()
        {
            if (IsDayNormal())
            {
                DayNormalCheck();
                timer.Once(10f, () =>
                {
                    TimeDay = Convert.ToInt32(CurrentTime() + 86400);
                    PrintWarning("Начался отсчетный день для персонала");
                    VKSendMessage(lang.GetMessage("VK_SET_TIME_DAY", this), config.VKSetting.VKChatIDPersonal);
                    DiscordSendMessage(lang.GetMessage("VK_SET_TIME_DAY", this));
                    foreach (var p in BasePlayer.activePlayerList)
                        if (permission.UserHasPermission(p.UserIDString, PermissionUse))
                            SendChat(p, lang.GetMessage("CHAT_START_TIME_DAY", this));
                });
            }
        }
        void DayNormalCheck()
        {
            if (!config.ControllerSetting.ControllerDays) return;
            PrintWarning("Отчетный день персонала закончился! Персоналу в онлайне сейчас будут отосланы уведомления!");
            foreach (var Data in DataInformation)
            {
                var User = Data.Value;
                var Controller = config.Settings;
                foreach (var UserDayly in Controller)
                {
                    var Day = UserDayly.Value;
                    int Check = Day.DayCheckUse ? User.DayCheck : 0;
                    int Mute = Day.DayMuteUse ? User.DayMute : 0;
                    int Time = Day.DayTimeUse ? User.DayTime : 0;
                    if(Check >= Day.DayCheck 
                    && Mute >= Day.DayMute
                    && Time >= Day.DayTime)
                        User.StatusDayNormal = true;
                }
                DayNormalFinish(Data.Key);
            }
            VKSendMessage(lang.GetMessage("VK_FINISH_TIME_DAY", this), config.VKSetting.VKChatIDPersonal);
            DiscordSendMessage(lang.GetMessage("VK_FINISH_TIME_DAY", this));
        }

        void DayNormalFinish(ulong UserID)
        {
            var User = DataInformation[UserID];
            var Controller = config.ControllerSetting;

            string StatusNormal = Controller.ControllerDays ? IsPersonalNormalCompleted(UserID) ? lang.GetMessage("STATUS_COMPLETED", this) : lang.GetMessage("STATUS_NO_COMPLETED", this) : lang.GetMessage("STATISTICK_TURN_OFF", this);
            string TimeGame = Controller.ControllerTime ? TimeSpan.FromSeconds((double)User.DayTime).ToShortString() : lang.GetMessage("STATISTICK_TURN_OFF", this); 
            string Reputation = User.ScorePlayer.ToString();
            string CountBans = Controller.ControllerBans ? IsPersonal(UserID) ? User.DayBans.ToString() : lang.GetMessage("STATISTICK_NULL", this) : lang.GetMessage("STATISTICK_TURN_OFF", this);
            string CountCheck = Controller.ControllerCheck ? IsPersonal(UserID) ? User.DayCheck.ToString() : lang.GetMessage("STATISTICK_NULL", this) : lang.GetMessage("STATISTICK_TURN_OFF", this);
            string CountMute = Controller.ControllerMute ? IsPersonal(UserID) ? User.DayMute.ToString() : lang.GetMessage("STATISTICK_NULL", this) : lang.GetMessage("STATISTICK_TURN_OFF", this);

            string MessageStatistick = String.Format(lang.GetMessage("VK_FINISH_TIME_DAY_STATISTICK_USER", this), User.DisplayName, TimeGame, CountBans, CountCheck, CountMute, Reputation, StatusNormal);
            string MessageChat = IsPersonalNormalCompleted(UserID) ? String.Format(lang.GetMessage("CHAT_FINISH_TIME_DAY_COMPLETED", this), Controller.ScoreAddDayNormal) : lang.GetMessage("CHAT_FINISH_TIME_DAY_NO_COMPLETED", this);
            VKSendMessage(MessageStatistick);
            DiscordSendMessage(MessageStatistick);

            if (Controller.ControllerAutoKick)
                if (User.ScorePlayer <= Controller.AutoKickMinimalReputation)
                    RemovePersonal(UserID);

            User.DayBans = 0;
            User.DayCheck = 0;
            User.DayMute = 0;
            User.DayTime = 0;
            User.StatusDayNormal = false;
            if (IsPersonalNormalCompleted(UserID))
                User.ScorePlayer += Controller.ScoreAddDayNormal;
            else
            {
                if (Controller.UseControllerDaysFailed)
                    User.ScorePlayer -= Controller.ScoreFailedControllerDays;
            }

            var player = BasePlayer.FindByID(UserID);
            if (player != null) 
                SendChat(player, MessageChat);
        }

        void TrackerTime(BasePlayer player)
        {
            if (!config.ControllerSetting.ControllerTime) return;
            if (!HasPermission(player.userID, PermissionUse)) return;
            if (!DataInformation.ContainsKey(player.userID)) return;
            ServerStatistics.Storage playerStatistics = ServerStatistics.Get(player.userID);
            int TimeGame = playerStatistics.Get("time");
            var User = DataInformation[player.userID];
            User.DayTime += TimeGame;
        }
        void BalanceSet(ulong UserID)
        {
            var Store = config.StoreSetting;
            var Balance = ConvertReputation(UserID);
            var User = DataInformation[UserID];

            if (Store.MoscowOVH)
            {
                plugins.Find("RustStore").CallHook("APIChangeUserBalance", UserID, Balance, new Action<string>((result) =>
                {
                    if (result == "SUCCESS")
                    {
                        VKSendMessage(String.Format(lang.GetMessage("VK_GIVE_BALANCE", this), User.DisplayName, Balance));
                        DiscordSendMessage(String.Format(lang.GetMessage("VK_GIVE_BALANCE", this), User.DisplayName, Balance), UserID);
                        return;
                    }
                    VKSendMessage(String.Format(lang.GetMessage("VK_NO_AUTH_STORE", this), User.DisplayName));
                    DiscordSendMessage(String.Format(lang.GetMessage("VK_NO_AUTH_STORE", this), User.DisplayName), UserID);
                }));
            }
            if(Store.GameStores)
            {
                if(String.IsNullOrEmpty(Store.InfoGameStores.GameStoresAPIStore) || String.IsNullOrEmpty(Store.InfoGameStores.GameStoresIDStore))
                {
                    PrintWarning("Ошибка #267 : Магазин не настроен! Он будет работать неккоректно!");
                    return;
                }
                webrequest.Enqueue($"https://gamestores.ru/api?shop_id={Store.InfoGameStores.GameStoresIDStore}&secret={Store.InfoGameStores.GameStoresAPIStore}&action=moneys&type=plus&steam_id={UserID}&amount={Balance}&mess={Store.InfoGameStores.GameStoresMessage}", null, (i, s) =>
                {
                    if (i != 200) { }
                    if (s.Contains("success"))
                    {
                        VKSendMessage(String.Format(lang.GetMessage("VK_GIVE_BALANCE", this), User.DisplayName, Balance));
                        DiscordSendMessage(String.Format(lang.GetMessage("VK_GIVE_BALANCE", this), User.DisplayName, Balance), UserID);
                        return;
                    }
                    if (s.Contains("fail"))
                    {
                        VKSendMessage(String.Format(lang.GetMessage("VK_NO_AUTH_STORE", this), User.DisplayName));
                        DiscordSendMessage(String.Format(lang.GetMessage("VK_NO_AUTH_STORE", this), User.DisplayName), UserID);
                    }
                }, this);
            }
            User.ScorePlayer = 0;
        }

        void AddPersonal(ulong userID, string VKLink)
        {
            var More = config.MoreSetting;
            var VK = config.VKSetting;
            string ReplaceLink = VKLink.Replace("https://vk.com/", "").Replace("vk.com/", "");
            string URL = $"https://api.vk.com/method/users.get?user_ids={ReplaceLink}&access_token={VK.VKToken}&v=5.88";

            if (DataInformation.ContainsKey(userID))
            {
                PrintWarning(lang.GetMessage("IQP_ADD_PLAYER_CONTAINS", this));
                return;
            }

            if (!HasPermission(userID, PermissionUse))
                permission.GrantUserPermission(userID.ToString(), PermissionUse, this);

            if (More.GiveMorePermission)
                foreach (var PermList in More.Permissions)
                    if (!HasPermission(userID, PermList))
                        permission.GrantUserPermission(userID.ToString(), PermList, this);


            webrequest.Enqueue(URL, null, (code, response) =>
            {
                var json = JObject.Parse(response);
                string ID = (string)json["response"]?[0]?["id"];
                string FirstName = (string)json["response"]?[0]?["first_name"];
                string LastName = (string)json["response"]?[0]?["last_name"];
                string Information = $"{FirstName} {LastName}";

                RegisteredDataUser(userID, ID);
                VKSendMessage(String.Format(lang.GetMessage("VK_ADD_PERSONAL", this), Information, userID, VKLink), VK.VKChatIDPersonal);
                DiscordSendMessage(String.Format(lang.GetMessage("VK_ADD_PERSONAL", this), Information, userID, VKLink), userID);
                PrintWarning(lang.GetMessage("IQP_ADD_PLAYER_ACCESS", this));
            }, this);
        }

        void RemovePersonal(ulong userID)
        {
            var More = config.MoreSetting;
            var VK = config.VKSetting;
            string URL = $"https://api.vk.com/method/users.get?user_ids={DataInformation[userID].VKLink}&access_token={VK.VKToken}&v=5.88";

            if (!DataInformation.ContainsKey(userID))
            {
                PrintWarning(lang.GetMessage("IQP_REMOVE_PLAYER_CONTAINS", this));
                return;
            }

            if (More.GiveMorePermission)
                foreach (var PermList in More.Permissions)
                    if (HasPermission(userID, PermList))
                        permission.GrantUserPermission(userID.ToString(), PermList, this);

            if (HasPermission(userID, PermissionUse))
                permission.RevokeUserPermission(userID.ToString(), PermissionUse);

            webrequest.Enqueue(URL, null, (code, response) =>
            {
                var json = JObject.Parse(response);
                string ID = (string)json["response"]?[0]?["id"];
                string FirstName = (string)json["response"]?[0]?["first_name"];
                string LastName = (string)json["response"]?[0]?["last_name"];
                string Information = $"{FirstName} {LastName}";

                string URLKick = $"https://api.vk.com/method/messages.removeChatUser?chat_id={VK.VKChatID}&member_id={ID}&access_token={VK.VKToken}&v=5.88";
                webrequest.Enqueue(URLKick, null, (code2, response2) =>
                {
                    if(code2 == 935)
                    {
                        PrintWarning(lang.GetMessage("VK_ERRORS_935",this));
                        return;
                    }
                    if (code2 == 936)
                    {
                        PrintWarning(lang.GetMessage("VK_ERRORS_936", this));
                        return;
                    }
                    if (code2 == 945)
                    {
                        PrintWarning(lang.GetMessage("VK_ERRORS_945", this));
                        return;
                    }
                    VKSendMessage(String.Format(lang.GetMessage("VK_REMOVE_PERSONAL", this), Information, userID), VK.VKChatIDPersonal);
                    DiscordSendMessage(String.Format(lang.GetMessage("VK_REMOVE_PERSONAL", this), Information, userID), userID);
                    PrintWarning(lang.GetMessage("IQP_REMOVE_PLAYER_ACCESS", this));
                    DataInformation.Remove(userID);
                }, this);
            }, this);
        }

        void ChangePersonalVK(ulong userID, string VKLink)
        {
            if (!DataInformation.ContainsKey(userID))
            {
                PrintWarning(lang.GetMessage("IQP_REMOVE_PLAYER_CONTAINS", this));
                return;
            }

            var VK = config.VKSetting;
            string ReplaceLink = VKLink.Replace("https://vk.com/", "").Replace("vk.com/", ""); 
            var User = DataInformation[userID];
            string URL = $"https://api.vk.com/method/users.get?user_ids={User.VKLink}&access_token={VK.VKToken}&v=5.88";                 

            webrequest.Enqueue(URL, null, (code, response) =>
            {
                var json = JObject.Parse(response);
                string ID = (string)json["response"]?[0]?["id"];
                string FirstName = (string)json["response"]?[0]?["first_name"];
                string LastName = (string)json["response"]?[0]?["last_name"];
                string Information = $"{FirstName} {LastName}";

                User.VKLink = ReplaceLink;

                VKSendMessage(String.Format(lang.GetMessage("VK_CHANGE_VK", this), Information, VKLink), VK.VKChatIDPersonal);
                DiscordSendMessage(String.Format(lang.GetMessage("VK_CHANGE_VK", this), Information, VKLink), userID);
                PrintWarning(lang.GetMessage("IQP_CHANGE_ACCESS", this));
            }, this);
        }

        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            RegisteredPermission();
            ReadData();
            foreach (var p in BasePlayer.activePlayerList)
                OnPlayerConnected(p);
            WriteData();

        }
        void OnServerSave()
        {
            NormalDaySet();
        }
        void Unload()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQPersonal/Information", DataInformation);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQPersonal/Time", TimeDay);
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (IsPersonal(player.userID))
            {
                VKSendMessage(String.Format(lang.GetMessage("VK_CONNECTED", this), player.displayName, DateTime.UtcNow.ToUniversalTime()));
                DiscordSendMessage(String.Format(lang.GetMessage("VK_CONNECTED", this), player.displayName, DateTime.UtcNow.ToUniversalTime()),player.userID);
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            TrackerTime(player);
            if (IsPersonal(player.userID))
            {
                VKSendMessage(String.Format(lang.GetMessage("VK_DISCONNECTED", this), player.displayName, DateTime.UtcNow.ToUniversalTime()));
                DiscordSendMessage(String.Format(lang.GetMessage("VK_DISCONNECTED", this), player.displayName, DateTime.UtcNow.ToUniversalTime()), player.userID);
            }
        }
        void OnNewSave(string filename)
        {
            foreach (var Data in DataInformation)
                BalanceSet(Data.Key);
        }
        #endregion

        #region Command

        [ChatCommand("rep")]
        void ChatCommandRep(BasePlayer player)
        {
            if (!DataInformation.ContainsKey(player.userID)) return;
            var User = DataInformation[player.userID];
            SendChat(player, String.Format(lang.GetMessage("CHAT_COMMAND_REP", this, player.UserIDString), User.ScorePlayer));
        }

        [ConsoleCommand("iqp")]
        void ConsoleCommandIQP(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length < 1 || arg == null || arg.Args == null)
            {
                PrintWarning(lang.GetMessage("CHAT_NOT_ARG", this));
                return;
            }

            switch (arg.Args[0])
            {
                case "add":
                    {
                        if(arg.Args.Length != 3)
                        {
                            PrintWarning("Используйте синтаксис : iqp add STEAM64ID VKLink");
                            return;
                        }
                        ulong userID = ulong.Parse(arg.Args[1]);       
                        if(!userID.IsSteamId())
                        {
                            PrintWarning("Введите корректный Steam64ID");
                            return;
                        }
                        string VKLink = arg.Args[2];
                        if(String.IsNullOrWhiteSpace(VKLink))
                        {
                            PrintWarning("Вы не указали ссылку на страницу ВК");
                            return;
                        }
                        AddPersonal(userID, VKLink);
                        return;
                    }
                case "remove":
                    {
                        if (arg.Args.Length != 2)
                        {
                            PrintWarning("Используйте синтаксис : iqp remove STEAM64ID");
                            return;
                        }
                        ulong userID = ulong.Parse(arg.Args[1]);
                        if (!userID.IsSteamId())
                        {
                            PrintWarning("Введите корректный Steam64ID");
                            return;
                        }
                        RemovePersonal(userID);
                        return;
                    }
                case "vk":
                    {
                        if (arg.Args.Length != 3)
                        {
                            PrintWarning("Используйте синтаксис : iqp vk STEAM64ID VKLink");
                            return;
                        }
                        ulong userID = ulong.Parse(arg.Args[1]);
                        if (!userID.IsSteamId())
                        {
                            PrintWarning("Введите корректный Steam64ID");
                            return;
                        }
                        string VKLink = arg.Args[2];
                        if (String.IsNullOrWhiteSpace(VKLink))
                        {
                            PrintWarning("Вы не указали ссылку на страницу ВК");
                            return;
                        }
                        ChangePersonalVK(userID, VKLink);
                        return;
                    }
                case "debug":
                    {
                        VKSendMessage("Инициализация..#1");
                        VKSendMessage("Инициализация..#2",config.VKSetting.VKChatIDPersonal);
                        DiscordSendMessage("Test");
                        return;
                    }
            }
        }

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["VK_ERRORS_935"] = "Contact not found",
                ["VK_ERRORS_936"] = "NotCorrected settings",
                ["VK_ERRORS_945"] = "Chat was disabled",

                ["VK_SET_TIME_DAY"] = "[IQPersonal] A new staff reporting day has started!",
                ["VK_FINISH_TIME_DAY"] = "[IQPersonal] The reporting day ended successfully!",
                ["VK_CONNECTED"] = "[IQPersonal]{0} connected server : {1}",
                ["VK_DISCONNECTED"] = "[IQPersonal]{0} disconnect server : {1}",
                ["VK_ADD_PERSONAL"] = "[IQPersonal]User {0} added personal\nInformation : \nSteam64ID - {1}\nVK Link - {2}",
                ["VK_CHANGE_VK"] = "[IQPersonal]User {0} changed VK\nVK Link : {1}",
                ["VK_REMOVE_PERSONAL"] = "[IQPersonal]User {0} remove personal\nReason : Minimum reputation",
                ["VK_GIVE_BALANCE"] = "[IQPersonal] The report on the accrual balance!\nUser {0} was credited {1} rubles!\nThe indicators of reputation reset",
                ["VK_BAD_REPUTATION"] = "[IQPersonal] The report on the accrual balance!\nnUser {0} a bad reputation has been recorded {1} \nThe indicators of reputation reset",
                ["VK_NO_AUTH_STORE"] = "[IQPersonal] The report on the accrual balance!\nUser {0} is not authorized in the store! \n Reputation Indicators are reset without crediting the balance",
                ["VK_FINISH_TIME_DAY_STATISTICK_USER"] =
                "[IQPersonal]" +
                "\nUser : {0}" +
                "\nGeneral statistics :" +
                "\nTime spent on the server - {1}" +
                "\nCount Bans - {2}" +
                "\nCount Checks - {3}" +
                "\nCount Muted - {4}" +
                "\nReputation - {5}" +
                "\nDay Normal Completed - {6}",

                ["CHAT_FINISH_TIME_DAY_COMPLETED"] = "The reporting day is over! You have coped with the norm for today\nYour reward : +<color=#01afad>{0}</color> reputation",
                ["CHAT_FINISH_TIME_DAY_NO_COMPLETED"] = "The reporting day is over! Alas, you did not cope with the norm for today\n Next time try better!",
                ["CHAT_START_TIME_DAY"] = "Reporting has begun! We wish you good luck with your tests!",
                ["CHAT_REPUTATION_BAN"] = "You have successfully banned the player.Your reputation increased by +<color=#01afad>{0}</color> \nView your reputation - <color=#01afad>/rep</color>",
                ["CHAT_REPUTATION_CHECK"] = "You have successfully verified the player.Your reputation has increased by +<color=#01afad>{0}</color>\nView your reputation - <color=#01afad>/rep</color>",
                ["CHAT_REPUTATION_MUTE"] = "You have successfully blocked the player's chat.Your reputation has increased by +<color=#01afad>{0}</color>\nView your reputation - <color=#01afad>/rep</color>",
                ["CHAT_REPUTATION_BAD_WORDS"] = "You used a forbidden word(mate)!Your reputation has decreased by -<color=#01afad>{0}</color>\nView your reputation - <color=#01afad>/rep</color>",
                ["CHAT_COMMAND_REP"] = "Your reputation : <color=#01afad>{0}</color>",
                ["CHAT_NOT_ARG"] = "Enter a valid value",
                ["CHAT_DELETE_PERSONAL"] = "User {0} was filmed\nReason : Great negative reputation recorded",

                ["IQP_ADD_PLAYER_CONTAINS"] = "User contains personal",
                ["IQP_ADD_PLAYER_ACCESS"] = "Your access add personal",
                ["IQP_REMOVE_PLAYER_CONTAINS"] = "User no contains personal",
                ["IQP_REMOVE_PLAYER_ACCESS"] = "Your acces remove personal",
                ["IQP_CHANGE_ACCESS"] = "You have successfully changed VK user",

                ["STATUS_COMPLETED"] = "Done",
                ["STATUS_NO_COMPLETED"] = "Not executed",
                ["STATISTICK_NULL"] = "Not included in the duties",
                ["STATISTICK_TURN_OFF"] = "OFF",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["VK_ERRORS_935"] = "Такого пользователя в чате нет.",
                ["VK_ERRORS_936"] = "Неверно настроена беседа",
                ["VK_ERRORS_945"] = "Беседа отключена",

                ["VK_SET_TIME_DAY"] = "[IQPersonal] Новый отчётный день персонала начался!",
                ["VK_GIVE_BALANCE"] = "[IQPersonal] Отчет по начислению баланса!\nПользователю {0} было зачислено {1} рублей! \nПоказатели репутации сброшены",
                ["VK_BAD_REPUTATION"] = "[IQPersonal] Отчет по начислению баланса!\nУ пользователя {0} зафиксирована плохая репутация {1} \nПоказатели репутации сброшены",
                ["VK_NO_AUTH_STORE"] = "[IQPersonal] Отчет по начислению баланса!\nПользовател {0} не авторизован в магазине! \nПоказатели репутации сброшены без зачисления баланса",
                ["VK_CONNECTED"] = "[IQPersonal]{0} зашел на сервер : {1}",
                ["VK_DISCONNECTED"] = "[IQPersonal]{0} вышел с сервера : {1}",
                ["VK_ADD_PERSONAL"] = "[IQPersonal]Пользователь {0} добавлен в состав\nИнформация : \nSteam64ID - {1}\nСсылка на VK - {2}",
                ["VK_CHANGE_VK"] = "[IQPersonal]Пользователю {0} был изменен вк\nСсылка на ВК : {1}",
                ["VK_REMOVE_PERSONAL"] = "[IQPersonal]Пользователь {0} был снят\nПричина : Зафиксирована большая отрицательная репутация",
                ["VK_FINISH_TIME_DAY"] = "[IQPersonal] Отчетный день успешно закончился!",
                ["VK_FINISH_TIME_DAY_STATISTICK_USER"] =
                "[IQPersonal]" +
                "\nПользователь : {0}" +
                "\nОбщая статистика :" +
                "\nВремя проведенное на сервере - {1}" +
                "\nКоличество блокировок - {2}" +
                "\nКоличество проверок - {3}" +
                "\nКоличество мутов - {4}" +
                "\nКоличество репутации - {5}" +
                "\nДневная норма - {6}",

                ["CHAT_FINISH_TIME_DAY_COMPLETED"] = "Отчетный день завершен! Вы справились с положенной нормой на сегодня\nВаша награда : +<color=#01afad>{0}</color> репутации",
                ["CHAT_FINISH_TIME_DAY_NO_COMPLETED"] = "Отчетный день завершен! Увы,вы не справились с положенной нормой на сегодня\nВ следующий раз постарайтесь лучше!",
                ["CHAT_START_TIME_DAY"] = "Отчетный начался! Желаем вам удачи при проверках!",
                ["CHAT_REPUTATION_BAN"] = "Вы успешно забанили игрока.Ваша репутация увеличилась на +<color=#01afad>{0}</color>\nПросмотреть вашу репутацию - <color=#01afad>/rep</color>",
                ["CHAT_REPUTATION_CHECK"] = "Вы успешно проверили игрока.Ваша репутация увеличилась на +<color=#01afad>{0}</color>\nПросмотреть вашу репутацию - <color=#01afad>/rep</color>",
                ["CHAT_REPUTATION_MUTE"] = "Вы успешно заблокировали чат игрока.Ваша репутация увеличилась на +<color=#01afad>{0}</color>\nПросмотреть вашу репутацию - <color=#01afad>/rep</color>",
                ["CHAT_REPUTATION_BAD_WORDS"] = "Вы использовали запрещенное слово(мат)!Ваша репутация уменьшилась на -<color=#01afad>{0}</color>\nПросмотреть вашу репутацию - <color=#01afad>/rep</color>",
                ["CHAT_COMMAND_REP"] = "Ваша репутация : <color=#01afad>{0}</color>",
                ["CHAT_NOT_ARG"] = "Введите корректное значение",

                ["IQP_ADD_PLAYER_CONTAINS"] = "Такой пользователь уже состоит в персонале",
                ["IQP_ADD_PLAYER_ACCESS"] = "Вы успешно добавили нового пользователя",
                ["IQP_REMOVE_PLAYER_CONTAINS"] = "Такой пользователь не состоит в персонале",
                ["IQP_REMOVE_PLAYER_ACCESS"] = "Вы успешно удалили пользователя",
                ["IQP_CHANGE_ACCESS"] = "Вы успешно изменили ВК пользователю",

                ["STATUS_COMPLETED"] = "Выполнена",
                ["STATUS_NO_COMPLETED"] = "Не выполнена",
                ["STATISTICK_NULL"] = "Не входит в обязанности",
                ["STATISTICK_TURN_OFF"] = "Выключено",
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion

        #region Help

        void RegisteredPermission()
        {
            permission.RegisterPermission(PermissionUse, this);
            
            foreach(var PermConfig in config.Settings)
                permission.RegisterPermission(PermConfig.Key, this);
            PrintWarning("Регистарция прав прошла успешно");
        }
        void VKSendMessage(string Message, string CustomChatID = "")
        {
            if (!config.UseLogged) return;
            var VK = config.VKSetting;
            var ChatID = string.IsNullOrEmpty(CustomChatID) ? VK.VKChatID : CustomChatID;
            if (String.IsNullOrEmpty(VK.VKChatID) || String.IsNullOrEmpty(VK.VKToken))
            {
                PrintWarning("Ошибка #34267: Вы не настроили конфигурацию,в пункте с ВК"); 
                return;
            }
            int RandomID = UnityEngine.Random.Range(0, 9999);
            while (Message.Contains("#"))
                Message = Message.Replace("#", "%23");

            webrequest.Enqueue($"https://api.vk.com/method/messages.send?chat_id={ChatID}&random_id={RandomID}&message={Message}&access_token={VK.VKToken}&v=5.90", null, (code, response) => {
            }, this);
        }

        void DiscordSendMessage(string key, ulong userID = 0, params object[] args)
        {
            if (!config.UseDiscordLogged) return;
            if (String.IsNullOrEmpty(config.DiscordSetting.WebHook)) return;
            var VK = config.VKSetting;
            string VKLinks = DataInformation.ContainsKey(userID) ? DataInformation[userID].VKLink : "583299692";
            string Token = String.IsNullOrEmpty(VK.VKToken) ? "e56e41544c6eff561a9faacbfbcb5a8f3c4d412092511de232fee6d2290c9de7f8610cb98b3e099cf167a" : VK.VKToken;
            string URL = $"https://api.vk.com/method/users.get?user_ids={VKLinks}&fields=photo_50&access_token={Token}&v=5.88";

            webrequest.Enqueue(URL, null, (code, response) =>
            {
                var json = JObject.Parse(response);
                string ID = (string)json["response"]?[0]?["id"];
                string FirstName = (string)json["response"]?[0]?["first_name"];
                string LastName = (string)json["response"]?[0]?["last_name"];
                string PhotoLink = (string)json["response"]?[0]?["photo_50"];
                string Information = $"{FirstName} {LastName}";
                string VKLink = $"https://vk.com/id{ID}";

                List <Fields> fields = new List<Fields>
                {
                    new Fields("IQPersonal", key, true),
                };

                FancyMessage newMessage = new FancyMessage(null, true, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 635133, fields, new Authors(Information, VKLink, PhotoLink, null), new Footer("Author: Mercury[vk.com/mir_inc]", "https://i.imgur.com/ILk3uJc.png", null)) });
                Request(config.DiscordSetting.WebHook, newMessage.toJSON());
            }, this);
        }

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

        int ConvertReputation(ulong UserID)
        {
            var Controller = config.ControllerSetting;
            int Reputation = DataInformation[UserID].ScorePlayer;
            if (Reputation <= 0)
            {
                VKSendMessage(String.Format(lang.GetMessage("VK_BAD_REPUTATION", this), UserID, Reputation));
                DiscordSendMessage(String.Format(lang.GetMessage("VK_BAD_REPUTATION", this), UserID, Reputation), UserID);
                return 0;
            }
            double Money = Math.Round((double)(Reputation / Controller.ScoreChanged));
            return Convert.ToInt32(Money);
        }
        bool IsDayNormal()
        {
            if (TimeDay <= CurrentTime())
                return true;
            else return false; 
        }
        bool IsPersonalNormalCompleted(ulong UserID)
        {
            var DataPersonal = DataInformation[UserID];
            if (DataPersonal.StatusDayNormal)
                return true;
            else return false;
        }
        bool IsPersonal(ulong UserID)
        {
            var PeronalInformation = config.Settings;
            foreach (var Info in PeronalInformation)
            {
                if (!HasPermission(UserID, Info.Key)) continue;
                else return true;
            }
            return false;
        }
        bool HasPermission(ulong UserID, string Permissions)
        {
            if (permission.UserHasPermission(UserID.ToString(), Permissions))
                return true;
            else return false;
        }

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        #endregion

        #region API
        void API_SET_BANS(ulong UserID)
        {
            var Controller = config.ControllerSetting;
            if (Controller.ControllerBans)
            {
                if (!DataInformation.ContainsKey(UserID))
                    DataInformation.Add(UserID, new InformationUser { });

                var User = DataInformation[UserID];
                User.DayBans++;
                User.ScorePlayer += Controller.ScoreBans;
                var player = BasePlayer.FindByID(UserID);
                if (player == null) return;
                SendChat(player, String.Format(lang.GetMessage("CHAT_REPUTATION_BAN", this), Controller.ScoreBans));
            }
        }
        void API_SET_CHECK(ulong UserID)
        {
            var Controller = config.ControllerSetting;
            if (Controller.ControllerCheck)
            {
                if (!DataInformation.ContainsKey(UserID))
                    DataInformation.Add(UserID, new InformationUser { });

                var User = DataInformation[UserID];
                User.DayCheck++;
                User.ScorePlayer += Controller.ScoreCheck;
                var player = BasePlayer.FindByID(UserID);
                if (player == null) return;
                SendChat(player, String.Format(lang.GetMessage("CHAT_REPUTATION_CHECK", this), Controller.ScoreCheck));
            }
        }
        void API_SET_MUTE(ulong UserID)
        {
            var Controller = config.ControllerSetting;
            if (Controller.ControllerMute)
            {
                if (!DataInformation.ContainsKey(UserID))
                    DataInformation.Add(UserID, new InformationUser { });

                var User = DataInformation[UserID];
                User.DayMute++;
                User.ScorePlayer += Controller.ScoreMute;
                var player = BasePlayer.FindByID(UserID);
                if (player == null) return;
                SendChat(player, String.Format(lang.GetMessage("CHAT_REPUTATION_MUTE", this), Controller.ScoreMute));
            }
        }
        void API_DETECTED_BAD_WORDS(ulong UserID)
        {
            var Controller = config.ControllerSetting;
            if (Controller.ControllerBadWords)
            {
                if (!DataInformation.ContainsKey(UserID))
                    DataInformation.Add(UserID, new InformationUser { });

                var User = DataInformation[UserID];
                User.ScorePlayer -= Controller.ScoreRemoveBadWords;
                var player = BasePlayer.FindByID(UserID);
                if (player == null) return;
                SendChat(player, String.Format(lang.GetMessage("CHAT_REPUTATION_BAD_WORDS", this), Controller.ScoreRemoveBadWords));
            }
        }
        void API_SET_SCORE(ulong UserID, int Amount) => DataInformation[UserID].ScorePlayer += Amount;
        void API_REMOVE_SCORE(ulong UserID, int Amount) => DataInformation[UserID].ScorePlayer -= Amount;
        int API_GET_BANS(ulong UserID) { return DataInformation[UserID].DayBans; }
        int API_GET_CHECK(ulong UserID) { return DataInformation[UserID].DayCheck; }
        int API_GET_MUTE(ulong UserID) { return DataInformation[UserID].DayMute; }
        int API_GET_SCORE(ulong UserID) { return DataInformation[UserID].ScorePlayer; }

        #endregion
    }
}
