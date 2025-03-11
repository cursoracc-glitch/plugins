using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Kits", "OMHOMHOM", 0.1)]

    class Kits: RustPlugin
    {
        #region Fields

        private string Layer = "UI.Kits";
        private string LayerBlur = "UI.Kits.Blur";
        private string LayerBlurKitsInfo = "UI.Kits.Blur";
        

        #endregion 

        #region Hooks
        
        object OnPlayerRespawned(BasePlayer player)
        { 
            player.inventory.Strip();
 
            foreach (var kitItem in _config.spawnKit)
            {
                var item = ItemManager.CreateByName(kitItem.shortname, kitItem.amount, kitItem.skinID);

                if (kitItem.place == "Одежда")
                {
                    item.MoveToContainer(player.inventory.containerWear);
                    
                    continue; 
                }

                if (kitItem.place == "Панель")
                {
                    item.MoveToContainer(player.inventory.containerBelt);
                    continue;
                }

                item.MoveToContainer(player.inventory.containerMain);
            }
            
            return null;
        }

        void Loaded()
        {
            LoadData();
			AddImage("https://static.moscow.ovh/images/games/rust//plugins/ultimate_ui/exit.png", "Kits_img_exit");
        }

        void OnServerInitialized()
        {
            timer.Every(310f, SaveData);
            
            foreach (var kit in _config.kits)
            {
                if (string.IsNullOrEmpty(kit.privilege)) continue;
                
                permission.RegisterPermission(kit.privilege, this);
            }
                        
            SaveConfig();
        }

        private KitsInfo GetKitsInfo(BasePlayer player)
        {
            KitsInfo result;
            if (!storedData.players.TryGetValue(player.userID, out result))
            {
                result = storedData.players[player.userID] = new KitsInfo();
            }

            return result;
        }

        private KitData GetKitData(KitsInfo kitsInfo, KitInfo kitInfo)
        {
            KitData result;
            if (!kitsInfo.kits.TryGetValue(kitInfo.kitName, out result))
            {
                result = kitsInfo.kits[kitInfo.kitName] = new KitData()
                {
                    amount = kitInfo.maxUse,
                    cooldown = 0
                };
            }

            return result;
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, LayerBlur);
            }
            
            SaveData();
        }

        #endregion

        #region Commands
        
        [ChatCommand("createkit")]
        private void CreateKit(BasePlayer player, string command, string[] args)
        {
            if (player.Connection.authLevel < 2) return;
            
            if (_config.kits.Exists(x => x.kitName == args[0]))
            {
                SendReply(player, "Название уже кита уже существует!");
                return;
            }

            _config.kits.Add(new KitInfo()
            {
                kitName = args[0],
                cooldownKit = 0,
                items = GetPlayerItems(player),
                maxUse = -1,
                privilege = ""
            });
            
            permission.RegisterPermission($"kits.default", this); 
            SendReply(player, $"Создали кит с именем {args[0]}");
            SaveConfig();
        }

        [ConsoleCommand("UI_KITS")]
        private void cmdConsoleHandler(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (!arg.HasArgs(1)) return;

            var cmd = arg.GetString(0);

            var kits = GetKitsForPlayer(player);
            var kitsInfo = GetKitsInfo(player);
            
            switch (cmd)
            {
                case "prev":
                {
                    player.SendConsoleCommand("UI_KITS showavailablekits");
                    break;
                }
                case "close":
                {
                    CuiHelper.DestroyUi(player, Layer);
                    CuiHelper.DestroyUi(player, LayerBlur);
                    break;
                }
                case "showinfokit":
                {
                    var targetNameKit = arg.GetString(1);
                    if (arg.HasArgs(3)) targetNameKit += $" {arg.GetString(2)}";
                    var kitInfo = _config.kits.FirstOrDefault(x => x.kitName.Equals(targetNameKit));
                    if (kitInfo == null) return;

                    CuiHelper.DestroyUi(player, Layer);

                    var container = new CuiElementContainer();
                    
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                    {
                        FadeIn = 0.2f,
                        Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                        Color = "0 0 0 1"
                    }
                    }, "Overlay", Layer);
                    container.Add(new CuiPanel
                    {
                    Image =
                    {
                        FadeIn = 0.2f,
                        Color = "0.2 0.2 0.17 0.7",
                        Material = "assets/content/ui/uibackgroundblur.mat"
                    }
                    }, Layer);
                    
                    container.Add(new CuiLabel
                    {
                        Text = { Text = targetNameKit.ToUpper(), Align = TextAnchor.UpperCenter, FontSize = 40, Font = "robotocondensed-bold.ttf" },
                        RectTransform = { AnchorMin = "0.3 1", AnchorMax = "0.7 1", OffsetMin = "0 -155", OffsetMax = "0 -91.6" }
                    }, Layer);
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "Данный набор содержит следующие предметы:", Align = TextAnchor.UpperCenter, FontSize = 18, Font = "robotocondensed-regular.ttf" },
                        RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -155", OffsetMax = "0 -133" }
                    }, Layer);

                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            GetImageComponent("https://static.moscow.ovh/images/games/rust//plugins/ultimate_ui/exit.png", "Kits_img_exit"),
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-73.9 20", OffsetMax = "-28.6 80"},
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            new CuiImageComponent {Color = "0.33 0.87 0.59 0.6"},
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 25.2"}
                        }
                    });
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_KITS prev",
                            Close = Layer
                        },
                        Text = { Text = "Вернуться назад", Align = TextAnchor.UpperCenter, FontSize = 18 },
                        RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 49.2" },
                    }, Layer);
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_KITS prev",
                            Close = Layer
                        },
                        Text = { Text = "" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }, Layer);

                    var itemSize = 103.3f;
                    var itemSep = 6.6f;
                    var num = Mathf.Min(6, kitInfo.items.Count);
                    var posX = -(itemSize * num + itemSep * (num - 1)) / 2f;
                    var posY = 0f;
                    
                    for (var i = 0; i < kitInfo.items.Count;)
                    {
                        var item = kitInfo.items[i];
                        container.Add(new CuiPanel
                        {
                            RectTransform =
                            {
                                AnchorMin = "0.5 0.65", AnchorMax = "0.5 0.65", OffsetMin = $"{posX} {posY - itemSize}", OffsetMax = $"{posX + itemSize} {posY}"
                            },
                            Image = { Color = "0 0 0 0.6" }
                        }, Layer, Layer + $".Item{i}");
                        
                        container.Add(new CuiElement
                        {
                            Parent = Layer + $".Item{i}",
                            Components =
                            {
                                GetItemImageComponent(item.shortname),
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                            }
                        });

                        if (item.amount > 1)
                        {
                            container.Add(new CuiLabel()
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-3 -3" },
                                Text = { Text = $"x{item.amount}", Font = "RobotoCondensed-Bold.ttf", Align = TextAnchor.LowerRight, FontSize = 14 }
                            }, Layer + $".Item{i}");
                        }

                        if (++i % 6 == 0)
                        {
                            posY -= itemSize + itemSep;
                            num = Mathf.Min(6, kitInfo.items.Count - i);
                            posX = -(itemSize * num + itemSep * (num - 1)) / 2f;
                        }
                        else posX += itemSize + itemSep;
                    }
                    CuiHelper.AddUi(player, container);

                    break;
                }
                case "showavailablekits":
                {
                    CuiHelper.DestroyUi(player, Layer);

                    var container = new CuiElementContainer();

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            FadeIn = 0.2f,
                            Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                            Color = "0 0 0 1"
                        }
                    }, "Overlay", Layer);
                    container.Add(new CuiPanel
                    {
                        Image =
                        {
                            FadeIn = 0.2f,
                            Color = "0.2 0.2 0.17 0.7",
                            Material = "assets/content/ui/uibackgroundblur.mat"
                        }
                    }, Layer);

                    container.Add(new CuiLabel
                    {
                        Text = { Text = "НАБОРЫ", Align = TextAnchor.UpperCenter, FontSize = 40, Font = "robotocondensed-bold.ttf" },
                        RectTransform = { AnchorMin = "0.3 1", AnchorMax = "0.7 1", OffsetMin = "0 -155", OffsetMax = "0 -91.6" }
                    }, Layer);
                    container.Add(new CuiLabel
                    {
                        Text = { Text = "Вы можете забрать наборы", Align = TextAnchor.UpperCenter, FontSize = 18, Font = "robotocondensed-regular.ttf" },
                        RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -155", OffsetMax = "0 -133" }
                    }, Layer);

                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            GetImageComponent("https://static.moscow.ovh/images/games/rust//plugins/ultimate_ui/exit.png", "Kits_img_exit"),
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-73.9 20", OffsetMax = "-28.6 80"},
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = Layer,
                        Components =
                        {
                            new CuiImageComponent {Color = "0.33 0.87 0.59 0.6"},
                            new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 25.2"}
                        }
                    });
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_KITS close",
                            Close = Layer
                        },
                        Text = { Text = "Покинуть страницу", Align = TextAnchor.UpperCenter, FontSize = 18 },
                        RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 49.2" },
                    }, Layer);
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_KITS close",
                            Close = Layer
                        },
                        Text = { Text = "" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    }, Layer);

                        var kitSizeX = 183.3f;
                    var kitSizeY = 46.6f;
                    var kitSepX = 13.3f;
                    var kitSepY = 24f;
                    var num = Mathf.Min(5, kits.Count);
                    var posX = -(kitSizeX * num + kitSepX * (num - 1)) / 2f;
                    var posY = 0f;
                    
                    for (var i = 0; i < kits.Count;)
                    {
                        var kit = kits.ElementAt(i);
                        var dataPlayer = GetKitData(kitsInfo, kit);
                        var time = dataPlayer.cooldown - TimeHelper.GetTimeStamp();

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.5 0.65", AnchorMax = "0.5 0.65", OffsetMin = $"{posX} {posY - kitSizeY}", OffsetMax = $"{posX + kitSizeX} {posY}"},
                            Text =
                            {
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 18,
                                Text = kit.kitName
                            },
                            Button =
                            {
                                Color = "0 0 0 0.6",
                                Command = $"UI_KITS givekit {kit.kitName} {i}"
                            }
                        }, Layer, Layer + $".Kits{i}");
                        
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-16 -16", OffsetMax = "0 0" },
                            Text =
                            {
                                Text = "?",
                                FontSize = 12,
                                Align = TextAnchor.MiddleCenter
                            },
                            Button =
                            {
                                Color = "0 0 0 0.6",
                                Command = $"UI_KITS showinfokit {kit.kitName}"
                            }
                        }, Layer + $".Kits{i}");

                        if (time < 0)
                        {
                            container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 4.6" },
                                Image = { Color = "0.33 0.87 0.59 0.6" }
                            }, Layer + $".Kits{i}", Layer + $".Kits{i}.Status");
                        }
                        else
                        {
                            container.Add(new CuiLabel
                            {
                                Text =
                                {
                                    Align = TextAnchor.LowerCenter,
                                    FontSize = 13,
                                    Font = "RobotoCondensed-Regular.ttf",
                                    Text = TimeHelper.FormatTime(TimeSpan.FromSeconds(time), 2)
                                },
                                RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 2", OffsetMax = $"0 {kitSepY}" }
                            }, Layer + $".Kits{i}", Layer + $".Kits{i}.Status.Text");
                                container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 4.6" },
                                Image = { Color = "0.87 0.33 0.33 0.5" }
                            }, Layer + $".Kits{i}", Layer + $".Kits{i}.Status");
                        }
                        if (++i % 5 == 0)
                        {
                            posY -= kitSizeY + kitSepY;
                            num = Mathf.Min(5, kits.Count - i);
                            posX = -(kitSizeX * num + kitSepX * (num - 1)) / 2f;
                        }
                        else posX += kitSizeX + kitSepX;
                    }

                    CuiHelper.AddUi(player, container);

                    break;
                }
                case "givekit":
                {
                    var nameKit = arg.GetString(1, "text");

                    int idKit;

                    if (arg.HasArgs(4))
                    {
                        idKit = arg.GetInt(3);
                        nameKit += " " + arg.GetString(2);
                    }
                    else idKit = arg.GetInt(2);

                    var kitInfo1 = _config.kits.Find(kit => kit.kitName == nameKit);
                    if (kitInfo1 == null) return;

                    var playerData = GetKitData(kitsInfo, kitInfo1);

                    var kitData = _config.kits.First(x => x.kitName == nameKit);
                    if (playerData != null)
                    {
                        if (playerData.cooldown > TimeHelper.GetTimeStamp()) return;

                        if (playerData.amount != -1)
                        {
                            if (playerData.amount == 0) return;
                        }

                        GiveItems(player, kitData);
                        playerData.cooldown = TimeHelper.GetTimeStamp() + kitData.cooldownKit;

                        CuiHelper.DestroyUi(player, Layer + $".Kits{idKit}.Status.Text");
                        CuiHelper.DestroyUi(player, Layer + $".Kits{idKit}.Status");
                        CuiHelper.DestroyUi(player, Layer + "Status");

                        var container = new CuiElementContainer();

                        container.Add(new CuiLabel
                        {
                            Text =
                        {
                            Align = TextAnchor.LowerCenter,
                            FontSize = 13,
                            Font = "RobotoCondensed-Regular.ttf",
                            Text = TimeHelper.FormatTime(TimeSpan.FromSeconds(playerData.cooldown - TimeHelper.GetTimeStamp()))
                        },
                            RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 2", OffsetMax = $"0 24" }
                        }, Layer + $".Kits{idKit}", Layer + $".Kits{idKit}.Status.Text");
                        container.Add(new CuiPanel
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 4.6" },
                            Image = { Color = "0.87 0.33 0.33 0.5" }
                        }, Layer + $".Kits{idKit}", Layer + $".Kits{idKit}.Status");

                        container.Add(new CuiLabel
                        {
                            Text = { Text = "Кит успешно выдан и отправлен к вам в инвентарь", Align = TextAnchor.LowerCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" },
                            RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-250 104", OffsetMax = "250 130" }
                        }, Layer, Layer + "Status");
                        CuiHelper.AddUi(player, container);

                        CuiHelper.AddUi(player, container);

                        if (kitData.maxUse != -1) playerData.amount -= 1;
                    }

                    break;
                }
            }
        }

        [ChatCommand("kit")] 
        void KitOpen(BasePlayer player, string command, string[] args)
        { 
            var ret = Interface.Call("CanRedeemKit", player) as string;
             
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
            
            player.SendConsoleCommand("UI_KITS showavailablekits");
        }
        
        [ChatCommand("kits")] 
        void KitsOpen(BasePlayer player, string command, string[] args)
        {
            var ret = Interface.Call("CanRedeemKit", player) as string;
            
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
            
            player.SendConsoleCommand("UI_KITS showavailablekits");
        }

        #endregion
        
        #region Methods
        
        private List<KitInfo> GetKitsForPlayer(BasePlayer player)
        {
            var kitsInfo = GetKitsInfo(player);
            return _config.kits.Where(kit => (string.IsNullOrEmpty(kit.privilege) || permission.UserHasPermission(player.UserIDString, kit.privilege)) && GetKitData(kitsInfo, kit).amount != 0).ToList(); 
        }
         
        private void GiveItems(BasePlayer player, KitInfo kit)
        {
            foreach(var kitItem in kit.items)
            {             
                GiveItem(player,BuildItem(kitItem.shortname,kitItem.amount,kitItem.skinID,kitItem.Condition,kitItem.Weapon,kitItem.Content), kitItem.place == "Панель" ? player.inventory.containerBelt : kitItem.place == "Одежда" ? player.inventory.containerWear : player.inventory.containerMain);
            }
        }
        
        private void GiveItem(BasePlayer player, Item item, ItemContainer cont = null)
        {
            if (item == null) return;
            
            player.GiveItem(item);
        }
        
        private Item BuildItem(string ShortName, int Amount, ulong SkinID, float Condition, Weapon weapon, List<ItemContent> Content)
        {
            Item item = ItemManager.CreateByName(ShortName, Amount > 1 ? Amount : 1, SkinID);
            item.condition = Condition;
            if(weapon != null)
            {
                ((BaseProjectile) item.GetHeldEntity()).primaryMagazine.contents = weapon.ammoAmount;
                ((BaseProjectile) item.GetHeldEntity()).primaryMagazine.ammoType = ItemManager.FindItemDefinition(weapon.ammoType);
            }
            if(Content != null)
            {
                foreach(var cont in Content)
                {
                    Item newCont = ItemManager.CreateByName(cont.ShortName, cont.Amount);
                    newCont.condition = cont.Condition;
                    newCont.MoveToContainer(item.contents);
                }
            }
            return item;
        }
        
        private List<ItemInfo> GetPlayerItems(BasePlayer player)
        {
            List<ItemInfo> kititems = new List<ItemInfo>();
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "Одежда");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "Рюкзак");
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, "Панель");
                    kititems.Add(iteminfo);
                }
            }
            return kititems;
        }
        
        private ItemInfo ItemToKit(Item item, string container)
        {
            ItemInfo itemInfo = new ItemInfo();

            itemInfo.amount = item.amount;
            itemInfo.place = container;
            itemInfo.shortname = item.info.shortname;
            itemInfo.Condition = item.condition;
            itemInfo.skinID = item.skin;
            itemInfo.Weapon = null;
            itemInfo.Content = null;
            
            if(item.info.category == ItemCategory.Weapon)
            {
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if(weapon != null)
                {
                    itemInfo.Weapon = new Weapon();
                    itemInfo.Weapon.ammoType = weapon.primaryMagazine.ammoType.shortname;
                    itemInfo.Weapon.ammoAmount = weapon.primaryMagazine.contents;
                }
            }
            
            if(item.contents != null)
            {
                itemInfo.Content = new List<ItemContent>();
                foreach (var cont in item.contents.itemList)
                {
                    itemInfo.Content.Add(new ItemContent()
                    {
                        Amount = cont.amount,
                        Condition = cont.condition,
                        ShortName = cont.info.shortname
                    });
                }
            }

            return itemInfo;
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

        #endregion 

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
                spawnKit = new List<ItemInfo>()
                {
                    new ItemInfo()
                    {
                        amount = 1,
                        shortname = "rock",
                        Content = null,
                        place = "Панель",
                    }
                }
            };
        }
        
        public Configuration _config;

        public class Configuration
        {
            [JsonProperty("Набор на респавне")] public List<ItemInfo> spawnKit = new List<ItemInfo>();
            [JsonProperty("Наборы")] public List<KitInfo> kits = new List<KitInfo>();
        }
        
        #endregion

        #region Class

        public class KitInfo
        {
            [JsonProperty("Название")] public string kitName = "";
            [JsonProperty("Максимум использований")] public int maxUse = 0;
            [JsonProperty("Кулдаун")] public double cooldownKit = 0;
            [JsonProperty("Привилегия")] public string privilege = "";
            [JsonProperty("Предметы")] public List<ItemInfo> items = new List<ItemInfo>();
        }
        
        public class ItemInfo
        {
            [JsonProperty("Позиция")] public int position = 0;
            [JsonProperty("Shortname")] public string shortname = "";
            [JsonProperty("Количество")] public int amount = 0;
            [JsonProperty("Место")] public string place = "Рюкзак";
            [JsonProperty("Скин")] public ulong skinID = 0U;
            [JsonProperty("Контейнер")] public List<ItemContent> Content { get; set; }
            [JsonProperty("Прочность")] public float Condition { get; set; }
            [JsonProperty("Оружие")] public Weapon Weapon { get; set; }
        }
        
        public class Weapon
        {
            public string ammoType { get; set; }
            public int ammoAmount { get; set; }
        }
        public class ItemContent
        {
            public string ShortName { get; set; }
            public float Condition { get; set; }
            public int Amount { get; set; }
        }

        #endregion
        
        #region Data

        class StoredData
        {
            public Dictionary<ulong, KitsInfo> players = new Dictionary<ulong, KitsInfo>();
        }

        class KitsInfo
        {
            public Dictionary<string, KitData> kits = new Dictionary<string, KitData>();
        }

        class KitData
        {
            [JsonProperty("a")]
            public int amount = 0;
            
            [JsonProperty("cd")]
            public double cooldown = 0;
        }
        
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Temporary/Kits/kits", storedData);
        }

        void LoadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("Temporary/Kits/kits");
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load data: {ex}");
            }
            
            if (storedData == null)
                storedData = new StoredData();
        }

        StoredData storedData;
        
        #endregion

        #region Helper

        private static class TimeHelper
        {
            public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
            {
                string result = string.Empty;
                switch (language)
                {
                    case "ru":
                        int i = 0;
                        if (time.Days != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Days, "д", "д", "д")}";
                            i++;
                        }

                        if (time.Hours != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Hours, "ч", "ч", "ч")}";
                            i++;
                        }

                        if (time.Minutes != 0 && i < maxSubstr)
                        {
                            if (!string.IsNullOrEmpty(result))
                                result += " ";

                            result += $"{Format(time.Minutes, "м", "м", "м")}";
                            i++;
                        }

                        
                        
                        if (time.Days == 0)
                        {
                            if (time.Seconds != 0 && i < maxSubstr)
                            {
                                if (!string.IsNullOrEmpty(result))
                                    result += " ";

                                result += $"{Format(time.Seconds, "с", "с", "с")}";
                                i++;
                            }
                        }

                        break;
                    case "en":
                        result = string.Format("{0}{1}{2}{3}",
                            time.Duration().Days > 0
                                ? $"{time.Days:0} day{(time.Days == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Hours > 0
                                ? $"{time.Hours:0} hour{(time.Hours == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Minutes > 0
                                ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? String.Empty : "s")}, "
                                : string.Empty,
                            time.Duration().Seconds > 0
                                ? $"{time.Seconds:0} second{(time.Seconds == 1 ? String.Empty : "s")}"
                                : string.Empty);

                        if (result.EndsWith(", ")) result = result.Substring(0, result.Length - 2);

                        if (string.IsNullOrEmpty(result)) result = "0 seconds";
                        break;
                }

                return result;
            }

            private static string Format(int units, string form1, string form2, string form3)
            {
                var tmp = units % 10;

                if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                    return $"{units}{form1}";

                if (tmp >= 2 && tmp <= 4)
                    return $"{units}{form2}";

                return $"{units}{form3}";
            }

            private static DateTime Epoch = new DateTime(1970, 1, 1);

            public static double GetTimeStamp()
            {
                return DateTime.Now.Subtract(Epoch).TotalSeconds;
            }
        }

        #endregion
		
		
		public CuiRawImageComponent GetAvatarImageComponent(ulong user_id, string color = "1.0 1.0 1.0 1.0"){
			
			if (plugins.Find("ImageLoader")) return plugins.Find("ImageLoader").Call("BuildAvatarImageComponent",user_id) as CuiRawImageComponent;
			if (plugins.Find("ImageLibrary")) {
				return new CuiRawImageComponent { Png = (string)plugins.Find("ImageLibrary").Call("GetImage", user_id.ToString()), Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga" };
			}
			return new CuiRawImageComponent {Url = "https://image.flaticon.com/icons/png/512/37/37943.png", Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
		}
		public CuiRawImageComponent GetImageComponent(string url, string shortName="", string color = "1.0 1.0 1.0 1.0"){
			
			if (plugins.Find("ImageLoader")) return plugins.Find("ImageLoader").Call("BuildImageComponent",url) as CuiRawImageComponent;
			if (plugins.Find("ImageLibrary")) {
				if (!string.IsNullOrEmpty(shortName)) url = shortName;
				//Puts($"{url}: "+ (string)plugins.Find("ImageLibrary").Call("GetImage", url));
				return new CuiRawImageComponent { Png = (string)plugins.Find("ImageLibrary").Call("GetImage", url), Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
			}
			return new CuiRawImageComponent {Url = url, Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
		}
		public CuiRawImageComponent GetItemImageComponent(string shortName){
			string itemUrl = shortName;
			if (plugins.Find("ImageLoader")) {itemUrl = $"https://static.moscow.ovh/images/games/rust/icons/{shortName}.png";}
            return GetImageComponent(itemUrl, shortName);
		}
		public bool AddImage(string url,string shortName=""){
			if (plugins.Find("ImageLoader")){				
				plugins.Find("ImageLoader").Call("CheckCachedOrCache", url);
				return true;
			}else
			if (plugins.Find("ImageLibrary")){
				if (string.IsNullOrEmpty(shortName)) shortName=url;
				plugins.Find("ImageLibrary").Call("AddImage", url, shortName);
				//Puts($"Add Image {shortName}");
				return true;
			}	
			return false;		
		}
		
    }
}