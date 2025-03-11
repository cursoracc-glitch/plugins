using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("VKBot", "SkiTles", "1.6")]
    class VKBot : RustPlugin
    {
        //Данный плагин принадлежит группе vk.com/vkbotrust
        //Данный плагин предоставляется в существующей форме,
        //"как есть", без каких бы то ни было явных или
        //подразумеваемых гарантий, разработчик не несет
        //ответственность в случае его неправильного использования.

        #region Variables
        private System.Random random = new System.Random();
        private string opc;
        private string stw;
        private string sts;
        private string str;
        private string stb;
        private string ste;
        private string wd;
        private string su;
        private string msg;
        private string cone;
        private string slprs;
        private string mapfile;
        private bool NewWipe = false;
        JsonSerializerSettings jsonsettings;
        private bool OxideUpdateNotice = false;
        private Dictionary<ulong, ulong> PlayersCheckList = new Dictionary<ulong, ulong>();
        private int serverOxideVersion;
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
            "wall.frame.cell"
        };
        private List<ulong> BDayPlayers = new List<ulong>();
        #endregion

        #region Config
        private ConfigData config;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Ключи VK API, ID группы")]
            public VKAPITokens VKAPIT { get; set; }

            [JsonProperty(PropertyName = "Настройки оповещений администраторов")]
            public AdminNotify AdmNotify { get; set; }

            [JsonProperty(PropertyName = "Настройки статуса")]
            public StatusSettings StatusStg { get; set; }

            [JsonProperty(PropertyName = "Оповещения при вайпе")]
            public WipeSettings WipeStg { get; set; }

            [JsonProperty(PropertyName = "Награда за вступление в группу")]
            public GroupGifts GrGifts { get; set; }

            [JsonProperty(PropertyName = "Награда для именинников")]
            public BDayGiftSet BDayGift { get; set; }

            [JsonProperty(PropertyName = "Настройки текста в уведомлениях в чат")]
            public TextSettings TxtSet { get; set; }

            [JsonProperty(PropertyName = "Поддержка нескольких серверов")]
            public MultipleServersSettings MltServSet { get; set; }

            [JsonProperty(PropertyName = "Проверка игроков на читы")]
            public PlayersCheckingSettings PlChkSet { get; set; }

            [JsonProperty(PropertyName = "Топ игроки вайпа и промо")]
            public TopWPlPromoSet TopWPlayersPromo { get; set; }

            public class VKAPITokens
            {
                [JsonProperty(PropertyName = "VK Token группы (для сообщений)")]
                [DefaultValue("Заполните эти поля, и выполните команду o.reload VKBot")]
                public string VKToken { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";

                [JsonProperty(PropertyName = "VK Token приложения (для записей на стене и статуса)")]
                [DefaultValue("Заполните эти поля, и выполните команду o.reload VKBot")]
                public string VKTokenApp { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";

                [JsonProperty(PropertyName = "VKID группы")]
                [DefaultValue("Заполните эти поля, и выполните команду o.reload VKBot")]
                public string GroupID { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";
            }

            public class AdminNotify
            {
                [JsonProperty(PropertyName = "VkID администраторов (пример /11111, 22222/)")]
                [DefaultValue("Заполните эти поля, и выполните команду o.reload VKBot")]
                public string VkID { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";

                [JsonProperty(PropertyName = "Включить отправку сообщений администратору командой /report ?")]
                [DefaultValue(true)]
                public bool SendReports { get; set; } = true;

                [JsonProperty(PropertyName = "Предупреждение о злоупотреблении функцией репортов")]
                [DefaultValue("Наличие в тексте нецензурных выражений, оскорблений администрации или игроков сервера, а так же большое количество безсмысленных сообщений приведет к бану!")]
                public string ReportsNotify { get; set; } = "Наличие в тексте нецензурных выражений, оскорблений администрации или игроков сервера, а так же большое количество безсмысленных сообщений приведет к бану!";

                [JsonProperty(PropertyName = "Отправлять сообщение администратору о бане игрока?")]
                [DefaultValue(true)]
                public bool UserBannedMsg { get; set; } = true;

                [JsonProperty(PropertyName = "Отправлять сообщение администратору о выходе нового обновления Oxide?")]
                [DefaultValue(true)]
                public bool OxideNewVersionMsg { get; set; } = true;
            }

            public class StatusSettings
            {
                [JsonProperty(PropertyName = "Обновлять статус в группе? Если стоит /false/ статистика собираться не будет")]
                [DefaultValue(true)]
                public bool UpdateStatus { get; set; } = true;

                [JsonProperty(PropertyName = "Вид статуса (1 - текущий сервер, 2 - список серверов, необходим Rust:IO на каждом сервере)")]
                [DefaultValue(1)]
                public int StatusSet { get; set; } = 1;

                [JsonProperty(PropertyName = "Онлайн в статусе вида '125/200'")]
                [DefaultValue(false)]
                public bool OnlWmaxslots { get; set; } = false;

                [JsonProperty(PropertyName = "Таймер обновления статуса (минуты)")]
                [DefaultValue(30)]
                public int UpdateTimer { get; set; } = 30;

                [JsonProperty(PropertyName = "Формат статуса")]
                [DefaultValue("{usertext}. Сервер вайпнут: {wipedate}. Онлайн игроков: {onlinecounter}. Спящих: {sleepers}. Добыто дерева: {woodcounter}. Добыто серы: {sulfurecounter}. Выпущено ракет: {rocketscounter}. Использовано взрывчатки: {explosivecounter}. Создано чертежей: {blueprintsconter}. {connect}")]
                public string StatusText { get; set; } = "{usertext}. Сервер вайпнут: {wipedate}. Онлайн игроков: {onlinecounter}. Добыто дерева: {woodcounter}. Добыто серы: {sulfurecounter}. Выпущено ракет: {rocketscounter}. Использовано взрывчатки: {explosivecounter}. Создано чертежей: {blueprintsconter}. {connect}";

                [JsonProperty(PropertyName = "Список счетчиков, которые будут отображаться в виде emoji")]
                [DefaultValue("onlinecounter, rocketscounter, blueprintsconter, explosivecounter, wipedate")]
                public string EmojiCounterList { get; set; } = "onlinecounter, rocketscounter, blueprintsconter, explosivecounter, wipedate";

                [JsonProperty(PropertyName = "Ссылка на коннект сервера вида /connect 111.111.111.11:11111/")]
                [DefaultValue("connect 111.111.111.11:11111")]
                public string connecturl { get; set; } = "connect 111.111.111.11:11111";

                [JsonProperty(PropertyName = "Текст для статуса")]
                [DefaultValue("Сервер 1")]
                public string StatusUT { get; set; } = "Сервер 1";
            }

            public class WipeSettings
            {
                [JsonProperty(PropertyName = "Отправлять пост в группу после вайпа?")]
                [DefaultValue(false)]
                public bool WPostB { get; set; } = false;

                [JsonProperty(PropertyName = "Текст поста о вайпе")]
                [DefaultValue("Заполните эти поля, и выполните команду o.reload VKBot")]
                public string WPostMsg { get; set; } = "Заполните эти поля, и выполните команду o.reload VKBot";

                [JsonProperty(PropertyName = "Добавить изображение к посту о вайпе?")]
                [DefaultValue(false)]
                public bool WPostAttB { get; set; } = false;

                [JsonProperty(PropertyName = "Ссылка на изображение к посту о вайпе вида 'photo-1_265827614' (изображение должно быть в альбоме группы)")]
                [DefaultValue("photo-1_265827614")]
                public string WPostAtt { get; set; } = "photo-1_265827614";

                [JsonProperty(PropertyName = "Отправлять сообщение администратору о вайпе?")]
                [DefaultValue(true)]
                public bool WPostMsgAdmin { get; set; } = true;

                [JsonProperty(PropertyName = "Отправлять игрокам сообщение о вайпе автоматически?")]
                [DefaultValue(false)]
                public bool WMsgPlayers { get; set; } = false;

                [JsonProperty(PropertyName = "Текст сообщения игрокам о вайпе (сообщение отправляется только тем кто подписался командой /vk wipealerts)")]
                [DefaultValue("Сервер вайпнут! Залетай скорее!")]
                public string WMsgText { get; set; } = "Сервер вайпнут! Залетай скорее!";

                [JsonProperty(PropertyName = "Игнорировать команду /vk wipealerts? (если включено, сообщение о вайпе будет отправляться всем)")]
                [DefaultValue(false)]
                public bool WCMDIgnore { get; set; } = false;
            }

            public class GroupGifts
            {
                [JsonProperty(PropertyName = "Выдавать подарок игроку за вступление в группу ВК?")]
                [DefaultValue(true)]
                public bool VKGroupGifts { get; set; } = true;

                [JsonProperty(PropertyName = "Подарки за вступление в группу (shortname предмета, количество)")]
                [DefaultValue(null)]
                public Dictionary<string, object> VKGroupGiftList { get; set; } = new Dictionary<string, object>
                {
                  {"supply.signal", 1},
                  {"pookie.bear", 2}
                };

                [JsonProperty(PropertyName = "Ссылка на группу ВК")]
                [DefaultValue("vk.com/1234")]
                public string VKGroupUrl { get; set; } = "vk.com/1234";

                [JsonProperty(PropertyName = "Оповещения в общий чат о получении награды")]
                [DefaultValue(true)]
                public bool GiftsBool { get; set; } = true;

                [JsonProperty(PropertyName = "Включить оповещения для игроков не получивших награду за вступление в группу?")]
                [DefaultValue(true)]
                public bool VKGGNotify { get; set; } = true;

                [JsonProperty(PropertyName = "Интервал оповещений для игроков не получивших награду за вступление в группу (в минутах)")]
                [DefaultValue(30)]
                public int VKGGTimer { get; set; } = 30;

                [JsonProperty(PropertyName = "Выдавать награду каждый вайп?")]
                [DefaultValue(true)]
                public bool GiftsWipe { get; set; } = true;
            }

            public class BDayGiftSet
            {
                [JsonProperty(PropertyName = "Включить награду для именинников?")]
                [DefaultValue(true)]
                public bool BDayEnabled { get; set; } = true;

                [JsonProperty(PropertyName = "Группа для именинников")]
                [DefaultValue("bdaygroup")]
                public string BDayGroup { get; set; } = "bdaygroup";

                [JsonProperty(PropertyName = "Текст поздравления")]
                [DefaultValue("<size=17><color=#990404>Администрация сервера поздравляет вас с днем рождения! В качестве подарка мы добавили вас в группу с рейтами x4 и китом bday!</color></size>")]
                public string BDayText { get; set; } = "<size=17><color=#990404>Администрация сервера поздравляет вас с днем рождения! В качестве подарка мы добавили вас в группу с рейтами x4 и китом bday!</color></size>";

                [JsonProperty(PropertyName = "Оповещения в общий чат о имениннках")]
                [DefaultValue(false)]
                public bool BDayNotify { get; set; } = false;
            }

            public class TextSettings
            {
                [JsonProperty(PropertyName = "Размер текста")]
                [DefaultValue("17")]
                public string TxtSize { get; set; } = "17";

                [JsonProperty(PropertyName = "Цвет выделенного текста")]
                [DefaultValue("#990404")]
                public string TxtColor { get; set; } = "#990404";
            }

            public class MultipleServersSettings
            {
                [JsonProperty(PropertyName = "Включить поддержку несколько серверов?")]
                [DefaultValue(false)]
                public bool MSSEnable { get; set; } = false;

                [JsonProperty(PropertyName = "Номер сервера")]
                [DefaultValue(1)]
                public int ServerNumber { get; set; } = 1;

                [JsonProperty(PropertyName = "Сервер 1 IP:PORT (пример: 111.111.111.111:28015)")]
                [DefaultValue("none")]
                public string Server1ip { get; set; } = "none";

                [JsonProperty(PropertyName = "Сервер 2 IP:PORT (пример: 111.111.111.111:28015)")]
                [DefaultValue("none")]
                public string Server2ip { get; set; } = "none";

                [JsonProperty(PropertyName = "Сервер 3 IP:PORT (пример: 111.111.111.111:28015)")]
                [DefaultValue("none")]
                public string Server3ip { get; set; } = "none";

                [JsonProperty(PropertyName = "Сервер 4 IP:PORT (пример: 111.111.111.111:28015)")]
                [DefaultValue("none")]
                public string Server4ip { get; set; } = "none";

                [JsonProperty(PropertyName = "Сервер 5 IP:PORT (пример: 111.111.111.111:28015)")]
                [DefaultValue("none")]
                public string Server5ip { get; set; } = "none";

                [JsonProperty(PropertyName = "Онлайн в emoji?")]
                [DefaultValue(true)]
                public bool EmojiStatus { get; set; } = true;
            }

            public class PlayersCheckingSettings
            {
                [JsonProperty(PropertyName = "Текст уведомления")]
                [DefaultValue("<color=#990404>Модератор вызвал вас на проверку.</color> \nНапишите свой скайп с помощью команды <color=#990404>/skype <НИК в СКАЙПЕ>.</color>\nЕсли вы покините сервер, Вы будете забанены на нашем проекте серверов.")]
                public string PlCheckText { get; set; } = "<color=#990404>Модератор вызвал вас на проверку.</color> \nНапишите свой скайп с помощью команды <color=#990404>/skype <НИК в СКАЙПЕ>.</color>\nЕсли вы покините сервер, Вы будете забанены на нашем проекте серверов.";

                [JsonProperty(PropertyName = "Размер текста")]
                [DefaultValue(17)]
                public int PlCheckSize { get; set; } = 17;

                [JsonProperty(PropertyName = "Привилегия для команд /alert и /unalert")]
                [DefaultValue("vkbot.checkplayers")]
                public string PlCheckPerm { get; set; } = "vkbot.checkplayers";

                [JsonProperty(PropertyName = "Позиция GUI AnchorMin (дефолт 0 0.826)")]
                [DefaultValue("0 0.826")]
                public string GUIAnchorMin { get; set; } = "0 0.826";

                [JsonProperty(PropertyName = "Позиция GUI AnchorMax (дефолт 1 0.965)")]
                [DefaultValue("1 0.965")]
                public string GUIAnchorMax { get; set; } = "1 0.965";
            }

            public class TopWPlPromoSet
            {
                [JsonProperty(PropertyName = "Включить топ игроков вайпа")]
                [DefaultValue(true)]
                public bool TopWPlEnabled { get; set; } = true;

                [JsonProperty(PropertyName = "Включить отправку промо кодов за топ?")]
                [DefaultValue(false)]
                public bool TopPlPromoGift { get; set; } = false;

                [JsonProperty(PropertyName = "Пост на стене группы о топ игроках вайпа")]
                [DefaultValue(true)]
                public bool TopPlPost { get; set; } = true;

                [JsonProperty(PropertyName = "Промо для топ рэйдера")]
                [DefaultValue("topraider")]
                public string TopRaiderPromo { get; set; } = "topraider";

                [JsonProperty(PropertyName = "Промо для топ килера")]
                [DefaultValue("topkiller")]
                public string TopKillerPromo { get; set; } = "topkiller";

                [JsonProperty(PropertyName = "Промо для топ фармера")]
                [DefaultValue("topfarmer")]
                public string TopFarmerPromo { get; set; } = "topfarmer";

                [JsonProperty(PropertyName = "Ссылка на донат магазин")]
                [DefaultValue("server.gamestores.ru")]
                public string StoreUrl { get; set; } = "server.gamestores.ru";

                [JsonProperty(PropertyName = "Автоматическая генерация промокодов после вайпа")]
                [DefaultValue(false)]
                public bool GenRandomPromo { get; set; } = false;
            }
        }
        private void LoadVariables()
        {
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;

            config = Config.ReadObject<ConfigData>();

            Config.WriteObject(config, true);
        }

        protected override void LoadDefaultConfig()
        {
            var configData = new ConfigData
            {
                VKAPIT = new ConfigData.VKAPITokens(),
                AdmNotify = new ConfigData.AdminNotify(),
                StatusStg = new ConfigData.StatusSettings(),
                WipeStg = new ConfigData.WipeSettings(),
                GrGifts = new ConfigData.GroupGifts(),
                BDayGift = new ConfigData.BDayGiftSet(),
                TxtSet = new ConfigData.TextSettings(),
                MltServSet = new ConfigData.MultipleServersSettings(),
                PlChkSet = new ConfigData.PlayersCheckingSettings(),
                TopWPlayersPromo = new ConfigData.TopWPlPromoSet()
            };
            Config.WriteObject(configData, true);
            PrintWarning("Поддержи разработчика! Вступи в группу vk.com/vkbotrust");
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
        }
        DataStorageStats statdata;
        DataStorageUsers usersdata;
        private DynamicConfigFile VKBData;
        private DynamicConfigFile StatData;
        void LoadData()
        {
            try
            {
                statdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageStats>("VKBot");
                usersdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageUsers>("VKBotUsers");
            }

            catch
            {
                statdata = new DataStorageStats();
                usersdata = new DataStorageUsers();
            }
        }
        #endregion

        #region Oxidehooks
        void OnServerInitialized()
        {
            LoadVariables();
            VKBData = Interface.Oxide.DataFileSystem.GetFile("VKBotUsers");
            StatData = Interface.Oxide.DataFileSystem.GetFile("VKBot");
            LoadData();
            if (!permission.PermissionExists(config.PlChkSet.PlCheckPerm)) permission.RegisterPermission(config.PlChkSet.PlCheckPerm, this);
            if (NewWipe)
            {
                WipeFunctions(mapfile);
            }
            if (config.StatusStg.UpdateStatus)
            {
                if (config.StatusStg.StatusSet == 1)
                {
                    timer.Repeat(config.StatusStg.UpdateTimer * 60, 0, Update1ServerStatus);
                }
                if (config.StatusStg.StatusSet == 2)
                {
                    timer.Repeat(config.StatusStg.UpdateTimer * 60, 0, UpdateMultiServerStatus);
                }
            }
            if (config.GrGifts.VKGGNotify)
            {
                timer.Repeat(config.GrGifts.VKGGTimer * 60, 0, GiftNotifier);
            }
            if (config.AdmNotify.OxideNewVersionMsg)
            {
                CheckOxideCommits();
                timer.Repeat(3600, 0, CheckOxideCommits);
            }
        }
        void OnServerSave()
        {
            if (config.TopWPlayersPromo.TopWPlEnabled)
            {
                VKBData.WriteObject(usersdata);
            }
        }
        private void Init()
        {
            cmd.AddChatCommand("report", this, "SendReport");
            cmd.AddChatCommand("vk", this, "VKcommand");
            cmd.AddConsoleCommand("updatestatus", this, "UStatus");
            cmd.AddConsoleCommand("sendmsgadmin", this, "MsgAdmin");
            cmd.AddConsoleCommand("wipealerts", this, "WipeAlerts");
            cmd.AddChatCommand("alert", this, "StartCheckPlayer");
            cmd.AddChatCommand("unalert", this, "StopCheckPlayer");
            cmd.AddChatCommand("skype", this, "SkypeSending");
            cmd.AddConsoleCommand("userinfo", this, "GetUserInfo");
            jsonsettings = new JsonSerializerSettings();
            jsonsettings.Converters.Add(new KeyValuePairConverter());
            serverOxideVersion = Convert.ToInt32(Manager.GetPlugin("RustCore").Version.Patch);
        }
        void Unload()
        {
            if (config.StatusStg.UpdateStatus)
            {
                StatData.WriteObject(statdata);
            }
            if (config.TopWPlayersPromo.TopWPlEnabled)
            {
                VKBData.WriteObject(usersdata);
            }
            if (PlayersCheckList.Count == 0) return;
            for (int i = 0; i < PlayersCheckList.Count; i++)
            {
                var playerid = PlayersCheckList.ElementAt(i).Value;
                var player = BasePlayer.FindByID(playerid);
                if (player != null)
                {
                    StopGui(player);
                }
            }
            PlayersCheckList.Clear();
            if (config.BDayGift.BDayEnabled && BDayPlayers.Count > 0)
            {
                foreach (var id in BDayPlayers)
                {
                    permission.RemoveUserGroup(id.ToString(), config.BDayGift.BDayGroup);
                    Puts($"Список игроков bday очищен"); //debug
                }
                BDayPlayers.Clear();
            }
        }
        void OnNewSave(string filename)
        {
            NewWipe = true;
            mapfile = filename;
        }
        void OnPlayerInit(BasePlayer player)
        {
            if (usersdata.VKUsersData.ContainsKey(player.userID))
            {
                if (usersdata.VKUsersData[player.userID].Name != player.displayName)
                {
                    usersdata.VKUsersData[player.userID].Name = player.displayName;
                    VKBData.WriteObject(usersdata);
                }
                if (usersdata.VKUsersData[player.userID].Bdate == null)
                {
                    AddBdate(player);
                }
            }
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!usersdata.VKUsersData.ContainsKey(player.userID)) return;
            if (config.BDayGift.BDayEnabled && permission.GroupExists(config.BDayGift.BDayGroup))
            {
                if (permission.UserHasGroup(player.userID.ToString(), config.BDayGift.BDayGroup)) return;
                var today = DateTime.Now.ToString("d.M", CultureInfo.InvariantCulture);
                var bday = usersdata.VKUsersData[player.userID].Bdate;
                if (bday == null || bday == "noinfo") return;
                string[] array = bday.Split('.');
                if (array.Length == 3)
                {
                    bday = bday.Remove(bday.Length - 5, 5);
                }
                if (bday == today)
                {
                    permission.AddUserGroup(player.userID.ToString(), config.BDayGift.BDayGroup);
                    PrintToChat(player, String.Format(config.BDayGift.BDayText));
                    Log("bday", $"Игрок {player.displayName} добавлен в группу {config.BDayGift.BDayGroup}");
                    BDayPlayers.Add(player.userID);
                    if (config.BDayGift.BDayNotify)
                    {
                        Server.Broadcast(String.Format($"<size={config.TxtSet.TxtSize}>Администрация сервера поздравляет игрока <color={config.TxtSet.TxtColor}>{player.displayName}</color> с Днем Рождения!</size>"));
                    }
                }
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (PlayersCheckList.Count > 0 && PlayersCheckList.ContainsValue(player.userID))
            {
                CuiHelper.DestroyUi(player, "AlertGUI");
                BasePlayer moder = null;
                for (int i = 0; i < PlayersCheckList.Count; i++)
                {
                    if (PlayersCheckList.ElementAt(i).Value == player.userID)
                    {
                        ulong moderid = PlayersCheckList.ElementAt(i).Key;
                        moder = BasePlayer.FindByID(moderid);
                        PlayersCheckList.Remove(moderid);
                        if (moder != null)
                        {
                            PrintToChat(moder, $"Игрок вызванный на проверку покинул сервер. Причина: {reason}");
                        }
                    }
                }
            }
            if (PlayersCheckList.Count > 0 && PlayersCheckList.ContainsKey(player.userID))
            {
                ulong targetid;
                PlayersCheckList.TryGetValue(player.userID, out targetid);
                BasePlayer target = BasePlayer.FindByID(targetid);
                if (target != null)
                {
                    CuiHelper.DestroyUi(target, "AlertGUI");
                    PrintToChat(target, $"Модератор отключился от сервера, ожидайте следующей проверки.");
                }
                PlayersCheckList.Remove(player.userID);
            }
            if (config.BDayGift.BDayEnabled && permission.GroupExists(config.BDayGift.BDayGroup))
            {
                if (BDayPlayers.Contains(player.userID))
                {
                    permission.RemoveUserGroup(player.userID.ToString(), config.BDayGift.BDayGroup);
                    BDayPlayers.Remove(player.userID);
                    Log("bday", $"Игрок {player.displayName} удален из группы {config.BDayGift.BDayGroup}");
                }
            }
        }
        #endregion

        #region Stats
        private void OnItemResearch(ResearchTable table, Item targetItem, BasePlayer player)
        {
            if (config.StatusStg.UpdateStatus)
            {
                statdata.Blueprints++;
            }
        }
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (config.StatusStg.UpdateStatus)
            {
                if (item.info.shortname == "wood")
                {
                    statdata.WoodGath = statdata.WoodGath + item.amount;
                }
                if (item.info.shortname == "sulfur.ore")
                {
                    statdata.SulfureGath = statdata.SulfureGath + item.amount;
                }
            }
            if (config.TopWPlayersPromo.TopWPlEnabled)
            {
                BasePlayer player = entity.ToPlayer();
                if (player == null) return;
                if (usersdata.VKUsersData.ContainsKey(player.userID))
                {
                    usersdata.VKUsersData[player.userID].Farm = usersdata.VKUsersData[player.userID].Farm + item.amount;
                }
            }
        }
        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (config.StatusStg.UpdateStatus && item.info.shortname == "sulfur.ore")
            {
                statdata.SulfureGath = statdata.SulfureGath + item.amount;
            }
            if (config.TopWPlayersPromo.TopWPlEnabled)
            {
                if (usersdata.VKUsersData.ContainsKey(player.userID))
                {
                    usersdata.VKUsersData[player.userID].Farm = usersdata.VKUsersData[player.userID].Farm + item.amount;
                }
            }
        }
        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (config.StatusStg.UpdateStatus)
            {
                if (item.info.shortname == "wood")
                {
                    statdata.WoodGath = statdata.WoodGath + item.amount;
                }
                if (item.info.shortname == "sulfur.ore")
                {
                    statdata.SulfureGath = statdata.SulfureGath + item.amount;
                }
            }
            if (config.TopWPlayersPromo.TopWPlEnabled)
            {
                if (usersdata.VKUsersData.ContainsKey(player.userID))
                {
                    usersdata.VKUsersData[player.userID].Farm = usersdata.VKUsersData[player.userID].Farm + item.amount;
                }
            }
        }
        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (config.StatusStg.UpdateStatus)
            {
                statdata.Rockets++;
            }
        }
        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (config.StatusStg.UpdateStatus)
            {
                List<object> include = new List<object>()
                {
                "explosive.satchel.deployed",
                "grenade.f1.deployed",
                "grenade.beancan.deployed",
                "explosive.timed.deployed"
                };
                if (include.Contains(entity.ShortPrefabName))
                {
                    statdata.Explosive++;
                }
            }
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (config.TopWPlayersPromo.TopWPlEnabled)
            {
                if (entity.name.Contains("corpse"))  return;
                if (hitInfo == null) return;
                var attacker = hitInfo.Initiator?.ToPlayer();
                if (attacker == null) return;
                if (entity is BasePlayer)
                {
                    CheckDeath(entity.ToPlayer(), hitInfo);
                }
                if (entity is BaseEntity)
                {
                    if (hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Explosion && hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Heat && hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Bullet) return;
                    if (attacker.userID == entity.OwnerID) return;
                    BuildingBlock block = entity.GetComponent<BuildingBlock>();
                    if (block != null)
                    {
                        if (block.currentGrade.gradeBase.type.ToString() == "Twigs" || block.currentGrade.gradeBase.type.ToString() == "Wood")
                        {
                            return;
                        }
                    }
                    else
                    {
                        bool ok = false;
                        foreach (var ent in allowedentity)
                        {
                            if (entity.LookupPrefab().name.Contains(ent))
                            {
                                ok = true;
                            }
                        }
                        if (!ok) return;
                    }
                    if (entity.OwnerID == 0) return;
                    if (usersdata.VKUsersData.ContainsKey(attacker.userID))
                    {
                        usersdata.VKUsersData[attacker.userID].Raids++;
                    }
                }                
            }
        }
        private void CheckDeath(BasePlayer player, HitInfo info)
        {
            if (info == null) return;
            var attacker = info.Initiator?.ToPlayer();
            if (attacker == null || attacker == player) return;
            if (!usersdata.VKUsersData.ContainsKey(attacker.userID)) return;
            if (!player.IsConnected) return;
            usersdata.VKUsersData[attacker.userID].Kills++;
        }
        #endregion

        #region Wipe
        private void WipeFunctions(string filename)
        {
            if (config.StatusStg.UpdateStatus)
            {
                statdata.Blueprints = 0;
                statdata.Rockets = 0;
                statdata.SulfureGath = 0;
                statdata.WoodGath = 0;
                statdata.Explosive = 0;
                StatData.WriteObject(statdata);
                NewWipe = false;
                if (config.StatusStg.StatusSet == 1) { Update1ServerStatus(); }
                if (config.StatusStg.StatusSet == 2) { UpdateMultiServerStatus(); }
            }
            if (config.WipeStg.WPostMsgAdmin)
            {
                string s = filename;
                string[] array = s.Split('/');
                int t = array.Length - 1;
                string savename = array[t];
                string[] mapname = savename.Split('.');
                string msg2 = null;
                if (config.MltServSet.MSSEnable)
                {
                    msg2 = $"[VKBot] Сервер {config.MltServSet.ServerNumber.ToString()} вайпнут. Установлена карта: {mapname[0]}. Размер: {mapname[1]}. Сид: {mapname[2]}";
                }
                else
                {
                    msg2 = $"[VKBot] Сервер вайпнут. Установлена карта: {mapname[0]}. Размер: {mapname[1]}. Сид: {mapname[2]}";
                }
                SendVkMessage(config.AdmNotify.VkID, msg2);
            }
            if (config.WipeStg.WPostB)
            {
                if (config.WipeStg.WPostAttB)
                {
                    SendVkWall($"{config.WipeStg.WPostMsg}&attachments={config.WipeStg.WPostAtt}");
                }
                else
                {
                    SendVkWall($"{config.WipeStg.WPostMsg}");
                }
            }
            if (config.GrGifts.GiftsWipe)
            {
                int amount = usersdata.VKUsersData.Count;
                if (amount != 0)
                {
                    for (int i = 0; i < amount; i++)
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
                    if (config.TopWPlayersPromo.TopPlPromoGift && config.TopWPlayersPromo.GenRandomPromo)
                    {
                        SetRandomPromo();
                    }
                }
                int amount = usersdata.VKUsersData.Count;
                if (amount != 0)
                {
                    for (int i = 0; i < amount; i++)
                    {
                        usersdata.VKUsersData.ElementAt(i).Value.Farm = 0;
                        usersdata.VKUsersData.ElementAt(i).Value.Kills = 0;
                        usersdata.VKUsersData.ElementAt(i).Value.Raids = 0;
                    }
                    VKBData.WriteObject(usersdata);
                }
            }
            if (config.WipeStg.WMsgPlayers)
            {
                WipeAlertsSend();
            }
        }
        private void WipeAlerts(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            WipeAlertsSend();
        }
        private void WipeAlertsSend()
        {
            List<string> UserList = new List<string>();
            string userlist = "";
            int usercount = 0;
            int amount = usersdata.VKUsersData.Count;
            if (amount != 0)
            {
                for (int i = 0; i < amount; i++)
                {
                    if (config.WipeStg.WCMDIgnore || usersdata.VKUsersData.ElementAt(i).Value.WipeMsg)
                    {
                        if (usercount == 100)
                        {
                            UserList.Add(userlist);
                            userlist = "";
                            usercount = 0;
                        }
                        if (usercount > 0)
                        {
                            userlist = userlist + ", ";
                        }
                        userlist = userlist + usersdata.VKUsersData.ElementAt(i).Value.VkID;
                        usercount++;
                    }
                }
            }
            if (userlist == "" && UserList.Count == 0) { PrintWarning($"Список адресатов рассылки о вайпе пуст."); return; }
            if (UserList.Count > 0)
            {
                foreach (var list in UserList)
                {
                    SendVkMessage(list, config.WipeStg.WMsgText);
                }
            }
            SendVkMessage(userlist, config.WipeStg.WMsgText);
        }
        #endregion

        #region MainMethods
        private void UStatus(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            if (config.StatusStg.UpdateStatus)
            {
                if (config.StatusStg.StatusSet == 1) { Update1ServerStatus(); }
                if (config.StatusStg.StatusSet == 2) { UpdateMultiServerStatus(); }
            }
            else
            {
                PrintWarning($"Функция обновления статуса отключена. Status update disabled");
            }
        }
        void OnPlayerBanned(string name, ulong id, string address, string reason)
        {
            if (config.AdmNotify.UserBannedMsg)
            {
                string msg2 = null;
                if (config.MltServSet.MSSEnable)
                {
                    msg2 = $"[Сервер {config.MltServSet.ServerNumber.ToString()}] Игрок {name} ({id}) был забанен на сервере. Причина: {reason}. Ссылка на профиль стим: steamcommunity.com/profiles/{id}/";
                }
                else
                {
                    msg2 = $"Игрок {name} ({id}) был забанен на сервере. Причина: {reason}. Ссылка на профиль стим: steamcommunity.com/profiles/{id}/";
                }
                if (usersdata.VKUsersData.ContainsKey(id) && usersdata.VKUsersData[id].Confirmed)
                {
                    msg2 = msg2 + $" . Ссылка на профиль ВК: vk.com/id{usersdata.VKUsersData[id].VkID}";
                }
                SendVkMessage(config.AdmNotify.VkID, msg2);
            }
        }
        private void SendReport(BasePlayer player, string cmd, string[] args)
        {
            if (config.AdmNotify.SendReports)
            {
                if (args.Length > 0)
                {
                    string text = string.Join(" ", args.Skip(0).ToArray());
                    string reciverID = config.AdmNotify.VkID;
                    string reporttext = "[VKBot]";
                    if (config.MltServSet.MSSEnable)
                    {
                        reporttext = reporttext + " [Сервер " + config.MltServSet.ServerNumber.ToString() + "]\n";
                    }
                    reporttext = reporttext + player.displayName + "(" + player.UserIDString + ")";
                    if (usersdata.VKUsersData.ContainsKey(player.userID))
                    {
                        if (usersdata.VKUsersData[player.userID].Confirmed)
                        {
                            reporttext = reporttext + ". ВК: vk.com/id" + usersdata.VKUsersData[player.userID].VkID;
                        }
                        else
                        {
                            reporttext = reporttext + ". ВК: vk.com/id" + usersdata.VKUsersData[player.userID].VkID + " (не подтвержден)";
                        }
                    }
                    reporttext = reporttext + "\nСообщение: " + text;                    
                    SendVkMessage(reciverID, reporttext);
                    Log("Log", $"{player.displayName} ({player.userID}): написал администратору: {reporttext}");
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Ваше сообщение было отправлено администратору. \n <color={config.TxtSet.TxtColor}>ВНИМАНИЕ! </color> {config.AdmNotify.ReportsNotify}</size>"));
                }
                else
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Введите команду <color={config.TxtSet.TxtColor}>/report сообщение</color> . Текст сообщения будет отправлен администратору. \n <color={config.TxtSet.TxtColor}>ВНИМАНИЕ! </color> {config.AdmNotify.ReportsNotify}</size>"));
                    return;
                }
            }
            else
            {
                PrintToChat(player, String.Format("Данная функция отключена администратором."));
            }
        }
        private void AddBdate(BasePlayer player)
        {
            if (usersdata.VKUsersData[player.userID].Bdate != null) return;
            string Userid = null;
            string userid = usersdata.VKUsersData[player.userID].VkID;
            string url2 = "https://api.vk.com/method/users.get?user_ids=" + userid + "&v=5.69&fields=bdate&access_token=" + config.VKAPIT.VKToken;
            webrequest.Enqueue(url2, null, (code, response) => {
                var json = JObject.Parse(response);
                Userid = (string)json["response"][0]["id"];
                if (Userid == null) return;
                usersdata.VKUsersData[player.userID].Bdate = "noinfo";
                var bdate = (string)json["response"][0]["bdate"];
                if (bdate != null)
                {
                    usersdata.VKUsersData[player.userID].Bdate = bdate;
                }
                VKBData.WriteObject(usersdata);
            }, this);
        }
        private void CheckVkUser(BasePlayer player, string url)
        {
            string Userid = null;
            string[] arr1 = url.Split('/');
            int num = arr1.Length - 1;
            string vkname = arr1[num];
            string url2 = "https://api.vk.com/method/users.get?user_ids=" + vkname + "&v=5.69&fields=bdate&access_token=" + config.VKAPIT.VKToken;
            webrequest.Enqueue(url2, null, (code, response) => {
                var json = JObject.Parse(response);
                Userid = (string)json["response"][0]["id"];
                string bdate = null;
                bdate = (string)json["response"][0]["bdate"];
                if (Userid != null)
                {
                    AddVKUser(player, Userid, bdate);
                }
                else
                {
                    PrintToChat(player, "Ошибка обработки вашей ссылки ВК, обратитесь к администратору.");
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
                    Raids = 0
                });
                VKBData.WriteObject(usersdata);
                SendVkMessage(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}");
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вам был отправлен код подтверждения. Если сообщение не пришло, зайдите в группу <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}</color> и напишите любое сообщение. После этого введите команду <color={config.TxtSet.TxtColor}>/vk confirm</color></size>"));
            }
            else
            {
                if (Userid == usersdata.VKUsersData[player.userID].VkID && usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы уже добавили и подтвердили свой профиль.</size>")); return; }
                if (Userid == usersdata.VKUsersData[player.userID].VkID && !usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы уже добавили свой профиль. Если вам не пришел код подтверждения, введите команду <color={config.TxtSet.TxtColor}>/vk confirm</color></size>")); return; }
                usersdata.VKUsersData[player.userID].Name = player.displayName;
                usersdata.VKUsersData[player.userID].VkID = Userid;
                usersdata.VKUsersData[player.userID].Confirmed = false;
                usersdata.VKUsersData[player.userID].ConfirmCode = random.Next(1, 9999999);
                usersdata.VKUsersData[player.userID].Bdate = bdate;
                VKBData.WriteObject(usersdata);
                SendVkMessage(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}");
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вам был отправлен код подтверждения. Если сообщение не пришло, зайдите в группу <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}</color> и напишите любое сообщение. После этого введите команду <color={config.TxtSet.TxtColor}>/vk confirm</color></size>"));
            }
        }
        private void VKcommand(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "add")
                {
                    if (args.Length == 1) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Список доступных команд: \n /vk add ссылка на вашу страницу - добавление вашего профиля ВК в базу</size>")); return; }
                    if (!args[1].Contains("vk.com/")) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Ссылка на страницу должна быть вида |vk.com/testpage| или |vk.com/id0000|</size>")); return; }
                    CheckVkUser(player, args[1]);
                }
                if (args[0] == "confirm")
                {
                    if (args.Length >= 2)
                    {
                        if (usersdata.VKUsersData.ContainsKey(player.userID))
                        {
                            if (usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы уже подтвердили свой профиль.</size>")); return; }
                            if (args[1] == usersdata.VKUsersData[player.userID].ConfirmCode.ToString())
                            {
                                usersdata.VKUsersData[player.userID].Confirmed = true;
                                VKBData.WriteObject(usersdata);
                                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы подтвердили свой профиль! Спасибо!</size>"));
                                if (config.GrGifts.VKGroupGifts) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы можете получить награду, если вступили в нашу группу <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}</color> введя команду <color={config.TxtSet.TxtColor}>/vk gift</color></size>")); }
                            }
                            else
                            {
                                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Неверный код подтверждения.</size>"));
                            }
                        }
                        else
                        {
                            PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Сначала добавьте свой VK ID командой <color={config.TxtSet.TxtColor}>/vk add ссылка на вашу страницу</color></size>"));
                        }
                    }
                    else
                    {
                        if (usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы уже подтвердили свой профиль.</size>")); return; }
                        if (usersdata.VKUsersData.ContainsKey(player.userID))
                        {
                            SendVkMessage(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}");
                            PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вам был отправлен код подтверждения. Если сообщение не пришло, зайдите в группу <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}</color> и напишите любое сообщение. После этого введите команду <color={config.TxtSet.TxtColor}>/vk confirm</color></size>"));
                        }
                        else
                        {
                            PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Сначала добавьте свой VK ID командой <color={config.TxtSet.TxtColor}>/vk add ссылка на вашу страницу</color></size>"));
                        }
                    }
                }
                if (args[0] == "gift")
                {
                    if (config.GrGifts.VKGroupGifts)
                    {
                        if (!usersdata.VKUsersData.ContainsKey(player.userID)) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Сначала добавьте свой VK ID командой <color={config.TxtSet.TxtColor}>/vk add ссылка на вашу страницу</color></size>")); return; }
                        if (!usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Сначала подтвердите свой профиль ВК командой <color={config.TxtSet.TxtColor}>/vk confirm</color></size>")); return; }
                        if (usersdata.VKUsersData[player.userID].GiftRecived) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы уже получили свою награду.</size>")); return; }
                        string url = $"https://api.vk.com/method/groups.isMember?group_id={config.VKAPIT.GroupID}&user_id={usersdata.VKUsersData[player.userID].VkID}&v=5.69&access_token={config.VKAPIT.VKToken}";
                        webrequest.Enqueue(url, null, (code, response) => {
                            var json = JObject.Parse(response);
                            string Result = (string)json["response"];
                            GetGift(code, Result, player);
                        }, this);
                    }
                    else
                    {
                        PrintToChat(player, String.Format("Данная функция отключена администратором."));
                    }
                }
                if (args[0] == "wipealerts")
                {
                    if (!usersdata.VKUsersData.ContainsKey(player.userID)) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Сначала добавьте свой VK ID командой <color={config.TxtSet.TxtColor}>/vk add ссылка на вашу страницу</color></size>")); return; }
                    if (!usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Сначала подтвердите свой профиль ВК командой <color={config.TxtSet.TxtColor}>/vk confirm</color></size>")); return; }
                    if (usersdata.VKUsersData[player.userID].WipeMsg)
                    {
                        usersdata.VKUsersData[player.userID].WipeMsg = false;
                        PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы <color={config.TxtSet.TxtColor}>отключили</color> подписку на сообщения о вапах сервера. Что бы включить подписку снова, введите команду <color={config.TxtSet.TxtColor}>/vk wipealerts</color></size>"));
                    }
                    else
                    {
                        usersdata.VKUsersData[player.userID].WipeMsg = true;
                        PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы <color={config.TxtSet.TxtColor}>включили</color> подписку на сообщения о вапах сервера. Что бы отключить подписку, введите команду <color={config.TxtSet.TxtColor}>/vk wipealerts</color></size>"));
                    }
                }
                if (args[0] != "add" && args[0] != "gift" && args[0] != "confirm")
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Список доступных команд: \n /vk add ссылка на вашу страницу - добавление вашего профиля ВК в базу. \n /vk confirm - подтверждение вашего профиля ВК</size>"));
                    if (config.GrGifts.VKGroupGifts)
                    {
                        PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>/vk gift - получение награды за вступление в группу <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}</color></size>"));
                    }
                }
            }
            else
            {
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Список доступных команд: \n /vk add ссылка на вашу страницу - добавление вашего профиля ВК в базу. \n /vk confirm - подтверждение вашего профиля ВК</size>"));
                if (config.GrGifts.VKGroupGifts)
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>/vk gift - получение награды за вступление в группу <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}</color></size>"));
                }
            }
        }
        private void GetGift(int code, string Result, BasePlayer player)
        {
            if (Result == "1")
            {
                int FreeSlots = 24 - player.inventory.containerMain.itemList.Count;
                if (FreeSlots >= config.GrGifts.VKGroupGiftList.Count)
                {
                    usersdata.VKUsersData[player.userID].GiftRecived = true;
                    VKBData.WriteObject(usersdata);
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы получили свою награду! Проверьте инвентарь!</size>"));
                    if (config.GrGifts.GiftsBool)
                    {
                        Server.Broadcast(String.Format($"<size={config.TxtSet.TxtSize}>Игрок <color={config.TxtSet.TxtColor}>{player.displayName}</color> получил награду за вступление в группу <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}.</color> \n Хочешь тоже получить награду? Введи в чат команду <color={config.TxtSet.TxtColor}>/vk gift</color>.</size>"));
                    }
                    for (int i = 0; i < config.GrGifts.VKGroupGiftList.Count; i++)
                    {
                        if (Convert.ToInt32(config.GrGifts.VKGroupGiftList.ElementAt(i).Value) > 0)
                        {
                            Item gift = ItemManager.CreateByName(config.GrGifts.VKGroupGiftList.ElementAt(i).Key, Convert.ToInt32(config.GrGifts.VKGroupGiftList.ElementAt(i).Value));
                            gift.MoveToContainer(player.inventory.containerMain, -1, false);
                        }
                    }
                }
                else
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Недостаточно места для получения награды.</size>"));
                }
            }
            else
            {
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы не являетесь участником группы <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}</color></size>"));
            }
        }
        private void GiftNotifier()
        {
            if (config.GrGifts.VKGroupGifts)
            {
                foreach (var pl in BasePlayer.activePlayerList)
                {
                    if (!usersdata.VKUsersData.ContainsKey(pl.userID))
                    {
                        PrintToChat(pl, String.Format($"<size={config.TxtSet.TxtSize}>Вступите в нашу группу <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}</color> и получите награду! \n Введите команду <color={config.TxtSet.TxtColor}>/vk gift</color></size>"));
                    }
                    else
                    {
                        if (!usersdata.VKUsersData[pl.userID].GiftRecived)
                        {
                            PrintToChat(pl, String.Format($"<size={config.TxtSet.TxtSize}>Вступите в нашу группу <color={config.TxtSet.TxtColor}>{config.GrGifts.VKGroupUrl}</color> и получите награду! \n Введите команду <color={config.TxtSet.TxtColor}>/vk gift</color></size>"));
                        }
                    }
                }
            }
        }
        void Update1ServerStatus()
        {
            var sleepPlayers = BasePlayer.sleepingPlayerList.Count.ToString();
            var onlcount = GetOnline();
            if (config.StatusStg.UpdateStatus)
            {
                StatData.WriteObject(statdata);
                if (config.StatusStg.EmojiCounterList.Contains("onlinecounter"))
                {
                    opc = EmojiCounters(onlcount);
                }
                else
                {
                    opc = onlcount;
                }
                if (config.StatusStg.EmojiCounterList.Contains("sleepers"))
                {
                    slprs = EmojiCounters(sleepPlayers);
                }
                else
                {
                    slprs = sleepPlayers;
                }
                if (config.StatusStg.EmojiCounterList.Contains("woodcounter"))
                {
                    stw = EmojiCounters(statdata.WoodGath.ToString());
                }
                else
                {
                    stw = statdata.WoodGath.ToString();
                }
                if (config.StatusStg.EmojiCounterList.Contains("sulfurecounter"))
                {
                    sts = EmojiCounters(statdata.SulfureGath.ToString());
                }
                else
                {
                    sts = statdata.SulfureGath.ToString();
                }
                if (config.StatusStg.EmojiCounterList.Contains("rocketscounter"))
                {
                    str = EmojiCounters(statdata.Rockets.ToString());
                }
                else
                {
                    str = statdata.Rockets.ToString();
                }
                if (config.StatusStg.EmojiCounterList.Contains("blueprintsconter"))
                {
                    stb = EmojiCounters(statdata.Blueprints.ToString());
                }
                else
                {
                    stb = statdata.Blueprints.ToString();
                }
                if (config.StatusStg.EmojiCounterList.Contains("explosivecounter"))
                {
                    ste = EmojiCounters(statdata.Explosive.ToString());
                }
                else
                {
                    ste = statdata.Explosive.ToString();
                }
                if (config.StatusStg.EmojiCounterList.Contains("wipedate"))
                {
                    wd = EmojiCounters((string)WipeDate());
                }
                else
                {
                    wd = (string)WipeDate();
                }
                if (config.StatusStg.EmojiCounterList.Contains("usertext"))
                {
                    su = EmojiCounters(config.StatusStg.StatusUT);
                }
                else
                {
                    su = config.StatusStg.StatusUT;
                }
                if (config.StatusStg.EmojiCounterList.Contains("connect"))
                {
                    cone = EmojiCounters(config.StatusStg.connecturl);
                }
                else
                {
                    cone = config.StatusStg.connecturl;
                }
                SendStatus(config.StatusStg.StatusText,
                    new KeyValuePair<string, string>("onlinecounter", opc),
                    new KeyValuePair<string, string>("sleepers", slprs),
                    new KeyValuePair<string, string>("woodcounter", stw),
                    new KeyValuePair<string, string>("sulfurecounter", sts),
                    new KeyValuePair<string, string>("rocketscounter", str),
                    new KeyValuePair<string, string>("blueprintsconter", stb),
                    new KeyValuePair<string, string>("explosivecounter", ste),
                    new KeyValuePair<string, string>("connect", cone),
                    new KeyValuePair<string, string>("wipedate", wd),
                    new KeyValuePair<string, string>("usertext", su));
            }
        }
        void UpdateMultiServerStatus()
        {
            string text = "";
            string server1 = "";
            string server2 = "";
            string server3 = "";
            string server4 = "";
            string server5 = "";
            if (config.MltServSet.Server1ip != "none")
            {
                var url = "http://" + config.MltServSet.Server1ip + "/status.json";
                webrequest.Enqueue(url, null, (code, response) => {
                    if (response != null || code == 200)
                    {

                        var jsonresponse3 = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
                        if (!(jsonresponse3 is Dictionary<string, object>) || jsonresponse3.Count == 0 || !jsonresponse3.ContainsKey("players") || !jsonresponse3.ContainsKey("maxplayers")) return;
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus) { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        server1 = "1⃣: " + online.ToString() + "/" + slots.ToString();
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
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus) { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        server2 = ", 2⃣: " + online.ToString() + "/" + slots.ToString();
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
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus) { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        server3 = ", 3⃣: " + online.ToString() + "/" + slots.ToString();
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
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus) { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        server4 = ", 4⃣: " + online.ToString() + "/" + slots.ToString();
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
                        string online = jsonresponse3["players"].ToString();
                        string slots = jsonresponse3["maxplayers"].ToString();
                        if (config.MltServSet.EmojiStatus) { online = EmojiCounters(online); slots = EmojiCounters(slots); }
                        server5 = ", 5⃣: " + online.ToString() + "/" + slots.ToString();
                    }
                }, this);
            }
            Puts("Обработка данных. Статус будет отправлен через 10 секунд.");
            timer.Once(10f, () =>
            {
                text = server1 + server2 + server3 + server4 + server5;
                if (text != "")
                {
                    StatusCheck(text);
                    SendVkStatus(text);
                }
                else
                {
                    PrintWarning("Текст статуса пуст, не заполнен конфиг или не получены данные с Rust:IO");
                }
            });
        }
        private void SendStatus(string key, params KeyValuePair<string, string>[] replacements)
        {
            string message = key;
            foreach (var replacement in replacements)
                message = message.Replace($"{{{replacement.Key}}}", replacement.Value);
            SendVkStatus(message);
        }
        private void MsgAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            if (arg.Args == null)
            {
                PrintWarning($"Текст сообщения отсутсвует, правильная команда |sendmsgadmin сообщение|. Message missing, use command |sendmsgadmin message|");
                return;
            }
            string[] args = arg.Args;
            if (args.Length > 0)
            {
                string text = null;
                if (config.MltServSet.MSSEnable)
                {
                    text = $"[VKBot msgadmin] [Сервер {config.MltServSet.ServerNumber}] " + string.Join(" ", args.Skip(0).ToArray());
                }
                else
                {
                    text = $"[VKBot msgadmin] " + string.Join(" ", args.Skip(0).ToArray());
                }
                SendVkMessage(config.AdmNotify.VkID, text);
                Log("Log", $"|sendmsgadmin| Отправлено новое сообщение администратору: ({text})");
            }
        }
        private void GetUserInfo(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            if (arg.Args == null)
            {
                PrintWarning($"Введите команду playerinfo имя для получения информации о игроке из базы vkbot");
                return;
            }
            string[] args = arg.Args;
            if (args.Length > 0)
            {
                bool returned = false;
                int amount = usersdata.VKUsersData.Count;
                if (amount != 0)
                {
                    for (int i = 0; i < amount; i++)
                    {
                        if (usersdata.VKUsersData.ElementAt(i).Value.Name.ToLower().Contains(args[0]))
                        {
                            returned = true;
                            string text = "Никнейм: " + usersdata.VKUsersData.ElementAt(i).Value.Name + "\nSTEAM: steamcommunity.com/profiles/" + usersdata.VKUsersData.ElementAt(i).Value.UserID + "/";
                            if (usersdata.VKUsersData.ElementAt(i).Value.Confirmed)
                            {
                                text = text + "\nVK: vk.com/id" + usersdata.VKUsersData.ElementAt(i).Value.VkID;
                            }
                            else
                            {
                                text = text + "\nVK: vk.com/id" + usersdata.VKUsersData.ElementAt(i).Value.VkID + " (не подтвержден)";
                            }
                            if (usersdata.VKUsersData.ElementAt(i).Value.Bdate != null && usersdata.VKUsersData.ElementAt(i).Value.Bdate != "noinfo")
                            {
                                text = text + "\nДата рождения: " + usersdata.VKUsersData.ElementAt(i).Value.Bdate;
                            }
                            if (config.TopWPlayersPromo.TopWPlEnabled)
                            {
                                text = text + "\nРазрушено строений: " + usersdata.VKUsersData.ElementAt(i).Value.Raids + "\nУбито игроков: " + usersdata.VKUsersData.ElementAt(i).Value.Kills + "\nНафармил: " + usersdata.VKUsersData.ElementAt(i).Value.Farm;
                            }
                            Puts(text);
                        }
                    }
                }
                if (!returned)
                {
                    Puts("Не найдено игроков с таким именем");
                }
            }
        }
        #endregion

        #region VKAPI
        private void SendVkMessage(string reciverID, string msg)
        {
            string type = "message";
            string url = "https://api.vk.com/method/messages.send?user_ids=" + reciverID + "&message=" + msg + "&v=5.69&access_token=" + config.VKAPIT.VKToken;
            webrequest.Enqueue(url, null, (code, response) => GetCallback(code, response, type), this);
        }
        private void SendVkWall(string msg)
        {
            string type = "post";
            string url = "https://api.vk.com/method/wall.post?owner_id=-" + config.VKAPIT.GroupID + "&message=" + msg + "&from_group=1&v=5.69&access_token=" + config.VKAPIT.VKTokenApp;
            webrequest.Enqueue(url, null, (code, response) => GetCallback(code, response, type), this);
        }
        private void SendVkStatus(string msg)
        {
            StatusCheck(msg);
            string type = "status";
            string url = "https://api.vk.com/method/status.set?group_id=" + config.VKAPIT.GroupID + "&text=" + msg + "&v=5.69&access_token=" + config.VKAPIT.VKTokenApp;
            webrequest.Enqueue(url, null, (code, response) => GetCallback(code, response, type), this);
        }
        #endregion

        #region VKBotAPI
        string GetUserVKId(ulong userid)
        {
            if (usersdata.VKUsersData.ContainsKey(userid) && usersdata.VKUsersData[userid].Confirmed)
            {
                return usersdata.VKUsersData[userid].VkID;
            }
            else
            {
                return null;
            }
        }
        string GetUserLastNotice(ulong userid)
        {
            if (usersdata.VKUsersData.ContainsKey(userid) && usersdata.VKUsersData[userid].Confirmed)
            {
                return usersdata.VKUsersData[userid].LastRaidNotice;
            }
            else
            {
                return null;
            }
        }
        string AdminVkID()
        {
            return config.AdmNotify.VkID;
        }
        private void VKAPISaveLastNotice(ulong userid, string lasttime)
        {
            if (usersdata.VKUsersData.ContainsKey(userid))
            {
                usersdata.VKUsersData[userid].LastRaidNotice = lasttime;
                VKBData.WriteObject(usersdata);
            }
            else
            {
                return;
            }
        }
        private void VKAPIWall(string text, string attachments, bool atimg)
        {
            if (atimg)
            {
                SendVkWall($"{text}&attachments={attachments}");
                Log("vkbotapi", $"Отправлен новый пост на стену: ({text}&attachments={attachments})");
            }
            else
            {
                SendVkWall($"{text}");
                Log("vkbotapi", $"Отправлен новый пост на стену: ({text})");
            }
        }
        private void VKAPIMsg(string text, string attachments, string reciverID, bool atimg)
        {
            if (atimg)
            {
                SendVkMessage(reciverID, $"{text}&attachment={attachments}");
                Log("vkbotapi", $"Отправлено новое сообщение пользователю {reciverID}: ({text}&attachments={attachments})");
            }
            else
            {
                SendVkMessage(reciverID, $"{text}");
                Log("vkbotapi", $"Отправлено новое сообщение пользователю {reciverID}: ({text})");
            }
        }
        private void VKAPIStatus(string msg)
        {
            SendVkStatus(msg);
            Log("vkbotapi", $"Отправлен новый статус: {msg}");
        }
        #endregion

        #region UpdatesChecks
        void CheckOxideCommits()
        {
            var url = $"https://api.github.com/repos/OxideMod/Oxide.Rust/releases/latest";
            Dictionary<string, string> userAgent = new Dictionary<string, string>();
            userAgent.Add("User-Agent", "OxideMod");
            try { webrequest.Enqueue(url, null, (code, response) => APIResponse(code, response), this, Core.Libraries.RequestMethod.GET, userAgent); }
            catch { timer.Once(60f, CheckOxideCommits); }
        }
        void APIResponse(int code, string response)
        {
            if (response != null || code == 200)
            {
                var jsonresponse2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(response, jsonsettings);
                if (!(jsonresponse2 is Dictionary<string, object>) || jsonresponse2.Count == 0 || !jsonresponse2.ContainsKey("name")) return;
                string message = (string)jsonresponse2["name"];
                if (message == "")
                    message = Oxide.Core.OxideMod.Version.ToString();
                var buildNum = Convert.ToInt32(message.Substring(4));
                bool buildChanged = false;
                if (serverOxideVersion < buildNum)
                {
                    buildChanged = true;
                }
                if (buildChanged && !OxideUpdateNotice)
                {
                    string oxidenotice = null;
                    if (config.MltServSet.MSSEnable)
                    {
                        oxidenotice = $"[VKBot] [Сервер {config.MltServSet.ServerNumber}] Вышло обновление Oxide {message}. Ссылка: github.com/OxideMod/Oxide/releases/latest";
                    }
                    else
                    {
                        oxidenotice = $"[VKBot] Вышло обновление Oxide {message}. Ссылка: github.com/OxideMod/Oxide.Rust/releases/latest";
                    }
                    SendVkMessage(config.AdmNotify.VkID, oxidenotice);
                    OxideUpdateNotice = true;
                }
            }
        }
        #endregion

        #region Helpers
        void Log(string filename, string text)
        {
            LogToFile(filename, $"[{DateTime.Now}] {text}", this);
        }
        void GetCallback(int code, string response, string type)
        {
            if (!response.Contains("error"))
            {
                Puts($"New {type} sended: {response}");
            }
            else
            {
                PrintWarning($"{type} not sended. Error logs: /oxide/logs/VKBot/");
                Log("Errors", $"{type} not sended. Error: {response}");

            }
        }
        private string EmojiCounters(string counter)
        {
            var chars = counter.ToCharArray();
            string emoji = "";
            for (int ctr = 0; ctr < chars.Length; ctr++)
            {
                List<object> digits = new List<object>()
                {
                    "0",
                    "1",
                    "2",
                    "3",
                    "4",
                    "5",
                    "6",
                    "7",
                    "8",
                    "9"
                };
                if (digits.Contains(chars[ctr].ToString()))
                {
                    string replace = chars[ctr] + "⃣";
                    emoji = emoji + replace;
                }
                else
                {
                    emoji = emoji + chars[ctr];
                }
            }
            return emoji;
        }
        private string WipeDate()
        {
            DateTime LastWipe = SaveRestore.SaveCreatedTime.ToLocalTime();
            string LastWipeInfo = LastWipe.ToString("dd.MM");
            return LastWipeInfo;
        }
        private string GetOnline()
        {
            string onlinecounter = "";
            List<ulong> OnlinePlayers = new List<ulong>();
            foreach (var pl in BasePlayer.activePlayerList)
            {
                OnlinePlayers.Add(pl.userID);
            }
            if (config.StatusStg.OnlWmaxslots)
            {
                var slots = ConVar.Server.maxplayers.ToString();
                onlinecounter = OnlinePlayers.Count.ToString() + "/" + slots.ToString();
            }
            else
            {
                onlinecounter = OnlinePlayers.Count.ToString();
            }
            return onlinecounter;
        }
        private static List<BasePlayer> FindPlayersOnline(string nameOrIdOrIp)
        {
            var players = new List<BasePlayer>();
            if (string.IsNullOrEmpty(nameOrIdOrIp)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            return players;
        }
        private void StatusCheck(string msg)
        {
            if (msg.Length > 140)
            {
                PrintWarning($"Текст статуса слишком длинный. Измените формат статуса чтобы текст отобразился полностью. Лимит символов в статусе - 140. Длина текста - {msg.Length.ToString()}");
            }
        }
        #endregion

        #region CheatCheking
        private void StartGUI(BasePlayer player)
        {
            var RankElements = new CuiElementContainer();
            var Choose = RankElements.Add(new CuiPanel
            {
                Image = { Color = $"0.0 0.0 0 0.35" },
                RectTransform = { AnchorMin = config.PlChkSet.GUIAnchorMin, AnchorMax = config.PlChkSet.GUIAnchorMax },
                CursorEnabled = false,
            }, "Hud", "AlertGUI");
            RankElements.Add(new CuiButton
            {
                Button = { Color = $"0.34 0.34 0.34 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = String.Format(config.PlChkSet.PlCheckText), Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = config.PlChkSet.PlCheckSize }
            }, Choose);
            CuiHelper.AddUi(player, RankElements);
        }
        private void StopGui(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "AlertGUI");
        }
        private void StopCheckPlayer(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), config.PlChkSet.PlCheckPerm))
            {
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Увас нет прав для использования данной команды</size>"));
                return;
            }
            if (args.Length == 1)
            {
                var targets = FindPlayersOnline(args[0]);
                if (targets.Count <= 0)
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Игрок не найден</size>"));
                    return;
                }
                if (targets.Count > 1)
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Найдено несколько игроков: " + string.Join(", ", targets.ConvertAll(p => p.displayName).ToArray()) + ". Уточните имя игрока.</size>"));
                    return;
                }
                var target = targets[0];
                if (target == player)
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вас вызвали на проверку, вы не можете сами ее закончить</size>"));
                    return;
                }
                if (!PlayersCheckList.ContainsValue(target.userID))
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Этого игрока не вызывали на проверку</size>"));
                    return;
                }
                ulong targetid;
                PlayersCheckList.TryGetValue(player.userID, out targetid);
                if (targetid != target.userID)
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы не можете закончить проверку, начатую другим модератором</size>"));
                    return;
                }
                StopGui(target);
                PlayersCheckList.Remove(player.userID);
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы закончили проверку игрока <color={config.TxtSet.TxtColor}>{target.displayName}</color></size>"));
            }
            else
            {
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Команда <color={config.TxtSet.TxtColor}>/unalert имя_игрока</color> или <color={config.TxtSet.TxtColor}>/unalert steamid</color></size>"));
                return;
            }
        }
        private void StartCheckPlayer(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), config.PlChkSet.PlCheckPerm))
            {
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Увас нет прав для использования данной команды</size>"));
                return;
            }
            if (!usersdata.VKUsersData.ContainsKey(player.userID) || !usersdata.VKUsersData[player.userID].Confirmed)
            {
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Сначала добавьте и подтвердите свой профиль командой <color={config.TxtSet.TxtColor}>/vk add ссылка на страницу</color></size>"));
                return;
            }
            if (args.Length == 1)
            {
                var targets = FindPlayersOnline(args[0]);
                if (targets.Count <= 0)
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Игрок не найден</size>"));
                    return;
                }
                if (targets.Count > 1)
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Найдено несколько игроков: " + string.Join(", ", targets.ConvertAll(p => p.displayName).ToArray()) + ". Уточните имя игрока.</size>"));
                    return;
                }
                var target = targets[0];
                if (target == player)
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы не можете проверить сами себя</size>"));
                    return;
                }
                if (PlayersCheckList.ContainsValue(target.userID))
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Этого игрока уже вызвали на проверку</size>"));
                    return;
                }
                if (PlayersCheckList.ContainsKey(player.userID))
                {
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Сначала закончите предыдущую проверку</size>"));
                    return;
                }
                StartGUI(target);
                PlayersCheckList.Add(player.userID, target.userID);
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вы вызвали игрока <color={config.TxtSet.TxtColor}>{target.displayName}</color> на проверку</size>"));
            }
            else
            {
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Команда <color={config.TxtSet.TxtColor}>/alert имя_игрока</color> или <color={config.TxtSet.TxtColor}>/alert steamid</color></size>"));
                return;
            }
        }
        private void SkypeSending(BasePlayer player, string cmd, string[] args)
        {
            if (!PlayersCheckList.ContainsValue(player.userID))
            {
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Вас не вызывали на проверку. Ваш скайп нам не нужен <color={config.TxtSet.TxtColor}>:)</color></size>"));
                return;
            }
            if (args.Length == 1)
            {
                string reciverid = null;
                for (int i = 0; i < PlayersCheckList.Count; i++)
                {
                    if (PlayersCheckList.ElementAt(i).Value == player.userID)
                    {
                        ulong moderid = PlayersCheckList.ElementAt(i).Key;
                        reciverid = usersdata.VKUsersData[moderid].VkID;
                    }
                }
                if (reciverid != null)
                {
                    if (config.MltServSet.MSSEnable)
                    {
                        msg = "[VKBot] [Сервер " + config.MltServSet.ServerNumber.ToString() + "] " + player.displayName + "(" + player.UserIDString + ") предоставил скайп для проверки: " + args[0] + ". По завершению проверки введите команду /unalert " + player.displayName + " . Ссылка на профиль стим: steamcommunity.com/profiles/" + player.userID.ToString() + "/";
                    }
                    else
                    {
                        msg = "[VKBot] " + player.displayName + "(" + player.UserIDString + ") предоставил скайп для проверки: " + args[0] + ". Ссылка на профиль стим: steamcommunity.com/profiles/" + player.userID.ToString() + "/";
                    }
                    if (usersdata.VKUsersData.ContainsKey(player.userID) && usersdata.VKUsersData[player.userID].Confirmed)
                    {
                        msg = msg + " . Ссылка на профиль ВК: vk.com/id" + usersdata.VKUsersData[player.userID].VkID;
                    }
                    SendVkMessage(reciverid, msg);
                    PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Ваш скайп был отправлен модератору. Ожидайте звонка.</size>"));
                }
                else
                {
                    PrintWarning("Не удалось найти id модератора. Напишите разработчику.");
                    return;
                }
            }
            else
            {
                PrintToChat(player, String.Format($"<size={config.TxtSet.TxtSize}>Команда <color={config.TxtSet.TxtColor}>:)/skype НИК в СКАЙПЕ</color></size>"));
                return;
            }
        }
        #endregion
        
        #region TopWipePlayersStatsAndPromo
        private ulong GetTopRaider()
        {
            int max = 0;
            ulong TopID = 0;
            int amount = usersdata.VKUsersData.Count;
            if (amount != 0)
            {
                for (int i = 0; i < amount; i++)
                {
                    if (usersdata.VKUsersData.ElementAt(i).Value.Raids > max)
                    {
                        max = usersdata.VKUsersData.ElementAt(i).Value.Raids;
                        TopID = usersdata.VKUsersData.ElementAt(i).Value.UserID;
                    }
                }
            }
            return TopID;
        }
        private ulong GetTopKiller()
        {
            int max = 0;
            ulong TopID = 0;
            int amount = usersdata.VKUsersData.Count;
            if (amount != 0)
            {
                for (int i = 0; i < amount; i++)
                {
                    if (usersdata.VKUsersData.ElementAt(i).Value.Kills > max)
                    {
                        max = usersdata.VKUsersData.ElementAt(i).Value.Kills;
                        TopID = usersdata.VKUsersData.ElementAt(i).Value.UserID;
                    }
                }
            }
            return TopID;
        }
        private ulong GetTopFarmer()
        {
            int max = 0;
            ulong TopID = 0;
            int amount = usersdata.VKUsersData.Count;
            if (amount != 0)
            {
                for (int i = 0; i < amount; i++)
                {
                    if (usersdata.VKUsersData.ElementAt(i).Value.Farm > max)
                    {
                        max = usersdata.VKUsersData.ElementAt(i).Value.Farm;
                        TopID = usersdata.VKUsersData.ElementAt(i).Value.UserID;
                    }
                }
            }
            return TopID;
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
                if (traider != 0)
                {
                    text = text + "\nТоп рэйдер: " + usersdata.VKUsersData[traider].Name;
                    check = true;
                }
                if (tkiller != 0)
                {
                    text = text + "\nТоп киллер: " + usersdata.VKUsersData[tkiller].Name;
                    check = true;
                }
                if (tfarmer != 0)
                {
                    text = text + "\nТоп фармер: " + usersdata.VKUsersData[tfarmer].Name;
                    check = true;
                }
                if (config.TopWPlayersPromo.TopPlPromoGift)
                {
                    text = text + "\nТоп игроки получают в качестве награды промокод на баланс в магазине.\n P.S. Если вы заняли топ место, но вам не пришел промокод, значит вы не добавили свой профиль вк командой /vk add или не подтвердили его.";
                }
                if (check)
                {
                    SendVkWall(text);
                }
            }
            if (traider != 0 && config.TopWPlayersPromo.TopPlPromoGift && usersdata.VKUsersData.ContainsKey(traider) && usersdata.VKUsersData[traider].Confirmed)
            {
                string text = "";
                if (config.MltServSet.MSSEnable)
                {
                    text = "[Сервер " + config.MltServSet.ServerNumber.ToString() + "] ";
                }
                text = text + "Поздравляем! Вы Топ 1 рэйдер по результатам этого вайпа! В качестве награды, вы получаете промокод " + config.TopWPlayersPromo.TopRaiderPromo + " на баланс в нашем магазине! " + config.TopWPlayersPromo.StoreUrl;
                string reciver = usersdata.VKUsersData[traider].VkID;
                SendVkMessage(reciver, text);
            }
            if (tkiller != 0 && config.TopWPlayersPromo.TopPlPromoGift && usersdata.VKUsersData.ContainsKey(tkiller) && usersdata.VKUsersData[tkiller].Confirmed)
            {
                string text = "";
                if (config.MltServSet.MSSEnable)
                {
                    text = "[Сервер " + config.MltServSet.ServerNumber.ToString() + "] ";
                }
                text = text + "Поздравляем! Вы Топ 1 киллер по результатам этого вайпа! В качестве награды, вы получаете промокод " + config.TopWPlayersPromo.TopKillerPromo + " на баланс в нашем магазине! " + config.TopWPlayersPromo.StoreUrl;
                string reciver = usersdata.VKUsersData[tkiller].VkID;
                SendVkMessage(reciver, text);
            }
            if (tfarmer != 0 && config.TopWPlayersPromo.TopPlPromoGift && usersdata.VKUsersData.ContainsKey(tfarmer) && usersdata.VKUsersData[tfarmer].Confirmed)
            {
                string text = "";
                if (config.MltServSet.MSSEnable)
                {
                    text = "[Сервер " + config.MltServSet.ServerNumber.ToString() + "] ";
                }
                text = text + "Поздравляем! Вы Топ 1 фармер по результатам этого вайпа! В качестве награды, вы получаете промокод " + config.TopWPlayersPromo.TopFarmerPromo + " на баланс в нашем магазине! " + config.TopWPlayersPromo.StoreUrl;
                string reciver = usersdata.VKUsersData[tfarmer].VkID;
                SendVkMessage(reciver, text);
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
            if (config.MltServSet.MSSEnable)
            {
                msg = msg + " [Сервер " + config.MltServSet.ServerNumber.ToString() + "]";
            }
            msg = msg + " В настройки добавлены новые промокоды: \nТоп рэйдер - " + config.TopWPlayersPromo.TopRaiderPromo + "\nТоп киллер - " + config.TopWPlayersPromo.TopKillerPromo + "\nТоп фармер - " + config.TopWPlayersPromo.TopFarmerPromo;
            SendVkMessage(config.AdmNotify.VkID, msg);
        }
        #endregion
    }
}