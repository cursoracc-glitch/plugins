using System.Collections.Generic;
using System.Globalization;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Crosshair", "MisterPixie", "3.1.0")]
    [Description("Allows the user to toggle a crosshair")]

    class Crosshair : RustPlugin
    {
        private string mainUI = "UI_MAIN";
        private CuiElementContainer _ui;
        private HashSet<string> _crosshairSettings = new HashSet<string>();
        private const string _usePerm = "crosshair.use";

        #region Classes
        private class UI
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool cursor = false)
            {
                var newElement = new CuiElementContainer
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = cursor
                        },
                        new CuiElement().Parent = "Hud",
                        panelName
                    }
                };

                return newElement;
            }

            public static void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 1f)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, FadeIn = fadein, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                }, panel, CuiHelper.GetGuid());
            }
        }
        #endregion

        #region Methods
        private void ToggleCrosshair(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, _usePerm))
            {
                PrintToChat(player, Lang("No Permission", player.UserIDString));
                return;
            }

            if (_crosshairSettings.Contains(player.UserIDString))
            {
                DestroyCrosshair(player);
                _crosshairSettings.Remove(player.UserIDString);
                player.ChatMessage(Lang("CrosshairOff", player.UserIDString));
                return;
            }

            CuiHelper.AddUi(player, _ui);
            _crosshairSettings.Add(player.UserIDString);
            player.ChatMessage(Lang("CrosshairOn", player.UserIDString));

        }

        private void DestroyCrosshair(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, mainUI);
        }

        private void DestroyAllCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null) return;

            DestroyCrosshair(player);
        }

        private void CreateUI()
        {
            var container = UI.CreateElementContainer(mainUI, $"{HexToColor("000000")} 0", "0.469 0.467", "0.53 0.53");
            UI.CreateLabel(ref container, mainUI, $"{HexToColor(configData.CrosshairColor)} 0.9", configData.CrosshairText, 16, "0 0", "1 1");
            _ui = container;
        }
        private static string HexToColor(string hexColor)
        {
            if (hexColor.IndexOf('#') != -1) hexColor = hexColor.Replace("#", "");

            var red = 0;
            var green = 0;
            var blue = 0;

            if (hexColor.Length == 6)
            {
                red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            }
            else if (hexColor.Length == 3)
            {
                red = int.Parse(hexColor[0] + hexColor[0].ToString(), NumberStyles.AllowHexSpecifier);
                green = int.Parse(hexColor[1] + hexColor[1].ToString(), NumberStyles.AllowHexSpecifier);
                blue = int.Parse(hexColor[2] + hexColor[2].ToString(), NumberStyles.AllowHexSpecifier);
            }

            return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255}";
        }
        #endregion

        #region Hooks
        private void Init()
        {
            LoadVariables();
            permission.RegisterPermission("crosshair.use", this);
            cmd.AddChatCommand("crosshair", this, "ToggleCrosshair");
            cmd.AddConsoleCommand("ui_destroy", this, "DestroyAllCommand");
            CreateUI();
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyCrosshair(player);
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            DestroyCrosshair(player);
            _crosshairSettings.Remove(player.UserIDString);
        }
        #endregion

        #region lang
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"CrosshairOn", "Crosshair turned on."},
                {"CrosshairOff", "Crosshair turned off."},
                {"No Permission", "Error, you lack permission."}
            }, this);
        }
        #endregion

        #region Config
        private ConfigData configData;
        private class ConfigData
        {
            public string CrosshairColor;
            public string CrosshairText;
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
                CrosshairColor = "#FF0000",
                CrosshairText = "·"
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}