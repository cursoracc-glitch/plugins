using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries.Covalence;
using Rust;

namespace Oxide.Plugins
{
    [Info("CaptureZone", "King", "1.0.0")]
    public class CaptureZone : RustPlugin
    {
        #region [Vars]
        [PluginReference] private Plugin ImageLibrary = null, Clans = null, MenuAlerts = null;
        private string[] _gatherHooks = {
            "OnDispenserGather",
            "OnDispenserBonus",
        };
        private static CaptureZone plugin;
        private Dictionary<String, DateTime> CooldownNotifyFarm = new Dictionary<String, DateTime>();
        private List<ulong> _CaptureCupboard = new List<ulong>();
        private List<ulong> _CaptureDropBox = new List<ulong>();
        private List<ulong> openUI = new List<ulong>();
        private Dictionary<string, int> _itemIds = new Dictionary<string, int>();
        #endregion

        #region [Data]
        public List<CaptureData> _CaptureList = new List<CaptureData>();
        public class CaptureData
        {
            public string captureName;

            public string nameClan;

            public int lastCapture;

            public ulong Cupboard;

            public ulong ResourseChest;

            public Vector3 capturePosition;
        }

		private void SaveCapture()
		{
			Interface.Oxide.DataFileSystem.WriteObject($"{Name}/CaptureList", _CaptureList);
		}

		private void LoadCapture()
		{
			try
			{
				_CaptureList = Interface.Oxide.DataFileSystem.ReadObject<List<CaptureData>>($"{Name}/CaptureList");
			}
			catch (Exception e)
			{
				PrintError(e.ToString());
			}

			if (_CaptureList == null) _CaptureList = new List<CaptureData>();
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

			LoadCapture();
		}

        private void OnServerInitialized()
        {
            GetCaptureZone();

            cmd.AddChatCommand("capture", this, "ChatCommandCaptureZoneUI");
            cmd.AddChatCommand("newcapture", this, "NewSpawnCaptureZone");
            cmd.AddChatCommand("removecapture", this, "DeleteCaptureZone");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/rMC4ulM.png", "button_capture_zone");
        }

		private void Unload()
		{
			SaveCapture();

            foreach (var key in _CaptureList)
                RemoveCaptureZone(key.capturePosition);

            _CaptureManager.RemoveAll(x =>
            {
                UnityEngine.Object.Destroy(x);
                return true;
            });

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                    CuiHelper.DestroyUi(player, "CaptureUI");

			plugin = null;
		}

		private void OnNewSave(string filename)
		{
			_CaptureList.Clear();
			SaveCapture();
		}
        #endregion

        #region [Rust]
        private void OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if(dispenser == null || player == null) return;

            var clan = GetClanTag(player.userID);
            if (string.IsNullOrEmpty(clan)) return;

            CaptureManager ActiveCaptureZone = FindCaptureZoneDistance(player.transform.position);
            if (ActiveCaptureZone == null || string.IsNullOrEmpty(ActiveCaptureZone._data.nameClan)) return;

            var memberList = GetClanMembers(ActiveCaptureZone._data.nameClan);
            if (memberList == null || memberList == new List<string>()) return;

            var entity = ActiveCaptureZone._entity;
            if (entity == null) return;
            var inventory = entity.GetComponent<StorageContainer>().inventory;
            if (inventory == null) return;

            var amount = Convert.ToInt32(item.amount * (config._ZoneSettings.CapturePrecent / 100f));
            if (amount <= 0) return;

            var itemToCreate = ItemManager.CreateByName(item.info.shortname, amount);
            if (itemToCreate.amount <= 0) return;

            itemToCreate.MoveToContainer(inventory);

			if (CooldownNotifyFarm.ContainsKey(ActiveCaptureZone._data.nameClan))
				if (CooldownNotifyFarm[ActiveCaptureZone._data.nameClan].Subtract(DateTime.Now).TotalSeconds >= 0) return;
            if (inventory.capacity == inventory.itemList.Count)
            {
                foreach (var key in memberList)
                {
                    var id = Convert.ToUInt64(key);
                    var memberClan = BasePlayer.FindByID(id);
                    if (memberClan == null || !memberClan.IsConnected) continue;

                    memberClan.ChatMessage($"Инвентарь территории {ActiveCaptureZone._data.captureName} переполен, залутайте его!");
                    CooldownNotifyFarm[ActiveCaptureZone._data.nameClan] = DateTime.Now.AddSeconds(5);
                }
            }
        }

