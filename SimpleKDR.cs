using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    [Info("SimpleKDR", "Sempai#3239", "3.0.01")]
    [Description("Simple KDR system for pvp servers")]

    public class SimpleKDR : RustPlugin
    {
        static SimpleKDR _instance;
        CuiElementContainer mainContainer = new CuiElementContainer();
        CuiElementContainer killText = new CuiElementContainer();
        CuiElementContainer baseUi = new CuiElementContainer();

        [PluginReference] Plugin ImageLibrary;

        #region Hooks

        private void Init() => _instance = this;

        void OnServerInitialized()
        {
            LoadData();

            permission.RegisterPermission("simplekdr.use", this);
            permission.RegisterPermission("simplekdr.hiden", this);
            cmd.AddChatCommand(config.ms.hideCmd, this, "ChatCommand");

            if (ImageLibrary != null)
            {
                ImageLibrary.Call("AddImage", config.panelKills.panelIcon, config.panelKills.panelIcon);
                ImageLibrary.Call("AddImage", config.panelDeaths.panelIcon, config.panelDeaths.panelIcon);
                ImageLibrary.Call("AddImage", config.panelRatio.panelIcon, config.panelRatio.panelIcon);
            }

            createBaseUI();
            createKillText();
            createMainContainer();

            foreach (var player in BasePlayer.activePlayerList)
            {

                if (!playerData.ContainsKey(player.userID))
                    playerData.Add(player.userID, new PlayerData());

                if (!permission.UserHasPermission(player.UserIDString, "simplekdr.use"))
                    return;


                CuiHelper.DestroyUi(player, "skdr_main");
                CuiHelper.AddUi(player, $"{mainContainer}");
                CuiHelper.AddUi(player, InsertData(player, $"{baseUi}"));
            }

            timer.Once(0.5f, () => {
                PlayerComponent(true);
            });
        }

        void OnPlayerConnected(BasePlayer player)
        {

            if (!playerData.ContainsKey(player.userID))
                playerData.Add(player.userID, new PlayerData());

            if (!permission.UserHasPermission(player.UserIDString, "simplekdr.use"))
                return;

            CuiHelper.AddUi(player, $"{mainContainer}");
            CuiHelper.AddUi(player, InsertData(player, $"{baseUi}"));

            PlayerComponent(true, player);
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            PlayerComponent(false, player);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                DestroyPanels(player);

            PlayerComponent(false);
            SaveData();
        }

        void OnServerSave() => SaveData();

        void OnNewSave()
        {
            if (!config.ms.wipeReset)
                return;

            LoadData();
            playerData.Clear();
            SaveData();
        }

        void OnEntityDeath(BasePlayer player, HitInfo info)
        {
            try
            {
                if (info == null || info.InitiatorPlayer == null) return;

                var attacker = info.InitiatorPlayer;

                if (attacker == player && config.ms.countSuicide)
                {
                    playerData[player.userID].deaths++;
                    //update ui
                    return;
                }

                if (attacker == player) return;

                if (info.HitEntity.IsNpc && !config.ms.countNpc) return;

                if (playerData.ContainsKey(attacker.userID))
                {
                    playerData[attacker.userID].kills++;
                    DestroyPanels(attacker);
                    CuiHelper.AddUi(attacker, InsertData(attacker, $"{baseUi}"));
                    RunMono(attacker);
                }
            }
            catch
            {
                //kekw
            }
        }

        #endregion 

        #region MonoBehavior

        private Dictionary<BasePlayer, BehaviorScript> _monoBehavior = new Dictionary<BasePlayer, BehaviorScript>();

        private void PlayerComponent(bool add, BasePlayer player = null)
        {
            if (player != null)
            {
                if (add)
                {
                    if (!_monoBehavior.ContainsKey(player))
                        _monoBehavior.Add(player, player.GetOrAddComponent<BehaviorScript>());
                }
                else
                {
                    var run = player.GetComponent<BehaviorScript>();
                    if (run != null)
                        UnityEngine.Object.Destroy(run);

                    if (_monoBehavior.ContainsKey(player))
                        _monoBehavior.Remove(player);
                }
                return;
            }

            if (add)
            {
                foreach (var _player in BasePlayer.activePlayerList)
                {
                    if (!_monoBehavior.ContainsKey(_player))
                        _monoBehavior.Add(_player, _player.GetOrAddComponent<BehaviorScript>());
                }
            }
            else
            {
                foreach (var _player in BasePlayer.activePlayerList)
                {
                    var run = _player.GetComponent<BehaviorScript>();
                    if (run != null)
                        UnityEngine.Object.Destroy(run);

                }
            }
        }

        private class BehaviorScript : FacepunchBehaviour
        {
            BasePlayer player;

            void Awake() => player = GetComponent<BasePlayer>();

            public void ShowKillMsg(BasePlayer _player)
            {
                player = _player;

                if (IsInvoking(nameof(DestroyUI)))
                {
                    CancelInvoke(nameof(DestroyUI));
                    CuiHelper.DestroyUi(player, "kill_text");

                }
                CuiHelper.AddUi(player, _instance.killText);

                Invoke(nameof(DestroyUI), 1.2f);
            }

            void DestroyUI() => CuiHelper.DestroyUi(player, "kill_text");
        }

        #endregion

        #region Methods/Functions

        [ConsoleCommand("kdr_wipe")]
        private void simplekdr_wipedata(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player();
            if (player != null && !player.IsAdmin) return;

            playerData.Clear();
            SaveData();

            if (player != null)
                    player.ConsoleMessage("Data wiped.");

            Puts("Data wiped.");

            foreach (var _player in BasePlayer.activePlayerList)
            {

                if (!playerData.ContainsKey(_player.userID))
                    playerData.Add(_player.userID, new PlayerData());

                if (!permission.UserHasPermission(_player.UserIDString, "simplekdr.use"))
                    return;


                CuiHelper.DestroyUi(_player, "skdr_main");
                CuiHelper.AddUi(_player, $"{mainContainer}");
                CuiHelper.AddUi(_player, InsertData(_player, $"{baseUi}"));
            }


        }

        void ChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "simplekdr.use")) 
            {   
                SendReply(player, GetLang("_noPerm"));
                return;
            }
                
            if (command == config.ms.hideCmd)
            {
                if (permission.UserHasPermission(player.UserIDString, "simplekdr.hiden"))
                {
                    SendReply(player, GetLang("_displayed"));
                    CuiHelper.DestroyUi(player, "skdr_main");
                    CuiHelper.AddUi(player, $"{mainContainer}");
                    CuiHelper.AddUi(player, InsertData(player, $"{baseUi}"));
                    return;
                }
                if (!permission.UserHasPermission(player.UserIDString, "simplekdr.hiden"))
                {
                    permission.GrantUserPermission(player.UserIDString, "simplekdr.hiden", null);
                    CuiHelper.DestroyUi(player, "skdr_main");
                    SendReply(player, GetLang("_hidden"));
                    return;
                }
            }
        }

        private void createBaseUI()
        {
            var ui = new CuiElementContainer();

            CUIClass.CreatePanel(ref ui, "KillsPanel", "skdr_main", config.panelKills.panelColor, config.panelKills.panelAnchorMin, config.panelKills.panelAnchorMax, false, 0f, "assets/icons/iconmaterial.mat");
            CUIClass.CreateImage(ref ui, "KillsPanel", Img(config.panelKills.panelIcon), $"0 0", $"1 1");
            CUIClass.CreateText(ref ui, "KillsText", "KillsPanel", $"1 1 1 0.75", config.panelKills.panelText, 10, "0 0", "1 1", TextAnchor.MiddleCenter, $"{config.panelKills.panelFont}", $"{config.panelKills.panelFontOutColor}", $"{config.panelKills.panelFontOut} {config.panelKills.panelFontOut}");

            CUIClass.CreatePanel(ref ui, "DeathsPanel", "skdr_main", config.panelDeaths.panelColor, config.panelDeaths.panelAnchorMin, config.panelDeaths.panelAnchorMax, false, 0f, "assets/icons/iconmaterial.mat");
            CUIClass.CreateImage(ref ui, "DeathsPanel", Img(config.panelDeaths.panelIcon), $"0 0", $"1 1");
            CUIClass.CreateText(ref ui, "DeathsText", "DeathsPanel", $"1 1 1 0.75", config.panelDeaths.panelText, 10, "0 0", "1 1", TextAnchor.MiddleCenter, $"{config.panelDeaths.panelFont}", $"{config.panelDeaths.panelFontOutColor}", $"{config.panelDeaths.panelFontOut} {config.panelDeaths.panelFontOut}");

            CUIClass.CreatePanel(ref ui, "RatioPanel", "skdr_main", config.panelRatio.panelColor, config.panelRatio.panelAnchorMin, config.panelRatio.panelAnchorMax, false, 0f, "assets/icons/iconmaterial.mat");
            CUIClass.CreateImage(ref ui, "RatioPanel", Img(config.panelRatio.panelIcon), $"0 0", $"1 1");
            CUIClass.CreateText(ref ui, "RatioText", "RatioPanel", $"1 1 1 0.75", config.panelRatio.panelText, 10, "0 0", "1 1", TextAnchor.MiddleCenter, $"{config.panelRatio.panelFont}", $"{config.panelRatio.panelFontOutColor}", $"{config.panelRatio.panelFontOut} {config.panelRatio.panelFontOut}");

            baseUi = ui;
        }

        void createKillText() => killText.Add(new CuiElement
        {
            Parent = "Overlay",
            Name = "kill_text",
            Components =
            {
                new CuiTextComponent
                {
                    Text = config.killPopUp.panelText,
                    FontSize = 11,
                    Font = config.killPopUp.panelFont,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1",
                    FadeIn = 0.4f,
                },

                new CuiOutlineComponent
                {
                    Color = config.killPopUp.panelFontOutColor,
                    Distance = $"{config.killPopUp.panelFontOut} {config.killPopUp.panelFontOut}"
                },

                new CuiRectTransformComponent
                {
                    AnchorMin = config.killPopUp.panelAnchorMin,
                    AnchorMax =  config.killPopUp.panelAnchorMax
                }
            },
            FadeOut = 0.3f
        });

        void createMainContainer() => mainContainer.Add(new CuiPanel
        {
            Image = {
                Color = "0 0 0 0",
                Material = "assets/icons/iconmaterial.mat",
                FadeIn = 0f
            },
            RectTransform = {
                AnchorMin = config.uic.anchorMin,
                AnchorMax = config.uic.anchorMax,
                OffsetMin = config.uic.offsetMin,
                OffsetMax = config.uic.offsetMax
            }
        },
            "Hud",
            "skdr_main"
        );

        private string Img(string link)
        {
            if (ImageLibrary != null)
            {
                if (!(bool)ImageLibrary.Call("HasImage", link))
                    return link;
                else
                    return (string)ImageLibrary?.Call("GetImage", link);
            }
            else return link;
        }

        private void RunMono(BasePlayer player)
        {
            if (!config.killPopUp.panelEnabled) return;

            var run = _monoBehavior[player];

            if (run == null) PlayerComponent(true, player);

            if (run != null) run.ShowKillMsg(player);
        }

        string InsertData(BasePlayer player, string ui)
        {
            var data = playerData[player.userID];

            if (data.kills == 0)
                return ui.Replace("{kills}", data.kills.ToString()).Replace("{deaths}", data.deaths.ToString()).Replace("{ratio}", (0).ToString("0.00"));

            if (data.deaths == 0)
                return ui.Replace("{kills}", data.kills.ToString()).Replace("{deaths}", data.deaths.ToString()).Replace("{ratio}", data.kills.ToString("0.00"));

            return ui.Replace("{kills}", data.kills.ToString()).Replace("{deaths}", data.deaths.ToString()).Replace("{ratio}", ((float)data.kills / (float)data.deaths).ToString("0.00"));
        }

        private void DestroyPanels(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "KillsPanel");
            CuiHelper.DestroyUi(player, "DeathsPanel");
            CuiHelper.DestroyUi(player, "RatioPanel");
        }


        #endregion

        #region Data

        private void SaveData()
        {
            if (playerData != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerData", playerData);
        }

        private Dictionary<ulong, PlayerData> playerData;

        private class PlayerData
        {
            public int kills;
            public int deaths;
            public string name;
        }

        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/PlayerData"))
            {
                playerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>($"{Name}/PlayerData");
            }
            else
            {
                playerData = new Dictionary<ulong, PlayerData>();
                SaveData();
            }
        }


        #endregion

        #region CUI Reusable

        public class CUIClass
        {
            public static void CreatePanel(ref CuiElementContainer _container, string _name, string _parent, string _color, string _anchorMin, string _anchorMax, bool _cursorOn = false, float _fade = 0f, string _mat2 = "")
            {
                _container.Add(new CuiPanel
                {
                    Image = { Color = _color, Material = _mat2, FadeIn = _fade },
                    RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                    CursorEnabled = _cursorOn
                },
                _parent,
                _name);
            }

            public static void CreateImage(ref CuiElementContainer _container, string _parent, string _image, string _anchorMin, string _anchorMax, float _fade = 0f)
            {
                if (_image.StartsWith("http") || _image.StartsWith("www"))
                {
                    _container.Add(new CuiElement
                    {
                        Parent = _parent,
                        Components =
                        {
                            new CuiRawImageComponent { Url = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fade},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax }
                        }
                    });
                }
                else
                {
                    _container.Add(new CuiElement
                    {
                        Parent = _parent,
                        Components =
                        {
                            new CuiRawImageComponent { Png = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fade},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax }
                        }
                    });
                }
            }

            public static void CreateText(ref CuiElementContainer _container, string _name, string _parent, string _color, string _text, int _size, string _anchorMin, string _anchorMax, TextAnchor _align = TextAnchor.MiddleCenter, string _font = "robotocondensed-bold.ttf", string _outlineColor = "", string _outlineScale = "")
            {


                _container.Add(new CuiElement
                {
                    Parent = _parent,
                    Name = _name,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = _text,
                            FontSize = _size,
                            Font = _font,
                            Align = _align,
                            Color = _color,
                            FadeIn = 0f,
                        },

                        new CuiOutlineComponent
                        {

                            Color = _outlineColor,
                            Distance = _outlineScale

                        },

                        new CuiRectTransformComponent
                        {
                             AnchorMin = _anchorMin,
                             AnchorMax = _anchorMax
                        }
                    },
                });
            }
        }
        #endregion

        #region Config 

        private Configuration config;
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.CreateConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        class Configuration
        {
            [JsonProperty(PropertyName = "Main Settings")]
            public MS ms { get; set; }

            public class MS
            {
                [JsonProperty("Reset data on wipe")]
                public bool wipeReset { get; set; }

                [JsonProperty("Count NPC kills")]
                public bool countNpc { get; set; }

                [JsonProperty("Count suicides")]
                public bool countSuicide { get; set; }

                [JsonProperty("Chat command to hide ui")]
                public string hideCmd { get; set; }

            }

            [JsonProperty(PropertyName = "Main Ui Container (used to position all panels at once)")]
            public UiC uic { get; set; }
            public class UiC
            {
                [JsonProperty("Anchor Min")]
                public string anchorMin { get; set; }

                [JsonProperty("Anchor Max")]
                public string anchorMax { get; set; }

                [JsonProperty("Offset Min")]
                public string offsetMin { get; set; }

                [JsonProperty("Offset Max")]
                public string offsetMax { get; set; }

            }

            [JsonProperty(PropertyName = "Kills Panel")]
            public PanelKills panelKills { get; set; }

            public class PanelKills
            {
                [JsonProperty("Enabled")]
                public bool panelEnabled { get; set; }

                [JsonProperty("Panel Color")]
                public string panelColor { get; set; }

                [JsonProperty("Img")]
                public string panelIcon { get; set; }

                [JsonProperty("Text")]
                public string panelText { get; set; }

                [JsonProperty("Font")]
                public string panelFont { get; set; }

                [JsonProperty("Text Outline Thickness")]
                public string panelFontOut { get; set; }

                [JsonProperty("Text Outline Color")]
                public string panelFontOutColor { get; set; }

                [JsonProperty("Anchor Min")]
                public string panelAnchorMin { get; set; }

                [JsonProperty("Anchor Max")]
                public string panelAnchorMax { get; set; }

            }

            [JsonProperty(PropertyName = "Deaths Panel")]
            public PanelDeaths panelDeaths { get; set; }

            public class PanelDeaths
            {
                [JsonProperty("Enabled")]
                public bool panelEnabled { get; set; }

                [JsonProperty("Panel Color")]
                public string panelColor { get; set; }

                [JsonProperty("Img")]
                public string panelIcon { get; set; }

                [JsonProperty("Text")]
                public string panelText { get; set; }

                [JsonProperty("Font")]
                public string panelFont { get; set; }

                [JsonProperty("Text Outline Thickness")]
                public string panelFontOut { get; set; }

                [JsonProperty("Text Outline Color")]
                public string panelFontOutColor { get; set; }

                [JsonProperty("Anchor Min")]
                public string panelAnchorMin { get; set; }

                [JsonProperty("Anchor Max")]
                public string panelAnchorMax { get; set; }

            }

            [JsonProperty(PropertyName = "Ratio Panel")]
            public PanelRatio panelRatio { get; set; }

            public class PanelRatio
            {
                [JsonProperty("Enabled")]
                public bool panelEnabled { get; set; }

                [JsonProperty("Panel Color")]
                public string panelColor { get; set; }

                [JsonProperty("Img")]
                public string panelIcon { get; set; }

                [JsonProperty("Text")]
                public string panelText { get; set; }

                [JsonProperty("Font")]
                public string panelFont { get; set; }

                [JsonProperty("Text Outline Thickness")]
                public string panelFontOut { get; set; }

                [JsonProperty("Text Outline Color")]
                public string panelFontOutColor { get; set; }

                [JsonProperty("Anchor Min")]
                public string panelAnchorMin { get; set; }

                [JsonProperty("Anchor Max")]
                public string panelAnchorMax { get; set; }

            }

            [JsonProperty(PropertyName = "Pop up notification on kill")]
            public KillPopUp killPopUp { get; set; }

            public class KillPopUp
            {
                [JsonProperty("Enabled")]
                public bool panelEnabled { get; set; }

                [JsonProperty("Text")]
                public string panelText { get; set; }

                [JsonProperty("Font")]
                public string panelFont { get; set; }

                [JsonProperty("Text Outline Thickness")]
                public string panelFontOut { get; set; }

                [JsonProperty("Text Outline Color")]
                public string panelFontOutColor { get; set; }

                [JsonProperty("Anchor Min")]
                public string panelAnchorMin { get; set; }

                [JsonProperty("Anchor Max")]
                public string panelAnchorMax { get; set; }

            }



            public static Configuration CreateConfig()
            {
                return new Configuration
                {

                    ms = new SimpleKDR.Configuration.MS
                    {

                        wipeReset = true,
                        countNpc = false,
                        countSuicide = true,
                        hideCmd = "kdr",
                    },

                    uic = new SimpleKDR.Configuration.UiC
                    {
                        anchorMin = "0 0",
                        anchorMax = "0 0",
                        offsetMin = "5 5",
                        offsetMax = "250 25",
                    },

                    panelKills = new SimpleKDR.Configuration.PanelKills
                    {
                        panelEnabled = true,
                        panelColor = "0 0 0 0",
                        panelIcon = "https://rustplugins.net/products/KDR-assets/greenstripe.png",
                        panelText = "<b>{kills}</b> KILLS",
                        panelFont = "robotocondensed-regular.ttf",
                        panelFontOut = "0",
                        panelFontOutColor = "0 0 0 0",
                        panelAnchorMin = "0 0",
                        panelAnchorMax = "0.3 1",
                    },

                    panelDeaths = new SimpleKDR.Configuration.PanelDeaths
                    {
                        panelEnabled = true,
                        panelColor = "0 0 0 0",
                        panelIcon = "https://rustplugins.net/products/KDR-assets/redstripe.png",
                        panelText = "<b>{deaths}</b> DEATHS",
                        panelFont = "robotocondensed-regular.ttf",
                        panelFontOut = "0",
                        panelFontOutColor = "0 0 0 0",
                        panelAnchorMin = "0.33 0",
                        panelAnchorMax = "0.63 1"
                    },

                    panelRatio = new SimpleKDR.Configuration.PanelRatio
                    {
                        panelEnabled = true,
                        panelColor = "0 0 0 0",
                        panelIcon = "https://rustplugins.net/products/KDR-assets/bluestripe.png",
                        panelText = "<b>{ratio}</b> RATIO",
                        panelFont = "robotocondensed-regular.ttf",
                        panelFontOut = "0",
                        panelFontOutColor = "0 0 0 0",
                        panelAnchorMin = "0.66 0",
                        panelAnchorMax = "1 1"
                    },

                    killPopUp = new SimpleKDR.Configuration.KillPopUp
                    {
                        panelEnabled = true,
                        panelText = "+1",
                        panelFont = "<size=17><color=#ffc126>+1 KILL</color></size>",
                        panelFontOut = "0.8",
                        panelFontOutColor = "0 0 0 1",
                        panelAnchorMin = "0.55 0.5",
                        panelAnchorMax = "0.66 0.6"
                    },

                };
            }

        }
        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["_noPerm"] = "<color=#C57039>[SimpleKDR]</color> You don't have permission to do that.",
                ["_hidden"] = "<color=#C57039>[SimpleKDR]</color> Hidden.",
                ["_displayed"] = "<color=#C57039>[SimpleKDR]</color> Displayed.",

            }, this);
        }

        private string GetLang(string _message) => lang.GetMessage(_message, this);

        #endregion

    }
}