using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("OnlinePanel", "OxideBro", "0.1.1")]
    public class OnlinePanel : RustPlugin
    {
        public class PlayerTime
        {
            public TimeSpan time = new TimeSpan();
            public Coroutine coroutine;
        }

        public Dictionary<BasePlayer, PlayerTime> PlayersTime = new Dictionary<BasePlayer, PlayerTime>();

        private PluginConfig config;

        private Coroutine UpdateActionValues;

        public bool CargoPlane;
        public bool BradleyAPC;
        public bool BaseHelicopter;
        public bool CargoShip;
        public bool CH47Helicopter;
        public bool Init;

        private IEnumerator UpdateValues()
        {
            while (Init)
            {
                CargoPlane = false;
                BradleyAPC = false;
                BaseHelicopter = false;
                CargoShip = false;
                CH47Helicopter = false;
                foreach (var entity in BaseNetworkable.serverEntities.Where(p => p is CargoPlane || p is BradleyAPC
                || p is BaseHelicopter || p is BaseHelicopter || p is CargoShip || p is CH47Helicopter))
                {
                    if (entity is CargoPlane)
                        CargoPlane = true;
                    if (entity is BradleyAPC)
                        BradleyAPC = true;
                    if (entity is BaseHelicopter)
                        BaseHelicopter = true;
                    if (entity is CargoShip)
                        CargoShip = true;
                    if (entity is CH47Helicopter)
                        CH47Helicopter = true;
                }
                yield return new WaitForSeconds(10);
            }
            yield return 0;
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за скачивание плагина на сайте RustPlugin.ru. <3 OxideBro!");
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
            if (config.PluginVersion < new VersionNumber(1, 1, 0))
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.In(1f, () => OnPlayerConnected(player));
                return;
            }
            if (!PlayersTime.ContainsKey(player)) PlayersTime.Add(player, new PlayerTime());
            if (!string.IsNullOrEmpty(config.IpGeoAPIKey))
            {
                var url = $"https://api.ipgeolocation.io/timezone?apiKey={config.IpGeoAPIKey}d&ip={player.Connection.ipaddress}";
                webrequest.Enqueue(url, string.Empty, (code, response) =>
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject<Parser>(response);
                        if (result != null)
                        {
                            var newtime = DateTime.Parse(result.date_time);
                            if (newtime != null)
                                PlayersTime[player].time = DateTime.UtcNow.ToLocalTime() - newtime;
                        }
                    }
                    catch
                    {
                    }

                }, this);
            }

            PlayersTime[player].coroutine = ServerMgr.Instance.StartCoroutine(StartUpdate(player));
        }

        void Unload()
        {
            Init = false;
            if (UpdateActionValues != null)
                ServerMgr.Instance.StopCoroutine(UpdateValues());
            foreach (var player in PlayersTime)
            {
                ServerMgr.Instance.StopCoroutine(player.Value.coroutine);
                CuiHelper.DestroyUi(player.Key, "OnlinePanel_Main");
            }
        }

        private IEnumerator StartUpdate(BasePlayer player)
        {
            while (player.IsConnected)
            {
                CreateMenu(player);
                yield return new WaitForSeconds(config.UpdateTime);
            }

            PlayersTime.Remove(player);
            yield return 0;
        }

        class PluginConfig
        {
            [JsonProperty("Версия конфигурации")]
            public VersionNumber PluginVersion = new VersionNumber();

            [JsonProperty("Размер текста")]
            public int TextSize = 12;

            [JsonProperty("Шаблон верх ({0} Игровое время | {1} ВРЕМЯ ИГРОКА | {2} ОНЛАЙН | {3} ПОДКЛЮЧЕНИЯ | {4} СЛИПЕРЫ)")]
            public string TextHeader = "ИГРОВОЕ ВРЕМЯ <color=#ffffff>{0}</color> | ВРЕМЯ <color=#ffffff>{1}</color> | ОНЛАЙН <color=#ffffff>{2}</color>| СЛИПЕРЫ <color=#ffffff>{4}</color>";

            [JsonProperty("Шаблон низ")]
            public string TextFooter = "- {tank} - {ship} - {plane} - {heli} - {chinook} -";

            [JsonProperty("Цвет подсветки, когда есть танк, корабль и тд")]
            public string ActiveColor = "#ffffff";

            [JsonProperty("Цвет неосновного текста с прозрачностью")]
            public string PassiveColor = "1 1 1 0.5";

            [JsonProperty("Название танка")]
            public string PAnzerName = "танк";

            [JsonProperty("Название корбля")]
            public string ShipName = "корабль";

            [JsonProperty("Название самолета")]
            public string AirName = "самолет";

            [JsonProperty("Название вертолета")]
            public string HeliName = "вертолет";

            [JsonProperty("Название чинука")]
            public string ChinookName = "чинук";

            [JsonProperty("Расоложение верхнего шаблона - MAX")]
            public string HeaderMax = "610 710";

            [JsonProperty("Расоложение верхнего шаблона - MIN")]
            public string HeaderMin = "300 695";

            [JsonProperty("Расоложение нижнего шаблона - MAX")]
            public string FooterMax = "610 698";

            [JsonProperty("Расоложение нижнего шаблона - MIN")]
            public string FooterMin = "300 650";

            [JsonProperty("Частота обновления игрового времени")]
            public float UpdateTime = 5.0f;

            [JsonProperty("Ключ от ipgeolocation.io (Для получения времени игрока, сервис бесплатный)")]
            public string IpGeoAPIKey = "";

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginVersion = new VersionNumber(),
                    IpGeoAPIKey = "",
                };
            }
        }

        class Parser
        {
            public string date_time;
        }

        void OnServerInitialized()
        {
            Init = true;
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
            UpdateActionValues = ServerMgr.Instance.StartCoroutine(UpdateValues());
        }

        void CreateMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "OnlinePanel_Main");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement
            {
                Parent = "Hud",
                Name = "OnlinePanel_Main",
                Components =
                    {
                        new CuiImageComponent { Color = "0 0 0 0"},
                        new CuiRectTransformComponent{ AnchorMin = "0.5 0", AnchorMax = $"0.5 0"},
                    }
            });

            var time = DateTime.UtcNow.ToLocalTime() - PlayersTime[player].time;

            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_Main",
                Components =
                    {
                        new CuiTextComponent { Color = config.PassiveColor.StartsWith("#") ? HexToRustFormat(config.PassiveColor) : config.PassiveColor, FontSize = config.TextSize, Align = TextAnchor.UpperCenter,

                            Text = string.Format(config.TextHeader, TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm"), time.ToString("HH:mm"), BasePlayer.activePlayerList.Count,  ServerMgr.Instance.connectionQueue.Joining + ServerMgr.Instance.connectionQueue.Queued, BasePlayer.sleepingPlayerList.Count)
                        },
                        new CuiRectTransformComponent{ AnchorMin = "0.5 0", AnchorMax = $"0.5 0", OffsetMin = config.HeaderMin, OffsetMax = config.HeaderMax},
                    }
            });
            var color = config.ActiveColor;
            container.Add(new CuiElement
            {
                Parent = "OnlinePanel_Main",
                Components =
                    {
                        new CuiTextComponent { Color = config.PassiveColor.StartsWith("#") ? HexToRustFormat(config.PassiveColor) : config.PassiveColor, FontSize = config.TextSize, Align = TextAnchor.UpperCenter, Text = config.TextFooter.Replace("{tank}", BradleyAPC ? $"<color={color}>{config.PAnzerName}</color>" :  config.PAnzerName).Replace("{ship}", CargoShip ? $"<color={color}>{config.ShipName}</color>" :  config.ShipName).Replace("{plane}", CargoPlane ? $"<color={color}>{config.AirName}</color>" :  config.AirName).Replace("{heli}",  BaseHelicopter ? $"<color={color}>{config.HeliName}</color>" :  config.HeliName).Replace("{chinook}",  CH47Helicopter ? $"<color={color}>{config.ChinookName}</color>" :  config.ChinookName)},
                        new CuiRectTransformComponent{ AnchorMin = "0.5 0", AnchorMax = $"0.5 0", OffsetMin = config.FooterMin, OffsetMax = config.FooterMax},
                    }
            });

            CuiHelper.AddUi(player, container);
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
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
    }
}
