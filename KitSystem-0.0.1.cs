using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("KitSystem", "Chibubrik / Deversive", "0.0.1")]
    class KitSystem : RustPlugin
    {
        #region Вар
        private static string KitsDostyp = "kits.dostup";
        private string Layer = "Kit_UI";
        private string Layer1 = "KitS_UI";

        [PluginReference] Plugin ImageLibrary, NoteUI;

        Dictionary<ulong, Data> Settings = new Dictionary<ulong, Data>();
        private Dictionary<BasePlayer, List<string>> _kitsGUI = new Dictionary<BasePlayer, List<string>>();
        #endregion

        #region Класс
        public class KitSettings 
        {
            [JsonProperty("Название набора")] public string Name;
            [JsonProperty("Формат названия набора")] public string DisplayName;
            [JsonProperty("Картинка кита")] public string Image;
            [JsonProperty("Название кулдаун набора")] public double Cooldown;
            [JsonProperty("Привилегия")] public string Perm;
            [JsonProperty("Предметы набора")] public List<ItemSettings> Items;
        }

        public class ItemSettings
        {
            [JsonProperty("Название предмета")] public string ShortName;
            [JsonProperty("Количество предметов")] public int Amount;
            [JsonProperty("Изучение")] public int Blueprint;
            [JsonProperty("Condition")] public float Condition;
            [JsonProperty("Weapon")] public Weapon Weapon;
            [JsonProperty("Content")] public List<ItemContent> Content;
            [JsonProperty("Скин предмета")] public ulong SkinID;
            [JsonProperty("Место в инвентаре")] public string Container;
        }
        public class Weapon
        {
            [JsonProperty("Тип пули")] public string ammoType;
            [JsonProperty("Количество")] public int ammoAmount;
        }
        public class ItemContent
        {
            [JsonProperty("Название предмета")] public string ShortName;
            [JsonProperty("Количество предметов")] public int Amount;
            [JsonProperty("Condition")] public float Condition;
        }
        
        
        public class AutoKit
        {
            [JsonProperty("Наборы при респавне")] public List<string> CustomKit;
        }

        public class Data
        {
            [JsonProperty("Список наборов и их кулдаун")] public Dictionary<string, KitData> SettingsData = new Dictionary<string, KitData>();
        }

        public class KitData
        {
            [JsonProperty("Кулдаун набора")] public double Cooldown;
        }
        #endregion

        #region Конфиг
        Configuration config;
        class Configuration 
        {
            [JsonProperty("Настройки наборов")] public List<KitSettings> settings;
            public static Configuration GetNewConfig() 
            {
                return new Configuration
                {
                    settings = new List<KitSettings>() 
                    {
                        new KitSettings 
                        {
                            Name = "start",
                            DisplayName = "Start",
                            Image = "https://imgur.com/xfVTgqN.png",
                            Cooldown = 10,
                            Perm = "kits.default",
                            Items = new List<ItemSettings>()
                            {
                                new ItemSettings
                                {
                                    ShortName = "wood",
                                    Amount = 1000,
                                    SkinID = 0,
                                    Container = "Main"
                                }
                            }
                        },
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
            LoadImage();


            foreach (var check in config.settings)
            {
                permission.RegisterPermission(check.Perm, this);
                permission.RegisterPermission(KitsDostyp, this);
                if (!string.IsNullOrEmpty(check.Image))
                    ImageLibrary.Call("AddImage", check.Image, check.Image);
            }

            foreach(BasePlayer check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        
        #region ImageLibrary
        
        
        
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);

        private string fonkits = "https://imgur.com/u3nFy3w.png";
        private string nabor = "https://imgur.com/4gT6gsP.png";

        void LoadImage()
        {
            AddImage(fonkits, "fonkits");
            AddImage(nabor, "nabor");
        }
        
        #endregion

        void OnPlayerConnected(BasePlayer player) => CreateDataBase(player); 

        void OnPlayerDisconnected(BasePlayer player, string reason) => SaveDataBase(player.userID);

        void Unload() 
        {
            foreach(var check in Settings)
                SaveDataBase(check.Key);

            foreach(BasePlayer check in BasePlayer.activePlayerList)
                DestroyUi(check);
        }
        #endregion

        #region Методы
        KitData GetDataBase(ulong userID, string name)
        {
            if (!Settings.ContainsKey(userID))
                Settings[userID].SettingsData = new Dictionary<string, KitData>();

            if (!Settings[userID].SettingsData.ContainsKey(name))
                Settings[userID].SettingsData[name] = new KitData();

            return Settings[userID].SettingsData[name];
        }

        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            result = $"{time.Hours.ToString("00")}:";
            result += $"{time.Minutes.ToString("00")}:";
            result += $"{time.Seconds.ToString("00")}";
            return result;
        }
        
        void DestroyUi(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.DestroyUi(player, Layer1);
            _kitsGUI.Remove(player);
        }

        void UpdateInterface(BasePlayer player, string Name)
        {
            var check = config.settings.FirstOrDefault(z => z.Name == Name);
            CuiHelper.DestroyUi(player, $"Name.{check.Name}");
            CuiHelper.DestroyUi(player, $"Info.{check.Name}");
            CuiHelper.DestroyUi(player, $"Take");
            var container = new CuiElementContainer();
            var db = GetDataBase(player.userID, check.Name);
            var Time = CurTime();

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0.03 0", AnchorMax = $"1 1", OffsetMin = "0 1", OffsetMax = "0 -1" },
                Text = { Text = $"<b><size=14>{check.DisplayName}</size></b>\nКолличество предметов: {check.Items.Count()}, Подождите: {FormatShortTime(TimeSpan.FromSeconds(db.Cooldown - Time))}", Color = "1 1 1 0.6", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, $"{check.Name}", $"Name.{check.Name}");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1", Command = $"kit info {check.Name}" },
                Text = { Text = "" }
            }, $"{check.Name}", $"Info.{check.Name}");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.2145 {0.232 + HeihtMax}", AnchorMax = $"0.376 {0.265f + HeihtMax}", OffsetMax = "0 0" },
                Button = { Color = "0.71 0.24 0.24 0.6" },
                Text = { Text = $"ПОДОЖДИТЕ: {FormatShortTime(TimeSpan.FromSeconds(db.Cooldown - Time))}", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer, "Take");

            CuiHelper.AddUi(player, container);
        }
        
        double CurTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;
        #endregion

        #region Дата
        void CreateDataBase(BasePlayer player)
        {
            var DataBase = Interface.Oxide.DataFileSystem.ReadObject<Data>($"KitSystem/{player.userID}");
            
            if (!Settings.ContainsKey(player.userID))
                Settings.Add(player.userID, new Data());
             
            Settings[player.userID] = DataBase ?? new Data();
        }

        void SaveDataBase(ulong userId) => Interface.Oxide.DataFileSystem.WriteObject($"KitSystem/{userId}", Settings[userId]);
        #endregion

        #region Создание наборов
        void CreateKit(BasePlayer player, string kitname)
        {
            if (config.settings.Exists(x => x.Name == kitname))
            {
                player.ConsoleMessage($"Набор {kitname} уже существует!");
                return;
            }
            config.settings.Add(new KitSettings
            {
                Name = kitname,
                DisplayName = kitname,
                Image = "https://imgur.com/xfVTgqN.png",
                Cooldown = 600,
                Perm = "kitsystem.use",
                Items = GetItems(player)
            }
            );
            player.ConsoleMessage($"Вы успешно создали набор {kitname}");
            SaveConfig();
        }

        List<ItemSettings> GetItems(BasePlayer player)
        {
            
            List<ItemSettings> kititems = new List<ItemSettings>();
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    var iteminfo = CreateItem(item, "wear");
                    kititems.Add(iteminfo);
                }
            }

            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item != null)
                {
                    var iteminfo = CreateItem(item, "main");
                    kititems.Add(iteminfo);
                }
            }

            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item != null)
                {
                    var iteminfo = CreateItem(item, "belt");
                    kititems.Add(iteminfo);
                }
            }

            return kititems;
        }
        
        
        ItemSettings CreateItem(Item item, string container)
        {
            ItemSettings items = new ItemSettings();
            items.Amount = item.amount;
            items.Container = container;
            items.SkinID = item.skin;
            items.Blueprint = item.blueprintTarget;
            items.ShortName = item.info.shortname;
            items.Condition = item.condition;
            items.Weapon = null;
            items.Content = null;
            if (item.info.category == ItemCategory.Weapon)
            {
                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    items.Weapon = new Weapon();
                    items.Weapon.ammoType = weapon.primaryMagazine.ammoType.shortname;
                    items.Weapon.ammoAmount = weapon.primaryMagazine.contents;
                }
            }
            if (item.contents != null)
            { 
                items.Content = new List<ItemContent>();
                foreach (var cont in item.contents.itemList)
                {
                    items.Content.Add(new ItemContent()
                        { Amount = cont.amount, Condition = cont.condition, ShortName = cont.info.shortname });
                }
            }

            return items;
        }
        #endregion

        #region Команды

        [ConsoleCommand("closeui1231")]
        void closeui1231(BasePlayer player, string command, string[] args) => DestroyUi(player);
        
        [ChatCommand("kits")]
        void ChatKits(BasePlayer player, string command, string[] args) => ChatKit(player, command, args);
        
        [ChatCommand("kit")]
        void ChatKit(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, KitsDostyp)) 
            {
                SendReply(player, "Unknown Command: Kit");
                        player.SendConsoleCommand("gametip.showgametip", string.Format("У  вас нету доступа к этой команде"));
                        timer.Once(5f, () => player.SendConsoleCommand("gametip.hidegametip"));
                return;
            }
            
            if (player == null) return;
            
            if (_kitsGUI.ContainsKey(player))
            {
                DestroyUi(player);
                return;
            }
            else
            {
                CloseUI(player);
                KitUI(player);
                return;
            }
        }

        [ConsoleCommand("kit")]
