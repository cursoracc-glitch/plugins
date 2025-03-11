// UI Connect скачан с сайта Server-rust.ru Сотни новых бесплатных плагинов уже на нашем сайте! 
// Присоеденяйся к нам! Server-rust.ru
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices.ComTypes;
using ConVar;
using Facepunch.Steamworks;
using JetBrains.Annotations;
using Mono.Security.X509;
using UnityEngine;
using Oxide.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using UnityEngine.Networking;
using Console = System.Console;

namespace Oxide.Plugins
{
	[Info("UIConnect", "A1M41K", "1.2.2")]
	class UIConnect : RustPlugin
	{
		#region Значения
		private int FontSizeText = 16;
        private string FontName = "robotocondensed-regular.ttf";
        private bool ShowGUI = false;
		private bool ShowPrefix = true;
		private bool ShowReason = true;
		private bool ShowDisconnect = true;
		private bool showconnectadmin = true;
		private string colorname = "#0081de";
		private string colorprefix = "#0081de";
		private string Prefix = "[UIConnect]";
		private string formatconnectChat = "Присоединился к игре {PLAYER}";
		private string formatLeaveChat = "Отключился от игры {PLAYER} [Причина: {0}]";
		private string formatconnectUI = "Присоединился: {Name}";
		private string AnchorMinUI = "0.01249945 0.03518518";
		private string AnchorMaxUI = "0.2447917 0.07685138";
		#endregion

		#region Hooks

				void OnPlayerInit(BasePlayer player, string[] args)
        		{	
        			timer.Once(5, () =>
        			{ 
        				foreach (var check in BasePlayer.activePlayerList)
        				updateGUI(check, player.net.connection.username);
        				timer.Once(5, () =>
        				{
        					foreach (var check in BasePlayer.activePlayerList)
        					CuiHelper.DestroyUi(check, "ConnectGUI");
        					});
        				});
			        
			        sendJoinMessage(player);
		        }
			
        		void OnServerInitialized()
        		{
        			LoadConfig();
        			LoadDefaultMessages();
        		}
		
		#endregion

		#region UI

				private void updateGUI(BasePlayer player, string Name)
        		{
        			CuiHelper.DestroyUi(player, "ConnectGUI");
        			ConnectGUI(player, Name);
        		}
        
        		private void ConnectGUI(BasePlayer player, string Name)
        		{
        			if (ShowGUI == false)
        			{
				        string message = formatconnectUI.Replace("{Name}", $"{color(colorname, Name)}");
        				var RankElements = new CuiElementContainer();
        				var Choose = RankElements.Add(new CuiPanel
        				{
        					Image = {Color = "0.1 0.1 0.1 0"},
        					RectTransform = {AnchorMin = $"{AnchorMinUI}", AnchorMax = $"{AnchorMaxUI}"},
        				}, "Hud", "ConnectGUI");
        	
        				RankElements.Add(new CuiElement
        				{
        					Parent = "ConnectGUI",
        					Components =
        					{
        						new CuiTextComponent()
        						{
        							Text = message ,
        							Align = TextAnchor.MiddleLeft,
        							Color = "1 1 1 1",
        							Font = FontName,
        							FontSize = FontSizeText
        						},
        						new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"},
        						new CuiOutlineComponent() {Color = "0 0 0 1", Distance = "0.5 1"}
        					}
        				});
        				CuiHelper.AddUi(player, RankElements);
        		    }
        		}

		#endregion
		
		#region config
		private new void LoadDefaultConfig()
		{
			PrintWarning("Создание нового файла конфигурации... | Благодарим за покупку данного плагина");
			LoadConfig();
		}
		
