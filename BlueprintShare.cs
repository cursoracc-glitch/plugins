using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    // Creation date: 11-01-2021
    // Last update date: 16-01-2021
    [Info("Blueprint Share", "Sempai#3239", "1.0.0")]
    [Description("https://rustworkshop.space/resources/blueprint-share.245/")]
    public class BlueprintShare : RustPlugin
    {
        #region Oxide Hooks

        private void Init()
        {
            if (config.shareTechTree == false)
            {
                Unsubscribe(nameof(OnTechTreeNodeUnlocked));
            }

            if (config.shareBlueprints == false)
            {
                Unsubscribe(nameof(OnPlayerStudyBlueprint));
            }
            
            Message.ChangeSenderID(config.senderId);
        }

        private void OnTechTreeNodeUnlocked(Workbench workbench, TechTreeData.NodeInstance node, BasePlayer player)
        {
            OnLearned(player, node.itemDef);
        }

        private void OnPlayerStudyBlueprint(BasePlayer player, Item item)
        {
            OnLearned(player, item.blueprintTargetDef);
        }

        #endregion

        #region Core
        
        private void OnLearned(BasePlayer player, ItemDefinition definition)
        {
            var listFriendsId = GetAllMates(player);
            if (listFriendsId.Count < 1)
            {
                return;
            }
            
            if (config.blockedShortnames.Contains(definition.shortname) == true)
            {
                Message.Send(player, Message.Key.CantBeShared, "{name}", definition.displayName.english);
                return;
            }

            foreach (var playerId in listFriendsId)
            {
                Unlock(playerId, definition, player.displayName);
            }
            
            Message.Send(player, Message.Key.LearnedNew, "{name}", definition.displayName.english, "{count}", listFriendsId.Count);
        }

        private void Unlock(ulong idLong, ItemDefinition targetItem, string referrer)
        {
            var playerInfo = SingletonComponent<ServerMgr>.Instance.persistance.GetPlayerInfo(idLong);
            if (playerInfo.unlockedItems.Contains(targetItem.itemid) == true)
            {
                return;
            }
            
            playerInfo.unlockedItems.Add(targetItem.itemid);
            SingletonComponent<ServerMgr>.Instance.persistance.SetPlayerInfo(idLong, playerInfo);

            var player = BasePlayer.FindByID(idLong);
            if (player != null)
            {
                player.SendNetworkUpdateImmediate();
                player.ClientRPCPlayer(null, player, "UnlockedBlueprint", targetItem.itemid);
                Message.Send(player, Message.Key.WasShared, "{player}", referrer, "{name}", targetItem.displayName.english);
            }
        }

        private List<ulong> GetAllMates(BasePlayer player)
        {
            var idString = player.UserIDString;
            var list = new List<ulong>();

            if (config.shareFriends == true)
            {
                var friends = GetFriends(idString);
                foreach (var friend in friends)
                {
                    list.Add(Convert.ToUInt64(friend));
                }
            }

            if (config.shareClan == true)
            {
                var clan = GetPlayerClan(idString);
                if (string.IsNullOrEmpty(clan) == false)
                {
                    var members = GetClanPlayers(clan);
                    foreach (var member in members)
                    {
                        list.Add(Convert.ToUInt64(member));
                    }
                }
            }

            if (config.shareTeam == true)
            {
                var team = player.Team;
                if (team != null)
                {
                    foreach (var member in team.members)
                    {
                        list.Add(member);
                    }
                }
            }

            list.RemoveAll(x => x == player.userID);
            return list.Distinct().ToList();
        }

        #endregion
        
        #region Classes

        private static ConfigDefinition config = new ConfigDefinition();

        private class ConfigDefinition
        {
            [JsonProperty("Blocked shortnames")]
            public string[] blockedShortnames =
            {};
            
            [JsonProperty("Share with Team")]
            public bool shareTeam = true;

            [JsonProperty("Share with Clan")]
            public bool shareClan = true;

            [JsonProperty("Share with Friends")]
            public bool shareFriends = false;

            [JsonProperty("Share physical blueprints research")]
            public bool shareBlueprints = true;
            
            [JsonProperty("Share tech tree research")]
            public bool shareTechTree = true;

            [JsonProperty("Sender ID")]
            public ulong senderId = 0;
        }
        
        private partial class Message
        {
            private static Dictionary<Key, object> messages = new Dictionary<Key, object>
            {
                {Key.CantBeShared, "<color=#ffff00>{name}</color> isimli eşya arkadaşların ile paylaştırılamaz."},
                {Key.LearnedNew, "<color=#ffff00>{name}</color> isimli eşyayı öğrendin ve bu blueprint <color=#ffff00>{count}</color> arkadaşın ile daha paylaşıldı!"},
                {Key.WasShared, "<color=#ffff00>{player}</color> isimli arkadaşın <color=#ffff00>{name}</color> eşyasının blueprintini öğrendi ve bu sana da paylaştırıldı."},
            };
            
            public enum Key
            { 
                LearnedNew,
                WasShared,
                CantBeShared,
            }
        }

        #endregion
        
        #region Configuration v2.1

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigDefinition>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                }

                LoadDefaultConfig();
                return;
            }

            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (Interface.Oxide.CallHook("OnConfigValidate") != null)
            {
                PrintWarning("Using default configuration...");
                config = new ConfigDefinition();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigDefinition();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Language System v2.3

        protected override void LoadDefaultMessages()
        {
            Message.Load(lang, this);
        }

        private partial class Message
        {
            private static RustPlugin plugin;
            private static Lang lang;
            private static ulong senderID = 0;

            public static void ChangeSenderID(ulong newValue)
            {
                senderID = newValue;
            }

            public static void Load(Lang v1, RustPlugin v2)
            {
                lang = v1;
                plugin = v2;

                var dictionary = new Dictionary<string, string>();
                foreach (var pair in messages)
                {
                    var key = pair.Key.ToString();
                    var value = pair.Value.ToString();
                    if(!dictionary.ContainsKey(key))
                        dictionary.Add(key, value);
                }

                lang.RegisterMessages(dictionary, plugin);
            }

            public static void Unload()
            {
                lang = null;
                plugin = null;
            }

            public static void Console(string message, Type type = Type.Normal)
            {
                message = $"[{plugin.Name}] {message}";
                switch (type)
                {
                    case Type.Normal:
                        Debug.Log(message);
                        break;

                    case Type.Warning:
                        Debug.LogWarning(message);
                        break;

                    case Type.Error:
                        Debug.LogError(message);
                        break;
                }
            }

            public static void Send(object receiver, string message, params object[] args)
            {
                message = FormattedMessage(message, args);
                SendMessage(receiver, message);
            }

            public static void Send(object receiver, Key key, params object[] args)
            {
                var userID = (receiver as BasePlayer)?.UserIDString;
                var message = GetMessage(key, userID, args);
                SendMessage(receiver, message);
            }

            public static void Broadcast(string message, params object[] args)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    message = FormattedMessage(message, args);
                    SendMessage(player, message);
                }
            }

            public static void Broadcast(Key key, params object[] args)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    var message = GetMessage(key, player.UserIDString, args);
                    SendMessage(player, message);
                }
            }

            public static string GetMessage(Key key, string playerID = null, params object[] args)
            {
                var keyString = key.ToString();
                var message = lang.GetMessage(keyString, plugin, playerID);
                if (message == keyString)
                {
                    return $"{keyString} is not defined in plugin!";
                }

                if (Interface.CallHook("OnLanguageValidate") != null)
                {
                    message = messages.FirstOrDefault(x => x.Key == key).Value as string;
                }

                return FormattedMessage(message, args);
            }

            public static string FormattedMessage(string message, params object[] args)
            {
                if (args != null && args.Length > 0)
                {
                    var organized = OrganizeArgs(args);
                    return ReplaceArgs(message, organized);
                }

                return message;
            }

            private static void SendMessage(object receiver, string message)
            {
                if (receiver == null || string.IsNullOrEmpty(message))
                {
                    return;
                }

                BasePlayer player = null;
                IPlayer iPlayer = null;
                ConsoleSystem.Arg console = null;

                if (receiver is BasePlayer)
                {
                    player = receiver as BasePlayer;
                }

                if (player == null && receiver is IPlayer)
                {
                    iPlayer = receiver as IPlayer;
                    player = BasePlayer.Find(iPlayer.Id);
                }

                if (player == null && receiver is ConsoleSystem.Arg)
                {
                    console = receiver as ConsoleSystem.Arg;
                    player = console.Connection?.player as BasePlayer;
                    message = $"[{plugin?.Name}] {message}";
                }

                if (player == null && receiver is Component)
                {
                    var obj = receiver as Component;
                    player = obj.GetComponent<BasePlayer>() ?? obj.GetComponentInParent<BasePlayer>() ??
                        obj.GetComponentInChildren<BasePlayer>();
                }

                if (player == null)
                {
                    message = $"[{plugin?.Name}] {message}";
                    Debug.Log(message);
                    return;
                }

                if (console != null)
                {
                    player.SendConsoleCommand("echo " + message);
                }

                if (senderID > 0)
                {
                    if (Interface.CallHook("OnMessagePlayer", message, player) != null)
                    {
                        return;
                    }

                    player.SendConsoleCommand("chat.add", (object) 2, (object) senderID, (object) message);
                }
                else
                {
                    player.ChatMessage(message);
                }
            }
            
            private static Dictionary<string, object> OrganizeArgs(object[] args)
            {
                var dic = new Dictionary<string, object>();
                for (var i = 0; i < args.Length; i += 2)
                {
                    var value = args[i].ToString();
                    var nextValue = i + 1 < args.Length ? args[i + 1] : null;
                    if (!dic.ContainsKey(value))
                        dic.Add(value, nextValue);
                }

                return dic;
            }

            private static string ReplaceArgs(string message, Dictionary<string, object> args)
            {
                if (args == null || args.Count < 1)
                {
                    return message;
                }

                foreach (var pair in args)
                {
                    var s0 = "{" + pair.Key + "}";
                    var s1 = pair.Key;
                    var s2 = pair.Value != null ? pair.Value.ToString() : "null";
                    message = message.Replace(s0, s2, StringComparison.InvariantCultureIgnoreCase);
                    message = message.Replace(s1, s2, StringComparison.InvariantCultureIgnoreCase);
                }

                return message;
            }

            public enum Type
            {
                Normal,
                Warning,
                Error
            }
        }
        
        #endregion 
        
        #region Friends Support 02.07.2020

        [PluginReference] private Plugin Friends, RustIOFriendListAPI;

        private bool IsFriends(BasePlayer player1, BasePlayer player2)
        {
            return IsFriends(player1.userID, player2.userID);
        }

        private bool IsFriends(BasePlayer player1, ulong player2)
        {
            return IsFriends(player1.userID, player2);
        }

        private bool IsFriends(ulong player1, BasePlayer player2)
        {
            return IsFriends(player1, player2.userID);
        }

        private bool IsFriends(ulong id1, ulong id2)
        {
            var flag1 = Friends?.Call<bool>("AreFriends", id1, id2) ?? false;
            var flag2 = RustIOFriendListAPI?.Call<bool>("AreFriendsS", id1.ToString(), id2.ToString()) ?? false;
            return flag1 || flag2;
        }
        
        private string[] GetFriends(string playerID)
        {
            var flag1 = Friends?.Call<string[]>("GetFriends", playerID) ?? new string[]{};
            var flag2 = RustIOFriendListAPI?.Call<string[]>("GetFriends", playerID) ?? new string[]{};
            return flag1.Length > 0 ? flag1 : flag2;
        }

        #endregion

        #region Teams Support

        private static bool InSameTeam(BasePlayer player1, BasePlayer player2)
        {
            return player1.currentTeam != 0 && player1.currentTeam == player2.currentTeam;
        }

        private static bool InSameTeam(BasePlayer player1, ulong player2)
        {
            var team = RelationshipManager.ServerInstance.FindTeam(player1.currentTeam);
            return team != null && team.members.Contains(player2);
        }

        private static bool InSameTeam(ulong player1, BasePlayer player2)
        {
            var team = RelationshipManager.ServerInstance.FindTeam(player2.currentTeam);
            return team != null && team.members.Contains(player1);
        }

        #endregion
        
        #region Clans Support
        
        [PluginReference] private Plugin Clans;
        
        private string GetPlayerClan(BasePlayer player)
        {
            return Clans?.Call<string>("GetClanOf", player.userID);
        }

        private string GetPlayerClan(ulong playerID)
        {
            return Clans?.Call<string>("GetClanOf", playerID);
        }
        
        private string GetPlayerClan(string playerID)
        {
            return Clans?.Call<string>("GetClanOf", playerID);
        }

        private bool InSameClan(BasePlayer player1, BasePlayer player2)
        {
            var clan1 = GetPlayerClan(player1);
            var clan2 = GetPlayerClan(player2);
            return string.IsNullOrEmpty(clan1) == false && string.Equals(clan1, clan2);
        }
        
        private bool InSameClan(ulong player1, ulong player2)
        {
            var clan1 = GetPlayerClan(player1);
            var clan2 = GetPlayerClan(player2);
            return string.IsNullOrEmpty(clan1) == false && string.Equals(clan1, clan2);
        }

        private bool InSameClan(BasePlayer player1, ulong player2)
        {
            var clan1 = GetPlayerClan(player1);
            var clan2 = GetPlayerClan(player2);
            return string.IsNullOrEmpty(clan1) == false && string.Equals(clan1, clan2);
        }
        
        private bool InSameClan(ulong player1, BasePlayer player2)
        {
            var clan1 = GetPlayerClan(player1);
            var clan2 = GetPlayerClan(player2);
            return string.IsNullOrEmpty(clan1) == false && string.Equals(clan1, clan2);
        }
        
        private bool IsClanOwner(string name, string playerID)
        {
            var clan = Clans?.Call<JObject>("GetClan", name);
            return clan != null && clan["owner"].ToString() == playerID;
        }
        
        private bool IsClanModerator(string name, string playerID)
        {
            var clan = Clans?.Call<JObject>("GetClan", name);
            return clan != null && clan["moderators"].Contains(playerID);
        }
        
        private ulong[] GetClanPlayers(string name)
        {
            var clan = Clans?.Call<JObject>("GetClan", name);
            if (clan == null)
            {
                return new ulong[]{};
            }
            
            return clan["members"].Select(x => (ulong) x).ToArray();
        }

        #endregion
    }
}
