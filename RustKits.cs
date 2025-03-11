using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RustKits", "VooDoo", "1.0.0")]
    [Description("Menu for RUST")]
    public class RustKits : RustPlugin
    {
        private static RustKits Instance;
        [PluginReference] Plugin XMenu;
        [PluginReference] Plugin ImageLibrary;
        [PluginReference] Plugin Notifications;

        private bool AddImage(string url, string imageName, ulong imageId = 0, Action callback = null) => (bool)ImageLibrary.Call("AddImage", url, imageName, imageId, callback);
        private string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false) => (string)ImageLibrary.Call("GetImage", imageName, imageId, returnUrl);

        #region Data
        private List<Kit> _kits = new List<Kit>();
        private Dictionary<ulong, Dictionary<string, KitData>> _kitsData = new Dictionary<ulong, Dictionary<string, KitData>>();

        public class KitData
        {
            public int Amount { get; set; }
            public double Cooldown { get; set; }
        }

        public class Kit
        {
            public string Name { get; set; }
            public string Image { get; set; }
            public string Permission { get; set; }
            public int Amount { get; set; }
            public double Cooldown { get; set; }
            public List<KitItem> Items { get; set; }
        }

        public class KitItem
        {
            public string Name { get; set; }
            public string Container { get; set; }
            public int Amount { get; set; }
            public ulong SkinID { get; set; }
            public float Condition { get; set; }
            public Weapon Weapon { get; set; }
        }

        public class Weapon
        {
            public string AmmoName { get; set; }
            public int Amount;

            public List<string> Content { get; set; }
        }      

        private List<Kit> GetKitsForPlayer(BasePlayer player)
        {
            return _kits.Where(kit => kit.Name != "autokit" && (string.IsNullOrEmpty(kit.Permission) || permission.UserHasPermission(player.UserIDString, kit.Permission)) && (kit.Amount == 0 || (kit.Amount > 0 && GetKitData(player, kit.Name).Amount < kit.Amount))).ToList();
        }

        private KitData GetKitData(BasePlayer player, string kitName)
        {
            Dictionary<string, KitData> kitDictionary;
            if (_kitsData.TryGetValue(player.userID, out kitDictionary))
            {
                KitData kitData;
                if (kitDictionary.TryGetValue(kitName, out kitData))
                {
                    return kitData;
                }
                else
                {
                    _kitsData[player.userID][kitName] = new KitData();
                    return _kitsData[player.userID][kitName];
                }
            }
            else
            {
                _kitsData[player.userID] = new Dictionary<string, KitData>();
                _kitsData[player.userID][kitName] = new KitData();
                return _kitsData[player.userID][kitName];
            }
        }

        private List<KitItem> GetPlayerItems(BasePlayer player)
        {
            List<KitItem> kititems = new List<KitItem>();
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "wear");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "main");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "belt");
                    kititems.Add(iteminfo);
                }
            }
            return kititems;
        }

        private KitItem ItemToKit(Item item, string container)
        {
            KitItem kitItem = new KitItem();
            kitItem.Amount = item.amount;
            kitItem.Container = container;
            kitItem.SkinID = item.skin;
            kitItem.Name = item.info.shortname;
            kitItem.Condition = item.condition;
            kitItem.Weapon = null;
            if (item.info.category == ItemCategory.Weapon)
            {
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    kitItem.Weapon = new Weapon();
                    kitItem.Weapon.AmmoName = weapon.primaryMagazine.ammoType.shortname;
                    kitItem.Weapon.Amount = weapon.primaryMagazine.contents;
                    
                    if (item.contents != null)
                    {
                        kitItem.Weapon.Content = new List<string>();
                        foreach (var content in item.contents.itemList)
                        {
                            kitItem.Weapon.Content.Add(content.info.shortname);
                        }
                    }


                }
            }
            return kitItem;
        }
        #endregion

        #region UnityMod Hooks
        private void OnPlayerRespawned(BasePlayer player)
        {
            if(_kits.Exists(x => x.Name == "AUTOKIT".ToLower()))
            {
                player.inventory.Strip();
                var kit = _kits.First(x => x.Name == "AUTOKIT".ToLower());
                GiveKit(player, kit);
			
            }
        }

        private void SaveKits()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Kits", _kits);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Kits_Data", _kitsData);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["TITLE"] = "<color=#fffffAA>List of KITS</color>",
                ["TIMER"] = "Timer",
                ["TITLE_CAN"] = "Available",
                ["BUTTON_CAN"] = "Receive",
                ["BUTTON_CANT"] = "Wait",
                ["KIT_NOTFOUND"] = "This kit does not exist",
                ["KIT_PERMISSION"] = "You are not authorized to use this kit",
                ["KIT_TIME"] = "You will be able to use this kit through {time}",
                ["KIT_NEEDSLOTS"] = "Unable to get kit - not enough space in inventory",
                ["KIT_RECEIVED"] = "You received the kit {kitname}",
                ["TITLE_KITSHOW"] = "<color=#fffffAA>List items of kit <color=#FFAA00AA>%</color></color>",
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["TITLE"] = "<color=#fffffAA>Список китов нашего сервера</color>",
                ["TIMER"] = "Таймер",
                ["TITLE_CAN"] = "Доступно",
                ["BUTTON_CAN"] = "Взять",
                ["BUTTON_CANT"] = "Подождите",
                ["KIT_NOTFOUND"] = "Этого набора не существует",
                ["KIT_PERMISSION"] = "У вас нет полномочий использовать этот набор",
                ["KIT_TIME"] = "Вы сможете использовать этот набор через {time}",
                ["KIT_NEEDSLOTS"] = "Невозможно получить набор - недостаточно места в инвентаре",
                ["KIT_RECEIVED"] = "Вы получили набор {kitname}",
                ["TITLE_KITSHOW"] = "<color=#fffffAA>Список содержимого набора <color=#FFAA00AA>%</color></color>",
            }, this, "ru");
        }

        private Timer initTimer;
        private void OnServerInitialized()
        {
            Instance = this;

            _kits = Interface.Oxide.DataFileSystem.ReadObject<List<Kit>>("Kits");
            _kitsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, KitData>>>("Kits_Data");

            for(int i = 0; i < _kits.Count; i++)
            {
                AddImage("file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "Kits" + Path.DirectorySeparatorChar + _kits[i].Image, _kits[i].Name);
                permission.RegisterPermission(_kits[i].Permission, this);
            }

            initTimer = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterSubMenu", this.Name, "Main", "Киты", "RenderKits", null);

                    int kitsID = (int)XMenu.Call("API_GetSubMenuID", "Main", "Киты");
                    cmd.AddChatCommand("kit", this, (p, cmd, args) => rust.RunClientCommand(p, $"custommenu true Main {kitsID}"));

                    initTimer.Destroy();
                }
            });

        }

        private void Unload()
        {
            SaveData();
        }

        [ConsoleCommand("kitadd")]
        private void AddKitCmd(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin)
            {
                Kit kit = new Kit()
                {
                    Name = "New Kit",
                    Amount = 0,
                    Cooldown = 100000,
                    Image = "rust.png",
                    Items = new List<KitItem>(),
                    Permission = "RustKits.hide"
                };

                for(int i = 0; i < arg.Player().inventory.containerBelt.itemList.Count; i++)
                {
                    KitItem kitItem = new KitItem()
                    {
                        Container = "belt",
                        Amount = arg.Player().inventory.containerBelt.itemList[i].amount,
                        Name = arg.Player().inventory.containerBelt.itemList[i].info.shortname,
                        SkinID = arg.Player().inventory.containerBelt.itemList[i].skin,
                        Condition = arg.Player().inventory.containerBelt.itemList[i].condition,
                    };

                    if (arg.Player().inventory.containerBelt.itemList[i].info.category == ItemCategory.Weapon)
                    {
                        BaseProjectile weapon = arg.Player().inventory.containerBelt.itemList[i].GetHeldEntity() as BaseProjectile;
                        if (weapon != null)
                        {
                            kitItem.Weapon = new Weapon();
                            kitItem.Weapon.AmmoName = weapon.primaryMagazine.ammoType.shortname;
                            kitItem.Weapon.Amount = weapon.primaryMagazine.contents;
                        }
                    }

                    kit.Items.Add(kitItem);
                }

                for (int i = 0; i < arg.Player().inventory.containerMain.itemList.Count; i++)
                {
                    KitItem kitItem = new KitItem()
                    {
                        Container = "main",
                        Amount = arg.Player().inventory.containerMain.itemList[i].amount,
                        Name = arg.Player().inventory.containerMain.itemList[i].info.shortname,
                        SkinID = arg.Player().inventory.containerMain.itemList[i].skin,
                        Condition = arg.Player().inventory.containerMain.itemList[i].condition,
                    };

                    if (arg.Player().inventory.containerMain.itemList[i].info.category == ItemCategory.Weapon)
                    {
                        BaseProjectile weapon = arg.Player().inventory.containerMain.itemList[i].GetHeldEntity() as BaseProjectile;
                        if (weapon != null)
                        {
                            kitItem.Weapon = new Weapon();
                            kitItem.Weapon.AmmoName = weapon.primaryMagazine.ammoType.shortname;
                            kitItem.Weapon.Amount = weapon.primaryMagazine.contents;
                        }
                    }

                    kit.Items.Add(kitItem);
                }

                for (int i = 0; i < arg.Player().inventory.containerWear.itemList.Count; i++)
                {
                    KitItem kitItem = new KitItem()
                    {
                        Container = "wear",
                        Amount = arg.Player().inventory.containerWear.itemList[i].amount,
                        Name = arg.Player().inventory.containerWear.itemList[i].info.shortname,
                        SkinID = arg.Player().inventory.containerWear.itemList[i].skin,
                        Condition = arg.Player().inventory.containerWear.itemList[i].condition,
                    };

                    if (arg.Player().inventory.containerWear.itemList[i].info.category == ItemCategory.Weapon)
                    {
                        BaseProjectile weapon = arg.Player().inventory.containerWear.itemList[i].GetHeldEntity() as BaseProjectile;
                        if (weapon != null)
                        {
                            kitItem.Weapon = new Weapon();
                            kitItem.Weapon.AmmoName = weapon.primaryMagazine.ammoType.shortname;
                            kitItem.Weapon.Amount = weapon.primaryMagazine.contents;
                        }
                    }

                    kit.Items.Add(kitItem);
                }


                _kits.Add(kit);
                SaveKits();
            }

        }

        [ConsoleCommand("kit")]
        private void GiveKitCmd(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (args.HasArgs(4))
            {
                string kitName = args.Args[0];
                int i = int.Parse(args.Args[1]);
                int positionX = int.Parse(args.Args[2]);
                int positionY = int.Parse(args.Args[3]);

                if(_kits.Exists(x => x.Name == kitName) == false)
                {;
                    return;
                }

                var kit = _kits.First(x => x.Name == kitName);
                if(string.IsNullOrEmpty(kit.Permission) == false && permission.UserHasPermission(player.UserIDString, kit.Permission) == false)
                {
                    return;
                }

                var kitData = GetKitData(player, kitName);
                if (kit.Amount > 0 && kitData.Amount >= kit.Amount)
                {
                    return;
                }

                if (kit.Cooldown > 0)
                {
                    var currentTime = GetCurrentTime();
                    if (kitData.Cooldown > currentTime)
                    {
                        return;
                    }
                }

                int beltcount = kit.Items.Where(j => j.Container == "belt").Count();
                int wearcount = kit.Items.Where(j => j.Container == "wear").Count();
                int maincount = kit.Items.Where(j => j.Container == "main").Count();
                int totalcount = beltcount + wearcount + maincount;

                if ((player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count) < beltcount ||
                    (player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count) < wearcount ||
                    (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count) < maincount)
                {
                    if (totalcount > (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count))
                    {
                        return;
                    }
                }

                GiveKit(player, kit);

                if (kit.Amount > 0)
                    kitData.Amount += 1;

                if (kit.Cooldown > 0)
                    kitData.Cooldown = GetCurrentTime() + kit.Cooldown;
                CuiElementContainer Container = new CuiElementContainer();
                RenderKit(player, Container, GetKitsForPlayer(player), i, positionX, positionY);
                CuiHelper.AddUi(player, Container);
            }
        }
		
        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";
		
        private void RenderKits(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
			BasePlayer player = BasePlayer.FindByID(userID);
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];
			
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
			
            Container.Add(new CuiElement
            {
                Name = "XMenu" + ".Content" + ".Kits",
                Parent = MenuContent,
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = "0 0 0 0",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = "XMenu" + ".Content"  + $".Title",
                Parent = "XMenu" + ".Content" + ".Kits",
                Components =
                    {
                        new CuiTextComponent
                        {
                            Text = lang.GetMessage("TITLE", this, player.UserIDString),
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 36,
                            Font = "robotocondensed-bold.ttf",
                            FadeIn = 0.5f,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = $"-275 -100",
                            OffsetMax = $"275 0",
                        }
                    }
            });
            List<Kit> Kits = GetKitsForPlayer(player);
            for (int i = 0, x = 0, y = 0; i < Kits.Count; i++)
            {
                if (x == 3)
                {
                    x = 0;
                    y++;
                }
                RenderKit(player, Container, Kits, i, x, y);
                x++;
            }
        }

        private void RenderKit(BasePlayer player, CuiElementContainer Container, List<Kit> Kits, int number, int positionX, int positionY)
        {
            CuiHelper.DestroyUi(player, "XMenu" + ".Content"  + $"Kit.{number}");
            double Cooldown = (GetCurrentTime() - GetKitData(player, Kits[number].Name).Cooldown) * -1;
            string Time = TimeExtensions.FormatShortTime(TimeSpan.FromSeconds(Cooldown));
            string BtnColor = "1 0.22 0.15 0.3";
            if (Cooldown < 0) BtnColor = "0.66 1 0.3 0.2";
            Container.Add(new CuiElement
            {
                Name = "XMenu" + ".Content"  + $"Kit.{number}",
                Parent = "XMenu" + ".Content" + ".Kits",
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = "0 0 0 0.5",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.5 1",
                                    AnchorMax = "0.5 1",
                                    OffsetMin = $"{-300 + positionX * 205} {-200 - positionY * 105}",
                                    OffsetMax = $"{-105 + positionX * 205} {-100 - positionY * 105}"
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = "XMenu" + ".Content"  + $"Kit.{number}.Background",
                Parent = "XMenu" + ".Content"  + $"Kit.{number}",
                Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = "1 1 1 0.05",
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.025 0.05",
                                        AnchorMax = "0.475 0.95",
                                    }
                                }
            });
            Container.Add(new CuiElement
            {
                Name = "XMenu" + ".Content"  + $"Kit.{number}.Img",
                Parent = "XMenu" + ".Content"  + $"Kit.{number}",
                Components =
                            {
                                new CuiRawImageComponent
                                {
                                    FadeIn = 0.5f,
                                    Png = GetImage(Kits[number].Name),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.025 0.05",
                                    AnchorMax = "0.475 0.95",
                                }
                            }
            });
            Container.Add(new CuiButton
            {
                Button = { Color = "1 1 1 0", Command = $"kitshow {Kits[number].Name}" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", },
                Text = { Text = "", Align = TextAnchor.MiddleCenter }
            }, "XMenu" + ".Content"  + $"Kit.{number}.Img");
            Container.Add(new CuiElement
            {
                Name = "XMenu" + ".Content"  + $"Kit.{number}.Title",
                Parent = "XMenu" + ".Content"  + $"Kit.{number}",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"<color=#fff9f9AA>{Kits[number].Name}</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 12,
                                    Font = "robotocondensed-bold.ttf",
                                    FadeIn = 0.5f,
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.5 0.7",
                                    AnchorMax = "1 1",
                                }
                            }
            });
            Container.Add(new CuiElement
            {
                Name = "XMenu" + ".Content"  + $"Kit.{number}.BtnBackground",
                Parent = "XMenu" + ".Content"  + $"Kit.{number}",
                Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = BtnColor,
                                    FadeIn = 0.5f,
                                    Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.525 0.05",
                                    AnchorMax = "0.95 0.3",
                                },
                            }
            });
            if (Cooldown > 0)
            {
                Container.Add(new CuiElement
                {
                    Name = "XMenu" + ".Content"  + $"Kit.{number}.Timer",
                    Parent = "XMenu" + ".Content"  + $"Kit.{number}",
                    Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color=#fff9f9AA>{lang.GetMessage("TIMER", this, player.UserIDString)}:\n {Time}</color>",
                                        Align = TextAnchor.LowerCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-bold.ttf",
                                        FadeIn = 0.5f,
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.5 0.35",
                                        AnchorMax = "1 0.7",
                                    }
                                }
                });
                Container.Add(new CuiElement
                {
                    Name = "XMenu" + ".Content"  + $"Kit.{number}.BtnTitle",
                    Parent = "XMenu" + ".Content"  + $"Kit.{number}",
                    Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color=#fff9f9AA>{lang.GetMessage("BUTTON_CANT", this, player.UserIDString)}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-bold.ttf",
                                        FadeIn = 0.5f,
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.525 0.05",
                                        AnchorMax = "0.95 0.3",
                                    }
                                }
                });
            }
            else
            {
                Container.Add(new CuiElement
                {
                    Name = "XMenu" + ".Content"  + $"Kit.{number}.Timer",
                    Parent = "XMenu" + ".Content"  + $"Kit.{number}",
                    Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color=#fff9f9AA>{lang.GetMessage("TITLE_CAN", this, player.UserIDString)}</color>",
                                        Align = TextAnchor.LowerCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-bold.ttf",
                                        FadeIn = 0.5f,
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.5 0.35",
                                        AnchorMax = "1 0.7",
                                    }
                                }
                });
                Container.Add(new CuiElement
                {
                    Name = "XMenu" + ".Content"  + $"Kit.{number}.BtnTitle",
                    Parent = "XMenu" + ".Content"  + $"Kit.{number}",
                    Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Text = $"<color=#fff9f9AA>{lang.GetMessage("BUTTON_CAN", this, player.UserIDString)}</color>",
                                        Align = TextAnchor.MiddleCenter,
                                        FontSize = 14,
                                        Font = "robotocondensed-bold.ttf",
                                        FadeIn = 0.5f,
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.525 0.05",
                                        AnchorMax = "0.95 0.3",
                                    }
                                }
                });
                Container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"kit {Kits[number].Name} {number} {positionX} {positionY}" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, "XMenu" + ".Content"  + $"Kit.{number}.BtnTitle");
            }
        }

        [ConsoleCommand("kitshow")]
        void CaseShowCmd(ConsoleSystem.Arg arg)
        {
            if (arg.HasArgs())
            {
                if (arg.Args[0] == "close")
                {
                    CuiHelper.DestroyUi(arg.Player(), "XMenu" + ".Content" );

                    return;
                }
                Kit _kit = _kits.Find(x => x.Name == arg.Args[0]);
                if (_kit == null)
                {
                    Notifications.Call("API_AddUINote", arg.Connection.userid, $"Не найден набор с таким именем");
                    return;
                }

                RenderKitInfo(arg.Player(), _kit);
            }
        }

        private void RenderKitInfo(BasePlayer player, Kit kit)
        {
            CuiHelper.DestroyUi(player, "XMenu" + ".Content" + ".Kits");
            CuiElementContainer Container = new CuiElementContainer();
            Container.Add(new CuiElement
            {
                Name = "XMenu" + ".Content"  + $".Title",
                Parent = "XMenu" + ".Content" ,
                Components =
                    {
                        new CuiTextComponent
                        {
                            Text = lang.GetMessage("TITLE_KITSHOW", this, player.UserIDString).Replace("%", kit.Name),
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 24,
                            Font = "robotocondensed-bold.ttf",
                            FadeIn = 0.5f,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 1",
                            AnchorMax = "0.5 1",
                            OffsetMin = $"-275 -75",
                            OffsetMax = $"275 0",
                        }
                    }
            });

            for(int i = 0, x = 0, y = 0; i < kit.Items.Count; i++)
            {
                if(x > 5)
                {
                    x = 0;
                    y++;
                }
                if (ItemManager.itemDictionaryByName.ContainsKey(kit.Items[i].Name))
                {
                    Container.Add(new CuiElement
                    {
                        Name = "XMenu" + ".Content"  + $"Kit.Info.Item{i}.Background",
                        Parent = "XMenu" + ".Content" ,
                        Components =
                                {
                                    new CuiImageComponent
                                    {
                                        Color = "1 1 1 0.05",
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "0.5 1",
                                        AnchorMax = "0.5 1",
                                        OffsetMin = $"{-275 + x * 90} {-155 - y * 90}",
                                        OffsetMax = $"{-195 + x * 90} {-75 - y * 90}",
                                    }
                                }
                    });

                    Container.Add(new CuiElement
                    {
                        Name = "XMenu" + ".Content"  + $"Kit.Info.Item{i}.Background.Img",
                        Parent = "XMenu" + ".Content"  + $"Kit.Info.Item{i}.Background",
                        Components =
                            {
                                new CuiRawImageComponent
                                {
                                    FadeIn = 0.5f,
                                    Png = GetImage(kit.Items[i].Name),
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = $"0.1 0.1",
                                    AnchorMax = $"0.9 0.9",
                                }
                            }
                    });

                    Container.Add(new CuiElement
                    {
                        Name = "XMenu" + ".Content"  + $"Kit.Info.Item{i}.Background.Amount",
                        Parent = "XMenu" + ".Content"  + $"Kit.Info.Item{i}.Background",
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = kit.Items[i].Amount + " шт",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 12,
                                Font = "robotocondensed-bold.ttf",
                                FadeIn = 0.5f,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 0",
                                AnchorMax = $"1 0.3",
                            }
                        }
                    });
                    x++;
                }
            }

            CuiHelper.AddUi(player, Container);
        }

        private void GiveKit(BasePlayer player, Kit kit)
        {
            foreach (var kitItem in kit.Items)
            {
                if (ItemManager.itemDictionaryByName.ContainsKey(kitItem.Name))
                {
                    GiveItem(player.inventory, BuildItem(kitItem.Name, kitItem.Amount, kitItem.SkinID, kitItem.Condition, kitItem.Weapon), kitItem.Container == "belt" ? player.inventory.containerBelt : kitItem.Container == "wear" ? player.inventory.containerWear : player.inventory.containerMain);
                }
                else
                {
                    if (!string.IsNullOrEmpty(kitItem.Name))
                    {
                        rust.RunServerCommand(kitItem.Name.Replace("%STEAMID%", player.userID.ToString()));
                    }
                }
            }
        }

        private void GiveItem(PlayerInventory inv, Item item, ItemContainer cont = null)
        {
            if (item == null) return;
            var a = item.MoveToContainer(cont) || item.MoveToContainer(inv.containerBelt) || item.MoveToContainer(inv.containerWear) || item.MoveToContainer(inv.containerMain);
        }

        private Item BuildItem(string ShortName, int Amount, ulong SkinID, float Condition, Weapon weapon)
        {
            Item item = ItemManager.CreateByName(ShortName, Amount > 1 ? Amount : 1, SkinID);
            item.condition = Condition;
            if (weapon != null)
            {
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents = weapon.Amount;
                (item.GetHeldEntity() as BaseProjectile).primaryMagazine.ammoType = ItemManager.FindItemDefinition(weapon.AmmoName);

                if (weapon.Content != null)
                {
                    foreach (var content in weapon.Content)
                    {
                        Item itemContent = ItemManager.CreateByName(content, 1);
                        itemContent.MoveToContainer(item.contents);
                    }
                }
            }

            return item;
        }
        #endregion

        #region Utils
        private double GetCurrentTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;

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
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        private static class TimeExtensions
        {
            public static string FormatShortTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0)
                    result += $"{time.Days}д ";

                if (time.Hours != 0)
                    result += $"{time.Hours}ч ";

                if (time.Minutes != 0)
                    result += $"{time.Minutes}м ";

                if (time.Seconds != 0)
                    result += $"{time.Seconds}с ";

                return result;
            }

            public static string FormatTime(TimeSpan time)
            {
                string result = string.Empty;
                if (time.Days != 0)
                    result += $"{Format(time.Days, "дней", "дня", "день")} ";

                if (time.Hours != 0)
                    result += $"{Format(time.Hours, "часов", "часа", "час")} ";

                if (time.Minutes != 0)
                    result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";

                if (time.Seconds != 0)
                    result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";

                return result;
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
        }
        #endregion
    }
}
