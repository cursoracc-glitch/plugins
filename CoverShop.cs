using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("CoverShop", "Fipp", "0.0.1")]
    public class CoverShop : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        private ConfigData _config;

        class ItemsCover
        {
            [JsonProperty("Shortname предмета")] public string ShortName;
            [JsonProperty("Цена(В Изоленте)")] public int AmountForBuy;
            [JsonProperty("Кол-во")] public int Amount;
        }

        class ConfigData
        {
            [JsonProperty("Шанс выпадение Изоленты из бочек и ящиков(0-100%")]
            public int Chance = 25;

            [JsonProperty("Сколько минимум будет падать Изоленты")]
            public int AmountMin = 10;

            [JsonProperty("Сколько максимум будет падать Изоленты")]
            public int AmountMax = 15;

            [JsonProperty("Товары в магазине")] public List<ItemsCover> ListItems { get; set; }

            public static ConfigData GetNewCong()
            {
                ConfigData newConfig = new ConfigData();

                newConfig.ListItems = new List<ItemsCover>
                {
                    new ItemsCover()
                    {
                        ShortName = "rifle.ak",
                        AmountForBuy = 150,
                        Amount = 1
                    },
                    new ItemsCover()
                    {
                        ShortName = "syringe.medical",
                        AmountForBuy = 250,
                        Amount = 5
                    },
                    new ItemsCover()
                    {
                        ShortName = "metal.fragments",
                        AmountForBuy = 350,
                        Amount = 1500
                    },
                };
                return newConfig;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<ConfigData>();

            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => _config = ConfigData.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(_config);

        void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintWarning("Не найден ImageLibrary! Плагину будет плохо");
            }
        }

        public string Layer = "Layer_ui";


        [ChatCommand("shop")]
        void Shoping(BasePlayer player, string command, string[] args)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            var Panel = container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0.8235294"},
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                CursorEnabled = true,
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "-100 -100", AnchorMax = "100 100", OffsetMax = "0 0"},
                Button = {Color = "0 0 0 0", Close = Layer},
                Text = {Text = ""}
            }, Layer);
            
            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.001562446 0.9092593", AnchorMax = "0.9953126 0.9851852", OffsetMax = "0 0" },
                    Button = { Color = HexToCuiColor("#FFFFFF00") },
                    Text = { Text = $"МАГАЗИН".ToUpper(), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 30, Color = "1 1 1 0.3870127" }
                }, Layer, Layer + ".Teting");
            
            CuiHelper.AddUi(player, container);
            ResourceFind(player, 1);
        }

        void ResourceFind(BasePlayer player, int page)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer + ".Back");
            container.Add(new CuiButton
                {
                    RectTransform =
                        {AnchorMin = "0.002085398 0.4527777", AnchorMax = "0.05104575 0.5462968", OffsetMax = "0 0"},
                    Button = {FadeIn = 0f, Color = HexToCuiColor("#FFFFFF00"), Command = $"CommandCover {page - 1}"},
                    Text =
                    {
                        Text = "<", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                        FontSize = 40, Color = "0.9953126 0.9851852"
                    }
                }, Layer, Layer + ".Back");
            CuiHelper.DestroyUi(player, Layer + ".Run");
            container.Add(new CuiButton
                {
                    RectTransform =
                        {AnchorMin = "0.9494721 0.4527777", AnchorMax = "0.9984304 0.5462968", OffsetMax = "0 0"},
                    Button = {FadeIn = 0f, Color = HexToCuiColor("#FFFFFF00"), Command = $"CommandCover {page + 1}"},
                    Text =
                    {
                        Text = ">", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                        FontSize = 40, Color = "0.9953126 0.9851852"
                    }
                }, Layer, Layer + ".Run");
            for (int i = 0; i < 15; i++)
            {
                CuiHelper.DestroyUi(player, Layer + $".{i}.Img");
                CuiHelper.DestroyUi(player, Layer + $".{i}");
            }
            foreach (var check in _config.ListItems.Select((i, t) => new {A = i, B = t - (page - 1) * 15}).Skip((page - 1) * 15).Take(15))
            {
                container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin =
                                $"{0.08593751 + check.B * 0.175 - Math.Floor((double) check.B / 5) * 5 * 0.175} {0.7333337 - Math.Floor((double) check.B / 5) * 0.30}",
                            AnchorMax =
                                $"{0.1880209 + check.B * 0.175 - Math.Floor((double) check.B / 5) * 5 * 0.175} {0.9064818 - Math.Floor((double) check.B / 5) * 0.30}",
                            OffsetMax = "0 0"
                        },
                        Button =
                        {
                            Color = HexToCuiColor("#FFFFFF00"),
                            Command = $"CommandCover buy {check.A.ShortName}"
                        },
                        Text =
                        {
                            Text = $"x{check.A.Amount}", Align = TextAnchor.LowerRight, Font = "robotocondensed-bold.ttf", FontSize = 15
                        }
                    }, Layer, Layer + $".{check.B}");
                
                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{check.B}",
                    Name = Layer + $".{check.B}.Img",
                    Components =
                    {
                        new CuiRawImageComponent
                            {FadeIn = 0.3f, Png = (string) ImageLibrary.Call("GetImage",  check.A.ShortName)},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });
                
                container.Add(new CuiElement
                {
                    FadeOut = 0.3f,
                    Parent = Layer + $".{check.B}",
                    Name = Layer + $".{check.B}.Txt",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = $"{check.A.AmountForBuy} Изоленты", Align = TextAnchor.UpperLeft,
                            Font = "robotocondensed-bold.ttf", FontSize = 15, Color = "1 1 1 0.6"
                        },
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1", OffsetMax = "-5 -1"}
                    }
                });
            }
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("CommandCover")]
        void CoverConsole(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

            if (player != null && args.HasArgs(1))
            {
                int page = 1;
                if (int.TryParse(args.Args[0], out page) && page > 0 && (page - 1) * 15 <= _config.ListItems.Count)
                {
                    ResourceFind(player, page);
                }
                else if (args.Args[0] == "buy")
                    {
                        if (args.HasArgs(2))
                        {
                            var items = _config.ListItems.FirstOrDefault(p => p.ShortName == args.Args[1]);
                            if (items != null)
                            {
                                var amountget = player.inventory.GetAmount(ItemManager.FindItemDefinition("ducttape").itemid);
                                if (amountget < items.AmountForBuy)
                                {
                                    SendReply(player, "У вас недостаточно Изоленты для покупки");
                                    return;
                                }
                                var item = ItemManager.CreateByName(items.ShortName, items.Amount);
                                player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                                player.inventory.Take(null, ItemManager.FindItemDefinition("ducttape").itemid, items.AmountForBuy);
                                SendReply(player, $"Вы успешно купили {ItemManager.FindItemDefinition($"{items.ShortName}").displayName.english}");
                            }
                        }
                    }
            }
        }
        
        private List<StorageContainer> handledContainers = new List<StorageContainer>();
        
        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container == null || player == null) return null;
            if (handledContainers.Contains(container)) return null;
            handledContainers.Add(container);

            if (Random.Range(0, 100) < _config.Chance)
            {
                if (container.inventory.itemList.Count == container.inventory.capacity)
                {
                    container.inventory.capacity++;
                }
                var item = ItemManager.CreateByName("ducttape", Random.Range(_config.AmountMin, _config.AmountMax), 1748707346);
                item.name = "Изолент";
                item.MoveToContainer(container.inventory);
            }
            return null;
        }
        private Item OnItemSplit(Item item, int amount)
        {
            if (item.info.shortname == "ducttape" && item.skin == 1748707346)
            {
                item.amount = item.amount - amount;
                Item splitAmount1 = ItemManager.CreateByItemID(item.info.itemid, amount, item.skin);
                splitAmount1.name = "Изолент";
                splitAmount1.MarkDirty();
                item.MarkDirty();
                return splitAmount1;
            }
            return null;
        }
        private static string HexToCuiColor(string hex)
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
        
        [ConsoleCommand("givecover")]
        void GiveCommands(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            BasePlayer player = args.Player();
            if (!player.IsAdmin) return;
            if (args.Args[0] == null) return;
            int amount;
            if (!int.TryParse(args.Args[0], out amount))
            {
                SendReply(player, "Вы не указали кол-во Изолент");
                return;
            }
            var item = ItemManager.CreateByName("ducttape", amount, 1748707346);
            item.name = "Изолент";
            player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
            SendReply(player, $"Вы успешно выдали {amount} Изолент");
        }
    }
}