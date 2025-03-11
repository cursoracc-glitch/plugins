using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Fast Ovens", "Orange", "1.0.6")]
    [Description("Make your ovens smelt faster")]
    public class FastOvens : RustPlugin
    {
        #region Oxide Hooks

        private void OnFuelConsume(BaseOven oven, Item item, ItemModBurnable burnable)
        {
            SmeltItems(oven);
        }

        #endregion

        #region Core

        private void SmeltItems(BaseOven oven)
        {
            var isCampfire = oven.inventory.capacity != 6 && oven.inventory.capacity != 18;
            if (isCampfire == true && config.workWithCampfires == false)
            {
                return;
            }

            foreach (var item in oven.inventory.itemList.ToArray())
            {
                var rate = GetRate(item.info.shortname);
                if (rate < 2)
                {
                    continue;
                }
                
                var resultModifier = 1;
                var resultDef = (ItemDefinition) null;
                var cookable = item.info.GetComponent<ItemModCookable>();
                if (cookable != null)
                {
                    resultDef = cookable.becomeOnCooked;
                    resultModifier = cookable.amountOfBecome;
                }
                else
                {
                        var burnable = item.info.GetComponent<ItemModBurnable>();
                        if (burnable != null)
                        {
                            resultDef = burnable.byproductItem;
                            resultModifier = burnable.byproductAmount * config.charcoalMultiplier;
                        }
                }

                if (config.stopBurningFood == true && resultDef != null && resultDef.shortname.EndsWith(".burned"))
                {
                    return;
                }

                Smelt(oven, item, rate, resultDef, resultModifier);
            }
        }

        private static void Smelt(BaseOven oven, Item cookingItem, int smeltRate, ItemDefinition targetDef, int targetMultiplier)
        {
            if (targetDef == null)
            {
                return;
            }

            if (targetMultiplier == 0)
            {
                targetMultiplier = 1;
            }

            var amount = 0;
            if (cookingItem.amount > smeltRate)
            {
                cookingItem.amount -= smeltRate;
                cookingItem.MarkDirty();
                amount = smeltRate;
            }
            else
            {
                cookingItem.GetHeldEntity()?.Kill();
                cookingItem.DoRemove();
                amount = cookingItem.amount;
            }

            if (amount == 0)
            {
                return;
            }

            var obj = ItemManager.Create(targetDef, amount * targetMultiplier);
            if (obj.MoveToContainer(oven.inventory) == false)
            {
                obj.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
            }
        }

        private int GetRate(string shortname)
        {
            var rate = 0;
            
            if (config.rates.TryGetValue(shortname, out rate) == true)
            {
                return rate;
            }
            
            if (shortname.Contains(".raw") && config.rates.TryGetValue("meat", out rate) == true)
            {
                return rate;
            }

            if (shortname.Contains(".ore") && config.rates.TryGetValue("ore", out rate) == true)
            {
                return rate;
            }
            
            if (config.rates.TryGetValue("*", out rate) == true)
            {
                return rate;
            }
            
            return 0;
        }

        #endregion

        #region Configuration | 2.0.0

        private static ConfigData config = new ConfigData();

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Work with campfires")]
            public bool workWithCampfires = true;

            [JsonProperty("Stop burning food")]
            public bool stopBurningFood = true;

            [JsonProperty(PropertyName = "Charcoal multiplier")]
            public int charcoalMultiplier = 2;
            
            [JsonProperty(PropertyName = "Rates")] 
            public Dictionary<string, int> rates = new Dictionary<string, int>();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }

                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private static void ValidateConfig()
        {
            if (config.rates.Count == 0)
            {
                config.rates = new Dictionary<string, int>
                {
                    {"ore", 10},
                    {"meat", 5},
                    {"hq.metal.ore", 5},
                    {"wood", 5},
                    {"*", 1},
                };
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}