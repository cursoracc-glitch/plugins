using Newtonsoft.Json;
using Oxide.Core;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Network;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using System.Collections;
using System.Text;

namespace Oxide.Plugins
{
    [Info("XDQuest", "DezLife", "2.0.5")]
    [Description("Расширенная квест система для вашего сервера!")]
    public class XDQuest : RustPlugin
    {
        #region Var
        private const string AuthorContact = "DezLife#1480 \nvk.com/dezlife";
        private const string filename = "XDQuestHouseNPC";
        private static XDQuest Instance;
        MonumentInfo monument;
        HashSet<Item> ItemForce = new HashSet<Item>();
        private BasePlayer npc;
        private List<BaseEntity> HouseNPC = new List<BaseEntity>();
        private Dictionary<string, string> ImageUI = new Dictionary<string, string>()
        {
            {"MAINFON", "https://i.imgur.com/sV7tvFE.png" },
            {"QUESTFON", "https://i.imgur.com/3yfcpYV.png" },
            {"DOWNBTN", "https://i.imgur.com/VYdpOFv.png" },
            {"UPBTN", "https://i.imgur.com/3E9KNpZ.png" },
            {"BluePrint", "https://i.imgur.com/b48U2XA.png" },
            {"InProcces", "https://i.imgur.com/IKE6USt.png" },
            {"CloseUI", "https://i.imgur.com/7oInHGR.png" }
        };
        #region Ref

        [PluginReference] Plugin CopyPaste, ImageLibrary, IQChat, Friends, Clans, Battles, Duel;

