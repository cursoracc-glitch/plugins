using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core.Libraries;
using System.IO;
namespace Oxide.Plugins
{
    [Info("Teleportation", "OxideBro/Ryamkk", "1.2.2")]

    class Teleportation : RustPlugin
    {
        [PluginReference]
        Plugin Clans;
        [PluginReference]
        Plugin Friends;
        readonly MethodInfo entitySnapshot = typeof(BasePlayer).GetMethod("SendEntitySnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
        Dictionary<ulong, Vector3> lastPositions = new Dictionary<ulong, Vector3>();
        Dictionary<BasePlayer, int> spectatingPlayers = new Dictionary<BasePlayer, int>();
        bool IsClanMember(ulong playerID, ulong targetID)
        {
            return (bool)(Clans?.Call("HasFriend", playerID, targetID) ?? false);
        }

        bool IsFriends(ulong playerID, ulong friendId)
        {
            return (bool)(Friends?.Call("AreFriends", playerID, friendId) ?? false);
        }

        #region CLASSES

        class TP
        {
            public BasePlayer Player;
            public BasePlayer Player2;
            public Vector3 pos;
            public int seconds;

            public TP(BasePlayer player, Vector3 pos, int seconds, BasePlayer player2 = null)
            {
                this.Player = player;
                this.pos = pos;
                this.seconds = seconds;
                this.Player2 = player2;
            }
        }

        #endregion
			
        #region CONFIGURATION
        const string TPADMIN = "teleportation.admin";
        Dictionary<string, int> homelimitPerms;
		Dictionary<string, int> teleportSecsPerms;
        Dictionary<string, int> tpkdPerms;
        Dictionary<string, int> tpkdhomePerms;
		int RadiationBlockTP;
	    int ColdBlockTP;
		int BleedingBlockTP;
		int CraftBlockTP;
		int homelimitDefault;
		int tpkdDefault;
		int tpkdhomeDefault;
        int teleportSecsDefault;
        int resetPendingTime;
		int CraftBlockHome;
		int RadiationBlockHome;
		int BleedingBlockHome;
		int ColdBlockHome;
		//int SleepingBagSkin;
	    bool CanSwimmingTP;
		bool CanCraftTP;
		bool CanAliveTP;
		bool CanRadiationTP;
		bool CanBleedingTP;
		bool CanWoundedTP;
		bool CanColdTP;
        bool restrictCupboard;
		bool enabledTP;
		bool enabledHome;
        bool homecupboard;
        bool adminsLogs;
        bool foundationOwner;
        bool foundationOwnerFC;
        bool restrictTPRCupboard;
        bool foundationEx;
        bool wipedData;
        bool createSleepingBug;
	    bool CanWoundedHome;
		bool CanColdHome;
	    bool CanSwimmingHome;
		bool CanCraftHome;
	    bool CanAliveHome;
		bool CanRadiationHome;
		bool CanBleedingHome;
        string EffectPrefab1;
        string EffectPrefab;
		string SleepingBagName;
        static DynamicConfigFile config;

        void WipeData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Teleportation/Homes", new Dictionary<string, FileInfo>());
            Interface.Oxide.DataFileSystem.WriteObject("Teleportation/AdminTpSave", new Dictionary<string, FileInfo>());
            SaveData();
            Interface.Oxide.ReloadPlugin(Title);
        }

        protected override void LoadDefaultConfig()
        {
			GetVariable(Config, "Общие настройки", "A. Логировать использование команд для администраторов", out adminsLogs, true);
			GetVariable(Config, "Общие настройки", "B. Звук предупреждения (пустое поле = звук отключен)", out EffectPrefab, "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab");
            GetVariable(Config, "Общие настройки", "C. Звук уведомления при получение запроса на телепорт (пустое поле = звук отключен)", out EffectPrefab1, "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab");
			GetVariable(Config, "Общие настройки", "D. Автоматический вайп данных при генерации новой карты", out wipedData, true);
			
			GetVariable(Config, "Телепортация домой", "A. Разрешить телепортацию", out enabledHome, true);
			GetVariable(Config, "Телепортация домой", "B. Запрещать отправлять запрос на телепортацию в воде", out CanSwimmingHome, true);
			GetVariable(Config, "Телепортация домой", "C. Запрещать отправлять запрос на телепортацию во время крафта", out CanCraftHome, true);
			GetVariable(Config, "Телепортация домой", "D. Разрешённое количество предметов на крафте", out CraftBlockHome, 0);
			GetVariable(Config, "Телепортация домой", "E. Отменять телепортацию при смерте", out CanAliveHome, true);
			GetVariable(Config, "Телепортация домой", "F. Запрещать отправлять запрос на телепортацию во время облечения радиацией", out CanRadiationHome, true);
			GetVariable(Config, "Телепортация домой", "G. Разрешённое радиационное облучения для телепортации", out RadiationBlockHome, 5);
			GetVariable(Config, "Телепортация домой", "H. Запрещать отправлять запрос на телепортацию во время кровотечения", out CanBleedingHome, true);
			GetVariable(Config, "Телепортация домой", "I. Разрешённый процент кровотечения для телепортации", out BleedingBlockHome, 0);
			GetVariable(Config, "Телепортация домой", "J. Отменять телепортацияю если игрок получил ранения", out CanWoundedHome, true);
			GetVariable(Config, "Телепортация домой", "K. Запрещать отправлять запрос на телепортацию если игроку холодно", out CanColdHome, true);
			GetVariable(Config, "Телепортация домой", "L. Разрешённый проект замерзания для телепортации", out ColdBlockHome, 3);
			GetVariable(Config, "Телепортация домой", "M. Запрещать отправлять запрос на телепортацию в зоне действия чужого шкафа", out homecupboard, true);
            GetVariable(Config, "Телепортация домой", "N. Разрешать сохранять местоположение только на фундаменте", out foundationEx, true);
            GetVariable(Config, "Телепортация домой", "O. Создавать объект при сохранении местоположения", out createSleepingBug, true);
			GetVariable(Config, "Телепортация домой", "P. Название создаваемого объекта", out SleepingBagName, "Sleeping Bag");
			//GetVariable(Config, "Телепортация домой", "Q. Скин создаваемого объекта", out SleepingBagName, 1174407153);
            GetVariable(Config, "Телепортация домой", "R. Запрещать сохранять местоположение если игрок не является владельцем фундамента", out foundationOwner, true);
            GetVariable(Config, "Телепортация домой", "S. Разрешать сохранять местоположение если игрок является другом или соклановцем владельца фундамента ", out foundationOwnerFC, true);
			GetVariable(Config, "Телепортация домой", "T. Ограничение на количество сохранённых местоположений", out homelimitDefault, 3);
			GetVariable(Config, "Телепортация домой", "U. Длительность перезарядки телепорта домой (в секундах)", out tpkdhomeDefault, 300);

            GetVariable(Config, "Телепортация к игроку", "A. Разрешить телепортацию", out enabledTP, true);
			GetVariable(Config, "Телепортация к игроку", "B. Время ответа на запрос телепортации (в секундах)", out resetPendingTime, 15);
			GetVariable(Config, "Телепортация к игроку", "C. Запрещать отправлять запрос на телепортацию в воде", out CanSwimmingTP, true);
			GetVariable(Config, "Телепортация к игроку", "D. Запрещать отправлять запрос на телепортацию во время крафта", out CanCraftTP, true);
			GetVariable(Config, "Телепортация к игроку", "E. Разрешённое количество предметов на крафте", out CraftBlockTP, 0);
			GetVariable(Config, "Телепортация к игроку", "F. Отменять телепортацию при смерте", out CanAliveTP, true);
			GetVariable(Config, "Телепортация к игроку", "G. Запрещать отправлять запрос на телепортацию во время облечения радиацией", out CanRadiationTP, true);
			GetVariable(Config, "Телепортация к игроку", "H. Разрешённое радиационное облучения для телепортации", out RadiationBlockTP, 5);
			GetVariable(Config, "Телепортация к игроку", "I. Запрещать отправлять запрос на телепортацию во время кровотечения", out CanBleedingTP, true);
			GetVariable(Config, "Телепортация к игроку", "J. Разрешённый процент кровотечения для телепортации", out BleedingBlockTP, 0);
			GetVariable(Config, "Телепортация к игроку", "K. Отменять телепортацияю если игрок получил ранения", out CanWoundedTP, true);
			GetVariable(Config, "Телепортация к игроку", "L. Запрещать отправлять запрос на телепортацию если игроку холодно", out CanColdTP, true);
			GetVariable(Config, "Телепортация к игроку", "M. Разрешённый проект замерзания для телепортации", out ColdBlockTP, 3);
			GetVariable(Config, "Телепортация к игроку", "N. Запрещать отправлять запрос на телепортацию в зоне действия чужого шкафа", out restrictTPRCupboard, true);
            GetVariable(Config, "Телепортация к игроку", "O. Запрещать принимать запрос на телепортацию в зоне действия чужого шкафа", out restrictCupboard, true);
			GetVariable(Config, "Телепортация к игроку", "P. Длительность задержки перед телепортацией (в секундах)", out teleportSecsDefault, 15);
			GetVariable(Config, "Телепортация к игроку", "Q. Длительность перезарядки телепорта (в секундах)", out tpkdDefault, 300);
			
            Config["Телепортация домой", "V. Ограничение на количество сохранённых местоположений с привилегией"] =
                homelimitPerms =
                    GetConfig("Ограничение на количество сохранённых местоположений с привилегией",
                            new Dictionary<string, object>()
							{
								{ "teleportation.vip", 4 },
								{ "teleportation.god", 5 },
								{ "teleportation.prem", 6 },
								{ "teleportation.elite", 7 },
								{ "teleportation.king", 5 },
								{ "teleportation.none", 999 }
							})
                        .ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, homelimitPerms.Keys.ToList());
			
