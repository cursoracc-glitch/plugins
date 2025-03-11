using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Rust;
using System;
using System.Linq;
using Random = UnityEngine.Random;
using ru = Oxide.Game.Rust;

namespace Oxide.Plugins
{
    [Info("MenuSystem", "TopPlugin.ru", "3.0.0")]
    class MenuSystem : RustPlugin
    {
        #region Вар
        private string Layer = "Menu_UI";

        [PluginReference] Plugin ImageLibrary;

        string Url = "";
        #endregion

        #region Класс
		public class MenuItem:SubMenuItem{
            [JsonProperty("Пункты подменю", Order = 1)]
			public List<SubMenuItem> items;
		}
		public class SubMenuItem{
            [JsonProperty("Заголовок пункта меню", Order = 1)]
			public string title;
            [JsonProperty("Чат команда для доступа к пункту меню", Order = 2)]
			public string chatCommand;
            [JsonProperty("Имя плагина, api которого необходимо вызвать", Order = 3)]
			public string pluginName;
            [JsonProperty("Имя функции из плагина для показа GUI", Order = 4)]
			public string functionName;
            [JsonProperty("Передаваемые аргументы (player - передавать не нужно он передается первым параметром)", Order = 5)]
			public List<object> args = new List<object>();
            [JsonProperty("Названия слоев для дестроя GUI", Order = 6)]
			public List<string> UILayers = new List<string>();
            [JsonProperty("Имя функции из плагина для дополнительной отработки закрытия", Order = 7)]
			public string closeFunctionName;
		}
		
		
        public class Settings
        {
            [JsonProperty("Название кнопки")] public string DisplayName;
            [JsonProperty("Описание")] public string Info;
            [JsonProperty("Команда")] public string Command;
        }
        #endregion

		public Dictionary<ulong,SubMenuItem> lastMenuElement = new Dictionary<ulong,SubMenuItem>();

        #region Кофниг
        Configuration config;
        class Configuration 
        {
            [JsonProperty("Расстояние между пунктами меню",Order=0)] public float punktOffset = 0.138f;
            [JsonProperty("Пункты меню",Order=1)] public Dictionary<string,MenuItem> menuItems;
            [JsonProperty("Настройки", Order=2)] public List<Settings> settings;
            public static Configuration GetNewConfig() 
            {
                return new Configuration
                {
					punktOffset = 0.138f,
                    settings = new List<Settings>() 
                    {
                        new Settings
                        {
                            DisplayName = "О СЕРВЕРЕ",
                            Info = "<b><size=20>О СЕРВЕРЕ</size></b>\n\nХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуитаnХуита",
                            Command = "i"
                        },
                        new Settings
                        {
                            DisplayName = "ПРАВИЛА",
                            Info = "<b><size=20>ПРАВИЛА</size></b>\n\nХуита",
                            Command = "p"
                        },
                        new Settings
                        {
                            DisplayName = "КОМАНДЫ И БИНДЫ",
                            Info = "<b><size=20>КОМАНДЫ И БИНДЫ</size></b>\n\nХуита",
                            Command = "b"
                        },
                    },
					menuItems = new Dictionary<string,MenuItem>(){
						["info"]  = new MenuItem(){
							title = "ГЛАВНАЯ",
							chatCommand="info",
							pluginName="",
							functionName="",
							args = new List<object>(){
								43,
								"ASD"
							},
							UILayers = new List<string>(){
								"UI_Layer1"
							},
							items = new List<SubMenuItem>(){
									new SubMenuItem(){
									title = "ПУНКТ1",
									chatCommand="info1",
									pluginName="",
									functionName="",
									args = new List<object>(){
										43,
										"ASD"
									},
									UILayers = new List<string>(){
										"UI_Layer1"
									},
								}
							}
						},
						["store"]  = new MenuItem(){
							title = "КОРЗИНА",
							chatCommand="store",
							pluginName="GameStoresRUST",
							functionName="InitializeStore",
							args = new List<object>(){
								"player"
							},
							UILayers = new List<string>(){
								"UI_GameStoresRUST_Store"
							}
						},
						["case"]  = new MenuItem(){
							title = "КЕЙСЫ",
							chatCommand="case",
							pluginName="CaseSystem",
							functionName="CaseUI",
							args = new List<object>(){
								"player"
							},
							UILayers = new List<string>(){
								"Case_UI"
							}
						},
						["cal"]  = new MenuItem(){
							title = "КАЛЕНДАРЬ",
							chatCommand="wipe",
							pluginName="WipeCalendar",
							functionName="CreateGui",
							args = new List<object>(){
								"player"
							},
							UILayers = new List<string>(){}
						},
						["wipe"]  = new MenuItem(){
							title = "ВАЙПБЛОК",
							chatCommand="wipe",
							pluginName="WipeCalendar",
							functionName="CreateGui",
							args = new List<object>(){
								"player"
							},
							UILayers = new List<string>(){}
						},
						["report"]  = new MenuItem(){
							title = "РЕПОРТ",
							chatCommand="wipe",
							pluginName="SoReport",
							functionName="StartUi",
							args = new List<object>(){
								"player"
							},
							UILayers = new List<string>(){"ReportMain","ReportBMod"}
						},
						["settings"]  = new MenuItem(){
							title = "НАСТРОЙКИ",
							chatCommand="settings",
							pluginName="",
							functionName="SettingsUI",
							args = new List<object>(){
								"player"
							},
							UILayers = new List<string>(){"settings","Settingsmain","UI_SettingsLayer.WINDOW_FRAME"}
						},
						["pass"]  = new MenuItem(){
							title = "БАТЛПАСС",
							chatCommand="pass",
							pluginName="PassSystem",
							functionName="PassUI",
							args = new List<object>(){
								"player"
							},
							UILayers = new List<string>(){"Pass"}
						}
					}
                };
            }
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.settings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Хуки
        void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------\n" +
            "     Author - https://topplugin.ru/\n" +
            "     VK - https://vk.com/rustnastroika\n" +
            "     Discord - https://discord.com/invite/5DPTsRmd3G\n" +
            "-----------------------------");
            ImageLibrary.Call("AddImage", "https://imgur.com/C2g6QoA.png", "Background");
            ImageLibrary.Call("AddImage", Url, "LogoImage");
			RegisterChatCommand();
        }
        #endregion

