using System;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Newtonsoft.Json.Converters;
using Facepunch;
using VLB;

using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Oxide.Plugins
{
    [Info("NPCVendingManager", "EcoSmile", "1.0.0")]
    class NPCVendingManager : RustPlugin
    {
        static NPCVendingManager ins;
        PluginConfig config;

        public class PluginConfig
        {
            [JsonProperty("Настройка торговых автоматов")]
            public Dictionary<string, List<VendingOrder>> Vending { get; set; }
        }

        public class VendingOrder
        {
            [JsonProperty("Добавить товар?")]
            public bool AddItem;
            [JsonProperty("Название покупаемого товара")]
            public string ItemToBuy { get; set; }
            [JsonProperty("Максимальный стак покупаемого предмета за один раз")] 
            public int BuyngItemMaxAmount { get; set; }
            [JsonProperty("SKINID покупаемого предмета")]
            public ulong BuyngItemSkinID { get; set; }
            [JsonProperty("Количество покупаемого товара за раз")]
            public int BuyngItemAmount { get; set; }
            [JsonProperty("Покупаемый товар это чертёж")]
            public bool BuyngItemIsBP { get; set; }
            [JsonProperty("Название платёжного товара")]
            public string PayItemShortName { get; set; }
            [JsonProperty("Количество платёжного товара за раз")]
            public int PayItemAmount { get; set; }
            [JsonProperty("Платёжный товар это чертёж")]
            public bool PayItemIsBP { get; set; }
        } 

        protected override void LoadDefaultConfig()
        { 
            config = new PluginConfig
            {
                Vending = GetVendingList(),
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            if (config.Vending.Count == 0)
            {
                LoadDefaultConfig();
                SaveConfig();
            } 
        }

        protected override void SaveConfig()
        { 
            Config.WriteObject(config);
        }

        private void OnServerInitialized()
        {
            ins = this;
            LoadConfig();
            VendingInit();
        }

        void Unload()
        {

        } 

        public Dictionary<string, List<VendingOrder>> GetVendingList()
        {
            Dictionary<string, List<VendingOrder>> vendings = new Dictionary<string, List<VendingOrder>>();
            var vendingList = BaseNetworkable.serverEntities.OfType<NPCVendingMachine>().Where(x => x != null && x.OwnerID == 0).ToList();
            foreach (var vending in vendingList)
            {
                if (vendings.ContainsKey(vending.shopName)) continue;
                vendings.Add(vending.shopName, new List<VendingOrder>());
                vendings[vending.shopName].Add(new VendingOrder() { AddItem = false, ItemToBuy = "", BuyngItemMaxAmount = 10, BuyngItemAmount = 0, BuyngItemSkinID = 0, BuyngItemIsBP = false, PayItemShortName = "", PayItemAmount = 0, PayItemIsBP = false });

            }

            return vendings;
        }

        void VendingInit()
        {
            var vendingList = BaseNetworkable.serverEntities.OfType<NPCVendingMachine>().Where(x => x != null && x.OwnerID == 0).ToList();

            foreach (var vending in vendingList)
            {
                if (config.Vending.ContainsKey(vending.shopName))
                {
                    vending.CancelInvoke(vending.InstallFromVendingOrders);
                    vending.InstallFromVendingOrders();
                    foreach (var item in config.Vending[vending.shopName])
                    {
                        if (!item.AddItem) continue;
                        ItemDefinition itemDefinition = ItemManager.FindItemDefinition(item.ItemToBuy);
                        if(itemDefinition == null)
                        {
                            PrintError($"ItemDefinition from BuyItem ShortName {item.ItemToBuy} not found.");
                            continue;
                        }
                        ItemDefinition x = ItemManager.FindItemDefinition(item.PayItemShortName);
                        if(x==null)
                        {
                            PrintError($"ItemDefinition from PayItem ShortName {item.PayItemAmount} not found.");
                            continue;
                        }
                        AddItemForSale(vending, itemDefinition.itemid, item.BuyngItemAmount, x.itemid, item.PayItemAmount, vending.GetBPState(item.BuyngItemIsBP, item.PayItemIsBP), item.BuyngItemMaxAmount, item.BuyngItemSkinID);
                    }
                }
            }
        }
        
        public void AddItemForSale(VendingMachine vending, int itemID, int amountToSell, int currencyID, int currencyPerTransaction, byte bpState, int maxByStack, ulong SkinID)
        {
            vending.AddSellOrder(itemID, amountToSell, currencyID, currencyPerTransaction, bpState);
            vending.transactionActive = true;
            if (bpState == 1 || bpState == 3)
            {
                for (int i = 0; i < maxByStack; i++)
                {
                    global::Item item = ItemManager.CreateByItemID(vending.blueprintBaseDef.itemid, 1, 0UL);
                    item.blueprintTarget = itemID;
                    vending.inventory.Insert(item);
                }
            }
            else
            {   
                vending.inventory.AddItem(ItemManager.FindItemDefinition(itemID), amountToSell * maxByStack, SkinID);
            }
            vending.transactionActive = false;
            vending.RefreshSellOrderStockLevel(null);
        }
    }  
} 
