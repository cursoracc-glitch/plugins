using System;
using System.Collections.Generic;
using System.Linq;
using Rust;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("ZealStatistics", "Kira", "1.0.7")]
    [Description("Плагин сбора статистики для сервера Rust.")]
    class ZealStatistics : RustPlugin
    {
        #region [Reference] / [Запросы]

        [PluginReference] Plugin ImageLibrary;
        private StoredData DataBase = new StoredData();

        private string GetImg(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        static public ConfigData config;


        public class ConfigData
        {
            [JsonProperty(PropertyName = "ZealStatistics")]
            public GUICFG ZealStatistics = new GUICFG();

            public class GUICFG
            {
                [JsonProperty(PropertyName = "Разрешить просматривать информацию об игроках ?")]
                public bool info;
            }
        }

        public ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                ZealStatistics = new ConfigData.GUICFG
                {
                    info = true
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region [Dictionary/Vars] / [Словари/Переменные]

        public ulong state1;
        public ulong state2;
        public ulong state3;
        public ulong state4;
        public ulong state5;

        private string Sharp = "assets/content/ui/ui.background.tile.psd";
        private string Blur = "assets/content/ui/uibackgroundblur.mat";
        private string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private string regular = "robotocondensed-regular.ttf";
        public ulong LastDamagePlayer;

        public class Filter
        {
            public string Name;
            public int Number;
        }

        public List<Filter> Filters = new List<Filter>
        {
            new Filter
            {
                Name = "● Убийствам",
                Number = 0
            },
            new Filter
            {
                Name = "● Смертям",
                Number = 1
            },
            new Filter
            {
                Name = "● Убийствам животных",
                Number = 2
            },
            new Filter
            {
                Name = "● Добыче серы",
                Number = 3
            },
            new Filter
            {
                Name = "● Добыче камня",
                Number = 4
            },
            new Filter
            {
                Name = "● Добыче металла",
                Number = 5
            },
            new Filter
            {
                Name = "● Добыче дерева",
                Number = 6
            },
            new Filter
            {
                Name = "● Уничтоженным вертолётам",
                Number = 7
            },
            new Filter
            {
                Name = "● Уничтоженным танкам",
                Number = 8
            },
            new Filter
            {
                Name = "● Наигранному времени",
                Number = 9
            }
        };

        #endregion

        #region [DrawUI] / [Показ UI]

        string Layer = "BoxStatistics";

        void PlayerList(BasePlayer player)
        {
            CuiElementContainer Gui = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);

            Gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#000000F9"),
                    Material = Blur,
                    Sprite = radial
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", Layer);

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "close.stats",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, Layer, "CloseStatistics");

            Gui.Add(new CuiElement
            {
                Name = "Zagolovok",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 35,
                        Text = "ПАНЕЛЬ УПРАВЛЕНИЯ СТАТИСТИКОЙ",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.9213709",
                        AnchorMax = "1 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "DescPl",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 25,
                        Text = "Выберите игрока, чтобы заблокировать сбор данных для него",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.8881495",
                        AnchorMax = "1 0.9446309"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "BoxPlayers",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.04166667 0.83425",
                        AnchorMax = "0.1536459 0.8731388"
                    }
                }
            });

            int x = 0, y = 0, num = 0;
            foreach (var plobj in BasePlayer.activePlayerList)
            {
                if (x == 8)
                {
                    x = 0;
                    y++;
                }

                string nick = plobj.displayName;
                if (DataBase.IgnorePlayers.Contains(plobj.userID))
                {
                    nick += " <color=#DE3A3A>✖</color>";
                }
                else
                {
                    nick += " <color=#62E24B>✔</color>";
                }


                Gui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "zstats.ban " + plobj.userID,
                            Color = HexToRustFormat("#0000005A"),
                            Material = Blur,
                            FadeIn = 0.1f + (num * 0.01f)
                        },
                        Text =
                        {
                            Text = nick,
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#ffffff"),
                            Font = regular,
                            FontSize = 15,
                            FadeIn = 0.1f + (num * 0.01f)
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0 + (x * 1.02)} {0 - (y * 1.1)}",
                            AnchorMax = $"{1 + (x * 1.02)} {1 - (y * 1.1)}"
                        }
                    }, "BoxPlayers", "Player" + num);
                x++;
                num++;
            }

            CuiHelper.AddUi(player, Gui);
        }

        void MainGui(BasePlayer player)
        {
            CheckDataBase(player);
            CuiElementContainer Gui = new CuiElementContainer();
            var DB = DataBase.StatisticDB[player.userID];
            filter = false;

            Gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#000000F9"),
                    Material = Blur,
                    Sprite = radial
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", "BoxStatistics");

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "close.stats",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, Layer, "ButtonCloseGUI");

            Gui.Add(new CuiElement
            {
                Name = Layer + "ServerName",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 40,
                        Text = ConVar.Server.hostname,
                        Font = "robotocondensed-regular.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.9213709",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "servertop",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat("#EAEAEAFF"),
                    FontSize = 18,
                    Text = "ПЕРЕЙТИ В ОБЩУЮ СТАТИСТИКУ\n▼"
                },
                RectTransform =
                {
                    AnchorMin = "0.3665158 0.01108896",
                    AnchorMax = "0.6329185 0.08165347"
                }
            }, Layer, "GoTop");

            if (player.IsAdmin)
            {
                Gui.Add(new CuiButton
                {
                    Button =
                    {
                        Command = "stats.manager",
                        Color = "0 0 0 0"
                    },
                    Text =
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 18,
                        Text = "ПАНЕЛЬ УПРАВЛЕНИЯ СТАТИСТИКОЙ\n▶"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.7331769 0.01108896",
                        AnchorMax = "1 0.08165347"
                    }
                }, Layer, "GoStatisticsManager");
            }

            Gui.Add(new CuiElement
            {
                Name = Layer + "AvatarBG",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#FFFFFFA4")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4360861 0.6925457",
                        AnchorMax = "0.5627829 0.9183521"
                    }
                }
            });


            Gui.Add(new CuiElement
            {
                Name = Layer + "Avatar",
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = "1 1 1 1",
                        Png = GetImg(player.UserIDString)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4372173 0.6945619",
                        AnchorMax = "0.5616518 0.9163328"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Sprite",
                Parent = Layer + "Avatar",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#000000AA"),
                        Sprite = radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.99 0.99"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Nick",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 23,
                        Text = player.displayName,
                        Font = "robotocondensed-regular.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3212669 0.641129",
                        AnchorMax = "0.6804298 0.6875"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line1",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3829193 0.6401247",
                        AnchorMax = "0.6193514 0.6411328"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "SteamID",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 14,
                        Text = player.UserIDString,
                        Font = "robotocondensed-regular.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3212669 0.6088712",
                        AnchorMax = "0.6804298 0.6421358"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #region BoxValue1

            Gui.Add(new CuiElement
            {
                Name = "BoxValue1",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1357466 0.4969758",
                        AnchorMax = "0.2697965 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName1",
                Parent = "BoxValue1",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Убийств"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line2",
                Parent = "BoxValue1",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value1",
                Parent = "BoxValue1",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Kills.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue2

            Gui.Add(new CuiElement
            {
                Name = "BoxValue2",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2828054 0.4969758",
                        AnchorMax = "0.4168553 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName2",
                Parent = "BoxValue2",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Смертей"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line3",
                Parent = "BoxValue2",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value2",
                Parent = "BoxValue2",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Death.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue3

            Gui.Add(new CuiElement
            {
                Name = "BoxValue3",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4298643 0.4969758",
                        AnchorMax = "0.5639175 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName3",
                Parent = "BoxValue3",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Убийств животных"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line4",
                Parent = "BoxValue3",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value3",
                Parent = "BoxValue3",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.AnimalKills.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue4

            Gui.Add(new CuiElement
            {
                Name = "BoxValue4",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5769231 0.4969758",
                        AnchorMax = "0.7109792 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName4",
                Parent = "BoxValue4",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 17,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Уничтожено танков"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line5",
                Parent = "BoxValue4",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value4",
                Parent = "BoxValue4",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.BradleyKills.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue5

            Gui.Add(new CuiElement
            {
                Name = "BoxValue5",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.7239819 0.4969758",
                        AnchorMax = "0.8580387 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName5",
                Parent = "BoxValue5",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 17,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Уничтожено вертолётов"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line6",
                Parent = "BoxValue5",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value5",
                Parent = "BoxValue5",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.HeliKills.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue6

            Gui.Add(new CuiElement
            {
                Name = "BoxValue6",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2092738 0.3669356",
                        AnchorMax = "0.343323 0.4677444"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName6",
                Parent = "BoxValue6",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Добыто серы"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line7",
                Parent = "BoxValue6",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value6",
                Parent = "BoxValue6",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Sulfur.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue7

            Gui.Add(new CuiElement
            {
                Name = "BoxValue7",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3563318 0.3669356",
                        AnchorMax = "0.4903821 0.4677444"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName7",
                Parent = "BoxValue7",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Добыто камня"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line8",
                Parent = "BoxValue7",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value7",
                Parent = "BoxValue7",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Stones.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue8

            Gui.Add(new CuiElement
            {
                Name = "BoxValue8",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5033906 0.3669356",
                        AnchorMax = "0.637444 0.4677444"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName8",
                Parent = "BoxValue8",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Добыто металла"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line9",
                Parent = "BoxValue8",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value8",
                Parent = "BoxValue8",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.MetalOre.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue9

            Gui.Add(new CuiElement
            {
                Name = "BoxValue9",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.6504495 0.3669356",
                        AnchorMax = "0.7845057 0.4677444"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName9",
                Parent = "BoxValue9",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Добыто дерева"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line10",
                Parent = "BoxValue9",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value9",
                Parent = "BoxValue9",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Wood.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue10

            Gui.Add(new CuiElement
            {
                Name = "BoxValue10",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4318515 0.2558242",
                        AnchorMax = "0.5659018 0.356633"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName10",
                Parent = "BoxValue10",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Наигранно времени"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line11",
                Parent = "BoxValue10",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4800047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value10",
                Parent = "BoxValue10",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = $"{GetPlayerTimePlayed(DataBase.StatisticDB[player.userID].TimePlayed)}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-1 0.1499948",
                        AnchorMax = "2 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            CuiHelper.AddUi(player, Gui);
        }

        void InfoPlayer(ulong steamid, ulong initiator)
        {
            BasePlayer initiatorpl = BasePlayer.FindByID(initiator);

            CuiElementContainer Gui = new CuiElementContainer();
            var DB = DataBase.StatisticDB[steamid];

            DestroyServerGUI(initiatorpl);
            DestroyMainGUI(initiatorpl);

            Gui.Add(new CuiElement
            {
                Name = Layer + "ServerName",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 40,
                        Text = $"СТАТИСТИКА ИГРОКА : {DB.Name}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.9213709",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "servertop",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat("#EAEAEAFF"),
                    FontSize = 18,
                    Text = "ПЕРЕЙТИ В ОБЩУЮ СТАТИСТИКУ\n▼"
                },
                RectTransform =
                {
                    AnchorMin = "0.3665158 0.01108896",
                    AnchorMax = "0.6329185 0.08165347"
                }
            }, Layer, "GoTop");

            Gui.Add(new CuiElement
            {
                Name = Layer + "AvatarBG",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#FFFFFFA4")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4360861 0.6925457",
                        AnchorMax = "0.5627829 0.9183521"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Avatar",
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = "1 1 1 1",
                        Png = GetImg(covalence.Players.FindPlayerById(DB.SteamID.ToString()).Id)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4372173 0.6945619",
                        AnchorMax = "0.5616518 0.9163328"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Sprite",
                Parent = Layer + "Avatar",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#000000AA"),
                        Sprite = radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.99 0.99"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Nick",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 23,
                        Text = DB.Name
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3212669 0.641129",
                        AnchorMax = "0.6804298 0.6875"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line1",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3829193 0.6401247",
                        AnchorMax = "0.6193514 0.6411328"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "SteamID",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 14,
                        Text = DB.SteamID.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3212669 0.6088712",
                        AnchorMax = "0.6804298 0.6421358"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #region BoxValue1

            Gui.Add(new CuiElement
            {
                Name = "BoxValue1",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1357466 0.4969758",
                        AnchorMax = "0.2697965 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName1",
                Parent = "BoxValue1",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Убийств"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line2",
                Parent = "BoxValue1",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value1",
                Parent = "BoxValue1",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Kills.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue2

            Gui.Add(new CuiElement
            {
                Name = "BoxValue2",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2828054 0.4969758",
                        AnchorMax = "0.4168553 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName2",
                Parent = "BoxValue2",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Смертей"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line3",
                Parent = "BoxValue2",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value2",
                Parent = "BoxValue2",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Death.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue3

            Gui.Add(new CuiElement
            {
                Name = "BoxValue3",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4298643 0.4969758",
                        AnchorMax = "0.5639175 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName3",
                Parent = "BoxValue3",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Убийств животных"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line4",
                Parent = "BoxValue3",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value3",
                Parent = "BoxValue3",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.AnimalKills.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue4

            Gui.Add(new CuiElement
            {
                Name = "BoxValue4",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5769231 0.4969758",
                        AnchorMax = "0.7109792 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName4",
                Parent = "BoxValue4",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 17,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Уничтожено танков"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line5",
                Parent = "BoxValue4",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value4",
                Parent = "BoxValue4",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.BradleyKills.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue5

            Gui.Add(new CuiElement
            {
                Name = "BoxValue5",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.7239819 0.4969758",
                        AnchorMax = "0.8580387 0.5977818"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName5",
                Parent = "BoxValue5",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 17,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Уничтожено вертолётов"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line6",
                Parent = "BoxValue5",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.5000047"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value5",
                Parent = "BoxValue5",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.HeliKills.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue6

            Gui.Add(new CuiElement
            {
                Name = "BoxValue6",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2092738 0.3669356",
                        AnchorMax = "0.343323 0.4677444"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName6",
                Parent = "BoxValue6",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Добыто серы"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line7",
                Parent = "BoxValue6",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value6",
                Parent = "BoxValue6",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Sulfur.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue7

            Gui.Add(new CuiElement
            {
                Name = "BoxValue7",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3563318 0.3669356",
                        AnchorMax = "0.4903821 0.4677444"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName7",
                Parent = "BoxValue7",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Добыто камня"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line8",
                Parent = "BoxValue7",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value7",
                Parent = "BoxValue7",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Stones.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue8

            Gui.Add(new CuiElement
            {
                Name = "BoxValue8",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5033906 0.3669356",
                        AnchorMax = "0.637444 0.4677444"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName8",
                Parent = "BoxValue8",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Добыто металла"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line9",
                Parent = "BoxValue8",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value8",
                Parent = "BoxValue8",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.MetalOre.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue9

            Gui.Add(new CuiElement
            {
                Name = "BoxValue9",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.6504495 0.3669356",
                        AnchorMax = "0.7845057 0.4677444"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName9",
                Parent = "BoxValue9",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Добыто дерева"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line10",
                Parent = "BoxValue9",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4900047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value9",
                Parent = "BoxValue9",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = DB.Wood.ToString()
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.1499948",
                        AnchorMax = "1 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            #region BoxValue10

            Gui.Add(new CuiElement
            {
                Name = "BoxValue10",
                Parent = "BoxStatistics",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4318515 0.2558242",
                        AnchorMax = "0.5659018 0.356633"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "ValueName10",
                Parent = "BoxValue10",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 18,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = "Наигранно времени"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.5300055",
                        AnchorMax = "1 0.8300002"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Line11",
                Parent = "BoxValue10",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01265823 0.4800047",
                        AnchorMax = "0.9873418 0.4900044"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = Layer + "Value10",
                Parent = "BoxValue10",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 19,
                        Color = HexToRustFormat("#CBCBCBFF"),
                        Text = $"{GetPlayerTimePlayed(DataBase.StatisticDB[steamid].TimePlayed)}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-1 0.1499948",
                        AnchorMax = "2 0.4499854"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            #endregion

            CuiHelper.AddUi(BasePlayer.FindByID(initiator), Gui);
        }

        void ServerTOP(BasePlayer player, int filter)
        {
            CheckDataBase(player);
            CuiElementContainer GUI = new CuiElementContainer();
            DestroyMainGUI(player);
            DestroyServerGUI(player);
            MathStates(player, filter);

            GUI.Add(new CuiButton
            {
                Button =
                {
                    Command = "backtomain",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleCenter,
                    Color = HexToRustFormat("#EAEAEAFF"),
                    FontSize = 18,
                    Text = "ПЕРЕЙТИ В ЛИЧНУЮ СТАТИСТИКУ\n▼"
                },
                RectTransform =
                {
                    AnchorMin = "0.3665158 0.01108896",
                    AnchorMax = "0.6329185 0.08165347"
                }
            }, Layer, "GoMainGui");


            GUI.Add(new CuiElement
            {
                Name = "ZagServerTOP",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 40,
                        Text = "ОБЩАЯ СТАТИСТИКА"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.9354839",
                        AnchorMax = "1 1"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });


            CuiHelper.AddUi(player, GUI);
        }

        #endregion

        #region [Hooks] / [Крюки]

        void MathStates(BasePlayer player, int filter)
        {
            CuiElementContainer GUI = new CuiElementContainer();

            var states = DataBase.StatisticDB.OrderByDescending(u => u.Value.Kills);
            switch (filter)
            {
                case 0:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.Kills);
                    break;
                case 1:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.Death);
                    break;
                case 2:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.AnimalKills);
                    break;
                case 3:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.Sulfur);
                    break;
                case 4:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.Stones);
                    break;
                case 5:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.MetalOre);
                    break;
                case 6:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.Wood);
                    break;
                case 7:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.HeliKills);
                    break;
                case 8:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.BradleyKills);
                    break;
                case 9:
                    states = DataBase.StatisticDB.OrderByDescending(u => u.Value.TimePlayed);
                    break;
            }

            int i = 1;
            foreach (var user in states)
            {
                if (i == 1)
                {
                    state1 = user.Value.SteamID;
                }

                if (i == 2)
                {
                    state2 = user.Value.SteamID;
                }

                if (i == 3)
                {
                    state3 = user.Value.SteamID;
                }

                if (i == 4)
                {
                    state4 = user.Value.SteamID;
                }

                if (i == 5)
                {
                    state5 = user.Value.SteamID;
                }

                i++;
            }

            if (state1 != 0)
            {
                #region TOP1

                var infopl1 = DataBase.StatisticDB[state1];
                GUI.Add(new CuiElement
                {
                    Name = "BGAvatarTOP1",
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#DEC128FF")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.4360861 0.6925457",
                            AnchorMax = "0.5627829 0.9183521"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "AvatarTOP1",
                    Parent = "BGAvatarTOP1",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Png = GetImg(covalence.Players.FindPlayerById(infopl1.SteamID.ToString()).Id)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0194796 0.0194732",
                            AnchorMax = "0.9739944 0.973"
                        }
                    }
                });


                GUI.Add(new CuiElement
                {
                    Name = "StateTXT1",
                    Parent = "AvatarTOP1",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#FFE700FF"),
                            FontSize = 20,
                            Text = "#1"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7891649 0.01617118",
                            AnchorMax = "0.9817026 0.1706513"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "Sprite1",
                    Parent = "AvatarTOP1",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#000000AA"),
                            Sprite = radial
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.99 0.99"
                        }
                    }
                });

                if (config.ZealStatistics.info == true)
                {
                    GUI.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "infopl " + infopl1.SteamID,
                            Color = HexToRustFormat("#0000006A"),
                            Sprite = radial
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 15,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            Text = "ИНФО"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.7136346 0.8636246",
                            AnchorMax = "0.9863615 0.9863536"
                        }
                    }, "AvatarTOP1", "InfoPlayer1");
                }

                GUI.Add(new CuiElement
                {
                    Name = "NickTOP1",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 26,
                            Text = infopl1.Name
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3212669 0.641129",
                            AnchorMax = "0.6804298 0.6875"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                #endregion
            }

            if (state2 != 0)
            {
                #region TOP2

                var infopl2 = DataBase.StatisticDB[state2];
                GUI.Add(new CuiElement
                {
                    Name = "BGAvatarTOP2",
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#969696E5")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.1809 0.3750011",
                            AnchorMax = "0.268 0.530243"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "AvatarTOP2",
                    Parent = "BGAvatarTOP2",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Png = GetImg(covalence.Players.FindPlayerById(infopl2.SteamID.ToString()).Id)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0194796 0.0194732",
                            AnchorMax = "0.972 0.973"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "StateTXT2",
                    Parent = "AvatarTOP2",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#C7C7C7E5"),
                            FontSize = 14,
                            Text = "#2"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7891649 0.01617118",
                            AnchorMax = "0.9817026 0.1706513"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "Sprite2",
                    Parent = "AvatarTOP2",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#000000AA"),
                            Sprite = radial
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.99 0.99"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "NickTOP2",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 20,
                            Text = infopl2.Name
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.141402 0.325601",
                            AnchorMax = "0.311086 0.371973"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
                if (config.ZealStatistics.info == true)
                {
                    GUI.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "infopl " + infopl2.SteamID,
                            Color = HexToRustFormat("#0000006A"),
                            Sprite = radial
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 10,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            Text = "ИНФО"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.7136346 0.8636246",
                            AnchorMax = "0.9863615 0.9863536"
                        }
                    }, "AvatarTOP2", "InfoPlayer2");
                }

                #endregion
            }

            if (state3 != 0)
            {
                #region TOP3

                var infopl3 = DataBase.StatisticDB[state3];
                GUI.Add(new CuiElement
                {
                    Name = "BGAvatarTOP3",
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#969696E5")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.361991 0.3750011",
                            AnchorMax = "0.4490981 0.530243"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "AvatarTOP3",
                    Parent = "BGAvatarTOP3",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Png = GetImg(covalence.Players.FindPlayerById(infopl3.SteamID.ToString()).Id)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.0194796 0.0194732",
                            AnchorMax = "0.971 0.973"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "StateTXT3",
                    Parent = "AvatarTOP3",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#C7C7C7E5"),
                            FontSize = 14,
                            Text = "#3"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7891649 0.01617118",
                            AnchorMax = "0.9817026 0.1706513"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "Sprite3",
                    Parent = "AvatarTOP3",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#000000AA"),
                            Sprite = radial
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.99 0.99"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "NickTOP3",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 20,
                            Text = infopl3.Name
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.3223982 0.325601",
                            AnchorMax = "0.4920815 0.3719733"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
                if (config.ZealStatistics.info == true)
                {
                    GUI.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "infopl " + infopl3.SteamID,
                            Color = HexToRustFormat("#0000006A"),
                            Sprite = radial
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 10,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            Text = "ИНФО"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.7136346 0.8636246",
                            AnchorMax = "0.9863615 0.9863536"
                        }
                    }, "AvatarTOP3", "InfoPlayer3");
                }

                #endregion
            }

            if (state4 != 0)
            {
                #region TOP4

                var infopl4 = DataBase.StatisticDB[state4];
                GUI.Add(new CuiElement
                {
                    Name = "BGAvatarTOP4",
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#969696E5")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5429865 0.375001",
                            AnchorMax = "0.6300992 0.530243"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "AvatarTOP4",
                    Parent = "BGAvatarTOP4",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Png = GetImg(covalence.Players.FindPlayerById(infopl4.SteamID.ToString()).Id)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.018 0.019",
                            AnchorMax = "0.973 0.973"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "StateTXT4",
                    Parent = "AvatarTOP4",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#C7C7C7E5"),
                            FontSize = 14,
                            Text = "#4"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7891649 0.01617118",
                            AnchorMax = "0.9817026 0.1706513"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "Sprite4",
                    Parent = "AvatarTOP4",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#000000AA"),
                            Sprite = radial
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.99 0.99"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "NickTOP4",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 20,
                            Text = infopl4.Name
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5033937 0.325601",
                            AnchorMax = "0.6742211 0.371973"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
                if (config.ZealStatistics.info == true)
                {
                    GUI.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "infopl " + infopl4.SteamID,
                            Color = HexToRustFormat("#0000006A"),
                            Sprite = radial
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 10,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            Text = "ИНФО"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.7136346 0.8636246",
                            AnchorMax = "0.9863615 0.9863536"
                        }
                    }, "AvatarTOP4", "InfoPlayer4");
                }

                #endregion
            }

            if (state5 != 0)
            {
                #region TOP5

                var infopl5 = DataBase.StatisticDB[state5];
                GUI.Add(new CuiElement
                {
                    Name = "BGAvatarTOP5",
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#969696E5")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7239819 0.3750011",
                            AnchorMax = "0.8110968 0.530243"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "AvatarTOP5",
                    Parent = "BGAvatarTOP5",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Png = GetImg(covalence.Players.FindPlayerById(infopl5.SteamID.ToString()).Id)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.018 0.019",
                            AnchorMax = "0.973 0.973"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "StateTXT5",
                    Parent = "AvatarTOP5",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#C7C7C7E5"),
                            FontSize = 14,
                            Text = "#5"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.7891649 0.01617118",
                            AnchorMax = "0.9817026 0.1706513"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "Sprite5",
                    Parent = "AvatarTOP5",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#000000AA"),
                            Sprite = radial
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "0.99 0.99"
                        }
                    }
                });

                GUI.Add(new CuiElement
                {
                    Name = "NickTOP5",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 20,
                            Text = infopl5.Name
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.6934386 0.325601",
                            AnchorMax = "0.8427621 0.3719733"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
                if (config.ZealStatistics.info == true)
                {
                    GUI.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "infopl " + infopl5.SteamID,
                            Color = HexToRustFormat("#0000006A"),
                            Sprite = radial
                        },
                        Text =
                        {
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 10,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            Text = "ИНФО"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.7136346 0.8636246",
                            AnchorMax = "0.9863615 0.9863536"
                        }
                    }, "AvatarTOP5", "InfoPlayer5");
                }

                #endregion
            }

            CuiHelper.AddUi(player, GUI);
        }

        void DrawFilters(BasePlayer player)
        {
            CuiElementContainer GUI = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "ShowFilters");
            GUI.Add(new CuiElement
            {
                Name = "BoxFilters",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.007291671 0.6638889",
                        AnchorMax = "0.159375 0.9898147"
                    }
                }
            });

            GUI.Add(new CuiButton
            {
                Button =
                {
                    Command = " ",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Align = TextAnchor.MiddleLeft,
                    Color = HexToRustFormat("#8E8E8EFF"),
                    FontSize = 16,
                    Text = "<size=15>ОТСОРТИРОВАТЬ ПО</size> ▼"
                },
                RectTransform =
                {
                    AnchorMin = "0.01369863 0.9011364",
                    AnchorMax = "0.9863014 0.9886364"
                }
            }, "BoxFilters", "ShowFilters");

            if (filter != false)
            {
                foreach (var Filter in Filters)
                {
                    CuiHelper.DestroyUi(player, "Filter" + Filter.Number);
                }

                filter = false;
            }
            else
            {
                int y = 0;
                foreach (var Filter in Filters)
                {
                    GUI.Add(new CuiButton
                        {
                            Button =
                            {
                                Command = "filter " + Filter.Number,
                                Color = "0 0 0 0"
                            },
                            Text =
                            {
                                Align = TextAnchor.MiddleLeft,
                                Color = HexToRustFormat("#8E8E8EFF"),
                                FontSize = 14,
                                Text = $"{Filter.Name}",
                                Font = "robotocondensed-regular.ttf",
                                FadeIn = 0.1f + (y * 0.1f)
                            },
                            RectTransform =
                            {
                                AnchorMin = $"0.01369863 {0.8022727 - (y * 0.08)}",
                                AnchorMax = $"0.9863014 {0.8897727 - (y * 0.08)}"
                            },
                            FadeOut = 0.9f - (y * 0.1f)
                        }, "BoxFilters", "Filter" + y);
                    y++;
                }

                filter = true;


                CuiHelper.AddUi(player, GUI);
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!player.userID.IsSteamId()) return;
            if (DataBase.IgnorePlayers.Contains(player.userID)) return;
            CheckDataBase(player);
            int timeplayed = Convert.ToInt32(player.Connection.GetSecondsConnected());
            if (timeplayed < 0)
            {
                timeplayed = 0;
                timeplayed = 0;
            }

            DataBase.StatisticDB[player.userID].TimePlayed += timeplayed;
            SaveData();
        }

        void OnServerInitialized()
        {
            if (ImageLibrary == null)
            {
                PrintError($"На сервере не установлен плагин [ImageLibrary]");
                Interface.Oxide.UnloadPlugin(Title);
                return;
            }

            Puts(" ");
            Puts("----------------------Контакты----------------------");
            Puts(" ");
            Puts(" Вконтакте : vk.com/kira_22001");
            Puts(" Discord : -Kira#1920");
            Puts(" Группа вконтакте : vk.com/skyeyeplugins");
            Puts(" ");
            Puts("---^-^----Приятного пользования----^-^---");
            Puts(" ");
            
            LoadData();
            
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!DataBase.StatisticDB.ContainsKey(player.userID))
                {
                    if (!player.userID.IsSteamId()) return;
                    AddPlayer(player);
                    Puts($"Добавлен игрок в базу {player.displayName}");
                }
            }
        }

        private void Unload()
        {
            SaveData();
        }

        private void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || player.IsNpc)
                return;
            if (!player.userID.IsSteamId()) return;
            if (DataBase.IgnorePlayers.Contains(player.userID)) return;
            CheckDataBase(player);
            var Dictinory = DataBase.StatisticDB[player.userID];
            if (info.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
                Dictinory.Death++;
            else
            {
                Dictinory.Death++;
                var attacker = info.InitiatorPlayer;
                if (attacker == null || attacker.IsNpc)
                    return;
                if (DataBase.IgnorePlayers.Contains(player.userID)) return;
                CheckDataBase(attacker);
                var AttackerDictinory = DataBase.StatisticDB[attacker.userID];
                AttackerDictinory.Kills++;
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                LastDamagePlayer = info.Initiator.ToPlayer().userID;
            if (entity is BradleyAPC && info.Initiator is BasePlayer)
                LastDamagePlayer = info.Initiator.ToPlayer().userID;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (hitinfo?.Initiator is BasePlayer)
            {
                var player = hitinfo.Initiator as BasePlayer;
                if (!player.userID.IsSteamId()) return;

                if (player.userID.IsSteamId() && !(player is NPCPlayer) && !(player is HTNPlayer))
                {
                    if (DataBase.IgnorePlayers.Contains(player.userID)) return;
                    CheckDataBase(player);
                    if (entity.name.Contains("agents/"))
                        switch (entity.ShortPrefabName)
                        {
                            case "bear":
                                DataBase.StatisticDB[player.userID].AnimalKills++;
                                break;
                            case "boar":
                                DataBase.StatisticDB[player.userID].AnimalKills++;
                                break;
                            case "chicken":
                                DataBase.StatisticDB[player.userID].AnimalKills++;
                                break;
                            case "horse":
                                DataBase.StatisticDB[player.userID].AnimalKills++;
                                break;
                            case "stag":
                                DataBase.StatisticDB[player.userID].AnimalKills++;
                                break;
                            case "wolf":
                                DataBase.StatisticDB[player.userID].AnimalKills++;
                                break;
                        }
                }
            }

            if (entity is BradleyAPC)
            {
                BasePlayer player;
                player = BasePlayer.FindByID(LastDamagePlayer);
                if (DataBase.IgnorePlayers.Contains(player.userID)) return;
                CheckDataBase(player);
                DataBase.StatisticDB[player.userID].BradleyKills++;
            }

            if (entity is BaseHelicopter)
            {
                BasePlayer player;
                player = BasePlayer.FindByID(LastDamagePlayer);
                if (DataBase.IgnorePlayers.Contains(player.userID)) return;
                CheckDataBase(player);
                DataBase.StatisticDB[player.userID].HeliKills++;
            }
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (DataBase.IgnorePlayers.Contains(player.userID)) return;
            CheckDataBase(player);
            switch (item.info.shortname)
            {
                case "stones":
                    DataBase.StatisticDB[player.userID].Stones += item.amount;
                    break;
                case "wood":
                    DataBase.StatisticDB[player.userID].Wood += item.amount;
                    break;
                case "metal.ore":
                    DataBase.StatisticDB[player.userID].MetalOre += item.amount;
                    break;
                case "sulfur.ore":
                    DataBase.StatisticDB[player.userID].Sulfur += item.amount;
                    break;
            }
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) =>
            OnDispenserGather(dispenser, entity, item);

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (DataBase.IgnorePlayers.Contains(player.userID)) return;
            CheckDataBase(player);
            switch (item.info.shortname)
            {
                case "stones":
                    DataBase.StatisticDB[player.userID].Stones += item.amount;
                    break;
                case "wood":
                    DataBase.StatisticDB[player.userID].Wood += item.amount;
                    break;
                case "metal.ore":
                    DataBase.StatisticDB[player.userID].MetalOre += item.amount;
                    break;
                case "sulfur.ore":
                    DataBase.StatisticDB[player.userID].Sulfur += item.amount;
                    break;
            }
        }

        #endregion

        #region [ChatCommand] / [Чат команды]

        [ChatCommand("stats")]
        private void MainGuiStatistics(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BoxStatistics");
            MainGui(player);
        }

        [ConsoleCommand("stats.manager")]
        private void ManagerPlayerStats(ConsoleSystem.Arg args)
        {
            if (!args.IsAdmin) return;
            PlayerList(args.Player());
        }

        [ConsoleCommand("zstats.ban")]
        private void AddIgnorePlayer(ConsoleSystem.Arg args)
        {
            if (!args.IsAdmin) return;
            var player = BasePlayer.FindByID(Convert.ToUInt64(args.Args[0]));
            CuiHelper.DestroyUi(args.Player(), Layer);
            if (!DataBase.IgnorePlayers.Contains(player.userID))
            {
                DataBase.IgnorePlayers.Add(player.userID);
                SendReply(args.Player(), $"Вы заблокировали сбор статистики игроку : {player.displayName}");
            }
            else
            {
                DataBase.IgnorePlayers.Remove(player.userID);
                SendReply(args.Player(), $"Вы разблокировали сбор статистики игроку : {player.displayName}");
            }

            SaveData();
        }

        [ConsoleCommand("servertop")]
        private void ServerGUITOP(ConsoleSystem.Arg args)
        {
            if (args.Player() == null)
            {
                return;
            }

            var player = args.Player();

            ServerTOP(player, 1);
            DrawFilters(player);
        }

        [ConsoleCommand("filter")]
        private void FilterTOP(ConsoleSystem.Arg args)
        {
            string msg = args.Args[0];

            int filternum = Convert.ToInt32(msg);
            var initiator = args.Player();
            ServerTOP(initiator, filternum);
        }

        [ConsoleCommand("backtomain")]
        private void BackMain(ConsoleSystem.Arg args)
        {
            if (args.Player() == null)
            {
                return;
            }

            var player = args.Player();
            CuiHelper.DestroyUi(player, "BoxStatistics");
            MainGui(player);
        }

        public bool filter = false;

        [ConsoleCommand("showfilter")]
        private void FilterList(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            DrawFilters(player);
        }

        [ConsoleCommand("infopl")]
        private void infoplayer(ConsoleSystem.Arg args)
        {
            if (config.ZealStatistics.info == true)
            {
                string msg = args.Args[0];

                ulong findpl = Convert.ToUInt64(msg);
                var initiator = args.Player();

                DestroyMainGUI(initiator);
                DrawFilters(initiator);
                DestroyServerGUI(initiator);

                InfoPlayer(findpl, initiator.userID);
            }
        }

        [ConsoleCommand("close.stats")]
        private void CloseMainGuiStatistics(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
            {
                var player = args.Player();
                CuiHelper.DestroyUi(player, "BoxStatistics");
            }
        }

        #endregion

        #region [DataBase] / [Хранение данных]

        public class StoredData
        {
            public Dictionary<ulong, StatisticDB> StatisticDB = new Dictionary<ulong, StatisticDB>();
            public List<ulong> IgnorePlayers = new List<ulong>();
        }


        public class StatisticDB
        {
            public string Name;
            public ulong SteamID;
            public int Kills;
            public int Death;
            public int AnimalKills;
            public int BradleyKills;
            public int HeliKills;
            public int Sulfur;
            public int Stones;
            public int MetalOre;
            public int Wood;
            public int TimePlayed;

            public StatisticDB()
            {
            }
        }

        [HookMethod("SaveData")]
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                DataBase = new StoredData();
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        [HookMethod("GetPlayerTimePlayed")]
        public string GetPlayerTimePlayed(int time)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(time);
            var Days = timeSpan.Days;
            var Hours = timeSpan.Hours;
            var Minuts = timeSpan.Minutes;
            return string.Format("{0} Дней - {1} Часов - {2} Минут", Days, Hours, Minuts);
        }

        [HookMethod("AddPlayer")]
        void AddPlayer(BasePlayer player)
        {
            var data = new StatisticDB
            {
                Name = player.displayName,
                SteamID = player.userID,
                Kills = 0,
                AnimalKills = 0,
                BradleyKills = 0,
                Death = 0,
                HeliKills = 0,
                Sulfur = 0,
                Stones = 0,
                MetalOre = 0,
                Wood = 0
            };

            DataBase.StatisticDB.Add(player.userID, data);
            SaveData();
        }

        [HookMethod("CheckDataBase")]
        void CheckDataBase(BasePlayer player)
        {
            if (!DataBase.StatisticDB.ContainsKey(player.userID)) AddPlayer(player);
        }

        void DestroyMainGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer + "ServerName");
            CuiHelper.DestroyUi(player, Layer + "AvatarBG");
            CuiHelper.DestroyUi(player, Layer + "Avatar");
            CuiHelper.DestroyUi(player, Layer + "Nick");
            CuiHelper.DestroyUi(player, Layer + "SteamID");
            CuiHelper.DestroyUi(player, Layer + "Line1");
            CuiHelper.DestroyUi(player, "BoxValue1");
            CuiHelper.DestroyUi(player, "BoxValue2");
            CuiHelper.DestroyUi(player, "BoxValue3");
            CuiHelper.DestroyUi(player, "BoxValue4");
            CuiHelper.DestroyUi(player, "BoxValue5");
            CuiHelper.DestroyUi(player, "BoxValue6");
            CuiHelper.DestroyUi(player, "BoxValue7");
            CuiHelper.DestroyUi(player, "BoxValue8");
            CuiHelper.DestroyUi(player, "BoxValue9");
            CuiHelper.DestroyUi(player, "BoxValue10");
        }

        void DestroyServerGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "GoMainGui");
            CuiHelper.DestroyUi(player, "FILTRES");
            CuiHelper.DestroyUi(player, "GoTop");
            CuiHelper.DestroyUi(player, "ZagServerTOP");
            CuiHelper.DestroyUi(player, "BGAvatarTOP1");
            CuiHelper.DestroyUi(player, "BGAvatarTOP2");
            CuiHelper.DestroyUi(player, "BGAvatarTOP3");
            CuiHelper.DestroyUi(player, "BGAvatarTOP4");
            CuiHelper.DestroyUi(player, "BGAvatarTOP5");
            CuiHelper.DestroyUi(player, "NickTOP1");
            CuiHelper.DestroyUi(player, "NickTOP2");
            CuiHelper.DestroyUi(player, "NickTOP3");
            CuiHelper.DestroyUi(player, "NickTOP4");
            CuiHelper.DestroyUi(player, "NickTOP5");
            CuiHelper.DestroyUi(player, "NickTOP1");
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

        #endregion
    }
}