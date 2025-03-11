using Newtonsoft.Json;
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
    [Info("SwapCoins", "Aliluya/Sparkless", "1.1.0")]
    class SwapCoins : RustPlugin
    {

        private string NameSilver = "Серебро";
        private string NameGold = "Золото";

        #region Настройка выпадения

        private List<string> listContainersSilver = new List<string>() // Выпадение серебра
        {
            {"loot-barrel-1"},
            {"loot-barrel-2"},
            {"loot_barrel_1"},
            {"loot_barrel_2"},
        };

        private List<string> ListContainersGold = new List<string>() // Выпадение золота!
        {
            {"codelockedhackablecrate"},
            {"crate_basic"},
            {"crate_elite"},
            {"crate_normal"},
            {"supply_drop"},
        };

        #endregion

        [PluginReference] Plugin ImageLibrary;

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Layer);
        }

        void OnServerInitialized()
        {
            foreach (var check in PluginImages)
                ImageLibrary.Call("AddImage", check.Value, check.Key);
				
				PrintWarning("Спасибо за использование плагина с coderust.space!!");
        }

        private PluginConfig config;

        public class CustomItem
        {
            [JsonProperty(PropertyName = "Shortname")]
            public string target;

            [JsonProperty(PropertyName = "Название предмета")]
            public string name;

            [JsonProperty(PropertyName = "Скин")] public ulong skinid;
        }

        class PluginConfig
        {
            [JsonProperty("Номер магазина gamestores")]
            public string ShopID = "7005";

            [JsonProperty("Секретный ключ")] public string APIKey = "f21311775b88ebca830ffa05f8456151";

            [JsonProperty("Ссылка на магазин")] public string servername = "test3.gamestores.ru";

            [JsonProperty("Мин рублей для обмена")]
            public int ObmenMoneyNeed = 20;

            [JsonProperty("Кол-во золотых монет к рублю")]
            public int gold = 10;

            [JsonProperty("Кол-во серебряных монет к рублю")]
            public int silver = 35;

            [JsonProperty("Кол-во золотых монет к серебряным")]
            public int obmen = 5;

            [JsonProperty("Какой магазин использовать (true = gamestores, false = moscow.ovh)")]
            public bool gamestores = true;

            [JsonProperty("Шанс выпадение серебра из ящиков")]
            public int ChanceSilver = 100;

            [JsonProperty("Мин.выпадение серебра(шт)")]
            public int MinSilver = 1;

            [JsonProperty("Макс.выпадение серебра(шт)")]
            public int MaxSilver = 3;

            [JsonProperty("Шанс выпадение золота из ящика")]
            public int ChanceGold = 100;

            [JsonProperty("Мин.выпадение золота(шт)")]
            public int MinGold = 1;

            [JsonProperty("Макс.выпадение золота(шт)")]
            public int MaxGold = 3;

            [JsonProperty(PropertyName = "Список предметов для замены скинов и имени")]
            public List<CustomItem> items;


        }

        private PluginConfig PanelConfig()
        {
            return new PluginConfig
            {
                items = new List<CustomItem>
                {
                    new CustomItem
                    {
                        target = "sticks",
                        name = "Серебро",
                        skinid = 1707224728
                    },
                    new CustomItem
                    {
                        target = "glue",
                        name = "Золото",
                        skinid = 1707233455
                    },
                }
            };
        }

        [JsonProperty("Изображения плагина")] private Dictionary<string, string> PluginImages =
            new Dictionary<string, string>
            {
                ["GOLD"] = "https://i.imgur.com/xqbDZyw.png",
                ["CASH"] = "https://i.imgur.com/fvyNxZK.png",
                ["SILVER"] = "https://i.imgur.com/e8mXHIA.png"
            };

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
            Config.WriteObject(PanelConfig(), true);
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity, Item item)
        {
            if (!(entity is LootContainer)) return;
            var container = (LootContainer) entity;
            if (handledContainers.Contains(container) || container.ShortPrefabName == "stocking_large_deployed" ||
                container.ShortPrefabName == "stocking_small_deployed") return;
            handledContainers.Add(container);
            List<int> ItemsList = new List<int>();
            if (listContainersSilver.Contains(container.ShortPrefabName))
            {
                if (UnityEngine.Random.Range(0f, 100f) < config.ChanceSilver)
                {
                    var itemContainer = container.inventory;
                    foreach (var i1 in itemContainer.itemList)
                    {
                        ItemsList.Add(i1.info.itemid);
                    }

                    if (!ItemsList.Contains(642482233))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        var count = UnityEngine.Random.Range(config.MinSilver, config.MaxSilver + 1);
                        item = ItemManager.CreateByName("sticks");
                        item.name = NameSilver;
                        item.MoveToContainer(itemContainer);
                    }
                }
            }

            if (ListContainersGold.Contains(container.ShortPrefabName))
            {
                if (UnityEngine.Random.Range(0f, 100f) < config.ChanceGold)
                {
                    var itemContainer = container.inventory;
                    foreach (var i1 in itemContainer.itemList)
                    {
                        ItemsList.Add(i1.info.itemid);
                    }

                    if (!ItemsList.Contains(-1899491405))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        var count = UnityEngine.Random.Range(config.MinGold, config.MaxGold + 1);
                        item = ItemManager.CreateByName("glue");
                        item.name = NameGold;
                        item.MoveToContainer(itemContainer);
                    }
                }
            }
        }

        private List<LootContainer> handledContainers = new List<LootContainer>();

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info, Item item)
        {
            if (info == null) return;
            if (entity?.net?.ID == null) return;
            var container = entity as LootContainer;
            var player = info?.InitiatorPlayer;
            if (player == null || container == null) return;
            List<int> ItemsList = new List<int>();
            if (ListContainersGold.Contains(container.ShortPrefabName))
            {
                if (UnityEngine.Random.Range(0f, 100f) < config.ChanceGold)
                {
                    var itemContainer = container.inventory;
                    foreach (var i1 in itemContainer.itemList)
                    {
                        ItemsList.Add(i1.info.itemid);
                    }

                    if (!ItemsList.Contains(-1899491405))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        var count = UnityEngine.Random.Range(config.MinGold, config.MaxGold + 1);
                        item = ItemManager.CreateByName("glue");
                        item.name = NameGold;
                        item.MarkDirty();
                        item.MoveToContainer(itemContainer);
                    }
                }
            }

            if (listContainersSilver.Contains(container.ShortPrefabName))
            {
                if (UnityEngine.Random.Range(0f, 100f) < config.ChanceSilver)
                {
                    var itemContainer = container.inventory;
                    foreach (var i1 in itemContainer.itemList)
                    {
                        ItemsList.Add(i1.info.itemid);
                    }

                    if (!ItemsList.Contains(642482233))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        var count = UnityEngine.Random.Range(config.MinSilver, config.MaxSilver + 1);
                        item = ItemManager.CreateByName("sticks");
                        item.name = NameSilver;
                        item.MarkDirty();
                        item.MoveToContainer(itemContainer);
                    }
                }
            }
            handledContainers.Remove(container);
        }
        [ConsoleCommand("UI_OBMENGOLDSILVER")]
        private void ObmenForGold(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            int amount = player.inventory.GetAmount(-1899491405);
            int result = player.inventory.GetAmount(-1899491405) * config.obmen;
            player.inventory.Take(null, -1899491405, amount);
            Item Give = ItemManager.CreateByItemID(Convert.ToInt32(642482233), result);
            player.GiveItem(Give);
            PrintToChat(player, $"Вы успешно обменяли золотые монеты на {result} серебряных монет!");
        }

        [ConsoleCommand("UI_OBMENRUBSILVER")]
        private void ObmenForRubSilver (ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            int kol = player.inventory.GetAmount(642482233);
            int obmenrub = player.inventory.GetAmount(642482233) / config.silver;
            player.inventory.Take(null, 642482233, kol);
            if (config.gamestores)
            {
                MoneyPlus(player.userID, obmenrub);
            }
            else
            {
                APIChangeUserBalance(player.userID, obmenrub, null);
            }
            PrintToChat(player, $"Вы успешно обменяли монеты на {obmenrub} бонусных рублей");
        }

        [ConsoleCommand("UI_OBMENRUBGOLD")]
        private void ObmenForRubGold(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            int amount = player.inventory.GetAmount(-1899491405);
            int obmenrub = player.inventory.GetAmount(-1899491405) / config.gold;
            player.inventory.Take(null, -1899491405, amount);
            if (config.gamestores)
            {
                MoneyPlus(player.userID, obmenrub);
            }
            else
            {
                APIChangeUserBalance(player.userID, obmenrub, null);
            }
            PrintToChat(player, $"Вы успешно обменяли монеты на {obmenrub} бонусных рублей");
        }

        const string Layer = "lay";
        [ChatCommand("swap")]
        private void DrawGui(BasePlayer player)
        {
            var result = player.inventory.GetAmount(-1899491405) * config.obmen;
            int obmenrub = player.inventory.GetAmount(642482233) / config.silver;
            int obmengoldrub = player.inventory.GetAmount(-1899491405) / config.gold;
            int silverneed = player.inventory.GetAmount(642482233);
            int goldneed = player.inventory.GetAmount(-1899491405);

            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            #region Parent
            container.Add(new CuiPanel
            {
                Image = { Color = HexToCuiColor("#48453EB7"), Material = "assets/content/ui/uibackgroundblur.mat", Sprite = "assets/content/ui/ui.background.tiletex.psd" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-250 -230", OffsetMax = "253 230" },
                CursorEnabled = true,
            }, "Overlay", Layer);
            #endregion

            #region Serebro
            container.Add(new CuiLabel
            {
                Text = { Text = $"КУРС ЗОЛОТА: x{config.gold} = 1р", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 11, Color = HexToCuiColor("#E4BC69FF") },
                RectTransform = { AnchorMin = "0.3759288 0.7588316", AnchorMax = "0.6274216 0.8301631" }
            }, Layer);
            container.Add(new CuiLabel
            {
                Text = { Text = $"КУРС СЕРЕБРА: x{config.silver} = 1р", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 11, Color = HexToCuiColor("#E4BC69FF") },
                RectTransform = { AnchorMin = "0.3759288 0.6059783", AnchorMax = "0.6274209 0.6773098" }
            }, Layer);
            container.Add(new CuiLabel
            {
                Text = { Text = $"КУРС ОБМЕНА: x1 = x{config.obmen}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 11, Color = HexToCuiColor("#E4BC69FF") },
                RectTransform = { AnchorMin = "0.3759288 0.453125", AnchorMax = "0.6274214 0.5244565" }
            }, Layer);
            #endregion

            #region add Amount
            container.Add(new CuiLabel
            {
                Text = { Text = $"x{player.inventory.GetAmount(-1899491405)}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 13, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.2511142 0.7404892", AnchorMax = "0.3461225 0.8118207" }
            }, Layer);

            container.Add(new CuiLabel
            {
                Text = { Text = $"x{player.inventory.GetAmount(-1899491405)}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 13, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.2511142 0.438857", AnchorMax = "0.3461225 0.5101894" }
            }, Layer);

            container.Add(new CuiLabel
            {
                Text = { Text = $"x{player.inventory.GetAmount(642482233)}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 13, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.2511142 0.5917107", AnchorMax = "0.3461225 0.6630422" }
            }, Layer);

            container.Add(new CuiLabel
            {
                Text = { Text = $"x{result}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 13, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.7298793 0.436819", AnchorMax = "0.8248861 0.5081514" }
            }, Layer);

            container.Add(new CuiLabel
            {
                Text = { Text = $"{obmenrub}р", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 13, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.7242907 0.5917107", AnchorMax = "0.8192974 0.6630422" }
            }, Layer);

            container.Add(new CuiLabel
            {
                Text = { Text = $"{obmengoldrub}р", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 13, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.7242907 0.7466034", AnchorMax = "0.8192974 0.8179349" }
            }, Layer);

            container.Add(new CuiLabel
            {
                Text = { Text = $"Минимальная сумма на обмен: {config.ObmenMoneyNeed}р.\nКурс обмена растёт каждый день на протяжении вайпа.\nПеред началом обмена, обязательно авторизируйтесь в магазине <color=#e4bc69>{config.servername}</color> если не делали этого раньше.", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 14, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.02197629 0.2146739", AnchorMax = "0.9739223 0.4164402" }
            }, Layer);
            #endregion

            #region gui
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiImageComponent { Color = HexToCuiColor("#D9D9D935")},
                        new CuiRectTransformComponent {AnchorMin = "0.2008157 0.7588316", AnchorMax = "0.3219046 0.8913044"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiImageComponent { Color = HexToCuiColor("#D9D9D935")},
                        new CuiRectTransformComponent {AnchorMin = "0.2008157 0.6100543", AnchorMax = "0.3219047 0.7425272"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiImageComponent { Color = HexToCuiColor("#D9D9D935")},
                        new CuiRectTransformComponent {AnchorMin = "0.2008157 0.4551631", AnchorMax = "0.3219047 0.587636"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiImageComponent { Color = HexToCuiColor("#D9D9D935")},
                        new CuiRectTransformComponent {AnchorMin = "0.6814457 0.7588316", AnchorMax = "0.8025347 0.8913044"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiImageComponent { Color = HexToCuiColor("#D9D9D935")},
                        new CuiRectTransformComponent {AnchorMin = "0.6814457 0.6080163", AnchorMax = "0.8025347 0.7404891"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiImageComponent { Color = HexToCuiColor("#D9D9D935")},
                        new CuiRectTransformComponent {AnchorMin = "0.6814458 0.4551631", AnchorMax = "0.8025348 0.587636"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "GOLD") , Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.213856 0.7853262", AnchorMax = "0.3070014 0.8872284"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "SILVER") , Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.213856 0.6324728", AnchorMax = "0.3070014 0.7343751"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "GOLD") , Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.213856 0.4816553", AnchorMax = "0.3070014 0.5835578"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "SILVER") , Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.6944845 0.4796185", AnchorMax = "0.7876285 0.5815212"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "CASH") , Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.6944845 0.7894036", AnchorMax = "0.7876285 0.8913059"}
                    }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiRawImageComponent {Png = (string) ImageLibrary.Call("GetImage", "CASH") , Color = "1 1 1 1"},
                        new CuiRectTransformComponent {AnchorMin = "0.6944845 0.6365489", AnchorMax = "0.7876285 0.7384512"}
                    }
            });

            container.Add(new CuiLabel
            {
                Text = { Text = "Обмен монет", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20, Color = HexToCuiColor("#C5BCB4FF") },
                RectTransform = { AnchorMin = "", AnchorMax = "" }
            }, Layer);

            container.Add(new CuiLabel
            {
                Text = { Text = "Обмен на бонусный баланс: средства можно потратить только на предметы и привилегии в магазине, их невозможно обменять на реальные деньги.", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 14, Color = HexToCuiColor("#E4BC69FF") },
                RectTransform = { AnchorMin = "0.02570242 0.1005435", AnchorMax = "0.9795111 0.2207881" }
            }, Layer);

            if (obmengoldrub >= config.ObmenMoneyNeed)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.400525 0.8213329", AnchorMax = "0.6017188 0.8926644" },
                    Button = { Color = HexToCuiColor("#7F7D789C"), Close = Layer, Command = "UI_OBMENRUBGOLD" },
                    Text = { Text = "ОБМЕНЯТЬ", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" }
                }, Layer);
            }
            else
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.400525 0.8213329", AnchorMax = "0.6017188 0.8926644" },
                    Button = { Color = HexToCuiColor("#4E4D4CD4")},
                    Text = { Text = "ОБМЕНЯТЬ", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" }
                }, Layer);
            }

            if (obmenrub >= config.ObmenMoneyNeed)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4001468 0.6711956", AnchorMax = "0.6013407 0.7425271" },
                    Button = { Color = HexToCuiColor("#7F7D789C"), Close = Layer, Command = "UI_OBMENRUBSILVER" },
                    Text = { Text = "ОБМЕНЯТЬ", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" }
                }, Layer);
            }
            else
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4001468 0.6711956", AnchorMax = "0.6013407 0.7425271" },
                    Button = { Color = HexToCuiColor("#4E4D4CD4") },
                    Text = { Text = "ОБМЕНЯТЬ", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" }
                }, Layer);
            }

            if (goldneed > 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4001467 0.5183412", AnchorMax = "0.6013407 0.5896727" },
                    Button = { Color = HexToCuiColor("#7F7D789C"), Close = Layer, Command = "UI_OBMENGOLDSILVER" },
                    Text = { Text = "ОБМЕНЯТЬ", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" }
                }, Layer);
            }
            else
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4001467 0.5183412", AnchorMax = "0.6013407 0.5896727" },
                    Button = { Color = HexToCuiColor("#4E4D4CD4") },
                    Text = { Text = "ОБМЕНЯТЬ", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" }
                }, Layer);
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.7913573 0.01086957", AnchorMax = "0.9925513 0.08220109" },
                Button = { Color = HexToCuiColor("#7F7D789C"), Close = Layer },
                Text = { Text = "ЗАКРЫТЬ", FontSize = 16, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-bold.ttf" }
            }, Layer);
            #endregion

            CuiHelper.AddUi(player, container);
        }

        #region Skins
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (item == null || item.info == null) return;

            var name = item.info.shortname.ToLower();

            foreach (var configRow in config.items)
            {
                if (configRow.target.ToLower() != name || configRow.skinid == item.skin) continue;
                item.name = configRow.name;
                item.skin = configRow.skinid;
            }
        }
        #endregion


        #region addmoney
        void APIChangeUserBalance(ulong steam, int balanceChange, Action<string> callback)
        {
            plugins.Find("RustStore").CallHook("APIChangeUserBalance", steam, balanceChange, new Action<string>((result) =>
            {
                if (result == "SUCCESS")
                {
                    Interface.Oxide.LogDebug($"Баланс пользователя {steam} увеличен на {balanceChange}");
                    return;
                }
                Interface.Oxide.LogDebug($"Баланс не был изменен, ошибка: {result}");
            }));
        }
        void MoneyPlus(ulong userId, int amount)
        {
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                { "action", "moneys" },
                { "type", "plus" },
                { "steam_id", userId.ToString() },
                { "amount", amount.ToString() }
            });
        }

        void ExecuteApiRequest(Dictionary<string, string> args)
        {
            string url = $"http://panel.gamestores.ru/api?shop_id={config.ShopID}&secret={config.APIKey}" +
                     $"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
            webrequest.EnqueueGet(url, (i, s) =>
            {
                if (i != 200)
                {
                    LogToFile("SwapCoins", $"Код ошибки: {i}, подробности:\n{s}", this);
                }
                else
                {
                    if (s.Contains("fail"))
                    {
                        return;
                    }
                }
            }, this);
        }
        #endregion

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
    }
}