using System;
using System.Collections.Generic;
using Newtonsoft.Json; 
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;    
using System.Linq;   
  
namespace Oxide.Plugins 
{ 
    [Info("XSkinMenu", "Я", "1.0.502")]
    class XSkinMenu : RustPlugin  
    { 
		#region Reference 
		
		[PluginReference] private Plugin ImageLibrary;
		
		#endregion
		 
		#region Config
		
		private SkinConfig config;

        private class SkinConfig  
        {		
			internal class GeneralSetting 
			{
				[JsonProperty("Сгенерировать/Проверять и добавлять новые скины принятые разработчиками или сделаные для твич дропсов")] public bool UpdateSkins;
				[JsonProperty("Сгенерировать/Проверять и добавлять новые скины добавленные разработчиками [ К примеру скин на хазмат ]")] public bool UpdateSkinsFacepunch;
				[JsonProperty("Отображать кнопку для удаления всех скинов")] public bool ButtonClear;
				[JsonProperty("Черный список скинов которые нельзя изменить. [ Например: огненные перчатки, огненный топор ]]")] public List<ulong> Blacklist = new List<ulong>();
			}			 
			     
			internal class MenuSSetting 
			{ 
				[JsonProperty("Иконка включенного параметра")] public string TButtonIcon = "";
				[JsonProperty("Иконка вылюченного параметра")] public string FButtonIcon = "";				
				[JsonProperty("Цвет включенного параметра")] public string CTButton = "";
				[JsonProperty("Цвет вылюченного параметра")] public string CFButton = "";
			}			
			
			[JsonProperty("Общие настройки")]
			public GeneralSetting Setting = new GeneralSetting();			
			[JsonProperty("Меню настроект")]
			public MenuSSetting MenuS = new MenuSSetting();
			[JsonProperty("Настройка категорий")] 
            public Dictionary<string, List<string>> Category = new Dictionary<string, List<string>>();								
			
			public static SkinConfig GetNewConfiguration()
            {
                return new SkinConfig
                {
					Setting = new GeneralSetting
					{
						UpdateSkins = true,
						UpdateSkinsFacepunch = false,
						ButtonClear = true,
						Blacklist = new List<ulong>
						{
							1742796979,
							841106268
						}
					},
					MenuS = new MenuSSetting
					{
						TButtonIcon = "assets/icons/check.png",
						FButtonIcon = "assets/icons/close.png",
						CTButton = "0.53 0.77 0.35 0.8",
						CFButton = "1 0.4 0.35 0.8"
					},
					Category = new Dictionary<string, List<string>>
					{
						["weapon"] = new List<string> { "gun.water", "pistol.revolver", "pistol.semiauto", "pistol.python", "pistol.eoka", "shotgun.waterpipe", "shotgun.double", "shotgun.pump", "bow.hunting", "crossbow", "grenade.f1", "smg.2", "smg.thompson", "smg.mp5", "rifle.ak", "rifle.lr300", "lmg.m249", "rocket.launcher", "rifle.semiauto", "rifle.m39", "rifle.bolt", "rifle.l96", "longsword", "salvaged.sword", "knife.combat", "bone.club", "knife.bone" },
						["construction"] = new List<string> { "wall.frame.garagedoor", "door.double.hinged.toptier", "door.double.hinged.metal", "door.double.hinged.wood", "door.hinged.toptier", "door.hinged.metal", "door.hinged.wood", "barricade.concrete", "barricade.sandbags" },
						["item"] = new List<string> { "locker", "vending.machine", "fridge", "furnace", "table", "chair", "box.wooden.large", "box.wooden", "rug.bear", "rug", "sleepingbag", "water.purifier", "target.reactive", "sled", "discofloor", "paddlingpool", "innertube", "boogieboard", "beachtowel", "beachparasol", "beachchair", "skull.trophy", "skullspikes" },
						["attire"] = new List<string> { "metal.facemask", "coffeecan.helmet", "riot.helmet", "bucket.helmet", "deer.skull.mask", "twitch.headset", "sunglasses", "mask.balaclava", "burlap.headwrap", "hat.miner", "hat.beenie", "hat.boonie", "hat.cap", "mask.bandana", "metal.plate.torso", "roadsign.jacket", "roadsign.kilt", "roadsign.gloves", "burlap.gloves", "attire.hide.poncho", "jacket.snow", "jacket", "tshirt.long", "hazmatsuit", "hoodie", "shirt.collared", "tshirt", "burlap.shirt", "attire.hide.vest", "shirt.tanktop", "attire.hide.helterneck", "pants", "burlap.trousers", "pants.shorts", "attire.hide.pants", "attire.hide.skirt", "shoes.boots", "burlap.shoes", "attire.hide.boots" },
						["tool"] = new List<string> { "fun.guitar", "jackhammer", "icepick.salvaged", "pickaxe", "stone.pickaxe", "rock", "hatchet", "stonehatchet", "explosive.satchel", "hammer" }
					}
				};
			}
        }
		 
