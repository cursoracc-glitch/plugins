using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ComponentBox", "TopPlugin.ru", "2.0.2"), Description("Allows players to store components in a secondary container carried in their inventory")]
    class ComponentBox : RustPlugin
    {
        #region Fields
        private const int PRESENT_ITEM_ID = -1622660759;
        private const ulong PRESENT_SKIN_ID = 1526403462;

        private const string PERMISSION_USE = "componentbox.use";
        #endregion

        #region Oxide Hooks     
        protected override void LoadDefaultMessages() => lang.RegisterMessages(Messages, this);

        private void OnServerInitialized()
        {
            permission.RegisterPermission(PERMISSION_USE, this);

            foreach (ConfigData.CraftingSettings.CraftItem craftItem in Configuration.Crafting.Cost)
                craftItem.Validate();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                return;

            if (!Configuration.Crafting.RequiresCrafting)
            {
                IEnumerable<Item> items = FindComponentBoxes(player);
                if (items.Count() > 0)
                {
                    SetupComponentBoxes(items);
                    return;
                }

                GiveComponentBox(player);
            }
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            timer.In(1f, () =>
            {
                if (player == null)
                    return;

                if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE) || Configuration.Crafting.RequiresCrafting)
                    return;

                IEnumerable<Item> items = FindComponentBoxes(player);
                if (items.Count() > 0)
                {
                    SetupComponentBoxes(items);
                    return;
                }

                if (!Configuration.Crafting.RequiresCrafting)
                    timer.In(1f, () => GiveComponentBox(player));
            });            
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null)            
                return;

            if (!Configuration.LeaveOnCorpse)
                DropComponentBoxItems(player);
        }
        #endregion

        #region Item Management
        private object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item == null || player == null)      
                return null;

            if (item.skin == PRESENT_SKIN_ID)
            {
                const string UNWRAP = "unwrap";
                const string DROP = "drop";

                if (action.Equals(UNWRAP, StringComparison.OrdinalIgnoreCase))
                {
                    if (player.inventory.loot.itemSource == item)
                        return false;

                    BeginLootingComponentBox(player, item, true);

                    Interface.CallHook("OnLootItem", player, item);
                    player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic");
                    return false;
                }
                else if (action.Equals(DROP, StringComparison.OrdinalIgnoreCase))
                {
                    if (!Configuration.IsDroppable)
                    {
                        Message(player, "Error.CantDrop");
                        return false;
                    }
                }
            }

            return null;
        }
       
        private object OnItemPickup(Item item, BasePlayer player)
        {
            if (item == null || player == null)
                return null;

            if (item.skin == PRESENT_SKIN_ID)
            {
                if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                {
                    Message(player, "Error.NoCarryPermission");
                    BeginLootingComponentBox(player, item, false);
                    return false;
                }

                if (Configuration.LimitCarryAmount)
                {
                    if (FindComponentBoxes(player).Count() > 0)
                    {
                        Message(player, "Error.HaveToolbox");
                        BeginLootingComponentBox(player, item, false);
                        return false;
                    }
                }
            }

            if (Configuration.DepositAutomatically && Configuration.Items.CanAcceptItem(item))
            {
                foreach (Item componentBox in FindComponentBoxes(player))
                {
                    int amountMoved = item.amount;

                    if (!item.CanMoveTo(componentBox.contents))
                        continue;

                    if (!componentBox.contents.CanAccept(item))
                        continue;

                    WorldItem worldItem = item.GetWorldEntity() as WorldItem;
                    worldItem.RemoveItem();
                    worldItem.ClientRPC(null, "PickupSound");

                    if (!item.MoveToContainer(componentBox.contents))
                    {
                        item.Drop(componentBox.contents.dropPosition, componentBox.contents.dropVelocity, default(Quaternion));
                        continue;
                    }
                    
                    player.Command("note.inv", item.info.itemid, amountMoved, string.IsNullOrEmpty(item.name) ? null : item.name, BasePlayer.GiveItemReason.PickedUp);
                    player.SignalBroadcast(BaseEntity.Signal.Gesture, "pickup_item");
                    return false;
                }
            }
            return null;
        }       

        private object CanAcceptItem(ItemContainer container, Item componentBox, int target)
        {
            if (container == null || componentBox == null)           
                return null;
            
            if (componentBox != null && componentBox.skin == PRESENT_SKIN_ID)
            {
                if (componentBox.contents != null && componentBox.contents == container)
                    return ItemContainer.CanAcceptResult.CannotAccept;

                if (componentBox.parent != null)
                {
                    if (componentBox.parent.playerOwner == container.playerOwner)
                        return null;

                    if (componentBox.parent.playerOwner != null && componentBox.parent.playerOwner.IsDead() && container.entityOwner is BaseCorpse)
                        return null;
                }

                if (Configuration.LimitCarryAmount && container.playerOwner != null)
                {
                    BasePlayer player = container.playerOwner;
                    IEnumerable<Item> componentBoxes = FindComponentBoxes(player);

                    if (componentBoxes.Count() > 0)
                    {
                        MoveContentsToPlayer(componentBox, container.playerOwner, componentBoxes);
                        Message(container.playerOwner, "Notification.ContentsTaken");
                        return ItemContainer.CanAcceptResult.CannotAccept;
                    }
                }

                if (container.entityOwner != null)
                {
                    if (!Configuration.IsStorable || (componentBox.contents != null && componentBox.contents.itemList.Count > 0))
                    {
                        if (componentBox.parent != null && componentBox.parent.playerOwner != null)
                        {
                            Message(componentBox.parent.playerOwner, !Configuration.IsStorable ? "Error.CantStore" : "Error.ToolboxContents");
                            return ItemContainer.CanAcceptResult.CannotAccept;
                        }
                    }
                }

                if (container.playerOwner != null && !permission.UserHasPermission(container.playerOwner.UserIDString, PERMISSION_USE))
                {
                    Message(container.playerOwner, "Error.NoCarryPermission");
                    return ItemContainer.CanAcceptResult.CannotAccept;
                }
            }

            if (container != null && container.parent != null && container.parent.skin == PRESENT_SKIN_ID && container.parent.parent?.playerOwner == null)            
                return ItemContainer.CanAcceptResult.CannotAccept;
            
            return null;
        }

        private object CanMoveItem(Item movedItem, PlayerInventory inventory, uint targetContainerID, int targetSlot, int amount)
        {
            if (movedItem == null || inventory == null)
                return null;

            if (movedItem.skin == PRESENT_SKIN_ID)
            {
                if (targetSlot == -1)
                {
                    if (targetContainerID != 0)
                    {
                        ItemContainer targetContainer = inventory.FindContainer(targetContainerID);
                        if (targetContainer == movedItem.contents)
                            return false;

                        MoveContentsToPlayer(movedItem, inventory.baseEntity);
                        return false;
                    }
                    else
                    {
                        if (Configuration.LimitCarryAmount)
                        {
                            MoveContentsToPlayer(movedItem, inventory.baseEntity);
                            Message(inventory.baseEntity, "Notification.ContentsTaken");
                            return false;
                        }

                        return null;
                    }
                }
            }

            if (Configuration.DepositAutomatically)
            {
                if (Configuration.Items.CanAcceptItem(movedItem))
                {
                    if ((inventory.loot.entitySource is LootContainer || inventory.loot.entitySource is LootableCorpse || (inventory.loot.itemSource?.GetOwnerPlayer() != inventory.baseEntity && inventory.loot.itemSource?.skin == PRESENT_SKIN_ID)))
                    {
                        if (targetContainerID == 0 && targetSlot == -1)
                        {
                            foreach (Item box in FindComponentBoxes(inventory.baseEntity))
                            {
                                if (box != null && box.contents != null)
                                {
                                    if (movedItem.MoveToContainer(box.contents))
                                        return false;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        } 
        #endregion

        #region Functions
        private void GiveComponentBox(BasePlayer player)
        {
            Item componentBox = ItemManager.CreateByItemID(PRESENT_ITEM_ID, 1, PRESENT_SKIN_ID);

            SetupComponentBox(componentBox);

            if (!componentBox.MoveToContainer(player.inventory.containerMain, player.inventory.containerMain.capacity - 1, false))
                player.GiveItem(componentBox);
        }

        private void SetupComponentBoxes(IEnumerable<Item> items)
        {
            foreach (Item componentBox in items)
                SetupComponentBox(componentBox);
        }

        private void SetupComponentBox(Item componentBox)
        {
            componentBox.name = "Component Box";

            if (componentBox.contents == null)
            {
                componentBox.contents = new ItemContainer();
                componentBox.contents.ServerInitialize(componentBox, Mathf.Clamp(Configuration.Slots, 1, 36));
                componentBox.contents.GiveUID();
            }
            else
            {
                if (componentBox.contents.capacity != Configuration.Slots)
                {
                    componentBox.contents.capacity = Mathf.Clamp(Configuration.Slots, 1, 36);
                    componentBox.contents.MarkDirty();
                }
            }

            componentBox.contents.allowedContents = ItemContainer.ContentsType.Generic;
            componentBox.contents.canAcceptItem = (Item item, int amount) => Configuration.Items.CanAcceptItem(item);
        }

        private IEnumerable<Item> FindComponentBoxes(BasePlayer player)
        {
            foreach (Item item in player.inventory.AllItems())
            {
                if (item.skin == PRESENT_SKIN_ID)                
                    yield return item;                
            }
        }

        private void BeginLootingComponentBox(BasePlayer player, Item item, bool isInInventory)
        {
            player.inventory.loot.Clear();
            player.inventory.loot.entitySource = RelationshipManager.ServerInstance;
            player.inventory.loot.PositionChecks = false;

            item.contents.onDirty += player.inventory.loot.MarkDirty;
            player.inventory.loot.itemSource = item;

            player.inventory.loot.AddContainer(item.contents);
            player.inventory.loot.SendImmediate();

            if (!isInInventory)
            {
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic_resizable");
                player.SendNetworkUpdate();
            }
        }

        private void DropComponentBoxItems(BasePlayer player)
        {
            foreach (Item componentBox in FindComponentBoxes(player))
            {
                if (componentBox.contents == null)
                    continue;

                for (int i = componentBox.contents.itemList.Count - 1; i >= 0; i--)
                {
                    Item item = componentBox.contents.itemList[i];
                    if (item != null && !player.inventory.GiveItem(item))
                        break;
                }

                if (componentBox.contents.itemList.Count > 0)
                {
                    const string BACKPACK_PREFAB = "assets/prefabs/misc/item drop/item_drop_backpack.prefab";

                    DroppedItemContainer droppedItemContainer = ItemContainer.Drop(BACKPACK_PREFAB, player.transform.position, Quaternion.identity, componentBox.contents);
                    if (droppedItemContainer != null)
                    {
                        droppedItemContainer.playerName = player.displayName;
                        droppedItemContainer.playerSteamID = player.userID;
                    }
                }

                componentBox.RemoveFromContainer();
                componentBox.Remove();
            }
        }

        private void MoveContentsToPlayer(Item componentBox, BasePlayer player, IEnumerable<Item> componentBoxes = null)
        {
            if (componentBoxes == null)
                componentBoxes = FindComponentBoxes(player);

            if (componentBoxes.Count() > 0)
            {
                for (int i = componentBox.contents.itemList.Count - 1; i >= 0; i--)
                {
                    Item item = componentBox.contents.itemList[i];
                    if (item != null)
                    {
                        bool hasMoved = false;
                        foreach (Item box in componentBoxes)
                        {
                            if (box.contents != null && item.MoveToContainer(box.contents))
                            {
                                hasMoved = true;
                                break;
                            }
                        }

                        if (!hasMoved && !item.MoveToContainer(player.inventory.containerMain))
                            item.Drop(player.transform.position, player.eyes.BodyForward() * 1.5f, Quaternion.identity);
                    }
                }
            }
        }
        #endregion

        #region Commands
        [ConsoleCommand("toolbox")]
        private void CraftToolboxConsoleCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player != null)            
                CraftToolboxCommand(player, arg.cmd.Name, arg.Args ?? Array.Empty<string>());            
        }

        [ChatCommand("toolbox")]
        private void CraftToolboxCommand(BasePlayer player, string command, string[] args)
        {
            switch (args.Length > 0 ? args[0] : "")
            {
                default:
                    {
                        Message(player, "Cmd.Format");
                        break;
                    }
                case "cost":
                    {
                        Message(player, "Craft.Cost", string.Join("\n", Configuration.Crafting.Requirements));
                        break;
                    }
                case "craft":
                    {                        
                        if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                        {
                            Message(player, "Cmd.Permission");
                            return;
                        }

                        if (!Configuration.Crafting.RequiresCrafting)
                        {
                            Message(player, "Craft.NotEnabled");
                            return;
                        }

                        if (FindComponentBoxes(player).Count() > 0)
                        {
                            Message(player, "Error.HaveToolbox");
                            return;
                        }

                        if (!Configuration.Crafting.CanAffordToCraft(player))
                            return;

                        Configuration.Crafting.PayCraftCost(player);

                        GiveComponentBox(player);

                        Message(player, "Craft.Crafted");
                        break;
                    }
                case "remove":
                    {
                        if (!Configuration.Crafting.RequiresCrafting)
                        {
                            Message(player, "Craft.NotEnabled");
                            return;
                        }

                        if (!permission.UserHasPermission(player.UserIDString, PERMISSION_USE))
                        {
                            Message(player, "Cmd.Permission");
                            return;
                        }

                        int boxCount = FindComponentBoxes(player).Count();
                        if (boxCount == 0)
                        {
                            Message(player, "Destroy.NoToolbox");
                            return;
                        }

                        if (Configuration.Crafting.RefundOnDestroy)
                        {
                            for (int i = 0; i < boxCount; i++)
                                Configuration.Crafting.RefundCraftCost(player);
                        }

                        DropComponentBoxItems(player);

                        Message(player, "Destroy.Destroyed");
                        break;
                    }
            }
        }
        #endregion

        #region Config        
        private ConfigData Configuration;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Leave component box on corpse when player is killed")]
            public bool LeaveOnCorpse { get; set; }            

            [JsonProperty(PropertyName = "Automatically place allowed items in the component box when picked up")]
            public bool DepositAutomatically { get; set; }

            [JsonProperty(PropertyName = "Number of slots in the component box (1 - 36)")]
            public int Slots { get; set; }

            [JsonProperty(PropertyName = "Allow players to drop the component box")]
            public bool IsDroppable { get; set; }

            [JsonProperty(PropertyName = "Allow players to store the component box in other containers")]
            public bool IsStorable { get; set; }

            [JsonProperty(PropertyName = "Only allow players to carry 1 component box at a time")]
            public bool LimitCarryAmount { get; set; }


            [JsonProperty(PropertyName = "Crafting Settings")]
            public CraftingSettings Crafting { get; set; }

            [JsonProperty(PropertyName = "Item Settings")]
            public ItemSettings Items { get; set; }


            public class ItemSettings
            {
                [JsonProperty(PropertyName = "Allowed item categories")]
                public Hash<ItemCategory, bool> Categories { get; set; }

                [JsonProperty(PropertyName = "Blocked item shortnames")]
                public string[] BlockedItems { get; set; }

                [JsonProperty(PropertyName = "Allowed item shortnames")]
                public string[] AllowedItems { get; set; }

                public bool CanAcceptItem(Item item)
                {
                    if (AllowedItems.Contains(item.info.shortname))
                        return true;

                    return Categories[item.info.category] && !BlockedItems.Contains(item.info.shortname);
                }
            }


            public class CraftingSettings
            {
                [JsonProperty(PropertyName = "Require component box to be crafted")]
                public bool RequiresCrafting { get; set; }

                [JsonProperty(PropertyName = "Refund crafting cost when destroying the component box")]
                public bool RefundOnDestroy { get; set; }

                [JsonProperty(PropertyName = "Refund cost fraction (0.0 - 1.0)")]
                public float RefundPercentage { get; set; }

                [JsonProperty(PropertyName = "Cost to craft a component box")]
                public List<CraftItem> Cost { get; set; }


                [JsonIgnore]
                private IEnumerable<string> craftingRequirements;

                [JsonIgnore]
                public IEnumerable<string> Requirements
                {
                    get
                    {
                        if (craftingRequirements == null)
                            craftingRequirements = Cost.Where(x => x.IsValid).Select(x => x.CostString);
                        return craftingRequirements;
                    }
                }


                public bool CanAffordToCraft(BasePlayer player)
                {
                    foreach (CraftItem craftItem in Cost)
                    {
                        if (!craftItem.IsValid)
                            continue;

                        if (player.inventory.GetAmount(craftItem.ItemID) < craftItem.Amount)
                        {
                            player.ChatMessage($"You require {craftItem.CostString} to craft!");
                            return false;
                        }                                              
                    }

                    return true;
                }

                public void PayCraftCost(BasePlayer player)
                {
                    foreach (CraftItem craftItem in Cost)
                    {
                        if (!craftItem.IsValid)
                            continue;

                        player.inventory.Take(null, craftItem.ItemID, craftItem.Amount);
                    }
                }

                public void RefundCraftCost(BasePlayer player)
                {
                    foreach (CraftItem craftItem in Cost)
                    {
                        if (!craftItem.IsValid)
                            continue;

                        int amount = Mathf.RoundToInt((float)craftItem.Amount * Mathf.Clamp01(RefundPercentage));
                        if (amount > 0)
                            player.GiveItem(ItemManager.CreateByItemID(craftItem.ItemID, amount));
                    }
                }

                public class CraftItem
                {
                    public string Shortname { get; set; }

                    public int Amount { get; set; }

                    [JsonIgnore]
                    public int ItemID { get; set; }

                    [JsonIgnore]
                    public string CostString { get; private set; }

                    [JsonIgnore]
                    public bool IsValid { get; private set; }

                    public void Validate()
                    {
                        ItemDefinition itemDefinition = ItemManager.FindItemDefinition(Shortname);
                        if (itemDefinition == null)
                        {
                            Debug.LogError($"ComponentBox has a invalid item shortname as a crafting cost : {Shortname}");
                            return;
                        }

                        ItemID = itemDefinition.itemid;
                        CostString = $"{Amount} x {itemDefinition.displayName.english}";
                        IsValid = true;
                    }
                }
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
                Items = new ConfigData.ItemSettings
                {
                    Categories = new Hash<ItemCategory, bool>
                    {
                        [ItemCategory.All] = false,
                        [ItemCategory.Weapon] = false,
                        [ItemCategory.Construction] = false,
                        [ItemCategory.Items] = false,
                        [ItemCategory.Resources] = false,
                        [ItemCategory.Attire] = false,
                        [ItemCategory.Tool] = false,
                        [ItemCategory.Medical] = false,
                        [ItemCategory.Food] = false,
                        [ItemCategory.Ammunition] = false,
                        [ItemCategory.Traps] = false,
                        [ItemCategory.Misc] = false,
                        [ItemCategory.Component] = true,
                        [ItemCategory.Electrical] = false,
                        [ItemCategory.Fun] = false,
                    },
                    BlockedItems = new string[]
                    {
                        "explosive.timed",
                        "ammo.rocket.basic",
                        "ammo.rocket.smoke",
                        "ammo.rocket.hv",
                        "ammo.rocket.fire",
                        "explosives",
                        "gunpowder",
                        "grenade.f1",
                        "grenade.beancan",
                        "explosive.satchel",
                        "ammo.grenadelauncher.he",
                        "ammo.grenadelauncher.smoke",
                        "xmas.present.large"
                    },
                    AllowedItems = new string[]
                    {
                        "scrap",
                        "keycard_green",
                        "keycard_blue",
                        "keycard_red",
                        "cctv.camera",
                        "targeting.computer"
                    }
                },
                Crafting = new ConfigData.CraftingSettings
                {
                    RequiresCrafting = false,
                    RefundOnDestroy = false,
                    RefundPercentage = 1f,
                    Cost = new List<ConfigData.CraftingSettings.CraftItem>
                    {
                        new ConfigData.CraftingSettings.CraftItem
                        {
                            Shortname = "metal.refined",
                            Amount = 10
                        },
                        new ConfigData.CraftingSettings.CraftItem
                        {
                            Shortname = "metal.fragments",
                            Amount = 400
                        }
                    }
                },
                DepositAutomatically = true,
                LeaveOnCorpse = false,
                Slots = 12,
                LimitCarryAmount = true,
                IsDroppable = true,
                IsStorable = true,
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

        #region Localization
        private void Message(BasePlayer player, string key, params string[] args)
        {
            string msg = lang.GetMessage(key, this, player.UserIDString);
            if (args != null && args.Length > 0)
                player.ChatMessage(string.Format(msg, args));
            else player.ChatMessage(msg);
        }

        private readonly Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Error.CantDrop"] = "You are not allowed to drop the component box!",
            ["Error.NoCarryPermission"] = "You do not have permission to carry a toolbox",
            ["Error.HaveToolbox"] = "You can only have one toolbox at a time",
            ["Error.CantStore"] = "You are not allowed to store the component box!",
            ["Error.ToolboxContents"] = "You can only store a component box in a container when it's empty",
            ["Notification.ContentsTaken"] = "You can only have one toolbox at a time. The contents of this toolbox have been moved to your inventory",
            ["Cmd.Format"] = "Incorrect format! Use /toolbox (craft, cost, remove)",
            ["Cmd.Permission"] = "You do not have the permission to use this command!",
            ["Craft.Cost"] = "Cost:\n{0}",
            ["Craft.NotEnabled"] = "The toolbox is not craftable!",
            ["Craft.Crafted"] = "You have crafted a toolbox!",
            ["Destroy.NoToolbox"] = "You don't have a toolbox!",
            ["Destroy.Destroyed"] = "Removed your toolbox!",
        };
        #endregion
    }
}
