using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("ShopSystem", "Sempai#3239", "1.0.0")]
    class ShopSystem : RustPlugin
    {
        #region Вар
        string Layer = "Shop_UI";

        ShopSystem ins;

        [PluginReference] Plugin ImageLibrary;

        Dictionary<ulong, string> ActiveButton = new Dictionary<ulong, string>();

        Dictionary<ulong, DataBase> DB = new Dictionary<ulong, DataBase>();

        public int Times = 70;
        public int RP = 100;
        #endregion

        #region Класс
        List<string> Category = new List<string>()
        {
            "All",
            "Ammunition",
            "Attire",
            "Components",
            "Construction",
            "Electrical",
            "Food",
            "Resources",
            "Tools",
            "Weapons"
        };

        public class Settings
        {
            public string ShortName;
            public int Amount;
            public int Price;
            public string Category;
        }

        public class DataBase
        {
            public int Rp = 0;
            public int Time = 70;
            public List<string> Item = new List<string>()
            {
                ""
            };
        }
        #endregion

        #region Конфиг
        Configuration config;
        class Configuration 
        {
            public List<Settings> settings;
            public static Configuration GetNewConfig() 
            {
                return new Configuration
                {
                    settings = new List<Settings>
                    {
                        new Settings
                        {
                            ShortName = "rifle.ak",
                            Amount = 1,
                            Price = 10,
                            Category = "Weapons"
                        }
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
            foreach (var check in config.settings)
                ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{check.ShortName}.png", check.ShortName);

            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            CreateDataBase(player); 
            TimeUpdate(player);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason) => SaveDataBase(player.userID);

        void Unload() 
        {
            foreach(var check in DB)
                SaveDataBase(check.Key);
        }
        #endregion

        #region Дата
        void CreateDataBase(BasePlayer player)
        {
            var DataBase = Interface.Oxide.DataFileSystem.ReadObject<DataBase>($"ShopSystem/{player.userID}");
            
            if (!DB.ContainsKey(player.userID))
                DB.Add(player.userID, new DataBase());
             
            DB[player.userID] = DataBase ?? new DataBase();
        }

        void SaveDataBase(ulong userId) => Interface.Oxide.DataFileSystem.WriteObject($"ShopSystem/{userId}", DB[userId]);
        #endregion

        #region Команды
        [ConsoleCommand("shop")]
        void ConsoleShop(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "category")
                {
                    ActiveButton[player.userID] = args.Args[1];
                    ItemUI(player, args.Args[1]);
                }
                if (args.Args[0] == "all")
                {
                    var db = DB[player.userID].Item;
                    if (db.Contains(args.Args[1]))
                        db.Remove(args.Args[1]);
                    else
                       db.Add(args.Args[1]);

                    ItemUI(player, ActiveButton[player.userID]);
                }
                if (args.Args[0] == "skip")
                {
                    ItemUI(player, ActiveButton[player.userID], int.Parse(args.Args[1]));
                }
                if (args.Args[0] == "buy")
                {
                    if (player.inventory.containerMain.itemList.Count >= 24)
                    {
                        SendReply(player, "Not enough space");
                        return;
                    }
                    var check = config.settings.FirstOrDefault(z => z.ShortName == args.Args[1]);
                    if (DB[player.userID].Rp >= check.Price)
                    {
                        var item = ItemManager.CreateByName(check.ShortName, check.Amount);
                        item.MoveToContainer(player.inventory.containerMain);
                        Effect x = new Effect("assets/bundled/prefabs/fx/notice/stack.world.fx.prefab", player, 0, new Vector3(), new Vector3());
                        EffectNetwork.Send(x, player.Connection);
                        DB[player.userID].Rp -= check.Price;
                        UpdateBalance(player);
                    }
                }
            }
        }
        #endregion

        #region Интерфейс
        void ShopUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            ActiveButton[player.userID] = "Ammunition";

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
                Text = { Text = $"SHOP", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-bold.ttf" }
            }, "Title");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.36 0.893", AnchorMax = $"0.97 0.954", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Description");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"Current play time: <color=#db8c5a>{FormatShortTime(TimeSpan.FromSeconds(DB[player.userID].Time))}</color>", Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-regular.ttf" }
            }, "Description");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.03 0.02", AnchorMax = $"0.25 0.07", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Balance");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"Balance: {DB[player.userID].Rp} RP", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Balance");

            CuiHelper.AddUi(player, container);
            ItemUI(player, ActiveButton[player.userID]);
        }

        void ItemUI(BasePlayer player, string category, int page = 0)
        {
            CuiHelper.DestroyUi(player, "Item");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" },
            }, Layer, "Item");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.85 0.02", AnchorMax = $"0.97 0.07", OffsetMax = "0 0" },
                Button = { Color = "0.86 0.55 0.35 1", Command = $"shop skip {page + 1}" },
                Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Item");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.72 0.02", AnchorMax = $"0.84 0.07", OffsetMax = "0 0" },
                Button = { Color = "0.86 0.55 0.35 1", Command = page >= 1 ? $"shop skip {page - 1}" : "" },
                Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-regular.ttf" }
            }, "Item");

            float width = 0f, height = 0.055f, startxBox = 0.028f, startyBox = 0.86f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in Category)
            {
                if (check == "All")
                    width = 0.05f;
                else
                    width = 0.0993f;

                var text = check == "All" ? "" : check;
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Button = { Color = "0 0 0 0.5", Command = $"shop category {check}" },
                    Text = { Text = text, Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, "Item", "Color");

                var color = ActiveButton[player.userID] == check ? "1 1 1 1" : "0 0 0 0";
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.1", OffsetMax = "0 0" },
                    Image = { Color = color },
                }, "Color");

                if (check == "All")
                {
                    container.Add(new CuiElement
                    {
                        Parent = "Color",
                        Components =
                        {
                            new CuiImageComponent { Sprite = "assets/icons/bleeding.png", FadeIn = 0.5f, Color = "0.86 0.55 0.35 1" },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7 7", OffsetMax = "-7 -7" }
                        }
                    });
                }

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            float width1 = 0.1572f, height1 = 0.18f, startxBox1 = 0.028f, startyBox1 = 0.8f - height1, xmin1 = startxBox1, ymin1 = startyBox1;

            if (ActiveButton[player.userID] == "All")
            {
                foreach (var items in DB[player.userID].Item.Skip(page * 24).Take(24))
                {
                            foreach (var check in config.settings.Where(z => z.ShortName == items))
            {
                if (DB[player.userID].Item.Contains(check.ShortName))
                {
                    Puts(check.ShortName);
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                        Image = { Color = "1 1 1 0.15" }
                    }, "Item", "Items");

                    container.Add(new CuiElement
                    {
                        Parent = "Items",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.ShortName), FadeIn = 0.5f },
                            new CuiRectTransformComponent { AnchorMin = "0 0.3", AnchorMax = "1 1", OffsetMin = "30 18", OffsetMax = "-30 -5" }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.8 0.79", AnchorMax = "1 0.99", OffsetMax = "0 0" },
                        Button = { Color = "1 1 1 0", Command = $"shop all {check.ShortName}" },
                        Text = { Text = "" }
                    }, "Items", "All");

                    var color = DB[player.userID].Item.Contains(check.ShortName) ? "0.86 0.55 0.35 1" : "1 1 1 1";
                    container.Add(new CuiElement
                    {
                        Parent = "All",
                        Components =
                        {
                            new CuiImageComponent { Sprite = "assets/icons/bleeding.png", FadeIn = 0.5f, Color = color },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "3 3", OffsetMax = "-3 -3" }
                        }
                    });

                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0.2", AnchorMax = "1 0.43", OffsetMax = "0 0" },
                        Image = { Color = "1 1 1 0.6" },
                    }, "Items", "Name");

                    var item = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(check.ShortName).itemid, 1, 0);
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"0.8 1", OffsetMax = "0 0" },
                        Text = { Text = $"{item.info.displayName.english}", Color = "0 0 0 1", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, "Name");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1", OffsetMax = "0 0" },
                        Text = { Text = $"x{check.Amount}", Color = "0 0 0 1", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, "Name");

                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2", OffsetMax = "0 0" },
                        Image = { Color = "0.38 0.37 0.38 1" },
                    }, "Items", "Price");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                        Text = { Text = $"{check.Price} RP", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                    }, "Price");

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"0.7 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 1", Command = $"shop buy {check.ShortName}" },
                        Text = { Text = $"BUY", Align = TextAnchor.MiddleCenter, FontSize = 17, Font = "robotocondensed-bold.ttf" }
                    }, "Price");

                    xmin1 += width1;
                    if (xmin1 + width1 >= 1)
                    {
                        xmin1 = startxBox1;
                        ymin1 -= height1;
                    }
                }
            }
            }
            }
            else
            {
foreach (var check in config.settings.Where(z => z.Category == ActiveButton[player.userID]).Skip(page * 24).Take(24))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                    Image = { Color = "1 1 1 0.15" }
                }, "Item", "Items");

                container.Add(new CuiElement
                {
                    Parent = "Items",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.ShortName), FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0.3", AnchorMax = "1 1", OffsetMin = "30 18", OffsetMax = "-30 -5" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.8 0.79", AnchorMax = "1 0.99", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0", Command = $"shop all {check.ShortName}" },
                    Text = { Text = "" }
                }, "Items", "All");

                var color = DB[player.userID].Item.Contains(check.ShortName) ? "0.86 0.55 0.35 1" : "1 1 1 1";
                container.Add(new CuiElement
                {
                    Parent = "All",
                    Components =
                    {
                        new CuiImageComponent { Sprite = "assets/icons/bleeding.png", FadeIn = 0.5f, Color = color },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "3 3", OffsetMax = "-3 -3" }
                    }
                });

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0.2", AnchorMax = "1 0.43", OffsetMax = "0 0" },
                    Image = { Color = "1 1 1 0.6" },
                }, "Items", "Name");

                var item = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(check.ShortName).itemid, 1, 0);
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"0.8 1", OffsetMax = "0 0" },
                    Text = { Text = $"{item.info.displayName.english}", Color = "0 0 0 1", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, "Name");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.97 1", OffsetMax = "0 0" },
                    Text = { Text = $"x{check.Amount}", Color = "0 0 0 1", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, "Name");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2", OffsetMax = "0 0" },
                    Image = { Color = "0.38 0.37 0.38 1" },
                }, "Items", "Price");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Text = { Text = $"{check.Price} RP", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, "Price");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.7 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 1", Command = $"shop buy {check.ShortName}" },
                    Text = { Text = $"BUY", Align = TextAnchor.MiddleCenter, FontSize = 17, Font = "robotocondensed-bold.ttf" }
                }, "Price");

                xmin1 += width1;
                if (xmin1 + width1 >= 1)
                {
                    xmin1 = startxBox1;
                    ymin1 -= height1;
                }
            }
            }

            CuiHelper.AddUi(player, container);
        }

        void UpdateBalance(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Balance");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.03 0.02", AnchorMax = $"0.25 0.07", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer, "Balance");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = $"Balance: {DB[player.userID].Rp} RP", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Balance");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Хелпер
        Timer Timer = null;
        void TimeUpdate(BasePlayer player)
        {
            Timer = timer.Every(60f, () => {
                if (player.IsConnected)
                {
                    var db = DB[player.userID];
                    db.Time -= 60;
                    if (db.Time <= 0)
                    {
                        db.Time = Times;
                        db.Rp += RP;
                        Timer.Destroy();
                        TimeUpdate(player);
                    }
                }
            });
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