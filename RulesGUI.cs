using UnityEngine;
using Rust;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System;
using System.Reflection;
using Oxide.Core;
using System.Linq;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins 
{ 
	[Info("Rules GUI", "Server-Rust", "1.4.9")]
	class RulesGUI : RustPlugin
	{
		List<string> text = new List<string>
		{
			"<size=32><color=#00FF00>Добро пожаловать на Server-Rust ! </color></size>",
			"",
			"<size=24><color=#00FF00></color> Чтобы узнать о доступных биндах наберите в чат команду ->  <color=#00FF00>/bind</color></size>",
			//"<size=24><color=#00FF00></color> Для более подробной информации о сервере используйте ->  <color=#00FF00>/help</color></size>",
			"",
			"<size=24><color=#00FF00></color> Магазин сервера:  <color=#00FF00>Server-Rust.RU</color>    |      Группа VK:  <color=#00FF00></color></size>",
			"",
			"Зарегистрируйся в магазине и получи  <color=#00FF00>25 руб на счет</color> !"
		};
		
		/*void Loaded()
		{
			foreach (BasePlayer current in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(current, "RulesGUI");
				DisplayUI(current);
			}
		}*/

		void Unloaded()
		{
			foreach (BasePlayer current in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(current, "RulesGUI");
			}
		}
		
		void UseUI(BasePlayer player, string msg)
		{ 
			var elements = new CuiElementContainer();

			var mainName = elements.Add(new CuiPanel
			{
				Image =
				{
					Color = "0.1 0.1 0.1 1"
				},
				RectTransform =
				{
					AnchorMin = "0 0",
					AnchorMax = "1 1"
				},
				CursorEnabled = true
			}, "Overlay", "RulesGUI"); 				 
			var Agree = new CuiButton
            {
                Button =
                {
                    Close = mainName,
                    Color = "0 255 0 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.44 0.193",
					AnchorMax = "0.566 0.228"
                },
                Text =
                {
                    Text = "ОК !",
                    FontSize = 22,
                    Align = TextAnchor.MiddleCenter
                }
            };
			elements.Add(new CuiLabel
			{
				Text =
                {
					Text = msg,
					
                    FontSize = 22,
					Font = "robotocondensed-regular.ttf",
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0 0.20",
                    AnchorMax = "1 0.9"
                }
			}, mainName);
			elements.Add(Agree, mainName);
			CuiHelper.AddUi(player, elements);
		}

		void DisplayUI(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(3, () => DisplayUI(player));
				return;
            }
            else 
			{
				string steamId = Convert.ToString(player.userID);
				string msg = "";
				foreach(var rule in text)
				msg = msg + rule.ToString() + "\n \n";
				UseUI(player, msg.ToString());
            }
        }
		
		void OnPlayerInit(BasePlayer player)		
		{
			DisplayUI(player);
		}
		
		[ChatCommand("bind")]
        void bindinfocmd(BasePlayer player, string command)
        {
            if (player == null) return;
            
            player.ChatMessage($"<size=16><color=#00FF00>》</color> Бинды вводить в консоли <color=#00FF00>F1</color> !</size>");
            player.ChatMessage($"<size=16><color=#00FF00>》</color> Карта на клавишу <color=#00FF00>M</color><size=7>\n\n</size>   <color=#00FF00>bind M map.open</color></size>");
            player.ChatMessage($"<size=16><color=#00FF00>》</color> Киты на клавишу <color=#00FF00>K</color><size=7>\n\n</size>   <color=#00FF00>bind K kit</color></size>");
            player.ChatMessage($"<size=16><color=#00FF00>》</color> Принять ТП на клавишу <color=#00FF00>Z</color><size=7>\n\n</size>   <color=#00FF00>bind Z tpa</color></size>");
            player.ChatMessage($"<size=16><color=#00FF00>》</color> Ремув на клавишу <color=#00FF00>DELETE</color><size=7>\n\n</size>   <color=#00FF00>bind DELETE remove</color></size>");
		}
	}
}