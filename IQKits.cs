using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using System.Text;
using ConVar;

namespace Oxide.Plugins
{
    [Info("IQKits", "Mercury", "0.0.3")]
    [Description("Лучшие наборы из всех,которые есть")]
    class IQKits : RustPlugin
    {
        /// <summary>
        /// Обновление 0.0.2
        /// - Добавил дополнительную проверку на юзера
        /// - Поправил удаление набора
        /// - Исправил шанс выпадения предмета
        /// - Добавлена возможность открывать набор после N дня вайпа
        /// - Добавлена возможность выдавать автоматический набор с учетом N дня вайпа
        /// 
        ///  /// Обновление 0.0.3
        /// - Исправил появление наборов после N дня вайпа
        /// </summary>

        #region Reference
        [PluginReference] Plugin ImageLibrary, IQPlagueSkill, IQChat;

        #region IQChat
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.ReferenceSetting.IQChatSetting;
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region ImageLibrary
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);
        public bool HasImage(string imageName) => (bool)ImageLibrary?.Call("HasImage", imageName);

        #endregion

        #region IQPlagueSkill
        bool IS_SKILL_COOLDOWN(BasePlayer player) => (bool)IQPlagueSkill?.CallHook("API_IS_COOLDOWN_SKILL_KITS", player);
        bool IS_SKILL_RARE(BasePlayer player) => (bool)IQPlagueSkill?.CallHook("API_IS_RARE_SKILL_KITS", player);
        int GET_SKILL_COOLDOWN_PERCENT() => (int)IQPlagueSkill?.CallHook("API_GET_COOLDOWN_IQKITS");
        int GET_SKILL_RARE_PERCENT() => (int)IQPlagueSkill?.CallHook("API_GET_RARE_IQKITS");
        #endregion

        #endregion

        #region Vars 
        public static DateTime TimeCreatedSave = SaveRestore.SaveCreatedTime.Date;
        public static DateTime RealTime = DateTime.Now.Date;
        public static int WipeTime = RealTime.Subtract(TimeCreatedSave).Days;

        enum TypeKits
        {
            Cooldown,
            Amount,
            Started,
            AmountCooldown,
        }
        enum ContainerItem
        {
            containerWear,
            containerBelt,
            containerMain
        }
        public Dictionary<BasePlayer, int> PagePlayers = new Dictionary<BasePlayer, int>();
        #endregion

        #region Configuration
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Общие настройки")]
            public GeneralSettings GeneralSetting = new GeneralSettings();
            [JsonProperty("Настройки интерфейса")]
            public InterfaceSettings InterfaceSetting = new InterfaceSettings();
            [JsonProperty("Настройка наборов")]
            public Dictionary<string, Kits> KitList = new Dictionary<string, Kits>();
            [JsonProperty("Настройки плагинов совместимости")]
            public ReferenceSettings ReferenceSetting = new ReferenceSettings();
            internal class ReferenceSettings
            {
                [JsonProperty("Настройки IQChat")]
                public IQChatSettings IQChatSetting = new IQChatSettings();
                internal class IQChatSettings
                {
                    [JsonProperty("IQChat : Кастомный префикс в чате")]
                    public string CustomPrefix;
                    [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
                    public string CustomAvatar;
                }
            }
            internal class GeneralSettings
            {
                [JsonProperty("Ключ стартового набора(Дается при возрождении)(если включен список нескольких наборов, данная функция отключается)")]
                public string StartKitKey;
                [JsonProperty("Использовать сразу несколько стартовых наборов(они будут выбираться случайно)")]
                public bool UseStartedKitList;
                [JsonProperty("Список ключей стартового набора(Дается при возрождении)")]
                public List<KitRandom> StartedKitList = new List<KitRandom>();
                internal class KitRandom
                {
                    [JsonProperty("Ключ набора")]
                    public string StartKitKey;
                    [JsonProperty("Права для набора(не оставляйте это поле пустым)")]
                    public string Permissions;
                }
            }
            internal class InterfaceSettings
            {
                [JsonProperty("HEX: Цвет заднего фона")]
                public string HEXBackground;
                [JsonProperty("HEX: Цвет текста")]
                public string HEXLabels;
                [JsonProperty("HEX: Кнопки с информацией")]
                public string HEXInfoItemButton;
                [JsonProperty("HEX: Цвет текста на кнопке с информацией")]
                public string HEXLabelsInfoItemButton;
                [JsonProperty("HEX: Цвет кнопки забрать")]
                public string HEXAccesButton;
                [JsonProperty("HEX: Цвет текста на кнопке забрать")]
                public string HEXLabelsAccesButton;
                [JsonProperty("HEX: Цвет полосы перезарядки")]
                public string HEXCooldowns;
                [JsonProperty("HEX: Цвет блоков с информацией")]
                public string HEXBlock;
                [JsonProperty("HEX: Цвет блоков на которых будут лежать предметы")]
                public string HEXBlockItemInfo;
                [JsonProperty("Время появления интерфейса(его плавность)")]
                public float InterfaceFadeOut;
                [JsonProperty("Время исчезновения интерфейса(его плавность)")]
                public float InterfaceFadeIn;
                [JsonProperty("PNG заднего фона с информацией о том,что находится в наборе")]
                public string PNGInfoPanel;
                [JsonProperty("PNG заднего фона уведомления")]
                public string PNGAlert;
            }
            internal class Kits
            {
                [JsonProperty("Тип набора(0 - С перезарядкой, 1 - Лимитированый, 2 - Стартовый(АвтоКит), 3 - Лимитированый с перезарядкой)")]
                public TypeKits TypeKit;
                [JsonProperty("Отображаемое имя")]
                public string DisplayName;
                [JsonProperty("Через сколько дней вайпа будет доступен набор")]
                public int WipeOpened;
                [JsonProperty("Права")]
                public string Permission;
                [JsonProperty("PNG(128x128)")]
                public string PNG;
                [JsonProperty("Sprite(Установится если отсутствует PNG)")]
                public string Sprite;
                [JsonProperty("Shortname(Установится если отсутствует PNG и Sprite)")]
                public string Shortname;
                [JsonProperty("Время перезарядки набора")]
                public int CoolDown;
                [JsonProperty("Количество сколько наборов можно взять")]
                public int Amount;
                [JsonProperty("Предметы , которые будут даваться в данном наборе")]
                public List<ItemsKit> ItemKits = new List<ItemsKit>();

