using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Random = UnityEngine.Random;
using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PanelV2", "fermenspwnz", "2.1.6")]
    [Description("Красивая панель с отображением онлайна, времени, вертолета, аирдропа, челнока, танка и корабля.")]
    class PanelV2 : RustPlugin
    {
        static int downloaded;
        Plugin ImageLibrary => Interface.Oxide.RootPluginManager.GetPlugin("ImageLibrary");
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        void gettimage(string url, string name)
        {
            string img = GetImage(name);
            string none = GetImage("NONE");
            string loading = GetImage("LOADING");
            if (string.IsNullOrEmpty(img) || string.IsNullOrEmpty(none) || none == img || string.IsNullOrEmpty(loading) || loading == img)
            {
                AddImage(url, name);
                timer.Once(1f, () => gettimage(url, name));
                return;
            }
            downloaded++;
            if (downloaded.Equals(config.imagelist.Count))
            {
                Debug.Log("Подгрузили все картинки! >>PANELV2<<");
                Subscribe(nameof(OnPlayerConnected));
                if (config.messages != null && config.messages.Count() > 0)
                {
                    new PluginTimers(this).Every(config.infotimer, () => FarmGUI(TypeGui.Message));
                }
                new PluginTimers(this).Every(5f, () => FarmGUI(TypeGui.Time));
                FarmGUI(TypeGui.All);
            }
        }
        List<ulong> _players = new List<ulong>();

        #region Config
        static Dictionary<string, string> _imagelist = new Dictionary<string, string>
        {
            {"https://gspics.org/images/2019/03/11/mUaUn.png","chelnok"},
            {"https://gspics.org/images/2019/03/11/mUSE7.png","heli"},
            {"https://gspics.org/images/2019/03/11/mUR8K.png","plane"},
            {"https://gspics.org/images/2019/03/11/mUOGE.png","cargo"},
            {"https://gspics.org/images/2019/03/24/UCU8o.png","tank"}
        };

        static List<string> _messages = new List<string>
        {
          "Настроить <color=#ffff99>Информационную строку</color> можно в конфиге плагина <color=#ffff66>PanelV2</color>",
          "Настроить <color=#ffff99>Infoрмационную строку</color> можно в конфиге плагина <color=#ffff66>PanelV2</color>",
          "Настроить <color=#ffff99>'Информационную строку'</color> можно в конфиге плагина <color=#ffff66>PanelV2</color>"
        };

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Накрутка онлайна")]
            public int onliner = 0;

            [JsonProperty("Информационная строка (если пусто, то выключена)")]
            public List<string> messages;

            [JsonProperty("Информационная строка | Частота обновлений в секундах")]
            public float infotimer;

            [JsonProperty("Сообщение | Онлайн")]
            public string message;

            [JsonProperty("Онлайн - размер текста")]
            public string onlinesize;

            [JsonProperty("Время - размер текста")]
            public string timersize;

            [JsonProperty("Картинки")]
            public Dictionary<string, string> imagelist;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    onliner = 0,
                    messages = _messages,
                    infotimer = 60f,
                    imagelist = _imagelist,
                    message = "Онлайн {players} из {maxplayers}",
                    onlinesize = "13",
                    timersize = "13",
                };
            }
        }
        #endregion

        #region Function
        string[] panels = { "timer", "boat", "tank", "FarmGUI3", "online", "plane", "helis", "chelnok" };
        void DestroyUI(Network.Connection player)
        {
            foreach (var z in panels) CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player }, null, "DestroyUI", z);
        }
        #endregion

        #region Hooks
        void Unload()
        {
            foreach (var z in panels) CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", z);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "message");
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            NextTick(() =>
            {
                FarmGUI(TypeGui.Online);
            });
        }

        static string GUIjsontimer = "";
        static string GUIjsononline = "";

        private void Init()
        {
            Unsubscribe(nameof(OnPlayerConnected));
        }

        void OnServerInitialized()
        {
            AddImage("http://i.imgur.com/sZepiWv.png", "NONE", 0);
            AddImage("http://i.imgur.com/lydxb0u.png", "LOADING", 0);
            if (config.timersize == null)
            {
                config.timersize = "13";
                config.onlinesize = "13";
                SaveConfig();
            }
            if (config.message == null)
            {
                config.imagelist = _imagelist;
                config.message = "Онлайн {players} из {maxplayers}";
                SaveConfig();
            }
            if (!ImageLibrary)
            {
                PrintWarning("Image Library не обнаружен, отгружаем Панель.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            GUIjsontimer = "[{\"name\":\"timer\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"fontSize\":\"{timesize}\",\"color\":\"1 1 1 0.5\",\"text\":\"{text}\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"0 29.976\",\"offsetmax\":\"55.8 55.7\"}]}]".Replace("{timesize}", config.timersize);
            GUIjsononline = "[{\"name\":\"online\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"fontSize\":\"{onlinesize}\",\"color\":\"1 1 1 0.5\",\"text\":\"{text}\",\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"0 5.09\",\"offsetmax\":\"120 25.19\"}]}]".Replace("{onlinesize}", config.onlinesize);
            foreach (var z in config.imagelist) gettimage(z.Key, z.Value);
            ships = UnityEngine.Object.FindObjectsOfType<CargoShip>().Count();
            planes = UnityEngine.Object.FindObjectsOfType<CargoPlane>().Count();
            helicopters = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>().Count();
            tanks = UnityEngine.Object.FindObjectsOfType<BradleyAPC>().Count();
            chinooks = UnityEngine.Object.FindObjectsOfType<CH47HelicopterAIController>().Count();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerConnected(player));
                return;
            }
            NextTick(() =>
            {
                if (player == null || !player.IsConnected) return;
                if (!_players.Contains(player.userID)) FarmGUI(TypeGui.All, new List<Network.Connection> { player.net.connection });
                FarmGUI(TypeGui.Online);
            });
        }



        int helicopters = 0;
        int planes = 0;
        int ships = 0;
        int tanks = 0;
        int chinooks = 0;

        void OnEntityKill(BaseNetworkable Entity)
        {
            if (Entity == null) return;
            if (Entity is BaseHelicopter)
            {
                helicopters--;
                if (helicopters == 0) FarmGUI(TypeGui.Heli);
            }
            else if (Entity is CargoPlane)
            {
                planes--;
                if (planes == 0) FarmGUI(TypeGui.Plane);
            }
            else if (Entity is CargoShip)
            {
                ships--;
                if (ships == 0) FarmGUI(TypeGui.Ship);
            }
            else if (Entity is BradleyAPC)
            {
                tanks--;
                if (tanks == 0) FarmGUI(TypeGui.Tank);
            }
            else if (Entity is CH47HelicopterAIController)
            {
                chinooks--;
                if (chinooks == 0) FarmGUI(TypeGui.Chinook);
            }
        }

        private void OnEntitySpawned(BaseNetworkable Entity)
        {
            if (Entity == null) return;
            if (Entity is BaseHelicopter)
            {
                helicopters++;
                if (helicopters == 1) FarmGUI(TypeGui.Heli);
            }
            else if (Entity is CargoPlane)
            {
                planes++;
                if (planes == 1) FarmGUI(TypeGui.Plane);
            }
            else if (Entity is CargoShip)
            {
                ships++;
                if (ships == 1) FarmGUI(TypeGui.Ship);
            }
            else if (Entity is BradleyAPC)
            {
                tanks++;
                if (tanks == 1) FarmGUI(TypeGui.Tank);
            }
            else if (Entity is CH47HelicopterAIController)
            {
                chinooks++;
                if (chinooks == 1) FarmGUI(TypeGui.Chinook);
            }
        }
        #endregion

        [ConsoleCommand("gategui")]
        void gategui(ConsoleSystem.Arg arg)
        {
            ulong userid = arg.Connection.userid;
            if (cooldown.ContainsKey(userid) && cooldown[userid] > DateTime.Now)
            {
                arg.Player().Command("chat.add", 2, 0, "Не так часто!");
                return;
            }
            if (!_players.Contains(userid))
            {
                _players.Add(userid);
                CloseGUI(arg.Connection);
            }
            else
            {
                _players.Remove(userid);
                if (!_players.Contains(userid)) FarmGUI(TypeGui.All, new List<Network.Connection> { arg.Connection });
                cooldown[userid] = DateTime.Now.AddSeconds(10);
            }
        }

        #region GUI
        enum TypeGui
        {
            Message, All, Online, Time, Heli, Tank, Chinook, Plane, Ship
        }

        void CloseGUI(Network.Connection connect)
        {
            DestroyUI(connect);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = connect }, null, "AddUI", GUIjsondisable);
        }

        Dictionary<ulong, DateTime> cooldown = new Dictionary<ulong, DateTime>();


        const string GUIjsonmessage = "[{\"name\":\"message\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"fontSize\":\"13\",\"color\":\"1 1 1 0.5\",\"text\":\"{text}\",\"align\":\"LowerCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.495 0\",\"anchormax\":\"0.495 0\",\"offsetmin\":\"-500 0\",\"offsetmax\":\"500 20\"}]}]";
        const string GUIjsonfon = "[{\"name\":\"FarmGUI3\", \"parent\":\"Hud\", \"components\":[{\"type\":\"UnityEngine.UI.Image\", \"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\"}]},{\"name\":\"7a37bb60454b43e995e914a1ccc042e8\",\"parent\":\"FarmGUI3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/namefontmaterial.mat\",\"color\":\"0.95 0.95 0.95 0.025\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"57.6 29.976\",\"offsetmax\":\"86.4 55.7\"}]},{\"name\":\"538cf01fbd6842298dbbc3addec341e8\",\"parent\":\"FarmGUI3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/namefontmaterial.mat\",\"color\":\"0.95 0.95 0.95 0.025\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"88.2 29.976\",\"offsetmax\":\"117 55.7\"}]},{\"name\":\"5db1aeebf0294093a77b0e72194c6946\",\"parent\":\"FarmGUI3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/namefontmaterial.mat\",\"color\":\"0.95 0.95 0.95 0.025\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"118.8 29.976\",\"offsetmax\":\"147.6 55.7\"}]},{\"name\":\"c6a8a5f3882649b8ae89392faf557f20\",\"parent\":\"FarmGUI3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/namefontmaterial.mat\",\"color\":\"0.95 0.95 0.95 0.025\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"149.4 29.976\",\"offsetmax\":\"178.2 55.7\"}]},{\"name\":\"1b31e40057534d32a978915563513fa1\",\"parent\":\"FarmGUI3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/namefontmaterial.mat\",\"color\":\"0.95 0.95 0.95 0.025\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"0 29.976\",\"offsetmax\":\"55.7 55.7\"}]},{\"name\":\"f6d9849bec0d4b15a757e986bdfdda4c\",\"parent\":\"FarmGUI3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/namefontmaterial.mat\",\"color\":\"0.95 0.95 0.95 0.025\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"118.8 2.171\",\"offsetmax\":\"147.6 28.056\"}]},{\"name\":\"402c08c95caa484f94a848e7dfdb2b37\",\"parent\":\"FarmGUI3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"material\":\"assets/content/ui/namefontmaterial.mat\",\"color\":\"0.95 0.95 0.95 0.025\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"0 2.171\",\"offsetmax\":\"117 28.056\"}]},{\"name\":\"c92e68d80c68458885a5c487f497b9f8\",\"parent\":\"FarmGUI3\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"gategui\",\"material\":\"assets/content/ui/namefontmaterial.mat\",\"color\":\"0.95 0.95 0.95 0.025\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"149.4 2.171\",\"offsetmax\":\"178.2 28.056\"}]},{\"parent\":\"c92e68d80c68458885a5c487f497b9f8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"↴\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\"}]}]";
        static string GUIjsondisable = "[{\"name\":\"FarmGUI3\", \"parent\":\"Hud\", \"components\":[{\"type\":\"UnityEngine.UI.Image\", \"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\"}]},{\"name\":\"c92e68d80c68458885a5c487f497b9f8\",\"parent\":\"FarmGUI3\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"gategui\",\"material\":\"assets/content/ui/namefontmaterial.mat\",\"color\":\"0.95 0.95 0.95 0.025\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"149.4 2.171\",\"offsetmax\":\"178.2 28.056\"}]},{\"parent\":\"c92e68d80c68458885a5c487f497b9f8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"↰\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\"}]}]";
        static string GUIjsononplane = "[{\"name\":\"plane\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"{color}\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"61.2 32.9\",\"offsetmax\":\"82.8 53\"}]}]";
        static string GUIjsononship = "[{\"name\":\"boat\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"{color}\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"153 32.9\",\"offsetmax\":\"174.6 53\"}]}]";
        static string GUIjsonontank = "[{\"name\":\"tank\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"{color}\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"122.4 5.09\",\"offsetmax\":\"144 25.19\"}]}]";
        static string GUIjsononheli = "[{\"name\":\"helis\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"{color}\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"91.8 32.9\",\"offsetmax\":\"113.4 53\"}]}]";
        static string GUIjsononchinook = "[{\"name\":\"chelnok\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"{color}\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.69 0.0195\",\"anchormax\":\"0.69 0.0195\",\"offsetmin\":\"122.4 32.9\",\"offsetmax\":\"144 53\"}]}]";

        private void FarmGUI(TypeGui funct = TypeGui.All, List<Network.Connection> sendto = null)
        {
            if (sendto == null) sendto = Network.Net.sv.connections.Where(x => !_players.Contains(x.userid)).ToList();
            if (funct == TypeGui.All)
            {
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "FarmGUI3");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", GUIjsonfon);
            }
            if (funct == TypeGui.All || funct == TypeGui.Message)
            {
                string text = GUIjsonmessage.Replace("{text}", config.messages[Random.Range(0, config.messages.Count)]);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "message");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", text);
            }
            if (funct == TypeGui.All || funct == TypeGui.Time)
            {
                string text = GUIjsontimer.Replace("{text}", TOD_Sky.Instance.Cycle.DateTime.ToString("HH:mm", CultureInfo.InvariantCulture));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "timer");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", text);
            }
            if (funct == TypeGui.All || funct == TypeGui.Online)
            {
                string text = GUIjsononline.Replace("{text}", config.message.Replace("{players}", (BasePlayer.activePlayerList.Count + config.onliner).ToString()).Replace("{maxplayers}", ConVar.Server.maxplayers.ToString()));
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "online");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", text);
            }
            if (funct == TypeGui.All || funct == TypeGui.Plane)
            {
                string gui = GUIjsononplane.Replace("{png}", GetImage("plane")).Replace("{color}", planes > 0 ? "0.5 1 0.5 0.7" : "1 1 1 0.7");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "plane");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", gui);
            }
            if (funct == TypeGui.All || funct == TypeGui.Ship)
            {
                string gui = GUIjsononship.Replace("{png}", GetImage("cargo")).Replace("{color}", ships > 0 ? "0 0.7 1 0.7" : "1 1 1 0.7");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "boat");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", gui);
            }
            if (funct == TypeGui.All || funct == TypeGui.Tank)
            {
                string gui = GUIjsonontank.Replace("{png}", GetImage("tank")).Replace("{color}", tanks > 0 ? "0.7 0.9 0.5 0.7" : "1 1 1 0.7");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "tank");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", gui);
            }
            if (funct == TypeGui.All || funct == TypeGui.Heli)
            {
                string gui = GUIjsononheli.Replace("{png}", GetImage("heli")).Replace("{color}", helicopters > 0 ? "1 0.5 0.5 0.7" : "1 1 1 0.7");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "helis");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", gui);
            }
            if (funct == TypeGui.All || funct == TypeGui.Chinook)
            {
                string gui = GUIjsononchinook.Replace("{png}", GetImage("chelnok")).Replace("{color}", chinooks > 0 ? "0.2 0.8 0.4 0.7" : "1 1 1 0.7");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "DestroyUI", "chelnok");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = sendto }, null, "AddUI", gui);
            }

        }
        #endregion
    }
}