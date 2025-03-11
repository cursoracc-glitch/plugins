using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Network;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
	[Info("Vehicle Manager", "WOLF-TOR", "1.0.0")]
	class VehicleManager : RustPlugin
	{
		#region Variables
		private static Configuration _config;
		public static VehicleManager _instance;
		#endregion

		#region Configuration
		public class Configuration
		{
			[JsonProperty("Разрешить подъем транспорта киянкой")]
			public bool allowPickupVehicle;

			[JsonProperty("shortname объекта который держит игрок при установке транспорта")]
			public string vehicleShortPrefabName;

			[JsonProperty("Список транспорта")]
			public List<VehicleInfo> vehicles;
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<Configuration>();
				this.SaveConfig();
			}
			catch
			{
				PrintError("Error reading config, please check!");
			}
		}

		protected override void LoadDefaultConfig()
		{
			_config = new Configuration();

			_config.allowPickupVehicle = true;
			_config.vehicleShortPrefabName = "box.wooden.large";
			_config.vehicles = new List<VehicleInfo>()
			{
				new VehicleInfo("boat", "assets/content/vehicles/boats/rowboat/rowboat.prefab", 2189173904, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("rhib", "assets/content/vehicles/boats/rhib/rhib.prefab", 2189175322, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("minicopter", "assets/content/vehicles/minicopter/minicopter.entity.prefab", 2189176096, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("balloon", "assets/prefabs/deployable/hot air balloon/hotairballoon.prefab", 2189176712, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("sedan", "assets/content/vehicles/sedan_a/sedantest.entity.prefab", 2189177307, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("horse", "assets/rust.ai/nextai/testridablehorse.prefab", 2189177940, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("ch47", "assets/prefabs/npc/ch47/ch47.entity.prefab", 2189180720, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("scraptransportheli", "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab", 2189181296, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("2module_car", "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab", 2162472804, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("3module_car", "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab", 2162478030, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab"),
				new VehicleInfo("4module_car", "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab", 2162478376, "assets/bundled/prefabs/fx/build/promote_TopTier.prefab")
			};
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(_config);
		}
		#endregion

		#region VehicleInfo Class
		public class VehicleInfo
		{
			[JsonProperty("Короткое имя транспорта")]
			public string shortname;

			[JsonProperty("prefab транспорта")]
			public string prefab;

			[JsonProperty("skinID транспорта")]
			public ulong skinID;

			[JsonProperty("Effect который проигрывается при установке транспорта")]
			public string placementEffect;

			public VehicleInfo(string shortname, string prefab, ulong skinID, string placementEffect = null)
			{
				this.shortname			= shortname;
				this.prefab				= prefab;
				this.skinID				= skinID;
				this.placementEffect	= placementEffect;
			}

			public string Give(BasePlayer player, string shortname, string text = null)
			{
				Item item = ItemManager.CreateByName(shortname, 1, this.skinID);
				if (item != null)
				{
					item.name = _instance._(player, this.shortname);

					if (!string.IsNullOrEmpty(text))
						item.text = text;

					player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
					return item.name;
				}

				return null;
			}

			public BaseEntity Spawn(Vector3 position, Quaternion rotation, BasePlayer player, Item ownerItem)
			{
				BaseEntity entity = GameManager.server.CreateEntity(this.prefab, position, rotation) as BaseEntity;

				if (entity != null)
				{
					entity.transform.Rotate(0, 90, 0);

					Interface.Oxide.CallHook("OnSpawnVehicle", player, entity, ownerItem);

					entity.Spawn();

					return entity;
				}

				return null;
			}

			public static VehicleInfo FindByShortname(string shortname)
			{
				if (shortname == null)
					return null;

				List<VehicleInfo> _vehicle = _config.vehicles.Where(v => v.shortname.Contains(shortname)).ToList();
				if (_vehicle != null && _vehicle.Count > 0)
					return _vehicle.First();

				return null;
			}

			public static VehicleInfo FindByPrefab(string prefab)
			{
				if (prefab == null)
					return null;

				List<VehicleInfo> _vehicle = _config.vehicles.Where(v => v.prefab == prefab).ToList();
				if (_vehicle != null && _vehicle.Count > 0)
					return _vehicle.First();

				return null;
			}

			public static VehicleInfo FindBySkinID(ulong skinID)
			{
				if (skinID == 0 || skinID == null)
					return null;

				List<VehicleInfo> _vehicle = _config.vehicles.Where(v => v.skinID == skinID).ToList();
				if (_vehicle != null && _vehicle.Count > 0)
					return _vehicle.First();

				return null;
			}

			public static List<string> ShortNameList() // Я хлебушек, мне так можно
			{
				List<string> _vehicles = new List<string>();
				foreach (VehicleInfo _vehicle in _config.vehicles)
					_vehicles.Add(_vehicle.shortname);

				return _vehicles;
			}
		}
		#endregion

		#region Language
		private void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["rhib"] = "RHIB",
				["minicopter"] = "MiniCopter",
				["boat"] = "Boat",
				["balloon"] = "Air balloon",
				["sedan"] = "Car",
				["horse"] = "Horse",
				["scraptransportheli"] = "Transport Helicopter",
				["ch47"] = "Chinook",
				["2module_car"] = "Small chassis",
				["3module_car"] = "Medium chassis",
				["4module_car"] = "Large chassis",

				["ConsoleSyntax"] = "Syntax: vehicle.give <steamid|username> <{0}> [shortname] [comment]",
				["PlayerNotFound"] = "Player '{0}' not found!",
				["VehicleNotFound"] = "Vehicle '{0}' not found!",
				["ShortnameNotFound"] = "Item shortname '{0}' not found!",
				["SuccessfullyGive"] = "Transport '{0}' successfully give to '{1}'",
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				["rhib"] = "Военный катер",
				["minicopter"] = "MiniCopter",
				["boat"] = "Лодка",
				["balloon"] = "Воздушный шар",
				["sedan"] = "Автомобиль",
				["horse"] = "Лошадь",
				["scraptransportheli"] = "Транспортный вертолет",
				["ch47"] = "Чинук",
				["2module_car"] = "Маленькое шасси",
				["3module_car"] = "Среднее шасси",
				["4module_car"] = "Большое шасси",

				["ConsoleSyntax"] = "Синтаксис: vehicle.give <steamid|username> <{0}> [shortname] [comment]",
				["PlayerNotFound"] = "Игрок с никнеймом '{0}' не найден!",
				["VehicleNotFound"] = "Транспорт с названием '{0}' не найден!",
				["ShortnameNotFound"] = "Item с shortname '{0}' не найден!",
				["SuccessfullyGive"] = "Транспорт '{0}' успешно выдан игроку '{1}'",
			}, this, "ru");
		}

		private string _(BasePlayer player, string key, params object[] args)
		{
			return string.Format(lang.GetMessage(key, this, player?.UserIDString), args);
		}
		#endregion

		#region Init
		private void OnServerInitialized()
		{
			_instance = this;

			foreach (BasePlayer player in BasePlayer.activePlayerList)
				foreach (Item item in player.inventory.AllItems())
					this.ChangeVehicleName(item, player);
		}

		private void Unload()
		{
			_instance = null;
			_config   = null;
		}
		#endregion

		#region OxideHooks
		private void OnPlayerConnected(BasePlayer player) 
		{
			player.inventory.AllItems().ToList().ForEach(it => this.ChangeVehicleName(it, player));
		}

		private void OnItemAddedToContainer(ItemContainer container, Item item) 
		{
			if (container == null || item == null || container.playerOwner == null)
				return;

			this.ChangeVehicleName(item, container.playerOwner);
		}

		private object OnNpcGiveSoldItem(NPCVendingMachine vending, Item soldItem, BasePlayer buyer) 
		{
			if (soldItem.skin > 0)
			{
				VehicleInfo vehicle = VehicleInfo.FindBySkinID(soldItem.skin);
				if (vehicle != null)
				{
					vehicle.Give(buyer, _config.vehicleShortPrefabName, "npcvending");
					return true;
				}
			}

			return null;
		}

		private object CanBuild(Planner plan, Construction construction, Construction.Target target) 
		{
			if (construction.fullName.Contains(_config.vehicleShortPrefabName))
			{
				Item ownerItem = plan.GetItem();
				if (ownerItem != null && ownerItem.info.shortname == _config.vehicleShortPrefabName)
				{
					VehicleInfo vehicle = VehicleInfo.FindBySkinID(ownerItem.skin);
					if (vehicle == null)
						return null;

					if (Interface.Oxide.CallHook("CanSpawnVehicle", target.player, ownerItem, plan, construction, target) != null)
						return true;

					BaseEntity baseEntity = construction.CreateConstruction(target, true);
					BaseEntity entity = vehicle.Spawn(baseEntity.transform.position, baseEntity.transform.rotation, target.player, ownerItem);
					entity.skinID = ownerItem.skin;

					NextTick(() =>
					{
						Interface.Oxide.CallHook("OnDoSpawnVehicle", target.player, entity, ownerItem);

						plan.PayForPlacement(target.player, construction);

						baseEntity.Kill();
					});

					if (!string.IsNullOrEmpty(vehicle.placementEffect))
						Effect.server.Run(vehicle.placementEffect, entity, 0u, Vector3.zero, Vector3.zero, null, false);

					return true;
				}
			}

			return null;
		}

		private object OnHammerHit(BasePlayer player, HitInfo info) 
		{
			if (!_config.allowPickupVehicle || player == null || info == null || info?.HitEntity == null) 
				return null;

			if (info.HitEntity is BaseVehicle && player.serverInput.IsDown(BUTTON.FIRE_SECONDARY))
			{
				VehicleInfo vehicle = VehicleInfo.FindBySkinID(info.HitEntity.skinID);
				if (vehicle == null)
				{
					vehicle = VehicleInfo.FindByPrefab(info.HitEntity.PrefabName);
					if (vehicle == null)
						return null;
				}

				if (Interface.Oxide.CallHook("CanPickupVehicle", player, info.HitEntity) != null)
					return true;

				string text = (string)Interface.Oxide.CallHook("OnPickupVehicle", player, info.HitEntity);
				vehicle.Give(player, _config.vehicleShortPrefabName, (string.IsNullOrEmpty(text) ? "pickup" : text));

				NextTick(() =>
				{
					info.HitEntity.Kill();
				});

				return false;
			}

			return null;
		}

		private object OnItemCustomName(int itemID, ulong skinID, string language = "ru") 
		{
			if (itemID == -932201673 && skinID == 0 && language == "ru")
			{
				return "Металлолом";
			}

			VehicleInfo vehicle = VehicleInfo.FindBySkinID(skinID);
			if (vehicle == null)
				return null;

			Dictionary<string, string>  messages = lang.GetMessages(language, this);
			if (!messages.ContainsKey(vehicle.shortname))
				return null;

			return messages[vehicle.shortname];
		}

		private void OnPlayerSetInfo(Connection connection, string key, string val)
		{
			if (key == "global.language")
			{
				NextTick(() =>
				{
					BasePlayer player = Player.FindById(connection.userid);
					if (player != null)
					{
						this.OnPlayerConnected(player);
					}
				});
			}
		}
		#endregion

		#region Console Hooks
		[ConsoleCommand("vehicle.give")]
		void ConsoleCommand_vehicleshop_give(ConsoleSystem.Arg arg)
		{
			BasePlayer p = arg?.Player() ?? null; 
			if (p != null && !p.IsAdmin) 
				return;

			if (!arg.HasArgs(2))
			{
				SendReply(arg, _(p, "ConsoleSyntax", string.Join("|",  VehicleInfo.ShortNameList())));
				return;
			}

			BasePlayer player = BasePlayer.Find(arg.GetString(0));
			if (player == null)
			{
				SendReply(arg, _(p, "PlayerNotFound", arg.GetString(0)));
				return;
			}

			VehicleInfo vehicle = VehicleInfo.FindByShortname(arg.GetString(1));
			if (vehicle == null)
			{
				SendReply(arg, _(p, "VehicleNotFound", arg.GetString(1)));
				SendReply(arg, _(p, "ConsoleSyntax", string.Join("|",  VehicleInfo.ShortNameList())));
				return;
			}

			string shortname = _config.vehicleShortPrefabName;
			if (arg.HasArgs(3) && arg.GetString(2) != "default")
			{
				ItemDefinition info = ItemManager.FindItemDefinition(arg.GetString(2));
				if (info == null) 
				{
					SendReply(arg, _(p, "ShortnameNotFound", arg.GetString(2)));
					return;
				}

				shortname = info.shortname;
			}

			SendReply(arg, _(p, "SuccessfullyGive", vehicle.Give(player, shortname, arg.GetString(3, "console")), player.displayName));
		}
		#endregion

		#region Helpers
		private void ChangeVehicleName(Item item, BasePlayer player) 
		{
			if (item == null || player == null || item?.info == null || item.info.shortname != _config.vehicleShortPrefabName || item.skin == 0)
				return;

			VehicleInfo vehicle = VehicleInfo.FindBySkinID(item.skin);
			if (vehicle == null)
				return;

			item.name = _(player, vehicle.shortname);
			player.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
		}
		#endregion
	}
}