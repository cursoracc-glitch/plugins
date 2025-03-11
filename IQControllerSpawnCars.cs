using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Rust.Modular;

namespace Oxide.Plugins
{
    [Info("IQControllerSpawnCars", "SkuliDropek", "1.0.3")]
    [Description("Хочу следовать за трендами блин")]
    class IQControllerSpawnCars : RustPlugin
    {
        // - Исправил NRE
        /// <summary>
        /// Обновление 0.0.4
        /// - Заменил хук OnEntitySpawned на OnVehicleModulesAssigned
        /// - Исправил некорректное заполнение машин заспавненых до загрузки плагина
        /// - Добавил заполнение уже созданных (до загрузки плагина) машин! (Если они пустые)
        /// Обновление 1.0.1
        /// - Добавлена возможность заспавнить топливо в транспорте
        /// </summary>

        #region Vars
        public enum SpawnType
        {
            TierFull,
            ElementsTier
        }
        #endregion

        #region Configuration 
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Выберите тип спавна. 0 - полный спавн по тирам(настраивайте шансы и тиры в листе)(т.е все детали сразу,в разном виде качества). 1 - Спавн отдельных деталей, с ограничениями в количестве и рандомным качеством в зависимости от шанса")]
            public SpawnType spawnType;
            [JsonProperty("Настройки тиров. Номер тира(1-3) и шанс.")]
            public Dictionary<Int32, Int32> TierRare = new Dictionary<Int32, Int32>();
            [JsonProperty("Настройка деталей и их шанс спавна.Шортнейм детали и шанс ее спавна")]
            public Dictionary<String, Int32> ElementSpawnRare = new Dictionary<String, Int32>();
            [JsonProperty("Ограниченное количество спавна деталей. 0 - без ограничений")]
            public Int32 LimitSpawnElement;

            [JsonProperty("Настройка заполнения топливом машин")]
            public FuelSettings fuelSettings = new FuelSettings();
            internal class FuelSettings
            {
                [JsonProperty("Включить спавн топлива в машинах (true - да/false - нет)")]
                public Boolean UseFuelSpawned = true;
                [JsonProperty("Статичное количество топлива (Если включен рандом, то этот показатель не будет учитываться)")]
                public Int32 FuelStatic = 100;
                [JsonProperty("Шанс заполнения топливом транспорт (0-100)")]
                public Int32 RareUsing = 100;

                [JsonProperty("Настройка рандомного спавна топлива")]
                public RandomFuel randomFuel = new RandomFuel();

                internal class RandomFuel
                {
                    [JsonProperty("Включить рандомное количество спавна топлива (true - да/false - нет)")]
                    public Boolean UseRandom = false;
                    [JsonProperty("Минимальное количество топлива")]
                    public Int32 MinFuel = 30;
                    [JsonProperty("Максимальное количество топлива")]
                    public Int32 MaxFuel = 200;
                }

