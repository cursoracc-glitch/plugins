using UnityEngine;
using Oxide.Core;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using ConVar;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;
using System.Linq;
using System;
		   		 		  						  	   		  	   		  	   		   		 		  		  
namespace Oxide.Plugins
{
    [Info("XLevels", "MONSTER", "1.1.12")]
    class XLevels : RustPlugin
    {
		
		private void Message(BasePlayer player, string Messages)
        {
            CuiElementContainer container = new CuiElementContainer();
		   		 		  						  	   		  	   		  	   		   		 		  		  
            container.Add(new CuiLabel
            {
				FadeOut = 0.5f,
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-250 0", OffsetMax = "-200 26" },
                Text = { FadeIn = 0.5f, Text = $"{Messages}", Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = 13, Color = "1 1 1 0.4" }
            }, ".GUIProgress", ".Message", ".Message");

            CuiHelper.AddUi(player, container);
		   		 		  						  	   		  	   		  	   		   		 		  		  
            timer.Once(2.5f, () => { CuiHelper.DestroyUi(player, ".Message"); });
        }
		
		private void OnPluginUnloaded(Plugin name)
		{
			if(name.Title == "Better Chat" && config.Setting.ChatPrefix)
				Subscribe(nameof(OnPlayerChat));
		}
		
		private void OnPluginLoaded(Plugin name)
		{
			if(name.Title == "Better Chat")
				Unsubscribe(nameof(OnPlayerChat));
		}
		
		[ConsoleCommand("level")]
		private void ccmdOpenMenu(ConsoleSystem.Arg arg)
		{
			BasePlayer player = arg.Player();
			
			Effect x = new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3());
			
            switch (arg.Args[0].ToLower())
            {                
				case "page_lvl":
				{
					int Page = int.Parse(arg.Args[2]);
					
					switch (arg.Args[1])    
					{
						case "next": 
						{ 
							RewardMenu(player, Page + 1);
							break;
						}						
						case "back":
						{
							RewardMenu(player, Page - 1);
							break;
						}
					}
					break;
				}				
				case "page_inv":
				{
					int Page = int.Parse(arg.Args[2]);
					
					switch (arg.Args[1])
					{
						case "next":
						{
							InventoryRewardMenu(player, Page + 1);
							break;
						}						
						case "back":
						{
							InventoryRewardMenu(player, Page - 1);
							break;
						}
					}
					break;
				}
				case "exchangeinfo":
				{
					if(!config.Setting.CouponsValide) return;
					
					ExchangeInfo(player);
					break;
				}				
				case "exchange":
				{
					if(!config.Setting.CouponsValide) return;
					
					CouponExchange(player, int.Parse(arg.Args[1]));
					break;
				}				
				case "info":
				{
					Info(player);
					break;
				}				
				case "inventory":
				{
					InventoryRewardMenu(player);
					break;
				}				
				case "top":
				{
					if(permission.UserHasPermission(player.UserIDString, permTop)) Top(player);
					break;
				}
			}
			
			EffectNetwork.Send(x, player.Connection);
		}
		
		private void OnLootEntityEnd(BasePlayer player, NPCVendingMachine machine)
		{
			if(config.Vending.VendingUse)
			{
				CuiHelper.DestroyUi(player, ".Level_Overlay");
				CuiHelper.DestroyUi(player, ".Button");
			}
		}
		
		[ConsoleCommand("x_levels")]
		private void ccmdMenuOpen(ConsoleSystem.Arg arg)
		{
			BasePlayer player = arg.Player();
			
			GUIOpen(player);
		}
		
