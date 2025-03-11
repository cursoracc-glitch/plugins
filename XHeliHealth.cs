using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("XHeliHealth", "Sempai#3239", "1.0.0")]
    internal class XHeliHealth : RustPlugin
    {
		
		private void Bradley(BradleyAPC apc)
		{
			if (apc == null) return;
			
			apc.InitializeHealth(config.Bradley.APCHealth, config.Bradley.APCHealth);
		}
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
		
		private void OnEntitySpawned(BaseEntity entity)
		{
			if (entity == null || entity.net == null) return;
			
			if (entity is BaseHelicopter)
			{
				if (config.Heli.HeliHealth) Heli(entity as BaseHelicopter);
			}
			
			if (entity is BradleyAPC)
			{
				if (config.Bradley.BradleyHealth) Bradley(entity as BradleyAPC);
			}
		}
		   		 		  						  	   		  	 	 		  						  		 			  	  	
        private class HealthConfig
        {				
            
			[JsonProperty("Настройка кастомного ХП вертолета")]
            public HeliSetting Heli = new HeliSetting();			

			public static HealthConfig GetNewConfiguration()
            {
                return new HealthConfig
                {
					Heli = new HeliSetting
					{
						HousingHealth = 10000,
						BladeHealth = 900,
						TailHealth = 500,
						HeliHealth = true
					},
					Bradley = new BradleySetting
					{
						APCHealth = 1000,
						BradleyHealth = true
					}
				};
			}			
			internal class HeliSetting
            {
				[JsonProperty("ХП переднего винта")]
                public int BladeHealth;				
				[JsonProperty("ХП заднего винта")]
                public int TailHealth;				
				[JsonProperty("Включить кастомное ХП вертолета")]
                public bool HeliHealth;						
                [JsonProperty("ХП корпуса вертолета")]
                public int HousingHealth;
            }			
			[JsonProperty("Настройка кастомного ХП танка")]
            public BradleySetting Bradley = new BradleySetting();		
			
			internal class BradleySetting
            {
				[JsonProperty("ХП танка")]
                public int APCHealth;				
				[JsonProperty("Включить кастомное ХП танка")]
                public bool BradleyHealth;
            } 			
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
		   		 		  						  	   		  	 	 		  						  		 			  	  	
            config = Config.ReadObject<HealthConfig>();
        }
		
		private void Heli(BaseHelicopter heli)
		{
			if (heli == null) return;
			
			heli.InitializeHealth(config.Heli.HousingHealth, config.Heli.HousingHealth);
			
			heli.weakspots[0].health = config.Heli.BladeHealth;
			heli.weakspots[1].health = config.Heli.TailHealth;
		}

        		
				
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Sempai\n" +
			"     VK - vk.com/rustnastroika\n" +
			"     Discord - Sempai#3239\n" +
			"     Config - v.3345\n" +
			"-----------------------------");
		}

        protected override void LoadDefaultConfig()
        {
            config = HealthConfig.GetNewConfiguration();

            PrintWarning("Создание начальной конфигурации плагина!!!");
        }
		
        private HealthConfig config;
		
			}
}
