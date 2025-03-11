using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Text;

namespace Oxide.Plugins
{
    [Info("CovertAdmin", "redBDGR", "1.0.6")]
    [Description("Go fully undercover and disguise yourself as another player")]
    internal class CovertAdmin : RustPlugin
    {
        [PluginReference] private Plugin BetterChat;

        private bool Changed;

        private Dictionary<string, CovertInfo> covertDic = new Dictionary<string, CovertInfo>();
        private string nameColour = "#54A7FB";
        private const string permissionName = "covertadmin.use";

        private Dictionary<string, object> playerNames = new Dictionary<string, object>();

        private string tags = "";
        private string textSize = "15";
        private bool disableOnLogout = true;

        private static Dictionary<string, object> RandomPlayerNames()
        {
            var x = new Dictionary<string, object> { { "RandomName1", "randomSteamID" }, { "RandomName2", "76561198381967577" } };
            return x;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadVariables();
        }

        private void LoadVariables()
        {
            playerNames = (Dictionary<string, object>)GetConfig("Settings", "Covert Names", RandomPlayerNames());
            tags = Convert.ToString(GetConfig("Settings", "Tags", ""));
            nameColour = Convert.ToString(GetConfig("Settings", "Covert Name Colour", "#54A7FB"));
            textSize = Convert.ToString(GetConfig("Settings", "Message Size", "15"));
            disableOnLogout = Convert.ToBoolean(GetConfig("Settings", "Disable On Logout", true));

            if (!Changed) return;
            SaveConfig();
            Changed = false;
        }

        private void Init()
        {
            LoadVariables();
            permission.RegisterPermission(permissionName, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                //chat
                ["format"] = "<size={0}>{1} <color={2}>{3}</color>: {4}</size>",
                ["No Permission"] = "You cannot use this command",
                ["Covert Disabled"] = "Covert mode disabled",
                ["Covert Enabled"] = "Covert mode enabled! You will now appear under the name of \"{0}\"",
                ["Already In Covert"] = "You are already in covert mode",
                ["Login Warning"] = "Your covert mode is still activated! type /covert to disable it"
                // [0] = size
                // [1] = tags
                // [2] = colour
                // [3] = name
                // [4] = text
            }, this);
        }

        private void Unload()
        {
            foreach (var entry in covertDic)
                if (entry.Value.player.IsConnected)
                    RenamePlayer(entry.Value.player, entry.Value.restoreName);
        }

