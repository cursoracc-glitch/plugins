using System.Reflection.Metadata;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Oxide.Core;
using UnityEngine.UIElements;

namespace Oxide.Plugins
{
    [Info("MagixLogo", "Frizen", "1.0.0")]

    public class MagixLogo : RustPlugin
    {
        #region Vars
        [PluginReference] private Plugin ImageLibrary, EventRandomizer, NoEscape, FreeOnline, MagixNotify;
        private Dictionary<BasePlayer, bool> _menuOpen = new Dictionary<BasePlayer, bool>();
        public static System.Random Random = new System.Random();
        private float randomhashed = 0;
        private const string _menuLayer = "Magix.Menu";
        private bool _isCargoShip = false;
        private bool _isBradley = false;
        private bool _isCh47 = false;
        private bool _isHeli = false;
        private const float fadeout = 0.25f;
        private const float fadein = 1f;
        #endregion

        #region Config
        private static Configuration _config;
        public class Configuration
        {

            [JsonProperty("Цвет активного ивента Bradley")]
            public string BradleyColor = "#FF6C0AFF";
            [JsonProperty("Цвет активного ивента Ch47")]
            public string Ch47Color = "#FF6C0AFF";
            [JsonProperty("Цвет активного ивента Heli")]
            public string HeliColor = "#FF6C0AFF";
            [JsonProperty("Время уведомления рейдблока в секундах")]
            public float NotifyTime = 5f;
            [JsonProperty("Цвет активного ивента Cargo")]
            public string CargoColor = "#FF6C0AFF";
            [JsonProperty("Список команд в выпадающем меню")]
            public Dictionary<string, string> MenuCmds = new Dictionary<string, string>();
            [JsonProperty("Картинки")]
            public Dictionary<string, string> Imgs = new Dictionary<string, string>();
            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    NotifyTime = 5f,
                    BradleyColor = "#FF6C0AFF",
                    Ch47Color = "#FF6C0AFF",
                    HeliColor = "#FF6C0AFF",
                    CargoColor = "#FF6C0AFF",
                    MenuCmds =
                    {
                        ["https://i.imgur.com/RKbohaU.png"] = "chat.say /report",
                        ["https://i.imgur.com/aAhH5iW.png"] = "chat.say /block",
                        ["https://i.imgur.com/HYoXoOu.png"] = "chat.say /friends",
                        ["https://i.imgur.com/PDvATmn.png"] = "chat.say /tasks"
                    },
                    Imgs =
                    {
                        ["Store"] = "https://i.imgur.com/RLMIvK6.png",
                        ["People"] = "https://i.imgur.com/4UivNDU.png",
                        ["Heli"] = "https://i.imgur.com/VupbypK.png",
                        ["CH47"] = "https://i.imgur.com/bAxVttR.png",
                        ["Bradley"] = "https://i.imgur.com/Yr9L8md.png",
                        ["Cargo"] = "https://i.imgur.com/2WcJOIO.png",
                        ["Menu"] = "https://i.imgur.com/naNbo7w.png",
                        ["RaidBlock"] = "https://i.imgur.com/hM8nGlJ.png",
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => _config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            GetOnline();
            FirstCheckEvents();

            timer.Once(15f, () =>
            {
                Interface.Oxide.ReloadPlugin("EventRandomizer");
            });

            foreach (var img in _config.Imgs)
            {
                ImageLibrary.Call("AddImage", img.Value, img.Key);
            }

            foreach (var img in _config.MenuCmds)
            {
                ImageLibrary.Call("AddImage", img.Key, img.Value);
            }

            foreach (var item in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(item);
            }
        }

        float GetOnline()
        {
            if (FreeOnline) randomhashed = FreeOnline.Call<int>("GetOnline");
            else randomhashed = BasePlayer.activePlayerList.Count;
            
            return randomhashed;
        }

        void OnPlayerSleep(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "CH47");
            CuiHelper.DestroyUi(player, "CH47BTN");
            CuiHelper.DestroyUi(player, "Heli");
            CuiHelper.DestroyUi(player, "HeliBTN");
            CuiHelper.DestroyUi(player, "Cargo");
            CuiHelper.DestroyUi(player, "CargoBTN");
            CuiHelper.DestroyUi(player, "Bradley");
            CuiHelper.DestroyUi(player, "BradleyBTN");
            CuiHelper.DestroyUi(player, "People");
            CuiHelper.DestroyUi(player, "MenuIMG");
            CuiHelper.DestroyUi(player, "MenuBTN");
            CuiHelper.DestroyUi(player, "StoreIMG");
            CuiHelper.DestroyUi(player, "StoreBTN");
            CuiHelper.DestroyUi(player, "Online");
            DestroyButtons(player);
            CuiHelper.DestroyUi(player, "LeftPanelLogo");
            CuiHelper.DestroyUi(player, _menuLayer);

        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            DrawMenu(player);
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            if (!_menuOpen.ContainsKey(player))
            {
                _menuOpen.Add(player, true);
            }