		protected override void LoadConfig()
        {
            base.LoadConfig(); 
			 
			try
			{
				config = Config.ReadObject<SkinConfig>();
			}
			catch  
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = SkinConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
		
		#endregion		
		
		#region Data
		
	    internal class Data 
		{
			[JsonProperty("Смена скинов в инвентаре")] public bool ChangeSI = true;
			[JsonProperty("Смена скинов на предметах")] public bool ChangeSE = true;
			[JsonProperty("Смена скинов при крафте")] public bool ChangeSC = true;
			[JsonProperty("Смена скинов в инвентаре после удаления")] public bool ChangeSCL = true;
			[JsonProperty("Смена скинов при попадании в инвентарь")] public bool ChangeSG;
			[JsonProperty("Скины")] public Dictionary<string, ulong> Skins = new Dictionary<string, ulong>();
		}
		
		private Dictionary<ulong, Data> StoredData = new Dictionary<ulong, Data>();
		private Dictionary<ulong, bool> StoredDataFriends = new Dictionary<ulong, bool>();
		public Dictionary<string, List<ulong>> StoredDataSkins = new Dictionary<string, List<ulong>>();
		
		private void LoadData(BasePlayer player)
		{ 
            var Data = Interface.Oxide.DataFileSystem.ReadObject<Data>($"XSkinMenu/UserSettings/{player.userID}");
             
            if (!StoredData.ContainsKey(player.userID)) 
                StoredData.Add(player.userID, new Data());            
			if (!StoredDataFriends.ContainsKey(player.userID)) 
                StoredDataFriends.Add(player.userID, false);
  
            StoredData[player.userID] = Data ?? new Data();  
			
			if (StoredData[player.userID].Skins.Count == 0)
			    foreach(var skin in StoredDataSkins) 
					StoredData[player.userID].Skins.Add(skin.Key, 0);
		}  
		
		private void SaveData(BasePlayer player) => Interface.Oxide.DataFileSystem.WriteObject($"XSkinMenu/UserSettings/{player.userID}", StoredData[player.userID]);
		
	    private void Unload()
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList)
			{
				SaveData(player);
				CuiHelper.DestroyUi(player, ".GUIS");
			}
			
			Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Friends", StoredDataFriends);
		}
		
		#endregion		
		
		private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();
		
		#region Commands
		
		[ChatCommand("skin")]
		private void cmdOpenGUI(BasePlayer player) 
		{
			if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.use"))
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
			else
			    GUI(player);		
		}
		
		[ChatCommand("skinentity")]
		private void cmdSetSkinEntity(BasePlayer player)
		{
			if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.entity"))
			{
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
				return;
			}
			
			if(StoredData[player.userID].ChangeSE)
			{
			    RaycastHit rhit;
 
			    if (!Physics.Raycast(player.eyes.HeadRay(), out rhit, 3f, LayerMask.GetMask("Deployed", "Construction"))) return;
			    var entity = rhit.GetEntity();
			
                if (entity != null && (entity.OwnerID == player.userID || player.currentTeam != 0 && player.Team.members.Contains(entity.OwnerID) && StoredDataFriends.ContainsKey(entity.OwnerID) && StoredDataFriends[entity.OwnerID]))
			    {
				    if(shortnamesEntity.ContainsKey(entity.ShortPrefabName))
				    {
				        var shortname = shortnamesEntity[entity.ShortPrefabName];
				        if(!StoredData[player.userID].Skins.ContainsKey(shortname)) return;
					
				        SetSkinEntity(player, entity, shortname);
				    }
			    }
			}
		}
		
		[ConsoleCommand("skin_c")]
		private void ccmdCategoryS(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.use")) 
			{
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
				return;
			}
			
			if (Cooldowns.ContainsKey(player))
                if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
			
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			Effect z = new Effect("assets/bundled/prefabs/fx/weapons/survey_charge/survey_charge_stick.prefab", player, 0, new Vector3(), new Vector3());
			
			switch(args.Args[0])
			{
				case "category":
				{
					CategoryGUI(player, int.Parse(args.Args[2]));
					ItemGUI(player, args.Args[1]);
					EffectNetwork.Send(x, player.Connection);
					break;
				}				
				case "skin":
				{ 
					SkinGUI(player, args.Args[1]);
					EffectNetwork.Send(x, player.Connection);
					break; 
				}				
				case "setskin":
				{ 
					string item = args.Args[1];
					ulong skin = ulong.Parse(args.Args[2]);
					
					if(!StoredData[player.userID].Skins.ContainsKey(item)) return;
					
					Effect y = new Effect("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", player, 0, new Vector3(), new Vector3());
					StoredData[player.userID].Skins[item] = skin;
					
					if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.inventory"))
						SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
					else
					    if(StoredData[player.userID].ChangeSI) SetSkinItem(player, item, skin);
					
					EffectNetwork.Send(y, player.Connection);
					break;
				}				
				case "clear":
				{
					string item = args.Args[1];
					StoredData[player.userID].Skins[item] = 0;
					
					CuiHelper.DestroyUi(player, $".I + {args.Args[2]}");
					if(StoredData[player.userID].ChangeSCL) SetSkinItem(player, item, 0);
					
					EffectNetwork.Send(z, player.Connection);
					break;
				}				
				case "clearall":
				{
					StoredData[player.userID].Skins.Clear();
					
					foreach(var skin in StoredDataSkins) 
						StoredData[player.userID].Skins.Add(skin.Key, 0);
					
					GUI(player);
					EffectNetwork.Send(z, player.Connection);
					break;
				}
			}
			
			Cooldowns[player] = DateTime.Now.AddSeconds(0.5f); 
		}
		
