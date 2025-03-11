using Oxide.Core.Plugins;
using Oxide.Core;
using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Time System", "Nimant", "1.0.1")]    
    public class TimeSystem : RustPlugin
    {
		
		#region Variables
		
		private bool Initialized;
		private int componentSearchAttempts = 0;
		private TOD_Time timeComponent = null;
		private bool activatedDay;							
		
		#endregion
		
		#region Hooks

		private void Init()
		{
			LoadVariables();
			Initialized = false;
		}

		private void Unload()
		{
			if (timeComponent == null || !Initialized) return;									
			timeComponent.OnSunrise -= OnSunrise;
            timeComponent.OnSunset -= OnSunset;			
			timeComponent.OnHour -= OnHour;
		}

		private void OnServerInitialized()
		{
			if (TOD_Sky.Instance == null)
            {
				componentSearchAttempts++;
                if (componentSearchAttempts < 100)
                    timer.Once(1f, OnServerInitialized);
                else
                    PrintWarning("Не найден нужный компонент времени, плагин не активен!");
                return;
            }
            timeComponent = TOD_Sky.Instance.Components.Time;
            if (timeComponent == null)
            {
                PrintWarning("Невозможно извлечь компонент времени, плагин не активен!");
                return;
            }			
			SetTimeComponent();			
		}
		
		private void OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.cmd == null || string.IsNullOrEmpty(arg.cmd.FullName)) return;
			var command = arg.cmd.FullName;						
			if (!command.ToLower().Contains("env.")) return;						
			var player = arg.Player();
			if (player != null && !player.IsAdmin) return;
									
			timer.Once(0.5f, OnHour);
		}	

		#endregion
		
		#region Main
		
        private void SetTimeComponent()
        {
            timeComponent.ProgressTime = true;
            timeComponent.UseTimeCurve = false;									
            timeComponent.OnSunrise += OnSunrise;
			timeComponent.OnSunset += OnSunset;			
			timeComponent.OnHour += OnHour;
			Initialized = true;
			
            if (TOD_Sky.Instance.Cycle.Hour >= configData.DayStart && TOD_Sky.Instance.Cycle.Hour < configData.NightStart)
			{
				activatedDay = false;		
                OnSunrise();
			}	
            else
			{	
				activatedDay = true;
                OnSunset();					
			}	
        }				        						
		
        private void OnHour()
        {
			if (!Initialized) return;																		
			if (TOD_Sky.Instance.Cycle.Hour >= configData.DayStart && TOD_Sky.Instance.Cycle.Hour < configData.NightStart && !activatedDay)
			{				
				OnSunrise();				
				return;
			}
			if ((TOD_Sky.Instance.Cycle.Hour >= configData.NightStart || TOD_Sky.Instance.Cycle.Hour < configData.DayStart) && activatedDay)
			{			
				OnSunset();
				return;
			}
		}

        private void OnSunrise()
        {
			if (!Initialized) return;						
			if (!(TOD_Sky.Instance.Cycle.Hour >= configData.DayStart && TOD_Sky.Instance.Cycle.Hour < configData.NightStart && !activatedDay)) return;						
			
			timeComponent.DayLengthInMinutes = configData.DayLength * (24.0f / (configData.NightStart - configData.DayStart));
			if (!activatedDay)
				Interface.CallHook("OnTimeSunrise");
			activatedDay = true;
        }

        private void OnSunset()
        {
			if (!Initialized) return;						
			if (!((TOD_Sky.Instance.Cycle.Hour >= configData.NightStart || TOD_Sky.Instance.Cycle.Hour < configData.DayStart) && activatedDay)) return;						
			
			timeComponent.DayLengthInMinutes = configData.NightLength * (24.0f / (24.0f - (configData.NightStart - configData.DayStart)));
			if (activatedDay)
				Interface.CallHook("OnTimeSunset");
			activatedDay = false;
        }        
		
		#endregion
		
		#region Config        				
		
        private ConfigData configData;
		
        private class ConfigData
        {            						
			[JsonProperty(PropertyName = "Час когда начинается день (восход)")]
			public int DayStart;
			[JsonProperty(PropertyName = "Час когда начинается ночь (закат)")]
			public int NightStart;
			[JsonProperty(PropertyName = "Длительность дня (в минутах)")]
			public int DayLength;
			[JsonProperty(PropertyName = "Длительность ночи (в минутах)")]
			public int NightLength;
        }
		
        private void LoadVariables() => configData = Config.ReadObject<ConfigData>();        
		
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
				DayStart = 8,
				NightStart = 19,
                DayLength = 30,
				NightLength = 15
            };
            SaveConfig(config);
			timer.Once(0.1f, ()=>SaveConfig(config));
        }        
		
        private void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
		
        #endregion

    }
}