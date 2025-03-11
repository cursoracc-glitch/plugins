using Oxide.Core;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
		   		 		  						  	   		  	  			  	  			  	   		  		  
namespace Oxide.Plugins
{
    [Info("IQBreakingTools", "Mercury", "0.0.9")]
    [Description("Что этот Mercury себе позволяет,он уже заебал клепать хуйню")]
    class IQBreakingTools : RustPlugin
    {
        private const String IQTools = "iqbreakingtools.tools";
        void OnLoseCondition(Item item, ref float amount)
        {
            if (item == null) return;
            if (config.BlackListShortname.Contains(item.info.shortname)) return;
            if (config.BlackList.Contains(item.skin)) return;
		   		 		  						  	   		  	  			  	  			  	   		  		  
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null) return;
            if (!player.UserIDString.IsSteamId()) return;
            if (config.RaidBlockBreaking && IsRaidBlocked(player)) return;
            if (config.StartLoseNoOwner && player.IsBuildingBlocked()) return;

            ItemCategory ItemCategory = item.info.category; //ItemManager.FindItemDefinition(item.info.itemid).category;

            if (ItemCategory == ItemCategory.Weapon && permission.UserHasPermission(player.UserIDString, IQWeapon)
                || ItemCategory == ItemCategory.Attire && permission.UserHasPermission(player.UserIDString, IQAttire)
                || ItemCategory == ItemCategory.Tool && permission.UserHasPermission(player.UserIDString, IQTools))
            {
                if (!config.BreakingProcesses.useProcessBreaking)
                    amount = 0;
                else amount -= amount / config.BreakingProcesses.ProcessBreakingAmount;
            }
            else if (permission.UserHasPermission(player.UserIDString, IQBreakingToolsPermission))
                if (config.ToolsList.Contains(item.info.shortname))
                {
                    if (!config.BreakingProcesses.useProcessBreaking)
                        amount = 0;
                    else amount -= amount / config.BreakingProcesses.ProcessBreakingAmount;
                }
        }
        private const String IQWeapon = "iqbreakingtools.weapon";

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
                PrintWarning("Ошибка #2144963" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        void RegisteredPermissions()
        {         
            permission.RegisterPermission(IQBreakingToolsPermission, this);
            permission.RegisterPermission(IQTools, this);
            permission.RegisterPermission(IQWeapon, this);
            permission.RegisterPermission(IQAttire, this);
            PrintWarning("Permissions - completed");
        }

            
        
        private static Configuration config = new Configuration();
        private const String IQAttire = "iqbreakingtools.attire";

        
                private void OnServerInitialized()
        {
            RegisteredPermissions();
        }
        
                
        
        private const String IQBreakingToolsPermission = "iqbreakingtools.use";
        protected override void SaveConfig() => Config.WriteObject(config);
        private class Configuration
        {
            [JsonProperty("Список предметов,которые не будут ломаться или на них будет замедленная поломка (shortname)")]
            public List<String> ToolsList = new List<String>();
            internal class BreakingProcess
            {
                [JsonProperty("На сколько срезать поломку (Пример : в 3 раза)")]
                public Single ProcessBreakingAmount;
                [JsonProperty("Включить замедленную поломку(неломайка заменится на замедленную поломку)")]
                public Boolean useProcessBreaking;
            }
            [JsonProperty("Отключать неломайку если у игрока рейдблок")]
            public Boolean RaidBlockBreaking;
            [JsonProperty("Настройка замедленной поломки")]
            public BreakingProcess BreakingProcesses = new BreakingProcess();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    StartLoseNoOwner = false,
                    RaidBlockBreaking = false,
                    BlackListShortname = new List<String>()
                    {
                        "jackhammer",
                        "chainsaw"
                    },
                    ToolsList = new List<String>
                    {
                        "rifle.ak",
                        "jackhammer",
                        "hatchet"
                    },
                    BlackList = new List<UInt64>
                    {
                        1337228,
                        2281337
                    },
                    BreakingProcesses = new BreakingProcess
                    {
                        useProcessBreaking = false,
                        ProcessBreakingAmount = 3,
                    }
                };
            }
            [JsonProperty("Список исключенных SkinID(Вещи с этим SkinID будут ломаться и на них не будет действовать замедленная поломка! Для кастомных предметов)")]
            public List<UInt64> BlackList = new List<UInt64>();
            [JsonProperty("Отключать неломайку если игрок атакует постройки в чужой билде(не авторизованный в шкафу)")]
            public Boolean StartLoseNoOwner;
            [JsonProperty("Список предметов исключений,которые БУДУТ ломаться [исключения в случае если вы выдавали права на категорию, но нужно исключить определенный предмет] (shortname)")]
            public List<String> BlackListShortname = new List<String>();
        }
        /// <summary>
        /// Обновление 0.0.x
        /// - Добавлен список исключений для предметов, которые будут категорично ломаться (даже если выданы права на категорию)
        /// - Корректировка метода с замедленной поломкой
        /// </summary>

        
                public Boolean IsRaidBlocked(BasePlayer player)
        {
            String ret = Interface.Call("CanTeleport", player) as String;
            if (ret != null)
                return true;
            else return false;
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
            }
}