        #region Команды
		void RegisterChatCommand(){
			foreach (MenuItem item in config.menuItems.Values){
				if (!string.IsNullOrEmpty(item.chatCommand)) Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddChatCommand(item.chatCommand, this, "MenuChatCommands");
				if (item.items==null) continue;
				foreach (SubMenuItem item2 in item.items.Where(x => !string.IsNullOrEmpty(x.chatCommand))){
					Interface.Oxide.GetLibrary<ru.Libraries.Command>(null).AddChatCommand(item2.chatCommand, this, "SubMenuChatCommands");					
				}
			}
		}
		private void MenuChatCommands(BasePlayer player, string command, string[] args)=>MenuUI(player, command);
		private void SubMenuChatCommands(BasePlayer player, string command, string[] args)=>MenuUI(player, command, true);

        [ConsoleCommand("closemenusystem")]
        void ConsoleCloseMenu(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            DestroyUI(player);
            CuiHelper.DestroyUi(player, Layer);
			CheckAdditionalClose(player);
        }
		public void CloseMenu(BasePlayer player){			
            DestroyUI(player);
            CuiHelper.DestroyUi(player, Layer);
			CheckAdditionalClose(player);
		}
        [ConsoleCommand("menu")]
        void ConsoleMenu(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            //ActiveButton[player.userID] = args.Args[0];
            DestroyUI(player);
            ButtonUI(player, args.Args[0]);
        }
        #endregion
        
        #region Интерфейс
        void MenuUI(BasePlayer player, string name = "menu", bool sub=false)
        {
			int pos = 0;
			if (sub){
				DestroySubUI(player,name);
				//Ищем меню первого уровня и позицию подпункта
				foreach(KeyValuePair<string, MenuItem> item1 in config.menuItems.Where(x=>x.Value.items!=null && x.Value.items.Count>0)){
					for(int i=0;i<item1.Value.items.Count;i++){
						if (item1.Value.items[i].chatCommand==name){
							pos = i;
							name = item1.Key;
						}						
					}				
				}			
			}
			
			
			
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" },
            }, "Overlay", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Background") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = "0.24 0.88", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, Layer, "Logo");

