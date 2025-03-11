using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Дополнительное GUI", "BadMandarin", "1.0.1")]
    [Description("Дополнительное GUI")]
    class MiniPanel : RustPlugin
    {
        #region Classes
        private class PluginConfig
        {
            [JsonProperty("Гл.Анчор панельки")]
            public string PanelAnchor;

            [JsonProperty("Гл.Офсет панельки (Min)")]
            public string PanelOffsetMin;
            [JsonProperty("Гл.Офсет панельки (Max)")]
            public string PanelOffsetMax;

            [JsonProperty("Гл.Текст панельки")]
            public string PanelText;
            [JsonProperty("Гл.Цвет текста")]
            public string PanelColor;
            [JsonProperty("Гл.Прозрачность текста")]
            public float PanelAlpha;
            [JsonProperty("Гл.Команда")]
            public string PanelCmd;

            [JsonProperty("Стрелочка (Цвет)")]
            public string ArrowColor;
            [JsonProperty("Стрелочка включена? (1 - да, 0 - нет)")]
            public bool ArrowMode;

            [JsonProperty("Больше панелей")]
            public List<AddtionalPanel> _listPanels = new List<AddtionalPanel>();
        }

        private class AddtionalPanel
        {
            [JsonProperty("Анчор панельки")]
            public string PanelAnchor;
            [JsonProperty("Офсет панельки (Min)")]
            public string PanelOffsetMin;
            [JsonProperty("Офсет панельки (Max)")]
            public string PanelOffsetMax;
            [JsonProperty("Картинка")]
            public string PanelImageUrl;
            [JsonProperty("Команда")]
            public string PanelCmd;
        }
        #endregion

        #region Variables
        private string UI_Layer = "UI_Panelka";
        private PluginConfig config;
        private Dictionary<ulong, bool> _playerInfo = new Dictionary<ulong, bool>();
        #endregion

        #region OxideHooks
        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        void Unload()
        {
            foreach(var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, UI_Layer);
            }
        }
            

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }
        
        void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                Draw_UIMain(player, config.PanelAnchor);
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            Draw_UIMain(player, config.PanelAnchor);
        }
        #endregion

        #region Interface
        private void Draw_UIMain(BasePlayer player, string MainPosition = "0.01 0.99")
        {
            if (player == null) return;

            if (!_playerInfo.ContainsKey(player.userID))
                _playerInfo.Add(player.userID, false);

            CuiElementContainer container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = false,
                        RectTransform = { AnchorMin = MainPosition, AnchorMax = MainPosition, OffsetMin = config.PanelOffsetMin, OffsetMax = config.PanelOffsetMax },
                        
                        Image = { Color = GetColor("#E6E6E6", 0f) }
                    },
                    "Overlay", UI_Layer
                },
                new CuiElement
                {
                    Parent = UI_Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = config.PanelText,
                            Align = TextAnchor.UpperLeft,
                            Font = "robotocondensed-bold.ttf",
                            Color = GetColor(config.PanelColor, config.PanelAlpha)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = config.PanelCmd },
                        Text = { Text = "" }
                    },
                    UI_Layer
                }
            };
            if(config._listPanels.Count() < 1)
            {
                CuiHelper.DestroyUi(player, UI_Layer);
                CuiHelper.AddUi(player, container);
                return;
            }
            if (config.ArrowMode)
            {
                container.Add(new CuiElement
                {
                    Parent = UI_Layer,
                    Name = UI_Layer + $".Toggle",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = _playerInfo[player.userID]?"<size=26>▶</size>":"<size=26>◀</size>",
                            Align = TextAnchor.MiddleCenter,
                            Color = GetColor(config.ArrowColor),
                            Font = "robotocondensed-bold.ttf"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "1 0",
                            AnchorMax = "1 1",
                            OffsetMin = "0 0",
                            OffsetMax = "25 -10"
                        }
                    }
                });
            
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = "UI_TogglePanel" },
                    Text = { Text = "" }
                }, UI_Layer + $".Toggle");
            }
            if (_playerInfo[player.userID])
            {
                CuiHelper.DestroyUi(player, UI_Layer);
                CuiHelper.AddUi(player, container);
                return;
            }


            int counter = 0;
            foreach(var panel in config._listPanels)
            {

                container.Add(new CuiElement
                {
                    Parent = UI_Layer,
                    Name = UI_Layer + $".Panel_{counter}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = panel.PanelImageUrl,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = panel.PanelAnchor,
                            AnchorMax = panel.PanelAnchor,
                            OffsetMin = panel.PanelOffsetMin,
                            OffsetMax = panel.PanelOffsetMax
                        }
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = panel.PanelCmd },
                    Text = { Text = "" }
                }, UI_Layer + $".Panel_{counter}");
                counter++;
            }

            CuiHelper.DestroyUi(player, UI_Layer);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Commands
        [ConsoleCommand("UI_TogglePanel")]
        private void CMD_UI_TogglePanel(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;

            if (!_playerInfo.ContainsKey(player.userID))
                _playerInfo.Add(player.userID, false);
            else
            {

                if (_playerInfo[player.userID])
                {
                    _playerInfo[player.userID] = false;
                    Draw_UIMain(player, config.PanelAnchor);
                }
                else
                {
                    _playerInfo[player.userID] = true;
                    Draw_UIMain(player, config.PanelAnchor);
                }
            }
            return;
        }
        #endregion

        #region Util
        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                PanelAnchor = "0.005 0.99",
                PanelOffsetMin = "0 -75",
                PanelOffsetMax = "180 0",
                PanelText = "<size=23>Игровой Сервер #1</size>\n<size=18>Открыть корзину</size>",
                PanelColor = "#E6E6E6",
                PanelAlpha = 0.5f,
                PanelCmd = "chat.say /store",
                ArrowMode = true,
                ArrowColor = "#FFFFFF",


                _listPanels = new List<AddtionalPanel>
                {
                   new AddtionalPanel
                   {
                        PanelAnchor = "1 1",
                        PanelOffsetMin = "0 -35",
                        PanelOffsetMax = "35 0",
                        PanelImageUrl = "https://steamcommunity-a.akamaihd.net/economy/image/-9a81dlWLwJ2UUGcVs_nsVtzdOEdtWwKGZZLQHTxDZ7I56KU0Zwwo4NUX4oFJZEHLbXU5A1PIYQNqhpOSV-fRPasw8rsUFJ5KBFZv668FFY4naeaJGhGtdnmx4Tek_bwY-iFlGlUsJMp3LuTot-mjFGxqUttZ2r3d4eLMlhpnZPxZK0/256fx256f",
                        PanelCmd = "chat.say /case1",
                   },
                   new AddtionalPanel
                   {
                        PanelAnchor = "1 1",
                        PanelOffsetMin = "35 -35",
                        PanelOffsetMax = "70 0",
                        PanelImageUrl = "https://ya-webdesign.com/images/csgo-cases-png-11.png",
                        PanelCmd = "chat.say /case2",
                   }
                }
            };
        }

        public static string GetColor(string hex, float alpha = 1f)
        {
            var color = ColorTranslator.FromHtml(hex);
            var r = Convert.ToInt16(color.R) / 255f;
            var g = Convert.ToInt16(color.G) / 255f;
            var b = Convert.ToInt16(color.B) / 255f;

            return $"{r} {g} {b} {alpha}";
        }
        #endregion
    }
}
