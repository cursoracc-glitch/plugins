using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("Clans Top", "King", "1.0.1")]
	public class ClansTop : RustPlugin
	{
        #region [Vars]
        [PluginReference] private Plugin Clans;
        private Dictionary<string, int> clanList = new Dictionary<string, int>();

        private const string Layer = "ClansTop.Layer";
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 0, 1))
                {
                    config.Settings.colorFirsClan = "#6692BBCC";
                    config.Settings.colorTwoClan = "#C08D5CCC";
                    config.Settings.colorThreeClan = "#C08D5CCC";
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class Settings
        {
            [JsonProperty("Раз в сколько секунд будет отправлятся сообщение ?")]
            public int chatSendTopTime;

            [JsonProperty("Отправлять в чат сообщения с топ 5 кланами ?")]
            public bool chatSendTop;

            [JsonProperty("Раз в сколько секунд обновлять панель топ трех кланов ?")]
            public int updatePanelTime;

            [JsonProperty("Использовать панель топ трех кланов ?")]
            public bool usePanelTop;

            [JsonProperty("Цвет первого места из панельки ( При использовании панели топ трех кланов )")]
            public string colorFirsClan;

            [JsonProperty("Цвет второго места из панельки ( При использовании панели топ трех кланов )")]
            public string colorTwoClan;

            [JsonProperty("Цвет третьего места из панельки ( При использовании панели топ трех кланов )")]
            public string colorThreeClan;
        }

        private class PluginConfig
        {
            [JsonProperty("Основные настройки плагина")]
            public Settings Settings;

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Settings = new Settings()
                    {
                        chatSendTopTime = 600,
                        chatSendTop = true,
                        updatePanelTime = 10,
                        usePanelTop = true,
                        colorFirsClan = "#6692BBCC",
                        colorTwoClan = "#C08D5CCC",
                        colorThreeClan = "#C08D5CCC",
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion

        #region [Oxide-Api]
		private void OnServerInitialized()
		{
            if (!Clans)
            {
                NextTick(() =>
                {
                    PrintWarning("Проверьте, установлен ли у вас плагин Clans");
                    Interface.Oxide.UnloadPlugin(Name);
                });
                return;
            }

            ChatPrintTop();
            
            if (config.Settings.chatSendTop)
                timer.Once(config.Settings.chatSendTopTime, ChatPrintTop);
            if (config.Settings.usePanelTop)
            {
                timer.Every(config.Settings.updatePanelTime, () =>
                {
                    UpdateTopDictionary();

                    foreach (var player in BasePlayer.activePlayerList)
                        GenerateTopUI(player);
                });

                foreach (var player in BasePlayer.activePlayerList)
                    OnPlayerConnected(player);
            }
		}

		private void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList) 
                CuiHelper.DestroyUi(player, Layer);
		}
        #endregion

        #region [Rust-Api]
		private void OnPlayerConnected(BasePlayer player)
		{
			UpdateTopDictionary();
            GenerateTopUI(player);
		}
        #endregion

        #region [Gui]
        private void GenerateTopUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "0 0", OffsetMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", Layer);

            foreach (var check in clanList.Select((i, t) => new { A = i, B = t }).Take(3))
            {
                string Color = check.B == 0 ? $"{HexToRustFormat(config.Settings.colorFirsClan)}" : check.B == 1 ? $"{HexToRustFormat(config.Settings.colorTwoClan)}" : check.B == 2 ? $"{HexToRustFormat(config.Settings.colorThreeClan)}" : "0 0 0 0";

                container.Add(new CuiPanel
                {
                    RectTransform = {   AnchorMin = "0 0", AnchorMax = "0 0",
                                        OffsetMin = $"{-199.5 + check.B * 128 - Math.Floor((float) check.B / 3) * 3 * 128} 0",
                                        OffsetMax = $"{-76 + check.B * 128 - Math.Floor((float) check.B / 3) * 3 * 128} 16.5", },
                    Image = { Color = "0 0 0 0", }
                }, Layer, Layer + ".Top" + $".{check.B}");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.145 1" },
                    Image = { Color = "1 1 1 0.025", Material = "assets/icons/greyout.mat" }
                }, Layer + ".Top" + $".{check.B}", Layer + ".Top" + $".{check.B}" + $".Pos{check.B}");

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Top" + $".{check.B}" + $".Pos{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.B + 1}", Color = "1 0.9294118 0.8666667 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    }
                });

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    RectTransform = { AnchorMin = "0.145 0", AnchorMax = "1 1" },
                    Image = { Color = Color, Material = "assets/icons/greyout.mat" }
                }, Layer + ".Top" + $".{check.B}", Layer + ".Top" + $".{check.B}" + $".Tag{check.B}");

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Top" + $".{check.B}" + $".Tag{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Key}", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 0.9294118 0.8666667 1", Align = TextAnchor.MiddleCenter },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    }
                });
            }

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region [SendToChat]
        private void ChatPrintTop()
        {
            UpdateTopDictionary();
            foreach (var player in BasePlayer.activePlayerList)
            {
                int i = 1;
                ServerBroadcast(player, "<color=#2394cb>ТОП КЛАНОВ:</color>", 0);
                foreach (var clan in clanList.Take(5))
                {
                    ServerBroadcast(player, $"<size=14>{i}.{clan.Key} - <color=#2394cb>{clan.Value}</color></size>", OwnerId(clan.Key));
                    i++;
                }
            }
        }
        #endregion

        #region [Func]
        string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8) throw new Exception(hex);

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        private void ServerBroadcast(BasePlayer player, string message, ulong AvatarID)
        {
            if (player == null || string.IsNullOrEmpty(message)) return;

            Player.Message(player, $"{message}", AvatarID);
        }

        private void UpdateTopDictionary()
        {
            if (clanList.Count > 0)
            {
                clanList.Clear();
                clanList = Clans?.Call<Dictionary<string, int>>("GetTops") ?? new Dictionary<string, int>();
            }
            else if (clanList.Count == 0)
            {
                clanList = Clans?.Call<Dictionary<string, int>>("GetTops") ?? new Dictionary<string, int>();
            }
        }

        private ulong OwnerId(string tag)
        {
            return (ulong)Clans?.Call("GetOwnerId", tag);
        }
        #endregion
    }
}