                internal class ItemsKit
                {
                    [JsonProperty("Выберите контейнер в который будет перенесен предмет(0 - Одежда, 1 - Панель быстрого доступа, 2 - Рюкзак)")]
                    public ContainerItem ContainerItemType;
                    [JsonProperty("Название предмета")]
                    public string DisplayName;
                    [JsonProperty("Shortname предмета")]
                    public string Shortname;
                    [JsonProperty("Количество(Если это команда,так-же указывайте число)")]
                    public int Amount;
                    [JsonProperty("Шанс на выпадения предмета(Оставьте 0 - если не нужен шанс)")]
                    public int Rare;
                    [JsonProperty("SkinID предмета")]
                    public ulong SkinID;
                    [JsonProperty("PNG предмета(если установлена команда)")]
                    public string PNG;
                    [JsonProperty("Sprite(Если установлена команда и не установлен PNG)")]
                    public string Sprite;
                    [JsonProperty("Команда(%STEAMID% заменится на ID пользователя)")]
                    public string Command;
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    ReferenceSetting = new ReferenceSettings
                    {
                        IQChatSetting = new ReferenceSettings.IQChatSettings
                        {
                            CustomAvatar = "",
                            CustomPrefix = "",
                        }
                    },
                    GeneralSetting = new GeneralSettings
                    {
                        UseStartedKitList = false,
                       
                        StartedKitList = new List<GeneralSettings.KitRandom>
                        {
                            new GeneralSettings.KitRandom
                            {
                                Permissions = "iqkits.default",
                                StartKitKey = "start1"
                            },
                            new GeneralSettings.KitRandom
                            {
                                Permissions = "iqkits.vip",
                                StartKitKey = "hunter"
                            },
                        },
                        StartKitKey = "start1",
                    },
                    InterfaceSetting = new InterfaceSettings
                    {
                        HEXBackground = "#0000006A",
                        HEXBlock = "#646361A6",
                        HEXAccesButton = "#708a47",
                        HEXBlockItemInfo = "#3D492837",
                        HEXInfoItemButton = "#8a6347",
                        HEXCooldowns = "#708A47D8",
                        HEXLabels = "#FFFFFFFF",
                        HEXLabelsAccesButton = "#C9E39FFF",
                        HEXLabelsInfoItemButton = "#C9E39FFF",
                        InterfaceFadeIn = 0.35f,
                        InterfaceFadeOut = 0.35f,
                        PNGAlert = "https://i.imgur.com/g4Mzn9a.png",
                        PNGInfoPanel = "https://i.imgur.com/9kbOqHK.png",
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
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения #57 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Data
        [JsonProperty("Дата с информацией о игроках")]
        public Dictionary<ulong, DataKitsUser> DataKitsUserList = new Dictionary<ulong, DataKitsUser>();

        public class DataKitsUser
        {
            [JsonProperty("Информация о наборах игрока")]
            public Dictionary<string, InfoKits> InfoKitsList = new Dictionary<string, InfoKits>();
            internal class InfoKits
            {
                public int Amount;
                public int Cooldown;
            }
        }  
        void ReadData() => DataKitsUserList = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, DataKitsUser>>("IQKits/KitsData");
        void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQKits/KitsData", DataKitsUserList);
        void RegisteredDataUser(BasePlayer player)
        {
            if (!DataKitsUserList.ContainsKey(player.userID))
                DataKitsUserList.Add(player.userID, new DataKitsUser
                {
                    InfoKitsList = new Dictionary<string, DataKitsUser.InfoKits> { }
                });

            foreach(var Kit in config.KitList.Where(x => !DataKitsUserList[player.userID].InfoKitsList.ContainsKey(x.Key)))
                DataKitsUserList[player.userID].InfoKitsList.Add(Kit.Key, new DataKitsUser.InfoKits { Amount = Kit.Value.Amount, Cooldown = 0 });
            
        }

        #endregion

        #region Metods 

        #region Images
        void LoadedImage()
        {
            var Interface = config.InterfaceSetting;
            foreach (var Kit in config.KitList.Where(k => !String.IsNullOrWhiteSpace(k.Value.PNG)))
            {
                if (!HasImage($"KIT_{Kit.Value.PNG}"))
                    AddImage(Kit.Value.PNG, $"KIT_{Kit.Value.PNG}");

                foreach (var img in Kit.Value.ItemKits.Where(i => !String.IsNullOrWhiteSpace(i.PNG)))
                {
                    if (!HasImage($"ITEM_KIT_PNG_{img.PNG}"))
                        AddImage(img.PNG, $"ITEM_KIT_PNG_{img.PNG}");
                }
            }
            if (!HasImage($"INFO_BACKGROUND_{Interface.PNGInfoPanel}"))
                AddImage(Interface.PNGInfoPanel, $"INFO_BACKGROUND_{Interface.PNGInfoPanel}");

            if (!HasImage($"INFO_ALERT_BACKGROUND_{Interface.PNGAlert}"))
                AddImage(Interface.PNGAlert, $"INFO_ALERT_BACKGROUND_{Interface.PNGAlert}");

            ServerMgr.Instance.StartCoroutine(DownloadImages());
        }
        private IEnumerator DownloadImages()
        {
            PrintError("AddImages SkyPlugins.ru...");
            foreach (var Kit in config.KitList)
                foreach (var img in Kit.Value.ItemKits.Where(i => !String.IsNullOrWhiteSpace(i.Shortname)))
                {
                    if (!HasImage($"{img.Shortname}_128px"))
                        AddImage($"http://rust.skyplugins.ru/getimage/{img.Shortname}/128", $"{img.Shortname}_128px");
                }
            yield return new WaitForSeconds(0.04f);
            PrintError("AddImages SkyPlugins.ru - completed..");
        }
        void CachingImage(BasePlayer player)
        {
            var Interface = config.InterfaceSetting;
            foreach (var Kit in config.KitList.Where(k => !String.IsNullOrWhiteSpace(k.Value.PNG)))
            {
                SendImage(player, $"KIT_{Kit.Value.PNG}");

                foreach (var ItemKit in Kit.Value.ItemKits.Where(ik => !String.IsNullOrWhiteSpace(ik.Shortname)))
                    SendImage(player, $"{ItemKit.Shortname}_128px");

                foreach (var img in Kit.Value.ItemKits.Where(i => !String.IsNullOrWhiteSpace(i.PNG)))
                    if (!HasImage($"ITEM_KIT_PNG_{img.PNG}"))
                        AddImage(img.PNG, $"ITEM_KIT_PNG_{img.PNG}");
                
            }
            SendImage(player, $"INFO_BACKGROUND_{Interface.PNGInfoPanel}");
            SendImage(player, $"INFO_ALERT_BACKGROUND_{Interface.PNGAlert}");
        }
        #endregion

        #region Registered Permissions
        void RegisteredPermissions()
        {
            var GeneralSettings = config.GeneralSetting;
            var KitList = config.KitList;

            foreach(var PermissionGeneral in GeneralSettings.StartedKitList)
                if (!permission.PermissionExists(PermissionGeneral.Permissions, this))
                    permission.RegisterPermission(PermissionGeneral.Permissions, this);

            foreach (var PermissionKits in KitList)
                if (!permission.PermissionExists(PermissionKits.Value.Permission, this))
                    permission.RegisterPermission(PermissionKits.Value.Permission, this);
        }
        #endregion

        #region AutoKit
        void AutoKitGive(BasePlayer player)
        {
            if (player == null) return;
            var GeneralSettings = config.GeneralSetting;
            var KitList = config.KitList;

            if (GeneralSettings.UseStartedKitList)
            {
                List<Configuration.GeneralSettings.KitRandom> RandomingKit = new List<Configuration.GeneralSettings.KitRandom>();
                foreach (var StartedKitList in GeneralSettings.StartedKitList.Where(k => permission.UserHasPermission(player.UserIDString, k.Permissions) && KitList.ContainsKey(k.StartKitKey) && WipeTime >= KitList[k.StartKitKey].WipeOpened))
                    RandomingKit.Add(StartedKitList);
                
                var RandomKit = RandomingKit.GetRandom();
                if (RandomKit == null) return;
                ParseAndGive(player, RandomKit.StartKitKey);
            }
            else
            {
                if (!KitList.ContainsKey(GeneralSettings.StartKitKey))
                {
                    PrintWarning("У вас не верно указан стартовый ключ, такого набора не существует! Игрок не получил его автоматически");
                    return;
                }
                ParseAndGive(player, GeneralSettings.StartKitKey);
            }
        }
        #endregion

        #region TakeKit
        void TakeKit(BasePlayer player, string KitKey)
        {
            var Kit = config.KitList[KitKey];
            var ItemKit = Kit.ItemKits;
            var Data = DataKitsUserList[player.userID].InfoKitsList[KitKey];

            if (Data.Cooldown >= CurrentTime()) return;

            int BeltAmount = ItemKit.Where(i => i.ContainerItemType == ContainerItem.containerBelt).Count();
            int WearAmount = ItemKit.Where(i => i.ContainerItemType == ContainerItem.containerWear).Count();
            int MainAmount = ItemKit.Where(i => i.ContainerItemType == ContainerItem.containerMain).Count();
            int Total = BeltAmount + WearAmount + MainAmount;
            if ((player.inventory.containerBelt.capacity - player.inventory.containerBelt.itemList.Count) < BeltAmount 
            || (player.inventory.containerWear.capacity - player.inventory.containerWear.itemList.Count) < WearAmount 
            || (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count) < MainAmount)
                if (Total > (player.inventory.containerMain.capacity - player.inventory.containerMain.itemList.Count))
                {
                    Interface_Alert_Kits(player, GetLang("UI_ALERT_FULL_INVENTORY", player.UserIDString));
                    return;
                }

            switch (Kit.TypeKit)
            {
                case TypeKits.Cooldown:
                    {
                        int Cooldown = IQPlagueSkill ? IS_SKILL_COOLDOWN(player) ? (Kit.CoolDown - (Kit.CoolDown / 100 * GET_SKILL_COOLDOWN_PERCENT())) : Kit.CoolDown : Kit.CoolDown;
                        Data.Cooldown = Convert.ToInt32(CurrentTime() + Cooldown);
                        break;
                    }
                case TypeKits.Amount:
                    {
                        Data.Amount--;
                        break;
                    }
                case TypeKits.AmountCooldown:
                    {
                        int Cooldown = IQPlagueSkill ? IS_SKILL_COOLDOWN(player) ? (Kit.CoolDown - (Kit.CoolDown / 100 * GET_SKILL_COOLDOWN_PERCENT())) : Kit.CoolDown : Kit.CoolDown;
                        Data.Amount--;
                        Data.Cooldown = Convert.ToInt32(CurrentTime() + Cooldown); break;
                    }
            }
            ParseAndGive(player, KitKey);
            DestroyKits(player);
            Interface_Loaded_Kits(player);
            Interface_Alert_Kits(player, GetLang("UI_ALERT_ACCES_KIT", player.UserIDString, Kit.DisplayName));
        }
        void ParseAndGive(BasePlayer player, string KitKey)
        {
            var Kit = config.KitList[KitKey];
            var ItemKit = Kit.ItemKits;
            foreach (var Item in ItemKit)
            {
                if (Item.Rare != 0)
                {
                    int Rare = IQPlagueSkill ? IS_SKILL_RARE(player) ? Item.Rare + GET_SKILL_RARE_PERCENT() : Item.Rare : Item.Rare;
                    if (!IsRareDrop(Rare)) continue;
                }

                if (!String.IsNullOrWhiteSpace(Item.Command))
                    rust.RunServerCommand(Item.Command.Replace("%STEAMID%", player.UserIDString));
                else
                {
                    Item item = ItemManager.CreateByName(Item.Shortname, Item.Amount > 1 ? Item.Amount : 1, Item.SkinID);
                    if (!String.IsNullOrWhiteSpace(Item.DisplayName))
                        item.name = Item.DisplayName;
                    GiveItem(player, item, Item.ContainerItemType == ContainerItem.containerBelt ? player.inventory.containerBelt : Item.ContainerItemType == ContainerItem.containerWear ? player.inventory.containerWear : player.inventory.containerMain);
                }
            }
        }
        private void GiveItem(BasePlayer player, Item item, ItemContainer cont = null)
        {
            if (item == null) return;
            var inv = player.inventory;

            var MovedContainer = item.MoveToContainer(cont) || item.MoveToContainer(inv.containerMain);
            if (!MovedContainer)
            {
                if (cont == inv.containerBelt)
                    MovedContainer = item.MoveToContainer(inv.containerWear);
                if (cont == inv.containerWear)
                    MovedContainer = item.MoveToContainer(inv.containerBelt);
            }

            if (!MovedContainer)
                item.Drop(player.GetCenter(), player.GetDropVelocity());
        }
        #endregion

        #region Kit Metods Admin

        #region Kit Add
        void CreateNewKit(BasePlayer player, string NameKit)
        {
            if (!player.IsAdmin) return;

            if (config.KitList.ContainsKey(NameKit))
            {
                SendChat(player, "Ключ данного набора уже существует!");
                return;
            }

            config.KitList.Add(NameKit, new Configuration.Kits
            {
                Amount = 0,
                CoolDown = 300,
                DisplayName = NameKit,
                Permission = "iqkits.setting",
                PNG = "",
                Shortname = "",
                Sprite = "assets/icons/gear.png",
                TypeKit = TypeKits.Cooldown,
                ItemKits = GetPlayerItems(player)
            });

            SaveConfig();
            SendChat(player, $"Набор с ключем {NameKit} успешно создан");
        }

        private List<Configuration.Kits.ItemsKit> GetPlayerItems(BasePlayer player)
        {
            List<Configuration.Kits.ItemsKit> kititems = new List<Configuration.Kits.ItemsKit>();
            foreach (Item item in player.inventory.containerWear.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, ContainerItem.containerWear);
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerMain.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, ContainerItem.containerMain);
                    kititems.Add(iteminfo);
                }
            }
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item != null)
                {
                    var iteminfo = ItemToKit(item, ContainerItem.containerBelt);
                    kititems.Add(iteminfo);
                }
            }
            return kititems;
        }
        private Configuration.Kits.ItemsKit ItemToKit(Item item, ContainerItem containerItem)
        {
            Configuration.Kits.ItemsKit ItemsKit = new Configuration.Kits.ItemsKit();

            ItemsKit.Amount = item.amount;
            ItemsKit.ContainerItemType = containerItem;
            ItemsKit.Shortname = item.info.shortname;
            ItemsKit.SkinID = item.skin;
            ItemsKit.Rare = 0;
            ItemsKit.PNG = "";
            ItemsKit.Sprite = "";
            ItemsKit.Command = "";
            ItemsKit.DisplayName = "";

            return ItemsKit;
        }
        #endregion

        #region Kit Remove
        void KitRemove(BasePlayer player, string NameKit)
        {
            if (!player.IsAdmin) return;

            if (!config.KitList.ContainsKey(NameKit))
            {
                SendChat(player, "Набора с таким ключем не существует!");
                return;
            }

            config.KitList.Remove(NameKit);
            SaveConfig();
            SendChat(player, $"Набора с ключем {NameKit} успешно удален");
        }

        #endregion

        #endregion
        public bool IsRareDrop(int Rare) => UnityEngine.Random.Range(0, 100) >= (100 - (Rare > 100 ? 100 : Rare));
        #endregion

        #region Hooks
        object OnPlayerRespawned(BasePlayer player)
        {
            player.inventory.Strip();
            AutoKitGive(player);
            return null;
        }
        private void OnServerInitialized()
        {
            RegisteredPermissions();
            ReadData();
            LoadedImage();

            foreach (BasePlayer p in BasePlayer.activePlayerList)
                OnPlayerConnected(p);

            WriteData();
        }
        void OnPlayerConnected(BasePlayer player)
        {
            CachingImage(player);
            RegisteredDataUser(player);
        }
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                DestroyAll(player);

            ServerMgr.Instance.StopCoroutine(DownloadImages());

            WriteData();
        }
        #endregion

        #region Commands
        [ChatCommand("kit")]
        void IQKITS_ChatCommand(BasePlayer player, string cmd, string[] arg)
        {
            if (arg.Length < 2 || arg == null || arg == null)
            {
                PagePlayers[player] = 0;
                Interface_IQ_Kits(player);
                return;
            }

            switch (arg[0])
            {
                case "create":
                case "createkit":
                case "add":
                case "new":
                    {
                        string NameKit = arg[1];
                        if(string.IsNullOrWhiteSpace(NameKit))
                        {
                            SendChat(player, "Введите корректное название!");
                            return;
                        }
                        CreateNewKit(player, NameKit);
                        break;
                    }
                case "remove":
                case "delete":
                case "revoke":
                    {
                        string NameKit = arg[1];
                        if (string.IsNullOrWhiteSpace(NameKit))
                        {
                            SendChat(player, "Введите корректное название!");
                            return;
                        }
                        KitRemove(player, NameKit);
                        break;
                    }
            }
        }
        [ConsoleCommand("kit_ui_func")]
        void IQKITS_UI_Func(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if(player == null)
            {
                PrintWarning("Это консольная команда должна отыгрываться от игрока!");
                return;
            }
            string Key = arg.Args[0];
            switch(Key)
            {
                case "information": 
                    {
                        string KitKey = arg.Args[1];
                        Interface_Info_Kits(player, KitKey);
                        break;
                    }
                case "take.kit": 
                    {
                        string KitKey = arg.Args[1];
                        TakeKit(player, KitKey);
                        break;
                    }
                case "close.ui":
                    {
                        DestroyAll(player);
                        break;
                    }
                case "hide.info": 
                    {
                        DestroyInfoKits(player);
                        break;
                    }
                case "next.page": 
                    {
                        DestroyKits(player);
                        PagePlayers[player]++;
                        Interface_Loaded_Kits(player);
                        break;
                    }
                case "back.page":
                    {
                        DestroyKits(player);
                        PagePlayers[player]--;
                        Interface_Loaded_Kits(player);
                        break;
                    }
            }
            
        }
        #endregion

        #region Interface
        void DestroyAll(BasePlayer player)
        {
            DestroyAlert(player);
            DestroyInfoKits(player);
            DestroyKits(player);
            CuiHelper.DestroyUi(player, "CLOSE_BTN");
            CuiHelper.DestroyUi(player, "DESCRIPTION");
            CuiHelper.DestroyUi(player, "TITLE");
            CuiHelper.DestroyUi(player, IQKITS_OVERLAY);
            player.SetFlag(BaseEntity.Flags.Reserved3, false);
            ServerMgr.Instance.StopCoroutine(UI_UpdateCooldown(player));
        }
        void DestroyKits(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, $"BTN_BACK_BUTTON");
            CuiHelper.DestroyUi(player, $"BTN_NEXT_BUTTON");

            for (int i = 0; i < 4; i++)
            {
                CuiHelper.DestroyUi(player, $"WHAT_INFO_{i}");
                CuiHelper.DestroyUi(player, $"TAKE_KIT_{i}");
                CuiHelper.DestroyUi(player, $"COOLDOWN_LINE_{i}");
                CuiHelper.DestroyUi(player, $"COOLDOWN_TITLE{i}");
                CuiHelper.DestroyUi(player, $"COOLDOWN_PANEL_{i}");
                CuiHelper.DestroyUi(player, $"TITLE_KIT_{i}");
                CuiHelper.DestroyUi(player, $"DISPLAY_NAME_PANEL_{i}");
                CuiHelper.DestroyUi(player, $"AVATAR_{i}");
                CuiHelper.DestroyUi(player, $"AVATAR_PANEL_{i}");
                CuiHelper.DestroyUi(player, $"KIT_PANEL_{i}");
            }
        }
        void DestroyAlert(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, $"TITLE_ALERT");
            CuiHelper.DestroyUi(player, $"INFO_ALERT_BACKGROUND");
        }
        void DestroyInfoKits(BasePlayer player)
        {
            for (int i = 0; i < 40; i++)
            {
                CuiHelper.DestroyUi(player, $"KIT_ITEM_AMOUNT_{i}");
                CuiHelper.DestroyUi(player, $"RARE_LABEL_{i}");
                CuiHelper.DestroyUi(player, $"RARE_BACKGROUND_{i}");
                CuiHelper.DestroyUi(player, $"KIT_ITEM_{i}");
                CuiHelper.DestroyUi(player, $"ITEM_{i}");
            }
            CuiHelper.DestroyUi(player, $"TITLE_KIT_INFO");
            CuiHelper.DestroyUi(player, $"HIDE_INFO_BTN");
            CuiHelper.DestroyUi(player, $"INFO_BACKGROUND");
        }

        public static string IQKITS_OVERLAY = "IQKITS_OVERLAY";
        void Interface_IQ_Kits(BasePlayer player)
        {
            DestroyAll(player);
            player.SetFlag(BaseEntity.Flags.Reserved3, true);
            CuiElementContainer container = new CuiElementContainer();
            var Interface = config.InterfaceSetting;
            float FadeIn = Interface.InterfaceFadeIn;
            float FadeOut = Interface.InterfaceFadeOut;

            container.Add(new CuiPanel
            {
                FadeOut = FadeOut,
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = FadeIn, Color = HexToRustFormat(Interface.HEXBackground), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "Overlay", IQKITS_OVERLAY);

            container.Add(new CuiLabel
            {
                FadeOut = FadeOut,
                RectTransform = { AnchorMin = "0 0.915", AnchorMax = "1 1" },
                Text = { FadeIn = FadeIn, Text = GetLang("UI_TITLE", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  IQKITS_OVERLAY, "TITLE");

            container.Add(new CuiLabel
            {
                FadeOut = FadeOut,
                RectTransform = { AnchorMin = "0 0.8925912", AnchorMax = "1 0.9351871" },
                Text = { FadeIn = FadeIn, Text = GetLang("UI_DESCRIPTION", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, IQKITS_OVERLAY, "DESCRIPTION");

            container.Add(new CuiButton
            {
                FadeOut = FadeOut - 0.2f,
                RectTransform = { AnchorMin = "0.8718751568 0.9388889", AnchorMax = "1 1" },
                Button = { FadeIn = FadeIn, Command = $"kit_ui_func close.ui", Color = "0 0 0 0" },
                Text = { FadeIn = FadeIn, Text = GetLang("UI_CLOSE_BTN", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabels), Align = TextAnchor.MiddleCenter }
            }, IQKITS_OVERLAY, "CLOSE_BTN");


            CuiHelper.AddUi(player, container);
            Interface_Loaded_Kits(player);
            ServerMgr.Instance.StartCoroutine(UI_UpdateCooldown(player));
        }

        #region LoadedKits
        void Interface_Loaded_Kits(BasePlayer player)
        {
            if(!DataKitsUserList.ContainsKey(player.userID))
                RegisteredDataUser(player);

            CuiElementContainer container = new CuiElementContainer();
            var Interface = config.InterfaceSetting;
            float FadeIn = Interface.InterfaceFadeIn;
            float FadeOut = Interface.InterfaceFadeOut;
            int CountKitPage = config.KitList.Where(k => (k.Value.TypeKit != TypeKits.Started && WipeTime >= k.Value.WipeOpened) && (((k.Value.TypeKit == TypeKits.Amount || k.Value.TypeKit == TypeKits.AmountCooldown) && DataKitsUserList[player.userID].InfoKitsList[k.Key].Amount > 0) || k.Value.TypeKit == TypeKits.Cooldown && permission.UserHasPermission(player.UserIDString, k.Value.Permission))).Skip(4 * (PagePlayers[player] + 1)).Take(4).Count();

            int x = 0, y = 0, i = 0;
            foreach (var Kit in config.KitList.Where(k => (k.Value.TypeKit != TypeKits.Started && WipeTime >= k.Value.WipeOpened) && (((k.Value.TypeKit == TypeKits.Amount || k.Value.TypeKit == TypeKits.AmountCooldown) && DataKitsUserList[player.userID].InfoKitsList[k.Key].Amount > 0) || k.Value.TypeKit == TypeKits.Cooldown && permission.UserHasPermission(player.UserIDString, k.Value.Permission))).Skip(4 * PagePlayers[player]).Take(4))
            {
                var Data = DataKitsUserList[player.userID].InfoKitsList[Kit.Key];

                container.Add(new CuiPanel
                {
                    FadeOut = FadeOut,
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = $"{0.08385417 + (x * 0.52)} {0.5842593 - (y * 0.342)}", AnchorMax = $"{0.3916667 + (x * 0.52)} {0.8231534 - (y * 0.342)}" },
                    Image = { FadeIn = FadeIn, Color = "0 0 0 0" }
                }, IQKITS_OVERLAY, $"KIT_PANEL_{i}");

                #region Avatar
                container.Add(new CuiPanel
                {
                    FadeOut = FadeOut,
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.43654821568 1" },
                    Image = { FadeIn = FadeIn, Color = HexToRustFormat(Interface.HEXBlock) }
                }, $"KIT_PANEL_{i}", $"AVATAR_PANEL_{i}");

                if (String.IsNullOrWhiteSpace(Kit.Value.Sprite))
                {
                    var ComponentAvatar = !String.IsNullOrWhiteSpace(Kit.Value.PNG) ? new CuiRawImageComponent { FadeIn = FadeIn, Png = GetImage($"KIT_{Kit.Value.PNG}") } : new CuiRawImageComponent { FadeIn = FadeIn, Png = GetImage(Kit.Value.Shortname) };
                    container.Add(new CuiElement
                    {
                        FadeOut = FadeOut,
                        Parent = $"AVATAR_PANEL_{i}",
                        Name = $"AVATAR_{i}",
                        Components =
                    {
                        ComponentAvatar,
                        new CuiRectTransformComponent{ AnchorMin = "0.0775194 0.07364181", AnchorMax = $"0.9224806 0.9185845"},
                    }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        FadeOut = FadeOut,
                        Parent = $"AVATAR_PANEL_{i}",
                        Name = $"AVATAR_{i}",
                        Components =
                    {
                        new CuiImageComponent { FadeIn = FadeIn, Sprite = Kit.Value.Sprite },
                        new CuiRectTransformComponent{ AnchorMin = "0.0775194 0.07364181", AnchorMax = $"0.9224806 0.9185845"},
                    }
                    });
                }
                #endregion

                #region Name

                container.Add(new CuiPanel
                {
                    FadeOut = FadeOut,
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = $"0.4602368 0.6472726", AnchorMax = $"1 1" },
                    Image = { FadeIn = FadeIn, Color = HexToRustFormat(Interface.HEXBlock) }
                }, $"KIT_PANEL_{i}", $"DISPLAY_NAME_PANEL_{i}");

                container.Add(new CuiLabel
                {
                    FadeOut = FadeOut,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.965517 0.9449963" },
                    Text = { FadeIn = FadeIn, Text = GetLang("UI_DISPLAY_NAME_KIT", player.UserIDString, Kit.Value.DisplayName.ToUpper()), Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }

                }, $"DISPLAY_NAME_PANEL_{i}", $"TITLE_KIT_{i}");
                #endregion

                #region Cooldown
                container.Add(new CuiPanel
                {
                    FadeOut = FadeOut,
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = $"0.4602368 0.2519316", AnchorMax = $"1 0.6046609" },
                    Image = { FadeIn = FadeIn, Color = HexToRustFormat(Interface.HEXBlock) }
                }, $"KIT_PANEL_{i}", $"COOLDOWN_PANEL_{i}");

                double XMax = Data.Cooldown >= CurrentTime() ? (double)((Data.Cooldown - CurrentTime()) * Math.Pow(Kit.Value.CoolDown, -1)) : Data.Amount != 0 ? (double)((Data.Amount) * Math.Pow(Kit.Value.Amount, -1)) : 1;
                container.Add(new CuiPanel
                {
                    FadeOut = FadeOut,
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"{XMax} 1", OffsetMin = "1 1", OffsetMax = "-2 -1" },
                    Image = { FadeIn = FadeIn, Color = HexToRustFormat(Interface.HEXCooldowns) }
                }, $"COOLDOWN_PANEL_{i}", $"COOLDOWN_LINE_{i}");

                string InfoAmountAndCooldown = Data.Cooldown >= CurrentTime() ? GetLang("UI_COOLDONW_KIT", player.UserIDString, FormatTime(TimeSpan.FromSeconds(Data.Cooldown - CurrentTime()))) : Data.Amount != 0 ? GetLang("UI_AMOUNT_KIT", player.UserIDString, Data.Amount) : GetLang("UI_COOLDONW_KIT_NO", player.UserIDString);
                container.Add(new CuiLabel
                {
                    FadeOut = FadeOut,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.965517 0.9449963" },
                    Text = { FadeIn = FadeIn, Text = InfoAmountAndCooldown, Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
                }, $"COOLDOWN_PANEL_{i}", $"COOLDOWN_TITLE{i}");
                #endregion

                #region Button

                container.Add(new CuiButton
                {
                    FadeOut = FadeOut - 0.2f,
                    RectTransform = { AnchorMin = "0.4653131568 0", AnchorMax = "0.7258883 0.2170733" },
                    Button = { FadeIn = FadeIn, Command = $"kit_ui_func information {Kit.Key}", Color = HexToRustFormat(Interface.HEXInfoItemButton), },
                    Text = { FadeIn = FadeIn, Text = GetLang("UI_BTN_WHAT_INFO", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabelsInfoItemButton), Align = TextAnchor.MiddleCenter }
                }, $"KIT_PANEL_{i}", $"WHAT_INFO_{i}");

                string KeyLangTake = Data.Cooldown >= CurrentTime() ? GetLang("UI_BTN_TAKE_KIT_BLOCK", player.UserIDString) : Data.Amount != 0 ? GetLang("UI_BTN_TAKE_KIT", player.UserIDString, Data.Amount) : GetLang("UI_BTN_TAKE_KIT", player.UserIDString);
                string HexButtonTake = Data.Cooldown >= CurrentTime() ? Interface.HEXInfoItemButton : Data.Amount != 0 ? Interface.HEXAccesButton : Interface.HEXAccesButton;
                string HexButtonLabelTake = Data.Cooldown >= CurrentTime() ? Interface.HEXLabelsInfoItemButton : Data.Amount != 0 ? Interface.HEXLabelsAccesButton : Interface.HEXLabelsAccesButton;
                string CommandButtonTake = Data.Cooldown >= CurrentTime() ? "" : Data.Amount != 0 ? $"kit_ui_func take.kit {Kit.Key}" : $"kit_ui_func take.kit {Kit.Key}";
                container.Add(new CuiButton
                {
                    FadeOut = FadeOut - 0.2f,
                    RectTransform = { AnchorMin = "0.7394261 0", AnchorMax = "1 0.2170733" },
                    Button = { FadeIn = FadeIn, Command = CommandButtonTake, Color = HexToRustFormat(HexButtonTake) },
                    Text = { FadeIn = FadeIn, Text = KeyLangTake, Color = HexToRustFormat(HexButtonLabelTake), Align = TextAnchor.MiddleCenter }
                }, $"KIT_PANEL_{i}", $"TAKE_KIT_{i}");

                #endregion

                x++;
                if (x >= 2)
                {
                    x = 0;
                    y++;
                }
                i++;
                if (x == 2 && y == 1) break;
            }

            if (PagePlayers[player] != 0)
            {
                container.Add(new CuiButton
                {
                    FadeOut = FadeOut - 0.2f,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.1015625 0.05462963" },
                    Button = { FadeIn = FadeIn, Command = "kit_ui_func back.page", Color = "0 0 0 0" },
                    Text = { FadeIn = FadeIn, Text = GetLang("UI_BACK_BTN", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabels), Align = TextAnchor.MiddleCenter }
                },  IQKITS_OVERLAY, $"BTN_BACK_BUTTON");
            }
            if(CountKitPage != 0)
            {
                container.Add(new CuiButton
                {
                    FadeOut = FadeOut - 0.2f,
                    RectTransform = { AnchorMin = "0.89895 0", AnchorMax = "1 0.05462963" },
                    Button = { FadeIn = FadeIn, Command = $"kit_ui_func next.page", Color = "0 0 0 0" },
                    Text = { FadeIn = FadeIn, Text = GetLang("UI_NEXT_BTN", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabels), Align = TextAnchor.MiddleCenter }
                }, IQKITS_OVERLAY, $"BTN_NEXT_BUTTON");
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Update Cooldown
        private IEnumerator UI_UpdateCooldown(BasePlayer player)
        {
            var Interface = config.InterfaceSetting;

            while (player.HasFlag(BaseEntity.Flags.Reserved3))
            {
                int i = 0;
                foreach (var Kit in config.KitList.Where(k => k.Value.TypeKit != TypeKits.Started && (((k.Value.TypeKit == TypeKits.Amount || k.Value.TypeKit == TypeKits.AmountCooldown) && DataKitsUserList[player.userID].InfoKitsList[k.Key].Amount > 0) || k.Value.TypeKit == TypeKits.Cooldown && permission.UserHasPermission(player.UserIDString, k.Value.Permission))).Skip(4 * PagePlayers[player]).Take(4))
                {
                    CuiElementContainer container = new CuiElementContainer();

                    CuiHelper.DestroyUi(player, $"COOLDOWN_LINE_{i}");
                    CuiHelper.DestroyUi(player, $"COOLDOWN_TITLE{i}");

                    var Data = DataKitsUserList[player.userID].InfoKitsList[Kit.Key];

                    double XMax = Data.Cooldown >= CurrentTime() ? (double)((Data.Cooldown - CurrentTime()) * Math.Pow(Kit.Value.CoolDown, -1)) : Data.Amount != 0 ? (double)((Data.Amount) * Math.Pow(Kit.Value.Amount, -1)) : 1;
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = true,
                        RectTransform = { AnchorMin = $"0 0", AnchorMax = $"{XMax} 1", OffsetMin = "1 1", OffsetMax = "-2 -1" },
                        Image = { Color = HexToRustFormat(Interface.HEXCooldowns) }
                    }, $"COOLDOWN_PANEL_{i}", $"COOLDOWN_LINE_{i}");

                    string InfoAmountAndCooldown = Data.Cooldown >= CurrentTime() ? GetLang("UI_COOLDONW_KIT", player.UserIDString, FormatTime(TimeSpan.FromSeconds(Data.Cooldown - CurrentTime()))) : Data.Amount != 0 ? GetLang("UI_AMOUNT_KIT", player.UserIDString, Data.Amount) : GetLang("UI_COOLDONW_KIT_NO", player.UserIDString);
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0.965517 0.9449963" },
                        Text = { Text = InfoAmountAndCooldown, Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
                    }, $"COOLDOWN_PANEL_{i}", $"COOLDOWN_TITLE{i}");

                    i++;
                    CuiHelper.AddUi(player, container);
                }
                yield return new WaitForSeconds(1);
            }
        }
        #endregion

        #region Information Kits
        void Interface_Info_Kits(BasePlayer player, string KitKey)
        {
            DestroyInfoKits(player);
            CuiElementContainer container = new CuiElementContainer();
            var Interface = config.InterfaceSetting;
            float FadeIn = Interface.InterfaceFadeIn;
            float FadeOut = Interface.InterfaceFadeOut;
            var Kit = config.KitList[KitKey];
            container.Add(new CuiElement
            {
                FadeOut = FadeOut,
                Parent = IQKITS_OVERLAY,
                Name = $"INFO_BACKGROUND",
                Components =
                    {
                        new CuiRawImageComponent { FadeIn = FadeIn, Png = GetImage($"INFO_BACKGROUND_{Interface.PNGInfoPanel}"),Color = HexToRustFormat(Interface.HEXBlock) },
                        new CuiRectTransformComponent{ AnchorMin = "0.4005208 0.2416667", AnchorMax = $"0.5958334 0.825"},
                    }
            });

            container.Add(new CuiButton
            {
                FadeOut = FadeOut - 0.2f,
                RectTransform = { AnchorMin = "0.02933349 0.01269239", AnchorMax = "0.9706663 0.1111111" },
                Button = { FadeIn = FadeIn, Command = $"kit_ui_func hide.info", Color = HexToRustFormat(Interface.HEXInfoItemButton) },
                Text = { FadeIn = FadeIn, Text = GetLang("UI_HIDE_BTN", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabelsInfoItemButton), Align = TextAnchor.MiddleCenter }
            }, $"INFO_BACKGROUND", $"HIDE_INFO_BTN");

            container.Add(new CuiLabel
            {
                FadeOut = FadeOut,
                RectTransform = { AnchorMin = "0.3916669 0.1444444", AnchorMax = "0.60625 0.237037" },
                Text = { FadeIn = FadeIn, Text = GetLang("UI_WHAT_INFO_TITLE", player.UserIDString, Kit.DisplayName.ToUpper()), Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperCenter }
            }, IQKITS_OVERLAY, $"TITLE_KIT_INFO");

            #region Centering
            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.413646f - 0.24f; /// Ширина
            float itemMargin = 0.439895f - 0.415f; /// Расстояние между 
            int itemCount = Kit.ItemKits.Count;
            float itemMinHeight = 0.89f; // Сдвиг по вертикали
            float itemHeight = 0.1f; /// Высота
            int ItemTarget = 5;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
            #endregion

            int i = 0;
            foreach (var Item in Kit.ItemKits.Take(35))
            {
                container.Add(new CuiElement
                {
                    FadeOut = FadeOut,
                    Parent = "INFO_BACKGROUND",
                    Name = $"KIT_ITEM_{i}",
                    Components = // debug
                    {
                        new CuiImageComponent { FadeIn = FadeIn, Color = HexToRustFormat("#37353E77") },
                        new CuiRectTransformComponent { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                        new CuiOutlineComponent { Color = HexToRustFormat(Interface.HEXBlockItemInfo), Distance = "0 -1.5", UseGraphicAlpha = true }
                    }
                });

                if (String.IsNullOrWhiteSpace(Item.Sprite))
                {
                    var ComponentAvatar = !String.IsNullOrWhiteSpace(Item.PNG) ? new CuiRawImageComponent { FadeIn = FadeIn, Png = GetImage($"ITEM_KIT_PNG_{Item.PNG}") } : new CuiRawImageComponent { FadeIn = FadeIn, Png = GetImage($"{Item.Shortname}_128px") };
                    container.Add(new CuiElement
                    {
                        FadeOut = FadeOut,
                        Parent = $"KIT_ITEM_{i}",
                        Name = $"ITEM_{i}",
                        Components =
                    {
                        ComponentAvatar,
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1"},
                    }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        FadeOut = FadeOut,
                        Parent = $"KIT_ITEM_{i}",
                        Name = $"ITEM_{i}",
                        Components =
                    {
                        new CuiImageComponent { FadeIn = FadeIn, Sprite = Item.Sprite },
                        new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1"},
                    }
                    });
                }

                if (Item.Rare != 0)
                {

                    int Rare = IQPlagueSkill ? IS_SKILL_RARE(player) ? Item.Rare + GET_SKILL_RARE_PERCENT() : Item.Rare : Item.Rare;
                    if (Rare >= 100) continue;
                    container.Add(new CuiPanel
                    {
                        FadeOut = FadeOut,
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Image = { FadeIn = FadeIn, Color = HexToRustFormat(Interface.HEXBackground) }
                    }, $"KIT_ITEM_{i}", $"RARE_BACKGROUND_{i}"); 

                    container.Add(new CuiLabel
                    {
                        FadeOut = FadeOut,
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Text = { FadeIn = FadeIn, Text = $"{Rare}%", FontSize = 10, Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                    }, $"RARE_BACKGROUND_{i}", $"RARE_LABEL_{i}");
                }

                container.Add(new CuiLabel
                {
                    FadeOut = FadeOut,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.930693 0.2688163" },
                    Text = { FadeIn = FadeIn, Text = $"x{Item.Amount}", FontSize = 10, Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                }, $"KIT_ITEM_{i}", $"KIT_ITEM_AMOUNT_{i}");

                #region Centring
                i++;
                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 1f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
                #endregion
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Alert Kits
        void Interface_Alert_Kits(BasePlayer player, string Message)
        {
            DestroyAlert(player);
            CuiElementContainer container = new CuiElementContainer();
            var Interface = config.InterfaceSetting;
            float FadeIn = Interface.InterfaceFadeIn;
            float FadeOut = Interface.InterfaceFadeOut;
            string AlertBackground = $"INFO_ALERT_BACKGROUND_{Interface.PNGAlert}";

            container.Add(new CuiElement
            {
                FadeOut = FadeOut,
                Parent = IQKITS_OVERLAY,
                Name = $"INFO_ALERT_BACKGROUND",
                Components =
                    {
                        new CuiRawImageComponent { FadeIn = FadeIn, Png = GetImage(AlertBackground), Color = HexToRustFormat(Interface.HEXBlock) },
                        new CuiRectTransformComponent{ AnchorMin = "0.3213542 0.01018518", AnchorMax = $"0.6958333 0.1101852"},
                    }
            });

            container.Add(new CuiLabel
            {
                FadeOut = FadeOut,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { FadeIn = FadeIn, Text = Message.ToUpper(), Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, "INFO_ALERT_BACKGROUND", "TITLE_ALERT");

            CuiHelper.AddUi(player, container);

            timer.Once(2.5f, () => { DestroyAlert(player); });
        }
        #endregion

        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            sb.Clear();
            return sb.AppendFormat("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a).ToString();
        }
        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TITLE"] = "<size=45><b>KITS MENU</b></size>",
                ["UI_DESCRIPTION"] = "<size=25><b>Your available kits are displayed here</b></size>",

                ["UI_DISPLAY_NAME_KIT"] = "<size=12><b>DISPLAY NAME KIT</b></size>\n <size=30><b>{0}</b></size>",
                ["UI_COOLDONW_KIT_NO"] = "<size=12><b>COOLDOWN</b></size>\n <size=25><b>KIT AVAILABLE</b></size>",
                ["UI_COOLDONW_KIT"] = "<size=12><b>COOLDOWN</b></size>\n <size=30><b>{0}</b></size>",
                ["UI_AMOUNT_KIT"] = "<size=12><b>AMOUNT</b></size>\n <size=30><b>{0}</b></size>",
                ["UI_BTN_WHAT_INFO"] = "<size=12><b>WHAT IS INSIDE?</b></size>",
                ["UI_BTN_TAKE_KIT"] = "<size=12><b>PICK UP</b></size>",
                ["UI_BTN_TAKE_KIT_BLOCK"] = "<size=12><b>WAIT</b></size>",
                ["UI_WHAT_INFO_TITLE"] = "<size=25><b>ITEMS IN THE {0} SET</b></size>",
                ["UI_CLOSE_BTN"] = "<size=30><b>CLOSE</b></size>",
                ["UI_HIDE_BTN"] = "<size=30><b>HIDE</b></size>",
                ["UI_NEXT_BTN"] = "<size=30><b>NEXT</b></size>",
                ["UI_BACK_BTN"] = "<size=30><b>BACK</b></size>",
                ["UI_ALERT_ACCES_KIT"] = "<size=20><b>YOU SUCCESSFULLY RECEIVED THE KIT {0}</b></size>",
                ["UI_ALERT_FULL_INVENTORY"] = "<size=20><b>YOU CANNOT TAKE THE KIT, THE INVENTORY IS OVERFULL</b></size>",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TITLE"] = "<size=45><b>НАБОРЫ</b></size>",
                ["UI_DESCRIPTION"] = "<size=25><b>Здесь отображены ваши доступные наборы</b></size>",

                ["UI_DISPLAY_NAME_KIT"] = "<size=12><b>НАЗВАНИЕ НАБОРА</b></size>\n <size=30><b>{0}</b></size>",
                ["UI_COOLDONW_KIT"] = "<size=12><b>ПЕРЕЗАРЯДКА</b></size>\n <size=30><b>{0}</b></size>",
                ["UI_AMOUNT_KIT"] = "<size=12><b>КОЛИЧЕСТВО</b></size>\n <size=30><b>{0}</b></size>",
                ["UI_COOLDONW_KIT_NO"] = "<size=12><b>ПЕРЕЗАРЯДКА</b></size>\n <size=25><b>НАБОР ДОСТУПЕН</b></size>",
                ["UI_BTN_WHAT_INFO"] = "<size=12><b>ЧТО ВНУТРИ?</b></size>",
                ["UI_BTN_TAKE_KIT"] = "<size=12><b>ЗАБРАТЬ</b></size>",
                ["UI_BTN_TAKE_KIT_BLOCK"] = "<size=12><b>ОЖИДАЙТЕ</b></size>",
                ["UI_WHAT_INFO_TITLE"] = "<size=25><b>ПРЕДМЕТЫ В НАБОРЕ {0}</b></size>",
                ["UI_CLOSE_BTN"] = "<size=30><b>ЗАКРЫТЬ</b></size>",
                ["UI_HIDE_BTN"] = "<size=30><b>СКРЫТЬ</b></size>",
                ["UI_NEXT_BTN"] = "<size=30><b>ВПЕРЕД</b></size>",
                ["UI_BACK_BTN"] = "<size=30><b>НАЗАД</b></size>",
                ["UI_ALERT_ACCES_KIT"] = "<size=20><b>ВЫ УСПЕШНО ПОЛУЧИЛИ НАБОР {0}</b></size>",
                ["UI_ALERT_FULL_INVENTORY"] = "<size=20><b>ВЫ НЕ МОЖЕТЕ ВЗЯТЬ НАБОР, ИНВЕНТАРЬ ПЕРЕПОЛНЕН</b></size>",
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }

        public static StringBuilder sb = new StringBuilder();
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        #endregion

        #region Util
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result = $"{Format(time.Days, "дней", "дня", "день")}";

            if (time.Hours != 0 && time.Days == 0)
                result = $"{Format(time.Hours, "часов", "часа", "час")}";

            if (time.Minutes != 0 && time.Hours == 0 && time.Days == 0)
                result = $"{Format(time.Minutes, "минут", "минуты", "минута")}";

            if (time.Seconds != 0 && time.Days == 0 && time.Minutes == 0 && time.Hours == 0)
                result = $"{Format(time.Seconds, "секунд", "секунды", "секунда")}";

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
        #endregion
    }
}
