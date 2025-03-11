using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Stats", "Я и Я", "1.0.0")]
    public class Stats : RustPlugin
    {
        #region Fields

        private string Layer = "UI_Stats";
        private const int Count = 9;

        private readonly string[] _statsName = { "Убийств", "Смертей", "Коэффициент", "Выстрелов сделано", "Попаданий", "Попаданий в голову", "Коэффицент попаданий в голову", "Коэффицент попаданий", "Добыто ресурсов" };

        private Dictionary<string, string> tops = new Dictionary<string, string>()
        {
            {"Убийств", "Убийцы"},
            {"Смертей", "Суицидники"},
            {"Добычи", "Добытчики"},
            {"Выстрелов", "Стрелки"},
            {"Попаданий в голову", "Хедшотеры"},
            {"Попаданий", "Стрелки"},
            {"в голову", "Хедшотеры"},
            {"попаданий", "Стрелки"}
        };
       
        #endregion

        #region Commands

        private void chatCmdStats(BasePlayer player, string command, string[] args)
        {
            ShowStatistic(player, player.userID);
        }

        private void consoleCmdStats(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection?.player as BasePlayer;

            if (player == null) return;

            switch (arg.GetString(0))
            {
                case "close":
                {
                    CuiHelper.DestroyUi(player, Layer);
                    break;
                }
            }
        }
 
        #endregion
        
        #region Hooks
        
        void OnServerInitialized()
        {
            LoadData();
            
            SaveData();
            
            cmd.AddChatCommand("stats", this, "chatCmdStats");
            //cmd.AddChatCommand("top", this, "chatCmdTop");
            //cmd.AddConsoleCommand("UI_Stats", this, "consoleCmdStats");
            
            foreach (var p in BasePlayer.activePlayerList) OnPlayerConnected(p);
            AddImage("https://i.ibb.co/3dkM0SX/frame.png","St_frame_img");
            AddImage("https://i.ibb.co/ykW7XrG/exit.png","Stat_exit_img");
        }

        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return true;

            if (storedData.ContainsKey(player.userID)) storedData[player.userID][StatsType.Gather] += item.amount;

            return null;
        }
        
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            var player = entity.ToPlayer();

            if (player == null) return true;

            if (storedData.ContainsKey(player.userID)) storedData[player.userID][StatsType.Gather] += item.amount;

            return null;
        }
        
        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (info == null) return null;
            if (player == null) return null;
            if (player.IsNpc) return null;
            if (info.InitiatorPlayer == null) return null; 
            if (info.InitiatorPlayer.IsNpc) return null;
            
            if (info.InitiatorPlayer != null)
            {
                var killer = info.InitiatorPlayer;

                if (killer != player) 
                { 
                    if (storedData.ContainsKey(killer.userID)) storedData[killer.userID][StatsType.Kill]++;
                }
                if(storedData.ContainsKey(player.userID)) storedData[player.userID][StatsType.Death]++;
            }
             
            return null; 
        }
        
        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            if(storedData.ContainsKey(player.userID)) storedData[player.userID][StatsType.Shot]++;
        }
        
        void OnPlayerConnected(BasePlayer player)
        {
            if(!storedData.ContainsKey(player.userID))
                storedData.Add(player.userID, new Dictionary<StatsType, int>
                {
                    {StatsType.Kill, 0},
                    {StatsType.Death, 0},
                    {StatsType.Shot, 0},
                    {StatsType.Hit, 0},
                    {StatsType.HitHead, 0},
                    {StatsType.Gather, 0}
                });
        }
        
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BasePlayer)
            {
                var attacker = info.InitiatorPlayer;  
                
                if (attacker != null)
                {
                    if (!attacker.IsNpc)
                    {
                        if (info.isHeadshot)
                        {
                            storedData[attacker.userID][StatsType.HitHead]++;
                        }
                        storedData[attacker.userID][StatsType.Hit]++;
                    }
                    else return null; 
                }
            }
            
            return null;
        }
        
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
            
            SaveData();
        }

        #endregion

        #region Methods

        [HookMethod("ApiGetName")]
        public string ApiGetName(ulong steamID)
        {
            return covalence.Players.FindPlayerById(steamID.ToString())?.Name ?? "UNKNOWN";
        }

        private void ShowStatistic(BasePlayer player, ulong userID)
        {
            var statPlayer = storedData[userID];
            var name = covalence.Players.FindPlayerById(userID.ToString())?.Name.Replace('"', ' ') ?? player.UserIDString;
            if (name.Length > 16) name = name.Substring(0, 16) + "..";

            var container = new CuiElementContainer();

            CuiHelper.DestroyUi(player, Layer);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                    {
                        FadeIn = 0.2f,
                        Sprite = "assets/content/ui/ui.background.transparent.radial.psd",
                        Color = "0 0 0 1"
                    }
            }, "Overlay", Layer);
            container.Add(new CuiPanel
            {
                Image =
                    {
                        FadeIn = 0.2f,
                        Color = "0.2 0.2 0.17 0.7",
                        Material = "assets/content/ui/uibackgroundblur.mat"
                    }
            }, Layer);

            container.Add(new CuiLabel
            {
                Text = { Text = "СТАТИСТИКА", Align = TextAnchor.UpperCenter, FontSize = 40, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.3 1", AnchorMax = "0.7 1", OffsetMin = "0 -150", OffsetMax = "0 -86.6" }
            }, Layer);
            container.Add(new CuiLabel
            {
                Text = { Text = "Тут можно посмотреть статистику игрока", Align = TextAnchor.UpperCenter, FontSize = 18, Font = "robotocondensed-regular.ttf" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 -150", OffsetMax = "0 -128" }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        GetImageComponent("https://i.ibb.co/ykW7XrG/exit.png","Stat_exit_img"),
                        new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-73.9 20", OffsetMax = "-28.6 80"},
                    }
            });
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                    {
                        new CuiImageComponent {Color = "0.33 0.87 0.59 0.6"},
                        new CuiRectTransformComponent {AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 25.2"}
                    }
            });
            
            container.Add(new CuiButton
            {
                Button =
                        {
                            Color = "0 0 0 0",
                            Command = "UI_Stats close",
                            Close = Layer
                        },
                Text = { Text = "Покинуть страницу", Align = TextAnchor.UpperCenter, FontSize = 18 },
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-291.3 22.6", OffsetMax = "-108 49.2" },
            }, Layer);
            container.Add(new CuiButton
            {
                Button =
                {
                    Color = "0 0 0 0",
                    Command = "UI_Stats close",
                    Close = Layer
                },
                Text = { Text = "" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
            }, Layer);

            container.Add(new CuiPanel
            {
                Image = null,
                RawImage = GetAvatarImageComponent(userID),
                RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-395.3 -96", OffsetMax = "-155.3 144"}
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    GetImageComponent("https://i.ibb.co/3dkM0SX/frame.png","St_frame_img", "0.33 0.87 0.59 0.3"),
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-395.3 -143.9",
                        OffsetMax = "-155.3 -100.6"
                    }
                }
            });

            container.Add(new CuiLabel
            {
                Text = { Text = name, Align = TextAnchor.MiddleCenter, FontSize = 26, Font = "robotocondensed-bold.ttf" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-395.3 -143.9", OffsetMax = "-155.3 -100.6" }
            }, Layer);

            var stats = new[]
            {
                statPlayer[StatsType.Kill].ToString(), statPlayer[StatsType.Death].ToString(),
                statPlayer[StatsType.Death]==0?"0":(statPlayer[StatsType.Kill] / (float) statPlayer[StatsType.Death]).ToString("N2"),
                statPlayer[StatsType.Shot].ToString(), statPlayer[StatsType.Hit].ToString(),
                statPlayer[StatsType.HitHead].ToString(),
                statPlayer[StatsType.Shot]==0?"0":(statPlayer[StatsType.Hit] / (float) statPlayer[StatsType.Shot]).ToString("N2"),
                statPlayer[StatsType.Shot]==0?"0":(statPlayer[StatsType.HitHead] / (float) statPlayer[StatsType.Shot]).ToString("N2"),
                statPlayer[StatsType.Gather].ToString()
            };
            var posY = 144f;
            var sizeY = 32f;

            for (var i = 0; i < 9; i++)
            {
                container.Add(new CuiElement
                {
                    Name = Layer + $".Stats{i}",
                    Parent = Layer,
                    Components =
                    {
                        new CuiImageComponent {Color = ((i + 2) % 2 == 0 ? "0.33 0.87 0.59 0.6" : "0 0 0 0")},
                        new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-146 {posY - sizeY}", OffsetMax = $"394.6 {posY}"}
                    }
                });
                container.Add(new CuiLabel
                {
                    Text = { Text = _statsName[i], Align = TextAnchor.MiddleLeft, FontSize = 24, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 0" }
                }, Layer + $".Stats{i}");
                container.Add(new CuiLabel
                {
                    Text = { Text = stats[i], Align = TextAnchor.MiddleRight, FontSize = 24, Font = "robotocondensed-bold.ttf" },
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "-5 0" }
                }, Layer + $".Stats{i}");
                posY -= sizeY;
            }
            CuiHelper.AddUi(player, container);
        }
        
        private static string HexToRGB(string hex)
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

        #region Data

        private enum StatsType
        {
            Kill,
            Death,
            Shot,
            Hit,
            HitHead,
            Gather
        }

        void SaveData()
        {
            StatData.WriteObject(storedData);
        }

        void LoadData()
        {
            StatData = Interface.Oxide.DataFileSystem.GetFile("Stats/stats");
            try
            {
                storedData = StatData.ReadObject<Dictionary<ulong, Dictionary<StatsType, int>>>();
            }
            catch
            {
                storedData = new Dictionary<ulong, Dictionary<StatsType, int>>();
            } 
        }

        Dictionary<ulong, Dictionary<StatsType, int>> storedData;
        private DynamicConfigFile StatData;

        #endregion

		public CuiRawImageComponent GetAvatarImageComponent(ulong user_id, string color = "1.0 1.0 1.0 1.0"){
			
			if (plugins.Find("ImageLoader")) return plugins.Find("ImageLoader").Call("BuildAvatarImageComponent",user_id) as CuiRawImageComponent;
			if (plugins.Find("ImageLibrary")) {
				return new CuiRawImageComponent { Png = (string)plugins.Find("ImageLibrary").Call("GetImage", user_id.ToString()), Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga" };
			}
			return new CuiRawImageComponent {Url = "https://image.flaticon.com/icons/png/512/37/37943.png", Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
		}
		public CuiRawImageComponent GetImageComponent(string url, string shortName="", string color = "1.0 1.0 1.0 1.0"){
			
			if (plugins.Find("ImageLoader")) return plugins.Find("ImageLoader").Call("BuildImageComponent",url) as CuiRawImageComponent;
			if (plugins.Find("ImageLibrary")) {
				if (!string.IsNullOrEmpty(shortName)) url = shortName;
				return new CuiRawImageComponent { Png = (string)plugins.Find("ImageLibrary").Call("GetImage", url), Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga" };
			}
			return new CuiRawImageComponent {Url = url, Color = color, Sprite = "assets/content/textures/generic/fulltransparent.tga"};
		}
		
		public CuiRawImageComponent GetItemImageComponent(string shortName){
			string itemUrl = shortName;
			if (plugins.Find("ImageLoader")) {itemUrl = $"https://static.moscow.ovh/images/games/rust/icons/{shortName}.png";}
            return GetImageComponent(itemUrl);
		}
		public bool AddImage(string url,string shortName=""){
			if (plugins.Find("ImageLoader")){				
				plugins.Find("ImageLoader").Call("CheckCachedOrCache", url);
				return true;
			}else
			if (plugins.Find("ImageLibrary")){
				if (string.IsNullOrEmpty(shortName)) shortName=url;
				plugins.Find("ImageLibrary").Call("AddImage", url, shortName);
				return true;
			}	
			return false;		
		}
    }
}