using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MenuRevolve", "TopPlugin.ru", "0.0.3")]
    [Description("Приятное меню для вашего сервера")]
    class MenuRevolve : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin IQChat;    
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Оглавление меню")]
            public string DisplayNmeMenu;
            [JsonProperty("Описание меню")]
            public string DescriptionMenu;
            [JsonProperty("Страницы с текстом")]
            public List<Settings> MenuConfiguration = new List<Settings>();
            [JsonProperty("Настройки для IQChat")]
            public ChatSettings ChatSetting = new ChatSettings();
            internal class Settings
            {
                [JsonProperty("Текст кнопки")]
                public string DisplayNameButton;
                [JsonProperty("Оглавление на информативном блоке")]
                public string TitleInfoBlock;
                [JsonProperty("Текст на странице")]
                public List<string> Text = new List<string>();
            }

            internal class ChatSettings
            {
                [JsonProperty("Префикс(IQChat)")]
                public string CustomPrefix;
                [JsonProperty("Steam64ID для аватарки(IQChat)")]
                public string CustomAvatar;
                [JsonProperty("Цвет-HEX для префикса(IQChat)")]
                public string HexColorPrefix;
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    DisplayNmeMenu = "<size=40><b>Добро пожаловать на сервер <color=#5cc4ff>ВАШ ТОПОВЫЙ</color><color=#986ceb>СЕРВЕР</color></b></size>",
                    DescriptionMenu = "<size=25>Это меню поможет вам ознакомиться с сервером</size>",
                    ChatSetting = new ChatSettings
                    {
                        CustomPrefix = "[ИНФО СЕРВЕРА]",
                        CustomAvatar = "0",
                        HexColorPrefix = "#FFAA99",
                    },
                    MenuConfiguration = new List<Settings>
                    {
                        new Settings
                        {
                            DisplayNameButton = "<size=25>Правила</size>",
                            TitleInfoBlock = "<size=30><b>Ознакомтесь с правилами сервера</b></size>",
                            Text = new List<string>
                            {
                                "<size=20><color=#DB4067><b>X</b></color></size><size=15>Вы можете форматировать текст по разному</size>",
                                "<size=21><color=#DB4067><b>X</b></color></size><size=15>Добавляя <b>жирности</b></size>",
                                "<size=22><color=#DB4067><b>X</b></color></size><size=15>Меняя <color=#34B3FF>цвета</color></size>",
                                "<size=23><color=#DB4067><b>X</b></color></size><size=15>Плагин не ограничивает вас</size>",
                            }
                        },
                        new Settings
                        {
                            DisplayNameButton = "<size=25>Команды</size>",
                            TitleInfoBlock = "Ознакомтесь с командами сервера",
                            Text = new List<string>
                            {
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                            }
                        },
                        new Settings
                        {
                            DisplayNameButton = "<size=25>Тестовый</size>",
                            TitleInfoBlock = "ТЕКСТ",
                            Text = new List<string>
                            {
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                                "<size=17> Тестовый текст </size>",
                            }
                        },
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
                PrintWarning($"Ошибка #58 чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        public List<ulong> NoOpenMenuListUsers = new List<ulong>();
        #endregion

        #region Hooks

        void Unload()
        {
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                var p = BasePlayer.activePlayerList[i];
                CuiHelper.DestroyUi(p, MAIN_PARENT);
            }
        }
        private void OnServerSave() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("MenuRevolve/NoOpenMenuListUsers", NoOpenMenuListUsers);
        private void OnServerInitialized()
        {
            NoOpenMenuListUsers = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<List<ulong>>("MenuRevolve/NoOpenMenuListUsers");
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("MenuRevolve/NoOpenMenuListUsers", NoOpenMenuListUsers);
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
            if (!NoOpenMenuListUsers.Contains(player.userID))
                USER_INTERFACE(player);
            else SendChat(lang.GetMessage("MENU_ALERT_AUTO_START",this, player.UserIDString), player);
        }

        #endregion

        #region Commands
        [ChatCommand("info")]
        void ChatCommandMenu(BasePlayer player)
        {
            USER_INTERFACE(player);
        }

        [ConsoleCommand("menu")]
        void MenuConsoleCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (arg.Args.Length <= 0 || arg.Args[0] == null)
            {
                USER_INTERFACE(player);
                return;
            }

            switch (arg.Args[0])
            {
                case "select_category":
                    {
                        int Category = Convert.ToInt32(arg.Args[1]);
                        SelectCategory(player, Category,0);
                        break;
                    }
                case "page_next":
                    {
                        int Category = Convert.ToInt32(arg.Args[1]);
                        int IndexList = Convert.ToInt32(arg.Args[2]);
                        SelectCategory(player, Category, IndexList + 1);
                        break;
                    }
                case "page_back":
                    {
                        int Category = Convert.ToInt32(arg.Args[1]);
                        int IndexList = Convert.ToInt32(arg.Args[2]);
                        SelectCategory(player, Category, IndexList - 1);
                        break;
                    }
                case "alert_func":
                    {
                        if (!NoOpenMenuListUsers.Contains(player.userID))
                        {
                            NoOpenMenuListUsers.Add(player.userID);
                            SendChat(lang.GetMessage("MENU_ALERT_OFF", this, player.UserIDString), player);
                        }
                        else
                        {
                            NoOpenMenuListUsers.Remove(player.userID);
                            SendChat(lang.GetMessage("MENU_ALERT_ON", this, player.UserIDString), player);
                        }
                        CuiHelper.DestroyUi(player, MAIN_PARENT);
                        break;
                    }

            }
        }
        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MENU_CLOSE"] = "Close",
                ["MENU_NO_ALERT"] = "Don't show again",
                ["MENU_YES_ALERT"] = "Show again",
                ["MENU_ALERT_OFF"] = "You have successfully <color=#db2d21>disabled</color> the menu when entering the server!\nTo open the menu, use the command <color=#5cc4ff>/menu</color>",
                ["MENU_ALERT_ON"] = "You have successfully <color=#72db21>enabled</color> the menu to open when you enter the server! \nTo open the menu, use the command <color=#5cc4ff>/menu</color>",
                ["MENU_ALERT_AUTO_START"] = "Welcome to the server <color=#5cc4ff>PONY</color><color=#986ceb>LAND</color></b\nYou have <color=#db2d21> disabled </color> the menu on login!\nTo open it, write <color=#5cc4ff>/menu</color>",
                ["MENU_UI_PAGE_ACTION_NEXT"] = "NEXT",
                ["MENU_UI_PAGE_ACTION_BACK"] = "BACK",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MENU_CLOSE"] = "Закрыть",
                ["MENU_NO_ALERT"] = "Больше не показывать",
                ["MENU_YES_ALERT"] = "Снова показывать",
                ["MENU_ALERT_OFF"] = "Вы успешно <color=#db2d21>отключили</color> открытие меню при входе на сервер!\nЧтобы открыть меню,используйте команду <color=#5cc4ff>/menu</color>",
                ["MENU_ALERT_ON"] = "Вы успешно <color=#72db21>включили</color> открытие меню при входе на сервер!\nЧтобы открыть меню,используйте команду <color=#5cc4ff>/menu</color>",
                ["MENU_ALERT_AUTO_START"] = "\nУ вас <color=#db2d21>отключен</color> показ меню при входе!\nЧтобы открыть его,пропишите <color=#5cc4ff>/menu</color>",
                ["MENU_UI_PAGE_ACTION_NEXT"] = "ДАЛЕЕ",
                ["MENU_UI_PAGE_ACTION_BACK"] = "НАЗАД",
            }, this, "ru");
            PrintWarning("Lang loaded");
        }
        #endregion

        #region UI

        #region Parents
        public static string MAIN_PARENT = "PARENT_MAIN";
        #endregion

        public void USER_INTERFACE(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, MAIN_PARENT);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur.mat", Sprite = "assets/content/ui/ui.background.transparent.radial.psd" }
            }, "Overlay", MAIN_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.01041669 0.02407", AnchorMax = "0.184375 0.8203704" },
                Image = { Color = "0 0 0 0.5" }
            },  MAIN_PARENT, "BLOCK_BUTTONS");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1 0.8333323", AnchorMax = "0.9 0.8342583" },
                Image = { Color = HexToRustFormat("#FFFFFF60") }
            }, MAIN_PARENT);

            #region CategoryMenu

            for (int i = 0; i < config.MenuConfiguration.Count; i++)
            {
                var ButtonText = config.MenuConfiguration[i].DisplayNameButton;

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 {0.9302325 - (i * 0.08)}", AnchorMax = $"1 {1 - (i * 0.08)}" },
                    Text = { Text = ButtonText, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                }, "BLOCK_BUTTONS",$"BUTTON_MAIN_{i}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Command = $"menu select_category {i}",Color = "0 0 0 0.1" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                },  $"BUTTON_MAIN_{i}", $"CATEGORY_NAME_{i}");
            }

            #endregion

            #region Titles

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8944442", AnchorMax = "1 0.9648145" },
                Text = { Text = config.DisplayNmeMenu, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, MAIN_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8351852", AnchorMax = "1 0.9009275" },
                Text = { Text = config.DescriptionMenu, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, MAIN_PARENT);

            #endregion

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8807292 0.9453704", AnchorMax = "0.9963542 1" },
                Button = { Close = MAIN_PARENT, Color = "0 0 0 0.1" },
                Text = { Text = lang.GetMessage("MENU_CLOSE",this, player.UserIDString), FontSize = 35, Align = TextAnchor.UpperRight }
            }, MAIN_PARENT);

            string AlertTextLang = !NoOpenMenuListUsers.Contains(player.userID) ? "MENU_NO_ALERT" : "MENU_YES_ALERT";
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8921875 0.9157408", AnchorMax = "0.9958 0.9425939" },
                Button = { Command = "menu alert_func", Color = "0 0 0 0.1" },
                Text = { Text = lang.GetMessage(AlertTextLang,this,player.UserIDString), FontSize = 12, Align = TextAnchor.UpperRight }
            }, MAIN_PARENT);

            CuiHelper.AddUi(player, container);
            SelectCategory(player, 0,0);
        }

        #region HelpMetodsUI

        void SelectCategory(BasePlayer player,int CategoryIndex,int Page)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "BLOCK_INFO");
            CuiHelper.DestroyUi(player, "BACK_BUTTON");
            CuiHelper.DestroyUi(player, "NEXT_BUTTON");
            var CountText = config.MenuConfiguration[CategoryIndex].Text.Count;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1875 0.0240741", AnchorMax = "0.9875 0.82037" },
                Image = { Color = "0 0 0 0.5" }
            }, MAIN_PARENT, "BLOCK_INFO");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9209301", AnchorMax = "1 1" },
                Text = { Text = config.MenuConfiguration[CategoryIndex].TitleInfoBlock, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter }
            }, "BLOCK_INFO");

            int i = 0;
            foreach(var cfg in config.MenuConfiguration[CategoryIndex].Text.Skip(Page * 16))
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 {0.8604651 - (i * 0.04)}", AnchorMax = $"1 {0.9081395 - (i * 0.04)}" },
                    Text = { Text = cfg, Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter }
                }, "BLOCK_INFO");
                i++;
                if (i == 20) break;
            }

            if (CountText > (Page + 1) * 20)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.8802256 0", AnchorMax = "1 0.08" },
                    Button = { Command = $"menu page_next {CategoryIndex} {Page}", Color = "0 0 0 0.1" },
                    Text = { Text = lang.GetMessage("MENU_UI_PAGE_ACTION_NEXT",this, player.UserIDString),FontSize = 30, Align = TextAnchor.UpperCenter }
                }, $"BLOCK_INFO", "NEXT_BUTTON");
            }
            if(Page != 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.1204373 0.05930" },
                    Button = { Command = $"menu page_back {CategoryIndex} {Page}", Color = "0 0 0 0.1" },
                    Text = { Text = lang.GetMessage("MENU_UI_PAGE_ACTION_BACK", this, player.UserIDString), FontSize = 30, Align = TextAnchor.UpperCenter }
                }, $"BLOCK_INFO", "BACK_BUTTON");
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #endregion

        #region Help
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

            UnityEngine.Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, $"<color={config.ChatSetting.HexColorPrefix}>{config.ChatSetting.CustomPrefix}</color>", config.ChatSetting.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        #endregion
    }
}