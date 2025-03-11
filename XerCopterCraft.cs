using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XerCopterCraft", "https://topplugin.ru/", "1.0.1")]
    class XerCopterCraft : RustPlugin
    {
        #region Reference

        [PluginReference] Plugin ImageLibrary;
        private static XerCopterCraft __ins;
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        //public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);

        public static class IMGLibrary
        {
            public static bool AddImage(string url, string imageName, ulong imageId = 0, Action callback = null) => (bool)__ins.ImageLibrary.Call("AddImage", url, imageName, imageId, callback);
            public static bool AddImageData(string imageName, byte[] array, ulong imageId = 0, Action callback = null) => (bool)__ins.ImageLibrary.Call("AddImageData", imageName, array, imageId, callback);
            public static string GetImageURL(string imageName, ulong imageId = 0) => (string)__ins.ImageLibrary.Call("GetImageURL", imageName, imageId);
            public static string GetImage(string imageName, ulong imageId = 0, bool returnUrl = false) => (string)__ins.ImageLibrary.Call("GetImage", imageName, imageId, returnUrl);
            public static List<ulong> GetImageList(string name) => (List<ulong>)__ins.ImageLibrary.Call("GetImageList", name);
            public static Dictionary<string, object> GetSkinInfo(string name, ulong id) => (Dictionary<string, object>)__ins.ImageLibrary.Call("GetSkinInfo", name, id);
            public static bool HasImage(string imageName, ulong imageId) => (bool)__ins.ImageLibrary.Call("HasImage", imageName, imageId);
            public static bool IsInStorage(uint crc) => (bool)__ins.ImageLibrary.Call("IsInStorage", crc);
            public static bool IsReady() => (bool)__ins.ImageLibrary.Call("IsReady");
            public static void ImportImageList(string title, Dictionary<string, string> imageList, ulong imageId = 0, bool replace = false, Action callback = null) => __ins.ImageLibrary.Call("ImportImageList", title, imageList, imageId, replace, callback);
            public static void ImportItemList(string title, Dictionary<string, Dictionary<ulong, string>> itemList, bool replace = false, Action callback = null) => __ins.ImageLibrary.Call("ImportItemList", title, itemList, replace, callback);
            public static void ImportImageData(string title, Dictionary<string, byte[]> imageList, ulong imageId = 0, bool replace = false, Action callback = null) => __ins.ImageLibrary.Call("ImportImageData", title, imageList, imageId, replace, callback);
            public static void LoadImageList(string title, List<KeyValuePair<string, ulong>> imageList, Action callback = null) => __ins.ImageLibrary.Call("LoadImageList", title, imageList, callback);
            public static void RemoveImage(string imageName, ulong imageId) => __ins?.ImageLibrary?.Call("RemoveImage", imageName, imageId);
        }

        #endregion

        #region Var
        private string prefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        #endregion

        #region Configuration
        private static Configuration config = new Configuration();

        private class Configuration
        {
            [JsonProperty("SkinId (Иконка в инвентаре)")]
            public ulong skinID = 1680939801;
            [JsonProperty("Миникоптер(Эту вещь игрок будет держать в руках,когда поставит - он заменится на коптер)")]
            public string Item = "electric.flasherlight";
            [JsonProperty("Название вещи в инвентаре")]
            public string ItemName = "Minicopter";
            [JsonProperty("Вещи для крафта")]
            public Dictionary<string, int> CraftItemList = new Dictionary<string, int>();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    CraftItemList = new Dictionary<string, int>
                    {
                        ["metalblade"] = 10,
                        ["rope"] = 15,
                        ["gears"] = 15,
                        ["stones"] = 5,
                        ["fuse"] = 1,
                        ["wood"] = 5000,
                        ["metal.fragments"] = 6500,
                    }
                };
            }
            
            internal class Interface
            {
                [JsonProperty("Title в меню")]
                public string TitleMenu = "Создание миникоптера";
                [JsonProperty("Title предметов в меню")]
                public string TitleItems = "Список предметов,которые требуются для создания миникоптера";
                [JsonProperty("Текст в кнопке")]
                public string ButtonTitle = "Создать";
                [JsonProperty("Символ показывающий,что у игрока достаточно предметов на крафт")]
                public string Sufficiently = "√";
                [JsonProperty("Цвет символа(HEX)")]
                public string SufficientlyColor = "#33F874FF";
                [JsonProperty("Цвет показателя,сколько необходимо еще компонентов на создание")]
                public string IndispensablyColor = "#F83232FF";
                [JsonProperty("Minicopter.png(512x512)")]
                public string CopterPNG = "https://i.imgur.com/PoeTa16.png";
            }

            internal class Other
            {
                [JsonProperty("Звук при создании коптера")]
                public string EffectCreatedCopter = "assets/prefabs/deployable/tier 1 workbench/effects/experiment-start.prefab";
                [JsonProperty("Звук когда у игрока недостаточно ресурсов")]
                public string EffectCanceled = "assets/prefabs/npc/autoturret/effects/targetlost.prefab";
            }

            [JsonProperty("Настройки интерфейса")]
            public Interface InterfaceSettings = new Interface();
            [JsonProperty("Дополнительные настройки")]
            public Other OtherSettings = new Other();
        }
    
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
      
        #endregion

        #region Commands

        [ChatCommand("copter")]
        void OpenCraftMenu(BasePlayer player)
        {
            OpenMenuCraft(player);
        }

        [ConsoleCommand("craft_copter")]
        void CraftCopter(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (!CraftCheck(player))
            {
                Effect.server.Run(config.OtherSettings.EffectCanceled, player.transform.localPosition);
                MessageUI(player, "Недостаточно ресурсов", config.InterfaceSettings.IndispensablyColor);
                return;
            }
            foreach (var item in config.CraftItemList)
            {
                player.inventory.Take(null, ItemManager.FindItemDefinition(item.Key).itemid, item.Value);
            }
            GiveMinicopter(player);
            MessageUI(player, "Миникоптер создан успешно", config.InterfaceSettings.SufficientlyColor);
            Effect.server.Run(config.OtherSettings.EffectCreatedCopter, player.transform.localPosition);
            CuiHelper.DestroyUi(player, MainPanel);
            LogToFile("XerCopterLog", $"{player.displayName + "/" + player.UserIDString} скрафтил коптер",this);
            PrintWarning($"{player.displayName + "/" + player.UserIDString} скрафтил коптер");
        }

        [ConsoleCommand("give_minicopter")]
        void GiveMinicopterCommand(ConsoleSystem.Arg args)
        {
            if (!(args.IsAdmin || args.IsRcon)) return;
            BasePlayer target = BasePlayer.FindByID(ulong.Parse(args.Args[0]));
            if (target == null) { PrintWarning("Игрока нет на сервере!Он не получил миникоптер!"); return; };
            GiveMinicopter(target);
            PrintWarning($"Миникоптер выдан игроку {target.userID}");
            if(target.IsConnected)
               MessageUI(target, "Вы получили миникоптер", config.InterfaceSettings.SufficientlyColor);
        }

        #endregion

        #region Hooks

        private string notifimage = "https://imgur.com/26ZzZI5.png";
        private string bell = "https://imgur.com/e4Jg2qz.png";
        string MainIMG = "https://imgur.com/uXKc6US.png";
        
        
        void OnServerInitialized()
        {
            __ins = this;
            IMGLibrary.AddImage(config.InterfaceSettings.CopterPNG, "CopterImage");
            ImageLibrary.Call("AddImage", notifimage, "info");
            ImageLibrary.Call("AddImage", bell, "bell");
            ImageLibrary.Call("AddImage", MainIMG, "MainIMG");
            
            foreach (var items in config.CraftItemList) 
            {
                if (!string.IsNullOrEmpty(items.Key) && !IMGLibrary.HasImage(items.Key, 0)) IMGLibrary.AddImage("https://rustlabs.com/img/items180/" + items.Key + ".png", items.Key, 0);
            }
            
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            CheckDeploy(go.ToBaseEntity());
        }

        #endregion

        #region Main

        [ConsoleCommand("giveminicopter")]
        void consolegiveminic(ConsoleSystem.Arg args)
        {
            if(!args.IsAdmin) return;
            GiveMinicopter(args.Player());
        }
        #region Main

        private void SpawnCopter(Vector3 position, Quaternion rotation = default(Quaternion), ulong ownerID = 0)
        {
            MiniCopter copter = (MiniCopter)GameManager.server.CreateEntity(prefab, position, rotation);
            if (copter == null) { return; }
            copter.Spawn();
        }

        private void GiveMinicopter(BasePlayer player, bool pickup = false)
        {
            var item = CreateItem();
            player.GiveItem(item);
        }

        private void CheckDeploy(BaseEntity entity)
        {
            if (entity == null) { return; }
            if (!CopterCheck(entity.skinID)) { return; }
            SpawnCopter(entity.transform.position, entity.transform.rotation, entity.OwnerID);
            timer.Once(0.5f, () => { entity.Kill(); });
        }

        private bool CopterCheck(ulong skin)
        {
            return skin != 0 && skin == config.skinID;
        }

        private Item CreateItem()
        {
            var item = ItemManager.CreateByName(config.Item, 1, config.skinID);
            if (item == null)
            {
                return null;
            }
            item.name = config.ItemName;
            return item;
        }

        private bool CraftCheck(BasePlayer player)
        {
            var craft = config.CraftItemList;
            var more = new Dictionary<string, int>();

            foreach (var component in craft)
            {
                var name = component.Key;
                var has = player.inventory.GetAmount(ItemManager.FindItemDefinition(component.Key).itemid);
                var need = component.Value;
                if (has < component.Value)
                {
                    if (!more.ContainsKey(name))
                    {
                        more.Add(name, 0);
                    }

                    more[name] += need - has;
                }
            }

            if (more.Count == 0)
                return true;
            else
                return false;
        }

        private bool UseCraft(BasePlayer player, string Short)
        {
            var craft = config.CraftItemList;
            var more = new Dictionary<string, int>();

            foreach (var component in craft)
            {
                var name = component.Key;
                var has = player.inventory.GetAmount(ItemManager.FindItemDefinition(component.Key).itemid);
                var need = component.Value;
                if (has < component.Value)
                {
                    if (!more.ContainsKey(name))
                    {
                        more.Add(name, 0);
                    }

                    more[name] += need - has;
                }
            }

            if (more.ContainsKey(Short))
                return true;
            else
                return false;
        }

        #endregion

        #endregion

        #region UI

        #region Parent
        static string MainPanel = "XCC_MAINPANEL_skykey";
        static string CraftItemsPanel = "XCC_CRAFT_ITEMS_PANEL";
        static string ItemParent = "XCC_CRAFT_ITEMS_PARENT";
        static string MessagePanel = "XCC_MESSAGE_PANEL";
        #endregion

        
        
        #region Message

        void MessageUI(BasePlayer player, string Messages, string Color)
        {
            CuiHelper.DestroyUi(player, MessagePanel);
            CuiElementContainer container = new CuiElementContainer();

            
                container.Add(new CuiPanel
                    {
                        Image = { FadeIn = 1f, Color = HexToRustFormat("#d4a1d400") },
                        RectTransform = { AnchorMin = "0.3947916 0.8259259", AnchorMax = "0.6682292 0.9287037" },
                        CursorEnabled = false,
                    }, "Overlay", MessagePanel);
                    container.Add(new CuiElement
                    {
                        Parent = MessagePanel,
                       //Name = "background",
                        Components =
                        {
                            //new CuiTextComponent { FadeIn = cfg.fadein, Text = "Уведомление", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "RobotoCondensed-bold.ttf" },
                            new CuiImageComponent { Png = (string) ImageLibrary.Call("GetImage", "info"), Material = "assets/icons/greyout.mat"},
                            new CuiRectTransformComponent {AnchorMin = "0.1012163 0.2342343", AnchorMax = "0.9657144 0.8018019"},
                            //new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = MessagePanel,
                        //Name = "background",
                        Components =
                        {
                            new CuiTextComponent { FadeIn = 1f, Text = "Уведомление", Align = TextAnchor.UpperLeft, FontSize = 16, Font = "RobotoCondensed-bold.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.1658087 0.4234225", AnchorMax = "0.6905609 0.7837829"},
                            //new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = MessagePanel,
                        //Name = "background",
                        Components =
                        {
                            new CuiTextComponent { FadeIn = 1f, Text = String.Format(Messages), Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "RobotoCondensed-regular.ttf" },
                            new CuiRectTransformComponent {AnchorMin = "0.1564356 0.2612609", AnchorMax = "0.9168315 0.6126121"},
                            //new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    container.Add(new CuiElement
                    {
                        Parent = MessagePanel,
                        //Name = "background",
                        Components =
                        {
                            //new CuiTextComponent { FadeIn = cfg.fadein, Text = "Уведомление", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "RobotoCondensed-bold.ttf" },
                            new CuiImageComponent { Png = (string) ImageLibrary.Call("GetImage", "bell"), Material = "assets/icons/greyout.mat"},
                            new CuiRectTransformComponent {AnchorMin = "0 0.09909844", AnchorMax = "0.1980195 0.9999994"},
                            //new CuiOutlineComponent {Color = "0 0 0 0", Distance = "0.3 0.3"}
                        }
                    });
                    

            CuiHelper.AddUi(player, container);

            timer.Once(2f, () => { CuiHelper.DestroyUi(player, MessagePanel); });
        }

        #endregion

        [ConsoleCommand("CloseUI21315asd")]
        private void CloseUI(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, MainPanel);
            CuiHelper.DestroyUi(player, "container123412");
        }

        
        #region MainMenu

        void OpenMenuCraft(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MainPanel);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true,
            }, "Overlay", MainPanel);

            container.Add(new CuiElement
            {
                Parent = MainPanel,
                Name = "container123412",
                Components =
                {
                    new CuiImageComponent { Png = GetImage(MainIMG), Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.2406265 0.1981481", AnchorMax = "0.7598959 0.795370" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0", Close = MainPanel },
                Text = { Text = "" }
            }, "container123412");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3842712 0.8139536", AnchorMax = "0.6459364 0.9689922" },
                Text = { Text = $"КОПТЕРЫ", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.UpperCenter, FontSize = 28, Font = "robotocondensed-bold.ttf" }
            },  "container123412");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3741207 0.7937984", AnchorMax = "0.665997 0.9100775" },
                Text = { Text = $"Здесь вы можете скрафтить коптер", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.UpperCenter, FontSize = 13, Font = "robotocondensed-regular.ttf" }
            },  "container123412");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4178125 0.5546297", AnchorMax = "0.5895833 0.5962934" },
                Button = { Command = "craft_copter", Color = HexToRustFormat("#319A56FF") },
                Text = { FadeIn = 0.9f, Text = "СОЗДАТЬ",Align = TextAnchor.MiddleCenter, FontSize = 15 }
            }, "container123412");

            container.Add(new CuiElement
            {
                Parent = "container123412",
                Components =
                {
                    new CuiRawImageComponent {
                        Png = GetImage("CopterImage"), 
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.4189581 0.6",
                        AnchorMax = "0.6168745 0.8925924"
                    },
                }
            });

            #region CraftItems

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1382527 0.06356597", AnchorMax = "0.8696083 0.4573647" },
                Image = { Color = "0 0 0 0" }
            },  "container123412", CraftItemsPanel);

            int x = 0, y = 0, i = 0;
            foreach (var items in config.CraftItemList)
            {
                string color = UseCraft(player, items.Key) ? "#A60D0D2F" : "#1FB91931";

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.01386664 + (x * 0.17)} {0.5463711 - (y * 0.45)}", AnchorMax = $"{0.1416122 + (x * 0.17)} {0.8891131 - (y * 0.45)}" },
                    Image = { Color = HexToRustFormat(color) }
                }, CraftItemsPanel, $"Item_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"Item_{i}",
                    Components =
                    {
                    new CuiRawImageComponent {
                        Png = GetImage(items.Key),
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                }
                });

                var has = player.inventory.GetAmount(ItemManager.FindItemDefinition(items.Key).itemid);
                var result = items.Value - has <= 0 ? $"<color={config.InterfaceSettings.SufficientlyColor}>{config.InterfaceSettings.Sufficiently}</color>" : $"<color={config.InterfaceSettings.IndispensablyColor}>{Convert.ToInt32(items.Value - has).ToString()}</color>";

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = String.Format(result.ToString()), Align = TextAnchor.LowerCenter, Font = "robotocondensed-bold.ttf", FontSize = 10, Color = HexToRustFormat("#FFFFFFFF") }
                }, $"Item_{i}");


                x++; i++;
                if (x == 6)
                {
                    x = 0;
                    y++;
                }
                if (x == 6 && y == 1)
                    break;
            }
            #endregion

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #endregion

        #region Help

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

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion        
    }
}
