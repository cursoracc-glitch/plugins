using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompanionServer;
using ConVar;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQChat", "Mercury", "0.3.4")]
    [Description("Самый приятный чат для вашего сервера из ветки IQ")]
    class IQChat : RustPlugin
    {
        /// <summary>
        /// Обновление 0.3.4
        /// - Добавлена поддержка IQRankSystem
        /// - Добавлен дополнительный префикс - Ранг совместно с плагином IQRankSystem
        /// - Добавлено отображение времени отыгранного на сервере - совместно с плагином IQRankSystem
        /// - Добавлен пункт в меню чата "Ранг" с поддержкой IQRankSystem
        /// - Добавлена сортировка по доступным рангам IQRankSystem
        /// - Добавлен пункт Включения/Отключения случайных сообщений при подключении игрока
        /// - Добавлен список случайных сообщений при подключении игрока, так-же с поддержкой его страны
        /// - Добавлен пункт Включения/Отключения случайных сообщений при отключении игрока
        /// - Добавлен список случайных сообщений при отключении игрока, так-же с поддержкой его причины 
        /// - Значительно оптимизировал форматирование сообщений
        /// </summary>


        #region Reference
        [PluginReference] Plugin IQPersonal, IQFakeActive, XDNotifications, IQRankSystem;

        #region IQPersonal
        public void SetMute(BasePlayer player) => IQPersonal?.CallHook("API_SET_MUTE", player.userID);
        public void BadWords(BasePlayer player) => IQPersonal?.CallHook("API_DETECTED_BAD_WORDS", player.userID);
        #endregion

        #region XDNotifications
        private void AddNotify(BasePlayer player, string title, string description, string command = "", string cmdyes = "", string cmdno = "")
        {
            if (!XDNotifications) return;
            var Setting = config.ReferenceSetting.XDNotificationsSettings;
            Interface.Oxide.CallHook("AddNotify", player, title, description, Setting.Color, Setting.AlertDelete, Setting.SoundEffect, command, cmdyes, cmdno);
        }
        #endregion

        #region IQFakeActive
        public bool IsFake(string DisplayName) => (bool)IQFakeActive?.Call("IsFake", DisplayName);
        void SyncReservedFinish()
        {
            if (!config.ReferenceSetting.IQFakeActiveSettings.UseIQFakeActive) return;
            PrintWarning("IQChat - успешно синхронизирована с IQFakeActive");
            PrintWarning("=============SYNC==================");
        }
        #endregion

        #region IQRankSystem
        string IQRankGetRank(ulong userID) => (string)(IQRankSystem?.Call("API_GET_RANK_NAME", userID));
        string IQRankGetTimeGame(ulong userID) => (string)(IQRankSystem?.Call("API_GET_TIME_GAME", userID));
        List<string> IQRankListKey(ulong userID) => (List<string>)(IQRankSystem?.Call("API_RANK_USER_KEYS", userID));
        string IQRankGetNameRankKey(string Key) => (string)(IQRankSystem?.Call("API_GET_RANK_NAME", Key));
        void IQRankSetRank(ulong userID, string RankKey) => IQRankSystem?.Call("API_SET_ACTIVE_RANK", userID, RankKey);
        bool IQRankUserAcces(ulong userID, string RankKey) => (bool)IQRankSystem?.Call("API_GET_RANK_ACCESS", userID, RankKey);

        #endregion

        #endregion

        #region Vars
        public Dictionary<BasePlayer, BasePlayer> PMHistory = new Dictionary<BasePlayer, BasePlayer>();

        public string PermMuteMenu = "iqchat.muteuse";
        class Response
        {
            [JsonProperty("country")]
            public string Country { get; set; }
        }
        public static StringBuilder sb = new StringBuilder();
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Права для смены ника")]
            public string RenamePermission;
            [JsonProperty("Настройка префиксов")]
            public List<AdvancedFuncion> PrefixList = new List<AdvancedFuncion>();
            [JsonProperty("Настройка цветов для ников")]
            public List<AdvancedFuncion> NickColorList = new List<AdvancedFuncion>();
            [JsonProperty("Настройка цветов для сообщений")]
            public List<AdvancedFuncion> MessageColorList = new List<AdvancedFuncion>();
            [JsonProperty("Настройка сообщений в чате")]
            public MessageSettings MessageSetting;
            [JsonProperty("Настройка причин блокировок чата")]
            public List<ReasonMuteChat> ReasonListChat = new List<ReasonMuteChat>();
            [JsonProperty("Настройка интерфейса")]
            public InterfaceSettings InterfaceSetting;
            [JsonProperty("Настройка оповещения")]
            public AlertSetting AlertSettings;         
            [JsonProperty("Настройка привилегий")]
            public AutoSetups AutoSetupSetting;
            [JsonProperty("Настройка Rust+")]
            public RustPlus RustPlusSettings;
            [JsonProperty("Дополнительная настройка")]
            public OtherSettings OtherSetting;
            [JsonProperty("Настройка автоответчика")]
            public AnswerMessage AnswerMessages = new AnswerMessage();

            [JsonProperty("Настройка плагинов поддержки")]
            public ReferenceSettings ReferenceSetting = new ReferenceSettings();
            internal class AdvancedFuncion
            {
                [JsonProperty("Права")]
                public string Permissions;
                [JsonProperty("Значение")]
                public string Argument;
            }
            internal class AnswerMessage
            {
                [JsonProperty("Включить автоответчик?(true - да/false - нет)")]
                public bool UseAnswer;
                [JsonProperty("Настройка сообщений [Ключевое слово] = Ответ")]
                public Dictionary<string, string> AnswerMessageList = new Dictionary<string, string>();
            }
            internal class RustPlus
            {
                [JsonProperty("Использовать Rust+")]
                public bool UseRustPlus;
                [JsonProperty("Название для уведомления Rust+")]
                public string DisplayNameAlert;
            }
            internal class ReasonMuteChat
            {
                [JsonProperty("Причина мута")]
                public string Reason;
                [JsonProperty("Время мута")]
                public int TimeMute;
            }
            internal class ReferenceSettings
            {
                [JsonProperty("Настройка XDNotifications")]
                public XDNotifications XDNotificationsSettings = new XDNotifications();
                [JsonProperty("Настройка IQFakeActive")]
                public IQFakeActive IQFakeActiveSettings = new IQFakeActive();
                [JsonProperty("Настройка IQRankSystem")]
                public IQRankSystem IQRankSystems = new IQRankSystem();
                internal class XDNotifications
                {
                    [JsonProperty("Включить поддержку XDNotifications(UI уведомления будут заменены на уведомление с XDNotifications)")]
                    public bool UseXDNotifications;
                    [JsonProperty("Цвет заднего фона уведомления(HEX)")]
                    public string Color;
                    [JsonProperty("Через сколько удалиться уведомление")]
                    public int AlertDelete;
                    [JsonProperty("Звуковой эффект")]
                    public string SoundEffect;
                }
                internal class IQRankSystem
                {
                    [JsonProperty("Использовать поддержку рангов")]
                    public bool UseRankSystem;
                    [JsonProperty("Отображать игрокам их отыгранное время рядом с рангом")]
                    public bool UseTimeStandart;
                }
                internal class IQFakeActive
                {
                    [JsonProperty("Использовать поддержку IQFakeActive")]
                    public bool UseIQFakeActive;
                }
            }
            internal class AutoSetups
            {
                [JsonProperty("Настройки сброса привилегий")]
                public ReturnDefault ReturnDefaultSetting = new ReturnDefault();
                [JsonProperty("Автоматической установки префиксов/цвета ника/цвета чата")]
                public SetupAuto SetupAutoSetting = new SetupAuto();
                internal class ReturnDefault
                {
                    [JsonProperty("Сбрасывать автоматически префикс при окончании его прав")]
                    public bool UseDropPrefix;
                    [JsonProperty("Сбрасывать автоматически цвет ника при окончании его прав")]
                    public bool UseDropColorNick;
                    [JsonProperty("Сбрасывать автоматически цвет чата при окончании его прав")]
                    public bool UseDropColorChat;

                    [JsonProperty("При окончании префикса, установится данный префикс")]
                    public string PrefixDefault;
                    [JsonProperty("При окончании цвета ника, установится данный цвет")]
                    public string NickDefault;
                    [JsonProperty("При окончании цвета сообщения, установится данный цвета")]
                    public string MessageDefault;
                }
                internal class SetupAuto
                {
                    [JsonProperty("Устанавливать автоматически префикс при получении его прав")]
                    public bool UseSetupAutoPrefix;
                    [JsonProperty("Устанавливать автоматически цвет ника при получении его прав")]
                    public bool UseSetupAutoColorNick;
                    [JsonProperty("Устанавливать автоматически цвет чата при получении его прав")]
                    public bool UseSetupAutoColorChat;

                }
            }
            internal class MessageSettings
            {
                [JsonProperty("Включить форматирование сообщений")]
                public bool FormatingMessage;
                [JsonProperty("Включить личные сообщения")]
                public bool PMActivate;
                [JsonProperty("Включить игнор ЛС игрокам(/ignore nick)")]
                public bool IgnoreUsePM;
                [JsonProperty("Включить Анти-Спам")]
                public bool AntiSpamActivate;
                [JsonProperty("Скрыть из чата выдачу предметов Админу")]
                public bool HideAdminGave;
                [JsonProperty("Использовать список запрещенных слов?")]
                public bool UseBadWords;
                [JsonProperty("Включить возможность использовать несколько префиксов сразу")]
                public bool MultiPrefix;
                [JsonProperty("Переносить мут в командный чат(В случае мута,игрок не сможет писать даже в командный чат)")]
                public bool MuteTeamChat;
                [JsonProperty("Пермишенс для иммунитета к антиспаму")]
                public string PermAdminImmunitetAntispam;
                [JsonProperty("Наименование оповещения в чат")]
                public string BroadcastTitle;
                [JsonProperty("Цвет сообщения оповещения в чат")]
                public string BroadcastColor;
                [JsonProperty("На какое сообщение заменять плохие слова")]
                public string ReplaceBadWord;
                [JsonProperty("Звук при при получении личного сообщения")]
                public string SoundPM;            
                [JsonProperty("Время,через которое удалится сообщение с UI от администратора")]
                public int TimeDeleteAlertUI;
                [JsonProperty("Steam64ID для аватарки в чате")]
                public ulong Steam64IDAvatar;
                [JsonProperty("Время через которое игрок может отправлять сообщение (АнтиСпам)")]
                public int FloodTime;
                [JsonProperty("Список плохих слов")]
                public List<string> BadWords = new List<string>();
            }
            internal class InterfaceSettings
            {
                [JsonProperty("Значения для плавного появления")]
                public float FadeIn;
                [JsonProperty("Основной цвет UI")]
                public string MainColor;
                [JsonProperty("Дополнительный цвет UI")]
                public string TwoMainColor;
                [JsonProperty("Цвет кнопок")]
                public string ButtonColor;
                [JsonProperty("Цвет текста")]
                public string LabelColor;
                [JsonProperty("Цвет UI уведомления")]
                public string AlertColor;
                [JsonProperty("Настройка расположения UI уведомления")]
                public AlertInterfaceSettings AlertInterfaceSetting;

                internal class AlertInterfaceSettings
                {
                    [JsonProperty("AnchorMin")]
                    public string AnchorMin;
                    [JsonProperty("AnchorMax")]
                    public string AnchorMax;
                    [JsonProperty("OffsetMin")]
                    public string OffsetMin;
                    [JsonProperty("OffsetMax")]
                    public string OffsetMax;
                }
            }
            internal class AlertSetting
            {
                [JsonProperty("Включить случайное сообщение зашедшему игроку")]
                public bool WelcomeMessageUse;
                [JsonProperty("Список сообщений игроку при входе")]
                public List<string> WelcomeMessage = new List<string>();
                [JsonProperty("Уведомлять о входе игрока в чат")]
                public bool ConnectedAlert;
                [JsonProperty("Включить случайные уведомления о входе игрока из списка")]
                public bool ConnectionAlertRandom;
                [JsonProperty("Случайные уведомления о входе игрока({0} - ник игрока, {1} - страна(если включено отображение страны)")]
                public List<string> RandomConnectionAlert = new List<string>();
                [JsonProperty("Отображать страну зашедшего игрока")]
                public bool ConnectedWorld;
                [JsonProperty("Уведомлять о выходе игрока в чат из списка")]
                public bool DisconnectedAlert;
                [JsonProperty("Включить случайные уведомления о входе игрока")]
                public bool DisconnectedAlertRandom;
                [JsonProperty("Случайные уведомления о входе игрока({0} - ник игрока, {1} - причина выхода(если включена причина)")]
                public List<string> RandomDisconnectedAlert = new List<string>();
                [JsonProperty("Отображать причину выхода игрока")]
                public bool DisconnectedReason;
                [JsonProperty("При уведомлении о входе/выходе игрока отображать его аватар напротив ника")]
                public bool ConnectedAvatarUse;
                [JsonProperty("Включить автоматические сообщения в чат")]
                public bool AlertMessage;
                [JsonProperty("Настройка отправки автоматических сообщений в чат")]
                public List<string> MessageList;
                [JsonProperty("Интервал отправки сообщений в чат(Броадкастер)")]
                public int MessageListTimer;
            }
            internal class OtherSettings
            {
                [JsonProperty("Использовать дискорд")]
                public bool UseDiscord;
                [JsonProperty("Вебхук для логирования чата в дискорд")]
                public string WebhooksChatLog;
                [JsonProperty("Вебхук для логирования информации о мутах в дискорде")]
                public string WebhooksMuteInfo;
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    PrefixList = new List<AdvancedFuncion>
                    {
                        new AdvancedFuncion
                        {
                            Permissions = "iqchat.default",
                            Argument = "<color=yellow><b>[+]</b></color>",
                        },
                        new AdvancedFuncion
                        {
                            Permissions = "iqchat.default",
                            Argument = "<color=yellow><b>[ИГРОК]</b></color>",
                        },
                        new AdvancedFuncion
                        {
                            Permissions = "iqchat.vip",
                            Argument = "<color=yellow><b>[VIP]</b></color>",
                        },
                    },
                    NickColorList = new List<AdvancedFuncion>
                    {
                        new AdvancedFuncion
                        {
                            Permissions = "iqchat.default",
                            Argument = "#DBEAEC",
                        },
                        new AdvancedFuncion
                        {
                            Permissions = "iqchat.default",
                            Argument = "#FFC428",
                        },
                        new AdvancedFuncion
                        {
                            Permissions = "iqchat.vip",
                            Argument = "#45AAB4",
                        },
                    },
                    MessageColorList = new List<AdvancedFuncion>
                    {
                        new AdvancedFuncion
                        {
                            Permissions = "iqchat.default",
                            Argument = "#DBEAEC",
                        },
                        new AdvancedFuncion
                        {
                            Permissions = "iqchat.default",
                            Argument = "#FFC428",
                        },
                        new AdvancedFuncion
                        {
                            Permissions = "iqchat.vip",
                            Argument = "#45AAB4",
                        },
                    },
                    AutoSetupSetting = new AutoSetups
                    {
                        ReturnDefaultSetting = new AutoSetups.ReturnDefault
                        {
                            UseDropColorChat = true,
                            UseDropColorNick = true,
                            UseDropPrefix = true,

                            PrefixDefault = "",
                            NickDefault = "",
                            MessageDefault = "",
                        },
                        SetupAutoSetting = new AutoSetups.SetupAuto
                        {
                            UseSetupAutoColorChat = true,
                            UseSetupAutoColorNick = true,
                            UseSetupAutoPrefix = true,
                        }
                    },
                    RustPlusSettings = new RustPlus
                    {
                        UseRustPlus = true,
                        DisplayNameAlert = "СУПЕР СЕРВЕР",
                    },
                    MessageSetting = new MessageSettings
                    {
                        UseBadWords = true,
                        HideAdminGave = true,
                        IgnoreUsePM = true,
                        MuteTeamChat = true,
                        PermAdminImmunitetAntispam = "iqchat.adminspam",
                        BroadcastTitle = "<color=#007FFF><b>[ОПОВЕЩЕНИЕ]</b></color>",
                        BroadcastColor = "#74ade1",
                        ReplaceBadWord = "Ругаюсь матом",
                        Steam64IDAvatar = 0,
                        TimeDeleteAlertUI = 5,
                        PMActivate = true,
                        SoundPM = "assets/bundled/prefabs/fx/notice/stack.world.fx.prefab",
                        AntiSpamActivate = true,
                        FloodTime = 5,
                        FormatingMessage = true,
                        MultiPrefix = true,
                        BadWords = new List<string> { "хуй", "гей", "говно", "бля", "тварь" }
                    },
                    ReasonListChat = new List<ReasonMuteChat>
                    {
                        new ReasonMuteChat
                        {
                            Reason = "Оскорбление родителей",
                            TimeMute = 1200,
                        },
                        new ReasonMuteChat
                        {
                            Reason = "Оскорбление игроков",
                            TimeMute = 100
                        }
                    },
                    RenamePermission = "iqchat.renameuse",                  
                    AlertSettings = new AlertSetting
                    {
                        MessageListTimer = 60,
                        WelcomeMessageUse = true,
                        ConnectionAlertRandom = false,
                        DisconnectedAlertRandom = false,
                        RandomConnectionAlert = new List<string>
                        {
                            "{0} влетел как дурачок из {1}",
                            "{0} залетел на сервер из {1}, соболезнуем",
                            "{0} прыгнул на сервачок"
                        },
                        RandomDisconnectedAlert = new List<string>
                        {
                            "{0} ушел в мир иной",
                            "{0} вылетел с сервера с причиной {1}",
                            "{0} пошел на другой сервачок"
                        },
                        ConnectedAlert = true,
                        ConnectedWorld = true,
                        DisconnectedAlert = true,
                        DisconnectedReason = true,
                        AlertMessage = true,
                        ConnectedAvatarUse = true,
                        MessageList = new List<string>
                        {
                        "Автоматическое сообщение #1",
                        "Автоматическое сообщение #2",
                        "Автоматическое сообщение #3",
                        "Автоматическое сообщение #4",
                        "Автоматическое сообщение #5",
                        "Автоматическое сообщение #6",
                        },
                        WelcomeMessage = new List<string>
                        {
                            "Добро пожаловать на сервер SUPERSERVER\nРады,что выбрал именно нас!",
                            "С возвращением на сервер!\nЖелаем тебе удачи",
                            "Добро пожаловать на сервер\nУ нас самые лучшие плагины",
                        },

                    },
                    InterfaceSetting = new InterfaceSettings
                    {
                        FadeIn = 0.2f,
                        MainColor = "#000000C0",
                        TwoMainColor = "#762424FF",
                        ButtonColor = "#802A2AFF",
                        LabelColor = "#D1C7BEFF",
                        AlertColor = "#802A2AFF",
                        AlertInterfaceSetting = new InterfaceSettings.AlertInterfaceSettings
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "0 -90",
                            OffsetMax = "320 -20"
                        }
                    }, 
                    OtherSetting = new OtherSettings
                    {
                        UseDiscord = false,
                        WebhooksChatLog = "",
                        WebhooksMuteInfo = "",
                    },
                    AnswerMessages = new AnswerMessage
                    {
                        UseAnswer = true,
                        AnswerMessageList = new Dictionary<string, string>
                        {
                            ["вайп"] = "Вайп будет 27.06",
                            ["wipe"] = "Вайп будет 27.06",
                            ["читер"] = "Нашли читера?Напиши /report и отправь жалобу"
                        }
                    },
                    ReferenceSetting = new ReferenceSettings
                    {
                        XDNotificationsSettings = new ReferenceSettings.XDNotifications
                        {                         
                            UseXDNotifications = false,
                            AlertDelete = 5,
                            Color = "#762424FF",
                            SoundEffect = "",
                        },
                        IQFakeActiveSettings = new ReferenceSettings.IQFakeActive
                        {
                            UseIQFakeActive = true,
                        },
                        IQRankSystems = new ReferenceSettings.IQRankSystem
                        {
                            UseRankSystem = false,
                            UseTimeStandart = true
                        }
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
                PrintWarning("Ошибка #132" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        void RegisteredPermissions()
        {
            for (int MsgColor = 0; MsgColor < config.MessageColorList.Count; MsgColor++)
                if (!permission.PermissionExists(config.MessageColorList[MsgColor].Permissions, this))
                    permission.RegisterPermission(config.MessageColorList[MsgColor].Permissions, this);

            for (int NickColorList = 0; NickColorList < config.NickColorList.Count; NickColorList++)
                if (!permission.PermissionExists(config.NickColorList[NickColorList].Permissions, this))
                    permission.RegisterPermission(config.NickColorList[NickColorList].Permissions, this);

            for (int PrefixList = 0; PrefixList < config.PrefixList.Count; PrefixList++)
                if (!permission.PermissionExists(config.PrefixList[PrefixList].Permissions, this))
                    permission.RegisterPermission(config.PrefixList[PrefixList].Permissions, this);

            permission.RegisterPermission(config.RenamePermission, this);
            permission.RegisterPermission(PermMuteMenu, this);
            permission.RegisterPermission(config.MessageSetting.PermAdminImmunitetAntispam,this);
            PrintWarning("Permissions - completed");
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        [JsonProperty("Дата с настройкой чата игрока")]
         public Dictionary<ulong, SettingUser> ChatSettingUser = new Dictionary<ulong, SettingUser>();
        [JsonProperty("Дата с Административной настройкой")] public AdminSettings AdminSetting = new AdminSettings();
        public class SettingUser
        {
            public string ChatPrefix;
            public List<string> MultiPrefix = new List<string>();
            public string NickColor;
            public string MessageColor;
            public double MuteChatTime;
            public double MuteVoiceTime;
            public List<ulong> IgnoredUsers = new List<ulong>();
        }

        public class AdminSettings
        {
            public bool MuteChatAll;
            public bool MuteVoiceAll;
            public Dictionary<ulong, string> RenameList = new Dictionary<ulong, string>()
;        }
        void ReadData()
        {
            ChatSettingUser = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, SettingUser>>("IQChat/IQUser");
            AdminSetting = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<AdminSettings>("IQChat/AdminSetting");
        }
        void WriteData()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQChat/IQUser", ChatSettingUser);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQChat/AdminSetting", AdminSetting);
        }

        void RegisteredDataUser(BasePlayer player)
        {
            if (!ChatSettingUser.ContainsKey(player.userID))
                ChatSettingUser.Add(player.userID, new SettingUser
                {
                    ChatPrefix = config.AutoSetupSetting.ReturnDefaultSetting.PrefixDefault,
                    NickColor = config.AutoSetupSetting.ReturnDefaultSetting.NickDefault,
                    MessageColor = config.AutoSetupSetting.ReturnDefaultSetting.MessageDefault,
                    MuteChatTime = 0,
                    MuteVoiceTime = 0,
                    MultiPrefix = new List<string> { },
                    IgnoredUsers = new List<ulong> { },
                    
                });
        }

        #endregion

        #region Hooks     
        private bool OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (Interface.Oxide.CallHook("CanChatMessage", player, message) != null) return false;
            SeparatorChat(channel, player, message);
            return false;
        }
        private object OnServerMessage(string message, string name)
        {
            if (config.MessageSetting.HideAdminGave)
                if (message.Contains("gave") && name == "SERVER")
                    return true;
            return null;
        }
        void OnUserPermissionGranted(string id, string permName) => AutoSetupData(id, permName);
        private void OnUserGroupAdded(string id, string groupName)
        {
            var PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;
            foreach (var permName in PermissionsGroup)
                AutoSetupData(id, permName); 
        }
        void OnUserPermissionRevoked(string id, string permName) => AutoReturnDefaultData(id, permName);
        void OnUserGroupRemoved(string id, string groupName)
        {
            var PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;
            foreach (var permName in PermissionsGroup)
                AutoReturnDefaultData(id, permName);
        }
        object OnPlayerVoice(BasePlayer player, Byte[] data)
        {
            var DataPlayer = ChatSettingUser[player.userID];
            bool IsMuted = DataPlayer.MuteVoiceTime > CurrentTime() ? true : false;
            if (IsMuted)
                return false;
            return null;
        }

        private void OnServerInitialized()
        {
            ReadData();
            foreach (var p in BasePlayer.activePlayerList)
                RegisteredDataUser(p);

            RegisteredPermissions();
            WriteData();
            BroadcastAuto();
        }
        void OnPlayerConnected(BasePlayer player)
        {
            RegisteredDataUser(player);
            var Alert = config.AlertSettings;
            if (Alert.ConnectedAlert)
            {
                string Avatar = Alert.ConnectedAvatarUse ? player.UserIDString : "";
                string Message = string.Empty;
                if (config.AlertSettings.ConnectedWorld)
                {
                    webrequest.Enqueue("http://ip-api.com/json/" + player.net.connection.ipaddress.Split(':')[0], null, (code, response) =>
                    {
                        if (code != 200 || response == null)
                            return;

                        string country = JsonConvert.DeserializeObject<Response>(response).Country;

                        if (Alert.ConnectionAlertRandom)
                        {
                            sb.Clear();
                            int RandomIndex = UnityEngine.Random.Range(0, Alert.RandomConnectionAlert.Count);
                            Message = sb.AppendFormat(Alert.RandomConnectionAlert[RandomIndex], player.displayName, country).ToString();
                        }
                        else Message = GetLang("WELCOME_PLAYER_WORLD", player.UserIDString, player.displayName, country);
                        ReplyBroadcast(Message, "", Avatar);
                    }, this);
                }
                else
                {
                    if (Alert.ConnectionAlertRandom)
                    {
                        sb.Clear();
                        int RandomIndex = UnityEngine.Random.Range(0, Alert.RandomConnectionAlert.Count);
                        Message = sb.AppendFormat(Alert.RandomConnectionAlert[RandomIndex], player.displayName).ToString();
                    }
                    else Message = GetLang("WELCOME_PLAYER", player.UserIDString, player.displayName);
                    ReplyBroadcast(Message);
                }
            }
            if (Alert.WelcomeMessageUse)
            {
                int RandomMessage = UnityEngine.Random.Range(0, Alert.WelcomeMessage.Count);
                string WelcomeMessage = Alert.WelcomeMessage[RandomMessage];
                ReplySystem(Chat.ChatChannel.Global, player, WelcomeMessage);
            }
        }      
        void Unload() => WriteData();

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var Alert = config.AlertSettings;
            if (Alert.DisconnectedAlert)
            {
                string Avatar = Alert.ConnectedAvatarUse ? player.UserIDString : "";
                string Message = string.Empty;
                if (Alert.DisconnectedAlertRandom)
                {
                    sb.Clear();
                    int RandomIndex = UnityEngine.Random.Range(0, Alert.RandomDisconnectedAlert.Count);
                    Message = sb.AppendFormat(Alert.RandomDisconnectedAlert[RandomIndex], player.displayName, reason).ToString();
                }
                else Message = config.AlertSettings.DisconnectedReason ? GetLang("LEAVE_PLAYER_REASON", player.UserIDString, player.displayName, reason) : GetLang("LEAVE_PLAYER", player.UserIDString, player.displayName);
                ReplyBroadcast(Message, "", Avatar);
            }
        }
        #endregion

        #region DiscordFunc

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

        void DiscordSendMessage(string key, string WebHooks, ulong userID = 0, params object[] args)
        {
            if (!config.OtherSetting.UseDiscord) return;
            if (String.IsNullOrWhiteSpace(WebHooks)) return;

            List<Fields> fields = new List<Fields>
                {
                    new Fields("IQChat", key, true),
                };

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 635133, fields, new Authors("IQChat", "https://vk.com/mir_inc", "https://i.imgur.com/ILk3uJc.png", null), new Footer("Author: Mercury[vk.com/mir_inc]", "https://i.imgur.com/ILk3uJc.png", null)) });
            Request($"{WebHooks}", newMessage.toJSON());
        }
        #endregion

        #region Func
        public bool IsMutedUser(ulong userID)
        {
            var DataPlayer = ChatSettingUser[userID];
            return DataPlayer.MuteChatTime > CurrentTime() ? true : false;
        }
        private void SeparatorChat(Chat.ChatChannel channel, BasePlayer player, string Message)
        {
            var DataPlayer = ChatSettingUser[player.userID];

            if (IsMutedUser(player.userID))
            {
                ReplySystem(Chat.ChatChannel.Global, player, GetLang("FUNC_MESSAGE_ISMUTED_TRUE", player.UserIDString, FormatTime(TimeSpan.FromSeconds(DataPlayer.MuteChatTime - CurrentTime()))));
                return;
            }

            var RankSettings = config.ReferenceSetting.IQRankSystems;
            var MessageSettings = config.MessageSetting;
            string OutMessage = Message;
            string PrefxiPlayer = "";
            string MessageSeparator = "";
            string ColorNickPlayer = DataPlayer.NickColor;
            string ColorMessagePlayer = DataPlayer.MessageColor;
            string DisplayName = AdminSetting.RenameList.ContainsKey(player.userID) ? AdminSetting.RenameList[player.userID] : player.displayName;

            if (MessageSettings.FormatingMessage)
                OutMessage = $"{Message.ToLower().Substring(0, 1).ToUpper()}{Message.Remove(0, 1).ToLower()}";

            if (MessageSettings.UseBadWords)
                foreach (var DetectedMessage in OutMessage.Split(' '))
                    if (MessageSettings.BadWords.Contains(DetectedMessage.ToLower()))
                    {
                        OutMessage = OutMessage.Replace(DetectedMessage, MessageSettings.ReplaceBadWord);
                        BadWords(player);
                    }

            if (MessageSettings.MultiPrefix)
            {
                if (DataPlayer.MultiPrefix != null)

                    for (int i = 0; i < DataPlayer.MultiPrefix.Count; i++)
                        PrefxiPlayer += DataPlayer.MultiPrefix[i];
            }
            else PrefxiPlayer = DataPlayer.ChatPrefix;

            string ModifiedNick = string.IsNullOrWhiteSpace(ColorNickPlayer) ? player.IsAdmin ? $"<color=#a8fc55>{DisplayName}</color>" : $"<color=#54aafe>{DisplayName}</color>" : $"<color={ColorNickPlayer}>{DisplayName}</color>";
            string ModifiedMessage = string.IsNullOrWhiteSpace(ColorMessagePlayer) ? OutMessage : $"<color={ColorMessagePlayer}>{OutMessage}</color>";
            string ModifiedChannel = channel == Chat.ChatChannel.Team ? "<color=#a5e664>[Team]</color>" : "";

            string Rank = string.Empty;
            string RankTime = string.Empty;
            if (IQRankSystem)
                if (RankSettings.UseRankSystem)
                {
                    if (RankSettings.UseTimeStandart)
                        RankTime = $"{IQRankGetTimeGame(player.userID)}";
                    Rank = $"{IQRankGetRank(player.userID)}";
                }
            MessageSeparator = !String.IsNullOrWhiteSpace(Rank) && !String.IsNullOrWhiteSpace(RankTime) ? $"{ModifiedChannel} [{RankTime}] [{Rank}] {PrefxiPlayer} {ModifiedNick}: {ModifiedMessage}" : !String.IsNullOrWhiteSpace(RankTime) ? $"{ModifiedChannel} [{RankTime}] {PrefxiPlayer} {ModifiedNick}: {ModifiedMessage}" : !String.IsNullOrWhiteSpace(Rank) ? $"{ModifiedChannel} [{Rank}] {PrefxiPlayer} {ModifiedNick}: {ModifiedMessage}" : $"{ModifiedChannel} {PrefxiPlayer} {ModifiedNick}: {ModifiedMessage}";


            if (config.RustPlusSettings.UseRustPlus)
                if (channel == Chat.ChatChannel.Team)
                {
                    RelationshipManager.PlayerTeam Team = RelationshipManager.Instance.FindTeam(player.currentTeam);
                    if (Team == null) return;
                    Util.BroadcastTeamChat(player.Team, player.userID, player.displayName, OutMessage, DataPlayer.MessageColor);
                }

            ReplyChat(channel, player, MessageSeparator);
            AnwserMessage(player, MessageSeparator.ToLower());
            Puts($"{player}: {OutMessage}");
            Log($"СООБЩЕНИЕ В ЧАТ : {player}: {ModifiedChannel} {OutMessage}");
            DiscordSendMessage(GetLang("DISCORD_SEND_LOG_CHAT", player.UserIDString, player.displayName, player.UserIDString, OutMessage, Message), config.OtherSetting.WebhooksChatLog, player.userID);

            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Message = $"{player.displayName} : {OutMessage}",
                UserId = player.UserIDString,
                Username = player.displayName,
                Channel = channel,
                Time = (DateTime.UtcNow.Hour * 3600) + (DateTime.UtcNow.Minute * 60),
            });
        }

        public void AutoSetupData(string id, string perm)
        {
            var AutoSetup = config.AutoSetupSetting.SetupAutoSetting;
            if (String.IsNullOrWhiteSpace(id)) return;
            if (String.IsNullOrWhiteSpace(perm)) return;
            ulong userID;
            if (!ulong.TryParse(id, out userID)) return;

            if (!ChatSettingUser.ContainsKey(userID)) return;
            var DataPlayer = ChatSettingUser[userID];

            var Prefix = config.PrefixList.FirstOrDefault(x => x.Permissions == perm);
            var ColorChat = config.MessageColorList.FirstOrDefault(x => x.Permissions == perm);
            var ColorNick = config.NickColorList.FirstOrDefault(x => x.Permissions == perm);
            if (AutoSetup.UseSetupAutoPrefix)
                if (Prefix != null)
                {
                    if (!config.MessageSetting.MultiPrefix)
                        DataPlayer.ChatPrefix = Prefix.Argument;
                    else DataPlayer.MultiPrefix.Add(Prefix.Argument);

                    BasePlayer player = BasePlayer.FindByID(userID);
                    if (player != null)
                        ReplySystem(Chat.ChatChannel.Global, player, GetLang("PREFIX_SETUP", player.UserIDString, Prefix.Argument));
                }
            if (AutoSetup.UseSetupAutoColorChat)
                if (ColorChat != null)
                {
                    DataPlayer.MessageColor = ColorChat.Argument;

                    BasePlayer player = BasePlayer.FindByID(userID);
                    if (player != null)
                        ReplySystem(Chat.ChatChannel.Global, player, GetLang("COLOR_CHAT_SETUP", player.UserIDString, ColorChat.Argument));

                }
            if (AutoSetup.UseSetupAutoColorNick)
                if (ColorNick != null)
                {
                    DataPlayer.NickColor = ColorNick.Argument;

                    BasePlayer player = BasePlayer.FindByID(userID);
                    if (player != null)
                        ReplySystem(Chat.ChatChannel.Global, player, GetLang("COLOR_NICK_SETUP", player.UserIDString, ColorNick.Argument));
                }
        }
        public void AutoReturnDefaultData(string id, string perm)
        {
            var AutoReturn = config.AutoSetupSetting.ReturnDefaultSetting;
            if (String.IsNullOrWhiteSpace(id)) return;
            if (String.IsNullOrWhiteSpace(perm)) return;
            ulong userID;
            if (!ulong.TryParse(id, out userID)) return;
            if (!userID.IsSteamId()) return;
            if (!ChatSettingUser.ContainsKey(userID)) return;

            var DataPlayer = ChatSettingUser[userID];

            var Prefix = config.PrefixList.FirstOrDefault(x => x.Permissions == perm);
            var ColorChat = config.MessageColorList.FirstOrDefault(x => x.Permissions == perm);
            var ColorNick = config.NickColorList.FirstOrDefault(x => x.Permissions == perm);

            if (AutoReturn.UseDropPrefix)
                if (Prefix != null)
                {
                    if (config.MessageSetting.MultiPrefix)
                    {
                        if (DataPlayer.MultiPrefix.Contains(Prefix.Argument))
                        {
                            DataPlayer.MultiPrefix.Remove(Prefix.Argument);

                            BasePlayer player = BasePlayer.FindByID(userID);
                            if (player != null)
                                ReplySystem(Chat.ChatChannel.Global, player, GetLang("PREFIX_RETURNRED", player.UserIDString, Prefix.Argument));
                        }
                    }
                    else if (DataPlayer.ChatPrefix == Prefix.Argument)
                    {
                        DataPlayer.ChatPrefix = AutoReturn.PrefixDefault;

                        BasePlayer player = BasePlayer.FindByID(userID);
                        if (player != null)
                            ReplySystem(Chat.ChatChannel.Global, player, GetLang("PREFIX_RETURNRED", player.UserIDString, Prefix.Argument));
                    }
                }
            if (AutoReturn.UseDropColorChat)
                if (ColorChat != null)
                    if (DataPlayer.MessageColor == ColorChat.Argument)
                    {
                        DataPlayer.MessageColor = AutoReturn.MessageDefault;

                        BasePlayer player = BasePlayer.FindByID(userID);
                        if (player != null)
                        ReplySystem(Chat.ChatChannel.Global, player, GetLang("COLOR_CHAT_RETURNRED", player.UserIDString, ColorChat.Argument));

                    }
            if (AutoReturn.UseDropColorNick)
                if (ColorNick != null)
                    if (DataPlayer.NickColor == ColorNick.Argument)
                    {
                        DataPlayer.NickColor = AutoReturn.NickDefault;

                        BasePlayer player = BasePlayer.FindByID(userID);
                        if (player != null)
                            ReplySystem(Chat.ChatChannel.Global, player, GetLang("COLOR_NICK_RETURNRED", player.UserIDString, ColorNick.Argument));
                    }
        }   
        public void AnwserMessage(BasePlayer player, string Message)
        {
            var Anwser = config.AnswerMessages;
            if (!Anwser.UseAnswer) return;

            foreach (var Anwsers in Anwser.AnswerMessageList)
                if (Message.Contains(Anwsers.Key.ToLower()))
                    ReplySystem(Chat.ChatChannel.Global, player, Anwsers.Value);
        }

        public void BroadcastAuto()
        {
            var Alert = config.AlertSettings;
            if (Alert.AlertMessage)
            {
                timer.Every(Alert.MessageListTimer, () =>
                 {
                     var RandomMsg = Alert.MessageList[UnityEngine.Random.Range(0, Alert.MessageList.Count)];
                     ReplyBroadcast(RandomMsg);
                 });
            }
        }
        public void MutePlayer(BasePlayer player, string Format, int ReasonIndex, string ResonCustom = "",string TimeCustom = "", BasePlayer Initiator = null)
        {
            var cfg = config.ReasonListChat[ReasonIndex];
            string Reason = string.IsNullOrEmpty(ResonCustom) ? cfg.Reason : ResonCustom;
            float TimeMute = string.IsNullOrEmpty(TimeCustom) ? cfg.TimeMute : Convert.ToInt32(TimeCustom);
            string DisplayInititator = Initiator == null ? "Администратор" : Initiator.displayName;
            ulong UserIdInitiator = Initiator == null ? 0 : Initiator.userID;
            switch (Format)
            {
                case "mutechat":
                    {
                        ChatSettingUser[player.userID].MuteChatTime = TimeMute + CurrentTime();
                        ReplyBroadcast(GetLang("FUNC_MESSAGE_MUTE_CHAT", player.UserIDString, DisplayInititator, player.displayName, FormatTime(TimeSpan.FromSeconds(TimeMute)), Reason));
                        if (Initiator != null)
                            SetMute(Initiator);
                        DiscordSendMessage(GetLang("DISCORD_SEND_LOG_MUTE", player.UserIDString, DisplayInititator, UserIdInitiator, player.displayName, player.userID, Reason), config.OtherSetting.WebhooksMuteInfo);
                        break;
                    }
                case "unmutechat":
                    {
                        ChatSettingUser[player.userID].MuteChatTime = 0;
                        ReplyBroadcast(GetLang("FUNC_MESSAGE_UNMUTE_CHAT",player.UserIDString), DisplayInititator);
                        break;
                    }
                case "mutevoice":
                    {
                        ChatSettingUser[player.userID].MuteVoiceTime = TimeMute + CurrentTime();
                        ReplyBroadcast(GetLang("FUNC_MESSAGE_MUTE_VOICE", player.UserIDString, DisplayInititator, player.displayName, FormatTime(TimeSpan.FromSeconds(TimeMute)), Reason)); 
                        break;
                    }
            }
        }       
        public void MuteAllChatPlayer(BasePlayer player,float TimeMute = 86400) => ChatSettingUser[player.userID].MuteChatTime = TimeMute + CurrentTime();
        public void RenameFunc(BasePlayer player,string NewName)
        {
            if (permission.UserHasPermission(player.UserIDString, config.RenamePermission))
            {
                if (!AdminSetting.RenameList.ContainsKey(player.userID))
                    AdminSetting.RenameList.Add(player.userID, NewName);
                else AdminSetting.RenameList[player.userID] = NewName;
                ReplySystem(Chat.ChatChannel.Global, player, GetLang("COMMAND_RENAME_SUCCES", player.UserIDString, NewName));
            }
            else ReplySystem(Chat.ChatChannel.Global, player, GetLang("COMMAND_NOT_PERMISSION",player.UserIDString)); 
        }
        void AlertUI(BasePlayer player, string[] arg)
        {
            if (player != null)
                if (!player.IsAdmin) return;

            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, GetLang("FUNC_MESSAGE_NO_ARG_BROADCAST", player.UserIDString));
                return;
            }
            string Message = "";
            foreach (var msg in arg)
                Message += " " + msg;

            foreach (BasePlayer p in BasePlayer.activePlayerList)
                UIAlert(p, Message);
        }
        void Alert(BasePlayer player, string[] arg)
        {
            if (player != null)
                if (!player.IsAdmin) return;

            if (arg.Length == 0 || arg == null)
            {
                if(player != null)
                ReplySystem(Chat.ChatChannel.Global, player, GetLang("FUNC_MESSAGE_NO_ARG_BROADCAST", player.UserIDString));
                return;
            }
            string Message = "";
            foreach (var msg in arg)
                Message += " " + msg;

            ReplyBroadcast(Message);
            if (config.RustPlusSettings.UseRustPlus)
                foreach(var playerList in BasePlayer.activePlayerList)
                    NotificationList.SendNotificationTo(playerList.userID, NotificationChannel.SmartAlarm, config.RustPlusSettings.DisplayNameAlert, Message, Util.GetServerPairingData());
        }

        #endregion

        #region Interface
        static string MAIN_PARENT = "MAIN_PARENT_UI";
        static string MUTE_MENU_PARENT = "MUTE_MENU_UI";
        static string ELEMENT_SETTINGS = "NEW_ELEMENT_SETTINGS";
        static string MAIN_ALERT_UI = "ALERT_UI_PLAYER";
        static string PANEL_ACTION = "PANEL_ACTION";
        static string PANEL_ACTION_HELPER = "PANEL_ACTION_HELPER";

        #region MainMenu

        public void UI_MainMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, MAIN_PARENT);
            var Interface = config.InterfaceSetting;
            float FadeInGlobal = Interface.FadeIn;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = FadeInGlobal, Color = "0 0 0 0"}
            }, "Overlay", MAIN_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.81875 0.468519", AnchorMax = "1 1" },   
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            },  MAIN_PARENT, PANEL_ACTION);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.93", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("TITLE_TWO", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, PANEL_ACTION);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 -45", OffsetMax = "231 -5" },
                Button = { FadeIn = FadeInGlobal, Close = MAIN_PARENT, Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_CLOSE_BTN", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
            }, PANEL_ACTION);

            #region ACTION BUTTON

            #region SettingPrefix

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.0 0.8383705", AnchorMax = "1 0.9179095" },
                Button = { FadeIn = FadeInGlobal, Command = "iq_chat setting prefix", Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat"},
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TEXT_PREFIX", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
            }, PANEL_ACTION, "PREFIX_SETTING");

            container.Add(new CuiElement
            {
                Parent = "PREFIX_SETTING",
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/sign.png" },
                        new CuiRectTransformComponent { AnchorMin = "0.04236809 0.1937432", AnchorMax = "0.1696546 0.7884976" }
                    }
            });

            #endregion

            #region SettingColorNick

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.0 0.7371891", AnchorMax = "1 0.8167281" },
                Button = { FadeIn = FadeInGlobal, Command = "iq_chat setting nick", Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TEXT_COLOR_NICK", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
            }, PANEL_ACTION, "COLOR_NICK_SETTING");

            container.Add(new CuiElement
            {
                Parent = "COLOR_NICK_SETTING",
                Components =
                    {
                        new CuiImageComponent {FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/sign.png"  },
                        new CuiRectTransformComponent { AnchorMin = "0.04236809 0.1937432", AnchorMax = "0.1696546 0.7884976" }
                    }
            });

            #endregion

            #region SettingText

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.0 0.6346937", AnchorMax = "1 0.7142327" },
                Button = { FadeIn = FadeInGlobal, Command = "iq_chat setting chat", Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TEXT_COLOR_MSG", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
            }, PANEL_ACTION, "TEXT_SETTING");

            container.Add(new CuiElement
            {
                Parent = "TEXT_SETTING",
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/sign.png" },
                        new CuiRectTransformComponent { AnchorMin = "0.04236809 0.1937432", AnchorMax = "0.1696546 0.7884976" }
                    }
            });

            #endregion

            #region SettingRank

            if (IQRankSystem)
                if (config.ReferenceSetting.IQRankSystems.UseRankSystem)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.1243169 0.5282561", AnchorMax = "1 0.6077951" },
                        Button = { FadeIn = FadeInGlobal, Command = "iq_chat setting rank", Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                        Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TEXT_RANK", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
                    }, PANEL_ACTION, "RANK_SYSTEM");

                    container.Add(new CuiElement
                    {
                        Parent = "RANK_SYSTEM",
                        Components =
                    {
                        new CuiImageComponent {FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/sign.png"  },
                        new CuiRectTransformComponent { AnchorMin = "0.04236809 0.1937432", AnchorMax = "0.1696546 0.7884976" }
                    }
                    });
                }
            #endregion

            #endregion

            #region ADMIN

            #region HELPERS
            if (permission.UserHasPermission(player.UserIDString, PermMuteMenu))
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.4323258", AnchorMax = "1 0.5171261" },
                    Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TEXT_MODER_PANEL", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, PANEL_ACTION);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.0 0.3298316", AnchorMax = "1 0.4093724" },
                    Button = { FadeIn = FadeInGlobal, Command = $"iq_chat mute menu", Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TEXT_MUTE_MENU_BTN", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
                }, PANEL_ACTION, "CHAT_SETTING_USER");

                container.Add(new CuiElement
                {
                    Parent = "CHAT_SETTING_USER",
                    Components =
                    {
                        new CuiImageComponent {FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/subtract.png" },
                        new CuiRectTransformComponent { AnchorMin = "0.04236809 0.1937432", AnchorMax = "0.1696546 0.7884976" }
                    }
                });
            }
            #endregion

            #region OWNER
            if (player.IsAdmin)
            {
                string CommandChat = "iq_chat admin_chat";
                string TextMuteChatButton = AdminSetting.MuteChatAll ? "UI_TEXT_ADMIN_PANEL_UNMUTE_CHAT_ALL" : "UI_TEXT_ADMIN_PANEL_MUTE_CHAT_ALL";
                string CommandMuteChatButton = AdminSetting.MuteChatAll ? "unmutechat" : "mutechat";
                string CommandVoice = "iq_chat admin_voice";
                string TextMuteVoiceButton = AdminSetting.MuteVoiceAll ? "UI_TEXT_ADMIN_PANEL_UNMUTE_VOICE_ALL" : "UI_TEXT_ADMIN_PANEL_MUTE_VOICE_ALL";
                string CommandMuteVoiceButton = AdminSetting.MuteVoiceAll ? "unmutevoice" : "mutevoice";

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.224706", AnchorMax = "1 0.3042471" },
                    Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TEXT_ADMIN_PANEL", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                },  PANEL_ACTION);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.0 0.1208954", AnchorMax = "1 0.200437" },
                    Button = { FadeIn = FadeInGlobal, Close = MAIN_PARENT, Command = $"{CommandChat} {CommandMuteChatButton}", Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat"},
                    Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage(TextMuteChatButton, this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
                },  PANEL_ACTION, "CHAT_SETTING_ADMIN");

                container.Add(new CuiElement
                {
                    Parent = "CHAT_SETTING_ADMIN",
                    Components =
                    {
                        new CuiImageComponent {FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/subtract.png" },
                        new CuiRectTransformComponent { AnchorMin = "0.04236809 0.1937432", AnchorMax = "0.1696546 0.7884976" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.1243169 0.02496903", AnchorMax = "1 0.1045107" },
                    Button = { FadeIn = FadeInGlobal, Close = MAIN_PARENT, Command = $"{CommandVoice} {CommandMuteVoiceButton}", Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage(TextMuteVoiceButton, this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
                },  PANEL_ACTION, "VOICE_SETTING_ADMIN");
            }
            container.Add(new CuiElement
            {
                Parent = "VOICE_SETTING_ADMIN",
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/subtract.png" },
                        new CuiRectTransformComponent { AnchorMin = "0.04236809 0.1937432", AnchorMax = "0.1696546 0.7884976" }
                    }
            });

            #endregion
            
            #endregion

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region PrefixSetting
        public void UI_PrefixSetting(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, ELEMENT_SETTINGS);
            CuiHelper.DestroyUi(player, MUTE_MENU_PARENT);
            CuiHelper.DestroyUi(player, PANEL_ACTION_HELPER);

            var Interface = config.InterfaceSetting;
            float FadeInGlobal = Interface.FadeIn;

            string Prefix = "";
            if (config.MessageSetting.MultiPrefix)
            {
                if (ChatSettingUser[player.userID].MultiPrefix != null)
                    for (int g = 0; g < ChatSettingUser[player.userID].MultiPrefix.Count; g++)
                        Prefix += ChatSettingUser[player.userID].MultiPrefix[g];
                else Prefix = ChatSettingUser[player.userID].ChatPrefix;
            }
            var PrefixList = config.PrefixList;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.4020834 0.3148148", AnchorMax = "0.8150954 1" },
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, MAIN_PARENT, ELEMENT_SETTINGS);

            int x = 0, y = 0, i = 0;
            foreach (var ElementPrefix in PrefixList)
            {
                if (!permission.UserHasPermission(player.UserIDString, ElementPrefix.Permissions)) continue;
                string LockStatus = "assets/icons/unlock.png";

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0 + (x * 0.525)} {0.8514473 - (y * 0.1)}", AnchorMax = $"{0.4708602 + (x * 0.525)} {0.9245502 - (y * 0.1)}" },
                    Button = { FadeIn = FadeInGlobal, Command = $"iq_chat action prefix {ElementPrefix.Argument} {ElementPrefix.Permissions}", Color = HexToRustFormat(config.InterfaceSetting.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat"},
                    Text = { FadeIn = FadeInGlobal, Text = ElementPrefix.Argument, FontSize = 17, Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
                }, ELEMENT_SETTINGS, $"BUTTON_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"BUTTON_{i}",
                    Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.03887215 0.1982514", AnchorMax = "0.1660901 0.7930056" }
                    }
                });

                x++;
                if (x == 2)
                {
                    y++;
                    x = 0;
                }
                i++;
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.93", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TITLE_NEW_PREFIX_ELEMENT", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, ELEMENT_SETTINGS);

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region NickSetting
        public void UI_NickSetting(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, ELEMENT_SETTINGS);
            CuiHelper.DestroyUi(player, MUTE_MENU_PARENT);
            CuiHelper.DestroyUi(player, PANEL_ACTION_HELPER);

            var Interface = config.InterfaceSetting;
            var ColorList = config.NickColorList;
            float FadeInGlobal = Interface.FadeIn;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.4020834 0.3148148", AnchorMax = "0.8150954 1" },
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, MAIN_PARENT, ELEMENT_SETTINGS);

            int x = 0, y = 0, i = 0;
            foreach (var ElementColor in ColorList)
            {
                if (!permission.UserHasPermission(player.UserIDString, ElementColor.Permissions)) continue;
                string LockStatus = "assets/icons/unlock.png";

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0 + (x * 0.525)} {0.8514473 - (y * 0.1)}", AnchorMax = $"{0.4708602 + (x * 0.525)} {0.9245502 - (y * 0.1)}" },
                    Button = { FadeIn = FadeInGlobal, Command = $"iq_chat action nick {ElementColor.Argument} {ElementColor.Permissions}", Color = HexToRustFormat(config.InterfaceSetting.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat"},
                    Text = { FadeIn = FadeInGlobal, Text = $"<color={ElementColor.Argument}>{player.displayName}</color>", FontSize = 17, Color = HexToRustFormat(Interface.LabelColor), Align = TextAnchor.MiddleCenter }
                }, ELEMENT_SETTINGS, $"BUTTON_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"BUTTON_{i}",
                    Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.03887215 0.1982514", AnchorMax = "0.1660901 0.7930056" }
                    }
                });

                x++;
                if (x == 2)
                {
                    y++;
                    x = 0;
                }
                i++;
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.93", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TITLE_NEW_NICK_COLOR_ELEMENT", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter}
            }, ELEMENT_SETTINGS);

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region ColorSetting
        public void UI_TextSetting(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, ELEMENT_SETTINGS);
            CuiHelper.DestroyUi(player, MUTE_MENU_PARENT);
            CuiHelper.DestroyUi(player, PANEL_ACTION_HELPER);

            var Interface = config.InterfaceSetting;
            var ColorList = config.MessageColorList;
            float FadeInGlobal = Interface.FadeIn;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.4020834 0.3148148", AnchorMax = "0.8150954 1" },
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, MAIN_PARENT, ELEMENT_SETTINGS);

            int x = 0, y = 0, i = 0;
            foreach (var ElementColor in ColorList)
            {
                if (!permission.UserHasPermission(player.UserIDString, ElementColor.Permissions)) continue;
                string LockStatus = "assets/icons/unlock.png";

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0 + (x * 0.525)} {0.8514473 - (y * 0.1)}", AnchorMax = $"{0.4708602 + (x * 0.525)} {0.9245502 - (y * 0.1)}" },
                    Button = { FadeIn = FadeInGlobal, Command = $"iq_chat action chat {ElementColor.Argument} {ElementColor.Permissions}", Color = HexToRustFormat(config.InterfaceSetting.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { FadeIn = FadeInGlobal, Text = $"<color={ElementColor.Argument}>Сообщение</color>", Align = TextAnchor.MiddleCenter }
                }, ELEMENT_SETTINGS, $"BUTTON_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"BUTTON_{i}",
                    Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.03887215 0.1982514", AnchorMax = "0.1660901 0.7930056" }
                    }
                });
                x++;
                if (x == 2)
                {
                    y++;
                    x = 0;
                }
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.93", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TITLE_NEW_MESSAGE_COLOR_ELEMENT", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter}
            }, ELEMENT_SETTINGS);

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region RankSetting
        public void UI_RankSettings(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, ELEMENT_SETTINGS);
            CuiHelper.DestroyUi(player, MUTE_MENU_PARENT);
            CuiHelper.DestroyUi(player, PANEL_ACTION_HELPER);

            var Interface = config.InterfaceSetting;
            List<string> RankKeys = IQRankListKey(player.userID);
            float FadeInGlobal = Interface.FadeIn;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5020834 0.1148148", AnchorMax = "0.8150954 0.8814815" },
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, MAIN_PARENT, ELEMENT_SETTINGS);

            int x = 0, y = 0, i = 0;
            foreach (var ElementRank in RankKeys.Where(r => IQRankUserAcces(player.userID, r)))
            {
                string LockStatus = "assets/icons/unlock.png";
                string RankName = IQRankGetNameRankKey(ElementRank);
                if (String.IsNullOrWhiteSpace(RankName)) continue;

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0 + (x * 0.525)} {0.8514473 - (y * 0.1)}", AnchorMax = $"{0.4708602 + (x * 0.525)} {0.9245502 - (y * 0.1)}" },
                    Button = { FadeIn = FadeInGlobal, Command = $"iq_chat action rank {ElementRank}", Color = HexToRustFormat(config.InterfaceSetting.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { FadeIn = FadeInGlobal, Text = $"{RankName}", Align = TextAnchor.MiddleCenter }
                }, ELEMENT_SETTINGS, $"BUTTON_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"BUTTON_{i}",
                    Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.03887215 0.1982514", AnchorMax = "0.1660901 0.7930056" }
                    }
                });
                x++;
                if (x == 2)
                {
                    y++;
                    x = 0;
                }
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.93", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_TITLE_NEW_MESSAGE_RANK_ELEMENT", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, ELEMENT_SETTINGS);

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region MuteMenu
        public void UI_MuteMenu(BasePlayer player, string TargetName = "")
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, MUTE_MENU_PARENT);
            CuiHelper.DestroyUi(player, ELEMENT_SETTINGS);
            CuiHelper.DestroyUi(player, PANEL_ACTION_HELPER);

            var Interface = config.InterfaceSetting;
            float FadeInGlobal = Interface.FadeIn;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1546875 0.1148148", AnchorMax = "0.8150954 0.8814815" },
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.TwoMainColor) }
            }, MAIN_PARENT, MUTE_MENU_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9227053", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_MUTE_PANEL_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  MUTE_MENU_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.898551", AnchorMax = "1 0.9456524" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_MUTE_PANEL_TITLE_ACTION", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, MUTE_MENU_PARENT);

            string SearchName = "";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.8417874", AnchorMax = "1 0.8961352" },
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.ButtonColor) }
            }, MUTE_MENU_PARENT, MUTE_MENU_PARENT + ".Input");

            container.Add(new CuiElement
            {
                Parent = MUTE_MENU_PARENT + ".Input",
                Name = MUTE_MENU_PARENT + ".Input.Current",
                Components =
                {
                    new CuiInputFieldComponent { Text = SearchName, FontSize = 14,Command = $"mute_search {SearchName}", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#ffffffFF"), CharsLimit = 15},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            int x = 0; int y = 0;
            foreach (var pList in BasePlayer.activePlayerList.Where(i => i.displayName.ToLower().Contains(TargetName.ToLower())))
            {
                string LockStatus = ChatSettingUser[pList.userID].MuteChatTime > CurrentTime() ? "assets/icons/lock.png" :
                                    ChatSettingUser[pList.userID].MuteVoiceTime > CurrentTime() ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0.006797731 + (x * 0.165)} {0.7838164 - (y * 0.057)}", AnchorMax = $"{0.1661653 + (x * 0.165)} {0.8309178 - (y * 0.057)}" },
                    Button = { FadeIn = FadeInGlobal, Command = $"iq_chat mute actionmenu {pList.userID}", Color = HexToRustFormat(Interface.ButtonColor) },
                    Text = { FadeIn = FadeInGlobal, Text = "", Align = TextAnchor.MiddleCenter }
                }, MUTE_MENU_PARENT, $"BUTTON{player.userID}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.1611373 0", AnchorMax = "1 1" },
                    Text = { FadeIn = FadeInGlobal, Text = pList.displayName.Replace(" ", ""), FontSize = 12, Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                }, $"BUTTON{player.userID}");

                container.Add(new CuiElement
                {
                    Parent = $"BUTTON{player.userID}",
                    Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.02369668 0.2051285", AnchorMax = "0.1374408 0.820514" }
                    }
                });

                x++;
                if (y == 12 && x == 6) break;

                if (x == 6)
                {
                    y++;
                    x = 0;
                }

            };

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.02870133 0.05434785", AnchorMax = "0.3300647 0.08333336" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_MUTE_PANEL_TITLE_HELPS_LOCK",this, player.UserIDString), FontSize = 10, Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            },  MUTE_MENU_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.02870133 0.01570053", AnchorMax = "0.3300647 0.04468608" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_MUTE_PANEL_TITLE_HELPS_UNLOCK", this, player.UserIDString), FontSize = 10, Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, MUTE_MENU_PARENT);

            container.Add(new CuiElement
            {
                Parent = MUTE_MENU_PARENT,
                Components =
                    {
                        new CuiImageComponent {FadeIn = FadeInGlobal,   Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/lock.png" },
                        new CuiRectTransformComponent { AnchorMin = "0.006797716 0.05434785", AnchorMax = "0.02492483 0.08333336" }
                    }
            });

            container.Add(new CuiElement
            {
                Parent = MUTE_MENU_PARENT,
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/unlock.png" },
                        new CuiRectTransformComponent { AnchorMin = "0.006797716 0.01449281", AnchorMax = "0.02492483 0.04347835" }
                    }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region MuteAction
        
        public void UI_MuteTakeAction(BasePlayer player,ulong userID)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PANEL_ACTION_HELPER);
            var Interface = config.InterfaceSetting;
            float FadeInGlobal = Interface.FadeIn;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.01197916 0.1148148", AnchorMax = "0.1505208 0.8814149" },  
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            },  MAIN_PARENT, PANEL_ACTION_HELPER);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.919082", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_MUTE_TAKE_ACTION_PANEL", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  PANEL_ACTION_HELPER);

            string LockStatus = ChatSettingUser[userID].MuteChatTime > CurrentTime() ? "assets/icons/unlock.png" :
                    ChatSettingUser[userID].MuteVoiceTime > CurrentTime() ? "assets/icons/unlock.png" : "assets/icons/lock.png";

            string ButtonChat = ChatSettingUser[userID].MuteChatTime > CurrentTime() ?  "UI_MUTE_TAKE_ACTION_CHAT_UNMUTE" : "UI_MUTE_TAKE_ACTION_CHAT";
            string ButtonVoice = ChatSettingUser[userID].MuteVoiceTime > CurrentTime() ? "UI_MUTE_TAKE_ACTION_VOICE_UNMUTE" : "UI_MUTE_TAKE_ACTION_VOICE";
            string ButtonCommandChat = ChatSettingUser[userID].MuteChatTime > CurrentTime() ? $"iq_chat mute action {userID} unmutechat" : $"iq_chat mute action {userID} mute mutechat";
            string ButtonCommandVoice = ChatSettingUser[userID].MuteVoiceTime > CurrentTime() ? $"iq_chat mute action {userID} unmutevoice" : $"iq_chat mute action {userID} mute mutevoice";

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.8357491", AnchorMax = "0.903084 0.8961352" },
                Button = { FadeIn = FadeInGlobal, Command = ButtonCommandChat, Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat"},
                Text = { FadeIn = FadeInGlobal, Text = "",  Align = TextAnchor.MiddleCenter }
            },  PANEL_ACTION_HELPER, "CHAT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1790024 0", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage(ButtonChat, this, player.UserIDString), FontSize = 10, Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, $"CHAT");

            container.Add(new CuiElement
            {
                Parent = $"CHAT",
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.02369668 0.2051285", AnchorMax = "0.1374408 0.820514" }
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.7620788", AnchorMax = "0.903084 0.8224649" },
                Button = { FadeIn = FadeInGlobal, Command = ButtonCommandVoice, Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat"},
                Text = { FadeIn = FadeInGlobal, Text = "", Align = TextAnchor.MiddleCenter }
            },  PANEL_ACTION_HELPER, "VOICE");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1790024 0", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage(ButtonVoice, this, player.UserIDString), FontSize = 10, Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, $"VOICE");

            container.Add(new CuiElement
            {
                Parent = $"VOICE",
                Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.LabelColor), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.02369668 0.2051285", AnchorMax = "0.1374408 0.820514" }
                    }
            });

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region ReasonMute
        void UI_ReasonMute(BasePlayer player,ulong userID, string MuteFormat)
        {
            CuiElementContainer container = new CuiElementContainer();
            var Interface = config.InterfaceSetting;
            float FadeInGlobal = Interface.FadeIn;

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.6702939", AnchorMax = "1 0.7512119" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_MUTE_TAKE_REASON_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  PANEL_ACTION_HELPER);

            int i = 0;
            foreach(var Reason in config.ReasonListChat)
            {           
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 {0.5942072 - (i * 0.07)}", AnchorMax = $"0.903084 {0.6545933 - (i * 0.07)}" },
                    Button = { FadeIn = FadeInGlobal, Command = $"iq_chat mute action {userID} mute_reason {MuteFormat} {i}", Color = HexToRustFormat(Interface.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { FadeIn = FadeInGlobal, Text = "", Align = TextAnchor.MiddleCenter }
                },  PANEL_ACTION_HELPER, $"BUTTON{i}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.1790024 0", AnchorMax = "1 1" },
                    Text = { FadeIn = FadeInGlobal, Text = Reason.Reason, FontSize = 10, Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                },  $"BUTTON{i}");

                container.Add(new CuiElement
                {
                    Parent = $"BUTTON{i}",
                    Components =
                    {
                        new CuiImageComponent { FadeIn = FadeInGlobal,  Color = HexToRustFormat(Interface.LabelColor), Sprite = "assets/icons/favourite_servers.png" },
                        new CuiRectTransformComponent { AnchorMin = "0.02369668 0.2051285", AnchorMax = "0.1374408 0.820514" }
                    }
                });
                i++;
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region UpdateLabel
        public void UpdateLabel(BasePlayer player, SettingUser DataPlayer, string Rank = "")
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "UPDATE_LABEL");

            string Prefix = "";
            if (config.MessageSetting.MultiPrefix)
            {
                if (DataPlayer.MultiPrefix != null)
                    for (int i = 0; i < DataPlayer.MultiPrefix.Count; i++)
                        Prefix += DataPlayer.MultiPrefix[i];
            }
            else Prefix = DataPlayer.ChatPrefix;
            string ResultNick = !String.IsNullOrEmpty(Rank) ? $"<b>[{Rank}] {Prefix}<color={DataPlayer.NickColor}>{player.displayName}</color> : <color={DataPlayer.MessageColor}> я лучший</color></b>" : $"<b>{Prefix}<color={DataPlayer.NickColor}>{player.displayName}</color> : <color={DataPlayer.MessageColor}> я лучший</color></b>";

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.07367153" },
                Text = { FadeIn = config.InterfaceSetting.FadeIn, Text = $"{ResultNick}", FontSize = 14, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"}
            },  ELEMENT_SETTINGS, "UPDATE_LABEL");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region UIAlert
        void UIAlert(BasePlayer player, string Message)
        {
            if (XDNotifications && config.ReferenceSetting.XDNotificationsSettings.UseXDNotifications)
            {
                AddNotify(player, lang.GetMessage("UI_ALERT_TITLE", this, player.UserIDString), Message);
                return;
            }
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, MAIN_ALERT_UI);
            var Interface = config.InterfaceSetting;
            var Transform = Interface.AlertInterfaceSetting;
            float FadeInGlobal = Interface.FadeIn;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = Transform.AnchorMin, AnchorMax = Transform.AnchorMax, OffsetMin = Transform.OffsetMin, OffsetMax = Transform.OffsetMax },
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(config.InterfaceSetting.ButtonColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", MAIN_ALERT_UI);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.025 0.5523812", AnchorMax = "0.1 0.8952149" },  //
                Image = { FadeIn = FadeInGlobal, Color = HexToRustFormat(Interface.MainColor), Sprite = "assets/icons/upgrade.png" }
            }, MAIN_ALERT_UI);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1125001 0.5037036", AnchorMax = "1 1" },
                Text = { FadeIn = FadeInGlobal, Text = lang.GetMessage("UI_ALERT_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, MAIN_ALERT_UI);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.5259256" },
                Text = { FadeIn = FadeInGlobal, Text = $"{Message}", FontSize = 12, Color = HexToRustFormat(Interface.LabelColor), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, MAIN_ALERT_UI);

            CuiHelper.AddUi(player, container);

            timer.Once(config.MessageSetting.TimeDeleteAlertUI, () =>
            {
                CuiHelper.DestroyUi(player, MAIN_ALERT_UI);
            });
        }
        #endregion

        #endregion

        #region Command

        #region UsingCommand
        [ConsoleCommand("mute")]
        void MuteCustomAdmin(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Args == null || arg.Args.Length != 3 || arg.Args.Length > 3)
            {
                PrintWarning("Неверный синтаксис,используйте : mute Steam64ID Причина Время(секунды)");
                return;
            }
            ulong userID = ulong.Parse(arg.Args[0]);
            if (!userID.IsSteamId())
            {
                PrintWarning("Неверный Steam64ID");
                return;
            }
            string Reason = arg.Args[1];
            string TimeMute = arg.Args[2];
            BasePlayer target = BasePlayer.FindByID(userID);
            if (target == null)
            {
                PrintWarning("Такого игрока нет на сервере");
                return;
            }
            MutePlayer(target, "mutechat", 0, Reason, TimeMute);
            Puts("Успешно");
        }
        [ConsoleCommand("unmute")]
        void UnMuteCustomAdmin(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Args == null || arg.Args.Length != 1 || arg.Args.Length > 1)
            {
                PrintWarning("Неверный синтаксис,используйте : unmute Steam64ID");
                return;
            }
            ulong userID = ulong.Parse(arg.Args[0]);
            if (!userID.IsSteamId())
            {
                PrintWarning("Неверный Steam64ID");
                return;
            }
            BasePlayer target = BasePlayer.FindByID(userID);
            if (target == null)
            {
                PrintWarning("Такого игрока нет на сервере");
                return;
            }
            ChatSettingUser[target.userID].MuteChatTime = 0;
            ReplyBroadcast(GetLang("FUNC_MESSAGE_UNMUTE_CHAT",target.UserIDString, "Администратор", target.displayName));
            Puts("Успешно");
        }
        [ChatCommand("chat")]
        void ChatCommandMenu(BasePlayer player) => UI_MainMenu(player);

        [ChatCommand("alert")]
        void ChatAlertPlayers(BasePlayer player, string cmd, string[] arg) => Alert(player, arg);

        [ChatCommand("alertui")]
        void ChatAlertPlayersUI(BasePlayer player, string cmd, string[] arg) => AlertUI(player, arg);

        [ChatCommand("rename")]
        void RenameMetods(BasePlayer player, string cmd, string[] arg)
        {
            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_RENAME_NOTARG", this, player.UserIDString));
                return;
            }
            string NewName = "";
            foreach (var name in arg)
                NewName += " " + name;
            RenameFunc(player, NewName);
        }

        #region PM

        [ChatCommand("pm")]
        void PmChat(BasePlayer player, string cmd, string[] arg)
        {
            if (!config.MessageSetting.PMActivate) return;
            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_PM_NOTARG", this, player.UserIDString));
                return;
            }
            string NameUser = arg[0];
            if (config.ReferenceSetting.IQFakeActiveSettings.UseIQFakeActive)
                if (IQFakeActive)
                    if (IsFake(NameUser))
                    {
                        ReplySystem(Chat.ChatChannel.Global, player, GetLang("COMMAND_PM_SUCCESS", player.UserIDString, string.Join(" ", arg.ToArray().ToArray()).Replace(NameUser, "")));
                        return;
                    }
            BasePlayer TargetUser = FindPlayer(NameUser);
            if (TargetUser == null || NameUser == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_PM_NOT_USER", this, player.UserIDString));
                return;
            }
            if (config.MessageSetting.IgnoreUsePM)
            {
                if (ChatSettingUser[TargetUser.userID].IgnoredUsers.Contains(player.userID))
                {
                    ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("IGNORE_NO_PM", this, player.UserIDString));
                    return;
                }
                if (ChatSettingUser[player.userID].IgnoredUsers.Contains(TargetUser.userID))
                {
                    ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("IGNORE_NO_PM_ME", this, player.UserIDString));
                    return;
                }
            }
            var argList = arg.ToArray();
            string Message = string.Join(" ", argList.ToArray()).Replace(NameUser, "");
            if (Message.Length > 125) return;
            if (Message.Length <= 0 || Message == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_PM_NOT_NULL_MSG", this, player.UserIDString));
                return;
            }

            PMHistory[TargetUser] = player;
            PMHistory[player] = TargetUser;
            var DisplayNick = AdminSetting.RenameList.ContainsKey(player.userID) ? AdminSetting.RenameList[player.userID] : player.displayName;

            ReplySystem(Chat.ChatChannel.Global, TargetUser, GetLang("COMMAND_PM_SEND_MSG", player.UserIDString, DisplayNick, Message));
            ReplySystem(Chat.ChatChannel.Global, player, GetLang("COMMAND_PM_SUCCESS", player.UserIDString, Message));
            Effect.server.Run(config.MessageSetting.SoundPM, TargetUser.GetNetworkPosition());
            Log($"ЛИЧНЫЕ СООБЩЕНИЯ : {player.userID}({DisplayNick}) отправил сообщение игроку - {TargetUser.displayName}\nСООБЩЕНИЕ : {Message}");

            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Message = $"ЛИЧНЫЕ СООБЩЕНИЯ : {DisplayNick}({player.userID}) -> {TargetUser.displayName} : СООБЩЕНИЕ : {Message}",
                UserId = player.UserIDString,
                Username = player.displayName,
                Channel = Chat.ChatChannel.Global,
                Time = (DateTime.UtcNow.Hour * 3600) + (DateTime.UtcNow.Minute * 60),
                Color = "#3f4bb8",
            });
            PrintWarning($"ЛИЧНЫЕ СООБЩЕНИЯ : {DisplayNick}({player.userID}) -> {TargetUser.displayName} : СООБЩЕНИЕ : {Message}");
        }

        [ChatCommand("r")]
        void RChat(BasePlayer player, string cmd, string[] arg)
        {
            if (!config.MessageSetting.PMActivate) return;
            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_R_NOTARG", this, player.UserIDString));
                return;
            }
            if (!PMHistory.ContainsKey(player))
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_R_NOTMSG", this, player.UserIDString));
                return;
            }
            BasePlayer RetargetUser = PMHistory[player];
            if (RetargetUser == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_PM_NOT_USER", this, player.UserIDString));
                return;
            }
            if (config.MessageSetting.IgnoreUsePM)
            {
                if (ChatSettingUser[RetargetUser.userID].IgnoredUsers.Contains(player.userID))
                {
                    ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("IGNORE_NO_PM", this, player.UserIDString));
                    return;
                }
                if (ChatSettingUser[player.userID].IgnoredUsers.Contains(RetargetUser.userID))
                {
                    ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("IGNORE_NO_PM_ME", this, player.UserIDString));
                    return;
                }
            }
            string Message = string.Join(" ", arg.ToArray());
            if (Message.Length > 125) return;
            if (Message.Length <= 0 || Message == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_PM_NOT_NULL_MSG", this, player.UserIDString));
                return;
            }
            PMHistory[RetargetUser] = player;
            var DisplayNick = AdminSetting.RenameList.ContainsKey(player.userID) ? AdminSetting.RenameList[player.userID] : player.displayName;

            ReplySystem(Chat.ChatChannel.Global, RetargetUser, GetLang("COMMAND_PM_SEND_MSG", player.UserIDString, DisplayNick, Message));
            ReplySystem(Chat.ChatChannel.Global, player, GetLang("COMMAND_PM_SUCCESS", player.UserIDString, Message));

            Effect.server.Run(config.MessageSetting.SoundPM, RetargetUser.GetNetworkPosition());
            Log($"ЛИЧНЫЕ СООБЩЕНИЯ : {player.displayName} отправил сообщение игроку - {RetargetUser.displayName}\nСООБЩЕНИЕ : {Message}");

            RCon.Broadcast(RCon.LogType.Chat, new Chat.ChatEntry
            {
                Message = $"ЛИЧНЫЕ СООБЩЕНИЯ : {DisplayNick}({player.userID}) -> {RetargetUser.displayName} : СООБЩЕНИЕ : {Message}",
                UserId = player.UserIDString,
                Username = player.displayName,
                Channel = Chat.ChatChannel.Global,
                Time = (DateTime.UtcNow.Hour * 3600) + (DateTime.UtcNow.Minute * 60),
                Color = "#3f4bb8",
            });
            PrintWarning($"ЛИЧНЫЕ СООБЩЕНИЯ : {DisplayNick}({player.userID}) -> {RetargetUser.displayName} : СООБЩЕНИЕ : {Message}");
        }

        [ChatCommand("ignore")]
        void IgnorePlayerPM(BasePlayer player, string cmd, string[] arg)
        {
            if (!config.MessageSetting.IgnoreUsePM) return;
            var ChatUser = ChatSettingUser[player.userID];
            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("INGORE_NOTARG", this, player.UserIDString));
                return;
            }
            string NameUser = arg[0];
            BasePlayer TargetUser = FindPlayer(NameUser);
            if (TargetUser == null || NameUser == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_PM_NOT_USER", this, player.UserIDString));
                return;
            }

            string Lang = !ChatUser.IgnoredUsers.Contains(TargetUser.userID) ? GetLang("IGNORE_ON_PLAYER", player.UserIDString, TargetUser.displayName) : GetLang("IGNORE_OFF_PLAYER", player.UserIDString, TargetUser.displayName);
            ReplySystem(Chat.ChatChannel.Global, player, Lang);
            if (!ChatUser.IgnoredUsers.Contains(TargetUser.userID))
                ChatUser.IgnoredUsers.Add(TargetUser.userID);
            else ChatUser.IgnoredUsers.Remove(TargetUser.userID);
        }

        #endregion

        [ConsoleCommand("alert")]
        void ChatAlertPlayersCMD(ConsoleSystem.Arg arg) => Alert(arg.Player(), arg.Args);

        [ConsoleCommand("alertui")]
        void ChatAlertPlayersUICMD(ConsoleSystem.Arg arg) => AlertUI(arg.Player(), arg.Args);

        [ConsoleCommand("alertuip")]
        void CmodAlertOnlyUser(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2)
            {
                PrintWarning("Используйте правильно ситаксис : alertuip Steam64ID Сообщение");
                return;
            }
            ulong SteamID = ulong.Parse(arg.Args[0]);
            var argList = arg.Args.ToArray();
            string Message = string.Join(" ", argList.ToArray().Skip(1));
            if (Message.Length > 125) return;
            if (Message.Length <= 0 || Message == null)
            {
                PrintWarning("Вы не указали сообщение игроку");
                return;
            }
            BasePlayer player = BasePlayer.FindByID(SteamID);
            if (player == null)
            {
                PrintWarning("Игрока нет в сети");
                return;
            }
            UIAlert(player, Message);
        }

        [ConsoleCommand("saybro")]
        void ChatAlertPlayerInPM(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2)
            {
                PrintWarning("Используйте правильно ситаксис : saybro Steam64ID Сообщение");
                return;
            }
            ulong SteamID = ulong.Parse(arg.Args[0]);
            var argList = arg.Args.ToArray();
            string Message = string.Join(" ", argList.ToArray());
            if (Message.Length > 125) return;
            if (Message.Length <= 0 || Message == null)
            {
                PrintWarning("Вы не указали сообщение игроку");
                return;
            }
            BasePlayer player = BasePlayer.FindByID(SteamID);
            if(player == null)
            {
                PrintWarning("Игрока нет в сети");
                return;
            }
            ReplySystem(Chat.ChatChannel.Global, player, Message.Replace(SteamID.ToString(), ""));
        }

        [ConsoleCommand("set")]
        private void ConsolesCommandPrefixSet(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length != 3)
            {
                PrintWarning("Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                return;
            }
            ulong Steam64ID = 0;
            BasePlayer player = null;
            if (ulong.TryParse(arg.Args[0], out Steam64ID))
                player = BasePlayer.FindByID(Steam64ID);
            if (player == null)
            {
                PrintWarning("Неверно указан SteamID игрока или ошибка в синтаксисе\nИспользуйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                return;
            }
            var DataPlayer = ChatSettingUser[player.userID];

            switch (arg.Args[1].ToLower())
            {
                case "prefix":
                    {
                        string KeyPrefix = arg.Args[2];
                        foreach (var Prefix in config.PrefixList.Where(x => x.Permissions == KeyPrefix))
                            if (config.PrefixList.Contains(Prefix))
                            {
                                DataPlayer.ChatPrefix = Prefix.Argument;
                                Puts($"Префикс успешно установлен на - {Prefix.Argument}");
                            }
                            else Puts("Неверно указан Permissions от префикса");
                        break;
                    }
                case "chat":
                    {
                        string KeyChatColor = arg.Args[2];
                        foreach (var ColorChat in config.PrefixList.Where(x => x.Permissions == KeyChatColor))
                            if (config.MessageColorList.Contains(ColorChat))
                            {
                                DataPlayer.MessageColor = ColorChat.Argument;
                                Puts($"Цвет сообщения успешно установлен на - {ColorChat.Argument}");
                            }
                            else Puts("Неверно указан Permissions от префикса");
                        break;
                    }
                case "nick":
                    {
                        string KeyNickColor = arg.Args[2];
                        foreach (var ColorChat in config.NickColorList.Where(x => x.Permissions == KeyNickColor))
                            if (config.NickColorList.Contains(ColorChat))
                            {
                                DataPlayer.NickColor = ColorChat.Argument;
                                Puts($"Цвет ника успешно установлен на - {ColorChat.Argument}");
                            }
                            else Puts("Неверно указан Permissions от префикса");
                        break;
                    }
                case "custom":
                    {
                        string CustomPrefix = arg.Args[2];
                        DataPlayer.ChatPrefix = CustomPrefix;
                        Puts($"Кастомный префикс успешно установлен на - {CustomPrefix}");
                        break;
                    }
                default:
                    {
                        PrintWarning("Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                        break;
                    }
            }

        }

        #endregion

        #region FuncCommand

        [ConsoleCommand("mute_search")]
        void ConsoleSearchMute(ConsoleSystem.Arg arg)
        {
            BasePlayer moder = arg.Player();
            if (arg.Args == null || arg.Args.Length == 0) return;
            string Searcher = arg.Args[0].ToLower();
            if (string.IsNullOrEmpty(Searcher) || Searcher.Length == 0 || Searcher.Length < 1) return;
            UI_MuteMenu(moder, Searcher);
        }                              
        
        [ConsoleCommand("iq_chat")]
        private void ConsoleCommandIQChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            var DataPlayer = ChatSettingUser[player.userID];

            switch (arg.Args[0])
            {
                #region Setting
                case "setting": 
                    {
                        switch(arg.Args[1])
                        {
                            case "prefix":
                                {
                                    UI_PrefixSetting(player);
                                    break;
                                }
                            case "nick":
                                {
                                    UI_NickSetting(player);
                                    break;
                                }
                            case "chat":
                                {
                                    UI_TextSetting(player);
                                    break;
                                }
                            case "rank":
                                {
                                    UI_RankSettings(player);
                                    break;
                                }
                        }
                        break;
                    }
                #endregion

                #region Action
                case "action": 
                    {
                        switch(arg.Args[1])
                        {
                            case "prefix":
                                {
                                    var Selected = arg.Args[2];
                                    var Permission = arg.Args[3];
                                    if (!permission.UserHasPermission(player.UserIDString, Permission)) return;

                                    if (config.MessageSetting.MultiPrefix)
                                    {
                                        if (!DataPlayer.MultiPrefix.Contains(Selected))
                                            DataPlayer.MultiPrefix.Add(Selected);
                                        else DataPlayer.MultiPrefix.Remove(Selected);
                                    }
                                    if (DataPlayer.ChatPrefix != Selected)
                                        DataPlayer.ChatPrefix = Selected;
                                    else DataPlayer.ChatPrefix = config.AutoSetupSetting.ReturnDefaultSetting.PrefixDefault;
                                    UpdateLabel(player, DataPlayer);
                                    break;
                                }
                            case "nick":
                                {
                                    var Selected = arg.Args[2];
                                    var Permission = arg.Args[3];
                                    if (!permission.UserHasPermission(player.UserIDString, Permission)) return;

                                    if (DataPlayer.NickColor != Selected)
                                        DataPlayer.NickColor = Selected;
                                    else DataPlayer.NickColor = config.AutoSetupSetting.ReturnDefaultSetting.NickDefault;
                                    UpdateLabel(player, DataPlayer);
                                    break;
                                }
                            case "chat":
                                {
                                    var Selected = arg.Args[2];
                                    var Permission = arg.Args[3];
                                    if (!permission.UserHasPermission(player.UserIDString, Permission)) return;

                                    if (DataPlayer.MessageColor != Selected)
                                        DataPlayer.MessageColor = Selected;
                                    else DataPlayer.MessageColor = config.AutoSetupSetting.ReturnDefaultSetting.MessageDefault;
                                    UpdateLabel(player, DataPlayer);
                                    break;
                                }
                            case "rank":
                                {
                                    string RankKey = arg.Args[2];
                                    IQRankSetRank(player.userID, RankKey);
                                    UpdateLabel(player, DataPlayer, IQRankGetNameRankKey(RankKey));
                                    break;
                                }
                        }
                        break;
                    }
                #endregion
                
                #region Mute
                case "mute":
                    {
                        string Action = arg.Args[1];
                        switch (Action)
                        {
                            case "menu":
                                {
                                    if (permission.UserHasPermission(player.UserIDString, PermMuteMenu))
                                        UI_MuteMenu(player);
                                    break;
                                }
                            case "actionmenu":
                                {
                                    BasePlayer target = BasePlayer.FindByID(ulong.Parse(arg.Args[2]));
                                    UI_MuteTakeAction(player, target.userID);
                                    break;
                                }
                            case "action": 
                                {
                                    BasePlayer target = BasePlayer.FindByID(ulong.Parse(arg.Args[2]));
                                    string MuteAction = arg.Args[3];
                                    switch (MuteAction)
                                    {
                                        case "mute":
                                            {
                                                string MuteFormat = arg.Args[4];
                                                UI_ReasonMute(player, target.userID, MuteFormat);
                                                break;
                                            }
                                        case "mute_reason":
                                            {
                                                CuiHelper.DestroyUi(player, MAIN_PARENT);
                                                string MuteFormat = arg.Args[4];
                                                int Index = Convert.ToInt32(arg.Args[5]);
                                                MutePlayer(target, MuteFormat, Index, "", "", player);
                                                break;
                                            }
                                        case "unmutechat":
                                            {
                                                CuiHelper.DestroyUi(player, MAIN_PARENT);
                                                ChatSettingUser[target.userID].MuteChatTime = 0;
                                                ReplyBroadcast(GetLang("FUNC_MESSAGE_UNMUTE_CHAT", player.UserIDString, player.displayName, target.displayName));
                                                break;
                                            }
                                        case "unmutevoice":
                                            {
                                                CuiHelper.DestroyUi(player, MAIN_PARENT);
                                                ChatSettingUser[target.userID].MuteVoiceTime = 0;
                                                ReplyBroadcast(GetLang("FUNC_MESSAGE_UNMUTE_VOICE", player.UserIDString, player.displayName, target.displayName));
                                                break;
                                            }
                                    }
                                    break;
                                }
                        }
                        break;
                    }              
                #endregion

                #region ADMIN
                case "admin_voice":
                    {
                        var Command = arg.Args[1];
                        switch(Command)
                        {
                            case "mutevoice":
                                {
                                    AdminSetting.MuteVoiceAll = true;
                                    foreach (var p in BasePlayer.activePlayerList)
                                        ChatSettingUser[p.userID].MuteVoiceTime = CurrentTime() + 86400;
                                    ReplyBroadcast(lang.GetMessage("FUNC_MESSAGE_MUTE_ALL_VOICE", this, player.UserIDString));
                                    break;
                                }
                            case "unmutevoice":
                                {
                                    AdminSetting.MuteVoiceAll = false;
                                    foreach (var p in BasePlayer.activePlayerList)
                                        ChatSettingUser[p.userID].MuteVoiceTime = 0;
                                    ReplyBroadcast(lang.GetMessage("FUNC_MESSAGE_UNMUTE_ALL_VOICE", this, player.UserIDString));
                                    break;
                                }
                        }
                        foreach (var p in BasePlayer.activePlayerList)
                            rust.RunServerCommand(Command, p.userID);
                        break;
                    }
                case "admin_chat":
                    {
                        var Command = arg.Args[1];
                        switch(Command)
                        {
                            case "mutechat":
                                {
                                    AdminSetting.MuteChatAll = true;
                                    foreach (var p in BasePlayer.activePlayerList)
                                        MuteAllChatPlayer(p);
                                    ReplyBroadcast(lang.GetMessage("FUNC_MESSAGE_MUTE_ALL_CHAT", this, player.UserIDString));
                                    break;
                                }
                            case "unmutechat":
                                {
                                    AdminSetting.MuteChatAll = false;
                                    foreach (var p in BasePlayer.activePlayerList)
                                        ChatSettingUser[p.userID].MuteChatTime = 0;
                                    ReplyBroadcast(lang.GetMessage("FUNC_MESSAGE_UNMUTE_ALL_CHAT", this, player.UserIDString));
                                    break;
                                }
                        }
                        break;
                    }
                    #endregion
            }
        }

        #endregion

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            PrintWarning("Языковой файл загружается...");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE_ONE"] = "<size=30><b>Chat SETTINGS</b></size>",
                ["TITLE_TWO"] = "<size=16><b>SELECT</b></size>",
                ["UI_CLOSE_BTN"] = "<size=20>CLOSE</size>",

                ["UI_TEXT_PREFIX"] = "<size=14>TAG</size>",
                ["UI_TEXT_COLOR_NICK"] = "<size=14>NICKNAME COLOR</size>",
                ["UI_TEXT_COLOR_MSG"] = "<size=14>TEXT COLOR</size>",
                ["UI_TEXT_RANK"] = "<size=23>RANKS</size>",
                ["UI_TEXT_VOTE_MENU"] = "<size=19>VOTED</size>",
                ["UI_TEXT_MUTE_MENU_BTN"] = "<size=14>MUTED PLAYERS</size>",

                ["UI_TEXT_ADMIN_PANEL"] = "<size=14><b>ADMIN PANEL</b></size>",
                ["UI_TEXT_MODER_PANEL"] = "<size=14><b>MOD PANEL</b></size>",
                ["UI_TEXT_ADMIN_PANEL_MUTE_CHAT_ALL"] = "<size=14>DISABLE CHAT</size>",
                ["UI_TEXT_ADMIN_PANEL_UNMUTE_CHAT_ALL"] = "<size=14>ENABLE CHAT</size>",
                ["UI_TEXT_ADMIN_PANEL_MUTE_VOICE_ALL"] = "<size=14>DISABLE VOICE</size>",
                ["UI_TEXT_ADMIN_PANEL_UNMUTE_VOICE_ALL"] = "<size=14>ENABLE VOICE</size>",

                ["UI_ALERT_TITLE"] = "<size=18><b>ATTENTION PLEASE</b></size>",

                ["UI_TITLE_NEW_PREFIX_ELEMENT"] = "<size=16><b>CHANGE TAG</b></size>",
                ["UI_TITLE_NEW_NICK_COLOR_ELEMENT"] = "<size=16><b>CHANGE NICKNAME COLOR</b></size>",
                ["UI_TITLE_NEW_MESSAGE_COLOR_ELEMENT"] = "<size=16><b>CHANGE TEXT COLOR</b></size>",
                ["UI_TITLE_NEW_MESSAGE_RANK_ELEMENT"] = "<size=16><b>CHANGER RANK</b></size>",

                ["FUNC_MESSAGE_MUTE_CHAT"] = "{0} muted {1} for {2}\nReason : {3}",
                ["FUNC_MESSAGE_UNMUTE_CHAT"] = "{0} unmuted {1}",
                ["FUNC_MESSAGE_MUTE_VOICE"] = "{0} muted voice to {1} for {2}\nReason : {3}",
                ["FUNC_MESSAGE_UNMUTE_VOICE"] = "{0} unmuted voice to {1}",
                ["FUNC_MESSAGE_MUTE_ALL_CHAT"] = "Chat disabled",
                ["FUNC_MESSAGE_UNMUTE_ALL_CHAT"] = "Chat enabled",
                ["FUNC_MESSAGE_MUTE_ALL_VOICE"] = "Voice chat disabled",
                ["FUNC_MESSAGE_UNMUTE_ALL_VOICE"] = "Voice chat enabled",

                ["FUNC_MESSAGE_ISMUTED_TRUE"] = "You can not send the messages {0}\nYou are muted",
                ["FUNC_MESSAGE_NO_ARG_BROADCAST"] = "You can not send an empty broadcast message!",

                ["UI_MUTE_PANEL_TITLE"] = "<size=20><b>MUTE PANEL</b></size>",
                ["UI_MUTE_PANEL_TITLE_ACTION"] = "<size=15>CHOOSE PLAYER OR SEARCH BY NICKNAME</size>",
                ["UI_MUTE_PANEL_TITLE_HELPS_LOCK"] = "<size=13><b>- PLAYER HAS DISABLED CHAT OR VOICECHAT</b></size>",
                ["UI_MUTE_PANEL_TITLE_HELPS_UNLOCK"] = "<size=13><b>- PLAYER HAS ENABLED CHAT OR VOICECHAT</b></size>",

                ["UI_MUTE_TAKE_ACTION_PANEL"] = "<size=18><b>SELECT\nACTION</b></size>",
                ["UI_MUTE_TAKE_ACTION_CHAT"] = "<size=12>MUTE\nCHAT</size>",
                ["UI_MUTE_TAKE_ACTION_CHAT_UNMUTE"] = "<size=12>UNMUTE\nCHAT</size>",
                ["UI_MUTE_TAKE_ACTION_VOICE"] = "<size=12>MUTE\nVOICE</size>",
                ["UI_MUTE_TAKE_ACTION_VOICE_UNMUTE"] = "<size=12>UNMUTE\nVOICE</size>",

                ["UI_MUTE_TAKE_REASON_TITLE"] = "<size=18><b>CHOOSE\nREASON</b></size>",

                ["COMMAND_NOT_PERMISSION"] = "You dont have permissions to use this command",
                ["COMMAND_RENAME_NOTARG"] = "For rename use : /rename New nickname",
                ["COMMAND_RENAME_SUCCES"] = "You have successful changed your name to {0}",

                ["COMMAND_PM_NOTARG"] = "To send pm use : /pm Nickname Message",
                ["COMMAND_PM_NOT_NULL_MSG"] = "Message is empty!",
                ["COMMAND_PM_NOT_USER"] = "User not found or offline",
                ["COMMAND_PM_SUCCESS"] = "Your private message sent successful\nMessage : {0}",
                ["COMMAND_PM_SEND_MSG"] = "Message from {0}\n{1}",

                ["COMMAND_R_NOTARG"] = "For reply use : /r Message",
                ["COMMAND_R_NOTMSG"] = "You dont have any private conversations yet!",

                ["FLOODERS_MESSAGE"] = "You're typing too fast! Please Wait {0} seconds",

                ["PREFIX_SETUP"] = "You have successfully removed the prefix {0}, it is already activated and installed",
                ["COLOR_CHAT_SETUP"] = "You have successfully picked up the <color={0}>chat color</color>, it is already activated and installed",
                ["COLOR_NICK_SETUP"] = "You have successfully taken the <color={0}>nickname color</color>, it is already activated and installed",

                ["PREFIX_RETURNRED"] = "Your prefix {0} expired, it was reset automatically",
                ["COLOR_CHAT_RETURNRED"] = "Action of your <color={0}>color chat</color> over, it is reset automatically",
                ["COLOR_NICK_RETURNRED"] = "Action of your <color={0}>color nick</color> over, it is reset automatically",

                ["WELCOME_PLAYER"] = "{0} came online",
                ["LEAVE_PLAYER"] = "{0} left",
                ["WELCOME_PLAYER_WORLD"] = "{0} came online. Country: {1}",
                ["LEAVE_PLAYER_REASON"] = "{0} left. Reason: {1}",

                ["IGNORE_ON_PLAYER"] = "You added {0} in black list",
                ["IGNORE_OFF_PLAYER"] = "You removed {0} from black list",
                ["IGNORE_NO_PM"] = "This player added you in black list. Your message has not been delivered.",
                ["IGNORE_NO_PM_ME"] = "You added this player in black list. Your message has not been delivered.",
                ["INGORE_NOTARG"] = "To ignore a player use : /ignore nickname",

                ["DISCORD_SEND_LOG_CHAT"] = "Player : {0}({1})\nFiltred message : {2}\nMessage : {3}",
                ["DISCORD_SEND_LOG_MUTE"] = "{0}({1}) give mute chat\nSuspect : {2}({3})\nReason : {4}",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE_ONE"] = "<size=30><b>НАСТРОЙКА ЧАТА</b></size>",
                ["TITLE_TWO"] = "<size=16><b>ВЫБЕРИТЕ ДЕЙСТВИЕ</b></size>",
                ["UI_CLOSE_BTN"] = "<size=20>ЗАКРЫТЬ</size>",

                ["UI_TEXT_PREFIX"] = "<size=23>ПРЕФИКС</size>",
                ["UI_TEXT_COLOR_NICK"] = "<size=23>НИК</size>",
                ["UI_TEXT_COLOR_MSG"] = "<size=23>ТЕКСТ</size>",
                ["UI_TEXT_RANK"] = "<size=23>РАНГ</size>",
                ["UI_TEXT_MUTE_MENU_BTN"] = "<size=23>МУТЫ</size>",

                ["UI_TEXT_ADMIN_PANEL"] = "<size=17><b>ПАНЕЛЬ\nАДМИНИСТРАТОРА</b></size>",
                ["UI_TEXT_MODER_PANEL"] = "<size=17><b>ПАНЕЛЬ\nМОДЕРАТОРА</b></size>",
                ["UI_TEXT_ADMIN_PANEL_MUTE_CHAT_ALL"] = "<size=14>ВЫКЛЮЧИТЬ ЧАТ</size>",
                ["UI_TEXT_ADMIN_PANEL_UNMUTE_CHAT_ALL"] = "<size=14>ВКЛЮЧИТЬ ЧАТ</size>",
                ["UI_TEXT_ADMIN_PANEL_MUTE_VOICE_ALL"] = "<size=14>ВЫКЛЮЧИТЬ ГОЛОС</size>",
                ["UI_TEXT_ADMIN_PANEL_UNMUTE_VOICE_ALL"] = "<size=14>ВКЛЮЧИТЬ ГОЛОС</size>",

                ["UI_ALERT_TITLE"] = "<size=18><b>МИНУТОЧКУ ВНИМАНИЯ</b></size>",

                ["UI_TITLE_NEW_PREFIX_ELEMENT"] = "<size=16><b>ВЫБЕРИТЕ ПРЕФИКС</b></size>",
                ["UI_TITLE_NEW_NICK_COLOR_ELEMENT"] = "<size=16><b>ВЫБЕРИТЕ ЦВЕТ НИКА</b></size>",
                ["UI_TITLE_NEW_MESSAGE_COLOR_ELEMENT"] = "<size=16><b>ВЫБЕРИТЕ ЦВЕТ ТЕКСТА</b></size>",
                ["UI_TITLE_NEW_MESSAGE_RANK_ELEMENT"] = "<size=16><b>ВЫБЕРИТЕ СЕБЕ РАНГ</b></size>",

                ["FUNC_MESSAGE_MUTE_CHAT"] = "{0} заблокировал чат игроку {1} на {2}\nПричина : {3}",
                ["FUNC_MESSAGE_UNMUTE_CHAT"] = "{0} разблокировал чат игроку {1}",
                ["FUNC_MESSAGE_MUTE_VOICE"] = "{0} заблокировал голос игроку {1} на {2}\nПричина : {3}",
                ["FUNC_MESSAGE_UNMUTE_VOICE"] = "{0} разблокировал голос игроку {1}",
                ["FUNC_MESSAGE_MUTE_ALL_CHAT"] = "Всем игрокам был заблокирован чат",
                ["FUNC_MESSAGE_UNMUTE_ALL_CHAT"] = "Всем игрокам был разблокирован чат",
                ["FUNC_MESSAGE_MUTE_ALL_VOICE"] = "Всем игрокам был заблокирован голос",
                ["FUNC_MESSAGE_UNMUTE_ALL_VOICE"] = "Всем игрокам был разблокирован голос",

                ["FUNC_MESSAGE_ISMUTED_TRUE"] = "Вы не можете отправлять сообщения еще {0}\nВаш чат заблокирован",
                ["FUNC_MESSAGE_NO_ARG_BROADCAST"] = "Вы не можете отправлять пустое сообщение в оповещение!",

                ["UI_MUTE_PANEL_TITLE"] = "<size=20><b>ПАНЕЛЬ УПРАВЛЕНИЯ БЛОКИРОВКАМИ ЧАТА</b></size>",
                ["UI_MUTE_PANEL_TITLE_ACTION"] = "<size=15>ВЫБЕРИТЕ ИГРОКА ИЛИ ВВЕДИТЕ НИК В ПОИСКЕ</size>",
                ["UI_MUTE_PANEL_TITLE_HELPS_LOCK"] = "<size=13><b>- У ИГРОКА ЗАБЛОКИРОВАН ЧАТ ИЛИ ГОЛОС</b></size>",
                ["UI_MUTE_PANEL_TITLE_HELPS_UNLOCK"] = "<size=13><b>- У ИГРОКА РАЗБЛОКИРОВАН ЧАТ ИЛИ ГОЛОС</b></size>",

                ["UI_MUTE_TAKE_ACTION_PANEL"] = "<size=18><b>ВЫБЕРИТЕ\nДЕЙСТВИЕ</b></size>",
                ["UI_MUTE_TAKE_ACTION_CHAT"] = "<size=12>ЗАБЛОКИРОВАТЬ\nЧАТ</size>",
                ["UI_MUTE_TAKE_ACTION_CHAT_UNMUTE"] = "<size=12>РАЗБЛОКИРОВАТЬ\nЧАТ</size>",
                ["UI_MUTE_TAKE_ACTION_VOICE"] = "<size=12>ЗАБЛОКИРОВАТЬ\nГОЛОС</size>",
                ["UI_MUTE_TAKE_ACTION_VOICE_UNMUTE"] = "<size=12>РАЗБЛОКИРОВАТЬ\nГОЛОС</size>",

                ["UI_MUTE_TAKE_REASON_TITLE"] = "<size=18><b>ВЫБЕРИТЕ\nПРИЧИНУ</b></size>",

                ["COMMAND_NOT_PERMISSION"] = "У вас недостаточно прав для данной команды",
                ["COMMAND_RENAME_NOTARG"] = "Используйте команду так : /rename Новый Ник",
                ["COMMAND_RENAME_SUCCES"] = "Вы успешно изменили ник на {0}",

                ["COMMAND_PM_NOTARG"] = "Используйте команду так : /pm Ник Игрока Сообщение",
                ["COMMAND_PM_NOT_NULL_MSG"] = "Вы не можете отправлять пустое сообщение",
                ["COMMAND_PM_NOT_USER"] = "Игрок не найден или не в сети",
                ["COMMAND_PM_SUCCESS"] = "Ваше сообщение успешно доставлено\nСообщение : {0}",
                ["COMMAND_PM_SEND_MSG"] = "Сообщение от {0}\n{1}",

                ["COMMAND_R_NOTARG"] = "Используйте команду так : /r Сообщение",
                ["COMMAND_R_NOTMSG"] = "Вам или вы ещё не писали игроку в личные сообщения!",

                ["FLOODERS_MESSAGE"] = "Вы пишите слишком быстро! Подождите {0} секунд",

                ["PREFIX_SETUP"] = "Вы успешно забрали префикс {0}, он уже активирован и установлен",
                ["COLOR_CHAT_SETUP"] = "Вы успешно забрали <color={0}>цвет чата</color>, он уже активирован и установлен",
                ["COLOR_NICK_SETUP"] = "Вы успешно забрали <color={0}>цвет ника</color>, он уже активирован и установлен",

                ["PREFIX_RETURNRED"] = "Действие вашего префикса {0} окончено, он сброшен автоматически",
                ["COLOR_CHAT_RETURNRED"] = "Действие вашего <color={0}>цвета чата</color> окончено, он сброшен автоматически",
                ["COLOR_NICK_RETURNRED"] = "Действие вашего префикса <color={0}>цвет чата</color> окончено, он сброшен автоматически",

                ["WELCOME_PLAYER"] = "{0} зашел на сервер",
                ["LEAVE_PLAYER"] = "{0} вышел с сервера",
                ["WELCOME_PLAYER_WORLD"] = "{0} зашел на сервер.Из {1}",
                ["LEAVE_PLAYER_REASON"] = "{0} вышел с сервера.Причина {1}",

                ["IGNORE_ON_PLAYER"] = "Вы добавили игрока {0} в черный список",
                ["IGNORE_OFF_PLAYER"] = "Вы убрали игрока {0} из черного списка",
                ["IGNORE_NO_PM"] = "Данный игрок добавил вас в ЧС,ваше сообщение не будет доставлено",
                ["IGNORE_NO_PM_ME"] = "Вы добавили данного игрока в ЧС,ваше сообщение не будет доставлено",
                ["INGORE_NOTARG"] = "Используйте команду так : /ignore Ник Игрока",

                ["DISCORD_SEND_LOG_CHAT"] = "Игрок : {0}({1})\nФильтрованное сообщение : {2}\nИзначальное сообщение : {3}",
                ["DISCORD_SEND_LOG_MUTE"] = "{0}({1}) выдал блокировку чата\nИгрок : {2}({3})\nПричина : {4}",
            }, this, "ru");
           
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion

        #region Helpers
        public void Log(string LoggedMessage) => LogToFile("IQChatLogs", LoggedMessage, this);
        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "дней", "дня", "день")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "часов", "часа", "час")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";

            if (time.Seconds != 0)
                result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";

            return result;
        }
        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }
        private BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var check in BasePlayer.activePlayerList.Where(x => x.displayName.ToLower().Contains(nameOrId.ToLower()) || x.UserIDString == nameOrId))
                return check;
            return null;
        }
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            sb.Clear();
            return sb.AppendFormat("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a).ToString();
        }
        #endregion

        #region ChatFunc

        public Dictionary<ulong, double> Flooders = new Dictionary<ulong, double>();
        void ReplyChat(Chat.ChatChannel channel, BasePlayer player, string OutMessage)
        {
            var MessageSetting = config.MessageSetting;
            if (MessageSetting.AntiSpamActivate)
                if (!permission.UserHasPermission(player.UserIDString, MessageSetting.PermAdminImmunitetAntispam))
                {
                    if (!Flooders.ContainsKey(player.userID))
                        Flooders.Add(player.userID, CurrentTime() + MessageSetting.FloodTime);
                    else
                        if (Flooders[player.userID] > CurrentTime())
                        {
                            ReplySystem(Chat.ChatChannel.Global, player, GetLang("FLOODERS_MESSAGE", player.UserIDString, Convert.ToInt32(Flooders[player.userID] - CurrentTime())));
                            return;
                        }

                    Flooders[player.userID] = MessageSetting.FloodTime + CurrentTime();
                }

            if (channel == Chat.ChatChannel.Global)
            {
                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    p.SendConsoleCommand("chat.add", new object[] { (int)channel, player.userID, OutMessage });
                }
                PrintToConsole(OutMessage);
            }
            if (channel == Chat.ChatChannel.Team)
            {
                RelationshipManager.PlayerTeam Team = RelationshipManager.Instance.FindTeam(player.currentTeam);
                if (Team == null) return;
                foreach (var FindPlayers in Team.members)
                {
                    BasePlayer TeamPlayer = BasePlayer.FindByID(FindPlayers);
                    if (TeamPlayer == null) continue;

                    TeamPlayer.SendConsoleCommand("chat.add", channel, player.userID, OutMessage);
                }
            }
        }

        void ReplySystem(Chat.ChatChannel channel, BasePlayer player, string Message,string CustomPrefix = "", string CustomAvatar = "", string CustomHex = "")
        {
            string Prefix = string.IsNullOrEmpty(CustomPrefix) ? config.MessageSetting.BroadcastTitle : CustomPrefix;
            ulong Avatar = string.IsNullOrEmpty(CustomAvatar) ? config.MessageSetting.Steam64IDAvatar : ulong.Parse(CustomAvatar);
            string Hex = string.IsNullOrEmpty(CustomHex) ? config.MessageSetting.BroadcastColor : CustomHex;

            string FormatMessage = $"{Prefix}<color={Hex}>{Message}</color>";
            if (channel == Chat.ChatChannel.Global)
                player.SendConsoleCommand("chat.add", channel, Avatar, FormatMessage);         
        }

        void ReplyBroadcast(string Message, string CustomPrefix = "", string CustomAvatar = "")
        {
            foreach(var p in BasePlayer.activePlayerList)
                ReplySystem(Chat.ChatChannel.Global, p, Message, CustomPrefix, CustomAvatar);
        }

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        #endregion

        #region API

        void API_SEND_PLAYER(BasePlayer player,string PlayerFormat, string Message, string Avatar, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var MessageSettings = config.MessageSetting;
            string OutMessage = Message;

            if (MessageSettings.FormatingMessage)
                OutMessage = $"{Message.ToLower().Substring(0, 1).ToUpper()}{Message.Remove(0, 1).ToLower()}";

            if (MessageSettings.UseBadWords)
                foreach (var DetectedMessage in OutMessage.Split(' '))
                    if (MessageSettings.BadWords.Contains(DetectedMessage.ToLower()))
                        OutMessage = OutMessage.Replace(DetectedMessage, MessageSettings.ReplaceBadWord);

            player.SendConsoleCommand("chat.add", channel, ulong.Parse(Avatar), $"{PlayerFormat}: {OutMessage}");
        }
        void API_SEND_PLAYER_PM(BasePlayer player, string DisplayName, string Message)
        {
            ReplySystem(Chat.ChatChannel.Global, player, GetLang("COMMAND_PM_SEND_MSG", player.UserIDString, DisplayName, Message));
            Effect.server.Run(config.MessageSetting.SoundPM, player.GetNetworkPosition());
        }
        void API_SEND_PLAYER_CONNECTED(BasePlayer player, string DisplayName, string country, string userID)
        {
            var Alert = config.AlertSettings;
            if (Alert.ConnectedAlert)
            {
                string Avatar = Alert.ConnectedAvatarUse ? userID : "";
                if (config.AlertSettings.ConnectedWorld)
                     ReplyBroadcast(GetLang("WELCOME_PLAYER_WORLD", player.UserIDString, DisplayName, country), "", Avatar);   
                else ReplyBroadcast(GetLang("WELCOME_PLAYER", player.UserIDString, DisplayName), "", Avatar);
            }
        }
        void API_SEND_PLAYER_DISCONNECTED(BasePlayer player, string DisplayName, string reason, string userID)
        {
            var Alert = config.AlertSettings;
            if (Alert.DisconnectedAlert)
            {
                string Avatar = Alert.ConnectedAvatarUse ? userID : "";
                string LangLeave = config.AlertSettings.DisconnectedReason ? GetLang("LEAVE_PLAYER_REASON",player.UserIDString, DisplayName, reason) : GetLang("LEAVE_PLAYER", player.UserIDString, DisplayName);
                ReplyBroadcast(LangLeave, "", Avatar);
            }
        }
        void API_ALERT(string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global, string CustomPrefix = "", string CustomAvatar = "", string CustomHex = "")
        {
            foreach (var p in BasePlayer.activePlayerList)
                ReplySystem(channel, p, Message, CustomPrefix, CustomAvatar, CustomHex);
        }
        void API_ALERT_PLAYER(BasePlayer player,string Message, string CustomPrefix = "", string CustomAvatar = "", string CustomHex = "") => ReplySystem(Chat.ChatChannel.Global, player, Message, CustomPrefix, CustomAvatar, CustomHex);
        void API_ALERT_PLAYER_UI(BasePlayer player, string Message) => UIAlert(player, Message);
        bool API_CHECK_MUTE_CHAT(ulong ID)
        {
            var DataPlayer = ChatSettingUser[ID];
            if (DataPlayer.MuteChatTime > CurrentTime())
                return true;
            else return false;
        }
        bool API_CHECK_VOICE_CHAT(ulong ID)
        {
            var DataPlayer = ChatSettingUser[ID];
            if (DataPlayer.MuteVoiceTime > CurrentTime())
                return true;
            else return false;
        }
        string API_GET_PREFIX(ulong ID)
        {
            var DataPlayer = ChatSettingUser[ID];
            return DataPlayer.ChatPrefix;
        }
        string API_GET_CHAT_COLOR(ulong ID)
        {
            var DataPlayer = ChatSettingUser[ID];
            return DataPlayer.MessageColor;
        }
        string API_GET_NICK_COLOR(ulong ID)
        {
            var DataPlayer = ChatSettingUser[ID];
            return DataPlayer.NickColor;
        }
        string API_GET_DEFUALT_PRFIX() => (string)config.AutoSetupSetting.ReturnDefaultSetting.PrefixDefault;
        string API_GET_DEFUALT_COLOR_NICK() => (string)config.AutoSetupSetting.ReturnDefaultSetting.NickDefault;
        string API_GET_DEFUALT_COLOR_CHAT() => (string)config.AutoSetupSetting.ReturnDefaultSetting.MessageDefault;
        #endregion
    }
}
