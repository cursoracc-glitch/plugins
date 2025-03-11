using System;
using System.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Help", "Chibubrik", "2.0.0")]
    class Help : RustPlugin
    {
        #region Variables
        private string Layer = "UI_Help";
        #endregion

        #region Class
        public Dictionary<string, HelpList> help = new Dictionary<string, HelpList>();
        public class HelpList
        {
            [JsonProperty("Название кнопки")] public string NameUI;
            [JsonProperty("Команда")] public string Command;
            [JsonProperty("Цвет кнопки")] public string ButtonColor;
            [JsonProperty("Размер текста")] public int Sizes;
            [JsonProperty("Список")] public List<Messages> MessagesList = new List<Messages>();
        }

        public class Messages
        {
            [JsonProperty("Текст")] public string Message;
        }
        #endregion

        #region Command
        [ChatCommand("help")]
        void cmdHelp(BasePlayer player, string command, string[] args) => HelpUI(player);

        [ConsoleCommand("help")]
        void cmdConsoleHelp(ConsoleSystem.Arg args)
        {
            string name = args.FullString.Split('+')[0];
            var player = args.Player();
            if (name.ToLower().Contains("/"))
            {
                CuiHelper.DestroyUi(player, Layer);
                player.Command($"chat.say {name}");
            }
            else
            {
                HelpUI(args.Player(), true, name);
            }
        }

        [ConsoleCommand("next.page")]
        void cmdNewxt(ConsoleSystem.Arg args)
        {
            var name = args.Args[0];
            var amount = int.Parse(args.Args[1]);
            HelpUI(args.Player(), true, name, amount);
        }
        #endregion

        #region UI
        private void HelpUI(BasePlayer player, bool section = false, string name = "", int numbr = 0)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            Dictionary<string, string> List = new Dictionary<string, string>();
            float gap = -0.0f;
            float width = 0.1584f;
            float height = 0.2f;
            float startxBox = 0.341f;
            float startyBox = 0.522f - height;
            float xmin = startxBox;
            float ymin = startyBox;
            int current = 1;

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.8", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.5f },
                FadeOut = 0.4f
            }, "Overlay", Layer);

            if (!section)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.3 0.59", AnchorMax = $"0.7 0.67", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = "Вы новый игрок в Rust?", Font = "robotocondensed-bold.ttf", FontSize = 40, Align = TextAnchor.MiddleCenter, FadeIn = 0.5f }
                }, Layer);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.3 0.54", AnchorMax = $"0.7 0.585", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = "Мы понимаем, что нас посетят много новых игроков\nЕсли это относится и к вам, нажмите на одну из двух кнопок!", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 0.5f },
                }, Layer);
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            if (section)
            {
                foreach (var message in help[name].MessagesList)
                {
                    if (List.ContainsKey(message.Message))
                    {
                        Puts($"В разделе {name} найдены дублирующиеся сообщения, они убраны с показа");
                        continue;
                    }
                    List.Add(message.Message, "");
                    if (List.Count == 0)
                    {
                        PrintWarning($"У раздела [{help[name].NameUI}] нету сообщений, раздел отключен!");
                        return;
                    }

                }
                container.Add(new CuiPanel()
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Image = { Color = "0 0 0 0", FadeIn = 0.5f },
                    FadeOut = 0.4f,
                }, Layer, $"{name}");

                foreach (var Messages in List)
                {
                    if (numbr > 0)
                    {
                        var List1 = List.Skip(numbr);
                        foreach (var nubm2 in List1)
                        {
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                Button = { Color = "0 0 0 0", Close = Layer },
                                Text = { Text = nubm2.Key, Font = "robotocondensed-regular.ttf", FontSize = help[name].Sizes, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 0.5f }
                            }, $"{name}");
                            if (List1.Count() > 1)
                            {
                                container.Add(new CuiButton
                                {
                                    RectTransform = { AnchorMin = "0.94 0.46", AnchorMax = "0.99 0.54", OffsetMax = "0 0" },
                                    Button = { Color = "0 0 0 0", Command = $"next.page {name} {numbr + 1}", FadeIn = 0.5f },
                                    Text = { Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 45, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 0.5f }
                                }, Layer, $"{name}");
                                break;
                            }

                        }
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.01 0.46", AnchorMax = "0.06 0.54", OffsetMax = "0 0" },
                            Button = { Color = "0 0 0 0", Command = $"next.page {name} {numbr - 1}", FadeIn = 0.5f },
                            Text = { Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 45, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 0.5f }
                        }, Layer, $"{name}");
                        CuiHelper.AddUi(player, container);
                        return;
                    }
                    else
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                            Button = { Color = "0 0 0 0", Close = Layer },
                            Text = { Text = Messages.Key, Font = "robotocondensed-regular.ttf", FontSize = help[name].Sizes, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 0.5f }
                        }, $"{name}");

                        if (List.Count > 1)
                        {
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0.94 0.46", AnchorMax = "0.99 0.54", OffsetMax = "0 0" },
                                Button = { Color = "0 0 0 0", Command = $"next.page {name} {+1}", FadeIn = 0.5f },
                                Text = { Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 45, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 0.5f }
                            }, Layer, $"{name}");
                            break;
                        }
                    }
                }
                CuiHelper.AddUi(player, container);
                return;
            }

            foreach (var check in help)
            {
                container.Add(new CuiButton()
                {
                    Button = { Command = $"help {help[check.Key].Command}", Color = help[check.Key].ButtonColor, Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = 0.5f },
                    RectTransform = {
                        AnchorMin = xmin + " " + ymin,
                        AnchorMax = (xmin + width) + " " + (ymin + height *1),
                        OffsetMax = "-25 -1",
                        OffsetMin = "25 10",
                    },
                    Text = { Text = $"{help[check.Key].NameUI}", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1", FontSize = 55 }
                }, Layer, $"{help[check.Key].NameUI}");
                xmin += width + gap;

                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height + gap;
                }

                if (current > 2)
                {
                    break;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Oxide
        private void OnServerInitialized()
        {
            LoadData();
            CheckData();
            if (EnabledLogin)
            {
                BasePlayer.activePlayerList.ForEach(OnPlayerInit);
            }
        }

        private bool EnabledLogin = true;
        void OnPlayerInit(BasePlayer player)
        {
            if (EnabledLogin)
            {
                if (player.IsReceivingSnapshot)
                {
                    NextTick(() =>
                    {
                        OnPlayerInit(player);
                        return;
                    });
                }
                HelpUI(player);
            }
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }

        private void CheckData()
        {
            foreach (var check in help)
            {
                if (string.IsNullOrEmpty(help[check.Key].Command))
                {
                    PrintError($"У кнопки {check.Key} не установлена команда!");
                    continue;
                }

                foreach (var Messages in help[check.Key].MessagesList)
                {
                    if (string.IsNullOrEmpty(Messages.Message))
                    {
                        PrintError($"В разделе {check.Key} не созданы сообщения!");
                    }
                }
            }
        }
        #endregion

        #region Data
        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Help/Text"))
            {
                help = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, HelpList>>("Help/Text");
            }
            else
            {
                help.Add("yes", new HelpList()
                {
                    NameUI = "Да",
                    Command = "yes",
                    ButtonColor = "0.16 0.71 0.39 1",
                    Sizes = 18,
                    MessagesList = new List<Messages>
                    {
                       new Messages
                       {
                           Message = "<b><size=22>С чего начать?</size></b>\nПервым делом Вам понадобится орудие для добычи дерева и камня, и к примеру лук для защиты. Изучите меню <b>''TAB''</b> с\nинвентарем. По мере добычи ресурсов в меню крафта <b>''Q''</b> Вы увидите разблокированные предметы для создания.\nРазбивая бочки Вам будут выпадать компоненты (используются для крафта) и скрап, который также можно потратить на\nИзучение предметов. Чем больше у вас рецептов - тем больше возможностей.\n\n<b><size=22>Что здесь делать?</size></b>\nМир открыт перед Вами, что делать - решаете Вы. Добыча ресурсов, крафт и коммуникация с другими обитателями\nпомогут вам выжить и обзавестись крепкими стенами.\n\n<b><size=22>Как построить дом?</size></b>\nДля строительства Вам понадобится план постройки, киянка и первоначальные ресурсы (дерево и камень). Для первого\nобустройства жилища вы можете получить набор <b>''Обустройство дома''</b>.(/kit). Не забудьте про <b>шкаф</b>!\n\n<b><size=22>Где мне найти сожителя?</size></b>\nВыжить в одиночку очень тяжело. Обзавестись знакомыми можно на ближайшем морском пляже, если вас не зарубят\nкамнем при первом диалоге, то у вас есть шансы! (<b>''V'' - голосовой чат</b>)\nТакже напарника можно найти в обсуждении нашей группы: <b>ГРУППА</b>\n\n<b><size=22>Куда мне обратиться за помощью?</size></b>\nДля получения помощи Вы можете обратиться в группе в вк сервера или ввести команду: <b>/menu</b> в чат.\nГруппа: <b>группа</b>\n\n<b><size=22>Приятной игры на проекте проект</size></b>"
                       },
                       new Messages
                       {
                           Message = ""
                       }
                    }
                });
                help.Add("no", new HelpList()
                {
                    NameUI = "Нет",
                    Command = "no",
                    ButtonColor = "0.72 0.24 0.24 1",
                    Sizes = 18,
                    MessagesList = new List<Messages>
                    {
                       new Messages
                       {
                           Message = "<b><size=22>Добро пожаловать на Проект, друг.</size></b>\n\n<b><size=22>ОСНОВНЫЕ КОМАНДЫ</size></b>\n/kit - открыть меню доступных наборов.\n/report ''ник игрока'' - совершить донос на нечестного работягу.\n/skin - текстильная мастерская.\n\n<b><size=22>ТЕЛЕПОРТАЦИЯ</size></b>\n/tpr ''ник игрока'' - отправить запрос на телепортацию к игроку.\n/tpa(/tpc) - принять/отклонить запрос на телепортацию.\n/sethome(/removehome) ''название'' - создать/удалить точку спавна.\n/home list - открыть список своих жилищ.\n/home ''название'' - телепортация на хату с указанным названием.\n\n<b><size=22>КООПЕРАТИВ</size></b>\n/team add ''имя игрока''\n/team tag ''название''\n\n<b><size=22>Подробнее на САЙТ.RU</size></b>"
                       },
                       new Messages
                       {
                           Message = ""
                       }
                    }
                });
                Interface.Oxide.DataFileSystem.WriteObject("Help/Text", help);
            }
        }
        #endregion

        #region Helpers
        private static string HexToCuiColor(string hex)
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

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
        #endregion
    }
}