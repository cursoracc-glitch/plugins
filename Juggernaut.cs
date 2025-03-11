using System;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Rust;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Juggernaut", "k1lly0u", "0.2.43", ResourceId = 0)]
    internal class Juggernaut : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Clans, Friends, LustyMap, EventManager, ServerRewards, Economics;

        private static Juggernaut ins;

        private StoredData storedData;
        private RestoreData restoreData;
        private DynamicConfigFile data, restorationData;

        private BasePlayer juggernaut = null;
        private BaseEntity[] finalSphere = null;
        private Vector3 endPos = Vector3.zero;

        private Timer nextMatch;
        private Timer startMatch;
        private Timer eventTimer;
        private Timer broadcastTimer;
        private Timer openMessage;
        private Timer uiTimer;
        private double eventEnd;
        private double nextTrigger;

        private string juggernautIcon;

        private bool isOpen;
        private bool hasStarted;
        private bool initialized;
        private bool hasDestinations;

        private static bool isUnloading;

        private List<ulong> optedIn = new List<ulong>();
        private Dictionary<ulong, Destinations> destinationCreator = new Dictionary<ulong, Destinations>();

        private const string sphereEnt = "assets/prefabs/visualization/sphere.prefab";
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission("juggernaut.canbe", this);
            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("juggernaut_data");
            restorationData = Interface.Oxide.DataFileSystem.GetFile("juggernaut_restoration_data");
            isUnloading = false;
        }

        private void OnServerInitialized()
        {
            ins = this;
            LoadData();

            if (storedData.destinations.Count > 0)
            {
                hasDestinations = true;
                StartEventTimer();
            }
            else PrintWarning("No destinations have been set! Unable to start the event");

            RemoveMapMarker(msg("juggernaut"), false);
            RemoveMapMarker(msg("possibleDest"), true);
            RemoveMapMarker(msg("destination"), false);

            Unsubscribe(nameof(CanNetworkTo));

            initialized = true;
        }

        private void OnServerSave() => SaveRestoreData();

        private void Unload()
        {
            isUnloading = true;

            if (eventTimer != null)
                eventTimer.Destroy();
            if (nextMatch != null)
                nextMatch.Destroy();
            if (startMatch != null)
                startMatch.Destroy();
            if (openMessage != null)
                openMessage.Destroy();
            if (uiTimer != null)
                uiTimer.Destroy();
            if (broadcastTimer != null)
                broadcastTimer.Destroy();

            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Main);
            CuiHelper.DestroyUi(juggernaut, Compass);

            if (juggernaut != null && hasStarted)
            {
                UnlockInventory(juggernaut);
                StripInventory(juggernaut);
                DestroyEvent();
            }

            SaveRestoreData();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => OnPlayerInit(player));
                return;
            }
            if (restoreData.HasRestoreData(player.userID))
                restoreData.RestorePlayer(player);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (optedIn.Contains(player.userID))
                optedIn.Remove(player.userID);
            if (player == juggernaut && hasStarted)
            {                
                UnlockInventory(player);
                StripInventory(player);
                DestroyEvent();
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;

            var victim = entity.ToPlayer();
            var attacker = info.InitiatorPlayer;
            if (victim != null && victim == juggernaut)
            {
                if (info.Initiator != null && info.Initiator.ShortPrefabName.Equals("beartrap") && configData.Juggernaut.NoBeartrapDamage)
                {
                    info.damageTypes.ScaleAll(0);
                    return;
                }
                if (info.WeaponPrefab != null && info.WeaponPrefab.ShortPrefabName.Equals("landmine") && configData.Juggernaut.NoLandmineDamage)
                {
                    info.damageTypes.ScaleAll(0);
                    return;
                }
                if (info.damageTypes.GetMajorityDamageType() == DamageType.Fall && configData.Juggernaut.NoFallDamage)
                {
                    info.damageTypes.ScaleAll(0);
                    return;
                }
                if (attacker != null)
                {
                    if (IsFriend(victim.userID, attacker.userID))
                    {
                        info.damageTypes.ScaleAll(0);
                        SendReply(attacker, msg("ff1", attacker.UserIDString));
                        return;
                    }
                    if (IsClanmate(victim.userID, attacker.userID))
                    {
                        info.damageTypes.ScaleAll(0);
                        SendReply(attacker, msg("ff2", attacker.UserIDString));
                        return;
                    }                    
                }                
                info.damageTypes.ScaleAll(configData.Juggernaut.DefenseMod);
                return;
            }
            
            if (attacker == null) return;
            if (attacker == juggernaut)
            {
                if (!configData.Juggernaut.DamageStructures && (entity is BuildingBlock || entity is SimpleBuildingBlock))
                {
                    info.damageTypes.ScaleAll(0);
                    return;
                }

                info.damageTypes.ScaleAll(configData.Juggernaut.AttackMod);
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!initialized || entity == null) return;
            var player = entity as BasePlayer;
            if (player != null)
            {
                TryRestorePlayer(player);
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;

            var victim = entity.ToPlayer();
            if (victim == null) return;
            if (victim == juggernaut)
            {                
                if (!configData.Prizes.Inventory)
                    StripInventory(victim);

                var attacker = info.InitiatorPlayer;
                if (attacker != null)
                {
                    if (victim == info.InitiatorPlayer)
                    {
                        StripInventory(victim);
                        PrintToChat(msg("juggerSuicide"));
                    }
                    else if (!IsFriend(victim.userID, attacker.userID) && !IsClanmate(victim.userID, attacker.userID))
                    {
                        PrintToChat(msg("juggerDead"));
                        if (configData.Prizes.Amount > 0)
                        {
                            if (configData.Prizes.Economics)
                                Economics?.Call("Deposit", attacker.userID, (double)configData.Prizes.Amount);
                            if (configData.Prizes.ServerRewards)
                                ServerRewards?.Call("AddPoints", attacker.userID, configData.Prizes.Amount);
                        }
                    }
                }
                else PrintToChat(msg("juggerDead"));

                UnlockInventory(victim);
                DestroyEvent();
            }
        }

        private object CanNetworkTo(BaseEntity entity, BasePlayer target)
        {
            if (entity == null || finalSphere == null || juggernaut == null || target == null) return null;
            if (finalSphere.Contains(entity))
            {
                if (target != juggernaut)
                    return false;
            }
            return null;
        }

        private object OnRunCommand(ConsoleSystem.Arg arg)
        {
            if (!hasStarted || arg.Connection == null || arg.Connection.player == null || arg.cmd.Name != "kill") return null;
            var player = arg.Connection.player as BasePlayer;
            if (player == null) return null;
            if (player == juggernaut)
            {
                SendReply(player, msg("noSuicide", player.UserIDString));
                return true;
            }      
            return null;
        }

        private object OnPlayerCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();

            if (player != null && juggernaut != null && player == juggernaut)
            {
                string text = arg.GetString(0, "text").ToLower();

                if (text.Length > 0 && text[0] == '/' && arg.cmd.FullName == "chat.say")
                {
                    if (configData.Game.CommandBlacklist.Any(entry => entry.StartsWith("/") ? text.StartsWith(entry) : text.Substring(1).StartsWith(entry)))
                    {
                        SendReply(player, msg("blacklistcmd", player.UserIDString));
                        return false;
                    }
                }
            }

            return null;
        }
        #endregion

        #region Functions
        private void LockInventory(BasePlayer player)
        {
            if (!player.inventory.containerMain.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerMain.SetFlag(ItemContainer.Flag.IsLocked, true);
            if (!player.inventory.containerBelt.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerBelt.SetFlag(ItemContainer.Flag.IsLocked, true);
            if (!player.inventory.containerWear.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, true);

            player.inventory.SendSnapshot();
        }

        private void UnlockInventory(BasePlayer player)
        {
            if (player.inventory.containerMain.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerMain.SetFlag(ItemContainer.Flag.IsLocked, false);
            if (player.inventory.containerBelt.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerBelt.SetFlag(ItemContainer.Flag.IsLocked, false);
            if (player.inventory.containerWear.HasFlag(ItemContainer.Flag.IsLocked))
                player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, false);

            player.inventory.SendSnapshot();
        }

        private void StartEventTimer()
        {
            optedIn.Clear();
            UnsubscribeHooks();
            nextMatch = timer.Once(configData.Timers.Interval, CheckConditions);
            nextTrigger = GrabCurrentTime() + configData.Timers.Interval;
        }

        private void CheckConditions()
        {
            if (BasePlayer.activePlayerList.Count >= configData.Conditions.OpenMin)
                OpenEvent();
        }

        private void OpenEvent()
        {
            if (string.IsNullOrEmpty(juggernautIcon) && configData.UI.ShowUITimer && !string.IsNullOrEmpty(configData.UI.IconUrl))
                Add(configData.UI.IconUrl);

            isOpen = true;
            startMatch = timer.Once(configData.Timers.Open, StartEvent);
            openMessage = timer.Repeat(45, 0, () => PrintToChat(msg("eventOpen")));
            nextTrigger = configData.Timers.Open + GrabCurrentTime();
            PrintToChat(msg("eventOpen"));
        }

        private void StartEvent()
        {
            isOpen = false;
            if (openMessage != null) openMessage.Destroy();

            if (optedIn.Count == 0)
            {
                PrintToChat(msg("noEntrants"));
                StartEventTimer();
                return;
            }
            var minEntrants = (int)Math.Round(BasePlayer.activePlayerList.Count * configData.Conditions.Percentage, 0);
            if (optedIn.Count < minEntrants)
            {
                PrintToChat(string.Format(msg("notEnoughEntrants"), $"{configData.Conditions.Percentage * 100}%"));
                StartEventTimer();
                return;
            }

            SubscribeHooks();

            var player = GetRandomPlayer();
            if (player == null)
            {
                StartEventTimer();
                return;
            }
            
            Destinations destinations = storedData.destinations.GetRandom();

            endPos = new Vector3(destinations.x2, destinations.y2, destinations.z2);

            restoreData.AddData(player);
            StripInventory(player);

            MovePosition(player, new Vector3(destinations.x1, destinations.y1, destinations.z1));
            PrepareJuggernaut(player);

            Subscribe(nameof(CanNetworkTo));
            CreateSphere();

            eventEnd = configData.Timers.Completion + GrabCurrentTime();
            nextTrigger = configData.Timers.Completion + GrabCurrentTime();

            hasStarted = true;
        }

        private BasePlayer GetRandomPlayer(int tries = 0)
        {
            if (tries > 4) return null;

            ulong playerId = optedIn.GetRandom();
            BasePlayer player = BasePlayer.activePlayerList.FirstOrDefault(x => x.userID == playerId);
            if (player == null || player.IsSleeping() || player.IsDead())
            {
                optedIn.Remove(playerId);
                if (optedIn.Count == 0)
                    return null;
                return GetRandomPlayer(++tries);
            }
            return player;
        }

        private void PrepareJuggernaut(BasePlayer player)
        {
            if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => PrepareJuggernaut(player));
                return;
            }
            juggernaut = player;

            if (configData.Juggernaut.ResetMetabolism)
            {
                player.metabolism.Reset();
                player.health = player.MaxHealth();
                player.metabolism.hydration.value = player.metabolism.hydration.max;
                player.metabolism.calories.value = player.metabolism.calories.max;
                player.metabolism.SendChangesToClient();
            }

            GiveJuggernautGear();
            PrintToChat(string.Format(msg("eventStart"), $"X:{Math.Round(endPos.x, 1)}, Z:{Math.Round(endPos.z, 1)}"));
            SendReply(player, string.Format(msg("juggerStart", player.UserIDString), $"X:{Math.Round(endPos.x, 1)}, Z:{Math.Round(endPos.z, 1)}"));

            eventTimer = timer.In(configData.Timers.Completion, EventCancel);
            if (configData.UI.ShowUITimer)
                RefreshAllUI();
            
            if (configData.Lusty.Enabled)
            {
                if (configData.Lusty.DestinationAmount > 1 && storedData.destinations.Count >= configData.Lusty.DestinationAmount)
                {
                    List<Destinations> destinations = new List<Destinations>(storedData.destinations);
                    int randomNumber = UnityEngine.Random.Range(1, configData.Lusty.DestinationAmount);
                    for (int i = 0; i < configData.Lusty.DestinationAmount; i++)
                    {
                        if (i == randomNumber)
                        {
                            AddMapMarker(endPos.x, endPos.z, $"{msg("possibleDest")} {i + 1}");
                        }
                        else
                        {
                            Destinations destination = destinations.GetRandom();
                            destinations.Remove(destination);
                            AddMapMarker(destination.x2, destination.z2, $"{msg("possibleDest")} {i + 1}");
                        }                        
                    }
                }
                else AddMapMarker(endPos.x, endPos.z, msg("destination"));
            }  
            if (configData.Game.BroadcastEvery > 0) 
                MapIconUpdater();
        }

        private void MapIconUpdater()
        {
            broadcastTimer = timer.Repeat(configData.Game.BroadcastEvery, 0, () =>
            {
                if (juggernaut != null)
                {
                    if (configData.Game.BroadcastEveryLM)
                    {
                        AddMapMarker(juggernaut.transform.position.x, juggernaut.transform.position.z, msg("juggernaut"));
                        timer.In(configData.Game.BroadcastLMSeconds, () => RemoveMapMarker(msg("juggernaut"), false));
                    }
                    else PrintToChat(string.Format(msg("positionBroadcast"), Math.Round(juggernaut.transform.position.x, 1), Math.Round(juggernaut.transform.position.z, 1)));
                }
            });
        }

        private void EventCancel()
        {           
            PrintToChat(msg("eventCancel"));
            TryRestorePlayer(juggernaut);
            DestroyEvent();
        }

        private void EventWin()
        {            
            PrintToChat(msg("juggerWin"));

            if (configData.Prizes.Inventory)
            {
                var remainingItems = new List<RestoreData.ItemData>();               
                remainingItems.AddRange(GetItems(juggernaut.inventory.containerBelt));
                remainingItems.AddRange(GetItems(juggernaut.inventory.containerMain));
                remainingItems.AddRange(GetItems(juggernaut.inventory.containerWear));

                if (!storedData.winnerRewards.ContainsKey(juggernaut.userID))
                    storedData.winnerRewards.Add(juggernaut.userID, remainingItems);
                else storedData.winnerRewards[juggernaut.userID] = remainingItems;
            }
            if (configData.Prizes.ServerRewards || configData.Prizes.Economics)
            {
                if (!storedData.winnerMoney.ContainsKey(juggernaut.userID))
                    storedData.winnerMoney.Add(juggernaut.userID, new List<Points>());
                storedData.winnerMoney[juggernaut.userID].Add(new Points
                {
                    isRp = configData.Prizes.ServerRewards,
                    amount = configData.Prizes.Amount
                });
            }

            TryRestorePlayer(juggernaut);
            DestroyEvent();
        }

        private void DestroyEvent()
        {
            hasStarted = false;

            if (eventTimer != null)
                eventTimer.Destroy();
            if (uiTimer != null)
                uiTimer.Destroy();
            if (broadcastTimer != null)
                broadcastTimer.Destroy();            

            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Main);
            CuiHelper.DestroyUi(juggernaut, Compass);

            juggernaut = null;

            if (finalSphere != null)
            {
                if (finalSphere[0].GetComponent<FinalSphere>())
                    UnityEngine.Object.Destroy(finalSphere[0].GetComponent<FinalSphere>());

                for (int i = 0; i < finalSphere.Length; i++)                
                    finalSphere[i].KillMessage();                
            }

            Unsubscribe(nameof(CanNetworkTo));

            SaveData();

            RemoveMapMarker(msg("juggernaut"), false);
            RemoveMapMarker(msg("possibleDest"), true);
            RemoveMapMarker(msg("destination"), false);
            
            StartEventTimer();
        }

        private void TryRestorePlayer(BasePlayer player)
        {
            if (player == null) return;
            if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.In(1, () => TryRestorePlayer(player));
                return;
            }

            UnlockInventory(player);

            if (restoreData.HasRestoreData(player.userID))            
                restoreData.RestorePlayer(player);  
        }

        private void GiveJuggernautGear()
        {
            foreach (var entry in configData.Juggernaut.Inventory)
            {
                Item item = null;
                if (entry.IsBP)
                {
                    item = ItemManager.CreateByItemID(-1887162396, entry.Amount, entry.SkinID);
                    item.blueprintTarget = ItemManager.itemList.Find(x => x.shortname == entry.Shortname)?.itemid ?? 0;
                }
                else item = ItemManager.CreateByName(entry.Shortname, entry.Amount, entry.SkinID);

                if (item == null)                
                    PrintError($"Error creating item: {entry.Shortname}. Check this is a correct shortname!");                
                else item.MoveToContainer(entry.Container == "wear" ? juggernaut.inventory.containerWear : entry.Container == "belt" ? juggernaut.inventory.containerBelt : juggernaut.inventory.containerMain);
            }
            LockInventory(juggernaut);
        }

        private void UnsubscribeHooks()
        {
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnEntityDeath));
            Unsubscribe(nameof(CanNetworkTo));
        }

        private void SubscribeHooks()
        {
            Subscribe(nameof(OnEntityTakeDamage));
            Subscribe(nameof(OnEntityDeath));
            Subscribe(nameof(CanNetworkTo));
        }

        private double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        private string GetGridString(Vector3 position)
        {
            Vector2 adjPosition = new Vector2((World.Size / 2) + position.x, (World.Size / 2) - position.z);
            return $"{NumberToString((int)(adjPosition.x / 145))}{((int)(adjPosition.y / 145)) - 1}";
        }

        private string NumberToString(int number)
        {
            bool a = number > 26;
            Char c = (Char)(65 + (a ? number - 26 : number));
            return a ? "A" + c : c.ToString();
        }
        #endregion

        #region Teleportation Management  
        private void MovePosition(BasePlayer player, Vector3 destination)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
            StartSleeping(player);
            player.MovePosition(destination);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

        private void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }
        #endregion

        #region Player Saving and Restoration
        public static void StripInventory(BasePlayer player)
        {
            Item[] allItems = player.inventory.AllItems();

            for (int i = allItems.Length - 1; i >= 0; i--)
            {
                Item item = allItems[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }

        private IEnumerable<RestoreData.ItemData> GetItems(ItemContainer container)
        {
            return container.itemList.Select(item => new RestoreData.ItemData
            {
                itemid = item.info.itemid,
                amount = item.amount,
                ammo = item.GetHeldEntity() is BaseProjectile ? (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents : item.GetHeldEntity() is FlameThrower ? (item.GetHeldEntity() as FlameThrower).ammo : 0,
                ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                position = item.position,
                skin = item.skin,
                condition = item.condition,
                maxCondition = item.maxCondition,
                instanceData = new RestoreData.ItemData.InstanceData(item),
                contents = item.contents?.itemList.Select(item1 => new RestoreData.ItemData
                {
                    itemid = item1.info.itemid,
                    amount = item1.amount,
                    condition = item1.condition
                }).ToArray()
            });
        }

        public class RestoreData
        {
            public Hash<ulong, PlayerData> restoreData = new Hash<ulong, PlayerData>();

            public void AddData(BasePlayer player)
            {
                restoreData[player.userID] = new PlayerData(player);
            }

            public void RemoveData(ulong playerId)
            {
                if (HasRestoreData(playerId))
                    restoreData.Remove(playerId);
            }

            public bool HasRestoreData(ulong playerId) => restoreData.ContainsKey(playerId);

            public void RestorePlayer(BasePlayer player)
            {
                if (isUnloading)
                {
                    player.Die();
                    player.ChatMessage("<color=#ce422b>The plugin has been unloaded!</color> We are unable to restore you at this time, when the plugin has been reloaded you will be restored to your previous state");
                    return;
                }

                PlayerData playerData;
                if (restoreData.TryGetValue(player.userID, out playerData))
                {
                    StripInventory(player);

                    ins.UnlockInventory(player);

                    if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
                    {
                        ins.timer.Once(1, () => RestorePlayer(player));
                        return;
                    }

                    ins.NextTick(() =>
                    {
                        if (player == null || playerData == null)
                            return;

                        playerData.SetStats(player);
                        ins.MovePosition(player, playerData.GetPosition());
                        RestoreAllItems(player, playerData);
                    });
                }
            }

            private void RestoreAllItems(BasePlayer player, PlayerData playerData)
            {
                if (player == null || !player.IsConnected)
                    return;

                if (RestoreItems(player, playerData.containerBelt, "belt") && RestoreItems(player, playerData.containerWear, "wear") && RestoreItems(player, playerData.containerMain, "main"))
                    RemoveData(player.userID);
            }

            private bool RestoreItems(BasePlayer player, ItemData[] itemData, string type)
            {
                ItemContainer container = type == "belt" ? player.inventory.containerBelt : type == "wear" ? player.inventory.containerWear : player.inventory.containerMain;

                for (int i = 0; i < itemData.Length; i++)
                {
                    Item item = CreateItem(itemData[i]);
                    item.position = itemData[i].position;
                    item.SetParent(container);
                }
                return true;
            }

            public Item CreateItem(ItemData itemData)
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

                FlameThrower flameThrower = item.GetHeldEntity() as FlameThrower;
                if (flameThrower != null)
                    flameThrower.ammo = itemData.ammo;


                if (itemData.contents != null)
                {
                    foreach (ItemData contentData in itemData.contents)
                    {
                        Item newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                        if (newContent != null)
                        {
                            newContent.condition = contentData.condition;
                            newContent.MoveToContainer(item.contents);
                        }
                    }
                }
                return item;
            }

            public class PlayerData
            {
                public float[] stats;
                public float[] position;
                public ItemData[] containerMain;
                public ItemData[] containerWear;
                public ItemData[] containerBelt;

                public PlayerData() { }

                public PlayerData(BasePlayer player)
                {
                    stats = GetStats(player);
                    position = GetPosition(player.transform.position);
                    containerBelt = ins.GetItems(player.inventory.containerBelt).ToArray();
                    containerMain = ins.GetItems(player.inventory.containerMain).ToArray();
                    containerWear = ins.GetItems(player.inventory.containerWear).ToArray();
                }
                
                private float[] GetStats(BasePlayer player) => new float[] { player.health, player.metabolism.hydration.value, player.metabolism.calories.value };

                public void SetStats(BasePlayer player)
                {
                    player.health = stats[0];
                    player.metabolism.hydration.value = stats[1];
                    player.metabolism.calories.value = stats[2];
                    player.metabolism.SendChangesToClient();
                }

                private float[] GetPosition(Vector3 position) => new float[] { position.x, position.y, position.z };

                public Vector3 GetPosition() => new Vector3(position[0], position[1], position[2]);
            }

            public class ItemData
            {
                public int itemid;
                public ulong skin;
                public int amount;
                public float condition;
                public float maxCondition;
                public int ammo;
                public string ammotype;
                public int position;
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
        #endregion

        #region Sphere Creation
        private void CreateSphere()
        {
            finalSphere = new SphereEntity[configData.Game.SphereDarkness];
            for (int i = 0; i < configData.Game.SphereDarkness; i++)
            {
                var sphere = (SphereEntity)GameManager.server.CreateEntity(sphereEnt, endPos, new Quaternion(), true);
                sphere.currentRadius = 5;
                sphere.lerpSpeed = 0;
                sphere.enableSaving = false;
                sphere.Spawn();

                finalSphere[i] = sphere;
            }            

            finalSphere[0].gameObject.AddComponent<FinalSphere>();
        }
       
        class FinalSphere : MonoBehaviour
        {
            public BaseEntity entity;
            void Awake()
            {
                entity = GetComponent<BaseEntity>();                
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = $"Juggernaut Final Zone";
                enabled = false;

                var collider = entity.gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 0.5f;
            }
            void OnDestroy()
            {
                if (!entity.IsDestroyed)
                    entity.Kill();               
            }
            void OnTriggerEnter(Collider obj)
            {
                if (obj.gameObject.layer == (int)Layer.Player_Server)
                {
                    var player = obj?.GetComponentInParent<BasePlayer>();
                    if (player != null)
                    {
                        if (player == ins.juggernaut)
                        {
                            ins.EventWin();
                        }
                    }
                }
            }             
        }
        #endregion

        #region Hooks
        #region Internal Checks
        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            if (!Clans) return false;
            object playerTag = Clans?.Call("GetClanOf", playerId);
            object friendTag = Clans?.Call("GetClanOf", friendId);
            if (playerTag is string && friendTag is string)
                if (playerTag == friendTag) return true;
            return false;
        }
        private bool IsFriend(ulong playerID, ulong friendID)
        {
            if (!Friends) return false;
            return (bool)Friends?.Call("IsFriend", playerID, friendID);
        }
        private bool IsPlaying(BasePlayer player)
        {
            if (!EventManager) return false;
            return (bool)EventManager.Call("isPlaying", player);
        }
        private void JoinedEvent(BasePlayer player)
        {
            if (optedIn.Contains(player.userID))
                optedIn.Remove(player.userID);
        }
        private void AddMapMarker(float x, float z, string markerName) => LustyMap?.Call("AddTemporaryMarker", x, z, markerName, configData.UI.IconUrl);
        private void RemoveMapMarker(string markerName, bool startsWith)
        {
            if (!startsWith)
                LustyMap?.Call("RemoveTemporaryMarker", markerName);
            else LustyMap?.Call("RemoveTemporaryMarkerStartsWith", markerName);
        }        
        #endregion

        #region External Checks
        object CanTrade(BasePlayer player)
        {
            if (player == juggernaut)
                return msg("tryTrading", player.UserIDString);
            return null;
        }
        object canRemove(BasePlayer player)
        {
            if (player == juggernaut)
                return msg("tryRemove", player.UserIDString);
            return null;
        }
        object CanTeleport(BasePlayer player)
        {
            if (player == juggernaut)
                return msg("tryTP", player.UserIDString);
            return null;
        }
        object canShop(BasePlayer player)
        {
            if (player == juggernaut)
                return msg("tryShop", player.UserIDString);
            return null;
        }
        #endregion
        #endregion

        #region UI
        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = "Hud",
                        panelName
                    }
                };
                return NewElement;
            }
            static public void AddImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax, float fadeOut = 0f)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion

        #region UI Creation
        private const string Main = "JuggernautMain";
        private const string Compass = "JuggernautCompass";
        private void CreateTimerUI(BasePlayer player)
        {
            var MainCont = UI.CreateElementContainer(Main, UI.Color(configData.UI.UIBackgroundColor, configData.UI.UIOpacity), $"{configData.UI.TimerPosition.XPosition} {configData.UI.TimerPosition.YPosition}", $"{configData.UI.TimerPosition.XPosition + configData.UI.TimerPosition.XDimension} {configData.UI.TimerPosition.YPosition + configData.UI.TimerPosition.YDimension}");

            if (!string.IsNullOrEmpty(juggernautIcon))
                UI.AddImage(ref MainCont, Main, juggernautIcon, "0.01 0.05", "0.425 0.95");
            UI.CreateLabel(ref MainCont, Main, "", GetFormatTime(eventEnd), 15, "0.5 0", "1 1", TextAnchor.MiddleLeft);

            CuiHelper.DestroyUi(player, Main);
            CuiHelper.AddUi(player, MainCont);
        }

        private string GetFormatTime(double timeLeft)
        {
            var time = timeLeft - GrabCurrentTime();
            double minutes = Math.Floor((double)(time / 60));
            time -= (int)(minutes * 60);
            return string.Format("{0:00}:{1:00}", minutes, time);
        }

        private void RefreshAllUI()
        {
            uiTimer = timer.Repeat(1, (int)(eventEnd - GrabCurrentTime()) - 1, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                    CreateTimerUI(player);
                if (configData.UI.ShowCoordsToJuggernaut)
                    ShowCompass();
            });
        }

        private void ShowCompass()
        {
            var MainCont = UI.CreateElementContainer(Compass, UI.Color(configData.UI.UIBackgroundColor, configData.UI.UIOpacity), $"{configData.UI.CompassPosition.XPosition} {configData.UI.CompassPosition.YPosition}", $"{configData.UI.CompassPosition.XPosition + configData.UI.CompassPosition.XDimension} {configData.UI.CompassPosition.YPosition + configData.UI.CompassPosition.YDimension}");
            UI.CreateLabel(ref MainCont, Compass, "", string.Format(msg("target1", juggernaut.UserIDString), GetGridString(endPos)), 10, "0.05 0.5", "0.95 0.95", TextAnchor.MiddleLeft);
            UI.CreateLabel(ref MainCont, Compass, "", string.Format(msg("target2", juggernaut.UserIDString), Math.Round(Vector3.Distance(juggernaut.transform.position, endPos), 2)), 10, "0.05 0.05", "0.95 0.5", TextAnchor.MiddleLeft);
            CuiHelper.DestroyUi(juggernaut, Compass);
            CuiHelper.AddUi(juggernaut, MainCont);
        }
        #endregion

        #region Imagery
        private WWW info;
        public void Add(string url)
        {
            info = new WWW(url);
            TryDownloadImage();
        }
        void TryDownloadImage()
        {
            if (!info.isDone)
            {
                timer.In(1, TryDownloadImage);
                return;
            }
            if (!string.IsNullOrEmpty(info.error))
            {
                PrintError(string.Format("Failed to load the Juggernaut icon! Error: {0}", info.error));
                return;
            }
            else juggernautIcon = FileStorage.server.Store(info.bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();            
        }
        #endregion

        #region Commands
        [ChatCommand("jug")]
        void cmdJuggernaut(BasePlayer player, string command, string[] args)
        {            
            if (args.Length == 0)
            {
                SendReply(player, $"<color=#ce422b>{Title}</color><color=#939393>  v{Version}  -</color> <color=#ce422b>{Author} @ www.chaoscode.io</color>");
                SendReply(player, msg("help0", player.UserIDString));
                SendReply(player, msg("help1", player.UserIDString));
                SendReply(player, msg("help2", player.UserIDString));
                SendReply(player, msg("help3", player.UserIDString));
                SendReply(player, msg("help4", player.UserIDString));
                if (player.IsAdmin)
                {
                    SendReply(player, msg("help5", player.UserIDString));
                    SendReply(player, msg("help6", player.UserIDString));
                    SendReply(player, msg("help7", player.UserIDString));
                    SendReply(player, msg("help8", player.UserIDString));
                }
                return;
            }
            switch (args[0].ToLower())
            {
                case "info":
                    string time = GetFormatTime(nextTrigger);
                    if (hasStarted)
                        SendReply(player, string.Format(msg("endsIn", player.UserIDString), time));
                    else if (isOpen)
                        SendReply(player, string.Format(msg("startsIn", player.UserIDString), time));
                    else SendReply(player, string.Format(msg("nextEvent", player.UserIDString), time));                    
                    return;
                case "join":
                    if (!permission.UserHasPermission(player.UserIDString, "juggernaut.canbe"))
                    {
                        SendReply(player, msg("noPerms", player.UserIDString));
                        return;
                    }
                    if (!isOpen)
                    {
                        SendReply(player, msg("notOpen", player.UserIDString));
                        return;
                    }
                    if (IsPlaying(player))
                    {
                        SendReply(player, msg("playingEM", player.UserIDString));
                        return;
                    }
                    if (!optedIn.Contains(player.userID))
                    {
                        optedIn.Add(player.userID);
                        SendReply(player, msg("enteredDraw", player.UserIDString));
                        PrintToChat(string.Format(msg("candidate"), player.displayName, optedIn.Count));
                    }
                    return;
                case "leave":
                    if (!isOpen)
                    {
                        SendReply(player, msg("notOpen", player.UserIDString));
                        return;
                    }
                    if (optedIn.Contains(player.userID))
                        optedIn.Remove(player.userID);
                    SendReply(player, msg("removedDraw", player.UserIDString));
                    return;
                case "claim":
                    if (storedData.winnerRewards.ContainsKey(player.userID))
                    {
                        foreach(var prize in storedData.winnerRewards[player.userID])
                        {
                            Item item = restoreData.CreateItem(prize);
                            if (item != null)
                                player.GiveItem(item, BaseEntity.GiveItemReason.Generic);
                        }
                        SendReply(player, msg("claimSuccess", player.UserIDString));
                        storedData.winnerRewards.Remove(player.userID);
                        SaveData();
                    }
                    else if (storedData.winnerMoney.ContainsKey(player.userID))
                    {
                        foreach(var prize in storedData.winnerMoney[player.userID])
                        {
                            if (prize.isRp)                            
                                ServerRewards?.Call("AddPoints", player.userID, prize.amount);
                            else Economics?.Call("Deposit", player.userID, (double)prize.amount);
                        }
                        SendReply(player, msg("claimSuccess", player.UserIDString));
                        storedData.winnerMoney.Remove(player.userID);
                        SaveData();
                    }
                    else SendReply(player, msg("noPrizes", player.UserIDString));
                    return;
                case "restore":
                    if (player == juggernaut)
                    {
                        SendReply(player, msg("cantRestore", player.UserIDString));
                        return;
                    }
                    if (restoreData.HasRestoreData(player.userID))
                    {
                        TryRestorePlayer(player);
                        return;
                    }
                    else SendReply(player, msg("noRestoreData", player.UserIDString));
                    return;
                case "open":
                    if (player.IsAdmin)
                    {
                        if (isOpen)
                        {
                            SendReply(player, msg("alreadyOpen", player.UserIDString));
                            return;
                        }
                        if (!hasDestinations)
                        {
                            SendReply(player, msg("noDestinations", player.UserIDString));
                            return;
                        }
                        if (hasStarted)
                        {
                            SendReply(player, msg("alreadyStarted", player.UserIDString));
                            return;
                        }
                        if (nextMatch != null)
                            nextMatch.Destroy();
                        OpenEvent();
                    }
                    return;
                case "start":
                    if (player.IsAdmin)
                    {
                        if (!isOpen)
                        {
                            SendReply(player, msg("isntOpen", player.UserIDString));
                            return;
                        }
                        if (hasStarted)
                        {
                            SendReply(player, msg("alreadyStarted", player.UserIDString));
                            return;
                        }                        
                        if (startMatch != null)
                            startMatch.Destroy();
                        if (openMessage != null)
                            openMessage.Destroy();
                        StartEvent();
                    }
                    return;
                case "cancel":
                    if (player.IsAdmin)
                    {
                        if (!isOpen && !hasStarted)
                        {
                            SendReply(player, msg("notStarted", player.UserIDString));
                            return;
                        }
                        if (isOpen)
                        {
                            if (startMatch != null)
                                startMatch.Destroy();
                            if (openMessage != null)
                                openMessage.Destroy();
                            isOpen = false;
                            PrintToChat(msg("adminCancel"));
                            StartEventTimer();
                            return;
                        }  
                        if (hasStarted)
                        {                            
                            PrintToChat(msg("adminCancel"));
                            TryRestorePlayer(juggernaut);
                            DestroyEvent();
                            return;
                        }                      
                    }
                    return;
                case "destination":
                    if (player.IsAdmin)
                    {
                        if (!destinationCreator.ContainsKey(player.userID))
                        {
                            destinationCreator.Add(player.userID, new Destinations { x1 = player.transform.position.x, y1 = player.transform.position.y, z1 = player.transform.position.z });
                            SendReply(player, msg("destCreate1",player.UserIDString));
                            return;
                        }
                        else
                        {
                            Destinations data = destinationCreator[player.userID];
                            data.x2 = player.transform.position.x;
                            data.y2 = player.transform.position.y;
                            data.z2 = player.transform.position.z;
                            storedData.destinations.Add(data);
                            destinationCreator.Remove(player.userID);
                            SaveData();
                            if (!hasDestinations)
                                hasDestinations = true;
                            SendReply(player, msg("destCreate2", player.UserIDString));
                        }
                    }
                    return;
                default:
                    break;
            }
        }
        [ConsoleCommand("jug")]
        void ccmdJuggernaut(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, $"- {Title}  v{Version}  - {Author} @ www.chaoscode.io -");
                SendReply(arg, "jug open - Force open the event");
                SendReply(arg, "jug start - Force start the event");
                SendReply(arg, "jug cancel - Cancel a pending or current event");
                SendReply(arg, "jug clearicon - Clears any Juggernaut related icons from the map");

                return;
            }
            switch (arg.Args[0].ToLower())
            {
                case "open":
                    if (isOpen)
                    {
                        SendReply(arg, msg("alreadyOpen"));
                        return;
                    }
                    if (!hasDestinations)
                    {
                        SendReply(arg, msg("noDestinations"));
                        return;
                    }
                    if (hasStarted)
                    {
                        SendReply(arg, msg("alreadyStarted"));
                        return;
                    }
                    if (nextMatch != null)
                        nextMatch.Destroy();
                    OpenEvent();

                    return;
                case "start":
                    if (!isOpen)
                    {
                        SendReply(arg, msg("isntOpen"));
                        return;
                    }
                    if (hasStarted)
                    {
                        SendReply(arg, msg("alreadyStarted"));
                        return;
                    }
                    if (startMatch != null)
                        startMatch.Destroy();
                    if (openMessage != null)
                        openMessage.Destroy();
                    StartEvent();
                    return;
                case "cancel":
                    if (!isOpen && !hasStarted)
                    {
                        SendReply(arg, msg("notStarted"));
                        return;
                    }
                    if (isOpen)
                    {
                        if (startMatch != null)
                            startMatch.Destroy();
                        if (openMessage != null)
                            openMessage.Destroy();
                        isOpen = false;
                        PrintToChat(msg("adminCancel"));
                        StartEventTimer();
                        return;
                    }
                    if (hasStarted)
                    {
                        PrintToChat(msg("adminCancel"));
                        TryRestorePlayer(juggernaut);
                        DestroyEvent();
                        return;
                    }
                    return;
                case "clearicon":
                    RemoveMapMarker(msg("juggernaut"), false);
                    RemoveMapMarker(msg("destination"), false);                                     
                    RemoveMapMarker(msg("possibleDest"), true);                    
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;        
        class ConfigData
        {
            [JsonProperty(PropertyName = "Juggernaut Settings")]
            public JuggernautOptions Juggernaut { get; set; }
            [JsonProperty(PropertyName = "Event Timers")]
            public EventTimers Timers { get; set; }
            [JsonProperty(PropertyName = "UI Settings")]
            public UISettings UI { get; set; }
            [JsonProperty(PropertyName = "Event Conditions")]
            public EventConditions Conditions { get; set; }
            [JsonProperty(PropertyName = "LustyMap Integration")]
            public LustyIntegration Lusty { get; set; }
            [JsonProperty(PropertyName = "Game Settings")]
            public GameOptions Game { get; set; }
            [JsonProperty(PropertyName = "Reward Settings")]
            public PrizeOptions Prizes { get; set; }

            public class JuggernautOptions
            {
                [JsonProperty(PropertyName = "Defense damage modifier")]
                public float DefenseMod { get; set; }
                [JsonProperty(PropertyName = "Attack damage modifier")]
                public float AttackMod { get; set; }
                [JsonProperty(PropertyName = "Can damage structures")]
                public bool DamageStructures { get; set; }
                [JsonProperty(PropertyName = "Start with full metabolism")]
                public bool ResetMetabolism { get; set; }
                [JsonProperty(PropertyName = "Disable landmine damage")]
                public bool NoLandmineDamage { get; set; }
                [JsonProperty(PropertyName = "Disable beartrap damage")]
                public bool NoBeartrapDamage { get; set; }
                [JsonProperty(PropertyName = "Disable fall damage")]
                public bool NoFallDamage { get; set; }
                [JsonProperty(PropertyName = "Inventory contents")]
                public List<InventoryItem> Inventory { get; set; }

                public class InventoryItem
                {
                    [JsonProperty(PropertyName = "Item shortname")]
                    public string Shortname { get; set; }
                    [JsonProperty(PropertyName = "Amount of item")]
                    public int Amount { get; set; }
                    [JsonProperty(PropertyName = "Item skin ID")]
                    public ulong SkinID { get; set; }
                    [JsonProperty(PropertyName = "Is this item a blueprint?")]
                    public bool IsBP { get; set; }
                    [JsonProperty(PropertyName = "Container (main, wear or belt)")]
                    public string Container { get; set; }
                }
            }            
            public class PrizeOptions
            {
                [JsonProperty(PropertyName = "Allow players to loot juggernaut as a prize")]
                public bool Inventory { get; set; }
                [JsonProperty(PropertyName = "Use Economics money as a prize")]
                public bool Economics { get; set; }
                [JsonProperty(PropertyName = "Use ServerRewards money as a prize")]
                public bool ServerRewards { get; set; }
                [JsonProperty(PropertyName = "Monetary amount")]
                public int Amount { get; set; }
            }
            public class EventTimers
            {
                [JsonProperty(PropertyName = "Amount of time to complete journey (seconds)")]
                public int Completion { get; set; }
                [JsonProperty(PropertyName = "Amount of time between events (seconds)")]
                public int Interval { get; set; }
                [JsonProperty(PropertyName = "Amount of time the entry process will remain open (seconds)")]
                public int Open { get; set; }
            }
            public class EventConditions
            {
                [JsonProperty(PropertyName = "The percentage of server players required for the event to start")]
                public float Percentage { get; set; }
                [JsonProperty(PropertyName = "The minimum amount of players on the server required to open the event")]
                public int OpenMin { get; set; }
            }
            public class LustyIntegration
            {
                [JsonProperty(PropertyName = "Show the destination icon on the map")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "The amount of possible destinations to show")]
                public int DestinationAmount { get; set; }
            }
            public class GameOptions
            {
                [JsonProperty(PropertyName = "Broadcast the juggernauts position to chat every X seconds")]
                public int BroadcastEvery { get; set; }
                [JsonProperty(PropertyName = "Broadcast the juggernauts position to LustyMap ever X seconds")]
                public bool BroadcastEveryLM { get; set; }
                [JsonProperty(PropertyName = "The amount of time the juggernauts position will remain on LustyMap if enabled (seconds)")]
                public int BroadcastLMSeconds { get; set; }
                [JsonProperty(PropertyName = "Destination sphere darkness")]
                public int SphereDarkness { get; set; }
                [JsonProperty(PropertyName = "Blacklisted commands for event players")]
                public string[] CommandBlacklist { get; set; }
            }
            public class UISettings
            {
                [JsonProperty(PropertyName = "Show the juggernaut their position so they can navigate")]
                public bool ShowCoordsToJuggernaut { get; set; }                
                [JsonProperty(PropertyName = "Display a timer showing how long the juggernaut has to get to their destination")]
                public bool ShowUITimer { get; set; }
                [JsonProperty(PropertyName = "The URL of the juggernaut icon")]
                public string IconUrl { get; set; }
                [JsonProperty(PropertyName = "Timer positioning")]
                public Position TimerPosition { get; set; }
                [JsonProperty(PropertyName = "Compass positioning")]
                public Position CompassPosition { get; set; }
                [JsonProperty(PropertyName = "UI background color (hex)")]
                public string UIBackgroundColor { get; set; }
                [JsonProperty(PropertyName = "UI opacity (0.0 - 1.0)")]
                public float UIOpacity { get; set; }

                public class Position
                {
                    [JsonProperty(PropertyName = "Horizontal start position (left)")]
                    public float XPosition { get; set; }
                    [JsonProperty(PropertyName = "Vertical start position (bottom)")]
                    public float YPosition { get; set; }
                    [JsonProperty(PropertyName = "Horizontal dimensions")]
                    public float XDimension { get; set; }
                    [JsonProperty(PropertyName = "Vertical dimensions")]
                    public float YDimension { get; set; }
                }
            }

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
                Conditions = new ConfigData.EventConditions
                {
                    OpenMin = 10,
                    Percentage = 0.6f
                },
                Timers = new ConfigData.EventTimers
                {
                    Open = 120,
                    Interval = 3600,
                    Completion = 900
                },
                Game = new ConfigData.GameOptions
                {
                    BroadcastEvery = 0,
                    BroadcastEveryLM = false,
                    BroadcastLMSeconds = 5,
                    SphereDarkness = 5,
                    CommandBlacklist = new string[]
                    {
                        "s",
                        "tp",
                        "tpa",
                        "tpr",
                        "home"
                    }
                },
                Lusty = new ConfigData.LustyIntegration
                {
                    DestinationAmount = 1,
                    Enabled = true
                },
                Juggernaut = new ConfigData.JuggernautOptions
                {
                    AttackMod = 1.5f,
                    DamageStructures = true,
                    ResetMetabolism = true,
                    DefenseMod = 0.5f,
                    NoBeartrapDamage = false,
                    NoLandmineDamage = false,
                    NoFallDamage = false,
                    Inventory = new List<ConfigData.JuggernautOptions.InventoryItem>
                    {
                        new ConfigData.JuggernautOptions.InventoryItem
                        {
                            Amount = 1,
                            Shortname = "heavy.plate.pants",
                            SkinID = 0,
                            IsBP = false,
                            Container = "wear"
                        },
                        new ConfigData.JuggernautOptions.InventoryItem
                        {
                            Amount = 1,
                            Shortname = "heavy.plate.jacket",
                            SkinID = 0,
                            IsBP = false,
                            Container = "wear"
                        },
                        new ConfigData.JuggernautOptions.InventoryItem
                        {
                            Amount = 1,
                            Shortname = "heavy.plate.helmet",
                            SkinID = 0,
                            IsBP = false,
                            Container = "wear"
                        },
                        new ConfigData.JuggernautOptions.InventoryItem
                        {
                            Amount = 1,
                            Shortname = "lmg.m249",
                            SkinID = 0,
                            IsBP = false,
                            Container = "belt"
                        },
                        new ConfigData.JuggernautOptions.InventoryItem
                        {
                            Amount = 500,
                            Shortname = "ammo.rifle.explosive",
                            SkinID = 0,
                            IsBP = false,
                            Container = "main"
                        },
                        new ConfigData.JuggernautOptions.InventoryItem
                        {
                            Amount = 3,
                            Shortname = "grenade.f1",
                            SkinID = 0,
                            IsBP = false,
                            Container = "belt"
                        },
                        new ConfigData.JuggernautOptions.InventoryItem
                        {
                            Amount = 3,
                            Shortname = "syringe.medical",
                            SkinID = 0,
                            IsBP = false,
                            Container = "belt"
                        }
                    }
                },
                Prizes = new ConfigData.PrizeOptions
                {
                    Amount = 0,
                    Economics = false,
                    Inventory = true,
                    ServerRewards = false
                },
                UI = new ConfigData.UISettings
                {
                    IconUrl = "http://www.rustedit.io/images/juggernaut_icon.png",
                    UIBackgroundColor = "#4C4C4C",
                    ShowCoordsToJuggernaut = true,
                    ShowUITimer = true,
                    CompassPosition = new ConfigData.UISettings.Position
                    {
                        XDimension = 0.11f,
                        XPosition = 0.725f,
                        YDimension = 0.05f,
                        YPosition = 0.026f,
                    },
                    TimerPosition = new ConfigData.UISettings.Position
                    {
                        XDimension = 0.07f,
                        XPosition = 0.65f,
                        YDimension = 0.05f,
                        YPosition = 0.026f,
                    },
                    UIOpacity = 0.7f
                },
                Version = Version
            };            
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management        
        private class Destinations
        {
            public float x1, y1, z1, x2, y2, z2;
        }

        private class Points
        {
            public int amount;
            public bool isRp;
        }

        private void SaveData() => data.WriteObject(storedData);

        private void SaveRestoreData() => restorationData.WriteObject(restoreData);

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
                restoreData = restorationData.ReadObject<RestoreData>();
            }
            catch
            {
                restoreData = new RestoreData();
            }
        }

        private class StoredData
        {
            public Dictionary<ulong, List<RestoreData.ItemData>> winnerRewards = new Dictionary<ulong, List<RestoreData.ItemData>>();
            public Dictionary<ulong, List<Points>> winnerMoney = new Dictionary<ulong, List<Points>>();
            public List<Destinations> destinations = new List<Destinations>();
        }
        #endregion

        #region Localization
        string msg(string key, string playerId = "") => lang.GetMessage(key, this, playerId);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"eventOpen", "<color=#ce422b>[Juggernaut] </color><color=#939393>: The juggernaut event is open for contestants! Type </color><color=#ce422b>/jug join</color><color=#939393> to join the list of juggernaut candidates</color>" },
            {"playerChosen", "<color=#ce422b>[Juggernaut] </color><color=#939393>: The juggernaut has been chosen! The event will start in </color><color=#ce422b>10</color><color=#939393> seconds</color>" },
            {"juggerChosen", "<color=#ce422b>[Juggernaut] </color><color=#939393>: You have been chosen to be the juggernaut!</color>" },
            {"eventStart", "<color=#ce422b>[Juggernaut] </color><color=#939393>: The juggernaut is inbound towards </color><color=#ce422b>{0}</color><color=#939393>! Find and kill him before he gets to the destination!</color>" },
            {"juggerStart", "<color=#ce422b>[Juggernaut]</color><color=#939393> : You must make your way to </color><color=#ce422b>{0}</color><color=#939393> and enter the white sphere to win the event!</color>" },
            {"juggerDead", "<color=#ce422b>[Juggernaut]</color><color=#939393> : The juggernaut has been killed and his loot is there for the taking!</color>" },
            {"juggerWin", "<color=#ce422b>[Juggernaut]</color><color=#939393> : The juggernaut made his way to the target destination and has won the event!</color>" },
            {"juggerClaim", "<color=#ce422b>[Juggernaut]</color><color=#939393> : You have won the event! To claim your reward type </color><color=#ce422b>/juggernaught claim</color><color=#939393> once you have emptied your inventory</color>" },
            {"noEntrants", "<color=#ce422b>[Juggernaut]</color><color=#939393> : No players signed up to be the juggernaut. The event has been cancelled!</color>" },
            {"notEnoughEntrants", "<color=#ce422b>[Juggernaut]</color><color=#939393> : Not enough players signed up to be the juggernaut. Minimum percentage of online players required to start is {0}</color>" },
            {"eventCancel", "<color=#ce422b>[Juggernaut]</color><color=#939393> : The event is now over as the juggernaut took too long to reach his destination</color>" },
            {"adminCancel", "<color=#ce422b>[Juggernaut]</color><color=#939393> : A admin has cancelled the event!</color>" },
            {"enteredDraw", "<color=#939393>You have entered yourself into the draw to be the juggernaut. You can leave at any time by typing </color><color=#ce422b>/jug leave</color>" },
            {"removedDraw", "<color=#939393>You have removed yourself from the draw to be the juggernaut</color>" },
            {"noPrizes", "<color=#939393>You do not have any outstanding prizes</color>" },
            {"claimSuccess", "<color=#939393>You have successfully claimed your prize</color>" },
            {"destination", "Juggernaut Destination" },
            {"possibleDest", "Possible Destination" },
            {"positionBroadcast", "<color=#939393>The juggernaut was last seen at</color> <color=#ce422b>X:{0}, Z:{1}</color>" },
            {"target1", "Grid Target : <color=#ce422b>{0}</color>" },
            {"target2", "Distance : <color=#ce422b>{0}m</color>" },
            //{"current", "Current : " },
            {"juggernaut", "Juggernaut" },
            {"noPerms", "<color=#939393>You do not have permission to be the juggernaut</color>" },
            {"help0", "<color=#ce422b>/jug info</color><color=#939393> - Show how much time is remaining between various stages of the event process</color>" },
            {"help1", "<color=#ce422b>/jug join</color><color=#939393> - Enter yourself into the draw to be a juggernaut</color>" },
            {"help2", "<color=#ce422b>/jug leave</color><color=#939393> - Remove yourself from the draw to be a juggernaut</color>"},
            {"help3", "<color=#ce422b>/jug claim</color><color=#939393> - Claim any outstanding prizes you have won (ensure a empty inventory when claiming!)</color>"},
            {"help4", "<color=#ce422b>/jug restore</color><color=#939393> - Restore your previous state before you became the juggernaut</color>" },
            {"help5", "<color=#ce422b>/jug open</color><color=#939393> - Force open a event</color>"},
            {"help6", "<color=#ce422b>/jug start</color><color=#939393> - Force start a event</color>"},
            {"help7", "<color=#ce422b>/jug cancel</color><color=#939393> - Cancel a current event</color>"},
            {"help8", "<color=#ce422b>/jug destination</color><color=#939393> - Create a new destination starting at your position / Add the end point of a destination at your position</color>"},
            {"notOpen", "<color=#939393>There is not currently a event open</color>"},
            {"playingEM", "<color=#939393>You can not join a juggernaut event when playing in Event Manager</color>"},
            {"cantRestore", "<color=#939393>You can't restore while you are the juggernaut</color>"},
            {"noRestoreData", "<color=#939393>You do not have any saved restore data</color>"},
            {"alreadyOpen", "<color=#939393>There is already a event open</color>"},
            {"noDestinations", "<color=#939393>No destinations have been set! Unable to start the event</color>"},
            {"alreadyStarted", "<color=#939393>There is already a event in progress</color>"},
            {"isntOpen", "<color=#939393>You must open a event before starting</color>"},
            {"notStarted", "<color=#939393>There is no event currently in progress</color>"},
            {"destCreate1", "<color=#939393>You have begun creating a new destination starting at your current position. Move to the position you would like the end point to be and type this command again</color>"},
            {"destCreate2", "<color=#939393>You have sucessfully created a new potential destination</color>"},
            {"tryTrading", "<color=#939393>You can't trade while you are the juggernaut!</color>"},
            {"tryRemove", "<color=#939393>You can't remove while you are the juggernaut!</color>"},
            {"tryTP", "<color=#939393>You can't teleport while you are the juggernaut!</color>"},
            {"tryShop", "<color=#939393>You can't shop while you are the juggernaut!</color>"},
            {"ff1", "<color=#939393>You can't hurt your friends when they are the juggernaut!</color>"},
            {"ff2", "<color=#939393>You can't hurt your clan mates when they are the juggernaut!</color>"},
            {"endsIn", "<color=#939393>The current event ends in : </color><color=#ce422b>{0}</color>" },
            {"startsIn", "<color=#939393>The event will start in : </color><color=#ce422b>{0}</color>" },
            {"nextEvent", "<color=#939393>The next event opens in : </color><color=#ce422b>{0}</color>" },
            {"noSuicide", "<color=#939393>You can not suicide whilst you are the juggernaut!</color>" },
            {"juggerSuicide", "<color=#939393>The juggernaut suicided! The event is over and the loot has been removed</color>" },
            {"candidate", "<color=#ce422b>{0}</color> <color=#939393>has registered as a juggernaut candidate!</color> <color=#ce422b>({1} players registered)</color>" },
            {"blacklistcmd", "<color=#939393>You can not run that command while you are the juggernaut!</color>" }
        };
        #endregion
    }
}
