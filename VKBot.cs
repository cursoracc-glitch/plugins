using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("VKBot", "SkiTles", "1.7.4")]
    class VKBot : RustPlugin
    {
        //Данный плагин принадлежит группе vk.com/vkbotrust
        //Данный плагин предоставляется в существующей форме,
        //"как есть", без каких бы то ни было явных или
        //подразумеваемых гарантий, разработчик не несет
        //ответственность в случае его неправильного использования.

        [PluginReference]
        Plugin Duel;

        #region Variables
        static string apiver = "v=5.92";
        private bool OxideUpdateSended = false;
        private System.Random random = new System.Random();
        private bool NewWipe = false;
        JsonSerializerSettings jsonsettings;
        private List<string> allowedentity = new List<string>()
        {
            "door",
            "wall.window.bars.metal",
            "wall.window.bars.toptier",
            "wall.external",
            "gates.external.high",
            "floor.ladder",
            "embrasure",
            "floor.grill",
            "wall.frame.fence",
            "wall.frame.cell",
            "foundation",
            "floor.frame",
            "floor.triangle",
            "floor",
            "foundation.steps",
            "foundation.triangle",
            "roof",
            "stairs.l",
            "stairs.u",
            "wall.doorway",
            "wall.frame",
            "wall.half",
            "wall.low",
            "wall.window",
            "wall",
            "wall.external.high.stone"
        };
        List<string> ExplosiveList = new List<string>()
                {
                "explosive.satchel.deployed",
                "grenade.f1.deployed",
                "grenade.beancan.deployed",
                "explosive.timed.deployed"
                };
        private List<ulong> BDayPlayers = new List<ulong>();
        class GiftItem
        {
            public string shortname;
            public ulong skinid;
            public int count;
        }
        class ServerInfo
        {
            public string name;
            public string online;
            public string slots;
            public string sleepers;
            public string map;
        }
        private Dictionary<BasePlayer, DateTime> GiftsList = new Dictionary<BasePlayer, DateTime>();
        #endregion

        #region Config
        private ConfigData config;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Ключи VK API, ID группы")]
            public VKAPITokens VKAPIT { get; set; }
            [JsonProperty(PropertyName = "Настройки оповещений администраторов")]
            public AdminNotify AdmNotify { get; set; }
            [JsonProperty(PropertyName = "Настройки оповещений в беседу")]
            public ChatNotify ChNotify { get; set; }
            [JsonProperty(PropertyName = "Настройки статуса")]
            public StatusSettings StatusStg { get; set; }
            [JsonProperty(PropertyName = "Оповещения при вайпе")]
            public WipeSettings WipeStg { get; set; }
            [JsonProperty(PropertyName = "Награда за вступление в группу")]
            public GroupGifts GrGifts { get; set; }
            [JsonProperty(PropertyName = "Награда для именинников")]
            public BDayGiftSet BDayGift { get; set; }
            [JsonProperty(PropertyName = "Поддержка нескольких серверов")]
            public MultipleServersSettings MltServSet { get; set; }
            [JsonProperty(PropertyName = "Топ игроки вайпа и промо")]
            public TopWPlPromoSet TopWPlayersPromo { get; set; }
            [JsonProperty(PropertyName = "Настройки чат команд")]
            public CommandSettings CMDSet { get; set; }
            [JsonProperty(PropertyName = "Динамическая обложка группы")]
            public DynamicGroupLabelSettings DGLSet { get; set; }
            [JsonProperty(PropertyName = "Виджет сообщества")]
            public GroupWidgetSettings GrWgSet { get; set; }
            [JsonProperty(PropertyName = "Настройки GUI меню")]
            public GUISettings GUISet { get; set; }

            public class VKAPITokens
            {
                [JsonProperty(PropertyName = "VK Token группы (для сообщений)")]
                public string VKToken { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";
                [JsonProperty(PropertyName = "VK Token приложения (для записей на стене и статуса)")]
                public string VKTokenApp { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";
                [JsonProperty(PropertyName = "VKID группы")]
                public string GroupID { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";
            }
            public class AdminNotify
            {
                [JsonProperty(PropertyName = "VkID администраторов (пример /11111, 22222/)")]
                public string VkID { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";
                [JsonProperty(PropertyName = "Включить отправку сообщений администратору командой /report ?")]
                public bool SendReports { get; set; } = true;
                [JsonProperty(PropertyName = "Включить GUI для команды /report ?")]
                public bool GUIReports { get; set; } = false;
                [JsonProperty(PropertyName = "Очистка базы репортов при вайпе?")]
                public bool ReportsWipe { get; set; } = true;
                [JsonProperty(PropertyName = "Предупреждение о злоупотреблении функцией репортов")]
                public string ReportsNotify { get; set; } = "Наличие в тексте нецензурных выражений, оскорблений администрации или игроков сервера, а так же большое количество безсмысленных сообщений приведет к бану!";
                [JsonProperty(PropertyName = "Отправлять сообщение администратору о бане игрока?")]
                public bool UserBannedMsg { get; set; } = true;
                [JsonProperty(PropertyName = "Комментарий в обсуждения о бане игрока?")]
                public bool UserBannedTopic { get; set; } = false;
                [JsonProperty(PropertyName = "ID обсуждения")]
                public string BannedTopicID { get; set; } = "none";
                [JsonProperty(PropertyName = "Отправлять сообщение администратору о нерабочих плагинах?")]
                public bool PluginsCheckMsg { get; set; } = true;
                [JsonProperty(PropertyName = "Проверка обновлений Oxide")]
                public bool OxideCheckMsg { get; set; } = false;
            }
            public class ChatNotify
            {
                [JsonProperty(PropertyName = "VK Token приложения (лучше использовать отдельную страницу для получения токена)")]
                public string ChNotfToken { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";
                [JsonProperty(PropertyName = "ID беседы")]
                public string ChatID { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";
                [JsonProperty(PropertyName = "Включить отправку оповещений в беседу?")]
                public bool ChNotfEnabled { get; set; } = false;
                [JsonProperty(PropertyName = "Дополнительная отправка оповещений в личку администраторам?")]
                public bool AdmMsg { get; set; } = false;
                [JsonProperty(PropertyName = "Список оповещений отправляемых в беседу (доступно: reports, wipe, bans, plugins)")]
                public string ChNotfSet { get; set; } = "reports, wipe, bans, plugins";
            }
            public class StatusSettings
            {
                [JsonProperty(PropertyName = "Обновлять статус в группе? Если стоит /false/ статистика собираться не будет")]
                public bool UpdateStatus { get; set; } = true;
                [JsonProperty(PropertyName = "Вид статуса (1 - текущий сервер, 2 - список серверов, необходим Rust:IO на каждом сервере)")]
                public int StatusSet { get; set; } = 1;
                [JsonProperty(PropertyName = "Онлайн в статусе вида '125/200'")]
                public bool OnlWmaxslots { get; set; } = false;
                [JsonProperty(PropertyName = "Таймер обновления статуса (минуты)")]
                public int UpdateTimer { get; set; } = 30;
                [JsonProperty(PropertyName = "Формат статуса")]
                public string StatusText { get; set; } = "{usertext}. Сервер вайпнут: {wipedate}. Онлайн игроков: {onlinecounter}. Спящих: {sleepers}. Добыто дерева: {woodcounter}. Добыто серы: {sulfurecounter}. Выпущено ракет: {rocketscounter}. Время обновления: {updatetime}. Использовано взрывчатки: {explosivecounter}. Создано чертежей: {blueprintsconter}. {connect}";
                [JsonProperty(PropertyName = "Список счетчиков, которые будут отображаться в виде emoji")]
                public string EmojiCounterList { get; set; } = "onlinecounter, rocketscounter, blueprintsconter, explosivecounter, wipedate";
                [JsonProperty(PropertyName = "Ссылка на коннект сервера вида /connect 111.111.111.11:11111/")]
                public string Connecturl { get; set; } = "connect 111.111.111.11:11111";
                [JsonProperty(PropertyName = "Текст для статуса")]
                public string StatusUT { get; set; } = "Сервер 1";
            }
            public class WipeSettings
            {
                [JsonProperty(PropertyName = "Отправлять пост в группу после вайпа?")]
                public bool WPostB { get; set; } = false;
                [JsonProperty(PropertyName = "Текст поста о вайпе")]
                public string WPostMsg { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";
                [JsonProperty(PropertyName = "Добавить изображение к посту о вайпе?")]
                public bool WPostAttB { get; set; } = false;
                [JsonProperty(PropertyName = "Ссылка на изображение к посту о вайпе вида 'photo-1_265827614' (изображение должно быть в альбоме группы)")]
                public string WPostAtt { get; set; } = "photo-1_265827614";
                [JsonProperty(PropertyName = "Отправлять сообщение администратору о вайпе?")]
                public bool WPostMsgAdmin { get; set; } = true;
                [JsonProperty(PropertyName = "Отправлять игрокам сообщение о вайпе автоматически?")]
                public bool WMsgPlayers { get; set; } = false;
                [JsonProperty(PropertyName = "Текст сообщения игрокам о вайпе (сообщение отправляется только тем кто подписался командой /vk wipealerts)")]
                public string WMsgText { get; set; } = "Сервер вайпнут! Залетай скорее!";
                [JsonProperty(PropertyName = "Игнорировать команду /vk wipealerts? (если включено, сообщение о вайпе будет отправляться всем)")]
                public bool WCMDIgnore { get; set; } = false;
                [JsonProperty(PropertyName = "Смена названия группы после вайпа")]
                public bool GrNameChange { get; set; } = false;
                [JsonProperty(PropertyName = "Название группы (переменная {wipedate} отображает дату последнего вайпа)")]
                public string GrName { get; set; } = "ServerName | WIPE {wipedate}";
            }
            public class GroupGifts
            {
                [JsonProperty(PropertyName = "Выдавать подарок игроку за вступление в группу ВК?")]
                public bool VKGroupGifts { get; set; } = true;
                [JsonProperty(PropertyName = "Подарок за вступление в группу (команда, если стоит none выдаются предметы из файла data/VKBot.json). Пример: grantperm {steamid} vkraidalert.allow 7d")]
                public string VKGroupGiftCMD { get; set; } = "none";
                [JsonProperty(PropertyName = "Описание команды")]
                public string GiftCMDdesc { get; set; } = "Оповещения о рейде на 7 дней";
                [JsonProperty(PropertyName = "Ссылка на группу ВК")]
                public string VKGroupUrl { get; set; } = "vk.com/1234";
                [JsonProperty(PropertyName = "Оповещения в общий чат о получении награды")]
                public bool GiftsBool { get; set; } = true;
                [JsonProperty(PropertyName = "Включить оповещения для игроков не получивших награду за вступление в группу?")]
                public bool VKGGNotify { get; set; } = true;
                [JsonProperty(PropertyName = "Интервал оповещений для игроков не получивших награду за вступление в группу (в минутах)")]
                public int VKGGTimer { get; set; } = 30;
                [JsonProperty(PropertyName = "Выдавать награду каждый вайп?")]
                public bool GiftsWipe { get; set; } = true;
            }
            public class BDayGiftSet
            {
                [JsonProperty(PropertyName = "Включить награду для именинников?")]
                public bool BDayEnabled { get; set; } = true;
                [JsonProperty(PropertyName = "Группа для именинников")]
                public string BDayGroup { get; set; } = "bdaygroup";
                [JsonProperty(PropertyName = "Оповещения в общий чат о именинниках")]
                public bool BDayNotify { get; set; } = false;
            }
            public class MultipleServersSettings
            {
                [JsonProperty(PropertyName = "Включить поддержку несколько серверов?")]
                public bool MSSEnable { get; set; } = false;
                [JsonProperty(PropertyName = "Номер сервера")]
                public int ServerNumber { get; set; } = 1;
                [JsonProperty(PropertyName = "Сервер 1 IP:PORT (пример: 111.111.111.111:28015)")]
                public string Server1ip { get; set; } = "none";
                [JsonProperty(PropertyName = "Название сервера 1 (если стоит none, используется номер)")]
                public string Server1name { get; set; } = "none";
                [JsonProperty(PropertyName = "Сервер 2 IP:PORT (пример: 111.111.111.111:28015)")]
                public string Server2ip { get; set; } = "none";
                [JsonProperty(PropertyName = "Название сервера 2 (если стоит none, используется номер)")]
                public string Server2name { get; set; } = "none";
                [JsonProperty(PropertyName = "Сервер 3 IP:PORT (пример: 111.111.111.111:28015)")]
                public string Server3ip { get; set; } = "none";
                [JsonProperty(PropertyName = "Название сервера 3 (если стоит none, используется номер)")]
                public string Server3name { get; set; } = "none";
                [JsonProperty(PropertyName = "Сервер 4 IP:PORT (пример: 111.111.111.111:28015)")]
                public string Server4ip { get; set; } = "none";
                [JsonProperty(PropertyName = "Название сервера 4 (если стоит none, используется номер)")]
                public string Server4name { get; set; } = "none";
                [JsonProperty(PropertyName = "Сервер 5 IP:PORT (пример: 111.111.111.111:28015)")]
                public string Server5ip { get; set; } = "none";
                [JsonProperty(PropertyName = "Название сервера 5 (если стоит none, используется номер)")]
                public string Server5name { get; set; } = "none";
                [JsonProperty(PropertyName = "Онлайн в emoji?")]
                public bool EmojiStatus { get; set; } = true;
            }
            public class TopWPlPromoSet
            {
                [JsonProperty(PropertyName = "Включить топ игроков вайпа")]
                public bool TopWPlEnabled { get; set; } = true;
                [JsonProperty(PropertyName = "Включить отправку промо кодов за топ?")]
                public bool TopPlPromoGift { get; set; } = false;
                [JsonProperty(PropertyName = "Пост на стене группы о топ игроках вайпа")]
                public bool TopPlPost { get; set; } = true;
                [JsonProperty(PropertyName = "Ссылка на изображение к посту вида 'photo-1_265827614' (изображение должно быть в альбоме группы), оставить 'none' если не нужно")]
                public string TopPlPostAtt { get; set; } = "none";
                [JsonProperty(PropertyName = "Промо для топ рэйдера")]
                public string TopRaiderPromo { get; set; } = "topraider";
                [JsonProperty(PropertyName = "Ссылка на изображение к сообщению топ рейдеру вида 'photo-1_265827614' (изображение должно быть в альбоме группы), оставить 'none' если не нужно")]
                public string TopRaiderPromoAtt { get; set; } = "none";
                [JsonProperty(PropertyName = "Промо для топ килера")]
                public string TopKillerPromo { get; set; } = "topkiller";
                [JsonProperty(PropertyName = "Ссылка на изображение к сообщению топ киллеру вида 'photo-1_265827614' (изображение должно быть в альбоме группы), оставить 'none' если не нужно")]
                public string TopKillerPromoAtt { get; set; } = "none";
                [JsonProperty(PropertyName = "Промо для топ фармера")]
                public string TopFarmerPromo { get; set; } = "topfarmer";
                [JsonProperty(PropertyName = "Ссылка на изображение к сообщению топ фармеру вида 'photo-1_265827614' (изображение должно быть в альбоме группы), оставить 'none' если не нужно")]
                public string TopFarmerPromoAtt { get; set; } = "none";
                [JsonProperty(PropertyName = "Ссылка на донат магазин")]
                public string StoreUrl { get; set; } = "server.gamestores.ru";
                [JsonProperty(PropertyName = "Автоматическая генерация промокодов после вайпа")]
                public bool GenRandomPromo { get; set; } = false;
            }
            public class CommandSettings
            {
                [JsonProperty(PropertyName = "Команда отправки сообщения администратору")]
                public string CMDreport { get; set; } = "report";
            }
            public class DynamicGroupLabelSettings
            {
                [JsonProperty(PropertyName = "Включить динамическую обложку?")]
                public bool DLEnable { get; set; } = false;
                [JsonProperty(PropertyName = "Ссылка на скрипт обновления")]
                public string DLUrl { get; set; } = "none";
                [JsonProperty(PropertyName = "Таймер обновления (в минутах)")]
                public int DLTimer { get; set; } = 10;
                [JsonProperty(PropertyName = "Обложка с онлайном нескольких серверов (все настройки ниже игнорируются)")]
                public bool DLMSEnable { get; set; } = false;
                [JsonProperty(PropertyName = "Текст блока 1 (доступны все переменные как в статусе)")]
                public string DLText1 { get; set; } = "none";
                [JsonProperty(PropertyName = "Текст блока 2 (доступны все переменные как в статусе)")]
                public string DLText2 { get; set; } = "none";
                [JsonProperty(PropertyName = "Текст блока 3 (доступны все переменные как в статусе)")]
                public string DLText3 { get; set; } = "none";
                [JsonProperty(PropertyName = "Текст блока 4 (доступны все переменные как в статусе)")]
                public string DLText4 { get; set; } = "none";
                [JsonProperty(PropertyName = "Текст блока 5 (доступны все переменные как в статусе)")]
                public string DLText5 { get; set; } = "none";
                [JsonProperty(PropertyName = "Текст блока 6 (доступны все переменные как в статусе)")]
                public string DLText6 { get; set; } = "none";
                [JsonProperty(PropertyName = "Текст блока 7 (доступны все переменные как в статусе)")]
                public string DLText7 { get; set; } = "none";
                [JsonProperty(PropertyName = "Включить вывод топ игроков на обложку?")]
                public bool TPLabel { get; set; } = false;
            }
            public class GroupWidgetSettings
            {
                [JsonProperty(PropertyName = "Включить обновление виджета?")]
                public bool WgEnable { get; set; } = false;
                [JsonProperty(PropertyName = "Таймер обновления (минуты)")]
                public int UpdateTimer { get; set; } = 3;
                [JsonProperty(PropertyName = "Заголовок виджета")]
                public string WgTitle { get; set; } = "Мониторинг серверов";
                [JsonProperty(PropertyName = "Ключ приложения для работы с виджетом (Инструкция - https://goo.gl/LpZujf)")]
                public string WgToken { get; set; } = "none";
                [JsonProperty(PropertyName = "Текст дополнительной ссылки (если стоит none, не используется)")]
                public string URLTitle { get; set; } = "none";
                [JsonProperty(PropertyName = "Дополнительная ссылка (разрешены только vk.com ссылки)")]
                public string URL { get; set; } = "none";
            }
            public class GUISettings
            {
                [JsonProperty(PropertyName = "Ссылка на логотип сервера")]
                public string Logo { get; set; } = "https://i.imgur.com/QNZykaS.png";
                [JsonProperty(PropertyName = "Позиция GUI AnchorMin (дефолт 0.347 0.218)")]
                public string AnchorMin { get; set; } = "0.347 0.218";
                [JsonProperty(PropertyName = "Позиция GUI AnchorMax (дефолт 0.643 0.782)")]
                public string AnchorMax { get; set; } = "0.643 0.782";
                [JsonProperty(PropertyName = "Цвет фона меню")]
                public string BgColor { get; set; } = "#00000099";
                [JsonProperty(PropertyName = "Цвет кнопки ЗАКРЫТЬ")]
                public string BCloseColor { get; set; } = "#DB0000ff";
                [JsonProperty(PropertyName = "Цвет кнопки ПОЛУЧИТЬ КОД")]
                public string BSendColor { get; set; } = "#1FEF00ff";
                [JsonProperty(PropertyName = "Цвет остальных кнопок")]
                public string BMenuColor { get; set; } = "#494949ff";
            }
        }
        private void LoadVariables()
        {
            bool changed = false;
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            config = Config.ReadObject<ConfigData>();
            if (config.AdmNotify == null) { config.AdmNotify = new ConfigData.AdminNotify(); changed = true; }
            if (config.ChNotify == null) { config.ChNotify = new ConfigData.ChatNotify(); changed = true; }
            if (config.WipeStg == null) { config.WipeStg = new ConfigData.WipeSettings(); changed = true; }
            if (config.GrGifts == null) { config.GrGifts = new ConfigData.GroupGifts(); changed = true; }
            if (config.TopWPlayersPromo == null) { config.TopWPlayersPromo = new ConfigData.TopWPlPromoSet(); changed = true; }
            if (config.CMDSet == null) { config.CMDSet = new ConfigData.CommandSettings(); changed = true; }
            if (config.DGLSet == null) { config.DGLSet = new ConfigData.DynamicGroupLabelSettings(); changed = true; }
            if (config.GrWgSet == null) { config.GrWgSet = new ConfigData.GroupWidgetSettings(); changed = true; }
            if (config.GUISet == null) { config.GUISet = new ConfigData.GUISettings(); changed = true; }
            if (config.GUISet.Logo == "https://i.imgur.com/QNZykaS.png" && ConVar.Server.headerimage != string.Empty) config.GUISet.Logo = ConVar.Server.headerimage;
            Config.WriteObject(config, true);
            if (changed) PrintWarning("Конфигурационный файл обновлен. Добавлены новые настройки. Список изменений в плагине - vk.com/topic-30818042_36264027");
        }
        protected override void LoadDefaultConfig()
        {
            var configData = new ConfigData
            {
                VKAPIT = new ConfigData.VKAPITokens(),
                AdmNotify = new ConfigData.AdminNotify(),
                ChNotify = new ConfigData.ChatNotify(),
                StatusStg = new ConfigData.StatusSettings(),
                WipeStg = new ConfigData.WipeSettings(),
                GrGifts = new ConfigData.GroupGifts(),
                BDayGift = new ConfigData.BDayGiftSet(),
                MltServSet = new ConfigData.MultipleServersSettings(),
                TopWPlayersPromo = new ConfigData.TopWPlPromoSet(),
                CMDSet = new ConfigData.CommandSettings(),
                DGLSet = new ConfigData.DynamicGroupLabelSettings(),
                GrWgSet = new ConfigData.GroupWidgetSettings(),
                GUISet = new ConfigData.GUISettings()
            };
            Config.WriteObject(configData, true);
            PrintWarning("Поддержи разработчика! Вступи в группу vk.com/vkbotrust");
            PrintWarning("Инструкция по настройке плагина - goo.gl/xRkEUa");
        }
        #endregion

        #region Datastorage
        class DataStorageStats
        {
            public int WoodGath;
            public int SulfureGath;
            public int Rockets;
            public int Blueprints;
            public int Explosive;
            public int Reports;
            public List<GiftItem> Gifts;
            public DataStorageStats() { }
        }
        class DataStorageUsers
        {
            public Dictionary<ulong, VKUDATA> VKUsersData = new Dictionary<ulong, VKUDATA>();
            public DataStorageUsers() { }
        }
        class VKUDATA
        {
            public ulong UserID;
            public string Name;
            public string VkID;
            public int ConfirmCode;
            public bool Confirmed;
            public bool GiftRecived;
            public string LastRaidNotice;
            public bool WipeMsg;
            public string Bdate;
            public int Raids;
            public int Kills;
            public int Farm;
            public string LastSeen;
        }
        class DataStorageReports
        {
            public Dictionary<int, REPORT> VKReportsData = new Dictionary<int, REPORT>();
            public DataStorageReports() { }
        }
        class REPORT
        {
            public ulong UserID;
            public string Name;
            public string Text;
        }
        DataStorageStats statdata;
        DataStorageUsers usersdata;
        DataStorageReports reportsdata;
        private DynamicConfigFile VKBData;
        private DynamicConfigFile StatData;
        private DynamicConfigFile ReportsData;
        void LoadData()
        {
            try
            {
                statdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageStats>("VKBot");
                usersdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageUsers>("VKBotUsers");
                reportsdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageReports>("VKBotReports");
            }

            catch
            {
                statdata = new DataStorageStats();
                usersdata = new DataStorageUsers();
                reportsdata = new DataStorageReports();
            }
        }
        #endregion

        #region Oxidehooks
        private void OnServerInitialized()
        {
            LoadVariables();
            if (!config.AdmNotify.GUIReports) { Unsubscribe(nameof(OnServerCommand)); Unsubscribe(nameof(OnPlayerCommand)); }
            VKBData = Interface.Oxide.DataFileSystem.GetFile("VKBotUsers");
            StatData = Interface.Oxide.DataFileSystem.GetFile("VKBot");
            ReportsData = Interface.Oxide.DataFileSystem.GetFile("VKBotReports");
            LoadData();
            if (statdata.Gifts == null)
            {
                statdata.Gifts = new List<GiftItem>() { new GiftItem { shortname = "supply.signal", count = 1, skinid = 0 }, new GiftItem { shortname = "pookie.bear", count = 2, skinid = 0 } };
                StatData.WriteObject(statdata);
            }
            cmd.AddChatCommand(config.CMDSet.CMDreport, this, "SendReport");
            CheckAdminID();
            if (NewWipe) WipeFunctions();
            if (config.StatusStg.UpdateStatus)
            {
                if (config.StatusStg.StatusSet == 1) timer.Repeat(config.StatusStg.UpdateTimer * 60, 0, Update1ServerStatus);
                if (config.StatusStg.StatusSet == 2) timer.Repeat(config.StatusStg.UpdateTimer * 60, 0, () => { UpdateMultiServerStatus("status"); });
            }
            if (config.GrWgSet.WgEnable)
            {
                if (config.GrWgSet.WgToken == "none") PrintWarning($"Ошибка обновления виджета! В файле конфигурации не указан ключ! Инструкция - https://goo.gl/LpZujf");
                else timer.Repeat(config.GrWgSet.UpdateTimer * 60, 0, () => { UpdateMultiServerStatus("widget"); });
            }
            if (config.DGLSet.DLEnable && config.DGLSet.DLUrl != "none")
            {
                timer.Repeat(config.DGLSet.DLTimer * 60, 0, () => {
                    if (config.DGLSet.DLMSEnable) { UpdateMultiServerStatus("label"); }
                    else { UpdateVKLabel(); }
                });
            }
            if (config.GrGifts.VKGGNotify) timer.Repeat(config.GrGifts.VKGGTimer * 60, 0, GiftNotifier);
            if (config.AdmNotify.PluginsCheckMsg) CheckPlugins();
            if (config.AdmNotify.OxideCheckMsg) CheckOxideUpdate();
        }
        private void OnServerSave()
        {
            if (config.TopWPlayersPromo.TopWPlEnabled) VKBData.WriteObject(usersdata);
            if (config.StatusStg.UpdateStatus || config.DGLSet.DLEnable) StatData.WriteObject(statdata);
        }
        private void Init()
        {
            cmd.AddChatCommand("vk", this, "VKcommand");
            cmd.AddConsoleCommand("updatestatus", this, "UStatus");
            cmd.AddConsoleCommand("updatewidget", this, "UWidget");
            cmd.AddConsoleCommand("updatelabel", this, "ULabel");
            cmd.AddConsoleCommand("sendmsgadmin", this, "MsgAdmin");
            cmd.AddConsoleCommand("wipealerts", this, "WipeAlerts");
            cmd.AddConsoleCommand("userinfo", this, "GetUserInfo");
            cmd.AddConsoleCommand("report.answer", this, "ReportAnswer");
            cmd.AddConsoleCommand("report.list", this, "ReportList");
            cmd.AddConsoleCommand("report.wipe", this, "ReportClear");
            cmd.AddConsoleCommand("usersdata.update", this, "UpdateUsersData");
            jsonsettings = new JsonSerializerSettings();
            jsonsettings.Converters.Add(new KeyValuePairConverter());
        }
        private void Loaded() => LoadMessages();
        private void Unload()
        {
            if (config.AdmNotify.SendReports) ReportsData.WriteObject(reportsdata);
            if (config.StatusStg.UpdateStatus || config.DGLSet.DLEnable) StatData.WriteObject(statdata);
            if (config.TopWPlayersPromo.TopWPlEnabled) VKBData.WriteObject(usersdata);
            if (config.BDayGift.BDayEnabled && BDayPlayers.Count > 0)
            {
                foreach (var id in BDayPlayers) permission.RemoveUserGroup(id.ToString(), config.BDayGift.BDayGroup);
                BDayPlayers.Clear();
            }
            UnloadAllGUI();
        }
        private void OnNewSave(string filename) => NewWipe = true;
        private void OnPlayerInit(BasePlayer player)
        {
            if (usersdata.VKUsersData.ContainsKey(player.userID) && usersdata.VKUsersData[player.userID].Name != player.displayName)
            {
                usersdata.VKUsersData[player.userID].Name = player.displayName;
                VKBData.WriteObject(usersdata);
            }
            if (OpenReportUI.Contains(player)) OpenReportUI.Remove(player);
        }
        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!usersdata.VKUsersData.ContainsKey(player.userID)) return;
            if (!config.BDayGift.BDayEnabled) return;
            if (config.BDayGift.BDayEnabled && permission.GroupExists(config.BDayGift.BDayGroup))
            {
                if (permission.UserHasGroup(player.userID.ToString(), config.BDayGift.BDayGroup)) return;
                var bday = usersdata.VKUsersData[player.userID].Bdate;
                if (bday == null || bday == "noinfo") return;
                if (bday.Split('.').Length == 3) bday.Remove(bday.Length - 5, 5);
                if (bday == DateTime.Now.ToString("d.M", CultureInfo.InvariantCulture))
                {
                    permission.AddUserGroup(player.userID.ToString(), config.BDayGift.BDayGroup);
                    PrintToChat(player, string.Format(GetMsg("ПоздравлениеИгрока")));
                    Log("bday", $"Игрок {player.displayName} добавлен в группу {config.BDayGift.BDayGroup}");
                    BDayPlayers.Add(player.userID);
                    if (config.BDayGift.BDayNotify) Server.Broadcast(string.Format(GetMsg("ДеньРожденияИгрока"), player.displayName));
                }
            }
        }
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (config.BDayGift.BDayEnabled && permission.GroupExists(config.BDayGift.BDayGroup))
            {
                if (BDayPlayers.Contains(player.userID))
                {
                    permission.RemoveUserGroup(player.userID.ToString(), config.BDayGift.BDayGroup);
                    BDayPlayers.Remove(player.userID);
                    Log("bday", $"Игрок {player.displayName} удален из группы {config.BDayGift.BDayGroup}");
                }
            }
            if (OpenReportUI.Contains(player)) OpenReportUI.Remove(player);
            if (usersdata.VKUsersData.ContainsKey(player.userID)) { usersdata.VKUsersData[player.userID].LastSeen = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"); VKBData.WriteObject(usersdata); }
        }
        private void OnPlayerBanned(string name, ulong id, string address, string reason, string msg2 = null)
        {
            if (config.MltServSet.MSSEnable) msg2 = $"[Сервер {config.MltServSet.ServerNumber.ToString()}] Игрок {name} ({id}) был забанен на сервере. Причина: {reason}. Ссылка на профиль стим: steamcommunity.com/profiles/{id}/";
            else msg2 = $"Игрок {name} ({id}) был забанен на сервере. Причина: {reason}. Ссылка на профиль стим: steamcommunity.com/profiles/{id}/";
            if (config.AdmNotify.UserBannedTopic && config.AdmNotify.BannedTopicID != "null") AddComentToBoard(config.AdmNotify.BannedTopicID, msg2);
            if (config.AdmNotify.UserBannedMsg)
            {
                if (usersdata.VKUsersData.ContainsKey(id) && usersdata.VKUsersData[id].Confirmed) msg2 = msg2 + $" . Ссылка на профиль ВК: vk.com/id{usersdata.VKUsersData[id].VkID}";
                if (config.ChNotify.ChNotfEnabled && config.ChNotify.ChNotfSet.Contains("bans"))
                {
                    SendChatMessage(config.ChNotify.ChatID, msg2);
                    if (config.ChNotify.AdmMsg) SendVkMessage(config.AdmNotify.VkID, msg2);
                }
                else SendVkMessage(config.AdmNotify.VkID, msg2);
            }
        }
        #endregion

        #region Stats
        private void OnItemResearch(ResearchTable table, Item targetItem, BasePlayer player)
        {
            if (config.StatusStg.UpdateStatus || config.DGLSet.DLEnable) statdata.Blueprints++;
        }
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (config.StatusStg.UpdateStatus || config.DGLSet.DLEnable)
            {
                if (item.info.shortname == "wood") statdata.WoodGath = statdata.WoodGath + item.amount;
                if (item.info.shortname == "sulfur.ore") statdata.SulfureGath = statdata.SulfureGath + item.amount;
            }
            if (config.TopWPlayersPromo.TopWPlEnabled)
            {
                BasePlayer player = entity.ToPlayer();
                if (player == null) return;
                if (usersdata.VKUsersData.ContainsKey(player.userID)) usersdata.VKUsersData[player.userID].Farm = usersdata.VKUsersData[player.userID].Farm + item.amount;
            }
        }
        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if ((config.StatusStg.UpdateStatus || config.DGLSet.DLEnable) && item.info.shortname == "sulfur.ore") statdata.SulfureGath = statdata.SulfureGath + item.amount;
            if (config.TopWPlayersPromo.TopWPlEnabled && usersdata.VKUsersData.ContainsKey(player.userID)) usersdata.VKUsersData[player.userID].Farm = usersdata.VKUsersData[player.userID].Farm + item.amount;
        }
        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (config.StatusStg.UpdateStatus || config.DGLSet.DLEnable)
            {
                if (item.info.shortname == "wood") statdata.WoodGath = statdata.WoodGath + item.amount;
                if (item.info.shortname == "sulfur.ore") statdata.SulfureGath = statdata.SulfureGath + item.amount;
            }
            if (config.TopWPlayersPromo.TopWPlEnabled && usersdata.VKUsersData.ContainsKey(player.userID)) usersdata.VKUsersData[player.userID].Farm = usersdata.VKUsersData[player.userID].Farm + item.amount;
        }
        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (config.StatusStg.UpdateStatus || config.DGLSet.DLEnable) statdata.Rockets++;
        }
        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (config.StatusStg.UpdateStatus || config.DGLSet.DLEnable && ExplosiveList.Contains(entity.ShortPrefabName)) statdata.Explosive++;
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (config.TopWPlayersPromo.TopWPlEnabled)
            {
                if (entity.name.Contains("corpse")) return;
                if (hitInfo == null) return;
                var attacker = hitInfo.Initiator?.ToPlayer();
                if (attacker == null) return;
                if (entity is BasePlayer) CheckDeath(entity.ToPlayer(), hitInfo, attacker);
                if (entity is BaseEntity)
                {
                    if (hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Explosion && hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Heat && hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Bullet) return;
                    if (attacker.userID == entity.OwnerID) return;
                    BuildingBlock block = entity.GetComponent<BuildingBlock>();
                    if (block != null)
                    {
                        if (block.currentGrade.gradeBase.type.ToString() == "Twigs" || block.currentGrade.gradeBase.type.ToString() == "Wood") return;
                    }
                    else
                    {
                        bool ok = false;
                        foreach (var ent in allowedentity)
                        {
                            if (entity.LookupPrefab().name.Contains(ent)) ok = true;
                        }
                        if (!ok) return;
                    }
                    if (entity.OwnerID == 0) return;
                    if (usersdata.VKUsersData.ContainsKey(attacker.userID)) usersdata.VKUsersData[attacker.userID].Raids++;
                }
            }
        }
        private void CheckDeath(BasePlayer player, HitInfo info, BasePlayer attacker)
        {
            if (IsNPC(player)) return;
            if (!usersdata.VKUsersData.ContainsKey(attacker.userID)) return;
            if (!player.IsConnected) return;
            if (Duel && (bool)Duel?.Call("IsDuelPlayer", player)) return;
            usersdata.VKUsersData[attacker.userID].Kills++;
        }
        #endregion

        #region Wipe
        private void WipeFunctions()
        {
            if (config.StatusStg.UpdateStatus)
            {
                statdata.Blueprints = 0;
                statdata.Rockets = 0;
                statdata.SulfureGath = 0;
                statdata.WoodGath = 0;
                statdata.Explosive = 0;
                StatData.WriteObject(statdata);
                if (config.StatusStg.StatusSet == 1) Update1ServerStatus();
                if (config.StatusStg.StatusSet == 2) UpdateMultiServerStatus("status");
            }
            if (config.WipeStg.WPostMsgAdmin)
            {
                string msg2 = "[VKBot] Сервер ";
                if (config.MltServSet.MSSEnable) msg2 = msg2 + config.MltServSet.ServerNumber.ToString() + " ";
                if (ConVar.Server.levelurl != string.Empty) msg2 = msg2 + $"вайпнут. Установлена карта: {ConVar.Server.levelurl}.";
                else msg2 = msg2 + $"вайпнут. Установлена карта: {ConVar.Server.level}. Размер: {ConVar.Server.worldsize}. Сид: {ConVar.Server.seed}";
                if (config.ChNotify.ChNotfEnabled && config.ChNotify.ChNotfSet.Contains("wipe"))
                {
                    SendChatMessage(config.ChNotify.ChatID, msg2);
                    if (config.ChNotify.AdmMsg) SendVkMessage(config.AdmNotify.VkID, msg2);
                }
                else SendVkMessage(config.AdmNotify.VkID, msg2);
            }
            if (config.WipeStg.WPostB)
            {
                if (config.WipeStg.WPostAttB) SendVkWall($"{config.WipeStg.WPostMsg}&attachments={config.WipeStg.WPostAtt}");
                else SendVkWall($"{config.WipeStg.WPostMsg}");
            }
            if (config.GrGifts.GiftsWipe)
            {
                if (usersdata.VKUsersData.Count != 0)
                {
                    for (int i = 0; i < usersdata.VKUsersData.Count; i++)
                    {
                        usersdata.VKUsersData.ElementAt(i).Value.GiftRecived = false;
                    }
                    VKBData.WriteObject(usersdata);
                }
            }
            if (config.TopWPlayersPromo.TopWPlEnabled)
            {
                if (config.TopWPlayersPromo.TopPlPost || config.TopWPlayersPromo.TopPlPromoGift)
                {
                    SendPromoMsgsAndPost();
                    if (config.TopWPlayersPromo.GenRandomPromo) SetRandomPromo();
                }
                if (usersdata.VKUsersData.Count != 0)
                {
                    for (int i = 0; i < usersdata.VKUsersData.Count; i++)
                    {
                        usersdata.VKUsersData.ElementAt(i).Value.Farm = 0;
                        usersdata.VKUsersData.ElementAt(i).Value.Kills = 0;
                        usersdata.VKUsersData.ElementAt(i).Value.Raids = 0;
                    }
                    VKBData.WriteObject(usersdata);
                }
            }
            if (config.WipeStg.WMsgPlayers) WipeAlertsSend();
            if (config.AdmNotify.SendReports && config.AdmNotify.ReportsWipe)
            {
                reportsdata.VKReportsData.Clear();
                ReportsData.WriteObject(reportsdata);
                statdata.Reports = 0;
                StatData.WriteObject(statdata);
            }
            if (config.WipeStg.GrNameChange)
            {
                string wipedate = WipeDate();
                string text = config.WipeStg.GrName.Replace("{wipedate}", wipedate);
                webrequest.Enqueue("https://api.vk.com/method/groups.edit?group_id=" + config.VKAPIT.GroupID + "&title=" + text + "&" + apiver + "&access_token=" + config.VKAPIT.VKTokenApp, null, (code, response) =>
                {
                    var json = JObject.Parse(response);
                    string Result = (string)json["response"];
                    if (Result == "1") PrintWarning($"Новое имя группы - {text}");
                    else
                    {
                        PrintWarning("Ошибка смены имени группы. Логи - /oxide/logs/VKBot/");
                        Log("Errors", $"group title not changed. Error: {response}");
                    }
                }, this);
            }
        }
        private void WipeAlerts(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            WipeAlertsSend();
        }
        private void WipeAlertsSend()
        {
            List<string> UserList = new List<string>();
            string userlist = "";
            int usercount = 0;
            if (usersdata.VKUsersData.Count != 0)
            {
                for (int i = 0; i < usersdata.VKUsersData.Count; i++)
                {
                    if (config.WipeStg.WCMDIgnore || usersdata.VKUsersData.ElementAt(i).Value.WipeMsg)
                    {
                        if (!ServerUsers.BanListString().Contains(usersdata.VKUsersData.ElementAt(i).Value.UserID.ToString()))
                        {
                            if (usercount == 100)
                            {
                                UserList.Add(userlist);
                                userlist = "";
                                usercount = 0;
                            }
                            if (usercount > 0) userlist = userlist + ", ";
                            userlist = userlist + usersdata.VKUsersData.ElementAt(i).Value.VkID;
                            usercount++;
                        }
                    }
                }
            }
            if (userlist == "" && UserList.Count == 0) { PrintWarning($"Список адресатов рассылки о вайпе пуст."); return; }
            if (UserList.Count > 0)
            {
                foreach (var list in UserList) SendVkMessage(list, config.WipeStg.WMsgText);
            }
            SendVkMessage(userlist, config.WipeStg.WMsgText);
        }
        #endregion

        #region MainMethods
        private void UStatus(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            if (config.StatusStg.UpdateStatus)
            {
                if (config.StatusStg.StatusSet == 1) Update1ServerStatus();
                if (config.StatusStg.StatusSet == 2) UpdateMultiServerStatus("status");
            }
            else PrintWarning($"Функция обновления статуса отключена.");
        }
        private void UpdateUsersData(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            DeleteOldUsers(arg.Args?[0]);
        }
        private void UWidget(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            if (config.GrWgSet.WgEnable)
            {
                if (config.GrWgSet.WgToken == "none") { PrintWarning($"Ошибка! В файле конфигурации не указан ключ!"); return; }
                UpdateMultiServerStatus("widget");
            }
            else PrintWarning($"Функция обновления статуса отключена.");
        }
        private string PrepareStatus(string input, string target)
        {
            string text = input;
            string temp = "";
            temp = GetOnline();
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("onlinecounter")) temp = EmojiCounters(temp);
            if (input.Contains("{onlinecounter}")) text = text.Replace("{onlinecounter}", temp);
            temp = BasePlayer.sleepingPlayerList.Count.ToString();
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("sleepers")) temp = EmojiCounters(temp);
            if (input.Contains("{sleepers}")) text = text.Replace("{sleepers}", temp);
            temp = statdata.WoodGath.ToString();
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("woodcounter")) temp = EmojiCounters(temp);
            if (input.Contains("{woodcounter}")) text = text.Replace("{woodcounter}", temp);
            temp = statdata.SulfureGath.ToString();
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("sulfurecounter")) temp = EmojiCounters(temp);
            if (input.Contains("{sulfurecounter}")) text = text.Replace("{sulfurecounter}", temp);
            temp = statdata.Rockets.ToString();
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("rocketscounter")) temp = EmojiCounters(temp);
            if (input.Contains("{rocketscounter}")) text = text.Replace("{rocketscounter}", temp);
            temp = statdata.Blueprints.ToString();
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("blueprintsconter")) temp = EmojiCounters(temp);
            if (input.Contains("{blueprintsconter}")) text = text.Replace("{blueprintsconter}", temp);
            temp = statdata.Explosive.ToString();
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("explosivecounter")) temp = EmojiCounters(temp);
            if (input.Contains("{explosivecounter}")) text = text.Replace("{explosivecounter}", temp);
            temp = WipeDate();
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("wipedate")) temp = EmojiCounters(temp);
            if (input.Contains("{wipedate}")) text = text.Replace("{wipedate}", temp);
            temp = config.StatusStg.Connecturl;
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("connect")) temp = EmojiCounters(temp);
            if (input.Contains("{connect}")) text = text.Replace("{connect}", temp);
            temp = config.StatusStg.StatusUT;
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("usertext")) temp = EmojiCounters(temp);
            if (input.Contains("{usertext}")) text = text.Replace("{usertext}", temp);
            temp = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
            if (target == "status" && config.StatusStg.EmojiCounterList.Contains("updatetime")) temp = EmojiCounters(temp);
            if (input.Contains("{updatetime}")) text = text.Replace("{updatetime}", temp);
            return text;
        }
        private void SendReport(BasePlayer player, string cmd, string[] args)
        {
            if (config.AdmNotify.SendReports)
            {
                if (args.Length > 0) CreateReport(player, string.Join(" ", args.Skip(0).ToArray()));
                else
                {
                    if (config.AdmNotify.GUIReports) ReportGUI(player);
                    else { PrintToChat(player, string.Format(GetMsg("КомандаРепорт"), config.AdmNotify.ReportsNotify)); return; }
                }
            }
            else PrintToChat(player, string.Format(GetMsg("ФункцияОтключена")));
        }
        private void CheckReport(BasePlayer player, string[] text)
        {
            if (text != null && text.Count() < 2) return;
            ulong uid;
            if (ulong.TryParse(text[1], out uid))
            {
                var utarget = BasePlayer.FindByID(uid);
                if (utarget != null && text.Count() > 2) CreateReport(player, string.Join(" ", text.Skip(2).ToArray()), utarget);
                else CreateReport(player, string.Join(" ", text.Skip(1).ToArray()));
            }
            else CreateReport(player, string.Join(" ", text.Skip(1).ToArray()));
        }
        private void CreateReport(BasePlayer player, string text, BasePlayer target = null)
        {
            string reportplayer = "";
            if (target != null) reportplayer = reportplayer + "Жалоба на игрока " + target.displayName + " (" + "steamcommunity.com/profiles/" + target.userID + "/) ";
            string reporttext = "[VKBot]";
            statdata.Reports = statdata.Reports + 1;
            int reportid = statdata.Reports;
            StatData.WriteObject(statdata);
            if (config.MltServSet.MSSEnable) reporttext = reporttext + " [Сервер " + config.MltServSet.ServerNumber.ToString() + "]";
            reporttext = reporttext + " " + player.displayName + " " + "(" + player.UserIDString + ")";
            if (usersdata.VKUsersData.ContainsKey(player.userID))
            {
                if (usersdata.VKUsersData[player.userID].Confirmed) reporttext = reporttext + ". ВК: vk.com/id" + usersdata.VKUsersData[player.userID].VkID;
                else reporttext = reporttext + ". ВК: vk.com/id" + usersdata.VKUsersData[player.userID].VkID + " (не подтвержден)";
            }
            reporttext = reporttext + " ID репорта: " + reportid;
            reporttext = reporttext + reportplayer;
            reporttext = reporttext + ". Сообщение: " + text;
            if (config.ChNotify.ChNotfEnabled && config.ChNotify.ChNotfSet.Contains("reports"))
            {
                SendChatMessage(config.ChNotify.ChatID, reporttext);
                if (config.ChNotify.AdmMsg) SendVkMessage(config.AdmNotify.VkID, reporttext);
            }
            else SendVkMessage(config.AdmNotify.VkID, reporttext);
            reportsdata.VKReportsData.Add(reportid, new REPORT
            {
                UserID = player.userID,
                Name = player.displayName,
                Text = reportplayer + text
            });
            ReportsData.WriteObject(reportsdata);
            Log("Log", $"{player.displayName} ({player.userID}): написал администратору: {reporttext}");
            PrintToChat(player, string.Format(GetMsg("РепортОтправлен"), config.AdmNotify.ReportsNotify));
        }
        private void CheckVkUser(BasePlayer player, string url)
        {
            string Userid = null;
            string[] arr1 = url.Split('/');
            string vkname = arr1[arr1.Length - 1];
            webrequest.Enqueue("https://api.vk.com/method/users.get?user_ids=" + vkname + "&" + apiver + "&fields=bdate&access_token=" + config.VKAPIT.VKToken, null, (code, response) => {
                if (!response.Contains("error"))
                {
                    var json = JObject.Parse(response);
                    Userid = (string)json["response"][0]["id"];
                    string bdate = (string)json["response"][0]["bdate"] ?? "noinfo";
                    if (Userid != null) AddVKUser(player, Userid, bdate);
                    else PrintToChat(player, "Ошибка обработки вашей ссылки ВК, обратитесь к администратору.");
                }
                else
                {
                    PrintWarning($"Ошибка проверки ВК профиля игрока {player.displayName} ({player.userID}). URL - {url}");
                    Log("checkresponce", $"Ошибка проверки ВК профиля игрока {player.displayName} ({player.userID}). URL - {url}. Ответ сервера ВК: {response}");
                }
            }, this);
        }
        private void AddVKUser(BasePlayer player, string Userid, string bdate)
        {
            if (!usersdata.VKUsersData.ContainsKey(player.userID))
            {
                usersdata.VKUsersData.Add(player.userID, new VKUDATA()
                {
                    UserID = player.userID,
                    Name = player.displayName,
                    VkID = Userid,
                    ConfirmCode = random.Next(1, 9999999),
                    Confirmed = false,
                    GiftRecived = false,
                    Bdate = bdate,
                    Farm = 0,
                    Kills = 0,
                    Raids = 0,
                    LastSeen = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
                });
                VKBData.WriteObject(usersdata);
                SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player);
            }
            else
            {
                if (Userid == usersdata.VKUsersData[player.userID].VkID && usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, string.Format(GetMsg("ПрофильДобавленИПодтвержден"))); return; }
                if (Userid == usersdata.VKUsersData[player.userID].VkID && !usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, string.Format(GetMsg("ПрофильДобавлен"))); return; }
                usersdata.VKUsersData[player.userID].Name = player.displayName;
                usersdata.VKUsersData[player.userID].VkID = Userid;
                usersdata.VKUsersData[player.userID].Confirmed = false;
                usersdata.VKUsersData[player.userID].ConfirmCode = random.Next(1, 9999999);
                usersdata.VKUsersData[player.userID].Bdate = bdate;
                VKBData.WriteObject(usersdata);
                SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player);
            }
        }
        private void VKcommand(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "add")
                {
                    if (args.Length == 1) { PrintToChat(player, string.Format(GetMsg("ДоступныеКоманды"))); return; }
                    if (!args[1].Contains("vk.com/")) { PrintToChat(player, string.Format(GetMsg("НеправильнаяСсылка"))); return; }
                    CheckVkUser(player, args[1]);
                }
                if (args[0] == "confirm")
                {
                    if (args.Length >= 2)
                    {
                        if (usersdata.VKUsersData.ContainsKey(player.userID))
                        {
                            if (usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, string.Format(GetMsg("ПрофильДобавленИПодтвержден"))); return; }
                            if (args[1] == usersdata.VKUsersData[player.userID].ConfirmCode.ToString())
                            {
                                usersdata.VKUsersData[player.userID].Confirmed = true;
                                VKBData.WriteObject(usersdata);
                                PrintToChat(player, string.Format(GetMsg("ПрофильПодтвержден")));
                                if (config.GrGifts.VKGroupGifts) PrintToChat(player, string.Format(GetMsg("ОповещениеОПодарках"), config.GrGifts.VKGroupUrl));
                            }
                            else PrintToChat(player, string.Format(GetMsg("НеверныйКод")));
                        }
                        else PrintToChat(player, string.Format(GetMsg("ПрофильНеДобавлен")));
                    }
                    else
                    {
                        if (!usersdata.VKUsersData.ContainsKey(player.userID)) { PrintToChat(player, string.Format(GetMsg("ПрофильНеДобавлен"))); return; }
                        if (usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, string.Format(GetMsg("ПрофильДобавленИПодтвержден"))); return; }
                        SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player);
                    }
                }
                if (args[0] == "gift") FixedGifts(player);
                if (args[0] == "wipealerts") WAlert(player);
                if (args[0] != "add" && args[0] != "gift" && args[0] != "confirm")
                {
                    PrintToChat(player, string.Format(GetMsg("ДоступныеКоманды")));
                    if (config.GrGifts.VKGroupGifts) PrintToChat(player, string.Format(GetMsg("ОповещениеОПодарках"), config.GrGifts.VKGroupUrl));
                }
            }
            else StartVKBotMainGUI(player);
        }
        private void WAlert(BasePlayer player)
        {
            if (!usersdata.VKUsersData.ContainsKey(player.userID)) { PrintToChat(player, string.Format(GetMsg("ПрофильНеДобавлен"))); return; }
            if (!usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, string.Format(GetMsg("ПрофильНеПодтвержден"))); return; }
            if (config.WipeStg.WCMDIgnore) { PrintToChat(player, string.Format(GetMsg("АвтоОповещенияОвайпе"))); return; }
            if (usersdata.VKUsersData[player.userID].WipeMsg)
            {
                usersdata.VKUsersData[player.userID].WipeMsg = false;
                VKBData.WriteObject(usersdata);
                PrintToChat(player, string.Format(GetMsg("ПодпискаОтключена")));
            }
            else
            {
                usersdata.VKUsersData[player.userID].WipeMsg = true;
                VKBData.WriteObject(usersdata);
                PrintToChat(player, string.Format(GetMsg("ПодпискаВключена")));
            }
        }
        private void VKGift(BasePlayer player)
        {
            if (config.GrGifts.VKGroupGifts)
            {
                if (!usersdata.VKUsersData.ContainsKey(player.userID)) { PrintToChat(player, string.Format(GetMsg("ПрофильНеДобавлен"))); return; }
                if (!usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, string.Format(GetMsg("ПрофильНеПодтвержден"))); return; }
                if (usersdata.VKUsersData[player.userID].GiftRecived) { PrintToChat(player, string.Format(GetMsg("НаградаУжеПолучена"))); return; }
                webrequest.Enqueue($"https://api.vk.com/method/groups.isMember?group_id={config.VKAPIT.GroupID}&user_id={usersdata.VKUsersData[player.userID].VkID}&" + apiver + $"&access_token={config.VKAPIT.VKToken}", null, (code, response) => {
                    if (response == null || !response.Contains("response")) return;
                    var json = JObject.Parse(response);
                    if (json == null) return;
                    string Result = (string)json["response"];
                    if (Result == null) return;
                    GetGift(code, Result, player);
                }, this);

            }
            else PrintToChat(player, string.Format(GetMsg("ФункцияОтключена")));
        }
        private void GetGift(int code, string Result, BasePlayer player)
        {
            if (Result == "1")
            {
                if (config.GrGifts.VKGroupGiftCMD == "none")
                {
                    if ((24 - player.inventory.containerMain.itemList.Count) >= statdata.Gifts.Count)
                    {
                        usersdata.VKUsersData[player.userID].GiftRecived = true;
                        VKBData.WriteObject(usersdata);
                        PrintToChat(player, string.Format(GetMsg("НаградаПолучена")));
                        if (config.GrGifts.GiftsBool) Server.Broadcast(string.Format(GetMsg("ПолучилНаграду"), player.displayName, config.GrGifts.VKGroupUrl));
                        foreach (GiftItem gf in statdata.Gifts) { Item gift = ItemManager.CreateByName(gf.shortname, gf.count, gf.skinid); gift.MoveToContainer(player.inventory.containerMain, -1, false); }
                    }
                    else PrintToChat(player, string.Format(GetMsg("НетМеста")));
                }
                else
                {
                    string cmd = config.GrGifts.VKGroupGiftCMD.Replace("{steamid}", player.userID.ToString());
                    rust.RunServerCommand(cmd);
                    usersdata.VKUsersData[player.userID].GiftRecived = true;
                    VKBData.WriteObject(usersdata);
                    PrintToChat(player, string.Format(GetMsg("НаградаПолученаКоманда"), config.GrGifts.GiftCMDdesc));
                    if (config.GrGifts.GiftsBool) Server.Broadcast(string.Format(GetMsg("ПолучилНаграду"), player.displayName, config.GrGifts.VKGroupUrl));
                }
            }
            else PrintToChat(player, string.Format(GetMsg("НеВступилВГруппу"), config.GrGifts.VKGroupUrl));
        }
        private void GiftNotifier()
        {
            if (config.GrGifts.VKGroupGifts)
            {
                foreach (var pl in BasePlayer.activePlayerList)
                {
                    if (!usersdata.VKUsersData.ContainsKey(pl.userID)) PrintToChat(pl, string.Format(GetMsg("ОповещениеОПодарках"), config.GrGifts.VKGroupUrl));
                    else
                    {
                        if (!usersdata.VKUsersData[pl.userID].GiftRecived) PrintToChat(pl, string.Format(GetMsg("ОповещениеОПодарках"), config.GrGifts.VKGroupUrl));
                    }
                }
            }
        }
        void Update1ServerStatus()
        {
            string status = PrepareStatus(config.StatusStg.StatusText, "status");
            StatusCheck(status);
            SendVkStatus(status);
        }
        void UpdateMultiServerStatus(string target)
        {
            string text = "";
            string server1 = "";
            string server2 = "";
            string server3 = "";
            string server4 = "";
            string server5 = "";
            Dictionary<int, ServerInfo> SList = new Dictionary<int, ServerInfo>();
            if (config.MltServSet.Server1ip != "none")
            {
                var url = "http://" + config.MltServSet.Server1ip + "/status.json";
                webrequest.Enqueue(url, null, (code, response) => {
                    if (response != null || code == 200)
                    {

                        var jsonresponse3 = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
                        if (!(jsonresponse3 is Dictionary<string, object>) || jsonresponse3.Count == 0 || !jsonresponse3.ContainsKey("players") || !jsonresponse3.ContainsKey("maxplayers")) return;
                        if (target == "widget" && (!jsonresponse3.ContainsKey("sleepers") || !jsonresponse3.ContainsKey("level"))) return;
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus && target == "status") { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        string name = "1⃣: ";
                        if (target == "widget") name = "1: ";
                        if (config.MltServSet.Server1name != "none")
                        {
                            name = config.MltServSet.Server1name + " ";
                            if (target == "widget") name = config.MltServSet.Server1name;
                        }
                        server1 = name + online.ToString() + "/" + slots.ToString();
                        if (target == "widget")
                        {
                            SList.Add(1, new ServerInfo() { name = name, online = online, slots = slots, sleepers = jsonresponse3["sleepers"].ToString(), map = jsonresponse3["level"].ToString() });
                        }
                    }
                }, this);
            }
            if (config.MltServSet.Server2ip != "none")
            {
                var url = "http://" + config.MltServSet.Server2ip + "/status.json";
                webrequest.Enqueue(url, null, (code, response) => {
                    if (response != null || code == 200)
                    {

                        var jsonresponse3 = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
                        if (!(jsonresponse3 is Dictionary<string, object>) || jsonresponse3.Count == 0 || !jsonresponse3.ContainsKey("players") || !jsonresponse3.ContainsKey("maxplayers")) return;
                        if (target == "widget" && (!jsonresponse3.ContainsKey("sleepers") || !jsonresponse3.ContainsKey("level"))) return;
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus && target == "status") { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        string name = ", 2⃣: ";
                        if (target == "widget") name = "2:";
                        if (config.MltServSet.Server2name != "none")
                        {
                            name = ", " + config.MltServSet.Server2name + " ";
                            if (target == "widget") name = config.MltServSet.Server2name;
                        }
                        server2 = name + online.ToString() + "/" + slots.ToString();
                        if (target == "widget")
                        {
                            SList.Add(2, new ServerInfo() { name = name, online = online, slots = slots, sleepers = jsonresponse3["sleepers"].ToString(), map = jsonresponse3["level"].ToString() });
                        }
                    }
                }, this);
            }
            if (config.MltServSet.Server3ip != "none")
            {
                var url = "http://" + config.MltServSet.Server3ip + "/status.json";
                webrequest.Enqueue(url, null, (code, response) => {
                    if (response != null || code == 200)
                    {

                        var jsonresponse3 = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
                        if (!(jsonresponse3 is Dictionary<string, object>) || jsonresponse3.Count == 0 || !jsonresponse3.ContainsKey("players") || !jsonresponse3.ContainsKey("maxplayers")) return;
                        if (target == "widget" && (!jsonresponse3.ContainsKey("sleepers") || !jsonresponse3.ContainsKey("level"))) return;
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus && target == "status") { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        string name = ", 3⃣: ";
                        if (target == "widget") name = "3:";
                        if (config.MltServSet.Server3name != "none")
                        {
                            name = ", " + config.MltServSet.Server3name + " ";
                            if (target == "widget") name = config.MltServSet.Server3name;
                        }
                        server3 = name + online.ToString() + "/" + slots.ToString();
                        if (target == "widget")
                        {
                            SList.Add(3, new ServerInfo() { name = name, online = online, slots = slots, sleepers = jsonresponse3["sleepers"].ToString(), map = jsonresponse3["level"].ToString() });
                        }
                    }
                }, this);
            }
            if (config.MltServSet.Server4ip != "none")
            {
                var url = "http://" + config.MltServSet.Server4ip + "/status.json";
                webrequest.Enqueue(url, null, (code, response) => {
                    if (response != null || code == 200)
                    {

                        var jsonresponse3 = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
                        if (!(jsonresponse3 is Dictionary<string, object>) || jsonresponse3.Count == 0 || !jsonresponse3.ContainsKey("players") || !jsonresponse3.ContainsKey("maxplayers")) return;
                        if (target == "widget" && (!jsonresponse3.ContainsKey("sleepers") || !jsonresponse3.ContainsKey("level"))) return;
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus && target == "status") { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        string name = ", 4⃣: ";
                        if (target == "widget") name = "4:";
                        if (config.MltServSet.Server4name != "none")
                        {
                            name = ", " + config.MltServSet.Server4name + " ";
                            if (target == "widget") name = config.MltServSet.Server4name;
                        }
                        server4 = name + online.ToString() + "/" + slots.ToString();
                        if (target == "widget")
                        {
                            SList.Add(4, new ServerInfo() { name = name, online = online, slots = slots, sleepers = jsonresponse3["sleepers"].ToString(), map = jsonresponse3["level"].ToString() });
                        }
                    }
                }, this);
            }
            if (config.MltServSet.Server5ip != "none")
            {
                var url = "http://" + config.MltServSet.Server5ip + "/status.json";
                webrequest.Enqueue(url, null, (code, response) => {
                    if (response != null || code == 200)
                    {

                        var jsonresponse3 = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
                        if (!(jsonresponse3 is Dictionary<string, object>) || jsonresponse3.Count == 0 || !jsonresponse3.ContainsKey("players") || !jsonresponse3.ContainsKey("maxplayers")) return;
                        if (target == "widget" && (!jsonresponse3.ContainsKey("sleepers") || !jsonresponse3.ContainsKey("level"))) return;
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus && target == "status") { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        string name = ", 5⃣: ";
                        if (target == "widget") name = "5:";
                        if (config.MltServSet.Server5name != "none")
                        {
                            name = ", " + config.MltServSet.Server5name + " ";
                            if (target == "widget") name = config.MltServSet.Server5name;
                        }
                        server5 = name + online.ToString() + "/" + slots.ToString();
                        if (target == "widget")
                        {
                            SList.Add(5, new ServerInfo() { name = name, online = online, slots = slots, sleepers = jsonresponse3["sleepers"].ToString(), map = jsonresponse3["level"].ToString() });
                        }
                    }
                }, this);
            }
            Puts("Обработка данных. Статус/обложка/виджет будет отправлен(а) через 10 секунд.");
            timer.Once(10f, () =>
            {
                if (target == "widget")
                {
                    PrepareWidgetCode(SList);
                    return;
                }
                text = server1 + server2 + server3 + server4 + server5;
                if (text != "")
                {
                    if (target == "status")
                    {
                        StatusCheck(text);
                        SendVkStatus(text);
                    }
                    if (target == "label")
                    {
                        text = text.Replace("⃣", "%23");
                        UpdateLabelMultiServer(text);
                    }
                }
                else PrintWarning("Текст для статуса/обложки пуст, не заполнен конфиг или не получены данные с Rust:IO");
            });
        }
        private void MsgAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            if (arg.Args == null)
            {
                PrintWarning($"Текст сообщения отсутсвует, правильная команда |sendmsgadmin сообщение|.");
                return;
            }
            string[] args = arg.Args;
            if (args.Length > 0)
            {
                string text = null;
                if (config.MltServSet.MSSEnable) text = $"[VKBot msgadmin] [Сервер {config.MltServSet.ServerNumber}] " + string.Join(" ", args.Skip(0).ToArray());
                else text = $"[VKBot msgadmin] " + string.Join(" ", args.Skip(0).ToArray());
                SendVkMessage(config.AdmNotify.VkID, text);
                Log("Log", $"|sendmsgadmin| Отправлено новое сообщение администратору: ({text})");
            }
        }
        private void ReportAnswer(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            if (arg.Args == null || arg.Args.Count() < 2) { PrintWarning($"Использование команды - reportanswer 'ID репорта' 'текст ответа'"); return; }
            if (reportsdata.VKReportsData.Count == 0) { PrintWarning($"База репортов пуста"); return; }
            int reportid = 0;
            reportid = Convert.ToInt32(arg.Args[0]);
            if (reportid == 0 || !reportsdata.VKReportsData.ContainsKey(reportid)) { PrintWarning($"Указан неверный ID репорта"); return; }
            string answer = string.Join(" ", arg.Args.Skip(1).ToArray());
            if (usersdata.VKUsersData.ContainsKey(reportsdata.VKReportsData[reportid].UserID) && usersdata.VKUsersData[reportsdata.VKReportsData[reportid].UserID].Confirmed)
            {
                string msg = string.Format(GetMsg("ОтветНаРепортВК")) + answer;
                SendVkMessage(usersdata.VKUsersData[reportsdata.VKReportsData[reportid].UserID].VkID, msg);
                PrintWarning($"Ваш ответ был отправлен игроку в ВК.");
                reportsdata.VKReportsData.Remove(reportid);
                ReportsData.WriteObject(reportsdata);
            }
            else
            {
                BasePlayer reciver = BasePlayer.FindByID(reportsdata.VKReportsData[reportid].UserID);
                if (reciver != null)
                {
                    PrintToChat(reciver, string.Format(GetMsg("ОтветНаРепортЧат")) + answer);
                    PrintWarning($"Ваш ответ был отправлен игроку в игровой чат.");
                    reportsdata.VKReportsData.Remove(reportid);
                    ReportsData.WriteObject(reportsdata);
                }
                else PrintWarning($"Игрок отправивший репорт оффлайн. Невозможно отправить ответ.");
            }
        }
        private void ReportList(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            if (reportsdata.VKReportsData.Count == 0) { PrintWarning($"База репортов пуста"); return; }
            foreach (var report in reportsdata.VKReportsData)
            {
                string status = "offline";
                if (BasePlayer.FindByID(report.Value.UserID) != null) status = "online";
                PrintWarning($"Репорт: ID {report.Key} от игрока {report.Value.Name} ({report.Value.UserID.ToString()}) ({status}). Текст: {report.Value.Text}");
            }
        }
        private void ReportClear(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            if (reportsdata.VKReportsData.Count == 0) { PrintWarning($"База репортов пуста"); return; }
            reportsdata.VKReportsData.Clear();
            ReportsData.WriteObject(reportsdata);
            statdata.Reports = 0;
            StatData.WriteObject(statdata);
            PrintWarning($"База репортов очищена");
        }
        private void GetUserInfo(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            if (arg.Args == null) { PrintWarning($"Введите команду userinfo ник/steamid/vkid для получения информации о игроке из базы vkbot"); return; }
            string[] args = arg.Args;
            if (args.Length > 0)
            {
                bool returned = false;
                foreach (var pl in usersdata.VKUsersData)
                {
                    if (pl.Value.Name.ToLower().Contains(args[0]) || pl.Value.UserID.ToString() == (args[0]) || pl.Value.VkID == (args[0]))
                    {
                        returned = true;
                        string text = "Никнейм: " + pl.Value.Name + "\nSTEAM: steamcommunity.com/profiles/" + pl.Value.UserID + "/";
                        if (pl.Value.Confirmed) text = text + "\nVK: vk.com/id" + pl.Value.VkID;
                        else text = text + "\nVK: vk.com/id" + pl.Value.VkID + " (не подтвержден)";
                        if (pl.Value.Bdate != null && pl.Value.Bdate != "noinfo") text = text + "\nДата рождения: " + pl.Value.Bdate;
                        if (config.TopWPlayersPromo.TopWPlEnabled) text = text + "\nРазрушено строений: " + pl.Value.Raids + "\nУбито игроков: " + pl.Value.Kills + "\nНафармил: " + pl.Value.Farm;
                        Puts(text);
                    }
                }
                if (!returned) Puts("Не найдено игроков с таким именем / steamid / vkid");
            }
        }
        private void SendConfCode(string reciverID, string msg, BasePlayer player) => webrequest.Enqueue("https://api.vk.com/method/messages.send?user_ids=" + reciverID + "&message=" + msg + "&" + apiver + "&random_id=" + RandomId() + "&access_token=" + config.VKAPIT.VKToken, null, (code, response) => GetCallback(code, response, "Код подтверждения", player), this);

        private void CheckPlugins()
        {
            var loadedPlugins = plugins.GetAll().Where(pl => !pl.IsCorePlugin).ToArray();
            var loadedPluginNames = new HashSet<string>(loadedPlugins.Select(pl => pl.Name));
            var unloadedPluginErrors = new Dictionary<string, string>();
            foreach (var loader in Interface.Oxide.GetPluginLoaders())
            {
                string msg;
                foreach (var name in loader.ScanDirectory(Interface.Oxide.PluginDirectory).Except(loadedPluginNames)) { unloadedPluginErrors[name] = loader.PluginErrors.TryGetValue(name, out msg) ? msg : "Unloaded"; }
            }
            if (unloadedPluginErrors.Count > 0)
            {
                string text = null;
                if (config.MltServSet.MSSEnable) text = $"[VKBot] [Сервер {config.MltServSet.ServerNumber}] Произошла ошибка загрузки следующих плагинов:";
                else text = $"[VKBot]  Произошла ошибка загрузки следующих плагинов:";
                foreach (var pluginerror in unloadedPluginErrors) text = text + " " + pluginerror.Key + ".";
                if (config.ChNotify.ChNotfEnabled && config.ChNotify.ChNotfSet.Contains("plugins"))
                {
                    SendChatMessage(config.ChNotify.ChatID, text);
                    if (config.ChNotify.AdmMsg) SendVkMessage(config.AdmNotify.VkID, text);
                }
                else SendVkMessage(config.AdmNotify.VkID, text);
            }
        }
        private void PrepareWidgetCode(Dictionary<int, ServerInfo> Slist)
        {
            string code = @"return{""title"":""" + config.GrWgSet.WgTitle + @""",""head"":[{""text"":""Сервер""},{""text"":""Онлайн""},{""text"":""Спящие""},{""text"":""Слоты""},{""text"":""Карта""}],""body"":[";
            if (Slist.Count != 0)
            {
                foreach (var info in Slist) code = code + @"[{""text"":""" + info.Value.name + @"""},{""text"":""" + info.Value.online + @"""},{""text"":""" + info.Value.sleepers + @"""},{""text"":""" + info.Value.slots + @"""},{""text"":""" + info.Value.map + @"""}],";
            }
            else
            {
                string map = ConVar.Server.level;
                if (ConVar.Server.levelurl != string.Empty) map = "Custom Map";
                code = code + @"[{""text"":""" + ConVar.Server.hostname + @"""},{""text"":""" + BasePlayer.activePlayerList.Count.ToString() + @"""},{""text"":""" + BasePlayer.sleepingPlayerList.Count.ToString() + @"""},{""text"":""" + ConVar.Server.maxplayers.ToString() + @"""},{""text"":""" + map + @"""}],";
            }
            code = code + @"],";
            if (config.GrWgSet.URLTitle != "none") code = code + @"""more"":""" + config.GrWgSet.URLTitle + @""",""more_url"": """ + config.GrWgSet.URL + @""",";
            code = code + "};";
            SendWidget(code);
        }
        #endregion

        #region VKAPI
        private void SendWidget(string widget) => webrequest.Enqueue("https://api.vk.com/method/appWidgets.update?type=table" + "&code=" + URLEncode(widget) + "&"+apiver+"&access_token=" + config.GrWgSet.WgToken, null, (code, response) => GetCallback(code, response, "Виджет"), this);
        private void SendChatMessage(string chatid, string msg) => webrequest.Enqueue("https://api.vk.com/method/messages.send?chat_id=" + chatid + "&message=" + URLEncode(msg) + "&"+apiver+ "&random_id=" + RandomId() +"&access_token=" + config.ChNotify.ChNotfToken, null, (code, response) => GetCallback(code, response, "Сообщение в беседу"), this);
        private void SendVkMessage(string reciverID, string msg) => webrequest.Enqueue("https://api.vk.com/method/messages.send?user_ids=" + reciverID + "&message=" + URLEncode(msg) + "&"+apiver + "&random_id=" + RandomId() + "&access_token=" + config.VKAPIT.VKToken, null, (code, response) => GetCallback(code, response, "Сообщение"), this);
        private void SendVkWall(string msg) => webrequest.Enqueue("https://api.vk.com/method/wall.post?owner_id=-" + config.VKAPIT.GroupID + "&message=" + URLEncode(msg) + "&from_group=1&"+apiver+"&access_token=" + config.VKAPIT.VKTokenApp, null, (code, response) => GetCallback(code, response, "Пост"), this);
        private void SendVkStatus(string msg) => webrequest.Enqueue("https://api.vk.com/method/status.set?group_id=" + config.VKAPIT.GroupID + "&text=" + URLEncode(msg) + "&" + apiver + "&access_token=" + config.VKAPIT.VKTokenApp, null, (code, response) => GetCallback(code, response, "Статус"), this);
        private void AddComentToBoard(string topicid, string msg) => webrequest.Enqueue("https://api.vk.com/method/board.createComment?group_id=" + config.VKAPIT.GroupID + "&topic_id=" + URLEncode(topicid) + "&from_group=1&message=" + msg + "&"+apiver+"&access_token=" + config.VKAPIT.VKTokenApp, null, (code, response) => GetCallback(code, response, "Комментарий в обсуждения"), this);
        private string RandomId() => random.Next(Int32.MinValue, Int32.MaxValue).ToString();
        #endregion

        #region VKBotAPI
        string GetUserVKId(ulong userid)
        {
            if (!usersdata.VKUsersData.ContainsKey(userid) || !usersdata.VKUsersData[userid].Confirmed) return null;
            if (BannedUsers.Contains(userid.ToString())) return null;
            return usersdata.VKUsersData[userid].VkID;
        }
        string GetUserLastNotice(ulong userid)
        {
            if (!usersdata.VKUsersData.ContainsKey(userid) || !usersdata.VKUsersData[userid].Confirmed) return null;
            return usersdata.VKUsersData[userid].LastRaidNotice;
        }
        string AdminVkID() => config.AdmNotify.VkID;
        private void VKAPIChatMsg(string text)
        {
            if (config.ChNotify.ChNotfEnabled) SendChatMessage(config.ChNotify.ChatID, text);
            else PrintWarning($"Сообщение не отправлено в беседу. Данная функция отключена. Текст сообщения: {text}");
        }
        private void VKAPISaveLastNotice(ulong userid, string lasttime)
        {
            if (usersdata.VKUsersData.ContainsKey(userid)) { usersdata.VKUsersData[userid].LastRaidNotice = lasttime; VKBData.WriteObject(usersdata); }
        }
        private void VKAPIWall(string text, string attachments, bool atimg)
        {
            if (atimg) { SendVkWall($"{text}&attachments={attachments}"); Log("vkbotapi", $"Отправлен новый пост на стену: ({text}&attachments={attachments})"); }
            else { SendVkWall($"{text}"); Log("vkbotapi", $"Отправлен новый пост на стену: ({text})"); }
        }
        private void VKAPIMsg(string text, string attachments, string reciverID, bool atimg)
        {
            if (atimg) { SendVkMessage(reciverID, $"{text}&attachment={attachments}"); Log("vkbotapi", $"Отправлено новое сообщение пользователю {reciverID}: ({text}&attachments={attachments})"); }
            else { SendVkMessage(reciverID, $"{text}"); Log("vkbotapi", $"Отправлено новое сообщение пользователю {reciverID}: ({text})"); }
        }
        private void VKAPIStatus(string msg)
        {
            StatusCheck(msg);
            SendVkStatus(msg);
            Log("vkbotapi", $"Отправлен новый статус: {msg}");
        }
        #endregion

        #region Helpers
        void Log(string filename, string text) => LogToFile(filename, $"[{DateTime.Now}] {text}", this);
        void GetCallback(int code, string response, string type, BasePlayer player = null)
        {
            if (!response.Contains("error")) { Puts($"{type} отправлен(о): {response}"); if (type == "Код подтверждения" && player != null) StartCodeSendedGUI(player); }
            else
            {
                if (type == "Код подтверждения")
                {
                    if (response.Contains("Can't send messages for users without permission") && player != null) StartVKBotHelpVKGUI(player);
                    else Log("errorconfcode", $"Ошибка отправки кода подтверждения. Ответ сервера ВК: {response}");
                }
                else
                {
                    PrintWarning($"{type} не отправлен(о). Файлы лога: /oxide/logs/VKBot/");
                    Log("Errors", $"{type} не отправлен(о). Ошибка: " + response);
                }
            }
        }
        private string EmojiCounters(string counter)
        {
            var chars = counter.ToCharArray();
            List<object> digits = new List<object>() { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            string emoji = "";
            for (int ctr = 0; ctr < chars.Length; ctr++)
            {                
                if (digits.Contains(chars[ctr].ToString()))
                {
                    string replace = chars[ctr] + "⃣";
                    emoji = emoji + replace;
                }
                else emoji = emoji + chars[ctr];
            }
            return emoji;
        }
        private string WipeDate() => SaveRestore.SaveCreatedTime.ToLocalTime().ToString("dd.MM");
        private string GetOnline()
        {
            string onlinecounter = BasePlayer.activePlayerList.Count.ToString();
            if (config.StatusStg.OnlWmaxslots) onlinecounter = onlinecounter + "/" + ConVar.Server.maxplayers.ToString();
            return onlinecounter;
        }
        private string URLEncode(string input)
        {
            if (input.Contains("#")) input = input.Replace("#", "%23");
            if (input.Contains("$")) input = input.Replace("$", "%24");
            if (input.Contains("+")) input = input.Replace("+", "%2B");
            if (input.Contains("/")) input = input.Replace("/", "%2F");
            if (input.Contains(":")) input = input.Replace(":", "%3A");
            if (input.Contains(";")) input = input.Replace(";", "%3B");
            if (input.Contains("?")) input = input.Replace("?", "%3F");
            if (input.Contains("@")) input = input.Replace("@", "%40");
            return input;
        }
        private void StatusCheck(string msg)
        {
            if (msg.Length > 140) PrintWarning($"Текст статуса слишком длинный. Измените формат статуса чтобы текст отобразился полностью. Лимит символов в статусе - 140. Длина текста - {msg.Length.ToString()}");
        }
        private bool IsNPC(BasePlayer player)
        {
            if (player is NPCPlayer) return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))  return true;
            return false;
        }
        private void CheckAdminID()
        {
            if (config.AdmNotify.VkID.Contains("/"))
            {
                string id = config.AdmNotify.VkID.Trim(new char[] { '/' });
                config.AdmNotify.VkID = id;
                Config.WriteObject(config, true);
                PrintWarning("VK ID администратора исправлен. Инструкция по настройке плагина - goo.gl/xRkEUa");
            }
        }
        private static string GetColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }
            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
        private void DeleteOldUsers(string days = null)
        {
            int ddays = 30;
            int t;
            if (days != null && Int32.TryParse(days, out t)) ddays = t;
            int deleted = 0;
            List<ulong> ForDelete = new List<ulong>();
            foreach (var user in usersdata.VKUsersData)
            {
                if (user.Value.LastSeen == null) ForDelete.Add(user.Key);
                else
                {
                    DateTime LNT;
                    if (DateTime.TryParseExact(user.Value.LastSeen, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out LNT) && DateTime.Now.Subtract(LNT).Days >= ddays) ForDelete.Add(user.Key);
                }
            }
            foreach (var d in ForDelete)
            {
                usersdata.VKUsersData.Remove(d); deleted++;
            }
            if (deleted > 0) { PrintWarning($"Удалено устаревших профилей игроков из базы VKBot: {deleted}"); VKBData.WriteObject(usersdata); }
            else PrintWarning($"Нет профилей для удаления.");
        }
        private void CheckOxideUpdate()
        {
            string currentver = Manager.GetPlugin("RustCore").Version.ToString();
            webrequest.Enqueue("https://umod.org/games/rust.json", null, (code, response) => 
            {
                if (code == 200 || response != null)
                {
                    var json = JObject.Parse(response);
                    if (json == null) return;
                    string latestver = (string)json["latest_release_version"];
                    if (latestver == null) return;
                    if (latestver != currentver && !OxideUpdateSended) { SendVkMessage(config.AdmNotify.VkID, $"Доступно новое обновление Oxide {latestver}. https://umod.org/games/rust"); OxideUpdateSended = true; }
                }
            }, this);
            timer.Once(3600f, () => { CheckOxideUpdate(); });
        }
        #endregion

        #region TopWipePlayersStatsAndPromo
        private string BannedUsers = ServerUsers.BanListString();
        private ulong GetTopRaider()
        {
            int max = 0;
            ulong TopID = 0;
            int amount = usersdata.VKUsersData.Count;
            if (amount != 0)
            {
                foreach (var pl in usersdata.VKUsersData)
                {
                    if (pl.Value.Confirmed && pl.Value.Raids > max && !BannedUsers.Contains(pl.Key.ToString())) { max = pl.Value.Raids; TopID = pl.Key; }
                }
            }
            if (max != 0) return TopID;
            else return 0;
        }
        private ulong GetTopKiller()
        {
            int max = 0;
            ulong TopID = 0;
            int amount = usersdata.VKUsersData.Count;
            if (amount != 0)
            {
                foreach (var pl in usersdata.VKUsersData)
                {
                    if (pl.Value.Confirmed && pl.Value.Kills > max && !BannedUsers.Contains(pl.Key.ToString())) { max = pl.Value.Kills; TopID = pl.Key; }
                }
            }
            if (max != 0) return TopID;
            else return 0;
        }
        private ulong GetTopFarmer()
        {
            int max = 0;
            ulong TopID = 0;
            int amount = usersdata.VKUsersData.Count;
            if (amount != 0)
            {
                foreach (var pl in usersdata.VKUsersData)
                {
                    if (pl.Value.Confirmed && pl.Value.Farm > max && !BannedUsers.Contains(pl.Key.ToString())) { max = pl.Value.Farm; TopID = pl.Key; }
                }
            }
            if (max != 0) return TopID;
            else return 0;
        }
        private void SendPromoMsgsAndPost()
        {
            var traider = GetTopRaider();
            var tkiller = GetTopKiller();
            var tfarmer = GetTopFarmer();
            if (config.TopWPlayersPromo.TopPlPost)
            {
                bool check = false;
                string text = "Топ игроки прошедшего вайпа:";
                if (traider != 0) { text = text + "\nТоп рэйдер: " + usersdata.VKUsersData[traider].Name; check = true; }
                if (tkiller != 0) { text = text + "\nТоп киллер: " + usersdata.VKUsersData[tkiller].Name; check = true; }
                if (tfarmer != 0) { text = text + "\nТоп фармер: " + usersdata.VKUsersData[tfarmer].Name; check = true; }
                if (config.TopWPlayersPromo.TopPlPromoGift) text = text + "\nТоп игроки получают в качестве награды промокод на баланс в магазине.";
                if (check)
                {
                    if (config.TopWPlayersPromo.TopPlPostAtt != "none") text = text + "&attachments=" + config.TopWPlayersPromo.TopPlPostAtt;
                    SendVkWall(text);
                }
            }
            if (traider != 0 && config.TopWPlayersPromo.TopPlPromoGift)
            {
                string text = string.Format(GetMsg("СообщениеИгрокуТопПромо"), "рейдер", config.TopWPlayersPromo.TopRaiderPromo, config.TopWPlayersPromo.StoreUrl);
                if (config.TopWPlayersPromo.TopRaiderPromoAtt != "none") text = text + "&attachments=" + config.TopWPlayersPromo.TopRaiderPromoAtt;
                SendVkMessage(usersdata.VKUsersData[traider].VkID, text);
            }
            if (tkiller != 0 && config.TopWPlayersPromo.TopPlPromoGift)
            {
                string text = string.Format(GetMsg("СообщениеИгрокуТопПромо"), "киллер", config.TopWPlayersPromo.TopKillerPromo, config.TopWPlayersPromo.StoreUrl);
                if (config.TopWPlayersPromo.TopKillerPromoAtt != "none") text = text + "&attachments=" + config.TopWPlayersPromo.TopKillerPromoAtt;
                SendVkMessage(usersdata.VKUsersData[tkiller].VkID, text);
            }
            if (tfarmer != 0 && config.TopWPlayersPromo.TopPlPromoGift)
            {
                string text = string.Format(GetMsg("СообщениеИгрокуТопПромо"), "фармер", config.TopWPlayersPromo.TopFarmerPromo, config.TopWPlayersPromo.StoreUrl);
                if (config.TopWPlayersPromo.TopFarmerPromoAtt != "none") text = text + "&attachments=" + config.TopWPlayersPromo.TopFarmerPromoAtt;
                SendVkMessage(usersdata.VKUsersData[tfarmer].VkID, text);
            }
        }
        private string PromoGenerator()
        {
            List<string> Chars = new List<string>() { "A", "1", "B", "2", "C", "3", "D", "4", "F", "5", "G", "6", "H", "7", "I", "8", "J", "9", "K", "0", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
            string promo = "";
            for (int i = 0; i < 6; i++)
            {
                promo = promo + Chars.GetRandom();
            }
            return promo;
        }
        private void SetRandomPromo()
        {
            config.TopWPlayersPromo.TopFarmerPromo = PromoGenerator();
            config.TopWPlayersPromo.TopKillerPromo = PromoGenerator();
            config.TopWPlayersPromo.TopRaiderPromo = PromoGenerator();
            Config.WriteObject(config, true);
            string msg = "[VKBot]";
            if (config.MltServSet.MSSEnable) msg = msg + " [Сервер " + config.MltServSet.ServerNumber.ToString() + "]";
            msg = msg + " В настройки добавлены новые промокоды: \nТоп рейдер - " + config.TopWPlayersPromo.TopRaiderPromo + "\nТоп киллер - " + config.TopWPlayersPromo.TopKillerPromo + "\nТоп фармер - " + config.TopWPlayersPromo.TopFarmerPromo;
            SendVkMessage(config.AdmNotify.VkID, msg);
        }
        #endregion

        #region DynamicLabelVK
        private void UpdateVKLabel()
        {
            string url = config.DGLSet.DLUrl + "?";
            int count = 0;
            if (config.DGLSet.DLText1 != "none")
            {
                if (count == 0)
                {
                    url = url + "t1=" + PrepareStatus(config.DGLSet.DLText1, "label");
                    count++;
                }
                else url = url + "&t1=" + PrepareStatus(config.DGLSet.DLText1, "label");
            }
            if (config.DGLSet.DLText2 != "none")
            {
                if (count == 0)
                {
                    url = url + "t2=" + PrepareStatus(config.DGLSet.DLText2, "label");
                    count++;
                }
                else url = url + "&t2=" + PrepareStatus(config.DGLSet.DLText2, "label");
            }
            if (config.DGLSet.DLText3 != "none")
            {
                if (count == 0)
                {
                    url = url + "t3=" + PrepareStatus(config.DGLSet.DLText3, "label");
                    count++;
                }
                else url = url + "&t3=" + PrepareStatus(config.DGLSet.DLText3, "label");
            }
            if (config.DGLSet.DLText4 != "none")
            {
                if (count == 0)
                {
                    url = url + "t4=" + PrepareStatus(config.DGLSet.DLText4, "label");
                    count++;
                }
                else url = url + "&t4=" + PrepareStatus(config.DGLSet.DLText4, "label");
            }
            if (config.DGLSet.DLText5 != "none")
            {
                if (count == 0)
                {
                    url = url + "t5=" + PrepareStatus(config.DGLSet.DLText5, "label");
                    count++;
                }
                else url = url + "&t5=" + PrepareStatus(config.DGLSet.DLText5, "label");
            }
            if (config.DGLSet.DLText6 != "none")
            {
                if (count == 0)
                {
                    url = url + "t6=" + PrepareStatus(config.DGLSet.DLText6, "label");
                    count++;
                }
                else url = url + "&t6=" + PrepareStatus(config.DGLSet.DLText6, "label");
            }
            if (config.DGLSet.DLText7 != "none")
            {
                if (count == 0)
                {
                    url = url + "t7=" + PrepareStatus(config.DGLSet.DLText7, "label");
                    count++;
                }
                else url = url + "&t7=" + PrepareStatus(config.DGLSet.DLText7, "label");
            }
            if (config.TopWPlayersPromo.TopWPlEnabled && config.DGLSet.TPLabel)
            {
                var tr = GetTopRaider();
                var tk = GetTopKiller();
                var tf = GetTopFarmer();
                if (tf != 0) url = url + "&tfarmer=" + tf.ToString();
                if (tk != 0) url = url + "&tkiller=" + tk.ToString();
                if (tr != 0) url = url + "&traider=" + tr.ToString();
            }
            webrequest.Enqueue(url, null, (code, response) => DLResult(code, response), this);
        }
        private void DLResult(int code, string response)
        {
            if (response.Contains("good")) Puts("Обложка группы обновлена");
            else Puts("Прозошла ошибка обновления обложки, проверьте настройки.");
        }
        private void ULabel(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) return;
            if (config.DGLSet.DLEnable && config.DGLSet.DLUrl != "none")
            {
                if (config.DGLSet.DLMSEnable) UpdateMultiServerStatus("label");
                else UpdateVKLabel();
            }
            else PrintWarning($"Функция обновления обложки отключена, или не указана ссылка на скрипт обновления.");
        }
        private void UpdateLabelMultiServer(string text) => webrequest.Enqueue(config.DGLSet.DLUrl + "?t1=" + text, null, (code, response) => DLResult(code, response), this);
        #endregion

        #region GUIBuilder
        private CuiElement BPanel(string name, string color, string anMin, string anMax, string parent = "Hud", bool cursor = false, float fade = 1f)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiImageComponent { Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = fade, Color = color },
                    new CuiRectTransformComponent { AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            if (cursor) Element.Components.Add(new CuiNeedsCursorComponent());
            return Element;
        }
        private CuiElement Panel(string name, string color, string anMin, string anMax, string parent = "Hud", bool cursor = false, float fade = 1f)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiImageComponent { FadeIn = fade, Color = color },
                    new CuiRectTransformComponent { AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            if (cursor) Element.Components.Add(new CuiNeedsCursorComponent());
            return Element;
        }
        private CuiElement Text(string parent, string color, string text, TextAnchor pos, int fsize, string anMin = "0 0", string anMax = "1 1", string fname = "robotocondensed-bold.ttf", float fade = 3f)
        {
            var Element = new CuiElement()
            {
                Parent = parent,
                Components =
                {
                    new CuiTextComponent() { Color = color, Text = text, Align = pos, Font = fname, FontSize = fsize, FadeIn = fade },
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private CuiElement Button(string name, string parent, string command, string color, string anMin, string anMax, float fade = 3f)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiButtonComponent { Command = command, Color = color, FadeIn = fade},
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private CuiElement Image(string parent, string url, string anMin, string anMax, float fade = 3f, string color = "1 1 1 1")
        {
            var Element = new CuiElement
            {
                Parent = parent,
                Components =
                {
                    new CuiRawImageComponent { Color = color, Url = url, FadeIn = fade},
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private CuiElement Input(string name, string parent, int fsize, string command, string anMin = "0 0", string anMax = "1 1", TextAnchor pos = TextAnchor.MiddleCenter, int chlimit = 300, bool psvd = false, float fade = 3f)
        {
            string text = "";
            var Element = new CuiElement
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiInputFieldComponent
                        {
                            Align = pos,
                            CharsLimit = chlimit,
                            FontSize = fsize,
                            Command = command + text,
                            IsPassword = psvd,
                            Text = text
                        },
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private void UnloadAllGUI()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "MainUI");
                CuiHelper.DestroyUi(player, "HelpUI");
                CuiHelper.DestroyUi(player, "AddVKUI");
                CuiHelper.DestroyUi(player, "CodeSendedUI");
                CuiHelper.DestroyUi(player, "ReportGUI");
                CuiHelper.DestroyUi(player, "PListGUI");
            }
        }
        #endregion

        #region MenuGUI
        private string UserName(string name)
        {
            if (name.Length > 15) name = name.Remove(12) + "...";
            return name;
        }
        private void StartVKBotAddVKGUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer
            {
                BPanel("AddVKUI", GetColor(config.GUISet.BgColor), config.GUISet.AnchorMin, config.GUISet.AnchorMax, "Hud", true),
                Image("AddVKUI", config.GUISet.Logo, "0.01 0.6", "0.99 0.99"),
                Text("AddVKUI", "1 1 1 1", UserName(player.displayName), TextAnchor.MiddleCenter, 18, "0.01 0.5", "0.99 0.6"),          
                Text("AddVKUI", "1 1 1 1", "Укажите ссылку на страницу ВК\nв поле ниже и нажмите ENTER", TextAnchor.MiddleCenter, 18, "0.01 0.33", "0.99 0.5"),
                Panel("back", "0 0.115 0 0.65", "0.01 0.13", "0.99 0.3", "AddVKUI"),
                Input("addvkinput", "back", 18, "vk.menugui addvkgui.addvk "),
                Button("AddVKcloseGUI", "AddVKUI", "vk.menugui addvkgui.close", GetColor(config.GUISet.BCloseColor), "0.01 0.01", "0.99 0.06"),
                Text("AddVKcloseGUI", "1 1 1 1", "Закрыть", TextAnchor.MiddleCenter, 18)
            };
            CuiHelper.AddUi(player, container);
        }
        private void StartVKBotMainGUI(BasePlayer player)
        {
            bool NeedRemove = false;
            string addvkbuutontext = "Добавить профиль ВК";
            string addvkbuttoncommand = "vk.menugui maingui.addvk";
            string addvkbuttoncolor = GetColor(config.GUISet.BMenuColor);
            string giftvkbuutontext = "Получить награду за\nвступление в группу ВК";
            string giftvkbuttoncommand = "vk.menugui maingui.gift";
            string giftvkbuttoncolor = GetColor(config.GUISet.BMenuColor);
            string addvkbuttonanmax = "0.99 0.5";
            CuiElementContainer container = new CuiElementContainer
            {
                BPanel("MainUI", GetColor(config.GUISet.BgColor), config.GUISet.AnchorMin, config.GUISet.AnchorMax, "Hud", true),            
                Image("MainUI", config.GUISet.Logo, "0.01 0.6", "0.99 0.99"),
                Text("MainUI", "1 1 1 1", UserName(player.displayName), TextAnchor.MiddleCenter, 18, "0.01 0.5", "0.99 0.6")
            };
            if (usersdata.VKUsersData.ContainsKey(player.userID))
            {
                if (!usersdata.VKUsersData[player.userID].Confirmed) { addvkbuutontext = "Подтвердить профиль"; addvkbuttoncommand = "vk.menugui maingui.confirm"; NeedRemove = true; addvkbuttonanmax = "0.49 0.5"; }
                else { addvkbuutontext = "Профиль добавлен"; addvkbuttoncommand = ""; addvkbuttoncolor = "0 0.115 0 0.65"; }
                if (usersdata.VKUsersData[player.userID].GiftRecived) { giftvkbuutontext = "Награда за вступление\nв группу ВК получена"; giftvkbuttoncommand = ""; giftvkbuttoncolor = "0 0.115 0 0.65"; }
            }
            container.Add(Button("VKAddButton", "MainUI", addvkbuttoncommand, addvkbuttoncolor, "0.01 0.43", addvkbuttonanmax));
            container.Add(Text("VKAddButton", "1 1 1 1", addvkbuutontext, TextAnchor.MiddleCenter, 18));
            if (NeedRemove)
            {
                container.Add(Button("VKRemoveButton", "MainUI", "vk.menugui maingui.removevk", addvkbuttoncolor, "0.51 0.43", "0.99 0.5"));
                container.Add(Text("VKRemoveButton", "1 1 1 1", "Удалить профиль", TextAnchor.MiddleCenter, 18));
            }
            if (config.GrGifts.VKGroupGifts)
            {
                container.Add(Button("VKGiftButton", "MainUI", giftvkbuttoncommand, giftvkbuttoncolor, "0.01 0.3", "0.99 0.42"));
                container.Add(Text("VKGiftButton", "1 1 1 1", giftvkbuutontext, TextAnchor.MiddleCenter, 18));
            }
            if (!config.WipeStg.WCMDIgnore)
            {
                string text = "Подписаться на\nоповещения о вайпе в ВК";
                if (usersdata.VKUsersData.ContainsKey(player.userID) && usersdata.VKUsersData[player.userID].WipeMsg) { text = "Отписаться от\nопвещений о вайпе в ВК"; }
                container.Add(Button("VKWipeAlertsButton", "MainUI", "vk.menugui maingui.walert", GetColor(config.GUISet.BMenuColor), "0.01 0.18", "0.99 0.29"));
                container.Add(Text("VKWipeAlertsButton", "1 1 1 1", text, TextAnchor.MiddleCenter, 18));
            }
            container.Add(Text("MainUI", "1 1 1 1", $"Группа сервера в ВК:\n<color=#049906>{config.GrGifts.VKGroupUrl}</color>", TextAnchor.MiddleCenter, 18, "0.01 0.06", "0.99 0.17"));         
            container.Add(Button("VKcloseGUI", "MainUI", "vk.menugui maingui.close", GetColor(config.GUISet.BCloseColor), "0.01 0.01", "0.99 0.06"));
            container.Add(Text("VKcloseGUI", "1 1 1 1", "Закрыть", TextAnchor.MiddleCenter, 18));
            CuiHelper.AddUi(player, container);
        }
        private void StartVKBotHelpVKGUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer
            {
                BPanel("HelpUI", GetColor(config.GUISet.BgColor), config.GUISet.AnchorMin, config.GUISet.AnchorMax, "Hud", true),
                Image("HelpUI", config.GUISet.Logo, "0.01 0.6", "0.99 0.99"),
                Text("HelpUI", "1 1 1 1", UserName(player.displayName), TextAnchor.MiddleCenter, 18, "0.01 0.5", "0.99 0.6"),
                Text("HelpUI", "1 1 1 1", "Наш бот не может отправить вам сообщение.\nОтправьте в сообщения группы слово <color=#049906>ИСПРАВИТЬ</color>\nи нажмите кнопку <color=#049906>ПОЛУЧИТЬ КОД</color>", TextAnchor.MiddleCenter, 18, "0.01 0.23", "0.99 0.5"),//Текст
                Button("VKsendGUI", "HelpUI", "vk.menugui helpgui.confirm", GetColor(config.GUISet.BSendColor), "0.01 0.08", "0.99 0.2"),
                Text("VKsendGUI", "1 1 1 1", "Получить код", TextAnchor.MiddleCenter, 18),
                Button("VKcloseGUI", "HelpUI", "vk.menugui helpgui.close", GetColor(config.GUISet.BCloseColor), "0.01 0.01", "0.99 0.06"),
                Text("VKcloseGUI", "1 1 1 1", "Закрыть", TextAnchor.MiddleCenter, 18)
            };
            CuiHelper.AddUi(player, container);
        }
        private void StartCodeSendedGUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer
            {
                BPanel("CodeSendedUI", GetColor(config.GUISet.BgColor), config.GUISet.AnchorMin, config.GUISet.AnchorMax, "Hud", true),
                Image("CodeSendedUI", config.GUISet.Logo, "0.01 0.6", "0.99 0.99"),
                Text("CodeSendedUI", "1 1 1 1", UserName(player.displayName), TextAnchor.MiddleCenter, 18, "0.01 0.5", "0.99 0.6"),
                Text("CodeSendedUI", "1 1 1 1", "На вашу страницу ВК отправлено\nсообщение с дальнейшими инструкциями.", TextAnchor.MiddleCenter, 18, "0.01 0.23", "0.99 0.5"),
                Button("VKcloseGUI", "CodeSendedUI", "vk.menugui csendui.close", GetColor(config.GUISet.BCloseColor), "0.01 0.01", "0.99 0.06"),
                Text("VKcloseGUI", "1 1 1 1", "Закрыть", TextAnchor.MiddleCenter, 18)
            };
            CuiHelper.AddUi(player, container);
        }
        [ConsoleCommand("vk.menugui")]
        private void CmdChoose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.Args == null) return;
            switch (arg.Args[0])
            {
                case "maingui.close":
                    CuiHelper.DestroyUi(player, "MainUI");
                    break;
                case "maingui.addvk":
                    CuiHelper.DestroyUi(player, "MainUI");
                    StartVKBotAddVKGUI(player);
                    break;
                case "maingui.removevk":
                    CuiHelper.DestroyUi(player, "MainUI");
                    if (usersdata.VKUsersData.ContainsKey(player.userID)) { usersdata.VKUsersData.Remove(player.userID); VKBData.WriteObject(usersdata); }
                    break;
                case "maingui.walert":
                    WAlert(player);
                    break;
                case "maingui.gift":
                    CuiHelper.DestroyUi(player, "MainUI");
                    FixedGifts(player);
                    break;
                case "maingui.confirm":
                    CuiHelper.DestroyUi(player, "MainUI");
                    SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player);
                    break;
                case "addvkgui.close":
                    CuiHelper.DestroyUi(player, "AddVKUI");
                    break;
                case "addvkgui.addvk":
                    string url = string.Join(" ", arg.Args.Skip(1).ToArray());
                    if (!url.Contains("vk.com/")) { PrintToChat(player, string.Format(GetMsg("НеправильнаяСсылка"))); return; }
                    CuiHelper.DestroyUi(player, "AddVKUI");
                    CheckVkUser(player, url);
                    break;
                case "helpgui.close":
                    CuiHelper.DestroyUi(player, "HelpUI");
                    break;
                case "helpgui.confirm":
                    CuiHelper.DestroyUi(player, "HelpUI");
                    SendConfCode(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}", player);
                    break;
                case "csendui.close":
                    CuiHelper.DestroyUi(player, "CodeSendedUI");
                    break;
            }
        }
        private void FixedGifts(BasePlayer player)
        {
            if (GiftsList.ContainsKey(player))
            {
                TimeSpan interval = DateTime.Now - GiftsList[player];
                if (interval.TotalSeconds < 15) { PrintToChat(player, "Слишком часто. Попробуйте позже."); return; }
                else { GiftsList[player] = DateTime.Now; VKGift(player); }
            }
            else
            {
                GiftsList.Add(player, DateTime.Now);
                VKGift(player);
            }
        }
        #endregion

        #region ReportGUI
        private List<BasePlayer> OpenReportUI = new List<BasePlayer>();
        private void ReportGUI(BasePlayer player, BasePlayer target = null)
        {
            string chpl = "\nЕсли хотите отправить жалобу на игрока, сначала нажмите на кнопку <color=#ff0000>ВЫБРАТЬ ИГРОКА</color>";
            if (target != null) chpl = $"\nЖалоба на игрока <color=#ff0000>{target.displayName}</color>";
            string title = "<color=#ff0000>" + config.AdmNotify.ReportsNotify + "</color>" + chpl + "\nВведите ваше сообщение в поле ниже и нажмите <color=#ff0000>ENTER</color>";
            CuiElementContainer container = new CuiElementContainer
            {
                BPanel("ReportGUI", "0 0 0 0.75", "0.2 0.125", "0.8 0.9", "Hud", true),
                Panel("header", "0 0 0 0.75", "0 0.93", "1 1", "ReportGUI"),
                Text("header", "1 1 1 1", "Отправка сообщения администратору", TextAnchor.MiddleCenter, 20),
                Button("close", "header", "vk.report close", "1 0 0 1", "0.94 0.01", "1.0 0.98"),
                Text("close", "1 1 1 1", "X", TextAnchor.MiddleCenter, 20),
                Panel("text", "0 0 0 0.75", "0 0.77", "1 0.93", "ReportGUI"),
                Text("text", "1 1 1 1", title, TextAnchor.MiddleCenter, 18)
            };
            if (target == null)
            {
                container.Add(Button("PlayerChoise", "ReportGUI", "vk.report choiceplayer", "0.7 1 0.6 0.4", "0.378 0.71", "0.628 0.76"));
                container.Add(Text("PlayerChoise", "1 1 1 1", "ВЫБРАТЬ ИГРОКА", TextAnchor.MiddleCenter, 18));
            }
            container.Add(Panel("inputbg", "0 0.115 0 0.65", "0 0", "1 0.698", "ReportGUI"));
            string command = "vk.report send ";
            if (target != null) command = command + target.userID + " ";
            container.Add(Input("reportinput", "inputbg", 18, command));
            OpenReportUI.Add(player);
            CuiHelper.AddUi(player, container);
        }
        [ConsoleCommand("vk.report")]
        private void ReportGUIChoose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (!config.AdmNotify.SendReports) { PrintToChat(player, string.Format(GetMsg("ФункцияОтключена"))); return; }
            if (arg.Args == null) return;
            switch (arg.Args[0])
            {
                case "close":
                    if (OpenReportUI.Contains(player)) OpenReportUI.Remove(player);
                    CuiHelper.DestroyUi(player, "ReportGUI");
                    break;
                case "choiceplayer":
                    if (OpenReportUI.Contains(player)) OpenReportUI.Remove(player);
                    CuiHelper.DestroyUi(player, "ReportGUI");
                    PListUI(player);
                    break;
                case "send":
                    if (OpenReportUI.Contains(player)) OpenReportUI.Remove(player);
                    CuiHelper.DestroyUi(player, "ReportGUI");
                    CheckReport(player, arg.Args);
                    break;
            }
        }
        private object OnServerCommand(ConsoleSystem.Arg arg) //блочим команды при наборе текста репорт
        {
            BasePlayer player = arg.Player();
            if (player == null || arg.cmd == null) return null;
            if (OpenReportUI.Contains(player) && !arg.cmd.FullName.ToLower().StartsWith("vk.report")) return true;
            return null;
        }
        private object OnPlayerCommand(ConsoleSystem.Arg arg) //блочим команды при наборе текста репорт
        {
            var player = (BasePlayer)arg.Connection.player;
            if (player != null)
            {
                if (OpenReportUI.Contains(player) && !arg.cmd.FullName.ToLower().StartsWith("vk.report")) return true;
            }
            return null;
        }
        #endregion

        #region PlayersListGUI
        [ConsoleCommand("vk.pllist")]
        private void PListCMD(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (arg.Args == null) return;
            switch (arg.Args[0])
            {
                case "close":
                    CuiHelper.DestroyUi(player, "PListGUI");
                    break;
                case "report":
                    ulong uid = 0;
                    if (arg.Args.Length > 1 && ulong.TryParse(arg.Args[1], out uid))
                    {
                        var utarget = BasePlayer.FindByID(uid);
                        if (utarget != null)
                        {
                            CuiHelper.DestroyUi(player, "PListGUI");
                            ReportGUI(player, utarget);
                        }
                        else { PrintToChat(player, string.Format(GetMsg("ИгрокНеНайден"))); return; }
                    }
                    else { PrintToChat(player, string.Format(GetMsg("ИгрокНеНайден"))); return; }
                    break;
                case "page":
                    int page;
                    if (arg.Args.Length < 2) return;
                    if (!Int32.TryParse(arg.Args[1], out page)) return;
                    GUIManager.Get(player).Page = page;
                    CuiHelper.DestroyUi(player, "PListGUI");
                    PListUI(player);
                    break;
            }
        }
        private void PListUI(BasePlayer player)
        {
            string text = "Выберите игрока на которого хотите пожаловаться.";
            List<BasePlayer> players = new List<BasePlayer>();
            foreach (var pl in BasePlayer.activePlayerList)
            {
                if (pl == player) continue;
                players.Add(pl);
            }
            if (players.Count == 0) { PrintToChat(player, "На сервере нет никого кроме вас."); if (OpenReportUI.Contains(player)) OpenReportUI.Remove(player); return; }
            players = players.OrderBy(x => x.displayName).ToList();
            int maxPages = CalculatePages(players.Count);
            string pageNum = (maxPages > 1) ? $" - {GUIManager.Get(player).Page}" : "";
            CuiElementContainer container = new CuiElementContainer
            {
                BPanel("PListGUI", "0 0 0 0.75", "0.2 0.125", "0.8 0.9", "Hud", true),
                Panel("header", "0 0 0 0.75", "0 0.93", "1 1", "PListGUI")
            };
            if (maxPages != 1) text = text + " Страница " + pageNum.ToString();
            container.Add(Text("header", "1 1 1 1", text, TextAnchor.MiddleCenter, 20));
            container.Add(Button("close", "header", "vk.pllist close", "1 0 0 1", "0.94 0.01", "1.0 0.98"));
            container.Add(Text("close", "1 1 1 1", "X", TextAnchor.MiddleCenter, 20));
            container.Add(Panel("playerslist", "0 0 0 0.75", "0 0", "1 0.9", "PListGUI"));
            var page = GUIManager.Get(player).Page;
            int playerCount = (page * 100) - 100;
            for (int j = 0; j < 20; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (players.ToArray().Length <= playerCount) continue;
                    string AnchorMin = (0.2f * i).ToString() + " " + (1f - (0.05f * j) - 0.05f).ToString();
                    string AnchorMax = ((0.2f * i) + 0.2f).ToString() + " " + (1f - (0.05f * j)).ToString();
                    string id = players.ToArray()[playerCount].UserIDString;
                    container.Add(Panel($"pn{id}", "0 0 0 0", AnchorMin, AnchorMax, "playerslist"));
                    container.Add(Button($"plbtn{id}", $"pn{id}", $"vk.pllist report {id}", "0 0 0 0.85", "0.05 0.05", "0.95 0.95"));
                    container.Add(Text($"plbtn{id}", "1 1 1 1", UserName(players.ToArray()[playerCount].displayName), TextAnchor.MiddleCenter, 18));
                    playerCount++;
                }
            }
            if (page < maxPages)
            {
                container.Add(Button("npg", "PListGUI", $"vk.pllist page {(page + 1).ToString()}", "0 0 0 0.75", "1.025 0.575", "1.1 0.675"));
                container.Add(Text("npg", "1 1 1 1", ">>", TextAnchor.MiddleCenter, 16));
            }
            if (page > 1)
            {
                container.Add(Button("ppg", "PListGUI", $"vk.pllist page {(page - 1).ToString()}", "0 0 0 0.75", "1.025 0.45", "1.1 0.55"));
                container.Add(Text("ppg", "1 1 1 1", ">>", TextAnchor.MiddleCenter, 16));
            }
            CuiHelper.AddUi(player, container);
        }
        int CalculatePages(int value) => (int)Math.Ceiling(value / 100d);
        class GUIManager
        {
            public static Dictionary<BasePlayer, GUIManager> Players = new Dictionary<BasePlayer, GUIManager>();
            public int Page = 1;
            public static GUIManager Get(BasePlayer player)
            {
                if (Players.ContainsKey(player)) return Players[player];
                Players.Add(player, new GUIManager());
                return Players[player];
            }
        }
        #endregion

        #region Langs
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"ПоздравлениеИгрока", "<size=17><color=#049906>Администрация сервера поздравляет вас с днем рождения! В качестве подарка мы добавили вас в группу с рейтами x4 и китом bday!</color></size>"},
                {"ДеньРожденияИгрока", "<size=17>Администрация сервера поздравляет игрока <color=#049906>{0}</color> с Днем Рождения!</size>"},
                {"РепортОтправлен", "<size=17>Ваше сообщение было отправлено администратору.\n<color=#049906>ВНИМАНИЕ!</color>\n{0}</size>"},
                {"КомандаРепорт", "<size=17>Введите команду <color=#049906>/report сообщение</color>.\n<color=#049906>ВНИМАНИЕ!</color>\n{0}</size>"},
                {"ФункцияОтключена", "<size=17><color=#049906>Данная функция отключена администратором.</color>.</size>"},
                {"ПрофильДобавленИПодтвержден", "<size=17>Вы уже добавили и подтвердили свой профиль.</size>"},
                {"ПрофильДобавлен", "<size=17>Вы уже добавили свой профиль. Если вам не пришел код подтверждения, введите команду <color=#049906>/vk confirm</color></size>"},
                {"ДоступныеКоманды", "<size=17>Список доступных команд:\n /vk add ссылка на вашу страницу - добавление вашего профиля ВК в базу.\n /vk confirm - подтверждение вашего профиля ВК</size>"},
                {"НеправильнаяСсылка", "<size=17>Ссылка на страницу должна быть вида |vk.com/testpage| или |vk.com/id0000|</size>"},
                {"ПрофильПодтвержден", "<size=17>Вы подтвердили свой профиль! Спасибо!</size>"},
                {"ОповещениеОПодарках", "<size=17>Вы можете получить награду, если вступили в нашу группу <color=#049906>{0}</color> введя команду <color=#049906>/vk gift.</color></size>"},
                {"НеверныйКод", "<size=17>Неверный код подтверждения.</size>"},
                {"ПрофильНеДобавлен", "<size=17>Сначала добавьте и подтвердите свой профиль командой <color=#049906>/vk add ссылка на вашу страницу.</color> Ссылка на должна быть вида |vk.com/testpage| или |vk.com/id0000|</size>"},
                {"КодОтправлен", "<size=17>Вам был отправлен код подтверждения. Если сообщение не пришло, зайдите в группу <color=#049906>{0}</color> и напишите любое сообщение. После этого введите команду <color=#049906>/vk confirm</color></size>"},
                {"ПрофильНеПодтвержден", "<size=17>Сначала подтвердите свой профиль ВК командой <color=#049906>/vk confirm</color></size>"},
                {"НаградаУжеПолучена", "<size=17>Вы уже получили свою награду.</size>"},
                {"ПодпискаОтключена", "<size=17>Вы <color=#049906>отключили</color> подписку на сообщения о вайпах сервера. Что бы включить подписку снова, введите команду <color=#049906>/vk wipealerts</color></size>"},
                {"ПодпискаВключена", "<size=17>Вы <color=#049906>включили</color> подписку на сообщения о вайпах сервера. Что бы отключить подписку, введите команду <color=#049906>/vk wipealerts</color></size>"},
                {"НаградаПолучена", "<size=17>Вы получили свою награду! Проверьте инвентарь!</size>"},
                {"ПолучилНаграду", "<size=17>Игрок <color=#049906>{0}</color> получил награду за вступление в группу <color=#049906>{1}.</color>\nХочешь тоже получить награду? Введи в чат команду <color=#049906>/vk gift</color>.</size>"},
                {"НетМеста", "<size=17>Недостаточно места для получения награды.</size>"},
                {"НаградаПолученаКоманда", "<size=17>За вступление в группу нашего сервера вы получили {0}</size>"},
                {"НеВступилВГруппу", "<size=17>Вы не являетесь участником группы <color=#049906>{0}</color></size>"},
                {"ОтветНаРепортЧат", "<size=17><color=#049906>Администратор ответил на ваше сообщение:</color>\n</size>"},
                {"ОтветНаРепортВК", "<size=17><color=#049906>Администратор ответил на ваше сообщение:</color>\n</size>"},
                {"ИгрокНеНайден", "<size=17>Игрок не найден</size>"},
                {"СообщениеИгрокуТопПромо", "Поздравляем! Вы Топ {0} по результатам этого вайпа! В качестве награды, вы получаете промокод {1} на баланс в нашем магазине! {2}"},
                {"АвтоОповещенияОвайпе", "<size=17>Сервер рассылает оповещения о вайпе всем. Подписка не требуется</size>"}
            }, this);
        }
        string GetMsg(string key) => lang.GetMessage(key, this);
        #endregion
    }
}