using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Oxide.Core;
using System.Text;

namespace Oxide.Plugins
{
	[Info("NoRecoil", "TopPlugin.ru/Sempai#3239", "0.1.7")]
	[Description("Removing recoil from rapid-fire weapons")]
	public class NoRecoil : RustPlugin
	{
        #region Var
        const String PermissionAdmin = "NoRecoil.admin";
		private string[] permittedWeapons = new string[] { "rifle.ak", "rifle.lr300", "smg.thompson", "smg.2", "smg.mp5" };
		#endregion

		#region Data 

		public Dictionary<UInt64, Dictionary<String, Boolean>> Users = new Dictionary<UInt64, Dictionary<String, Boolean>>();

		#endregion

		#region Configuration
		private static Configuration config = new Configuration();
		private class Configuration
		{
			[JsonProperty("Weapon customization")]
			public RecoilWeapon recoilWeapon = new RecoilWeapon();

			internal class RecoilWeapon
			{
				[JsonProperty("Whether to add a prefix to the weapon")]
				public Boolean addPrefixGun;
				[JsonProperty("weapon prefix")]
				public String prefixGun;
				[JsonProperty("Item ShortName / settings")]
				public Dictionary<String, weaponModifer> settingsRecoil = new Dictionary<String, weaponModifer>();

				internal class weaponModifer
				{
					[JsonProperty("Max Ammo (not more than 1000)")]
					public Int32 maxAmmo;
					[JsonProperty("Return percentage 100 - there will be no recoil. 50 - recoil reduced by 50%")]
					public Int32 recoilDeform;
					[JsonProperty("SkinId")]
					public ulong skinId;
				}
			}
				
			public static Configuration GetNewConfiguration()
			{
				return new Configuration
				{
					recoilWeapon = new RecoilWeapon
					{
						addPrefixGun = true,
						prefixGun = "[No recoil]",
						settingsRecoil = new Dictionary<string, RecoilWeapon.weaponModifer>
						{
							["rifle.ak"] = new RecoilWeapon.weaponModifer { maxAmmo = 30, recoilDeform = 100, skinId = 0UL },
							["rifle.lr300"] = new RecoilWeapon.weaponModifer { maxAmmo = 30, recoilDeform = 100, skinId = 0UL },
							["smg.thompson"] = new RecoilWeapon.weaponModifer { maxAmmo = 20, recoilDeform = 100, skinId = 0UL },
							["smg.2"] = new RecoilWeapon.weaponModifer { maxAmmo = 24, recoilDeform = 100, skinId = 0UL },
							["smg.mp5"] = new RecoilWeapon.weaponModifer { maxAmmo = 30, recoilDeform = 100, skinId = 0UL },
						}
					},
				};
			}
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				config = Config.ReadObject<Configuration>();
				if (config == null)
					LoadDefaultConfig();
			}
			catch
			{
				PrintWarning($"Configuration read error 'oxide/config/{Name}', creating a new configuration !!");
				LoadDefaultConfig();
			}

			List<String> removeKey = new List<String>();
            foreach (var item in config.recoilWeapon.settingsRecoil)
            {
				if (!permittedWeapons.Contains(item.Key))
				{
					PrintWarning("You tried to add unauthorized weapons to the config! It has been removed from the config.");
					removeKey.Add(item.Key);
					continue;
				}
				if (item.Value.maxAmmo > 1000)
					config.recoilWeapon.settingsRecoil[item.Key].maxAmmo = 1000;
				if(item.Value.recoilDeform > 100)
					config.recoilWeapon.settingsRecoil[item.Key].recoilDeform = 100;
			}

			if (removeKey.Count > 0)
                foreach (var item in removeKey)
					config.recoilWeapon.settingsRecoil.Remove(item);

			NextTick(SaveConfig);
		}

		protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
		protected override void SaveConfig() => Config.WriteObject(config);

		#endregion

