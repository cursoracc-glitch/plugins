#define DEBUG

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("XMenu", "VooDoo", "1.0.0")]
    [Description("C# Constructor menu")]

    public class XMenu : RustPlugin
    {
        private static XMenu instance;

        #region ImageLibrary Addon
        [PluginReference] Plugin ImageLibrary;
        bool AddImage(string url, string imageName, ulong imageId, Action callback = null) => (bool)ImageLibrary.Call("AddImage", url, imageName, imageId, callback);
        string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false) => (string)ImageLibrary.Call("GetImage", imageName, imageId, returnUrl);
        #endregion

        #region Menu
        private List<MenuItem> MenuItems = new List<MenuItem>();
        public class MenuItem
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public List<SubMenuItem> Items { get; set; }
            public PluginResponse PluginResponse { get; set; }

            public MenuItem(string PluginName, string Name, string Path, string Command = "", CuiElementContainer Container = null)
            {
                this.Name = Name;
                this.Path = Path;
                this.Items = new List<SubMenuItem>();

                if (!string.IsNullOrEmpty(Command) || Container != null)
                    this.PluginResponse = new PluginResponse(PluginName, Command, Container);

                instance.MenuItems.Add(this);
            }
        }

        public class SubMenuItem
        {
            public string Title { get; set; }
            public MenuItem Parent { get; set; }
            public PluginResponse PluginResponse { get; set; }

            public SubMenuItem(string PluginName, string Name, string Title, string Command = "", CuiElementContainer Container = null)
            {
                this.Parent = instance.MenuItems.Where(x => x.Name == Name).FirstOrDefault();
                if (Parent != null)
                {
                    this.Title = Title;
                    if (!string.IsNullOrEmpty(Command) || Container != null)
                        this.PluginResponse = new PluginResponse(PluginName, Command, Container);

                    Parent.Items.Add(this);
                }
            }
        }

        public class PluginResponse
        {
            private Plugin Plugin;
            private CuiElementContainer Container;
            private string Command;

            public PluginResponse(string PluginName, string Command, CuiElementContainer Container)
            {
                this.Plugin = string.IsNullOrEmpty(PluginName) ? null : instance.Manager.GetPlugin(PluginName);
                this.Command = Command;
                this.Container = Container;
            }

            public CuiElementContainer GetContainer() => Container;
            public void PluginCall(ulong userID, params object[] args) => Plugin.Call(Command, userID, args);
            public bool IsContainer => Container != null && Container.Count > 0;
            public bool IsCommand => !string.IsNullOrEmpty(Command);
        }
        #endregion

        #region Config
        private PluginConfig config;
        private class PluginConfig
        {
            public ColorConfig colorConfig;
            public class ColorConfig
            {
                public string outlineColor;
                public string backgroundColor;
                public string menuItemsColor;
                public string subMenuItemsColor;
                public string subMenuSelectedItemsColor;
                public string subMenuItemsTextColor;
                public string menuContentHighlighting;
                public string menuContentHighlightingalternative;

                public string gradientColor;
            }

            public string welcomeMsg;
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
                    outlineColor = "#2C2E29D9",
                    backgroundColor = "#595A21FF",
                    menuItemsColor = "#FFFFFAD9",
                    subMenuItemsColor = "#00000000",
                    subMenuSelectedItemsColor = "#00000010",
                    subMenuItemsTextColor = "#FFFFFF99",
                    menuContentHighlighting = "#00000033",
                    menuContentHighlightingalternative = "#00000040",

                    gradientColor = "#000000E6",
                },
                welcomeMsg = $""
            };
        }
        #endregion

        #region Layers
        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";
        #endregion

        #region RenderTemplate
        public void Render(CuiElementContainer Container)
        {
            #region Close
            Container.Add(new CuiElement
            {
                Name = MenuLayer,
                Parent = "Overlay",
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            Sprite = "assets/content/ui/ui.background.tiletex.psd",
                            Material = "assets/content/ui/uibackgroundblur.mat",
                            Color = "0 0 0 0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                        },
                        new CuiNeedsCursorComponent()
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuLayer + ".Close",
                Parent = MenuLayer,
                Components =
                    {
                        new CuiButtonComponent
                        {
                            Color = "0 0 0 0",
                            Close = MenuLayer
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                    }
            });
            #endregion

            Container.Add(new CuiElement
            {
                Name = MenuLayer + ".Outline",
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.outlineColor),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-510 -250",
                            OffsetMax = "510 290"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuLayer + ".Background",
                Parent = MenuLayer,
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            Sprite = "assets/content/textures/generic/background/background.bmp"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-500 -240",
                            OffsetMax = "500 280"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuLayer + ".Line",
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.outlineColor),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-450 -240",
                            OffsetMax = "-440 280"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuLayer + ".Gradient",
                Parent = MenuLayer,
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.gradientColor),
                            Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-510 -250",
                            OffsetMax = "510 290"
                        }
                    }
            });
        }
        #endregion

        #region Menu
        public void RenderMenu(CuiElementContainer Container)
        {
            Container.Add(new CuiElement
            {
                Name = MenuItemsLayer,
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
                            OffsetMin = "-500 -240",
                            OffsetMax = "-450 280"
                        }
                    }
            });

            for (int i = 0; i < instance.MenuItems.Count; i++)
            {
                if (instance.MenuItems[i].Path.StartsWith("assets/"))
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuItemsLayer + $".{i}",
                        Parent = MenuItemsLayer,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat(config.colorConfig.menuItemsColor),
                                Sprite = instance.MenuItems[i].Path,
                                Material = "assets/icons/iconmaterial.mat",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"5 {-45 - i * 50}",
                                OffsetMax = $"45 {-5 - i * 50}"
                            },
                            new CuiOutlineComponent
                            {
                                Color = "0 0 0 1",
                                Distance = "-1 1"
                            }
                        }
                    });
                }
                else
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuItemsLayer + $".{i}",
                        Parent = MenuItemsLayer,
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(config.colorConfig.menuItemsColor),
                                Png = GetImage(instance.MenuItems[i].Path),
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"5 {-45 - i * 50}",
                                OffsetMax = $"45 {-5 - i * 50}"
                            },
                            new CuiOutlineComponent
                            {
                                Color = "0 0 0 1",
                                Distance = "-1 1"
                            }
                        }
                    });
                }
                Container.Add(new CuiElement
                {
                    Name = MenuItemsLayer + $".{i}",
                    Parent = MenuItemsLayer,
                    Components =
                        {
                            new CuiButtonComponent
                            {
                                Color = "0 0 0 0",
                                Command = $"custommenu false {instance.MenuItems.ElementAt(i).Name} 0"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 1",
                                AnchorMax = $"0 1",
                                OffsetMin = $"5 {-45 - i * 50}",
                                OffsetMax = $"45 {-5 - i * 50}"
                            },
                        }
                });
            }
        }
        #endregion

        #region SubMenu
        public void RenderSubMenu(CuiElementContainer Container, MenuItem menuItem, int selectedMenu)
        {
            Container.Add(new CuiElement
            {
                Name = MenuSubItemsLayer,
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
                            OffsetMin = "-400 -240",
                            OffsetMax = "-295 230"
                        },
                    }
            });

            for (int i = 0; i < menuItem.Items.Count; i++)
            {
                if (selectedMenu == i)
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuSubItemsLayer + ".TitleGradient",
                        Parent = MenuSubItemsLayer,
                        Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = HexToRustFormat(config.colorConfig.subMenuSelectedItemsColor),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1.05",
                                    AnchorMax = "0 1.05",
                                    OffsetMin = $"0 {(50 + i * 45) * -1}",
                                    OffsetMax = $"185 {(0 + i * 45) * -1}"
                                }
                            }
                    });
                }

                Container.Add(new CuiElement
                {
                    Name = MenuSubItemsLayer + $".MenuLabel.{i}",
                    Parent = MenuSubItemsLayer,
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color={config.colorConfig.subMenuItemsTextColor}>{menuItem.Items[i].Title}</color>",
                                Align = TextAnchor.MiddleRight,
                                FontSize = 24,
                                Font = "robotocondensed-bold.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1.05",
                                AnchorMax = "0 1.05",
                                OffsetMin = $"0 {(50 + i * 45) * -1}",
                                OffsetMax = $"175 {(0 + i * 45) * -1}"
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
                });

                if (selectedMenu != i)
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuSubItemsLayer + $".Button_{i}",
                        Parent = MenuSubItemsLayer,
                        Components =
                        {
                            new CuiButtonComponent
                            {
                                Color = HexToRustFormat(config.colorConfig.subMenuItemsColor),
                                Command = $"custommenu false {menuItem.Name} {i}"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1.05",
                                AnchorMax = "0 1.05",
                                OffsetMin = $"0 {(50 + i * 45) * -1}",
                                OffsetMax = $"185 {(0 + i * 45) * -1}"
                            },
                        }
                    });
                }
            }
        }
        #endregion

        #region Main Page
        public CuiElementContainer JSON_MainPage()
        {
            CuiElementContainer Container = new CuiElementContainer();
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
                            OffsetMin = "-215 -240",
                            OffsetMax = "500 280"
                        },
                    }
            });