		private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item) =>
			OnDispenserGather(dispenser, player, item);

        private object OnEntityGroundMissing(BaseEntity entity)
        {
            if (entity.OwnerID == 9997)
                return false;

            return null;
        }

		private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
		{
			if(entity.OwnerID == 9997)
				return true;
			
			return null;
		}

        object CanLootEntity(BasePlayer player, DropBox DropBox)
        {
            if (player == null || DropBox == null || !_CaptureDropBox.Contains(DropBox.net.ID.Value)) return null;

            var clanName = GetClanTag(player.userID);
            if (string.IsNullOrEmpty(clanName)) return false;

            CaptureManager ActiveCaptureZone = FindCaptureZoneDropBox(DropBox.net.ID.Value);
            if (ActiveCaptureZone != null)
            {
                if (clanName != ActiveCaptureZone._data.nameClan)
                    return false;
            }
            return null;
        }

        object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege == null || player == null || !_CaptureCupboard.Contains(privilege.net.ID.Value)) return null;

            CaptureManager ActiveCaptureZone = FindCaptureZoneCupboard(privilege.net.ID.Value);
            if (ActiveCaptureZone != null)
            {
                var clan = GetClanTag(player.userID);
                if (string.IsNullOrEmpty(clan))
                {
                    player.ChatMessage("У вас нет клана.");
                    return false;
                }

                if (ActiveCaptureZone.CaptureClan == clan)
                {
                    player.ChatMessage("Ваш клан уже захватывает данную территорию!");
                    return false;
                }

                var CaptureOtherZone = FindCaptureOtherClan(clan);
                if (CaptureOtherZone != null)
                {
                    player.ChatMessage("Ваш клан уже захватывает другую территорию, дождитесь завершения захвата.");
                    return false;
                }

                ActiveCaptureZone.StartCapture(clan, player);
                return false;
            }

            return null;
        }

        private object CanBuild(Planner builder, Construction prefab, Construction.Target target)
        {
            var player = builder.GetOwnerPlayer();
            if (player == null) return null;

            CaptureManager ActiveCaptureZone = FindCaptureZoneDistance(player.transform.position);
            if (ActiveCaptureZone != null)
            {
                if (Vector3.Distance(player.transform.position, ActiveCaptureZone.transform.position) < config._ZoneSettings.RadiusBuild)
                {
                    player.ChatMessage("Нельзя строится так близко к захвату!");
                    return false;
                }
            }

            return null;
        }
        #endregion

        #region [Capture]
        private void GetCaptureZone()
        {
            /*if (_CaptureList.Count == 0)
            {
                var MapSize = TerrainMeta.Size / 2;

                Dictionary<Vector3, String> ListPosition = new Dictionary<Vector3, String>()
                {
                    [new Vector3(-MapSize.x, 0, MapSize.z)] = "A", // Левая верхняя
                    [new Vector3(MapSize.x / 9, 0, MapSize.z)] = "B", // Центральная верхняя
                    [new Vector3(MapSize.x, 0, MapSize.z)] = "C", // Правая верхняя

                    [new Vector3(-MapSize.x, 0, MapSize.z / 9)] = "D", // Левая Центральная
                    [new Vector3(MapSize.x / 9, 0, MapSize.z / 9)] = "E", // Центральная Центральная
                    [new Vector3(MapSize.x, 0, MapSize.z / 9)] = "F", // Правая Центральная

                    [new Vector3(-MapSize.x, 0, -MapSize.z)] = "G", // Левая Нижняя
                    [new Vector3(MapSize.x / 9, 0, -MapSize.z)] = "H", // Центральная Нижняя
                    [new Vector3(MapSize.x, 0, -MapSize.z)] = "L", // Правая Нижняя
                };

                foreach (var key in ListPosition)
                {
                    var center = key.Key / 2;
                    Vector3 lastPosition = center;

                    for (int i = 0; i < 300; i++)
                    {
                        lastPosition = RandomCircle(lastPosition);
                        if (ValidPosition(ref lastPosition))
                        {
                            GetNewData(lastPosition, key.Value);
                            break;
                        }
                    }
                }
            }
            else
            {*/
                foreach (var key in _CaptureList)
                {
                    SpawnCaptureZone(key.capturePosition);
                }
            //}
        }

        private void GetNewData(Vector3 position, String Name)
        {
            _CaptureList.Add(new CaptureData
            {
                captureName = Name,
                nameClan = string.Empty,
                lastCapture = 0,
                Cupboard = 0,
                ResourseChest = 0,
                capturePosition = position,
            });

            SpawnCaptureZone(position);
        }
        #endregion

        #region [Prefab]
		public class PrefabCaptureZone
		{
			public Vector3 Position;
			public string ShortPrefabName;
			public Vector3 Rotation;

			public PrefabCaptureZone(string shortname, Vector3 pos, Vector3 rot)
			{
				ShortPrefabName = shortname; Position = pos; Rotation = rot;
			}
		}

		public List<PrefabCaptureZone> _PrefabCaptureZone = new List<PrefabCaptureZone>
		{
            // Foundation
            new PrefabCaptureZone("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(0, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(0, 0, 3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(0, 0, -3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(3, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(3, 0, 3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(3, 0, -3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(-3, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(-3, 0, 3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/foundation/foundation.prefab", new Vector3(-3, 0, -3), new Vector3(0, 0, 0)),

            // CupboardPositionRoom
            new PrefabCaptureZone("assets/prefabs/building core/wall/wall.prefab", new Vector3(1.5f, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall/wall.prefab", new Vector3(0, 0, 1.5f), new Vector3(0, 270, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall/wall.prefab", new Vector3(0, 0, -1.5f), new Vector3(0, 270, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(-1.5f, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/door.double.hinged/door.double.hinged.toptier.prefab", new Vector3(-1.5f, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab", new Vector3(1.1f, 0.1f, 0), new Vector3(0, 270, 0)),

            // Room
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(-4.5f, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(-4.5f, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(-4.5f, 0, 3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(-4.5f, 0, 3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(-4.5f, 0, -3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(-4.5f, 0, -3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(4.5f, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(4.5f, 0, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(4.5f, 0, 3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(4.5f, 0, 3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(4.5f, 0, -3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(4.5f, 0, -3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(0, 0, -4.5f), new Vector3(0, 90, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab", new Vector3(0, 0, -4.5f), new Vector3(0, 90, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(3, 0, -4.5f), new Vector3(0, 90, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(3, 0, -4.5f), new Vector3(0, 90, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(-3, 0, -4.5f), new Vector3(0, 90, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(-3, 0, -4.5f), new Vector3(0, 90, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(0, 0, 4.5f), new Vector3(0, 270, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab", new Vector3(0, 0, 4.5f), new Vector3(0, 270, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(3, 0, 4.5f), new Vector3(0, 270, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(3, 0, 4.5f), new Vector3(0, 270, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/wall.frame/wall.frame.prefab", new Vector3(-3, 0, 4.5f), new Vector3(0, 270, 0)),
            new PrefabCaptureZone("assets/prefabs/building/wall.frame.cell/wall.frame.cell.prefab", new Vector3(-3, 0, 4.5f), new Vector3(0, 270, 0)),

            //Floor
            new PrefabCaptureZone("assets/prefabs/building core/floor/floor.prefab", new Vector3(0, 3, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/floor/floor.prefab", new Vector3(3, 3, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/floor/floor.prefab", new Vector3(3, 3, 3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/floor/floor.prefab", new Vector3(3, 3, -3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/floor/floor.prefab", new Vector3(-3, 3, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/floor/floor.prefab", new Vector3(-3, 3, 3), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building core/floor/floor.prefab", new Vector3(-3, 3, -3), new Vector3(0, 0, 0)),

            //Rest
            new PrefabCaptureZone("assets/prefabs/deployable/signs/sign.pole.banner.large.prefab", new Vector3(0, 3, 0), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", new Vector3(-0.7f, 1.5f, 1.6f), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", new Vector3(0.7f, 1.5f, 1.6f), new Vector3(0, 0, 0)),
            new PrefabCaptureZone("assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", new Vector3(-0.7f, 1.5f, -1.6f), new Vector3(0, 180, 0)),
            new PrefabCaptureZone("assets/prefabs/building/ladder.wall.wood/ladder.wooden.wall.prefab", new Vector3(0.7f, 1.5f, -1.6f), new Vector3(0, 180, 0)),
            new PrefabCaptureZone("assets/prefabs/deployable/dropbox/dropbox.deployed.prefab", new Vector3(0.8f, 1.2f, 0), new Vector3(0, 90, 0)),
		};

		private void RemoveCaptureZone(Vector3 position)
		{
			List<BaseEntity> list_entity = new List<BaseEntity>();
			Vis.Entities(position, 25, list_entity);
			
			list_entity = list_entity.Distinct().ToList();
			list_entity = list_entity.Where(x => !(x is BasePlayer) && !(x is PlayerCorpse) && !(x is DroppedItemContainer) && !(x is DroppedItem)).ToList();
			
			foreach(BaseEntity entity in list_entity)
				if(entity != null && !entity.IsDestroyed)
					entity.Kill();
		}

		private void SpawnCaptureZone(Vector3 position)
		{
            RemoveCaptureZone(position);

            var find = _CaptureList.FirstOrDefault(p => p.capturePosition == position);
            if (find == null) return;

			foreach(var key in _PrefabCaptureZone)
			{
				BaseEntity prefab = GameManager.server.CreateEntity(key.ShortPrefabName, position + key.Position, Quaternion.Euler(key.Rotation)) as BaseEntity;
				
				prefab.OwnerID = 9997;
				prefab.Spawn();
				
				if (prefab is BuildingBlock)
				{
					BuildingBlock block = prefab as BuildingBlock;
					
					block.grade = BuildingGrade.Enum.TopTier;
					block.SetHealthToMax();
				}

                if (prefab is BuildingPrivlidge)
                {
                    BuildingPrivlidge build = prefab as BuildingPrivlidge;

                    _CaptureCupboard.Add(build.net.ID.Value);
                    find.Cupboard = build.net.ID.Value;
                }

                if (prefab is DropBox)
                {
                    DropBox dropbox = prefab as DropBox;
                    dropbox.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                    dropbox.inventory.capacity = 40;

                    _CaptureDropBox.Add(dropbox.net.ID.Value);
                    find.ResourseChest = dropbox.net.ID.Value;
                    var obj = new GameObject();
                    obj.transform.position = dropbox.transform.position;
                    var component = obj.AddComponent<CaptureManager>();
                    component.GetComponent<CaptureManager>().Init(find, dropbox);
                    _CaptureManager.Add(component);
                }

                if (prefab is Door)
                {
                    Door door = prefab as Door;

                    door.pickup.enabled = false;
                    door.canTakeLock = false;
                    door.canTakeCloser = false;
                    door.SendNetworkUpdateImmediate();
                }

                if (prefab is BaseLadder)
                {
                    BaseLadder ladder = prefab as BaseLadder;

                    ladder.pickup.enabled = false;
                }
			}
		}
        #endregion

        #region [UI]
        private void ChatCommandCaptureZoneUI(BasePlayer player) => CaptureZoneUI(player, 0);

        private void CaptureZoneUI(BasePlayer player, int page)
        {
            var container = new CuiElementContainer();
            string Layer = "CaptureZone_UI";
            string colored = "0 0 0 0.5";

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

            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiTextComponent { Text = $"Список территорий", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.915", AnchorMax = $"1 1" },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.00325 0.01", AnchorMax = $"0.035 0.067" },
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Command = page >= 1 ? $"UI_CAPTURE ChangePageCapture {page - 1}" : "" },
                Text = { Text = $"-", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.0395 0.01", AnchorMax = $"0.07125 0.067" },
                Image = { Color = "0.2 0.2 0.2 0.65", Material = "assets/icons/greyout.mat" }
            }, Layer + ".Main", Layer + ".Main" + ".pText");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                Text = { Text = $"{page + 1}", Color = "1 1 1 0.85", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter }
            }, Layer + ".Main" + ".pText");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.07575 0.01", AnchorMax = $"0.1075 0.067" },
                Button = { Color = "0.46 0.44 0.42 0.65", Material = "assets/icons/greyout.mat", Command = _CaptureManager.Skip(9 * (page + 1)).Count() > 0 ? $"UI_CAPTURE ChangePageCapture {page + 1}" : "" },
                Text = { Text = $"+", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.890915 0.01", AnchorMax = $"0.99379 0.067", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Close = Layer },
                Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.86", AnchorMax = "0.999 0.92" },
                Image = { Color = "1 1 1 0" }
            }, Layer + ".Main", Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"НАЗВАНИЕ ТЕРРИТОРИИ", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.025 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"ВЛАДЕЛЕЦ ТЕРРИТОРИИ", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.23 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"СОСТОЯНИЕ ТЕРРИТОРИИ", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.42 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            container.Add(new CuiLabel
            {
                Text = { Text = $"СОБРАТЬ РЕСУРСЫ", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = $"0.85 0", AnchorMax = $"1 1" },
            }, Layer + ".Main" + ".Text");

            for (int y = 0; y < 9; y++)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.0055 {0.775 - y * 0.085}", AnchorMax = $"0.989 {0.85 - y * 0.085}" },
                    Image = { Color = colored }
                }, Layer + ".Main", Layer + ".Main" + $".TopLine{y}");
            }

            int i = 0;
            foreach (var key in _CaptureManager.Skip(page * 9).Take(9))
            {
                string nameClan = key._data.nameClan;
                if (string.IsNullOrEmpty(key._data.nameClan))
                    nameClan = "Никто";
                bool isTime = Facepunch.Math.Epoch.Current - key._data.lastCapture < config._ZoneSettings.CaptureCooldown;
                string State = key._IsCapture == true ? "Идет захват" : isTime == true ? $"{GetFormatTime(TimeSpan.FromSeconds(key._data.lastCapture + config._ZoneSettings.CaptureCooldown - Facepunch.Math.Epoch.Current))}" : "Можно захватить";

                container.Add(new CuiLabel
                {
                    Text = { Text = $"{key._data.captureName}", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.027 0", AnchorMax = $"1 1" },
                }, Layer + ".Main" + $".TopLine{i}");

                container.Add(new CuiLabel
                {
                    Text = { Text = $"{nameClan}", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.234 0", AnchorMax = $"1 1" },
                }, Layer + ".Main" + $".TopLine{i}");

                container.Add(new CuiLabel
                {
                    Text = { Text = $"{State}", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft },
                    RectTransform = { AnchorMin = $"0.424 0", AnchorMax = $"1 1" },
                }, Layer + ".Main" + $".TopLine{i}");

                container.Add(new CuiLabel
                {
                    Text = { Text = $"Открыть", Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                    RectTransform = { AnchorMin = $"0.85 0", AnchorMax = $"0.985 0.98" },
                }, Layer + ".Main" + $".TopLine{i}");

                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"UI_CAPTURE OpenInventory {key._data.ResourseChest}" },
                    RectTransform = { AnchorMin = $"0.85 0", AnchorMax = $"0.985 0.98" },
                }, Layer + ".Main" + $".TopLine{i}");

                i++;
            }

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }

        private void CaptureZoneUI_Inventory(BasePlayer player, StorageContainer containers, CaptureManager Capture)
        {
            var container = new CuiElementContainer();
            var Items = containers.inventory.itemList.Count;
            string Layer = "CaptureZone_UI";
            string colored = "0 0 0 0.5";

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

            container.Add(new CuiElement
            {
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiTextComponent { Text = $"Ивентарь ящика территории {Capture._data.captureName}", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.915", AnchorMax = $"1 1" },
                }
            });

            if (Items == 0)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer + ".Main",
                    Components =
                    {
                        new CuiTextComponent { Text = $"Инвентарь к сожалению пуст :(", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 32, Font = "robotocondensed-bold.ttf" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"1 1" },
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.890915 0.01", AnchorMax = $"0.99379 0.067", OffsetMax = "0 0" },
                    Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Close = Layer },
                    Text = { Text = $"ЗАКРЫТЬ", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, Layer + ".Main");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.77804 0.01", AnchorMax = $"0.880915 0.067", OffsetMax = "0 0" },
                    Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Command = "UI_CAPTURE ReturnToMenu" },
                    Text = { Text = $"НАЗАД", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf" }
                }, Layer + ".Main");

                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.AddUi(player, container);
                return;
            }

            for (int i = 0, y = 0, x = 0; i < 40; i++)
            {
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{0.01 + (x * 0.123)} {0.76 - (y * 0.17)}", AnchorMax = $"{0.117 + (x * 0.123)} {0.92 - (y * 0.17)}" },
                    Image = { Color = colored }
                }, Layer + ".Main", Layer + ".Main" + $"Item{i}");

                if (Items - 1 >= i)
                {
                    var Item = containers.inventory.itemList.ElementAt(i);
                    
                    container.Add(new CuiElement
                    {
                        Parent = Layer + ".Main" + $"Item{i}",
                        Components =
                        {
                            new CuiImageComponent { ItemId = FindItemID(Item.info.shortname), SkinId = 0 },
                            new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 0 ", AnchorMax = "1 1", OffsetMax = "-5 0", OffsetMin = "5 2" },
                        Button = { Color = "0 0 0 0", Command = $"UI_CAPTURE TakeItemInventory {Capture._data.ResourseChest} {Item.info.shortname}" },
                        Text = { Text = $"{Item.amount}" + "шт", Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                    }, Layer + ".Main" + $"Item{i}");
                }

                x++;
                if (x == 8)
                {
                    x = 0;
                    y++;
                }
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.77804 0.01", AnchorMax = $"0.880915 0.067", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.44 0.42 0.85", Material = "assets/icons/greyout.mat", Command = "UI_CAPTURE ReturnToMenu" },
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
        [ConsoleCommand("UI_CAPTURE")]
        private void CaptureUIHandler(ConsoleSystem.Arg args)
        {
			BasePlayer player = args?.Player();
			if (player == null || !args.HasArgs()) return;

            var clan = GetClanTag(player.userID);
            if (clan == null) return;

            switch (args.Args[0])
            {
                case "OpenInventory":
                {
                    var find = FindCaptureZoneDropBox(ulong.Parse(args.Args[1]));
                    if (find == null) return;

                    if (string.IsNullOrEmpty(clan))
                    {
                        player.ChatMessage("У вас нет клана.");
                        return;
                    }

                    if (find._data.nameClan != clan)
                    {
                        player.ChatMessage("Данная территория находится не под вашем контролем.");
                        return;
                    }

                    StorageContainer container;
                    container = find._entity.GetComponent<StorageContainer>();
                    container.inventory.MarkDirty();
                    container.UpdateNetworkGroup();
                    CaptureZoneUI_Inventory(player, container, find);
                    break;
                }
                case "TakeItemInventory":
                {
                    var find = FindCaptureZoneDropBox(ulong.Parse(args.Args[1]));
                    if (find == null || find._data.nameClan != clan) return;

                    StorageContainer container;
                    container = find._entity.GetComponent<StorageContainer>();

                    var findItem = container.inventory.itemList.FirstOrDefault(p => p.info.shortname == args.Args[2]);
                    if (findItem == null) return;

                    var item = ItemManager.CreateByName(findItem.info.shortname, findItem.amount, findItem.skin);
                    item.name = findItem.name;
                    item.MarkDirty();
                    player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                    findItem.RemoveFromContainer();
                    CaptureZoneUI_Inventory(player, container, find);
                    break;
                }
                case "ReturnToMenu":
                {
                    CaptureZoneUI(player, 0);
                    break;
                }
                case "ChangePageCapture":
                {
                    CaptureZoneUI(player, int.Parse(args.Args[1]));
                    break;
                }
            }
        }
        #endregion

        #region [Component]
        private CaptureManager FindCaptureZoneDistance(Vector3 pos) =>
             _CaptureManager.Where(p => Vector3.Distance(p.transform.position, pos) < config._ZoneSettings.RadiusZone).FirstOrDefault();

        private CaptureManager FindCaptureZoneCupboard(ulong netID) => 
             _CaptureManager.FirstOrDefault(p => p._data.Cupboard == netID);

        private CaptureManager FindCaptureZoneDropBox(ulong netID) => 
             _CaptureManager.FirstOrDefault(p => p._data.ResourseChest == netID);

        private CaptureManager FindCaptureOtherClan(string nameClan) => 
             _CaptureManager.FirstOrDefault(p => p.CaptureClan == nameClan);

        private static List<CaptureManager> _CaptureManager = new List<CaptureManager>();

        public class CaptureManager : MonoBehaviour
        {
            private List<BasePlayer> _Players;
            private MapMarkerGenericRadius mapMarker;
            private VendingMachineMapMarker vendingMarker;
            private SphereCollider sphereCollider;

            public CaptureData _data;
            public BaseEntity _entity;
            public bool _IsCapture = false;
            public int CaptureTime = 0;
            public string CaptureClan = string.Empty;

            private void Awake()
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.gameObject.layer = (int)Rust.Layer.Reserved1;
                sphereCollider.isTrigger = true;
                sphereCollider.radius = config._ZoneSettings.RadiusZone;
                InvokeRepeating("Timer", 1f, 1f);
            }

            private void OnTriggerEnter(Collider other)
            {
                var target = other.GetComponentInParent<BasePlayer>();
                if (target == null || _Players.Contains(target)) return;
                var Text = _data.nameClan == string.Empty ? "Данная территория не захвачена, успей захватить" : $"Вы зашли в территорию {_data.captureName}.\nПод владением клана: {_data.nameClan}";
                target.ChatMessage($"{Text}");
                _Players.Remove(target);
            }

            private void OnTriggerExit(Collider other)
            {
                var target = other.GetComponentInParent<BasePlayer>();
                if (target == null && !_Players.Contains(target)) return;
                var Text = _data.nameClan == string.Empty ? "Вы вышли из территории которая не захвачена, успей захватить" : $"Вы вышли из территории {_data.captureName}.\nПод владением клана: {_data.nameClan}";
                target.ChatMessage($"{Text}");
                _Players.Remove(target);
            }

            public void Init(CaptureData data, BaseEntity entity)
            {
                _data = data;
                _entity = entity; 
                _Players = new List<BasePlayer>();

                if (Facepunch.Math.Epoch.Current - _data.lastCapture < config._ZoneSettings.CaptureCooldown)
                    AddMarker(config._MarkerInGameSettings.markerColorCantCapture);
                else
                    AddMarker(config._MarkerInGameSettings.markerColorCanCapture);
            }

            private void Timer()
            {
                if (_IsCapture)
                {
                    bool HavePlayerCapture = false;

                    var _clanList = plugin.GetClanMembers(CaptureClan);
                    if (_clanList == null || _clanList == new List<string>())
                    {
                        CaptureClan = string.Empty;
                        _IsCapture = false;
                        CaptureTime = 0;
                        return;
                    }

                    foreach (var key in _clanList)
                    {
                        var id = Convert.ToUInt64(key);
                        var memberClan = BasePlayer.FindByID(id);
                        if (memberClan == null || !memberClan.IsConnected) continue;

                        if (Vector3.Distance(memberClan.transform.position, transform.position) < config._ZoneSettings.RadiusCapture)
                        {
                            HavePlayerCapture = true;
                            break;
                        }
                    }

                    if (!HavePlayerCapture)
                    {
                        CaptureClan = string.Empty;
                        _IsCapture = false;
                        CaptureTime = 0;

                        foreach (var key in _clanList)
                        {
                            var id = Convert.ToUInt64(key);
                            var memberClan = BasePlayer.FindByID(id);
                            if (memberClan == null || !memberClan.IsConnected) continue;

                            if (config.useMenuAlerts)
                            {
                                plugin.MenuAlerts?.Call("RemoveAlertMenu", memberClan, $"{plugin.Name}");
                            }
                            else
                            {
                                CuiHelper.DestroyUi(memberClan, "CaptureUI");
                                if (plugin.openUI.Contains(memberClan.userID))
                                    plugin.openUI.Remove(memberClan.userID);
                            }
                            memberClan.ChatMessage("Ваш клан ушел слишком далеко. Ваш захват сбит, начните захват заново!");
                        }
                        
                        AddMarker(config._MarkerInGameSettings.markerColorCanCapture);
                    }

                    CaptureTime++;
                    if (CaptureTime >= config._ZoneSettings.CaputureSecond)
                    {
                        FinishCapture();
                    }
                    else
                    {
                        if (!config.useMenuAlerts)
                        {
                            foreach (var key in _clanList)
                            {
                                var id = Convert.ToUInt64(key);
                                var memberClan = BasePlayer.FindByID(id);
                                if (memberClan == null || !memberClan.IsConnected) continue;

                                if (plugin.openUI.Contains(memberClan.userID))
                                    plugin.CaptureZoneInfoUpdate(memberClan, this);
                            }
                        }
                    }
                }

                if (Facepunch.Math.Epoch.Current - _data.lastCapture == config._ZoneSettings.CaptureCooldown && !_IsCapture)
                {
                    plugin.Server.Broadcast($"<color=#ffde5a>ВНИМАНИЕ!</color>\n<size=12>Территорию <color=#ffde5a>{_data.captureName}</color> можно снова захватить!");
                    mapMarker.color1 = ConvertToColor(config._MarkerInGameSettings.markerColorCanCapture);
                }

                mapMarker.SendUpdate();
            }

            public void StartCapture(String tag, BasePlayer player)
            {
                if (tag == CaptureClan)
                {
                    player.ChatMessage("Ваш клан уже захватывает эту территорию!");
                    return;
                }

                if (Facepunch.Math.Epoch.Current - _data.lastCapture < config._ZoneSettings.CaptureCooldown)
                {
                    var time = TimeSpan.FromSeconds(_data.lastCapture + config._ZoneSettings.CaptureCooldown - Facepunch.Math.Epoch.Current);
                    player.ChatMessage($"Данная территория еще находится на откате. Подождите {time.Hours}час, {time.Minutes}мин, {time.Seconds}сек.");
                    return;
                }

                if (_IsCapture)
                {
                    var _clanList = plugin.GetClanMembers(CaptureClan);
                    if (_clanList == null || _clanList == new List<string>()) return;

                    foreach (var key in _clanList)
                    {
                        var id = Convert.ToUInt64(key);
                        var memberClan = BasePlayer.FindByID(id);
                        if (memberClan == null || !memberClan.IsConnected) continue;

                        memberClan.ChatMessage($"Ваш захват перехватил клан: {tag}");
                        if (config.useMenuAlerts)
                        {
                            plugin.MenuAlerts?.Call("RemoveAlertMenu", memberClan, $"{plugin.Name}");
                        }
                        else
                        {
                                if (plugin.openUI.Contains(memberClan.userID))
                                    plugin.openUI.Remove(memberClan.userID);
                                CuiHelper.DestroyUi(memberClan, "CaptureUI");
                        }
                    }
                }
                else
                {
                    plugin.Server.Broadcast($"<color=#ffde5a>ВНИМАНИЕ!</color>\n<size=12>Начался захват территории <color=#ffde5a>{_data.captureName}</color>.\nЗахватывается кланом <color=#ffde5a>{tag}</color>");
                    mapMarker.color1 = ConvertToColor(config._MarkerInGameSettings.markerColorCapture);
                    vendingMarker.markerShopName = $"Территория {_data.captureName}\nИдет захват кланом: {tag}";
                    vendingMarker.SendNetworkUpdate();
                }

                var clanList = plugin.GetClanMembers(tag);
                if (clanList == null || clanList == new List<string>()) return;

                foreach (var key in clanList)
                {
                    var id = Convert.ToUInt64(key);
                    var memberClan = BasePlayer.FindByID(id);
                    if (memberClan == null || !memberClan.IsConnected) continue;

                    if (config.useMenuAlerts)
                    {
                        plugin.MenuAlerts?.Call("SendAlertMenu", memberClan, Facepunch.Math.Epoch.Current,(int)config._ZoneSettings.CaputureSecond, $"CAPTURE ZONE", $"Вы начали захват территории: (<color=#ffde5a>{_data.captureName}</color>). По окончании времени вы получите (<color=#ffde5a>{config._ZoneSettings.HowGivePoint}</color>) клановых очков. Не отходите от захвата на (<color=#ffde5a>{config._ZoneSettings.RadiusCapture}</color>) м.", true, "button_capture_zone", $"{plugin.Name}");
                    }
                    else
                    {
                        plugin.CaptureZoneMain(memberClan, this);
                    }
                }

                CaptureClan = tag;
                CaptureTime = 0;
                _IsCapture = true;
            }

            private void FinishCapture()
            {
                CaptureTime = 0;
                _IsCapture = false;

                var _clanList = plugin.GetClanMembers(CaptureClan);
                if (_clanList == null || _clanList == new List<string>())
                {
                    CaptureClan = string.Empty;
                    mapMarker.color1 = ConvertToColor(config._MarkerInGameSettings.markerColorCanCapture);
                    return;
                }

                foreach (var key in _clanList)
                {
                    var id = Convert.ToUInt64(key);
                    var memberClan = BasePlayer.FindByID(id);
                    if (memberClan == null || !memberClan.IsConnected) continue;

                    if (plugin.openUI.Contains(memberClan.userID))
                        plugin.openUI.Remove(memberClan.userID);
                    CuiHelper.DestroyUi(memberClan, "CaptureUI");
                    memberClan.ChatMessage($"Ваш клан успешно захватил территорию {_data.captureName}");
                }

                plugin.Clans?.Call("GiveClanPoints", CaptureClan, config._ZoneSettings.HowGivePoint);
                _data.nameClan = CaptureClan;
                vendingMarker.markerShopName = $"Территория: {_data.captureName}\nКлан: {_data.nameClan}";
                vendingMarker.SendNetworkUpdate();
                _data.lastCapture = Facepunch.Math.Epoch.Current;
                CaptureClan = string.Empty;
                mapMarker.color1 = ConvertToColor(config._MarkerInGameSettings.markerColorCantCapture);
                plugin.Server.Broadcast($"<color=#ffde5a>ВНИМАНИЕ!</color>\n<size=12>Клан <color=#ffde5a>{_data.nameClan}</color> захватил территорию <color=#ffde5a>{_data.captureName}</color>.");
            }

            private void RemoveMarker()
            {
                if (mapMarker != null && !mapMarker.IsDestroyed) 
                    mapMarker.Kill();
                if (vendingMarker != null && !vendingMarker.IsDestroyed) 
                    vendingMarker.Kill();
            }

            private void AddMarker(string color)
            {
                RemoveMarker();

                string nameClan = _data.nameClan;
                if (string.IsNullOrEmpty(_data.nameClan))
                    nameClan = "Никто";

                mapMarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", transform.position).GetComponent<MapMarkerGenericRadius>();
                vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", transform.position).GetComponent<VendingMachineMapMarker>();

                mapMarker.radius = config._MarkerInGameSettings.markerRadius;
                mapMarker.color1 = ConvertToColor(color);
                mapMarker.alpha = config._MarkerInGameSettings.markerAlpha;
                mapMarker.enabled = true;
                mapMarker.OwnerID = 0;
                mapMarker.Spawn();
                mapMarker.SendUpdate();

                vendingMarker.markerShopName = $"Территория: {_data.captureName}\nКлан: {nameClan}";
                vendingMarker.OwnerID = 0;
                vendingMarker.Spawn();
                vendingMarker.enabled = false;
            }

            public void DestroyComp() => OnDestroy();
            private void OnDestroy()
            {
                RemoveMarker();
                Destroy(this);
            }

            private Color ConvertToColor(string color)
            {
                if (color.StartsWith("#")) color = color.Substring(1);
                int red = int.Parse(color.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(color.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(color.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return new Color((float)red / 255, (float)green / 255, (float)blue / 255);
            }
        }
        #endregion

        #region [GUI]
        [ConsoleCommand("OpenPanelCapture")]
        private void cmdOpenCaptureZone(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null || !args.HasArgs() || openUI.Contains(player.userID)) return;

            CaptureManager Component = FindCaptureZoneCupboard(ulong.Parse(args.Args[0]));
            if (Component == null) return;

            CaptureZoneInfo(player, Component);
            CaptureZoneInfoUpdate(player, Component);
            openUI.Add(player.userID);
        }

        [ConsoleCommand("ClosePanelCapture")]
        private void cmdCloseCaptureZone(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null || !openUI.Contains(player.userID)) return;

            CuiHelper.DestroyUi(player, "CaptureUI" + ".Info");
            openUI.Remove(player.userID);
        }

        public void CaptureZoneMain(BasePlayer player, CaptureManager Component)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "1 0.5", AnchorMax = "1 0.5", OffsetMin = "-42.5 2", OffsetMax = "-2 40.5" },
                Image = { Color = "0.5 0.5 0.5 0.25", Material = "assets/icons/greyout.mat" }
            }, "Overlay", "CaptureUI");

            container.Add(new CuiElement
            {
                Parent = "CaptureUI",
                Components =
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary?.Call("GetImage", "button_capture_zone") },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "4 4", OffsetMax = "-4 -4"},
                    new CuiOutlineComponent{Color = "0 0 0 1", Distance = "0.15 0.15"}
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = {  Color = "0 0 0 0", Command = $"OpenPanelCapture {Component._data.Cupboard}" },
                Text = { Text = "" }
            }, "CaptureUI");

            CuiHelper.DestroyUi(player, "CaptureUI");
            CuiHelper.AddUi(player, container);
        }

        public void CaptureZoneInfo(BasePlayer player, CaptureManager Component)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-223.521 -19.5", OffsetMax = "-24.017 19.5" },
                Image = { Color = "0.5 0.5 0.5 0.25", Material = "assets/icons/greyout.mat" }
            }, "CaptureUI", "CaptureUI" + ".Info");

            container.Add(new CuiElement()
            {
                Parent = "CaptureUI" + ".Info",
                Components =
                {
                    new CuiTextComponent{Color = "1 1 1 1",Text = $"Происходит захват территории {Component._data.captureName}", Align = TextAnchor.MiddleLeft, FontSize = 11, Font = "robotocondensed-regular.ttf"},
                    new CuiRectTransformComponent{AnchorMin = "0.02 0", AnchorMax = "0.925 0.8"},
                    new CuiOutlineComponent {Color = "0 0 0 1", Distance = "0.4 0.4"},
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.925 0.625", AnchorMax = "0.995 0.97"},
                Button = { Command = $"ClosePanelCapture", Color = "0.9 0 0 0.65", Material = "assets/icons/greyout.mat" },
                Text = { Text = "✘", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-bold.ttf" }
            }, "CaptureUI" + ".Info");

            CuiHelper.DestroyUi(player, "CaptureUI" + ".Info");
            CuiHelper.AddUi(player, container);
        }

        public void CaptureZoneInfoUpdate(BasePlayer player, CaptureManager Component)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiElement()
            {
                Parent = "CaptureUI" + ".Info",
                Name = "CaptureUI" + ".Info" + ".TextUpdate",
                Components =
                {
                    new CuiTextComponent{Color = "1 1 1 1", Text = $"CAPTURE ZONE: ({GetFormatTime(TimeSpan.FromSeconds(config._ZoneSettings.CaputureSecond - Component.CaptureTime))})", Align = TextAnchor.UpperLeft, FontSize = 14, Font = "robotocondensed-bold.ttf"},
                    new CuiRectTransformComponent{AnchorMin = "0.02 0", AnchorMax = "0.925 0.96"},
                    new CuiOutlineComponent {Color = "0 0 0 1", Distance = "0.4 0.4"},
                }
            });

            CuiHelper.DestroyUi(player, "CaptureUI" + ".Info" + ".TextUpdate");
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region [Position]
        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] {
                "Terrain", "World", "Default", "Construction", "Deployed"
            }
            )) && !hit.collider.name.Contains("rock_cliff")) return Mathf.Max(hit.point.y, y);
            return y;
        }

        private bool ValidPosition(ref Vector3 randomPos)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(randomPos, Vector3.down, out hitInfo, 500f, Layers.Solid)) randomPos.y = hitInfo.point.y;
            else return false;
            if (WaterLevel.Test(randomPos, false, false)) return false;
            var colliders = new List<Collider>();
            Vis.Colliders(randomPos, 15f, colliders);
            if (colliders.Where(col => col.name.ToLower().Contains("prevent") && col.name.ToLower().Contains("building")).Count() > 0) return false;
            var entities = new List<BaseEntity>();
            Vis.Entities(randomPos, 15f, entities);
            if (entities.Where(ent => ent is BaseVehicle || ent is CargoShip || ent is BaseHelicopter || ent is BradleyAPC || ent is TreeEntity || ent is OreResourceEntity).Count() > 0) return false;
            var cupboards = new List<BuildingPrivlidge>();
            Vis.Entities(randomPos, 20f + 10f, cupboards);
            if (cupboards.Count > 0) return false;
            return true;
        }

        Vector3 RandomCircle(Vector3 center)
        {
            float ang = UnityEngine.Random.value * 360;
            Vector3 pos;
            pos.x = center.x + 25f * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.z = center.z + 25f * Mathf.Cos(ang * Mathf.Deg2Rad);
            pos.y = center.y;
            pos.y = GetGroundPosition(pos);
            return pos;
        }
        #endregion

        #region [Functional]
        private string GetFormatTime(TimeSpan timespan)
        {
            return string.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, System.Math.Floor(timespan.TotalHours));
        }

            private string GetColorLine(int count, int max)
            {
                float n = max > 0 ? (float)ColorLine.Length / max : 0;
                var index = (int)(count * n);
                if (index > 0) index--;
                return ColorLine[index];
            }

        private string[] ColorLine = { "1.00 1.00 1.00 1.00", "1.00 0.98 0.96 1.00", "1.00 0.97 0.92 1.00", "1.00 0.96 0.88 1.00", "1.00 0.94 0.84 1.00", "1.00 0.93 0.80 1.00", "1.00 0.91 0.76 1.00", "1.00 0.90 0.71 1.00", "1.00 0.89 0.67 1.00", "1.00 0.87 0.63 1.00", "1.00 0.85 0.59 1.00", "1.00 0.84 0.55 1.00", "1.00 0.83 0.51 1.00", "1.00 0.81 0.47 1.00", "1.00 0.80 0.43 1.00", "1.00 0.78 0.39 1.00", "1.00 0.77 0.35 1.00", "1.00 0.76 0.31 1.00", "1.00 0.74 0.27 1.00", "1.00 0.73 0.22 1.00", "1.00 0.71 0.18 1.00", "1.00 0.70 0.14 1.00", "1.00 0.68 0.10 1.00", "1.00 0.67 0.06 1.00", "1.00 0.65 0.02 1.00", "1.00 0.64 0.00 1.00", "1.00 0.61 0.00 1.00", "1.00 0.58 0.00 1.00", "1.00 0.55 0.00 1.00", "1.00 0.53 0.00 1.00", "1.00 0.50 0.00 1.00", "1.00 0.47 0.00 1.00", "1.00 0.45 0.00 1.00", "1.00 0.42 0.00 1.00", "1.00 0.40 0.00 1.00", "1.00 0.37 0.00 1.00", "1.00 0.35 0.00 1.00", "1.00 0.32 0.00 1.00", "1.00 0.29 0.00 1.00", "1.00 0.26 0.00 1.00", "1.00 0.24 0.00 1.00", "1.00 0.21 0.00 1.00", "1.00 0.18 0.00 1.00", "1.00 0.16 0.00 1.00", "1.00 0.13 0.00 1.00", "1.00 0.11 0.00 1.00", "1.00 0.08 0.00 1.00", "1.00 0.05 0.00 1.00", "1.00 0.03 0.00 1.00", "1.00 0.00 0.00 1.00" };

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

        private void NewSpawnCaptureZone(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length <= 0)
            {
                player.ChatMessage("Вы не указали название для территории.");
                return;
            }

            var position = player.transform.position;
            // position.y = GetGroundPosition(position);
            GetNewData(position, args[0]);

            player.ChatMessage($"Вы успешно создали территорию {args[0]}.");
        }

        private void DeleteCaptureZone(BasePlayer player, string command, string[] args)
        {
            if (args == null || args.Length <= 0)
            {
                player.ChatMessage($"Вы не указали название для территории. {_CaptureManager.Count}");
                return;
            }

            var find = _CaptureList.FirstOrDefault(p => p.captureName == args[0]);
            if (find == null) return;

            _CaptureCupboard.Remove(find.Cupboard);
            _CaptureDropBox.Remove(find.ResourseChest);
            RemoveCaptureZone(find.capturePosition);

            CaptureManager CaptureComponent = FindCaptureZoneCupboard(find.Cupboard);
            if (CaptureComponent != null)
            {
                CaptureComponent.DestroyComp();
                _CaptureManager.Remove(CaptureComponent);
            }

            _CaptureList.Remove(find);
            player.ChatMessage($"Вы успешно удалили территорию {args[0]}");
        }
        #endregion

        #region [Clans]
        private string GetClanTag(ulong id) => (string)Clans?.CallHook("GetClanTag", id);
        private List<string> GetClanMembers(string tag)
        {
            if (Clans)
            {
                var clan = Clans?.Call("GetMembersClan", tag) as List<string>;
                return clan.ToList();
            }
            return new List<string>();
        }
        #endregion

        #region [Config]
        private static PluginConfig config;

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

        public class MarkerInGameSettings
        {
            [JsonProperty("Радиус маркера")]
            public float markerRadius;

            [JsonProperty("Прозрачность маркера")]
            public float markerAlpha;

            [JsonProperty("Цвет маркера когда можно захватить")]
            public string markerColorCanCapture;

            [JsonProperty("Цвет маркера когда идет захват")]
            public string markerColorCapture;

            [JsonProperty("Цвет маркера когда нельзя захватить")]
            public string markerColorCantCapture;
        }

        public class ZoneSettings
        {
            [JsonProperty("Радиус территории")]
            public int RadiusZone;

            [JsonProperty("Радиус захвата территории ( Дистанция захвата, если игрок дальше захват прерывается )")]
            public int RadiusCapture;

            [JsonProperty("Радиус строительства территории ( Дистанция постройки, сколько метров от захвата нельзя строится )")]
            public int RadiusBuild;

            [JsonProperty("Сколько будет длится захват территории")]
            public int CaputureSecond;

            [JsonProperty("Сколько будет перезарядка между захватами")]
            public int CaptureCooldown;

            [JsonProperty("Сколько будет даваться очков за захват территории")]
            public int HowGivePoint;

            [JsonProperty("Процент налога фарма на территории")]
            public int CapturePrecent;
        }

        private class PluginConfig
        {
            [JsonProperty("Настройки территорий")]
            public ZoneSettings _ZoneSettings = new ZoneSettings();

            [JsonProperty("Настройки отметки на карте ( G )")] 
            public MarkerInGameSettings _MarkerInGameSettings = new MarkerInGameSettings();

            [JsonProperty("Использовать для меню плагин MenuAlerts ?")] 
            public bool useMenuAlerts;

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _ZoneSettings = new ZoneSettings()
                    {
                        RadiusZone = 100,
                        RadiusCapture = 10,
                        RadiusBuild = 20,
                        CaputureSecond = 300,
                        CaptureCooldown = 1200,
                        HowGivePoint = 250,
                        CapturePrecent = 25,
                    },
                    _MarkerInGameSettings = new MarkerInGameSettings()
                    {
                        markerRadius = 0.5f,
                        markerAlpha = 0.4f,
                        markerColorCanCapture = "#10c916",
                        markerColorCantCapture = "#ffb700",
                        markerColorCapture = "#ed0707"
                    },
                    useMenuAlerts = false,
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion
    }
}