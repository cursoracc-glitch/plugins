using System;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using System.Linq;
		   		 		  						  	   		  	   		  	  			  		  		   		 
namespace Oxide.Plugins
{
    [Info("XMenu", "Monster", "1.1.12")]
    class XMenu : RustPlugin
    {
		protected override void LoadDefaultConfig() => config = MenuConfig.GetNewConfiguration();
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if(player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
			
			if(config.Setting.Connect)
			{
				GUIMenu(player);
				GUILogo(player, true);
					
				if(config.Setting.Reload)
					players.Add(player);
				
				if(config.ButtonAdd.Count >= 1)
					GUIAddButtonOpen(player);
			}
			else
			    GUILogo(player);
		}
		
				
				
		int FakeOnline => (int)IQFakeActive?.Call("GetOnline");
		
		private void GUIPluginsInfo(BasePlayer player)
		{
            CuiElementContainer container = new CuiElementContainer();			
			
			MenuConfig.MenuSetting menu = config.Menu;
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = menu.PAnchorMin, AnchorMax = menu.PAnchorMax, OffsetMin = menu.POffsetMin, OffsetMax = menu.POffsetMax },
                Image = { Color = "0 0 0 0" }
            }, ".MenuGUI", ".PluginsInfoGUI", ".PluginsInfoGUI");						

            int y = 0, count = config.PluginsInfo.Count; 
		   		 		  						  	   		  	   		  	  			  		  		   		 
            foreach(var plugininfo in config.PluginsInfo.Where(p => plugins.Find(p.PluginName)))
			{
				container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 {-18 - (y * 21)}", OffsetMax = $"0 {0 - (y * 21)}" },
                    Image = { Color = config.Menu.MenuColor, Material = config.Menu.MenuMaterial }
                }, ".PluginsInfoGUI", ".InfoText");
				
				if(plugininfo.Parameter == "player")
				    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { Text = string.Format(lang.GetMessage(plugininfo.PluginName, this, player.UserIDString), plugins.Find(plugininfo.PluginName).CallHook(plugininfo.HookName, player)), Align = TextAnchor.MiddleCenter, FontSize = 11 }
                    }, ".InfoText");
				else if(plugininfo.Parameter == "userID")
					container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Text = { Text = string.Format(lang.GetMessage(plugininfo.PluginName, this, player.UserIDString), plugins.Find(plugininfo.PluginName).CallHook(plugininfo.HookName, player.userID)), Align = TextAnchor.MiddleCenter, FontSize = 11 }
                    }, ".InfoText");
					
				y++;
			}			
			
			CuiHelper.AddUi(player, container);
		}
		
		void SyncReservedFinish()
        {
            PrintWarning(LanguageEnglish ? $"{Name} - successfully synced with IQFakeActive" : $"{Name} - успешно синхронизирована с IQFakeActive");
            PrintWarning("=============SYNC==================");
        }

        		
		public int maxplayers, online, joining, sleeping;
        protected override void SaveConfig() => Config.WriteObject(config);
		
				
		
        private MenuConfig config;
		
		private List<BaseNetworkable> _events = new List<BaseNetworkable>();
		private List<BasePlayer> players = new List<BasePlayer>();
		
		private void GUIAddButton(BasePlayer player)
		{
			CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -20", OffsetMax = "0 -2.5" },
                Button = { Color = config.Menu.MenuColor, Command = "ui_menu addbutton_close", Material = config.Menu.MenuMaterial },
                Text = { Text = "123" }
            }, ".LogoGUI", ".AddButton_Close", ".AddButton_Close");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 -1" },
                Text = { Text = lang.GetMessage("AddCLOSE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 10, Color = "1 1 1 0.805641" }
            }, ".AddButton_Close");
			
			int x = 0;
			
			foreach(var button in config.ButtonAdd)
			{
				float fadein = 0.5f + (x * 0.5f);
				
				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = $"0 {-22.5 - (x * 17.5)}", OffsetMax = $"0 {-7.5 - (x * 17.5)}" },
					Button = { FadeIn = fadein, Color = config.Menu.MenuColor, Command = $"ui_menu_b '{button.Value}'", Material = config.Menu.MenuMaterial },
					Text = { Text = "" } 
				}, ".AddButton_Close", ".AddButton_N");
				
			    container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 -1" },
                    Text = { FadeIn = fadein, Text = lang.GetMessage(button.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 10, Color = "1 1 1 0.805641" }
                }, ".AddButton_N");
				
				x++;
			}
			
			CuiHelper.AddUi(player, container);
		}
		
		private void GUIPlayerPos(BasePlayer player)
		{
			MenuConfig.MenuSetting menu = config.Menu;
			Vector3 playerPos = player.transform.position;
			
			CuiElementContainer container = new CuiElementContainer();
			
			if(config.Setting.AGrig)
			{
				container.Add(new CuiPanel
				{
					RectTransform = { AnchorMin = menu.GAnchorMin, AnchorMax = menu.GAnchorMax, OffsetMin = menu.GOffsetMin, OffsetMax = menu.GOffsetMax },
					Image = { Color = menu.MenuColor, Material = menu.MenuMaterial }
				}, ".MenuGUI", ".PlayerPosGrid", ".PlayerPosGrid");
				
				container.Add(new CuiLabel
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
					Text = { Text = PhoneController.PositionToGridCoord(playerPos), Align = TextAnchor.MiddleCenter, FontSize = 11 }
				}, ".PlayerPosGrid");
			}
			
			if(config.Setting.ACoordinates)
			{
				container.Add(new CuiPanel
				{
					RectTransform = { AnchorMin = menu.CAnchorMin, AnchorMax = menu.CAnchorMax, OffsetMin = menu.COffsetMin, OffsetMax = menu.COffsetMax },
					Image = { Color = menu.MenuColor, Material = menu.MenuMaterial }
				}, ".MenuGUI", ".PlayerPos", ".PlayerPos");
				
				container.Add(new CuiLabel
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
					Text = { Text = string.Format(lang.GetMessage("COORDINATES", this, player.UserIDString), playerPos), Align = TextAnchor.MiddleCenter, FontSize = 9 }
				}, ".PlayerPos");
			}
			
			CuiHelper.AddUi(player, container);
		}
		private const bool LanguageEnglish = false;
		
		[ConsoleCommand("ui_menu")]
		void cmdOpenGUI(ConsoleSystem.Arg args)
		{
			BasePlayer player = args?.Player();
			
			if(player == null) return;
			
			if(Cooldowns.ContainsKey(player))
				if(Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
			
			switch(args.Args[0])
			{
				case "open":
				{
					GUIMenu(player);
					GUILogo(player, true);
					
					if(config.ButtonAdd.Count >= 1)
						GUIAddButtonOpen(player);
					
					if(config.Setting.Reload && !players.Contains(player))
					    players.Add(player);
					
					break;
				}				 
				case "close":
				{
					DestroyUiMenu(player);
					
					break;
				}
				case "addbutton":
				{
					if(config.ButtonAdd.Count >= 1)
					{
						CuiHelper.DestroyUi(player, ".AddButton_Open");
						
						GUIAddButton(player);
					}
					
					break;
				}
				case "addbutton_close":
				{
					if(config.ButtonAdd.Count >= 1)
					{
						CuiHelper.DestroyUi(player, ".AddButton_Close");
						
						GUIAddButtonOpen(player);
					}
					
					break;
				}
			}
			
			EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
			Cooldowns[player] = DateTime.Now.AddSeconds(0.3f);
		}
		
		private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();
		   		 		  						  	   		  	   		  	  			  		  		   		 
        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<MenuConfig>(); 
			}
			catch
			{
				PrintWarning(LanguageEnglish ? "Configuration read error! Creating a default configuration!" : "Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		
		private void Update()
		{
			maxplayers = ConVar.Server.maxplayers;
			online = config.Setting.FakeOnline == 1 && IQFakeActive ? FakeOnline : config.Setting.FakeOnline == 2 && FGS ? BasePlayer.activePlayerList.Count + (int)FGS?.CallHook("getFakes") : BasePlayer.activePlayerList.Count;
			joining = ServerMgr.Instance.connectionQueue.Joining;
			sleeping = BasePlayer.sleepingPlayerList.Count;
			time = TOD_Sky.Instance.Cycle.DateTime.ToString(config.Setting.TimeFormat);
			
			timer.Once(config.Setting.IReload, () => Update());
		}
		
		private void Unload()
		{
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
			    CuiHelper.DestroyUi(player, ".LogoGUI");
			    CuiHelper.DestroyUi(player, ".MenuGUI");
			}
		}
		
				 
				
		private void GUILogo(BasePlayer player, bool open = false) 
		{
            CuiElementContainer container = new CuiElementContainer();
			
			MenuConfig.LogoSetting logo = config.Logo;
			
			bool activemission = player.HasActiveMission() && config.Menu.Mission;
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = logo.AnchorMin, AnchorMax = logo.AnchorMax, OffsetMin = activemission ? logo.MMOffsetMin : logo.OffsetMin, OffsetMax = activemission ? logo.MMOffsetMax : logo.OffsetMax },
                Image = { Png = (string) ImageLibrary.Call("GetImage", ".LogoIMG"), Color = logo.LogoColor, Material = logo.LogoMaterial }
            }, "Overlay", ".LogoGUI", ".LogoGUI");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = open ? "ui_menu close" : "ui_menu open" },
                Text = { Text = "" }
            }, ".LogoGUI");
			
			CuiHelper.AddUi(player, container);
		}
		
				
				
		[ChatCommand("uimenu")]
		void cmdShowHideMenu(BasePlayer player, string command, string[] args)
		{
			if(args != null && args.Length >= 1 && permission.UserHasPermission(player.UserIDString, "xmenu.usecmd"))
			{
				switch(args[0])
				{
					case "on":
					{
						GUILogo(player);
						
						break;
					}					
					case "off":
					{
						CuiHelper.DestroyUi(player, ".LogoGUI");
						
						DestroyUiMenu(player, false);
						
						break;
					}
				}
			}
		}
		
				
				
        private void InitializeLang()
        {
			Dictionary<string, string> langen = new Dictionary<string, string>
			{
				["TITLE"] = "MELONRUST X1000000\n<size=9> O {0}/{1} |+ {2} | s {3} | T {4}</size>",
				["AddOPEN"] = "MORE BUTTONS",
				["AddCLOSE"] = "CLOSE",
				["COORDINATES"] = "YOUR COORDINATES\n{0}"
			};			
			
			Dictionary<string, string> langru = new Dictionary<string, string>
			{
				["TITLE"] = "MELONRUST X1000000\n<size=9> O {0}/{1} |+ {2} | s {3} | T {4}</size>",
				["AddOPEN"] = "БОЛЬШЕ КНОПОК",
				["AddCLOSE"] = "ЗАКРЫТЬ",
				["COORDINATES"] = "ВАШИ КООРДИНАТЫ\n{0}"
			};			
			
			Dictionary<string, string> languk = new Dictionary<string, string>
			{
				["TITLE"] = "ЛАСКАВО ПРОСИМО НА MELONRUST\n<size=9> O {0}/{1} |+ {2} | s {3} | T {4}</size>",
				["AddOPEN"] = "БІЛЬШЕ КНОПОК",
				["AddCLOSE"] = "ЗАКРИТИ",
				["COORDINATES"] = "ВАШІ КООРДИНАТИ\n{0}"
			};			
			
			Dictionary<string, string> langes = new Dictionary<string, string>
			{
				["TITLE"] = "BIENVENIDOS A MELONrUST\n<size=9> O {0}/{1} |+ {2} | s {3} | T {4}</size>",
				["AddOPEN"] = "MÁS BOTONES",
				["AddCLOSE"] = "CERCA",
				["COORDINATES"] = "SUS COORDENADAS\n{0}"
			};
				
			foreach(var button in config.Button)
			{
				langen.Add(button.Key, "BUTTON");
				langru.Add(button.Key, "Репорт");
				languk.Add(button.Key, "КНОПКА");
				langes.Add(button.Key, "BOTÓN");
			}			
			
			foreach(var button in config.ButtonAdd)
			{
				langen.Add(button.Key, "BUTTON");
				langru.Add(button.Key, "КНОПКА");
				languk.Add(button.Key, "КНОПКА");
				langes.Add(button.Key, "BOTÓN");
			}
			
			foreach(var plugininfo in config.PluginsInfo)
			{
				langen.Add(plugininfo.PluginName, "BALANCE: {0}$");
				langru.Add(plugininfo.PluginName, "БАЛАНС: {0}$");
				languk.Add(plugininfo.PluginName, "БАЛАНС: {0}$");
				langes.Add(plugininfo.PluginName, "BALANCE: {0}$");
			}
			 
            lang.RegisterMessages(langen, this);
            lang.RegisterMessages(langru, this, "ru");
            lang.RegisterMessages(languk, this, "uk");
            lang.RegisterMessages(langes, this, "es-ES");
        }
		
		private void OnEntitySpawned(BaseNetworkable entity)
		{
			if(entity is CargoShip)
			{
				_activeevents["CargoShip"] = true;
				_events.Add(entity);
			}
			if(entity is CargoPlane)
			{
				_activeevents["CargoPlane"] = true;
				_events.Add(entity);
			}
			if(entity is BradleyAPC)
			{
				_activeevents["BradleyAPC"] = true;
				_events.Add(entity);
			}
			if(entity is PatrolHelicopter)
			{
				_activeevents["BaseHelicopter"] = true;
				_events.Add(entity);
			}
			if(entity is CH47Helicopter)
			{
				_activeevents["CH47Helicopter"] = true;
				_events.Add(entity);
			}
		}
		
				
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"     Config - v.5641\n" + 
			"-----------------------------");
			
			ImageLibrary.Call("AddImage", config.Logo.LogoURL, ".LogoIMG");
			
			int x = 0;
			
			foreach(var image in config.ButtonP)
			{
				if(!String.IsNullOrEmpty(image.LinkImageURL))
					ImageLibrary.Call("AddImage", image.LinkImageURL, ".Button_P" + x);
				
				x++;
			}
			
			foreach(var image in config.Event.Events)
			    ImageLibrary.Call("AddImage", image.Value.EventURL, $".{image.Key}");
			
			foreach(var entity in BaseNetworkable.serverEntities)
			{
				if(entity is CargoShip)
				{
					_activeevents["CargoShip"] = true;
					_events.Add(entity);
				}
				
				if(entity is CargoPlane)
				{
					_activeevents["CargoPlane"] = true;
					_events.Add(entity);
				}
				if(entity is BradleyAPC)
				{
					_activeevents["BradleyAPC"] = true;
					_events.Add(entity);
				}
				if(entity is PatrolHelicopter)
				{
					_activeevents["BaseHelicopter"] = true;
					_events.Add(entity);
				}
				if(entity is CH47Helicopter)
				{
					_activeevents["CH47Helicopter"] = true;
					_events.Add(entity);
				}
			}
			
			Update();
			BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
			
			if(config.Setting.Reload)
				timer.Every(config.Setting.IReload < 10 ? 10 : config.Setting.IReload, () =>
				{
					foreach(var i in players)
						GUIMenuInfo(i);
						
					if(config.Setting.AGrig || config.Setting.ACoordinates)
						timer.Once(1, () =>
						{
							foreach(var i in players)
								GUIPlayerPos(i);
						});
						
					if(config.Setting.AEvents)
						timer.Once(2.5f, () =>
						{
							foreach(var i in players)
								GUIEvent(i);
						});
						
					if(config.Setting.APIuginsInfo && config.Setting.ReloadPluginsInfo)
						timer.Once(4, () =>
						{
							foreach(var i in players)
								GUIPluginsInfo(i);
						});
				});
			
			permission.RegisterPermission("xmenu.usecmd", this);
			
			InitializeLang();
		}
		
				
		[PluginReference] private Plugin ImageLibrary, IQFakeActive, FGS;
		
		private void GUIMenu(BasePlayer player)
		{
            CuiElementContainer container = new CuiElementContainer();
			
			MenuConfig.MenuSetting menu = config.Menu;
			
			bool activemission = player.HasActiveMission() && menu.Mission;
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = menu.MAnchorMin, AnchorMax = menu.MAnchorMax, OffsetMin = activemission ? menu.MMOffsetMin : menu.MOffsetMin, OffsetMax = activemission ? menu.MMOffsetMax : menu.MOffsetMax },
                Image = { Color = menu.MenuColor, Material = menu.MenuMaterial }
            }, "Overlay", ".MenuGUI", ".MenuGUI");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "40 -55", OffsetMax = "42.5 -5" },
                Image = { Color = config.Menu.LineColor }
            }, ".MenuGUI");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "322.5 -55", OffsetMax = "325 -5" },
                Image = { Color = config.Menu.LineColor }
            }, ".MenuGUI");
			
			int count = config.Button.Count, count1 = config.ButtonP.Count;
			
			foreach(var i in config.Button)
			{ 
				double offset = -(26 * count--) - (1.25 * count--);
				
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offset + 5} -25", OffsetMax = $"{offset + 57} -10" },
                    Button = { Color = menu.ButtonColor, Command = $"ui_menu_b '{i.Value}'" },
                    Text = { Text = "" }
                }, ".MenuGUI", ".BUTTON");
				
			    container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage(i.Key, this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = menu.ButtonSize, Color = menu.ButtonTextColor }
                }, ".BUTTON");
			}
			
			int x = 0;
			
			foreach(var i in config.ButtonP)
			{
				double offset = (6.5 * count1--) + (2.5 * count1--);
				
				if(String.IsNullOrEmpty(i.LinkImageURL))
					container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"155 {offset - 14.25}", OffsetMax = $"170 {offset + 0.75}" },
						Button = { Color = "1 1 1 1", Sprite = i.LinkImageGame, Command = $"ui_menu_b '{i.Command}'" },
						Text = { Text = "" }
					}, ".MenuGUI");
				else
				{
					container.Add(new CuiElement
					{
						Parent = ".MenuGUI",
						Name = ".BUTTON_P",
						Components =
						{
							new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", ".Button_P" + x) },
							new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"155 {offset - 14.25}", OffsetMax = $"170 {offset + 0.75}" }
						}
					});
					
					container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
						Button = { Color = "0 0 0 0", Command = $"ui_menu_b '{i.Command}'" },
						Text = { Text = "" }
					}, ".BUTTON_P");
				}
				
				x++;
			}
			
			CuiHelper.AddUi(player, container);
			
			GUIMenuInfo(player);
			
			if(config.Setting.AEvents)
			    GUIEvent(player);
			
			if(config.Setting.APIuginsInfo)
				GUIPluginsInfo(player);
			
			if(config.Setting.AGrig || config.Setting.ACoordinates)
				GUIPlayerPos(player);
		}
		
		private void GUIEvent(BasePlayer player)
		{
			MenuConfig.MenuSetting menu = config.Menu;
			
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = menu.EAnchorMin, AnchorMax = menu.EAnchorMax, OffsetMin = menu.EOffsetMin, OffsetMax = menu.EOffsetMax },
                Image = { Color = config.Event.EMenuColor, Material = config.Event.EMenuMaterial }
            }, ".MenuGUI", ".EventGUI", ".EventGUI");
			
			int count = config.Event.Events.Count;
			
			foreach(var i in config.Event.Events) 
			{
				double offset = -(11.25 * count--) - (1.5 * count--);
				
				container.Add(new CuiPanel 
                { 
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{offset} -11.25", OffsetMax = $"{offset + 22.5} 11.25" },
                    Image = { Color = config.Event.EBackgroundColor }
                }, ".EventGUI", $".{i.Key}");
				
				container.Add(new CuiPanel 
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "2.5 2.5", OffsetMax = "-2.5 -2.5" },
					Image = { Png = (string) ImageLibrary.Call("GetImage", $".{i.Key}"), Color = _activeevents[i.Key] ? i.Value.EventAColor : i.Value.EventDColor }
				}, $".{i.Key}");
			}  
			
			CuiHelper.AddUi(player, container); 
		}
		public string time;
		
		private void DestroyUiMenu(BasePlayer player, bool offlogo = true)
		{
			if(config.Setting.Reload)
				players.Remove(player);
			
			CuiHelper.DestroyUi(player, ".MenuGUI");
			CuiHelper.DestroyUi(player, ".AddButton_Open");
			CuiHelper.DestroyUi(player, ".AddButton_Close");
			
			if(offlogo)
				GUILogo(player);
		}
		
		private void GUIMenuInfo(BasePlayer player)
		{
            CuiElementContainer container = new CuiElementContainer();
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "47.5 -40", OffsetMax = "318 -5" },
                Text = { Text = string.Format(lang.GetMessage("TITLE", this, player.UserIDString), online, maxplayers, joining, sleeping, time), Align = TextAnchor.UpperCenter, FontSize = 13, Color = "1 1 1 1" }
            }, ".MenuGUI", ".MenuInfoGUI", ".MenuInfoGUI");
			
			CuiHelper.AddUi(player, container);
		}
		
		private void OnPlayerDisconnected(BasePlayer player)
		{
			if(config.Setting.Reload)
			    players.Remove(player);
		}
		
		[ConsoleCommand("ui_menu_b")]
		void cmdCloseGUI(ConsoleSystem.Arg args)
		{
			BasePlayer player = args?.Player();
			
			if(player == null) return;
			
			if(Cooldowns.ContainsKey(player))
				if(Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
			
			player.SendConsoleCommand(args.Args[0].Replace("'", ""));
			
			if(config.Menu.CloseMenu)
				DestroyUiMenu(player);
		   		 		  						  	   		  	   		  	  			  		  		   		 
			Cooldowns[player] = DateTime.Now.AddSeconds(0.3f);
		}
		
		private void OnEntityKill(BaseNetworkable entity)
		{
			if(entity is CargoShip)
			{
				_events.Remove(entity);
				
				if(_events.Where(x => x is CargoShip).Count() == 0)
					_activeevents["CargoShip"] = false;
			}
			if(entity is CargoPlane)
			{
				_events.Remove(entity);
				
				if(_events.Where(x => x is CargoPlane).Count() == 0)
					_activeevents["CargoPlane"] = false;
			}
			if(entity is BradleyAPC)
			{
				_events.Remove(entity);
				
				if(_events.Where(x => x is BradleyAPC).Count() == 0)
					_activeevents["BradleyAPC"] = false;
			}
			if(entity is PatrolHelicopter)
			{
				_events.Remove(entity);
				
				if(_events.Where(x => x is PatrolHelicopter).Count() == 0)
					_activeevents["BaseHelicopter"] = false;
			}
			if(entity is CH47Helicopter)
			{
				_events.Remove(entity);
				
				if(_events.Where(x => x is CH47Helicopter).Count() == 0)
					_activeevents["CH47Helicopter"] = false;
			}
		}
		
		private Dictionary<string, bool> _activeevents = new Dictionary<string, bool>
		{
			["CargoPlane"] = false,
			["BaseHelicopter"] = false,
			["CargoShip"] = false,
			["CH47Helicopter"] = false,
			["BradleyAPC"] = false
		};
		
		private void GUIAddButtonOpen(BasePlayer player)
		{
			CuiElementContainer container = new CuiElementContainer();
					
			container.Add(new CuiButton
			{
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -20", OffsetMax = "0 -2.5" },
				Button = { Color = config.Menu.MenuColor, Command = "ui_menu addbutton", Material = config.Menu.MenuMaterial },
				Text = { Text = "" }
			}, ".LogoGUI", ".AddButton_Open", ".AddButton_Open");
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 -1" },
                Text = { Text = lang.GetMessage("AddOPEN", this, player.UserIDString), Align = TextAnchor.MiddleCenter, FontSize = 8, Color = "1 1 1 0.805641" }
            }, ".AddButton_Open");
					
			CuiHelper.AddUi(player, container);
		}
 
        private class MenuConfig
        {		
		    
            internal class MenuSetting
			{
				[JsonProperty(LanguageEnglish ? "Menu color" : "Цвет меню")] public string MenuColor;
				[JsonProperty(LanguageEnglish ? "Menu material" : "Материал меню")] public string MenuMaterial;
				[JsonProperty(LanguageEnglish ? "Button color" : "Цвет кнопок")] public string ButtonColor;
				[JsonProperty(LanguageEnglish ? "Button text color" : "Цвет текста кнопок")] public string ButtonTextColor;
				[JsonProperty(LanguageEnglish ? "Side line color" : "Цвет боковых линий")] public string LineColor;
				[JsonProperty(LanguageEnglish ? "Button text size" : "Размер текста кнопок")] public int ButtonSize;
				[JsonProperty(LanguageEnglish ? "Close the menu after pressing one of the buttons" : "Закрывать меню после нажатия одной из кнопок")] public bool CloseMenu;
				[JsonProperty(LanguageEnglish ? "Move the menu/logo when the mission is active" : "Сдвинуть меню/лого при активной мисси")] public bool Mission;
				[JsonProperty(LanguageEnglish ? "Menu - AnchorMin" : "Меню - AnchorMin")] public string MAnchorMin;
                [JsonProperty(LanguageEnglish ? "Menu - AnchorMax" : "Меню - AnchorMax")] public string MAnchorMax;
                [JsonProperty(LanguageEnglish ? "Menu - OffsetMin" : "Меню - OffsetMin")] public string MOffsetMin;
                [JsonProperty(LanguageEnglish ? "Menu - OffsetMax" : "Меню - OffsetMax")] public string MOffsetMax;
                [JsonProperty(LanguageEnglish ? "Move menu - OffsetMin" : "Сдвинуть меню - OffsetMin")] public string MMOffsetMin;
                [JsonProperty(LanguageEnglish ? "Move menu - OffsetMax" : "Сдвинуть меню - OffsetMax")] public string MMOffsetMax;				
				[JsonProperty(LanguageEnglish ? "Plugin info - AnchorMin" : "Инфа плагинов - AnchorMin")] public string PAnchorMin;
                [JsonProperty(LanguageEnglish ? "Plugin info - AnchorMax" : "Инфа плагинов - AnchorMax")] public string PAnchorMax;
                [JsonProperty(LanguageEnglish ? "Plugin info - OffsetMin" : "Инфа плагинов - OffsetMin")] public string POffsetMin;
                [JsonProperty(LanguageEnglish ? "Plugin info - OffsetMax" : "Инфа плагинов - OffsetMax")] public string POffsetMax;
				
				[JsonProperty(LanguageEnglish ? "Events - AnchorMin" : "Ивенты - AnchorMin")] public string EAnchorMin = "0 1";
				[JsonProperty(LanguageEnglish ? "Events - AnchorMax" : "Ивенты - AnchorMax")] public string EAnchorMax = "0 1";
				[JsonProperty(LanguageEnglish ? "Events - OffsetMin" : "Ивенты - OffsetMin")] public string EOffsetMin = "112.25 -90";
				[JsonProperty(LanguageEnglish ? "Events - OffsetMax" : "Ивенты - OffsetMax")] public string EOffsetMax = "242.75 -62.5";				
				
				[JsonProperty(LanguageEnglish ? "Coordinates - AnchorMin" : "Координаты - AnchorMin")] public string CAnchorMin = "0 0";
				[JsonProperty(LanguageEnglish ? "Coordinates - AnchorMax" : "Координаты - AnchorMax")] public string CAnchorMax = "0 0";
				[JsonProperty(LanguageEnglish ? "Coordinates - OffsetMin" : "Координаты - OffsetMin")] public string COffsetMin = "244.75 -29.5";
				[JsonProperty(LanguageEnglish ? "Coordinates - OffsetMax" : "Координаты - OffsetMax")] public string COffsetMax = "355 -2";				
				
				[JsonProperty(LanguageEnglish ? "Grid - AnchorMin" : "Сетка - AnchorMin")] public string GAnchorMin = "0 0";
				[JsonProperty(LanguageEnglish ? "Grid - AnchorMax" : "Сетка - AnchorMax")] public string GAnchorMax = "0 0";
				[JsonProperty(LanguageEnglish ? "Grid - OffsetMin" : "Сетка - OffsetMin")] public string GOffsetMin = "83 -29.5";
				[JsonProperty(LanguageEnglish ? "Grid - OffsetMax" : "Сетка - OffsetMax")] public string GOffsetMax = "110.5 -2";
			}
            [JsonProperty(LanguageEnglish ? "Settings buttons [ Key_text | Command ] - [ Text setting in oxide/lang ]" : "Настройка кнопок [ Ключ_текста | Команда ] - [ Настройка текста в oxide/lang ]")] 
            public Dictionary<string, string> Button;
		    internal class GeneralSetting
			{
				[JsonProperty(LanguageEnglish ? "Show coordinates" : "Отображать координаты")] public bool ACoordinates = true;
                [JsonProperty(LanguageEnglish ? "Display information of other plugins" : "Отображать информацию других плагинов")] public bool APIuginsInfo;
                [JsonProperty(LanguageEnglish ? "Update information of other plugins [ Updates only when the menu is open ]" : "Обновлять информацию других плагинов [ Обновляется только при открытом меню ]")] public bool ReloadPluginsInfo;
                [JsonProperty(LanguageEnglish ? "Fake online from the plugin - [ Default - 0 | IQFakeActive - 1 | FGS - 2] - ( Displayed only in the panel and nowhere else )" : "Фейк онлайн от плагина - [ Default - 0 | IQFakeActive - 1 | FGS - 2]")] public int FakeOnline;
				[JsonProperty(LanguageEnglish ? "Time format - [ HH:mm - 24:00 | hh:mm tt - 12:00 ]" : "Формат времени - [ HH:mm - 24:00 | hh:mm tt - 12:00 ]")] public string TimeFormat;
                [JsonProperty(LanguageEnglish ? "Open menu after connection" : "Открытое меню после подключения")] public bool Connect;
                [JsonProperty(LanguageEnglish ? "Update menu [ Only the open menu is updated ]" : "Обновлять меню [ Обновляется только открытое меню ]")] public bool Reload;
                [JsonProperty(LanguageEnglish ? "Open menu refresh interval" : "Интервал обновления открытого меню")] public float IReload;
				[JsonProperty(LanguageEnglish ? "Show grid" : "Отображать сетку")] public bool AGrig = true;
                [JsonProperty(LanguageEnglish ? "Show events" : "Отображать ивенты")] public bool AEvents;
			}			   
			[JsonProperty(LanguageEnglish ? "Settings events" : "Настройка ивентов")]
            public EventsSetting Event;
			[JsonProperty(LanguageEnglish ? "Settings menu" : "Настройка меню")]
            public MenuSetting Menu;
            [JsonProperty(LanguageEnglish ? "Settings additional buttons" : "Настройка дополнительных кнопок")]
            public List<ButtonPlus> ButtonP;
			 
			internal class PluginsInfoSetting	
			{
                [JsonProperty(LanguageEnglish ? "Plugin name" : "Название плагина")] public string PluginName;				
                [JsonProperty(LanguageEnglish ? "Method name(API)" : "Название метода(API)")] public string HookName;				
                [JsonProperty(LanguageEnglish ? "Hook parameter type - [ player | userID ]" : "Тип параметра хука - [ player | userID ]")] public string Parameter;				

                public PluginsInfoSetting(string pluginname, string hookname, string parameter)
				{
					PluginName = pluginname; HookName = hookname; Parameter = parameter;
				}				
			}			
			
			public static MenuConfig GetNewConfiguration()
            {
                return new MenuConfig
                {
                    Setting = new GeneralSetting
                    {
						Connect = false,
						APIuginsInfo = false,
						AEvents = true,
						AGrig = true,
						ACoordinates = true,
						Reload = false,
						ReloadPluginsInfo = false,
						IReload = 12.5f,
						FakeOnline = 0,
						TimeFormat = "HH:mm"
                    },
					Logo = new LogoSetting
					{
						LogoURL = "https://i.ibb.co/DKnnQw0/Hh7W3hz.png",
						LogoColor = "1 1 1 1",
                        LogoMaterial = "assets/icons/greyout.mat",
						AnchorMin = "0 1",
						AnchorMax = "0 1",
						OffsetMin = "10 -78",
						OffsetMax = "80 -8",
						MMOffsetMin = "10 -178",
						MMOffsetMax = "80 -108"
					},
                    Menu = new MenuSetting
                    {
						MenuColor = "1 0.2705641 0 0.505641",
						MenuMaterial = "assets/icons/greyout.mat",
						ButtonColor = "0.21705641 0.22105641 0.20905641 0.7505641",
						ButtonTextColor = "1 1 1 1",
						LineColor = "1 1 1 1",
						ButtonSize = 9,
						Mission = false,
						CloseMenu = false,
						MAnchorMin = "0 1",
						MAnchorMax = "0 1",
						MOffsetMin = "45 -72.5",
						MOffsetMax = "400 -12.5",
						MMOffsetMin = "45 -172.5",
						MMOffsetMax = "400 -112.5",						
						PAnchorMin = "0 1",
						PAnchorMax = "0 1",
						POffsetMin = "357.5 -60",
						POffsetMax = "445 0",
						EAnchorMin = "0 1",
						EAnchorMax = "0 1",
						EOffsetMin = "112.25 -90",
						EOffsetMax = "242.75 -62.5",
						CAnchorMin = "0 0",
						CAnchorMax = "0 0",
						COffsetMin = "244.75 -29.5",
						COffsetMax = "355 -2",
						GAnchorMin = "0 0",
						GAnchorMax = "0 0",
						GOffsetMin = "83 -29.5",
						GOffsetMax = "110.5 -2"
                    },
					Button = new Dictionary<string, string>
					{
						["REWARD"] = "chat.say /reward",
			            ["CALENDAR"] = "chat.say /calendar",
			            ["SHOP"] = "chat.say /s",
			            ["CRAFT"] = "chat.say /craft",  
			            ["INFO"] = "chat.say /info" 
					},					
					ButtonP = new List<ButtonPlus>
					{
						new ButtonPlus("store", "", "assets/icons/open.png"),
						new ButtonPlus("chat.say /s", "https://i.ibb.co/ykDPJ4B/GRZseo8.png", "assets/icons/community_servers.png"),
						new ButtonPlus("chat.say /stats", "", "assets/icons/market.png")
					},
					ButtonAdd = new Dictionary<string, string>
					{
						["KIT_VIP"] = "chat.say \"/kit vip\"",
						["KIT_PREM"] = "chat.say \"/kit premium\"",
						["KIT_ELITE"] = "chat.say \"/kit elite\"",
						["KIT_GOLD"] = "chat.say \"/kit gold\"",
						["LEVEL"] = "chat.say /level"
					},
					Event = new EventsSetting 
					{
						EMenuColor = "1 0.2705641 0 0.505641",
						EMenuMaterial = "assets/icons/greyout.mat",
						EBackgroundColor = "0.217 0.221 0.209 0.75",
						Events = new Dictionary<string, EventSetting>
						{
							["CargoPlane"] = new EventSetting
							{
								EventURL = "https://i.ibb.co/m6Fvdn1/01.png",
								EventAColor = "1 0.5 0.5 1",
								EventDColor = "1 1 1 1"
							},
							["BaseHelicopter"] = new EventSetting
							{
								EventURL = "https://i.ibb.co/Sf0w95T/03.png",
								EventAColor = "1 0.5 1 1",
								EventDColor = "1 1 1 1"
							},
				            ["CargoShip"] = new EventSetting
						    {
							    EventURL = "https://i.ibb.co/LvRq2X3/02.png",
							    EventAColor = "0.5 0.5 1 1",
							    EventDColor = "1 1 1 1"
						    },
							["CH47Helicopter"] = new EventSetting
							{
						    	EventURL = "https://i.ibb.co/DCcp6Td/04.png",
						    	EventAColor = "0.5 1 1 1",
						    	EventDColor = "1 1 1 1"
						    },							
							["BradleyAPC"] = new EventSetting
							{
								EventURL = "https://i.ibb.co/5L6qYR4/05.png",
								EventAColor = "1 1 0.5 1",
								EventDColor = "1 1 1 1"
							}												
						}
					},
					PluginsInfo = new List<PluginsInfoSetting>
					{
						new PluginsInfoSetting("XShop", "API_GetBalance", "player"),
						new PluginsInfoSetting("XLevels", "API_GetLevel", "player")
					}
				};
			}
			
			[JsonProperty(LanguageEnglish ? "General settings" : "Общие настройки")]
            public GeneralSetting Setting;
		   		 		  						  	   		  	   		  	  			  		  		   		 
            internal class EventsSetting	
			{
				[JsonProperty(LanguageEnglish ? "Event menu color" : "Цвет меню ивентов")] public string EMenuColor;
				[JsonProperty(LanguageEnglish ? "Event menu material" : "Материал меню ивентов")] public string EMenuMaterial;
				[JsonProperty(LanguageEnglish ? "Event icons background color" : "Цвет фона иконок ивентов")] public string EBackgroundColor;
				
				[JsonProperty(LanguageEnglish ? "Setting up event icons" : "Настройка иконок ивентов")]
                public Dictionary<string, EventSetting> Events; 
			}            
			
			internal class ButtonPlus
			{
				[JsonProperty(LanguageEnglish ? "Command" : "Команда")] public string Command;
				[JsonProperty(LanguageEnglish ? "Link to image from internet" : "Ссылка на картинку из интернета")] public string LinkImageURL;
				[JsonProperty(LanguageEnglish ? "Link to icon from the game" : "Ссылка на иконку из игры")] public string LinkImageGame;
				
				public ButtonPlus(string cmd, string url, string game)
				{
					Command = cmd; LinkImageURL = url; LinkImageGame = game;
				}
			}
		   		 		  						  	   		  	   		  	  			  		  		   		 
		    internal class EventSetting
			{
				[JsonProperty(LanguageEnglish ? "Link to event image" : "Ссылка на картинку ивента")] public string EventURL;
				[JsonProperty(LanguageEnglish ? "Active event color" : "Цвет активного ивента")] public string EventAColor;
				[JsonProperty(LanguageEnglish ? "Inactive event color" : "Цвет неактивного ивента")] public string EventDColor;
			}	 
			[JsonProperty(LanguageEnglish ? "Dropdown buttons - [ Key_text | Command ] - [ Text setting in oxide/lang ]" : "Кнопки выпадающего меню - [ Ключ_текста - команда ] - [ Настройка текста в oxide/lang ]")]
            public Dictionary<string, string> ButtonAdd;
			[JsonProperty(LanguageEnglish ? "Settings logo" : "Настройка логотипа")]
            public LogoSetting Logo;
		   		 		  						  	   		  	   		  	  			  		  		   		 
            internal class LogoSetting
			{
                [JsonProperty(LanguageEnglish ? "Link to the logo image" : "Ссылка на картинку логотипа")] public string LogoURL;
                [JsonProperty(LanguageEnglish ? "Logo color" : "Цвет логотипа")] public string LogoColor;
                [JsonProperty(LanguageEnglish ? "Logo material" : "Материал логотипа")] public string LogoMaterial;
                [JsonProperty(LanguageEnglish ? "Logo - AnchorMin" : "Лого - AnchorMin")] public string AnchorMin;
                [JsonProperty(LanguageEnglish ? "Logo - AnchorMax" : "Лого - AnchorMax")] public string AnchorMax;
                [JsonProperty(LanguageEnglish ? "Logo - OffsetMin" : "Лого - OffsetMin")] public string OffsetMin;
                [JsonProperty(LanguageEnglish ? "Logo - OffsetMax" : "Лого - OffsetMax")] public string OffsetMax;
				[JsonProperty(LanguageEnglish ? "Move logo - OffsetMin" : "Сдвинуть лого - OffsetMin")] public string MMOffsetMin;
                [JsonProperty(LanguageEnglish ? "Move logo - OffsetMax" : "Сдвинуть лого - OffsetMax")] public string MMOffsetMax;
			}	
			[JsonProperty(LanguageEnglish ? "Configuring information for other plugins. [ Hooks with parameter type - player(BasePlayer) | userID(ulong) ]" : "Настройка информации других плагинов. [ Хуки c типом параметра - player(BasePlayer) | userID(ulong) ]")]
            public List<PluginsInfoSetting> PluginsInfo;
        }

        	}
}