/*
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Logo",
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
                            OffsetMin = "80 -187.5",
                            OffsetMax = "630 -50"
                        }
                    }
            });*/
           /* Container.Add(new CuiElement
            {
                Name = MenuContent + ".Logo" + ".Img",
                Parent = MenuContent + ".Logo",
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Sprite = "assets/content/ui/menuui/rustlogo-blurred.png",
                            FadeIn = 0.5f,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                        }
                    }
            });*/
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
                            OffsetMin = "80 -187.5",
                            OffsetMax = "630 -50"
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
                                Text = $"<color=#fff9f9AA>ДОБРО ПОЖАЛОВАТЬ НА СЕРВЕР\n<size=58><b>RADIANT RUST</b></size>\nУдачной игры!",
                                Align = TextAnchor.UpperCenter,
                                FontSize = 20,
                                Font = "robotocondensed-regular.ttf",
                                FadeIn = 0.5f,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.025 0.025",
                                AnchorMax = "0.975 0.975",
                            },
                            new CuiOutlineComponent()
                            {
                                 Color = "0 0 0 1",
                                 Distance = "0.5 -0.5"
                            }
                        }
            });
            return Container;
        }
        #endregion

        #region CMD
        [ConsoleCommand("custommenuclose")]
        void CmdClose(ConsoleSystem.Arg arg)
        {
            CuiHelper.DestroyUi(arg.Player(), MenuLayer);
        }

        [ConsoleCommand("custommenu")]
        void CmdShow(ConsoleSystem.Arg arg)
        {
            try
            {
                CuiElementContainer Container = new CuiElementContainer();

                bool FullRender = true;
                string Name = MenuItems[0].Name;
                int ID = 0;
                int Page = 0;
                string Args = string.Empty;

                if (arg.HasArgs(1)) FullRender = bool.Parse(arg.Args[0]);
                if (arg.HasArgs(2)) Name = arg.Args[1];
                if (arg.HasArgs(3)) ID = int.Parse(arg.Args[2]);
                if (arg.HasArgs(4)) Page = int.Parse(arg.Args[3]);
               // if (arg.HasArgs(5)) Args = string.Join(" ", arg.Args.Skip(4));





                if (FullRender)
                {
                    CuiHelper.DestroyUi(arg.Player(), MenuLayer);

                    Render(Container);
                    RenderMenu(Container);
                }
                else
                {
                    CuiHelper.DestroyUi(arg.Player(), MenuSubItemsLayer);
                    CuiHelper.DestroyUi(arg.Player(), MenuContent);
                }

                MenuItem menuItem = MenuItems.Where(x => x.Name == Name).FirstOrDefault();

                if (menuItem.PluginResponse == null)
                    RenderSubMenu(Container, menuItem, ID);


                if (menuItem.Items.Count > 0)
                {
                    if (menuItem.Items[ID].PluginResponse.IsContainer)
                        Container.AddRange(menuItem.Items[ID].PluginResponse.GetContainer());
                    else
                        menuItem.Items[ID].PluginResponse.PluginCall(arg.Connection.userid, (object)Container, (object)FullRender, (object)Name, (object)ID, (object)Page, (object)Args);
                }
                else
                {
                    if (menuItem.PluginResponse.IsContainer)
                        Container.AddRange(menuItem.PluginResponse.GetContainer());
                    else
                        menuItem.PluginResponse.PluginCall(arg.Connection.userid, (object)Container, (object)FullRender, (object)Name, (object)ID, (object)Page, (object)Args);
                }

                CuiHelper.AddUi(arg.Player(), Container);
            }
            catch(Exception ex)
            {
                Puts(ex.ToString());
            }
        }
        #endregion

        #region uModHook's

        private List<string> pluginsQueue = new List<string>();
        void OnServerInitialized()
        {
            instance = this;

            new MenuItem(this.Name, "Main", "assets/icons/gear.png");
            new SubMenuItem(this.Name, "Main", "Главная", "", JSON_MainPage());
            cmd.AddChatCommand("menu", this, (p, cmd, args) => rust.RunClientCommand(p, "custommenu true Main"));

            foreach (var menuItem in MenuItems)
            {
                if (!menuItem.Path.StartsWith("assets/"))
                {
                    AddImage(menuItem.Path, menuItem.Path, 0);
                    PrintError(menuItem.Path);
                }
            }

            pluginsQueue = Interface.Oxide.DataFileSystem.ReadObject<List<string>>("PluginsQueue");
            for (int i = 0; i < pluginsQueue.Count; i++)
            {
                int x = i;
                timer.In(1f + 0.5f * i, () => rust.RunServerCommand($"o.reload {pluginsQueue.ElementAt(x)}"));
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null)
                return;

            if (player.IsReceivingSnapshot)
            {
                timer.In(1f, () => OnPlayerConnected(player));
                return;
            }

            rust.RunClientCommand(player, "custommenu");
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, MenuLayer);

            Interface.Oxide.DataFileSystem.WriteObject<List<string>>("PluginsQueue", pluginsQueue);
        }
        #endregion

        #region API
        void API_RegisterMenu(string PluginName, string Name, string Path, string Command = "", object Container = null)
        {
            if (instance.MenuItems.Where(x => x.Name == Name).Count() > 0)
                instance.MenuItems.Remove(instance.MenuItems.Where(x => x.Name == Name).FirstOrDefault());

            new MenuItem(PluginName, Name, Path, Command, (Container != null) ? (Container as CuiElementContainer) : null);

            if (!pluginsQueue.Contains(PluginName))
                pluginsQueue.Add(PluginName);
        }
        void API_RegisterSubMenu(string PluginName, string Name, string Title, string Command = "", object Container = null)
        {
            if (instance.MenuItems.Where(x => x.Name == Name).Count() > 0)
            {
                if (instance.MenuItems.Where(x => x.Name == Name).FirstOrDefault().Items.Where(x => x.Title == Title).Count() > 0)
                    instance.MenuItems.Where(x => x.Name == Name).FirstOrDefault().Items.Remove(instance.MenuItems.Where(x => x.Name == Name).FirstOrDefault().Items.Where(x => x.Title == Title).FirstOrDefault());

                new SubMenuItem(PluginName, Name, Title, Command, ((Container != null) ? (Container as CuiElementContainer) : null));

                if (!pluginsQueue.Contains(PluginName))
                    pluginsQueue.Add(PluginName);
            }
        }
        int API_GetSubMenuID(string Name, string Title) => MenuItems.Where(x => x.Name == Name).FirstOrDefault().Items.IndexOf(MenuItems.Where(x => x.Name == Name).FirstOrDefault().Items.Where(z => z.Title == Title).FirstOrDefault());
        #endregion

        #region Utils
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