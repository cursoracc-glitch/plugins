using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;
using ConVar;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("IQReportSystem", "Mercury", "0.1.9")]
    class IQReportSystem : RustPlugin
    {
        /// <summary>
        /// - Поправил , когда игроки с меню в режиме оффлайн пропадали
        /// 
        /// Обновление 0.1.8 :
        /// - Исправил совместимость с IQFakeActive
        /// - Исправил метод с жалобой, когда не был установлен IQFakeActive
        /// - Исправлены страницы
        /// - Исправлен поиск по нику или SteamID 
        /// - Корректирован поиск по игрокам , теперь он ищет не зависимо от регистра
        /// - Оптимизировал плагин
        /// 
        ///  Обновление 0.1.9 :
        ///  - Обновлены страницы
        ///  - Поправлена ошибка с стимИд после поиска
        /// </summary>

        #region Reference
        [PluginReference] Plugin GameWerAC, ImageLibrary, MultiFighting, IQChat, Friends, IQPersonal, IQFakeActive;
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public void SetCheck(BasePlayer player) => IQPersonal?.CallHook("API_SET_CHECK", player.userID);
        public void SetBans(BasePlayer player) => IQPersonal?.CallHook("API_SET_BANS", player.userID);
        public void SetScore(ulong UserID, int Amount) => IQPersonal?.CallHook("API_SET_SCORE", UserID, Amount);
        public void RemoveScore(ulong UserID, int Amount) => IQPersonal?.CallHook("API_REMOVE_SCORE", UserID, Amount);

        #region IQFakeActive
        public bool IsFake(ulong userID)
        {
            if (!IQFakeActive) return false;
            if (!config.IQFakeActiveSettings.UseIQFakeActive) return false;

            return (bool)IQFakeActive?.Call("IsFake", userID);
        }
        public string FindFakeName(ulong userID) => (string)IQFakeActive?.Call("FindFakeName", userID);
        public void StartSysncFakeActive() => IQFakeActive?.Call("SyncReserved");
        void SyncReservedFinish(string JSON)
        {
            if (!config.IQFakeActiveSettings.UseIQFakeActive) return;
            List<FakePlayer> ContentDeserialize = JsonConvert.DeserializeObject<List<FakePlayer>>(JSON);
            PlayerBases = ContentDeserialize;

            PrintWarning("IQReportSystem - успешно синхронизирована с IQFakeActive");
            PrintWarning("=============SYNC==================");

            
        }
        public List<FakePlayer> PlayerBases = new List<FakePlayer>();
        public class FakePlayer
        {
            public ulong UserID;
            public string DisplayName;
            public string IQChatPreifx;
            public string IQChatColorChat;
            public string IQChatColorNick;
        }
        #endregion

        #endregion

        #region Vars

        #region Permission
        string PermissionModeration = "moderation.iqreportsystem";
        string PermissionAdmin = "admin.iqreportsystem";
        #endregion

        #region Lists
        public Dictionary<ulong, int> CooldownPC = new Dictionary<ulong, int>();
        public Dictionary<ulong, PlayerSaveCheckClass> PlayerSaveCheck = new Dictionary<ulong, PlayerSaveCheckClass>();
        public class PlayerSaveCheckClass
        {
            public string Discord;
            public string NickName;
            public string StatusNetwork;

            public ulong ModeratorID;
        }
        #endregion

        #region JSON
        private class Response
        {
            public List<string> last_ip;
            public string last_nick;
            public List<ulong> another_accs;
            public List<last_checks> last_check;
            public class last_checks
            {
                public ulong moderSteamID;
                public string serverName;
                public int time;
            }
            public List<RustCCBans> bans;
            public class RustCCBans
            {
                public int banID;
                public string reason;
                public string serverName;
                public int OVHserverID;
                public int banDate;
            }
        }
        #endregion

        #endregion

        #region Configuration
        private static Configuration config = new Configuration();
        public class Configuration
        {
            [JsonProperty("Основные настройки")]
            public Settings Setting = new Settings();
            [JsonProperty("Причины репорта")]
            public List<string> ReasonReport = new List<string>();
            [JsonProperty("Причины блокировки")] 
            public List<BanReason> ReasonBan = new List<BanReason>();
            [JsonProperty("Настройки RustCheatCheck(Будет при проверке выдавать доступ в чекер и выводить информацию модератору)")]
            public RCCSettings RCCSetting = new RCCSettings();
            [JsonProperty("Настройка репутации для проверяющих")]
            public RaitingSettings RaitingSetting = new RaitingSettings();
            [JsonProperty("Настройка совместной работы с IQFakeActive")]
            public IQFakeActive IQFakeActiveSettings = new IQFakeActive();
            internal class BanReason
            {
                [JsonProperty("Название")]
                public string DisplayName;
                [JsonProperty("Команда")]
                public string Command;
            }
            internal class RCCSettings
            {
                [JsonProperty("Включить поддержку RCC")]
                public bool RCCUse;
                [JsonProperty("Ключ от RCC")]
                public string Key;
            }
            internal class RaitingSettings
            {
                [JsonProperty("Сколько репутации снимать за 1-2 звезды(IQPersonal)")]
                public int RemoveAmountOneTwo;
                [JsonProperty("Сколько репутации давать за 3-4 звезды(IQPersonal)")]
                public int GiveAmountThreeFour;
                [JsonProperty("Сколько репутации давать за 5 звезд(IQPersonal)")]
                public int GiveAmountFive;
            }
            internal class IQFakeActive
            {
                [JsonProperty("Использовать IQFakeActive")]
                public bool UseIQFakeActive;
            }
            internal class Settings
            {
                [JsonProperty("Настройки IQChat")]
                public ChatSettings ChatSetting = new ChatSettings();
                [JsonProperty("Настройки интерфейса")]
                public InterfaceSetting Interface = new InterfaceSetting();

                [JsonProperty("Включить/отключить общее оповоещение для всех игроков(если игрока вызвали на проверку или вынесли вердикт,сообщение будет видно всем)")]
                public bool UseAlertUsers;

                [JsonProperty("Включить/отключить оповещение о максимальном кол-во репортов")]
                public bool MaxReportAlert;
                [JsonProperty("Максимальное количество репортов")]
                public int MaxReport;
                [JsonProperty("Перезарядка для отправки репорта(секунды)")]
                public int CooldownTime;
                [JsonProperty("Запретить друзьям репортить друг друга")]
                public bool FriendNoReport;
                [JsonProperty("Включить логирование в беседу ВК")]
                public bool VKMessage;
                [JsonProperty("Включить логирование в Discord")]
                public bool DiscrodMessage;
                [JsonProperty("Webhooks для дискорда")]
                public string WebHook;
                [JsonProperty("Настройки ВК")]
                public VKSetting VKSettings = new VKSetting();

                internal class ChatSettings
                {
                    [JsonProperty("IQChat : Кастомный префикс в чате")]
                    public string CustomPrefix;
                    [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
                    public string CustomAvatar;
                }
                internal class VKSetting
                {
                    [JsonProperty("Токен от группы ВК(От группы будут идти сообщения в беседу.Вам нужно добавить свою группу в беседу!)")]
                    public string Token;
                    [JsonProperty("ID беседы для группы")]
                    public string ChatID;
                }
                internal class InterfaceSetting
                {
                    [JsonProperty("Настройка интерфейса для отправки жалобы")]
                    public ReasonInterfaceSetting ReasonInterface = new ReasonInterfaceSetting();
                    [JsonProperty("Настройка интерфейса для уведомления")]
                    public AlertInterfaceSettings AlertInterface = new AlertInterfaceSettings(); 
                    [JsonProperty("Настройка интерфейса для мини-панели модератора")]
                    public ModeratorPanelInterfaceSettings ModderatorPanel = new ModeratorPanelInterfaceSettings(); 
                    [JsonProperty("Настройка интерфейса для рейтинга")]
                    public RaitingInterfaceSettings RaitingInterface = new RaitingInterfaceSettings();
                    [JsonProperty("Sprite для рейтинга(звезды)")]
                    public string SpriteRaiting;
                    [JsonProperty("Sprite для кнопки жалоб в панели модератора")]
                    public string SpriteReportModeration;
                    [JsonProperty("Sprite для иконки жалоб")]
                    public string SpriteReport;
                    [JsonProperty("Цвет текста в плагине")]
                    public string HexLabels;
                    [JsonProperty("Цвет боковой панели")]
                    public string HexRightMenu;
                    [JsonProperty("Цвет кнопок боковой панели")]
                    public string HexButtonRightMenu;
                    [JsonProperty("Цвет основной панели")]
                    public string HexMainPanel;
                    [JsonProperty("Цвет панели поиска и заднего фона игроков")]
                    public string HexSearchPanel; 
                    [JsonProperty("Цвет кнопки у игрока для перехода к действию")]
                    public string HexPlayerButton;
                    [JsonProperty("Sprite кнопки у игрока для преехода к действию")]
                    public string SpritePlayerButton;

                    internal class ReasonInterfaceSetting
                    {
                        [JsonProperty("Цвет основной панели")]
                        public string HexMain;
                        [JsonProperty("Цвет панели с заголовком")]
                        public string HexTitlePanel;
                        [JsonProperty("Цвет жалоб")]
                        public string HexButton;
                        [JsonProperty("Цвет текста с жалобами")]
                        public string HexLabel;
                        [JsonProperty("Sprite кнопки закрыть")]
                        public string SpriteClose;             
                        [JsonProperty("Sprite панели с заголовком")]
                        public string SpriteTitlePanel;
                        [JsonProperty("Цвет кнопки закрыть")]
                        public string HexClose;
                    }
                    internal class AlertInterfaceSettings
                    {
                        [JsonProperty("Цвет основной панели")]
                        public string HexMain;
                        [JsonProperty("Цвет заголовка и полоски")]
                        public string HexTitle;
                        [JsonProperty("Цвет текста")]
                        public string HexLabel;
                    }
                    internal class ModeratorPanelInterfaceSettings
                    {
                        [JsonProperty("Цвет основной панели")]
                        public string HexMain;
                        [JsonProperty("Цвет панели с заголовком")]
                        public string HexTitlePanel;
                        [JsonProperty("Sprite панели с заголовком")]
                        public string SpriteTitlePanel;
                        [JsonProperty("Цвет текста")]
                        public string HexLabel;
                        [JsonProperty("Цвет кнопки вердикт и задний фон причин")]
                        public string HexBanButton;
                        [JsonProperty("Цвет кнопки окончания проверки")]
                        public string HexStopButton;
                    }
                    internal class RaitingInterfaceSettings
                    {
                        [JsonProperty("Цвет основной панели")]
                        public string HexMain;
                        [JsonProperty("Цвет панели с заголовком")]
                        public string HexTitlePanel;
                        [JsonProperty("Sprite панели с заголовком")]
                        public string SpriteTitlePanel;
                        [JsonProperty("Цвет текста")]
                        public string HexLabel;
                        [JsonProperty("Цвет иконок с рейтингом")]
                        public string HexRaitingButton;
                        [JsonProperty("Sprite рейтинга")]
                        public string SpriteRaiting;
                    }
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    Setting = new Settings
                    {
                        UseAlertUsers = true,
                        FriendNoReport = false,
                        MaxReport = 3,
                        MaxReportAlert = true,
                        CooldownTime = 600,
                        VKMessage = true,
                        DiscrodMessage = false,
                        WebHook = "",
                        ChatSetting = new Settings.ChatSettings
                        {
                            CustomAvatar = "",
                            CustomPrefix = ""
                        },
                        VKSettings = new Settings.VKSetting
                        {
                            Token = "",
                            ChatID = ""
                        },
                        Interface = new Settings.InterfaceSetting
                        {
                            SpriteRaiting = "assets/icons/favourite_servers.png",
                            SpriteReportModeration = "assets/icons/subtract.png",
                            SpriteReport = "assets/icons/examine.png",
                            SpritePlayerButton = "assets/icons/vote_up.png",
                            HexPlayerButton = "#45542BFF",
                            HexLabels = "#DAD1C7FF",
                            HexButtonRightMenu = "#802A2AFF",
                            HexMainPanel = "#21211AF2",
                            HexRightMenu = "#762424FF",
                            HexSearchPanel = "#3B3D37FF",
                            ReasonInterface = new Settings.InterfaceSetting.ReasonInterfaceSetting
                            {
                                HexMain = "#585450FF",
                                HexTitlePanel = "#54514DFF",
                                HexButton = "#3E482EFF",
                                HexClose = "#B4371EFF",
                                HexLabel = "#bdd197",
                                SpriteClose = "assets/icons/vote_down.png",
                                SpriteTitlePanel = "assets/icons/connection.png"
                            },
                            AlertInterface = new Settings.InterfaceSetting.AlertInterfaceSettings
                            {
                                HexMain = "#21211AF2",
                                HexLabel = "#DAD1C7FF",
                                HexTitle = "#B4371EFF",
                            },
                            ModderatorPanel = new Settings.InterfaceSetting.ModeratorPanelInterfaceSettings
                            {
                                HexMain = "#575450FF",
                                HexTitlePanel = "#54514DFF",
                                HexLabel = "#DAD1C7FF",
                                SpriteTitlePanel = "assets/icons/study.png",
                                HexBanButton = "#B4371EFF",
                                HexStopButton = "#3E482EFF"
                            },
                            RaitingInterface = new Settings.InterfaceSetting.RaitingInterfaceSettings
                            {
                                HexMain = "#575450FF",
                                HexTitlePanel = "#54514DFF",
                                HexLabel = "#DAD1C7FF",
                                HexRaitingButton = "#cdb980",
                                SpriteTitlePanel = "assets/icons/ignite.png",
                                SpriteRaiting = "assets/icons/favourite_servers.png"
                            }
                        }
                    },
                    ReasonReport = new List<string>
                    {
                        "Использование читов",
                        "Макросы",
                        "Игра 3+",                      
                    },
                    ReasonBan = new List<BanReason>
                    { 
                        new BanReason
                        {
                            DisplayName = "Использование читов",
                            Command = "ban {0} soft",
                        },
                        new BanReason
                        {
                            DisplayName = "Макросы",
                            Command = "ban {0} 30d macros",
                        },
                        new BanReason
                        {
                            DisplayName = "Игра 3+",
                            Command = "ban {0} 14d 3+",
                        },
                        new BanReason
                        {
                            DisplayName = "Отказ",
                            Command = "ban {0} 7d otkaz",
                        },
                    },
                    RCCSetting = new RCCSettings
                    {
                        RCCUse = false,
                        Key = "xxxxxxxxxxxxxxRCCKey",
                    },
                    IQFakeActiveSettings = new IQFakeActive
                    {
                      UseIQFakeActive = false,
                    },
                    RaitingSetting = new RaitingSettings
                    {
                        RemoveAmountOneTwo = 4,
                        GiveAmountThreeFour = 3,
                        GiveAmountFive = 5
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
                PrintWarning($"Ошибка чтения конфигурации #93 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        public Dictionary<ulong, PlayerInfo> ReportInformation = new Dictionary<ulong, PlayerInfo>();
        public Dictionary<ulong, ModeratorInfo> ModeratorInformation = new Dictionary<ulong, ModeratorInfo>();
        public class PlayerInfo
        {
            [JsonProperty("Отображаемое имя")]
            public string DisplayName;
            [JsonProperty("IP Адреса")]
            public List<string> IP;
            [JsonProperty("Последняя жалоба")]
            public string LastReport;
            [JsonProperty("Последний проверяющий модератор")]
            public string LastCheckModerator;
            [JsonProperty("Количество проверок")]
            public int CheckCount;
            [JsonProperty("История жалоб")]
            public List<string> ReportHistory;
            [JsonProperty("Количество жалоб")]
            public int ReportCount;
            [JsonProperty("Игровой статус")]
            public string GameStatus;
        }

        public class ModeratorInfo
        {
            [JsonProperty("Проверки игроков с вердиктами")]
            public Dictionary<string, string> CheckPlayerModerator = new Dictionary<string, string>();
            [JsonProperty("Блокировки игроков с вердиктом")]
            public Dictionary<string, string> BanPlayerModerator = new Dictionary<string, string>();
            [JsonProperty("Общее количество проверок")]
            public int CheckCount;
            [JsonProperty("История оценок модератора")]
            public List<int> Arrayrating;
            [JsonProperty("Средняя оценка качества")]
            public float AverageRating;
        }
        #endregion

        #region Metods

        #region MetodsReport

        void Metods_PlayerConnected(BasePlayer player)
        {
            if (player == null)
                return;
            if (!ReportInformation.ContainsKey(player.userID))
            {
                PlayerInfo pInfo = new PlayerInfo
                {
                    DisplayName = player.displayName,
                    IP = new List<string>(),
                    LastReport = "",
                    LastCheckModerator = "",
                    CheckCount = 0,
                    ReportCount = 0,
                    GameStatus = IsSteam(player.UserIDString),
                    ReportHistory = new List<string>(),
                };
                ReportInformation.Add(player.userID, pInfo);
            }
            else
            {
                var User = ReportInformation[player.userID];
                var IP = covalence.Players.FindPlayerById(player.UserIDString).Address;

                User.GameStatus = IsSteam(player.UserIDString);
                if (!String.IsNullOrWhiteSpace(IP))
                    if (!User.IP.Contains(IP))
                        User.IP.Add(IP);
            }

            if (permission.UserHasPermission(player.UserIDString, PermissionModeration))
            {
                if (!ModeratorInformation.ContainsKey(player.userID))
                {
                    ModeratorInfo mInfo = new ModeratorInfo
                    {
                        CheckCount = 0,
                        BanPlayerModerator = new Dictionary<string, string>(),
                        CheckPlayerModerator = new Dictionary<string, string>(),
                        Arrayrating = new List<int>(),
                        AverageRating = 0,
                    };
                    ModeratorInformation.Add(player.userID, mInfo);
                }
            }
            else
            {
                if (ModeratorInformation.ContainsKey(player.userID))
                    ModeratorInformation.Remove(player.userID);
            }
            Metods_StatusNetwork(player, lang.GetMessage("NETWORD_STATUS_ONLINE", this, player.UserIDString));
        }

        void Metods_Report(BasePlayer target, int ReasonIndex)
        {
            if (permission.UserHasPermission(target.UserIDString, PermissionAdmin))
                return;

            if (IsSteam(target.UserIDString) == lang.GetMessage("IS_STEAM_STATUS_PIRATE", this, target.UserIDString))
            {
                if (GameWerAC != null)
                {
                    GameWerAC.Call("GetScreenReport", target);
                    Puts("Выполнен скриншот экрана для пирата");
                }
            }

            string ReasonReport = config.ReasonReport[ReasonIndex];

            var User = ReportInformation[target.userID];
            User.ReportCount++;
            User.LastReport = ReasonReport;
            User.ReportHistory.Insert(0, ReasonReport);

            if (config.Setting.MaxReportAlert)
                if (User.ReportCount >= config.Setting.MaxReport)
                {
                    foreach (var MList in BasePlayer.activePlayerList)
                        if (permission.UserHasPermission(MList.UserIDString, PermissionModeration))
                            SendChat(MList, String.Format(lang.GetMessage("METODS_HELP_MODERS", this, MList.UserIDString), target.displayName, User.ReportCount));
                    VKSendMessage(String.Format(lang.GetMessage("METODS_HELP_MODERS_VK", this), target.displayName, User.ReportCount));
                    DiscordSendMessage(String.Format(lang.GetMessage("METODS_HELP_MODERS_VK", this), target.displayName, User.ReportCount));
                }
        }

        #endregion

        #region MetodsCooldown
        void Metods_GiveCooldown(ulong ID,  int cooldown)
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

        #region MetodsModeration

        void Metods_CheckModeration(BasePlayer Suspect, BasePlayer Moderator)
        {
            if (PlayerSaveCheck.ContainsKey(Suspect.userID))
            {
                SendChat(Moderator, lang.GetMessage("PLAYER_CHECKED", this));
                return;
            }
            else PlayerSaveCheck.Add(Suspect.userID, new PlayerSaveCheckClass {  });
            SendChat(Moderator, String.Format(lang.GetMessage("METODS_MODER_START_CHECK",this, Moderator.UserIDString),Suspect.displayName));
            VKSendMessage(String.Format(lang.GetMessage("METODS_MODER_START_CHECK_VK", this),Moderator.displayName,Moderator.UserIDString,Suspect.displayName,Suspect.UserIDString));           
            Metods_AFK(Suspect.userID, Moderator);

            if(config.Setting.UseAlertUsers)
                foreach(var p in BasePlayer.activePlayerList)
                    SendChat(p, String.Format(lang.GetMessage("METODS_MODER_START_CHECK_VK", this, p.UserIDString), Moderator.displayName, Moderator.UserIDString, Suspect.displayName, Suspect.UserIDString));
        }

        void Metods_CheckModerationFinish(BasePlayer moderator, ulong SuspectID)
        {
            IPlayer Suspect = covalence.Players.FindPlayerById(SuspectID.ToString());
            if (Suspect.IsConnected)
            {
                BasePlayer SOnline = BasePlayer.FindByID(ulong.Parse(Suspect.Id));
                CuiHelper.DestroyUi(SOnline, PARENT_UI_ALERT_SEND);
                SendChat(SOnline, lang.GetMessage("MSG_CHECK_CHECK_STOP", this, moderator.UserIDString));
            }

            CuiHelper.DestroyUi(moderator, PARENT_UI_MODERATOR_MINI_PANEL);
            PlayerSaveCheck.Remove(ulong.Parse(Suspect.Id));

            var User = ReportInformation[ulong.Parse(Suspect.Id)];
            var Moderator = ModeratorInformation[moderator.userID];

            Moderator.CheckCount++;
            if (!Moderator.CheckPlayerModerator.ContainsKey(Suspect.Name))
                Moderator.CheckPlayerModerator.Add(Suspect.Name, User.LastReport);

            User.ReportCount = 0;
            User.ReportHistory.Clear();
            User.LastReport = lang.GetMessage("NON_REPORT",this);
            User.CheckCount++;
            User.LastCheckModerator = moderator.displayName;

            SendChat(moderator, lang.GetMessage("METODS_MODER_STOP_CHECK",this, moderator.UserIDString));
            VKSendMessage(String.Format(lang.GetMessage("METODS_MODER_STOP_CHECK_VK",this),moderator.displayName));
            DiscordSendMessage(String.Format(lang.GetMessage("METODS_MODER_STOP_CHECK_VK",this),moderator.displayName));
            SetCheck(moderator);

            if (config.Setting.UseAlertUsers)
                foreach (var p in BasePlayer.activePlayerList)
                    SendChat(p, String.Format(lang.GetMessage("METODS_MODER_STOP_CHECK_ALERT", this, p.UserIDString), moderator.displayName, Suspect.Name));
        }

        void Metods_StatusNetwork(BasePlayer Suspect, string Reason)
        {
            if (Suspect == null) return;
            if (PlayerSaveCheck.ContainsKey(Suspect.userID))
            {
                if (Suspect.IsConnected)
                    if (Suspect.IsReceivingSnapshot)
                    {
                        timer.Once(3, () => Metods_StatusNetwork(Suspect, lang.GetMessage("NETWORD_STATUS_ONLINE", this, Suspect.UserIDString)));
                        return;
                    }

                PlayerSaveCheck[Suspect.userID].StatusNetwork = Reason;
                BasePlayer Moderator = BasePlayer.FindByID(PlayerSaveCheck[Suspect.userID].ModeratorID);

                CuiHelper.DestroyUi(Moderator, UI_MODERATION_CHECK_MENU_NETWORK);
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.6782616 0.5478261", AnchorMax = "0.9884076 0.7333333" },
                    Text = { Text = $"{PlayerSaveCheck[Suspect.userID].StatusNetwork}", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter, Color = HexToRustFormat("#FFFFFFFF") }
                }, PARENT_UI_MODERATOR_MINI_PANEL, UI_MODERATION_CHECK_MENU_NETWORK);

                CuiHelper.AddUi(Moderator, container);
                UI_AlertSendPlayer(Suspect);             

                SendChat(Moderator, String.Format(lang.GetMessage("STATUS_CHANGED", this, Moderator.UserIDString), Suspect.displayName, Reason));
                VKSendMessage(String.Format(lang.GetMessage("STATUS_CHANGED_VK", this), Suspect.displayName, Reason));
                DiscordSendMessage(String.Format(lang.GetMessage("STATUS_CHANGED_VK", this), Suspect.displayName, Reason));
            }
        }

        public Timer ModerTimeOutTimer;
        void Metods_ModeratorExitCheck(BasePlayer Moderator)
        {
            foreach (var ModeratorCritical in PlayerSaveCheck)
                if (ModeratorCritical.Value.ModeratorID == Moderator.userID)
                {
                    IPlayer ModeratorOffline = covalence.Players.FindPlayerById(ModeratorCritical.Value.ModeratorID.ToString());
                    IPlayer Suspect = covalence.Players.FindPlayerById(ModeratorCritical.Key.ToString());
                    int TimeOutCount = 0;
                    ModerTimeOutTimer = timer.Repeat(5, 10, () =>
                        {
                            if (ModeratorOffline.IsConnected)
                            {
                                UI_MiniPanelModerator(Moderator, ModeratorCritical.Key);
                                SendChat(Moderator, lang.GetMessage("MODERATOR_RETURN_WELCOME",this, Moderator.UserIDString));
                                if (ModerTimeOutTimer != null)
                                {
                                    ModerTimeOutTimer.Destroy();
                                    ModerTimeOutTimer = null;
                                }
                                return;
                            }
                            else
                            {
                                TimeOutCount++;
                                if (TimeOutCount >= 10)
                                {
                                    PlayerSaveCheck.Remove(ModeratorCritical.Key);

                                    foreach (var OnlineModeration in BasePlayer.activePlayerList)
                                        if (permission.UserHasPermission(OnlineModeration.UserIDString, PermissionModeration))
                                            if (Suspect.IsConnected)
                                            {                                             
                                                BasePlayer SOnline = BasePlayer.FindByID(ulong.Parse(Suspect.Id));
                                                CuiHelper.DestroyUi(SOnline, PARENT_UI_ALERT_SEND);
                                           
                                                SendChat(SOnline, String.Format(lang.GetMessage("MODERATOR_DISCONNECTED_STOP_CHECK",this, Moderator.UserIDString),ModeratorOffline.Name));
                                                SendChat(OnlineModeration, String.Format(lang.GetMessage("MODERATOR_DISCONNECTED_STOP_RESEND",this, Moderator.UserIDString),ModeratorOffline.Name,Suspect.Name));
                                                VKSendMessage(String.Format(lang.GetMessage("MODERATOR_DISCONNECTED_STOP_RESEND", this), ModeratorOffline.Name, Suspect.Name));
                                                DiscordSendMessage(String.Format(lang.GetMessage("MODERATOR_DISCONNECTED_STOP_RESEND", this), ModeratorOffline.Name, Suspect.Name));

                                                if (ModerTimeOutTimer != null)
                                                {
                                                    ModerTimeOutTimer.Destroy();
                                                    ModerTimeOutTimer = null;
                                                }
                                            }
                                    return;
                                }
                            }
                        });

                }
        }

        void Metods_ModeratorBanned(BasePlayer Moderator,ulong SuspectID, int i)
        {
            CuiHelper.DestroyUi(Moderator, PARENT_UI_MODERATOR_MINI_PANEL);
            IPlayer Suspect = covalence.Players.FindPlayerById(SuspectID.ToString());
            string Reason = config.ReasonBan[i].DisplayName;

            rust.RunClientCommand(Moderator, String.Format(config.ReasonBan[i].Command, SuspectID));
            PlayerSaveCheck.Remove(SuspectID);

            var ModeratorInfo = ModeratorInformation[Moderator.userID];
            ModeratorInfo.CheckCount++;
            if (!ModeratorInfo.CheckPlayerModerator.ContainsKey(Suspect.Name))
                ModeratorInfo.CheckPlayerModerator.Add(Suspect.Name, Reason);
            if (!ModeratorInfo.BanPlayerModerator.ContainsKey(Suspect.Name))
                ModeratorInfo.BanPlayerModerator.Add(Suspect.Name, Reason);

            SendChat(Moderator, String.Format(lang.GetMessage("MODERATOR_COMPLETED_CHECK", this, Moderator.UserIDString), Reason));
            VKSendMessage(String.Format(lang.GetMessage("MODERATOR_COMPLETED_CHECK_VK", this), Moderator.displayName, Moderator.UserIDString, Suspect.Name, SuspectID, Reason, AFKCheck[SuspectID]));
            DiscordSendMessage(String.Format(lang.GetMessage("MODERATOR_COMPLETED_CHECK_VK", this), Moderator.displayName, Moderator.UserIDString, Suspect.Name, SuspectID, Reason, AFKCheck[SuspectID]));
            SetBans(Moderator);

            if (config.Setting.UseAlertUsers)
                foreach (var p in BasePlayer.activePlayerList)
                    SendChat(p, String.Format(lang.GetMessage("MODERATOR_COMPLETED_CHECK_ALERT", this, p.UserIDString), Moderator.displayName, Suspect.Name, Reason));
        }

        #endregion

        #region MetodsAFK
        void Metods_CheckStopInAFK(BasePlayer moderator, string ID)
        {
            IPlayer Suspect = covalence.Players.FindPlayerById(ID);
            if (Suspect.IsConnected)
            {
                BasePlayer SOnline = BasePlayer.FindByID(ulong.Parse(Suspect.Id));
                CuiHelper.DestroyUi(SOnline, PARENT_UI_ALERT_SEND);
            }
            CuiHelper.DestroyUi(moderator, PARENT_UI_MODERATOR_MINI_PANEL);
            PlayerSaveCheck.Remove(ulong.Parse(Suspect.Id));

            SendChat(moderator, lang.GetMessage("PLAYER_AFK_CHECK_STOP",this));
            VKSendMessage(String.Format(lang.GetMessage("PLAYER_AFK_CHECK_STOP_VK", this), moderator.displayName, moderator.userID, Suspect.Name));
            DiscordSendMessage(String.Format(lang.GetMessage("PLAYER_AFK_CHECK_STOP_VK", this), moderator.displayName, moderator.userID, Suspect.Name));
        }

        public Dictionary<ulong,int> AFKCheck = new Dictionary<ulong, int>();
        void Metods_AFK(ulong SuspectID, BasePlayer moderator)
        {
            IPlayer Suspect = covalence.Players.FindPlayerById(SuspectID.ToString());
            if (!AFKCheck.ContainsKey(SuspectID))
                AFKCheck.Add(SuspectID, 0);
            else AFKCheck[SuspectID] = 0;

            int tryAFK = 0;
            SavePositionAFK(Suspect, moderator, tryAFK);
            timer.Repeat(5f, 6, () =>
            {
                SavePositionAFK(Suspect, moderator,tryAFK);
                tryAFK++;
            });
        }

        readonly Hash<string, GenericPosition> lastPosition = new Hash<string, GenericPosition>();
        void SavePositionAFK(IPlayer Suspect, BasePlayer moderator, int num)
        {
            var pPosition = Suspect.Position();
            if (!lastPosition.ContainsKey(Suspect.Id))
                lastPosition.Add(Suspect.Id, pPosition);
            else
            {
                if (lastPosition[Suspect.Id] != pPosition)
                    SendChat(moderator, String.Format(lang.GetMessage("PLAYER_AFK_CHANGE_POS",this, moderator.UserIDString),num));
                else
                {
                    SendChat(moderator, String.Format(lang.GetMessage("PLAYER_AFK_CHANGE_NO_POS", this, moderator.UserIDString), num));
                    AFKCheck[ulong.Parse(Suspect.Id)] += 1;
                }
                lastPosition[Suspect.Id] = pPosition;
            }

            if (num >= 5)
            {
                if (AFKCheck[ulong.Parse(Suspect.Id)] >= 3)
                    Metods_CheckStopInAFK(moderator, Suspect.Id);
                else
                {
                    BasePlayer SuspectOnline = BasePlayer.FindByID(ulong.Parse(Suspect.Id));
                    UI_AlertSendPlayer(SuspectOnline);
                    PlayerSaveCheck = new Dictionary<ulong, PlayerSaveCheckClass>
                    {
                        [SuspectOnline.userID] = new PlayerSaveCheckClass
                        {
                            Discord = lang.GetMessage("DISCORD_NULL",this),
                            NickName = SuspectOnline.displayName,
                            StatusNetwork = lang.GetMessage("NETWORD_STATUS_ONLINE", this, SuspectOnline.UserIDString),

                            ModeratorID = moderator.userID,
                        }
                    };
                    UI_MiniPanelModerator(moderator, SuspectOnline.userID);
                    SendChat(moderator, lang.GetMessage("PLAYER_NON_AFK", this, moderator.UserIDString));
                    
                    if(config.RCCSetting.RCCUse)
                    {
                        string Key = config.RCCSetting.Key;
                        if (String.IsNullOrEmpty(Key)) return;
                        try
                        {
                            string API = $"https://rustcheatcheck.ru/panel/api?action=addPlayer&key={Key}&player={Suspect.Id}";
                            webrequest.Enqueue(API, null, (code, response) => { }, this);
                        }
                        catch { }
                    }
                }
            }
        }

        #endregion

        #endregion

        #region Command

        #region UseCommand
        [ChatCommand("report")]
        void ReportChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            if (args == null || args.Length == 0)
            {
                rust.RunClientCommand(player, "custommenu true Report");
                return;
            }
        }

        [ConsoleCommand("report.list")]
        void ReportList(ConsoleSystem.Arg arg)
        {
            PrintError(lang.GetMessage("REPORT_LIST_CONSOLE",this));
            foreach (var List in BasePlayer.activePlayerList)
                if (ReportInformation[List.userID].ReportCount >= config.Setting.MaxReport)
                    PrintError($"{List.displayName} : {ReportInformation[List.userID].ReportCount}");

        }

        [ChatCommand("discord")]
        void SendDiscord(BasePlayer Suspect, string command, string[] args)
        {
            if (!PlayerSaveCheck.ContainsKey(Suspect.userID))
            {
                SendChat(Suspect, lang.GetMessage("MSG_CHECK_DISCORD", this, Suspect.UserIDString));
                return;
            }
            string Discord = "";
            foreach (var arg in args)
                Discord += " " + arg;

            PlayerSaveCheck[Suspect.userID].Discord = Discord;

            SendChat(Suspect, String.Format(lang.GetMessage("MSG_DISCORD_SEND", this, Suspect.UserIDString),Discord));
            VKSendMessage(String.Format(lang.GetMessage("DISCROD_VK_SEND", this), Suspect.displayName, Suspect.userID, Discord));
            DiscordSendMessage(String.Format(lang.GetMessage("DISCROD_VK_SEND", this), Suspect.displayName, Suspect.userID, Discord));

            BasePlayer Moderator = BasePlayer.FindByID(PlayerSaveCheck[Suspect.userID].ModeratorID);
            CuiHelper.DestroyUi(Moderator, UI_MODERATION_CHECK_MENU_DISCORD);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01159436 0.5478261", AnchorMax = "0.7072465 0.7333333" },
                Text = { Text = $"Discord : {PlayerSaveCheck[Suspect.userID].Discord}", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter, Color = HexToRustFormat("#FFFFFFFF") }
            }, PARENT_UI_MODERATOR_MINI_PANEL, UI_MODERATION_CHECK_MENU_DISCORD);

            CuiHelper.AddUi(Moderator, container);
        }
       
        [ConsoleCommand("call")]
        void CallAdminCheck(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (!player.IsAdmin) return;
            ulong SuspectID = ulong.Parse(arg.Args[0]);
            if(player == null)
            {
                PrintWarning("Вы должны быть на сервере");
                SendChat(player, "Вы должны быть на сервере");
                return;
            }
            BasePlayer Suspect = BasePlayer.FindByID(SuspectID);
            if(Suspect == null)
            {
                PrintWarning("Игрока нет");
                SendChat(player, "Игрока нет");
                return;
            }
            Metods_CheckModeration(Suspect, player);
            Puts("Вы вызвали игрока на проверку"); 
            SendChat(player, "Вы вызвали игрока на проверку");
        }

        [ConsoleCommand("report")]
        void ReportCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (arg == null || arg.Args == null || arg.Args.Length == 0)
            {
                if (player == null) return;
                rust.RunClientCommand(player, "custommenu true Report");
                return;
            }

            switch (arg.Args[0].ToLower())
            {
                case "give":
                    {
                        if (arg.Args.Length != 3)
                        {
                            PrintWarning("Используйте правильный синтаксис : report give SteamID Amount");
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(arg.Args[1]))
                        {
                            PrintWarning("Укажите корректный Steam64ID");
                            return;
                        }
                        if (!arg.Args[1].IsSteamId())
                        {
                            PrintWarning("Укажите корректный Steam64ID");
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(arg.Args[2]))
                        {
                            PrintWarning("Укажите корректное количество");
                            return;
                        }

                        ReportInformation[ulong.Parse(arg.Args[1])].ReportCount += Convert.ToInt32(arg.Args[2]);
                        if (player != null)
                        {
                            VKSendMessage(String.Format(lang.GetMessage("CONSOLE_REPORT_GIVE", this), arg.Args[1], arg.Args[2], ReportInformation[ulong.Parse(arg.Args[1])].ReportCount));
                            DiscordSendMessage(String.Format(lang.GetMessage("CONSOLE_REPORT_GIVE", this), arg.Args[1], arg.Args[2], ReportInformation[ulong.Parse(arg.Args[1])].ReportCount));
                        }
                        PrintWarning("ACCESS");
                        return;
                    }
                case "remove":
                    {
                        if (arg.Args.Length != 3)
                        {
                            PrintWarning("Используйте правильный синтаксис : report remove SteamID Amount");
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(arg.Args[1]))
                        {
                            PrintWarning("Укажите корректный Steam64ID");
                            return;
                        }
                        if (!arg.Args[1].IsSteamId())
                        {
                            PrintWarning("Укажите корректный Steam64ID");
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(arg.Args[2]))
                        {
                            PrintWarning("Укажите корректное количество");
                            return;
                        }
                        ReportInformation[ulong.Parse(arg.Args[1])].ReportCount -= Convert.ToInt32(arg.Args[2]);
                        if (player != null)
                        {
                            VKSendMessage(String.Format(lang.GetMessage("CONSOLE_REPORT_REMOVE", this), arg.Args[1], arg.Args[2], ReportInformation[ulong.Parse(arg.Args[1])].ReportCount));
                        }
                        PrintWarning("ACCESS");
                        return;
                    }
            }
        }

        #endregion

        #region FuncCommand
        [ConsoleCommand("iqreport")]
        void IQReportSystemCommands(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            string Key = arg.Args[0].ToLower();

            switch (Key)
            {
                case "page":
                    {
                        string PageAction = arg.Args[1];
                        bool Moderation = Convert.ToBoolean(arg.Args[2]);
                        int Page = Convert.ToInt32(arg.Args[3]);
                        switch (PageAction)
                        {
                            case "next":
                                {
                                    UI_Player_Loaded(player, Moderation, Page + 1);
                                    break;
                                }
                            case "back":
                                {
                                    UI_Player_Loaded(player, Moderation, Page - 1);
                                    break;
                                }
                        }
                        break;
                    };
                case "moderation_menu":
                    {
                        UI_PanelReportsPlayer(player, true);
                        break;
                    };
                case "reports_menu":
                    {
                        //  int BaseID = int.Parse(arg.Args[1]);
                        
                        ulong UserID = ulong.Parse(arg.Args[1]);
                        UI_SendReport(player, UserID);
                        break;
                    }
                case "moderation_send":
                    {
                        ulong SuspectID = ulong.Parse(arg.Args[1]);
                        BasePlayer Suspect = BasePlayer.FindByID(SuspectID);

                        UI_ModerReport(player, Suspect);
                        break;
                    }
                case "send_report":
                    {
                        ulong SuspectID = ulong.Parse(arg.Args[1]);
                        int IndexReason = Convert.ToInt32(arg.Args[2]);
                        string ReasonReport = config.ReasonReport[IndexReason];

                        if (player == null) return;
                        

                        if (Metods_GetCooldown(player.userID) == true)
                        {
                            SendChat(player, String.Format(lang.GetMessage("MSG_COOLDOWN", this, player.UserIDString), FormatTime(TimeSpan.FromSeconds(Math.Max(0, CooldownPC[player.userID] - CurrentTime())))));
                            CuiHelper.DestroyUi(player, "XMenu");
                            return;
                        }
                        
                        if(IsFake(SuspectID))
                        {
                            Metods_GiveCooldown(player.userID, config.Setting.CooldownTime);
                            SendChat(player, String.Format(lang.GetMessage("MSG_REPORTED_SUSPECT", this, player.UserIDString), FindFakeName(SuspectID), ReasonReport));
                            CuiHelper.DestroyUi(player, "XMenu");
                            return;
                        }
                        BasePlayer Suspect = BasePlayer.FindByID(SuspectID);
                        if (Suspect == null) return;

                        Metods_Report(Suspect, IndexReason);
                        Metods_GiveCooldown(player.userID, config.Setting.CooldownTime);
                        SendChat(player, String.Format(lang.GetMessage("MSG_REPORTED_SUSPECT", this, player.UserIDString), Suspect.displayName, ReasonReport));
                        VKSendMessage(String.Format(lang.GetMessage("METODS_SEND_REPORT_VK", this), Suspect.displayName, Suspect.UserIDString, ReasonReport, player.displayName, player.userID));
                        DiscordSendMessage(String.Format(lang.GetMessage("METODS_SEND_REPORT_VK", this), Suspect.displayName, Suspect.UserIDString, ReasonReport, player.displayName, player.userID));
                        CuiHelper.DestroyUi(player, "XMenu");
                        break;
                    }
                case "search":
                    {
                        if (arg.Args.Length != 4) return;

                        bool Moderation = Convert.ToBoolean(arg.Args[1]);
                        int Page = Convert.ToInt32(arg.Args[2]);
                        string SearchSay = arg.Args[3];

                        UI_Player_Loaded(player, Moderation, Page, SearchSay);
                        break;
                    }
                case "send_check":
                    {
                        if (player == null) return;
                        ulong SuspectID = ulong.Parse(arg.Args[1]);
                        BasePlayer Suspect = BasePlayer.FindByID(SuspectID);
                        if (permission.UserHasPermission(player.UserIDString, PermissionModeration))
                            Metods_CheckModeration(Suspect, player);

                        break;
                    }
                case "moderator_reason_ban":
                    {
                        if (player == null) return;
                        ulong SuspectID = ulong.Parse(arg.Args[1]);
                        BasePlayer Suspect = BasePlayer.FindByID(SuspectID);
                        if (permission.UserHasPermission(player.UserIDString, PermissionModeration))
                            UI_OpenReasonsBan(player, SuspectID);
                        break;
                    }
                case "moderator_ban":
                    {
                        if (player == null) return;
                        ulong SuspectID = ulong.Parse(arg.Args[1]);
                        BasePlayer Suspect = BasePlayer.FindByID(SuspectID);
                        int Index = Convert.ToInt32(arg.Args[2]);
                        Metods_ModeratorBanned(player, SuspectID, Index);
                        break;
                    }
                case "moderator_stop":
                    {
                        if (player == null) return;
                        ulong SuspectID = ulong.Parse(arg.Args[1]);
                        BasePlayer Suspect = BasePlayer.FindByID(SuspectID);
                        if (permission.UserHasPermission(player.UserIDString, PermissionModeration))
                            Metods_CheckModerationFinish(player, SuspectID);
                        UI_RaitingSend(Suspect, player);
                        return;
                    }
                case "raiting": 
                    {
                        BasePlayer Moderator = BasePlayer.FindByID(ulong.Parse(arg.Args[1]));
                        int Raiting = Convert.ToInt32(arg.Args[2]);
                        var RaitingModerator = ModeratorInformation[Moderator.userID].Arrayrating;

                        VKSendMessage(String.Format(lang.GetMessage("UI_RAITING_MODERATION_VK_GIVE", this), player.displayName, player.UserIDString, Moderator.displayName, Moderator.UserIDString, Raiting));
                        DiscordSendMessage(String.Format(lang.GetMessage("UI_RAITING_MODERATION_VK_GIVE", this), player.displayName, player.UserIDString, Moderator.displayName, Moderator.UserIDString, Raiting));
                        SendChat(player, lang.GetMessage("UI_RAITING_MODERATION_VK_GIVE_THX", this, player.UserIDString));

                        RaitingModerator.Add(Raiting);

                        if (Raiting <= 2)
                        {
                            RemoveScore(Moderator.userID, config.RaitingSetting.RemoveAmountOneTwo);
                            SendChat(Moderator, String.Format(lang.GetMessage("UI_RAIT_ALERT_MODER", this, Moderator.UserIDString), Raiting, $"-{config.RaitingSetting.RemoveAmountOneTwo}"));
                            return;
                        }
                        if (Raiting > 2 && Raiting < 5)
                        {
                            SetScore(Moderator.userID, config.RaitingSetting.GiveAmountThreeFour);
                            SendChat(Moderator, String.Format(lang.GetMessage("UI_RAIT_ALERT_MODER", this, Moderator.UserIDString), Raiting, $"{config.RaitingSetting.GiveAmountThreeFour}"));
                            return;
                        }
                        if (Raiting >= 5)
                        {
                            SetScore(Moderator.userID, config.RaitingSetting.GiveAmountFive);
                            SendChat(Moderator, String.Format(lang.GetMessage("UI_RAIT_ALERT_MODER", this, Moderator.UserIDString), Raiting, $"{config.RaitingSetting.GiveAmountFive}"));
                            return;
                        }
                        break;
                    }
            }
        }
        #endregion

        public float GetAverageRaiting(ulong userID)
        {
            var Data = ModeratorInformation[userID];
            float AverageRaiting = Data.AverageRating;
            int RaitingFull = 0;
            for(int i = 0; i < Data.Arrayrating.Count; i++)
                RaitingFull += Data.Arrayrating[i];

            int FormulDivision = Data.Arrayrating.Count == 0 ? 1 : Data.Arrayrating.Count;
            AverageRaiting = RaitingFull / FormulDivision;
            return AverageRaiting;
        }

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PLAYER_CHECKED"] = "The player had already check!",

                ["MSG_REPORTED_SUSPECT"] = "You have successfully submitted a player report - {0}\nReported : {1}\nThe moderator will review your complaint as soon as possible!",
                ["MSG_CHECK_DISCORD"] = "You can't send Discord without checking!",
                ["MSG_CHECK_CHECK_STOP"] = "You have successfully passed the test!\nWe wish you a pleasant game on our server!",
                ["MSG_COOLDOWN"] = "You have recently sent a complaint!\nWait <color=#47AF5DFF>{0}</color>",
                ["MSG_DISCORD_SEND"] = "You have successfully submitted the information!\nDiscord - {0}\nExpect a call from the moderator",

                ["UI_MODERATOR_PANEL_TITLE"] = "Server moderator menu",
                ["UI_MODERATOR_PANEL_DESCRIPTION"] = "This list shows players who have reached the limit of complaints,click on the player to get more information",
                ["UI_MODERATOR_PANEL_START_CHECK"] = "Call for verification",
                ["UI_STATUS"] = "Status",

                ["NETWORD_STATUS_ONLINE"] = "Online",
                ["IS_STEAM_STATUS_PIRATE"] = "Pirate",
                ["IS_STEAM_STATUS_LICENSE"] = "License",

                ["METODS_SEND_REPORT_VK"] = "[IQReportSystem]\nA complaint has been sent to player {0} ({1})!\nComplaint - {2}\nReporter : {3}({4})",
                ["METODS_HELP_MODERS"] = "Player <color=#47AF5DFF>{0}</color> reached the limit of reports!\nThe number of his reports - <color=#47AF5DFF>{1}</color>\nModeration that is free - check the player!",
                ["METODS_HELP_MODERS_VK"] = "[IQReportSystem]\nPlayer {0} reached the limit of reports!\nThe number of his reports - {1}\nModeration that is free - check the player!",
                ["METODS_MODER_START_CHECK"] = "You started checking!\nSuspect - <color=#47AF5DFF>{0}</color>\nGetting started with AFK!\nIf the player is not AFK, they will receive a notification of verification!",
                ["METODS_MODER_START_CHECK_VK"] = "[IQReportSystem]\nModerator {0}({1}) started checking!\nSuspect - {2}({3})",
                ["METODS_MODER_STOP_CHECK"] = "Verification completed.\nHave a nice day!\nDo not forget to check the complaint list!",
                ["METODS_MODER_STOP_CHECK_VK"] = "[IQReportSystem]\nModerator {0} finished checking!",
                ["METODS_MODER_STOP_CHECK_ALERT"] = "Moderator {0} has finished Player check - {1}\nNo illegal found",

                ["NON_REPORT"] = "No complaints",
                ["MODERATOR_RETURN_WELCOME"] = "Welcome back!\nthe check was not canceled, continue!",
                ["STATUS_CHANGED"] = "The player's {0} status has changed to: {1}\n Wait for the player on the server for 10 minutes!\nIf the player does not enter after 10 minutes-issue a ban for Refusal",
                ["STATUS_CHANGED_VK"] = "[IQReportSystem]The player's {0} status has changed to: {1}\n Wait for the player on the server for 10 minutes!\nIf the player does not enter after 10 minutes-issue a ban for Refusal",
                ["MODERATOR_DISCONNECTED_STOP_CHECK"] = "The check was removed!\nModerator {0} left the server\nReason: connection Failure\nWe apologize!\nWe will inform the other moderation!",
                ["MODERATOR_DISCONNECTED_STOP_RESEND"] = "Moderator {0} finally left the server during verification!\n Player {1} is waiting for other moderators to check!",
                ["MODERATOR_COMPLETED_CHECK"] = "You successfully completed the review and delivered your verdict\nYour verdict : {0}",
                ["MODERATOR_COMPLETED_CHECK_ALERT"] = "Moderator {0} successfully completed the review  {1}\nVerdict: {2}",
                ["MODERATOR_COMPLETED_CHECK_VK"] = "[[IQ Report System]\nModerator {0}[(1)] finished checking \nSuspect {2}[{3}]\nVerdict : {4}\n[AFK Check]Player didn't move : {5}/5",
                ["PLAYER_AFK_CHECK_STOP"] = "Suspect AFK\nThe check is removed automatically!",
                ["PLAYER_AFK_CHECK_STOP_VK"] = "[IQReportSystem]\nModerator {0}({1}) checking the player {2}.\nThe AFK suspect and the check was removed!",
                ["PLAYER_AFK_CHANGE_POS"] = "The player was moving! Check {0}/5",
                ["PLAYER_AFK_CHANGE_NO_POS"] = "The player didn't move! Check {0}/5",
                ["PLAYER_NON_AFK"] = "The player moves.\nProverite on!",
                ["DISCORD_NULL"] = "Not provided",
                ["REPORT_LIST_CONSOLE"] = "\n[IQReportSystem]:\nList of players in the Moderation Panel",
                ["DISCROD_VK_SEND"] = "[IQReportSystem]\nSuspect {0}({1}) provided Discord for verification!\nDiscord - {2}",
                ["CONSOLE_REPORT_GIVE"] = "Player {0} is successfully added to the report in the amount of {1}. Its number is - {2}",
                ["CONSOLE_REPORT_REMOVE"] = "Player {0} successfully removed reports in the amount of - {1} His number is - {2}",
                ["MODERATOR_NON_OPEN_MENU"] = "You can't open the moderator menu when checking a player!\nFinish checking!",
                ["UI_RAITING_MODERATION_VK_GIVE"] = "[IQReportSystem]:\nPlayer {0}({1}) rated the work of moderator {2}({3}) on {4} stars",
                ["UI_RAITING_MODERATION_VK_GIVE_THX"] = "Thx!",
                ["UI_RAIT_ALERT_MODER"] = "Your work has been rated with {0} stars. You gain {1} reputation.",

                ["UI_NEW_CHECKED_COUNT"] = "<size=14>CHECKED: {0}</size>",
                ["UI_NEW_CLOSE"] = "<size=24><b>CLOSE</b></size>",

                ["UI_NEW_MODERATION_TITLE_PANEL"] = "<size=18>PANEL MODERATION</size>",
                ["UI_NEW_MODERATION_REPORT_BTN"] = "<size=20><b>REPORTS</b></size>",

                ["UI_NEW_MODERATION_TITLE_STATS"] = "<size=18><b>CHECK STATISTICS</b></size>",
                ["UI_NEW_MODERATION_STATS_COUNTCHECK"] = "<size=8>NUMBER OF CHECKS: {0}</size>",
                ["UI_NEW_MODERATION_STATS_COUNTBANS"] = "<size=8>BLOCKEDS: {0}</size>",
                ["UI_NEW_MODERATION_STATS_STARS"] = "<size=15><b>QUALITY CONTROL : {0}</b></size>",

                ["UI_NEW_MODERATION_REPORT_PANEL_TITLE"] = "<size=70><b>REPORTS</b></size>",
                ["UI_NEW_MODERATION_REPORT_PANEL_DESCRIPTION"] = "<size=8>CHOOSE A PLAYER TO SEND TO IT'S COMPLAINT</size>",
                ["UI_NEW_MODERATION_REPORT_PANEL_SEARCH_DESCRIPTION"] = "<size=14>ENTER NICK OR STEAM64ID TO SEARCH A PLAYER</size>",
                ["UI_NEW_MODERATION_REPORT_PANEL_DESCRIPTION_MODERATOR"] = "<size=8>SELECT A PLAYER IN THE PANEL TO START A CHECK</size>",
                ["UI_NEW_MODERATION_REPORT_PANEL_SEARCH_TITLE"] = "<size=50><b>SEARCH</b></size>",

                ["UI_NEW_MODERATION_REPORT_PANEL_PLAYER_REPORTEDS"] = "<size=14><b>REPORTS : {0}</b></size>",

                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_TITLE"] = "<size=30><b>COMPLAINT INFORMATION</b></size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_DESC"] = "<size=8>CHOOSE THE REASON FOR THE PLAYER COMPLAINT</size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_TITLE"] = "<size=25><b>PLAYER</b></size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_CLOSE"] = "<size=18><b>CLOSE</b></size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_LIST"] = "<size=20><b>CHOOSE THE REASON FROM THE LIST</b></size>",


                ["UI_NEW_MODERATION_MODER_GO_CHECK_TITLE"] = "<size=30><b>PLAYER INFORMATION</b></size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_DESC"] = "<size=8>CHOOSE ACTION WHICH YOU WANT TO PERFORM</size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_TITLE"] = "<size=25><b>DETAILED INFORMATION</b></size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_HISTORY_REPORT"] = "<size=18>HISTORY OF COMPLAINTS</size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_HISTORY_RCC"] = "<size=18>INFO RCC</size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_REPORTS"] = "<size=25><b>REPORTS</b></size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_LAST_CHECK"] = "<size=14>LAST CHECKER: {0}</size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_COUNT_CHECK"] = "<size=14>CHECK COUNTS: {0}</size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_GOCHECK"] = "<size=18>START</size>",

                ["UI_NEW_ALERT_PLAYER_WARNING"] = "<size=40><b>YOU CALLED TO THE TEST</b></size>",
                ["UI_NEW_ALERT_PLAYER_TITLE"] = "<size=18>You exceeded the maximum allowable number of complaints.\npoetomu,provide your ex, in order to be contacted by our moderation!\nPV case of ignoring this message, you will get a lock! (You have 5 minutes)</size>",
                ["UI_NEW_ALERT_PLAYER_DESC"] = "<size=15>to provide data for communication,use the commands:\n/discord\npdale you will be contacted by the moderator</size>",

                ["UI_NEW_MINI_PANEL_MODERATOR_TITLE"] = "<size=14><b>MENU MODERATOR</b></size>",
                ["UI_NEW_MINI_PANEL_MODERATOR_BAN"] = "<size=18><b>BAN</b></size>",
                ["UI_NEW_MINI_PANEL_MODERATOR_STOP"] = "<size=18><b>STOP</b></size>",

                ["UI_NEW_RAITING_PANEL"] = "<size=13><b>GIVE RAITING</b>></size>",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["PLAYER_CHECKED"] = "Данного игрока уже проверяют!",

                ["MSG_REPORTED_SUSPECT"] = "Вы успешно отправили жалобу на игрока - {0}\nЖалоба : {1}\nМодератор рассмотрит вашу жалобу как можно скорее!",
                ["MSG_CHECK_DISCORD"] = "Вы не можете отправить Discord без проверки!",
                ["MSG_CHECK_CHECK_STOP"] = "Вы успешно прошли проверку!\nЖелаем приятной игры на нашем сервере!",
                ["MSG_COOLDOWN"] = "Вы недавно отправляли жалобу!\nПодождите еще <color=#47AF5DFF>{0}</color>",
                ["MSG_DISCORD_SEND"] = "Вы успешно предоставили данные!\nDiscord - {0}\nОжидайте звонка от модератора",

                ["UI_MODERATOR_PANEL_TITLE"] = "Меню модератора сервера",
                ["UI_MODERATOR_PANEL_DESCRIPTION"] = "В данном списке отображены игроки - достигшие предела жалоб,нажмите по игроку чтобы получить больше информации",
                ["UI_MODERATOR_PANEL_START_CHECK"] = "Вызвать на проверку",
                ["UI_STATUS"] = "Статус",

                ["NETWORD_STATUS_ONLINE"] = "Онлайн",
                ["IS_STEAM_STATUS_PIRATE"] = "Пират",
                ["IS_STEAM_STATUS_LICENSE"] = "Лицензия", 

                ["METODS_SEND_REPORT_VK"] = "[IQReportSystem]\nНа игрока {0}({1}) отправили жалобу!\nЖалоба - {2}\nОтправил жалобу : {3}({4})",
                ["METODS_HELP_MODERS"] = "Игрок <color=#47AF5DFF>{0}</color> достиг предельного количества репортов!\nКоличество его репортов - <color=#47AF5DFF>{1}</color>\nМодерация которая свободна - проверьте игрока!",
                ["METODS_HELP_MODERS_VK"] = "[IQReportSystem]\nИгрок {0} достиг предельного количества репортов!\nКоличество его репортов - {1}\nМодерация которая свободна - проверьте игрока!",
                ["METODS_MODER_START_CHECK"] = "Вы начали проверку!\nПодозреваемый - <color=#47AF5DFF>{0}</color>\nНачинаем проверку на AFK!\nЕсли игрок не AFK - ему выведут уведомление о проверке!",
                ["METODS_MODER_START_CHECK_VK"] = "[IQReportSystem]\nМодератор {0}({1}) начал проверку!\nПодозреваемый - {2}({3})",
                ["METODS_MODER_STOP_CHECK"] = "Проверка завершена.\nУдачного дня!\nНе забывай проверять список жалоб!",
                ["METODS_MODER_STOP_CHECK_VK"] = "[IQReportSystem]\nМодератор {0} закончил проверку!",
                ["METODS_MODER_STOP_CHECK_ALERT"] = "Модератор {0} закончил проверку игрока - {1}\nЗапрещенного не обнаружено",
                ["NON_REPORT"] = "Жалоб нет",
                ["MODERATOR_RETURN_WELCOME"] = "С возвращением!\nПроверка не была отменена,продолжайте!",
                ["STATUS_CHANGED"] = "У игрока {0} изменился статус на : {1}\nОжидайте игрока на сервере в течении 10 минут!\nЕсли игрок не зайдет после 10 минут - выдавайте бан за Отказ",
                ["STATUS_CHANGED_VK"] = "[IQReportSystem]У игрока {0} изменился статус на : {1}\nОжидайте игрока на сервере в течении 10 минут!\nЕсли игрок не зайдет после 10 минут - выдавайте бан за Отказ",
                ["MODERATOR_DISCONNECTED_STOP_CHECK"] = "Проверка была снята!\nМодератор {0} покинул сервер\n Причина : Разрыв соединения\nПриносим свои извинения!\nМы сообщим другой модерации!",
                ["MODERATOR_DISCONNECTED_STOP_RESEND"] = "Модератор {0} окончательно покинул сервер во время проверки!\nИгрок {1} ожидает других модераторов для проверки!",
                ["MODERATOR_COMPLETED_CHECK"] = "Вы успешно завершили проверку и вынесли свой вердикт\nВаш вердикт : {0}",
                ["MODERATOR_COMPLETED_CHECK_ALERT"] = "Модератор {0} закончил проверку игрока {1}\nВердикт: {2}",
                ["MODERATOR_COMPLETED_CHECK_VK"] = "[IQReportSystem]\nМодератор {0}[(1)] закончил проверку\n Подозреваемый {2}[{3}]\nВердикт : {4}\n[Проверка на AFK]Игрок не двигался : {5}/5",
                ["PLAYER_AFK_CHECK_STOP"] = "Игрок AFK\nПроверка снята автоматически!",
                ["PLAYER_AFK_CHECK_STOP_VK"] = "[IQReportSystem]\nМодератор {0}({1}) проверял игрока {2}.\nИгрок AFK и проверка была снята!",
                ["PLAYER_AFK_CHANGE_POS"] = "Игрок двигался! Проверка {0}/5",
                ["PLAYER_AFK_CHANGE_NO_POS"] = "Игрок не двигался! Проверка {0}/5",
                ["PLAYER_NON_AFK"] = "Игрок двигается.\nПроверяйте дальше!",
                ["DISCORD_NULL"] = "Не предоставлен",
                ["REPORT_LIST_CONSOLE"] = "\n[IQReportSystem]:\nСписок игроков в Панели-Модерации",
                ["DISCROD_VK_SEND"] = "[IQReportSystem]\nИгрок {0}({1}) предоставил Discord на проверку!\nDiscord - {2}",
                ["CONSOLE_REPORT_GIVE"] = "Игроку {0} успешно добавлены репорты в количестве - {1}. Его количество составляет - {2}",
                ["CONSOLE_REPORT_REMOVE"] = "Игроку {0} успешно сняты репорты в количестве - {1} Его количество составляет - {2}",
                ["MODERATOR_NON_OPEN_MENU"] = "Вы не можете открыть меню модератора при проверке игрока!\nОкончите проверку!",
                ["UI_RAITING_MODERATION_VK_GIVE"] = "[IQReportSystem]:\nИгрок {0}({1}) оценил работу модератора {2}({3}) на {4} звезды",
                ["UI_RAITING_MODERATION_VK_GIVE_THX"] = "Спасибо за ваш отзыв!",
                ["UI_RAIT_ALERT_MODER"] = "Вашу работу оценили в {0} звезд(ы). Вы получаете {1} репутации",


                ["UI_NEW_CHECKED_COUNT"] = "<size=14>ПРОВЕРЕН: {0}</size>",
                ["UI_NEW_CLOSE"] = "<size=24><b>ЗАКРЫТЬ</b></size>",

                ["UI_NEW_MODERATION_TITLE_PANEL"] = "<size=18>ПАНЕЛЬ ПРОВЕРЯЮЩЕГО</size>",
                ["UI_NEW_MODERATION_REPORT_BTN"] = "<size=20><b>ЖАЛОБЫ</b></size>",

                ["UI_NEW_MODERATION_TITLE_STATS"] = "<size=18><b>СТАТИСТИКА ПРОВЕРЯЮЩЕГО</b></size>",
                ["UI_NEW_MODERATION_STATS_COUNTCHECK"] = "<size=8>КОЛИЧЕСТВО ПРОВЕРОК: {0}</size>",
                ["UI_NEW_MODERATION_STATS_COUNTBANS"] = "<size=8>БЛОКИРОВОК ВЫДАНО: {0}</size>",
                ["UI_NEW_MODERATION_STATS_STARS"] = "<size=15><b>ОЦЕНКА КАЧЕСТВА : {0}</b></size>",

                ["UI_NEW_MODERATION_REPORT_PANEL_TITLE"] = "<size=70><b>ЖАЛОБЫ</b></size>",
                ["UI_NEW_MODERATION_REPORT_PANEL_DESCRIPTION"] = "<size=8>ВЫБЕРИТЕ ИГРОКА ЧТОБЫ ОТПРАВИТЬ НА НЕГО ЖАЛОБУ</size>",
                ["UI_NEW_MODERATION_REPORT_PANEL_DESCRIPTION_MODERATOR"] = "<size=8>ВЫБЕРИТЕ ИГРОКА В ПАНЕЛИ ЧТОБЫ НАЧАТЬ ПРОВЕРКУ</size>",
                ["UI_NEW_MODERATION_REPORT_PANEL_SEARCH_DESCRIPTION"] = "<size=14>ВВЕДИТЕ НИК ИЛИ STEAM64ID ДЛЯ ПОИСКА ИГРОКА</size>",
                ["UI_NEW_MODERATION_REPORT_PANEL_SEARCH_TITLE"] = "<size=50><b>ПОИСК</b></size>",

                ["UI_NEW_MODERATION_REPORT_PANEL_PLAYER_REPORTEDS"] = "<size=14><b>ЖАЛОБЫ : {0}</b></size>",

                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_TITLE"] = "<size=30><b>ИНФОРМАЦИЯ О ЖАЛОБЕ</b></size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_DESC"] = "<size=8>ВЫБЕРИТЕ ПРИЧИНУ ДЛЯ ЖАЛОБЫ НА ИГРОКА</size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_TITLE"] = "<size=25><b>ИГРОК</b></size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_CLOSE"] = "<size=18><b>ЗАКРЫТЬ</b></size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_LIST"] = "<size=20><b>ВЫБЕРИТЕ ПРИЧИНУ ИЗ СПИСКА</b></size>",

                ["UI_NEW_MODERATION_MODER_GO_CHECK_TITLE"] = "<size=30><b>ИНФОРМАЦИЯ О ИГРОКЕ</b></size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_DESC"] = "<size=8>ВЫБЕРИТЕ ДЕЙСТВИЯ КОТОРОЕ ХОТИТЕ СОВЕРШИТЬ</size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_TITLE"] = "<size=25><b>ПОДРОБНАЯ ИНФОРМАЦИЯ</b></size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_HISTORY_REPORT"] = "<size=18>ИСТОРИЯ ЖАЛОБ</size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_HISTORY_RCC"] = "<size=18>ИНФОРМАЦИЯ RCC</size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_REPORTS"] = "<size=25><b>ЖАЛОБ</b></size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_LAST_CHECK"] = "<size=14>ПОСЛЕДНИЙ ПРОВЕРЯЮЩИЙ : {0}</size>",
                ["UI_NEW_MODERATION_MODER_GO_CHECK_MORE_COUNT_CHECK"] = "<size=14>КОЛИЧЕСТВО ПРОВЕРОК: {0}</size>",
                ["UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_GOCHECK"] = "<size=18>ПРОВЕРИТЬ</size>",

                ["UI_NEW_ALERT_PLAYER_WARNING"] = "<size=40><b>ВАС ВЫЗВАЛИ НА ПРОВЕРКУ</b></size>",
                ["UI_NEW_ALERT_PLAYER_TITLE"] = "<size=18>Вы превысили максимально-допустимое количество жалоб.\nПоэтому,предоставьте ваш Discord, для того чтобы с вами связалась наша модерация!\nВ случае игнорирования данного сообщения - вы получите блокировку! (У вас имеется 5 минут)</size>",
                ["UI_NEW_ALERT_PLAYER_DESC"] = "<size=15>Чтобы предоставить данные для связи,используйте команды:\n/discord\nДалее с вами свяжется модератор</size>",

                ["UI_NEW_MINI_PANEL_MODERATOR_TITLE"] = "<size=14><b>МЕНЮ ПРОВЕРЯЮЩЕГО</b></size>",
                ["UI_NEW_MINI_PANEL_MODERATOR_BAN"] = "<size=18><b>ВЕРДИКТ</b></size>",
                ["UI_NEW_MINI_PANEL_MODERATOR_STOP"] = "<size=18><b>СТОП</b></size>",


                ["UI_NEW_RAITING_PANEL"] = "<size=13><b>ОЦЕНИТЕ ПРОВЕРЯЮЩЕГО</b>></size>",
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion

        #region Interface

        public static string PARENT_UI =  "MAIN_PARENT_UI";
        public static string PARENT_UI_REPORT_MENU = "PARENT_UI_REPORT_MENU";
        public static string PARENT_UI_PLAYER_PANEL = "PARENT_UI_PLAYER_PANEL";
        public static string PARENT_UI_PLAYER_REPORT = "PARENT_UI_PLAYER_REPORT";
        public static string PARENT_UI_MODER_REPORT = "PARENT_UI_MODER_REPORT";
        public static string PARENT_UI_ALERT_SEND = "PARENT_UI_ALERT_SEND";
        public static string PARENT_UI_MODERATOR_MINI_PANEL = "PARENT_UI_MODERATOR_MINI_PANEL";
        private static string UI_MODERATION_CHECK_MENU_DISCORD = "UI_MODERATION_CHECK_MENU_DISCORD_PARENT";
        private static string UI_MODERATION_CHECK_MENU_NETWORK = "UI_MODERATION_CHECK_MENU_NETWORK_PARENT";
        private static string UI_MODERATION_RAITING = "UI_MODERATION_RAITING";


        #region UI Main Interface
        private void RenderReport(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];

            UI_Interface(BasePlayer.FindByID(userID), Container);
        }

        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";

        void UI_Interface(BasePlayer player, CuiElementContainer container)
        {
            CuiHelper.DestroyUi(player, PARENT_UI);
            var Interface = config.Setting.Interface;
            var InformationUser = ReportInformation[player.userID];

            container.Add(new CuiElement
            {
                Name = MenuContent,
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-430 -230",
                            OffsetMax = "490 270"
                        },
                    }
            });

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-150 0", OffsetMax = "0 500" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(Interface.HexRightMenu), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, MenuContent, PARENT_UI);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.006666541 0.7046295", AnchorMax = "1 0.7462999" },
                Text = { Text = $"<b><size=8>{player.displayName}</size></b>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperCenter }
            }, PARENT_UI);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.6851852", AnchorMax = "1 0.7166605" },
                Text = { Text = String.Format(lang.GetMessage("UI_NEW_CHECKED_COUNT", this, player.UserIDString), InformationUser.CheckCount), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperCenter }
            }, PARENT_UI);

            string ImageAvatar = GetImage(player.UserIDString, 0);
            container.Add(new CuiElement
            {
                Parent = PARENT_UI,
                Name = $"AVATAR",
                Components =
                 {
                    new CuiRawImageComponent { Png = ImageAvatar,Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent{ AnchorMin = "0.08076949 0.7518547", AnchorMax = $"0.9341028 0.9888917"},
                 }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.05555556" },
                Button = { Close = PARENT_UI, Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("UI_NEW_CLOSE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleCenter }
            }, PARENT_UI);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.6851852", AnchorMax = "1 0.7166605" },
                Text = { Text = String.Format(lang.GetMessage("UI_NEW_CHECKED_COUNT", this, player.UserIDString), InformationUser.CheckCount), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperCenter }
            }, PARENT_UI);

            if (permission.UserHasPermission(player.UserIDString, PermissionModeration))
            {
                if (!ModeratorInformation.ContainsKey(player.userID))
                    Metods_PlayerConnected(player);

                var InformationModerator = ModeratorInformation[player.userID];

                #region PANEL MODERATION MENU

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.4907467", AnchorMax = "1 0.5574059" },
                    Text = { Text = lang.GetMessage("UI_NEW_MODERATION_TITLE_PANEL", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.1533333 0.4212967", AnchorMax = "1 0.47963" },
                    Button = { Command = "iqreport moderation_menu", Color = HexToRustFormat(Interface.HexButtonRightMenu), Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { Text = lang.GetMessage("UI_NEW_MODERATION_REPORT_BTN", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Align = TextAnchor.MiddleCenter }
                },  PARENT_UI, "BTN_REPORTS");

                container.Add(new CuiElement
                {
                    Parent = "BTN_REPORTS",
                    Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = Interface.SpriteReportModeration },
                        new CuiRectTransformComponent { AnchorMin = "0.02362165 0", AnchorMax = "0.2755902 1" }
                    }
                });

                #endregion

                #region STATS PANEL MODERATION MENU

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.2574087", AnchorMax = "1 0.3138935" },
                    Text = { Text = lang.GetMessage("UI_NEW_MODERATION_TITLE_STATS", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.003333271 0.1240732", AnchorMax = "1 0.2500008" },
                    Image = { Color = HexToRustFormat(Interface.HexButtonRightMenu), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                },  PARENT_UI, "STATS_MODERATION");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.6911764", AnchorMax = "1 1" },
                    Text = { Text = String.Format(lang.GetMessage("UI_NEW_MODERATION_STATS_COUNTCHECK", this, player.UserIDString), InformationModerator.CheckCount), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                },  "STATS_MODERATION");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.5441177", AnchorMax = "1 0.7499995" },
                    Text = { Text = String.Format(lang.GetMessage("UI_NEW_MODERATION_STATS_COUNTBANS", this, player.UserIDString), InformationModerator.BanPlayerModerator.Count), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, "STATS_MODERATION");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2573562" },
                    Text = { Text = String.Format(lang.GetMessage("UI_NEW_MODERATION_STATS_STARS", this, player.UserIDString), GetAverageRaiting(player.userID)), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.LowerCenter }
                }, "STATS_MODERATION");

                for (int i = 0; i < 5; i++)
                {
                    string ColorStar = Math.Floor(GetAverageRaiting(player.userID)) == 0 ? "#D9BA6AA2" : Math.Floor(GetAverageRaiting(player.userID)) >= i+1 ? "#d9ba6a" : "#D9BA6AA2";
                    container.Add(new CuiElement
                    {
                        Parent = "STATS_MODERATION",
                        Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(ColorStar), Sprite = Interface.SpriteRaiting },
                        new CuiRectTransformComponent { AnchorMin = $"{0.07023425 + (i * 0.175)} 0.2058797", AnchorMax = $"{0.2307694 + (i * 0.175)} 0.5661694" }
                    }
                    });
                }

                #endregion
            }

            timer.In(0.3f, () =>
            {
                UI_PanelReportsPlayer(player);
            });
        }

        #endregion

        #region UI Panel Reports Interface

        void UI_PanelReportsPlayer(BasePlayer player,  bool Moderation = false)
        {
            var Interface = config.Setting.Interface;
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_UI_REPORT_MENU);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "-770 0", OffsetMax = "0 500" },
                Image = { Color = HexToRustFormat(Interface.HexMainPanel), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            },  PARENT_UI, PARENT_UI_REPORT_MENU);

            container.Add(new CuiElement
            {
                Parent = PARENT_UI_REPORT_MENU,
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = Interface.SpriteReport },
                        new CuiRectTransformComponent { AnchorMin = "0.007901235 0.8648087", AnchorMax = "0.09876543 0.9833272" }
                    }
            });

            string SearchName = "";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3799841 0.8888888", AnchorMax = "0.7234258 0.925926" },
                Image = { Color = HexToRustFormat(Interface.HexSearchPanel), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            },  PARENT_UI_REPORT_MENU, PARENT_UI_REPORT_MENU + ".Input");

            container.Add(new CuiElement
            {
                Parent = PARENT_UI_REPORT_MENU + ".Input",
                Name = PARENT_UI_REPORT_MENU + ".Input.Current",
                Components =
                { 
                    new CuiInputFieldComponent { Text = SearchName, FontSize = 14,Command = $"iqreport search {Moderation} {0} {SearchName}", Align = TextAnchor.MiddleLeft, Color = HexToRustFormat(Interface.HexLabels), CharsLimit = 15},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09925909 0.8462963", AnchorMax = "0.4113576 0.9805495" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_REPORT_PANEL_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, PARENT_UI_REPORT_MENU);

            string DescriptionReportTitle = Moderation ? "UI_NEW_MODERATION_REPORT_PANEL_DESCRIPTION_MODERATOR" : "UI_NEW_MODERATION_REPORT_PANEL_DESCRIPTION";
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1 0.9546276", AnchorMax = "0.38 0.98" },
                Text = { Text = lang.GetMessage(DescriptionReportTitle, this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, PARENT_UI_REPORT_MENU);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7315881 0.8675926", AnchorMax = "0.9332258 0.9527258" }, 
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_REPORT_PANEL_SEARCH_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, PARENT_UI_REPORT_MENU);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3799841 0.9277778", AnchorMax = "0.7313576 0.95" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_REPORT_PANEL_SEARCH_DESCRIPTION", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, PARENT_UI_REPORT_MENU);

            CuiHelper.AddUi(player, container);
            UI_Player_Loaded(player, Moderation);
        }

        #endregion

        #region UI Player Loaded Interface

        void UI_Player_Loaded(BasePlayer player, bool Moderation = false, int Page = 0, string TargetName = "", bool debug = true)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_UI_PLAYER_PANEL);
            var Interface = config.Setting.Interface;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.8296296" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_UI_REPORT_MENU, PARENT_UI_PLAYER_PANEL);


            int x = 0, y = 0, i = 20 * Page;

            IEnumerable<FakePlayer> playerList = (IEnumerable<FakePlayer>)(Moderation ? PlayerBases.Where(z => (ReportInformation.ContainsKey(z.UserID) && ReportInformation[z.UserID].ReportCount >= config.Setting.MaxReport) && z.DisplayName.ToLower().Contains(TargetName.ToLower()) || z.UserID.ToString().Contains(TargetName)).Skip(Page * 20)
                                                            : PlayerBases.Where(z => z.DisplayName.Contains(TargetName.ToLower()) || z.UserID.ToString().Contains(TargetName.ToLower())).Skip(i));

            var ActiviteList = ((Moderation ? BasePlayer.activePlayerList.Where(z => (ReportInformation.ContainsKey(z.userID) && ReportInformation[z.userID].ReportCount >= config.Setting.MaxReport) && z.displayName.ToLower().Contains(TargetName.ToLower()) || z.userID.ToString().Contains(TargetName)).Skip(Page * 20)
                                                : BasePlayer.activePlayerList.Where(z => z.displayName.ToLower().Contains(TargetName.ToLower()) || z.userID.ToString().Contains(TargetName)).Skip(i)));

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4866512 0.008887243", AnchorMax = "0.5140741 0.05357143" },
                Text = { Text = $"<size=10>{Page}</size>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, PARENT_UI_PLAYER_PANEL);

            if (IQFakeActive && config.IQFakeActiveSettings.UseIQFakeActive)
            {
                if (Page + 1 < (int)Math.Ceiling(((double)playerList.Count()) / 20))
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5222067 0.008887243", AnchorMax = "0.5496294 0.05357143" },
                        Button = { Command = $"iqreport page next {Moderation} {Page}", Color = HexToRustFormat(Interface.HexPlayerButton), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        Text = { Text = "<b><size=8>></size></b>", Align = TextAnchor.MiddleCenter }
                    }, PARENT_UI_PLAYER_PANEL);
                }
            }
            else
            {
                if ((Page + 1) * 20 < BasePlayer.activePlayerList.Count())
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.5222067 0.008887243", AnchorMax = "0.5496294 0.05357143" },
                        Button = { Command = $"iqreport page next {Moderation} {Page}", Color = HexToRustFormat(Interface.HexPlayerButton), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                        Text = { Text = "<b><size=8>></size></b>", Align = TextAnchor.MiddleCenter }
                    }, PARENT_UI_PLAYER_PANEL);
                }
            }

            if (Page > 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4510956 0.008887243", AnchorMax = "0.4785185 0.05357143" },
                    Button = { Command = $"iqreport page back {Moderation} {Page}", Color = HexToRustFormat(Interface.HexPlayerButton), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "<b><size=8><</size></b>", Align = TextAnchor.MiddleCenter }
                }, PARENT_UI_PLAYER_PANEL);
            }
           
            if (IQFakeActive && config.IQFakeActiveSettings.UseIQFakeActive)
                foreach (var Plist in playerList)
                {
                    ulong UserID = Plist.UserID;
                    string DisplayName = Plist.DisplayName;
                    if (Friends != null)
                        if (config.Setting.FriendNoReport)
                            if ((bool)Friends.Call("HasFriend", player.userID, UserID)) continue;

                    if (UserID == player.userID) continue;
                    if (!IsFake(UserID))
                        if (permission.UserHasPermission(UserID.ToString(), PermissionAdmin)) continue;

                    container.Add(new CuiPanel
                    {
                        RectTransform = { 
                            AnchorMin = $"{0.00246954 + (x * 0.2540)} {0.8671876 - (y * 0.2)}", 
                            AnchorMax = $"{0.2 + (x * 0.2540)} {0.9910715 - (y * 0.2)}" },
                        Image = { Color = HexToRustFormat(Interface.HexSearchPanel), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                    }, PARENT_UI_PLAYER_PANEL, $"PLAYER_{i}");

                    string ImageAvatar = GetImage(UserID.ToString(), 0);
                    container.Add(new CuiElement
                    {
                        Parent = $"PLAYER_{i}",
                        Components =
                         {
                            new CuiRawImageComponent { Png = ImageAvatar,Color = HexToRustFormat("#FFFFFFAA") },
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0.3600007 1"},
                         }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.3699992 0.6756751", AnchorMax = "1 1" },
                        Text = { Text = $"<b><size=8>{DisplayName}</size></b>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
                    }, $"PLAYER_{i}");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.3699992 0.432432", AnchorMax = "1 0.7567569" },
                        Text = { Text = $"<size=10>{UserID}</size>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
                    }, $"PLAYER_{i}");

                    string CMDD = Moderation ? $"iqreport moderation_send {UserID}" : $"iqreport reports_menu {UserID}"; ///// DEBUG
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "2 0", OffsetMax = "22 75" },
                        Button = { Command = CMDD, Color = HexToRustFormat(Interface.HexPlayerButton) },
                        Text = { Text = "" }
                    }, $"PLAYER_{i}", $"BTN_ACTION_{i}");

                    container.Add(new CuiElement
                    {
                        Parent = $"BTN_ACTION_{i}",
                        Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = Interface.SpritePlayerButton },
                        new CuiRectTransformComponent { AnchorMin = "0 0.41", AnchorMax = "0.96 0.62" }
                    }
                    });

                    if (Moderation)
                    {
                        if (IsFake(UserID)) continue;

                        var InformationUser = ReportInformation[UserID];
                        string IsSteamSprite = IsSteam(UserID.ToString()) == lang.GetMessage("IS_STEAM_STATUS_LICENSE", this, UserID.ToString()) ? "assets/icons/steam.png" : "assets/icons/poison.png";

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0.3699992 0.01801781", AnchorMax = "1 0.3042333" },
                            Text = { Text = String.Format(lang.GetMessage("UI_NEW_MODERATION_REPORT_PANEL_PLAYER_REPORTEDS", this, UserID.ToString()), InformationUser.ReportCount), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
                        }, $"PLAYER_{i}");

                        container.Add(new CuiElement
                        {
                            Parent = $"PLAYER_{i}",
                            Components =
                        {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = IsSteamSprite },
                        new CuiRectTransformComponent { AnchorMin = "0.8500056 0.03801781", AnchorMax = "0.9900023 0.4632426" }
                        }
                        });
                    }

                    i++;
                    x++;
                    if (x == 4)
                    {
                        x = 0;
                        y++;
                    }
                    if (y == 5 && x == 0)
                        break;
                }
            else
            {
                foreach (var Plist in ActiviteList)
                {
                    ulong UserID = Plist.userID;
                    string DisplayName = Plist.displayName;
                    if (Friends != null)
                        if (config.Setting.FriendNoReport)
                            if ((bool)Friends.Call("HasFriend", player.userID, UserID)) continue;

                    if (UserID == player.userID) continue;
                    if (permission.UserHasPermission(UserID.ToString(), PermissionAdmin)) continue;

                    container.Add(new CuiPanel
                    {
                        RectTransform = { 
                            AnchorMin = $"{0.00246954 + (x * 0.2540)} {0.8671876 - (y * 0.2)}", 
                            AnchorMax = $"{0.2 + (x * 0.2540)} {0.9910715 - (y * 0.2)}" },
                        Image = { Color = HexToRustFormat(Interface.HexSearchPanel), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                    }, PARENT_UI_PLAYER_PANEL, $"PLAYER_{i}");

                    string ImageAvatar = GetImage(UserID.ToString(), 0);
                    container.Add(new CuiElement
                    {
                        Parent = $"PLAYER_{i}",
                        Components =
                         {
                            new CuiRawImageComponent { Png = ImageAvatar,Color = HexToRustFormat("#FFFFFFAA") },
                            new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"0.3600007 1"},
                         }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.3699992 0.6756751", AnchorMax = "1 1" },
                        Text = { Text = $"<b><size=8>{DisplayName}</size></b>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
                    }, $"PLAYER_{i}");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0.3699992 0.432432", AnchorMax = "1 0.7567569" },
                        Text = { Text = $"<size=10>{UserID}</size>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
                    }, $"PLAYER_{i}");

                    string CMDD = Moderation ? $"iqreport moderation_send {UserID}" : $"iqreport reports_menu {UserID}"; 
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "2 0", OffsetMax = "14 52.5" },
                        Button = { Command = CMDD, Color = HexToRustFormat(Interface.HexPlayerButton) },
                        Text = { Text = "" }
                    }, $"PLAYER_{i}", $"BTN_ACTION_{i}");

                    container.Add(new CuiElement
                    {
                        Parent = $"BTN_ACTION_{i}",
                        Components =
                        {
                            new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = Interface.SpritePlayerButton },
                            new CuiRectTransformComponent { AnchorMin = "0 0.41", AnchorMax = "0.96 0.62" }
                        }
                    });

                    if (Moderation)
                    {
                        if (IsFake(UserID)) continue;

                        var InformationUser = ReportInformation[UserID];
                        string IsSteamSprite = IsSteam(UserID.ToString()) == lang.GetMessage("IS_STEAM_STATUS_LICENSE", this, UserID.ToString()) ? "assets/icons/steam.png" : "assets/icons/poison.png";

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0.3699992 0.01801781", AnchorMax = "1 0.3042333" },
                            Text = { Text = String.Format(lang.GetMessage("UI_NEW_MODERATION_REPORT_PANEL_PLAYER_REPORTEDS", this, UserID.ToString()), InformationUser.ReportCount), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
                        }, $"PLAYER_{i}");

                        container.Add(new CuiElement
                        {
                            Parent = $"PLAYER_{i}",
                            Components =
                        {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = IsSteamSprite },
                        new CuiRectTransformComponent { AnchorMin = "0.8500056 0.04801781", AnchorMax = "0.9900023 0.4632426" }
                        }
                        });
                    }

                    i++;
                    x++;
                    if (x == 4)
                    {
                        x = 0;
                        y++;
                    }
                    if (y == 5 && x == 0)
                        break;
                }
            }
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region UI Send Report
        void UI_SendReport(BasePlayer player, ulong UserID)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_UI_PLAYER_REPORT);
         //   ulong UserID = IQFakeActive ? PlayerBases.FirstOrDefault(x => x.UserID == UserID).use : BasePlayer.activePlayerList[BaseID].userID;
            string DisplayName = IQFakeActive ? PlayerBases.FirstOrDefault(j => j.UserID == UserID).DisplayName : BasePlayer.FindByID(UserID).displayName;

            var InterfaceReport = config.Setting.Interface.ReasonInterface;
            var Interface= config.Setting.Interface;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat("#21211AF2") }
            },  "Overlay", PARENT_UI_PLAYER_REPORT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.25 0.1768519", AnchorMax = "0.7 0.8" },
                Image = { Color = HexToRustFormat(InterfaceReport.HexMain) }
            },  PARENT_UI_PLAYER_REPORT,"PANEL_MAIN_REPORT");

            #region TitlePanel

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.8662704", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat(InterfaceReport.HexTitlePanel) }
            },  "PANEL_MAIN_REPORT", "TITLE_PANEL");

            container.Add(new CuiElement
            {
                Parent = $"TITLE_PANEL",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = InterfaceReport.SpriteTitlePanel },
                        new CuiRectTransformComponent { AnchorMin = "0.01851851 0.1000004", AnchorMax = "0.09259258 0.8111112" }
                    }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09953713 0", AnchorMax = "1 0.6333335" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_SEND_REPORT_PANEL_TITLE",this,player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            },  "TITLE_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09953713 0.4444447", AnchorMax = "1 0.9" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_SEND_REPORT_PANEL_DESC", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "TITLE_PANEL");

            #endregion

            #region MainPanel
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09143519 0.7028232", AnchorMax = "0.2731481 0.768202" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "PANEL_MAIN_REPORT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5973254", AnchorMax = "1 0.6493313" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_LIST", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "PANEL_MAIN_REPORT");

            string ImageAvatar = GetImage((string)UserID.ToString(), 0);
            container.Add(new CuiElement
            {
                Parent = $"PANEL_MAIN_REPORT",
                Components =
                 {
                    new CuiRawImageComponent { Png = ImageAvatar,Color = HexToRustFormat("#FFFFFFAA") },
                    new CuiRectTransformComponent{ AnchorMin = "0.33449 0.6909361", AnchorMax = $"0.4085641 0.7860327"},
                 }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.412036 0.7444279", AnchorMax = "0.9918982 0.7860327" },
                Text = { Text = $"<b><size=16>{DisplayName}</size></b>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
            }, "PANEL_MAIN_REPORT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.412036 0.6909361", AnchorMax = "0.9918982 0.7265974" },
                Text = { Text = $"<size=15>{UserID.ToString()}</size>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
            }, "PANEL_MAIN_REPORT");
            #endregion

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3263901 0.01188706", AnchorMax = "0.6678232 0.08023772" },
                Button = { Close = PARENT_UI_PLAYER_REPORT, Color = HexToRustFormat(InterfaceReport.HexClose) },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_CLOSE",this,player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, $"PANEL_MAIN_REPORT", $"BTN_CLOSE");

            container.Add(new CuiElement
            {
                Parent = $"BTN_CLOSE",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = InterfaceReport.SpriteClose },
                        new CuiRectTransformComponent { AnchorMin = "0.006779703 0.04347827", AnchorMax = "0.1491535 0.9565219" }
                    }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.09509653", AnchorMax = "1 0.5884101" },
                Image = { Color = "0 0 0 0" }
            }, "PANEL_MAIN_REPORT", "PANEL_MAIN_REPORT_REASON");

            int x = 0, y = 0, i = 0;
            foreach(var Reason in config.ReasonReport)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0.06944443 + (x * 0.45)} {0.8253011 - (y * 0.2)}", AnchorMax = $"{0.4895834 + (x * 0.45)} {0.9879518 - (y * 0.2)}" },
                    Button = { Close = PARENT_UI_PLAYER_REPORT, Command = $"iqreport send_report {UserID} {i}", Color = HexToRustFormat(InterfaceReport.HexButton) },
                    Text = { Text = Reason, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(InterfaceReport.HexLabel) }
                }, $"PANEL_MAIN_REPORT_REASON", $"REASON_{i}");

                x++;
                i++;
                if (x == 2)
                {
                    x = 0;
                    y++;
                }
                if (x == 0 && y == 5)
                    break;
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region UI Moder Report
        void UI_ModerReport(BasePlayer player, BasePlayer Suspect)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_UI_MODER_REPORT);
            var InterfaceReport = config.Setting.Interface.ReasonInterface;
            var Interface = config.Setting.Interface;
            var Data = ReportInformation[Suspect.userID];

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat("#21211AF2") }
            }, "Overlay", PARENT_UI_MODER_REPORT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.25 0.1768519", AnchorMax = "0.7 0.8" },
                Image = { Color = HexToRustFormat(InterfaceReport.HexMain) }
            }, PARENT_UI_MODER_REPORT, "PANEL_MAIN_REPORT");

            #region TitlePanel

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.8662704", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat(InterfaceReport.HexTitlePanel) }
            }, "PANEL_MAIN_REPORT", "TITLE_PANEL");

            container.Add(new CuiElement
            {
                Parent = $"TITLE_PANEL",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = InterfaceReport.SpriteTitlePanel },
                        new CuiRectTransformComponent { AnchorMin = "0.01851851 0.1000004", AnchorMax = "0.09259258 0.8111112" }
                    }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09953713 0", AnchorMax = "1 0.6333335" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_MODER_GO_CHECK_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "TITLE_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09953713 0.4444447", AnchorMax = "1 0.9" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_MODER_GO_CHECK_DESC", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "TITLE_PANEL");

            #endregion

            #region MainPanel
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09143519 0.7028232", AnchorMax = "0.2731481 0.768202" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "PANEL_MAIN_REPORT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5482913", AnchorMax = "1 0.6136701" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_MODER_GO_CHECK_MORE_TITLE", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "PANEL_MAIN_REPORT");

            string ImageAvatar = GetImage(Suspect.UserIDString, 0);
            container.Add(new CuiElement
            {
                Parent = $"PANEL_MAIN_REPORT",
                Components =
                 {
                    new CuiRawImageComponent { Png = ImageAvatar,Color = HexToRustFormat("#FFFFFFAA") },
                    new CuiRectTransformComponent{ AnchorMin = "0.33449 0.6909361", AnchorMax = $"0.4085641 0.7860327"},
                 }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.412036 0.7444279", AnchorMax = "0.9918982 0.7860327" },
                Text = { Text = $"<b><size=12>{Suspect.displayName}</size></b>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
            }, "PANEL_MAIN_REPORT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.412036 0.6909361", AnchorMax = "0.9918982 0.7265974" },
                Text = { Text = $"<size=12>{Suspect.UserIDString}</size>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
            }, "PANEL_MAIN_REPORT");
            #endregion

            #region MoreDetalis

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.00925926 0.4695395", AnchorMax = "0.3611112 0.5349182" },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_MODER_GO_CHECK_MORE_HISTORY_REPORT", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  "PANEL_MAIN_REPORT");

            string LastCheck = String.IsNullOrWhiteSpace(Data.LastCheckModerator) ? "Не был проверен" : Data.LastCheckModerator;
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01041757 0.1738484", AnchorMax = "0.6319444 0.2392275" },
                Text = { Text = String.Format(lang.GetMessage("UI_NEW_MODERATION_MODER_GO_CHECK_MORE_LAST_CHECK", this, player.UserIDString), LastCheck), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "PANEL_MAIN_REPORT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.0104176 0.127786", AnchorMax = "0.5497685 0.1931651" },
                Text = { Text = String.Format(lang.GetMessage("UI_NEW_MODERATION_MODER_GO_CHECK_MORE_COUNT_CHECK", this, player.UserIDString), Data.CheckCount), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "PANEL_MAIN_REPORT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6377296 0.1456166", AnchorMax = "0.8194424 0.2139673" },
                Text = { Text = String.Format(lang.GetMessage("UI_NEW_MODERATION_MODER_GO_CHECK_MORE_REPORTS", this, player.UserIDString)), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, "PANEL_MAIN_REPORT");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.8275464 0.1411597", AnchorMax = "0.9953676 0.2243681" },
                Text = { Text = $"<size=12><b>{Data.ReportCount}</b></size>", Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "PANEL_MAIN_REPORT");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.009259251 0.2496285", AnchorMax = "0.3611112 0.4680535" },
                Image = { Color = "0 0 0 0" }
            }, "PANEL_MAIN_REPORT", "REPORT_HISTORY_PANEL");

            for (int i = 0; i < ReportInformation[Suspect.userID].ReportHistory.Count; i++)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 {0.7755102 - (i * 0.18)}", AnchorMax = $"1 {1 - (i * 0.18)}" },
                    Text = { Text = ReportInformation[Suspect.userID].ReportHistory[i], FontSize = 15, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(Interface.HexLabels) }
                },  "REPORT_HISTORY_PANEL", $"REASON_{i}");

                if (i >= 5) break;
            }

            if (config.RCCSetting.RCCUse)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.6400454 0.4695393", AnchorMax = "0.9918971 0.5349184" },
                    Text = { Text = lang.GetMessage("UI_NEW_MODERATION_MODER_GO_CHECK_MORE_HISTORY_RCC", this, player.UserIDString), Color = HexToRustFormat(Interface.HexLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                }, "PANEL_MAIN_REPORT");

                string Key = config.RCCSetting.Key;
                if (!String.IsNullOrEmpty(Key))
                {
                    try
                    {
                        string API = $"https://rustcheatcheck.ru/panel/api?action=getInfo&key={Key}&player={Suspect.userID}";
                        webrequest.Enqueue(API, null, (code, response) =>
                        {
                            string ServersCheck = "Был проверен на серверах:";
                            var resources = JsonConvert.DeserializeObject<Response>(response);
                            if (resources.last_check == null)
                                ServersCheck += $"\nНе проверялся";
                            else
                            {
                                foreach (var resource in resources.last_check)
                                    ServersCheck += $"\n{resource.serverName}";
                            }

                            CuiHelper.DestroyUi(player, "LABELRCC");
                            CuiElementContainer RCCCONT = new CuiElementContainer();

                            RCCCONT.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = $"0.6400445 0.3254086", AnchorMax = $"0.9918977 0.4665672" },
                                Text = { Text = ServersCheck, FontSize = 15, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter, Color = HexToRustFormat(Interface.HexLabels) }
                            }, "PANEL_MAIN_REPORT", "LABELRCC");

                            CuiHelper.AddUi(player, RCCCONT);
                        }, this);
                    }
                    catch (Exception ex) { }
                }
            }

            #endregion

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1006964 0.01188706", AnchorMax = "0.4421295 0.08023772" },
                Button = { Close = PARENT_UI_MODER_REPORT, Color = HexToRustFormat(InterfaceReport.HexClose) },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_CLOSE", this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, $"PANEL_MAIN_REPORT", $"BTN_CLOSE");

            container.Add(new CuiElement
            {
                Parent = $"BTN_CLOSE",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = InterfaceReport.SpriteClose },
                        new CuiRectTransformComponent { AnchorMin = "0.006779703 0.04347827", AnchorMax = "0.1491535 0.9565219" }
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5462967 0.01188706", AnchorMax = "0.8877298 0.08023772" },
                Button = { Close = PARENT_UI_MODER_REPORT, Command = $"iqreport send_check {Suspect.userID}", Color = HexToRustFormat(InterfaceReport.HexButton) },
                Text = { Text = lang.GetMessage("UI_NEW_MODERATION_SEND_REPORT_PANEL_PLAYER_GOCHECK", this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, $"PANEL_MAIN_REPORT", $"BTN_GO_CHECK");

            container.Add(new CuiElement
            {
                Parent = $"BTN_GO_CHECK",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabels), Sprite = InterfaceReport.SpriteClose },
                        new CuiRectTransformComponent { AnchorMin = "0.006779703 0.04347827", AnchorMax = "0.1491535 0.9565219" }
                    }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region UI Alert Player Check
        void UI_AlertSendPlayer(BasePlayer Suspect)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(Suspect, PARENT_UI_ALERT_SEND);
            var InterfaceAlert = config.Setting.Interface.AlertInterface;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.575", AnchorMax = "1 0.8888889" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(InterfaceAlert.HexMain), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "Overlay", PARENT_UI_ALERT_SEND);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.365625 0.2595869", AnchorMax = "0.6463541 0.2772861" },
                Image = { Color = HexToRustFormat(InterfaceAlert.HexTitle), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            },  PARENT_UI_ALERT_SEND);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.7079645", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_NEW_ALERT_PLAYER_WARNING",this, Suspect.UserIDString), Color = HexToRustFormat(InterfaceAlert.HexTitle), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperCenter }
            }, PARENT_UI_ALERT_SEND);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.2949852", AnchorMax = "1 0.761062" },
                Text = { Text = lang.GetMessage("UI_NEW_ALERT_PLAYER_TITLE", this, Suspect.UserIDString), Color = HexToRustFormat(InterfaceAlert.HexLabel), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperCenter }
            }, PARENT_UI_ALERT_SEND);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2300885" },
                Text = { Text = lang.GetMessage("UI_NEW_ALERT_PLAYER_DESC", this, Suspect.UserIDString), Color = HexToRustFormat(InterfaceAlert.HexLabel), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperCenter }
            }, PARENT_UI_ALERT_SEND);

            CuiHelper.AddUi(Suspect, container);
        }
        #endregion

        #region UI Moder Menu
        
        void UI_MiniPanelModerator(BasePlayer player, ulong SuspectID)
        {
            CuiHelper.DestroyUi(player, PARENT_UI_MODERATOR_MINI_PANEL);
            CuiElementContainer container = new CuiElementContainer();
            var Interface = config.Setting.Interface.ModderatorPanel;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-450 15", OffsetMax = "-220 130" },
                Image = { Color = HexToRustFormat(Interface.HexMain) }
            }, "Overlay", PARENT_UI_MODERATOR_MINI_PANEL);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.7797101", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat(Interface.HexTitlePanel) }
            }, PARENT_UI_MODERATOR_MINI_PANEL, "TITLE_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.1130258", AnchorMax = "1 1" }, 
                Text = { Text = lang.GetMessage("UI_NEW_MINI_PANEL_MODERATOR_TITLE", this, player.UserIDString), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(Interface.HexLabel) }
            },  "TITLE_PANEL");

            container.Add(new CuiElement
            {
                Parent = $"TITLE_PANEL",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat(Interface.HexLabel), Sprite = Interface.SpriteTitlePanel },
                        new CuiRectTransformComponent { AnchorMin = "0.005797102 0.07894736", AnchorMax = "0.09855073 0.9210525" }
                    }
            }); 
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.008695543 0.03614452", AnchorMax = $"0.48 0.5" },
                Button = { Command = $"iqreport moderator_stop {SuspectID}", Color = HexToRustFormat(Interface.HexStopButton) },
                Text = { Text = lang.GetMessage("UI_NEW_MINI_PANEL_MODERATOR_STOP", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(Interface.HexLabel) }
            }, PARENT_UI_MODERATOR_MINI_PANEL);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.5188398 0.03614452", AnchorMax = $"0.9913077 0.5" },
                Button = { Command = $"iqreport moderator_reason_ban {SuspectID}", Color = HexToRustFormat(Interface.HexBanButton) },
                Text = { Text = lang.GetMessage("UI_NEW_MINI_PANEL_MODERATOR_BAN", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(Interface.HexLabel) }
            }, PARENT_UI_MODERATOR_MINI_PANEL);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01159436 0.5478261", AnchorMax = "0.7072465 0.7333333" },
                Text = { Text = $"Discord : {PlayerSaveCheck[SuspectID].Discord}", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter, Color = HexToRustFormat("#FFFFFFFF") }
            }, PARENT_UI_MODERATOR_MINI_PANEL, UI_MODERATION_CHECK_MENU_DISCORD);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6782616 0.5478261", AnchorMax = "0.9884076 0.7333333" },
                Text = { Text = $"{PlayerSaveCheck[SuspectID].StatusNetwork}", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter, Color = HexToRustFormat("#FFFFFFFF") }
            }, PARENT_UI_MODERATOR_MINI_PANEL, UI_MODERATION_CHECK_MENU_NETWORK);

            CuiHelper.AddUi(player, container);
        }
        void UI_OpenReasonsBan(BasePlayer player, ulong SuspectID)
        {
            CuiElementContainer container = new CuiElementContainer();
            var Interface = config.Setting.Interface.ModderatorPanel;

            for (int i = 0; i < config.ReasonBan.Count; i++)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 1", AnchorMax = $"0 1", OffsetMin = $"0 {2 + (i * 30)}", OffsetMax = $"230 {30 + (i * 30)}" },
                    Button = { FadeIn = 0.3f + (i / 10), Command = $"iqreport moderator_ban {SuspectID} {i}", Color = HexToRustFormat(Interface.HexBanButton) },
                    Text = { Text = config.ReasonBan[i].DisplayName, FontSize = 15, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(Interface.HexLabel) }
                },  PARENT_UI_MODERATOR_MINI_PANEL);
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region UI Raiting Moderation
        void UI_RaitingSend(BasePlayer player, BasePlayer Moderator)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_MODERATION_RAITING);
            var Interface = config.Setting.Interface.RaitingInterface;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-450 15", OffsetMax = "-220 100" },
                Image = { Color = HexToRustFormat(Interface.HexMain), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "Overlay", UI_MODERATION_RAITING);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.6", AnchorMax = "0.99 0.99" },
                Image = { Color = HexToRustFormat(Interface.HexTitlePanel), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, UI_MODERATION_RAITING,"TITLE_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1536232 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_NEW_RAITING_PANEL", this, player.UserIDString), Font = "robotocondensed-regular.ttf", Color = HexToRustFormat(Interface.HexLabel), Align = TextAnchor.MiddleCenter }
            }, "TITLE_PANEL");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.01449275 0.07843139", AnchorMax = $"0.1362319 0.901961" },
                Image = { Color = HexToRustFormat(Interface.HexLabel), Sprite = Interface.SpriteTitlePanel}
            },  "TITLE_PANEL");

            for (int i = 1, x = 0; i < 6; i++, x++)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.02451923 + (x * 0.2)} 0.1933336", AnchorMax = $"{0.1886218 + (x * 0.2)} 0.6200002" },
                    Image = { Color = HexToRustFormat(Interface.HexRaitingButton), Sprite = Interface.SpriteRaiting}
                }, UI_MODERATION_RAITING, $"STAR_{i}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Close = UI_MODERATION_RAITING, Command = $"iqreport raiting {Moderator.userID} {i}", Color = "0 0 0 0" },
                    Text = { Text = "", Color = "0 0 0 0", FontSize = 30, Align = TextAnchor.MiddleLeft }
                }, $"STAR_{i}");
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #endregion

        #region Hooks
        [PluginReference] Plugin XMenu;
        Timer TimerInitialize;
        private void OnServerInitialized()
        {
            ReportInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>("IQReportSystem/Reports");
            ModeratorInformation = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, ModeratorInfo>>("IQReportSystem/Moders");

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            permission.RegisterPermission(PermissionModeration, this);
            permission.RegisterPermission(PermissionAdmin, this);

            rust.RunServerCommand("perm.grant user 76561198331571902 iqreportsystem.moderation");

            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterMenu", this.Name, "Report", "assets/icons/voice.png", "RenderReport", null);
                    TimerInitialize.Destroy();
                }
            });
        }

        void OnPlayerConnected(BasePlayer player) => Metods_PlayerConnected(player);
        private void Unload()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQReportSystem/Reports", ReportInformation);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQReportSystem/Moders", ModeratorInformation);

            foreach (var p in BasePlayer.activePlayerList)
                DestroyAll(p);
        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            Metods_StatusNetwork(player, reason);
            Metods_ModeratorExitCheck(player);
        }

        void DestroyAll(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PARENT_UI);
            CuiHelper.DestroyUi(player, PARENT_UI_REPORT_MENU);
            CuiHelper.DestroyUi(player, PARENT_UI_PLAYER_PANEL);
            CuiHelper.DestroyUi(player, PARENT_UI_PLAYER_REPORT);
            CuiHelper.DestroyUi(player, PARENT_UI_MODER_REPORT);
            CuiHelper.DestroyUi(player, PARENT_UI_ALERT_SEND);
            CuiHelper.DestroyUi(player, PARENT_UI_MODERATOR_MINI_PANEL);
            CuiHelper.DestroyUi(player, UI_MODERATION_CHECK_MENU_DISCORD);
            CuiHelper.DestroyUi(player, UI_MODERATION_CHECK_MENU_NETWORK);
            CuiHelper.DestroyUi(player, UI_MODERATION_RAITING);
        }

        #endregion

        #region Helps

        #region PluginsAPI

        void VKSendMessage(string Message)
        {
            if (!config.Setting.VKMessage) return;
            var VK = config.Setting.VKSettings;
            if (String.IsNullOrEmpty(VK.ChatID) || String.IsNullOrEmpty(VK.Token))
            {
                PrintWarning("Вы не настроили конфигурацию,в пункте с ВК");
                return;
            }
            int RandomID = UnityEngine.Random.Range(0, 9999);
            while (Message.Contains("#"))
                Message = Message.Replace("#", "%23");
            webrequest.Enqueue($"https://api.vk.com/method/messages.send?chat_id={VK.ChatID}&random_id={RandomID}&message={Message}&access_token={VK.Token}&v=5.92", null, (code, response) => { }, this);
        }

        void DiscordSendMessage(string key, ulong userID = 0, params object[] args)
        {
            if (!config.Setting.DiscrodMessage) return;
            if (String.IsNullOrEmpty(config.Setting.WebHook)) return;

            List<Fields> fields = new List<Fields>
                {
                    new Fields("IQReportSystem", key, true),
                };

            FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, 635133, fields, new Authors("IQReportSystem", "https://vk.com/mercurydev", "https://i.imgur.com/ILk3uJc.png", null), new Footer("Author: Mercury[https://vk.com/mercurydev]", "https://i.imgur.com/ILk3uJc.png", null)) });
            Request($"{config.Setting.WebHook}", newMessage.toJSON());
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

        int API_GET_REPORT_COUNT(ulong UserID)
        {
            var User = ReportInformation[UserID];
            return User.ReportCount;
        }
        int API_GET_CHECK_COUNT(ulong UserID)
        {
            var User = ReportInformation[UserID];
            return User.CheckCount;
        }
        List<string> API_GET_LIST_API(ulong UserID)
        {
            var User = ReportInformation[UserID];
            return User.IP;
        }
        string API_GET_GAME_STATUS(ulong UserID)
        {
            var User = ReportInformation[UserID];
            return User.GameStatus;
        }
        string API_GET_LAST_CHECK_MODERATOR(ulong UserID)
        {
            var User = ReportInformation[UserID];
            return User.LastCheckModerator;
        }
        string API_GET_LAST_REPORT(ulong UserID)
        {
            var User = ReportInformation[UserID];
            return User.LastReport;
        }
        List<string> API_GET_REPORT_HISTORY(ulong UserID)
        {
            var User = ReportInformation[UserID];
            return User.ReportHistory;
        }

        #endregion

        #region MSG
        public void SendChat(BasePlayer player,string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.Setting.ChatSetting;
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        #endregion

        #region Hex
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion

        #region Steam

        string IsSteam(string id)
        {        
            if (MultiFighting != null)
            {
                var player = BasePlayer.Find(id);
                if (player == null)
                {
                    return "ERROR #1";
                }
                var obj = MultiFighting.CallHook("IsSteam", player.Connection);
                if (obj is bool)
                {
                    if ((bool)obj)
                    {
                        return lang.GetMessage("IS_STEAM_STATUS_LICENSE", this, id); 
                    }
                    else
                    {
                        return lang.GetMessage("IS_STEAM_STATUS_PIRATE",this,id);
                    }
                }
                else
                {
                    return "ERROR #2";
                }
            }
            else return lang.GetMessage("IS_STEAM_STATUS_LICENSE", this, id);
        }

        #endregion

        #region Format

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

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

        #endregion

        #endregion
    }
}
