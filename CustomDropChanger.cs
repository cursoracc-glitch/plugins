using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins {
	[Info("CustomDropChanger", "Server-Rust.ru", "1.1.4")]
	[Description("Позволяет добавить лут во все ящики, скачано с Server-Rust.ru")]
	class CustomDropChanger : RustPlugin {
		#region Поля

		private List<string> containerNames = new List<string> {
			"crate_basic",
			"crate_elite",
			"crate_mine",
			"crate_tools",
			"crate_normal",
			"crate_normal_2",
			"crate_normal_2_food",
			"crate_normal_2_medical",
			"crate_underwater_advanced",
			"crate_underwater_basic",
			"foodbox",
			"loot_barrel_1",
			"loot_barrel_2",
			"loot-barrel-1",
			"loot-barrel-2",
			"loot_trash",
			"minecart",
			"bradley_crate",
			"oil_barrel",
			"heli_crate",
			"codelockedhackablecrate",
			"supply_drop",
			"trash-pile-1"
		};

		private Dictionary<uint, int> conditionPendingList = new Dictionary<uint, int>();
		private Dictionary<ItemContainer, List<Item>> spawnedLoot = new Dictionary<ItemContainer, List<Item>>();
		private List<LootContainer> affectedContainers = new List<LootContainer>();
		private bool isReady = false;

		#endregion

		#region Конфиг

		private NewDropItemConfig config;

		private class ItemDropConfig {
			[JsonProperty("Название предмета (ShortName)")]
			public string ItemShortName;

			[JsonProperty("Минимальное количество предмета")]
			public int MinCount;

			[JsonProperty("Максимальное количество предмета")]
			public int MaxCount;

			[JsonProperty("Шанс выпадения предмета (0 - отключить)")]
			public int Chance;

			[JsonProperty("Это чертеж?")] public bool IsBluePrint;

			[JsonProperty("Имя предмета (оставьте пустым для стандартного)")]
			public string Name;

			[JsonProperty("Описание предмета (оставьте пустым для стандартного)")]
			public string Description;

			[JsonProperty("ID Скина (0 - стандартный)")]
			public ulong Skin;

			[JsonProperty("Целостность предмета в % от 1 до 100 (0 - стандартная)")]
			public int Condition;
		}

		private class NewDropItemConfig {
			[JsonProperty("Добавляем лут в ученых?")]
			public bool EnableScientistLoot { get; set; }

			[JsonProperty("Оставить стандартный лут в контейнерах и ученых?")]
			public bool EnableStandartLoot { get; set; }

			[JsonProperty("Список лута ученых:")]
			public List<ItemDropConfig> ScientistLootSettings { get; set; }

			[JsonProperty("Настройка контейнеров:")]
			public Dictionary<string, List<ItemDropConfig>> ChestSettings { get; set; }
		}

		protected override void LoadDefaultConfig() {
			var chestSettings = new Dictionary<string, List<ItemDropConfig>>();

			foreach (var container in containerNames) {
				chestSettings.Add(container,
					new List<ItemDropConfig> {
						new ItemDropConfig {
								MinCount = 1,
								MaxCount = 2,
								Chance = 40,
								ItemShortName = "researchpaper",
								IsBluePrint = false,
								Name = "",
								Description = "",
								Skin = 0,
								Condition = 0
							}
					});
			}

			config = new NewDropItemConfig {
				EnableScientistLoot = true,
				EnableStandartLoot  = true,
				ScientistLootSettings = new List<ItemDropConfig> {
					new ItemDropConfig {
								MinCount = 1,
								MaxCount = 2,
								Chance = 40,
								ItemShortName = "researchpaper",
								IsBluePrint = false,
								Name = "",
								Description = "",
								Skin = 0,
								Condition = 0
							}
				},
				ChestSettings = chestSettings
			};
			SaveConfig();
			PrintWarning("Creating default config");
		}

		protected override void LoadConfig() {
			base.LoadConfig();

			config = Config.ReadObject<NewDropItemConfig>();
		}

		protected override void SaveConfig() => Config.WriteObject(config);

		#endregion

		#region Загрузка и выгрузка

		private void Loaded() {
			var containers = UnityEngine.Object.FindObjectsOfType<LootContainer>();
			var count      = 0;

			foreach (var container in containers) {
				ProcessContainer(container);
				count++;
			}

			isReady = true;
			PrintWarning(
				$"Обновлено {count} контейнеров. Спасибо за приобретение плагина на https://DarkPlugins.ru. Использование с других сайтов, не гарантирует корректную работу");
		}

		void Unload() {
			foreach (var row in spawnedLoot)
			foreach (var loot in row.Value) {
				row.Key.Remove(loot);
			}

			foreach (var container in affectedContainers) {
				container.SpawnLoot();
			}
		}

		#endregion

		#region Логика

		private List<ItemDropConfig> GetItemListByChest(string chestname) {
			if (!config.ChestSettings.ContainsKey(chestname)) {
				PrintWarning($"Ящик с именем '{chestname}' не найден в конфиге!");
				return new List<ItemDropConfig>();
			}

			return config.ChestSettings[chestname];
		}

		private void AddToContainer(ItemDropConfig item, ItemContainer container) {
			var amount  = UnityEngine.Random.Range(item.MinCount, item.MaxCount);
			var newItem = item.IsBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.ItemShortName, amount, item.Skin);

			if (newItem == null) {
				PrintError($"Предмет {item.ItemShortName} не найден!");
				return;
			}

			if (item.IsBluePrint) {
				var bpItemDef = ItemManager.FindItemDefinition(item.ItemShortName);

				if (bpItemDef == null) {
					PrintError($"Предмет {item.ItemShortName} для создания чертежа не найден!");
					return;
				}

				newItem.blueprintTarget = bpItemDef.itemid;
			}

			if (!item.IsBluePrint && item.Condition != 0) {
				conditionPendingList.Add(newItem.uid, item.Condition);
			}

			if (item.Name != "") newItem.name = item.Name;
			if (item.Description != "") newItem.name += "\n <size=20>"+item.Description+"</size>";

			if (!spawnedLoot.ContainsKey(container)) spawnedLoot.Add(container, new List<Item>());
			spawnedLoot[container].Add(newItem);

			newItem.MoveToContainer(container);
		}

		private void ProcessScientist(ItemContainer container) {
			if (!config.EnableScientistLoot) return;

			if (!config.EnableStandartLoot) {
				container.Clear();
				var flag = 193;
				ItemManager.DoRemoves();
			}

			foreach (var item in config.ScientistLootSettings) {
				if (UnityEngine.Random.Range(1, 100) > item.Chance) continue;

				AddToContainer(item, container);
			} 
		}

		private void ProcessContainer(LootContainer container) {
			NextTick(() => {
				if (container == null || container.inventory == null) return;

				if (!config.EnableStandartLoot) {
					if (!affectedContainers.Contains(container)) affectedContainers.Add(container);
					container.inventory.itemList.Clear();
					ItemManager.DoRemoves();
				}

				foreach (var item in GetItemListByChest(container.ShortPrefabName)) {
					if (UnityEngine.Random.Range(1, 100) > item.Chance) continue;

					AddToContainer(item, container.inventory);
				}
			});
		}

		#endregion

		


		
		
		

		#region Хуки Oxide

		private void OnLootSpawn(LootContainer lootContainer) {
			if (!isReady) return;

			//if (!containerNames.Any(f => f == lootContainer.ShortPrefabName)) return;
			ProcessContainer(lootContainer);
		}

		private void OnEntitySpawned(BaseNetworkable entity) {
			NextTick(() => {
				if (entity == null || !(entity is NPCPlayerCorpse)) return;

				var npc = entity as NPCPlayerCorpse;
				var inv = npc.containers[0];

				ProcessScientist(inv);
			});
		}

		void OnItemAddedToContainer(ItemContainer container, Item item) {
			NextTick(() => {
				if (item == null) return;
				if (!conditionPendingList.ContainsKey(item.uid)) return;

				item.condition = item.info.condition.max / 100 * conditionPendingList[item.uid];

				conditionPendingList.Remove(item.uid);
			});
		}

		#endregion
	}
}
