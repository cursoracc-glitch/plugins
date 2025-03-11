/*
 ########### README ####################################################
                                                                             
  !!! DON'T EDIT THIS FILE !!!
                                                                     
 ########### CHANGES ###################################################

 1.0.0
    - Plugin release
 1.0.1
    - Change CanNetworkTo to object

 #######################################################################
*/

using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Pvp Markers", "Paul", "1.0.1")]
    [Description("PvP Markers on the map")]
    class PvpMarkers : RustPlugin
    {
        #region [Fields]

        private const string permAllow = "pvpmarkers.allow";
        private Configuration config;
        private HashSet<MapMarkerGenericRadius> pvpMarkers = new HashSet<MapMarkerGenericRadius>();

        #endregion

        #region [Oxide Hooks]

        private void Init() => permission.RegisterPermission(permAllow, this);

        private void Unload() => ClearMarkers();

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (info == null)
                return;

            if (player.IsNpc && !config.allowNpc)
                return;

            var attacker = info.InitiatorPlayer;
            if (attacker == null)
                return;

            if (player == attacker)
                return;

            if (IsFar(attacker.ServerPosition))
                CreateMarker(player.ServerPosition);
        }

        private object CanNetworkTo(MapMarkerGenericRadius marker, BasePlayer player)
        {
            if (marker == null || player == null)
                return null;

            if (pvpMarkers.Contains(marker) && !permission.UserHasPermission(player.UserIDString, permAllow))
                return false;

            return null;
        }

        #endregion

        #region [Hooks]   

        private void CreateMarker(Vector3 position)
        {
            MapMarkerGenericRadius marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", position) as MapMarkerGenericRadius;
            if (marker == null)
                return;

            pvpMarkers.Add(marker);
            marker.alpha = config.markerConfiguration.markerAlpha;
            marker.radius = config.markerConfiguration.markerRadius;
            marker.color1 = ParseColor(config.markerConfiguration.markerColor1);
            marker.color2 = ParseColor(config.markerConfiguration.markerColor2);
            marker.Spawn();
            marker.SendUpdate();

            timer.In(config.markerConfiguration.markerDuration, () =>
            {
                marker.Kill();
                marker.SendUpdate();
                pvpMarkers.Remove(marker);
            });
        }

        private void ClearMarkers()
        {
            foreach (var marker in pvpMarkers)
            {
                if (marker != null)
                {
                    marker.Kill();
                    marker.SendUpdate();
                }
            }

            pvpMarkers.Clear();
        }

        private bool IsFar(Vector3 position)
        {
            bool isFar = true;
            foreach (var marker in pvpMarkers)
            {
                if (GetDistance(marker.ServerPosition, position) < config.markerDistance)
                {
                    isFar = false;
                    break;
                }
            }

            return isFar;
        }

        private double GetDistance(Vector3 pos1, Vector3 pos2)
        {
            return Math.Round(Vector3.Distance(pos1, pos2), 0);
        }

        private Color ParseColor(string hexColor)
        {
            if (!hexColor.StartsWith("#"))
                hexColor = $"#{hexColor}";

            Color color;
            if (ColorUtility.TryParseHtmlString(hexColor, out color))
                return color;

            return Color.white;
        }

        #endregion

        #region [Chat Commands]

        [ChatCommand("pmtest")]
        private void cmdRaidMarker(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, GetLang("NoPerm", player.UserIDString));
                return;
            }

            CreateMarker(player.ServerPosition);
            SendReply(player, GetLang("TestPvpMarker", player.UserIDString));
        }

        #endregion

        #region [Classes]

        private class Configuration
        {
            [JsonProperty(PropertyName = "Distance when place new marker from another marker")]
            public int markerDistance;

            [JsonProperty(PropertyName = "Allow NPC")]
            public bool allowNpc;

            [JsonProperty(PropertyName = "Marker configuration")]
            public MarkerConfiguration markerConfiguration;

            public VersionNumber version;
        }

        private class MarkerConfiguration
        {
            [JsonProperty(PropertyName = "Alpha")]
            public float markerAlpha;

            [JsonProperty(PropertyName = "Radius")]
            public float markerRadius;

            [JsonProperty(PropertyName = "Color1")]
            public string markerColor1;

            [JsonProperty(PropertyName = "Color2")]
            public string markerColor2;

            [JsonProperty(PropertyName = "Duration")]
            public float markerDuration;
        }

        #endregion

        #region [Config]

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                markerDistance = 20,
                allowNpc = false,
                markerConfiguration = new MarkerConfiguration
                {
                    markerAlpha = 0.6f,
                    markerRadius = 0.35f,
                    markerDuration = 90f,
                    markerColor1 = "#000000",
                    markerColor2 = "#3498db"
                },
                version = Version
            };
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
            Puts("Generating new configuration file........");
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();

                if (config == null)
                    LoadDefaultConfig();

                if (config.version < Version)
                    UpdateConfig();
            }
            catch
            {
                PrintError("######### Configuration file is not valid! #########");
                return;
            }

            SaveConfig();
        }

        private void UpdateConfig()
        {
            Puts("Updating configuration values.....");
            config.version = Version;
            Puts("Configuration updated");
        }

        #endregion

        #region [Localization]

        private string GetLang(string key, string playerID) => lang.GetMessage(key, this, playerID);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "NoPerm", "You don't have permissions" },
                { "TestPvpMarker", "Test PvP Marker created on your position" }

            }, this);
        }

        #endregion
    }
}