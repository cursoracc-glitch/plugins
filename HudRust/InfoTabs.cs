using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("InfoTabs", "VooDoo", "1.0.0")]
    [Description("Rules and Commands for XMenu")]
    public class InfoTabs : RustPlugin
    {
        [PluginReference] Plugin XMenu;


        #region Config
        private PluginConfig config;
        private class PluginConfig
        {
            public ColorConfig colorConfig;
            public class ColorConfig
            {
                public string menuContentHighlighting;
                public string menuContentHighlightingalternative;

                public string menuContentText;
                public string menuContentTextAlternative;

                public string gradientColor;
            }

            public Dictionary<string, string> commandsTab;
            public Dictionary<string, string> bindsTab;

            public List<string> rulesTab;
        }

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                colorConfig = new PluginConfig.ColorConfig()
                {
                    menuContentHighlighting = "#0000007f",
                    menuContentHighlightingalternative = "#FFFFFF10",
                    menuContentTextAlternative = "#90BD47",
                    menuContentText = "#FFFFFFAA",
                    gradientColor = "#00000099",
                },
                commandsTab = new Dictionary<string, string>()
                {
                    ["custommenu"] = "Открыть это меню",
                },
                bindsTab = new Dictionary<string, string>()
                {
                    ["bind z custommenu"] = "Открыть это меню",
                },
                rulesTab = new List<string>()
                {
                    "Текст",
                },
            };
        }
        #endregion

        #region Layers
        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";
        #endregion

        Timer TimerInitialize;
        private void OnServerInitialized()
        {
            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterSubMenu", this.Name, "Main", "Правила", "RenderRules", null);
                    XMenu.Call("API_RegisterSubMenu", this.Name, "Main", "Команды", "RenderCommands", null);
                    XMenu.Call("API_RegisterSubMenu", this.Name, "Main", "Бинды", "RenderBinds", null);

                    int RulesID = (int)XMenu.Call("API_GetSubMenuID", "Main", "Информация");
                    cmd.AddChatCommand("help", this, (p, cmd, args) => rust.RunClientCommand(p, $"custommenu true Main {RulesID}"));

                    TimerInitialize.Destroy();
                }
            });
        }

        private void RenderCommands(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];

            Container.Add(new CuiElement
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
                            OffsetMin = "-215 -230",
                            OffsetMax = "500 270"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info",
                Parent = MenuContent,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "80 -460",
                            OffsetMax = "630 -10"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Title",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color={config.colorConfig.menuContentTextAlternative}>Команда</color>",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 24,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.9",
                                AnchorMax = "0.975 0.975",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Title",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color={config.colorConfig.menuContentText}>Описание</color>",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 24,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.4 0.9",
                                AnchorMax = "0.95 0.975",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });

            string Commands = $"<color={config.colorConfig.menuContentTextAlternative}>";
            string Info = $"<color={config.colorConfig.menuContentText}>";
            for (int i = 0, x = 0; i < config.commandsTab.Count; i++)
            {
                if(Page * 27 > i)
                    continue;

                if (x > 26)
                    continue;

                Commands += $"{config.commandsTab.ElementAt(i).Key}\n";
                Info += $"{config.commandsTab.ElementAt(i).Value}\n";

                x++;
            }

            Commands += "</color>";
            Info += "</color>";

            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Text",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = Commands,
                                Align = TextAnchor.UpperLeft,
                                FontSize = 12,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0",
                                AnchorMax = "0.375 0.88",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Text",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = Info,
                                Align = TextAnchor.UpperLeft,
                                FontSize = 12,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.4 0",
                                AnchorMax = "0.95 0.88",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });

            if ((int)Page > 0)
            {
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".BackTitle",
                    Parent = MenuContent,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "80 -500",
                            OffsetMax = "110 -470"
                        }
                    }
                });
                Container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"custommenu false Main {ID} {(int)Page - 1}", },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                }, MenuContent + ".BackTitle", MenuContent + ".BackTitleBtn");
            }
            if ((int)(Page * 27) + 27 < config.commandsTab.Count)
            {
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".NextTitle",
                    Parent = MenuContent,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "600 -500",
                            OffsetMax = "630 -470"
                        }
                    }
                });
                Container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"custommenu false Main {ID} {(int)Page + 1}", },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                }, MenuContent + ".NextTitle", MenuContent + ".NextTitleBtn");
            }
        }

        private void RenderBinds(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];

            Container.Add(new CuiElement
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
                            OffsetMin = "-215 -230",
                            OffsetMax = "500 270"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info",
                Parent = MenuContent,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "80 -460",
                            OffsetMax = "630 -10"
                        }
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Title",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color={config.colorConfig.menuContentTextAlternative}>Бинд</color>",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 24,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.9",
                                AnchorMax = "0.975 0.975",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Title",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color={config.colorConfig.menuContentText}>Описание</color>",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 24,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.4 0.9",
                                AnchorMax = "0.95 0.975",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });

            string Commands = $"<color={config.colorConfig.menuContentTextAlternative}>";
            string Info = $"<color={config.colorConfig.menuContentText}>";
            for (int i = 0, x = 0; i < config.bindsTab.Count; i++)
            {
                if (Page * 27 > i)
                    continue;

                if (x > 26)
                    continue;

                Commands += $"{config.bindsTab.ElementAt(i).Key}\n";
                Info += $"{config.bindsTab.ElementAt(i).Value}\n";

                x++;
            }

            Commands += "</color>";
            Info += "</color>";

            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Text",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = Commands,
                                Align = TextAnchor.UpperLeft,
                                FontSize = 12,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0",
                                AnchorMax = "0.375 0.88",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Text",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = Info,
                                Align = TextAnchor.UpperLeft,
                                FontSize = 12,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.4 0",
                                AnchorMax = "0.95 0.88",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });

            if ((int)Page > 0)
            {
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".BackTitle",
                    Parent = MenuContent,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "80 -500",
                            OffsetMax = "110 -470"
                        }
                    }
                });
                Container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"custommenu false Main {ID} {(int)Page - 1}", },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                }, MenuContent + ".BackTitle", MenuContent + ".BackTitleBtn");
            }
            if ((int)(Page * 27) + 27 < config.bindsTab.Count)
            {
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".NextTitle",
                    Parent = MenuContent,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "600 -550",
                            OffsetMax = "630 -520"
                        }
                    }
                });
                Container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"custommenu false Main {ID} {(int)Page + 1}", },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                }, MenuContent + ".NextTitle", MenuContent + ".NextTitleBtn");
            }
        }

        private void RenderRules(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];

            Container.Add(new CuiElement
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
                            OffsetMin = "-215 -230",
                            OffsetMax = "500 270"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info",
                Parent = MenuContent,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "80 -460",
                            OffsetMax = "630 -10"
                        }
                    }
            });

            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Title",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color={config.colorConfig.menuContentText}>Информация и правила | Страница №{Page+1}</color>",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 24,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0.9",
                                AnchorMax = "0.95 0.975",
                            }
                        }
            });

            string Info = $"<color={config.colorConfig.menuContentText}>";
            for (int i = 0, x = 0; i < config.rulesTab.Count; i++)
            {
                if (Page * 27 > i)
                    continue;

                if (x > 26)
                    continue;

                Info += $"{config.rulesTab.ElementAt(i)}\n";

                x++;
            }
            Info += "</color>";

            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + ".Text",
                Parent = MenuContent + ".Info",
                Components =
                        {
                            new CuiTextComponent
                            {
                                Text = Info,
                                Align = TextAnchor.UpperLeft,
                                FontSize = 12,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0",
                                AnchorMax = "0.95 0.88",
                            }
                        }
            });

            if ((int)Page > 0)
            {
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".BackTitle",
                    Parent = MenuContent,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "80 -500",
                            OffsetMax = "110 -470"
                        }
                    }
                });
                Container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"custommenu false Main {ID} {(int)Page - 1}", },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                }, MenuContent + ".BackTitle", MenuContent + ".BackTitleBtn");
            }
            if ((int)(Page * 27) + 27 < config.rulesTab.Count)
            {
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".NextTitle",
                    Parent = MenuContent,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "600 -500",
                            OffsetMax = "630 -470"
                        }
                    }
                });
                Container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"custommenu false Main {ID} {(int)Page + 1}", },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                }, MenuContent + ".NextTitle", MenuContent + ".NextTitleBtn");
            }
        }




        #region Utils
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
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

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
        #endregion
    }
}