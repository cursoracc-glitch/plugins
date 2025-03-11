using Facepunch.Extend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using ProtoBuf;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info( "ClanPoint", "Molik", "0.0.1" )]
    class ClanPoint : RustPlugin
    {
        [PluginReference] Plugin Clans;
        [ChatCommand("giveclan")]
        void CmdChatGiveClan(BasePlayer player, string command, string[] args )
        {
            if(player.IsAdmin || player == null)
            {
            string tag = args[0];
            int amount = Convert.ToInt32(args[1]);
            if(amount <= 0)
            {
                player.ChatMessage("Нельзя очки меньше нуля!");
            }
            else
            {
            Clans?.Call("GiveClanPoints", tag, amount);
            player.ChatMessage("Все вы выдали очки!");
            }
            }
            else
            {
                player.ChatMessage("Вы не админ!!!");
            }
        }
        [ChatCommand("remclan")]
        void CmdChatRemClan(BasePlayer player, string command, string[] args )
        {
            if(player.IsAdmin || player == null)
            {
            string tag = args[0];
            int amount = Convert.ToInt32(args[1]);
            if(amount <= 0)
            {
                player.ChatMessage("Нельзя очки меньше нуля!");
            }
            else
            {
            Clans?.Call("RemClanPoints", tag, amount);
            player.ChatMessage("Все вы забрали очки!");
            }
            }
            else
            {
                player.ChatMessage("Вы не админ!!!");
            }
        }
    }
}