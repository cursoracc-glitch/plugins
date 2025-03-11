
using System;
using System.Globalization;
using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("InfoPanel", "Sparkless", "0.0.2")]
    public class InfoPanel : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        private Configuration _config;

        private string CmdText = "/MENU";
        private string CmdCommand = "chat.say /menu";

        class Configuration
        {
            [JsonProperty("Название сервера")] 
            public string NameServer = "FURY RUST #1 MAX2";
            [JsonProperty("Иконка (где коробка)")] 
            public string StoreIcons = "https://i.imgur.com/J9ghJrj.png";
            [JsonProperty("Команда при нажатии на коробку")]
            public string CommandStore = "chat.say /store";
            [JsonProperty("Иконка(Онлайн)")] 
            public string OnlineIcons = "https://i.imgur.com/WvssHc7.png";
            [JsonProperty("Иконка(Спящих)")] 
            public string SleepIcons = "https://i.imgur.com/kzRLOJp.png";
            
             [JsonProperty("Иконка 1 кнопки")] 
             public string Knopka1 = "https://i.imgur.com/J9ghJrj.png";
             [JsonProperty("Иконка 2 кнопки")] 
             public string Knopka2 = "https://i.imgur.com/J9ghJrj.png";
             [JsonProperty("Иконка 3 кнопки")] 
             public string Knopka3 = "https://i.imgur.com/J9ghJrj.png";
             [JsonProperty("Иконка 4 кнопки")] 
             public string Knopka4 = "https://i.imgur.com/J9ghJrj.png";
             [JsonProperty("Иконка 5 кнопки")] 
             public string Knopka5 = "https://i.imgur.com/J9ghJrj.png";

             [JsonProperty("Текст на 1 кнопки")] 
             public string KnopkaText1 = "БЛОКИРОВКА ОРУЖИЙ";
             [JsonProperty("Текст на 2 кнопки")] 
             public string KnopkaText2 = "БЛОКИРОВКА ОРУЖИЙ";
             [JsonProperty("Текст на 3 кнопки")] 
             public string KnopkaText3 = "БЛОКИРОВКА ОРУЖИЙ";
             [JsonProperty("Текст на 4 кнопки")] 
             public string KnopkaText4 = "БЛОКИРОВКА ОРУЖИЙ";
             [JsonProperty("Текст на 5 кнопки")]
             public string KnopkaText5 = "БЛОКИРОВКА ОРУЖИЙ";

             [JsonProperty("Команда при нажатии на 1 кнопку")]
             public string CommandKnopka1 = "chat.say /store";
             [JsonProperty("Команда при нажатии на 2 кнопку")]
             public string CommandKnopka2 = "chat.say /store";
             [JsonProperty("Команда при нажатии на 3 кнопку")]
             public string CommandKnopka3 = "chat.say /store";
             [JsonProperty("Команда при нажатии на 4 кнопку")]
             public string CommandKnopka4 = "chat.say /store";
             [JsonProperty("Команда при нажатии на 5 кнопку")]
             public string CommandKnopka5 = "chat.say /store";
             
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(_config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.JsonEror");
                PrintError("Конфиг сломался, загружаю новый!...");
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        protected override void LoadDefaultConfig() => _config = new Configuration();
        protected override void SaveConfig() => Config.WriteObject(_config);

        void OnServerInitialized()
        {
            
            ImageLibrary.Call("AddImage", _config.OnlineIcons, "onlineicons");
            ImageLibrary.Call("AddImage", _config.SleepIcons, "sleepicons");
            ImageLibrary.Call("AddImage", _config.StoreIcons, "storeicons");
            ImageLibrary.Call("AddImage", _config.Knopka1, "knopka1");
            ImageLibrary.Call("AddImage", _config.Knopka2, "knopka2");
            ImageLibrary.Call("AddImage", _config.Knopka3, "knopka3");
            ImageLibrary.Call("AddImage", _config.Knopka4, "knopka4");
            ImageLibrary.Call("AddImage", _config.Knopka5, "knopka5");
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
            PrintWarning($"Плагин сделал Sparkless");
            PrintWarning($"Хотите сменить команду /menu на что то другое? отпишите мне на форуме");
        }
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "Layer");  
                CuiHelper.DestroyUi(player, "Open");  
            }  
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
			
            foreach (var players in BasePlayer.activePlayerList)
            {
                timer.Once(1, () =>
                {
                    DrawMenu(players);
                });
            }
        }
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
                timer.Once(1, () =>
                {
                   DrawMenu(players);
                });
            }
        }
        private string Layer = "Layer";

        private void DrawMenu(BasePlayer player)
        {
            var Sleepers = BasePlayer.sleepingPlayerList.Count.ToString();
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            var Panel = container.Add(new CuiPanel
            {
                Image = {Color = HexToCuiColor("#FFFFFF00")},
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -265", OffsetMax = "190 3"},
                CursorEnabled = false,
            }, "Overlay", Layer);
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "0.6117647 0.6117647 0.6117647 0.1565329"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -40", OffsetMax = "45 -2"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "storeicons"),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMax = "45 0",
                        OffsetMin = "5 -40"
                    },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "0.6117647 0.6117647 0.6117647 0.1565329"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "190 0", OffsetMin = "49.5 -20"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFFFF"), FadeIn = 1f, Text = _config.NameServer, FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "50 -22", OffsetMax = "190 0"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "0.6117647 0.6117647 0.6117647 0.1565329"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "115 -22", OffsetMin = "50 -40"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFFFF"), FadeIn = 1f, Text = $"{BasePlayer.activePlayerList.Count}/{ConVar.Server.maxplayers}", FontSize = 13, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "70 -40", OffsetMax = "115 -22"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "onlineicons"),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMax = "70 -22",
                        OffsetMin = "50 -39"
                    },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "0.6117647 0.6117647 0.6117647 0.1565329"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "190 -22", OffsetMin = "120 -40"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "sleepicons"),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMax = "140 -22",
                        OffsetMin = "123 -40"
                    },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFFFF"), FadeIn = 1f, Text = $"{Sleepers}", FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "190 -22", OffsetMin = "140 -40"},
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -60", OffsetMax = "45 -43"},
                Button = { Command = CmdCommand, Color =  "0.6117647 0.6117647 0.6117647 0.1565329"},
                Text = { Text = CmdText, FontSize = 14, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
            }, Layer);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -40", OffsetMax = "45 -2"},
                Button = { Command = _config.CommandStore, Color =  HexToCuiColor("#FFFFFF00")},
                Text = { Text = "" }
            }, Layer);

            CuiHelper.AddUi(player, container);
        }


        private string Open = "Open";
        [ChatCommand("menu")]
        private void OpenMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Open);
            var container = new CuiElementContainer();
            var Panel = container.Add(new CuiPanel
            {
                Image = {Color = HexToCuiColor("#FFFFFF00")},
                RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -265", OffsetMax = "190 3"},
                CursorEnabled = false,
            }, "Overlay", Open);
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "0.6117647 0.6117647 0.6117647 0.1565329"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "190 -65", OffsetMin = "5 -100"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "0.6117647 0.6117647 0.6117647 0.1565329"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "190 -104", OffsetMin = "5 -139"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "0.6117647 0.6117647 0.6117647 0.1565329"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "190 -143", OffsetMin = "5 -178"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "0.6117647 0.6117647 0.6117647 0.1565329"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "190 -182", OffsetMin = "5 -218"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "0.6117647 0.6117647 0.6117647 0.1565329"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "190 -222", OffsetMin = "5 -258"}
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "knopka1"),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMax = "46.5 -65",
                        OffsetMin = "5 -100"
                    },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "knopka2"),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMax = "46.5 -104",
                        OffsetMin = "5 -140"
                    },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "knopka3"),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMax = "46.5 -143",
                        OffsetMin = "5 -178"
                    },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "knopka4"),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMax = "46.5 -182",
                        OffsetMin = "5 -219"
                    },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Png = (string) ImageLibrary.Call("GetImage", "knopka5"),
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "0 1",
                        OffsetMax = "46.5 -220",
                        OffsetMin = "5 -257"
                    },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFFFF"), FadeIn = 1f, Text = _config.KnopkaText1, FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1" , OffsetMin = "50 -100", OffsetMax = "190 -65"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFFFF"), FadeIn = 1f, Text = _config.KnopkaText2, FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "50 -140", OffsetMax = "190 -104"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFFFF"), FadeIn = 1f, Text = _config.KnopkaText3, FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "50 -178", OffsetMax = "190 -143"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFFFF"), FadeIn = 1f, Text = _config.KnopkaText4, FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "50 -219", OffsetMax = "190 -182"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFFFF"), FadeIn = 1f, Text = _config.KnopkaText5, FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "50 -257", OffsetMax = "190 -220"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = Open,
                Components =
                {
                    new CuiImageComponent {FadeIn = 1f, Color = "1 0 0 0.1276685"},
                    new CuiRectTransformComponent {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "190 -45", OffsetMin = "174 -60"}
                }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "174 -60", OffsetMax = "190 -45"},
                Button = { Close = Open, Color =  HexToCuiColor("#FFFFFF00")},
                Text = { Text = "X", FontSize = 13, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
            }, Open);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -100", OffsetMax = "190 -65"},
                Button = { Command = _config.CommandKnopka1, Close = Open, Color =  HexToCuiColor("#FFFFFF00")},
                Text = { Text = "" }
            }, Open);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -139", OffsetMax = "190 -104"},
                Button = { Command = _config.CommandKnopka2, Close = Open, Color =  HexToCuiColor("#FFFFFF00")},
                Text = { Text = "" }
            }, Open);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -178", OffsetMax = "190 -143"},
                Button = { Command = _config.CommandKnopka3, Close = Open, Color =  HexToCuiColor("#FFFFFF00")},
                Text = { Text = "" }
            }, Open);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -218", OffsetMax = "190 -182"},
                Button = { Command = _config.CommandKnopka4, Close = Open, Color =  HexToCuiColor("#FFFFFF00")},
                Text = { Text = "" }
            }, Open);
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "5 -258", OffsetMax = "190 -222"},
                Button = { Command = _config.CommandKnopka5, Close = Open, Color =  HexToCuiColor("#FFFFFF00")},
                Text = { Text = "" }
            }, Open);


            CuiHelper.AddUi(player, container);
        }
        
        #region Helpers

        private static string HexToCuiColor(string hex)
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

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
			
        #endregion
    }
}