        public void SendChat(BasePlayer player, string Message, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, config.settingsIQChat.prifix, config.settingsIQChat.SteamID);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        public bool IsFriends(ulong userID, ulong targetID)
        {
            if (Friends)
                return (bool)Friends?.Call("HasFriend", userID, targetID);
            else return false;
        }
        public bool IsClans(ulong userID, ulong targetID)
        {
            if (Clans)
                return (bool)Clans?.Call("HasFriend", userID, targetID);
            else return false;
        }
        public bool IsDuel(ulong userID)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel) return (bool)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else return false;
        }

        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);
        #endregion

        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["QUEST_ACTIVE"] = "Your active tasks: {0}",
                ["NOT_QUEST_ACTIVE"] = "You have no active tasks",
                ["QUEST_Insufficient_resources"] = "You don't have {0}, you should definitely bring this to Sidorovich",
                ["QUEST_not_resources"] = "You don't have enough {0}, you need {1}",
                ["QUEST_ACTIVE_COMPLITE"] = "{0}\nQuests completed!\nDo not forget to hand it over to Sidorovich.",
                ["NOT_QUEST_ACTIVE_COMPLITE"] = "<size=9>{0}</size>\nLeft: {1}\n{2}",
                ["AVAILABLE_MISSIONS"] = "<b>AVAILABLE JOBS</b><size=14>({0})</size>",
                ["ACTIVE_MISSIONS"] = "<b>ACTIVE JOBS</B><size=14>({0})</size>",
                ["NOT_AVAILABLE_MISSIONS"] = "You have no tasks available :(",
                ["REWARD_FOR_QUESTIONS"] = "<b>REWARD FOR COMPLETING THE MISSION</b>",
                ["CAN"] = "can",
                ["CAN'T"] = "can't",
                ["absent's"] = "absent",
                ["QUEST_ACTIVE_LIMIT"] = "You have too much <color=#4286f4>unfinished</color> assignments!",
                ["QUEST_ACTIVE_COLDOWN"] = "You cannot take this quest at the moment",
                ["QUEST_took_tasks"] = "You already <color=#4286f4>have taken</color> this task!",
                ["QUEST_completed_tasks"] = "You already <color=#4286f4>performed</color> this task!",
                ["QUEST_completed_took"] = "You <color=#4286f4>successfully</color> took the task {0}",
                ["QUEST_tasks_completed"] = "Thanks, keep your <color=#4286f4>reward</color>!",
                ["QUEST_no_place"] = "Hey wait, you're everything <color=#4286f4>you won't take</color>, make room!",
                ["QUEST_did_not_cope"] = "Sorry that you <color=#4286f4>did not cope</color> with the task!\n" +
                 $"Anyway, you can try again!",
                ["QUEST_done"] = "Performed!",
                ["QUEST_take"] = "TAKE",
                ["QUEST_turn"] = "Hand over",
                ["QUEST_REFUSE"] = "REFUSE",
                ["QUEST_Finished"] = "You have completed the task: <color=#4286f4>{0}</color>",
                ["QUEST_DONTREPEAT"] = "You have already completed this quest.!",
                ["QUEST_target"] = "Need to: {0}\n" +
                "Re-take {1}\n" +
                "CD to re-take : {2}",
                ["QUEST_targetrtho"] = "Need to: {0}\n" +
                "Progress: {1}/{2}\n" +
                "Re-take {3}\n" +
                "CD to re-take : {4}",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["QUEST_ACTIVE"] = "Ваши активные задачи: {0}",
                ["NOT_QUEST_ACTIVE"] = "У вас нет активных задач",
                ["QUEST_Insufficient_resources"] = "У вас нету {0}, нужно обязательно принести это сидоровичу",
                ["QUEST_not_resources"] = "У вас не достаточно {0},  нужно {1}",
                ["QUEST_ACTIVE_COMPLITE"] = "{0}\nЗадания выполнено!\nНе забудьте сдать его сидоровичу.",
                ["NOT_QUEST_ACTIVE_COMPLITE"] = "<size=9>{0}</size>\nПрогресс: {1}/{2}\n{3}",
                ["AVAILABLE_MISSIONS"] = "<b>ДОСТУПНЫЕ ЗАДАНИЯ</b><size=14>({0})</size>",
                ["ACTIVE_MISSIONS"] = "<b>АКТИВНЫЕ ЗАДАНИЯ</B><size=14>({0})</size>",
                ["NOT_AVAILABLE_MISSIONS"] = "У вас нет доступных задач :(",
                ["REWARD_FOR_QUESTIONS"] = "<b>НАГРАДА ЗА ВЫПОЛНЕНИЯ ЗАДАНИЯ</b>",
                ["CAN"] = "можно",
                ["CAN'T"] = "нельзя",
                ["absent's"] = "отсутствует",
                ["QUEST_ACTIVE_LIMIT"] = "У тебя слишком много <color=#4286f4>не законченных</color> заданий!",
                ["QUEST_ACTIVE_COLDOWN"] = "В данный момент вы не можете взять этот квест",
                ["QUEST_took_tasks"] = "Вы уже <color=#4286f4>взяли</color> это задание!",
                ["QUEST_completed_tasks"] = "Вы уже <color=#4286f4>выполняли</color> это задание!",
                ["QUEST_completed_took"] = "Вы <color=#4286f4>успешно</color> взяли задание {0}",
                ["QUEST_tasks_completed"] = "Спасибо, держи свою <color=#4286f4>награду</color>!",
                ["QUEST_no_place"] = "Эй, погоди, ты всё <color=#4286f4>не унесёшь</color>, освободи место!",
                ["QUEST_did_not_cope"] = "Жаль что ты <color=#4286f4>не справился</color> с заданием!\n" +
                 $"В любом случае, ты можешь попробовать ещё раз!",
                ["QUEST_done"] = "Выполнено!",
                ["QUEST_take"] = "ВЗЯТЬ",
                ["QUEST_turn"] = "СДАТЬ",
                ["QUEST_REFUSE"] = "ОТКАЗАТЬСЯ",
                ["QUEST_Finished"] = "Вы закончили задание: <color=#4286f4>{0}</color>",
                ["QUEST_DONTREPEAT"] = "Вы уже выполняли этот квест!",
                ["QUEST_target"] = "Нужно: {0}\n" +
                "Повторно брать {1}\n" +
                "Кд на повторное взятие: {2}",
                ["QUEST_targetrtho"] = "Нужно: {0}\n" +
                "Прогресс: {1}/{2}\n" +
                "Повторно брать {3}\n" +
                "Кд на повторное взятие: {4}",
            }, this, "ru");
        }

        #endregion

        #region Configuration
        public static Configuration config = new Configuration();
        public class Configuration
        {
            public class itemsNpc
            {
                [JsonProperty("ShortName")]
                public string ShortName;

                [JsonProperty("SkinId")]
                public ulong SkinId;
            }
            public class Settings
            {
                [JsonProperty("Колличевство единовременно взятых квестов")]
                public int questCount;
                [JsonProperty("Голосовое оповещение при выполнении задания")]
                public bool SoundEffect;
                [JsonProperty("Эфект")]
                public string Effect;
                [JsonProperty("Названия файла с квестами")]
                public string questListDataName;
                [JsonProperty("Названия файла с Аудио для NPC(Не менять!!!)")]
                public string audioDataPath;
                [JsonProperty("Команда для открытия квест листа с прогрессом")]
                public string questListProgress;
                [JsonProperty("Идентификатор вашей постройки")]
                public string buildid;
            }
            public class SettingsNpc
            {
                [JsonProperty("Имя нпс")]
                public string Name;

                [JsonProperty("id npc (От его ид зависит его внешность)")]
                public ulong userId;

                [JsonProperty("Одежда нпс")]
                public List<itemsNpc> Wear = new List<itemsNpc>();
            }
            public class SettingsIQChat
            {
                [JsonProperty("Префикс в чате")]
                public string prifix;

                [JsonProperty("SteamID - Для аватарки из профиля стим")]
                public string SteamID;
            }

            [JsonProperty("Настройки NPC")]
            public SettingsNpc settingsNpc;
            [JsonProperty("Настройки")]
            public Settings settings;
            [JsonProperty("Настройки IQChat (Если есть)")]
            public SettingsIQChat settingsIQChat;
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    settingsIQChat = new SettingsIQChat
                    {
                        prifix = "Сидорович:",
                        SteamID = "21"
                    },
                    settings = new Settings
                    {
                        questCount = 3,
                        SoundEffect = true,
                        Effect = "assets/prefabs/locks/keypad/effects/lock.code.lock.prefab",
                        questListDataName = "Quest",
                        audioDataPath = "Audio",
                        questListProgress = "qlist",
                        buildid = "1"
                    },
                    settingsNpc = new SettingsNpc
                    {
                        Name = "Сидорович\n",
                        userId = 21,
                        Wear = new List<itemsNpc>
                        {
                            new itemsNpc
                            {
                                ShortName = "pants",
                                SkinId = 960252273,
                            },
                            new itemsNpc
                            {
                                ShortName = "hoodie",
                                SkinId = 959641236,
                            },
                            new itemsNpc
                            {
                                ShortName = "shoes.boots",
                                SkinId = 962503020,
                            }
                        }
                    },
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
                PrintWarning("Ошибка чтения конфигурации 'oxide/config/', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            if(config.settings.audioDataPath == null)
            {
                config.settings.audioDataPath = "Audio";
            }
            if (config.settings.buildid == null)
            {
                config.settings.buildid = "1";
            }
            if (!BuildingList.ContainsKey(config.settings.buildid))
            {
                PrintWarning("Вы указали неверный Идентификатор, Спавн стандартной постройки...");
                config.settings.buildid = "1";
            }
            path = "XDQuest/" + config.settings.audioDataPath;
            LoadDataSound();
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config, true);

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
                    if (player != null && player.IsConnected)
                    {
                        if (config.settings.SoundEffect)
                            Instance.RunEffect(player, config.settings.Effect);
                        Instance.SendChat(player, Instance.GetLang("QUEST_Finished", player.UserIDString, parentQuest.DisplayName)); 
                        Interface.CallHook("QuestCompleted", player, Instance.GetLang("QUEST_Finished", player.UserIDString, parentQuest.DisplayName));
                    }
                    Finished = true;
                }
                if (Instance.openQuestPlayers.Contains(UserID))
                {
                    CuiHelper.DestroyUi(player, QuestListLAYER);
                    Instance.UI_QuestList(player); 
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
            Добыть,
            Убить,
            Скрафтить,
            Изучить,
            Залутать,
            УлучшитьПостройку,
            ИспользоватьКарточкуДоступа,
            установить,
        }
        public enum PrizeType
        {
            Предмет,
            Чертёж,
            КастомПредмет,
            Команда
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
               name = "XDQuestHouseNPCNew",
               Deg2Rad = 4.72f,
               pos = new Vector3(-3.86f, 3.32f, 43.99f)
            },
            ["2"] = new Building
            {
                name = "XDQuestHouseNPCNewYear",
                Deg2Rad = 4.76f,
                pos = new Vector3(-3.86f, 3.34f, 43.99f)
            }
        };

        void GenerateBuilding()
        {
            ClearEnt();
            Subscribe("OnPasteFinished");
            var options = new List<string> { "stability", "true", "deployables", "true", "autoheight", "false", "entityowner", "false" };

            Vector3 resultVector = GetResultVector();
            var success = CopyPaste.Call("TryPasteFromVector3", resultVector, (monument.transform.rotation.eulerAngles * Mathf.Deg2Rad).y - BuildingList[config.settings.buildid].Deg2Rad, BuildingList[config.settings.buildid].name, options.ToArray());

            if (success is string)
            {
                PrintWarning("Ошибка #1 \nПлагин не будет работать, Обратитесь к разработчику" + AuthorContact);
                Unsubscribe("OnPasteFinished");
                return;
            }
            GravityItemAdd();
            timer.Once(5f, () =>
            {
                CrategravityItems();
                InitializeNPC(resultVector);
            });
        }

        public void InitializeNPC(Vector3 pos)
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
            List<BaseChair> chairs = new List<BaseChair>();
            Vis.Entities(npc.transform.position, 2f, chairs);
            foreach (var chair in chairs.Distinct().ToList())
            {
                chair.MountPlayer(npc);
                npc.OverrideViewAngles(chair.mountAnchor.transform.rotation.eulerAngles);
                npc.eyes.NetworkUpdate(chair.mountAnchor.transform.rotation);
                npc.ClientRPCPlayer(null, npc, "ForcePositionTo", npc.transform.position);
                chair.SetFlag(BaseEntity.Flags.Busy, true);
                break;
            }
            ZoneTrigger zone = new GameObject().AddComponent<ZoneTrigger>();
            zone.Activate(pos, 4.6f);
            #region Одеваем нпс
            if (config.settingsNpc.Wear.Count > 0)
                for (int i = 0; i < config.settingsNpc.Wear.Count; i++)
                    ItemManager.Create(ItemManager.FindItemDefinition(config.settingsNpc.Wear[i].ShortName), 1, config.settingsNpc.Wear[i].SkinId).MoveToContainer(npc.inventory.containerWear);  
            #endregion
        }

        private void ClearEnt()
        {
            BasePlayer findplayer = FindMyBot(config.settingsNpc.userId);

            if (findplayer != null)
                findplayer.KillMessage();

            List<BaseEntity> obj = new List<BaseEntity>();
            Vis.Entities(GetResultVector(), 10f, obj, LayerMask.GetMask("Construction", "Deployable", "Deployed", "Debris"));

            foreach (BaseEntity item in obj?.Where(x => x.OwnerID == 1893562145))
            {
                if (item == null) continue;
                item.Kill();
            }
        }

        void OnPasteFinished(List<BaseEntity> pastedEntities)
        {
            try
            {
                HouseNPC = pastedEntities;
                foreach (BaseEntity item in HouseNPC)
                {
                    item.OwnerID = 1893562145;
                    if (item as BuildingBlock)
                    {
                        var build = item as BuildingBlock;
                        build.SetFlag(BaseEntity.Flags.Reserved1, false);
                        build.SetFlag(BaseEntity.Flags.Reserved2, false);
                    }
                    if (item is BaseChair)
                        continue;
                    if (item.name.Contains("woodenbox"))
                    {
                        var box = item as BaseCombatEntity;
                        box.pickup.enabled = false;
                        continue;
                    }
                    else if (item.name.Contains("light") || item.name.Contains("lantern"))
                    {
                        item.enableSaving = true;
                        item.SendNetworkUpdate();
                        item.SetFlag(BaseEntity.Flags.Reserved8, true);
                        item.SetFlag(BaseEntity.Flags.On, true);
                    }
                    item.SetFlag(BaseEntity.Flags.Busy, true);
                    item.SetFlag(BaseEntity.Flags.Locked, true);    
                }
                PrintWarning($"Постройка обработана успешно {HouseNPC.Count}");
                Unsubscribe("OnPasteFinished");
            }
            catch  (Exception ex)
            {
               PrintError("Ошибка при загрузке постройки! Подробности в лог файле!!\nОбратитесь к разработчику" + AuthorContact); Log(ex.Message, "LogError");
            }       
        }

        #endregion

        #region Hooks
        #region QuestHook
        #region Type Upgrade
        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.УлучшитьПостройку && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return null;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if ((int)grade == Convert.ToInt16(playerQuests[i].parentQuest.Target))
                {
                    playerQuests[i].AddCount();
                }
            }
            return null;
        }
        #endregion
        #region IQPlagueSkill
        void StudySkill(BasePlayer player, string name)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.IQPlagueSkill && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if (playerQuests[i].parentQuest.Target == name || playerQuests[i].parentQuest.Target == "0")
                {
                    playerQuests[i].AddCount();
                }
            }
        }
        #endregion
        #region HeadReward
        void KillHead(BasePlayer player)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.IQHeadReward && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                playerQuests[i].AddCount();
            }
        }

        #endregion
        #region IqCase
        void OpenCase(BasePlayer player, string name)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.IQCases && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if (playerQuests[i].parentQuest.Target == name || playerQuests[i].parentQuest.Target == "0")
                {
                    playerQuests[i].AddCount();
                }
            }
        }
        #endregion
        #region OreBonus
        void RadOreGive(BasePlayer player, Item item)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.OreBonus && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if (playerQuests[i].parentQuest.Target == item.info.shortname || playerQuests[i].parentQuest.Target == "0")
                {
                    playerQuests[i].AddCount(item.amount);
                }
            }
        }
        #endregion
        #region Chinook
        void LootHack(BasePlayer player)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.XDChinookIvent && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                playerQuests[i].AddCount();
            }
        }
        #endregion
        #region Добыть
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            NextTick(() =>
            {
                BasePlayer player;
                if (entity is BasePlayer)
                {
                    player = entity as BasePlayer;
                    List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.Добыть && x.Finished == false).ToList();
                    if (playerQuests == null || playerQuests.Count == 0) return;
                    for (int i = 0; i < playerQuests.Count; i++)
                    {
                        if (item.info.shortname.Contains(playerQuests[i].parentQuest.Target))
                        {
                            playerQuests[i].AddCount(item.amount);
                        }
                    }
                }
            });
        }
        void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.Добыть && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if (item.info.shortname.Contains(playerQuests[i].parentQuest.Target))
                {
                    playerQuests[i].AddCount(item.amount);
                }
            }
        }
        #endregion
        #region Скрафтить
        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            var player = task.owner;
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.Скрафтить && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if (task.blueprint.targetItem.shortname.Contains(playerQuests[i].parentQuest.Target))
                {
                    playerQuests[i].AddCount(item.amount);
                }
            }
        }
        #endregion
        #region Изучить
        void OnItemResearch(ResearchTable table, Item targetItem, BasePlayer player)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.Изучить && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if (targetItem.info.shortname.Contains(playerQuests[i].parentQuest.Target))
                {
                    playerQuests[i].AddCount();
                }
            }
        }
        #endregion
        #region установить

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan?.GetOwnerPlayer();
            if (player == null || go == null || plan.GetItem() == null) return;
            BaseEntity ent = go.ToBaseEntity();
            if (ent == null) return;
            if (ent.skinID == 11543256361) return;
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.установить && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if (plan.GetItem().info.shortname.Contains(playerQuests[i].parentQuest.Target))
                {
                    playerQuests[i].AddCount();
                }
            }
        }
        object CanPickupEntity(BasePlayer player, BaseCombatEntity entity)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.установить && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return null;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if (entity.pickup.itemTarget.shortname.Contains(playerQuests[i].parentQuest.Target))
                {
                    entity.skinID = 11543256361;      
                }
            }
            return null;
        }
        #endregion
        #region Залутать
        private void OnLootEntity(BasePlayer player, LootContainer entity)
        {
            if (entity.OwnerID == 133722822222222 || entity.OwnerID >= 7656000000 || entity == null)
                return;
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.Залутать && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                for (int u = 0; u < entity.inventory.itemList.Count(); u++)
                {
                    if (entity.inventory.itemList[u].info.shortname.Contains(playerQuests[i].parentQuest.Target))
                        playerQuests[i].AddCount(entity.inventory.itemList[u].amount);
                }
            }
            entity.OwnerID = 133722822222222;
        }

        private void OnContainerDropItems(ItemContainer container)
        {
            if (container == null) return;
            BaseEntity entity = container.entityOwner;
            if (entity == null) return;
            if (!entity.ShortPrefabName.Contains("barrel")) return;
            foreach (Item lootitem in container.itemList)
                lootitem.SetFlag(global::Item.Flag.Placeholder, true);
        }

        object OnItemPickup(Item item, BasePlayer player)
        {
            if (item == null) return null;
            if (!item.HasFlag(global::Item.Flag.Placeholder)) return null;
            item.SetFlag(global::Item.Flag.Placeholder, false);

            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.Залутать && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return null;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                  if (item.info.shortname.Contains(playerQuests[i].parentQuest.Target))
                        playerQuests[i].AddCount(item.amount);
            }
            return null;
        }

        #endregion
        #region Использовать карточку доступа
        private void OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            List<PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.ИспользоватьКарточкуДоступа && x.Finished == false).ToList();
            if (playerQuests == null || playerQuests.Count == 0) return;
            for (int i = 0; i < playerQuests.Count; i++)
            {
                if (card.GetItem().info.shortname.Contains(playerQuests[i].parentQuest.Target))
                {
                    playerQuests[i].AddCount();
                }
            }
        }
        #endregion
        #region  Убить/взорвать/уничтожить что либо
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null || info == null)
                    return;
                List<PlayerQuest> playerQuests = null;

                string entname = entity?.ShortPrefabName;
                if (entname == "testridablehorse")
                {
                    entname = "horse";
                }
                if (entname.Contains("servergibs"))
                    return;
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

                if (player != null)
                {
                    if (entity.ToPlayer() != null && entity.ToPlayer() == player)
                        return;
                    if (entity.ToPlayer() != null)
                    {
                        if (IsFriends(player.userID, entity.ToPlayer().userID))
                            return;
                        if (IsClans(player.userID, entity.ToPlayer().userID))
                            return;
                        if (IsDuel(player.userID))
                            return;
                    }
                      
                    playerQuests = storedData.players[player.userID].PlayerQuestsAll.Values.Where(x => x.parentQuest.QuestType == QuestType.Убить && x.Finished == false).ToList();
                    if (playerQuests == null || playerQuests.Count == 0)
                        return;
                    for (int i = 0; i < playerQuests.Count; i++)
                    {
                        if (entity.PrefabName.Contains(playerQuests[i].parentQuest.Target))
                        {
                            playerQuests[i].AddCount();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            try
            {
                if (info.damageTypes.Has(Rust.DamageType.Decay))
                {
                    if (victim?.OwnerID == 1893562145)
                    {
                        info.damageTypes.Scale(DamageType.Decay, 0);
                    }
                }
            }
            catch (NullReferenceException) { }
        }

        #endregion
        #endregion

        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container.skinID == 1195832261)
            {
                UI_DrawInterface(player);
                return false;
            }
            return null;
        }
        object CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (block.OwnerID == 1893562145) return false;
            else return null;
        }
        void Init()
        {
            LoadDataPlayer();
            LoadDataQuestList(ref QuestList);
            Unsubscribe("OnPasteFinished");
        }

        private void OnServerInitialized()
        {
            
            monument = TerrainMeta.Path.Monuments.FirstOrDefault(p => p.name.ToLower() == "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab");
            Instance = this;
            if (!CopyPaste)
            {
                PrintError("Проверьте установлен ли у вас плагин 'CopyPaste'");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            else if (CopyPaste.Version < new VersionNumber(4,1,26))
            {
                PrintError("У вас старая версия CopyPaste!\nПожалуйста обновите плагин до последней версии (4.1.26 или выше) - https://umod.org/plugins/copy-paste");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            if (monument == null)
            {
                PrintError("Походу у вас отсутствует 'Город НПС' !\nПожалуйста обратитесь к разработчику" + AuthorContact);
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }  

            cmd.AddChatCommand(config.settings.questListProgress, this, nameof(UI_QuestList));
            ServerMgr.Instance.StartCoroutine(DownloadImages());
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
            LoadDataCopyPaste();


        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (!storedData.players.ContainsKey(player.userID))
            {
                storedData.players.Add(player.userID, new PlayerData());
            }
            else
            {
                foreach (var item in storedData.players[player.userID].PlayerQuestsAll)
                {
                    var curentQuest = QuestList.FirstOrDefault(p => p.Value.DisplayName == item.Value.parentQuest.DisplayName);
                    if(curentQuest.Value == null)
                    {
                        NextTick(() => {
                            storedData.players[player.userID].PlayerQuestsAll.Remove(item.Key);
                        });
                    }
                }
            }
            foreach (var item in ImageUI)
                SendImage(player, item.Key);
            PlayersTime.Add(player.userID, null);
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

        void Unload()
        {
            SaveData();
            DestroyAll<ZoneTrigger>();
            for (int i = 0; i < HouseNPC.Count; i++)
            {
                if (!HouseNPC[i].IsDestroyed)
                    HouseNPC[i]?.Kill();
            }
            npc?.KillMessage();
            foreach (var items in ItemForce)
                items?.RemoveFromWorld();
            foreach(BasePlayer p in BasePlayer.activePlayerList)
            {
                if (PlayersTime.ContainsKey(p.userID))
                {
                    if(PlayersTime[p.userID] != null)
                        ServerMgr.Instance.StopCoroutine(PlayersTime[p.userID]);
                    PlayersTime.Remove(p.userID);
                }
                CuiHelper.DestroyUi(p, Layers); CuiHelper.DestroyUi(p, QuestListLAYER);
            }
            ServerMgr.Instance.StopCoroutine(DownloadImages());

        }
        #endregion

        #region Летающии итемы

        public class GravityItem
        {
            public string Shortname;
            public Vector3 vector;
            public Quaternion quaternion;
        }
        List<GravityItem> gravityItems = new List<GravityItem>();

        private void GravityItemAdd()
        {
            gravityItems.Add(new GravityItem
            {
                Shortname = "map",
                vector = monument.transform.position + monument.transform.rotation * new Vector3(-4.96f, 3.32f, 46.66f),
                quaternion = new Quaternion(1.99f, 0.0f, 0, 2),
            });
            gravityItems.Add(new GravityItem
            {
                Shortname = "rifle.ak",
                vector = monument.transform.position + monument.transform.rotation * new Vector3(-5.56f, 2.68f, 44.95f),
                quaternion = new Quaternion(0f, -1.0f, 0, 2),
            });
            gravityItems.Add(new GravityItem
            {
                Shortname = "targeting.computer",
                vector = monument.transform.position + monument.transform.rotation * new Vector3(-5.73f, 2.68f, 46.15f),
                quaternion = new Quaternion(0, -1.39f, 0, 2),      
            });    
        }

        public void CrategravityItems()
        {
            for (int i = 0; i < gravityItems.Count; i++)
            {
                Item Item = ItemManager.CreateByName(gravityItems[i].Shortname, 1); 
                Item.Drop(gravityItems[i].vector, Vector3.up, monument.transform.rotation * gravityItems[i].quaternion);
                var Items = Item.GetWorldEntity() as DroppedItem;
                Items.allowPickup = false;
                Items.CancelInvoke(Items.IdleDestroy);
                var rigidbody = Item.GetWorldEntity().GetComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                ItemForce.Add(Item);
            }
        }
        #endregion

        #region TriggerNpc
        System.Random rnd = new System.Random();
        private class ZoneTrigger : MonoBehaviour
        {
            private float ZoneRadius;
            private Vector3 Position;

            private SphereCollider sphereCollider;
            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "ZoneTrigger";
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
                if (player != null)
                {
                    int mIndex = Instance.rnd.Next(Instance._Data.Hey.Length);
                    Instance.API_NPC_SendToAll(Instance._Data.Hey[mIndex]);
                }
            }

            private void OnTriggerExit(Collider col)
            {
                BasePlayer player = col.GetComponentInParent<BasePlayer>();
                if (player != null)
                {
                    int mIndex = Instance.rnd.Next(Instance._Data.Bye.Length);
                    Instance.API_NPC_SendToAll(Instance._Data.Bye[mIndex]);
                    player.SendConsoleCommand("Close_Layer");
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
                        sphereCollider.name = "ZoneTrigger";
                    }
                    sphereCollider.radius = ZoneRadius;
                }
            }
        }
        #endregion

        #region SoundNpc
        public class AudioData
        {
            public String Name;
            public Single Length = 0;
            public List<VoicePacket> VoicePacketList = new List<VoicePacket>();
            public class VoicePacket
            {
                public Single TimeOffset;
                public Byte[] Stream;
            }
        }

        public class BotSpeakerData
        {
            public Dictionary<string, AudioData> AudioClips = new Dictionary<string, AudioData>();
            public string[] Hey;
            public string[] Bye;

        }
        public string path = string.Empty;
        private BotSpeakerData _Data;
        private DynamicConfigFile DataFile;

        private void LoadDataSound()
        {
            if(String.IsNullOrWhiteSpace(path))
            {
                PrintError("Ошибка при загрузке звуков! Плагин будет перезагружен " + AuthorContact);
                return;
            }
            DataFile = Interface.Oxide.DataFileSystem.GetFile(path);
            if (Interface.GetMod().DataFileSystem.ReadObject<BotSpeakerData>(path).AudioClips.Count == 0)
            {
                PrintWarning("Загрузка звуков для NPC...");
                webrequest.Enqueue($"http://utilite.skyplugins.ru/xdquest/{config.settings.audioDataPath}.json", null, (i, s) => {
                    if (i == 200) WriteToData(s);
                    else { PrintError("Ошибка при загрузке звуков!\nОбратитесь к разработчику " + AuthorContact); Log(i.ToString(), "LogError"); }
                }, this, RequestMethod.GET);;
            }
            _Data = DataFile.ReadObject<BotSpeakerData>();
        }

        void WriteToData(string calback)
        {
            _Data = JsonConvert.DeserializeObject<BotSpeakerData>(calback);
            DataFile.WriteObject(_Data);
        }   
        public HashSet<uint> BotAlerts = new HashSet<uint>();
        void API_NPC_SendToAll(string clipName)
        {
            try
            {
                if (BotAlerts.Contains(npc.net.ID)) return;
                else BotAlerts.Add(npc.net.ID);

                AudioData audio;
                if (_Data.AudioClips.TryGetValue(clipName, out audio))
                {
                    var clip = _Data.AudioClips[clipName];
                    timer.Once(clip.Length, () => BotAlerts.Remove(npc.net.ID));

                    audio.VoicePacketList.ForEach(packet =>
                    {
                        timer.Once(packet.TimeOffset, () =>
                        {
                            if (Net.sv.write.Start())
                            {
                                Net.sv.write.PacketID(Message.Type.VoiceData);
                                Net.sv.write.UInt32(npc.net.ID);
                                Net.sv.write.BytesWithSize(packet.Stream);
                                var write = Net.sv.write;
                                var sendInfo = new SendInfo()
                                {
                                    connections = BasePlayer.activePlayerList.Select(player => player.Connection).ToList(),
                                    priority = Priority.Immediate
                                };
                                write.Send(sendInfo);
                            }
                        });
                    });
                }
            }
            catch(NullReferenceException ex)
            {
                PrintError($"Ошибка загрузки звуков! Попробуйте перезагрузить плагин удалив в дате файли {config.settings.audioDataPath}\nИли свяжитесь с разработчиком + {AuthorContact}");
            }  
        }

        #endregion

        #region HelpMetods
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
                    case "en":
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

        private static DateTime Epoch = new DateTime(1970, 1, 1);

        public static double GetTimeStamp()
        {
            return DateTime.Now.Subtract(Epoch).TotalSeconds;
        }
        private IEnumerator DownloadImages()
        {
            foreach (var item in ImageUI)
                AddImage(item.Value, item.Key);

            PrintError("AddImages...");
            foreach (var img in QuestList)
            {
                for (int i = 0; i < img.Value.PrizeList.Count; i++)
                {
                    var typeimg = img.Value.PrizeList[i];
                    if (typeimg.type == PrizeType.КастомПредмет)
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", typeimg.ShortName, typeimg.SkinID))
                            ImageLibrary.Call("AddImage", $"http://rust.skyplugins.ru/getskin/{typeimg.SkinID}/", typeimg.ShortName, typeimg.SkinID);
                    }
                    else if (typeimg.type == PrizeType.Команда)
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", typeimg.Url))
                            ImageLibrary.Call("AddImage", typeimg.Url, typeimg.Url);
                    }
                    else
                    {
                        if (!(bool)ImageLibrary?.Call("HasImage", typeimg.ShortName + 128))
                            ImageLibrary.Call("AddImage", $"http://rust.skyplugins.ru/getimage/{typeimg.ShortName}/128", typeimg.ShortName + 128);
                    }
                    yield return new WaitForSeconds(0.05f);
                }
            }
            PrintError("All Image load!");
            yield return 0;
        }
        public void LoadDataCopyPaste()
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("copypaste/" + BuildingList[config.settings.buildid].name))
            {
                PrintError($"Файл постройки не найден!\nНачинаем импортировать...");
                webrequest.Enqueue($"http://utilite.skyplugins.ru/xdquest/{BuildingList[config.settings.buildid].name}.json", null, (i, s) =>
                {
                    if (i == 200)
                    {
                        PasteData obj = JsonConvert.DeserializeObject<PasteData>(s);
                        Interface.Oxide.DataFileSystem.WriteObject("copypaste/" + BuildingList[config.settings.buildid].name, obj);
                    }
                    else
                    {
                        PrintError("Ошибка при загрузке постройки!\nПробуем загрузить еще раз"); Log(i.ToString(), "LogError");
                        timer.Once(10f, () => LoadDataCopyPaste());
                        return;
                    }
                }, this, RequestMethod.GET);
            }
            timer.Once(5f, () =>
            {
                GenerateBuilding();
            });
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

        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            sb.Clear();
            return sb.AppendFormat("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a).ToString();
        }
        public class PasteData
        {
            public Dictionary<string, object> @default;
            public ICollection<Dictionary<string, object>> entities;
            public Dictionary<string, object> protocol;
        }
        private Vector3 GetResultVector()
        {
            return monument.transform.position + monument.transform.rotation * BuildingList[config.settings.buildid].pos;
        }
        private BasePlayer FindMyBot(ulong userid)
        {
            return UnityEngine.Object.FindObjectsOfType<BasePlayer>().FirstOrDefault(x => x.userID == userid);
        }
        private void DestroyAll<T>()
        {
            var objects = UnityEngine.Object.FindObjectsOfType(typeof(T));
            objects?.ToList().ForEach(UnityEngine.Object.Destroy);
        }
        void Log(string msg, string file)
        {
            LogToFile(file, $"[{DateTime.Now}] {msg}", this);
        }
        #endregion

        #region Interface

        private const string Layers = "UI_Layer";
        private const string QuestListPanel = "QuestListPanel";
        private const string QuestListLAYER = "QuestListLAYER";
        private const string QuestNotticePlayer = "QuestNotticePlayer";
        HashSet<ulong> openQuestPlayers = new HashSet<ulong>();

        [ConsoleCommand("Close_UI")]
        void CloseUiPlayer(ConsoleSystem.Arg arg)
        {
            if (openQuestPlayers.Contains(arg.Player().userID))
                openQuestPlayers.Remove(arg.Player().userID);
            CuiHelper.DestroyUi(arg.Player(), QuestListLAYER);
            arg.Player().SetFlag(BaseEntity.Flags.Reserved3, false);
            if(PlayersTime[arg.Player().userID] != null)
                ServerMgr.Instance.StopCoroutine(PlayersTime[arg.Player().userID]);
        }
        [ConsoleCommand("Close_Layer")]
        void CloseLayerPlayer(ConsoleSystem.Arg arg)
        {
            if (openQuestPlayers.Contains(arg.Player().userID))
                openQuestPlayers.Remove(arg.Player().userID);
            CuiHelper.DestroyUi(arg.Player(), Layers);
            arg.Player().SetFlag(BaseEntity.Flags.Reserved3, false);
            if (PlayersTime[arg.Player().userID] != null)
                ServerMgr.Instance.StopCoroutine(PlayersTime[arg.Player().userID]);
        }
        void UI_QuestList(BasePlayer player)
        {
            Dictionary<int, PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll;
            if (playerQuests == null) return;
            if (!openQuestPlayers.Contains(player.userID))
                openQuestPlayers.Add(player.userID);

            CuiHelper.DestroyUi(player, QuestListLAYER);
            int ds = -81 + (-75 * playerQuests.Count);
            var txtname = playerQuests.Count > 0 ? GetLang("QUEST_ACTIVE", player.UserIDString, playerQuests.Count) :
                                                   GetLang("NOT_QUEST_ACTIVE", player.UserIDString);
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"0 {ds}", OffsetMax = "200 -50" },
                Image = { Color = HexToRustFormat("#24241EDF"), Material = "assets/content/ui/uibackgroundblur.mat" },
            }, "Overlay", QuestListLAYER);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -30", OffsetMax = "180 0" },
                Text = { Text = txtname, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 15, Color = "1.00 1.00 0.87 1.00" }
            }, QuestListLAYER);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-20 -20", OffsetMax = "0 0" },
                Button = { Color = "0.79 0.24 0.24 0.90", Command = "Close_UI" },
                Text = { Text = "<b>x</b>", FontSize = 16, Align = TextAnchor.MiddleCenter }
            }, QuestListLAYER);

            int size = 75, i = 0;

            foreach (var item in playerQuests)
            {
                var color = item.Value.Finished == true ? "0.07 0.81 0.36 0.52" : HexToRustFormat("#774033FF");
                var txt = item.Value.Finished == true ? GetLang("QUEST_ACTIVE_COMPLITE", player.UserIDString, item.Value.parentQuest.DisplayName) :
                                                        GetLang("NOT_QUEST_ACTIVE_COMPLITE", player.UserIDString, item.Value.parentQuest.DisplayName, item.Value.Count, item.Value.parentQuest.Amount, item.Value.parentQuest.Missions);
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    RectTransform = {  AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMin = $"1 {-100 - i*size}",
                        OffsetMax = $"199 {-30 - i*size}"},
                    Image = { Color = color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                }, QuestListLAYER, QuestListLAYER + i);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = txt, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 12, Color = "1.00 1.00 1.00 1.00" }
                }, QuestListLAYER + i);
                i++;
            }
            CuiHelper.AddUi(player, container);
        }

        private void HelpUiNottice(BasePlayer player, string msg, string sprite = "assets/icons/warning.png", string color = "#C25619FF")
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, QuestNotticePlayer);
            container.Add(new CuiPanel
            {
                FadeOut = 0.30f,
                RectTransform = { AnchorMin = "0.5046874 0.8685184", AnchorMax = "0.8749999 0.9611109", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0", FadeIn = 0.40f }
            }, Layers, QuestNotticePlayer);

            container.Add(new CuiElement
            {
                Parent = QuestNotticePlayer,
                FadeOut = 0.30f,
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage("QUESTFON"), Color = HexToRustFormat(color), FadeIn = 0.40f  },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"},
                }
            });

            container.Add(new CuiElement
            {
                Parent = QuestNotticePlayer,
                FadeOut = 0.30f,
                Components =
                {
                    new CuiImageComponent {Sprite = sprite, Color = HexToRustFormat("#FFFFFFFF"), FadeIn = 0.40f  },
                    new CuiRectTransformComponent{ AnchorMin = "0.02672293 0.25", AnchorMax = "0.09704643 0.7499998"},
                }
            });

            container.Add(new CuiLabel
            {
                FadeOut = 0.30f,
                RectTransform = { AnchorMin = "0.1139241 0.08999151", AnchorMax = "0.9423349 0.8999914", OffsetMax = "0 0" },
                Text = { Text = msg, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = HexToRustFormat("#FFFFFFFF"), FadeIn = 0.40f }
            }, QuestNotticePlayer);

            CuiHelper.AddUi(player, container);
            timer.Once(4.5f, () => { CuiHelper.DestroyUi(player, QuestNotticePlayer); });
        }

        private void UI_DrawInterface(BasePlayer player, bool upd = false, int page = 0)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layers);
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", Layers);

            container.Add(new CuiElement
            {
                Parent = Layers,
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage("MAINFON"), Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"},
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layers,
                Name = "closeui",
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage("CloseUI"), Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent{ AnchorMin = "0.8874999 0.8759267", AnchorMax = "0.940625 0.9527785"},
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Command = "Close_Layer" },
                Text = { Text = "" }
            }, "closeui");
            CuiHelper.AddUi(player, container);
            QuestLists(player, 0, false, false);
        }

        public void QuestLists(BasePlayer player, int page = 0, bool upd = false, bool active = false)
        {
            CuiHelper.DestroyUi(player, QuestListPanel);
            CuiHelper.DestroyUi(player, "UPBTN");
            CuiHelper.DestroyUi(player, "DOWNBTN");
            CuiHelper.DestroyUi(player, "allquest");
            CuiHelper.DestroyUi(player, "activequest");
            CuiElementContainer container = new CuiElementContainer();
            int y = 0, i = 7 * page, indexQuest = 0;

            Dictionary<int, PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll;
            if (playerQuests == null) return;

            if (page != 0 && !active)
            {
                container.Add(new CuiElement
                {
                    Parent = Layers,
                    Name = "UPBTN",
                    Components =
                {
                    new CuiRawImageComponent { Png = GetImage("UPBTN"), Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent{ AnchorMin = "0.01302087 0.7935191", AnchorMax = "0.05677087 0.8870376"},
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_Handler page {page - 1}" },
                    Text = { Text = "" }
                }, "UPBTN");
            }
            if (page + 1 < (int)Math.Ceiling(((double)QuestList.Count - playerQuests.Count) / 7) && !active)
            {
                container.Add(new CuiElement
                {
                    Parent = Layers,
                    Name = "DOWNBTN",
                    Components =
                {
                    new CuiRawImageComponent { Png = GetImage("DOWNBTN"), Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent{ AnchorMin = "0.01302087 0.1444528", AnchorMax = "0.05677087 0.2379713"},
                }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = $"UI_Handler page {page + 1}" },
                    Text = { Text = "" }
                }, "DOWNBTN");
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.06354167 0.9222221", AnchorMax = "0.2505214 0.9842592" },
                Button = { Color = "0 0 0 0", Command = $"UI_Handler allquest" },
                Text = { Text = GetLang("AVAILABLE_MISSIONS", player.UserIDString, QuestList.Count - playerQuests.Count), FontSize = 18, Align = TextAnchor.MiddleCenter }
            }, Layers, "allquest");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.275522 0.9222221", AnchorMax = "0.4624973 0.9842592" },
                Button = { Color = "0 0 0 0", Command = $"UI_Handler activequest" },
                Text = { Text = GetLang("ACTIVE_MISSIONS", player.UserIDString, playerQuests.Count), FontSize = 18, Align = TextAnchor.MiddleCenter }
            }, Layers, "activequest");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.07291577 0.1490741", AnchorMax = "0.382291 0.875" },
                Image = { Color = "0 0 0 0" }
            }, Layers, QuestListPanel);

            if (!active)
            {
                foreach (var quest in QuestList.Where(x => !playerQuests.ContainsKey(x.Key)).Skip(page * 7))
                {
                    if(y == 0)
                      indexQuest = quest.Key;

                    container.Add(new CuiElement
                    {
                        Parent = QuestListPanel,
                        Name = $"quest_{i}",
                        Components =
                    {
                    new CuiRawImageComponent { Png = GetImage("QUESTFON"), Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent{ AnchorMin = $"0 {0.8928571 - (y * 0.15)}", AnchorMax = $"1 {0.9987245 - (y * 0.15)}"},
                    }
                    });
                    string text = string.Empty;
                    if (quest.Value.DisplayName.Length >= 49) text = quest.Value.DisplayName.Substring(0, 49) + "...";
                    else text = quest.Value.DisplayName;

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.07407689 0.1445791", AnchorMax = "0.9696992 0.8674704", OffsetMax = "0 0" },
                        Text = { Text = $"<b>{text}</b>", Align = TextAnchor.MiddleLeft, FontSize = 16, Color = HexToRustFormat("#FFFFFFFF") }
                    }, $"quest_{i}");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = $"UI_Handler questinfo {quest.Key}" },
                        Text = { Text = "" }
                    }, $"quest_{i}");
                    if (y >= 6) break;
                    y++; i++;
                }
                if (indexQuest == 0)
                {
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { Text = GetLang("NOT_AVAILABLE_MISSIONS", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 19, Color = HexToRustFormat("#FFFFFFFF") }
                    }, QuestListPanel);
                }
            }
            else
            {
                if (playerQuests.Count == 0)
                {
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { Text = GetLang("NOT_AVAILABLE_MISSIONS", player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 19, Color = HexToRustFormat("#FFFFFFFF") }
                    }, QuestListPanel);
                }
                foreach (var quest in playerQuests.Skip(page * 7).Take(7))
                {
                    if (y == 0)
                        indexQuest = quest.Key;
                    container.Add(new CuiElement
                    {
                        Parent = QuestListPanel,
                        Name = $"quest_{i}",
                        Components =
                    {
                    new CuiRawImageComponent { Png = GetImage("QUESTFON"), Color = HexToRustFormat("#44C218FF") },
                    new CuiRectTransformComponent{ AnchorMin = $"0 {0.8928571 - (y * 0.15)}", AnchorMax = $"1 {0.9987245 - (y * 0.15)}"},
                    }
                    });

                    string text = string.Empty;
                    if (quest.Value.parentQuest.DisplayName.Length >= 43) text = quest.Value.parentQuest.DisplayName.Substring(0, 43) + "...";
                    else text = quest.Value.parentQuest.DisplayName;
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.07407689 0.1445791", AnchorMax = "0.9696992 0.8674704", OffsetMax = "0 0" },
                        Text = { Text = $"<b>{text}</b>", Align = TextAnchor.MiddleLeft, FontSize = 16, Color = HexToRustFormat("#FFFFFFFF") }
                    }, $"quest_{i}");

                    CuiImageComponent component = quest.Value.Finished ? new CuiImageComponent { Sprite = "assets/icons/check.png", Color = HexToRustFormat("#FFFFFFFF") } : new CuiImageComponent { Png = GetImage("InProcces"), Color = HexToRustFormat("#FFFFFFFF") };
                    container.Add(new CuiElement
                    {
                        Parent = $"quest_{i}",
                        Components =
                    {
                    component,
                    new CuiRectTransformComponent{ AnchorMin = $"0.8737399 0.1204819", AnchorMax = $"0.9747499 0.8433737"},
                    }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = $"UI_Handler questinfo {quest.Key}" },
                        Text = { Text = "" }
                    }, $"quest_{i}");
                    y++; i++;
                }
            }
            if (indexQuest != 0)
                OpenQuestInfo(player, indexQuest);
            CuiHelper.AddUi(player, container);
        }

        private void OpenQuestInfo(BasePlayer player, int quest)
        {
            player.SetFlag(BaseEntity.Flags.Reserved3, false);

            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "QuestInfo");
            Dictionary<int, PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll;
            if (playerQuests == null) return;
            var quests = QuestList[quest];
            var curentQuest = playerQuests.FirstOrDefault(p => p.Value.parentQuest.DisplayName == quests.DisplayName);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.4458334 0.1898148", AnchorMax = "0.9369792 0.8074074", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, Layers, "QuestInfo");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01378565 0.4907678", AnchorMax = "1.003181 0.6868463", OffsetMax = "0 0" },
                Text = { Text = $"<b>{quests.DisplayName}</b>", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-bold.ttf", FontSize = 25, Color = HexToRustFormat("#FFFFFFFF") }
            }, "QuestInfo");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01484602 0.3118441", AnchorMax = "0.988335 0.4821225", OffsetMax = "0 0" },
                Text = { Text = quests.Description, Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = HexToRustFormat("#FFFFFFFF") }
            }, "QuestInfo");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.04984076 0.23240", AnchorMax = "0.5365851 0.3039169", OffsetMax = "0 0" },/////////////////////////////////////////////
                Text = { Text = GetLang("REWARD_FOR_QUESTIONS", player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 14, Color = HexToRustFormat("#FFFFFFFF") }
            }, "QuestInfo", "questinfo1");
            
            string userepeat = quests.UseRepeat ? GetLang("CAN", player.UserIDString) : GetLang("CAN'T", player.UserIDString);
            string useCooldown = quests.Cooldown > 0 ? TimeHelper.FormatTime(TimeSpan.FromSeconds(quests.Cooldown)) : GetLang("absent's", player.UserIDString);
            string msg = curentQuest.Value == null ? GetLang("QUEST_target", player.UserIDString, quests.Missions, userepeat, useCooldown) :
                                                     GetLang("QUEST_targetrtho", player.UserIDString, quests.Missions, curentQuest.Value.Count, quests.Amount, userepeat, useCooldown);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.628844 0.1139431", AnchorMax = "0.911983 0.3163418", OffsetMax = "0 0" },
                Text = { Text = msg, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-bold.ttf", FontSize = 12, Color = HexToRustFormat("#FFFFFFFF") }
            }, "QuestInfo");

            string command = "";
            string color = "";
            string text = "";
            if (curentQuest.Value == null)
            {
                if (!quests.UseRepeat && storedData.players[player.userID].PlayerQuestsFinish.Contains(quests.DisplayName))
                {
                    text = lang.GetMessage("QUEST_done", this, player.UserIDString);
                    color = "0.38 0.66 0.65 1.00";
                    command = $"UI_Handler get {quest}";
                }
                else
                {
                    text = lang.GetMessage("QUEST_take", this, player.UserIDString);
                    color = "0.43 0.52 0.29 1.00";
                    command = $"UI_Handler get {quest}";
                }
            }
            else if (curentQuest.Value.Finished)
            {
                text = lang.GetMessage("QUEST_turn", this, player.UserIDString);
                color = "0.29 0.40 0.52 1.00";
                command = $"UI_Handler finish {quest}";
            }
            else
            {
                text = lang.GetMessage("QUEST_REFUSE", this, player.UserIDString);
                color = "0.52 0.29 0.29 1.00";
                command = $"UI_Handler finish {quest}";
            }
            if (storedData.players[player.userID].PlayerQuestsCooldown != null)
            {
                if (storedData.players[player.userID].PlayerQuestsCooldown.ContainsKey(quests.DisplayName))
                {
                    if (storedData.players[player.userID].PlayerQuestsCooldown[quests.DisplayName] >= GetTimeStamp())
                    {
                        text = lang.GetMessage(TimeHelper.FormatTime(TimeSpan.FromSeconds(storedData.players[player.userID].PlayerQuestsCooldown[quests.DisplayName] - GetTimeStamp())), this, player.UserIDString);
                        color = "0.73 0.09 0.20 1.00";
                        command = $"UI_Handler coldown";
                        player.SetFlag(BaseEntity.Flags.Reserved3, true);
                    }
                }
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.6277843 0.036911586", AnchorMax = "0.9162256 0.1072717" },
                Button = { Color = color, Command = command },
                Text = { Text = text, Align = TextAnchor.MiddleCenter, FontSize = 18 }
            }, "QuestInfo", Layers + ".info");


            for (int i = 0; i < quests.PrizeList.Count; i++)
            {
                var prize = quests.PrizeList[i];

                string prizeLayer = "QuestInfo" + $".{i}";
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0.03287371 + i * 0.10f} 0.08788858", AnchorMax = $"{0.1283138 + i * 0.10f} 0.2228211", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat("#73737339") },
                    Text = { Text = "" }
                }, "QuestInfo", prizeLayer);

                var img = prize.type == PrizeType.КастомПредмет ? GetImage(prize.ShortName, prize.SkinID) : prize.type == PrizeType.Предмет ? GetImage(prize.ShortName + 128) : prize.type == PrizeType.Команда ? GetImage(prize.Url) : "";
                if (img != "")
                {
                    container.Add(new CuiElement
                    {
                        Parent = prizeLayer,
                        Components =
                                {
                                    new CuiRawImageComponent { Png = img },
                                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                                }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Parent = prizeLayer,
                        Components =
                                {
                                    new CuiRawImageComponent { Png = GetImage("BluePrint") },
                                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                                }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = prizeLayer,
                        Components =
                                {
                                    new CuiRawImageComponent { Png = GetImage(prize.ShortName + 128) },
                                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                                }
                    });
                }

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-2 0", OffsetMin = "0 2" },
                    Text = { Text = $"x{prize.Amount}", Font = "droidsansmono.ttf", Align = TextAnchor.LowerRight, FontSize = 13, Color = HexToRustFormat("#FFFFFFFF") }
                }, prizeLayer);
            }
            CuiHelper.AddUi(player, container);
            if (storedData.players[player.userID].PlayerQuestsCooldown.ContainsKey(quests.DisplayName))
            {
                if(PlayersTime[player.userID] != null)
                    ServerMgr.Instance.StopCoroutine(PlayersTime[player.userID]);
                PlayersTime[player.userID] = ServerMgr.Instance.StartCoroutine(StartUpdate(player, quest));
            }       
        }

        public Dictionary<ulong, Coroutine> PlayersTime = new Dictionary<ulong, Coroutine>();
        private IEnumerator StartUpdate(BasePlayer player, int quest)
        {
            var check = QuestList[quest];

            while (player.HasFlag(BaseEntity.Flags.Reserved3))
            {
                    string questLayer = Layers + ".info";
                if (storedData.players[player.userID].PlayerQuestsCooldown.ContainsKey(check.DisplayName))
                {
                    if (storedData.players[player.userID]?.PlayerQuestsCooldown[check.DisplayName] >= GetTimeStamp())
                    {
                        CuiElementContainer container = new CuiElementContainer();
                        CuiHelper.DestroyUi(player, questLayer);

                        string text = TimeHelper.FormatTime(TimeSpan.FromSeconds(storedData.players[player.userID].PlayerQuestsCooldown[check.DisplayName] - GetTimeStamp()));

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.6277843 0.03691586", AnchorMax = "0.9162256 0.1072717" },
                            Button = { Color = "0.73 0.09 0.20 1.00", Command = "UI_Handler coldown" },
                            Text = { Text = text, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 13, Color = HexToRustFormat("#d9e1c6") }
                        }, "QuestInfo", Layers + ".info");

                        CuiHelper.AddUi(player, container);
                    }
                    else if (storedData.players[player.userID].PlayerQuestsCooldown[check.DisplayName] != 0)
                    {
                        storedData.players[player.userID].PlayerQuestsCooldown.Remove(check.DisplayName);
                        OpenQuestInfo(player, quest);
                    }
                }              
                yield return new WaitForSeconds(1);
            }
        }
        #endregion

        [ConsoleCommand("UI_Handler")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            Dictionary<int, PlayerQuest> playerQuests = storedData.players[player.userID].PlayerQuestsAll;
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
                                        HelpUiNottice(player, GetLang("QUEST_ACTIVE_LIMIT", player.UserIDString));
                                        return;
                                    }
                                    if (playerQuests.Any(p => p.Value.parentQuest.DisplayName == currentQuest.DisplayName))
                                    {
                                        HelpUiNottice(player, GetLang("QUEST_took_tasks", player.UserIDString));
                                        return;
                                    }
                                    if (!currentQuest.UseRepeat && storedData.players[player.userID].PlayerQuestsFinish.Contains(currentQuest.DisplayName))
                                    {
                                        HelpUiNottice(player, GetLang("QUEST_completed_tasks", player.UserIDString));
                                        return;
                                    }
                                    if (storedData.players[player.userID].PlayerQuestsFinish.Contains(currentQuest.DisplayName))
                                    {
                                        HelpUiNottice(player, GetLang("QUEST_DONTREPEAT", player.UserIDString));
                                        return;
                                    }
                                    playerQuests.Add(questIndex, new PlayerQuest() { UserID = player.userID, parentQuest = currentQuest });
                                    if (currentQuest.Cooldown != 0)
                                    {
                                        if (!storedData.players[player.userID].PlayerQuestsCooldown.ContainsKey(currentQuest.DisplayName))
                                        {
                                            storedData.players[player.userID].PlayerQuestsCooldown.Add(currentQuest.DisplayName, 0);
                                        }
                                    }
                                    QuestLists(player, 0, true);
                                    OpenQuestInfo(player, questIndex);
                                    HelpUiNottice(player, GetLang("QUEST_completed_took", player.UserIDString, currentQuest.DisplayName));
                                }
                            }
                            break;
                        }
                    case "page":
                        {
                            int pageIndex;
                            if (int.TryParse(args.Args[1], out pageIndex))
                            {
                                QuestLists(player, pageIndex);
                            }
                            break;
                        }
                    case "activequest":
                        {
                            QuestLists(player, 0, false, true);
                            break;
                        }
                    case "allquest":
                        {
                            QuestLists(player, 0, false, false);
                            break;
                        }
                    case "coldown":
                        {
                            HelpUiNottice(player, GetLang("QUEST_ACTIVE_COLDOWN", player.UserIDString));
                            break;
                        }
                    case "questinfo":
                        {
                            int pageIndex;
                            if (int.TryParse(args.Args[1], out pageIndex))
                            {
                                OpenQuestInfo(player, pageIndex);
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
                                    var currentQuest = playerQuests.FirstOrDefault(p => p.Value.parentQuest.DisplayName == globalQuest.DisplayName);
                                    if (currentQuest.Value == null)
                                        return;

                                    if (currentQuest.Value.Finished)
                                    {
                                        if (24 - player.inventory.containerMain.itemList.Count < currentQuest.Value.parentQuest.PrizeList.Where(x => x.type != PrizeType.Команда).Count())
                                        {
                                            HelpUiNottice(player, GetLang("QUEST_no_place", player.UserIDString));
                                            return;
                                        }

                                        if (currentQuest.Value.parentQuest.QuestType == QuestType.Добыть || currentQuest.Value.parentQuest.QuestType == QuestType.Залутать)
                                        {
                                            var idItem = ItemManager.FindItemDefinition(currentQuest.Value.parentQuest.Target);
                                            var item = player?.inventory?.GetAmount(idItem.itemid);
                                            if (item == 0 || item == null)
                                            {
                                                HelpUiNottice(player, GetLang("QUEST_Insufficient_resources", player.UserIDString, idItem.displayName.english));
                                                return;
                                            }
                                            if (item < currentQuest.Value.parentQuest.Amount)
                                            {
                                                HelpUiNottice(player, GetLang("QUEST_not_resources", player.UserIDString, idItem.displayName.english, currentQuest.Value.parentQuest.Amount));
                                                return;
                                            }
                                            if (item >= currentQuest.Value.parentQuest.Amount)
                                            {
                                                player.inventory.Take(null, idItem.itemid, currentQuest.Value.parentQuest.Amount);
                                            }

                                        }
                                        HelpUiNottice(player, GetLang("QUEST_tasks_completed", player.UserIDString));                

                                        currentQuest.Value.Finished = false;
                                        for (int i = 0; i < currentQuest.Value.parentQuest.PrizeList.Count; i++)
                                        {
                                            var check = currentQuest.Value.parentQuest.PrizeList[i];
                                            switch (check.type)
                                            {
                                                case PrizeType.Предмет:
                                                    Item newItem = ItemManager.CreateByPartialName(check.ShortName, check.Amount);
                                                    player.GiveItem(newItem, BaseEntity.GiveItemReason.Crafted);
                                                    break;
                                                case PrizeType.Команда:
                                                    Server.Command(check.Command.Replace("%STEAMID%", player.UserIDString));
                                                    break;
                                                case PrizeType.КастомПредмет:
                                                    Item customItem = ItemManager.CreateByPartialName(check.ShortName, check.Amount, check.SkinID);
                                                    customItem.name = check.Name;
                                                    player.GiveItem(customItem, BaseEntity.GiveItemReason.Crafted);
                                                    break;
                                                case PrizeType.Чертёж:
                                                    Item itemBp = ItemManager.CreateByItemID(-996920608, check.Amount);
                                                    itemBp.blueprintTarget = ItemManager.itemList.Find(x => x.shortname == check.ShortName)?.itemid ?? 0;
                                                    player.GiveItem(itemBp, BaseEntity.GiveItemReason.Crafted);
                                                    break;
                                            }
                                        }
                                        if (!currentQuest.Value.parentQuest.UseRepeat && globalQuest.Cooldown == 0)
                                        {
                                            storedData.players[player.userID].PlayerQuestsFinish.Add(currentQuest.Value.parentQuest.DisplayName);
                                        }
                                        else
                                        {
                                            storedData.players[player.userID].PlayerQuestsCooldown[globalQuest.DisplayName] = GetTimeStamp() + globalQuest.Cooldown;
                                        }
                                        playerQuests.Remove(currentQuest.Key);
                                        QuestLists(player, 0, true, true);
                                        OpenQuestInfo(player, questIndex);
                                    }
                                    else
                                    {
                                        HelpUiNottice(player, GetLang("QUEST_did_not_cope", player.UserIDString));
                                        playerQuests.Remove(currentQuest.Key);
                                        QuestLists(player, 0, true, true);
                                        OpenQuestInfo(player, questIndex);
                                    }
                                }
                                else
                                    HelpUiNottice(player, "Вы <color=#4286f4>не брали</color> этого задания!");
                            }
                            break;
                        }
                }
            }
        }

        #region Data
        class PlayerData
        {
            public List<string> PlayerQuestsFinish = new List<string>();
            public Dictionary<int, PlayerQuest> PlayerQuestsAll = new Dictionary<int, PlayerQuest>();
            public Dictionary<string, double> PlayerQuestsCooldown = new Dictionary<string, double>();
        }

        class StoredData
        {
            public Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();
        }

        void SaveData()
        {
            SaveDataQuestList(QuestList);
            if (StatData != null)
                StatData.WriteObject(storedData);
            if (DataFile != null)
                DataFile.WriteObject(_Data);
        }

        private void SaveDataQuestList<T>(T data)
        {
            string resultName = this.Name + $"/{config.settings.questListDataName}";
            Interface.Oxide.DataFileSystem.WriteObject(resultName, data);
        }

        private void LoadDataQuestList<T>(ref T data)
        {
            string resultName = this.Name + $"/{config.settings.questListDataName}";

            if (Interface.Oxide.DataFileSystem.ExistsDatafile(resultName))
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<T>(resultName);
            }
            else
            {
                Interface.Oxide.DataFileSystem.WriteObject(resultName, data);
            }
        }

        void LoadDataPlayer()
        {
            string resultName = this.Name + $"/PlayerInfo";
            StatData = Interface.Oxide.DataFileSystem.GetFile(resultName);
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(resultName);
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        StoredData storedData;
        private DynamicConfigFile StatData;
        #endregion

        #region QuestListDefault
        private Dictionary<int, Quest> QuestList = new Dictionary<int, Quest>
        {
            [1] = new Quest
            {
                DisplayName = "Генетика - мое призвание",
                Description = "Изучая генетические соединения растений,далее животных, я дошел до генетических соединений людей!\nТеперь я могу изменить свое ДНК для получения приимуществ, нужно изучить 1 любой навык!",
                Missions = "Изучить 1 любой навык",
                QuestType = QuestType.IQPlagueSkill,
                Target = "0",
                Amount = 1,
                UseRepeat = false,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "workbench1",
                         SkinID = 0,
                         ShortName = "workbench1",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Чертёж,
                         Name = "smg.thompson",
                         SkinID = 0,
                         ShortName = "smg.thompson",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "ammothompson",
                         SkinID = 0,
                         ShortName = "ammo.pistol.fire",
                         Amount = 128,
                         Url = "",
                         Command = "",
                    },

                }
            },
            [2] = new Quest
            {
                DisplayName = "Дикий запад",
                Description = "Сейчас твоя голова может стоит денег, поэтому стоит подумать 10 раз прежде чем убивать кепку,\nведь кто знает,что после убийства этой кепки на тебя ополчится весь сервер,чтобы убить тебя и забрать награду!\nЭтим мы и займемся, получим награду за голову , найди и убей игрока в розыске!",
                Missions = "Убить человека с наградой за голову",
                QuestType = QuestType.IQHeadReward,
                Target = "0",
                Amount = 1,
                UseRepeat = true,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "sulfur",
                         SkinID = 0,
                         ShortName = "sulfur",
                         Amount = 500,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "pookie.bear",
                         SkinID = 0,
                         ShortName = "pookie.bear",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "scrap",
                         SkinID = 0,
                         ShortName = "scrap",
                         Amount = 100,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "pistol.revolver",
                         SkinID = 0,
                         ShortName = "pistol.revolver",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "ammo.pistol",
                         SkinID = 0,
                         ShortName = "ammo.pistol",
                         Amount = 64,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [3] = new Quest
            {
                DisplayName = "Удача на моей стороне",
                Description = "Открой любой кейс,думаю нам повезет и мы потешимся получив отличный лут!",
                Missions = "Открыть 1 любой кейс",
                QuestType = QuestType.IQCases,
                Target = "0",
                Amount = 1,
                UseRepeat = false,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Команда,
                         Name = "Халява",
                         SkinID = 0,
                         ShortName = "",
                         Amount = 500,
                         Url = "https://i.imgur.com/N93o4D2.png",
                         Command = "iqcase give %STEAMID% freecase 1",
                    },
                }
            },
            [4] = new Quest
            {
                DisplayName = "Залежи руды",
                Description = "Давай добудем необычную руду,которая редко,но попадается во время добычи привычной тебе руды!",
                Missions = "Добыть 15 любой радиоактивной руды",
                QuestType = QuestType.OreBonus,
                Target = "0",
                Amount = 15,
                UseRepeat = true,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "icepick.salvaged",
                         SkinID = 0,
                         ShortName = "icepick.salvaged",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "cakefiveyear",
                         SkinID = 0,
                         ShortName = "cakefiveyear",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "bed",
                         SkinID = 0,
                         ShortName = "bed",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [5] = new Quest
            {
                DisplayName = "Кто успел - тот и съел",
                Description = "Будь быстрым и ловким, по другому в этом мире не выжить!\nЗалутай первее особый груз и сохрани его,чтобы получить награду!",
                Missions = "Залутать особый груз 1 раз",
                QuestType = QuestType.XDChinookIvent,
                Target = "0",
                Amount = 1,
                UseRepeat = true,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "supply.signal",
                         SkinID = 0,
                         ShortName = "supply.signal",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "jackhammer",
                         SkinID = 0,
                         ShortName = "jackhammer",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "rf.detonator",
                         SkinID = 0,
                         ShortName = "rf.detonator",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "dropbox",
                         SkinID = 0,
                         ShortName = "dropbox",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [6] = new Quest
            {
                DisplayName = "Лес наповал!",
                Description = "Пора уже расчистить место для своей огромной базы!\nСруби несколько деревьев, сразу двух зайцев одним деревом,ха!",
                Missions = "Добыть 15000 дерева",
                QuestType = QuestType.Добыть,
                Target = "wood",
                Amount = 100,
                UseRepeat = true,
                Cooldown = 120,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "chainsaw",
                         SkinID = 0,
                         ShortName = "chainsaw",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "ladder.wooden.wall",
                         SkinID = 0,
                         ShortName = "ladder.wooden.wall",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [7] = new Quest
            {
                DisplayName = "Штурм космодрома",
                Description = "Пора заглянуть на заброшенный космодром и найти документы о всех полетах,узнать,что вообще творится на этом острове и почему каждый хочет убить друг друга!\nВзорви танк,мать его!",
                Missions = "Взорвать танк 1",
                QuestType = QuestType.Убить,
                Target = "bradleyapc",
                Amount = 1,
                UseRepeat = true,
                Cooldown = 1200,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Чертёж,
                         Name = "explosive.timed",
                         SkinID = 0,
                         ShortName = "explosive.timed",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "arcade.machine.chippy",
                         SkinID = 0,
                         ShortName = "arcade.machine.chippy",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "small.oil.refinery",
                         SkinID = 0,
                         ShortName = "small.oil.refinery",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.КастомПредмет,
                         Name = "Фрагмент VIP",
                         SkinID = 2101056280,
                         ShortName = "skull.human",
                         Amount = 5,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [8] = new Quest
            {
                DisplayName = "Пора на бой!",
                Description = "Хватит сидеть дома,ты что, на карантине? Компоненты в руки и бегом к чертежу крафтить пушки!\nСкрафти оружие и давай на выход!",
                Missions = "Скрафтить 3 калаша",
                QuestType = QuestType.Скрафтить,
                Target = "rifle.ak",
                Amount = 3,
                UseRepeat = false,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "explosive.satchel",
                         SkinID = 0,
                         ShortName = "explosive.satchel",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "ammo.rifle.hv",
                         SkinID = 0,
                         ShortName = "ammo.rifle.hv",
                         Amount = 128,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [9] = new Quest
            {
                DisplayName = "Нужна полная защита дома!",
                Description = "Создай самые защищенные двери и установи их домой!\nНам нужно изучить бронированную одинарную дверь, где ее найти? Это уже твоя проблема!\nНайди и изучи, нам еще существовать,не забыл?",
                Missions = "Изучить бронированную одинарную дверь",
                QuestType = QuestType.Изучить,
                Target = "door.hinged.toptier",
                Amount = 1,
                UseRepeat = false,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "door.double.hinged.toptier",
                         SkinID = 0,
                         ShortName = "door.double.hinged.toptier",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [10] = new Quest
            {
                DisplayName = "Штурмуешь?Бери все и сразу!",
                Description = "Если штурмовать что-то,то значит забрать ВСЕ!\nВторого шанса не будет,найди и залутай 300 скрапа, глядишь,что-то из тебя и выйдет после этого!\nДавай не мешкай,бегом!",
                Missions = "Залутать 300 скрапа",
                QuestType = QuestType.Залутать,
                Target = "scrap",
                Amount = 300,
                UseRepeat = false,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "weapon.mod.8x.scope",
                         SkinID = 0,
                         ShortName = "weapon.mod.8x.scope",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "rifle.l96",
                         SkinID = 0,
                         ShortName = "rifle.l96",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "syringe.medical",
                         SkinID = 0,
                         ShortName = "syringe.medical",
                         Amount = 15,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [11] = new Quest
            {
                DisplayName = "Улучшил двери и окна, а о каркасе дома забыл?",
                Description = "Хорошие двери и окна не значит,что тебя не зарейдят, рейдеры умные и бьют в самые слабые места дома!\nПредугадай их действия и улучши 20 элементов построек в МВК",
                Missions = "улучшить 20 элементов построек в МВК",
                QuestType = QuestType.УлучшитьПостройку,
                Target = "4",
                Amount = 20,
                UseRepeat = false,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "guntrap",
                         SkinID = 0,
                         ShortName = "guntrap",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "autoturret",
                         SkinID = 0,
                         ShortName = "autoturret",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "electric.solarpanel.large",
                         SkinID = 0,
                         ShortName = "electric.solarpanel.large",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "electric.switch",
                         SkinID = 0,
                         ShortName = "electric.switch",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "electric.splitter",
                         SkinID = 0,
                         ShortName = "electric.splitter",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [12] = new Quest
            {
                DisplayName = "Властвуй!",
                Description = "Не забыл какую цель мы преследуем?\nЗахватить господство над островом,а чтобы заполучить его у тебя должны быть карты ко всем дверям!\nОткрой 10 красных дверей,чтобы доказать свое превосходство,ведь за этими дверьми таится прекрасный лут!",
                Missions = "Открыть 10 красных дверей",
                QuestType = QuestType.ИспользоватьКарточкуДоступа,
                Target = "red",
                Amount = 10,
                UseRepeat = false,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "rocket.launcher",
                         SkinID = 0,
                         ShortName = "rocket.launcher",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "heavy.plate.helmet",
                         SkinID = 0,
                         ShortName = "heavy.plate.helmet",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "heavy.plate.jacket",
                         SkinID = 0,
                         ShortName = "heavy.plate.jacket",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "explosive.satchel",
                         SkinID = 0,
                         ShortName = "explosive.satchel",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "ammo.rocket.basic",
                         SkinID = 0,
                         ShortName = "ammo.rocket.basic",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                }
            },
            [13] = new Quest
            {
                DisplayName = "Время переплавки",
                Description = "Пара переплавить все свои добытые ресурсы, нужно сделать для этого все возможное!",
                Missions = "Установи 3 небольших печки",
                QuestType = QuestType.установить,
                Target = "furnace",
                Amount = 3,
                UseRepeat = false,
                Cooldown = 0,

                PrizeList = new List<Quest.Prize>
                {
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "heavy.plate.helmet",
                         SkinID = 0,
                         ShortName = "heavy.plate.helmet",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "heavy.plate.jacket",
                         SkinID = 0,
                         ShortName = "heavy.plate.jacket",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                    new Quest.Prize
                    {
                         type = PrizeType.Предмет,
                         Name = "explosive.satchel",
                         SkinID = 0,
                         ShortName = "explosive.satchel",
                         Amount = 1,
                         Url = "",
                         Command = "",
                    },
                }
            },
        };
        #endregion
    }
}