		[ConsoleCommand("skin_s")]
		private void ccmdSetting(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.setting")) 
			{
				SendReply(player, lang.GetMessage("NOPERM", this, player.UserIDString));
				return;
			}
			
			if (Cooldowns.ContainsKey(player))
                if (Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
			
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			
			switch(args.Args[0])
			{
				case "open":
				{
					SettingGUI(player);
					break;
				}				
				case "inventory":
				{
					StoredData[player.userID].ChangeSI = !StoredData[player.userID].ChangeSI;
					SettingGUI(player);
					break;
				}				
				case "entity":
				{
					StoredData[player.userID].ChangeSE = !StoredData[player.userID].ChangeSE;
					SettingGUI(player);
					break;
				}				
				case "craft":
				{
					StoredData[player.userID].ChangeSC = !StoredData[player.userID].ChangeSC;
					SettingGUI(player);
					break;
				}				
				case "clear":
				{
					StoredData[player.userID].ChangeSCL = !StoredData[player.userID].ChangeSCL;
					SettingGUI(player);
					break;
				}				
				case "give":
				{
					StoredData[player.userID].ChangeSG = !StoredData[player.userID].ChangeSG;
					SettingGUI(player);
					break;
				}				
				case "friends":
				{
					StoredDataFriends[player.userID] = !StoredDataFriends[player.userID];
					SettingGUI(player);
					break;
				}
			}
			
			EffectNetwork.Send(x, player.Connection);
			Cooldowns[player] = DateTime.Now.AddSeconds(0.5f);
		}
		
		[ConsoleCommand("page.xskinmenu")]
		private void ccmdPage(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			
			string item = args.Args[2];
			int Page = int.Parse(args.Args[3]);
			
			switch (args.Args[0])
			{
				case "item":
				{
					switch(args.Args[1])
					{
				        case "next":
				        {
				        	ItemGUI(player, item, Page + 1);	
				        	break;
				        }						
				        case "back":
				        {
				        	ItemGUI(player, item, Page - 1);
				        	break;
				        }
					}
					break;
				}				
				case "skin":
				{
					switch(args.Args[1])
					{
				        case "next":
				        {
				        	SkinGUI(player, item, Page + 1);	
				        	break;
				        }						
				        case "back":
				        {
				    	    SkinGUI(player, item, Page - 1);
				    	    break;
				        }
					}
					break;
				}
			}
			
			EffectNetwork.Send(x, player.Connection);
		}
		
		[ConsoleCommand("xskin")]
		private void ccmdAdmin(ConsoleSystem.Arg args)
		{
			if (args.Player() == null || args.Player().IsAdmin)
			{
				string item = args.Args[1];
				
				if(!StoredDataSkins.ContainsKey(item))
				{
					PrintWarning($"Не найдено предмета <{item}> в списке!");
					return;
				}
				
				switch(args.Args[0])
				{
					case "add": 
					{
						ulong skinID = ulong.Parse(args.Args[2]);
							
						if(StoredDataSkins[item].Contains(skinID))
							PrintWarning($"Скин <{skinID}> уже есть в списке скинов предмета <{item}>!");
						else
						{
							StoredDataSkins[item].Add(skinID);
							PrintWarning($"Скин <{skinID}> успешно добавлен в список скинов предмета <{item}>!");
						}
						
						break;
					}					
					case "remove":
					{
						ulong skinID = ulong.Parse(args.Args[2]);
						
						if(StoredDataSkins[item].Contains(skinID))
						{
							StoredDataSkins[item].Remove(skinID);
							PrintWarning($"Скин <{skinID}> успешно удален из списка скинов предмета <{item}>!");
						}
						else
							PrintWarning($"Скин <{skinID}> не найден в списке скинов предмета <{item}>!");
						
						break;
					}					
					case "list": 
					{
						if(StoredDataSkins[item].Count == 0)
						{
							PrintWarning($"Список скинов предмета <{item}> пуст!");
							return;
						}
						
						string skinslist = $"Список скинов предмета <{item}>:\n";
						
						foreach(ulong skinID in StoredDataSkins[item])
						    skinslist += $"\n{skinID}";
						
						PrintWarning(skinslist);
						
						break;
					}					
					case "clearlist":
					{
						if(StoredDataSkins[item].Count == 0)
						{
							PrintWarning($"Список скинов предмета <{item}> уже пуст!");
							return;
						}
						else
						{
							StoredDataSkins[item].Clear();
							PrintWarning($"Список скинов предмета <{item}> успешно очищен!");
						} 
						
						break;  
					}					  
				}
				
				Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Skins", StoredDataSkins);
			}
		}
		 
		#endregion		 
		
		private readonly Dictionary<string, string> shortnamesEntity = new Dictionary<string, string>();
		
		#region Hooks 
		 
	    private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - TopPlugin.ru\n" +
			"     VK - vk.com/rustnastroika\n" + 
			"     Discord - PsiX#2920\n" +
			"     Config - v.3192\n" + 
			"-----------------------------"); 

			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XSkinMenu/Friends"))
                StoredDataFriends = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>("XSkinMenu/Friends");			
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("XSkinMenu/Skins"))
                StoredDataSkins = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, List<ulong>>>("XSkinMenu/Skins");			
			
			foreach (var category in config.Category)
			    foreach (var item in category.Value)
					if (!ImageLibrary.Call<bool>("HasImage", item + 150))
				        ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{item}/{150}", item + 150);
					
			foreach(var item in StoredDataSkins)
			    foreach(var skin in item.Value)
			        if (!ImageLibrary.Call<bool>("HasImage", $"{skin}" + 152) && !errorskins.ContainsKey(skin) && !facepunchskins.ContainsKey(skin))
					    ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin/{skin}/{150}", $"{skin}" + 152);
					else if(errorskins.ContainsKey(skin))
						ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{errorskins[skin]}/{150}", errorskins[skin] + 150);					
					else if(facepunchskins.ContainsKey(skin))
						ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{facepunchskins[skin]}/{150}", facepunchskins[skin] + 150);
				
			foreach (var item in ItemManager.GetItemDefinitions())
			{
				var prefab = item.GetComponent<ItemModDeployable>()?.entityPrefab?.resourcePath;
				if (string.IsNullOrEmpty(prefab)) continue;
				 
				var shortPrefabName = Utility.GetFileNameWithoutExtension(prefab);
				if (!string.IsNullOrEmpty(shortPrefabName) && !shortnamesEntity.ContainsKey(shortPrefabName))
				    shortnamesEntity.Add(shortPrefabName, item.shortname);
			}
			  
			GenerateItems();
				
			BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
			timer.Every(180, () => BasePlayer.activePlayerList.ToList().ForEach(SaveData));
			timer.Every(200, () => Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Friends", StoredDataFriends));
			
			InitializeLang();
			permission.RegisterPermission("xskinmenu.use", this);
			permission.RegisterPermission("xskinmenu.setting", this);
			permission.RegisterPermission("xskinmenu.craft", this);
			permission.RegisterPermission("xskinmenu.entity", this);
			permission.RegisterPermission("xskinmenu.inventory", this);   
			permission.RegisterPermission("xskinmenu.give", this);   
		}
		
		private void GenerateItems()
		{
			if(config.Setting.UpdateSkins)
				foreach (var pair in Rust.Workshop.Approved.All)
				{
					if (pair.Value == null || pair.Value.Skinnable == null) continue;
				
					ulong skinID = pair.Value.WorkshopdId; 
				
					string item = pair.Value.Skinnable.ItemName;
					if (item.Contains("lr300")) item = "rifle.lr300";
				
					if(!StoredDataSkins.ContainsKey(item))
						StoredDataSkins.Add(item, new List<ulong>());
				
					if(!StoredDataSkins[item].Contains(skinID))
						StoredDataSkins[item].Add(skinID);
				}
			
			if(config.Setting.UpdateSkinsFacepunch)
				foreach (ItemDefinition item in ItemManager.GetItemDefinitions())
				{
					foreach(var skinID in ItemSkinDirectory.ForItem(item).Select(skin => Convert.ToUInt64(skin.id)))
					{
						if(!StoredDataSkins.ContainsKey(item.shortname))
						StoredDataSkins.Add(item.shortname, new List<ulong>());
				
						if(!StoredDataSkins[item.shortname].Contains(skinID))
						StoredDataSkins[item.shortname].Add(skinID);
					}
				}	
			
			Interface.Oxide.DataFileSystem.WriteObject("XSkinMenu/Skins", StoredDataSkins);
		}
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }		
			
			LoadData(player);
		}  
		
	    private void OnPlayerDisconnected(BasePlayer player)
		{
			if (StoredData.ContainsKey(player.userID)) 
			{   
				SaveData(player);
				StoredData.Remove(player.userID);
			}			
			  
			if (Cooldowns.ContainsKey(player)) 
				Cooldowns.Remove(player);  
		}
		
		public Dictionary<ulong, string> errorskins = new Dictionary<ulong, string>
		{
			[10180] = "hazmatsuit.spacesuit",
			[10201] = "hazmatsuit.nomadsuit",
			[10189] = "door.hinged.industrial.a",
			[13050] = "skullspikes.candles",
			[13051] = "skullspikes.pumpkin",
			[13052] = "skull.trophy.jar",
			[13053] = "skull.trophy.jar2",
			[13054] = "skull.trophy.table",
			[13056] = "sled.xmas", 
			[13057] = "discofloor.largetiles",
			[10198] = "factorydoor", 
			//[] = "sofa.pattern" 
		};		
		
		public Dictionary<ulong, string> facepunchskins = new Dictionary<ulong, string>
		{
			[10026] = "hat.cap.grey", [10027] = "hat.cap.forestcamo", [10028] = "hat.cap.red", [10029] = "hat.cap.blue", [10030] = "hat.cap.green", [10045] = "hat.cap.skin.cap.rescuecap", [10055] = "hat.cap.skin.cap.friendlycap",
			[10122] = "box.wooden.large.skin.woodstorage.christmasstorage", [10123] = "box.wooden.large.skin.woodstorage.firstaidgreen", [10124] = "box.wooden.large.skin.woodstorage.ammowoodenbox", [10141] = "box.wooden.large.skin.woodstorage.gunbox", [10135] = "rifle.ak.skin.ak47.digitalcamoak47", [10137] = "rifle.ak.skin.ak47.militarycamoak47", [10138] = "rifle.ak.skin.ak47.temperedak47",
			[10037] = "sleepingbag.skin.blueplaid", [10076] = "sleepingbag.skin.woodcamo", [10077] = "sleepingbag.skin.tigercrown", [10107] = "sleepingbag.skin.horrorbag", [10119] = "sleepingbag.skin.christmasbag", [10121] = "sleepingbag.skin.astonchristmas",
			[10052] = "hoodie.skin.bloody", [10086] = "hoodie.skin.skeleton", [10129] = "hoodie.skin.safetycrew", [10132] = "hoodie.skin.rhinocrunch", [10133] = "hoodie.skin.cuda87", [10142] = "hoodie.skin.bchillz", [14072] = "hoodie.green", [14178] = "hoodie.blue", [14179] = "hoodie.black",
			[10001] = "pants.jeans", [10019] = "pants.forestcamo", [10020] = "pants.urbancamo", [10021] = "pants.snowcamo", [10048] = "pants.skin.punkrock", [10049] = "pants.skin.bluetrackv.2", [10078] = "pants.skin.oldprisoner",
			[10114] = "pistol.revolver.skin.revolver.revolveroutback",
			[10022] = "shoes.boots.tan", [10023] = "shoes.boots.black",  [10034] = "shoes.boots.skin.boots.punkboots",
			[10044] = "shoes.boots.skin.boots.scavengedsneakers", [10080] = "shoes.boots.skin.boots.armyboots", [10088] = "shoes.boots.skin.boots.bloodyboots",
			[10074] = "shotgun.pump.skin.pumpshotgunchieftain", [10140] = "shotgun.pump.skin.pumpshotgun.theswampmaster",
			[10115] = "rifle.bolt.skin.boltrifle.ghostboltrifle", [10116] = "rifle.bolt.skin.boltrifle.tundraboltrifle", [10117] = "rifle.bolt.skin.boltrifle.dreamcatcher",
			[10059] = "mask.bandana.green.skin", [10060] = "mask.bandana.blue.skin", [10061] = "mask.bandana.black.skin", [10062] = "mask.bandana.camo.snow.skin", [10063] = "mask.bandana.camo.forest.skin", [10064] = "mask.bandana.skull.black.skin", [10065] = "mask.bandana.skull.red.skin", [10066] = "mask.bandana.camo.desert.skin", [10067] = "mask.bandana.checkered.black.skin", [10079] = "mask.bandana.skin.bandana.wizardbandana", [10104] = "mask.bandana.skin.bandana.creepyclownbandana",
			[10058] = "hat.boonie.skin.boonie.farmerhat",
			[10136] = "burlap.shirt.skin.burlapshirt.piratevest&shirt",
			[10143] = "shotgun.waterpipe.skin.pipeshotgun.thepeacepipe",
			[10197] = "rock.skull.skin",
			[10127] = "bucket.helmet.skin.buckethat.medichelmet",
			[10112] = "jacket.snow.woodland", [10113] = "jacket.snow.black", 
			[10008] = "jacket.snowcamo", [10009] = "jacket.green", [10010] = "jacket.red", [10011] = "jacket.blue", [10012] = "jacket.desertcamo", [10013] = "jacket.multicam", [10014] = "jacket.urbancamo", [10015] = "jacket.hunter", [10072] = "jacket.skin.provocateur",
			[10073] = "pistol.semiauto.skin.semiautopistol.redshine", [10081] = "pistol.semiauto.skin.semiautopistol.reapernotepistol", [10087] = "pistol.semiauto.skin.semiautopistol.contaminationpistol", [10108] = "pistol.semiauto.skin.semiautopistol.halloweenbat",
			[10128] = "burlap.gloves.skin.burlapgloves.boxer'sbandages",
			[10004] = "tshirt.long.black", [10005] = "tshirt.long.grey", [10006] = "tshirt.long.orange", [10007] = "tshirt.long.yellow", [10032] = "tshirt.long.shirtgreen", [10036] = "tshirt.long.skin.longtshirt.signpainter'slongtshirt", [10042] = "tshirt.long.skin.longtshirt.varsityjacket", [10047] = "tshirt.long.skin.longtshirt.azteclongt-shirt", [10050] = "tshirt.long.skin.longtshirt.frankensteinssweater", [10051] = "tshirt.long.skin.longtshirt.nightmaresweater", [10106] = "tshirt.long.skin.longtshirt.creepyjack", [10118] = "tshirt.long.skin.longtshirt.merryreindeer",
			[101] = "tshirt.red", [10002] = "tshirt.gmod", [10003] = "tshirt.black", [10024] = "tshirt.flag.germany", [10025] = "tshirt.flag.russia", [10033] = "tshirt.skin.baseball", [10035] = "tshirt.skin.hackervalleyveteran", [10038] = "tshirt.skin.murderer", [10039] = "tshirt.skin.targetpractice", [10041] = "tshirt.skin.blackskull&bones", [10043] = "tshirt.skin.vyshyvanka", [10046] = "tshirt.skin.missingtexturesfull", [10053] = "tshirt.skin.smilet-shirt", [10130] = "tshirt.skin.argylescavenger", [10134] = "tshirt.skin.serwinter", [14177] = "tshirt.blue", [14181] = "tshirt.forestcamo", [584379] = "tshirt.urbancamo",
			[10054] = "mask.balaclava.skin.balaclava.rorschachskull", [10057] = "mask.balaclava.skin.balaclava.muricabalaclava", [10068] = "mask.balaclava.camo.forest.skin", [10069] = "mask.balaclava.camo.desert.skin", [10070] = "mask.balaclava.check.red.skin", [10071] = "mask.balaclava.stripe.yellow.skin", [10075] = "mask.balaclava.skin.balaclava.nightmarebalaclava", [10084] = "mask.balaclava.skin.balaclava.therustknight", [10090] = "mask.balaclava.skin.balaclava.skinbalaclava", [10111] = "mask.balaclava.skin.balaclava.zipperface", [10139] = "mask.balaclava.skin.balaclava.valentinebalaclava",
			[10016] = "hat.beenie.red", [10017] = "hat.beenie.green", [10018] = "hat.beenie.blue", [10040] = "hat.beenie.skin.beenie.rastabeenie", [10085] = "hat.beenie.skin.beenie.winterdeers", [14180] = "hat.beenie.black",
			[13000] = "beachchair.camo", [13001] = "beachchair.flamingo", [13002] = "beachchair.leaves", [13003] = "beachchair.pizza", [13004] = "beachchair.target", [13005] = "beachchair.tarp", [13006] = "beachchair.zenlabs", 
			[13007] = "beachparasol.camo", [13008] = "beachparasol.flamingo", [13009] = "beachparasol.leaves", [13010] = "beachparasol.pizza", [13011] = "beachparasol.target", [13012] = "beachparasol.tarp", [13013] = "beachparasol.zenlabs",
			[13014] = "beachtowel.brownstripes", [13015] = "beachtowel.cobalt", [13016] = "beachtowel.dye", [13017] = "beachtowel.fish", [13018] = "beachtowel.shell", [13019] = "beachtowel.stripes", [13020] = "beachtowel.waves", 
			[13021] = "boogieboard.corrugated", [13022] = "boogieboard.crashtest", [13023] = "boogieboard.flames", [13024] = "boogieboard.marbled", [13025] = "boogieboard.shark", [13026] = "boogieboard.stars",
			[13027] = "innertube.camo", [13028] = "innertube.donut", [13029] = "innertube.horse", [13030] = "innertube.tire", [13031] = "innertube.unicorn", [13032] = "innertube.watermelon", [13033] = "innertube.zebra",
			[13034] = "paddlingpool.hottub", [13035] = "paddlingpool.rainbow", [13036] = "paddlingpool.seascape", [13037] = "paddlingpool.spawnpool",
			[13038] = "sunglasses.01.chalk", [13039] = "sunglasses.01.tortoise", [13040] = "sunglasses.02.black", [13041] = "sunglasses.02.camo", [13042] = "sunglasses.02.red", [13043] = "sunglasses.03.black", [13044] = "sunglasses.03.chrome", [13045] = "sunglasses.03.gold",
			[13046] = "gun.water.watergun.green", [13047] = "gun.water.watergun.grey", [13048] = "gun.water.watergun.orange", [13049] = "gun.water.watergun.yellow",
			[13055] = "twitch.headset.hat.cap_headset.twitch"
		};
		
		private void OnItemCraftFinished(ItemCraftTask task, Item item)
		{
			if(task.skinID == 0)
			{
				BasePlayer player = task.owner;
				
				if(!StoredData[player.userID].Skins.ContainsKey(item.info.shortname) || !permission.UserHasPermission(player.UserIDString, "xskinmenu.craft")) return;
				if(!StoredData[player.userID].ChangeSG && StoredData[player.userID].ChangeSC) 
					SetSkinCraftGive(player, item);
			}
		} 
		
		private void SetSkinItem(BasePlayer player, string item, ulong skin) 
		{
			foreach (var i in player.inventory.FindItemIDs(ItemManager.FindItemDefinition(item).itemid))
			{
				if (i.skin == skin || config.Setting.Blacklist.Contains(i.skin)) continue;
				
				if(errorskins.ContainsKey(skin))
				{
					var erroritem = errorskins[skin];
					
					i.UseItem();
					Item newitem = ItemManager.CreateByName(erroritem);
					newitem.condition = i.condition;
					newitem.maxCondition = i.maxCondition;
					
					player.GiveItem(newitem); 
				}
				else
				{
                    i.skin = skin;
                    i.MarkDirty();

                    BaseEntity entity = i.GetHeldEntity();
                    if (entity != null)
                    {
                        entity.skinID = skin;
                        entity.SendNetworkUpdate();
                    }
				}
			}
		}
		
		private void SetSkinCraftGive(BasePlayer player, Item item)
		{
			string shortname = item.info.shortname;
			ulong skin = StoredData[player.userID].Skins[shortname];
			
			if (item.skin == skin || config.Setting.Blacklist.Contains(item.skin)) return;
			
			if(errorskins.ContainsKey(skin))
			{
				var erroritem = errorskins[skin];
					
				item.UseItem();
				Item newitem = ItemManager.CreateByName(erroritem);
					
				player.GiveItem(newitem);
			}
			else
		    {
                item.skin = skin; 
                item.MarkDirty();

                BaseEntity entity = item.GetHeldEntity(); 
                if (entity != null)
                {
                    entity.skinID = skin;
                    entity.SendNetworkUpdate();
				}
			}
		}		
		
		private void SetSkinEntity(BasePlayer player, BaseEntity entity, string shortname)
		{
			ulong skin = StoredData[player.userID].Skins[shortname];
			
			if(skin == entity.skinID || skin == 0) return;
			if(errorskins.ContainsKey(skin))
			{
				SendInfo(player, lang.GetMessage("ERRORSKIN", this, player.UserIDString));
				return;
			}
			
			entity.skinID = skin;
            entity.SendNetworkUpdate();
			Effect.server.Run("assets/prefabs/deployable/repair bench/effects/skinchange_spraypaint.prefab", entity.transform.localPosition);
		}
		
		private void OnItemAddedToContainer(ItemContainer container, Item item)
		{
			if(container == null || item == null) return;
			if(container.playerOwner != null)
			{
				BasePlayer player = container.playerOwner;
			
				if(player == null || player.IsNpc || !player.userID.IsSteamId() || player.IsSleeping()) return;
				if(!permission.UserHasPermission(player.UserIDString, "xskinmenu.give") || !StoredData.ContainsKey(player.userID) || !StoredData[player.userID].Skins.ContainsKey(item.info.shortname)) return;
				if(StoredData[player.userID].ChangeSG) 
					SetSkinCraftGive(player, item);
			}
		} 
		
		#endregion		
		
		#region GUI
		
		private void GUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".GUIS");
            CuiElementContainer container = new CuiElementContainer(); 
			
			container.Add(new CuiPanel
            {
				CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-507.5 -260", OffsetMax = "507.5 290" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/icons/greyout.mat" }
            }, "Overlay", ".GUIS");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = "0.217 0.221 0.209 0.95" }
            }, ".GUIS", ".SGUI");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "470 237.5", OffsetMax = "497.5 265" },
                Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/close.png", Close = ".GUIS" },
                Text = { Text = "" }
            }, ".SGUI");			
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-497.5 237.5", OffsetMax = "-470 265" },
                Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/gear.png", Command = "skin_s open" },
                Text = { Text = "" }
            }, ".SGUI");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-455 237.5", OffsetMax = "455 265" },
                Text = { Text = lang.GetMessage("TITLE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "1 1 1 0.75" }
            }, ".SGUI");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-507.5 227.5", OffsetMax = "507.5 232.5" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".SGUI");				
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-507.5 177.5", OffsetMax = "507.5 182.5" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".SGUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "460 227.5", OffsetMax = "465 275" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".SGUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-465 227.5", OffsetMax = "-460 275" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".SGUI");   
			
			CuiHelper.AddUi(player, container);
			
			CategoryGUI(player);
			if(config.Category.Count != 0) ItemGUI(player, config.Category.ElementAt(0).Key);
		}
		
		private void CategoryGUI(BasePlayer player, int page = 0)
		{
			CuiHelper.DestroyUi(player, ".SkinBUTTON");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-497.5 187.5", OffsetMax = "497.5 222.5" },
                Image = { Color = "0 0 0 0" }
            }, ".SGUI", ".SkinBUTTON");
			
			int x = 0, count = config.Category.Count; 
			
			foreach(var category in config.Category)
			{
				string color = page == x ? "0.53 0.77 0.35 0.8" : "0 0 0 0";
				double offset = -(81 * count--) + -(2.5 * count--);

				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offset} -17.5", OffsetMax = $"{offset + 162} 17.5" },
                    Button = { Color = "0.51703192 0.521 0.509 0.5", Material = "assets/icons/greyout.mat", Command = $"skin_c category {category.Key} {x}" },
                    Text = { Text = lang.GetMessage(category.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 18, Color = "0.75 0.75 0.75 1" }
                }, ".SkinBUTTON", ".BUTTON");
 
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMax = "0 1.5" },
                    Image = { Color = color, Material = "assets/icons/greyout.mat" }
                }, ".BUTTON");
				
				x++;
			}
			
			CuiHelper.AddUi(player, container);
		}
		
		private void ItemGUI(BasePlayer player, string category, int Page = 0)
		{
			CuiHelper.DestroyUi(player, ".SettingGUI");
			CuiHelper.DestroyUi(player, ".SkinGUI");
			CuiHelper.DestroyUi(player, ".ItemGUI");
            CuiElementContainer container = new CuiElementContainer(); 
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-502.5 -270", OffsetMax = "502.5 177.5" },
                Image = { Color = "0 0 0 0" }
            }, ".SGUI", ".ItemGUI");
			
			int x = 0, y = 0, z = 0;
			
			foreach(var item in config.Category[category].Skip(Page * 40))
			{		
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-497.5 + (x * 100)} {123.25 - (y * 100)}", OffsetMax = $"{-402.5 + (x * 100)} {218.25 - (y * 100)}" },
                    Image = { Color = "0.51703192 0.521 0.509 0.5", Material = "assets/icons/greyout.mat" }
                }, ".ItemGUI", ".Item");
				
				container.Add(new CuiElement 
                {
                    Parent = ".Item",
                    Components =
                    {
					    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item + 150) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });
				
				if(StoredDataSkins.ContainsKey(item) && StoredDataSkins[item].Count != 0 && StoredData[player.userID].Skins.ContainsKey(item))
				{
				    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = $"skin_c skin {item}" },
                        Text = { Text = "" }
                    }, ".Item");				    
				
				    if(StoredData[player.userID].Skins[item] != 0)
				        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-20 5", OffsetMax = "-5 20" },
                            Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/clear.png", Command = $"skin_c clear {item} {z}" },
                            Text = { Text = "" }
                        }, ".Item", $".I + {z}");
				}
				
				x++;
				z++;
				
				if(x == 10)
				{
					x = 0;
					y++;
					
					if(y == 4)
						break;
				}
			}
			
			bool back = Page != 0;
			bool next = config.Category[category].Count > ((Page + 1) * 40);

			container.Add(new CuiButton 
            {    
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-190 5", OffsetMax = "-100 31.75" },
                Button = { Color = back ? "0.65 0.29 0.24 1" : "0.65 0.29 0.24 0.4", Command = back ? $"page.xskinmenu item back {category} {Page}" : "" },
                Text = { Text = lang.GetMessage("BACK", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = back ? "0.92 0.79 0.76 1" : "0.92 0.79 0.76 0.4" }
            }, ".ItemGUI");				 			
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-95 5", OffsetMax = "-5 31.75" },
                Button = { Color = next ? "0.35 0.45 0.25 1" : "0.35 0.45 0.25 0.4", Command = next ? $"page.xskinmenu item next {category} {Page}" : "" },
                Text = { Text = lang.GetMessage("NEXT", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = next ? "0.75 0.95 0.41 1" : "0.75 0.95 0.41 0.4" }
            }, ".ItemGUI");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 36.75", OffsetMax = "0 41.75" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/icons/greyout.mat" }
            }, ".ItemGUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-200 0", OffsetMax = "-195 36.75" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/icons/greyout.mat" }
            }, ".ItemGUI");
			
			if(config.Setting.ButtonClear)
			{
				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "5 5", OffsetMax = "190 31.75" },
					Button = { Color = "0.35 0.45 0.25 1", Command = "skin_c clearall" },
					Text = { Text = lang.GetMessage("CLEARALL", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf", Color = "0.75 0.95 0.41 1" }
				}, ".ItemGUI");
			
				container.Add(new CuiPanel
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "195 0", OffsetMax = "200 36.75" },
					Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/icons/greyout.mat" }
				}, ".ItemGUI");
			}
			
			CuiHelper.AddUi(player, container);
		}		
		
		private void SkinGUI(BasePlayer player, string item, int Page = 0)
		{
			CuiHelper.DestroyUi(player, ".SkinGUI");
            CuiElementContainer container = new CuiElementContainer(); 
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-502.5 -270", OffsetMax = "502.5 177.5" },
                Image = { Color = "0.217 0.221 0.209 1" }
            }, ".SGUI", ".SkinGUI");
			
			int x = 0, y = 0;
			ulong s = StoredData[player.userID].Skins[item];
			
			foreach(var skin in StoredDataSkins[item].Skip(Page * 40))
			{		
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-497.5 + (x * 100)} {123.25 - (y * 100)}", OffsetMax = $"{-402.5 + (x * 100)} {218.25 - (y * 100)}" },
                    Image = { Color = s == skin ? "0.53 0.77 0.35 0.8" : "0.51703192 0.521 0.509 0.5", Material = "assets/icons/greyout.mat" }
                }, ".SkinGUI", ".Skin");
				
				container.Add(new CuiElement
                {
                    Parent = ".Skin",
                    Components =
                    {
					    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", errorskins.ContainsKey(skin) ? errorskins[skin] + 150 : facepunchskins.ContainsKey(skin) ? facepunchskins[skin] + 150 : $"{skin}152") },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });
				
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"skin_c setskin {item} {skin}" },
                    Text = { Text = "" }
                }, ".Skin");				
				
				x++;
				
				if(x == 10)
				{
					x = 0;
					y++;
					
					if(y == 4)
						break;
				}
			}
			
			bool back = Page != 0;
			bool next = StoredDataSkins[item].Count > ((Page + 1) * 40);
			
			container.Add(new CuiButton 
            {    
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-190 5", OffsetMax = "-100 31.75" },
                Button = { Color = back ? "0.65 0.29 0.24 1" : "0.65 0.29 0.24 0.4", Command = back ? $"page.xskinmenu skin back {item} {Page}" : "" },
                Text = { Text = lang.GetMessage("BACK", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = back ? "0.92 0.79 0.76 1" : "0.92 0.79 0.76 0.4" }
            }, ".SkinGUI");				 			
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-95 5", OffsetMax = "-5 31.75" },
                Button = { Color = next ? "0.35 0.45 0.25 1" : "0.35 0.45 0.25 0.4", Command = next ? $"page.xskinmenu skin next {item} {Page}" : "" },
                Text = { Text = lang.GetMessage("NEXT", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = next ? "0.75 0.95 0.41 1" : "0.75 0.95 0.41 0.4" }
            }, ".SkinGUI");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 36.75", OffsetMax = "0 41.75" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/icons/greyout.mat" }
            }, ".SkinGUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-200 0", OffsetMax = "-195 36.75" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/icons/greyout.mat" }
            }, ".SkinGUI");
			
			CuiHelper.AddUi(player, container);
		}
		
		private void SettingGUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".SettingGUI");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-502.5 -270", OffsetMax = "502.5 177.5" },
                Image = { Color = "0.217 0.221 0.209 1" }
            }, ".SGUI", ".SettingGUI");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-300 -105", OffsetMax = "300 -80" },
                Text = { Text = lang.GetMessage("SETINFO", this, player.UserIDString), Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "0.75 0.75 0.75 0.4" }
            }, ".SettingGUI");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = ".SettingGUI" },
                Text = { Text = "" }
            }, ".SettingGUI");
			
			container.Add(new CuiPanel
            { 
				CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -75", OffsetMax = "400 75" },
                Image = { Color = "0.51703192 0.521 0.509 0.95", Material = "assets/icons/greyout.mat" }
            }, ".SettingGUI", ".SGUIM");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = "0.217 0.221 0.209 0.95" }
            }, ".SGUIM", ".SGUIMM");
			
			Dictionary<string, bool> setting = new Dictionary<string, bool>
			{
				["inventory"] = StoredData[player.userID].ChangeSI,
				["entity"] = StoredData[player.userID].ChangeSE,
				["craft"] = StoredData[player.userID].ChangeSC,
				["clear"] = StoredData[player.userID].ChangeSCL,
				["give"] = StoredData[player.userID].ChangeSG,
				["friends"] = StoredDataFriends[player.userID]
			};
			
			int x = 0, y = 0;
			
			foreach(var s in setting) 
			{		
			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-390 + (x * 392.5)} {25 - (y * 45)}", OffsetMax = $"{-2.5 + (x * 392.5)} {65 - (y * 45)}" },
                    Image = { Color = "0.51703192 0.521 0.509 0.5", Material = "assets/icons/greyout.mat" }
                }, ".SGUIMM", ".SM");
				
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-190 -15", OffsetMax = "-160 15" },
                    Button = { Color = s.Value ? config.MenuS.CTButton : config.MenuS.CFButton, Sprite = s.Value ? config.MenuS.TButtonIcon : config.MenuS.FButtonIcon, Command = $"skin_s {s.Key}" },
                    Text = { Text = "" }
                }, ".SM");				
				
			    container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-155 -15", OffsetMax = "190 15" },
                    Text = { Text = lang.GetMessage(s.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = "0.75 0.75 0.75 1" }
                }, ".SM");
				
				x++;
				
				if(x == 2)
				{
					x = 0;
					y++;
				}
			}
			
			CuiHelper.AddUi(player, container);
		}  
		
		#endregion
		
		#region Message
		
		private void SendInfo(BasePlayer player, string message)
        {
            player.SendConsoleCommand("gametip.showgametip", message);
            timer.Once(5f, () => player.SendConsoleCommand("gametip.hidegametip"));
        }
		
		#endregion
		
		#region Lang
 
        private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "COOL SERVER SKINS MENU",
				["SETINFO"] = "YOU CAN CUSTOMIZE THE MENU DEPENDING ON THE SITUATION!",
				["ERRORSKIN"] = "THE SKIN YOU CHOSE CAN BE CHANGED ONLY IN THE INVENTORY OR WHEN CRAFTING!",
				["CLEARALL"] = "UPDATE MY SKIN LIST",
				["NOPERM"] = "No permissions!",
				["NEXT"] = "NEXT",
				["BACK"] = "BACK",
                ["weapon"] = "WEAPON",
                ["construction"] = "CONSTRUCTION",
                ["item"] = "ITEM",
                ["attire"] = "ATTIRE",
                ["tool"] = "TOOL",
                ["inventory"] = "CHANGE SKIN IN INVENTORY",
                ["entity"] = "CHANGE SKIN ON OBJECTS", 
                ["craft"] = "CHANGE SKIN WHEN CRAFTING",
                ["clear"] = "CHANGE SKIN WHEN DELETING",
                ["give"] = "SKIN CHANGE WHEN DROP IN INVENTORY",
				["friends"] = "ALLOW FRIENDS TO CHANGE YOUR SKINS"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "МЕНЮ СКИНОВ КРУТОГО СЕРВЕРА",
                ["SETINFO"] = "ВЫ МОЖЕТЕ КАСТОМНО НАСТРАИВАТЬ МЕНЮ В ЗАВИСИМОСТИ ОТ СИТУАЦИИ!",
                ["ERRORSKIN"] = "ВЫБРАННЫЙ ВАМИ СКИН МОЖНО ИЗМЕНИТЬ ТОЛЬКО В ИНВЕНТАРЕ ИЛИ ПРИ КРАФТЕ!",
                ["CLEARALL"] = "ОБНОВИТЬ МОЙ СПИСОК СКИНОВ",
				["NOPERM"] = "Недостаточно прав!",
				["NEXT"] = "ДАЛЕЕ",
				["BACK"] = "НАЗАД",
                ["weapon"] = "ОРУЖИЕ",
                ["construction"] = "СТРОИТЕЛЬСТВО",
                ["item"] = "ПРЕДМЕТЫ",
                ["attire"] = "ОДЕЖДА",
                ["tool"] = "ИНСТРУМЕНТЫ",
                ["inventory"] = "ПОМЕНЯТЬ СКИН В ИНВЕНТАРЕ",
                ["entity"] = "ПОМЕНЯТЬ СКИН НА ПРЕДМЕТАХ",
                ["craft"] = "ПОМЕНЯТЬ СКИН ПРИ КРАФТЕ",
                ["clear"] = "ПОМЕНЯТЬ СКИН ПРИ УДАЛЕНИИ",
                ["give"] = "ПОМЕНЯТЬ СКИН ПРИ ПОПАДАНИИ В ИНВЕНТАРЬ",
                ["friends"] = "РАЗРЕШИТЬ ДРУЗЬЯМ ИЗМЕНЯТЬ ВАШИ СКИНЫ"
            }, this, "ru");
        }

        #endregion
	}
}