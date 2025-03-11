using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CustomCraft", "CASHR", "1.0.0")]
    public class CustomCraft : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

        public string Layer = "UI_CustomCraft.Craft";
        public string LayerBlur = "UI.CustomCraft.Blur";

        public Dictionary<ulong, int> playerModifity = new Dictionary<ulong, int>();
        #region Config       
        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
        }


        protected override void LoadDefaultConfig()
        {
            _config = new Configuration()
            {
                items = new List<ItemInfo>()
            };
        }

        public Configuration _config;

        public class Configuration
        {
            [JsonProperty("Новые предметы")] public List<ItemInfo> items = new List<ItemInfo>();
        }

        public class ItemInfo
        {
            [JsonProperty("Название предмета")] public string name = "";
            [JsonProperty("Текст кнопки")] public string textBtn = "";
            [JsonProperty("Шортнейм предмета")] public string shortname = "";
            [JsonProperty("Айди предмета")] public int idItem = 0;
            [JsonProperty("Верстак для крафта")] public int WorkBench = 1;
            [JsonProperty("Время крафта( в секундках) ")] public float timeCraft = 60f;
            [JsonProperty("Крафтить шт")] public int countCraft = 1;
            [JsonProperty("СкинИД предмета[Не менять]")] public ulong skinID = 0U;
            [JsonProperty("Ссылка на иконку")] public string png = null;
            [JsonProperty("Отображать крафт предмета")] public bool isCrafted = true;
            [JsonProperty("Описание для крафта")] public string descriptionCraft = "Тестовый предмет";
            [JsonProperty("Предметы для крафта")] public List<CraftInfo> craftItems = new List<CraftInfo>();
        }

        public class CraftInfo
        {
            [JsonProperty("Шортнейм предмета")] public string shortname = "";
            [JsonProperty("Иконка предмета")] public string icons = null;
            [JsonProperty("Количество предмета")] public int amount = 0;
            [JsonProperty("СкинИД предмета")] public ulong skinID = 0U;
            [JsonProperty("АйтемИД предмета")] public int itemID = 0;
        }

        #endregion

        #region Command
        [ChatCommand("craft")]
        void chatCmdCraft(BasePlayer player, string command, string[] args)
        {
            CraftGUI(player);


        }
        [ConsoleCommand("UI_CustomCraft")]
        void consoleCmdCustomCraft(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;

            if (player == null) return;

            ItemInfo craftItem = null;

            var name = "";
            var amount = 0;

            foreach (var args in arg.Args)
            {
                if (args == "choicecraft" || args == "close" || args == "craftitem" || args == "addmodifity" ||
                    args == "removemodifity") continue;
                if (name.Length == 0)
                {
                    name += args;
                }
                else name += $" {args}";
            }

            if (arg.GetString(0).Equals("close") || arg.GetString(0).Equals("giveitems"))
            {

            }
            else
            {
                craftItem = _config.items.Find(x => x.name.Contains(name));
                if (craftItem == null) return;
            }

            CuiElementContainer container = new CuiElementContainer();

            switch (arg.GetString(0))
            {
                case "giveitems":

                    foreach (var item in _config.items)
                    {
                        Item newItem = ItemManager.CreateByItemID(item.idItem, item.countCraft, item.skinID);

                        newItem.name = item.name;

                        player.GiveItem(newItem);
                    }

                    break;

                case "craftitem":

                    var success = true;

                    Dictionary<Item, int> items = new Dictionary<Item, int>();

                    foreach (var craftedItem in craftItem.craftItems)
                    {

                        var haveItem = HaveItem(player, craftedItem.itemID, craftedItem.skinID, craftedItem.amount);
                        if (!haveItem)
                        {
                            success = false;
                            SendReply(player,
                                "<color=#afafaf>Вы не можете скрафтить предмет! Не хватает ингредиента!</color>");
                            return;
                        }
                        var item = FindItem(player, craftedItem.itemID, craftedItem.skinID, craftedItem.amount);

                        items.Add(item, craftedItem.amount * playerModifity[player.userID]);
                    }

                    foreach (var item in items)
                    {
                        item.Key.UseItem(item.Value);
                    }

                    if (success)
                    {
                        player.SendConsoleCommand("UI_Craft close");
                        Item craft = ItemManager.CreateByName(craftItem.shortname, craftItem.countCraft * playerModifity[player.userID], craftItem.skinID);

                        craft.name = craftItem.name;


                        player.GiveItem(craft);
                    }

                    break;

                case "addmodifity":

                    playerModifity[player.userID] += 1;

                    CuiHelper.DestroyUi(player, Layer + ".CountPanel.Text");

                    amount = craftItem.countCraft * playerModifity[player.userID];

                    container.Add(new CuiLabel()
                    {
                        RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 1" },
                        Text =
                            {
                                Align = TextAnchor.MiddleCenter, Text = $"{amount} шт.",
                                FontSize = 28
                            }
                    }, Layer + ".CountPanel", Layer + ".CountPanel.Text");

                    CuiHelper.AddUi(player, container);

                    UpdateItemsInfo(player, name);

                    break;

                case "removemodifity":

                    if (playerModifity[player.userID] == craftItem.countCraft) return;
                    playerModifity[player.userID] -= 1;

                    CuiHelper.DestroyUi(player, Layer + ".CountPanel.Text");

                    amount = craftItem.countCraft * playerModifity[player.userID];

                    container.Add(new CuiLabel()
                    {
                        RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 1" },
                        Text =
                            {
                                Align = TextAnchor.MiddleCenter, Text = $"{amount} шт.",
                                FontSize = 28
                            }
                    }, Layer + ".CountPanel", Layer + ".CountPanel.Text");

                    container.Add(new CuiButton()
                    {
                        RectTransform =
                            {
                                AnchorMin = $"0.463021 {0.246296 - 0.093519}",
                                AnchorMax = $"0.504688 {0.32037 - 0.093519}"
                            },
                        Button =
                            {
                                Color = "0 0 0 0.8",
                                Command = $"UI_CustomCraft removemodifity {craftItem.name}",

                            },
                        Text =
                            {
                                Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Text = "<b>-</b>", FontSize = 24
                            }
                    }, Layer, Layer + ".Btn1");

                    container.Add(new CuiButton()
                    {
                        RectTransform =
                            {
                                AnchorMin = $"0.615106 {0.246296 - 0.093519}",
                                AnchorMax = $"0.656772 {0.32037 - 0.093519}"
                            },
                        Button =
                            {
                                Color = "0 0 0 0.8",
                                Command = $"UI_CustomCraft addmodifity {craftItem.name}",

                            },
                        Text =
                            {
                                Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Text = "<b>+</b>", FontSize = 24
                            }
                    }, Layer, Layer + ".Btn2");

                    CuiHelper.AddUi(player, container);

                    UpdateItemsInfo(player, name);

                    break;

                case "close":

                    CuiHelper.DestroyUi(player, Layer);
                    CuiHelper.DestroyUi(player, LayerBlur);

                    break;

                case "choicecraft":

                    CuiHelper.DestroyUi(player, Layer + ".Items");
                    CuiHelper.DestroyUi(player, Layer + ".TextBox");
                    CuiHelper.DestroyUi(player, Layer + ".Menu.MainImage");
                    CuiHelper.DestroyUi(player, Layer + ".Btn1");
                    CuiHelper.DestroyUi(player, Layer + ".Btn2");
                    CuiHelper.DestroyUi(player, Layer + ".Btn3");
                    CuiHelper.DestroyUi(player, Layer + ".CountPanel.Text");

                    playerModifity[player.userID] = 1;

                    container.Add(new CuiLabel()
                    {
                        RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 1" },
                        Text =
                            {
                                Align = TextAnchor.MiddleCenter, Text = $"{craftItem.countCraft * playerModifity[player.userID]} шт.",
                                FontSize = 28
                            }
                    }, Layer + ".CountPanel", Layer + ".CountPanel.Text");

                    container.Add(new CuiButton()
                    {
                        RectTransform =
                            {
                                AnchorMin = $"0.6635427 {0.246296 - 0.093519}",
                                AnchorMax = $"0.8380209 {0.32037 - 0.093519}"
                            },
                        Button =
                            {
                                Color = "0 0 0 0.8",
                                Command = $"UI_CustomCraft craftitem {craftItem.name}"
                            },
                        Text =
                            {
                                Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Text = "<b>СКРАФТИТЬ</b>",
                                FontSize = 24
                            }
                    }, Layer, Layer + ".Btn3");

                    container.Add(new CuiButton()
                    {
                        RectTransform =
                            {
                                AnchorMin = $"0.463021 {0.246296 - 0.093519}",
                                AnchorMax = $"0.504688 {0.32037 - 0.093519}"
                            },
                        Button =
                            {
                                Color = "0 0 0 0.8",
                                Command = $"UI_CustomCraft removemodifity {craftItem.name}",

                            },
                        Text =
                            {
                                Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Text = "<b>-</b>", FontSize = 24
                            }
                    }, Layer, Layer + ".Btn1");

                    container.Add(new CuiButton()
                    {
                        RectTransform =
                            {
                                AnchorMin = $"0.615106 {0.246296 - 0.093519}",
                                AnchorMax = $"0.656772 {0.32037 - 0.093519}"
                            },
                        Button =
                            {
                                Color = "0 0 0 0.8",
                                Command = $"UI_CustomCraft addmodifity {craftItem.name}",

                            },
                        Text =
                            {
                                Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Text = "<b>+</b>", FontSize = 24
                            }
                    }, Layer, Layer + ".Btn2");

                    container.Add(new CuiPanel()
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Image = { Color = "0 0 0 0" }
                    }, Layer + ".Menu", Layer + ".Items");

                    container.Add(new CuiPanel()
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Image = { Color = "0 0 0 0" }
                    }, Layer + ".Menu", Layer + ".TextBox");

                    container.Add(new CuiElement()
                    {
                        Parent = Layer + ".Menu",
                        Name = Layer + ".Menu.MainImage",
                        Components =
                        {
                            new CuiRawImageComponent()
                            {
                                Color = "1 1 1 1",
                                Png = craftItem.png == null
                                    ? GetImage(craftItem.shortname) : GetImage(craftItem.png),

                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = $"0.422221 0.453911",
                                AnchorMax = $"0.83889 0.933911"
                            },
                        }
                    });

                    container.Add(new CuiLabel()
                    {
                        RectTransform = { AnchorMin = $"0.184722 0.9184", AnchorMax = "0.773611 0.9952" },
                        Text =
                            {
                                Align = TextAnchor.MiddleCenter, Color = HexToRGB("#FFFFFFD6"),
                                Text = $"<b>{craftItem.name}</b>", FontSize = 20
                            }
                    }, Layer + ".TextBox");

                    container.Add(new CuiLabel()
                    {
                        RectTransform = { AnchorMin = $"0.020833 0.435201", AnchorMax = "0.405555 0.904" },
                        Text =
                            {
                                Align = TextAnchor.UpperCenter, Color = HexToRGB("#FFFFFFD6"),
                                Text = $"<size=20>Описание предмета:</size>\n <color=#00bfff>{craftItem.descriptionCraft}</color>",
                                FontSize = 16
                            }
                    }, Layer + ".TextBox");

                    float minPosition = 0f;
                    var itemWidth = (0.193633f - 0.054745f);
                    var itemHeight = 0.421575f - 0.261575f;
                    var itemCount = craftItem.craftItems.Count;
                    var itemMargin = 0.019399f;

                    if (itemCount >= 5)
                        minPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
                    else minPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                    itemCount -= 5;

                    var minHeight = 0.238401f;
                    var modifity = 1;

                    var countItem = 0;
                    foreach (var item in craftItem.craftItems)
                    {
                        countItem += 1;

                        var haveItem = HaveItem(player, item.itemID, item.skinID, item.amount * modifity);
                        container.Add(new CuiElement()
                        {
                            Parent = Layer + ".Items",
                            Name = Layer + $".Item{item.shortname}",
                            Components =
                            {
                                new CuiImageComponent()
                                {
                                    Color = haveItem ? HexToRGB("#80A47DFF") : HexToRGB("#A47D7DFF"),

                                },
                                new CuiOutlineComponent()
                                {
                                    Color = haveItem ? HexToRGB("#A0D29CFF") : HexToRGB("#C29494FF"),
                                    Distance = "2 2",
                                },
                                new CuiRectTransformComponent()
                                {
                                    AnchorMin = $"{minPosition} {minHeight}",
                                    AnchorMax = $"{minPosition + itemWidth} {minHeight + itemHeight}"
                                },
                            }
                        });
                        string png;
                        if (item.icons != null)
                        {
                            png = GetImage(item.icons);
                        }
                        else
                        {
                            png = GetImage(item.shortname);
                        }
                        container.Add(new CuiElement()
                        {
                            Parent = Layer + $".Item{item.shortname}",
                            Name = Layer + $".Item{item.shortname}.Image",
                            Components =
                            {
                                new CuiRawImageComponent()
                                {
                                    Color = "1 1 1 1",
                                    Png = png,

                                },
                                new CuiRectTransformComponent()
                                {
                                    AnchorMin = "0 0", AnchorMax = "1 1"
                                },
                            }
                        });

                        container.Add(new CuiLabel()
                        {
                            Text =
                                {
                                    Text = $"x{item.amount * modifity}", Align = TextAnchor.LowerRight, FontSize = 12
                                },
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-3 -3" }
                        }, Layer + $".Item{item.shortname}.Image");

                        if (countItem % 5 == 0)
                        {
                            if (itemCount > 5)
                            {
                                minPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
                                itemCount -= 5;
                            }
                            else
                            {
                                minPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                            }

                            minHeight -= ((itemMargin * 2) + itemHeight);
                        }
                        else
                        {
                            minPosition += (itemWidth + itemMargin);
                        }
                    }

                    CuiHelper.AddUi(player, container);

                    break;
            }
        }

        #endregion

        #region OxideHooks
       
        private void OnServerInitialized()
        {

            LoadConfig();
            SaveConfig();

            Dictionary<string, string> images = new Dictionary<string, string>();

            foreach (var craftItem in _config.items)
            {
                if (craftItem.png != null)
                {
                    ImageLibrary.Call("AddImage", craftItem.png, craftItem.png);
                }

                foreach (var item in craftItem.craftItems)
                {
                    ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{item.shortname}.png",
                        item.shortname);
                    if (item.icons != null)
                    {
                        ImageLibrary.Call("AddImage", $"{item.icons}",
                            item.icons);
                    }
                }
            }
        }        

        #endregion

        #region Function
        public void ReplyWithHelper(BasePlayer player, string message, string[] args = null)
        {
            if (args != null)
                message = string.Format(message, args);
            player.SendConsoleCommand("chat.add 0", new object[2]
            {
                76561198090669418,
                string.Format("<size=16><color={2}>{0}</color>:</size>\n{1}", "Виртуальный помощник:", message, "#00bfff")
            });
        }       
        private static string HexToRGB(string hex)
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
        public Item FindItem(BasePlayer player, int itemID, ulong skinID, int amount)
        {
            Item item = null;

            if (skinID == 0U)
            {
                if (player.inventory.FindItemID(itemID) != null && player.inventory.FindItemID(itemID).amount >= amount)
                    return player.inventory.FindItemID(itemID);
            }
            else
            {

                List<Item> items = new List<Item>();

                items.AddRange(player.inventory.FindItemIDs(itemID));

                foreach (var findItem in items)
                {
                    if (findItem.skin == skinID && findItem.amount >= amount)
                    {
                        return findItem;
                    }
                }
            }

            return item;
        }
        public bool HaveItem(BasePlayer player, int itemID, ulong skinID, int amount)
        {
            if (skinID == 0U)
            {
                if (player.inventory.FindItemID(itemID) != null &&
                    player.inventory.FindItemID(itemID).amount >= amount) return true;
                return false;
            }
            else
            {

                List<Item> items = new List<Item>();

                items.AddRange(player.inventory.FindItemIDs(itemID));

                foreach (var item in items)
                {
                    if (item.skin == skinID && item.amount >= amount)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        #endregion

        #region GUI
        public void UpdateItemsInfo(BasePlayer player, string name)
        {
            CuiHelper.DestroyUi(player, Layer + ".Items");

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Menu", Layer + ".Items");

            var craftItem = _config.items.Find(x => x.name.Contains(name));

            float minPosition = 0f;
            var itemWidth = (0.193633f - 0.054745f);
            var itemHeight = 0.421575f - 0.261575f;
            var itemCount = craftItem.craftItems.Count;
            var itemMargin = 0.019399f;

            if (itemCount >= 5)
                minPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
            else minPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
            itemCount -= 5;

            var minHeight = 0.238401f;
            var modifity = playerModifity[player.userID];

            var countItem = 0;
            foreach (var item in craftItem.craftItems)
            {
                countItem += 1;

                var haveItem = HaveItem(player, item.itemID, item.skinID, item.amount * modifity);

                container.Add(new CuiElement()
                {
                    Parent = Layer + ".Items",
                    Name = Layer + $".Item{item.shortname}",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = haveItem ? HexToRGB("#80A47DFF") : HexToRGB("#A47D7DFF"),
                            Sprite = "Assets/Content/UI/UI.Background.Tile.psd"
                        },
                        new CuiOutlineComponent()
                        {
                            Color = haveItem ? HexToRGB("#A0D29CFF") : HexToRGB("#C29494FF"),
                            Distance = "2 2",
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = $"{minPosition} {minHeight}",
                            AnchorMax = $"{minPosition + itemWidth} {minHeight + itemHeight}"
                        },
                    }
                });
                string png;
                if (item.icons != null)
                {
                    png = GetImage(item.icons);
                }
                else
                {
                    png = GetImage(item.shortname);
                }
                container.Add(new CuiElement()
                {
                    Parent = Layer + $".Item{item.shortname}",
                    Name = Layer + $".Item{item.shortname}.Image",
                    Components =
                    {
                        new CuiRawImageComponent()
                        {
                            Color = "1 1 1 1",
                            Png = png,
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1"
                        },
                    }
                });

                container.Add(new CuiLabel()
                {
                    Text =
                        {
                            Text = $"x{item.amount * modifity}", Align = TextAnchor.LowerRight, FontSize = 12
                        },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-3 -3" }
                }, Layer + $".Item{item.shortname}.Image");

                if (countItem % 5 == 0)
                {
                    if (itemCount > 5)
                    {
                        minPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
                        itemCount -= 5;
                    }
                    else
                    {
                        minPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                    }

                    minHeight -= ((itemMargin * 2) + itemHeight);
                }
                else
                {
                    minPosition += (itemWidth + itemMargin);
                }
            }

            CuiHelper.AddUi(player, container);
        }
        private void CraftGUI(BasePlayer player)
        {            
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.DestroyUi(player, LayerBlur);

            playerModifity[player.userID] = 1;

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel()
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.8", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", LayerBlur);

            container.Add(new CuiButton()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Command = "UI_CustomCraft close", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "Overlay", Layer);

            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0.2942708 0.9148148", AnchorMax = "0.6432291 0.9898146" },
                Text = { Align = TextAnchor.MiddleCenter, Text = "<b>СИСТЕМА УНИКАЛЬНЫХ ПРЕДМЕТОВ</b>", FontSize = 28 }
            }, Layer);

            var buttonHeight = (0.926852f - 0.873148f);
            var buttonMargin = (0.873148f - 0.853706f);
            var buttonCount = _config.items.Count;

            var buttonMinHeight = 0.45f + buttonCount / 2f * buttonHeight + (buttonCount - 1) / 2f * buttonMargin;

            foreach (var craftItem in _config.items)
            {
                container.Add(new CuiButton()
                {
                    RectTransform =
                    {
                        AnchorMin = $"{0.008854} {buttonMinHeight}",
                        AnchorMax = $"{0.225521} {buttonMinHeight + buttonHeight}"
                    },
                    Button = { Color = "0 191 255 0.5", Command = $"UI_CustomCraft choicecraft {craftItem.name}", },
                    Text = { Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Text = craftItem.textBtn, FontSize = 18 }
                }, Layer);

                buttonMinHeight -= (buttonHeight + buttonMargin);
            }           
                
            var currentCraft = _config.items.ElementAt(0);

            container.Add(new CuiPanel()
            {
                RectTransform =
                        {AnchorMin = $"0.463021 {0.332407 - 0.093519}", AnchorMax = $"0.838021 {0.911111 - 0.093519}"},
                Image = { Color = "0 0 0 0.8", }
            }, Layer, Layer + ".Menu");

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Menu", Layer + ".Items");

            container.Add(new CuiElement()
            {
                Parent = Layer + ".Menu",
                Name = Layer + ".Menu.MainImage",
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Color = "1 1 1 1",
                        Png = currentCraft.png == null
                            ? GetImage(currentCraft.shortname) : GetImage(currentCraft.png),

                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = $"0.422221 0.453911",
                        AnchorMax = $"0.83889 0.933911"
                    },
                }
            });

            container.Add(new CuiPanel()
            {
                RectTransform =
                        {AnchorMin = $"0.510417 {0.246296 - 0.093519}", AnchorMax = $"0.609375 {0.32037 - 0.093519}"},
                Image = { Color = "0 0 0 0.8", },
            }, Layer, Layer + ".CountPanel");

            var modifity = 1;

            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 1" },
                Text =
                    {
                        Align = TextAnchor.MiddleCenter, Text = $"{currentCraft.countCraft * modifity} шт.",
                        FontSize = 28
                    }
            }, Layer + ".CountPanel", Layer + ".CountPanel.Text");

            container.Add(new CuiButton()
            {
                RectTransform =
                    {AnchorMin = $"0.463021 {0.246296 - 0.093519}", AnchorMax = $"0.504688 {0.32037 - 0.093519}"},
                Button = { Color = "0 0 0 0.8", Command = $"UI_CustomCraft removemodifity {currentCraft.name}", },
                Text = { Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Text = "<b>-</b>", FontSize = 24 }
            }, Layer, Layer + ".Btn1");

            container.Add(new CuiButton()
            {
                RectTransform =
                    {AnchorMin = $"0.615106 {0.246296 - 0.093519}", AnchorMax = $"0.656772 {0.32037 - 0.093519}"},
                Button = { Color = "0 0 0 0.8", Command = $"UI_CustomCraft addmodifity {currentCraft.name}", },
                Text = { Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Text = "<b>+</b>", FontSize = 24 }
            }, Layer, Layer + ".Btn2");

            container.Add(new CuiButton()
            {
                RectTransform =
                    {AnchorMin = $"0.6635427 {0.246296 - 0.093519}", AnchorMax = $"0.8380209 {0.32037 - 0.093519}"},
                Button = { Color = "0 0 0 0.8", Command = $"UI_CustomCraft craftitem {currentCraft.name}" },
                Text = { Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Text = "<b>СКРАФТИТЬ</b>", FontSize = 24 }
            }, Layer, Layer + ".Btn3");

            container.Add(new CuiPanel()
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Menu", Layer + ".TextBox");

            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = $"0.184722 0.9184", AnchorMax = "0.773611 0.9952" },
                Text = { Align = TextAnchor.MiddleCenter, Color = HexToRGB("#FFFFFFD6"), Text = $"<b>{currentCraft.name}</b>", FontSize = 20 }
            }, Layer + ".TextBox");

            container.Add(new CuiLabel()
            {
                RectTransform = { AnchorMin = $"0.020833 0.435201", AnchorMax = "0.405555 0.904" },
                Text = { Align = TextAnchor.UpperCenter, Color = HexToRGB("#FFFFFFD6"), Text = $"<size=20>Описание предмета:</size>\n{currentCraft.descriptionCraft}", FontSize = 16 }
            }, Layer + ".TextBox");

            float minPosition = 0f;
            var itemWidth = (0.193633f - 0.054745f);
            var itemHeight = 0.421575f - 0.261575f;
            var itemCount = currentCraft.craftItems.Count;
            var itemMargin = 0.019399f;

            if (itemCount >= 5)
                minPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
            else minPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
            itemCount -= 5;

            var minHeight = 0.238401f;

            var countItem = 0;
            foreach (var item in currentCraft.craftItems)
            {
                countItem += 1;

                var haveItem = HaveItem(player, item.itemID, item.skinID, item.amount * modifity);

                container.Add(new CuiElement()
                {
                    Parent = Layer + ".Items",
                    Name = Layer + $".Item{item.shortname}",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = haveItem ? HexToRGB("#80A47DFF") : HexToRGB("#A47D7DFF"),
                            Sprite = "Assets/Content/UI/UI.Background.Tile.psd"
                        },
                        new CuiOutlineComponent()
                        {
                            Color = haveItem ? HexToRGB("#A0D29CFF") : HexToRGB("#C29494FF"),
                            Distance = "2 2",
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = $"{minPosition} {minHeight}",
                            AnchorMax = $"{minPosition + itemWidth} {minHeight + itemHeight}"
                        },
                    }
                });
                string png;
                if (item.icons != null)
                {
                    png = GetImage(item.icons);
                }
                else
                {
                    png = GetImage(item.shortname);
                }
                container.Add(new CuiElement()
                {
                    Parent = Layer + $".Item{item.shortname}",
                    Name = Layer + $".Item{item.shortname}.Image",
                    Components =
                    {
                        new CuiRawImageComponent()
                        {
                            Color = "1 1 1 1",
                            Png = png,

                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1"
                        },
                    }
                });

                container.Add(new CuiLabel()
                {
                    Text = { Text = $"x{item.amount * modifity}", Align = TextAnchor.LowerRight, FontSize = 12 },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-3 -3" }
                }, Layer + $".Item{item.shortname}.Image");

                if (countItem % 5 == 0)
                {
                    if (itemCount > 5)
                    {
                        minPosition = 0.5f - 5 / 2f * itemWidth - (5 - 1) / 2f * itemMargin;
                        itemCount -= 5;
                    }
                    else
                    {
                        minPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                    }

                    minHeight -= ((itemMargin * 2) + itemHeight);
                }
                else
                {
                    minPosition += (itemWidth + itemMargin);
                }

            }

            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}