using System;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("VoteDay", "S1m0n", "1.0.0")]
    class VoteDay : RustPlugin
    {
        #region Fields
        private List<ulong> votesReceived = new List<ulong>();
        static Dictionary<string, string> imageIds = new Dictionary<string, string>();

        private bool voteOpen;
        private bool isWaiting;
        private int timeRemaining;
        private int requiredVotes;
        private Timer voteTimer;
        private Timer timeMonitor;
        #endregion

        #region UI
        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent,
                        panelName
                    }
                };
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion

        #region UI Creation
        private const string Main = "VDUIMain";
        private void CreateTimeUI(BasePlayer player)
        {
            var MainCont = UI.CreateElementContainer(Main, UI.Color(configData.Colors.UIBackgroundColor, configData.Colors.UIBackgroundAlpha), "0.702 0.96", "1 1");
            UI.CreateLabel(ref MainCont, Main, "", $"<color={configData.Colors.MainColor}>{msg("Пропуcтить ночь", player.UserIDString)}</color>", 20, "0.018 0", "3 1", TextAnchor.MiddleLeft);

            var percentVotes = System.Convert.ToDouble((float)votesReceived.Count / (float)requiredVotes);
            var yMaxVotes = 0.25f + (0.55f * percentVotes);
            UI.CreatePanel(ref MainCont, Main, UI.Color(configData.Colors.UIBackgroundColor, configData.Colors.UIBackgroundAlpha), $"0.4 0", $"0.84 1");
            UI.CreatePanel(ref MainCont, Main, UI.Color(configData.Colors.ProgressBarColor, 1), $"0.4 0.15", $"{yMaxVotes} 0.85");            
            UI.CreateLabel(ref MainCont, Main, "", $"{votesReceived.Count} / {requiredVotes}", 19, "0.25 0.15", "1 0.85");

            UI.CreateLabel(ref MainCont, Main, "", GetFormatTime(), 20, "0.8 0.1", "0.98 0.9", TextAnchor.MiddleRight);

            CuiHelper.DestroyUi(player, Main);
            CuiHelper.AddUi(player, MainCont);
        }
        
        private void RefreshAllUI()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (voteOpen)
                    CreateTimeUI(player);
                else CuiHelper.DestroyUi(player, Main);
            }
        }
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            lang.RegisterMessages(Messages, this);
            permission.RegisterPermission("voteday.admin", this);           
        }
        void OnServerInitialized()
        {
            LoadVariables();
            votesReceived = new List<ulong>();
            requiredVotes = 0;
            voteOpen = false;
            timeRemaining = 0;
            CheckTime();
        }
        void OnPlayerDisconnected(BasePlayer player) => CuiHelper.DestroyUi(player, Main);
        void Unload()
        {
            if (voteTimer != null)
                voteTimer.Destroy();            

            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Main);
        }
        #endregion

        #region Functions
        private void OpenVote()
        {
            var required = BasePlayer.activePlayerList.Count * configData.Options.RequiredVotePercentage;
            if (required < 1) required = 1;
            requiredVotes = Convert.ToInt32(required);
            voteOpen = true;
            Print("commandSyn");
            VoteTimer();
        }
        private void VoteTimer()
        {
            timeRemaining = configData.Options.VoteOpenTime;
            voteTimer = timer.Repeat(1, timeRemaining, () =>
            {
                RefreshAllUI();
                timeRemaining--;
                switch (timeRemaining)
                {
                    case 0:
                        TallyVotes();
                        return;
                    case 240:
                    case 180:
                    case 120:
                    case 60:
                    case 30:
                        MessageAll();
                        break;
                    default:
                        break;
                }                            
            });
        }
        private void MessageAll()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player != null)
                {
                    if (!AlreadyVoted(player))
                        Reply(player, "commandSyn");
                }
            }
        }
        private string GetFormatTime()
        {
            var time = timeRemaining;
            double minutes = Math.Floor((double)(time / 60));
            time -= (int)(minutes * 60);            
            return string.Format("{0:00}:{1:00}", minutes, time);
        }
        private void CheckTime()
        {            
            if (!voteOpen)
            {
                if (isWaiting)
                {
                    timeMonitor = timer.Once(20, () => CheckTime());
                    return;
                }
                
                if ((TOD_Sky.Instance.Cycle.Hour >= configData.Options.TimeToOpen && TOD_Sky.Instance.Cycle.Hour < 24) || (TOD_Sky.Instance.Cycle.Hour >= 0 && TOD_Sky.Instance.Cycle.Hour < configData.Options.TimeToSet))                                    
                    OpenVote();                
                else timeMonitor = timer.Once(20, () => CheckTime());
            }
            else
            {
                if (TOD_Sky.Instance.Cycle.Hour >= configData.Options.TimeToSet && TOD_Sky.Instance.Cycle.Hour < configData.Options.TimeToOpen)                
                    VoteEnd(false);
            }
        }
        private void TallyVotes()
        {
            if (votesReceived.Count >= requiredVotes)
                VoteEnd(true);
            else VoteEnd(false);
        }
        private void VoteEnd(bool success)
        {
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Main);
            voteOpen = false;
            requiredVotes = 0;
            voteTimer.Destroy();
            votesReceived.Clear();
            timeRemaining = 0;

            if (success)
            {
                TOD_Sky.Instance.Cycle.Hour = configData.Options.TimeToSet;
                Print("votingSuccessful");
            }
            else Print("votingUnsuccessful");
            isWaiting = true;
            timer.In(600, () => isWaiting = false);          
            CheckTime();
        }
        #endregion

        #region Helpers
        private bool AlreadyVoted(BasePlayer player) => votesReceived.Contains(player.userID);
        #endregion

        #region ChatCommands
        [ChatCommand("voteday")]
        private void cmdVoteDay(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                if (voteOpen)
                {
                    if (!AlreadyVoted(player))
                    {
                        votesReceived.Add(player.userID);
                        Reply(player, "voteSuccess");
                        if (votesReceived.Count >= requiredVotes)
                            VoteEnd(true);
                        return;
                    }
                }
                else Reply(player, "noVote");
            }
            else
            {
                if (!permission.UserHasPermission(player.UserIDString, "voteday.admin")) return;
                switch (args[0].ToLower())
                {
                    case "open":
                        if (!voteOpen)
                            OpenVote();
                        else Reply(player, "alreadyOpen");
                        return;
                    case "close":
                        if (voteOpen)
                            VoteEnd(false);
                        else Reply(player, "noVote");
                        return;
                    default:
                        Reply(player, "invalidSyntax");
                        break;
                }
            }
        }       
        #endregion

        #region Config        
        private ConfigData configData;
        class Colors
        {
            public string MainColor { get; set; }
            public string MSGColor { get; set; }
            public string ProgressBarColor { get; set; }
            public string UIBackgroundColor { get; set; }
            public float UIBackgroundAlpha { get; set; }
        }
        
        class Options
        {
            public float RequiredVotePercentage { get; set; }
            public float TimeToOpen { get; set; }
            public float TimeToSet { get; set; }
            public int VoteOpenTime { get; set; }            
        }
        class ConfigData
        {
            public Colors Colors { get; set; }            
            public Options Options { get; set; } 
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {

                Colors = new Colors
                {
                    MainColor = "#ffae00",
                    MSGColor = "#ffffff",
                    ProgressBarColor = "#EBB146",
                    UIBackgroundAlpha = 0.7f,
                    UIBackgroundColor = "#404040"
                },
                Options = new Options
                {
                    RequiredVotePercentage = 0.4f,
                    TimeToOpen = 18f,
                    TimeToSet = 8f,
                    VoteOpenTime = 240
                }                
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Localization
        void Reply(BasePlayer player, string langKey) => SendReply(player, msg(langKey, player.UserIDString).Replace("{main}", $"<color={configData.Colors.MainColor}>").Replace("{msg}", $"<color={configData.Colors.MSGColor}>").Replace("{percent}", (configData.Options.RequiredVotePercentage * 100).ToString()));
        void Print(string langKey) => PrintToChat(msg(langKey).Replace("{main}", $"<color={configData.Colors.MainColor}>").Replace("{msg}", $"<color={configData.Colors.MSGColor}>").Replace("{percent}", (configData.Options.RequiredVotePercentage * 100).ToString()));
        string msg(string key, string playerId = "") => lang.GetMessage(key, this, playerId);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"voteSuccess", "{msg}Вы проголосовали за пропуск ночи!</color>" },
            {"noVote", "{msg}На данный момент голосование не запущено!</color>" },
            {"alreadyOpen", "{msg}Голосование уже запущено!</color>" },
            {"invalidSyntax", "{msg}Неправильный синтаксис!</color> {main}/voteday open</color>{msg} или </color> {main}/voteday close</color>" },
            {"votingSuccessful", "{main}Голосование прошло успешно!</color>{msg} Ночь пропущена</color>" } ,
            {"votingUnsuccessful", "{msg}Голосование не удалось! Слишком мало игроков проголосовало!</color>" },
            {"commandSyn", "{msg}Введите </color>{main}/voteday</color>{msg}, если вы хотите пропустить ночь!\n-- Необходимо </color>{main}{percent}%</color>{msg} голосов от общего количества игроков</color>" },
            {"skipNight", "Пропуcтить ночь" }        
        };
        #endregion
    }
}

