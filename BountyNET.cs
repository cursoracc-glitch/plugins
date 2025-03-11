//Requires: RustNET
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("BountyNET", "k1lly0u", "0.1.8")]
    [Description("A bounty system that can only be access via a RustNET terminal")]
    class BountyNET : RustPlugin
    {
        [PluginReference] Plugin Clans, Friends, PopupNotifications, Economics, ServerRewards;
        private static BountyNET ins;

        private StoredData storedData;
        private OfflinePlayers offlinePlayers;
        private DynamicConfigFile data, offline;

        private Dictionary<ulong, ulong> bountyCreator = new Dictionary<ulong, ulong>();
        private Dictionary<StorageContainer, ulong> openContainers = new Dictionary<StorageContainer, ulong>();
        private Dictionary<int, string> idToDisplayName = new Dictionary<int, string>();

        private string boxPrefab = "assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab";

        private FriendManager friendManager;

        #region Oxide Hooks     
        private void Loaded()
        {
            permission.RegisterPermission("bountynet.use", this);
            permission.RegisterPermission("bountynet.admin", this);

            lang.RegisterMessages(messages, this);

            friendManager = new GameObject("FriendManager").AddComponent<FriendManager>();

            data = Interface.Oxide.DataFileSystem.GetFile("RustNET/bounty_data");
            offline = Interface.Oxide.DataFileSystem.GetFile("RustNET/bounty_offlineplayers");
        }

        private void OnServerInitialized()
        {
            ins = this;
            idToDisplayName = ItemManager.itemList.ToDictionary(x => x.itemid, y => y.displayName.english);
            LoadData();

            offlinePlayers.RemoveOldPlayers();

            RustNET.RegisterModule(Title, this);
            RustNET.ins.AddImage(Title, configData.RustNETIcon);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            PlayerData playerData;
            if (storedData.players.TryGetValue(player.userID, out playerData))
            {
                if (playerData.activeBounties.Count > 0)
                    BroadcastToPlayer(player, string.Format(msg("Chat.OutstandingBounties", player.userID), playerData.activeBounties.Count));

                playerData.displayName = player.displayName;
            }

            offlinePlayers.OnPlayerInit(player.UserIDString);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            PlayerData playerData;
            if (storedData.players.TryGetValue(player.userID, out playerData))
                playerData.UpdateWantedTime();

            offlinePlayers.AddOfflinePlayer(player.UserIDString);
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return null;

            if (entity is StorageContainer && openContainers.ContainsKey(entity as StorageContainer))
                return false;

            return null;
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;

            BasePlayer victim = entity.ToPlayer();
            BasePlayer attacker = info.InitiatorPlayer;

            if (victim == null || attacker == null || attacker.GetComponent<NPCPlayer>())
                return;

            PlayerData victimData;
            if (!storedData.players.TryGetValue(victim.userID, out victimData))
                return;

            if (victimData.activeBounties.Count == 0)
                return;

            if (IsFriendlyPlayer(victim.userID, attacker.userID))
            {
                BroadcastToPlayer(attacker, msg("Chat.IsFriend1", attacker.userID));
                return;
            }

            victimData.UpdateWantedTime();

            List<int> rewards = victimData.activeBounties.Select(x => x.rewardId).ToList();
            victimData.activeBounties.Clear();

            PlayerData attackerData;
            if (!storedData.players.TryGetValue(attacker.userID, out attackerData))
            {
                attackerData = new PlayerData(attacker.displayName);
                storedData.players.Add(attacker.userID, attackerData);
            }

            attackerData.ClaimRewards(rewards);
            BroadcastToPlayer(attacker, string.Format(msg("Chat.RewardPending"), victim.displayName, rewards.Count));
        }

        private object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (player == null || container == null || !openContainers.ContainsKey(container))
                return null;

            if (openContainers[container] != player.userID)
                return false;

            return null;
        }

        private void OnPlayerLootEnd(PlayerLoot inventory)
        {
            BasePlayer player = inventory.GetComponent<BasePlayer>();

            if (bountyCreator.ContainsKey(player.userID))
            {
                StorageContainer container = inventory.entitySource.GetComponent<StorageContainer>();
                if (container != null)
                {
                    if (container.inventory.itemList.Count == 0)
                        SendReply(player, msg("Chat.NoItemsDeposited", player.userID));
                    else CreateNewBounty(player, bountyCreator[player.userID], 0, 0, container.inventory, false);

                    openContainers.Remove(container);
                    ClearContainer(container.inventory);
                    container.DieInstantly();
                }
                bountyCreator.Remove(player.userID);
            }
        }

        private void OnServerSave() => SaveData();

        private void Unload()
        {
            if (!ServerMgr.Instance.Restarting)
                SaveData();

            if (friendManager != null)
                UnityEngine.Object.Destroy(friendManager.gameObject);

            ins = null;
        }
        #endregion

        #region Functions  
        private void BroadcastToPlayer(BasePlayer player, string message)
        {
            if (configData.Notifications.UsePopupNotifications && PopupNotifications)
                PopupNotifications?.Call("CreatePopupOnPlayer", message, player, configData.Notifications.PopupDuration);
            else SendReply(player, message);
        }

        private void PopupToPlayer(BasePlayer player, string message, float duration = 5f)
        {
            string panelId = $"RustNET.Popup {UnityEngine.Random.Range(0, 100000)}";
            CuiElementContainer container = RustNET.UI.Container(RustNET.uiColors[RustNET.Colors.Background], "0.15 0.115", "0.85 0.145", false, "Hud", panelId);
            RustNET.UI.Label(ref container, message, 11, "0 0", "1 1", TextAnchor.MiddleCenter, panelId);
            CuiHelper.AddUi(player, container);
            timer.In(duration, () => CuiHelper.DestroyUi(player, panelId));
        }

        private void CreateNewBounty(BasePlayer initiator, ulong targetId, int rpAmount, int ecoAmount, ItemContainer container, bool notify)
        {
            IPlayer target = covalence.Players.FindPlayerById(targetId.ToString());

            PlayerData playerData;
            if (!storedData.players.TryGetValue(targetId, out playerData))
            {
                playerData = new PlayerData(target?.Name ?? "No Name");
                storedData.players.Add(targetId, playerData);
            }

            playerData.totalBounties++;

            int rewardId = GetUniqueId();
            storedData.rewards.Add(rewardId, new RewardInfo(rpAmount, ecoAmount, container));
            playerData.activeBounties.Add(new PlayerData.BountyInfo(initiator.userID, initiator.displayName, rewardId));

            BasePlayer targetPlayer = target?.Object as BasePlayer;
            if (targetPlayer != null)
                BroadcastToPlayer(targetPlayer, string.Format(msg("Chat.PlacedTarget", targetPlayer.userID), initiator.displayName));

            if (notify)
                PopupToPlayer(initiator, string.Format(msg("UI.Add.PlacedInitiator", initiator.userID), target?.Name ?? "No Name"));
            else BroadcastToPlayer(initiator, string.Format(msg("Chat.PlacedInitiator", initiator.userID), target?.Name ?? "No Name"));

            if (configData.Notifications.BroadcastNewBounties)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (player == initiator || player.userID == targetId)
                        continue;
                    BroadcastToPlayer(player, string.Format(msg("Chat.PlacedGlobal", player.userID), initiator.displayName, target?.Name ?? "No Name"));
                }
            }
        }        

        private void GivePlayerRewards(BasePlayer player, RewardInfo rewardInfo, bool notify)
        {
            if (rewardInfo.econAmount > 0 && Economics)
                Economics?.Call("Deposit", player.UserIDString, (double)rewardInfo.econAmount);

            if (rewardInfo.rpAmount > 0 && ServerRewards)
                ServerRewards?.Call("AddPoints", player.userID, rewardInfo.rpAmount);

            if (rewardInfo.rewardItems.Count > 0)
            {
                foreach (RewardInfo.ItemData itemData in rewardInfo.rewardItems)
                {
                    Item item = CreateItem(itemData);
                    player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                }
            }

            if (notify)
                PopupToPlayer(player, msg("UI.Reward.Claimed", player.userID));
        }

        private Item CreateItem(RewardInfo.ItemData itemData)
        {
            Item item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
            item.condition = itemData.condition;

            if (itemData.instanceData != null)
                itemData.instanceData.Restore(item);

            BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                if (!string.IsNullOrEmpty(itemData.ammotype))
                    weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                weapon.primaryMagazine.contents = itemData.ammo;
            }
            if (itemData.contents != null)
            {
                foreach (var contentData in itemData.contents)
                {
                    var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                    if (newContent != null)
                    {
                        newContent.condition = contentData.condition;
                        newContent.MoveToContainer(item.contents);
                    }
                }
            }
            return item;
        }

        private void SpawnItemContainer(BasePlayer player)
        {
            StorageContainer container = (StorageContainer)GameManager.server.CreateEntity(boxPrefab, player.transform.position + player.eyes.BodyForward(), new Quaternion(), true);
            container.enableSaving = false;
            container.Spawn();

            openContainers.Add(container, player.userID);
            timer.In(0.15f, () => OpenInventory(player, container));
        }

        private void OpenInventory(BasePlayer player, StorageContainer container)
        {
            player.inventory.loot.Clear();
            player.inventory.loot.entitySource = container;
            player.inventory.loot.itemSource = null;
            player.inventory.loot.AddContainer(container.inventory);
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic");
            player.SendNetworkUpdate();
        }

        private void ClearContainer(ItemContainer itemContainer)
        {
            if (itemContainer == null || itemContainer.itemList == null) return;
            while (itemContainer.itemList.Count > 0)
            {
                var item = itemContainer.itemList[0];
                item.RemoveFromContainer();
                item.Remove(0f);
            }
        }

        private int GetUniqueId()
        {
            int uid = UnityEngine.Random.Range(0, 10000);
            if (storedData.rewards.ContainsKey(uid))
                return GetUniqueId();
            return uid;
        }

        private double CurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private List<BasePlayer> FindPlayer(string partialNameOrId)
        {
            List<BasePlayer> players = new List<BasePlayer>();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (partialNameOrId == player.UserIDString)
                    return new List<BasePlayer>() { player };

                if (player.displayName.ToLower().Contains(partialNameOrId.ToLower()))
                    players.Add(player);
            }
            return players;
        }

        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            if (hours > 0)
                return string.Format("{0:00}h {1:00}m {2:00}s", hours, mins, secs);
            else return string.Format("{0:00}m {1:00}s", mins, secs);
        }

        private string RemoveTag(string str)
        {
            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
            {
                str = str.Substring(str.IndexOf("]") + 1).Trim();
            }
            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
                RemoveTag(str);
            return str;
        }

        private string TrimToSize(string str)
        {
            if (str.Length > 20)
                str = str.Substring(0, 20);
            return str;
        }

        private string GetHelpString(ulong playerId, bool title) => title ? msg("UI.Help.Title", playerId) : msg("UI.Help", playerId);

        private bool AllowPublicAccess() => true;
        #endregion

        #region Friends
        private class FriendManager : MonoBehaviour
        {
            private List<FriendEntry> friends = new List<FriendEntry>();

            private void Awake()
            {
                enabled = false;
                InvokeHandler.InvokeRepeating(this, RemoveOldData, 60, 60);
            }

            public void OnFriendshipEnded(string playerId, string friendId)
            {
                friends.Add(new FriendEntry(playerId, friendId));
            }

            public bool WereFriends(string playerId, string friendId)
            {
                bool flag = false;
                IEnumerable<FriendEntry> entries = friends.Where(x => x.playerId == playerId || x.friendId == playerId);
                foreach(FriendEntry entry in entries)
                {
                    if (entry.playerId == friendId || entry.friendId == friendId)
                    {
                        flag = true;
                        break;
                    }
                }
                entries = null;
                return flag;
            }

            public bool WereFriends(ulong playerId, ulong friendId) => WereFriends(playerId.ToString(), friendId.ToString());

            private void RemoveOldData()
            {
                double currentTime = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

                for (int i = friends.Count - 1; i >= 0; i--)
                {
                    FriendEntry entry = friends.ElementAt(i);
                    if (currentTime > entry.removeAt)
                        friends.Remove(entry);
                }
            }

            public struct FriendEntry
            {
                public string playerId;
                public string friendId;

                public double removeAt;

                public FriendEntry(string playerId, string friendId)
                {
                    this.playerId = playerId;
                    this.friendId = friendId;

                    removeAt = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds + 3600;
                }
            }
        }

        public bool IsFriendlyPlayer(ulong playerId, ulong friendId)
        {
            if (playerId == friendId || IsFriend(playerId, friendId) || IsClanmate(playerId, friendId) || IsTeamMate(playerId, friendId) || friendManager.WereFriends(playerId, friendId))
                return true;
            return false;
        }

        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            if (!Clans || !configData.IgnoreClans) return false;
            object playerTag = Clans?.Call("GetClanOf", playerId);
            object friendTag = Clans?.Call("GetClanOf", friendId);
            if ((playerTag is string && !string.IsNullOrEmpty((string)playerTag)) && (friendTag is string && !string.IsNullOrEmpty((string)friendTag)))
                if (playerTag == friendTag) return true;
            return false;
        }

        private bool IsFriend(ulong playerID, ulong friendID)
        {
            if (!Friends || !configData.IgnoreFriends) return false;
            return (bool)Friends?.Call("AreFriends", playerID, friendID);
        }

        private bool IsTeamMate(ulong playerId, ulong friendId)
        {
            BasePlayer player = RelationshipManager.FindByID(playerId);
            if (player == null)
                return false;

            RelationshipManager.PlayerTeam playerTeam = RelationshipManager.Instance.FindTeam(player.currentTeam);
            if (playerTeam != null)
                return playerTeam.members.Contains(friendId) || playerTeam.invites.Contains(friendId);

            return false;
        }

        private void OnFriendRemoved(string playerId, string friendId)
        {
            friendManager.OnFriendshipEnded(playerId.ToString(), friendId.ToString());
        }

        private void OnServerCommand(ConsoleSystem.Arg arg)
        {
            string command = arg?.cmd?.FullName;
            if (string.IsNullOrEmpty(command))
                return;

            BasePlayer player = arg.Player();
            if (player == null)
                return;

            if (command == "relationshipmanager.leaveteam")
            {
                RelationshipManager.PlayerTeam currentTeam = RelationshipManager.Instance.FindTeam(player.currentTeam);
                if (currentTeam == null)                
                    return;

                foreach (ulong member in currentTeam.members)
                {
                    if (player.userID == member)
                        continue;
                    OnFriendRemoved(player.UserIDString, member.ToString());
                }                
                return;
            }

            if (command == "relationshipmanager.kickmember")
            {
                RelationshipManager.PlayerTeam currentTeam = RelationshipManager.Instance.FindTeam(player.currentTeam);
                if (currentTeam == null)
                    return;

                ulong target = arg.GetULong(0, (ulong)0);
                foreach (ulong member in currentTeam.members)
                {
                    if (target == member)
                        continue;
                    OnFriendRemoved(target.ToString(), member.ToString());
                }
            }
        }
        #endregion

        #region UI  
        #region Main Menu
        private CuiElementContainer CreateBountyContainer(BasePlayer player, int terminalId, string title, string returnCommand = null)
        {
            CuiElementContainer container = RustNET.ins.GetBaseContainer(player, terminalId, Title);

            RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], "0.04 0.765", "0.96 0.8");
            RustNET.UI.Label(ref container, msg("UI.Menu.Bounty", player.userID) + title, 12, "0.05 0.765", "0.8 0.8", TextAnchor.MiddleLeft);
            RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Return", player.userID), 11, "0.82 0.765", "0.96 0.8", string.IsNullOrEmpty(returnCommand) ? $"bounty.changepage {terminalId} home" : returnCommand);

            if (!permission.UserHasPermission(player.UserIDString, "bountynet.use"))
            {
                RustNET.UI.Label(ref container, msg("UI.Menu.NoPermission", player.userID), 12, "0.05 0.5", "0.95 0.7");
                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
                return null;
            }
            return container;
        }

        private void CreateConsoleWindow(BasePlayer player, int terminalId, int page)
        {
            CuiElementContainer container = CreateBountyContainer(player, terminalId, string.Empty, $"rustnet.changepage {terminalId}");

            if (container != null)               
            {
                int i = 0;

                if (configData.Rewards.AllowItems)
                {
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (i * 0.04f))}", $"0.31 {(0.755f - (i * 0.04f))}");
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Menu.AddItems", player.userID), 11, $"0.05 {0.725f - (i * 0.04f)}", $"0.3 {0.755f - (i * 0.04f)}", $"bounty.changepage {terminalId} add items", TextAnchor.MiddleLeft);
                    i++;
                }

                if (configData.Rewards.AllowServerRewards && ServerRewards)
                {
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (i * 0.04f))}", $"0.31 {(0.755f - (i * 0.04f))}");
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Menu.AddRP", player.userID), 11, $"0.05 {0.725f - (i * 0.04f)}", $"0.3 {0.755f - (i * 0.04f)}", $"bounty.changepage {terminalId} add rp", TextAnchor.MiddleLeft);
                    i++;
                }

                if (configData.Rewards.AllowEconomics && Economics)
                {
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (i * 0.04f))}", $"0.31 {(0.755f - (i * 0.04f))}");
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Menu.AddEco", player.userID), 11, $"0.05 {0.725f - (i * 0.04f)}", $"0.3 {0.755f - (i * 0.04f)}", $"bounty.changepage {terminalId} add eco", TextAnchor.MiddleLeft);
                    i++;
                }


                RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (i * 0.04f))}", $"0.31 {(0.755f - (i * 0.04f))}");
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Menu.Cancel", player.userID), 11, $"0.05 {0.725f - (i * 0.04f)}", $"0.3 {0.755f - (i * 0.04f)}", $"bounty.changepage {terminalId} cancel 0", TextAnchor.MiddleLeft);
                i++;

                RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (i * 0.04f))}", $"0.31 {(0.755f - (i * 0.04f))}");
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Menu.Claim", player.userID), 11, $"0.05 {0.725f - (i * 0.04f)}", $"0.3 {0.755f - (i * 0.04f)}", $"bounty.changepage {terminalId} claim 0", TextAnchor.MiddleLeft);
                i++;

                RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (i * 0.04f))}", $"0.31 {(0.755f - (i * 0.04f))}");
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Menu.View", player.userID), 11, $"0.05 {0.725f - (i * 0.04f)}", $"0.3 {0.755f - (i * 0.04f)}", $"bounty.changepage {terminalId} view 0", TextAnchor.MiddleLeft);
                i++;

                RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (i * 0.04f))}", $"0.31 {(0.755f - (i * 0.04f))}");
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Menu.Wanted", player.userID), 11, $"0.05 {0.725f - (i * 0.04f)}", $"0.3 {0.755f - (i * 0.04f)}", $"bounty.changepage {terminalId} wanted 0", TextAnchor.MiddleLeft);
                i++;

                RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (i * 0.04f))}", $"0.31 {(0.755f - (i * 0.04f))}");
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Menu.Hunters", player.userID), 11, $"0.05 {0.725f - (i * 0.04f)}", $"0.3 {0.755f - (i * 0.04f)}", $"bounty.changepage {terminalId} hunters 0", TextAnchor.MiddleLeft);
                i++;
                
                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
            }            
        }       
        #endregion

        #region Bounty Creation       
        private void CreatePlayerSelectionMenu(BasePlayer player, int terminalId, int page, bool isOffline, string type)
        {
            CuiElementContainer container = CreateBountyContainer(player, terminalId, msg("UI.SelectPlayer", player.userID));
            if (container != null)
            {
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], isOffline ? msg("UI.Selection.Offline", player.userID) : msg("UI.Selection.Online", player.userID), 11, "0.59 0.765", "0.81 0.8", $"bounty.changepage {terminalId} selection {page} {!isOffline} {type}");

                int count = 0;
                int startAt = page * 90;
                                
                IPlayer[] players = isOffline ? offlinePlayers.GetOfflineList().OrderBy(x => x.Name).ToArray() : covalence.Players.Connected.OrderBy(x => x.Name).ToArray();
                for (int i = startAt; i < (startAt + 90 > players.Length ? players.Length : startAt + 90); i++)
                {
                    IPlayer target = players.ElementAt(i);

                    //if (target.Id == player.UserIDString)
                        //continue;

                    PlayerData playerData;
                    if (storedData.players.TryGetValue(ulong.Parse(target.Id), out playerData))
                    {
                        if (playerData.GetBountyOf(player.userID) != null)
                            continue;
                    }

                    float[] position = GetButtonPosition(count);

                    if (count == 0 || count % 5 == 0)
                        RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {position[1]}", $"0.96 {position[3]}");

                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], string.IsNullOrEmpty(target.Name) ? target.Id : TrimToSize(RemoveTag(target.Name)), 11, $"{position[0]} {position[1]}", $"{position[2]} {position[3]}", $"bounty.addbounty {type} {terminalId} {target.Id} 0");

                    count++;
                }

                int totalPages = players.Length / 90;

                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Back", player.userID), 11, "0.3 0.01", "0.44 0.04", page > 0 ? $"bounty.changepage {terminalId} selection {page - 1} {isOffline} {type}" : "");
                RustNET.UI.Label(ref container, string.Format(RustNET.msg("UI.Page", player.userID), page + 1, totalPages + 1), 11, "0.44 0.01", "0.56 0.04");
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Next", player.userID), 11, "0.56 0.01", "0.7 0.04", page + 1 <= totalPages ? $"bounty.changepage {terminalId} selection {page + 1} {isOffline} {type}" : "");

                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
            }
        }

        private void CreateNewAmountMenu(BasePlayer player, int terminalId, ulong targetId, int amount, bool isRp)
        {
            CuiElementContainer container = CreateBountyContainer(player, terminalId, msg("UI.Tip.SelectAmount", player.userID));

            if (container != null)
            {           
                string targetName = covalence.Players.FindPlayerById(targetId.ToString())?.Name ?? targetId.ToString();

                RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 0.725", $"0.96 0.755");
                RustNET.UI.Label(ref container, string.Format(isRp ? msg("UI.Tip.SelectAmountRP", player.userID) : msg("UI.Tip.SelectAmountEco", player.userID), targetName), 11, $"0.05 0.725", $"0.95 0.755", TextAnchor.MiddleLeft);

                RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 0.685", $"0.96 0.715");
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], "- 1000", 11, $"0.04 0.685", $"0.1 0.715", $"bounty.addbounty {(isRp ? "rp" : "eco")} {terminalId} {targetId} {amount - 1000}", TextAnchor.MiddleCenter);
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], "- 100", 11, $"0.11 0.685", $"0.16 0.715", $"bounty.addbounty {(isRp ? "rp" : "eco")} {terminalId} {targetId} {amount - 100}", TextAnchor.MiddleCenter);
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], "- 10", 11, $"0.17 0.685", $"0.22 0.715", $"bounty.addbounty {(isRp ? "rp" : "eco")} {terminalId} {targetId} {amount - 10}", TextAnchor.MiddleCenter);
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], "- 1", 11, $"0.23 0.685", $"0.28 0.715", $"bounty.addbounty {(isRp ? "rp" : "eco")} {terminalId} {targetId} {amount - 1}", TextAnchor.MiddleCenter);
                RustNET.UI.Label(ref container, string.Format((isRp ? msg("UI.Reward.RP", player.userID) : msg("UI.Reward.Econ", player.userID)), amount), 11, $"0.28 0.685", $"0.48 0.715", TextAnchor.MiddleCenter);
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], "+ 1", 11, $"0.48 0.685", $"0.53 0.715", $"bounty.addbounty {(isRp ? "rp" : "eco")} {terminalId} {targetId} {amount + 1}", TextAnchor.MiddleCenter);
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], "+ 10", 11, $"0.54 0.685", $"0.59 0.715", $"bounty.addbounty {(isRp ? "rp" : "eco")} {terminalId} {targetId} {amount + 10}", TextAnchor.MiddleCenter);
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], "+ 100", 11, $"0.6 0.685", $"0.65 0.715", $"bounty.addbounty {(isRp ? "rp" : "eco")} {terminalId} {targetId} {amount + 100}", TextAnchor.MiddleCenter);
                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], "+ 1000", 11, $"0.66 0.685", $"0.72 0.715", $"bounty.addbounty {(isRp ? "rp" : "eco")} {terminalId} {targetId} {amount + 1000}", TextAnchor.MiddleCenter);

                RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Add.Confirm", player.userID), 11, $"0.82 0.685", $"0.96 0.715", $"bounty.addbounty {(isRp ? "rp" : "eco")} {terminalId} {targetId} {amount} confirm", TextAnchor.MiddleCenter);

                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
            }
        }
        #endregion

        #region Cancel Bounty
        private void CreateCancelBountyMenu(BasePlayer player, int terminalId, int page)
        {
            CuiElementContainer container = CreateBountyContainer(player, terminalId, msg("UI.Tip.Cancel", player.userID));
            if (container != null)
            {   
                Dictionary<ulong, PlayerData.BountyInfo> activeBounties = storedData.players.Where(x => x.Value.activeBounties.Exists(y => y.initiatorId == player.userID)).ToDictionary(x => x.Key, y => y.Value.activeBounties.FirstOrDefault(x => x.initiatorId == player.userID));

                if (activeBounties == null || activeBounties.Count == 0)
                    RustNET.UI.Label(ref container, msg("UI.Cancel.NoneSet", player.userID), 12, "0.05 0.5", "0.95 0.7");
                else
                {
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {0.725f}", $"0.96 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.View.Name", player.userID), 11, $"0.05 {0.725f}", $"0.24 {0.755f}", TextAnchor.MiddleLeft);
                    RustNET.UI.Label(ref container, msg("UI.View.TimeActive", player.userID), 11, $"0.25 {0.725f}", $"0.44 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.View.Reward", player.userID), 11, $"0.45 {0.725f}", $"0.81 {0.755f}", TextAnchor.MiddleLeft);
                    
                    int count = 1;
                    int startAt = page * 17;
                    for (int i = startAt; i < (startAt + 17 > activeBounties.Count ? activeBounties.Count : startAt + 17); i++)
                    {
                        var bountyInfo = activeBounties.ElementAt(i);

                        RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (count * 0.04f))}", $"0.96 {(0.755f - (count * 0.04f))}");
                        RustNET.UI.Label(ref container, $"> {covalence.Players.FindPlayerById(bountyInfo.Key.ToString())?.Name ?? bountyInfo.Key.ToString()}", 11, $"0.05 {0.725f - (count * 0.04f)}", $"0.24 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);
                        RustNET.UI.Label(ref container, FormatTime(CurrentTime() - bountyInfo.Value.initiatedTime), 11, $"0.25 {0.725f - (count * 0.04f)}", $"0.44 {0.755f - (count * 0.04f)}");
                        RustNET.UI.Label(ref container, storedData.rewards[bountyInfo.Value.rewardId].GetRewardString(player.userID), 10, $"0.45 {0.725f - (count * 0.04f)}", $"0.81 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);

                        RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.View.Cancel", player.userID), 11, $"0.82 {0.725f - (count * 0.04f)}", $"0.96 {0.755f - (count * 0.04f)}", $"bounty.cancelbounty {terminalId} {page} {bountyInfo.Key} {player.userID} false");

                        count++;
                    }

                    int totalPages = activeBounties.Count / 17;

                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Back", player.userID), 11, "0.3 0.01", "0.44 0.04", page > 0 ? $"bounty.changepage {terminalId} cancel {page - 1}" : "");
                    RustNET.UI.Label(ref container, string.Format(RustNET.msg("UI.Page", player.userID), page + 1, totalPages + 1), 11, "0.44 0.01", "0.56 0.04");
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Next", player.userID), 11, "0.56 0.01", "0.7 0.04", page + 1 <= totalPages ? $"bounty.changepage {terminalId} cancel {page + 1}" : "");
                }

                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
            }            
        }
        #endregion

        #region Claim
        private void CreateClaimMenu(BasePlayer player, int terminalId, int page)
        {
            CuiElementContainer container = CreateBountyContainer(player, terminalId, msg("UI.Tip.Claim", player.userID));
            if (container != null)
            {
                PlayerData playerData;  
                if (!storedData.players.TryGetValue(player.userID, out playerData) || playerData.unclaimedRewards.Count == 0)
                    RustNET.UI.Label(ref container, msg("UI.Claim.NoRewardsPending", player.userID), 12, "0.05 0.5", "0.95 0.7");
                else
                {
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {0.725f}", $"0.96 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.View.Reward", player.userID), 11, $"0.05 {0.725f}", $"0.81 {0.755f}", TextAnchor.MiddleLeft);                    

                    int count = 1;
                    int startAt = page * 17;
                    for (int i = startAt; i < (startAt + 17 > playerData.unclaimedRewards.Count ? playerData.unclaimedRewards.Count : startAt + 17); i++)
                    {
                        RewardInfo rewardInfo = storedData.rewards[playerData.unclaimedRewards.ElementAt(i)];

                        RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (count * 0.04f))}", $"0.96 {(0.755f - (count * 0.04f))}");
                        RustNET.UI.Label(ref container, $"> {rewardInfo.GetRewardString(player.userID)}", 11, $"0.05 {0.725f - (count * 0.04f)}", $"0.81 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);                        

                        RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.Claim.ClaimReward", player.userID), 11, $"0.82 {0.725f - (count * 0.04f)}", $"0.96 {0.755f - (count * 0.04f)}", $"bounty.claimreward {terminalId} {page} {playerData.unclaimedRewards.ElementAt(i)}");

                        count++;
                    }

                    int totalPages = playerData.unclaimedRewards.Count / 17;

                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Back", player.userID), 11, "0.3 0.01", "0.44 0.04", page > 0 ? $"bounty.changepage {terminalId} claim {page - 1}" : "");
                    RustNET.UI.Label(ref container, string.Format(RustNET.msg("UI.Page", player.userID), page + 1, totalPages + 1), 11, "0.44 0.01", "0.56 0.04");
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Next", player.userID), 11, "0.56 0.01", "0.7 0.04", page + 1 <= totalPages ? $"bounty.changepage {terminalId} claim {page + 1}" : "");
                }

                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
            }
        }    
        #endregion

        #region Bounty View
        private void CreateBountyViewMenu(BasePlayer player, int terminalId, int page)
        {
            CuiElementContainer container = CreateBountyContainer(player, terminalId, msg("UI.View.ActiveBounties", player.userID));
            if (container != null)             
            {
                Dictionary<ulong, PlayerData> activeBounties = storedData.players.Where(x => x.Value.activeBounties.Count > 0).ToDictionary(x => x.Key, y => y.Value);

                if (activeBounties == null || activeBounties.Count == 0)
                    RustNET.UI.Label(ref container, msg("UI.View.NoActiveBounties", player.userID), 12, "0.05 0.5", "0.95 0.7");
                else
                {
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {0.725f}", $"0.96 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.View.Name", player.userID), 11, $"0.05 {0.725f}", $"0.39 {0.755f}", TextAnchor.MiddleLeft);
                    RustNET.UI.Label(ref container, msg("UI.Wanted.CurrentTime", player.userID), 11, $"0.4 {0.725f}", $"0.63 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.Wanted.ActiveBounties", player.userID), 11, $"0.64 {0.725f}", $"0.79 {0.755f}");

                    int count = 1;
                    int startAt = page * 17;
                    for (int i = startAt; i < (startAt + 17 > activeBounties.Count ? activeBounties.Count : startAt + 17); i++)
                    {
                        KeyValuePair<ulong, PlayerData> playerData = activeBounties.ElementAt(i);                        
                        RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (count * 0.04f))}", $"0.96 {(0.755f - (count * 0.04f))}");
                        RustNET.UI.Label(ref container, $"> {playerData.Value.displayName}", 11, $"0.05 {0.725f - (count * 0.04f)}", $"0.39 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);
                        RustNET.UI.Label(ref container, FormatTime(CurrentTime() - playerData.Value.activeBounties.Min(x => x.initiatedTime)), 11, $"0.4 {0.725f - (count * 0.04f)}", $"0.63 {0.755f - (count * 0.04f)}");
                        RustNET.UI.Label(ref container, playerData.Value.activeBounties.Count.ToString(), 11, $"0.64 {0.725f - (count * 0.04f)}", $"0.79 {0.755f - (count * 0.04f)}");
                        RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], msg("UI.ViewBounties", player.userID), 11, $"0.82 {0.725f - (count * 0.04f)}", $"0.96 {0.755f - (count * 0.04f)}", $"bounty.viewbounties {terminalId} {playerData.Key} 0");

                        count++;
                    }

                    int totalPages = activeBounties.Count / 17;

                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Back", player.userID), 11, "0.3 0.01", "0.44 0.04", page > 0 ? $"bounty.changepage {terminalId} view {page - 1}" : "");
                    RustNET.UI.Label(ref container, string.Format(RustNET.msg("UI.Page", player.userID), page + 1, totalPages + 1), 11, "0.44 0.01", "0.56 0.04");
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Next", player.userID), 11, "0.56 0.01", "0.7 0.04", page + 1 <= totalPages ? $"bounty.changepage {terminalId} view {page + 1}" : "");
                }
                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
            }            
        }

        private void CreateIndividualBountyView(BasePlayer player, int terminalId, ulong targetId, int page)
        {
            CuiElementContainer container = CreateBountyContainer(player, terminalId, string.Format(msg("UI.View.ActiveBountiesPlayer", player.userID), covalence.Players.FindPlayerById(targetId.ToString())?.Name ?? targetId.ToString()), $"bounty.changepage {terminalId} view 0");

            if (container != null)              
            {
                PlayerData playerData;
                if (!storedData.players.TryGetValue(targetId, out playerData) || playerData.activeBounties.Count == 0)
                    RustNET.UI.Label(ref container, msg("UI.View.NoActiveBountiesPlayer", player.userID), 12, "0.05 0.5", "0.95 0.7");
                else
                {
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {0.725f}", $"0.96 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.View.PlacedBy", player.userID), 11, $"0.05 {0.725f}", $"0.24 {0.755f}", TextAnchor.MiddleLeft);
                    RustNET.UI.Label(ref container, msg("UI.View.TimeActive", player.userID), 11, $"0.25 {0.725f}", $"0.44 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.View.Reward", player.userID), 11, $"0.45 {0.725f}", $"0.91 {0.755f}", TextAnchor.MiddleLeft);
                    if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, "bountynet.admin"))
                        RustNET.UI.Label(ref container, msg("UI.View.AdminCancel", player.userID), 11, $"0.82 {0.725f}", $"0.95 {0.755f}", TextAnchor.MiddleRight);

                    int count = 1;
                    int startAt = page * 17;
                    for (int i = startAt; i < (startAt + 17 > playerData.activeBounties.Count ? playerData.activeBounties.Count : startAt + 17); i++)
                    {
                        PlayerData.BountyInfo bountyInfo = playerData.activeBounties.ElementAt(i);
                                                
                        RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (count * 0.04f))}", $"0.96 {(0.755f - (count * 0.04f))}");
                        RustNET.UI.Label(ref container, $"> {bountyInfo.initiatorName}", 11, $"0.05 {0.725f - (count * 0.04f)}", $"0.24 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);
                        RustNET.UI.Label(ref container, FormatTime(CurrentTime() - bountyInfo.initiatedTime), 11, $"0.25 {0.725f - (count * 0.04f)}", $"0.44 {0.755f - (count * 0.04f)}");
                        RustNET.UI.Label(ref container, storedData.rewards[bountyInfo.rewardId].GetRewardString(player.userID), 10, $"0.45 {0.725f - (count * 0.04f)}", $"0.925 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);

                        if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, "bountynet.admin"))
                            RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], "X", 10, $"0.935 {0.725f - (count * 0.04f)}", $"0.96 {0.755f - (count * 0.04f)}", $"bounty.cancelbounty {terminalId} {page} {targetId} {bountyInfo.initiatorId} true");

                        count++;
                    }

                    int totalPages = playerData.activeBounties.Count / 17;

                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Back", player.userID), 11, "0.3 0.01", "0.44 0.04", page > 0 ? $"bounty.viewbounties {terminalId} {targetId} {page - 1}" : "");
                    RustNET.UI.Label(ref container, string.Format(RustNET.msg("UI.Page", player.userID), page + 1, totalPages + 1), 11, "0.44 0.01", "0.56 0.04");
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Next", player.userID), 11, "0.56 0.01", "0.7 0.04", page + 1 <= totalPages ? $"bounty.viewbounties {terminalId} {targetId} {page + 1}" : "");
                }

                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
            }           
        }
        #endregion

        #region Most Wanted
        private void CreateWantedMenu(BasePlayer player, int terminalId, int page)
        {
            CuiElementContainer container = CreateBountyContainer(player, terminalId, msg("UI.Tip.Wanted", player.userID));
            if (container != null)             
            {
                PlayerData[] wantedPlayers = storedData.players.Values.Where(x => x.totalBounties > 0).OrderByDescending(x => x.totalWantedTime + x.GetCurrentWantedTime()).ToArray();

                if (wantedPlayers == null || wantedPlayers.Length == 0)
                    RustNET.UI.Label(ref container, msg("UI.Wanted.NoWantedPlayers", player.userID), 12, "0.05 0.5", "0.95 0.7");
                else
                {
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {0.725f}", $"0.96 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.View.Name", player.userID), 11, $"0.05 {0.725f}", $"0.39 {0.755f}", TextAnchor.MiddleLeft);
                    RustNET.UI.Label(ref container, msg("UI.Wanted.TotalTime", player.userID), 11, $"0.4 {0.725f}", $"0.63 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.Wanted.TotalBounties", player.userID), 11, $"0.64 {0.725f}", $"0.79 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.Wanted.ActiveBounties", player.userID), 11, $"0.8 {0.725f}", $"0.95 {0.755f}");

                    int count = 1;
                    int startAt = page * 17;
                    for (int i = startAt; i < (startAt + 17 > wantedPlayers.Length ? wantedPlayers.Length : startAt + 17); i++)
                    {
                        PlayerData playerData = wantedPlayers.ElementAt(i);
                        RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (count * 0.04f))}", $"0.96 {(0.755f - (count * 0.04f))}");
                        RustNET.UI.Label(ref container, $"> {playerData.displayName}", 11, $"0.05 {0.725f - (count * 0.04f)}", $"0.39 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);
                        RustNET.UI.Label(ref container, FormatTime(playerData.totalWantedTime + playerData.GetCurrentWantedTime()), 11, $"0.4 {0.725f - (count * 0.04f)}", $"0.63 {0.755f - (count * 0.04f)}");
                        RustNET.UI.Label(ref container, playerData.totalBounties.ToString(), 11, $"0.64 {0.725f - (count * 0.04f)}", $"0.79 {0.755f - (count * 0.04f)}");
                        RustNET.UI.Label(ref container, playerData.activeBounties.Count.ToString(), 11, $"0.8 {0.725f - (count * 0.04f)}", $"0.95 {0.755f - (count * 0.04f)}");

                        count++;
                    }

                    int totalPages = wantedPlayers.Length / 17;

                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Back", player.userID), 11, "0.3 0.01", "0.44 0.04", page > 0 ? $"bounty.changepage {terminalId} wanted {page - 1}" : "");
                    RustNET.UI.Label(ref container, string.Format(RustNET.msg("UI.Page", player.userID), page + 1, totalPages + 1), 11, "0.44 0.01", "0.56 0.04");
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Next", player.userID), 11, "0.56 0.01", "0.7 0.04", page + 1 <= totalPages ? $"bounty.changepage {terminalId} wanted {page + 1}" : "");
                }

                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
            }            
        }
        #endregion

        #region Hunters
        private void CreateHuntersMenu(BasePlayer player, int terminalId, int page)
        {
            CuiElementContainer container = CreateBountyContainer(player, terminalId, msg("UI.Tip.Hunters", player.userID));
            if (container != null)
            {
                PlayerData[] wantedPlayers = storedData.players.Values.Where(x => x.bountiesClaimed > 0).OrderByDescending(x => x.bountiesClaimed).ToArray();

                if (wantedPlayers == null || wantedPlayers.Length == 0)
                    RustNET.UI.Label(ref container, msg("UI.Hunters.NoHunters", player.userID), 12, "0.05 0.5", "0.95 0.7");
                else
                {
                    RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {0.725f}", $"0.96 {0.755f}");
                    RustNET.UI.Label(ref container, msg("UI.View.Name", player.userID), 11, $"0.05 {0.725f}", $"0.4 {0.755f}", TextAnchor.MiddleLeft);                   
                    RustNET.UI.Label(ref container, msg("UI.Hunters.ClaimedBounties", player.userID), 11, $"0.8 {0.725f}", $"0.95 {0.755f}");

                    int count = 1;
                    int startAt = page * 17;
                    for (int i = startAt; i < (startAt + 17 > wantedPlayers.Length ? wantedPlayers.Length : startAt + 17); i++)
                    {
                        PlayerData playerData = wantedPlayers.ElementAt(i);
                        RustNET.UI.Panel(ref container, RustNET.uiColors[RustNET.Colors.Panel], $"0.04 {(0.725f - (count * 0.04f))}", $"0.96 {(0.755f - (count * 0.04f))}");
                        RustNET.UI.Label(ref container, $"> {playerData.displayName}", 11, $"0.05 {0.725f - (count * 0.04f)}", $"0.4 {0.755f - (count * 0.04f)}", TextAnchor.MiddleLeft);
                        RustNET.UI.Label(ref container, playerData.bountiesClaimed.ToString(), 11, $"0.8 {0.725f - (count * 0.04f)}", $"0.95 {0.755f - (count * 0.04f)}");

                        count++;
                    }

                    int totalPages = wantedPlayers.Length / 17;

                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Back", player.userID), 11, "0.3 0.01", "0.44 0.04", page > 0 ? $"bounty.changepage {terminalId} wanted {page - 1}" : "");
                    RustNET.UI.Label(ref container, string.Format(RustNET.msg("UI.Page", player.userID), page + 1, totalPages + 1), 11, "0.44 0.01", "0.56 0.04");
                    RustNET.UI.Button(ref container, RustNET.uiColors[RustNET.Colors.Button], RustNET.msg("UI.Next", player.userID), 11, "0.56 0.01", "0.7 0.04", page + 1 <= totalPages ? $"bounty.changepage {terminalId} wanted {page + 1}" : "");
                }

                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);
                CuiHelper.AddUi(player, container);
            }            
        }
        #endregion          
        #endregion

        #region UI Commands
        [ConsoleCommand("bounty.changepage")]
        private void ccmdChangePage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, "bountynet.use"))
                return;

            switch (arg.Args[1])
            {
                case "add":
                    CreatePlayerSelectionMenu(player, arg.GetInt(0), 0, false, arg.Args[2]);                    
                    return;
                case "cancel":
                    CreateCancelBountyMenu(player, arg.GetInt(0), arg.GetInt(2));
                    return;
                case "claim":
                    CreateClaimMenu(player, arg.GetInt(0), arg.GetInt(2));
                    return;
                case "view":
                    CreateBountyViewMenu(player, arg.GetInt(0), arg.GetInt(2));
                    return;
                case "wanted":
                    CreateWantedMenu(player, arg.GetInt(0), arg.GetInt(2));
                    return;
                case "hunters":
                    CreateHuntersMenu(player, arg.GetInt(0), arg.GetInt(2));
                    return;                
                case "selection":                   
                    CreatePlayerSelectionMenu(player, arg.GetInt(0), arg.GetInt(2), arg.GetBool(3), arg.Args[4]);
                    return;
                case "home":
                    CreateConsoleWindow(player, arg.GetInt(0), 0);
                    return;                
            }
        }

        [ConsoleCommand("bounty.claimreward")]
        private void ccmdClaimReward(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, "bountynet.use"))
                return;
            
            RewardInfo rewardInfo;
            if (storedData.rewards.TryGetValue(arg.GetInt(2), out rewardInfo))
            {
                GivePlayerRewards(player, rewardInfo, true);
                storedData.rewards.Remove(arg.GetInt(2));
            }

            PlayerData playerData;
            if (storedData.players.TryGetValue(player.userID, out playerData))            
                playerData.unclaimedRewards.Remove(arg.GetInt(2));

            CreateClaimMenu(player, arg.GetInt(0), arg.GetInt(1));
        }

        [ConsoleCommand("bounty.addbounty")]
        private void ccmdAddNewBounty(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, "bountynet.use"))
                return;
            
            int terminalId = arg.GetInt(1);
            ulong targetId = arg.GetUInt64(2);

            PlayerData targetData;
            if (storedData.players.TryGetValue(targetId, out targetData))
            {
                if (targetData.GetBountyOf(player.userID) != null)
                {
                    PopupToPlayer(player, msg("UI.Add.HasBounty", player.userID));
                    return;
                }
            }

            if (configData.SetLimit > 0)
            {
                if (storedData.players.TryGetValue(player.userID, out targetData))
                {
                    if (targetData.GetTotalBounties() >= configData.SetLimit)
                    {
                        PopupToPlayer(player, string.Format(msg("UI.Add.Limit", player.userID), configData.SetLimit));
                        return;
                    }
                }
            }

            if (arg.Args[0] == "items")
            {
                CuiHelper.DestroyUi(player, RustNET.RustNET_Panel);

                SpawnItemContainer(player);
                if (bountyCreator.ContainsKey(player.userID))
                    bountyCreator[player.userID] = targetId;
                else bountyCreator.Add(player.userID, targetId);

                return;
            }

            bool isRp = arg.Args[0] == "rp";

            int amount = 0;
            if (arg.Args.Length >= 4)            
                amount = arg.GetInt(3);

            int available = isRp ? (int)ServerRewards?.Call("CheckPoints", player.userID) : Convert.ToInt32((double)Economics?.Call("Balance", player.UserIDString));

            if (amount > available)
                amount = available;
            if (amount < 0)
                amount = 0;
           
            if (arg.Args.Length == 5)
            {
                if (isRp)
                    ServerRewards?.Call("TakePoints", player.userID, amount);
                else Economics?.Call("Withdraw", player.UserIDString, (double)amount);

                CreateNewBounty(player, targetId, isRp ? amount : 0, !isRp ? amount : 0, null, true);
                CreateConsoleWindow(player, terminalId, 0);
                return;
            }

            CreateNewAmountMenu(player, terminalId, targetId, amount, arg.Args[0] == "rp");
        }

        [ConsoleCommand("bounty.cancelbounty")]
        private void ccmdClearBounty(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, "bountynet.use"))
                return;           

            PlayerData targetData;
            if (storedData.players.TryGetValue(arg.GetUInt64(2), out targetData))
            {
                PlayerData.BountyInfo bountyInfo = targetData.GetBountyOf(arg.GetUInt64(3));
                if (bountyInfo != null)
                {
                    RewardInfo rewardInfo = storedData.rewards[bountyInfo.rewardId];

                    if (!arg.GetBool(4))
                        GivePlayerRewards(player, rewardInfo, false);

                    storedData.rewards.Remove(bountyInfo.rewardId);
                    targetData.activeBounties.Remove(bountyInfo);

                    IPlayer target = covalence.Players.FindPlayerById(arg.GetUInt64(2).ToString());
                    if (target != null && target.IsConnected)
                    {
                        BasePlayer targetPlayer = target.Object as BasePlayer;
                        if (targetPlayer != null)
                        {
                            if (arg.GetBool(4))
                                BroadcastToPlayer(targetPlayer, string.Format(msg("Chat.Cancelled.Target.Admin", targetPlayer.userID), bountyInfo.initiatorName));
                            else BroadcastToPlayer(targetPlayer, string.Format(msg("Chat.Cancelled.Target", targetPlayer.userID), player.displayName));
                        }
                    }

                    if (arg.GetBool(4))
                        BroadcastToPlayer(player, string.Format(msg("Chat.Cancelled.Initiator.Admin", player.userID), target.Name));
                    PopupToPlayer(player, string.Format(msg("UI.Cancelled.Initiator", player.userID), target.Name));
                }
            }
                        
            if (arg.GetBool(4))
                CreateIndividualBountyView(player, arg.GetInt(0), arg.GetUInt64(2), 0);
            else CreateCancelBountyMenu(player, arg.GetInt(0), arg.GetInt(1));
        }
        
        [ConsoleCommand("bounty.viewbounties")]
        private void ccmdViewBounties(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !permission.UserHasPermission(player.UserIDString, "bountynet.use"))
                return;
          
            CreateIndividualBountyView(player, arg.GetInt(0), arg.GetUInt64(1), arg.GetInt(2));
        }
        #endregion

        #region UI Functions
        private float[] GetButtonPosition(int i)
        {           
            int rowNumber = i == 0 ? 0 : RowNumber(5, i);
            int columnNumber = i - (rowNumber * 5);

            float offsetX = 0.04f + ((0.01f + 0.176f) * columnNumber);
            float offsetY = (0.725f - (rowNumber * 0.04f));
            
            return new float[] { offsetX, offsetY, offsetX + 0.176f, offsetY + 0.03f };
        }

        private int RowNumber(int max, int count) => Mathf.FloorToInt(count / max);

        private void ClearBounties(BasePlayer player, int terminalId, int page, ulong targetId)
        {
            PlayerData playerData;
            if (storedData.players.TryGetValue(targetId, out playerData) || playerData.activeBounties.Count == 0)
            {
                foreach (var bounty in playerData.activeBounties)
                    storedData.rewards.Remove(bounty.rewardId);
                playerData.activeBounties.Clear();
            }
            CreateBountyViewMenu(player, terminalId, page);
        }
        #endregion

        #region Commands  
        [ConsoleCommand("bounty")]
        private void ccmdBounty(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "bounty view <target name or ID> - View active bounties on the specified player");
                SendReply(arg, "bounty top - View the top 20 bounty hunters");
                SendReply(arg, "bounty wanted - View the top 20 most wanted players");
                SendReply(arg, "bounty clear <target name or ID> - Clear all active bounties on the specified player");
                SendReply(arg, "bounty wipe - Wipe all bounty data");
                return;
            }

            switch (arg.Args[0].ToLower())
            {
                case "view":
                    {
                        if (arg.Args.Length < 2)
                        {
                            SendReply(arg, "Invalid command syntax! Type 'bounty' to see available commands");
                            return;
                        }

                        IPlayer targetPlayer = covalence.Players.FindPlayer(arg.Args[1]);
                        if (targetPlayer == null)
                        {
                            SendReply(arg, "Unable to find a player with that name or ID");
                            return;
                        }

                        PlayerData playerData;
                        if (!storedData.players.TryGetValue(ulong.Parse(targetPlayer.Id), out playerData) || playerData.activeBounties.Count == 0)
                        {
                            SendReply(arg, "That player does not have any active bounties");
                            return;
                        }

                        SendReply(arg, string.Format("{0} has {1} active bounties", targetPlayer.Name, playerData.activeBounties.Count));
                        foreach (var bounty in playerData.activeBounties)
                        {
                            RewardInfo rewardInfo = storedData.rewards[bounty.rewardId];
                            string reward = string.Empty;
                            if (rewardInfo.rewardItems.Count > 1)
                            {
                                for (int i = 0; i < rewardInfo.rewardItems.Count; i++)
                                {
                                    RewardInfo.ItemData itemData = rewardInfo.rewardItems.ElementAt(i);
                                    reward += (string.Format("{0}x {1}", itemData.amount, idToDisplayName[itemData.itemid]) + (i < rewardInfo.rewardItems.Count - 1 ? ", " : ""));
                                }
                            }
                            else reward = rewardInfo.econAmount > 0 ? string.Format("{0} economics", rewardInfo.econAmount) : string.Format("{0} rp", rewardInfo.rpAmount);

                            SendReply(arg, string.Format("Placed by {0} {1} ago. Reward: {2}", bounty.initiatorName, FormatTime(CurrentTime() - bounty.initiatedTime), reward));
                        }
                    }
                    return;
                case "top":
                    IEnumerable<PlayerData> top20Hunters = storedData.players.Values.OrderByDescending(x => x.bountiesClaimed).Take(20);
                    string hunterMessage = "Top 20 Hunters:";

                    foreach (PlayerData playerData in top20Hunters)
                        hunterMessage += string.Format("\n{0} - {1} bounties collected", playerData.displayName, playerData.bountiesClaimed);

                    SendReply(arg, hunterMessage);
                    return;
                case "wanted":
                    IEnumerable<PlayerData> top20Hunted = storedData.players.Values.OrderByDescending(x => x.totalWantedTime + x.GetCurrentWantedTime()).Take(20);
                    string wantedMessage = "Top 20 Most Wanted:";

                    foreach (PlayerData playerData in top20Hunted)
                        wantedMessage += string.Format("\n{0} has all together been on the run for {1} with a total of {2} bounties", playerData.displayName, FormatTime(playerData.totalWantedTime + playerData.GetCurrentWantedTime()), playerData.totalBounties);

                    SendReply(arg, wantedMessage);
                    return;
                case "clear":
                    {
                        if (arg.Args.Length < 2)
                        {
                            SendReply(arg, "Invalid command syntax! Type 'bounty' to see available commands");
                            return;
                        }

                        IPlayer targetPlayer = covalence.Players.FindPlayer(arg.Args[1]);
                        if (targetPlayer == null)
                        {
                            SendReply(arg, "Unable to find a player with that name or ID");
                            return;
                        }

                        PlayerData playerData;
                        if (!storedData.players.TryGetValue(ulong.Parse(targetPlayer.Id), out playerData) || playerData.activeBounties.Count == 0)
                        {
                            SendReply(arg, "That player does not have any active bounties");
                            return;
                        }

                        foreach (var bounty in playerData.activeBounties)
                            storedData.rewards.Remove(bounty.rewardId);
                        playerData.activeBounties.Clear();

                        SendReply(arg, $"You have cleared all pending bounties from {targetPlayer.Name}");
                    }
                    return;
                case "wipe":
                    storedData = new StoredData();
                    SaveData();
                    SendReply(arg, "All data has been wiped!");
                    return;
                default:
                    SendReply(arg, "Invalid command syntax! Type 'bounty' to see available commands");
                    break;
            }
        }
        #endregion        

        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Ignore kills by clan members")]
            public bool IgnoreClans { get; set; }
            [JsonProperty(PropertyName = "Ignore kills by friends")]
            public bool IgnoreFriends { get; set; }
            [JsonProperty(PropertyName = "Set a limit of how many active bounties a player can have at any time (set to 0 to disable the limit)")]
            public int SetLimit { get; set; }
            [JsonProperty(PropertyName = "Notification Options")]
            public NotificationOptions Notifications { get; set; }
            [JsonProperty(PropertyName = "Reward Options")]
            public RewardOptions Rewards { get; set; }

            public class NotificationOptions
            {
                [JsonProperty(PropertyName = "PopupNotifications - Broadcast using PopupNotifications")]
                public bool UsePopupNotifications { get; set; }
                [JsonProperty(PropertyName = "PopupNotifications - Duration of notification")]
                public float PopupDuration { get; set; }
                [JsonProperty(PropertyName = "Broadcast new bounties globally")]
                public bool BroadcastNewBounties { get; set; }
                [JsonProperty(PropertyName = "Reminders - Remind targets they have a bounty on them")]
                public bool ShowReminders { get; set; }
                [JsonProperty(PropertyName = "Reminders - Amount of time between reminders (in minutes)")]
                public int ReminderTime { get; set; }
            }

            public class RewardOptions
            {
                [JsonProperty(PropertyName = "Allow bounties to be placed using Economics")]
                public bool AllowEconomics { get; set; }
                [JsonProperty(PropertyName = "Allow bounties to be placed using RP")]
                public bool AllowServerRewards { get; set; }
                [JsonProperty(PropertyName = "Allow bounties to be placed using items")]
                public bool AllowItems { get; set; }
            }
            [JsonProperty(PropertyName = "Bounty icon URL for RustNET menu")]
            public string RustNETIcon { get; set; }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                IgnoreClans = true,
                IgnoreFriends = true,
                SetLimit = 0,
                Notifications = new ConfigData.NotificationOptions
                {
                    BroadcastNewBounties = true,
                    PopupDuration = 8f,
                    ReminderTime = 30,
                    ShowReminders = true,
                    UsePopupNotifications = false
                },
                Rewards = new ConfigData.RewardOptions
                {
                    AllowEconomics = true,
                    AllowItems = true,
                    AllowServerRewards = true
                },
                RustNETIcon = "https://www.chaoscode.io/oxide/Images/RustNET/bountyicon.png",
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 1, 5))
                configData.SetLimit = 0;
            
            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management        
        private void SaveData()
        {
            data.WriteObject(storedData);
            offline.WriteObject(offlinePlayers);
        }

        private void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
            try
            {
                offlinePlayers = offline.ReadObject<OfflinePlayers>();
            }
            catch
            {
                offlinePlayers = new OfflinePlayers();
            }
        }

        private class StoredData
        {
            public Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();
            public Dictionary<int, RewardInfo> rewards = new Dictionary<int, RewardInfo>();
        }

        private class PlayerData
        {
            public string displayName;
            public int totalBounties;
            public int bountiesClaimed;
            public double totalWantedTime;
            public List<BountyInfo> activeBounties = new List<BountyInfo>();
            public List<int> unclaimedRewards = new List<int>();

            public PlayerData() { }

            public PlayerData(string displayName)
            {
                this.displayName = displayName;
            }

            public void ClaimRewards(List<int> rewards)
            {
                foreach (int reward in rewards)
                {
                    unclaimedRewards.Add(reward);
                    bountiesClaimed++;
                }
            }

            public BountyInfo GetBountyOf(ulong initiatorId)
            {
                foreach(BountyInfo bountyInfo in activeBounties)
                {
                    if (bountyInfo.initiatorId == initiatorId)
                        return bountyInfo;
                }
                return null;
            }

            public int GetTotalBounties() => activeBounties.Count;

            public void UpdateWantedTime()
            {
                totalWantedTime += GetCurrentWantedTime();
            }

            public double GetCurrentWantedTime()
            {
                double largestTime = 0;
                foreach (BountyInfo bountyInfo in activeBounties)
                {
                    double time = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds - bountyInfo.initiatedTime;
                    if (time > largestTime)
                        largestTime = time;
                }
                return largestTime;
            }

            public class BountyInfo
            {
                public ulong initiatorId;
                public string initiatorName;
                public double initiatedTime;
                public int rewardId;

                public BountyInfo() { }
                public BountyInfo(ulong initiatorId, string initiatorName, int rewardId)
                {
                    this.initiatorId = initiatorId;
                    this.initiatorName = initiatorName;
                    this.rewardId = rewardId;
                    this.initiatedTime = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                }
            }
        }

        private class RewardInfo
        {
            public int rpAmount;
            public int econAmount;
            public List<ItemData> rewardItems = new List<ItemData>();

            public RewardInfo() { }
            public RewardInfo(int rpAmount, int econAmount, ItemContainer container)
            {
                this.rpAmount = rpAmount;
                this.econAmount = econAmount;
                if (container != null)
                    rewardItems = GetItems(container).ToList();
            }

            public string GetRewardString(ulong playerId)
            {
                string reward = string.Empty;
                if (rewardItems.Count > 0)
                {
                    Dictionary<int, int> items = new Dictionary<int, int>();
                    foreach (ItemData itemData in rewardItems)
                    {
                        if (!items.ContainsKey(itemData.itemid))
                            items.Add(itemData.itemid, itemData.amount);
                        else items[itemData.itemid] += itemData.amount;
                    }

                    for (int i = 0; i < items.Count; i++)
                    {
                        KeyValuePair<int, int> item = items.ElementAt(i);
                        reward += (string.Format(ins.msg("UI.Reward.Item", playerId), item.Value, ins.idToDisplayName[item.Key]) + (i < items.Count - 1 ? ", " : ""));
                    }
                }
                else reward = econAmount > 0 ? string.Format(ins.msg("UI.Reward.Econ", playerId), econAmount) : string.Format(ins.msg("UI.Reward.RP", playerId), rpAmount);
                return reward;
            }

            private IEnumerable<ItemData> GetItems(ItemContainer container)
            {
                return container.itemList.Select(item => new ItemData
                {
                    itemid = item.info.itemid,
                    amount = item.amount,
                    ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                    ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                    skin = item.skin,
                    condition = item.condition,
                    instanceData = new ItemData.InstanceData(item),
                    contents = item.contents?.itemList.Select(item1 => new ItemData
                    {
                        itemid = item1.info.itemid,
                        amount = item1.amount,
                        condition = item1.condition
                    }).ToArray()
                });
            }

            public class ItemData
            {
                public int itemid;
                public ulong skin;
                public int amount;
                public float condition;
                public int ammo;
                public string ammotype;
                public InstanceData instanceData;
                public ItemData[] contents;

                public class InstanceData
                {
                    public int dataInt;
                    public int blueprintTarget;
                    public int blueprintAmount;

                    public InstanceData() { }
                    public InstanceData(Item item)
                    {
                        if (item.instanceData == null)
                            return;

                        dataInt = item.instanceData.dataInt;
                        blueprintAmount = item.instanceData.blueprintAmount;
                        blueprintTarget = item.instanceData.blueprintTarget;
                    }

                    public void Restore(Item item)
                    {
                        item.instanceData = new ProtoBuf.Item.InstanceData();
                        item.instanceData.blueprintAmount = blueprintAmount;
                        item.instanceData.blueprintTarget = blueprintTarget;
                        item.instanceData.dataInt = dataInt;
                    }
                }
            }
        }

        private class OfflinePlayers
        {
            public Hash<string, double> offlinePlayers = new Hash<string, double>();

            public void AddOfflinePlayer(string userId) => offlinePlayers[userId] = CurrentTime();

            public void OnPlayerInit(string userId)
            {
                if (offlinePlayers.ContainsKey(userId))
                    offlinePlayers.Remove(userId);
            }

            public void RemoveOldPlayers()
            {
                double currentTime = CurrentTime();

                for (int i = offlinePlayers.Count - 1; i >= 0; i--)
                {
                    var user = offlinePlayers.ElementAt(i);
                    if (currentTime - user.Value > 604800)
                        offlinePlayers.Remove(user);
                }
            }

            public IPlayer[] GetOfflineList() => ins.covalence.Players.All.Where(x => offlinePlayers.ContainsKey(x.Id)).ToArray();

            public double CurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }
        #endregion

        #region Localization
        private string msg(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId == 0U ? null : playerId.ToString());

        Dictionary<string, string> messages = new Dictionary<string, string>
        {
            ["UI.Add.HasBounty"] = "You already have a active bounty on that player",
            ["UI.Add.PlacedInitiator"] = "You have successfully placed a bounty on <color=#28ffa6>{0}</color>",
            ["UI.Add.Confirm"] = "> <color=#28ffa6>CONFIRM</color> <",
            ["UI.Add.Limit"] = "You can only have <color=#28ffa6>{0}</color> active bounties at any given time",
            ["UI.Claim.NoRewardsPending"] = "You do not have any pending rewards to claim",
            ["UI.Claim.ClaimReward"] = "> CLAIM <",
            ["UI.View.PlacedBy"] = "> Placed By",
            ["UI.View.TimeActive"] = "Time Active",
            ["UI.View.Reward"] = "Reward",
            ["UI.View.Cancel"] = "> CANCEL <",
            ["UI.View.AdminCancel"] = "Admin Cancel",
            ["UI.View.ActiveBounties"] = " - Active bounties <",
            ["UI.View.ActiveBountiesPlayer"] = " - Active bounties for {0} <",
            ["UI.ViewBounties"] = "> VIEW <",
            ["UI.View.NoActiveBounties"] = "There are currently no active bounties",
            ["UI.View.NoActiveBountiesPlayer"] = "There are currently no active bounties for this player",
            ["UI.Cancel.NoneSet"] = "You have not set any bounties",
            ["UI.Hunters.ClaimedBounties"] = "Bounties Claimed",
            ["UI.Hunters.NoHunters"] = "No bounties have been claimed yet",
            ["UI.View.Name"] = "> Player Name",
            ["UI.Wanted.CurrentTime"] = "Current Wanted Time",
            ["UI.Wanted.TotalTime"] = "Total Wanted Time",
            ["UI.Wanted.TotalBounties"] = "Total Bounties",
            ["UI.Wanted.ActiveBounties"] = "Active Bounties",
            ["UI.Wanted.NoWantedPlayers"] = "There are currently no wanted players",
            ["UI.Menu.Bounty"] = "> <color=#28ffa6>RustNET Bounty Network</color>",
            ["UI.Menu.NoPermission"] = "You do not have permission to access the bounty menu",
            ["UI.Menu.AddItems"] = " > Create Bounty (items)",
            ["UI.Menu.AddRP"] = " > Create Bounty (RP)",
            ["UI.Menu.AddEco"] = " > Create Bounty (Economics)",
            ["UI.Menu.Cancel"] = " > Cancel Bounty",
            ["UI.Menu.Claim"] = " > Claim Rewards",
            ["UI.Menu.View"] = " > Active Bounties",
            ["UI.Menu.Wanted"] = " > Most Wanted",
            ["UI.Menu.Hunters"] = " > Top Hunters",            
            ["UI.SelectPlayer"] = " - Select a player <",
            ["UI.Tip.Hunters"] = " - Top bounty hunters <",
            ["UI.Tip.Wanted"] = " - Most wanted players <",
            ["UI.Tip.Cancel"] = " - Cancel active bounty <",
            ["UI.Tip.Claim"] = " - Claim a bounty reward <",
            ["UI.Tip.Create"] = " - Create a new bounty <",
            ["UI.Tip.SelectAmount"] = " - Select an amount <",
            ["UI.Tip.SelectAmountRP"] = "Select an amount of RP reward for your bounty on {0}",
            ["UI.Tip.SelectAmountEco"] = "Select an amount of Economics reward for your bounty on {0}",           
            ["UI.Selection.Offline"] = "> <color=#28ffa6>VIEW ONLINE PLAYERS</color> <",
            ["UI.Selection.Online"] = "> <color=#28ffa6>VIEW OFFLINE PLAYERS</color> <",
            ["UI.Reward.Econ"] = "${0}",
            ["UI.Reward.RP"] = "{0} RP",
            ["UI.Reward.Item"] = "{0} x {1}",
            ["UI.Reward.Claimed"] = "Rewards have been claimed!",
            ["Chat.RewardPending"] = "<color=#D3D3D3><color=#ce422b>{0}</color> had <color=#ce422b>{1}</color> outstanding bounties on them. You can claim your rewards by accessing the bounty menu via a </color> <color=#ce422b>RustNET</color> terminal",
            ["Chat.IsFriend1"] = "<color=#D3D3D3>You cannot claim a bounty on a current or recent friend, clan mate, or team member</color>",
            ["Chat.PlacedTarget"] = "<color=#ce422b>{0} </color><color=#D3D3D3>has placed a bounty on you</color>",
            ["Chat.PlacedInitiator"] = "<color=#D3D3D3>You have successfully placed a bounty on</color> <color=#ce422b>{0}</color>",
            ["Chat.PlacedGlobal"] = "<color=#ce422b>{0} <color=#D3D3D3>has placed a bounty on</color> {1}</color>",
            ["Chat.Cancelled.Target"] = "<color=#ce422b>{0} </color><color=#D3D3D3>has cancelled their bounty on you</color>",
            ["Chat.Cancelled.Target.Admin"] = "<color=#D3D3D3>A admin has cancelled <color=#ce422b>{0}</color>'s bounty on you</color>",
            ["UI.Cancelled.Initiator"] = "You have cancelled the bounty on <color=#28ffa6>{0}</color>",
            ["Chat.Cancelled.Initiator.Admin"] = "<color=#D3D3D3>A admin has cancelled your bounty on</color> <color=#ce422b>{0}</color>",
            ["Chat.NoItemsDeposited"] = "<color=#D3D3D3>You did not place any items in the box</color>",
            ["Chat.OutstandingBounties"] = "<color=#D3D3D3>You have <color=#ce422b>{0}</color> active bounties on you!</color>",
            ["UI.Help.Title"] = "> <color=#28ffa6>Bounty Help Menu</color> <",
            ["UI.Help"] = "> Using BountyNET.\n\nYou can create a bounty using one of 3 types of rewards (depending on what is allowed on the server).\nCreating a bounty with items will allow you to place items into a box. These items will be the reward for the player who completes the bounty.\nCreating a bounty using RP or Economics will issue the amount you specify as the reward.\n> NOTE: Items and currency will be deducted when you place the bounty\n\nYou can cancel an active bounty at any time by clicking 'Cancel Bounty' and selecting the bounty you wish to cancel\n\nClaim your rewards for killing a wanted player by click 'Claim Rewards' and selecting the reward you wish to claim. If it is items be sure you have enough inventory space before claiming!\n\n'Active Bounties' is a list of currently active bounties, you can view the rewards offered for these bounties from this menu\n\n'Most Wanted' is a leaderboard of the players who have evaded death for the longest period of time\n\n'Top Hunters' is a leaderboard of players who have claimed the most bounties",
        };
        #endregion
    }
}
