using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;
using Newtonsoft.Json;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("NickeManager", "empty", "1.0.6")]
    [Description("Плагин для управлением никнеймов игроков")]
    public class NickManager : RustPlugin
    {
        #region [Reference] / [Запросы]

        [PluginReference] Plugin ImageLibrary;
        private StoredData DataBase = new StoredData();

        private string GetImg(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        }

        public string GetImage(string shortname, ulong skin = 0) =>
            (string) ImageLibrary?.Call("GetImage", shortname, skin);

        #endregion

        #region [Configuraton] / [Конфигурация]

        static public ConfigData config;


        public class ConfigData
        {
            [JsonProperty(PropertyName = "ZealNickManager")]
            public NickManager ZealNickManager = new NickManager();

            public class NickManager 
            {
                [JsonProperty(PropertyName = "Удалять запрещённые символы/фразы на ?")]
                public bool ReplaceNick;

                [JsonProperty(PropertyName = "Уведомлять игрока о замене никнейм ?")]
                public bool AlertReplaceNick;

                [JsonProperty(PropertyName = "Хранить историю никнеймов игрока ?")]
                public bool HistoryNickSave;

                [JsonProperty(PropertyName = "Разрешение на использование ZealNickManager")]
                public string PermissionUse;

                [JsonProperty(PropertyName = "Запрещенные символы/фразы")]
                public List<string> ReplacesValue = new List<string>();
            }
        }

        public ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                ZealNickManager = new ConfigData.NickManager
                {
                    ReplaceNick = true,
                    AlertReplaceNick = true,
                    HistoryNickSave = true,
                    PermissionUse = "zealnickmanager.use",
                    ReplacesValue = new List<string> {".ru", ".com", ".net"}
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region [Dictionary/Vars] / [Словари/Переменные]

        private string Sharp = "assets/content/ui/ui.background.tile.psd";
        private string Blur = "assets/content/ui/uibackgroundblur.mat";
        private string radial = "assets/content/ui/ui.background.transparent.radial.psd";
        private string regular = "robotocondensed-regular.ttf";

        private string Layer = "BoxNickManager";

        #endregion

        #region [DrawUI] / [Показ UI]

        void PlayerList(BasePlayer player)
        {
            CuiElementContainer Gui = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);

            Gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = HexToRustFormat("#000000F9"),
                    Material = Blur,
                    Sprite = radial
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", "BoxNickManager");

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "close.nickmanager",
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, Layer, "CloseNickManager");

            Gui.Add(new CuiElement
            {
                Name = "Zagolovok",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 35,
                        Text = "ПАНЕЛЬ УПРАВЛЕНИЯ ИГРОВЫМИ НИКАМИ",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.9213709",
                        AnchorMax = "1 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "DescPl",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 25,
                        Text = "Выберите игрока",
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.8881495",
                        AnchorMax = "1 0.9446309"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "BoxPlayers",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.04166667 0.83425",
                        AnchorMax = "0.1536459 0.8731388"
                    }
                }
            });

            int x = 0, y = 0, num = 0;
            foreach (var plobj in BasePlayer.activePlayerList)
            {
                if (x == 8)
                {
                    x = 0;
                    y++;
                }

                Gui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = "nickmanager.info " + plobj.userID,
                            Color = HexToRustFormat("#0000005A"),
                            Material = Blur,
                            FadeIn = 0.1f + (num * 0.01f)
                        },
                        Text =
                        {
                            Text = plobj.displayName,
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#ffffff"),
                            Font = regular,
                            FontSize = 15,
                            FadeIn = 0.1f + (num * 0.01f)
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0 + (x * 1.02)} {0 - (y * 1.1)}",
                            AnchorMax = $"{1 + (x * 1.02)} {1 - (y * 1.1)}"
                        }
                    }, "BoxPlayers", "Player" + num);
                x++;
                num++;
            }

            CuiHelper.AddUi(player, Gui);
        }

        void PlayerInfo(BasePlayer player, ulong SteamID)
        {
            CheckDataBase(BasePlayer.FindByID(SteamID));
            CuiElementContainer Gui = new CuiElementContainer();

            CuiHelper.DestroyUi(player, "BoxPlayers");
            CuiHelper.DestroyUi(player, "Zagolovok");
            CuiHelper.DestroyUi(player, "DescPl");

            var infopl = DataBase.NickManager[SteamID];

            Gui.Add(new CuiElement
            {
                Name = "ZagolovokInfoPl",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        FontSize = 35,
                        Text = "ИНФОРМАЦИЯ ИГРОКА : " + infopl.Name,
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.9213709",
                        AnchorMax = "1 1"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "AvatarBG",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#FFFFFFA4")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4360861 0.6925457",
                        AnchorMax = "0.5627829 0.9183521"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "Avatar",
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = "1 1 1 1",
                        Png = GetImage(infopl.SteamID.ToString(), 0)
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4372173 0.6945619",
                        AnchorMax = "0.5616518 0.9163328"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "Sprite",
                Parent = "Avatar",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#000000AA"),
                        Sprite = radial
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "0.99 0.99"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "SettingsNickIco",
                Parent = "Avatar",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Color = HexToRustFormat("#C7C6C6FF"),
                        Sprite = Sharp,
                        Png = GetImg("SettingsNick")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.8310199 0.826149",
                        AnchorMax = "0.9984439 0.9931549"
                    }
                }
            });

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "changename.gui " + SteamID,
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = " "
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "SettingsNickIco", "ButtonSettings");

            Gui.Add(new CuiElement
            {
                Name = "Nick",
                Parent = Layer,
                FadeOut = 1f,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 23,
                        Text = infopl.Name,
                        Font = "robotocondensed-regular.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3212669 0.641129",
                        AnchorMax = "0.6804298 0.6875"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "Line1",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat("#CBCBCBFF")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3829193 0.6401247",
                        AnchorMax = "0.6193514 0.6411328"
                    }
                }
            });

            Gui.Add(new CuiElement
            {
                Name = "SteamID",
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 14,
                        Text = infopl.SteamID.ToString(),
                        Font = "robotocondensed-regular.ttf"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3212669 0.6088712",
                        AnchorMax = "0.6804298 0.6421358"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            if (config.ZealNickManager.HistoryNickSave == true)
            {
                Gui.Add(new CuiElement
                {
                    Name = "ZagHistoryNick",
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FontSize = 25,
                            Text = "ИСТОРИЯ НИКНЕЙМОВ",
                            Font = regular
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.5577981",
                            AnchorMax = "1 0.6041691"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });

                HistoryNick(player, SteamID);
            }

            CuiHelper.AddUi(player, Gui);
        }

        void HistoryNick(BasePlayer player, ulong SteamID)
        {
            CuiElementContainer Gui = new CuiElementContainer();

            CuiHelper.DestroyUi(player, "BoxHistoryNick");

            Gui.Add(new CuiElement
            {
                Name = "BoxHistoryNick",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0 0 0 0"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0.550926"
                    }
                }
            });

            if (DataBase.NickManager[SteamID].NickHistory.Count == 0)
            {
                Gui.Add(new CuiElement
                {
                    Name = "HistoryEmpty",
                    Parent = "BoxHistoryNick",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Align = TextAnchor.UpperCenter,
                            Color = HexToRustFormat("#EAEAEAFF"),
                            FadeIn = 1f,
                            Font = regular,
                            FontSize = 35,
                            Text = "История никнеймов пуста"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        new CuiOutlineComponent
                        {
                            Color = HexToRustFormat("#000000AE"),
                            Distance = "0.5 0.5"
                        }
                    }
                });
            }

            int x = 0, y = 0, num = 0;
            foreach (var nick in DataBase.NickManager[SteamID].NickHistory)
            {
                if (x == 7)
                {
                    x = 0;
                    y++;
                }


                string playernick = nick.Key;
                if (playernick.Length >= 13)
                {
                    playernick = playernick.Remove(playernick.Length - 3) + "...";
                }

                Gui.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = " ",
                            Color = HexToRustFormat("#0000005A"),
                            Material = Blur,
                            FadeIn = 0.1f + (num * 0.01f)
                        },
                        Text =
                        {
                            Text = $"{playernick} | {nick.Value}",
                            Align = TextAnchor.MiddleCenter,
                            Color = HexToRustFormat("#ffffff"),
                            Font = regular,
                            FontSize = 15,
                            FadeIn = 0.1f + (num * 0.01f)
                        },
                        RectTransform =
                        {
                            AnchorMin = $"{0.0578426 + (x * 0.126)} {0.9139073 - (y * 0.0866)}",
                            AnchorMax = $"{0.1808237 + (x * 0.126)} {0.9917219 - (y * 0.0866)}"
                        }
                    }, "BoxHistoryNick", "Nick" + num);

                x++;
                num++;
            }

            CuiHelper.AddUi(player, Gui);
        }

        void ChangeName(BasePlayer player, ulong SteamID)
        {
            CuiElementContainer Gui = new CuiElementContainer();
            var infopl = DataBase.NickManager[SteamID];
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, true);
            CuiHelper.DestroyUi(player, "Nick");

            string text = " ";

            Gui.Add(new CuiElement
            {
                Name = "ChangeNameInput",
                Parent = Layer,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        CharsLimit = 20,
                        Color = HexToRustFormat("#EAEAEAFF"),
                        Command = $"changename {SteamID} {text}",
                        Text = text,
                        FontSize = 23,
                        Font = regular
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3212669 0.6453704",
                        AnchorMax = "0.6804298 0.6842592"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            CuiHelper.AddUi(player, Gui);
        }

        #endregion

        #region [ChatCommand] / [Чат команды]

        [ChatCommand("steamid")]
        private void NickManagerGui(BasePlayer player)
        {
            CheckDataBase(player);
            if (!permission.UserHasPermission(player.UserIDString, config.ZealNickManager.PermissionUse))
            {
                SendReply(player, "У вас нет прав на использование данной команды");
                return;
            }

            PlayerList(player);
        }

        [ChatCommand("replace")]
        private void Replaces(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            CheckDataBase(player);
            foreach (var str in config.ZealNickManager.ReplacesValue)
            {
                player.displayName = player.displayName.Replace(str, "");
            }

            DataBase.NickManager[player.userID].Name = player.displayName;
            Puts(player.displayName);
        }

        [ConsoleCommand("changename.gui")]
        private void ChangeNameText(ConsoleSystem.Arg args)
        {
            CheckDataBase(args.Player());
            string msg = args.Args[0];

            ulong steamid = Convert.ToUInt64(msg);
            var initiator = args.Player();

            ChangeName(initiator, steamid);
        }

        [ConsoleCommand("nickmanager.info")]
        private void NickManagerInfoPlayer(ConsoleSystem.Arg args)
        {
            CheckDataBase(args.Player());
            string msg = args.Args[0];

            ulong findplayer = Convert.ToUInt64(msg);
            var initiator = args.Player();

            PlayerInfo(initiator, findplayer);
        }

        [ConsoleCommand("changename")]
        private void ChangeName(ConsoleSystem.Arg args)
        {
            args.Player().SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false);
            CheckDataBase(args.Player());
            if (!args.Player().IsAdmin) return;

            string nick = null;
            foreach (var arg in args.Args)
            {
                if (arg != null)
                {
                    if (arg != args.Args[0])
                    {
                        nick += arg;
                    }
                }
            }

            string SteamID = args.Args[0];
            ulong FindPlayer = Convert.ToUInt64(SteamID);
            BasePlayer player = BasePlayer.FindByID(FindPlayer);

            CuiHelper.DestroyUi(args.Player(), "ChangeNameInput");
            player.displayName = nick;
            player.IPlayer.Name = nick;
            player.Connection.username = nick;
            player.SendNetworkUpdate();
            player.SendEntityUpdate();

            DataBase.NickManager[FindPlayer].Name = nick;
            if (!DataBase.NickManager[FindPlayer].NickHistory.ContainsKey(nick))
            {
                DataBase.NickManager[FindPlayer].NickHistory.Add(nick, DateTime.Now.ToString("d"));
            }

            DataBase.NickManager[FindPlayer].NickReplace = nick;
            DataBase.NickManager[FindPlayer].ReplaceNick = true;
            SaveData();

            CuiElementContainer Gui = new CuiElementContainer();

            Gui.Add(new CuiElement
            {
                Name = "Nick",
                Parent = Layer,
                FadeOut = 0.5f,
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                        FontSize = 23,
                        Text = DataBase.NickManager[FindPlayer].Name,
                        Font = "robotocondensed-regular.ttf",
                        FadeIn = 0.5f
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3212669 0.641129",
                        AnchorMax = "0.6804298 0.6875"
                    },
                    new CuiOutlineComponent
                    {
                        Color = HexToRustFormat("#000000AE"),
                        Distance = "0.5 0.5"
                    }
                }
            });

            if (config.ZealNickManager.HistoryNickSave == true)
            {
                HistoryNick(args.Player(), FindPlayer);
            }

            CuiHelper.AddUi(args.Player(), Gui);
        }

        [ConsoleCommand("close.nickmanager")]
        private void NickManagerGuiClose(ConsoleSystem.Arg args)
        {
            CheckDataBase(args.Player());
            args.Player().SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false);
            if (args.Player() != null)
            {
                var player = args.Player();
                CuiHelper.DestroyUi(player, Layer);
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        void CheckDataBase(BasePlayer player)
        {
            if (!DataBase.NickManager.ContainsKey(player.userID)) AddPlayer(player);
        }

        void AddPlayer(BasePlayer player)
        {
            var data = new NickBD
            {
                Name = player.displayName,
                SteamID = player.userID,
                NickReplace = "",
                ReplaceNick = false
            };

            DataBase.NickManager.Add(player.userID, data);
            SaveData();
        }

        void OnPlayerInit(BasePlayer player)
        {
            CheckDataBase(player);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ChatMute, false);
            if (DataBase.NickManager[player.userID].ReplaceNick == true)
            {
                if (player.displayName != DataBase.NickManager[player.userID].NickReplace)
                {
                    player.displayName = DataBase.NickManager[player.userID].NickReplace;
                    player.IPlayer.Name = DataBase.NickManager[player.userID].NickReplace;
                    player.Connection.username = DataBase.NickManager[player.userID].NickReplace;
                }
                else
                {
                    DataBase.NickManager[player.userID].ReplaceNick = false;
                }
            }

            if (config.ZealNickManager.ReplaceNick == true)
            {
                string oldnick = player.displayName;
                string newnick = "";

                foreach (var str in config.ZealNickManager.ReplacesValue)
                {
                    if (player.displayName.Contains(str))
                    {
                        player.displayName = player.displayName.Replace(str, "");
                        player.IPlayer.Name = player.displayName;
                        player.Connection.username = player.displayName;
                        newnick = player.displayName;
                        DataBase.NickManager[player.userID].Name = newnick;
                        PrintWarning($"Ник : {oldnick} заменен на : {newnick}");
                        if (config.ZealNickManager.AlertReplaceNick == true)
                        {
                            SendReply(player,
                                $"Ваш никнейм заменён на : {newnick}, так как в нем содержится запрещённые символы/фразы");
                        }
                    }
                }
            }
            else
            {
                foreach (var str in config.ZealNickManager.ReplacesValue)
                {
                    if (player.displayName.Contains(str))
                    {
                        PrintWarning(
                            $"Ник : {player.displayName} запрещён, но отключена функция замены запрещённых символов/фраз");
                    }
                }
            }

            if (config.ZealNickManager.HistoryNickSave == true)
            {
                if (!DataBase.NickManager[player.userID].NickHistory.ContainsKey(player.displayName))
                {
                    DataBase.NickManager[player.userID].NickHistory.Add(player.displayName, DateTime.Now.ToString("d"));
                    PrintWarning(
                        $"Игроку : {player.userID} в историю никнеймов добавлен ник : {player.displayName} | Дата добавления : {DateTime.Now.ToString("d")}");
                }
            }
        }

        void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintError($"На сервере не установлен плагин [ImageLibrary]");
                Interface.Oxide.UnloadPlugin(Title);
                return;
            }

            Puts("Автор плагина : Kira | Контакты : Discord - -Kira#1920");
            Puts($"Плагин : {Title}");
            Puts("Приятного пользования ^-^");

            LoadData();
            ImageLibrary.Call("AddImage", $"https://i.imgur.com/3c2jZzR.png", "SettingsNick");
            permission.RegisterPermission(config.ZealNickManager.PermissionUse, this);
            foreach (var player in BasePlayer.activePlayerList)
            {
                CheckDataBase(player);
                if (config.ZealNickManager.ReplaceNick == true)
                {
                    string oldnick = player.displayName;
                    string newnick = "";

                    foreach (var str in config.ZealNickManager.ReplacesValue)
                    {
                        if (player.displayName.Contains(str))
                        {
                            player.displayName = player.displayName.Replace(str, "");
                            newnick = player.displayName;
                            DataBase.NickManager[player.userID].Name = newnick;
                            PrintWarning($"Ник : {oldnick} заменен на : {newnick}");
                            if (config.ZealNickManager.AlertReplaceNick == true)
                            {
                                SendReply(player,
                                    $"Ваш никнейм заменён на : {newnick}, так как в нем содержится запрещённые символы/фразы");
                            }
                        }
                    }
                }
                else
                {
                    foreach (var str in config.ZealNickManager.ReplacesValue)
                    {
                        if (player.displayName.Contains(str))
                        {
                            PrintWarning(
                                $"Ник : {player.displayName} запрещён, но отключена функция замены запрещённых символов/фраз");
                        }
                    } 
                }

                if (config.ZealNickManager.HistoryNickSave == true)
                {
                    if (!DataBase.NickManager[player.userID].NickHistory.ContainsKey(player.displayName))
                    {
                        DataBase.NickManager[player.userID].NickHistory
                            .Add(player.displayName, DateTime.Now.ToString("d"));
                        PrintWarning(
                            $"Игроку : {player.userID} в историю никнеймов добавлен ник : {player.displayName} | Дата добавления : {DateTime.Now.ToString("d")}");
                    }
                }
            }
        }

        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [DataBase] / [Хранение данных]

        class StoredData
        {
            public Dictionary<ulong, NickBD> NickManager = new Dictionary<ulong, NickBD>();
        }


        class NickBD
        {
            public string Name;
            public ulong SteamID;
            public string NickReplace;
            public bool ReplaceNick;
            public Dictionary<string, string> NickHistory = new Dictionary<string, string>();

            public NickBD()
            {
            }
        }


        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception e)
            {
                DataBase = new StoredData();
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

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