		private new void LoadConfig()
		{
			GetConfig("Настройка Префикса", "Цвет префикса", ref colorprefix);
			GetConfig("Настройка Префикса", "Префикс", ref Prefix);
			GetConfig("Настройка Префикса", "Отображать префикс?", ref ShowPrefix);
			GetConfig("Основные настройки","Показывать оповещение о входе в чат вместо UI", ref ShowGUI);
			GetConfig("Основные настройки", "Показывать отключение игроков", ref ShowDisconnect);
			GetConfig("Основные настройки", "Показывать админа при подключение?", ref showconnectadmin);
			GetConfig("Формат сообщения","Присоединение CHAT", ref formatconnectChat);
			GetConfig("Формат сообщения","Присоединение UI", ref formatconnectUI);
			GetConfig("Формат сообщения","Отключение CHAT", ref formatLeaveChat);
			GetConfig("Настройка UI", "Расположение панели Min", ref AnchorMinUI);
			GetConfig("Настройка UI", "Расположение панели Max", ref AnchorMaxUI);
			GetConfig("Настройка UI","Размер текста", ref FontSizeText);
			GetConfig("Настройка UI", "Цвет ника при подключение", ref colorname);
			GetConfig("Настройка UI","Шрифт Текст", ref FontName);
			SaveConfig();
			
		}
		private void GetConfig<T>(string menu, string Key, ref T var)
		{
			if (Config[menu, Key] != null)
			{
				var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
			}

			Config[menu, Key] = var;
		}
		#endregion
		
		string color(string color, string text)
		{
			return $"<color={color}>{text}</color>";
		}


		string getMessageFormat(BasePlayer player, string type)
		{
			if (type == "Join")
			{
				string message = formatconnectChat.Replace("{PLAYER}", $"{color(colorname, player.displayName)}");
				if (ShowPrefix)
					return $"{color(colorprefix, Prefix)} {message}";
				return message;
			}
			if (type == "Leave")
			{
				string reason;
				string message = formatLeaveChat.Replace("{PLAYER}", $"{color(colorname, player.displayName)}"); 
				if (ShowPrefix)
					return $"{color(colorprefix, Prefix)} {message}";
				return message;
			}
			return "Ошибка";
		}
		
		void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			Puts($"<color=#fff>[{Prefix}]</color> {player.displayName} вышел со сервера [Причина: {reason}]");
			LogToFile("disconnect", $"[{DateTime.Now.ToShortTimeString()}] {player.displayName} вышел со сервера [Причина: {reason}]", this, true);
			
			foreach (BasePlayer sender in BasePlayer.activePlayerList)
				PrintToConsole(sender, $"<color=#fff>{Prefix}</color> <color=#fff>{player.displayName}</color> вышел со сервера [Причина: <color=#fff>{reason}</color>]");
			
			if (ShowDisconnect == true)
			{
				sendLeaveMessage(player, reason);
			}
		}
		
		void sendLeaveMessage(BasePlayer player, string reason)
		{
			if (ShowGUI == true)
			{
				string text = getMessageFormat(player, "Leave");
				foreach (BasePlayer p in BasePlayer.activePlayerList)
					PrintToChat(p, text, reason);
				foreach (BasePlayer sender in BasePlayer.activePlayerList)
					PrintToConsole(sender, text, reason);
			}
		}
		
		void sendJoinMessage(BasePlayer player)
		{
			if (ShowGUI == true)
			{
				string text = getMessageFormat(player, "Join");
				if (showconnectadmin == false)
				{
					if (player.IsAdmin)
					{
						string connect = $"Вы успешно присоединились скрывая личность";
						if (ShowPrefix)
							connect = $"<color={colorprefix}>{Prefix}</color> Вы успешно присоединились скрывая личность";
						PrintToChat(player, connect);
					}
					else
					{
						foreach (BasePlayer p in BasePlayer.activePlayerList)
						{
							PrintToChat(p, text);
							PrintToConsole(p, $"<color=#fff>{Prefix}</color> <color=#fff>{player.displayName}</color> присоединился к игре");
						}		
					}
				}
				else
				{
					foreach (BasePlayer p in BasePlayer.activePlayerList)
						PrintToChat(p, text);
				}
			}
			foreach (BasePlayer sender in BasePlayer.activePlayerList)
				PrintToConsole(sender, $"<color=#fff>{Prefix}</color> <color=#fff>{player.displayName}</color> присоединился к игре");
		}
	}
}