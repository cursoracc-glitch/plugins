using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json; 

namespace Oxide.Plugins
{
    [Info("XInfoMenuLITE", "Sempai#3239", "1.0.0")] 
    class XInfoMenuLITE : RustPlugin
    {
	    #region Configuration 

        private InfoMenuConfig config; 

        private class InfoMenuConfig
        {							            
            [JsonProperty("Кнопки и сообщения")]
            public Dictionary<string, string> Message;            	
			
			public static InfoMenuConfig GetNewConfiguration()
            {
                return new InfoMenuConfig
                {
					Message = new Dictionary<string, string>
					{
						["1"] = "111",
						["2"] = "222",
						["3"] = "333",
						["4"] = "444"						
					}
				};
			}
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
			
			try
			{
				config = Config.ReadObject<InfoMenuConfig>(); 
			}
			catch
			{
				PrintWarning("Ошибка чтения конфигурации! Создание дефолтной конфигурации!");
				LoadDefaultConfig();
			}
			
			SaveConfig();
        }
		protected override void LoadDefaultConfig() => config = InfoMenuConfig.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
		
		#region Commands
		
		[ChatCommand("help")]
		private void cmdOpenGUI(BasePlayer player) => GUI(player);		
		
		[ChatCommand("info")]
		private void cmdOpenGUII(BasePlayer player) => GUI(player);
		
		[ConsoleCommand("info_page")]
		private void ccmdPage(ConsoleSystem.Arg args)
		{
			BasePlayer player = args.Player();
			
			GUI(player, int.Parse(args.Args[0]));
			EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/notice/loot.drag.grab.fx.prefab", player, 0, new Vector3(), new Vector3()), player.Connection);
		}
		
		#endregion
		
		#region Hooks
		
		private void OnServerInitialized()
		{
			PrintWarning("\n-----------------------------\n" +
			"     Discord - Sempai#3239\n" +
			"-----------------------------"); 
		}
		
		#endregion
		 
		#region GUI
		
		private void GUI(BasePlayer player, int page = 0)
		{
			CuiHelper.DestroyUi(player, ".GUILITE");
            CuiElementContainer container = new CuiElementContainer();		
			
			container.Add(new CuiPanel
            {
				CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -195", OffsetMax = "400 255" },
                Image = { Color = "0.117 0.121 0.109 0.995", Material = "assets/icons/greyout.mat" }
            }, "Overlay", ".GUILITE");
			
			container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-300 -300", OffsetMax = "300 300" },
                Button = { Color = "0 0 0 0", Close = ".GUILITE" },
                Text = { Text = "" }
            }, ".GUILITE");			
			
			container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -175", OffsetMax = "400 -170" },
                Image = { Color = "0.217 0.221 0.209 0.995", Material = "assets/icons/greyout.mat" }
            }, ".GUILITE");			
			
			container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-400 -170", OffsetMax = "400 225" },
                Text = { Text = config.Message.ElementAt(page).Value, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = "0.75 0.75 0.75 0.5" }
            }, ".GUILITE");
			
			int x = 0;
			int count = config.Message.Count;
			
			foreach(var button in config.Message)
			{
				string color = page == x ? "0.53 0.77 0.35 1" : "0 0 0 0";
				double offset = -(63.75 * count--) - (2.5 * count--);
				
				container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"{127.5 + offset - 127.5} -220", OffsetMax = $"{127.5 + offset} -180" },
                    Button = { Color = "0.217 0.221 0.209 0.995", Command = $"info_page {x}" },
                    Text = { Text = button.Key, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "0.75 0.75 0.75 0.5" }
                }, ".GUILITE", ".BButton");

			    container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMax = "0 1" },
                    Image = { Color = color, Material = "assets/icons/greyout.mat" }
                }, ".BButton");				
				
				x++;
			}
			
			CuiHelper.AddUi(player, container);
		}
		
		#endregion
	}
}