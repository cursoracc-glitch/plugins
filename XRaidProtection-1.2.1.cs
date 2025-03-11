using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using System.Globalization;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("XRaidProtection", "Monster", "1.2.1")]
    class XRaidProtection : RustPlugin
	{
		private const bool LanguageEnglish = false;
		
		#region Reference
		
		[PluginReference] private Plugin IQChat, RaidableBases, AbandonedBases, Convoy;
		
        #endregion
		
	    #region Configuration

        private RaidConfig config;

        private class RaidConfig
        {													
			internal class MessageSetting
			{			
				[JsonProperty(LanguageEnglish ? "Interval between messages. Min - 10 sec" : "Интервал между сообщениями. Мин - 10 сек")] public int TimeMessage;
			}			
			
			internal class TimeSetting
			{
				[JsonProperty(LanguageEnglish ? "Start protection | Hour" : "Начало защиты | Часы")] public int HourStart;
				[JsonProperty(LanguageEnglish ? "Start protection | Minute" : "Начало защиты | Минуты")] public int MinuteStart;
				[JsonProperty(LanguageEnglish ? "End protection | Hour" : "Конец защиты | Часы")] public int HourEnd;
				[JsonProperty(LanguageEnglish ? "End protection | Minute" : "Конец защиты | Минуты")] public int MinuteEnd;
				[JsonProperty(LanguageEnglish ? "Timezone - UTC+0:00" : "Часовой пояс - UTC+0:00")] public int GMT;
				[JsonProperty(LanguageEnglish ? "Protection activity check timer (.sec)" : "Время таймера проверки активности защиты (.сек)")] public int Timer;
				[JsonProperty(LanguageEnglish ? "Use local computer/hosting time. [ Requests to an external time service and time zone will be disabled ]" : "Использовать локальное время компьютера/хостинга. [ Запросы к внешнему сервису точного времени и часовой пояс будут отключены ]")] public bool LocalTime;
			}			
			
			internal class Settings
			{
				[JsonProperty(LanguageEnglish ? "Sound effect" : "Звуковой эффект")] public string Effect;
				[JsonProperty(LanguageEnglish ? "Protection percentage for all players. 1.0 - 100%" : "Процент защиты для всех игроков. 1.0 - 100%")] public float Damage;
				[JsonProperty(LanguageEnglish ? "Protection percentage for all vehicles. 1.0 - 100%" : "Процент защиты для всех транспортных средств. 1.0 - 100%")] public float DamageV;
				[JsonProperty(LanguageEnglish ? "Protected all vehicles" : "Защита всех транспортных средств")] public bool ProtectV;
				[JsonProperty(LanguageEnglish ? "Enable sound effect" : "Включить звуковой эффект")] public bool TEffect;
				[JsonProperty(LanguageEnglish ? "Protection activity only in the first days of the wipe" : "Активность защиты только в первые дни вайпа")] public bool PDay;
				[JsonProperty(LanguageEnglish ? "How protection will work in the early days of the wipe - [ True - all day | False - only in the specified time range ]" : "Как будет работать защита в первые дни вайпа - [ True - круглые сутки | False - только в указанном временном диапазоне ]")] public bool PDayO;
				[JsonProperty(LanguageEnglish ? "How many first days after the wipe will be active protection" : "Сколько первых дней после вайпа будет активной защита")] public int PDays;
				[JsonProperty(LanguageEnglish ? "Percentage of protection by day, only at ( Protection activity only in the first days of the wipe ). If the list is empty or no day is specified, the protection percentage will be taken from ( Protection percentage for all players. 1.0 - 100% )" : "Процент защиты по дням, только при ( Активность защиты только в первые дни вайпа ). Если список пуст или не указан день, процент защиты будет взят из параметра ( Процент защиты для всех игроков. 1.0 - 100% )")] public Dictionary<int, float> ProtectDays;
				[JsonProperty(LanguageEnglish ? "Enable GUI message" : "Включить GUI сообщение")] public bool TGUIMessage;
				[JsonProperty(LanguageEnglish ? "Enable chat message" : "Включить чат сообщение")] public bool TMessage;
				[JsonProperty(LanguageEnglish ? "Allow breaking twigs during defense" : "Разрешить ломать солому во время защиты")] public bool Twigs;
				[JsonProperty(LanguageEnglish ? "Enable protection for players with permission when the main protect is not active" : "Включить защиту для игроков с разрешением, когда основная защита не активна")] public bool PermProtect;
				[JsonProperty(LanguageEnglish ? "Enable protection against damage from helicopters, MLRS, submarines, etc. [ Excluding rot damage ]" : "Включить защиту от урона вертолета, МЛРС, подводных лодок и т.д. [ Кроме урона от гниения ]")] public bool MoreDamage;
				[JsonProperty(LanguageEnglish ? "0 - Protection only for players with permission, 1 - Protection for all players, 2 - Protection for players with permission and for all players" : "0 - Защита только для игроков с пермишеном, 1 - Защита для всех игроков, 2 - Защита для игроков с пермишеном и для всех игроков")] public int TypeProtection;
				[JsonProperty(LanguageEnglish ? "What days of the week protection can be active - [ Does not apply to protection in the early days of the wipe ] - [ Sun - 1, Mon - 2, Tue - 3, Wed - 4, Thu - 5, Fri - 6, Sat - 7 ]" : "В какие дни недели защита может быть активной - [ Не распространяется на защиту в первые дни вайпа ] - [ Вс - 1, Пн - 2, Вт - 3, Ср - 4, Чт - 5, Пт - 6, Сб - 7 ]")] public int[] DayOfWeekProtection = new int[] { 1, 2, 3, 4, 5, 6, 7 };
			}
			
			internal class GUISettings
			{
				[JsonProperty(LanguageEnglish ? "AnchorMin [ When damage is dealt ]" : "AnchorMin [ Когда наносится урон ]")] public string AnchorMinD;
				[JsonProperty(LanguageEnglish ? "AnchorMax [ When damage is dealt ]" : "AnchorMax [ Когда наносится урон ]")] public string AnchorMaxD;
				[JsonProperty(LanguageEnglish ? "OffsetMin [ When damage is dealt ]" : "OffsetMin [ Когда наносится урон ]")] public string OffsetMinD;
				[JsonProperty(LanguageEnglish ? "OffsetMax [ When damage is dealt ]" : "OffsetMax [ Когда наносится урон ]")] public string OffsetMaxD;
				[JsonProperty(LanguageEnglish ? "Use icons [ When damage is dealt ]" : "Использовать иконки [ Когда наносится урон ]")] public bool IconD;
				[JsonProperty(LanguageEnglish ? "AnchorMin [ Status is always displayed ]" : "AnchorMin [ Статус всегда отображается ]")] public string AnchorMinA;
				[JsonProperty(LanguageEnglish ? "AnchorMax [ Status is always displayed ]" : "AnchorMax [ Статус всегда отображается ]")] public string AnchorMaxA;
				[JsonProperty(LanguageEnglish ? "OffsetMin [ Status is always displayed ]" : "OffsetMin [ Статус всегда отображается ]")] public string OffsetMinA;
				[JsonProperty(LanguageEnglish ? "OffsetMax [ Status is always displayed ]" : "OffsetMax [ Статус всегда отображается ]")] public string OffsetMaxA;
				[JsonProperty(LanguageEnglish ? "Use icons [ Status is always displayed ]" : "Использовать иконки [ Статус всегда отображается ]")] public bool IconA;
				[JsonProperty(LanguageEnglish ? "Text color" : "Цвет текста")] public string ColorText;
				[JsonProperty(LanguageEnglish ? "Text size" : "Размер текста")] public int SizeText;
				[JsonProperty(LanguageEnglish ? "Always display UI when protection is active" : "Постоянно отображать UI при активной защите")] public bool ActiveUI;
				[JsonProperty(LanguageEnglish ? "Always display UI when protection is inactive" : "Постоянно отображать UI при неактивной защите")] public bool DeactiveUI;
			}
			
			[JsonProperty(LanguageEnglish ? "Messages in chat and GUI" : "Сообщения в чат и GUI")]
            public MessageSetting Message = new MessageSetting();
			[JsonProperty(LanguageEnglish ? "Setting the protection duration" : "Настройка времени действия защиты")]
            public TimeSetting Time = new TimeSetting();
			[JsonProperty(LanguageEnglish ? "General settings" : "Общее")]
            public Settings Setting = new Settings();
			[JsonProperty(LanguageEnglish ? "GUI settings" : "Настройка GUI")]
            public GUISettings GUI = new GUISettings();
			[JsonProperty(LanguageEnglish ? "List of prefabs that will be protected" : "Список префабов которые будут под защитой")]
            public List<string> Prefabs = new List<string>();
            [JsonProperty(LanguageEnglish ? "Setting permissions - [ Permisssion | The percentage of protection by permission. 1.0 - 100% ]" : "Настройка пермишенов - [ Пермишен | Процент защиты по пермишену. 1.0 - 100% ]")]
			public Dictionary<string, float> Permisssion = new Dictionary<string, float>();
			[JsonProperty(LanguageEnglish ? "Wipe date" : "Дата вайпа")]
            public string DateWipe;
			[JsonProperty(LanguageEnglish ? "Time" : "Время")]
			public DateTime MSCTime;

			public static RaidConfig GetNewConfiguration()
            {
                return new RaidConfig
                {
					Message = new MessageSetting
					{
						TimeMessage = 30
					},
					Time = new TimeSetting
					{
						HourStart = 22,
						MinuteStart = 0,
						HourEnd = 7,
						MinuteEnd = 0,
						GMT = LanguageEnglish ? 0 : 3,
						Timer = 120,
						LocalTime = true
					},
					Setting = new Settings
					{
						Effect = "assets/bundled/prefabs/fx/invite_notice.prefab",
						Damage = 0.5f,
						DamageV = 0.5f,
						ProtectV = false,
						TEffect = true,
						PDay = false,
						PDayO = false,
						PDays = 3,
						ProtectDays = new Dictionary<int, float> { [1] = 1.0f, [2] = 0.7f, [3] = 0.5f },
						TGUIMessage = true,
						TMessage = false,
						Twigs = true,
						PermProtect = false,
						MoreDamage = true,
						TypeProtection = 1,
						DayOfWeekProtection = new int[] { 1, 2, 3, 4, 5, 6, 7 }
						
					},					
					GUI = new GUISettings
					{
						AnchorMinD = "0.5 0",
						AnchorMaxD = "0.5 0",
						OffsetMinD = "-194.5 80",
						OffsetMaxD = "175.5 110",
						IconD = true,
						AnchorMinA = "0.5 0",
						AnchorMaxA = "0.5 0",
						OffsetMinA = "-194.5 -6",
						OffsetMaxA = "175.5 24",
						IconA = false,
						ColorText = "1 1 1 0.4",
						SizeText = 12,
						ActiveUI = true,
						DeactiveUI = true
					},
					Prefabs = new List<string>
					{
						"cupboard.tool.deployed",
						"wall.frame.shopfront.metal"
					},
					Permisssion = new Dictionary<string, float>
					{
						["xraidprotection.elite"] = 0.90f,
						["xraidprotection.perm"] = 0.80f,
						["xraidprotection.vip"] = 0.70f
					}
				}; 
			}			 
		}			

		protected override void LoadConfig()
        {
            base.LoadConfig(); 
			 
			try 
			{ 
				config = Config.ReadObject<RaidConfig>();
			}
			catch  
			{
				PrintWarning(LanguageEnglish ? "Configuration read error! Creating a default configuration!" : "Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			if(config.Setting.ProtectDays == null)
				config.Setting.ProtectDays = new Dictionary<int, float> { [1] = 1.0f, [2] = 0.5f, [3] = 0.3f };
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = RaidConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		#region Commands
		
		[ChatCommand("protect")]
		void cmdProtection(BasePlayer player)
		{
			if(config.Setting.TypeProtection != 0)
				if(_time)
				{
					if(config.Setting.PDay && config.Setting.PDayO)
						Player.Reply(player, string.Format(lang.GetMessage("WIPE_CHAT_MESSAGE", this, player.UserIDString), config.Setting.PDays, config.Setting.ProtectDays.Count == 0 ? $"{config.Setting.Damage * 100}" : $"{config.Setting.ProtectDays.First().Value * 100}-{config.Setting.ProtectDays.Last().Value * 100}") + string.Format(lang.GetMessage("LEFT", this, player.UserIDString), (config.MSCTime - DateTime.ParseExact(config.DateWipe, "dd/MM/yyyy", CultureInfo.InvariantCulture).AddDays(config.Setting.PDays)).ToString("dd' d.  'hh':'mm':'ss")));
					else
						Player.Reply(player, lang.GetMessage("P_YES_ACTIVE", this, player.UserIDString));
				}
				else
					Player.Reply(player, lang.GetMessage("P_NO_ACTIVE", this, player.UserIDString));
		}
		
		#endregion
		
		#region Hooks
		
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Author - Monster\n" +
			"     VK - vk.com/idannopol\n" +
			"     Discord - Monster#4837\n" +
			"-----------------------------");
			
			foreach(var perm in config.Permisssion)
			    permission.RegisterPermission(perm.Key, this);
			
			On = new TimeSpan(config.Time.HourStart, config.Time.MinuteStart, 0);
			Off = new TimeSpan(config.Time.HourEnd, config.Time.MinuteEnd, 0);
			
			timer.Once(5, () => BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected));
			
			MSC();
			InitializeLang();
			timer.Once(1, () => 
			{
				config.DateWipe = SaveRestore.SaveCreatedTime.ToString("dd/MM/yyyy");
				SaveConfig();
			});
			timer.Every(config.Time.Timer, () => MSC());
		}
		
		private void Unload()
		{
			foreach(var player in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(player, ".MessagePanel");
				CuiHelper.DestroyUi(player, ".Icon1");
				CuiHelper.DestroyUi(player, ".Icon2");				
				
				CuiHelper.DestroyUi(player, ".MessagePanelX");
				CuiHelper.DestroyUi(player, ".Icon1X");
				CuiHelper.DestroyUi(player, ".Icon2X");
			}
		}
		
		private void OnPlayerConnected(BasePlayer player)
		{
			if(player.IsReceivingSnapshot)
			{
				NextTick(() => OnPlayerConnected(player));
				return;
			}
			
			if(config.GUI.ActiveUI && _time || config.GUI.DeactiveUI && !_time)
				Message(player, "", false);
		}
		
		private void MSC()
		{
			if(config.Time.LocalTime)
			{
				config.MSCTime = DateTime.Now;
				SaveConfig();
			}
			else
				webrequest.Enqueue("http://worldtimeapi.org/api/timezone/Europe/London", null, (code, response) =>
				{
					if(code != 200 || response == null) return;
				
					config.MSCTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(double.Parse(JsonConvert.DeserializeObject<JObject>(response)["unixtime"].ToString()) + (config.Time.GMT * 3600));
					SaveConfig();
				}, this);
				
			timer.Once(3, () =>
			{
				bool time_1 = _time;
				
				_time = Time();
				
				if(!time_1 && _time)
				{
					foreach(var player in BasePlayer.activePlayerList)
					{
						OnPlayerConnected(player);
						Player.Reply(player, lang.GetMessage("P_ACTIVE", this, player.UserIDString));
					}
				}
				
				if(time_1 && !_time)
				{
					foreach(var player in BasePlayer.activePlayerList)
						Player.Reply(player, lang.GetMessage("P_DEACTIVE", this, player.UserIDString));
						
					if(!config.GUI.DeactiveUI) Unload();
					else BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
				}
			});
		} 
		
		private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if(entity == null || info == null || info.damageTypes.Has(Rust.DamageType.Decay)) return;
			
			if(EventTerritory(entity.transform.position)) return;
			
            if(_time)
            {
				if(config.Setting.ProtectV)
					if((entity is BaseVehicle || entity is HotAirBalloon) && info.InitiatorPlayer != null && (!Convoy?.Call<bool>("IsConvoyVehicle", entity) ?? true))
						if(info.InitiatorPlayer.userID.IsSteamId())
						{
							info.damageTypes.ScaleAll(1.0f - config.Setting.DamageV);
							return;
						}
				
                if(config.Setting.TypeProtection == 0 || config.Setting.TypeProtection == 2)
                    foreach(var perm in config.Permisssion)
                        if(permission.UserHasPermission(entity.OwnerID.ToString(), perm.Key))
                        {
                            Protection(entity, info, perm.Value);
                            return;
                        }
                
                if(config.Setting.TypeProtection == 1 || config.Setting.TypeProtection == 2)
                    Protection(entity, info, config.Setting.PDay && config.Setting.ProtectDays.ContainsKey(_day) ? config.Setting.ProtectDays[_day] : config.Setting.Damage);
            }
            else if(config.Setting.PermProtect)
            {
                foreach(var perm in config.Permisssion)
                    if(permission.UserHasPermission(entity.OwnerID.ToString(), perm.Key))
                    {
                        Protection(entity, info, perm.Value);
                        break;
                    }
            }
		}
		
		private void Protection(BaseCombatEntity entity, HitInfo info, float damage)
		{
				bool ent = entity is BuildingBlock;
				
				if(config.Setting.Twigs && ent)
					if((entity as BuildingBlock).grade == BuildingGrade.Enum.Twigs)
						return;
				
		        if(ent || entity is Door || entity is SimpleBuildingBlock || config.Prefabs.Contains(entity.ShortPrefabName))
			    {
					BasePlayer player = info.InitiatorPlayer;
					var dm = info.damageTypes.Total();
					
					if(config.Setting.MoreDamage && player == null || player != null)
						info.damageTypes.ScaleAll(1.0f - damage);
					
					if(dm >= 1.5 && player != null)
					{
						if(Cooldowns.ContainsKey(player))
							if(Cooldowns[player].Subtract(DateTime.Now).TotalSeconds >= 0) return;
						
						string s_damage = $"{damage * 100}";
						
						if(config.Setting.TEffect) EffectNetwork.Send(new Effect(config.Setting.Effect, player, 0, new Vector3(), new Vector3()), player.Connection);
						if(config.Setting.TGUIMessage) Message(player, s_damage);
						if(config.Setting.TMessage)
						{
							string message = config.Setting.PDay && config.Setting.PDayO ? string.Format(lang.GetMessage("WIPE_CHAT_MESSAGE", this, player.UserIDString), config.Setting.PDays, s_damage) : s_damage == "100" ? string.Format(lang.GetMessage("FULL_CHAT_MESSAGE", this, player.UserIDString), s_damage, Off.ToString(@"hh\:mm"), On.ToString(@"hh\:mm")) : string.Format(lang.GetMessage("CHAT_MESSAGE", this, player.UserIDString), s_damage, On.ToString(@"hh\:mm"), Off.ToString(@"hh\:mm"));
							
							if(IQChat)
								IQChatPuts(player, message);
							else
								SendReply(player, message);
						}
					
						if(config.Message.TimeMessage < 10)
							Cooldowns[player] = DateTime.Now.AddSeconds(10);
						else
							Cooldowns[player] = DateTime.Now.AddSeconds(config.Message.TimeMessage);
					}
			    }
		}
		
		private bool EventTerritory(Vector3 entityPos) => Convert.ToBoolean(RaidableBases?.Call("EventTerritory", entityPos)) || Convert.ToBoolean(AbandonedBases?.Call("EventTerritory", entityPos));
		
		#endregion
		
		#region DateTime
		
		private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();
		TimeSpan On, Off;
		bool _time;
		int _day;
		
		private bool Time()
		{
			if(config.Setting.PDay)
			{
				DateTime days = config.MSCTime - DateTime.ParseExact(config.DateWipe, "dd/MM/yyyy", CultureInfo.InvariantCulture).Subtract(new DateTime(1970, 1, 1));
				int d = days.Subtract(new DateTime(1970, 1, 1)).Days;
				 
				_day = 1 + d;
				 
				if(config.Setting.PDayO)
					return config.Setting.PDays > d;
				else
					return config.Setting.PDays > d && IsRange();
			}
			else
				if(config.Setting.DayOfWeekProtection.Contains((int)config.MSCTime.DayOfWeek + 1))
					return IsRange();
				else
					return false;
		}
		
		private bool IsRange()
		{
			int startTime = 100010;
			var Now = config.MSCTime.TimeOfDay;
			
			if(On < Off)
				return On <= Now && Now <= Off;
			
			return !(Off < Now && Now < On);
		}
		
		#endregion 
		 
		#region Message

        private void Message(BasePlayer player, string s_damage, bool destroy = true) 
        {
            CuiElementContainer container = new CuiElementContainer();
			
			if(destroy)
			{
				container.Add(new CuiLabel
				{
					FadeOut = 0.75f,
					RectTransform = { AnchorMin = config.GUI.AnchorMinD, AnchorMax = config.GUI.AnchorMaxD, OffsetMin = config.GUI.OffsetMinD, OffsetMax = config.GUI.OffsetMaxD },
					Text = { FadeIn = 0.75f, Text = config.Setting.PDay && config.Setting.PDayO ? string.Format(lang.GetMessage("WIPE_UI_MESSAGE", this, player.UserIDString), config.Setting.PDays, s_damage) : s_damage == "100" ? string.Format(lang.GetMessage("FULL_UI_MESSAGE", this, player.UserIDString), s_damage, Off.ToString(@"hh\:mm"), On.ToString(@"hh\:mm")) : string.Format(lang.GetMessage("UI_MESSAGE", this, player.UserIDString), s_damage, On.ToString(@"hh\:mm"), Off.ToString(@"hh\:mm")), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = config.GUI.SizeText, Color = config.GUI.ColorText }
				}, "Hud", ".MessagePanel", ".MessagePanel");
			
				if(config.GUI.IconD) 
				{
					container.Add(new CuiButton
					{
						FadeOut = 0.75f,
						RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-180 2", OffsetMax = "-154 28" },
						Button = { Color = "0.8 0.5 0.5 0.8", Sprite = "assets/icons/vote_down.png" },
						Text = { Text = "" }
					}, ".MessagePanel", ".Icon1");			
			
					container.Add(new CuiButton
					{
						FadeOut = 0.75f,
						RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "154 2", OffsetMax = "180 28" },
						Button = { Color = "0.8 0.5 0.5 0.8", Sprite = "assets/icons/vote_down.png" },
						Text = { Text = "" }
					}, ".MessagePanel", ".Icon2");
				}
			}
			else
			{
				container.Add(new CuiLabel
				{
					FadeOut = 0.75f,
					RectTransform = { AnchorMin = config.GUI.AnchorMinA, AnchorMax = config.GUI.AnchorMaxA, OffsetMin = config.GUI.OffsetMinA, OffsetMax = config.GUI.OffsetMaxA },
					Text = { FadeIn = 0.75f, Text = _time ? lang.GetMessage("P_YES_ACTIVE", this, player.UserIDString) : lang.GetMessage("P_NO_ACTIVE", this, player.UserIDString), Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = config.GUI.SizeText, Color = config.GUI.ColorText }
				}, "Hud", ".MessagePanelX", ".MessagePanelX");
			
				if(config.GUI.IconA) 
				{
					container.Add(new CuiButton
					{
						FadeOut = 0.75f,
						RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-180 2", OffsetMax = "-154 28" },
						Button = { Color = "0.8 0.5 0.5 0.8", Sprite = "assets/icons/vote_down.png" },
						Text = { Text = "" }
					}, ".MessagePanelX", ".Icon1X");			
			
					container.Add(new CuiButton
					{
						FadeOut = 0.75f,
						RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "154 2", OffsetMax = "180 28" },
						Button = { Color = "0.8 0.5 0.5 0.8", Sprite = "assets/icons/vote_down.png" },
						Text = { Text = "" }
					}, ".MessagePanelX", ".Icon2X");
				}
			}

            CuiHelper.AddUi(player, container);  

			if(destroy)
				timer.Once(7.5f, () => { CuiHelper.DestroyUi(player, ".MessagePanel"); CuiHelper.DestroyUi(player, ".Icon1"); CuiHelper.DestroyUi(player, ".Icon2"); });
        }

        #endregion
		
		#region Lang
 
        private void InitializeLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MESSAGE"] = "NIGHT RAIDING PROTECTION ACTIVE: {0}%. FROM {1} TO {2}!",
                ["CHAT_MESSAGE"] = "<size=12>NIGHT RAIDING PROTECTION ACTIVE: {0}%. FROM {1} TO {2}!</size>",
				["FULL_UI_MESSAGE"] = "PROTECTION: {0}%. RAIDING IS ALLOWED FROM {1} TO {2}!",
                ["FULL_CHAT_MESSAGE"] = "<size=12>PROTECTION: {0}%. RAIDING IS ALLOWED FROM {1} TO {2}!</size>",
				["WIPE_UI_MESSAGE"] = "IN THE FIRST {0} WIPE DAYS PROTECTION IN ACTIVITY! {1}%",
                ["WIPE_CHAT_MESSAGE"] = "<size=12>IN THE FIRST {0} WIPE DAYS PROTECTION IN ACTIVITY! {1}%</size>",
				["P_ACTIVE"] = "<color=#f74d4d90>NIGHT RAID PROTECTION IS ACTIVATED!</color>",
				["P_DEACTIVE"] = "<color=#4ad44890>NIGHT RAID PROTECTION IS DEACTIVATED!</color>",
				["P_NO_ACTIVE"] = "NIGHT RAID PROTECTION IS NOT ACTIVE NOW!",
				["P_YES_ACTIVE"] = "NIGHT RAID PROTECTION IS NOW ACTIVE!",
				["LEFT"] = "\n<size=10>LEFT: {0}</size>"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MESSAGE"] = "АКТИВНА НОЧНАЯ ЗАЩИТА ОТ РЕЙДА: {0}%. С {1} ДО {2}!",
                ["CHAT_MESSAGE"] = "<size=12>АКТИВНА НОЧНАЯ ЗАЩИТА ОТ РЕЙДА: {0}%. С {1} ДО {2}!</size>",
				["FULL_UI_MESSAGE"] = "ЗАЩИТА: {0}%. РЕЙДИТЬ РАЗРЕШЕНО С {1} ДО {2}!",
                ["FULL_CHAT_MESSAGE"] = "<size=12>ЗАЩИТА: {0}%. РЕЙДИТЬ РАЗРЕШЕНО С {1} ДО {2}!</size>",
				["WIPE_UI_MESSAGE"] = "В ПЕРВЫЕ {0} ДНЯ ВАЙПА ДЕЙСТВУЕТ ЗАЩИТА! {1}%",
                ["WIPE_CHAT_MESSAGE"] = "<size=12>В ПЕРВЫЕ {0} ДНЯ ВАЙПА ДЕЙСТВУЕТ ЗАЩИТА! {1}%</size>",
				["P_ACTIVE"] = "<color=#f74d4d90>НОЧНАЯ ЗАЩИТА ОТ РЕЙДА АКТИВИРОВАНА!</color>",
				["P_DEACTIVE"] = "<color=#4ad44890>НОЧНАЯ ЗАЩИТА ОТ РЕЙДА ДЕАКТИВИРОВАНА!</color>",
				["P_NO_ACTIVE"] = "НОЧНАЯ ЗАЩИТА ОТ РЕЙДА СЕЙЧАС НЕ АКТИВНА!",
				["P_YES_ACTIVE"] = "НОЧНАЯ ЗАЩИТА ОТ РЕЙДА СЕЙЧАС АКТИВНА!",
				["LEFT"] = "\n<size=10>ОСТАЛОСЬ: {0}</size>"
            }, this, "ru");
			
			lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MESSAGE"] = "АКТИВНИЙ НІЧНИЙ ЗАХИСТ ВІД РЕЙДУ: {0}%. З {1} ДО {2}!",
                ["CHAT_MESSAGE"] = "<size=12>АКТИВНИЙ НІЧНИЙ ЗАХИСТ ВІД РЕЙДУ: {0}%. З {1} ДО {2}!</size>",
				["FULL_UI_MESSAGE"] = "ЗАХИСТ: {0}%. РЕЙДИТИ ДОЗВОЛЕНО З {1} ДО {2}!",
                ["FULL_CHAT_MESSAGE"] = "<size=12>ЗАХИСТ: {0}%. РЕЙДИТИ ДОЗВОЛЕНО З {1} ДО {2}!</size>",
				["WIPE_UI_MESSAGE"] = "У ПЕРШІ {0} ДНЯ ВАЙПУ ДІЄ ЗАХИСТ! {1}%",
                ["WIPE_CHAT_MESSAGE"] = "<size=12>У ПЕРШІ {0} ДНЯ ВАЙПУ ДІЄ ЗАХИСТ! {1}%</size>",
				["P_ACTIVE"] = "<color=#f74d4d90>НІЧНИЙ ЗАХИСТ ВІД РЕЙДУ АКТИВОВАНИЙ!</color>",
				["P_DEACTIVE"] = "<color=#4ad44890>НІЧНИЙ ЗАХИСТ ВІД РЕЙДУ ДЕАКТИВОВАНИЙ!</color>",
				["P_NO_ACTIVE"] = "НІЧНИЙ ЗАХИСТ ВІД РЕЙДУ ЗАРАЗ НЕ АКТИВНИЙ!",
				["P_YES_ACTIVE"] = "НІЧНИЙ ЗАХИСТ ВІД РЕЙДУ ЗАРАЗ АКТИВНИЙ!",
				["LEFT"] = "\n<size=10>ЗАЛИШИЛОСЯ: {0}</size>"
            }, this, "uk");
			
			lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MESSAGE"] = "PROTECCIÓN CONTRA ATAQUES NOCTURNOS ACTIVADA: {0}%. DE {1} A {2}!",
                ["CHAT_MESSAGE"] = "<size=12>PROTECCIÓN CONTRA ATAQUES NOCTURNOS ACTIVADA: {0}%. DE {1} A {2}!</size>",
				["FULL_UI_MESSAGE"] = "PROTECCIÓN: {0}%. SE PERMITE ASALTAR DE {1} A {2}!",
                ["FULL_CHAT_MESSAGE"] = "<size=12>PROTECCIÓN: {0}%. SE PERMITE ASALTAR DE {1} A {2}!</size>",
				["WIPE_UI_MESSAGE"] = "EN LOS PRIMEROS {0} DÍAS LIMPIEZA ¡PROTECCIÓN EN ACTIVIDAD! {1}%",
                ["WIPE_CHAT_MESSAGE"] = "<size=12>EN LOS PRIMEROS {0} DÍAS LIMPIEZA ¡PROTECCIÓN EN ACTIVIDAD! {1}%</size>",
				["P_ACTIVE"] = "<color=#f74d4d90>¡LA PROTECCIÓN CONTRA RAID NOCTURNO ESTÁ ACTIVADA!</color>",
				["P_DEACTIVE"] = "<color=#4ad44890>¡LA PROTECCIÓN CONTRA RAID NOCTURNO ESTÁ DESACTIVADA!</color>",
				["P_NO_ACTIVE"] = "<size=10>¡LA PROTECCIÓN CONTRA RAID NOCTURNO NO ESTÁ ACTIVADA AHORA!</size>",
				["P_YES_ACTIVE"] = "¡LA PROTECCIÓN CONTRA RAID NOCTURNO YA ESTÁ ACTIVA!",
				["LEFT"] = "\n<size=10>IZQUIERDA: {0}</size>"
            }, this, "es-ES");
			
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MESSAGE"] = "DÉFENSE CONTRE LES RAIDS NOCTURNES ACTIVE: {0}%. DE {1} À {2}!",
                ["CHAT_MESSAGE"] = "<size=12>DÉFENSE CONTRE LES RAIDS NOCTURNES ACTIVE: {0}%. DE {1} À {2}!</size>",
				["FULL_UI_MESSAGE"] = "PROTECTION: {0}%. LES RAIDS SONT AUTORISÉS DE {1} À {2}!",
                ["FULL_CHAT_MESSAGE"] = "<size=12>PROTECTION: {0}%. LES RAIDS SONT AUTORISÉS DE {1} À {2}!</size>",
				["WIPE_UI_MESSAGE"] = "PROTECTION POUR LES {0} PREMIERS JOURS DE LA VYP! {1}%",
                ["WIPE_CHAT_MESSAGE"] = "<size=12>PROTECTION POUR LES {0} PREMIERS JOURS DE LA VYP! {1}%</size>",
				["P_ACTIVE"] = "<color=#f74d4d90>DÉFENSE CONTRE LES RAIDS NOCTURNES ACTIVÉE!</color>",
				["P_DEACTIVE"] = "<color=#4ad44890>DÉFENSE CONTRE LES RAIDS NOCTURNES DÉSACTIVÉE!</color>",
				["P_NO_ACTIVE"] = "LA DÉFENSE CONTRE LES RAIDS NOCTURNES EST DÉSORMAIS INACTIVE !",
				["P_YES_ACTIVE"] = "LA DÉFENSE CONTRE LES RAIDS NOCTURNES EST DÉSORMAIS ACTIVE!",
				["LEFT"] = "\n<size=10>RESTANTES: {0}</size>"
            }, this, "fr");
			
			Dictionary<string, string> PT_BR = new Dictionary<string, string>
			{
                ["UI_MESSAGE"] = "DEFESA DE ATAQUE NOCTURNO ACTIVA: {0}%. DAS {1} ÀS {2}!",
                ["CHAT_MESSAGE"] = "<size=12>DEFESA DE ATAQUE NOCTURNO ACTIVA: {0}%. DAS {1} ÀS {2}!</size>",
				["FULL_UI_MESSAGE"] = "PROTECÇÃO: {0}%. A INVASÃO É PERMITIDA DAS {1} ÀS {2}!",
                ["FULL_CHAT_MESSAGE"] = "<size=12>PROTECÇÃO: {0}%. A INVASÃO É PERMITIDA DAS {1} ÀS {2}!</size>",
				["WIPE_UI_MESSAGE"] = "PARA OS PRIMEIROS {0} DIAS DO VYP É A PROTECÇÃO! {1}%",
                ["WIPE_CHAT_MESSAGE"] = "<size=12>PARA OS PRIMEIROS {0} DIAS DO VYP É A PROTECÇÃO! {1}%</size>",
				["P_ACTIVE"] = "<color=#f74d4d90>DEFESA DE ATAQUE NOCTURNO ACTIVADA!</color>",
				["P_DEACTIVE"] = "<color=#4ad44890>DEFESA CONTRA ATAQUES NOCTURNOS DESACTIVADA!</color>",
				["P_NO_ACTIVE"] = "A DEFESA DO ATAQUE NOCTURNO ESTÁ AGORA INACTIVA!",
				["P_YES_ACTIVE"] = "A DEFESA DO ATAQUE NOCTURNO ESTÁ AGORA ACTIVA!",
				["LEFT"] = "\n<size=10>SEGUINTE: {0}</size>"
			};
			
			lang.RegisterMessages(PT_BR, this, "pt-PT");
			lang.RegisterMessages(PT_BR, this, "pt-BR");
        }

        #endregion
		
		#region IQChat API
		
		private void IQChatPuts(BasePlayer player, string Message) => IQChat?.Call("API_ALERT_PLAYER", player, Message);
		
		#endregion
	}
}