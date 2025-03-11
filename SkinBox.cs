using Newtonsoft.Json;
using Oxide.Core.Libraries;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("SkinBox", "k1lly0u", "2.0.4"), Description("Allows you to reskin item's by placing it in the SkinBox and selecting a new skin")]
    class SkinBox : RustPlugin
    {
        #region Fields
        [PluginReference]
        private Plugin ServerRewards, Economics;


        private Hash<string, HashSet<ulong>> _skinList = new Hash<string, HashSet<ulong>>();

        private Hash<ulong, string> _skinPermissions = new Hash<ulong, string>();

        private Hash<ulong, LootHandler> _activeSkinBoxes = new Hash<ulong, LootHandler>();

        private Hash<string, string> _shortnameToDisplayname = new Hash<string, string>();

        private Hash<ulong, string> _skinNameLookup = new Hash<ulong, string>();

        private Hash<ulong, double> _cooldownTimes = new Hash<ulong, double>();


        private bool _apiKeyMissing = false;

        private bool _skinsLoaded = false;


        private CostType _costType;


        private static SkinBox Instance { get; set; }

        private static Func<string, ulong, string> GetMessage;

        private const string COFFIN_PREFAB = "assets/prefabs/misc/halloween/coffin/coffinstorage.prefab";

        private const int SCRAP_ITEM_ID = -932201673;

        private enum CostType { Scrap, ServerRewards, Economics }
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            Instance = this;

            Configuration.Permission.RegisterPermissions(permission, this);
            Configuration.Permission.ReverseCustomSkinPermissions(ref _skinPermissions);

            Configuration.Command.RegisterCommands(cmd, this);

            _costType = ParseType<CostType>(Configuration.Cost.Currency);

            GetMessage = (string key, ulong userId) => lang.GetMessage(key, this, userId.ToString());
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoAPIKey"] = "The server owner has not entered a Steam API key in the config. Unable to continue!",
                ["SkinsLoading"] = "SkinBox is still gathering skins. Please try again soon",
                ["NoPermission"] = "You don't have permission to use the SkinBox",
                ["ToNearPlayer"] = "The SkinBox is currently not usable at this place",
                ["CooldownTime"] = "You need to wait {0} seconds to use the SkinBox again",

                ["NotEnoughBalanceOpen"] = "You need at least {0} {1} to open the SkinBox",
                ["NotEnoughBalanceUse"] = "You would need at least {0} {1} to skin {2}",
                ["NotEnoughBalanceTake"] = "{0} was not skinned. You do not have enough {1}",
                ["CostToUse"] = "Skinning a item is not free!\n{0} {3} to skin a deployable\n{1} {3} to skin a weapon\n{2} {3} to skin attire",

                ["Cost.Scrap"] = "Scrap",
                ["Cost.ServerRewards"] = "RP",
                ["Cost.Economics"] = "Eco",
            }, this);
        }

        private void OnServerInitialized()
        {
            if (string.IsNullOrEmpty(Configuration.Skins.APIKey))
            {
                _apiKeyMissing = true;
                SendAPIMissingWarning();                
                return;
            }

            if ((Steamworks.SteamInventory.Definitions?.Length ?? 0) == 0)
            {
                PrintWarning("Waiting for Steamworks to update item definitions....");
                Steamworks.SteamInventory.OnDefinitionsUpdated += StartSkinRequest;
            }
            else StartSkinRequest();
        }

        private object CanAcceptItem(ItemContainer container, Item item)
        {
            if (container.entityOwner == null)
                return null;

            LootHandler lootHandler = container.entityOwner.GetComponent<LootHandler>();
            if (lootHandler == null)
                return null;

            if (item.isBroken || lootHandler.HasItem || !HasItemPermissions(lootHandler.Looter, item))
                return ItemContainer.CanAcceptResult.CannotAccept;

            string shortname = GetRedirectedShortname(item.info.shortname);

            if (!Configuration.Skins.UseRedirected && !shortname.Equals(item.info.shortname, StringComparison.OrdinalIgnoreCase))
                return ItemContainer.CanAcceptResult.CannotAccept;

            HashSet<ulong> skins;
            if (!_skinList.TryGetValue(shortname, out skins) || skins.Count == 0)
            {
                Debug.Log(skins?.Count ?? 0);
                return ItemContainer.CanAcceptResult.CannotAccept;
            }

            int reskinCost = GetReskinCost(item);

            if (reskinCost > 0 && !CanAffordToUse(lootHandler.Looter, reskinCost))
            {
                lootHandler.PopupMessage(string.Format(GetMessage("NotEnoughBalanceUse", lootHandler.Looter.userID), 
                                                                   reskinCost,
                                                                   GetCostType(lootHandler.Looter.userID),
                                                                   item.info.displayName.english));

                return ItemContainer.CanAcceptResult.CannotAccept;

            }

            return null;
        }

        private void OnItemSplit(Item item, int amount)
        {
            LootHandler lootHandler = item.parent?.entityOwner?.GetComponent<LootHandler>();
            if (lootHandler != null)
                lootHandler.CheckItemHasSplit(item);
        }        

        private object CanMoveItem(Item movedItem, PlayerInventory inventory, uint targetContainerID, int targetSlot, int amount)
        {
            if (movedItem.parent?.entityOwner != null)
            {
                LootHandler lootHandler = movedItem.parent?.entityOwner?.GetComponent<LootHandler>();
                if (lootHandler != null && lootHandler.InputAmount > 1)
                {
                    ItemContainer targetContainer = inventory.FindContainer(targetContainerID);
                    if (targetContainer != null && targetContainer.GetSlot(targetSlot) != null)
                        return false;
                }
            }
            return null;
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item) => container?.entityOwner?.GetComponent<LootHandler>()?.OnItemAdded(item);

        private void OnItemRemovedFromContainer(ItemContainer container, Item item) => container?.entityOwner?.GetComponent<LootHandler>()?.OnItemRemoved(item);

        private void OnLootEntityEnd(BasePlayer player, StorageContainer storageContainer)
        {
            LootHandler lootHandler;
            if (_activeSkinBoxes.TryGetValue(player.userID, out lootHandler))            
                UnityEngine.Object.Destroy(lootHandler);            
        }

        private void Unload()
        {
            LootHandler[] lootHandlers = UnityEngine.Object.FindObjectsOfType<LootHandler>();
            for (int i = 0; i < lootHandlers.Length; i++)
            {
                LootHandler lootHandler = lootHandlers[i];
                if (lootHandler.Looter != null)                
                    lootHandler.Looter.EndLooting();                
                UnityEngine.Object.Destroy(lootHandler);
            }

            Configuration = null;
            Instance = null;
        }
        #endregion

        #region Functions
        private void SendAPIMissingWarning()
        {
            Debug.LogWarning("You must enter a Steam API key in the config!\nYou can get a API key here -> https://steamcommunity.com/dev/apikey \nOnce you have your API key copy it to the 'Skin Options/Steam API Key' field in your SkinBox.json config file");
        }

        private void ChatMessage(BasePlayer player, string key, params object[] args)
        {
            if (args == null)
                player.ChatMessage(lang.GetMessage(key, this, player.UserIDString));
            else player.ChatMessage(string.Format(lang.GetMessage(key, this, player.UserIDString), args));
        }

        private void CreateSkinBox(BasePlayer player)
        {
            StorageContainer container = GameManager.server.CreateEntity(COFFIN_PREFAB, player.transform.position + (Vector3.down * 250f)) as StorageContainer;
            container.limitNetworking = true;
            container.enableSaving = false;

            UnityEngine.Object.Destroy(container.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.Destroy(container.GetComponent<GroundWatch>());

            container.Spawn();

            LootHandler lootHandler;
            if (_activeSkinBoxes.TryGetValue(player.userID, out lootHandler))
            {
                player.EndLooting();
                UnityEngine.Object.Destroy(lootHandler);
            }

            lootHandler = container.gameObject.AddComponent<LootHandler>();
            lootHandler.Looter = player;

            player.inventory.loot.Clear();
            player.inventory.loot.PositionChecks = false;
            player.inventory.loot.entitySource = container;
            player.inventory.loot.itemSource = null;
            player.inventory.loot.MarkDirty();
            player.inventory.loot.AddContainer(container.inventory);
            player.inventory.loot.SendImmediate();

            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", container.panelName);
            container.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);

            if (Configuration.Cost.Enabled && !permission.UserHasPermission(player.UserIDString, Configuration.Permission.NoCost))
            {
                lootHandler.PopupMessage(string.Format(GetMessage("CostToUse", lootHandler.Looter.userID), Configuration.Cost.Deployable, 
                                                                                                           Configuration.Cost.Weapon, 
                                                                                                           Configuration.Cost.Attire,
                                                                                                           GetCostType(player.userID)));
            }

            _activeSkinBoxes[player.userID] = lootHandler;
        }
        #endregion

        #region Helpers
        private void GetSkinsFor(BasePlayer player, string shortname, ref List<ulong> list)
        {
            list.Clear();

            HashSet<ulong> skins;
            if (_skinList.TryGetValue(shortname, out skins))
            {                
                foreach(ulong skinId in skins)
                {
                    string perm;
                    if (_skinPermissions.TryGetValue(skinId, out perm) && !permission.UserHasPermission(player.UserIDString, perm))
                        continue;

                    if (Configuration.Blacklist.Contains(skinId) && player.net.connection.authLevel < Configuration.Other.BlacklistAuth)
                        continue;

                    list.Add(skinId);
                }
            }
        }

        private bool HasItemPermissions(BasePlayer player, Item item)
        {
            switch (item.info.category)
            {
                case ItemCategory.Weapon:
                case ItemCategory.Tool:
                    return permission.UserHasPermission(player.UserIDString, Configuration.Permission.Weapon);
                case ItemCategory.Construction:                   
                case ItemCategory.Items:
                    return permission.UserHasPermission(player.UserIDString, Configuration.Permission.Deployable);
                case ItemCategory.Attire:
                    return permission.UserHasPermission(player.UserIDString, Configuration.Permission.Attire);
                default:
                    return true;
            }            
        }
        
        private T ParseType<T>(string type)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), type, true);
            }
            catch
            {
                return default(T);
            }
        }

        private double CurrentTime => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        #endregion

        #region Usage Costs 
        private string GetCostType(ulong playerId) => GetMessage($"Cost.{_costType}", playerId);

        private int GetReskinCost(Item item)
        {
            if (!Configuration.Cost.Enabled)
                return 0;

            switch (item.info.category)
            {
                case ItemCategory.Weapon:
                case ItemCategory.Tool:
                    return Configuration.Cost.Weapon;
                case ItemCategory.Construction:
                case ItemCategory.Items:
                    return Configuration.Cost.Deployable;
                case ItemCategory.Attire:
                    return Configuration.Cost.Attire;
                default:
                    return 0;
            }
        }

        private bool CanAffordToUse(BasePlayer player, int amount)
        {
            if (!Configuration.Cost.Enabled || amount == 0 || permission.UserHasPermission(player.UserIDString, Configuration.Permission.NoCost))
                return true;

            switch (_costType)
            {
                case CostType.Scrap:
                    return player.inventory.GetAmount(SCRAP_ITEM_ID) >= amount;
                case CostType.ServerRewards:
                    return (int)ServerRewards?.Call("CheckPoints", player.userID) >= amount;
                case CostType.Economics:
                    return (double)Economics?.Call("Balance", player.UserIDString) >= amount;                
            }

            return false;
        }

        private bool ChargePlayer(BasePlayer player, ItemCategory itemCategory)
        {
            if (!Configuration.Cost.Enabled || permission.UserHasPermission(player.UserIDString, Configuration.Permission.NoCost))
                return true;

            int amount = itemCategory == ItemCategory.Weapon || itemCategory == ItemCategory.Tool ? Configuration.Cost.Weapon :
                         itemCategory == ItemCategory.Items || itemCategory == ItemCategory.Construction ? Configuration.Cost.Deployable :
                         itemCategory == ItemCategory.Attire ? Configuration.Cost.Attire : 0;

            return ChargePlayer(player, amount);
        }

        private bool ChargePlayer(BasePlayer player, int amount)
        {
            if (amount == 0 || !Configuration.Cost.Enabled || permission.UserHasPermission(player.UserIDString, Configuration.Permission.NoCost))
                return true;

            switch (_costType)
            {
                case CostType.Scrap:
                    if (amount <= player.inventory.GetAmount(SCRAP_ITEM_ID))
                    {
                        player.inventory.Take(null, SCRAP_ITEM_ID, amount);
                        return true;
                    }
                    return false;
                case CostType.ServerRewards:
                    return (bool)ServerRewards?.Call("TakePoints", player.userID, amount);
                case CostType.Economics:
                    return (bool)Economics?.Call("Withdraw", player.UserIDString, (double)amount);
            }
            return false;
        }
        #endregion

        #region Cooldown
        private void ApplyCooldown(BasePlayer player)
        {
            if (!Configuration.Cooldown.Enabled || permission.UserHasPermission(player.UserIDString, Configuration.Permission.NoCooldown))
                return;

            _cooldownTimes[player.userID] = CurrentTime + Configuration.Cooldown.Time;
        }

        private bool IsOnCooldown(BasePlayer player)
        {
            if (!Configuration.Cooldown.Enabled || permission.UserHasPermission(player.UserIDString, Configuration.Permission.NoCooldown))
                return false;

            double time;
            if (_cooldownTimes.TryGetValue(player.userID, out time) && time > CurrentTime)
                return true;

            return false;
        }

        private bool IsOnCooldown(BasePlayer player, out double remaining)
        {
            remaining = 0;

            if (!Configuration.Cooldown.Enabled || permission.UserHasPermission(player.UserIDString, Configuration.Permission.NoCooldown))
                return false;

            double time;
            if (_cooldownTimes.TryGetValue(player.userID, out time) && time > CurrentTime)
            {
                remaining = time - CurrentTime;
                return true;
            }
            
            return false;
        }
        #endregion

        #region Approved Skins
        private void StartSkinRequest()
        {
            Steamworks.SteamInventory.OnDefinitionsUpdated -= StartSkinRequest;

            UpdateWorkshopNameConversionList();

            FindItemRedirects();

            if (!Configuration.Skins.UseApproved && !Configuration.Skins.UseWorkshop)
            {
                PrintError("You have approved skins and workshop skins disabled. This leaves no skins available to use in SkinBox!");
                return;
            }

            if (!Configuration.Skins.UseApproved && Configuration.Skins.UseWorkshop)
            {
                VerifyWorkshopSkins();
                return;
            }

            PrintWarning("Retrieving approved skin lists...");

            CollectApprovedSkins();
        }

        private void CollectApprovedSkins()
        {
            int count = 0;

            bool addApprovedPermission = Configuration.Permission.Approved != Configuration.Permission.Use;

            bool updateConfig = false;

            List<ulong> list = Facepunch.Pool.GetList<ulong>();

            List<int> itemSkinDirectory = Facepunch.Pool.GetList<int>();
            itemSkinDirectory.AddRange(ItemSkinDirectory.Instance.skins.Select(x => x.id));

            foreach (ItemDefinition itemDefinition in ItemManager.itemList)
            {
                list.Clear();                 

                foreach (Steamworks.InventoryDef item in Steamworks.SteamInventory.Definitions)
                {
                    string shortname = item.GetProperty("itemshortname");
                    if (string.IsNullOrEmpty(shortname) || item.Id < 100)
                        continue;
                                        
                    if (_workshopNameToShortname.ContainsKey(shortname))
                        shortname = _workshopNameToShortname[shortname];

                    if (!shortname.Equals(itemDefinition.shortname, StringComparison.OrdinalIgnoreCase))  
                        continue;                    

                    ulong skinId;

                    if (itemSkinDirectory.Contains(item.Id))
                        skinId = (ulong)item.Id;
                    else if (!ulong.TryParse(item.GetProperty("workshopid"), out skinId))
                        continue;

                    if (list.Contains(skinId) || Configuration.Skins.ApprovedLimit > 0 && list.Count >= Configuration.Skins.ApprovedLimit)                    
                        continue;
                    
                    list.Add(skinId);

                    _skinNameLookup[skinId] = item.Name;
                }

                if (list.Count > 1)
                {
                    count += list.Count;

                    HashSet<ulong> skins;
                    if (!_skinList.TryGetValue(itemDefinition.shortname, out skins))
                        skins = _skinList[itemDefinition.shortname] = new HashSet<ulong>();

                    int removeCount = 0;

                    list.ForEach((ulong skin) =>
                    {
                        if (Configuration.Skins.RemoveApproved && Configuration.SkinList.ContainsKey(itemDefinition.shortname) && 
                                                                  Configuration.SkinList[itemDefinition.shortname].Contains(skin))
                        {
                            Configuration.SkinList[itemDefinition.shortname].Remove(skin);
                            removeCount++;
                            updateConfig = true;
                        }

                        skins.Add(skin);

                        if (addApprovedPermission)
                            _skinPermissions[skin] = Configuration.Permission.Approved;
                    });

                    if (removeCount > 0)
                        Debug.Log($"[SkinBox] Removed {removeCount} approved skin ID's for {itemDefinition.shortname} from the config skin list");
                }
            }

            if (updateConfig)
                SaveConfig();

            Facepunch.Pool.FreeList(ref list);
            Facepunch.Pool.FreeList(ref itemSkinDirectory);

            Debug.Log($"[SkinBox] - Loaded {count} approved skins");

            if (Configuration.Skins.UseWorkshop && Configuration.SkinList.Sum(x => x.Value.Count) > 0)
                VerifyWorkshopSkins();
            else
            {
                _skinsLoaded = true;
                Interface.Oxide.CallHook("OnSkinBoxSkinsLoaded", _skinList);
                Debug.Log($"[SkinBox] - SkinBox has loaded all required skins and is ready to use! ({_skinList.Values.Sum(x => x.Count)} skins acrosss {_skinList.Count} items)");
            }
        }
        #endregion

        #region Workshop Skins
        private List<ulong> _skinsToVerify = new List<ulong>();

        private const string PUBLISHED_FILE_DETAILS = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
        private const string COLLECTION_DETAILS = "https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/";
        private const string ITEMS_BODY = "?key={0}&itemcount={1}";
        private const string ITEM_ENTRY = "&publishedfileids[{0}]={1}";
        private const string COLLECTION_BODY = "?key={0}&collectioncount=1&publishedfileids[0]={1}";

        private void VerifyWorkshopSkins()
        {
            foreach (HashSet<ulong> list in Configuration.SkinList.Values)
                _skinsToVerify.AddRange(list);

            SendWorkshopQuery();
        }

        private void SendWorkshopQuery(int page = 0, ConsoleSystem.Arg arg = null, string perm = null)
        {
            int totalPages = Mathf.CeilToInt((float)_skinsToVerify.Count / 100f);
            int index = page * 100;
            int limit = Mathf.Min((page + 1) * 100, _skinsToVerify.Count);
            string details = string.Format(ITEMS_BODY, Configuration.Skins.APIKey, (limit - index));

            for (int i = index; i < limit; i++)
            {
                details += string.Format(ITEM_ENTRY, i - index, _skinsToVerify[i]);
            }

            try
            {
                webrequest.Enqueue(PUBLISHED_FILE_DETAILS, details, (code, response) => ServerMgr.Instance.StartCoroutine(ValidateRequiredSkins(code, response, page + 1, totalPages, false, arg, perm)), this, RequestMethod.POST);
            }
            catch { }
        }

        private void SendWorkshopCollectionQuery(ulong collectionId, bool add, ConsoleSystem.Arg arg = null, string perm = null)
        {
            string details = string.Format(COLLECTION_BODY, Configuration.Skins.APIKey, collectionId);

            try
            {
                webrequest.Enqueue(COLLECTION_DETAILS, details, (code, response) => ServerMgr.Instance.StartCoroutine(ProcessCollectionRequest(code, response, add, arg, perm)), this, RequestMethod.POST);
            }
            catch { }
        }
       
        private IEnumerator ValidateRequiredSkins(int code, string response, int page, int totalPages, bool isCollection, ConsoleSystem.Arg arg, string perm)
        {
            bool hasChanged = false;
            if (response != null && code == 200)
            {
                QueryResponse queryRespone = JsonConvert.DeserializeObject<QueryResponse>(response);
                if (queryRespone != null && queryRespone.response != null && queryRespone.response.publishedfiledetails?.Length > 0)
                {
                    SendResponse($"Processing workshop response. Page: {page} / {totalPages}", arg);                    

                    foreach (PublishedFileDetails publishedFileDetails in queryRespone.response.publishedfiledetails)
                    {
                        if (publishedFileDetails.tags != null)
                        {
                            foreach (PublishedFileDetails.Tag tag in publishedFileDetails.tags)
                            {                                
                                if (string.IsNullOrEmpty(tag.tag))
                                    continue;

                                ulong workshopid = Convert.ToUInt64(publishedFileDetails.publishedfileid);

                                string adjTag = tag.tag.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "").Replace(".item", "");
                                if (_workshopNameToShortname.ContainsKey(adjTag))
                                {
                                    string shortname = _workshopNameToShortname[adjTag];

                                    if (shortname == "ammo.snowballgun")
                                        continue;

                                    HashSet<ulong> skins;
                                    if (!_skinList.TryGetValue(shortname, out skins))
                                        skins = _skinList[shortname] = new HashSet<ulong>();

                                    if (!skins.Contains(workshopid))
                                    {
                                        skins.Add(workshopid);
                                        _skinNameLookup[workshopid] = publishedFileDetails.title;
                                    }

                                    HashSet<ulong> configSkins;
                                    if (!Configuration.SkinList.TryGetValue(shortname, out configSkins))
                                        configSkins = Configuration.SkinList[shortname] = new HashSet<ulong>();

                                    if (!configSkins.Contains(workshopid))
                                    {
                                        hasChanged = true;
                                        configSkins.Add(workshopid);
                                    }

                                    if (!string.IsNullOrEmpty(perm) && !Configuration.Permission.Custom[perm].Contains(workshopid))
                                    {
                                        hasChanged = true;
                                        Configuration.Permission.Custom[perm].Add(workshopid);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            yield return CoroutineEx.waitForEndOfFrame;
            yield return CoroutineEx.waitForEndOfFrame;

            if (hasChanged)
                SaveConfig();

            if (page < totalPages)
                SendWorkshopQuery(page);
            else
            {
                if (!_skinsLoaded)
                {
                    _skinsLoaded = true;
                    Interface.Oxide.CallHook("OnSkinBoxSkinsLoaded", _skinList);
                    Debug.Log($"[SkinBox] - SkinBox has loaded all required skins and is ready to use! ({_skinList.Values.Sum(x => x.Count)} skins acrosss {_skinList.Count} items)");
                }
                else SendResponse("New skins have been added!", arg);
            }
        }

        private IEnumerator ProcessCollectionRequest(int code, string response, bool add, ConsoleSystem.Arg arg, string perm)
        {
            if (response != null && code == 200)
            {
                SendResponse("Processing collection response", arg);

                CollectionQueryResponse collectionQuery = JsonConvert.DeserializeObject<CollectionQueryResponse>(response);
                if (collectionQuery == null || !(collectionQuery is CollectionQueryResponse))
                {
                    SendResponse("Failed to receive a valid workshop collection response", arg);
                    yield break;
                }

                if (collectionQuery.response.resultcount == 0 || collectionQuery.response.collectiondetails == null ||
                    collectionQuery.response.collectiondetails.Length == 0 || collectionQuery.response.collectiondetails[0].result != 1)
                {
                    SendResponse("Failed to receive a valid workshop collection response", arg);
                    yield break;
                }

                _skinsToVerify.Clear();

                foreach (CollectionChild child in collectionQuery.response.collectiondetails[0].children)
                {
                    try
                    {
                        _skinsToVerify.Add(Convert.ToUInt64(child.publishedfileid));
                    }
                    catch { }
                }

                if (_skinsToVerify.Count == 0)
                {
                    SendResponse("No valid skin ID's in the specified collection", arg);
                    yield break;
                }

                if (add)
                    SendWorkshopQuery(0, arg, perm);
                else RemoveSkins(arg, perm);
            }
            else SendResponse($"[SkinBox] Collection response failed. Error code {code}", arg);
        }

        private void RemoveSkins(ConsoleSystem.Arg arg, string perm = null)
        {
            int removedCount = 0;
            for (int y = _skinList.Count - 1; y >= 0; y--)
            {
                KeyValuePair<string, HashSet<ulong>> skin = _skinList.ElementAt(y);

                for (int i = 0; i < _skinsToVerify.Count; i++)
                {
                    ulong skinId = _skinsToVerify[i];
                    if (skin.Value.Contains(skinId))
                    {
                        skin.Value.Remove(skinId);
                        Configuration.SkinList[skin.Key].Remove(skinId);
                        removedCount++;

                        if (!string.IsNullOrEmpty(perm))
                            Configuration.Permission.Custom[perm].Remove(skinId);
                    }
                }

            }

            if (removedCount > 0)
                SaveConfig();

            SendReply(arg, $"[SkinBox] - Removed {removedCount} skins");
        }

        private void SendResponse(string message, ConsoleSystem.Arg arg)
        {
            if (arg != null)
                SendReply(arg, message);
            else Debug.Log($"[PlayerSkins] - {message}");
        }
        #endregion

        #region Workshop Name Conversions
        private Dictionary<string, string> _workshopNameToShortname = new Dictionary<string, string>
        {
            {"longtshirt", "tshirt.long" },
            {"cap", "hat.cap" },
            {"beenie", "hat.beenie" },
            {"boonie", "hat.boonie" },
            {"balaclava", "mask.balaclava" },
            {"pipeshotgun", "shotgun.waterpipe" },
            {"woodstorage", "box.wooden" },
            {"ak47", "rifle.ak" },
            {"bearrug", "rug.bear" },
            {"boltrifle", "rifle.bolt" },
            {"bandana", "mask.bandana" },
            {"hideshirt", "attire.hide.vest" },
            {"snowjacket", "jacket.snow" },
            {"buckethat", "bucket.helmet" },
            {"semiautopistol", "pistol.semiauto" },            
            {"roadsignvest", "roadsign.jacket" },
            {"roadsignpants", "roadsign.kilt" },
            {"burlappants", "burlap.trousers" },
            {"collaredshirt", "shirt.collared" },
            {"mp5", "smg.mp5" },
            {"sword", "salvaged.sword" },
            {"workboots", "shoes.boots" },
            {"vagabondjacket", "jacket" },
            {"hideshoes", "attire.hide.boots" },
            {"deerskullmask", "deer.skull.mask" },
            {"minerhat", "hat.miner" },
            {"lr300", "rifle.lr300" },
            {"lr300.item", "rifle.lr300" },
            {"burlapgloves", "burlap.gloves" },
            {"burlap.gloves", "burlap.gloves"},
            {"leather.gloves", "burlap.gloves"},
            {"python", "pistol.python" },
            {"m39", "rifle.m39" },
            {"woodendoubledoor", "door.double.hinged.wood" }
        };

        private void UpdateWorkshopNameConversionList()
        {
            foreach (ItemDefinition item in ItemManager.itemList)
            {
                _shortnameToDisplayname[item.shortname] = item.displayName.english;

                string workshopName = item.displayName.english.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "");
                
                _workshopNameToShortname[workshopName] = item.shortname;
                _workshopNameToShortname[item.shortname] = item.shortname;
                _workshopNameToShortname[item.shortname.Replace(".", "")] = item.shortname;
            }

            foreach (Skinnable skin in Skinnable.All.ToList())
            {
                if (string.IsNullOrEmpty(skin.Name) || string.IsNullOrEmpty(skin.ItemName))
                    continue;

                _workshopNameToShortname[skin.Name.ToLower()] = skin.ItemName.ToLower();                
            }
        }
        #endregion

        #region Item Skin Redirects
        private Hash<string, string> _itemSkinRedirects = new Hash<string, string>();

        private void FindItemRedirects()
        {            
            bool addApprovedPermission = Configuration.Permission.Approved != Configuration.Permission.Use;

            foreach (ItemSkinDirectory.Skin skin in ItemSkinDirectory.Instance.skins)
            {
                ItemSkin itemSkin = skin.invItem as ItemSkin;
                if (itemSkin == null || itemSkin.Redirect == null)                
                    continue;

                _itemSkinRedirects[itemSkin.Redirect.shortname] = itemSkin.itemDefinition.shortname;

                if (Configuration.Skins.UseRedirected)
                {
                    HashSet<ulong> skins;
                    if (!_skinList.TryGetValue(itemSkin.itemDefinition.shortname, out skins))
                        skins = _skinList[itemSkin.itemDefinition.shortname] = new HashSet<ulong>();

                    skins.Add((ulong)skin.id);

                    if (addApprovedPermission)
                        _skinPermissions[(ulong)skin.id] = Configuration.Permission.Approved;
                }
            }
        }

        private string GetRedirectedShortname(string shortname)
        {
            string redirectedName;

            if (_itemSkinRedirects.TryGetValue(shortname, out redirectedName))
                return redirectedName;

            return shortname;
        }
        #endregion

        #region SkinBox Component
        private class LootHandler : MonoBehaviour
        {
            internal StorageContainer Entity { get; private set; }

            internal BasePlayer Looter { get; set; }


            internal bool HasItem { get; private set; }

            internal int InputAmount => inputItem?.amount ?? 0;


            private InputItem inputItem;

            private int _currentPage = 0;

            private int _maximumPages = 0;

            private int _itemsPerPage;

            private List<ulong> _availableSkins;


            private void Awake()
            {
                _availableSkins = Facepunch.Pool.GetList<ulong>();

                Entity = GetComponent<StorageContainer>();

                if (!Configuration.Other.AllowStacks)
                {
                    Entity.maxStackSize = 1;
                    Entity.inventory.maxStackSize = 1;
                }

                Entity.inventory.capacity = 1;

                Entity.SetFlag(BaseEntity.Flags.Open, true, false);
            }

            private void OnDestroy()
            {
                CuiHelper.DestroyUi(Looter, UI_PANEL);

                Instance?._activeSkinBoxes?.Remove(Looter.userID);

                if (HasItem && inputItem != null)
                    Looter.GiveItem(inputItem.Create(), BaseEntity.GiveItemReason.PickedUp);

                Facepunch.Pool.FreeList(ref _availableSkins);

                if (Entity != null && !Entity.IsDestroyed)
                {
                    if (Entity.inventory.itemList.Count > 0)
                        ClearContainer();

                    Entity.Kill(BaseNetworkable.DestroyMode.None);
                }
            }

            internal void OnItemAdded(Item item)
            {
                if (HasItem)
                    return;

                HasItem = true;

                string shortname = Instance.GetRedirectedShortname(item.info.shortname);

                Instance.GetSkinsFor(Looter, shortname, ref _availableSkins);

                _availableSkins.Remove(0UL);

                if (item.skin != 0UL)
                    _availableSkins.Remove(item.skin);

                inputItem = new InputItem(shortname, item);

                _itemsPerPage = inputItem.skin == 0UL ? 41 : 40;

                _currentPage = 0;
                _maximumPages = Mathf.Min(Configuration.Skins.MaximumPages, Mathf.CeilToInt((float)_availableSkins.Count / (float)_itemsPerPage));

                if (_currentPage > 0 || _maximumPages > 1)
                    CreateOverlay();

                RemoveItem(item);
                ClearContainer();

                StartCoroutine(FillContainer());
            }

            internal void OnItemRemoved(Item item)
            {
                if (!HasItem)
                    return;

                CuiHelper.DestroyUi(Looter, UI_PANEL);

                bool skinChanged = item.skin != 0UL && item.skin != inputItem.skin;

                inputItem.CloneTo(item);

                if (skinChanged && !Instance.ChargePlayer(Looter, inputItem.itemDefinition.category))
                {
                    item.skin = inputItem.skin;

                    if (item.GetHeldEntity() != null)
                        item.GetHeldEntity().skinID = inputItem.skin;

                    PopupMessage(string.Format(GetMessage("NotEnoughBalanceTake", Looter.userID), item.info.displayName.english, Instance.GetCostType(Looter.userID)));
                    //Instance.ChatMessage(Looter, $"NotEnoughBalanceTake{Instance._costType}", item.info.displayName.english);
                }

                item.MarkDirty();

                ClearContainer();

                Entity.inventory.capacity = 1;
                Entity.inventory.MarkDirty();

                inputItem.Dispose();
                inputItem = null;
                HasItem = false;

                if (Configuration.Cooldown.ActivateOnTake)
                    Instance.ApplyCooldown(Looter);

                if (Instance.IsOnCooldown(Looter))
                    Looter.EndLooting();
            }

            internal void ChangePage(int change)
            {
                _currentPage = Mathf.Clamp(_currentPage + change, 0, _maximumPages);

                StartCoroutine(RefillContainer());
            }

            private IEnumerator RefillContainer()
            {
                ClearContainer();

                yield return StartCoroutine(FillContainer());

                CreateOverlay();
            }

            private IEnumerator FillContainer()
            {
                Entity.inventory.capacity = Mathf.Min(_availableSkins.Count, (_currentPage + 1) * _itemsPerPage) - (_currentPage * _itemsPerPage) + (inputItem.skin == 0UL ? 1 : 2);

                CreateItem(0UL);

                if (inputItem.skin != 0UL)
                    CreateItem(inputItem.skin);

                for (int i = _currentPage * _itemsPerPage; i < Mathf.Min(_availableSkins.Count, (_currentPage + 1) * _itemsPerPage); i++)
                {
                    CreateItem(_availableSkins[i]);

                    if (i % 2 == 0)
                        yield return null;
                }
            }

            private void ClearContainer()
            {
                for (int i = Entity.inventory.itemList.Count - 1; i >= 0; i--)
                    RemoveItem(Entity.inventory.itemList[i]);
            }

            private Item CreateItem(ulong skinId)
            {
                Item item = ItemManager.Create(inputItem.itemDefinition, 1, skinId);
                item.contents?.SetFlag(ItemContainer.Flag.IsLocked, true);
                item.contents?.SetFlag(ItemContainer.Flag.NoItemInput, true);

                if (skinId != 0UL)
                    Instance._skinNameLookup.TryGetValue(skinId, out item.name);

                if (!InsertItem(item))
                    item.Remove(0f);
                else item.MarkDirty();

                return item;
            }

            private bool InsertItem(Item item)
            {
                if (Entity.inventory.itemList.Contains(item))
                    return false;

                if (Entity.inventory.IsFull())
                    return false;

                Entity.inventory.itemList.Add(item);
                item.parent = Entity.inventory;

                if (!Entity.inventory.FindPosition(item))
                    return false;

                Entity.inventory.MarkDirty();
                Entity.inventory.onItemAddedRemoved?.Invoke(item, true);

                return true;
            }

            private void RemoveItem(Item item)
            {
                if (!Entity.inventory.itemList.Contains(item))
                    return;

                Entity.inventory.onPreItemRemove?.Invoke(item);

                Entity.inventory.itemList.Remove(item);
                item.parent = null;

                Entity.inventory.MarkDirty();

                Entity.inventory.onItemAddedRemoved?.Invoke(item, false);

                item.Remove(0f);
            }

            internal void CheckItemHasSplit(Item item) => StartCoroutine(CheckItemHasSplit(item, item.amount)); // Item split dupe solution?

            private IEnumerator CheckItemHasSplit(Item item, int originalAmount)
            {
                yield return null;

                if (item != null && item.amount != originalAmount)
                {
                    int splitAmount = originalAmount - item.amount;
                    Looter.inventory.Take(null, item.info.itemid, splitAmount);
                    item.amount += splitAmount;
                }
            }

            private class InputItem
            {
                public ItemDefinition itemDefinition;
                public int amount;
                public ulong skin;

                public float condition;
                public float maxCondition;

                public int magazineContents;
                public int magazineCapacity;
                public ItemDefinition ammoType;

                public List<InputItem> contents;

                internal InputItem(string shortname, Item item)
                {
                    if (!item.info.shortname.Equals(shortname))
                        itemDefinition = ItemManager.FindItemDefinition(shortname);
                    else itemDefinition = item.info;

                    amount = item.amount;
                    skin = item.skin;

                    if (item.hasCondition)
                    {
                        condition = item.condition;
                        maxCondition = item.maxCondition;
                    }

                    BaseProjectile baseProjectile = item.GetHeldEntity() as BaseProjectile;
                    if (baseProjectile != null)
                    {
                        magazineContents = baseProjectile.primaryMagazine.contents;
                        magazineCapacity = baseProjectile.primaryMagazine.capacity;
                        ammoType = baseProjectile.primaryMagazine.ammoType;
                    }

                    if (item.contents?.itemList?.Count > 0)
                    {
                        contents = Facepunch.Pool.GetList<InputItem>();

                        for (int i = 0; i < item.contents.itemList.Count; i++)
                        {
                            Item content = item.contents.itemList[i];
                            contents.Add(new InputItem(content.info.shortname, content));
                        }
                    }
                }

                internal void Dispose()
                {
                    if (contents != null)
                        Facepunch.Pool.FreeList(ref contents);
                }

                internal Item Create()
                {
                    Item item = ItemManager.Create(itemDefinition, amount, skin);

                    if (item.hasCondition)
                    {
                        item.condition = condition;
                        item.maxCondition = maxCondition;
                    }

                    BaseProjectile baseProjectile = item.GetHeldEntity() as BaseProjectile;
                    if (baseProjectile != null)
                    {
                        baseProjectile.primaryMagazine.contents = magazineContents;
                        baseProjectile.primaryMagazine.capacity = magazineCapacity;
                        baseProjectile.primaryMagazine.ammoType = ammoType;
                    }

                    if (contents?.Count > 0)
                    {
                        for (int i = 0; i < contents.Count; i++)
                        {
                            InputItem content = contents[i];

                            Item attachment = ItemManager.Create(content.itemDefinition, content.amount, content.skin);
                            attachment.MoveToContainer(item.contents);
                        }
                    }

                    item.MarkDirty();
                    return item;
                }

                internal void CloneTo(Item item)
                {
                    item.contents?.SetFlag(ItemContainer.Flag.IsLocked, false);
                    item.contents?.SetFlag(ItemContainer.Flag.NoItemInput, false);

                    item.amount = amount;

                    if (item.hasCondition)
                    {
                        item.condition = condition;
                        item.maxCondition = maxCondition;
                    }

                    BaseProjectile baseProjectile = item.GetHeldEntity() as BaseProjectile;
                    if (baseProjectile != null && baseProjectile.primaryMagazine != null)
                    {
                        baseProjectile.primaryMagazine.contents = magazineContents;
                        baseProjectile.primaryMagazine.capacity = magazineCapacity;
                        baseProjectile.primaryMagazine.ammoType = ammoType;
                    }

                    if (contents?.Count > 0)
                    {
                        for (int i = 0; i < contents.Count; i++)
                        {
                            InputItem content = contents[i];

                            Item attachment = ItemManager.Create(content.itemDefinition, content.amount, content.skin);
                            attachment.MoveToContainer(item.contents);
                        }
                    }

                    item.MarkDirty();
                }
            }

            #region UI
            private const string UI_PANEL = "SkinBox_UI";
            private const string UI_POPUP = "SkinBox_Popup";

            private const string PAGE_COLOR = "0.65 0.65 0.65 0.06";
            private const string PAGE_TEXT_COLOR = "0.7 0.7 0.7 1.0";
            private const string BUTTON_COLOR = "0.75 0.75 0.75 0.1";
            private const string BUTTON_TEXT_COLOR = "0.77 0.68 0.68 1";

            private readonly UI4 Popup = new UI4(0.65f, 0.8f, 0.99f, 0.99f);
            private readonly UI4 Container = new UI4(0.9505f, 0.15f, 0.99f, 0.6f);
            private readonly UI4 BackButton = new UI4(0f, 0.7f, 1f, 1f);
            private readonly UI4 Text = new UI4(0f, 0.3f, 1f, 0.7f);
            private readonly UI4 NextButton = new UI4(0f, 0f, 1f, 0.3f);

            private void CreateOverlay()
            {
                CuiElementContainer container = UI.Container(UI_PANEL, Container);

                UI.Button(container, UI_PANEL, BUTTON_COLOR, "◀", BUTTON_TEXT_COLOR, 50, BackButton, _currentPage > 0 ? "skinbox.pageprev" : "");

                UI.Panel(container, UI_PANEL, PAGE_COLOR, Text);
                UI.Label(container, UI_PANEL, $"{_currentPage + 1}\nof\n{_maximumPages}", PAGE_TEXT_COLOR, 20, Text);

                UI.Button(container, UI_PANEL, BUTTON_COLOR, "▶", BUTTON_TEXT_COLOR, 50, NextButton, (_currentPage + 1) < _maximumPages ? "skinbox.pagenext" : "");

                CuiHelper.DestroyUi(Looter, UI_PANEL);
                CuiHelper.AddUi(Looter, container);
            }

            internal void PopupMessage(string message)
            {
                CuiElementContainer container = UI.Container(UI_POPUP, Popup);
             
                UI.Label(container, UI_POPUP, message, BUTTON_TEXT_COLOR, 15, UI4.Full, TextAnchor.UpperRight);

                CuiHelper.DestroyUi(Looter, UI_POPUP);
                CuiHelper.AddUi(Looter, container);

                Looter.Invoke(() => CuiHelper.DestroyUi(Looter, UI_POPUP), 5f);
            }
            #endregion
        }
        #endregion

        #region UI
        public static class UI
        {
            public static CuiElementContainer Container(string panel, UI4 dimensions, string parent = "Hud.Menu")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() } },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
                return container;
            }

            public static void Panel(CuiElementContainer container, string panel, string color, UI4 dimensions)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                },
                panel);
            }

            public static void Label(CuiElementContainer container, string panel, string text, string color, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Color = color, Align = align, Text = text },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);

            }

            public static void Button(CuiElementContainer container, string panel, string color, string text, string textColor, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    Text = { Text = text, Color = textColor, FontSize = size, Align = align }
                },
                panel);
            }            
        }
        public class UI4
        {
            public float xMin, yMin, xMax, yMax;

            public static UI4 Full { get; private set; } = new UI4(0f, 0f, 1f, 1f);

            public UI4(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }
            public string GetMin() => $"{xMin} {yMin}";
            public string GetMax() => $"{xMax} {yMax}";
        }
        #endregion

        #region UI Command
        [ConsoleCommand("skinbox.pagenext")]
        private void cmdPageNext(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Connection == null)
                return;

            BasePlayer player = arg.Connection?.player as BasePlayer;
            if (player == null)
                return;

            LootHandler lootHandler;
            if (_activeSkinBoxes.TryGetValue(player.userID, out lootHandler))
                lootHandler.ChangePage(1);            
        }

        [ConsoleCommand("skinbox.pageprev")]
        private void cmdPagePrev(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Connection == null)
                return;

            BasePlayer player = arg.Connection?.player as BasePlayer;
            if (player == null)
                return;

            LootHandler lootHandler;
            if (_activeSkinBoxes.TryGetValue(player.userID, out lootHandler))
                lootHandler.ChangePage(-1);
        }
        #endregion

        #region Chat Commands        
        private void cmdSkinBox(BasePlayer player, string command, string[] args)
        {            
            if (_apiKeyMissing)
            {
                SendAPIMissingWarning();
                ChatMessage(player, "NoAPIKey");
                return;
            }

            if (!_skinsLoaded)
            {
                ChatMessage(player, "SkinsLoading");
                return;
            }

            if (player.inventory.loot.IsLooting())
                return;

            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, Configuration.Permission.Use))
            {
                ChatMessage(player, "NoPermission");
                return;
            }

            double cooldownRemaining;
            if (IsOnCooldown(player, out cooldownRemaining))
            {
                ChatMessage(player, "CooldownTime", Mathf.RoundToInt((float)cooldownRemaining));
                return;
            }

            if (!ChargePlayer(player, Configuration.Cost.Open))
            {
                ChatMessage(player, "NotEnoughBalanceOpen", Configuration.Cost.Open, GetCostType(player.userID));
                return;
            }

            timer.In(0.2f, () => CreateSkinBox(player));
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("skinbox.cmds")]
        private void cmdListCmds(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
                return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n> SkinBox command overview <");

            TextTable textTable = new TextTable();
            textTable.AddColumn("Command");
            textTable.AddColumn("Description");
            textTable.AddRow(new string[] { "skinbox.addskin", "Add one or more skin-id's to the workshop skin list" });
            textTable.AddRow(new string[] { "skinbox.removeskin", "Remove one or more skin-id's from the workshop skin list" });
            textTable.AddRow(new string[] { "skinbox.addvipskin", "Add one or more skin-id's to the workshop skin list for the specified permission" });
            textTable.AddRow(new string[] { "skinbox.removevipskin", "Remove one or more skin-id's from the workshop skin list for the specified permission" });
            textTable.AddRow(new string[] { "skinbox.addexcluded", "Add one or more skin-id's to the exclusion list (for players)" });
            textTable.AddRow(new string[] { "skinbox.removeexcluded", "Remove one or more skin-id's from the exclusion list" });
            textTable.AddRow(new string[] { "skinbox.addcollection", "Adds a whole skin-collection to the workshop skin list"});
            textTable.AddRow(new string[] { "skinbox.removecollection", "Removes a whole collection from the workshop skin list" });
            textTable.AddRow(new string[] { "skinbox.addvipcollection", "Adds a whole skin-collection to the workshop skin list for the specified permission" });
            textTable.AddRow(new string[] { "skinbox.removevipcollection", "Removes a whole collection from the workshop skin list for the specified permission" });

            sb.AppendLine(textTable.ToString());
            SendReply(arg, sb.ToString());
        }

        #region Add/Remove Skins
        [ConsoleCommand("skinbox.addskin")]
        private void consoleAddSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in one or more workshop skin ID's");
                return;
            }

            _skinsToVerify.Clear();

            for (int i = 0; i < arg.Args.Length; i++)
            {
                ulong fileId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out fileId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as it's not a number");
                    continue;
                }
                else
                {
                    if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
                    {
                        SendReply(arg, $"Ignored '{arg.Args[i]}' as it is not the correct length (9 - 10 digits)");
                        continue;
                    }

                    _skinsToVerify.Add(fileId);
                }
            }

            if (_skinsToVerify.Count > 0)
                SendWorkshopQuery(0, arg);
            else SendReply(arg, "No valid skin ID's were entered");
        }

        [ConsoleCommand("skinbox.removeskin")]
        private void consoleRemoveSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in one or more workshop skin ID's");
                return;
            }

            _skinsToVerify.Clear();

            for (int i = 0; i < arg.Args.Length; i++)
            {
                ulong fileId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out fileId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as it's not a number");
                    continue;
                }
                else
                {
                    if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
                    {
                        SendReply(arg, $"Ignored '{arg.Args[i]}' as it is not the correct length (9 - 10 digits)");
                        continue;
                    }

                    _skinsToVerify.Add(fileId);
                }
            }

            if (_skinsToVerify.Count > 0)
                RemoveSkins(arg);
            else SendReply(arg, "No valid skin ID's were entered");
        }

        [ConsoleCommand("skinbox.addvipskin")]
        private void consoleAddVIPSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "You need to type in a permission and one or more workshop skin ID's");
                return;
            }

            string perm = arg.Args[0];
            if (!Configuration.Permission.Custom.ContainsKey(perm))
            {
                SendReply(arg, $"The permission {perm} does not exist in the custom permission section of the config");
                return;
            }

            _skinsToVerify.Clear();
            
            for (int i = 1; i < arg.Args.Length; i++)
            {
                ulong fileId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out fileId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as it's not a number");
                    continue;
                }
                else
                {
                    if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
                    {
                        SendReply(arg, $"Ignored '{arg.Args[i]}' as it is not the correct length (9 - 10 digits)");
                        continue;
                    }

                    _skinsToVerify.Add(fileId);
                }
            }

            if (_skinsToVerify.Count > 0)
                SendWorkshopQuery(0, arg, perm);
            else SendReply(arg, "No valid skin ID's were entered");
        }

        [ConsoleCommand("skinbox.removevipskin")]
        private void consoleRemoveVIPSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "You need to type in a permission and one or more workshop skin ID's");
                return;
            }

            string perm = arg.Args[0];
            if (!Configuration.Permission.Custom.ContainsKey(perm))
            {
                SendReply(arg, $"The permission {perm} does not exist in the custom permission section of the config");
                return;
            }

            _skinsToVerify.Clear();

            for (int i = 1; i < arg.Args.Length; i++)
            {
                ulong fileId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out fileId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as it's not a number");
                    continue;
                }
                else
                {
                    if (arg.Args[i].Length < 9 || arg.Args[i].Length > 10)
                    {
                        SendReply(arg, $"Ignored '{arg.Args[i]}' as it is not the correct length (9 - 10 digits)");
                        continue;
                    }

                    _skinsToVerify.Add(fileId);
                }
            }

            if (_skinsToVerify.Count > 0)
                RemoveSkins(arg, perm);
            else SendReply(arg, "No valid skin ID's were entered");
        }
        #endregion

        #region Add/Remove Collections
        [ConsoleCommand("skinbox.addcollection")]
        private void consoleAddCollection(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in a skin collection ID");
                return;
            }

            ulong collectionId = 0uL;
            if (!ulong.TryParse(arg.Args[0], out collectionId))
            {
                SendReply(arg, $"{arg.Args[0]} is an invalid collection ID");
                return;
            }

            SendWorkshopCollectionQuery(collectionId, true, arg);
        }

        [ConsoleCommand("skinbox.removecollection")]
        private void consoleRemoveCollection(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in a skin collection ID");
                return;
            }

            ulong collectionId = 0uL;
            if (!ulong.TryParse(arg.Args[0], out collectionId))
            {
                SendReply(arg, $"{arg.Args[0]} is an invalid collection ID");
                return;
            }

            SendWorkshopCollectionQuery(collectionId, false, arg);
        }

        [ConsoleCommand("skinbox.addvipcollection")]
        private void consoleAddVIPCollection(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "You need to type in a permission and one or more workshop skin ID's");
                return;
            }

            string perm = arg.Args[0];
            if (!Configuration.Permission.Custom.ContainsKey(perm))
            {
                SendReply(arg, $"The permission {perm} does not exist in the custom permission section of the config");
                return;
            }

            ulong collectionId = 0uL;
            if (!ulong.TryParse(arg.Args[1], out collectionId))
            {
                SendReply(arg, $"{arg.Args[1]} is an invalid collection ID");
                return;
            }

            SendWorkshopCollectionQuery(collectionId, true, arg, perm);
        }

        [ConsoleCommand("skinbox.removevipcollection")]
        private void consoleRemoveVIPCollection(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, "You need to type in a permission and one or more workshop skin ID's");
                return;
            }

            string perm = arg.Args[0];
            if (!Configuration.Permission.Custom.ContainsKey(perm))
            {
                SendReply(arg, $"The permission {perm} does not exist in the custom permission section of the config");
                return;
            }

            ulong collectionId = 0uL;
            if (!ulong.TryParse(arg.Args[1], out collectionId))
            {
                SendReply(arg, $"{arg.Args[1]} is an invalid collection ID");
                return;
            }

            SendWorkshopCollectionQuery(collectionId, false, arg, perm);
        }
        #endregion

        #region Blacklisted Skins
        [ConsoleCommand("skinbox.addexcluded")]
        private void consoleAddExcluded(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in one or more skin ID's");
                return;
            }

            int count = 0;

            for (int i = 0; i < arg.Args.Length; i++)
            {
                ulong skinId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out skinId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as it's not a number");
                    continue;
                }
                else
                {
                    Configuration.Blacklist.Add(skinId);
                    count++;
                }
            }

            if (count > 0)
            {
                SaveConfig();
                SendReply(arg, $"Blacklisted {count} skin ID's");
            }
            else SendReply(arg, "No skin ID's were added to the blacklist");
        }

        [ConsoleCommand("skinbox.removeexcluded")]
        private void consoleRemoveExcluded(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && !permission.UserHasPermission(arg.Connection.userid.ToString(), Configuration.Permission.Admin))
            {
                SendReply(arg, "You do not have permission to use this command");
                return;
            }

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 1)
            {
                SendReply(arg, "You need to type in one or more skin ID's");
                return;
            }

            int count = 0;

            for (int i = 0; i < arg.Args.Length; i++)
            {
                ulong skinId = 0uL;
                if (!ulong.TryParse(arg.Args[i], out skinId))
                {
                    SendReply(arg, $"Ignored '{arg.Args[i]}' as it's not a number");
                    continue;
                }
                else
                {
                    if (Configuration.Blacklist.Contains(skinId))
                    {
                        Configuration.Blacklist.Remove(skinId);
                        SendReply(arg, $"The skin ID {skinId} is not on the blacklist");
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                SaveConfig();
                SendReply(arg, $"Removed {count} skin ID's from the blacklist");
            }
            else SendReply(arg, "No skin ID's were removed from the blacklist");
        }
        #endregion

        [ConsoleCommand("skinbox.open")]
        private void consoleSkinboxOpen(ConsoleSystem.Arg arg)
        {
            if (arg == null)
                return;

            if (!_skinsLoaded)
            {
                SendReply(arg, "SkinBox is still loading skins. Please wait");
                return;
            }

            if (arg.Connection == null)
            {
                if (arg.Args == null || arg.Args.Length == 0)
                {
                    SendReply(arg, "This command requires a Steam ID of the target user");
                    return;
                }

                ulong targetUserID = 0uL;
                if (!ulong.TryParse(arg.Args[0], out targetUserID) || !Oxide.Core.ExtensionMethods.IsSteamId(targetUserID))
                {
                    SendReply(arg, "Invalid Steam ID entered");
                    return;
                }

                BasePlayer targetPlayer = BasePlayer.FindByID(targetUserID);
                if (targetPlayer == null || !targetPlayer.IsConnected)
                {
                    SendReply(arg, $"Unable to find a player with the specified Steam ID");
                    return;
                }

                if (targetPlayer.IsDead())
                {
                    SendReply(arg, $"The specified player is currently dead");
                    return;
                }

                if (!targetPlayer.inventory.loot.IsLooting())
                    CreateSkinBox(targetPlayer);
            }
            else if (arg.Connection != null && arg.Connection.player != null)
            {
                BasePlayer player = arg.Player();

                cmdSkinBox(player, string.Empty, Array.Empty<string>());
            }
        }
        #endregion

        #region API
        private bool IsSkinBoxPlayer(ulong playerId) => _activeSkinBoxes.ContainsKey(playerId);
        #endregion

        #region Config        
        private static ConfigData Configuration;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Skin Options")]
            public SkinOptions Skins { get; set; }

            [JsonProperty(PropertyName = "Cooldown Options")]
            public CooldownOptions Cooldown { get; set; }

            [JsonProperty(PropertyName = "Command Options")]
            public CommandOptions Command { get; set; }

            [JsonProperty(PropertyName = "Permission Options")]
            public PermissionOptions Permission { get; set; }

            [JsonProperty(PropertyName = "Usage Cost Options")]
            public CostOptions Cost { get; set; }

            [JsonProperty(PropertyName = "Other Options")]
            public OtherOptions Other { get; set; }

            [JsonProperty(PropertyName = "Imported Workshop Skins")]
            public Hash<string, HashSet<ulong>> SkinList { get; set; }

            [JsonProperty(PropertyName = "Blacklisted Skin ID's")]
            public HashSet<ulong> Blacklist { get; set; }

            public class SkinOptions
            {
                [JsonProperty(PropertyName = "Maximum number of approved skins allowed for each item (-1 is unlimited)")]
                public int ApprovedLimit { get; set; }

                [JsonProperty(PropertyName = "Maximum number of pages viewable")]
                public int MaximumPages { get; set; }

                [JsonProperty(PropertyName = "Include approved skins")]
                public bool UseApproved { get; set; }

                [JsonProperty(PropertyName = "Include manually imported workshop skins")]
                public bool UseWorkshop { get; set; }

                [JsonProperty(PropertyName = "Remove approved skin ID's from config workshop skin list")]
                public bool RemoveApproved { get; set; }

                [JsonProperty(PropertyName = "Include redirected skins")]
                public bool UseRedirected { get; set; }

                [JsonProperty(PropertyName = "Steam API key for workshop skins (https://steamcommunity.com/dev/apikey)")]
                public string APIKey { get; set; }
            }

            public class CooldownOptions
            {
                [JsonProperty(PropertyName = "Enable cooldowns")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Cooldown time start's when a item is removed from the box")]
                public bool ActivateOnTake { get; set; }

                [JsonProperty(PropertyName = "Length of cooldown time (seconds)")]
                public int Time { get; set; }
            }

            public class PermissionOptions
            {
                [JsonProperty(PropertyName = "Permission required to use SkinBox")]
                public string Use { get; set; }

                [JsonProperty(PropertyName = "Permission required to use admin functions")]
                public string Admin { get; set; }

                [JsonProperty(PropertyName = "Permission that bypasses usage costs")]
                public string NoCost { get; set; }

                [JsonProperty(PropertyName = "Permission that bypasses usage cooldown")]
                public string NoCooldown { get; set; }

                [JsonProperty(PropertyName = "Permission required to skin weapons")]
                public string Weapon { get; set; }

                [JsonProperty(PropertyName = "Permission required to skin deployables")]
                public string Deployable { get; set; }

                [JsonProperty(PropertyName = "Permission required to skin attire")]
                public string Attire { get; set; }

                [JsonProperty(PropertyName = "Permission required to view approved skins")]
                public string Approved { get; set; }

                [JsonProperty(PropertyName = "Custom permissions per skin")]
                public Hash<string, List<ulong>> Custom { get; set; }

                public void RegisterPermissions(Permission permission, Plugin plugin)
                {
                    permission.RegisterPermission(Use, plugin);
                    permission.RegisterPermission(Admin, plugin);
                    permission.RegisterPermission(NoCost, plugin);
                    permission.RegisterPermission(NoCooldown, plugin);

                    if (!permission.PermissionExists(Weapon, plugin))
                        permission.RegisterPermission(Weapon, plugin);

                    if (!permission.PermissionExists(Deployable, plugin))
                        permission.RegisterPermission(Deployable, plugin);

                    if (!permission.PermissionExists(Attire, plugin))
                        permission.RegisterPermission(Attire, plugin);

                    if (!permission.PermissionExists(Approved, plugin))
                        permission.RegisterPermission(Approved, plugin);

                    foreach (string perm in Custom.Keys)
                    {
                        if (!permission.PermissionExists(perm, plugin))
                            permission.RegisterPermission(perm, plugin);
                    }
                }

                public void ReverseCustomSkinPermissions(ref Hash<ulong, string> list)
                {
                    foreach (KeyValuePair<string, List<ulong>> kvp in Custom)
                    {
                        for (int i = 0; i < kvp.Value.Count; i++)
                        {
                            list[kvp.Value[i]] = kvp.Key;
                        }
                    }
                }
            }

            public class CommandOptions
            {
                [JsonProperty(PropertyName = "Commands to open the SkinBox")]
                public string[] Commands { get; set; }

                internal void RegisterCommands(Game.Rust.Libraries.Command cmd, Plugin plugin)
                {
                    for (int i = 0; i < Commands.Length; i++)                    
                        cmd.AddChatCommand(Commands[i], plugin, "cmdSkinBox");                    
                }
            }

            public class CostOptions
            {
                [JsonProperty(PropertyName = "Enable usage costs")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Currency used for usage costs (Scrap, Economics, ServerRewards)")]
                public string Currency { get; set; }

                [JsonProperty(PropertyName = "Cost to open the SkinBox")]
                public int Open { get; set; }

                [JsonProperty(PropertyName = "Cost to skin deployables")]
                public int Deployable { get; set; }

                [JsonProperty(PropertyName = "Cost to skin attire")]
                public int Attire { get; set; }

                [JsonProperty(PropertyName = "Cost to skin weapons")]
                public int Weapon { get; set; }
            }  
            
            public class OtherOptions
            {
                [JsonProperty(PropertyName = "Allow stacked items")]
                public bool AllowStacks { get; set; }

                [JsonProperty(PropertyName = "Auth-level required to view blacklisted skins")]
                public int BlacklistAuth { get; set; }
            }
                        
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Configuration = Config.ReadObject<ConfigData>();

            if (Configuration.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(Configuration, true);
        }

        protected override void LoadDefaultConfig() => Configuration = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Skins = new ConfigData.SkinOptions
                {
                    APIKey = string.Empty,
                    ApprovedLimit = -1,
                    MaximumPages = 3,
                    UseApproved = true,
                    RemoveApproved = false,
                    UseRedirected = true,
                    UseWorkshop = true
                },
                Command = new ConfigData.CommandOptions
                {
                    Commands = new string[] { "skinbox", "sb" }
                },
                Permission = new ConfigData.PermissionOptions
                {
                    Admin = "skinbox.admin",
                    NoCost = "skinbox.ignorecost",
                    NoCooldown = "skinbox.ignorecooldown",
                    Use = "skinbox.use",
                    Approved = "skinbox.use",
                    Attire = "skinbox.use",
                    Deployable = "skinbox.use",
                    Weapon = "skinbox.use",
                    Custom = new Hash<string, List<ulong>>
                    {
                        ["skinbox.example1"] = new List<ulong>() { 9990, 9991, 9992 },
                        ["skinbox.example2"] = new List<ulong>() { 9993, 9994, 9995 },
                        ["skinbox.example3"] = new List<ulong>() { 9996, 9997, 9998 }
                    }
                },
                Cooldown = new ConfigData.CooldownOptions
                {
                    Enabled = false,
                    ActivateOnTake = true,
                    Time = 60
                },
                Cost = new ConfigData.CostOptions
                {
                    Enabled = false,
                    Currency = "Scrap",
                    Open = 5,
                    Weapon = 30,
                    Attire = 20,
                    Deployable = 10
                },
                Other = new ConfigData.OtherOptions
                {
                    AllowStacks = false,
                    BlacklistAuth = 2,
                },
                SkinList = new Hash<string, HashSet<ulong>>(),
                Blacklist = new HashSet<ulong>(),
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(Configuration, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (Configuration.Version < new VersionNumber(2, 0, 0))
                Configuration = baseConfig;

            Configuration.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region JSON Deserialization        
        public class QueryResponse
        {
            public Response response;
        }

        public class Response
        {
            public int total;
            public PublishedFileDetails[] publishedfiledetails;
        }

        public class PublishedFileDetails
        {
            public int result;
            public string publishedfileid;
            public string creator;
            public int creator_appid;
            public int consumer_appid;
            public int consumer_shortcutid;
            public string filename;
            public string file_size;
            public string preview_file_size;
            public string file_url;
            public string preview_url;
            public string url;
            public string hcontent_file;
            public string hcontent_preview;
            public string title;
            public string file_description;
            public int time_created;
            public int time_updated;
            public int visibility;
            public int flags;
            public bool workshop_file;
            public bool workshop_accepted;
            public bool show_subscribe_all;
            public int num_comments_public;
            public bool banned;
            public string ban_reason;
            public string banner;
            public bool can_be_deleted;
            public string app_name;
            public int file_type;
            public bool can_subscribe;
            public int subscriptions;
            public int favorited;
            public int followers;
            public int lifetime_subscriptions;
            public int lifetime_favorited;
            public int lifetime_followers;
            public string lifetime_playtime;
            public string lifetime_playtime_sessions;
            public int views;
            public int num_children;
            public int num_reports;
            public Preview[] previews;
            public Tag[] tags;
            public int language;
            public bool maybe_inappropriate_sex;
            public bool maybe_inappropriate_violence;

            public class Tag
            {
                public string tag;
                public bool adminonly;
            }

        }

        public class PublishedFileQueryResponse
        {
            public FileResponse response { get; set; }
        }

        public class FileResponse
        {
            public int result { get; set; }
            public int resultcount { get; set; }
            public PublishedFileQueryDetail[] publishedfiledetails { get; set; }
        }

        public class PublishedFileQueryDetail
        {
            public string publishedfileid { get; set; }
            public int result { get; set; }
            public string creator { get; set; }
            public int creator_app_id { get; set; }
            public int consumer_app_id { get; set; }
            public string filename { get; set; }
            public int file_size { get; set; }
            public string preview_url { get; set; }
            public string hcontent_preview { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public int time_created { get; set; }
            public int time_updated { get; set; }
            public int visibility { get; set; }
            public int banned { get; set; }
            public string ban_reason { get; set; }
            public int subscriptions { get; set; }
            public int favorited { get; set; }
            public int lifetime_subscriptions { get; set; }
            public int lifetime_favorited { get; set; }
            public int views { get; set; }
            public Tag[] tags { get; set; }

            public class Tag
            {
                public string tag { get; set; }
            }
        }

        public class Preview
        {
            public string previewid;
            public int sortorder;
            public string url;
            public int size;
            public string filename;
            public int preview_type;
            public string youtubevideoid;
            public string external_reference;
        }


        public class CollectionQueryResponse
        {
            public CollectionResponse response { get; set; }
        }

        public class CollectionResponse
        {
            public int result { get; set; }
            public int resultcount { get; set; }
            public CollectionDetails[] collectiondetails { get; set; }
        }

        public class CollectionDetails
        {
            public string publishedfileid { get; set; }
            public int result { get; set; }
            public CollectionChild[] children { get; set; }
        }

        public class CollectionChild
        {
            public string publishedfileid { get; set; }
            public int sortorder { get; set; }
            public int filetype { get; set; }
        }

        #endregion
    }
}
