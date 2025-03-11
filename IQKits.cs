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
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("IQKits", "SkuliDropek", "0.1.5")]
    [Description("Лучшие наборы из всех,которые есть")]
    class IQKits : RustPlugin
    {
        /// <summary>
        /// Обновление 0.1.0
        /// - FIX NRE если в оружие 0 патронов
        /// - Добавлена проверка на админа в чат командах
        /// - Убрал дебаг
        /// - Добавлена возможность разрешать/запрещать использовать определенные наборы во время рейдблока
        /// - Добавлена поддержка IQRankSystem
        /// - Добавлена возможность разрешать использовать определенные наборы с наличием ранга
        /// Обновление 0.1.2
        /// - Исправил неккоректную запись набора в дата файл с типами "Amount/Amount + Cooldown"
        /// Обновление 0.1.3
        /// /// </summary>

        #region Reference
        [PluginReference] Plugin ImageLibrary, IQPlagueSkill, IQChat, IQRankSystem;

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

        #region RaidBlocked
        public Boolean IsRaidBlocked(BasePlayer player, Boolean Skipped)
        {
            if (Skipped) return false;

            var ret = Interface.Call("CanTeleport", player) as String;
            if (ret != null)
                return true;
            else return false;
        }
        #endregion

        #region IQRankSystem
        Boolean IsRank(UInt64 userID, String Key)
        {
            if (!IQRankSystem) return true;
            if (String.IsNullOrWhiteSpace(Key)) return true;
            return (Boolean)IQRankSystem?.Call("API_GET_AVAILABILITY_RANK_USER", userID, Key);
        }
        String GetRankName(String Key)
        {
            String Rank = string.Empty;
            if (!IQRankSystem) return Rank;
            return (String)IQRankSystem?.Call("API_GET_RANK_NAME", Key);
        }
        #endregion

        #endregion

        #region Vars 
        public static DateTime TimeCreatedSave = SaveRestore.SaveCreatedTime.Date;
        public static DateTime RealTime = DateTime.Now.Date;
        public static int WipeTime = RealTime.Subtract(TimeCreatedSave).Days;

        enum BiomeType
        {
            None,
            Arid,
            Temperate,
            Tundra,
            Arctic
        }

        enum TypeContent
        {
            Ammo,
            Contents
        }

        enum TypeAutoKit
        {
            Single,
            List,
            PriorityList,
            BiomeList,
        }

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
            public Dictionary<String, Kits> KitList = new Dictionary<String, Kits>();
            [JsonProperty("Настройки плагинов совместимости")]
            public ReferenceSettings ReferenceSetting = new ReferenceSettings();
            internal class ReferenceSettings
            {
                [JsonProperty("Настройки IQChat")]
                public IQChatSettings IQChatSetting = new IQChatSettings();
                internal class IQChatSettings
                {
                    [JsonProperty("IQChat : Кастомный префикс в чате")]
                    public String CustomPrefix;
                    [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
                    public String CustomAvatar;
                }
            }

            internal class GeneralSettings
            {
                [JsonProperty("Настройки автоматических китов")]
                public AutoKit AutoKitSettings = new AutoKit();
                [JsonProperty("Автоматическая очистка наборов у игроков после вайпа (true - включено/false - выключено)")]
                public Boolean AutoWipeClearKits;
                internal class AutoKit
                {
                    [JsonProperty("Тип автокитов : 0 - Единый ключ, 1 - Случаный список, 2 - Приоритетный список, 3 - Биомный список")]
                    public TypeAutoKit TypeAuto;
                    [JsonProperty("Ключ набора (Тип 0 - Единый ключ)")]
                    public String StartKitKey;
                    [JsonProperty("Список ключей набора (Тип 1 - Случайный список). Дается один из случайных автокитов доступных игроку")]
                    public List<KitSettings> KitListRandom = new List<KitSettings>();
                    [JsonProperty("Список ключей набора (Тип 2 - Приоритетный список). Дается доступный набор игроку, который выше других")]
                    public List<KitSettings> KitListPriority = new List<KitSettings>();
                    [JsonProperty("Список ключей наборов по биомам (Тип 3 - Биомный список). Дается доступный набор игроку, в зависимости от биома")]
                    public List<BiomeKits> BiomeStartedKitList = new List<BiomeKits>();

                    internal class BiomeKits
                    {
                        [JsonProperty("Настройка набора")]
                        public KitSettings Kits = new KitSettings();
                        [JsonProperty("Номер биома в котором будет даваться набор ( 1 - Arid, 2 - Temperate, 3 - Tundra, 4 - Arctic )")]
                        public BiomeType biomeType;
                    }

                    internal class KitSettings
                    {
                        [JsonProperty("Ключ набора")]
                        public String StartKitKey;
                        [JsonProperty("Права для набора(не оставляйте это поле пустым)")]
                        public String Permissions;
                    }
                }
            }
            internal class InterfaceSettings
            {
                [JsonProperty("Использовать кнопку ЗАКРЫТЬ в UI (true - да/false - нет). Если установлено false - ui будет закрываться при нажатии в любом месте")]
                public Boolean CloseType;
                [JsonProperty("Закрывать интерфейс после выбора набора")]
                public Boolean CloseUiTakeKit;
                [JsonProperty("HEX: Цвет заднего фона")]
                public String HEXBackground;
                [JsonProperty("HEX: Цвет текста")]
                public String HEXLabels;
                [JsonProperty("HEX: Кнопки с информацией")]
                public String HEXInfoItemButton;
                [JsonProperty("HEX: Цвет текста на кнопке с информацией")]
                public String HEXLabelsInfoItemButton;
                [JsonProperty("HEX: Цвет кнопки забрать")]
                public String HEXAccesButton;
                [JsonProperty("HEX: Цвет текста на кнопке забрать")]
                public String HEXLabelsAccesButton;
                [JsonProperty("HEX: Цвет полосы перезарядки")]
                public String HEXCooldowns;
                [JsonProperty("HEX: Цвет блоков с информацией")]
                public String HEXBlock;
                [JsonProperty("HEX: Цвет блоков на которых будут лежать предметы")]
                public String HEXBlockItemInfo;
                [JsonProperty("Время появления интерфейса(его плавность)")]
                public Single InterfaceFadeOut;
                [JsonProperty("Время исчезновения интерфейса(его плавность)")]
                public Single InterfaceFadeIn;
                [JsonProperty("PNG заднего фона с информацией о том,что находится в наборе")]
                public String PNGInfoPanel;
                [JsonProperty("PNG заднего фона уведомления")]
                public String PNGAlert;
            }
            internal class Kits
            {
                [JsonProperty("Тип набора(0 - С перезарядкой, 1 - Лимитированый, 2 - Стартовый(АвтоКит), 3 - Лимитированый с перезарядкой)")]
                public TypeKits TypeKit;
                [JsonProperty("Отображаемое имя")]
                public String DisplayName;
                [JsonProperty("Через сколько дней вайпа будет доступен набор")]
                public Int32 WipeOpened;
                [JsonProperty("Разрешить использовать этот набор во время рейдблока (true - да/false - нет)")]
                public Boolean UseRaidBlock = true;
                [JsonProperty("IQRankSystem : Разрешить использовать этот набор только по рангу (Впишите ключ с рангом). Если вам это не нужно - оставьте поле пустым")]
                public String RankUser = "";
                [JsonProperty("Права")]
                public String Permission;
                [JsonProperty("PNG(128x128)")]
                public String PNG;
                [JsonProperty("Sprite(Установится если отсутствует PNG)")]
                public String Sprite;
                [JsonProperty("Shortname(Установится если отсутствует PNG и Sprite)")]
                public String Shortname;
                [JsonProperty("Время перезарядки набора")]
                public Int32 CoolDown;
                [JsonProperty("Количество сколько наборов можно взять")]
                public Int32 Amount;
                [JsonProperty("Предметы , которые будут даваться в данном наборе")]
                public List<ItemsKit> ItemKits = new List<ItemsKit>();

                internal class ItemsKit
                {
                    [JsonProperty("Выберите контейнер в который будет перенесен предмет(0 - Одежда, 1 - Панель быстрого доступа, 2 - Рюкзак)")]
                    public ContainerItem ContainerItemType;
                    [JsonProperty("Название предмета")]
                    public String DisplayName;
                    [JsonProperty("Shortname предмета")]
                    public String Shortname;
                    [JsonProperty("Количество(Если это команда,так-же указывайте число)")]
                    public Int32 Amount;
                    [JsonProperty("Настройки случайного количества выпадения предмета")]
                    public RandomingDrop RandomDropSettings = new RandomingDrop();
                    [JsonProperty("Шанс на выпадения предмета(Оставьте 0 - если не нужен шанс)")]
                    public Int32 Rare;
                    [JsonProperty("SkinID предмета")]
                    public UInt64 SkinID;
                    [JsonProperty("PNG предмета(если установлена команда)")]
                    public String PNG;
                    [JsonProperty("Sprite(Если установлена команда и не установлен PNG)")]
                    public String Sprite;
                    [JsonProperty("Команда(%STEAMID% заменится на ID пользователя)")]
                    public String Command;
                    [JsonProperty("Содержимое внутри предмета (Пример : Вода в бутылке) не корректируйте эти значения, если не знаете для чего они. Используйте встроенные команды")]
                    public List<ItemContents> ContentsItem = new List<ItemContents>();
                    internal class ItemContents
                    {
                        [JsonProperty("Тип : 0 - Патроны | 1 - Контент")]
                        public TypeContent ContentType;
                        [JsonProperty("Shortname предмета")]
                        public String Shortname = "";
                        [JsonProperty("Количество предметов")]
                        public Int32 Amount = 0;
                        [JsonProperty("Целостность предмета")]
                        public Single Condition = 0;
                    }

                    internal class RandomingDrop
                    {
                        [JsonProperty("Использовать случайное выпадение предмета(Действует только на предметы)")]
                        public Boolean UseRandomItems;
                        [JsonProperty("Минимальное количество")]
                        public Int32 MinAmount;
                        [JsonProperty("Максимальное количество")]
                        public Int32 MaxAmount;
                    }
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
                        },
                    },
                    KitList = new Dictionary<String, Kits>
                    {
                        #region Start Kits
                        ["start1"] = new Kits
                        {
                            TypeKit = TypeKits.Started,
                            Amount = 0,
                            CoolDown = 0,
                            WipeOpened = 0,
                            DisplayName = "Новичок",
                            Permission = "iqkits.start1",
                            RankUser = "",
                            UseRaidBlock = false,
                            PNG = "",
                            Shortname = "",
                            Sprite = "",
                            ItemKits = new List<Kits.ItemsKit>
                            {
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak103",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                },
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak104",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                },
                            },
                        },
                        ["start2"] = new Kits
                        {
                            TypeKit = TypeKits.Started,
                            Amount = 0,
                            CoolDown = 0,
                            WipeOpened = 0,
                            DisplayName = "Новичок #2",
                            Permission = "iqkits.start2",
                            RankUser = "",
                            UseRaidBlock = false,
                            PNG = "",
                            Shortname = "",
                            Sprite = "",
                            ItemKits = new List<Kits.ItemsKit>
                            {
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak103",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                },
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak104",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                },
                            },
                        },
                        ["start3Premium"] = new Kits
                        {
                            TypeKit = TypeKits.Started,
                            Amount = 0,
                            CoolDown = 0,
                            WipeOpened = 0,
                            DisplayName = "Премиум новичок",
                            Permission = "iqkits.premium",
                            RankUser = "",
                            UseRaidBlock = false,
                            PNG = "",
                            Shortname = "",
                            Sprite = "",
                            ItemKits = new List<Kits.ItemsKit>
                            {
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak103",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                },
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak104",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                },
                            },
                        },
                        #endregion

                        #region Kits
                        ["hunter"] = new Kits
                        {
                            TypeKit = TypeKits.Cooldown,
                            Amount = 0,
                            CoolDown = 300,
                            WipeOpened = 2,
                            DisplayName = "Охотник",
                            Permission = "iqkits.default",
                            RankUser = "",
                            UseRaidBlock = false,
                            PNG = "",
                            Shortname = "rifle.ak",
                            Sprite = "",
                            ItemKits = new List<Kits.ItemsKit>
                            {
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak103",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                    ContentsItem = new List<Kits.ItemsKit.ItemContents> { }
                                },
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak104",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                    ContentsItem = new List<Kits.ItemsKit.ItemContents> { }
                                },
                            },
                        },
                        ["med"] = new Kits
                        {
                            TypeKit = TypeKits.Amount,
                            Amount = 10,
                            CoolDown = 0,
                            WipeOpened = 1,
                            DisplayName = "Медик",
                            Permission = "iqkits.default",
                            RankUser = "",
                            UseRaidBlock = false,
                            PNG = "",
                            Shortname = "",
                            Sprite = "assets/icons/broadcast.png",
                            ItemKits = new List<Kits.ItemsKit>
                            {
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak103",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                    ContentsItem = new List<Kits.ItemsKit.ItemContents> { }
                                },
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak104",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                    ContentsItem = new List<Kits.ItemsKit.ItemContents> { }
                                },
                            },
                        },
                        ["food"] = new Kits
                        {
                            TypeKit = TypeKits.AmountCooldown,
                            Amount = 10,
                            CoolDown = 300,
                            DisplayName = "Еда",
                            WipeOpened = 2,
                            Permission = "iqkits.default",
                            RankUser = "",
                            UseRaidBlock = false,
                            PNG = "https://i.imgur.com/rSWlSlN.png",
                            Shortname = "",
                            Sprite = "",
                            ItemKits = new List<Kits.ItemsKit>
                            {
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak103",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                    ContentsItem = new List<Kits.ItemsKit.ItemContents> { }
                                },
                                new Kits.ItemsKit
                                {
                                    ContainerItemType = ContainerItem.containerMain,
                                    RandomDropSettings = new Kits.ItemsKit.RandomingDrop
                                    {
                                        UseRandomItems = false,
                                        MinAmount = 1,
                                        MaxAmount = 1
                                    },
                                    DisplayName = "Ak104",
                                    Amount = 1,
                                    Command = "",
                                    PNG = "",
                                    Shortname = "rifle.ak",
                                    Rare = 0,
                                    Sprite = "",
                                    SkinID = 0,
                                    ContentsItem = new List<Kits.ItemsKit.ItemContents> { }
                                },
                            },
                        },
                        #endregion
                    },
                    GeneralSetting = new GeneralSettings
                    {
                        AutoWipeClearKits = true,
                        AutoKitSettings = new GeneralSettings.AutoKit
                        {
                            TypeAuto = TypeAutoKit.Single,
                            StartKitKey = "start1",
                            KitListRandom = new List<GeneralSettings.AutoKit.KitSettings>
                            {
                                new GeneralSettings.AutoKit.KitSettings
                                {
                                    Permissions = "iqkits.vip",
                                    StartKitKey = "start1"
                                },
                                new GeneralSettings.AutoKit.KitSettings
                                {
                                    Permissions = "iqkits.hunter",
                                    StartKitKey = "food"
                                },
                            },
                            KitListPriority = new List<GeneralSettings.AutoKit.KitSettings>
                            {
                                new GeneralSettings.AutoKit.KitSettings
                                {
                                    Permissions = "iqkits.vip",
                                    StartKitKey = "start1"
                                },
                                new GeneralSettings.AutoKit.KitSettings
                                {
                                    Permissions = "iqkits.hunter",
                                    StartKitKey = "food"
                                },
                            },
                            BiomeStartedKitList = new List<GeneralSettings.AutoKit.BiomeKits>
                            {
                                   new GeneralSettings.AutoKit.BiomeKits
                                   {
                                       biomeType = BiomeType.Arctic,
                                       Kits = new GeneralSettings.AutoKit.KitSettings
                                       {
                                           Permissions = "iqkits.default",
                                           StartKitKey = "start1"
                                       }
                                   },
                                   new GeneralSettings.AutoKit.BiomeKits
                                   {
                                       biomeType = BiomeType.Arid,
                                       Kits = new GeneralSettings.AutoKit.KitSettings
                                       {
                                           Permissions = "iqkits.default",
                                           StartKitKey = "start1"
                                       }
                                   },
                                   new GeneralSettings.AutoKit.BiomeKits
                                   {
                                       biomeType = BiomeType.None,
                                       Kits = new GeneralSettings.AutoKit.KitSettings
                                       {
                                           Permissions = "iqkits.default",
                                           StartKitKey = "start1"
                                       }
                                   },
                                   new GeneralSettings.AutoKit.BiomeKits
                                   {
                                       biomeType = BiomeType.Temperate,
                                       Kits = new GeneralSettings.AutoKit.KitSettings
                                       {
                                           Permissions = "iqkits.default",
                                           StartKitKey = "start1"
                                       }
                                   },
                                   new GeneralSettings.AutoKit.BiomeKits
                                   {
                                       biomeType = BiomeType.Tundra,
                                       Kits = new GeneralSettings.AutoKit.KitSettings
                                       {
                                           Permissions = "iqkits.default",
                                           StartKitKey = "start1"
                                       }
                                   },
                            },                      
                        },
                    },
                    InterfaceSetting = new InterfaceSettings
                    {
                        CloseType = true,
                        CloseUiTakeKit = false,
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
        public Hash<ulong, DataKitsUser> DataKitsUserList = new Hash<ulong, DataKitsUser>();

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
        private void ClearData()
        {
            if (!config.GeneralSetting.AutoWipeClearKits) return;
            DataKitsUserList.Clear();
            WriteData();
        }
        void ReadData() => DataKitsUserList = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Hash<ulong, DataKitsUser>>("IQKits/KitsData");
        void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQKits/KitsData", DataKitsUserList);
        void RegisteredDataUser(BasePlayer player)
        {
            if (!DataKitsUserList.ContainsKey(player.userID))
                DataKitsUserList.Add(player.userID, new DataKitsUser
                {
                    InfoKitsList = new Dictionary<string, DataKitsUser.InfoKits> { }
                });

            foreach (var Kit in config.KitList.Where(x => !DataKitsUserList[player.userID].InfoKitsList.ContainsKey(x.Key) && (String.IsNullOrWhiteSpace(x.Value.Permission) || permission.UserHasPermission(player.UserIDString, x.Value.Permission))))
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
                        AddImage($"https://api.skyplugins.ru/api/getimage/{img.Shortname}/128", $"{img.Shortname}_128px");
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

            foreach(var PermissionGeneral in GeneralSettings.AutoKitSettings.KitListRandom)
                if (!permission.PermissionExists(PermissionGeneral.Permissions, this))
                    permission.RegisterPermission(PermissionGeneral.Permissions, this);

            foreach (var PermissionGeneral in GeneralSettings.AutoKitSettings.KitListPriority)
                if (!permission.PermissionExists(PermissionGeneral.Permissions, this))
                    permission.RegisterPermission(PermissionGeneral.Permissions, this);

            foreach (var PermissionGeneral in GeneralSettings.AutoKitSettings.BiomeStartedKitList)
                if (!permission.PermissionExists(PermissionGeneral.Kits.Permissions, this))
                    permission.RegisterPermission(PermissionGeneral.Kits.Permissions, this);

            foreach (var PermissionKits in KitList)
                if (!permission.PermissionExists(PermissionKits.Value.Permission, this))
                    permission.RegisterPermission(PermissionKits.Value.Permission, this);
        }
        #endregion

        #region AutoKit
        void AutoKitGive(BasePlayer player)
        {
            if (player == null) return;
            Configuration.GeneralSettings.AutoKit AutoKit = config.GeneralSetting.AutoKitSettings;
            Dictionary<String, Configuration.Kits> KitList = config.KitList;

            switch(AutoKit.TypeAuto)
            {
                case TypeAutoKit.Single:
                    {
                        if (String.IsNullOrWhiteSpace(AutoKit.StartKitKey) || !KitList.ContainsKey(AutoKit.StartKitKey))
                        {
                            PrintWarning("У вас не верно указан стартовый ключ, такого набора не существует! Игрок не получил его автоматически");
                            return;
                        }
                        ParseAndGive(player, AutoKit.StartKitKey);
                        break;
                    }
                case TypeAutoKit.List:
                    {
                        Configuration.GeneralSettings.AutoKit.KitSettings RandomKit = AutoKit.KitListRandom.Where(k => permission.UserHasPermission(player.UserIDString, k.Permissions) && KitList.ContainsKey(k.StartKitKey) && WipeTime >= KitList[k.StartKitKey].WipeOpened).ToList().GetRandom();
                        if (RandomKit == null) return;
                        ParseAndGive(player, RandomKit.StartKitKey);
                        break;
                    }
                case TypeAutoKit.PriorityList:
                    {
                        Configuration.GeneralSettings.AutoKit.KitSettings Kit = AutoKit.KitListPriority.FirstOrDefault(k => permission.UserHasPermission(player.UserIDString, k.Permissions) && KitList.ContainsKey(k.StartKitKey) && WipeTime >= KitList[k.StartKitKey].WipeOpened);
                        if (Kit == null) return;
                        ParseAndGive(player, Kit.StartKitKey);
                        break;
                    }
                case TypeAutoKit.BiomeList:
                    {
                        Configuration.GeneralSettings.AutoKit.BiomeKits BiomeKit = AutoKit.BiomeStartedKitList.FirstOrDefault(k => GetBiome(player) == k.biomeType && permission.UserHasPermission(player.UserIDString, k.Kits.Permissions) && KitList.ContainsKey(k.Kits.StartKitKey) && WipeTime >= KitList[k.Kits.StartKitKey].WipeOpened);
                        if (BiomeKit == null) return;
                        ParseAndGive(player, BiomeKit.Kits.StartKitKey);
                        break;
                    }
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
            if (!config.InterfaceSetting.CloseUiTakeKit)
            {
                DestroyKits(player);
                Interface_Loaded_Kits(player);
                Interface_Alert_Kits(player, GetLang("UI_ALERT_ACCES_KIT", player.UserIDString, Kit.DisplayName));
            }
            else DestroyAll(player);
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
                    Int32 Amount = Item.RandomDropSettings.UseRandomItems ? UnityEngine.Random.Range(Item.RandomDropSettings.MinAmount, Item.RandomDropSettings.MaxAmount) : (Item.Amount > 1 ? Item.Amount : 1);
                    Item item = ItemManager.CreateByName(Item.Shortname, Amount, Item.SkinID);
                    if (!String.IsNullOrWhiteSpace(Item.DisplayName))
                        item.name = Item.DisplayName;

                    foreach(var Content in Item.ContentsItem)
                    {
                        Item ItemContent = ItemManager.CreateByName(Content.Shortname, Content.Amount);
                        ItemContent.condition = Content.Condition;
                        switch(Content.ContentType)
                        {
                            case TypeContent.Contents:
                                {
                                    ItemContent.MoveToContainer(item.contents);
                                    break;
                                }
                            case TypeContent.Ammo:
                                {
                                    BaseProjectile Weapon = item.GetHeldEntity() as BaseProjectile;
                                    if (Weapon != null)
                                    {
                                        Weapon.primaryMagazine.contents = ItemContent.amount;
                                        Weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(Content.Shortname);
                                    }
                                    break;
                                }
                        }
                    }

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
                UseRaidBlock = false,
                RankUser = "",
                Sprite = "assets/icons/gear.png",
                TypeKit = TypeKits.Cooldown,
                ItemKits = GetPlayerItems(player),
                WipeOpened = 0,
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
            ItemsKit.RandomDropSettings = new Configuration.Kits.ItemsKit.RandomingDrop
            {
                MinAmount = 0,
                MaxAmount = 0,
                UseRandomItems = false,
            };
            ItemsKit.ContentsItem = GetContentItem(item);

            return ItemsKit;
        }

        private List<Configuration.Kits.ItemsKit.ItemContents> GetContentItem(Item Item)
        {
            List<Configuration.Kits.ItemsKit.ItemContents> Contents = new List<Configuration.Kits.ItemsKit.ItemContents>();

            if (Item.contents != null)
                foreach (Item Content in Item.contents.itemList)
                {
                    Configuration.Kits.ItemsKit.ItemContents ContentItem = new Configuration.Kits.ItemsKit.ItemContents();
                    ContentItem.ContentType = TypeContent.Contents;
                    ContentItem.Shortname = Content.info.shortname;
                    ContentItem.Amount = Content.amount;
                    ContentItem.Condition = Content.condition;
                    Contents.Add(ContentItem);
                }
            BaseProjectile Weapon = Item.GetHeldEntity() as BaseProjectile;
            if (Weapon != null)
            {
                Configuration.Kits.ItemsKit.ItemContents ContentItem = new Configuration.Kits.ItemsKit.ItemContents();
                ContentItem.ContentType = TypeContent.Ammo;
                ContentItem.Shortname = Weapon.primaryMagazine.ammoType.shortname;
                ContentItem.Amount = Weapon.primaryMagazine.contents == 0 ? 1 : Weapon.primaryMagazine.contents;
                ContentItem.Condition = Weapon.primaryMagazine.ammoType.condition.max;
                Contents.Add(ContentItem);
            }

            return Contents;
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

        #region Kit Edit
        void KitEdit(BasePlayer player, String NameKit)
        {
            if (!player.IsAdmin) return;

            if (!config.KitList.ContainsKey(NameKit))
            {
                SendChat(player, "Ключ данного набора не существует!");
                return;
            }

            var Kit = config.KitList[NameKit];
            Kit.ItemKits = GetPlayerItems(player);

            SaveConfig();
            SendChat(player, $"Предметы набора с ключем {NameKit} успешно изменены, настройки сохранены");
        }
        #endregion

        #endregion

        public bool IsRareDrop(int Rare) => UnityEngine.Random.Range(0, 100) >= (100 - (Rare > 100 ? 100 : Rare));
        #endregion

        #region Hooks
        void OnNewSave(string filename) => ClearData();
        object OnPlayerRespawned(BasePlayer player)
        {
            player.inventory.Strip();
            AutoKitGive(player);
            return null;
        }
        void Init() => ReadData();
        private void OnServerInitialized()
        {
            RegisteredPermissions();
            LoadedImage();

            foreach (BasePlayer p in BasePlayer.activePlayerList)
                OnPlayerConnected(p);
        }
        void OnPlayerConnected(BasePlayer player)
        {
            CachingImage(player);
            RegisteredDataUser(player);            
        }
        void OnPlayerDisconnected(BasePlayer player, string reason) => player.SetFlag(BaseEntity.Flags.Reserved3, false);
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                DestroyAll(player);

            ServerMgr.Instance.StopCoroutine(DownloadImages());

            CheckKit();
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
                        if (!player.IsAdmin) return;
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
                        if (!player.IsAdmin) return;
                        string NameKit = arg[1];
                        if (string.IsNullOrWhiteSpace(NameKit))
                        {
                            SendChat(player, "Введите корректное название!");
                            return;
                        }
                        KitRemove(player, NameKit);
                        break;
                    }
                case "copy":
                case "edit":
                    {
                        if (!player.IsAdmin) return;
                        string NameKit = arg[1];
                        if (string.IsNullOrWhiteSpace(NameKit))
                        {
                            SendChat(player, "Введите корректное название!");
                            return;
                        }
                        KitEdit(player,NameKit);
                        break;
                    }
                case "give":
                    {
                        if (!player.IsAdmin) return;
                        String IDarName = arg[1];
                        if(String.IsNullOrWhiteSpace(IDarName))
                        {
                            SendChat(player, "Введите корректное имя или ID");
                            return;
                        }
                        BasePlayer TargetUser = BasePlayer.Find(IDarName);
                        if(TargetUser == null)
                        {
                            SendChat(player, "Такого игрока нет на сервере");
                            return;
                        }
                        String KitKey = arg[2];
                        if(String.IsNullOrWhiteSpace(KitKey))
                        {
                            SendChat(player, "Введите корректный ключ набора");
                            return;
                        }
                        if(!config.KitList.ContainsKey(KitKey))
                        {
                            SendChat(player, "Набора с данным ключем не существует");
                            return;
                        }
                        ParseAndGive(TargetUser, KitKey);
                        break;
                    }
            }
        }
        [ConsoleCommand("kit")]
        void IQKITS_ConsoleCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null || !player.IsAdmin)
                if (arg.Args.Length < 2 || arg == null || arg == null)
                {
                    PagePlayers[player] = 0;
                    Interface_IQ_Kits(player);
                    return;
                }

            switch (arg.Args[0])
            {
                case "create":
                case "createkit":
                case "add":
                case "new":
                    {
                        if (player == null || !player.IsAdmin) return;
                        string NameKit = arg.Args[1];
                        if (string.IsNullOrWhiteSpace(NameKit))
                        {
                            PrintToConsole(player, "Введите корректное название!");
                            return;
                        }
                        CreateNewKit(player, NameKit);
                        break;
                    }
                case "remove":
                case "delete":
                case "revoke":
                    {
                        if (player == null || !player.IsAdmin) return;
                        string NameKit = arg.Args[1];
                        if (string.IsNullOrWhiteSpace(NameKit))
                        {
                            PrintToConsole(player, "Введите корректное название!");
                            return;
                        }
                        KitRemove(player, NameKit);
                        break;
                    }
                case "give":
                    {
                        if (player != null && !player.IsAdmin) return;

                        String IDarName = arg.Args[1];
                        if (String.IsNullOrWhiteSpace(IDarName))
                        {
                            if (player != null)
                                PrintToConsole(player, "Введите корректное имя или ID");
                            PrintError("Введите корректное имя или ID");
                            return;
                        }
                        BasePlayer TargetUser = BasePlayer.Find(IDarName);
                        if (TargetUser == null)
                        {
                            if (player != null)
                                PrintToConsole(player, "Такого игрока нет на сервере");
                            PrintError("Такого игрока нет на сервере");
                            return;
                        }
                        String KitKey = arg.Args[2];
                        if (String.IsNullOrWhiteSpace(KitKey))
                        {
                            if (player != null)
                                PrintToConsole(player, "Введите корректный ключ набора");
                            PrintError("Введите корректный ключ набора");
                            return;
                        }
                        if (!config.KitList.ContainsKey(KitKey))
                        {
                            if (player != null)
                                PrintToConsole(player, "Набора с данным ключем не существует");
                            PrintError("Набора с данным ключем не существует");
                            return;
                        }
                        ParseAndGive(TargetUser, KitKey);
                        if (player != null)
                            PrintToConsole(player, "Успешно выдан набор");
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
                RectTransform = { AnchorMin = "0 0.9149", AnchorMax = "1 1" },
                Text = { FadeIn = FadeIn, Text = GetLang("UI_TITLE", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  IQKITS_OVERLAY, "TITLE");

            container.Add(new CuiLabel
            {
                FadeOut = FadeOut,
                RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 0.94" },
                Text = { FadeIn = FadeIn, Text = GetLang("UI_DESCRIPTION", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, IQKITS_OVERLAY, "DESCRIPTION");

            if (Interface.CloseType)
            {
                container.Add(new CuiButton
                {
                    FadeOut = FadeOut - 0.2f,
                    RectTransform = { AnchorMin = "0.87 0.94", AnchorMax = "1 1" },
                    Button = { FadeIn = FadeIn, Command = $"kit_ui_func close.ui", Color = "0 0 0 0" },
                    Text = { FadeIn = FadeIn, Text = GetLang("UI_CLOSE_BTN", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabels), Align = TextAnchor.MiddleCenter }
                }, IQKITS_OVERLAY, "CLOSE_BTN");
            }
            else
            {
                container.Add(new CuiButton
                {
                    FadeOut = FadeOut - 0.2f,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { FadeIn = FadeIn, Command = $"kit_ui_func close.ui", Color = "0 0 0 0" },
                    Text = { FadeIn = FadeIn, Text = "" }
                }, IQKITS_OVERLAY, "CLOSE_BTN");
            }


            CuiHelper.AddUi(player, container);
            Interface_Loaded_Kits(player);
            ServerMgr.Instance.StartCoroutine(UI_UpdateCooldown(player));
        }

        #region LoadedKits
        void Interface_Loaded_Kits(BasePlayer player)
        {
            RegisteredDataUser(player);
            if (config.KitList.Where(k => ((k.Value.TypeKit != TypeKits.Started && WipeTime >= k.Value.WipeOpened) 
                                      && (((k.Value.TypeKit == TypeKits.Amount || k.Value.TypeKit == TypeKits.AmountCooldown) && (DataKitsUserList[player.userID].InfoKitsList.ContainsKey(k.Key) && DataKitsUserList[player.userID].InfoKitsList[k.Key].Amount > 0)) || k.Value.TypeKit == TypeKits.Cooldown)
                                      && (String.IsNullOrWhiteSpace(k.Value.Permission) || permission.UserHasPermission(player.UserIDString, k.Value.Permission))
                                      && !IsRaidBlocked(player, k.Value.UseRaidBlock)
                                      && IsRank(player.userID, k.Value.RankUser)
                                      )).Skip(4 * PagePlayers[player]).Take(4).Count() == 0) return;

            CuiElementContainer container = new CuiElementContainer();
            var Interface = config.InterfaceSetting;
            float FadeIn = Interface.InterfaceFadeIn;
            float FadeOut = Interface.InterfaceFadeOut;
            int CountKitPage = config.KitList.Where(k => ((k.Value.TypeKit != TypeKits.Started && WipeTime >= k.Value.WipeOpened) 
                                                     && (((k.Value.TypeKit == TypeKits.Amount || k.Value.TypeKit == TypeKits.AmountCooldown) && (DataKitsUserList[player.userID].InfoKitsList.ContainsKey(k.Key) && DataKitsUserList[player.userID].InfoKitsList[k.Key].Amount > 0)) || k.Value.TypeKit == TypeKits.Cooldown)
                                                     && (String.IsNullOrWhiteSpace(k.Value.Permission) || permission.UserHasPermission(player.UserIDString, k.Value.Permission))
                                                     && !IsRaidBlocked(player, k.Value.UseRaidBlock)
                                                     && IsRank(player.userID, k.Value.RankUser)
                                                     )).Skip(4 * (PagePlayers[player] + 1)).Take(4).Count();

            int x = 0, y = 0, i = 0;
            foreach (var Kit in config.KitList.Where(k => ((k.Value.TypeKit != TypeKits.Started && WipeTime >= k.Value.WipeOpened) 
                                                      && (((k.Value.TypeKit == TypeKits.Amount || k.Value.TypeKit == TypeKits.AmountCooldown) && (DataKitsUserList[player.userID].InfoKitsList.ContainsKey(k.Key) && DataKitsUserList[player.userID].InfoKitsList[k.Key].Amount > 0)) || k.Value.TypeKit == TypeKits.Cooldown) 
                                                      && (String.IsNullOrWhiteSpace(k.Value.Permission) || permission.UserHasPermission(player.UserIDString, k.Value.Permission))
                                                      && !IsRaidBlocked(player, k.Value.UseRaidBlock) 
                                                      && IsRank(player.userID, k.Value.RankUser)
                                                      )).Skip(4 * PagePlayers[player]).Take(4))
            {
                var Data = DataKitsUserList[player.userID].InfoKitsList[Kit.Key];

                container.Add(new CuiPanel
                {
                    FadeOut = FadeOut,
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = $"{0.08 + (x * 0.52)} {0.5839 - (y * 0.3419)}", AnchorMax = $"{0.39159 + (x * 0.52)} {0.82 - (y * 0.3419)}" },
                    Image = { FadeIn = FadeIn, Color = "0 0 0 0" }
                }, IQKITS_OVERLAY, $"KIT_PANEL_{i}");

                #region Avatar
                container.Add(new CuiPanel
                {
                    FadeOut = FadeOut,
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.4359 1" }, 
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
                        new CuiRectTransformComponent{ AnchorMin = "0.077 0.073", AnchorMax = $"0.92 0.91"},
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
                        new CuiRectTransformComponent{ AnchorMin = "0.077 0.073", AnchorMax = $"0.92 0.91"},
                    }
                    });
                }
                #endregion

                #region Name

                container.Add(new CuiPanel
                {
                    FadeOut = FadeOut,
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = $"0.46 0.64", AnchorMax = $"1 1" },
                    Image = { FadeIn = FadeIn, Color = HexToRustFormat(Interface.HEXBlock) }
                }, $"KIT_PANEL_{i}", $"DISPLAY_NAME_PANEL_{i}");

                container.Add(new CuiLabel
                {
                    FadeOut = FadeOut,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.9649 0.945" },
                    Text = { FadeIn = FadeIn, Text = GetLang("UI_DISPLAY_NAME_KIT", player.UserIDString, Kit.Value.DisplayName.ToUpper()), Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }

                }, $"DISPLAY_NAME_PANEL_{i}", $"TITLE_KIT_{i}");
                #endregion

                #region Cooldown
                container.Add(new CuiPanel
                {
                    FadeOut = FadeOut,
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = $"0.46 0.25", AnchorMax = $"1 0.6" },
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
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.96 0.94" },
                    Text = { FadeIn = FadeIn, Text = InfoAmountAndCooldown, Color = HexToRustFormat(Interface.HEXLabels), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
                }, $"COOLDOWN_PANEL_{i}", $"COOLDOWN_TITLE{i}");
                #endregion

                #region Button

                container.Add(new CuiButton
                {
                    FadeOut = FadeOut - 0.2f,
                    RectTransform = { AnchorMin = "0.46 0", AnchorMax = "0.72 0.21" },  //
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
                    RectTransform = { AnchorMin = "0.73 0", AnchorMax = "1 0.21" },
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
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.10 0.054" },
                    Button = { FadeIn = FadeIn, Command = "kit_ui_func back.page", Color = "0 0 0 0" },
                    Text = { FadeIn = FadeIn, Text = GetLang("UI_BACK_BTN", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabels), Align = TextAnchor.MiddleCenter }
                }, IQKITS_OVERLAY, $"BTN_BACK_BUTTON");
            }
            if (CountKitPage != 0)
            {
                container.Add(new CuiButton
                {
                    FadeOut = FadeOut - 0.2f,
                    RectTransform = { AnchorMin = "0.89 0", AnchorMax = "1 0.054" },
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
                foreach (var Kit in config.KitList.Where(k => ((k.Value.TypeKit != TypeKits.Started && WipeTime >= k.Value.WipeOpened) 
                                                          && (((k.Value.TypeKit == TypeKits.Amount || k.Value.TypeKit == TypeKits.AmountCooldown) && (DataKitsUserList[player.userID].InfoKitsList.ContainsKey(k.Key) && DataKitsUserList[player.userID].InfoKitsList[k.Key].Amount > 0)) || k.Value.TypeKit == TypeKits.Cooldown)
                                                          && permission.UserHasPermission(player.UserIDString, k.Value.Permission)
                                                          && !IsRaidBlocked(player, k.Value.UseRaidBlock)
                                                          && IsRank(player.userID, k.Value.RankUser)
                                                          )).Skip(4 * PagePlayers[player]).Take(4))
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
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "0.96 0.94" },
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
                        new CuiRectTransformComponent{ AnchorMin = "0.40 0.24", AnchorMax = $"0.59 0.8249"},
                    }
            });

            container.Add(new CuiButton
            {
                FadeOut = FadeOut - 0.2f,
                RectTransform = { AnchorMin = "0.029 0.012689", AnchorMax = "0.97 0.11" },
                Button = { FadeIn = FadeIn, Command = $"kit_ui_func hide.info", Color = HexToRustFormat(Interface.HEXInfoItemButton) },
                Text = { FadeIn = FadeIn, Text = GetLang("UI_HIDE_BTN", player.UserIDString), Color = HexToRustFormat(Interface.HEXLabelsInfoItemButton), Align = TextAnchor.MiddleCenter }
            }, $"INFO_BACKGROUND", $"HIDE_INFO_BTN");

            container.Add(new CuiLabel
            {
                FadeOut = FadeOut,
                RectTransform = { AnchorMin = "0.39 0.14", AnchorMax = "0.6 0.2" },
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
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.93 0.268" },
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
                        new CuiRectTransformComponent{ AnchorMin = "0.32 0.01", AnchorMax = $"0.69 0.11"},
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
        void CheckKit()
        {
            foreach (var Data in DataKitsUserList)
            {
                UInt64 PlayerID = Data.Key;

                foreach (var kitList in config.KitList.Where(k => DataKitsUserList[PlayerID].InfoKitsList.ContainsKey(k.Key)))
                {
                    String KitKey = kitList.Key;
                    var DataPlayer = Data.Value.InfoKitsList[KitKey];

                    if (!permission.UserHasPermission(PlayerID.ToString(), kitList.Value.Permission))
                        DataKitsUserList[PlayerID].InfoKitsList.Remove(KitKey);
                }
            }
        }

        BiomeType GetBiome(BasePlayer player)
        {
            if (TerrainMeta.BiomeMap.GetBiome(player.transform.position, 1) > 0.5) return BiomeType.Arid;
            if (TerrainMeta.BiomeMap.GetBiome(player.transform.position, 2) > 0.5) return BiomeType.Temperate;
            if (TerrainMeta.BiomeMap.GetBiome(player.transform.position, 4) > 0.5) return BiomeType.Tundra;
            if (TerrainMeta.BiomeMap.GetBiome(player.transform.position, 8) > 0.5) return BiomeType.Arctic;
            return BiomeType.None;
        }

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

        #region API

        #region API_KITS

        String API_KIT_GET_AUTO_KIT() => config.GeneralSetting.AutoKitSettings.StartKitKey;

        List<String> API_KIT_GET_AUTO_KIT_LIST()
        {
            List<String> KitList = new List<string>();
            for(int i = 0; i < config.GeneralSetting.AutoKitSettings.KitListRandom.Count; i++)
                KitList.Add(config.GeneralSetting.AutoKitSettings.KitListRandom[i].StartKitKey);
            return KitList;
        }

        List<String> API_KIT_GET_ALL_KIT_LIST()
        {
            List<String> KitList = new List<string>();
            foreach (var Kit in config.KitList)
                KitList.Add(Kit.Key);

            return KitList;
        }

        internal class ShortInfoKit
        {
            public String Shortname;
            public Int32 Amount;
            public UInt64 SkinID;
        }

        String API_KIT_GET_ITEMS(String KitKey)
        {
            Configuration.Kits Kit = config.KitList[KitKey];
            if(Kit == null) return String.Empty;
            List<ShortInfoKit> ShortKitList = new List<ShortInfoKit>();

            foreach(Configuration.Kits.ItemsKit KitItems in Kit.ItemKits.Where(x => !String.IsNullOrWhiteSpace(x.Shortname)))
                ShortKitList.Add(new ShortInfoKit { Shortname = KitItems.Shortname, Amount = KitItems.Amount, SkinID = KitItems.SkinID });

            return JsonConvert.SerializeObject(ShortKitList);
        }
        String API_KIT_GET_NAME(String KitKey)
        {
            if (String.IsNullOrWhiteSpace(KitKey)) return "NONE";
            if (!config.KitList.ContainsKey(KitKey)) return "NONE";
            return config.KitList[KitKey].DisplayName;
        }

        Boolean API_IS_KIT(String KitKey)
        {
            if (String.IsNullOrWhiteSpace(KitKey)) return false;
            return (Boolean)config.KitList.ContainsKey(KitKey);
        }

        Int32 API_KIT_GET_MAX_AMOUNT(String KitKey)
        {
            if (String.IsNullOrWhiteSpace(KitKey)) return 0;
            if (!config.KitList.ContainsKey(KitKey))
            {
                PrintError($"API_KIT_GET_MAX_AMOUNT : Ключа {KitKey} не существует!");
                return 0;
            }
            return config.KitList[KitKey].Amount;
        }

        Int32 API_KIT_GET_MAX_COOLDOWN(String KitKey)
        {
            if (String.IsNullOrWhiteSpace(KitKey)) return 0;
            if (!config.KitList.ContainsKey(KitKey))
            {
                PrintError($"API_KIT_GET_MAX_COOLDOWN : Ключа {KitKey} не существует!");
                return 0;
            }
            return config.KitList[KitKey].CoolDown;
        }

        #endregion

        #region PLAYERS_API

        void API_KIT_GIVE(BasePlayer player, String KitKey)
        {
            if (player == null) return;
            if (!config.KitList.ContainsKey(KitKey))
            {
                PrintError($"Ключа {KitKey} не существует, набор не выдан!");
                return;
            }
            ParseAndGive(player, KitKey);
        }

        Boolean API_IS_KIT_PLAYER(BasePlayer player, String KitKey)
        {
            if (player == null) return false;
            if (String.IsNullOrWhiteSpace(KitKey)) return false;
            if (!DataKitsUserList.ContainsKey(player.userID)) return false;
            if (!config.KitList.ContainsKey(KitKey)) return false;
            return DataKitsUserList[player.userID].InfoKitsList.ContainsKey(KitKey);
        }

        Int32 API_KIT_PLAYER_GET_COOLDOWN(BasePlayer player, String KitKey)
        {
            if (player == null) return 0;
            if (String.IsNullOrWhiteSpace(KitKey)) return 0;
            if (!DataKitsUserList.ContainsKey(player.userID))
            {
                PrintError($"API_KIT_PLAYER_GET_COOLDOWN : Такого игрока не существует в дата-файле");
                return 0;
            }
            if (!config.KitList.ContainsKey(KitKey))
            {
                PrintError($"API_KIT_PLAYER_GET_COOLDOWN : Ключа {KitKey} не существует");
                return 0;
            }
            if(!DataKitsUserList[player.userID].InfoKitsList.ContainsKey(KitKey))
            {
                PrintError($"API_KIT_PLAYER_GET_COOLDOWN : У игрока нет данного набора {KitKey}");
                return 0;
            }
            return DataKitsUserList[player.userID].InfoKitsList[KitKey].Cooldown;
        }

        Int32 API_KIT_PLAYER_GET_AMOUNT(BasePlayer player, String KitKey)
        {
            if (player == null) return 0;
            if (String.IsNullOrWhiteSpace(KitKey)) return 0;
            if (!DataKitsUserList.ContainsKey(player.userID))
            {
                PrintError($"API_KIT_PLAYER_GET_AMOUNT : Такого игрока не существует в дата-файле");
                return 0;
            }
            if (!config.KitList.ContainsKey(KitKey))
            {
                PrintError($"API_KIT_PLAYER_GET_AMOUNT : Ключа {KitKey} не существует");
                return 0;
            }
            if (!DataKitsUserList[player.userID].InfoKitsList.ContainsKey(KitKey))
            {
                PrintError($"API_KIT_PLAYER_GET_AMOUNT : У игрока нет данного набора {KitKey}");
                return 0;
            }
            return DataKitsUserList[player.userID].InfoKitsList[KitKey].Amount;
        }

        #endregion

        #endregion
    }
}
