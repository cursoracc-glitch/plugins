using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("SpectatorPlus", "https://shoprust.ru/", "0.0.4")]
    class SpectatorPlus : RustPlugin
    {
        [PluginReference] Plugin MultiFighting;

        #region CFG

        private static Configuration config = new Configuration();

        public class Configuration
        {
            [JsonProperty("Положение панели")] public Settings Setting = new Settings();
            [JsonProperty("Настройка кнопок")] public List<Buttons> ButtonsSet = new List<Buttons>();
            [JsonProperty("Причины бана")] public List<BanReason> ReasonBan = new List<BanReason>();

            [JsonProperty("Другие настройки")] public AnotherSettings Another = new AnotherSettings();


            [JsonProperty(
                "Положения кнопок с дествиями(нужно для размещения кнопок в заданном порядке) не рекомендую менять это, а то кнопки могут быть отрисованы неправильно")]
            public List<ButtonPos> Positions = new List<ButtonPos>();

            internal class Settings
            {
                [JsonProperty("AnchorMin")] public string MainAnchorMin;
                [JsonProperty("AnchorMax")] public string MainAnchorMax;
                [JsonProperty("OffsetMin")] public string MainOffsetMin;
                [JsonProperty("OffsetMax")] public string MainOffsetMax;
            }

            internal class Buttons
            {
                [JsonProperty("Надпись на кнопке")] public string Title;

                [JsonProperty("Спец-Функция(если нужна просто команда оставьте поле пустым)")]
                public string Funk;

                [JsonProperty("Команда ([ID] заменяется на ID наблюдюдаемого)")]
                public string Command;

                [JsonProperty("Пермишн на отображение")]
                public string Perm;
            }

            internal class BanReason
            {
                [JsonProperty("Название")] public string ReasonName;
                [JsonProperty("Команда")] public string BanCommand;
            }

            internal class AnotherSettings
            {
                [JsonProperty("На сколько хп хилять/ранить наблюдаемого по кнопке Heal/Hurt")]
                public float HP;
            }


            internal class ButtonPos
            {
                [JsonProperty("OffsetMin")] public string oMin;
                [JsonProperty("OffsetMax")] public string oMax;
            }


            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    Setting = new Settings
                    {
                        MainAnchorMin = "1 0.5",
                        MainAnchorMax = "1 0.5",
                        MainOffsetMin = "-200 -170",
                        MainOffsetMax = "-10 170"
                    },
                    ButtonsSet = new List<Buttons>
                    {
                        new Buttons
                        {
                            Title = "BAN",
                            Funk = "BanMenu",
                            Command = "",
                            Perm = "SpectatorPlus.canban"
                        },
                        new Buttons
                        {
                            Title = "",
                            Funk = "HPControl",
                            Command = "",
                            Perm = "SpectatorPlus.CanHPControl"
                        },
                        new Buttons
                        {
                            Title = "CALL",
                            Funk = "",
                            Command = "call [ID]",
                            Perm = "SpectatorPlus.Cancall"
                        }
                    },
                    ReasonBan = new List<BanReason>
                    {
                        new BanReason
                        {
                            ReasonName = "СОФТ",
                            BanCommand = "ban [ID] 30d cheats",
                        },
                        new BanReason
                        {
                            ReasonName = "МАКРОС",
                            BanCommand = "ban [ID] 30d macros",
                        }
                    },
                    Another = new AnotherSettings
                    {
                        HP = 10f
                    },
                    Positions = new List<ButtonPos>
                    {
                        new ButtonPos
                        {
                            oMin = "-92 83",
                            oMax = "-2 108"
                        },
                        new ButtonPos
                        {
                            oMin = "2 83",
                            oMax = "92 108"
                        },
                        new ButtonPos
                        {
                            oMin = "-92 55",
                            oMax = "-2 80"
                        },
                        new ButtonPos
                        {
                            oMin = "2 55",
                            oMax = "92 80"
                        },
                        new ButtonPos
                        {
                            oMin = "-92 27",
                            oMax = "-2 52"
                        },
                        new ButtonPos
                        {
                            oMin = "2 27",
                            oMax = "92 52"
                        },
                        new ButtonPos
                        {
                            oMin = "-92 -1",
                            oMax = "-2 24"
                        },
                        new ButtonPos
                        {
                            oMin = "2 -1",
                            oMax = "92 24"
                        }
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                Puts("!!!!ОШИБКА КОНФИГУРАЦИИ!!!! создаем новую");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
        #region LANG

        private void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"NO_ADM", "You not admin!"},
                {"NO_PERM", "You dont have permission"},
                {"NOT_FOUND", "Not found"},
                {"NOT_FOUND_RAYCAST", "Not found in line of sight"},
                {"CANT_SPEC_ADMIN", "You cant spectating another admin"},
                {"START_SPEC", "Start spectating"}
            }, this, "en");
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"NO_ADM", "НЕ АДМИН!"},
                {"NO_PERM", "НЕТ РАЗРЕШЕНИЯ!"},
                {"NOT_FOUND", "Не найден!"},
                {"NOT_FOUND_RAYCAST", "Не найден по линии взгляда!"},
                {"CANT_SPEC_ADMIN", "Вы не можете наблюдать за другим админом"},
                {"START_SPEC", "Начинаем слежку"}
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно!");
        }

        #endregion
        #region Load/Unload

        void OnServerInitialized()
        {
            for (int i = 0; i < config.ButtonsSet.Count; i++)
            {
                if (config.ButtonsSet[i].Perm.Length <= 0) return;
                permission.RegisterPermission(config.ButtonsSet[i].Perm, this);
            }

            permission.RegisterPermission("SpectatorPlus.canspectate", this);
        }

        string spectateLayer = "specLayer";
        string spectateLayerBan = "specLayerBan";
        private string spectateHPControl = "specHPControl";

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, spectateLayer);
                CuiHelper.DestroyUi(player, spectateLayerBan);
            }
        }

        #endregion
        #region Commands

        [ChatCommand("spec")]
        void StartSpectate(BasePlayer admin, string command, string[] args)
        {
            if (!admin.IsAdmin)
            {
                admin.ChatMessage(lang.GetMessage("NO_ADM", this, admin.UserIDString));
                return;
            }

            if (!permission.UserHasPermission(admin.UserIDString, "SpectatorPlus.canspectate"))
            {
                admin.ChatMessage(lang.GetMessage("NO_PERM", this, admin.UserIDString));
                return;
            }

            if (args.Length == 0)
            {
                RaycastHit hit;
                if (!Physics.Raycast(admin.eyes.HeadRay(), out hit, float.MaxValue,
                        LayerMask.GetMask("Player (Server)")))
                {
                    admin.ChatMessage(lang.GetMessage("NOT_FOUND_RAYCAST", this, admin.UserIDString));
                    return;
                }
                else
                {
                    var targetPlayer = hit.GetEntity() as BasePlayer;
                    if (targetPlayer == null)
                    {
                        admin.ChatMessage(lang.GetMessage("NOT_FOUND_RAYCAST", this, admin.UserIDString));
                        return;
                    }
                    else
                    {
                        if (targetPlayer.IsAdmin)
                        {
                            admin.ChatMessage(lang.GetMessage("CANT_SPEC_ADMIN", this, admin.UserIDString));
                            return;
                        }

                        rust.RunClientCommand(admin, $"spectate {targetPlayer.userID}");
                    }
                }
            }
            else
            {
                var targetPlayer = BasePlayer.Find(args[0]);
                if (targetPlayer == null)
                {
                    admin.ChatMessage("Не найден!");
                    return;
                }
                else
                {
                    admin.ChatMessage("Начинаем слежку!");
                    rust.RunClientCommand(admin, $"spectate {targetPlayer.userID}");
                }
            }
        }

        [ChatCommand("specstop")]
        private void SpectatingEnd(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, spectateLayer);
            rust.RunClientCommand(player, "respawn");
        }

        object OnPlayerSpectateEnd(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, spectateLayer);
            return null;
        }

        private object CanSpectateTarget(BasePlayer player, string filter)
        {
            var target = BasePlayer.Find(filter);
            if (player == target)
            {
                player.ChatMessage("Вы не можете следить за самим собой.");
                return false;
            }

            if (target.IsAdmin)
            {
                player.ChatMessage("Вы не можете следить за другими админами.");
                return false;
            }
            if(target.IsDead() || target.IsSleeping())
            {
                player.ChatMessage("Игрок не валидный!");
                return false;
            }
            CuiHelper.DestroyUi(player, spectateLayer);

            if (permission.UserHasPermission(player.UserIDString, "SpectatorPlus.canspectate"))
            {
                
                timer.Once(0.1f, () =>  
                    {
                        if (string.IsNullOrEmpty(filter))
                        {
                            Puts($"{player.displayName} tries to spectate with a nulled player!");
                            rust.RunClientCommand(player, "respawn");
                        }
                        else
                        {
                            Puts($"{player.displayName} tries to spectate with a filter: {filter}");
                            DrawBlockInfo(player, filter);
                        }
                    });
            }
            else
            {
                player.ChatMessage(lang.GetMessage("NO_PERM", this, player.UserIDString));
                return false;
            }

            return null;
            }

            #endregion

            #region GUI

            private void DrawBlockInfo(BasePlayer player, string filter)
            {
                var currentSuspect = BasePlayer.Find(filter);
                CuiHelper.DestroyUi(player, spectateLayer);

                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    RectTransform =
                    {
                        AnchorMin = $"{config.Setting.MainAnchorMin}", AnchorMax = $"{config.Setting.MainAnchorMax}",
                        OffsetMin = $"{config.Setting.MainOffsetMin}", OffsetMax = $"{config.Setting.MainOffsetMax}"
                    },
                    Image = {Color = "0, 0, 0, 0.5"},
                }, "Overlay", spectateLayer);

                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "SPECTATOR+",
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-bold.ttf"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-95 0", OffsetMax = "95 25"
                    }
                }, spectateLayer);

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-92 3", OffsetMax = "92 28"},
                    Button = {Color = "250, 0, 0, 0.70", Command = "chat.say /specstop"},
                    Text =
                    {
                        Text = $"STOP", Font = "robotocondensed-bold.ttf", Color = HexToRustFormat("#FFFFFF"),
                        Align = TextAnchor.MiddleCenter, FontSize = 18
                    },
                }, spectateLayer);
                if (MultiFighting)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform =
                            {AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-92 -28", OffsetMax = "62 -3"},
                        Button = {Color = "50, 50, 50, 0.36", Command = ""},
                        Text =
                        {
                            Text = $"{currentSuspect.displayName}", Font = "robotocondensed-bold.ttf",
                            Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, FontSize = 18
                        },
                    }, spectateLayer);
                    string suspectid = currentSuspect.userID.ToString();
                    var isSteamSprite = IsSteam(suspectid) == "IS_STEAM"
                        ? "assets/icons/steam.png"
                        : "assets/icons/poison.png";

                    container.Add(new CuiElement
                        {
                            Parent = spectateLayer,
                            Components =
                            {
                                new CuiImageComponent {Color = HexToRustFormat("#FFFFFF"), Sprite = isSteamSprite},
                                new CuiRectTransformComponent
                                    {AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "65 -28", OffsetMax = "92 -3"},
                            }
                        }
                    );
                }
                else
                {
                    container.Add(new CuiButton
                    {
                        RectTransform =
                            {AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-92 -28", OffsetMax = "92 -3"},
                        Button = {Color = "50, 50, 50, 0.36", Command = ""},
                        Text =
                        {
                            Text = $"{currentSuspect.displayName}", Font = "robotocondensed-bold.ttf",
                            Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, FontSize = 18
                        },
                    }, spectateLayer);
                }

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = "-92 -59", OffsetMax = "92 -32"},
                    Button = {Color = "50, 50, 50, 0.36", Command = $""},
                    Text =
                    {
                        Text = $"{currentSuspect.userID}", Font = "robotocondensed-bold.ttf",
                        Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, FontSize = 18
                    },
                }, spectateLayer);
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-92 31", OffsetMax = "-20 55"},
                    Button = {Color = "50, 50, 50, 0.36", Command = $"spectatorbutton {currentSuspect.UserIDString} prevplayer"},
                    Text =
                    {
                        Text = "<<<", Font = "robotocondensed-bold.ttf",
                        Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, FontSize = 18
                    },
                }, spectateLayer);
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "20 31", OffsetMax = "92 55"},
                    Button = {Color = "50, 50, 50, 0.36", Command = $"spectatorbutton {currentSuspect.UserIDString} nextplayer"},
                    Text =
                    {
                        Text = ">>>", Font = "robotocondensed-bold.ttf",
                        Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, FontSize = 18
                    },
                }, spectateLayer);
                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-18 31", OffsetMax = "18 55"},
                    Button = {Color = "50, 50, 50, 0.36", Command = ""},
                    Text =
                    {
                        Text = (BasePlayer.activePlayerList.IndexOf(currentSuspect) + 1).ToString(), Font = "robotocondensed-bold.ttf",
                        Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, FontSize = 18
                    },
                }, spectateLayer);
                for (int i = 0; i < config.ButtonsSet.Count; i++)
                {
                    DrawButtons(player, container, currentSuspect, i);
                }
                CuiHelper.AddUi(player, container);
            }
            
            void DrawButtons(BasePlayer player, CuiElementContainer container, BasePlayer currentSuspect, int i)
            {
                if (!permission.UserHasPermission(player.UserIDString, config.ButtonsSet[i].Perm))
                {
                    container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", 
                            AnchorMax = "0.5 0.5", 
                            OffsetMin = config.Positions[i].oMin,
                            OffsetMax = config.Positions[i].oMax
                        },

                        Button =
                        {
                            Color = "50, 50, 50, 0.36",
                            Command = ""
                        },

                        Text =
                        {
                            Text = "---", Font = "robotocondensed-bold.ttf",
                            Color = HexToRustFormat("#FFFFFF"),
                            Align = TextAnchor.MiddleCenter, FontSize = 18
                        },
                    }, spectateLayer);
                    return;
                }

                if (config.ButtonsSet[i].Funk.Length <= 0)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = config.Positions[i].oMin,
                            OffsetMax = config.Positions[i].oMax
                        },

                        Button =
                        {
                            Color = "100, 0, 0, 0.36",
                            Command = config.ButtonsSet[i].Command.Replace("[ID]", currentSuspect.UserIDString)
                        },

                        Text =
                        {
                            Text = config.ButtonsSet[i].Title, Font = "robotocondensed-bold.ttf",
                            Color = HexToRustFormat("#FFFFFF"),
                            Align = TextAnchor.MiddleCenter, FontSize = 18
                        },
                    }, spectateLayer);
                }
                else if (config.ButtonsSet[i].Funk == "BanMenu")
                {
                    container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = config.Positions[i].oMin,
                            OffsetMax = config.Positions[i].oMax
                        },

                        Button =
                        {
                            Color = "100, 0, 0, 0.36", Command = $"spectatorbutton {currentSuspect.UserIDString} BanMenu"
                        },

                        Text =
                        {
                            Text = config.ButtonsSet[i].Title, Font = "robotocondensed-bold.ttf",
                            Color = HexToRustFormat("#FFFFFF"),
                            Align = TextAnchor.MiddleCenter, FontSize = 18
                        },
                    }, spectateLayer);
                }
                else if (config.ButtonsSet[i].Funk == "HPControl")
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = config.Positions[i].oMin,
                            OffsetMax = config.Positions[i].oMax
                        },
                        Image = {Color = "0, 0, 0, 0.3"}
                    }, spectateLayer, spectateHPControl);
                    container.Add(new CuiButton
                    {
                        RectTransform =
                            {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45 -12", OffsetMax = "-2 12"},

                        Button =
                        {
                            Color = "0, 100, 0, 0.36",
                            Command = $"spectatorbutton {currentSuspect.UserIDString} HPControll heal"
                        },

                        Text =
                        {
                            Text = "HEAL", Font = "robotocondensed-bold.ttf", Color = HexToRustFormat("#FFFFFF"),
                            Align = TextAnchor.MiddleCenter, FontSize = 18
                        },
                    }, spectateHPControl);
                    container.Add(new CuiButton
                    {
                        RectTransform =
                            {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "2 -12", OffsetMax = "45 12"},

                        Button =
                        {
                            Color = "100, 0, 0, 0.36",
                            Command = $"spectatorbutton {currentSuspect.UserIDString} HPControll hurt"
                        },

                        Text =
                        {
                            Text = "HURT", Font = "robotocondensed-bold.ttf", Color = HexToRustFormat("#FFFFFF"),
                            Align = TextAnchor.MiddleCenter, FontSize = 18
                        },
                    }, spectateHPControl);
                }
                else
                    PrintWarning(
                        $"Спец-Функции с именем {config.ButtonsSet[i].Funk} не существует. Проверьте параметр Спец-Функция в Параметре кнопки {i}  конфиге плагина");
            }
            
            #region InterfaceButtons
            
            [ConsoleCommand("spectatorbutton")]
            void Button(ConsoleSystem.Arg arg)
            {
                BasePlayer player = arg.Player();
                if (!player.IsAdmin) return;
                var target = BasePlayer.Find(arg.Args[0]);
                switch (arg.Args[1])
                {
                    case "HPControll":
                        switch (arg.Args[2])
                        {
                            case "heal":
                                target.Heal(config.Another.HP);
                                player.ChatMessage($"Вы похиляли наблюдаемого");
                                return;

                            case "hurt":
                                target.Hurt(config.Another.HP);
                                player.ChatMessage($"Вы ранили наблюдаемого");
                                return;
                        }

                        break;
                    case "BanMenu":
                        CuiElementContainer bancontainer = new CuiElementContainer();
                        CuiHelper.DestroyUi(player, spectateLayerBan);
                        bancontainer.Add(new CuiPanel
                        {
                            CursorEnabled = true,
                            RectTransform =
                                {AnchorMin = "0 0.5", AnchorMax = "0 0.5", OffsetMin = "-195 -170", OffsetMax = "-5 170"},
                            Image = {Color = "0, 0, 0, 0"},
                        }, spectateLayer, spectateLayerBan);

                        for (int i = 0; i < config.ReasonBan.Count; i++)
                        {
                            bancontainer.Add(new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin = "0.5 1", AnchorMax = "0.5 1", OffsetMin = $"-92 {(-28 - (31 * i))}",
                                    OffsetMax = $"92 {(-3 - (29 * i))}"
                                },
                                Button =
                                {
                                    Color = "70, 0, 0, 1",
                                    Command = config.ReasonBan[i].BanCommand.Replace("[ID]", $"{target.UserIDString}")
                                },
                                Text =
                                {
                                    Text = $"{config.ReasonBan[i].ReasonName}", Font = "robotocondensed-bold.ttf",
                                    Color = HexToRustFormat("#FFFFFF"), Align = TextAnchor.MiddleCenter, FontSize = 18
                                },
                            }, spectateLayerBan);
                        }
                        CuiHelper.AddUi(player, bancontainer);
                        break;
                    case "nextplayer":
                    {
                        int index = BasePlayer.activePlayerList.IndexOf(target) + 1;
                        if (index == BasePlayer.activePlayerList.Count) index = 0;
                        if (BasePlayer.activePlayerList[index].IsAdmin) index++;
                        if (target.IsDead() || target.IsSleeping()) index++;
                        rust.RunClientCommand(player, $"spectate {BasePlayer.activePlayerList[index].userID}");
                        break;
                    }
                    case "prevplayer":
                    {
                        int index = BasePlayer.activePlayerList.IndexOf(target) - 1;
                        if (index < 0) index = BasePlayer.activePlayerList.Count - 1;
                        if (BasePlayer.activePlayerList[index].IsAdmin) index--;
                        if (target.IsDead() || target.IsSleeping()) index--;
                        rust.RunClientCommand(player, $"spectate {BasePlayer.activePlayerList[index].userID}");
                        break;
                    }
                }
            }

            #endregion

            #endregion
            #region HelpMethods

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

            string IsSteam(string suspectid)
            {
                if (MultiFighting != null)
                {
                    var player = BasePlayer.Find(suspectid);
                    if (player == null)
                    {
                        return "ERROR #1";
                    }

                    var obj = MultiFighting.CallHook("IsSteam", player.Connection);
                    if (obj is bool)
                    {
                        if ((bool) obj)
                        {
                            return ("IS_STEAM");
                        }
                        else
                        {
                            return ("IS_PIRATE");
                        }
                    }
                    else
                    {
                        return "ERROR #2";
                    }
                }
                else return ("IS_STEAM");
            }

            #endregion
    }
}