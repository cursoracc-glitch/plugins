using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQChat", "Mercury", "0.1.0")]
    [Description("aaaaaaaaaaaaaaaaaaa")]
    class IQChat : RustPlugin
    {
        #region Vars
        public string PermMuteMenu = "iqchat.muteuse";
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Права для смены ника")]
            public string RenamePermission;
            [JsonProperty("Настройка префиксов")]
            public Dictionary<string, string> PrefixList = new Dictionary<string, string>();
            [JsonProperty("Настройка цветов для ников")]
            public Dictionary<string, string> NickColorList = new Dictionary<string, string>();
            [JsonProperty("Настройка цветов для сообщений")]
            public Dictionary<string, string> MessageColorList = new Dictionary<string, string>();
            [JsonProperty("Настройка сообщений в чате")]
            public MessageSettings MessageSetting;                                  
            [JsonProperty("Настройка причин блокировок чата")]
            public Dictionary<string, int> ReasonListChat = new Dictionary<string, int>();
            [JsonProperty("Настройка интерфейса")]
            public InterfaceSettings InterfaceSetting;
            [JsonProperty("Настройка оповещения")]
            public AlertSetting AlertSettings;

            internal class MessageSettings
            {
                [JsonProperty("Наименование оповещения в чат")]
                public string BroadcastTitle;
                [JsonProperty("Цвет сообщения оповещения в чат")]
                public string BroadcastColor;
                [JsonProperty("На какое сообщение заменять плохие слова")]
                public string ReplaceBadWord;
                [JsonProperty("Steam64ID для аватарки в чате")]
                public ulong Steam64IDAvatar;
                [JsonProperty("Список плохих слов")]
                public List<string> BadWords = new List<string>();
                [JsonProperty("Время,через которое удалится сообщение с UI от администратора")]
                public int TimeDeleteAlertUI;
                [JsonProperty("Включить форматирование сообщений")]
                public bool FormatingMessage;
                [JsonProperty("Включить личные сообщения")]
                public bool PMActivate;
                [JsonProperty("Включить Анти-Спам")]
                public bool AntiSpamActivate;
                [JsonProperty("Звук при при получении личного сообщения")]
                public string SoundPM;
                [JsonProperty("Время через которое игрок может отправлять сообщение (АнтиСпам)")]
                public int FloodTime;
            }

            internal class InterfaceSettings
            {
                [JsonProperty("Основной цвет UI")]
                public string MainColor;
                [JsonProperty("Дополнительный #1 цвет UI")]
                public string TwoMainColor;
                [JsonProperty("Дополнительный #2 цвет UI")]
                public string ThreeMainColor;
                [JsonProperty("Основной цвет UI панели МУТОВ")]
                public string MainColorMute;
            }

            internal class AlertSetting
            {
                [JsonProperty("Включить случайное сообщение зашедшему игроку")]
                public bool WelcomeMessageUse;
                [JsonProperty("Список сообщений игроку при входе")]
                public List<string> WelcomeMessage = new List<string>();
                [JsonProperty("Уведомлять о входе игрока в чат")]
                public bool ConnectedAlert;
                [JsonProperty("Уведомлять о выходе игрока в чат")]
                public bool DisconnectedAlert;
                [JsonProperty("При уведомлении о входе/выходе игрока отображать его аватар напротив ника")]
                public bool ConnectedAvatarUse;
                [JsonProperty("Включить автоматические сообщения в чат")]
                public bool AlertMessage;
                [JsonProperty("Настройка отправки автоматических сообщений в чат")]
                public List<string> MessageList;
                [JsonProperty("Интервал отправки сообщений в чат(Броадкастер)")]
                public int MessageListTimer;
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    PrefixList = new Dictionary<string, string>
                    {
                        ["iqchat.default"] = "<color=yellow><b>[+]</b></color>",
                        ["iqchat.vip"] = "<color=yellow><b>[VIP]</b></color>",
                        ["iqchat.premium"] = "<color=red><b>[PREMIUM]</b></color>",
                    },
                    NickColorList = new Dictionary<string, string>
                    {
                        ["iqchat.default"] = "#DBEAEC",
                        ["iqchat.vip"] = "#FFC428",
                        ["iqchat.premium"] = "#45AAB4",
                    },
                    MessageColorList = new Dictionary<string, string>
                    {
                        ["iqchat.default"] = "#DBEAEC",
                        ["iqchat.vip"] = "#FFC428",
                        ["iqchat.premium"] = "#45AAB4",
                    },
                    MessageSetting = new MessageSettings
                    {
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
                        BadWords = new List<string> { "хуй", "гей", "говно", "бля", "тварь" }
                    },
                    ReasonListChat = new Dictionary<string, int>
                    {
                        ["Оскорбление игроков"] = 120,
                        ["Оскорбление родителей"] = 1200,
                    },
                    RenamePermission = "iqchat.renameuse",                  
                    AlertSettings = new AlertSetting
                    {
                        MessageListTimer = 60,
                        WelcomeMessageUse = true,
                        ConnectedAlert = true,
                        DisconnectedAlert = true,
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
                        MainColor = "#6B803EFF",
                        TwoMainColor = "#6D942BFF",
                        ThreeMainColor = "#8EBA43FF",
                        MainColorMute = "#6A803EFF",
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
                PrintWarning("Ошибка #1" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        void RegisteredPermissions()
        {
            for(int i = 0; i < config.MessageColorList.Count; i++)
                permission.RegisterPermission(config.MessageColorList.ElementAt(i).Key, this);
            for (int j = 0; j < config.NickColorList.Count; j++)
                permission.RegisterPermission(config.NickColorList.ElementAt(j).Key, this);
            for (int g = 0; g < config.PrefixList.Count; g++)
                permission.RegisterPermission(config.PrefixList.ElementAt(g).Key, this);

            permission.RegisterPermission(config.RenamePermission, this);
            permission.RegisterPermission(PermMuteMenu, this);
            PrintWarning("Permissions - completed");
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        [JsonProperty("Дата с настройкой чата игрока")] public Dictionary<ulong, SettingUser> ChatSettingUser = new Dictionary<ulong, SettingUser>();
        [JsonProperty("Дата с Административной настройкой")] public AdminSettings AdminSetting = new AdminSettings();
        public class SettingUser
        {
            public string ChatPrefix;
            public string NickColor;
            public string MessageColor;
            public double MuteChatTime;
            public double MuteVoiceTime;
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
        void WriteData() => timer.Every(60f, () =>
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQChat/IQUser", ChatSettingUser);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQChat/AdminSetting", AdminSetting);
        });

        void RegisteredDataUser(BasePlayer player)
        {
            if (!ChatSettingUser.ContainsKey(player.userID))
                ChatSettingUser.Add(player.userID, new SettingUser
                {
                    ChatPrefix = config.PrefixList.ElementAt(0).Value,
                    NickColor = config.NickColorList.ElementAt(0).Value,
                    MessageColor = config.MessageColorList.ElementAt(0).Value,
                    MuteChatTime = 0,
                    MuteVoiceTime = 0,
                });
        }

        #endregion

        #region Hooks
       
        private bool OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (Interface.Oxide.CallHook("CanChatMessage", player, message) != null) return false;
            Message(channel, player, message);
            return false;
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
            BasePlayer.activePlayerList.ForEach(p => RegisteredDataUser(p));
            RegisteredPermissions();
            WriteData();
            BroadcastAuto();
        }
        private void OnPlayerInit(BasePlayer player)
        {
            RegisteredDataUser(player);
            ReturnDefaultData(player);
            var Alert = config.AlertSettings;
            if (Alert.ConnectedAlert)
            {
                string Avatar = Alert.ConnectedAvatarUse ? player.UserIDString : "";
                ReplyBroadcast(String.Format(lang.GetMessage("WELCOME_PLAYER", this, player.UserIDString), player.displayName), "", Avatar);
            }
            if (Alert.WelcomeMessageUse)
            {
                int RandomMessage = UnityEngine.Random.Range(0, Alert.WelcomeMessage.Count);
                string WelcomeMessage = Alert.WelcomeMessage[RandomMessage];
                ReplySystem(Chat.ChatChannel.Global, player, WelcomeMessage);
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var Alert = config.AlertSettings;
            if (Alert.DisconnectedAlert)
            {
                string Avatar = Alert.ConnectedAvatarUse ? player.UserIDString : "";
                ReplyBroadcast(String.Format(lang.GetMessage("LEAVE_PLAYER", this, player.UserIDString), player.displayName), "", Avatar);
            }
        }
        #endregion

        #region Func
        private void Message(Chat.ChatChannel channel, BasePlayer player, string message)
        {
            if (config.MessageSetting.FormatingMessage)
            {
                message = message.ToLower();
                var firstLetter = message.Substring(0, 1);
                message = message.Remove(0, 1);
                message = firstLetter.ToUpper() + message;
            }
            var cfg = config.MessageSetting;

            for (int i = 0; i < cfg.BadWords.Count; i++)
            {
                var BadWord = cfg.BadWords[i];
                foreach (var msg in message.Split(' '))
                {
                    if (msg.ToLower() == BadWord.ToLower())
                        message = message.Replace(msg, cfg.ReplaceBadWord, StringComparison.OrdinalIgnoreCase);
                }
            }
            string FormatMessage = "";
            var DataPlayer = ChatSettingUser[player.userID];

            bool IsMuted = DataPlayer.MuteChatTime > CurrentTime() ? true : false;
            if(IsMuted)
            {
                ReplySystem(Chat.ChatChannel.Global, player,string.Format(lang.GetMessage("FUNC_MESSAGE_ISMUTED_TRUE",this,player.UserIDString),FormatTime(TimeSpan.FromSeconds(DataPlayer.MuteChatTime - CurrentTime()))));
                return;
            }
        
            Dictionary<string, object> ChatTags = new Dictionary<string, object>
            {
                ["Player"] = player,
                ["Message"] = message,
                ["Prefixes"] = DataPlayer.ChatPrefix
            };

            var HookResult = Interface.Oxide.CallHook("OnChatSystemMessage", ChatTags);
            var DisplayNick = AdminSetting.RenameList.ContainsKey(player.userID) ? AdminSetting.RenameList[player.userID] : player.displayName;
            if (HookResult != null)
            {
                if (HookResult is bool) return;
                if (channel == Chat.ChatChannel.Team)
                    FormatMessage = $"<color=#a5e664>[Team]</color> {ChatTags["Prefixes"].ToString().Replace("— ", "")} <color={DataPlayer.NickColor}>{DisplayNick}</color>: <color={DataPlayer.MessageColor}>{ChatTags["Message"]}</color>";
                else FormatMessage = $"{ChatTags["Prefixes"].ToString().Replace("— ", "")} <color={DataPlayer.NickColor}>{DisplayNick}</color>:  <color={DataPlayer.MessageColor}>{ChatTags["Message"]}</color>";
            }
            else
                if (channel == Chat.ChatChannel.Team)
                    FormatMessage = $"<color=#a5e664>[Team]</color> {DataPlayer.ChatPrefix.Replace("—", "")} <color={DataPlayer.NickColor}>{DisplayNick}</color>: <color={DataPlayer.MessageColor}>{message}</color>";
                else FormatMessage = $"{DataPlayer.ChatPrefix.Replace("—", "")} <color={DataPlayer.NickColor}>{DisplayNick}</color>: <color={DataPlayer.MessageColor}>{message}</color>";

            ReplyChat(channel, player, FormatMessage);
            Puts($"{player}: {message}");
            Log($"СООБЩЕНИЕ В ЧАТ : {FormatMessage}");
        }

        public void ReturnDefaultData(BasePlayer player)
        {
            var DataPlayer = ChatSettingUser[player.userID];
            var PrefixPerm = config.PrefixList.FirstOrDefault(x => x.Value == DataPlayer.ChatPrefix).Key;
            var PrefixColorMsg= config.MessageColorList.FirstOrDefault(x => x.Value == DataPlayer.MessageColor).Key;
            var PrefixColorNick = config.NickColorList.FirstOrDefault(x => x.Value == DataPlayer.NickColor).Key;

            if (!permission.UserHasPermission(player.UserIDString, PrefixPerm))
                DataPlayer.ChatPrefix = config.PrefixList.ElementAt(0).Value;

            if (!permission.UserHasPermission(player.UserIDString, PrefixColorMsg))
                DataPlayer.MessageColor = config.MessageColorList.ElementAt(0).Value;

            if (!permission.UserHasPermission(player.UserIDString, PrefixColorNick))
                DataPlayer.NickColor = config.NickColorList.ElementAt(0).Value;
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

        public void MutePlayer(BasePlayer player,BasePlayer Initiator,string Format,int ReasonIndex)
        {
            var cfg = config.ReasonListChat.ElementAt(ReasonIndex);
            string Reason = cfg.Key;
            float TimeMute = cfg.Value;
            switch (Format)
            {
                case "mutechat":
                    {
                        ChatSettingUser[player.userID].MuteChatTime = TimeMute + CurrentTime();
                        ReplyBroadcast(string.Format(lang.GetMessage("FUNC_MESSAGE_MUTE_CHAT", this, player.UserIDString), Initiator.displayName,player.displayName,FormatTime(TimeSpan.FromSeconds(TimeMute)), Reason));
                        break;
                    }
                case "unmutechat":
                    {
                        ChatSettingUser[player.userID].MuteChatTime = 0;
                        ReplyBroadcast(string.Format(lang.GetMessage("FUNC_MESSAGE_UNMUTE_CHAT", this, player.UserIDString), Initiator.displayName));
                        break;
                    }
                case "mutevoice":
                    {
                        ChatSettingUser[player.userID].MuteVoiceTime = TimeMute + CurrentTime();
                        ReplyBroadcast(string.Format(lang.GetMessage("FUNC_MESSAGE_MUTE_VOICE", this), Initiator.displayName, player.displayName, FormatTime(TimeSpan.FromSeconds(TimeMute)), Reason)); 
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
                ReplySystem(Chat.ChatChannel.Global, player, String.Format(lang.GetMessage("COMMAND_RENAME_SUCCES", this, player.UserIDString),NewName));
            }
            else ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_NOT_PERMISSION", this, player.UserIDString)); 
        }

        #endregion

        #region Interface
        static string MAIN_PARENT = "MAIN_PARENT_UI";
        static string MUTE_MENU_PARENT = "MUTE_MENU_UI";
        static string ELEMENT_SETTINGS = "NEW_ELEMENT_SETTINGS";
        static string MAIN_ALERT_UI = "ALERT_UI_PLAYER";

        public void UI_MainMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, MAIN_PARENT);

            #region Panels

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5",OffsetMin = "-300 -150", OffsetMax = "0 200" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(config.InterfaceSetting.MainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", MAIN_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.8628572", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            },  MAIN_PARENT, "TITLE_PANEL");

            #endregion

            #region Labels

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.3888887", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("TITLE_ONE",this,player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TITLE_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.4694438" },
                Text = { Text = lang.GetMessage("TITLE_TWO", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TITLE_PANEL");

            #region BtnLabels

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01 0.7828571", AnchorMax = "1 0.8419046" },
                Text = { Text = lang.GetMessage("UI_TEXT_PREFIX", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft, FadeIn = 0.3f }
            },  MAIN_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01 0.6285695", AnchorMax = "1 0.6876169" },
                Text = { Text = lang.GetMessage("UI_TEXT_COLOR_NICK", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft, FadeIn = 0.3f }
            }, MAIN_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.01 0.3806", AnchorMax = "1 0.5428537" },
                Text = { Text = lang.GetMessage("UI_TEXT_COLOR_MSG", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft, FadeIn = 0.3f }
            }, MAIN_PARENT);

            #endregion

            #endregion

            #region Buttons

            container.Add(new CuiButton
            {
                FadeOut = 0.2f,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0",OffsetMin = "0 -30", OffsetMax = "300 -1" },
                Button = { Close = MAIN_PARENT, Color = HexToRustFormat("#C43E28FF"), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                Text = { Text = lang.GetMessage("UI_CLOSE_BTN",this,player.UserIDString), Color = HexToRustFormat("#FFBBB0FF"), Align = TextAnchor.MiddleCenter }
            }, MAIN_PARENT,"BTN_CLOSE_PARENT");

            container.Add(new CuiElement
            {
                Parent = "BTN_CLOSE_PARENT",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#772500FF"), Sprite = "assets/icons/close.png"  },
                        new CuiRectTransformComponent { AnchorMin = "0.01896266 0.1418719", AnchorMax = "0.09458128 0.8512315" }
                    }
            });

            #region PrefixSettings

            container.Add(new CuiButton
            {
                FadeOut = 0.2f,
                RectTransform = { AnchorMin = "0 0.7085695", AnchorMax = "1 0.7790456" },
                Button = { Command = "iq_chat newelementsettingsprefix", Color = HexToRustFormat(config.InterfaceSetting.ThreeMainColor), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                Text = { Text = lang.GetMessage("UI_TEXT_GO_SETTINGS", this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, MAIN_PARENT, "BTN_PREFIX_SETTINGS");

            container.Add(new CuiElement
            {
                Parent = "BTN_PREFIX_SETTINGS",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = "assets/icons/gear.png",  },
                        new CuiRectTransformComponent { AnchorMin = "0.01333339 0.1621625", AnchorMax = "0.06666672 0.8108124" }
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "BTN_PREFIX_SETTINGS",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = "assets/icons/gear.png"  },
                        new CuiRectTransformComponent { AnchorMin = "0.9333356 0.1621625", AnchorMax = "0.9866689 0.8108124" }
                    }
            });

            #endregion

            #region NickColorSettings

            container.Add(new CuiButton
            {
                FadeOut = 0.2f,
                RectTransform = { AnchorMin = "0 0.5542819", AnchorMax = "1 0.62475266" },
                Button = { Command = "iq_chat newelementsettingscolornick", Color = HexToRustFormat(config.InterfaceSetting.ThreeMainColor), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                Text = { Text = lang.GetMessage("UI_TEXT_GO_SETTINGS", this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, MAIN_PARENT, "BTN_NICK_COLOR_SETTINGS");

            container.Add(new CuiElement
            {
                Parent = "BTN_NICK_COLOR_SETTINGS",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = "assets/icons/gear.png"  },
                        new CuiRectTransformComponent { AnchorMin = "0.01333339 0.1621625", AnchorMax = "0.06666672 0.8108124" }
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "BTN_NICK_COLOR_SETTINGS",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = "assets/icons/gear.png"  },
                        new CuiRectTransformComponent { AnchorMin = "0.9333356 0.1621625", AnchorMax = "0.9866689 0.8108124" }
                    }
            });

            #endregion

            #region MsgColorSettings

            container.Add(new CuiButton
            {
                FadeOut = 0.2f,
                RectTransform = { AnchorMin = "0 0.4114244", AnchorMax = "1 0.19015" },
                Button = { Command = "iq_chat newelementsettingcolormessage", Color = HexToRustFormat(config.InterfaceSetting.ThreeMainColor), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                Text = { Text = lang.GetMessage("UI_TEXT_GO_SETTINGS", this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            }, MAIN_PARENT, "BTN_MSG_COLOR_SETTINGS");

            container.Add(new CuiElement
            {
                Parent = "BTN_MSG_COLOR_SETTINGS",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = "assets/icons/gear.png"  },
                        new CuiRectTransformComponent { AnchorMin = "0.01333339 0.1621625", AnchorMax = "0.06666672 0.8108124" }
                    }
            });

            container.Add(new CuiElement
            {
                Parent = "BTN_MSG_COLOR_SETTINGS",
                Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = "assets/icons/gear.png"  },
                        new CuiRectTransformComponent { AnchorMin = "0.9333356 0.1621625", AnchorMax = "0.9866689 0.8108124" }
                    }
            });

            #endregion

            #endregion

            #region AdminPanel
            if (player.IsAdmin)
            {
                string CommandChat = "iq_chat admin_chat";
                string ColorMuteChatButton = AdminSetting.MuteChatAll ? "#C43E28FF" : config.InterfaceSetting.ThreeMainColor;
                string TextMuteChatButton = AdminSetting.MuteChatAll ? "UI_TEXT_ADMIN_PANEL_UNMUTE_CHAT_ALL" : "UI_TEXT_ADMIN_PANEL_MUTE_CHAT_ALL";
                string CommandMuteChatButton = AdminSetting.MuteChatAll ? "unmutechat" : "mutechat";
                string CommandVoice = "iq_chat admin_voice";
                string ColorMuteVoiceButton = AdminSetting.MuteVoiceAll ? "#C43E28FF" : config.InterfaceSetting.ThreeMainColor;
                string TextMuteVoiceButton = AdminSetting.MuteVoiceAll ? "UI_TEXT_ADMIN_PANEL_UNMUTE_VOICE_ALL" : "UI_TEXT_ADMIN_PANEL_MUTE_VOICE_ALL";
                string CommandMuteVoiceButton = AdminSetting.MuteVoiceAll ? "unmutevoice" : "mutevoice";

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0.3104762", AnchorMax = "1 0.3923809" },
                    Image = { Color = HexToRustFormat(config.InterfaceSetting.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
                }, MAIN_PARENT, "AdminPanel_TITLE");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = lang.GetMessage("UI_TEXT_ADMIN_PANEL", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
                }, "AdminPanel_TITLE");

                container.Add(new CuiButton
                {
                    FadeOut = 0.2f,
                    RectTransform = { AnchorMin = "0 0.2342857", AnchorMax = "1 0.3047618" },
                    Button = { Close = MAIN_PARENT, Command = $"{CommandChat} {CommandMuteChatButton}", Color = HexToRustFormat(ColorMuteChatButton), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                    Text = { Text = lang.GetMessage(TextMuteChatButton, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
                }, MAIN_PARENT);

                container.Add(new CuiButton
                {
                    FadeOut = 0.2f,
                    RectTransform = { AnchorMin = "0 0.1580954", AnchorMax = "1 0.2285715" },
                    Button = { Close = MAIN_PARENT, Command = $"{CommandVoice} {CommandMuteVoiceButton}", Color = HexToRustFormat(ColorMuteVoiceButton), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                    Text = { Text = lang.GetMessage(TextMuteVoiceButton, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
                }, MAIN_PARENT);               
            }
            #endregion

            CuiHelper.AddUi(player, container);
        }

        public void UI_MuteMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, MUTE_MENU_PARENT);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-500 -250", OffsetMax = "500 300" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(config.InterfaceSetting.MainColorMute), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", MUTE_MENU_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.8957576", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, MUTE_MENU_PARENT, "TITLE_PANEL_MUTE");

            #region PlayerList
            int x = 0; int y = 0;
            foreach (var pList in BasePlayer.activePlayerList)
            {
                string ColorButton = ChatSettingUser[pList.userID].MuteChatTime > CurrentTime() ? "#C43E28FF" : ChatSettingUser[pList.userID].MuteVoiceTime > CurrentTime() ? "#C43E28FF" : config.InterfaceSetting.ThreeMainColor;
                container.Add(new CuiButton
                {
                    FadeOut = 0.2f,
                    RectTransform = { AnchorMin = $"{0.004058793 + (x * 0.144)} {0.7551514 - (y * 0.05)}", AnchorMax = $"{0.1306667 + (x * 0.144)} {0.7974398 - (y * 0.05)}" },
                    Button = { Command = $"iq_chat mute_take_action {pList.userID}", Color = HexToRustFormat(ColorButton), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                    Text = { Text = pList.displayName, Align = TextAnchor.MiddleCenter }
                }, MUTE_MENU_PARENT, "BUTTON" + player.userID);

                x++;
                if (x == 7)
                {
                    y++;
                    x = 0;
                }
                if (y == 13 && x == 6) break;

            };

            #endregion

            #region Helps

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.002666675 0.05212118", AnchorMax = "0.02333329 0.0881" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.ThreeMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, MUTE_MENU_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.002666662 0.006060584", AnchorMax = "0.02333332 0.04242422" },
                Image = { Color = HexToRustFormat("#C43E28FF"), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, MUTE_MENU_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.02133331 0.05212118", AnchorMax = "0.282 0.0882" },
                Text = { Text = lang.GetMessage("UI_MUTE_PANEL_TITLE_HELPS_GREEN", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, MUTE_MENU_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.02133331 0.00823", AnchorMax = "0.2746667 0.04" },
                Text = { Text = lang.GetMessage("UI_MUTE_PANEL_TITLE_HELPS_RED", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, MUTE_MENU_PARENT);

            #endregion

            #region Labels

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.2906973", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_MUTE_PANEL_TITLE", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TITLE_PANEL_MUTE");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.4418605" },
                Text = { Text = lang.GetMessage("UI_MUTE_PANEL_TITLE_ACTION", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TITLE_PANEL_MUTE");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8036363", AnchorMax = "1 0.8436363" },
                Text = { Text = lang.GetMessage("UI_MUTE_PANEL_TITLE_PLIST", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, MUTE_MENU_PARENT);

            #endregion

            #region Buttons

            container.Add(new CuiButton
            {
                FadeOut = 0.2f,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -35", OffsetMax = "0 -3" },
                Button = { Close = MUTE_MENU_PARENT, Color = HexToRustFormat("#C43E28FF"), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                Text = { Text = lang.GetMessage("UI_CLOSE_BTN", this, player.UserIDString), Color = HexToRustFormat("#FFBBB0FF"), Align = TextAnchor.MiddleCenter }
            }, MUTE_MENU_PARENT, "BTN_CLOSE_MUTE_MENU_PARENT");

            #endregion

            CuiHelper.AddUi(player, container);
        }

        public void UI_MuteTakeAction(BasePlayer player,ulong userID)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "TAKE_ACTION_MUTE");

            #region Panels

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3526041 0.6037037", AnchorMax = "0.65125 0.778889" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.MainColorMute), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, MUTE_MENU_PARENT, "TAKE_ACTION_MUTE");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.6712323", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "TAKE_ACTION_MUTE","TITLE_MENU_MUTE_ACTION");

            #endregion

            #region Labels

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_MUTE_TAKE_ACTION_PANEL", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TITLE_MENU_MUTE_ACTION");

            #endregion

            #region Buttons
            string ButtonColorChat = ChatSettingUser[userID].MuteChatTime > CurrentTime() ? "#C43E28FF" : config.InterfaceSetting.ThreeMainColor;
            string ButtonColorVoice = ChatSettingUser[userID].MuteVoiceTime > CurrentTime() ? "#C43E28FF" : config.InterfaceSetting.ThreeMainColor;
            string ButtonChat = ChatSettingUser[userID].MuteChatTime > CurrentTime() ?  "UI_MUTE_TAKE_ACTION_CHAT_UNMUTE" : "UI_MUTE_TAKE_ACTION_CHAT";
            string ButtonVoice = ChatSettingUser[userID].MuteVoiceTime > CurrentTime() ? "UI_MUTE_TAKE_ACTION_VOICE_UNMUTE" : "UI_MUTE_TAKE_ACTION_VOICE";
            string ButtonCommandChat = ChatSettingUser[userID].MuteChatTime > CurrentTime() ? $"iq_chat mute_action {userID} unmutechat" : $"iq_chat mute_action {userID} mute mutechat";
            string ButtonCommandVoice = ChatSettingUser[userID].MuteVoiceTime > CurrentTime() ? $"iq_chat mute_action {userID} unmutevoice" : $"iq_chat mute_action {userID} mute mutevoice";
            container.Add(new CuiButton
            {
                FadeOut = 0.2f,
                RectTransform = { AnchorMin = "0 0.3787669", AnchorMax = "1 0.6127237" },
                Button = { Command = ButtonCommandChat, Color = HexToRustFormat(ButtonColorChat), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                Text = { Text = lang.GetMessage(ButtonChat,this,player.UserIDString),  Align = TextAnchor.MiddleCenter }
            },  "TAKE_ACTION_MUTE");

            container.Add(new CuiButton
            {
                FadeOut = 0.2f,
                RectTransform = { AnchorMin = "0 0.07369863", AnchorMax = "1 0.3150686"},
                Button = { Command = ButtonCommandVoice, Color = HexToRustFormat(ButtonColorVoice), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                Text = { Text = lang.GetMessage(ButtonVoice, this, player.UserIDString), Align = TextAnchor.MiddleCenter }
            },  "TAKE_ACTION_MUTE");

            #endregion

            CuiHelper.AddUi(player, container);
        }

        void UI_MuteTakeActionShowListReason(BasePlayer player,ulong userID,string MuteFormat)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "TAKE_PANEL_REASON");

            #region Panels
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 -300", OffsetMax = "300 0" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.MainColorMute), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "TAKE_ACTION_MUTE","TAKE_PANEL_REASON");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.8859987", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "TAKE_PANEL_REASON", "TAKE_ACTION_MUTE_TITLE");

            #endregion

            #region Labels

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_MUTE_TAKE_REASON_TITLE", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TAKE_ACTION_MUTE_TITLE");

            #endregion

            #region Buttons
            for (int i = 0; i < config.ReasonListChat.Count; i++)
            {
                var Reason = config.ReasonListChat.ElementAt(i);
                container.Add(new CuiButton
                {
                    FadeOut = 0.2f,
                    RectTransform = { AnchorMin = $"0 {0.76898 - (i * 0.118)}", AnchorMax = $"1 {0.8694967 - (i * 0.118)}" },
                    Button = { Command = $"iq_chat mute_action {userID} mute_reason {MuteFormat} {i}", Color = HexToRustFormat("#90BC37FF"), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                    Text = { Text = Reason.Key, Align = TextAnchor.MiddleCenter }
                }, "TAKE_PANEL_REASON", "BUTTON" + i);
            }

            #endregion

            CuiHelper.AddUi(player, container);
        }

        #region NewElementSettings
        public void NewElementSettings(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, ELEMENT_SETTINGS);

            #region Panels

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "-300 -30", OffsetMax = "-1 350" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(config.InterfaceSetting.MainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            },  MAIN_PARENT, ELEMENT_SETTINGS);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.9959 0.07606" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            },  ELEMENT_SETTINGS, "MySettingPanel");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.9126315", AnchorMax = "0.9959 1" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ELEMENT_SETTINGS, "TitlePanel");

            #endregion

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region NewElementPrefixSettings
        public void NewElementPrefixSetting(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            var Prefix = ChatSettingUser[player.userID].ChatPrefix;
            var MyColorNick = ChatSettingUser[player.userID].NickColor;
            var PrefixList = config.PrefixList;
            #region Buttons

            int x = 0, y = 0;
            for(int i = 0; i < PrefixList.Count; i++)
            {
                var ElementPrefix = PrefixList.ElementAt(i);
                if (!permission.UserHasPermission(player.UserIDString, ElementPrefix.Key)) continue;
                container.Add(new CuiButton
                {
                    FadeOut = 0.2f,
                    RectTransform = { AnchorMin = $"{0.009514093 + (x * 0.594)} {0.77595 - (y * 0.07)}", AnchorMax = $"{0.3950973 + (x * 0.594)} {0.8368425 - (y * 0.07)}" },
                    Button = { Command = $"iq_chat prefix_selected {ElementPrefix.Value} MySettingPanel Prefix_Label {ElementPrefix.Value}<color={MyColorNick}>{player.displayName}</color>", Color = HexToRustFormat(config.InterfaceSetting.ThreeMainColor), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                    Text = { Text = ElementPrefix.Value, Color = HexToRustFormat("#FFBBB0FF"), Align = TextAnchor.MiddleCenter }
                },  ELEMENT_SETTINGS, $"BUTTON_{i}");
                x++;
                if (x == 2)
                {
                    y++;
                    x = 0;
                }
            }
            #endregion

            #region Labels

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_TEXT_PREFIX",this,player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TitlePanel");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = $"{Prefix}<color={MyColorNick}>{player.displayName}</color>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "MySettingPanel", "Prefix_Label");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8476694", AnchorMax = "1 0.9090226" },
                Text = { Text = lang.GetMessage("UI_TITLE_NEW_PREFIX_ELEMENT",this,player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            },  ELEMENT_SETTINGS);

            #endregion

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region NewElementNickColorSetting
        public void NewElementNickColorSetting(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            var MyColor = ChatSettingUser[player.userID].NickColor;
            var MyPrefix = ChatSettingUser[player.userID].ChatPrefix;
            var ColorList = config.NickColorList;
            #region Buttons

            int x = 0, y = 0;
            for (int i = 0; i < ColorList.Count; i++)
            {
                var ElementColor = ColorList.ElementAt(i);
                if (!permission.UserHasPermission(player.UserIDString, ElementColor.Key)) continue;
                container.Add(new CuiButton
                {
                    FadeOut = 0.2f,
                    RectTransform = { AnchorMin = $"{0.009514093 + (x * 0.594)} {0.77595 - (y * 0.07)}", AnchorMax = $"{0.3950973 + (x * 0.594)} {0.8368425 - (y * 0.07)}" },
                    Button = { Command = $"iq_chat nick_color_selected {ElementColor.Value} MySettingPanel Nick_Color_Label {MyPrefix}<color={ElementColor.Value}>{player.displayName}</color>", Color = HexToRustFormat(config.InterfaceSetting.ThreeMainColor), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                    Text = { Text = $"<color={ElementColor.Value}>{player.displayName}</color>", Align = TextAnchor.MiddleCenter }
                }, ELEMENT_SETTINGS, $"BUTTON_{i}");
                x++;
                if (x == 2)
                {
                    y++;
                    x = 0;
                }
            }
            #endregion

            #region Labels

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_TEXT_COLOR_NICK", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TitlePanel");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = $"{MyPrefix}<color={MyColor}>{player.displayName}</color>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "MySettingPanel", "Nick_Color_Label");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8476694", AnchorMax = "1 0.9090226" },
                Text = { Text = lang.GetMessage("UI_TITLE_NEW_NICK_COLOR_ELEMENT", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, ELEMENT_SETTINGS);

            #endregion

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region NewElementMessageColorSetting
        public void NewElementMessageColorSetting(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            var MyColor = ChatSettingUser[player.userID].MessageColor;
            var MyColorNick = ChatSettingUser[player.userID].NickColor;
            var MyPrefix = ChatSettingUser[player.userID].ChatPrefix;
            var ColorList = config.MessageColorList;
            #region Buttons

            int x = 0, y = 0;
            for (int i = 0; i < ColorList.Count; i++)
            {
                var ElementColor = ColorList.ElementAt(i);
                if (!permission.UserHasPermission(player.UserIDString, ElementColor.Key)) continue;
                container.Add(new CuiButton
                {
                    FadeOut = 0.2f,
                    RectTransform = { AnchorMin = $"{0.009514093 + (x * 0.594)} {0.77595 - (y * 0.07)}", AnchorMax = $"{0.3950973 + (x * 0.594)} {0.8368425 - (y * 0.07)}" },
                    Button = { Command = $"iq_chat message_color_selected {ElementColor.Value} MySettingPanel Message_Color_Label {MyPrefix}<color={MyColorNick}>{player.displayName}</color>:<color={ElementColor.Value}>Сообщение</color>", Color = HexToRustFormat(config.InterfaceSetting.ThreeMainColor), Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.1f },
                    Text = { Text = $"<color={ElementColor.Value}>Сообщение</color>", Align = TextAnchor.MiddleCenter }
                }, ELEMENT_SETTINGS, $"BUTTON_{i}");
                x++;
                if (x == 2)
                {
                    y++;
                    x = 0;
                }
            }

            #endregion

            #region Labels

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("UI_TEXT_COLOR_MSG", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TitlePanel");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = $"{MyPrefix}<color={MyColorNick}>{player.displayName}</color>:<color={MyColor}>Сообщение</color>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "MySettingPanel", "Message_Color_Label");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8476694", AnchorMax = "1 0.9090226" },
                Text = { Text = lang.GetMessage("UI_TITLE_NEW_MESSAGE_COLOR_ELEMENT", this, player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, ELEMENT_SETTINGS);

            #endregion


            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region UpdateLabel

        public void UpdateLabel(BasePlayer player,string Parent,string Name ,string TextLabel)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Name);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = $"{TextLabel}", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            },  Parent, Name);

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region UIAlert
        void UIAlert(BasePlayer player,string Message)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, MAIN_ALERT_UI);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-250 -280", OffsetMax = "231 -250" },
                Image = { Color = HexToRustFormat(config.InterfaceSetting.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            },  "Overlay", MAIN_ALERT_UI);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = $"{Message}", FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            },  MAIN_ALERT_UI);

            CuiHelper.AddUi(player, container);

            timer.Once(config.MessageSetting.TimeDeleteAlertUI, () =>
            {
                CuiHelper.DestroyUi(player, MAIN_ALERT_UI);
            });
        }
        #endregion

        #endregion

        #region Command

        [ChatCommand("chat")]
        void ChatCommandMenu(BasePlayer player)
        {
            UI_MainMenu(player);
        }

        [ChatCommand("mute")]
        void ChatMuteCommandMenu(BasePlayer player, string cmd, string[] arg)
        {
            if (arg.Length == 0 || arg == null)
            {
                if (permission.UserHasPermission(player.UserIDString, PermMuteMenu))
                    UI_MuteMenu(player);
            }
        }

        [ChatCommand("alert")]
        void ChatAlertPlayers(BasePlayer player,string cmd,string[] arg)
        {
            if (!player.IsAdmin) return;
            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("FUNC_MESSAGE_NO_ARG_BROADCAST", this, player.UserIDString));
                return;
            }
            string Message = "";
            foreach (var msg in arg)
                Message += " " + msg;
            
            ReplyBroadcast(Message);
        }

        [ConsoleCommand("alert")]
        void ChatAlertPlayersCMD(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length == 0 || arg.Args == null)
            {
                PrintWarning(lang.GetMessage("FUNC_MESSAGE_NO_ARG_BROADCAST", this));
                return;
            }
            string Message = "";
            foreach (var msg in arg.Args)
                Message += " " + msg;

            ReplyBroadcast(Message);
        }

        [ConsoleCommand("alertui")]
        void ChatAlertPlayersUICMD(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length == 0 || arg.Args == null)
            {
                PrintWarning(lang.GetMessage("FUNC_MESSAGE_NO_ARG_BROADCAST", this));
                return;
            }
            string Message = "";
            foreach (var msg in arg.Args)
                Message += " " + msg;

            BasePlayer.activePlayerList.ForEach(p => UIAlert(p, Message));
        }

        [ChatCommand("alertui")]
        void ChatAlertPlayersUI(BasePlayer player, string cmd, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (arg.Length == 0 || arg == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("FUNC_MESSAGE_NO_ARG_BROADCAST", this, player.UserIDString));
                return;
            }
            string Message = "";
            foreach (var msg in arg)
                Message += " " + msg;

            BasePlayer.activePlayerList.ForEach(p => UIAlert(p, Message));
        }

        [ChatCommand("rename")]
        void RenameMetods(BasePlayer player,string cmd, string[] arg)
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
                    
        public Dictionary<BasePlayer, BasePlayer> PMHistory = new Dictionary<BasePlayer, BasePlayer>();

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
            BasePlayer TargetUser = FindPlayer(NameUser);
            if (TargetUser == null || NameUser == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_PM_NOT_USER", this, player.UserIDString));
                return;
            }
            var argList = arg.ToList();
            argList.RemoveAt(0);
            string Message = string.Join(" ", argList.ToArray());
            if (Message.Length > 125) return;
            if(Message.Length <= 0 || Message == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_PM_NOT_NULL_MSG", this, player.UserIDString));
                return;
            }

            PMHistory[TargetUser] = player;
            PMHistory[player] = TargetUser;
            var DisplayNick = AdminSetting.RenameList.ContainsKey(player.userID) ? AdminSetting.RenameList[player.userID] : player.displayName;

            ReplySystem(Chat.ChatChannel.Global, TargetUser, String.Format(lang.GetMessage("COMMAND_PM_SEND_MSG",this,player.UserIDString), DisplayNick, Message));
            ReplySystem(Chat.ChatChannel.Global, player, String.Format(lang.GetMessage("COMMAND_PM_SUCCESS", this,player.UserIDString), Message));
            Effect.server.Run(config.MessageSetting.SoundPM, TargetUser.GetNetworkPosition());
            Log($"ЛИЧНЫЕ СООБЩЕНИЯ : {player.displayName}({DisplayNick}) отправил сообщение игроку - {TargetUser.displayName}\nСООБЩЕНИЕ : {Message}");
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
            var argList = arg.ToList();
            string Message = string.Join(" ", argList.ToArray());
            if (Message.Length > 125) return;
            if (Message.Length <= 0 || Message == null)
            {
                ReplySystem(Chat.ChatChannel.Global, player, lang.GetMessage("COMMAND_PM_NOT_NULL_MSG", this, player.UserIDString));
                return;
            }
            PMHistory[RetargetUser] = player;
            var DisplayNick = AdminSetting.RenameList.ContainsKey(player.userID) ? AdminSetting.RenameList[player.userID] : player.displayName;

            ReplySystem(Chat.ChatChannel.Global, RetargetUser, String.Format(lang.GetMessage("COMMAND_PM_SEND_MSG", this, player.UserIDString), DisplayNick, Message));
            ReplySystem(Chat.ChatChannel.Global, player, String.Format(lang.GetMessage("COMMAND_PM_SUCCESS", this, player.UserIDString), Message));
            Effect.server.Run(config.MessageSetting.SoundPM, RetargetUser.GetNetworkPosition());
            Log($"ЛИЧНЫЕ СООБЩЕНИЯ : {player.displayName} отправил сообщение игроку - {RetargetUser.displayName}\nСООБЩЕНИЕ : {Message}");
        }
        
        [ConsoleCommand("set")]
        private void ConsolesCommandPrefixSet(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 1 || arg.Args[0].Length < 0)
            {
                Puts("Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                return;
            }
            ulong Steam64ID = 0;
            BasePlayer player = null;
            if (ulong.TryParse(arg.Args[0], out Steam64ID))
                player = BasePlayer.FindByID(Steam64ID);
            if (player == null)
            {
                Puts("Неверно указан SteamID игрока или ошибка в синтаксисе\nИспользуйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                return;
            }
            var DataPlayer = ChatSettingUser[player.userID];

            switch (arg.Args[1].ToLower())
            {
                case "prefix":
                    {
                        string KeyPrefix = arg.Args[2];
                        if (config.PrefixList.ContainsKey(KeyPrefix))
                        {
                            var Prefix = config.PrefixList[KeyPrefix];
                            DataPlayer.ChatPrefix = Prefix;
                            Puts($"Префикс успешно установлен на - {Prefix}");
                        }
                        else Puts("Неверно указан Permissions от префикса");
                        break;
                    }
                case "chat":
                    {
                        string KeyChatColor = arg.Args[2];
                        if (config.PrefixList.ContainsKey(KeyChatColor))
                        {
                            var Message = config.MessageColorList[KeyChatColor];
                            DataPlayer.MessageColor = Message;
                            Puts($"Цвет сообщения успешно установлен на - {Message}");
                        }
                        else Puts("Неверно указан Permissions от префикса");

                        break;
                    }
                case "nick":
                    {
                        string KeyNickColor = arg.Args[2];
                        if (config.PrefixList.ContainsKey(KeyNickColor))
                        {
                            var Nick = config.NickColorList[KeyNickColor];
                            DataPlayer.NickColor = Nick;
                            Puts($"Цвет ника успешно установлен на - {Nick}");
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
                        Puts("Используйте правильно ситаксис : set [Steam64ID] [prefix/chat/nick/custom] [Argument]");
                        break;
                    }
            }

        }

        [ConsoleCommand("iq_chat")] 
        private void ConsoleCommandIQChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            var DataPlayer = ChatSettingUser[player.userID];
            switch (arg.Args[0])
            {
                case "newelementsettingsprefix":
                    {
                        NewElementSettings(player);
                        NewElementPrefixSetting(player);
                        break;
                    }
                case "newelementsettingscolornick":
                    {
                        NewElementSettings(player);
                        NewElementNickColorSetting(player);
                        break;
                    }
                case "newelementsettingcolormessage":
                    {
                        NewElementSettings(player);
                        NewElementMessageColorSetting(player);
                        break;
                    }
                case "prefix_selected":
                    {
                        var SelectedPrefix = arg.Args[1];
                        var ParentLabel = arg.Args[2];
                        var NameLabel = arg.Args[3];
                        var TextLabel = arg.Args[4];
                        DataPlayer.ChatPrefix = SelectedPrefix;
                        UpdateLabel(player, ParentLabel, NameLabel, TextLabel);
                        break;
                    }
                case "nick_color_selected":
                    {
                        var SelectedNickColor = arg.Args[1];
                        var ParentLabel = arg.Args[2];
                        var NameLabel = arg.Args[3];
                        var TextLabel = arg.Args[4];
                        DataPlayer.NickColor = SelectedNickColor;
                        UpdateLabel(player, ParentLabel, NameLabel, TextLabel);
                        break;
                    }
                case "message_color_selected":
                    {
                        var SelectedMessageColor = arg.Args[1];
                        var ParentLabel = arg.Args[2];
                        var NameLabel = arg.Args[3];
                        var TextLabel = arg.Args[4];
                        DataPlayer.MessageColor = SelectedMessageColor;
                        UpdateLabel(player, ParentLabel, NameLabel, TextLabel);
                        break;
                    }
                case "mute_take_action": 
                    {
                        BasePlayer target = BasePlayer.FindByID(ulong.Parse(arg.Args[1]));
                        UI_MuteTakeAction(player, target.userID);
                        break;
                    }
                case "mute_action": 
                    {
                        BasePlayer target = BasePlayer.FindByID(ulong.Parse(arg.Args[1]));
                        string Action = arg.Args[2];
                        switch (Action)
                        {
                            case "mute":
                                {
                                    string MuteFormat = arg.Args[3];
                                    UI_MuteTakeActionShowListReason(player, target.userID, MuteFormat);
                                    break;
                                }
                            case "mute_reason": 
                                {
                                    CuiHelper.DestroyUi(player, MUTE_MENU_PARENT);
                                    string MuteFormat = arg.Args[3];
                                    int Index = Convert.ToInt32(arg.Args[4]);
                                    MutePlayer(target, player, MuteFormat, Index);
                                    break;
                                }
                            case "unmutechat":
                                {
                                    CuiHelper.DestroyUi(player, MUTE_MENU_PARENT);
                                    ChatSettingUser[target.userID].MuteChatTime = 0;
                                    ReplyBroadcast(string.Format(lang.GetMessage("FUNC_MESSAGE_UNMUTE_CHAT",this),player.displayName,target.displayName));
                                    break;
                                }
                            case "unmutevoice":
                                {
                                    CuiHelper.DestroyUi(player, MUTE_MENU_PARENT);
                                    ChatSettingUser[target.userID].MuteVoiceTime = 0;
                                    ReplyBroadcast(string.Format(lang.GetMessage("FUNC_MESSAGE_UNMUTE_VOICE", this), player.displayName,target.displayName));
                                    break;
                                }
                        }
                        break;
                    }
                case "admin_voice":
                    {
                        var Command = arg.Args[1];
                        switch(Command)
                        {
                            case "mutevoice":
                                {
                                    AdminSetting.MuteVoiceAll = true;
                                    BasePlayer.activePlayerList.ForEach(p => ChatSettingUser[p.userID].MuteVoiceTime = CurrentTime() + 86400);
                                    ReplyBroadcast(lang.GetMessage("FUNC_MESSAGE_MUTE_ALL_VOICE", this, player.UserIDString));
                                    break;
                                }
                            case "unmutevoice":
                                {
                                    AdminSetting.MuteVoiceAll = false;
                                    BasePlayer.activePlayerList.ForEach(p => ChatSettingUser[p.userID].MuteVoiceTime = 0);
                                    ReplyBroadcast(lang.GetMessage("FUNC_MESSAGE_UNMUTE_ALL_VOICE", this, player.UserIDString));
                                    break;
                                }
                        }
                        BasePlayer.activePlayerList.ForEach(p => { rust.RunServerCommand(Command, p.userID); });
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
                                    BasePlayer.activePlayerList.ForEach(p => MuteAllChatPlayer(p));
                                    ReplyBroadcast(lang.GetMessage("FUNC_MESSAGE_MUTE_ALL_CHAT", this, player.UserIDString));
                                    break;
                                }
                            case "unmutechat":
                                {
                                    AdminSetting.MuteChatAll = false;
                                    BasePlayer.activePlayerList.ForEach(p => ChatSettingUser[p.userID].MuteChatTime = 0);
                                    ReplyBroadcast(lang.GetMessage("FUNC_MESSAGE_UNMUTE_ALL_CHAT", this, player.UserIDString));
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            PrintWarning("Языковой файл загружается...");
            Dictionary<string, string> Lang = new Dictionary<string, string>
            {
                ["TITLE_ONE"] = "<size=20><b>Hастройка чата</b></size>",
                ["TITLE_TWO"] = "<size=12><b>Bыберите действие</b></size>",
                ["UI_CLOSE_BTN"] = "<size=20><b>Закрыть</b></size>",

                ["UI_TEXT_PREFIX"] = "<size=13><b>Hастройка префикса</b></size>",
                ["UI_TEXT_COLOR_NICK"] = "<size=13><b>Hастройка цвета ника</b></size>",
                ["UI_TEXT_COLOR_MSG"] = "<size=13><b>Hастройка цвета чата</b></size>",
                ["UI_TEXT_GO_SETTINGS"] = "<size=14><b>Перейти к настройкам</b></size>",

                ["UI_TEXT_ADMIN_PANEL"] = "<size=18><b>Панель Aдминистратора</b></size>",
                ["UI_TEXT_ADMIN_PANEL_MUTE_CHAT_ALL"] = "<size=14><b>3аблокировать всем чат</b></size>",
                ["UI_TEXT_ADMIN_PANEL_UNMUTE_CHAT_ALL"] = "<size=14><b>Разблокировать всем чат</b></size>",
                ["UI_TEXT_ADMIN_PANEL_MUTE_VOICE_ALL"] = "<size=14><b>3аблокировать всем голос</b></size>",
                ["UI_TEXT_ADMIN_PANEL_UNMUTE_VOICE_ALL"] = "<size=14><b>Разблокировать всем голос</b></size>",

                ["UI_TITLE_NEW_PREFIX_ELEMENT"] = "<size=16><b>Ваши доступные префексы</b></size>",
                ["UI_TITLE_NEW_NICK_COLOR_ELEMENT"] = "<size=16><b>Ваш доступные цвета для ника</b></size>",
                ["UI_TITLE_NEW_MESSAGE_COLOR_ELEMENT"] = "<size=16><b>Ваш доступные цвета для сообщений</b></size>",

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

                ["UI_MUTE_PANEL_TITLE"] = "<size=24><b>Панель управления блокировками чата</b></size>",
                ["UI_MUTE_PANEL_TITLE_ACTION"] = "<size=16><b>Выберите игрока или введите ник в поиске</b></size>",
                ["UI_MUTE_PANEL_TITLE_PLIST"] = "<size=18><b>Список игроков</b></size>",
                ["UI_MUTE_PANEL_TITLE_HELPS_GREEN"] = "<size=13><b>- У игрока разблокирован чат или голос</b></size>",
                ["UI_MUTE_PANEL_TITLE_HELPS_RED"] = "<size=13><b>- У игрока заблокирован чат или голос</b></size>",

                ["UI_MUTE_TAKE_ACTION_PANEL"] = "<size=20><b>Bыберите действие</b></size>",
                ["UI_MUTE_TAKE_ACTION_CHAT"] = "<size=15><b>3аблокировать чат</b></size>",
                ["UI_MUTE_TAKE_ACTION_CHAT_UNMUTE"] = "<size=15><b>Разблокировать чат</b></size>",
                ["UI_MUTE_TAKE_ACTION_VOICE"] = "<size=15><b>3аблокировать голос</b></size>",
                ["UI_MUTE_TAKE_ACTION_VOICE_UNMUTE"] = "<size=15><b>Разблокировать голос</b></size>",

                ["UI_MUTE_TAKE_REASON_TITLE"] = "<size=17><b>Bыберите причину</b></size>",

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

                ["WELCOME_PLAYER"] = "{0} зашел на сервер",
                ["LEAVE_PLAYER"] = "{0} вышел с сервера",
            };

            lang.RegisterMessages(Lang, this);
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion

        #region Helpers

        public void Log(string LoggedMessage) => LogToFile("IQChatLogs", LoggedMessage, this);

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

                        result += $"{Format(time.Days, "дней", "дня", "день")}";
                        i++;
                    }

                    if (time.Hours != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Hours, "часов", "часа", "час")}";
                        i++;
                    }

                    if (time.Minutes != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Minutes, "минут", "минуты", "минута")}";
                        i++;
                    }

                    if (time.Seconds != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")}";
                        i++;
                    }

                    break;
                case "en":
                    result = string.Format("{0}{1}{2}{3}",
                        time.Duration().Days > 0 ? $"{time.Days:0} day{(time.Days == 1 ? String.Empty : "s")}, " : string.Empty,
                        time.Duration().Hours > 0 ? $"{time.Hours:0} hour{(time.Hours == 1 ? String.Empty : "s")}, " : string.Empty,
                        time.Duration().Minutes > 0 ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? String.Empty : "s")}, " : string.Empty,
                        time.Duration().Seconds > 0 ? $"{time.Seconds:0} second{(time.Seconds == 1 ? String.Empty : "s")}" : string.Empty);

                    if (result.EndsWith(", ")) result = result.Substring(0, result.Length - 2);

                    if (string.IsNullOrEmpty(result)) result = "0 seconds";
                    break;
            }
            return result;
        }

        private BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var check in BasePlayer.activePlayerList)
            {
                if (check.displayName.ToLower().Contains(nameOrId.ToLower()) || check.userID.ToString() == nameOrId)
                    return check;
            }

            return null;
        }
        public static long TimeToSeconds(string time)
        {
            time = time.Replace(" ", "").Replace("d", "d ").Replace("h", "h ").Replace("m", "m ").Replace("s", "s ").TrimEnd(' ');
            var arr = time.Split(' ');
            long seconds = 0;
            foreach (var s in arr)
            {
                var n = s.Substring(s.Length - 1, 1);
                var t = s.Remove(s.Length - 1, 1);
                int d = int.Parse(t);
                switch (n)
                {
                    case "s":
                        seconds += d;
                        break;
                    case "m":
                        seconds += d * 60;
                        break;
                    case "h":
                        seconds += d * 3600;
                        break;
                    case "d":
                        seconds += d * 86400;
                        break;
                }
            }
            return seconds;
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

        public Dictionary<ulong, double> Flooders = new Dictionary<ulong, double>();
        void ReplyChat(Chat.ChatChannel channel, BasePlayer player, string format)
        {
            var MessageSetting = config.MessageSetting;
            if (MessageSetting.AntiSpamActivate)
            {
                if (Flooders.ContainsKey(player.userID))
                {
                    if (Flooders[player.userID] > CurrentTime())
                    {
                        ReplySystem(Chat.ChatChannel.Global, player, string.Format(lang.GetMessage("FLOODERS_MESSAGE", this, player.UserIDString), Convert.ToInt32(Flooders[player.userID] - CurrentTime())));
                        Flooders[player.userID] = MessageSetting.FloodTime + CurrentTime();
                        return;
                    }
                }
                else Flooders.Add(player.userID, CurrentTime() + MessageSetting.FloodTime);

                Flooders[player.userID] = MessageSetting.FloodTime + CurrentTime();
            }

            if (channel == Chat.ChatChannel.Global)
            {
                BasePlayer.activePlayerList.ForEach(p => p.SendConsoleCommand("chat.add", channel, player.userID, format));
                PrintToConsole(format);
            }
            if (channel == Chat.ChatChannel.Team)
            {
                RelationshipManager.PlayerTeam Team = RelationshipManager.Instance.FindTeam(player.currentTeam);
                if (Team == null) return;
                foreach (var FindPlayers in Team.members)
                {
                    BasePlayer TeamPlayer = BasePlayer.FindByID(FindPlayers);
                    if (TeamPlayer == null) return;
                        TeamPlayer.SendConsoleCommand("chat.add", channel, player.userID, format);
                }
            }
        }

        void ReplySystem(Chat.ChatChannel channel, BasePlayer player, string Message,string CustomPrefix = "", string CustomAvatar = "")
        {
            string Prefix = string.IsNullOrEmpty(CustomPrefix) ? config.MessageSetting.BroadcastTitle : CustomPrefix;
            ulong Avatar = string.IsNullOrEmpty(CustomAvatar) ? config.MessageSetting.Steam64IDAvatar : ulong.Parse(CustomAvatar);

            string FormatMessage = $"{Prefix} <color={config.MessageSetting.BroadcastColor}>{Message}</color>";
            if (channel == Chat.ChatChannel.Global)
                player.SendConsoleCommand("chat.add", channel, Avatar, FormatMessage);         
        }

        void ReplyBroadcast(string Message, string CustomPrefix = "", string CustomAvatar = "")
        {
            BasePlayer.activePlayerList.ForEach(p => { ReplySystem(Chat.ChatChannel.Global, p, Message, CustomPrefix, CustomAvatar);  });
        }

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        #endregion

        #region API

        void API_ALERT(string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global, string CustomPrefix = "", string CustomAvatar = "") => BasePlayer.activePlayerList.ForEach(p => { ReplySystem(channel, p, Message, CustomPrefix, CustomAvatar); });
        void API_ALERT_PLAYER(BasePlayer player,string Message, string CustomPrefix = "", string CustomAvatar = "") => ReplySystem(Chat.ChatChannel.Global, player, Message, CustomPrefix, CustomAvatar);
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
        #endregion
    }
}