                public Int32 GetFuelSpawned()
                {
                    Int32 Fuel = 0;

                    if(UseFuelSpawned)
                        if (Oxide.Core.Random.Range(0, 100) <= RareUsing)
                        {
                            if (randomFuel.UseRandom)
                                Fuel = Oxide.Core.Random.Range(randomFuel.MinFuel, randomFuel.MaxFuel);
                            else Fuel = FuelStatic;
                        }

                    return Fuel;
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    spawnType = SpawnType.TierFull,
                    fuelSettings = new FuelSettings
                    {
                        FuelStatic = 100,
                        RareUsing = 100,
                        UseFuelSpawned = true,
                        randomFuel = new FuelSettings.RandomFuel
                        {
                            MinFuel = 30,
                            MaxFuel = 200,
                            UseRandom = false,
                        }
                    },
                    TierRare = new Dictionary<Int32, Int32>
                    {
                        [1] = 80,
                        [2] = 50,
                        [3] = 25,
                    },
                    ElementSpawnRare = new Dictionary<String, Int32>
                    {
                        ["carburetor1"] = 80,
                        ["crankshaft1"] = 80,
                        ["sparkplug1"] = 80,
                        ["piston1"] = 80,       
                        ["carburetor2"] = 60,
                        ["crankshaft2"] = 60,
                        ["sparkplug2"] = 60,
                        ["piston2"] = 60,
                        ["carburetor3"] = 20,
                        ["crankshaft3"] = 20,
                        ["sparkplug3"] = 20,
                        ["piston3"] = 20, 
                    },
                    LimitSpawnElement = 0,
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
                PrintWarning($"Ошибка чтения #1434 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!"); //
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        private void OnServerInitialized()
        {
            foreach(var Entity in BaseNetworkable.serverEntities.entityList.Where(x => x.Value.ShortPrefabName.Contains("module_car") && x.Value?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory.itemList.Count == 0))
            {
                ModularCar Car = Entity.Value as ModularCar;
                CarUpgrade(Car);
            }
        }
        void OnVehicleModulesAssigned(ModularCar Car, ItemModVehicleModule[] modulePreset) => CarUpgrade(Car);
        private void CarUpgrade(ModularCar Car)
        {
            if (Car == null) return;
            var CarSetting = config.spawnType;
            switch (CarSetting)
            {
                case SpawnType.TierFull:
                    {
                        SpawnFullTier(Car);
                        break;
                    }
                case SpawnType.ElementsTier:
                    {
                        SpawnElementsTier(Car);
                        break;
                    }
                default: { break; }
            }

            FuelSystemSpawned(Car.GetFuelSystem());
        }

        #region Metods
        private void FuelSystemSpawned(EntityFuelSystem FuelSystem)
        {
            if (FuelSystem == null) return;
            Int32 FuelAmount = config.fuelSettings.GetFuelSpawned();
            if (FuelAmount == 0) return;

            NextTick(() =>
            {
                StorageContainer FuelContainer = FuelSystem.fuelStorageInstance.Get(true);
                if (FuelContainer == null || FuelContainer.inventory == null) return;
                Item FuelItem = ItemManager.CreateByName("lowgradefuel", FuelAmount);
                if (FuelItem == null) return;
                FuelItem.MoveToContainer(FuelContainer.inventory);
            });
        }

        void SpawnFullTier(ModularCar Car)
        {
            var CarSetting = config.TierRare;
            if (Car == null) return;
            foreach (var Tier in CarSetting)
            {
                if (!IsRare(Tier.Value)) continue;
                if (Car.GetComponentInChildren<VehicleModuleEngine>() == null) continue;
                Car?.GetComponentInChildren<VehicleModuleEngine>()?.AdminFixUp(Tier.Key);
            }
        }

        void SpawnElementsTier(ModularCar Car)
        {
            if (Car == null) return;
            var CarSetting = config.ElementSpawnRare;
            Int32 LimitElement = (Int32)(config.LimitSpawnElement != 0 ? config.LimitSpawnElement : Car?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory.capacity);
            for (Int32 j = 0; j < LimitElement; j++)
            {
                Int32 SlotElemt = UnityEngine.Random.Range(0, (Int32)(Car?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory.capacity));
                foreach (var TierElement in CarSetting)
                {
                    if (!IsRare(TierElement.Value)) continue;
                    if (Car?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory.GetSlot(SlotElemt) != null) continue;

                    Item ElementCar = ItemManager.CreateByName(TierElement.Key, 1);
                    if (Car.GetComponentInChildren<EngineStorage>().ItemFilter(ElementCar, j))
                        ElementCar.MoveToContainer(Car?.GetComponentInChildren<VehicleModuleEngine>()?.GetContainer()?.inventory, j, false);
                }
            }
        }
        public bool IsRare(Int32 Rare)
        {
            if (UnityEngine.Random.Range(0, 100) >= (100 - Rare))
                return true;
            else return false;
        }
        #endregion
    }
}
