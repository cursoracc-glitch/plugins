using System;
using System.Collections.Generic;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("IQHeadReward", "SkuliDropek", "1.0.9")]
    [Description("Reward your heads")]
    class IQHeadReward : RustPlugin
    {
        /// <summary>
        /// Обновление 1.0.х
        /// - Переписал метод проверки на кланы
        /// - Добавлена возможность включения и отключения маркера для игрока и ящика с наградой отдельно!
        /// - Убрал лог загрузки изображений в консоль от API
        /// </summary>

        #region Vars
        private const Boolean LanguageRu = true;

        public const String PermissionImmunitete = "iqheadreward.invise";
        public Dictionary<BaseEntity, CustomMapMarker> MapMarkers = new Dictionary<BaseEntity, CustomMapMarker>();
        static Double CurrentTime() => Facepunch.Math.Epoch.Current;
        public enum TypeReward
        {
            Item,
            Command,
            IQEconomics
        }
        public enum TypeTask
        {
            New,
            Retry
        }

        String PrefabStash = "assets/prefabs/deployable/small stash/small_stash_deployed.prefab";
        String PrefabBarricade = "assets/prefabs/deployable/door barricades/door_barricade_dbl_a.prefab";

        #endregion

        #region Reference
        [PluginReference] Plugin ImageLibrary, IQChat, IQEconomic, IQPlagueSkill, Friends, Clans, Battles, Duel, Duelist, IQRankSystem;

        #region ImageLibrary
        private String GetImage(String fileName, UInt64 skin = 0)
        {
            var imageId = (String)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return String.Empty;
        }
        public Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public void SendImage(BasePlayer player, String imageName, UInt64 imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);
        public Boolean HasImage(String imageName) => (Boolean)ImageLibrary?.Call("HasImage", imageName);
        #endregion

        #region Friends, Clans , Duel
        public Boolean IsFriends(UInt64 userID, UInt64 targetID)
        {
            if (Friends)
                return (Boolean)Friends?.Call("HasFriend", userID, targetID);
            else return false;
        }
        private bool IsClans(String userID, String targetID)
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

        public Boolean IsDuel(UInt64 userID)
        {
            if (Battles)
                return (Boolean)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel) return (Boolean)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else if (Duelist) return (Boolean)Duelist?.Call("inEvent", BasePlayer.FindByID(userID));
            else return false;
        }

        private Dictionary<UInt64, List<UInt64>> WantedTeams = new Dictionary<UInt64, List<UInt64>>();
        private Boolean IsTeamController(BasePlayer Wanted) => WantedTeams.ContainsKey(Wanted.Team.teamID) && config.Setting.AntiAbuse;
        private void TeamControllerAdd(BasePlayer Wanted)
        {
            if (Wanted == null && !config.Setting.AntiAbuse) return;
            RelationshipManager.PlayerTeam Team = Wanted.Team;
            if (Team == null) return;
            if (!IsTeamController(Wanted))
                WantedTeams.Add(Team.teamID, Team.members);
            else WantedTeams[Team.teamID] = WantedTeams[Team.teamID].Concat(Team.members).ToList();
        }
        private void TeamControllerRemove(BasePlayer Wanted)
        {
            if (Wanted == null || !config.Setting.AntiAbuse) return;
            RelationshipManager.PlayerTeam Team = Wanted.Team;
            if (IsTeamController(Wanted))
                WantedTeams.Remove(Team.teamID);
        }
        #endregion

        #region IQEconomics
        public String IQEcoMoney => (String)IQEconomic?.Call("API_GET_MONEY_IL");
        public void IQEconomicSetBalance(UInt64 userID, Int32 Balance) => IQEconomic?.Call("API_SET_BALANCE", userID, Balance);
        Int32 IQEconomicGetBalance(UInt64 userID) => IQEconomic == null ? 0 : (Int32)IQEconomic.Call("API_GET_BALANCE", userID);
        void IQEconomicRemoveBalance(UInt64 userID, Int32 Balance) => IQEconomic?.Call("API_REMOVE_BALANCE", userID, Balance);
        #endregion

        #region IQPlagueSkill
        public Boolean IQPlagueSkillUse(BasePlayer player) => (Boolean)IQPlagueSkill.Call("API_HEAD_REWARD_SKILL", player);
        #endregion

        #region IQChat
        public void SendChat(BasePlayer player, String Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.Setting.ChatSetting;
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion
        
        #region RaidBlocked
        public Boolean IsRaidBlocked(BasePlayer player)
        {
            var ret = Interface.Call("CanTeleport", player) as String;
            if (ret != null)
                return true;
            else return false;
        }
        #endregion

        #region IQRankSystem
        Boolean IsRank(UInt64 userID, String Key)
        {
            if (!IQRankSystem) return false;
            return (Boolean)IQRankSystem?.Call("API_GET_AVAILABILITY_RANK_USER", userID, Key);
        }
        String GetRankName(String Key)
        {
            String Rank = string.Empty;
            if (!IQRankSystem) return Rank;
            return (String)IQRankSystem?.Call("API_GET_RANK_NAME", Key);
        }
        #endregion

        #endregion

        #region Data
        private Dictionary<UInt64, HeadTask> PrePlayerHeads = new Dictionary<UInt64, HeadTask>();
        private Dictionary<UInt64, List<Configuration.Settings.ItemList>> DistributionPlayerReturned = new Dictionary<UInt64, List<Configuration.Settings.ItemList>>();
        [JsonProperty(LanguageRu ? "Задания на головы"  :  "Tasks for heads")]
        private List<HeadTask> PlayerHeads = new List<HeadTask>();
        public class HeadTask
        {
            public String WantedName;
            public UInt64 WantedID;
            public Double Cooldown;
            public Dictionary<UInt64, List<Configuration.Settings.ItemList>> RewardList;
            public Double CooldownItem;
        }
        void ReadData()
        {
            PlayerHeads = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<List<HeadTask>>("IQSystem/IQHeadReward/PlayerHeads");
            WantedTeams = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, List<UInt64>>>("IQSystem/IQHeadReward/PlayersHeadsTeam");
            DistributionPlayerReturned = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, List<Configuration.Settings.ItemList>>>("IQSystem/IQHeadReward/DistributionPlayerReturned");
        }
        void WriteData()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQHeadReward/PlayerHeads", PlayerHeads);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQHeadReward/PlayersHeadsTeam", WantedTeams);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQHeadReward/DistributionPlayerReturned", DistributionPlayerReturned);
        }

        #endregion

        #region Configuration

        private static Configuration config = new Configuration();

        public class Configuration
        {
            [JsonProperty("Настройка плагина | Settings plugin")]
            public Settings Setting = new Settings();

            internal class Settings
            {
                [JsonProperty("Настройки IQChat | Settings IQChat")]
                public ChatSettings ChatSetting = new ChatSettings();
                [JsonProperty("Настройки IQRankSystem | Settings IQRankSystem")]
                public IQRankSystem IQRankSystemSetting = new IQRankSystem();
                [JsonProperty("Настройки UI уведомления | Notification UI Settings")]
                public AlertUI AlertUISetting = new AlertUI();
                [JsonProperty("Настройка создания объявления за голову игроками | Setting up the creation of ads for the head of players")]
                public CustomCreated CustomCreatedSetting = new CustomCreated();
                [JsonProperty("Настройка метки на карте для игрока | Setting up a placemark on the map for the player")]
                public MapMark mapMark = new MapMark();

                internal class IQRankSystem
                {
                    [JsonProperty("Использовать IQRankSystem [true-да/false-нет] (При наличии ранга игрок сможет создавать объявления на голову игрока) | Use IQRankSystem [true-yes/false-no] (If there is a rank, the player will be able to create ads on the player's head)")]
                    public Boolean UseRank = false;
                    [JsonProperty("Впишите ключ ранга, который требуется для разблокировки возможности | Enter the rank key that is required to unlock the feature")]
                    public String Rank = "";
                }
                internal class MapMark
                {
                    [JsonProperty("Включить отображение игрока на G карте | Enable the display of the player on the G map")]
                    public Boolean UseMark;
                    [JsonProperty("Включить отображение ящиков с наградой для игрока на G карте | Enable display of reward boxes for the player on the G map")]
                    public Boolean UseMarkCrates;
                    [JsonProperty("Радиус отображения метки на карте | The radius of the placemark display on the map")]
                    public Single Radius;
                    [JsonProperty("Время обновления метки (В секундах) | Time to update the placemark (In seconds)")]
                    public Single RefreshRate;
                    [JsonProperty("Название метки (%NAME% - выведет имя игрока) | Marker name (%NAME% - displays the player's name)")]
                    public String DisplayName;
                    [JsonProperty("HEX цвет метки | HEX Marker color")]
                    public String Color;
                    [JsonProperty("HEX цвет обводки метки | HEX color of the marker outline")]
                    public String OutLineColor;
                }

                [JsonProperty("Включить внутреннюю защиту от попытки абуза со своей тимой | Enable internal protection against an attempt to abuse with your team")]
                public Boolean AntiAbuse;
                [JsonProperty("Автоматический поиск игроков(true - включен/false - отключен) | Automatic search for players(true-enabled/false-disabled)")]
                public Boolean TurnAutoFilling;
                [JsonProperty("Через сколько искать игроков для задания цели | After how long to search for players to set a goal")]
                public Int32 TimeFinding;
                [JsonProperty("Оповещать всех игроков о том,что появилась новая награда за голову(ture - да/false - нет) | Notify all players that a new head reward has appeared(ture-yes/false-no)")]
                public Boolean UseAlertHead;
                [JsonProperty("Максимальное количество наград за голову(Исходя из списка наград,будет выбираться рандомное количество) | The maximum number of awards per head(Based on the list of awards, a random number will be selected)")]
                public Int32 MaxReward;
                [JsonProperty("Настройка наград за голову(Рандомно будет выбираться N количество) | Setting up head rewards(N numbers will be randomly selected)")]
                public List<ItemList> itemLists;
                public class ItemList
                {
                    [JsonProperty("Тип награды(от этого зависит,что будут выдавать) : 0 - Предмет , 1 - Команда, 2 - IQEconomic | The type of reward (it depends on what will be given): 0-Item, 1-Command, 2-IQEconomic")]
                    public TypeReward TypeRewards;
                    [JsonProperty("Команда(%STEAMID% - замениться на ID игрока) | Command (%STEAMID% - change to player ID)")]
                    public String Command;
                    [JsonProperty("Отображаемое имя | Display name")]
                    public String DisplayName;
                    [JsonProperty("Shortname")]
                    public String Shortname;
                    [JsonProperty("SkinID")]
                    public UInt64 SkinID;
                    [JsonProperty("Минимальное количество | Minimal amount")]
                    public Int32 AmountMin;
                    [JsonProperty("Максимальное количество | Maximum amount")]
                    public Int32 AmountMax;
                    [JsonProperty("Настройки IQEconomic | Setting IQEconomic")]
                    public IQEconomics IQEconomic = new IQEconomics();
                    internal class IQEconomics
                    {
                        [JsonProperty("IQEconomic : Минимальный баланс | IQEconomic : Minimal balance")]
                        public Int32 MinBalance;
                        [JsonProperty("IQEconomic : Максимальный баланс | IQEconomic : Maximum balance")]
                        public Int32 MaxBalance;
                    }
                }
                internal class ChatSettings
                {
                    [JsonProperty("IQChat : Кастомный префикс в чате | IQChat : Custom prefix in the chat")]
                    public String CustomPrefix;
                    [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется) | IQChat : Custom avatar in the chat(If required)")]
                    public String CustomAvatar;
                }
                internal class AlertUI
                {
                    [JsonProperty("Использовать UI уведомление о том,что за голову игрока назначена награда | Use the UI notification that a reward is assigned for the player's head")]
                    public Boolean UseAlertUI;
                    [JsonProperty("Возможность скрывать игрокам UI уведомление о том,что за голову назначена награда (по клику на уведомление) (true - да/false - нет) | The ability to hide the UI notification to players that a reward is assigned for the head (by clicking on the notification) (true-yes/false-no)")]
                    public Boolean UserCloseUI;
                    [JsonProperty("Ссылка на PNG | Link PNG (En Image - https://i.imgur.com/S4NxpUa.png)")]
                    public String PNG;
                    [JsonProperty("AnchorMin")]
                    public String AnchorMin;
                    [JsonProperty("AnchorMax")]
                    public String AnchorMax;
                    [JsonProperty("OffsetMin")]
                    public String OffsetMin;
                    [JsonProperty("OffsetMax")]
                    public String OffsetMax;
                }

                internal class CustomCreated
                {
                    [JsonProperty("Настройка возврата предметов при предсоздании награды за голову(не объявив ее) | Setting up the return of items when pre-creating a head reward(without declaring it)")]
                    public ReturnPreCreated ReturnCreated = new ReturnPreCreated();
                    [JsonProperty("Разрешить создавать игрокам использовать команду /ih во время рейдблока(true - да/false - нет) | Allow players to create and use the /ih command during a raid block(true-yes/false-no)")]
                    public Boolean UseCommandBlock = false;
                    [JsonProperty("Разрешить создавать игрокам награды за голову | Allow players to create rewards for their heads")]
                    public Boolean UseCustomReward = false;
                    [JsonProperty("Разрешить игрокам выбирать время из списка иначе будет устанавливаться из конфига по умолчанию | Allow players to choose the time from the list otherwise it will be set from the default config")]
                    public Boolean UseCutomTime = false;
                    [JsonProperty("Максимальное количество предметов в качестве награды от одного игрока | The maximum number of items as a reward from one player")]
                    public Int32 MaximumRewardCountUser = 10;
                    [JsonProperty("Настройки времени | Time Settings")]
                    public TimeSettings TimeSetting = new TimeSettings();

                    internal class ReturnPreCreated
                    {
                        [JsonProperty("Возвращать предметы игрокам через время, которые они вложили в предсоздания объявления (Не создав его до конца) | Return items to players after the time that they have invested in the pre-creation of the ad (Without creating it until the end)")]
                        public Boolean UseReturnTimer = true;
                        [JsonProperty("Время через которое будет возврат | The time after which the refund will be made")]
                        public Int32 Time = 60;
                    }
                    internal class TimeSettings
                    {
                        [JsonProperty("Время по умолчанию(секунды) | Default time(seconds)")]
                        public Int32 TimeDefault = 600;

                        [JsonProperty("Настраиваемый список для выбора времени игркоками | Customizable list for selecting the time of playcocks")]
                        public List<TimeCreatedMore> CustomTimeSettings = new List<TimeCreatedMore>();
                        internal class TimeCreatedMore
                        {
                            [JsonProperty("Использовать поддержку IQEconomic | Use IQEconomic support")]
                            public Boolean IQEconomic_CustomTime = false;
                            [JsonProperty("Время(секунды) | Time(seconds)")]
                            public Int32 Time = 0;
                            [JsonProperty("Цена за установку данного времени, должна быть поддержка IQEconomic | The price for the installation of this time, there must be support for IQEconomic")]
                            public Int32 IQEconomic_Balance = 0;
                        }
                    }
                }
            }

        public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    Setting = new Settings
                    {
                        TurnAutoFilling = true,
                        AntiAbuse = true,
                        TimeFinding = 600,
                        UseAlertHead = true,
                        MaxReward = 3,
                        IQRankSystemSetting = new Settings.IQRankSystem
                        {
                            Rank = "",
                            UseRank = false,
                        },
                        mapMark = new Settings.MapMark
                        {
                          Color = "#ff4948",
                          OutLineColor = "#333336",
                          DisplayName = "%NAME%",
                          Radius = 0.65f,
                          RefreshRate = 0.2f,
                          UseMark = true,
                          UseMarkCrates = true,
                        },
                        CustomCreatedSetting = new Settings.CustomCreated
                        {
                            UseCommandBlock = false,
                            UseCutomTime = true,
                            UseCustomReward = true,
                            MaximumRewardCountUser = 10,
                            ReturnCreated = new Settings.CustomCreated.ReturnPreCreated
                            {
                                Time = 60,
                                UseReturnTimer = true
                            },
                            TimeSetting = new Settings.CustomCreated.TimeSettings
                            {
                                TimeDefault = 600,
                                CustomTimeSettings = new List<Settings.CustomCreated.TimeSettings.TimeCreatedMore>
                                {
                                    new Settings.CustomCreated.TimeSettings.TimeCreatedMore
                                    {
                                        Time = 2500,
                                        IQEconomic_Balance = 230,
                                        IQEconomic_CustomTime = true,
                                    },
                                    new Settings.CustomCreated.TimeSettings.TimeCreatedMore
                                    {
                                        Time = 3000,
                                        IQEconomic_Balance = 250,
                                        IQEconomic_CustomTime = true,
                                    },
                                    new Settings.CustomCreated.TimeSettings.TimeCreatedMore
                                    {
                                        Time = 1000,
                                        IQEconomic_Balance = 250,
                                        IQEconomic_CustomTime = false,
                                    },
                                }
                            }
                        },
                        itemLists = new List<Settings.ItemList> 
                        {
                            new Settings.ItemList
                            {
                                TypeRewards = TypeReward.Item,
                                Command = "",
                                DisplayName = "Kalash",
                                Shortname = "rifle.ak",
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinID = 0,
                                IQEconomic = new Settings.ItemList.IQEconomics
                                {
                                    MinBalance = 1,
                                    MaxBalance = 10
                                },
                            },
                            new Settings.ItemList
                            {
                                TypeRewards = TypeReward.Item,
                                Command = "",
                                DisplayName = "",
                                Shortname = "rifle.ak",
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinID = 0,
                                IQEconomic = new Settings.ItemList.IQEconomics
                                {
                                    MinBalance = 1,
                                    MaxBalance = 10
                                },
                            },
                            new Settings.ItemList
                            {
                                TypeRewards = TypeReward.Item,
                                Command = "",
                                DisplayName = "",
                                Shortname = "wood",
                                AmountMin = 3000,
                                AmountMax = 6000,
                                SkinID = 0,
                                IQEconomic = new Settings.ItemList.IQEconomics
                                {
                                    MinBalance = 1,
                                    MaxBalance = 10
                                },
                            },
                            new Settings.ItemList
                            {
                                TypeRewards = TypeReward.Item,
                                Command = "",
                                DisplayName = "",
                                Shortname = "metal.fragments",
                                AmountMin = 100,
                                AmountMax = 2000,
                                SkinID = 0,
                                IQEconomic = new Settings.ItemList.IQEconomics
                                {
                                    MinBalance = 1,
                                    MaxBalance = 10
                                },
                            },
                            new Settings.ItemList
                            {
                                TypeRewards = TypeReward.Item,
                                Command = "",
                                DisplayName = "",
                                Shortname = "skull.human",
                                AmountMin = 1,
                                AmountMax = 10,
                                SkinID = 0,
                                IQEconomic = new Settings.ItemList.IQEconomics
                                {
                                    MinBalance = 1,
                                    MaxBalance = 10
                                },
                            },
                            new Settings.ItemList
                            {
                                TypeRewards = TypeReward.Item,
                                Command = "",
                                DisplayName = "",
                                Shortname = "scrap",
                                AmountMin = 111,
                                AmountMax = 2222,
                                SkinID = 0,
                                IQEconomic = new Settings.ItemList.IQEconomics
                                {
                                    MinBalance = 1,
                                    MaxBalance = 10
                                },
                            },
                            new Settings.ItemList
                            {
                                TypeRewards = TypeReward.Item,
                                Command = "",
                                DisplayName = "",
                                Shortname = "skull.wolf",
                                AmountMin = 1,
                                AmountMax = 15,
                                SkinID = 0,
                                IQEconomic = new Settings.ItemList.IQEconomics
                                {
                                    MinBalance = 1,
                                    MaxBalance = 10
                                },
                            },
                            new Settings.ItemList
                            {
                                TypeRewards = TypeReward.Item,
                                Command = "",
                                DisplayName = "",
                                Shortname = "sulfur",
                                AmountMin = 1333,
                                AmountMax = 1532,
                                SkinID = 0,
                                IQEconomic = new Settings.ItemList.IQEconomics
                                {
                                    MinBalance = 1,
                                    MaxBalance = 10
                                },
                            },
                            new Settings.ItemList
                            {
                                TypeRewards = TypeReward.Command,
                                Command = "say %STEAMID%",
                                DisplayName = "",
                                Shortname = "",
                                AmountMin = 0,
                                AmountMax = 0,
                                SkinID = 0,
                                IQEconomic = new Settings.ItemList.IQEconomics
                                {
                                    MinBalance = 1,
                                    MaxBalance = 10
                                },
                            },
                        },
                        ChatSetting = new Settings.ChatSettings
                        {
                            CustomAvatar = "",
                            CustomPrefix = "[<color=#CC3226>IQHeadReward</color>]\n"
                        },
                        AlertUISetting = new Settings.AlertUI
                        {
                            UseAlertUI = true,
                            UserCloseUI = false,
                            PNG = "https://i.imgur.com/QVatu3D.png",
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "0 -68.056",
                            OffsetMax = "273.878 0"
                        },
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
                PrintWarning("Error #87" + $"configuration readings 'oxide/config/{Name}', creating a new configuration! #45");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        
        #endregion

        #region Metods

        private void FillingPlayer() 
        {
            BasePlayer RandomPlayer = BasePlayer.activePlayerList.Where(p => PlayerHeads.Count(x => p.userID == x.WantedID) == 0 && !p.IsDead() && !permission.UserHasPermission(p.UserIDString, PermissionImmunitete)).ToList().GetRandom();
            if (RandomPlayer == null) return;
            List<Configuration.Settings.ItemList> RewardList = config.Setting.itemLists;
            Int32 MaxReward = RewardList.Count <= config.Setting.MaxReward ? RewardList.Count : config.Setting.MaxReward;
            if (IQPlagueSkill && IQPlagueSkillUse(RandomPlayer)) return;

            HeadTask headTask = new HeadTask();
            headTask.Cooldown = config.Setting.CustomCreatedSetting.TimeSetting.TimeDefault;
            headTask.WantedID = RandomPlayer.userID;
            headTask.WantedName = RandomPlayer.displayName;
            UInt64 RandomID = UInt64.Parse(UnityEngine.Random.Range(0, 99999).ToString());
            if (headTask.RewardList == null)
                headTask.RewardList = new Dictionary<ulong, List<Configuration.Settings.ItemList>>();
                headTask.RewardList[RandomID] = GetRandomReward(RewardList, MaxReward);

            PlayerHeads.Add(headTask);

            if (config.Setting.UseAlertHead)
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    SendChat(player, GetLang("NEW_HEAD", player.UserIDString, RandomPlayer.displayName));
            if (config.Setting.AlertUISetting.UseAlertUI)
                AlertUI(RandomPlayer);

            PlayerHeadAlert(RandomPlayer.userID, TypeTask.New);
            TeamControllerAdd(RandomPlayer);
            NextTick(() => { CreateMarker(RandomPlayer); });
        }

        #region Spawn Position Reward
        private int maxTry = 250000;

        private List<Vector3>[] patternPositionsAboveWater = new List<Vector3>[5];
        private List<Vector3>[] patternPositionsUnderWater = new List<Vector3>[5];
        private List<Vector3> busyPoints3D = new List<Vector3>();

        private readonly Quaternion[] directions =
        {
            Quaternion.Euler(90, 0, 0),
            Quaternion.Euler(0, 0, 90),
            Quaternion.Euler(0, 0, 180)
        };

        private void FillPatterns()
        {
            Vector3[] startPositions = { new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(-1, 0, -1), new Vector3(1, 0, -1) };

            patternPositionsAboveWater[0] = new List<Vector3> { new Vector3(0, -0.6f, 0) };
            for (int loop = 1; loop < 5; loop++)
            {
                patternPositionsAboveWater[loop] = new List<Vector3>();

                for (int step = 0; step < loop * 2; step++)
                {
                    for (int pos = 0; pos < 4; pos++)
                    {
                        Vector3 sPos = startPositions[pos] * step;
                        for (int rot = 0; rot < 3; rot++)
                        {
                            Vector3 rPos = directions[rot] * sPos;
                            rPos.y = -0.6f;
                            patternPositionsAboveWater[loop].Add(rPos);
                        }
                    }
                }
            }

            for (int i = 0; i < patternPositionsAboveWater.Length; i++)
            {
                patternPositionsUnderWater[i] = new List<Vector3>();
                foreach (var vPos in patternPositionsAboveWater[i])
                {
                    var rPos = new Vector3(vPos.x, -0.6f, vPos.z);
                    patternPositionsUnderWater[i].Add(rPos);
                }
            }
        }
        public bool IsFlat(ref Vector3 position)
        {
            List<Vector3>[] AboveWater = new List<Vector3>[5];

            Array.Copy(patternPositionsAboveWater, AboveWater, patternPositionsAboveWater.Length);

            for (int i = 0; i < AboveWater.Length; i++)
            {
                for (int j = 0; j < AboveWater[i].Count; j++)
                {
                    Vector3 pPos = AboveWater[i][j];
                    Vector3 resultAbovePos = new Vector3(pPos.x + position.x, position.y + 0.6f, pPos.z + position.z);
                    Vector3 resultUnderPos = new Vector3(pPos.x + position.x, position.y - 0.6f, pPos.z + position.z);

                    if (resultAbovePos.y >= TerrainMeta.HeightMap.GetHeight(resultAbovePos) && resultUnderPos.y <= TerrainMeta.HeightMap.GetHeight(resultUnderPos))
                    {
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        public bool IsDistancePoint(Vector3 point)
        {
            bool result = busyPoints3D.Count(x => Vector3.Distance(point, x) < 20f) == 0;
            return result;
        }
        private void GenerateSpawnPoints()
        {
            for (int i = 0; i < 100; i++)
            {
                maxTry -= 1;
                Vector3 point3D = new Vector3();
                Vector2 point2D = new Vector3(UnityEngine.Random.Range(-TerrainMeta.Size.x / 2, TerrainMeta.Size.x / 2), UnityEngine.Random.Range(-TerrainMeta.Size.z / 2, TerrainMeta.Size.z / 2));

                point3D.x = point2D.x;
                point3D.z = point2D.y;
                point3D.y = TerrainMeta.HeightMap.GetHeight(point3D);

                if (!IsFlat(ref point3D))
                    continue;

                if (!Is3DPointValid(ref point3D))
                    continue;

                if (!IsDistancePoint(point3D))
                    continue;

                if (point3D != Vector3.zero)
                {
                    AcceptValue(ref point3D);
                }
            }
            if (maxTry > 0)
            {
                NextTick(() =>
                {
                    GenerateSpawnPoints();
                });
            }
            else
            {
                PrintWarning(LanguageRu ? $"{busyPoints3D.Count} точек сгенерированно!"  :  $"{busyPoints3D.Count} points generated!");
                maxTry = 250000;
            }
        }
        private bool Is3DPointValid(ref Vector3 point)
        {
            List<BuildingPrivlidge> cupboards = new List<BuildingPrivlidge>();
            Vis.Entities(point, 5, cupboards);
            if (UnityEngine.Physics.CheckSphere(point, 5, LayerMask.GetMask("Construction", "Default", "Deployed", "World", "Trigger", "Prevent Building")) || cupboards.Count > 0 || point.y < ConVar.Env.oceanlevel + 4f)
            {
                return false;
            }
            return true;
        }

        private void AcceptValue(ref Vector3 point)
        {
            busyPoints3D.Add(point);
        }

        #endregion

        #region Spawn Reward

        private void RewardCreated(List<Configuration.Settings.ItemList> Rewards, BasePlayer Killer)
        {
            if (busyPoints3D == null) return;

            foreach (Configuration.Settings.ItemList Reward in Rewards.Where(c => c.TypeRewards != TypeReward.Item))
            {
                switch (Reward.TypeRewards)
                {
                    case TypeReward.Command:
                        {
                            rust.RunServerCommand(Reward.Command.Replace("%STEAMID%", Killer.UserIDString));
                            break;
                        }
                    case TypeReward.IQEconomics:
                        {
                            IQEconomicSetBalance(Killer.userID, UnityEngine.Random.Range(Reward.IQEconomic.MinBalance, Reward.IQEconomic.MaxBalance));
                            break;
                        }
                }
            }

            Double CrateCount = Math.Ceiling((Double)Rewards.Count(c => c.TypeRewards == TypeReward.Item) / (Double)6);
            for (Int32 Count = 0; Count < CrateCount; Count++)
            {
                Double SkipCount = (Double)Count * 6;

                Vector3 RandomPosition = busyPoints3D.GetRandom();
                CreatedCrate(Rewards.Where(c => c.TypeRewards == TypeReward.Item).Skip((Int32)SkipCount).ToList(), RandomPosition, Killer);
            }

            String CrateMessage = String.Empty;
            if (CrateCount != 0)
                CrateMessage = GetLang("CHAT_ALERT_KILLED_REWARD_ITEM", Killer.UserIDString, CrateCount);

            String OtherMessage = String.Empty;
            if(Rewards.Count(c => c.TypeRewards != TypeReward.Item) != 0)
                OtherMessage = GetLang("CHAT_ALERT_KILLED_REWARD_OTHER", Killer.UserIDString, Rewards.Count(c => c.TypeRewards != TypeReward.Item));

            String AllReward = $"{CrateMessage}\n{OtherMessage}";

            String Message = GetLang("CHAT_ALERT_KILLED_REWARD_PLAYER", Killer.UserIDString, AllReward);
            SendChat(Killer, Message);
        }

        private void CreatedCrate(List<Configuration.Settings.ItemList> Rewards, Vector3 Position, BasePlayer Killer)
        {
            Vector3 SpawnPos = new Vector3 { x = Position.x, y = TerrainMeta.HeightMap.GetHeight(Position), z = Position.z };

            StashContainer Container = (StashContainer)GameManager.server.CreateEntity(PrefabStash, SpawnPos);
            Container.Spawn();

            for (Int32 Item = 0; Item < Rewards.Count; Item++)
            {
                Configuration.Settings.ItemList Reward = Rewards[Item];

                Item ItemCreated = CreatedItem(Reward);
                ItemCreated.MoveToContainer(Container.inventory);
            }

            BaseEntity Barricade = (BaseEntity)GameManager.server.CreateEntity(PrefabBarricade, new Vector3(SpawnPos.x, SpawnPos.y - 0.14f, SpawnPos.z - 1), Quaternion.Euler(new Vector3(90, 0, 0)));
            Barricade.Spawn();

            Container.SetFlag(BaseEntity.Flags.Reserved5, true, true);
            Container.CancelInvoke(Container.Decay);

            CreateMarker(Barricade, Killer);
        }

        private Item CreatedItem(Configuration.Settings.ItemList Reward)
        {
            Item CreateItem = ItemManager.CreateByName(Reward.Shortname, UnityEngine.Random.Range(Reward.AmountMin, Reward.AmountMax), Reward.SkinID);
            if (!String.IsNullOrWhiteSpace(Reward.DisplayName))
                CreateItem.name = Reward.DisplayName;

            return CreateItem;
        }

        #endregion

        private void CheckHeadItemsCooldown()
        {
            if (PrePlayerHeads.Count == 0) return;

            foreach(var Task in PrePlayerHeads)
            {
                if (!Task.Value.RewardList.ContainsKey(Task.Key) || Task.Value.RewardList[Task.Key].Count == 0) continue;
                if(!IsCooldown(Task.Value.CooldownItem))
                {
                    DistributionReturnedItems(Task.Value, true);

                    BasePlayer OwnerPlayer = BasePlayer.FindByID(Task.Key);
                    if (OwnerPlayer == null) return;
                    ReturnedItems(OwnerPlayer, true);
                    NextTick(() => { PrePlayerHeads.Remove(Task.Key); });
                }
            }
        }
        private void CheckHeadsCooldown()
        {
            if (PlayerHeads.Count == 0) return;
               
            for(Int32 HeadCount = 0; HeadCount < PlayerHeads.Count; HeadCount++)
            {
                HeadTask Task = PlayerHeads[HeadCount];
                if(!IsCooldown(Task))
                {
                    foreach (BasePlayer player in BasePlayer.allPlayerList)
                        SendChat(player, GetLang("CHAT_ALERT_COOLDOWNS_FALSE", player.UserIDString, Task.WantedName));
                    DistributionReturnedItems(Task);
                    PlayerHeads.Remove(Task);

                    BasePlayer Wanted = BasePlayer.FindByID(Task.WantedID);
                    if (Wanted != null)
                    {
                        CuiHelper.DestroyUi(Wanted, ALERT_UI);

                        if (MapMarkers.ContainsKey(Wanted))
                            UnityEngine.Object.Destroy(MapMarkers[Wanted]);
                    }
                }
            }
        }

        private Boolean IsCooldown(HeadTask Task) => (Task.Cooldown - CurrentTime()) > 0;
        private Boolean IsCooldown(Double Cooldown) => (Cooldown - CurrentTime()) > 0;
        #endregion

        #region Returned Item

        private void DistributionReturnedItems(HeadTask Task, Boolean IsPreCreated = false)
        {
            foreach (KeyValuePair<UInt64, List<Configuration.Settings.ItemList>> Rewards in Task.RewardList.Where(Rewards => !DistributionPlayerReturned.ContainsKey(Rewards.Key)))
            {
                DistributionPlayerReturned.Add(Rewards.Key, Rewards.Value);

                BasePlayer player = BasePlayer.FindByID(Rewards.Key);
                if (player != null)
                {
                    SendChat(player, GetLang(!IsPreCreated ? "CHAT_ALERT_RETURNED_ITEMS" : "CHAT_ALERT_RETURNED_ITEMS_COOLDOWN_PRE_CREATED", player.UserIDString));
                }
            }
        }

        private void ReturnedItems(BasePlayer player, Boolean IsPreCreated = false)
        {
            if (player == null || !DistributionPlayerReturned.ContainsKey(player.userID)) return;

            foreach(Configuration.Settings.ItemList Item in DistributionPlayerReturned[player.userID])
            {
                Item ItemReturned = CreatedItem(Item);
                player.GiveItem(ItemReturned, BaseEntity.GiveItemReason.Generic);
            }

            DistributionPlayerReturned.Remove(player.userID);
            CuiHelper.DestroyUi(player, "IQHEADREWARD_PANEL");

            if (!IsPreCreated)
                SendChat(player, GetLang("CHAT_ALERT_RETURNED_ITEMS_FINISH", player.UserIDString));
        }

        #endregion

        #region Killed Task

        private void KilledTask(BasePlayer Killer, BasePlayer Wanted)
        {
            if (!PlayerHeads.Exists(h => h.WantedID == Wanted.userID)) return;

            HeadTask Task = PlayerHeads.First(h => h.WantedID == Wanted.userID);
            RewardCreated(Task.RewardList.SelectMany(x => x.Value).ToList(), Killer);

            PlayerHeads.Remove(Task);
            CuiHelper.DestroyUi(Wanted, ALERT_UI);

            if (MapMarkers.ContainsKey(Wanted))
                UnityEngine.Object.Destroy(MapMarkers[Wanted]);
            Interface.Oxide.CallHook("KilledTask", Killer, Wanted);
        }

        #endregion

        #region Created Task

        private void CreatedTask(BasePlayer player)
        {
            if (player == null) return;
            if (!PrePlayerHeads.ContainsKey(player.userID)) return;

            HeadTask Task = PrePlayerHeads[player.userID];
            if (!PlayerHeads.Exists(x => x.WantedID == Task.WantedID))
            {
                PlayerHeadAlert(Task.WantedID, TypeTask.New);
                SendChat(player, GetLang("CHAT_CREATED_TASK_RESULT", player.UserIDString, FormatTime(TimeSpan.FromSeconds(Task.Cooldown), player.UserIDString)));
                Task.Cooldown += CurrentTime();
                PlayerHeads.Add(Task);
                Interface.Oxide.CallHook("TaskCreated", player, Task.WantedID);
            }
            else
            {
                HeadTask ExistsTask = PlayerHeads.FirstOrDefault(x => x.WantedID == Task.WantedID);
                if (ExistsTask == null)
                {
                    PlayerHeadAlert(Task.WantedID, TypeTask.New);
                    SendChat(player, GetLang("CHAT_CREATED_TASK_RESULT", player.UserIDString, FormatTime(TimeSpan.FromSeconds(Task.Cooldown), player.UserIDString)));
                    Task.Cooldown += CurrentTime();
                    PlayerHeads.Add(Task);
                    Interface.Oxide.CallHook("TaskCreated", player, Task.WantedID);
                }
                else
                {
                    if (ExistsTask.RewardList.ContainsKey(player.userID))
                        ExistsTask.RewardList[player.userID] = ExistsTask.RewardList[player.userID].Concat(Task.RewardList[player.userID]).ToList();
                    else
                        ExistsTask.RewardList.Add(player.userID, Task.RewardList[player.userID]);

                    var Time = config.Setting.CustomCreatedSetting.TimeSetting.CustomTimeSettings.FirstOrDefault(t => t.Time == Task.Cooldown);
                    if (Time != null && Time.IQEconomic_CustomTime)
                    {
                        IQEconomicRemoveBalance(player.userID, Time.IQEconomic_Balance);
                        ExistsTask.Cooldown += Time.Time;
                        SendChat(player, GetLang("CHAT_CREATED_TASK_RESULT_MORE_ITEMS_ADD_PLUS_TIME", player.UserIDString, FormatTime(TimeSpan.FromSeconds(ExistsTask.Cooldown - CurrentTime()), player.UserIDString)));
                    }
                    else
                        SendChat(player, GetLang("CHAT_CREATED_TASK_RESULT_MORE_ITEMS_ADD", player.UserIDString));

                    PlayerHeadAlert(Task.WantedID, TypeTask.Retry);
                    Interface.Oxide.CallHook("UpdateTask", player, Task.WantedID);
                }
            }

            BasePlayer Wanted =  BasePlayer.FindByID(Task.WantedID);

                if (Wanted != null)
                    CreateMarker(Wanted);
                else CreateMarker(BasePlayer.allPlayerList.First(b => b.userID == Task.WantedID));

            CuiHelper.DestroyUi(player, "IQHEADREWARD_PANEL");
            PrePlayerHeads.Remove(player.userID);

            if (Wanted != null)
            {
                TeamControllerAdd(Wanted);
                AlertUI(Wanted);

                if (config.Setting.UseAlertHead)
                    foreach (BasePlayer allP in BasePlayer.activePlayerList)
                        SendChat(allP, GetLang("NEW_HEAD", allP.UserIDString, Wanted.displayName));
            }
        }

        private void PlayerHeadAlert(UInt64 WantedID, TypeTask typeTask)
        {
            BasePlayer wanted = BasePlayer.FindByID(WantedID);
            if (wanted == null) return;

            switch(typeTask)
            {
                case TypeTask.New:
                    {
                        if (config.Setting.AlertUISetting.UseAlertUI)
                            AlertUI(wanted);

                        SendChat(wanted, GetLang("CHAT_ALERT_TASK_PLAYER", wanted.UserIDString));
                        break;
                    }
                case TypeTask.Retry:
                    {
                        SendChat(wanted, GetLang("CHAT_ALERT_TASK_PLAYER_RETRY", wanted.UserIDString));
                        break;
                    }
            }
        }

        #endregion

        #region Metods Items

        private enum TypeCreatedTask
        {
            TakePlayer,
            TakeItem,
            TakeTime
        }

        private void LocalCreatedTask(TypeCreatedTask TypeCreatedTask, UInt64 OwnerID, Item ItemTake = null, Int32 Amount = 0, UInt64 WantedID = 0, Int32 Cooldown = 0)
        {
            BasePlayer player = BasePlayer.FindByID(OwnerID);
            if (player == null) return;

            if (!PrePlayerHeads.ContainsKey(OwnerID))
                PrePlayerHeads.Add(OwnerID, new HeadTask { Cooldown = 0, WantedID = 0,  RewardList = new Dictionary<ulong, List<Configuration.Settings.ItemList>> { } });

            var Data = PrePlayerHeads[OwnerID];

            if (!Data.RewardList.ContainsKey(OwnerID))
                Data.RewardList.Add(OwnerID, new List<Configuration.Settings.ItemList> { });

            if (Cooldown != 0 && Data.Cooldown == 0)
                Data.Cooldown = Cooldown;

            switch (TypeCreatedTask)
            {
                case TypeCreatedTask.TakePlayer:
                    {
                        Data.WantedID = WantedID;
                        BasePlayer Wanted = BasePlayer.FindByID(WantedID);
                        if (Wanted != null)
                            Data.WantedName = Wanted.displayName;
                        Take_Reward_Items(player);
                        break;
                    }
                case TypeCreatedTask.TakeItem:
                    {
                        if (Amount <= 0 || Amount > ItemTake.amount) return;
                        Data.RewardList[OwnerID].Add(new Configuration.Settings.ItemList
                        {
                            DisplayName = ItemTake.name,
                            AmountMin = Amount,
                            AmountMax = Amount,
                            Command = String.Empty,
                            IQEconomic = new Configuration.Settings.ItemList.IQEconomics
                            {
                                MaxBalance = 0,
                                MinBalance = 0,
                            },
                            Shortname = ItemTake.info.shortname,
                            SkinID = ItemTake.skin,
                            TypeRewards = TypeReward.Item,
                        });

                        player.inventory.Take(null, ItemTake.info.itemid, Amount);
                        Take_Reward_Items(player);
                        if (config.Setting.CustomCreatedSetting.ReturnCreated.UseReturnTimer)
                            Data.CooldownItem = config.Setting.CustomCreatedSetting.ReturnCreated.Time;
                        break;
                    }
                case TypeCreatedTask.TakeTime:
                    {
                        Data.Cooldown = Cooldown;
                        break;
                    }
            }
            Update_Task(player);
        }

        private void LocalTaskRemoveItem(BasePlayer player, Item ItemTake)
        {
            var Data = PrePlayerHeads[player.userID];
            var DataItem = Data.RewardList[player.userID].FirstOrDefault(x => x.Shortname == ItemTake.info.shortname && x.SkinID == ItemTake.skin && x.AmountMax == ItemTake.amount);
            if (DataItem == null) return;
            Data.RewardList[player.userID].Remove(DataItem);
            player.GiveItem(ItemTake, BaseEntity.GiveItemReason.Generic);
            Take_Reward_Items(player);
            Update_Task(player);
        }

        #endregion

        #region Metods ReHead
       
        private void ReheadPlayer(BasePlayer player, UInt64 WantedID)
        {
            HeadTask ActiveTask = PlayerHeads.FirstOrDefault(h => h.WantedID == WantedID);
            if (ActiveTask == null) return;

            Created_Head_Task(player, true);
            LocalCreatedTask(TypeCreatedTask.TakePlayer, player.userID, WantedID: WantedID);
            Take_Reward_Items(player);
        }

        #endregion

        #region Utilites

        private IEnumerator DownloadImages()
        {
            foreach (ItemDefinition Item in ItemManager.itemList)
            {
                String Shortname = Item.shortname;
                if (Shortname == "rhib" || Shortname == "vehicle.chassis" || Shortname == "vehicle.module") continue;
                String PngKey = $"{Shortname}_64px";
               
                if (!HasImage(PngKey))
                    AddImage($"http://api.skyplugins.ru/api/getimage/{Shortname}/64", PngKey);
            }
            yield return new WaitForSeconds(0.04f);
        }

        void LoadedImage()
        {
            if (!HasImage($"WANTED"))
                AddImage("https://i.imgur.com/5vfDpgD.png", "WANTED");
            if (!HasImage($"WANTED_ITEMS"))
                AddImage("https://i.imgur.com/D3Uv9T7.png", "WANTED_ITEMS");    

            if (config.Setting.AlertUISetting.UseAlertUI)
                if (!HasImage($"ALERT_UI_{config.Setting.AlertUISetting.PNG}"))
                    AddImage(config.Setting.AlertUISetting.PNG, $"ALERT_UI_{config.Setting.AlertUISetting.PNG}");

            ServerMgr.Instance.StartCoroutine(DownloadImages());
        }
        void CahedImages(BasePlayer player)
        {
            SendImage(player, $"WANTED");
            SendImage(player, $"WANTED_ITEMS");

            if (config.Setting.AlertUISetting.UseAlertUI)
                SendImage(player, $"ALERT_UI_{config.Setting.AlertUISetting.PNG}");
        }

        List<Configuration.Settings.ItemList> GetRandomReward(List<Configuration.Settings.ItemList> RewardList, Int32 CountReward)
        {
            List<Configuration.Settings.ItemList> Rewards = RewardList.OrderBy(s => UnityEngine.Random.value).Take(CountReward).ToList();
            return Rewards;
        }
        private Single GetPercentStatus(BasePlayer player, HeadTask Data)
        {
            Boolean TimeCutom = config.Setting.CustomCreatedSetting.UseCutomTime;
            Single ResultPercent = 0f;

            if (Data.WantedID != 0)
                ResultPercent += TimeCutom ? 0.2f : 0.5f;

            if (Data.RewardList.ContainsKey(player.userID) && Data.RewardList[player.userID].Count != 0)
                ResultPercent += TimeCutom ? 0.3f : 0.5f;

            if (TimeCutom && Data.Cooldown != 0)
                ResultPercent += 0.5f;

            return ResultPercent;
        }

        public String FormatTime(TimeSpan time, String UserID)
        {
            String Result = String.Empty;
            String Days = GetLang("TITLE_FORMAT_DAYS", UserID);
            String Hourse = GetLang("TITLE_FORMAT_HOURSE", UserID);
            String Minutes = GetLang("TITLE_FORMAT_MINUTES", UserID);
            String Seconds = GetLang("TITLE_FORMAT_SECONDS", UserID);

            if (time.Seconds != 0)
                Result = $"{Format(time.Seconds, Seconds, Seconds, Seconds)}";

            if (time.Minutes != 0)
                Result = $"{Format(time.Minutes, Minutes, Minutes, Minutes)}";

            if (time.Hours != 0)
                Result = $"{Format(time.Hours, Hourse, Hourse, Hourse)}";

            if (time.Days != 0)
                Result = $"{Format(time.Days, Days, Days, Days)}";

            return Result;
        }

        private String Format(Int32 units, String form1, String form2, String form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units}{form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units}{form2}";

            return $"{units}{form3}";
        }

        #endregion

        #region Hooks
        private void Init() => ReadData();
        private void OnServerInitialized()
        {
            LoadedImage();

            if (config.Setting.TurnAutoFilling)
                timer.Every(config.Setting.TimeFinding, () =>
                {
                    CheckHeadsCooldown();

                    if (PlayerHeads.Count < 3)
                        FillingPlayer();
                });
            if (config.Setting.CustomCreatedSetting.ReturnCreated.UseReturnTimer)
                timer.Every(config.Setting.CustomCreatedSetting.ReturnCreated.Time, () => { CheckHeadItemsCooldown(); });

            foreach (BasePlayer p in BasePlayer.activePlayerList)
                OnPlayerConnected(p);

            if (!permission.PermissionExists(PermissionImmunitete, this))
                permission.RegisterPermission(PermissionImmunitete, this);
            FillPatterns();
            NextTick(() =>
            {
                GenerateSpawnPoints();
            });

            foreach (HeadTask headTask in PlayerHeads)
            {
                BasePlayer Wanted = BasePlayer.allPlayerList.FirstOrDefault(b => b.userID == headTask.WantedID);
                if (Wanted != null)
                    CreateMarker(Wanted);
            }
        }


        void OnPlayerConnected(BasePlayer player)
        {
            if (PlayerHeads.Exists(p => p.WantedID == player.userID))
            {
                AlertUI(player);
                NextTick(() => { CreateMarker(player); });
            }

            CahedImages(player);
        }

        void OnPlayerDeath(BasePlayer Wanted, HitInfo info)
        {
            if (Wanted == null || info == null || Wanted.userID < 2147483647) return;
            BasePlayer Killer = info.InitiatorPlayer;
            if (Killer == null || Wanted.userID == Killer.userID) return;
            KilledTask(Killer, Wanted);
        }

        void Unload()
        {
            foreach(var p in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(p, ALERT_UI);
                CuiHelper.DestroyUi(p, "IQHEADREWARD_PLAYER_PANEL");
                CuiHelper.DestroyUi(p, "IQHEADREWARD_PANEL");
            }
            WriteData();

            RemoveMarkers();

            ServerMgr.Instance.StopCoroutine(DownloadImages());
        }
        #endregion

        #region Command
        [ChatCommand("ih")]
        void ChatCommandHeads(BasePlayer player) => UI_Interface(player);   

        [ChatCommand("ir")]
        void ChatCommandReturned(BasePlayer player) => ReturnedItems(player);

        [ConsoleCommand("ih")]
        void ConsoleCommandHeads(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;

            String Action = args.Args[0]; 
            switch(Action)
            {
                case "created.open.menu": 
                    {
                        Created_Head_Task(player);
                        Take_Reward_Items(player);
                        break;
                    }
                case "rehead.task": 
                    {
                        UInt64 WantedID = UInt64.Parse(args.Args[1]);
                        ReheadPlayer(player, WantedID);
                        break;
                    }
                case "player.panel":
                    {
                        switch (args.Args[1])
                        {
                            case "head.show.itemlist":
                                {
                                    String AnchorMin = args.Args[2];
                                    String AnchorMax = args.Args[3];
                                    Int32 CountUI;
                                    if (!Int32.TryParse(args.Args[4], out CountUI)) return;
                                    List<Configuration.Settings.ItemList> ItemList = PlayerHeads[CountUI].RewardList.SelectMany(x => x.Value).ToList();
                                    if (ItemList == null) return;

                                    ShowItemsPlayerPanelMore(player, AnchorMin, AnchorMax, CountUI, ItemList);
                                    break;
                                }
                            case "head.show.itemlist.return": 
                                {
                                    String AnchorMin = args.Args[2];
                                    String AnchorMax = args.Args[3];
                                    Int32 CountUI;
                                    if (!Int32.TryParse(args.Args[4], out CountUI)) return;
                                    HeadTask Heads = PlayerHeads[CountUI];
                                    if (Heads == null) return;

                                    CuiElementContainer container = new CuiElementContainer();
                                    ShowPlayerPanel(player, container, CountUI, Heads, AnchorMin.Replace("'", ""), AnchorMax.Replace("'", ""));
                                    CuiHelper.AddUi(player, container);
                                    break;
                                }
                        }
                        break;
                    }
                case "input.drop":
                    {
                        Int32 AmountItem = Int32.Parse(args.Args[1]);
                        String Shortname = args.Args[2];
                        UInt64 SkinID = UInt64.Parse(args.Args[3]);
                        Item Item = player.inventory.AllItems().FirstOrDefault(i => i.skin == SkinID && i.info.shortname == Shortname);
                        if (Item == null) return;

                        Show_Amount_Drop_Input(player, Item);
                        break;
                    }
                case "update.task": 
                    {
                        Update_Task(player);
                        break;
                    }
                case "created.task": 
                    {
                        String ActionCreated = args.Args[1];
                        switch(ActionCreated)
                        {
                            case "search.player.task.open":
                                {
                                    Search_Player_Tasked(player);
                                    break;
                                }
                            case "search.player.task.confirm.open":
                                {
                                    if (args.Args.Length != 3) return;
                                    String NickOrID = args.Args[2];
                                    if (String.IsNullOrWhiteSpace(NickOrID)) return;
                                    BasePlayer TaskPlayer = BasePlayer.Find(NickOrID);
                                    if (TaskPlayer == null) return;
                                    if (TaskPlayer == player) return;
                                    Show_Confirm_Player_Tasked(player, TaskPlayer);
                                    break;
                                }
                            case "search.player.task.confirm.yes": 
                                {
                                    UInt64 WantedID = UInt64.Parse(args.Args[2]);
                                    LocalCreatedTask(TypeCreatedTask.TakePlayer, player.userID, WantedID: WantedID);
                                    break;
                                } 
                            case "get.item.amount":
                                {
                                    if (args.Args.Length != 6) return;
                                    String Shortname = args.Args[2];
                                    if (String.IsNullOrWhiteSpace(Shortname)) return;
                                    UInt64 SkinID;
                                    if (!UInt64.TryParse(args.Args[3], out SkinID)) return;
                                    Regex regex = new Regex("^[0-9]+$");
                                    Int32 Amount;
                                    if (!Int32.TryParse(args.Args[4], out Amount)) return;
                                    if (!regex.IsMatch(args.Args[4])) return;
                                    Int32 ItemAmount;
                                    if (!Int32.TryParse(args.Args[5], out ItemAmount)) return;
                                    if (!regex.IsMatch(args.Args[5])) return;
                                    if (ItemAmount > Amount) return;

                                    Item ThisItem = ItemManager.CreateByName(Shortname, ItemAmount, SkinID);
                                    LocalCreatedTask(TypeCreatedTask.TakeItem, player.userID, ThisItem, ItemAmount);
                                    break;
                                }
                            case "remove.item": 
                                {
                                    String Shortname = args.Args[2];
                                    if (String.IsNullOrWhiteSpace(Shortname)) return;
                                    UInt64 SkinID;
                                    if (!UInt64.TryParse(args.Args[3], out SkinID)) return;
                                    Int32 Amount;
                                    if (!Int32.TryParse(args.Args[4], out Amount)) return;
                                    Item ThisItem = ItemManager.CreateByName(Shortname, Amount, SkinID);
                                    LocalTaskRemoveItem(player, ThisItem);
                                    break;
                                }
                            case "turn.time": 
                                {
                                    Int32 Cooldown;
                                    if (!Int32.TryParse(args.Args[2], out Cooldown)) return;
                                    LocalCreatedTask(TypeCreatedTask.TakeTime, player.userID, Cooldown: Cooldown);
                                    break;
                                }
                            case "show.reward.list": 
                                {
                                    List<Configuration.Settings.ItemList> ItemList = PrePlayerHeads[player.userID].RewardList[player.userID];
                                    Show_Item_Reward(player, ItemList);
                                    break;
                                }
                            case "result.created": 
                                {
                                    CreatedTask(player);
                                    break;
                                }
                        }
                        break;
                    }
                    
            }
        }

        [ChatCommand("debugih")]
        void DevDebugCommand(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            FillingPlayer();
        }

        #endregion

        #region UI
        public static string ALERT_UI = "ALERT_UI";

        #region Main UI

        void UI_Interface(BasePlayer player)
        {
            if (config.Setting.CustomCreatedSetting.UseCommandBlock && IsRaidBlocked(player)) return;

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#00000096"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "Overlay", "IQHEADREWARD_PLAYER_PANEL");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = "IQHEADREWARD_PLAYER_PANEL", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "IQHEADREWARD_PLAYER_PANEL", "CLOSE_BTN");
            
            #region Centering
            Int32 Count = 0;
            Int32 ItemCount = 0;
            Single itemMinPosition = 219f;
            Single itemWidth = 0.413646f - 0.1f; 
            Single itemMargin = 0.439895f - 0.42f; 
            Int32 itemCount = PlayerHeads.Count < 3 ? PlayerHeads.Count + 1 : PlayerHeads.Count;
            Single itemMinHeight = 0.09f; 
            Single itemHeight = 0.76f; 
            Int32 ItemTarget = 3;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
            #endregion

            foreach (HeadTask Heads in PlayerHeads)
            {
                ShowPlayerPanel(player, container, Count, Heads, $"{itemMinPosition} {itemMinHeight}", $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}");

                #region Centring
                Count++;
                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 1f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
                #endregion
            }

            if (PlayerHeads.Count < 3)
            {
                container.Add(new CuiElement
                {
                    Name = "CreatedImage",
                    Parent = "IQHEADREWARD_PLAYER_PANEL",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("WANTED_ITEMS")},
                    new CuiRectTransformComponent{ AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                }
                }); 

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = "IQHEADREWARD_PLAYER_PANEL", Command = "ih created.open.menu", Color = "0 0 0 0" },
                    Text = { Text = "" }
                }, "CreatedImage", "CreatedButton");
            }

            container.Add(new CuiElement
            {
                Name = "TitlePanel",
                Parent = "IQHEADREWARD_PLAYER_PANEL",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_MAIN_MENU_IH_TITLE",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, Color = "0.8274511 0.7372549 0.5529412 1"},
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-244.5 298.672", OffsetMax = "241.5 351.665" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "TitleThree",
                Parent = "IQHEADREWARD_PLAYER_PANEL",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_MAIN_MENU_IH_DESCRIPTION_TITLE",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "0.8509805 0.764706 0.5843138 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-433.577 264.726", OffsetMax = "434.764 298.674" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "TitleTwo",
                Parent = "IQHEADREWARD_PLAYER_PANEL",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_MAIN_MENU_IH_DESCRIPTION",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 17, Align = TextAnchor.MiddleCenter, Color = "0.8509805 0.764706 0.5843138 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-615.508 -352.983", OffsetMax = "612.492 -308.685" }
                }
            });

            CuiHelper.DestroyUi(player, "IQHEADREWARD_PLAYER_PANEL");
            CuiHelper.AddUi(player, container);
        }      

        void ShowPlayerPanel(BasePlayer player, CuiElementContainer container, Int32 Count, HeadTask Heads, String AnchorMin, String AnchorMax)
        {
            BasePlayer Wanted = BasePlayer.FindByID(Heads.WantedID);

            CuiHelper.DestroyUi(player, $"HEADS_PANEL_{Count}");
            CuiHelper.DestroyUi(player, $"HEAD_PANEL_ITEM_{Count}");
            container.Add(new CuiElement
            {
                Name = $"HEADS_PANEL_{Count}",
                Parent = "IQHEADREWARD_PLAYER_PANEL",
                Components = {
                    new CuiImageComponent { Color = "0 0 0 0"},
                    new CuiRectTransformComponent{ AnchorMin = AnchorMin, AnchorMax = AnchorMax },
                }
            });

            container.Add(new CuiElement
            { 
                Parent = $"HEADS_PANEL_{Count}",
                Name = "AVATAR_TASK",
                Components =
                    {
                    new CuiRawImageComponent { Png = GetImage(Heads.WantedID.ToString()),Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent { AnchorMin = "0.26 0.32", AnchorMax = "0.74 0.68" }
                    }
            });

            if (Wanted != null)
            {
                container.Add(new CuiElement
                {
                    Name = "AVATAR_TASK_NAME_PLAYER",
                    Parent = "AVATAR_TASK",
                    Components = {
                    new CuiTextComponent { Text = $"<b>{Wanted.displayName}</b>", Font = "robotocondensed-regular.ttf", FontSize = 17, Align = TextAnchor.LowerCenter, Color = "0.8509801408 0.764706 0.5843138 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.05", AnchorMax = "1 1" }
                }
                });
            }

            container.Add(new CuiElement
            {
                Name = "HEADS_IMAGE",
                Parent = $"HEADS_PANEL_{Count}",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("WANTED")},
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1" },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = "IQHEADREWARD_PLAYER_PANEL", Command = $"ih rehead.task {Heads.WantedID}", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, $"HEADS_PANEL_{Count}", "REHEAD_PLAYER");

            ShowItemsPlayerPanel(player, container, Count, Heads, AnchorMin,AnchorMax);
        }

        #region ShowItemsPlayerPanel
        void ShowItemsPlayerPanel(BasePlayer player, CuiElementContainer container, Int32 Count, HeadTask Task, String AnchorMin, String AnchorMax)
        {
            CuiHelper.DestroyUi(player, "REWARDS_PLAYERS");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1 0.12", AnchorMax = "0.9 0.22" },
                Image = { Color = "0 0 0 0" }
            }, $"HEADS_PANEL_{Count}", $"REWARDS_PLAYERS_{Count}");

            List<Configuration.Settings.ItemList> ItemList = Task.RewardList.SelectMany(x => x.Value).ToList();

            #region Centering
            Int32 Count_Items = 0;
            Int32 ItemCount_Items = 0;
            Single itemMinPosition_Items = 219f;
            Single itemWidth_Items = 0.413646f - 0.25f; 
            Single itemMargin_Items = 0.439895f - 0.42f; 
            Int32 itemCount_Items = ItemList.Count;
            Single itemMinHeight_Items = 0f;
            Single itemHeight_Items = 1f; 
            Int32 ItemTarget_Items = 5;

            if (itemCount_Items > ItemTarget_Items)
            {
                itemMinPosition_Items = 0.5f - ItemTarget_Items / 2f * itemWidth_Items - (ItemTarget_Items - 1) / 2f * itemMargin_Items;
                itemCount_Items -= ItemTarget_Items;
            }
            else itemMinPosition_Items = 0.5f - itemCount_Items / 2f * itemWidth_Items - (itemCount_Items - 1) / 2f * itemMargin_Items;
            #endregion

            foreach (var ItemTask in ItemList.Take(5))
            {
                container.Add(new CuiElement
                {
                    Name = $"ITEM_{Count_Items}",
                    Parent = $"REWARDS_PLAYERS_{Count}",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#E1940050") },
                        new CuiRectTransformComponent{ AnchorMin = $"{itemMinPosition_Items} {itemMinHeight_Items}", AnchorMax = $"{itemMinPosition_Items + itemWidth_Items} {itemMinHeight_Items + itemHeight_Items}" },
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 -1", UseGraphicAlpha = true },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = $"ITEM_{Count_Items}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage($"{ItemTask.Shortname}_64px"), Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent{ AnchorMin = $"0.05 0.05", AnchorMax = $"0.95 0.95" },
                    }
                });

                String Amount = $"x{ItemTask.AmountMax}";

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = Amount, FontSize = 10, Color = HexToRustFormat("#180D00FF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerCenter }
                }, $"ITEM_{Count_Items}");

                #region Centring
                Count_Items++;
                ItemCount_Items++;
                itemMinPosition_Items += (itemWidth_Items + itemMargin_Items);
                if (ItemCount_Items % ItemTarget_Items == 0)
                {
                    itemMinHeight_Items -= (itemHeight_Items + (itemMargin_Items * 1f));
                    if (itemCount_Items > ItemTarget_Items)
                    {
                        itemMinPosition_Items = 0.5f - ItemTarget_Items / 2f * itemWidth_Items - (ItemTarget_Items - 1) / 2f * itemMargin_Items;
                        itemCount_Items -= ItemTarget_Items;
                    }
                    else itemMinPosition_Items = 0.5f - itemCount_Items / 2f * itemWidth_Items - (itemCount_Items - 1) / 2f * itemMargin_Items;
                }
                #endregion
            }

            if (ItemList.Count > 5)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat("#00000026"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Command = $"ih player.panel head.show.itemlist '{AnchorMin}' '{AnchorMax}' {Count}" },
                    Text = { Text = GetLang("UI_CREATED_TASK_HEAD_SHOW_MORE_ITEMS", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.LowerCenter, Color = "0.09411766 0.0509804 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, $"REWARDS_PLAYERS_{Count}", $"REWARDS_PLAYERS_ITEM_MORE_{Count}");
            }
        }
        #endregion

        #region ShowItemsPlayerPanelMore

        void ShowItemsPlayerPanelMore(BasePlayer player, String AnchorMin, String AnchorMax, Int32 CountUI, List<Configuration.Settings.ItemList> ItemList)
        {
            CuiHelper.DestroyUi(player, $"HEADS_PANEL_{CountUI}");
            CuiHelper.DestroyUi(player, $"HEAD_PANEL_ITEM_{CountUI}");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = $"HEAD_PANEL_ITEM_{CountUI}",
                Parent = "IQHEADREWARD_PLAYER_PANEL",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("WANTED_ITEMS") },
                    new CuiRectTransformComponent { AnchorMin = AnchorMin.Replace("'", ""), AnchorMax = AnchorMax.Replace("'", "") }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-178.521 -195", OffsetMax = "178.521 133" }
            }, $"HEAD_PANEL_ITEM_{CountUI}", $"PANEL_REWARDS_{CountUI}");

            #region Centering
            Int32 Count = 0;
            Int32 ItemCount = 0;
            Single itemMinPosition = 219f;
            Single itemWidth = 0.413646f - 0.26f; 
            Single itemMargin = 0.439895f - 0.425f; 
            Int32 itemCount = ItemList.Count;
            Single itemMinHeight = 0.83f; 
            Single itemHeight = 0.17f;
            Int32 ItemTarget = 6;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
            #endregion

            foreach (var Item in ItemList.Take(36))
            {
                container.Add(new CuiElement
                {
                    Name = $"ITEM_{Count}",
                    Parent = $"PANEL_REWARDS_{CountUI}",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#E1940050") },
                        new CuiRectTransformComponent{ AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 -1", UseGraphicAlpha = true },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = $"ITEM_{Count}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage($"{Item.Shortname}_64px"), Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent{ AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    }
                });

                String Amount = $"x{Item.AmountMax}";

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = Amount, FontSize = 8, Color = HexToRustFormat("#180D00FF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerCenter }
                }, $"ITEM_{Count}");

                #region Centring
                Count++;
                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 1f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
                #endregion
            }

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = $"ih player.panel head.show.itemlist.return {AnchorMin} {AnchorMax} {CountUI}" },
                Text = { Text = GetLang("UI_CREATED_TASK_HEAD_ITEM_LIST_BACK", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "0.09411766 0.0509804 0 0.9" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-159.371 -292", OffsetMax = "161.179 -190" }
            }, $"HEAD_PANEL_ITEM_{CountUI}", "BtnBack");

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #endregion

        #region Take Head

        void Created_Head_Task(BasePlayer player, Boolean Rehead = false)
        {
            CuiHelper.DestroyUi(player, "IQHEADREWARD_PANEL");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#00000096"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0.008", OffsetMax = "-0.0041408 0" }
            }, "Overlay", "IQHEADREWARD_PANEL");

            container.Add(new CuiElement
            {
                Name = "TitleNameRewardCustom",
                Parent = "IQHEADREWARD_PANEL",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_TITLE",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter, Color = "0.8274511 0.7372549 0.5529 1" },  ///
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "590.68 -65.31201408", OffsetMax = "0.005 -20.368" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = "IQHEADREWARD_PANEL", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "IQHEADREWARD_PANEL", "CLOSE_BTN");

            CuiHelper.AddUi(player, container);
            if (!Rehead)
                Update_Task(player);
        }

        #region Take Reward Items

        void Take_Reward_Items(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "InputAmount" + ".Input");
            CuiHelper.DestroyUi(player, "Title.Reward.Amount");
            CuiHelper.DestroyUi(player, "TakeRewards");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0.005 -292.594", OffsetMax = "595.935 173.53" }
            }, "IQHEADREWARD_PANEL", "TakeRewards");

            container.Add(new CuiElement
            {
                Name = "TextTakeRewards",
                Parent = "TakeRewards",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_TAKE_REWARDS_TITLE", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "0.8274511 0.7372549 0.5529 1" },  //
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-297.924 154", OffsetMax = "298.006 209.46" }
                }
            });

            for (Int32 i = 0, x = 0, y = 0; i < 36; i++)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0.7", Material = "assets/content/ui/uibackgroundblur.mat" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-291 - (-73.2 * x)} {84 + (-73.2 * y)}", OffsetMax = $"{-221 - (-73.2 * x)} {154 + (-73.2 * y)}" }
                }, "TakeRewards", $"InventorySlot_{i}");

                x++;
                if (x == 8 && y == 3)
                {
                    x = 2;
                    y++;
                }
                else if (x >= 8)
                {
                    x = 0;
                    y++;
                }
            }

            Int32 Slot = 0;
            foreach (var Item in player.inventory.AllItems())
            {
                String PNG = GetImage($"{Item.info.shortname}_64px");
                container.Add(new CuiElement
                {
                    Parent = $"InventorySlot_{Slot}",
                    Components = {
                    new CuiRawImageComponent { Png = PNG },
                    new CuiRectTransformComponent { AnchorMin = $"0.08 0.04", AnchorMax = $"0.9 0.9" },
                }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"ih input.drop {Item.amount} {Item.info.shortname} {Item.skin}", Color = "0 0 0 0" },
                    Text = { Text = $"x{Item.amount}", Align = TextAnchor.LowerCenter, Color = "1 1 1 0.6" }
                }, $"InventorySlot_{Slot}");

                Slot++;
            }

            if (GetAmountReward(player) >= config.Setting.CustomCreatedSetting.MaximumRewardCountUser || (IQRankSystem && config.Setting.IQRankSystemSetting.UseRank && !String.IsNullOrWhiteSpace(config.Setting.IQRankSystemSetting.Rank) && !IsRank(player.userID, config.Setting.IQRankSystemSetting.Rank)))
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0.1", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, "TakeRewards", "BLUR_BLOCK_ADD");

                String Label = (config.Setting.IQRankSystemSetting.UseRank && !String.IsNullOrWhiteSpace(config.Setting.IQRankSystemSetting.Rank) && !IsRank(player.userID, config.Setting.IQRankSystemSetting.Rank)) ? GetLang("UI_CREATED_TASK_PRECREATED_ITEM_RANK_SYSTEM", player.UserIDString, GetRankName(config.Setting.IQRankSystemSetting.Rank)) : GetAmountReward(player) >= config.Setting.CustomCreatedSetting.MaximumRewardCountUser ? GetLang("UI_CREATED_TASK_PRECREATED_ITEM_COUNT_MAX", player.UserIDString) : "";

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = Label, FontSize = 20, Color = "0.8509805 0.764706 0.5843138 1", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                },  "BLUR_BLOCK_ADD");
            }

            CuiHelper.AddUi(player, container);
        }

        private Int32 GetAmountReward(BasePlayer player)
        {
            Int32 Amount = 0;
            if (!PrePlayerHeads.ContainsKey(player.userID)
                || !PrePlayerHeads[player.userID].RewardList.ContainsKey(player.userID)) return Amount;
            Amount += PrePlayerHeads[player.userID].RewardList[player.userID].Count;

            var PHeads = PlayerHeads.FirstOrDefault(x => x.WantedID == PrePlayerHeads[player.userID].WantedID);
            if (PHeads == null) return Amount;

            var RewardList = PHeads.RewardList;
            if (RewardList != null && RewardList.ContainsKey(player.userID))
                Amount += RewardList[player.userID].Count;
            return Amount;
        }
        #endregion

        #region Take Amount
        void Show_Amount_Drop_Input(BasePlayer player, Item Item)
        {
            CuiHelper.DestroyUi(player, "InputAmount" + ".Input");
            CuiHelper.DestroyUi(player, "Title.Reward.Amount");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Parent = "TakeRewards",
                Name = "Title.Reward.Amount", 
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_TAKE_REWARDS_GET_AMOUNT_TITLE",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.LowerLeft, Color = HexToRustFormat("#D6BD8D") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-297.92 -233.06", OffsetMax = "72.607 -209.051" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                }
            });

            String Amount = String.Empty;
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "640.043 29.094", OffsetMax = "-44.103 67.402" }, 
                Image = { Color = HexToRustFormat("#D6BD8D") }
            }, "IQHEADREWARD_PANEL", "InputAmount" + ".Input");

            container.Add(new CuiElement
            {
                Parent = "InputAmount" + ".Input",
                Name = "InputAmount" + ".Input.Current",
                Components =
                {
                    new CuiInputFieldComponent { Text = Amount, FontSize = 25, Command = $"ih created.task get.item.amount {Item.info.shortname} {Item.skin} {Amount} {Item.amount}", Align = TextAnchor.MiddleCenter, CharsLimit = Item.amount.ToString().Length},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Search Player

        void Search_Player_Tasked(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SearchPlayerPanel");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.7", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0.002", OffsetMax = "-0.001 0" }
            }, "IQHEADREWARD_PANEL", "SearchPlayerPanel");

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Close = "SearchPlayerPanel" },
                Text = { Text = "" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, "SearchPlayerPanel", "ButtonClose");

            container.Add(new CuiElement
            {
                Name = "InputNickInfo",
                Parent = "SearchPlayerPanel",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_SEARCH_PLAYER_TITLE", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 35, Align = TextAnchor.MiddleCenter, Color = "0.8274511 0.7372549 0.5529412 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-640 282.876", OffsetMax = "640 359.99" }
                }
            });

            String NickName = "";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-402.85 245.614", OffsetMax = "356.353 282.879" },
                Image = { Color = HexToRustFormat("#D6BD8D") }
            }, "SearchPlayerPanel", "InputPanelSearch" + ".Input");

            container.Add(new CuiElement
            {
                Parent = "InputPanelSearch" + ".Input",
                Name = "InputPanelSearch" + ".Input.Current",
                Components =
                {
                    new CuiInputFieldComponent { Text = NickName, FontSize = 20, Command = $"ih created.task search.player.task.confirm.open {NickName}", Align = TextAnchor.MiddleCenter, CharsLimit = 25},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "InputSprite",
                Parent = "SearchPlayerPanel",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Sprite = "assets/content/ui/death-marker@4x.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "363 233.62", OffsetMax = "411 294.88" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "InputNickInfoDescription",
                Parent = "SearchPlayerPanel",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_SEARCH_PLAYER_DESCRIPTION_TITLE",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "0.8274511 0.7372549 0.5529412 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-639.997 209.983", OffsetMax = "640.003 245.617" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        void Show_Confirm_Player_Tasked(BasePlayer player, BasePlayer TaskPlayer)
        {
            CuiHelper.DestroyUi(player, "ConfirmPlayer");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 1 1 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-406.925 -181.381", OffsetMax = "406.925 105.381" }
            }, "SearchPlayerPanel", "ConfirmPlayer");

            container.Add(new CuiElement
            {
                Name = "ConfirmInformation",
                Parent = "ConfirmPlayer",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_SEARCH_PLAYER_CONFIRM_TITLE",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleLeft, Color = HexToRustFormat("#D6BD8D") },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-119.905 95.792", OffsetMax = "406.925 143.381" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "CPPlayerNick",
                Parent = "ConfirmPlayer",
                Components = {
                    new CuiTextComponent { Text = TaskPlayer.displayName, Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-406.67 -143.38", OffsetMax = "-119.91 -112.975" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = $"ConfirmPlayer",
                Name = "ConfirmInformation_Avatar",
                Components =
                    {
                    new CuiRawImageComponent { Png = GetImage(TaskPlayer.UserIDString),Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-406.92 -143.381", OffsetMax = "-119.9 143.38" }
                    }
            });

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat("#56A65B"), Command = $"ih created.task search.player.task.confirm.yes {TaskPlayer.userID}" },
                Text = { Text = GetLang("UI_CREATED_TASK_HEAD_SEARCH_PLAYER_CONFIRM_TITLE_YES", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 35, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-119.911 -143.381", OffsetMax = "145.059 90.399" }
            }, "ConfirmPlayer", "ButtonYes");

            container.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat("#B75852"), Close = "ConfirmPlayer" },
                Text = { Text = GetLang("UI_CREATED_TASK_HEAD_SEARCH_PLAYER_CONFIRM_TITLE_NO", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 35, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "145.06 -143.38", OffsetMax = "410.02 90.4" }
            }, "ConfirmPlayer", "ButtonNo");

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Take Time

        void Take_Time_Custom(BasePlayer player)
        {
            var CustomCreated = config.Setting.CustomCreatedSetting;
            if (!CustomCreated.UseCutomTime) return;

            CuiHelper.DestroyUi(player, "PanelTakeTime");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "0.045 145.71", OffsetMax = "595.975 255.69" }
            }, "IQHEADREWARD_PANEL", "PanelTakeTime");

            container.Add(new CuiElement
            {
                Name = "NameTime",
                Parent = "PanelTakeTime",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_STATUS_PRECREATED_TIME_TITLE",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#D6BD8D") },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-297.967 19.357", OffsetMax = "297.963 44.99" }
                }
            });

            #region Centering
            Int32 Count = 0;
            Int32 ItemCount = 0;
            Single itemMinPosition = 219f;
            Single itemWidth = 0.413646f - 0.28f; 
            Single itemMargin = 0.439895f - 0.4f; 
            Int32 itemCount = CustomCreated.TimeSetting.CustomTimeSettings.Count(x => IQEconomicGetBalance(player.userID) >= x.IQEconomic_Balance || !x.IQEconomic_CustomTime);
            Single itemMinHeight = 0.48f;
            Single itemHeight = 0.16f; 
            Int32 ItemTarget = 5;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
            #endregion

            foreach (Configuration.Settings.CustomCreated.TimeSettings.TimeCreatedMore Time in CustomCreated.TimeSetting.CustomTimeSettings.Where(x => IQEconomicGetBalance(player.userID) >= x.IQEconomic_Balance || !x.IQEconomic_CustomTime))
            {
                String SpriteCheckBox = PrePlayerHeads.ContainsKey(player.userID) && PrePlayerHeads[player.userID].Cooldown == Time.Time ? "assets/icons/check.png" : "assets/icons/close.png";
                String ColorSpriteCheckBox = PrePlayerHeads.ContainsKey(player.userID) && PrePlayerHeads[player.userID].Cooldown == Time.Time ? "#4DBE62" : "#BE4C54";
                String TimeResult = Time.IQEconomic_CustomTime && Time.IQEconomic_Balance != 0 ?  $"- {FormatTime(TimeSpan.FromSeconds((Double)Time.Time), player.UserIDString)} <color=white><size=10>(₽)</size></color>" : $"- {FormatTime(TimeSpan.FromSeconds((Double)Time.Time), player.UserIDString)}";
                String Command = PrePlayerHeads.ContainsKey(player.userID) && PrePlayerHeads[player.userID].Cooldown == Time.Time ? "" : $"ih created.task turn.time {Time.Time}";

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = { Color = "0 0 0 0" }
                }, $"PanelTakeTime", $"ITEM_{Count}");

                container.Add(new CuiElement
                {
                    Parent = $"ITEM_{Count}",
                    Components = {
                    new CuiTextComponent { Text = TimeResult, Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "0.8274511 0.7372549 0.5529412 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.27 0", AnchorMax = "1 1" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = $"CHECK_BOX_{Count}",
                    Parent = $"ITEM_{Count}",
                    Components = {
                    new CuiRawImageComponent { Color = HexToRustFormat(ColorSpriteCheckBox), Sprite = SpriteCheckBox, Material = "assets/icons/iconmaterial.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.06 0.2", AnchorMax = "0.2 0.8" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = Command },
                    Text = { Text = "" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                },  $"CHECK_BOX_{Count}");

                #region Centring
                Count++;
                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 1f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
                #endregion
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Show List Items
        void Show_Item_Reward(BasePlayer player, List<Configuration.Settings.ItemList> ItemList) 
        {
            CuiHelper.DestroyUi(player, "LogoRewardList");
            CuiHelper.DestroyUi(player, "LogoRewardPanel");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "LogoRewardList",
                Parent = "IQHEADREWARD_PANEL",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("WANTED_ITEMS") },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "-9.71 -347.342", OffsetMax = "483.016 287.958" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-178.521 -218.394", OffsetMax = "178.521 149" }
            }, "LogoRewardList", "PanelRewards");

            #region Centering
            Int32 Count = 0;
            Int32 ItemCount = 0;
            Single itemMinPosition = 219f;
            Single itemWidth = 0.413646f - 0.26f; 
            Single itemMargin = 0.439895f - 0.425f;  
            Int32 itemCount = ItemList.Count;
            Single itemMinHeight = 0.85f; 
            Single itemHeight = 0.15f; 
            Int32 ItemTarget = 6;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
            #endregion

            foreach(var Item in ItemList.Take(36))
            {
                container.Add(new CuiElement
                {
                    Name = $"ITEM_{Count}",
                    Parent = $"PanelRewards",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#E1940050") },
                        new CuiRectTransformComponent{ AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 -1", UseGraphicAlpha = true },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = $"ITEM_{Count}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage($"{Item.Shortname}_64px"), Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent{ AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    }
                });

                String Amount = $"x{Item.AmountMax}";

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = Amount, FontSize = 8, Color = HexToRustFormat("#180D00FF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerCenter }
                }, $"ITEM_{Count}");

                container.Add(new CuiElement
                {
                    Name = $"TakeButtonRemove_Sprite_{Count}",
                    Parent = $"ITEM_{Count}",
                    Components = {
                    new CuiRawImageComponent { Color = HexToRustFormat("#BE4C54F1"), Material = "assets/icons/iconmaterial.mat", Sprite = "assets/icons/close.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.35 0.35", AnchorMax = "0.65 0.65" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"ih created.task remove.item {Item.Shortname} {Item.SkinID} {Item.AmountMax}" },
                    Text = { Text = "", Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, $"TakeButtonRemove_Sprite_{Count}", "TakeButtonRemove");

                #region Centring
                Count++;
                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 1f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
                #endregion
            }

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = "ih update.task" },
                Text = { Text = GetLang("UI_CREATED_TASK_HEAD_ITEM_LIST_BACK", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "0.09411766 0.0509804 0 0.9" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-159.371 -292", OffsetMax = "161.179 -260.265" }
            }, "LogoRewardList", "BtnBack");

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Update Task

        void Update_Task(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SearchPlayerPanel");
            CuiHelper.DestroyUi(player, "LogoRewardList");
            CuiHelper.DestroyUi(player, "LogoRewardPanel");
            CuiHelper.DestroyUi(player, "TakePlayerButton");
            CuiHelper.DestroyUi(player, "AVATAR_TASK");
            CuiHelper.DestroyUi(player, "CreatedResult");
            CuiElementContainer container = new CuiElementContainer();
            if (PrePlayerHeads.ContainsKey(player.userID) && PrePlayerHeads[player.userID].WantedID != 0)
            {
                container.Add(new CuiElement
                {
                    Parent = $"IQHEADREWARD_PANEL",
                    Name = "AVATAR_TASK",
                    Components =
                    {
                    new CuiRawImageComponent { Png = GetImage(PrePlayerHeads[player.userID].WantedID.ToString()),Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-510.84 -154.63", OffsetMax = "-276.58 87.97" }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "AVATAR_TASK_NAME_PLAYER",
                    Parent = "AVATAR_TASK",
                    Components = {
                    new CuiTextComponent { Text = $"<b>{PrePlayerHeads[player.userID].WantedName}</b>", Font = "robotocondensed-regular.ttf", FontSize = 17, Align = TextAnchor.LowerCenter, Color = "0.8509805 0.764706 0.5843138 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.05", AnchorMax = "1 1" }
                }
                }); 
            }

            container.Add(new CuiElement
            {
                Name = "LogoRewardPanel",
                Parent = "IQHEADREWARD_PANEL",
                Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = GetImage("WANTED") },
                    new CuiRectTransformComponent { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "11.492 -352.992", OffsetMax = "481.095 287.958" }
                }
            });
            
            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = "ih created.task search.player.task.open" },
                Text = { Text = GetLang("UI_CREATED_TASK_HEAD_TAKE_PLAYER_TITLE", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "0.09411766 0.0509804 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-510.84 -154.63", OffsetMax = "-276.58 87.97" }
            }, "IQHEADREWARD_PANEL", "TakePlayerButton");

            if (PrePlayerHeads.ContainsKey(player.userID))
            {
                if (GetPercentStatus(player, PrePlayerHeads[player.userID]) >= 1)
                {
                    if (config.Setting.CustomCreatedSetting.UseCutomTime)
                    {
                        var Time = config.Setting.CustomCreatedSetting.TimeSetting.CustomTimeSettings.FirstOrDefault(t => t.Time == PrePlayerHeads[player.userID].Cooldown);
                        if (Time != null)
                        {
                            String TimeResult = Time.IQEconomic_CustomTime && Time.IQEconomic_Balance != 0 ? $"{GetLang("UI_CREATED_TASK_HEAD_PRECREATED_RESULT_BTN", player.UserIDString)} <color=white><size=16>+{Time.IQEconomic_Balance}(₽)</size></color>" : GetLang("UI_CREATED_TASK_HEAD_PRECREATED_RESULT_BTN", player.UserIDString);

                            container.Add(new CuiButton
                            {
                                Button = { Color = HexToRustFormat("#D5BC8E"), Command = "ih created.task result.created" },
                                Text = { Text = TimeResult, Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "0.09411766 0.05094 0 1" }, //
                                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "137.657 249.939", OffsetMax = "458.283 287.961" }
                            }, "IQHEADREWARD_PANEL", "CreatedResult");
                        }
                    }
                    else
                    {
                        container.Add(new CuiButton
                        {
                            Button = { Color = HexToRustFormat("#D5BC8E"), Command = "ih created.task result.created" },
                            Text = { Text = GetLang("UI_CREATED_TASK_HEAD_PRECREATED_RESULT_BTN", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "0.09411766 0.05094 0 1" }, //
                            RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "137.657 249.939", OffsetMax = "458.283 287.961" }
                        }, "IQHEADREWARD_PANEL", "CreatedResult");
                    }
                }
            }
            CuiHelper.AddUi(player, container);
            Update_Task_Items(player);
            Update_Status_Created(player);
            Take_Time_Custom(player);
        }

        #endregion

        #region Update Take Items

        void Update_Task_Items(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "RewardListPanel");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-181 -242.249", OffsetMax = "181 -180.151" },
                Image = { Color = "0 0 0 0" }
            }, "LogoRewardPanel", "RewardListPanel");

            if (!PrePlayerHeads.ContainsKey(player.userID) || !PrePlayerHeads[player.userID].RewardList.ContainsKey(player.userID)) return;
            var TaskUsers = PrePlayerHeads[player.userID].RewardList[player.userID];

            #region Centering
            Int32 Count = 0;
            Int32 ItemCount = 0;
            Single itemMinPosition = 219f;
            Single itemWidth = 0.413646f - 0.25f;
            Single itemMargin = 0.439895f - 0.42f;
            Int32 itemCount = TaskUsers.Count;
            Single itemMinHeight = 0f; 
            Single itemHeight = 1f; 
            Int32 ItemTarget = 5;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
            #endregion

            foreach (var TaskUser in TaskUsers.Take(5))
            {
                container.Add(new CuiElement
                {
                    Name = $"ITEM_{Count}",
                    Parent = $"RewardListPanel",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat("#E1940050") },
                        new CuiRectTransformComponent{ AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "1 -1", UseGraphicAlpha = true },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = $"ITEM_{Count}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = GetImage($"{TaskUser.Shortname}_64px"), Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent{ AnchorMin = $"0.05 0.05", AnchorMax = $"0.95 0.95" },
                    }
                });

                String Amount = $"x{TaskUser.AmountMax}";

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = Amount, FontSize = 10, Color = HexToRustFormat("#180D00FF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerCenter }
                }, $"ITEM_{Count}");

                container.Add(new CuiElement 
                {
                    Name = $"TakeButtonRemove_Sprite_{Count}",
                    Parent = $"ITEM_{Count}",
                    Components = {
                    new CuiRawImageComponent { Color = HexToRustFormat("#BE4C54F1"), Material = "assets/icons/iconmaterial.mat", Sprite = "assets/icons/close.png" },
                    new CuiRectTransformComponent { AnchorMin = "0.35 0.35", AnchorMax = "0.65 0.65" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"ih created.task remove.item {TaskUser.Shortname} {TaskUser.SkinID} {TaskUser.AmountMax}" },
                    Text = { Text = "", Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, $"TakeButtonRemove_Sprite_{Count}", "TakeButtonRemove");

                #region Centring
                Count++;
                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 1f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
                #endregion
            }

            if (TaskUsers.Count > 5)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat("#00000026"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Command = "ih created.task show.reward.list" },
                    Text = { Text = GetLang("UI_CREATED_TASK_HEAD_SHOW_MORE_ITEMS",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.LowerCenter, Color = "0.09411766 0.0509804 0 1" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, "RewardListPanel", "TakeButtonMoreItems");
            }
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Update Status Created

        void Update_Status_Created(BasePlayer player)
        {
            String SpritePlayer = String.Empty;
            String ColorSpritePlayer = String.Empty;
            String SpriteItems = String.Empty;
            String ColorSpriteItems = String.Empty;
            String SpriteTime = String.Empty;
            String ColorSpriteTime = String.Empty;
            HeadTask Data = new HeadTask { RewardList = new Dictionary<ulong, List<Configuration.Settings.ItemList>> { }, WantedID = 0, Cooldown = 0 };
            if (PrePlayerHeads.ContainsKey(player.userID))
                Data = PrePlayerHeads[player.userID];

            Single ProgressY = GetPercentStatus(player, Data);

            CuiHelper.DestroyUi(player, "UpdateStatusCreated");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-607.988 297.559", OffsetMax = "-180.407 346.287" }
            }, "IQHEADREWARD_PANEL", "UpdateStatusCreated");

            container.Add(new CuiElement
            {   
                Name = "ProgressTitle",
                Parent = "UpdateStatusCreated",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_STATUS_PRECREATED_TITLE",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "0.8274511 0.7372549 0.5529412 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-211.38 6.724", OffsetMax = "-75.62 23.364" }
                }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.8274511 0.7372549 0.5529412 0.45" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-211.38 -21.375", OffsetMax = "-205.857 5" }
            }, "UpdateStatusCreated", "ProgressBar");

            if (Data.WantedID != 0)
            {
                SpritePlayer = "assets/icons/check.png";
                ColorSpritePlayer = "#4DBE62";
            }
            else
            {
                SpritePlayer = "assets/icons/close.png";
                ColorSpritePlayer = "#BE4C54";
            }

            container.Add(new CuiElement
            {
                Name = "CheckBoxTakePlayer",
                Parent = "UpdateStatusCreated",
                Components = {
                    new CuiRawImageComponent { Color = HexToRustFormat(ColorSpritePlayer), Sprite = SpritePlayer, Material = "assets/icons/iconmaterial.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-203.38 -21.5", OffsetMax = "-191.38 -9.5" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "TitleCheckBoxTakePlayer",
                Parent = "UpdateStatusCreated",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_STATUS_PRECREATED_PLAYER",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.UpperLeft, Color = "0.8274511 0.7372549 0.5529412 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-189.765 -21.5", OffsetMax = "-49.835 -9.5" }
                }
            });

            if (Data.RewardList.ContainsKey(player.userID) && Data.RewardList[player.userID].Count != 0)
            {
                SpriteItems = "assets/icons/check.png";
                ColorSpriteItems = "#4DBE62";
            }
            else
            {
                SpriteItems = "assets/icons/close.png";
                ColorSpriteItems = "#BE4C54";
            }

            container.Add(new CuiElement
            {
                Name = "CheckBoxTakeItem",
                Parent = "UpdateStatusCreated",
                Components = {
                    new CuiRawImageComponent { Color = HexToRustFormat(ColorSpriteItems), Sprite = SpriteItems, Material = "assets/icons/iconmaterial.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-203.38 -7.125", OffsetMax = "-191.38 4.875" }
                }
            });

            container.Add(new CuiElement
            {
                Name = "TitleCheckBoxTakeItem",
                Parent = "UpdateStatusCreated",
                Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_STATUS_PRECREATED_ITEMS",player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.UpperLeft, Color = "0.8274511 0.7372549 0.5529412 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-189.764 -7.125", OffsetMax = "-49.836 4.875" }
                }
            });

            if (config.Setting.CustomCreatedSetting.UseCutomTime)
            {
                SpriteTime = Data.Cooldown != 0 ? "assets/icons/check.png" : "assets/icons/close.png";
                ColorSpriteTime = Data.Cooldown != 0 ? "#4DBE62" : "#BE4C54";

                container.Add(new CuiElement
                {
                    Name = "CheckBoxTakeTime",
                    Parent = "UpdateStatusCreated",
                    Components = {
                    new CuiRawImageComponent { Color = HexToRustFormat(ColorSpriteTime), Sprite = SpriteTime, Material = "assets/icons/iconmaterial.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45.4 -7.125", OffsetMax = "-33.4 4.875" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "TitleCheckBoxTakeTime",
                    Parent = "UpdateStatusCreated",
                    Components = {
                    new CuiTextComponent { Text = GetLang("UI_CREATED_TASK_HEAD_STATUS_PRECREATED_TIME", player.UserIDString), Font = "robotocondensed-regular.ttf", FontSize = 8, Align = TextAnchor.UpperLeft, Color = "0.8274511 0.7372549 0.5529412 1"  },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-31.764 -7.125", OffsetMax = "108.164 4.875" }
                }
                });
            }

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.8274511 0.7372549 0.5529412 0.9" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 {ProgressY}", OffsetMin = "1 1", OffsetMax = "0 -1" }
            },  "ProgressBar", "ProgressBarProcess");

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #endregion

        #region AlertUI

        void AlertUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, ALERT_UI);
            var Interface = config.Setting.AlertUISetting;

            container.Add(new CuiElement
            {
                FadeOut = 0.5f,
                Parent = "Overlay",
                Name = ALERT_UI,
                Components =
                    {
                    new CuiRawImageComponent { FadeIn = 0.5f, Png = GetImage($"ALERT_UI_{config.Setting.AlertUISetting.PNG}"),Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent{ AnchorMin = Interface.AnchorMin, AnchorMax = Interface.AnchorMax, OffsetMin = Interface.OffsetMin, OffsetMax = Interface.OffsetMax},
                    }
            });

            if(Interface.UserCloseUI)
            {
                container.Add(new CuiButton
                {
                    FadeOut = 0.5f,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { FadeIn = 0.5f, Close = ALERT_UI, Color = "0 0 0 0" },
                    Text = { FadeIn = 0.5f, Text = "" }
                }, ALERT_UI, "CLOSE_BTN_ALERT");
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion

        #region Lang

        public static StringBuilder sb = new StringBuilder();
        public String GetLang(String LangKey, String userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NEW_HEAD"] = "Attention!\nPer player <color=#CC3226>{0}</color> there was a bounty on his head!\nYou can find out about the award by following the command <color=#966C43>/ih</color>",
                ["HEAD_KILLED"] = "You killed the player on whom the reward was assigned!\nCongratulate!\nYou can pick up your reward in the menu- <color=#966C43>/ih</color>",
                ["HEAD_KILLED_ALERT"] = "Wanted <color=#CC3226>{0}</color> on whom the reward was assigned, they were killed",

                ["UI_TITLE"] = "<b><size=40>Bulletin board</size></b>",
                ["UI_DESCRIPTION"] = "<b><size=20>This board displays the players who have a reward</size></b>",
                ["UI_CLOSE"] = "<b><size=30>Close</size></b>",

                ["UI_CREATED_TASK_HEAD_TITLE"] = "Head bounty announcement",
                ["UI_CREATED_TASK_HEAD_TAKE_PLAYER_TITLE"] = "SELECT A PLAYER",
                ["UI_CREATED_TASK_HEAD_TAKE_REWARDS_TITLE"] = "Choose a reward that you are ready to give for killing a player!",
                ["UI_CREATED_TASK_HEAD_TAKE_REWARDS_GET_AMOUNT_TITLE"] = "Enter the quantity you need :",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_TITLE"] = "Enter the player's nickname to announce a reward for his head",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_DESCRIPTION_TITLE"] = "After entering the nickname, press «Enter» and make sure that this is the right player",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_CONFIRM_TITLE"] = "MAKE SURE THAT THIS IS EXACTLY THE RIGHT PLAYER FOR YOU?",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_CONFIRM_TITLE_YES"] = "YES, THAT'S RIGHT",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_CONFIRM_TITLE_NO"] = "NO, THIS IS NOT THE ONE",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_TIME"] = "- Set the execution time",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_ITEMS"] = "- Select an item as a reward",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_PLAYER"] = "- Select a player",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_TITLE"] = " Creation progress:",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_TIME_TITLE"] = "Select the time you want to set for this task",
                ["UI_CREATED_TASK_HEAD_ITEM_LIST_BACK"] = "<b>GO BACK</b>",
                ["UI_CREATED_TASK_HEAD_PRECREATED_RESULT_BTN"] = "<b>CREATE A TASK</b>",
                ["UI_CREATED_TASK_HEAD_SHOW_MORE_ITEMS"] = "<b>CLICK TO VIEW ALL AWARDS</b>",
                ["UI_CREATED_TASK_PRECREATED_ITEM_COUNT_MAX"] = "<b>LIMITATION</b>\nYou can no longer add items for this player\nThe limit has been exceeded\nYou can delete the old item and replace it with a new one",
                ["UI_CREATED_TASK_PRECREATED_ITEM_RANK_SYSTEM"] = "<b>LIMITATION</b>\nYou can't create a bounty on your head, you need to get a rank {0} to open this feature",

                ["CHAT_CREATED_TASK_RESULT"] = "You have successfully created a task for the player, it will be active <color=#CC3226>{0}</color>",
                ["CHAT_CREATED_TASK_RESULT_MORE_ITEMS_ADD"] = "A task has already been created for the player, you have successfully added items as a reward for his head!\nThe excitement on his head is growing!",
                ["CHAT_CREATED_TASK_RESULT_MORE_ITEMS_ADD_PLUS_TIME"] = "A task has already been created for the player, you have successfully added items as a reward for his head and increased the time for a fee!\nThe task will be active <color=#CC3226>{0}</color>\nThe excitement on his head is growing!",
                ["CHAT_ALERT_TASK_PLAYER"] = "There's a bounty on your head!\n Be vigilant, because the danger is at every step!",
                ["CHAT_ALERT_TASK_PLAYER_RETRY"] = "A reward has been re-assigned for your head!\nYou've obviously crossed the wrong people",
                ["CHAT_ALERT_KILLED_REWARD_PLAYER"] = "You have successfully destroyed the target!\nYour reward has been created!\n You received : <color=#CC3226>{0}</color>",
                ["CHAT_ALERT_KILLED_REWARD_ITEM"] = "<color=#CC3226>{0}</color> a box(a/s/k) with items as a reward are scattered and marked on <color=#966C43>G</color> map",
                ["CHAT_ALERT_KILLED_REWARD_OTHER"] = "<color=#CC3226>{0}</color> the awards have already been issued to you",
                ["CHAT_ALERT_RETURNED_ITEMS"] = "The time for killing the selected player has expired, the ad has been removed from him, you can pick up your items through the team <color=#966C43>/ir</color>",
                ["CHAT_ALERT_RETURNED_ITEMS_COOLDOWN_PRE_CREATED"] = "The time for creating a bounty for the head has expired",
                ["CHAT_ALERT_RETURNED_ITEMS_FINISH"] = "Your items have been successfully returned to you and added to your inventory",
                ["CHAT_ALERT_COOLDOWNS_FALSE"] = "Time for an ad per player's head <color=#CC3226>{0}</color> expired, you can create a new ad for the player's head!",
                ["YOUR_CRATE_BRO"] = "YOUR REWARD BOX",

                ["UI_MAIN_MENU_IH_DESCRIPTION"] = "If there are less than 3 tasks , you can announce a head reward for a player that you don't like by clicking on the" + "or add a reward to an existing ad, just click on it",
                ["UI_MAIN_MENU_IH_DESCRIPTION_TITLE"] = "This board displays the players on which the head reward is announced",
                ["UI_MAIN_MENU_IH_TITLE"] = "BULLETIN BOARD",

                ["TITLE_FORMAT_DAYS"] = "D",
                ["TITLE_FORMAT_HOURSE"] = "H",
                ["TITLE_FORMAT_MINUTES"] = "M",
                ["TITLE_FORMAT_SECONDS"] = "S",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NEW_HEAD"] = "Внимание!\nНа игрока <color=#CC3226>{0}</color> была назначена награда за его голову!\nУзнать о награде можно по команде <color=#966C43>/ih</color>",
                ["HEAD_KILLED"] = "Вы убили игрока,на которого была назначена награда!\nПоздравляем!\nВы можете забрать свою награду в меню - <color=#966C43>/ih</color>",
                ["HEAD_KILLED_ALERT"] = "Игрока <color=#CC3226>{0}</color> на которого была назначена награда - убили",

                ["UI_TITLE"] = "<b><size=40>Доска объявлений</size></b>",
                ["UI_DESCRIPTION"] = "<b><size=20>На данной доске отображены игроки,на которых назначена награда</size></b>",
                ["UI_CLOSE"] = "<b><size=30>3акрыть</size></b>",

                ["UI_CREATED_TASK_HEAD_TITLE"] = "Объявление награды за голову",
                ["UI_CREATED_TASK_HEAD_TAKE_PLAYER_TITLE"] = "ВЫБРАТЬ ИГРОКА",
                ["UI_CREATED_TASK_HEAD_TAKE_REWARDS_TITLE"] = "Выберите награду, которую вы готовы дать за убийство игрока!",
                ["UI_CREATED_TASK_HEAD_TAKE_REWARDS_GET_AMOUNT_TITLE"] = "Введите количество, которое вам требуется :",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_TITLE"] = "Введите ник игрока чтобы объявить за его голову награду",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_DESCRIPTION_TITLE"] = "После введения ника нажмите «Enter» и убедитесь тот ли это игрок",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_CONFIRM_TITLE"] = "УБЕДИТЕСЬ, ЭТО ТОЧНО НУЖНЫЙ ВАМ ИГРОК?",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_CONFIRM_TITLE_YES"] = "ДА, ВСЕ ВЕРНО",
                ["UI_CREATED_TASK_HEAD_SEARCH_PLAYER_CONFIRM_TITLE_NO"] = "НЕТ, ЭТО НЕ ТОТ",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_TIME"] = "- Установить время на исполнение",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_ITEMS"] = "- Выбрать предмет в качестве награды",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_PLAYER"] = "- Выбрать игрока",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_TITLE"] = "Прогресс создания:",
                ["UI_CREATED_TASK_HEAD_STATUS_PRECREATED_TIME_TITLE"] = "Выберите время, которое хотите установить на данную задачу",
                ["UI_CREATED_TASK_HEAD_ITEM_LIST_BACK"] = "<b>ВЕРНУТЬСЯ НАЗАД</b>",
                ["UI_CREATED_TASK_HEAD_PRECREATED_RESULT_BTN"] = "<b>СОЗДАТЬ ЗАДАНИЕ</b>",
                ["UI_CREATED_TASK_HEAD_SHOW_MORE_ITEMS"] = "<b>НАЖМИТЕ ЧТОБЫ ПРОСМОТРЕТЬ ВСЕ НАГРАДЫ</b>",
                ["UI_CREATED_TASK_PRECREATED_ITEM_COUNT_MAX"] = "<b>ОГРАНИЧЕНИЕ</b>\nВы больше не можете добавлять предметы для этого игрока\nПревышен лимит\nВы можете удалить старый предмет и заменить новым",
                ["UI_CREATED_TASK_PRECREATED_ITEM_RANK_SYSTEM"] = "<b>ОГРАНИЧЕНИЕ</b>\nВы не можете создавать награду за голову, вам требуется получить ранг {0} для открытия данной возможности",

                ["CHAT_CREATED_TASK_RESULT"] = "Вы успешно создали задание на игрока, оно будет активно <color=#CC3226>{0}</color>",
                ["CHAT_CREATED_TASK_RESULT_MORE_ITEMS_ADD"] = "На игрока уже было создано задание, вы успешно добавили предметы в качестве награды за его голову!\nАжиотаж на его голову нарастает!",
                ["CHAT_CREATED_TASK_RESULT_MORE_ITEMS_ADD_PLUS_TIME"] = "На игрока уже было создано задание, вы успешно добавили предметы в качестве награды за его голову и увеличили время за плату!\nЗадание будет активно <color=#CC3226>{0}</color>\nАжиотаж на его голову нарастает!",
                ["CHAT_ALERT_TASK_PLAYER"] = "За вашу голову назначена награда!\nБудьте бдительны, ведь опасность на каждом шагу!",
                ["CHAT_ALERT_TASK_PLAYER_RETRY"] = "За вашу голову повторно назначена награда!\nВы явно перешли дорогу не тем людям",
                ["CHAT_ALERT_KILLED_REWARD_PLAYER"] = "Вы успешно уничтожили цель!\n Ваша награда создана!\n Вы получили : <color=#CC3226>{0}</color>",
                ["CHAT_ALERT_KILLED_REWARD_ITEM"] = "<color=#CC3226>{0}</color> ящик(а/ов/к) с предметами в качестве награды разбросаны и помечены на <color=#966C43>G</color> карте",
                ["CHAT_ALERT_KILLED_REWARD_OTHER"] = "<color=#CC3226>{0}</color> наград вам уже выданы",
                ["CHAT_ALERT_RETURNED_ITEMS"] = "Время на убийство выбранного игрока истекло, с него снято объявление, вы можете забрать свои предметы через команду <color=#966C43>/ir</color>",
                ["CHAT_ALERT_RETURNED_ITEMS_COOLDOWN_PRE_CREATED"] = "Истекло время создания награды за голову",
                ["CHAT_ALERT_RETURNED_ITEMS_FINISH"] = "Ваши предметы успешно вернуты вам и добавлены в инвентарь",
                ["CHAT_ALERT_COOLDOWNS_FALSE"] = "Время на объявление за голову игрока <color=#CC3226>{0}</color> истекло, вы можете создать новое объявление за голову игрока!",
                ["YOUR_CRATE_BRO"] = "ВАШ ЯЩИК С НАГРАДОЙ",

                ["UI_MAIN_MENU_IH_DESCRIPTION"] = "Если заданий меньше 3-х - вы сможете объявить награду за голову на игрока, который вам не нравится нажав на «+» или дополнить награду на уже существующее объявление, просто нажмите на него",
                ["UI_MAIN_MENU_IH_DESCRIPTION_TITLE"] = "На данной доске отображены игроки на которых объявлена награда за голову",
                ["UI_MAIN_MENU_IH_TITLE"] = "ДОСКА ОБЪЯВЛЕНИЙ",

                ["TITLE_FORMAT_DAYS"] = "Д",
                ["TITLE_FORMAT_HOURSE"] = "Ч",
                ["TITLE_FORMAT_MINUTES"] = "М",
                ["TITLE_FORMAT_SECONDS"] = "С",
            }, this, "ru");
        }
        #endregion

        #region Map Mark 

        private object CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            MapMarker mapComponent = entity.GetComponent<MapMarker>();
            if (mapComponent != null)
            {
                VendingMachineMapMarker x = mapComponent.GetComponent<VendingMachineMapMarker>();
                if (x != null && x.OwnerID != 0)
                {
                    if (x.server_vendingMachine != null)
                        return null;

                    if (x.OwnerID != target.userID)
                        return false;
                }
                MapMarkerGenericRadius y = mapComponent.GetComponent<MapMarkerGenericRadius>();
                if (y != null && y.OwnerID != 0)
                    if (y.OwnerID != target.userID)
                        return false;
            }
            return null;
        }

        private void CreateMarker(BaseEntity WantedEntity)
        {
            if (!config.Setting.mapMark.UseMark) return;

            var Marker = config.Setting.mapMark;
            BasePlayer Wanted = WantedEntity.ToPlayer();
            if (MapMarkers.ContainsKey(Wanted)) return;

            CustomMapMarker MapMarker = new CustomMapMarker();
            MapMarker.DisplayName = Marker.DisplayName.Replace("%NAME%", Wanted.displayName.ToUpper());
            MapMarker.Radius = Marker.Radius;
            MapMarker.RefreshRate = Marker.RefreshRate;
            MapMarker.Parent = WantedEntity;
            MapMarker.IDMark = 0;
            ColorUtility.TryParseHtmlString($"{Marker.Color}", out MapMarker.Color);
            ColorUtility.TryParseHtmlString($"{Marker.OutLineColor}", out MapMarker.OutLineColor);

            Wanted.gameObject.AddComponent<CustomMapMarker>().Init(MapMarker);
            MapMarkers.Add(Wanted, MapMarker);
        }

        private void CreateMarker(BaseEntity Entity, BasePlayer Killer)
        {
            if (!config.Setting.mapMark.UseMarkCrates) return;
            if (Killer == null) return;
            if (MapMarkers.ContainsKey(Entity)) return;

            var Marker = config.Setting.mapMark;

            CustomMapMarker MapMarker = new CustomMapMarker();
            MapMarker.DisplayName = GetLang("YOUR_CRATE_BRO", Killer.UserIDString);
            MapMarker.Radius = Marker.Radius;
            MapMarker.RefreshRate = Marker.RefreshRate;
            MapMarker.Parent = Entity;
            MapMarker.IDMark = Killer.userID;
            ColorUtility.TryParseHtmlString($"{Marker.Color}", out MapMarker.Color);
            ColorUtility.TryParseHtmlString($"{Marker.OutLineColor}", out MapMarker.OutLineColor);

            Entity.gameObject.AddComponent<CustomMapMarker>().Init(MapMarker);
            MapMarkers.Add(Entity, MapMarker);
        }

        private void RemoveMarkers()
        {
            foreach (CustomMapMarker Marker in UnityEngine.Object.FindObjectsOfType<CustomMapMarker>())
                UnityEngine.Object.Destroy(Marker);
        }

        private const String GenericPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";
        private const String VendingPrefab = "assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab";

        #region Scripts

        public class CustomMapMarker : FacepunchBehaviour
        {
            private VendingMachineMapMarker Vending;
            private MapMarkerGenericRadius Generic;

            public BaseEntity Parent;

            public Single Radius;
            public Color Color;
            public Color OutLineColor;
            public String DisplayName;
            public Single RefreshRate;

            public UInt64 IDMark;

            public void Init(CustomMapMarker Info)
            {
                this.Radius = Info.Radius;
                this.Color = Info.Color;
                this.OutLineColor = Info.OutLineColor;
                this.DisplayName = Info.DisplayName;
                this.RefreshRate = Info.RefreshRate;
                this.Parent = Info.Parent;
                this.IDMark = Info.IDMark;
            }
            private void Start()
            {
                transform.position = Parent.transform.position;
                CreateMarkers();
            }

            private void CreateMarkers()
            {
                Vending = GameManager.server.CreateEntity(VendingPrefab, Parent.transform.position, Quaternion.identity, true).GetComponent<VendingMachineMapMarker>();
                Vending.markerShopName = DisplayName;
                Vending.enableSaving = false;
                Vending.OwnerID = IDMark;
                Vending.Spawn();

                Generic = GameManager.server.CreateEntity(GenericPrefab, Vending.transform.position, Quaternion.identity, true).GetComponent<MapMarkerGenericRadius>();
                Generic.color1 = Color;
                Generic.color2 = OutLineColor;
                Generic.radius = Radius;
                Generic.alpha = 1f;
                Generic.enableSaving = false;
                Generic.OwnerID = IDMark;
                Generic.Spawn();

                Vending.SetParent(Parent, true , true);
                Generic.SetParent(Vending, true, true);

                if (RefreshRate > 0f) InvokeRepeating(nameof(UpdateMarkers), RefreshRate, 0.5f);
            }

            private void UpdateMarkers()
            {
                Vending.SendNetworkUpdate();
                Generic.SendUpdate();
                Generic.SendNetworkUpdate();
            }

            private void DestroyMakers()
            {
                if (Vending.IsValid())
                    Vending.Kill();

                if (Generic.IsValid())
                    Generic.Kill();
            }

            private void OnDestroy() => DestroyMakers();
        }

        #endregion

        #endregion
    }
}
