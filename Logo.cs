using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Logo", "poof", "1.0.0")]
    public class Logo : RustPlugin
    {
		#region Settings [Настройка]
		
		string LogoName = "GGT RUST • MAX 5";
		
		#endregion
		
		#region Hooks [Хуки]
		
        private void OnServerInitialized()
        {
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
        }
		
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "LogoPanel");  
            }   
        }
		
        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
			
            foreach (var players in BasePlayer.activePlayerList)
            {
			    timer.Once(1, () =>
			    {
                    DrawInterface(players);
			    });
            }
        }
		
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
			    timer.Once(1, () =>
			    {
				    DrawInterface(players);
			    });
            }
        }
		
		#endregion
		
		#region UI [Визуальная часть]
		
        void DrawInterface(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "LogoPanel");
            
			var QueueCount = ServerMgr.Instance.connectionQueue.Queued.ToString();
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                Image = { Color = $"0 0 0 0" },
                RectTransform = { AnchorMin = "0.0359375 0.9388888", AnchorMax = "0.203125 0.9875" },
                CursorEnabled = false,
            }, "Overlay", "LogoPanel");
            
            container.Add(new CuiElement
            {
                Parent = "LogoPanel",
                Components = {
                    new CuiTextComponent() { Color = HexToRustFormat("#FFFFFF91"), FadeIn = 1f, Text = LogoName, FontSize = 20, Align = TextAnchor.UpperLeft, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.0140186 0.1428578", AnchorMax = "0.9859813 1.057143" },
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "LogoPanel",
                Components = {
                    new CuiTextComponent() { Color = HexToRustFormat("#FFFFFF91"), FadeIn = 1f, Text = $"ОНЛАЙН: {BasePlayer.activePlayerList.Count} (+{QueueCount}) ЧЕЛ.", FontSize = 15, Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.0140186 -0.02857186", AnchorMax = "0.9859813 0.5428569" },
                }
            });
            
            CuiHelper.AddUi(player, container);
        }
		
		#endregion
		
		#region Helpers [Доп. методы]
		
		private static string HexToRustFormat(string hex)
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

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
		
		#endregion
	}
}