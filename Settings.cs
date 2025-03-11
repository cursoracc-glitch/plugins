using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Settings", "VooDoo", "1.0.0")]
    [Description("Settings switch's")]
    public class Settings : RustPlugin
    {
        [PluginReference] Plugin XMenu;

        #region Config
        public Dictionary<ulong, Dictionary<int, UserSettings>> userSettings = new Dictionary<ulong, Dictionary<int, UserSettings>>();
        public class UserSettings
        {
            public bool isOn;
            public Dictionary<int, UserSettings> settings;
        }

        private PluginConfig config;
        private class PluginConfig
        {
            public ColorConfig colorConfig;
            public class ColorConfig
            {
                public string menuContentHighlighting;
                public string menuContentHighlightingalternative;

                public string gradientColor;
            }

            public Dictionary<int, PluginSettings> pluginSettings;
            public class PluginSettings
            {
                public string commandDescription;
                public string command;
                public bool isChecked;
                public bool onlyOne;

                public Dictionary<int, PluginSettings> subSettings;
            }
        }

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                colorConfig = new PluginConfig.ColorConfig()
                {
                    menuContentHighlighting = "#0000007f",
                    menuContentHighlightingalternative = "#FFFFFF10",

                    gradientColor = "#00000099",
                },

                pluginSettings = new Dictionary<int, PluginConfig.PluginSettings>()
                {
                    [0] = new PluginConfig.PluginSettings
                    {
                        commandDescription = "Описание пункта в меню настроек",
                        command = "chat.say Switch",
                        isChecked = false,
                        onlyOne = true,
                        subSettings = new Dictionary<int, PluginConfig.PluginSettings>()
                        {
                            [0] = new PluginConfig.PluginSettings
                            {
                                commandDescription = "Описание подпункта #1",
                                command = "chat.say SubSwitch1",
                                isChecked = true,
                                subSettings = null,
                                onlyOne = false
                            },
                            [1] = new PluginConfig.PluginSettings
                            {
                                commandDescription = "Описание подпункта #2",
                                command = "chat.say SubSwitch2",
                                isChecked = true,
                                subSettings = null,
                                onlyOne = false
                            },
                            [2] = new PluginConfig.PluginSettings
                            {
                                commandDescription = "Описание подпункта #3",
                                command = "chat.say SubSwitch3",
                                isChecked = true,
                                subSettings = null,
                                onlyOne = false
                            }
                        }
                     },
                    [1] = new PluginConfig.PluginSettings
                    {
                        commandDescription = "Описание пункта в меню настроек",
                        command = "chat.say Switch",
                        isChecked = false,
                        onlyOne = false,
                        subSettings = new Dictionary<int, PluginConfig.PluginSettings>()
                        {
                            [0] = new PluginConfig.PluginSettings
                            {
                                commandDescription = "Описание подпункта #1",
                                command = "chat.say SubSwitch1",
                                isChecked = true,
                                subSettings = null,
                                onlyOne = false
                            },
                            [1] = new PluginConfig.PluginSettings
                            {
                                commandDescription = "Описание подпункта #2",
                                command = "chat.say SubSwitch2",
                                isChecked = true,
                                subSettings = null,
                                onlyOne = false
                            },
                            [2] = new PluginConfig.PluginSettings
                            {
                                commandDescription = "Описание подпункта #3",
                                command = "chat.say SubSwitch3",
                                isChecked = true,
                                subSettings = null,
                                onlyOne = false
                            }
                        }
                    },
                    [2] = new PluginConfig.PluginSettings
                    {
                        commandDescription = "Описание пункта в меню настроек",
                        command = "chat.say Switch",
                        isChecked = false,
                        onlyOne = true,
                        subSettings = new Dictionary<int, PluginConfig.PluginSettings>()
                        {
                            [0] = new PluginConfig.PluginSettings
                            {
                                commandDescription = "Описание подпункта #1",
                                command = "chat.say SubSwitch1",
                                isChecked = true,
                                subSettings = null,
                                onlyOne = false
                            },
                            [1] = new PluginConfig.PluginSettings
                            {
                                commandDescription = "Описание подпункта #2",
                                command = "chat.say SubSwitch2",
                                isChecked = true,
                                subSettings = null,
                                onlyOne = false
                            },
                            [2] = new PluginConfig.PluginSettings
                            {
                                commandDescription = "Описание подпункта #3",
                                command = "chat.say SubSwitch3",
                                isChecked = true,
                                subSettings = null,
                                onlyOne = false
                            }
                        }
                    }
                }
            };
        }
        #endregion

        #region Layers
        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";
        #endregion


        Timer TimerInitialize;
        private void OnServerInitialized()
        {
            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterSubMenu", this.Name, "Main", "Настройки", "RenderSettings", null);

                    int SettingsID = (int)XMenu.Call("API_GetSubMenuID", "Main", "Настройки");
                    cmd.AddChatCommand("settings", this, (p, cmd, args) => rust.RunClientCommand(p, $"custommenu true Main {SettingsID}"));

                    TimerInitialize.Destroy();
                }
            });

            foreach (var p in BasePlayer.activePlayerList) { OnPlayerConnected(p); }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null)
                return;

            if (player.IsReceivingSnapshot)
            {
                timer.In(1f, () => OnPlayerConnected(player));
                return;
            }

            if (!userSettings.ContainsKey(player.userID))
            {
                userSettings.Add(player.userID, new Dictionary<int, UserSettings>());

                foreach(var settings in config.pluginSettings)
                {
                    userSettings[player.userID].Add(settings.Key, new UserSettings()
                    {
                            isOn = settings.Value.isChecked,
                            settings = new Dictionary<int, UserSettings>()
                    });

                    foreach(var subSettings in settings.Value.subSettings)
                    {
                        userSettings[player.userID][settings.Key].settings.Add(subSettings.Key, new UserSettings()
                        {
                            isOn = subSettings.Value.isChecked,
                            settings = null
                        });
                    }
                }
            }
            else
            {
                foreach (var settings in config.pluginSettings)
                {
                    if (!userSettings[player.userID].ContainsKey(settings.Key))
                    {
                        userSettings[player.userID].Add(settings.Key, new UserSettings()
                        {
                            isOn = settings.Value.isChecked,
                            settings = new Dictionary<int, UserSettings>()
                        });
                    }

                    foreach (var subSettings in settings.Value.subSettings)
                    {
                        if (!userSettings[player.userID][settings.Key].settings.ContainsKey(subSettings.Key))
                        {
                            userSettings[player.userID][settings.Key].settings.Add(subSettings.Key, new UserSettings()
                            {
                                isOn = subSettings.Value.isChecked,
                                settings = null
                            });
                        }
                    }
                }
            }
        }

        private void RenderSettings(ulong userID, object[] objects)
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
                            OffsetMin = "-215 -230",
                            OffsetMax = "500 270"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info",
                Parent = MenuContent,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat(config.colorConfig.menuContentHighlighting),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "80 -460",
                            OffsetMax = "630 -10"
                        }
                    }
            });

            for(int i = 0, x = 0; i < config.pluginSettings.Count; i++)
            {
                string text = $"<color=#FFFFFF66>☐  {config.pluginSettings.ElementAt(i).Value.commandDescription}</color>";
                if(userSettings[userID][config.pluginSettings.ElementAt(i).Key].isOn)
                    text = $"<color=#FFFFFF>☑  {config.pluginSettings.ElementAt(i).Value.commandDescription}</color>";

                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".Info" + $".{i}.-1",
                    Parent = MenuContent + ".Info",
                    Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = text,
                                    Align = TextAnchor.MiddleLeft,
                                    FontSize = 16,
                                    Font = "robotocondensed-regular.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"30 {-50 - x * 25}",
                                    OffsetMax = $"550 {-25 - x * 25}"
                                }
                            }
                });

                Container.Add(new CuiButton
                {
                    Button = { Color = "1 1 1 0", Command = $"settings.sendcommand {x} {i} -1 {config.pluginSettings.ElementAt(i).Value.command.Replace("%STEAMID%", userID.ToString())}" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, MenuContent + ".Info" + $".{i}.-1", MenuContent + ".Info" + $".{i}.Btn");
                x++;

                for(int j = 0; j < config.pluginSettings.ElementAt(i).Value.subSettings.Count; j++)
                {
                    string subText = $"<color=#FFFFFF66>☐  {config.pluginSettings.ElementAt(i).Value.subSettings.ElementAt(j).Value.commandDescription}</color>";
                    if (userSettings[userID][config.pluginSettings.ElementAt(i).Key].settings.ElementAt(j).Value.isOn)
                        subText = $"<color=#FFFFFF>☑  {config.pluginSettings.ElementAt(i).Value.subSettings.ElementAt(j).Value.commandDescription}</color>";

                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".Info" + $".{i}.{j}",
                        Parent = MenuContent + ".Info",
                        Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = subText,
                                    Align = TextAnchor.MiddleLeft,
                                    FontSize = 16,
                                    Font = "robotocondensed-regular.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"60 {-50 - x * 25}",
                                    OffsetMax = $"550 {-25 - x * 25}"
                                }
                            }
                    });
                    Container.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = $"settings.sendcommand {x} {i} {j} {config.pluginSettings.ElementAt(i).Value.subSettings.ElementAt(j).Value.command.Replace("%STEAMID%", userID.ToString())}" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", },
                        Text = { Text = "", Align = TextAnchor.MiddleCenter }
                    }, MenuContent + ".Info" + $".{i}.{j}", MenuContent + ".Info" + $".{i}.{j}.Btn");
                    x++;
                }
            }
        }

        [ConsoleCommand("settings.sendcommand")]
        private void SendCommand(ConsoleSystem.Arg arg)
        {
            if (arg.HasArgs(4))
            {
                int x = int.Parse(arg.Args[0]);
                int i = int.Parse(arg.Args[1]);
                int j = int.Parse(arg.Args[2]);

                string command = string.Join(" ", arg.Args.Skip(3));

                rust.RunClientCommand(arg.Player(), command.Replace("%STEAMID%",arg.Player().UserIDString));

                if (j != -1 && config.pluginSettings[i].onlyOne)
                {
                    x = 0;
                    for(int o = 0; o < config.pluginSettings.Count; o++)
                    {
                        x++;
                        if (o >= i)
                            break;

                        for(int p = 0; p < config.pluginSettings.ElementAt(o).Value.subSettings.Count; p++)
                        {
                            x++;
                        }
                    }
                    for(int k = 0; k < config.pluginSettings[i].subSettings.Count; k++)
                    {
                        CuiHelper.DestroyUi(arg.Player(), MenuContent + ".Info" + $".{i}.{k}");
                        if (k != j)
                            userSettings[arg.Connection.userid][i].settings[k].isOn = false;
                        else
                            userSettings[arg.Connection.userid][i].settings[k].isOn = true;

                        CuiElementContainer Container = new CuiElementContainer();
                        bool isOn =userSettings[arg.Connection.userid][i].settings[k].isOn;
                        string subText = $"{(isOn ? "<color=#FFFFFF>☑" : "<color=#FFFFFF66>☐")}  {config.pluginSettings.ElementAt(i).Value.subSettings.ElementAt(k).Value.commandDescription}</color>";
                        Container.Add(new CuiElement
                        {
                            Name = MenuContent + ".Info" + $".{i}.{k}",
                            Parent = MenuContent + ".Info",
                            Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = subText,
                                    Align = TextAnchor.MiddleLeft,
                                    FontSize = 16,
                                    Font = "robotocondensed-regular.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"{60} {-50 - x * 25}",
                                    OffsetMax = $"550 {-25 - x * 25}"
                                }
                            }
                        });


                        Container.Add(new CuiButton
                        {
                            Button = { Color = "1 1 1 0", Command = $"settings.sendcommand {x} {i} {k} {config.pluginSettings.ElementAt(i).Value.subSettings.ElementAt(k).Value.command}" },
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", },
                            Text = { Text = "", Align = TextAnchor.MiddleCenter }
                        }, MenuContent + ".Info" + $".{i}.{k}", MenuContent + ".Info" + $".{i}.{k}.Btn");
                        CuiHelper.AddUi(arg.Player(), Container);
                        x++;
                    }
                    return;
                }
                else
                {
                    CuiHelper.DestroyUi(arg.Player(), MenuContent + ".Info" + $".{i}.{j}");

                    if (j != -1)
                    {
                        userSettings[arg.Connection.userid][i].settings[j].isOn = !userSettings[arg.Connection.userid][i].settings[j].isOn;
                    }
                    else
                    {
                        userSettings[arg.Connection.userid][i].isOn = !userSettings[arg.Connection.userid][i].isOn;
                    }

                    bool isOn = j != -1 ? userSettings[arg.Connection.userid][i].settings[j].isOn : userSettings[arg.Connection.userid][i].isOn;
                    CuiElementContainer Container = new CuiElementContainer();
                    string subText = $"{(isOn ? "<color=#FFFFFF>☑" : "<color=#FFFFFF66>☐")}  {(j != -1 ? config.pluginSettings.ElementAt(i).Value.subSettings.ElementAt(j).Value.commandDescription : config.pluginSettings.ElementAt(i).Value.commandDescription)}</color>";
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".Info" + $".{i}.{j}",
                        Parent = MenuContent + ".Info",
                        Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = subText,
                                    Align = TextAnchor.MiddleLeft,
                                    FontSize = 16,
                                    Font = "robotocondensed-regular.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"{(j != -1 ? "60" : "30")} {-50 - x * 25}",
                                    OffsetMax = $"550 {-25 - x * 25}"
                                }
                            }
                    });


                    Container.Add(new CuiButton
                    {
                        Button = { Color = "1 1 1 0", Command = $"settings.sendcommand {x} {i} {j} {(j != -1 ? config.pluginSettings.ElementAt(i).Value.subSettings.ElementAt(j).Value.command.Replace("%STEAMID%", arg.Player().userID.ToString()) : config.pluginSettings.ElementAt(i).Value.command.Replace("%STEAMID%", arg.Player().userID.ToString()))}" },
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", },
                        Text = { Text = "", Align = TextAnchor.MiddleCenter }
                    }, MenuContent + ".Info" + $".{i}.{j}", MenuContent + ".Info" + $".{i}.{j}.Btn");
                    CuiHelper.AddUi(arg.Player(), Container);
                }
            }
        }

        #region Utils
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

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
        #endregion
    }
}
