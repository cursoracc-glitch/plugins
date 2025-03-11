using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins {
	[Info("SkinChanger", "rever", "1.0.0")]
	[Description("Меняет скины и имена предметов по их shortname")]
	class SkinChanger : RustPlugin {
		public class ItemRecord {
			[JsonProperty(PropertyName = "Shortname")]
			public string target;

			[JsonProperty(PropertyName = "Новое имя")]
			public string name;

			[JsonProperty(PropertyName = "Новый id скина")]
			public ulong skinid;
		}

		public class Configurarion {
			[JsonProperty(PropertyName = "Список предметов для замены скинов и имени")]
			public List<ItemRecord> items;
		}

		public Configurarion config;

		protected override void LoadDefaultConfig() {
			config = new Configurarion {
				items = new List<ItemRecord> {
					new ItemRecord {
						target = "sticks",
						name   = "Ёлочка",
						skinid = 1351406603
					}
				}
			};
			SaveConfig();
		}

		protected override void SaveConfig() => Config.WriteObject(config);

		private void Loaded() {
			try {
				config = Config.ReadObject<Configurarion>();
			} catch {
				LoadDefaultConfig();
			}
		}

		void OnItemAddedToContainer(ItemContainer container, Item item) {
			if (item == null || item.info == null) return;

			var name = item.info.shortname.ToLower();

			foreach (var configRow in config.items) {
				if (configRow.target.ToLower() != name || configRow.skinid == item.skin) continue;

				item.name = configRow.name;
				item.skin = configRow.skinid;
			}
		}

		void OnItemDropped(Item item, BaseEntity entity) { }
	}
}
