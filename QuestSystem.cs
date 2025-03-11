using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Plugins.SignArtistClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Color = UnityEngine.Color;
using System.Globalization;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using ConVar;
using Oxide.Core.Libraries;
using VLB;

namespace Oxide.Plugins
{
    [Info("QuestSystem", "http://topplugin.ru/ DezLife - НУ ты и лошара", "2.0.7")]
    public class QuestSystem : RustPlugin
    {
        public static QuestSystem instance;
        [PluginReference] Plugin IQChat, RustMap;

        #region eNums
        private enum QuestType
        {
            убить,
            добыть,
            скрафтить,
            найти,
            улучшить
        }

        #endregion

        #region Classes

        private class Quest
        {
            internal class Prize
            {
                [JsonProperty("Это кастом предмет ? (Если это обычный предмет ставим false)")]
                public bool CustomItem;
                [JsonProperty("Отображаемое имя (Для кастом)")]
                public string DisplayName;
                [JsonProperty("Skin id (Для кастом но можно и для обычного предмета чтоб дать ему скин)")]
                public ulong Skin;
                [JsonProperty("Shortname выдаваемого предмета")]
                public string Shortname;
                [JsonProperty("количество")]
                public int Amount;
                [JsonProperty("Ссылка на картинку (Если используете команду для выдачи то обязательно!)")]
                public string ExternalURL;
                [JsonProperty("Команда")]
                public string Command;
            }

            [JsonProperty("Названия квеста")]
            public string DisplayName;
            [JsonProperty("Описания")]
            public string Description;
            [JsonProperty("Тип квеста (0 - убить, 1 - добыть, 2 - скрафтить, 3 - найти, 4 - улучшить)")]
            public QuestType QuestType;
            [JsonProperty("То чего добыть (На русском)")]
            public string NiceTarget;
            [JsonProperty("В зависимости от квеста. Если убить человека то player. Если добыть дерева то wood; Если это улучшения то 1 - дерево, 2 - камень и тд")]
            public string Target;
            [JsonProperty("Количевство")]
            public int Amount;




            [JsonProperty("Настройка награды")]
            public List<Prize> PrizeList = new List<Prize>();
            public List<ulong> FinishedPlayers = new List<ulong>();
        }

        private class PlayerQuest
        {
            public Quest parentQuest;

            public ulong UserID;

            public bool Finished;
            public int Count;

            public void AddCount(int amount = 1)
            {
                Count += amount;
                if (parentQuest.Amount <= Count)
                {

                    BasePlayer player = BasePlayer.FindByID(UserID);
                    if (player != null && player.IsConnected)
                    {
                        if (config.SoundEff)
                            instance.RunEffect(player, config.SoundEffPath);
                        instance.SendChat(instance.lang.GetMessage("QUEST", instance, player.UserIDString), String.Format(instance.lang.GetMessage("QUEST_Finished", instance, player.UserIDString), parentQuest.DisplayName), player);
                    }

                    Finished = true;
                }
            }

            public int LeftAmount() => parentQuest.Amount - Count;
        }

