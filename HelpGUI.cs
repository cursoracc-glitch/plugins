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
	[Info("Help GUI", "reyzi", "1.0")]
	[Description("Плагин отображает экран помощи")] 
	class HelpGUI : RustPlugin
	{ 
		private bool Changed;
		private string text;
		private string backroundimageurl;
		
		void Loaded()  
		{
			permission.RegisterPermission("helpgui.use", this);
			data = Interface.GetMod().DataFileSystem.ReadObject<Data>("HelpGUIdata");
			LoadVariables();
		}
		
		object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;  
        } 
		
		void LoadVariables() 
		{
			text = Convert.ToString(GetConfig("Messages", "HELP_MESSAGE", new List<string>{
				"<color=lime> - - - - - - - - - -Помощь - - - - - - - - - -</color>\n<color=lime>/tpr</color> <color=purple>- Телепортироваться к игроку</color>\n<color=lime>/pm</color> <color=purple>- Написать сообщение игроку</color>\n<color=lime>/home</color> <color=purple>- Посмотреть команды плагина /home</color>\n<color=lime>/clan</color> <color=purple>- Посмотреть информацию о кланах</color>\n<color=lime>/kit</color> <color=purple>- Посмотреть все доступные киты для игроков</color>\n<color=lime>/remove</color> <color=purple>- Удаление ваших построек</color>\n<color=lime>/friend</color> <color=purple>- Посмотреть как добавить в друзья</color>\n<color=lime>/ad</color> <color=purple>- Авто-Закрытие дверей</color>\n<color=lime>/rec</color> <color=purple>- Переработчик</color>\n<color=lime>/sil</color> <color=purple>- Загрузить фото на табличку</color>\n<color=lime>/trade</color> <color=purple>- Обмен с игроком ресурсами</color>\n\n\n\n\n\n<size=12>Хуган и базука 2 петуха</size>"
			}));
			
			if (Changed)
			{
				SaveConfig();
				Changed = false;
			
			}	
		}
		
		protected override void LoadDefaultConfig()
		{
			Puts("Создание нового файла конфигурации!");
			Config.Clear();
			LoadVariables();
		}


		

		class Data
		{
			public List<string> Players = new List<string>{};
		}


		Data data;

		void Unloaded()
		{
			foreach (BasePlayer current in BasePlayer.activePlayerList)
			{
				CuiHelper.DestroyUi(current, "HelpGUI");
			}
		}
		
		
		void UseUI(BasePlayer player, string msg)
		{ 
			var elements = new CuiElementContainer();

			var mainName = elements.Add(new CuiPanel
			{
				Image =
				{
					Color = "0.1 0.1 0.1 0.9"
				},
				RectTransform =
				{
					AnchorMin = "0.343 0.237",
					AnchorMax = "0.641 0.832"
				},
				CursorEnabled = true
			}, "Overlay", "HelpGUI"); 			 
			var Agree = new CuiButton
            {
                Button =
                {
                    Close = mainName,
                    Color = "0.48 0.48 0.48 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.949 0.949",
					AnchorMax = "1 1"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 17,
                    Align = TextAnchor.MiddleCenter
                }
            };
			elements.Add(new CuiLabel
			{
				Text =
                {
					Text = msg, 
                    FontSize = 17,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
			}, mainName);
			elements.Add(Agree, mainName);
			CuiHelper.AddUi(player, elements);
		}
	
		[ChatCommand("help")]
		void cmdRule(BasePlayer player, string cmd, string[] args)
		{
			string msg = "";
			foreach(var help in Config["Messages", "HELP_MESSAGE"] as List<object>)
			msg = msg + help.ToString() + "\n";
			UseUI(player, msg.ToString());
		}

		void DisplayUI(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => DisplayUI(player));
            }
            else 
			{
				string steamId = Convert.ToString(player.userID);
				{			
					if(data.Players.Contains(steamId)) return;
					string msg = "";
					foreach(var help in Config["Messages", "HELP_MESSAGE"] as List<object>)
					msg = msg + help.ToString() + "\n";
					UseUI(player, msg.ToString());
					data.Players.Add(steamId);	
					Interface.GetMod().DataFileSystem.WriteObject("HelpGUIdata", data);
				}
            }
        }
		
		
		void OnPlayerInit(BasePlayer player)		
		{
			DisplayUI(player);		
		}
	}
}