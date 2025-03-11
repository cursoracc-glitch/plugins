using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("RaidAlerts", "King", "1.1.0")]
    public class RaidAlerts : RustPlugin
    {
        #region [Vars]
        [PluginReference] private Plugin Clans = null;
        private Dictionary<string, int> _itemIds = new Dictionary<string, int>();
        private const string Layer = "RaidAlerts.Layer";
        #endregion

        #region [Data]
        Dictionary<ulong, playerData> _playerList = new Dictionary<ulong, playerData>();

		public class playerData
		{
            public string vkID = string.Empty;

            public DateTime vkCooldown;

            public DateTime gameCooldown;
        }

		private playerData GetPlayerData(ulong member)
		{
			if (!_playerList.ContainsKey(member))
				_playerList.Add(member, new playerData());

			return _playerList[member];
		}

		private void SaveRaidData()
		{
			Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerList", _playerList);
		}

		private void LoadRaidData()
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
		private void Init()
		{
			LoadRaidData();
		}

        private void OnServerInitialized()
        {
            cmd.AddChatCommand("raid", this, "RaidAlertsUI");
        }

		private void Unload()
		{
			SaveRaidData();
		}
        #endregion

        #region [Rust]
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || entity == null) return;

            BasePlayer player = info.InitiatorPlayer;
            if (player == null) return;

            if (Clans != null)
            {
                var IsFriend = (bool)Clans?.CallHook("IsTeammates", entity.OwnerID, player.userID);
                if (IsFriend) return;
            }

            if (entity is BuildingBlock)
            {
                int tier = (int)(entity as BuildingBlock).grade;
                if (tier <= 0) return;
                AlertsManager(entity, player, tier);
            }
            else if (entity is DecayEntity || entity is SamSite || entity is AutoTurret)
            {
                AlertsManager(entity, player);
            }
        }
        #endregion

        #region [AlertsManager]
        private void AlertsManager(BaseCombatEntity entity, BasePlayer player, int grade = 0)
        {
            if (entity == null || player == null) return;

            Vector3 entityPosition = entity.transform.position;
            string entityName = entity.ShortPrefabName;

            if (grade == 1) entityName += " Wood";
            else if (grade == 2) entityName += " Stone";
            else if (grade == 3) entityName += " Metal";
            else if (grade == 4) entityName += " TopTier";

            BuildingPrivlidge buildingPrivlidge = entity is BuildingPrivlidge ? entity as BuildingPrivlidge : entity.GetBuildingPrivilege(entity.WorldSpaceBounds());
            if (buildingPrivlidge == null) return;
            if (!buildingPrivlidge.AnyAuthed()) return;

            var playerList = buildingPrivlidge.authorizedPlayers.ToList();

            string displayName = covalence.Players.FindPlayer(player.UserIDString).Name;
            if (string.IsNullOrEmpty(displayName)) return;

            string quad = getGrid(entityPosition);
            string Text = string.Empty;

            if (TranslateRaidAlert.ContainsKey(entityName))
            {
                Text = $"{displayName} сломал {TranslateRaidAlert[entityName]}, квадрат {quad}.";
            }
            else
            {
                Text = $"{displayName} начал рейд, в квадрате {quad}.";
            }

            foreach (var playerAuth in playerList)
            {
                BasePlayer findPlayer = BasePlayer.FindByID(playerAuth.userid);
                var data = GetPlayerData(playerAuth.userid);

                if (findPlayer != null)
                {
                    if (data.gameCooldown < DateTime.Now)
                    {
                        NotifyUi(findPlayer, Text);
                        data.gameCooldown = DateTime.Now.AddSeconds(config._Settings.cooldownGame);
                    }

                    if (data.vkCooldown < DateTime.Now)
                    {
                        SendRequest(Text, data);
                        data.vkCooldown = DateTime.Now.AddSeconds(config._Settings.cooldDownVk);
                    }
                }
                else
                {
                    if (data.vkCooldown < DateTime.Now)
                    {
                        SendRequest(Text, data);
                        data.vkCooldown = DateTime.Now.AddSeconds(config._Settings.cooldDownVk);
                    }
                }
            }
        }
        #endregion

        #region [GUI]
        private void NotifyUi(BasePlayer player, String Text)
        {
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image = { Color = "0.5 0.5 0.5 0.25", Material = "assets/icons/greyout.mat" },
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-245.182 -155.661", OffsetMax = "-2.618 -102.735" },
                CursorEnabled = false,
            }, "Overlay", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".itemImage",
                Components =
                {
                    new CuiImageComponent {Color = "0.49 0.44 0.38 0.75", Material = "assets/icons/greyout.mat"},
                    new CuiRectTransformComponent { AnchorMin = "0.01586128 0.08839238", AnchorMax = "0.1925 0.9208925" }
                }
            });

			container.Add(new CuiElement
			{
				Parent = Layer + ".itemImage",
				Components =
				{
					new CuiImageComponent { ItemId = FindItemID(config._Settings.imageNotify), SkinId = 0 },
					new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
				}
			});

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent() { Color = "1 1 1 0.65", Text = $"Оповещение о рейде!", FontSize = 14, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent {AnchorMin = "0.215 0.585", AnchorMax = "1 1"},
                    new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.15 0.15"},
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent() { Color = "1 1 1 0.65", Text = Text, FontSize = 12, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.215 0", AnchorMax = "1 0.7" },
                    new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "0.15 0.15" },
                }
            });

            container.Add(new CuiButton
            {
                Button = { Color = "0.9 0 0 0.65", Material = "assets/icons/greyout.mat", Close = Layer },
                RectTransform = { AnchorMin = "0.94 0.725", AnchorMax = "0.995 0.98" }
            }, Layer, "CloseX");

            container.Add(new CuiElement
            {
                Parent = "CloseX",
                Components =
                {
                    new CuiTextComponent() { Color = "1 1 1 0.65", Text = $"✘", FontSize = 12, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"},
                    new CuiOutlineComponent{ Color = "0 0 0 1", Distance = "0.35 0.35" },
                }
            });

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
            timer.Once(config._Settings.destroyUi, () => CuiHelper.DestroyUi(player, Layer));
        }

        private void RaidAlertsUI(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" }
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Close = Layer }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-263 -173", OffsetMax = "267.5 173" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, Layer, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.8 0.02", AnchorMax = $"0.98 0.12" },
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Close = Layer },
                Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiTextComponent { Text = $"Оповещение о рейде", Color = "1 1 1 0.65", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.845", AnchorMax = $"1 1" },
                }
            });

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.125 0.645", AnchorMax = "0.875 0.715" },
                Image = { Color = "0 0 0 0.5" }
            }, Layer + ".Main", Layer + ".Main" + ".inputPanel");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.01 0", AnchorMax = "0.74 1" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Main" + ".inputPanel", Layer + ".Main" + ".inputPanel" + ".Text");

            container.Add(new CuiElement()
            {
                Parent = Layer + ".Main" + ".inputPanel" + ".Text",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        FontSize = 18,
                        Command = $"UI_RAID ",
                        Font = "robotocondensed-bold.ttf",
                        Text = "https://vk.com/",
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "1 1"
                    }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.74 0", AnchorMax = $"1 0.97" },
                Button = { Color = "0.67 0.95 0.60 0.55", Command = "" },
                Text = { Text = $"Привязать", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main" + ".inputPanel");

            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiTextComponent { Text = $"Чтобы привязать свой VK, скопируйте и вставьте ссылку, нажмите\nкнопку привязать.", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.535", AnchorMax = $"1 0.645" },
                }
            });

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }
        #endregion
    
        #region [ConsoleCommand]
        [ConsoleCommand("UI_RAID")]
        void cmdUiRaid(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;

            if (!args.HasArgs())
            {
                player.ChatMessage("Вы ничего не указали.");
                return;
            }

            string vkID = string.Empty;
            vkID = string.Join(" ", args.Args);

            if (!vkID.Contains("https://vk.com/") || vkID == "https://vk.com/")
            {
                player.ChatMessage("Вы не указали ссылку на станицу!");
                return;
            }

            var data = GetPlayerData(player.userID);
            if (data == null) return;

            string finishID = vkID.Remove(0, 15);

            data.vkID = finishID;
            player.ChatMessage($"Вы подключили свой аккаунт {finishID} к рейд оповещения!");
        }
        #endregion

        #region [Request]
        private void SendRequest(String Message, playerData data)
        {
            if (string.IsNullOrEmpty(data.vkID) || string.IsNullOrEmpty(config._Settings.tokenVk)) return;
            string request = $"https://api.vk.com/method/messages.send?domain={data.vkID}&message={Message}\nАйпи сервера{ConVar.Server.ip}:{ConVar.Server.port}&v=5.86&access_token={config._Settings.tokenVk}";
            webrequest.Enqueue(request, null, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    PrintError("Сообщение в вк не отправлено.");
                    data.vkID = string.Empty;
                    return;
                }
            }, this, Core.Libraries.RequestMethod.GET);
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

        private static string getGrid(Vector3 pos)
        {
            var letter = 'A';
            var x = Mathf.Floor((pos.x + ConVar.Server.worldsize / 2f) / 146.3f) % 26;
            var z = Mathf.Floor(ConVar.Server.worldsize / 146.3f) -
                    Mathf.Floor((pos.z + ConVar.Server.worldsize / 2f) / 146.3f);
            letter = (char)(letter + x);
            return $"{letter}{z}";
        }

        private Dictionary<string, string> TranslateRaidAlert = new Dictionary<string, string>
        {
            { "wall Stone", "вашу каменную стену"},
            { "wall.low Stone", "вашу каменную низкую стену"},
            { "wall.frame Stone", "ваш каменный настенный каркас"},
            { "foundation Stone", "ваш каменный фундамент"},
            { "roof Stone", "вашу каменную крышу"},
            { "wall.doorway Stone", "ваш каменный дверной проём"},
            { "foundation.steps Stone", "ваши каменные ступеньки"},
            { "block.stair.lshape Stone", "вашу каменную L-лестницу"},
            { "block.stair.ushape Stone", "вашу каменную U-лестницу"},
            { "foundation.triangle Stone", "ваш каменный треугольный фундамент"},
            { "wall.window Stone", "ваш каменное окно"},
            { "wall.half Stone", "вашу каменную полустену"},
            { "wall Metal", "вашу металлическую стену"},
            { "wall.low Metal", "вашу металлическую низкую стену"},
            { "wall.frame Metal", "ваш металлический настенный каркас"},
            { "foundation Metal", "ваш металлический фундамент"},
            { "roof Metal", "вашу металлическую крышу"},
            { "wall.doorway Metal", "ваш металлический дверной проём"},
            { "foundation.steps Metal", "ваши металлические ступеньки"},
            { "block.stair.lshape Metal", "вашу металлическую L-лестницу"},
            { "block.stair.ushape Metal", "вашу металлическую U-лестницу"},
            { "foundation.triangle Metal", "ваш металлический треугольный фундамент"},
            { "wall.window Metal", "ваше металлическое окно"},
            { "wall.half Metal", "вашу металлическую полустену"},
            { "wall TopTier", "вашу бронированную стену"},
            { "wall.low TopTier", "вашу бронированную низкую стену"},
            { "wall.frame TopTier", "ваш бронированный настенный каркас"},
            { "foundation TopTier", "ваш бронированный фундамент"},
            { "roof TopTier", "вашу бронированную крышу"},
            { "wall.doorway TopTier", "ваш бронированный дверной проём"},
            { "foundation.steps TopTier", "ваши бронированные ступеньки"},
            { "block.stair.lshape TopTier", "вашу бронированную L-лестницу"},
            { "block.stair.ushape TopTier", "вашу бронированную U-лестницу"},
            { "foundation.triangle TopTier", "ваш бронированный треугольный фундамент"},
            { "wall.window TopTier", "ваше бронированное окно"},
            { "wall.half TopTier", "вашу бронированную полустену"},
            { "wall Wood", "вашу деревянную стену"},
            { "wall.low Wood", "вашу деревянную низкую стену"},
            { "wall.frame Wood", "ваш деревянный настенный каркас"},
            { "foundation Wood", "ваш деревянный фундамент"},
            { "roof Wood", "вашу деревянную крышу"},
            { "wall.doorway Wood", "ваш деревянный дверной проём"},
            { "foundation.steps Wood", "ваши деревянные ступеньки"},
            { "block.stair.lshape Wood", "вашу деревянную L-лестницу"},
            { "block.stair.ushape Wood", "вашу деревянную U-лестницу"},
            { "foundation.triangle Wood", "ваш деревянный треугольный фундамент"},
            { "wall.window Wood", "ваше деревянное окно"},
            { "door.hinged.metal", "вашу металлическую дверь"},
            { "floor Wood", "ваш деревянный пол"},
            { "floor Metal", "ваш металлический пол"},
            { "door.hinged.wood", "вашу деревянную дверь"},
            { "floor Stone", "ваш каменный пол"},
            { "door.double.hinged.wood", "вашу двойную деревянную дверь"},
            { "door.double.hinged.metal", "вашу двойную металлическую дверь"},
            { "shutter.wood.a", "ваши деревянные ставни"},
            { "wall.frame.garagedoor", "вашу гаражную дверь"},
            { "wall.window.bars.wood", "вашу деревянную решетку"},
            { "floor.triangle Stone", "ваш каменный треугольный потолок"},
            { "wall.external.high.wood", "ваши высокие деревянные ворота"},
            {"autoturret_deployed", "вашу автоматическую турель"},
            {"sam_site_turret_deployed", "вашу ПВО"},
            { "door.double.hinged.toptier", "вашу двойную бронированную дверь"},
            { "floor.triangle Metal", "ваш металлический треугольный потолок"},
            { "wall.frame.netting", "вашу сетчатую стену"},
            { "door.hinged.toptier", "вашу бронированную дверь"},
            { "shutter.metal.embrasure.a", "ваши металлические ставни"},
            { "wall.external.high.stone", "вашу высокую каменную стену"},
            { "gates.external.high.stone", "ваши высокие каменные ворота"},
            { "floor.ladder.hatch", "ваш люк с лестнице"},
            { "floor.grill", "ваш решетчатый настил"},
            { "floor.triangle Wood", "ваш деревянный треугольный потолок"},
            { "floor.triangle TopTier", "ваш бронированный треугольный потолок"},
            { "gates.external.high.wood", "ваши высокие деревянные ворота"},
            { "wall.half Wood", "вашу деревянную полустену"},
            { "floor TopTier", "ваш треугольный бронированный потолок"},
            { "wall.frame.cell", "вашу тюремную стену"},
            { "wall.window.bars.metal", "вашу металлическую решетку"},
            { "wall.frame.fence", "ваш сетчатый забор"},
            { "shutter.metal.embrasure.b", "вашу металлическую бойницу"},
            { "wall.window.glass.reinforced", "ваше окно из укрепленного стекла"},
            { "wall.frame.fence.gate", "вашу сетчатую дверь"},
            { "floor.frame Stone", "ваш каменный пол"},
            { "wall.frame.cell.gate", "вашу тюремную решетку"},
            { "floor.frame Metal", "ваш металический пол"},
            { "floor.frame Wood", "ваш деревянный пол" }
        };
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

        public class PluginSettings
        {
            [JsonProperty("Кд на отправку оповещений в игре [Минуты]")]
            public int cooldownGame;

            [JsonProperty("Кд на отправку оповещений в вк [Минуты]")]
            public int cooldDownVk;

            [JsonProperty("Время, через которое пропадает UI [секунды]")]
            public float destroyUi;

            [JsonProperty("Изображение какого ShortName будет на оповещение")]
            public string imageNotify;

            [JsonProperty("Токен группы вконтакте, указывайте верный [Если не хотите использовать опопвещения в вк оставляйте пустым]")]
            public string tokenVk;
        }

        private class PluginConfig
        {
            [JsonProperty("Основная настройка плагина")]
            public PluginSettings _Settings = new PluginSettings();

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _Settings = new PluginSettings()
                    {
                        cooldownGame = 2,
                        cooldDownVk = 4,
                        destroyUi = 15f,
                        imageNotify = "grenade.f1",
                        tokenVk = string.Empty,
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion
    }
}