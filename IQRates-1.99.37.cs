using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;
using Object = System.Object;
using System.Text;
using Facepunch;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("IQRates", "Mercury", "1.99.37")]
    [Description("Настройка рейтинга на сервере")]
    class IQRates : RustPlugin
    {
        
        private const Boolean LanguageEn = false;
        bool IsPermission(string userID,string Permission)
        {
            if (permission.UserHasPermission(userID, Permission))
                return true;
            else return false;
        }
        private void StartCargoPlane(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (!EventSettings.CargoPlaneSetting.FullOff && EventSettings.CargoPlaneSetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.CargoPlaneSetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.CargoPlaneSetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.CargoPlaneSetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.CargoPlaneSetting.EventSpawnTime;
                timer.Once(TimeSpawn, () =>
                {
                    StartCargoPlane(EventSettings);
                    SpawnPlane();
                    
                    if(EventSettings.CargoPlaneSetting.useGameTip)
                        MessageGameTipsError("ALERT_CARGOPLANE");
                });
            }
        }

        UInt64? GetQuarryPlayer(UInt64 NetID)
        {
            if (!DataQuarryPlayer.ContainsKey(NetID)) return null;
            if (DataQuarryPlayer[NetID] == 0) return null;

            return DataQuarryPlayer[NetID];
        }
        void StartupFreeze()
        {
            if (!config.pluginSettings.OtherSetting.UseFreezeTime) return;
            timeComponent.ProgressTime = true;
            ConVar.Env.time = config.pluginSettings.OtherSetting.FreezeTime;
        }
                
        void OnQuarryToggled(MiningQuarry quarry, BasePlayer player) => AddQuarryPlayer(quarry.net.ID.Value, player.userID);

        private class OvenController : FacepunchBehaviour
        {
            private static readonly Dictionary<BaseOven, OvenController> Controllers = new Dictionary<BaseOven, OvenController>();
            private BaseOven _oven;
            private float _speed;
            private string _ownerId;
            private Int32 _ticks;
            private Int32 _speedFuel;
            
            private bool IsFurnace => (int)_oven.temperature >= 2;

            private void Awake()
            {
                _oven = (BaseOven)gameObject.ToBaseEntity();
                _ownerId = _oven.OwnerID.ToString();
            }

            public object Switch(BasePlayer player)
            {
                if (!IsFurnace || _oven.needsBuildingPrivilegeToUse && (player != null && !player.CanBuild()))
                    return null;

                if (_oven.IsOn())
                    StopCooking();
                else
                {
                    _ownerId = _oven.OwnerID != 0 || player == null ? _oven.OwnerID.ToString() : player.UserIDString;
                    StartCooking();
                }
                return false;
            }

            public void TryRestart()
            {
                if (!_oven.IsOn())
                    return;
                _oven.CancelInvoke(_oven.Cook);
                StopCooking();
                StartCooking();
            }
            private void Kill()
            {
                if (_oven.IsOn())
                {
                    StopCooking();
                    _oven.StartCooking();
                }
                Destroy(this);
            }

            
            public static OvenController GetOrAdd(BaseOven oven)
            {
                OvenController controller;
                if (Controllers.TryGetValue(oven, out controller))
                    return controller;
                controller = oven.gameObject.AddComponent<OvenController>();
                Controllers[oven] = controller;
                return controller;
            }

            public static void TryRestartAll()
            {
                foreach (var pair in Controllers)
                {
                    pair.Value.TryRestart();
                }
            }
            public static void KillAll()
            {
                foreach (var pair in Controllers)
                {
                    pair.Value.Kill();
                }
                Controllers.Clear();
            }
            public void OnDestroy()
            {
                Destroy(this);
            }
                        
            private void StartCooking()
            {
                if(_oven.IndustrialMode != BaseOven.IndustrialSlotMode.ElectricFurnace)
                    if (_oven.FindBurnable() == null)
                        return;

                Single Multiplace = _.GetMultiplaceBurnableSpeed(_ownerId);
                _speed = (Single)(0.5f / Multiplace); // 0.57 * M
                Int32 MultiplaceFuel = _.GetMultiplaceBurnableFuelSpeed(_ownerId);
                _speedFuel = MultiplaceFuel;
                
                StopCooking();
                
                _oven.inventory.temperature = _oven.cookingTemperature;
                _oven.UpdateAttachmentTemperature();

                InvokeRepeating(Cook, _speed, _speed);
                _oven.SetFlag(BaseEntity.Flags.On, true);

            }
            
            private void StopCooking()
            {
                CancelInvoke(Cook);
                _oven.StopCooking();
                _oven.SetFlag(BaseEntity.Flags.On, false);
                _oven.SendNetworkUpdate();
                
                if (_oven.inventory != null)
                {
                    foreach (Item item in _oven.inventory.itemList)
                    {
                        if (item.HasFlag(global::Item.Flag.Cooking))
                            item.SetFlag(global::Item.Flag.Cooking, false);
                        item.MarkDirty();
                    }
                }
            }
            
            public void Cook()
            {
                if (!_oven.HasFlag(BaseEntity.Flags.On))
                {
                    StopCooking();
                    return;
                }
                Item item = _oven.FindBurnable();

                if (Interface.CallHook("OnOvenCook", this, item) != null)
                    return;
                
                if (_oven.IndustrialMode != BaseOven.IndustrialSlotMode.ElectricFurnace)
                {
                    if (item == null)
                    {
                        StopCooking();
                        return;
                    }
                }

                _oven.Cook();

                SmeltItems();

                if (_oven.IndustrialMode != BaseOven.IndustrialSlotMode.ElectricFurnace)
                {
                    var component = item.info.GetComponent<ItemModBurnable>();
                    item.fuel -= 0.5f * (_oven.cookingTemperature / 200f) * _speedFuel;

                    if (!item.HasFlag(global::Item.Flag.OnFire))
                    {
                        item.SetFlag(global::Item.Flag.OnFire, true);
                        item.MarkDirty();
                    }

		   		 		  						  	   		   		 		  	 	 		  						  				
                    if (item.fuel <= 0f)
                    {
                        _oven.ConsumeFuel(item, component);
                    }
                }

                _ticks++;
                
                Interface.CallHook("OnOvenCooked", this, item,  _oven.GetSlot(BaseEntity.Slot.FireMod));
            }
            private void SmeltItems()
            {
                if (_ticks % 1 != 0)
                    return;

                for (var i = 0; i < _oven.inventory.itemList.Count; i++)
                {
                    var item = _oven.inventory.itemList[i];
                    
                    if (item == null || !item.IsValid() || item.info == null)
                        continue;

                    var cookable = item.info.GetComponent<ItemModCookable>();
                    if (cookable == null)
                        continue;

                    if (_.IsBlackListBurnable(item.info.shortname)) continue;
                    
                    Single temperature = item.temperature;
                    
                    if ((temperature < cookable.lowTemp || temperature > cookable.highTemp))
                    {
                        if (!cookable.setCookingFlag || !item.HasFlag(global::Item.Flag.Cooking)) continue;
                        item.SetFlag(global::Item.Flag.Cooking, false);
                        item.MarkDirty();
                        continue;
                    }

                    if (!cookable.CanBeCookedByAtTemperature(temperature) && _.IsBlackListBurnable(item.info.shortname))
                    {
                        if (!cookable.setCookingFlag || !item.HasFlag(global::Item.Flag.Cooking))
                            continue;

                        item.SetFlag(global::Item.Flag.Cooking, false);
                        item.MarkDirty();
                        continue;
                    }

                    if (cookable.cookTime > 0 && _ticks * 1f / 1 % cookable.cookTime > 0)
                        continue;

                    if (cookable.setCookingFlag && !item.HasFlag(global::Item.Flag.Cooking))
                    {
                        item.SetFlag(global::Item.Flag.Cooking, true);
                        item.MarkDirty();
                    }

                    var position = item.position;
                    if (item.amount > 1)
                    {
                        item.amount--;
                        item.MarkDirty();
                    }
                    else
                    {
                        item.Remove();
                    }

                    if (cookable.becomeOnCooked == null) continue;

                    var item2 = ItemManager.Create(cookable.becomeOnCooked,
                        (int)(cookable.amountOfBecome * 1f));
		   		 		  						  	   		   		 		  	 	 		  						  				
                    if (item2 == null || item2.MoveToContainer(item.parent, position) ||
                        item2.MoveToContainer(item.parent))
                        continue;
		   		 		  						  	   		   		 		  	 	 		  						  				
                    item2.Drop(item.parent.dropPosition, item.parent.dropVelocity);
                    //if (!item.parent.entityOwner) continue;
                    StopCooking();
                }
            }
        }
        
                
        
        
        private static StringBuilder sb = new StringBuilder();

                private const string prefabCH47 = "assets/prefabs/npc/ch47/ch47scientists.entity.prefab";
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

        
        
                
        [ChatCommand("rates")]
        private void GetInfoMyRates(BasePlayer player)
        {
            if (player == null) return;

            var PrivilegyRates = config.pluginSettings.RateSetting.PrivilegyRates;
            Boolean IsTimes = IsTime();
            Configuration.PluginSettings.Rates.AllRates Rates = IsTimes ? config.pluginSettings.RateSetting.DayRates : config.pluginSettings.RateSetting.NightRates;
            Single bonusRate = GetBonusRateDayOfWeek(player);
            
            foreach (var RatesSetting in PrivilegyRates)
                if (IsPermission(player.UserIDString, RatesSetting.Key))
                {
                    Rates = IsTimes ? RatesSetting.Value.DayRates : RatesSetting.Value.NightRates;
                    break;
                }

            SendChat(GetLang("MY_RATES_INFO", player.UserIDString, Rates.GatherRate + bonusRate, Rates.LootRate + bonusRate, Rates.PickUpRate + bonusRate, Rates.QuarryRate + bonusRate, Rates.ExcavatorRate + bonusRate, Rates.GrowableRate + bonusRate), player);
        }

        
                
        private void OnMixingTableToggle(MixingTable table, BasePlayer player)
        {
            if (table.IsOn())
                return;
            
            Single speedMixing = GetSpeeedMixingTable(player);
            
            NextTick(() =>
            {
                table.RemainingMixTime *= speedMixing;
                table.TotalMixTime *= speedMixing;
                table.SendNetworkUpdateImmediate();

                if (!(table.RemainingMixTime < 1f)) return;
                table.CancelInvoke(table.TickMix);
                table.Invoke(table.TickMix, table.RemainingMixTime);
            });
        }
        public Int32 GetMultiplaceBurnableFuelSpeed(String ownerid)
        {
            Int32 Multiplace = config.pluginSettings.RateSetting.SpeedFuelBurnable;
            if (config.pluginSettings.RateSetting.UseSpeedBurnableList)
            {
                var SpeedInList = config.pluginSettings.RateSetting.SpeedBurableList.OrderByDescending(z => z.SpeedFuelBurnable).FirstOrDefault(x => permission.UserHasPermission(ownerid, x.Permissions));
                if (SpeedInList != null)
                    Multiplace = SpeedInList.SpeedFuelBurnable;
            }
            return Multiplace;
        }

        private object OnPlayerAddModifiers(BasePlayer player, Item item, ItemModConsumable consumable)
        {
            if (!TeaModifers.ContainsKey(item.info.shortname)) return true;
            List<ModifierDefintion> mods = Pool.Get<List<ModifierDefintion>>();

            Dictionary<String, Configuration.PluginSettings.Rates.DayAnNightRate> PrivilegyRates =
                config.pluginSettings.RateSetting.PrivilegyRates;
            Boolean IsTimes = IsTime();
            Configuration.PluginSettings.Rates.AllRates Rates = IsTimes
                ? config.pluginSettings.RateSetting.DayRates
                : config.pluginSettings.RateSetting.NightRates;
            Configuration.PluginSettings.Rates.AllRates DefaultRates = IsTimes
                ? config.pluginSettings.RateSetting.DayRates
                : config.pluginSettings.RateSetting.NightRates;
		   		 		  						  	   		   		 		  	 	 		  						  				
            Single ModiferDifference = 1.0f;
            Single DefaultRate = 1.0f;
            Single PlayerRate = 1.0f;

            foreach (var RatesSetting in PrivilegyRates)
                if (IsPermission(player.UserIDString, RatesSetting.Key))
                    Rates = IsTimes ? RatesSetting.Value.DayRates : RatesSetting.Value.NightRates;

            Single BonusRate = GetBonusRate(player);

            ModiferTea TeaLocal = TeaModifers[item.info.shortname];
            
            switch (TeaLocal.Type)
            {
                case Modifier.ModifierType.Ore_Yield:
                {
                    DefaultRate = DefaultRates.GatherRate;
                    PlayerRate = Rates.GatherRate + BonusRate;

                    ModiferDifference = (PlayerRate - DefaultRate) <= 0 ? 1 : (PlayerRate - DefaultRate);
            
                    mods.Add(GetDefintionModifer(TeaLocal.Type, TeaLocal.Duration,
                        TeaLocal.Value / ModiferDifference));
                    
                    break;
                }
                case Modifier.ModifierType.Wood_Yield:
                {
                    DefaultRate = DefaultRates.GatherRate;
                    PlayerRate = Rates.GatherRate + BonusRate;

                    ModiferDifference = (PlayerRate - DefaultRate) <= 0 ? 1 : (PlayerRate - DefaultRate);

                    mods.Add(GetDefintionModifer(TeaLocal.Type, TeaLocal.Duration,
                        TeaLocal.Value / ModiferDifference));
                    
                    break;
                }
                case Modifier.ModifierType.Scrap_Yield:
                {
                    DefaultRate = DefaultRates.LootRate;
                    PlayerRate = Rates.LootRate + BonusRate;

                    ModiferDifference = (PlayerRate - DefaultRate) <= 0 ? 1 : (PlayerRate - DefaultRate);

                    mods.Add(GetDefintionModifer(TeaLocal.Type, TeaLocal.Duration,
                        TeaLocal.Value / ModiferDifference));
                    
                    break;
                }
            }
            
            player.modifiers.Add(mods);
            Pool.FreeUnmanaged(ref mods);

            return true;
        }
        private TriggeredEventPrefab[] defaultEvents;
        void SpawnCH47()
        {
            UnSubProSub();
        
            var position = new Vector3(ConVar.Server.worldsize, 100, ConVar.Server.worldsize) - new Vector3(50f, 0f, 50f);
            var entity = GameManager.server.CreateEntity(prefabCH47, position) as CH47HelicopterAIController;
            entity?.TriggeredEventSpawn();
            entity?.Spawn();
        }
        private void UnSubProSub(int time = 1)
        {
            Unsubscribe("OnEntitySpawned");
            timer.Once(time, () =>
            {
                Subscribe("OnEntitySpawned");
            });
        }
        private void OnEntitySpawned(Minicopter copter)
        {
            if (copter == null) return;
            FuelSystemRating(copter.GetFuelSystem(), config.pluginSettings.OtherSetting.FuelSetting.AmountMinicopter);
		   		 		  						  	   		   		 		  	 	 		  						  				
            if (config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel)
                copter.fuelPerSec = config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.ConsumedCopter;
        }
        private void StartBreadley(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (SpacePort == null) return;
            if (!EventSettings.BreadlaySetting.FullOff && EventSettings.BreadlaySetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.BreadlaySetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.BreadlaySetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.BreadlaySetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.BreadlaySetting.EventSpawnTime;
                timer.Once(TimeSpawn, () =>
                {
                    StartBreadley(EventSettings);
                    SpawnTank();
                    
                    if(EventSettings.BreadlaySetting.useGameTip)
                        MessageGameTipsError("ALERT_BRADLEY");
                });
            }
        }

        private void Init() => ReadData();

        int Converted(Types RateType, string Shortname, float Amount, BasePlayer player = null, String UserID = null)
        {
            float ConvertedAmount = Amount;

            ItemDefinition definition = ItemManager.FindItemDefinition(Shortname);
            if(definition != null && (config.pluginSettings.RateSetting.TypeList == TypeListUsed.BlackList && !IsWhiteList(Shortname)))
                foreach (String BlackItemCategory in config.pluginSettings.RateSetting.BlackListCategory)
                {
                    ItemCategory Category;
                    if (!Enum.TryParse(BlackItemCategory, out Category)) continue;
                    
                    if (Category == definition.category)
                    {
                        //PrintToChat($"DEBUG : Категория {BlackItemCategory} заблокирована для множителя");
                        return Convert.ToInt32(ConvertedAmount);
                    }
                }
            
            if (config.pluginSettings.RateSetting.TypeList == TypeListUsed.BlackList)
            {
                if (IsBlackList(Shortname))
                    return Convert.ToInt32(ConvertedAmount);
            }
            else
            {
                if (!IsWhiteList(Shortname))
                {
                    //PrintToChat($"DEBUG : Предмет {Shortname} заблокирована для #4468837173 множителя");
                    return Convert.ToInt32(ConvertedAmount);
                }
            }

            var PrivilegyRates = config.pluginSettings.RateSetting.PrivilegyRates;
            Boolean IsTimes = IsTime();
            Configuration.PluginSettings.Rates.AllRates Rates = IsTimes ? config.pluginSettings.RateSetting.DayRates : config.pluginSettings.RateSetting.NightRates;
            
            UserID = player != null ? player.UserIDString : UserID;
		   		 		  						  	   		   		 		  	 	 		  						  				
            if (UserID != null)
            {
                var CustomRate = IsTimes ? config.pluginSettings.RateSetting.CustomRatesPermissions.DayRates : config.pluginSettings.RateSetting.CustomRatesPermissions.NightRates;

                var Rate = CustomRate.FirstOrDefault(x => IsPermission(UserID, x.Key)); //dbg
                if (Rate.Value != null)
                    foreach (Configuration.PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis RateValue in Rate.Value.Where(x => x.Shortname == Shortname))
                    {
                        ConvertedAmount = Amount * RateValue.Rate;
                        return (int)ConvertedAmount;
                    }

                foreach (var RatesSetting in PrivilegyRates)
                    if (IsPermission(UserID, RatesSetting.Key))
                    {
                        Rates = IsTimes ? RatesSetting.Value.DayRates : RatesSetting.Value.NightRates;
                        break;
                    }
            }

            Single BonusRate = GetBonusRate(player);
            Single bonusRateDayOfWeek = GetBonusRateDayOfWeek(player);

            BonusRate += bonusRateDayOfWeek;

            switch (RateType)
            {
                case Types.Gather:
                {
                    ConvertedAmount = Amount * (Rates.GatherRate + BonusRate);
                    break;
                }
                case Types.Loot:
                {
                    ConvertedAmount = Amount * (Rates.LootRate + BonusRate);
                    break;
                }
                case Types.PickUP:
                {
                    ConvertedAmount = Amount * (Rates.PickUpRate + BonusRate);
                    break;
                }
                case Types.Growable:
                {
                    ConvertedAmount = Amount * (Rates.GrowableRate + BonusRate);
                    break;
                }
                case Types.Quarry:
                {
                    Single QuarryRates = GetRateQuarryDetalis(Rates, Shortname);
                    ConvertedAmount = Amount * (QuarryRates + BonusRate);
                    break;
                }
                case Types.Excavator:
                {
                    ConvertedAmount = Amount * (Rates.ExcavatorRate + BonusRate);
                    break;
                }
                case Types.Fishing:
                {
                    ConvertedAmount = Amount * (Rates.FishRate + BonusRate);
                    break;
                }
            }
		   		 		  						  	   		   		 		  	 	 		  						  				
            return Convert.ToInt32(ConvertedAmount);
        }
        
        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (item == null || quarry == null) return;

            // PrintError(GetQuarryPlayer(quarry.net.ID + 27447773688).ToString() + "   " + Converted(Types.Quarry, item.info.shortname,
            //     item.amount, null, GetQuarryPlayer(quarry.OwnerID).ToString()).ToString());
            item.amount = Converted(Types.Quarry, item.info.shortname, item.amount, null, GetQuarryPlayer(quarry.net.ID.Value).ToString());
        }
        public void SendChat(String Message, BasePlayer player, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                if (config.pluginSettings.ReferenceSettings.IQChatSetting.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, config.pluginSettings.ReferenceSettings.IQChatSetting.CustomPrefix, config.pluginSettings.ReferenceSettings.IQChatSetting.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        private const string prefabPatrol = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
        /// <summary>
        /// Обновление 1.94.31
        /// - Исправлен метод удаления чинука после обновления игры
        /// - Полностью изменен метод обнаружения стандартных ивентов
        /// 
        
        [PluginReference] Plugin IQChat;
		   		 		  						  	   		   		 		  	 	 		  						  				
        
        
        Item OnFishCatch(Item item, BaseFishingRod rod, BasePlayer player)
        {
            if (item == null || player == null) return null;

            Int32 Rate = Converted(Types.Fishing, item.info.shortname, item.amount, player);
            item.amount = Rate;
            return null;
        }
       
        private void OnEntitySpawned(BradleyAPC entity)
        {
            Configuration.PluginSettings.OtherSettings.EventSettings.Setting EvenTimer = config.pluginSettings.OtherSetting.EventSetting.BreadlaySetting;
            if (!EvenTimer.FullOff && !EvenTimer.UseEventCustom) return;
        
            NextTick(() =>
            {
                if (entity.OwnerID != 0 || entity.skinID != 0) return;
                entity.Kill();
            });
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || entity.net == null) return; 
            UInt64 NetID = entity.net.ID.Value;
            if (LootersListCrateID.Contains(NetID))
                LootersListCrateID.Remove(NetID);   
            
            if (LootersSaveListCrateID.Contains(NetID))
                LootersSaveListCrateID.Remove(NetID);    
        }
        public List<UInt64> LootersSaveListCrateID = new List<UInt64>();
		   		 		  						  	   		   		 		  	 	 		  						  				
        
                private TOD_Time timeComponent = null;
        public enum SkipType
        {
            Day,
            Night
        }

        
        public class ModiferTea
        {
            public Modifier.ModifierType Type;
            public Single Duration;
            public Single Value;
        }
        private void OnEntitySpawned(ScrapTransportHelicopter helicopter)
        {
            if (helicopter == null) return;
            FuelSystemRating(helicopter.GetFuelSystem(), config.pluginSettings.OtherSetting.FuelSetting.AmountScrapTransport);

            if (config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel)
                helicopter.fuelPerSec = config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.ConsumedScrapTransport;
        }
        
        Single GetBonusRate(BasePlayer player)
        {
            Single Bonus = 0;
            if (player == null) return Bonus;

            if (BonusRates.TryGetValue(player, out Single bonusRate))
                Bonus = bonusRate;
            
            return Bonus;
        }

        void OnSunset()
        {
            timeComponent.DayLengthInMinutes = config.pluginSettings.OtherSetting.NightTime * (24.0f / (24.0f - (TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime)));
            activatedDay = false;
            if (!config.pluginSettings.OtherSetting.UseSkipTime) return;
            if (config.pluginSettings.OtherSetting.TypeSkipped == SkipType.Night)
                TOD_Sky.Instance.Cycle.Hour = config.pluginSettings.OtherSetting.DayStart;
        }
        public enum TypeListUsed
        {
            WhiteList,
            BlackList
        }
        
        private void OnEntitySpawned(TrainCar trainCar) 
        {
            if (!config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel) return;
            if (trainCar == null) return;
            StorageContainer fuelContainer = (trainCar.GetFuelSystem() as EntityFuelSystem)?.GetFuelContainer();
            if (fuelContainer == null) return;
            if (!fuelContainer.TryGetComponent<TrainEngine>(out TrainEngine trainEngine)) return;
            trainEngine.maxFuelPerSec = config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.ConsumedTrain;
        } 
        private void OnServerInitialized()
        {
            _ = this;

            SpacePort = TerrainMeta.Path.Monuments.FirstOrDefault(p => p.name.ToLower().Contains("launch_site"));

            StartEvent();
            foreach (var RateCustom in config.pluginSettings.RateSetting.PrivilegyRates)
                Register(RateCustom.Key);

            foreach (Configuration.PluginSettings.Rates.RecyclerController.PresetRecycler presetRecycler in config.pluginSettings.RateSetting.RecyclersController.PrivilageSpeedRecycler)
                    Register(presetRecycler.Permissions);
            
            if (config.pluginSettings.RateSetting.UseSpeedBurnableList)
                foreach (var BurnableList in config.pluginSettings.RateSetting.SpeedBurableList)
                    Register(BurnableList.Permissions);    
		   		 		  						  	   		   		 		  	 	 		  						  				
            List<String> PrivilegyCustomRatePermissions = config.pluginSettings.RateSetting.CustomRatesPermissions.NightRates.Keys.Union(config.pluginSettings.RateSetting.CustomRatesPermissions.DayRates.Keys).ToList();
            foreach (var RateItemCustom in PrivilegyCustomRatePermissions)
                Register(RateItemCustom);

            timer.Once(5, GetTimeComponent);

            if (config.pluginSettings.RateSetting.IgnoreSpeedBurnablePrefabList.Contains("electric.furnace"))
                Unsubscribe(nameof(OnSwitchToggled));
		   		 		  						  	   		   		 		  	 	 		  						  				
            Configuration.PluginSettings.OtherSettings.EventSettings eventController = config.pluginSettings.OtherSetting.EventSetting;
            if (!config.pluginSettings.OtherSetting.FuelSetting.useAutoFillFuel && !config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel
                && (!eventController.BreadlaySetting.UseEventCustom && !eventController.BreadlaySetting.FullOff))
            {
                Unsubscribe(nameof(OnEntitySpawned));
            }
            
            if(!config.pluginSettings.RateSetting.useSpeedMixingTable)
                Unsubscribe(nameof(OnMixingTableToggle));
            else
            {
                foreach (Configuration.PluginSettings.Rates.SpeedMixingTable speedMixingTable in config.pluginSettings.RateSetting.speedMixingTables)
                    Register(speedMixingTable.Permissions);
            }
            
            foreach (BaseOven oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
            {
                if (config.pluginSettings.RateSetting.UseSpeedBurnable)
                {
                    if (config.pluginSettings.RateSetting.IgnoreSpeedBurnablePrefabList.Contains(oven.ShortPrefabName))
                        continue;
                    
                    OvenController.GetOrAdd(oven).TryRestart();
                }
            }

            if (!config.pluginSettings.RateSetting.UseSpeedBurnable)
            {
                Unsubscribe(nameof(OnOvenToggle));
                Unsubscribe(nameof(OnOvenStart));
            }
            
            if(!config.pluginSettings.RateSetting.RecyclersController.UseRecyclerSpeed)
                Unsubscribe(nameof(OnRecyclerToggle));
            
            if(!config.pluginSettings.RateSetting.UseTeaController)
                Unsubscribe(nameof(OnPlayerAddModifiers));
		   		 		  						  	   		   		 		  	 	 		  						  				
            if (config.pluginSettings.OtherSetting.FuelSetting.useAutoFillFuel ||
                config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel)
                initializeTransport = ServerMgr.Instance.StartCoroutine(InitializeTransport());
            
            if (config.pluginSettings.RateSetting.rateControllerDayOfWeek.useRateBonusDayOfWeek)
            {
                String errorValidDayOfWeek = IsValidConfigDayOfWeek();

                if (errorValidDayOfWeek != null && !String.IsNullOrWhiteSpace(errorValidDayOfWeek))
                {
                    PrintWarning(LanguageEn ? $"Attention! Intersections found in days! The bonus for days of the week is disabled!{errorValidDayOfWeek}" : $"Внимание! Найдены пересечения в днях! Бонус по дням недели отключен!{errorValidDayOfWeek}");
                    return;
                }
                
                Puts(LanguageEn ? $"Server Information: Day of the week: {DateTime.UtcNow.DayOfWeek}. Time (hours): {DateTime.UtcNow.Hour}" : $"Серверная информация : День недели : {DateTime.UtcNow.DayOfWeek}. Время (часы) : {DateTime.UtcNow.Hour}");
                UpdateBonusStatus(true);
                timer.Every(5, () => { UpdateBonusStatus(); });
            }
        }
        public List<UInt64> LootersListCrateID = new List<UInt64>();

        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            if (item == null || player == null) return;
            
            int Rate = Converted(Types.Gather, item.info.shortname, item.amount, player);
            item.amount = Rate;
        }
        
        void OnWireConnect(BasePlayer player, IOEntity entity1, int inputs, IOEntity entity2, int outputs)
        {
            BaseEntity parentIoEntity = entity1.GetParentEntity();
            if (parentIoEntity == null) return;
            if (parentIoEntity is not BaseOven oven) return;
            OvenController.GetOrAdd(oven).Switch(player); 
        }

        
                private void FuelSystemRating(IFuelSystem fuelSystem, Int32 Amount)
        {
            if (!config.pluginSettings.OtherSetting.FuelSetting.useAutoFillFuel) return;
            if (fuelSystem == null) return;
            if (fuelSystem is not EntityFuelSystem entityFuelSystem) return;
            
            NextTick(() =>
            {
                Item Fuel = entityFuelSystem.GetFuelItem();
                if (Fuel == null) return;
                
                if (Fuel.amount is 50 or 100)
                    Fuel.amount = Amount;
            });
        }
        
        private void OnNewSave(String filename)
        {
            DataQuarryPlayer.Clear();
            LootersSaveListCrateID.Clear();
            
            WriteData();
        }
        void SetTimeComponent()
        {
            if (!config.pluginSettings.OtherSetting.UseTime) return;

            Single hour = TOD_Sky.Instance.Cycle.Hour;
            Int32 dayStart = config.pluginSettings.OtherSetting.DayStart;
            Int32 nightStart = config.pluginSettings.OtherSetting.NightStart;

            Single dayDifference = Mathf.Abs(hour - dayStart);
            Single nightDifference = Mathf.Abs(hour - nightStart);

            if (dayDifference < nightDifference)
            {
                sendMessageDay = false;
                sendMessageNight = true;
            }
            else
            {
                sendMessageDay = true;
                sendMessageNight = false;
            }
            
            timeComponent.ProgressTime = true;
            timeComponent.UseTimeCurve = false;
            timeComponent.OnSunrise += OnSunrise;
            timeComponent.OnSunset += OnSunset;
            timeComponent.OnHour += OnHour;

            if (TOD_Sky.Instance.Cycle.Hour > TOD_Sky.Instance.SunriseTime && TOD_Sky.Instance.Cycle.Hour < TOD_Sky.Instance.SunsetTime)
                OnSunrise();
            else
                OnSunset();
        }
        
                
                public Dictionary<UInt64, UInt64> DataQuarryPlayer = new Dictionary<UInt64, UInt64>();
        
        
        private static Configuration config = new Configuration();
        void OnContainerDropItems(ItemContainer container)
        {
            if (container == null) return;
            var Container = container.entityOwner as LootContainer;
            if (Container == null) return;
            UInt64 NetID = Container.net.ID.Value;
            if (LootersListCrateID.Contains(NetID)) return;
            
            BasePlayer player = Container.lastAttacker as BasePlayer;
		   		 		  						  	   		   		 		  	 	 		  						  				
            foreach (var item in container.itemList)
                item.amount = Converted(Types.Loot, item.info.shortname, item.amount, player);
        }
        void API_BONUS_RATE_ADDPLAYER(BasePlayer player, Single Rate)
        {
            if (player == null) return;
            if (!BonusRates.ContainsKey(player))
                BonusRates.Add(player, Rate);
            else BonusRates[player] = Rate;
        }
        
        void SpawnHeli()
        {
            UnSubProSub();
        
            var position = new Vector3(ConVar.Server.worldsize, 100, ConVar.Server.worldsize) - new Vector3(50f, 0f, 50f);
            var entity = GameManager.server.CreateEntity(prefabPatrol, position);
            entity?.Spawn();
        }
        private void StartHelicopter(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (!EventSettings.HelicopterSetting.FullOff && EventSettings.HelicopterSetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.HelicopterSetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.HelicopterSetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.HelicopterSetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.HelicopterSetting.EventSpawnTime;
                timer.Once(TimeSpawn, () =>
                {
                    StartHelicopter(EventSettings);
                    SpawnHeli();
                    
                    if(EventSettings.HelicopterSetting.useGameTip)
                        MessageGameTipsError("ALERT_HELICOPTER");
                });
            }
        }
        void AddQuarryPlayer(UInt64 NetID, UInt64 userID)
        {
            if (DataQuarryPlayer.ContainsKey(NetID))
                DataQuarryPlayer[NetID] = userID;
            else DataQuarryPlayer.Add(NetID, userID);
        }
        private void OnEntitySpawned(Snowmobile snowmobiles)
        {
            if (snowmobiles == null) return;
            if (config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel)
                snowmobiles.maxFuelPerSec = config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.ConsumedSnowmobile;
        } 
        
                private BasePlayer ExcavatorPlayer = null;
        
        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            if (item == null || player == null) return;
            item.amount = Converted(Types.Growable, item.info.shortname, item.amount, player);
        }
        
        private void UpdateBonusStatus(Boolean isInit = false)
        {
            Configuration.PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays currentRateBonusDay = GetRateBonusForCurrentTime();
            
            if (currentRateBonusDay != rateDayOfWeek && !isInit) 
            {
                rateDayOfWeek = currentRateBonusDay;

                if (currentRateBonusDay != null)
                {
                    String startDayOfWeek = currentRateBonusDay.timeStartBonus.dayOfWeek.ToUpper();
                    String stopDayOfWeek = currentRateBonusDay.timeStopBonus.dayOfWeek.ToUpper();
                    
                    foreach (BasePlayer player in BasePlayer.activePlayerList)
                    {
                        SendChat(GetLang("RATE_BONUS_DAY_OF_WEEK", player.UserIDString, GetBonusRateDayOfWeek(player), 
                            GetLang(startDayOfWeek, player.UserIDString), $"{currentRateBonusDay.timeStartBonus.timeHours}:00",
                            GetLang(stopDayOfWeek, player.UserIDString), $"{currentRateBonusDay.timeStopBonus.timeHours}:00"), player);
                    }
                }
                else
                {
                    foreach (BasePlayer player in BasePlayer.activePlayerList)
                        SendChat(GetLang("RATE_BONUS_DAY_OF_WEEK_END", player.UserIDString), player);
                }
            }
        }
        bool IsTime()
        {
            var Settings = config.pluginSettings.OtherSetting;
            float TimeServer = TOD_Sky.Instance.Cycle.Hour;
            return TimeServer < Settings.NightStart && Settings.DayStart <= TimeServer;
        }
        float GetRareCoal(BasePlayer player = null)
        {
            Boolean IsTimes = IsTime();
		   		 		  						  	   		   		 		  	 	 		  						  				
            var Rates = IsTimes ? config.pluginSettings.RateSetting.DayRates : config.pluginSettings.RateSetting.NightRates;
            var PrivilegyRates = config.pluginSettings.RateSetting.PrivilegyRates;
		   		 		  						  	   		   		 		  	 	 		  						  				
            if (player != null)
            {
                foreach (var RatesSetting in PrivilegyRates)
                    if (IsPermission(player.UserIDString, RatesSetting.Key))
                        Rates = IsTimes ? RatesSetting.Value.DayRates : RatesSetting.Value.NightRates;
            }

            float Rare = Rates.CoalRare;
            float RareResult = (100 - Rare) / 100;
            return RareResult;
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        void SpawnCargo()
        {
            UnSubProSub();
            
            var cargoShip = GameManager.server.CreateEntity(prefabShip) as CargoShip;
            if (cargoShip == null) return;
            cargoShip.TriggeredEventSpawn();
            cargoShip.SendNetworkUpdate();
            cargoShip.RefreshActiveLayout();
            cargoShip.Spawn();
        }
        private class Configuration
        {
            [JsonProperty(LanguageEn ? "Plugin setup" : "Настройка плагина")]
            public PluginSettings pluginSettings = new PluginSettings();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    pluginSettings = new PluginSettings
                    {
                        ReferenceSettings = new PluginSettings.ReferencePlugin
                        {
                            IQChatSetting = new PluginSettings.ReferencePlugin.IQChatReference
                            {
                                CustomAvatar = "0",
                                CustomPrefix = "[IQRates]",
                                UIAlertUse = false,
                            },
                        },
                        RateSetting = new PluginSettings.Rates
                        {
                            UseTeaController = false,
                            UseBlackListPrefabs = false,
                            rateControllerDayOfWeek = new PluginSettings.Rates.RateControllerDayOfWeek
                            {
                                useRateBonusDayOfWeek = false,
                                rateBonusDayOfWeek = new List<PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays>
                                {
                                    new PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays
                                    {
                                        upBonusRate = 0.5f,
                                        privilageUpRates = new Dictionary<String, Single>()
                                        {
                                            ["iqrates.premium"] = 2.0f,
                                            ["iqrates.vip"] = 1.0f,
                                        },
                                        timeStartBonus = new PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays.TimeController()
                                        {
                                            dayOfWeek = "Saturday",
                                            timeHours = 18,
                                        },
                                        timeStopBonus = new PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays.TimeController()
                                        {
                                            dayOfWeek = "Monday",
                                            timeHours = 12,
                                        },
                                    },
                                    new PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays
                                    {
                                        upBonusRate = 0.5f,
                                        privilageUpRates = new Dictionary<String, Single>()
                                        {
                                            ["iqrates.premium"] = 2.0f,
                                            ["iqrates.vip"] = 1.0f,
                                        },
                                        timeStartBonus = new PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays.TimeController()
                                        {
                                            dayOfWeek = "Thursday",
                                            timeHours = 18,
                                        },
                                        timeStopBonus = new PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays.TimeController()
                                        {
                                            dayOfWeek = "Friday",
                                            timeHours = 12,
                                        },
                                    },
                                },
                            },
                            BlackListCategory = new List<String>()
                            {
                                "Weapon", 
                                "Ammunition",
                                "Traps",
                                "Attire",
                                "Items",
                                "Tool",
                                "Component"
                            },
                            WhiteList = new List<String>()
                            {
                                "scrap",
                                "rope",
                                "metalblade",
                                "propanetank",
                                "tarp",
                                "sewingkit",
                                "fuse",
                                "metalspring",
                                "roadsigns",
                                "sheetmetal",
                                "gears",
                                "riflebody",
                                "smgbody",
                                "semibody",
                            },
                            TypeList = TypeListUsed.BlackList,
                            BlackListPrefabs = new List<String>()
                            {
                                "crate_elite",
                                "crate_normal"
                            },
                            UseSpeedBurnable = true,
                            useSpeedMixingTable = false,
                            speedMixingTables = new List<PluginSettings.Rates.SpeedMixingTable>()
                            {
                                new PluginSettings.Rates.SpeedMixingTable()
                                {
                                    
                                    Permissions = "iqrates.default",
                                    SpeedMixing = 0,
                                },
                                new PluginSettings.Rates.SpeedMixingTable()
                                {
                                    
                                    Permissions = "iqrates.vip",
                                    SpeedMixing = 20,
                                },
                                new PluginSettings.Rates.SpeedMixingTable()
                                {
                                    
                                    Permissions = "iqrates.premium",
                                    SpeedMixing = 50,
                                },
                            },
                            SpeedBurnable = 3.5f,
                            SpeedFuelBurnable = 1,
                            UseBlackListBurnable = false,
                            BlackListBurnable = new List<String>
                            {
                                "wolfmeat.cooked",
                                "deermeat.cooked",
                                "meat.pork.cooked",
                                "humanmeat.cooked",
                                "chicken.cooked",
                                "bearmeat.cooked",
                                "horsemeat.cooked",
                            },
                            RecyclersController = new PluginSettings.Rates.RecyclerController
                            {
                                UseRecyclerSpeed = false,
                                DefaultSpeedRecycler = 5,
                                PrivilageSpeedRecycler = new List<PluginSettings.Rates.RecyclerController.PresetRecycler>
                                {
                                    new PluginSettings.Rates.RecyclerController.PresetRecycler 
                                    {
                                        Permissions = "iqrates.recyclerhyperfast",
                                        SpeedRecyclers = 0 
                                    },
                                   new PluginSettings.Rates.RecyclerController.PresetRecycler
                                   {
                                       Permissions = "iqrates.recyclerfast",
                                       SpeedRecyclers = 3
                                   },
                                },
                            },
                            UseSpeedBurnableList = true,
                            IgnoreSpeedBurnablePrefabList = new List<String>()
                            {
                               "",  
                            },
                            SpeedBurableList = new List<PluginSettings.Rates.SpeedBurnablePreset>
                            {
                                new PluginSettings.Rates.SpeedBurnablePreset
                                {
                                    Permissions = "iqrates.vip",
                                    SpeedBurnable = 5.0f,
                                    SpeedFuelBurnable = 20,
                                },
                                new PluginSettings.Rates.SpeedBurnablePreset
                                {
                                    Permissions = "iqrates.speedrun",
                                    SpeedBurnable = 55.0f,
                                    SpeedFuelBurnable = 20,
                                },
                                new PluginSettings.Rates.SpeedBurnablePreset
                                {
                                    Permissions = "iqrates.fuck",
                                    SpeedBurnable = 200f,
                                    SpeedFuelBurnable = 20,
                                },
                            },
                            DayRates = new PluginSettings.Rates.AllRates
                            {
                                GatherRate = 1.0f,
                                LootRate = 1.0f,
                                PickUpRate = 1.0f,
                                GrowableRate = 1.0f,
                                QuarryRate = 1.0f,
                                FishRate = 1.0f,
                                QuarryDetalis = new PluginSettings.Rates.AllRates.QuarryRateDetalis
                                {
                                    UseDetalisRateQuarry = false,
                                    ShortnameListQuarry = new Dictionary<String, Single>()
                                    {
                                        ["metal.ore"] = 10,
                                        ["sulfur.ore"] = 5
                                    }
                                },
                                ExcavatorRate = 1.0f,
                                CoalRare = 10,
                            },
                            NightRates = new PluginSettings.Rates.AllRates
                            {
                                GatherRate = 2.0f,
                                LootRate = 2.0f,
                                PickUpRate = 2.0f,
                                GrowableRate = 2.0f,
                                QuarryRate = 2.0f,
                                FishRate = 2.0f,
                                QuarryDetalis = new PluginSettings.Rates.AllRates.QuarryRateDetalis
                                {
                                    UseDetalisRateQuarry = false,
                                    ShortnameListQuarry = new Dictionary<String, Single>()
                                    {
                                        ["metal.ore"] = 10,
                                        ["sulfur.ore"] = 5
                                    }
                                },
                                ExcavatorRate = 2.0f,
                                CoalRare = 15,
                            },
                            CustomRatesPermissions = new PluginSettings.Rates.PermissionsRate
                            {
                                DayRates = new Dictionary<String, List<PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis>>
                                {
                                    ["iqrates.gg"] = new List<PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis>
                                    {
                                        new PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis
                                        {
                                            Rate = 200.0f,
                                            Shortname = "wood",
                                        },
                                        new PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis
                                        {
                                              Rate = 200.0f,
                                              Shortname = "stones",
                                        }
                                    }
                                },
                                NightRates = new Dictionary<string, List<PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis>>
                                {
                                    ["iqrates.gg"] = new List<PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis>
                                    {
                                        new PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis
                                        {
                                            Rate = 400.0f,
                                            Shortname = "wood",
                                        },
                                        new PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis
                                        {
                                              Rate = 400.0f,
                                              Shortname = "stones",
                                        }
                                    }
                                },
                            },
                            PrivilegyRates = new Dictionary<string, PluginSettings.Rates.DayAnNightRate>
                            {
                                ["iqrates.vip"] = new PluginSettings.Rates.DayAnNightRate
                                {
                                    DayRates =
                                    {
                                        GatherRate = 3.0f,
                                        LootRate = 3.0f,
                                        PickUpRate = 3.0f,
                                        QuarryRate = 3.0f,
                                        FishRate = 3.0f,
                                        QuarryDetalis = new PluginSettings.Rates.AllRates.QuarryRateDetalis
                                        {
                                            UseDetalisRateQuarry = false,
                                            ShortnameListQuarry = new Dictionary<String, Single>()
                                            {
                                                ["metal.ore"] = 10,
                                                ["sulfur.ore"] = 5
                                            }
                                        },
                                        GrowableRate = 3.0f,
                                        ExcavatorRate = 3.0f,
                                        CoalRare = 15,
                                    },
                                    NightRates = new PluginSettings.Rates.AllRates
                                    {
                                        GatherRate = 13.0f,
                                        LootRate = 13.0f,
                                        PickUpRate = 13.0f,
                                        GrowableRate = 13.0f,
                                        QuarryRate = 13.0f,
                                        FishRate = 13.0f,
                                        QuarryDetalis = new PluginSettings.Rates.AllRates.QuarryRateDetalis
                                        {
                                            UseDetalisRateQuarry = false,
                                            ShortnameListQuarry = new Dictionary<String, Single>()
                                            {
                                                ["metal.ore"] = 10,
                                                ["sulfur.ore"] = 5
                                            }
                                        },
                                        ExcavatorRate = 13.0f,
                                        CoalRare = 25,
                                    }
                                },
                                ["iqrates.premium"] = new PluginSettings.Rates.DayAnNightRate
                                {
                                    DayRates =
                                    {
                                        GatherRate = 3.5f,
                                        LootRate = 3.5f,
                                        PickUpRate = 3.5f,
                                        GrowableRate = 3.5f,
                                        QuarryRate = 3.5f,
                                        FishRate = 3.5f,
                                        QuarryDetalis = new PluginSettings.Rates.AllRates.QuarryRateDetalis
                                        {
                                            UseDetalisRateQuarry = false,
                                            ShortnameListQuarry = new Dictionary<String, Single>()
                                            {
                                                ["metal.ore"] = 10,
                                                ["sulfur.ore"] = 5
                                            }
                                        },
                                        ExcavatorRate = 3.5f,
                                        CoalRare = 20,
                                    },
                                    NightRates = new PluginSettings.Rates.AllRates
                                    {
                                        GatherRate = 13.5f,
                                        LootRate = 13.5f,
                                        PickUpRate = 13.5f,
                                        GrowableRate = 13.5f,
                                        QuarryRate = 13.5f,
                                        FishRate = 13.5f,
                                        QuarryDetalis = new PluginSettings.Rates.AllRates.QuarryRateDetalis
                                        {
                                            UseDetalisRateQuarry = false,
                                            ShortnameListQuarry = new Dictionary<String, Single>()
                                            {
                                                ["metal.ore"] = 10,
                                                ["sulfur.ore"] = 5
                                            }
                                        },
                                        ExcavatorRate = 13.5f,
                                        CoalRare = 20,
                                    }
                                },
                            },
                            BlackList = new List<String>
                            {
                                "shortname",
                            },
                        },
                        OtherSetting = new PluginSettings.OtherSettings
                        {
                            UseAlertDayNight = true,
                            UseSkipTime = true,
                            TypeSkipped = SkipType.Night,
                            UseTime = false,
                            FreezeTime = 12,
                            UseFreezeTime = true,
                            DayStart = 10,
                            NightStart = 22,
                            DayTime = 5,
                            NightTime = 1,
                            FuelConsumedTransportSetting = new PluginSettings.OtherSettings.FuelConsumedTransport
                            {
                                useConsumedFuel = false,
                                ConsumedHotAirBalloon = 0.25f,
                                ConsumedSnowmobile = 0.15f,
                                ConsumedTrain = 0.15f,
                                ConsumedBoat = 0.25f,
                                ConsumedSubmarine = 0.15f,
                                ConsumedCopter = 0.25f,
                                ConsumedScrapTransport = 0.25f,
                                ConsumedAttackHelicopter = 0.25f,
                            },
                            FuelSetting = new PluginSettings.OtherSettings.FuelSettings
                            {
                                useAutoFillFuel = false, 
                                AmountBoat = 200,
                                AmountMinicopter = 200,
                                AmountScrapTransport = 200,
                                AmountSubmarine = 200,
                                AmountAttackHelicopter = 200,
                            },
                            EventSetting = new PluginSettings.OtherSettings.EventSettings
                            {
                                BreadlaySetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    useGameTip = false,
                                    FullOff = false,
                                    UseEventCustom = true,
                                    EventSpawnTime = 3000,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = false,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 1000,
                                    },
                                },
                                CargoPlaneSetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    useGameTip = false,
                                    FullOff = false,
                                    UseEventCustom = true,
                                    EventSpawnTime = 5000,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = false,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 1000,
                                    },
                                },
                                CargoShipSetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    useGameTip = false,
                                    FullOff = false,
                                    UseEventCustom = true,
                                    EventSpawnTime = 4500,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = true,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 8000,
                                    },
                                },
                                ChinoockSetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    useGameTip = false,
                                    FullOff = true,
                                    UseEventCustom = false,
                                    EventSpawnTime = 3000,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = false,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 1000,
                                    },
                                },
                                HelicopterSetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    useGameTip = false,
                                    FullOff = true,
                                    UseEventCustom = false,
                                    EventSpawnTime = 3000,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = false,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 1000,
                                    },
                                },
                            }
                        },
                    }
                };
            }
		   		 		  						  	   		   		 		  	 	 		  						  				
            internal class PluginSettings
            {

                internal class ReferencePlugin
                {
                    internal class IQChatReference
                    {
                        [JsonProperty(LanguageEn ? "IQChat : Custom chat avatar (If required)" : "IQChat : Кастомный аватар в чате(Если требуется)")]
                        public String CustomAvatar = "0";
                        [JsonProperty(LanguageEn ? "IQChat : Use UI Notifications" : "IQChat : Использовать UI уведомления")]
                        public Boolean UIAlertUse = false;
                        [JsonProperty(LanguageEn ? "IQChat : Custom prefix in chat" : "IQChat : Кастомный префикс в чате")]
                        public String CustomPrefix = "[IQRates]";
                    }
                    [JsonProperty(LanguageEn ? "Setting up IQChat" : "Настройка IQChat")]
                    public IQChatReference IQChatSetting = new IQChatReference();
                }
                [JsonProperty(LanguageEn ? "Rating settings" : "Настройка рейтингов")]
                public Rates RateSetting = new Rates();
                [JsonProperty(LanguageEn ? "Additional plugin settings" : "Дополнительная настройка плагина")]
                public OtherSettings OtherSetting = new OtherSettings();     
                internal class OtherSettings
                {
                    [JsonProperty(LanguageEn ? "Event settings on the server" : "Настройки ивентов на сервере")]
                    public EventSettings EventSetting = new EventSettings();   
                    [JsonProperty(LanguageEn ? "Fuel settings when buying vehicles from NPCs" : "Настройки топлива при покупке транспорта у NPC")]
                    public FuelSettings FuelSetting = new FuelSettings();
                    [JsonProperty(LanguageEn ? "Fuel Consumption Rating Settings" : "Настройки рейтинга потребления топлива")]
                    public FuelConsumedTransport FuelConsumedTransportSetting = new FuelConsumedTransport();
                    internal class FuelConsumedTransport
                    {
                        [JsonProperty(LanguageEn ? "Use the fuel consumption rating in transport (true - yes/false - no)" : "Использовать рейтинг потребления топлива в транспорте (true - да/false - нет)")]
                        public Boolean useConsumedFuel;
                        [JsonProperty(LanguageEn ? "Hotairballoon fuel consumption rating (Default = 0.25)" : "Рейтинг потребление топлива у воздушного шара (Стандартно = 0.25)")]
                        public Single ConsumedHotAirBalloon= 0.25f;
                        [JsonProperty(LanguageEn ? "Snowmobile fuel consumption rating (Default = 0.15)" : "Рейтинг потребление топлива снегоходов (Стандартно = 0.15)")]
                        public Single ConsumedSnowmobile = 0.15f;         
                        [JsonProperty(LanguageEn ? "Train fuel consumption rating (Default = 0.15)" : "Рейтинг потребление топлива поездов (Стандартно = 0.15)")]
                        public Single ConsumedTrain = 0.15f;
                        [JsonProperty(LanguageEn ? "Rowboat fuel consumption rating (Default = 0.25)" : "Рейтинг потребление топлива у лодок (Стандартно = 0.25)")]
                        public Single ConsumedBoat = 0.25f;
                        [JsonProperty(LanguageEn ? "Submarine fuel consumption rating (Default = 0.15)" : "Рейтинг потребление топлива у субмарин (Стандартно = 0.15)")]
                        public Single ConsumedSubmarine = 0.15f;
                        [JsonProperty(LanguageEn ? "Minicopter fuel consumption rating (Default = 0.25)" : "Рейтинг потребление топлива у миникоптера (Стандартно = 0.25)")]
                        public Single ConsumedCopter = 0.25f;
                        [JsonProperty(LanguageEn ? "ScrapTransportHelicopter fuel consumption rating (Default = 0.25)" : "Рейтинг потребление топлива у коровы (Стандартно = 0.25)")]
                        public Single ConsumedScrapTransport = 0.25f;
                        [JsonProperty(LanguageEn ? "Attack-Helicopter fuel consumption rating (Default = 0.25)" : "Рейтинг потребление топлива у боевого-вертолета (Стандартно = 0.25)")]
                        public Single ConsumedAttackHelicopter = 0.25f;
                    }
                    internal class FuelSettings
                    {
                        [JsonProperty(LanguageEn ? "Use fuel replenishment when buying vehicles from NPC (true - yes/false - no)" : "Использовать пополнение топлива при покупке транспорта у NPC (true - да/false - нет)")]
                        public Boolean useAutoFillFuel;
                        [JsonProperty(LanguageEn ? "Amount of fuel for boats" : "Кол-во топлива у лодок")]
                        public Int32 AmountBoat = 200;
                        [JsonProperty(LanguageEn ? "The amount of fuel in submarines" : "Кол-во топлива у подводных лодок")]
                        public Int32 AmountSubmarine = 200;
                        [JsonProperty(LanguageEn ? "Minicopter fuel quantity" : "Кол-во топлива у миникоптера")]
                        public Int32 AmountMinicopter = 200;
                        [JsonProperty(LanguageEn ? "Helicopter fuel quantity" : "Кол-во топлива у вертолета")]
                        public Int32 AmountScrapTransport = 200;
                        [JsonProperty(LanguageEn ? "Attack-Helicopter fuel quantity" : "Кол-во топлива у боевого вертолета")]
                        public Int32 AmountAttackHelicopter = 200;
                    }

                    [JsonProperty(LanguageEn ? "Use Time Acceleration" : "Использовать ускорение времени")]
                    public Boolean UseTime;
                    [JsonProperty(LanguageEn ? "Use time freeze (the time will be the one you set in the item &lt;Frozen time on the server&gt;)" : "Использовать заморозку времени(время будет такое, какое вы установите в пунке <Замороженное время на сервере>)")]
                    public Boolean UseFreezeTime;
                    [JsonProperty(LanguageEn ? "Frozen time on the server (Set time that will not change and be forever on the server, must be true on &lt;Use time freeze&gt;" : "Замороженное время на сервере (Установите время, которое не будет изменяться и будет вечно на сервере, должен быть true на <Использовать заморозку времени>")]
                    public Int32 FreezeTime;
                    [JsonProperty(LanguageEn ? "What time will the day start?" : "Укажите во сколько будет начинаться день")]
                    public Int32 DayStart;
                    [JsonProperty(LanguageEn ? "What time will the night start?" : "Укажите во сколько будет начинаться ночь")]
                    public Int32 NightStart;
                    [JsonProperty(LanguageEn ? "Specify how long the day will be in minutes" : "Укажите сколько будет длится день в минутах")]
                    public Int32 DayTime;
                    [JsonProperty(LanguageEn ? "Specify how long the night will last in minutes" : "Укажите сколько будет длится ночь в минутах")]
                    public Int32 NightTime;

                    [JsonProperty(LanguageEn ? "Use notification of players about the change of day and night (switching rates. The message is configured in the lang)" : "Использовать уведомление игроков о смене дня и ночи (переключение рейтов. Сообщение настраивается в лэнге)")]
                    public Boolean UseAlertDayNight = true;
                    [JsonProperty(LanguageEn ? "Enable the ability to completely skip the time of day (selected in the paragraph below)" : "Включить возможность полного пропуска времени суток(выбирается в пункте ниже)")]
                    public Boolean UseSkipTime = true;
                    [JsonProperty(LanguageEn ? "Select the type of time-of-day skip (0 - Skip day, 1 - Skip night)" : "Выберите тип пропуска времени суток (0 - Пропускать день, 1 - Пропускать ночь)(Не забудьте включить возможность полного пропуска времени суток)")]
                    public SkipType TypeSkipped = SkipType.Night;
		   		 		  						  	   		   		 		  	 	 		  						  				
                    internal class EventSettings
                    {
                        [JsonProperty(LanguageEn ? "Helicopter spawn custom settings" : "Кастомные настройки спавна вертолета")]
                        public Setting HelicopterSetting = new Setting();
                        [JsonProperty(LanguageEn ? "Custom tank spawn settings" : "Кастомные настройки спавна танка")]
                        public Setting BreadlaySetting = new Setting();
                        [JsonProperty(LanguageEn ? "Custom ship spawn settings" : "Кастомные настройки спавна корабля")]
                        public Setting CargoShipSetting = new Setting();
                        [JsonProperty(LanguageEn ? "Airdrop spawn custom settings" : "Кастомные настройки спавна аирдропа")]
                        public Setting CargoPlaneSetting = new Setting();
                        [JsonProperty(LanguageEn ? "Chinook custom spawn settings" : "Кастомные настройки спавна чинука")]
                        public Setting ChinoockSetting = new Setting();
                        internal class Setting
                        {
                            [JsonProperty(LanguageEn ? "Use notifications for running events (configurable in lang)" : "Использовать уведомления у запущенных ивентах (настраивается в lang)")]
                            public Boolean useGameTip;
                            [JsonProperty(LanguageEn ? "Completely disable event spawning on the server (true - yes/false - no)" : "Полностью отключить спавн ивента на сервере(true - да/false - нет)")]
                            public Boolean FullOff;
                            [JsonProperty(LanguageEn ? "Enable custom spawn event (true - yes/false - no)" : "Включить кастомный спавн ивент(true - да/false - нет)")]
                            public Boolean UseEventCustom;
                            [JsonProperty(LanguageEn ? "Static event spawn time" : "Статическое время спавна ивента")]
                            public Int32 EventSpawnTime;
                            [JsonProperty(LanguageEn ? "Random spawn time settings" : "Настройки случайного времени спавна")]
                            public RandomingTime RandomTimeSpawn = new RandomingTime();
                            internal class RandomingTime
                            {
                                [JsonProperty(LanguageEn ? "Use random event spawn time (static time will not be taken into account) (true - yes/false - no)" : "Использовать случайное время спавно ивента(статическое время не будет учитываться)(true - да/false - нет)")]
                                public Boolean UseRandomTime;
                                [JsonProperty(LanguageEn ? "Minimum event spawn value" : "Минимальное значение спавна ивента")]
                                public Int32 MinEventSpawnTime;
                                [JsonProperty(LanguageEn ? "Max event spawn value" : "Максимальное значении спавна ивента")]
                                public Int32 MaxEventSpawnTime;
                            }
                        }
                    }
                }

                internal class Rates
                {
                    [JsonProperty(LanguageEn ? "Ranking setting during the day" : "Настройка рейтинга днем")]
                    public AllRates DayRates = new AllRates();
                    [JsonProperty(LanguageEn ? "Setting the rating at night" : "Настройка рейтинга ночью")]
                    public AllRates NightRates = new AllRates();
                    [JsonProperty(LanguageEn ? "Setting privileges and ratings specifically for them [iqrates.vip] = { Setting } (Descending)" : "Настройка привилегий и рейтингов конкретно для них [iqrates.vip] = { Настройка } (По убыванию)")]
                    public Dictionary<String, DayAnNightRate> PrivilegyRates = new Dictionary<String, DayAnNightRate>();
                    [JsonProperty(LanguageEn ? "Setting up rating increase by days of the week" : "Настройка увеличения рейтинга по дням недели")]
                    public RateControllerDayOfWeek rateControllerDayOfWeek = new RateControllerDayOfWeek();
                    internal class RateControllerDayOfWeek
                    {
                        [JsonProperty(LanguageEn ? "Using the function to increase ratings based on the days of the week (time and days are taken from your server machine)" : "Использовать функцию увеличения рейтинга по дням недели (время и дни берутся с вашей серверной машины)")]
                        public Boolean useRateBonusDayOfWeek;
                        [JsonProperty(LanguageEn ? "Setting the days of the week and time intervals (make sure that different intervals do not overlap with each other, otherwise it may lead to conflicts)" : "Настройка дней недели и промежутка времени (учтите, чтобы разные промежутка не пересекались друг с другом, иначе это может привести к конфликту)")]
                        public List<RateBonusDays> rateBonusDayOfWeek = new();

                        internal class RateBonusDays
                        {
                            [JsonProperty(LanguageEn ? "How much additional rating will be added to the xN player? For example, if a player has x4, and this parameter is 1.5, the player's rating will be x5.5" : "Сколько будет добавлено дополнительного рейтинга к xN игрока (Например : у игрока х4, данный параметр равен 1.5, в результате у игрока будет x5.5)")]
                            public Single upBonusRate;
                            [JsonProperty(LanguageEn ? "How much additional rating will be added to the xN player, based on permissions (Permission = xN)" : "Сколько будет добавлено дополнительного рейтинга к xN игрока, по привилегиям (Permission = xN)")]
                            public Dictionary<String, Single> privilageUpRates = new Dictionary<String, Single>();
                            [JsonProperty(LanguageEn ? "Configuration of the day and time for starting the bonus rating" : "Настройка дня и времени запуска бонусного рейтинга")]
                            public TimeController timeStartBonus = new();
                            [JsonProperty(LanguageEn ? "Configuration of the day and time for the end of the bonus rating" : "Настройка дня и времени заверешения бонусного рейтинга")]
                            public TimeController timeStopBonus = new();

                            internal class TimeController
                            {
                                [JsonProperty(LanguageEn ? "Specify the hours (24-hour format)" : "Укажите часы (Формат 24 часа)")]
                                public Int32 timeHours;
                                [JsonProperty(LanguageEn ? "Day of the week: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday" : "День недели : Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday")]
                                public String dayOfWeek;
                            }
                        }
                    }

                    [JsonProperty(LanguageEn ? "Setting custom rates (items) by permission - setting (Descending)" : "Настройка кастомных рейтов(предметов) по пермишенсу - настройка (По убыванию)")]
                    public PermissionsRate CustomRatesPermissions = new PermissionsRate();
		   		 		  						  	   		   		 		  	 	 		  						  				
                    [JsonProperty(LanguageEn ? "Select the type of sheet to use: 0 - White sheet, 1 - Black sheet (White sheet - the ratings of only those items that are in it increase, the rest are ignored | The Black sheet is completely the opposite)"
                                             : "Выберите тип используемого листа : 0 - Белый лист, 1 - Черный лист (Белый лист - увеличиваются рейтинги только тех предметов - которые в нем, остальные игнорируются | Черный лист полностью наоборот)")]
                    public TypeListUsed TypeList = TypeListUsed.BlackList;
                    [JsonProperty(LanguageEn ? "Black list of items that will not be categorically affected by the rating" : "Черный лист предметов,на которые катигорично не будут действовать рейтинг")]
                    public List<String> BlackList = new List<String>();
                    [JsonProperty(LanguageEn ? "A white list of items that will ONLY be affected by ratings - the rest is ignored" : "Белый лист предметов, на которые ТОЛЬКО будут действовать рейтинги - остальное игнорируются")]
                    public List<String> WhiteList = new List<String>();
                    [JsonProperty(LanguageEn ? "A blacklist of categories that will NOT be affected by ratings" : "Черный список категорий на которые НЕ БУДУТ действовать рейтинги")]
                    public List<String> BlackListCategory = new List<String>();

                    [JsonProperty(LanguageEn ? "Use a tea controller? (Works on scrap, ore and wood tea). If enabled, it will set % to production due to the difference between rates (standard / privileges)" : "Использовать контроллер чая? (Работае на скрап, рудный и древесный чай). Если включено - будет устанавливать % к добычи за счет разницы между рейтами (стандартный / привилегии)")]
                    public Boolean UseTeaController;
                    [JsonProperty(LanguageEn ? "Use a prefabs blacklist?" : "Использовать черный лист префабов?")]
                    public Boolean UseBlackListPrefabs;
                    [JsonProperty(LanguageEn ? "Black list of prefabs(ShortPrefabName) - which will not be affected by ratings" : "Черный лист префабов(ShortPrefabName) - на которые не будут действовать рейтинги")]
                    public List<String> BlackListPrefabs = new List<String>();
                    
                    [JsonProperty(LanguageEn ? "Enable melting speed in furnaces (true - yes/false - no)" : "Включить скорость плавки в печах(true - да/false - нет)")]
                    public Boolean UseSpeedBurnable;
                    [JsonProperty(LanguageEn ? "Smelting Fuel Usage Rating (If the list is enabled, this value will be the default value for all non-licensed)" : "Рейтинг использования топлива при переплавки(Если включен список - это значение будет стандартное для всех у кого нет прав)")]
                    public Int32 SpeedFuelBurnable = 1;
                    [JsonProperty(LanguageEn ? "Use a blacklist of items for melting?" : "Использовать черный список предметов для плавки?")]
                    public Boolean UseBlackListBurnable = false;
                    [JsonProperty(LanguageEn ? "A black list of items for the stove, which will not be categorically affected by melting" : "Черный лист предметов для печки,на которые катигорично не будут действовать плавка")]
                    public List<String> BlackListBurnable = new List<String>();
                    [JsonProperty(LanguageEn ? "Furnace smelting speed (If the list is enabled, this value will be the default for everyone who does not have rights)" : "Скорость плавки печей(Если включен список - это значение будет стандартное для всех у кого нет прав)")]
                    public Single SpeedBurnable;
                    [JsonProperty(LanguageEn ? "Enable list of melting speed in furnaces (true - yes/false - no)" : "Включить список скорости плавки в печах(true - да/false - нет)")]
                    public Boolean UseSpeedBurnableList;
                    [JsonProperty(LanguageEn ? "Setting the melting speed in furnaces by privileges" : "Настройка скорости плавки в печах по привилегиям")]
                    public List<SpeedBurnablePreset> SpeedBurableList = new List<SpeedBurnablePreset>();
                    [JsonProperty(LanguageEn ? "Enable tea mixing acceleration in the mixing table (true - yes/false - no)" : "Включить ускорение смешивания чая в столе смешивания (true - да/false - нет)")]
                    public Boolean useSpeedMixingTable;
                    [JsonProperty(LanguageEn ? "Setting the tea mixing speed" : "Настройка скорости смешивания чая")]
                    public List<SpeedMixingTable> speedMixingTables = new List<SpeedMixingTable>();
                    [JsonProperty(LanguageEn ? "A blacklist of prefabs (ShortPrefabName) that will not be affected by acceleration (example: campfire) [If you don't need it, leave it empty]" : "Черный список префабов(ShortPrefabName), на которые не будет действовать ускорение (пример : campfire) [Если вам не нужно - оставьте пустым]")]
                    public List<String> IgnoreSpeedBurnablePrefabList = new List<String>();
                    
                    internal class DayAnNightRate
                    {
                        [JsonProperty(LanguageEn ? "Ranking setting during the day" : "Настройка рейтинга днем")]
                        public AllRates DayRates = new AllRates();
                        [JsonProperty(LanguageEn ? "Setting the rating at night" : "Настройка рейтинга ночью")]
                        public AllRates NightRates = new AllRates();
                    }
                    
                    [JsonProperty(LanguageEn ? "Setting up a recycler" : "Настройка переработчика")]
                    public RecyclerController RecyclersController = new RecyclerController(); 
                    internal class RecyclerController
                    {
                        [JsonProperty(LanguageEn ? "Use the processing time editing functions" : "Использовать функции редактирования времени переработки")]
                        public Boolean UseRecyclerSpeed;
                        [JsonProperty(LanguageEn ? "Static processing time (in seconds) (Will be set according to the standard or if the player does not have the privilege) (Default = 5s)" : "Статичное время переработки (в секундах) (Будет установлено по стандарту или если у игрока нет привилеии) (По умолчанию = 5с)")]
                        public Single DefaultSpeedRecycler;

                        [JsonProperty(LanguageEn ? "Setting the processing time for privileges (adjust from greater privilege to lesser privilege)" : "Настройка времени переработки для привилегий (настраивать от большей привилегии к меньшей)")]
                        public List<PresetRecycler> PrivilageSpeedRecycler = new List<PresetRecycler>();
                        internal class PresetRecycler
                        {
                            [JsonProperty(LanguageEn ? "Permissions" : "Права")]
                            public String Permissions;
                            [JsonProperty(LanguageEn ? "Standard processing time (in seconds)" : "Стандартное время переработки (в секундах)")]
                            public Single SpeedRecyclers;
                        }
                    }
                    internal class SpeedBurnablePreset
                    {
                        [JsonProperty(LanguageEn ? "Permissions" : "Права")]
                        public String Permissions;
                        [JsonProperty(LanguageEn ? "Furnace melting speed" : "Скорость плавки печей")]
                        public Single SpeedBurnable;
                        [JsonProperty(LanguageEn ? "Smelting Fuel Use Rating" : "Рейтинг использования топлива при переплавки")]
                        public Int32 SpeedFuelBurnable = 1;
                    }
                    
                    internal class SpeedMixingTable
                    {
                        [JsonProperty(LanguageEn ? "Permissions" : "Права")]
                        public String Permissions;
                        [JsonProperty(LanguageEn ? "Mixing speed (0-100%)" : "Скорость смешивания (0-100%)")]
                        public Int32 SpeedMixing;
                    }
                    
                    internal class PermissionsRate
                    {
                        [JsonProperty(LanguageEn ? "Ranking setting during the day" : "Настройка рейтинга днем")]
                        public Dictionary<String, List<PermissionsRateDetalis>> DayRates = new Dictionary<String, List<PermissionsRateDetalis>>();
                        [JsonProperty(LanguageEn ? "Setting the rating at night" : "Настройка рейтинга ночью")]
                        public Dictionary<String, List<PermissionsRateDetalis>> NightRates = new Dictionary<String, List<PermissionsRateDetalis>>();
                        public class PermissionsRateDetalis
                        {
                            [JsonProperty(LanguageEn ? "Shortname" : "Shortname")]
                            public String Shortname;
                            [JsonProperty(LanguageEn ? "Rate" : "Рейтинг")]
                            public Single Rate;
                        }
                    }
                    internal class AllRates
                    {
                        [JsonProperty(LanguageEn ? "Rating of extracted resources" : "Рейтинг добываемых ресурсов")]
                        public Single GatherRate;
                        [JsonProperty(LanguageEn ? "Rating of found items" : "Рейтинг найденных предметов")]
                        public Single LootRate;
                        [JsonProperty(LanguageEn ? "Pickup Rating" : "Рейтинг поднимаемых предметов")]
                        public Single PickUpRate;
                        [JsonProperty(LanguageEn ? "Rating of plants raised from the beds" : "Рейтинг поднимаемых растений с грядок")]
                        public Single GrowableRate = 1.0f;
                        [JsonProperty(LanguageEn ? "Quarry rating" : "Рейтинг карьеров")]
                        public Single QuarryRate;
                        [JsonProperty(LanguageEn ? "Detailed rating settings in the career" : "Детальная настройка рейтинга в карьере")]
                        public QuarryRateDetalis QuarryDetalis = new QuarryRateDetalis();
                        [JsonProperty(LanguageEn ? "Excavator Rating" : "Рейтинг экскаватора")]
                        public Single ExcavatorRate;
                        [JsonProperty(LanguageEn ? "Coal drop chance" : "Шанс выпадения угля")]
                        public Single CoalRare;
                        [JsonProperty(LanguageEn ? "Rating of items caught from the sea (fishing)" : "Рейтинг предметов выловленных с моря (рыбалки)")]
                        public Single FishRate;

                        internal class QuarryRateDetalis
                        {
                            [JsonProperty(LanguageEn ? "Use the detailed setting of the career rating (otherwise the 'Career Rating' will be taken for all subjects)" : "Использовать детальную настройку рейтинга карьеров (иначе будет браться 'Рейтинг карьеров' для всех предметов)")]
                            public Boolean UseDetalisRateQuarry;
                            [JsonProperty(LanguageEn ? "The item dropped out of the career - rating" : "Предмет выпадаемый из карьера - рейтинг")]
                            public Dictionary<String, Single> ShortnameListQuarry = new Dictionary<String, Single>();
                        }
                    }
                }
                [JsonProperty(LanguageEn ? "Configuring supported plugins" : "Настройка поддерживаемых плагинов")]
                public ReferencePlugin ReferenceSettings = new ReferencePlugin();
            }
        }
        
        object OnContainerDropGrowable(ItemContainer container, Item item)
        {
            if (container == null) return false;
            var Container = container.entityOwner as LootContainer;
            if (Container == null) return false;
            UInt64 NetID = Container.net.ID.Value;
            if (NetID == 14358899 && item.info.itemid == 1998363) return false;

            return null;
        }

                private Configuration.PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays rateDayOfWeek = null;
        //ss
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();

                if (config.pluginSettings.RateSetting.BlackListCategory == null)
                    config.pluginSettings.RateSetting.BlackListCategory = new List<String>()
                    {
                        "Weapon", 
                        "Ammunition",
                        "Traps",
                        "Attire",
                        "Items",
                        "Tools",
                        "Component"
                    };
                if (config.pluginSettings.RateSetting.WhiteList == null)
                {
                    config.pluginSettings.RateSetting.WhiteList = new List<String>()
                    {
                        "wood",
                        "sulfur.ore"
                    };
                }
                
                if (config.pluginSettings.RateSetting.RecyclersController.DefaultSpeedRecycler == 0f)
                    config.pluginSettings.RateSetting.RecyclersController.DefaultSpeedRecycler = 5f;
                if (config.pluginSettings.RateSetting.BlackListPrefabs == null ||
                    config.pluginSettings.RateSetting.BlackListPrefabs.Count == 0)
                    config.pluginSettings.RateSetting.BlackListPrefabs = new List<String>()
                    {
                        "crate_elite",
                        "crate_normal"
                    };
                    
                if (config.pluginSettings.RateSetting.RecyclersController.PrivilageSpeedRecycler == null ||
                    config.pluginSettings.RateSetting.RecyclersController.PrivilageSpeedRecycler.Count == 0)
                {
                    config.pluginSettings.RateSetting.RecyclersController.PrivilageSpeedRecycler =
                        new List<Configuration.PluginSettings.Rates.RecyclerController.PresetRecycler>()
                        {
                            new Configuration.PluginSettings.Rates.RecyclerController.PresetRecycler()
                            {
                                Permissions = "iqrates.recyclerhyperfast",
                                SpeedRecyclers = 0
                            },
                            new Configuration.PluginSettings.Rates.RecyclerController.PresetRecycler()
                            {
                                Permissions = "iqrates.recyclerfast",
                                SpeedRecyclers = 3
                            },
                        };
                }
		   		 		  						  	   		   		 		  	 	 		  						  				
                if (config.pluginSettings.RateSetting.DayRates.QuarryDetalis.ShortnameListQuarry.Count == 0)
                    config.pluginSettings.RateSetting.DayRates.QuarryDetalis =
                        new Configuration.PluginSettings.Rates.AllRates.QuarryRateDetalis()
                        {
                            UseDetalisRateQuarry = false,
                            ShortnameListQuarry = new Dictionary<String, Single>()
                            {
                                ["metal.ore"] = 10,
                                ["sulfur.ore"] = 5
                            }
                        };
                
                if (config.pluginSettings.RateSetting.NightRates.QuarryDetalis.ShortnameListQuarry.Count == 0)
                    config.pluginSettings.RateSetting.NightRates.QuarryDetalis =
                        new Configuration.PluginSettings.Rates.AllRates.QuarryRateDetalis()
                        {
                            UseDetalisRateQuarry = false,
                            ShortnameListQuarry = new Dictionary<String, Single>()
                            {
                                ["metal.ore"] = 10,
                                ["sulfur.ore"] = 5
                            }
                        };

                if (config.pluginSettings.RateSetting.speedMixingTables == null || config.pluginSettings.RateSetting.speedMixingTables.Count == 0)
                {
                    config.pluginSettings.RateSetting.speedMixingTables =
                        new List<Configuration.PluginSettings.Rates.SpeedMixingTable>()
                        {
                            new Configuration.PluginSettings.Rates.SpeedMixingTable()
                            {

                                Permissions = "iqrates.default",
                                SpeedMixing = 0,
                            },
                            new Configuration.PluginSettings.Rates.SpeedMixingTable()
                            {

                                Permissions = "iqrates.vip",
                                SpeedMixing = 20,
                            },
                            new Configuration.PluginSettings.Rates.SpeedMixingTable()
                            {

                                Permissions = "iqrates.premium",
                                SpeedMixing = 50,
                            },
                        };
                }
            }
            catch
            {                       
                PrintWarning(LanguageEn ? "Error #334332143" + $"read configuration 'oxide/config/{Name}', create a new configuration!!" : "Ошибка #334343" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!"); 
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        private void GetTimeComponent()
        {
            timeComponent = TOD_Sky.Instance.Components.Time;
            if (timeComponent == null) return;
            SetTimeComponent();
            StartupFreeze();
        }

        bool IsBlackList(string Shortname)
        {
            var BlackList = config.pluginSettings.RateSetting.BlackList;
            if (BlackList.Contains(Shortname))
                return true;
            else return false;
        }  
        
        private MonumentInfo SpacePort;
        private object OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            if (config.pluginSettings.RateSetting.IgnoreSpeedBurnablePrefabList.Contains(oven.ShortPrefabName))
                return null;
            
            return OvenController.GetOrAdd(oven).Switch(player);
        }
        bool IsWhiteList(string Shortname)
        {
            var WhiteList = config.pluginSettings.RateSetting.WhiteList;
            if (WhiteList.Contains(Shortname))
                return true;
            else return false;
        }      

        private void OnEntitySpawned(BaseSubmarine submarine)
        {
            if (submarine == null) return;
            FuelSystemRating(submarine.GetFuelSystem(), config.pluginSettings.OtherSetting.FuelSetting.AmountSubmarine);

            if (config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel)
                submarine.maxFuelPerSec = config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.ConsumedSubmarine;
        }
        private void OnEntitySpawned(MotorRowboat boat)
        {
            if (boat == null) return;
            FuelSystemRating(boat.GetFuelSystem(), config.pluginSettings.OtherSetting.FuelSetting.AmountBoat);

            if (config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel)
                boat.fuelPerSec = config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.ConsumedBoat;
        } 
        Single GetRateQuarryDetalis(Configuration.PluginSettings.Rates.AllRates Rates, String Shortname)
        {
            Single Rate = Rates.QuarryRate;
		   		 		  						  	   		   		 		  	 	 		  						  				
            if (!Rates.QuarryDetalis.UseDetalisRateQuarry) return Rate;
            return Rates.QuarryDetalis.ShortnameListQuarry.ContainsKey(Shortname) ? Rates.QuarryDetalis.ShortnameListQuarry[Shortname] : Rate;
        }

        void OnHour()
        {
            Single hour = TOD_Sky.Instance.Cycle.Hour;
            var Sunrise = TOD_Sky.Instance.SunriseTime;
            var Sunset = TOD_Sky.Instance.SunsetTime;
            Int32 dayStart = config.pluginSettings.OtherSetting.DayStart;
            Int32 nightStart = config.pluginSettings.OtherSetting.NightStart;
            
            if (hour > Sunrise && hour < Sunset && hour >= dayStart && !activatedDay)
                OnSunrise();
            else if ((hour > Sunset || hour < Sunrise) && hour >= nightStart && activatedDay)
                OnSunset();
		   		 		  						  	   		   		 		  	 	 		  						  				
            if (config.pluginSettings.OtherSetting.UseSkipTime ||
                !config.pluginSettings.OtherSetting.UseAlertDayNight) return;
            
            if (!sendMessageDay && sendMessageNight && Mathf.Abs(hour - dayStart) <= 0.1f)
            {
                Configuration.PluginSettings.Rates.AllRates Rate = config.pluginSettings.RateSetting.DayRates;
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    SendChat(GetLang("DAY_RATES_ALERT", player.UserIDString, Rate.GatherRate, Rate.LootRate, Rate.PickUpRate, Rate.QuarryRate, Rate.ExcavatorRate, Rate.GrowableRate), player);
           
                sendMessageDay = true;
                sendMessageNight = false;
            }
            else if (!sendMessageNight && sendMessageDay &&  Mathf.Abs(hour - nightStart) <= 0.1f)
            {
                Configuration.PluginSettings.Rates.AllRates Rate = config.pluginSettings.RateSetting.NightRates;
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    SendChat(GetLang("NIGHT_RATES_ALERT", player.UserIDString, Rate.GatherRate, Rate.LootRate, Rate.PickUpRate, Rate.QuarryRate, Rate.ExcavatorRate, Rate.GrowableRate), player);
           
                sendMessageNight = true;
                sendMessageDay = false;
            }
        }

        private void SpawnTank()
        {
            UnSubProSub();
            if (!BradleySpawner.singleton.spawned.isSpawned)
                BradleySpawner.singleton?.SpawnBradley();
        }

        private Single GetBonusRateDayOfWeek(BasePlayer player)
        {
            if (rateDayOfWeek == null)
                return 0;

            if (player == null)
                return rateDayOfWeek.upBonusRate;
            
            foreach (KeyValuePair<string,float> privilageUpRate in rateDayOfWeek.privilageUpRates.OrderByDescending(x => x.Value))
            {
                if (permission.UserHasPermission(player.UserIDString, privilageUpRate.Key))
                    return privilageUpRate.Value;
            }
            
            return rateDayOfWeek.upBonusRate;
        }
        private Boolean sendMessageNight;
        private void OnEntitySpawned(HotAirBalloon hotAirBalloon)
        {
            if (!config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel) return;
            if (hotAirBalloon == null) return;
            hotAirBalloon.fuelPerSec = config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.ConsumedHotAirBalloon;
        }
        
        
        
        private IEnumerator InitializeTransport()
        {
            foreach (BaseNetworkable entity in BaseNetworkable.serverEntities.Where(e => e is BaseVehicle)) 
            {
                if (entity is MotorRowboat)
                    OnEntitySpawned((MotorRowboat)entity);
                if(entity is Snowmobile)
                    OnEntitySpawned((Snowmobile)entity);
                if(entity is HotAirBalloon)
                    OnEntitySpawned((HotAirBalloon)entity);
                if(entity is RHIB)
                    OnEntitySpawned((RHIB)entity);
                if(entity is BaseSubmarine)
                    OnEntitySpawned((BaseSubmarine)entity);
                if(entity is Minicopter)
                    OnEntitySpawned((Minicopter)entity);
                if(entity is ScrapTransportHelicopter)
                    OnEntitySpawned((ScrapTransportHelicopter)entity);
                
                yield return CoroutineEx.waitForSeconds(0.03f); 
            }
        }
        int API_CONVERT(Int32 RateType, string Shortname, float Amount, BasePlayer player = null) => Converted((Types)RateType, Shortname, Amount, player);
        void SpawnPlane()
        {
            UnSubProSub();
        
            var position = new Vector3(ConVar.Server.worldsize, 100, ConVar.Server.worldsize) - new Vector3(50f, 0f, 50f);
            var entity = GameManager.server.CreateEntity(prefabPlane, position);
            entity?.Spawn();
        }
        
        private void StartCargoShip(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (!EventSettings.CargoShipSetting.FullOff && EventSettings.CargoShipSetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.CargoShipSetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.CargoShipSetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.CargoShipSetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.CargoShipSetting.EventSpawnTime;
                timer.Once(TimeSpawn, () =>
                {
                    StartCargoShip(EventSettings);
                    SpawnCargo();
                    
                    if(EventSettings.CargoShipSetting.useGameTip)
                        MessageGameTipsError("ALERT_CARGOSHIP");
                });
            }
        }
       
        
        
        private void OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            if (!recycler.IsOn())
            {
                NextTick(() =>
                {
                    if (!recycler.IsOn())
                        return;

                    Single Speed = GetSpeedRecycler(player);
                    recycler.InvokeRepeating(recycler.RecycleThink, Speed, Speed);
                });
            }
        }
		   		 		  						  	   		   		 		  	 	 		  						  				
        private readonly Dictionary<String, ModiferTea> TeaModifers = new Dictionary<String, ModiferTea>
        {
            ["oretea.advanced"] = new ModiferTea()
            {
                Duration = 1800f,
                Value = 0.35f,
                Type = Modifier.ModifierType.Ore_Yield
            },
            ["oretea"] = new ModiferTea()
            {
                Duration = 1800f,
                Value = 0.2f,
                Type = Modifier.ModifierType.Ore_Yield
            },
            ["oretea.pure"] = new ModiferTea()
            {
                Duration = 1800f,
                Value = 0.5f,
                Type = Modifier.ModifierType.Ore_Yield
            },
            ["woodtea.advanced"] = new ModiferTea()
            {
                Duration = 1800f,
                Value = 1.0f,
                Type = Modifier.ModifierType.Wood_Yield
            },
            ["woodtea"] = new ModiferTea()
            {
                Duration = 1800f,
                Value = 0.5f,
                Type = Modifier.ModifierType.Wood_Yield
            },
            ["woodtea.pure"] = new ModiferTea()
            {
                Duration = 1800f,
                Value = 2.0f,
                Type = Modifier.ModifierType.Wood_Yield
            },
            ["scraptea.advanced"] = new ModiferTea()
            {
                Duration = 2700f,
                Value = 2.25f,
                Type = Modifier.ModifierType.Scrap_Yield
            },
            ["scraptea"] = new ModiferTea()
            {
                Duration = 1800f,
                Value = 1.0f,
                Type = Modifier.ModifierType.Scrap_Yield
            },
            ["scraptea.pure"] = new ModiferTea()
            {
                Duration = 3600f,
                Value = 3.5f,
                Type = Modifier.ModifierType.Scrap_Yield
            },
        };
        
        
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity == null) return;

            if(config.pluginSettings.RateSetting.UseBlackListPrefabs)
                if (config.pluginSettings.RateSetting.BlackListPrefabs.Contains(entity.ShortPrefabName))
                    return;
            
            LootContainer container = entity as LootContainer;

            if (entity.net == null) return;
            UInt64 NetID = entity.net.ID.Value;
            if (LootersListCrateID.Contains(NetID) || LootersSaveListCrateID.Contains(NetID)) return;

            if (container == null)
            {
                if (!(entity is NPCPlayerCorpse)) return;
                
                NPCPlayerCorpse corpse = (NPCPlayerCorpse)entity;
                foreach (ItemContainer corpseContainer in corpse.containers)
                {
                    foreach (Item item in corpseContainer.itemList)
                        item.amount = Converted(Types.Loot, item.info.shortname, item.amount, player);
                }
            }
            else
            {
                foreach (Item item in container.inventory.itemList)
                    item.amount = Converted(Types.Loot, item.info.shortname, item.amount, player);
            }
            
            LootersListCrateID.Add(NetID);
            
            if(entity is SupplyDrop or HackableLockedCrate)
                LootersSaveListCrateID.Add(NetID);
        }

                
        
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity as BasePlayer;
            if (item == null || player == null) return null;
            Int32 Rate = Converted(Types.Gather, item.info.shortname, item.amount, player);
            item.amount = Rate;

            return null;
        }
        private Boolean sendMessageDay;

        
                
        private void MessageGameTipsError(String langKey)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                player.SendConsoleCommand("gametip.showtoast", new object[]{ "1", GetLang(langKey, player.UserIDString) });
        }

        void ReadData()
        {
            DataQuarryPlayer = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, UInt64>>("IQSystem/IQRates/Quarrys");
            LootersSaveListCrateID = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<List<UInt64>>("IQSystem/IQRates/LootersCratedIDs");
        }
        void StartEvent()
        {
            Configuration.PluginSettings.OtherSettings.EventSettings EventSettings = config.pluginSettings.OtherSetting.EventSetting;
            StartCargoShip(EventSettings);
            StartCargoPlane(EventSettings);
            StartBreadley(EventSettings);
            StartChinoock(EventSettings);
            StartHelicopter(EventSettings);
        }
        int API_CONVERT_GATHER(string Shortname, float Amount, BasePlayer player = null) => Converted(Types.Gather, Shortname, Amount, player);
        private void OnEntitySpawned(AttackHelicopter helicopter)
        {
            if (helicopter == null) return;
            FuelSystemRating(helicopter.GetFuelSystem(), config.pluginSettings.OtherSetting.FuelSetting.AmountAttackHelicopter);
		   		 		  						  	   		   		 		  	 	 		  						  				
            if (config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel)
                helicopter.fuelPerSec = config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.ConsumedAttackHelicopter;
        }
        
        
        enum Types
        {
            Gather,
            Loot,
            PickUP,
            Quarry,
            Excavator,
            Growable,
            Fishing
        }

        void OnSunrise()
        {
            timeComponent.DayLengthInMinutes = config.pluginSettings.OtherSetting.DayTime * (24.0f / (TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime));
            activatedDay = true;
            if (!config.pluginSettings.OtherSetting.UseSkipTime) return;
            if (config.pluginSettings.OtherSetting.TypeSkipped == SkipType.Day)
                TOD_Sky.Instance.Cycle.Hour = config.pluginSettings.OtherSetting.NightStart;
        }

        private Int32 GetRandomTime(Int32 Min, Int32 Max) => UnityEngine.Random.Range(Min, Max);
        
        
        private Single GetSpeedRecycler(BasePlayer player)
        {
            Configuration.PluginSettings.Rates.RecyclerController Recycler = config.pluginSettings.RateSetting.RecyclersController;
		   		 		  						  	   		   		 		  	 	 		  						  				
            foreach (Configuration.PluginSettings.Rates.RecyclerController.PresetRecycler presetRecycler in Recycler.PrivilageSpeedRecycler)
            {
                if (permission.UserHasPermission(player.UserIDString, presetRecycler.Permissions))
                    return presetRecycler.SpeedRecyclers;
            }
            
            return Recycler.DefaultSpeedRecycler;
        }
        private object OnExcavatorGather(ExcavatorArm arm, Item item)
        {
            if (arm == null) return null;
            if (item == null) return null;
            item.amount = Converted(Types.Excavator, item.info.shortname, item.amount, ExcavatorPlayer);
            return null;
        }
        
        private object OnOvenStart(BaseOven oven)
        {
            if (config.pluginSettings.RateSetting.IgnoreSpeedBurnablePrefabList.Contains(oven.ShortPrefabName))
                return null;
            
            return OvenController.GetOrAdd(oven).Switch(null);
        }
		   		 		  						  	   		   		 		  	 	 		  						  				
        void WriteData()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQRates/Quarrys", DataQuarryPlayer);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystem/IQRates/LootersCratedIDs", LootersSaveListCrateID);
        }
        
        
        void OnFuelConsume(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven == null) return;
            
            burnable.byproductChance = GetRareCoal(BasePlayer.FindByID(oven.OwnerID));
            if (burnable.byproductChance == 0)
                burnable.byproductChance = -1;
        }
        
                
        void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            foreach(ItemAmount item in collectible.itemList)
                item.amount = Converted(Types.PickUP, item.itemDef.shortname, (Int32)item.amount, player);
        }
        private Coroutine initializeTransport = null;
 
        private Configuration.PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays GetRateBonusForCurrentTime()
        {
            DayOfWeek currentDayOfWeek = DateTime.UtcNow.DayOfWeek;
            Int32 currentHour = DateTime.UtcNow.Hour;
            
            foreach (Configuration.PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays rateBonusDay in config.pluginSettings.RateSetting.rateControllerDayOfWeek.rateBonusDayOfWeek)
            {
                DayOfWeek startDayOfWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), rateBonusDay.timeStartBonus.dayOfWeek);
                Int32 startHour = rateBonusDay.timeStartBonus.timeHours;
                
                DayOfWeek stopDayOfWeek = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), rateBonusDay.timeStopBonus.dayOfWeek);
                Int32 stopHour = rateBonusDay.timeStopBonus.timeHours;
                
                Boolean isWithinSameDay = startDayOfWeek == stopDayOfWeek && currentDayOfWeek == startDayOfWeek && currentHour >= startHour && currentHour <= stopHour;
                Boolean isCrossingMidnight = startDayOfWeek < stopDayOfWeek && ((currentDayOfWeek == startDayOfWeek && currentHour >= startHour) || (currentDayOfWeek == stopDayOfWeek && currentHour <= stopHour) || (currentDayOfWeek > startDayOfWeek && currentDayOfWeek < stopDayOfWeek));
                Boolean isWrappingAroundWeek = startDayOfWeek > stopDayOfWeek && ((currentDayOfWeek == startDayOfWeek && currentHour >= startHour) || (currentDayOfWeek == stopDayOfWeek && currentHour <= stopHour) || (currentDayOfWeek > startDayOfWeek || currentDayOfWeek < stopDayOfWeek));
                
                if (isWithinSameDay || isCrossingMidnight || isWrappingAroundWeek)
                    return rateBonusDay;
            }

            return null;
        }
        private void StartChinoock(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (!EventSettings.ChinoockSetting.FullOff && EventSettings.ChinoockSetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.ChinoockSetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.ChinoockSetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.ChinoockSetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.ChinoockSetting.EventSpawnTime;
                timer.Once(TimeSpawn, () =>
                {
                    StartChinoock(EventSettings);
                    SpawnCH47();
                    
                    if(EventSettings.ChinoockSetting.useGameTip)
                        MessageGameTipsError("ALERT_CHINOOCK");
                });
            }
        }
        
        
        private String IsValidConfigDayOfWeek()
        {
            if (!config.pluginSettings.RateSetting.rateControllerDayOfWeek.useRateBonusDayOfWeek) return String.Empty;
            List<Configuration.PluginSettings.Rates.RateControllerDayOfWeek.RateBonusDays> bonusPeriods = config.pluginSettings.RateSetting.rateControllerDayOfWeek.rateBonusDayOfWeek;

            String periodsError = String.Empty;
            
            for (int i = 0; i < bonusPeriods.Count; i++)
            {
                var period1 = bonusPeriods[i];

                int start1 = GetTotalHoursFromStartOfWeek(period1.timeStartBonus.dayOfWeek,
                    period1.timeStartBonus.timeHours);
                int end1 = GetTotalHoursFromStartOfWeek(period1.timeStopBonus.dayOfWeek,
                    period1.timeStopBonus.timeHours);

                for (int j = i + 1; j < bonusPeriods.Count; j++)
                {
                    var period2 = bonusPeriods[j];
                    int start2 = GetTotalHoursFromStartOfWeek(period2.timeStartBonus.dayOfWeek,
                        period2.timeStartBonus.timeHours);
                    int end2 = GetTotalHoursFromStartOfWeek(period2.timeStopBonus.dayOfWeek,
                        period2.timeStopBonus.timeHours);

                    if (IsOverlapping(start1, end1, start2, end2))
                    {
                        periodsError += LanguageEn
                            ? $"\nIntersections in : Period 1 : {period1.timeStartBonus.dayOfWeek} {period1.timeStartBonus.timeHours}:00 => {period1.timeStopBonus.dayOfWeek} {period1.timeStopBonus.timeHours}:00 vs Period 2 : {period2.timeStartBonus.dayOfWeek} {period2.timeStartBonus.timeHours}:00 => {period2.timeStopBonus.dayOfWeek} {period2.timeStopBonus.timeHours}:00"
                            : $"\nПересечения в : Период 1 : {period1.timeStartBonus.dayOfWeek} {period1.timeStartBonus.timeHours}:00 => {period1.timeStopBonus.dayOfWeek} {period1.timeStopBonus.timeHours}:00 vs Период 2 : {period2.timeStartBonus.dayOfWeek} {period2.timeStartBonus.timeHours}:00 => {period2.timeStopBonus.dayOfWeek} {period2.timeStopBonus.timeHours}:00";
                        return periodsError; 
                    }
                }
            }

            return String.Empty;; 
        }
        protected override void SaveConfig() => Config.WriteObject(config);

        private ModifierDefintion GetDefintionModifer(Modifier.ModifierType Type, Single Duration, Single Value)
        {
            ModifierDefintion def = new ModifierDefintion
            {
                source = Modifier.ModifierSource.Tea,
                type = Type,
                duration = Duration,
                value = Value <= 0 ? 1.0f : Value
            };

            return def;
        }
        private const string prefabPlane = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
		   		 		  						  	   		   		 		  	 	 		  						  				
        private bool IsOverlapping(int start1, int end1, int start2, int end2)
        {
            if (start1 <= end1)
            {
                if (start2 <= end2) 
                    return start1 < end2 && start2 < end1;
                return start1 < end2 || start2 < end1;
            }
            if (start2 <= end2) 
                return start2 < end1 || start1 < end2;
            return true;
        }
        void OnExcavatorResourceSet(ExcavatorArm arm, string resourceName, BasePlayer player)
        {
            if (arm == null || player == null) return;
            ExcavatorPlayer = player;
        }
        
                
        private Single GetSpeeedMixingTable(BasePlayer player)
        {
            foreach (Configuration.PluginSettings.Rates.SpeedMixingTable speedMixing in config.pluginSettings.RateSetting.speedMixingTables.OrderByDescending(x => x.SpeedMixing))
            {
                if (permission.UserHasPermission(player.UserIDString, speedMixing.Permissions))
                    return 1.0f - (speedMixing.SpeedMixing / 100.0f);
            }

            return 1;
        }

        void API_BONUS_RATE_ADDPLAYER(UInt64 userID, Single Rate)
        {
            BasePlayer player = BasePlayer.FindByID(userID);
            API_BONUS_RATE_ADDPLAYER(player, Rate);
        }
        
        public void Register(string Permissions)
        {
            if (!String.IsNullOrWhiteSpace(Permissions))
                if (!permission.PermissionExists(Permissions, this))
                    permission.RegisterPermission(Permissions, this);
        }
        private Boolean activatedDay;
        bool IsBlackListBurnable(string Shortname)
        {
            var BlackList = config.pluginSettings.RateSetting.BlackListBurnable;
            if (BlackList.Contains(Shortname))
                return true;
            else return false;
        }

        private int GetTotalHoursFromStartOfWeek(string dayOfWeek, int hour)
        {
            DayOfWeek day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayOfWeek);
            int dayOffset = (int)day;
            return dayOffset * 24 + hour;
        }
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ALERT_HELICOPTER"] = "PATROL HELICOPTER INBOUND",
                ["ALERT_CARGOPLANE"] = "AIRDROP INBOUND",
                ["ALERT_CHINOOCK"] = "CHINOOCK INBOUND",
                ["ALERT_BRADLEY"] = "BRADLEY INBOUND LAUNCH SITE",
                ["ALERT_CARGOSHIP"] = "CARGO SHIP INBOUND",
                
                ["MY_RATES_INFO"] = "Your resource rating at the moment :" +
                "\n- Rating of extracted resources: <color=#eb4034>x{0}</color>" +
                "\n- Rating of found items: <color=#eb4034>х{1}</color>" +
                "\n- Rating of raised items: <color=#eb4034>х{2}</color>" +
                "\n- Career rankings: <color=#eb4034>x{3}</color>" +
                "\n- Excavator Rating: <color=#eb4034>x{4}</color>" +
                "\n- Rating of growable : <color=#eb4034>x{5}</color>",

                ["DAY_RATES_ALERT"] = "The day has come!" +
                "\nThe global rating on the server has been changed :" +
                "\n- Rating of extracted resources: <color=#eb4034>x{0}</color>" +
                "\n- Rating of found items: <color=#eb4034>х{1}</color>" +
                "\n- Rating of raised items: <color=#eb4034>х{2}</color>" +
                "\n- Career rankings: <color=#eb4034>x{3}</color>" +
                "\n- Excavator Rating: <color=#eb4034>x{4}</color>" +
                "\n- Rating of growable : <color=#eb4034>x{5}</color>",

                ["NIGHT_RATES_ALERT"] = "Night came!" +
                "\nThe global rating on the server has been changed :" +
                "\n- Rating of extracted resources: <color=#eb4034>x{0}</color>" +
                "\n- Rating of found items: <color=#eb4034>х{1}</color>" +
                "\n- Rating of raised items: <color=#eb4034>х{2}</color>" +
                "\n- Career rankings: <color=#eb4034>x{3}</color>" +
                "\n- Excavator Rating: <color=#eb4034>x{4}</color>" +
                "\n- Rating of growable : <color=#eb4034>x{5}</color>",
                
                ["RATE_BONUS_DAY_OF_WEEK"] = "Attention, survivors!" +
                                             "\nThe increased rating period has begun!" +
                                             "\nA coefficient of <color=#eb4034>x{0}</color> has been added to all your ratings" +
                                             "\nIt will be active from <color=#eb4034>{1} {2}</color> until <color=#eb4034>{3} {4}</color>\n\nHurry up and take advantage of the bonuses!",

                
                ["RATE_BONUS_DAY_OF_WEEK_END"] = "Attention! The increased rating period has ended.\nThank you for participating!\nStay tuned for new bonuses and continue your adventures!",
                
                ["MONDAY"] = "monday",
                ["TUESDAY"] = "tuesday",
                ["WEDNESDAY"] = "wednesday",
                ["THURSDAY"] = "thursday",
                ["FRIDAY"] = "friday",
                ["SATURDAY"] = "saturday",
                ["SUNDAY"] = "sunday",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ALERT_HELICOPTER"] = "ПАТРУЛЬНЫЙ ВЕРТОЛЕТ ПРИБЫЛ",
                ["ALERT_CARGOPLANE"] = "АЙРДРОП ПРИБЫЛ",
                ["ALERT_CHINOOCK"] = "ЧИНУК ПРИБЫЛ",
                ["ALERT_BRADLEY"] = "ТАНК ПРИБЫЛ НА КОСМОДРОМ",
                ["ALERT_CARGOSHIP"] = "КОРАБЛЬ ПРИБЫЛ В МОРЕ",
                
                ["MY_RATES_INFO"] = "Ваш рейтинг ресурсов на данный момент :" +
                "\n- Рейтинг добываемых ресурсов: <color=#eb4034>x{0}</color>" +
                "\n- Рейтинг найденных предметов: <color=#eb4034>х{1}</color>" +
                "\n- Рейтинг поднимаемых предметов: <color=#eb4034>х{2}</color>" +
                "\n- Рейтинг карьеров: <color=#eb4034>x{3}</color>" +
                "\n- Рейтинг экскаватора: <color=#eb4034>x{4}</color>" +
                "\n- Рейтинг грядок : <color=#eb4034>x{5}</color>",

                ["DAY_RATES_ALERT"] = "Наступил день!" +
                "\nГлобальный рейтинг на сервере был изменен :" +
                "\n- Рейтинг добываемых ресурсов: <color=#eb4034>x{0}</color>" +
                "\n- Рейтинг найденных предметов: <color=#eb4034>х{1}</color>" +
                "\n- Рейтинг поднимаемых предметов: <color=#eb4034>х{2}</color>" +
                "\n- Рейтинг карьеров: <color=#eb4034>x{3}</color>" +
                "\n- Рейтинг экскаватора: <color=#eb4034>x{4}</color>" +
                "\n- Рейтинг грядок : <color=#eb4034>x{5}</color>", 
                
                ["NIGHT_RATES_ALERT"] = "Наступила ночь!" +
                "\nГлобальный рейтинг на сервере был изменен :" +
                "\n- Рейтинг добываемых ресурсов: <color=#eb4034>x{0}</color>" +
                "\n- Рейтинг найденных предметов: <color=#eb4034>х{1}</color>" +
                "\n- Рейтинг поднимаемых предметов: <color=#eb4034>х{2}</color>" +
                "\n- Рейтинг карьеров: <color=#eb4034>x{3}</color>" +
                "\n- Рейтинг экскаватора: <color=#eb4034>x{4}</color>" +
                "\n- Рейтинг грядок : <color=#eb4034>x{5}</color>",
                
                ["RATE_BONUS_DAY_OF_WEEK"] = "Внимание, выжившие!" + 
                                             "\nНаступил период увеличенного рейтинга!" + 
                                             "\nКо всем <color=#eb4034>вашим</color> рейтингам прибавлен коэффициент <color=#eb4034>x{0}</color>" + 
                                             "\nОн будет действовать с <color=#eb4034>{1} {2}</color>, до <color=#eb4034>{3} {4}</color>\n\nПоспешите воспользоваться бонусами!",
                
                ["RATE_BONUS_DAY_OF_WEEK_END"] =  "Внимание! Период увеличенного рейтинга подошел к концу.\nСпасибо за участие!\nСледите за новыми бонусами и продолжайте свои приключения!",
                
                ["MONDAY"] = "понедельника",
                ["TUESDAY"] = "вторника",
                ["WEDNESDAY"] = "средаы",
                ["THURSDAY"] = "четверга",
                ["FRIDAY"] = "пятницы",
                ["SATURDAY"] = "субботы",
                ["SUNDAY"] = "воскресенья",
            }, this, "ru");
        }
        
                
        private Object OnEventTrigger(TriggeredEventPrefab info)
        {
            switch (info.name)
            {
                case "assets/bundled/prefabs/world/event_cargoheli.prefab":
                {
                    Configuration.PluginSettings.OtherSettings.EventSettings.Setting EventTimer = config.pluginSettings.OtherSetting.EventSetting.ChinoockSetting;
                    if (EventTimer.FullOff || EventTimer.UseEventCustom)
                        return true;
                    break;
                }
                case "assets/bundled/prefabs/world/event_helicopter.prefab":
                {
                    Configuration.PluginSettings.OtherSettings.EventSettings.Setting EventTimer = config.pluginSettings.OtherSetting.EventSetting.HelicopterSetting;
                    if (EventTimer.FullOff || EventTimer.UseEventCustom)
                        return true;
                    break;
                }
                case "assets/bundled/prefabs/world/event_cargoship.prefab":
                {
                    Configuration.PluginSettings.OtherSettings.EventSettings.Setting EventTimer = config.pluginSettings.OtherSetting.EventSetting.CargoShipSetting;
                    if (EventTimer.FullOff || EventTimer.UseEventCustom)
                        return true;
                    break;
                }
                case "assets/bundled/prefabs/world/event_airdrop.prefab":
                {
                    Configuration.PluginSettings.OtherSettings.EventSettings.Setting EventTimer = config.pluginSettings.OtherSetting.EventSetting.CargoPlaneSetting;
                    if (EventTimer.FullOff || EventTimer.UseEventCustom)
                        return true;
                    break;
                }
            }
            return null;
        }

                
                
        public Single GetMultiplaceBurnableSpeed(String ownerid)
        {
            Single Multiplace = config.pluginSettings.RateSetting.SpeedBurnable;
            if (config.pluginSettings.RateSetting.UseSpeedBurnableList)
            {
                var SpeedInList = config.pluginSettings.RateSetting.SpeedBurableList.OrderByDescending(z => z.SpeedBurnable).FirstOrDefault(x => permission.UserHasPermission(ownerid, x.Permissions));
                if (SpeedInList != null)
                    Multiplace = SpeedInList.SpeedBurnable;
            }
            return Multiplace;
        }

        
                private void Unload()
        {
            WriteData();    
            
            OvenController.KillAll();
            if (timeComponent != null)
            {
                timeComponent.OnSunrise -= OnSunrise;
                timeComponent.OnSunset -= OnSunset;
                timeComponent.OnHour -= OnHour;
                timeComponent.ProgressTime = true;
            }
            
            if (initializeTransport != null)
            {
                ServerMgr.Instance.StopCoroutine(initializeTransport);
                initializeTransport = null;
            }

            if (rateDayOfWeek != null)
                rateDayOfWeek = null;
        }
        
        
        public Dictionary<BasePlayer, Single> BonusRates = new Dictionary<BasePlayer, Single>();

        
        private void OnEntitySpawned(RHIB boat)
        {
            if (boat == null) return;
            FuelSystemRating(boat.GetFuelSystem(), config.pluginSettings.OtherSetting.FuelSetting.AmountBoat);

            if (config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.useConsumedFuel)
                boat.fuelPerSec = config.pluginSettings.OtherSetting.FuelConsumedTransportSetting.ConsumedBoat;
        }    
        void OnSwitchToggled(IOEntity entity, BasePlayer player)
        {
            foreach (IOEntity.IOSlot outputSlot in entity.outputs)
            {
                if (outputSlot.connectedTo == null) continue;
                if (outputSlot.connectedTo.ioEnt == null) continue;
                BaseEntity entityConnected = outputSlot.connectedTo.ioEnt.GetEntity();
                if (entityConnected == null) continue;
                BaseEntity parentIoEntity = entityConnected.GetParentEntity();
                if (parentIoEntity == null) continue;
                if (parentIoEntity is not BaseOven oven) continue;
                OvenController.GetOrAdd(oven).Switch(player); 
            }
        }
        public static IQRates _;
        private const string prefabShip = "assets/content/vehicles/boats/cargoship/cargoshiptest.prefab";
            }
}
