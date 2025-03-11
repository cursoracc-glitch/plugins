using Oxide.Core;
using System.Linq;
using System;

using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("Vending In Stock", "AVOcoder / rostov114", "1.1.0")]
    [Description("VendingMachines sell-orders always in stock")]
    class VendingInStock : RustPlugin
    {
        #region Configuration
        private Configuration _config;
        private class Configuration
        {
            [JsonProperty(PropertyName = "Disable native refill")]
            public bool disableNativeRefill = false;

            [JsonProperty(PropertyName = "Do not refill items")]
            public string[] noRefillItems =
            { 
                "put item shortname here"
            };

            [JsonProperty(PropertyName = "Do not refill vendings")]
            public string[] noRefillVendings =
            {
                "put vending orders name here (see console command >>> vending_orders_name <<<)"
            };

            public bool NoRefillItem(Item item)
            {
                if (noRefillItems == null)
                    return false;

                return noRefillItems.Contains(item.info.shortname);
            }

            public bool NoRefillVending(NPCVendingMachine vm)
            {
                if (noRefillVendings == null || vm.vendingOrders == null || vm.vendingOrders.name == null)
                    return false;

                return noRefillVendings.Contains(vm.vendingOrders.name);
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                SaveConfig();
            }
            catch
            {
                PrintError("Error reading config, please check!");

                Unsubscribe(nameof(OnServerInitialized));
                Unsubscribe(nameof(Unload));
                Unsubscribe(nameof(CanPurchaseItem));
            }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            if (!_config.disableNativeRefill)
                return;

            foreach (NPCVendingMachine vm in BaseNetworkable.serverEntities.Where(e => e is NPCVendingMachine))
            {
                timer.Once(1f, () =>
                {
                    if (vm == null || vm.IsDestroyed || _config.NoRefillVending(vm)) 
                        return;

                    foreach (Item item in vm.inventory.itemList)
                    {
                        if (_config.NoRefillItem(item))
                            PrintWarning($"Native refill disabled and enabled not refill item! Please disable 'Disable native refill' OR add vending in 'Do not refill vendings'! Vending name: {vm.shopName}, Item: {item.info.shortname}");
                    }

                    vm.CancelInvoke(new Action(vm.Refill));
                });
            }
        }

        private void Unload()
        {
            if (!_config.disableNativeRefill)
                return;

            foreach (NPCVendingMachine vm in BaseNetworkable.serverEntities.Where(e => e is NPCVendingMachine))
            {
                if (_config.NoRefillVending(vm)) 
                    continue;

                vm.InvokeRandomized(new Action(vm.Refill), 1f, 1f, 0.1f);
            }
        }

        private void CanPurchaseItem(BasePlayer buyer, Item soldItem, Action<BasePlayer, Item> onItemPurchased, NPCVendingMachine vm)
        {
            if (vm == null || soldItem == null || soldItem.info == null)
                return;

            if (_config.NoRefillVending(vm))
                return; 

            if (_config.NoRefillItem(soldItem))
            {
                if (_config.disableNativeRefill)
                    PrintWarning($"Native refill disabled and enabled not refill item! Please disable 'Disable native refill' OR add vending in 'Do not refill vendings'! Vending name: {vm.shopName}, Item: {soldItem.info.shortname}");

                return;
            }

            if (Interface.CallHook("CanVendingStockRefill", vm, soldItem, buyer) != null)
                return;

            Item item = ItemManager.Create(soldItem.info, soldItem.amount, soldItem.skin);
            if (soldItem.blueprintTarget != 0)
                item.blueprintTarget = soldItem.blueprintTarget;

            if (soldItem.instanceData != null)
                item.instanceData.dataInt = soldItem.instanceData.dataInt;

            NextTick(() =>
            {
                if (item == null)
                    return;

                if (vm == null || vm.IsDestroyed) {
                    item.Remove(0f);
                    return;
                }

                vm.transactionActive = true;
                if (!item.MoveToContainer(vm.inventory, -1, true))
                    item.Remove(0f);

                vm.transactionActive = false;
                vm.FullUpdate();
            });
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("vending_orders_name")]
        private void vending_orders_name(ConsoleSystem.Arg arg)
        {
            BasePlayer p = arg?.Player() ?? null; 
            if (p != null && !p.IsAdmin) 
                return;

            TextTable textTable = new TextTable();
            textTable.AddColumn("Vending name");
            textTable.AddColumn("Vending orders name (use plugin config)");

            List<string> _cache = Pool.GetList<string>();
            foreach (NPCVendingMachine vm in BaseNetworkable.serverEntities.Where(e => e is NPCVendingMachine))
            {
                if (vm == null || vm.vendingOrders == null || vm.vendingOrders.name == null || _cache.Contains(vm.vendingOrders.name))
                    continue;

                _cache.Add(vm.vendingOrders.name);
                textTable.AddRow(new string[]
                {
                    vm.shopName,
                    vm.vendingOrders.name
                });
            }

            Pool.FreeList<string>(ref _cache);
            arg.ReplyWith(textTable.ToString());
        }
        #endregion
    }
}