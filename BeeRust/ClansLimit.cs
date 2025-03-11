using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("ClansLimit", "King", "1.0.0")]
    class ClansLimit : RustPlugin
    {
        #region [Vars]
        [PluginReference] private Plugin Clans = null;

        private const string Layer = "ClansLimit.Layer";
        private Dictionary<string, int> _itemIds = new Dictionary<string, int>();
        private Dictionary<ulong, DateTime> CoolDown = new Dictionary<ulong, DateTime>();
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
                if (Version == new VersionNumber(1, 0, 0))
                {
                    //
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class LimitSettings
        {
            [JsonProperty(PropertyName = "Лимит для обьекта")]
            public int Limit;

            [JsonProperty(PropertyName = "ShortName для картинки")]
            public string Image;

            [JsonProperty(PropertyName = "Название обьекта в Ui")]
            public string DisplayName;
        }

        public class MainSettings
        {
            [JsonProperty("Запретить строится без клана ?")] 
            public bool canBuildNoClan;

            [JsonProperty("Запретить строится без клана только по Entity которые ограничены ?")] 
            public bool canBuildNoClanLimits;

            [JsonProperty("Использовать оповещения с помощью чата ?")] 
            public bool useChat;

            [JsonProperty("КД на оповещения")]
            public int timeCooldown;

            [JsonProperty("Команда для открытия UI")] 
            public string commandOpenUi;
        }

        private class PluginConfig
        {
            [JsonProperty("Основные настройки")]
            public MainSettings _MainSettings = new MainSettings();

            [JsonProperty(PropertyName = "Настройка лимитов")]
            public Dictionary<string, LimitSettings> _LimitSettings;

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _MainSettings = new MainSettings
                    {
                        canBuildNoClan = false,
                        timeCooldown = 5,
                        commandOpenUi = "limit",
                    }, 
                    _LimitSettings = new Dictionary<string, LimitSettings>()
                    {
                        ["assets/prefabs/npc/autoturret/autoturret_deployed.prefab"] = new LimitSettings()
                        {
                            Limit = 125,
                            Image = "autoturret",
                            DisplayName = "Автоматическая турель"
                        },
                        ["assets/prefabs/npc/sam_site_turret/sam_site_turret_deployed.prefab"] = new LimitSettings()
                        {
                            Limit = 20,
                            Image = "samsite",
                            DisplayName = "Зенитная установка"
                        },
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion

        #region [ClansData]
        private Dictionary<string, ClanData> _clansList = new Dictionary<string, ClanData>();

        private class ClanData
        {
            public Dictionary<string, int> Limit = new Dictionary<string, int>();

            public Dictionary<ulong, PlayerData> playerBuild = new Dictionary<ulong, PlayerData>();
        }

		private void SaveClans()
		{
			Interface.Oxide.DataFileSystem.WriteObject($"{Name}/ClansData", _clansList);
		}

		private void LoadClans()
		{
			try
			{
				_clansList = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, ClanData>>($"{Name}/ClansData");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_clansList == null) _clansList = new Dictionary<string, ClanData>();
		}
        #endregion

        #region [PlayersData]
        private Dictionary<ulong, PlayerData> _playersList = new Dictionary<ulong, PlayerData>();

        private class PlayerData
        {
            public Dictionary<string, int> Limit = new Dictionary<string, int>();
        }

		private void SavePlayers()
		{
			Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerData", _playersList);
		}

		private void LoadPlayers()
		{
			try
			{
				_playersList = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>($"{Name}/PlayerData");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_playersList == null) _playersList = new Dictionary<ulong, PlayerData>();
		}
        #endregion

        #region [Oxide]
        private void Init()
        {
            LoadClans();

            LoadPlayers();
        }

        private void OnServerInitialized()
        {
            foreach(var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            cmd.AddChatCommand(config._MainSettings.commandOpenUi, this, "cmdChatLimit");
        }

        private void Unload()
        {
            SaveClans();

            SavePlayers();
        }

		private void OnNewSave(string filename)
		{
			_clansList.Clear();
            _playersList.Clear();
			SaveClans();
            SavePlayers();
		}
        #endregion

        #region [GUI]
        private void cmdChatLimit(BasePlayer player)
        {
            var container = new CuiElementContainer();

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

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-493 -293", OffsetMax = "497.5 293" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, Layer, Layer + ".Main");

            var clan = GetClanTag(player.userID);
            if (string.IsNullOrEmpty(clan))
            {
                container.Add(new CuiElement
                {
                    Parent = Layer + ".Main",
                    Components =
                    {
                        new CuiTextComponent { Text = $"Вы не можете увидеть лимиты без клана.\nСоздайте клан /clan", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 32, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"1 1" },
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.890915 0.01", AnchorMax = $"0.99379 0.067", OffsetMax = "0 0" },
                    Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Close = Layer },
                    Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, Layer + ".Main");

                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.AddUi(player, container);
                return;
            }

            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiTextComponent { Text = $"Лимит для вашего клана", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.915", AnchorMax = $"1 1" },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.890915 0.01", AnchorMax = $"0.99379 0.067", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Close = Layer },
                Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            foreach (var check in config._LimitSettings.Select((i, t) => new { A = i, B = t}))
            {
                int Amount = 0;
                var key = config._LimitSettings[check.A.Key];

                if (_clansList.ContainsKey(clan))
                {
                    if (_clansList[clan].Limit.ContainsKey(check.A.Key))
                    {
                        Amount = _clansList[clan].Limit[check.A.Key];
                    }
                }

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.05 + check.B * 0.235 - Math.Floor((float) check.B / 4) * 4 * 0.235} {0.6 - Math.Floor((float) check.B/ 4) * 0.2}",
                                      AnchorMax = $"{0.25 + check.B * 0.235 - Math.Floor((float) check.B / 4) * 4 * 0.235} {0.9 - Math.Floor((float) check.B / 4) * 0.2}", },
                    Image = { Color = "0 0 0 0.25", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                }, Layer + ".Main", Layer + ".Main" + $".Limit({check.B})");

                container.Add(new CuiElement
                {
                    Parent = Layer + ".Main" + $".Limit({check.B})",
                    Components =
                    {
                        new CuiImageComponent { ItemId = FindItemID(key.Image), SkinId = 0 },
                        new CuiRectTransformComponent { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.95" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0 ", AnchorMax = "1 1", OffsetMax = "-5 0", OffsetMin = "5 4" },
                    Button = { Color = "0 0 0 0", Command = $"UI_LIMIT OpenLimitInfo {check.A.Key} {clan}" },
                    Text = { Text = $"Доступно: {key.Limit - Amount}", Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 14 }
                }, Layer + ".Main" + $".Limit({check.B})");
            }

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void cmdChatLimitInfo(BasePlayer player, string prefabName, string tag)
        {
            var container = new CuiElementContainer();

            var clan = GetClanTag(player.userID);
            if (string.IsNullOrEmpty(clan)) return;

            var cfg = config._LimitSettings[prefabName];

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

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-493 -293", OffsetMax = "497.5 293" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, Layer, Layer + ".Main");

            if (!_clansList.ContainsKey(clan) || !_clansList[clan].Limit.ContainsKey(prefabName))
            {
                container.Add(new CuiElement
                {
                    Parent = Layer + ".Main",
                    Components =
                    {
                        new CuiTextComponent { Text = $"Ваш клан еще не поставил {cfg.DisplayName}.\nПодробностей нету!", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 32, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"1 1" },
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.77804 0.01", AnchorMax = $"0.880915 0.067", OffsetMax = "0 0" },
                    Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Command = "UI_LIMIT ReturnToMenu" },
                    Text = { Text = $"НАЗАД", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, Layer + ".Main");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.890915 0.01", AnchorMax = $"0.99379 0.067", OffsetMax = "0 0" },
                    Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Close = Layer },
                    Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, Layer + ".Main");

                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.AddUi(player, container);
                return;
            }

            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiTextComponent { Text = $"Список лимита игроков клана", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.915", AnchorMax = $"1 1" },
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.86", AnchorMax = "0.999 0.92" },
                Image = { Color = "1 1 1 0" }
            }, Layer + ".Main", Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"ИМЯ ИГРОКА", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"УСТАНОВЛЕНО", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.85 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            for (int y = 0; y < 9; y++)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.0055 {0.775 - y * 0.085}", AnchorMax = $"0.989 {0.85 - y * 0.085}" },
                    Image = { Color = "0 0 0 0.5" }
                }, Layer + ".Main", Layer + ".Main" + $".TopLine{y}");
            }

            int i = 0;
            foreach (var key in _clansList[clan].playerBuild)
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = covalence.Players.FindPlayerById($"{key.Key}") != null ? $"{covalence.Players.FindPlayerById($"{key.Key}").Name}" : "UNKNOWN", Color = "1 1 1 0.85", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.027 0", AnchorMax = $"1 1" },
                }, Layer + ".Main" + $".TopLine{i}");

                int Amount = 0;
                if ( _clansList[clan].playerBuild.ContainsKey(key.Key))
                {
                    if ( _clansList[clan].playerBuild[key.Key].Limit.ContainsKey(prefabName))
                    {
                        Amount =  _clansList[clan].playerBuild[key.Key].Limit[prefabName];
                    }
                }

                container.Add(new CuiLabel
                {
                    Text = { Text = $"{Amount} шт.", Font = "robotocondensed-regular.ttf", FontSize = 14, Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.8 0", AnchorMax = $"1 1" },
                }, Layer + ".Main" + $".TopLine{i}");

                i++;
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.77804 0.01", AnchorMax = $"0.880915 0.067", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Command = "UI_LIMIT ReturnToMenu" },
                Text = { Text = $"НАЗАД", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.890915 0.01", AnchorMax = $"0.99379 0.067", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Close = Layer },
                Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region [ConsoleCommand]
        [ConsoleCommand("UI_LIMIT")]
        void cmdUiClansLimit(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null || !args.HasArgs()) return;
            switch (args.Args[0])
            {
                case "OpenLimitInfo":
                {
                    string prefabName = args.Args[1];
                    string clanTag = args.Args[2];
                    if(args.HasArgs(4))
                    {
                        prefabName = $"{args.Args[1]} {args.Args[2]}";
                        clanTag = args.Args[3];
                    }
                    cmdChatLimitInfo(player, prefabName, clanTag);
                    break;
                }
                case "ReturnToMenu":
                {
                    cmdChatLimit(player);
                    break;
                }
            }
        }
        #endregion

        #region [Rust]
        private void OnPlayerConnected(BasePlayer player)
        {
            if(!CoolDown.ContainsKey(player.userID))
                CoolDown.Add(player.userID, new DateTime());
        }

        private object CanBuild(Planner builder, Construction prefab, Construction.Target target)
        {
            String prefabName = prefab.fullName ?? "";
            if (prefabName == "") return null;

            if (prefab.fullName.Contains("assets/prefabs/building core"))
                prefabName = "assets/prefabs/building core";
            if (!config._LimitSettings.ContainsKey(prefabName)) return null;

            BasePlayer player = builder.GetOwnerPlayer();
            if (player == null) return null;

            Int32 limit = GetLimit(prefabName);
            if (limit == 0)
            {
                player.ChatMessage("Объект запрещен на сервере!");
                return false;
            }

            String clan = GetClanTag(player.userID);
            if (string.IsNullOrEmpty(clan))
            {
                player.ChatMessage($"Чтобы строить {config._LimitSettings[prefabName].DisplayName} вам нужно быть в клане.");
                return false;
            }

            if (!_clansList.ContainsKey(clan))
                _clansList.Add(clan, new ClanData());

            var data = _clansList[clan];
            if (data == null) return null;

            if (!data.playerBuild.ContainsKey(player.userID))
            {
                data.playerBuild.Add(player.userID, new PlayerData());
                if (_playersList.ContainsKey(player.userID))
                {
                    foreach (var oldData in _playersList[player.userID].Limit)
                    {
                        if (!data.Limit.ContainsKey(oldData.Key))
                        {
                            data.Limit.Add(oldData.Key, oldData.Value);
                            data.playerBuild[player.userID].Limit.Add(oldData.Key, oldData.Value);
                            continue;
                        }
                        data.Limit[oldData.Key] += oldData.Value;
                        data.playerBuild[player.userID].Limit.Add(oldData.Key, oldData.Value);
                    }
                    _playersList.Remove(player.userID);
                }
            }

            var playerData = data.playerBuild[player.userID];
            if (playerData == null) return null;

            if (data.Limit.ContainsKey(prefabName))
            {
                if (!playerData.Limit.ContainsKey(prefabName))
                    playerData.Limit.Add(prefabName, 0);
                if (data.Limit[prefabName] >= limit)
                {
                    player.ChatMessage("Ваш клан достиг лимита по постройки этого объекта");
                    return false;
                }
            }

            return null;
        }

        private void OnEntitySpawned(BaseEntity entity)
        {
            BasePlayer player = BasePlayer.FindAwakeOrSleeping(entity.OwnerID.ToString());
            if (player == null) return;

            String prefabName = entity.PrefabName ?? "";
            if (prefabName == "") return;

            if (!config._LimitSettings.ContainsKey(prefabName)) return;

            Int32 limit = GetLimit(prefabName);
            String clan = GetClanTag(player.userID);

            if (!_clansList.ContainsKey(clan))
                _clansList.Add(clan, new ClanData());

            var data = _clansList[clan];
            if (data == null) return;

            if (!data.playerBuild.ContainsKey(player.userID))
            {
                data.playerBuild.Add(player.userID, new PlayerData());
                if (_playersList.ContainsKey(player.userID))
                {
                    foreach (var oldData in _playersList[player.userID].Limit)
                    {
                        if (!data.Limit.ContainsKey(oldData.Key))
                        {
                            data.Limit.Add(oldData.Key, oldData.Value);
                            data.playerBuild[player.userID].Limit.Add(oldData.Key, oldData.Value);
                            continue;
                        }
                        data.Limit[oldData.Key] += oldData.Value;
                        data.playerBuild[player.userID].Limit.Add(oldData.Key, oldData.Value);
                    }
                    _playersList.Remove(player.userID);
                }
            }

            var playerData = data.playerBuild[player.userID];
            if (playerData == null) return;

            if (data.Limit.ContainsKey(prefabName))
            {
                if (data.Limit[prefabName] <= (limit - 1))
                {
                    data.Limit[prefabName]++;
                    playerData.Limit[prefabName]++;
                    SendPlayer(player, $"Вы можете построить еще {limit - data.Limit[prefabName]} {config._LimitSettings[prefabName].DisplayName}");
                    return;
                }
            }
            data.Limit.Add(prefabName, 1);
            playerData.Limit.Add(prefabName, 1);
            SendPlayer(player, $"Вы можете построить еще {limit - data.Limit[prefabName]} {config._LimitSettings[prefabName].DisplayName}");
        }

        private void OnEntityKill(BaseEntity entity)
        {
            if (entity.OwnerID == 0) return;
            var prefabName = entity.PrefabName;
            if (prefabName.Contains("assets/prefabs/building core"))
                prefabName = "assets/prefabs/building core";
            if (_playersList.ContainsKey(entity.OwnerID))
            {
                if (!_playersList[entity.OwnerID].Limit.ContainsKey(prefabName)) return;
                _playersList[entity.OwnerID].Limit[prefabName]--;
            }
            var clan = GetClanTag(entity.OwnerID);
            if (string.IsNullOrEmpty(clan)) return;
            if (!_clansList.ContainsKey(clan)) return;
            var clanData = _clansList[clan];
            if (!clanData.Limit.ContainsKey(prefabName)) return;
            clanData.Limit[prefabName]--;
            if (!clanData.playerBuild.ContainsKey(entity.OwnerID)) return;
            var playerData = clanData.playerBuild[entity.OwnerID];
            if (!playerData.Limit.ContainsKey(prefabName)) return;
            playerData.Limit[prefabName]--;
        }
        #endregion

        #region [Clans]
        private void OnClanDisbanded(string tag, List<ulong> memberUserIDs)
		{
            if (!_clansList.ContainsKey(tag)) return;

            var data = _clansList[tag];
            if (data == null) return;

            foreach (var key in memberUserIDs)
            {
                if (!data.playerBuild.ContainsKey(key)) continue;
                var playerData = data.playerBuild[key];
                if (playerData == null) continue;

                if (!_playersList.ContainsKey(key))
                    _playersList.Add(key, new PlayerData());

                var pdata = _playersList[key];
                if (pdata == null) continue;

                foreach (var _data in playerData.Limit)
                {
                    data.Limit[_data.Key] -= _data.Value;
                    pdata.Limit.Add(_data.Key, _data.Value);
                }
            }
            _clansList.Remove(tag);
		}

        private void OnClanMemberGone(ulong userID, string tag)
        {
            if (!_clansList.ContainsKey(tag)) return;

            var data = _clansList[tag];
            if (data == null) return;

            if (!data.playerBuild.ContainsKey(userID)) return;

            var playerData = data.playerBuild[userID];
            if (playerData == null) return;

            if (!_playersList.ContainsKey(userID))
                 _playersList.Add(userID, new PlayerData());

            var pdata = _playersList[userID];
            if (pdata == null) return;

            foreach (var _data in playerData.Limit)
            {
                data.Limit[_data.Key] -= _data.Value;
                if (!pdata.Limit.ContainsKey(_data.Key))
                    pdata.Limit.Add(_data.Key, _data.Value);
                else
                    pdata.Limit[_data.Key] += _data.Value;
            }

            data.playerBuild.Remove(userID);
        }
        #endregion

        #region [Functional]
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
        private void SendPlayer(BasePlayer player, string message)
        {
            if ((DateTime.Now - CoolDown[player.userID]).TotalSeconds < config._MainSettings.timeCooldown) return;
            player.ChatMessage(message);
            CoolDown[player.userID] = DateTime.Now;
        }
        private string GetClanTag(ulong playerID) => Clans?.Call<string>("GetClanTag", playerID);
        private int GetLimit(string name)=> config._LimitSettings[name].Limit;
        #endregion
    }
}