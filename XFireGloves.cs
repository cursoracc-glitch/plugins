using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("XFireGloves", "Monster", "1.0.16")]
	class XFireGloves : RustPlugin
	{
		   		 		  						  	   		  	 	 		  						  	   		  	   
        
        		
		[ConsoleCommand("gl_give")]
        private void GlovesGive(ConsoleSystem.Arg args)
        {
			if(args.Player() != null || args.Args == null || args.Args.Length < 2) return;
			
			BasePlayer player = BasePlayer.FindByID(ulong.Parse(args.Args[0]));
			
			if(player == null) return;
			
			ulong skinID;
			ulong.TryParse(args.Args[1], out skinID);
			
			if(config.Gloves.ContainsKey(skinID))
			{
				var glove = config.Gloves[skinID];
				
				Item item = ItemManager.CreateByName("burlap.gloves", 1, skinID);
				item.name = glove.NameGloves;
			
				if(glove.Remove) 
					item.busyTime = -glove.UseValue;
			
				player.GiveItem(item);
			}
        }
		protected override void LoadDefaultConfig() => config = GlovesConfig.GetNewConfiguration();
		
		private bool RemoveGloves(BasePlayer player, Item gloves)
		{
			gloves.busyTime += 1;
			
			if(gloves.busyTime >= 0)
			{
				Effect x = new Effect("assets/bundled/prefabs/fx/impacts/slash/cloth/cloth1.prefab", player, 0, new Vector3(), new Vector3());
				
                gloves.RemoveFromWorld(); 
				gloves.RemoveFromContainer();
				
				EffectNetwork.Send(x, player.Connection);
		   		 		  						  	   		  	 	 		  						  	   		  	   
                return true;				
		    }
			
			return false;
		}
		
				
				
		private void OnLootEntity(BasePlayer player, BaseEntity entity)
		{
			if(config.Setting.Permission && !permission.UserHasPermission(player.UserIDString, "xfiregloves.glovesloot")) return;
			
			if(!(entity is LootContainer) || player == null || entity.OwnerID != 0) return;
			
		    var inventory = entity.GetComponent<LootContainer>().inventory;
			var lootcontainer = entity.GetComponent<LootContainer>();
			
			foreach(var glove in config.Gloves)
				foreach(var crate in glove.Value.Crate)
					if(crate.Key == lootcontainer.ShortPrefabName)
						if(UnityEngine.Random.Range(0, 100) <= crate.Value)
						{
							Item item = ItemManager.CreateByName("burlap.gloves", 1, glove.Key);
                            item.name = glove.Value.NameGloves;
							
							if(glove.Value.Remove) 
								item.busyTime = -glove.Value.UseValue;
							
                            item.MoveToContainer(inventory);
						}
			
			entity.OwnerID = player.userID;
		}
		
				
				
		private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
			var player = entity.ToPlayer();
			
			if(player == null) return;
			
			foreach(var attire in player.inventory.containerWear.itemList.ToList())
				if(config.Gloves.ContainsKey(attire.skin))
				{
					var glove = config.Gloves[attire.skin];
						
					if(glove.GatherGloves) item.amount = (int)(item.amount * glove.GatherValue);
						
					if(item.info.shortname == "wood" && glove.SmeltWood == false) continue;
					else if(Smelting.ContainsKey(item.info) && glove.SmeltGather) item.info = Smelting[item.info];
				}
		}
		   		 		  						  	   		  	 	 		  						  	   		  	   
        		
				
		private void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     \n" +
			"-----------------------------");
			
			permission.RegisterPermission("xfiregloves.glovesloot", this);
			
            Smelt();
        }
		
		private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
		{
			if(player == null) return;
			
			foreach(var attire in player.inventory.containerWear.itemList.ToList())
				if(config.Gloves.ContainsKey(attire.skin))
				{
					var glove = config.Gloves[attire.skin];
						
					foreach(ItemAmount item in collectible.itemList)
					{
						if(glove.PickupGloves) item.amount = (int)(item.amount * glove.PickupValue);
						if(glove.RadiationPickup) player.metabolism.radiation_poison.value += glove.RadiationPickupValue;
						
						if(item.itemDef.shortname == "wood" && glove.SmeltWood == false) continue;
						else if(Smelting.ContainsKey(item.itemDef) && glove.SmeltPickup) item.itemDef = Smelting[item.itemDef];
						
						if(glove.Remove) 
							if(RemoveGloves(player, attire)) break;
					}
				}
		}
		
				
				
		private object OnItemRecycle(Item item, Recycler recycler)
        {
			if(item != null && config.Gloves.ContainsKey(item.skin))
			{
				var glove = config.Gloves[item.skin];
				
				if(item.info.shortname.Equals("burlap.gloves") && item.skin.Equals(item.skin))
				{
					foreach(var items in glove.ItemList)
						if(UnityEngine.Random.Range(0, 100) <= items.ChanceDrop)
						{
							Item itemc = ItemManager.CreateByName(items.Shortname, UnityEngine.Random.Range(items.Min, items.Max), items.SkinID);
							itemc.name = items.Name;
							itemc.text = items.Text;
							itemc.busyTime = -items.UseValue;
							
							recycler.MoveItemToOutput(itemc);
						}
				
                    item.RemoveFromWorld(); 
				    item.RemoveFromContainer();
				
                    return false;
				}
			}
			
			return null;
        }

        private class GlovesConfig
        {					
			 
			public static GlovesConfig GetNewConfiguration()
            {
                return new GlovesConfig
                {
					Setting = new SettingSetting
					{
						Permission = true
					},
					Gloves = new Dictionary<ulong, GlovesSetting>
					{
						[1742796979] = new GlovesSetting("Огненные перчатки", 1.0f, 1.0f, 2.0f, 1.0f, 25, new List<ItemSetting> { new ItemSetting("wood", 0, "", 50, "", 0, 50, 100) }, false, true, true, true, true, true, true, true, true, true, new Dictionary<string, float>{ ["crate_tools"] = 50.0f, ["crate_normal_2"] = 50.0f }),
						[841106268] = new GlovesSetting("Огненные перчатки", 1.0f, 1.0f, 2.0f, 1.0f, 25, new List<ItemSetting> { new ItemSetting("pistol.revolver", 0, "", 50, "", 0, 1, 1) }, false, false, true, true, true, true, true, true, true, true, new Dictionary<string, float>{ ["crate_tools"] = 50.0f, ["crate_normal_2"] = 50.0f })
					}
				};
			}
			
			internal class GlovesSetting
			{
				[JsonProperty("Имя огненных перчаток")] public string NameGloves;
				[JsonProperty("Рейты добываемых ресурсов в перчатках")] public float GatherValue;                
				[JsonProperty("Рейты подбираемых ресурсов в перчатках")] public float PickupValue;
				[JsonProperty("Кол-во радиации при подборе ресурсов")] public float RadiationPickupValue;					
				[JsonProperty("Кол-во радиации при бонусной добыче")] public float RadiationBonusValue;				
				[JsonProperty("Кол-во юзов")] public int UseValue;
				[JsonProperty("Список кастомных предметов после переработки")] public List<ItemSetting> ItemList;
				
				[JsonProperty("Включить удаление перчаток после N юзов.")] public bool Remove;				
				[JsonProperty("Включить переплавку дерева")] public bool SmeltWood;		
			    [JsonProperty("Включить переплавку добываемых ресурсов")] public bool SmeltGather;			
			    [JsonProperty("Включить переплавку подбираемых ресурсов")] public bool SmeltPickup;			    
				[JsonProperty("Включить рейты добываемых ресурсов")] public bool GatherGloves;			    
				[JsonProperty("Включить рейты подбираемых ресурсов")] public bool PickupGloves;				    
				[JsonProperty("Включить кастомные предметы после переработке огненных перчаток")] public bool RecyclerGloves;					
				[JsonProperty("Включить выпадение перчаток из ящиков с определенным шансом")] public bool CrateGloves;				
				[JsonProperty("Включить накопление радиации при подборе ресурсов.")] public bool RadiationPickup;				
				[JsonProperty("Включить накопление радиации при бонусной добыче.")] public bool RadiationBonus;
				
				[JsonProperty("Настройка шанса выпадения из ящиков и бочек. Имя ящика/бочки | Шанс выпадения: 100.0 - 100%")] public Dictionary<string, float> Crate;	
				
				public GlovesSetting(string n, float gv, float pv, float rpv, float rbv, int uv, List<ItemSetting> il, bool rr, bool sw, bool sg, bool sp, bool gg, bool pg, bool rg, bool cg, bool rp, bool rb, Dictionary<string, float> c)
				{
					NameGloves = n; GatherValue = gv; PickupValue = pv; RadiationPickupValue = rpv; RadiationBonusValue = rbv; UseValue = uv; ItemList = il; Remove = rr; SmeltWood = sw; SmeltGather = sg; SmeltPickup = sp; GatherGloves = gg; PickupGloves = pg; RecyclerGloves = rg; CrateGloves = cg; RadiationPickup = rp; RadiationBonus = rb; Crate = c;
				}
			}
            
			[JsonProperty("Общее")]
            public SettingSetting Setting = new SettingSetting();			
			[JsonProperty("Список огненных перчаток")] 
            public Dictionary<ulong, GlovesSetting> Gloves = new Dictionary<ulong, GlovesSetting>();				 
			
			internal class ItemSetting
			{
				[JsonProperty("Шортнейм предмета")] public string Shortname;
                [JsonProperty("Скин предмета")] public ulong SkinID;  				
                [JsonProperty("Имя предмета")] public string Name;
				[JsonProperty("Шанс выпадения [ 100.0 - 100% ]")] public int ChanceDrop;
				[JsonProperty("Текст [ Если это записка ]")] public string Text;
				[JsonProperty("Кол-во юзов [ Если это перчатки ]")] public int UseValue;
				[JsonProperty("Минимальное количество")] public int Min;				
				[JsonProperty("Максимальное количество")] public int Max;
				
				public ItemSetting(string shortname, ulong skinid, string name, int chancedrop, string text, int usevalue, int min, int max)
				{
					Shortname = shortname; SkinID = skinid; Name = name; ChanceDrop = chancedrop; Text = text; UseValue = usevalue; Min = min; Max = max;
				}
			}
			internal class SettingSetting
			{
				[JsonProperty("Использовать разрешение на шанс найти перчатки в ящиках: xfiregloves.glovesloot")] public bool Permission;
			}
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<GlovesConfig>(); 
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
        
        private GlovesConfig config;
		
		private void Smelt()
		{
			Smelting = ItemManager.GetItemDefinitions().Where(i => SmeltingItemList.Contains(i.shortname)).ToDictionary(i => i, i => i.GetComponent<ItemModCookable>()?.becomeOnCooked);
            Smelting.Add(ItemManager.FindItemDefinition("wood"), ItemManager.FindItemDefinition("charcoal"));
		}
        protected override void SaveConfig() => Config.WriteObject(config);
        public List<string> SmeltingItemList = new List<string>
        {
            "chicken.raw",
            "humanmeat.raw",
            "bearmeat",
            "deermeat.raw",
            "meat.boar",
            "wolfmeat.raw",
			"horsemeat.raw",
            "hq.metal.ore",
            "metal.ore",
            "sulfur.ore",
			//"fr3365"
        };
		
        private Dictionary<ItemDefinition, ItemDefinition> Smelting;
		
		private void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
		{
			if(player == null) return;
			
			foreach(var attire in player.inventory.containerWear.itemList.ToList())
				if(config.Gloves.ContainsKey(attire.skin))
				{
					var glove = config.Gloves[attire.skin];
						
					if(glove.GatherGloves) item.amount = (int)(item.amount * glove.GatherValue);
					if(glove.RadiationBonus) player.metabolism.radiation_poison.value += glove.RadiationBonusValue;
						
					if(item.info.shortname == "wood" && glove.SmeltWood == false) continue;
					else if(Smelting.ContainsKey(item.info) && glove.SmeltGather) item.info = Smelting[item.info];
						
					if(glove.Remove	) 
						if(RemoveGloves(player, attire)) break;
				}
		}
		
			}
}
