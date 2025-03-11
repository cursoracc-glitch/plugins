using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using ConVar;
using System.Linq;
using Oxide.Core;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("IQFakeActive", "Mercury", "0.0.3")]
    [Description("Актив вашего сервера, но немного не тот :)")]
    class IQFakeActive : RustPlugin
    {
        /// <summary>
        /// Обновление 0.0.2 :
        /// - Изменил метод подсчета дней вайпов(Плагин поймет какой у вас день вайпа,независимо от его загрузки на ваш сервер)
        /// - Оптимизировал плагин
        /// - Избавился от дата файла, заменил на автоматизированный процесс
        /// - Добавил настройку скачков онлайна в конфигурации
        /// - Исправил мою оплошность в проигрывании звуков(после дебага не сменил показатели таймера)
        /// - Добавил настройку интервала проигрывания звуков
        /// - Тестово ввел метод парсинга чата
        /// - Тестово ввел метод парсинга игроков
        /// - Парсинг чата и игроков работает по принципу : Используете общую базу - пополняйте общую базу
        /// - Добавил в методы с парсингом на защиту Администратора
        /// - Добавил в метод с парсингом проверки на пустые сообщения
        /// - Добавил в метод парсинга сообщений проверку на запрещенные слова, чтобы ваш сервер не пополнял базу плохими словами
        /// - Добавил API во вкладку с API(Если вам что-то не понятно,обращайтесь, помогу)
        /// - Добавил зависимость к генерации онлайна от времени суток
        /// 
        /// Обновление 0.0.3
        /// - Поправил локальную генерацию игроков
        /// - Исправил автоматический подсчет времени вайпа для более корректного отображения и генерации онлайна
        /// - Переделал резервирование игроков с учетом реальных игроков
        /// - Изменил API для получения резервированных игроков, теперь оно более корректно и удобно
        /// - Добавил проверку на сходство фейка с реальным игроком в сообщениях, теперь вы не встретите фейк чат с схожими никами реальных игроков
        /// - Добавил проверку на сходство фейка с реальным игроком в личных сообщения
        /// - Добавил проверку на сходство фейка с реальным игроком при сообщении о подсоединение к серверву
        /// - Добавил проверку на сходство фейка с реальным игроком при сообщении о отключении от серверва
        /// - Добавил замену фейкового игрока при сообщении о его отлючении с заменой в резерве
        /// - Привел активность в чате к общему, теперь сообщения для всех одинаковые
        /// - Добавил коррекцию логирования
        /// </summary>


        #region Vars
        public int FakeOnline = 0;
        public static DateTime TimeCreatedSave = SaveRestore.SaveCreatedTime.Date;
        public static DateTime RealTime = DateTime.Now.Date;
        public static int SaveCreated = RealTime.Subtract(TimeCreatedSave).Days;
        #endregion

        #region Reference
        [PluginReference] Plugin IQChat, ImageLibrary;

        #region IQChat
        private enum IQChatGetType
        {
            Prefix,
            ChatColor,
            NickColor,
        }
        private string GetInfoIQChat(IQChatGetType TypeInfo, ulong ID, bool Default)
        {
            if (!IQChat) return "";
            switch (TypeInfo)
            {
                case IQChatGetType.Prefix:
                    {
                        string Prefix = Default == true ? (string)IQChat?.Call("API_GET_DEFUALT_PRFIX") : (string)(IQChat?.Call("API_GET_PREFIX", ID));
                        return Prefix;
                    }
                case IQChatGetType.ChatColor:
                    {
                        string ChatColor = Default == true ? (string)IQChat?.Call("API_GET_DEFUALT_COLOR_CHAT") : (string)(IQChat?.Call("API_GET_CHAT_COLOR", ID));
                        return ChatColor;
                    }
                case IQChatGetType.NickColor:
                    {
                        string NickColor = Default == true ? (string)IQChat?.Call("API_GET_DEFUALT_COLOR_NICK") : (string)(IQChat?.Call("API_GET_NICK_COLOR", ID)); 
                        return NickColor;
                    }
            }
            return "";
        }
        #endregion

        #region Image Library
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public bool HasImage(string imageName) => (bool)ImageLibrary?.Call("HasImage", imageName);


        #endregion

        #endregion

        #region Configuration 
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка создания фейковых игроков")]
            public FakePlayerSettings FakePlayers = new FakePlayerSettings();
            [JsonProperty("Настройка актива")]
            public ActiveSettings FakeActive = new ActiveSettings();
            [JsonProperty("Настройка онлайна")]
            public FakeOnlineSettings FakeOnline = new FakeOnlineSettings();
            [JsonProperty("Включить лоигрование действий плагина в консоль")]
            public bool UseLogConsole;

            internal class FakeOnlineSettings
            {
                [JsonProperty("Настройка интервала обновления кол-во онлайна(сек)")]
                public int IntervalUpdateOnline;
                [JsonProperty("Детальная настройка типов онлайна")]
                public UpdateOnline SettingsUpdateOnline = new UpdateOnline();
                internal class UpdateOnline
                {
                    [JsonProperty("Настройка обновления онлайна")]
                    public StandartFormul StandartFormulSetting = new StandartFormul();
                    internal class StandartFormul
                    {
                        [JsonProperty("Минимальный множитель онлайна(От этого показателя зависит скачок онлайна при обновлении)")]
                        public float MinimumFactor;
                        [JsonProperty("Максимальный множитель онлайна(От этого показателя зависит скачок онлайна при обновлении)")]
                        public float MaximumFactor;
                        [JsonProperty("Включить зависимость генерации оналйна от времени суток?")]
                        public bool DayTimeGerenation;
                    }
                }
            }
            internal class FakePlayerSettings
            {
                [JsonProperty("Использовать игроков с общей базы игроков(true - да/false - нет, вы сами будете задавать параметры)")]
                public bool PlayersDB;
                [JsonProperty("Использовать сообщение с общей базы игроков(true - да/false - нет, вы сами будете задавать параметры)")]
                public bool ChatsDB;

                [JsonProperty("Локальный - список ников с которыми будут создаваться игроки(Общая база игроков должна быть отключена)")]
                public List<string> ListNickName = new List<string>();
                [JsonProperty("Локальный - список сообщений которые будут отправляться в чат(Общая база игроков должна быть отключена)")]
                public List<string> ListMessages = new List<string>();
            }
            internal class ActiveSettings
            {
                [JsonProperty("Настройка актива в чате")]
                public ChatActiveSetting ChatActive = new ChatActiveSetting();
                [JsonProperty("Настройка иммитации актива с помощью звуков(будь то рейд, будь то кто-то ходит рядом или добывает)")]
                public SounActiveSettings SoundActive = new SounActiveSettings();
                internal class SounActiveSettings
                {
                    [JsonProperty("Использовать звуки?")]
                    public bool UseLocalSoundBase;
                    [JsonProperty("Минимальный интервал проигрывания звука")]
                    public int MinimumIntervalSound;
                    [JsonProperty("Максимальный интервал проигрывания звука")]
                    public int MaximumIntervalSound;
                    [JsonProperty("Локальный лист звуков и их настройка")]
                    public List<Sounds> SoundLists = new List<Sounds>();
                    public class Sounds
                    {
                        [JsonProperty("Ваш звук")]
                        public string SoundPath;
                        [JsonProperty("Минимальная позиция от игрока")]
                        public int MinPos;
                        [JsonProperty("Максимальная позиция от игрока")]
                        public int MaxPos;
                        [JsonProperty("Шанс проигрывания данного звука")]
                        public int Rare;
                        [JsonProperty("На какой день после WIPE будет отыгрываться данный звук")]
                        public int DayFaktor;
                    }
                }
                internal class ChatActiveSetting
                {
                    [JsonProperty("IQChat : Дополниттельные настройки для IQChat")]
                    public IQChat IQChatSettings = new IQChat();
                    [JsonProperty("IQChat : Настройки подключения и отключения для IQChat")]
                    public IQChatNetwork IQChatNetworkSetting = new IQChatNetwork();
                    [JsonProperty("IQChat : Настройки личных сообщений для IQChat")]
                    public IQChatPM IQChatPMSettings = new IQChatPM();
                    [JsonProperty("Использовать черный список слов")]
                    public bool UseBlackList;
                    [JsonProperty("Укажите слова,которые будут запрещены в чате")]
                    public List<string> BlackList = new List<string>();
                    [JsonProperty("Укажите минимальный интервал отправки сообщения в чат(секунды)")]
                    public int MinimumInterval;
                    [JsonProperty("Укажите максимальный интервал отправки сообщения в чат(секунды)")]
                    public int MaximumInterval;

                    internal class IQChat
                    {
                        [JsonProperty("IQChat : Использовать отображение стандартного префикса/цветов в IQChat(для общей базы,игнорируются настройки ниже, в этом классе)")]
                        public bool DefaultSettings;
                        [JsonProperty("IQChat : Использовать отображение префикса IQChat(Для общей базы игроков)")]
                        public bool PrefixUse;
                        [JsonProperty("IQChat : Использовать цвет ника IQChat(Для общей базы игроков)")]
                        public bool ColorNickUse;
                        [JsonProperty("IQChat : Использовать цвет сообщения IQChat(Для общей базы игроков)")]
                        public bool ColorChatUse;
                    }
                    internal class IQChatNetwork
                    {
                        [JsonProperty("IQChat : Использовать подключение/отключение в чате")]
                        public bool UseNetwork;
                        [JsonProperty("IQChat : Список стран для подключения")]
                        public List<string> CountryListConnected = new List<string>();
                        [JsonProperty("IQChat : Список причин отсоединения от сервера")]
                        public List<string> ReasonListDisconnected = new List<string>();
                    }
                    internal class IQChatPM
                    {
                        [JsonProperty("IQChat : Использовать случайное сообщение в ЛС")]
                        public bool UseRandomPM;
                        [JsonProperty("IQChat : Список случайных сообщений в ЛС")]
                        public List<string> PMListMessage = new List<string>();
                        [JsonProperty("Укажите минимальный интервал отправки сообщения в ЛС(секунды)")]
                        public int MinimumInterval;
                        [JsonProperty("Укажите максимальный интервал отправки сообщения в ЛС(секунды)")]
                        public int MaximumInterval;
                    }
                }
            }
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    UseLogConsole = true,
                    FakeOnline = new FakeOnlineSettings
                    {
                        IntervalUpdateOnline = 20,
                        SettingsUpdateOnline = new FakeOnlineSettings.UpdateOnline
                        {
                            StandartFormulSetting = new FakeOnlineSettings.UpdateOnline.StandartFormul
                            {
                                DayTimeGerenation = true,
                                MinimumFactor = 1.2f,
                                MaximumFactor = 1.35f,
                            },
                        }
                    },
                    FakePlayers = new FakePlayerSettings
                    {
                        ChatsDB = true,
                        PlayersDB = true,
                        ListNickName = new List<string>
                        {
                            "Mercury",
                            "Debil",
                            "Fake#1",
                            "Fake#2",
                            "Fake#3",
                            "Fake#4",
                            "Fake#5s"
                        },
                        ListMessages = new List<string>
                        {
                            "hi",
                            "привет",
                            "классный сервер"
                        }
                    },
                    FakeActive = new ActiveSettings
                    {
                        SoundActive = new ActiveSettings.SounActiveSettings
                        {
                            UseLocalSoundBase = true,
                            MinimumIntervalSound = 228,
                            MaximumIntervalSound = 1337,
                            SoundLists = new List<ActiveSettings.SounActiveSettings.Sounds>
                            {
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 30,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 60,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 50,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/prefabs/deployable/campfire/effects/campfire-deploy.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 50,
                                    MinPos = 45,
                                    MaxPos = 70,
                                    SoundPath = "assets/prefabs/locks/keypad/effects/lock.code.updated.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 50,
                                    MinPos = 70,
                                    MaxPos = 100,
                                    SoundPath = "assets/prefabs/npc/sam_site_turret/effects/tube_launch.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 80,
                                    MinPos = 10,
                                    MaxPos = 30,
                                    SoundPath = "assets/prefabs/weapons/bow/effects/fire.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 80,
                                    MinPos = 10,
                                    MaxPos = 30,
                                    SoundPath = "assets/prefabs/weapons/bow/effects/fire.prefab"
                                },       
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 1,
                                    Rare = 80,
                                    MinPos = 10,
                                    MaxPos = 30,
                                    SoundPath = "assets/prefabs/weapons/knife/effects/strike-soft.prefab"
                                }, 
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 2,
                                    Rare = 30,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 3,
                                    Rare = 30,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                                new ActiveSettings.SounActiveSettings.Sounds
                                {
                                    DayFaktor = 3,
                                    Rare = 30,
                                    MinPos = 30,
                                    MaxPos = 50,
                                    SoundPath = "assets/bundled/prefabs/fx/impacts/stab/concrete/concrete1.prefab"
                                },
                            }
                        },
                        ChatActive = new ActiveSettings.ChatActiveSetting
                        {
                            UseBlackList = true,
                            BlackList = new List<string>
                            {
                                "читы",
                                "mercury",
                                "гадость",
                                "сука",
                                "блядь",
                                "тварь",
                                "сервер",
                                "говно",
                                "хуйня",
                                "накрутка",
                                "фейк",
                                "крутят",
                            },
                            MinimumInterval = 5,
                            MaximumInterval = 30,
                            IQChatSettings = new ActiveSettings.ChatActiveSetting.IQChat
                            {
                                DefaultSettings = true,
                                PrefixUse = true,
                                ColorChatUse = true,
                                ColorNickUse = true,
                            },
                            IQChatNetworkSetting = new ActiveSettings.ChatActiveSetting.IQChatNetwork
                            {
                                UseNetwork = true,
                                CountryListConnected = new List<string>
                                {
                                    "Russia",
                                    "Ukraine",
                                    "Germany"
                                },
                                ReasonListDisconnected = new List<string>
                                {
                                    "Disconnected",
                                    "Time Out",
                                },
                            },
                            IQChatPMSettings = new ActiveSettings.ChatActiveSetting.IQChatPM
                            {
                                UseRandomPM = true,
                                MinimumInterval = 300,
                                MaximumInterval = 900,
                                PMListMessage = new List<string>
                                {
                                    "прив",
                                    "го в тиму",
                                    "хай",
                                    "трейд?",
                                }
                            }
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
                PrintWarning($"Ошибка чтения # конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Core

        #region Generated Online

        public void GeneratedOnline()
        {
            timer.Every(config.FakeOnline.IntervalUpdateOnline, () =>
            {
                if (BasePlayer.activePlayerList.Count == 0)
                {
                    if (config.UseLogConsole)
                        PrintWarning("\n\nОбновление отоброжаемоего онлайна не было,т.к онлайн сервера составляет - 0\n\n");
                    return;
                }

                var SettingsOnline = config.FakeOnline.SettingsUpdateOnline.StandartFormulSetting;

                int MaxOnline = ConVar.Server.maxplayers;
                int ThisOnline = BasePlayer.activePlayerList.Count;
                float Randoming = UnityEngine.Random.Range(SettingsOnline.MinimumFactor, SettingsOnline.MaximumFactor); 
                float Time = float.Parse($"1.{DateTime.Now.Hour}{DateTime.Now.Minute}");
                int DayFactor = SaveCreated <= 1 ? 2 : SaveCreated;
                PrintError(DayFactor.ToString());
                float AvaregeOnline = SettingsOnline.DayTimeGerenation ? (((MaxOnline - ThisOnline) / DayFactor * Randoming) / Time) : ((MaxOnline - ThisOnline) / DayFactor * Randoming);
                FakeOnline = Convert.ToInt32(AvaregeOnline);

                foreach (var player in BasePlayer.activePlayerList)
                    if (AvaregeOnline > FakeOnline)
                        ChatNetworkConnected(player);
                    else ChatNetworkDisconnected(player);

                if (config.UseLogConsole)
                    PrintWarning($"\n\nКоличество онлайна обновлено :\nОтображаемый онлайн: {FakeOnline}\nНастоящий онлайн: {BasePlayer.activePlayerList.Count}\n\n");
            });
        }

        #endregion

        #region Generation Player Core

        public List<FakePlayer> ReservedPlayer = new List<FakePlayer>();
        public List<FakePlayer> FakePlayerList = new List<FakePlayer>();
        public List<Messages> FakeMessageList = new List<Messages>();
        public class Messages
        {
            public string Message;
        }
        public class FakePlayer
        {
            public ulong UserID;
            public string DisplayName;
            public string IQChatPreifx;
            public string IQChatColorChat;
            public string IQChatColorNick;
        }
        void SyncReserved()
        {
            if(BasePlayer.activePlayerList.Count == 0)
            {
                if (config.UseLogConsole)
                {
                    PrintWarning("=============SYNC==================");
                    PrintWarning("Синхронизация и резервирование не было т.к онлайн сервера составляет - 0");
                    PrintWarning("=============SYNC==================");
                }
                return;
            }
            ReservedPlayer.Clear();
            for (int i = 0; i < FakeOnline - BasePlayer.activePlayerList.Count; i++)
            {
                int RandomIndex = UnityEngine.Random.Range(0, FakePlayerList.Count);
                ReservedPlayer.Add(FakePlayerList[RandomIndex]);
            }
            foreach(BasePlayer player in BasePlayer.activePlayerList) 
            {
                FakePlayer presetPlayer = new FakePlayer();
                presetPlayer.DisplayName = player.displayName; 
                presetPlayer.UserID = player.userID;
                presetPlayer.IQChatPreifx = IQChat ? GetInfoIQChat(IQChatGetType.Prefix, player.userID, false) : "";
                presetPlayer.IQChatColorChat = IQChat ? GetInfoIQChat(IQChatGetType.ChatColor, player.userID, false) : "";
                presetPlayer.IQChatColorNick = IQChat ? GetInfoIQChat(IQChatGetType.NickColor, player.userID, false) : "";
                ReservedPlayer.Add(presetPlayer);
            }
            string JSON = JsonConvert.SerializeObject(ReservedPlayer);
            if (config.UseLogConsole)
            {
                PrintWarning("=============SYNC==================");
                PrintWarning("Запущена синхронизация и резервирование игроков под онлайн..");
                PrintWarning($"Всего сгенерировано игроков: {FakePlayerList.Count}");
                PrintWarning($"Онлайн: {FakeOnline}");
                PrintWarning($"Синхронизация завершена, в резерве: {ReservedPlayer.Count}");
                PrintWarning("=============SYNC==================");
            }

            ServerMgr.Instance.StartCoroutine(AddPlayerAvatar());
            Interface.Oxide.CallHook("SyncReservedFinish", JSON);
        }
        private void GeneratedAll()
        {
            PrintWarning("Генерация активности..");
            if (config.FakePlayers.PlayersDB)
                GetPlayerDB();
            else GeneratedPlayer();

            if (config.FakePlayers.ChatsDB)
                GetMessageDB();
            else GeneratedMessage();

            PrintWarning("Генерация игроков сообщений в чате завершена..");
        }

        #region Local Base

        private ulong GeneratedSteam64ID()
        {
            ulong GeneratedID = (ulong)UnityEngine.Random.Range(76561100000000011, 76561199999999999);
            return GeneratedID;
        }
        private string GeneratedNickName()
        {
            int RandomIndexNick = UnityEngine.Random.Range(0, config.FakePlayers.ListNickName.Count);
            string NickName = config.FakePlayers.ListNickName[RandomIndexNick];
            return NickName;
        }

        private void GeneratedPlayer()
        {
            if (config.FakePlayers.ListNickName.Count == 0)
            {
                PrintError("Ошибка # генерации локальной базы игроков! Введите ники в список ников");
                return;
            }
            for (int i = 0; i < config.FakePlayers.ListNickName.Count; i++)
            {
                string DisplayName = GeneratedNickName();
                ulong UserID = GeneratedSteam64ID();

                FakePlayerList.Add(new FakePlayer
                {
                    DisplayName = DisplayName,
                    UserID = UserID,
                    IQChatColorChat = "",
                    IQChatColorNick = "",
                    IQChatPreifx = ""
                });
            }
            PrintWarning("Игроки с локальной базы сгенерированы успешно!");
        }

        private void GeneratedMessage()
        {
            if (config.FakePlayers.ListMessages.Count == 0)
            {
                PrintError("Ошибка генерации локальной базы сообщений! Введите в нее сообщения");
                return;
            }
            for (int i = 0; i < config.FakePlayers.ListMessages.Count; i++)
                FakeMessageList.Add(new Messages { Message = config.FakePlayers.ListMessages[i] });
        }

        #endregion

        #region Set Data Base
        private void DumpPlayers(BasePlayer player)
        {
            if (!config.FakePlayers.PlayersDB) return;
            if (player.IsAdmin) return;

            string Prefix = IQChat ? GetInfoIQChat(IQChatGetType.Prefix, player.userID, false) : "";
            string ChatColor = IQChat ? GetInfoIQChat(IQChatGetType.ChatColor, player.userID, false) : "";
            string NickColor = IQChat ? GetInfoIQChat(IQChatGetType.NickColor, player.userID, false) : "";
            string DisplayName = player.displayName;
            ulong UserID = player.userID;
            string API = $"http://utilite.skyplugins.ru/iqfake/iqfacekphp.php?action=dump&actiondump=user&displayname={DisplayName}&userid={UserID}&iqchatprefix={Prefix}&iqchatcolorchat={ChatColor}&iqchatcolornick={NickColor}";

            try { webrequest.Enqueue(API, null, (code, response) => { }, this); }
            catch (Exception ex) { }
        }

        private void DumpChat(string Message)
        {
            if (!config.FakePlayers.ChatsDB) return;
            if (config.FakeActive.ChatActive.BlackList.Contains(Message)) return;

            string API = $"http://utilite.skyplugins.ru/iqfake/iqfacekphp.php?action=dump&actiondump=message&text={Message}";
            try { webrequest.Enqueue(API, null, (code, response) => { }, this); }
            catch (Exception ex) { }
        }

        #endregion

        #region Get Data Base

        private void GetPlayerDB()
        {
            if (!config.FakePlayers.PlayersDB) return;

            string API = "http://utilite.skyplugins.ru/iqfake/iqfacekphp.php?action=get&actionget=user";
            try
            {
                webrequest.Enqueue(API, null, (code, response) =>
                {
                    FakePlayerList = JsonConvert.DeserializeObject<List<FakePlayer>>(response);
                }, this);
            }
            catch (Exception ex)
            {
                PrintError($"Ошибка с генерацией игроков в базе данных\n\n{ex.ToString()}");
            }

            PrintWarning("Игроки с базы данных успешно сгенерированы!");
        }

        private void GetMessageDB()
        {
            if (!config.FakePlayers.ChatsDB) return;

            string API = "http://utilite.skyplugins.ru/iqfake/iqfacekphp.php?action=get&actionget=chat";
            try
            {
                webrequest.Enqueue(API, null, (code, response) =>
                {
                    FakeMessageList = JsonConvert.DeserializeObject<List<Messages>>(response);
                }, this);
            }
            catch (Exception ex)
            {
                PrintError($"Ошибка с генерацией игроков в базе данных\n\n{ex.ToString()}");
            }
            PrintWarning("Чат с базы данных успешно сгенерированы!");
        }

        #endregion

        #endregion

        #region Generate Avatart
        public IEnumerator AddPlayerAvatar()
        {
            if (ImageLibrary)
            {
                foreach (var p in ReservedPlayer)
                {
                    if (HasImage(p.UserID.ToString())) continue;

                    string url = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=3F2959BD838BF8FB544B9A767F873457&" + "steamids=" + p.UserID;
                    webrequest.Enqueue(url, null, (code, response) =>
                    {
                        string Avatar = (string)JObject.Parse(response)["response"]["players"][0]["avatarfull"];
                        AddImage(Avatar, p.UserID.ToString());
                    }, this);
                    yield return new WaitForSeconds(0.2f);
                }

                PrintWarning("Синхронизация аватарок заверешна");
                PrintWarning("=============SYNC==================");
            }
        }
        #endregion

        #endregion

        #region Active Metods

        private FakePlayer GetFake()
        {
            if (ReservedPlayer == null) return null;
            if (ReservedPlayer.Count == 0) return null;
            return ReservedPlayer[UnityEngine.Random.Range(0, ReservedPlayer.Count)];
        }
        public bool IsRare(int Rare)
        {
            if (UnityEngine.Random.Range(0, 100) >= (100 - Rare))
                return true;
            else return false;
        }

        #region Chat Active
        private void StartChat()
        {
            int TimerRandom = UnityEngine.Random.Range(config.FakeActive.ChatActive.MinimumInterval, config.FakeActive.ChatActive.MaximumInterval);
            timer.Every(TimerRandom, () =>
            {
                string Message = GetMessage();
                foreach (var player in BasePlayer.activePlayerList)
                    SendMessage(player, Message);
            });

            int TimerRandomPM = UnityEngine.Random.Range(config.FakeActive.ChatActive.IQChatPMSettings.MinimumInterval, config.FakeActive.ChatActive.IQChatPMSettings.MaximumInterval);
            timer.Every(TimerRandomPM, () => { SendRandomPM(); });
        }
        public void SendMessage(BasePlayer player, string Message) 
        {
            var MessageSettings = config.FakeActive.ChatActive.IQChatSettings;
            if (String.IsNullOrWhiteSpace(Message)) return;
            FakePlayer Player = GetFake();
            if (Player == null) return;
            BasePlayer RealUser = BasePlayer.Find(Player.DisplayName);
            if (RealUser != null && RealUser.IsConnected && !RealUser.IsSleeping())
            {
                ReservedPlayer.Remove(Player);
                return;
            }
            string Prefix = IQChat ? config.FakeActive.ChatActive.IQChatSettings.DefaultSettings ? GetInfoIQChat(IQChatGetType.Prefix, player.userID, true) : MessageSettings.PrefixUse ? !String.IsNullOrWhiteSpace(Player.IQChatPreifx) ? Player.IQChatPreifx : "" : "" : "";
            string DisplayName = IQChat ? config.FakeActive.ChatActive.IQChatSettings.DefaultSettings ? !String.IsNullOrWhiteSpace(GetInfoIQChat(IQChatGetType.NickColor, player.userID, true)) ? $"<color={GetInfoIQChat(IQChatGetType.NickColor, player.userID, true)}>{Player.DisplayName}</color> " : Player.DisplayName : MessageSettings.ColorNickUse ? !String.IsNullOrWhiteSpace(Player.IQChatColorNick) ? $"<color={Player.IQChatColorNick}>{Player.DisplayName}</color> " : Player.DisplayName : Player.DisplayName : Player.DisplayName;
            string ColorMessage = IQChat ? config.FakeActive.ChatActive.IQChatSettings.DefaultSettings ? GetInfoIQChat(IQChatGetType.ChatColor, player.userID, true) : MessageSettings.ColorChatUse ? !String.IsNullOrWhiteSpace(Player.IQChatColorChat) ? Player.IQChatColorChat : "#ffffff" : "#ffffff" : "#ffffff";
            string FormatPlayer = $"{Prefix} {DisplayName}";
            string FormatMessage = !String.IsNullOrWhiteSpace(ColorMessage) ? $"<color={ColorMessage}>{Message}</color>" : $"{Message}";

            if (IQChat)
                IQChat?.Call("API_SEND_PLAYER", player, FormatPlayer, FormatMessage, $"{Player.UserID}");
            else player.SendConsoleCommand("chat.add", Chat.ChatChannel.Global, Player.UserID, $"{FormatPlayer}");

            if (config.UseLogConsole)
                PrintWarning($"\nОтправлено сообщение в общий чат от Fake-Player: {FormatPlayer}({Player.UserID})\nСообщение: {Message}\n\n");
        }
        public void ChatNetworkConnected(BasePlayer player)
        {
            var MessageSettings = config.FakeActive.ChatActive.IQChatNetworkSetting;
            if (!MessageSettings.UseNetwork) return;
            if (IQChat)
            {
                FakePlayer Player = GetFake();
                if (Player == null) return;
                BasePlayer RealUser = BasePlayer.Find(Player.DisplayName);
                if (RealUser != null && RealUser.IsConnected && !RealUser.IsSleeping())
                {
                    ReservedPlayer.Remove(Player);
                    return;
                }
                string Country = GetCountry();
                IQChat?.Call("API_SEND_PLAYER_CONNECTED", player, Player.DisplayName, Country, Player.UserID.ToString());

                if (config.UseLogConsole)
                    PrintWarning($"\nПодключение к серверу во время изменения онлайна Fake-Player: {Player.DisplayName}({Player.UserID})\n\n");
            }
        }
        public void ChatNetworkDisconnected(BasePlayer player)
        {
            var MessageSettings = config.FakeActive.ChatActive.IQChatNetworkSetting;
            if (!MessageSettings.UseNetwork) return;
            if (IQChat)
            {
                FakePlayer Player = GetFake();
                if (Player == null) return;
                BasePlayer RealUser = BasePlayer.Find(Player.DisplayName);
                if (RealUser != null && RealUser.IsConnected && !RealUser.IsSleeping())
                {
                    ReservedPlayer.Remove(Player);
                    return;
                }
                string Reason = GetReason();
                IQChat?.Call("API_SEND_PLAYER_DISCONNECTED", player, Player.DisplayName, Reason, Player.UserID.ToString());

                if (config.UseLogConsole)
                    PrintWarning($"\nОтсоединение от сервера во время изменения онлайна Fake-Player: {Player.DisplayName}({Player.UserID})\n\n");
                ReservedPlayer.Remove(Player);
            }
        }
        public void SendRandomPM()
        {
            if (!config.FakeActive.ChatActive.IQChatPMSettings.UseRandomPM) return;
            int IndexRandomPlayer = UnityEngine.Random.Range(0, BasePlayer.activePlayerList.Count);
            BasePlayer RandomPlayer = BasePlayer.activePlayerList[IndexRandomPlayer];
            if (RandomPlayer == null) return;
            if (!RandomPlayer.IsConnected) return;
            if (!IQChat) return;
            string Message = GetPM();
            FakePlayer Player = GetFake();
            BasePlayer RealUser = BasePlayer.Find(Player.DisplayName);
            if (RealUser != null && RealUser.IsConnected && !RealUser.IsSleeping())
            {
                ReservedPlayer.Remove(Player);
                return;
            }
            if (Player == null) return;
            IQChat?.Call("API_SEND_PLAYER_PM", RandomPlayer, Player.DisplayName, Message);

            if (config.UseLogConsole)
                PrintWarning($"\nОтправлено личное сообщение от Fake-Player: {Player.DisplayName}({Player.UserID}) для игрока : {RandomPlayer.displayName}({RandomPlayer.userID})\nСообщение: {Message}\n\n");
        }

        #region Help Metods Chat Active
        public string GetMessage()
        {
            var MessageSettings = config.FakeActive.ChatActive;
            string Message = FakeMessageList[UnityEngine.Random.Range(0, FakeMessageList.Count)].Message;
            foreach (var BlackList in MessageSettings.BlackList)
                Message = Message.Replace(BlackList, "");
            return Message;
        }
        public string GetCountry()
        {
            var CountryList = config.FakeActive.ChatActive.IQChatNetworkSetting.CountryListConnected;
            int RandomCountry = UnityEngine.Random.Range(0, CountryList.Count);
            return CountryList[RandomCountry];
        }
        public string GetReason()
        {
            var ReasonList = config.FakeActive.ChatActive.IQChatNetworkSetting.ReasonListDisconnected;
            int RandomReason = UnityEngine.Random.Range(0, ReasonList.Count);
            return ReasonList[RandomReason];
        }
        public string GetPM()
        {
            var PMList = config.FakeActive.ChatActive.IQChatPMSettings.PMListMessage;
            int RnadomPM = UnityEngine.Random.Range(0, PMList.Count);
            return PMList[RnadomPM];
        }
        #endregion

        #endregion

        #region Sound Active
        private string GetRandomEffect()
        {
            var Sound = config.FakeActive.SoundActive;
            if (!Sound.UseLocalSoundBase) return null;

            var SoundList = Sound.SoundLists.OrderBy(x => x.DayFaktor == SaveCreated);
            int RandomSoundList = UnityEngine.Random.Range(0, SoundList.Count());
            if (!IsRare(SoundList.ToList()[RandomSoundList].Rare)) return null;

            return SoundList.ToList()[RandomSoundList].SoundPath;
        }
        void StartSoundEffects()
        {
            var Sound = config.FakeActive.SoundActive;
            int RandomTimer = UnityEngine.Random.Range(Sound.MinimumIntervalSound, Sound.MaximumIntervalSound);
            timer.Once(RandomTimer, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    string PathEffect = GetRandomEffect();
                    if (String.IsNullOrWhiteSpace(PathEffect) || PathEffect == null)
                    {
                        StartSoundEffects();
                        return;
                    }
                    var EffectPos = config.FakeActive.SoundActive.SoundLists.FirstOrDefault(x => x.SoundPath == PathEffect);
                    if (EffectPos == null)
                    {
                        StartSoundEffects();
                        return;
                    }
                    Effect effect = new Effect();
                    int RandomXZ = UnityEngine.Random.Range(EffectPos.MinPos, EffectPos.MaxPos);
                    Vector3 PosSound = new Vector3(player.transform.position.x + RandomXZ, player.transform.position.y, player.transform.position.z + RandomXZ);
                    effect.Init(Effect.Type.Generic, PosSound, PosSound, (Network.Connection)null);
                    effect.pooledString = PathEffect;
                    EffectNetwork.Send(effect, player.net.connection);
                }
                StartSoundEffects();
                if (config.UseLogConsole)
                    PrintWarning($"\n\nДля игроков были проиграны звуки");
            });
        }

        #endregion

        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            PrintWarning("------------------------");
            PrintWarning("IQFakeActive by Mercury");
            PrintWarning($"Текущий реальный онлайн : {BasePlayer.activePlayerList.Count}");
            PrintWarning("Сейчас начнется генерация активности, ожидайте..Process: 478.."); 
            PrintWarning("------------------------");
            //Генерация игроков и чата
            GeneratedAll();

            //Генерируем онлайн от множителя и дополнительных факторов
            GeneratedOnline();

            //Запуск актива в чате
            StartChat();

            //Резервируем игроков
            timer.Once(30f, () =>
            {
                SyncReserved();
                timer.Every(600f, () => { SyncReserved(); });
            });

            //Запускаем звуки
            StartSoundEffects();
        }
        void Unload() => ServerMgr.Instance.StopCoroutine(AddPlayerAvatar());

        private bool OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (player.IsAdmin) return false;
            if (String.IsNullOrWhiteSpace(message)) return false;
            if (channel == Chat.ChatChannel.Team || channel == Chat.ChatChannel.Server) return false;
            DumpChat(message);
            return false;
        }
        void OnPlayerConnected(BasePlayer player) => DumpPlayers(player);
        #endregion

        #region Commands
        [ConsoleCommand("iqfa")]
        void IQFakeActiveCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Args == null || arg.Args.Length != 1 || arg.Args.Length > 1)
            {
                PrintWarning("===========SYNTAX===========");
                PrintWarning("Используйте команды:");
                PrintWarning("iqfa online - для показателя онлайна");
                PrintWarning("iqfa synh - синхронизация игроков в резерв");
                PrintWarning("===========SYNTAX===========");
                return;
            }
            string ActionCommand = arg.Args[0].ToLower();
            switch(ActionCommand)
            {
                case "online":
                case "player":
                case "players":
                    {
                        PrintWarning("===========INFORMATION===========");
                        PrintWarning($"Настоящий онлайн : {BasePlayer.activePlayerList.Count}");
                        PrintWarning($"Общий онлайн : {FakeOnline}");
                        PrintWarning("===========INFORMATION===========");
                        break;
                    }
                case "synh":
                case "synchronization":
                case "update":
                case "refresh":
                    {
                        SyncReserved();
                        break;
                    }
            }
        }
        #endregion

        #region API

        bool IsFake(ulong userID) => FakePlayerList.Where(x => x.UserID == userID).Count() > 0;
        bool IsFake(string DisplayName) => FakePlayerList.Where(x => x.DisplayName.Contains(DisplayName)).Count() > 0;
        int GetOnline() => FakeOnline;
        ulong GetFakeIDRandom() => (ulong)FakePlayerList[UnityEngine.Random.Range(0, FakePlayerList.Count)].UserID;
        string GetFakeNameRandom() => (string)FakePlayerList[UnityEngine.Random.Range(0, FakePlayerList.Count)].DisplayName;
        string FindFakeName(ulong ID)
        {
            var Fake = ReservedPlayer.FirstOrDefault(x => x.UserID == ID);
            if (Fake == null) return null;
            return Fake.DisplayName;
        }
        int DayWipe() => SaveCreated;
        #endregion
    }
}