            container.Add(new CuiElement
            {
                Parent = "Logo",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "LogoImage") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.068 0.17", AnchorMax = "0.2 0.73", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, Layer, "Button");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.945 0.91", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = "closemenusystem" },
                Text = { Text = $"✖", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 50, Font = "robotocondensed-bold.ttf" }
            }, Layer);

            CuiHelper.AddUi(player, container);
            DestroyUI(player);
            ButtonUI(player, name);
			SubUI(player, name, pos);
        }

		void CheckAdditionalClose(BasePlayer player){			
			//Смотрим щелкали ли мы куда то ранее и нужно ли вызывать доп функцию закрытия
			if (!lastMenuElement.ContainsKey(player.userID)) lastMenuElement.Add(player.userID, null);
			SubMenuItem last = lastMenuElement[player.userID];
			if (last!=null){
				Puts($"LAST ELEMENT IS {last.pluginName}");
				if (!string.IsNullOrEmpty(last.closeFunctionName)){
					if (!string.IsNullOrEmpty(last.pluginName) && plugins.Find(last.pluginName)){ 
						Puts("CALL CLOSE FUNCTION");
						plugins.Find(last.pluginName).Call(last.closeFunctionName, player);
					}							
				}
			}
		}
		bool ExecExternalCMD(SubMenuItem item, BasePlayer player){
			string pluginName="";
			CheckAdditionalClose(player);
			
			if (item==null || player == null || string.IsNullOrEmpty(item.functionName)) return false;
			if (item.functionName == "close") {CloseMenu(player);return false;}
			pluginName = "MenuSystem";
			if (!string.IsNullOrEmpty(item.pluginName)) pluginName = item.pluginName;
			if (!plugins.Find(pluginName)) {PrintWarning($"Plugin not found: {pluginName}");return false;}
			var Plugin = plugins.Find(pluginName);
			if (Plugin==null) return false;
			if (item.args==null || item.args.Count==0){
				Plugin.Call(item.functionName, player);				
				Puts($"{pluginName}.Call({item.functionName}, {player});");
			}
			switch(item.args.Count){
				case 1: 
					Plugin.Call(item.functionName, player, item.args[0]);
					break;
				case 2: 
					Plugin.Call(item.functionName, player,item.args[0],item.args[1]);
					break;
				case 3: 
					Plugin.Call(item.functionName, player,item.args[0],item.args[1],item.args[2]);
					//Puts($"Plugin.Call({item.functionName}, {player},{item.args[0]},{item.args[1]},{item.args[2]});");
					break;
				case 4: 
					Plugin.Call(item.functionName, player,item.args[0],item.args[1],item.args[2],item.args[3]);
					break;
			}	
			//Сохраняем указатель что открыли
			lastMenuElement[player.userID] = item;
			return true;
		}

        void ButtonUI(BasePlayer player, string name="")
        {			
            //DestroyUI(player);
            CuiHelper.DestroyUi(player, "Command");
            CuiHelper.DestroyUi(player, "Info");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.13", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0"  }
            }, "Button", "Command");
			
			string cmd = "";
            float width = 1f, height = config.punktOffset, startxBox = 0f, startyBox = 0.995f - height, xmin = startxBox, ymin = startyBox;
            foreach (KeyValuePair<string, MenuItem> item in config.menuItems)
            {
                //var color = ActiveButton[player.userID] == item.Key ? "1 1 1 1" : "1 1 1 0.3";
				var color ="1 1 1 0.3";
				if (name == item.Key){
					ExecExternalCMD(item.Value, player);
					color = "1 1 1 1";
				}
				cmd = $"menu {item.Key}";
				if (item.Value.items!=null && item.Value.items.Count>0) cmd = $"subMenu {item.Key}";
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "0 1", OffsetMax = "0 -1" },
                    Button = { Color = "0 0 0 0", Command = cmd },
                    Text = { Text = item.Value.title, Color = color, Align = TextAnchor.MiddleLeft, FontSize = 25, Font = "robotocondensed-bold.ttf" }
                }, "Command");

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            CuiHelper.AddUi(player, container);
        }
		
        [ConsoleCommand("subMenu")]
        void ConsoleSubMenuCmd(ConsoleSystem.Arg args)
        {
			var player = args.Player();
			if (args.Args.Length==0) return;
			int pos = 0;
			if (args.Args.Length==2) 
				if (!int.TryParse(args.Args[1], out pos)) pos =0;
            //ActiveButton[player.userID] = args.Args[0];
            DestroyUI(player);
            ButtonUI(player, args.Args[0]);
			SubUI(player, args.Args[0],pos);
        }
        void SubUI(BasePlayer player, string keyItem, int pos = 0)
        {
			if (!config.menuItems.ContainsKey(keyItem)) return;
			MenuItem item = config.menuItems[keyItem];
			if (item==null || item.items==null || item.items.Count<1) {
				//PrintWarning($"MenuItem {keyItem} not contains submenu");
				return;
			}
            //ActiveButton2[player.userID] = "i";
            var container = new CuiElementContainer();
			
            CuiHelper.DestroyUi(player, "Info");
			
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.276 0", AnchorMax = "0.945 1", OffsetMax = "0 0" },
                Image = { Color = "0.117 0.121 0.109 0" }
            }, Layer, "Info");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.289 1", OffsetMax = "0 0" },
                Image = { Color = "0.549 0.270 0.215 0.7", Material = "" }
            }, "Info", "Left");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.09 0.91", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = item.title, Color = "0.929 0.882 0.847 0.8", Align = TextAnchor.MiddleLeft, FontSize = 33, Font = "robotocondensed-bold.ttf" }
            }, "Left");

            CuiHelper.AddUi(player, container);
            SubButton(player,keyItem,pos);
        }


        void SubButton(BasePlayer player, string keyItem,int index=0)
        {
			if (!config.menuItems.ContainsKey(keyItem)) return;
			MenuItem item = config.menuItems[keyItem];
			if (item==null || item.items==null || item.items.Count<1) {
				PrintWarning($"MenuItem {keyItem} not contains submenu");
				return;
			}
            CuiHelper.DestroyUi(player, "InfoButton");
            CuiHelper.DestroyUi(player, "Menu_UI2");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0"  }
            }, "Left", "InfoButton");

            float width = 1f, height = 0.06f, startxBox = 0f, startyBox = 0.6f - height, xmin = startxBox, ymin = startyBox;
			int i=0;
			SubMenuItem SelectedItem = null;
            foreach (SubMenuItem check in item.items)
            {
                var color =  "0 0 0 0";
				if (index == i){
					SelectedItem = check;
					color = "0.149 0.145 0.137 0.8";
				}
				
				container.Add(new CuiPanel
				{
					RectTransform = { AnchorMin = "0.289 0", AnchorMax = "1 1", OffsetMax = "0 0" },
					Image = { Color = "0.117 0.121 0.109 0.8" }
				}, "Info", "Menu_UI2");
				
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "0 1", OffsetMax = "0 -1" },
                    Button = { Color = color, Command = $"subMenu {keyItem} {i}" },
                    Text = { Text = $"{check.title}      ", Color = "1 1 1 0.8", Align = TextAnchor.MiddleRight, FontSize = 18, Font = "robotocondensed-bold.ttf" }
                }, "InfoButton");
				
                

                xmin += width;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
				i++;
            }

            CuiHelper.AddUi(player, container);
			if (SelectedItem!=null)	ExecExternalCMD(SelectedItem, player);
			
        }
		

        /*
        void MapUI(BasePlayer player) {
            player.Command("map.open");
            player.Command("map_settings");
        }*/
        #endregion

        #region Хелпер
        void DestroyUI(BasePlayer player)
        {
			foreach (MenuItem menuItem in config.menuItems.Values.Where(x => x.UILayers!=null && x.UILayers.Count>0))
            {				
				foreach (string layer in menuItem.UILayers)
					CuiHelper.DestroyUi(player, layer);
			}
			CuiHelper.DestroyUi(player, "Info");
        }
        void DestroySubUI(BasePlayer player, string key)
        {
			if (!config.menuItems.ContainsKey(key)) return;
			if (key!="map") player.Command("close_map");
			MenuItem menuItem = config.menuItems[key];			
			if (menuItem.items==null || menuItem.items.Count<1) return;
			foreach (SubMenuItem subMenu in menuItem.items.Where(x=>x.UILayers!=null && x.UILayers.Count>0))
				foreach (string layer in subMenu.UILayers) CuiHelper.DestroyUi(player, layer);
        }
        #endregion
    }
}