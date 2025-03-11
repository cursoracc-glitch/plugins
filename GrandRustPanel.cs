using System;
using System.Globalization;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("GrandRustPanel", "", "2.0.0")]
    public class GrandRustPanel : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        public Dictionary<ulong, bool> PanelVisibility = new Dictionary<ulong, bool>();
        string Layer = "GRPLayer";
		string Layer1 = "GRPLayer_Store";

        #region Config

        private PluginConfig cfg;

        public class PluginConfig
        {
            [JsonProperty("Основные настройки")]
            public Settings MainSettings = new Settings();

            [JsonProperty("Эвенты")]
            public ESettings EventSets = new ESettings();

            [JsonProperty("Сообщения")]
            public MessageSettings SettingsMessages = new MessageSettings();


            public class Settings
            {
                [JsonProperty("Включить показ кнопки магазина?")]
                public bool EnableStore = true;
                [JsonProperty("Время обновления поля времени сервера")]
                public float RefreshTimer = 10f;
            }

            public class ESettings
            {
                [JsonProperty("Показывать эвент \"танк\"?")]
                public bool EnableTank = true;
            }

            public class MessageSettings
            {
                [JsonProperty("Время обновления сообщений")]
                public float RefreshTimer = 30f;
                [JsonProperty("Размер текста для автосообщений")]
                public int TextSize = 12;
                [JsonProperty("Список сообщений", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> Messages = new List<string>
                {
                    "<color=lime>Пример сообщения 1</color>",
                    "<color=red>Пример сообщения 2</color>",
                    "<color=blue>Пример сообщения 3</color>"
                };
            }
        }

        private void Init()
        {
            cfg = Config.ReadObject<PluginConfig>();
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
        }

        #endregion

        #region Lang [Локализация]

        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"PanelHelpMessage", "Вы должны выбрать один из вариантов:\n/panel <color=green>on</color> - включает показ панели\n/panel <color=red>off</color> - выключает показ панели" },
			{"PanelOff", "Вы <color=red>выключили</color> показ панели" },
			{"PanelOn", "Вы <color=green>включили</color> показ панели" },
        };

        #endregion

        #region Hooks

        void InitializeLang()
        {
            lang.RegisterMessages(Messages, this, "ru");
            Messages = lang.GetMessages("ru", this);
        }

        private void OnServerInitialized()
        {
            if (!plugins.Exists("ImageLibrary"))
            {
                PrintWarning("Плагин 'ImageLibrary' не загружен, дальнейшая работа плагина невозможна!");
                Interface.Oxide.UnloadPlugin("ImageLibrary");
                return;
            }
            InitializeLang();

            ImageLibrary.Call("AddImage", "https://ia.wampi.ru/2020/11/07/OO5Bx507.png", "menu");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/5I4CK1F.png", "time");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/OUbRw5Y.png", "players");

            ImageLibrary.Call("AddImage", "https://i.imgur.com/c858A8C.png", "plane");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/Hj79nfP.png", "heli");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/ZJKbaZX.png", "ch47");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/5H4rcv6.png", "cargo");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/hjiqzkH.png", "tank");

            ImageLibrary.Call("AddImage", "https://i.imgur.com/2YSxBtf.png", "plane_called");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/DGZVlHJ.png", "heli_called");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/VR8YaTR.png", "ch47_called");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/FOO3f9E.png", "cargo_called");
            ImageLibrary.Call("AddImage", "https://i.imgur.com/vNziRyx.png", "tank_called");

            InvokeHandler.Instance.InvokeRepeating(UpdateTime, cfg.MainSettings.RefreshTimer, cfg.MainSettings.RefreshTimer);
            InvokeHandler.Instance.InvokeRepeating(DrawNewMessage, cfg.SettingsMessages.RefreshTimer, cfg.SettingsMessages.RefreshTimer);

            foreach (var player in BasePlayer.activePlayerList) OnPlayerConnected(player);
        }

        void Unload()
        {
            InvokeHandler.Instance.CancelInvoke(UpdateTime);
            InvokeHandler.Instance.CancelInvoke(DrawNewMessage);
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
				CuiHelper.DestroyUi(player, Layer1);
                CuiHelper.DestroyUi(player, "Message");
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (entity is CargoPlane || entity is BaseHelicopter || entity is CargoShip || entity is CH47Helicopter || entity is BradleyAPC)
            {
                var tag = entity is CargoPlane ? "plane" : entity is BradleyAPC ? "tank" : entity is BaseHelicopter ? "heli" : entity is CargoShip ? "cargo" : entity is CH47Helicopter ? "ch47" : "";
                timer.Once(1f, () => { foreach (var players in BasePlayer.activePlayerList) DrawEvents(players, tag); });
            }
            else return;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            if (!PanelVisibility.ContainsKey(player.userID))
                PanelVisibility.Add(player.userID, true);

            timer.Once(1f, () => {
                foreach (var players in BasePlayer.activePlayerList)
                {
					DrawStoreMenu(players);
					DrawMessage(players);
                    if (PanelVisibility[players.userID] == false) return;
                    DrawMenu(players);
                }
            });
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            timer.Once(1f, () => {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    if (PanelVisibility[players.userID] == false) return;
                    DrawMenu(players);
                }
            });
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null || entity.net == null) return;
            if (entity is CargoPlane)
                foreach (var player in BasePlayer.activePlayerList) DrawEvents(player, "plane");
            if (entity is BaseHelicopter)
                foreach (var player in BasePlayer.activePlayerList) DrawEvents(player, "heli");
            if (entity is CargoShip)
                foreach (var player in BasePlayer.activePlayerList) DrawEvents(player, "cargo");
            if (entity is CH47Helicopter)
                foreach (var player in BasePlayer.activePlayerList) DrawEvents(player, "ch47");
            if (entity is BradleyAPC && cfg.EventSets.EnableTank)
                foreach (var player in BasePlayer.activePlayerList) DrawEvents(player, "tank");
            else return;
        }

        #endregion

        #region Custom Bools

        bool HasHeli()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is BaseHelicopter)
                    return true;
            return false;
        }
        bool HasPlane()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is CargoPlane)
                    return true;
            return false;
        }
        bool HasCargo()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is CargoShip)
                    return true;
            return false;
        }

        bool HasCh47()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is CH47Helicopter)
                    return true;
            return false;
        }

        bool HasBradley()
        {
            foreach (var check in BaseNetworkable.serverEntities)
                if (check is BradleyAPC)
                    return true;
            return false;
        }

        #endregion

        #region Commands

        [ChatCommand("panel")]
        void PanelCommand(BasePlayer player, string command, string[] args)
        {
            int activeplayerscount = 0;
            if (player == null) return;
            if (args.Length < 1)
            {
                player.ChatMessage(Messages["PanelHelpMessage"]);
                return;
            }
            if (args[0] == "off")
            {
                PanelVisibility[player.userID] = false;
                UpdateClose(player);
				player.ChatMessage(Messages["PanelOff"]);
                CuiHelper.DestroyUi(player, Layer);
            }
            else if (args[0] == "on")
            {
                PanelVisibility[player.userID] = true;
                UpdateClose(player);
				player.ChatMessage(Messages["PanelOn"]);
                OnPlayerConnected(player);
            }
            else return;
        }

        [ConsoleCommand("panel")]
        void PanelConsoleCommand(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            int activeplayerscount = 0;
            if (player == null) return;
            if (args.Args.Length < 1)
            {
                player.ChatMessage(Messages["PanelHelpMessage"]);
                return;
            }
            if (args.Args[0] == "off")
            {
                PanelVisibility[player.userID] = false;
				player.ChatMessage(Messages["PanelOff"]);
                CuiHelper.DestroyUi(player, Layer);
                UpdateClose(player);
            }
            else if (args.Args[0] == "on")
            {
                PanelVisibility[player.userID] = true;
				player.ChatMessage(Messages["PanelOn"]);
                OnPlayerConnected(player);
                UpdateClose(player);
            }
            else return;
        }

        #endregion

        #region UI

        void DrawNewMessage()
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(players, "Message" + ".Message");
                var container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Parent = "Message",
                    Name = "Message" + ".Message",
                    FadeOut = 0.2f,
                    Components =
                    {
                        new CuiTextComponent { Text = cfg.SettingsMessages.Messages[new System.Random().Next(cfg.SettingsMessages.Messages.Count)], Align = TextAnchor.MiddleCenter, FontSize = cfg.SettingsMessages.TextSize, Font = "RobotoCondensed-bold.ttf" },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"},
                        new CuiOutlineComponent {Color = "0 0 0 1", Distance = "0.5 0.5"},
                    }
                });
                CuiHelper.AddUi(players, container);
            }
        }

        void UpdateTime()
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
                if (PanelVisibility[players.userID] == false) return;
                var time = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm");
                CuiHelper.DestroyUi(players, Layer + ".Time");
                var container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".Time",
                    FadeOut = 0.2f,
                    Components =
                    {
                        new CuiTextComponent { Text = time, Color = HexToRustFormat("#ACACACFF"), Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "RobotoCondensed-bold.ttf" },
                        new CuiRectTransformComponent {AnchorMin = "0.1782179 0", AnchorMax = "0.3014302 1"},
                        new CuiOutlineComponent {Color = "0 0 0 0.7", Distance = "0.2 0.2"},
                    }
                });
                CuiHelper.AddUi(players, container);
            }

        }

        void DrawEvents(BasePlayer player, string name)
        {
            if (PanelVisibility[player.userID] == false) return;
            var container = new CuiElementContainer();
            if (name == "plane")
            {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(players, Layer + ".Plane");
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Name = Layer + ".Plane",
                        FadeOut = 0.1f,
                        Components =
                        {
                            new CuiRawImageComponent { FadeIn = 0.1f, Png = HasPlane() ? (string)ImageLibrary.Call("GetImage", "plane_called") : (string)ImageLibrary.Call("GetImage", "plane") },
                            new CuiRectTransformComponent { AnchorMin = "0.5585056 0.02469136", AnchorMax = "0.6465144 1.012346" }
                        }
                    });
                }
            }
            if (name == "heli")
            {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(players, Layer + ".Heli");
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Name = Layer + ".Heli",
                        FadeOut = 0.1f,
                        Components =
                        {
                            new CuiRawImageComponent { FadeIn = 0.1f, Png = HasHeli() ? (string)ImageLibrary.Call("GetImage", "heli_called") : (string)ImageLibrary.Call("GetImage", "heli") },
                            new CuiRectTransformComponent { AnchorMin = "0.6465154 0.02469136", AnchorMax = "0.7345243 1.012346" }
                        }
                    });
                }
            }
            if (name == "ch47")
            {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(players, Layer + ".Ch47");
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Name = Layer + ".Ch47",
                        FadeOut = 0.1f,
                        Components =
                        {
                            new CuiRawImageComponent { FadeIn = 0.1f, Png = HasCh47() ? (string)ImageLibrary.Call("GetImage", "ch47_called") : (string)ImageLibrary.Call("GetImage", "ch47") },
                            new CuiRectTransformComponent { AnchorMin = "0.7345252 0.02469136", AnchorMax = "0.8225342 1.012346" }
                        }
                    });
                }
            }
            if (name == "cargo")
            {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(players, Layer + ".Cargo");
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Name = Layer + ".Cargo",
                        FadeOut = 0.1f,
                        Components =
                        {
                            new CuiRawImageComponent { FadeIn = 0.1f, Png = HasCargo() ? (string)ImageLibrary.Call("GetImage", "cargo_called") : (string)ImageLibrary.Call("GetImage", "cargo") },
                            new CuiRectTransformComponent { AnchorMin = "0.822535 0.02469136", AnchorMax = "0.910544 1.012346" }
                        }
                    });
                }
            }
            if (name == "tank" && cfg.EventSets.EnableTank)
            {
                foreach (var players in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(players, Layer + ".Tank");
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Name = Layer + ".Tank",
                        FadeOut = 0.1f,
                        Components =
                        {
                            new CuiRawImageComponent { FadeIn = 0.1f, Png = HasBradley() ? (string)ImageLibrary.Call("GetImage", "tank_called") : (string)ImageLibrary.Call("GetImage", "tank") },
                            new CuiRectTransformComponent { AnchorMin = "0.9127451 0.02469136", AnchorMax = "1.000754 1.012346" }
                        }
                    });
                }
            }
            CuiHelper.AddUi(player, container);
        }

        private void DrawStoreMenu(BasePlayer player)
        {
			if (!cfg.MainSettings.EnableStore) return;
            CuiHelper.DestroyUi(player, Layer1);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat("#FFFFFF00") },
                RectTransform = { AnchorMin = "0.001041666 0.9648147", AnchorMax = "0.001041666 0.9648147", OffsetMin = "10 -4", OffsetMax = "313 23" },
                CursorEnabled = false,
            }, "Hud", Layer1);
			
                container.Add(new CuiElement
                {
                    Parent = Layer1,
                    Name = Layer1 + ".Store",
                    Components =
                    {
                        new CuiRawImageComponent { Color = HexToRustFormat("#FFFFFFCC"), Png = (string)ImageLibrary.Call("GetImage", "menu") },
                        new CuiRectTransformComponent { AnchorMin = "0.001849664 0.02469136", AnchorMax = "0.08985749 1.012346" }
                    }
                });
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = "chat.say /menu" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" }
                }, Layer1 + ".Store");
				
				CuiHelper.AddUi(player, container);
                UpdateClose(player);
		}

        private void UpdateClose(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Close");
            var container = new CuiElementContainer();
            var command = PanelVisibility[player.userID] == true ? "panel off" : "panel on";
            var text = PanelVisibility[player.userID] == true ? "<" : ">";
            var anchormin = PanelVisibility[player.userID] == true ? "1 0" : "0.1 0";
            var anchormax = PanelVisibility[player.userID] == true ? "1.07 1" : "0.17 1";
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = anchormin, AnchorMax = anchormax, OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = command },
                Text = { Text = text, Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, Layer1, "Close");

            CuiHelper.AddUi(player, container);
        }

        private void DrawMenu(BasePlayer player)
        {
            if (PanelVisibility[player.userID] == false) return;
            var online = BasePlayer.activePlayerList.Count().ToString();
            var time = TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm");

            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat("#FFFFFF00") },
                RectTransform = { AnchorMin = "0.001041666 0.9648147", AnchorMax = "0.001041666 0.9648147", OffsetMin = "10 -4", OffsetMax = "313 23" },
                CursorEnabled = false,
            }, "Hud", Layer);

            if (cfg.MainSettings.EnableStore)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".Store",
                    Components =
                    {
                        new CuiRawImageComponent { Color = HexToRustFormat("#FFFFFFCC"), Png = (string)ImageLibrary.Call("GetImage", "menu") },
                        new CuiRectTransformComponent { AnchorMin = "0.001849664 0.02469136", AnchorMax = "0.08985749 1.012346" }
                    }
                });
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = "chat.say /menu" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "" }
                }, Layer + ".Store");
            }

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Color = HexToRustFormat("#FFFFFFCC"), Png = (string)ImageLibrary.Call("GetImage", "time") },
                    new CuiRectTransformComponent { AnchorMin = "0.0898585 0.02469136", AnchorMax = "0.1778662 1.012346" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Time",
                FadeOut = 0.2f,
                Components =
                {
                    new CuiTextComponent { Text = time, Color = HexToRustFormat("#ACACACFF"), Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "RobotoCondensed-bold.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.1782179 0", AnchorMax = "0.3014302 1"},
                    new CuiOutlineComponent {Color = "0 0 0 0.7", Distance = "0.2 0.2"},
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Color = HexToRustFormat("#FFFFFFCC"), Png = (string)ImageLibrary.Call("GetImage", "players") },
                    new CuiRectTransformComponent { AnchorMin = "0.3032796 0.02469136", AnchorMax = "0.3912872 1.012346" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent { Text = online + "/" + ConVar.Server.maxplayers, Color = HexToRustFormat("#ACACACFF"), Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "RobotoCondensed-bold.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.3938391 0", AnchorMax = "0.5522553 1"},
                    new CuiOutlineComponent {Color = "0 0 0 0.7", Distance = "0.2 0.2"},
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Plane",
                FadeOut = 0.1f,
                Components =
                {
                    new CuiRawImageComponent { FadeIn = 0.1f, Png = HasPlane() ? (string)ImageLibrary.Call("GetImage", "plane_called") : (string)ImageLibrary.Call("GetImage", "plane") },
                    new CuiRectTransformComponent { AnchorMin = "0.5585056 0.02469136", AnchorMax = "0.6465144 1.012346" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Heli",
                FadeOut = 0.1f,
                Components =
                {
                    new CuiRawImageComponent { FadeIn = 0.1f, Png = HasHeli() ? (string)ImageLibrary.Call("GetImage", "heli_called") : (string)ImageLibrary.Call("GetImage", "heli") },
                    new CuiRectTransformComponent { AnchorMin = "0.6465154 0.02469136", AnchorMax = "0.7345243 1.012346" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Ch47",
                FadeOut = 0.1f,
                Components =
                {
                    new CuiRawImageComponent { FadeIn = 0.1f, Png = HasCh47() ? (string)ImageLibrary.Call("GetImage", "ch47_called") : (string)ImageLibrary.Call("GetImage", "ch47") },
                    new CuiRectTransformComponent { AnchorMin = "0.7345252 0.02469136", AnchorMax = "0.8225342 1.012346" }
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Cargo",
                FadeOut = 0.1f,
                Components =
                {
                    new CuiRawImageComponent { FadeIn = 0.1f , Png = HasCargo() ? (string)ImageLibrary.Call("GetImage", "cargo_called") : (string)ImageLibrary.Call("GetImage", "cargo") },
                    new CuiRectTransformComponent { AnchorMin = "0.822535 0.02469136", AnchorMax = "0.910544 1.012346" }
                }
            });
            if (cfg.EventSets.EnableTank)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".Tank",
                    FadeOut = 0.1f,
                    Components =
                    {
                        new CuiRawImageComponent { FadeIn = 0.1f, Png = HasBradley() ? (string)ImageLibrary.Call("GetImage", "tank_called") : (string)ImageLibrary.Call("GetImage", "tank") },
                        new CuiRectTransformComponent { AnchorMin = "0.9127451 0.02469136", AnchorMax = "1.000754 1.012346" }
                    }
                });
            }

            CuiHelper.AddUi(player, container);
        }

        void DrawMessage(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Message");
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat("#FFFFFF00") },
                RectTransform = { AnchorMin = "0.3453124 -0.0009259344", AnchorMax = "0.6416667 0.0287037" },
                CursorEnabled = false,
            }, "Hud", "Message");

            container.Add(new CuiElement
            {
                Parent = "Message",
                Name = "Message" + ".Message",
                FadeOut = 0.2f,
                Components =
                {
                    new CuiTextComponent { Text = cfg.SettingsMessages.Messages[new System.Random().Next(cfg.SettingsMessages.Messages.Count)], Align = TextAnchor.MiddleCenter, FontSize = cfg.SettingsMessages.TextSize, Font = "RobotoCondensed-bold.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"},
                    new CuiOutlineComponent {Color = "0 0 0 1", Distance = "0.5 0.5"},
                }
            });
            CuiHelper.AddUi(player, container);
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

        #endregion
    }
}