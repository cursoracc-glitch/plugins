using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQMenu", "Mercury", "0.0.1")]
    [Description("Ясно клоун")]
    class IQMenu : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin ImageLibrary;
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);

        #endregion

        #region Vars
        public List<ulong> PlayerOpenMenu = new List<ulong>();
        #endregion

        #region Configuration 
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка меню")] public List<MenuClass> MenuSettings = new List<MenuClass>();
            [JsonProperty("Иконка для главного меню")] public string UrlMenu;
            [JsonProperty("Название сервера для главного меню")] public string ServerName;
            [JsonProperty("Название кнопки главного меню")] public string ButtonName;
            [JsonProperty("Настройка броадкаста")] public List<string> BroadCastList = new List<string>();

            internal class MenuClass
            {
                [JsonProperty("Иконка для кнопки")] public string URLIco;
                [JsonProperty("Название для кнопки")] public string DisplayName;
                [JsonProperty("Команда для кнопки")] public string Command;
            }

            public static Configuration GetNewConfiguration() 
            {
                return new Configuration
                {
                    ServerName = "<b><size=26>СУПЕР <color=#85C84F>СЕРВЕР</color> | MAX 3</size></b>",
                    ButtonName = "<b><size=18>МЕНЮ</size></b>",
                    UrlMenu = "https://i.imgur.com/chc6Jfs.png",
                    MenuSettings = new List<MenuClass>
                    {
                        new MenuClass
                        {
                            DisplayName = "Магазин",
                            URLIco = "https://i.imgur.com/Us7eiTz.png",
                            Command = "UI_GameStoresRUST"
                        },
                        new MenuClass
                        {
                            DisplayName = "Ваша кастом кнопка",
                            URLIco = "https://i.imgur.com/QaueVCY.png",
                            Command = "chat.say /craft",
                        },
                        new MenuClass
                        {
                            DisplayName = "Еще одна кнопка",
                            URLIco = "https://i.imgur.com/zfM6hpw.png",
                            Command = "chat.say /report",
                        },
                    },
                    BroadCastList = new List<string>
                    {
                        "<b>Тестовое оповещение - <color=#3B85F5FF>Цвет</color></b>",
                        "<b>Тестовое оповещение - <color=#3B85F5FF>Цвет</color></b>",
                        "<b>Тестовое оповещение - <color=#3B85F5FF>Цвет</color></b>",
                        "<b>Тестовое оповещение - <color=#3B85F5FF>Цвет</color></b>",
                        "<b>Тестовое оповещение - <color=#3B85F5FF>Цвет</color></b>",
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
                PrintWarning($"Ошибка чтения #57 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Interface

        public static string MENU_PARENT = "MENU_PARENT_LAYER";
        public static string DROP_MENU_PANEL = "DROP_MENU_LAYER";
        public static string BROADCAST_PARENT = "BROADCAST_PARENT_LAYER";

        void InterfaceMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, MENU_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -60", OffsetMax = "300 0" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", MENU_PARENT);

            container.Add(new CuiElement
            {
                Parent = MENU_PARENT,
                Components =
                        {
                        new CuiRawImageComponent { Png = GetImage(config.ButtonName),  Color = HexToRustFormat("#FFFFFF8B") },
                        new CuiRectTransformComponent{  AnchorMin = $"0.2 0.06484989", AnchorMax = $"0.2888888 0.4727611" },
                        }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.02 0.4787878", AnchorMax = "1 1" },
                Text = { Text = config.ServerName, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = HexToRustFormat("#FFFFFF8B") }
            }, MENU_PARENT);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.2022 0.5515152" },
                Button = { Command = $"pmenu open", Color = "0 0 0 0" },
                Text = { Text = config.ButtonName, Color = HexToRustFormat("#FFFFFF8B"), Align = TextAnchor.MiddleCenter }
            },  MENU_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.242223 0", AnchorMax = "0.5311611 0.5515152" },
                Text = { Text = $"<b><size=14>Онлайн</size></b>\n<b><size=12>{BasePlayer.activePlayerList.Count}/{ConVar.Server.maxplayers}</size></b>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter, Color = HexToRustFormat("#FFFFFF8B") }
            }, MENU_PARENT, "ONLINE_TEXT");

            CuiHelper.AddUi(player, container);
        }

        void UpdateOnlineLabel()
        {
            timer.Every(30f, () =>
             {
                 for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
                 {
                     var player = BasePlayer.activePlayerList[i];
                     CuiHelper.DestroyUi(player, "ONLINE_TEXT");
                     CuiElementContainer container = new CuiElementContainer();
                     container.Add(new CuiLabel
                     {
                         RectTransform = { AnchorMin = "0.242223 0", AnchorMax = "0.5311111 0.5515152" },
                         Text = { Text = $"<b><size=14>Онлайн</size></b>\n<b><size=12>{BasePlayer.activePlayerList.Count}/{ConVar.Server.maxplayers}</size></b>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.UpperCenter, Color = HexToRustFormat("#FFFFFF8B") }
                     }, MENU_PARENT, "ONLINE_TEXT");
                     CuiHelper.AddUi(player, container);
                 }
             });
        }

        void BroadCast()
        {
            timer.Every(120f, () =>
            {
                for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    CuiHelper.DestroyUi(player, BROADCAST_PARENT);
                    CuiElementContainer container = new CuiElementContainer();
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = "300 20" },
                        Text = { Text = $"{config.BroadCastList[UnityEngine.Random.Range(0, config.BroadCastList.Count)]}", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFF8B") }
                    }, "Overlay", BROADCAST_PARENT);
                    CuiHelper.AddUi(player, container);
                }
            });
        }

        void DropListMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, DROP_MENU_PANEL);

            container.Add(new CuiPanel
            {
                FadeOut = 0.1f,
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"0 -600", OffsetMax = "300 -60" },
                Image = { FadeIn = 0.1f, Color = "0 0 0 0" }
            }, MENU_PARENT, DROP_MENU_PANEL);


            for (int i = 0; i < config.MenuSettings.Count; i++)
            {
                var cfg = config.MenuSettings[i];

                container.Add(new CuiElement
                {
                    Parent = DROP_MENU_PANEL,
                    Components =
                        {
                        new CuiRawImageComponent { Png = GetImage(cfg.DisplayName),  Color = HexToRustFormat("#FFFFFF8B") },
                        new CuiRectTransformComponent{  AnchorMin = $"0.02 {0.95 - (i * 0.06)}", AnchorMax = $"0.11 {0.995 - (i * 0.06)}" },
                        }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.15 {0.95 - (i * 0.06)}", AnchorMax = $"1 {0.99 - (i * 0.06)}" },
                    Button = { Command = $"{cfg.Command}", Color = "0 0 0 0" },
                    Text = { Text = cfg.DisplayName, Color = HexToRustFormat("#FFFFFFFF"), Align = TextAnchor.MiddleLeft }
                }, DROP_MENU_PANEL);
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            LoadImage();
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++) 
            { 
                var player = BasePlayer.activePlayerList[i];
                InterfaceMenu(player);
            }
            BroadCast();
            UpdateOnlineLabel();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerInit(player);
                    return;
                });
            }
            InterfaceMenu(player);
        }

        void Unload()
        {
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                var player = BasePlayer.activePlayerList[i];
                CuiHelper.DestroyUi(player, BROADCAST_PARENT);
                CuiHelper.DestroyUi(player, DROP_MENU_PANEL);
                CuiHelper.DestroyUi(player, MENU_PARENT);
            }
        }

        #endregion

        #region Command

        [ConsoleCommand("pmenu")]
        void PerMentCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            switch (arg.Args[0])
            {
                case "open":
                    {
                        if (IsOpenMenu(player))
                        {
                            CuiHelper.DestroyUi(player, DROP_MENU_PANEL);
                            PlayerOpenMenu.Remove(player.userID);
                        }
                        else
                        {
                            DropListMenu(player);
                            PlayerOpenMenu.Add(player.userID);
                        }
                        break;
                    }
            }
        }

        #endregion

        #region Metods

        void LoadImage()
        {
            AddImage(config.UrlMenu, config.ButtonName);
            for (int i = 0; i < config.MenuSettings.Count; i++)
                AddImage(config.MenuSettings[i].URLIco, config.MenuSettings[i].DisplayName);
        }

        bool IsOpenMenu(BasePlayer player)
        {
            if (PlayerOpenMenu.Contains(player.userID)) return true;
            else return false;
        }

        #endregion

        #region Utilites

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

        #endregion
    }
}
