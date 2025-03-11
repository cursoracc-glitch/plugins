using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("The Golden Egg", "crunch", "1.9.24")]
    [Description("Once found, the Golden Egg can bring you great riches, but it doesn't want to remain hidden...")]
    class TheGoldenEgg : CovalencePlugin
    {
        [PluginReference]
        private Plugin TimedPermissions, RaidableBases;
        private Configuration _pluginConfig;
        private static TheGoldenEgg ins;
        private const string permissionUse = "thegoldenegg.use";
        private StoredData storedData;
        private DynamicConfigFile data;
        public float chance = 0f;
        public ulong eggUID = 0;
        public ulong chinookID = 0;
        private List<ulong> crateID = new List<ulong>();
        private Timer markerTimer, rewardTimer, delayTimer, saveTimer, blockTimer, moveTimer, crateEventTimer, crateMarkTimer, safeTimer, buildTimer, raidBaseTimer;
        private int resource, amount, spawnTime;
        private string cleanName, itemName;
        private bool blockEggOpen, safezone = false, inPriv = false, pop = false, priv = false, tod = false, blocked = false, isBlocked = false, crateDrop, isRunning = false;
        public static string currentOwner;
        private DateTime startTime;
        private static PluginTimers Timer;
        public List<MapMarkerGenericRadius> eggRadMarker = new List<MapMarkerGenericRadius>();
        public List<VendingMachineMapMarker> eggVendMarker = new List<VendingMachineMapMarker>();
        public MapMarkerGenericRadius _marker;
        public Dictionary<string, Item> _spawnedItems = new Dictionary<string, Item>();
        public Dictionary<ulong, HackableLockedCrate> _spawnedCrates = new Dictionary<ulong, HackableLockedCrate>();
        readonly int blockLayer = LayerMask.GetMask("Player (Server)");
        private List<ulong> allowDraw = new List<ulong>();
        private List<uint> buildID = new List<uint>();
        public Vector3 pos, lastPosition, cratePos;

        void Init()
        {
            LoadData();
            AddCommands();
            Timer = timer;
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnItemPickup));
            Unsubscribe(nameof(OnItemDropped));
            Unsubscribe(nameof(CanEntityTakeDamage));
            Unsubscribe(nameof(OnCollectiblePickup));
            Unsubscribe(nameof(OnHelicopterDropCrate));
            Unsubscribe(nameof(OnCrateDropped));
            Unsubscribe(nameof(CanHackCrate));
            Unsubscribe(nameof(OnPlayerCommand));

        }

        void OnServerInitialized(bool initial)
        {
            permission.RegisterPermission(permissionUse, this);

            _pluginConfig.EggRewards.Remove("item.one");
            _pluginConfig.EggRewards.Remove("item.two");
            _pluginConfig.EggRewards.Remove("etc");

            crateDrop = false;

            foreach (var ent in BaseNetworkable.serverEntities)
            {
                if (ent is StorageContainer)
                {
                    var container = (ent as StorageContainer)?.inventory?.itemList ?? null;
                    for (int j = 0; j < container.Count; j++)
                    {
                        if (container[j].text == "6E657264")
                        {
                            SaveDataTimer();

                            var playerID = (ent as StorageContainer).OwnerID;
                            var player = players.FindPlayerById(Convert.ToString(playerID));
                            var bPlayer = BasePlayer.FindAwakeOrSleeping(Convert.ToString(playerID));

                            priv = false;

                            if (bPlayer != null && _pluginConfig.NoPrivTime)
                                priv = (ent as StorageContainer).inventory.entityOwner?.GetBuildingPrivilege()?.IsAuthed(bPlayer) ?? false;

                            if (_pluginConfig.TimeOfDay)
                            {
                                TimeSpan tsStart = DateTime.Parse(_pluginConfig.StartTime).TimeOfDay;
                                TimeSpan tsEnd = DateTime.Parse(_pluginConfig.EndTime).TimeOfDay;

                                if (IsBetween(tsStart, tsEnd))
                                    tod = true;
                            }

                            if ((!priv) && (!tod) && (ent.ShortPrefabName != "small_stash_deployed" || (ent.ShortPrefabName == "small_stash_deployed" && !_pluginConfig.BlockStashTime)))
                            {
                                if (_pluginConfig.ServerPop == 0 || (covalence.Server.Players + ConVar.Admin.ServerInfo().Joining >= _pluginConfig.ServerPop))
                                    storedData.OnEggMove(player);
                            }

                            pos = ent.transform.position;

                            eggUID = container[j].uid.Value;
                            itemName = String.IsNullOrEmpty(_pluginConfig.customItemS.ItemName) ? "The Golden Egg" : _pluginConfig.customItemS.ItemName;

                            if (!_spawnedItems.ContainsKey(container[j].text))
                            {
                                _spawnedItems.Add(container[j].text, container[j]);
                            }

                            Unsubscribe(nameof(CanLootEntity));
                            Subscribe(nameof(OnItemAddedToContainer));
                            Subscribe(nameof(OnItemRemovedFromContainer));
                            Subscribe(nameof(OnItemPickup));
                            Subscribe(nameof(OnItemDropped));
                            if (_pluginConfig.roamS.IncreasePickup)
                                Subscribe(nameof(OnCollectiblePickup));
                            if (_pluginConfig.tpveS.DisableTruePVE)
                                Subscribe(nameof(CanEntityTakeDamage));
                            if (_pluginConfig.BlockEggCrack)
                                Subscribe(nameof(OnEntityTakeDamage));
                            if (_pluginConfig.crateS.crateEvent)
                            {
                                Subscribe(nameof(OnHelicopterDropCrate));
                                Subscribe(nameof(OnCrateDropped));
                                Subscribe(nameof(CanHackCrate));
                            }
                            if (_pluginConfig.UseBlacklist)
                            {
                                Subscribe(nameof(OnPlayerCommand));
                            }

                            if (_pluginConfig.markerS.boxMarker)
                            {
                                CreateMarkers();
                                markerTimer = timer.Repeat(_pluginConfig.markerS.BoxMarkerRefreshTime, 0, () =>
                                {
                                    /*                                    if (ent == null)
                                                                       {
                                                                           if (markerTimer != null) markerTimer.Destroy();
                                                                           MarkerDelete();
                                                                           markerTimer = null;
                                                                       }
                                                                       else
                                                                       { */
                                    pos = ent.transform.position;
                                    CreateMarkers();
                                    //}
                                });
                            }
                            else
                            {
                                if (markerTimer != null)
                                {
                                    markerTimer.Destroy();
                                    markerTimer = null;
                                }
                                MarkerDelete();
                            }

                            resource = 0;
                            spawnTime = _pluginConfig.resourceS.ResourceSpawnTime;

                            for (int x = 0; x < container.Count; x++)
                            {
                                var shortName = container[x].info.shortname;
                                cleanName = container[x].info.displayName.english;

                                if (ent.ShortPrefabName == "small_stash_deployed") return;

                                var isDone = false;
                                switch (shortName)
                                {
                                    case "metal.refined":
                                    case "lowgradefuel":
                                    case "metal.fragments":
                                    case "scrap":
                                        resource = container[x].info.itemid;
                                        amount = GetResourceAmount(shortName);
                                        isDone = true;
                                        break;

                                    case "sulfur":
                                        if (_pluginConfig.resourceS.AllowSulfurCooked)
                                        {
                                            resource = container[x].info.itemid;
                                            amount = GetResourceAmount(shortName);
                                            isDone = true;
                                        }
                                        break;

                                    case "sulfur.ore":
                                        if (_pluginConfig.resourceS.AllowSulfurOre)
                                        {
                                            resource = container[x].info.itemid;
                                            amount = GetResourceAmount(shortName);
                                            isDone = true;
                                        }
                                        break;
                                }

                                if (isDone) break;

                                if (shortName == _pluginConfig.resourceS.CustomItem1)
                                {
                                    resource = container[x].info.itemid;
                                    amount = _pluginConfig.resourceS.CustomAmount1;
                                    spawnTime = _pluginConfig.resourceS.CustomTime1;
                                    break;
                                }
                                else if (shortName == _pluginConfig.resourceS.CustomItem2)
                                {
                                    resource = container[x].info.itemid;
                                    amount = _pluginConfig.resourceS.CustomAmount2;
                                    spawnTime = _pluginConfig.resourceS.CustomTime2;
                                    break;
                                }
                                else if (shortName == _pluginConfig.resourceS.CustomItem3)
                                {
                                    resource = container[x].info.itemid;
                                    amount = _pluginConfig.resourceS.CustomAmount3;
                                    spawnTime = _pluginConfig.resourceS.CustomTime3;
                                    break;
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            if (resource == 0)
                            {
                                resource = -932201673;
                                amount = GetResourceAmount("scrap");
                                cleanName = "Scrap";
                            }

                            if (rewardTimer != null) rewardTimer.Destroy();
                            startTime = DateTime.Now;

                            rewardTimer = timer.Repeat(spawnTime, 0, () =>
                            {
                                SpawnRewards(ent as BaseEntity, resource, amount);
                                startTime = DateTime.Now;
                            });

                            return;
                        }
                    }
                }
                else if (ent is BasePlayer basePlayer)
                {
                    List<Item> items = new List<Item>();
                    basePlayer.inventory.GetAllItems(items);
                    if (items != null && items.Count > 0) for (int j = 0; j < items.Count; j++)
                        {
                            if (items[j].text == "6E657264")
                            {
                                SaveDataTimer();

                                var playerID = (ent as BasePlayer).userID;
                                var player = players.FindPlayerById(Convert.ToString(playerID));
                                var bPlayer = BasePlayer.FindAwakeOrSleeping(Convert.ToString(playerID));

                                currentOwner = (ent as BasePlayer).UserIDString;

                                priv = false;

                                if (bPlayer != null && _pluginConfig.NoPrivTime)
                                    priv = bPlayer?.GetBuildingPrivilege()?.IsAuthed(bPlayer) ?? false;

                                if (!priv)
                                {
                                    if (_pluginConfig.ServerPop == 0 || (covalence.Server.Players + ConVar.Admin.ServerInfo().Joining >= _pluginConfig.ServerPop))
                                        storedData.OnEggMove(player);
                                }

                                eggUID = items[j].uid.Value;
                                itemName = String.IsNullOrEmpty(_pluginConfig.customItemS.ItemName) ? "The Golden Egg" : _pluginConfig.customItemS.ItemName;

                                if (!_spawnedItems.ContainsKey(items[j].text))
                                {
                                    _spawnedItems.Add(items[j].text, items[j]);
                                }

                                Unsubscribe(nameof(CanLootEntity));
                                Subscribe(nameof(OnItemAddedToContainer));
                                Subscribe(nameof(OnItemRemovedFromContainer));
                                Subscribe(nameof(OnItemPickup));
                                Subscribe(nameof(OnItemDropped));
                                if (_pluginConfig.roamS.IncreasePickup)
                                    Subscribe(nameof(OnCollectiblePickup));
                                if (_pluginConfig.tpveS.DisableTruePVE)
                                    Subscribe(nameof(CanEntityTakeDamage));
                                if (_pluginConfig.BlockEggCrack)
                                    Subscribe(nameof(OnEntityTakeDamage));
                                if (_pluginConfig.crateS.crateEvent)
                                {
                                    Subscribe(nameof(OnHelicopterDropCrate));
                                    Subscribe(nameof(OnCrateDropped));
                                    Subscribe(nameof(CanHackCrate));
                                }
                                if (_pluginConfig.UseBlacklist)
                                {
                                    Subscribe(nameof(OnPlayerCommand));
                                }

                                pos = ent.transform.position;
                                safezone = false;
                                inPriv = false;
                                pop = false;
                                tod = false;

                                CreateMarkers(bPlayer);

                                markerTimer = timer.Repeat(_pluginConfig.markerS.PlayerMarkerRefreshTime, 0, () =>
                                {
                                    pos = ent.transform.position;
                                    CreateMarkers(bPlayer);
                                });
                            }
                        }
                }
                else if (ent is DroppedItem)
                {
                    if (ent == null || ent.IsDestroyed)
                    {
                        continue;
                    }

                    var drop = ent as DroppedItem;

                    if (drop.item.text == "6E657264")
                    {
                        eggUID = drop.item.uid.Value;
                        itemName = String.IsNullOrEmpty(_pluginConfig.customItemS.ItemName) ? "The Golden Egg" : _pluginConfig.customItemS.ItemName;

                        if (!_spawnedItems.ContainsKey(drop.item.text))
                        {
                            _spawnedItems.Add(drop.item.text, drop.item);
                        }

                        Unsubscribe(nameof(CanLootEntity));
                        Subscribe(nameof(OnItemAddedToContainer));
                        Subscribe(nameof(OnItemRemovedFromContainer));
                        Subscribe(nameof(OnItemPickup));
                        Subscribe(nameof(OnItemDropped));
                        if (_pluginConfig.roamS.IncreasePickup)
                            Subscribe(nameof(OnCollectiblePickup));
                        if (_pluginConfig.tpveS.DisableTruePVE)
                            Subscribe(nameof(CanEntityTakeDamage));
                        if (_pluginConfig.BlockEggCrack)
                            Subscribe(nameof(OnEntityTakeDamage));
                        if (_pluginConfig.crateS.crateEvent)
                        {
                            Subscribe(nameof(OnHelicopterDropCrate));
                            Subscribe(nameof(OnCrateDropped));
                            Subscribe(nameof(CanHackCrate));
                        }
                        if (_pluginConfig.UseBlacklist)
                        {
                            Subscribe(nameof(OnPlayerCommand));
                        }

                        pos = ent.transform.position;
                        safezone = false;
                        inPriv = false;
                        pop = false;
                        tod = false;

                        CreateMarkers();

                        markerTimer = timer.Repeat(_pluginConfig.markerS.PlayerMarkerRefreshTime, 0, () =>
                        {
                            pos = ent.transform.position;

                            CreateMarkers();
                        });
                    }
                }
            }
            if (eggUID == 0)
            {
                Subscribe(nameof(CanLootEntity));
                Unsubscribe(nameof(OnItemAddedToContainer));
                Unsubscribe(nameof(OnItemRemovedFromContainer));
                if (_pluginConfig.BlockEggCrack)
                    Unsubscribe(nameof(OnEntityTakeDamage));
                if (_pluginConfig.tpveS.DisableTruePVE)
                    Unsubscribe(nameof(CanEntityTakeDamage));
                if (_pluginConfig.roamS.IncreasePickup)
                    Unsubscribe(nameof(OnCollectiblePickup));
                if (_pluginConfig.crateS.crateEvent)
                {
                    Unsubscribe(nameof(OnHelicopterDropCrate));
                    Unsubscribe(nameof(OnCrateDropped));
                    Unsubscribe(nameof(CanHackCrate));
                }
                if (_pluginConfig.UseBlacklist)
                {
                    Unsubscribe(nameof(OnPlayerCommand));
                }
            }

            if (_pluginConfig.KillTime != 0)
                timer.Every(60f, UpdateLoop);

            runEvent();
        }

        private void runEvent()
        {
            if (eggUID == 0) return;

            if (chinookID != 0)
            {
                Puts("The chinook event is already running");
                return;
            }

            if (_pluginConfig.crateS.crateEvent && _pluginConfig.crateS.crateRepeat && (covalence.Server.Players + ConVar.Admin.ServerInfo().Joining >= _pluginConfig.crateS.cratePop))
            {
                crateEventTimer = timer.In(UnityEngine.Random.Range(_pluginConfig.crateS.minStartTime, _pluginConfig.crateS.maxStartTime), () =>
                {
                    Puts("The chinook event has started");
                    chinookID = 0;
                    crateID.Clear();
                    _spawnedCrates.Clear();

                    if (_marker != null)
                    {
                        _marker.Kill();
                        _marker.SendUpdate();
                    }
                    if (crateMarkTimer != null)
                        crateMarkTimer.Destroy();

                    var chinook = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab") as CH47HelicopterAIController;
                    if (chinook == null)
                    {
                        return;
                    }

                    chinook.numCrates = _pluginConfig.crateS.numCrates;

                    chinook.TriggeredEventSpawn();
                    chinook.Spawn();

                    chinookID = chinook.net.ID.Value;
                });
            }
            return;
        }

        private void UpdateLoop()
        {
            if (String.IsNullOrEmpty(storedData._eggCreated)) return;

            if ((DateTime.Now - DateTime.Parse((string)storedData._eggCreated)).TotalMinutes < _pluginConfig.KillTime)
                return;

            if (_spawnedItems != null)
            {
                foreach (var _item in _spawnedItems.ToList())
                {
                    if (_item.Key == "6E657264")
                    {
                        Item item = _item.Value;
                        if (item != null)
                        {
                            item.Remove();
                            _spawnedItems.Remove(_item.Key);
                        }
                    }
                }
            }
        }

        void Unload()
        {
            storedData.GetTotals();
            SaveData();

            Timer = null;
            ins = null;

            if (markerTimer != null)
            {
                markerTimer.Destroy();
                markerTimer = null;
            }
            if (rewardTimer != null) rewardTimer.Destroy();
            if (crateMarkTimer != null) crateMarkTimer.Destroy();
            if (_marker != null)
            {
                _marker.Kill();
                _marker.SendUpdate();
            }
            MarkerDelete();
        }

        void OnNewSave(string filename)
        {
            if (_pluginConfig.ClearData)
            {
                data.Clear();
                data.Save();
                storedData = data.ReadObject<StoredData>();
                if (storedData == null)
                {
                    storedData = new StoredData();
                }

                Log("Wiped player_data.json");
            }
        }

        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (player == null || container == null || eggUID != 0 || spamCheck.ContainsKey(player.userID) || player.IsNpc || !player.userID.IsSteamId())
                return null;

            switch (container.ShortPrefabName)
            {
                case "codelockedhackablecrate":
                case "chinooklockedcrate":
                case "chinooklockedcratecodelocked":
                case "codelockedhackablecrate_oilrig":
                    chance = _pluginConfig.LockedCrateEggChance;
                    break;

                case "crate_elite":
                case "bradley_crate":
                    chance = _pluginConfig.EliteCrateEggChance;
                    break;

                case "crate_normal":
                    chance = _pluginConfig.MilitaryCrateEggChance;
                    break;

                case "crate_normal_2":
                    chance = _pluginConfig.NormalCrateEggChance;
                    break;

                default:
                    return null;
            }

            var randomInt = UnityEngine.Random.Range(0f, 100f);

            AddSpamBlock(player.userID);

            if (randomInt <= chance)
            {
                Item item = ItemManager.CreateByItemID(-1002156085, 1);
                eggUID = item.uid.Value;

                if (_pluginConfig.customItemS.ItemSkin != 0)
                    item.skin = _pluginConfig.customItemS.ItemSkin;

                SaveDataTimer();
                Subscribe(nameof(OnItemPickup));
                Subscribe(nameof(OnItemDropped));

                itemName = String.IsNullOrEmpty(_pluginConfig.customItemS.ItemName) ? "The Golden Egg" : _pluginConfig.customItemS.ItemName;

                item.name = itemName;
                item.text = "6E657264";
                item.MarkDirty();

                if (!_spawnedItems.ContainsKey(item.text))
                {
                    _spawnedItems.Add(item.text, item);
                }

                if (player != null && player.userID.IsSteamId())
                {
                    Puts($"{player.displayName} found the egg");
                    player.GiveItem(item);

                    runEvent();

                    storedData._eggCreated = DateTime.Now.ToString();
                    SaveData();

                    if (_pluginConfig.roamS.IncreaseHealth || _pluginConfig.roamS.IncreaseGather)
                        SetModifiers(player);

                    Subscribe(nameof(OnItemAddedToContainer));
                    Subscribe(nameof(OnItemRemovedFromContainer));
                    if (_pluginConfig.BlockEggCrack)
                        Subscribe(nameof(OnEntityTakeDamage));
                    if (_pluginConfig.tpveS.DisableTruePVE)
                        Subscribe(nameof(CanEntityTakeDamage));
                    if (_pluginConfig.roamS.IncreasePickup)
                        Subscribe(nameof(OnCollectiblePickup));
                    if (_pluginConfig.crateS.crateEvent)
                    {
                        Subscribe(nameof(OnHelicopterDropCrate));
                        Subscribe(nameof(OnCrateDropped));
                        Subscribe(nameof(CanHackCrate));
                    }
                    if (_pluginConfig.UseBlacklist)
                    {
                        Subscribe(nameof(OnPlayerCommand));
                    }

                    storedData.OnEggMove(player.IPlayer);

                    if (_pluginConfig.markerS.playerMarker)
                        server.Broadcast($"{itemName} has been found!! Its location will appear on the map in {_pluginConfig.markerS.InitialMarkerDelay} seconds");
                    else
                        server.Broadcast($"{itemName} has been found!!");

                    if (!String.IsNullOrEmpty(_pluginConfig.sendWebhook) && _pluginConfig.sendWebhook != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks")
                    {
                        new DiscordWebhook()
                        .AddEmbed()
                            .SetDescription($"{itemName.ToUpper()} IS FOUND!")
                            .SetColor(3447003)
                            .AddField("Location", $"{(player.transform.position)}, but will soon be on the move!", true)
                            .SetTimestamp(DateTime.UtcNow)
                            .AddThumbnail(_pluginConfig.customItemS.ItemFoundDiscord)
                            .EndEmbed()
                            .Send(this, _pluginConfig.sendWebhook);
                    }

                    CuiHelper.DestroyUi(player, "EggUI");
                    CuiHelper.AddUi(player, CreateEggUI());

                    var effect = "assets/bundled/prefabs/fx/explosions/water_bomb.prefab";
                    EffectNetwork.Send(new Effect(effect, player, 0, new Vector3(), new Vector3()), player.Connection);

                    timer.Once(10f, () =>
                    {
                        CuiHelper.DestroyUi(player, "EggUI");
                    });

                    delayTimer = timer.Once(_pluginConfig.markerS.InitialMarkerDelay, () =>
                    {
                        var inv = new List<Item>();
                        player.inventory.GetAllItems(inv);
                        if (!inv.Contains(item))
                        {
                            return;
                        }

                        pos = player.transform.position;
                        safezone = false;
                        inPriv = false;
                        pop = false;
                        tod = false;

                        CreateMarkers(player);
                        markerTimer = timer.Repeat(_pluginConfig.markerS.PlayerMarkerRefreshTime, 0, () =>
                        {
                            pos = player.transform.position;
                            CreateMarkers(player);
                        });
                    });
                }
                Unsubscribe(nameof(CanLootEntity));
                return null;
            }

            return null;
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container == null || item == null)
                return;

            switch (item.info.shortname)
            {
                case "metal.refined":
                case "lowgradefuel":
                case "metal.fragments":
                case "scrap":
                case "sulfur":
                case "sulfur.ore":

                    HandleAdds(container, item);
                    break;
            }

            if (item.info.shortname == _pluginConfig.resourceS.CustomItem1
                || item.info.shortname == _pluginConfig.resourceS.CustomItem2
                || item.info.shortname == _pluginConfig.resourceS.CustomItem3
                )
            {
                HandleAdds(container, item);
            }

            if (item.text == "6E657264")
            {
                var playerOwner = container.playerOwner;
                var contOwner = container.entityOwner;

                //if (markerTimer != null) markerTimer.Destroy();
                if (rewardTimer != null) rewardTimer.Destroy();

                if (playerOwner != null && (_pluginConfig.roamS.IncreaseHealth || _pluginConfig.roamS.IncreaseGather))
                {
                    SetModifiers(playerOwner);
                    return;
                }

                if (markerTimer != null)
                {
                    markerTimer.Destroy();
                    markerTimer = null;
                    MarkerDelete();
                }
                pos = contOwner.transform.position;

                if (contOwner != null)
                {
                    var inventory = container.itemList.ToArray();

                    resource = 0;
                    spawnTime = _pluginConfig.resourceS.ResourceSpawnTime;

                    foreach (Item inv in inventory)
                    {
                        var shortName = inv.info.shortname;

                        var isDone = false;
                        switch (shortName)
                        {
                            case "metal.refined":
                            case "lowgradefuel":
                            case "metal.fragments":
                            case "scrap":
                                resource = inv.info.itemid;
                                amount = GetResourceAmount(shortName);
                                cleanName = inv.info.displayName.english;
                                isDone = true;
                                break;

                            case "sulfur":
                                if (_pluginConfig.resourceS.AllowSulfurCooked)
                                {
                                    resource = inv.info.itemid;
                                    amount = GetResourceAmount(shortName);
                                    cleanName = inv.info.displayName.english;
                                    isDone = true;
                                }
                                break;

                            case "sulfur.ore":
                                if (_pluginConfig.resourceS.AllowSulfurOre)
                                {
                                    resource = inv.info.itemid;
                                    amount = GetResourceAmount(shortName);
                                    cleanName = inv.info.displayName.english;
                                    isDone = true;
                                }
                                break;
                        }

                        if (isDone) break;

                        if (shortName == _pluginConfig.resourceS.CustomItem1)
                        {
                            resource = inv.info.itemid;
                            amount = _pluginConfig.resourceS.CustomAmount1;
                            spawnTime = _pluginConfig.resourceS.CustomTime1;
                            cleanName = inv.info.displayName.english;
                            break;
                        }
                        else if (shortName == _pluginConfig.resourceS.CustomItem2)
                        {
                            resource = inv.info.itemid;
                            amount = _pluginConfig.resourceS.CustomAmount2;
                            spawnTime = _pluginConfig.resourceS.CustomTime2;
                            cleanName = inv.info.displayName.english;
                            break;
                        }
                        else if (shortName == _pluginConfig.resourceS.CustomItem3)
                        {
                            resource = inv.info.itemid;
                            amount = _pluginConfig.resourceS.CustomAmount3;
                            spawnTime = _pluginConfig.resourceS.CustomTime3;
                            cleanName = inv.info.displayName.english;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (resource == 0)
                    {
                        resource = -932201673;
                        amount = GetResourceAmount("scrap");
                        cleanName = "Scrap";
                    }

                    priv = false;
                    var ownerID = contOwner.OwnerID;

                    currentOwner = ownerID.ToString();

                    if (contOwner.ShortPrefabName != "small_stash_deployed" && amount != 0)
                    {
                        var player = ownerID.IsSteamId() ? BasePlayer.FindAwakeOrSleeping(Convert.ToString(ownerID)) : null;

                        if (player != null && _pluginConfig.NoPrivTime)
                            priv = container.entityOwner?.GetBuildingPrivilege()?.IsAuthed(player) ?? false;

                        if (player != null && player.currentTeam != 0UL)
                        {
                            var teamMembers = player.Team.members.ToList();

                            foreach (var member in teamMembers)
                            {
                                if (!String.IsNullOrEmpty(cleanName))
                                {
                                    var teamPlayer = players.FindPlayerById(Convert.ToString(member));
                                    if (teamPlayer.IsConnected)
                                    {
                                        var bPlayer = BasePlayer.FindByID(member);
                                        bPlayer.ChatMessage(String.Format(GetMsg("Generating", Convert.ToString(bPlayer.userID)), cleanName));
                                    }
                                }
                            }
                        }

                        if (player != null && player.currentTeam == 0 && amount != 0)
                        {
                            if (!String.IsNullOrEmpty(cleanName)) player.ChatMessage(String.Format(GetMsg("Generating", Convert.ToString(player.userID)), cleanName));
                        }
                    }

                    if (_pluginConfig.TimeOfDay)
                    {
                        tod = false;
                        TimeSpan tsStart = DateTime.Parse(_pluginConfig.StartTime).TimeOfDay;
                        TimeSpan tsEnd = DateTime.Parse(_pluginConfig.EndTime).TimeOfDay;

                        if (IsBetween(tsStart, tsEnd))
                            tod = true;
                    }

                    var playerI = players.FindPlayerById(Convert.ToString(ownerID));

                    if (playerI != null)
                        currentOwner = playerI.Id;

                    if (contOwner.ShortPrefabName != "small_stash_deployed" || (contOwner.ShortPrefabName == "small_stash_deployed" && !_pluginConfig.BlockStashTime))
                    {
                        if (playerI != null && !priv && !tod)
                        {
                            if (_pluginConfig.ServerPop == 0 || (covalence.Server.Players + ConVar.Admin.ServerInfo().Joining >= _pluginConfig.ServerPop))
                                storedData.OnEggMove(playerI);
                        }

                        if (rewardTimer != null) rewardTimer.Destroy();
                        startTime = DateTime.Now;
                        rewardTimer = timer.Repeat(spawnTime, 0, () =>
                        {
                            SpawnRewards(contOwner, resource, amount);
                            startTime = DateTime.Now;
                        });
                    }
                }
                if (_pluginConfig.markerS.boxMarker)
                {
                    CreateMarkers();
                    markerTimer = timer.Repeat(_pluginConfig.markerS.BoxMarkerRefreshTime, 0, () =>
                    {
                        /*                         if (contOwner == null)
                                                {
                                                    if (markerTimer != null) markerTimer.Destroy();
                                                    MarkerDelete();
                                                    markerTimer = null;
                                                }
                                                else
                                                { */
                        pos = contOwner.transform.position;
                        CreateMarkers();
                        //}
                    });
                }
                else
                {
                    if (markerTimer != null)
                    {
                        markerTimer.Destroy();
                        markerTimer = null;
                    }
                    MarkerDelete();
                }
            }
            return;
        }

        void HandleAdds(ItemContainer container, Item item)
        {
            var contOwner = container.entityOwner;

            if (contOwner != null && contOwner.ShortPrefabName != "small_stash_deployed")
            {
                var inventory = container.itemList.ToArray();
                var itemID = item.info.itemid;

                foreach (Item inv in inventory)
                {
                    if (inv.text == "6E657264")
                    {
                        if ((!_pluginConfig.resourceS.AllowSulfurCooked && item.info.shortname == "sulfur") || (!_pluginConfig.resourceS.AllowSulfurOre && item.info.shortname == "sulfur.ore")) break;

                        cleanName = item.info.displayName.english;
                        var ownerID = contOwner.OwnerID;

                        amount = GetResourceAmount(item.info.shortname);

                        var player = ownerID.IsSteamId() ? BasePlayer.FindAwakeOrSleeping(Convert.ToString(ownerID)) : null;

                        if (player != null && player.currentTeam != 0UL && amount != 0)
                        {
                            var teamMembers = player.Team.members.ToList();

                            foreach (var member in teamMembers)
                            {
                                var teamPlayer = players.FindPlayerById(Convert.ToString(member));
                                if (teamPlayer.IsConnected)
                                {
                                    var bPlayer = BasePlayer.FindByID(member);
                                    bPlayer.ChatMessage(String.Format(GetMsg("Generating", Convert.ToString(bPlayer.userID)), cleanName));
                                }
                            }
                        }

                        if (player != null && player.currentTeam == 0 && amount != 0)
                        {
                            player.ChatMessage(String.Format(GetMsg("Generating", Convert.ToString(player.userID)), cleanName));
                        }

                        spawnTime = GetSpawnTime(item.info.shortname);

                        if (rewardTimer != null) rewardTimer.Destroy();
                        startTime = DateTime.Now;
                        rewardTimer = timer.Repeat(spawnTime, 0, () =>
                        {
                            SpawnRewards(contOwner, itemID, amount);
                            startTime = DateTime.Now;
                        });

                        break;
                    }
                }
            }

            return;
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            Item eggItem;
            _spawnedItems.TryGetValue("6E657264", out eggItem);

            if (eggItem == null) return null;

            if (eggItem.GetOwnerPlayer() != null && eggItem.GetOwnerPlayer() == player && (action.Equals("consume")) && (item.info.shortname.StartsWith("maxhealthtea") || item.info.shortname.StartsWith("oretea") || item.info.shortname.StartsWith("woodtea"))) return true;
            if (item?.info?.shortname == null || item.text != "6E657264" || action != "unwrap") return null;

            if (_pluginConfig.BlockEggCrack && blockEggOpen)
            {
                var placeHolder = itemName != "The Golden Egg" ? "item" : "egg";

                player.ChatMessage(String.Format(GetMsg("Blocked", Convert.ToString(player.userID)), placeHolder));
                return true;
            }

            if (player != null && _pluginConfig.roamS.IncreaseHealth)
            {
                player.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Max_Health,
                        value = 0,
                        duration = 0.1f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
                player.modifiers.SendChangesToClient();
            }

            if (player != null && _pluginConfig.roamS.IncreaseGather)
            {
                player.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Ore_Yield,
                        value = 0,
                        duration = 0.1f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
                player.modifiers.SendChangesToClient();
            }

            storedData.DestroyTimers();

            if (markerTimer != null)
            {
                markerTimer.Destroy();
                markerTimer = null;
            }
            MarkerDelete();

            if (_pluginConfig.UseCustomList)
            {
                int index = UnityEngine.Random.Range(0, _pluginConfig.EggRewards.Count);

                try
                {
                    item.Remove();
                    player.SendNetworkUpdateImmediate();

                    var effect = "assets/bundled/prefabs/fx/gestures/eat_soft.prefab";
                    EffectNetwork.Send(new Effect(effect, player, 0, new Vector3(), new Vector3()), player.Connection);

                    var reward = ItemManager.CreateByName(_pluginConfig.EggRewards[index], 1);
                    player.GiveItem(reward);

                    return true;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            if (container == null || item?.info?.shortname == null || item.text != "6E657264")
                return;

            var playerOwner = container.playerOwner;
            var newplayercontainer = item.GetOwnerPlayer();
            var contOwner = container.entityOwner;

            storedData.DestroyTimers();

            if (rewardTimer != null) rewardTimer.Destroy();
            if (delayTimer != null) delayTimer.Destroy();

            if (playerOwner != null && _pluginConfig.roamS.IncreaseHealth)
            {
                playerOwner.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Max_Health,
                        value = 0,
                        duration = 0.1f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
                playerOwner.modifiers.SendChangesToClient();
            }

            if (playerOwner != null && _pluginConfig.roamS.IncreaseGather)
            {
                playerOwner.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Ore_Yield,
                        value = 0,
                        duration = 0.1f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
                playerOwner.modifiers.SendChangesToClient();
            }

            NextTick(() =>
            {
                var player = item.GetOwnerPlayer();

                if (player != null)
                {
                    var playerI = players.FindPlayerById(Convert.ToString(player.userID));

                    storedData.OnEggMove(playerI);

                    if (markerTimer != null)
                    {
                        markerTimer.Destroy();
                        markerTimer = null;
                    }

                    if (delayTimer != null) delayTimer.Destroy();
                    MarkerDelete();

                    pos = player.transform.position;
                    safezone = false;
                    inPriv = false;
                    pop = false;
                    tod = false;

                    CreateMarkers(player);

                    markerTimer = timer.Repeat(_pluginConfig.markerS.PlayerMarkerRefreshTime, 0, () =>
                    {
                        pos = player.transform.position;
                        CreateMarkers(player);
                    });
                }
            });
        }

        public static bool IsBetween(TimeSpan start, TimeSpan end)
        {
            var time = DateTime.Now.TimeOfDay;
            // Start time and end time are in the same day
            if (start <= end)
                return time >= start && time <= end;
            // Start time and end time are on different days
            return time >= start || time <= end;
        }

        private void SetModifiers(BasePlayer player)
        {
            if (_pluginConfig.roamS.RoamTod)
            {
                TimeSpan tsStart = DateTime.Parse(_pluginConfig.roamS.RoamStart).TimeOfDay;
                TimeSpan tsEnd = DateTime.Parse(_pluginConfig.roamS.RoamEnd).TimeOfDay;

                if (IsBetween(tsStart, tsEnd))
                {
                    player.ChatMessage(String.Format(GetMsg("RoamDisabledTod", player.UserIDString), _pluginConfig.roamS.RoamStart, _pluginConfig.roamS.RoamEnd));
                    return;
                }
            }

            if (_pluginConfig.roamS.RoamPop != 0 && (covalence.Server.Players + ConVar.Admin.ServerInfo().Joining < _pluginConfig.roamS.RoamPop))
            {
                player.ChatMessage(String.Format(GetMsg("RoamDisabledPop", player.UserIDString), _pluginConfig.roamS.RoamPop));
                return;
            }

            if (_pluginConfig.roamS.IncreaseHealth)
            {
                if (player == null || player.modifiers == null) return;
                var startHealth = player.StartMaxHealth();
                var maxHealth = _pluginConfig.roamS.TotalHealth;
                var healthMultiplier = (maxHealth - startHealth) / startHealth;

                /*                 float healthIncrease;
                                if ((healthIncrease = player.modifiers.GetValue(Modifier.ModifierType.Max_Health, -1000f)) > -1000f &&
                                    Math.Abs(healthIncrease - healthMultiplier) > 0.1f) healthMultiplier += healthIncrease; */

                player.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Max_Health,
                        value = healthMultiplier,
                        duration = 2600f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
                player.modifiers.SendChangesToClient();
            }

            if (_pluginConfig.roamS.IncreaseGather)
            {
                player.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Ore_Yield,
                        value = (_pluginConfig.roamS.GatherMultiplier / 2),
                        duration = 2600f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
                player.modifiers.SendChangesToClient();
            }
            return;
        }

        object OnItemPickup(Item item, BasePlayer player)
        {
            if (item == null || player == null || item.text != "6E657264") return null;

            currentOwner = player.UserIDString;

            player.ChatMessage(String.Format(GetMsg("Pickup", Convert.ToString(player.userID)), itemName));

            if (_pluginConfig.roamS.IncreaseHealth || _pluginConfig.roamS.IncreaseGather)
                SetModifiers(player);

            SaveDataTimer();

            priv = false;

            if (player != null && _pluginConfig.NoPrivTime)
                priv = player?.GetBuildingPrivilege()?.IsAuthed(player) ?? false;

            if (!priv)
            {
                if (_pluginConfig.ServerPop == 0 || (covalence.Server.Players + ConVar.Admin.ServerInfo().Joining >= _pluginConfig.ServerPop))
                {
                    storedData.OnEggMove(player.IPlayer);
                }
            }

            if (markerTimer != null)
            {
                markerTimer.Destroy();
                markerTimer = null;
            }
            if (delayTimer != null) delayTimer.Destroy();
            MarkerDelete();

            pos = player.transform.position;
            safezone = false;
            inPriv = false;
            pop = false;
            tod = false;

            CreateMarkers(player);
            markerTimer = timer.Repeat(_pluginConfig.markerS.PlayerMarkerRefreshTime, 0, () =>
            {
                pos = player.transform.position;
                CreateMarkers(player);
            });

            return null;
        }

        void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item == null || entity == null || item.text != "6E657264") return;

            if (markerTimer != null)
            {
                markerTimer.Destroy();
                markerTimer = null;
            }

            if (delayTimer != null) delayTimer.Destroy();
            //MarkerDelete();

            storedData.DestroyTimers();

            var player = BasePlayer.FindAwakeOrSleeping(currentOwner);

            if (player != null && _pluginConfig.roamS.IncreaseHealth)
            {
                player.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Max_Health,
                        value = 0,
                        duration = 0.1f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
            }

            if (player != null && _pluginConfig.roamS.IncreaseGather)
            {
                player.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Ore_Yield,
                        value = 0,
                        duration = 0.1f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
            }
            player.modifiers.SendChangesToClient();
        }

        object OnEntityKill(HackableLockedCrate crate)
        {
            if (crate == null || _spawnedCrates.Count == 0)
                return null;

            if (_spawnedCrates.ContainsKey(crate.net.ID.Value))
            {
                _spawnedCrates.Remove(crate.net.ID.Value);

                if (crateID.Contains(crate.net.ID.Value))
                    crateID.Remove(crate.net.ID.Value);

                if (_spawnedCrates.Count == 0)
                {
                    chinookID = 0;
                    if (_marker != null)
                    {
                        _marker.Kill();
                        _marker.SendUpdate();
                    }
                    if (crateMarkTimer != null) crateMarkTimer.Destroy();
                }
            }

            return null;
        }

        object OnItemRemove(Item item)
        {
            if (item.text != "6E657264" || item == null) return null;

            eggUID = 0;
            storedData._eggCreated = "";
            SaveData();

            if (_spawnedItems != null && _spawnedItems.ContainsKey("6E657264"))
            {
                _spawnedItems.Remove("6E657264");
            }

            var player = BasePlayer.FindAwakeOrSleeping(currentOwner);

            if (player != null && _pluginConfig.roamS.IncreaseHealth)
            {
                player.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Max_Health,
                        value = 0,
                        duration = 0.1f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
            }

            if (player != null && _pluginConfig.roamS.IncreaseGather)
            {
                player.modifiers.Add(new List<ModifierDefintion>
                {
                    new ModifierDefintion
                    {
                        type = Modifier.ModifierType.Ore_Yield,
                        value = 0,
                        duration = 0.1f,
                        source = Modifier.ModifierSource.Tea
                    }
                });
            }
            if (player != null)
                player.modifiers.SendChangesToClient();

            Subscribe(nameof(CanLootEntity));
            Unsubscribe(nameof(OnItemAddedToContainer));
            Unsubscribe(nameof(OnItemRemovedFromContainer));
            Unsubscribe(nameof(OnItemPickup));
            Unsubscribe(nameof(OnItemDropped));
            if (_pluginConfig.tpveS.DisableTruePVE) Unsubscribe(nameof(CanEntityTakeDamage));
            if (_pluginConfig.BlockEggCrack) Unsubscribe(nameof(OnEntityTakeDamage));
            if (_pluginConfig.roamS.IncreasePickup)
                Unsubscribe(nameof(OnCollectiblePickup));
            if (_pluginConfig.crateS.crateEvent)
            {
                Unsubscribe(nameof(OnHelicopterDropCrate));
                Unsubscribe(nameof(OnCrateDropped));
                Unsubscribe(nameof(CanHackCrate));
            }
            if (_pluginConfig.UseBlacklist)
            {
                Unsubscribe(nameof(OnPlayerCommand));
            }

            server.Broadcast($"{itemName} has been lost!! It can now be found again in a random crate");

            if (!String.IsNullOrEmpty(_pluginConfig.sendWebhook) && _pluginConfig.sendWebhook != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks")
            {
                new DiscordWebhook()
                .AddEmbed()
                    .SetDescription($"{itemName.ToUpper()} IS LOST!")
                    .SetColor(3447003)
                    .AddField("It can now be found in crates", "Good luck!", true)
                    .SetTimestamp(DateTime.UtcNow)
                    .AddThumbnail(_pluginConfig.customItemS.ItemLostDiscord)
                    .EndEmbed()
                    .Send(this, _pluginConfig.sendWebhook);
            }

            NextTick(() =>
            {
                storedData.DestroyTimers();
                if (rewardTimer != null) rewardTimer.Destroy();
                if (markerTimer != null)
                {
                    markerTimer.Destroy();
                    markerTimer = null;
                }
                if (delayTimer != null) delayTimer.Destroy();
                if (blockTimer != null) blockTimer.Destroy();
                if (crateEventTimer != null) crateEventTimer.Destroy();
                MarkerDelete();

                currentOwner = "";
            });

            return null;
        }

        private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (player == null || collectible == null) return;

            Item eggItem;
            _spawnedItems.TryGetValue("6E657264", out eggItem);

            if (eggItem == null) return;

            if (eggItem.GetOwnerPlayer() == null || eggItem.GetOwnerPlayer() != player)
                return;

            if (_pluginConfig.roamS.RoamTod)
            {
                TimeSpan tsStart = DateTime.Parse(_pluginConfig.roamS.RoamStart).TimeOfDay;
                TimeSpan tsEnd = DateTime.Parse(_pluginConfig.roamS.RoamEnd).TimeOfDay;

                if (IsBetween(tsStart, tsEnd))
                {
                    return;
                }
            }

            if (_pluginConfig.roamS.RoamPop != 0 && (covalence.Server.Players + ConVar.Admin.ServerInfo().Joining < _pluginConfig.roamS.RoamPop))
                return;

            foreach (ItemAmount item in collectible.itemList)
            {
                float modifier = _pluginConfig.roamS.PickupMultiplier;
                item.amount = (int)(item.amount * modifier);
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (eggUID == 0 || entity == null || hitInfo == null || entity.IsNpc || entity is BasePlayer || hitInfo.Initiator == null || hitInfo.Initiator.transform == null || hitInfo?.InitiatorPlayer == null)
                return;

            if (!(CheckDamage(entity, hitInfo))) return;
            if (hitInfo.InitiatorPlayer.userID == entity.OwnerID) return;

            blockEggOpen = true;

            if (blockTimer != null) blockTimer.Destroy();
            {
                blockTimer = timer.Once(_pluginConfig.BlockInterval, () =>
                {
                    blockEggOpen = false;
                    buildID.Clear();
                });
            }
        }

        object CanEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (eggUID == 0 || hitInfo == null || entity.IsNpc || entity?.transform == null || hitInfo?.Initiator == null || hitInfo?.InitiatorPlayer == null)
                return null;

            if (CheckDamage(entity, hitInfo))
            {
                OnProcessPlayerEntity(entity, hitInfo);
                return (object)true;
            }

            return null;
        }

        object OnProcessPlayerEntity(BaseEntity entity, HitInfo info)
        {
            return true;
        }

        bool CheckDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            Item item;
            _spawnedItems.TryGetValue("6E657264", out item);

            if (item == null) return false;

            var eggOwner = item.GetEntityOwner();

            Vector3 eggPos = eggOwner != null ? eggOwner.transform.position : pos;

            float dist = Vector3.Distance(hitInfo.PointEnd, eggPos);

            if (!(entity is BasePlayer))
            {
                BuildingPrivlidge privilege = entity?.GetBuildingPrivilege() ?? null;

                if (privilege == null) return false;

                if (buildID != null && buildID.Contains(privilege.buildingID))
                {
                    return true;
                }

                if (CheckOwner(entity, hitInfo.InitiatorPlayer, privilege))
                {
                    if (item.GetOwnerPlayer() != null && item.GetOwnerPlayer() == hitInfo?.InitiatorPlayer)
                    {
                        return false;
                    }
                    if (hitInfo.InitiatorPlayer.UserIDString == currentOwner)
                    {
                        return false;
                    }

                    if (!buildID.Contains(privilege.buildingID))
                        buildID.Add(privilege.buildingID);
                    return true;
                }
            }

            if ((entity is BasePlayer))
            {
                var nearbyTargets = Pool.GetList<BasePlayer>();
                Vis.Entities(entity.transform.position, _pluginConfig.tpveS.DamageDistance, nearbyTargets, blockLayer);

                if (nearbyTargets.Count > 0 && dist <= _pluginConfig.tpveS.DamageDistance)
                {
                    foreach (BasePlayer nearbyTarget in nearbyTargets)
                    {
                        if (nearbyTarget.IsNpc) continue;
                        if (nearbyTarget == entity && nearbyTarget.UserIDString == currentOwner)
                        {
                            if (hitInfo.ProjectileDistance > _pluginConfig.tpveS.DamageDistance)
                            {
                                Pool.FreeList(ref nearbyTargets);
                                return false;
                            }
                        }
                        if (nearbyTarget.UserIDString != currentOwner && hitInfo.InitiatorPlayer.UserIDString != currentOwner) continue;

                        Pool.FreeList(ref nearbyTargets);
                        return true;
                    }
                }

                Pool.FreeList(ref nearbyTargets);
                return false;
            }
            return false;
        }

        bool CheckOwner(BaseEntity target, BasePlayer source, BuildingPrivlidge priv = null)
        {
            if (target.OwnerID.IsSteamId())
            {
                BuildingPrivlidge privilege = target.GetBuildingPrivilege();
                if (privilege == null) return false;

                Item item;
                _spawnedItems.TryGetValue("6E657264", out item);

                if (item == null) return false;

                var eggOwner = item.GetEntityOwner();

                if (eggOwner == null) return false;

                var eggPriv = eggOwner.GetBuildingPrivilege();
                if (eggPriv == null) return false;

                var priv1 = eggPriv.buildingID;
                var priv2 = priv.buildingID;

                var authlist = privilege.authorizedPlayers.Select(x => x.userid).ToList();

                foreach (var auth in authlist)
                {
                    if (Convert.ToString(auth) == currentOwner && priv1 == priv2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private List<StoredData.UserData> leaderList = new List<StoredData.UserData>();

        private void CmdRunCommand(IPlayer player, string cmd, string[] args)
        {
            if (args.Length >= 1)
            {
                switch (args[0].ToLower())
                {
                    case "help":
                        {
                            var placeHolder = itemName != "The Golden Egg" ? "item" : "egg";

                            if (_pluginConfig.markerS.playerMarker)
                                player.Message(GetMsg("Help1", player.Id));
                            player.Message(String.Format(GetMsg("Help2", player.Id), placeHolder));
                            player.Message(String.Format(GetMsg("Help3", player.Id), placeHolder));
                            if (_pluginConfig.roamS.IncreaseGather || _pluginConfig.roamS.IncreaseHealth || _pluginConfig.roamS.IncreasePickup)
                                player.Message(String.Format(GetMsg("Help9", player.Id), placeHolder));
                            player.Message(String.Format(GetMsg("Help4", player.Id), _pluginConfig.customItemS.Command));
                            player.Message(String.Format(GetMsg("Help5", player.Id), _pluginConfig.customItemS.Command));
                            if (_pluginConfig.PinpointEggCommand)
                                player.Message(String.Format(GetMsg("Help6", player.Id), _pluginConfig.customItemS.Command, placeHolder));
                            if (_pluginConfig.KillTime != 0)
                                player.Message(String.Format(GetMsg("Help10", player.Id), _pluginConfig.customItemS.Command, placeHolder));
                            player.Message(String.Format(GetMsg("Help7", player.Id), _pluginConfig.customItemS.Command));
                            if (player.IsAdmin)
                            {
                                player.Message(String.Format(GetMsg("Help8", player.Id), _pluginConfig.customItemS.Command, placeHolder));
                                player.Message(String.Format(GetMsg("Help11", player.Id), _pluginConfig.customItemS.Command));
                                player.Message(String.Format(GetMsg("Help12", player.Id), _pluginConfig.customItemS.Command, placeHolder));
                                player.Message(String.Format(GetMsg("Help13", player.Id), _pluginConfig.customItemS.Command, placeHolder));
                            }
                        }
                        return;

                    case "find":
                        {
                            if (eggUID == 0) return;

                            if (!player.HasPermission(permissionUse))
                            {
                                player.Message(GetMsg("Perms", player.Id));
                                return;
                            }

                            if (_pluginConfig.PinpointEggCommand)
                            {
                                var basePlayer = player.Object as BasePlayer;
                                double distance = Math.Round(Vector3.Distance(basePlayer.transform.position, pos), 2);
                                EggText(basePlayer, pos, String.Format(GetMsg("DisplayText", player.Id), itemName));
                            }
                        }
                        return;

                    case "time":
                        {
                            if (eggUID == 0 || amount == 0) return;

                            TimeSpan t = DateTime.Now - startTime;
                            var countdown = new TimeSpan(0, 0, spawnTime).Subtract(t).ToString(@"hh\:mm\:ss");

                            if (String.IsNullOrEmpty(cleanName)) cleanName = "Resource";
                            player.Message(string.Format(GetMsg("Generation", player.Id), amount, cleanName, countdown));
                        }
                        return;

                    case "list":
                        {
                            var resourceList = new List<string>();

                            if (_pluginConfig.resourceS.ScrapSpawnAmount != 0)
                                resourceList.Add($"Scrap ({TimeSpan.FromSeconds(_pluginConfig.resourceS.ResourceSpawnTime)})");
                            if (_pluginConfig.resourceS.HQMSpawnAmount != 0)
                                resourceList.Add($"High Quality Metal ({TimeSpan.FromSeconds(_pluginConfig.resourceS.ResourceSpawnTime)})");
                            if (_pluginConfig.resourceS.LowGradeSpawnAmount != 0)
                                resourceList.Add($"Low Grade Fuel ({TimeSpan.FromSeconds(_pluginConfig.resourceS.ResourceSpawnTime)})");
                            if (_pluginConfig.resourceS.FragsSpawnAmount != 0)
                                resourceList.Add($"Metal Fragments ({TimeSpan.FromSeconds(_pluginConfig.resourceS.ResourceSpawnTime)})");
                            if (_pluginConfig.resourceS.AllowSulfurCooked)
                                resourceList.Add($"Cooked Sulfur ({TimeSpan.FromSeconds(_pluginConfig.resourceS.ResourceSpawnTime)})");
                            if (_pluginConfig.resourceS.AllowSulfurOre)
                                resourceList.Add($"Sulfur Ore ({TimeSpan.FromSeconds(_pluginConfig.resourceS.ResourceSpawnTime)})");
                            if (!string.IsNullOrEmpty(_pluginConfig.resourceS.CustomItem1))
                                resourceList.Add($"{_pluginConfig.resourceS.CustomItem1} ({TimeSpan.FromSeconds(_pluginConfig.resourceS.CustomTime1)})");
                            if (!string.IsNullOrEmpty(_pluginConfig.resourceS.CustomItem2))
                                resourceList.Add($"{_pluginConfig.resourceS.CustomItem2} ({TimeSpan.FromSeconds(_pluginConfig.resourceS.CustomTime2)})");
                            if (!string.IsNullOrEmpty(_pluginConfig.resourceS.CustomItem3))
                                resourceList.Add($"{_pluginConfig.resourceS.CustomItem3} ({TimeSpan.FromSeconds(_pluginConfig.resourceS.CustomTime3)})");

                            player.Message(string.Format(GetMsg("Resource", player.Id), String.Join(", ", resourceList)));
                        }
                        return;

                    case "leader":
                        {
                            string message = GetMsg("EggLeaderboard", player.Id);
                            storedData.GetEggLeaders(leaderList);

                            for (int i = 0; i < Math.Min(5, leaderList.Count); i++)
                            {
                                message += string.Format(GetMsg("LeaderList"), leaderList[i].displayName, FormatTime(leaderList[i].eggtime), FormatTime(leaderList[i].TeamTotal));
                            }
                            player.Message(message);
                        }
                        return;

                    case "kill":
                        {
                            if (!player.IsAdmin)
                            {
                                return;
                            }

                            if (_spawnedItems != null)
                            {
                                foreach (var _item in _spawnedItems.ToList())
                                {
                                    if (_item.Key == "6E657264")
                                    {
                                        Item item = _item.Value;
                                        if (item != null)
                                        {
                                            item.Remove();
                                            _spawnedItems.Remove(_item.Key);
                                        }
                                    }
                                }
                            }
                        }
                        return;

                    case "expire":
                        {
                            if (eggUID == 0) return;

                            if (_pluginConfig.KillTime == 0) return;

                            var futureDate = (DateTime.Parse((string)storedData._eggCreated)).AddMinutes(_pluginConfig.KillTime);
                            var minutes = (futureDate - DateTime.Now).TotalMinutes;

                            var days = ((int)minutes / 1440);
                            var hours = ((int)minutes % 1440) / 60;
                            var mins = (int)minutes % 60;

                            player.Message(string.Format(GetMsg("Expire", player.Id), itemName, days, hours, mins));
                        }
                        return;

                    case "event":
                        {
                            if (!player.IsAdmin)
                                return;

                            if (chinookID != 0)
                            {
                                Puts("The chinook event is already running");
                                return;
                            }

                            crateID.Clear();
                            _spawnedCrates.Clear();

                            if (_marker != null)
                            {
                                _marker.Kill();
                                _marker.SendUpdate();
                            }
                            if (crateMarkTimer != null)
                                crateMarkTimer.Destroy();

                            var chinook = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab") as CH47HelicopterAIController;
                            if (chinook == null)
                            {
                                return;
                            }
                            chinook.numCrates = _pluginConfig.crateS.numCrates;

                            chinook.TriggeredEventSpawn();
                            chinook.Spawn();

                            chinookID = chinook.net.ID.Value;
                        }
                        return;

                    case "winner":
                        {
                            if (!player.IsAdmin)
                                return;

                            if (string.IsNullOrEmpty(_pluginConfig.GroupName) || string.IsNullOrEmpty(_pluginConfig.GroupDuration))
                            {
                                Logger.Info("Either the group name or duration is blank");
                                return;
                            }

                            storedData.GetEggLeaders(leaderList);
                            if (leaderList.Count == 0) return;

                            var leader = leaderList.First();
                            if (leader == null) return;

                            foreach (var user in storedData._eggPossession)
                            {
                                if (storedData._eggPossession[user.Key] == leader)
                                {
                                    if (user.Value.Team?.Count > 0)
                                    {
                                        foreach (var teamMember in user.Value.Team)
                                        {
                                            AddToGroupTimed(teamMember.ToString(), _pluginConfig.GroupName, _pluginConfig.GroupDuration);
                                        }
                                    }
                                    else
                                    {
                                        AddToGroupTimed(user.Key.ToString(), _pluginConfig.GroupName, _pluginConfig.GroupDuration);
                                    }
                                }
                            }
                            server.Broadcast(String.Format(GetMsg("Winner"), leader.displayName, itemName));
                        }
                        return;

                    case "purge":
                        {
                            if (!player.IsAdmin)
                                return;

                            if (_spawnedItems != null)
                            {
                                foreach (var _item in _spawnedItems.ToList())
                                {
                                    if (_item.Key == "6E657264")
                                    {
                                        Item item = _item.Value;
                                        if (item != null)
                                        {
                                            item.Remove();
                                            _spawnedItems.Remove(_item.Key);
                                        }
                                    }
                                }
                            }

                            data.Clear();
                            data.Save();
                            storedData = data.ReadObject<StoredData>();
                            if (storedData == null)
                            {
                                storedData = new StoredData();
                            }

                            Log("Egg destroyed and player data cleared");
                        }
                        return;
                }
            }
        }

        private void AddToGroupTimed(string userId, string groupName, string duration)
        {
            if (TimedPermissions == null)
            {
                LogError($"Unable to add user {userId} to group {groupName} because TimedPermissions is not loaded.");
                return;
            }

            server.Command($"addgroup {userId} {groupName} {duration}");
        }

        object OnHelicopterDropCrate(CH47HelicopterAIController heli)
        {
            if (heli.net.ID.Value == chinookID)
            {
                crateDrop = true;
                timer.In(0.5f, () =>
                {
                    heli.DropCrate();
                });

                if (heli.numCrates == 1)
                {
                    server.Broadcast(String.Format(GetMsg("EventStart"), _pluginConfig.crateS.numCrates, itemName));
                    runEvent();
                }
            }

            return null;
        }

        void OnCrateDropped(HackableLockedCrate crate)
        {
            if (crate == null)
                return;

            NextTick(() =>
            {
                if (crateDrop == true)
                {
                    crateID.Add(crate.net.ID.Value);

                    if (!_spawnedCrates.ContainsKey(crate.net.ID.Value))
                        _spawnedCrates.Add(crate.net.ID.Value, crate);

                    crate.shouldDecay = true;

                    if (_pluginConfig.crateS.crateMarker && crateID.Count == 1)
                    {
                        cratePos = crate.ServerPosition;
                        SpawnMarker();
                        crateMarkTimer = timer.Repeat(30, 0, () =>
                        {
                            SpawnMarker();
                        });
                    }

                    timer.In(8f, () =>
                    {
                        crateDrop = false;
                    });
                }
            });
        }

        private static Color GetColor(string hex)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.yellow;
        }

        public void SpawnMarker()
        {
            if (_marker != null)
            {
                _marker.Kill();
                _marker.SendUpdate();
            }

            if (crateID.Count == 0)
                return;

            HackableLockedCrate crate;
            _spawnedCrates.TryGetValue(crateID[0], out crate);

            if (crate == null) return;

            _marker = (MapMarkerGenericRadius)GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", cratePos);
            _marker.color1 = GetColor(_pluginConfig.crateS.crateColor1);
            _marker.color2 = GetColor(_pluginConfig.crateS.crateColor2);
            _marker.alpha = _pluginConfig.crateS.crateMarkerAlpha;
            _marker.radius = _pluginConfig.crateS.crateMarkerRadius;
            _marker.Spawn();
            _marker.SetParent(crate);
            _marker.transform.localPosition = Vector3.zero;
            _marker.SendUpdate();
        }

        object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null || crate == null)
                return null;

            if (crateID.Contains(crate.net.ID.Value))
            {
                Item eggItem;
                _spawnedItems.TryGetValue("6E657264", out eggItem);

                if (eggItem == null) return null;

                var inv = new List<Item>();
                player.inventory.GetAllItems(inv);
                if (inv.Contains(eggItem))
                {
                    crate.hackSeconds = HackableLockedCrate.requiredHackSeconds - _pluginConfig.crateS.crateTimer;

                    if (_pluginConfig.crateS.addedItems > 0)
                    {
                        crate.inventory.capacity = 35;
                        for (int i = 0; i < _pluginConfig.crateS.addedItems; i++)
                        {
                            if (crate.lootDefinition != null)
                                crate.lootDefinition.SpawnIntoContainer(crate.inventory);
                        }
                    }

                    server.Broadcast(String.Format(GetMsg("CrateHacked"), _pluginConfig.crateS.crateTimer));

                    return null;
                }

                player.ChatMessage(String.Format(GetMsg("EggHack", Convert.ToString(player.userID)), itemName));
                return false;
            }
            return null;
        }

        private static string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            int days = dateDifference.Days;
            int hours = dateDifference.Hours;
            hours += (days * 24);
            return string.Format("{0:00}h:{1:00}m:{2:00}s", hours, dateDifference.Minutes, dateDifference.Seconds);
        }

        private void AddCommands()
        {
            if (String.IsNullOrEmpty(_pluginConfig.customItemS.Command))
            {
                _pluginConfig.customItemS.Command = "egg";
                SaveConfig();
            }

            AddCovalenceCommand(_pluginConfig.customItemS.Command, nameof(CmdRunCommand));
        }

        void EggText(BasePlayer player, Vector3 eggPos, string text)
        {
            if (!player || !player.IsConnected || eggPos == Vector3.zero || string.IsNullOrEmpty(text))
                return;

            bool isAdmin = player.IsAdmin;

            try
            {
                if (!player.IsAdmin)
                {
                    var uid = player.userID;

                    if (!allowDraw.Contains(uid))
                    {
                        allowDraw.Add(uid);
                        timer.Once(15f, () => allowDraw.Remove(uid));
                    }

                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                    player.SendNetworkUpdateImmediate();
                }

                if (player.IsAdmin || allowDraw.Contains(player.userID))
                    player.SendConsoleCommand("ddraw.text", 15f, Color.yellow, eggPos, text);
            }
            catch (Exception ex)
            {
                Puts("Error drawing eggtext: {0} -- {1}", ex.Message, ex.StackTrace);
            }

            if (!isAdmin)
            {
                if (player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin))
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    player.SendNetworkUpdateImmediate();
                }
            }
        }

        private string GetMsg(string key, string userId = null, params object[] args) => lang.GetMessage(key, this, userId);

        private int GetSpawnTime(string shortname)
        {
            int spawnTime = 0;

            if (shortname == _pluginConfig.resourceS.CustomItem1)
                spawnTime = _pluginConfig.resourceS.CustomTime1;
            else if (shortname == _pluginConfig.resourceS.CustomItem2)
                spawnTime = _pluginConfig.resourceS.CustomTime2;
            else if (shortname == _pluginConfig.resourceS.CustomItem3)
                spawnTime = _pluginConfig.resourceS.CustomTime3;
            else spawnTime = _pluginConfig.resourceS.ResourceSpawnTime;

            return spawnTime;
        }

        private int GetResourceAmount(string shortname)
        {
            int resourceAmount = 0;

            if (shortname == "metal.refined")
                resourceAmount = _pluginConfig.resourceS.HQMSpawnAmount;
            else if (shortname == "lowgradefuel")
                resourceAmount = _pluginConfig.resourceS.LowGradeSpawnAmount;
            else if (shortname == "metal.fragments")
                resourceAmount = _pluginConfig.resourceS.FragsSpawnAmount;
            else if (shortname == "scrap")
                resourceAmount = _pluginConfig.resourceS.ScrapSpawnAmount;
            else if (shortname == "sulfur")
                resourceAmount = _pluginConfig.resourceS.SulfurCookedAmount;
            else if (shortname == "sulfur.ore")
                resourceAmount = _pluginConfig.resourceS.SulfurOreAmount;
            else if (shortname == _pluginConfig.resourceS.CustomItem1)
                resourceAmount = _pluginConfig.resourceS.CustomAmount1;
            else if (shortname == _pluginConfig.resourceS.CustomItem2)
                resourceAmount = _pluginConfig.resourceS.CustomAmount2;
            else if (shortname == _pluginConfig.resourceS.CustomItem3)
                resourceAmount = _pluginConfig.resourceS.CustomAmount3;
            else resourceAmount = 0;

            return resourceAmount;
        }

        void SpawnRewards(BaseEntity contOwner, int resource, int amount)
        {
            if (contOwner.name.Contains("corpse") || resource == 0 || amount == 0)
            {
                rewardTimer.Destroy();
                return;
            }

            Item item = ItemManager.CreateByItemID(resource, amount);

            var parentContainer = (BaseEntity)contOwner as StorageContainer;

            item.MoveToContainer(parentContainer.inventory);

            return;
        }

        [HookMethod("OnPlayerEnteredRaidableBase")]
        void OnPlayerEnteredRaidableBase(BasePlayer player, Vector3 raidPos, bool allowPVP)
        {
            if (eggUID == 0 || _pluginConfig.raidBaseDestroy == 0) return;

            var placeHolder = itemName != "The Golden Egg" ? "item" : "egg";
            player.ChatMessage(String.Format(GetMsg("RaidBaseDestroy", Convert.ToString(player.userID)), placeHolder, _pluginConfig.raidBaseDestroy));
            raidBaseTimer = timer.In(_pluginConfig.raidBaseDestroy, () =>
            {
                if (_spawnedItems != null)
                {
                    foreach (var _item in _spawnedItems.ToList())
                    {
                        if (_item.Key == "6E657264")
                        {
                            Item item = _item.Value;
                            if (item != null)
                            {
                                item.Remove();
                                _spawnedItems.Remove(_item.Key);
                            }
                        }
                    }
                    raidBaseTimer = null;
                }
            });
        }

        [HookMethod("OnPlayerExitedRaidableBase")]
        void OnPlayerExitedRaidableBase(BasePlayer player, Vector3 raidPos, bool allowPVP)
        {
            if (raidBaseTimer != null)
            {
                raidBaseTimer.Destroy();
                raidBaseTimer = null;
            }
        }

        void CreateMarkers(BasePlayer player = null)
        {

            if (_pluginConfig.BlockSafeZone && player != null)
            {
                if (!(player.InSafeZone()) && safezone)
                {
                    storedData.OnEggMove(player.IPlayer);
                    safezone = false;

                    if (_pluginConfig.safeDestroy > 0)
                    {
                        if (safeTimer != null)
                        {
                            safeTimer.Destroy();
                            safeTimer = null;
                        }
                    }
                }

                if (player.InSafeZone() && !safezone)
                {
                    var placeHolder = itemName != "The Golden Egg" ? "item" : "egg";
                    player.ChatMessage(String.Format(GetMsg("SafeZone", Convert.ToString(player.userID)), placeHolder));
                    storedData.DestroyTimers();
                    safezone = true;

                    if (_pluginConfig.safeDestroy > 0 && safeTimer == null)
                    {
                        player.ChatMessage(String.Format(GetMsg("SafeDestroy", Convert.ToString(player.userID)), placeHolder, _pluginConfig.safeDestroy));
                        safeTimer = timer.In(_pluginConfig.safeDestroy, () =>
                        {
                            if (_spawnedItems != null)
                            {
                                foreach (var _item in _spawnedItems.ToList())
                                {
                                    if (_item.Key == "6E657264")
                                    {
                                        Item item = _item.Value;
                                        if (item != null)
                                        {
                                            item.Remove();
                                            _spawnedItems.Remove(_item.Key);
                                        }
                                    }
                                }
                                safeTimer = null;
                            }
                        });
                    }
                }
            }

            if (_pluginConfig.blockedDestroy > 0 && player != null)
            {
                blocked = player?.IsBuildingBlocked() ?? false;
                var placeHolder = itemName != "The Golden Egg" ? "item" : "egg";

                if (!blocked && isBlocked)
                {
                    isBlocked = false;

                    if (_pluginConfig.blockedDestroy > 0)
                    {
                        if (buildTimer != null)
                        {
                            buildTimer.Destroy();
                            buildTimer = null;
                        }
                    }
                }

                if (blocked && !isBlocked)
                {
                    isBlocked = true;

                    if (buildTimer == null)
                    {
                        player.ChatMessage(String.Format(GetMsg("blockDestroy", Convert.ToString(player.userID)), placeHolder, _pluginConfig.blockedDestroy));
                        buildTimer = timer.In(_pluginConfig.blockedDestroy, () =>
                        {
                            if (_spawnedItems != null)
                            {
                                foreach (var _item in _spawnedItems.ToList())
                                {
                                    if (_item.Key == "6E657264")
                                    {
                                        Item item = _item.Value;
                                        if (item != null)
                                        {
                                            item.Remove();
                                            _spawnedItems.Remove(_item.Key);
                                        }
                                    }
                                }
                            }
                            buildTimer = null;
                        });

                    }
                }
            }

            if (_pluginConfig.NoPrivTime && player != null)
            {
                priv = player?.GetBuildingPrivilege()?.IsAuthed(player) ?? false;

                if (!priv && inPriv)
                {
                    storedData.OnEggMove(player.IPlayer);
                    inPriv = false;
                }

                if (priv && !inPriv)
                {
                    storedData.DestroyTimers();
                    inPriv = true;
                }
            }

            if (_pluginConfig.ServerPop != 0 && player != null)
            {
                if ((covalence.Server.Players + ConVar.Admin.ServerInfo().Joining >= _pluginConfig.ServerPop) && pop)
                {
                    storedData.OnEggMove(player.IPlayer);
                    pop = false;
                }

                if ((covalence.Server.Players + ConVar.Admin.ServerInfo().Joining < _pluginConfig.ServerPop) && !pop)
                {
                    storedData.DestroyTimers();
                    pop = true;
                }
            }

            if (_pluginConfig.TimeOfDay && player != null)
            {
                TimeSpan tsStart = DateTime.Parse(_pluginConfig.StartTime).TimeOfDay;
                TimeSpan tsEnd = DateTime.Parse(_pluginConfig.EndTime).TimeOfDay;

                if (!IsBetween(tsStart, tsEnd) && tod)
                {
                    storedData.OnEggMove(player.IPlayer);
                    tod = false;
                }

                if (IsBetween(tsStart, tsEnd) && !tod)
                {
                    storedData.DestroyTimers();
                    tod = true;
                }
            }

            if (_pluginConfig.crateS.crateEvent && _pluginConfig.crateS.crateTOD && (covalence.Server.Players + ConVar.Admin.ServerInfo().Joining >= _pluginConfig.crateS.cratePop))
            {
                TimeSpan tsStart = DateTime.Parse(_pluginConfig.crateS.eventStartTime).TimeOfDay;
                TimeSpan tsEnd = DateTime.Parse(_pluginConfig.crateS.eventEndTime).TimeOfDay;

                if (IsBetween(tsStart, tsEnd) && isRunning == false)
                {
                    if (chinookID != 0)
                    {
                        Puts("The chinook event is already running");
                        isRunning = true;
                        goto MoveOn;
                    }

                    isRunning = true;
                    var timePeriod = (DateTime.Parse(_pluginConfig.crateS.eventEndTime) - DateTime.Now).TotalSeconds;

                    timer.In(UnityEngine.Random.Range(0, (float)timePeriod), () =>
                    {
                        Puts("The chinook event has started");

                        crateID.Clear();
                        _spawnedCrates.Clear();

                        if (_marker != null)
                        {
                            _marker.Kill();
                            _marker.SendUpdate();
                        }
                        if (crateMarkTimer != null)
                            crateMarkTimer.Destroy();

                        var chinook = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab") as CH47HelicopterAIController;
                        /*                         if (chinook == null)
                                                {
                                                    return;
                                                } */

                        chinook.numCrates = _pluginConfig.crateS.numCrates;

                        chinook.TriggeredEventSpawn();
                        chinook.Spawn();

                        chinookID = chinook.net.ID.Value;
                    });
                }

                if (!IsBetween(tsStart, tsEnd) && isRunning == true)
                {
                    isRunning = false;
                }
            }


        MoveOn:
            if (_pluginConfig.roamS.RoamMessage)
            {
                var dist = Vector3.Distance(pos, lastPosition);

                if (dist > 50 && moveTimer == null && lastPosition != Vector3.zero)
                {
                    server.Broadcast(String.Format(GetMsg("Roaming"), itemName));

                    moveTimer = timer.Once(300f, () =>
                    {
                        moveTimer.Destroy();
                        moveTimer = null;
                    });
                }
                lastPosition = pos;
            }

            if (!_pluginConfig.markerS.playerMarker) return;

            NextTick(() =>
            {
                MarkerDelete();
                MapMarkerGenericRadius customMarker;
                customMarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", pos) as MapMarkerGenericRadius;
                customMarker.alpha = _pluginConfig.markerS.MarkerAlpha;

                if (!ColorUtility.TryParseHtmlString(_pluginConfig.markerS.color1, out customMarker.color1))
                {
                    customMarker.color1 = Color.red;
                    PrintError($"Invalid map marker color1: {_pluginConfig.markerS.color1}");
                }

                if (!ColorUtility.TryParseHtmlString(_pluginConfig.markerS.color2, out customMarker.color2))
                {
                    customMarker.color2 = Color.black;
                    PrintError($"Invalid map marker color2: {_pluginConfig.markerS.color2}");
                }

                customMarker.radius = _pluginConfig.markerS.MarkerRadius;
                eggRadMarker.Add(customMarker);

                if (_pluginConfig.markerS.AddVendingMarker)
                {
                    VendingMachineMapMarker customVendingMarker;
                    customVendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", pos) as VendingMachineMapMarker;
                    if (customVendingMarker == null) return;
                    customVendingMarker.markerShopName = _pluginConfig.markerS.VendingMarkerName;
                    eggVendMarker.Add(customVendingMarker);

                    foreach (var Vend in eggVendMarker)
                    {
                        Vend.Spawn();
                        //MapMarker.serverMapMarkers.Remove(Vend); //Enable to remove marker from Rust+
                    }
                }

                foreach (var Rad in eggRadMarker)
                {
                    Rad.Spawn();
                    //MapMarker.serverMapMarkers.Remove(Rad); //Enable to remove marker from Rust+
                    Rad.SendUpdate();
                }
            });
        }

        void MarkerDelete()
        {
            foreach (var Rad in eggRadMarker)
            {
                if (Rad != null)
                {
                    Rad.Kill();
                    Rad.SendUpdate();
                }
            }

            if (_pluginConfig.markerS.AddVendingMarker)
            {
                foreach (var Vend in eggVendMarker)
                {
                    if (Vend != null) Vend.Kill();
                }
                eggVendMarker.Clear();
            }

            eggRadMarker.Clear();
        }

        private void LoadData()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("TheGoldenEgg/player_data");
            storedData = data.ReadObject<StoredData>();
            if (storedData == null)
                storedData = new StoredData();
        }

        private void SaveDataTimer()
        {
            if (saveTimer != null) return;

            saveTimer = timer.Repeat(_pluginConfig.SaveInterval, 0, () =>
            {
                storedData.GetTotals();
                SaveData();
            });
        }

        private void SaveData() => data.WriteObject(storedData);

        object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsValid() || player.transform == null)
            {
                return null;
            }

            Item eggItem;
            _spawnedItems.TryGetValue("6E657264", out eggItem);

            if (eggItem == null) return null;
            var inv = new List<Item>();
            player.inventory.GetAllItems(inv);
            if (inv.Contains(eggItem))
            {
                if (_pluginConfig.UseBlacklist && _pluginConfig.BlacklistedCommands.Any(entry => entry.Replace("/", "").Equals(command, StringComparison.OrdinalIgnoreCase)))
                {
                    player.ChatMessage(String.Format(GetMsg("CommandDisabled", player.UserIDString), itemName));
                    return true;
                }
            }

            return null;
        }

        private class StoredData
        {
            [JsonProperty]
            internal Hash<ulong, UserData> _eggPossession = new Hash<ulong, UserData>();

            [JsonProperty]
            internal string _eggCreated;

            public void DestroyTimers()
            {
                foreach (KeyValuePair<ulong, UserData> entry in _eggPossession)
                {
                    if (entry.Value._timer != null)
                    {
                        entry.Value._timer.Destroy();
                        entry.Value._timer = null;
                    }
                }
            }

            public void OnEggMove(IPlayer user)
            {
                UserData userData;

                DestroyTimers();

                if (user == null) return;

                if (!_eggPossession.TryGetValue(Convert.ToUInt64(user.Id), out userData))
                    userData = _eggPossession[Convert.ToUInt64(user.Id)] = new UserData();

                var player = BasePlayer.FindAwakeOrSleeping(user.Id);

                userData.OnEggMove(user);

                TheGoldenEgg.currentOwner = user.Id;

                if (player != null && player.Team != null && _eggPossession.ContainsKey(player.userID))
                {
                    foreach (var memberID in player.Team.members)
                    {
                        if (!_eggPossession[player.userID].Team.Contains(memberID))
                            _eggPossession[player.userID].Team.Add(memberID);
                    }

                    foreach (var memberLeft in _eggPossession[player.userID].Team)
                    {
                        if (!player.Team.members.Contains(memberLeft))
                        {
                            Logger.Info($"{memberLeft} removed from team");
                            _eggPossession[player.userID].Team.Remove(memberLeft);
                        }
                    }
                }

                if (player != null && player.Team == null && _eggPossession.ContainsKey(player.userID) && _eggPossession[player.userID].Team?.Count > 0)
                {
                    Logger.Info($"ok");
                    _eggPossession[player.userID].Team.Clear();
                }

            }
            public void GetTotals()
            {
                foreach (var player in _eggPossession)
                {
                    _eggPossession[player.Key].TeamTotal = player.Value.eggtime;
                    if (player.Value.Team?.Count > 1)
                    {
                        foreach (var teamMember in player.Value.Team)
                        {
                            if (teamMember != player.Key && _eggPossession.ContainsKey(teamMember))
                                _eggPossession[player.Key].TeamTotal += _eggPossession[teamMember].eggtime;
                        }
                    }
                }
            }
            public void GetEggLeaders(List<UserData> list)
            {
                list.Clear();
                list.AddRange(_eggPossession.Values);

                list.Sort((UserData a, UserData b) =>
                {
                    return a.eggtime.CompareTo(b.eggtime) * -1;
                });
            }

            public class UserData
            {
                public double eggtime;
                public string displayName;
                public List<ulong> Team = new List<ulong>();
                public double TeamTotal;

                [JsonIgnore]
                public Timer _timer;
                private const float timer_Refresh = 30f;

                public void OnEggMove(IPlayer user)
                {
                    displayName = user.Name;

                    StartTimer();
                }

                private void StartTimer()
                {
                    _timer = Timer.Repeat(timer_Refresh, 0, () =>
                    {
                        eggtime += timer_Refresh;
                    });
                }
            }
        }

        #region GUI
        private CuiElementContainer CreateEggUI()
        {
            CuiElementContainer elements = new CuiElementContainer();
            string panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0.5 0.5 0.5 0.0" },
                RectTransform = { AnchorMin = "0.257 0.643", AnchorMax = "0.726 0.921" }
            }, "Hud.Menu", "EggUI");
            elements.Add(new CuiElement
            {
                Parent = panel,
                Components =
                    {
                        new CuiRawImageComponent {Color = "1 1 1 1", Url = String.IsNullOrEmpty(_pluginConfig.customItemS.ItemFoundImage) ? "https://i.imgur.com/uVV8CD0.png" : _pluginConfig.customItemS.ItemFoundImage},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
            });
            return elements;
        }

        #endregion GUI

        Dictionary<ulong, Timer> spamCheck = new Dictionary<ulong, Timer>();

        void AddSpamBlock(ulong playerID)
        {
            if (!spamCheck.ContainsKey(playerID))
            {
                spamCheck.Add(playerID, spamTimer(playerID));
            }
        }

        void RemoveSpamBlock(ulong playerID)
        {
            if (spamCheck.ContainsKey(playerID))
            {
                spamCheck[playerID].Destroy();
                spamCheck.Remove(playerID);
            }
        }

        Timer spamTimer(ulong playerID)
        {
            return timer.Once(2f, () =>
            {
                RemoveSpamBlock(playerID);
            });
        }

        #region Language File
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["DisplayText"] = "{0}",
                ["Pickup"] = "You've picked up {0}!",
                ["Generation"] = "{0} {1} will be generated in {2}",
                ["Generating"] = "Now generating {0}",
                ["Perms"] = "You dont have the permissions to use this command!",
                ["Help1"] = $"When found, the player will be marked on the map after <color=#FFA500>{_pluginConfig.markerS.InitialMarkerDelay}</color> seconds",
                ["Help2"] = "If enabled, the {0} will be marked on the map when on a <color=#FFA500>player</color>, or in a <color=#FFA500>container</color>",
                ["Help3"] = "When placed in a container alongside a resource, the {0} will generate that resource",
                ["Help4"] = "Use <color=#FFA500>/{0} list</color> to show available resources and their generation time",
                ["Help5"] = "Use <color=#FFA500>/{0} time</color> to show the time remaining to the next resource generation",
                ["Help6"] = "Use <color=#FFA500>/{0} find</color> to pinpoint the {1}s location",
                ["Help7"] = "Use <color=#FFA500>/{0} leader</color> to show the current leaderboard times",
                ["Help8"] = "Use <color=#FFA500>/{0} kill</color> to destroy the {1}",
                ["Help10"] = "Use <color=#FFA500>/{0} expire</color> to check the time remaining before the {1} destroys itself",
                ["Help11"] = "Use <color=#FFA500>/{0} event</color> to manually trigger the chinook event",
                ["Help12"] = "Use <color=#FFA500>/{0} winner</color> to grant the {1} leader (and team) an oxide group",
                ["Help13"] = "Use <color=#FFA500>/{0} purge</color> to destroy the {1} and wipe the data file",
                ["Help9"] = "While carrying the {0}, your health and/or gather rate will be increased",
                ["Resource"] = "Enabled resources: <color=#FFA500>{0}</color>",
                ["Blocked"] = "Cracking the {0} is blocked during raids",
                ["EggLeaderboard"] = "[#ffd479]Leaderboard:[/#]",
                ["LeaderList"] = "\n[#Ffbc2c]{0}[/#] - [#45b6fe]{1}[/#] ([#Ffbc2c]Team: {2}[/#])",
                ["SafeZone"] = "You will not gain time while holding the {0} in a safe zone",
                ["Roaming"] = "{0} is on the move!!",
                ["RoamDisabledTod"] = "Roaming buffs are disabled between {0} and {1}",
                ["RoamDisabledPop"] = "Roaming buffs are disabled while the server pop is under {0}",
                ["Expire"] = "{0} will destroy itself in {1} days, {2} hours, {3} minutes",
                ["EggHack"] = "You cannot hack this crate without {0}",
                ["EventStart"] = "A chinook has delivered {0} golden crates to a monument. {1} is required to hack them, leaving the owner vulnerable...",
                ["CrateHacked"] = "A golden crate is being hacked! It will unlock in {0} seconds",
                ["CommandDisabled"] = "This command is blocked while you're holding {0}",
                ["SafeDestroy"] = "The {0} will be destroyed in {1} seconds unless you leave the safezone",
                ["RaidBaseDestroy"] = "The {0} will be destroyed in {1} seconds unless you leave the Raidable Base zone",
                ["blockDestroy"] = "The {0} will be destroyed in {1} seconds unless you leave the building blocked zone",
                ["Winner"] = "{0} is the winner of {1} event!",
            }, this);
        }
        #endregion

        #region Discord
        class DiscordWebhook
        {
            public string content { get; set; } = "";
            public List<DiscordEmbed> embeds { get; set; } = new List<DiscordEmbed>();

            public DiscordWebhook SetContent(string Content)
            {
                this.content = Content;
                return this;
            }

            public DiscordEmbed AddEmbed()
            {
                var embed = new DiscordEmbed(this);
                this.embeds.Add(embed);

                return embed;
            }

            public void Send(TheGoldenEgg plugin, string Webhook)
            {
                plugin.webrequest.Enqueue(Webhook, JsonConvert.SerializeObject(this), (code, body) => { }, plugin, RequestMethod.POST, new Dictionary<string, string>() {
                        { "Content-Type", "application/json" }
                    });
            }
        }

        class DiscordEmbed
        {
            private DiscordWebhook _webhook;

            public string description { get; set; } = "";
            public string title { get; set; } = "";
            public DateTime timestamp { get; set; }
            public int color { get; set; } = 0;
            public List<DiscordEmbedField> fields = new List<DiscordEmbedField>();
            public DiscordEmbedThumbnail thumbnail { get; set; } = new DiscordEmbedThumbnail();

            public DiscordEmbed SetDescription(string Description)
            {
                this.description = Description;
                return this;
            }
            public DiscordEmbed SetTitle(string Title)
            {
                this.title = Title;
                return this;
            }
            public DiscordEmbed SetTimestamp(DateTime Timestamp)
            {
                this.timestamp = Timestamp;
                return this;
            }
            public DiscordEmbed SetColor(int Color)
            {
                this.color = Color;
                return this;
            }
            public DiscordEmbed AddThumbnail(string Url)
            {
                this.thumbnail = (new DiscordEmbedThumbnail() { url = Url });
                return this;
            }
            public DiscordEmbed AddField(string Name, string Value, bool Inline = false)
            {
                this.fields.Add(new DiscordEmbedField() { name = Name, value = Value, inline = Inline });
                return this;
            }
            public DiscordEmbed AddFields(IEnumerable<DiscordEmbedField> Fields)
            {
                this.fields.AddRange(fields);
                return this;
            }
            public DiscordEmbed AddEmbed()
            {
                return this._webhook.AddEmbed();
            }
            public DiscordWebhook EndEmbed()
            {
                return this._webhook;
            }
            public DiscordEmbed() { }
            public DiscordEmbed(DiscordWebhook WebHook)
            {
                this._webhook = WebHook;
            }
        }
        class DiscordEmbedThumbnail
        {
            public string url { get; set; } = "";
        }

        class DiscordEmbedField
        {
            public string name { get; set; } = "";
            public string value { get; set; } = "";
            public bool inline { get; set; } = false;

            public DiscordEmbedField() { }
            public DiscordEmbedField(string Name, string Value, bool Inline = false)
            {
                this.name = Name;
                this.value = Value;
                this.inline = Inline;
            }
        }
        #endregion

        #region Configuration

        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("Egg spawn chance for Locked Crates (0 to 100)")]
            public float LockedCrateEggChance = 0.04f;

            [JsonProperty("Egg spawn chance for Elite Crates")]
            public float EliteCrateEggChance = 0.04f;

            [JsonProperty("Egg spawn chance for Military Crates")]
            public float MilitaryCrateEggChance = 0.02f;

            [JsonProperty("Egg spawn chance for Normal Crates")]
            public float NormalCrateEggChance = 0.01f;

            [JsonProperty(PropertyName = "Marker Settings")]
            public MarkerSettings markerS = new MarkerSettings();

            [JsonProperty(PropertyName = "Resource Settings")]
            public ResourceSettings resourceS = new ResourceSettings();

            [JsonProperty(PropertyName = "Item Customisation")]
            public ItemCustomisation customItemS = new ItemCustomisation();

            [JsonProperty(PropertyName = "Roam Settings")]
            public RoamSettings roamS = new RoamSettings();

            [JsonProperty(PropertyName = "Event Settings")]
            public CrateEvent crateS = new CrateEvent();

            [JsonProperty("Let players with permission pinpoint the egg on screen (use /egg find)")]
            public bool PinpointEggCommand = false;

            [JsonProperty("Send a webhook when the egg is found/destroyed")]
            public string sendWebhook = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

            [JsonProperty("Don't add time while the player is in a safe zone")]
            public bool BlockSafeZone = true;

            [JsonProperty("Don't add time while the egg is in a stash")]
            public bool BlockStashTime = true;

            [JsonProperty("Don't add time while the egg is in a building")]
            public bool NoPrivTime = false;

            [JsonProperty("Don't add time while server pop is below (leave at 0 to disable)")]
            public int ServerPop = 0;

            [JsonProperty("Don't add time between certain hours")]
            public bool TimeOfDay = false;

            [JsonProperty("Start of time period")]
            public string StartTime = "1AM";

            [JsonProperty("End of time period")]
            public string EndTime = "6AM";

            [JsonProperty("Destroy the egg if in a safe zone for longer than (seconds, leave at 0 to disable)")]
            public float safeDestroy = 0;

            [JsonProperty("Destroy the egg if in a building blocked zone for longer than (seconds, leave at 0 to disable)")]
            public float blockedDestroy = 0;

            [JsonProperty("Destroy the egg if in a Raidable Base zone for longer than (seconds, leave at 0 to disable)")]
            public float raidBaseDestroy = 0;

            [JsonProperty("Name of permission group to grant with /egg winner (requires Timed Permissions plugin)")]
            public string GroupName = "";

            [JsonProperty("Duration to grant access to group (requires Timed Permissions plugin). Format: 1d12h30m")]
            public string GroupDuration = "28d";

            [JsonProperty("Destroy the egg after x minutes (leave at 0 to disable)")]
            public int KillTime = 0;

            [JsonProperty("Block player from cracking open the egg while being raided")]
            public bool BlockEggCrack = true;

            [JsonProperty("Raid block timer")]
            public int BlockInterval = 300;

            [JsonProperty("Data save interval")]
            public int SaveInterval = 300;

            [JsonProperty("Clear data on map wipe")]
            public bool ClearData = true;

            [JsonProperty("Use custom item list when cracking open the egg")]
            public bool UseCustomList = false;

            [JsonProperty("Custom item list (use item shortname, eg rifle.m39, explosive.timed, etc")]
            public List<string> EggRewards = new List<string>
                {
                        "item.one",
                        "item.two",
                        "etc"
                };

            [JsonProperty("Blacklist commands whilst holding the egg")]
            public bool UseBlacklist = false;

            [JsonProperty("Blacklisted commands")]
            public List<string> BlacklistedCommands = new List<string>
                {
                    "/tp",
                    "/trade",
                    "/shop",
                    "/rw",
                    "/bank",
                    "/home",
                    "/remove"
                };

            [JsonProperty(PropertyName = "TruePVE Only")]
            public TruePVE tpveS = new TruePVE();
        }

        public class TruePVE
        {
            [JsonProperty("Enable damage to players and bases if they have the egg")]
            public bool DisableTruePVE = false;

            [JsonProperty("Max distance between players for damage to register")]
            public float DamageDistance = 100f;
        }

        public class CrateEvent
        {
            [JsonProperty("Run the chinook event")]
            public bool crateEvent = false;

            [JsonProperty("Number of crates to drop")]
            public int numCrates = 2;

            [JsonProperty("Crate unlock time")]
            public int crateTimer = 300;

            [JsonProperty("Maximum additional items to add to the crate(s)")]
            public int addedItems = 15;

            [JsonProperty("Run the event once, between a certain time")]
            public bool crateTOD = false;

            [JsonProperty("Start of time period")]
            public string eventStartTime = "7PM";

            [JsonProperty("End of time period")]
            public string eventEndTime = "10PM";

            [JsonProperty("Run the event on repeat")]
            public bool crateRepeat = false;

            [JsonProperty("Minimum time between events (seconds)")]
            public float minStartTime = 3600;

            [JsonProperty("Maximum time between events (seconds)")]
            public float maxStartTime = 7200;

            [JsonProperty("Don't run the event if server pop is below (leave at 0 to disable)")]
            public int cratePop = 0;

            [JsonProperty("Show map marker")]
            public bool crateMarker = true;

            [JsonProperty("Marker Radius")]
            public float crateMarkerRadius = 0.5f;

            [JsonProperty("Marker Transparency")]
            public float crateMarkerAlpha = 0.4f;

            [JsonProperty("Marker Color (hex)")]
            public string crateColor1 = "#ecf97f";

            [JsonProperty("Marker Border Color (hex)")]
            public string crateColor2 = "#000000";

        }

        public class ItemCustomisation
        {
            [JsonProperty("Item Name")]
            public string ItemName = "The Golden Egg";

            [JsonProperty("Item Skin ID")]
            public ulong ItemSkin = 0;

            [JsonProperty("Item Found Image in Game (use an image 1000x400)")]
            public string ItemFoundImage = "https://i.imgur.com/uVV8CD0.png";

            [JsonProperty(PropertyName = "Chat command", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Command = "egg";

            [JsonProperty("Item Found Image for the Discord Webhook")]
            public string ItemFoundDiscord = "https://i.imgur.com/Knn0X37.png";

            [JsonProperty("Item Lost Image for the Discord Webhook")]
            public string ItemLostDiscord = "https://i.imgur.com/GxlVNSa.png";
        }

        public class MarkerSettings
        {
            [JsonProperty("Show map marker when the egg is in a box")]
            public bool boxMarker = true;

            [JsonProperty("Show map marker when the egg is on a player")]
            public bool playerMarker = true;

            [JsonProperty("Player marker refresh time (seconds)")]
            public int PlayerMarkerRefreshTime = 25;

            [JsonProperty("Box marker refresh time (seconds)")]
            public int BoxMarkerRefreshTime = 60;

            [JsonProperty("Initial marker delay when the egg is found")]
            public int InitialMarkerDelay = 60;

            [JsonProperty("Marker Radius")]
            public float MarkerRadius = 0.7f;

            [JsonProperty("Marker Transparency")]
            public float MarkerAlpha = 0.5f;

            [JsonProperty("Marker Color (hex)")]
            public string color1 = "#fa030a";

            [JsonProperty("Marker Border Color (hex)")]
            public string color2 = "#000000";

            [JsonProperty("Add a Vending marker")]
            public bool AddVendingMarker = false;

            [JsonProperty("Vending Marker Name")]
            public string VendingMarkerName = "The Golden Egg";
        }

        public class RoamSettings
        {
            [JsonProperty("Increase health whilst holding the egg")]
            public bool IncreaseHealth = true;

            [JsonProperty("Total health")]
            public int TotalHealth = 150;

            [JsonProperty("Increase ore/wood gather rate whilst holding the egg")]
            public bool IncreaseGather = true;

            [JsonProperty("Gather multipler")]
            public float GatherMultiplier = 2.0f;

            [JsonProperty("Increase pickup amount whilst holding the egg (hemp/food etc)")]
            public bool IncreasePickup = true;

            [JsonProperty("Pickup multipler")]
            public float PickupMultiplier = 2.0f;

            [JsonProperty("Don't allow roam bonus while server pop is below (leave at 0 to disable)")]
            public int RoamPop = 0;

            [JsonProperty("Don't allow roam bonus between certain hours")]
            public bool RoamTod = false;

            [JsonProperty("Start of time period")]
            public string RoamStart = "1AM";

            [JsonProperty("End of time period")]
            public string RoamEnd = "6AM";

            [JsonProperty("Broadcast a chat message when someone starts roaming")]
            public bool RoamMessage = true;
        }

        public class ResourceSettings
        {
            [JsonProperty("Resource Spawn Time (seconds)")]
            public int ResourceSpawnTime = 3600;

            [JsonProperty("Scrap Spawn Amount (0 to disable)")]
            public int ScrapSpawnAmount = 50;

            [JsonProperty("HQM Spawn Amount")]
            public int HQMSpawnAmount = 25;

            [JsonProperty("Low Grade Spawn Amount")]
            public int LowGradeSpawnAmount = 100;

            [JsonProperty("Metal Frags Spawn Amount")]
            public int FragsSpawnAmount = 1000;

            [JsonProperty("Allow Sulfur Ore")]
            public bool AllowSulfurOre = false;

            [JsonProperty("Sulfur Ore Spawn Amount")]
            public int SulfurOreAmount = 800;

            [JsonProperty("Allow Cooked Sulfur")]
            public bool AllowSulfurCooked = false;

            [JsonProperty("Cooked Sulfur Spawn Amount")]
            public int SulfurCookedAmount = 500;

            [JsonProperty("Custom Item 1 (use item shortname, eg ammo.rifle, gears, green.berry)")]
            public string CustomItem1 = "";

            [JsonProperty("Custom Item 1 Amount")]
            public int CustomAmount1 = 1;

            [JsonProperty("Custom Item 1 Spawn Time (seconds)")]
            public int CustomTime1 = 7200;

            [JsonProperty("Custom Item 2")]
            public string CustomItem2 = "";

            [JsonProperty("Custom Item 2 Amount")]
            public int CustomAmount2 = 1;

            [JsonProperty("Custom Item 2 Spawn Time (seconds)")]
            public int CustomTime2 = 7200;

            [JsonProperty("Custom Item 3")]
            public string CustomItem3 = "";

            [JsonProperty("Custom Item 3 Amount")]
            public int CustomAmount3 = 1;

            [JsonProperty("Custom Item 3 Spawn Time (seconds)")]
            public int CustomTime3 = 7200;
        }

        private Configuration GetDefaultConfig() => new Configuration();

        /*
        Thanks to WhiteThunder for the below code - https://github.com/WheteThunger/MonumentAddons

        MIT License

        Copyright (c) 2021 WhiteThunder

        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:

        The above copyright notice and this permission notice shall be included in all
        copies or substantial portions of the Software.

        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        SOFTWARE.
        */

        private class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
                {
                    Logger.Warning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }

            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Warning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }
        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_pluginConfig, true);
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        #endregion

        private static class Logger
        {
            public static void Info(string message) => Interface.Oxide.LogInfo($"[The Golden Egg] {message}");
            public static void Error(string message) => Interface.Oxide.LogError($"[The Golden Egg] {message}");
            public static void Warning(string message) => Interface.Oxide.LogWarning($"[The Golden Egg] {message}");
        }
    }
}
