using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("Rainbow Name", "sami37", "1.0.3")]
    [Description("Set your vip a special color name")]
    public class RainbowName : RustPlugin
    {
        [PluginReference] Plugin BetterChat;
        private string colors;
        private List<string> colorsList = new List<string>();
        private Random rand = new Random();
        private DynamicConfigFile PDATA;
        PlayersData pdata;

        public class PlayersData
        {
            public Dictionary<string, bool> StatePlayers = new Dictionary<string, bool>();
        }

        #region config

        void Init()
        {
            permission.RegisterPermission("rainbowname.color", this);
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            LoadConfig();
        }

        #endregion

        void Loaded()
        {
            PDATA = Interface.Oxide.DataFileSystem.GetFile(this.Title + "_Player");
            pdata = PDATA.ReadObject<PlayersData>();
            if (pdata?.StatePlayers == null)
                pdata = new PlayersData();
            Unsubscribe(nameof(OnBetterChat));
            LoadConfig();
        }

        string ListToString<T>(List<T> list, int first = 0, string seperator = ", ")
        {
            return string.Join(seperator, list.Select(val => val.ToString()).Skip(first).ToArray());
        }

        void SetConfig(params object[] args)
        {
            List<string> stringArgs = new List<string>();
            foreach (var arg in args)
                stringArgs.Add(arg.ToString());

            stringArgs.RemoveAt(args.Length - 1);
            if (Config.Get(stringArgs.ToArray()) == null)
                Config.Set(args);
        }

        T GetConfig<T>(T defaultVal, params object[] args)
        {
            List<string> stringArgs = new List<string>();
            foreach (var arg in args)
                stringArgs.Add(arg.ToString());

            if (Config.Get(stringArgs.ToArray()) == null)
            {
                PrintError(
                    $"The plugin failed to read something from the config: {ListToString(stringArgs, 0, "/")}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T) Convert.ChangeType(Config.Get(stringArgs.ToArray()), typeof(T));
        }

        void LoadConfig()
        {
            SetConfig("General", "Color List", "#20b2aa,#e0eee0,#333333,#474747,#aeeeee,#7ccd7c,#bcd2ee,#ee7942,#ffc1c1,#228b22,#454545,#eecbad,#ff83fa,#00ffff,#9e9e9e,#7a7a7a,#ff6a6a,#a52a2a,#f08080,#838b8b,#050505,#eee5de,#cdb38b,#00b2ee,#8b7355,#ffffe0,#a3a3a3,#8b814c,#b5b5b5,#cdc9a5");
            SaveConfig();

            colors = GetConfig(
                "#20b2aa,#e0eee0,#333333,#474747,#aeeeee,#7ccd7c,#bcd2ee,#ee7942,#ffc1c1,#228b22,#454545,#eecbad,#ff83fa,#00ffff,#9e9e9e,#7a7a7a,#ff6a6a,#a52a2a,#f08080,#838b8b,#050505,#eee5de,#cdb38b,#00b2ee,#8b7355,#ffffe0,#a3a3a3,#8b814c,#b5b5b5,#cdc9a5",
                "General", "Color List");
            colorsList = colors.Split(',').ToList();
        }

        bool BetterChatIns() => (BetterChat != null);

        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            if (BetterChatIns()) return null;
            BasePlayer player = (BasePlayer)arg.Connection.player;
            if (pdata.StatePlayers.ContainsKey(player.UserIDString) && pdata.StatePlayers[player.UserIDString] == false)
                return null;
            if (!permission.UserHasPermission(player.UserIDString, "rainbowname.color")) return null;
            string charName = StripRichText(player.displayName);
            string[] stringarry = new string[charName.Length];
            for (int i = 0; i < charName.Length; i++)
            {
                stringarry[i] = charName[i].ToString();
            }
            for (int i = 0; i < stringarry.Length; i++)
            {
                stringarry[i] = "<color=" + colorsList.ElementAt(rand.Next(0, colorsList.Count)) + ">" + stringarry[i] + "</color>";
            }

            string argMsg = arg.GetString(0, "text");

            string message = string.Join("", stringarry) + " : " + argMsg;

            Server.Broadcast(message, player.userID);

            return true;
        }

        private object OnBetterChat(Dictionary<string, object> data)
        {
            IPlayer player = data["Player"] as IPlayer;
            if (player != null && !permission.UserHasPermission(player.Id, "rainbowname.color")) return null;
            if (pdata.StatePlayers.ContainsKey(player.Id) && pdata.StatePlayers[player.Id] == false)
                return null;
            string charName = player.Name;
            string[] stringarry = new string[charName.Length];
            for (int i = 0; i < charName.Length; i++)
            {
                stringarry[i] = charName[i].ToString();
            }
            for (int i = 0; i < stringarry.Length; i++)
            {
                stringarry[i] = "<color=" + colorsList.ElementAt(rand.Next(0, colorsList.Count)) + ">" + stringarry[i] + "</color>";
            }

            data["Username"] = string.Join("", stringarry).Replace("[", "<").Replace("]", ">");
            data["Player"] = player;

            return data;
        }

        private string StripRichText(string text)
        {
            text = Regex.Replace(text, "<.*?>", String.Empty);
            return Formatter.ToPlaintext(text);
        }

        [ChatCommand("rn")]
        void CmdChat(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                SendReply(player, "Syntax: /rn on|off");
                return;
            }
            if (args.Length > 0)
            {
                if (args[0].ToLower() == "on")
                {
                    if (!pdata.StatePlayers.ContainsKey(player.UserIDString))
                    {
                        pdata.StatePlayers.Add(player.UserIDString, true);
                        return;
                    }

                    pdata.StatePlayers[player.UserIDString] = true;
                }
                else if (args[0].ToLower() == "off")
                {
                    if (!pdata.StatePlayers.ContainsKey(player.UserIDString))
                    {
                        pdata.StatePlayers.Add(player.UserIDString, false);
                        return;
                    }

                    pdata.StatePlayers[player.UserIDString] = false;
                }
            }
        }
    }
}