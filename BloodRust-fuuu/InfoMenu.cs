using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("InfoMenu", "TopPlugin.ru", "3.0.0")]
    public class InfoMenu : RustPlugin
    {
		
		#region Variables
		
		[PluginReference] 
		private Plugin ImageLibrary;

		#endregion
        		
		#region Config 
		
		//Управляет высотой пункта меню
		readonly int HEIGHT_MENU_ELEMENT = 35;
		
        private PluginConfig config;
		
		private class PluginConfig
        {
            [JsonProperty("Вкладки")]
            public Dictionary<string, List<Tabs>> tabs = new Dictionary<string, List<Tabs>>();
            public static PluginConfig DefaultConfig()
            {
				PluginConfig conf = new PluginConfig();
                conf.tabs = new  Dictionary<string, List<Tabs>>();
				List<Tabs> Tab1 = new List<Tabs>();
				Tab1.Add(new Tabs(){Title = "О СЕРВЕРЕ", Pages = new List<Page>()});
				Tab1[0].Pages.Add(new Page(){Images = new List<Images>(),Buttons = new List<Buttons>(), blocks = new List<TextBlocks>()});
				//Тексты
				List<TextBlocks> blocks1 = Tab1[0].Pages[0].blocks;
				blocks1.Add(new TextBlocks(){colums = new List<TextColumns>()});
				blocks1[0].colums.Add(new TextColumns{Anchor=TextAnchor.UpperCenter, TextSize=24,OutlineColor ="0 0 0 0", TextList = new List<string>(){"<b><color=#DADADADA>ДОБРО ПОЖАЛОВАТЬ НА СЕРВЕР!</color></b>"}, PositionType = "MiddleCenter",Center = "0.5 0.95", Height = 30.0, Width=550.0});
				blocks1[0].colums.Add(new TextColumns{Anchor=TextAnchor.MiddleCenter, TextSize=16,OutlineColor ="0 0 0 0", TextList = new List<string>(){
					"Мы рады приветствовать вас на сервере <color=yellow>БЛА БЛА СЕРВ</color>!",
                    "Наш сервер - отличный выбор для любителей спокойного развития и реалистичных сражений, с множеством приятных плагинов, не мешающих классическому геймплею и развитию.",
                    "Мы постарались сделать внешний вид сервера более удобным для восприятия.",
                    "Наши сервера мы делаем учитывая все ваши пожелания!",
                    "Играйте на наших серверах, советуйте другим, помогайте нам своими предложениями сделать сервер лучше!",
                    "У нас самая лояльная администрация!"
				}, PositionType = "MiddleCenter",Center = "0.5 0.5", Height = 200.0, Width=550.0});
		
				Tab1.Add(new Tabs(){Title = "ПРИМЕР С КНОПКАМИ", Pages = new List<Page>()});
				Tab1[1].Pages.Add(new Page(){Images = new List<Images>(),Buttons = new List<Buttons>(), blocks = new List<TextBlocks>()});
				//Изображения
				List<Images> Images2 = Tab1[1].Pages[0].Images;
				Images2.Add(new Images(){URL="https://i.ibb.co/7bmLn7P/xdcobalt.jpg", PositionType="UpperLeft",Center= "0.0 0.805", Height = 150.0, Width=150.0});
				//Тексты
				List<TextBlocks> blocks2 = Tab1[1].Pages[0].blocks;
				blocks2.Add(new TextBlocks(){colums = new List<TextColumns>()});
				blocks2[0].colums.Add(new TextColumns{Anchor=TextAnchor.UpperCenter, TextSize=24,OutlineColor ="0 0 0 0", TextList = new List<string>(){"<b><color=#DADADADA>ЛАБОРАТОРИЯ КОБАЛЬТ</color></b>"}, PositionType = "MiddleCenter",Center = "0.5 0.95", Height = 30.0, Width=550.0});
				blocks2[0].colums.Add(new TextColumns{Anchor=TextAnchor.UpperLeft, TextSize=16,OutlineColor ="0 0 0 0", TextList = new List<string>(){"<color=#DADADADA>Никто не знает что происходит на этом острове!\nПериодически ученые разворачивают на нем иследовательские центры под засекреченным названием <<КОБАЛЬТ>></color>"}, PositionType = "UpperLeft",Center = "0.0 0.9", Height = 100.0, Width=550.0});
				blocks2[0].colums.Add(new TextColumns{Anchor=TextAnchor.UpperLeft, TextSize=16,OutlineColor ="0 0 0 0", TextList = new List<string>(){"<color=#DADADADA>Данные базы очень хорошо охраняются, а раз они хорошо охраняются, значит там есть чем поживиться.</color>"}, PositionType = "UpperLeft",Center = "0.315 0.807", Height = 70.0, Width=350.0});
				blocks2[0].colums.Add(new TextColumns{Anchor=TextAnchor.UpperLeft, TextSize=16,OutlineColor ="0 0 0 0", TextList = new List<string>(){"<color=#e84c3d>Будьте внимательны!</color> <color=#DADADADA>После того как вы доберетесь до ценного груза, сработает сигнализация. На звук сирены приходит подмога! Ученые серьезно вооружены и откроют огонь на поражение по любому кто приблизится к базе!</color>"}, PositionType = "UpperLeft",Center = "0.315 0.723", Height = 100.0, Width=350.0});
				
				//Кнопки
				List<Buttons> Buttons = Tab1[1].Pages[0].Buttons;
				Buttons.Add(new Buttons(){isClient=true,CommandText="chat.say TEST",CloseMenu=true,Color="#d035266D",Caption="Первая кнопка для текста в чат",TextSize=16, PositionType = "MiddleCenter",Center = "0.5 0.5", Height = 30.0, Width=550.0});
				Buttons.Add(new Buttons(){isClient=false,CommandText="inventory.giveto %STEAMID% rifle.ak 1",CloseMenu=true,Color="#df35266D",Caption="Выдать АК47 используя %STEAMID%",TextSize=16, PositionType = "MiddleCenter",Center = "0.5 0.42", Height = 30.0, Width=550.0});

				conf.tabs.Add("main",Tab1);
                return conf;
            }
        }

        public class Tabs
        {
            [JsonProperty("Заголовок вкладки")]
            public string Title;
            [JsonProperty("Страницы")]
            public List<Page> Pages = new List<Page>();
        }

        public class Page
        {
            [JsonProperty("Изображения")]
            public List<Images> Images = new List<Images>();
            [JsonProperty("Кнопки")]
            public List<Buttons> Buttons = new List<Buttons>();
            [JsonProperty("Блоки текста")]
            public List<TextBlocks> blocks = new List<TextBlocks>();
        }

        public class Images
        {
            [JsonProperty("Ссылка")]
            public string URL;
            [JsonProperty("Алгоритм позиционирования (LeftTop,MiddleCenter,RightBottom и т.д")]
            public string PositionType = "MiddleCenter";
            [JsonProperty("Позиция изображения ((0.0 - 1.0),(0.0 - 1.0))")]
            public string Center = "0.5 0.5";
            [JsonProperty("Ширина (в пикселях)")]
            public double Width;
            [JsonProperty("Высота (в пикселях)")]
            public double Height;
        }
        public class Buttons
        {
            [JsonProperty("Это команда клиентская?")]
            public bool isClient = true;
            [JsonProperty("Команда (Допускаются шаблоны %STEAMID%)")]
            public string CommandText="";
            [JsonProperty("Нужно ли закрыть меню?")]
            public bool CloseMenu = true;
            [JsonProperty("Цвет кнопки (RGBA)")]
            public string Color;
            [JsonProperty("Заголовок кнопки")]
            public string Caption="";
            [JsonProperty("Размер шрифта")]
            public int TextSize;
            [JsonProperty("Алгоритм позиционирования (LeftTop,MiddleCenter,RightBottom и т.д")]
            public string PositionType = "MiddleCenter";
            [JsonProperty("Позиция размещения ((0.0 - 1.0),(0.0 - 1.0))")]
            public string Center = "0.5 0.5";
            [JsonProperty("Ширина (в пикселях)")]
            public double Width;
            [JsonProperty("Высота (в пикселях)")]
            public double Height;
        }


        public class TextBlocks
        {
            [JsonProperty("Колонки текста")]
            public List<TextColumns> colums = new List<TextColumns>();
        }

        public class TextColumns
        {
            [JsonProperty("Выравнивание (Left/Center/Right))")]
            public TextAnchor Anchor;

            [JsonProperty("Размер шрифта")]
            public int TextSize;
            [JsonProperty("Цвет тени текста (RGBA)")]
            public string OutlineColor;
            [JsonProperty("Строки текста")]
            public List<string> TextList = new List<string>();
            [JsonProperty("Алгоритм позиционирования (LeftTop,MiddleCenter,RightBottom и т.д")]
            public string PositionType = "MiddleCenter";
            [JsonProperty("Позиция размещения блока текста ((0.0 - 1.0),(0.0 - 1.0))")]
            public string Center = "0.5 0.5";
            [JsonProperty("Ширина (в пикселях)")]
            public double Width;
            [JsonProperty("Высота (в пикселях)")]
            public double Height;
        }    
		
		
		
		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<PluginConfig>();
				if (config?.tabs == null || config.tabs.Count<1){
					//if (config.tabs[0].Pages==null || config.tabs[0].Pages.Count<1){
						PrintWarning("Errorn in config file. Created default config.");
						LoadDefaultConfig();
					//}
				}
			}
			catch
			{
				LoadDefaultConfig();
			}
			NextTick(SaveConfig);
		}
		
		protected override void SaveConfig() => Config.WriteObject(config);
		//////////////////////
		
        protected override void LoadDefaultConfig()
        {            
            config = PluginConfig.DefaultConfig();
        }
		
		#endregion

		#region Hooks	            
        private void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------\n" +
            "     Author - https://topplugin.ru/\n" +
            "     VK - https://vk.com/rustnastroika\n" +
            "     Discord - https://discord.com/invite/5DPTsRmd3G\n" +
            "-----------------------------");
            if (!ImageLibrary)
            {
                PrintError("Imagelibrary not found!"); 
                return;
            }
            IEnumerable<Images> images = from messages in config.tabs.Values from message in messages from pages in message.Pages from image in pages.Images select image;
            foreach (var image in images.ToList())            
                ImageLibrary?.Call("AddImage", image.URL, image.URL);     
			
        }                      
 
        private void Unload()
        {
            BasePlayer.activePlayerList.ToList().ForEach(player => 
				{ 
					CuiHelper.DestroyUi(player, BaseLayer);					
					CuiHelper.DestroyUi(player, LeftLayer);
				});
        }
		#endregion
		
		#region GUI
		private static string BaseLayer = "InfoMenu_base";
		private static string LeftLayer = "InfoMenu_left";
		private static string ContentLayer = "InfoMenu_content";
		
        [ConsoleCommand("infomenu_selectpage")]
        private void cmdSelectPage(ConsoleSystem.Arg args)
        {
            var player = args.Player();
			string group = args.Args[0];
            var tabIndex = args.GetInt(1);
            var page = args.GetInt(2);			
            CreateMenu(player, group, tabIndex, page, false);
        } 
		
        [ConsoleCommand("im_exec_cmd")]
        void ConsoleMenu(ConsoleSystem.Arg args)
        {
            var player = args.Player();     
			if (player==null) return;
            if (!player || !args.HasArgs(5)) return;
			string group = args.Args[0];
            bool isClient = bool.Parse(args.Args[1]);
            bool CloseMenu = bool.Parse(args.Args[2]);
            int tabIndex = args.GetInt(3);
            int page = args.GetInt(4);
            int but = args.GetInt(5);
			var Group = config.tabs[group];
			if (Group==null) return;
			if (Group.Count<tabIndex || Group[tabIndex]==null) return; 
			if (Group[tabIndex].Pages.Count<page || Group[tabIndex].Pages[page]==null) return;
			if (Group[tabIndex].Pages[page].Buttons.Count<but || Group[tabIndex].Pages[page].Buttons[but]==null) return;
			string command = Group[tabIndex].Pages[page].Buttons[but].CommandText;
			if (string.IsNullOrEmpty(command)) return;
			string commanda =command.Replace("'", "").Replace("%STEAMID%", player.UserIDString);
			if (isClient){
				player.Command(commanda);
			}else{
				rust.RunServerCommand(commanda);
			}
			if (CloseMenu) player.Command("closemenusystem");
        }
		
        private void CreateMenu(BasePlayer player, string group, int tabIndex = 0, int page = 0, bool isStart = true)
        {        
			Puts($"player={player} group={group}");
            CuiElementContainer container = new CuiElementContainer();
			
			if (isStart)
			{
				container.Add(new CuiPanel
				{
					CursorEnabled = true,
					Image =	{Color = "0 0 0 0"},
					RectTransform = {AnchorMin = "0.276 0", AnchorMax = "0.945 1", OffsetMin = "0 0", OffsetMax = "0 0"}
				}, "Menu_UI", BaseLayer);
				
				container.Add(new CuiPanel()
				{ 
					CursorEnabled = true,
					RectTransform = {AnchorMin = "0 0", AnchorMax = "0.289 1", OffsetMin = "0 0", OffsetMax = "0 0"},
					Image         = {Color = "0.549 0.270 0.215 0.7", Material = "" }
				}, BaseLayer, BaseLayer + ".RedPanel");				
				
				
				container.Add(new CuiPanel()
				{ 
					CursorEnabled = true,
					RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"},
					Image         = {Color = "0 0 0 0" }
				}, BaseLayer + ".RedPanel", BaseLayer + ".Left");			
				
				container.Add(new CuiPanel()
				{ 
					CursorEnabled = true,
					RectTransform = {AnchorMin = "0.289 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"},
					Image         = {Color = "0.117 0.121 0.109 0.95" }
				}, BaseLayer, BaseLayer + ".CONTENT_MAIN");		
				container.Add(new CuiLabel
				{
					RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "20 -100", OffsetMax = "0 -15"},
					Text = { Text = "ИНФОРМАЦИЯ", Align = TextAnchor.UpperLeft, Font = "robotocondensed-bold.ttf", Color = "0.929 0.882 0.847 0.8", FontSize = 33 }
				}, BaseLayer + ".Left");
							
			}
			
			CuiHelper.DestroyUi(player, LeftLayer);
						
			// левая панель меню под кнопки 
			container.Add(new CuiPanel
			{
				Image =
				{
					Color = "0 0 0 0"
				},
				RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"},
			}, BaseLayer + ".Left", LeftLayer);
			 
			
			CuiHelper.DestroyUi(player, ContentLayer); 
			container.Add(new CuiPanel()
			{ 
				CursorEnabled = true,
				RectTransform = {AnchorMin = "0.05 0", AnchorMax = "0.95 1", OffsetMin = "0 0", OffsetMax = "0 0"},
				Image         = {Color = "0 0 0 0" }
			}, BaseLayer + ".CONTENT_MAIN", ContentLayer); 
		
		
			var Group = config.tabs[group];
			
			//ССЫЛКИ 
			float topPosition = (Group.Count() / 2f * HEIGHT_MENU_ELEMENT + (Group.Count() - 1) / 2f * 5);
			int i=0;
			foreach (var res in Group){
				container.Add(new CuiButton 
				{
					RectTransform = { AnchorMin = "0 0.5", AnchorMax = "1 0.51", OffsetMin = $"0 {topPosition - HEIGHT_MENU_ELEMENT}", OffsetMax = $"0 {topPosition}" },
					Button = { Color = tabIndex == i ? "0.149 0.145 0.137 0.8" : "0 0 0 0", Command = $"infomenu_selectpage {group} {i} {0}"},
					Text = { Text = "", Align = TextAnchor.MiddleRight, Font = "robotocondensed-bold.ttf", FontSize = 14 }
				}, LeftLayer, LeftLayer + i); 
		
				container.Add(new CuiLabel
				{
					RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"0 0", OffsetMax = $"-20 0" },
					Text = { Text = res.Title.ToUpper(), Align = TextAnchor.MiddleRight, Font = "robotocondensed-bold.ttf", FontSize = 18, Color = "0.929 0.882 0.847 1"}
				}, LeftLayer + i);
				i++;
				topPosition -= HEIGHT_MENU_ELEMENT + 5;
			}
			var PageTab = Group[tabIndex];
            var PageSelect = PageTab.Pages[page];

			string omin="";
			string omax="";
			// расставляем изображения на выбранной странице
            if (PageSelect.Images.Count > 0)
            {
                foreach (var image in PageSelect.Images)
                {
					//Смотрим алгоритм позиционирования
					GetOffsetPos(image.PositionType.ToUpper(),image.Height, image.Width, ref omin, ref omax);
                    container.Add(new CuiElement()
                    {
                        Parent = ContentLayer,
                        Components = {
                            new CuiRawImageComponent {
                                Png = (string)ImageLibrary?.Call("GetImage", image.URL), Color = "1 1 1 0.9"
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin=$"{image.Center}", AnchorMax= $"{image.Center}", OffsetMin = omin, OffsetMax = omax
                            },
                        }
                    }
                    );
                }
            }	
			// расставляем текст на выбранной странице
            foreach (var block in PageSelect.blocks)
            {
                foreach (var select in block.colums)
                {
					var text = string.Join("\n", select.TextList);
					
					GetOffsetPos(select.PositionType.ToUpper(),select.Height, select.Width, ref omin, ref omax);
					
                    container.Add(new CuiElement
                    {
                        Name = CuiHelper.GetGuid(),
                        Parent = ContentLayer, 
                        Components =
                            {
                                new CuiTextComponent { Text = text, FontSize = select.TextSize, Align = select.Anchor, Color = "1 1 1 1",Font="robotocondensed-regular.ttf" , FadeIn = isStart ? 0.5f : 0.2f},
                                new CuiRectTransformComponent{ AnchorMin = select.Center, AnchorMax = select.Center, OffsetMin = omin, OffsetMax = omax},
                                new CuiOutlineComponent {Color = ParseColorFromRGBA(select.OutlineColor), Distance = "0.5 -0.5" }
                            }
                    });
                }
            }
			// расставляем кнопки на выбранной странице
            if (PageSelect.Buttons.Count > 0)
            {
				int l=0;
                foreach (var button in PageSelect.Buttons)
                {
					//Смотрим алгоритм позиционирования
					GetOffsetPos(button.PositionType.ToUpper(),button.Height, button.Width, ref omin, ref omax);
					string cmd = $"im_exec_cmd {group} {button.isClient} {button.CloseMenu} {tabIndex} {page} {l}";
					
					container.Add(new CuiButton 
					{
						RectTransform = {AnchorMin=$"{button.Center}", AnchorMax= $"{button.Center}", OffsetMin = omin, OffsetMax = omax},
						Button = { Color = HexToCuiColor(button.Color), Command = cmd},
						Text = { Text = button.Caption, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = button.TextSize}
					}, ContentLayer, ContentLayer + "btn"+l); 
					l++;
                }
            }	
            #region Скип страниц
			if (PageTab.Pages.Count>1){
				if (page>0){
					container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0.34 0.0", AnchorMax = $"0.407 0.11", OffsetMax = "0 0" },
						Button = { Color = "0 0 0 0", Command = $"infomenu_selectpage {group} {tabIndex} {page-1}"},
						Text = { Text = $"<", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
					}, ContentLayer);
				}
				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0.41 0.0", AnchorMax = $"0.59 0.106", OffsetMax = "0 0" },
					Button = { Color = "0 0 0 0" },
					Text = { Text = $"СТРАНИЦА: {page+1}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter }
				}, ContentLayer);
				if (PageTab.Pages.Count > (page+1)){
					container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0.593 0.0", AnchorMax = $"0.66 0.11", OffsetMax = "0 0" },
						Button = { Color = "0 0 0 0", Command = $"infomenu_selectpage {group} {tabIndex} {page+1}"} ,
						Text = { Text = $">", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
					}, ContentLayer);
				}
			}
            #endregion

            CuiHelper.AddUi(player, container);
        }
		 
		
        private void CreateTab(BasePlayer player, string group, int tabIndex = 0, int page = 0)
        {        		
			var Group = config.tabs[group];		
			var PageTab = Group[tabIndex];
            var PageSelect = PageTab.Pages[page];

			string omin="";
			string omax="";
			
			CuiElementContainer container = new CuiElementContainer();
		
			CuiHelper.DestroyUi(player, ContentLayer);
			container.Add(new CuiPanel
			{
				CursorEnabled = true,
				Image =	{Color = "0 0 0 0"},
				RectTransform = { AnchorMin = "0.485 0", AnchorMax = "0.945 1" }
			}, "Menu_UI", ContentLayer);
			
			// расставляем изображения на выбранной странице
            if (PageSelect.Images.Count > 0)
            {
                foreach (var image in PageSelect.Images)
                {
					//Смотрим алгоритм позиционирования
					GetOffsetPos(image.PositionType.ToUpper(),image.Height, image.Width, ref omin, ref omax);
                    container.Add(new CuiElement()
                    {
                        Parent = ContentLayer,
                        Components = {
                            new CuiRawImageComponent {
                                Png = (string)ImageLibrary?.Call("GetImage", image.URL), Color = "1 1 1 0.9"
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin=$"{image.Center}", AnchorMax= $"{image.Center}", OffsetMin = omin, OffsetMax = omax
                            },
                        }
                    });
                }
            }	
			// расставляем текст на выбранной странице
            foreach (var block in PageSelect.blocks)
            {
                foreach (var select in block.colums)
                {
					var text = string.Join("\n", select.TextList);
					
					GetOffsetPos(select.PositionType.ToUpper(),select.Height, select.Width, ref omin, ref omax);
					
                    container.Add(new CuiElement
                    {
                        Name = CuiHelper.GetGuid(),
                        Parent = ContentLayer, 
                        Components =
                            {
                                new CuiTextComponent { Text = text, FontSize = select.TextSize, Align = select.Anchor, Color = "1 1 1 1",Font="robotocondensed-regular.ttf" , FadeIn = 0.2f},
                                new CuiRectTransformComponent{ AnchorMin = select.Center, AnchorMax = select.Center, OffsetMin = omin, OffsetMax = omax},
                                new CuiOutlineComponent {Color = ParseColorFromRGBA(select.OutlineColor), Distance = "0.5 -0.5" }
                            }
                    });
                }
            }
			// расставляем кнопки на выбранной странице
            if (PageSelect.Buttons.Count > 0)
            {
				int l=0;
                foreach (var button in PageSelect.Buttons)
                {
					//Смотрим алгоритм позиционирования
					GetOffsetPos(button.PositionType.ToUpper(),button.Height, button.Width, ref omin, ref omax);
					string cmd = $"im_exec_cmd {group} {button.isClient} {button.CloseMenu} {tabIndex} {page} {l}";
					
					container.Add(new CuiButton 
					{
						RectTransform = {AnchorMin=$"{button.Center}", AnchorMax= $"{button.Center}", OffsetMin = omin, OffsetMax = omax},
						Button = { Color = HexToCuiColor(button.Color), Command = cmd},
						Text = { Text = button.Caption, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = button.TextSize}
					}, ContentLayer, ContentLayer + "btn"+l); 
					l++;
                }
            }
            #region Скип страниц
			if (PageTab.Pages.Count>1){
				if (page>0){
					container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0.34 0.0", AnchorMax = $"0.407 0.11", OffsetMax = "0 0" },
						Button = { Color = "0 0 0 0", Command = $"infomenu_selectpage {group} {tabIndex} {page-1}"},
						Text = { Text = $"<", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
					}, ContentLayer);
				}
				container.Add(new CuiButton
				{
					RectTransform = { AnchorMin = "0.41 0.0", AnchorMax = $"0.59 0.106", OffsetMax = "0 0" },
					Button = { Color = "0 0 0 0" },
					Text = { Text = $"СТРАНИЦА: {page+1}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter }
				}, ContentLayer);
				if (PageTab.Pages.Count > (page+1)){
					container.Add(new CuiButton
					{
						RectTransform = { AnchorMin = "0.593 0.0", AnchorMax = $"0.66 0.11", OffsetMax = "0 0" },
						Button = { Color = "0 0 0 0", Command = $"infomenu_selectpage {group} {tabIndex} {page+1}"} ,
						Text = { Text = $">", Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter }
					}, ContentLayer);
				}
			}
            #endregion

            CuiHelper.AddUi(player, container);
        }
		
		public void GetOffsetPos(string pos,double Height, double Width, ref string omin, ref string omax){
			switch(pos){
				case "UPPERLEFT":
					omin = $"0 -{Height}";
					omax = $"{Width} 0";
				break;
				case "UPPERCENTER": 
					omin = $"-{(float)Width/2} -{Height}";
					omax = $"{(float)Width/2} 0";
				break;
				case "UPPERRIGHT":
					omin = $"-{Width} -{Height}";
					omax = $"0 0";
				break; 
				case "MIDDLELEFT":
					omin = $"0 -{(float)Height/2}";
					omax = $"{Width} {(float)Height/2}";
				break;
				case "MIDDLECENTER":
					omin = $"-{(float)Width/2} -{(float)Height/2}";
					omax = $"{(float)Width/2} {(float)Height/2}";
				break;
				case "MIDDLERIGHT":
					omin = $"-{Width} -{(float)Height/2}";
					omax = $"0 {(float)Height/2}";
				break;
				case "BOTTOMLEFT":
					omin = $"0 0";
					omax = $"{Width} {Height}";
				break;
				case "BOTTOMCENTER":
					omin = $"-{(float)Width/2} 0";
					omax = $"{(float)Width/2} {Height}";
				break;
				case "BOTTOMRIGHT":
					omin = $"-{Width} 0";
					omax = $"0 {Height}";
				break;
			}
		}
		
		
		private void InitImage(ref CuiElementContainer container, string png, string ipanel)
		{
			container.Add(new CuiElement
			{
				Name = CuiHelper.GetGuid(),
				Parent = ipanel,				
				Components =
				{
					new CuiRawImageComponent { Png = png },
					new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
				}
			});
		}
		#endregion

        #region Helper
		
		private static string HexToCuiColor(string hex)
		{
			if (string.IsNullOrEmpty(hex))
			{
				hex = "#FFFFFFFF";
			}
			var str = hex.Trim('#');
			if (str.Length == 6)
				str += "FF";
			if (str.Length != 8)
			{
				throw new Exception(hex);
				throw new InvalidOperationException("Cannot convert a wrong format.");
			}
			var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
			var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
			var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
			var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
			Color color = new Color32(r, g, b, a);
			return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
		}
        public static string ParseColorFromRGBA(string cssColor)
        {
            cssColor = cssColor.Trim();
            string[] parts = cssColor.Split(' ');
            int r = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int g = int.Parse(parts[1], CultureInfo.InvariantCulture);
            int b = int.Parse(parts[2], CultureInfo.InvariantCulture);
            float a = float.Parse(parts[3], CultureInfo.InvariantCulture);
            var finish = System.Drawing.Color.FromArgb((int)(a * 255), r, g, b);
            cssColor = "#" + finish.R.ToString("X2") + finish.G.ToString("X2") + finish.B.ToString("X2") + finish.A.ToString("X2");
            var str = cssColor.Trim('#');
            if (str.Length == 6)
                str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(cssColor);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }
            var r1 = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g1 = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b1 = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a1 = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r1, g1, b1, a1);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion
		
    }
}
