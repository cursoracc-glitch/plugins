using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.IO;
using System.Collections;


namespace Oxide.Plugins
{

  [Info("MyMiniUI","GAGA","1.0.0")]
  public class MyMiniUI : RustPlugin
   {
[PluginReference] Plugin ImageLibrary;

private void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList) 
                CuiHelper.DestroyUi(player, Layer);
		}


private string Layer = "UI_DrawInterface"; 

void OnServerInitialized()
        {
ImageLibrary?.Call("AddImage", "https://i.postimg.cc/zXCmCgVY/helicopter-1.png", "GAGU");
            
      
            foreach (var player in BasePlayer.activePlayerList)
			{
				OnPlayerConnected(player);
			}
        }




 private void OnPlayerConnected(BasePlayer player)
		{
			if (player.IsReceivingSnapshot)
			{
				NextTick(() => OnPlayerConnected(player));
				return;
			}

			UI_DrawInterface(player );
		}











        private void UI_DrawInterface(BasePlayer player)
        {
            var elements = new CuiElementContainer();
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "447 48", OffsetMax = "447 48" },
                Image = { Color = "0 0 0 0", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "assets/content/ui/uibackgroundblur.mat"  }
            }, "Overlay", Layer);

                
            
           // container.Add(new CuiButton
           // {
           //     RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-335 -30", OffsetMax = "-275 30" },
           //     Button = {Command = $"chat.say /nomini", Color = "1.00 0.00 0.00 1.00" },
            //    Text = { Text = $"", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-regular.ttf" }
           // }, Layer );
          container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiRawImageComponent() { Png = (string)ImageLibrary?.Call("GetImage", "GAGU"), Color = "1.00 1.00 1.00 1" },
                        new CuiRectTransformComponent(){  AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-264 -30", OffsetMax = "-204 30" },
                    }
                });

elements.Add(new CuiButton
                    {
                        Button = { Command = $"chat.say /switchmini", Color = "1 0.96 0.88 0.15"},
                        RectTransform = {  AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-264 -30", OffsetMax = "-204 30" },
                        Text = { Text = "", Align = TextAnchor.MiddleCenter, FontSize = 12 }
                    }, Layer);

           
          //  container.Add(new CuiButton
           // {
           //     RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-264 -30", OffsetMax = "-204 30" },
           //     Button = {Command = $"chat.say /mymini", Color = "0.00 0.65 0.00 1.00" },
           //     Text = { Text = $"/mymini", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", Font = "robotocondensed-regular.ttf" }
           // }, Layer ); 

CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
            CuiHelper.AddUi(player, elements);
        }
















   







   }


}