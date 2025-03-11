using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConVar;
using Network;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQBackpack", "SkuliDropek", "1.0.7")]
    [Description("Просто твой любимый рюкзачок :)")]
    class IQBackpack : RustPlugin
    {
        /// <summary>
        /// Обновление 1.0.х
        /// - Исправил перекладывание предметов в рюкзак через ПКМ/бинд
        /// - Теперь при добавления игрока в группу/выдаче группе или игроку разрешений игрок получит уведомление в чате и его интерфейс и кол-во слотов автоматически обновится
        /// - Теперь при удалении игрока из группы/удалении у группы или игрока разрешений игрок получит уведомление в чате и его интерфейс и кол-во слотов автоматически обновится (если у него предметов было больше, чем допустимое кол-во слотов - они выпадут)
        /// - Добавлена возможность включить закрытие рюкзака при повторном нажатии на UI-рюкзака или использовании бинда/команды - настраивается в конфигурационном файле
        /// - Если у игрока доступно 0 слотов рюкзака - UI более не отобразится
        /// - Если отключена возможность на улучшение рюкзака в меню крафта рюкзака не будут отображаться заблокированные слоты
        /// </summary>
        /// 

        #region Reference
        [PluginReference] Plugin ImageLibrary, IQChat, Battles, Duel;

        #region Duel / Battles
        public Boolean IsDuel(UInt64 userID)
        {
            if (Battles)
                return (Boolean)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel) return (Boolean)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else return false;
        }
        #endregion

        #region IQChat
        public void SendChat(String Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            Configuration.Reference.IQChat Chat = config.References.IQChatSetting;
            if (IQChat)
                if (Chat.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region Image Library
        private String GetImage(String fileName, UInt64 skin = 0)
        {
            var imageId = (String)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return String.Empty;
        }
        private String GetImage(Configuration.Backpack.BackpackCraft.ItemCraft CItem)
        {
            String PNG = CItem.SkinID == 0 ? GetImage($"{CItem.Shortname}_128px") : GetImage($"{CItem.Shortname}_128px_{CItem.SkinID}", CItem.SkinID);
            return PNG;
        }
        public Boolean AddImage(String url, String shortname, UInt64 skin = 0) => (Boolean)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public Boolean HasImage(String imageName) => (Boolean)ImageLibrary?.Call("HasImage", imageName);
        private IEnumerator DownloadImages()
        {
            Puts("Генерируем интерфейс...");

            if (!HasImage($"{config.BackpackItem.UrlBackpack}"))
                AddImage(config.BackpackItem.UrlBackpack, config.BackpackItem.UrlBackpack);

            foreach (Configuration.Backpack.BackpackCraft BPOption in config.BackpackItem.BackpacOption.Where(c => c.CraftItems != null && c.CraftItems.Count != 0))
                foreach (Configuration.Backpack.BackpackCraft.ItemCraft CItem in BPOption.CraftItems)
                {
                    if (CItem.SkinID != 0)
                    {
                        if (!HasImage($"{CItem.Shortname}_128px_{CItem.SkinID}"))
                            AddImage($"http://api.skyplugins.ru/api/getskin/{CItem.SkinID}/128", $"{CItem.Shortname}_128px_{CItem.SkinID}", CItem.SkinID);
                    }
                    else
                    {
                        if (!HasImage($"{CItem.Shortname}_128px"))
                            AddImage($"http://api.skyplugins.ru/api/getimage/{CItem.Shortname}/128", $"{CItem.Shortname}_128px");
                    }
                }
            yield return new WaitForSeconds(0.04f);

            Puts("Интерфейс был успешно сгенерирован!");

            _interface = new InterfaceBuilder();

            timer.Once(3f, () =>
            {
                foreach (BasePlayer player in BasePlayer.allPlayerList)
                    OnPlayerConnected(player);
            });
        }

        #endregion

        #endregion

        #region Vars
        public static IQBackpack _ = null;
        private enum TypeBackpack
        {
            Wear,
            OnlyPermission
        }
        private enum TypeDropBackpack
        {
            NoDrop,
            DropItems,
            DropBackpack,
        }
        private enum TypeDurability
        {
            None,
            Time,
            Count,
        }
        private Dictionary<BasePlayer, BackpackBehaviour> PlayerBackpack = new Dictionary<BasePlayer, BackpackBehaviour>();
        private Dictionary<BasePlayer, ShopFrontBehavior> PlayerUpgrades = new Dictionary<BasePlayer, ShopFrontBehavior>();
        private List<BasePlayer> PlayerUseBackpacks = new List<BasePlayer>();
        private Dictionary<BasePlayer, BackpackSpine> SpinesBackpacks = new Dictionary<BasePlayer, BackpackSpine>();

        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка рюкзака")]
            public Backpack BackpackItem = new Backpack();
            [JsonProperty("Настройка поддерживающих плагинов")]
            public Reference References = new Reference();

            [JsonProperty("Дополнительная настройка")]
            public Turneds TurnedsSetting = new Turneds();
            internal class Turneds
            {
                [JsonProperty("Тип работы рюкзака : 0 - требуется надеть его, чтобы пользоватья, 1 - требуются лишь права(из вариаций рюкзаков)")]
                public TypeBackpack Types = TypeBackpack.Wear;
                [JsonProperty("Отображать рюкзак за спиной игрока(модельку рюкзака, работает на всех поворотах и изгибах игрока) [ВНИМАНИЕ, ИСПОЛЬЗУЙТЕ ЭТО НА PVE СЕРВЕРЕ, ИЗ-ЗА ОСОБЕННОСТЕЙ ИГРЫ В ТАКОЙ РЮКЗАК НА СПИНЕ НЕ БУДЕТ ПРОХОДИТЬ УРОН]")]
                public Boolean UseSpineBackpack = false;
                [JsonProperty("Использовать возможность скрафтить рюкзак (true - да/false - нет)")]
                public Boolean UseCrafting = true;
                [JsonProperty("Использовать возможность улучшать рюкзаки и увеличивать в нем слоты (true - да/false - нет) [Дополнительно настраивается в каждом рюкзаке, учтите - это общий параметр, детальная настройка в вариациях рюкзаков]")]
                public Boolean UseUpgradeBackpack = true;
                [JsonProperty("Закрывать рюкзак при повторном нажатии на UI/использовании бинда, если он открыт")]
                public Boolean ClosePressedAgain = true;
                [JsonProperty("Тип выпадения рюкзака : 0 - Не выпадает при смерти, 1 - Выбрасывает предметы вокруг трупа, 2 - Выбрасывает рюкзак с предметами")]
                public TypeDropBackpack TypeDropBackpack = TypeDropBackpack.DropBackpack;
                [JsonProperty("Время удаления рюкзака при выпадении (Работает с : 2 - Выбрасывает рюкзак с предметами)")]
                public Single RemoveBackpack = 200f;
                [JsonProperty("Настройка интерфейса")]
                public VisualBackpackSlot VisualBackpackSlots = new VisualBackpackSlot();

                internal class VisualBackpackSlot
                {
                    [JsonProperty("Использовать отображение UI рюкзака возле слотов (true - да/false - нет)")]
                    public Boolean UseVisual = true;
                    [JsonProperty("Отображать количество слотов в рюкзаке на UI (true - да/false - нет)")]
                    public Boolean UseSlots = true;
                    [JsonProperty("Отображать полосу заполненности рюкзака на UI (true - да/false - нет)")]
                    public Boolean UseIsFulled = true;
                    [JsonProperty("Разрешить открывать рюкзак нажава на UI интерфейс (true - да/false - нет)")]
                    public Boolean UseButton = true;
                    [JsonProperty("Настройка цветов полосы заполненности")]
                    public ColorProgress ColorProgressBar = new ColorProgress();
                    [JsonProperty("Настройка позиции UI слота с рюкзаком")]
                    public Position PositionSlotVisual = new Position();
                    [JsonProperty("Настройка позиции UI для улучшения рюкзака")]
                    public Position PositionUpgrade = new Position();
                    internal class ColorProgress
                    {
                        [JsonProperty("Цвет полосы заполненности, когда рюкзак заполнен на >30%")]
                        public String ColorMinimal = "0.44 0.53 0.26 1.00";
                        [JsonProperty("Цвет полосы заполненности, когда рюкзак заполнен на >60%")]
                        public String ColorAverage = "0.98 0.53 0.26 1.00";
                        [JsonProperty("Цвет полосы заполненности, когда рюкзак заполнен на >80%")]
                        public String ColorMaximum = "0.98 0.20 0.28 1.00";
                    }
                    internal class Position
                    {
                        public String AnchorMin;
                        public String AnchorMax;
                        public String OffsetMin;
                        public String OffsetMax;
                    }

                }
            }
            internal class Reference
            {
                [JsonProperty("Настройка IQChat")]
                public IQChat IQChatSetting = new IQChat();
                internal class IQChat
                {
                    [JsonProperty("IQChat : Кастомный префикс в чате")]
                    public String CustomPrefix = "[IQBackpack]";
                    [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
                    public String CustomAvatar = "0";
                    [JsonProperty("IQChat : Использовать UI уведомления")]
                    public Boolean UIAlertUse = false;
                }
            }
            internal class Backpack
            {
                [JsonProperty("Shortname для рюкзака (нужен предмет, который является одеждой)")]
                public String Shortname = "burlap.gloves";     
                [JsonProperty("SkinID рюкзака")]
                public UInt64 SkinID = 2726640855; 
                [JsonProperty("Ссылка на картинку для отображения рюкзака")]
                public String UrlBackpack = "https://i.imgur.com/rPeKd9R.png"; 
                
                [JsonProperty("Вариации рюкзаков по привилегиям (Дается доступный набор игроку, который выше других)")]
                public List<BackpackCraft> BackpacOption = new List<BackpackCraft>();
                internal class BackpackCraft
                {
                    [JsonProperty("Права для возможности крафтить и носить данный рюкзак(не оставляйте это поле пустым, иначе оно не будет учитываться)")]
                    public String Permissions = "iqbackpack.7slot";
                    [JsonProperty("Количество слотов у данного рюкзака")]
                    public Int32 AmountSlot = 7;
                    [JsonProperty("Черный список предметов для данного рюкзака")]
                    public List<String> BlackListItems = new List<String>();
                    [JsonProperty("Предметы для крафта рюкзака")]
                    public List<ItemCraft> CraftItems = new List<ItemCraft>();
                    [JsonProperty("Настройка улучшений рюкзака (Улучшение будет постепенное в зависимости от листа, сверху -> вниз (максимальное количество слотов - 42))")]
                    public List<UpgradeBackpack> UpgradeList = new List<UpgradeBackpack>();
                    internal class UpgradeBackpack
                    {
                        [JsonProperty("Предметы для улучшения на этот уровень")]
                        public List<ItemCraft> CraftItems = new List<ItemCraft>();
                        [JsonProperty("Сколько слотов добавлять за это улучшение")]
                        public Int32 SlotUpgrade;
                    }
                    internal class ItemCraft
                    {
                        public String Shortname;
                        public UInt64 SkinID;
                        [JsonProperty("Количество")]
                        public Int32 Amount;
                    }
                }

            }
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    BackpackItem = new Backpack
                    {
                        Shortname = "burlap.gloves",
                        SkinID = 2726640855,
                        UrlBackpack = "https://i.imgur.com/rPeKd9R.png",

                        BackpacOption = new List<Backpack.BackpackCraft>
                        {
                            new Backpack.BackpackCraft
                            {
                                AmountSlot = 7,
                                Permissions = "iqbackpack.7slot",
                                BlackListItems = new List<String> { },
                                CraftItems = new List<Backpack.BackpackCraft.ItemCraft>{ },
                                UpgradeList = new List<Backpack.BackpackCraft.UpgradeBackpack> {  },
                            },
                            new Backpack.BackpackCraft
                            {
                                AmountSlot = 15,
                                Permissions = "iqbackpack.15slot",
                                BlackListItems = new List<String>
                                {
                                    "rocket.launcher",
                                    "ammo.rocket.basic",
                                    "explosive.satchel",
                                    "supply.signal",
                                    "explosive.timed",
                                },
                                CraftItems = new List<Backpack.BackpackCraft.ItemCraft>
                                {
                                    new Backpack.BackpackCraft.ItemCraft
                                    {
                                        Shortname = "leather",
                                        Amount = 50,
                                        SkinID = 0
                                    },
                                    new Backpack.BackpackCraft.ItemCraft
                                    {
                                        Shortname = "cloth",
                                        Amount = 200,
                                        SkinID = 0
                                    },
                                    new Backpack.BackpackCraft.ItemCraft
                                    {
                                        Shortname = "sewingkit",
                                        Amount = 10,
                                        SkinID = 0
                                    },
                                },
                                UpgradeList = new List<Backpack.BackpackCraft.UpgradeBackpack>
                                {
                                    new Backpack.BackpackCraft.UpgradeBackpack
                                    {
                                        SlotUpgrade = 3,
                                        CraftItems = new List<Backpack.BackpackCraft.ItemCraft>
                                        {
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "burlap.gloves",
                                                Amount = 1,
                                                SkinID = 2726640855
                                            },
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "leather",
                                                Amount = 150,
                                                SkinID = 0
                                            },
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "cloth",
                                                Amount = 300,
                                                SkinID = 0
                                            },
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "sewingkit",
                                                Amount = 5,
                                                SkinID = 0
                                            },
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "metal.fragments",
                                                Amount = 1000,
                                                SkinID = 0
                                            },
                                        }
                                    },
                                    new Backpack.BackpackCraft.UpgradeBackpack
                                    {
                                        SlotUpgrade = 10,
                                        CraftItems = new List<Backpack.BackpackCraft.ItemCraft>
                                        {
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "burlap.gloves",
                                                Amount = 1,
                                                SkinID = 2726640855
                                            },
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "leather",
                                                Amount = 500,
                                                SkinID = 0
                                            },
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "cloth",
                                                Amount = 1000,
                                                SkinID = 0
                                            },
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "sewingkit",
                                                Amount = 25,
                                                SkinID = 0
                                            },
                                            new Backpack.BackpackCraft.ItemCraft
                                            {
                                                Shortname = "metal.fragments",
                                                Amount = 15000,
                                                SkinID = 0
                                            },
                                        }
                                    },
                                }
                            }
                        }
                    },
                    TurnedsSetting = new Turneds
                    {
                        Types = TypeBackpack.Wear,
                        UseCrafting = true,
                        UseUpgradeBackpack = true,
                        ClosePressedAgain = true,
                        UseSpineBackpack = false,
                        TypeDropBackpack = TypeDropBackpack.DropBackpack,
                        RemoveBackpack = 200f,
                        VisualBackpackSlots = new Turneds.VisualBackpackSlot
                        {
                            UseVisual = true,
                            UseSlots = true,
                            UseIsFulled = true,
                            UseButton = true,
                            ColorProgressBar = new Turneds.VisualBackpackSlot.ColorProgress
                            {
                                ColorMinimal = "0.44 0.53 0.26 1.00",
                                ColorAverage = "0.98 0.53 0.26 1.00",
                                ColorMaximum = "0.98 0.20 0.28 1.00",
                            },
                            PositionSlotVisual = new Turneds.VisualBackpackSlot.Position
                            {
                                AnchorMin = "0.5 0",
                                AnchorMax = "0.5 0",
                                OffsetMin = "-264.276 17.943",
                                OffsetMax = "-203.724 78.087"
                            },
                            PositionUpgrade = new Turneds.VisualBackpackSlot.Position
                            {
                                AnchorMin = "0.5 0.5", 
                                AnchorMax = "0.5 0.5",
                                OffsetMin = "194.836 -36.769",
                                OffsetMax = "575.164 36.785"
                            }
                        },
                    },
                    References = new Reference
                    {
                        IQChatSetting = new Reference.IQChat
                        {
                            CustomAvatar = "0",
                            CustomPrefix = "[IQBackpack] ",
                            UIAlertUse = false,
                        }
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
                PrintWarning("Ошибка " + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data

        public Dictionary<UInt64, BackpackInfo> Backpacks = new Dictionary<UInt64, BackpackInfo>();
        internal class BackpackInfo
        {
            public Int32 AmountSlot = 0;
            public Int32 IndexUpgrade = -1;
            public List<SavedItem> Items = new List<SavedItem>();

            internal class SavedItem
            {
                public Int32 TargetSlot;
                public String Shortname;
                public Int32 Itemid;
                public Single Condition;
                public Single Maxcondition;
                public Int32 Amount;
                public Int32 Ammoamount;
                public String Ammotype;
                public Int32 Flamefuel;
                public UInt64 Skinid;
                public String Name;
                public Boolean Weapon;
                public Int32 Blueprint;
                public Single BusyTime;
                public Boolean OnFire;
                public List<SavedItem> Mods;
            }
        }
        void ReadData() => Backpacks = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, BackpackInfo>>("IQBackpack/Backpacks");
        void WriteData() => Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQBackpack/Backpacks", Backpacks);

        #endregion

        #region Hooks
        void Init() => ReadData();
        void OnServerInitialized()
        {
            _ = this;
            ServerMgr.Instance.StartCoroutine(DownloadImages());

            RegisteredPermissions();

            if(config.TurnedsSetting.TypeDropBackpack == TypeDropBackpack.NoDrop)
                Unsubscribe("OnPlayerDeath");

            if (config.TurnedsSetting.Types == TypeBackpack.OnlyPermission)
                Unsubscribe("CanWearItem");
            else
            {
                Unsubscribe("OnUserPermissionGranted");
                Unsubscribe("OnUserPermissionRevoked");
                Unsubscribe("OnUserGroupAdded");
                Unsubscribe("OnUserGroupRemoved");
            }

            if (!config.TurnedsSetting.UseUpgradeBackpack)
            {
                Unsubscribe("CanLootEntity");
                Unsubscribe("OnItemStacked");
                Unsubscribe("OnItemSplit");
                Unsubscribe("CanStackItem");
            }
            if (!config.TurnedsSetting.UseSpineBackpack)
                Unsubscribe("OnPlayerDisconnected");
        }
        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null) return;
            DropBackpack(player, config.TurnedsSetting.TypeDropBackpack);
            return;
        }
        void OnPlayerConnected(BasePlayer player)
        {
            if (_interface == null)
            {
                timer.Once(3f, () => OnPlayerConnected(player));
                return;
            }
            if (player == null) return;

            if (!PlayerBackpack.ContainsKey(player))
                PlayerBackpack.Add(player, null);

            if (config.TurnedsSetting.UseUpgradeBackpack)
                if (!PlayerUpgrades.ContainsKey(player))
                    PlayerUpgrades.Add(player, null);

            if (!config.TurnedsSetting.VisualBackpackSlots.UseVisual) return;

            if (config.TurnedsSetting.Types == TypeBackpack.Wear) { 
                if (GetBackpack(player) == null) return;
            }
            else {
                if (!Backpacks.ContainsKey(player.userID))
                    Backpacks.Add(player.userID, new BackpackInfo 
                    {
                        AmountSlot = GetAvailableSlots(player)
                    });
            }

            if (!player.IsDead() && !IsDuel(player.userID))
            {
                DrawUI_Backpack_Visual(player);
                BackpackSpawnSpine(player);
            }
        }
        void OnPlayerDisconnected(BasePlayer player, string reason) => BackpackRemoveSpine(player);
        private void OnServerShutdown() => Unload();

        void Unload()
        {
            ServerMgr.Instance.StopCoroutine(DownloadImages());
            InterfaceBuilder.DestroyAll();
            WriteData();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                foreach (Item item in player.inventory.AllItems())
                    item.SetFlag(global::Item.Flag.IsLocked, false);

            foreach (BasePlayer player in BasePlayer.activePlayerList.Where(p => PlayerBackpack.ContainsKey(p) && PlayerBackpack[p] != null))
                PlayerBackpack[player].Destroy();

            if (config.TurnedsSetting.UseUpgradeBackpack)
                foreach (BasePlayer player in BasePlayer.activePlayerList.Where(p => PlayerUpgrades.ContainsKey(p) && PlayerUpgrades[p] != null))
                    PlayerUpgrades[player].Destroy();

            if (SpinesBackpacks != null && SpinesBackpacks.Count != 0)
                foreach (KeyValuePair<BasePlayer, BackpackSpine> sBp in SpinesBackpacks.Where(bp => bp.Value != null))
                    sBp.Value.KillParent();

            SpinesBackpacks.Clear();

            _ = null;
        }

        #region Backpack Hooks
        public Dictionary<UInt64, List<Connection>> ContainerGetPlayer = new Dictionary<UInt64, List<Connection>>();
        void NetworkIDGetSet(UInt64 NetID, Boolean SetOrRemove, BasePlayer player = null)
        {
            if (player == null) return;
            if (!ContainerGetPlayer.ContainsKey(NetID))
                ContainerGetPlayer.Add(NetID, new List<Connection> { player.Connection });
            else
            {
                if (SetOrRemove)
                {
                    if (!ContainerGetPlayer[NetID].Contains(player.Connection))
                        ContainerGetPlayer[NetID].Add(player.Connection);
                }
                else
                {
                    if (ContainerGetPlayer[NetID].Count <= 1)
                        ContainerGetPlayer.Remove(NetID);
                    else ContainerGetPlayer[NetID].Remove(player.Connection);
                }
            }
        }

        object OnLootNetworkUpdate(PlayerLoot loot)
        {
            if (loot == null)
                return null;
            BasePlayer player = loot.GetComponent<BasePlayer>();
            if (player == null)
                return null;
            if (loot.entitySource == null || loot.entitySource.net == null)
                return null;
            UInt64 NetID = loot.entitySource.net.ID;
            NetworkIDGetSet(NetID, true, player);
            return null;
        }
        void CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container == null || player == null || !(Boolean)(container is Workbench)) return;
            DrawUI_Backpack_Upgrade_Workbench(player);
            return;
        }

        void OnPlayerSleepEnded(BasePlayer player) => OnPlayerConnected(player);
        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (entity == null || player == null) return;

            Item backpack = GetBackpack(player);

            if (config.TurnedsSetting.UseUpgradeBackpack)
            {
                if (entity is Workbench)
                {
                    CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Upgrade_Workbench);
                    CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Upgrade_Info);
                    return;
                }
                if (entity is ShopFront)
                {
                    ShopFrontBehavior upgradeHandler = null;
                    if (PlayerUpgrades.ContainsKey(player) && PlayerUpgrades[player] != null)
                        upgradeHandler = PlayerUpgrades[player];

                    if (upgradeHandler != null)
                        upgradeHandler.Close();

                    CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Upgrade_Workbench);
                    CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Upgrade_Info);

                    if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                        if (backpack == null) return;

                    DrawUI_Backpack_Visual(player);
                    BackpackSpawnSpine(player);
                    return;
                }

                UInt64 NetID = entity.net.ID;
                NetworkIDGetSet(NetID, false, player);
            }

            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                if (backpack == null) return;

            BackpackBehaviour backpackHandler = null;
            if (PlayerBackpack.ContainsKey(player) && PlayerBackpack[player] != null)
                backpackHandler = PlayerBackpack[player];

            StorageContainer storage = entity as StorageContainer;

            if (player != null && storage != null && backpackHandler != null && storage == backpackHandler.Container)
            {
                backpackHandler.Close();
                DrawUI_Backpack_Visual(player);
                BackpackSpawnSpine(player);
            }
        }

        object CanWearItem(PlayerInventory inventory, Item item, int targetSlot)
        {
            if (inventory == null || item == null) return null;
            BasePlayer player = inventory.gameObject.ToBaseEntity() as BasePlayer;

            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                if (player != null && item.skin == config.BackpackItem.SkinID)
                {
                    if (!Backpacks.ContainsKey(item.uid) && item.skin == config.BackpackItem.SkinID)
                        Backpacks.Add(item.uid, new BackpackInfo());

                    if (GetBackpack(player) != null) return false;
                    NextTick(() =>
                    {
                        DrawUI_Backpack_Visual(player);
                        BackpackSpawnSpine(player);
                        if (player?.inventory?.loot?.entitySource is Workbench)
                            DrawUI_Backpack_Upgrade_Workbench(player);
                    });
                }

            return null;
        }
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (item == null || container == null) return;
            BasePlayer player = container.playerOwner;

            if (player == null || player is ScientistNPC || player is HumanNPC || player is NPCPlayer) return;

            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                if (item.skin == config.BackpackItem.SkinID)
                {
                    if (GetBackpack(player) == null)
                    {
                        CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
                        BackpackRemoveSpine(player);
                    }
                }

            if (config.TurnedsSetting.UseUpgradeBackpack)
            {
                if (container?.entityOwner is ShopFront || player?.inventory?.loot?.entitySource is ShopFront)
                {
                    if (PlayerUpgrades.ContainsKey(player) && (container.playerOwner == player || container.uid == (UInt32)player.userID))
                        if (PlayerUpgrades[player] != null)
                        {
                            PlayerUpgrades[player].VendorLock(true);
                            DrawUI_Backpack_Upgrade_Info_Controller(player);
                        }
                    return;
                }
                if (player?.inventory?.loot?.entitySource is Workbench)
                {
                    DrawUI_Backpack_Upgrade_Workbench(player);
                    return;
                }
            }
        }
        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item == null || entity == null) return;
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null || player is ScientistNPC || player is HumanNPC || player is NPCPlayer) return;

            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                if (item.skin == config.BackpackItem.SkinID)
                {
                    NextTick(() =>
                    {
                        if (GetBackpack(player) == null)
                        {
                            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
                            BackpackRemoveSpine(player);
                        }
                    });
                    return;
                }

            if (config.TurnedsSetting.UseUpgradeBackpack)
            {
                if ((Boolean)(item?.GetRootContainer()?.entityOwner is ShopFront))
                {
                    if (PlayerUpgrades.ContainsKey(player))
                    {
                        NextTick(() =>
                        {
                            if (PlayerUpgrades[player] != null && PlayerUpgrades[player].Container.vendorInventory != null && PlayerUpgrades[player].Container.customerInventory != null)
                                PlayerUpgrades[player].VendorLock(true);
                            DrawUI_Backpack_Upgrade_Info_Controller(player);
                        });
                    }
                    return;
                }
                if (player?.inventory?.loot?.entitySource is Workbench)
                {
                    NextTick(() => { DrawUI_Backpack_Upgrade_Workbench(player); });
                    return;
                }
            }
        }
        void OnItemStacked(Item destinationItem, Item sourceItem, ItemContainer destinationContainer)
        {
            if (destinationItem == null || sourceItem == null || destinationContainer == null) return;
            BasePlayer player = destinationContainer.playerOwner;
            if (player == null || player is ScientistNPC || player is HumanNPC || player is NPCPlayer) return;

            if (PlayerUpgrades.ContainsKey(player))
            {
                if (PlayerUpgrades[player] != null && PlayerUpgrades[player].Container.vendorInventory != null && PlayerUpgrades[player].Container.customerInventory != null)
                    PlayerUpgrades[player].VendorLock(true);

                DrawUI_Backpack_Upgrade_Info_Controller(player);
            }
        }
        private Item OnItemSplit(Item item, int amount)
        {
            if (item == null) return null;
            if (plugins.Find("Stacks") || plugins.Find("CustomSkinsStacksFix") || plugins.Find("SkinBox")) return null;
            if (item.IsLocked())
            {
                Item x = ItemManager.CreateByPartialName(item.info.shortname, amount);
                x.name = item.name;
                x.skin = item.skin;
                x.amount = amount;
                x.SetFlag(global::Item.Flag.IsLocked, true);
                item.amount -= amount;
                return x;
            }
            return null;
        }

        object CanStackItem(Item item, Item targetItem)
        {
            if (item == null || targetItem == null) return null;
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null || player is ScientistNPC || player is HumanNPC || player is NPCPlayer) return null;
            if (PlayerUpgrades.ContainsKey(player))
            {
                NextTick(() =>
                {
                    if (PlayerUpgrades[player] != null && PlayerUpgrades[player].Container.vendorInventory != null && PlayerUpgrades[player].Container.customerInventory != null)
                        PlayerUpgrades[player].VendorLock(true);
                    DrawUI_Backpack_Upgrade_Info_Controller(player);
                });
            }
            return null;
        }
        object CanAcceptItem(ItemContainer container, Item item)
        {
            if (container == null || item == null) return null;
            BasePlayer player = container.playerOwner;
            if (player == null)
            {
                if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                {
                    NextTick(() =>
                    {
                        //// ERROR ???
                        if (item.parent == null || item.parent.entityOwner == null || item.parent.entityOwner.net == null) return;
                        if (item.skin != config.BackpackItem.SkinID) return;
                        UInt64 NetID = item.parent.entityOwner.net.ID;
                        if (NetID == 0) return;
                        if (ContainerGetPlayer.ContainsKey(NetID))
                            CommunityEntity.ServerInstance.ClientRPCEx(new SendInfo(ContainerGetPlayer[NetID]), null, "DestroyUI", "UI_BACKPACK_VISUAL");
                    });
                }
                return null;
            }
            if (player is ScientistNPC || player is HumanNPC || player is NPCPlayer) return null;
            if (!PlayerUseBackpacks.Contains(player)) return null;

            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                if (item.skin == config.BackpackItem.SkinID)
                {
                    if (item.IsLocked())
                        return ItemContainer.CanAcceptResult.CannotAccept;

                    CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
                    BackpackRemoveSpine(player);
                    return null;
                }
            if (GetBackpackOption(player).BlackListItems.Contains(item.info.shortname) && item.IsLocked())
                return ItemContainer.CanAcceptResult.CannotAccept;

            return null;
        }


        #endregion

        #region Permissions Hooks
        
        void OnUserPermissionGranted(string id, string permName) => UpdatePermissions(id, permName, true);
        void OnUserPermissionRevoked(string id, string permName) => UpdatePermissions(id, permName, false);

        void OnUserGroupAdded(string id, string groupName)
        {
            String[] PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;

            foreach (var Option in config.BackpackItem.BackpacOption.Where(x => PermissionsGroup.Contains(x.Permissions)))
                UpdatePermissions(id, Option.Permissions, true);
        }

        void OnUserGroupRemoved(string id, string groupName)
        {
            String[] PermissionsGroup = permission.GetGroupPermissions(groupName);
            if (PermissionsGroup == null) return;

            foreach (var Option in config.BackpackItem.BackpacOption.Where(x => PermissionsGroup.Contains(x.Permissions)))
                UpdatePermissions(id, Option.Permissions, false);
        }
        void OnGroupPermissionGranted(string name, string perm)
        {
            String[] GroupUser = permission.GetUsersInGroup(name);
            if (GroupUser == null) return;

            foreach(String IDs in GroupUser)
                UpdatePermissions(IDs.Substring(0,17), perm, true);
        }
        void OnGroupPermissionRevoked(string name, string perm)
        {
            String[] GroupUser = permission.GetUsersInGroup(name);
            if (GroupUser == null) return;

            foreach (String IDs in GroupUser)
                UpdatePermissions(IDs.Substring(0, 17), perm, false);
        }
        #endregion

        #endregion

        #region Metods

        #region Backpack Permissions
        private void UpdatePermissions(String ID, String Permissions, Boolean IsGranted)
        {          
            UInt64 UserID = UInt64.Parse(ID);
            BasePlayer player = BasePlayer.FindByID(UserID);
            if(player == null) return;

            UInt64 IDBackpack = GetBackpackID(player);
            if (!Backpacks.ContainsKey(IDBackpack)) return;
            player.EndLooting();

            Int32 AvailableSlots = GetAvailableSlots(player);
            if (AvailableSlots < GetBusySlotsBackpack(player))
            {
                Int32 Count = Backpacks[IDBackpack].Items.Count - 1;
                foreach (BackpackInfo.SavedItem Sitem in Backpacks[IDBackpack].Items.Take((Backpacks[IDBackpack].Items.Count - AvailableSlots)))
                {
                    NextTick(() =>
                    {
                        Item itemDrop = BuildItem(Sitem);
                        itemDrop.DropAndTossUpwards(player.transform.position, 2f);

                        Backpacks[IDBackpack].Items.RemoveAt(Count);
                        Count--;
                    });
                }
            }
            Backpacks[IDBackpack].AmountSlot = AvailableSlots;
            
            NextTick(() => { 
                DrawUI_Backpack_Visual(player);
                SendChat(GetLang((IsGranted ? "BACKPACK_GRANT" : "BACKPACK_REVOKE"), player.UserIDString, AvailableSlots), player);
            });
        }
        #endregion

        #region Backpack Action
        private void OpenBP(BasePlayer player)
        {
            if (IsDuel(player.userID)) return;
            Item backpack = GetBackpack(player);

            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                if (backpack == null)
                {
                    SendChat(GetLang("BACKPACK_NO_WEARING", player.UserIDString), player);
                    return;
                }

            BackpackBehaviour backpackHandler = null;
            if (PlayerBackpack.ContainsKey(player))
            {
                if (PlayerBackpack[player] != null)
                    backpackHandler = PlayerBackpack[player];
            }
            else PlayerBackpack.Add(player, null);
            if (backpackHandler == null)
            {
                backpackHandler = player.gameObject.AddComponent<BackpackBehaviour>();
                backpackHandler.Backpack = backpack;

                PlayerBackpack[player] = backpackHandler;
            }
            if (backpackHandler.Container != null)
            {
                if (config.TurnedsSetting.ClosePressedAgain)
                    player.EndLooting();
                else SendChat(GetLang("BACKPACK_IS_OPENED", player.UserIDString), player);
                return;
            }
            backpackHandler.Open();
        }

        private void BackpackSpawnSpine(BasePlayer player)
        {
            if (!config.TurnedsSetting.UseSpineBackpack) return;
            if (SpinesBackpacks.ContainsKey(player) && SpinesBackpacks[player] != null) return;
            BackpackSpine backpackSpine = player.gameObject.AddComponent<BackpackSpine>();
            SpinesBackpacks.Add(player,backpackSpine);
        }
        private void BackpackRemoveSpine(BasePlayer player)
        {
            if (!config.TurnedsSetting.UseSpineBackpack) return;
            if (!SpinesBackpacks.ContainsKey(player) || SpinesBackpacks[player] == null) return;

            SpinesBackpacks[player].KillParent();
            SpinesBackpacks.Remove(player);
        }

        #endregion

        #region Generate Backpack

        public static StorageContainer CreateContainer(BasePlayer player)
        {
            StorageContainer storage = GameManager.server.CreateEntity("assets/prefabs/misc/halloween/coffin/coffinstorage.prefab") as StorageContainer;
            if (storage == null) return null;

            var containerEntity = storage as StorageContainer;
            if (containerEntity == null)
            {
                UnityEngine.Object.Destroy(storage);
                return null;
            }
            UnityEngine.Object.DestroyImmediate(storage.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(storage.GetComponent<GroundWatch>());

            foreach (var collider in storage.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(collider);

            storage.transform.position = new Vector3(player.ServerPosition.x, player.ServerPosition.y - 100f, player.ServerPosition.z);
            storage.panelName = "generic_resizable";

            ItemContainer container = new ItemContainer { playerOwner = player };
            container.ServerInitialize((Item)null, _.GetSlotsBackpack(player));
            if ((Int32)container.uid == 0)
                container.GiveUID();

            storage.inventory = container;
            storage.OwnerID = player.userID;

            storage._limitedNetworking = false;
            storage.EnableSaving(false);

            storage.SendMessage("SetDeployedBy", player, (SendMessageOptions)SendMessageOptions.DontRequireReceiver);
            storage.Spawn();

            storage.inventory.allowedContents = ItemContainer.ContentsType.Generic;
            return storage;
        }
       
        private static void PlayerLootContainer(BasePlayer player, StorageContainer container)
        {
            container.SetFlag(BaseEntity.Flags.Open, true, false);
            player.inventory.loot.StartLootingEntity(container, false);
            player.inventory.loot.AddContainer(container.inventory);
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic_resizable");
            container.SendNetworkUpdate();
        }

        #endregion

        #region Upgrade Backpack

        #region Generate Shop Front
        public static ShopFront CreateShopFront(BasePlayer player)
        {
            ShopFront shopFront = GameManager.server.CreateEntity("assets/prefabs/building/wall.frame.shopfront/wall.frame.shopfront.metal.prefab") as ShopFront;
            if (shopFront == null) return null;

            shopFront.transform.position = new Vector3(player.ServerPosition.x, player.ServerPosition.y + 100f, player.ServerPosition.z);
            shopFront.panelName = "shopfront";

            if (!shopFront) return null;

            UnityEngine.Object.DestroyImmediate(shopFront.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(shopFront.GetComponent<GroundWatch>());

            foreach (var collider in shopFront.GetComponentsInChildren<Collider>())
                UnityEngine.Object.DestroyImmediate(collider);

            shopFront._limitedNetworking = true;
            shopFront.EnableSaving(false);

            shopFront.SendMessage("SetDeployedBy", player, (SendMessageOptions)SendMessageOptions.DontRequireReceiver);
            shopFront.Spawn();

            return shopFront;
        }

        private static void PlayerLootContainer(BasePlayer player, ShopFront shopFront, List<Configuration.Backpack.BackpackCraft.ItemCraft> ItemList)
        {
            shopFront.SetFlag(BaseEntity.Flags.Open, true, false);

            shopFront.vendorInventory.capacity = 12;
            shopFront.customerInventory.capacity = 12;

            shopFront.customerPlayer = player;
            shopFront.customerInventory.playerOwner = player;
            shopFront.customerInventory.uid = (UInt32)player.userID;

            foreach (Configuration.Backpack.BackpackCraft.ItemCraft UpgradeItems in ItemList)
            {
                Item item = ItemManager.CreateByName(UpgradeItems.Shortname, UpgradeItems.Amount, UpgradeItems.SkinID);
                item.MoveToContainer(shopFront.vendorInventory);
            }

            player.inventory.loot.StartLootingEntity(shopFront, false);
            player.inventory.loot.AddContainer(shopFront.vendorInventory);
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "shopfront");
            player.inventory.loot.AddContainer(shopFront.customerInventory);
            player.inventory.loot.SendImmediate();

            shopFront.UpdatePlayers();
            shopFront.DecayTouch();
            shopFront.SendNetworkUpdate();
        }
        #endregion

        private void OpenUpgradeMenu(BasePlayer player)
        {
            if (!config.TurnedsSetting.UseUpgradeBackpack) return;
            if (player == null) return;

            ShopFrontBehavior upgradeHandler = null;
            if (PlayerBackpack.ContainsKey(player))
            {
                if (PlayerBackpack[player] != null)
                    upgradeHandler = PlayerUpgrades[player];
            }
            else PlayerBackpack.Add(player, null);

            if (upgradeHandler == null)
            {
                upgradeHandler = player.gameObject.AddComponent<ShopFrontBehavior>();
                PlayerUpgrades[player] = upgradeHandler;
            }
            if (upgradeHandler.Container != null) return;

            upgradeHandler.Open();
        }

        private void UpgradeBackpack(BasePlayer player)
        {
            if (player == null) return;
            if (!PlayerUpgrades.ContainsKey(player)) return;
            if (!HaveAllItem(player, PlayerUpgrades[player].ItemList, PlayerUpgrades[player].Container.customerInventory)) return;
            Configuration.Backpack.BackpackCraft Option = GetBackpackOption(player);
            if (Option == null) return;
            UInt64 ID = PlayerUpgrades[player].BackpackID;
            Int32 IndexUpgrade = Backpacks[ID].IndexUpgrade;
            if (Option.UpgradeList[IndexUpgrade + 1] == null) return;

            PlayerUpgrades[player].Container.customerInventory.SetLocked(true);

            TakeItems(player, PlayerUpgrades[player].ItemList, PlayerUpgrades[player].Container.customerInventory);

            if(PlayerUpgrades[player].ItemList.Count(x => x.SkinID == config.BackpackItem.SkinID) != 0)
            {
                Item backpack = ItemManager.CreateByName(config.BackpackItem.Shortname, 1, config.BackpackItem.SkinID);
                Int32 Slot = Backpacks[ID].AmountSlot + Option.UpgradeList[IndexUpgrade + 1].SlotUpgrade;

                backpack.name = GetLang("BACKPACK_TITLE", player.UserIDString, Slot);

                Backpacks.Add(backpack.uid, new BackpackInfo()
                {
                    AmountSlot = Slot,
                    IndexUpgrade = IndexUpgrade + 1,
                    Items = Backpacks[ID].Items
                });

                player.GiveItem(backpack);
                Backpacks.Remove(ID);
            }
            else
            {
                Backpacks[ID].IndexUpgrade++;
                Backpacks[ID].AmountSlot += PlayerUpgrades[player].UpgradeSlotsUp;

                if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                {
                    Item BackpackItem = GetBackpack(player);
                    if (BackpackItem != null)
                    {
                        BackpackItem.name = GetLang("BACKPACK_TITLE", player.UserIDString, Backpacks[ID].AmountSlot);
                        player.SendNetworkUpdate();
                        BackpackItem.MarkDirty();
                    }
                    //else
                    //{
                    //    Item itemBackpack = player.inventory.AllItems().FirstOrDefault(x => x.skin == config.BackpackItem.SkinID && x.info.shortname == config.BackpackItem.Shortname);
                    //    if(itemBackpack != null)
                    //    {
                    //        itemBackpack.name = GetLang("BACKPACK_TITLE", player.UserIDString, Backpacks[ID].AmountSlot);
                    //        player.SendNetworkUpdate();
                    //        itemBackpack.MarkDirty();
                    //    }
                    //}
                }
            }

            PlayerUpgrades[player].Container.PlayerStoppedLooting(player);
        }


        #endregion

        #region Saved Items
        private List<BackpackInfo.SavedItem> GetSavedList(UInt64 ID)
        {
            List<BackpackInfo.SavedItem> SavedList = null;
            if (Backpacks.ContainsKey(ID))
                SavedList = Backpacks[ID].Items;

            return SavedList;
        }
        static List<BackpackInfo.SavedItem> SaveItems(List<Item> items) => items.Select(SaveItem).ToList();
        static BackpackInfo.SavedItem SaveItem(Item item)
        {
            BackpackInfo.SavedItem iItem = new BackpackInfo.SavedItem
            {
                TargetSlot = item.position,
                Shortname = item.info?.shortname,
                Amount = item.amount,
                Mods = new List<BackpackInfo.SavedItem>(),
                Skinid = item.skin,
                BusyTime = item.busyTime,

            };
            if (item.HasFlag(global::Item.Flag.OnFire))
            {
                iItem.OnFire = true;
            }
            if (item.info == null) return iItem;
            iItem.Itemid = item.info.itemid;
            iItem.Weapon = false;

            if (item.contents != null && item.info.category.ToString() != "Weapon")
            {
                foreach (var itemCont in item.contents.itemList)
                {
                    Debug.Log(itemCont.info.shortname);

                    if (itemCont.info.itemid != 0)
                        iItem.Mods.Add(SaveItem(itemCont));
                }
            }

            iItem.Name = item.name;
            if (item.hasCondition)
            {
                iItem.Condition = item.condition;
                iItem.Maxcondition = item.maxCondition;
            }

            if (item.blueprintTarget != 0) iItem.Blueprint = item.blueprintTarget;

            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower != null)
                iItem.Flamefuel = flameThrower.ammo;
            if (item.info.category.ToString() != "Weapon") return iItem;
            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon == null) return iItem;
            if (weapon.primaryMagazine == null) return iItem;
            iItem.Ammoamount = weapon.primaryMagazine.contents;
            iItem.Ammotype = weapon.primaryMagazine.ammoType.shortname;
            iItem.Weapon = true;

            if (item.contents != null)
                foreach (var mod in item.contents.itemList)
                    if (mod.info.itemid != 0)
                        iItem.Mods.Add(SaveItem(mod));
            return iItem;
        }
        static Item BuildItem(BackpackInfo.SavedItem sItem)
        {
            if (sItem.Amount < 1) sItem.Amount = 799 > 0 ? 1 : 0;
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.Itemid, sItem.Amount, sItem.Skinid);
            item.position = sItem.TargetSlot;

            if (item.hasCondition)
            {
                item.condition = sItem.Condition;
                item.maxCondition = sItem.Maxcondition;
                item.busyTime = sItem.BusyTime;
            }

            if (sItem.Blueprint != 0)
                item.blueprintTarget = sItem.Blueprint;

            if (sItem.Mods != null)
            {
                if (sItem.Mods != null)
                    foreach (var mod in sItem.Mods)
                        item.contents.AddItem(BuildItem(mod).info, mod.Amount);
            }

            if (sItem.Name != null)
                item.name = sItem.Name;

            if (sItem.OnFire)
                item.SetFlag(global::Item.Flag.OnFire, true);

            FlameThrower flameThrower = item.GetHeldEntity()?.GetComponent<FlameThrower>();
            if (flameThrower)
                flameThrower.ammo = sItem.Flamefuel;
            return item;
        }
        static Item BuildWeapon(BackpackInfo.SavedItem sItem)
        {
            Item item = null;
            item = ItemManager.CreateByItemID(sItem.Itemid, 1, sItem.Skinid);
            item.position = sItem.TargetSlot;

            if (item.hasCondition)
            {
                item.condition = sItem.Condition;
                item.maxCondition = sItem.Maxcondition;
            }

            if (sItem.Blueprint != 0)
                item.blueprintTarget = sItem.Blueprint;

            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                var def = ItemManager.FindItemDefinition(sItem.Ammotype);
                weapon.primaryMagazine.ammoType = def;
                weapon.primaryMagazine.contents = sItem.Ammoamount;
            }

            if (sItem.Mods != null)
                foreach (var mod in sItem.Mods)
                    item.contents.AddItem(BuildItem(mod).info, 1);
            return item;
        }
        static List<Item> RestoreItems(List<BackpackInfo.SavedItem> sItems)
        {
            return sItems.Select(sItem =>
            {
                if (sItem.Weapon) return BuildWeapon(sItem);
                return BuildItem(sItem);
            }).Where(i => i != null).ToList();
        }
        #endregion

        #region Drop Backpack
        private void DropBackpack(BasePlayer player, TypeDropBackpack typeDropBackpack)
        {
            if (!PlayerBackpack.ContainsKey(player)) return;
            if (PlayerBackpack[player] != null)
                PlayerBackpack[player].Close();
            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);
            BackpackRemoveSpine(player);
            UInt64 ID = GetBackpackID(player);
            Item Backpack = GetBackpack(player);
            List<BackpackInfo.SavedItem> SavedList = GetSavedList(ID);
            if (SavedList == null || SavedList.Count == 0) return;
            switch (typeDropBackpack)
            {
                case TypeDropBackpack.DropItems:
                    {
                        foreach(BackpackInfo.SavedItem sItem in SavedList)
                        {
                            Item BuildedItem = BuildItem(sItem);
                            BuildedItem.DropAndTossUpwards(player.transform.position, Oxide.Core.Random.Range(2, 6));
                        }
                        break;
                    }
                case TypeDropBackpack.DropBackpack:
                    {
                        String Prefab = "assets/prefabs/misc/item drop/item_drop_backpack.prefab";
                        DroppedItemContainer BackpackDrop = (BaseEntity)GameManager.server.CreateEntity(Prefab, player.transform.position + new Vector3(Oxide.Core.Random.Range(-1f, 1f),0f,0f)) as DroppedItemContainer;
                        BackpackDrop.gameObject.AddComponent<NoRagdollCollision>();

                        BackpackDrop.lootPanelName = "generic_resizable";
                        BackpackDrop.playerName = $"{player.displayName ?? "Somebody"}'s Backpack";
                        BackpackDrop.playerSteamID = player.userID;

                        BackpackDrop.inventory = new ItemContainer();
                        BackpackDrop.inventory.ServerInitialize(null, GetSlotsBackpack(player));
                        BackpackDrop.inventory.GiveUID();
                        BackpackDrop.inventory.entityOwner = BackpackDrop;
                        BackpackDrop.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);

                        foreach (BackpackInfo.SavedItem sItem in SavedList)
                        {
                            Item BuildedItem = BuildItem(sItem);
                            BuildedItem.MoveToContainer(BackpackDrop.inventory, sItem.TargetSlot); 
                        }

                        BackpackDrop.SendNetworkUpdate();
                        BackpackDrop.Spawn();
                        BackpackDrop.ResetRemovalTime(Math.Max(config.TurnedsSetting.RemoveBackpack, BackpackDrop.CalculateRemovalTime()));
                        break;
                    }
                default:
                    break;
            }
            SavedList.Clear();
            if (Backpack != null)
                Backpack.Remove();
            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
                Backpacks.Remove(ID);
        }
        private class NoRagdollCollision : FacepunchBehaviour
        {
            private Collider _collider;

            private void Awake()
            {
                _collider = GetComponent<Collider>();
            }

            private void OnCollisionEnter(Collision collision)
            {
                if (collision.collider.IsOnLayer(Rust.Layer.Ragdoll))
                {
                    UnityEngine.Physics.IgnoreCollision(_collider, collision.collider);
                }
            }
        }
        #endregion

        #region Craft Backpack

        private void CraftingBackpack(BasePlayer player)
        {
            Configuration.Backpack.BackpackCraft BCraft = GetBackpackOption(player);
            if (BCraft == null || BCraft.CraftItems == null) return;

            if (!HaveAllItem(player, BCraft.CraftItems))
            {
                SendChat(GetLang("CRAFTING_BACKPACK_NO_ITEMS", player.UserIDString), player);
                return;
            }

            TakeItems(player, BCraft.CraftItems);

            Item backpack = ItemManager.CreateByName(config.BackpackItem.Shortname, 1, config.BackpackItem.SkinID);
            Int32 Slot = GetAvailableSlots(player);

            backpack.name = GetLang("BACKPACK_TITLE", player.UserIDString, Slot);

            Backpacks.Add(backpack.uid, new BackpackInfo()
            {
                AmountSlot = Slot,
            });

            player.GiveItem(backpack);
        }

        #endregion

        #region Other
        private UInt64 GetBackpackID(BasePlayer player)
        {
            UInt64 ID = 0;
            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
            {
                Item Backpack = GetBackpack(player);
                if (Backpack == null) return ID;
                ID = Backpack.uid;
            }
            else ID = player.userID;

            return ID;
        }
        private Item GetBackpack(BasePlayer player)
        {
            Item item = player.inventory.containerWear.itemList.Find(x => x.skin == config.BackpackItem.SkinID);
            return item;
        }
        private Configuration.Backpack.BackpackCraft GetBackpackOption(BasePlayer player)
        {
            Configuration.Backpack.BackpackCraft BCraft = config.BackpackItem.BackpacOption.FirstOrDefault(x => permission.UserHasPermission(player.UserIDString, x.Permissions));
            return BCraft;
        }
        private List<Item> GetItemBlacklist(BasePlayer player)
        {
            Configuration.Backpack.BackpackCraft Backpack = GetBackpackOption(player);
            if (Backpack == null || Backpack.BlackListItems == null || Backpack.BlackListItems.Count == 0) return null;
            List<Item> ItemList = new List<Item>();

            foreach(Item item in player.inventory.AllItems())
                foreach(String Shortname in Backpack.BlackListItems)
                    if (item.info.shortname == Shortname)
                        ItemList.Add(item);

            return ItemList;
        }
        private Int32 GetAvailableSlots(BasePlayer player)
        {
            Int32 AvailableSlots = 0;

            Configuration.Backpack.BackpackCraft BCraft = GetBackpackOption(player);
            if (BCraft == null) return AvailableSlots;
            AvailableSlots = BCraft.AmountSlot;

            return AvailableSlots;
        }
        private Int32 GetMaximumUpgradeSlots(BasePlayer player)
        {
            Configuration.Backpack.BackpackCraft BackpackOption = GetBackpackOption(player);
            if (BackpackOption == null) return 0;
            Int32 Slots = BackpackOption.AmountSlot;

            foreach (Configuration.Backpack.BackpackCraft.UpgradeBackpack Upgrades in BackpackOption.UpgradeList)
                Slots += Upgrades.SlotUpgrade;

            return Slots;
        }
        private Int32 GetSlotsBackpack(BasePlayer player)
        {
            Int32 SlotsBackpack = 0;
            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
            {
                Item item = GetBackpack(player);

                if (item != null)
                    if (Backpacks.ContainsKey(item.uid))
                        SlotsBackpack = Backpacks[item.uid].AmountSlot;
            }
            else
            {
                if (Backpacks.ContainsKey(player.userID))
                    SlotsBackpack = Backpacks[player.userID].AmountSlot;
            }

            return SlotsBackpack;
        }
        private Int32 GetBusySlotsBackpack(BasePlayer player)
        {
            if (config.TurnedsSetting.Types == TypeBackpack.Wear)
            {
                Item item = GetBackpack(player);

                if (item != null)
                    if (Backpacks.ContainsKey(item.uid))
                    {
                        PrintError(item.uid.ToString() + " " +Backpacks[item.uid].Items.Count.ToString());
                        return Backpacks[item.uid].Items.Count;
                    }
            }
            else
            {
                if (Backpacks.ContainsKey(player.userID))
                    return Backpacks[player.userID].Items.Count;
            }

            return 0;
        }
        private void RegisteredPermissions()
        {
            foreach (Configuration.Backpack.BackpackCraft BPCraft in config.BackpackItem.BackpacOption)
                permission.RegisterPermission(BPCraft.Permissions, this);
        }
        private Boolean HaveAllItem(BasePlayer player, List<Configuration.Backpack.BackpackCraft.ItemCraft> BPCraft, ItemContainer contaner = null)
        {
            Int32 TrueItem = 0;
            for (Int32 i = 0; i < BPCraft.Count; i++)
            {
                Configuration.Backpack.BackpackCraft.ItemCraft Item = BPCraft[i];
                if (HaveItem(player, Item.Shortname, Item.Amount, Item.SkinID, contaner))
                    TrueItem++;
            }

            return TrueItem >= BPCraft.Count;
        }

        private Boolean HaveItem(BasePlayer player, String Shortname, Int32 Amount, UInt64 SkinID = 0, ItemContainer contaner = null)
        {
            Int32 ItemAmount = 0;
            foreach (Item ItemRequires in contaner == null ? player.inventory.AllItems().ToList() : contaner.itemList)
            {
                if (ItemRequires == null) continue;
                if (ItemRequires.info.shortname != Shortname) continue;
                if (ItemRequires.skin != SkinID) continue;
                ItemAmount += ItemRequires.amount;
            }
            return ItemAmount >= Amount;
        }
        private void TakeItems(BasePlayer player, List<Configuration.Backpack.BackpackCraft.ItemCraft> BPCraft, ItemContainer contaner = null)
        {
            Int32 Index = 0;
            List<Int32> ItemList = new List<Int32>();
            List<Item> ContainerItems = contaner == null ? player.inventory.AllItems().ToList() : contaner.itemList;

            foreach (Configuration.Backpack.BackpackCraft.ItemCraft ItemTake in BPCraft)
            {
                ItemList.Add(ItemTake.Amount);
                foreach (Item ItemPlayer in ContainerItems.Where(x => x.skin == ItemTake.SkinID && x.info.shortname == ItemTake.Shortname))
                {
                    if (ItemList[Index] <= 0) continue;
                    ItemList[Index] -= ItemPlayer.amount;
                    ItemPlayer.UseItem(ItemList[Index] > 0 ? ItemList[Index] : ItemTake.Amount);
                }
                Index++;
            }
        }
        #endregion

        #endregion

        #region Commands

        #region Open BP
        [ConsoleCommand("bp")]
        void OpenBackpackConsole(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            OpenBP(player);
        }

        [ChatCommand("bp")]
        void OpenBackpackChat(BasePlayer player)
        {
            if (player == null) return;
            OpenBP(player);
        }

        #endregion

        #region Upgrade BP
        [ConsoleCommand("backpack.upgrade")]
        void BackpackUpgradeCommand(ConsoleSystem.Arg arg)
        {
            if (!config.TurnedsSetting.UseUpgradeBackpack) return;
            BasePlayer player = arg.Player();
            if (player == null) return;

            UpgradeBackpack(player);
        }

        [ConsoleCommand("backpack.upgrade.menu")]
        void BackpackUpgrade(ConsoleSystem.Arg arg)
        {
            if (!config.TurnedsSetting.UseUpgradeBackpack) return;
            BasePlayer player = arg.Player();
            if (player == null) return;
            OpenUpgradeMenu(player);
        }
        [ChatCommand("upgradebp")]
        void UpgradeBPCommand(BasePlayer player)
        {
            if (IsDuel(player.userID)) return;
            if (player == null) return;
            OpenUpgradeMenu(player);
        }

        #endregion

        #region Crafting BP

        [ConsoleCommand("backpack.crafting")]
        void BackpackCrafting(ConsoleSystem.Arg arg)
        {
            if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;
            BasePlayer player = arg.Player();
            if (player == null) return;

            CraftingBackpack(player);
        }

        [ChatCommand("backpack")]
        void BackpackCraftMenu(BasePlayer player)
        {
            if (IsDuel(player.userID)) return;
            if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;
            if (_interface == null)
            {
                SendChat(GetLang("BACKPACK_NO_INITIALIZE", player.UserIDString), player);
                return;
            }
            DrawUI_Backpack_Main(player);
        }
        #endregion

        #endregion

        #region Interface
        private void DrawUI_Backpack_Main(BasePlayer player)
        {
            if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;
            String Interface = InterfaceBuilder.GetInterface("UI_Backpack_Main");
            if (Interface == null) return;

            Interface = Interface.Replace("%CRAFT_BTN%", GetLang("CRAFT_BTN", player.UserIDString));
            Interface = Interface.Replace("%SLOT_AVALIBLE_TITLE%", GetLang("SLOT_AVALIBLE_TITLE", player.UserIDString));
            Interface = Interface.Replace("%TITLE_PLUGIN_CRAFT_MENU%", GetLang("TITLE_PLUGIN_CRAFT_MENU", player.UserIDString));
            Interface = Interface.Replace("%TITLE_STORE_INFORMATION%", GetLang("TITLE_STORE_INFORMATION", player.UserIDString));
            Interface = Interface.Replace("%TITLE_HAVE_ITEMS%", GetLang("TITLE_HAVE_ITEMS", player.UserIDString));
            Interface = Interface.Replace("%TITLE_HAVE_ITEMS_DESCRIPTION%", GetLang("TITLE_HAVE_ITEMS_DESCRIPTION", player.UserIDString));

            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack);
            CuiHelper.AddUi(player, Interface);

            DrawUI_Backpack_Slots(player);
            DrawUI_Backpack_ItemCrafts(player);
        }    
       
        #region DrawUI Slots
        private void DrawUI_Backpack_Slots(BasePlayer player)
        {
            if (_interface == null) return;
            if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;

            Int32 SlotY = 0;
            Int32 SlotX = 0;
            Int32 SlotAmount = config.TurnedsSetting.UseUpgradeBackpack ? 42 : GetAvailableSlots(player);
            for (Int32 Slot = 0; Slot < SlotAmount; Slot++)
            {
                String Interface = InterfaceBuilder.GetInterface("UI_Backpack_Slots");
                if (Interface == null) return;
                // %OFFSET_MIN% =  
                // %OFFSET_MAX% = 
                Interface = Interface.Replace("%INDEX%", $"{Slot}");
                Interface = Interface.Replace("%OFFSET_MIN%", $"{-211 + (SlotX * 72)} {144.6 - (SlotY * 72)}");
                Interface = Interface.Replace("%OFFSET_MAX%", $"{-147 + (SlotX * 72)} {208.6 - (SlotY * 72)}");
                //container.Add(new CuiPanel
                //{
                //    CursorEnabled = false,
                //    Image = { Color = "0.91 0.87 0.83 0.1"/*"0.79 0.77 0.62 0.1"*/, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                //    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-211 + (SlotX * 72)} {144.6 - (SlotY * 72)}", OffsetMax = $"{-147 + (SlotX * 72)} {208.6 - (SlotY * 72)}" }
                //}, "SlotPanels", $"Slot_{Slot}");
                
                SlotX++;
                if (SlotX == 6)
                {
                    SlotX = 0;
                    SlotY++;
                }
                CuiHelper.AddUi(player, Interface);
            }



            if (config.TurnedsSetting.UseUpgradeBackpack)
                DrawUI_Backpack_Slots_Locked(player);
        }

        private void DrawUI_Backpack_Slots_Locked(BasePlayer player)
        {
            if (_interface == null) return;
            if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;
            Int32 AvailableSlots = GetAvailableSlots(player);
            Int32 LockedSlots = 42 - AvailableSlots;

            for (Int32 LockSlots = LockedSlots; LockSlots > 0; LockSlots--)
            {
                String Interface = InterfaceBuilder.GetInterface("UI_Backpack_Slots_Lock");
                if (Interface == null) return;

                Interface = Interface.Replace("%INDEX%", $"{42 - LockSlots}");
                CuiHelper.AddUi(player, Interface);
            }
        }
        #endregion

        #region DrawUI CraftItem
        private void DrawUI_Backpack_ItemCrafts(BasePlayer player)
        {
            if (_interface == null) return;
            if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;
            Configuration.Backpack.BackpackCraft BCraft = GetBackpackOption(player);
            if (BCraft == null || BCraft.CraftItems == null) return;

            #region Centering
            Int32 ItemCount = 0;
            Single itemMinPosition = 219f;
            Single itemWidth = 0.15f; /// Ширина
            Single itemMargin = 0.062f; /// Расстояние между 
            Int32 itemCount = BCraft.CraftItems.Count;
            Single itemMinHeight = 0.84f; // Сдвиг по вертикали
            Single itemHeight = 0.16f; /// Высота
            Int32 ItemTarget = 5;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            #endregion

            foreach (Configuration.Backpack.BackpackCraft.ItemCraft item in BCraft.CraftItems)
            {
                String Interface = InterfaceBuilder.GetInterface("UI_Backpack_Craft_Item");
                if (Interface == null) return;

                Interface = Interface.Replace("%ANCHOR_MIN%", $"{itemMinPosition} {itemMinHeight}");
                Interface = Interface.Replace("%ANCHOR_MAX%", $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}");
                Interface = Interface.Replace("%AMOUNT%", $"{item.Amount}");
                Interface = Interface.Replace("%PNG_ITEM%", GetImage(item));
                Interface = Interface.Replace("%COLOR_PANEL%", HaveItem(player, item.Shortname, item.Amount, item.SkinID) ? "0.57 1.00 0.65 1.00" : "0.98 0.13 0.18 1.00");

                CuiHelper.AddUi(player, Interface);

                #region Centering
                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 0.5f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }
                #endregion
            }
        }
        #endregion

        #region DrawUI Backpack Visual
        private Int32 GetSlotsPercent(Single Percent, Single Slots)
        {
            Single ReturnSlot = (((Single)Slots / 100.0f) * Percent);
            return (Int32)ReturnSlot;
        }
        private void DrawUI_Backpack_Visual(BasePlayer player)
        {
            if (!config.TurnedsSetting.VisualBackpackSlots.UseVisual || _interface == null) return;
            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Visual);

            String Interface = InterfaceBuilder.GetInterface("UI_Backpack_Visual_Backpack_Slot");
            if (Interface == null) return;
           
            Single BusySlots = (Single)GetBusySlotsBackpack(player);
            Single Slots = (Single)GetSlotsBackpack(player);
            if (Slots == 0) return;
            Single Y_Progress = (Single)((Single)BusySlots / (Single)Slots);

            String Y_Progress_Color = BusySlots >= GetSlotsPercent(80.0f, Slots) ? config.TurnedsSetting.VisualBackpackSlots.ColorProgressBar.ColorMaximum :
                                      BusySlots >= GetSlotsPercent(60.0f, Slots) ? config.TurnedsSetting.VisualBackpackSlots.ColorProgressBar.ColorAverage :
                                                                                   config.TurnedsSetting.VisualBackpackSlots.ColorProgressBar.ColorMinimal;

            Interface = Interface.Replace("%CRAFT_BTN%", GetLang("CRAFT_BTN", player.UserIDString));
            Interface = Interface.Replace("%SLOTS_INFO%", $"<b>{BusySlots}/{Slots}</b>");
            Interface = Interface.Replace("%Y_PROGRESS%", $"{Y_Progress}");
            Interface = Interface.Replace("%Y_PROGRESS_COLOR%", $"{Y_Progress_Color}");

            CuiHelper.AddUi(player, Interface);
        }
        #endregion

        #region DrawUI Backpak Upgrade
        private void DrawUI_Backpack_Upgrade_Info(BasePlayer player)
        {
            if (!config.TurnedsSetting.UseUpgradeBackpack || _interface == null) return;

            if (!PlayerUpgrades.ContainsKey(player)) return;
             String Interface = InterfaceBuilder.GetInterface("UI_Backpack_Upgrade_Info");
            if (Interface == null) return;

            Interface = Interface.Replace("%BACKPACK_UPGRADE_DESCRIPTION%", GetLang("BACKPACK_UPGRADE_DESCRIPTION", player.UserIDString));
            Interface = Interface.Replace("%BACKPACK_UPGRADE_TITLE%", GetLang("BACKPACK_UPGRADE_TITLE", player.UserIDString));

            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Upgrade_Workbench);
            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Upgrade_Info);
            CuiHelper.AddUi(player, Interface);
            DrawUI_Backpack_Upgrade_Info_Controller(player);
        }

        private void DrawUI_Backpack_Upgrade_Info_Controller(BasePlayer player)
        {
            if (!config.TurnedsSetting.UseUpgradeBackpack || _interface == null) return;

            if (!PlayerUpgrades.ContainsKey(player)) return;
            if (PlayerUpgrades[player] == null) return;
            String Interface = InterfaceBuilder.GetInterface("UI_Backpack_Upgrade_Info_Controller");
            if (Interface == null) return;

            Boolean IsUpgrade = HaveAllItem(player, PlayerUpgrades[player].ItemList, PlayerUpgrades[player].Container.customerInventory);
            Interface = Interface.Replace("%BACKPACK_UPGRADE_WORKBENCH_INFORMATION_SLOTS%", GetLang("BACKPACK_UPGRADE_WORKBENCH_INFORMATION_SLOTS", player.UserIDString, PlayerUpgrades[player].UpgradeSlotsUp));
            Interface = Interface.Replace("%BACKPACK_UPGRADE_WORKBENCH_INFORMATION_BUTTON_TILE%", GetLang((IsUpgrade ? "BACKPACK_UPGRADE_WORKBENCH_INFORMATION_BUTTON_TILE" : "BACKPACK_UPGRADE_WORKBENCH_INFORMATION_BUTTON_TILE_FALSE_RESOURCE"), player.UserIDString));
            Interface = Interface.Replace("%BUTTON_COLOR%", (IsUpgrade ? "0.4431373 0.5450981 0.2627451 1" : "0.7568628 0.2273419 0.2078431 1"));


            CuiHelper.DestroyUi(player, "BackpackStatusUpgrade");
            CuiHelper.AddUi(player, Interface);
        }
        #endregion

        #region DrawUI Backpak Upgrade Workbench
        private void DrawUI_Backpack_Upgrade_Workbench(BasePlayer player)
        {
            if (!config.TurnedsSetting.UseUpgradeBackpack || _interface == null) return;

            CuiHelper.DestroyUi(player, InterfaceBuilder.UI_Backpack_Upgrade_Workbench);
            String Interface = InterfaceBuilder.GetInterface("UI_Backpack_Upgrade_Workbench");
            if (Interface == null) return;
            Configuration.Backpack.BackpackCraft BackpackOption = GetBackpackOption(player);
            if (BackpackOption == null) return;

            UInt64 ID = GetBackpackID(player);
            if (!Backpacks.ContainsKey(ID)) return;

            Boolean IsUpgrade = (BackpackOption.UpgradeList.Count - 1) < (Backpacks[ID].IndexUpgrade + 1); 
            Interface = Interface.Replace("%BACKPACK_UPGRADE_WORKBENCH_BUTTON%", GetLang(IsUpgrade ? "BACKPACK_UPGRADE_WORKBENCH_BUTTON_MAXIMUM" : "BACKPACK_UPGRADE_WORKBENCH_BUTTON", player.UserIDString));
            Interface = Interface.Replace("%COLOR%", IsUpgrade ? "0.7568628 0.2273419 0.2078431 1" : "0.4431373 0.5450981 0.2627451 1");
            Interface = Interface.Replace("%COMMAND%", IsUpgrade ? "" : "backpack.upgrade.menu");
            Interface = Interface.Replace("%BACKPACK_UPGRADE_WORKBENCH_DESCRIPTION%", GetLang("BACKPACK_UPGRADE_WORKBENCH_DESCRIPTION", player.UserIDString, GetMaximumUpgradeSlots(player) >= 42 ? 42 : GetMaximumUpgradeSlots(player)));
            Interface = Interface.Replace("%BACKPACK_UPGRADE_WORKBENCH_TITLE%", GetLang("BACKPACK_UPGRADE_WORKBENCH_TITLE", player.UserIDString));

            CuiHelper.AddUi(player, Interface);
        }
        #endregion

        private static InterfaceBuilder _interface;
        private class InterfaceBuilder
        {
            #region Vars

            public static InterfaceBuilder Instance;
            public const String UI_Backpack = "UI_BACKPACK";
            public const String UI_Backpack_Visual = "UI_BACKPACK_VISUAL";
            public const String UI_Backpack_Upgrade_Info = "UI_BACKPACK_UPGRATE_INFO";
            public const String UI_Backpack_Upgrade_Workbench = "UI_BACKPACK_UPGRATE_WORKBENCH";
            public Dictionary<String, String> Interfaces;

            #endregion

            #region Main

            public InterfaceBuilder()
            {
                Instance = this;
                Interfaces = new Dictionary<String, String>();
                BuildingBackpack_Main();
                BuildingBackpack_Slots();
                BuildingBackpack_Slots_Lock();
                BuildingBackpack_Craft();
                BuildingBackpack_UpgradeBackpack_Workbench();
                BuildingBackpack_UpgradeBackpack_Info();
                BuildingBackpack_UpgradeBackpack_Info_Controller();

                if (config.TurnedsSetting.VisualBackpackSlots.UseVisual)
                    BuildingBackpack_Visual_Backpack_Slot();
            }

            public static void AddInterface(String name, String json)
            {
                if (Instance.Interfaces.ContainsKey(name))
                {
                    _.PrintError($"Error! Tried to add existing cui elements! -> {name}");
                    return;
                }

                Instance.Interfaces.Add(name, json);
            }

            public static string GetInterface(String name)
            {
                string json = string.Empty;
                if (Instance.Interfaces.TryGetValue(name, out json) == false)
                {
                    _.PrintWarning($"Warning! UI elements not found by name! -> {name}");
                }

                return json;
            }

            public static void DestroyAll()
            {
                for (var i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    
                    CuiHelper.DestroyUi(player, UI_Backpack);
                    CuiHelper.DestroyUi(player, UI_Backpack_Visual);
                    CuiHelper.DestroyUi(player, UI_Backpack_Upgrade_Info);
                    CuiHelper.DestroyUi(player, UI_Backpack_Upgrade_Workbench);
                }
            }

            #endregion

            #region Building Interface
            private void BuildingBackpack_Main()
            {
                if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    Image = { Color = "0 0 0 0.8", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, "Overlay", UI_Backpack);

                container.Add(new CuiElement
                {
                    Name = "BackpackImage",
                    Parent = UI_Backpack,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = _.GetImage(config.BackpackItem.UrlBackpack) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-605 -104", OffsetMax = "-349 152" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = "backpack.crafting" },
                    Text = { Text = "%CRAFT_BTN%", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, Color = "0.91 0.87 0.83 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-605 -160.464", OffsetMax = "-349 -103.995" }
                }, UI_Backpack, "TitleCraft");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "205.779 -360", OffsetMax = "628.221 360" }
                }, UI_Backpack, "SlotPanels");

                container.Add(new CuiElement
                {
                    Name = "TitleSlot",
                    Parent = "SlotPanels",
                    Components = {
                    new CuiTextComponent { Text = "%SLOT_AVALIBLE_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.LowerLeft, Color = "0.91 0.87 0.83 1"},
                 	new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-211.221 210.295", OffsetMax = "211.219 242.64"  }
                    }
                });

                container.Add(new CuiElement
                {
                    Name = "TitleCraftMenu",
                    Parent = UI_Backpack,
                    Components = {
                    new CuiTextComponent { Text = "%TITLE_PLUGIN_CRAFT_MENU%", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleLeft, Color = "0.91 0.87 0.83 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-632.1 294.025", OffsetMax = "-213.633 354.497" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "InformationStore",
                    Parent = UI_Backpack,
                    Components = {
                    new CuiTextComponent { Text = "%TITLE_STORE_INFORMATION%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.UpperLeft, Color = "0.91 0.87 0.83 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-629 270.595", OffsetMax = "0 302.805" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0"},
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-328.658 -282.7", OffsetMax = "184.658 208.6" }
                }, UI_Backpack, "CraftPanel");

                container.Add(new CuiElement
                {
                    Name = "AHaveItemBackpack",
                    Parent = "CraftPanel",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE_HAVE_ITEMS%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.LowerLeft, Color = "0.91 0.87 0.83 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-256.658 -144.925", OffsetMax = "256.662 -118.475" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.91 0.87 0.83 0.15", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-256.66 -250.39", OffsetMax = "256.66 -150.39" }
                }, "CraftPanel", "TitleBlur");

                container.Add(new CuiElement
                {
                    Name = "InstructionLabel",
                    Parent = "TitleBlur",
                    Components = {
                    new CuiTextComponent { Text = "%TITLE_HAVE_ITEMS_DESCRIPTION%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "0.91 0.87 0.83 1"  },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-244.095 -37.435", OffsetMax = "244.095 37.435" }
                }
                });

                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Close = UI_Backpack },
                    Text = { Text = "✖", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-57 -57", OffsetMax = "0 0" }
                },  UI_Backpack, "CloseBackpack");

                AddInterface("UI_Backpack_Main", container.ToJson());
            }
            #endregion

            #region Building Slots

            private void BuildingBackpack_Slots()
            {
                if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.91 0.87 0.83 0.1"/*"0.79 0.77 0.62 0.1"*/, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "%OFFSET_MIN%", OffsetMax = "%OFFSET_MAX%" }
                }, "SlotPanels", $"Slot_%INDEX%");

                //Int32 SlotY = 0;
                //Int32 SlotX = 0;
                //for (Int32 Slot = 0; Slot < 42; Slot++)
                //{
                //    container.Add(new CuiPanel 
                //    {
                //        CursorEnabled = false,
                //        Image = { Color = "0.91 0.87 0.83 0.1"/*"0.79 0.77 0.62 0.1"*/, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                //        RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-211 + (SlotX * 72)} {144.6 - (SlotY * 72)}", OffsetMax = $"{-147 + (SlotX * 72)} {208.6 - (SlotY * 72)}" }
                //    }, "SlotPanels", $"Slot_{Slot}");

                //    SlotX++;
                //    if(SlotX == 6)
                //    {
                //        SlotX = 0;
                //        SlotY++;
                //    }
                //}

                AddInterface("UI_Backpack_Slots", container.ToJson());
            }    
            private void BuildingBackpack_Slots_Lock()
            {
                if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0.8", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", Sprite = "assets/icons/lock.png" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                }, $"Slot_%INDEX%");

                AddInterface("UI_Backpack_Slots_Lock", container.ToJson());
            }

            #endregion

            #region Building Crafts
            private void BuildingBackpack_Craft()
            {
                if (!config.TurnedsSetting.UseCrafting || config.TurnedsSetting.Types == TypeBackpack.OnlyPermission) return;
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 0"/*"0.3679245 0.3679245 0.3679245 1" */},
                    RectTransform = { AnchorMin = "%ANCHOR_MIN%", AnchorMax = "%ANCHOR_MAX%" }
                }, "CraftPanel", "ItemCraft");

                container.Add(new CuiElement
                {
                    Name = "ImageIcon",
                    Parent = "ItemCraft",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = "%PNG_ITEM%" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-40 -37.51", OffsetMax = "40 40" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "InfoCount",
                    Parent = "ItemCraft",
                    Components = {
                    new CuiTextComponent { Text = $"X%AMOUNT%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.LowerRight, Color = "0.91 0.87 0.83 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-37.859 -37.51", OffsetMax = "37.383 -13.787" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "%COLOR_PANEL%" },
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-40 0", OffsetMax = "40 2.49" }
                }, "ItemCraft", "StatusPanel");

                AddInterface("UI_Backpack_Craft_Item", container.ToJson());
            }
            #endregion

            #region Building BackpackSlot Visual
            private void BuildingBackpack_Visual_Backpack_Slot()
            {
                CuiElementContainer container = new CuiElementContainer();
                Configuration.Turneds.VisualBackpackSlot.Position Position = config.TurnedsSetting.VisualBackpackSlots.PositionSlotVisual;

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0.15" },
                    RectTransform = { AnchorMin = Position.AnchorMin, AnchorMax = Position.AnchorMax, OffsetMin = Position.OffsetMin, OffsetMax = Position.OffsetMax }
                }, "Overlay", UI_Backpack_Visual);

                container.Add(new CuiElement
                {
                    Name = "BpImage",
                    Parent = UI_Backpack_Visual,
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Png = _.GetImage(config.BackpackItem.UrlBackpack) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-25 -25", OffsetMax = "25 25" }
                }
                });

                if (config.TurnedsSetting.VisualBackpackSlots.UseSlots)
                {
                    container.Add(new CuiElement
                    {
                        Name = "IsFullSlots",
                        Parent = UI_Backpack_Visual,
                        Components = {
                        new CuiTextComponent { Text = "%SLOTS_INFO%", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleRight, Color = "0.91 0.87 0.83 0.5"  },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-26.017 -30.072", OffsetMax = "26.017 -11.002" }
                    }
                    });
                }

                if (config.TurnedsSetting.VisualBackpackSlots.UseSlots)
                {
                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "0 0 0 0.2" },
                        RectTransform = { AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "0 -30.072", OffsetMax = "3.736 30.072" }
                    }, UI_Backpack_Visual, "IsFullPanel");

                    container.Add(new CuiPanel
                    {
                        CursorEnabled = false,
                        Image = { Color = "%Y_PROGRESS_COLOR%" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 %Y_PROGRESS%", OffsetMin = "0.5 1", OffsetMax = "0 0" }
                    }, "IsFullPanel", "IsFullProgress");
                }

                if (config.TurnedsSetting.VisualBackpackSlots.UseButton)
                {
                    container.Add(new CuiButton
                    {
                        Button = { Color = "0 0 0 0", Command = "bp" },
                        Text = { Text = "", Font = "robotocondensed-regular.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }, UI_Backpack_Visual, "OPEN_BACKPACK");
                }

                AddInterface("UI_Backpack_Visual_Backpack_Slot", container.ToJson());
            }
            #endregion

            #region Building Upgrade Backpack
            private void BuildingBackpack_UpgradeBackpack_Info()
            {
                if (!config.TurnedsSetting.UseUpgradeBackpack) return;
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0.10581383" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "194.836 178.923", OffsetMax = "575.164 252.477" }
                }, "Overlay", UI_Backpack_Upgrade_Info);

                container.Add(new CuiElement
                {
                    Name = "Titles",
                    Parent = UI_Backpack_Upgrade_Info,
                    Components = {
                    new CuiTextComponent { Text = "%BACKPACK_UPGRADE_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.LowerLeft, Color = "0.91 0.87 0.83 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-190.166 41.466", OffsetMax = "190.164 73.334" }
                }
                }); 

                container.Add(new CuiElement
                {
                    Name = "DescriptionInstruction",
                    Parent = UI_Backpack_Upgrade_Info,
                    Components = {
                    new CuiTextComponent { Text = "%BACKPACK_UPGRADE_DESCRIPTION%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleLeft, Color = "0.91 0.87 0.83 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-180.794 -33.795", OffsetMax = "180.792 33.795" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0.10581383" },
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-190.166 -430.851", OffsetMax = "190.154 -360.833" }
                }, UI_Backpack_Upgrade_Info, "BackpackStatusUpgrade");

                container.Add(new CuiButton
                {
                    Button = { Color = "0.4431373 0.5450981 0.2627451 1", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "%BACKPACK_UPGRADE_WORKBENCH_INFORMATION_BUTTON_TILE%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "0.91 0.87 0.83 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-118.576 -10.398", OffsetMax = "118.576 29.798" }
                }, "BackpackStatusUpgrade", "UpgradeButton");

                container.Add(new CuiElement
                {
                    Name = "InformationSlots",
                    Parent = "BackpackStatusUpgrade",
                    Components = {
                    new CuiTextComponent { Text = "%BACKPACK_UPGRADE_WORKBENCH_INFORMATION_SLOTS%", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0.91 0.87 0.83 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-190.16 -35.008", OffsetMax = "190.16 -10.398" }
                }
                });

                AddInterface("UI_Backpack_Upgrade_Info", container.ToJson());
            }
            private void BuildingBackpack_UpgradeBackpack_Info_Controller()
            {
                if (!config.TurnedsSetting.UseUpgradeBackpack) return;
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0.10581383" },
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-190.166 -430.851", OffsetMax = "190.154 -360.833" }
                }, UI_Backpack_Upgrade_Info, "BackpackStatusUpgrade"); 

                container.Add(new CuiButton
                {
                    Button = { Color = "%BUTTON_COLOR%", Command = "backpack.upgrade", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "%BACKPACK_UPGRADE_WORKBENCH_INFORMATION_BUTTON_TILE%", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "0.91 0.87 0.83 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-118.576 -10.398", OffsetMax = "118.576 29.798" }
                }, "BackpackStatusUpgrade", "UpgradeButton");

                container.Add(new CuiElement
                {
                    Name = "InformationSlots",
                    Parent = "BackpackStatusUpgrade",
                    Components = {
                    new CuiTextComponent { Text = "%BACKPACK_UPGRADE_WORKBENCH_INFORMATION_SLOTS%", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "0.91 0.87 0.83 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-190.16 -35.008", OffsetMax = "190.16 -10.398" }
                }
                });

                AddInterface("UI_Backpack_Upgrade_Info_Controller", container.ToJson());
            }
            #endregion

            #region Building Upgrade Backpack Workbench
            private void BuildingBackpack_UpgradeBackpack_Workbench()
            {
                if (!config.TurnedsSetting.UseUpgradeBackpack) return;
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0.025", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    RectTransform = { AnchorMin = config.TurnedsSetting.VisualBackpackSlots.PositionUpgrade.AnchorMin, AnchorMax = config.TurnedsSetting.VisualBackpackSlots.PositionUpgrade.AnchorMax, OffsetMin = config.TurnedsSetting.VisualBackpackSlots.PositionUpgrade.OffsetMin, OffsetMax = config.TurnedsSetting.VisualBackpackSlots.PositionUpgrade.OffsetMax }
                }, "Overlay", UI_Backpack_Upgrade_Workbench);

                container.Add(new CuiElement
                {
                    Name = "Titles",
                    Parent = UI_Backpack_Upgrade_Workbench,
                    Components = {
                    new CuiTextComponent { Text = "%BACKPACK_UPGRADE_WORKBENCH_TITLE%", Font = "robotocondensed-regular.ttf", FontSize = 20, Align = TextAnchor.LowerLeft, Color = "0.91 0.87 0.83 1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-190.166 41.466", OffsetMax = "190.164 73.334" }
                }
                });

                container.Add(new CuiElement
                {
                    Name = "DescriptionInstruction",
                    Parent = UI_Backpack_Upgrade_Workbench,
                    Components = {
                    new CuiTextComponent { Text = "%BACKPACK_UPGRADE_WORKBENCH_DESCRIPTION%", Font = "robotocondensed-regular.ttf", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "0.91 0.87 0.83 0.7" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-123.077 -33.795", OffsetMax = "180.793 33.795" }
                }
                });
                container.Add(new CuiElement
                {
                    Name = "SpriteLogo",
                    Parent = UI_Backpack_Upgrade_Workbench,
                    Components = {
                    new CuiImageComponent { Color = "1 1 1 0.35", Sprite = "assets/icons/tools.png", },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-174.6 -16", OffsetMax = "-142.6 16" }
                }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "1 1 1 0.025", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-190.168 -91.693", OffsetMax = "190.162 -41.7" }
                },  UI_Backpack_Upgrade_Workbench, "PanelButtonInfo");

                container.Add(new CuiButton
                {
                    Button = { Command = "%COMMAND%", Color = "%COLOR%", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    Text = { Text = "%BACKPACK_UPGRADE_WORKBENCH_BUTTON%", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.91 0.87 0.83 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-65.306 -15.956", OffsetMax = "65.306 15.956" },
                },  "PanelButtonInfo", "ButtonOpenMenu");

                AddInterface("UI_Backpack_Upgrade_Workbench", container.ToJson());
            }

            #endregion
        }

        #endregion

        #region Lang    
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
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CRAFT_BTN"] = "CRAFT",
                ["SLOT_AVALIBLE_TITLE"] = "<b>YOUR AVAILABLE NUMBER OF SLOTS</b>",
                ["TITLE_PLUGIN_CRAFT_MENU"] = "CREATING BACKPACK",
                ["TITLE_STORE_INFORMATION"] = "YOU CAN EXPAND THE NUMBER OF SLOTS IN OUR STORE - VK.COM/MERCURYDEV",
                ["TITLE_HAVE_ITEMS"] = "<b>REQUIRED ITEMS FOR CRAFTING A BACKPACK</b>",
                ["TITLE_HAVE_ITEMS_DESCRIPTION"] = "In this section, the items that are required to create this backpack are displayed, you need to collect all the items and press the 'CRAFT' button, if you have the right amount collected, the indicator will change color to green under the item",

                ["CRAFTING_BACKPACK_NO_ITEMS"] = "You don't have enough items to create a backpack",
                ["BACKPACK_TITLE"] = "BACKPACK {0} SLOT(S)",
                ["BACKPACK_IS_OPENED"] = "Do you already have your backpack open!",
                ["BACKPACK_NO_WEARING"] = "In order to use a backpack, you need to put it on!",
                ["BACKPACK_NO_INITIALIZE"] = "The plugin is loading, expect you will be able to open crafting soon!",

                ["BACKPACK_UPGRADE_TITLE"] = "<b>Manual</b>",
                ["BACKPACK_UPGRADE_DESCRIPTION"] = "You need to collect these items and put them in the slot together with the backpack, in exchange you will receive an improved rucksack with an additional number of slots",
                
                ["BACKPACK_UPGRADE_WORKBENCH_TITLE"] = "<b>UPGRADE BACKPACK</b>",
                ["BACKPACK_UPGRADE_WORKBENCH_DESCRIPTION"] = "You can upgrade the backpack and increase the slots in it, to go to the upgrade menu, press the green button!\nThe maximum number of improvements to {0} slot(s)",
                ["BACKPACK_UPGRADE_WORKBENCH_BUTTON"] = "<b>UPGRADE BACKPACK</b>",
                ["BACKPACK_UPGRADE_WORKBENCH_INFORMATION_SLOTS"] = "By upgrading the backpack to this level, you will receive: +{0} slot(s)",
                ["BACKPACK_UPGRADE_WORKBENCH_INFORMATION_BUTTON_TILE"] = "<b>UPGRADE BACKPACK</b>",
                ["BACKPACK_UPGRADE_WORKBENCH_BUTTON_MAXIMUM"] = "<b>MAX UPGRADE</b>",
                ["BACKPACK_UPGRADE_WORKBENCH_INFORMATION_BUTTON_TILE_FALSE_RESOURCE"] = "<b>INSUFFICIENT RESOURCES</b>",

                ["BACKPACK_GRANT"] = "You have successfully received a backpack, the number of slots has been increased to : {0}",
                ["BACKPACK_REVOKE"] = "Your extra slots privilege expired, slots reduced to : {0}",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["CRAFT_BTN"] = "СКРАФТИТЬ",
                ["SLOT_AVALIBLE_TITLE"] = "<b>ВАШЕ ДОСТУПНОЕ КОЛИЧЕСТВО СЛОТОВ</b>",
                ["TITLE_PLUGIN_CRAFT_MENU"] = "СОЗДАНИЕ РЮКЗАКА",
                ["TITLE_STORE_INFORMATION"] = "РАСШИРИТЬ КОЛИЧЕСТВО СЛОТОВ МОЖНО У НАС В МАГАЗИНЕ - VK.COM/MERCURYDEV",
                ["TITLE_HAVE_ITEMS"] = "<b>ТРЕБУЕМЫЕ ПРЕДМЕТЫ ДЛЯ КРАФТА РЮКЗАКА</b>",
                ["TITLE_HAVE_ITEMS_DESCRIPTION"] = "В этой секции отображаются предметы, которые требуются для создания данного рюкзака, вам нужно собрать все элементы и нажать кнопку 'СКРАФТИТЬ', если у вас будет собрано нужное количество, под предметом индикатор изменит цвет на зеленый",

                ["CRAFTING_BACKPACK_NO_ITEMS"] = "У вас недостаточно предметов для создания рюкзака",
                ["BACKPACK_TITLE"] = "РЮКЗАК {0} СЛОТА(ОВ)",
                ["BACKPACK_IS_OPENED"] = "У вас уже открыт рюкзак!",
                ["BACKPACK_NO_WEARING"] = "Для того, чтобы использовать рюкзак, необходимо его надеть!",
                ["BACKPACK_NO_INITIALIZE"] = "Плагин загружается, ожидайте, вскоре вы сможете открыть крафт!",

                ["BACKPACK_UPGRADE_TITLE"] = "<b>Инструкция</b>",
                ["BACKPACK_UPGRADE_DESCRIPTION"] = "Вам нужно собрать указанные предметы и переложить в слот вместе с рюкзаком, в обмен вы получите улучшенный рюкзак с дополнительным количеством слотов",

                ["BACKPACK_UPGRADE_WORKBENCH_TITLE"] = "<b>УЛУЧШЕНИЕ РЮКЗАКА</b>",
                ["BACKPACK_UPGRADE_WORKBENCH_BUTTON"] = "<b>УЛУЧШИТЬ РЮК.</b>",
                ["BACKPACK_UPGRADE_WORKBENCH_BUTTON_MAXIMUM"] = "<b>МАКСИМУМ УЛ.</b>",
                ["BACKPACK_UPGRADE_WORKBENCH_DESCRIPTION"] = "Вы можете улучшать рюкзак и увеличивать слоты в нем, для перехода в меню улучшения нажмите зеленую кнопку!\nМаксимальное количество улучшений до - {0} слот(а/ов)",
                ["BACKPACK_UPGRADE_WORKBENCH_INFORMATION_SLOTS"] = "Улучшив рюкзак до данного уровня вы получите : +{0} слот(а/ов)",
                ["BACKPACK_UPGRADE_WORKBENCH_INFORMATION_BUTTON_TILE"] = "<b>УЛУЧШИТЬ РЮКЗАК</b>",
                ["BACKPACK_UPGRADE_WORKBENCH_INFORMATION_BUTTON_TILE_FALSE_RESOURCE"] = "<b>НЕДОСТАТОЧНО РЕСУРСОВ</b>",

                ["BACKPACK_GRANT"] = "Вы успешно получили рюкзак, количество слотов увеличено до : {0}",
                ["BACKPACK_REVOKE"] = "У вас истекла привилегия с дополнительными слотами, слоты уменьшились до : {0}",


            }, this, "ru");
        }
        #endregion

        #region Behaviour

        #region BP
        private class BackpackBehaviour : FacepunchBehaviour
        {
            public Item Backpack = null;
            private BasePlayer Player = null;
            public StorageContainer Container = null;
            public UInt64 BackpackID = 0;
            private Dictionary<Item, Item.Flag> SaveFlags = new Dictionary<Item, Item.Flag>();
            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
                BackpackID = _.GetBackpackID(Player);
            }
            private void BlackListAction(Boolean State)
            {
                List<Item> Itemlist = _.GetItemBlacklist(Player);
                if (Itemlist == null) return;

                foreach (Item item in Itemlist)
                {
                    if (State)
                        if (!SaveFlags.ContainsKey(item))
                            SaveFlags.Add(item, item.flags);

                    item.SetFlag(global::Item.Flag.IsLocked, State);
                }

                if(!State)
                    foreach(KeyValuePair<Item, Item.Flag> Items in SaveFlags)
                        Items.Key.SetFlag(Items.Value, true);

                Player.SendNetworkUpdate();
            }
            public void Open()
            {
                Container = CreateContainer(Player);

                PushItems();

                LockSlot(true);

                _.timer.Once(0.1f, () => PlayerLootContainer(Player, Container));
                BlackListAction(true);

                if (!_.PlayerUseBackpacks.Contains(Player))
                    _.PlayerUseBackpacks.Add(Player);
            }

            public void LockSlot(bool state)
            {
                if (Backpack == null) return;

                Backpack.LockUnlock(state);

                foreach (var item in Player.inventory.AllItems())
                    if (item.skin == Backpack.skin)
                        item.LockUnlock(state);
            }

            public void Close()
            {
                LockSlot(false);
                _.Backpacks[BackpackID].Items = SaveItems(Container.inventory.itemList);
                Container.inventory.Clear();
                Container.Kill();
                Container = null;

                Destroy(false);
                BlackListAction(false);
                if (_.PlayerUseBackpacks.Contains(Player))
                    _.PlayerUseBackpacks.Remove(Player);
            }

            private void PushItems()
            {
                //_.Unsubscribe("OnItemAddedToContainer");

                var items = RestoreItems(_.Backpacks[BackpackID].Items);
                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(Container.inventory, items[i].position);

               // _.Subscribe("OnItemAddedToContainer");
            }

            public void Destroy(bool isClose = true)
            {
                if (isClose)
                    Close();

                UnityEngine.Object.Destroy(this);
            }
        }
        #endregion

        #region Upgrade BP
        private class ShopFrontBehavior : FacepunchBehaviour
        {
            private BasePlayer Player = null;
            public ShopFront Container = null;
            public Int32 UpgradeSlotsUp = 0;
            public UInt64 BackpackID = 0;
            public List<Configuration.Backpack.BackpackCraft.ItemCraft> ItemList = null;
            private Dictionary<Item, Item.Flag> SaveFlags = new Dictionary<Item, Item.Flag>();
            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
                BackpackID = _.GetBackpackID(Player);
                ItemList = GetListUpgrade();
            }
            public void Open()
            {
                if (ItemList == null) return;
                Container = CreateShopFront(Player);
                Invoke(() =>
                {
                    PlayerLootContainer(Player, Container, ItemList);
                    VendorLock(true);
                    WhiteListAction(true);
                    _.DrawUI_Backpack_Upgrade_Info(Player);
                }, 0.1f);      
            }
            private List<Configuration.Backpack.BackpackCraft.ItemCraft> GetListUpgrade()
            {
                Configuration.Backpack.BackpackCraft Backpack = _.GetBackpackOption(Player);
                if (Backpack == null) return null;
                if (!_.Backpacks.ContainsKey(BackpackID)) return null;
                Int32 IndexUpgrade = _.Backpacks[BackpackID].IndexUpgrade;
                if ((Backpack.UpgradeList.Count - 1) < (IndexUpgrade + 1)) return null;
                List<Configuration.Backpack.BackpackCraft.ItemCraft> ItemList = Backpack.UpgradeList[IndexUpgrade + 1].CraftItems;
                if (ItemList == null || ItemList.Count == 0) return null;
                UpgradeSlotsUp = Backpack.UpgradeList[IndexUpgrade + 1].SlotUpgrade;

                return ItemList;
            }
            private List<Item> GetWhiteList()
            {
                List<Item> WhiteList = new List<Item>();

                foreach (Configuration.Backpack.BackpackCraft.ItemCraft Items in ItemList)
                    foreach (Item item in Player.inventory.AllItems())
                    {
                        if (item.info.shortname == Items.Shortname)
                            WhiteList.Add(item);
                    }
                return WhiteList;
            }


            private void WhiteListAction(Boolean State)
            {
                if (ItemList == null) return;

                foreach (Item item in Player.inventory.AllItems().Where(i => !GetWhiteList().Contains(i)))
                {
                    if (State)
                        if (!SaveFlags.ContainsKey(item))
                            SaveFlags.Add(item, item.flags);

                    item.SetFlag(global::Item.Flag.IsLocked, State);
                }

                foreach (KeyValuePair<Item, Item.Flag> Items in SaveFlags)
                    Items.Key.SetFlag(Items.Value, true);

                Player.SendNetworkUpdate();
            }
            public void VendorLock(Boolean State)
            {
                if (Container == null || Container.vendorInventory == null) return;
                Container.vendorInventory.SetLocked(State);
            }
            public void Close()
            {
                WhiteListAction(false);
                if (Container != null)
                {
                    Container.inventory.Clear();
                    Container.Kill();
                    Container = null;
                }
                CuiHelper.DestroyUi(Player, InterfaceBuilder.UI_Backpack_Upgrade_Info);
                CuiHelper.DestroyUi(Player, InterfaceBuilder.UI_Backpack_Upgrade_Workbench);
                Destroy(false);
            }

            public void Destroy(bool isClose = true)
            {
                if (isClose)
                    Close();

                UnityEngine.Object.Destroy(this);
            }
        }
        #endregion

        #region Backpack Spine [PVE]

        class BackpackSpine : FacepunchBehaviour
        {
            Rigidbody rigidbodyBP;
            DroppedItemContainer backpack;
            BasePlayer player;

            public BackpackSpine()
            {
                player = GetComponent<BasePlayer>();

                backpack = GameManager.server.CreateEntity("assets/prefabs/misc/item drop/item_drop_backpack.prefab", player.transform.position + new Vector3(0f, -7f, 0f), new Quaternion()) as DroppedItemContainer;
                rigidbodyBP = backpack.GetComponent<Rigidbody>();
                rigidbodyBP.useGravity = false;
                rigidbodyBP.isKinematic = true;
                rigidbodyBP.drag = 0;
                rigidbodyBP.interpolation = RigidbodyInterpolation.Extrapolate;
                backpack.OwnerID = player.userID;

                ItemContainer container = new ItemContainer { playerOwner = player };
                container.ServerInitialize((Item)null, _.GetSlotsBackpack(player));
                if ((Int32)container.uid == 0)
                    container.GiveUID();

                backpack.inventory = container;
                backpack.SetFlag(BaseEntity.Flags.Busy, true);
                backpack.SetFlag(BaseEntity.Flags.Locked, true);
                backpack.Spawn();

                backpack.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                backpack.UpdateNetworkGroup();

                backpack.transform.localPosition = new Vector3(-0.05f, 0.03f, 0f);
                backpack.transform.localRotation = new Quaternion(-3f, 0f, 3f, 0f);

                backpack.SetParent(player, "spine2");
                backpack.ResetRemovalTime(Math.Max(99999999999999999, backpack.CalculateRemovalTime()));
                enabled = true;
            }

            public void OnDestroy() => KillParent();

            public void KillParent()
            {
                enabled = false;
                
                if (!backpack.IsDestroyed)
                    backpack.Kill();
                UnityEngine.GameObject.Destroy(this);
            }
        }

        #endregion

        #endregion
    }
}
