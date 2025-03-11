using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZoneInfoGUI", "SNAK84", "1.0.1")]
    public class ZoneInfoGUI : RustPlugin
    {
        [PluginReference] Plugin ZoneManager;

        #region Config
        private ConfigData Conf;
        private class ConfigData
        {
            public string AnchorMax = "0.307 0.95";
            public string AnchorMin = "0 0.92";
            public int FontSize = 15;
            public List<ZoneConfig> ZonesConfig = new List<ZoneConfig>();
        }
        private class ZoneConfig
        {
            public bool Enable = true;
            public string ZoneID;
            public string Text = "Вы находитесь в зоне {zonename}";
            public string Color = "0 1 0 0.4";
            public string TextColor = "1 1 1 1";
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            var config = new ConfigData();
            SaveConfig();
        }

        private void LoadConfigVariables()
        {
            Conf = Config.ReadObject<ConfigData>();
            SaveConfig(Conf);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        #endregion


        #region Oxide hooks
        void Init()
        {
            LoadConfigVariables();
        }

        void OnPluginLoaded(Plugin name)
        {
            if (name == ZoneManager)
            {
                PrintError("Plugin ZoneManager has been loaded");
                OnServerInitialized();
            }
        }

        void OnServerInitialized()
        {
            if (!ZoneManager)
            {
                PrintError("Unable to find a valid ZoneManager plugin! Unable to continue");
                return;
            }

            int DelZone = 0, AddZone = 0;

            List<string> Zones = ZoneManager.Call<string[]>("GetZoneIDs").ToList<string>();

            foreach (string ZoneId in Zones)
            {
                ZoneConfig Zone = Conf.ZonesConfig.Find(p => p.ZoneID == ZoneId);

                if (Zone == null)
                {
                    Conf.ZonesConfig.Add(new ZoneConfig { ZoneID = ZoneId });
                    AddZone++;
                }
            }
            
            foreach (ZoneConfig Zone in Conf.ZonesConfig)
            {
                if (ZoneManager.Call<string>("CheckZoneID", Zone.ZoneID) == null)
                {
                    Conf.ZonesConfig.Remove(Zone);
                    DelZone++;
                }
            }

            SaveConfig(Conf);
            if (AddZone > 0) Puts("Added new zones to config: " + AddZone.ToString());
            if (DelZone > 0) Puts("Removed zones from config: " + DelZone.ToString());

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                string[] PZones = ZoneManager.Call<string[]>("GetPlayerZoneIDs", player);
                if (PZones != null) CreateUI(player, GetZone(PZones[0]));
            }

        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            string[] PZones = ZoneManager?.Call<string[]>("GetPlayerZoneIDs", player);
            if (PZones != null) CreateUI(player, GetZone(PZones[0]));
        }

        void OnEnterZone(string ZoneID, BasePlayer player)
        {
            CreateUI(player, GetZone(ZoneID));
        }

        void OnExitZone(string ZoneID, BasePlayer player)
        {
            DestroyUI(player);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            DestroyUI(player);
        }

        #endregion

        ZoneConfig GetZone(string ZoneId)
        {
            ZoneConfig Zone = Conf.ZonesConfig.Find(p => p.ZoneID == ZoneId);
            if (Zone == null)
            {
                Zone = new ZoneConfig { ZoneID = ZoneId };
                Conf.ZonesConfig.Add(Zone);
            }

            return Zone;
        }


        #region UI


        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ZoneInfoGUI");
        }

        void CreateUI(BasePlayer player, ZoneConfig Zone)
        {

            DestroyUI(player);

            if (!Zone.Enable) return;

            string ZoneText = Zone.Text.Replace("{zonename}", ZoneManager.Call<string>("GetZoneName", Zone.ZoneID));

            CuiElementContainer container = new CuiElementContainer();

            var panel = container.Add(new CuiPanel()
            {
                Image = { Color = Zone.Color },
                RectTransform = { AnchorMin = Conf.AnchorMin, AnchorMax = Conf.AnchorMax }
            }, "Hud", "ZoneInfoGUI");

            CuiElement element = new CuiElement
            {
                Parent = panel,
                Components = {
                    new CuiTextComponent { Text = ZoneText, FontSize = Conf.FontSize, Color = Zone.TextColor, Align = TextAnchor.MiddleCenter },
                    new CuiRectTransformComponent { AnchorMin = "0.0 0.0", AnchorMax = "1.0 1.0" }
                }
            };
            container.Add(element);

            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}