        #endregion

        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["QUEST_ACTIVE"] = "<size=16>Your active tasks:</ size>",
                ["QUEST"] = "<size=16>Tasks:</size>",
                ["QUEST_ACTIVE_comp"] = "You <color=#4286f4>are absent</color> active tasks, take them to the city!",
                ["QUEST_ACTIVE_PROGRESS"] = "You can <color=#4286f4>to complete</color> the task!",
                ["QUEST_ACTIVE_PROGRESS_GO"] = "Left: {0} {1} {2}\n",
                ["QUEST_ACTIVE_LIMIT"] = "You have too much <color=#4286f4>not finished</color> tasks!",
                ["QUEST_took_tasks"] = "You already <color=#4286f4>have taken</color> this task!",
                ["QUEST_completed_tasks"] = "You already <color=#4286f4>performed</color> this task!",
                ["QUEST_completed_took"] = "You <color=#4286f4>successfully</color> took the task {0}",
                ["QUEST_tasks_completed"] = "Thanks hold your <color=#4286f4>reward</color>!",
                ["QUEST_no_place"] = "Hey wait you everything <color=#4286f4>you will not carry away</color>, make room!",
                ["QUEST_did_not_cope"] = "Sorry you <color=#4286f4>did not cope</color> with the task!\n" +
                 $"Anyway, you can try again!",
                ["QUEST_back"] = "FORWARD",
                ["QUEST_next"] = "BACK",
                ["QUEST_prize"] = "REWARD FOR PERFORMANCE OF THE TASK",
                ["QUEST_done"] = "DONE",
                ["QUEST_take"] = "TAKE",
                ["QUEST_turn"] = "FOR RENT",
                ["QUEST_REFUSE"] = "REFUSE",
                ["QUEST_Finished"] = "You have completed the task: <color=#4286f4>{0}</color>",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["QUEST_ACTIVE"] = "<size=16>Ваши активные задачи:</size>",
                ["QUEST"] = "<size=16>Задания:</size>",
                ["QUEST_ACTIVE_comp"] = "У вас <color=#4286f4>отсутствуют</color> активные задачи, возьмите их в городе!",
                ["QUEST_ACTIVE_PROGRESS"] = "Вы можете <color=#4286f4>завершить</color> задачу!",
                ["QUEST_ACTIVE_PROGRESS_GO"] = "Осталось: {0} {1} {2}\n",
                ["QUEST_ACTIVE_LIMIT"] = "У тебя слишком много <color=#4286f4>не законченных</color> заданий!",
                ["QUEST_took_tasks"] = "Вы уже <color=#4286f4>взяли</color> это задание!",
                ["QUEST_completed_tasks"] = "Вы уже <color=#4286f4>выполняли</color> это задание!",
                ["QUEST_completed_took"] = "Вы <color=#4286f4>успешно</color> взяли задание {0}",
                ["QUEST_tasks_completed"] = "Спасибо, держи свою <color=#4286f4>награду</color>!",
                ["QUEST_no_place"] = "Эй, погоди, ты всё <color=#4286f4>не унесёшь</color>, освободи место!",
                ["QUEST_did_not_cope"] = "Жаль что ты <color=#4286f4>не справился</color> с заданием!\n" +
                 $"В любом случае, ты можешь попробовать ещё раз!",
                ["QUEST_back"] = "ВПЕРЕД",
                ["QUEST_next"] = "НАЗАД",
                ["QUEST_prize"] = "НАГРАДА ЗА ВЫПОЛНЕНИЕ ЗАДАНИЯ",
                ["QUEST_done"] = "ГОТОВО",
                ["QUEST_take"] = "ВЗЯТЬ",
                ["QUEST_turn"] = "СДАТЬ",
                ["QUEST_REFUSE"] = "ОТКАЗАТЬСЯ",
                ["QUEST_Finished"] = "Вы закончили задание: <color=#4286f4>{0}</color>",

            }, this, "ru");
        }

        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Можно ли повторно брать квесты ?")]
            public bool QuestGo;
            [JsonProperty("Картинка для доски заданий (Желательный размер 256 на 128)")]
            public string ImageURL;
            [JsonProperty("Максимум квестов которых игрок сможет взять за раз")]
            public int MaxQuestAmount;
            [JsonProperty("Оповестить игрока звуковым оповещением о том что он выполнил квест?")]
            public bool SoundEff;
            [JsonProperty("Эфект для проигрования")]
            public string SoundEffPath;
            [JsonProperty("Делать маркер на карте j?")]
            public bool MapMarkers;
            [JsonProperty("Сбрасывать прогрес квестов у игроков во время вайпа:?")]
            public bool WipeData;
            [JsonProperty("Включить прогресс бар для игроков ?")]
            public bool ProgressBar;
            [JsonProperty("SteamWebApiKey (Получить можно тут https://steamcommunity.com/dev/apikey)")]
            public string SteamWebApiKey;


            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    QuestGo = true,
                    ImageURL = "https://i.imgur.com/Zd10HwE.png",
                    MaxQuestAmount = 3,
                    SoundEff = true,
                    SoundEffPath = "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab",
                    MapMarkers = false,
                    WipeData = true,
                    ProgressBar = true,
                    SteamWebApiKey = ""


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
                PrintWarning("Ошибка #153" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Variables

        [PluginReference] private Plugin ImageLibrary;
        private BaseEntity Sign = null;

        private List<Quest> QuestList = new List<Quest>
        {
            {
                new Quest
                {
                    DisplayName = "Тупая железяка!!",
                    Description = "Как же меня достал этот Танк,я не могу ходить в космодром!\nУничтожь танк и приходи за наградой",

                    QuestType = QuestType.убить,
                    NiceTarget = "танк",
                    Target = "bradleyapc",
                    Amount = 1,

                    PrizeList = new List<Quest.Prize>
                    {
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "rifle.l96",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "weapon.mod.small.scope",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "jackhammer",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "",
                            Amount = 1,

                            ExternalURL = "https://i.imgur.com/9OJhuKj.png",
                            Command = "emerald.give %STEAMID% Рубин 15",
                        },
                    }
                }
            },
            {
                new Quest
                {
                    DisplayName = "Нужно срочно отстроить дом!",
                    Description = "Нам нужно где то складывать свои ресурсы!\n Добудь 1000 камня и возвращайся за наградой",

                    QuestType = QuestType.добыть,
                    NiceTarget = "камня",
                    Target = "stones",
                    Amount = 1000,

                    PrizeList = new List<Quest.Prize>
                    {
                        new Quest.Prize
                        {
                            CustomItem = true,
                            DisplayName = "Радиактивная сера",
                            Skin = 1681986132,
                            Shortname = "glue",
                            Amount = 10,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "supply.signal",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "door.hinged.toptier",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "",
                            Amount = 1,

                            ExternalURL = "https://i.imgur.com/9OJhuKj.png",
                            Command = "emerald.give %STEAMID% Рубин 25",
                        },
                    }
                }
            },
            {
                new Quest
                {
                    DisplayName = "Хммм... Нужно сделать гараж для мини-коптера!",
                    Description = "Нужно побольше места для коптеров\nСкрафти 1 гаражных дверей",

                    QuestType = QuestType.скрафтить,
                    NiceTarget = "гаражных дверей",
                    Target = "wall.frame.garagedoor",
                    Amount = 1,

                    PrizeList = new List<Quest.Prize>
                    {
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "",
                            Amount = 1,

                            ExternalURL = "https://i.imgur.com/PoeTa16.png",
                            Command = "give_minicopter %STEAMID%",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "floor.ladder.hatch",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Shortname = "keycard_blue",
                            Amount = 2,

                            ExternalURL = "",
                            Command = "",
                        },
                    }
                }
            },
            {
                new Quest
                {
                    DisplayName = "Мне нужно больше изучений!",
                    Description = "Мне нужно изучить множество вещей,но у меня не хватает скрапа\nНайди 100 скрапа и приходи за наградой!",

                    QuestType = QuestType.найти,
                    NiceTarget = "скрап(-а)",
                    Target = "scrap",
                    Amount = 100,

                    PrizeList = new List<Quest.Prize>
                    {
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "smg.thompson",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "ammo.pistol.fire",
                            Amount = 64,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "",
                            Amount = 1,

                            ExternalURL = "https://i.imgur.com/9OJhuKj.png",
                            Command = "emerald.give %STEAMID% Рубин 15",
                        },
                    }
                }
            },
            {
                new Quest
                {
                    DisplayName = "Нужно укрепить свою строения в камень!",
                    Description = "Улучши 15 разных построек в камень",

                    QuestType = QuestType.улучшить,
                    NiceTarget = "раз в камень",
                    Target = "2",
                    Amount = 3,

                    PrizeList = new List<Quest.Prize>
                    {
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "gates.external.high.stone",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "bed",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "furnace.large",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                    }
                }
            },
            {
                new Quest
                {
                    DisplayName = "Мой дом, моя крепость",
                    Description = "Нужно лучше защитить свою базу\nСкрафти 10 заборов",

                    QuestType = QuestType.скрафтить,
                    NiceTarget = "забор(-ов)",
                    Target = "wall.external.high.stone",
                    Amount = 10,

                    PrizeList = new List<Quest.Prize>
                    {
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "wall.window.glass.reinforced",
                            Amount = 3,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "metal.fragments",
                            Amount = 5000,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "",
                            Amount = 1,

                            ExternalURL = "https://i.imgur.com/JnPNvJi.png",
                            Command = "givetool %STEAMID% pickaxe",
                        },
                    }
                }
            },
            {
                new Quest
                {
                    DisplayName = "Мои любимые часы",
                    Description = "Мои любимые часы барахлят,опять эти шестеренки..\nНайди 40 шестеренок и приходи за наградой!",

                    QuestType = QuestType.найти,
                    NiceTarget = "шестеренка(-ок)",
                    Target = "gears",
                    Amount = 40,

                    PrizeList = new List<Quest.Prize>
                    {
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "autoturret",
                            Amount = 1,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "supply.signal",
                            Amount = 2,

                            ExternalURL = "",
                            Command = "",
                        },
                        new Quest.Prize
                        {
                            CustomItem = false,
                            DisplayName = "",
                            Skin = 0,
                            Shortname = "",
                            Amount = 1,

                            ExternalURL = "https://i.imgur.com/9OJhuKj.png",
                            Command = "emerald.give %STEAMID% Рубин 15",
                        },
                    }
                }
            },
        };
        private Dictionary<ulong, List<PlayerQuest>> PlayerQuests = new Dictionary<ulong, List<PlayerQuest>>();
        private Dictionary<string, List<ulong>> PlayerQuestsFinish = new Dictionary<string, List<ulong>>();

        #endregion
        
        
        #region Метка на g карте 
        private void CreateMarker(Vector3 position, float refreshRate, string name, string displayName,
           float radius = 0.10f, string colorMarker = "ff60df", string colorOutline = "00FFFFFF")
        {
            var marker = new GameObject().AddComponent<CustomMapMarker>();
            marker.name = name;
            marker.displayName = displayName;
            marker.radius = radius;
            marker.position = position;
            marker.refreshRate = refreshRate;
            ColorUtility.TryParseHtmlString($"#{colorMarker}", out marker.color1);
            ColorUtility.TryParseHtmlString($"#{colorOutline}", out marker.color2);
        }

        private void RemoveMarkers()
        {
            foreach (var marker in UnityEngine.Object.FindObjectsOfType<CustomMapMarker>())
            {
                UnityEngine.Object.Destroy(marker);
            }
        }

        private const string genericPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";
        private const string vendingPrefab = "assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab";

        #region Scripts

        private class CustomMapMarker : MonoBehaviour
        {
            private VendingMachineMapMarker vending;
            private MapMarkerGenericRadius generic;
            public BaseEntity parent;
            private bool asChild;

            public float radius;
            public Color color1;
            public Color color2;
            public string displayName;
            public float refreshRate;
            public Vector3 position;
            public bool placedByPlayer;

            private void Start()
            {
                transform.position = position;
                asChild = parent != null;
                CreateMarkers();
            }

            private void CreateMarkers()
            {
                vending = GameManager.server.CreateEntity(vendingPrefab, position)
                    .GetComponent<VendingMachineMapMarker>();
                vending.markerShopName = displayName;
                vending.enableSaving = false;
                vending.Spawn();

                generic = GameManager.server.CreateEntity(genericPrefab).GetComponent<MapMarkerGenericRadius>();
                generic.color1 = color1;
                generic.color2 = color2;
                generic.radius = radius;
                generic.alpha = 1f;
                generic.enableSaving = false;
                generic.SetParent(vending);
                generic.Spawn();

                UpdateMarkers();

                if (refreshRate > 0f)
                {
                    if (asChild)
                    {
                        InvokeRepeating(nameof(UpdatePosition), refreshRate, refreshRate);
                    }
                    else
                    {
                        InvokeRepeating(nameof(UpdateMarkers), refreshRate, refreshRate);
                    }
                }
            }

            private void UpdatePosition()
            {
                if (asChild == true)
                {
                    if (parent.IsValid() == false)
                    {
                        Destroy(this);
                        return;
                    }
                    else
                    {
                        var pos = parent.transform.position;
                        transform.position = pos;
                        vending.transform.position = pos;
                    }
                }

                UpdateMarkers();
            }

            private void UpdateMarkers()
            {
                vending.SendNetworkUpdate();
                generic.SendUpdate();
            }

            private void DestroyMakers()
            {
                if (vending.IsValid())
                {
                    vending.Kill();
                }

                if (generic.IsValid())
                {
                    generic.Kill();
                }


            }

            private void OnDestroy()
            {
                DestroyMakers();
            }
        }

        #endregion

        #endregion

        #region Initialization

        private void OnServerInitialized()
        {
            instance = this;
            LoadData("Quest", ref QuestList, false);
            LoadData("Players", ref PlayerQuests, true);


            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerInit(BasePlayer.activePlayerList[i]);
            }
            if (!ImageLibrary)
            {
                PrintError($"ERROR! Plugin ImageLibrary not found!");
                return;
            }

            if (PlayerQuestsFinish.Count == 0 && !config.QuestGo)
            {
                for(int i = 0; i < QuestList.Count(); i++)
                {
                    PlayerQuestsFinish.Add(QuestList[i].DisplayName, new List<ulong>());
                } 
            }
            LoadData("PlayersFinish", ref PlayerQuestsFinish, true);
            for(int i = 0; i < QuestList.Count; i++)
            {
                for(int u = 0; u < QuestList[i].PrizeList.Count; u++)
                {
                    ImageLibrary.Call("AddImage", QuestList[i].PrizeList[u].ExternalURL, QuestList[i].PrizeList[u].ExternalURL);
                }      
            }

            Unsubscribe("OnPlayerInput");
            if (!TryPlaceSign())
            {
                PrintError($"ERROR! Plugin could not place sign!");
                return;
            }
        }


        #endregion

        #region Functions

        private bool TryPlaceSign()
        {
            var monument = UnityEngine.Object.FindObjectsOfType<MonumentInfo>().FirstOrDefault(p => p.name.Contains("compound"));

            if (monument == null)
            {
                return false;
            }

            timer.Once(1, () =>
            {
                Vector3 resultVector = monument.transform.position;

                Sign = GameManager.server.CreateEntity("assets/prefabs/deployable/signs/sign.post.town.roof.prefab", monument.transform.position + monument.transform.rotation * new Vector3(-5.4f, 0, -0.5f), monument.transform.rotation);
                Sign.SetFlag(BaseEntity.Flags.Locked, true);
                Sign.Spawn();
                if (config.MapMarkers)
                    CreateMarker(Sign.transform.position, 5, "ЗАДАНИЯ", "ЗАДАНИЯ");
                RustMap?.Call("AddTemporaryMarker", "https://i.imgur.com/UtXBFFQ.png", "ЗАДАНИЯ", Sign.transform.position);
                ServerMgr.Instance.StartCoroutine(DownloadImage(config.ImageURL, Sign as Signage));
                Subscribe("OnPlayerInput");
            });
            return true;
        }

        #endregion

        #region Hooks

        void OnNewSave(string filename)
        {
            if (config.WipeData)
            {
                PlayerQuests.Clear();
                PrintWarning("Обнаружен WIPE . Дата игроков сброшена");
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.USE) && Vector3.Distance(Sign.transform.position, player.transform.position) < 5)
            {
                UI_DrawInterface(player);
            }
        }

        private void Unload()
        {
            if (Sign != null && !Sign.IsDestroyed)
                Sign.Kill();
            LoadData("Quest", ref QuestList, false);
            SaveData("Players", PlayerQuests, false);
            SaveData("PlayersFinish", PlayerQuestsFinish, false);
            if (config.MapMarkers)
                RemoveMarkers();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!PlayerQuests.ContainsKey(player.userID))
                PlayerQuests.Add(player.userID, new List<PlayerQuest>());
        }


        #region Type Upgrade
        private void CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            List<PlayerQuest> playerQuests = null;
            if (PlayerQuests.TryGetValue(player.userID, out playerQuests))
            {
                for(int i = 0; i < playerQuests.Count; i++)
                {
                    if (playerQuests[i].parentQuest.QuestType == QuestType.улучшить && playerQuests[i].Finished == false)
                    {
                        if ((int)grade == Convert.ToInt16(playerQuests[i].parentQuest.Target))
                        {
                            playerQuests[i].AddCount();
                        }
                    }
                }  
            }
        }
        #endregion

        #region Type death
        private Dictionary<uint, Dictionary<ulong, int>> HeliAttackers = new Dictionary<uint, Dictionary<ulong, int>>();

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null || info == null) return;

                BasePlayer player = null;

                if (info.InitiatorPlayer != null)
                    player = info.InitiatorPlayer;
                else if (entity.GetComponent<BaseHelicopter>() != null)
                    player = BasePlayer.FindByID(GetLastAttacker(entity.net.ID));

                List<PlayerQuest> playerQuests = null;

                if (PlayerQuests.TryGetValue(player.userID, out playerQuests))
                {
                    for (int i = 0; i < playerQuests.Count; i++)
                    {
                        if (playerQuests[i].parentQuest.QuestType == QuestType.убить && playerQuests[i].Finished == false)
                        {
                            if (entity.PrefabName.Contains(playerQuests[i].parentQuest.Target))
                            {
                                playerQuests[i].AddCount();
                            }
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
            if (victim.GetComponent<BaseHelicopter>() != null && info?.Initiator?.ToPlayer() != null)
            {
                var heli = victim.GetComponent<BaseHelicopter>();
                var player = info.Initiator.ToPlayer();
                NextTick(() =>
                {
                    if (heli == null) return;
                    if (!HeliAttackers.ContainsKey(heli.net.ID))
                        HeliAttackers.Add(heli.net.ID, new Dictionary<ulong, int>());
                    if (!HeliAttackers[heli.net.ID].ContainsKey(player.userID))
                        HeliAttackers[heli.net.ID].Add(player.userID, 0);
                    HeliAttackers[heli.net.ID][player.userID]++;
                });
            }
        }

        private ulong GetLastAttacker(uint id)
        {
            int hits = 0;
            ulong majorityPlayer = 0U;
            if (HeliAttackers.ContainsKey(id))
            {
                foreach (var score in HeliAttackers[id])
                {
                    if (score.Value > hits)
                        majorityPlayer = score.Key;
                }
            }
            return majorityPlayer;
        }

        #endregion

        #region Type loot
        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity.OwnerID == 1337228 || entity.OwnerID >= 7656000000 || entity.GetComponent<StorageContainer>() == null)
                return;


            List<PlayerQuest> playerQuests = null;
            if (PlayerQuests.TryGetValue(player.userID, out playerQuests))
            {
                for (int i = 0; i < playerQuests.Count; i++)
                {
                    if (playerQuests[i].parentQuest.QuestType == QuestType.найти && playerQuests[i].Finished == false)
                    {
                        for (int u = 0; u < entity.GetComponent<StorageContainer>().inventory.itemList.Count(); u++)
                        {
                            if (entity.GetComponent<StorageContainer>().inventory.itemList[u].info.shortname.Contains(playerQuests[i].parentQuest.Target))
                                playerQuests[i].AddCount(entity.GetComponent<StorageContainer>().inventory.itemList[u].amount);
                        }
                    }
                }   
            }
            entity.OwnerID = 1337228;
        }
        #endregion

        #region Type Gather

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player;
            if (entity is BasePlayer)
            {
                player = entity as BasePlayer;
                List<PlayerQuest> playerQuests = null;
                if (PlayerQuests.TryGetValue(player.userID, out playerQuests))
                {
                    for (int i = 0; i < playerQuests.Count; i++)
                    {
                        if (playerQuests[i].parentQuest.QuestType == QuestType.добыть && playerQuests[i].Finished == false)
                        {
                            if (item.info.shortname.Contains(playerQuests[i].parentQuest.Target))
                            {
                                playerQuests[i].AddCount(item.amount);
                            }
                        }
                    }
                }
            }
        }
        void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);

        void OnCollectiblePickup(Item item, BasePlayer player)
        {

            List<PlayerQuest> playerQuests = null;
            if (PlayerQuests.TryGetValue(player.userID, out playerQuests))
            {
                for (int i = 0; i < playerQuests.Count; i++)
                {
                    if (playerQuests[i].parentQuest.QuestType == QuestType.добыть && playerQuests[i].Finished == false)
                    {
                        if (item.info.shortname.Contains(playerQuests[i].parentQuest.Target))
                        {
                            playerQuests[i].AddCount(item.amount);
                        }
                    }
                }
            }
        }
        #endregion

        #region Type craft
        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            List<PlayerQuest> playerQuests = null;
            BasePlayer player = task.owner;

            if (player != null && PlayerQuests.TryGetValue(player.userID, out playerQuests))
            {
                for (int i = 0; i < playerQuests.Count; i++)
                {
                    if (playerQuests[i].parentQuest.QuestType == QuestType.скрафтить && playerQuests[i].Finished == false)
                    {
                        if (task.blueprint.targetItem.shortname.Contains(playerQuests[i].parentQuest.Target))
                        {
                            playerQuests[i].AddCount(item.amount);
                        }
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Commands

        [ChatCommand("quest")]
        private void CmdChatQuest(BasePlayer player, string command, string[] args)
        {
            List<PlayerQuest> playerQuests = null;
            if (PlayerQuests.TryGetValue(player.userID, out playerQuests))
            {
                if (args.Length > 0)
                {
                    switch (args[0].ToLower())
                    {
                        case "list":
                            {
                                if (playerQuests.Count == 0)
                                {
                                    SendChat(lang.GetMessage("QUEST_ACTIVE", this, player.UserIDString), lang.GetMessage("QUEST_ACTIVE_comp", this, player.UserIDString), player);
                                    return;
                                }
                                else
                                {
                                    string message = "";
                                    for(int i = 0; i < playerQuests.Count; i++)
                                    {
                                        message += $"\n{i + 1}. {playerQuests[i].parentQuest.DisplayName}\n\n" +
                                                   $"{playerQuests[i].parentQuest.Description}\n";

                                        if (playerQuests[i].Finished)
                                        {
                                            message += lang.GetMessage("QUEST_ACTIVE_PROGRESS", this, player.UserIDString);
                                        }
                                        else
                                        {
                                            message += String.Format(lang.GetMessage("QUEST_ACTIVE_PROGRESS_GO", this, player.UserIDString), playerQuests[i].parentQuest.QuestType.ToString(), playerQuests[i].LeftAmount(), playerQuests[i].parentQuest.NiceTarget);
                                        }
                                    }      
                                    SendChat(lang.GetMessage("QUEST_ACTIVE", this, player.UserIDString), message, player);
                                    break;
                                }
                            }
                    }
                }
            }
        }

        [ConsoleCommand("UI_Handler")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            List<PlayerQuest> playerQuests = null;

            if (player != null && args.HasArgs(1) && PlayerQuests.TryGetValue(player.userID, out playerQuests))
            {
                switch (args.Args[0])
                {
                    case "get":
                        {
                            int questIndex;
                            if (args.HasArgs(2) && int.TryParse(args.Args[1], out questIndex))
                            {
                                var currentQuest = QuestList.ElementAtOrDefault(questIndex);
                                if (currentQuest != null)
                                {
                                    if (playerQuests.Count >= config.MaxQuestAmount)
                                    {
                                        SendChat(lang.GetMessage("QUEST", this, player.UserIDString), lang.GetMessage("QUEST_ACTIVE_LIMIT", this, player.UserIDString), player);
                                        return;
                                    }
                                    if (playerQuests.Any(p => p.parentQuest.DisplayName == currentQuest.DisplayName))
                                    {
                                        SendChat(lang.GetMessage("QUEST", this, player.UserIDString), lang.GetMessage("QUEST_took_tasks", this, player.UserIDString), player);
                                        return;
                                    }

                                    if (!config.QuestGo && PlayerQuestsFinish[currentQuest.DisplayName].Contains(player.userID))
                                    {
                                        SendChat(lang.GetMessage("QUEST", this, player.UserIDString), lang.GetMessage("QUEST_completed_tasks", this, player.UserIDString), player);
                                        return;
                                    }

                                    playerQuests.Add(new PlayerQuest() { UserID = player.userID, parentQuest = currentQuest });
                                    SendChat(lang.GetMessage("QUEST", this, player.UserIDString), String.Format(lang.GetMessage("QUEST_completed_took", this, player.UserIDString), currentQuest.DisplayName), player);
                                    UI_DrawInterface(player, 0, questIndex.ToString());
                                }
                            }

                            break;
                        }
                    case "page":
                        {
                            int pageIndex;
                            if (int.TryParse(args.Args[1], out pageIndex))
                            {
                                UI_DrawInterface(player, pageIndex);
                            }

                            break;
                        }
                    case "finish":
                        {
                            int questIndex;
                            if (args.HasArgs(2) && int.TryParse(args.Args[1], out questIndex))
                            {
                                var globalQuest = QuestList.ElementAtOrDefault(questIndex);
                                if (globalQuest != null)
                                {
                                    var currentQuest = playerQuests.FirstOrDefault(p => p.parentQuest.DisplayName == globalQuest.DisplayName);
                                    if (currentQuest == null)
                                        return;

                                    if (currentQuest.Finished)
                                    {
                                        SendChat(lang.GetMessage("QUEST", this, player.UserIDString), lang.GetMessage("QUEST_tasks_completed", this, player.UserIDString), player);

                                        if (24 - player.inventory.containerMain.itemList.Count < currentQuest.parentQuest.PrizeList.Count)
                                        {
                                            SendChat(lang.GetMessage("QUEST", this, player.UserIDString), lang.GetMessage("QUEST_no_place", this, player.UserIDString), player);
                                            return;
                                        }
                                        else
                                        {
                                            currentQuest.Finished = false;
                                            for(int i = 0; i < currentQuest.parentQuest.PrizeList.Count; i++)
                                            {
                                                var check = currentQuest.parentQuest.PrizeList[i];
                                                if (check.Shortname != "")
                                                {
                                                    if (check.CustomItem)
                                                    {
                                                        Item newItem = ItemManager.CreateByPartialName(check.Shortname, check.Amount, check.Skin);
                                                        newItem.name = check.DisplayName;
                                                        newItem.MoveToContainer(player.inventory.containerMain);
                                                    }
                                                    else
                                                    {
                                                        Item newItem = ItemManager.CreateByPartialName(check.Shortname, check.Amount);
                                                        if (check.Skin != 0)
                                                            newItem.skin = check.Skin;
                                                        newItem.MoveToContainer(player.inventory.containerMain);
                                                    }

                                                }
                                                if (check.Command != "")
                                                {
                                                    Server.Command(check.Command.Replace("%STEAMID%", player.UserIDString));
                                                }
                                            }
                                        }
                                        if (!config.QuestGo)
                                        {
                                            PlayerQuestsFinish[globalQuest.DisplayName].Add(player.userID);
                                        }

                                        playerQuests.Remove(currentQuest);
                                    }
                                    else
                                    {
                                        SendChat(lang.GetMessage("QUEST", this, player.UserIDString), lang.GetMessage("QUEST_did_not_cope", this, player.UserIDString), player);


                                        playerQuests.Remove(currentQuest);
                                    }
                                    UI_DrawInterface(player, 0, questIndex.ToString());
                                }
                                else
                                {
                                    SendChat(lang.GetMessage("QUEST", this, player.UserIDString), $"Вы <color=#4286f4>не брали</color> этого задания!", player);
                                }
                            }

                            break;
                        }
                }
            }
        }

        #endregion

        #region Interface

        private const string Layer = "UI_Layer";
        private void UI_DrawInterface(BasePlayer player, int page = 0, string questUpdate = "")
        {
            List<PlayerQuest> playerQuests;
            if (!PlayerQuests.TryGetValue(player.userID, out playerQuests))
                return;

            CuiElementContainer container = new CuiElementContainer();

            if (questUpdate == "")
            {
                CuiHelper.DestroyUi(player, Layer);
                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0.09687495 0.093518253", AnchorMax = "0.903125 0.9064815", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0" }
                }, "Overlay", Layer);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "-1 -1", AnchorMax = "2 2", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0.9", Close = Layer },
                    Text = { Text = "" }
                }, Layer);

                if (page != 0)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 -0.08", AnchorMax = "0 -0.08", OffsetMax = "100 25" },
                        Button = { Color = HexToRustFormat("8686862A"), Command = $"UI_Handler page {page - 1}" },
                        Text = { Text = lang.GetMessage("QUEST_next", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16 }
                    }, Layer);
                }

                if (QuestList.Count > 6)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "1 -0.08", AnchorMax = "1 -0.08", OffsetMin = "-100 0", OffsetMax = "0 25" },
                        Button = { Color = HexToRustFormat("8686862A"), Command = $"UI_Handler page {page + 1}" },
                        Text = { Text = lang.GetMessage("QUEST_back", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16 }
                    }, Layer);
                }

                var firstPosition = 0.01769468 - (double)page * 0.49;
                for (int i = 0; i < QuestList.Count(); i++)
                {
                    var check = QuestList[i];
                    var currentPlayerQuest = playerQuests.FirstOrDefault(p => p.parentQuest.DisplayName == check.DisplayName);

                    string questLayer = Layer + $".{i}";

                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Name = questLayer,
                        Components =
                        {
                            new CuiImageComponent { Color = HexToRustFormat("#8686862A"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{firstPosition + Math.Floor((double) i / 3) * 0.49} {0.6743472 - i * 0.34 + Math.Floor((double) i / 3) * 3 * 0.34}",
                                AnchorMax = $"{firstPosition + 0.4870099 + Math.Floor((double) i / 3) * 0.49} {1 - i * 0.34 + Math.Floor((double) i / 3) * 3 * 0.34}",
                                OffsetMax = "0 0"
                            }
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.01805133 0.8251275", AnchorMax = "0.7714755 1", OffsetMax = "0 0" },
                        Text = { Text = check.DisplayName, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 18, Color = HexToRustFormat("#E5DCD5FF") }
                    }, questLayer);

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.01009262 0.6047886", AnchorMax = "1 0.8251275", OffsetMax = "0 0" },
                        Text = { Text = check.Description, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = HexToRustFormat("#9E9791FF") }
                    }, questLayer);

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.1870269 0.4172195", AnchorMax = "0.8129731 0.52784253", OffsetMax = "0 0" },
                        Text = { Text = lang.GetMessage("QUEST_prize", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = HexToRustFormat("#D0BC84FF") }
                    }, questLayer);

                    var currentList = check.PrizeList;
                    float minPosition = 0.53f - (float)currentList.Count / 2 * 0.12f - (float)(currentList.Count - 1) / 2 * 0.04f;

                    for (int o = 0; o < currentList.Count(); o++)
                    {
                        string prizeLayer = questLayer + $".{o}";
                        var prize = currentList[o];
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"{minPosition + o * 0.12f} 0.1291364", AnchorMax = $"{minPosition + (o + 1) * 0.12f} 0.4194313", OffsetMax = "0 0" },
                            Button = { Color = HexToRustFormat("#73737370") },
                            Text = { Text = "" }
                        }, questLayer, prizeLayer);

                        var trueImage = prize.Shortname == "" ? (string)ImageLibrary.Call("GetImage", prize.ExternalURL) : GetItemImage(prize.Shortname, prize.Skin);
                        container.Add(new CuiElement
                        {
                            Parent = prizeLayer,
                            Components =
                            {
                                new CuiRawImageComponent { Png = trueImage },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                            }
                        });

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-2 0", OffsetMin = "0 2" },
                            Text = { Text = $"x{prize.Amount}", Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerRight, FontSize = 16, Color = HexToRustFormat("#CDCDCDFF") }
                        }, prizeLayer);

                        minPosition += 0.01f;
                    }

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.7812921 0.802927", AnchorMax = "0.9858913 0.966369", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, questLayer, questLayer + ".BtnHolder");

                    string command = "";
                    string color = "";
                    string text = "";
                    if (config.ProgressBar)
                    {
                        if (currentPlayerQuest != null)
                        {
                            float y = (float)currentPlayerQuest.Count / currentPlayerQuest.parentQuest.Amount;

                            if (currentPlayerQuest.Count > currentPlayerQuest.parentQuest.Amount)
                            {
                                currentPlayerQuest.Count = currentPlayerQuest.parentQuest.Amount;
                            }
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0.1865106 0.01372072", AnchorMax = "0.8046368 0.1116493", OffsetMax = "0 0" },
                                Button = { Color = HexToRustFormat("#FFFFFF3E") },
                                Text = { Text = "" }
                            }, questLayer, questLayer + "bar");


                            container.Add(new CuiElement
                            {
                                Parent = questLayer + "bar",
                                Components =
                        {
                             new CuiImageComponent { Color = HexToRustFormat("#A8E6008A"),Sprite = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"{y} 1", OffsetMin = "1 1", OffsetMax = "-2 -1"  },
                        }
                            });

                            container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1 1", OffsetMax = "-2 -1" },
                                Text = { Text = $"{currentPlayerQuest.Count}" + "/" + $"{currentPlayerQuest.parentQuest.Amount}" + $" ({Math.Floor(y * 100).ToString()}%)", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 10, Color = HexToRustFormat("#FFFFFFFF") }
                            }, questLayer + "bar");

                        }
                    }

                    if (currentPlayerQuest == null)
                    {
                        if (!config.QuestGo && PlayerQuestsFinish[check.DisplayName].Contains(player.userID))
                        {
                            text = lang.GetMessage("QUEST_done", this, player.UserIDString);
                            color = "#61a9a5";
                            command = $"UI_Handler get {i}";
                        }
                        else
                        {
                            text = lang.GetMessage("QUEST_take", this, player.UserIDString);
                            color = "#6d854bFF";
                            command = $"UI_Handler get {i}";
                        }
                    }
                    else if (currentPlayerQuest.Finished)
                    {

                        text = lang.GetMessage("QUEST_turn", this, player.UserIDString);
                        color = "#4B6785FF";
                        command = $"UI_Handler finish {QuestList.IndexOf(QuestList.FirstOrDefault(p => p.DisplayName == currentPlayerQuest.parentQuest.DisplayName))}";
                    }
                    else
                    {

                        text = lang.GetMessage("QUEST_REFUSE", this, player.UserIDString);
                        color = "#854B4BFF";
                        command = $"UI_Handler finish {QuestList.IndexOf(QuestList.FirstOrDefault(p => p.DisplayName == currentPlayerQuest.parentQuest.DisplayName))}";
                    }

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = HexToRustFormat(color), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Command = command },
                        Text = { Text = text, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16, Color = HexToRustFormat("#d9e1c6") }
                    }, questLayer + ".BtnHolder", questLayer + ".Btn");
                }
            }
            else
            {
                string questLayer = Layer + $".{questUpdate}";
                CuiHelper.DestroyUi(player, questLayer + ".Btn");
                CuiHelper.DestroyUi(player, questLayer + "bar");
               
                var updateQuest = QuestList.ElementAt(Convert.ToInt32(questUpdate));
                var currentPlayerQuest = playerQuests.FirstOrDefault(p => p.parentQuest.DisplayName == updateQuest.DisplayName);

                string command = "";
                string color = "";
                string text = "";
                if (config.ProgressBar)
                {
                    if (currentPlayerQuest != null)
                    {
                        float y = (float)currentPlayerQuest.Count / currentPlayerQuest.parentQuest.Amount;

                        if (currentPlayerQuest.Count > currentPlayerQuest.parentQuest.Amount)
                        {
                            currentPlayerQuest.Count = currentPlayerQuest.parentQuest.Amount;
                        }
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.1865106 0.01372072", AnchorMax = "0.8046368 0.1116493", OffsetMax = "0 0" },
                            Button = { Color = HexToRustFormat("#FFFFFF3E") },
                            Text = { Text = "" }
                        }, questLayer, questLayer + "bar");


                        container.Add(new CuiElement
                        {
                            Parent = questLayer + "bar",
                            Components =
                        {
                             new CuiImageComponent { Color = HexToRustFormat("#A8E6008A"),Sprite = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"{y} 1", OffsetMin = "1 1", OffsetMax = "-2 -1"  },
                        }
                        });

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "1 1", OffsetMax = "-2 -1" },
                            Text = { Text = $"{currentPlayerQuest.Count}" + "/" + $"{currentPlayerQuest.parentQuest.Amount}" + $" ({Math.Floor(y * 100).ToString()}%)", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FontSize = 10, Color = HexToRustFormat("#FFFFFFFF") }
                        }, questLayer + "bar");

                    }
                }


                if (currentPlayerQuest == null)
                {   
                    if (!config.QuestGo && PlayerQuestsFinish[updateQuest.DisplayName].Contains(player.userID))
                    {
                        text = lang.GetMessage("QUEST_done", this, player.UserIDString);
                        color = "#61a9a5";
                        command = $"UI_Handler get {questUpdate}";
                    }
                    else
                    {
                        text = lang.GetMessage("QUEST_take", this, player.UserIDString);
                        color = "#6d854bFF";
                        command = $"UI_Handler get {questUpdate}";
                    }
                }
                else if (currentPlayerQuest.Finished)
                {
                    text = lang.GetMessage("QUEST_turn", this, player.UserIDString);
                    color = "#4B6785FF";
                    command = $"UI_Handler finish {QuestList.IndexOf(QuestList.FirstOrDefault(p => p.DisplayName == currentPlayerQuest.parentQuest.DisplayName))}";
                }
                else
                {
                    text = lang.GetMessage("QUEST_REFUSE", this, player.UserIDString);
                    color = "#854B4BFF";
                    command = $"UI_Handler finish {QuestList.IndexOf(QuestList.FirstOrDefault(p => p.DisplayName == currentPlayerQuest.parentQuest.DisplayName))}";
                }

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat(color), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Command = command, Close = questLayer + "bar" },
                    Text = { Text = text, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16, Color = HexToRustFormat("#d9e1c6") }
                }, questLayer + ".BtnHolder", questLayer + ".Btn");
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Helps   

        public void SendChat(string Descrip, string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Descrip);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region PlayerValidatorModule

        void RunEffect(BasePlayer player, string path)
        {
            Effect effect = new Effect();
            effect.Init(Effect.Type.Generic, player.transform.position, player.transform.forward, (Network.Connection)null);
            effect.pooledString = path; EffectNetwork.Send(effect, player.net.connection);
        }

        private bool IsEntityPlayer(BaseEntity entity, out BasePlayer result)
        {
            result = null;
            if (entity is BasePlayer)
            {
                BasePlayer player = entity as BasePlayer;
                if (player.IsNpc)
                {
                    return false;
                }
                if (player.GetComponent<BaseNPC>() != null)
                    return false;

                result = player;
                return true;
            }

            return false;
        }

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
                SaveData(name, data, true);
            }
        }

        private void SaveData<T>(string name, T data, bool autoSave)
        {
            string resultName = this.Name + $"/{name}";

            Interface.Oxide.DataFileSystem.WriteObject(resultName, data);

            if (autoSave)
            {
                timer.Every(60, () => SaveData<T>(name, data, false));
            }
        }

        #endregion

        #region UIWorkerModule

        private static string HexToRustFormat(string hex)
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
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion

        #region CoroutinesModule

        private IEnumerator DownloadImage(string url, Signage sign)
        {
            using (WWW www = new WWW(url))
            {
                yield return www;

                if (www.error != null)
                {
                    PrintError("ERROR!!! Can't connect to host with image!");
                    yield break;
                }
                byte[] imageBytes;
                imageBytes = www.bytes;
                byte[] resizedImageBytes = imageBytes.ResizeImage(256, 128, 256, 128, false);

                if (sign.textureID > 0)
                {
                    FileStorage.server.Remove(sign.textureID, FileStorage.Type.png, sign.net.ID);
                }

                sign.textureID = FileStorage.server.Store(resizedImageBytes, FileStorage.Type.png, sign.net.ID);

                sign.SendNetworkUpdate();
            }
        }

        #endregion

        #region LoadSkins

        #region SteampoweredAPI 
        private class SteampoweredResult
        {
            public Response response;
            public class Response
            {
                [JsonProperty("result")]
                public int result;

                [JsonProperty("resultcount")]
                public int resultcount;

                [JsonProperty("publishedfiledetails")]
                public List<PublishedFiled> publishedfiledetails;
                public class PublishedFiled
                {
                    [JsonProperty("publishedfileid")]
                    public ulong publishedfileid;

                    [JsonProperty("result")]
                    public int result;

                    [JsonProperty("creator")]
                    public string creator;

                    [JsonProperty("creator_app_id")]
                    public int creator_app_id;

                    [JsonProperty("consumer_app_id")]
                    public int consumer_app_id;

                    [JsonProperty("filename")]
                    public string filename;

                    [JsonProperty("file_size")]
                    public int file_size;

                    [JsonProperty("preview_url")]
                    public string preview_url;

                    [JsonProperty("hcontent_preview")]
                    public string hcontent_preview;

                    [JsonProperty("title")]
                    public string title;

                    [JsonProperty("description")]
                    public string description;

                    [JsonProperty("time_created")]
                    public int time_created;

                    [JsonProperty("time_updated")]
                    public int time_updated;

                    [JsonProperty("visibility")]
                    public int visibility;

                    [JsonProperty("banned")]
                    public int banned;

                    [JsonProperty("ban_reason")]
                    public string ban_reason;

                    [JsonProperty("subscriptions")]
                    public int subscriptions;

                    [JsonProperty("favorited")]
                    public int favorited;

                    [JsonProperty("lifetime_subscriptions")]
                    public int lifetime_subscriptions;

                    [JsonProperty("lifetime_favorited")]
                    public int lifetime_favorited;

                    [JsonProperty("views")]
                    public int views;

                    [JsonProperty("tags")]
                    public List<Tag> tags;
                    public class Tag
                    {
                        [JsonProperty("tag")]
                        public string tag;
                    }
                }
            }
        }
        #endregion
        public string GetItemImage(string shortname, ulong skinID = 0)
        {
            if (skinID > 0)
            {
                if (ImageLibrary.Call<bool>("HasImage", shortname, skinID) == false && ImageLibrary.Call<Dictionary<string, object>>("GetSkinInfo", shortname, skinID) == null)
                {
                    if (config.SteamWebApiKey == null || config.SteamWebApiKey == string.Empty || config.SteamWebApiKey.Length != 32)
                    {
                        PrintError($"Steam Web API key not set! Check the configuration!");
                        return ImageLibrary.Call<string>("GetImage", shortname);
                    }
                    else
                    {
                        webrequest.Enqueue("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/", $"key={config.SteamWebApiKey}&itemcount=1&publishedfileids%5B0%5D={skinID}", (code, response) =>
                        {
                            if (code != 200 || response == null)
                            {
                                PrintError($"Image failed to download! Code HTTP error: {code} - Image Name: {shortname} - Image skinID: {skinID} - Response: {response}");
                                return;
                            }

                            SteampoweredResult sr = JsonConvert.DeserializeObject<SteampoweredResult>(response);
                            if (sr == null || !(sr is SteampoweredResult) || sr.response.result == 0 || sr.response.resultcount == 0)
                            {
                                PrintError($"Image failed to download! Error: Parse JSON response - Image Name: {shortname} - Image skinID: {skinID} - Response: {response}");
                                return;
                            }

                            foreach (SteampoweredResult.Response.PublishedFiled publishedfiled in sr.response.publishedfiledetails)
                            {
                                ImageLibrary.Call("AddImage", publishedfiled.preview_url, shortname, skinID);
                            }

                        }, this, RequestMethod.POST);

                        return ImageLibrary.Call<string>("GetImage", "LOADING");
                    }
                }
            }

            return ImageLibrary.Call<string>("GetImage", shortname, skinID);
        }

        #endregion
    }


    namespace SignArtistClasses
    {
        /// <summary>
        /// Extension class with extension methods used by the <see cref="SignArtist"/> plugin.
        /// </summary>
        public static class Extensions
        {
            /// <summary>
            /// Resizes an image from the <see cref="byte"/> array to a new image with a specific width and height.
            /// </summary>
            /// <param name="bytes">Source image. </param>
            /// <param name="width">New image canvas width. </param>
            /// <param name="height">New image canvas height. </param>
            /// <param name="targetWidth">New image width. </param>
            /// <param name="targetHeight">New image height. </param>
            /// <param name="enforceJpeg"><see cref="bool"/> value, true to save the images as JPG, false for PNG. </param>
            public static byte[] ResizeImage(this byte[] bytes, int width, int height, int targetWidth, int targetHeight, bool enforceJpeg)
            {
                byte[] resizedImageBytes;

                using (MemoryStream originalBytesStream = new MemoryStream(), resizedBytesStream = new MemoryStream())
                {
                    // Write the downloaded image bytes array to the memorystream and create a new Bitmap from it.
                    originalBytesStream.Write(bytes, 0, bytes.Length);
                    Bitmap image = new Bitmap(originalBytesStream);

                    // Check if the width and height match, if they don't we will have to resize this image.
                    if (image.Width != width || image.Height != height)
                    {
                        // Create a new Bitmap with the target size.
                        Bitmap resizedImage = new Bitmap(width, height);

                        // Draw the original image onto the new image and resize it accordingly.
                        using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(resizedImage))
                        {
                            graphics.DrawImage(image, new Rectangle(0, 0, targetWidth, targetHeight));
                        }

                        // Save the bitmap to a MemoryStream as either Jpeg or Png.
                        if (enforceJpeg)
                        {
                            resizedImage.Save(resizedBytesStream, ImageFormat.Jpeg);
                        }
                        else
                        {
                            resizedImage.Save(resizedBytesStream, ImageFormat.Png);
                        }

                        // Grab the bytes array from the new image's MemoryStream and dispose of the resized image Bitmap.
                        resizedImageBytes = resizedBytesStream.ToArray();
                        resizedImage.Dispose();
                    }
                    else
                    {
                        // The image has the correct size so we can just return the original bytes without doing any resizing.
                        resizedImageBytes = bytes;
                    }

                    // Dispose of the original image Bitmap.
                    image.Dispose();
                }

                // Return the bytes array.
                return resizedImageBytes;
            }

            /// <summary>
            /// Converts a string to its escaped representation for the image placeholder text value.
            /// </summary>
            /// <param name="stringToEscape">The string to escape.</param>
            public static string EscapeForUrl(this string stringToEscape)
            {
                // Escape initial values.
                stringToEscape = Uri.EscapeDataString(stringToEscape);

                // Convert \r\n, \r and \n into linebreaks.
                stringToEscape = stringToEscape.Replace("%5Cr%5Cn", "%5Cn").Replace("%5Cr", "%5Cn").Replace("%5Cn", "%0A");

                // Return the converted message
                return stringToEscape;
            }
        }
    }
}