//==================================================================\\
//В Китах с Oxide в любое удобно место (Только не ломайте структуру)
//==================================================================\\
//   [HookMethod("GetPlayerKitList")]
//    private List<object[]> GetPlayerKitList(BasePlayer player)
//    {
//        List<object[]> kitsInfo = new List<object[]>();
//        string reason = string.Empty;
//
//       foreach (var pair in storedData.Kits)
//        {
//            var kitData = GetKitData(player.userID, pair.Key);
//            var cansee = CanSeeKit(player, pair.Key, false, out reason);
//           var time = CanSeeKit(player, pair.Key, false, out reason);
//            if (!cansee && string.IsNullOrEmpty(reason)) continue;
//            object[] kitInfo = new object[7];
//            kitInfo[0] = cansee;
//            kitInfo[1] = pair.Value.name;
//            kitInfo[2] = pair.Value.description;
//            kitInfo[3] = reason;
//            kitInfo[4] = pair.Value.max < 0 ? "0" : (pair.Value.max - kitData.max).ToString();
//            kitInfo[5] = pair.Value.cooldown <= 0 ? string.Empty : CurrentTime() > kitData.cooldown ? "0" : Math.Abs(Math.Ceiling(CurrentTime() - kitData.cooldown)).ToString();
//            kitInfo[6] = pair.Value.max;
//            kitsInfo.Add(kitInfo);
//        }
//        return kitsInfo;
//    }
//
//   [HookMethod("AvailabilityKit")]
//    private void AvailabilityKit(BasePlayer player, string kitname)
//    {
//        TryGiveKit(player, kitname);
//    }
//==================================================================\\