		#region Lang
		private new void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<String, String>
			{
				["NoRecoil_NoPermission"] = "Ha you thought",
				["NoRecoil_WrongSyntaxConsole"] = "Wrong syntax, use : nrg Steam64ID/Nick ShortName",
				["NoRecoil_WrongSyntaxChat"] = "Wrong syntax, use : /nrg ShortName",
				["NoRecoil_PlayerNotFound"] = "There is no such player!",
				["NoRecoil_ErrorNameGun"] = "{0} does not exist in the configuration",
				["NoRecoil_GunGiveSucc"] = "The weapon has been successfully issued to the player!",
				["NoRecoil_UserNotPerm"] = "This player does not have permission to play with this weapon. First he needs to issue permissions - {0}",
				["NoRecoil_OffModification"] = "You have disabled the modification for everything {0}",
				["NoRecoil_EnableModification"] = "Weapon successfully modified",
				["NoRecoil_NoModificationPermission"] = "Ha you thought",
				["NoRecoil_NotAllowed"] = "Ha you thought",
				["NoRecoil_Mustbeactive"] = "Ha you thought",
			}, this);

			lang.RegisterMessages(new Dictionary<String, String>
			{
				["NoRecoil_NoPermission"] = "У вас нет прав на использование этой команды.",
				["NoRecoil_WrongSyntaxConsole"] = "Неправильный синтаксис, используйте: nrg Steam64ID/Nick ShortName",
				["NoRecoil_WrongSyntaxChat"] = "Неправильный синтаксис, используйте: /nrg ShortName",
				["NoRecoil_PlayerNotFound"] = "Такого игрока нет!",
				["NoRecoil_ErrorNameGun"] = "{0} не существует в конфигурации",
				["NoRecoil_GunGiveSucc"] = "Оружие успешно выдано игроку!",
				["NoRecoil_UserNotPerm"] = "У этого игрока нет разрешения играть с этим оружием. Сначала ему нужно выдать разрешения - {0}",
				["NoRecoil_OffModification"] = "Вы отключили модификацию для всего {0}",
				["NoRecoil_EnableModification"] = "Оружие успешно модифицировано",
				["NoRecoil_NoModificationPermission"] = "У вас нет разрешения на изменение этого оружия.",
				["NoRecoil_NotAllowed"] = "Это оружие не входит в список разрешенных",
				["NoRecoil_Mustbeactive"] = "Оружие должно быть в руках",
			}, this, "ru");
		}

		public static StringBuilder sb = new StringBuilder();
		public String GetLang(String LangKey, String userID = null, params object[] args)
		{
			sb.Clear();
			if (args != null)
			{
				sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
				return sb.ToString();
			}
			return lang.GetMessage(LangKey, this, userID);
		}
		#endregion

		#region Hooks

		void CheckValidator(BasePlayer player)
		{
			if (!Users.ContainsKey(player.userID))
			{
				Dictionary<string, bool> weaponList = new Dictionary<string, bool>();
				foreach (var item in config.recoilWeapon.settingsRecoil)
					weaponList.Add(item.Key, true);
				Users.Add(player.userID, weaponList);
			}

			var WeaponList = config.recoilWeapon.settingsRecoil;

			foreach (var weapon in WeaponList.Where(gun => !Users[player.userID].ContainsKey(gun.Key)))
				Users[player.userID].Add(weapon.Key, true);

		}
		private void OnPlayerConnected(BasePlayer player) => CheckValidator(player);

		void Unload()
		{
			ModifiItemAll(true);
			Interface.Oxide.DataFileSystem.WriteObject("NoRecoilUserData", Users);
		}

		private void OnServerInitialized()
		{
			foreach (BasePlayer player in BasePlayer.activePlayerList)
				OnPlayerConnected(player);
			ModifiItemAll(false);
		}
		private void Init()
		{
			Users = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<UInt64, Dictionary<String, Boolean>>>("NoRecoilUserData");
			permission.RegisterPermission(PermissionAdmin, this);
			foreach (var permReg in config.recoilWeapon.settingsRecoil)
				permission.RegisterPermission("NoRecoil." + permReg.Key, this);
		}

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
			if (container?.entityOwner is AutoTurret && item?.GetHeldEntity() is BaseProjectile)
			{
				ModifiItemDefault(item);
				return;
			}
            if (container?.playerOwner == null)
                return;
            CheckAndModifyItem(item, container.playerOwner);
        }

		private void OnItemCraftFinished(ItemCraftTask task, Item item)
		{
			if (!(item.GetHeldEntity() is BaseProjectile))
				return;
			CheckAndModifyItem(item, task.owner);
		}
		object OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
		{
			if (player == null || player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
				return null;
			CheckAndModifyItem(projectile.GetItem(), player);
			return null;		
		}
		object OnReloadMagazine(BasePlayer player, BaseProjectile projectile, int desiredAmount)
		{
			if (player == null || player.GetComponent<NPCPlayer>() != null || player.GetComponent<BaseNpc>() != null || player.IsNpc)
				return null;

			Int32 maxAmmoWeapon = 0;
			String weapon = player.GetActiveItem().info.shortname;

			if (config.recoilWeapon.settingsRecoil.ContainsKey(weapon) && player.GetActiveItem().HasFlag(global::Item.Flag.Placeholder))
				maxAmmoWeapon = config.recoilWeapon.settingsRecoil[weapon].maxAmmo;
			else
				return null;

			if (projectile.primaryMagazine.contents >= maxAmmoWeapon)
				return false;

			List<global::Item> list = player.inventory.FindItemIDs(projectile.primaryMagazine.ammoType.itemid).ToList<global::Item>();
			if (list.Count == 0)
			{
				List<global::Item> list2 = new List<global::Item>();
				player.inventory.FindAmmo(list2, projectile.primaryMagazine.definition.ammoTypes);
				if (list2.Count == 0)
				{
					return false;
				}
				list = player.inventory.FindItemIDs(list2[0].info.itemid).ToList<global::Item>();
				if (list == null || list.Count == 0)
				{
					return false;
				}
				if (projectile.primaryMagazine.contents > 0)
				{

					player.GiveItem(ItemManager.CreateByItemID(projectile.primaryMagazine.ammoType.itemid, projectile.primaryMagazine.contents, 0UL),BaseEntity.GiveItemReason.Generic);

					projectile.primaryMagazine.contents = 0;
				}
				projectile.primaryMagazine.ammoType = list[0].info;
			}
			int num = desiredAmount;
			if (num == -1)
			{
				num = maxAmmoWeapon - projectile.primaryMagazine.contents;
			}
			foreach (global::Item item in list)
			{
				int amount = item.amount;
				int num2 = Mathf.Min(num, item.amount);
				item.UseItem(num2);
				projectile.primaryMagazine.contents += num2;
				num -= num2;
				if (num <= 0)
				{
					break;
				}
			}
			projectile.SendNetworkUpdateImmediate(false);
			return false;
		}
        #endregion

        #region Commands
        [ConsoleCommand("nrg")]
		void GiveNoRecoilWeapon(ConsoleSystem.Arg arg)
		{
			if (arg == null || arg.Args == null || arg.Args.Length != 2 || arg.Args.Length > 2)
			{
				PrintWarning(GetLang("NoRecoil_WrongSyntaxConsole"));
				return;
			}
			BasePlayer player = BasePlayer.Find(arg.Args[0]);
			String WeaponShortname = arg.Args[1];

			if (player == null)
			{
				PrintWarning(GetLang("NoRecoil_PlayerNotFound"));
				return;
			}			
			if (!config.recoilWeapon.settingsRecoil.ContainsKey(WeaponShortname))
			{
				PrintWarning(GetLang("NoRecoil_ErrorNameGun", null, WeaponShortname));
				return;
			}
			if (permission.UserHasPermission(player.UserIDString, "NoRecoil." + WeaponShortname))
			{
				PrintWarning(GetLang("NoRecoil_UserNotPerm", null, "NoRecoil." + WeaponShortname));
				return;
			}
			CreateNoRecoilWeapon(player, WeaponShortname);
			PrintWarning(GetLang("NoRecoil_GunGiveSucc"));
		}

		[ChatCommand("nrg")]
		void GiveNoRecoilWeaponCmd(BasePlayer player, string cmd, string[] arg)
		{
			if (arg == null || arg.Length != 1 || arg.Length > 1)
			{
				SendChat(GetLang("NoRecoil_WrongSyntaxChat", player.UserIDString), player);
				return;
			}
			if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin))
			{
				SendChat(GetLang("NoRecoil_NoPermission", player.UserIDString), player);
				return;
			}

			String WeaponShortname = arg[0];
			if (!config.recoilWeapon.settingsRecoil.ContainsKey(WeaponShortname))
			{
				SendChat(GetLang("NoRecoil_ErrorNameGun", player.UserIDString, WeaponShortname), player);
				return;
			}
			CreateNoRecoilWeapon(player, WeaponShortname);
			SendChat(GetLang("NoRecoil_GunGiveSucc", player.UserIDString), player);
		}

		[ChatCommand("nr")]
		void NoRecoilWeaponCmd(BasePlayer player)
		{
			Item recoilWeapon = player.GetActiveItem();
			if (recoilWeapon != null)
			{
				var weapon = recoilWeapon?.GetHeldEntity()?.GetComponent<BaseProjectile>();
				if (weapon != null && config.recoilWeapon.settingsRecoil.ContainsKey(recoilWeapon.info.shortname))
				{
					if (permission.UserHasPermission(player.UserIDString, "NoRecoil." + recoilWeapon.info.shortname) || permission.UserHasPermission(player.UserIDString, PermissionAdmin))
					{						
						if (recoilWeapon.HasFlag(global::Item.Flag.Placeholder))
						{
							ModifiItemDefault(recoilWeapon);
							SendChat(GetLang("NoRecoil_OffModification", player.UserIDString, recoilWeapon.info.displayName.english), player);
							Users[player.userID][recoilWeapon.info.shortname] = false;
						}
						else
						{
							ModifiItem(recoilWeapon);
							SendChat(GetLang("NoRecoil_EnableModification", player.UserIDString), player);
							Users[player.userID][recoilWeapon.info.shortname] = true;
						}
					}
					else
					{
						SendChat(GetLang("NoRecoil_NoModificationPermission", player.UserIDString), player);
						return;
					}
				}
				else
				{
					SendChat(GetLang("NoRecoil_NotAllowed", player.UserIDString), player);
					return;
				}
			}
			else
			{
				SendChat(GetLang("NoRecoil_Mustbeactive", player.UserIDString), player);
				return;
			}
		}
		#endregion

		#region Metods
		public void SendChat(string Message, BasePlayer player)
		{
			PrintToChat(player, Message);
		}

		void CreateNoRecoilWeapon(BasePlayer player, String name)
		{
			Item item = ItemManager.CreateByName(name);
			ModifiItem(item);
			player.GiveItem(item);
		}
		private void ModifiItemAll(Boolean unload)
		{
			BaseProjectile[] attachments = (BaseProjectile[])GameObject.FindObjectsOfType(typeof(BaseProjectile));
			if (attachments != null)
				foreach (BaseProjectile attachment in attachments)
					CheckAndModifyItem(attachment?.GetItem(), attachment?.GetItem()?.GetOwnerPlayer(), unload);
		}

		private void ModifiItem(Item item)
		{
			item.SetFlag(global::Item.Flag.Placeholder, true);
			BaseProjectile projectile = item.GetHeldEntity() as BaseProjectile;
			var cfg = config.recoilWeapon.settingsRecoil[item.info.shortname];
			projectile.primaryMagazine.capacity = cfg.recoilDeform == 100 ? int.MaxValue : projectile.primaryMagazine.definition.builtInSize * cfg.recoilDeform;
			if (config.recoilWeapon.addPrefixGun)
				item.name = item.info.displayName.english + " " + config.recoilWeapon.prefixGun;
			if (cfg.skinId != 0UL)
				item.skin = cfg.skinId;
			projectile.SendNetworkUpdate();
			item.MarkDirty();
		}

		private void ModifiItemDefault(Item item)
		{
			item.SetFlag(global::Item.Flag.Placeholder, false);
			BaseProjectile projectile = item.GetHeldEntity() as BaseProjectile;
			projectile.primaryMagazine.capacity = projectile.primaryMagazine.definition.builtInSize;
			if (config.recoilWeapon.addPrefixGun)
				item.name = item.info.displayName.english;
			if (config.recoilWeapon.settingsRecoil[item.info.shortname].skinId != 0UL)
				item.skin = 0UL;
			projectile.SendNetworkUpdate();
			item.MarkDirty();
		}

		private void CheckAndModifyItem(Item item, BasePlayer player, Boolean unload = false)
		{
			var weapon = item?.GetHeldEntity()?.GetComponent<BaseProjectile>();
			if (weapon == null)
				return;
			if (!config.recoilWeapon.settingsRecoil.ContainsKey(item.info.shortname))
				return;
			if (!unload)
			{
				if (player == null || player.IsNpc)
					return;
				if (Users.ContainsKey(player.userID))
					if (Users[player.userID][item.info.shortname])
					{
						if (permission.UserHasPermission(player.UserIDString, "NoRecoil." + item.info.shortname) || permission.UserHasPermission(player.UserIDString, PermissionAdmin))
							ModifiItem(item);
						else
							ModifiItemDefault(item);
					}
			}
			else
				ModifiItemDefault(item);

		}
		#endregion
	}
}