using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("BHair", "King", "1.0.0")]
	public class BHair : RustPlugin
	{
        #region [Vars]
        [PluginReference] Plugin ImageLibrary;
        private static BHair plugin;
        public string Layer = "BHair.Layer";
        public string NewLayer = "BHair.NewLayer";
        public string NewLineLayer = "BHair.NewLineLayer";
        public string HairLayer = "BHair.HairLayer";
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 0, 0))
                {
                    //
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class MainSettings
        {
            [JsonProperty("Команда для меню прицелов")]
            public string openMenu;

            [JsonProperty(PropertyName = "Минимальный размер прицелов")]
            public int MinHairSize;

            [JsonProperty(PropertyName = "Максимальный размер прицелов")]
            public int MaxHairSize;
        }

        public class SettingsDefaultHair
        {
            [JsonProperty("Айди стандартного прицела ( Первоначальный айди прицела )")]
            public int HairID;

            [JsonProperty("Размеры стандратного прицела ( Первоначальный размер прицела )")]
            public int SizeHair;
        }

        public class SettingsHair
        {
            [JsonProperty("Картинка - ( Прицел )")]
            public string Image;

            [JsonProperty("Айди прицела ( При создании новых прибавлять )")]
            public int HairID;
        }

        private class PluginConfig
        {
            [JsonProperty("Основные настройки")]
            public MainSettings _MainSettings = new MainSettings();

            [JsonProperty("Стандартные настройки игрока")]
            public SettingsDefaultHair _SettingsDefaultHair = new SettingsDefaultHair();

            [JsonProperty("Настройка выбора прицелов")]
            public List<SettingsHair> _SettingsHair = new List<SettingsHair>();

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _MainSettings = new MainSettings
                    {
                        openMenu = "hair",
                        MinHairSize = 5,
                        MaxHairSize = 20,
                    },
                    _SettingsDefaultHair = new SettingsDefaultHair
                    {
                        HairID = 14,
                        SizeHair = 7,
                    },
                    _SettingsHair = new List<SettingsHair>()
                    {
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/O1T5M2S.png",
                            HairID = 1,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/62zrHBV.png",
                            HairID = 2,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/RACMuqg.png",
                            HairID = 3,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/tqtF73m.png",
                            HairID = 4,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/lBZ2Khj.png",
                            HairID = 5,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/7zs9aHt.png",
                            HairID = 6,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/udgZFcU.png",
                            HairID = 7,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/mIbPpj3.png",
                            HairID = 8,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/XCSkVNk.png",
                            HairID = 9,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/mIbPpj3.png",
                            HairID = 10,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/udgZFcU.png",
                            HairID = 11,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/7zs9aHt.png",
                            HairID = 12,
                        },
                        new SettingsHair()
                        {
                            Image = "https://imgur.com/lBZ2Khj.png",
                            HairID = 13,
                        },
                        new SettingsHair()
                        {
                            Image = "https://i.imgur.com/21Af6kF.png",
                            HairID = 14,
                        },
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion

        #region [Data]
        private void LoadData()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>("BHair/playerData");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (data == null) data = new PluginData();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("BHair/playerData", data);

        private class PluginData
        {
            public Dictionary<ulong, PlayerData> PlayerData = new Dictionary<ulong, PlayerData>();
        }
    
        private static PluginData data;

        private class PlayerData
        {
            public int HairID;
            public int HairSize;

            public static PlayerData GetOrAdd(BasePlayer player)
            {
                return GetOrAdd(player.userID);
            }

            public static PlayerData GetOrAdd(ulong userId)
            {
                if (!data.PlayerData.ContainsKey(userId))
                    data.PlayerData.Add(userId, new PlayerData
                    {
                        HairID = plugin.config._SettingsDefaultHair.HairID,
                        HairSize = plugin.config._SettingsDefaultHair.SizeHair,
                    });

                return data.PlayerData[userId];
            }
        }
        #endregion

        #region [ImageLibrary]
        private bool HasImage(string imageName, ulong imageId = 0) => (bool)ImageLibrary.Call("HasImage", imageName, imageId);
        private bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        private string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        #endregion

        #region [Oxide]
        private void OnServerInitialized()
        {
            plugin = this;
            LoadData();
            cmd.AddChatCommand(config._MainSettings.openMenu, this, "MainUi");
            foreach (var key in config._SettingsHair)
                AddImage(key.Image, key.Image);

            AddImage("https://i.postimg.cc/5t74ZzBr/C2g6QoA.png", $"{Name}.Background");
            AddImage("https://i.postimg.cc/V6rG9J5S/Group-11-1-1.png", $"ItemFon");
            AddImage("https://i.postimg.cc/Gt5jZ44x/uHTdwjY.png", $"{Name}.BlockFon");

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }
        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, HairLayer);
            }

            SaveData();
            plugin = null;
            data = null;
        }
        #endregion

        #region [Rust-Api]
        private void OnPlayerConnected(BasePlayer player)
        {
            var data = PlayerData.GetOrAdd(player);
            if (data != null)
            {
                var find = config._SettingsHair.FirstOrDefault(p => p.HairID == data.HairID);
                if (find == null) return;

                HairUI(player, find.Image);
            }
        }
        #endregion

        #region [Ui]
        private void MainUi(BasePlayer player)
        {
            #region [Vars]
            CuiElementContainer container = new CuiElementContainer();
            #endregion

            #region [Parrent]
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Color = "0 0 0 0.7" }
            }, "Overlay", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage($"{Name}.Background"), Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0.35", Material = "assets/content/ui/uibackgroundblur.mat", Sprite = "assets/content/ui/ui.background.transparent.radial.psd" }
            }, Layer);
            #endregion

            #region [Main-Ui]
            container.Add(new CuiPanel
            {
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.65" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-200 -185", OffsetMax = "203 186" },
            }, Layer, Layer + ".Main");

            container.Add(new CuiPanel
            {
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.65" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-180 -223", OffsetMax = "183 -195" },
            }, Layer, Layer + ".ChangeSize");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-43 -263", OffsetMax = "46 -235" },
                Text = { Text = "ЗАКРЫТЬ", Font = "robotocondensed-bold.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.65" },
                Button = { Color = "0.3773585 0.3755785 0.3755785 0.65", Close = Layer }
            }, Layer);
            #endregion

            #region [Text]
            container.Add(new CuiLabel
            {
                Text = { Text = $"ВЫБЕРИТЕ ПРИЦЕЛ", Color = "1 1 1 0.85", FontSize = 32, Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperCenter },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-200 186", OffsetMax = "203 235" },
            }, Layer);
            #endregion

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
            HairList(player);
            SizeLine(player);
        }

        private void HairList(BasePlayer player, Int32 page = 0)
        {
            #region [Vars]
            CuiElementContainer container = new CuiElementContainer();

            var ItemList = config._SettingsHair.Skip(25 * page).Take(25).ToList();
            var data = PlayerData.GetOrAdd(player);
            #endregion

            #region [Parrent]
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Main", Layer + ".Main" + ".LayerItem");
            #endregion

            #region [Items]
            for (Int32 i = 0, x = 0, y = 0; i < 25; i++)
            {
                if (ItemList.Count - 1 >= i)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = $"{0.036 + x * 0.187} {0.775 - y * 0.185}", AnchorMax = $"{0.206 + x * 0.187} {0.945 - y * 0.185}" },
                        Image = { Color = "0 0 0 0" }
                    }, Layer + ".Main" + ".LayerItem", Layer + ".Main" + ".LayerItem" + $".Item{i}");

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Main" + ".LayerItem" + $".Item{i}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage(ItemList[i].Image) },
                            new CuiRectTransformComponent { AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.8" }
                        }
                    });

                    if (data.HairID == ItemList[i].HairID)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = Layer + ".Main" + ".LayerItem" + $".Item{i}",
                            Components =
                            {
                                new CuiRawImageComponent { Png = GetImage($"ItemFon"), Color = "1 1 1 1" },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = Layer + ".Main" + ".LayerItem" + $".Item{i}",
                            Components =
                            {
                                new CuiRawImageComponent { Png = GetImage($"{Name}.BlockFon"), Color = "0 0 0 1" },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                            }
                        });

                        container.Add(new CuiPanel
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                            Image = { Color = "0 0 0 0.5" }
                        }, Layer + ".Main" + ".LayerItem" + $".Item{i}");
                    }

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = $"UI_BHAIR sethair {ItemList[i].HairID} {page}" },
                        Text = { Text = "" }
                    }, Layer + ".Main" + ".LayerItem" + $".Item{i}");
                }
                else
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = $"{0.036 + x * 0.187} {0.775 - y * 0.185}", AnchorMax = $"{0.206 + x * 0.187} {0.945 - y * 0.185}" },
                        Image = { Color = "0 0 0 0" }
                    }, Layer + ".Main" + ".LayerItem", Layer + ".Main" + ".LayerItem" + $".Item{i}");

                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Main" + ".LayerItem" + $".Item{i}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage($"{Name}.BlockFon"), Color = "0 0 0 1" },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                        }
                    });

                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Image = { Color = "0 0 0 0.5" }
                    }, Layer + ".Main" + ".LayerItem" + $".Item{i}");
                }

                x++;
                if (x == 5)
                {
                    x = 0;
                    y++;
                }
            }
            #endregion

            #region [Page]
            container.Add(new CuiButton
            {
                Button = { Color = "0.3773585 0.3755785 0.3755785 0.65", Command = config._SettingsHair.Skip(25 * (page + 1)).Count() > 0 ? $"UI_BHAIR setpage {page + 1}" : "" },
                Text = { Text = ">", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, Color = config._SettingsHair.Skip(25 * (page + 1)).Count() > 0 ? "1 1 1 0.65" : "1 1 1 0.15" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "207.5 -13", OffsetMax = "234 13" }
            }, Layer + ".Main" + ".LayerItem");

            container.Add(new CuiButton
            {
                Button = { Color = "0.3773585 0.3755785 0.3755785 0.65", Command = page >= 1 ? $"UI_BHAIR setpage {page - 1}" : "" },
                Text = { Text = "<", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, Color = page >= 1 ? "1 1 1 0.65" : "1 1 1 0.15" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-234 -13", OffsetMax = "-207.5 13" }
            }, Layer + ".Main" + ".LayerItem");
            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Main" + ".LayerItem");
            CuiHelper.AddUi(player, container);
        }

        private void SizeLine(BasePlayer player)
        {
            #region [Vars]
            CuiElementContainer container = new CuiElementContainer();

            var data = PlayerData.GetOrAdd(player);
            float width; float xSwitch; width = 250; xSwitch = 0;
            var steps = config._MainSettings.MaxHairSize - config._MainSettings.MinHairSize;
            var progress = (float)(data.HairSize - 5) / steps;
            var size = width / steps;
            #endregion

            #region [Parrent]
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".ChangeSize", Layer + ".ChangeSize" + ".Layer");
            #endregion

            #region [Main-Ui]
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.1 0.385", AnchorMax = "0.896 0.56"},
                Image = { Color = "0.2 0.2 0.2 0.45", Material = "assets/icons/greyout.mat" }
            }, Layer + ".ChangeSize" + ".Layer", ".HairSize.Line");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"{progress} 0.95" },
                Image = { Color = "1 1 0.55", Material = "assets/icons/greyout.mat" }
            }, ".HairSize.Line", ".HairSize.Line.Finish");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-5 -5", OffsetMax = "5 5" },
                Image = { Color = "0 0 0 1", Material = "assets/icons/greyout.mat" }
            }, ".HairSize.Line.Finish");

            for (var i = config._MainSettings.MinHairSize; i <= config._MainSettings.MaxHairSize; i++)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = $"{xSwitch} 0", OffsetMax = $"{xSwitch + size} 0" },
                    Text = { Text = "" },
                    Button = { Color = "0 0 0 0", Command = $"UI_BHAIR setsize {i}" }
                }, ".HairSize.Line");
                xSwitch += size;
            }
            #endregion

            CuiHelper.DestroyUi(player, Layer + ".ChangeSize" + ".Layer");
            CuiHelper.AddUi(player, container);
        }

        private void HairUI(BasePlayer player, string hair)
        {
            CuiHelper.DestroyUi(player, HairLayer);
            var container = new CuiElementContainer();
            var data = PlayerData.GetOrAdd(player);
            float margin = data.HairSize;
            if (data.HairID == config._SettingsHair.Count()) return;

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", HairLayer);

            container.Add(new CuiElement
            {
                Parent = HairLayer,
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage($"{hair}"), Color = "1 1 1 0.8" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"-{margin} -{margin}", OffsetMax = $"{margin} {margin}" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region [ConsoleCommand && ChatCommand]
        [ConsoleCommand("UI_BHAIR")]
        private void CmdConsoleMarkers(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            if (player == null || !arg.HasArgs()) return;

            switch (arg.Args[0])
            {
                case "setsize":
                {
                    int HairSize;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out HairSize)) return;

                    var data = PlayerData.GetOrAdd(player);
                    if (data == null) return;

                    var find = config._SettingsHair.FirstOrDefault(p => p.HairID == data.HairID);
                    if (find == null) return;

                    data.HairSize = HairSize;

                    HairUI(player, find.Image);
                    SizeLine(player);
                    break;
                }
                case "sethair":
                {
                    int HairID;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out HairID)) return;

                    var data = PlayerData.GetOrAdd(player);
                    if (data == null) return;

                    var find = config._SettingsHair.FirstOrDefault(p => p.HairID == HairID);
                    if (find == null) return;

                    data.HairID = HairID;

                    HairUI(player, find.Image);
                    HairList(player, int.Parse(arg.Args[2]));
                    break;
                }
                case "setpage":
                {
                    int Page;
                    if (!arg.HasArgs(2) || !int.TryParse(arg.Args[1], out Page)) return;

                    HairList(player, Page);
                    break;
                }
            }
        }
        #endregion
    }
}