        [ChatCommand("covertstatus")]
        private void CovertStatusCMD(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionName))
            {
                player.ChatMessage(msg("No Permission", player.UserIDString));
                return;
            }
            if (covertDic.ContainsKey(player.UserIDString))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Current covert status:");
                sb.AppendLine($"Display Name: {covertDic[player.UserIDString].covertName}");
                sb.AppendLine($"Covert ID: {covertDic[player.UserIDString].covertID}");
                player.ChatMessage(sb.ToString().TrimEnd());
            }
            else
                player.ChatMessage("Covert mode is currently disabled");
        }

        [ChatCommand("covert")]
        private void CovertCMD(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionName))
            {
                player.ChatMessage(msg("No Permission", player.UserIDString));
                return;
            }
            switch (args.Length)
            {
                case 0:
                    if (covertDic.ContainsKey(player.UserIDString))
                    {
                        RenamePlayer(player, covertDic[player.UserIDString].restoreName);
                        covertDic.Remove(player.UserIDString);
                        player.ChatMessage(msg("Covert Disabled", player.UserIDString));
                    }
                    else
                    {
                        var names = playerNames.Keys.ToList();
                        var coverName = names[Convert.ToInt16(Mathf.Round(Random.Range(0f, Convert.ToSingle(playerNames.Count) - 1f)))];
                        covertDic.Add(player.UserIDString, new CovertInfo { covertName = coverName, restoreName = player.displayName, covertID = Convert.ToUInt64(playerNames[coverName]), player = player, restoreID = player.userID });
                        RenamePlayer(player, coverName);
                        player.ChatMessage(string.Format(msg("Covert Enabled", player.UserIDString), coverName));
                    }
                    break;
                case 1:
                    if (covertDic.ContainsKey(player.UserIDString))
                    {
                        player.ChatMessage(msg("Already In Covert", player.UserIDString));
                        return;
                    }
                    covertDic.Add(player.UserIDString, new CovertInfo { covertName = args[0], restoreName = player.displayName, covertID = 76561198136204161, player = player, restoreID = player.userID });
                    RenamePlayer(player, args[0]);
                    player.ChatMessage(string.Format(msg("Covert Enabled", player.UserIDString), args[0]));
                    break;
                case 2:
                    if (covertDic.ContainsKey(player.UserIDString))
                    {
                        player.ChatMessage(msg("Already In Covert", player.UserIDString));
                        return;
                    }
                    covertDic.Add(player.UserIDString, new CovertInfo { covertName = args[0], restoreName = player.displayName, covertID = Convert.ToUInt64(args[1]), player = player, restoreID = player.userID });
                    RenamePlayer(player, args[0]);
                    player.ChatMessage(string.Format(msg("Covert Enabled", player.UserIDString), args[0]));
                    break;
            }
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!covertDic.ContainsKey(player.UserIDString))
                return;
            if (disableOnLogout)
            {
                RenamePlayer(player, covertDic[player.UserIDString].restoreName);
                covertDic.Remove(player.UserIDString);
            }
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (disableOnLogout)
                return;
            if (!covertDic.ContainsKey(player.UserIDString))
                return;
            RenamePlayer(player, covertDic[player.UserIDString].covertName);
            player.ChatMessage(msg("Login Warning", player.UserIDString));
        }

        private object OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (info == null)
                return null;
            if (info.Initiator == null)
                return null;
            BasePlayer attacker = info.Initiator.GetComponent<BasePlayer>();
            if (!attacker)
                return null;
            if (covertDic.ContainsKey(attacker.UserIDString))
            {
                attacker.userID = covertDic[attacker.UserIDString].covertID;
                timer.Once(0.2f, () =>
                {
                    if (attacker != null)
                        attacker.userID = covertDic[attacker.UserIDString].restoreID;
                });
            }
            return null;
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (covertDic.ContainsKey(player.UserIDString))
                RenamePlayer(player, covertDic[player.UserIDString].covertName);
        }

        private void RenamePlayer(BasePlayer player, string name)
        {
            player.displayName = name;
            IPlayer _player = covalence.Players.FindPlayerById(player.UserIDString);
            _player.Rename(name);
            player.SendNetworkUpdateImmediate();
        }

        private object OnBetterChat(Dictionary<string, object> data)
        {
            var player = data["Player"] as IPlayer;
            if (player == null)
                return null;
            if (!covertDic.ContainsKey(player.Id))
                return null;
            rust.BroadcastChat(null, string.Format(msg("format"), textSize, tags, nameColour, player.Name, data["Text"]), covertDic[player.Id].covertID.ToString()); //  76561198136204161
            return false;
        }

        private object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return null;
            if (!covertDic.ContainsKey(player.UserIDString)) return null;
            player.userID = covertDic[player.UserIDString].covertID;
            timer.Once(0.2f, () =>
            {
                if (!player) return;
                if (covertDic.ContainsKey(player.UserIDString))
                    player.userID = covertDic[player.UserIDString].restoreID;
            });
            return true;
        }

        private object OnUserChat(IPlayer player, string message)
        {
            if (covertDic.ContainsKey(player.Id))
            {
                if (!BetterChat)
                    rust.BroadcastChat(null, string.Format(msg("format"), textSize, tags, nameColour, player.Name, message, covertDic[player.Id].covertID.ToString()));
                return true;
            }
            return null;
        }

        private object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }
            object value;
            if (data.TryGetValue(datavalue, out value)) return value;
            value = defaultValue;
            data[datavalue] = value;
            Changed = true;
            return value;
        }

        private string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        private class CovertInfo
        {
            public ulong covertID;
            public string covertName;
            public BasePlayer player;
            public string restoreName;
            public ulong restoreID;
        }
    }
}