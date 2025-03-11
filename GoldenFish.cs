using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("GoldenFish", "Mercury & DezLife", "1.0.831")]
    [Description("RF")]
    class GoldenFish : RustPlugin
    {    
        #region CONFIG

        public class CustomItem
        {
            [JsonProperty("Отображаемое имя", Order = 0)]
            public string DisplayName;
            [JsonProperty("Этот парамтр менять не нужно", Order = 1)]
            public string ShortName;

            [JsonProperty("Шанс выпадения", Order = 2)]
            public int DropChance;
            [JsonProperty("Кол-вл выпадения", Order = 3)]
            public int DropAmount;

            [JsonProperty("Скин ID рыбки", Order = 4)]
            public ulong SkinId = 1686591036;

            [JsonProperty("Призы за переработку", Order = 5)]
            public List<ItemGiveInfo> GiveItems = new List<ItemGiveInfo>();

            public Item Copy(int amount = 1)
            {
                Item x = ItemManager.CreateByPartialName(ShortName, amount);
                x.skin = SkinId;
                x.name = DisplayName;

                return x;
            }

            public Item CreateItem(int amount)
            {
                Item item = ItemManager.CreateByPartialName(ShortName, amount);
                item.name = DisplayName;
                item.skin = SkinId;
                return item;
            }
        }

        public class ItemGiveInfo
        {
            [JsonProperty("Шортнейм предмета")] public string shortname;
            [JsonProperty("СкинИД предмета")] public ulong skinID;
            [JsonProperty("Минимальное количество предметов")] public int Minamount;
            [JsonProperty("Максимальное количество предметов")] public int Maxamount;
        }


        private Configuration _CFG;

        public class Configuration
        {
            [JsonProperty("Настройка плагина")]
            public CustomItem customItem = new CustomItem();
        }
        protected override void LoadDefaultConfig()
        {
            _CFG = new Configuration()
            {
                customItem =  new CustomItem
                {
                    DisplayName = "Золотая рыбка",
                    ShortName = "fish.troutsmall",
                    DropChance = 30,
                    DropAmount = 1,
                    SkinId = 1686591036,
                    GiveItems = new List<ItemGiveInfo>
                    {
                         new ItemGiveInfo
                         {
                             shortname = "stones",
                             skinID = 0U,
                             Minamount = 100,
                             Maxamount = 2000
                         },
                         new ItemGiveInfo
                         {
                             shortname = "metal.refined",
                             skinID = 0U,
                             Minamount = 50,
                             Maxamount = 100
                         },
                         new ItemGiveInfo
                         {
                             shortname = "rifle.m39",
                             skinID = 0U,
                             Minamount = 1,
                             Maxamount = 1
                         },
                         new ItemGiveInfo
                         {
                             shortname = "explosive.satchel",
                             skinID = 0U,
                             Minamount = 1,
                             Maxamount = 3
                         },
                         new ItemGiveInfo
                         {
                             shortname = "surveycharge",
                             skinID = 0U,
                             Minamount = 1,
                             Maxamount = 5
                         },
                    }
                }          
            };
            SaveConfig(_CFG);
        }

        void SaveConfig(Configuration config)
        {
            Config.WriteObject(config, true);
            SaveConfig();
        }

        public void LoadConfigVars()
        {
            _CFG = Config.ReadObject<Configuration>();
            Config.WriteObject(_CFG, true);
        }
        #endregion

        #region command

        [ChatCommand("gf.give")]
        private void CmdChatDebugGoldfSpawn(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
                var item = _CFG.customItem.CreateItem(100);
                item.MoveToContainer(player.inventory.containerMain);
            
        }

        #endregion

        #region hooks
        void OnServerInitialized()
        {
            LoadConfigVars();
            PrintWarning($"--------------------------------- -");
            PrintWarning($"             GoldenFish            ");
            PrintWarning($"     Author = Mercury & DezLife    ");
            PrintWarning($"        vk.com/skyeyeplugins       ");
            PrintWarning($"         Version = {Version}       ");
            PrintWarning($"-----------------------------------");
        }

        private Item OnItemSplit(Item item, int amount)
        {
            var customItem = _CFG.customItem;
            if (customItem.SkinId == item.skin)
            {
                if (customItem != null)
                {
                    Item x = ItemManager.CreateByPartialName(customItem.ShortName, amount);
                    x.name = customItem.DisplayName;
                    x.skin = customItem.SkinId;
                    x.amount = amount;

                    item.amount -= amount;
                    return x;
                }
            }
            return null;
        }

        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.GetItem().skin != targetItem.GetItem().skin)
            if (item.GetItem().skin == _CFG.customItem.SkinId || targetItem.GetItem().skin == _CFG.customItem.SkinId) return false;
            return null;
        }

        object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin != targetItem.skin)
            if (item.skin == _CFG.customItem.SkinId || targetItem.skin == _CFG.customItem.SkinId) return false;
            return null;
        }

        #endregion

        List<ulong> Players = new List<ulong>();
        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container.ShortPrefabName == "survivalfishtrap.deployed")
            {
                if (Players.Contains(container.net.ID)) return null;
                Players.Add(container.net.ID);
                container.SetFlag(BaseEntity.Flags.Busy, true);
            }
            return null;
        }

        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (entity.ShortPrefabName == "survivalfishtrap.deployed")
            {
                if (Players.Contains(entity.net.ID))
                {
                    Players.Remove(entity.net.ID);
                    entity.SetFlag(BaseEntity.Flags.Busy, false);
                }
            }
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container.entityOwner == null) return;
            if (container.entityOwner.ShortPrefabName == "survivalfishtrap.deployed" && item.info.shortname == "fish.troutsmall")
            {
                if (Players.Contains(container.entityOwner.net.ID)) return;

                bool goodChance = UnityEngine.Random.Range(0, 100) >= (100 - _CFG.customItem.DropChance);
                if (goodChance)
                {
                    item.skin = _CFG.customItem.SkinId;
                    item.name = _CFG.customItem.DisplayName;
                }    
            }
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item == null || action == null || action == "")
                return null;
            if (item.info.shortname != "fish.troutsmall")
                return null;
            if (action != "Gut")
                return null;
            if (player == null)
                return null;
            if (_CFG.customItem.SkinId != item.skin) return null;

            var iteItemsGives = _CFG.customItem.GiveItems;

            int RandomItem = UnityEngine.Random.Range(0, _CFG.customItem.GiveItems.Count);
            Item itemS = ItemManager.CreateByName(iteItemsGives[RandomItem].shortname, UnityEngine.Random.Range(iteItemsGives[RandomItem].Minamount, iteItemsGives[RandomItem].Maxamount), iteItemsGives[RandomItem].skinID);

            if (!player.inventory.containerMain.IsFull()) itemS.MoveToContainer(player.inventory.containerMain);
            else player.GiveItem(itemS, BaseEntity.GiveItemReason.Generic);
            ItemRemovalThink(item, player, 1);
            return false;

        }
        private static void ItemRemovalThink(Item item, BasePlayer player, int itemsToTake)
        {
            if (item.amount == itemsToTake)
            {
                item.RemoveFromContainer();
                item.Remove();
            }
            else
            {
                item.amount = item.amount - itemsToTake;
                player.inventory.SendSnapshot();
            }
        }
    }
}
