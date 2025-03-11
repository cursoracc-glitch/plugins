using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RatesController", "", "2.1.3")] 
    [Description("Rate and Time controller - can change rates based on current game time")]

    class RatesController : RustPlugin
    {   
        private bool isDay;

        private string PickUpDayString = "Рейт поднимаемых ресурсов днём";
        private string PickUpNightString = "Рейт поднимаемых ресурсов ночью";
        private string GatherDayString = "Рейт добываемых ресурсов днём";
        private string GatherNightString = "Рейт добываемых ресурсов ночью";
        private string QuerryDayString = "Рейт добываемых ресурсов в карьере днём";
        private string QuerryNightString = "Рейт добываемых ресурсов в карьере ночью";
        private string LootDayString = "Рейт лута днём(если включен)";
        private string LootNightString = "Рейт лута ночью(если включен)";
        private string SmeltDayString = "Скорость работы печей днём";
        private string SmeltNightString = "Скорость работы печей ночью";

        private string DefaultRatesCfg = "Общие рейты ресурсов";
        private string CustomRatesCfg = "Изменение рейтов для игроков с привилегиями";
        private string DefaultPickupRatesCfg = "Стандартные рейты поднимаемых ресурсов";
        private string DefaultGatherRatesCfg = "Стандартные рейты добываемых ресурсов";
        private string DefaultSmeltRatesCfg = "Стандартное время переработки ресурсов (в секундах)";
        private string DefaultQuerryRatesCfg = "Стандартные рейты добываемых ресурсов в карьере";
        private string PrefixCfg = "Префикс в чате";
        private string PrefixColorCfg = "Цвет префикса в чате";
        private string UseLootMultyplierCfg = "Использовать умножение лута(выключите для совместимости с контроллерами лута)";
        private string DayStartCfg = "Час начала дня(игровое время)";
        private string DayLenghtCfg = "Длина дня(в минутах)";
        private string NightStartCfg = "Час начала ночи(игровое время)";
        private string NightLenghtCfg = "Длина ночи(в минутах)";
        private string WarnChatCfg = "Выводить сообщения в чат о начале дня или ночи";
        private string CoalRateDayCfg = "Рейт угля при сжигании дерева днём";
        private string CoalRateNightCfg = "Рейт угля при сжигании дерева ночью";
        private string CoalChanceDayCfg = "Шанс производства угля днём";
        private string CoalChanceNightCfg = "Шанс производства угля ночью";
        private string BlacklistedLootCfg = "Список лута, на который не действуют множители";
        private string MoreHQMCfg = "Добавить металл высокого качества во все рудные жилы";

        private List<string> AvaliableMods;
        private Dictionary<string, float> SmeltBackup = new Dictionary<string, float>();
        //Откат обновления ящиков
        Dictionary<int, DateTime> CratesCD = new Dictionary<int, DateTime>();
        //Список рудных жил и их бонусов
        Dictionary<BaseEntity, Dictionary<string, float>> DefaultFinishBonuses = new Dictionary<BaseEntity, Dictionary<string, float>>();

        private double CoalRate = 1f;
        private int CoalChance = 25;
        private double SmeltRate = 1f;
        private double GatherRate = 1f;
        private double PickupRate = 1f;
        private double QuerryRate = 1f;
        private double LootRate = 1f;

        #region config setup

        private string Prefix = "[Rates controller]";//Префикс плагина в чате
        private bool UseLootMultyplier = true; //Использовать множители лута
        private string PrefixColor = "#ff0000";//Цвет префикса
        private float DayStart = 6f;//Час, когда начинается день
        private float NightStart = 18f;//Час, когда начинается ночь
        private uint DayLenght = 30u;//Длинна дня
        private uint NightLenght = 30u;//Длинна ночи
        private bool WarnChat = true;//Выводить ли в чат оповещения о смене дня\ночи и рейтов.
        private bool MoreHQM = false; //Добавить ли во все рудные жилы мвк?

        //Стандартные рейты, для игроков без привелегий
        private Dictionary<string, double> DefaultRates = new Dictionary<string, double>();
        //Рейты для игроков с привелегиями
        private Dictionary<string, Dictionary<string, double>> CustomRates = new Dictionary<string, Dictionary<string, double>>();

        private double CoalRateDay = 1f;
        private double CoalRateNight = 1f;
        private int CoalChanceDay = 25;
        private int CoalChanceNight = 25;
        private Dictionary<string, double> DefaultSmeltRates = new Dictionary<string, double>();
        private Dictionary<string, double> DefaultGatherRates = new Dictionary<string, double>();
        private Dictionary<string, double> DefaultPickupRates = new Dictionary<string, double>();
        private Dictionary<string, double> DefaultQuerryRates = new Dictionary<string, double>();
        private List<string> BlacklistedLoot = new List<string>();
        
        #endregion

        #region Loading config

        //Загрузка стандартного конфиг-файла. Вызывается ТОЛЬКО в случае отсутствия файла PluginName.json в папке config
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за приобритение плагина на сайте RustPlugin.ru.\n Если вы приобрели плагин в другом месте - вы теряете все гарантии.");
        }

        void LoadConfigValues()
        {
            AvaliableMods = new List<string>()
            {
                GatherDayString,
                GatherNightString,
                PickUpDayString,
                PickUpNightString,
                QuerryDayString,
                QuerryNightString,
                LootDayString,
                LootNightString,
                SmeltDayString,
                SmeltNightString
            };
            Dictionary<string, object> defaultRates = CreatePerms(AvaliableMods, 1f);
            Dictionary<string, object> customRates = new Dictionary<string, object>()
            {
                ["ratescontroller.premium"] = CreatePerms(AvaliableMods, 3f),
                ["ratescontroller.vip"] = CreatePerms(AvaliableMods, 2f)
            };
            Dictionary<string, object> defaultGatherRates = new Dictionary<string, object>()
            {
                { "Animal Fat", 1.0},
                {"Bear Meat", 1.0},
                {"Bone Fragments", 1.0},
                {"Cloth", 1.0},
                {"High Quality Metal Ore", 1.0},
                {"Human Skull", 0.1},
                {"Leather", 1.0},
                {"Metal Ore", 1.0},
                {"Pork", 1.0},
                {"Raw Chicken Breast", 1.0},
                {"Raw Human Meat", 1.0},
                {"Raw Wolf Meat", 1.0},
                {"Stones", 1.0},
                {"Sulfur Ore", 1.0},
                {"Wolf Skull", 1.0},
                {"Wood", 1.0}
            };
            Dictionary<string, object> defaultPickupRates = new Dictionary<string, object>()
            {
                {"Metal Ore", 1.0},
                {"Stones", 1.0},
                {"Sulfur Ore", 1.0},
                {"Wood", 1.0},
                {"Hemp Seed", 1.0},
                {"Corn Seed", 1.0},
                {"Pumpkin Seed", 1.0},
                {"Cloth", 1.0},
                {"Pumpkin", 1.0},
                {"Corn", 1.0},
                {"Wolf Skull", 1.0}
            };
            Dictionary<string, object> defaultQuerryRates = new Dictionary<string, object>()
            {
                {"High Quality Metal Ore", 1.0},
                {"Metal Fragments", 1.0},
                {"Metal Ore", 1.0},
                {"Stones", 1.0},
                {"Sulfur Ore", 1.0}
            };
            List<object> blacklistedLoot = new List<object>()
            {
                "Rotten Apple",
                "Spoiled Wolf Meat",
                "Spoiled Chicken",
                "Spoiled Human Meat"
            };
            Dictionary<string, object> defaultSmeltRates = new Dictionary<string, object>();
            //var itemDefinitions = ItemManager.itemList;
            var itemDefinitions = ItemManager.GetItemDefinitions();
            foreach (var item in itemDefinitions)
            {
                // Записываем стандартные рейты готовки
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable != null)
                {
                    defaultSmeltRates.Add(item.displayName.english, cookable.cookTime);
                    SmeltBackup.Add(item.displayName.english, cookable.cookTime);
                }
            }
            GetConfig(BlacklistedLootCfg, ref blacklistedLoot);
            GetConfig(DefaultRatesCfg, ref defaultRates);
            GetConfig(CustomRatesCfg, ref customRates);
            GetConfig(DefaultPickupRatesCfg, ref defaultPickupRates);
            GetConfig(DefaultGatherRatesCfg, ref defaultGatherRates);
            GetConfig(DefaultSmeltRatesCfg, ref defaultSmeltRates);
            GetConfig(DefaultQuerryRatesCfg, ref defaultQuerryRates);
            GetConfig(PrefixCfg, ref Prefix);
            GetConfig(PrefixColorCfg, ref PrefixColor);
            GetConfig(UseLootMultyplierCfg, ref UseLootMultyplier);
            GetConfig(DayStartCfg, ref DayStart);
            GetConfig(DayLenghtCfg, ref DayLenght);
            GetConfig(NightStartCfg, ref NightStart);
            GetConfig(NightLenghtCfg, ref NightLenght);
            GetConfig(WarnChatCfg, ref WarnChat);
            GetConfig(CoalRateDayCfg, ref CoalRateDay);
            GetConfig(CoalRateNightCfg, ref CoalRateNight);
            GetConfig(CoalChanceDayCfg, ref CoalChanceDay);
            GetConfig(CoalChanceNightCfg, ref CoalChanceNight);
            GetConfig(MoreHQMCfg, ref MoreHQM);
            SaveConfig();

            BlacklistedLoot = blacklistedLoot.Select(x => x.ToString()).ToList();

            foreach (var item in defaultRates)
            {
                double mod;
                if(!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultRates.Add(item.Key, mod);
            }
            
            foreach (var item in customRates)
            {
                Dictionary<string, object> perms = (Dictionary<string, object>)item.Value;
                Dictionary<string, double> Perms = new Dictionary<string, double>();
                foreach(var p in perms)
                {
                    double mod;
                    if(!double.TryParse(p.Value.ToString(), out mod))
                    {
                        PrintWarning($"Custom rates for {item.Key} - {p.Key} is incorrect and will not work untill it will be resolved.");
                        continue;
                    }
                    Perms.Add(p.Key, mod);
                }
                CustomRates.Add(item.Key, Perms);
            }
            
            foreach (var item in defaultGatherRates)
            {
                double mod;
                if(!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default gather rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultGatherRates.Add(item.Key, mod);
            }
            
            foreach (var item in defaultPickupRates)
            {
                double mod;
                if(!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default pickup rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultPickupRates.Add(item.Key, mod);
            }
            
            foreach (var item in defaultQuerryRates)
            {
                double mod;
                if(!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default querry rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultQuerryRates.Add(item.Key, mod);
            }
            
            foreach (var item in defaultSmeltRates)
            {
                double mod;
                if(!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default smelt rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultSmeltRates.Add(item.Key, mod);
            }

        }
        #endregion

        #region localization
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LootRate"] = "Loot from lootboxes x{rates}\n",
                ["CoalRate"] = "Coal producing rates is x{rates}\n",
                ["No Permission"] = "You don't have permission to use this command.",
                ["Day Starts"] = "The Day is starting!\n",
                ["Night Starts"] = "The Night is starting!\n",
                ["PickupRates"] = "Pickup rates was changed to x{rates}\n",
                ["GatherRates"] = "Gather rates was changed to x{rates}\n",
                ["QuerryRates"] = "Quarry gathering rate was changed to x{rates}\n",
                ["Smelt Rate"] = "Smelt Rate was changed to x{rates}\n",
                ["PersonalRates"] = "Your personal rates:\n",
                ["PickUpRatesPers"] = "Your personal Pickup rates is x{rates}\n",
                ["GatherRatesPers"] = "Your personal Gather rates is x{rates}\n",
                ["QueryPers"] = "Your personal Query rates is x{rates}\n",
                ["SmeltPers"] = "Your personal Smelt rates is x{rates}\n",
                ["LootPers"] = "Your personal Loot rates is x{rates}"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LootRate"] = "Рейты лута x{rates}\n",
                ["CoalRate"] = "Рейты производства угля x{rates}\n",
                ["No Permission"] = "Недостаточно прав на выполнение данной команды.",
                ["Day Starts"] = "Начинается день!\n",
                ["Night Starts"] = "Начинается ночь!\n",
                ["PickupRates"] = "Рейты подбираемых предметов x{rates}\n",
                ["GatherRates"] = "Рейты добычи изменены на x{rates}\n",
                ["QuerryRates"] = "Рейты добычи ресурсов в карьерах изменены на x{rates}\n",
                ["Smelt Rate"] = "Скорость работы печей x{rates}\n",
                ["PersonalRates"] = "Ваши личные рейты:\n",
                ["PickUpRatesPers"] = "Рейты подбираемых предметов x{rates}\n",
                ["GatherRatesPers"] = "Рейты добычи x{rates}\n",
                ["QueryPers"] = "Рейты работы карьера x{rates}\n",
                ["SmeltPers"] = "Рейты переплавки x{rates}\n",
                ["LootPers"] = "Рейты лута x{rates}"
            }, this, "ru");
        }
        #endregion

        #region initializing
        void OnServerInitialized()
        {
            //Загружаем конфиг из файла
            LoadConfigValues();
            
            //Подгружаем данные локализации
            LoadMessages();
            if (!UseLootMultyplier)
            {
                Unsubscribe("OnEntityTakeDamage");
                Unsubscribe("OnLootEntity");
            }
            //Инициализируем управление временем - получаем компоненту времени
            timer.Once(3, GetTimeComponent);

            foreach (var perm in CustomRates.Keys)
            {
                permission.RegisterPermission($"{perm}".ToLower(), this);
                //PrintWarning($"{Title}.{perm}".ToLower());
            }
            var curtime = covalence.Server.Time;
            //Если при старте на сервере день - ставим в false, дабы вызывалось событие OnDayStart()
            isDay = (DayStart <= curtime.Hour && curtime.Hour < NightStart) ? false : true;

            UpdateFurnaces();
        }

        //Вызывается при выгрузке плагина
        void Unload()
        {
            //Очищаем привящку к ивнту
            if(timeComponent != null)
                timeComponent.OnHour -= OnHour;
            //Восстанавливаем стандартное время переплавки
            foreach (var item in ItemManager.GetItemDefinitions())
            {
                if (!SmeltBackup.ContainsKey(item.displayName.english)) continue;
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable != null)
                {
                    cookable.cookTime = SmeltBackup[item.displayName.english];
                }
            }
        }

        #endregion

        #region Time managment

        //Переменная, для хранения компоненты времени
        private TOD_Time timeComponent = null;

        //Заморожено ли время.
        private bool Frozen = false;

        #region main
        //Колличество попыток определения компоненты
        private uint componentSearchAttempts = 0;

        //Инициализация управления временем
        private void GetTimeComponent()
        {
            //Если Instance == 0,
            if (TOD_Sky.Instance == null)
            {
                //Увеличиваем номер попытки
                ++componentSearchAttempts;
                if (this.componentSearchAttempts < 50)
                {
                    PrintWarning("Restarting timer for GetTimeComponent(). Attempt " + componentSearchAttempts.ToString() + "/10.");
                    timer.Once(3, GetTimeComponent);
                }
                else
                {
                    RaiseError("Could not find required component after 50 attempts. Plugin will not work without it.\nTry to reload it ant if this won't fix the issue contact the developer - https://vk.com/vlad_00003");
                }

                return;
            }

            if (TOD_Sky.Instance != null && componentSearchAttempts >= 0)
            {
                Puts("Found TOD_Time component after attempt " + componentSearchAttempts.ToString() + ".");
            }

            //Записываем компаненту времени
            timeComponent = TOD_Sky.Instance.Components.Time;

            if (timeComponent == null)
            {
                RaiseError("Could not fetch time component. Plugin will not work without it.");
                return;
            }

            //Добавляем ивент к событию
            timeComponent.OnHour += OnHour;

            //Вызываем функцию, дабы узнать текущее время суток.
            OnHour();

        }

        //Идёт ли прогресс времени, данные хватаем из игры
        //Ибо костылями - мир полнится....
        private bool ProgressTime
        {
            get
            {
                return timeComponent.ProgressTime;
            }
            set
            {
                timeComponent.ProgressTime = value;
            }
        }

        //Обработчик события OnHour
        private void OnHour()
        {
            if (DayStart <= CurrentHour && CurrentHour < NightStart)
            {
                if (!isDay)
                {
                    //Устанавливаем время суток на День
                    isDay = true;
                    //Вызываем процедуру обновления длинны суток
                    UpdateDayLenght(DayLenght, false);
                    Interface.Oxide.CallHook("OnDayStart");
                }
            }
            else
            {
                if (isDay)
                {
                    //Устанавливаем время суток на ночь
                    isDay = false;
                    //Вызываем процедуру обновления длинны суток
                    UpdateDayLenght(NightLenght, true);
                    Interface.Oxide.CallHook("OnNightStart");
                }
            }
        }

        //Функция, обновляющяя длительность суток в зависимости от времени суток в игре.
        void UpdateDayLenght(uint Lenght, bool night)
        {
            float dif = NightStart - DayStart;
            if (night)
            {
                dif = (24 - dif);
            }
            float part = 24.0f / dif;
            float newLenght = part * Lenght;
            if (newLenght == 0) newLenght = 0.1f;
            timeComponent.DayLengthInMinutes = newLenght;
        }

        #endregion

        #region Helpers

        //Возвращает текущий час
        public float CurrentHour
        {
            get
            {
                return TOD_Sky.Instance.Cycle.Hour;
            }
        }

        #endregion

        #endregion

        #region Rate controller
        //Бочки с лутом
        //void ClearContainer(LootContainer container)
        //{
        //    while (container.inventory.itemList.Count > 0)
        //    {
        //        var item = container.inventory.itemList[0];
        //        item.RemoveFromContainer();
        //        item.Remove(0f);
        //    }
        //}
        //private void OnEntitySpawned(BaseNetworkable entity)
        //{
        //    if (!UseLootMultyplier) { return; }
        //    if (entity is SupplyDrop) { return; }
        //    if (entity is LockedByEntCrate) { return; }
        //    if (entity is Stocking) { return; }
        //    if (entity is LootContainer)
        //    {
        //        var lootbox = entity as LootContainer;

        //        ClearContainer(lootbox);
        //        lootbox.PopulateLoot();

        //        foreach (Item lootitem in lootbox.inventory.itemList.ToList())
        //        {
        //            if (lootitem.info.stackable > 1)
        //            {
        //                if (BlacklistedLoot.Contains(lootitem.info.displayName.english) || BlacklistedLoot.Contains(lootitem.info.shortname)) continue;
        //                lootitem.amount = (int)(lootitem.amount * LootRate);
        //            }
        //        }
        //    }
        //}
        //private void UpdateLoot()
        //{
        //    foreach (var container in UnityEngine.Object.FindObjectsOfType<LootContainer>())
        //    {
        //        if (container is SupplyDrop) { continue; }
        //        if (container is LockedByEntCrate) { continue; }
        //        if (container is Stocking) { continue; }
        //        ClearContainer(container);
        //        container.PopulateLoot();
        //        foreach (Item lootitem in container.inventory.itemList.ToList())
        //        {
        //            if (lootitem.info.stackable > 1)
        //            {
        //                if (BlacklistedLoot.Contains(lootitem.info.displayName.english) || BlacklistedLoot.Contains(lootitem.info.shortname)) continue;
        //                lootitem.amount = (int)(lootitem.amount * LootRate);
        //            }
        //        }
        //    }
        //}
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var lootcont = entity as LootContainer;
            if (lootcont is SupplyDrop) return;
            var player = info.InitiatorPlayer;
            if (player == null) return;

            if (!lootcont) return;
            if (lootcont.OwnerID == player.userID) return;
            lootcont.OwnerID = player.userID;

            lootcont.SpawnLoot();

            double rate;
            if (isDay)
            {
                rate = GetUserRates(player.UserIDString, LootDayString);
            }
            else
            {
                rate = GetUserRates(player.UserIDString, LootNightString);
            }

            foreach (var lootitem in lootcont.inventory.itemList)
            {
                if (lootitem.info.stackable > 1)
                {
                    if (BlacklistedLoot.Contains(lootitem.info.displayName.english) || BlacklistedLoot.Contains(lootitem.info.shortname)) continue;
                    var new_amount = (int)(lootitem.amount * rate);
                    lootitem.amount = new_amount > 1 ? new_amount : 1;
                }
            }
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity is SupplyDrop)  return; 
            if (entity is LockedByEntCrate)  return; 
            if (entity is Stocking)  return; 

            var lootcont = entity as LootContainer;
            if (!lootcont) return;
            if (lootcont.OwnerID == player.userID) return;
            var instanceID = lootcont.GetInstanceID();
            DateTime cd;
            if(!CratesCD.TryGetValue(instanceID, out cd))
            {
                cd = DateTime.Now;
                CratesCD.Add(instanceID, cd);
            }
            if(cd.Subtract(DateTime.Now).TotalSeconds > 0)
            {
                return;
            }
            CratesCD[instanceID] = DateTime.Now.AddMinutes(2);
            lootcont.OwnerID = player.userID;
            lootcont.SpawnLoot();

            double rate;
            if (isDay)
            {
                rate = GetUserRates(player.UserIDString, LootDayString);
            }else
            {
                rate = GetUserRates(player.UserIDString, LootNightString);
            }

            foreach (var lootitem in lootcont.inventory.itemList)
            {
                if (lootitem.info.stackable > 1)
                {
                    if (BlacklistedLoot.Contains(lootitem.info.displayName.english) || BlacklistedLoot.Contains(lootitem.info.shortname)) continue;
                    var new_amount = (int)(lootitem.amount * rate);
                    lootitem.amount = new_amount > 1 ? new_amount : 1;
                }
            }
        }
        private void UpdateFurnaces()
        {
            var baseOvens = Resources.FindObjectsOfTypeAll<BaseOven>().Where(c => c.isActiveAndEnabled).Cast<BaseEntity>().ToList();
            foreach (var oven in baseOvens)
            {
                if (oven.HasFlag(BaseEntity.Flags.On))
                {
                    double ovenMultiplier;
                    if (isDay)
                    {
                        ovenMultiplier = GetUserRates(oven.OwnerID.ToString(), SmeltDayString);
                    }else
                    {
                        ovenMultiplier = GetUserRates(oven.OwnerID.ToString(), SmeltNightString);
                    }
                    if (ovenMultiplier > 10f) ovenMultiplier = 10f;
                    if (ovenMultiplier < 0.1f) ovenMultiplier = 0.1f;
                    InvokeHandler.CancelInvoke(oven.GetComponent<MonoBehaviour>(), new Action((oven as BaseOven).Cook));
                    (oven as BaseOven).inventory.temperature = CookingTemperature((oven as BaseOven).temperature);
                    (oven as BaseOven).UpdateAttachmentTemperature();
                    InvokeHandler.InvokeRepeating(oven.GetComponent<MonoBehaviour>(), new Action((oven as BaseOven).Cook), (float)(0.5f / ovenMultiplier), (float)(0.5f / ovenMultiplier));

                }
            }
        }
        float CookingTemperature(BaseOven.TemperatureType temperature)
        {
            switch (temperature)
            {
                case BaseOven.TemperatureType.Warming:
                    return 50f;
                case BaseOven.TemperatureType.Cooking:
                    return 200f;
                case BaseOven.TemperatureType.Smelting:
                    return 1000f;
                case BaseOven.TemperatureType.Fractioning:
                    return 1500f;
                default:
                    return 15f;
            }
        }
        object OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            if (!oven.HasFlag(BaseEntity.Flags.On))
            {
                double ovenMultiplier;
                if (isDay)
                {
                    ovenMultiplier = GetUserRates(oven.OwnerID.ToString(), SmeltDayString);
                }
                else
                {
                    ovenMultiplier = GetUserRates(oven.OwnerID.ToString(), SmeltNightString);
                }
                if (ovenMultiplier > 10f) ovenMultiplier = 10f;
                if (ovenMultiplier < 0.1f) ovenMultiplier = 0.1f;
                StartCooking(oven, oven.GetComponent<BaseEntity>(), ovenMultiplier);
                return false;
            }
            return null;
        }
        void StartCooking(BaseOven oven, BaseEntity entity, double ovenMultiplier)
        {
            if (FindBurnable(oven) == null)
                return;
            oven.inventory.temperature = CookingTemperature(oven.temperature);
            oven.UpdateAttachmentTemperature();
            InvokeHandler.CancelInvoke(entity.GetComponent<MonoBehaviour>(), new Action(oven.Cook));
            InvokeHandler.InvokeRepeating(entity.GetComponent<MonoBehaviour>(), new Action(oven.Cook), (float)(0.5f / ovenMultiplier), (float)(0.5f / ovenMultiplier));
            entity.SetFlag(BaseEntity.Flags.On, true, false);
        }
        Item FindBurnable(BaseOven oven)
        {
            if (oven.inventory == null)
                return null;
            foreach (Item current in oven.inventory.itemList)
            {
                ItemModBurnable component = current.info.GetComponent<ItemModBurnable>();
                if (component && (oven.fuelType == null || current.info == oven.fuelType))
                    return current;
            }
            return null;
        }

        private void OnDayStart()
        {
            CoalRate = CoalRateDay;
            CoalChance = CoalChanceDay;
            SmeltRate = DefaultRates[SmeltDayString];
            GatherRate = DefaultRates[GatherDayString];
            PickupRate = DefaultRates[PickUpDayString];
            QuerryRate = DefaultRates[QuerryDayString];
            LootRate = DefaultRates[LootDayString];
            //Обновляем скорость переплавки
            //UpdateSmeltTime();
            UpdateFurnaces();

            //Оповещаем игоков о смене времени суток
            if (WarnChat)
            {
                RatesToChat();
            }
        }
        private void OnNightStart()
        {
            CoalRate = CoalRateNight;
            CoalChance = CoalChanceNight;
            SmeltRate = DefaultRates[SmeltNightString];
            GatherRate = DefaultRates[GatherNightString];
            PickupRate = DefaultRates[PickUpNightString];
            QuerryRate = DefaultRates[QuerryNightString];
            LootRate = DefaultRates[LootNightString];
            //Обновляем скорость переплавки
            //UpdateSmeltTime();
            UpdateFurnaces();

            //Оповещаем игоков о смене времени суток
            if (WarnChat)
            {
                RatesToChat();
            }
        }
        private void RatesToChat()
        {
            string Message = string.Empty;
            Message = isDay ? GetMsg("Day Starts") : GetMsg("Night Starts");
            Message += GetMsg("GatherRates").Replace("{rates}", GatherRate.ToString());
            Message += GetMsg("PickupRates").Replace("{rates}", PickupRate.ToString());
            Message += GetMsg("QuerryRates").Replace("{rates}", QuerryRate.ToString());
            Message += GetMsg("Smelt Rate").Replace("{rates}", SmeltRate.ToString());
            Message += GetMsg("CoalRate").Replace("{rates}", CoalRate.ToString());
            if (UseLootMultyplier)
                Message += GetMsg("LootRate").Replace("{rates}", LootRate.ToString());
            SendToChat(Message);
        }


        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            double mod = 1f;
            if (DefaultPickupRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultPickupRates[item.info.displayName.english];
            }
            int new_amount;
            if (isDay)
            {
                new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpDayString) * mod);
                item.amount = new_amount > 1 ? new_amount : 1;
                return;
            }
            new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpNightString) * mod);
            item.amount = new_amount > 1 ? new_amount : 1;
        }
        void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
        {
            double mod = 1f;
            if (DefaultPickupRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultPickupRates[item.info.displayName.english];
            }
            int new_amount;
            if (isDay)
            {
                new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpDayString) * mod);
                item.amount = new_amount > 1 ? new_amount : 1;
                return;
            }
            new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpNightString) * mod);
            item.amount = new_amount > 1 ? new_amount : 1;
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            var lootcont = entity as LootContainer;
            if (lootcont)
            {
                var instanceid = lootcont.GetInstanceID();
                if (CratesCD.ContainsKey(instanceid))
                {
                    CratesCD.Remove(instanceid);
                }
            }
            BaseEntity dispenser = entity as BaseEntity;
            if (DefaultFinishBonuses.ContainsKey(dispenser))
            {
                DefaultFinishBonuses.Remove(dispenser);
            }
        }
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer()) return;
            double mod = 1f;
            if (DefaultGatherRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultGatherRates[item.info.displayName.english];
            }
            int new_amount;
            double rate;
            if (isDay)
            {
                rate = GetUserRates(entity.ToPlayer().UserIDString, GatherDayString);
            }
            else
            {
                rate = GetUserRates(entity.ToPlayer().UserIDString, GatherNightString);
            }
            new_amount = (int)(item.amount * rate * mod);
            item.amount = new_amount > 1 ? new_amount : 1;

            BaseEntity dispenserEnt = dispenser.GetComponent<BaseEntity>();
            if (MoreHQM && dispenser.gatherType == ResourceDispenser.GatherType.Ore)
            {
                bool HaveHQM = dispenser.finishBonus.Any(x => x.itemDef.displayName.english == "High Quality Metal Ore");
                if (!HaveHQM)
                {
                    dispenser.finishBonus.Add(new ItemAmount(ItemManager.FindItemDefinition(2133577942), 2f));
                }
            }
            if (!DefaultFinishBonuses.ContainsKey(dispenserEnt))
            {
                DefaultFinishBonuses[dispenserEnt] = dispenser.finishBonus.Select(p => new KeyValuePair<string, float>(p.itemDef.displayName.english, p.amount)).ToDictionary(x => x.Key, x => x.Value);
            }
            foreach (var bonus in dispenser.finishBonus)
            {
                if (DefaultFinishBonuses[dispenserEnt].ContainsKey(bonus.itemDef.displayName.english))
                {
                    float default_bonus = DefaultFinishBonuses[dispenserEnt][bonus.itemDef.displayName.english];
                    double bonus_mode = 1f;
                    if (DefaultGatherRates.ContainsKey(bonus.itemDef.displayName.english))
                    {
                        bonus_mode = DefaultGatherRates[bonus.itemDef.displayName.english];
                    }
                    int new_bonus_amount = (int)(default_bonus * rate * bonus_mode);
                    bonus.amount = new_bonus_amount > 1 ? new_bonus_amount : 1;
                }
            }
        }
        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            double mod = 1f;
            if (DefaultQuerryRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultQuerryRates[item.info.displayName.english];
            }
            int new_amount;
            if (isDay)
            {
                new_amount = (int)(item.amount * GetUserRates(quarry.OwnerID, QuerryDayString) * mod);
                item.amount = new_amount > 1 ? new_amount : 1;
                return;
            }
            new_amount = (int)(item.amount * GetUserRates(quarry.OwnerID, QuerryNightString) * mod);
            item.amount = new_amount > 1 ? new_amount : 1;
        }
        void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven == null) return;
            burnable.byproductAmount = (int)Math.Ceiling(CoalRate);
            burnable.byproductChance = (100 - CoalChance) / 100f;
        }
        //void UpdateSmeltTime()
        //{
        //    var itemDefinitions = ItemManager.GetItemDefinitions();
        //    foreach (var item in itemDefinitions)
        //    {
        //        var cookable = item.GetComponent<ItemModCookable>();
        //        if (cookable != null)
        //        {
        //            if (DefaultSmeltRates.ContainsKey(item.displayName.english))
        //            {
        //                cookable.cookTime = (float)(DefaultSmeltRates[item.displayName.english] / SmeltRate);
        //            }else
        //            {
        //                DefaultSmeltRates.Add(item.displayName.english, cookable.cookTime);
        //                cookable.cookTime = (float)(DefaultSmeltRates[item.displayName.english] / SmeltRate);
        //            }
        //        }
        //    }
        //}
        #endregion

        #region Сonsole commands
        [ConsoleCommand("env.freeze")]
        void TimeFreeze(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (Frozen)
            {
                //Puts("The time is already frozen!");
                arg.ReplyWith("The time is already frozen!");
                return;
            }
            Frozen = true;
            ProgressTime = false;
            //Puts("The time was frozen.");
            arg.ReplyWith("The time was frozen.");
        }

        [ConsoleCommand("env.unfreeze")]
        void TimeUnFreeze(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (!Frozen)
            {
                arg.ReplyWith("The time is not frozen!");
                return;
            }
            Frozen = false;
            ProgressTime = true;
            arg.ReplyWith("The time was unfrozen.");
        }

        [ConsoleCommand("rates.show")]
        void ShowRates(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null)
            {
                arg.ReplyWith("Usage rates.show steamid [type]");
                return;
            }
            var target = covalence.Players.FindPlayer(arg.Args[0]);
            if (target == null)
            {
                arg.ReplyWith("User not found or multiply user mathces");
                return;
            }
            if (arg.Args.Length >= 2)
            {
                arg.ReplyWith(GetUserRates(target.Id, arg.Args[1]).ToString());
                return;
            }
            string reply = $"User '{target.Name}' current rates:\n";
            foreach (var p in AvaliableMods)
            {
                reply += p + ": " + GetUserRates(target.Id, p).ToString() + " \n";
            }
            arg.ReplyWith(reply);
        }
        #endregion

        #region Chat commands
        [ChatCommand("rates")]
        private void ShowRatesChat(BasePlayer player, string command, string[] args)
        {
            string reply = GetMsg("PersonalRates", player.UserIDString);
            if (isDay)
            {
                reply += GetMsg("GatherRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, GatherDayString).ToString());
                reply += GetMsg("PickUpRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, PickUpDayString).ToString());
                reply += GetMsg("QueryPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, QuerryDayString).ToString());
                reply += GetMsg("SmeltPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, SmeltDayString).ToString());
                reply += GetMsg("LootPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, LootDayString).ToString());
                SendToChat(player, reply);
                return;
            }
            reply += GetMsg("GatherRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, GatherNightString).ToString());
            reply += GetMsg("PickUpRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, PickUpNightString).ToString());
            reply += GetMsg("QueryPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, QuerryNightString).ToString());
            reply += GetMsg("SmeltPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, SmeltNightString).ToString());
            reply += GetMsg("LootPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, LootNightString).ToString());
            SendToChat(player, reply);
        }
        #endregion

        #region Helpers
        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }

        //Функция, отправляющая сообщение в чат конкретному пользователю, добавляет префикс
        private void SendToChat(BasePlayer Player, string Message)
        {
            PrintToChat(Player, "<color=" + PrefixColor + ">" + Prefix + "</color> " + Message);
        }

        //Перезгрузка функции отправки собщения в чат - отправляет сообщение всем пользователям
        private void SendToChat(string Message)
        {
            PrintToChat("<color=" + PrefixColor + ">" + Prefix + "</color> " + Message);
        }

        //Функция получения строки из языкового файла
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        double GetUserRates(string steamId, string RateType)
        {
            /*
             * Из списка кастомных привелегий выбираем только те, на которые у игрока есть привелегия.
             * выбираем только сами привелегии, без названий. Только содерживое
             * Из них выбираем те, где есть нужный нам тип рейтов. И выбираем только нужные нам типы рейтов.
             */
            var playergroups = CustomRates.Where(i => permission.UserHasPermission(steamId, i.Key)).Select(i => i.Value).
                Where(i => i.ContainsKey(RateType)).Select(i => i[RateType]);
            return playergroups.Count() > 0 ? playergroups.Aggregate((i1, i2) => i1 > i2 ? i1 : i2) : DefaultRates[RateType];
        }
        //Перегрузка функции. Ибо мне так будет проще)
        double GetUserRates(ulong steamId, string RateType) => GetUserRates(steamId.ToString(), RateType);

        Dictionary<string, object> CreatePerms(List<string> mods, double rate)
        {
            return mods.ToDictionary(x => x, x => (object)rate);
        }


        #endregion
    }
}