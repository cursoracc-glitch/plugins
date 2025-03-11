using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using ProtoBuf;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Friends", "ServerRust.ru", "1.1.12", ResourceId = 686)]
    public class Friends : CovalencePlugin
    {
        #region Configuration and Stored Data
        private ConfigData configData;
        public Timer mytimer;
        private Dictionary<ulong, PlayerData> FriendsData;
        private readonly Dictionary<ulong, HashSet<ulong>> ReverseData = new Dictionary<ulong, HashSet<ulong>>();
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);
        private Dictionary<string, double> userCooldowns = new Dictionary<string, double>();

        private class ConfigData
        {
            [JsonProperty("Максимально друзей")]
            public int MaxFriends { get; set; }
            [JsonProperty("Задержка на добавление в друзья (КД)")]
            public double FriendCooldown { get; set; }
            [JsonProperty("Включить настройку авторизации друзей в замках для игрока")]
            public bool ShareCodeLocks { get; set; }
            [JsonProperty("Включить настройку авторизации друзей в турреляъ для игрока")]
            public bool ShareAutoTurrets { get; set; }
            [JsonProperty("Время кэширования")]
            public int CacheTime { get; set; }
        }

        private class PlayerData
        {
            public bool TurrentAuthorization = false;
            public bool AttackFriend = false;
            public bool CodeAuthorization = false;
            public string Name { get; set; } = string.Empty;
            public HashSet<ulong> Friends { get; set; } = new HashSet<ulong>();
            public Dictionary<ulong, int> Cached { get; set; } = new Dictionary<ulong, int>();
            public bool IsCached(ulong userId)
            {
                int time;
                if (!Cached.TryGetValue(userId, out time)) return false;
                if (time >= (int)DateTime.UtcNow.Subtract(Epoch).TotalSeconds) return true;
                Cached.Remove(userId);
                return false;
            }
        }

        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                MaxFriends = 5,
                FriendCooldown = 30.0,
                ShareCodeLocks = false,
                ShareAutoTurrets = false,
                CacheTime = 0 //60 * 60 * 24
            };
            Config.WriteObject(config, true);
        }



        #endregion

        #region Localization
        private Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
                {"AlreadyOnList", "{0} уже находится в списке Ваших друзей."},
                {"Disconnected", "Игрока нет в сети"},
                {"CantAddSelf", "Вы не можете добавить себя в друзья."},
                {"FriendAdded", "{0} стал Вашим другом."},
                {"FriendRemoved", "{0} удален из Вашего списка друзей."},
                {"FriendlistFull", "Список Ваших друзей переполнен"},
                {"List", "Список Ваших друзей: {0}:\n{1}"},
                {"MultiplePlayers", "Было найдено несколько игроков, пожалуйста, уточните: {0}"},
                {"NoFriends", "У Вас нет друзей =(."},
                {"NotOnFriendlist", "{0} не найден в вашем списке друзей."},
                {"PlayerNotFound", "Игрок '{0}' не найден."},
                {"Syntax", "<color=blue><size=17>Friends</size><size=15> by Rustplugin.ru</size></color>\n\n<color=blue> Команды:</color>\n<color=blue>/friend add (+)</color> - что бы добавить нового друга\n<color=blue>/friend remove(-) </color>- что бы удалить друга\n<color=blue>/friend list</color>- Список ваших друзей\n<color=blue>/friend accept </color>- Принять заявку в друзья.\n<color=blue>/friend ff </color>- Включение/Отключение урона по вашим друзьям."},
                {"AlreadyPending", "На ваш предыдущий запрос дружбы игрок еще не ответил, ожидайте его ответа!"},
                {"PendingBusy", "У игрока уже есть запрос от другого игрока, ожидайте пока игрок игрок не ответит другому"},
                {"PendingSuccessSend", "Запрос успешно отправлен, ожидайте пока он приймет его."},
                {"PendingNotFound", "К вам нет запросов в друзья"},
                {"Pending", "{0} отправил вам запрос в друзья, чтобы принять введите в чат <color=blue>/friend accept</color>"},
                {"Cooldown", "Извините, но нельзя так часто использовать команду <color=blue>/friend {1}</color>, подождите {0:00}"},
                {"Codelock", "Вы <color={1}>{0}</color> автоматическую авторизацию друзей в Ваших дверях"},
                {"Turrets", "Вы <color={1}>{0}</color> автоматическую авторизацию друзей в Ваших туррелях"},
                {"CodeLocks", "\n<color=blue>/friend codelock</color> - Включить/Отключить авторизацию ваших друзей в ваших дверях" },
                {"Turret", "\n<color=blue>/friend turret</color> - Включить/Отключить авторизацию ваших друзей в ваших туррелях" },
                {"Attack", "Внимание {0} Ваш друг,вы неможете его ранить. Включение/Отключение урона по друзьям /friend ff" },
                {"Attacks", "Вы <color={1}>{0}</color> урон по Вашим друзьям"},
        };

        #endregion

        #region Initialization
        private void OnServerInitialized()
        {
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            configData = Config.ReadObject<ConfigData>();
            try
            {
                FriendsData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>(nameof(Friends));
            }
            catch
            {
                FriendsData = new Dictionary<ulong, PlayerData>();
            }

            foreach (var data in FriendsData)
                foreach (var friend in data.Value.Friends)
                    AddFriendReverse(data.Key, friend);
        }

        void OnServerSave()
        {
            SaveFriends();
        }

        void Unload()
        {
            SaveFriends();
        }
        #endregion

        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity targ)
        {
            if (!configData.ShareAutoTurrets || !(targ is BasePlayer) || turret.OwnerID <= 0) return null;
            var player = (BasePlayer)targ;
            if (turret.IsAuthed(player) || !HasFriend(turret.OwnerID, player.userID)) return null;
            if (FriendsData.ContainsKey(turret.OwnerID))
                if (FriendsData[turret.OwnerID].TurrentAuthorization)
                {
                    turret.authorizedPlayers.Add(new PlayerNameID
                    {
                        userid = player.userID,
                        username = player.displayName
                    });
                    turret.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                    return false;
                }
            return null;
        }

        object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
        {
            if (!configData.ShareCodeLocks || !(@lock is CodeLock) || @lock.GetParentEntity().OwnerID <= 0) return null;
            if (@lock.GetParentEntity().OwnerID == player.userID) return null;
            if (HasFriend(@lock.GetParentEntity().OwnerID, player.userID))
            {
                if (FriendsData.ContainsKey(@lock.GetParentEntity().OwnerID))
                    if (FriendsData[@lock.GetParentEntity().OwnerID].CodeAuthorization) return true;
            }
            return null;
        }

        public Dictionary<BasePlayer, int> CooldownList = new Dictionary<BasePlayer, int>();

        void OnEntityTakeDamage(BaseCombatEntity vic, HitInfo info)
        {
            try
            {
                if (vic != null && vic is BasePlayer && info?.Initiator != null && info.Initiator is BasePlayer && vic != info.Initiator)
                {
                    BasePlayer vitim = vic as BasePlayer;
                    if (vitim == null) return;
                    BasePlayer iniciator = info.Initiator as BasePlayer;
                    if (iniciator == null) return;
                    if (HasFriend(iniciator.userID, vitim.userID))
                    {
                        if (!FriendsData[iniciator.userID].AttackFriend)
                        {
                            info.damageTypes?.ScaleAll(0f);
                            if (CooldownList.ContainsKey(iniciator) && CooldownList[iniciator] < GrabCurrentTime())
                            {
                                CooldownList[iniciator] = (int)GrabCurrentTime() + 30;
                                iniciator.ChatMessage(string.Format(Messages["Attack"], vitim.displayName));
                            }
                            if (!CooldownList.ContainsKey(iniciator))
                            {
                                CooldownList[iniciator] = (int)GrabCurrentTime() + 30;
                                iniciator.ChatMessage(string.Format(Messages["Attack"], vitim.displayName));
                            }
                        }
                    }
                }
            }
            catch (NullReferenceException)
            {
            }
        }

        private void SaveFriends()
        {
            if (FriendsData != null ) Interface.Oxide.DataFileSystem.WriteObject("Friends", FriendsData);
        }

        #region Add/Remove Friends

        private bool AddFriend(ulong playerId, ulong friendId)
        {
            var playerData = GetPlayerData(playerId);
            if (playerData.Friends.Count >= configData.MaxFriends || !playerData.Friends.Add(friendId)) return false;
            var playerData2 = GetPlayerData(friendId);
            if (playerData2.Friends.Count >= configData.MaxFriends || !playerData2.Friends.Add(playerId)) return false;
            AddFriendReverse(playerId, friendId);
            AddFriendReverse(friendId, playerId);
            Interface.Oxide.CallHook("OnFriendAdded", playerId.ToString(), friendId.ToString());
            return true;
        }

        private bool AddFriendS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return AddFriend(playerId, friendId);
        }

        private bool RemoveFriend(ulong playerId, ulong friendId)
        {
            var playerData = GetPlayerData(playerId);
            if (!playerData.Friends.Remove(friendId)) return false;
            if (!GetPlayerData(friendId).Friends.Remove(playerId)) return false;
            HashSet<ulong> friends;
            var reply = 596;
            if (reply == 0) { };
            if (ReverseData.TryGetValue(friendId, out friends))
                friends.Remove(playerId);
            if (ReverseData.TryGetValue(playerId, out friends))
                friends.Remove(friendId);
            if (configData.CacheTime > 0)
                playerData.Cached[friendId] = (int)DateTime.UtcNow.Subtract(Epoch).TotalSeconds + configData.CacheTime;

            if (configData.ShareAutoTurrets)
            {
                var turrets = UnityEngine.Object.FindObjectsOfType<AutoTurret>();
                foreach (var turret in turrets)
                {
                    if (turret.OwnerID != playerId) continue;
                    turret.authorizedPlayers.RemoveAll(a => a.userid == friendId);
                }
            }
            if (configData.ShareCodeLocks)
            {
                var codeLocks = UnityEngine.Object.FindObjectsOfType<CodeLock>();
                foreach (var codeLock in codeLocks)
                {
                    var entity = codeLock.GetParentEntity();
                    if (entity == null || entity.OwnerID != playerId) continue;
                    var whitelistPlayers = (List<ulong>)codeLock.whitelistPlayers;
                    whitelistPlayers.RemoveAll(a => a == friendId);
                }
            }
            Interface.Oxide.CallHook("OnFriendRemoved", playerId.ToString(), friendId.ToString());
            return true;
        }

        private bool RemoveFriendS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return RemoveFriend(playerId, friendId);
        }

        #endregion

        #region Friend Checks

        private bool HasFriend(ulong playerId, ulong friendId) => GetPlayerData(playerId).Friends.Contains(friendId);

        private bool HasFriendS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return HasFriend(playerId, friendId);
        }

        private bool HadFriend(ulong playerId, ulong friendId)
        {
            var playerData = GetPlayerData(playerId);
            return playerData.Friends.Contains(friendId) || playerData.IsCached(friendId);
        }

        private bool HadFriendS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return HadFriend(playerId, friendId);
        }

        private bool AreFriends(ulong playerId, ulong friendId)
        {
            return GetPlayerData(playerId).Friends.Contains(friendId) && GetPlayerData(friendId).Friends.Contains(playerId);
        }

        private bool AreFriendsS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return AreFriends(playerId, friendId);
        }

        private bool WereFriends(ulong playerId, ulong friendId)
        {
            var playerData = GetPlayerData(playerId);
            var friendData = GetPlayerData(friendId);
            return (playerData.Friends.Contains(friendId) || playerData.IsCached(friendId)) && (friendData.Friends.Contains(playerId) || friendData.IsCached(playerId));
        }

        private bool WereFriendsS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return WereFriends(playerId, friendId);
        }

        private bool IsFriend(ulong playerId, ulong friendId)
        {
            return GetPlayerData(friendId).Friends.Contains(playerId);
        }

        private bool IsFriendS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return IsFriend(playerId, friendId);
        }

        private bool WasFriend(ulong playerId, ulong friendId)
        {
            var playerData = GetPlayerData(friendId);
            return playerData.Friends.Contains(playerId) || playerData.IsCached(playerId);
        }

        private bool WasFriendS(string playerS, string friendS)
        {
            if (string.IsNullOrEmpty(playerS) || string.IsNullOrEmpty(friendS)) return false;
            var playerId = Convert.ToUInt64(playerS);
            var friendId = Convert.ToUInt64(friendS);
            return WasFriend(playerId, friendId);
        }

        #endregion

        #region Friend Lists

        private ulong[] GetFriends(ulong playerId) => GetPlayerData(playerId).Friends.ToArray();

        private string[] GetFriendsS(string playerS)
        {
            var playerId = Convert.ToUInt64(playerS);
            return GetPlayerData(playerId).Friends.ToList().ConvertAll(f => f.ToString()).ToArray();
        }

        private string[] GetFriendList(ulong playerId)
        {
            var playerData = GetPlayerData(playerId);
            var players = new List<string>();
            foreach (var friend in playerData.Friends)
                players.Add(GetPlayerData(friend).Name);
            return players.ToArray();
        }

        private string[] GetFriendListS(string playerS) => GetFriendList(Convert.ToUInt64(playerS));

        private ulong[] IsFriendOf(ulong playerId)
        {
            HashSet<ulong> friends;
            return ReverseData.TryGetValue(playerId, out friends) ? friends.ToArray() : new ulong[0];
        }

        private string[] IsFriendOfS(string playerS)
        {
            var playerId = Convert.ToUInt64(playerS);
            var friends = IsFriendOf(playerId);
            return friends.ToList().ConvertAll(f => f.ToString()).ToArray();
        }

        #endregion

        private PlayerData GetPlayerData(ulong playerId)
        {
            var player = players.FindPlayerById(playerId.ToString());
            PlayerData playerData;
            if (!FriendsData.TryGetValue(playerId, out playerData))
                FriendsData[playerId] = playerData = new PlayerData();
            if (player != null) playerData.Name = player.Name;
            return playerData;
        }

        private double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
        }

        #region Commands
        private Dictionary<ulong, ulong> pendings = new Dictionary<ulong, ulong>();

        [Command("friend")]
        private void FriendCommand(IPlayer player, string command, string[] args)
        {
            if (player.Id == "server_console")
            {
                player.Reply($"Command '{command}' can only be used by players", command);
                return;
            }

            if (args == null || args.Length <= 0 || args.Length == 1 && args[0].ToLower() != "accept" && args[0].ToLower() != "ff" && args[0].ToLower() != "codelock" && args[0].ToLower() != "turret" && !args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                Reply(player, "Syntax");
                return;
            }
            double time = GrabCurrentTime();

            switch (args[0].ToLower())
            {
                case "list":
                    var friendList = GetFriendListS(player.Id);
                    if (friendList.Length > 0)
                        Reply(player, "List", $"{friendList.Length}/{configData.MaxFriends}", string.Join(", ", friendList));
                    else
                        Reply(player, "NoFriends");
                    return;

                case "add":
                case "+":
                    double nextUseTime = !userCooldowns.ContainsKey(player.Id) ? 0 : userCooldowns[player.Id];
                    if (nextUseTime > time)
                    {
                        Reply(player, "Cooldown", FormatTime(nextUseTime - time), "add");
                        return;
                    }
                    var foundPlayers = players.FindPlayers(args[1]).ToArray();

                    if (foundPlayers.Length > 1)
                    {
                        Reply(player, "MultiplePlayers", string.Join(", ", foundPlayers.Select(p => p.Name).ToArray()));
                        return;
                    }

                    var friendPlayer = foundPlayers.Length == 1 ? foundPlayers[0] : null;
                    if (friendPlayer == null)
                    {
                        Reply(player, "PlayerNotFound", args[1]);
                        return;
                    }
                    if (player == friendPlayer)
                    {
                        Reply(player, "CantAddSelf");
                        return;
                    }
                    var playerData = GetPlayerData(Convert.ToUInt64(player.Id));
                    if (playerData.Friends.Count >= configData.MaxFriends)
                    {
                        Reply(player, "FriendlistFull");
                        return;
                    }

                    if (playerData.Friends.Contains(Convert.ToUInt64(friendPlayer.Id)))
                    {
                        Reply(player, "AlreadyOnList", friendPlayer.Name);
                        return;
                    }
                    if (pendings.ContainsKey(Convert.ToUInt64(friendPlayer.Id)))
                    {
                        if (pendings[Convert.ToUInt64(friendPlayer.Id)] == Convert.ToUInt64(player.Id))
                        {
                            Reply(player, "AlreadyPending");
                            return;
                        }
                        Reply(player, "PendingBusy");
                        return;
                    }
                    if (!friendPlayer.IsConnected)
                    {
                        Reply(player, "Disconnected");
                        return;
                    }
                    PendingAdd(player, friendPlayer);
                    mytimer = timer.Once(60f, () =>
                    {
                        ulong sender;
                        if (pendings.TryGetValue(Convert.ToUInt64(friendPlayer.Id), out sender) && sender == Convert.ToUInt64(player.Id))
                        {
                            pendings.Remove(Convert.ToUInt64(friendPlayer.Id));
                        }
                    });
                    Reply(player, "PendingSuccessSend");
                    if (friendPlayer.IsConnected)
                    {
                        Reply(friendPlayer, "Pending", player.Name);
                    }
                    if (!userCooldowns.ContainsKey(player.Id))
                        userCooldowns.Add(player.Id, time + configData.FriendCooldown);
                    else
                        userCooldowns[player.Id] = time + configData.FriendCooldown;
                    return;
                case "accept":
                    double nextUseTime2 = !userCooldowns.ContainsKey(player.Id) ? 0 : userCooldowns[player.Id];
                    if (nextUseTime2 > time)
                    {
                        Reply(player, "Cooldown", FormatTime(nextUseTime2 - time), "add");
                        return;
                    }
                    ulong sensder;
                    if (pendings.TryGetValue(Convert.ToUInt64(player.Id), out sensder))
                    {

                        AddFriendS(player.Id, sensder.ToString());
                        Reply(player, "FriendAdded", GetPlayerData(Convert.ToUInt64(sensder)).Name);
                        var p = players.FindPlayerById(sensder.ToString());
                        Reply(p, "FriendAdded", player.Name);
                        pendings.Remove(Convert.ToUInt64(player.Id));

                        if (!userCooldowns.ContainsKey(player.Id))
                            userCooldowns.Add(player.Id, time + configData.FriendCooldown);
                        else
                            userCooldowns[player.Id] = time + configData.FriendCooldown;

                    }
                    else
                    {
                        Reply(player, "PendingNotFound");
                    }
                    return;
                case "remove":
                case "-":
                    var friend = FindFriend(args[1]);
                    if (friend <= 0)
                    {
                        Reply(player, "NotOnFriendlist", args[1]);
                        return;
                    }

                    var removed = RemoveFriendS(player.Id, friend.ToString());
                    Reply(player, removed ? "FriendRemoved" : "NotOnFriendlist", args[1]);
                    return;

                case "codelock":
                    if (!configData.ShareCodeLocks) return;
                    var playerSetting = GetPlayerData(Convert.ToUInt64(player.Id));
                    if (!playerSetting.CodeAuthorization)
                    {
                        playerSetting.CodeAuthorization = true;
                        Reply(player, "Codelock", "включили", "green");
                    }
                    else
                    {
                        playerSetting.CodeAuthorization = false;
                        Reply(player, "Codelock", "отключили", "red");
                    }
                    return;
                case "turret":
                    if (!configData.ShareAutoTurrets) return;
                    var playerSetting1 = GetPlayerData(Convert.ToUInt64(player.Id));
                    if (!playerSetting1.TurrentAuthorization)
                    {
                        playerSetting1.TurrentAuthorization = true;
                        Reply(player, "Turrets", "включили", "green");
                    }
                    else
                    {
                        playerSetting1.TurrentAuthorization = false;
                        Reply(player, "Turrets", "отключили", "red");
                    }
                    return;
                case "ff":
                    var playerSetting2 = GetPlayerData(Convert.ToUInt64(player.Id));
                    if (!playerSetting2.AttackFriend)
                    {
                        playerSetting2.AttackFriend = true;
                        Reply(player, "Attacks", "включили", "green");
                    }
                    else
                    {
                        playerSetting2.AttackFriend = false;
                        Reply(player, "Attacks", "отключили", "red");
                    }
                    return;
            }
        }

        private void PendingAdd(IPlayer sender, IPlayer target)
        {
            pendings[Convert.ToUInt64(target.Id)] = Convert.ToUInt64(sender.Id);
            
        }
        #endregion

        private void Reply(IPlayer player, string langKey, params object[] args)
        {
            if (langKey == "Syntax")
            {
                if (configData.ShareCodeLocks && configData.ShareAutoTurrets)
                {
                    player.Reply(string.Format(Messages["Syntax"] + Messages["CodeLocks"] + Messages["Turret"], args));
                    return;
                }

                if (configData.ShareCodeLocks)
                {
                    player.Reply(string.Format(Messages["Syntax"] + Messages["CodeLocks"], args));
                    return;
                }
                if (configData.ShareAutoTurrets)
                {
                    player.Reply(string.Format(Messages["Syntax"] + Messages["Turret"], args));
                    return;
                }
            }
            player.Reply(string.Format(Messages[langKey], args));
        }

        private void SendHelpText(object obj)
        {
            var player = players.FindPlayerByObj(obj);
            if (player != null) Reply(player, "HelpText");
        }

        private void AddFriendReverse(ulong playerId, ulong friendId)
        {
            HashSet<ulong> friends;
            if (!ReverseData.TryGetValue(friendId, out friends))
                ReverseData[friendId] = friends = new HashSet<ulong>();
            friends.Add(playerId);
        }

        private ulong FindFriend(string friend)
        {
            if (string.IsNullOrEmpty(friend)) return 0;
            foreach (var playerData in FriendsData)
            {
                if (playerData.Key.ToString().Equals(friend) || playerData.Value.Name.IndexOf(friend, StringComparison.OrdinalIgnoreCase) >= 0)
                    return playerData.Key;
            }
            return 0;
        }
    }
}  
                                                                                 
                