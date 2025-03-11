using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DVMenu", "ClayMond", "0.0.5")]
    [Description("Спасибо за покупку - by russia-oxide.ru")]
    class DVMenu : RustPlugin
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
        public List<BasePlayer> IsOpenMenu = new List<BasePlayer>();
        public List<string> ActiveEvent = new List<string>();
        #endregion

        #region Configuration


        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройки плагина")]
            public SettingsPlugin SettingPlugin = new SettingsPlugin();

            internal class SettingsPlugin
            {
                [JsonProperty("Настройка иконки в панели")]
                public string PNG;
                [JsonProperty("Настройка логотипа в панели")]
                public string Logo;
                [JsonProperty("Командая при нажатие на логотип")]
                public string Bind;
                [JsonProperty("HEX Цвет панели")]
                public string HexColorPanel;
				[JsonProperty("HEX Цвет текста панели")]
				public string TextColorPanel;
                [JsonProperty("Интервал обновления информационной панели")]
                public int IntervalUpdateInfoPanel;
                [JsonProperty("Интервал обновления ивентов")]
                public int IntervalUpdateEvents;

            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    SettingPlugin = new SettingsPlugin
                    {
                        HexColorPanel = "#FFFFFF",
						TextColorPanel = "#FFFFFF",
                        IntervalUpdateInfoPanel = 5,
                        IntervalUpdateEvents = 5,
						Bind = "chat.say /info",
						Logo = "https://i.ibb.co/6Fzdntv/wolf.png",
                        PNG = "https://i.ibb.co/D74C9sZ/PHTlu4K.png",
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
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region UI

        #region Parent
        public static string INTERFACE_PARENT_PANEL = "INTERFACE_MENU_PARENT_PANEL";
		public static string LOGO_PANEL = "LOGO_PANEL";
        #endregion

        void OnPlayerConnected(BasePlayer player)
        {
            UI_Panel_Interface(player);
			LOGO_UI(player);
        }


        void UI_Panel_Interface(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, INTERFACE_PARENT_PANEL);
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-200 -50", OffsetMax = "-3 -3" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", INTERFACE_PARENT_PANEL);

            container.Add(new CuiElement
            {
                Parent = INTERFACE_PARENT_PANEL,
                Name = "Joined",
                Components = {
                        new CuiImageComponent {
                            Png = GetImage("PNG_PANEL"),
                            Color = HexToRustFormat(config.SettingPlugin.HexColorPanel),
							
                        },
                        new CuiRectTransformComponent { AnchorMin = "0.7700684 0.07777759", AnchorMax = "0.9768711 0.922222" }
                    },
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1.1" },
                Text = { Text = $"<size=9>ЗАХОДЯТ</size>\n<size=16>{SingletonComponent<ServerMgr>.Instance.connectionQueue.Joining}</size>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(config.SettingPlugin.TextColorPanel) }
            }, "Joined");

            container.Add(new CuiElement
            {
                Parent = INTERFACE_PARENT_PANEL,
                Name = "Sleepers",
                Components = {
                        new CuiImageComponent {
                            Png = GetImage("PNG_PANEL"),
							Color = HexToRustFormat(config.SettingPlugin.HexColorPanel),
                        },
                        new CuiRectTransformComponent { AnchorMin = "0.5390563 0.07777759", AnchorMax = "0.745859 0.922222" }
                    },
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1.1" },
                Text = { Text = $"<size=9>СПЯЩИЕ</size>\n<size=16>{BasePlayer.sleepingPlayerList.Count.ToString()}</size>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(config.SettingPlugin.TextColorPanel) }
            }, "Sleepers");

            container.Add(new CuiElement
            {
                Parent = INTERFACE_PARENT_PANEL,
                Name = "Online",
                Components = {
                        new CuiImageComponent {
                            Png = GetImage("PNG_PANEL"),
							Color = HexToRustFormat(config.SettingPlugin.HexColorPanel),
                        },
                        new CuiRectTransformComponent { AnchorMin = "0.3074842 0.07777759", AnchorMax = "0.514289 0.922222" }
                    },
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1.1" },
                Text = { Text = $"<size=9>ОНЛАЙН</size>\n<size=16>{BasePlayer.activePlayerList.Count.ToString()}</size>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(config.SettingPlugin.TextColorPanel) }
            }, "Online");

            CuiHelper.AddUi(player, container);
        }

 		void LOGO_UI(BasePlayer player)
		{
            CuiElementContainer container = new CuiElementContainer();
			CuiHelper.DestroyUi(player, LOGO_PANEL);
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "10 -77", OffsetMax = "80 -8" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", LOGO_PANEL);
			
            container.Add(new CuiElement
            {
                Parent = LOGO_PANEL,
                Name = "LogoURL",
                Components = {
                        new CuiImageComponent {
                            Png = GetImage("LOGO_PANEL"),
							Color = "1 1 1 1",
							Material = "assets/icons/greyout.mat",
                        },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1.1" }
                    },
            });
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = config.SettingPlugin.Bind },
                Text = { Text = "" }
            }, LOGO_PANEL);
			
			CuiHelper.AddUi(player, container);
		}
 
        #endregion


        #region Hooks
        private void OnServerInitialized()
        {
			
			Puts("Спасибо за покупку плагина");
			Puts("Thank you for purchasing the plugin");
			
            LoadImage();

            timer.Every(config.SettingPlugin.IntervalUpdateInfoPanel, () =>
            {
				foreach(var player in BasePlayer.activePlayerList)
				    LOGO_UI(player);
            });

            timer.Every(config.SettingPlugin.IntervalUpdateInfoPanel, () =>
            {
				foreach(var player in BasePlayer.activePlayerList)
				    UI_Panel_Interface(player);
            });
            
			foreach(var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        } 

        #endregion

        #region HelpMetods
        private static string HexToRustFormat(string hex)
        {
            UnityEngine.Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        void LoadImage()
        {

            AddImage(config.SettingPlugin.PNG, "PNG_PANEL");
			AddImage(config.SettingPlugin.Logo, "LOGO_PANEL");
        }

        #endregion
    }
}