		private void RewardMenuFon(BasePlayer player)
		{
            CuiElementContainer container = new CuiElementContainer();
			
			bool couponsvalide = config.Setting.CouponsValide;
			bool vipvalide = config.Setting.VIPValide;
			
			var colorO = config.GUI.ColorBackgroundO;
			var colorT = config.GUI.ColorBackgroundT;
			
			container.Add(new CuiPanel
            {
                CursorEnabled = true,
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-532.5 -140", OffsetMax = "532.5 150" },
                Image = { Color = colorO, Material = "assets/icons/greyout.mat" }
            }, "Overlay", ".Level_Overlay", ".Level_Overlay");				
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = colorT }
            }, ".Level_Overlay", ".LevelO_Overlay"); 

			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-532.5 -105", OffsetMax = "532.5 -100" },
                Image = { Color = colorO, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".LevelO_Overlay");		
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = vipvalide ? "-132.5 -140" : "-327.5 -140", OffsetMax = vipvalide ? "-127.5 -105" : "-322.5 -105" },
                Image = { Color = colorO, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".LevelO_Overlay");			
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "327.5 -140", OffsetMax = "332.5 -105" },
                Image = { Color = colorO, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".LevelO_Overlay");	
 
			container.Add(new CuiButton 
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "5 5", OffsetMax = "195 30" },
                Button = { Color = "0.35 0.45 0.25 1", Command = "level inventory" },
                Text = { Text = lang.GetMessage("OPENINV", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = "0.75 0.95 0.41 1" }
            }, ".LevelO_Overlay");
			
			bool permvip = permission.UserHasPermission(player.UserIDString, permVip);
			bool permtop = permission.UserHasPermission(player.UserIDString, permTop);
			
			if(vipvalide)
				if(permvip)
					container.Add(new CuiButton 
					{    
						RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "200 5", OffsetMax = "390 30" },
						Button = { Color = "0.35 0.45 0.25 1" },
						Text = { Text = lang.GetMessage("VIPYES", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = "0.75 0.95 0.41 1" }
					}, ".LevelO_Overlay");	
				else	
					container.Add(new CuiButton 
					{    
						RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "200 5", OffsetMax = "390 30" },
						Button = { Color = "0.65 0.29 0.24 1" },
						Text = { Text = lang.GetMessage("VIPNO", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = "0.92 0.79 0.76 1" }
					}, ".LevelO_Overlay");
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-375 150", OffsetMax = "375 210" },
                Image = { Color = colorO, Material = "assets/icons/greyout.mat" }
            }, ".Level_Overlay", ".Title");
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = colorT }
            }, ".Title", ".T_Title");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-365 -20", OffsetMax = "-325 20" },
                Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/warning.png", Command = "level info" },
                Text = { Text = "" }
            }, ".T_Title");			
			
            container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-320 -25", OffsetMax = "-315 25" },
                Image = { Color = colorO, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".T_Title");			
			
			if(couponsvalide)
			{
				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-310 -20", OffsetMax = "-270 20" },
					Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/refresh.png", Command = "level exchangeinfo" },
					Text = { Text = "" }
				}, ".T_Title");
			
				container.Add(new CuiPanel
				{
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-265 -25", OffsetMax = "-260 25" },
					Image = { Color = colorO, Material = "assets/content/ui/uibackgroundblur.mat" }
				}, ".T_Title");
			} 
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "325 -20", OffsetMax = "365 20" },
                Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/close.png", Close = ".Level_Overlay" },
                Text = { Text = "" }
            }, ".T_Title");
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "315 -25", OffsetMax = "320 25" },
                Image = { Color = colorO, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".T_Title");
			
			if(permtop)
			{
				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "270 -20", OffsetMax = "310 20" },
					Button = { Color = "1 1 1 0.75", Sprite = "assets/icons/community_servers.png", Command = "level top" },
					Text = { Text = "" }
				}, ".T_Title");
			
				container.Add(new CuiPanel
				{
					RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "260 -25", OffsetMax = "265 25" },
					Image = { Color = colorO, Material = "assets/content/ui/uibackgroundblur.mat" }
				}, ".T_Title");
			}
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 1", OffsetMin = couponsvalide ? "-260 0" : "-315 0", OffsetMax = permtop ? "260 0" : "315 0" },
                Text = { Text = lang.GetMessage("LevelTitle", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "1 1 1 0.75" }
            }, ".T_Title");
			
			CuiHelper.AddUi(player, container);
			
			RewardMenu(player);
		}
        protected override void SaveConfig() => Config.WriteObject(config);
		
				
				
		private void OnServerInitialized()
		{	
			PrintWarning("\n-----------------------------\n" +
            "     Author - https://discord.gg/jPz48g7XXY\n" +
            "     VK - https://discord.gg/jPz48g7XXY\n" +
            "     Discord - https://discord.gg/jPz48g7XXY\n" +
			"     \n" +
			"-----------------------------");
			 
			InitializeLang();
			
			if(BetterChat || !config.Setting.ChatPrefix)
                Unsubscribe(nameof(OnPlayerChat));
			
			foreach(var i in config.Award)
			{
				var award = i.Value;
				string imgName = award.Shortname + award.SkinID;
				
				if(award.URLImage != String.Empty)
					ImageLibrary.Call("AddImage", award.URLImage, imgName + 151);
				else if(award.SkinID != 0)
					ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin/{award.SkinID}/{150}", imgName + 150);
				else
					ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{award.Shortname}/{150}", imgName + 150);
			}
			
			foreach(var i in config.AwardVip)
			{
				var award = i.Value;
				string imgName = award.Shortname + award.SkinID;
				
				if(award.URLImage != String.Empty)
					ImageLibrary.Call("AddImage", award.URLImage, imgName + 151);
				else if(award.SkinID != 0)
					ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin/{award.SkinID}/{150}", imgName + 150);
				else
					ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getimage/{award.Shortname}/{150}", imgName + 150);
			}
				
			foreach(var i in config.Coupon)
				ImageLibrary.Call("AddImage", $"http://api.skyplugins.ru/api/getskin/{i.SkinID}/{150}", i.SkinID.ToString());
			
			if(Interface.Oxide.DataFileSystem.ExistsDatafile("XDataSystem/XLevels/XLevels"))
                StoredData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, XLevelsData>>("XDataSystem/XLevels/XLevels");
			
			BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
			
			timer.Every(90, () =>  
			{ 
			    if(StoredData != null) Interface.Oxide.DataFileSystem.WriteObject("XDataSystem/XLevels/XLevels", StoredData);
				BasePlayer.activePlayerList.ToList().ForEach(SaveData); 
			}).Callback();
			
			foreach(string command in config.Setting.CommandList)
				cmd.AddChatCommand(command, this, cmdMenuOpen);
			
			permission.RegisterPermission(permVip, this);
			permission.RegisterPermission(permTop, this);
			
			foreach(var perm in config.XPRate.XPRatePermisssion)
				permission.RegisterPermission(perm.Key, this);
			foreach(var perm in config.Online.Permisssion)
                permission.RegisterPermission(perm.Key, this);
 
            if(config.Online.OnlineXP) 
				timer.Every(config.Online.TimeXP, () => BasePlayer.activePlayerList.ToList().ForEach(OGiveXP));		
		}
		
		private void Info(BasePlayer player)
        {
			CuiHelper.DestroyUi(player, ".ExchangeInfo");
			CuiHelper.DestroyUi(player, ".Top");
            CuiElementContainer container = new CuiElementContainer();

			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0.21 0.22 0.20 1", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".LevelO_Overlay", ".Info", ".Info");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = ".Info" },
                Text = { Text = "" }
            }, ".Info");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-225 -75", OffsetMax = "225 75" },
                Text = { Text = lang.GetMessage("Info", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 22, Color = "1 1 1 0.5" }
            }, ".Info");

            CuiHelper.AddUi(player, container);
        }		
		
		private string API_GetPlayerPrefix(BasePlayer player) => GetPlayerPrefix(player.userID);
		
		private object OnBetterChat(Dictionary<string, object> chat)
        {
			IPlayer player = chat["Player"] as IPlayer;
			
			string prefix = GetPlayerPrefix(Convert.ToUInt64(player.Id));
			
			if(!config.Setting.ChatPrefix || String.IsNullOrEmpty(prefix)) return chat;
			
			string name = $"{prefix} " + chat["Username"];
			chat["Username"] = name;

            return chat;
        }
		   		 		  						  	   		  	   		  	   		   		 		  		  
        private class LevelConfig 
        {
			
			[JsonProperty(LanguageEnglish ? "Coupons for XP" : "Купоны на ХР")]
			public List<Coupons> Coupon;	
			[JsonProperty(LanguageEnglish ? "VIP level reward" : "Вип награда за уровни")]
			public Dictionary<int, Awards> AwardVip;
			
			internal class OnlineSetting
			{
				[JsonProperty(LanguageEnglish ? "Enable issuing XP to online players" : "Включить выдачу XP онлайн игрокам")] public bool OnlineXP;
                [JsonProperty(LanguageEnglish ? "XP issue interval (in sec.)" : "Интервал выдачи XP (в сек.)")] public float TimeXP;	
			    [JsonProperty(LanguageEnglish ? "Setting up permissions. [ Permission | XP ]" : "Настройка пермишенов. [ Пермишен | XP ]")] public Dictionary<string, float> Permisssion;				
			}
			[JsonProperty(LanguageEnglish ? "Issuing XP for online" : "Выдача XP за онлайн")]
			public OnlineSetting Online = new OnlineSetting();
			
			internal class GUISetting
			{
				[JsonProperty("AnchorMin")] public string AnchorMin;
				[JsonProperty("AnchorMax")] public string AnchorMax;
				[JsonProperty("OffsetMin")] public string OffsetMin;
				[JsonProperty("OffsetMax")] public string OffsetMax;
				[JsonProperty(LanguageEnglish ? "Show mini-bar" : "Отображать мини-панель")] public bool EnablePanel;
				[JsonProperty(LanguageEnglish ? "Color background_1" : "Цвет фона_1")] public string ColorBackgroundO = "";
				[JsonProperty(LanguageEnglish ? "Color background_2" : "Цвет фона_2")] public string ColorBackgroundT = "";
				[JsonProperty(LanguageEnglish ? "Display reward container - [ True - Only when there is a reward in the level | False - Always ]" : "Отображать контейнер наград - [ True - Только когда есть награда на уровне | False - Всегда ]")] public bool ContainerReward;
				[JsonProperty(LanguageEnglish ? "Display required reward level" : "Отображать требуемый уровень наград")] public bool RewardNumber;
				[JsonProperty(LanguageEnglish ? "Display required VIP reward level" : "Отображать требуемый уровень ВИП наград")] public bool RewardNumberVIP;
			}			
			[JsonProperty(LanguageEnglish ? "XP multiplier" : "Множитель XP")]
			public XPRateSetting XPRate = new XPRateSetting();			
			[JsonProperty(LanguageEnglish ? "Vendings settings" : "Настройки магазинов")]
			public VendingSetting Vending = new VendingSetting();
			
			internal class XPSetting		
            {
				[JsonProperty(LanguageEnglish ? "XP for the pickup of resources" : "ХР за подбор ресурсов")] public Dictionary<string, float> PickupXP;				
				[JsonProperty(LanguageEnglish ? "XP for harvest" : "ХР за сбор урожая")] public Dictionary<string, float> HarvestXP;				
				[JsonProperty(LanguageEnglish ? "XP for bonus resources" : "ХР за бонусную добычу")] public Dictionary<string, float> GatherBonusXP;				
				[JsonProperty(LanguageEnglish ? "XP for kill / destroy barrel" : "ХР за убийство / разбитие бочек")] public Dictionary<string, float> KillXP;				
				[JsonProperty(LanguageEnglish ? "XP for opening crates" : "ХР за открытие ящиков")] public Dictionary<string, float> CrateXP;
			}
			[JsonProperty(LanguageEnglish ? "Settings levels" : "Настройка уровней")]
            public LevelSetting Level = new LevelSetting();

			internal class VendingSetting
			{
				[JsonProperty(LanguageEnglish ? "Open the level menu. [ True - Immediately after the opening of the NPC shop | False - UI button ]" : "Открывать меню уровней. [ True - Сразу после открытия НПС магазина | False - Через UI кнопку ]")] public bool VendingOpen;  
				[JsonProperty(LanguageEnglish ? "Access to the level menu is only through the NPC shops. [ True - NPC shop | False - Command ]" : "Доступ к меню уровней только через магазины НПС. [ True - Магазины НПС | False - Команда ]")] public bool VendingUse;
				[JsonProperty(LanguageEnglish ? "List of NPC shops in which you can open the level menu (shop name)" : "Список магазинов НПС в которых можно открыть меню уровней (Имя магазина)")] public List<string> ListNPCVending;				
			}
			
			internal class Coupons
			{
			    [JsonProperty(LanguageEnglish ? "Coupon name" : "Имя купона")] public string Name;
				[JsonProperty(LanguageEnglish ? "Coupon text" : "Текст купона")] public string Text;
				[JsonProperty(LanguageEnglish ? "Coupon skin" : "Скин купона")] public ulong SkinID;
				[JsonProperty(LanguageEnglish ? "XP amount" : "Кол-во ХР")] public int XP;

				[JsonProperty(LanguageEnglish ? "Setting the chance of falling out of crates/barrels" : "Настройка шанса выпадения из ящиков/бочек")]
                public List<Crates> Crate;

                public Coupons(string name, string text, ulong skinid, int xp, List<Crates> crates)
                {
                    Name = name; Text = text; SkinID = skinid; XP = xp; Crate = crates;
                }
			} 
			 
			[JsonProperty(LanguageEnglish ? "General settings" : "Общие настройки")]
			public GeneralSetting Setting = new GeneralSetting();
			internal class GeneralSetting
			{
				[JsonProperty(LanguageEnglish ? "List of commands to open the menu" : "Список команд для открытия меню")] public List<string> CommandList;
				[JsonProperty(LanguageEnglish ? "XP for the pickup of resources" : "Зачислять ХР за подбор ресурсов")] public bool PickupValide;
				[JsonProperty(LanguageEnglish ? "Display the level in prefix" : "Отображать уровень в префиксе")] public bool ChatPrefixLevel;
				[JsonProperty(LanguageEnglish ? "XP for opening crates" : "Зачислять ХР за открытие ящиков")] public bool CrateValide;
				[JsonProperty(LanguageEnglish ? "List of available ranks - [ Level - Rank ] ( If the list is empty, then the rank will not be displayed in the menu )" : "Список доступных рангов - [ Уровень - Ранг ] ( Если список пуст, то ранг не будет отображаться в меню )")] public Dictionary<int, string> RankList;
				[JsonProperty(LanguageEnglish ? "XP for kill" : "Зачислять ХР за убийство")] public bool KillValide;
				[JsonProperty(LanguageEnglish ? "Display the rank in prefix" : "Отображать ранг в префиксе")] public bool ChatPrefixRank;
				[JsonProperty(LanguageEnglish ? "Include messages of received rewards in chat" : "Включить сообщения полученных  наград в чат")] public bool ChatTakeMessages;
				[JsonProperty(LanguageEnglish ? "Enable prefix in chat - [ Set to False if the prefix should be disabled or the prefix is used by a third party chat plugin ]" : "Включить префикс в чате - [ Установите False, если префикс нужно отключить или префикс используется сторонним плагином для чата ]")] public bool ChatPrefix;
				[JsonProperty(LanguageEnglish ? "There is a plugin for custom loot" : "Есть плагин на кастомный лут")] public bool PluginCLoot;
				[JsonProperty(LanguageEnglish ? "XP for harvest" : "Зачислять ХР за сбор урожая")] public bool HarvestValide;
				[JsonProperty(LanguageEnglish ? "Reset the level and XP of the player after reaching the maximum level - [ Players will re-open levels and receive rewards ]" : "Обнулять уровень и ХР игрока после достижения максимального уровня - [ Игроки повторно будет открывать уровни и получать награды ]")] public bool ClearAll;
				[JsonProperty(LanguageEnglish ? "Profile SteamID for custom avatar" : "SteamID профиля для кастомной аватарки")] public ulong SteamID;
				[JsonProperty(LanguageEnglish ? "Take VIP Reward - [ True - take only with permission | False - take at any time without permission ]" : "Забрать ВИП награду - [ True - забрать только с пермишеном | False - забрать в любое время без пермишена ]")] public bool TakeReward;
				[JsonProperty(LanguageEnglish ? "Get VIP reward - [ True - only with permission | False - without permission ]" : "Получить ВИП награду - [ True - только с пермишеном | False - без пермишена ]")] public bool MoveReward;
				[JsonProperty(LanguageEnglish ? "Enable VIP rewards" : "Включить ВИП награды")] public bool VIPValide;
				[JsonProperty(LanguageEnglish ? "XP for bonus resources" : "Зачислять ХР за бонусные ресурсы")] public bool BonusValide;
				[JsonProperty(LanguageEnglish ? "Include level up messages in chat" : "Включить сообщения повышения уровня в чат")] public bool ChatLevelMessages;
				[JsonProperty(LanguageEnglish ? "Exchange coupons if you have already reached the maximum level - [ Suitable for top players ]" : "Обменивать купоны если уже достигнут максимальный уровень - [ Подходит для топа игроков ]")] public bool GiveXPCoupon;
				[JsonProperty(LanguageEnglish ? "Add XP if the maximum level is already reached - [ Suitable for top players ]" : "Засчитывать ХР если уже достигнут максимальный уровень - [ Подходит для топа игроков ]")] public bool GiveXP;
				[JsonProperty(LanguageEnglish ? "Enable coupons" : "Включить купоны")] public bool CouponsValide;
			}	
			[JsonProperty(LanguageEnglish ? "XP settings | Shortname : ValueXP" : "Настройка ХР | Shortname : ValueXP")]
            public XPSetting XP = new XPSetting();
			[JsonProperty(LanguageEnglish ? "Level reward" : "Награда за уровни")]
			public Dictionary<int, Awards> Award;			
			
			public static LevelConfig GetNewConfiguration()
            {
                return new LevelConfig
                {
					Setting = new GeneralSetting
					{
						PickupValide = true,
						HarvestValide = true,
						BonusValide = true,
						KillValide = true,
						CrateValide = true,
						ChatTakeMessages = true,
						ChatLevelMessages = true,
						CouponsValide = true,  
						VIPValide = true,
						MoveReward = true,
						TakeReward = true,
						GiveXP = false,
						GiveXPCoupon = false,
						ClearAll = false,
						PluginCLoot = false,
						SteamID = 0,
						CommandList = new List<string>
						{
							"level",
							"lvl",
							"pass"
						},
						ChatPrefix = true,
						ChatPrefixLevel = true,
						ChatPrefixRank = true,
						RankList = LanguageEnglish ? new Dictionary<int, string>
						{
							[0] = "CAP-Х",
							[1] = "IRON-1",
							[2] = "IRON-2",
							[3] = "IRON-3",
							[4] = "BRONZE-1",
							[5] = "BRONZE-2",
							[6] = "BRONZE-3",
							[7] = "SILVER-1",
							[8] = "SILVER-2",
							[9] = "SILVER-3",
							[10] = "GOLD-1",
							[11] = "GOLD-2",
							[12] = "GOLD-3",
							[13] = "PLATINUM-1",
							[14] = "PLATINUM-2",
							[15] = "PLATINUM-3",
							[16] = "DIAMOND-1",
							[17] = "DIAMOND-2",
							[18] = "DIAMOND-3",
							[19] = "IMMORTAL-1",
							[20] = "IMMORTAL-2",
							[21] = "IMMORTAL-3",
							[22] = "RADIANT-1",
							[23] = "RADIANT-2",
							[24] = "RADIANT-3",
							[25] = "GOD"
						} : new Dictionary<int, string>
						{
							[0] = "КЕПКА-Х",
							[1] = "ЖЕЛЕЗО-1",
							[2] = "ЖЕЛЕЗО-2",
							[3] = "ЖЕЛЕЗО-3",
							[4] = "БРОНЗА-1",
							[5] = "БРОНЗА-2",
							[6] = "БРОНЗА-3",
							[7] = "СЕРЕБРО-1",
							[8] = "СЕРЕБРО-2",
							[9] = "СЕРЕБРО-3",
							[10] = "ЗОЛОТО-1",
							[11] = "ЗОЛОТО-2",
							[12] = "ЗОЛОТО-3",
							[13] = "ПЛАТИНА-1",
							[14] = "ПЛАТИНА-2",
							[15] = "ПЛАТИНА-3",
							[16] = "АЛМАЗ-1",
							[17] = "АЛМАЗ-2",
							[18] = "АЛМАЗ-3",
							[19] = "БЕССМЕРТНЫЙ-1",
							[20] = "БЕССМЕРТНЫЙ-2",
							[21] = "БЕССМЕРТНЫЙ-3",
							[22] = "РАДИАНТ-1",
							[23] = "РАДИАНТ-2",
							[24] = "РАДИАНТ-3",
							[25] = "БОГ"
						}
					},
					Vending = new VendingSetting
					{
						VendingOpen = false,
						VendingUse = false,
						ListNPCVending = new List<string>
						{
							"Black Market"
						}
					},	
					XPRate = new XPRateSetting
					{
						XPRateCoupon = false,
						XPRatePermisssion = new Dictionary<string, float>
						{
							["xlevels.125p"] = 2.25f,
							["xlevels.75p"] = 1.75f,
							["xlevels.10p"] = 1.1f
						}
					},
					Online = new OnlineSetting
					{
						OnlineXP = false,
						TimeXP = 15.0f,
						Permisssion = new Dictionary<string, float>
						{
							["xlevels.default"] = 5.0f
						}
					},
					GUI = new GUISetting
					{
						AnchorMin = "1 0",
						AnchorMax = "1 0",
						OffsetMin = "-403 16",
						OffsetMax = "-210 42",
						EnablePanel = true,
						ColorBackgroundO = "0.51 0.52 0.50 0.95",
						ColorBackgroundT = "0.21 0.22 0.20 0.95",
						ContainerReward = false,
						RewardNumber = true,
						RewardNumberVIP = true
					},
					Level = new LevelSetting
					{
						LevelMax = 25,
						LevelXP = 100,
						LevelXPUP = 25
					},
					XP = new XPSetting
					{
						PickupXP = new Dictionary<string, float>
						{
							["stones"] = 10.0f,
							["sulfur.ore"] = 15.0f,
							["metal.ore"] = 12.5f
						},
						HarvestXP = new Dictionary<string, float>
						{
							["potato.entity"] = 2.5f,
							["corn.entity"] = 1.75f,
							["hemp.emtity"] = 0.25f
						},
						GatherBonusXP = new Dictionary<string, float>
						{
							["stones"] = 5.0f,
							["sulfur.ore"] = 10.0f,
							["metal.ore"] = 7.5f
						},
						KillXP = new Dictionary<string, float>
						{
							["boar"] = 10.0f, 
							["loot-barrel-1"] = 7.5f,
							["heavyscientist"] = 2.5f
						},
						CrateXP = new Dictionary<string, float>
						{ 
							["crate_normal"] = 5.0f,
							["crate_normal_2"] = 1.0f,
							["crate_tools"] = 3.5f
						}
					},
					Coupon = LanguageEnglish ? new List<Coupons>
					{
						new Coupons("Coupon 5ХР", "Coupon for 5ХР\n\nExchange them and get XP to level up!\n\nCommand for exchange - /level", 2925118427, 5, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 3) }),
						new Coupons("Coupon 10ХР", "Coupon for 10ХР\n\nExchange them and get XP to level up!\n\nCommand for exchange - /level", 2925118536, 10, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 3) }),
						new Coupons("Coupon 25ХР", "Coupon for 25ХР\n\nExchange them and get XP to level up!\n\nCommand for exchange - /level", 2925118910, 25, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 2) }),
						new Coupons("Coupon 50ХР", "Coupon for 50ХР\n\nExchange them and get XP to level up!\n\nCommand for exchange - /level", 2925119087, 50, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 2) }),
						new Coupons("Coupon 100ХР", "Coupon for 100ХР\n\nExchange them and get XP to level up!\n\nCommand for exchange - /level", 2925119157, 100, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 1) }),
						new Coupons("Coupon 200ХР", "Coupon for 200ХР\n\nExchange them and get XP to level up!\n\nCommand for exchange - /level", 2925119248, 200, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 1) }),
						new Coupons("Coupon 500ХР", "Coupon for 500ХР\n\nExchange them and get XP to level up!\n\nCommand for exchange - /level", 2925119644, 500, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 1) })
					} : new List<Coupons>
					{
						new Coupons("Купон 5ХР", "Купон на 5ХР\n\nОбменяйте его и получите ХР для повышения уровня!\n\nКоманда для обмена - /level", 2925118427, 5, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 3) }),
						new Coupons("Купон 10ХР", "Купон на 10ХР\n\nОбменяйте его и получите ХР для повышения уровня!\n\nКоманда для обмена - /level", 2925118536, 10, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 3) }),
						new Coupons("Купон 25ХР", "Купон на 25ХР\n\nОбменяйте его и получите ХР для повышения уровня!\n\nКоманда для обмена - /level", 2925118910, 25, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 2) }),
						new Coupons("Купон 50ХР", "Купон на 50ХР\n\nОбменяйте его и получите ХР для повышения уровня!\n\nКоманда для обмена - /level", 2925119087, 50, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 2) }),
						new Coupons("Купон 100ХР", "Купон на 100ХР\n\nОбменяйте его и получите ХР для повышения уровня!\n\nКоманда для обмена - /level", 2925119157, 100, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 1) }),
						new Coupons("Купон 200ХР", "Купон на 200ХР\n\nОбменяйте его и получите ХР для повышения уровня!\n\nКоманда для обмена - /level", 2925119248, 200, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 1) }),
						new Coupons("Купон 500ХР", "Купон на 500ХР\n\nОбменяйте его и получите ХР для повышения уровня!\n\nКоманда для обмена - /level", 2925119644, 500, new List<Crates>{ new Crates("crate_normal_2", 50.0f, 1, 1) })
					},
					Award = new Dictionary<int, Awards>
					{
						[1] = new Awards("wood", "Wood", 1250, 0, "", "", false),
						[2] = new Awards("charcoal", "Charcoal", 1500, 0, "", "", false),
						[3] = new Awards("metal.ore", "Metal ore", 1000, 0, "", "", false),
						[4] = new Awards("metal.fragments", "Metal fragments", 750, 0, "", "", false),
						[5] = new Awards("sulfur.ore", "Sulfur ore", 500, 0, "", "", false),
						[6] = new Awards("sulfur", "Sulfur", 300, 0, "", "", false),
						[7] = new Awards("gunpowder", "Gunpowder", 400, 0, "", "", false),
						[8] = new Awards("hq.metal.ore", "HQM ore", 25, 0, "", "", false), 
						[9] = new Awards("metal.refined", "HQM", 20, 0, "", "", false),
						[10] = new Awards("scrap", "Scrap", 50, 0, "", "", false)
					},					
					AwardVip = new Dictionary<int, Awards>
					{
						[1] = new Awards("wood", "Wood", 1250, 0, "", "", false),
						[2] = new Awards("charcoal", "Charcoal", 1500, 0, "", "", false),
						[3] = new Awards("metal.ore", "Metal ore", 1000, 0, "", "", false),
						[4] = new Awards("metal.fragments", "Metal fragments", 750, 0, "", "", false),
						[5] = new Awards("sulfur.ore", "Sulfur ore", 500, 0, "", "", false),
						[6] = new Awards("sulfur", "Sulfur", 300, 0, "", "", false),
						[7] = new Awards("gunpowder", "Gunpowder", 400, 0, "", "", false),
						[8] = new Awards("hq.metal.ore", "HQM ore", 25, 0, "", "", false), 
						[9] = new Awards("metal.refined", "HQM", 20, 0, "", "", false),
						[10] = new Awards("scrap", "Scrap", 50, 0, "", "", false)
					}
				};
			}
			
			internal class Crates
			{ 
				[JsonProperty(LanguageEnglish ? "Name crate/barrel" : "Имя ящика/бочки")] public string NameCrate;				
				[JsonProperty(LanguageEnglish ? "Drop chance" : "Шанс выпадения")] public float ChanceDrop;				
				[JsonProperty(LanguageEnglish ? "Minimum amount of coupons" : "Минимальное количество купона")] public int CouponMin;				
				[JsonProperty(LanguageEnglish ? "Maximum number of coupons" : "Максимальное количество купона")] public int CouponMax;	

                public Crates(string namecrate, float chancedrop, int cmin, int cmax)
				{
					NameCrate = namecrate; ChanceDrop = chancedrop; CouponMin = cmin; CouponMax = cmax;
				}				
			}	
			
			internal class LevelSetting
			{
				[JsonProperty(LanguageEnglish ? "Maximum level" : "Максимальный уровень")] public int LevelMax;				
				[JsonProperty(LanguageEnglish ? "Number of XP to upgrade one level" : "Кол-во ХР для повышения одного уровня")] public float LevelXP;									
				[JsonProperty(LanguageEnglish ? "How much to increase the number of XP with each level" : "На сколько увеличивать кол-во ХР с каждым уровнем")] public float LevelXPUP;									
			}
			[JsonProperty(LanguageEnglish ? "Mini-bar location / Main menu settings" : "Расположение мини-панели / Настройки главного меню")]
            public GUISetting GUI = new GUISetting();
		   		 		  						  	   		  	   		  	   		   		 		  		  
			internal class XPRateSetting
			{
				[JsonProperty(LanguageEnglish ? "Enable XP multiplier when exchanging coupons - [ This parameter affects only the multipliers for the exchange of coupons ]" : "Включить множитель XP при обмене купонов - [ Данный параметр влияет только на множители для обмена купонов ]")] public bool XPRateCoupon;
				[JsonProperty(LanguageEnglish ? "Setting up permissions for XP multipliers for the exchange of coupons and other actions. [ Permission | XP multiplier ]" : "Настройка пермишенов для умножителей ХР на обмен купонов и других действий. [ Пермишен | Множитель XP ]")] public Dictionary<string, float> XPRatePermisssion = new Dictionary<string, float>();
			}			

			internal class Awards
			{
				[JsonProperty(LanguageEnglish ? "Item shortname / custom reward name [ Must not be empty ]" : "Шортнейм предмета / имя кастомной награды [ Не должно быть пустым ]")] public string Shortname;
                [JsonProperty(LanguageEnglish ? "Reward display name" : "Отображаемое имя награды")] public string Name;
                [JsonProperty(LanguageEnglish ? "Item quantity" : "Количество предмета")] public int Amount;
				[JsonProperty(LanguageEnglish ? "Item skin" : "Скин предмета")] public ulong SkinID;
				[JsonProperty(LanguageEnglish ? "Command" : "Команда")] public string Command;
				[JsonProperty(LanguageEnglish ? "Link to custom image" : "Ссылка на кастомную картинку")] public string URLImage;
				[JsonProperty(LanguageEnglish ? "Hide reward - [ Reward will not be displayed until the player reaches this level ]" : "Скрыть награду - [ Награда не будет отображаться пока игрок не достигнет данного уровня ]")] public bool Hide;
		   		 		  						  	   		  	   		  	   		   		 		  		  
                public Awards(string shortname, string name, int amount, ulong skinid, string command, string urlimage, bool hide)
                {
                    Shortname = shortname; Name = name; Amount = amount; SkinID = skinid; Command = command; URLImage = urlimage; Hide = hide;
                }
			}			
        }
		
		private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
		{
			if(!config.Setting.KillValide) return;
			if(entity == null) return;
			
			BasePlayer player = info?.InitiatorPlayer;
			
			if(player == null || player.IsNpc) return;
			
			if(config.XP.KillXP.ContainsKey(entity.ShortPrefabName))
				XPRateRerm(player, config.XP.KillXP[entity.ShortPrefabName]);
		}

        		
				
		private int API_GetLevel(BasePlayer player) => StoredData.ContainsKey(player.userID) ? StoredData[player.userID].Level : 0;
		private int API_GetLevel(ulong userID) => StoredData.ContainsKey(userID) ? StoredData[userID].Level : 0;
		
		private void RewardMenu(BasePlayer player, int Page = 0)
		{
            CuiElementContainer container = new CuiElementContainer();			
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 142.5", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, ".LevelO_Overlay", ".Pass", ".Pass");
			 
			int count = Page * 10;
			int x = 0, g = 0, z = 0 + count, zvip = 0 + count;
			var data = StoredData[player.userID];
			bool vipvalide = config.Setting.VIPValide;
			
			for(int i = 1 + count; i <= 10 + count; i++)
			{		
				bool icon = data.Level >= i, contains = config.Award.ContainsKey(i), containerreward = config.GUI.ContainerReward;
				
				if(!containerreward || containerreward && contains)
				{
					container.Add(new CuiPanel
					{
						RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-475 + (x * 95)} {(vipvalide ? -31.25 : -82.5)}", OffsetMax = $"{-380 + (x * 95)} {(vipvalide ? 63.75 : 12.5)}" },
						Image = { Color = "0 0 0 0" }
					}, ".Pass", ".Award");
		   		 		  						  	   		  	   		  	   		   		 		  		  
					container.Add(new CuiPanel
					{
						RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5" },
						Image = { Color = "0.41 0.42 0.40 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
					}, ".Award", ".Awards");

					container.Add(new CuiPanel
					{
						RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "45 -10", OffsetMax = "-45 -92.5" },
						Image = { Color = "0.41 0.42 0.40 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
					}, ".Award");
				
					if(contains)
					{
						var award = config.Award[i];
						
						if(award.Hide && !icon)
							container.Add(new CuiPanel
							{
								RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7.5 7.5", OffsetMax = "-7.5 -7.5" },
								Image = { Color = "0.31 0.32 0.30 0.8", Sprite = "assets/icons/blunt.png" },
							}, ".Awards");
						else
						{
							container.Add(new CuiElement
							{
								Parent = ".Awards",
								Components =
								{
									new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", award.Shortname + award.SkinID + (award.URLImage == String.Empty ? 150 : 151)) },
									new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7.5 7.5", OffsetMax = "-7.5 -7.5" }
								}
							});		
					
							container.Add(new CuiLabel
							{
								RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-2.5 0" },
								Text = { Text = award.Amount == 0 ? "" : $"x{award.Amount}", Align = TextAnchor.LowerRight, FontSize = 12, Color = "1 1 1 0.75" }
							}, ".Awards");
						}
					}
					
					if(icon)
						container.Add(new CuiElement
						{
							Parent = ".Awards",
							Components =
							{
								new CuiImageComponent { Color = "0.38 0.61 0.99 1", Sprite = "assets/icons/check.png" },
								new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "25 25", OffsetMax = "-25 -25" },
								new CuiOutlineComponent { Color = "0 0 0 1", Distance = "-1 1" }
							}
						});
					else if(config.GUI.RewardNumber)
						container.Add(new CuiElement
						{
							Parent = ".Awards",
							Components = 
							{
								new CuiTextComponent { Text = $"{i}", Align = TextAnchor.MiddleCenter, FontSize = 35, Color = "0.38 0.61 0.99 1" },
								new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
								new CuiOutlineComponent { Color = "0 0 0 1", Distance = "-1 1" }
							}
						});
				}
				
				x++;
				z++;
				
				if(z == config.Level.LevelMax)
					break;
			}

			bool permvip = permission.UserHasPermission(player.UserIDString, permVip);
			
			if(vipvalide)
				for(int i = 1 + count; i <= 10 + count; i++)
				{		
					bool icon = data.Level >= i, contains = config.AwardVip.ContainsKey(i), containerreward = config.GUI.ContainerReward;
					
					if(!containerreward || containerreward && contains)
					{
						container.Add(new CuiPanel
						{
							RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-475 + (g * 95)} -166.25", OffsetMax = $"{-380 + (g * 95)} -71.25" },
							Image = { Color = "0 0 0 0" }
						}, ".Pass", ".Award");
		   		 		  						  	   		  	   		  	   		   		 		  		  
						container.Add(new CuiPanel
						{
							RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5" },
							Image = { Color = permvip ? "0.41 0.42 0.40 0.95" : "0.41 0.42 0.40 0.25", Material = "assets/content/ui/uibackgroundblur.mat" }
						}, ".Award", ".Awards");
		   		 		  						  	   		  	   		  	   		   		 		  		  
						container.Add(new CuiPanel
						{
							RectTransform = { AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-2.5 -2.5", OffsetMax = "2.5 10" },
							Image = { Color = permvip ? "0.41 0.42 0.40 0.95" : "0.41 0.42 0.40 0.25", Material = "assets/content/ui/uibackgroundblur.mat" }
						}, ".Award");
				
						if(contains)
						{
							var awardvip = config.AwardVip[i];
							
							if(awardvip.Hide && !icon)
								container.Add(new CuiPanel
								{
									RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7.5 7.5", OffsetMax = "-7.5 -7.5" },
									Image = { Color = permvip ? "0.31 0.32 0.30 0.8" : "0.51 0.52 0.50 0.4", Sprite = "assets/icons/blunt.png" },
								}, ".Awards");
							else
							{
								container.Add(new CuiElement
								{
									Parent = ".Awards",
									Components =
									{
										new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", awardvip.Shortname + awardvip.SkinID + (awardvip.URLImage == String.Empty ? 150 : 151)), Color = permvip ? "1 1 1 1" : "1 1 1 0.5" },
										new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7.5 7.5", OffsetMax = "-7.5 -7.5" }
									}
								});		
				
								container.Add(new CuiLabel
								{
									RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-2.5 0" },
									Text = { Text = awardvip.Amount == 0 ? "" : $"x{awardvip.Amount}", Align = TextAnchor.LowerRight, FontSize = 12, Color = permvip ? "1 1 1 0.75" : "1 1 1 0.37" }
								}, ".Awards");
							}
						}
						
						if(icon)
							container.Add(new CuiElement
							{
								Parent = ".Awards",
								Components =
								{
									new CuiImageComponent { Color = "0.38 0.61 0.99 1", Sprite = "assets/icons/check.png" },
									new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "25 25", OffsetMax = "-25 -25" },
									new CuiOutlineComponent { Color = "0 0 0 1", Distance = "-1 1" }
								}
							});
						else if(config.GUI.RewardNumberVIP)
							container.Add(new CuiElement
							{
								Parent = ".Awards",
								Components =
								{
									new CuiTextComponent { Text = $"{i}", Align = TextAnchor.MiddleCenter, FontSize = 35, Color = "0.38 0.61 0.99 1" },
									new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
									new CuiOutlineComponent { Color = "0 0 0 1", Distance = "-1 1" }
								}
							});
					}
					
					g++;
					zvip++;
				
					if(zvip == config.Level.LevelMax)
						break;
				}
			 
			bool back = Page != 0;
			bool next = config.Level.LevelMax > ((Page + 1) * 10);
			
			container.Add(new CuiButton 
            {    
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-190 -137.5", OffsetMax = "-100 -112.5" },
                Button = { Color = back ? "0.65 0.29 0.24 1" : "0.65 0.29 0.24 0.4", Command = back ? $"level page_lvl back {Page}" : "" },
                Text = { Text = lang.GetMessage("BACK", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = back ? "0.92 0.79 0.76 1" : "0.92 0.79 0.76 0.4" }
            }, ".Pass");				 			
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-95 -137.5", OffsetMax = "-5 -112.5" },
                Button = { Color = next ? "0.35 0.45 0.25 1" : "0.35 0.45 0.25 0.4", Command = next ? $"level page_lvl next {Page}" : "" },
                Text = { Text = lang.GetMessage("NEXT", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = next ? "0.75 0.95 0.41 1" : "0.75 0.95 0.41 0.4" }
            }, ".Pass");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-522.5 {(vipvalide ? -61.25 : -112.5)}", OffsetMax = $"522.5 {(vipvalide ? -41.25 : -92.5)}" },
                Image = { Color = "0.5 0.5 0.5 0.45" }
            }, ".Pass", ".Pass_Level");
			
			float y = (data.Level - (Page * 10.0f)) / 10.0f;
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = y >= 1 ? "1045 0" : y < 0 ? "0 0" : $"{95 * (data.Level % 10)} 0" },
                Image = { Color = "0.5 0.5 0.5 0.9" }
            }, ".Pass_Level");
			 
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = string.Format(lang.GetMessage(data.Level >= config.Level.LevelMax ? "Level_2" : "Level", this, player.UserIDString), data.Level, config.Level.LevelMax, data.XP, data.Level * config.Level.LevelXPUP + config.Level.LevelXP, config.Setting.RankList.Count == 0 ? String.Empty : string.Format(lang.GetMessage("RANK", this, player.UserIDString), config.Setting.RankList.ContainsKey(data.Level) ? config.Setting.RankList[data.Level] : "∞")), Align = TextAnchor.MiddleCenter, FontSize = 13, Color = "1 1 1 0.75" }
            }, ".Pass_Level");
			
			CuiHelper.AddUi(player, container);
		}
		
		private void SaveData(BasePlayer player) => Interface.Oxide.DataFileSystem.WriteObject($"XDataSystem/XLevels/InvItems/{player.userID}", StoredDataInv[player.userID]);
		
		private void LootSpawn(LootContainer lootContainer)
		{
			var cfg = config.Coupon; 
			
			for(int i = 0; i < cfg.Count; i++)
				for(int j = 0; j < cfg[i].Crate.Count; j++)
				{
					var crate = cfg[i].Crate[j];
					
					if(crate.NameCrate == lootContainer.ShortPrefabName)
					    if(UnityEngine.Random.Range(0, 100) <= crate.ChanceDrop)
                        {
                            Item item = ItemManager.CreateByItemID(1414245162, UnityEngine.Random.Range(crate.CouponMin, crate.CouponMax), cfg[i].SkinID);
                            item.name = cfg[i].Name;
                            item.text = cfg[i].Text;
								
                            item.MoveToContainer(lootContainer.inventory);
                        }
				}
		}
		
		[ConsoleCommand("level_give_xp")]
		private void ccmdGiveXP(ConsoleSystem.Arg arg)
		{
			BasePlayer player = arg.Player();
			
			if(player == null || player.IsAdmin)
			{
				ulong steamID = Convert.ToUInt64(arg.Args[0]);
				BasePlayer rplayer = BasePlayer.FindByID(steamID);
				float xp = Convert.ToSingle(arg.Args[1]);
				
				if(rplayer != null)
					AddData(rplayer, xp);
				else
					PrintWarning(LanguageEnglish ? $"Error give XP to the player - [ {steamID} ] - maybe it's offline!" : $"Ошибка выдачи XP игроку - [ {steamID} ] - возможно он оффлайн!");
			}
		}
		
		private void InventoryRewardMenu(BasePlayer player, int Page = 0)
		{
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 40", OffsetMax = "0 0" },
                Image = { Color = "0.21 0.22 0.20 1" }
            }, ".LevelO_Overlay", ".Inventory_Items", ".Inventory_Items");
			
			if(StoredDataInv[player.userID].Count == 0)
			    container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage("InventoryEmpty", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 20, Color = "1 1 1 0.4" }
                }, ".Inventory_Items");
			
			int x = 0, y = 0, z = 0;
			bool permvip = permission.UserHasPermission(player.UserIDString, permVip);
			
            foreach(var inv in StoredDataInv[player.userID].Skip(Page * 39))
			{
				bool rewardlock = inv.IsVIP && config.Setting.TakeReward && !permvip;
				
				container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-522 + (x * 80.75)} {40 - (y * 77.5)}", OffsetMax = $"{-447 + (x * 80.75)} {115 - (y * 77.5)}" },
                    Image = { Color = rewardlock ? "0.41 0.42 0.40 0.47" : "0.41 0.42 0.40 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
                }, ".Inventory_Items", ".Invitems");
				
				if(rewardlock)
					container.Add(new CuiPanel
					{
						RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-15 -15", OffsetMax = "-2.5 -2.5" },
						Image = { Color = "0.75 0.75 0.75 1", Sprite = "assets/icons/bp-lock.png" }
					}, ".Invitems");
				
				container.Add(new CuiElement
                {
                    Parent = ".Invitems",
                    Components =
                    {
					    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", inv.Shortname + inv.SkinID + (inv.URLImage == String.Empty ? 150 : 151)), Color = rewardlock ? "1 1 1 0.4" : "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7.5 7.5", OffsetMax = "-7.5 -7.5" }
                    }
                });
				
				container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-2.5 0" },
                    Text = { Text = inv.Amount == 0 ? "" : $"x{inv.Amount}", Align = TextAnchor.LowerRight, FontSize = 12, Color = rewardlock ? "1 1 1 0.15" : "1 1 1 0.75" }
                }, ".Invitems");
				
				if(!rewardlock)
				{
					container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
						Button = { Color = "0 0 0 0", Command = $"level_take {z + (Page * 39)} {Page}" },
						Text = { Text = "" }
					}, ".Invitems", ".InvitemsTake");
				
					container.Add(new CuiElement
					{
						Parent = ".InvitemsTake",
						Components =
						{
							new CuiTextComponent { Text = lang.GetMessage("Take", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 12, Color = "0.38 0.61 0.99 1" },
							new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
							new CuiOutlineComponent { Color = rewardlock ? "0 0 0 0.4" : "0 0 0 1", Distance = "-1 1" }
						}
					});
				}					
				
				x++;
				z++;
				 
				if(x == 13)
				{
					x = 0;
					y++;
					
					if(y == 3)
						break;
				}
			}
			
			bool back = Page != 0;
			bool next = StoredDataInv[player.userID].Count > ((Page + 1) * 39);
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-195 -40", OffsetMax = "-1 -5" },
                Image = { Color = "0.21 0.22 0.20 1" }
            }, ".Inventory_Items", ".BB");
			
			container.Add(new CuiButton 
            {   
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-93 -12.5", OffsetMax = "-3 12.5" },
                Button = { Color = back ? "0.65 0.29 0.24 1" : "0.65 0.29 0.24 0.4", Command = back ? $"level page_inv back {Page}" : "" },
                Text = { Text = lang.GetMessage("BACK", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = back ? "0.92 0.79 0.76 1" : "0.92 0.79 0.76 0.4" }
            }, ".BB");				 			
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "2.5 -12.5", OffsetMax = "92.5 12.5" },
                Button = { Color = next ? "0.35 0.45 0.25 1" : "0.35 0.45 0.25 0.4", Command = next ? $"level page_inv next {Page}" : "" },
                Text = { Text = lang.GetMessage("NEXT", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = next ? "0.75 0.95 0.41 1" : "0.75 0.95 0.41 0.4" }
            }, ".BB");
			
			container.Add(new CuiButton 
            {    
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "5 -35", OffsetMax = "195 -10" },
                Button = { Color = "0.65 0.29 0.24 1", Close = ".Inventory_Items" },
                Text = { Text = lang.GetMessage("CLOSEINV", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf", Color = "0.92 0.79 0.76 1" }
            }, ".Inventory_Items");
			
			CuiHelper.AddUi(player, container);
		}
		
		private void OnLootEntity(BasePlayer player, LootContainer container, Item item)
		{
			if(!config.Setting.CrateValide) return;
			if(container.OwnerID != 0 || player == null) return;
				
			if(config.XP.CrateXP.ContainsKey(container.ShortPrefabName))
				XPRateRerm(player, config.XP.CrateXP[container.ShortPrefabName]);
			
			container.OwnerID = player.userID;
		} 
		
				
	    
        private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LevelTitle"] = "COOL SERVER LEVEL MENU",
                ["Level"] = "LEVEL: {0}/{1}   |   XP: {2}/{3}{4}",
                ["Level_2"] = "LEVEL: {0}/{1}   |   XP: {2}{4}",
				["Level_Top"] = "{0}.     LEVEL: {1}   |   XP: {2}\n{3}",
                ["LevelGUI"] = "LEVEL: {0}",
                ["Exchange"] = "EXCHANGE VALUES",
                ["ExchangeTrue"] = "You have successfully exchanged coupons for - {0} XP.",
                ["InventaryInfo"] = "You have {0} XP coupons in your inventory!",
                ["Info"] = "Level up and get rewards!",
                ["Take"] = "TAKE",
                ["TakeItem"] = "You have successfully received - {0} [{1} pcs].",
				["LevelUP"] = "<color=#00FF00>[XLevels]</color> : Level up!\n<size=12>Your level: <color=orange>{0}</color></size>",
				["XP"] = "On you {0}XP",
                ["InventoryEmpty"] = "YOUR INVENTORY IS EMPTY!",
				["CLOSEINV"] = "CLOSE INVENTORY",
				["OPENINV"] = "OPEN INVENTORY",
				["NEXT"] = "NEXT",
				["BACK"] = "BACK",
				["VIPYES"] = "VIP ACTIVE",
				["VIPNO"] = "VIP INACTIVE",
				["BUTTON"] = "OPEN MENU",
				["RANK"] = "   |   RANK: {0}"
            }, this);
			
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LevelTitle"] = "МЕНЮ УРОВНЕЙ КРУТОГО СЕРВЕРА",
                ["Level"] = "УРОВЕНЬ: {0}/{1}   |   XP: {2}/{3}{4}",
                ["Level_2"] = "УРОВЕНЬ: {0}/{1}   |   XP: {2}{4}",
                ["Level_Top"] = "{0}.     УРОВЕНЬ: {1}   |   XP: {2}\n{3}",
                ["LevelGUI"] = "УРОВЕНЬ: {0}",
                ["Exchange"] = "ОБМЕНЯТЬ КУПОНЫ",
                ["ExchangeTrue"] = "Вы успешно обменяли купоны на - {0} XP.",
                ["InventaryInfo"] = "В вашем инвентаре купонов на {0} XP!",
                ["Info"] = "Повышайте уровень и получайте награду!",
                ["Take"] = "ЗАБРАТЬ",
                ["TakeItem"] = "Вы успешно получили - {0} [ {1} шт ].",
                ["LevelUP"] = "<color=#00FF00>[XLevels]</color> : Уровень повышен!\n<size=12>Ваш уровень: <color=orange>{0}</color></size>",
                ["XP"] = "У вас на {0}XP",
                ["InventoryEmpty"] = "ВАШ ИНВЕНТАРЬ ПУСТ!",
				["CLOSEINV"] = "ЗАКРЫТЬ ИНВЕНТАРЬ",
				["OPENINV"] = "ОТКРЫТЬ ИНВЕНТАРЬ",
				["NEXT"] = "ДАЛЕЕ",
				["BACK"] = "НАЗАД",
				["VIPYES"] = "ВИП АКТИВЕН",
				["VIPNO"] = "ВИП НЕАКТИВЕН",
				["BUTTON"] = "ОТКРЫТЬ МЕНЮ",
				["RANK"] = "   |   РАНГ: {0}"
            }, this, "ru");
			
			lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LevelTitle"] = "МЕНЮ РІВНІВ КРУТОГО СЕРВЕРУ",
                ["Level"] = "РІВЕНЬ: {0}/{1}   |   XP: {2}/{3}{4}",
                ["Level_2"] = "РІВЕНЬ: {0}/{1}   |   XP: {2}{4}",
                ["Level_Top"] = "{0}.     РІВЕНЬ: {1}   |   XP: {2}\n{3}",
                ["LevelGUI"] = "РІВЕНЬ: {0}",
                ["Exchange"] = "ОБМІНЯТИ КУПОНИ",
                ["ExchangeTrue"] = "Ви успішно обміняли купони на - {0} XP.",
                ["InventaryInfo"] = "У вашому інвентарі купонів на {0} XP!",
                ["Info"] = "Підвищуйте рівень та отримуйте нагороду!",
                ["Take"] = "ЗАБРАТИ",
                ["TakeItem"] = "Ви успішно отримали - {0} [ {1} шт ].",
                ["LevelUP"] = "<color=#00FF00>[XLevels]</color> : Рівень підвищено!\n<size=12>Ваш рівень: <color=orange>{0}</color></size>",
                ["XP"] = "У вас на {0}XP",
                ["InventoryEmpty"] = "ВАШ ІНВЕНТАР ПОРОЖНІЙ!",
				["CLOSEINV"] = "ЗАКРИТИ ІНВЕНТАР",
				["OPENINV"] = "ВІДКРИТИ ІНВЕНТАР",
				["NEXT"] = "ДАЛІ",
				["BACK"] = "НАЗАД",
				["VIPYES"] = "ВІП АКТИВНИЙ",
				["VIPNO"] = "ВІП НЕАКТИВНИЙ",
				["BUTTON"] = "ВІДКРИТИ МЕНЮ",
				["RANK"] = "   |   РАНГ: {0}"
            }, this, "uk");
			
			lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LevelTitle"] = "MENÚ DE NIVEL DE SERVIDOR FRESCO",
                ["Level"] = "NIVEL: {0}/{1}   |   XP: {2}/{3}{4}",
                ["Level_2"] = "NIVEL: {0}/{1}   |   XP: {2}{4}",
				["Level_Top"] = "{0}.     NIVEL: {1}   |   XP: {2}\n{3}",
                ["LevelGUI"] = "NIVEL: {0}",
                ["Exchange"] = "VALORES DE CAMBIO",
                ["ExchangeTrue"] = "Has canjeado con éxito cupones por - {0} XP.",
                ["InventaryInfo"] = "¡Tienes {0} cupones de XP en tu inventario!",
                ["Info"] = "¡Sube de nivel y obtén recompensas!",
                ["Take"] = "LLEVAR",
                ["TakeItem"] = "Ha recibido con éxito - {0} [{1} piezas].",
				["LevelUP"] = "<color=#00FF00>[XLevels]</color> : ¡Elevar a mismo nivel!\n<size=12>Tu nivel: <color=orange>{0}</color></size>",
				["XP"] = "En ti {0}XP",
                ["InventoryEmpty"] = "¡TU INVENTARIO ESTÁ VACÍO!",
				["CLOSEINV"] = "CERRAR INVENTARIO",
				["OPENINV"] = "ABRIR EL INVENTARIO",
				["NEXT"] = "PRÓXIMO",
				["BACK"] = "ATRÁS",
				["VIPYES"] = "VIP ACTIVO",
				["VIPNO"] = "VIP INACTIVO",
				["BUTTON"] = "MENÚ ABIERTO",
				["RANK"] = "   |   RANGO: {0}"
            }, this, "es-ES");
        }
		
		private const string permVip = "xlevels.vip";
		
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if(StoredData.ContainsKey(player.userID))
                SaveData(player);
        }
		
		private void GUIOpen(BasePlayer player)
		{
			if(config.Vending.VendingUse && player.inventory.loot.entitySource is NPCVendingMachine)
			{
				NPCVendingMachine machine = player.inventory.loot.entitySource.GetComponent<NPCVendingMachine>();
				
				if(config.Vending.ListNPCVending.Contains(machine.shopName))
					RewardMenuFon(player);
			}
			else if(!config.Vending.VendingUse)
				RewardMenuFon(player);
		}
		
		private void Top(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".ExchangeInfo");
            CuiHelper.DestroyUi(player, ".Info");
			CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0.21 0.22 0.20 1", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".LevelO_Overlay", ".Top", ".Top");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = ".Top" },
                Text = { Text = "" }
            }, ".Top");
			
			int x = 0, y = 0, z = 1;
			
			foreach(var i in StoredData.OrderByDescending(h => h.Value.XP).OrderByDescending(h => h.Value.Level).Take(12))
			{
				var rplayer = covalence.Players.FindPlayerById(i.Key.ToString());
				
				container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-522 + (x * 350)} {85 - (y * 55)}", OffsetMax = $"{-177 + (x * 350)} {135 - (y * 55)}" },
                    Image = { Color = "0.31 0.32 0.30 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
                }, ".Top", ".TopList");
				
				container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = string.Format(lang.GetMessage("Level_Top", this, player.UserIDString), z, i.Value.Level, i.Value.XP, rplayer.Name), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "1 1 1 0.75" }
                }, ".TopList");
				
				container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-170 -22.5", OffsetMax = "-125 22.5" },
                    Image = { Color = "0.41 0.42 0.40 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
                }, ".TopList", ".TopAvatar");				
				
				container.Add(new CuiElement
                {
                    Parent = ".TopAvatar",
                    Components =
                    {
					    new CuiRawImageComponent { FadeIn = 0.5f, Png = (string) ImageLibrary.Call("GetImage", rplayer.Id) },
                        new CuiRectTransformComponent { AnchorMin = "0 0",  AnchorMax = "1 1", OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5" }
                    }
                });
				
				y++;
				z++;
					
				if(y == 4)
				{
					y = 0;
					x++;
				}
			}
			
			CuiHelper.AddUi(player, container);
		}
		 
	    private void Unload()
		{
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, ".Panel_GUI");
				CuiHelper.DestroyUi(player, ".Level_Overlay");
				SaveData(player);
			}
			
			if(StoredData != null) Interface.Oxide.DataFileSystem.WriteObject("XDataSystem/XLevels/XLevels", StoredData);
			config = null;
		} 
		
		private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
		{
			string prefix = GetPlayerPrefix(player.userID);
			
			if(String.IsNullOrEmpty(prefix)) return null;
			
			if(channel == ConVar.Chat.ChatChannel.Team)
				return null;
			else
			{
				PrintToChat($"{prefix} | " + $"<color=#538fef>{player.displayName}</color>: " + message);
			    return true;
			}
		}
		
		private void OnLootSpawn(LootContainer lootContainer)
		{
			if(config.Setting.CouponsValide)
				if(config.Setting.PluginCLoot)
					NextTick(() => LootSpawn(lootContainer));
				else
					LootSpawn(lootContainer);
		}
		
				
				
		private void OnOpenVendingShop(NPCVendingMachine machine, BasePlayer player)
		{
			if(config.Vending.VendingUse && config.Vending.ListNPCVending.Contains(machine.shopName))
				if(config.Vending.VendingOpen)
					GUIOpen(player);
				else
					Button(player);
		}
		
				
				
		private class XLevelsData
        {
			[JsonProperty(LanguageEnglish ? "Level" : "Уровень")] public int Level = 0;
			[JsonProperty(LanguageEnglish ? "XP" : "ХР")] public float XP = 0;
        }
		private const string permTop = "xlevels.top";
		
		private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
			if(!config.Setting.BonusValide) return;
			if(dispenser == null || item == null || player == null) return;
			
			if(config.XP.GatherBonusXP.ContainsKey(item.info.shortname))
				XPRateRerm(player, config.XP.GatherBonusXP[item.info.shortname]);
		}
		
		private void DataMove(BasePlayer player, int number)
		{			
			if(player == null) return;
			 
			if(config.Award.ContainsKey(number))
			{
				var Award = config.Award[number];
				InvItems Inventory = new InvItems(Award.Shortname, Award.Name, Award.Amount, Award.SkinID, Award.Command, Award.URLImage, false);

				StoredDataInv[player.userID].Add(Inventory);
			}
		   		 		  						  	   		  	   		  	   		   		 		  		  
			bool contains = config.AwardVip.ContainsKey(number);
			
			if(config.Setting.VIPValide)
				if(contains && config.Setting.MoveReward && permission.UserHasPermission(player.UserIDString, permVip) || contains && !config.Setting.MoveReward)
				{
					var AwardVip = config.AwardVip[number];
					InvItems Inventory = new InvItems(AwardVip.Shortname, AwardVip.Name, AwardVip.Amount, AwardVip.SkinID, AwardVip.Command, AwardVip.URLImage, true);

					StoredDataInv[player.userID].Add(Inventory);
				}
		}
		
		protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<LevelConfig>();
			}
			catch
			{
				PrintWarning(LanguageEnglish ? "Configuration read error! Creating a default configuration!" : "Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		
		private class InvItems
        {
		    [JsonProperty(LanguageEnglish ? "Item shortname" : "Шортнейм предмета")] public string Shortname;
            [JsonProperty(LanguageEnglish ? "Award display name" : "Отображаемое имя награды")] public string Name;
            [JsonProperty(LanguageEnglish ? "Item quantity" : "Количество предмета")] public int Amount;
			[JsonProperty(LanguageEnglish ? "Item skin" : "Скин предмета")] public ulong SkinID;
			[JsonProperty(LanguageEnglish ? "Command" : "Команда")] public string Command;
			[JsonProperty(LanguageEnglish ? "Link to custom image" : "Своя картинка")] public string URLImage;
			[JsonProperty(LanguageEnglish ? "VIP reward" : "Вип награда")] public bool IsVIP;
		   		 		  						  	   		  	   		  	   		   		 		  		  
            public InvItems(string shortname, string name, int amount, ulong skinid, string command, string urlimage, bool isvip)
			{
				Shortname = shortname; Name = name; Amount = amount; SkinID = skinid; Command = command; URLImage = urlimage; IsVIP = isvip;
			}
        }
		
		private void API_GiveXP(BasePlayer player, float XPAmount)
		{
			if(StoredData.ContainsKey(player.userID))
				AddData(player, XPAmount);
		}
		
		private void OGiveXP(BasePlayer player)
		{
			foreach(var perm in config.Online.Permisssion)
                if(permission.UserHasPermission(player.UserIDString, perm.Key))
				{
					AddData(player, perm.Value);
					break;
				}
		}
		
				
				
        private LevelConfig config;
		private Dictionary<ulong, List<InvItems>> StoredDataInv = new Dictionary<ulong, List<InvItems>>();
		
				
		[PluginReference] private Plugin ImageLibrary, StackSizeController, BetterChat;
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if(player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
			
			if(!StoredData.ContainsKey(player.userID))
				StoredData.Add(player.userID, new XLevelsData());		
			
			if(config.GUI.EnablePanel) GUI(player);
			LoadData(player);
		} 
		
		private object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
		{
            for(int i = 0; i < config.Coupon.Count; i++)
			{
			    if(item.GetItem().skin == config.Coupon[i].SkinID)
				    if(!targetItem.GetItem().skin.Equals(item.GetItem().skin)) return false;
				
                if(targetItem.GetItem().skin == config.Coupon[i].SkinID)
				    if(!item.GetItem().skin.Equals(targetItem.GetItem().skin)) return false; 
			}

            return null;
		}
		
		private void ChatMessage(BasePlayer player, string message) => Player.Reply(player, message, config.Setting.SteamID);
		
				
	    		
		private Item OnItemSplit(Item item, int amount)
        {
			if(StackSizeController) return null;
			
            for(int i = 0; i < config.Coupon.Count; i++)
			    if(item.skin == config.Coupon[i].SkinID)
                {
                    item.amount -= amount;
				
                    var Item = ItemManager.Create(item.info, amount, item.skin);
					
                    Item.name = item.name;
                    Item.skin = item.skin;
                    Item.text = item.text;
                    Item.amount = amount;
		            item.MarkDirty();
				
                    return Item;
                }
			
            return null;
        }
		
				
				
		private void cmdMenuOpen(BasePlayer player, string command, string[] args) => GUIOpen(player);
		
		private void DataTake(BasePlayer player, int number, int page)
		{
			if(player == null) return;
			
			var InventoryItem = StoredDataInv[player.userID][number];
			
			if(InventoryItem.IsVIP && config.Setting.TakeReward && !permission.UserHasPermission(player.UserIDString, permVip)) return;
			
			if(InventoryItem.Command != String.Empty)
				Server.Command($"{InventoryItem.Command}".Replace("%STEAMID%", player.UserIDString));	
            else
			{
				Item item = ItemManager.CreateByName(InventoryItem.Shortname, InventoryItem.Amount, InventoryItem.SkinID);
				item.name = InventoryItem.Name;
				
				player.GiveItem(item);
			}
			
			StoredDataInv[player.userID].Remove(InventoryItem);
			EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/weapons/survey_charge/survey_charge_stick.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
			
			InventoryRewardMenu(player, page);
			
			if(config.Setting.ChatTakeMessages)
				ChatMessage(player, string.Format(lang.GetMessage("TakeItem", this, player.UserIDString), InventoryItem.Name, InventoryItem.Amount));
		}
		
		private void CouponExchange(BasePlayer player, int x)
		{
			if(player.inventory.FindItemsByItemID(1414245162).Count == 0) return;
			
			int level = StoredData[player.userID].Level;
			
			if(config.Setting.GiveXPCoupon && level >= config.Level.LevelMax || level < config.Level.LevelMax)
			{
				int xp = 0;
		   		 		  						  	   		  	   		  	   		   		 		  		  
				foreach(var item in player.inventory.FindItemsByItemID(1414245162))
						if(item.skin == config.Coupon[x].SkinID)
						{
							xp += config.Coupon[x].XP * item.amount;
							
							item.RemoveFromContainer();
						}
			
				CuiHelper.DestroyUi(player, ".Inventory_Items");
				
				xp = config.XPRate.XPRateCoupon ? (int)XPRatePermCoupon(player, xp) : xp;
				
				AddData(player, xp);
				RewardMenu(player);
				ExchangeInfo(player);
			
				if(config.Setting.ChatTakeMessages) ChatMessage(player, string.Format(lang.GetMessage("ExchangeTrue", this, player.UserIDString), xp));
			}
		}
		
		private void XPRateRerm(BasePlayer player, float xp)
		{
			foreach(var perm in config.XPRate.XPRatePermisssion)
				if(permission.UserHasPermission(player.UserIDString, perm.Key))
				{
					xp *= perm.Value;
					break;
				}
			
			AddData(player, xp);
		}
		protected override void LoadDefaultConfig() => config = LevelConfig.GetNewConfiguration();
		private const bool LanguageEnglish = false;
		
		private void OnGrowableGather(GrowableEntity plant, BasePlayer player)
		{
			if(!config.Setting.HarvestValide) return;
			if(plant == null || player == null) return;
				
			if(config.XP.HarvestXP.ContainsKey(plant.ShortPrefabName))
				XPRateRerm(player, config.XP.HarvestXP[plant.ShortPrefabName]);
		}
		 
				
	    		
		private void AddData(BasePlayer player, float XPData)
		{
			bool maxlevel = StoredData[player.userID].Level >= config.Level.LevelMax;
			
			if(config.Setting.GiveXP && maxlevel)
			{
				NextTick(() => {
					if(config.GUI.EnablePanel) GUI(player);
					Message(player, $"+ {XPData}");
				});
				
				StoredData[player.userID].XP += XPData;
				return;
			}
			else if(maxlevel) return;
			
			float xp2 = XPData;
			
			NextTick(() => {
				if(config.GUI.EnablePanel) GUI(player);
			    Message(player, $"+ {xp2}");
			});
			
			while(XPData > StoredData[player.userID].Level * config.Level.LevelXPUP + config.Level.LevelXP)
			{
				StoredData[player.userID].Level += 1;
				XPData -= StoredData[player.userID].Level * config.Level.LevelXPUP + config.Level.LevelXP;
				
				int level = StoredData[player.userID].Level;
				
				DataMove(player, level);
				 
				if(config.Setting.ChatLevelMessages) 
					ChatMessage(player, string.Format(lang.GetMessage("LevelUP", this, player.UserIDString), level));
				
                if(level >= config.Level.LevelMax)
				{
					if(config.Setting.ClearAll)
					{
						StoredData[player.userID].Level = 0;
						StoredData[player.userID].XP = 0;
					}
					
					if(!config.Setting.GiveXP) StoredData[player.userID].XP = 0;
					return;
				}
			}
			
			float xpUP = StoredData[player.userID].Level * config.Level.LevelXPUP + config.Level.LevelXP;
			
			if(XPData > 0) 
		    {
				StoredData[player.userID].XP += XPData;
				
				if(StoredData[player.userID].XP >= xpUP)
			    {
				    StoredData[player.userID].XP -= xpUP;
				    StoredData[player.userID].Level += 1;
		   		 		  						  	   		  	   		  	   		   		 		  		  
					int level = StoredData[player.userID].Level;
		   		 		  						  	   		  	   		  	   		   		 		  		  
                    DataMove(player, level);
		   		 		  						  	   		  	   		  	   		   		 		  		  
					if(config.Setting.ChatLevelMessages) 
						ChatMessage(player, string.Format(lang.GetMessage("LevelUP", this, player.UserIDString), level));
					
					if(level >= config.Level.LevelMax)
					{
						if(config.Setting.ClearAll)
						{
							StoredData[player.userID].Level = 0;
							StoredData[player.userID].XP = 0;
						}
						
						if(!config.Setting.GiveXP) StoredData[player.userID].XP = 0;
					}
			    }
	        }
		}
		
		private void LoadData(BasePlayer player)
		{
            var Inventory = Interface.Oxide.DataFileSystem.ReadObject<List<InvItems>>($"XDataSystem/XLevels/InvItems/{player.userID}");
            
            if(!StoredDataInv.ContainsKey(player.userID))
                StoredDataInv.Add(player.userID, new List<InvItems>());	

            StoredDataInv[player.userID] = Inventory ?? new List<InvItems>();
		}
		
		[ConsoleCommand("level_take")]
		private void ccmdTakeData(ConsoleSystem.Arg arg)
		{
			BasePlayer player = arg?.Player();
			
			if(player == null || StoredDataInv[player.userID].Count == 0) return;
					
		    DataTake(player, int.Parse(arg.Args[0]), int.Parse(arg.Args[1]));
		}
		
		private string GetPlayerPrefix(ulong userID)
		{
			string prefix = String.Empty;
			
			if(StoredData.ContainsKey(userID))
			{
				if(config.Setting.ChatPrefixLevel && config.Setting.ChatPrefixRank)
					prefix = $"[ <color=orange>{StoredData[userID].Level}</color> ] [ <color=orange>{(config.Setting.RankList.ContainsKey(StoredData[userID].Level) ? config.Setting.RankList[StoredData[userID].Level] : "∞")}</color> ]";
				else if(config.Setting.ChatPrefixLevel)
					prefix = $"[ <color=orange>{StoredData[userID].Level}</color> ]";
				else if(config.Setting.ChatPrefixRank)
					prefix = $"[ <color=orange>{(config.Setting.RankList.ContainsKey(StoredData[userID].Level) ? config.Setting.RankList[StoredData[userID].Level] : "∞")}</color> ]";
			}
			
			return prefix;
		}
		
		private void GUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMax = "0 0"  },
                Image = { Color = "0 0 0 0" }
            }, "Hud", ".Panel_GUI", ".Panel_GUI");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = config.GUI.AnchorMin, AnchorMax = config.GUI.AnchorMax, OffsetMin = config.GUI.OffsetMin, OffsetMax = config.GUI.OffsetMax },
                Image = { Color = "0.96 0.91 0.87 0.02", Material = "assets/icons/greyout.mat" }
            }, ".Panel_GUI", ".GUIProgress");	
		   		 		  						  	   		  	   		  	   		   		 		  		  
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "4 5", OffsetMax = "-172 -5" },
                Button = { Color = "0.9 0.9 0.9 0.6", Sprite = "assets/icons/upgrade.png" },
                Text = { Text = "" }
            }, ".GUIProgress");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "25 3", OffsetMax = "-4 -3" },
                Image = { Color = "0 0 0 0" }
            }, ".GUIProgress", ".Progress");
			
			var data = StoredData[player.userID];
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = data.Level < config.Level.LevelMax ? $"{1.0 / (data.Level * config.Level.LevelXPUP + config.Level.LevelXP) * data.XP} 1" : "1 1", OffsetMax = "0 0" },
                Image = { FadeIn = 0.25f,  Color = "0.29 0.60 0.83 0.92", Material = "assets/icons/greyout.mat" }
            }, ".Progress");
			
		    container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "-7 0" },
                Text = { Text = string.Format(lang.GetMessage("LevelGUI", this, player.UserIDString), data.Level), Align = TextAnchor.MiddleRight, FontSize = 13, Color = "1 1 1 0.6" }
            }, ".Progress");
			
		    container.Add(new CuiLabel
		    {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "7 0", OffsetMax = "0 0" },
                Text = { Text = $"XP: {Math.Round(data.XP, 2)}", Align = TextAnchor.MiddleLeft, FontSize = 13, Color = "1 1 1 0.6" }
            }, ".Progress");
			
			CuiHelper.AddUi(player, container); 
		}
		
		private Dictionary<ulong, XLevelsData> StoredData = new Dictionary<ulong, XLevelsData>();
		private string API_GetPlayerPrefix(ulong userID) => GetPlayerPrefix(userID);
		
		private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
			if(!config.Setting.PickupValide) return;
			if(collectible == null || player == null) return;
			
			foreach(ItemAmount item in collectible.itemList)
				if(config.XP.PickupXP.ContainsKey(item.itemDef.shortname))
					XPRateRerm(player, config.XP.PickupXP[item.itemDef.shortname]);
		}
		
				
				
		private int CouponInfo(BasePlayer player, int x)
		{
			int xp = 0;
			
			foreach(var item in player.inventory.FindItemsByItemID(1414245162))
				    if(item.skin == config.Coupon[x].SkinID)
				    	xp += config.Coupon[x].XP * item.amount;
			
			return config.XPRate.XPRateCoupon ? (int)XPRatePermCoupon(player, xp) : xp;
		}
		
		private void ExchangeInfo(BasePlayer player)
        {
			CuiHelper.DestroyUi(player, ".Info");
			CuiHelper.DestroyUi(player, ".Top");
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0.21 0.22 0.20 1", Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".LevelO_Overlay", ".ExchangeInfo", ".ExchangeInfo");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = ".ExchangeInfo" },
                Text = { Text = "" }
            }, ".ExchangeInfo");
			
			int x = 0;
			int count = config.Coupon.Count, level = StoredData[player.userID].Level;
			
			foreach(var coupon in config.Coupon)
			{
				double offset = -(70 * count--) - (5.0 * count--);
				int xp = CouponInfo(player, x);
				bool activebutton = config.Setting.GiveXPCoupon && level >= config.Level.LevelMax && xp != 0 || level < config.Level.LevelMax && xp != 0;
					
				container.Add(new CuiPanel
                {
				    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{140 + offset - 140} -51", OffsetMax = $"{140 + offset} 89" },
                    Image = { Color = "0.41 0.42 0.40 0.95", Material = "assets/content/ui/uibackgroundblur.mat" }
                }, ".ExchangeInfo", ".Coupon");

				container.Add(new CuiElement
                {
                    Parent = ".Coupon",
                    Components =
                    {
					    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", coupon.SkinID.ToString()) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "22.5 22.5", OffsetMax = "-22.5 -22.5" }
                    }
                });	
		   		 		  						  	   		  	   		  	   		   		 		  		  
			    container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMax = "0 20" },
                    Text = { Text = string.Format(lang.GetMessage("XP", this, player.UserIDString), xp), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 11, Color = "1 1 1 0.5" }
                }, ".Coupon");				
 
			    container.Add(new CuiButton
                { 
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -37.5", OffsetMax = "0 -10" },
                    Button = { Color = activebutton ? "0.41 0.42 0.40 0.95" : "0.41 0.42 0.40 0.15", Material = "assets/icons/greyout.mat", Command = activebutton ? $"level exchange {x}" : "" },
                    Text = { Text = lang.GetMessage("Exchange", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 14, Color = activebutton ? "0.75 0.75 0.75 1" : "0.75 0.75 0.75 0.2" }
                }, ".Coupon");
 
                x++;				
			}

            CuiHelper.AddUi(player, container);
        }
		
				
				
		private void Button(BasePlayer player)
		{
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-447 16", OffsetMax = "-210 98" },
                Image = { Color = "0.51 0.52 0.50 0.95", Material = "assets/icons/greyout.mat" }
            }, "Overlay", ".Button", ".Button");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Button = { Color = "0.21 0.22 0.20 0.95", Material = "assets/icons/greyout.mat", Command = "x_levels" },
                Text = { Text = lang.GetMessage("BUTTON", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 22, Color = "1 1 1 0.25" }
            }, ".Button");
			
			CuiHelper.AddUi(player, container);
		}
		
		private float XPRatePermCoupon(BasePlayer player, float xp)
		{
			foreach(var perm in config.XPRate.XPRatePermisssion)
				if(permission.UserHasPermission(player.UserIDString, perm.Key))
					return xp * perm.Value;
			
			return xp;
		}
		
			}
}
