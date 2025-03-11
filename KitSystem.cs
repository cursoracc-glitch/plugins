using System;
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("KitSystem", "Sempai#3239", "1.0.0")]
    class KitSystem : RustPlugin
    {
        #region Вар
        string Layer = "Kit_UI";

        [PluginReference] Plugin ImageLibrary;

        Dictionary<ulong, Data> Settings = new Dictionary<ulong, Data>();
        #endregion

        #region Класс
        public class KitSettings 
        {
            public string Name;
            public string DisplayName;
            public double Cooldown;
            public int Amount;
            public string Perm;
            public string Url;
            public List<ItemSettings> Items;
        }

        public class ItemSettings
        {
            public string ShortName;
            public int Amount;
            public ulong SkinID;
            public string Container;
        }

        public class Data
        {
            public Dictionary<string, KitData> SettingsData = new Dictionary<string, KitData>();
        }

        public class KitData
        {
            public double Cooldown;
        }
        #endregion

        #region Конфиг
        Configuration config;
        class Configuration 
        {
            public string BannerURL = "";
            public string Description = "Buy more kits at <color=#db8c5a>store.хуита.ru</color>";
            public List<KitSettings> settings;
            public static Configuration GetNewConfig() 
            {
                return new Configuration
                {
                    settings = new List<KitSettings>() 
                    {
                        new KitSettings 
                        {
                            Name = "start",
                            DisplayName = "Start",
                            Cooldown = 10,
                            Amount = 2,
                            Perm = "kitsystem.use",
                            Url = "https://imgur.com/zpXoO6a.png",
                            Items = new List<ItemSettings>()
                            {
                                new ItemSettings
                                {
                                    ShortName = "wood",
                                    Amount = 1000,
                                    SkinID = 0,
                                    Container = "Main"
                                }
                            }
                        },
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
                if (config?.settings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Хуки
        void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", config.BannerURL, "Banner");
            foreach (var check in config.settings)
            {
                ImageLibrary.Call("AddImage", check.Url, check.Url);
                permission.RegisterPermission(check.Perm, this);
                foreach (var item in check.Items)
                    ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{item.ShortName}.png", item.ShortName);
            }

            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        void OnPlayerConnected(BasePlayer player) => CreateDataBase(player); 

        void OnPlayerDisconnected(BasePlayer player, string reason) => SaveDataBase(player.userID);

        void Unload() 
        {
            foreach(var check in Settings)
                SaveDataBase(check.Key);
        }
        #endregion

        #region Дата
        void CreateDataBase(BasePlayer player)
        {
            var DataBase = Interface.Oxide.DataFileSystem.ReadObject<Data>($"KitSystem/{player.userID}");
            
            if (!Settings.ContainsKey(player.userID))
                Settings.Add(player.userID, new Data());
             
            Settings[player.userID] = DataBase ?? new Data();
        }

        void SaveDataBase(ulong userId) => Interface.Oxide.DataFileSystem.WriteObject($"KitSystem/{userId}", Settings[userId]);
        #endregion

        #region Команды
        [ConsoleCommand("kit")]
        void ConsoleKit(ConsoleSystem.Arg args)
        {
            var Time = CurTime();
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "take")
                {
                    var check = config.settings.FirstOrDefault(z => z.Name == args.Args[1]);
                    if (player.inventory.containerMain.itemList.Count >= 24 || player.inventory.containerWear.itemList.Count >= 7 || player.inventory.containerBelt.itemList.Count >= 6)
                    {
                        SendReply(player, "Not enough space");
                        return;
                    }
                    var db = GetDataBase(player.userID, check.Name);
                    if (db.Cooldown > Time)
                    {
                        SendReply(player, "Wait");
                        return;
                    }
                    if (check.Cooldown > 0) db.Cooldown = Time + check.Cooldown;
                    if (check.Amount == 0)
                    {
                        SendReply(player, "You can no longer use this set");
                        return;
                    }
                    check.Amount -= 1;
                    foreach (var item in check.Items)
                    {
                        var items = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(item.ShortName).itemid, item.Amount, item.SkinID);
                        var main = item.Container == "Main" ? player.inventory.containerMain : player.inventory.containerWear;
                        var belt = item.Container == "Belt" ? player.inventory.containerBelt : main;
                        var moved = items.MoveToContainer(belt) || items.MoveToContainer(player.inventory.containerMain);
                    }
                    SendReply(player, "The set is received");
                    InterfaceKit(player);
                    Effect x = new Effect("assets/bundled/prefabs/fx/notice/stack.world.fx.prefab", player, 0, new Vector3(), new Vector3());
                    EffectNetwork.Send(x, player.Connection);
                }
                if (args.Args[0] == "back")
                {
                    UI(player, "");
                }
                if (args.Args[0] == "previev")
                {
                    UI(player, args.Args[1]);
                }
                if (args.Args[0] == "skip")
                {
                    InterfaceKit(player, int.Parse(args.Args[1]));
                }
            }
        }
        #endregion

        #region Интерфейс
        void KitUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.284 0", AnchorMax = "0.952 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.6" },
            }, "Menu", Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.032 0.893", AnchorMax = $"0.347 0.954", OffsetMax = "0 0" },
                Image = { Color = "0.86 0.55 0.35 1" }
            }, Layer, "Title");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"KITS", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-bold.ttf" }
            }, "Title");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.36 0.893", AnchorMax = $"0.97 0.954", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Description");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = config.Description, Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Description");

            CuiHelper.AddUi(player, container);
            UI(player, "");
        }

        void UI(BasePlayer player, string name)
        {
            var container = new CuiElementContainer();

            if (name == "")
            {
                CuiHelper.DestroyUi(player, "Name");
                CuiHelper.DestroyUi(player, "Back");
                CuiHelper.DestroyUi(player, "Inventory");
                CuiHelper.DestroyUi(player, "Clothing");
                CuiHelper.DestroyUi(player, "HotBar");
                CuiHelper.DestroyUi(player, "Items");
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.03 0.75", AnchorMax = $"0.97 0.86", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0.5" }
                }, Layer, "Banner");

                container.Add(new CuiElement
                {
                    Parent = "Banner",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Banner"), FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });

                CuiHelper.AddUi(player, container);
                InterfaceKit(player);
            }
            else
            {
                CuiHelper.DestroyUi(player, "Kit");
                CuiHelper.DestroyUi(player, "Banner");
                var check = config.settings.FirstOrDefault(z => z.Name == name);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.15 0.8", AnchorMax = $"0.97 0.86", OffsetMax = "0 0" },
                    Image = { Color = "1 1 1 1" }
                }, Layer, "Name");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0" }
                }, Layer, "Items");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = check.DisplayName.ToUpper(), Color = "0.38 0.37 0.38 1", Align = TextAnchor.MiddleCenter, FontSize = 35, Font = "robotocondensed-bold.ttf" }
                }, "Name");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.03 0.8", AnchorMax = $"0.14 0.86", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0.6", Command = "kit back" },
                    Text = { Text = "BACK", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, Layer, "Back");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.03 0.72", AnchorMax = $"0.495 0.79", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0.5" }
                }, Layer, "Inventory");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = "INVENTORY", Align = TextAnchor.MiddleCenter, FontSize = 35, Font = "robotocondensed-regular.ttf" }
                }, "Inventory");

                float width = 0.0782f, height = 0.09f, startxBox = 0.028f, startyBox = 0.715f - height, xmin = startxBox, ymin = startyBox;
                for (int z = 0; z < 24; z++)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                        Button = { Color = "0 0 0 0.5", Command = $"" },
                        Text = { Text = $"", Align = TextAnchor.UpperCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                    }, "Items");

                    xmin += width;
                    if (xmin + width + 0.45f >= 1)
                    {
                        xmin = startxBox;
                        ymin -= height;
                    }
                }

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.505 0.72", AnchorMax = $"0.97 0.79", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0.5" }
                }, Layer, "Clothing");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = "CLOTHING", Align = TextAnchor.MiddleCenter, FontSize = 35, Font = "robotocondensed-regular.ttf" }
                }, "Clothing");

                float width1 = 0.0782f, height1 = 0.09f, startxBox1 = 0.503f, startyBox1 = 0.715f - height1, xmin1 = startxBox1, ymin1 = startyBox1;
                for (int z = 0; z < 6; z++)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                        Button = { Color = "0 0 0 0.5", Command = $"" },
                        Text = { Text = $"", Align = TextAnchor.UpperCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                    }, "Items");

                    xmin1 += width1;
                    if (xmin1 + width1>= 1)
                    {
                        xmin1 = startxBox1;
                        ymin1 -= height1;
                    }
                }

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.505 0.538", AnchorMax = $"0.97 0.622", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0.5" }
                }, Layer, "HotBar");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = "HOTBAR", Align = TextAnchor.MiddleCenter, FontSize = 35, Font = "robotocondensed-regular.ttf" }
                }, "HotBar");

                float width2 = 0.0782f, height2 = 0.09f, startxBox2 = 0.503f, startyBox2 = 0.535f - height2, xmin2 = startxBox2, ymin2 = startyBox2;
                for (int z = 0; z < 6; z++)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"{xmin2} {ymin2}", AnchorMax = $"{xmin2 + width2} {ymin2 + height2 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                        Button = { Color = "0 0 0 0.5", Command = $"" },
                        Text = { Text = $"", Align = TextAnchor.UpperCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                    }, "Items");

                    xmin2 += width2;
                    if (xmin2 + width2>= 1)
                    {
                        xmin2 = startxBox2;
                        ymin2 -= height2;
                    }
                }

                float width3 = 0.0782f, height3 = 0.09f, startxBox3 = 0.028f, startyBox3 = 0.715f - height3, xmin3 = startxBox3, ymin3 = startyBox3;
                float width4 = 0.0782f, height4 = 0.09f, startxBox4 = 0.503f, startyBox4 = 0.715f - height4, xmin4 = startxBox4, ymin4 = startyBox4;
                float width5 = 0.0782f, height5 = 0.09f, startxBox5 = 0.503f, startyBox5 = 0.535f - height5, xmin5 = startxBox5, ymin5 = startyBox5;
                foreach (var item in check.Items)
                {
                    if (item.Container == "Main")
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"{xmin3} {ymin3}", AnchorMax = $"{xmin3 + width3} {ymin3 + height3 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                            Button = { Color = "0 0 0 0", Command = $"" },
                            Text = { Text = $"x{item.Amount} ", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                        }, "Items", "Item");

                        container.Add(new CuiElement
                        {
                            Parent = "Item",
                            Components =
                            {
                                new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.ShortName), FadeIn = 0.5f },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "8 8", OffsetMax = "-8 -8" }
                            }
                        });

                        xmin3 += width3;
                        if (xmin3 + width3 + 0.45f >= 1)
                        {
                            xmin3 = startxBox3;
                            ymin3 -= height3;
                        }
                    }
                    if (item.Container == "Wear")
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"{xmin4} {ymin4}", AnchorMax = $"{xmin4 + width4} {ymin4 + height4 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                            Button = { Color = "0 0 0 0", Command = $"" },
                            Text = { Text = $"x{item.Amount} ", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                        }, "Items", "Item");

                        container.Add(new CuiElement
                        {
                            Parent = "Item",
                            Components =
                            {
                                new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.ShortName), FadeIn = 0.5f },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "8 8", OffsetMax = "-8 -8" }
                            }
                        });

                        xmin4 += width4;
                    }
                    if (item.Container == "Belt")
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = $"{xmin5} {ymin5}", AnchorMax = $"{xmin5 + width5} {ymin5 + height5 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                            Button = { Color = "0 0 0 0", Command = $"" },
                            Text = { Text = $"x{item.Amount} ", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                        }, "Items", "Item");

                        container.Add(new CuiElement
                        {
                            Parent = "Item",
                            Components =
                            {
                                new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.ShortName), FadeIn = 0.5f },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "8 8", OffsetMax = "-8 -8" }
                            }
                        });

                        xmin5 += width5;
                    }
                }

                CuiHelper.AddUi(player, container);
            }
        }

        void InterfaceKit(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, "Kit");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" },
            }, Layer, "Kit");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.85 0.03", AnchorMax = $"0.97 0.09", OffsetMax = "0 0" },
                Button = { Color = "0.86 0.55 0.35 1", Command = config.settings.Count() > (page + 1) * 6 ? $"kit skip {page + 1}" : "" },
                Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Kit");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.72 0.03", AnchorMax = $"0.84 0.09", OffsetMax = "0 0" },
                Button = { Color = "0.86 0.55 0.35 1", Command = page >= 1 ? $"kit skip {page - 1}" : "" },
                Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Kit");

            float width = 0.472f, height = 0.2f, startxBox = 0.028f, startyBox = 0.72f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in config.settings.Where(z => (string.IsNullOrEmpty(z.Perm) || permission.UserHasPermission(player.UserIDString, z.Perm))).Skip(page * 6).Take(6).ToList())
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                    Button = { Color = "0.38 0.37 0.38 0.6", Command = $"" },
                    Text = { Text = $"", Align = TextAnchor.UpperCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, "Kit", "Kits");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.35 1", OffsetMax = "0 0" },
                    Image = { Color = "0.38 0.37 0.38 1" }
                }, "Kits", "KitImage");

                container.Add(new CuiElement
                {
                    Parent = $"KitImage",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.Url), FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2 2", OffsetMax = "-2 -2" }
                    }
                });

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.36 0.7", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0" }
                }, "Kits", "Name");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = check.DisplayName.ToUpper(), Align = TextAnchor.MiddleLeft, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, "Name");

                var db = GetDataBase(player.userID, check.Name);
                var Time = CurTime();
                var time = db.Cooldown > 0 && (db.Cooldown > Time) ? $"AVAILABLE IN {FormatShortTime(TimeSpan.FromSeconds(db.Cooldown - Time))}" : "AVAILABLE NOW";
                var amount = check.Amount > 0 ? $"{check.Amount} REMAINING USE" : "The set is not available";

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.36 0.24", AnchorMax = $"1 0.69", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0" }
                }, "Kits", "Use");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.02 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = $"{time}\n{amount}", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, "Use");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.37 0.03", AnchorMax = $"0.67 0.23", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 1", Command = $"kit previev {check.Name}" },
                    Text = { Text = "PREVIEV", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, "Kits");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.68 0.03", AnchorMax = $"0.98 0.23", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 1", Command = $"kit take {check.Name}" },
                    Text = { Text = "CLAIM", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, "Kits");

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Хелпер
        KitData GetDataBase(ulong userID, string name)
        {
            if (!Settings.ContainsKey(userID))
                Settings[userID].SettingsData = new Dictionary<string, KitData>();

            if (!Settings[userID].SettingsData.ContainsKey(name))
                Settings[userID].SettingsData[name] = new KitData();

            return Settings[userID].SettingsData[name];
        }

        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            result = $"{time.Hours.ToString("00")}:";
            result += $"{time.Minutes.ToString("00")}:";
            result += $"{time.Seconds.ToString("00")}";
            return result;
        }

        double CurTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;
        #endregion
    }
}