void ConsoleKit(ConsoleSystem.Arg args)
{
    var Time = CurTime();
    var player = args.Player();
    if (player != null && args.HasArgs(1))
    {
        if (args.Args[0] == "new")
        {
            if (!player.IsAdmin) return;
            if (args.Args.Length < 2)
            {
                player.ConsoleMessage("Используйте: kit new [название набора]");
                return;
            }
            CreateKit(player, args.Args[1]);
        }
        else if (args.Args[0] == "remove")
        {
            if (!player.IsAdmin) return;
            if (args.Args.Length < 2)
            {
                player.ConsoleMessage("Используйте: kit remove [название набора]");
                return;
            }
            if (config.settings.RemoveAll(z => z.Name == args.Args[1]) <= 0)
            {
                player.ConsoleMessage("Этого набора не существует!");
                return;
            }
            player.ConsoleMessage("Набор успешно удален!");
            SaveConfig();
        }                
        else if (args.Args[0] == "ui")
        {
            DestroyUi(player);
        }
        /*else if (args.Args[0] == "info")
        {
            InfoKitUI(player, args.Args[1]);
        }*/
        else if (args.Args[0] == "skip")
        {
            KitListUI(player, int.Parse(args.Args[1]));
            CuiHelper.DestroyUi(player, "Take");
        }
        else if (args.Args[0] == "take")
        {
            var check = config.settings.FirstOrDefault(z => z.Name == args.Args[1]);
            if (!permission.UserHasPermission(player.UserIDString, check.Perm))
            {
                SendReply(player, $"<size=12>Набор <color=#ee3e61>{check.DisplayName}</color> недоступен!</size>");
                return;
            }
                    
            int beltcount = check.Items.Where(i => i.Container == "belt").Count();
            int wearcount = check.Items.Where(i => i.Container == "wear").Count();
            int maincount = check.Items.Where(i => i.Container == "main").Count();
            int totalcount = beltcount + wearcount + maincount;
            if ((player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count) < beltcount || (player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count) < wearcount || (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count) < maincount) if (totalcount > (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count))
            {
                player.SendConsoleCommand($"note.inv {player.userID} -1 \"Недостаточно места\"");
                player.SendConsoleCommand("gametip.showgametip", "Недостаточно места");
                timer.Once(5f, () => player.SendConsoleCommand("gametip.hidegametip"));
                return;
            }

            /*if (player.inventory.containerMain.itemList.Count >= 24)
            {
                player.SendConsoleCommand($"note.inv {player.userID} -1 \"Недостаточно места\"");
                player.SendConsoleCommand("gametip.showgametip", "Недостаточно места");
                timer.Once(5f, () => player.SendConsoleCommand("gametip.hidegametip"));
                return;
            }*/
            var db = GetDataBase(player.userID, check.Name);
            if (check.Cooldown > 0) 
            {
                if (db.Cooldown > Time)
                {
                    SendReply(player, "<size=12>У вас временный тайм-аут на киты</size>");
                    return;
                }
                db.Cooldown = Time + check.Cooldown;
            }
            UpdateInterface(player, check.Name);
            foreach (var item in check.Items)
            {
                var items = ItemManager.CreateByItemID(ItemManager.FindItemDefinition(item.ShortName).itemid, item.Amount, item.SkinID);
                var main = item.Container == "main" ? player.inventory.containerMain : player.inventory.containerWear;
                var belt = item.Container == "belt" ? player.inventory.containerBelt : main;
                var moved = items.MoveToContainer(belt) || items.MoveToContainer(player.inventory.containerMain);
            }
            DestroyUi(player);

            NoteUI?.Call("DrawInfoNote", player, "Вы получили набор");
            Effect x = new Effect("assets/bundled/prefabs/fx/notice/stack.world.fx.prefab", player, 0, new Vector3(), new Vector3());
            EffectNetwork.Send(x, player.Connection);
        }
    }
}
        #endregion

         #region Интерфейс
        float HeihtMax = 0, HeightMax = 0;

        void CloseUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer1);    
            var container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"  },
                Image = {  Color = "0 0 0 0" },
            }, "Overlay", Layer1);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"  },
                Button = { Color = HexToRustFormat("#82956200"), Command = $"kit ui" },
                Text = { Text = "" }
            }, Layer1);
            
            
            CuiHelper.AddUi(player, container);
        }
        void KitUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            _kitsGUI[player] = new List<string>();
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.2854167 0.9996268", OffsetMax = "0 0"  },
                Image = {  Png = GetImage("fonkits"), Material = "assets/icons/greyout.mat" },
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"  },
                Button = { Color = HexToRustFormat("#82956200"), Command = $"kit ui" },
                Text = { Text = "" }
            }, Layer);

            /*container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5"}
            }, Layer);*/

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.120438 0.8916667", AnchorMax = "0.325 0.9212964" },
                Text = { Text = $"НАБОРЫ", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.UpperLeft, FontSize = 15, Font = "robotocondensed-bold.ttf" }
            }, Layer);
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.120438 0.8188241", AnchorMax = "0.8266424 0.8919995" },
                Text = { Text = $"НЕ ЗАБЫВАЙТЕ ЧТО ПРИ #STORMRUST ВЫ ПОЛУЧИТЕ ДОПОЛЬНИТЕЛЬНЫЙ КИТ!", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.UpperLeft, FontSize = 13, Font = "robotocondensed-regular.ttf" }
            }, Layer);

            CuiHelper.AddUi(player, container);
            KitListUI(player);
            //InfoKitUI(player, config.settings.ElementAt(0).Name);
        }

        void KitListUI(BasePlayer player, int page = 1)
        {
            CuiHelper.DestroyUi(player, "Kits");
            var container = new CuiElementContainer();
            var name = config.settings.Where(z => (string.IsNullOrEmpty(z.Perm) || permission.UserHasPermission(player.UserIDString, z.Perm))).ToList();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1363576 0.07595426", AnchorMax = "0.8649635 0.7994517", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0"}
            }, Layer, "Kits");
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"  },
                Button = { Color = HexToRustFormat("#82956200"), Command = $"kit ui" },
                Text = { Text = "" }
            }, "Kits");
            
            /*
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.7", Command = $"kit ui" },
                Text = { Text = "" }
            }, Layer, "Kits");
            */

            float width = 1f, height = 0.155f, startxBox = 0f, startyBox = 1f - height, xmin = startxBox, ymin = startyBox;
            int x = 0;
            foreach (var check in name.Skip((page - 1) * 6).Take(6))
            {
                x++;
                if (x == 1) {
                    HeihtMax = 0.428f;
                    HeightMax = 0.78f;
                }
                if (x == 2) {
                    HeihtMax = 0.342f;
                    HeightMax = 0.625f;
                } 
                if (x == 3) {
                    HeihtMax = 0.257f;
                    HeightMax = 0.469f;
                }
                if (x == 4) { 
                    HeihtMax = 0.172f;
                    HeightMax = 0.313f;
                }
                if (x == 5) { 
                    HeihtMax = 0.088f;
                    HeightMax = 0.16f;
                }
                if (x == 6) { 
                    HeihtMax = 0;
                    HeightMax = 0;
                }
                var db = GetDataBase(player.userID, check.Name);
                var Time = CurTime();

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Image = { Png = GetImage("nabor"), Material = "assets/icons/greyout.mat" }
                }, "Kits", $"{check.Name}");
                xmin += width;
                if (xmin + width >= 0)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
                
                
                if (db.Cooldown > 0 && (db.Cooldown > Time))
                {
                    
                    container.Add(new CuiElement
                    {
                        Parent = $"{check.Name}",
                        Name = $"Info.{check.Name}",
                        Components =
                        {
                            new CuiImageComponent { Color = "0 0 0 0.7" },
                            new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1", OffsetMin = "0 1", OffsetMax = "0 -1"  }
                        }
                    });
                    
                    container.Add(new CuiElement
                    {
                        Parent = $"{check.Name}",
                        Name = $"Info.{check.Name}",
                        Components =
                        {
                            new CuiImageComponent { Color = HexToRustFormat("#5E373780") },
                            new CuiRectTransformComponent { AnchorMin = $"0.025 0.14", AnchorMax = $"0.24 0.88", OffsetMin = "0 1", OffsetMax = "0 -1"  }
                        }
                    });

                    container.Add(new CuiElement
                    {
                        Parent = $"{check.Name}",
                        Name = $"Info.{check.Name}",
                        Components =
                        {
                            new CuiImageComponent { Png = GetImage(check.Image), Material = "assets/icons/greyout.mat" },
                            new CuiRectTransformComponent { AnchorMin = $"0.045 0.22", AnchorMax = $"0.20 0.81", OffsetMin = "0 1", OffsetMax = "0 -1"  }
                        }
                    });
                    container.Add(new CuiLabel
                    {   
                        RectTransform = { AnchorMin = $"0.25 0", AnchorMax = $"1 1", OffsetMin = "0 1", OffsetMax = "0 -1" },
                        Text = { Text = $"<b><size=14>Набор - {check.DisplayName}</size></b>", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, $"{check.Name}", $"Name.{check.Name}");
                    container.Add(new CuiLabel
                    {   
                        RectTransform = { AnchorMin = $"0.55 0", AnchorMax = $"1 0.34", OffsetMin = "0 1", OffsetMax = "0 -1" },
                        Text = { Text = $"Пожалуйста подождите: {FormatShortTime(TimeSpan.FromSeconds(db.Cooldown - Time))}", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, $"{check.Name}", $"Name.{check.Name}");
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Parent = $"{check.Name}",
                        Name = $"Info.{check.Name}",
                        Components =
                        {
                            new CuiImageComponent { Color = HexToRustFormat("#1D1D1D80") },
                            new CuiRectTransformComponent { AnchorMin = $"0.025 0.14", AnchorMax = $"0.24 0.88", OffsetMin = "0 1", OffsetMax = "0 -1"  }
                        }
                    });
                    
                    container.Add(new CuiElement
                    {
                        Parent = $"{check.Name}",
                        Name = $"Info.{check.Name}",
                        Components =
                        {
                            new CuiImageComponent { Png = GetImage(check.Image), Material = "assets/icons/greyout.mat" },
                            new CuiRectTransformComponent { AnchorMin = $"0.045 0.22", AnchorMax = $"0.20 0.81", OffsetMin = "0 1", OffsetMax = "0 -1"  }
                        }
                    });
                    container.Add(new CuiLabel
                    {   
                        RectTransform = { AnchorMin = $"0.25 0", AnchorMax = $"1 1", OffsetMin = "0 1", OffsetMax = "0 -1" },
                        Text = { Text = $"<b><size=14>Набор - {check.DisplayName}</size></b>", Color = HexToRustFormat("#CAD5DF"), Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, $"{check.Name}", $"Name.{check.Name}");
                    container.Add(new CuiLabel
                    {   
                        RectTransform = { AnchorMin = $"0.55 0", AnchorMax = $"1 0.34", OffsetMin = "0 1", OffsetMax = "0 -1" },
                        Text = { Text = $"Можете забрать", Color = HexToRustFormat("#8E8E8E"), Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, $"{check.Name}", $"Name.{check.Name}");
                    
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = $"0.8171875 0.02592605", AnchorMax = $"0.9833334 0.32", OffsetMin = "0 1", OffsetMax = "0 -1" },
                        Button = { Color = HexToRustFormat("#829562"), Command = $"kit take {check.Name}" },
                        Text = { Text = "ВЗЯТЬ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf"}
                    }, $"{check.Name}", $"Info.{check.Name}");
                }
                /*if (!permission.UserHasPermission(player.UserIDString, check.Perm)) 
                {
                    container.Add(new CuiElement
                    {
                        Parent = $"{check.Name}",
                        Name = $"Info.{check.Name}",
                        Components =
                        {
                            new CuiImageComponent { Color = HexToRustFormat("#1D1D1D") },
                            new CuiRectTransformComponent { AnchorMin = $"0.025 0.14", AnchorMax = $"0.24 0.88", OffsetMin = "0 1", OffsetMax = "0 -1"  }
                        }
                    });
                    
                    container.Add(new CuiElement
                    {
                        Parent = $"{check.Name}",
                        Name = $"Info.{check.Name}",
                        Components =
                        {
                            new CuiImageComponent { Png = GetImage(check.Image), Material = "assets/icons/greyout.mat" },
                            new CuiRectTransformComponent { AnchorMin = $"0.045 0.22", AnchorMax = $"0.20 0.81", OffsetMin = "0 1", OffsetMax = "0 -1"  }
                        }
                    });
                    container.Add(new CuiLabel
                    {   
                        RectTransform = { AnchorMin = $"0.25 0", AnchorMax = $"1 1", OffsetMin = "0 1", OffsetMax = "0 -1" },
                        Text = { Text = $"<b><size=14>Набор - {check.DisplayName}</size></b>", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, $"{check.Name}", $"Name.{check.Name}");
                    container.Add(new CuiLabel
                    {   
                        RectTransform = { AnchorMin = $"0.55 0", AnchorMax = $"1 0.34", OffsetMin = "0 1", OffsetMax = "0 -1" },
                        Text = { Text = $"Вам данный набор недоступен", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                    }, $"{check.Name}", $"Name.{check.Name}");
                }*/
            }

            if (page != 1)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.688 {0 + HeightMax}", AnchorMax = $"0.76 {0.04 + HeightMax}", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat("#829562"), Command = $"kit skip {page - 1}" },
                    Text = { Text = $"▲", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                }, "Kits");
            }

            if (config.settings.ToList().Count > page * 6)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.86 {0 + HeightMax}", AnchorMax = $"0.93 {0.04 + HeightMax}", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat("#829562"), Command = $"kit skip {page + 1}" },
                    Text = { Text = $"▼", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                }, "Kits");
            }

            CuiHelper.AddUi(player, container);
        }

        /*void InfoKitUI(BasePlayer player, string name)
        {
            CuiHelper.DestroyUi(player, "ItemInventory");
            CuiHelper.DestroyUi(player, "Take");
            var container = new CuiElementContainer();
            var check = config.settings.FirstOrDefault(z => z.Name == name);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.47 0", AnchorMax = "0.81 1", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0" }
            }, Layer, "ItemInventory");

            float width1 = 0.15f, height1 = 0.085f, startxBox1 = 0f, startyBox1 = 0.78f - height1, xmin1 = startxBox1, ymin1 = startyBox1;
            foreach (var item in check.Items.Where(z => z.Container == "Рюкзак"))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Image = { Color = "1 1 1 0" }
                }, "ItemInventory", "Item");

                container.Add(new CuiElement
                {
                    Parent = "Item",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.ShortName), Color = "1 1 1 0.8", FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 10", OffsetMax = "-10 -10" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.01", AnchorMax = "0.95 1", OffsetMax = "0 0" },
                    Text = { Text = $"{item.Amount}x", Color = "1 1 1 0.9", Align = TextAnchor.LowerRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, "Item");

                xmin1 += width1;
                if (xmin1 + width1 >= 1)
                {
                    xmin1 = startxBox1;
                    ymin1 -= height1;
                }
            }

            float width2 = 0.15f, height2 = 0.085f, startxBox2 = 0f, startyBox2 = 0.42f - height2, xmin2 = startxBox2, ymin2 = startyBox2;
            foreach (var item in check.Items.Where(z => z.Container == "Одежда"))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin2} {ymin2}", AnchorMax = $"{xmin2 + width2} {ymin2 + height2 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Image = { Color = "1 1 1 0" }
                }, "ItemInventory", "Item");

                container.Add(new CuiElement
                {
                    Parent = "Item",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.ShortName), Color = "1 1 1 0.8", FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 10", OffsetMax = "-10 -10" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.01", AnchorMax = "0.95 1", OffsetMax = "0 0" },
                    Text = { Text = $"{item.Amount}x", Color = "1 1 1 0.9", Align = TextAnchor.LowerRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, "Item");
                xmin2 += width2;
            }

            float width3 = 0.15f, height3 = 0.085f, startxBox3 = 0f, startyBox3 = 0.315f - height3, xmin3 = startxBox3, ymin3 = startyBox3;
            foreach (var item in check.Items.Where(z => z.Container == "Пояс"))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{xmin3} {ymin3}", AnchorMax = $"{xmin3 + width3} {ymin3 + height3 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Image = { Color = "1 1 1 0" }
                }, "ItemInventory", "Item");

                container.Add(new CuiElement
                {
                    Parent = "Item",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.ShortName), Color = "1 1 1 0.8", FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 10", OffsetMax = "-10 -10" }
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.01", AnchorMax = "0.95 1", OffsetMax = "0 0" },
                    Text = { Text = $"{item.Amount}x", Color = "1 1 1 0.9", Align = TextAnchor.LowerRight, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, "Item");
                xmin3 += width3;
            }

            var db = GetDataBase(player.userID, check.Name);
            var Time = CurTime();

            if (db.Cooldown > 0 && (db.Cooldown > Time))
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.2145 {0.232 + HeihtMax}", AnchorMax = $"0.376 {0.265f + HeihtMax}", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat("#b05454") },
                    Text = { Text = $"ПОДОЖДИТЕ: {FormatShortTime(TimeSpan.FromSeconds(db.Cooldown - Time))}", Color = "1 1 1 0.9", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, Layer, "Take");
            }
            else
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.2145 {0.232 + HeihtMax}", AnchorMax = $"0.376 {0.265f + HeihtMax}", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat("#5eb054"), Command = $"kit take {name}" },
                    Text = { Text = "ПОЛУЧИТЬ НАБОР", Color = "1 1 1 0.9", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, Layer, "Take");
            }

            CuiHelper.AddUi(player, container);
        }*/
        #endregion

        #region HexToRustFormat
        private static string HexToRustFormat(string hex)
        {
            UnityEngine.Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion
    }
}