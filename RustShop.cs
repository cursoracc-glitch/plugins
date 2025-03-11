using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RustShop", "OxideBro", "1.0.3")]
    public class RustShop : RustPlugin
    {
        [PluginReference]
        Plugin ImageLibrary;

        #region Variables
        class DataStorage
        {
            public Dictionary<ulong, PlayersBalances> PlayerBalance = new Dictionary<ulong, PlayersBalances>();
            public DataStorage() { }
        }

        class PlayersBalances
        {
            public string Name;
            public int Balances;
            public int Time;
        }

        DataStorage data;
        private List<string> ListedCategories = new List<string>();
        private class ProductsData
        {
            [JsonProperty("Название предмета")]
            public string Name;
            [JsonProperty("Категория предмета")]
            public string Category;
            [JsonProperty("Стоимость предмета")]
            public int Price;
            [JsonProperty("Количество предмета")]
            public int Amount;
            [JsonProperty("Система. Короткое название предмета")]
            public string ShortName;
        }

        public int StartBalance = 0;
        public int HourAmount = 15;

        private List<ProductsData> shopElements = new List<ProductsData>();

        static List<string> permisions = new List<string>();

        static int GetDiscountSize(BasePlayer player)
        {
            for (int i = permisions.Count - 1; i >= 0; i--)
                if (PermissionService.HasPermission(player, permisions[i]))
                    return Convert.ToInt32(permisions[i].Replace("rustshop.discount", ""));
            return 0;
        }
        #endregion

        #region Config
        bool BPEnabled = false;
        double BPPrice = 1.0;
        string AMin = "0.01244507 0.08203125";
        string AMax = "0.1537335 0.1210937";

        private void LoadConfigValues()
        {
            bool changed = false;
            var _permisions = new List<object>()
            {
                {"rustshop.discount10"},
                {"rustshop.discount20"},
                {"rustshop.discount50"},
                {"rustshop.discount70"},
                {"rustshop.discount90"}
            };
            if (GetConfig("Основные", "Список привилегий и размера скидок (rustshop.discount99 - где 99 это размер скидки)", ref _permisions))
            {
                Puts("Привилегии созданы, Shop загружен!");
                changed = true;
            }
            permisions = _permisions.Select(p => p.ToString()).ToList();
            var _categories = new List<object>()
            {
                { "Testing" }
            };

            if (GetConfig("Основные", "Список категорий товаров", ref _categories))
            {
                Puts("Категории созданы, Shop загружен!");
                changed = true;
            }

            if (GetConfig("UI", "Button: AnchorMin", ref AMin))
            {
                Puts("Добавлены новые пункты в конфигурацию: AnchorMin");
                changed = true;
            }

            if (GetConfig("UI", "Button: AnchorMax", ref AMax))
            {
                Puts("Добавлены новые пункты в конфигурацию: AnchorMax");
                changed = true;
            }

            if (GetConfig("Основные", "Стартовый баланс игрока", ref StartBalance))
            {
                changed = true;
            }
            if (GetConfig("Основные", "Количество рублей за наигранный час", ref HourAmount))
            {
                changed = true;
            }

            if (GetConfig("Основные", "Включить к товарам продажу чертежей (Появиться дополнительная кнопка 'Чертёж')", ref BPEnabled))
            {
                PrintWarning("Добавлены новые пункты в конфигурацию: Включить к товарам продажу чертежей");
                changed = true;
            }
            if (GetConfig("Основные", "Цена чертежа: Цена предмета * Число (умножение)", ref BPPrice))
            {
                PrintWarning("Добавлены новые пункты в конфигурацию: Цена предмета * Число (умножение)");
                changed = true;
            }
            ListedCategories = _categories.Select(p => p.ToString()).ToList();
            if (changed)
                SaveConfig();
        }

        private bool GetConfig<T>(string MainMenu, string Key, ref T var)
        {
            if (Config[MainMenu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[MainMenu, Key], typeof(T));
                return false;
            }
            Config[MainMenu, Key] = var;
            return true;
        }
        #endregion

        #region Initialization
        private List<string> Available = new List<string>();

        private void OnServerInitialized()
        {
            LoadData();
            LoadConfig();
            LoadConfigValues();
            PermissionService.RegisterPermissions(this, permisions);
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            if (!plugins.Exists("ImageLibrary"))
            {
                PrintError("ImageLibrary can not be found! Can not continue");
                Interface.Oxide.UnloadPlugin("RustShop");
                return;
            }
            else
            {
                foreach (var check in ItemManager.itemList)
                    plugins.Find("ImageLibrary").CallHook("GetImage", check.shortname, 0, true);
            }
            foreach (var check in BasePlayer.activePlayerList)
            {
                OnPlayerInit(check);
            }
            foreach (var check in shopElements)
            {
                if (string.IsNullOrEmpty(check.Name))
                {
                    PrintError($"Внимание! У предмета не установлено название!");
                    continue;
                }
                if (check.Price == 0)
                    PrintError($"Внимание! У предмета: {check.Name} не установлена цена!");

                if (check.Amount == 0)
                    PrintError($"Внимание! У предмета: {check.Name} не установлено количество!");

                if (string.IsNullOrEmpty(check.Category))
                    PrintError($"Внимание! У предмета: {check.Name} не установлена категория!");
                if (!ListedCategories.Contains(check.Category))
                    PrintError($"Внимание! У предмета: {check.Name} не верная категория, предмет не будет отображен!");
            }
            var bplist = ItemManager.GetBlueprints();
            foreach (var bp in bplist)
            {
                if (bp.userCraftable && !bp.defaultBlueprint)
                {
                    Available.Add(bp.targetItem.shortname);
                }
            }
            timer.Every(30f, TimerHandler);
            timer.Every(360, SaveData);
        }

        Dictionary<BasePlayer, int> timers = new Dictionary<BasePlayer, int>();
        void TimerHandler()
        {
            foreach (var player in timers.Keys.ToList())
            {
                var seconds = timers[player] -= 30;
                if (seconds > 3600)
                {
                    data.PlayerBalance[player.userID].Time = data.PlayerBalance[player.userID].Time += 3600;
                    TimerHandler();
                    break;
                }
                if (seconds <= 0)
                {
                    timers.Remove(player);
                    ChangeBalance(player, "add", HourAmount);
                    data.PlayerBalance[player.userID].Time = data.PlayerBalance[player.userID].Time = 3600;
                    SaveData();
                    SendReply(player, $"Вам насчитано {HourAmount} рублей на игровой баланс магазина за активную игру на сервере\nЧто бы открыть магазин, используйте /shop");
                    timers.Add(player, data.PlayerBalance[player.userID].Time);
                }
            }
        }

        List<ulong> activePlayers = new List<ulong>();
        void DeactivateTimer(BasePlayer player)
        {
            data.PlayerBalance[player.userID].Time = timers[player];
            activePlayers.Remove(player.userID);
            timers.Remove(player);
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";
            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";
            return $"{units} {form3}";
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1f, () => OnPlayerInit(player));
                return;
            }
            if (!data.PlayerBalance.ContainsKey(player.userID))
            {
                data.PlayerBalance.Add(player.userID, new PlayersBalances()
                {
                    Name = player.displayName,
                    Balances = StartBalance,
                    Time = 3600,
                });
                SaveData();
            }
            if (data.PlayerBalance.ContainsKey(player.userID))
            {
                timers.Add(player, data.PlayerBalance[player.userID].Time);
                ActivateTimer(player.userID);
                DrawButton(player);
            }
        }

        void ActivateTimer(ulong userId)
        {
            if (!activePlayers.Contains(userId))
            {
                activePlayers.Add(userId);
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            DeactivateTimer(player);
        }

        void OnServerSave()
        {
            SaveData();
        }

        void SaveData()
        {
            foreach (var player in timers.Keys.ToList())
            {
                data.PlayerBalance[player.userID].Time = timers[player];
            }
            Interface.Oxide.DataFileSystem.WriteObject("RustShop/PlayersBalance", data);
        }

        void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<DataStorage>("RustShop/PlayersBalance");
            }
            catch
            {
                data = new DataStorage();
            }
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("RustShop/ShopItems"))
            {
                shopElements.Add(new ProductsData
                {
                    Name = "Тестовый предмет #1",
                    ShortName = "rifle.ak",
                    Category = "Testing",
                    Price = 1,
                    Amount = 10
                });
                shopElements.Add(new ProductsData
                {
                    Name = "Тестовый предмет #2",
                    ShortName = "rifle.ak",
                    Category = "Testing",
                    Price = 2,
                    Amount = 10
                });
                shopElements.Add(new ProductsData
                {
                    Name = "Тестовый предмет #3",
                    ShortName = "rifle.ak",
                    Category = "Testing",
                    Price = 3,
                    Amount = 10
                });
                Interface.Oxide.DataFileSystem.WriteObject("RustShop/ShopItems", shopElements);
                return;
            }
            shopElements = Interface.Oxide.DataFileSystem.ReadObject<List<ProductsData>>("RustShop/ShopItems");
        }

        void ChangeBalance(BasePlayer player, string mode, int Amount, bool change = false)
        {
            if (!data.PlayerBalance.ContainsKey(player.userID))
                data.PlayerBalance.Add(player.userID, new PlayersBalances()
                {
                    Name = "Добавлен по API",
                    Balances = 0,
                    Time = 0
                });
            if (mode == "add")
            {
                data.PlayerBalance[player.userID].Balances += Amount;
                if (!change) if (player != null) SendReply(player, Messages["MONEYADD"], Amount);
            }
            if (mode == "remove")
            {
                data.PlayerBalance[player.userID].Balances -= Amount;
                if (!change) if (player != null) SendReply(player, Messages["MONEYREMOVE"], Amount);
            }
            if (player != null)
            {
                DrawButton(player);
            }
        }

        private void GiveBlueprint(BasePlayer player, string itemkey, int amount)
        {
            Item item = null;
            if (!Available.Contains(itemkey))
            {
                Puts(itemkey);
                return;
            }
            item = ItemManager.CreateByItemID(-996920608, amount);

            item.blueprintTarget = ItemManager.itemList.Find(x => x.shortname == itemkey)?.itemid ?? 0;
            player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "shopbp");
                CuiHelper.DestroyUi(player, "shop.bp");
            }
            SaveData();
        }
        #endregion

        #region Commands
        [ConsoleCommand("shop_changebalance")]
        void cmdChangeBalance(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (args.Connection != null)
                if (!player.IsAdmin) return;
            if (args == null || args.Args.Length != 3 && args.Args[1] != "balance")
            {
                Puts("Вы не верно ввели команду, используйте: shop_changebalance Name/SteamID add/remove Count");
                return;
            }
            var findPlayer = (BasePlayer.Find(args.Args[0]) ?? BasePlayer.FindSleeping(args.Args[0]));
            if (findPlayer == null)
            {
                Puts($"Игрок {args.Args[0]} не найден в списке игроков");
                return;
            }
            if (!data.PlayerBalance.ContainsKey(findPlayer.userID))
            {
                data.PlayerBalance.Add(findPlayer.userID, new PlayersBalances()
                {
                    Name = "Добавлен по API",
                    Balances = 0,
                    Time = 0
                });
            }
            if (args.Args[1] == "balance")
            {
                Puts($"Баланс игрока {findPlayer}: {data.PlayerBalance[findPlayer.userID].Balances}");
                return;
            }
            int change;
            if (!int.TryParse(args.Args[2], out change))
            {
                Puts("В сумме необходимо ввести число");
                return;
            }
            if (args.Args[1] != "add" && args.Args[1] != "remove" && args.Args[1] != "balance")
            {
                Puts("Вы не верно указали вид пополнения, используйте: add либо remove");
                return;
            }
            switch (args.Args[1])
            {
                case "add":
                    ChangeBalance(findPlayer, "add", change);
                    Puts($"Игроку {findPlayer} пополнен баланс на: {change}. Баланс игрока: {data.PlayerBalance[findPlayer.userID].Balances}");
                    break;
                case "remove":
                    if (data.PlayerBalance[findPlayer.userID].Balances < change)
                    {
                        Puts($"Баланс игрока ({data.PlayerBalance[findPlayer.userID].Balances}) меньше чем вы указали ({change}). Баланс не изменен");
                        return;
                    }
                    ChangeBalance(findPlayer, "remove", change);
                    Puts($"C баланса игрока {findPlayer} удалено: {change}. Баланс игрока: {data.PlayerBalance[findPlayer.userID].Balances}");
                    break;
            }
        }

        [ChatCommand("shop")]
        void cmdChatShop(BasePlayer player, string command, string[] args) => ShopGUI(player);

        [ConsoleCommand("shop")]
        void cmdConsoleShop(ConsoleSystem.Arg args)
        {
            ShopGUI(args.Player(), args.FullString);
        }

        [ConsoleCommand("product_info")]
        void cmdProductInfo(ConsoleSystem.Arg args)
        {
            string name = args.Args[0];
            var player = args.Player();
            DrawProductInfo(player, name);
        }

        [ConsoleCommand("Buy")]
        void cmdConsoleBuy(ConsoleSystem.Arg args)
        {
            string name = args.Args[0];
            var player = args.Player();
            ProductsData probable = shopElements.Find(p => p.ShortName == name);
            var dicsount = (probable.Price * GetDiscountSize(player) / 100);
            int price = (probable.Price - dicsount);
            if (probable == null)
            {
                SendReply(player, Messages["ERROR"]);
                return;
            }
            switch (args.Args[1])
            {
                case "bp":
                    if (price > data.PlayerBalance[player.userID].Balances)
                    {
                        SendReply(player, Messages["NOMONEY"]);
                        return;
                    }
                    if (args.Player().inventory.containerMain.itemList.Count == 24)
                    {
                        SendReply(player, Messages["FULLINV"]);
                        return;
                    }
                    GiveBlueprint(player, probable.ShortName, 1);
                    double bpprice = (price * BPPrice);
                    ChangeBalance(player, "remove", (int)bpprice, true);
                    SendReply(player, Messages["BUY"], probable.Name + " (Чертёж)", 1);
                    ShopGUI(args.Player(), probable.Category);
                    break;
                case "main":
                    if (price > data.PlayerBalance[player.userID].Balances)
                    {
                        SendReply(player, Messages["NOMONEY"]);
                        return;
                    }
                    if (args.Player().inventory.containerMain.itemList.Count == 24)
                    {
                        SendReply(player, Messages["FULLINV"]);
                        return;
                    }
                    Item x = ItemManager.CreateByPartialName(probable.ShortName, probable.Amount);
                    player.GiveItem(x, BaseEntity.GiveItemReason.PickedUp);
                    ChangeBalance(player, "remove", price, true);
                    SendReply(player, Messages["BUY"], probable.Name , probable.Amount);
                    ShopGUI(args.Player(), probable.Category);
                    break;
            }
        }
        #endregion

        #region GUI
        string Button = "[{\"name\":\"shopbp\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.2705882 0.5137255 0.7137255 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"{min}\",\"anchormax\":\"{max}\"}]},{\"name\":\"balance_bp\",\"parent\":\"shopbp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"1 0.6431373 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.3510639 0.97\"}]},{\"name\":\"balance\",\"parent\":\"balance_bp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"<b>{B} р.</b>\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2595528\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"shopbp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Магазин: <b>/shop</b>\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2706043\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3575131 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"shopbp\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"shop\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"shopbp\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0 0 0 0.6545441\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.99 0.05\"}]}]";

        void DrawButton(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => DrawButton(player));
            }
            CuiHelper.DestroyUi(player, "shopbp");
            CuiHelper.AddUi(player, Button
                .Replace("{B}", data.PlayerBalance[player.userID].Balances.ToString()).Replace("{min}", AMin).Replace("{max}", AMax));
        }
        //shop.bp
        string Product = "[{\"name\":\"product_2\",\"parent\":\"shop.bp\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0 0 0 0.95\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"product_3\",\"parent\":\"product_2\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"1 0.6431373 0 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3 0.36\",\"anchormax\":\"0.7 0.6953125\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"product_4\",\"parent\":\"product_3\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.2666667 0.509804 0.7098039 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.4 0.997\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"product_5\",\"parent\":\"product_4\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.15 0.01709393\",\"anchormax\":\"0.85 0.9829058\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"product_6\",\"parent\":\"product_3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Цена: {price}\nСкидка: {dis}\nКол-во: {count}\",\"fontSize\":17},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.5411765\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4469253 0.3359739\",\"anchormax\":\"0.9959737 0.7825413\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"product_8\",\"parent\":\"product_3\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"product_2\",\"color\":\"1 0 0 0.790022\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.7941908\",\"anchormax\":\"0.09553441 0.997\"}]},{\"name\":\"product_9\",\"parent\":\"product_8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"X\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"product_6\",\"parent\":\"product_3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Купить:\",\"fontSize\":18,\"align\":\"UpperCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4417135\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4011713 0.238894\",\"anchormax\":\"0.9959737 0.38\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"product_6\",\"parent\":\"product_3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{name}\",\"fontSize\":21,\"align\":\"UpperCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.4417135\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4011713 0.8446723\",\"anchormax\":\"0.9959737 0.9922338\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        void DrawProductInfo(BasePlayer player, string product)
        {
            CuiHelper.DestroyUi(player, "product_2");
            ProductsData probable = shopElements.Find(p => p.ShortName == product);
            if (probable == null) return;
            var dicsount = (probable.Price * GetDiscountSize(player) / 100);
            int price = (probable.Price - dicsount);
            var text = dicsount > 0 ? $"<color=#FB7578>{price}P</color>" : $"<color=0.27 0.51 0.71 0.5>{price}P</color>";
            bool bp;
            if (BPEnabled)
            {
                bp = Available.Contains(probable.ShortName);
            }
            else
            {
                bp = false;
            }
            double bpprice = (price * BPPrice);
            if (bp) text = text + $" | Чертёж: <color=#FB7578>{bpprice}Р</color>";
            CuiHelper.AddUi(player, Product
                .Replace("{png}", (string)ImageLibrary.Call("GetImage", probable.ShortName)).Replace("{name}", probable.Name)
                .Replace("{price}", text).Replace("{dis}", GetDiscountSize(player).ToString() + "%").Replace("{count}", probable.Amount.ToString()));
            var container = new CuiElementContainer();
           
            if (BPEnabled)
            {
                var size = !bp ? "0.995" : "0.7";
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.405 0.01", AnchorMax = $"{size} 0.2" },
                    Button = { Color = "0.20 0.38 0.53 1.00", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "Assets/Content/UI/UI.Background.Tile.psd", Command = $"Buy {probable.ShortName} main" },
                    Text = { Text = $"✔ Предмет", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter }

                }, "product_3", "product_2" + "." + probable.ShortName);
                if (bp)
                    
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.71 0.01", AnchorMax = "0.9965 0.2" },
                        Button = { Color = "0.27 0.51 0.71 1", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "Assets/Content/UI/UI.Background.Tile.psd", Command = $"Buy {probable.ShortName} bp" },
                        Text = { Text = $"★ Чертёж", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter }
                    }, "product_3", "product_2" + "." + probable.ShortName);
            }
            else
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.405 0.01", AnchorMax = "0.995 0.2" },
                    Button = { Color = "0.27 0.51 0.71 1", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "Assets/Content/UI/UI.Background.Tile.psd", Command = $"Buy {probable.ShortName} main" },
                    Text = { Text = $"✔ Предмет", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter }

                }, "product_3", "product_2" + "." + probable.ShortName);
            }
            CuiHelper.AddUi(player, container);
        }

        string GUI = "[{\"name\":\"shop.bp\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0.3903706\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.1 0.17\",\"anchormax\":\"0.9 0.83\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"shop.title\",\"parent\":\"shop.bp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.2705882 0.509804 0.7058824 0.83351\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.9281093\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"shop.titletext\",\"parent\":\"shop.title\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{TITLE}\",\"fontSize\":23,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.8342283\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"menuBp\",\"parent\":\"shop.bp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0.0627451\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.2307114 0.9281093\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp1\",\"parent\":\"menuBp\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"1 0.6431373 0 0.6117647\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.02999998 0.1283876\",\"anchormax\":\"0.95 0.27\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp1text\",\"parent\":\"bp1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"БАЛАНС - {B} Р\nСкидка: {C}%\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2980392\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp1\",\"parent\":\"menuBp\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"close\":\"shop.bp\",\"color\":\"0.6795254 0 0 0.9493701\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.03 0.02210394\",\"anchormax\":\"0.95 0.1028782\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"bp1text\",\"parent\":\"bp1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"ВЫХОД\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2980392\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"dinamicitems\",\"parent\":\"shop.bp\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2307114 0\",\"anchormax\":\"0.9999998 0.9261365\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        private void ShopGUI(BasePlayer player, string name = "")
        {
            CuiHelper.DestroyUi(player, "shop.bp");
            string color = "0.27 0.51 0.71 1.00";
            if (name == "") name = ListedCategories.First();
            CuiHelper.AddUi(player, GUI
                .Replace("{TITLE}", Messages["TITLE"])
                .Replace("{B}", data.PlayerBalance[player.userID].Balances.ToString())
                .Replace("{C}", GetDiscountSize(player).ToString()));
            var container = new CuiElementContainer();
            var reply = 540;
            if (reply == 0) { }
            float gap = +0.0f;
            float width = 0.195f;
            float height = 0.32f;
            float startxBox = 0.01f;
            float startyBox = 0.97f - height;
            float xmin = startxBox;
            float ymin = startyBox;
            int current = 1;

            float gap1 = +0.001f;
            float width1 = 0.98f;
            float height1 = 0.1f;
            float startxBox1 = 0.0f;
            float startyBox1 = 0.99f - height1;
            float xmin1 = startxBox1;
            float ymin1 = startyBox1;
            int categories = 1;
            foreach (var cetegories in ListedCategories)
            {
                categories++;
                color = name == cetegories ? "1.00 0.64 0.00 1.00" : "0.27 0.51 0.71 1";
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                       AnchorMin = xmin1 + " " + ymin1,
                        AnchorMax = (xmin1 + width1) + " " + (ymin1 + height1 *1),
                        OffsetMax = "-1 -1",
                        OffsetMin = "5 5",
                    },
                    Button = { Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "Assets/Content/UI/UI.Background.Tile.psd", Color = color, Command = $"shop {cetegories}" },
                    Text = { Text = $"{cetegories}", Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter }
                },
                 "menuBp", "menuBp" + "." + cetegories);
                xmin1 += width1 + gap1;
                if (xmin1 + width1 >= 1)
                {
                    xmin1 = startxBox1;
                    ymin1 -= height1 + gap1;
                }
                if (categories > 7) break;
            }

            foreach (var check in shopElements.Where(p => p.Category.ToString() == name))
            {
                var dicsount = (check.Price * GetDiscountSize(player) / 100);
                int price = (check.Price - dicsount);
                var pricecolor = "0.27 0.51 0.71 0.5";
               
                var text = dicsount > 0 ? $"Цена: <color=#FB7578>{price}P</color> <size=12>(-{GetDiscountSize(player)}%)</size>" : $"Цена: <color={pricecolor}>{price}P</color>";

                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                       AnchorMin = xmin + " " + ymin,
                        AnchorMax = (xmin + width) + " " + (ymin + height),
                        OffsetMax = "-1 -1",
                        OffsetMin = "5 5",
                    },
                    Button = { Color = "0.00 0.00 0.00 0.4", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "Assets/Content/UI/UI.Background.Tile.psd", },
                    Text = { Text = $"", Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.LowerCenter, FadeIn = 0.5f }
                }, "dinamicitems", "dinamicitems" + "." + check.ShortName);

                xmin += width + gap;

                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height + gap;
                }
                container.Add(new CuiElement()
                {
                    Parent = "dinamicitems" + "." + check.ShortName,
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.ShortName), FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0.15 0.1", AnchorMax = "0.85 1" }
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1" },
                    Button = { Color = "0.27 0.51 0.71 0.7", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "Assets/Content/UI/UI.Background.Tile.psd", },
                    Text = { Text = text, Font = "robotocondensed-bold.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter }
                }, "dinamicitems" + "." + check.ShortName);
              
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.0 0", AnchorMax = "0.997 0.2" },
                        Button = { Color = "1.00 0.64 0.00 1.00", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "Assets/Content/UI/UI.Background.Tile.psd", Command = $"product_info {check.ShortName}" },
                        Text = { Text = $"Купить ({check.Amount} шт.)", Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter }

                    }, "dinamicitems" + "." + check.ShortName);
                current++;
                if (current == 16)
                {
                    break;
                }
            }
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region API
        private void AddBalance(ulong userId, int Amount)
        {
            if (!data.PlayerBalance.ContainsKey(userId))
                data.PlayerBalance.Add(userId, new PlayersBalances()
                {
                    Name = "Добавлен по API",
                    Balances = 0,
                    Time = 0
                });
            var player = BasePlayer.FindByID(userId);
            ChangeBalance(player, "add", Amount);
            if (player != null)
            {
                SendReply(player, $"Вам насчитано {Amount} рублей на игровой баланс магазина\nПроверка и использование баланса /shop");
            }
        }

        private object RemoveBalance(ulong userId, int Amount)
        {
            if (!data.PlayerBalance.ContainsKey(userId))
                data.PlayerBalance.Add(userId, new PlayersBalances()
                {
                    Name = "Добавлен по API",
                    Balances = 0,
                    Time = 0
                });
            if (data.PlayerBalance[userId].Balances < Amount)
            {
                return false;
            }
            var player = BasePlayer.FindByID(userId);
            ChangeBalance(player, "remove", Amount);

            if (player != null)
            {
                SendReply(player, $"У Вас снято {Amount} рублей с игрового баланса");
            }
            return true;
        }

        private object GetBalance(ulong userId)
        {
            if (!data.PlayerBalance.ContainsKey(userId))
                data.PlayerBalance.Add(userId, new PlayersBalances()
                {
                    Name = "Добавлен по API",
                    Balances = 0,
                    Time = 0
                });
            return data.PlayerBalance[userId].Balances;
        }
        #endregion

        #region Permissions
        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                if (player == null || string.IsNullOrEmpty(permissionName))
                    return false;

                var uid = player.UserIDString;
                if (permission.UserHasPermission(uid, permissionName))
                    return true;

                return false;
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");

                foreach (var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }
        #endregion

        #region Messages
        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"TITLE", "ИГРОВОЙ МАГАЗИН RUSTPLUGIN.RU" },
            {"ERROR", "Администрация допустила ошибку! Предмет не был куплен!" },
            {"BUY", "Вы успешно приобрели предмет: {0} [{1}x]" },
            {"FULLINV", "У вас недостаточно места в инвентаре, освободите место!" },
            {"NOMONEY", "У вас недостаточно бонусов на балансе!" },
            {"MONEYADD", "Вам насчитано {0} рублей на внутриигровой баланс магазина\nПроверка и использование баланса /shop" },
            {"MONEYREMOVE", "C Вашего баланса внутриигрового магазина снято {0} рублей" }
        };
        #endregion
    }
}