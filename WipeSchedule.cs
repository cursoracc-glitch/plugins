using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WipeSchedule", "Mevent", "1.0.4⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠")]
    public class WipeSchedule : RustPlugin
    {
        #region Fields⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        private string Layer = "UI_WipeSchedule";
        public enum Types
        {
            None,
            GLOBAL_WIPE,
            WIPE
        }

        private Dictionary<int, string> DaysOfWeek = new Dictionary<int, string>()
        {
            [1] = "Понедельник",
            [2] = "Вторник",
            [3] = "Среда",
            [4] = "Четверг",
            [5] = "Пятница",
            [6] = "Суббота",
            [7] = "Воскресенье",
        };

        public List<DayClass> DaysList = new List<DayClass>();

        public class DayClass
        {
            public int day;
            public string color;
            public Types types;
            public string description;
        }
        #endregion

        #region Config⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        private ConfigData config;

        private class ConfigData
        {
            [JsonProperty("Список чат-команд для открытия календаря")]
            public List<string> Commands;
            [JsonProperty("Раз во сколько секунд обновлять календарь?")]
            public int Delay;
            [JsonProperty("Цвет дней в нынешнем месяце")]
            public string ActiveColor;
            [JsonProperty("Цвет дней в предыдущем и следующем месяце")]
            public string DisactiveColor;
            [JsonProperty("Настройка")]
            public Dictionary<int, WipeClass> wipe;
        }

        private class WipeClass
        {
            [JsonProperty("Тип")]
            public Types type;
            [JsonProperty("Цвет кнопки")]
            public string color;
            [JsonProperty("Описание")]
            public string description;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                Commands = new List<string>
                {
                    "wipe", "schedule", "wipedate"
                },
                Delay = 7200,
                ActiveColor = "0.67 0.67 0.67 0.8",
                DisactiveColor = "0 0 0 0.6",
                wipe = new Dictionary<int, WipeClass>
                {
                    {
                        1, new WipeClass
                        {
                            color = "0.78 0.30 0.26 0.8",
                            type = Types.GLOBAL_WIPE,
                            description = "ГЛОБАЛЬНЫЙ ВАЙП"
                        }
                    },
                    {
                        9, new WipeClass
                        {
                            color = "0.45 0.64 0.45 0.8",
                            type = Types.WIPE,
                            description = "ВАЙП КАРТЫ"
                        }
                    },
                    {
                        16, new WipeClass
                        {
                            color = "0.78 0.30 0.26 0.8",
                            type = Types.GLOBAL_WIPE,
                            description = "ГЛОБАЛЬНЫЙ ВАЙП"
                        }
                    },
                    {
                        23, new WipeClass
                        {
                            color = "0.45 0.64 0.45 0.8",
                            type = Types.WIPE,
                            description = "ВАЙП КАРТЫ"
                        }
                    },
                    {
                        30, new WipeClass
                        {
                            color = "0.45 0.64 0.45 0.8",
                            type = Types.WIPE,
                            description = "ВАЙП КАРТЫ"
                        }
                    }
                }
            };
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            PrintError("Configuration file is corrupt(or not exists), creating new one!");
            config = GetDefaultConfig();
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Hooks⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        [PluginReference] Plugin XMenu;
        Timer TimerInitialize;
        private void OnServerInitialized()
        {

            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterMenu", this.Name, "WipeSchedule", "assets/icons/demolish_immediate.png", "RenderSchedule", null);

                    cmd.AddChatCommand("wipe", this, (p, cmd, args) => rust.RunClientCommand(p, "custommenu true WipeSchedule"));
                    TimerInitialize.Destroy();
                }
            });

            PrintWarning("  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            PrintWarning($"     {Name} v{Version} loading");
            PrintWarning($"        Plugin loaded - OK");
            PrintWarning("  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");

            for (int i = 0; i < config.Commands.Count; i++)
                cmd.AddChatCommand(config.Commands[i], this, nameof(CmdChatSchedule));

            cmd.AddConsoleCommand("UI_Schedule", this, nameof(CmdConsoleSchedule));

            CalculateTable();

            timer.Every(config.Delay, () => CalculateTable());
        }

        private void CmdChatSchedule(BasePlayer player, string command, string[] args)
        {
            
        }

        private void CmdConsoleSchedule(ConsoleSystem.Arg args)
        {
            var player = args.Player();

            int index = 0;
            if (!args.HasArgs(1) || !int.TryParse(args.Args[0], out index)) return;

            var check = DaysList[index];
            if (check.types != Types.None)
            {
                var container = new CuiElementContainer();
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = $"{check.description}", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, Layer + $".Day.Of.{index}", Layer + $".Day.Of.{index}.Text");
                CuiHelper.DestroyUi(player, Layer + $".Day.Of.{index}.Text");
                CuiHelper.AddUi(player, container);
            }
        }
        #endregion

        #region Interface⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";


        private void RenderSchedule(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];

            Container.Add(new CuiElement
            {
                Name = MenuContent,
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-430 -230",
                            OffsetMax = "490 270"
                        },
                    }
            });

            BuildUI(BasePlayer.FindByID(userID), Container);
        }

        private void BuildUI(BasePlayer player, CuiElementContainer container)
        {
            var monthName = FirstUpper(DateTime.Now.ToString("MMMM", CultureInfo.GetCultureInfo("ru-RU")));

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0 0 0 0.3" },
                Text = { Text = "" }
            }, MenuContent, Layer + ".BG");
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-450 -245", OffsetMax = "250 210" },
                Image = { Color = "0.3 0.3 0.3 0.5" },
                CursorEnabled = true
            }, Layer + ".BG", Layer);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "5 -25", OffsetMax = "200 25" },
                Text = { Text = $"<b>{monthName}</b>", FontSize = 32, Align = TextAnchor.MiddleCenter }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "5 -350", OffsetMax = "200 -25" },
                Text = { Text = $"ИНФОРМАЦИЯ О ВАПАХ TRASH RUST:\n\n<color=#73A473>Вайп без чертежей:</color> раз в 5 дней! Меняется карта и ничего не скидывается кроме статистики сервера - /stats, вайп блокировки предметов и оружия - /wipe\n\n<color=#C74D43>Глобальный вайп:</color> раз в 10 дней! Меняется карта, сбрасываются навыки вашего РПГ - /rpg, денежные бонусы - /bonus, все изученные вами чертежи вместе со статистикой в целом.\n\n*Период времени проведения вайпа по техническим причинам может меняться, но в целом отталкиваться от текущей даты, поэтому просим вас зарание ознакамливаться с информцией в группе нашего сервера <color=#FFAA00AA>vk.com/TRASHRUST</color>", FontSize = 12, Align = TextAnchor.MiddleCenter }
            }, Layer);

            #region Loop
            var xDaysSwitch = 2.5f;
            for (int i = 1; i <= 7; i++)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xDaysSwitch} 5", OffsetMax = $"{xDaysSwitch + 95} 30" },
                    Button = { Command = "", Color = "0.37 0.37 0.37 0.8" },
                    Text = { Text = DaysOfWeek[i], Align = TextAnchor.MiddleCenter, FontSize = 13, Color = "1 1 1 0.8", Font = "robotocondensed-regular.ttf" }
                }, Layer);
                xDaysSwitch += 100;
            }

            var ySwitch = -5;
            var xSwitch = 2.5f;
            for (int i = 0; i < DaysList.Count; i++)
            {
                var check = DaysList[i];

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xSwitch} {ySwitch - 70}", OffsetMax = $"{xSwitch + 95} {ySwitch}" },
                    Button = { Color = check.color, Command = $"UI_Schedule {i}", FadeIn = 1f },
                    Text = { Text = "" }
                }, Layer, Layer + $".Day.Of.{i}");
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = $"{check.day}", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 16 }
                }, Layer + $".Day.Of.{i}", Layer + $".Day.Of.{i}.Text");

                xSwitch += 100;

                if ((i + 1) % 7 == 0)
                {
                    xSwitch = 2.5f;
                    ySwitch -= 75;
                }
            }
            #endregion

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "712.5 5", OffsetMax = "737.5 30" },
                Image = { Color = "0.78 0.30 0.26 0.8" }
            }, Layer, Layer + ".GlobalWipe");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = "5 -20", OffsetMax = "175 20" },
                Text = { Text = "- Глобальный вайп с удалением чертежей", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".GlobalWipe");
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "712.5 40", OffsetMax = "737.5 65" },
                Image = { Color = "0.45 0.64 0.45 0.8", }
            }, Layer, Layer + ".Wipe");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = "5 -20", OffsetMax = "175 20" },
                Text = { Text = "- Вайп без удаления чертежей", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".Wipe");

            CuiHelper.DestroyUi(player, Layer);
            //CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Utils⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        private void CalculateTable()
        {
            DaysList.Clear();

            Calendar myCal = CultureInfo.InvariantCulture.Calendar;
            DateTime myDT = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1, myCal);

            var PreviousMonth = myDT.AddMonths(-1);
            var DaysInPreviousMonth = (int)DateTime.DaysInMonth(PreviousMonth.Year, PreviousMonth.Month);

            int j = Convert.ToInt32(myCal.GetDayOfWeek(myDT)) - 1;

            j = j == -1 ? 6 : j;

            var LastDay = new DateTime(PreviousMonth.Year, PreviousMonth.Month, DaysInPreviousMonth);
            var backDays = LastDay.AddDays(-j + 1);
            for (int m = 0; m < j; m++)
            {
                DaysList.Add(new DayClass
                {
                    day = backDays.Day,
                    color = config.DisactiveColor,
                    description = string.Empty,
                    types = Types.None
                });
                backDays = backDays.AddDays(1);
            }

            int month = myCal.GetMonth(myDT);
            while (myCal.GetMonth(myDT) == month)
            {
                var check = config.wipe.Where(x => x.Key == myDT.Day).FirstOrDefault().Value != null;

                DaysList.Add(new DayClass
                {
                    day = myDT.Day,
                    color = check ? config.wipe[myDT.Day].color : config.ActiveColor,
                    description = check ? config.wipe[myDT.Day].description : string.Empty,
                    types = check ? config.wipe[myDT.Day].type : Types.None
                });

                myDT = myDT.AddDays(1);
                j--;
            }

            if (DaysList.Count < 42)
            {
                var DaysToEndTable = 42 - DaysList.Count;

                for (int i = 1; i <= DaysToEndTable; i++)
                {
                    DaysList.Add(new DayClass
                    {
                        day = i,
                        color = config.DisactiveColor,
                        description = string.Empty,
                        types = Types.None
                    });
                }
            }
        }

        public string FirstUpper(string str)
        {
            str = str.ToLower();
            string[] s = str.Split(' ');
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].Length > 1)
                    s[i] = s[i].Substring(0, 1).ToUpper() + s[i].Substring(1, s[i].Length - 1);
                else s[i] = s[i].ToUpper();
            }
            return string.Join(" ", s);
        }
        #endregion
    }
}