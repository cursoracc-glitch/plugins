using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using Oxide.Game.Rust.Libraries;
using UnityEngine;
using UnityEngine.UI;

namespace Oxide.Plugins
{
    [Info("SkipNight", "A1M41K", "2.2.11")]
    [Description("Плагин для пропуска ночи")]
    public class SkipNight : RustPlugin
    {
        private bool _isRunning;
        private int RequiredVotes;
        private short votesyes;
        private short votesno;
        private int SkipNig;
        private int ReturnNig;
        private int ToSkipNight;


        private readonly HashSet<BasePlayer> _votedPlayers = new HashSet<BasePlayer>();
        private readonly HashSet<BasePlayer> _votedPlayersList = new HashSet<BasePlayer>();

        private Timer t;

        #region CUI

        private void StartCUI()
        {
            var playersCount = BasePlayer.activePlayerList.Count;
            for (var i = 0; i < playersCount; i++)
            {
                var player = BasePlayer.activePlayerList[i];
                DrawCUI(player);
            }
        }

        private void DrawCUI(BasePlayer player)
        {
            // ReSharper disable once UseObjectOrCo    llectionInitializer
            var container = new CuiElementContainer();
            container.Add(_cui.Panel, name: "NightVote", parent: "Overlay");
            container.Add(_cui.ButtonYes, "NightVote", "NightVoteButtonYes");
            container.Add(_cui.ButtonNo, "NightVote", "NightVoteButtonNo");
            container.Add(_cui.ElementPanel);
            container.Add(_cui.ElementPanelText);
            CuiHelper.AddUi(player, container);
        }

        private void DrawSkip(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SkipNight");
            var VoteUI = new CuiElementContainer();
            var VoteGUI = VoteUI.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image =
                {
                    Color = "0 0 0 0",
                },
                RectTransform =
                {
                    AnchorMin = _cui.PanelMin,
                    AnchorMax = _cui.PanelMax
                },
            }, "Overlay", "SkipNight");

