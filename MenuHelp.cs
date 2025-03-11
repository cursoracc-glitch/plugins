using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("Menu Help", "OxideBro", "0.1.1")]
    class MenuHelp : RustPlugin
    {
        #region Class
        public class MenuList
        {
            public string Type;
            public string NameUI;
            public string Command;
            public int FrontSize;
            public string BackgroundColor;
            public List<Messages> MessagesList = new List<Messages>();
        }

        public class Messages
        {
            public string Message;
            public string Date;
        }
        
        #endregion

        #region Config
        string Title;
        string AnchorMin;
        string AnchorMax;
        string ButtonsColor;
        string BackgroundColor;
        bool EnabledLogin;
        protected override void LoadDefaultConfig()
        {
            GetVariable(Config, "Титл главного раздела", out Title, "МЕНЮ ПОМОЩИ ИГРОКАМ");
            GetVariable(Config, "Anchor Min основного меню", out AnchorMin, "0.25 0.1993055");
            GetVariable(Config, "Anchor Max основного меню", out AnchorMax, "0.75 0.8006945");
            GetVariable(Config, "Цвет фона основного меню", out BackgroundColor, "0 0 0 0.50");
            GetVariable(Config, "Цвет кнопок основного меню", out ButtonsColor, "0.92 0.28 0.28 0.7");
            GetVariable(Config, "Открывать меню при каждом заходе игрока", out EnabledLogin, false);
            SaveConfig();
        }

        public static void GetVariable<T>(DynamicConfigFile config, string name, out T value, T defaultValue)
        {
            config[name] = value = config[name] == null ? defaultValue : (T)Convert.ChangeType(config[name], typeof(T));
        }
        #endregion


        #region UI
        private string Layer = "HELP";

        private void HelpGUI(BasePlayer player, bool section = false, string name = "", bool bReturn = false, int numbr = 0)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => HelpGUI(player));
            }
            CuiHelper.DestroyUi(player, Layer);
            Dictionary<string, string> List = new Dictionary<string, string>();
            var container = new CuiElementContainer();
            float gap = -0.0f;
            float width = 0.3f;
            float height = 0.27f;
            float startxBox = 0.05f;
            float startyBox = 0.85f - height;
            float xmin = startxBox;
            float ymin = startyBox;
            var reply = 540;
            if (reply == 0) { }
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = AnchorMin, AnchorMax = AnchorMax },
                Image = { Color = BackgroundColor }
            }, "Hud", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.92", AnchorMax = $"1 1" },
                Button = { Color = "0.03 0.03 0.03 0.5" },
                Text = { Text = Title, Font = "robotocondensed-bold.ttf", FontSize = 21, Align = TextAnchor.MiddleCenter }
            }, Layer);

            if (bReturn)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.88 0.92", AnchorMax = "0.9479111 0.999" },
                    Button = { Color = ButtonsColor, Command = "help" },
                    Text = { Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 23, Align = TextAnchor.MiddleCenter }
                }, Layer);
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.95 0.92", AnchorMax = "0.999 0.998" },
                Button = { Color = "0.93 0.35 0.36 1.00", Close = Layer },
                Text = { Text = "X", Font = "robotocondensed-bold.ttf", FontSize = 23, Align = TextAnchor.MiddleCenter }
            }, Layer);

            if (section)
            {
                foreach (var message in menuElement[name].MessagesList)
                {
                    if (List.ContainsKey(message.Message))
                    {
                        Puts($"В разделе {name} найдены дублирующиеся сообщения, они убраны с показа");
                        continue;
                    }
                    List.Add(message.Message, message.Date);
                    if (List.Count == 0)
                    {
                        PrintWarning($"Внимание! У раздела [{menuElement[name].Type}] нету сообщений, раздел отключен!");
                        SendReply(player, "Извините но в данный момент нету сообщений по данному разделу.");
                        return;
                    }

                }
                container.Add(new CuiPanel()
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.999 0.9198" },
                    Image = { Color = menuElement[name].BackgroundColor, FadeIn = 0.5f },
                    FadeOut = 0.5f,
                }, Layer, $"ui.menu.{name}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.86", AnchorMax = "1 0.92" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = menuElement[name].NameUI, Font = "robotocondensed-bold.ttf", FontSize = 22, Align = TextAnchor.MiddleCenter },
                    FadeOut = 0.5f,
                }, $"ui.menu.{name}");

                foreach (var Messages in List)
                {
                    if (numbr > 0)
                    {
                        var List1 = List.Skip(numbr);
                        foreach (var nubm2 in List1)
                        {
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 0.83" },
                                Button = { Color = "0 0 0 0" },
                                Text = { Text = nubm2.Key, Font = "robotocondensed-bold.ttf", FontSize = menuElement[name].FrontSize, Align = TextAnchor.UpperLeft, FadeIn = 1f }
                            }, $"ui.menu.{name}");
                            if (name == "news")
                            {
                                container.Add(new CuiButton
                                {
                                    RectTransform = { AnchorMin = "0.38 0.03", AnchorMax = "0.62 0.13" },
                                    Button = { Color = ButtonsColor },
                                    Text = { Text = nubm2.Value, Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter }
                                }, Layer, $"ui.menu.{name}");
                            }
                            if (List1.Count() > 1)
                            {
                                container.Add(new CuiButton
                                {
                                    RectTransform = { AnchorMin = "0.7 0.03", AnchorMax = "0.89 0.13" },
                                    Button = { Color = ButtonsColor, Command = $"next.page {name} {numbr + 1}" },
                                    Text = { Text = "Дальше", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter }
                                }, Layer, $"ui.menu.{name}");
                                break;
                            }

                        }
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.11 0.03", AnchorMax = "0.30 0.13" },
                            Button = { Color = ButtonsColor, Command = $"next.page {name} {numbr - 1}" },
                            Text = { Text = "Назад", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter }
                        }, Layer, $"ui.menu.{name}");
                        CuiHelper.AddUi(player, container);
                        return;
                    }
                    else
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.1 0", AnchorMax = "0.9 0.83" },
                            Button = { Color = "0 0 0 0" },
                            Text = { Text = Messages.Key, Font = "robotocondensed-bold.ttf", FontSize = menuElement[name].FrontSize, Align = TextAnchor.UpperLeft, FadeIn = 1f }
                        }, $"ui.menu.{name}");

                        if (name == "news")
                        {
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0.38 0.03", AnchorMax = "0.62 0.13" },
                                Button = { Color = ButtonsColor },
                                Text = { Text = Messages.Value, Font = "robotocondensed-bold.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter }
                            }, Layer, $"ui.menu.{name}");
                        }
                        if (List.Count > 1)
                        {
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0.7 0.03", AnchorMax = "0.89 0.13" },
                                Button = { Color = ButtonsColor, Command = $"next.page {name} {+1}" },
                                Text = { Text = "Дальше", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter }
                            }, Layer, $"ui.menu.{name}");
                            break;
                        }
                    }

                }
                CuiHelper.AddUi(player, container);
                return;
            }

            int current = 1;

            foreach (var check in menuElement)
            {
                container.Add(new CuiButton()
                {
                    Button = { Command = $"MENUOPEN {menuElement[check.Key].Command}", Color = ButtonsColor, FadeIn = 0.5f },
                    RectTransform = {
                        AnchorMin = xmin + " " + ymin,
                        AnchorMax = (xmin + width) + " " + (ymin + height *1),
                        OffsetMax = "-1 -1",
                        OffsetMin = "5 5",
                    },
                    Text = { Text = $"{menuElement[check.Key].NameUI}", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 18 }
                },
                 Layer, $"ui.menu.{menuElement[check.Key].Type}");
                xmin += width + gap;

                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height + gap;
                }
                current++;

                if (current > 9)
                {
                    PrintWarning("Количество элементов превысило максимальную вместительность UI, отображено 9 позиций");
                    break;
                }
            }
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Commands
        [ConsoleCommand("menu")]
        void cmdConsoleHelp(ConsoleSystem.Arg args)
        {
            HelpGUI(args.Player());
        }

        [ConsoleCommand("next.page")]
        void cmdNewxt(ConsoleSystem.Arg args)
        {
            var name = args.Args[0];
            var amount = int.Parse(args.Args[1]);
            HelpGUI(args.Player(), true, name, true, amount);
        }

        [ChatCommand("menu")]
        void cmdChatHelp(BasePlayer player, string command, string[] args) => HelpGUI(player);

        [ChatCommand("news")]
        void cmdChatNews(BasePlayer player, string command, string[] args)
        {
            if (player == null || !player.IsAdmin) return;

            if (args.Length == 0 || args.Length < 1)
            {
                SendReply(player, "Используйте /news add Новость");
                return;
            }

            if (args.Length >= 1 && args[0] == "add")
            {
                string Msg = "";
                for (int i = 1; i < args.Length; i++)
                    Msg += " " + args[i];

                if (!menuElement.ContainsKey("news"))
                {
                    SendReply(player, "Раздела NEWS не существует, перед созданием сообщений создайте раздел, а после добавляйте новости");
                    return;
                }
                menuElement["news"].MessagesList.Add(new Messages
                {
                    Message = Msg,
                    Date = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")
                });

                Interface.Oxide.DataFileSystem.WriteObject("MenuHelp", menuElement);
                SendReply(player, $"Вы добавили новость, с текстом: {Msg}");
            }
        }

        [ConsoleCommand("MENUOPEN")]
        void cmdConsoleHelpOpen(ConsoleSystem.Arg args)
        {
            Puts("1");
            string name = args.FullString.Split('+')[0];
            var player = args.Player();
            if (name.ToLower().Contains("/"))
            {
                Puts("1");
                CuiHelper.DestroyUi(player, Layer);
                player.Command($"chat.say {name}");
            }
            else
                HelpGUI(args.Player(), true, name, true);

        }
        #endregion

        #region Data
        public Dictionary<string, MenuList> menuElement = new Dictionary<string, MenuList>();

        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("MenuHelp"))
                menuElement = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, MenuList>>("MenuHelp");
            else
            {
                menuElement.Add("news", new MenuList()
                {
                    Type = "news",
                    NameUI = "НОВОСТИ",
                    Command = "news",
                    FrontSize = 20,
                    BackgroundColor = "0.24 0.68 0.72 0.0",
                    MessagesList = new List<Messages>
                    {
                       new Messages
                       {
                           Message = "",
                           Date = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")
                        },
                       new Messages
                       {
                           Message = "",
                           Date = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")
                       }
                    }
                });
                menuElement.Add("rules", new MenuList()
                {
                    Type = "rules",
                    NameUI = "ПРАВИЛА",
                    Command = "rules",
                    FrontSize = 20,
                    BackgroundColor = "0.24 0.68 0.72 0.0",
                    MessagesList = new List<Messages>
                    {
                       new Messages
                       {
                           Message = "",
                           Date = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")
                       },
                       new Messages
                       {
                           Message = "",
                           Date = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")
                       }
                    }
                });
                menuElement.Add("help", new MenuList()
                {
                    Type = "help",
                    NameUI = "ПОМОЩЬ",
                    Command = "help",
                    FrontSize = 20,
                    BackgroundColor = "0.24 0.68 0.72 0.0",
                    MessagesList = new List<Messages>
                    {
                       new Messages
                       {
                           Message = "",
                           Date = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")
                       },
                       new Messages
                       {
                           Message = "",
                           Date = DateTime.UtcNow.ToString("dd.MM.yyyy HH:mm")
                       }
                    }
                });
                menuElement.Add("shop", new MenuList()
                {
                    Type = "shop",
                    NameUI = "МАГАЗИН\n/shop",
                    Command = "/shop",
                    FrontSize = 20,
                    BackgroundColor = "0.24 0.68 0.72 0.0",
                    MessagesList = new List<Messages> { }
                });
                menuElement.Add("case", new MenuList()
                {
                    Type = "case",
                    NameUI = "КЕЙСЫ\n/case",
                    Command = "/case",
                    FrontSize = 20,
                    BackgroundColor = "0.24 0.68 0.72 0.0",
                    MessagesList = new List<Messages> { }
                });
                Interface.Oxide.DataFileSystem.WriteObject("MenuHelp", menuElement);
            }
        }
        #endregion

        #region Core
        private void CheckData()
        {
            foreach (var button in menuElement)
            {
                if (string.IsNullOrEmpty(menuElement[button.Key].Command))
                {
                    PrintError($"Внимание! У кнопки {button.Key} не установлена команда!");
                    continue;
                }

                foreach (var Messages in menuElement[button.Key].MessagesList)
                {
                    if (string.IsNullOrEmpty(Messages.Message))
                    {
                        PrintError($"Внимание! В разделе {button.Key} не созданы сообщения!");
                    }
                }
            }
        }

        #endregion

        #region Oxide
        private void OnServerInitialized()
        {
            LoadDefaultConfig();
            LoadData();
            CheckData();
        }

        void OnPlayerInit(BasePlayer player)
        {
         if (EnabledLogin) HelpGUI(player);
        }


        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }
        #endregion
    }
}
            