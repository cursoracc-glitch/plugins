using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("CaseSystem", "Topplugin.ru", "3.0.0")]
    class CaseSystem : RustPlugin
    {
        #region Вар
        string Layer = "Case_UI";

        [PluginReference] Plugin ImageLibrary, XpSystem;

        Dictionary<ulong, PlayerInventory> Settings = new Dictionary<ulong, PlayerInventory>();

        private static CaseSystem inst;
        #endregion

        #region Класс
        public class CaseSettings
        {
            [JsonProperty("ID кейса")] public string Id;
            [JsonProperty("Название кейса")] public string DisplayName;
            [JsonProperty("Цена кейса")] public float Count;
            [JsonProperty("Изображение кейса")] public string Url;
            [JsonProperty("Список предметов")] public List<Items> items;
        }

        public class Items
        {
            [JsonProperty("Название предмета/команды")] public string DisplayName;
            [JsonProperty("Короткое название предмета")] public string ShortName;
            [JsonProperty("Минимальное количество при выпадени")] public int AmountMin;
            [JsonProperty("Максимальное Количество при выпадени")] public int AmountMax;
            [JsonProperty("Команда")] public string Command;
            [JsonProperty("Изображение")] public string Url;
            public int GetRandomAmount() => Core.Random.Range(AmountMin, AmountMax);
        }

        private class InventoryItem
        {
            [JsonProperty("Название предмета/команды")] public string DisplayName;
            [JsonProperty("Короткое название предмета")] public string ShortName;
            [JsonProperty("Кол-во предмета")] public int Amount;
            [JsonProperty("Команда")] public string Command;
            [JsonProperty("Изображение")] public string Url;

            public Item GiveItem(BasePlayer player)
            {
                if (!string.IsNullOrEmpty(Command)) inst.Server.Command(Command.Replace("%STEAMID%", player.UserIDString));
                if (!string.IsNullOrEmpty(ShortName))
                {
                    Item item = ItemManager.CreateByPartialName(ShortName, Amount);

                    return item;
                }
                return null;
            }

            public static InventoryItem Generate(Items items)
            {
                return new InventoryItem
                {
                    DisplayName = items.DisplayName,
                    ShortName = items.ShortName,
                    Amount = items.GetRandomAmount(),
                    Command = items.Command,
                    Url = items.Url
                };
            }
        }

        private class PlayerInventory
        {
            [JsonProperty("Список вещей")] public List<InventoryItem> Inventory = new List<InventoryItem>();
        }
        #endregion

        #region Конфиг
        Configuration config;
        class Configuration 
        {
            [JsonProperty("Настройки кейсов")] public List<CaseSettings> settings;
            public static Configuration GetNewConfig() 
            {
                return new Configuration
                {
                    settings = new List<CaseSettings>()
                    {
                        new CaseSettings
                        {
                            Id = "1",
                            DisplayName = "Кейс 1",
                            Count = 100,
                            Url = "",
                            items = new List<Items>()
                            {
                                new Items
                                {
                                    DisplayName = "Дерево",
                                    ShortName = "wood",
                                    AmountMin = 1000,
                                    AmountMax = 2000,
                                    Command = null,
                                    Url = null
                                }
                            }
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
            PrintWarning("\n-----------------------------\n" +
            "     Author - https://topplugin.ru/\n" +
            "     VK - https://vk.com/rustnastroika\n" +
            "     Discord - https://discord.com/invite/5DPTsRmd3G\n" +
            "-----------------------------");
            inst = this;
            foreach (var check in config.settings)
            {
                ImageLibrary.Call("AddImage", check.Url, check.Url);
                foreach (var item in check.items)
                    ImageLibrary.Call("AddImage", item.Url, item.Url);
            }
            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        void OnPlayerConnected(BasePlayer player) =>  CreateDataBase(player);

        void OnPlayerDisconnected(BasePlayer player, string reason) => SaveDataBase(player.userID);

        void Unload() 
        {
            foreach(var check in Settings)
                SaveDataBase(check.Key);

            foreach(var check in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(check, Layer);
        }
        #endregion

        #region Методы
        void CreateDataBase(BasePlayer player)
        {
            var DataBase = Interface.Oxide.DataFileSystem.ReadObject<PlayerInventory>($"CaseSystem/{player.userID}");
            
            if (!Settings.ContainsKey(player.userID))
                Settings.Add(player.userID, new PlayerInventory());
             
            Settings[player.userID] = DataBase ?? new PlayerInventory();
        }

        void SaveDataBase(ulong userId) => Interface.Oxide.DataFileSystem.WriteObject($"CaseSystem/{userId}", Settings[userId]);

        InventoryItem AddItem(BasePlayer player, Items items)
        {
            var item = InventoryItem.Generate(items);
            Settings[player.userID].Inventory.Add(item);
            return item;
        }

        void UpdateBalance(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Balance");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.203 0.54", AnchorMax = $"0.397 0.585", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1" },
                Text = { Text = $"Ваш баланс: {XpSystem.Call("API_GetXp", player.userID)}xp", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer, "Balance");
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Команды
        [ConsoleCommand("case")]
        void ConsoleCase(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "check")
                {
                    CaseUI(player, args.Args[1]);
                }
                if (args.Args[0] == "open")
                {
                    var check = config.settings.FirstOrDefault(z => z.Id == args.Args[1]);
                    if ((float)XpSystem.Call("API_GetXp", player.userID) >= check.Count)
                    {
                        var items = check.items.ToList().GetRandom();
                        XpSystem.Call("API_ShopRemBalance", player.userID, check.Count);
                        UpdateBalance(player);
                        RewardUI(player, items);
                    }
                }
                if (args.Args[0] == "inventory")
                {
                    InventoryUI(player);
                }
                if (args.Args[0] == "take")
                {
                    var item = Settings[player.userID].Inventory.ElementAt(int.Parse(args.Args[1]));
                    if (item.ShortName != null)
                    {
                        if (player.inventory.containerMain.itemList.Count >= 24)
                        {
                            player.ChatMessage($"<color=#8fde5b><size=16>Кейсы:</size></color>\nУ вас <color=#8fde5b>недостаточно</color> места в основном инвентаре!");
                            return;
                        }
                    }
                    var text = item.Command != null ? $"<color=#8fde5b><size=16>Кейсы:</size></color>\nВы получили услугу: <color=#8fde5b>{item.DisplayName}</color>" : $"<color=#8fde5b><size=16>Кейсы:</size></color>\nВы получили предмет: <color=#8fde5b>{item.DisplayName}</color>\nВ размере: <color=#8fde5b>{item.Amount}шт.</color>";
                    SendReply(player, text);
                    item.GiveItem(player)?.MoveToContainer(player.inventory.containerMain);
                    Settings[player.userID].Inventory.Remove(item);
                    InventoryUI(player);
                }
                if (args.Args[0] == "skip")
                {
                    InventoryUI(player, int.Parse(args.Args[1]));
                }
            }
        }
        #endregion

        #region Интерфейс
        void CaseUI(BasePlayer player, string Id = "1")
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            var cases = config.settings.FirstOrDefault(z => z.Id == Id);
            int ItemCount = cases.items.Count(), CountItem = 0, Count = 6;
            float Position = 0.5f, Width = 0.1f, Height = 0.115f, Margin = 0.005f, MinHeight = 0.3f;

            if (ItemCount >= Count) Position = 0.5f - Count / 2f * Width - (Count - 1) / 2f * Margin;
            else Position = 0.5f - ItemCount / 2f * Width - (ItemCount - 1) / 2f * Margin;
            ItemCount -= Count;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.289 0", AnchorMax = "0.945 1", OffsetMax = "0 0" },
                Image = { Color = "0.117 0.121 0.109 0.95" },
            }, "Menu_UI", Layer);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 0.97", OffsetMax = "0 0" },
                Text = { Text = $"<b><size=45>Кейсы</size></b>\nПривет {player.displayName}, ты открыл меню рулетки в которой ты найдешь множество кейсов и сможешь испытать свою удачу\nКаждый кейс имеет свое назначение и за каждый из них тебе предстоит заплатить!\nПополнить баланс ты можешь добывая ресурсы, убивая игроков, сбивая вертолеты, взрывая танки!", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.203 0.54", AnchorMax = $"0.397 0.585", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1" },
                Text = { Text = $"Ваш баланс: {XpSystem.Call("API_GetXp", player.userID)}xp", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer, "Balance");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.403 0.54", AnchorMax = $"0.598 0.585", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1", Command = "case inventory" },
                Text = { Text = $"Инвентарь", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.603 0.54", AnchorMax = $"0.798 0.585", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1", Command = $"case open {cases.Id}" },
                Text = { Text = $"ОТКРЫТЬ\n{cases.DisplayName}: {cases.Count}xp", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.43", AnchorMax = "1 0.51", OffsetMax = "0 0" },
                Text = { Text = $"<b><size=33>ИНФОРМАЦИЯ</size></b>\n{cases.DisplayName} - список доступных предметов", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer);

            float width = 0.2f, height = 0.21f, startxBox = 0.2f, startyBox = 0.8f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in config.settings)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Button = { Color = "1 1 1 0.1", Command = $"case check {check.Id}" },
                    Text = { Text = $"", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, Layer, "Images");

                container.Add(new CuiElement
                {
                    Parent = "Images",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.Url) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 10", OffsetMax = "-5 -10" }
                    }
                });
                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            foreach (var check in cases.items)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{Position} {MinHeight}", AnchorMax = $"{Position + Width} {MinHeight + Height}", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.1" },
                    Text = { Text = "" }
                }, Layer, "Items");

                var image = check.Command != null ? check.Url : check.ShortName;
                container.Add(new CuiElement
                {
                    Parent = "Items",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", image) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });

                var textAmount = check.AmountMin != check.AmountMax ? $"{check.AmountMin}-{check.AmountMax}" : $"{check.AmountMax}";
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = $"X{textAmount} ", Color = "1 1 1 0.5", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.LowerRight }
                }, "Items");

                CountItem += 1;
                if (CountItem % Count == 0)
                {
                    if (ItemCount > Count)
                    {
                        Position = 0.5f - Count / 2f * Width - (Count - 1) / 2f * Margin;
                        ItemCount -= Count;
                    }
                    else
                    {
                        Position = 0.5f - ItemCount / 2f * Width - (ItemCount - 1) / 2f * Margin;
                    }
                    MinHeight -= ((Margin * 2) + Height);
                }
                else
                {
                    Position += (Width + Margin);
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void RewardUI(BasePlayer player, Items items)
        {
            CuiHelper.DestroyUi(player, "Reward");
            var container = new CuiElementContainer();
            var item = AddItem(player, items);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0.117 0.121 0.109 0.95", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
            }, Layer, "Reward");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.42 0.44", AnchorMax = "0.58 0.61", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.1" },
            }, "Reward", "Image");

            var image = items.Command != null ? items.Url : items.ShortName;
            container.Add(new CuiElement
            {
                Parent = "Image",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", image) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 10", OffsetMax = "-5 -10" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"{item.Amount}шт. ", Color = "1 1 1 0.5", Align = TextAnchor.LowerRight, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Image");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.42 0.4", AnchorMax = "0.58 0.435", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1", Close = "Reward" },
                Text = { Text = "ПРИНЯТЬ НАГРАДУ", Color = "1 1 1 0.5", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter }
            }, "Reward");

            CuiHelper.AddUi(player, container);
        }

        void InventoryUI(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, "Inventory");
            var container = new CuiElementContainer();

            int ItemCount = Settings[player.userID].Inventory.Count(), CountItem = 0, Count = 6;
            float Position = 0.5f, Width = 0.13f, Height = 0.155f, Margin = 0.005f, MinHeight = 0.74f;

            if (ItemCount >= Count) Position = 0.5f - Count / 2f * Width - (Count - 1) / 2f * Margin;
            else Position = 0.5f - ItemCount / 2f * Width - (ItemCount - 1) / 2f * Margin;
            ItemCount -= Count;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0.117 0.121 0.109 0.95" },
            }, Layer, "Inventory");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-2 -2", AnchorMax = "2 2", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = "Inventory" },
                Text = { Text = "" }
            }, "Inventory");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = Settings[player.userID].Inventory.Count() == 0 ? "0 0" : "0 0.9", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = "Inventory" },
                Text = { Text = Settings[player.userID].Inventory.Count() == 0 ? $"ИНВЕНТАРЬ ПУСТ" : "ИНВЕНТАРЬ", Color = "1 1 1 0.5", Font = "robotocondensed-bold.ttf", FontSize = Settings[player.userID].Inventory.Count() == 0 ? 60 : 40, Align = TextAnchor.MiddleCenter }
            }, "Inventory");

            if (page != 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.45", AnchorMax = $"0.06 0.55", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"case skip {page - 1}" },
                    Text = { Text = $"<", Color = "1 1 1 0.5", Font = "robotocondensed-bold.ttf", FontSize = 60, Align = TextAnchor.MiddleCenter }
                }, "Inventory");
            }

            if ((float)Settings[player.userID].Inventory.Count > (page + 1) * 30)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.94 0.45", AnchorMax = $"1 0.55", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"case skip {page + 1}" },
                    Text = { Text = $">", Color = "1 1 1 0.5", Font = "robotocondensed-bold.ttf", FontSize = 60, Align = TextAnchor.MiddleCenter }
                }, "Inventory");
            }

            var list = Settings[player.userID].Inventory.Skip(page * 30).Take(30);
            foreach (var check in list.Select((i, t) => new { A = i, B = t }))
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{Position} {MinHeight}", AnchorMax = $"{Position + Width} {MinHeight + Height}", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.1" },
                    Text = { Text = "" }
                }, "Inventory", $"{check.B}");

                var image = check.A.Command != null ? check.A.Url : check.A.ShortName;
                container.Add(new CuiElement
                {
                    Parent = $"{check.B}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", image) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"case take {check.B + page * 30}" },
                    Text = { Text = $"X{check.A.Amount} ", Color = "1 1 1 0.5", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.LowerRight }
                }, $"{check.B}");

                CountItem += 1;
                if (CountItem % Count == 0)
                {
                    if (ItemCount > Count)
                    {
                        Position = 0.5f - Count / 2f * Width - (Count - 1) / 2f * Margin;
                        ItemCount -= Count;
                    }
                    else
                    {
                        Position = 0.5f - ItemCount / 2f * Width - (ItemCount - 1) / 2f * Margin;
                    }
                    MinHeight -= ((Margin * 2) + Height);
                }
                else
                {
                    Position += (Width + Margin);
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}