            VoteUI.Add(new CuiElement
            {
                Parent = "SkipNight",
                Name = "SkipNight" + "Headrs",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#404040C7"),
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = _cui.TextPanelMin,
                        AnchorMax = _cui.TextPanelMax
                    },
                }
            });
            VoteUI.Add(new CuiElement
            {
                Parent = "SkipNight" + "Headrs",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = _cui.TextPanelCFG,
                        Color = HexToRustFormat(_cui.ColorTextSkip),
                        Font = "robotocondensed-bold.ttf",
                        FontSize = _cui.SizeText,
                        Align = TextAnchor.MiddleCenter,
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            var golos = votesyes + votesno;
            var gol = golos >= BasePlayer.activePlayerList.Count * (_settingsFastSkip.VotesPlayers / 100f);
            var ggol = _settingsFastSkip.fasttimevote == true ? gol : votesyes > votesno;
            var text = ggol ? "Ночь пропущена" : "Ночь не пропущена";
            
            VoteUI.Add(new CuiElement
            {
                Parent = "SkipNight",
                Name = "SkipNight" + "SkipYes",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#404040C7"),
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.05263185 0.1363642",
                        AnchorMax = "0.9473684 0.5113642"
                    },
                }
            });

            VoteUI.Add(new CuiElement
            {
                Parent = "SkipNight" + "SkipYes",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = text,
                        Color = HexToRustFormat(_cui.ColorTextSkip),
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter,
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.03743315 0",
                        AnchorMax = "0.9625669 1.030302"
                    }
                }
            }); 
            
            CuiHelper.AddUi(player, VoteUI);
        }

        private void DrawUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "InfoVote");
            var VoteUI = new CuiElementContainer();
            var VoteGUI = VoteUI.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image =
                {
                    Color = "0 0 0 0",    
                },
                RectTransform =
                {
                    AnchorMin = _cui.PanelMin,
                    AnchorMax = _cui.PanelMax
                },
            }, "Overlay", "InfoVote");

            VoteUI.Add(new CuiElement
            {
                Parent = "InfoVote",
                Name = "InfoVote" + "Headrs",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#404040C7"),
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = _cui.TextPanelMin,
                        AnchorMax = _cui.TextPanelMax
                    },
                }
            });

            VoteUI.Add(new CuiElement
            {
                Parent = "InfoVote" + "Headrs",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = _cui.TextPanelCFG,
                        Color = HexToRustFormat(_cui.ColorTextSkip),
                        Font = "robotocondensed-bold.ttf",
                        FontSize = _cui.SizeText,
                        Align = TextAnchor.MiddleCenter,
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            VoteUI.Add(new CuiElement
            {
                Parent = "InfoVote",
                Name = "InfoVote" + "VotesYes",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#404040C7"),
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.05741573 0.05681869",
                        AnchorMax = "0.4880377 0.5340914"
                    }
                }
                
            });
            
            VoteUI.Add(new CuiElement
            {
                Parent = "InfoVote" + "VotesYes",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"{votesyes}",
                        Color = HexToRustFormat(_cui.ColorTextSkip),
                        Align = TextAnchor.MiddleCenter,
                        FontSize = _cui.SizeVoteYes,
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
                
            });
            
            VoteUI.Add(new CuiElement
            {
                Parent = "InfoVote",
                Name = "InfoVote" + "VotesNo",
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = HexToRustFormat("#404040C7"),
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.5167453 0.05681815",
                        AnchorMax = "0.9473673 0.534091"
                    }
                }
                
            });
            
            VoteUI.Add(new CuiElement
            {
                Parent = "InfoVote" + "VotesNo",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"{votesno}",
                        Color = HexToRustFormat(_cui.ColorTextSkip),
                        Align = TextAnchor.MiddleCenter,
                        FontSize = _cui.SizeVoteNo,
                        FadeIn = 3f
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
                
            });
            CuiHelper.AddUi(player, VoteUI);
        }

        #endregion

        #region OxideHooks

        void OnServerInitialized()
        {
            #region Panel

            if (!_config.Cui.TryGetValue(_config.CUINumber, out _cui))
            {
                _config.ShowGui = false;
                SaveConfig();
            }
            else
            {
                _cui.Panel = new CuiPanel
                {
                    CursorEnabled = false,
                    Image =
                    {
                        Color = "0 0 0 0",
                    },
                    RectTransform =
                    {
                        AnchorMin = _cui.PanelMin,
                        AnchorMax = _cui.PanelMax
                    },
                };
                _cui.ElementPanel = new CuiElement
                {
                    Parent = "NightVote",
                    Name = "NightVote" + "Headr",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = HexToRustFormat("#404040C7"),
                            FadeIn = 3f
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = _cui.TextPanelMin,
                            AnchorMax = _cui.TextPanelMax
                        },
                    }
                };
                _cui.ElementPanelText = new CuiElement
                {
                    Parent = "NightVote" + "Headr",
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = _cui.TextPanelCFG,
                            Color = HexToRustFormat(_cui.ColorTextSkip),
                            Font = "robotocondensed-bold.ttf",
                            FontSize = _cui.SizeText,
                            Align = TextAnchor.MiddleCenter,
                            FadeIn = 3f
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                };
                
                _cui.ButtonNo = new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = _cui.ButtonMinNo,
                        AnchorMax = _cui.ButtonMaxNo
                    },
                    Button =
                    {
                        Close = "NightVote",
                        Command = "nightvoteno",
                        Color = HexToRustFormat("#404040C7"),
                        FadeIn = 3f,
                    },
                    Text =
                    {
                        Text = _cui.TextButtonNo,
                        Color = HexToRustFormat(_cui.ColorTextSkip),
                        Align = TextAnchor.MiddleCenter,
                        FontSize = _cui.SizeTextYes,
                        FadeIn = 3f
                    }
                };
                
                                
                _cui.ButtonYes = new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = _cui.ButtonMinYes,
                        AnchorMax = _cui.ButtonMaxYes
                    },
                    Button =
                    {
                        Close = "NightVote",
                        Command = "nightvoteyes",
                        Color = HexToRustFormat("#404040C7"),
                        FadeIn = 3f,
                    },
                    Text =
                    {
                        Text = _cui.TextButtonYes,
                        Color = HexToRustFormat(_cui.ColorTextSkip),
                        Align = TextAnchor.MiddleCenter,
                        FontSize = _cui.SizeTextYes,
                        FadeIn = 3f,
                    }
                };
            }

            #endregion

            var cmdLib = GetLibrary<Command>();
            cmdLib.AddChatCommand(_config.Command, this, CommandChatVote);
            cmdLib.AddConsoleCommand("nightvoteno", this, CommandConsoleNo);
            cmdLib.AddConsoleCommand("nightvoteyes", this, CommandConsoleYes);

            _isRunning = false;
            RequiredVotes = 0;
            LoadConfig();
            LoadDefaultMessages();

            permission.RegisterPermission(_config.PermAdmin, this);

            t = timer.Every(_settingsSkip.TimeReload, () =>
            {
                if (TOD_Sky.Instance.Cycle.Hour >= TimeSpan.Parse(_settingsSkip.TimeStart).TotalHours &&
                    TOD_Sky.Instance.Cycle.Hour < TimeSpan.Parse(_settingsSkip.TimeEnd).TotalHours)
                {
                    StartVote();
                }
            });

            LoadMessages();
        }

        void Unload()
        {
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(p, "NightVote");
                CuiHelper.DestroyUi(p, "InfoVote");
                CuiHelper.DestroyUi(p, "SkipNight");
            }
            ClearList();
        }

        #endregion

        #region Helpers

        private string GetMsg(string key, string userId = null) => lang.GetMessage(key, this, userId);
        
        private void Broadcast(BasePlayer player, string msgId, params object[] args) // Chat 
        {
            PrintToChat(player, lang.GetMessage(msgId, this, player.UserIDString), args);
        }
        
        public void Reply(BasePlayer player, string message, string[] args = null, string header = "Пропуск ночи")
        {
            bool flag = args != null;
            if (flag)
            {
                message = string.Format(message, args);
            }
            PrintToChat(player, string.Format("<size=16><color=#378a1e>{0}</color>:</size>\n{1}", header, message));
        }
        
        
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

        private void broadcast(string message)
        {
            foreach (var VARIABLE in BasePlayer.activePlayerList)
            {
                PrintToChat(message);
            }
        }
        
        public static string FormatTime(int time
        )
        {
            string result = string.Empty;
            if (time != 0)
                result += $"{Format(time, "ночей", "ночи", "ночь")} ";

            return result;
        }
        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }
        

        private void StartVote()
        {
            if (_isRunning)
                return;
            if (_config.ShowGui)
            {
                StartCUI();
                var playersCount = BasePlayer.activePlayerList.Count;
                for (var i = 0; i < playersCount; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    Reply(player, GetMsg("StartVote", player.UserIDString));
                }
            }
            else
            {
                var playersCount = BasePlayer.activePlayerList.Count;
                for (var i = 0; i < playersCount; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    Reply(player, GetMsg("StartVoteChat".Replace("{0}", _config.Command), player.UserIDString));
                }
            }

            timer.Once(_settingsSkip.TimeVote, () => { EndVote(); });
            _isRunning = true;
        }

        private void EndVote()
        {
            if (!_isRunning)
                return;
			
            _isRunning = false;

            if (_settingsFastSkip.fasttimevote == true)
            {
                var golov = votesyes + votesno;
                if (golov >= BasePlayer.activePlayerList.Count * (_settingsFastSkip.VotesPlayers / 100f))
                {
                    skipnight();
                }
                else
                {
                    var playersCount = BasePlayer.activePlayerList.Count;
                    if (TOD_Sky.Instance.Cycle.Hour >= (float) TimeSpan.Parse(_settingsSkip.TimeStart).TotalHours &&
                        TOD_Sky.Instance.Cycle.Hour <= (float) TimeSpan.Parse(_settingsSkip.TimeEnd).TotalHours)
                    {
                        TOD_Sky.Instance.Cycle.Hour = (float) TimeSpan.Parse(_settingsSkip.TimeEnd).TotalHours;
                    }
                    for (var i = 0; i < playersCount; i++)
                    {
                        var player = BasePlayer.activePlayerList[i];
                        if (_config.ShowGui)
                        {
                            DrawSkip(player);
                            timer.Once(5, () => { CuiHelper.DestroyUi(player, "SkipNight"); });
                        }
                        else
                        {
                            Reply(player, GetMsg("NoSkippedNight", player.UserIDString));
                        }
                    
                    }  
                }
            }
            else
            {
                skipnight();
            }

            if (_config.ShowGui)
            {
                var playersCount = BasePlayer.activePlayerList.Count;
                for (var i = 0; i < playersCount; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    CuiHelper.DestroyUi(player, "NightVote");
                    CuiHelper.DestroyUi(player, "InfoVote");
                }
            }

            ClearList();
        }

        private void HGS()
        {
            if (_settingsFastSkip.fasttime == true)
            {
                var golos = votesyes + votesno;
                if (golos >= BasePlayer.activePlayerList.Count * (_settingsFastSkip.VotesPlayers/100f) )
                {
                    EndVote();
                }                
            }
        }
        
        private void skipnight()
        {
            if (votesyes > votesno)
            {
                TOD_Sky.Instance.Cycle.Hour = (float) TimeSpan.Parse(_settingsSkip.TimeSet).TotalHours;
                var playersCount = BasePlayer.activePlayerList.Count;
                for (var i = 0; i < playersCount; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    if (_config.ShowGui)
                    {
                        DrawSkip(player);
                        timer.Once(5, () => { CuiHelper.DestroyUi(player, "SkipNight"); });
                    }
                    else
                    {
                        Reply(player, GetMsg("SkipNight", player.UserIDString));
                    }
                    
                }
            }
            else
            {
                var playersCount = BasePlayer.activePlayerList.Count;
                if (TOD_Sky.Instance.Cycle.Hour >= (float) TimeSpan.Parse(_settingsSkip.TimeStart).TotalHours &&
                    TOD_Sky.Instance.Cycle.Hour <= (float) TimeSpan.Parse(_settingsSkip.TimeEnd).TotalHours)
                {
                    TOD_Sky.Instance.Cycle.Hour = (float) TimeSpan.Parse(_settingsSkip.TimeEnd).TotalHours;
                }
                for (var i = 0; i < playersCount; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    if (_config.ShowGui)
                    {
                        DrawSkip(player);
                        timer.Once(5, () => { CuiHelper.DestroyUi(player, "SkipNight"); });
                    }
                    else
                    {
                        Reply(player, GetMsg("NoSkippedNight", player.UserIDString));
                    }
                    
                }
            }
            
        }

        string color(string color, string text)
        {
            return $"<color={color}>{text}</color>";
        }

        string getMessageFormat()
        {
            string message = _config.textformat.Replace("{VOTES}", $"{votesyes}/{votesno}");
            if (_config.showperfix)
                return $"[{color(_config.ColorPerfix, _config.prefix)}] {message}";
            return message;
        }

        void SendVotes()
        {
            string text = getMessageFormat();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                PrintToChat(player, text, votesyes, votesno);
        }

        void ClearList()
        {
            _votedPlayers.Clear();
            _votedPlayersList.Clear();
            votesyes = 0;
            votesno = 0;
        }

        #endregion

        #region Localization

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"NoPermission", "У вас нет доступа к данной команде"},
                {"NoArguments", "Используйте: /{0} help"},
                {
                    "Help",
                    "/{0} yes -- Проголосовать за пропуск\n" +
                    "/{0} no -- Проголосовать против пропуска\n"
                },
                {"Not Going", "Голосование еще не началось"},
                {"Already Voted", "Простите, вы уже проголосовали"},
                {
                    "SkipNight",
                    "Ночь успешно пропущена"
                },
                {"NoSkippedNight", "Ночь не была пропущена"},
                {"Success", "Ваш голос успешно засчитан, ожидайте результат."},
                {"StartVote", "Началось голосование за пропуск ночи"},
                {
                    "StartVoteChat",
                    "Используйте:\n" +
                    "/{0} yes -- Проголосовать за пропуск\n" +
                    "/{0} no -- Проголосовать против пропуска\n"
                },
                {"Already Going", "Голосование уже идет"},
                {"Error", "Вы ввели не правильно команду"}
            }, this);
        }

        #endregion

        #region Config

        private Configuration _config;
        private SettingsCUI _cui;
        private SettingsSkip _settingsSkip = new SettingsSkip();
        private SettingsFastSkip _settingsFastSkip = new SettingsFastSkip();

        private class Configuration
        {
            [JsonProperty(PropertyName = "Включить GUI?")]
            public bool ShowGui = true;

            [JsonProperty(PropertyName = "Разрешение на команды")]
            public string PermAdmin = "skip.use";

            [JsonProperty(PropertyName = "Отображать голоса?")]
            public bool showvotes = true;

            [JsonProperty(PropertyName = "Показывать префикс")]
            public bool showperfix = true;

            [JsonProperty(PropertyName = "Префикс")]
            public string prefix = "SkipNight";

            [JsonProperty(PropertyName = "Цвет префикса")]
            public string ColorPerfix = "#ff7043";

            [JsonProperty(PropertyName = "Команда для голосования")]
            public string Command = "vote";

            [JsonProperty(PropertyName = "Формат сообщение чат")]
            public string textformat = "Проголосовало: {VOTES}";

            [JsonProperty(PropertyName = "Настройка ускореного голосования 'Пропуска ночи'", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<SettingsFastSkip> _settingsfastskips = new List<SettingsFastSkip>() {new SettingsFastSkip()};

            [JsonProperty(PropertyName = "Настройка голосования 'Пропуска ночи'", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<SettingsSkip> _settingsskips = new List<SettingsSkip>() {new SettingsSkip()};

            [JsonProperty(PropertyName = "Номер Панели")]
            public short CUINumber = 0;

            [JsonProperty(PropertyName = "Настройки панели голосования")]
            public Dictionary<short, SettingsCUI> Cui = new Dictionary<short, SettingsCUI> {{0, new SettingsCUI()}};
        }
        
        private class SettingsFastSkip
        {
            [JsonProperty(PropertyName = "Включить ускоренный пропуск ночи?)")]
            public bool fasttime = false;

            [JsonProperty(PropertyName = "Включить пропуск ночи по процентам")]
            public bool fasttimevote = false;
            
            [JsonProperty(PropertyName = "Процент проголосовавших игроков (От 1% до 100%) (голоса общие)")]
            public int VotesPlayers = 60;
        }

        private class SettingsSkip
        {
            
            [JsonProperty(PropertyName = "Время продолжительности голосования")]
            public short TimeVote = 60;
            
            [JsonProperty(PropertyName = "Время проверки на голосования")]
            public short TimeReload = 10;
            
            [JsonProperty(PropertyName = "Время начала голосования (Время для проверки)")]
            public string TimeStart = "20:00";

            [JsonProperty(PropertyName = "Время до какого часа будет голосование (Время для проверки)")]
            public string TimeEnd = "21:00";

            [JsonProperty(PropertyName = "Время установки после голосования")]
            public string TimeSet = "08:00";

            [JsonProperty(PropertyName = "Время автосообщения")]
            public short TimeMessages = 15;
        }

        private class SettingsCUI
        {
            [JsonIgnore] public CuiPanel Panel;
            [JsonIgnore] public CuiButton ButtonYes;
            [JsonIgnore] public CuiElement ButtonYesPanel;
            [JsonIgnore] public CuiButton ButtonNo;
            [JsonIgnore] public CuiElement ButtonNoPanel;
            [JsonIgnore] public CuiLabel TextPanel;
            [JsonIgnore] public CuiElement ElementPanel;
            [JsonIgnore] public CuiElement ElementPanelText;

            [JsonProperty(PropertyName = "Минимальное положение панели")]
            public string PanelMin = "0.2177083 0.02499999";

            [JsonProperty(PropertyName = "Максимальное положение панели")]
            public string PanelMax = "0.3265625 0.1064815";

            [JsonProperty(PropertyName = "Минимальное положение кнопки ✔")]
            public string ButtonMinYes = "0.05241300 0.05681869";

            [JsonProperty(PropertyName = "Максимальное положение кнопки ✔")]
            public string ButtonMaxYes = "0.4880377 0.5340914";

            [JsonProperty(PropertyName = "Минимальное положение кнопки ✖")]
            public string ButtonMinNo = "0.5167453 0.05681815";

            [JsonProperty(PropertyName = "Максимальное положение кнопки ✖")]
            public string ButtonMaxNo = "0.9473673 0.534091";

            [JsonProperty(PropertyName = "Минимальное положение панели текста")]
            public string TextPanelMin = "0.05263185 0.5909092";

            [JsonProperty(PropertyName = "Максимальное положение панели текста")]
            public string TextPanelMax = "0.9473684 0.9659092";

            [JsonProperty(PropertyName = "Цвет текста")]
            public string ColorTextSkip = "#B1B1B1FF";

            [JsonProperty(PropertyName = "Текст Кнопки отказывания")]
            public string TextButtonNo = "✖";
            
            [JsonProperty(PropertyName = "Текст Кнопки согласия")]
            public string TextButtonYes = "✔";
            
            [JsonProperty(PropertyName = "Текст пропуска ночи UI")]
            public string TextPanelCFG = "Пропуск ночи:";

            [JsonProperty(PropertyName = "Размер текста 'Пропуск ночи'")]
            public int SizeText = 14;

            [JsonProperty(PropertyName = "Размер текста Кнопки Согласие")]
            public int SizeTextYes = 24;
            
            [JsonProperty(PropertyName = "Размер текста Кнопки против")]
            public int SizeTextNo = 24;

            [JsonProperty(PropertyName = "Размер текста голоса Согласие")]
            public int SizeVoteYes = 24;
            
            [JsonProperty(PropertyName = "Размер текста голоса против")]
            public int SizeVoteNo = 24;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
            }
            catch (Exception e)
            {
                Puts(e.ToString());
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
            PrintWarning("Создание нового файла конфигурации...");
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        #region Commands

        private bool CommandConsoleYes(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
                return false;

            var player = BasePlayer.FindByID(arg.Connection.userid);
            CommandChatVote(player, "", new[] {"yes"});
            return false;
        }

        private bool CommandConsoleNo(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null)
                return false;

            var player = BasePlayer.FindByID(arg.Connection.userid);
                CommandChatVote(player, "", new[] {"no"});
            return false;
        }

        private void CommandChatVote(BasePlayer player, string command, string[] args)
        {
            var userId = player.UserIDString;
            if (args.Length < 1)
            {
                Reply(player, GetMsg("NoArguments".Replace("{0}", _config.Command), userId));
                return;
            }

            switch (args[0])
            {
                case "yes":
                {
                    if (!_isRunning)
                    {
                        Reply(player, GetMsg("Not Going", userId));
                        return;
                    }

                    if (_votedPlayers.Contains(player))
                    {
                        Reply(player, GetMsg("Already Voted", userId));
                        return;
                    }

                    _votedPlayers.Add(player);
                    _votedPlayersList.Add(player);
                    votesyes++;
                    CuiHelper.DestroyUi(player, "NightVote");
                    if (_config.showvotes)
                    {
                        if (_config.ShowGui)
                        {
                            foreach (var check in _votedPlayersList)
                            {
                                DrawUI(check);
                            }
                        }
                        else
                        {
                            SendVotes();
                        }
                    }

                    HGS();
                    
                    Reply(player, GetMsg("Success", userId));
                    return;
                }

                case "no":
                {
                    if (!_isRunning)
                    {
                        Reply(player, GetMsg("Not Going", userId));
                        return;
                    }

                    if (_votedPlayers.Contains(player))
                    {
                        Reply(player, GetMsg("Already Voted", userId));
                        return;
                    }
                    CuiHelper.DestroyUi(player, "NightVote");
                    _votedPlayers.Add(player);
                    _votedPlayersList.Add(player);
                    votesno++;

                    if (_config.showvotes)
                    {
                        if (_config.ShowGui)
                        {
                            foreach (var check in _votedPlayersList)
                            {
                                DrawUI(check);
                            }
                        }
                        else
                        {
                            SendVotes();
                        }
                    }
                    
                    HGS();
                    Reply(player, GetMsg("Success", userId));
                    return;
                }

                case "open":
                {
                    if (!permission.UserHasPermission(userId, _config.PermAdmin))
                    {
                        Reply(player, GetMsg("NoPermission", userId), header:"Помощник по командам");
                        return;
                    }

                    if (_isRunning)
                    {
                        Reply(player, GetMsg("Already Going", userId));
                        return;
                    }

                    CuiHelper.DestroyUi(player, "SkipNight");
                    StartVote();
                    Reply(player, "Вы начали в ручную пропуск ночи");

                    return;
                }

                case "stop":
                {
                    if (!permission.UserHasPermission(userId, _config.PermAdmin))
                    {
                        Reply(player, GetMsg("NoPermission", userId), header:"Помощник по командам");
                        return;
                    }

                    if (!_isRunning)
                    {
                        Reply(player, GetMsg("Not Going", userId));
                        return;
                    }

                    EndVote();
                    Reply(player, "Вы закончили голосование за пропуск ночи");

                    return;
                }
                case "help":
                {
                    Reply(player, GetMsg("Help".Replace("{0}", _config.Command), userId), header:"Помощник по командам");
                    return;
                }

                default:
                {
                    if (player)
                        Reply(player,GetMsg("Error", userId), header:"Помощник по командам");
                    return;
                }
            }
        }

        #endregion
    }
}