using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XCraft", "VooDoo", "1.0")]
    [Description("Craft tab for XMenu")]
    public class XCraft : RustPlugin
    {
        [PluginReference] Plugin XMenu;
        [PluginReference] Plugin Notifications;

        #region Image Library
        [PluginReference] Plugin ImageLibrary;
        bool AddImage(string url, string imageName, ulong imageId, Action callback = null) => (bool)ImageLibrary.Call("AddImage", url, imageName, imageId, callback);
        string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false) => (string)ImageLibrary.Call("GetImage", imageName, imageId, returnUrl);
        #endregion

        #region Config
        private PluginConfig config;
        private class PluginConfig
        {
            public ColorConfig colorConfig;
            public class ColorConfig
            {
                public string menuContentHighlighting;
                public string menuContentHighlightingalternative;

                public string gradientColor;
            }
        }

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                colorConfig = new PluginConfig.ColorConfig()
                {
                    menuContentHighlighting = "#0000007f",
                    menuContentHighlightingalternative = "#FFFFFF10",

                    gradientColor = "#00000099",
                }
            };
        }
        #endregion

        #region Data
        private Dictionary<string, int> categoryID = new Dictionary<string, int>();
        public Dictionary<string, Category> CategoryList = new Dictionary<string, Category>();
        public Dictionary<string, Category> CategoryListCache = new Dictionary<string, Category>();

        public class Category
        {
            public string CategoryName;
            public Dictionary<string, ItemMK2> Items;
        }

        public class ItemMK2
        {
            public string DisplayName;
            public string DisplayDescription;
            public string DisplayImage;

            public int WorkbenchLevel;
            public List<Ingredient> Ingredients;

            public string ItemResult;
            public string CommandResult;
        }

        public class Ingredient
        {
            public string DisplayName;
            public string ShortName;
            public int Amount;
            public ulong SkinID;
        }
        #endregion

        #region Initialize
        Timer TimerInitialize;
        private void OnServerInitialized()
        {
            CategoryList = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, Category>>("CraftData");

            foreach (var category in CategoryList)
            {
                foreach (var itemMk2 in category.Value.Items)
                {
                    if (itemMk2.Value.DisplayImage.StartsWith("http"))
                        AddImage(itemMk2.Value.DisplayImage, itemMk2.Value.DisplayImage, 0);
                }
            }

            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterMenu", this.Name, "CraftMenu", "assets/icons/player_carry.png", null, null);
                    foreach (var Category in CategoryList)
                        XMenu.Call("API_RegisterSubMenu", this.Name, "CraftMenu", Category.Key, $"RenderCraftMenu", null);

                    foreach (var Category in CategoryList)
                        categoryID.Add(Category.Key, (int)XMenu.Call("API_GetSubMenuID", "CraftMenu", Category.Key));

                    cmd.AddChatCommand("craft", this, (p, cmd, args) => rust.RunClientCommand(p, "custommenu true CraftMenu"));
                    TimerInitialize.Destroy();
                }
            });
        }
        #endregion

        #region Layers
        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";
        #endregion

        #region UI
        private void RenderCraftMenu(ulong userID, object[] objects)
        {
            BasePlayer player = BasePlayer.FindByID(userID);
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            string Title = CategoryList.ElementAt((int)objects[3]).Key;
            string Name = string.Empty;
            int itemCount = 1;

            if (!string.IsNullOrEmpty((string)objects[5]))
            {
                itemCount = int.Parse(((string)objects[5]).Split(' ')[0]);
                Name = string.Join(" ", ((string)objects[5]).Split(' ').Skip(1));
            }

            Container.Add(new CuiElement
            {
                Name = MenuContent,
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-215 -230",
                            OffsetMax = "500 270"
                        },
                    }
            });

            if (string.IsNullOrEmpty(Name))
            {
                for (int i = 0, x = 0, y = 0; i < CategoryList[Title].Items.Count; i++, x++)
                {
                    if (x > 5)
                    {
                        x = 0;
                        y++;
                    }

                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + "." + CategoryList[Title].Items.ElementAt(i).Key,
                        Parent = MenuContent,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0 0 0 0.5",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"{62.5 + x * 100} {-140 - y * 100}",
                                OffsetMax = $"{152.5 + x * 100} {-50 - y * 100}"
                            }
                        }
                    });

                    Container.Add(new CuiButton
                    {
                        Button = { Color = "0 0 0 0", Command = $"custommenu false CraftMenu {categoryID[Title]} 0 1 {CategoryList[Title].Items.ElementAt(i).Key}", },
                        RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{62.5 + x * 100} {-140 - y * 100}", OffsetMax = $"{152.5 + x * 100} {-50 - y * 100}" },
                        Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                    }, MenuContent, MenuContent + "." + CategoryList[Title].Items.ElementAt(i).Key + ".Btn");

                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + "." + CategoryList[Title].Items.ElementAt(i).Key + ".Img",
                        Parent = MenuContent + "." + CategoryList[Title].Items.ElementAt(i).Key,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "1 1 1 0.05",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.10 0.10",
                                AnchorMax = "0.90 0.90",
                            }
                        }
                    });

                    if (!string.IsNullOrEmpty(CategoryList[Title].Items.ElementAt(i).Value.ItemResult))
                    {
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + "." + CategoryList[Title].Items.ElementAt(i).Key + ".Img",
                            Parent = MenuContent + "." + CategoryList[Title].Items.ElementAt(i).Key + ".Img",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = GetImage(CategoryList[Title].Items.ElementAt(i).Value.ItemResult),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.10 0.10",
                                    AnchorMax = "0.90 0.90",
                                }
                            }
                        });
                    }
                    else
                    {
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + "." + CategoryList[Title].Items.ElementAt(i).Key + ".Img",
                            Parent = MenuContent + "." + CategoryList[Title].Items.ElementAt(i).Key + ".Img",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = GetImage(CategoryList[Title].Items.ElementAt(i).Value.DisplayImage)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.10 0.10",
                                    AnchorMax = "0.90 0.90",
                                }
                            }
                        });
                    }
                }
            }
            else
            {
                bool canCraft = HaveResourcesForItem(player, CategoryList[Title].Items[Name], itemCount);
                Container.Add(new CuiElement
                {
                    Name = MenuContent,
                    Parent = MenuContent,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0.5",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "62.5 -500",
                            OffsetMax = "650 -50"
                        }
                    }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".Title",
                    Parent = MenuContent,
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"Крафт предмета <color=#90BD47>{Name}</color>",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 18,
                                Font = "robotocondensed-regular.ttf",
                                FadeIn = 0.5f,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.1 0.875",
                                AnchorMax = "0.9 0.975",
                            }
                        }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + "." + Name,
                    Parent = MenuContent,
                    Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0 0 0 0.5",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.420 0.675",
                                AnchorMax = "0.580 0.875",
                            }
                        }
                });

                if (CategoryList[Title].Items[Name].WorkbenchLevel > 0)
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".Workbench",
                        Parent = MenuContent,
                        Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color=#FF0000AA>Для крафта требуется верстак {CategoryList[Title].Items[Name].WorkbenchLevel} уровня</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 14,
                                    Font = "robotocondensed-regular.ttf",
                                    FadeIn = 0.5f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.1 0.625",
                                    AnchorMax = "0.9 0.675",
                                }
                            }
                    });
                }

                Container.Add(new CuiElement
                {
                    Name = MenuContent + "." + Name + ".Img",
                    Parent = MenuContent + "." + Name,
                    Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "1 1 1 0.05",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.10 0.10",
                                AnchorMax = "0.90 0.90",
                            }
                        }
                });

                if (!string.IsNullOrEmpty(CategoryList[Title].Items[Name].ItemResult))
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + "." + Name + ".Img",
                        Parent = MenuContent + "." + Name + ".Img",
                        Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = GetImage(CategoryList[Title].Items[Name].ItemResult),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.10 0.10",
                                    AnchorMax = "0.90 0.90",
                                }
                            }
                    });
                }
                else
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + "." + Name + ".Img",
                        Parent = MenuContent + "." + Name + ".Img",
                        Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = GetImage(CategoryList[Title].Items[Name].DisplayImage)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.10 0.10",
                                    AnchorMax = "0.90 0.90",
                                }
                            }
                    });
                }

                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".Title",
                    Parent = MenuContent,
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"{CategoryList[Title].Items[Name].DisplayDescription}",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 12,
                                Font = "robotocondensed-regular.ttf",
                                FadeIn = 0.5f,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.1 0.425",
                                AnchorMax = "0.9 0.625",
                            }
                        }
                });

                if (!canCraft)
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".Title",
                        Parent = MenuContent,
                        Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color=#FF0000FF>У вас не хватает ресурсов для крафта {Name}</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 12,
                                    Font = "robotocondensed-regular.ttf",
                                    FadeIn = 0.5f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.1 0.025",
                                    AnchorMax = "0.9 0.075",
                                }
                            }
                    });
                }

                for (int i = 0; i < CategoryList[Title].Items[Name].Ingredients.Count; i++)
                {
                    string name = string.IsNullOrEmpty(CategoryList[Title].Items[Name].Ingredients.ElementAt(i).DisplayName) ? ItemManager.itemDictionaryByName[CategoryList[Title].Items[Name].Ingredients.ElementAt(i).ShortName].displayName.english : CategoryList[Title].Items[Name].Ingredients.ElementAt(i).DisplayName;
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".TableCount" + i,
                        Parent = MenuContent,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "1 1 1 0.05",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0.1 {0.251 - i * 0.05}",
                                AnchorMax = $"0.2 {0.301 - i * 0.05}",
                            }
                        }
                    });
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".TableCount" + i + ".Title",
                        Parent = MenuContent + ".TableCount" + i,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color=#FFFFFFAA>{CategoryList[Title].Items[Name].Ingredients.ElementAt(i).Amount}</color>",
                                Align = TextAnchor.MiddleRight,
                                FontSize = 10,
                                Font = "robotocondensed-regular.ttf",
                                FadeIn = 0.5f,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0",
                                AnchorMax = "0.95 1",
                            }
                        }
                    });
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".TableName" + i,
                        Parent = MenuContent,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "1 1 1 0.05",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0.201 {0.251 - i * 0.05}",
                                AnchorMax = $"0.7 {0.301 - i * 0.05}",
                            }
                        }
                    });
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".TableName" + i + ".Title",
                        Parent = MenuContent + ".TableName" + i,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color=#FFFFFFAA>{name}</color>",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 10,
                                Font = "robotocondensed-regular.ttf",
                                FadeIn = 0.5f,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0",
                                AnchorMax = "0.95 1",
                            }
                        }
                    });

                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".TableCountM" + i,
                        Parent = MenuContent,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "1 1 1 0.05",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0.701 {0.251 - i * 0.05}",
                                AnchorMax = $"0.8 {0.301 - i * 0.05}",
                            }
                        }
                    });
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".TableCountM" + i + ".Title",
                        Parent = MenuContent + ".TableCountM" + i,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color=#FFFFFFAA>{CategoryList[Title].Items[Name].Ingredients.ElementAt(i).Amount * itemCount}</color>",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 10,
                                Font = "robotocondensed-regular.ttf",
                                FadeIn = 0.5f,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0",
                                AnchorMax = "0.95 1",
                            }
                        }
                    });

                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".TableCountH" + i,
                        Parent = MenuContent,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "1 1 1 0.05",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0.801 {0.251 - i * 0.05}",
                                AnchorMax = $"0.9 {0.301 - i * 0.05}",
                            }
                        }
                    });
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".TableCountH" + i + ".Title",
                        Parent = MenuContent + ".TableCountH" + i,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color=#FFFFFFAA>{GetItemAmount(player, CategoryList[Title].Items[Name].Ingredients.ElementAt(i).ShortName, CategoryList[Title].Items[Name].Ingredients.ElementAt(i).SkinID)}</color>",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 10,
                                Font = "robotocondensed-regular.ttf",
                                FadeIn = 0.5f,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.05 0",
                                AnchorMax = "0.95 1",
                            }
                        }
                    });
                }

                if (itemCount > 1)
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".BackTitle",
                        Parent = MenuContent,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0 0 0 0.5",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = "0 -300",
                                OffsetMax = "30 -270"
                            }
                        }
                    });
                    Container.Add(new CuiButton
                    {
                        Button = { Color = "0 0 0 0", Command = $"custommenu false CraftMenu {categoryID[Title]} 0 {itemCount - 1} {Name}", },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Text = { Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                    }, MenuContent + ".BackTitle", MenuContent + ".BackTitleBtn");
                }

                if (canCraft && !(player.currentCraftLevel < CategoryList[Title].Items[Name].WorkbenchLevel))
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".NextTitle",
                        Parent = MenuContent,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0 0 0 0.5",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = "205 -300",
                                OffsetMax = "395 -270"
                            }
                        }
                    });
                    Container.Add(new CuiButton
                    {
                        Button = { Color = "0 0 0 0", Command = $"excraft {Title} {itemCount} {Name}" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Text = { Text = "Скрафтить", Align = TextAnchor.MiddleCenter, FontSize = 16 }
                    }, MenuContent + ".NextTitle", MenuContent + ".NextTitleBtn");
                }

                if (true)
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".NextTitle",
                        Parent = MenuContent,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = "0 0 0 0.5",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = "560 -300",
                                OffsetMax = "590 -270"
                            }
                        }
                    });
                    Container.Add(new CuiButton
                    {
                        Button = { Color = "0 0 0 0", Command = $"custommenu false CraftMenu {categoryID[Title]} 0 {itemCount + 1} {Name}", },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Text = { Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 20 }
                    }, MenuContent + ".NextTitle", MenuContent + ".NextTitleBtn");
                }
            }
        }
        #endregion

        [ConsoleCommand("excraft")]
        void CmdCraft(ConsoleSystem.Arg arg)
        {
            string Title = arg.Args[0];
            string ItemName = string.Join(" ", arg.Args.Skip(2));
            int Amount = int.Parse(arg.Args[1]);

            ItemMK2 item = CategoryList[Title].Items[ItemName];
            if (!HaveResourcesForItem(arg.Player(), item, Amount))
            {
                Notifications.Call("API_AddUINote", arg.Connection.userid, $"У вас не хватает ресурсов для крафта {ItemName}");
                return;
            }

            if (arg.Player().currentCraftLevel < item.WorkbenchLevel)
            {
                Notifications.Call("API_AddUINote", arg.Connection.userid, $"Вы должны стоять у верстака {item.WorkbenchLevel} уровня для крафта {ItemName}");
                return;
            }

            GetResourcesForItem(arg.Player(), item, Amount);

            if (!string.IsNullOrEmpty(item.ItemResult))
            {
                var result = ItemManager.CreateByName(item.ItemResult, Amount);
                if (!result.MoveToContainer(arg.Player().inventory.containerMain))
                    result.Drop(arg.Player().transform.position, Vector3.up);
            }
            else if (!string.IsNullOrEmpty(item.CommandResult))
                for (int i = 0; i < Amount; i++)
                    rust.RunServerCommand(string.Format(item.CommandResult, arg.Player().userID));

            rust.RunClientCommand(arg.Player(), $"custommenu false CraftMenu {categoryID[Title]} 0 {(int)Amount} {ItemName}");
        }

        #region Get/Have Resources for Craft
        private void GetResourcesForItem(BasePlayer player, ItemMK2 itemMk2, int piec)
        {
            foreach (var itemcraft in itemMk2.Ingredients)
                TakeItem(player, itemcraft.ShortName, itemcraft.SkinID, itemcraft.Amount * piec);
        }

        private bool HaveResourcesForItem(BasePlayer player, ItemMK2 itemMk2, int? piec)
        {
            foreach (var itemcraft in itemMk2.Ingredients)
            {
                if (GetItemAmount(player, itemcraft.ShortName, itemcraft.SkinID) < itemcraft.Amount * piec)
                    return false;
            }
            return true;
        }
        #endregion

        #region GetAmount & Take Items (Items with SkinID)
        public static int GetItemAmount(BasePlayer player, string shortName, ulong skinID)
        {
            int amount = 0;
            player.inventory.containerMain.itemList.ForEach(item =>
            {
                if (item.info.shortname == shortName && item.skin == skinID)
                    amount += item.amount;
            });

            return amount;
        }

        public static bool TakeItem(BasePlayer player, string shortName, ulong skinID, int amount)
        {
            Dictionary<Item, int> itemAmount = new Dictionary<Item, int>();
            player.inventory.containerMain.itemList.ForEach(item =>
            {
                if (item.info.shortname == shortName && item.skin == skinID)
                    itemAmount.Add(item, player.inventory.containerMain.itemList.IndexOf(item));
            });

            foreach (var item in itemAmount)
            {
                if (amount > 0)
                {
                    if (item.Key.amount > amount)
                    {
                        player.inventory.containerMain.itemList[item.Value].amount -= amount;
                        amount -= amount;
                        player.inventory.containerMain.itemList[item.Value].MarkDirty();
                    }
                    else
                    {
                        amount -= item.Key.amount;
                        player.inventory.containerMain.itemList[item.Value].Remove();
                        player.inventory.containerMain.MarkDirty();
                    }
                }
            }

            return true;
        }
        #endregion

        #region Utils
        private static string HexToRustFormat(string hex)
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
        #endregion
    }
}