using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins 
{
	[Info("KitsUI", "OxideBro / RustPlugin.ru", "0.1.0")]
	[Description("UI оболочка для китов с Oxide - https://oxidemod.org/plugins/kits.668/")]
	class KitsUI : RustPlugin
	{
		[PluginReference]         
		private Plugin Kits;
		
		private Dictionary<BasePlayer, int> openUI = new Dictionary<BasePlayer, int>();
		private void OnServerInitialized()
		{
			if (Kits == null)
			{
				PrintError("Плагин 'Kits' не найден, работа плагина не возможна"); 
			}

			LoadDefaultConfig();
			timer.Every(1f, TimerHandle);
		}

		void TimerHandle() 
		{
			foreach (var player in openUI)
			{
				DrawKitsUI(player.Key, player.Value);
			}
		}

		public string BackroungPanelColor = "0 0 0 0.3";
		protected override void LoadDefaultConfig()
		{
			GetVeriables("UI", "Цвет фона китов GUI", ref BackroungPanelColor);
			SaveConfig();
		}
		
		[ChatCommand("kits")]         
		void cmdChatKitsUI(BasePlayer player)
		{
			if (player == null) return;
			if (openUI.ContainsKey(player))
			{
				openUI[player] = 0;
			}
			DrawKitsUI(player, 0);
		}
		
		[ConsoleCommand("kitsgui_show")]
		void cmdShowKitsUI(ConsoleSystem.Arg arg)
		{
			var player = arg.Connection.player as BasePlayer;
			if (player == null) return;
			if (openUI.ContainsKey(player))
			{
				openUI[player] = 0; 
			}
			DrawKitsUI(player, 0);
		}
		
		[ConsoleCommand("destroykitsui")]
		void cmdDestroyKitsUI(ConsoleSystem.Arg arg)
		{
			var player = arg.Connection.player as BasePlayer;
			if (player == null) return;
			DestroyKitsUI(player);
			openUI.Remove(player);
		}

		void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList) 
			DestroyKitsUI(player);
		}
		
		[ConsoleCommand("trytogetkit")]
		void cmdTryToGetKit(ConsoleSystem.Arg arg)
		{
			var player = arg.Connection.player as BasePlayer;
			if (arg.Args.Length > 0)
			{
				Kits?.Call("AvailabilityKit", player, arg.Args[0]);
				if (openUI.ContainsKey(player))
				{
					DestroyKitsUI(player);
					DrawKitsUI(player, openUI[player]);
				}
			}
		}
		
		[ConsoleCommand("trytogetkit_nextpage")]
		void cmdTryToGetKitNextPage(ConsoleSystem.Arg arg)
		{
			var player = arg.Connection.player as BasePlayer;
			if (arg.Args.Length > 0)
			{
				openUI[player] = int.Parse(arg.Args[0]);
				DrawKitsUI(player, int.Parse(arg.Args[0]));
			} 
		}

		private void DestroyKitsUI(BasePlayer player)
		{
			CuiHelper.DestroyUi(player, "KitsUIHUD");
			CuiHelper.DestroyUi(player, "ButtonBG");
		}

		string Button = "[{\"name\":\"Report.Player5\",\"parent\":\"KitsUIHUD\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"{color}\"},{\"type\":\"RectTransform\",\"anchormin\":\"{amin}\",\"anchormax\":\"{amax}\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Report.Player5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2784314\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Report.Player5\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"{command}\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]"; 
		string GUI = "[{\"name\":\"KitsUIHUD\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"{color}\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.45\",\"anchormax\":\"1 0.6\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"KitsUIHUD\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2784314\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"KitsUIHUD\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"{command}\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]"; 
		string ButtonBG = "[{\"name\":\"ButtonBG\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"{color}\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.35 0.38\",\"anchormax\":\"0.65 0.44\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"ButtonBG\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2784314\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"ButtonBG\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"{command}\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]"; string Page = "[{\"name\":\"Report.Player5\",\"parent\":\"ButtonBG\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"{color}\"},{\"type\":\"RectTransform\",\"anchormin\":\"{amin}\",\"anchormax\":\"{amax}\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Report.Player5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.2784314\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Report.Player5\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"{command}\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]"; 
		
		private void DrawKitsUI(BasePlayer player, int page)
		{
			DestroyKitsUI(player);
			var kitsList = Kits?.Call("GetPlayerKitList", player) as List<object[]>;
			var lols = new CuiElementContainer();
			if (kitsList != null && kitsList.Count > 0)
			{
				if (!openUI.ContainsKey(player)) openUI.Add(player, page);
				var reply = 309;
				if (reply == 0) { } 
				int i = 0;
				float gap = 0.01f;
				float width = 0.15f;
				float height = 0.6f;
				float startxBox = 0.015f;
				float startyBox = 0.8f - height;
				float xmin = startxBox;
				float ymin = startyBox;
				if (kitsList.Count > 6)
				{
					CuiHelper.AddUi(player, ButtonBG.Replace("{text}", $"Страница: {page + 1}")
					                                .Replace("{color}", BackroungPanelColor)
							                        .Replace("{command}", ""));
				}

				CuiHelper.AddUi(player, GUI.Replace("{text}", "")
				                           .Replace("{color}", BackroungPanelColor)
										   .Replace("{command}", ""));
	            
				CuiHelper.AddUi(player, Button.Replace("{amin}", "0.97 0.8")
				                              .Replace("{amax}", "0.999 0.998")
											  .Replace("{text}", "X")
											  .Replace("{color}", "0.93 0.35 0.36 1.00")
											  .Replace("{command}", "destroykitsui"));

			    foreach (var kit in kitsList.Skip(6 * page))
				{
					var color = (bool)kit[0] ? "0.41 0.48 0.31 1.00" : "0.74 0.25 0.20 1.00";
					var amin = xmin + " " + ymin;
					var amax = (xmin + width) + " " + (ymin + height * 1);
					var text = Convert.ToUInt32(kit[6]) > 0 ? kit[1] + "\n" + $"Доступно: {kit[4]}" : kit[1];
					if (kit[5].ToString() != "")
					{
						if (Convert.ToUInt32(kit[5]) > 0) text = text + "\n" + $"<size=12>Осталось: {FormatTime(TimeSpan.FromSeconds(Convert.ToUInt32(kit[5])))}</size>";
					}
					var command = "TryToGetKit " + ((string)kitsList[6 * page + i][1]).ToLower();
					DrawKitsInfo(player, amin, amax, color, command, text.ToString());
					xmin += width + gap;
					if (xmin + width >= 1)
					{
						xmin = startxBox; ymin -= height + gap;
					} i++;
					if (i >= 6)
					{
						CuiHelper.AddUi(player, Page.Replace("{amin}", "0.6954611 0.1553825")
						                            .Replace("{amax}", "0.9807224 0.826389")
													.Replace("{text}", "Следующая >")
													.Replace("{color}", "0.41 0.48 0.31 1.00")
													.Replace("{command}", $"trytogetkit_nextpage {page + 1}"));
						break;
					}
				}

				if (page != 0)
				{
					CuiHelper.AddUi(player, Page.Replace("{amin}", "0.01464129 0.1553825")
					                            .Replace("{amax}", "0.2999026 0.826389")
												.Replace("{text}", "< Предыдущая")
												.Replace("{color}", "0.41 0.48 0.31 1.00")
												.Replace("{command}", $"trytogetkit_nextpage {page - 1}"));
				}
			}
			else
			{
				CuiHelper.AddUi(player, GUI.Replace("{text}", "<size=18><b>Извините, но для Вас пока нету доступных китов</b></size>")
				                           .Replace("{color}", BackroungPanelColor)
										   .Replace("{command}", ""));
				
				CuiHelper.AddUi(player, Button.Replace("{amin}", "0.97 0.8")
				                              .Replace("{amax}", "0.999 0.998")
											  .Replace("{text}", "X")
											  .Replace("{color}", "0.93 0.35 0.36 1.00")
											  .Replace("{command}", "destroykitsui"));
			}
		}

		void DrawKitsInfo(BasePlayer player, string amin, string amax, string color, string command, string text)
		{
			CuiHelper.AddUi(player, Button.Replace("{amin}", amin)
			                              .Replace("{amax}", amax)
										  .Replace("{text}", text.ToString())
										  .Replace("{color}", color)
										  .Replace("{command}", command));
		}

		public static string FormatTime(TimeSpan time)
		{
			string result = string.Empty;
			if (time.Days != 0) result += $"{Format(time.Days, "дней", "дня", "день")} ";
			if (time.Hours != 0) result += $"{Format(time.Hours, "часов", "часа", "час")} ";
			if (time.Minutes != 0) result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";
			if (time.Seconds != 0) result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";
			return result;
		}

		private static string Format(int units, string form1, string form2, string form3)
		{
			var tmp = units % 10;
			if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9) 
				return $"{units} {form1}";
			
			if (tmp >= 2 && tmp <= 4) return $"{units} {form2}";
			    return $"{units} {form3}";
		}

		private void GetVeriables<T>(string menu, string Key, ref T var)
		{
			if (Config[menu, Key] != null)
			{
				var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
			}
			Config[menu, Key] = var;
		}
	}
}