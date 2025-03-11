using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("UltraniumOre", "CASHR", "1.0.0")]
    public class UltraniumOre : RustPlugin
    {
        #region Configuration
        private enum Type
        {
            None,
            Gather,
            Oven
        }
        [JsonProperty("Тут трогать только DisplayName")]
        private Dictionary<Type, CustomItem> Items = new Dictionary<Type, CustomItem>
        {
            [Type.Gather] = new CustomItem
            {
                DisplayName = "Ультраниумная руда",
                ShortName = "coal",
                SkinID = 1719213706
            },
            [Type.Oven] = new CustomItem
            {
                DisplayName = "Ультраниум обработанный",
                ShortName = "ducttape",
                SkinID = 1838446313
            },

        };
        private class CustomItem
        {
            public string DisplayName;
            public string ShortName;
            public ulong SkinID;

            public Item CreateItem(int amount)
            {
                Item item = ItemManager.CreateByPartialName(ShortName, amount);
                item.name = DisplayName;
                item.skin = SkinID;

                if (item.info.GetComponent<ItemModCookable>() != null)
                    item.info.GetComponent<ItemModCookable>().OnItemCreated(item);

                return item;
            }
        }




        #endregion
        private class Configuration
        {
            [JsonProperty("Шанс выпадения руды")]
            public int Chance = 10;

            public static Configuration GetNewConf()
            {
                return new Configuration();
            }

        }
        protected override void LoadDefaultConfig() => _config = Configuration.GetNewConf();
        protected override void SaveConfig() => Config.WriteObject(_config);
        private Configuration _config = new Configuration();
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        #region Oxide Hooks
        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (item.info.shortname == Items[Type.Oven].ShortName && item.skin == 0)
            {
                item.skin = Items[Type.Oven].SkinID;
                item.name = Items[Type.Oven].DisplayName;
            }
        }

        void OnServerInitialized()
        {
            LoadConfig();
            var itemInfo = ItemManager.FindItemDefinition(Items[Type.Gather].ShortName);
            if (itemInfo.GetComponent<ItemModCookable>() == null) itemInfo.gameObject.AddComponent<ItemModCookable>();
            itemInfo.stackable = 5000;
            var _itemInfo = ItemManager.FindItemDefinition(Items[Type.Oven].ShortName);
            _itemInfo.stackable = 5000;
            var burnMod = itemInfo.gameObject.GetComponent<ItemModCookable>();
            burnMod.becomeOnCooked = ItemManager.FindItemDefinition(Items[Type.Oven].ShortName);
            burnMod.amountOfBecome = 1;
            burnMod.highTemp = 1200;
            burnMod.lowTemp = 800;
            burnMod.cookTime = 15;
            PrintWarning("Все файлы созданы успешно, плагин работает!");
        }


        private void CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            if (item.info.shortname == Items[Type.Oven].ShortName && targetPos == -1)
            {
                item.name = Items[Type.Oven].DisplayName;
                item.skin = Items[Type.Oven].SkinID;
            }
        }


        private Item OnItemSplit(Item item, int amount)
        {
            if (item.info.shortname == Items[Type.Gather].ShortName && item.skin == Items[Type.Gather].SkinID)
            {
                Item x = Items[Type.Gather].CreateItem(amount);
                item.amount -= amount;
                item.MarkDirty();
                x.MarkDirty();
                return x;
            }
            if (item.info.shortname == Items[Type.Oven].ShortName && item.skin == Items[Type.Oven].SkinID)
            {
                Item x = Items[Type.Oven].CreateItem(amount);
                item.amount -= amount;
                item.MarkDirty();
                x.MarkDirty();
                return x;
            }
            return null;
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();

            if (dispenser == null || player == null || item == null)
                return;
            if (dispenser.gatherType != ResourceDispenser.GatherType.Ore) return;
            int amount;
            switch (item.info.shortname)
            {
                case "sulfur.ore":
                    if (Core.Random.Range(1, 100) == _config.Chance)
                    {
                        amount = Core.Random.Range(5, 20);
                        Item dropItem = Items[Type.Gather].CreateItem(amount);
                        foreach (var x in player.inventory.AllItems())
                        {
                            if (x.skin == Items[Type.Gather].SkinID)
                            {
                                x.amount += amount;
                                player.SendConsoleCommand($"note.inv {dropItem.info.itemid} {dropItem.amount} \"Ультраниумная руда\"");
                                return;
                            }
                        }
                        if (24 - player.inventory.containerMain.itemList.Count > 0)
                        {
                            dropItem.MoveToContainer(player.inventory.containerMain, -1, true);
                            player.SendConsoleCommand($"note.inv {dropItem.info.itemid} {dropItem.amount} \"Ультраниумная руда\"");
                            return;
                        }
                        else if (6 - player.inventory.containerBelt.itemList.Count > 0)
                        {
                            dropItem.MoveToContainer(player.inventory.containerBelt, -1, true);
                            player.SendConsoleCommand($"note.inv {dropItem.info.itemid} {dropItem.amount} \"Ультраниумная руда\"");
                            return;
                        }
                        else
                        {
                            dropItem.Drop(player.transform.position, Vector3.up);
                            PrintToChat(player, "Ультраниумная руда брошена под ноги!");
                        }
                        return;
                    }
                    break;
            }

        }
        #endregion

    }
}