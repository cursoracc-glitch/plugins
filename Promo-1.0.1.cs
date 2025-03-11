using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
	[Info("Promo", "xkrystalll", "1.0.1")]
	class Promo : RustPlugin
	{
		#region Classes

		internal class ItemData
		{
			[JsonProperty(Order = 0)] public string Shortname;
			[JsonProperty("Skin", Order = 1)] public ulong SkinID;
			[JsonProperty(Order = 2)] public int Amount;

			[JsonProperty("Display name (still empty if not need to change)", Order = 4)]
			public string DisplayName;

			public Item ToItem()
			{
				Item item = ItemManager.CreateByName(Shortname, Amount, SkinID);
				if (!string.IsNullOrEmpty(DisplayName))
					item.name = DisplayName;

				var heldEntity = item.GetHeldEntity();
				if (heldEntity != null)
					heldEntity.SendNetworkUpdate();
				return item;
			}
		}

		internal class Reward
		{
			[JsonProperty("Команды (%STEAMID% - id игрока) | Оставить пустым если нужно выдавать предметы ниже", Order = 0)]
			public List<string> Commands;
			[JsonProperty("Предметы", Order = 1)]
			public List<ItemData> Items;

			public new RewardType GetType() => Commands.IsNullOrEmpty() ? RewardType.Items : RewardType.Command;
		}
		
		internal class Promocode
		{
			public string Code;
			public int Usages;
			public bool Enabled;
			public Reward Reward;
		}
		#endregion

		#region Fields

		private Dictionary<string, int> EnteredPromocodesAmount = new();
		private Dictionary<ulong, List<string>> EnteredPromocodesPlayers = new();

		internal enum RewardType : byte
		{
			Items = 0,
			Command = 1
		}
		
		#endregion

		#region Hooks
		private void OnServerInitialized()
		{
			LoadData();
			foreach (var x in cfg.Promocodes)
			{
				if (!x.Value.Enabled)
					continue;
				cmd.AddChatCommand(x.Value.Code, this, nameof(cmdTakePromocode));
			}
		}

		private void Unload() => SaveData();

		private void OnServerSave() => SaveData();

		#endregion

		#region Methods

		private bool CanTakePromocode(BasePlayer target, string code)
		{
			var promocode = cfg.Promocodes.FirstOrDefault(x => x.Value.Code == code);

			if (promocode.Value == null)
				return false;
			
			if (EnteredPromocodesAmount.TryGetValue(promocode.Key, out var usagesAmount))
				if (usagesAmount > promocode.Value.Usages)
					return false;
			
			if (EnteredPromocodesPlayers.TryGetValue(target.userID, out var usedByPlayer))
				if (usedByPlayer.Contains(promocode.Key))
				{
					SendMessage(target, GetMsg("entered.alreadyentered", target.userID));
					return false;
				}

			return true;
		}

		private void TakeCode(BasePlayer player, KeyValuePair<string, Promocode> promocodeInfo)
		{
			if (promocodeInfo.Value == null)
				return;
			
			if (!EnteredPromocodesPlayers.ContainsKey(player.userID))
				EnteredPromocodesPlayers.Add(player.userID, new());

			if (!CanTakePromocode(player, promocodeInfo.Value.Code))
				return;


			switch (promocodeInfo.Value.Reward.GetType())
			{
				case RewardType.Command:
					foreach (var x in promocodeInfo.Value.Reward.Commands)
						Server.Command(x.Replace("%STEAMID%", player.UserIDString));
					break;
				case RewardType.Items:
					foreach (var x in promocodeInfo.Value.Reward.Items)
						player.GiveItem(x.ToItem());
					break;
				
				default:
					throw new ArgumentException("Promocode::Reward");
			}
			
			if (!EnteredPromocodesAmount.ContainsKey(promocodeInfo.Key))
				EnteredPromocodesAmount.Add(promocodeInfo.Key, 0);

			EnteredPromocodesPlayers[player.userID].Add(promocodeInfo.Key);
			EnteredPromocodesAmount[promocodeInfo.Key]++;

			SendMessage(player, GetMsg("entered.success", player.userID));
		}

		private void SendMessage(BasePlayer player, string message)
		{
			player.SendConsoleCommand("chat.add", new object[]
			{
				2,
				cfg.AvatarID,
				message
			});
		}
		#endregion

		#region Commands
		private void cmdTakePromocode(BasePlayer player, string command)
		{
			if (player == null || string.IsNullOrEmpty(command))
				return;

			var code = cfg.Promocodes.FirstOrDefault(x => x.Value.Code == command);

			if (code.Value == null)
				return;
			
			if (CanTakePromocode(player, command))
				TakeCode(player, new(code.Key, code.Value));
		}

		#endregion
		
		#region Config

		private ConfigData cfg;

		public class ConfigData
		{
			[JsonProperty("Аватарка")] public ulong AvatarID;
			[JsonProperty("Промокоды")] public Dictionary<string, Promocode> Promocodes;
		}

		protected override void LoadDefaultConfig()
		{
			var config = new ConfigData
			{
				AvatarID = 76561198802642520,
				Promocodes = new()
				{
					["test1"] = new()
					{
						Code = "promocode",
						Usages = 10,
						Enabled = true,
						Reward = new()
						{
							Commands = null,
							Items = new()
							{
								new()
								{
									Shortname = "rifle.ak",
									SkinID = 123,
									Amount = 1,
									DisplayName = "TEST AK"
								},
								new()
								{
									Shortname = "rifle.lr300",
									SkinID = 0,
									Amount = 1,
									DisplayName = null
								}
							}
						},
						
					},
					["test2"] = new()
					{
						Code = "promocode2",
						Usages = 10,
						Enabled = true,
						Reward = new()
						{
							Commands = new()
							{
								"test 1 %STEAMID%",
								"testtest %STEAMID%"
							}
						}
					}
				}
			};
			SaveConfig(config);
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			cfg = Config.ReadObject<ConfigData>();
			SaveConfig(cfg);
		}

		private void SaveConfig(object config)
		{
			Config.WriteObject(config, true);
		}
		#endregion

		#region Data

		private void SaveData()
		{
			Interface.Oxide.DataFileSystem.WriteObject($"{Title}/playersEnteredCodes", EnteredPromocodesPlayers);
			Interface.Oxide.DataFileSystem.WriteObject($"{Title}/promoUsages", EnteredPromocodesAmount);
		}

		private void LoadData()
		{
			EnteredPromocodesPlayers = Interface.Oxide?.DataFileSystem?.ReadObject<Dictionary<ulong, List<string>>>($"{Title}/playersEnteredCodes")
			    ?? new();
			EnteredPromocodesAmount = Interface.Oxide?.DataFileSystem?.ReadObject<Dictionary<string, int>>($"{Title}/promoUsages")
			    ?? new();
		}
		#endregion
		
		#region Langs

		private void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["entered.success"] = "Promocode successfully used",
				["entered.alreadyentered"] = "You already used this promocode"
			}, this);

			lang.RegisterMessages(new Dictionary<string, string>
			{
				["entered.success"] = "Промокод успешно активирован",
				["entered.alreadyentered"] = "Вы уже вводили этот промокод"
			}, this, "ru");
		}
		private string GetMsg(string key, ulong id, params object[] args) =>
			string.Format(lang.GetMessage(key, this, id.ToString()), args);
		#endregion
	}
}