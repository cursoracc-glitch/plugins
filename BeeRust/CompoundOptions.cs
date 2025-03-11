using System.Collections.Generic;
using System.Linq;
using Rust.Ai;
using Oxide.Core;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Compound Options", "FastBurst", "1.2.5")]
    [Description("Compound monument options")]
    class CompoundOptions : RustPlugin
    {
        #region Vars
        private bool dataChanged;
        private StorageData data;
        private StorageData defaultOrders;
        #endregion

        #region Oxide hooks
        private void Loaded()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<StorageData>(Name);
                defaultOrders = Interface.Oxide.DataFileSystem.ReadObject<StorageData>(Name + "_default");
            }
            catch { }

            if (data == null)
            {
                data = new StorageData();
            }
            if (defaultOrders == null)
            {
                defaultOrders = new StorageData();
            }

            if (data.VendingMachinesOrders == null)
            {
                data.VendingMachinesOrders = new Dictionary<string, Order[]>();
            }
            if (defaultOrders.VendingMachinesOrders == null)
            {
                defaultOrders.VendingMachinesOrders = new Dictionary<string, Order[]>();
            }
        }

        private void Unload()
        {
            foreach (var entity in BaseNetworkable.serverEntities.ToList())
            {
                if (entity is NPCVendingMachine)
                {
                    var vending = entity as NPCVendingMachine;
                    if (configData.General.allowConsoleOutput)
                        Puts($"Restoring default orders for {vending.ShortPrefabName}");
                    if (defaultOrders.VendingMachinesOrders != null)
                    {
                        vending.vendingOrders.orders = GetDefaultOrders(vending);
                        vending.InstallFromVendingOrders();
                    }
                }
            }
        }

        private void Init()
        {
            Unsubscribe(nameof(OnEntitySpawned));
            //LoadVariables();
        }

        private void OnServerInitialized()
        {
            Subscribe(nameof(OnEntitySpawned));

            foreach (var entity in BaseNetworkable.serverEntities.ToList())
            {
                if (entity is NPCVendingMachine)
                {
                    var vending = entity as NPCVendingMachine;
                    AddVendingOrders(vending, true);
                    UpdateVending(vending);
                }
                else if (entity is NPCPlayer)
                {
                    KillNPCPlayer(entity as NPCPlayer);
                }
                else if (entity is NPCAutoTurret)
                {
                    ProcessNPCTurret(entity as NPCAutoTurret);
                }
            }

            //LoadVariables();
            SaveData();
        }

        private void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
        {
            if (!(trigger is TriggerSafeZone) && !(entity is BasePlayer)) return;

            var safeZone = trigger as TriggerSafeZone;
            if (safeZone == null) return;

            safeZone.enabled = !configData.General.disableCompoundTrigger;
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is NPCVendingMachine)
            {
                UpdateVending(entity as NPCVendingMachine);
                SaveData();
            }
            else if (entity is NPCPlayer)
            {
                KillNPCPlayer(entity as NPCPlayer);
            }
            else if (entity is NPCAutoTurret)
            {
                ProcessNPCTurret(entity as NPCAutoTurret);
            }
        }
        #endregion

        #region Implementation
        private void KillNPCPlayer(NPCPlayer npcPlayer)
        {
            var npcSpawner = npcPlayer.gameObject.GetComponent<ScientistSpawner>();
            if (npcSpawner == null) return;

            if (npcSpawner.IsMilitaryTunnelLab && configData.General.disallowCompoundNPC || npcSpawner.IsBandit && configData.General.disallowBanditNPC)
            {
                if (!npcPlayer.IsDestroyed) npcPlayer.Kill(BaseNetworkable.DestroyMode.Gib);
            }
        }

        private void ProcessNPCTurret(NPCAutoTurret npcAutoTurret)
        {
            npcAutoTurret.SetFlag(NPCAutoTurret.Flags.On, !configData.General.disableCompoundTurrets, !configData.General.disableCompoundTurrets);
            npcAutoTurret.UpdateNetworkGroup();
            npcAutoTurret.SendNetworkUpdateImmediate();
        }

        private void AddVendingOrders(NPCVendingMachine vending, bool def = false)
        {
            if (vending == null || vending.IsDestroyed)
            {
                Puts("Null or destroyed machine...");
                return;
            }
            if (!def)
            {
                if (data.VendingMachinesOrders.ContainsKey(vending.vendingOrders.name))
                {
                    return;
                }
            }
            List<Order> orders = new List<Order>();
            foreach (var order in vending.vendingOrders.orders)
            {
                orders.Add(new Order
                {
                    _comment = $"Sell {order.sellItem.displayName.english} x {order.sellItemAmount} for {order.currencyItem.displayName.english} x {order.currencyAmount}",
                    sellAmount = order.currencyAmount,
                    currencyAmount = order.sellItemAmount,
                    sellId = order.sellItem.itemid,
                    sellAsBP = order.sellItemAsBP,
                    currencyId = order.currencyItem.itemid,
                    weight = 100,
                    refillAmount = 100000,
                    refillDelay = 0.0f
                });
            }
            if (def)
            {
                if (orders == null) return;

                if (configData.General.allowConsoleOutput)
                    Puts($"Trying to save default vendingOrders for {vending.vendingOrders.name}");

                if (defaultOrders == null) defaultOrders = new StorageData();
                if (defaultOrders.VendingMachinesOrders.ContainsKey(vending.vendingOrders.name)) return;
                defaultOrders.VendingMachinesOrders.Add(vending.vendingOrders.name, orders.ToArray());
            }
            else
            {
                data.VendingMachinesOrders.Add(vending.vendingOrders.name, orders.ToArray());
            }
            if (configData.General.allowConsoleOutput)
                Puts($"Added Vending Machine: {vending.vendingOrders.name} to data file!");
            dataChanged = true;
        }

        private void UpdateVending(NPCVendingMachine vending)
        {
            if (vending == null || vending.IsDestroyed)
            {
                return;
            }

            AddVendingOrders(vending);
            NextTick(() =>
            {
                vending.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                vending.SendNetworkUpdateImmediate();
            });

            if (configData.General.disableCompoundVendingMachines)
            {
                vending.ClearSellOrders();
                vending.inventory.Clear();
            }
            else if (configData.General.allowCustomCompoundVendingMachines)
            {
                vending.vendingOrders.orders = GetNewOrders(vending);
                vending.InstallFromVendingOrders();
            }
        }

        private NPCVendingOrder.Entry[] GetDefaultOrders(NPCVendingMachine vending)
        {
            List<NPCVendingOrder.Entry> temp = new List<NPCVendingOrder.Entry>();
            foreach (var order in defaultOrders.VendingMachinesOrders[vending.vendingOrders.name])
            {
                temp.Add(new NPCVendingOrder.Entry
                {
                    currencyAmount = order.sellAmount,
                    currencyAsBP = order.currencyAsBP,
                    currencyItem = ItemManager.FindItemDefinition(order.currencyId),
                    sellItem = ItemManager.FindItemDefinition(order.sellId),
                    sellItemAmount = order.currencyAmount,
                    sellItemAsBP = order.sellAsBP,
                    weight = 100,
                    refillAmount = 100000,
                    refillDelay = 0.0f
                });
            }
            return temp.ToArray();
        }

        private NPCVendingOrder.Entry[] GetNewOrders(NPCVendingMachine vending)
        {
            List<NPCVendingOrder.Entry> temp = new List<NPCVendingOrder.Entry>();
            foreach (var order in data.VendingMachinesOrders[vending.vendingOrders.name])
            {
                ItemDefinition currencyItem = ItemManager.FindItemDefinition(order.currencyId);
                if (currencyItem == null)
                {
                    PrintError($"Item id {order.currencyId} is invalid. Skipping sell order.");
                    continue;
                }

                ItemDefinition sellItem = ItemManager.FindItemDefinition(order.sellId);
                if (sellItem == null)
                {
                    PrintError($"Item id {order.sellId} is invalid. Skipping sell order.");
                    continue;
                }

                temp.Add(new NPCVendingOrder.Entry
                {
                    currencyAmount = order.sellAmount,
                    currencyAsBP = order.currencyAsBP,
                    currencyItem = currencyItem,
                    sellItem = sellItem,
                    sellItemAmount = order.currencyAmount,
                    sellItemAsBP = order.sellAsBP,
                    weight = 100,
                    refillAmount = 100000,
                    refillDelay = 0.0f
                });
            }
            return temp.ToArray();
        }
        #endregion       

        #region Commmands
        [ChatCommand("compreset")]
        private void cmdCompReset(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "You doesn't have permission to that!");
                return;
            }

            Interface.Oxide.ReloadPlugin(Name);
        }

        [ConsoleCommand("compreset")]
        private void ccmdCompReset(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            if (arg.IsAdmin == false)
            {
                Puts("You doesn't have permission to that!");
                return;
            }

            Interface.Oxide.ReloadPlugin(Name);
        }
        #endregion

        #region Save data classes
        private class StorageData
        {
            public Dictionary<string, Order[]> VendingMachinesOrders { get; set; }
        }

        private class Order
        {
            public string _comment;
            public int sellId;
            public int sellAmount;
            public bool sellAsBP;
            public int currencyId;
            public int currencyAmount;
            public bool currencyAsBP;
            public int weight;
            public int refillAmount;
            public float refillDelay;
        }
        private void SaveData()
        {
            if (dataChanged)
            {
                Interface.Oxide.DataFileSystem.WriteObject(Name, data);
                Interface.Oxide.DataFileSystem.WriteObject(Name + "_default", defaultOrders);
                dataChanged = false;
            }
        }

        #endregion

        #region Config
        private static ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "General Settings")]
            public GeneralSettings General { get; set; }

            public class GeneralSettings
            {
                [JsonProperty(PropertyName = "Allow console status outputs")]
                public bool allowConsoleOutput { get; set; }
                [JsonProperty(PropertyName = "Allow custom sell list for Compound vending machines (see in data)")]
                public bool allowCustomCompoundVendingMachines { get; set; }
                [JsonProperty(PropertyName = "Disallow Bandit NPC")]
                public bool disallowBanditNPC { get; set; }
                [JsonProperty(PropertyName = "Disallow Compound NPC")]
                public bool disallowCompoundNPC { get; set; }
                [JsonProperty(PropertyName = "Disable Compound Turrets")]
                public bool disableCompoundTurrets { get; set; }
                [JsonProperty(PropertyName = "Disable Compound SafeZone trigger")]
                public bool disableCompoundTrigger { get; set; }
                [JsonProperty(PropertyName = "Disable Compound Vending Machines")]
                public bool disableCompoundVendingMachines { get; set; }
            }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();
        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                General = new ConfigData.GeneralSettings
                {
                    allowConsoleOutput = true,
                    allowCustomCompoundVendingMachines = true,
                    disallowBanditNPC = false,
                    disallowCompoundNPC = false,
                    disableCompoundTurrets = false,
                    disableCompoundTrigger = false,
                    disableCompoundVendingMachines = false
                },
                Version = Version
            };
        }
        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();
            if (configData.Version < new Core.VersionNumber(1, 2, 5))
            {
                configData = baseConfig;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }
        #endregion
    }
}