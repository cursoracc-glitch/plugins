using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Facepunch.Utility;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;
using UnityEngine.Networking;
using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    [Info("XDQuest", "SkuliDropek", "6.0.8")]
    [Description("Расширенная квест система для вашего сервера!")]
    public class XDQuest : RustPlugin
    {
        /// <summary> //6.0.8
        /// - Исправлен API для Raidable Bases (Nivex)
        /// - Убрано новогодние оформление
        /// - Добавлена возможность очищать прогресс при вайпе. По стандарту включено в конфигурации
        /// </summary>

        #region ReferencePlugins
        [PluginReference] Plugin CopyPaste, ImageLibrary, IQChat, Friends, Clans, Battles, Duel, Notify;
        private void SendChat(BasePlayer player, string Message, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, config.settingsIQChat.prifix, config.settingsIQChat.SteamID);
            else
                player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        private bool IsFriends(ulong userID, ulong targetID)
        {
            if (Friends)
                return (bool)Friends?.Call("HasFriend", userID, targetID);
            else
                return false;
        }
        private bool IsClans(string userID, string targetID)
        {
            if (Clans)
            {
                String TagUserID = (String)Clans?.Call("GetClanOf", userID);
                String TagTargetID = (String)Clans?.Call("GetClanOf", targetID);
                if (TagUserID == null && TagTargetID == null)
                    return false;
                return (bool)(TagUserID == TagTargetID);
            }
            else
                return false;
        }
        private bool IsDuel(ulong userID)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel)
                return (bool)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else
                return false;
        }
        private string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        private void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);
        #endregion

        #region Variables
        private static XDQuest Instance;
        private static readonly String Key = "OZh9xW02owxY1dd";
        private MonumentInfo monument;
        private BasePlayer npc;
        private ComputerStation chairNpc;
        private List<BaseEntity> HouseNPC = new List<BaseEntity>();
        private static List<uint> Light = new List<uint> { 1392608348, 110576239, 3341019015, 1797934483, 2409469892, 3887352222, 3953213470 };
        private List<Quest> QuestList = new List<Quest>();
        private SafeZone safeZone = null;
        private ZoneTrigger zoneTrigger = null;
        private Dictionary<ulong, PlayerData> playersInfo = new Dictionary<ulong, PlayerData>();

        private class PlayerData
        {
            public List<string> PlayerQuestsFinish = new List<string>();
            public List<PlayerQuest> PlayerQuestsAll = new List<PlayerQuest>();
            public Dictionary<string, double> PlayerQuestsCooldown = new Dictionary<string, double>();
        }
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["XDQUEST_CopyPasteError"] = "There was a problem with CopyPaste! Contact the Developer!\nDezLife#1480\nnvk.com/dezlife",
                ["XDQUEST_CopyPasteSuccessfully"] = "The building has spawned successfully!",
                ["XDQUEST_BuildingPasteError"] = "There was a problem with spawning the Building! Contact the Developer!\nDezLife#1480\nvk.com/dezlife",
                ["XDQUEST_MissingOutPost"] = "Your map doesnt have an Outpost Monument. Please use a custom spawn point.",
                ["XDQUEST_MissingQuests"] = "You do not have a file with tasks, the plugin will not work correctly! Create one on the Website - https://xdquest.skyplugins.ru/ or use the included one.",
                ["XDQUEST_BuildingLoad"] = "Initializing building for NPC...",
                ["XDQUEST_KeyAuth"] = "The plugin did not pass the authentication on the server!\nCheck the plugin version or contact the developer\nDezLife#1480\nvk.com/dezlife",
                ["XDQUEST_FileNotFoundOnServer"] = "File {0} could not be found on server. Contact the developer\nDezLife#1480\nvk.com/dezlife",
                ["XDQUEST_ServerNotResponse"] = "Unable to load the file {0}, Server response: {1}. Retrying to download...",
                ["XDQUEST_FileNotLoad"] = "Downloading the file {0} was unsuccessful. Contact the developer\nDezLife#1480\nvk.com/dezlife",
                ["XDQUEST_UI_TASKLIST"] = "Quest List",
                ["XDQUEST_UI_Awards"] = "Rewards",
                ["XDQUEST_UI_TASKCount"] = "<color=#42a1f5>{0}</color> QUESTS",
                ["XDQUEST_UI_CHIPperformed"] = "Completed",
                ["XDQUEST_UI_CHIPInProgress"] = "In progress",
                ["XDQUEST_UI_QUESTREPEATCAN"] = "Yes",
                ["XDQUEST_UI_QUESTREPEATfForbidden"] = "No",
                ["XDQUEST_UI_Missing"] = "Missing",
                ["XDQUEST_UI_InfoRepeatInCD"] = "Repeat {0}  |  Cooldown {1}  |  Hand in {2}",
                ["XDQUEST_UI_QuestNecessary"] = "Needed",
                ["XDQUEST_UI_QuestNotNecessary"] = "Not needed",
                ["XDQUEST_UI_QuestBtnPerformed"] = "COMPLETED",
                ["XDQUEST_UI_QuestBtnTake"] = "TAKE",
                ["XDQUEST_UI_QuestBtnPass"] = "COMPLETE",
                ["XDQUEST_UI_QuestBtnRefuse"] = "REFUSE",
                ["XDQUEST_UI_ACTIVEOBJECTIVES"] = "Objective: {0}",
                ["XDQUEST_UI_MiniQLInfo"] = "{0}\nProgress: {1} / {2}\nQuest: {3}",
                ["XDQUEST_UI_CMDCustomPosAdd"] = "You have successfully added a custom building position.\n(You need to reload the plugin)\nYou can rotate the building in the config!\nRemember to enable the option to spawn a building on a custom position in the config.",
                ["XDQUEST_UI_QuestLimit"] = "You have to many <color=#4286f4>unfinished</color> Quests",
                ["XDQUEST_UI_AlreadyTaken"] = "You have already <color=#4286f4>taken</color> this Quest!",
                ["XDQUEST_UI_AlreadyDone"] = "You have already <color=#4286f4>completed</color> this Quest!",
                ["XDQUEST_UI_TookTasks"] = "You have <color=#4286f4>successfully</color> accepted the Quest {0}",
                ["XDQUEST_UI_ACTIVECOLDOWN"] = "This Quest is on Cooldown.",
                ["XDQUEST_UI_LackOfSpace"] = "Your inventory is full! Clear some space and try again!",
                ["XDQUEST_UI_QuestsCompleted"] = "Quest Completed! Enjoy your reward!",
                ["XDQUEST_UI_PassedTasks"] = "So this Quest was to much for you? \n Try again later!",
                ["XDQUEST_UI_ActiveQuestCount"] = "You have no active Quests.",
                ["XDQUEST_Finished_QUEST"] = "You have completed the task: <color=#4286f4>{0}</color>",
                ["XDQUEST_UI_InsufficientResources"] = "You don't have {0}, you should definitely bring this to Sidorovich",
                ["XDQUEST_UI_NotResourcesAmount"] = "You don't have enough {0}, you need {1}",
                ["XDQUEST_SoundLoadErrorExt"] = "The voice file {0} is missing, upload it using this path - (/data/XDQuest/Sounds). Or remove it from the configuration"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["XDQUEST_CopyPasteError"] = "Возникла проблема с CopyPaste! Обратитесь к разработчику\nDezLife#1480\nvk.com/dezlife",
                ["XDQUEST_CopyPasteSuccessfully"] = "Постройка успешно заспавнена!",
                ["XDQUEST_BuildingPasteError"] = "Ошибка спавна поостройки! Обратитесь к разработчику\nDezLife#1480\nvk.com/dezlife",
                ["XDQUEST_MissingOutPost"] = "У вас отсутствует outpost, вы можете использовать кастомную позицию для постройки",
                ["XDQUEST_MissingQuests"] = "У вас отсутсвует файл с заданиями, плагин будет работать не коректно!  Создайте его на сайте - https://xdquest.skyplugins.ru/ или используйте стандартный",
                ["XDQUEST_BuildingLoad"] = "Инициализация постройки для NPC...",
                ["XDQUEST_KeyAuth"] = "Плагин не смог пройти аунтефикацию на сервере!\n Сверьте версию плагина или свяжитесь с разработчиком\nDezLife#1480\nvk.com/dezlife",
                ["XDQUEST_FileNotFoundOnServer"] = "Файл {0}, не найден на сервере. Обратитесь к разработчику\nDezLife#1480\nvk.com/dezlife",
                ["XDQUEST_ServerNotResponse"] = "Невозможно загрузить файл {0}, Ответ сервера: {1}. Пробуем повторить загрузку...",
                ["XDQUEST_FileNotLoad"] = "Повторная загрузка файла {0}, не увенчалась успехом. Обратитесь к разработчику\nDezLife#1480\nvk.com/dezlife",
                ["XDQUEST_UI_TASKLIST"] = "СПИСОК ЗАДАНИЙ",
                ["XDQUEST_UI_Awards"] = "Награды",
                ["XDQUEST_UI_TASKCount"] = "<color=#42a1f5>{0}</color> ЗАДАНИЙ",
                ["XDQUEST_UI_CHIPperformed"] = "выполнено",
                ["XDQUEST_UI_CHIPInProgress"] = "выполняется",
                ["XDQUEST_UI_QUESTREPEATCAN"] = "можно",
                ["XDQUEST_UI_QUESTREPEATfForbidden"] = "нельзя",
                ["XDQUEST_UI_Missing"] = "отсутствует",
                ["XDQUEST_UI_InfoRepeatInCD"] = "Повторно брать {0}  |  Кд на повторное взятие {1}  |  Сдать добытое {2}",
                ["XDQUEST_UI_QuestNecessary"] = "нужно",
                ["XDQUEST_UI_QuestNotNecessary"] = "не нужно",
                ["XDQUEST_UI_QuestBtnPerformed"] = "ВЫПОЛНЕНО",
                ["XDQUEST_UI_QuestBtnTake"] = "ВЗЯТЬ",
                ["XDQUEST_UI_QuestBtnPass"] = "ЗАВЕРШИТЬ",
                ["XDQUEST_UI_QuestBtnRefuse"] = "ОТКАЗАТЬСЯ",
                ["XDQUEST_UI_ACTIVEOBJECTIVES"] = "АКТИВНЫЕ ЗАДАЧИ: {0}",
                ["XDQUEST_UI_MiniQLInfo"] = "{0}\nПрогресс: {1} / {2}\nЗадача: {3}",
                ["XDQUEST_UI_CMDCustomPosAdd"] = "Вы успешно добавили кастомную позицию для постройки.\n(Вам нужно перезагрузить плагин)\nПовернуть ее можно в конфиге!\nТак же не забудъте включить в конфиге возможность спавнить постройку на кастомной позиции",
                ["XDQUEST_UI_QuestLimit"] = "У тебя слишком много <color=#4286f4>не законченных</color> заданий!",
                ["XDQUEST_UI_AlreadyTaken"] = "Вы уже <color=#4286f4>взяли</color> это задание!",
                ["XDQUEST_UI_AlreadyDone"] = "Вы уже <color=#4286f4>выполняли</color> это задание!",
                ["XDQUEST_UI_TookTasks"] = "Вы <color=#4286f4>успешно</color> взяли задание {0}",
                ["XDQUEST_UI_ACTIVECOLDOWN"] = "В данный момент вы не можете взять этот квест",
                ["XDQUEST_UI_LackOfSpace"] = "Эй, погоди, ты всё <color=#4286f4>не унесёшь</color>, освободи место!",
                ["XDQUEST_UI_QuestsCompleted"] = "Спасибо, держи свою <color=#4286f4>награду</color>!",
                ["XDQUEST_UI_PassedTasks"] = "Жаль что ты <color=#4286f4>не справился</color> с заданием!\nВ любом случае, ты можешь попробовать ещё раз!",
                ["XDQUEST_UI_ActiveQuestCount"] = "У вас нет активных заданий.",
                ["XDQUEST_Finished_QUEST"] = "Вы выполнили задание: <color=#4286f4>{0}</color>",
                ["XDQUEST_UI_InsufficientResources"] = "У вас нету {0}, нужно обязательно принести это сидоровичу",
                ["XDQUEST_UI_NotResourcesAmount"] = "У вас не достаточно {0},  нужно {1}",
                ["XDQUEST_SoundLoadErrorExt"] = "Отсутсвует голосовой файл {0}, загрузите его по этому пути - (/data/XDQuest/Sounds). Или удалите его из конфигурации"
            }, this, "ru");
        }

        #endregion

        #region Configuration
        private Configuration config;
        private class Configuration
        {
            public class itemsNpc
            {
                [JsonProperty("ShortName")]
                public String ShortName;

                [JsonProperty("SkinId")]
                public ulong SkinId;
            }
            public class Settings
            {
                /*3*/[JsonProperty("Максимальное колличевство единовременно взятых квестов")]
                ////[JsonProperty("The maximum number of simultaneously taken quests")]
                public Int32 questCount = 3;
                /*3*/[JsonProperty("Голосовое оповещение при выполнении задания")]
                ////[JsonProperty("Voice notification when completing a task")]
                public Boolean SoundEffect = true;
                /*3*/[JsonProperty("Включите этот параметр если у вас гниет постройка")]
                ////[JsonProperty("Enable this option if your building is rotting")]
                public Boolean useDecay = false;
                /*3*/[JsonProperty("Отчищать прогресс игроков при вайпе ?")]
                ////[JsonProperty("Clear player progress when wipe ?")]
                public Boolean useWipe = true;
                /*3*/[JsonProperty("Эфект")]
                ////[JsonProperty("Effect")]
                public String Effect = "assets/prefabs/locks/keypad/effects/lock.code.lock.prefab";
                /*3*/[JsonProperty("Names of the file with quests")]
                ////[JsonProperty("Names of the file with quests")]
                public String questListDataName = "Quest";
                /*3*/[JsonProperty("Команда для открытия квест листа с прогрессом")]
                ////[JsonProperty("The command to open a quest list with progress")]
                public String questListProgress = "qlist";
                /*3*/[JsonProperty("Радиус безопасной зоны (Как в городе нпс)")]
                ////[JsonProperty("Safe Zone Radius (As in NPC City)")]
                public float saveZoneRadius = 25;
                /*3*/[JsonProperty("Включить ли радио у нпс в здании ?")]
                ////[JsonProperty("Should the radio be turned on for the NPCs in the building?")]
                public Boolean useRadio = true;
                /*3*/[JsonProperty("Ссылка на радио станцию которая будет играть в доме")]
                ////[JsonProperty("Link to the radio station that will play in the house")]
                public String RadioStation = "http://radio.skyplugins.ru:8020/stalker.mp3";
                /*3*/[JsonProperty("Использовать метку на внутриигровой карте ? (Требуется https://skyplugins.ru/resources/428/)")]
                ////[JsonProperty("Use a placemark on an in-game map ? (Required https://skyplugins.ru/resources/428/)")]
                public Boolean mapUse = false;
                /*3*/[JsonProperty("Имя метки на карте")]
                ////[JsonProperty("Name of the placemark on the map")]
                public String nameMarkerMap = "QUEST ROOM";
                /*3*/[JsonProperty("Цвет маркера (без #)")]
                ////[JsonProperty("Marker color (without #)")]
                public String colorMarker = "f3ecad";
                /*3*/[JsonProperty("Цвет обводки (без #)")]
                ////[JsonProperty("Outline color (without #)")]
                public String colorOutline = "ff3535";
            }

            public class CustomPosition
            {
                /*3*/[JsonProperty("Использовать кастомную позицию постройки ?")]
                ////[JsonProperty("Use a custom construction position ?")]
                public Boolean useCustomPos = false;
                /*3*/[JsonProperty("Позиция постройки")]
                ////[JsonProperty("Construction position")]
                public Vector3 pos = Vector3.zero;
                /*3*/[JsonProperty("Поворот постройки (Этим параметром вы можете повернуть постройку)")]
                ////[JsonProperty("Rotate the building (You can use this parameter to rotate the building)")]
                public Int32 rotation = 0;
            }
            public class SettingsNpc
            {
                /*3*/[JsonProperty("Имя нпс")]
                ////[JsonProperty("NPC name")]
                public String Name = "Сидорович\n"; ///Sidorovich/Сидорович
                /*3*/[JsonProperty("id npc (От его ид зависит его внешность)")]
                ////[JsonProperty("npc id (His appearance depends on his ID)")]
                public ulong userId = 21;
                /*3*/[JsonProperty("Одежда нпс")]
                ////[JsonProperty("NPC Clothing")]
                public List<itemsNpc> Wear = new List<itemsNpc>();
            }
            public class SettingsIQChat
            {
                /*3*/[JsonProperty("Префикс в чате")]
                ////[JsonProperty("Prefix in the chat")]
                public String prifix = "Sidorovich:";

                /*3*/[JsonProperty("SteamID - Для аватарки из профиля стим")]
                ////[JsonProperty("SteamID - For the avatar from the steam profile")]
                public String SteamID = "21";
            }

            public class SettingsNotify
            {
                /*3*/[JsonProperty("Включить уведомления (Требуется - https://codefling.com/plugins/notify)")]
                ////[JsonProperty("Enable notifications (Is required - https://codefling.com/plugins/notify)")]
                public bool useNotify = false;
                /*3*/[JsonProperty("Тип уведомления (Требуется - https://codefling.com/plugins/notify)")]
                ////[JsonProperty("Notification Type (Is required - https://codefling.com/plugins/notify)")]
                public int typeNotify = 0;
            }

            public class SettingsSoundNPC
            {
                /*3*/[JsonProperty("Включить возможность разговаривать NPC")]
                ////[JsonProperty("Enable the ability to talk to NPCs")]
                public bool soundUse = true;
                /*3*/[JsonProperty("Заполнять стандартными звуками ? (Нужно добавить звуки в дату!)")]
                ////[JsonProperty("Fill it with standard sounds ? (Need to add sounds to the date!)")]
                public bool soundAddToCfg = true;
                /*3*/[JsonProperty("Название файлов со звуком для приветствия")]
                ////[JsonProperty("The name of the files with the greeting sound")]
                public List<string> heySound = new List<string>();
                /*3*/[JsonProperty("Название файлов со звуком для прощание")]
                ////[JsonProperty("The name of the files with the sound for farewell")]
                public List<string> byeSound = new List<string>();
                /*3*/[JsonProperty("Название файлов со звуком для взятие задания")]
                ////[JsonProperty("The name of the files with the sound for taking the task")]
                public List<string> takeQuestSound = new List<string>();
                /*3*/[JsonProperty("Название файлов со звуком для сдачи задания")]
                ////[JsonProperty("The name of the files with the sound to complete the task")]
                public List<string> turnQuestSound = new List<string>();
            }
            /*3*/[JsonProperty("Настройка кастомной позиции постройки")]
            ////[JsonProperty("Setting up a custom building position")]
            public CustomPosition customPosition = new CustomPosition();
            /*3*/[JsonProperty("Настройки NPC")]
            ////[JsonProperty("NPC Settings")]
            public SettingsNpc settingsNpc = new SettingsNpc();
            /*3*/[JsonProperty("Настройки")]
            ////[JsonProperty("Settings")]
            public Settings settings = new Settings();
            /*3*/[JsonProperty("Настройки звуков/разговоров NPC")]
            ////[JsonProperty("NPC Sound/Conversation Settings")]
            public SettingsSoundNPC settingsSoundNPC = new SettingsSoundNPC();
            /*3*/[JsonProperty("Настройки IQChat (Если есть)")]
            ////[JsonProperty("Ichat Settings (If any)")]
            public SettingsIQChat settingsIQChat = new SettingsIQChat();
            /*3*/[JsonProperty("Настройки уведомления")]
            ////[JsonProperty("Notification Settings")]
            public SettingsNotify settingsNotify = new SettingsNotify();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    throw new Exception();
                SaveConfig();
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
            }
            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (config.settingsNpc.Wear.Count == 0)
            {
                config.settingsNpc.Wear = new List<Configuration.itemsNpc>
                {
                     new Configuration.itemsNpc
                     {
                         ShortName = "pants",
                         SkinId = 960252273,
                     },
                     new Configuration.itemsNpc
                     {
                         ShortName = "hoodie",
                         SkinId = 959641236,
                     },
                     new Configuration.itemsNpc
                     {
                         ShortName = "shoes.boots",
                         SkinId = 962503020,
                     }
                };
            }
            #region RU
            if (config.settingsSoundNPC.soundAddToCfg)
            {
                if (config.settingsSoundNPC.heySound.Count == 0)
                {
                    config.settingsSoundNPC.heySound = new List<string>
                    {
                        "hello_1", "hello_2"
                    };
                }
                if (config.settingsSoundNPC.byeSound.Count == 0)
                {
                    config.settingsSoundNPC.byeSound = new List<string>
                    {
                        "bye_1", "bye_2"
                    };
                }
                if (config.settingsSoundNPC.takeQuestSound.Count == 0)
                {
                    config.settingsSoundNPC.takeQuestSound = new List<string>
                    {
                        "pick_up_quest_1", "pick_up_quest_2"
                    };
                }
                if (config.settingsSoundNPC.turnQuestSound.Count == 0)
                {
                    config.settingsSoundNPC.turnQuestSound = new List<string>
                    {
                        "turn_in_quest_1", "turn_in_quest_2"
                    };
                }
                config.settingsSoundNPC.soundAddToCfg = false;
            }
            #endregion

            #region EN
            //if (config.settingsSoundNPC.soundAddToCfg)
            //{
            //    if (config.settingsSoundNPC.heySound.Count == 0)
            //    {
            //        config.settingsSoundNPC.heySound = new List<string>
            //        {
            //            "hello_1", "hello_2", "hello_3"
            //        };
            //    }
            //    if (config.settingsSoundNPC.byeSound.Count == 0)
            //    {
            //        config.settingsSoundNPC.byeSound = new List<string>
            //        {
            //            "bye_1", "bye_2", "bye_3"
            //        };
            //    }
            //    if (config.settingsSoundNPC.takeQuestSound.Count == 0)
            //    {
            //        config.settingsSoundNPC.takeQuestSound = new List<string>
            //        {
            //            "pick_up_quest"
            //        };
            //    }
            //    if (config.settingsSoundNPC.turnQuestSound.Count == 0)
            //    {
            //        config.settingsSoundNPC.turnQuestSound = new List<string>
            //        {
            //            "turn_in_quest", "turn_in_quest_2"
            //        };
            //    }
            //    config.settingsSoundNPC.soundAddToCfg = false;
            //}
            #endregion
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }
        #endregion  

        #region QuestData
        private class PlayerQuest
        {
            public Quest parentQuest;

            public ulong UserID;

            public bool Finished;
            public int Count;

            public void AddCount(int amount = 1)
            {
                Count += amount;
                BasePlayer player = BasePlayer.FindByID(UserID);
                if (parentQuest.Amount <= Count)
                {
                    Count = parentQuest.Amount;
                    if (player != null && player.IsConnected)
                    {
                        if (Instance.config.settings.SoundEffect)
                            Instance.RunEffect(player, Instance.config.settings.Effect);

                        if (Instance.config.settingsNotify.useNotify && Instance.Notify)
                            Instance.Notify.CallHook("SendNotify", player, Instance.config.settingsNotify.typeNotify, Instance.GetLang("XDQUEST_Finished_QUEST", player.UserIDString, parentQuest.DisplayName));
                        else
                            Instance.SendChat(player, Instance.GetLang("XDQUEST_Finished_QUEST", player.UserIDString, parentQuest.DisplayName));

                        Interface.CallHook("OnQuestCompleted", player, parentQuest.DisplayName);
                    }
                    Finished = true;
                }
                if (Instance.openQuestPlayers.Contains(UserID))
                {
                    CuiHelper.DestroyUi(player, MiniQuestList);
                    Instance.OpenMQL_CMD(player);
                }
            }
            public int LeftAmount() => parentQuest.Amount - Count;
        }

        public enum QuestType
        {
            IQPlagueSkill,
            IQHeadReward,
            IQCases,
            OreBonus,
            XDChinookIvent,
            Gather,
            EntityKill,
            Craft,
            Research,
            Loot,
            Grade,
            Swipe,
            Deploy,
            PurchaseFromNpc,
            HackCrate,
            RecycleItem,
            Growseedlings,
            RaidableBases,
            Fishing
        }
        public enum PrizeType
        {
            Item,
            BluePrint,
            CustomItem,
            Command
        }
        private class Quest
        {
            internal class Prize
            {
                public string nameprize;
                public PrizeType type;
                public string ShortName;
                public int Amount;
                public string Name;
                public ulong SkinID;
                public string Command;
                public string Url;
            }

            public string DisplayName;
            public string Description;
            public string Missions;
            public QuestType QuestType;
            public string Target;
            public int Amount;
            public bool UseRepeat;
            public bool Bring;
            public int Cooldown;
            public List<Prize> PrizeList = new List<Prize>();
        }

        #endregion

        #region MetodsBuildingAndNpc
        private class Building
        {
            public string name;
            public float Deg2Rad;
            public Vector3 pos;
        }

        private Dictionary<string, Building> BuildingList = new Dictionary<string, Building>
        {
            ["1"] = new Building
            {
                name = "QuestHouseChristmas",
                Deg2Rad = 4.75f,
                pos = new Vector3(-7.32f, 1.76f, 43.83f)
            },
            ["2"] = new Building
            {
                name = "QuestHouse6",
                Deg2Rad = 5.38f,
                pos = new Vector3(-6.76f, 1.73f, 46.63f)
            },
        };

        void GenerateBuilding()
        {
            var options = new List<string> { "stability", "true", "deployables", "true", "autoheight", "false", "entityowner", "true" };
            Vector3 resultVector = GetResultVector();
            var success = CopyPaste.Call("TryPasteFromVector3", resultVector, config.customPosition.useCustomPos ? (Vector3.zero * Mathf.Deg2Rad).y - config.customPosition.rotation : (monument.transform.rotation.eulerAngles * Mathf.Deg2Rad).y - BuildingList["2"].Deg2Rad, BuildingList["2"].name, options.ToArray());
            if (success is string)
            {
                PrintWarning(GetLang("XDQUEST_CopyPasteError"));
                return;
            }
            if (config.settings.mapUse)
                Interface.CallHook("API_CreateMarker", resultVector, "xdquest", 0, 3f, 0.2f, config.settings.nameMarkerMap, config.settings.colorMarker, config.settings.colorOutline);
            if (config.customPosition.useCustomPos)
            {
                safeZone = new GameObject().AddComponent<SafeZone>();
                safeZone.Activate(resultVector, config.settings.saveZoneRadius);
            }
        }
        private void CrateBox(Vector3 pos)
        {
            BaseEntity box = GameManager.server.CreateEntity("assets/prefabs/deployable/quarry/fuelstorage.prefab", pos + Vector3.up, Quaternion.identity);
            box.enableSaving = false;
            box.OwnerID = 76561198283599982;
            box.skinID = 1195832261;
            box.Spawn();
            HouseNPC.Add(box);
        }
        private void InitializeNPC(Vector3 pos)
        {
            npc = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", pos) as BasePlayer;
            if (npc == null)
            {
                Interface.Oxide.LogError($"Initializing NPC failed! NPC Component == null #3");
                return;
            }
            npc.userID = config.settingsNpc.userId;
            npc.name = config.settingsNpc.Name;
            npc.displayName = npc.name;
            npc.Spawn();
            npc.SendNetworkUpdate();
            chairNpc.MountPlayer(npc);
            chairNpc.SetFlag(BaseEntity.Flags.Busy, true);
            CrateBox(npc.transform.position);
            if (config.settingsSoundNPC.soundUse)
            {
                zoneTrigger = new GameObject().AddComponent<ZoneTrigger>();
                zoneTrigger.Activate(pos, 3.2f);
            }
            #region NpcWearStart
            if (config.settingsNpc.Wear.Count > 0)
                for (int i = 0; i < config.settingsNpc.Wear.Count; i++)
                    ItemManager.Create(ItemManager.FindItemDefinition(config.settingsNpc.Wear[i].ShortName), 1, config.settingsNpc.Wear[i].SkinId).MoveToContainer(npc.inventory.containerWear);
            #endregion
        }
        private void ClearEnt()
        {
            IEnumerable<BasePlayer> findplayer = FindMyBot(config.settingsNpc.userId);
            foreach (var player in findplayer)
                if (player != null)
                    player.KillMessage();

            List<BaseEntity> obj = new List<BaseEntity>();
            Vis.Entities(GetResultVector(), 10f, obj, LayerMask.GetMask("Construction", "Deployable", "Deployed", "Debris", "Default"));
            foreach (BaseEntity item in obj.Where(x => x.OwnerID == 76561198283599982))
            {
                if (item == null)
                    continue;
                item.Kill();
            }
            timer.Once(5f, () => { GenerateBuilding(); });
        }

        void OnPasteFinished(List<BaseEntity> pastedEntities, string fileName)
        {
            if (fileName.ToLower() != BuildingList["2"].name.ToLower())
                return;
            try
            {
                HouseNPC.AddRange(pastedEntities);
                foreach (BaseEntity item in HouseNPC)
                {
                    if (Light.Contains(item.prefabID))
                    {
                        item.enableSaving = true;
                        item?.SetFlag(BaseEntity.Flags.Reserved8, true);
                        item?.SetFlag(BaseEntity.Flags.On, true);
                        item?.SendNetworkUpdate();
                    }

                    NeonSign neonSign = item as NeonSign;
                    if (neonSign != null)
                    {
                        neonSign.SetFlag(BaseEntity.Flags.On, true);
                        neonSign.SetFlag(BaseEntity.Flags.Reserved8, true);
                        neonSign.isAnimating = true;
                        neonSign.InvokeRepeating(neonSign.animationLoopAction, 2f, 2f);
                        neonSign.SendNetworkUpdate();
                    }

                    SnowMachine snowMachine = item as SnowMachine;
                    if (snowMachine != null)
                    {
                        snowMachine.SetFlag(BaseEntity.Flags.Reserved8, true);
                        snowMachine.SetFlag(BaseEntity.Flags.Reserved7, false);
                        snowMachine.SetFlag(BaseEntity.Flags.Reserved6, true);
                        snowMachine.SetFlag(BaseEntity.Flags.Reserved5, true);
                    }

                    DecayEntity decayEntety = item as DecayEntity;
                    if (decayEntety != null)
                    {
                        decayEntety.decay = null;
                        decayEntety.decayVariance = 0;
                        decayEntety.ResetUpkeepTime();
                        decayEntety.DecayTouch();
                    }

                    DeployableBoomBox boomBox = item as DeployableBoomBox;
                    if (boomBox != null)
                    {
                        if (config.settings.useRadio)
                        {
                            NextTick(() => { 
                            boomBox.BoxController.CurrentRadioIp = config.settings.RadioStation;
                            boomBox.BoxController.ConditionLossRate = 0;
                            boomBox.BoxController.baseEntity.ClientRPC(null, "OnRadioIPChanged", boomBox.BoxController.CurrentRadioIp);
                            if (!boomBox.BoxController.IsOn())
                            {
                                boomBox.BoxController.ServerTogglePlay(true);
                            }
                            boomBox.BoxController.baseEntity.SendNetworkUpdate(global::BasePlayer.NetworkQueue.Update);
                            });
                        }
                    }

                    BuildingBlock build = item as BuildingBlock;
                    if (build != null)
                    {
                        build.StopBeingDemolishable();
                        build.StopBeingRotatable();
                    }

                    if (item is ComputerStation)
                    {
                        chairNpc = item as ComputerStation;
                        continue;
                    }

                    Door door = item as Door;
                    if (door != null)
                    {
                        door.pickup.enabled = false;
                        door.canTakeLock = false;
                        door.canTakeCloser = false;
                    }

                    item.SetFlag(BaseEntity.Flags.Busy, true);
                    item.SetFlag(BaseEntity.Flags.Locked, true);
                }
                if(chairNpc != null)
                    InitializeNPC(chairNpc.transform.position);
                PrintWarning(GetLang("XDQUEST_CopyPasteSuccessfully"));
            }
            catch (Exception ex)
            {
                PrintError(GetLang("XDQUEST_BuildingPasteError"));
                Log(ex.InnerException.Message, "LogError");
            }
        }



        #endregion

        #region Scripts
        private class ZoneTrigger : FacepunchBehaviour
        {
            private float ZoneRadius;
            private Vector3 Position;

            private SphereCollider sphereCollider;
            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "ZoneTrigger1866";
                enabled = false;
            }

            public void Activate(Vector3 pos, float radius)
            {
                Position = pos;
                ZoneRadius = radius;
                transform.position = Position;
                transform.rotation = new Quaternion();

                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;
            }
            private void OnTriggerEnter(Collider col)
            {
                BasePlayer player = col.GetComponentInParent<BasePlayer>();
                if (player != null && !player.IsNpc && player.userID.IsSteamId())
                {
                    if (player.IsVisible(Instance.npc.eyes.position))
                    {
                        int mIndex = Random.Range(0, Instance.config.settingsSoundNPC.heySound.Count);
                        Instance.SoundPlay(Instance.config.settingsSoundNPC.heySound[mIndex]);
                    }
                }
            }

            private void OnTriggerExit(Collider col)
            {
                BasePlayer player = col.GetComponentInParent<BasePlayer>();
                if (player != null && !player.IsNpc && player.userID.IsSteamId())
                {
                    if (player.IsVisible(Instance.npc.eyes.position))
                    {
                        int mIndex = Random.Range(0, Instance.config.settingsSoundNPC.byeSound.Count);
                        Instance.SoundPlay(Instance.config.settingsSoundNPC.byeSound[mIndex]);
                    }
                    player.SendConsoleCommand("CloseMainUI");
                    if (Instance.openQuestPlayers.Contains(player.userID))
                    {
                        CuiHelper.DestroyUi(player, MiniQuestList);
                        Instance.OpenMQL_CMD(player);
                    }
                }
            }

            private void OnDestroy()
            {
                Destroy(gameObject);
                CancelInvoke();
            }

            private void UpdateCollider()
            {
                sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                        sphereCollider.name = "ZoneTrigger1866";
                    }
                    sphereCollider.radius = ZoneRadius;
                }
            }
        }
        private class SafeZone : MonoBehaviour
        {
            private Vector3 Position;
            private float Radius;
            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "NpcZonesOrRadiation";
                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }
            public void Activate(Vector3 pos, float radius)
            {
                Position = pos;
                Radius = radius;
                transform.position = Position;
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;
                var safeZone = gameObject.GetComponent<TriggerSafeZone>();
                safeZone = safeZone ?? gameObject.AddComponent<TriggerSafeZone>();
                safeZone.interestLayers = LayerMask.GetMask("Player (Server)");
                safeZone.enabled = true;
            }
            private void OnDestroy()
            {
                Destroy(gameObject);
            }
            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = Radius;
                }
            }
        }
        #endregion

        #region Hooks
        #region QuestHook
        #region Type Upgrade
        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            QuestProgress(player, QuestType.Grade, ((int)grade).ToString());
            return null;
        }
        #endregion
        #region IQPlagueSkill
        void StudySkill(BasePlayer player, string name) => QuestProgress(player, QuestType.IQPlagueSkill, name);
        #endregion
        #region HeadReward
        void KillHead(BasePlayer player) => QuestProgress(player, QuestType.IQHeadReward);

        #endregion
        #region IqCase
        void OpenCase(BasePlayer player, string name) => QuestProgress(player, QuestType.IQCases, name);
        #endregion
        #region OreBonus
        void RadOreGive(BasePlayer player, Item item) => QuestProgress(player, QuestType.OreBonus, item.info.shortname, "", null, item.amount);
        #endregion
        #region Chinook
        void LootHack(BasePlayer player) => QuestProgress(player, QuestType.XDChinookIvent);
        #endregion
        #region Gather
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            NextTick(() =>
            {
                BasePlayer player = entity as BasePlayer; ;
                if (player != null)
                    QuestProgress(player, QuestType.Gather, item.info.shortname, "", null, item.amount);
            });
        }
        void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);

        void OnCollectiblePickup(Item item, BasePlayer player) => QuestProgress(player, QuestType.Gather, item.info.shortname, "", null, item.amount);
        #endregion
        #region Craft
        void OnItemCraftFinished(ItemCraftTask task, Item item) => QuestProgress(task.owner, QuestType.Craft, task.blueprint.targetItem.shortname, "", null, item.amount);
        #endregion
        #region Research
        void OnTechTreeNodeUnlock(Workbench workbench, TechTreeData.NodeInstance node, BasePlayer player) => QuestProgress(player, QuestType.Research, node.itemDef.shortname);
        void OnItemResearch(ResearchTable table, Item targetItem, BasePlayer player) => QuestProgress(player, QuestType.Research, targetItem.info.shortname);
        #endregion
        #region Deploy
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan?.GetOwnerPlayer();
            if (player == null || go == null || plan.GetItem() == null)
                return;
            BaseEntity ent = go.ToBaseEntity();
            if (ent == null || ent.skinID == 11543256361)
                return;
            QuestProgress(player, QuestType.Deploy, plan.GetItem().info.shortname);
        }
        #endregion
        #region Loot
        private static Dictionary<BasePlayer, List<UInt64>> LootersListCarte = new Dictionary<BasePlayer, List<UInt64>>();
        private void OnLootEntity(BasePlayer player, LootContainer entity)
        {
            if (entity == null || entity.net == null || player == null || entity.OwnerID.IsSteamId())
                return;
            if (!LootersListCarte.ContainsKey(player))
                LootersListCarte.Add(player, new List<UInt64> { });
            UInt64 netId = entity.net.ID;
            if (LootersListCarte[player].Contains(netId))
                return;
            QuestProgress(player, QuestType.Loot, "", "", entity.inventory.itemList);
            LootersListCarte[player].Add(netId);
        }
        private void OnLootEntity(BasePlayer player, NPCPlayerCorpse entity)
        {
            if (entity.OwnerID.IsSteamId() || entity == null)
                return;
            if (!LootersListCarte.ContainsKey(player))
                LootersListCarte.Add(player, new List<UInt64> { });
            UInt64 netId = entity.net.ID;
            if (LootersListCarte[player].Contains(netId))
                return;
            QuestProgress(player, QuestType.Loot, "", "", entity.containers[0].itemList);
            LootersListCarte[player].Add(netId);
        }

        private void OnContainerDropItems(ItemContainer container)
        {
            if (container == null)
                return;
            BaseEntity entity = container.entityOwner;
            if (entity == null)
                return;
            if (!entity.ShortPrefabName.Contains("barrel"))
                return;
            foreach (Item lootitem in container.itemList)
                lootitem.SetFlag(global::Item.Flag.Placeholder, true);
        }
        void OnItemPickup(Item item, BasePlayer player)
        {
            if (item == null || !item.HasFlag(global::Item.Flag.Placeholder))
                return;

            item.SetFlag(global::Item.Flag.Placeholder, false);
            QuestProgress(player, QuestType.Loot, item.info.shortname, item.skin.ToString(), count: item.amount);
        }

        #endregion
        #region Swipe
        private void OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            global::Item item = card.GetItem();
            if (item != null && card.accessLevel == cardReader.accessLevel && item.conditionNormalized > 0f)
            {
                QuestProgress(player, QuestType.Swipe, card.GetItem().info.shortname);
            }
        }
        #endregion
        #region  EntityKill/взорвать/уничтожить что либо 

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || player.userID < 2147483647)
                return;
            BasePlayer attacker = info.InitiatorPlayer;
            if (attacker == null)
                return;

            if (IsFriends(player.userID, attacker.userID) || IsClans(player.UserIDString, attacker.UserIDString) || IsDuel(attacker.userID) || player.userID == attacker.userID)
                return;

            QuestProgress(player, QuestType.EntityKill, "player");
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null || info == null)
                    return;

                string targetName = entity?.ShortPrefabName;
                if (targetName.Contains("corpse") || targetName.Contains("servergibs") || targetName.Contains("player"))
                    return;
                if (targetName == "testridablehorse")
                {
                    targetName = "horse";
                }

                BasePlayer player = null;

                if (info.InitiatorPlayer != null)
                    player = info.InitiatorPlayer;
                else if (entity.GetComponent<BaseHelicopter>() != null)
                {
                    PatrolHelicopterAI helicopterAI = entity?.GetComponent<PatrolHelicopterAI>();
                    if (helicopterAI == null)
                        return;
                    player = helicopterAI._targetList[helicopterAI._targetList.Count - 1].ply;
                }
                if (player != null && !player.IsNpc && entity.ToPlayer() != player)
                {
                    QuestProgress(player, QuestType.EntityKill, targetName.ToLower());
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion
        #region (NEW) Покупки у НПС
        void OnNpcGiveSoldItem(NPCVendingMachine machine, Item soldItem, BasePlayer buyer) => QuestProgress(buyer, QuestType.PurchaseFromNpc, soldItem.info.shortname, "", null, soldItem.amount);
        #endregion
        #region(NEW) Взлом ящика
        void CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            QuestProgress(player, QuestType.HackCrate, "", "", null);
        }
        #endregion
        #region (NEW) RecycleItem (Игрок не должен выходить из интерфейса переработчика)
        private Dictionary<uint, BasePlayer> recyclePlayer = new Dictionary<uint, BasePlayer>();
        void OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            if (!recycler.IsOn())
            {
                if (recyclePlayer.ContainsKey(recycler.net.ID))
                {
                    recyclePlayer.Remove(recycler.net.ID);
                    recyclePlayer.Add(recycler.net.ID, player);
                }
                else
                    recyclePlayer.Add(recycler.net.ID, player);
            }
            else
               if (recyclePlayer.ContainsKey(recycler.net.ID))
                recyclePlayer.Remove(recycler.net.ID);
        }
        void OnRecycleItem(Recycler recycler, Item item)
        {
            if (recyclePlayer.ContainsKey(recycler.net.ID))
            {
                int num2 = 1;
                if (item.amount > 1)
                    num2 = Mathf.CeilToInt(Mathf.Min((float)item.amount, (float)item.info.stackable * 0.1f));
                QuestProgress(recyclePlayer[recycler.net.ID], QuestType.RecycleItem, item.info.shortname, "", null, num2);
            }
        }
        #endregion
        #region (NEW) Growseedlings
        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            QuestProgress(player, QuestType.Growseedlings, item.info.shortname, "", null, item.amount);
        }
        #endregion
        #region (NEW) Raidable Bases (Nivex)
        void OnRaidableBaseCompleted(Vector3 location, int mode, bool allowPVP, string id, float spawnTime, float despawnTime, float loadingTime, ulong ownerId, BasePlayer owner, List<BasePlayer> raiders)
        {
            BasePlayer player = null;
            if (owner == null)
                if (raiders?.Count != 0)
                    player = raiders[0];
                else
                    return;
            else
                player = owner;

            if (player != null)
                QuestProgress(player, QuestType.RaidableBases, mode.ToString(), "", null);
        }
        #endregion
        #region (NEW) Fishing
        void OnFishCatch(Item fish, BaseFishingRod fishingRod, BasePlayer player)
        {
            if (player == null || fish == null)
                return;
            QuestProgress(player, QuestType.Fishing, fish.info.shortname, "", null, fish.amount);
        }
        #endregion

        #endregion
        private void OnNewSave()
        {
            if (config.settings.useWipe)
            {
                playersInfo?.Clear();
            }
        }
        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container.skinID == 1195832261)
            {
                MainUi(player);
                return false;
            }
            return null;
        }
        object CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (block.OwnerID == 76561198283599982)
                return false;
            else
                return null;
        }
        void Init()
        {
            LoadPlayerData();
            LoadQuestData();
            Unsubscribe(nameof(OnEntityTakeDamage));
        }

        private void OnServerInitialized()
        {
            monument = TerrainMeta.Path.Monuments.FirstOrDefault(p => p.name.ToLower().Contains("compound") && p.IsSafeZone == true);
            Instance = this;
            if (!CopyPaste)
            {
                NextTick(() => {
                    PrintError("Check if you have the 'Copy Paste'plugin installed");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }
            else if (CopyPaste.Version < new VersionNumber(4, 1, 27))
            {
                NextTick(() => {
                    PrintError("You have an old version of Copy Paste!\nplease update the plugin to the latest version (4.1.27 or higher) - https://umod.org/plugins/copy-paste");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }
            if (monument == null && !config.customPosition.useCustomPos)
            {
                NextTick(() =>
                {
                    PrintError(GetLang("XDQUEST_MissingOutPost"));
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }
            if (QuestList.Count == 0)
            {
                NextTick(() =>
                {
                    PrintError(GetLang("XDQUEST_MissingQuests"));
                });
                return;
            }
            //cmd.AddChatCommand(config.settings.questListProgress, this, nameof(OpenMQL_CMD));
            ImageUi.DownloadImages();
            ServerMgr.Instance.StartCoroutine(DownloadImages());   
            LoadDataCopyPaste();
            LoadDataSound();
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
            if (config.settings.useDecay)
                Subscribe(nameof(OnEntityTakeDamage));
        }
        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if (info.damageTypes.Has(DamageType.Decay))
            {
                if (victim?.OwnerID == 76561198283599982)
                {
                    info.damageTypes.Scale(DamageType.Decay, 0);
                }
            }
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (!playersInfo.ContainsKey(player.userID))
            {
                playersInfo.Add(player.userID, new PlayerData());
            }
            else
            {
                foreach (var item in playersInfo[player.userID].PlayerQuestsAll)
                {
                    var curentQuest = QuestList.FirstOrDefault(p => p.DisplayName == item.parentQuest.DisplayName);
                    if (curentQuest == null)
                    {
                        NextTick(() => {
                            playersInfo[player.userID].PlayerQuestsAll.Remove(item);
                        });
                    }
                }
            }
            foreach (KeyValuePair<String, String> item in ImageUi.Images)
                SendImage(player, item.Key);

            PlayersTime.Add(player.userID, null);
        }
        void OnServerSave()
        {
            timer.Once(10f, SaveData);
        }
        private void OnPlayerDisconnected(BasePlayer d)
        {
            if (openQuestPlayers.Contains(d.userID))
                openQuestPlayers.Remove(d.userID);
            if (PlayersTime.ContainsKey(d.userID))
            {
                if (PlayersTime[d.userID] != null)
                    ServerMgr.Instance.StopCoroutine(PlayersTime[d.userID]);
                PlayersTime.Remove(d.userID);
            }
        }
        void OnServerShutdown() => Unload();
        void Unload()
        {
            Instance = null;
            if (config.settings.mapUse)
                Interface.CallHook("API_RemoveMarker", "xdquest");
            SaveData();
            if(zoneTrigger != null)
                UnityEngine.Object.DestroyImmediate(zoneTrigger);
            if(safeZone != null)
                UnityEngine.Object.DestroyImmediate(safeZone);

            for (int i = 0; i < HouseNPC.Count; i++)
            {
                if (!HouseNPC[i].IsDestroyed)
                    HouseNPC[i]?.Kill();
            }
            npc?.KillMessage();
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                if (PlayersTime.ContainsKey(p.userID))
                {
                    if (PlayersTime[p.userID] != null)
                        ServerMgr.Instance.StopCoroutine(PlayersTime[p.userID]);
                }
                PlayersTime.Clear();
                CuiHelper.DestroyUi(p, MiniQuestList); CuiHelper.DestroyUi(p, Layers);
            }
            ImageUi.Unload();
        }
        #endregion

        #region SoundNpc (HumanNpc)

        private readonly Hash<string, NpcSound> Sounds = new Hash<string, NpcSound>();
        private readonly Hash<string, NpcSound> cached = new Hash<string, NpcSound>();

        public class NpcSound
        {
            [JsonConverter(typeof(SoundFileConverter))]
            public List<byte[]> Data = new List<byte[]>();
        }

        private class SoundFileConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                JToken value = JToken.Load(reader);
                return Instance.FromSaveData(Compression.Uncompress(Convert.FromBase64String(value.ToString())));
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(List<byte>) == objectType;
            }
        }
        private void LoadDataSound()
        {
            var sound = config.settingsSoundNPC.heySound.Concat(config.settingsSoundNPC.byeSound).Concat(config.settingsSoundNPC.takeQuestSound).Concat(config.settingsSoundNPC.turnQuestSound);
            foreach (var item in sound)
                LoadDataSound(item);
        }
        private NpcSound LoadDataSound(string name)
        {
            NpcSound cache = cached[name];
            if (cache != null)
                return cache;

            if (Interface.Oxide.DataFileSystem.ExistsDatafile(Name + "/Sounds/" + name))
            {
                NpcSound data = Interface.GetMod().DataFileSystem.ReadObject<NpcSound>(Name + "/Sounds/" + name);
                if (data == null)
                    return null;

                cached[name] = data;
                return data;
            }
            else
            {
                PrintWarning(GetLang("XDQUEST_SoundLoadErrorExt", null, name));
                return null;
            }      
        }

        public List<uint> BotAlerts = new List<uint>();
        private Coroutine SoundRoutine { get; set; }
        public void SoundPlay(string clip)
        {
            if (BotAlerts.Contains(npc.net.ID))
                return;
            else
                BotAlerts.Add(npc.net.ID);

            if (SoundRoutine == null)
                SoundRoutine = InvokeHandler.Instance.StartCoroutine(API_NPC_SendToAll(clip));
        }
        private List<byte[]> FromSaveData(byte[] bytes)
        {
            List<int> dataSize = new List<int>();
            List<byte[]> dataBytes = new List<byte[]>();

            int offset = 0;
            while (true)
            {
                dataSize.Add(BitConverter.ToInt32(bytes, offset));
                offset += 4;

                int sum = dataSize.Sum();
                if (sum == bytes.Length - offset)
                {
                    break;
                }

                if (sum > bytes.Length - offset)
                {
                    throw new ArgumentOutOfRangeException(nameof(dataSize),
                        $"Voice Data is outside the saved range {dataSize.Sum()} > {bytes.Length - offset}");
                }
            }

            foreach (int size in dataSize)
            {
                dataBytes.Add(bytes.Skip(offset).Take(size).ToArray());
                offset += size;
            }

            return dataBytes;
        }
        private IEnumerator API_NPC_SendToAll(string clipName)
        {
            NpcSound sound = LoadDataSound(clipName);
            if (sound == null)
            {
                SoundRoutine = null;
                BotAlerts.Remove(npc.net.ID);
                yield break;
            }
            yield return CoroutineEx.waitForSeconds(0.1f);  

            foreach (var data in sound.Data)
            {
                if (npc == null)
                    break;
                SendSound(npc.net.ID, data);
                yield return CoroutineEx.waitForSeconds(0.07f);
            }
            SoundRoutine = null;
            BotAlerts.Remove(npc.net.ID);
            yield break;
        }

        private void SendSound(uint netId, byte[] data)
        {
            if (!Net.sv.write.Start())
                return;
            foreach (BasePlayer current in BasePlayer.activePlayerList.Where(current => current.IsConnected && Vector3.Distance(npc.transform.position, current.transform.position) <= 100))
            {
                if (npc == null)
                    return;
                Net.sv.write.PacketID(Message.Type.VoiceData);
                Net.sv.write.UInt32(netId);
                Net.sv.write.BytesWithSize(data);
                Net.sv.write.Send(new SendInfo(current.Connection) { priority = Priority.Immediate });
            }
        }
        #endregion

        #region ApiLoadData

        private void LoadDataCopyPaste(Boolean repeat = false)
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("copypaste/" + BuildingList["2"].name))
            {
                PrintWarning("Dont habe build for copypaste");
            }
            else
            {
                ClearEnt();
            }
        }

        private IEnumerator DownloadImages()
        {
            PrintWarning("Loading icon for item....");
            foreach (var img in QuestList)
            {
                for (int i = 0; i < img.PrizeList.Count; i++)
                {
                    var typeimg = img.PrizeList[i];
                    if (typeimg.type == PrizeType.CustomItem)
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", typeimg.ShortName + 128, typeimg.SkinID))
                            ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin/{typeimg.SkinID}/128", typeimg.ShortName + 128, typeimg.SkinID);
                    }
                    else if (typeimg.type == PrizeType.Command)
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", typeimg.Url))
                            ImageLibrary.Call("AddImage", typeimg.Url, typeimg.Url);
                    }
                    else
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", typeimg.ShortName + 128))
                            ImageLibrary.Call("AddImage", $"https://www.rustedit.io/images/imagelibrary/{typeimg.ShortName}.png", typeimg.ShortName + 128);
                    }
                    yield return new WaitForSeconds(0.05f);
                }
            }
            PrintWarning("All icon load!");
            yield return 0;
        }
        private static class ImageUi
        {
            public static Dictionary<string, string> Images = new Dictionary<string, string>();
            private static Dictionary<int, string> _images = new Dictionary<int, string>()
            {
                { 1, "https://i.imgur.com/MaVprLF.png" },
                { 2, "https://i.imgur.com/olNJLKj.png" },
                { 3, "https://i.imgur.com/0gaw5bk.png" },
                { 4, "https://i.imgur.com/SUpj74n.png" },
                { 5, "https://i.imgur.com/R4Z7kga.png" },
                { 6, "https://i.imgur.com/O5tKfx8.png" },
                { 7, "https://i.imgur.com/3oJDUaW.png" },
                { 8, "https://i.imgur.com/8O7rbkB.png" },
                { 9, "https://i.imgur.com/Vc644y4.png" },
                { 10, "https://i.imgur.com/P3VQCGj.png" },
                { 11, "https://i.imgur.com/teD0rUV.png" },
                { 12, "https://i.imgur.com/EMwKXBG.png" },
                { 13, "https://i.imgur.com/7DLsfNL.png" },
                { 14, "https://i.imgur.com/3XcngXN.png" },
                { 15, "https://i.imgur.com/suFxlPG.png" },
            };
            public static void DownloadImages()
            {
                ServerMgr.Instance.StartCoroutine(AddImage($"https://www.rustedit.io/images/imagelibrary/blueprintbase.png", "blueprintbase"));

                for (int i = 1; i < 16; i++)
                {
                    ServerMgr.Instance.StartCoroutine(AddImage($"{_images[i]}", i.ToString()));
                }
            }

            private static IEnumerator AddImage(string url, string name)
            {
                UnityWebRequest www = UnityWebRequest.Get(url);

                yield return www.SendWebRequest();
                if (Instance == null)
                    yield break;
                if (www.isNetworkError || www.isHttpError)
                {
                    Instance.PrintWarning(string.Format("Image download error! Error: {0}, Image name: {1}", www.error, name));
                    www.Dispose();

                    yield break;
                }

                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(www.downloadHandler.data);
                if (texture != null)
                {
                    byte[] bytes = texture.EncodeToPNG();
                    Interface.Oxide.DataFileSystem.WriteObject($"qwest/{name}", bytes.ToList());
                    var image = FileStorage.server.Store(bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                    if (!Images.ContainsKey(name))
                        Images.Add(name, image);
                    else
                        Images[name] = image;
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                www.Dispose();
                yield break;
            }

            public static string GetImage(String ImgKey)
            {
                if (Images.ContainsKey(ImgKey))
                    return Images[ImgKey];
                return Instance.GetImage("LOADING");
            }

            public static void Unload()
            {
                foreach (var item in Images)
                    FileStorage.server.RemoveExact(uint.Parse(item.Value), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID, 0U);
            }
        }
        #endregion

        #region HelpMetods
        private void QuestProgress(BasePlayer player, QuestType questType, String entName = "", String skinId = "", List<Item> items = null, int count = 1)
        {
            if (player == null || !playersInfo.ContainsKey(player.userID))
                return;

            var playerQuests = playersInfo[player.userID].PlayerQuestsAll.Where(x => x.parentQuest.QuestType == questType && x.Finished == false);
            if (playerQuests == null)
                return;
            foreach (PlayerQuest quest in playerQuests)
            {
                if (entName == "" && items == null)
                {
                    quest.AddCount(count);
                    return;
                }
                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        if (item.info.shortname.ToLower().Contains(quest.parentQuest.Target.ToLower()) || item.skin.ToString() == quest.parentQuest.Target)
                            quest.AddCount(item.amount);
                    }
                }
                else
                {
                    if (questType == QuestType.OreBonus || questType == QuestType.IQCases || questType == QuestType.IQHeadReward)
                    {
                        if (quest.parentQuest.Target == entName || quest.parentQuest.Target == "0")
                            quest.AddCount(count);
                        continue;
                    }
                    if (entName.ToLower().Contains(quest.parentQuest.Target.ToLower()) || skinId == quest.parentQuest.Target)
                        quest.AddCount(count);
                }
            }

        }
        void RunEffect(BasePlayer player, string path)
        {
            Effect effect = new Effect();
            effect.Init(Effect.Type.Generic, player.transform.position, player.transform.forward, (Connection)null);
            effect.pooledString = path; EffectNetwork.Send(effect, player.net.connection);
        }
        public static class TimeHelper
        {
            public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
            {
                string result = string.Empty;
                switch (language)
                {
                    case "ru":
                        int i = 0;
                        if (time.Days != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Days, "д", "д", "д")}";
                            i++;
                        }
                        if (time.Hours != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Hours, "ч", "ч", "ч")}";
                            i++;
                        }
                        if (time.Minutes != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Minutes, "м", "м", "м")}";
                            i++;
                        }
                        if (time.Days == 0)
                        {
                            if (time.Seconds != 0 && i < maxSubstr)
                            {
                                if (!string.IsNullOrEmpty(result))
                                    result += " ";

                                result += $"{Format(time.Seconds, "с", "с", "с")}";
                                i++;
                            }
                        }
                        break;
                    default:
                        result = string.Format("{0}{1}{2}{3}",
                            time.Duration().Days > 0
                                ? $"{time.Days:0} day{(time.Days == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Hours > 0
                                ? $"{time.Hours:0} hour{(time.Hours == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Minutes > 0
                                ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Seconds > 0
                                ? $"{time.Seconds:0} second{(time.Seconds == 1 ? String.Empty : "s")}"
                                : string.Empty);

                        if (result.EndsWith(", ")) result = result.Substring(0, result.Length - 2);

                        if (string.IsNullOrEmpty(result)) result = "0 seconds";
                        break;
                }
                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units}{form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units}{form2}";

                return $"{units}{form3}";
            }
        }
        private static double CurrentTime() => Facepunch.Math.Epoch.Current;

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

        private class PasteData
        {
            public Dictionary<string, object> @default;
            public ICollection<Dictionary<string, object>> entities;
            public Dictionary<string, object> protocol;
        }
        public Vector3 GetResultVector()
        {
            if (config.customPosition.useCustomPos)
                return config.customPosition.pos;
            return monument.transform.position + monument.transform.rotation * BuildingList["2"].pos;
        }
        private IEnumerable<BasePlayer> FindMyBot(ulong userid)
        {
            return BasePlayer.allPlayerList.Where(x => x.userID == userid);
        }
        void Log(string msg, string file)
        {
            LogToFile(file, $"[{DateTime.Now}] {msg}", this);
        }
        #endregion

        #region NewUi

        List<ulong> openQuestPlayers = new List<ulong>();
        private Dictionary<ulong, Coroutine> PlayersTime = new Dictionary<ulong, Coroutine>();
        private const string MiniQuestList = "Mini_QuestList";
        private const string Layers = "UI_QuestMain";
        private const string LayerMainBackground = "UI_QuestMainBackground";

        #region MainUI

        void MainUi(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Overlay", Layers);

            container.Add(new CuiElement
            {
                Name = LayerMainBackground,
                Parent = Layers,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",Png = ImageUi.GetImage("1")},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "CloseUIImage",
                Parent = LayerMainBackground,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",Png = ImageUi.GetImage("2")},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "96.039 87.558", OffsetMax = "135.315 114.647" }                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = "CloseMainUI" },
                Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "96.039 87.558", OffsetMax = "135.315 114.647" }
            }, LayerMainBackground, "BtnCloseUI");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "96.227 191.4", OffsetMax = "208.973 211.399" },
                Text = { Text = GetLang("XDQUEST_UI_TASKLIST", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "0.7169812 0.7169812 0.7169812 1" }
            }, LayerMainBackground, "LabelQuestList");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-269.184 -102.227", OffsetMax = "-197.242 -72.373" },
                Text = { Text = GetLang("XDQUEST_UI_Awards", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, LayerMainBackground, "PrizeTitle");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "250.187 191.399", OffsetMax = "350.187 211.401" },
                Text = { Text = GetLang("XDQUEST_UI_TASKCount", player.UserIDString, QuestList.Count), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
            }, LayerMainBackground, "LabelQuestCount");

            CuiHelper.DestroyUi(player, "UI_QuestMain");
            CuiHelper.AddUi(player, container);
            QuestListUI(player);
            QuestInfo(player, 0);
        }

        #endregion

        #region QuestList
        void QuestListUI(BasePlayer player, int page = 0)
        {
            PlayerData playerQuests = playersInfo[player.userID];
            if (playerQuests == null)
                return;
            int y = 0;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "96.23 -234.241", OffsetMax = "347.79 181.441" }
            }, LayerMainBackground, "QuestListPanel");

            #region PageSettings

            if (page != 0)
            {
                container.Add(new CuiElement
                {
                    Parent = LayerMainBackground,
                    Name = "UPBTN",
                    Components =
                    {
                        new CuiRawImageComponent { Png = ImageUi.GetImage("3"), Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "182.89 87.565", OffsetMax = "221.51 114.635" }   }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_Handler page {page - 1}" },
                    Text = { Text = "" }
                }, "UPBTN");
            }
            if (page + 1 < (int)Math.Ceiling((double)QuestList.Count / 6))
            {
                container.Add(new CuiElement
                {
                    Parent = LayerMainBackground,
                    Name = "DOWNBTN",
                    Components =
                    {
                        new CuiRawImageComponent { Png = ImageUi.GetImage("4"), Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "139.598 87.568", OffsetMax = "178.326 114.632" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_Handler page {page + 1}" },
                    Text = { Text = "" }
                }, "DOWNBTN");
            }

            #endregion

            var ql = QuestList.OrderByDescending(q => playerQuests.PlayerQuestsAll.Exists(x => x.parentQuest.DisplayName == q.DisplayName ))
                .ThenByDescending(r => !playerQuests.PlayerQuestsFinish.Exists(q => q == r.DisplayName) && !playerQuests.PlayerQuestsCooldown.ContainsKey(r.DisplayName));

            foreach (var item in ql.Skip(page * 6))
            {
                Int32 index = QuestList.IndexOf(item);
                var curentQuest = playerQuests.PlayerQuestsAll.FirstOrDefault(p => p.parentQuest.DisplayName == item.DisplayName);
                container.Add(new CuiElement
                {
                    Name = "Quest",
                    Parent = "QuestListPanel",
                    Components = {
                    new CuiRawImageComponent { Color = $"1 1 1 1", Png = ImageUi.GetImage("5") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-125.78 {-67.933 - (y * 69.413)}", OffsetMax = $"125.78 {-1.06 - (y * 69.413)}" }
                }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-109.661 -33", OffsetMax = "113.14 -12.085" },
                    Text = { Text = item.DisplayName, Font = "robotocondensed-bold.ttf", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, "Quest", "QuestName");

                if (curentQuest != null)
                {
                    String Img = "";
                    String Txt = "";
                    if (curentQuest.Finished)
                    {
                        Img = "15";
                        Txt = GetLang("XDQUEST_UI_CHIPperformed", player.UserIDString);
                    }
                    else
                    {
                        Img = "14";
                        Txt = GetLang("XDQUEST_UI_CHIPInProgress", player.UserIDString);
                    }

                    container.Add(new CuiElement
                    {
                        Name = "QuestBar",
                        Parent = "Quest",
                        Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage(Img) },
                            new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "17.19 -16.717", OffsetMax = "97.902 -2.411" }
                        }
                    });
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-34.924 -7.153", OffsetMax = "40.356 7.153" },
                        Text = { Text = Txt, Font = "robotocondensed-bold.ttf", FontSize = 10, Align = TextAnchor.UpperCenter, Color = "1 1 1 1" }
                    }, "QuestBar", "BarLabel");
                }


                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"UI_Handler questinfo {index}" },
                    Text = { Text = "" }
                }, $"Quest");


                if (y >= 5)
                    break;
                y++;
            }

            CuiHelper.DestroyUi(player, "DOWNBTN");
            CuiHelper.DestroyUi(player, "UPBTN");
            CuiHelper.DestroyUi(player, "QuestListPanel");
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region QuestInfo
        private void QuestInfo(BasePlayer player, int quest)
        {
            List<PlayerQuest> playerQuests = playersInfo[player.userID].PlayerQuestsAll;
            if (playerQuests == null)
                return;
            player.SetFlag(BaseEntity.Flags.Reserved3, false);
            var quests = QuestList[quest];
            var curentQuest = playerQuests.FirstOrDefault(p => p.parentQuest.DisplayName == quests.DisplayName);

            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-280.488 -234.241", OffsetMax = "564.144 212.279" }
            }, LayerMainBackground, "QuestInfoPanel");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "23.704 -42.956", OffsetMax = "420.496 -16.044" },
                Text = { Text = quests.DisplayName, Font = "robotocondensed-bold.ttf", FontSize = 19, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, "QuestInfoPanel", "QuestName");

            string userepeat = quests.UseRepeat ? GetLang("XDQUEST_UI_QUESTREPEATCAN", player.UserIDString) : GetLang("XDQUEST_UI_QUESTREPEATfForbidden", player.UserIDString);
            string useCooldown = quests.Cooldown > 0 ? TimeHelper.FormatTime(TimeSpan.FromSeconds(quests.Cooldown), 5, lang.GetLanguage(player.UserIDString)) : GetLang("XDQUEST_UI_Missing", player.UserIDString);
            string bring = quests.Bring ? GetLang("XDQUEST_UI_QuestNecessary", player.UserIDString) : GetLang("XDQUEST_UI_QuestNotNecessary", player.UserIDString);
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "23.705 -54.066", OffsetMax = "420.495 -40.134" },
                Text = { Text = GetLang("XDQUEST_UI_InfoRepeatInCD", player.UserIDString, userepeat, useCooldown, bring), Font = "robotocondensed-regular.ttf", FontSize = 10, Align = TextAnchor.UpperLeft, Color = "0.9607844 0.5843138 0.1960784 1" }
            }, "QuestInfoPanel", "QuestInfo2");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-398.895 -289.293", OffsetMax = "106.815 -76.2" },
                Text = { Text = quests.Description, Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" }
            }, "QuestInfoPanel", "QuestDescription");

            #region QuestButton

            string command = "";
            string image = "";
            string text = "";
            if (curentQuest == null)
            {
                if (!quests.UseRepeat && playersInfo[player.userID].PlayerQuestsFinish.Contains(quests.DisplayName))
                {
                    text = GetLang("XDQUEST_UI_QuestBtnPerformed", player.UserIDString);
                    image = "6";
                    command = $"UI_Handler get {quest}";
                }
                else
                {
                    text = GetLang("XDQUEST_UI_QuestBtnTake", player.UserIDString);
                    image = "7";
                    command = $"UI_Handler get {quest}";
                }
            }
            else if (curentQuest.Finished)
            {
                text = GetLang("XDQUEST_UI_QuestBtnPass", player.UserIDString);
                image = "7";
                command = $"UI_Handler finish {quest}";
            }
            else
            {
                text = GetLang("XDQUEST_UI_QuestBtnRefuse", player.UserIDString);
                image = "6";
                command = $"UI_Handler finish {quest}";
            }
            if (playersInfo[player.userID].PlayerQuestsCooldown != null && playersInfo[player.userID].PlayerQuestsCooldown.ContainsKey(quests.DisplayName) && playersInfo[player.userID].PlayerQuestsCooldown[quests.DisplayName] >= CurrentTime())
            {
                text = lang.GetMessage(TimeHelper.FormatTime(TimeSpan.FromSeconds(playersInfo[player.userID].PlayerQuestsCooldown[quests.DisplayName] - CurrentTime()), 5, lang.GetLanguage(player.UserIDString)), this, player.UserIDString);
                image = "6";
                command = $"UI_Handler coldown";
                player.SetFlag(BaseEntity.Flags.Reserved3, true);
            }

            container.Add(new CuiElement
            {
                Name = Layers + "QuestButtonImage",
                Parent = "QuestInfoPanel",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png =ImageUi.GetImage(image)},
                    new CuiRectTransformComponent { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-416.142 -49.709", OffsetMax = "-306.058 -7.691" }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = command },
                Text = { Text = text, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-55.039 -21.01", OffsetMax = "55.041 21.009" }
            }, Layers + "QuestButtonImage", Layers + "ButtonQuest");

            #endregion

            #region QuestCheckBox

            container.Add(new CuiElement
            {
                Name = "QuestCheckBox",
                Parent = "QuestInfoPanel",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1",  Png = ImageUi.GetImage("9")  },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-279.228 1.334", OffsetMax = "-1.217 125.64" }
                }
            });

            String CheckBox = curentQuest == null ? "10" : curentQuest.Finished ? "11" : "10";

            container.Add(new CuiElement
            {
                Name = "CheckBoxImg",
                Parent = "QuestCheckBox",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage(CheckBox) },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "20.729 -35.467", OffsetMax = "38.205 -18.005" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-91.326 -55.693", OffsetMax = "136.647 -16.904" },
                Text = { Text = quests.Missions, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "1 1 1 1" }
            }, "QuestCheckBox", "CheckBoxTxt");

            if (curentQuest != null)
            {
                Double Factor = 278.005 * curentQuest.Count / curentQuest.parentQuest.Amount;
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.3843138 0.3686275 0.3843138 0.9137255" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "-0.000 -0.153", OffsetMax = $"278.005 40.106" }
                }, "QuestCheckBox", "QuestProgresBar");
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.4462442 0.8679245 0.5786404 0.6137255" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "-0.000 -0.153", OffsetMax = $"{Factor} 40.106" }
                }, "QuestProgresBar");
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-139.005 -20.129", OffsetMax = "139.005 20.13" },
                    Text = { Text = $"{curentQuest.Count} / {curentQuest.parentQuest.Amount}", Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "QuestProgresBar", "Progres");
            }

            #endregion

            #region PrizeList
            for (int i = 0, x = 0, y = 0; i < quests.PrizeList.Count; i++)
            {
                var prize = quests.PrizeList[i];

                string prizeLayer = "QuestInfo" + $".{i}";
                container.Add(new CuiElement
                {
                    Name = prizeLayer,
                    Parent = "QuestInfoPanel",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("8")},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{23.42 + (x * 120.912)} {79.39 - (y * 78.345)}", OffsetMax = $"{129.555 + (x * 120.912)} {125.9 - (y * 78.345)}" }
                }
                });

                var img = prize.type == PrizeType.CustomItem ? GetImage(prize.ShortName + 128, prize.SkinID) : prize.type == PrizeType.Item ? GetImage(prize.ShortName + 128) : prize.type == PrizeType.Command ? GetImage(prize.Url) : "";
                if (img != "")
                {
                    container.Add(new CuiElement
                    {
                        Parent = prizeLayer,
                        Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png = img },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.059 -20.625", OffsetMax = "32.941 22.375" }
                        }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Parent = prizeLayer,
                        Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png =ImageUi.GetImage("blueprintbase") },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.059 -20.625", OffsetMax = "32.941 22.375" }
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = prizeLayer,
                        Components = {
                            new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage(prize.ShortName + 128) },
                            new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-10.059 -20.625", OffsetMax = "32.941 22.375" }
                        }
                    });
                }
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-61.669 0.67", OffsetMax = "-5.931 17.33" },
                    Text = { Text = $"x{prize.Amount}", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleRight, Color = "1 1 1 1" }
                }, prizeLayer);

                x++;
                if (x == 4)
                {
                    x = 0;
                    y++;

                    if (y == 2)
                    {
                        break;
                    }
                }
            }


            #endregion

            CuiHelper.DestroyUi(player, "QuestInfoPanel");
            CuiHelper.AddUi(player, container);

            if (playersInfo[player.userID].PlayerQuestsCooldown.ContainsKey(quests.DisplayName))
            {
                if (PlayersTime[player.userID] != null)
                    ServerMgr.Instance.StopCoroutine(PlayersTime[player.userID]);
                PlayersTime[player.userID] = ServerMgr.Instance.StartCoroutine(StartUpdate(player, quest));
            }
        }

        private IEnumerator StartUpdate(BasePlayer player, int quest)
        {
            var check = QuestList[quest];

            while (player.HasFlag(BaseEntity.Flags.Reserved3))
            {
                string questLayer = Layers + "ButtonQuest";

                if (playersInfo[player.userID].PlayerQuestsCooldown.ContainsKey(check.DisplayName) && playersInfo[player.userID]?.PlayerQuestsCooldown[check.DisplayName] >= CurrentTime())
                {
                    CuiElementContainer container = new CuiElementContainer();
                    CuiHelper.DestroyUi(player, questLayer);

                    string text = TimeHelper.FormatTime(TimeSpan.FromSeconds(playersInfo[player.userID].PlayerQuestsCooldown[check.DisplayName] - CurrentTime()), 5, lang.GetLanguage(player.UserIDString));

                    container.Add(new CuiButton
                    {
                        Button = { Color = "0 0 0 0", Command = "UI_Handler coldown" },
                        Text = { Text = text, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-55.039 -21.01", OffsetMax = "55.041 21.009" }
                    }, Layers + "QuestButtonImage", Layers + "ButtonQuest");

                    CuiHelper.AddUi(player, container);
                }
                else if (playersInfo[player.userID].PlayerQuestsCooldown[check.DisplayName] != 0)
                {
                    playersInfo[player.userID].PlayerQuestsCooldown.Remove(check.DisplayName);
                    QuestInfo(player, quest);
                }

                yield return new WaitForSeconds(1);
            }
        }
        #endregion

        #region MiniQuestList
        private void OpenMQL_CMD(BasePlayer player) => UIMiniQuestList(player);

        void UIMiniQuestList(BasePlayer player, int page = 0)
        {
            List<PlayerQuest> playerQuests = playersInfo[player.userID].PlayerQuestsAll;
            if (playerQuests == null)
                return;
            if (playerQuests.Count == 0)
            {
                SendReply(player, GetLang("XDQUEST_UI_ActiveQuestCount", player.UserIDString));
                if (openQuestPlayers.Contains(player.userID))
                    openQuestPlayers.Remove(player.userID);
                return;
            }
            if (!openQuestPlayers.Contains(player.userID))
                openQuestPlayers.Add(player.userID);

            IEnumerable<PlayerQuest> qlist = playerQuests.Skip(page * 8).Take(8);
            int questCount = qlist.Count();
            int qc = -72 * questCount;
            Double ds = 207.912 + qc;
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = $"0 {ds}", OffsetMax = "304.808 303.288" }
            }, "Overlay", MiniQuestList);

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = "CloseMiniQuestList" },
                Text = { Text = "x", Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 0 0 1" },
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-20 -20", OffsetMax = "0 0" }
            }, MiniQuestList, "MiniQuestClosseBtn");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "3.825 -23.035", OffsetMax = "173.821 0" },
                Text = { Text = GetLang("XDQUEST_UI_ACTIVEOBJECTIVES", player.UserIDString, playerQuests.Count), Font = "robotocondensed-bold.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, MiniQuestList, "LabelMiniQuestPanel");       

            int size = 72, i = 0;
            foreach (var item in qlist)
            {
                String color = item.Finished ? "0.1960784 0.7176471 0.4235294 1" : "0.9490197 0.3764706 0.3960785 1";
                container.Add(new CuiElement
                {
                    Name = "MiniQuestImage",
                    Parent = MiniQuestList,
                    Components = {
                    new CuiRawImageComponent { Color = "0 0 0 1", Png = ImageUi.GetImage("5") },
                    new CuiRectTransformComponent {  AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"3.829 {-90.188 - i*size}", OffsetMax = $"299.599 {-23.035 - i*size}" }
                }
                });
                container.Add(new CuiElement
                {
                    Name = "ImgForMiniQuest",
                    Parent = "MiniQuestImage",
                    Components = {
                    new CuiRawImageComponent { Color = color, Png = ImageUi.GetImage("13") },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0.112 -33.576", OffsetMax = "12.577 33.577" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "LabelForMiniQuest",
                    Parent = "MiniQuestImage",
                    Components = {
                    new CuiTextComponent { Text =GetLang("XDQUEST_UI_MiniQLInfo", player.UserIDString, item.parentQuest.DisplayName, item.Count, item.parentQuest.Amount, item.parentQuest.Missions), Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" },
                    new CuiOutlineComponent {Color = "0 0 0 1", Distance = "0.6 0.6"},
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "14.925 -28.867", OffsetMax = "283.625 28.867" }
                }
                });
                i++;
            }

            #region Page
            int pageCount = (int)Math.Ceiling((double)playerQuests.Count / 8);
            if (pageCount > 1)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0" },
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"3.829 {-126.593 - (questCount - 1) * size}", OffsetMax = $"145.353 {-90.187 - (questCount - 1) * size}" }
                }, MiniQuestList, "Panel_1410");
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-22.598 -11.514", OffsetMax = "21.517 11.514" },
                    Text = { Text = $"{page + 1}/{pageCount}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, "Panel_1410");
                if (page + 1 < pageCount)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "Panel_1410",
                        Name = "DOWNBTN",
                        Components =
                    {
                        new CuiRawImageComponent { Png = ImageUi.GetImage("4"), Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-61.326 -13.326", OffsetMax = "-22.598 13.535" }
                    }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = $"UI_Handler pageQLIST {page + 1}" },
                        Text = { Text = "" }
                    }, "DOWNBTN");
                }
                if (page != 0)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "Panel_1410",
                        Name = "UPBTN",
                        Components =
                    {
                        new CuiRawImageComponent { Png = ImageUi.GetImage("3"), Color = "1 1 1 1" },
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "21.517 -13.326", OffsetMax = "60.138 13.743" }   }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = $"UI_Handler pageQLIST {page - 1}" },
                        Text = { Text = "" }
                    }, "UPBTN");
                }
            }
            
            #endregion
            CuiHelper.DestroyUi(player, MiniQuestList);
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Notice
        private void UINottice(BasePlayer player, string msg, string sprite = "assets/icons/warning.png", string color = "0.76 0.34 0.10 1.00")
        {
            var container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                FadeOut = 0.30f,
                Name = "QuestUiNotice",
                Parent = LayerMainBackground,
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = ImageUi.GetImage("12"), FadeIn = 0.30f },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "96.235 -110.028", OffsetMax = "391.685 -43.111" }                }
            });

            container.Add(new CuiElement
            {
                FadeOut = 0.30f,
                Name = "NoticeFeed",
                Parent = "QuestUiNotice",
                Components = {
                    new CuiRawImageComponent { Color = color, Png = ImageUi.GetImage("13"), FadeIn = 0.30f },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0.276 -33.458", OffsetMax = "12.692 33.459" }
                }
            });
            //container.Add(new CuiElement
            //{
            //    Parent = "QuestUi",
            //    Components = {
            //        new CuiRawImageComponent { Color = HexToRustFormat(color), Png = ImageUi.GetImage("16"), FadeIn = 0.30f },
            //        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0.45 -23.24", OffsetMax = "1.3567 12.1866" }
            //    }
            //});

            container.Add(new CuiElement
            {
                FadeOut = 0.30f,
                Name = "NoticeSprite",
                Parent = "QuestUiNotice",
                Components = {
                    new CuiImageComponent { Color = "1 1 1 1", Sprite = sprite, FadeIn = 0.30f },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "23.5 -15.5", OffsetMax = "54.5 15.5" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-78.262 -33.458", OffsetMax = "143.522 33.459" },
                Text = { Text = msg, Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleLeft, Color = "1 1 1 1", FadeIn = 0.30f }
            }, "QuestUiNotice", "NoticeText");

            CuiHelper.DestroyUi(player, "NoticeText");
            CuiHelper.DestroyUi(player, "NoticeSprite");
            CuiHelper.DestroyUi(player, "NoticeFeed");
            CuiHelper.DestroyUi(player, "QuestUiNotice");
            CuiHelper.AddUi(player, container);
            timer.Once(3.5f, () => {
                CuiHelper.DestroyUi(player, "NoticeText");
                CuiHelper.DestroyUi(player, "NoticeSprite");
                CuiHelper.DestroyUi(player, "NoticeFeed");
                CuiHelper.DestroyUi(player, "QuestUiNotice");
            });
        }

        #endregion

        #endregion

        #region Command    
        [ConsoleCommand("CloseMiniQuestList")]
        void CloseMiniQuestList(ConsoleSystem.Arg arg)
        {
            CuiHelper.DestroyUi(arg.Player(), MiniQuestList);
            if (openQuestPlayers.Contains(arg.Player().userID))
                openQuestPlayers.Remove(arg.Player().userID);
        }

        [ConsoleCommand("CloseMainUI")]
        void CloseLayerPlayer(ConsoleSystem.Arg arg)
        {
            CuiHelper.DestroyUi(arg.Player(), Layers);
            arg.Player().SetFlag(BaseEntity.Flags.Reserved3, false);
            if (PlayersTime[arg.Player().userID] != null)
                ServerMgr.Instance.StopCoroutine(PlayersTime[arg.Player().userID]);
        }

        [ChatCommand("quest.saveposition")]
        void CustomPosSave(BasePlayer player)
        {
            config.customPosition.pos = player.transform.position;
            SaveConfig();
            PrintToChat(player, GetLang("XDQUEST_UI_CMDCustomPosAdd", player.UserIDString));
        }

        [ChatCommand("quest.tphouse")]
        void TpToQuestHouse(BasePlayer player)
        {
            if (player.IsAdmin)
                player.Teleport(GetResultVector());
        }

        [ConsoleCommand("UI_Handler")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            List<PlayerQuest> playerQuests = playersInfo[player.userID].PlayerQuestsAll;
            if (playerQuests == null)
                return;

            if (player != null && args.HasArgs(1))
            {
                switch (args.Args[0])
                {
                    case "get":
                        {
                            int questIndex;
                            if (args.HasArgs(2) && int.TryParse(args.Args[1], out questIndex))
                            {
                                var currentQuest = QuestList[questIndex];
                                if (currentQuest != null)
                                {
                                    if (playerQuests.Count >= config.settings.questCount)
                                    {
                                        UINottice(player, GetLang("XDQUEST_UI_QuestLimit", player.UserIDString));
                                        return;
                                    }
                                    if (playerQuests.Any(p => p.parentQuest.DisplayName == currentQuest.DisplayName))
                                    {
                                        UINottice(player, GetLang("XDQUEST_UI_AlreadyTaken", player.UserIDString));
                                        return;
                                    }
                                    if (!currentQuest.UseRepeat && playersInfo[player.userID].PlayerQuestsFinish.Contains(currentQuest.DisplayName))
                                    {
                                        UINottice(player, GetLang("XDQUEST_UI_AlreadyDone", player.UserIDString));
                                        return;
                                    }
                                    if (playersInfo[player.userID].PlayerQuestsFinish.Contains(currentQuest.DisplayName))
                                    {
                                        UINottice(player, GetLang("XDQUEST_UI_AlreadyDone", player.UserIDString));
                                        return;
                                    }
                                    playerQuests.Add(new PlayerQuest() { UserID = player.userID, parentQuest = currentQuest });
                                    QuestListUI(player, 0);
                                    QuestInfo(player, questIndex);
                                    UINottice(player, GetLang("XDQUEST_UI_TookTasks", player.UserIDString, currentQuest.DisplayName));
                                    if (Instance.config.settingsSoundNPC.takeQuestSound.Count != 0 && config.settingsSoundNPC.soundUse)
                                    {
                                        int mIndex = Random.Range(0, Instance.config.settingsSoundNPC.takeQuestSound.Count);
                                        Instance.SoundPlay(Instance.config.settingsSoundNPC.takeQuestSound[mIndex]);
                                    }           
                                }
                            }
                            break;
                        }
                    case "page":
                        {
                            int pageIndex;
                            if (int.TryParse(args.Args[1], out pageIndex))
                            {
                                QuestListUI(player, pageIndex);
                            }
                            break;
                        }
                    case "pageQLIST":
                        {
                            int pageIndex;
                            if (int.TryParse(args.Args[1], out pageIndex))
                            {
                                UIMiniQuestList(player, pageIndex);
                            }
                            break;
                        }
                    case "coldown":
                        {
                            UINottice(player, GetLang("XDQUEST_UI_ACTIVECOLDOWN", player.UserIDString));
                            break;
                        }
                    case "questinfo":
                        {
                            int pageIndex;
                            if (int.TryParse(args.Args[1], out pageIndex))
                            {
                                QuestInfo(player, pageIndex);
                            }
                            break;
                        }
                    case "finish":
                        {
                            int questIndex;
                            if (args.HasArgs(2) && int.TryParse(args.Args[1], out questIndex))
                            {
                                var globalQuest = QuestList[questIndex];
                                if (globalQuest != null)
                                {
                                    var currentQuest = playerQuests.FirstOrDefault(p => p.parentQuest.DisplayName == globalQuest.DisplayName);
                                    if (currentQuest == null)
                                        return;

                                    if (currentQuest.Finished)
                                    {
                                        if (24 - player.inventory.containerMain.itemList.Count < currentQuest.parentQuest.PrizeList.Where(x => x.type != PrizeType.Command).Count())
                                        {
                                            UINottice(player, GetLang("XDQUEST_UI_LackOfSpace", player.UserIDString));
                                            return;
                                        }
                                        ulong skins;
                                        if (currentQuest.parentQuest.Bring)
                                        {
                                            if (currentQuest.parentQuest.QuestType == QuestType.Loot && ulong.TryParse(currentQuest.parentQuest.Target, out skins))
                                            {
                                                List<Item> acceptedItems = new List<Item>();
                                                Int32 itemAmount = 0;
                                                Int32 amountQuest = currentQuest.parentQuest.Amount;
                                                String itemName = String.Empty;
                                                foreach (Item item in player.inventory.AllItems())
                                                {
                                                    if (item.skin == skins)
                                                    {
                                                        acceptedItems.Add(item);
                                                        itemAmount += item.amount;
                                                        itemName = item.info.displayName.english;
                                                    }
                                                }

                                                if (acceptedItems.Count == 0)
                                                {
                                                    UINottice(player, GetLang("XDQUEST_UI_InsufficientResources", player.UserIDString));
                                                    return;
                                                }
                                                if (itemAmount < amountQuest)
                                                {
                                                    UINottice(player, GetLang("XDQUEST_UI_NotResourcesAmount", player.UserIDString, itemName, amountQuest));
                                                    return;
                                                }

                                                foreach (Item use in acceptedItems)
                                                {
                                                    if (use.amount == amountQuest)
                                                    {
                                                        use.RemoveFromContainer();
                                                        use.Remove();
                                                        amountQuest = 0;
                                                        break;
                                                    }
                                                    if (use.amount > amountQuest)
                                                    {
                                                        use.amount -= amountQuest;
                                                        player.inventory.SendSnapshot();
                                                        amountQuest = 0;
                                                        break;
                                                    }
                                                    if (use.amount < amountQuest)
                                                    {
                                                        amountQuest -= use.amount;
                                                        use.RemoveFromContainer();
                                                        use.Remove();
                                                    }
                                                }
                                            }
                                            else if (currentQuest.parentQuest.QuestType == QuestType.Gather || currentQuest.parentQuest.QuestType == QuestType.Loot || currentQuest.parentQuest.QuestType == QuestType.Craft || currentQuest.parentQuest.QuestType == QuestType.PurchaseFromNpc || currentQuest.parentQuest.QuestType == QuestType.Growseedlings || currentQuest.parentQuest.QuestType == QuestType.Fishing)
                                            {
                                                var idItem = ItemManager.FindItemDefinition(currentQuest.parentQuest.Target);
                                                var item = player?.inventory?.GetAmount(idItem.itemid);
                                                if (item == 0 || item == null)
                                                {
                                                    UINottice(player, GetLang("XDQUEST_UI_InsufficientResources", player.UserIDString, idItem.displayName.english));
                                                    return;
                                                }
                                                if (item < currentQuest.parentQuest.Amount)
                                                {
                                                    UINottice(player, GetLang("XDQUEST_UI_NotResourcesAmount", player.UserIDString, idItem.displayName.english, currentQuest.parentQuest.Amount));
                                                    return;
                                                }
                                                if (item >= currentQuest.parentQuest.Amount)
                                                {
                                                    player.inventory.Take(null, idItem.itemid, currentQuest.parentQuest.Amount);
                                                }

                                            }
                                        }

                                        UINottice(player, GetLang("XDQUEST_UI_QuestsCompleted", player.UserIDString));

                                        currentQuest.Finished = false;
                                        for (int i = 0; i < currentQuest.parentQuest.PrizeList.Count; i++)
                                        {
                                            var check = currentQuest.parentQuest.PrizeList[i];
                                            switch (check.type)
                                            {
                                                case PrizeType.Item:
                                                    Item newItem = ItemManager.CreateByPartialName(check.ShortName, check.Amount);
                                                    player.GiveItem(newItem, BaseEntity.GiveItemReason.Crafted);
                                                    break;
                                                case PrizeType.Command:
                                                    Server.Command(check.Command.Replace("%STEAMID%", player.UserIDString));
                                                    break;
                                                case PrizeType.CustomItem:
                                                    Item customItem = ItemManager.CreateByPartialName(check.ShortName, check.Amount, check.SkinID);
                                                    customItem.name = check.Name;
                                                    player.GiveItem(customItem, BaseEntity.GiveItemReason.Crafted);
                                                    break;
                                                case PrizeType.BluePrint:
                                                    Item itemBp = ItemManager.CreateByItemID(-996920608, check.Amount);
                                                    itemBp.blueprintTarget = ItemManager.itemList.Find(x => x.shortname == check.ShortName)?.itemid ?? 0;
                                                    player.GiveItem(itemBp, BaseEntity.GiveItemReason.Crafted);
                                                    break;
                                            }
                                        }
                                        if (!currentQuest.parentQuest.UseRepeat)
                                        {
                                            playersInfo[player.userID].PlayerQuestsFinish.Add(currentQuest.parentQuest.DisplayName);
                                        }
                                        else if(globalQuest.Cooldown > 0)
                                        {
                                            if (!playersInfo[player.userID].PlayerQuestsCooldown.ContainsKey(globalQuest.DisplayName))
                                                playersInfo[player.userID].PlayerQuestsCooldown.Add(globalQuest.DisplayName, CurrentTime() + globalQuest.Cooldown);
                                            else
                                                playersInfo[player.userID].PlayerQuestsCooldown[globalQuest.DisplayName] = CurrentTime() + globalQuest.Cooldown;
                                        }
                                        playerQuests.Remove(currentQuest);
                                        QuestListUI(player, 0);
                                        QuestInfo(player, questIndex);
                                        if (Instance.config.settingsSoundNPC.turnQuestSound.Count != 0 && config.settingsSoundNPC.soundUse)
                                        {
                                            int mIndex = Random.Range(0, Instance.config.settingsSoundNPC.turnQuestSound.Count);
                                            Instance.SoundPlay(Instance.config.settingsSoundNPC.turnQuestSound[mIndex]);
                                        }      
                                    }
                                    else
                                    {
                                        UINottice(player, GetLang("XDQUEST_UI_PassedTasks", player.UserIDString));
                                        playerQuests.Remove(currentQuest);
                                        QuestListUI(player, 0);
                                        QuestInfo(player, questIndex);
                                    }
                                }
                                else
                                    UINottice(player, "Вы <color=#4286f4>не брали</color> этого задания!");
                            }
                            break;
                        }
                }     
            }
        }
        #endregion

        #region Data
        private List<Quest> LoadQuestListBuffer()
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile(this.Name + $"/{config.settings.questListDataName}"))
            {
                return null;
            }

            var data = Interface.Oxide.DataFileSystem.ReadObject<List<Quest>>(this.Name + $"/{config.settings.questListDataName}");
            if (data == null)
                return null;
            return data;
        }
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
        private void LoadQuestData() 
        {
            var quest64 = LoadQuestListBuffer();
            if (quest64 == null)
            {
                QuestList = new List<Quest>();
                return;
            }
            //if (quest64.Data == null)
            //{
            //    QuestList = new List<Quest>();
            //    return;
            //}
            QuestList = quest64;
        }
        private void LoadPlayerData() => playersInfo = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>(this.Name + $"/PlayerInfo");
        private void SavePlayerData() => Interface.GetMod().DataFileSystem.WriteObject(this.Name + $"/PlayerInfo", playersInfo);
        void SaveData()
        {
            SavePlayerData();   
        }
        #endregion
    }
}
