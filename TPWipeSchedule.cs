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
    [Info("TPWipeSchedule", "Sempai#3239", "3.0.0⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠")]
    public class TPWipeSchedule : RustPlugin
    {
        #region Fields⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        private string Layer = "UI_WipeSchedule";

        [PluginReference] Plugin ImageLibrary;
        public enum Types
        {
            None,
            GLOBAL_WIPE,
            WIPE
        }

        private Dictionary<int, string> DaysOfWeek = new Dictionary<int, string>()
        {
            [1] = "ПН",
            [2] = "ВТ",
            [3] = "СР",
            [4] = "ЧТ",
            [5] = "ПТ",
            [6] = "СБ",
            [7] = "ВС",
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
            [JsonProperty("Раз во сколько секунд обновлять календарь?")]
            public int Delay;
            [JsonProperty("Цвет дней в нынешнем месяце (изображение)")]
            public string ActiveImage;
            [JsonProperty("Настройка")]
            public Dictionary<int, WipeClass> wipe;
        }

        private class WipeClass
        {
            [JsonProperty("Тип")]
            public Types type;
            [JsonProperty("Изображение с цветом кнопки")]
            public string image;
            [JsonProperty("Описание")]
            public string description;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                Delay = 7200,
                ActiveImage = "https://rustage.su/img/server/ui/calendar_day.png",
                wipe = new Dictionary<int, WipeClass>
                {
                    {
                        1, new WipeClass
                        {
                            image = "https://rustage.su/img/server/ui/calendar_wipe_global.png",
                            type = Types.GLOBAL_WIPE,
                            description = "ГЛОБАЛЬНЫЙ ВАЙП"
                        }
                    },
                    {
                        9, new WipeClass
                        {
                            image = "https://rustage.su/img/server/ui/calendar_wipe_map.png",
                            type = Types.WIPE,
                            description = "ВАЙП КАРТЫ"
                        }
                    },
                    {
                        16, new WipeClass
                        {
                            image = "https://rustage.su/img/server/ui/calendar_wipe_global.png",
                            type = Types.GLOBAL_WIPE,
                            description = "ГЛОБАЛЬНЫЙ ВАЙП"
                        }
                    },
                    {
                        23, new WipeClass
                        {
                            image = "https://rustage.su/img/server/ui/calendar_wipe_map.png",
                            type = Types.WIPE,
                            description = "ВАЙП КАРТЫ"
                        }
                    },
                    {
                        30, new WipeClass
                        {
                            image = "https://rustage.su/img/server/ui/calendar_wipe_map.png",
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
        private void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------\n" +
            "     Author - https://topplugin.ru/\n" +
            "     VK - https://vk.com/rustnastroika\n" +
            "     Discord - https://discord.com/invite/5DPTsRmd3G\n" +
            "-----------------------------");
            PrintWarning("  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            PrintWarning($"     {Name} v{Version} loading");
            PrintWarning($"        Plugin loaded - OK");
            PrintWarning("  ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            ImageLibrary.Call("AddImage", $"https://rustage.su/img/server/ui/calendar_back.png", "ahDNpde");
            ImageLibrary.Call("AddImage", config.ActiveImage, config.ActiveImage);

            foreach (var check in config.wipe)
                ImageLibrary.Call("AddImage", check.Value.image, check.Value.image);
            
            cmd.AddConsoleCommand("UI_Schedule", this, nameof(CmdConsoleSchedule));

            CalculateTable();

            timer.Every(config.Delay, () => CalculateTable());
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
                    Text = { Text = $"{check.description}", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 8, Font = "robotocondensed-regular.ttf" }
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

        void BuildUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var monthName = FirstUpper(DateTime.Now.ToString("MMMM", CultureInfo.GetCultureInfo("ru-RU")));

            container.Add(new CuiElement
            {
                Name = "lay" + ".Main",
                Parent = ".Mains",
                Components = 
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "ahDNpde"), Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "-0.315 -0.27", AnchorMax = "1.3 1.275", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, "lay" + ".Main", Layer + ".BG");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8 0.804", AnchorMax = "0.817 0.832" },
                Button = { Close = "Menu_UI", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "lay" + ".Main");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-143.3 -245", OffsetMax = "200 210" },
                Image = { Color = "0 0 0 0" },
                CursorEnabled = true
            }, Layer + ".BG", Layer);

            #region Loop
            var xDaysSwitch = 17.0f;
            for (int i = 1; i <= 7; i++)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xDaysSwitch} -130", OffsetMax = $"{xDaysSwitch + 20} -90" },
                    Text = { Text = DaysOfWeek[i], Align = TextAnchor.MiddleCenter, FontSize = 11, Color = "1 1 1 0.6", Font = "robotocondensed-regular.ttf" }
                }, Layer);
                xDaysSwitch += 39.0f;
            }

            var ySwitch = -16;
            var xSwitch = 15.5f;
            for (int i = 0; i < DaysList.Count; i++)
            {
                var check = DaysList[i];

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"{xSwitch} {ySwitch - 157}", OffsetMax = $"{xSwitch + 35} {ySwitch - 120}" },
                    Button = { Color = "0 0 0 0", Command = $"UI_Schedule {i}", FadeIn = 1f },
                    Text = { Text = "" }
                }, Layer, Layer + $".Day.Of.{i}");

                if (check.color != "0 0 0 0")
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer + $".Day.Of.{i}",
                        Components = 
                        {
                            new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", check.color) },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        }
                    });
                }

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = $"{check.day}", Color = "1 1 1 0.6", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                }, Layer + $".Day.Of.{i}", Layer + $".Day.Of.{i}.Text");

                xSwitch += 37.7f;

                if ((i + 1) % 7 == 0)
                {
                    xSwitch = 15.0f;
                    ySwitch -= 40;
                }
            }
            #endregion

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "320.5 290", OffsetMax = "333.5 318" },
                Image = { Color = "0 0 0 0" }
            }, Layer, Layer + ".GlobalWipe");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = "5 -20", OffsetMax = "175 20" },
                Text = { Text = "- Глобал вайп с чертежами", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".GlobalWipe");
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "320.5 315", OffsetMax = "333.5 347" },
                Image = { Color = "0 0 0 0", }
            }, Layer, Layer + ".Wipe");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = "5 -20", OffsetMax = "175 20" },
                Text = { Text = "- Вайп без удаления чертежей", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".Wipe");

            CuiHelper.AddUi(player, container);
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
                    color = "0 0 0 0",
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
                    color = check ? config.wipe[myDT.Day].image : config.ActiveImage,
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
                        color = "0 0 0 0",
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