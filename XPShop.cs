using Oxide.Core;
using System.Collections.Generic;
using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Globalization;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("XPShop", "Chibubrik", "1.0.0")]
    class XPShop : RustPlugin
    {
        #region Вариабле
        private string Layer = "XP_SHOP";

        [PluginReference] private Plugin ImageLibrary;
        #endregion

        #region Класс
        public class Xp
        {
            [JsonProperty("Ник игрока")] public string Name;
            [JsonProperty("Кол-во XP у игрока!")] public float XP;
        }

        public class ShopItems
        {
            [JsonProperty("Название предмета")] public string ShortName;
            [JsonProperty("Описание")] public string Description;
            [JsonProperty("Количество предмета при покупке")] public int Amount;
            [JsonProperty("Количество предмета при продаже")] public int Amount2;
            [JsonProperty("Цена покупки")] public int Price;
            [JsonProperty("Цена продажи")] public int Price2;
        }
        #endregion

        #region Конфиг
        private Configuration config;
        private class Configuration
        {
            [JsonProperty("Xp за убийства игроков")] public int KillPlayer = 50;
            [JsonProperty("Xp за добычу дерева")] public int ExtractionWood = 1;
            [JsonProperty("Xp за добычу камня")] public int ExtractionStones = 1;
            [JsonProperty("Xp за добычу металла")] public int ExtractionMetall = 1;
            [JsonProperty("Xp за добычу серы")] public int ExtractionSulfur = 1;
            [JsonProperty("Xp за добычу металла высокого качества")] public int ExtractionMetallHQ = 4;
            [JsonProperty("Предметы доступные для покупки")] public List<ShopItems> ShopItems;

            public static Configuration GetNewCong()
            {
                return new Configuration
                {
                    ShopItems = new List<ShopItems>
                    {
                        new ShopItems
                        {
                            ShortName = "rifle.ak",
                            Description = "описание",
                            Amount = 1,
                            Amount2 = 2,
                            Price = 50,
                            Price2 = 30
                        },
                        new ShopItems
                        {
                            ShortName = "sulfur",
                            Description = "описание",
                            Amount = 1000,
                            Amount2 = 5,
                            Price = 30,
                            Price2 = 20
                        },
                        new ShopItems
                        {
                            ShortName = "wood",
                            Description = "описание",
                            Amount = 1000,
                            Amount2 = 10,
                            Price = 70,
                            Price2 = 60
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
                if (config?.ShopItems == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Что то с этим конфигом не так! 'oxide/config/{Name}', создаём новую конфигурацию!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Команды
        [ChatCommand("shop")]
        private void ShopXp(BasePlayer player, string command, string[] args)
        {
            DrawUI(player, 1);
        }

        [ConsoleCommand("UI_Page")]
        private void CmdConsolePage(ConsoleSystem.Arg args)
        {
            string name = args.Args[0];
            BasePlayer player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                int page = 1;
                if (int.TryParse(args.Args[0], out page) && page > 0 && (page - 1) * 12 <= config.ShopItems.Count)
                {
                    DrawUI(player, page);
                }
                else if (page == -999)
                {
                    CuiHelper.DestroyUi(player, Layer);
                }
            }
        }

        [ConsoleCommand("item")]
        private void CmdXpBuy(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "buy")
                {
                    if (args.HasArgs(2))
                    {
                        var item = config.ShopItems.FirstOrDefault(p => p.ShortName == args.Args[1]);
                        if (Item != null)
                        {
                            if (XPShops[player.userID].XP >= item.Price)
                            {
                                Effect.server.Run("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player.transform.position);
                                XPShops[player.userID].XP -= item.Price;

                                CuiElementContainer container = new CuiElementContainer();
                                CuiHelper.DestroyUi(player, Layer + "balance");
                                container.Add(new CuiButton
                                {
                                    RectTransform = { AnchorMin = "0 0.92", AnchorMax = $"1 1", OffsetMax = "0 0" },
                                    Button = { Color = "0 0 0 0" },
                                    Text = { Text = $"ВАШ БАЛАНС: {XPShops[player.userID].XP}XP", Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter }
                                }, Layer, Layer + "balance");

                                CuiHelper.AddUi(player, container);
                                player.inventory.GiveItem(ItemManager.CreateByName(item.ShortName, item.Amount));
                                player.SendConsoleCommand("note.inv " + ItemManager.FindItemDefinition(item.ShortName).itemid + " " + item.Amount);
                            }
                            else
                            {
                                Effect.server.Run("assets/prefabs/npc/autoturret/effects/targetlost.prefab", player.transform.position);
                                return;
                            }
                        }
                    }
                }
                else if (args.Args[0] == "sell")
                {
                    var item = config.ShopItems.FirstOrDefault(p => p.ShortName == args.Args[1]);
                    var sticks = player.inventory.GetAmount(ItemManager.FindItemDefinition(item.ShortName).itemid);
                    if (sticks >= item.Amount2)
                    {
                        player.inventory.Take(null, ItemManager.FindItemDefinition(item.ShortName).itemid, item.Amount2);
                    }
                    else
                    {
                        Effect.server.Run("assets/prefabs/npc/autoturret/effects/targetlost.prefab", player.transform.position);
                        return;
                    }
                    Effect.server.Run("assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab", player.transform.position);
                    XPShops[player.userID].XP += item.Price2;

                    CuiElementContainer container = new CuiElementContainer();
                    CuiHelper.DestroyUi(player, Layer + "balance");
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0.92", AnchorMax = $"1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0" },
                        Text = { Text = $"ВАШ БАЛАНС: {XPShops[player.userID].XP}XP", Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter }
                    }, Layer, Layer + "balance");

                    CuiHelper.AddUi(player, container);
                }
            }
        }
        #endregion

        #region Оксид
        private Dictionary<ulong, Xp> XPShops;
        private void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("XP/Player"))
            {
                XPShops = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Xp>>("XP/Player");
            }
            else
            {
                XPShops = new Dictionary<ulong, Xp>();
            }

            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
            timer.Every(30, SaveData);

            SaveConfig();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!XPShops.ContainsKey(player.userID))
            {
                XPShops.Add(player.userID, new Xp
                {
                    Name = player.displayName.ToUpper(),
                    XP = 0
                });
            }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("XP/Player", XPShops);
        }

        void OnEntityDeath(BasePlayer player, HitInfo info)
        {
            if (info.InitiatorPlayer != null) player = info.InitiatorPlayer;
            XPShops[player.userID].XP += config.KillPlayer;
            return;
        }

        void OnPlantGather(PlantEntity plant, Item item, BasePlayer player)
        {
            ProcessItem(player, item);
            return;
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            ProcessItem(player, item);
            return;
        }

        void ProcessItem(BasePlayer player, Item item)
        {
            switch (item.info.shortname)
            {
                case "wood":
                    XPShops[player.userID].XP += config.ExtractionWood;
                    return;
                    break;
                case "stones":
                    XPShops[player.userID].XP += config.ExtractionStones;
                    return;
                    break;
                case "metal.ore":
                    XPShops[player.userID].XP += config.ExtractionMetall;
                    return;
                    break;
                case "sulfur.ore":
                    XPShops[player.userID].XP += config.ExtractionSulfur;
                    return;
                    break;
                case "hq.metal.ore":
                    XPShops[player.userID].XP += config.ExtractionMetallHQ;
                    return;
                    break;
            }
        }
        #endregion

        #region Интерфейс
        private void DrawUI(BasePlayer player, int page = 1)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            float gap = -0.0f;
            float width = 0.31f;
            float height = 0.2f;
            float startxBox = 0.03f;
            float startyBox = 0.91f - height;
            float xmin = startxBox;
            float ymin = startyBox;
            int current = 1;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" }
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-2 -2", AnchorMax = "2 2", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.3", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.92", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"ВАШ БАЛАНС: {XPShops[player.userID].XP}XP", Font = "robotocondensed-regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter }
            }, Layer, Layer + "balance");

            #region Скип страниц
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.34 0.05", AnchorMax = $"0.407 0.1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = $"UI_Page {page - 1}" },
                Text = { Text = $"<", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.41 0.05", AnchorMax = $"0.59 0.1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"Страница: {page}", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.593 0.05", AnchorMax = $"0.66 0.1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = $"UI_Page {page + 1}" },
                Text = { Text = $">", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
            }, Layer);
            #endregion 

            foreach (var check in config.ShopItems.Skip((page - 1) * 12).Take(12))
            {
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = xmin + " " + ymin,
                        AnchorMax = (xmin + width) + " " + (ymin + height *1),
                        OffsetMax = "-1 -1",
                        OffsetMin = "5 5",
                    },
                    Button = { Color = "1 1 1 0.03", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = $"", Align = TextAnchor.LowerRight, Font = "robotocondensed-bold.ttf", FontSize = 12 }
                }, Layer, $".{check.ShortName}");
                xmin += width + gap;

                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height + gap;
                }

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"0.4 1", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.01" },
                    Text = { Text = $"", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
                }, $".{check.ShortName}", ".Image");

                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = ".Image",
                    Components =
                    {
                        new CuiRawImageComponent { FadeIn = 0.3f, Png = (string) ImageLibrary.Call("GetImage", check.ShortName)},
                        new CuiRectTransformComponent { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.41 0.405", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = $"{check.Description}", Color = HexToUiColor("#FFFFFF5A"), Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter }
                }, $".{check.ShortName}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.41 0.26", AnchorMax = $"0.7 0.4", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.03" },
                    Text = { Text = $"ПОКУПКА", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter }
                }, $".{check.ShortName}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.71 0.26", AnchorMax = $"1 0.4", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.03" },
                    Text = { Text = $"ПРОДАЖА", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter }
                }, $".{check.ShortName}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.41 0", AnchorMax = $"0.7 0.25", OffsetMax = "0 0" },
                    Button = { Color = HexToUiColor("#7ed1587A"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Command = $"item buy {check.ShortName}" },
                    Text = { Text = $"Цена: {check.Price}xp\nКоличество: {check.Amount}", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter }
                }, $".{check.ShortName}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.71 0", AnchorMax = $"1 0.25", OffsetMax = "0 0" },
                    Button = { Color = HexToUiColor("#d158587A"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Command = $"item sell {check.ShortName}" },
                    Text = { Text = $"Цена: {check.Price2}xp\nКоличество: {check.Amount2} ", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter }
                }, $".{check.ShortName}");

                current++;
                if (current > 12)
                {
                    break;
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Хелпер
        private static string HexToUiColor(string hex)
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