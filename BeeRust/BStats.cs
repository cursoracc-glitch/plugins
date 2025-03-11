using System.IO;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BStats", "King", "1.1.1")]
    public class BStats : RustPlugin
    {
        #region [Vars]
        [PluginReference] private Plugin ImageLibrary = null;
        private string[] _gatherHooks = {
            "OnDispenserGather",
            "OnDispenserBonus",
            "OnCollectiblePickup",
        };
        private static BStats plugin;
        private const string Layer = "BStats.Layer";

        private readonly Dictionary<ulong, BasePlayer> _lastHeli = new Dictionary<ulong, BasePlayer>();
        private Dictionary<string, int> _itemIds = new Dictionary<string, int>();
        private List<ulong> _lootEntity = new List<ulong>();
        #endregion

        #region [ImageLibrary]
        private bool HasImage(string imageName, ulong imageId = 0) => (bool)ImageLibrary.Call("HasImage", imageName, imageId);
        private bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        private string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        #endregion

        #region [Data]
        Dictionary<ulong, playerData> _playerList = new Dictionary<ulong, playerData>();

		public class playerData
		{
			public string Name;

            public int Point;

            public int PlayTimeInServer = 0;

            public int Kill = 0;

            public int Death = 0;

            public Dictionary<string, int> Gather = new Dictionary<string, int>()
            {
                { "wood", 0 },
                { "stones", 0 },
                { "metal.ore", 0 },
                { "sulfur.ore", 0},
                { "hq.metal.ore", 0 },
                { "cloth", 0},
                { "leather", 0},
                { "fat.animal", 0},
                { "loot-barrel", 0}
            };

            public int TotalFarm() => Gather.Sum(p => p.Value);
        }

		private playerData GetPlayerData(ulong member)
		{
			if (!_playerList.ContainsKey(member))
				_playerList.Add(member, new playerData());

			return _playerList[member];
		}

		private void SavePlayer()
		{
			Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerList", _playerList);
		}

		private void LoadPlayer()
		{
			try
			{
				_playerList = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, playerData>>($"{Name}/PlayerList");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_playerList == null) _playerList = new Dictionary<ulong, playerData>();
		}
        #endregion

        #region [Oxide]
        private void OnPluginLoaded(Plugin plugin)
        {
            NextTick(() =>
            {
                foreach (string hook in _gatherHooks)
                {
                    Unsubscribe(hook);
                    Subscribe(hook);
                }
            });
        }

		private void Init()
		{
			plugin = this;

			LoadPlayer();
		}

        private void OnServerInitialized()
        {
            cmd.AddChatCommand(config.openMenuTop, this, "cmdOpenStats");
            
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            if (config._NotifyChatRandom.chatSendTop)
                timer.Every(config._NotifyChatRandom.chatSendTopTime, GetRandomTopPlayer);
            timer.Every(60, TimeHandle);
        }

		private void Unload()
		{
			foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }

			SavePlayer();
			plugin = null;
		}

		private void OnNewSave(string filename)
		{
            WipeEnded();
		}
        #endregion

        #region [Reward]
        private void WipeEnded()
        {
            if(config._GameStoreSettings.GivePrize && !string.IsNullOrEmpty(config._GameStoreSettings.ShopID) && !string.IsNullOrEmpty(config._GameStoreSettings.SecretKey))
            {
                var sortedData = _playerList.OrderByDescending(x => x.Value.Point);
                int pos = 1;

                foreach (var user in sortedData)
                {
                    if (config._GameStoreSettings.RewardSettings.ContainsKey(pos))
                    {
                        var args = new Dictionary<string, string>()
                        {
                            { "action", "moneys" },
                            { "type", "plus" },
                            { "steam_id", user.Key.ToString() },
                            { "amount", config._GameStoreSettings.RewardSettings[pos].ToString() }
                        };
                        string url = $"https://gamestores.ru/api/?shop_id={config._GameStoreSettings.ShopID}&secret={config._GameStoreSettings.SecretKey}" + $"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
                        webrequest.Enqueue(url, null, (i, s) =>
                        {
                            if (i != 200)
                            {
                                PrintError($"Ошибка {i}: {s}");
                                return;
                            }
                        }, this);
                    }
                    pos++;
                }
            }

            foreach (var playerData in _playerList)
            {
                playerData.Value.Point = 0;
                playerData.Value.PlayTimeInServer = 0;
                playerData.Value.Kill = 0;
                playerData.Value.Death = 0;
                playerData.Value.Gather = new Dictionary<string, int>()
                {
                    ["wood"] = 0,
                    ["stones"] = 0,
                    ["metal.ore"] = 0,
                    ["hq.metal.ore"] = 0,
                    ["sulfur.ore"] = 0,
                    ["cloth"] = 0,
                    ["leather"] = 0,
                    ["fat.animal"] = 0,
                    ["loot-barrel"] = 0
                };
            }
            SavePlayer();
        }
        #endregion

        #region [Gui]
        private void PlayerTop(BasePlayer player, int page = 0)
        {
            #region [Vars]
            var container = new CuiElementContainer();
            #endregion

            #region [Parrent]
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" }
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Close = Layer }
            }, Layer);
            #endregion

            #region [Main-Gui]
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-255 -79", OffsetMax = "260 226" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, Layer, Layer + ".Main");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-255 -174", OffsetMax = "260 -85" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, Layer, Layer + ".Description");
            #endregion

            #region [Text]
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.933", AnchorMax = "0.997 0.997" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Main", Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"#", Color = "1 1 1 1", FontSize = 13, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.033 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"ИМЯ ИГРОКА", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.094 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"НАГРАДА", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.7 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"ОЧКИ", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.88 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"Очки даются:\nУбийство +{config._PointsKillDeath.pKill}, добыча камня +{config._PointsSettings.pStone}, добыча метала +{config._PointsSettings.pMetal}, добыча серы +{config._PointsSettings.pSulfur}, разрушение бочки +{config._PointsSettings.pBarrel},\nсбитие вертолета +{config._PointsDestroy.dHeli}, уничтожение танка +{config._PointsDestroy.dBradley}\nОчки отнимаются:\nСмерть -{config._PointsKillDeath.pDeath}, самоубийство -{config._PointsKillDeath.pSuicide}\nНаграда выдается после вайпа на сервере!", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.0125 0", AnchorMax = $"1 1" },
            }, Layer + ".Description");
            #endregion

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
            TopPlayerList(player, page);
        }

        private void TopPlayerList(BasePlayer player, int page = 0)
        {
            #region [Vars]
            var playerList = _playerList.OrderByDescending(p => p.Value.Point);
            var container = new CuiElementContainer();
            string colored = "0 0 0 0.5";
            int i = 0;
            #endregion

            #region [Main]
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Main", Layer + ".Main" + "TopPlayerList");
            #endregion

            #region [Button]
            container.Add(new CuiButton
            {
                Button = { Color = "0.38 0.62 0.12 0.85", Command = $"UI_BSTATS OpenProfileStats {player.userID}", Material = "assets/icons/greyout.mat" },
                Text = { Text = "МОЙ ПРОФИЛЬ", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.4 0.0185", AnchorMax = $"0.6 0.125" },
            }, Layer + ".Main" + "TopPlayerList");

            container.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Close = Layer },
                Text = { Text = "ЗАКРЫТЬ", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.8 0.0185", AnchorMax = $"0.99 0.125" },
            }, Layer + ".Main" + "TopPlayerList");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.088 0.0185", AnchorMax = $"0.165 0.125" },
                Image = { Color = "0.2 0.2 0.2 0.25", Material = "assets/icons/greyout.mat" }
            }, Layer + ".Main" + "TopPlayerList", Layer + ".Main" + "TopPlayerList" + ".Page");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                Text = { Text = $"{page + 1}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, Layer + ".Main" + "TopPlayerList" + ".Page");

            container.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Command = page > 0 ? $"UI_BSTATS ChangeTopPage {page - 1}" : "" },
                Text = { Text = "-", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.01 0.0185", AnchorMax = $"0.088 0.125" },
            }, Layer + ".Main" + "TopPlayerList");

            container.Add(new CuiButton
            {
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Command = playerList.Skip(10 * (page + 1)).Count() > 0 ? $"UI_BSTATS ChangeTopPage {page + 1}" : "" },
                Text = { Text = "+", FontSize = 22, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = $"0.165 0.0185", AnchorMax = $"0.244 0.125" },
            }, Layer + ".Main" + "TopPlayerList");
            #endregion

            #region [PlayerInfo]
            for (int y = 0; y < 10; y++)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.0055 {0.858 - y * 0.0795}", AnchorMax = $"0.989 {0.93 - y * 0.0795}" },
                    Image = { Color = colored }
                }, Layer + ".Main" + "TopPlayerList", Layer + ".Main" + "TopPlayerList" + $".TopLine{y}");
            }

            foreach (var key in playerList.Skip(10 * page).Take(playerList.ToList().Count >= 10 ? 10 : playerList.ToList().Count))
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = $"{i + (1 + (page * 10))}", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.067 1" },
                }, Layer + ".Main" + "TopPlayerList" + $".TopLine{i}");

                container.Add(new CuiLabel
                {
                    Text = { Text = BasePlayer.FindByID(key.Key) != null ? "<color=lime>●</color>" : "<color=red>●</color>", Font = "robotocondensed-regular.ttf", FontSize = 7, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.0925 0", AnchorMax = $"0.44 1" },
                }, Layer + ".Main" + "TopPlayerList" + $".TopLine{i}");

                container.Add(new CuiLabel
                {
                    Text = { Text = $"{key.Value.Name}", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.1085 0", AnchorMax = $"0.45 1" },
                }, Layer + ".Main" + "TopPlayerList" + $".TopLine{i}");

                if (config._GameStoreSettings.RewardSettings.ContainsKey(i + (1 + (page * 10))))
                {
                    container.Add(new CuiLabel
                    {
                        Text = { Text = $"{config._GameStoreSettings.RewardSettings[i + (1 + (page * 10))]}", Color = "1 1 1 1", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                        RectTransform = { AnchorMin = $"0.695 0", AnchorMax = $"0.8 1" },
                    }, Layer + ".Main" + "TopPlayerList" + $".TopLine{i}");
                }

                container.Add(new CuiLabel
                {
                    Text = { Text = $"{key.Value.Point}", Font = "robotocondensed-regular.ttf", FontSize = 12, Color = "1 1 1 1", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.83 0", AnchorMax = $"1 1" },
                }, Layer + ".Main" + "TopPlayerList" + $".TopLine{i}");

                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"UI_BSTATS OpenProfileStats {key.Key}" },
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }, Layer + ".Main" + "TopPlayerList" + $".TopLine{i}");

                i++;
            }
            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Main" + "TopPlayerList");
            CuiHelper.AddUi(player, container);
        }

        private void PlayerTopInfo(BasePlayer player, ulong playerID)
        {
            #region [Vars]
            var container = new CuiElementContainer();
            string colored = "0 0 0 0.5";

            var data = GetPlayerData(playerID);
            if (data == null) return;
            #endregion

            #region [Parrent]
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" }
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Close = Layer }
            }, Layer);
            #endregion

            #region [Main-Gui]
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-254 -69", OffsetMax = "259 213" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, Layer, Layer + ".Profile");
            #endregion

            #region [Avatar]
            container.Add(new CuiElement
            {
                Parent = Layer + ".Profile",
                Components =
                {
                    new CuiRawImageComponent { Png = GetImage($"avatar_{playerID}") },
                    new CuiRectTransformComponent { AnchorMin = $"0.06 0.43", AnchorMax = $"0.31075 0.89" }
                }
            });
            #endregion

            #region [Title]
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.44 0.91", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Text = { Text = "●", Color = BasePlayer.FindByID(playerID) != null ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".Profile");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.46 0.91", AnchorMax = $"0.99 1", OffsetMax = "0 0" },
                Text = { Text = $"{data.Name}", Color = "1 1 1 0.8", Align = TextAnchor.MiddleLeft, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".Profile");
            #endregion

            #region [Info]
            Dictionary<string, string> _playerInfo = new Dictionary<string, string>()
            {
                { "МЕСТО В ТОПЕ:", $"{GetTopScore(playerID)}" },
                { "ОЧКОВ:", $"{data.Point}" },
                { "АКТИВНОСТЬ:", $"{data.PlayTimeInServer}м." },
                { "УБИЙСТВ:", $"{data.Kill}" },
                { "СМЕРТЕЙ:", $"{data.Death}" },
                { "К/Д:", $"{(data.Death == 0 ? data.Kill : (float)Math.Round(((float)data.Kill) / data.Death, 2))}" },
            };

            foreach (var check in _playerInfo.Select((i, t) => new { A = i, B = t }))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.365 {0.775 - Math.Floor((float) check.B/ 1) * 0.0625}",
                                        AnchorMax = $"0.938 {0.83099 - Math.Floor((float) check.B / 1) * 0.0625}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, Layer + ".Profile", Layer + ".Profile" + ".Info" + $".{check.B}");

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Profile" + ".Info" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Key}", Color = "1 1 1 1", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0.011 0", AnchorMax = $"1 1" },
                    }
                }); 

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Profile" + ".Info" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = $"{check.A.Value}", Color = "1 1 1 1", Align = TextAnchor.MiddleRight, FontSize = 12, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"0.985 1" },
                    }
                }); 
            }
            #endregion

            #region [Resourse]
            foreach (var check in data.Gather.OrderByDescending(x => x.Value).Select((i, t) => new { A = i, B = t }))
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.025 + check.B * 0.107 - Math.Floor((float) check.B / 9) * 9 * 0.107} 0.175",
                                        AnchorMax = $"{0.115 + check.B * 0.107 - Math.Floor((float) check.B / 9) * 9 * 0.107} 0.345", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/icons/greyout.mat" }
                }, Layer + ".Profile", Layer + ".Profile" + $".{check.B}");

                if (FindItemID(check.A.Key) != 0)
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Profile" + $".{check.B}",
                        Components =
                        {
                            new CuiImageComponent { ItemId = FindItemID(check.A.Key), SkinId = 0 },
                            new CuiRectTransformComponent {AnchorMin = "0.025 0.025", AnchorMax = "0.975 0.975"}
                        }
                    });
                }
                else
                {
                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Profile" + $".{check.B}",
                        Components =
                        {
                            new CuiRawImageComponent { Png = GetImage(check.A.Key) },
                            new CuiRectTransformComponent {AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95"}
                        }
                    });
                }

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Profile" + $".{check.B}",
                    Components =
                    {
                        new CuiTextComponent { Text = check.A.Value.ToString(), Color = "1 1 1 1", Align = TextAnchor.MiddleRight, FontSize = 10, Font = "robotocondensed-regular.ttf" },
                        new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"0.89 0.31" },
                    }
                });
            }
            #endregion

            #region [Button]
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.5925 0.015", AnchorMax = $"0.79 0.131", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 0.6", Material = "assets/icons/greyout.mat", Command = $"UI_BSTATS ReturnToPlayerTop {Math.Ceiling(GetTopScore(playerID) / 10f)}" },
                Text = { Text = $"НАЗАД", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Profile");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.795 0.015", AnchorMax = $"0.991 0.131", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 0.6", Material = "assets/icons/greyout.mat", Close = Layer },
                Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Profile");
            #endregion

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region [Connect]
        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || !player.userID.IsSteamId()) return;

            GetAvatar(player.UserIDString, avatar => AddImage(avatar, $"avatar_{player.UserIDString}"));

            var data = GetPlayerData(player.userID);
            if (data == null || string.IsNullOrEmpty(player.displayName)) return;

            var Name = covalence.Players.FindPlayerById(player.UserIDString)?.Name;
            if (data.Name != Name)
                data.Name = Name;
        }
        #endregion

        #region [Gather]
		private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
		{
            if (!entity.ToPlayer() || entity == null || item == null) return;

            var player = entity.ToPlayer();
            if (player == null || player.IsNpc) return;

            AddResourse(player, item.info.shortname, item.amount);
		}

		private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
		{
            if (!entity.ToPlayer() || entity == null || item == null) return;

            var player = entity.ToPlayer();
            if (player == null || player.IsNpc) return;

            AddResourse(player, item.info.shortname, item.amount, true);
		}

        private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
		{
			if (player == null || collectible == null || collectible.itemList == null) return;

			foreach (var itemAmount in collectible.itemList)
            {
			    if (itemAmount.itemDef != null)
                {
                    AddResourse(player, itemAmount.itemDef.shortname, (int)itemAmount.amount);
                }
            }
		}
        #endregion

        #region [Entity]
		private void OnEntityTakeDamage(PatrolHelicopter entity, HitInfo info)
		{
			if (entity != null && entity.net != null && info.InitiatorPlayer != null)
				_lastHeli[entity.net.ID.Value] = info.InitiatorPlayer;
		}

		private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
		{
			if (entity == null || info == null) return;

			if (entity is PatrolHelicopter)
			{
				if (_lastHeli.ContainsKey(entity.net.ID.Value))
				{
                    var dataHeli = GetPlayerData(_lastHeli[entity.net.ID.Value].userID);
                    if (dataHeli == null) return;
                    dataHeli.Point += config._PointsDestroy.dHeli;
				}
				return;
			}

			var player = info.InitiatorPlayer;
			if (player == null) return;

            var data = GetPlayerData(player.userID);
            if (data == null) return;

            if (entity is BradleyAPC)
            {
                data.Point += config._PointsDestroy.dBradley;
            }
            else if (entity.name.Contains("barrel"))
            {
                data.Point += config._PointsSettings.pBarrel;
                data.Gather["loot-barrel"]++;
            }
		}
        #endregion

        #region [Loot]
        private void OnLootEntity(BasePlayer player, LootContainer entity)
        {
            if (player == null || entity == null || entity?.net?.ID == null || _lootEntity.Contains(entity.net.ID.Value)) return;

            var data = GetPlayerData(player.userID);
            if (data == null) return;

            data.Point += config._PointsSettings.pBarrel;
            data.Gather["loot-barrel"]++;
            
            _lootEntity.Add(entity.net.ID.Value);
        }
        #endregion

        #region [Death]
        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || !player.userID.IsSteamId()) return;
            
            if (info.damageTypes.Has(DamageType.Suicide))
            {
                var data = GetPlayerData(player.userID);
                if (data == null) return;
                
                data.Point -= config._PointsKillDeath.pSuicide;
                data.Death++;
                return;
            }
            
            var attacker = info.InitiatorPlayer;
            if (attacker == null || !attacker.userID.IsSteamId() || IsTeammates(player.userID, attacker.userID)) return;

            if (player.userID.IsSteamId())
            {
                var data = GetPlayerData(player.userID);
                if (data != null)
                {
                    data.Point -= config._PointsKillDeath.pDeath;
                    data.Death++;
                }

                var dataAttacker = GetPlayerData(attacker.userID);
                if (dataAttacker != null)
                {
                    dataAttacker.Point += config._PointsKillDeath.pKill;
                    dataAttacker.Kill++;
                }
            }
        }
        #endregion

        #region [ConsoleCommand]
        private void cmdOpenStats(BasePlayer player) => PlayerTop(player);

        [ConsoleCommand("UI_BSTATS")]
        private void StatsUIHandler(ConsoleSystem.Arg args)
        {
			BasePlayer player = args?.Player();
			if (player == null || !args.HasArgs()) return;

            switch (args.Args[0])
            {
                case "OpenProfileStats":
                {
                    PlayerTopInfo(player, ulong.Parse(args.Args[1]));
                    break;
                }
                case "ReturnToPlayerTop":
                {
                    PlayerTop(player, int.Parse(args.Args[1]) - 1);
                    break;
                }
                case "ChangeTopPage":
                {
                    TopPlayerList(player, int.Parse(args.Args[1]));
                    break;
                }
            }
        }
        #endregion

		#region [Avatar]
		private readonly Regex Regex = new Regex(@"<avatarFull><!\[CDATA\[(.*)\]\]></avatarFull>");
		private void GetAvatar(string userId, Action<string> callback)
		{
			if (callback == null) return;

			try
			{
				webrequest.Enqueue($"http://steamcommunity.com/profiles/{userId}?xml=1", null, (code, response) =>
				{
					if (code != 200 || response == null)
						return;

					var avatar = Regex.Match(response).Groups[1].ToString();
					if (string.IsNullOrEmpty(avatar))
						return;

					callback.Invoke(avatar);
				}, this);
			}
			catch (Exception e)
			{
				PrintError($"{e.Message}");
			}
		}
        #endregion

        #region [NotifyChat]
        private void GetRandomTopPlayer()
        {
            int random = Core.Random.Range(0, 9);

            switch (random)
            {
                case 0:
                {
                    var playerList = _playerList.OrderByDescending(p => p.Value.Kill).Take(5);
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        int i = 1;
                        ServerBroadcast(player, "<size=18><color=#FFDD2FFF>Больше всего убийств:</color></size>", 0);
                        foreach (var key in playerList)
                        {
                            ServerBroadcast(player, $"<size=16>{i}.{key.Value.Name} - <color=#FFDD2FFF>{key.Value.Kill}</color></size>", key.Key);
                            i++;
                        }
                    }
                    break;
                }
                case 1:
                {
                    var playerList = _playerList.OrderByDescending(p => p.Value.Death).Take(5);
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        int i = 1;
                        ServerBroadcast(player, "<size=18><color=#FFDD2FFF>Больше всего смертей:</color></size>", 0);
                        foreach (var key in playerList)
                        {
                            ServerBroadcast(player, $"<size=16>{i}.{key.Value.Name} - <color=#FFDD2FFF>{key.Value.Death}</color></size>", key.Key);
                            i++;
                        }
                    }
                    break;
                }
                case 2:
                {
                    var playerList = _playerList.OrderByDescending(p => p.Value.TotalFarm()).Take(5);
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        int i = 1;
                        ServerBroadcast(player, "<size=18><color=#FFDD2FFF>Больше всего фарма:</color></size>", 0);
                        foreach (var key in playerList)
                        {
                            ServerBroadcast(player, $"<size=16>{i}.{key.Value.Name} - <color=#FFDD2FFF>{key.Value.TotalFarm()}</color></size>", key.Key);
                            i++;
                        }
                    }
                    break;
                }
                case 3:
                {
                    var playerList = _playerList.OrderByDescending(p => p.Value.Gather["hq.metal.ore"]).Take(5);
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        int i = 1;
                        ServerBroadcast(player, "<size=18><color=#FFDD2FFF>Добыто МВК:</color></size>", 0);
                        foreach (var key in playerList)
                        {
                            ServerBroadcast(player, $"<size=16>{i}.{key.Value.Name} - <color=#FFDD2FFF>{key.Value.Gather["hq.metal.ore"]}</color></size>", key.Key);
                            i++;
                        }
                    }
                    break;
                }
                case 4:
                {
                    var playerList = _playerList.OrderByDescending(p => p.Value.Gather["metal.ore"]).Take(5);
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        int i = 1;
                        ServerBroadcast(player, "<size=18><color=#FFDD2FFF>Добыто Металла:</color></size>", 0);
                        foreach (var key in playerList)
                        {
                            ServerBroadcast(player, $"<size=16>{i}.{key.Value.Name} - <color=#FFDD2FFF>{key.Value.Gather["metal.ore"]}</color></size>", key.Key);
                            i++;
                        }
                    }
                    break;
                }
                case 5:
                {
                    var playerList = _playerList.OrderByDescending(p => p.Value.Gather["sulfur.ore"]).Take(5);
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        int i = 1;
                        ServerBroadcast(player, "<size=18><color=#FFDD2FFF>Добыто Серы:</color></size>", 0);
                        foreach (var key in playerList)
                        {
                            ServerBroadcast(player, $"<size=16>{i}.{key.Value.Name} - <color=#FFDD2FFF>{key.Value.Gather["sulfur.ore"]}</color></size>", key.Key);
                            i++;
                        }
                    }
                    break;
                }
                case 6:
                {
                    var playerList = _playerList.OrderByDescending(p => p.Value.Gather["loot-barrel"]).Take(5);
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        int i = 1;
                        ServerBroadcast(player, "<size=18><color=#FFDD2FFF>Добыто бочек и залутно ящиков:</color></size>", 0);
                        foreach (var key in playerList)
                        {
                            ServerBroadcast(player, $"<size=16>{i}.{key.Value.Name} - <color=#FFDD2FFF>{key.Value.Gather["loot-barrel"]}</color></size>", key.Key);
                            i++;
                        }
                    }
                    break;
                }
                case 7:
                {
                    var playerList = _playerList.OrderByDescending(p => p.Value.PlayTimeInServer).Take(5);
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        int i = 1;
                        ServerBroadcast(player, "<size=18><color=#FFDD2FFF>Проведено больше всего время на сервере:</color></size>", 0);
                        foreach (var key in playerList)
                        {
                            ServerBroadcast(player, $"<size=16>{i}.{key.Value.Name} - <color=#FFDD2FFF>{FormatShortTime(TimeSpan.FromSeconds(key.Value.PlayTimeInServer * 60))}</color></size>", key.Key);
                            i++;
                        }
                    }
                    break;
                }
                case 8:
                {
                    var playerList = _playerList.OrderByDescending(p => p.Value.Point).Take(5);
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        int i = 1;
                        ServerBroadcast(player, "<size=18><color=#FFDD2FFF>Больше всего очков:</color></size>", 0);
                        foreach (var key in playerList)
                        {
                            ServerBroadcast(player, $"<size=16>{i}.{key.Value.Name} - <color=#FFDD2FFF>{key.Value.Point}</color></size>", key.Key);
                            i++;
                        }
                    }
                    break;
                }
            }
        }

        private void ServerBroadcast(BasePlayer player, string message, ulong AvatarID)
        {
            if (player == null || string.IsNullOrEmpty(message)) return;

            Player.Message(player, $"{message}", AvatarID);
        }

        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{time.Days} д. ";

            if (time.Hours != 0)
                result += $"{time.Hours} час. ";

            if (time.Minutes != 0)
                result += $"{time.Minutes} мин. ";

            if (time.Seconds != 0)
                result += $"{time.Seconds} сек. ";

            return result;
        }
        #endregion

        #region [Functional]
		private bool IsTeammates(ulong player, ulong friend)
		{
			return player == friend ||
			       RelationshipManager.ServerInstance.FindPlayersTeam(player)?.members?.Contains(friend) == true;
		}

		private void TimeHandle()
		{
            foreach (var player in BasePlayer.activePlayerList)
            {
                var data = GetPlayerData(player.userID);
                if (data == null) continue;
                
                data.PlayTimeInServer++;
            }
		}

        private int GetTopScore(ulong userid)
        {
            int Top = 1;
            var RaitingNumber = _playerList.OrderByDescending(x => x.Value.Point);

            foreach (var Data in RaitingNumber)
            {
                if (Data.Key == userid)
                    break;
                Top++;
            }

            return Top;
        }

		private int FindItemID(string shortName)
		{
			int val;
			if (_itemIds.TryGetValue(shortName, out val))
				return val;

			var definition = ItemManager.FindItemDefinition(shortName);
			if (definition == null) return 0;

			val = definition.itemid;
			_itemIds[shortName] = val;
			return val;
		}
        #endregion

        #region [AddResourse]
        private void AddResourse(BasePlayer player, string shortname, int amount, bool GivePoint = false)
        {
            if (player == null || string.IsNullOrEmpty(shortname) || amount <= 0) return;

            var data = GetPlayerData(player.userID);
            if (data == null || !data.Gather.ContainsKey(shortname)) return;

            switch (shortname)
            {
                case "wood":
                {
                    data.Gather[shortname] += amount;
                    if (GivePoint)
                    {
                        data.Point += config._PointsSettings.pWood;
                    }
                    break;
                }
                case "stones":
                {
                    data.Gather[shortname] += amount;
                    if (GivePoint)
                    {
                        data.Point += config._PointsSettings.pStone;
                    }
                    break;
                }
                case "metal.ore": case "metal.fragments":
                {
                    data.Gather["metal.ore"] += amount;
                    if (GivePoint)
                    {
                        data.Point += config._PointsSettings.pMetal;
                    }
                    break;
                }
                case "sulfur.ore": case "sulfur":
                {
                    data.Gather["sulfur.ore"] += amount;
                    if (GivePoint)
                    {
                        data.Point += config._PointsSettings.pMetal;
                    }
                    break;
                }
                case "hq.metal.ore": case "metal.refined":
                {
                    data.Gather["hq.metal.ore"] += amount;
                    break;
                }
                case "leather":
                {
                    data.Gather[shortname] += amount;
                    break;
                }
                case "cloth":
                {
                    data.Gather[shortname] += amount;
                    break;
                }
                case "fat.animal":
                {
                    data.Gather[shortname] += amount;                 
                    break;
                }
            }
        }
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
                if (Version == new VersionNumber(1, 1, 1))
                {
                    config._NotifyChatRandom.chatSendTop = true;
                    config._NotifyChatRandom.chatSendTopTime = 1200;
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class PointsSettings
        {
            [JsonProperty("Сколько давать очков за дерево")]
            public int pWood = 5;

            [JsonProperty("Сколько давать очков за каменный камень")]
            public int pStone = 5;

            [JsonProperty("Сколько давать очков за металический камень")]
            public int pMetal = 5;

            [JsonProperty("Сколько давать очков за серный камень")]
            public int pSulfur = 5;

            [JsonProperty("Сколько давать очков за уничтожение бочки | Лутание обычного ящика у дороги")]
            public int pBarrel = 5;
        }

        public class PointsDestroy
        {
            [JsonProperty("Сколько давать очков за уничтожение вертолета")]
            public int dHeli = 1500;

            [JsonProperty("Сколько давать очков за уничтожение танка")]
            public int dBradley = 750;
        }

        public class PointsKillDeath
        {
            [JsonProperty("Сколько давать очков за убийство игрока")]
            public int pKill = 40;

            [JsonProperty("Сколько отнимать очков за смерть")]
            public int pDeath = 15;

            [JsonProperty("Сколько отнимать очков за суицид")]
            public int pSuicide = 15;
        }

        public class GameStoreSettings
        {
            [JsonProperty("Включить авто выдачу призов при вайпе сервера?")]
            public bool GivePrize = true;

            [JsonProperty("ИД магазина в сервисе")] 
            public string ShopID = "";

            [JsonProperty("Секретный ключ (не распростраяйте его)")] 
            public string SecretKey = "";

            [JsonProperty("Место в топе и выдаваемый баланс игроку")]
            public Dictionary<int, float> RewardSettings;
        }

        public class NotifyChatRandom
        {
            [JsonProperty("Отправлять в чат сообщения с топ 5 игроками ?")]
            public bool chatSendTop = true;

            [JsonProperty("Раз в сколько секунд будет отправлятся сообщение ?")]
            public int chatSendTopTime = 1200;
        }

        private class PluginConfig
        {
            [JsonProperty("Команда для открытия топа")]
            public string openMenuTop;

            [JsonProperty("Настройка начисления очков за добычу")]
            public PointsSettings _PointsSettings = new PointsSettings();

            [JsonProperty("Настройка начисления очков за уничтожение")]
            public PointsDestroy _PointsDestroy = new PointsDestroy();

            [JsonProperty("Настройка начисления и отнимания очков за убийства и смерти")]
            public PointsKillDeath _PointsKillDeath = new PointsKillDeath();

            [JsonProperty("Настройка призов")]
            public GameStoreSettings _GameStoreSettings = new GameStoreSettings();

            [JsonProperty("Настройка оповещений в чате")]
            public NotifyChatRandom _NotifyChatRandom = new NotifyChatRandom();

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    openMenuTop = "top",
                    _PointsDestroy = new PointsDestroy()
                    {
                        dHeli = 1500,
                        dBradley = 750,
                    },
                    _PointsKillDeath = new PointsKillDeath()
                    {
                        pKill = 40,
                        pDeath = 15,
                        pSuicide = 15,
                    },
                    _PointsSettings = new PointsSettings()
                    {
                        pWood = 5,
                        pStone = 5,
                        pMetal = 5,
                        pSulfur = 5,
                        pBarrel = 5,
                    },
                    _GameStoreSettings = new GameStoreSettings()
                    {
                        GivePrize = true,
                        ShopID = "",
                        SecretKey = "",
                        RewardSettings = new Dictionary<int, float>()
                        {
                            [1] = 400f,
                            [2] = 250f,
                            [3] = 150f,
                            [4] = 100f,
                            [5] = 50f,
                            [6] = 50f,
                            [7] = 30f,
                        },
                    },
                    _NotifyChatRandom = new NotifyChatRandom()
                    {
                        chatSendTop = true,
                        chatSendTopTime = 1200,
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion
    }
}