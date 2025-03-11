using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XWipeCalendar", "TopPlugin.ru", "3.0.0")]
    class XWipeCalendar : RustPlugin
    {
		#region Configuration

        private CalendarConfig config;

        private class CalendarConfig
        {		          
			internal class GUISetting
			{
				[JsonProperty("Цвет фона_1")]
				public string ColorBackgroundO;					
				[JsonProperty("Цвет фона_2")]
				public string ColorBackgroundT;				
				[JsonProperty("Цвет чисел текучего месяца")]
				public string ColorNumericM;					
				[JsonProperty("Цвет чисел следующего месяца")]
				public string ColorNumericNM;				
				[JsonProperty("Цвет блоков")]
				public string ColorBlock;				
				[JsonProperty("Закрытие календаря нажатием в любой точке экрана")]
				public bool ButtonClose;				
			}
			
			internal class EventsSetting
		    {
				[JsonProperty("Цвет")]
				public string Color;			
				[JsonProperty("Список дней события")]
				public List<int> Day; 
				
				public EventsSetting(string color, List<int> day)
				{
					Color = color; Day = day;
				}
			}
				
			[JsonProperty("Настройка GUI")]
			public GUISetting GUI;			
			[JsonProperty("Список событий. Описание событий - oxide/lang")]
			public List<EventsSetting> Events;
			[JsonProperty("Время по МСК")]			
			public DateTime MSCTime;
			
			public static CalendarConfig GetNewConfiguration()
            {
                return new CalendarConfig
                {
					GUI = new GUISetting
					{
						ColorBackgroundO = "0.517 0.521 0.509 0.95",
						ColorBackgroundT = "0.217 0.221 0.209 0.95",
						ColorNumericM = "1 1 1 0.75",
						ColorNumericNM = "1 1 1 0.1",
						ColorBlock = "0.417 0.421 0.409 0.95",
						ButtonClose = false
					},
					Events = new List<EventsSetting>
					{
						new EventsSetting("0.61 0.18 0.18 1", new List<int> { 3, 16 } ),
						new EventsSetting("0.5 0.18 0.61 1", new List<int> { 5, 38 } ),
						new EventsSetting("0.18 0.21 0.61 1", new List<int> { 8, 14 } ),
						new EventsSetting("0.18 0.49 0.61 1", new List<int> { 21, 34 } ),
						new EventsSetting("0.21 0.61 0.18 1", new List<int> { 11 } ),
						new EventsSetting("0.61 0.55 0.18 1", new List<int> { 29 } ),
						new EventsSetting("0.61 0.31 0.18 1", new List<int> { 25 } )
					}
				};
			}
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<CalendarConfig>();
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = CalendarConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		#region Gommands
		
		[ChatCommand("wipe")]
		private void cmdOpenGUI(BasePlayer player)
		{
			if (permission.UserHasPermission(player.UserIDString, "xwipecalendar.use"))
			    GUI(player);
			else
				SendReply(player, lang.GetMessage("CHATNP", this, player.UserIDString));
		}		
		
		[ChatCommand("calendar")]
		private void cmdOpenGUII(BasePlayer player)
		{
			if (permission.UserHasPermission(player.UserIDString, "xwipecalendar.use"))
			    GUI(player);
			else
				SendReply(player, lang.GetMessage("CHATNP", this, player.UserIDString));
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
			
			permission.RegisterPermission("xwipecalendar.use", this);
			
			InitializeLang();
			MSC();
		}
		
		private void OnServerSave() => MSC();
		
		private void MSC()
		{
			webrequest.Enqueue("https://time100.ru/api.php", null, (code, response) =>
            {
                if (code != 200 || response == null) return;
				
			    config.MSCTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime().AddSeconds(double.Parse(response));
			    Config.WriteObject(config);
            }, this);
		}
		
		#endregion
		
		#region GUI
		
		void GUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, ".OverlayB");
            CuiElementContainer container = new CuiElementContainer();
				
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.276 0", AnchorMax = "0.945 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Menu_UI", ".OverlayB");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-357.5 -267.5", OffsetMax = "357.5 267.5" },
                Image = { Color = config.GUI.ColorBackgroundO, Material = "assets/icons/greyout.mat" }
            }, ".OverlayB", ".Overlay");
			
			if (config.GUI.ButtonClose)
			    container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-350 -125", OffsetMax = "350 125" },
                    Button = { Color = "0 0 0 0", Close = ".OverlayB" },
                    Text = { Text = "" }
                }, ".Overlay");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" },
                Image = { Color = config.GUI.ColorBackgroundT }
            }, ".Overlay", ".OverlayGUI");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-357.5 220", OffsetMax = "357.5 225" },
                Image = { Color = config.GUI.ColorBackgroundO, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".Overlay");
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "319 220", OffsetMax = "314 267.5" },
                Image = { Color = config.GUI.ColorBackgroundO, Material = "assets/content/ui/uibackgroundblur.mat" }
            }, ".Overlay");	
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-310 220", OffsetMax = "350 267.5" },
                Text = { Text = string.Format(lang.GetMessage("TITLE", this, player.UserIDString), lang.GetMessage(config.MSCTime.ToString("MMMM"), this, player.UserIDString), config.MSCTime.Year), Align = TextAnchor.MiddleCenter, FontSize = 18, Color = "1 1 1 0.75" }
            }, ".Overlay");
			
			int x = 0, y = 0, z = 1, h = 0, g = 0, j = 0;
			int daynow = config.MSCTime.Day;
			
			for (int i = 1; i <= 42; i++)
			{
				container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-347.5 + (x * 100)} {132.5 - (y * 65)}", OffsetMax = $"{-252.5 + (x * 100)} {192.5 - (y * 65)}" },
                    Image = { Color = config.GUI.ColorBlock, Material = "assets/content/ui/uibackgroundblur.mat" }
                }, ".OverlayGUI", ".GUII" + i);	
				
				x++;
					
			    if (x == 7)
				{
				    x = 0;
					y++;
					
					if (y == 6)
						break;
				}
			}
			
			int eventscount = config.Events.Count;
			DateTime date = new DateTime(config.MSCTime.Year, config.MSCTime.Month, 1);
			int dayscount = DateTime.DaysInMonth(config.MSCTime.Year, config.MSCTime.Month);
			int dayofweek = (int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek;
			
		    container.Add(new CuiElement
            {
                Parent = ".GUII" + (config.MSCTime.Day + dayofweek - 1),
                Components =
                {
					new CuiImageComponent { Color = "0.417 0.421 0.409 0.95", Material = "assets/content/ui/uibackgroundblur.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
					new CuiOutlineComponent { Color = "0.75 0.75 0.75 1", Distance = "1.5 1.5" }
                }
            });
			
			foreach(var events in config.Events)
			{
				foreach(var eventscolor in events.Day)
				{
					if (eventscolor + dayofweek - 1 > 42) continue;  
					
				    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Image = { Color = events.Color, Material = "assets/content/ui/uibackgroundblur.mat" }
                    }, ".GUII" + (eventscolor + dayofweek - 1));
				}
				
				container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-47.5 - (47.5 * (eventscount + (eventscount * 0.0525 - 0.0525) - 1)) + (g * 100)} -257.5", OffsetMax = $"{47.5 - (47.5 * (eventscount + (eventscount * 0.0525 - 0.0525) - 1)) + (g * 100)} -197.5" },
                    Image = { Color = events.Color, Material = "assets/content/ui/uibackgroundblur.mat" }
                }, ".OverlayGUI", ".EventText");
				
				container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = lang.GetMessage("EVENT" + (1 + g), this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = "1 1 1 0.75" }
                }, ".EventText");
				
				g++;
				
				if (g == 7)
					break;
			}
			
			for (int i = 0; i <= 6; i++)
			{
				container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{-347.5 + (h * 100)} 196", OffsetMax = $"{-252.5 + (h * 100)} 216" },
                    Text = { Text = lang.GetMessage("DAY" + (1 + i), this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = "1 1 1 0.75" }
                }, ".OverlayGUI");
				
				h++;
			}
			
			int count = ++dayscount;

			for (int i = 0; i <= 41; i++)
			{
				container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = $"{z}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 30, Color = j != 0 ? config.GUI.ColorNumericNM : config.GUI.ColorNumericM }
                }, ".GUII" + (i + dayofweek));
				
				z++;
				if (z == count)
				{
					j++;
					z = 1;
				}
			}
			
			CuiHelper.AddUi(player, container);
		}
		
		#endregion
		
		#region Lang

        void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "CALENDAR COOL SERVER | {0} - {1}",					
                ["CHATNP"] = "No permission!",					
			    ["January"] = "JANUARY",
			    ["February"] = "FEBRUARY",
			    ["March"] = "MARCH",
			    ["April"] = "APRIL",
			    ["May"] = "MAY",
			    ["June"] = "JUNE",
			    ["July"] = "JULY",
			    ["August"] = "AUGUST",
			    ["September"] = "SEPTEMBER",
			    ["October"] = "OCTOBER",
			    ["November"] = "NOVEMBER",
			    ["December"] = "DECEMBER",					
			    ["DAY1"] = "MONDAY",
			    ["DAY2"] = "TUESDAY",
			    ["DAY3"] = "WEDNESDAY",
			    ["DAY4"] = "THURSDAY",
			    ["DAY5"] = "FRIDAY",
			    ["DAY6"] = "SATURDAY",
			    ["DAY7"] = "SUNDAY",
                ["EVENT1"] = "EVENT 1",				
                ["EVENT2"] = "EVENT 2",				
                ["EVENT3"] = "EVENT 3",				
                ["EVENT4"] = "EVENT 4",				
                ["EVENT5"] = "EVENT 5",				
                ["EVENT6"] = "EVENT 6",				
                ["EVENT7"] = "EVENT 7",				
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "КАЛЕНДАРЬ КРУТОГО СЕРВЕРА | {0} - {1}",					
                ["CHATNP"] = "Недостаточно прав!",					
			    ["January"] = "ЯНВАРЬ",
			    ["February"] = "ФЕВРАЛЬ",
			    ["March"] = "МАРТ",
			    ["April"] = "АПРЕЛЬ",
			    ["May"] = "МАЙ",
			    ["June"] = "ИЮНЬ",
			    ["July"] = "ИЮЛЬ",
			    ["August"] = "АВГУСТ",
			    ["September"] = "СЕНТЯБРЬ",
			    ["October"] = "ОКТЯБРЬ",
			    ["November"] = "НОЯБРЬ",
			    ["December"] = "ДЕКАБРЬ",					
			    ["DAY1"] = "ПОНЕДЕЛЬНИК",
			    ["DAY2"] = "ВТОРНИК",
			    ["DAY3"] = "СРЕДА",
			    ["DAY4"] = "ЧЕТВЕРГ",
			    ["DAY5"] = "ПЯТНИЦА",
			    ["DAY6"] = "СУББОТА",
			    ["DAY7"] = "ВОСКРЕСЕНЬЕ",
                ["EVENT1"] = "СОБЫТИЕ 1",				
                ["EVENT2"] = "СОБЫТИЕ 2",				
                ["EVENT3"] = "СОБЫТИЕ 3",				
                ["EVENT4"] = "СОБЫТИЕ 4",				
                ["EVENT5"] = "СОБЫТИЕ 5",				
                ["EVENT6"] = "СОБЫТИЕ 6",				
                ["EVENT7"] = "СОБЫТИЕ 7"				
            }, this, "ru");            
			
			lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TITLE"] = "SERVIDOR DE CALENDARIO COOL | {0} - {1}",					
                ["CHATNP"] = "¡No hay suficientes derechos!",					
			    ["January"] = "ENERO",
			    ["February"] = "FEBRERO",
			    ["March"] = "MARZO",
			    ["April"] = "ABRIL",
			    ["May"] = "MAYO",
			    ["June"] = "JUNIO",
			    ["July"] = "JULIO",
			    ["August"] = "AGOSTO",
			    ["September"] = "SEPTIMBRE",
			    ["October"] = "OCTUBRE",
			    ["November"] = "NOVIEMBRE",
			    ["December"] = "DECIEMBRE",					
			    ["DAY1"] = "LUNES",
			    ["DAY2"] = "MARTES",
			    ["DAY3"] = "MIERCOLES",
			    ["DAY4"] = "JUEVES",
			    ["DAY5"] = "VIERNES",
			    ["DAY6"] = "SABADO",
			    ["DAY7"] = "DOMINGO",
                ["EVENT1"] = "EVENTO 1",				
                ["EVENT2"] = "EVENTO 2",				
                ["EVENT3"] = "EVENTO 3",				
                ["EVENT4"] = "EVENTO 4",				
                ["EVENT5"] = "EVENTO 5",				
                ["EVENT6"] = "EVENTO 6",				
                ["EVENT7"] = "EVENTO 7",					
            }, this, "es-ES");
        }

        #endregion
	}
}