using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("DVTeleportation", "Sempai#3239", "1.5.0")]
    [Description("Спасибо за покупку - by Sempai#3239")]
    class DVTeleportation : RustPlugin
    {
        [PluginReference] Plugin Clans;
        [PluginReference] Plugin Friends;
        Dictionary<ulong, Vector3> lastPositions = new Dictionary<ulong, Vector3>();
        Dictionary<BasePlayer, int> spectatingPlayers = new Dictionary<BasePlayer, int>();
        bool IsClanMember(ulong playerid = 294912, ulong targetID = 0) => (bool)(Clans?.Call("HasFriend", playerid, targetID) ?? false);
        bool IsFriends(ulong playerID = 0, ulong friendId = 0) => (bool)(Friends?.Call("AreFriends", playerID, friendId) ?? false);

        bool IsTeamate(BasePlayer player, ulong targetID)
        {
            if (player.currentTeam == 0) return false;
            var team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
            if (team == null) return false;
            var list = RelationshipManager.ServerInstance.FindTeam(player.currentTeam).members.Where(p => p == targetID).ToList();
            return list.Count > 0;
        }

        class TP
        {
            public BasePlayer Player;
            public BasePlayer Player2;
            public Vector3 pos;
            public float totalSeconds;
            public bool EnabledShip;
            public int seconds;
            public bool TPL;
            public TP(BasePlayer player, Vector3 Pos, int Seconds, bool EnabledShip1, bool tpl, BasePlayer player2 = null)
            {    
                Player = player;
                pos = Pos;
                seconds = Seconds;
                totalSeconds = Seconds;
                EnabledShip = EnabledShip1;
                Player2 = player2;
                TPL = tpl;
            }
        }
        const string TPADMIN = "DVTeleportation.admin";
        int homelimitDefault;
        Dictionary<string, int> homelimitPerms;
        int tpkdDefault;
        Dictionary<string, int> tpkdPerms;
        int tpkdhomeDefault;
        Dictionary<string, int> tpkdhomePerms;
        int teleportSecsDefault;
        int resetPendingTime;
        bool restrictCupboard;
        bool enabledTPR;
        bool homecupboard;
        bool homecupboardblock;
        bool adminsLogs;
        bool foundationOwner;
        bool foundationOwnerFC;

        bool restrictTPRCupboard;
        bool foundationEx;
        bool wipedData;
        bool createSleepingBug;
        string EffectPrefab1;
        string EffectPrefab;
        bool EnabledShipTP;
        bool EnabledBallonTP;
        bool CancelTPMetabolism;
        bool CancelTPCold;
        bool CancelTPRadiation;
        bool FriendsEnabled;
        bool CancelTPWounded;
        bool EnabledTPLForPlayers;
        int TPLCooldown;
        int TplPedingTime;
        bool TPLAdmin;
        string AutoTPAPermission;

        static DynamicConfigFile config;
        Dictionary<string, int> teleportSecsPerms;
        void OnNewSave()
        {
            if (wipedData)
            {
                PrintWarning("Обнаружен вайп. Очищаем данные с data/DVTeleportation");
                WipeData();
            }
        }
        void WipeData()
        {
            LoadData();
            tpsave = new List<TPList>();
            homes = new Dictionary<ulong, Dictionary<string, Vector3>>();
            SaveData();
        }
        protected override void LoadDefaultConfig()
        {
            GetVariable(Config, "Запрещать отправлять запрос на телепортацию в зоне действия чужого шкафа", out homecupboard, true);
            GetVariable(Config, "Удалять точку теоепртации если она в билде в какой не авторизован игрок", out homecupboard, true);
            GetVariable(Config, "Звук уведомления при получение запроса на телепорт (пустое поле = звук отключен)", out EffectPrefab1, "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab");
            GetVariable(Config, "Звук предупреждения (пустое поле = звук отключен)", out EffectPrefab, "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab");
            GetVariable(Config, "Разрешать сохранять местоположение только на фундаменте", out foundationEx, true);
            GetVariable(Config, "Создавать объект при сохранении местоположения в виде Sleeping Bag", out createSleepingBug, true);
            GetVariable(Config, "Автоматический вайп данных при генерации новой карты", out wipedData, true);
            GetVariable(Config, "Запрещать принимать запрос на телепортацию в зоне действия чужого шкафа", out restrictCupboard, true);
            GetVariable(Config, "Запрещать сохранять местоположение если игрок не является владельцем фундамента", out foundationOwner, true);
            GetVariable(Config, "Привилегия на использование автоматического приёма телепорта", out AutoTPAPermission, "DVTeleportation.autotpa");
            GetVariable(Config, "Разрешать сохранять местоположение если игрок является другом или соклановцем или тимейтом владельца фундамента ", out foundationOwnerFC, true);
            GetVariable(Config, "Логировать использование команд для администраторов", out adminsLogs, true);
            GetVariable(Config, "Включить телепортацию (TPR/TPA) только к друзьям, соклановкам или тимейту", out FriendsEnabled, true);
            GetVariable(Config, "Разрешить команду TPR игрокам (false = /tpr не будет работать)", out enabledTPR, true);
            GetVariable(Config, "Разрешить отправку и приём телепорта и телепорт домой на корабле", out EnabledShipTP, true);
            GetVariable(Config, "Разрешить отправку и приём телепорта на воздушном шаре", out EnabledBallonTP, true);
            GetVariable(Config, "Запрещать отправлять запрос на телепортацию в зоне действия чужого шкафа", out restrictTPRCupboard, true);
            GetVariable(Config, "Отмета телепорта игрока (Home/TP) если у него кровотечение", out CancelTPMetabolism, true);
            GetVariable(Config, "Отмета телепорта игрока (Home/TP) если игрок ранен", out CancelTPWounded, true);
            GetVariable(Config, "Отмета телепорта игрока (Home/TP) если ему холодно", out CancelTPCold, true);
            GetVariable(Config, "Отмета телепорта игрока (Home/TP) если он облучен радиацией", out CancelTPRadiation, true);
            GetVariable(Config, "Время ответа на запрос телепортации (в секундах)", out resetPendingTime, 15);
            GetVariable(Config, "Ограничение на количество сохранённых местоположений", out homelimitDefault, 3);
            GetVariable(Config, "[TPL] Разрешить игрокам использовать TPL", out EnabledTPLForPlayers, false);
            GetVariable(Config, "[TPL] Задержка телепортации игрока на TPL", out TplPedingTime, 15);
            GetVariable(Config, "[TPL] Cooldown телепортации игрока на TPL", out TPLCooldown, 15);
            GetVariable(Config, "[TPL] Телепортировать админа без задержки и кулдауна?", out TPLAdmin, true);
            Config["Ограничение на количество сохранённых местоположений с привилегией"] = homelimitPerms = GetConfig("Ограничение на количество сохранённых местоположений с привилегией", new Dictionary<string, object>() {
                    {
                    "DVTeleportation.vip", 5
                }
            }
            ).ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, homelimitPerms.Keys.ToList());
            GetVariable(Config, "Длительность задержки перед телепортацией (в секундах)", out teleportSecsDefault, 15);
            Config["Длительность задержки перед телепортацией с привилегией (в секундах)"] = teleportSecsPerms = GetConfig("Длительность задержки перед телепортацией с привилегией (в секундах)", new Dictionary<string, object>() {
                    {
                    "DVTeleportation.vip", 10
                }
            }
            ).ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, teleportSecsPerms.Keys.ToList());
            GetVariable(Config, "Длительность перезарядки телепорта (в секундах)", out tpkdDefault, 300);
            Config["Длительность перезарядки телепорта с привилегией (в секундах)"] = tpkdPerms = GetConfig("Длительность перезарядки телепорта с привилегией (в секундах)", new Dictionary<string, object>() {
                    {
                    "DVTeleportation.vip", 150
                }
            }
            ).ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, tpkdPerms.Keys.ToList());
            GetVariable(Config, "Длительность перезарядки телепорта домой (в секундах)", out tpkdhomeDefault, 300);
            Config["Длительность перезарядки телепорта домой с привилегией (в секундах)"] = tpkdhomePerms = GetConfig("Длительность перезарядки телепорта домой с привилегией (в секундах)", new Dictionary<string, object>() {
                    {
                    "DVTeleportation.vip", 150
                }
            }
            ).ToDictionary(p => p.Key, p => Convert.ToInt32(p.Value));
            PermissionService.RegisterPermissions(this, tpkdhomePerms.Keys.ToList());
            SaveConfig();
        }
        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        public static void GetVariable<T>(DynamicConfigFile config, string name, out T value, T defaultValue)
        {
            config[name] = value = config[name] == null ? defaultValue : (T)Convert.ChangeType(config[name], typeof(T));
        }
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
        public BasePlayer FindBasePlayer(string nameOrUserId)
        {
            nameOrUserId = nameOrUserId.ToLower();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId) return player;
            }
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId) return player;
            }
            return default(BasePlayer);
        }
        Dictionary<ulong, Dictionary<string, Vector3>> homes;
        List<TPList> tpsave;
        class TPList
        {
            public string Name;
            public Vector3 pos;
        }
        Dictionary<ulong, int> cooldownsTP = new Dictionary<ulong, int>();
        Dictionary<ulong, int> cooldownsHOME = new Dictionary<ulong, int>();
        List<TP> tpQueue = new List<TP>();
        List<TP> pendings = new List<TP>();
        List<ulong> sethomeBlock = new List<ulong>();

        /*[ChatCommand("atp")]
        void cmdAutoTPA(BasePlayer player, string com, string[] args)
        {
            if (!PermissionService.HasPermission(player.userID, AutoTPAPermission))
            {
                SendReply(player, Messages["TPAPerm"]);
                return;
            }

            if (!AutoTPA.ContainsKey(player.userID))
                AutoTPA.Add(player.userID, new AutoTPASettings());


            var data = AutoTPA[player.userID];

            if (args == null || args.Length <= 0)
            {
                if (data.Enabled)
                {
                    data.Enabled = false;
                    SendReply(player, Messages["TPADisable"]);

                }
                else
                {
                    data.Enabled = true;
                    SendReply(player, string.Format(Messages["TPAEnabled"], Messages["TPAEnabledInfo"]));
                }
                return;
            }

            switch (args[0])
            {
                case "add":
                    if (args.Length < 2)
                    {
                        SendReply(player, Messages["TPAEAddError"]);
                        return;
                    }

                    var target = covalence.Players.FindPlayers(args[1]).ToList();

                    if (target == null || target.Count <= 0)
                    {
                        SendReply(player, string.Format(Messages["TPAEAddPlayerNotFound"], "⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠"));
                        return;
                    }

                    if (target.Count > 1)
                    {
                        SendReply(player, string.Format(Messages["TPAEAddPlayers"], string.Join(", ", target.Select(p => p.Name).Take(5))));
                        return;
                    }
                    if (data.PlayersList.ContainsKey(target[0].Name))
                    {
                        SendReply(player, string.Format(Messages["TPAEAddContains"], target[0].Name));
                        return;
                    }

                    data.PlayersList.Add(target[0].Name, ulong.Parse(target[0].Id));

                    SendReply(player, string.Format(Messages["TPAEAddSuccess"], target[0].Name));

                    break;
                case "remove":
                    if (args.Length < 2)
                    {
                        SendReply(player, Messages["TPARemoveError"]);
                        return;
                    }
                    var key = data.PlayersList.FirstOrDefault(p => p.Key.ToLower().Contains(args[1].ToLower())).Key;
                    if (string.IsNullOrEmpty(key))
                    {
                        SendReply(player, string.Format(Messages["TPAEAddPlayerNotFound"], string.Format(Messages["TPAEnabledList"], string.Join(", ", data.PlayersList.Select(p => p.Key)))));
                        return;
                    }

                    data.PlayersList.Remove(key);
                    SendReply(player, string.Format(Messages["TPAERemoveSuccess"], key));

                    break;
                case "list":
                    if (data.PlayersList.Count <= 0)
                        SendReply(player, Messages["TPAEListNotFound"]);
                    else
                        SendReply(player, string.Format(Messages["TPAEnabledList"], string.Join(", ", data.PlayersList.Select(p => p.Key))));
                    break;
            }
        }
        */

        [ChatCommand("sethome")]
        void cmdChatSetHome(BasePlayer player, string command, string[] args)
        {
            var uid = player.userID;
            var pos = player.transform.position;
            var entity = new BaseEntity();

            RaycastHit hitInfo;
            if (!Physics.Raycast(player.transform.position, Vector3.down, out hitInfo, 1f))
            {
                SendReply(player, Messages["buildingBrockmissing"]);
                return;
            }
            if (hitInfo.GetEntity() == null || !(hitInfo.GetEntity() is BuildingBlock))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["buildingBrockmissing"]);
                return;
            }

            if (foundationEx && !hitInfo.GetEntity().ShortPrefabName.Contains("foundation"))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["foundationmissing"]);
                return;
            }
            entity = hitInfo.GetEntity();
            var point = player.transform.position;
            point.y = player.transform.position.y;
            pos = point;
            if (args.Length != 1)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["sethomeArgsError"]);
                return;
            }
            if (CancelTPMetabolism && player.metabolism.bleeding.value > 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["woundedAction"]);
                return;
            }
            if (CancelTPRadiation && player.metabolism.radiation_poison.value > 10)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["Radiation"]);
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["woundedAction"]);
                return;
            }
            if (sethomeBlock.Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["sethomeBlock"]);
                return;
            }

            if (foundationOwnerFC && foundationOwner)
            {

                if (entity.OwnerID != uid)
                {
                    if (!IsFriends(entity.OwnerID, player.userID) && !IsClanMember(entity.OwnerID, player.userID) && !IsTeamate(player, entity.OwnerID))
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["foundationownerFC"]);
                        return;
                    }
                }
            }
            if (foundationOwner)
            {

                if (entity.OwnerID != uid && foundationOwnerFC == (!IsFriends(entity.OwnerID, player.userID) && !IsClanMember(entity.OwnerID, player.userID) && IsTeamate(player, entity.OwnerID)))
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["foundationowner"]);
                    return;
                }
            }
            var name = args[0];
            SetHome(player, name, pos);
        }

        [ChatCommand("removehome")]
        void cmdChatRemoveHome(BasePlayer player, string command, string[] args)
        {
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
            if (!homes.ContainsKey(player.userID) || homes[player.userID].Count == 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["homesmissing"]);
                return;
            }
            var playerHomes = homes[player.userID];
            var time = (GetHomeLimit(player.userID) - playerHomes.Count);
            var homelist = playerHomes.Select(x => GetSleepingBag(x.Key, x.Value) != null ? $"{x.Key} {x.Value}" : $"Дом: {x.Key} {x.Value}");

            foreach (var home in playerHomes.ToList())
            {
                if (createSleepingBug)
                    if (!GetSleepingBag(home.Key, home.Value)) playerHomes.Remove(home.Key);
            }
            SendReply(player, Messages["homeslist"], time, string.Join("\n", homelist.ToArray()));
        }
        [ChatCommand("home")]
        void cmdChatHome(BasePlayer player, string command, string[] args)
        {
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
            if (!EnabledShipTP && player.GetParentEntity() is CargoShip)
            {
                SendReply(player, Messages["PlayerIsOnCargoShip"]);
                return;
            }
            if (!EnabledBallonTP && player.GetParentEntity() is HotAirBalloon)
            {
                SendReply(player, Messages["PlayerIsOnHotAirBalloon"]);
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                SendReply(player, Messages["woundedAction"]);
                return;
            }
            if (player.metabolism.bleeding.value > 0 && CancelTPMetabolism)
            {
                SendReply(player, Messages["woundedAction"]);
                return;
            }

            if (CancelTPRadiation && player.metabolism.radiation_poison.value > 10)
            {
                SendReply(player, Messages["Radiation"]);
                return;
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
                    foreach (var sleepingBag in SleepingBag.FindForPlayer(player.userID, true))
                    {
                        if (Vector3.Distance(sleepingBag.transform.position, playerHomes[name]) < 1)
                        {
                            sleepingBag.Kill();
                            break;
                        }
                    }
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["sleepingbagmissing"]);
                    playerHomes.Remove(name);
                    return;
                }

                if (bag.GetBuildingPrivilege() != null && !bag.GetBuildingPrivilege().IsAuthed(player))
                {
                    foreach (var sleepingBag in SleepingBag.FindForPlayer(player.userID, true))
                    {
                        if (Vector3.Distance(sleepingBag.transform.position, playerHomes[name]) < 1)
                        {
                            sleepingBag.Kill();
                            break;
                        }
                    }
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["sleepingbugbuildblock"]);
                    playerHomes.Remove(name);
                    return;
                }
            }
            else
            {
                var bulds = GetBuldings(pos);
                if (bulds == null)
                {
                    foreach (var sleepingBag in SleepingBag.FindForPlayer(player.userID, true))
                    {
                        if (Vector3.Distance(sleepingBag.transform.position, playerHomes[name]) < 1)
                        {
                            sleepingBag.Kill();
                            break;
                        }
                    }
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["buildingBrockmissingR"]);
                    playerHomes.Remove(name);
                    return;
                }

                if (bulds.GetBuildingPrivilege() != null && !bulds.GetBuildingPrivilege().IsAuthed(player))
                {
                    foreach (var sleepingBag in SleepingBag.FindForPlayer(player.userID, true))
                    {
                        if (Vector3.Distance(sleepingBag.transform.position, playerHomes[name]) < 1)
                        {
                            sleepingBag.Kill();
                            break;
                        }
                    }
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["sleepingbugbuildblock"]);
                    playerHomes.Remove(name);

                    return;
                }
            }
            if (CancelTPCold && player.metabolism.temperature.value < 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["coldplayer"]);
                return;
            }
            if (tpQueue.Any(p => p.Player == player) || pendings.Any(p => p.Player2 == player))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpError"]);
                return;
            }
            var lastTp = tpQueue.Find(p => p.Player == player);
            if (lastTp != null)
            {
                tpQueue.Remove(lastTp);
            }
            tpQueue.Add(new TP(player, pos, time, false, false));
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendReply(player, String.Format(Messages["homequeue"], name, TimeToString(time)));
        }
        [ChatCommand("tpr")]
        void cmdChatTpr(BasePlayer player, string command, string[] args)
        {
            if (!enabledTPR) return;
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
            if (!EnabledShipTP && player.GetParentEntity() is CargoShip)
            {
                SendReply(player, Messages["PlayerIsOnCargoShip"]);
                return;
            }
            if (!EnabledBallonTP && player.GetParentEntity() is HotAirBalloon)
            {
                SendReply(player, Messages["PlayerIsOnHotAirBalloon"]);
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                SendReply(player, Messages["woundedAction"]);
                return;
            }
            if (player.metabolism.bleeding.value > 0 && CancelTPMetabolism)
            {
                SendReply(player, Messages["woundedAction"]);
                return;
            }
            if (CancelTPRadiation && player.metabolism.radiation_poison.value > 10)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["Radiation"]);
                return;
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
            if (target == null || !target.IsConnected)
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
            if (FriendsEnabled)
                if (!IsFriends(target.userID, player.userID) && !IsTeamate(player, target.userID) && !IsClanMember(player.userID, target.userID))
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["PlayerNotFriend"]);
                    return;
                }
            int seconds = 0;
            if (restrictCupboard && player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null && !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
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
            if (CancelTPCold && player.metabolism.temperature.value < 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["coldplayer"]);
                return;
            }
            if (tpQueue.Any(p => p.Player == player) || pendings.Any(p => p.Player2 == player))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpError"]);
                return;
            }

            if (tpQueue.Any(p => p.Player == target) || pendings.Any(p => p.Player2 == target))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpError"]);
                return;
            }
            SendReply(player, string.Format(Messages["tprrequestsuccess"], target.displayName));
            SendReply(target, string.Format(Messages["tprpending"], player.displayName));
            Effect.server.Run(EffectPrefab1, target, 0, Vector3.zero, Vector3.forward);

            DVTeleportationAcceptUI(target, player.displayName);
            pendings.Add(new TP(target, Vector3.zero, 15, false, false, player));
            if (!IsFriends(target.userID, player.userID) && !IsClanMember(player.userID, target.userID) && !player.Team.members.Contains(target.userID)) return;
            cmdChatTpa(target, command, new string[] { "atp" });
            SendReply(target, Messages["TPASuccess"]);
        }
        [ChatCommand("tpa")]
        void cmdChatTpa(BasePlayer player, string command, string[] args)
        {
            if (!enabledTPR) return;
            var tp = pendings.Find(p => p.Player == player);
            if (tp == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpanotexist"]);
                return;
            }
            BasePlayer pendingPlayer = tp.Player2;
            if (pendingPlayer == null)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpanotexist"]);
                return;
            }
            if (!EnabledBallonTP && player.GetParentEntity() is HotAirBalloon)
            {
                SendReply(player, Messages["PlayerIsOnHotAirBalloon"]);
                return;
            }
            if (!EnabledShipTP && player.GetParentEntity() is CargoShip)
            {
                SendReply(player, Messages["PlayerIsOnCargoShip"]);
                return;
            }
            if (CancelTPCold && player.metabolism.temperature.value < 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["coldplayer"]);
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                SendReply(player, Messages["woundedAction"]);
                return;
            }
            if (player.metabolism.bleeding.value > 0 && CancelTPMetabolism)
            {
                SendReply(player, Messages["woundedAction"]);
                return;
            }
            if (CancelTPRadiation && player.metabolism.radiation_poison.value > 10)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["Radiation"]);
                return;
            }
            if (restrictCupboard && player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null && !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpacupboard"]);
                return;
            }
            var ret = Interface.Call("CanTeleport", player) as string;
            if (ret != null)
            {
                SendReply(player, ret);
                return;
            }
            if (FriendsEnabled)
                if (!IsFriends(pendingPlayer.userID, player.userID) && !IsTeamate(player, pendingPlayer.userID) && !IsClanMember(player.userID, pendingPlayer.userID))
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["PlayerNotFriend"]);
                    return;
                }
            var time = GetTeleportTime(pendingPlayer.userID);
            pendings.Remove(tp);
            var lastTp = tpQueue.Find(p => p.Player == pendingPlayer);
            if (lastTp != null)
            {
                tpQueue.Remove(lastTp);
            }
            var Enabled = player.GetParentEntity() is CargoShip || player.GetParentEntity() is HotAirBalloon;
            tpQueue.Add(new TP(pendingPlayer, player.transform.position, time, Enabled, false, player));
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendReply(pendingPlayer, string.Format(Messages["tpqueue"], player.displayName, TimeToString(time)));
            if (args.Length <= 0) SendReply(player, String.Format(Messages["tpasuccess"], pendingPlayer.displayName, TimeToString(time)));
            CuiHelper.DestroyUi(player, PARENT_TELEPORT);
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
                CuiHelper.DestroyUi(player, "Teleport");
                CuiHelper.DestroyUi(target, "Teleport");
                CuiHelper.DestroyUi(target, PARENT_TELEPORT);
                return;
            }
            if (player.IsWounded() && CancelTPWounded)
            {
                SendReply(player, Messages["woundedAction"]);
                return;
            }
            if (player.metabolism.bleeding.value > 0 && CancelTPMetabolism)
            {
                SendReply(player, Messages["woundedAction"]);
                return;
            }
            if (CancelTPRadiation && player.metabolism.radiation_poison.value > 10)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["Radiation"]);
                return;
            }
            if (CancelTPCold && player.metabolism.temperature.value < 0)
            {
                SendReply(player, Messages["coldplayer"]);
                return;
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
                    CuiHelper.DestroyUi(player, "Teleport");
                    CuiHelper.DestroyUi(tpQ.Player, "Teleport");
                    return;
                }
                if (tpQ.Player == player)
                {
                    SendReply(player, Messages["tpc"]);
                    if (tpQ.Player2 != null)
                    {
                        SendReply(tpQ.Player2, string.Format(Messages["tpctarget"], player.displayName));
                        CuiHelper.DestroyUi(tpQ.Player2, "Teleport");
                    }
                    tpQueue.Remove(tpQ);
                    CuiHelper.DestroyUi(player, "Teleport");

                    return;
                }
            }
        }
        void SpectateFinish(BasePlayer player)
        {
            player.Command("camoffset", "0,1,0");
            player.StopSpectating();
            player.SetParent(null);
            player.gameObject.SetLayerRecursive(17);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
            player.SendNetworkUpdateImmediate();
            player.metabolism.Reset();
            player.InvokeRepeating("InventoryUpdate", 1f, 0.1f * UnityEngine.Random.Range(0.99f, 1.01f));
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "StartLoading");
            player.StartSleeping();
            if (lastPositions.ContainsKey(player.userID))
            {
                Vector3 lastPosition = lastPositions[player.userID] + Vector3.up;
                player.MovePosition(lastPosition);
                if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "ForcePositionTo", lastPosition);
                lastPositions.Remove(player.userID);

            }

            if (player.net?.connection != null) player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try
            {
                player.ClearEntityQueue(null);
            }
            catch { }
            player.SendFullSnapshot();

            SendReply(player, "Слежка закончена!");
        }


        private void OnUserConnected(IPlayer player) => ResetSpectate(player);

        private void OnUserDisconnected(IPlayer player) => ResetSpectate(player);

        private void ResetSpectate(IPlayer player)
        {
            player.Command("camoffset 0,1,0");

            if (lastPositions.ContainsKey(ulong.Parse(player.Id)))
            {
                lastPositions.Remove(ulong.Parse(player.Id));
            }
        }

        object OnPlayerSpectateEnd(BasePlayer player, string spectateFilter)
        {
            player.Command("camoffset", "0,1,0");
            return null;
        }

        [ChatCommand("tpl")]
        void cmdChattpGo(BasePlayer player, string command, string[] args)
        {
            if (!EnabledTPLForPlayers && !player.IsAdmin && !PermissionService.HasPermission(player.userID, "DVTeleportation.admin")) return;
            if (args == null || args.Length == 0)
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["tpArgsError"]);
                return;
            }
            switch (args[0])
            {
                default:
                    if (tpsave.Count <= 0)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["homesmissing"]);
                        return;
                    }
                    var nametp = args[0];
                    var tp = tpsave.Find(p => p.Name == nametp);
                    if (tp == null)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["homenotexist"]);
                        return;
                    }
                    var position = tp.pos;
                    var ret = Interface.Call("CanTeleport", player) as string;
                    if (ret != null)
                    {
                        SendReply(player, ret);
                        return;
                    }
                    int seconds;
                    if (cooldownsHOME.TryGetValue(player.userID, out seconds) && seconds > 0)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, string.Format(Messages["tpkd"], TimeToString(seconds)));
                        return;
                    }
                    var lastTp = tpQueue.Find(p => p.Player == player);
                    if (lastTp != null) tpQueue.Remove(lastTp);
                    Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
                    if (TPLAdmin && player.IsAdmin || PermissionService.HasPermission(player.userID, "DVTeleportation.admin")) Teleport(player, position);
                    else
                    {
                        tpQueue.Add(new TP(player, position, TplPedingTime, false, true));
                        SendReply(player, String.Format(Messages["homequeue"], nametp, TimeToString(TplPedingTime)));
                    }
                    return;
                case "add":
                    if (!player.IsAdmin && !PermissionService.HasPermission(player.userID, "DVTeleportation.admin")) return;
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
                    if (!player.IsAdmin && !PermissionService.HasPermission(player.userID, "DVTeleportation.admin")) return;
                    if (args == null || args.Length == 1)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["removetpArgsError"]);
                        return;
                    }
                    nametp = args[1];
                    if (tpsave.Count > 0)
                    {
                        tp = tpsave.Find(p => p.Name == nametp);
                        if (tp == null)
                        {
                            Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                            SendReply(player, Messages["homesmissing"]);
                            return;
                        }
                        Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
                        tpsave.Remove(tp);
                        SendReply(player, Messages["removehomesuccess"], nametp);
                    }
                    return;
                case "list":
                    if (tpsave.Count <= 0)
                    {
                        Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                        SendReply(player, Messages["TPLmissing"]);
                        return;
                    }
                    var tplist = tpsave.Select(x => $"{x.Name} {x.pos}");
                    SendReply(player, Messages["TPLList"], string.Join("\n", tplist.ToArray()));
                    return;
            }
        }
        [ChatCommand("tpspec")]
        void cmdTPSpec(BasePlayer player, string command, string[] args)
        {
            if (!PermissionService.HasPermission(player.userID, "DVTeleportation.admin")) return;
            if (!player.IsSpectating())
            {
                if (args.Length == 0 || args.Length != 1)
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
                        lastPositions[player.userID] = player.transform.position;
                        HeldEntity heldEntity = player.GetActiveItem()?.GetHeldEntity() as HeldEntity;
                        heldEntity?.SetHeld(false);
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
                        player.gameObject.SetLayerRecursive(10);
                        player.CancelInvoke("MetabolismUpdate");
                        player.CancelInvoke("InventoryUpdate");
                        player.ClearEntityQueue();
                        player.SendEntitySnapshot(target);
                        player.gameObject.Identity();
                        player.SetParent(target);
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
                        player.Command("camoffset", "0, 1.3, 0");
                        SendReply(player, $"Вы наблюдаете за игроком {target}! Что бы переключаться между игроками, нажимайте: Пробел\nЧтобы выйти с режима наблюдения, введите: /tpspec");
                        break;
                }
            }
            else SpectateFinish(player);
        }
        [ChatCommand("tp")]
        void cmdTP(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !PermissionService.HasPermission(player.userID, "DVTeleportation.admin")) return;
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
                    float x = float.Parse(args[0].Replace(",", ""));
                    float y = float.Parse(args[1].Replace(",", ""));
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
            PrintWarning("Запущен ручной вайп. Очищаем данные с data/DVTeleportation");
            WipeData();
        }
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

        void Loaded()
        {
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            LoadDefaultConfig();
        }

        void OnServerInitialized()
        {
            LoadData();
            permission.RegisterPermission(TPADMIN, this);
            permission.RegisterPermission(AutoTPAPermission, this);
            timer.Every(1f, DVTeleportationTimerHandle);
        }

        void OnServerSave()
        => SaveData();

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, PARENT_TELEPORT);
                CuiHelper.DestroyUi(player, "Teleport");
            }
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

        object OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null) return null;
            var tp = tpQueue.Find(p => p.Player == player);
            if (tp != null)
            {
                SendReply(tp.Player, Messages["tpwounded"]);
                tpQueue.Remove(tp);
            }
            return null;
        }

        [PluginReference] Plugin Duel;
        bool InDuel(BasePlayer player) => Duel?.Call<bool>("IsPlayerOnActiveDuel", player) ?? false;

        void DVTeleportationTimerHandle()
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
                if (--cooldownsHOME[uid] <= 0) tpkdHomeToRemove.Add(uid);
            }
            tpkdHomeToRemove.ForEach(p => cooldownsHOME.Remove(p));
            for (int i = pendings.Count - 1;
            i >= 0;
            i--)
            {
                var pend = pendings[i];
                if (pend.Player != null && pend.Player.IsConnected && pend.Player.IsWounded())
                {
                    SendReply(pend.Player, Messages["tpwounded"]);

                    if (pend.Player2 != null && pend.Player2.IsConnected)
                    {
                        CuiHelper.DestroyUi(pend.Player2, "Teleport");
                        CuiHelper.DestroyUi(pend.Player2, PARENT_TELEPORT);
                    }
                    if (pend.Player != null && pend.Player.IsConnected)
                    {
                        CuiHelper.DestroyUi(pend.Player, "Teleport");
                        CuiHelper.DestroyUi(pend.Player, PARENT_TELEPORT);
                    }
                    pendings.RemoveAt(i);
                    continue;
                }
                if (--pend.seconds <= 0)
                {
                    pendings.RemoveAt(i);
                    if (pend.Player2 != null && pend.Player2.IsConnected)
                    {
                        SendReply(pend.Player2, Messages["tppendingcanceled"]);
                        CuiHelper.DestroyUi(pend.Player2, "Teleport");
                        CuiHelper.DestroyUi(pend.Player2, PARENT_TELEPORT);
                    }
                    if (pend.Player != null && pend.Player.IsConnected)
                    {
                        SendReply(pend.Player, Messages["tpacanceled"]);
                        CuiHelper.DestroyUi(pend.Player, "Teleport");
                        CuiHelper.DestroyUi(pend.Player, PARENT_TELEPORT);
                    }
                }
                else
                {
                    if (pend.Player != null && pend.Player.IsConnected)
                    {
                        CreateUpdateUI(pend.Player, pend, "PendingTextPlayer2");
                    }
                    if (pend.Player2 != null && pend.Player2.IsConnected)
                    {
                        CreateUpdateUI(pend.Player2, pend, "PendingTextPlayer1");
                    }
                }
            }
            for (int i = tpQueue.Count - 1;
            i >= 0;
            i--)
            {
                var reply = 288;
                if (reply == 0) { }
                var tp = tpQueue[i];
                if (tp.Player != null)
                {

                    if (tp.Player.IsDead())
                    {
                        if (tp.Player2 != null && tp.Player2.IsConnected)
                        {
                            CuiHelper.DestroyUi(tp.Player2, "Teleport");
                            CuiHelper.DestroyUi(tp.Player2, PARENT_TELEPORT);
                        }
                        CuiHelper.DestroyUi(tp.Player, "Teleport");
                        CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        tpQueue.RemoveAt(i);
                        continue;
                    }
                    if (tp.Player.IsConnected && (CancelTPWounded && tp.Player.IsWounded()) || (tp.Player.metabolism.bleeding.value > 0 && CancelTPMetabolism) || (CancelTPRadiation && tp.Player.metabolism.radiation_poison.value > 10))
                    {
                        SendReply(tp.Player, Messages["tpwounded"]);
                        if (tp.Player2 != null && tp.Player2.IsConnected)
                        {
                            SendReply(tp.Player2, Messages["tpWoundedTarget"]);
                            CuiHelper.DestroyUi(tp.Player2, "Teleport");
                            CuiHelper.DestroyUi(tp.Player2, PARENT_TELEPORT);
                        }
                        CuiHelper.DestroyUi(tp.Player, "Teleport");
                        CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        tpQueue.RemoveAt(i);
                        continue;
                    }
                    if (InDuel(tp.Player))
                    {
                        SendReply(tp.Player, Messages["InDuel"]);
                        if (tp.Player2 != null && tp.Player2.IsConnected)
                        {
                            SendReply(tp.Player2, Messages["InDuelTarget"]);
                            CuiHelper.DestroyUi(tp.Player2, "Teleport");
                            CuiHelper.DestroyUi(tp.Player2, PARENT_TELEPORT);
                        }
                        CuiHelper.DestroyUi(tp.Player, "Teleport");
                        CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        tpQueue.RemoveAt(i);
                        continue;
                    }
                    if (restrictTPRCupboard)
                    {
                        var privilege = tp.Player.GetBuildingPrivilege(tp.Player.WorldSpaceBounds());
                        if (privilege != null && !tp.Player.IsBuildingAuthed())
                        {
                            Effect.server.Run(EffectPrefab, tp.Player, 0, Vector3.zero, Vector3.forward);

                            SendReply(tp.Player, Messages["tpcupboard"]);
                            if (tp.Player2 != null && tp.Player2.IsConnected)
                            {
                                SendReply(tp.Player2, Messages["tpcupboardTarget"]);
                                CuiHelper.DestroyUi(tp.Player2, "Teleport");
                                CuiHelper.DestroyUi(tp.Player2, PARENT_TELEPORT);
                            }
                            CuiHelper.DestroyUi(tp.Player, "Teleport");
                            CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                            tpQueue.RemoveAt(i);
                            return;
                        }
                    }
                }

                if (tp.Player2 != null)
                {
                    if (tp.Player2.IsConnected && (tp.Player2.IsWounded() && CancelTPWounded) || (tp.Player2.metabolism.bleeding.value > 0 && CancelTPMetabolism) || (CancelTPRadiation && tp.Player2.radiationLevel > 10))
                    {
                        SendReply(tp.Player2, Messages["tpwounded"]);
                        if (tp.Player != null && tp.Player.IsConnected)
                        {
                            SendReply(tp.Player, Messages["tpWoundedTarget"]);
                            CuiHelper.DestroyUi(tp.Player, "Teleport");
                            CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        }
                        CuiHelper.DestroyUi(tp.Player2, "Teleport");
                        CuiHelper.DestroyUi(tp.Player2, PARENT_TELEPORT);
                        tpQueue.RemoveAt(i);
                        continue;
                    }
                    if (InDuel(tp.Player2))
                    {
                        SendReply(tp.Player2, Messages["InDuel"]);
                        if (tp.Player != null && tp.Player.IsConnected)
                        {
                            SendReply(tp.Player, Messages["InDuelTarget"]);
                            CuiHelper.DestroyUi(tp.Player, "Teleport");
                            CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        }
                        CuiHelper.DestroyUi(tp.Player2, "Teleport");
                        CuiHelper.DestroyUi(tp.Player2, PARENT_TELEPORT);
                        tpQueue.RemoveAt(i);
                        continue;
                    }
                    if (restrictTPRCupboard)
                    {
                        var privilege = tp.Player2.GetBuildingPrivilege(tp.Player2.WorldSpaceBounds());
                        if (privilege != null && !tp.Player2.IsBuildingAuthed())
                        {
                            Effect.server.Run(EffectPrefab, tp.Player2, 0, Vector3.zero, Vector3.forward);
                            if (tp.Player != null && tp.Player.IsConnected)
                            {
                                SendReply(tp.Player, Messages["tpcupboardTarget"]);
                                CuiHelper.DestroyUi(tp.Player, "Teleport");
                                CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                            }

                            SendReply(tp.Player2, Messages["tpcupboard"]);
                            CuiHelper.DestroyUi(tp.Player2, "Teleport");
                            CuiHelper.DestroyUi(tp.Player2, PARENT_TELEPORT);
                            return;
                        }
                    }
                }
                if (--tp.seconds <= 0)
                {
                    tpQueue.RemoveAt(i);
                    var ret = Interface.CallHook("CanTeleport", tp.Player) as string;
                    if (ret != null)
                    {
                        SendReply(tp.Player, ret);
                        CuiHelper.DestroyUi(tp.Player, "Teleport");
                        CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        continue;
                    }
                    if (CheckInsideInFoundation(tp.pos))
                    {
                        SendReply(tp.Player, Messages["InsideInFoundationTP"]);
                        CuiHelper.DestroyUi(tp.Player, "Teleport");
                        CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        continue;
                    }
                    if (tp.Player2 != null)
                    {

                        if (tp.Player.GetMountedVehicle() is ScrapTransportHelicopter || tp.Player.GetMountedVehicle() is MiniCopter || tp.Player2.GetMountedVehicle() is ScrapTransportHelicopter || tp.Player2.GetMountedVehicle() is MiniCopter || tp.Player.GetComponentInParent<ScrapTransportHelicopter>() || tp.Player2.GetComponentInParent<ScrapTransportHelicopter>())
                        {
                            SendReply(tp.Player, Messages["TeleportaCancelHeli"]);
                            SendReply(tp.Player2, Messages["TeleportaCancelHeli"]);
                            CuiHelper.DestroyUi(tp.Player, "Teleport");
                            CuiHelper.DestroyUi(tp.Player2, "Teleport");
                            CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                            continue;
                        }
                        tp.Player.SetParent(tp.Player2.GetParentEntity());
                        if (tp.EnabledShip) tp.pos = tp.Player2.transform.position;
                    }
                    if (tp.Player2 != null && tp.Player != null && tp.Player.IsConnected && tp.Player2.IsConnected)
                    {
                        var seconds = GetKD(tp.Player.userID);
                        cooldownsTP[tp.Player.userID] = seconds;
                        SendReply(tp.Player, string.Format(Messages["tpplayersuccess"], tp.Player2.displayName));
                        CuiHelper.DestroyUi(tp.Player, "Teleport");
                        CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        CuiHelper.DestroyUi(tp.Player2, "Teleport");
                    }
                    else if (tp.Player != null && tp.Player.IsConnected)
                    {
                        tp.Player.SetParent(null);
                        if (tp.TPL)
                        {
                            var seconds = TPLCooldown;
                            cooldownsHOME[tp.Player.userID] = seconds;
                            SendReply(tp.Player, Messages["tplsuccess"]);
                            CuiHelper.DestroyUi(tp.Player, "Teleport");
                            CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        }
                        else
                        {
                            var seconds = GetKDHome(tp.Player.userID);
                            cooldownsHOME[tp.Player.userID] = seconds;
                            SendReply(tp.Player, Messages["tphomesuccess"]);
                            CuiHelper.DestroyUi(tp.Player, "Teleport");
                            CuiHelper.DestroyUi(tp.Player, PARENT_TELEPORT);
                        }
                    }
                    if (tp.Player.IsDead())
                        continue;
                    Teleport(tp.Player, tp.pos);
                    NextTick(() => Interface.CallHook("OnPlayerTeleported", tp.Player));
                }
                else
                {
                    CreateUpdateUI(tp.Player, tp, "TimerTextPlayer1");
                    if (tp.Player2 != null && tp.Player2.IsConnected)
                    {
                        CreateUpdateUI(tp.Player2, tp, "TimerTextPlayer2");
                    }
                }
            }
        }

        void SetTpSave(BasePlayer player, string name)
        {
            var position = player.transform.position;
            if (tpsave.Count > 0)
            {
                var tp = tpsave.Find(p => p.Name == name);
                if (tp != null)
                {
                    Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                    SendReply(player, Messages["homeexist"]);
                    return;
                }
            }
            tpsave.Add(new TPList()
            {
                Name = name,
                pos = position
            }
            );

            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward);
            SendReply(player, Messages["homesucces"], name);
            timer.Once(10f, () => sethomeBlock.Remove(player.userID));
        }

        private readonly int blockLayer = LayerMask.GetMask("Construction");

        private bool ValidBlock(BaseEntity entity, Vector3 position)
        {
            Vector3 center = entity.CenterPoint();

            List<BaseEntity> ents = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(center, 1.5f, ents);
            foreach (BaseEntity wall in ents)
            {
                if (wall.name.Contains("external.high"))
                    return false;
            }
            if (entity.PrefabName.Contains("triangle.prefab"))
            {
                if (Math.Abs(center.x - position.x) < 0.45f && Math.Abs(center.z - position.z) < 0.45f)
                    return true;
            }
            else if (entity.PrefabName.Contains("foundation.prefab") || entity.PrefabName.Contains("floor.prefab"))
            {
                if (Math.Abs(center.x - position.x) < 0.7f && Math.Abs(center.z - position.z) < 0.7f)
                    return true;
            }
            return false;
        }

        private List<BuildingBlock> GetFoundation(Vector3 position)
        {
            RaycastHit hitinfo;
            var entities = new List<BuildingBlock>();

            if (Physics.Raycast(position, Vector3.down, out hitinfo, 2.5f, blockLayer) && hitinfo.GetEntity().IsValid())
            {
                var entity = hitinfo.GetEntity();
                if (entity.PrefabName.Contains("foundation") || position.y < entity.WorldSpaceBounds().ToBounds().max.y)
                {
                    if (ValidBlock(entity, position))
                    {
                        entities.Add(entity as BuildingBlock);
                    }
                }
            }
            return entities;
        }


        void SetHome(BasePlayer player, string name, Vector3 pos)
        {
            var uid = player.userID;
            if (player.GetBuildingPrivilege(player.WorldSpaceBounds()) != null && !player.GetBuildingPrivilege(player.WorldSpaceBounds()).authorizedPlayers.Select(p => p.userid).Contains(player.userID))
            {
                Effect.server.Run(EffectPrefab, player, 0, Vector3.zero, Vector3.forward);
                SendReply(player, Messages["sethomecupboard"]);
                return;
            }
            Dictionary<string, Vector3> playerHomes;
            if (!homes.TryGetValue(uid, out playerHomes)) playerHomes = (homes[uid] = new Dictionary<string, Vector3>());
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
            List<BuildingBlock> foundation = GetFoundation(pos);
            foundation.RemoveAll(x => !x.IsValid() || x.IsDestroyed);

            if (foundation.Count <= 0)
            {
                SendReply(player, Messages["foundationmissing"]);
                return;
            }

            if (CheckInsideInFoundation(player.transform.position))
            {
                SendReply(player, Messages["InsideInFoundation"]);
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
            timer.Once(10f, () => sethomeBlock.Remove(player.userID));
        }


        private bool CheckInsideInFoundation(Vector3 position)
        {
            foreach (var hit in Physics.RaycastAll(position, Vector3.up, 2f, LayerMask.GetMask("Terrain", "World", "Construction", "Deployed")))
            {
                if (!foundationEx && hit.GetCollider().name.Contains("foundation")) return true;
                else
                if (hit.GetCollider().name.Contains("foundation")) return true;
            }
            foreach (var hit in Physics.RaycastAll(position + Vector3.up + Vector3.up + Vector3.up + Vector3.up, Vector3.down, 2f, LayerMask.GetMask("Terrain", "World", "Construction", "Deployed")))
            {
                if (hit.GetCollider().name.Contains("foundation")) return true;
            }
            return false;
        }
        int GetKDHome(ulong uid)
        {
            int min = tpkdhomeDefault;
            foreach (var privilege in tpkdhomePerms) if (PermissionService.HasPermission(uid, privilege.Key)) min = Mathf.Min(min, privilege.Value);
            return min;
        }
        int GetKD(ulong uid)
        {
            int min = tpkdDefault;
            foreach (var privilege in tpkdPerms) if (PermissionService.HasPermission(uid, privilege.Key)) min = Mathf.Min(min, privilege.Value);
            return min;
        }
        int GetHomeLimit(ulong uid)
        {
            int max = homelimitDefault;
            foreach (var privilege in homelimitPerms) if (PermissionService.HasPermission(uid, privilege.Key)) max = Mathf.Max(max, privilege.Value);
            return max;
        }
        int GetTeleportTime(ulong uid)
        {
            int min = teleportSecsDefault;
            foreach (var privilege in teleportSecsPerms) if (PermissionService.HasPermission(uid, privilege.Key)) min = Mathf.Min(min, privilege.Value);
            return min;
        }
        BaseEntity GetBuldings(Vector3 pos)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Ray(pos, Vector3.down), out hit, 0.2f))
            {
                var entity = hit.GetEntity();
                if (entity != null)
                {
                    if (entity.GetComponent<BuildingBlock>() == null) return null;
                    return entity;
                }
                else return null;
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
            SleepingBag sleepingBag = GameManager.server.CreateEntity("assets/prefabs/deployable/sleeping bag/sleepingbag_leather_deployed.prefab", pos, Quaternion.identity) as SleepingBag;
            if (sleepingBag == null) return;
            sleepingBag.skinID = 2597355840;
            sleepingBag.deployerUserID = player.userID;
            sleepingBag.niceName = name;
            sleepingBag.OwnerID = player.userID;
            sleepingBag.Spawn();
            sleepingBag.SendNetworkUpdate();
        }
        Dictionary<string, Vector3> GetHomes(ulong uid)
        {
            Dictionary<string, Vector3> positions;
            if (!homes.TryGetValue(uid, out positions)) return null;
            return positions.ToDictionary(p => p.Key, p => p.Value);
        }

        public void Teleport(BasePlayer player, BasePlayer target) => Teleport(player, target.transform.position);
        public void Teleport(BasePlayer player, float x, float y, float z) => Teleport(player, new Vector3(x, y, z));
        public void Teleport(BasePlayer player, Vector3 position)
        {
            player.RemoveFromTriggers();
            if (player.GetParentEntity() != null)
                player.SetParent(null);
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
            BaseMountable mount = player.GetMounted();
            if (mount != null) mount.DismountPlayer(player);
            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "StartLoading");
            player.StartSleeping();
            player.Teleport(position);
            player.SendFullSnapshot();
            if (player.net?.connection != null) player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.SendNetworkUpdateImmediate();
            player.UpdateNetworkGroup();

        }
        DynamicConfigFile homesFile = Interface.Oxide.DataFileSystem.GetFile("DVTeleportation/Homes");
        DynamicConfigFile tpsaveFile = Interface.Oxide.DataFileSystem.GetFile("DVTeleportation/AdminTpSave");
        public Dictionary<ulong, AutoTPASettings> AutoTPA = new Dictionary<ulong, AutoTPASettings>();

        public class AutoTPASettings
        {
            public bool Enabled;
            public Dictionary<string, ulong> PlayersList = new Dictionary<string, ulong>();
        }


        void LoadData()
        {
            try
            {
                tpsave = tpsaveFile.ReadObject<List<TPList>>();
                if (tpsave == null)
                {
                    PrintError("File AdminTpSave is null! Create new data files");
                    tpsave = new List<TPList>();
                }
                homes = homesFile.ReadObject<Dictionary<ulong, Dictionary<string, Vector3>>>();
                if (homes == null)
                {
                    PrintError("File Homes is null! Create new data files");
                    homes = new Dictionary<ulong, Dictionary<string, Vector3>>();
                }
                AutoTPA = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, AutoTPASettings>>($"DVTeleportation/AutoTPA");

            }
            catch
            {
                tpsave = new List<TPList>();
                homes = new Dictionary<ulong, Dictionary<string, Vector3>>();
                AutoTPA = new Dictionary<ulong, AutoTPASettings>();
            }
        }
        void SaveData()
        {
            if (tpsave != null) tpsaveFile.WriteObject(tpsave);
            if (homes != null) homesFile.WriteObject(homes);
            if (AutoTPA != null) Interface.Oxide.DataFileSystem.WriteObject($"DVTeleportation/AutoTPA", AutoTPA);

        }
        Dictionary<string, string> Messages = new Dictionary<string, string>() {
                {
                "foundationmissing", "Фундамент не найден!"
            },


                {
                "buildingBrockmissing", "Создание местоположение разрешено только на строительных блоках"
            }
            ,
            {
                "InDuel", "Вы на Дуэли. Телепорт запрещен!"
            },
            {
                "InDuelTarget", "Игрок на Дуэли. Телепорт запрещен!"
            }
            ,
            {
                "foundationmissingR", "Фундамент не найден, местоположение было удалено!"
            }
            ,
            {
                "buildingBrockmissingR", "Строительный блок не найден, местоположение было удалено!"
            }
            ,
            {
                "playerisyou", "Нельзя отправлять телепорт самому себе!"
            }
            , {
                "maxhomes", "У вас максимальное кол-во местоположений!"
            }
            , {
                "homeexist", "Такое местоположение уже существует!"
            }
            , {
                "homesucces", "Местоположение {0} успешно установлено!"
            }
            , {
                "sethomeArgsError", "Для установки местоположения используйте /sethome ИМЯ"
            }
            , {
                "settpArgsError", "Для установки местоположения используйте /tpl add ИМЯ"
            }
            , {
                "homeArgsError", "Для телепортации на местоположение используйте /home ИМЯ"
            }
            , {
                "tpArgsError", "Для телепортации на местоположение используйте /tpl ИМЯ"
            }
            , {
                "tpError", "Запрещено! Вы в очереди на телепортацию"
            }
            , {
                "homenotexist", "Местоположение с таким названием не найдено!"
            }
            , {
                "homequeue", "Телепортация на {0} будет через {1}"
            }
            , {
                "tpwounded", "Вы получили ранение! Телепортация отменена!"
            }
            , {
                "tphomesuccess", "Вы телепортированы домой!"
            }
            , {
                "tplsuccess", "Вы успешно телепортированы!"
            }
            , {
                "tptpsuccess", "Вы телепортированы на указаное место!"
            }
            , {
                "homesmissing", "У вас нет доступных местоположений!"
            }
            , {
                "TPLmissing", "Для вас нет доступных местоположений!"
            }
            , {
                "TPLList", "Доступные точки местоположения:\n{0}"
            }
            , {
                "removehomeArgsError", "Для удаления местоположения используйте /removehome ИМЯ"
            }
            , {
                "removetpArgsError", "Для удаления местоположения используйте /tpl remove ИМЯ"
            }
            , {
                "removehomesuccess", "Местоположение {0} успешно удалено"
            }
            , {
                "sleepingbagmissing", "Спальный мешок не найден, местоположение удалено!"
            },
            {
                "sleepingbugbuildblock", "Вы не авторизованы в билде точки телепортации, местоположение удалено!"
            }
            , {
                "tprArgsError", "Для отправки запроса на телепортация используйте /tpr НИК"
            }
            , {
                "playermissing", "Игрок не найден"
            }
            , {
                "PlayerNotFriend", "Игрок не являеться Вашим другом! Телепорт запрещен!"
            }
            , {
                "tpspecError", "Не правильно введена команда. Используйте: /tpspec НИК"
            }
            , {
                "playermissingOff", "Игрок не в сети"
            }
            , {
                "playermissingOrDeath", "Игрок не найден, или он мёртв"
            }
            , {
                "playerItsYou", "Нельзя следить за самым собой"
            }
            , {
                " playerItsSpec", "Игрок уже за кем то наблюдает"
            }
            , {
                "tprrequestsuccess", "Запрос {0} успешно отправлен"
            }
            , {
                "tprpending", "{0} отправил вам запрос на телепортацию\nЧтобы принять используйте /tpa\nЧтобы отказаться используйте /tpc"
            }
            , {
                "tpanotexist", "У вас нет активных запросов на телепортацию!"
            }
            , {
                "tpqueue", "{0} принял ваш запрос на телепортацию\nВы будете телепортированы через {1}"
            }
            , {
                "tpc", "Телепортация успешно отменена!"
            }
            , {
                "tpctarget", "{0} отменил телепортацию!"
            }
            , {
                "tpplayersuccess", "Вы успешно телепортировались к {0}"
            }
            , {
                "tpasuccess", "Вы приняли запрос телепортации от {0}\nОн будет телепортирован через {1}"
            }
            , {
                "tppendingcanceled", "Запрос телепортации отменён"
            }
            , {
                "tpcupboard", "Телепортация в зоне действия чужого шкафа запрещена!"
            }, {
                "tpcupboardTarget", "Вы или игрок находитесь в зоне действия чужого шкафа!"
            }
            , {
                "tphomecupboard", "Телепортация домой в зоне действия чужого шкафа запрещена!"
            }
            , {
                "tpacupboard", "Принятие телепортации в зоне действия чужого шкафа запрещена!"
            }
            , {
                "sethomecupboard", "Установка местоположения в зоне действия чужого шкафа запрещена!"
            }
            , {
                "tpacanceled", "Вы не ответили на запрос."
            }
            , {
                "tpkd", "Телепортация на перезарядке!\nОсталось {0}"
            }
            , {
                "tpWoundedTarget", "Игрок ранен. Телепортация отменена!"
            }
            , {
                "woundedAction", "Вы ранены!"
            }
            , {
                "coldplayer", "Вам холодно!"
            }
            , {
                "Radiation", "Вы облучены радиацией!"
            }
            , {
                "sethomeBlock", "Нельзя использовать /sethome слишком часто, попробуйте позже!"
            }
            , {
                "foundationowner", "Нельзя использовать /sethome не на своих строениях!"
            }
            , {
                "foundationownerFC", "Создатель обьекта не являеться вашим соклановцем или другом, /sethome запрещен"
            }
            , {
                "homeslist", "Доступное количество местоположений: {0}\n{1}"
            }
            , {
                "tplist", "Ваши сохраненные метоположения:\n{0}"
            }
            , {
                "PlayerIsOnCargoShip", "Вы не можете телепортироваться на грузовом корабле."
            }
            , {
                "PlayerIsOnHotAirBalloon", "Вы не можете телепортироваться на воздушном шаре."
            }
            , {
                "InsideInFoundation", "Вы не можете устанавливать местоположение находясь в фундаменте"
            }
            , {
                "InsideInFoundationTP", "Телепортация запрещена, местоположение находится в фундаменте"
            }
            ,{
                "TPAPerm", "У Вас нету права использовать эту команду"
            },
            {
                "TPAEnabled", "Вы успешно <color=#FDAE37>включили</color> автопринятие запроса на телепорт\n{0}"
            },
             {
                "TPADisable", "Вы успешно <color=#FDAE37>отключили</color> автопринятие запроса на телепорт"
            },
            {
                "TPAEnabledInfo", "Добавление нового игрока <color=#FDAE37>/atp add Name/SteamID</color>\nУдаление игрока <color=#FDAE37>/atp remove Name</color>\nСписок игроков <color=#FDAE37>/apt list</color>"
            },
            {
                "TPAEnabledList", "Список игроков для каких у Вас включен автоматический приём телепорта:\n{0}"
            },
            {
                "TPAEListNotFound", "Вы пока еще не добавили не одного игрока в список, используйте <color=#FDAE37>/atp add Name/SteamID</color>"
            },
            {
                "TPAEAddError", "Вы не указали игрока, используйте <color=#FDAE37>/atp add Name/SteamID</color>"
            },{
                "TPARemoveError", "Вы не указали игрока, используйте <color=#FDAE37>/atp remove Name</color>"
            },
            {
                "TPARemoveNotFound", "Игрока <color=#FDAE37>{0}</color> нету в списке, используйте <color=#FDAE37>/atp remove Name</color>"
            },
            {
                "TPAEAddPlayerNotFound", "Игрок не найден! Попробуйте уточнить <color=#FDAE37>имя</color>"
            },
            {
                "TPAEAddSuccess", "Игрок <color=#FDAE37>{0}</color> успешно добавлен в список"
            },
            {
                "TPAEAddContains", "Игрок <color=#FDAE37>{0}</color> уже добавлен в список"
            },
            {
                "TPAERemoveSuccess", "Игрок <color=#FDAE37>{0}</color> успешно удален со списка"
            },
            {
                "TPAEAddPlayers", "Найдено <color=#FDAE37>несколько</color> игроков с похожим ником:\n{0}"
            },
            {
                "TPASuccess", "Вы <color=#FDAE37>автоматически</color> приняли запрос на телепортацию так как у вас игрок в списке разрешенных."
            },
            {
                "TeleportaCancelHeli", "Телепорт отменен, кто то из игроков использует транспортное средство."
            },
            {
            "TimerTextPlayer1", "ТЕЛЕПОРТ АКТИВИРОВАН: ОСТАЛОСЬ ЖДАТЬ - <b>{0}</b>"
            },
            {
                "TimerTextPlayer2", "ТЕЛЕПОРТ К ВАМ: ОЖИДАНИЕ ОТВЕТА - <b>{0}</b>"
            },
            {
                "PendingTextPlayer1", "ТЕЛЕПОРТ ОТПРАВЛЕН: ОЖИДАНИЕ ОТВЕТА - <b>{0}</b>"
            },
            {
                "PendingTextPlayer2", "ОСТАЛОСЬ ЖДАТЬ - <b>{0}</b>"
            }
        };
        #region TPAcceptUI
        void CreateUpdateUI(BasePlayer player, DVTeleportation.TP teleport, string langMessage = "PendingTextPlayer1")
        {
            CuiHelper.DestroyUi(player, "Teleport");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3447913 0.112037", AnchorMax = "0.640625 0.1393229", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.15" }
            }, "Hud", "Teleport");
            container.Add(new CuiElement
            {
                Parent = "Teleport",
                Name = "Teleport.Update",
                Components =
                {
                    new CuiImageComponent { Color = "0.84 0.25 0.78 0.95" },
                    new CuiRectTransformComponent {AnchorMin = $"0 0", AnchorMax = $"{(float) (teleport.totalSeconds - (teleport.totalSeconds - teleport.seconds)) / teleport.totalSeconds} 1", OffsetMin = "0 0", OffsetMax = "0 0"},
                }
            });
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = string.Format(Messages[langMessage], GetFormatTime(TimeSpan.FromSeconds(teleport.totalSeconds - (teleport.totalSeconds - teleport.seconds)))), Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.9", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, "Teleport", "Teleport" + ".Info");
            CuiHelper.AddUi(player, container);
        }
        private string GetFormatTime(TimeSpan timespan)
        {
            return string.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, System.Math.Floor(timespan.TotalHours));
        }

        public static string PARENT_TELEPORT = "PARENT_DVTeleportation_UI";
        void DVTeleportationAcceptUI(BasePlayer target, string TprName)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(target, PARENT_TELEPORT);

            container.Add(new CuiPanel
            {
                // RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "-400 16", OffsetMax = "-210 98" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "130 18", OffsetMax = "350 98" },
                Image = { Color = HexToRustFormat("#000000"), Material = "assets/content/ui/uibackgroundblur.mat" } //  //"#d02424B1" "#544E44FF"
            }, "Overlay", PARENT_TELEPORT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.01 0.02", AnchorMax = "0.99 0.75" },
                Image = { Color = HexToRustFormat("#F65050"), Material = "assets/content/ui/uibackgroundblur.mat" } // , #bdb4ab #4b98d0
            }, PARENT_TELEPORT, "TWO_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.009999931 0.7777777", AnchorMax = "1 1" },
                Text = { Text = $"<size=12><b>Запрос от {TprName}</b></size>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
            }, PARENT_TELEPORT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5814307", AnchorMax = "1 1" },
                Text = { Text = $"<size=20><b>ПРИНЯТЬ ТЕЛЕПОРТ?</b></size>", Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF") }
            }, "TWO_PANEL");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.03079109 0.09436831", AnchorMax = "0.4158611 0.4517729" },
                Button = { Close = PARENT_TELEPORT, Command = "chat.say /tpa", Color = HexToRustFormat("#8b3131FF"), Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { Text = "<color=#FFFFFF><b>☑ ПРИНЯТЬ</b></color>", Align = TextAnchor.MiddleCenter }
            }, "TWO_PANEL");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5857509 0.09436831", AnchorMax = "0.9708213 0.4517729" },
                Button = { Close = PARENT_TELEPORT, Command = "chat.say /tpc", Color = HexToRustFormat("#d02424FF"), Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { Text = "<color=#FFFFFF><b>☒ ОТМЕНА</b></color>", Align = TextAnchor.MiddleCenter } //#FF1400FF //#FF2E00FF
            }, "TWO_PANEL");

            CuiHelper.AddUi(target, container);
        }
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            UnityEngine.Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        #endregion
    }
}