            Config["Телепортация к игроку", "R. Длительность задержки перед телепортацией с привилегией (в секундах)"] =
                teleportSecsPerms =
                    GetConfig("Длительность задержки перед телепортацией с привилегией (в секундах)",
                            new Dictionary<string, object>()
							{
								{ "teleportation.vip", 10 },
								{ "teleportation.god", 9 },
								{ "teleportation.prem", 8 },
								{ "teleportation.elite", 7 },
								{ "teleportation.king", 6 },
								{ "teleportation.none", 1 }
							})
                        .ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, teleportSecsPerms.Keys.ToList());

            Config["Телепортация к игроку", "U. Длительность перезарядки телепорта с привилегией (в секундах)"] =
                tpkdPerms =
                    GetConfig("Длительность перезарядки телепорта с привилегией (в секундах)",
                            new Dictionary<string, object>()
							{
								{ "teleportation.vip", 250 },
								{ "teleportation.god", 200 },
								{ "teleportation.prem", 150 },
								{ "teleportation.elite", 100 },
								{ "teleportation.king", 50 },
								{ "teleportation.none", 1 }
							})
                        .ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, tpkdPerms.Keys.ToList());
			
            Config["Телепортация домой", "W. Длительность перезарядки телепорта домой с привилегией (в секундах)"] =
                tpkdhomePerms =
                    GetConfig("Длительность перезарядки телепорта домой с привилегией (в секундах)",
                            new Dictionary<string, object>()
							{
								{ "teleportation.vip", 250 },
								{ "teleportation.god", 200 },
								{ "teleportation.prem", 150 },
								{ "teleportation.elite", 100 },
								{ "teleportation.king", 50 },
								{ "teleportation.none", 1 }
							})
                        .ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, tpkdhomePerms.Keys.ToList());

            SaveConfig();
        }

        T GetConfig<T>(string name, T defaultValue)
            => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));

        public static void GetVariable<T>(DynamicConfigFile config, string name, string Key, out T value, T defaultValue)
        {
            config[name, Key] = value = config[name, Key] == null ? defaultValue : (T)Convert.ChangeType(config[name, Key], typeof(T));
        }
        #endregion

        #region Permissions
        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(ulong uid, string permissionName)
            {
                return !string.IsNullOrEmpty(permissionName) && permission.UserHasPermission(uid.ToString(), permissionName);
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");

                foreach (var permissionName in permissions.Where(permissionName => !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }
        #endregion

        #region FIELDS

        public BasePlayer FindBasePlayer(string nameOrUserId)
        {
            nameOrUserId = nameOrUserId.ToLower();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId)
                    return player;
            }
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId)
                    return player;
            }
            return default(BasePlayer);
        }

        private FieldInfo SleepingBagUnlockTimeField = typeof(SleepingBag).GetField("unlockTime",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly int groundLayer = LayerMask.GetMask("Terrain", "World");
        private readonly int buildingMask = Rust.Layers.Server.Buildings;
        
        Dictionary<ulong, Dictionary<string, Vector3>> homes;
        Dictionary<ulong, Dictionary<string, Vector3>> tpsave;
        Dictionary<ulong, int> cooldownsTP = new Dictionary<ulong, int>();
        Dictionary<ulong, int> cooldownsHOME = new Dictionary<ulong, int>();
        List<TP> tpQueue = new List<TP>();
        List<TP> pendings = new List<TP>();
        List<ulong> sethomeBlock = new List<ulong>();

        #endregion

        #region COMMANDS

        [ChatCommand("sethome")]
        void cmdChatSetHome(BasePlayer player, string command, string[] args)
        {
			if (!enabledHome) return;
            var uid = player.userID;
            var pos = player.GetNetworkPosition();
            var foundation = GetFoundation(pos);
            var bulds = GetBuldings(pos);
            if (foundationEx)
            {
                if (foundation == null)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["foundationmissing"]);
                    return;
                }
            }
            if (!foundationEx)
            {
                if (bulds == null)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["foundationmissing"]);
                    return;
                }
            }
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["sethomeArgsError"]);
                return;
            }
			if(CanSwimmingHome)
			{
				if (player.IsSwimming())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Swimming"]);
                    return;
                }
			}
			if(CanCraftHome)
			{
				if (player.inventory.crafting.queue.Count > CraftBlockHome)
				{
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Craft"]);
                    return;
                }
			}
			if(CanAliveHome)
			{
				if (!player.IsAlive())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Alive"]);
                    return;
                }
			}
			if(CanRadiationHome)
			{
				if (player.metabolism.radiation_poison.value > RadiationBlockHome)
                {
				    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Radiation"]);
                    return;
                }
			}
			if(CanBleedingHome)
			{
                if (player.metabolism.bleeding.value > BleedingBlockHome)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Bleeding"]);
                    return;
                }
			}
			if(CanWoundedHome)
			{
                if (player.IsWounded())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Wounded"]);
                    return;
                }
			}
			if(CanColdHome)
			{
			    if (player.metabolism.temperature.value < ColdBlockHome)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Cold"]);
                    return;
                }
			}
            if (sethomeBlock.Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["sethomeBlock"]);
                return;
            }
            if (foundationOwnerFC && foundationOwner)
            {
                if (!foundationEx && bulds.OwnerID != uid)
                {
                    if (!IsFriends(bulds.OwnerID, player.userID) && !IsClanMember(bulds.OwnerID, player.userID))
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["foundationownerFC"]);
                        return;
                    }
                }
                if (foundationEx && foundation.OwnerID != uid)
                {
                    if (!IsFriends(foundation.OwnerID, player.userID) && !IsClanMember(foundation.OwnerID, player.userID))
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["foundationownerFC"]);
                        return;
                    }
                }

            }
            if (foundationOwner)
            {
                if (foundationEx && foundation.OwnerID != uid && foundationOwnerFC == (!IsFriends(foundation.OwnerID, player.userID) && !IsClanMember(foundation.OwnerID, player.userID)))
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["foundationowner"]);
                    return;
                }
                if (!foundationEx && bulds.OwnerID != uid && foundationOwnerFC == (!IsFriends(bulds.OwnerID, player.userID) && !IsClanMember(bulds.OwnerID, player.userID)))
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["foundationowner"]);
                    return;
                }
            }
            var name = args[0];

            SetHome(player, name);
        }

        [ChatCommand("removehome")]
        void cmdChatRemoveHome(BasePlayer player, string command, string[] args)
        {
			if (!enabledHome) return;
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["removehomeArgsError"]);
                return;
            }
            if (!homes.ContainsKey(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["homesmissing"]);
                return;
            }
            var name = args[0];
            var playerHomes = homes[player.userID];
            if (!playerHomes.ContainsKey(name))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["homenotexist"]);
                return;
            }
            foreach (var sleepingBag in SleepingBag.FindForPlayer(player.userID, true))
            {
                if (Vector3.Distance(sleepingBag.transform.position, playerHomes[name]) < 1)
                {
                    sleepingBag.Kill();
                    break;
                }
            }
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            playerHomes.Remove(name);
            SendReply(player, Messages["removehomesuccess"], name);
        }

        [ConsoleCommand("home")]
        void cmdHome(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            if (arg.Args == null || arg.Args.Length < 1) return;
            cmdChatHome(player, "", new[] { arg.Args[0] });
        }

        [ChatCommand("homelist")]
        private void cmdHomeList(BasePlayer player, string command, string[] args)
        {
			if (!enabledHome) return;
            if (!homes.ContainsKey(player.userID) || homes[player.userID].Count == 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["homesmissing"]);
                return;
            }
            var playerHomes = homes[player.userID];
            var time = (GetHomeLimit(player.userID) - playerHomes.Count);
            var homelist = playerHomes.Select(x =>  GetSleepingBag(x.Key, x.Value) != null ? $"{x.Key} {x.Value}" : $"Дом: {x.Key} {x.Value}");
            foreach (var home in playerHomes.ToList())
            {
                if (createSleepingBug)
                {
                    if (!GetSleepingBag(home.Key, home.Value))
                        playerHomes.Remove(home.Key);
                }
            }
            SendReply(player, Messages["homeslist"], time, string.Join("\n", homelist.ToArray()));
        }

        [ChatCommand("home")]
        void cmdChatHome(BasePlayer player, string command, string[] args)
        {
			if (!enabledHome) return;
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["homeArgsError"]);
                return;
            }
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
			if(CanSwimmingHome)
			{
				if (player.IsSwimming())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Swimming"]);
                    return;
                }
			}
			if(CanCraftHome)
			{
				if (player.inventory.crafting.queue.Count > CraftBlockHome)
				{
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Craft"]);
                    return;
                }
			}
			if(CanAliveHome)
			{
				if (!player.IsAlive())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Alive"]);
                    return;
                }
			}
			if(CanRadiationHome)
			{
				if (player.metabolism.radiation_poison.value > RadiationBlockHome)
                {
				    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Radiation"]);
                    return;
                }
			}
			if(CanBleedingHome)
			{
                if (player.metabolism.bleeding.value > BleedingBlockHome)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Bleeding"]);
                    return;
                }
			}
			if(CanWoundedHome)
			{
                if (player.IsWounded())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Wounded"]);
                    return;
                }
			}
			if(CanColdHome)
			{
			    if (player.metabolism.temperature.value < ColdBlockHome)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Cold"]);
                    return;
                }
			}
            int seconds;
            if (cooldownsHOME.TryGetValue(player.userID, out seconds) && seconds > 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, string.Format(Messages["tpkd"], TimeToString(seconds)));
                return;
            }
            if (homecupboard)
            {
                var privilege = player.GetBuildingPrivilege(player.WorldSpaceBounds());
                if (privilege != null && !player.IsBuildingAuthed())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["tphomecupboard"]);
                    return;
                }
            }
            if (!homes.ContainsKey(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["homesmissing"]);
                return;
            }
            var name = args[0];
            var playerHomes = homes[player.userID];
            if (!playerHomes.ContainsKey(name))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["homenotexist"]);
                return;
            }
            var time = GetTeleportTime(player.userID);
            var pos = playerHomes[name];

            SleepingBag bag = GetSleepingBag(name, pos);
            if (createSleepingBug)
            {
                if (bag == null)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["sleepingbagmissing"]);
                    playerHomes.Remove(name);
                    return;
                }
            }
            if (!createSleepingBug)
            {
                var bulds = GetBuldings(pos);
                if (bulds == null)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["foundationmissingR"]);
                    playerHomes.Remove(name);
                    return;
                }
            }
            if (player.metabolism.temperature.value < ColdBlockHome)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["Cold"]);
                return;
            }


            var lastTp = tpQueue.Find(p => p.Player == player);
            if (lastTp != null)
            {
                tpQueue.Remove(lastTp);
            }
            tpQueue.Add(new TP(player, pos, time));
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendReply(player, String.Format(Messages["homequeue"], name, TimeToString(time)));
            SaveData();
        }

        [ChatCommand("tpr")]
        void cmdChatTpr(BasePlayer player, string command, string[] args)
        {
            if (!enabledTP) return;
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tprArgsError"]);
                return;
            }
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
			if(CanSwimmingTP)
			{
				if (player.IsSwimming())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Swimming"]);
                    return;
                }
			}
			if(CanCraftTP)
			{
				if (player.inventory.crafting.queue.Count > CraftBlockTP)
				{
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Craft"]);
                    return;
                }
			}
			if(CanAliveTP)
			{
				if (!player.IsAlive())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Alive"]);
                    return;
                }
			}
			if(CanRadiationTP)
			{
				if (player.metabolism.radiation_poison.value > RadiationBlockTP)
                {
				    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Radiation"]);
                    return;
                }
			}
			if(CanBleedingTP)
			{
                if (player.metabolism.bleeding.value > BleedingBlockTP)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Bleeding"]);
                    return;
                }
			}
			if(CanWoundedTP)
			{
                if (player.IsWounded())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Wounded"]);
                    return;
                }
			}
			if(CanColdTP)
			{
			    if (player.metabolism.temperature.value < ColdBlockTP)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Cold"]);
                    return;
                }
			}
            if (restrictTPRCupboard)
            {
                var privilege = player.GetBuildingPrivilege(player.WorldSpaceBounds());
                if (privilege != null && !player.IsBuildingAuthed())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["tpcupboard"]);
                    return;
                }
            }

            var name = args[0];
            var target = FindBasePlayer(name);
            if (target == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["playermissing"]);
                return;
            }
            if (target == player)
            {
                SendReply(player, Messages["playerisyou"]);
                return;
            }
            int seconds = 0;
            if (restrictCupboard && tpQueue.Any(p => p.Player == player && p.Player2 != null) &&
                player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null &&
                !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpcupboard"]);
                return;
            }
            if (cooldownsTP.TryGetValue(player.userID, out seconds) && seconds > 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, string.Format(Messages["tpkd"], TimeToString(seconds)));
                return;
            }

			if(CanSwimmingTP)
			{
				if (player.IsSwimming())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Swimming"]);
                    return;
                }
			}
			if(CanCraftTP)
			{
				if (player.inventory.crafting.queue.Count > CraftBlockTP)
				{
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Craft"]);
                    return;
                }
			}
			if(CanAliveTP)
			{
				if (!player.IsAlive())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Alive"]);
                    return;
                }
			}
			if(CanRadiationTP)
			{
				if (player.metabolism.radiation_poison.value > RadiationBlockTP)
                {
				    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Radiation"]);
                    return;
                }
			}
			if(CanBleedingTP)
			{
                if (player.metabolism.bleeding.value > BleedingBlockTP)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Bleeding"]);
                    return;
                }
			}
			if(CanWoundedTP)
			{
                if (player.IsWounded())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Wounded"]);
                    return;
                }
			}
			if(CanColdTP)
			{
			    if (player.metabolism.temperature.value < ColdBlockTP)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Cold"]);
                    return;
                }
			}

            if (tpQueue.Any(p => p.Player == player) ||
                pendings.Any(p => p.Player2 == player))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpError"]);
                return;
            }


            SendReply(player, string.Format(Messages["tprrequestsuccess"], target.displayName));
            SendReply(target, string.Format(Messages["tprpending"], player.displayName));
            Effect.server.Run(EffectPrefab1, target, 0, Vector3.zero, Vector3.forward);
            pendings.Add(new TP(target, Vector3.zero, 15, player));
        }

        [ChatCommand("tpa")]
        void cmdChatTpa(BasePlayer player, string command, string[] args)
        {
            if (!enabledTP) return;
            var tp = pendings.Find(p => p.Player == player);
            BasePlayer pendingPlayer = tp?.Player2;
            if (pendingPlayer == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpanotexist"]);
                return;
            }

			if(CanSwimmingTP)
			{
				if (player.IsSwimming())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Swimming"]);
                    return;
                }
			}
			if(CanCraftTP)
			{
				if (player.inventory.crafting.queue.Count > CraftBlockTP)
				{
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Craft"]);
                    return;
                }
			}
			if(CanAliveTP)
			{
				if (!player.IsAlive())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Alive"]);
                    return;
                }
			}
			if(CanRadiationTP)
			{
				if (player.metabolism.radiation_poison.value > RadiationBlockTP)
                {
				    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Radiation"]);
                    return;
                }
			}
			if(CanBleedingTP)
			{
                if (player.metabolism.bleeding.value > BleedingBlockTP)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Bleeding"]);
                    return;
                }
			}
			if(CanWoundedTP)
			{
                if (player.IsWounded())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Wounded"]);
                    return;
                }
			}
			if(CanColdTP)
			{
			    if (player.metabolism.temperature.value < ColdBlockTP)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Cold"]);
                    return;
                }
			}
			
            if (restrictCupboard && player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null &&
                !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpacupboard"]); return;
            }


            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }

            var time = GetTeleportTime(pendingPlayer.userID);
            pendings.Remove(tp);

            var lastTp = tpQueue.Find(p => p.Player == pendingPlayer);
            if (lastTp != null)
            {
                tpQueue.Remove(lastTp);
            }

            tpQueue.Add(new TP(pendingPlayer, player.GetNetworkPosition(), time, player));
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendReply(pendingPlayer, string.Format(Messages["tpqueue"], player.displayName, TimeToString(time)));
            SendReply(player, String.Format(Messages["tpasuccess"], pendingPlayer.displayName, TimeToString(time)));
        }


        [ChatCommand("tpc")]
        void cmdChatTpc(BasePlayer player, string command, string[] args)
        {
            var tp = pendings.Find(p => p.Player == player);
            BasePlayer target = tp?.Player2;
            if (target != null)
            {
                pendings.Remove(tp);
                SendReply(player, Messages["tpc"]);
                SendReply(target, string.Format(Messages["tpctarget"], player.displayName));
                return;
            }
			
			if(CanSwimmingTP)
			{
				if (player.IsSwimming())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Swimming"]);
                    return;
                }
			}
			if(CanCraftTP)
			{
				if (player.inventory.crafting.queue.Count > CraftBlockTP)
				{
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Craft"]);
                    return;
                }
			}
			if(CanAliveTP)
			{
				if (!player.IsAlive())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Alive"]);
                    return;
                }
			}
			if(CanRadiationTP)
			{
				if (player.metabolism.radiation_poison.value > RadiationBlockTP)
                {
				    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Radiation"]);
                    return;
                }
			}
			if(CanBleedingTP)
			{
                if (player.metabolism.bleeding.value > BleedingBlockTP)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Bleeding"]);
                    return;
                }
			}
			if(CanWoundedTP)
			{
                if (player.IsWounded())
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Wounded"]);
                    return;
                }
			}
			if(CanColdTP)
			{
			    if (player.metabolism.temperature.value < ColdBlockTP)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["Cold"]);
                    return;
                }
			}
			
            foreach (var pend in pendings)
            {
                if (pend.Player2 == player)
                {
                    SendReply(player, Messages["tpc"]);
                    SendReply(pend.Player, string.Format(Messages["tpctarget"], player.displayName));
                    pendings.Remove(pend);
                    return;
                }
            }
            foreach (var tpQ in tpQueue)
            {
                if (tpQ.Player2 != null && tpQ.Player2 == player)
                {
                    SendReply(player, Messages["tpc"]);
                    SendReply(tpQ.Player, string.Format(Messages["tpctarget"], player.displayName));
                    tpQueue.Remove(tpQ);
                    return;
                }
                if (tpQ.Player == player)
                {
                    SendReply(player, Messages["tpc"]);
                    if (tpQ.Player2 != null)
                        SendReply(tpQ.Player2, string.Format(Messages["tpctarget"], player.displayName));
                    tpQueue.Remove(tpQ);
                    return;
                }
            }
        }

        #region ADMIN COMMANDS
        void SpectateFinish(BasePlayer player)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
            player.Command("camoffset", "0,1,0");
            player.SetParent(null);
            player.gameObject.SetLayerRecursive(17);
            player.metabolism.Reset();
            player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
            var heldEntity = player.GetActiveItem()?.GetHeldEntity() as HeldEntity;
            heldEntity?.SetHeld(true);

            // Teleport to original location after spectating
            if (lastPositions.ContainsKey(player.userID))
            {
                var lastPosition = lastPositions[player.userID];
                Teleport(player, new Vector3(lastPosition.x, lastPosition.y, lastPosition.z));
                lastPositions.Remove(player.userID);
            }

            player.StopSpectating();
            SendReply(player, "Слежка закончена!");
        }

        [ChatCommand("tpl")]
        void cmdChattpGo(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !PermissionService.HasPermission(player.userID, "teleportation.admin")) return;
            if (args == null || args.Length == 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpArgsError"]);
                return;
            }
            switch (args[0])
            {
                default:
                    if (!tpsave.ContainsKey(player.userID))
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["homesmissing"]);
                        return;
                    }
                    var nametp = args[0];
                    var playerTP = tpsave[player.userID];
                    if (!playerTP.ContainsKey(nametp))
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["homenotexist"]);
                        return;
                    }
                    var position = playerTP[nametp];
                    var ret = Interface.Call("CanTeleport", player) as string;
                    if (ret != null)
                    {
                        SendReply(player, ret);
                        return;
                    }
                    var lastTp = tpQueue.Find(p => p.Player == player);
                    if (lastTp != null)
                    {
                        tpQueue.Remove(lastTp);
                    }
                    tpQueue.Add(new TP(player, position, 0));
                    Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
                    SaveData();
                    return;
                case "add":
                    if (args == null || args.Length == 1)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["settpArgsError"]);
                        return;
                    }
                    var nameAdd = args[1];
                    SetTpSave(player, nameAdd);
                    return;

                case "remove":
                    if (args == null || args.Length == 1)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["removetpArgsError"]);
                        return;
                    }
                    if (!tpsave.ContainsKey(player.userID))
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["homesmissing"]);
                        return;
                    }
                    var playertp = tpsave[player.userID];
                    var name = args[1];
                    if (!playertp.ContainsKey(name))
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["homenotexist"]);
                        return;
                    }

                    Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
                    playertp.Remove(name);
                    SaveData();
                    SendReply(player, Messages["removehomesuccess"], name);
                    return;
                case "list":
                    if (!tpsave.ContainsKey(player.userID) || tpsave[player.userID].Count == 0)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["homesmissing"]);
                        return;
                    }
                    var playerTPList = tpsave[player.userID];
                    var tplist = playerTPList.Select(x => GetSleepingBag(x.Key, x.Value) != null ? $"{x.Key} ({x.Value})" : $"{x.Key} {x.Value}");

                    SendReply(player, Messages["tplist"], string.Join("\n", tplist.ToArray()));
                    return;



            }
        }

        [ChatCommand("tpspec")]
        void cmdTPSpec(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !PermissionService.HasPermission(player.userID, "teleportation.admin")) return;
            if (args.Length == null || args.Length != 1)
            {
                SendReply(player, Messages["tpspecError"]);
                return;
            }
            string name = args[0];
            BasePlayer target = FindBasePlayer(name);
            if (target == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["playermissing"]);
                return;
            }
            if (!player.IsSpectating())
            {
                switch (args.Length)
                {
                    case 1:
                        if (!target.IsConnected)
                        {
                            SendReply(player, Messages["playermissingOff"]);
                            return;
                        }

                        if (target.IsDead())
                        {
                            SendReply(player, Messages["playermissingOrDeath"]);
                            return;
                        }

                        if (ReferenceEquals(target, player))
                        {
                            SendReply(player, Messages["playerItsYou"]);
                            return;
                        }

                        if (target.IsSpectating())
                        {
                            SendReply(player, Messages["playerItsSpec"]);
                            return;
                        }
                        spectatingPlayers.Remove(target);
                        lastPositions[player.userID] = player.GetNetworkPosition();
                        var heldEntity = player.GetActiveItem()?.GetHeldEntity() as HeldEntity;
                        heldEntity?.SetHeld(false);
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                        player.gameObject.SetLayerRecursive(10);
                        player.CancelInvoke("MetabolismUpdate");
                        player.CancelInvoke("InventoryUpdate");
                        player.ClearEntityQueue();
                        entitySnapshot.Invoke(player, new object[] { target });
                        player.gameObject.Identity();
                        player.SetParent(target);
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
                        player.Command("camoffset 0,1.3,0");
                        SendReply(player, $"Вы наблюдаете за игроком {target}! Что бы переключаться между игроками, нажимайте: Пробел");
                        break;
                }
            }
            else
            {
                SpectateFinish(player);
            }
        }

        [ChatCommand("tp")]
        void cmdTP(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !PermissionService.HasPermission(player.userID, "teleportation.admin")) return;
            switch (args.Length)
            {
                case 1:
                    string name = args[0];
                    BasePlayer target = FindBasePlayer(name);
                    if (target == null)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["playermissing"]);
                        return;
                    }
                    if (adminsLogs)
                    {
                        LogToFile("admin", $"[{DateTime.Now.ToShortTimeString()}] {player} телепортировался к {target}", this, true);
                    }
                    Teleport(player, target);
                    break;
                case 2:
                    string name1 = args[0];
                    string name2 = args[1];
                    BasePlayer target1 = FindBasePlayer(name1);
                    BasePlayer target2 = FindBasePlayer(name2);

                    if (target1 == null || target2 == null)
                    {
                        SendReply(player, Messages["playermissing"]);
                        return;
                    }
                    if (adminsLogs)
                    {
                        LogToFile("admin", $"[{DateTime.Now.ToShortTimeString()}] Игрок {player} телепортировал {target1} к {target2}", this, true);
                    }
                    Teleport(target1, target2);
                    break;
                case 3:

                    float x = float.Parse(args[0]);
                    float y = float.Parse(args[1]);
                    float z = float.Parse(args[2]);
                    if (adminsLogs)
                    {
                        LogToFile("admin", $"[{DateTime.Now.ToShortTimeString()}] Игрок {player} телепортировался на координаты: ({x} / {y} / {z})", this, true);
                    }
                    Teleport(player, x, y, z);
                    break;

            }
        }

        [ConsoleCommand("home.wipe")]
        private void CmdTest(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            WipeData();
        }


        #endregion

        #endregion

        #region OXIDE HOOKS

        public string TimeToString(double time)
        {
            TimeSpan elapsedTime = TimeSpan.FromSeconds(time);
            int hours = elapsedTime.Hours;
            int minutes = elapsedTime.Minutes;
            int seconds = elapsedTime.Seconds;
            int days = Mathf.FloorToInt((float)elapsedTime.TotalDays);
            string s = "";

            if (days > 0) s += $"{days} дн.";
            if (hours > 0) s += $"{hours} ч. ";
            if (minutes > 0) s += $"{minutes} мин. ";
            if (seconds > 0) s += $"{seconds} сек.";
            else s = s.TrimEnd(' ');
            return s;
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            pendings.RemoveAll(p => p.Player == player || p.Player2 == player);
            tpQueue.RemoveAll(p => p.Player == player || p.Player2 == player);
        }

        void OnNewSave()
        {
            if (wipedData)
            {
                PrintWarning("Обнаружен вайп. Очищаем данные с data/Teleportation");

                Interface.Oxide.DataFileSystem.WriteObject("Teleportation/Homes", new Dictionary<string, FileInfo>());
                Interface.Oxide.DataFileSystem.WriteObject("Teleportation/AdminTpSave", new Dictionary<string, FileInfo>());
                LoadData();
                Interface.Oxide.ReloadPlugin(Title);
            }
        }

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            PermissionService.RegisterPermissions(this, new List<string>() { TPADMIN });
            LoadData();
            timer.Every(1f, TeleportationTimerHandle);
        }

        void Unload()
        {
            SaveData();
        }

        void OnEntityBuilt(Planner planner, GameObject gameobject)
        {
            if (planner == null || gameobject == null) return;
            var player = planner.GetOwnerPlayer();
            BaseEntity entity = gameobject.ToBaseEntity();
            if (entity == null) return;
            if (gameobject.name.Contains("foundation"))
            {
                var pos = gameobject.transform.position;
                foreach (var pending in tpQueue)
                {
                    if (Vector3.Distance(pending.pos, pos) < 3)
                    {
                        entity.Kill();
                        SendReply(planner.GetOwnerPlayer(), "Нельзя, тут телепортируется игрок!");
                        return;
                    }
                }
            }
        }

        #endregion

        #region CORE

        void TeleportationTimerHandle()
        {
            List<ulong> tpkdToRemove = new List<ulong>();
            foreach (var uid in cooldownsTP.Keys.ToList())
            {
                if (--cooldownsTP[uid] <= 0)
                {
                    tpkdToRemove.Add(uid);
                }
            }
            tpkdToRemove.ForEach(p => cooldownsTP.Remove(p));

            List<ulong> tpkdHomeToRemove = new List<ulong>();
            foreach (var uid in cooldownsHOME.Keys.ToList())
            {
                if (--cooldownsHOME[uid] <= 0)
                {
                    tpkdHomeToRemove.Add(uid);
                }
            }
            tpkdHomeToRemove.ForEach(p => cooldownsHOME.Remove(p));

            for (int i = pendings.Count - 1; i >= 0; i--)
            {
                var pend = pendings[i];
                if (pend.Player2 != null && pend.Player2.IsConnected && pend.Player2.IsWounded())
                {
                    SendReply(pend.Player2, Messages["tpwounded"]);
                    pendings.RemoveAt(i);
                    continue;
                }
                if (--pend.seconds <= 0)
                {
                    pendings.RemoveAt(i);
                    if (pend.Player2 != null && pend.Player2.IsConnected) SendReply(pend.Player2, Messages["tppendingcanceled"]);
                    if (pend.Player != null && pend.Player.IsConnected) SendReply(pend.Player, Messages["tpacanceled"]);
                }
            }
            for (int i = tpQueue.Count - 1; i >= 0; i--)
            {
                var reply = 229;
                var tp = tpQueue[i];
                if (tp.Player != null && tp.Player.IsConnected && (tp.Player.metabolism.bleeding.value > 0 || tp.Player.IsWounded()))
                {
                    SendReply(tp.Player, Messages["tpwounded"]);
                    if (tp.Player2 != null && tp.Player.IsConnected)
                        SendReply(tp.Player2, Messages["tpWoundedTarget"]);
                    tpQueue.RemoveAt(i);
                    continue;
                }
                if (--tp.seconds <= 0)
                {
                    tpQueue.RemoveAt(i);
                    var ret = Interface.CallHook("CanTeleport", tp.Player) as string;
                    if (ret != null)
                    {
                        SendReply(tp.Player, ret);
                        continue;
                    }
                    Teleport(tp.Player, tp.pos);

                    if (tp.Player2 != null && tp.Player != null && tp.Player.IsConnected && tp.Player2.IsConnected)
                    {
                        var seconds = GetKD(tp.Player.userID);
                        SetCooldown(tp.Player, "tp", seconds);
                        cooldownsTP[tp.Player.userID] = seconds;
                        SendReply(tp.Player, string.Format(Messages["tpplayersuccess"], tp.Player2.displayName));
                    }
                    else
                    if (tp.Player != null && tp.Player.IsConnected)
                    {
                        var seconds = GetKDHome(tp.Player.userID);
                        SetCooldown(tp.Player, "home", seconds);
                        cooldownsHOME[tp.Player.userID] = seconds;
                        SendReply(tp.Player, Messages["tphomesuccess"]);
                    }
                    NextTick(() => Interface.CallHook("OnPlayerTeleported", tp.Player));
                }
            }
        }

        void SetTpSave(BasePlayer player, string name)
        {
            var uid = player.userID;
            var pos = player.GetNetworkPosition();


            Dictionary<string, Vector3> adminTP;
            if (!tpsave.TryGetValue(uid, out adminTP))
                adminTP = (tpsave[uid] = new Dictionary<string, Vector3>());

            if (adminTP.ContainsKey(name))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["homeexist"]);
                return;
            }
            adminTP.Add(name, pos);
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendReply(player, Messages["homesucces"], name);
            SaveData();
            timer.Once(10f, () => sethomeBlock.Remove(player.userID));
        }

        void SetHome(BasePlayer player, string name)
        {
            var uid = player.userID;
            var pos = player.GetNetworkPosition();

            if (player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null &&
                !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["sethomecupboard"]);
                return;
            }
            Dictionary<string, Vector3> playerHomes;
            if (!homes.TryGetValue(uid, out playerHomes))
                playerHomes = (homes[uid] = new Dictionary<string, Vector3>());
            if (GetHomeLimit(uid) == playerHomes.Count)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["maxhomes"]);
                return;
            }

            if (playerHomes.ContainsKey(name))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["homeexist"]);
                return;
            }
            playerHomes.Add(name, pos);

            if (createSleepingBug)
            {
                CreateSleepingBag(player, pos, name);
            }

            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendReply(player, Messages["homesucces"], name);
            sethomeBlock.Add(player.userID);
            SaveData();
            timer.Once(10f, () => sethomeBlock.Remove(player.userID));
        }

        int GetKDHome(ulong uid)
        {
            int min = tpkdhomeDefault;
            foreach (var privilege in tpkdhomePerms)
                if (PermissionService.HasPermission(uid, privilege.Key))
                    min = Mathf.Min(min, privilege.Value);
            return min;
        }

        int GetKD(ulong uid)
        {
            int min = tpkdDefault;
            foreach (var privilege in tpkdPerms)
                if (PermissionService.HasPermission(uid, privilege.Key))
                    min = Mathf.Min(min, privilege.Value);
            return min;
        }
        int GetHomeLimit(ulong uid)
        {
            int max = homelimitDefault;
            foreach (var privilege in homelimitPerms)
                if (PermissionService.HasPermission(uid, privilege.Key))
                    max = Mathf.Max(max, privilege.Value);
            return max;
        }
        int GetTeleportTime(ulong uid)
        {
            int min = teleportSecsDefault;
            foreach (var privilege in teleportSecsPerms)
                if (PermissionService.HasPermission(uid, privilege.Key))
                    min = Mathf.Min(min, privilege.Value);
            return min;
        }
        BaseEntity GetBuldings(Vector3 pos)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(pos, Vector3.down), out hit, 0.1f))
            {
                var entity = hit.GetEntity();
                if (entity != null && entity.ShortPrefabName.Contains("floor"))
                    return entity;
                if (entity != null && entity.ShortPrefabName.Contains("foundation"))
                    return entity;
            }
            return null;
        }
        BaseEntity GetFoundation(Vector3 pos)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(pos, Vector3.down), out hit, 0.1f))
            {
                var entity = hit.GetEntity();
                if (entity != null && entity.ShortPrefabName.Contains("foundation"))
                    return entity;
            }
            return null;
        }


        SleepingBag GetSleepingBag(string name, Vector3 pos)
        {
            List<SleepingBag> sleepingBags = new List<SleepingBag>();
            Vis.Components(pos, .1f, sleepingBags);
            return sleepingBags.Count > 0 ? sleepingBags[0] : null;
        }

        void CreateSleepingBag(BasePlayer player, Vector3 pos, string name)
        {
            SleepingBag sleepingBag =
                GameManager.server.CreateEntity("assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab", pos,
                    Quaternion.identity) as SleepingBag;
            if (sleepingBag == null) return;
            sleepingBag.skinID = 1174407153;
            sleepingBag.deployerUserID = player.userID;
            sleepingBag.niceName = SleepingBagName;
            sleepingBag.OwnerID = player.userID;
            //SleepingBagUnlockTimeField.SetValue(sleepingBag, Time.realtimeSinceStartup + 300f);
            sleepingBag.Spawn();
        }

        #endregion

        #region API

        Dictionary<string, Vector3> GetHomes(ulong uid)
        {
            Dictionary<string, Vector3> positions;
            if (!homes.TryGetValue(uid, out positions))
                return null;
            return positions.ToDictionary(p => p.Key, p => p.Value);
        }

        #endregion

        #region TELEPORTATION

        public void Teleport(BasePlayer player, BasePlayer target) => Teleport(player, target.transform.position);

        public void Teleport(BasePlayer player, float x, float y, float z) => Teleport(player, new Vector3(x, y, z));

        public void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.IsDead() && player.IsConnected)
            {
                player.RespawnAt(position, Quaternion.identity);
                return;
            }
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");

            player.StartSleeping();
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            //player.UpdatePlayerCollider(true, false);
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            //TODO temporary for potential rust bug
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

        #endregion

        #region Cooldown
        DynamicConfigFile cooldownsFile = Interface.Oxide.DataFileSystem.GetFile("Teleportation/Cooldowns");

        private class Cooldown
        {
            public ulong UserId;
            public double Expired;
            [JsonIgnore]
            public Action OnExpired;
        }
        private static Dictionary<string, List<Cooldown>> cooldowns;

        internal static void Service()
        {
            var time = GrabCurrentTime();
            List<string> toRemove = new List<string>();
            foreach (var cd in cooldowns)
            {
                var keyCooldowns = cd.Value;
                List<string> toRemoveCooldowns = new List<string>();
                for (var i = keyCooldowns.Count - 1; i >= 0; i--)
                {
                    var cooldown = keyCooldowns[i];
                    if (cooldown.Expired < time)
                    {
                        cooldown.OnExpired?.Invoke();
                        keyCooldowns.RemoveAt(i); ;
                    }
                }
                if (keyCooldowns.Count == 0) toRemove.Add(cd.Key);
            }
            toRemove.ForEach(p => cooldowns.Remove(p));
        }

        public static void SetCooldown(BasePlayer player, string key, int seconds, Action onExpired = null)
        {
            List<Cooldown> cooldownList;
            if (!cooldowns.TryGetValue(key, out cooldownList))
                cooldowns[key] = cooldownList = new List<Cooldown>();
            cooldownList.Add(new Cooldown()
            {
                UserId = player.userID,
                Expired = GrabCurrentTime() + (double)seconds,
                OnExpired = onExpired
            });
        }

        public static int GetCooldown(BasePlayer player, string key)
        {
            List<Cooldown> source = new List<Cooldown>();
            if (cooldowns.TryGetValue(key, out source))
            {
                Cooldown cooldown = source.FirstOrDefault<Cooldown>((Func<Cooldown, bool>)(p => (long)p.UserId == (long)player.userID));
                if (cooldown != null)
                    return (int)(cooldown.Expired - GrabCurrentTime());
            }
            return 0;
        }
		
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        #endregion

        #region DATA

        DynamicConfigFile homesFile = Interface.Oxide.DataFileSystem.GetFile("Teleportation/Homes");
        DynamicConfigFile tpsaveFile = Interface.Oxide.DataFileSystem.GetFile("Teleportation/AdminTpSave");

        void OnServerSave()
        {
            SaveData();
        }

        void LoadData()
        {
            tpsaveFile.Settings.Converters.Add(converter);
            tpsave = tpsaveFile.ReadObject<Dictionary<ulong, Dictionary<string, Vector3>>>();

            homesFile.Settings.Converters.Add(converter);
            homes = homesFile.ReadObject<Dictionary<ulong, Dictionary<string, Vector3>>>();
            cooldowns = cooldownsFile.ReadObject<Dictionary<string, List<Cooldown>>>() ??
                        new Dictionary<string, List<Cooldown>>();
        }

        void SaveData()
        {
            tpsaveFile.WriteObject(tpsave);
            homesFile.WriteObject(homes);
            cooldownsFile.WriteObject(cooldowns);
        }

        #endregion

        #region LOCALIZATION

        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"foundationmissing", "Фундамент не найден!" },
            {"foundationmissingR", "Фундамент не найден, местоположение было удалено!" },
            {"playerisyou", "Нельзя отправлять телепорт самому себе!" },
            {"maxhomes", "У вас максимальное кол-во местоположений!" },
            {"homeexist", "Такое местоположение уже существует!" },
            {"homesucces", "Местоположение {0} успешно установлено!" },
            {"sethomeArgsError", "Для установки местоположения используйте /sethome ИМЯ" },
            {"settpArgsError", "Для установки местоположения используйте /tpl add ИМЯ" },
            {"homeArgsError", "Для телепортации на местоположение используйте /home ИМЯ" },
            {"tpArgsError", "Для телепортации на местоположение используйте /tpl ИМЯ" },
            {"tpError", "Запрещено! Вы в очереди на телепортацию" },
            {"homenotexist", "Местоположение с таким названием не найдено!" },
            {"homequeue", "Телепортация на {0} будет через {1}" },
            {"tpwounded", "Вы получили ранение, телепортация отменена!" },
            {"tphomesuccess", "Вы телепортированы домой!" },
            {"tptpsuccess", "Вы телепортированы на указаное место!" },
            {"homesmissing", "У вас нет доступных местоположений!" },
            {"removehomeArgsError", "Для удаления местоположения используйте /removehome ИМЯ" },
            {"removetpArgsError", "Для удаления местоположения используйте /tpl remove ИМЯ" },
            {"removehomesuccess", "Местоположение {0} успешно удалено" },
            {"sleepingbagmissing", "Спальный мешок не найден, местоположение удалено!" },
            {"tprArgsError", "Для отправки запроса на телепортация используйте /tpr НИК" },
            {"playermissing", "Игрок не найден" },
            {"tpspecError", "Не правильно введена команда. Используйте: /tpspec НИК" },
            {"playermissingOff", "Игрок не в сети" },
            {"playermissingOrDeath", "Игрок не найден, или он мёртв" },
            {"playerItsYou", "Нельзя следить за самым собой" },
            {"playerItsSpec", "Игрок уже за кем то наблюдает" },
            {"tprrequestsuccess", "Запрос {0} успешно отправлен" },
            {"tprpending", "{0} отправил вам запрос на телепортацию\nЧтобы принять используйте /tpa\nЧтобы отказаться используйте /tpc" },
            {"tpanotexist", "У вас нет активных запросов на телепортацию!" },
            {"tpqueue", "{0} принял ваш запрос на телепортацию\nВы будете телепортированы через {1}" },
            {"tpc", "Телепортация успешно отменена!" },
            {"tpctarget", "{0} отменил телепортацию!" },
            {"tpplayersuccess", "Вы успешно телепортировались к {0}" },
            {"tpasuccess", "Вы приняли запрос телепортации от {0}\nОн будет телепортирован через {1}" },
            {"tppendingcanceled", "Запрос телепортации отменён" },
            {"tpcupboard", "Телепортация в зоне действия чужого шкафа запрещена!" },
            {"tphomecupboard", "Телепортация домой в зоне действия чужого шкафа запрещена!" },
            {"tpacupboard", "Принятие телепортации в зоне действия чужого шкафа запрещена!" },
            {"sethomecupboard", "Установка местоположения в зоне действия чужого шкафа запрещена!" },
            {"tpacanceled", "Вы не ответили на запрос." },
            {"tpkd", "Телепортация на перезарядке!\nОсталось {0}" },
            {"tpWoundedTarget", "Игрок ранен, телепортация отменена!" },
			{"Radiation", "Телепортация во время радиационного облучения запрещена!\nТелепорт отменён." },
            {"Bleeding", "Телепортация во время кровотечения запрещена!\nТелепорт отменён." },
			{"Swimming", "Телепортация в воде запрещена!\nТелепорт отменён."},
			{"Craft", "Телепортация во время крафта запрещена!\nТелепорт отменён."},
			{"Alive", "Вы погибли!\nТелепорт отменён."},
            {"Cold", "Вам холодно, телепортация отменена!" },
			{"Wounded", "Вы получили ранения, телепорт отменён!" },
            {"sethomeBlock", "Нельзя использовать /sethome слишком часто, попробуйте позже!" },
            {"foundationowner", "Нельзя использовать /sethome не на своих строениях!" },
            {"foundationownerFC", "Создатель обьекта не являеться вашим соклановцем или другом, /sethome запрещен" },
            {"homeslist", "Доступное количество местоположений: {0}\n{1}" },
            {"tplist", "Ваши сохраненные метоположения:\n{0}" }


        };

        #endregion

        #region VECTOR CONVERTER

        static UnityVector3Converter converter = new UnityVector3Converter();
        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }

        #endregion

    }
}

                   