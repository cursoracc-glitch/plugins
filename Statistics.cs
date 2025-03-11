using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Math;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Statistics", "oxide-russia.ru", "0.1.5")]
    [Description("Statistics")]
    public class Statistics : RustPlugin
    {
        [PluginReference]
        Plugin ImageLibrary;
        private StoredData DataBase = new StoredData();
        public ulong lastDamageName;
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);

        #region Config

        private static ConfigFile config;

        public class ConfigFile
        {
            [JsonProperty(PropertyName = "StatsCmd")]
            public string StatsCmd { get; set; } = "stats";
            
            [JsonProperty(PropertyName = "ServerNAME")]
            public string ServerName { get; set; } = "YourServerName";
            
            [JsonProperty(PropertyName = "MainHeaderColor")]
            public string MainHeaderColor { get; set; } = "#C6C6C6FF";
            
            [JsonProperty(PropertyName = "MainHeaderButtonColor")]
            public string MainHeaderButtonColor { get; set; } = "#969696FF";
            
            [JsonProperty(PropertyName = "MainHeaderButtonEdgeColor")]
            public string MainHeaderButtonEdgeColor { get; set; } = "#C6C6C6FF";
            
            [JsonProperty(PropertyName = "MainHeaderButtonCloseColor")]
            public string MainHeaderButtonCloseColor { get; set; } = "#969696FF";
            
            [JsonProperty(PropertyName = "PlayerStatsHeaderColor1")]
            public string PlayerStatsHeaderColor1 { get; set; } = "#969696FF";
            
            [JsonProperty(PropertyName = "PlayerStatsHeaderColor2")]
            public string PlayerStatsHeaderColor2 { get; set; } = "#C6C6C6FF";
            
            [JsonProperty(PropertyName = "PlayerStatsBlocColor1")]
            public string PlayerStatsBlocColor1 { get; set; } = "#C6C6C6FF";
            
            [JsonProperty(PropertyName = "PlayerStatsBlocColor2")]
            public string PlayerStatsBlocColor2 { get; set; } = "#969696FF";
            
            [JsonProperty(PropertyName = "PlayerStatsProfileLineColor")]
            public string PlayerStatsProfileLineColor { get; set; } = "#C6C6C6FF";
            
            [JsonProperty(PropertyName = "PlayerStatsTextColor")]
            public string PlayerStatsTextColor { get; set; } = "#FFFFFFFF"; 
            
            [JsonProperty(PropertyName = "ServerStatsButtonsUpLine")]
            public string ServerStatsButtonsUpLine { get; set; } = "#C6C6C6FF";  
            
            [JsonProperty(PropertyName = "ServerStatsButtonsDownLine")]
            public string ServerStatsButtonsDownLine { get; set; } = "#C6C6C6FF";   
            
            [JsonProperty(PropertyName = "ServerStatsButtonsColor")]
            public string ServerStatsButtonsColor { get; set; } = "#969696FF"; 
            
            [JsonProperty(PropertyName = "ServerStatsButtonsEdgeColor")]
            public string ServerStatsButtonsEdge { get; set; } = "#C6C6C6FF"; 
            
            [JsonProperty(PropertyName = "ServerStatsTopColor1")]
            public string ServerStatsTopColor1 { get; set; } = "#C6C6C6FF"; 
            
            [JsonProperty(PropertyName = "ServerStatsTopColor2")]
            public string ServerStatsTopColor2 { get; set; } = "#969696FF";  
            
            [JsonProperty(PropertyName = "ServerStatsTopEdgeColor")]
            public string ServerStatsTopEdgeColor { get; set; } = "#FFFFFFFF"; 
            
            [JsonProperty(PropertyName = "ServerStatsTextColor")]
            public string ServerStatsTextColor { get; set; } = "#FFFFFFFF"; 
            
            [JsonProperty(PropertyName = "ServerStatsTopTextColor1")]
            public string ServerStatsTopTextColor1 { get; set; } = "#FFFFFFFF"; 
            
            [JsonProperty(PropertyName = "ServerStatsTopTextColor2")]
            public string ServerStatsTopTextColor2 { get; set; } = "#FFFFFFFF"; 
            
        }
        protected override void SaveConfig() => Config.WriteObject(config); 
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigFile>();
                if (config == null)
                    Regenerate();
            }
            catch { Regenerate(); }
        }
        private void Regenerate()
        {
            LoadDefaultConfig();
        }
        
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigFile();
            Config.WriteObject(config);
        }
        #endregion

        #region Lang

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["MyStatsButton"] = "Моя статистика",
                ["ServerStatsButton"] = "Обшая статистика",
                ["AnimalKillHeader"] = "Убийства животных",
                ["Bears"] = "Медведей",
                ["Horses"] = "Лошадей",
                ["Stug"] = "Оленей",
                ["Boar"] = "Кабанов",
                ["Chicken"] = "Куриц",
                ["Wolf"] = "Волков",
                ["KillsHeader"] = "Убийства",
                ["Kills"] = "Убийств",
                ["Deatchs"] = "Смертей",
                ["Animals"] = "Животных",
                ["Bradleys"] = "Танков",
                ["Helis"] = "Вертолётов",
                ["GatherHeader"] = "Добыто",
                ["Sulfur"] = "Сера",
                ["Stones"] = "Камни",
                ["Metal"] = "Железа",
                ["Wood"] = "Дерева",
                ["Profile"] = "Профиль",
                ["Time"] = "Время",
                ["TimeButton"] = "Время",
                ["KillsButton"] = "Убийств",
                ["DeatchsButton"] = "Смертей",
                ["AnimalsButton"] = "Животных",
                ["BradleysButton"] = "Танк",
                ["HelisButton"] = "Верт",
                ["SulfurButton"] = "Сера",
                ["StonesButton"] = "Камни",
                ["MetalButton"] = "Железа",
                ["WoodButton"] = "Дерева"
                
            }, this);
        }

        #endregion
        #region Data

        public class StoredData {
            public Dictionary<ulong, PlayerInfo> PlayerInfo = new Dictionary<ulong, PlayerInfo>();
        }
        public enum Animal
        {
            Bear,
            Boar,
            Chicken,
            Wolf,
            Horse,
            Stag
        }
        public class PlayerInfo {
            public string Name;
            public int kills;
            public int deaths;
            public int animalkills;
            public int bradleykills;
            public int helikills;
            public int wood;
            public int stones;
            public int irons;
            public int sulfur;
            public int time;
            public int Bear;
            public int Boar;
            public int Chicken;
            public int Wolf;
            public int Horse;
            public int Stag;
            public PlayerInfo() { }
        }
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);

        private void LoadData() {
            try {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            } catch (Exception e) {
                DataBase = new StoredData();
            }
        }
        void AddPlayer(BasePlayer player)
        {
            var data = new PlayerInfo {            
                Name = player.displayName,
                kills = 0,
                deaths = 0,
                animalkills = 0,
                bradleykills = 0,
                helikills = 0,
                wood = 0,
                stones = 0,
                irons = 0,
                sulfur = 0,
                time = 0,
                Bear = 0,
                Boar = 0,
                Chicken = 0,
                Wolf = 0,
                Horse = 0,
                Stag = 0
            };
            DataBase.PlayerInfo.Add(Convert.ToUInt64(player.userID), data);
            SaveData();
            
        }
        
        #endregion
        
        //Desigh by Kira
        #region PlayerStatsGui
        
        public void PlayerStatGuiCreate(BasePlayer player)
        {
            if(player == null)
                return;
            string AvatarImage = GetImage(player.UserIDString, 0);
            var Data = DataBase.PlayerInfo[player.userID];
            CuiHelper.DestroyUi(player, "PlayerStats");
            var PlayerStatsGui = new CuiElementContainer();
            var PlStatsGui = PlayerStatsGui.Add(new CuiPanel
            {
              Image = {Color = "0 0 0 0"},

             RectTransform = { AnchorMax = "1 1", AnchorMin = "0 0"}
            }, "Hud", "PlayerStats");
            
            PlayerStatsGui.Add(new CuiButton
            {
              Button = { Close = "PlayerStats",Color = "0 0 0 0",},
              RectTransform = { AnchorMax = "1 1",AnchorMin = "0 0"},
              Text = { Text = "",Color = "0 0 0 0"}
            },PlStatsGui);
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats",
                Name = "PlayerStats"+ "BackGround",
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Color = "0 0 0 0.50", 
                        Sprite = "assets/content/ui/ui.spashscreen.psd",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2202381 0.361979",
                        AnchorMax = "0.770864 0.8125"
                    }
                }
                
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.MainHeaderColor)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0.896152",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = config.ServerName,
                        FontSize = 28,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2690016 0.8855821",
                        AnchorMax = "0.9349946 1.016647"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0.5"},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.8401511 0.7185797",
                        AnchorMax = "0.9945088 0.8945763"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsProfileLineColor)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.8412383 0.8017578",
                        AnchorMax = "0.9966829 0.8123275"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsProfileLineColor)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.994765 0.3296121",
                        AnchorMax = "1.0002 0.9088357"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Profile",this),
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.8368888 0.8158216",
                        AnchorMax = "0.9955958 0.896152"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "PlayerImg",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Color = "1 1 1 1",
                        Png = AvatarImage
                        
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.8401511 0.4078285",
                        AnchorMax = "0.9934217 0.7249228"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
            Parent = "PlayerStats" + "PlayerImg",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = player.displayName,
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 14,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "1 1.246667"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsProfileLineColor)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.8401511 0.3296115",
                        AnchorMax = "0.9944085 0.4079273"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Time",this)+" "+GetPlaytimeClock(Data.time),
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 15,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.8401511 0.3296115",
                        AnchorMax = "0.9944085 0.4079273"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "GatherHeader",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsHeaderColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.5626061 0.8033028",
                        AnchorMax = "0.8289275 0.8794051"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "GatherHeader",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsHeaderColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 -0.155246",
                        AnchorMax = "1 0"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "GatherHeader",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsHeaderColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.979881 -0.155246",
                        AnchorMax = "1 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "GatherHeader",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("GatherHeader",this),
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            });

            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Sulfur",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.5626054 0.7081748",
                        AnchorMax = "0.8017511 0.7779353"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Sulfur",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.97"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Sulfur",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Sulfur",this)+": "+Data.sulfur+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Stones",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.56260444 0.6278445",
                        AnchorMax = "0.80175 0.697605"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Stones",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Stones",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Stones",this)+": "+Data.stones+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Metal",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.5626044 0.5475143",
                        AnchorMax = "0.80175 0.6172748"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Metal",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent() 
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Metal",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Metal",this)+": "+Data.irons+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Wood",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.5626044 0.467184",
                        AnchorMax = "0.80175 0.5369444"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Wood",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Wood",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Wood",this)+": "+Data.wood+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "KillsHeader",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsHeaderColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2807915 0.8033028",
                        AnchorMax = "0.5471129 0.8794051"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "KillsHeader",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsHeaderColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 -0.155246",
                        AnchorMax = "1 0"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "KillsHeader",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsHeaderColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.979881 -0.155246",
                        AnchorMax = "1 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "KillsHeader",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("KillsHeader",this),
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.016042 0.038466",
                        AnchorMax = "0.962574 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Players",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2807903 0.7081748",
                        AnchorMax = "0.519936 0.7779353"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Players",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.97"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Players",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Kills",this)+": "+Data.kills+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Death",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2807903 0.6278445",
                        AnchorMax = "0.519936 0.697605"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Death",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Death",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Deatchs",this)+": "+Data.deaths+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Animals",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2807903 0.5475143",
                        AnchorMax = "0.519936 0.6172737"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Animals",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Animals",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Animals",this)+": "+Data.animalkills+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Bradley",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2807903 0.467184",
                        AnchorMax = "0.519936 0.5369449"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Bradley",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Bradley",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Bradleys",this)+": "+Data.bradleykills+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Heli",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2807903 0.3868537",
                        AnchorMax = "0.519936 0.4566151"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Heli",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Heli",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Helis",this)+": "+Data.helikills+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            
           PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "AnimalsKillHeader",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsHeaderColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.002174052 0.8033028",
                        AnchorMax = "0.2684954 0.8794054"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "AnimalsKillHeader",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsHeaderColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 -0.155247",
                        AnchorMax = "1 0"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "AnimalsKillHeader",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsHeaderColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.979881 -0.155246",
                        AnchorMax = "1 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "AnimalsKillHeader",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("AnimalKillHeader",this),
                        FontSize = 17,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.016042 0.038466",
                        AnchorMax = "0.962574 1.03847"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Bears",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.002174052 0.7081748",
                        AnchorMax = "0.2413198 0.7779353"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Bears",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.97"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Bears",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Bears",this)+": "+Data.Bear+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 0.97"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Horse",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.002174052 0.6278445",
                        AnchorMax = "0.2413224 0.697605"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Horse",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Horse",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Horses",this)+": "+Data.Horse+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Deer",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.002174052 0.5475143",
                        AnchorMax = "0.2413224 0.6172748"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Deer",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Deer",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Stug",this)+": "+Data.Stag+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Boar",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.002174052 0.467184",
                        AnchorMax = "0.2413224 0.536945"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Boar",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Boar",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Boar",this)+": "+Data.Boar+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Chicken",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.002174052 0.3868537",
                        AnchorMax = "0.2413224 0.4566142"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Chicken",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.95"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Chicken",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Chicken",this)+": "+Data.Chicken+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Name = "PlayerStats" + "Wolf",
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor1)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.002174052 0.3065234",
                        AnchorMax = "0.2413224 0.3762839"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {

                Parent = "PlayerStats" + "Wolf",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.PlayerStatsBlocColor2)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1.027175 0.97"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "Wolf",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = lang.GetMessage("Wolf",this)+": "+Data.Wolf+"",
                        Color = HexToRustFormat(config.PlayerStatsTextColor),
                        FontSize = 18,
                        Align = TextAnchor.UpperLeft
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.033148 0",
                        AnchorMax = "1 1"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiElement
            {
                Parent = "PlayerStats" + "BackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.MainHeaderButtonEdgeColor)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2492921 0.896152",
                        AnchorMax = "0.2542441 0.9997358"
                    }
                }
            });
            PlayerStatsGui.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat(config.MainHeaderButtonColor),Command = "serverstats_gui"},
                    RectTransform = { AnchorMax = "0.248104 1", AnchorMin = "0 0.896152"},
                    Text = { Text = lang.GetMessage("ServerStatsButton",this), Color = HexToRustFormat(config.PlayerStatsTextColor), FontSize = 16,Align = TextAnchor.MiddleCenter}
            },"PlayerStats"+ "BackGround");
            PlayerStatsGui.Add(new CuiButton
            {
                Button = { Color = HexToRustFormat(config.MainHeaderButtonCloseColor),Close = "PlayerStats"},
                RectTransform = { AnchorMax = "1 1", AnchorMin = "0.9499407 0.9257474"},
                Text = { Text = "X",FontSize = 20,Align = TextAnchor.MiddleCenter}
            },"PlayerStats"+ "BackGround");
            CuiHelper.AddUi(player, PlayerStatsGui);
        }
        
        #endregion
        //Desigh by Kira
        #region ServerTopGui
     
        public void ServerStatGuiCreate(BasePlayer player)
        {
            if (player == null)
                return;
            CuiHelper.DestroyUi(player, "ServerStats");
            var ServerStatsGui = new CuiElementContainer();
            var SrvStatsGui = ServerStatsGui.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0"},

                RectTransform = { AnchorMax = "1 1", AnchorMin = "0 0"}
            }, "Hud", "ServerStats");
            ServerStatsGui.Add(new CuiButton
            {
                Button = { Close = "ServerStats",Color = "0 0 0 0"},
                RectTransform = { AnchorMax = "1 1",AnchorMin = "0 0"}

            },SrvStatsGui);
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStats",
                Name = "ServerStatsBackGround",
                Components =
                {
                    new CuiRawImageComponent()
                    {
                        Color = "0 0 0 0.50", 
                        Sprite = "assets/content/ui/ui.spashscreen.psd",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2202381 0.361979",
                        AnchorMax = "0.770864 0.8125"
                    }
                }
                
            });
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.MainHeaderColor)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0.896152",
                        AnchorMax = "1 1"
                    }
                }
            });
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = config.ServerName,
                        FontSize = 28,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2690016 0.8855821",
                        AnchorMax = "0.9349946 1.016647"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat(config.MainHeaderButtonColor),Command = "PlayerStats_gui"},
                    RectTransform = {AnchorMax = "0.248104 1", AnchorMin = "0 0.896152"},
                    Text = { Text = lang.GetMessage("MyStatsButton",this), Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiButton
                {
                    Button = { Color = HexToRustFormat(config.MainHeaderButtonCloseColor),Close = "ServerStats"},
                    RectTransform = { AnchorMax = "1 1", AnchorMin = "0.9499407 0.9257474"},
                    Text = { Text = "X",FontSize = 20,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.MainHeaderButtonEdgeColor)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2492921 0.896152",
                        AnchorMax = "0.2542441 0.9997358"
                    }
                }
            });
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.ServerStatsButtonsUpLine)},
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0 0.8803822",
                        AnchorMax = "1 0.8889621"
                    }
                }
            });
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = HexToRustFormat(config.ServerStatsButtonsUpLine)},
                    new CuiRectTransformComponent()
                    {
                         AnchorMin = "0 0.7984205",
                        AnchorMax = "1 0.8070003"
                    }
                }
            });
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.004126973 0.8053755",
                        AnchorMax = "0.1096696 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                   Button =
                   {
                       Color = HexToRustFormat(config.ServerStatsButtonsColor),
                       Command = "topkills"
                   },
                   RectTransform = { 
                       AnchorMin = "0.004126973 0.8053755",
                       AnchorMax = "0.1096696 0.8816238"
                       },
                   Text = { Text = lang.GetMessage("KillsButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.1141795 0.8053755",
                        AnchorMax = "0.2281555 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsColor),
                        Command = "topDeaths"
                    },
                    RectTransform = { 
                        AnchorMin = "0.1141795 0.8053756",
                        AnchorMax = "0.2281555 0.8816238"
                    },
                    Text = { Text = lang.GetMessage("DeatchsButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter},
                    
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.4955396 0.8053756",
                        AnchorMax = "0.6097016 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
            {
                Button =
                {
                    Color = HexToRustFormat(config.ServerStatsButtonsColor),
                    Command = "topAnimalkills"
                },
                RectTransform = { 
                    AnchorMin = "0.4955396 0.8053756",
                    AnchorMax = "0.6097016 0.8816238"
                },
                Text = { Text = lang.GetMessage("AnimalsButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
            },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.3417817 0.8053756",
                        AnchorMax = "0.4096888 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsColor),
                        Command = "topTanks"
                    },
                    RectTransform = { 
                        AnchorMin = "0.3417817 0.8053756",
                        AnchorMax = "0.4096888 0.8816238"
                    },
                    Text = { Text = lang.GetMessage("BradleysButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.4137693 0.8053756",
                        AnchorMax = "0.4912157 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsColor),
                        Command = "topHeli"
                    },
                    RectTransform = { 
                        AnchorMin = "0.4137693 0.8053756",
                        AnchorMax = "0.4912157 0.8816238"
                    },
                    Text = { Text = lang.GetMessage("HelisButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2328372 0.8053756",
                        AnchorMax = "0.3379452 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsColor),
                        Command = "topServerTime"
                    },
                    RectTransform = { 
                        AnchorMin = "0.2328372 0.8053756",
                        AnchorMax = "0.3379452 0.8816238"
                    },
                    Text = { Text = lang.GetMessage("TimeButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.6033972 0.8053756",
                        AnchorMax = "0.7042729 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsColor),
                        Command = "topWood"
                    },
                    RectTransform = { 
                        AnchorMin = "0.6033972 0.8053756",
                        AnchorMax = "0.7042729 0.8816238"
                    },
                    Text = { Text = lang.GetMessage("WoodButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.816939 0.8053756",
                        AnchorMax = "0.9216781 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsColor),
                        Command = "topStones"
                    },
                    RectTransform = { 
                        AnchorMin = "0.816939 0.8053756",
                        AnchorMax = "0.9216781 0.8816238"
                    },
                    Text = { Text = lang.GetMessage("StonesButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.7090817 0.8053756",
                        AnchorMax = "0.8129755 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsColor),
                        Command = "topMetal"
                    },
                    RectTransform = { 
                        AnchorMin = "0.7090817 0.8053756",
                        AnchorMax = "0.8129755 0.8816238"
                    },
                    Text = { Text = lang.GetMessage("MetalButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
            ServerStatsGui.Add(new CuiElement
            {
                Parent = "ServerStatsBackGround",
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0"},
                    new CuiOutlineComponent()
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsEdge),
                        Distance = "3 0"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.9260262 0.8053756",
                        AnchorMax = "0.9951354 0.8816238"
                    }
                }
            });
            ServerStatsGui.Add(new CuiButton
                {
                    Button =
                    {
                        Color = HexToRustFormat(config.ServerStatsButtonsColor),
                        Command = "topSulfur"
                    },
                    RectTransform = { 
                        AnchorMin = "0.9260262 0.8053756",
                        AnchorMax = "0.9951354 0.8816238"
                    },
                    Text = { Text = lang.GetMessage("SulfurButton",this),Color = HexToRustFormat(config.ServerStatsTextColor),FontSize = 16,Align = TextAnchor.MiddleCenter}
                },"ServerStatsBackGround");
           

            CuiHelper.AddUi(player, ServerStatsGui);
            
        }
private string TopGui = "[{\"name\":\"BoxRating1\",\"parent\":\"ServerStatsBackGround\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"-0.004468113 0.1092254\",\"anchormax\":\"0.46855 0.7226822\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number1\",\"parent\":\"BoxRating1\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01149032 0.8034965\",\"anchormax\":\"0.9885097 0.9825175\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number1\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 0\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number1\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.000777 0\",\"anchormax\":\"0.071113 1.000025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"1\",\"fontSize\":28,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.012293 0.050013\",\"anchormax\":\"0.061523 0.975017\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick0} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -3.1E-05\",\"anchormax\":\"0.5 0.9999701\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number1\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value0}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.4999999 0.07502806\",\"anchormax\":\"0.9800001 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number2\",\"parent\":\"BoxRating1\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01149032 0.606993\",\"anchormax\":\"0.9885097 0.786014\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 1E-06\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.000777 2.5E-05\",\"anchormax\":\"0.071113 1.000025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"2\",\"fontSize\":27,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.012293 0.025012\",\"anchormax\":\"0.061523 0.950012\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick1} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -2.4E-05\",\"anchormax\":\"0.5 0.999976\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number2\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value1}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.075028\",\"anchormax\":\"0.98 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number3\",\"parent\":\"BoxRating1\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01149032 0.4104896\",\"anchormax\":\"0.9885097 0.5895105\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 1E-06\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number3\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.8062065 0.8062065 0.8062065 1\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.000777 2.6E-05\",\"anchormax\":\"0.071113 1.000026\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"3\",\"fontSize\":26,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.012293 0.025007\",\"anchormax\":\"0.061523 0.950008\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick2} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -0.025031\",\"anchormax\":\"0.5 0.97497\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number3\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value2}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.075028\",\"anchormax\":\"0.98 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number4\",\"parent\":\"BoxRating1\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01149032 0.213986\",\"anchormax\":\"0.9885097 0.393007\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number4\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 1E-06\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number4\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"1E-06 2.5E-05\",\"anchormax\":\"0.070553 1.000025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"4\",\"fontSize\":24,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009225 0.050008\",\"anchormax\":\"0.058455 0.975009\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick3} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -3E-05\",\"anchormax\":\"0.5 0.999971\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number4\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value3}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.075028\",\"anchormax\":\"0.98 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number5\",\"parent\":\"BoxRating1\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01149032 0.01748264\",\"anchormax\":\"0.9885097 0.1965036\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number5\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 1E-06\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number5\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"1E-06 2.5E-05\",\"anchormax\":\"0.070553 1.000025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"5\",\"fontSize\":22,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009199 0.025007\",\"anchormax\":\"0.058279 0.950007\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick4} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -3.1E-05\",\"anchormax\":\"0.5 0.99997\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number5\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value4}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.075028\",\"anchormax\":\"0.98 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BoxRating2\",\"parent\":\"ServerStatsBackGround\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5847 0.1092254\",\"anchormax\":\"1.004292 0.7226822\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number6\",\"parent\":\"BoxRating2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01295337 0.8034965\",\"anchormax\":\"0.9870467 0.9825175\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number6\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 1E-06\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number6\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"1E-06 2.5E-05\",\"anchormax\":\"0.070553 1.000025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"6\",\"fontSize\":22,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.012231 0.050007\",\"anchormax\":\"0.061161 0.975007\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick5} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -3.1E-05\",\"anchormax\":\"0.5 0.99997\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number6\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value5}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.075028\",\"anchormax\":\"0.9803833 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number7\",\"parent\":\"BoxRating2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01295337 0.606993\",\"anchormax\":\"0.9870467 0.786014\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number7\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 1E-06\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number7\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 2.5E-05\",\"anchormax\":\"0.070551 1.000025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number7\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"7\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.013058 0.050007\",\"anchormax\":\"0.062138 0.975007\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number7\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick6} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -3.1E-05\",\"anchormax\":\"0.5 0.99997\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number7\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value6}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.075028\",\"anchormax\":\"0.9803833 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number8\",\"parent\":\"BoxRating2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01295337 0.4104896\",\"anchormax\":\"0.9870467 0.5895105\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number8\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 1E-06\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number8\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"1E-06 2.5E-05\",\"anchormax\":\"0.070553 1.000025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"8\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.012231 0.050007\",\"anchormax\":\"0.061161 0.975007\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick7} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -3.1E-05\",\"anchormax\":\"0.5 0.99997\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number8\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value7}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.075028\",\"anchormax\":\"0.9803833 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number9\",\"parent\":\"BoxRating2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01295337 0.213986\",\"anchormax\":\"0.9870467 0.393007\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number9\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 1E-06\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number9\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"1E-06 2.5E-05\",\"anchormax\":\"0.070553 1.000025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"9\",\"fontSize\":16,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.012231 0.050007\",\"anchormax\":\"0.061161 0.975007\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick8} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -3.1E-05\",\"anchormax\":\"0.5 0.99997\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number9\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value8}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.075028\",\"anchormax\":\"0.9803833 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Number10\",\"parent\":\"BoxRating2\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01295337 0.01748264\",\"anchormax\":\"0.9870467 0.1965036\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Ramka\",\"parent\":\"Number10\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopEdgeColor}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.984665 1E-06\",\"anchormax\":\"1.000002 1.000001\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BGTXT\",\"parent\":\"Number10\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{TopColor2}\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.8900158 0.8900158 0.8900158 1\",\"distance\":\"1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"1E-06 2.5E-05\",\"anchormax\":\"0.070553 1.000025\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"TXT\",\"parent\":\"Number10\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"10\",\"fontSize\":13,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.1176471 0.1176471 0.1176471 0.4253244\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.009173 0.025007\",\"anchormax\":\"0.058103 0.950007\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Nick\",\"parent\":\"Number10\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Nick9} :\",\"fontSize\":20,\"align\":\"MiddleLeft\",\"color\":\"{TopNickText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.07811538 -3.1E-05\",\"anchormax\":\"0.5 0.99997\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Value\",\"parent\":\"Number10\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Value9}\",\"fontSize\":20,\"align\":\"MiddleRight\",\"color\":\"{TopValueText}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.075028\",\"anchormax\":\"0.9803833 0.976303\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}]";
        #endregion

        #region Commands
        void CmdStats(IPlayer player, string command, string[] args)
        {

            BasePlayer pl = player.Object as BasePlayer;
            int timeplayed = Epoch.Current -  timelist[pl.userID];
            timelist[pl.userID] = Epoch.Current;
            DataBase.PlayerInfo[pl.userID].time += timeplayed;
            PlayerStatGuiCreate(pl);
        }
        [ConsoleCommand("serverstats_gui")]
        void CmdServerStatsGui(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "PlayerStats");
            
           ServerStatGuiCreate(args.Player());  
     
        }  
        [ConsoleCommand("PlayerStats_gui")]  
        void CmdPlayerStatsGui(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "ServerStats");
            
            PlayerStatGuiCreate(args.Player());
  
        }
        [ConsoleCommand("topkills")]
        void CmdTopKills(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            
            var top = Dicter.OrderByDescending(pair => pair.Value.kills);
            string Topg = TopGui;
            int i = 0;
            foreach (var Finder in top)
            {
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i + "}", Finder.Value.kills.ToString()).Replace("{Nick" + i + "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }
                i++;
            }
            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        [ConsoleCommand("topDeaths")]
        void CmdTopDeaths(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            var top = Dicter.OrderByDescending(pair => pair.Value.deaths);
            string Topg = TopGui;
            int i = 0;
            foreach (var Finder in top)
            {
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i+ "}", Finder.Value.deaths.ToString()).Replace("{Nick" + i+ "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }
                i++;
            }
            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        [ConsoleCommand("topTanks")]
        void CmdTopTanks(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            var top = Dicter.OrderByDescending(pair => pair.Value.bradleykills);
            string Topg = TopGui;
            int i = 0;
            foreach (var Finder in top)
            {
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i+ "}", Finder.Value.bradleykills.ToString()).Replace("{Nick" + i+ "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }
                i++;
            }
            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        [ConsoleCommand("topHeli")]
        void CmdTopHeli(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            var top = Dicter.OrderByDescending(pair => pair.Value.helikills);
            string Topg = TopGui;
            int i = 0;
            foreach (var Finder in top)
            {
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i+ "}", Finder.Value.helikills.ToString()).Replace("{Nick" + i+ "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }

                i++;
            }

            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        [ConsoleCommand("topAnimalkills")]
        void CmdTopAnimalKills(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            var top = Dicter.OrderByDescending(pair => pair.Value.animalkills);
            string Topg = TopGui;
            int i = 0;
            foreach (var Finder in top)
            {
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i+ "}", Finder.Value.animalkills.ToString()).Replace("{Nick" + i+ "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }
                i++;
            }
            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        [ConsoleCommand("topServerTime")]
        void CmdTopServerTime(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            var top = Dicter.OrderByDescending(pair => pair.Value.time);
            string Topg = TopGui;

            int i = 0;
            foreach (var Finder in top)
            {
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i+ "}", GetPlaytimeClock(Finder.Value.time)).Replace("{Nick" + i+ "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }
                i++;
            }
            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        [ConsoleCommand("topWood")]
        void CmdTopWood(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            var top = Dicter.OrderByDescending(pair => pair.Value.wood);
            string Topg = TopGui;
            int i = 0;
            foreach (var Finder in top)
            {
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i+ "}", Finder.Value.wood.ToString()).Replace("{Nick" + i+ "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }
                i++;
            }
            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        [ConsoleCommand("topStones")]
        void CmdTopStones(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            var top = Dicter.OrderByDescending(pair => pair.Value.stones);
            string Topg = TopGui;
            int i = 0;
            foreach (var Finder in top)
            {
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i+ "}", Finder.Value.stones.ToString()).Replace("{Nick" + i+ "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }

                i++;
            }
            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        [ConsoleCommand("topMetal")]
        void CmdTopMetal(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            var top = Dicter.OrderByDescending(pair => pair.Value.irons);
            string Topg = TopGui;
            int i = 0;
            foreach (var Finder in top)
            {
                
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i+ "}", Finder.Value.irons.ToString()).Replace("{Nick" + i+ "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }
                i++;
            }
            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        [ConsoleCommand("topSulfur")]
        void CmdTopSulfur(ConsoleSystem.Arg args)
        {
            CuiHelper.DestroyUi(args.Player(), "BoxRating1");
            CuiHelper.DestroyUi(args.Player(), "BoxRating2");
            var Dicter = DataBase.PlayerInfo;
            var top = Dicter.OrderByDescending(pair => pair.Value.sulfur);
            string Topg = TopGui;
            int i = 0;
            foreach (var Finder in top)
            {
                if (i < 10)
                {
                    Topg = Topg.Replace("{Value" + i+ "}", Finder.Value.sulfur.ToString()).Replace("{Nick" + i+ "}", Finder.Value.Name)
                        .Replace("{TopColor1}", HexToRustFormat(config.ServerStatsTopColor1))
                        .Replace("{TopColor2}", HexToRustFormat(config.ServerStatsTopColor2))
                        .Replace("{TopNickText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopValueText}", HexToRustFormat(config.ServerStatsTopTextColor1))
                        .Replace("{TopEdgeColor}", HexToRustFormat(config.ServerStatsTopEdgeColor));
                    
                }
                i++;
            }
            CuiHelper.AddUi(args.Player(), Topg);
 
        }
        
        #endregion
       
        #region  Hooks
        Dictionary<ulong, int> timelist = new Dictionary<ulong, int>();
        private void OnPlayerInit(BasePlayer player)
        {
            if (!DataBase.PlayerInfo.ContainsKey(player.userID)) AddPlayer(player);
            if (DataBase.PlayerInfo[player.userID].Name != player.displayName) DataBase.PlayerInfo[player.userID].Name = player.displayName;
               timelist.Add(player.userID,Epoch.Current);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            int timeplayed = Epoch.Current -  timelist[player.userID];
            DataBase.PlayerInfo[player.userID].time += timeplayed;
            timelist.Remove(player.userID);
        }
        void Init() 
        { 
            LoadConfig();
            AddCovalenceCommand(config.StatsCmd,"CmdStats","Statistics.use");
        } 
        private void OnServerInitialized() {
            LoadData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                timelist.Add(player.userID,Epoch.Current);
            }
        }
        private void Unload()
        {
            SaveData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                int timeplayed = Epoch.Current -  timelist[player.userID];
                DataBase.PlayerInfo[player.userID].time += timeplayed;
                timelist.Remove(player.userID);
            }
        }
        private void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || player.IsNpc)
                return;
            CheckDb(player);
            var Dictinory = DataBase.PlayerInfo[player.userID];
            if (info.damageTypes.GetMajorityDamageType() == DamageType.Suicide)
                Dictinory.deaths++;
            else
            {
                Dictinory.deaths++;
                var attacker = info.InitiatorPlayer;
                if (attacker == null || attacker.IsNpc)
                    return;
                CheckDb(attacker);
                var AttackerDictinory = DataBase.PlayerInfo[attacker.userID];
                AttackerDictinory.kills++;
            }
         
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                lastDamageName = info.Initiator.ToPlayer().userID;
            if (entity is BradleyAPC && info.Initiator is BasePlayer)
                lastDamageName = info.Initiator.ToPlayer().userID;
           
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitinfo)
        {
            
            if (hitinfo?.Initiator is BasePlayer)
            {
                var player = hitinfo.Initiator as BasePlayer;
               
                if (player.userID.IsSteamId() && !(player is NPCPlayer) && !(player is HTNPlayer))
                {
                    CheckDb(player);
                    var Dictinory = DataBase.PlayerInfo[player.userID];
                    if (entity.name.Contains("agents/"))
                        switch (entity.ShortPrefabName)
                        {
                            case "bear":
                                Dictinory.Bear++;
                                Dictinory.animalkills++;
                                break;
                            case "boar":
                                Dictinory.Boar++;
                                Dictinory.animalkills++;
                                break;
                            case "chicken":
                                Dictinory.Chicken++;
                                Dictinory.animalkills++;
                                break;
                            case "horse":
                                Dictinory.Horse++;
                                Dictinory.animalkills++;
                                break;
                            case "stag":
                                Dictinory.Stag++;
                                Dictinory.animalkills++;
                                break;
                            case "wolf":
                                Dictinory.Wolf++;
                                Dictinory.animalkills++;
                                break;
                        }
                }
            }

            if (entity is BradleyAPC)
            {
                BasePlayer player;
                player = BasePlayer.FindByID(lastDamageName);
                CheckDb(player);
                var Dictinory = DataBase.PlayerInfo[player.userID];
                if (player != null)
                {
                    Dictinory.bradleykills++;
                }
            }
           if (entity is BaseHelicopter)
            {
                BasePlayer player;
                player = BasePlayer.FindByID(lastDamageName);
                CheckDb(player);
                var Dictinory = DataBase.PlayerInfo[player.userID];
                if (player != null)
                {
                    Dictinory.helikills++;
                }
            }
        }
        
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            CheckDb(player);
            var Dictinory = DataBase.PlayerInfo[player.userID];
            switch (item.info.shortname)
            {
                case "stones":
                    Dictinory.stones +=item.amount;
                    break;
                case "wood":
                    Dictinory.wood +=item.amount;
                    break;
                case "metal.ore":
                    Dictinory.irons +=item.amount;
                    break;
                case "sulfur.ore":
                    Dictinory.sulfur +=item.amount;
                    break;
            }
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            CheckDb(player);
            var Dictinory = DataBase.PlayerInfo[player.userID];
            switch (item.info.shortname)
            {
                case "stones":
                    Dictinory.stones +=item.amount;
                    break;
                case "wood":
                    Dictinory.wood +=item.amount;
                    break;
                case "metal.ore":
                    Dictinory.irons +=item.amount;
                    break;
                case "sulfur.ore":
                    Dictinory.sulfur +=item.amount;
                    break;
            }
        }
        
        #endregion
        
        #region Utils

        void CheckDb(BasePlayer player)
        {
            if (!DataBase.PlayerInfo.ContainsKey(player.userID)) AddPlayer(player);
        }
        
        private string GetPlaytimeClock(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time); 
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
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

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion
    }
}