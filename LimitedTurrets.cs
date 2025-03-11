using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins {
	[Info("LimitedTurrets", "own3r/rever", "1.0.1")]
	[Description("Ограничение турелей")]
	class LimitedTurrets : RustPlugin {
		#region Поля

		[PluginReference] private Plugin ImageLibrary;

		public Dictionary<string, string> turretTypes = new Dictionary<string, string>() {
			{"autoturret_deployed", "Автоматическая турель"},
			{"guntrap.deployed", "Гантрап"},
			{"flameturret.deployed", "Огненная турель"}
		};

		public Dictionary<ulong, Dictionary<string, int>> turrets = new Dictionary<ulong, Dictionary<string, int>>();

		#endregion

		#region Конфигурация

		public class Configuration {
			[JsonProperty(PropertyName = "Версия конфига (не менять)")]
			public int version;

			[JsonProperty(PropertyName = "Привилегии")]
			public Dictionary<string, Dictionary<string, int>> privelegies { get; set; } = new Dictionary<string, Dictionary<string, int>>();

			[JsonProperty(PropertyName = "Разрешить привилегию limitedturrets.bypass для игнорирования лимитов")]
			public bool useBypass;

			[JsonProperty(PropertyName = "Разрешить админам игнорировать лимиты")]
			public bool allowAdmin;

			[JsonProperty(PropertyName = "Команда для открытия UI")]
			public string showUICmd;

			[JsonProperty(PropertyName = "Разрешить UI")]
			public bool allowUI;

			[JsonProperty(PropertyName = "Разрешить информирование в чате о изменении лимита")]
			public bool allowNotify;
		}

		public Configuration config;

		protected override void LoadDefaultConfig() {
			config = new Configuration {
				version = 1,
				privelegies = {
					{
						"limitedturrets.default", new Dictionary<string, int> {
							{"autoturret_deployed", 5},
							{"guntrap.deployed", 5},
							{"flameturret.deployed", 5}
						}
					}, {
						"limitedturrets.vip", new Dictionary<string, int> {
							{"autoturret_deployed", 15},
							{"guntrap.deployed", 15},
							{"flameturret.deployed", 15}
						}
					}
				},
				useBypass   = true,
				allowAdmin  = true,
				showUICmd   = "turret",
				allowUI     = true,
				allowNotify = true
			};
			SaveConfig();
		}

		protected override void SaveConfig() => Config.WriteObject(config);

		#endregion

		#region Инициализаця и выгрузка

		private void Loaded() {
			try {
				config = Config.ReadObject<Configuration>();
			} catch {
				LoadDefaultConfig();
			}

			if (config.useBypass) {
				permission.RegisterPermission($"limitedturrets.bypass", this);
			}

			foreach (var privelegy in config.privelegies) {
				permission.RegisterPermission(privelegy.Key, this);
			}

			if (config.allowUI) {
				cmd.AddChatCommand(config.showUICmd ?? "turret", this, cmdShowUI);
			}
		}

		private void OnServerInitialized() {
			if (!Interface.Oxide.DataFileSystem.ExistsDatafile(Title)) return;

			turrets = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, int>>>(Title);

			if (!ImageLibrary) {
				PrintError("Не найден плагин ImageLibrary - UI работать не будет!");
				return;
			}

			foreach (var type in turretTypes) {
				//@todo: словарь с URL иконок
				var name = type.Key.Replace("_deployed", "").Replace(".deployed", "");
				ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{name}.png", type.Key);
			}
		}

		void Unload() {
			SaveState();
		}

		#endregion

		#region Права/Привилегия

		void UpdatePermissions(string id, string name) {
			ulong intId;
			ulong.TryParse(id, out intId);
			if (!turrets.ContainsKey(intId)) return;

			foreach (var type in turretTypes) {
				var cnt = GetTurretCount(id, type.Key);
				if (!turrets[intId].ContainsKey(type.Key)) continue;

				var curr = turrets[intId][type.Key];

				if (cnt < curr) turrets[intId][type.Key] = cnt;
			}
		}

		void OnUserPermissionRevoked(string id, string perm) {
			UpdatePermissions(id, perm);
		}

		void OnUserPermissionGranted(string id, string perm) {
			UpdatePermissions(id, perm);
		}

		bool CheckBypass(BasePlayer player) {
			if (player == null) return false;
			if (config.allowAdmin && player.IsAdmin) return true;
			if (config.useBypass  && permission.UserHasPermission(player.UserIDString, "limitedturrets.bypass")) return true;

			return false;
		}

		#endregion

		#region UI

		public void ShowUIPanel(BasePlayer player) {
			if (player == null || !player.IsConnected || !ImageLibrary) return;

			if (player.IsReceivingSnapshot) {
				timer.Once(0.1f, () => { ShowUIPanel(player); });
				return;
			}

			var json =
				"[{\"name\":\"LTUIPanel\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.05274077 0.05567554 0.06511204 0.552046\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.315625 0.4370371\",\"anchormax\":\"0.5914066 0.6661676\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LTUIPImg1\",\"parent\":\"LTUIPanel\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{p1}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.04157076 0.199005\",\"anchormax\":\"0.3002312 0.747377\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LTUIPTxt1\",\"parent\":\"LTUIPanel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{t1}/{t1m}\",\"fontSize\":21,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.04618971 0.004484296\",\"anchormax\":\"0.3002312 0.202052\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LTUIPImg2\",\"parent\":\"LTUIPanel\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{p2}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3672055 0.1990049\",\"anchormax\":\"0.6258663 0.747377\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LTUIPTxt2\",\"parent\":\"LTUIPanel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{t2}/{t2m}\",\"fontSize\":21,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3718243 0\",\"anchormax\":\"0.6258663 0.202052\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LTUIPImg3\",\"parent\":\"LTUIPanel\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{p3}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6928407 0.199005\",\"anchormax\":\"0.9515015 0.747377\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LTUIPTxt3\",\"parent\":\"LTUIPanel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{t3}/{t3m}\",\"fontSize\":21,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.6817745 0\",\"anchormax\":\"0.9121802 0.1939699\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LTUITitle\",\"parent\":\"LTUIPanel\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Установлено турелей:\",\"fontSize\":22,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01154769 0.7668161\",\"anchormax\":\"0.9815242 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"LTUIBtn\",\"parent\":\"LTUIPanel\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"LTUIPClose\",\"color\":\"1 1 1 0\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2796875 0.4521484\",\"anchormax\":\"0.615625 0.6679688\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}]";
			var cb = new StringBuilder(json);

			var i = 1;

			foreach (var type in turretTypes) {
				var curr = 0;

				if (turrets.ContainsKey(player.userID)) {
					curr = turrets[player.userID][type.Key];
				}

				cb.Replace("{t" + i + "}", curr.ToString());
				cb.Replace("{t" + i + "m}", GetTurretCount(player.UserIDString, type.Key).ToString());
				cb.Replace("{p" + i + "}", (string)ImageLibrary?.Call("GetImage", type.Key) ?? "");
				i++;
			}

			CuiHelper.AddUi(player, cb.ToString());
		}

		#endregion

		#region Комманды

		private void cmdShowUI(BasePlayer player, string command, string[] args) {
			if (CheckBypass(player)) {
				SendReply(player, "У вас нет лимита на туррели!");
				return;
			}

			ShowUIPanel(player);
		}

		[ConsoleCommand("LTUIPClose")]
		void cmdLTUIPClose(ConsoleSystem.Arg arg) {
			var player = arg.Player();

			if (player == null || !player.IsConnected) return;

			CuiHelper.DestroyUi(player, "LTUIPanel");
		}

		[ConsoleCommand("lt.add")]
		void cmdAddPrivelege(ConsoleSystem.Arg arg) {
			//@todo: Доработать для автоформирования в соответствии с turretTypes
			var player = arg.Player();
			if (player == null || !player.IsAdmin) return;

			if (!arg.HasArgs() || arg.Args.Length != turretTypes.Count + 1) {
				SendReply(arg, "Синтаксис: lt.add ИмяПривилегии Автоматическая Гантрап Огненная");
				SendReply(arg, $"Пример: lt.add {Title.ToLower()}.premium 10 30 20");
				return;
			}

			var newRow = new Dictionary<string, int>();

			var i = 1;

			foreach (var type in turretTypes) {
				int cInt;
				int.TryParse(arg.Args[i], out cInt);
				newRow.Add(type.Key, cInt);
				i++;
			}

			config.privelegies.Add(arg.Args[0], newRow);
			SaveConfig();
			covalence.Server.Command($"o.reload {Title}");
		}

		[ConsoleCommand("lt.remove")]
		void cmdRemovePrivelege(ConsoleSystem.Arg arg) {
			var player = arg.Player();
			if (player == null || !player.IsAdmin) return;

			if (!arg.HasArgs() || arg.Args.Length != 1) {
				SendReply(arg, "Синтаксис: lt.remove ИмяПривилегии");
				SendReply(arg, $"Пример: lt.remove {Title.ToLower()}.premium");
				return;
			}

			if (!config.privelegies.ContainsKey(arg.Args[0])) {
				SendReply(arg, $"В конфиге не найдена привилегия '{arg.Args[0]}'");
				return;
			}

			config.privelegies.Remove(arg.Args[0]);
			SaveConfig();
			covalence.Server.Command($"o.reload {Title}");
		}

		[ConsoleCommand("lt.flush")]
		void cmdFlushUser(ConsoleSystem.Arg arg) {
			var player = arg.Player();
			if (player == null || !player.IsAdmin) return;

			if (!arg.HasArgs() || arg.Args.Length != 1) {
				SendReply(arg, "Синтаксис: lt.flush SteamId");
				SendReply(arg, "Пример: lt.flush 76561198058966464");
				return;
			}

			ulong steamId;
			ulong.TryParse(arg.Args[0], out steamId);

			if (steamId == 0) {
				SendReply(arg, $"Ошибка: '{arg.Args[0]}' не SteamId");
				return;
			}

			if (!turrets.ContainsKey(steamId)) {
				SendReply(arg, $"Ошибка: пользователь не найден");
				return;
			}
			turrets.Remove(steamId);
			SaveState();
			SendReply(arg, "Лимиты для пользователя сброшены");
		}

		#endregion

		#region Методы - основная реализация функционала

		string GetTurretType(string turretName) {
			foreach (var turretType in turretTypes) {
				if (turretName.Contains(turretType.Key)) return turretType.Key;
			}

			return null;
		}

		public int GetTurretCount(string playerId, string type) {
			var cntMax = 0;
			Puts("GetTurretCount");
			foreach (var privelegy in config.privelegies) {
				Puts(privelegy.Key);
				if (!permission.UserHasPermission(playerId, privelegy.Key) && privelegy.Key != "limitedturrets.default") continue;
				
				if (!privelegy.Value.ContainsKey(type)) {
					PrintWarning($"Для привилегии '{privelegy.Key}' не указанна турель '{type}!'");
					continue;
				}

				if (cntMax > privelegy.Value[type]) continue;

				cntMax = privelegy.Value[type];
			}

			return cntMax;
		}

		void CheckTurret(BaseEntity entity) {
			if (entity == null) return;

			var type = GetTurretType(entity.PrefabName);
			if (type == null) return;

			var player = BasePlayer.activePlayerList.Find(f => f.userID == entity.OwnerID);
			if (CheckBypass(player)) return;

			if (!turrets.ContainsKey(entity.OwnerID)) {
				PrintWarning($"Аномальная ситуация №1! Сообщите разработчику это число - [{entity.OwnerID}] и предоставьте файл data/{Title}.json");
				return;
			}

			if (turrets[entity.OwnerID][type] > 0) turrets[entity.OwnerID][type]--;
			if (!config.allowNotify) return;
			if (player == null || !player.IsConnected) return;

			var sum = GetTurretCount(player.UserIDString, type) - turrets[entity.OwnerID][type];
			SendReply(player,
				$"Количество размещенных '{turretTypes[type]}' уменьшилось!\n{(sum <= 0 ? "Больше установить нельзя!" : "Можно установить еще " + sum)}");
		}

		void SaveState() {
			Interface.Oxide.DataFileSystem.WriteObject(Title, turrets);
		}

		#endregion

		#region Хуки Oxide

		object CanBuild(Planner planner, Construction prefab, Construction.Target target) {
			if (prefab == null) return null;

			var type = GetTurretType(prefab.fullName);
			if (type == null) return null;

			var player = planner.GetOwnerPlayer();
			if (player == null) return false;
			if (CheckBypass(player)) return null;

			var userId = planner.GetOwnerPlayer().userID;

			if (!turrets.ContainsKey(userId)) {
				turrets.Add(userId, new Dictionary<string, int>());

				foreach (var turretType in turretTypes) {
					turrets[userId].Add(turretType.Key, 0);
				}
			}

			var currentCount = turrets[userId][type];

			var cntMax = GetTurretCount(player.UserIDString, type);

			if (currentCount > cntMax - 1) {
				if (player.IsConnected) {
					SendReply(player, $"Вы достигли лимита размещенных '{turretTypes[type]}', больше установить нельзя!");
				}

				return false;
			}

			turrets[userId][type]++;

			if (player.IsConnected && config.allowNotify) {
				var sum = cntMax - turrets[userId][type];
				SendReply(player,
					$"Количество размещенных '{turretTypes[type]}' увеличилось!\n{(sum <= 0 ? "Больше установить нельзя!" : "Можно установить еще " + sum)}");
			}

			return null;
		}

		void OnEntityDeath(BaseCombatEntity entity, HitInfo info) {
			CheckTurret(entity);
		}

		bool CanPickupEntity(BasePlayer player, BaseCombatEntity entity) {
			CheckTurret(entity);
			return true;
		}

		void OnNewSave(string filename) {
			turrets = new Dictionary<ulong, Dictionary<string, int>>();
			SaveState();
		}

		void OnServerSave() {
			SaveState();
		}

		#endregion
	}
}