            DrawMenu(player);

            timer.Once(1f, () =>
            {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    if (players.userID == player.userID) continue;
                    bool IsOpen = false;
                    if (_menuOpen.TryGetValue(players, out IsOpen) && IsOpen)
                    {
                        OnlineUi(players);
                    }
                }
            });
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            timer.Once(1f, () =>
            {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    bool IsOpen = false;
                    if (_menuOpen.TryGetValue(players, out IsOpen) && IsOpen)
                    {
                        OnlineUi(players);
                    }
                }
            });
        }


        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (entity is CH47Helicopter)
            {
                _isCh47 = true;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "CH47");
                }
                return;
            }
            if (entity is BaseHelicopter)
            {
                _isHeli = true;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Heli");
                }
                return;
            }
            if (entity is CargoShip)
            {
                _isCargoShip = true;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Cargo");
                }
                return;
            }
            if (entity is BradleyAPC)
            {
                _isBradley = true;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Bradley");
                }
                return;
            }
          
        }

        void OnEntityKill(BaseEntity entity)
        {
            if (entity == null) return;
            if (entity is CH47Helicopter)
            {
                _isCh47 = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "CH47");
                }
                return;
            }
            if (entity is BaseHelicopter)
            {
                _isHeli = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Heli");
                }
                return;
            }
            if (entity is CargoShip)
            {
                _isCargoShip = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Cargo");
                }
                return;
            }
            if (entity is BradleyAPC)
            {
                _isBradley = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Bradley");
                }
                return;
            }
            
        }

        void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (entity == null) return;
            if (entity is CH47Helicopter)
            {
                _isCh47 = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "CH47");
                }
                return;
            }
            if (entity is BaseHelicopter)
            {
                _isHeli = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Heli");
                }
                return;
            }
            if (entity is CargoShip)
            {
                _isCargoShip = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Cargo");
                }
                return;
            }
            if (entity is BradleyAPC)
            {
                _isBradley = false;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    RefreshEvents(player, "Bradley");
                }
                return;
            }
           
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "CH47");
                CuiHelper.DestroyUi(player, "CH47BTN");
                CuiHelper.DestroyUi(player, "Heli");
                CuiHelper.DestroyUi(player, "HeliBTN");
                CuiHelper.DestroyUi(player, "Cargo");
                CuiHelper.DestroyUi(player, "CargoBTN");
                CuiHelper.DestroyUi(player, "Bradley");
                CuiHelper.DestroyUi(player, "BradleyBTN");
                CuiHelper.DestroyUi(player, "People");
                CuiHelper.DestroyUi(player, "MenuIMG");
                CuiHelper.DestroyUi(player, "MenuBTN");
                CuiHelper.DestroyUi(player, "StoreIMG");
                CuiHelper.DestroyUi(player, "StoreBTN");
                CuiHelper.DestroyUi(player, "Online");
                DestroyButtons(player);
                CuiHelper.DestroyUi(player, _menuLayer);
                CuiHelper.DestroyUi(player, "LeftPanelLogo");

            }
        }

        #endregion

        #region Methods
        private void FirstCheckEvents()
        {

            foreach (var entity in BaseEntity.serverEntities)
            {
                if (entity as CargoShip)
                {
                    _isCargoShip = true;
                }
                if (entity as BradleyAPC)
                {
                    _isBradley = true;
                }
                if (entity as CH47Helicopter)
                {
                    _isCh47 = true;
                }
                if (entity as BaseHelicopter)
                {
                    _isHeli = true;
                }
            }
           
        }



        private void DrawMenu(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "CH47");
            CuiHelper.DestroyUi(player, "CH47BTN");
            CuiHelper.DestroyUi(player, "Heli");
            CuiHelper.DestroyUi(player, "HeliBTN");
            CuiHelper.DestroyUi(player, "Cargo");
            CuiHelper.DestroyUi(player, "CargoBTN");
            CuiHelper.DestroyUi(player, "Bradley");
            CuiHelper.DestroyUi(player, "BradleyBTN");
            CuiHelper.DestroyUi(player, "People");
            CuiHelper.DestroyUi(player, "MenuIMG");
            CuiHelper.DestroyUi(player, "MenuBTN");
            CuiHelper.DestroyUi(player, "StoreIMG");
            CuiHelper.DestroyUi(player, "StoreBTN");
            CuiHelper.DestroyUi(player, "Online");
            DestroyButtons(player);
            CuiHelper.DestroyUi(player, _menuLayer);
            CuiHelper.DestroyUi(player, "LeftPanelLogo");


            container.Add(new CuiPanel
            {
                FadeOut = fadeout,
                Image = { Color = $"0 0 0 0", FadeIn = fadein},
                RectTransform = { AnchorMin = $"0.006249996 0.9546296", AnchorMax = $"0.1734375 0.9981481" }
            }, "Overlay", $"LeftPanelLogo");

            container.Add(new CuiPanel
            {
                FadeOut = fadeout,
                Image = { Color = $"0 0 0 0", FadeIn = fadein},
                RectTransform = { AnchorMin = $"0.8322843 0.9546296", AnchorMax = $"0.9994677 0.9981481" }
            }, "Overlay", _menuLayer);


            container.Add(new CuiButton
            {
                FadeOut = fadeout,
                RectTransform =
                        {
                            AnchorMin = $"4.423782E-09 0.1489384",
                            AnchorMax = $"0.09968854 0.8297893"
                        },
                Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"chat.say /store",
                        },
                Text =
                        {
                            Text = ""
                        }
            }, "LeftPanelLogo", $"StoreBTN");

            container.Add(new CuiElement
            {
                Name = "StoreIMG",
                Parent = $"StoreBTN",
                FadeOut = fadeout,
                Components =
                        {
                            new CuiRawImageComponent{Color = "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "Store"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0 0", AnchorMax = "1 1"}
                        }
            });

            container.Add(new CuiButton
            {
                FadeOut = fadeout,
                RectTransform =
                        {
                            AnchorMin = $"0.8664309 0.1489384",
                            AnchorMax = $"0.9661204 0.8297893"
                        },
                Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"magix.logo",
                        },
                Text =
                        {
                            Text = ""
                        }
            }, _menuLayer, $"MenuBTN");

            container.Add(new CuiElement
            {
                Name = "MenuIMG",
                Parent = $"MenuBTN",
                FadeOut = fadeout,
                Components =
                        {
                            new CuiRawImageComponent{Color = "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "Menu"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0 0", AnchorMax = "1 1"}
                        }
            });

            container.Add(new CuiElement
            {
                Name = "People",
                Parent = $"LeftPanelLogo",
                FadeOut = fadeout,
                Components =
                        {
                            new CuiRawImageComponent{Color = "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "People"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.1214953 0.1489384", AnchorMax = "0.2211839 0.82979"}
                        }
            });

            bool IsOpen = false;
            _menuOpen.TryGetValue(player, out IsOpen);
            CuiHelper.AddUi(player, container);

            if (IsOpen)
            {
                RefreshEvents(player, "All");
                OnlineUi(player);
                ButtonUi(player);
            }
            if (player == null) return;
            if(NoEscape != null)
            {
                if (NoEscape.Call<bool>("IsBlocked", player))
                {
                    RaidBlockUi(player, NoEscape.Call<double>("NoEscape_Time", player));
                }
            }

        }

        private void DestroyButtons(BasePlayer player)
        {
            foreach (var x in _config.MenuCmds)
            {
                CuiHelper.DestroyUi(player, _menuLayer + x.Value);
                CuiHelper.DestroyUi(player, _menuLayer + x.Value + "IMG");
            }
        }

        private void ButtonUi(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            DestroyButtons(player);

            double anchormin = 0.7418166, anchormax = 0.8415062, margin = 0.8415062 - 0.7106612;
            foreach (var x in _config.MenuCmds)
            {
                container.Add(new CuiButton
                {
                    FadeOut = fadeout,
                    RectTransform =
                        {
                            AnchorMin = $"{anchormin} 0.1489384",
                            AnchorMax = $"{anchormax} 0.8297893"
                        },
                    Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"{x.Value}",
                        },
                    Text =
                        {
                            Text = ""
                        }
                }, _menuLayer, _menuLayer + x.Value);

                container.Add(new CuiElement
                {
                    Name = _menuLayer + x.Value + "IMG",
                    Parent = _menuLayer + x.Value,
                    FadeOut = fadeout,
                    Components =
                        {
                            new CuiRawImageComponent{Color = "1 1 1 0.7", Png = ImageLibrary?.Call<string>("GetImage", x.Value),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0 0", AnchorMax = "1 1"}
                        }
                });

                anchormin -= margin;
                anchormax -= margin;
            }

            CuiHelper.AddUi(player, container);
        }

        private void OnlineUi(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();

            CuiHelper.DestroyUi(player, "Online");
            bool IsOpen = false;
            _menuOpen.TryGetValue(player, out IsOpen);
            if (IsOpen)
            {
                container.Add(new CuiElement
                {
                    Parent = $"LeftPanelLogo",
                    Name = "Online",
                    FadeOut = fadeout,
                    Components =
                            {
                                new CuiTextComponent{Color = $"{HexToRustFormat("#E8E2D9E5")}", Text = $"{GetOnline()}/{ConVar.Server.maxplayers}", Align = TextAnchor.MiddleCenter, FontSize = 13, Font = "robotocondensed-bold.ttf", FadeIn = fadein},
                                new CuiRectTransformComponent{AnchorMin = "0.2545756 0.2415287", AnchorMax = "0.4695288 0.6883376"}
                            }
                });

            }


            CuiHelper.AddUi(player, container);
        }


        private void SendNotify(BasePlayer player, string message, int show = 5, string type = "Event")
        {
            if (MagixNotify != null)
                MagixNotify?.Call("SendNotify", player, type, message, show);
            else
                SendReply(player, message);
        }

        private void RefreshEvents(BasePlayer player, string events)
        {
            CuiElementContainer container = new CuiElementContainer();

            bool IsOpen = false;
            _menuOpen.TryGetValue(player, out IsOpen);
            if (!IsOpen) return;

            switch (events)
            {
                case "All":
                    CuiHelper.DestroyUi(player, "CH47");
                    CuiHelper.DestroyUi(player, "CH47BTN");
                    CuiHelper.DestroyUi(player, "Heli");
                    CuiHelper.DestroyUi(player, "HeliBTN");
                    CuiHelper.DestroyUi(player, "Cargo");
                    CuiHelper.DestroyUi(player, "CargoBTN");
                    CuiHelper.DestroyUi(player, "Bradley");
                    CuiHelper.DestroyUi(player, "BradleyBTN");


                    container.Add(new CuiElement
                    {
                        Name = "CH47",
                        Parent = $"LeftPanelLogo",
                        FadeOut = fadeout,
                        Components =
                        {
                            new CuiRawImageComponent{Color = _isCh47 ? $"{HexToRustFormat(_config.Ch47Color)}" : "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "CH47"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.632399 0.1489384", AnchorMax = "0.7320886 0.82979"}
                        }
                    });

                    container.Add(new CuiButton
                    {
                        FadeOut = fadeout,
                        RectTransform =
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                        Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"magix.info CH47",
                        },
                        Text =
                        {
                            Text = ""
                        }
                    }, "CH47", $"CH47BTN");

                    container.Add(new CuiElement
                    {
                        Name = "Heli",
                        Parent = $"LeftPanelLogo",
                        FadeOut = fadeout,
                        Components =
                        {
                            new CuiRawImageComponent{Color = _isHeli ? $"{HexToRustFormat(_config.HeliColor)}" : "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "Heli"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.5077883 0.1489384", AnchorMax = "0.6074779 0.82979"}
                        }
                    });


                    container.Add(new CuiButton
                    {
                        FadeOut = fadeout,
                        RectTransform =
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                        Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"magix.info Heli",
                        },
                        Text =
                        {
                            Text = ""
                        }
                    }, "Heli", $"HeliBTN");

                    container.Add(new CuiElement
                    {
                        Name = "Cargo",
                        Parent = $"LeftPanelLogo",
                        FadeOut = fadeout,
                        Components =
                        {
                            new CuiRawImageComponent{Color = _isCargoShip ? $"{HexToRustFormat(_config.CargoColor)}" : "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "Cargo"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.8820077 0.1489384", AnchorMax = "0.9816972 0.82979"}
                        }
                    });


                    container.Add(new CuiButton
                    {
                        FadeOut = fadeout,
                        RectTransform =
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                        Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"magix.info Cargo",
                        },
                        Text =
                        {
                            Text = ""
                        }
                    }, "Cargo", $"CargoBTN");

                    container.Add(new CuiElement
                    {
                        Name = "Bradley",
                        Parent = $"LeftPanelLogo",
                        FadeOut = fadeout,
                        Components =
                        {
                            new CuiRawImageComponent{Color = _isBradley ? $"{HexToRustFormat(_config.BradleyColor)}" : "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "Bradley"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.7573982 0.1489384", AnchorMax = "0.8570878 0.82979"}
                        }
                    });


                    container.Add(new CuiButton
                    {
                        FadeOut = fadeout,
                        RectTransform =
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                        Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"magix.info Bradley",
                        },
                        Text =
                        {
                            Text = ""
                        }
                    }, "Bradley", $"BradleyBTN");



                    break;
                case "CH47":
                    CuiHelper.DestroyUi(player, "CH47");
                    CuiHelper.DestroyUi(player, "CH47BTN");

                    container.Add(new CuiElement
                    {
                        Name = "CH47",
                        Parent = $"LeftPanelLogo",
                        FadeOut = fadeout,
                        Components =
                        {
                            new CuiRawImageComponent{Color = _isCh47 ? $"{HexToRustFormat(_config.Ch47Color)}" : "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "CH47"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.632399 0.1489384", AnchorMax = "0.7320886 0.82979"}
                        }
                    });

                    container.Add(new CuiButton
                    {
                        FadeOut = fadeout,
                        RectTransform =
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                        Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"magix.info CH47",
                        },
                        Text =
                        {
                            Text = ""
                        }
                    }, "CH47", $"CH47BTN");

                    break;
                case "Heli":
                    CuiHelper.DestroyUi(player, "Heli");
                    CuiHelper.DestroyUi(player, "HeliBTN");

                    container.Add(new CuiElement
                    {
                        Name = "Heli",
                        Parent = $"LeftPanelLogo",
                        FadeOut = fadeout,
                        Components =
                        {
                            new CuiRawImageComponent{Color = _isHeli ? $"{HexToRustFormat(_config.HeliColor)}" : "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "Heli"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.5077883 0.1489384", AnchorMax = "0.6074779 0.82979"}
                        }
                    });


                    container.Add(new CuiButton
                    {
                        FadeOut = fadeout,
                        RectTransform =
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                        Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"magix.info Heli",
                        },
                        Text =
                        {
                            Text = ""
                        }
                    }, "Heli", $"HeliBTN");
                    break;
                case "Cargo":
                    CuiHelper.DestroyUi(player, "Cargo");
                    CuiHelper.DestroyUi(player, "CargoBTN");

                    container.Add(new CuiElement
                    {
                        Name = "Cargo",
                        Parent = $"LeftPanelLogo",
                        FadeOut = fadeout,
                        Components =
                        {
                            new CuiRawImageComponent{Color = _isCargoShip ? $"{HexToRustFormat(_config.CargoColor)}" : "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "Cargo"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.8820077 0.1489384", AnchorMax = "0.9816972 0.82979"}
                        }
                    });


                    container.Add(new CuiButton
                    {
                        FadeOut = fadeout,
                        RectTransform =
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                        Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"magix.info Cargo",
                        },
                        Text =
                        {
                            Text = ""
                        }
                    }, "Cargo", $"CargoBTN");

                    break;
                case "Bradley":
                    CuiHelper.DestroyUi(player, "Bradley");
                    CuiHelper.DestroyUi(player, "BradleyBTN");

                    container.Add(new CuiElement
                    {
                        Name = "Bradley",
                        Parent = $"LeftPanelLogo",
                        FadeOut = fadeout,
                        Components =
                        {
                            new CuiRawImageComponent{Color = _isBradley ? $"{HexToRustFormat(_config.BradleyColor)}" : "1 1 1 0.7", Png = ImageLibrary.Call<string>("GetImage", "Bradley"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.7573982 0.1489384", AnchorMax = "0.8570878 0.82979" }
                        }
                    });


                    container.Add(new CuiButton
                    {
                        FadeOut = fadeout,
                        RectTransform =
                        {
                            AnchorMin = $"0 0",
                            AnchorMax = $"1 1"
                        },
                        Button =
                        {
                            FadeIn = fadein,
                            Color = "0 0 0 0",
                            Command = $"magix.info Bradley",
                        },
                        Text =
                        {
                            Text = ""
                        }
                    }, "Bradley", $"BradleyBTN");

                    break;
            }

            CuiHelper.AddUi(player, container);
        }


        [HookMethod("RaidBlockMagix")]
        public void RaidBlockUi(BasePlayer player, double time)
        {
            bool IsOpen = false;
            CuiElementContainer container = new CuiElementContainer();

            CuiHelper.DestroyUi(player, "RB");
            CuiHelper.DestroyUi(player, "RBText");


            if (_menuOpen.TryGetValue(player, out IsOpen) && IsOpen)
            {
                container.Add(new CuiElement
                {
                    Name = "RB",
                    Parent = $"LeftPanelLogo",
                    FadeOut = fadeout,
                    Components =
                        {
                            new CuiRawImageComponent{Color = $"{HexToRustFormat("#C34D4D")}", Png = ImageLibrary.Call<string>("GetImage", "RaidBlock"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "1.003507 0.1489384", AnchorMax = "1.103196 0.82979"}
                        }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Name = "RB",
                    Parent = $"LeftPanelLogo",
                    FadeOut = fadeout,
                    Components =
                        {
                            new CuiRawImageComponent{Color = $"{HexToRustFormat("#C34D4D")}", Png = ImageLibrary.Call<string>("GetImage", "RaidBlock"),  FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0.2429907 0.1489384", AnchorMax = "0.3426793 0.82979"}
                        }
                });
            }

            container.Add(new CuiElement
            {
                Parent = $"RB",
                Name = "RBText",
                FadeOut = fadeout,
                Components =
                        {
                            new CuiTextComponent{Color = $"1 1 1 0.7", Text = $"{GetFormatTime(TimeSpan.FromSeconds(time))}", Align = TextAnchor.MiddleCenter, FontSize = 8, Font = "robotocondensed-bold.ttf", FadeIn = fadein},
                            new CuiRectTransformComponent{AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "25 0", OffsetMax = "45 20"}
                        }
            });


            CuiHelper.AddUi(player, container);

        }



        [HookMethod("IsOpen")]
        public bool IsOpened(BasePlayer player)
        {
            bool isOpen = false;
            _menuOpen.TryGetValue(player, out isOpen);
            return isOpen;
        }

        #endregion

        #region Helpers
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
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

        [ConsoleCommand("magix.logo")]
        private void MenuHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            if (!_menuOpen.ContainsKey(player)) _menuOpen.Add(player, false);

            if (!_menuOpen[player])
            {
                _menuOpen[player] = true;
                DrawMenu(player);
                return;
            }
            if (_menuOpen[player])
            {
                _menuOpen[player] = false;
                DrawMenu(player);
                return;
            }
        }

        [ConsoleCommand("magix.info")]
        private void InfoHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;

            var text = string.Empty;
            switch (args.Args[0])
            {
                case "CH47":
                    if (_isCh47)
                    {
                        text = "Статус ивента:\nАктивен";
                    }
                    else
                    {
                        text = $"Ивент <color=#55C4E7>''ЧИНУК''</color> начнётся через:\n<color=#55C4E7>{EventRandomizer?.Call<string>("Ch47Time")}</color>";
                    }
                    SendNotify(player, text);
                    break;
                case "Heli":
                    if (_isHeli)
                    {
                        text = "Статус ивента:\nАктивен";
                    }
                    else
                    {
                        text = $"Ивент <color=#55C4E7>''ВЕРТОЛЁТ''</color> начнётся через:\n<color=#55C4E7>{EventRandomizer?.Call<string>("HeliTime")}</color>";
                    }
                    SendNotify(player, text);
                    break;
                case "Bradley":
                    if (_isBradley)
                    {
                        text = "Статус ивента:\nАктивен";
                    }
                    else
                    {
                        text = $"Ивент <color=#55C4E7>''ТАНК''</color> начнётся через:\n<color=#55C4E7>{EventRandomizer?.Call<string>("BradleyTime")}</color>";
                    }
                    SendNotify(player, text);
                    break;
                case "Cargo":
                    if (_isCargoShip)
                    {
                        text = "Статус ивента:\nАктивен";
                    }
                    else
                    {
                        text = $"Ивент <color=#55C4E7>''КАРГО''</color> начнётся через:\n<color=#55C4E7>{EventRandomizer?.Call<string>("CargoTime")}</color>";
                    }
                    SendNotify(player, text);
                    break;
            }


        }

        private string GetFormatTime(TimeSpan timespan)
        {
            return string.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, System.Math.Floor(timespan.TotalHours));
        }

        #endregion
    }

}