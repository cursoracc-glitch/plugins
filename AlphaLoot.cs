using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AlphaLoot", "k1lly0u", "3.1.3")]
    class AlphaLoot : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin CustomLootSpawns, EventLoot, FancyDrop, SkinBox;

        private StoredData storedData;
        private StoredData bradleyData;
        private StoredData heliData;

        private DynamicConfigFile data, bradley, heli, skinData;

        private bool updateContainerCapacities = false;

        private static Hash<string, HashSet<SkinEntry>> weightedSkinIds;
        private static Hash<string, List<ulong>> importedSkinIds;
        private static Hash<string, int> defaultScrapAmounts;

        private const string ADMIN_PERMISSION = "alphaloot.admin";

        private const string HELI_CRATE = "heli_crate";
        private const string BRADLEY_CRATE = "bradley_crate";
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            defaultScrapAmounts = new Hash<string, int>();

            permission.RegisterPermission(ADMIN_PERMISSION, this);
            
            LoadData();

            if (updateContainerCapacities)
                SetCapacityLimits();
        }

        private void OnServerInitialized()
        {
            PopulateContainerDefinitions(ref storedData, ref heliData, ref bradleyData);
            SaveData();
            Puts($"Loaded {storedData.loot_advanced.Count + storedData.loot_simple.Count} loot container definitions and {storedData.npcs_advanced.Count + storedData.npcs_simple.Count} npc loot definitions");
            Puts($"Loaded {heliData.loot_advanced.Count + heliData.loot_simple.Count} heli loot profiles");
            Puts($"Loaded {bradleyData.loot_advanced.Count + bradleyData.loot_simple.Count} bradley loot profiles");

            if (configData.AutoUpdate)
                AutoUpdateItemLists();
            
            if (configData.UseSkinboxSkins || configData.UseApprovedSkins)
            {
                if ((Steamworks.SteamInventory.Definitions?.Length ?? 0) == 0)
                {
                    PrintWarning("Waiting for Steamworks to initialize to load item skins");
                    Steamworks.SteamInventory.OnDefinitionsUpdated += LoadSkins;
                }
                else LoadSkins();
            } 
            else RefreshLootContents();
        }

        private void OnEntitySpawned(BradleyAPC bradleyApc)
        {
            if (bradleyApc != null)
                bradleyApc.maxCratesToSpawn = configData.BradleyCrates;
        }

        private void OnEntitySpawned(BaseHelicopter baseHelicopter)
        {
            if (baseHelicopter != null)
                baseHelicopter.maxCratesToSpawn = configData.HelicopterCrates;
        }

        private object OnCorpsePopulate(BaseEntity entity, LootableCorpse corpse)
        {
            if (entity == null || corpse == null)
                return null;

            object obj = Interface.CallHook("CanPopulateLoot", entity, corpse);
            if (obj != null)
                return null;

            return PopulateLoot(entity, corpse) ? corpse : null;
        }

        private object OnLootSpawn(LootContainer container)
        {
            if (container == null)
                return null;

            if (CustomLootSpawns && (bool)CustomLootSpawns.Call("IsLootBox", container as BaseEntity))
                return null;

            if (EventLoot && (bool)EventLoot.Call("IsEventLootContainer", container as BaseEntity))
                return null;

            if (FancyDrop && container is SupplyDrop && !configData.OverrideFancyDrop)
                return null;

            object obj = Interface.CallHook("CanPopulateLoot", container);
            if (obj != null)
                return null;
            
            if (PopulateLoot(container))
                return true;

            return null;
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null)
                return;

            if (!permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
                return;

            LootContainer lootContainer = info?.HitEntity?.GetComponent<LootContainer>();
            if (lootContainer == null)
                return;

            player.ChatMessage($"Viewing loot generation for: <color=#ffff00>{lootContainer.ShortPrefabName}</color>");
            lootContainer.gameObject.AddComponent<LootCycler>();
            player.inventory.loot.StartLootingEntity(lootContainer, false);
            player.inventory.loot.AddContainer(lootContainer.inventory);
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", lootContainer.panelName);
        }

        private void OnLootEntityEnd(BasePlayer player, StorageContainer container)
        {
            LootCycler lootCycler = container?.GetComponent<LootCycler>();
            if (lootCycler != null)
                UnityEngine.Object.Destroy(lootCycler);
        }

        private void Unload()
        {
            RefreshLootContents(null, true);
            configData = null;

            weightedSkinIds = null;
            importedSkinIds = null;
            defaultScrapAmounts = null;
        }
        #endregion

        #region Skins
        private void LoadSkins()
        {
            Steamworks.SteamInventory.OnDefinitionsUpdated -= LoadSkins;

            if (configData.UseSkinboxSkins)
                PopulateSkinListFromSkinBox();

            if (configData.UseApprovedSkins)
                PopulateSkinListFromApproved();

            RefreshLootContents();
        }

        private void PopulateSkinListFromSkinBox()
        {
            Dictionary<string, LinkedList<ulong>> dictionary = SkinBox?.Call("getSkincache") as Dictionary<string, LinkedList<ulong>>;
            if (dictionary != null)
            {
                importedSkinIds = new Hash<string, List<ulong>>();

                foreach(KeyValuePair<string, LinkedList<ulong>> kvp in dictionary)
                {
                    List<ulong> list = new List<ulong>();
                    list.AddRange(kvp.Value);
                    importedSkinIds[kvp.Key] = list;
                }

                Puts($"Imported {importedSkinIds.Sum(x => x.Value.Count)} skins from SkinBox");
            }            
        }

        private void PopulateSkinListFromApproved()
        {
            if (importedSkinIds == null)
                importedSkinIds = new Hash<string, List<ulong>>();

            List<int> itemSkinDirectory = Pool.GetList<int>();
            itemSkinDirectory.AddRange(ItemSkinDirectory.Instance.skins.Select(x => x.id));

            int count = 0;

            foreach (InventoryDef item in Steamworks.SteamInventory.Definitions)
            {
                string shortname = item.GetProperty("itemshortname");
                if (string.IsNullOrEmpty(shortname) || item.Id < 100)
                    continue;

                ulong wsid;
                if (itemSkinDirectory.Contains(item.Id))
                    wsid = (ulong)item.Id;
                else
                {
                    if (!ulong.TryParse(item.GetProperty("workshopid"), out wsid))
                        continue;
                }

                if (!importedSkinIds.ContainsKey(shortname))
                    importedSkinIds[shortname] = new List<ulong>();

                if (!importedSkinIds[shortname].Contains(wsid))
                {
                    importedSkinIds[shortname].Add(wsid);
                    count++;
                }
            }

            Puts($"Imported {count} approved skins that weren't already in the list");
            Pool.FreeList(ref itemSkinDirectory);
        }
        #endregion

        #region Container Population
        private void RefreshLootContents(ConsoleSystem.Arg arg = null, bool setDefaultScrap = false)
        {
            LootContainer[] lootContainers = UnityEngine.Object.FindObjectsOfType<LootContainer>();
            
            if (arg != null)
                SendReply(arg, $"Repopulating loot for {lootContainers?.Length} containers");
            else Puts($"Repopulating loot for {lootContainers?.Length} containers");

            for (int i = 0; i < lootContainers?.Length; i++)
            {
                LootContainer lootContainer = lootContainers[i];
                if (lootContainer != null && !lootContainer.IsDestroyed)
                {
                    if (lootContainer.inventory == null)
                    {
                        lootContainer.CreateInventory(true);
                        lootContainer.OnInventoryFirstCreated(lootContainer.inventory);
                    }

                    if (setDefaultScrap)
                    {
                        int scrapAmount;
                        if (defaultScrapAmounts.TryGetValue(lootContainer.ShortPrefabName, out scrapAmount))
                            lootContainer.scrapAmount = scrapAmount;
                    }

                    lootContainer.inventory.capacity = lootContainer.inventorySlots;
                    lootContainer.CancelInvoke(lootContainer.SpawnLoot);
                    lootContainer.Invoke(lootContainer.SpawnLoot, UnityEngine.Random.Range(1f, 20f));
                }
            }
        }

        private void PopulateContainerDefinitions(ref StoredData storedData, ref StoredData heliData, ref StoredData bradleyData)
        {
            storedData.IsBaseLootTable = true;

            List<LootContainer> containers = Resources.FindObjectsOfTypeAll<LootContainer>().OrderBy(x => x.ShortPrefabName).ToList();

            for (int i = 0; i < containers.Count; i++)
            {
                LootContainer lootContainer = containers[i];

                if (!defaultScrapAmounts.ContainsKey(lootContainer.ShortPrefabName))
                    defaultScrapAmounts[lootContainer.ShortPrefabName] = lootContainer.scrapAmount;

                if (lootContainer.ShortPrefabName.Equals(HELI_CRATE))
                {
                    BaseLootContainerProfile lootContainerProfile;

                    if (storedData.TryGetLootProfile(HELI_CRATE, out lootContainerProfile))
                    {
                        heliData.CloneLootProfile(HELI_CRATE, lootContainerProfile);
                        storedData.RemoveProfile(HELI_CRATE);
                        Debug.LogWarning($"Helicopter loot profiles have been removed from your loot table and placed in its own data file. (/data/AlphaLoot/LootProfiles/{configData.HeliProfileName}.json)");
                    }
                    else
                    {
                        if (heliData.HasAnyProfiles)
                            continue;

                        heliData.CreateDefaultLootProfile(lootContainer);
                    }
                }
                else if (lootContainer.ShortPrefabName.Equals(BRADLEY_CRATE))
                {
                    BaseLootContainerProfile lootContainerProfile;

                    if (storedData.TryGetLootProfile(BRADLEY_CRATE, out lootContainerProfile))
                    {
                        bradleyData.CloneLootProfile(BRADLEY_CRATE, lootContainerProfile);
                        storedData.RemoveProfile(BRADLEY_CRATE);
                        Debug.LogWarning($"Bradley loot profiles have been removed from your loot table and placed in its own data file. (/data/AlphaLoot/LootProfiles/{configData.BradleyProfileName}.json)");
                    }
                    else
                    {
                        if (bradleyData.HasAnyProfiles)
                            continue;

                        bradleyData.CreateDefaultLootProfile(lootContainer);
                    }
                }
                else storedData.CreateDefaultLootProfile(containers[i]);
            }

            for (int i = 0; i < npcPrefabs.Count; i++)
            {
                string prefab = npcPrefabs[i];
                GameObject obj = GameManager.server.FindPrefab(prefab);

                prefab = ToShortName(prefab);

                HTNPlayer htnPlayer = obj.GetComponent<HTNPlayer>();
                if (htnPlayer != null)
                {
                    BaseNpcDefinition baseDefinition = htnPlayer.AiDefinition;
                    if (baseDefinition is Rust.Ai.HTN.Murderer.MurdererDefinition)
                    {
                        storedData.CreateDefaultLootProfile(prefab, (baseDefinition as Rust.Ai.HTN.Murderer.MurdererDefinition).Loot);
                        continue;
                    }

                    if (baseDefinition is Rust.Ai.HTN.Scientist.ScientistDefinition)
                    {
                        storedData.CreateDefaultLootProfile(prefab, (baseDefinition as Rust.Ai.HTN.Scientist.ScientistDefinition).Loot);
                        continue;
                    }

                    if (baseDefinition is Rust.Ai.HTN.NPCTurret.NPCTurretDefinition)
                    {
                        storedData.CreateDefaultLootProfile(prefab, (baseDefinition as Rust.Ai.HTN.NPCTurret.NPCTurretDefinition).Loot);
                        continue;
                    }

                    if (baseDefinition is Rust.Ai.HTN.ScientistAStar.ScientistAStarDefinition)
                    {
                        storedData.CreateDefaultLootProfile(prefab, (baseDefinition as Rust.Ai.HTN.ScientistAStar.ScientistAStarDefinition).Loot);
                        continue;
                    }

                    if (baseDefinition is Rust.Ai.HTN.ScientistJunkpile.ScientistJunkpileDefinition)
                    {
                        storedData.CreateDefaultLootProfile(prefab, (baseDefinition as Rust.Ai.HTN.ScientistJunkpile.ScientistJunkpileDefinition).Loot);
                        continue;
                    }

                    continue;
                }

                global::HumanNPC humanNPC = obj.GetComponent<global::HumanNPC>();
                if (humanNPC != null)
                {
                    storedData.CreateDefaultLootProfile(prefab, humanNPC.LootSpawnSlots);
                    continue;
                }

                NPCMurderer npcMurderer = obj.GetComponent<NPCMurderer>();
                if (npcMurderer != null)
                {
                    storedData.CreateDefaultLootProfile(prefab, npcMurderer.LootSpawnSlots);
                    continue;
                }

                Scientist scientist = obj.GetComponent<Scientist>();
                if (scientist != null)
                {
                    storedData.CreateDefaultLootProfile(prefab, scientist.LootSpawnSlots);
                    continue;
                }
            }            
        }

        private bool PopulateLoot(LootContainer container)
        {            
            BaseLootContainerProfile lootProfile;

            if (container.ShortPrefabName.Equals(HELI_CRATE))
            {
                if (heliData.GetRandomLootProfile(out lootProfile))
                {
                    PopulateLootContainer(container, lootProfile);
                    return true;
                }
            }
            else if (container.ShortPrefabName.Equals(BRADLEY_CRATE))
            {
                if (bradleyData.GetRandomLootProfile(out lootProfile))
                {
                    PopulateLootContainer(container, lootProfile);
                    return true;
                }
            }
            else
            {
                if (storedData.TryGetLootProfile(container.ShortPrefabName, out lootProfile) && lootProfile.Enabled)
                {                    
                    PopulateLootContainer(container, lootProfile);
                    return true;
                }
            }

            return false;
        }

        private void PopulateLootContainer(LootContainer container, BaseLootContainerProfile lootProfile)
        {
            container.destroyOnEmpty = lootProfile.DestroyOnEmpty;

            lootProfile.PopulateLoot(container.inventory);

            container.CancelInvoke(container.SpawnLoot);

            if (lootProfile.ShouldRefreshContents)
                container.Invoke(container.SpawnLoot, UnityEngine.Random.Range(lootProfile.MinSecondsBetweenRefresh, lootProfile.MaxSecondsBetweenRefresh));
        }

        private bool PopulateLoot(BaseEntity entity, LootableCorpse corpse)
        {
            BaseLootProfile lootProfile;
            if (storedData.TryGetNPCProfile(entity.ShortPrefabName, out lootProfile))
            {
                if (!lootProfile.Enabled)
                    return false;

                lootProfile.PopulateLoot(corpse.containers[0]);
                return true;
            }
            return false;
        }
        #endregion

        #region Functions
        private string ToShortName(string name)
        {
            return name.Split('/').Last().Replace(".prefab", "");
        }

        private LootContainer FindContainer(BasePlayer player)
        {
            RaycastHit raycastHit;
            if (Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 20f))
            {
                LootContainer lootContainer = raycastHit.GetEntity() as LootContainer;
                return lootContainer;
            }
            return null;
        }

        private object WantsToHandleFancyDropLoot() => configData.OverrideFancyDrop ? (object)true : null;
        #endregion

        #region Auto-Updater
        private void AutoUpdateItemLists()
        {
            const string lastDefaultTable = "AlphaLoot/AutoUpdater/do_not_edit_this_file";
            
            ItemList lastItemList;

            if (Interface.Oxide.DataFileSystem.ExistsDatafile(lastDefaultTable))
            {
                lastItemList = Interface.Oxide.DataFileSystem.GetFile(lastDefaultTable).ReadObject<ItemList>();

                if (lastItemList != null)
                {
                    if (lastItemList.protocol == Rust.Protocol.printable)
                    {
                        Debug.Log("[AlphaLoot Auto Updater] - Last item list protocol matches current protocol. No new items added");
                        return;
                    }

                    List<int> newItems = Pool.GetList<int>();

                    ItemManager.itemList.ForEach(x =>
                    {
                        if (!lastItemList.itemIds.Contains(x.itemid))
                            newItems.Add(x.itemid);
                    });

                    if (newItems.Count > 0)
                    {
                        int additions = 0;

                        Debug.Log($"[AlphaLoot Auto Updater] - Found {newItems.Count} new game items. Adding them to your loot table");

                        AddItemsToLootTable(newItems, out additions);

                        if (additions > 0)
                        {
                            Debug.Log($"[AlphaLoot Auto Updater] - Added {additions} new loot definitions to the loot table");
                            SaveData();
                            Interface.Oxide.DataFileSystem.WriteObject<ItemList>(lastDefaultTable, new ItemList() { itemIds = ItemManager.itemDictionary.Keys.ToList(), protocol = Rust.Protocol.printable });
                        }
                    }
                    else Debug.Log("[AlphaLoot Auto Updater] - No new items in game");

                    Pool.FreeList(ref newItems);
                }
            }
            else
            {
                Debug.Log("[AlphaLoot Auto Updater] - Generating item list for auto-updater. Future game updates will automatically add new items to your loot table. You can disable this feature in the config");
                Interface.Oxide.DataFileSystem.WriteObject<ItemList>(lastDefaultTable, new ItemList() { itemIds = ItemManager.itemDictionary.Keys.ToList(), protocol = Rust.Protocol.printable });
            }
        }

        private void AddSpecifiedItemsToLootTable(params string[] args)
        {
            int additions = 0;

            List<int> items = Pool.GetList<int>();

            foreach(string str in args)
            {
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(str);
                if (itemDefinition != null)
                    items.Add(itemDefinition.itemid);
            }

            if (items.Count > 0)
            {
                Debug.Log($"[AlphaLoot] - Adding {items.Count} specified items to your loot table");

                AddItemsToLootTable(items, out additions);

                if (additions > 0)
                {
                    Debug.Log($"[AlphaLoot] - Successfully added {additions} new loot definitions to the loot table");
                    SaveData();
                }
            }
            else Debug.Log("[AlphaLoot] - Failed to find item definitions for the shortname's supplied");

            Pool.FreeList(ref items);
        }

        private void AddItemsToLootTable(List<int> items, out int additions)
        {
            additions = 0;

            StoredData defaultLootTable = new StoredData();
            StoredData defaultHeliLootTable = new StoredData();
            StoredData defaultBradleyLootTable = new StoredData();

            PopulateContainerDefinitions(ref defaultLootTable, ref defaultHeliLootTable, ref defaultBradleyLootTable);

            foreach (KeyValuePair<string, AdvancedLootContainerProfile> kvp in defaultLootTable.loot_advanced)
            {
                foreach (int itemid in items)
                {
                    string shortname = ItemManager.itemDictionary[itemid].shortname;

                    NewLootItem newLootItem = new NewLootItem();
                    float score = 0;
                    int multiplier = 1;

                    foreach (LootSpawnSlot lootSpawnSlot in kvp.Value.LootSpawnSlots)
                    {
                        FindItemAndCalculateScoreRecursive(lootSpawnSlot.LootDefinition, shortname, ref newLootItem, ref multiplier);
                        score += (lootSpawnSlot.Probability * newLootItem.Score) * lootSpawnSlot.NumberToSpawn;
                    }

                    if (score > 0)
                    {
                        AdvancedLootContainerProfile advancedLootProfile;
                        if (storedData.loot_advanced.TryGetValue(kvp.Key, out advancedLootProfile))
                        {
                            LootSpawnSlot newLootSpawnSlot = new LootSpawnSlot
                            {
                                LootDefinition = new LootSpawn()
                                {
                                    Items = new ItemAmountRanged[] { newLootItem.Item },
                                    SubSpawn = new LootSpawn.Entry[0]
                                },
                                NumberToSpawn = 1,
                                Probability = Mathf.Clamp01(score * multiplier)
                            };

                            int index = advancedLootProfile.LootSpawnSlots.Length;

                            System.Array.Resize(ref advancedLootProfile.LootSpawnSlots, index + 1);

                            advancedLootProfile.LootSpawnSlots[index] = newLootSpawnSlot;

                            additions++;
                            Debug.Log($"[AlphaLoot] - Added {shortname} to advanced loot profile ({kvp.Key}) with a calculated probability of {newLootSpawnSlot.Probability}");
                        }
                        else
                        {
                            SimpleLootContainerProfile simpleLootContainerProfile;
                            if (storedData.loot_simple.TryGetValue(kvp.Key, out simpleLootContainerProfile))
                            {
                                ItemAmountWeighted itemAmountWeighted = new ItemAmountWeighted()
                                {
                                    BlueprintChance = newLootItem.Item.BlueprintChance,
                                    Condition = new ItemAmount.ConditionItem() { MinCondition = newLootItem.Item.Condition.MinCondition, MaxCondition = newLootItem.Item.Condition.MaxCondition },
                                    MaxAmount = newLootItem.Item.MaxAmount,
                                    MinAmount = newLootItem.Item.MinAmount,
                                    Shortname = shortname,
                                    Weight = Mathf.Max(Mathf.RoundToInt(((float)simpleLootContainerProfile.Items.Sum(x => x.Weight) * score) * multiplier), 1)
                                };

                                int index = simpleLootContainerProfile.Items.Length;

                                System.Array.Resize(ref simpleLootContainerProfile.Items, index + 1);

                                simpleLootContainerProfile.Items[index] = itemAmountWeighted;

                                additions++;
                                Debug.Log($"[AlphaLoot] - Added {shortname} to simple loot profile ({kvp.Key}) with a calculated weight of {itemAmountWeighted.Weight}");
                            }
                        }
                        continue;
                    }
                }
            }

            foreach (KeyValuePair<string, AdvancedLootContainerProfile> kvp in defaultHeliLootTable.loot_advanced)
            {
                foreach (int itemid in items)
                {
                    string shortname = ItemManager.itemDictionary[itemid].shortname;

                    NewLootItem newLootItem = new NewLootItem();
                    float score = 0;
                    int multiplier = 1;

                    foreach (LootSpawnSlot lootSpawnSlot in kvp.Value.LootSpawnSlots)
                    {
                        FindItemAndCalculateScoreRecursive(lootSpawnSlot.LootDefinition, shortname, ref newLootItem, ref multiplier);
                        score += (lootSpawnSlot.Probability * newLootItem.Score) * lootSpawnSlot.NumberToSpawn;
                    }

                    if (score > 0)
                    {
                        AdvancedLootContainerProfile advancedLootProfile;
                        if (heliData.loot_advanced.TryGetValue(kvp.Key, out advancedLootProfile))
                        {
                            LootSpawnSlot newLootSpawnSlot = new LootSpawnSlot
                            {
                                LootDefinition = new LootSpawn()
                                {
                                    Items = new ItemAmountRanged[] { newLootItem.Item },
                                    SubSpawn = new LootSpawn.Entry[0]
                                },
                                NumberToSpawn = 1,
                                Probability = Mathf.Clamp01(score * multiplier)
                            };

                            int index = advancedLootProfile.LootSpawnSlots.Length;

                            System.Array.Resize(ref advancedLootProfile.LootSpawnSlots, index + 1);

                            advancedLootProfile.LootSpawnSlots[index] = newLootSpawnSlot;

                            additions++;
                            Debug.Log($"[AlphaLoot] - Added {shortname} to advanced heli loot profile ({kvp.Key}) with a calculated probability of {newLootSpawnSlot.Probability}");
                        }
                        else
                        {
                            SimpleLootContainerProfile simpleLootContainerProfile;
                            if (heliData.loot_simple.TryGetValue(kvp.Key, out simpleLootContainerProfile))
                            {
                                ItemAmountWeighted itemAmountWeighted = new ItemAmountWeighted()
                                {
                                    BlueprintChance = newLootItem.Item.BlueprintChance,
                                    Condition = new ItemAmount.ConditionItem() { MinCondition = newLootItem.Item.Condition.MinCondition, MaxCondition = newLootItem.Item.Condition.MaxCondition },
                                    MaxAmount = newLootItem.Item.MaxAmount,
                                    MinAmount = newLootItem.Item.MinAmount,
                                    Shortname = shortname,
                                    Weight = Mathf.Max(Mathf.RoundToInt(((float)simpleLootContainerProfile.Items.Sum(x => x.Weight) * score) * multiplier), 1)
                                };

                                int index = simpleLootContainerProfile.Items.Length;

                                System.Array.Resize(ref simpleLootContainerProfile.Items, index + 1);

                                simpleLootContainerProfile.Items[index] = itemAmountWeighted;

                                additions++;
                                Debug.Log($"[AlphaLoot] - Added {shortname} to simple heli loot profile ({kvp.Key}) with a calculated weight of {itemAmountWeighted.Weight}");
                            }
                        }
                        continue;
                    }
                }
            }

            foreach (KeyValuePair<string, AdvancedLootContainerProfile> kvp in defaultBradleyLootTable.loot_advanced)
            {
                foreach (int itemid in items)
                {
                    string shortname = ItemManager.itemDictionary[itemid].shortname;

                    NewLootItem newLootItem = new NewLootItem();
                    float score = 0;
                    int multiplier = 1;

                    foreach (LootSpawnSlot lootSpawnSlot in kvp.Value.LootSpawnSlots)
                    {
                        FindItemAndCalculateScoreRecursive(lootSpawnSlot.LootDefinition, shortname, ref newLootItem, ref multiplier);
                        score += (lootSpawnSlot.Probability * newLootItem.Score) * lootSpawnSlot.NumberToSpawn;
                    }

                    if (score > 0)
                    {
                        AdvancedLootContainerProfile advancedLootProfile;
                        if (bradleyData.loot_advanced.TryGetValue(kvp.Key, out advancedLootProfile))
                        {
                            LootSpawnSlot newLootSpawnSlot = new LootSpawnSlot
                            {
                                LootDefinition = new LootSpawn()
                                {
                                    Items = new ItemAmountRanged[] { newLootItem.Item },
                                    SubSpawn = new LootSpawn.Entry[0]
                                },
                                NumberToSpawn = 1,
                                Probability = Mathf.Clamp01(score * multiplier)
                            };

                            int index = advancedLootProfile.LootSpawnSlots.Length;

                            System.Array.Resize(ref advancedLootProfile.LootSpawnSlots, index + 1);

                            advancedLootProfile.LootSpawnSlots[index] = newLootSpawnSlot;

                            additions++;
                            Debug.Log($"[AlphaLoot] - Added {shortname} to advanced bradley loot profile ({kvp.Key}) with a calculated probability of {newLootSpawnSlot.Probability}");
                        }
                        else
                        {
                            SimpleLootContainerProfile simpleLootContainerProfile;
                            if (bradleyData.loot_simple.TryGetValue(kvp.Key, out simpleLootContainerProfile))
                            {
                                ItemAmountWeighted itemAmountWeighted = new ItemAmountWeighted()
                                {
                                    BlueprintChance = newLootItem.Item.BlueprintChance,
                                    Condition = new ItemAmount.ConditionItem() { MinCondition = newLootItem.Item.Condition.MinCondition, MaxCondition = newLootItem.Item.Condition.MaxCondition },
                                    MaxAmount = newLootItem.Item.MaxAmount,
                                    MinAmount = newLootItem.Item.MinAmount,
                                    Shortname = shortname,
                                    Weight = Mathf.Max(Mathf.RoundToInt(((float)simpleLootContainerProfile.Items.Sum(x => x.Weight) * score) * multiplier), 1)
                                };

                                int index = simpleLootContainerProfile.Items.Length;

                                System.Array.Resize(ref simpleLootContainerProfile.Items, index + 1);

                                simpleLootContainerProfile.Items[index] = itemAmountWeighted;

                                additions++;
                                Debug.Log($"[AlphaLoot] - Added {shortname} to simple bradley loot profile ({kvp.Key}) with a calculated weight of {itemAmountWeighted.Weight}");
                            }
                        }
                        continue;
                    }
                }
            }
        }
        
        private void FindItemAndCalculateScoreRecursive(LootSpawn lootSpawn, string shortname, ref NewLootItem newLootItem, ref int multiplier)
        {            
            if (lootSpawn.SubSpawn.Length > 0)
            {
                foreach(LootSpawn.Entry lootSpawnEntry in lootSpawn.SubSpawn)
                {
                    FindItemAndCalculateScoreRecursive(lootSpawnEntry.Category, shortname, ref newLootItem, ref multiplier);

                    if (newLootItem.HasItem)
                    {
                        if (newLootItem.Score == 0)
                            newLootItem.Score = (float)lootSpawnEntry.Weight / (float)lootSpawn.SubSpawn.Sum(x => x.Weight);
                        else newLootItem.Score *= (float)lootSpawnEntry.Weight / (float)lootSpawn.SubSpawn.Sum(x => x.Weight);

                        return;
                    }
                }
            }
            else if (lootSpawn.Items.Length > 0)
            {
                foreach(ItemAmountRanged itemAmountRanged in lootSpawn.Items)
                {
                    if (itemAmountRanged.Shortname == shortname)
                    {
                        if (newLootItem.HasItem)
                            multiplier++;
                        newLootItem.Item = itemAmountRanged;
                    }
                }
            }
        }

        private class NewLootItem
        {
            public ItemAmountRanged Item;
            public float Score;

            public bool HasItem => Item != null;

            public void Clear()
            {
                Item = null;
                Score = 0;
            }
        }

        private class ItemList
        {
            public List<int> itemIds = new List<int>();
            public string protocol;
        }
        #endregion

        #region Components
        private class LootCycler : MonoBehaviour
        {
            private LootContainer lootContainer;

            private void Awake()
            {
                lootContainer = GetComponent<LootContainer>();
                InvokeHandler.InvokeRepeating(this, lootContainer.SpawnLoot, 3f, 3f);
            }
        }
        #endregion

        #region Commands
        [ChatCommand("aloot")]
        private void cmdRepopulateTarget(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
            {
                SendReply(player, "You do not have permission to use this command");
                return;
            }
            
            if (args == null || args.Length == 0)
            {
                SendReply(player, "/aloot repopulate - Repopulate the container you are looking at");
                SendReply(player, "/aloot view - List the contents of the container you are looking at");
                SendReply(player, "/aloot repopulateall - Repopulate every loot container on the map (can take upto 20 seconds)");
                return;
            }

            switch (args[0].ToLower())
            {
                case "repopulate":
                    {
                        LootContainer lootContainer = FindContainer(player);
                        if (lootContainer != null)
                        {
                            lootContainer.CancelInvoke(lootContainer.SpawnLoot);
                            lootContainer.SpawnLoot();

                            SendReply(player, $"Refreshed loot contents for {lootContainer.ShortPrefabName}");
                        }
                        else SendReply(player, "No loot container found");
                    }
                    return;
                case "view":
                    {
                        LootContainer lootContainer = FindContainer(player);
                        if (lootContainer != null)
                        {                            
                            SendReply(player, $"Loot contents for {lootContainer.ShortPrefabName};");
                            SendReply(player, lootContainer.inventory.itemList.Select(x => $"{x.info.displayName.english} x{x.amount}").ToSentence());
                        }
                        else SendReply(player, "No loot container found");
                    }
                    return;
                case "repopulateall":
                    {
                        SendReply(player, "Refreshing all loot containers...");
                        RefreshLootContents();
                    }
                    return;
                default:
                    SendReply(player, "Invalid syntax!");
                    break;
            }            
        }        

        [ConsoleCommand("al.repopulateall")]
        private void ccmdRepopulateAll(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                BasePlayer player = arg.Player();
                if (player != null && !permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            RefreshLootContents(arg);
        }

        [ConsoleCommand("al.additems")]
        private void ccmdAddItemsl(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                BasePlayer player = arg.Player();
                if (player != null && !permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "al.additems <shortname> <opt:shortname> <opt:shortname>... - Add the specified item(s) to your loot table.\nThis finds the containers the items are in from the default loot table, calculates a score and adds it to your existing loot table.\nYou can enter as many shortnames as you like");
                return;
            }

            AddSpecifiedItemsToLootTable(arg.Args);
        }

        [ConsoleCommand("al.search")]
        private void ccmdSearchItem(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                BasePlayer player = arg.Player();
                if (player != null && !permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            string shortname = arg.GetString(0);
            if (string.IsNullOrEmpty(shortname) || !ItemManager.itemDictionaryByName.ContainsKey(shortname))
            {
                SendReply(arg, "You must enter a valid item shortname to search");
                return;
            }

            Hash<string, int> containers = new Hash<string, int>();

            foreach (KeyValuePair<string, AdvancedLootContainerProfile> profile in storedData.loot_advanced)
            {
                int count = 0;
                foreach (LootSpawnSlot lootSpawnSlot in profile.Value.LootSpawnSlots)
                {
                    FindItemCountRecursive(lootSpawnSlot.LootDefinition, shortname, ref count);
                }

                if (count > 0)
                    containers[profile.Key] = count;
            }

            foreach (KeyValuePair<string, AdvancedNPCLootProfile> profile in storedData.npcs_advanced)
            {
                int count = 0;
                foreach (LootSpawnSlot lootSpawnSlot in profile.Value.LootSpawnSlots)
                {
                    FindItemCountRecursive(lootSpawnSlot.LootDefinition, shortname, ref count);
                }

                if (count > 0)
                    containers[profile.Key] = count;
            }

            foreach (KeyValuePair<string, SimpleLootContainerProfile> profile in storedData.loot_simple)
            {
                int count = 0;
                foreach (ItemAmountWeighted itemAmountWeighted in profile.Value.Items)
                {
                    if (itemAmountWeighted.Shortname == shortname)
                        count++;
                }

                if (count > 0)
                    containers[profile.Key] = count;
            }

            foreach (KeyValuePair<string, SimpleNPCLootProfile> profile in storedData.npcs_simple)
            {
                int count = 0;
                foreach (ItemAmountWeighted itemAmountWeighted in profile.Value.Items)
                {
                    if (itemAmountWeighted.Shortname == shortname)
                        count++;
                }

                if (count > 0)
                    containers[profile.Key] = count;
            }

            if (containers.Count == 0)
            {
                SendReply(arg, $"The item {shortname} was not found in any loot profiles");
                return;
            }
            else
            {
                SendReply(arg, $"Found item {shortname} {containers.Sum(x => x.Value)} times in {containers.Count} loot profiles;{containers.Select(x => $"\n{x.Key} (x{x.Value})").ToSentence()}");
                return;
            }
        }

        private void FindItemCountRecursive(LootSpawn lootSpawn, string shortname, ref int count)
        {
            if (lootSpawn.SubSpawn.Length > 0)
            {
                foreach (LootSpawn.Entry lootSpawnEntry in lootSpawn.SubSpawn)                
                    FindItemCountRecursive(lootSpawnEntry.Category, shortname, ref count);                
            }
            else if (lootSpawn.Items.Length > 0)
            {
                foreach (ItemAmountRanged itemAmountRanged in lootSpawn.Items)
                {
                    if (itemAmountRanged.Shortname == shortname)                    
                        count++;                    
                }
            }
        }

        [ConsoleCommand("al.setloottable")]
        private void ccmdChangeConfig(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                BasePlayer player = arg.Player();
                if (player != null && !permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length != 1)
            {
                SendReply(arg, "Invalid arguments supplied! al.setloottable \"file name\"");
                return;
            }

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"AlphaLoot/LootProfiles/{arg.Args[0]}"))
            {
                SendReply(arg, $"Unable to find a loot table with the name {arg.Args[0]}");
                return;
            }

            configData.ProfileName = arg.Args[0];
            SaveConfig();
           
            SendReply(arg, $"Loot table set to: {configData.ProfileName}");

            LoadLootTable();

            RefreshLootContents(arg);
        }

        [ConsoleCommand("al.setheliloottable")]
        private void ccmdChangeHeliConfig(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                BasePlayer player = arg.Player();
                if (player != null && !permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length != 1)
            {
                SendReply(arg, "Invalid arguments supplied! al.setheliloottable \"heli file name\"");
                return;
            }

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"AlphaLoot/LootProfiles/{arg.Args[0]}"))
            {
                SendReply(arg, $"Unable to find a heli loot table with the name {arg.Args[0]}");
                return;
            }

            configData.HeliProfileName = arg.Args[0];
            SaveConfig();

            SendReply(arg, $"Heli Loot table set to: {configData.HeliProfileName}");

            LoadHeliTable();
        }

        [ConsoleCommand("al.setbradleyloottable")]
        private void ccmdChangeBradleyConfig(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                BasePlayer player = arg.Player();
                if (player != null && !permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length != 1)
            {
                SendReply(arg, "Invalid arguments supplied! al.setbradleyloottable \"heli file name\"");
                return;
            }

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"AlphaLoot/LootProfiles/{arg.Args[0]}"))
            {
                SendReply(arg, $"Unable to find a bradley loot table with the name {arg.Args[0]}");
                return;
            }

            configData.BradleyProfileName = arg.Args[0];
            SaveConfig();

            SendReply(arg, $"Bradley Loot table set to: {configData.BradleyProfileName}");

            LoadBradleyTable();
        }

        [ConsoleCommand("al.generatetable")]
        private void ccmdGenerateTable(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                BasePlayer player = arg.Player();
                if (player != null && !permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length < 3)
            {
                SendReply(arg, "al.generatetable <filename> <heli_filename> <bradley_filename> - Generate the default loot table to the specified file");
                return;
            }

            string fileName = arg.GetString(0);
            if (fileName.Equals(configData.ProfileName, System.StringComparison.OrdinalIgnoreCase))
            {
                SendReply(arg, "The filename you entered is the same as the loot table currently being used. Change the filename to something else");
                return;
            }

            string heliFileName = arg.GetString(1);
            if (fileName.Equals(configData.HeliProfileName, System.StringComparison.OrdinalIgnoreCase))
            {
                SendReply(arg, "The heli filename you entered is the same as the heli loot table currently being used. Change the filename to something else");
                return;
            }

            string bradleyFileName = arg.GetString(2);
            if (fileName.Equals(configData.BradleyProfileName, System.StringComparison.OrdinalIgnoreCase))
            {
                SendReply(arg, "The bradley filename you entered is the same as the bradley loot table currently being used. Change the filename to something else");
                return;
            }

            StoredData storedData = new StoredData();
            StoredData heliData = new StoredData();
            StoredData bradleyData = new StoredData();

            PopulateContainerDefinitions(ref storedData, ref heliData, ref bradleyData);

            Interface.Oxide.DataFileSystem.WriteObject<StoredData>($"AlphaLoot/LootProfiles/{fileName}", storedData);
            Interface.Oxide.DataFileSystem.WriteObject<StoredData>($"AlphaLoot/LootProfiles/{heliFileName}", heliData);
            Interface.Oxide.DataFileSystem.WriteObject<StoredData>($"AlphaLoot/LootProfiles/{bradleyFileName}", bradleyData);

            SendReply(arg, $"Generated a default loot table to /oxide/data/AlphaLoot/LootProfiles/ ({fileName}.json, {heliFileName}.json and {bradleyFileName}.json)");
        }

        [ConsoleCommand("al.skins")]
        private void ccmdALSkins(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                BasePlayer player = arg.Player();
                if (player != null && !permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "al.skins add <shortname> <skinid> <opt:weight> - Add a single skin to the random skin list, with a optional argument to specify the weight of this skin");
                SendReply(arg, "al.skins remove <shortname> - Remove all skins for the specified item");
                SendReply(arg, "al.skins remove <shortname> <skinid> - Remove an individual skin");
                return;
            }

            switch (arg.Args[0].ToLower())
            {
                case "add":
                    {                        
                        string shortname = arg.GetString(1, string.Empty);
                        ulong skinId = arg.GetULong(2, 0UL);
                        int weight = arg.GetInt(3, 1);

                        if (string.IsNullOrEmpty(shortname) || ItemManager.FindItemDefinition(shortname) == null)
                        {
                            SendReply(arg, "You must enter a valid item shortname");
                            return;
                        }
                                                
                        if (!weightedSkinIds.ContainsKey(shortname))
                            weightedSkinIds[shortname] = new HashSet<SkinEntry>();

                        weightedSkinIds[shortname].Add(new SkinEntry(skinId, weight));
                        skinData.WriteObject(weightedSkinIds);

                        SendReply(arg, $"You have added the skin {skinId} for item {shortname} with a weight of {weight}");
                    }
                    return;
                case "remove":
                    {
                        string shortname = arg.GetString(1, string.Empty);
                        if (string.IsNullOrEmpty(shortname) || ItemManager.FindItemDefinition(shortname) == null)
                        {
                            SendReply(arg, "You must enter a valid item shortname");
                            return;
                        }

                        if (arg.Args.Length < 3)
                        {
                            weightedSkinIds.Remove(shortname);
                            skinData.WriteObject(weightedSkinIds);
                            SendReply(arg, $"You have removed all skins for item {shortname}");
                        }
                        else
                        {
                            ulong skinId = arg.GetULong(2, 0UL);
                            
                            HashSet<SkinEntry> list;
                            if (weightedSkinIds.TryGetValue(shortname, out list))
                            {
                                for (int i = list.Count - 1; i >= 0; i--)
                                {
                                    SkinEntry skinEntry = list.ElementAt(i);
                                    if (skinEntry.SkinID == skinId)
                                    {
                                        weightedSkinIds[shortname].Remove(skinEntry);
                                        skinData.WriteObject(weightedSkinIds);
                                        SendReply(arg, $"You have removed the skin {skinId} for item {shortname}");
                                        return;
                                    }
                                }                                
                            }
                            else
                            {
                                SendReply(arg, $"There are no skins saved for item {shortname}");
                                return;
                            }
                        }                        
                    }
                    return;
                default:
                    break;
            }
        }
        #endregion

        #region Config        
        private static ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Auto-update loot tables with new items")]
            public bool AutoUpdate { get; set; }

            [JsonProperty(PropertyName = "Global Loot Multiplier (multiplies all loot amounts by the number specified)")]
            public float GlobalMultiplier { get; set; }

            [JsonProperty(PropertyName = "Apply global and individual loot multipliers to un-stackable items")]
            public bool MultiplyUnstackable { get; set; }

            [JsonProperty(PropertyName = "Loot Table Name")]
            public string ProfileName { get; set; }

            [JsonProperty(PropertyName = "Heli Loot Table Name")]
            public string HeliProfileName { get; set; }

            [JsonProperty(PropertyName = "Bradley Loot Table Name")]
            public string BradleyProfileName { get; set; }

            [JsonProperty(PropertyName = "Amount of crates to drop (Bradley APC - default 3)")]
            public int BradleyCrates { get; set; }

            [JsonProperty(PropertyName = "Amount of crates to drop (Patrol Helicopter - default 4)")]
            public int HelicopterCrates { get; set; }

            [JsonProperty(PropertyName = "Override FancyDrop containers with supply drop profile")]
            public bool OverrideFancyDrop { get; set; }

            [JsonProperty(PropertyName = "Use skins from the SkinBox skin list")]
            public bool UseSkinboxSkins { get; set; }

            [JsonProperty(PropertyName = "Use skins from the approved skin list")]
            public bool UseApprovedSkins { get; set; }
                        
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
                AutoUpdate = false,
                GlobalMultiplier = 1f,
                MultiplyUnstackable = false,
                ProfileName = "default_loottable",
                HeliProfileName = "default_heli_loottable",
                BradleyProfileName = "default_bradley_loottable",
                BradleyCrates = 3,
                HelicopterCrates = 4,
                OverrideFancyDrop = false,
                UseSkinboxSkins = false,
                UseApprovedSkins = false,
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(3, 0, 1))
            {
                configData.BradleyCrates = 3;
                configData.HelicopterCrates = 4;
            }

            if (configData.Version < new VersionNumber(3, 0, 4))
            {
                configData.UseApprovedSkins = false;
                configData.UseSkinboxSkins = false;
            }

            if (configData.Version < new VersionNumber(3, 0, 5))
            {
                updateContainerCapacities = true;
            }

            if (configData.Version < new VersionNumber(3, 0, 14))
            {
                configData.HeliProfileName = baseConfig.HeliProfileName;
                configData.BradleyProfileName = baseConfig.BradleyProfileName;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData()
        {
            data.WriteObject(storedData);
            heli.WriteObject(heliData);
            bradley.WriteObject(bradleyData);
        }

        private void LoadData()
        {
            LoadLootTable();
            LoadHeliTable();
            LoadBradleyTable();
            LoadSkinsData();
        }

        private void LoadLootTable()
        {
            PrintWarning($"Loading Loot Table from {configData.ProfileName}.json!");

            data = Interface.Oxide.DataFileSystem.GetFile($"AlphaLoot/LootProfiles/{configData.ProfileName}");

            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }

            if (!storedData.IsValid)
            {
                PrintWarning("Invalid loot table file loaded, it contains no loot definitions! If this is a fresh install you can ignore this message, otherwise are you trying to load a ALv2.x.x loot table in to v3.x.x?");
                storedData = new StoredData();
            }
        }

        private void LoadHeliTable()
        {
            PrintWarning($"Loading Heli Loot Table from {configData.HeliProfileName}.json!");

            heli = Interface.Oxide.DataFileSystem.GetFile($"AlphaLoot/LootProfiles/{configData.HeliProfileName}");
            try
            {
                heliData = heli.ReadObject<StoredData>();
            }
            catch
            {
                heliData = new StoredData();
            }

            heliData.IsBaseLootTable = false;
            heliData.ProfileName = "heli_crate";
        }

        private void LoadBradleyTable()
        {
            PrintWarning($"Loading Bradley Loot Table from {configData.BradleyProfileName}.json!");

            bradley = Interface.Oxide.DataFileSystem.GetFile($"AlphaLoot/LootProfiles/{configData.BradleyProfileName}");
            try
            {
                bradleyData = bradley.ReadObject<StoredData>();
            }
            catch
            {
                bradleyData = new StoredData();
            }

            bradleyData.IsBaseLootTable = false;
            bradleyData.ProfileName = "bradley_crate";
        }

        private void LoadSkinsData()
        {
            skinData = Interface.Oxide.DataFileSystem.GetFile("AlphaLoot/item_skin_ids");

            try
            {
                weightedSkinIds = skinData.ReadObject<Hash<string, HashSet<SkinEntry>>>();
            }
            catch
            {
                weightedSkinIds = new Hash<string, HashSet<SkinEntry>>();
            }

            if (weightedSkinIds == null)
                weightedSkinIds = new Hash<string, HashSet<SkinEntry>>();
        }

        private void SetCapacityLimits()
        {
            int count = 0;

            foreach (KeyValuePair<string, AdvancedLootContainerProfile> kvp in storedData.loot_advanced)
            {
                if (kvp.Value.MaximumItems == -1)
                {
                    string prefabPath = string.Empty;
                    for (int i = 0; i < GameManifest.Current.entities.Length; i++)
                    {
                        string path = GameManifest.Current.entities[i];

                        if (path.EndsWith($"{kvp.Key}.prefab", System.StringComparison.OrdinalIgnoreCase))
                        {
                            prefabPath = path;
                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        LootContainer container = GameManager.server.FindPrefab(prefabPath.ToLower()).GetComponent<LootContainer>();                       
                        kvp.Value.MaximumItems = container.inventorySlots;
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                Puts($"Updated capacity limits for {count} advanced loot profiles");
                SaveData();
            }
        }

        #region Data Structure
        public class BaseLootProfile
        {
            public bool Enabled = true;

            public bool AllowSkinnedItems = true;

            public float LootMultiplier = 1;

            public int MinScrapAmount;
            public int MaxScrapAmount;

            [JsonIgnore]
            private static ItemDefinition _scrapDefinition;

            [JsonIgnore]
            public ItemDefinition ScrapDefinition
            {
                get
                {
                    if (_scrapDefinition == null)
                        _scrapDefinition = ItemManager.FindItemDefinition("scrap");
                    return _scrapDefinition;
                }
            }

            [JsonIgnore]
            private static ItemDefinition _blueprintBase;

            [JsonIgnore]
            public ItemDefinition BlueprintBaseDefinition
            {
                get
                {
                    if (_blueprintBase == null)
                        _blueprintBase = ItemManager.FindItemDefinition("blueprintbase");
                    return _blueprintBase;
                }
            }


            public int GetScrapAmount() => UnityEngine.Random.Range(MinScrapAmount, MaxScrapAmount);

            public virtual void PopulateLoot(ItemContainer container)
            {
                int scrapAmount = Mathf.RoundToInt(GetScrapAmount() * configData.GlobalMultiplier);
                if (scrapAmount > 0)
                {
                    container.capacity = container.itemList.Count + 1;

                    if (container.entityOwner is LootContainer)
                    {
                        (container.entityOwner as LootContainer).scrapAmount = scrapAmount;
                        (container.entityOwner as LootContainer).GenerateScrap();
                    }
                    else ItemManager.Create(ScrapDefinition, scrapAmount).MoveToContainer(container);
                }
                else container.capacity = container.itemList.Count;
            }
        }

        public class BaseLootContainerProfile : BaseLootProfile
        {
            public bool DestroyOnEmpty = true;
            public bool ShouldRefreshContents;
            
            public int MinSecondsBetweenRefresh = 3600;
            public int MaxSecondsBetweenRefresh = 7200;
        }

        public class SimpleNPCLootProfile : BaseLootProfile
        {
            public int MinimumItems;
            public int MaximumItems;

            public ItemAmountWeighted[] Items;

            public SimpleNPCLootProfile() { }

            public SimpleNPCLootProfile(SimpleNPCLootProfile lootContainerProfile)
            {
                AllowSkinnedItems = lootContainerProfile.AllowSkinnedItems;
               
                MinScrapAmount = lootContainerProfile.MinScrapAmount;
                MaxScrapAmount = lootContainerProfile.MaxScrapAmount;

                MinimumItems = lootContainerProfile.MinimumItems;
                MaximumItems = lootContainerProfile.MaximumItems;

                Items = lootContainerProfile.Items;

                Enabled = lootContainerProfile.Enabled;
            }

            public override void PopulateLoot(ItemContainer container)
            {
                int count = UnityEngine.Random.Range(MinimumItems, MaximumItems + 1);

                container.capacity = count;

                List<ItemAmountWeighted> items = Pool.GetList<ItemAmountWeighted>();
                items.AddRange(Items);

                int itemCount = 0;
                while (itemCount < count)
                {
                    int totalWeight = items.Sum((ItemAmountWeighted x) => x.Weight);

                    int random = UnityEngine.Random.Range(0, totalWeight);

                    for (int y = 0; y < items.Count; y++)
                    {
                        ItemAmountWeighted itemAmountWeighted = items[y];
                        ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemAmountWeighted.Shortname);

                        totalWeight -= items[y].Weight;
                        if (random >= totalWeight)
                        {
                            items.Remove(itemAmountWeighted);

                            Item item = null;
                            if (itemAmountWeighted.WantsBlueprint())
                            {
                                ItemDefinition blueprintBaseDef = BlueprintBaseDefinition;
                                if (blueprintBaseDef == null)
                                    continue;

                                item = ItemManager.Create(blueprintBaseDef);
                                item.blueprintTarget = itemAmountWeighted.ItemID;
                            }
                            else
                            {
                                item = ItemManager.CreateByItemID(itemAmountWeighted.ItemID, (int)itemAmountWeighted.GetAmount(LootMultiplier), AllowSkinnedItems ? itemAmountWeighted.RandomSkinID() : 0UL);

                                if (item.hasCondition)
                                    item.condition = itemAmountWeighted.GetConditionFraction() * item.info.condition.max;
                            }
                            if (item != null)
                            {
                                item.OnVirginSpawn();
                                if (!item.MoveToContainer(container, -1, true))
                                    item.Remove(0f);
                            }

                            itemCount++;
                            break;
                        }
                    }

                    if (items.Count == 0)
                        items.AddRange(Items);
                }

                Pool.FreeList(ref items);
                base.PopulateLoot(container);
            }
        }

        public class AdvancedNPCLootProfile : BaseLootProfile
        {       
            public LootSpawnSlot[] LootSpawnSlots;

            public int MaximumItems = -1;

            public AdvancedNPCLootProfile() { }

            public AdvancedNPCLootProfile(LootContainer.LootSpawnSlot[] lootSpawnSlots)
            {                
                LootSpawnSlots = new LootSpawnSlot[lootSpawnSlots?.Length ?? 0];

                for (int i = 0; i < lootSpawnSlots?.Length; i++)
                {
                    LootSpawnSlots[i] = new LootSpawnSlot(lootSpawnSlots[i], true);
                }
            }

            public AdvancedNPCLootProfile(AdvancedNPCLootProfile lootContainerProfile)
            {                
                MinScrapAmount = lootContainerProfile.MinScrapAmount;
                MaxScrapAmount = lootContainerProfile.MaxScrapAmount;

                MaximumItems = lootContainerProfile.MaximumItems;

                LootSpawnSlots = lootContainerProfile.LootSpawnSlots;

                AllowSkinnedItems = lootContainerProfile.AllowSkinnedItems;

                LootMultiplier = lootContainerProfile.LootMultiplier;

                Enabled = lootContainerProfile.Enabled;
            }

            public override void PopulateLoot(ItemContainer container)
            {
                if (LootSpawnSlots != null && LootSpawnSlots.Length != 0)
                {
                    container.capacity = MaximumItems == -1 ? 36 : MaximumItems;
                    for (int i = 0; i < LootSpawnSlots.Length; i++)
                    {
                        LootSpawnSlot lootSpawnSlot = LootSpawnSlots[i];
                        for (int j = 0; j < lootSpawnSlot.NumberToSpawn; j++)
                        {
                            if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.Probability)
                            {
                                lootSpawnSlot.LootDefinition.SpawnIntoContainer(container, this);
                            }
                        }
                    }
                }

                base.PopulateLoot(container);
            }
        }

        public class SimpleLootContainerProfile : BaseLootContainerProfile
        {
            public int MinimumItems;
            public int MaximumItems;

            public ItemAmountWeighted[] Items;

            public SimpleLootContainerProfile() { }

            public SimpleLootContainerProfile(SimpleLootContainerProfile lootContainerProfile)
            {                
                DestroyOnEmpty = lootContainerProfile.DestroyOnEmpty;

                AllowSkinnedItems = lootContainerProfile.AllowSkinnedItems;
                ShouldRefreshContents = lootContainerProfile.ShouldRefreshContents;
                MinSecondsBetweenRefresh = lootContainerProfile.MinSecondsBetweenRefresh;
                MaxSecondsBetweenRefresh = lootContainerProfile.MaxSecondsBetweenRefresh;

                MinScrapAmount = lootContainerProfile.MinScrapAmount;
                MaxScrapAmount = lootContainerProfile.MaxScrapAmount;

                MinimumItems = lootContainerProfile.MinimumItems;
                MaximumItems = lootContainerProfile.MaximumItems;

                Items = lootContainerProfile.Items;

                Enabled = lootContainerProfile.Enabled;
            }

            public override void PopulateLoot(ItemContainer container)
            {
                int count = UnityEngine.Random.Range(MinimumItems, MaximumItems + 1);

                container.capacity = count;

                List<ItemAmountWeighted> items = Pool.GetList<ItemAmountWeighted>();
                items.AddRange(Items);

                int itemCount = 0;
                while (itemCount < count)
                {
                    int totalWeight = items.Sum((ItemAmountWeighted x) => x.Weight);

                    int random = UnityEngine.Random.Range(0, totalWeight);

                    for (int y = 0; y < items.Count; y++)
                    {
                        ItemAmountWeighted itemAmountWeighted = items[y];
                        ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemAmountWeighted.Shortname);

                        totalWeight -= items[y].Weight;
                        if (random >= totalWeight)
                        {
                            items.Remove(itemAmountWeighted);

                            Item item = null;
                            if (itemAmountWeighted.WantsBlueprint())
                            {
                                ItemDefinition blueprintBaseDef = BlueprintBaseDefinition;
                                if (blueprintBaseDef == null)
                                    continue;

                                item = ItemManager.Create(blueprintBaseDef, 1, 0UL);
                                item.blueprintTarget = itemAmountWeighted.ItemID;
                            }
                            else
                            {
                                item = ItemManager.CreateByItemID(itemAmountWeighted.ItemID, (int)itemAmountWeighted.GetAmount(LootMultiplier), AllowSkinnedItems ? itemAmountWeighted.RandomSkinID() : 0UL);

                                if (item.hasCondition)
                                    item.condition = itemAmountWeighted.GetConditionFraction() * item.info.condition.max;
                            }
                            if (item != null)
                            {
                                item.OnVirginSpawn();
                                if (!item.MoveToContainer(container, -1, true))
                                    item.Remove(0f);
                            }

                            itemCount++;
                            break;
                        }
                    }

                    if (items.Count == 0)
                        items.AddRange(Items);
                }

                Pool.FreeList(ref items);
                base.PopulateLoot(container);
            }
        }

        public class AdvancedLootContainerProfile : BaseLootContainerProfile
        {
            public LootSpawnSlot[] LootSpawnSlots;

            public int MaximumItems = -1;

            public AdvancedLootContainerProfile() { }

            public AdvancedLootContainerProfile(LootContainer container)
            {
                bool hasCondition = container.SpawnType == LootContainer.spawnType.ROADSIDE || container.SpawnType == LootContainer.spawnType.TOWN;

                DestroyOnEmpty = container.destroyOnEmpty;
                ShouldRefreshContents = (float.IsInfinity(container.minSecondsBetweenRefresh) || float.IsInfinity(container.maxSecondsBetweenRefresh)) ? false : container.shouldRefreshContents;

                int scrapAmount;
                if (!defaultScrapAmounts.TryGetValue(container.ShortPrefabName, out scrapAmount))
                    scrapAmount = 1;

                MinScrapAmount = MaxScrapAmount = scrapAmount;

                MinSecondsBetweenRefresh = !ShouldRefreshContents ? 0 : Mathf.RoundToInt(container.minSecondsBetweenRefresh);
                MaxSecondsBetweenRefresh = !ShouldRefreshContents ? 0 : Mathf.RoundToInt(container.maxSecondsBetweenRefresh);

                MaximumItems = container.inventorySlots;

                LootSpawnSlots = new LootSpawnSlot[(container.LootSpawnSlots?.Length ?? 0) + 1];

                if (container.LootSpawnSlots?.Length > 0)
                {
                    LootSpawnSlots = new LootSpawnSlot[container.LootSpawnSlots?.Length ?? 0];
                    for (int i = 0; i < container.LootSpawnSlots?.Length; i++)
                    {
                        LootSpawnSlots[i] = new LootSpawnSlot(container.LootSpawnSlots[i], hasCondition);
                    }
                }
                else
                {
                    if (container.lootDefinition != null)
                    {
                        LootSpawnSlots = new LootSpawnSlot[]
                        {
                            new LootSpawnSlot(container.lootDefinition, container.maxDefinitionsToSpawn, hasCondition)
                        };
                    }
                }
            }

            public AdvancedLootContainerProfile(AdvancedLootContainerProfile lootContainerProfile)
            {
                DestroyOnEmpty = lootContainerProfile.DestroyOnEmpty;

                AllowSkinnedItems = lootContainerProfile.AllowSkinnedItems;

                ShouldRefreshContents = lootContainerProfile.ShouldRefreshContents;

                MinSecondsBetweenRefresh = lootContainerProfile.MinSecondsBetweenRefresh;
                MaxSecondsBetweenRefresh = lootContainerProfile.MaxSecondsBetweenRefresh;

                MinScrapAmount = lootContainerProfile.MinScrapAmount;
                MaxScrapAmount = lootContainerProfile.MaxScrapAmount;
                             
                MaximumItems = lootContainerProfile.MaximumItems;

                LootSpawnSlots = lootContainerProfile.LootSpawnSlots;

                Enabled = lootContainerProfile.Enabled;
            }

            public override void PopulateLoot(ItemContainer container)
            {
                if (LootSpawnSlots != null && LootSpawnSlots.Length != 0)
                {
                    container.capacity = MaximumItems == -1 ? 36 : MaximumItems;

                    for (int i = 0; i < LootSpawnSlots.Length; i++)
                    {
                        LootSpawnSlot lootSpawnSlot = LootSpawnSlots[i];
                        for (int j = 0; j < lootSpawnSlot.NumberToSpawn; j++)
                        {
                            if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.Probability)
                            {
                                lootSpawnSlot.LootDefinition.SpawnIntoContainer(container, this);
                            }
                        }
                    }
                }

                base.PopulateLoot(container);
            }
        }

        public class LootSpawnSlot
        {
            public LootSpawn LootDefinition;

            public int NumberToSpawn;

            public float Probability;

            public LootSpawnSlot() { }

            public LootSpawnSlot(global::LootSpawn lootSpawn, int numberToSpawn, bool hasCondition)
            {
                LootDefinition = new LootSpawn(lootSpawn, hasCondition);
                NumberToSpawn = numberToSpawn;
                Probability = 1f;
            }

            public LootSpawnSlot(LootContainer.LootSpawnSlot lootSpawnSlot, bool hasCondition)
            {
                LootDefinition = new LootSpawn(lootSpawnSlot.definition, hasCondition);
                NumberToSpawn = lootSpawnSlot.numberToSpawn;
                Probability = lootSpawnSlot.probability;
            }
        }

        public class LootSpawn
        {
            public ItemAmountRanged[] Items;

            public Entry[] SubSpawn;

            public byte[] Node = new byte[0];

            public LootSpawn() { }

            public LootSpawn(global::LootSpawn lootSpawn, bool hasCondition)
            {
                Items = new ItemAmountRanged[lootSpawn.items?.Length ?? 0];

                for (int i = 0; i < lootSpawn.items?.Length; i++)
                {
                    global::ItemAmountRanged itemAmountRanged = lootSpawn.items[i];

                    Items[i] = new ItemAmountRanged(itemAmountRanged.itemDef, itemAmountRanged.amount, itemAmountRanged.maxAmount, hasCondition);
                }

                SubSpawn = new Entry[lootSpawn.subSpawn?.Length ?? 0];

                for (int i = 0; i < lootSpawn.subSpawn?.Length; i++)
                {
                    global::LootSpawn.Entry subspawn = lootSpawn.subSpawn[i];

                    SubSpawn[i] = new Entry()
                    {
                        Category = new LootSpawn(subspawn.category, hasCondition),
                        Weight = subspawn.weight
                    };                   
                }
            }

            public void SpawnIntoContainer(ItemContainer container, BaseLootProfile lootProfile)
            {
                if (SubSpawn != null && SubSpawn.Length != 0)
                {
                    SubCategoryIntoContainer(container, lootProfile);
                    return;
                }

                if (Items != null)
                {
                    foreach (ItemAmountRanged itemAmountRanged in Items)
                    {
                        if (itemAmountRanged != null)
                        {
                            Item item = null;
                            if (itemAmountRanged.WantsBlueprint())
                            {
                                ItemDefinition blueprintBaseDef = lootProfile.BlueprintBaseDefinition;
                                if (blueprintBaseDef == null)                                
                                    continue;
                                
                                item = ItemManager.Create(blueprintBaseDef, 1, 0UL);
                                item.blueprintTarget = itemAmountRanged.ItemID;
                            }
                            else
                            {
                                item = ItemManager.CreateByItemID(itemAmountRanged.ItemID, (int)itemAmountRanged.GetAmount(lootProfile.LootMultiplier), lootProfile.AllowSkinnedItems ? itemAmountRanged.RandomSkinID() : 0UL);

                                if (item.hasCondition)
                                    item.condition = itemAmountRanged.GetConditionFraction() * item.info.condition.max;
                            }
                            if (item != null)
                            {
                                item.OnVirginSpawn();
                                if (!item.MoveToContainer(container, -1, true))                                
                                    item.Remove(0f);                                   
                            }
                        }
                    }
                }
            }

            private void SubCategoryIntoContainer(ItemContainer container, BaseLootProfile lootProfile)
            {
                int totalWeight = SubSpawn.Sum((LootSpawn.Entry x) => x.Weight);

                int random = UnityEngine.Random.Range(0, totalWeight);

                for (int i = 0; i < SubSpawn.Length; i++)
                {
                    if (SubSpawn[i].Category != null)
                    {
                        totalWeight -= SubSpawn[i].Weight;
                        if (random >= totalWeight)
                        {
                            SubSpawn[i].Category.SpawnIntoContainer(container, lootProfile);
                            return;
                        }
                    }
                }
            }

            public class Entry
            {
                public LootSpawn Category;

                public int Weight;

                public byte[] Node = new byte[0];
            }
        }

        public class ItemAmountWeighted : ItemAmountRanged
        {
            public int Weight = 1;
        }

        public class ItemAmountRanged : ItemAmount
        {
            public float MaxAmount = -1f;

            public ItemAmountRanged() : base() { }

            public ItemAmountRanged(ItemDefinition item = null, float amount = 0f, float maxAmount = -1f, bool hasCondition = false) : base(item, amount, hasCondition)
            {
                this.MaxAmount = Mathf.Max(maxAmount, amount);
            }

            public override float GetAmount(float lootMultiplier)
            {
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(ItemID);                
                bool isStackable = (itemDefinition.stackable > 1 && !itemDefinition.condition.enabled) || configData.MultiplyUnstackable;

                if (this.MinAmount == this.MaxAmount)
                {
                    if (!isStackable)
                        return Mathf.Clamp(this.MinAmount, 1, int.MaxValue);

                    return Mathf.Clamp((this.MinAmount * lootMultiplier) * configData.GlobalMultiplier, 1, int.MaxValue);
                }

                if (!isStackable)
                    return (int)Mathf.Clamp(UnityEngine.Random.Range(this.MinAmount, this.MaxAmount), 1, int.MaxValue);

                return (int)Mathf.Clamp((UnityEngine.Random.Range(this.MinAmount, this.MaxAmount) * lootMultiplier) * configData.GlobalMultiplier, 1, int.MaxValue);
            }
        }
                
        public class ItemAmount
        {
            public string Shortname;

            public float BlueprintChance;
           
            public float MinAmount;

            public ConditionItem Condition;

            [JsonIgnore]
            private int _itemId = -1;

            [JsonIgnore]
            public int ItemID
            {
                get
                {
                    if (_itemId < 0)
                        _itemId = ItemManager.FindItemDefinition(Shortname).itemid;
                    return _itemId;
                }
            }
            
            public ItemAmount() { }

            public ItemAmount(ItemDefinition item = null, float amount = 0f, bool hasCondition= false)
            {
                Shortname = item.shortname;

                BlueprintChance = item.spawnAsBlueprint ? 1f : 0f;

                MinAmount = amount;

                Condition = new ConditionItem
                {
                    MinCondition = hasCondition && item.condition.enabled ? item.condition.foundCondition.fractionMin : 1f,
                    MaxCondition = hasCondition && item.condition.enabled ? item.condition.foundCondition.fractionMax : 1f
                };
            }

            public virtual float GetAmount(float lootMultiplier)
            {
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(ItemID);
                bool isStackable = (itemDefinition.stackable > 1 && !itemDefinition.condition.enabled) || configData.MultiplyUnstackable;

                if (!isStackable)
                    return (int)Mathf.Clamp(this.MinAmount, 1, int.MaxValue);

                return (int)Mathf.Clamp((MinAmount * lootMultiplier) * configData.GlobalMultiplier, 1, int.MaxValue);
            }

            public ulong RandomSkinID()
            {                
                HashSet<SkinEntry> hashset;
                if (weightedSkinIds.TryGetValue(Shortname, out hashset) && hashset.Count > 0)
                {
                    int totalWeight = hashset.Sum((SkinEntry x) => x.Weight);

                    int random = UnityEngine.Random.Range(0, totalWeight);

                    foreach (SkinEntry skinEntry in hashset)
                    {
                        totalWeight -= skinEntry.Weight;

                        if (random >= totalWeight)                        
                            return skinEntry.SkinID;                        
                    }

                }

                if (importedSkinIds != null)
                {
                    List<ulong> list;
                    if (importedSkinIds.TryGetValue(Shortname, out list) && list.Count > 0)
                    {
                        return list.GetRandom();
                    }
                }

                return 0UL;           
            }

            public float GetConditionFraction() => UnityEngine.Random.Range(Condition.MinCondition, Condition.MaxCondition);

            public bool WantsBlueprint() => UnityEngine.Random.Range(0.0f, 1.0f) < BlueprintChance;

            public class ConditionItem
            {
                public float MinCondition;

                public float MaxCondition;
            }
        }

        public class SkinEntry
        {
            public int Weight;
            public ulong SkinID;

            public SkinEntry() { }

            public SkinEntry(ulong skinId, int weight = 1)
            {
                this.SkinID = skinId;
                this.Weight = weight;
            }
        }
        #endregion

        private class StoredData
        {
            public Hash<string, SimpleLootContainerProfile> loot_simple = new Hash<string, SimpleLootContainerProfile>();

            public Hash<string, AdvancedLootContainerProfile> loot_advanced = new Hash<string, AdvancedLootContainerProfile>();

            public Hash<string, AdvancedNPCLootProfile> npcs_advanced = new Hash<string, AdvancedNPCLootProfile>();

            public Hash<string, SimpleNPCLootProfile> npcs_simple = new Hash<string, SimpleNPCLootProfile>();

            public bool IsBaseLootTable = true;

            public string ProfileName = string.Empty;
           
            [JsonIgnore]
            public bool IsValid => loot_simple != null && loot_advanced != null && npcs_advanced != null && npcs_simple != null && (loot_advanced.Count > 0 || loot_simple.Count > 0) && (npcs_advanced.Count != 0 || npcs_simple.Count != 0);

            [JsonIgnore]
            public bool HasAnyProfiles => loot_simple != null && loot_advanced != null && npcs_advanced != null && npcs_simple != null && (loot_advanced.Count > 0 || loot_simple.Count > 0 || npcs_advanced.Count > 0 || npcs_simple.Count > 0);

            public void CreateDefaultLootProfile(LootContainer container)
            {                
                string shortPrefabName = container.ShortPrefabName;
                if (string.IsNullOrEmpty(shortPrefabName))                
                    shortPrefabName = container.name;
                
                if (loot_advanced.ContainsKey(shortPrefabName) || loot_simple.ContainsKey(shortPrefabName))
                    return;

                loot_advanced.Add(shortPrefabName, new AdvancedLootContainerProfile(container));
            }

            public void CloneLootProfile(string shortname, BaseLootProfile lootContainerProfile)
            {
                if (lootContainerProfile is AdvancedLootContainerProfile)
                {
                    loot_advanced[shortname] = new AdvancedLootContainerProfile(lootContainerProfile as AdvancedLootContainerProfile);
                }
                else if (lootContainerProfile is SimpleLootContainerProfile)
                {
                    loot_simple[shortname] = new SimpleLootContainerProfile(lootContainerProfile as SimpleLootContainerProfile);
                }
                else if (lootContainerProfile is AdvancedNPCLootProfile)
                {
                    npcs_advanced[shortname] = new AdvancedNPCLootProfile(lootContainerProfile as AdvancedNPCLootProfile);
                }
                else if (lootContainerProfile is SimpleNPCLootProfile)
                {
                    npcs_simple[shortname] = new SimpleNPCLootProfile(lootContainerProfile as SimpleNPCLootProfile);
                }
            }

            public void CreateDefaultLootProfile(string shortPrefabName, LootContainer.LootSpawnSlot[] lootSpawnSlots)
            {                
                if (npcs_advanced.ContainsKey(shortPrefabName) || npcs_simple.ContainsKey(shortPrefabName))
                    return;

                npcs_advanced.Add(shortPrefabName, new AdvancedNPCLootProfile(lootSpawnSlots));
            }

            public bool TryGetLootProfile(string shortname, out BaseLootContainerProfile profile)
            {
                AdvancedLootContainerProfile advancedLootContainerProfile;
                if (loot_advanced.TryGetValue(shortname, out advancedLootContainerProfile))
                {
                    profile = advancedLootContainerProfile;
                    return true;
                }

                SimpleLootContainerProfile simpleLootContainerProfile;
                if (loot_simple.TryGetValue(shortname, out simpleLootContainerProfile))
                {
                    profile = simpleLootContainerProfile;
                    return true;
                }

                profile = null;
                return false;
            }

            public bool TryGetNPCProfile(string shortname, out BaseLootProfile profile)
            {
                AdvancedNPCLootProfile advancedNPCLootProfile;
                if (npcs_advanced.TryGetValue(shortname, out advancedNPCLootProfile))
                {
                    profile = advancedNPCLootProfile;
                    return true;
                }

                SimpleNPCLootProfile simpleNPCLootProfile;
                if (npcs_simple.TryGetValue(shortname, out simpleNPCLootProfile))
                {
                    profile = simpleNPCLootProfile;
                    return true;
                }

                profile = null;
                return false;
            }

            #region Random Profiles
            [JsonIgnore]
            private List<BaseLootContainerProfile> randomList = new List<BaseLootContainerProfile>();

            public bool GetRandomLootProfile(out BaseLootContainerProfile profile)
            {
                if (randomList.Count == 0)
                {
                    randomList.AddRange(loot_simple.Values);
                    randomList.AddRange(loot_advanced.Values);
                }

                RESTART_RANDOM:
                if (randomList.Count == 0)
                {
                    profile = null;
                    return false;
                }

                profile = randomList.GetRandom();                
                randomList.Remove(profile);

                if (!profile.Enabled)
                    goto RESTART_RANDOM;

                return true;
            }
            #endregion

            public void RemoveProfile(string shortname)
            {
                loot_simple.Remove(shortname);
                loot_advanced.Remove(shortname);
                npcs_simple.Remove(shortname);
                npcs_advanced.Remove(shortname);
            }
        }
        #endregion

        private List<string> npcPrefabs = new List<string>
        {
            "assets/prefabs/npc/bandit/guard/bandit_guard.prefab",
            "assets/prefabs/npc/murderer/murderer.prefab",
            "assets/prefabs/npc/scarecrow/scarecrow.prefab",
            "assets/prefabs/npc/scientist/groundpatrolpurple.prefab",
            "assets/prefabs/npc/scientist/htn/scientist_astar_full_any.prefab",
            "assets/prefabs/npc/scientist/htn/scientist_full_any.prefab",
            "assets/prefabs/npc/scientist/htn/scientist_full_lr300.prefab",
            "assets/prefabs/npc/scientist/htn/scientist_full_mp5.prefab",
            "assets/prefabs/npc/scientist/htn/scientist_full_pistol.prefab",
            "assets/prefabs/npc/scientist/htn/scientist_full_shotgun.prefab",
            "assets/prefabs/npc/scientist/htn/scientist_junkpile_pistol.prefab",
            "assets/prefabs/npc/scientist/htn/scientist_turret_any.prefab",
            "assets/prefabs/npc/scientist/htn/scientist_turret_lr300.prefab",
            "assets/prefabs/npc/scientist/scientist.prefab",
            "assets/prefabs/npc/scientist/scientist_gunner.prefab",
            "assets/prefabs/npc/scientist/scientistjunkpile.prefab",
            "assets/prefabs/npc/scientist/scientistpeacekeeper.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/heavyscientist/heavyscientist.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/humannpc.prefab",
            "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc.prefab",
            "assets/rust.ai/agents/npcplayer/npcplayertest.prefab",
        };
    }
}
