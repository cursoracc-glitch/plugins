using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System;
using SilentOrbit.ProtocolBuffers;
using System.Linq;
using System.Diagnostics;
using System.IO;
using Network;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Plugins.CustomItemDefinitionExtensions;
using UnityEngine.SceneManagement;
using Oxide.Core;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("CustomItemDefinitions", "0xF [dsc.gg/0xf-plugins]", "1.3.0")]
    [Description("Library of the Future. Allows you to create your own full-fledged custom items with own item definition.")]
    public class CustomItemDefinitions : RustPlugin
    {
        #region Consts
        public const ItemDefinition.Flag CUSTOM_DEFINITION_FLAG = (ItemDefinition.Flag)128;
        public static readonly ItemDefinition FallbackItemDefinition = ItemManager.FindItemDefinition("coal");
        public static readonly ItemDefinition FakeBlueprintItemDefinition = ItemManager.FindItemDefinition("rhib");
        #endregion

        #region CustomItemDefinition Class
        public class CustomItemDefinition
        {
            public int parentItemId;
            public string shortname;
            public int? itemId;
            public string defaultName;
            public string defaultDescription;
            public ulong defaultSkinId;
            public int? maxStackSize;
            public ItemCategory? category;
            public ItemDefinition.Flag flags;
            public ItemMod[] itemMods;
            public bool repairable;
            public bool craftable;
            public List<ItemAmount> blueprintIngredients;
            public int workbenchLevelRequired;

            public static CustomItemDefinition FromObject(object @object)
            {
                Type cidType = typeof(CustomItemDefinition);
                Type argType = @object.GetType();
                CustomItemDefinition @new = new CustomItemDefinition();

                foreach (PropertyInfo propertyInfo in argType.GetProperties())
                    SetField(@new, propertyInfo.Name, propertyInfo.GetValue(@object));

                foreach (FieldInfo fieldInfo in argType.GetFields())
                    SetField(@new, fieldInfo.Name, fieldInfo.GetValue(@object));

                void SetField(CustomItemDefinition cid, string fieldName, object value)
                {
                    FieldInfo fieldInfo = cidType.GetField(fieldName);
                    if (fieldInfo == null)
                    {
                        PluginInstance?.PrintWarning(string.Format("The field named \"{0}\" is missing in the CustomItemDefinition class, skipped", fieldName));
                        return;
                    }
                    fieldInfo.SetValue(cid, value);
                }
                return @new;
            }
        }
        #endregion

        #region Variables
        private static CustomItemDefinitions PluginInstance;
        private static Dictionary<int, ItemDefinition> AlreadyCreatedItemDefinitions;
        private static Dictionary<Plugin, HashSet<CustomItemDefinition>> CustomDefinitions = new Dictionary<Plugin, HashSet<CustomItemDefinition>>();
        #endregion

        #region Hooks
        void Init()
        {

            PluginInstance = this;

            CacheAlreadyCreated();

            FieldsToMutate.Initialize();

#if CARBON
            HarmonyInstance.Patch(AccessTools.Method(typeof(Carbon.Core.ModLoader), "UninitializePlugin"), prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.UnloadPlugin)));
#else
            HarmonyInstance.Patch(AccessTools.Method(typeof(Oxide.Core.OxideMod), "UnloadPlugin"), prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.UnloadPlugin)));
#endif
            HarmonyInstance.Patch(AccessTools.Method(typeof(Item), "Load"), prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.Item_Load1)), postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.Item_Load2)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(VendingMachineMapMarker), "GetAppMarkerData"), postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.VendingMachineMapMarker_GetAppMarkerData)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(StorageMonitor), "FillEntityPayload"), postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.StorageMonitor_FillEntityPayload)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(ProtoBuf.PlayerUpdateLoot), "WriteToStream"), new HarmonyMethod(typeof(Patches), nameof(Patches.PlayerUpdateLoot_WriteToStream)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(ProtoBuf.UpdateItem), "WriteToStream"), new HarmonyMethod(typeof(Patches), nameof(Patches.UpdateItem_WriteToStream)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(ProtoBuf.UpdateItemContainer), "WriteToStream"), new HarmonyMethod(typeof(Patches), nameof(Patches.UpdateItemContainer_WriteToStream)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(ProtoBuf.VendingMachine.SellOrderContainer), "WriteToStream"), new HarmonyMethod(typeof(Patches), nameof(Patches.SellOrderContainer_WriteToStream)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(ProtoBuf.ItemAmountList), "WriteToStream"), new HarmonyMethod(typeof(Patches), nameof(Patches.ItemAmountList_WriteToStream)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(ProtoBuf.IndustrialConveyor.ItemFilterList), "WriteToStream"), new HarmonyMethod(typeof(Patches), nameof(Patches.ItemFilterList_WriteToStream)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(ProtoBuf.IndustrialConveyorTransfer), "WriteToStream"), new HarmonyMethod(typeof(Patches), nameof(Patches.IndustrialConveyorTransfer_WriteToStream)));
            HarmonyInstance.Patch(AccessTools.Method(typeof(CuiImageComponent), "get_ItemId"), postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.CuiItemIconPatch)));
        }

        private void Loaded()
        {
            Interface.Oxide.CallHook("OnCIDLoaded", this);
        }

        private void Unload()
        {
            foreach (Plugin plugin in CustomDefinitions.Keys)
                UnloadPluginItemDefinitions(plugin);
            CustomDefinitions.Clear();

            Interface.Oxide.CallHook("OnCIDUnloaded");
        }

        private static void OnItemDefinitionBroken(Item item, ProtoBuf.Item protoItem)
        {
            UnityEngine.Debug.LogWarning("Item has broken definition, the fallback item definition will be applied to it.");
            item.info = FallbackItemDefinition;
            item.text = protoItem.itemid.ToString();
        }

        void OnEntitySaved(BaseNetworkable entity, BaseNetworkable.SaveInfo saveInfo)
        {
            if (!saveInfo.forDisk)
                Mutate.ToClientSide(saveInfo.msg);
        }

        void OnLootNetworkUpdate(PlayerLoot loot)
        {
            RepairBench repairBench = loot.entitySource as RepairBench;
            if (repairBench == null)
                return;

            Item repairableItem = repairBench.inventory?.GetSlot(0);
            if (repairableItem == null)
                return;

            ItemDefinition itemDefinition = repairableItem.info;
            if (!itemDefinition.Blueprint || !itemDefinition.condition.repairable)
                return;

            BasePlayer player = loot.baseEntity;

            if (IsValidCustomItemDefinition(itemDefinition) && player.blueprints.HasUnlocked(itemDefinition))
                SendFakeUnlockedBlueprint(player, repairableItem.info.Parent.itemid);
            else
                player.SendNetworkUpdateImmediate();
        }

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem == null || newItem.info != ItemManager.blueprintBaseDef)
                return;

            ItemDefinition blueprintTargetDef = newItem.blueprintTargetDef;
            if (blueprintTargetDef == null)
                return;

            player.ShowToast(GameTip.Styles.Blue_Normal, $"{blueprintTargetDef.displayName.translated} BLUEPRINT");
        }

        object OnItemCraft(IndustrialCrafter crafter, ItemBlueprint blueprint)
        {
            if (IsValidCustomItemDefinition(blueprint.targetItem) && !blueprint.userCraftable)
                return false;
            return null;
        }
        #endregion

        #region Commands
        [ConsoleCommand("itemid")]
        private void itemid(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
                return;
            string shortname = arg.GetString(0);
            if (string.IsNullOrEmpty(shortname))
                return;

            ItemDefinition itemDefinition = ItemManager.FindItemDefinition(shortname);
            if (itemDefinition == null)
            {
                arg.ReplyWith("Item definion for the specified short name was not found.");
                return;
            }

            arg.ReplyWith(itemDefinition.itemid);
        }
        #endregion

        #region Classes
        public static class FieldsToMutate
        {
            public static List<FieldInfo> validFields;

            public static void Initialize()
            {
                validFields = CollectFields(typeof(ProtoBuf.Entity));
            }

            private static List<FieldInfo> CollectFields(Type type)
            {
                List<FieldInfo> result = new List<FieldInfo>();
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (IsValidField(field))
                    {
                        result.Add(field);
                    }
                    else if (typeof(IProto).IsAssignableFrom(field.FieldType))
                    {
                        List<FieldInfo> collectedFields = CollectFields(field.FieldType);
                        if (collectedFields.Count > 0)
                        {
                            result.Add(field);
                            result.AddRange(CollectFields(field.FieldType));
                        }
                    }
                }
                return result;
            }

            public static bool IsValidField(FieldInfo field)
            {
                return Mutate.TypeToMethodMap.ContainsKey(field.FieldType);
            }
        }

        public static class Mutate
        {
            public static readonly Dictionary<Type, Action<object>> TypeToMethodMap = new Dictionary<Type, Action<object>>
            {
                { typeof(ProtoBuf.Item), obj => ToClientSide((ProtoBuf.Item)obj) },
                { typeof(ProtoBuf.ItemContainer), obj => ToClientSide((ProtoBuf.ItemContainer)obj) },
                { typeof(ProtoBuf.VendingMachine.SellOrder), obj => ToClientSide((ProtoBuf.VendingMachine.SellOrder)obj) },
                { typeof(ProtoBuf.WeaponRackItem), obj => ToClientSide((ProtoBuf.WeaponRackItem)obj) },
                { typeof(ProtoBuf.IndustrialConveyor.ItemFilter), obj => ToClientSide((ProtoBuf.IndustrialConveyor.ItemFilter)obj) },
                { typeof(ProtoBuf.ItemCrafter.Task), obj => ToClientSide((ProtoBuf.ItemCrafter.Task)obj) },
                { typeof(ProtoBuf.AppEntityPayload.Item), obj => ToClientSide((ProtoBuf.AppEntityPayload.Item)obj) },
                { typeof(ProtoBuf.ItemAmountList), obj => ToClientSide((ProtoBuf.ItemAmountList)obj) },
                { typeof(ProtoBuf.FrankensteinTable), obj => ToClientSide((ProtoBuf.FrankensteinTable)obj) },
                { typeof(List<ProtoBuf.Item>), obj => ToClientSide((List<ProtoBuf.Item>)obj) },
                { typeof(List<ProtoBuf.ItemContainer>), obj => ToClientSide((List<ProtoBuf.ItemContainer>)obj) },
                { typeof(List<ProtoBuf.VendingMachine.SellOrder>), obj => ToClientSide((List<ProtoBuf.VendingMachine.SellOrder>)obj) },
                { typeof(List<ProtoBuf.WeaponRackItem>), obj => ToClientSide((List<ProtoBuf.WeaponRackItem>)obj) },
                { typeof(List<ProtoBuf.IndustrialConveyor.ItemFilter>), obj => ToClientSide((List<ProtoBuf.IndustrialConveyor.ItemFilter>)obj) },
                { typeof(List<ProtoBuf.ItemCrafter.Task>), obj => ToClientSide((List<ProtoBuf.ItemCrafter.Task>)obj) },
                { typeof(List<ProtoBuf.AppEntityPayload.Item>), obj => ToClientSide((List<ProtoBuf.AppEntityPayload.Item>)obj) },
            };

            public static void ToClientSide(ProtoBuf.Entity protoEntity)
            {
                if (FieldsToMutate.validFields == null)
                    return;

                Stack<object> stack = new Stack<object>();
                stack.Push(protoEntity);
                for (int i = 0; i < FieldsToMutate.validFields.Count; i++)
                {
                    FieldInfo field = FieldsToMutate.validFields[i];
                    object peek = stack.Peek();
                    if (field.DeclaringType == peek.GetType())
                    {
                        object fieldValue = field.GetValue(peek);
                        if (fieldValue != null)
                        {
                            if (FieldsToMutate.IsValidField(field))
                            {
                                Mutate.ToClientSide(fieldValue);
                                while (i + 1 < FieldsToMutate.validFields.Count && FieldsToMutate.validFields[i + 1]?.DeclaringType != peek.GetType())
                                {
                                    stack.Pop();
                                    peek = stack.Peek();
                                }
                            }
                            else
                            {
                                stack.Push(fieldValue);
                            }
                        }
                    }
                }
            }

            public static void ToClientSide(object obj)
            {
                if (TypeToMethodMap.TryGetValue(obj.GetType(), out var method))
                {
                    method.Invoke(obj);
                }
                else
                {
                    UnityEngine.Debug.LogError("Unknown type to mutate: " + obj.GetType());
                }
            }

            public static void ToClientSide(ProtoBuf.VendingMachine.SellOrderContainer sellOrderContainer)
            {
                if (sellOrderContainer.sellOrders != null)
                    ToClientSide(sellOrderContainer.sellOrders);
            }

            public static void ToClientSide(List<ProtoBuf.VendingMachine.SellOrder> sellOrders)
            {
                foreach (ProtoBuf.VendingMachine.SellOrder order in sellOrders)
                    ToClientSide(order);
            }

            public static void ToClientSide(ProtoBuf.VendingMachine.SellOrder sellOrder)
            {
                ItemDefinition itemDefToSell = ItemManager.FindItemDefinition(sellOrder.itemToSellID);
                if (IsValidCustomItemDefinition(itemDefToSell))
                    sellOrder.itemToSellID = itemDefToSell.Parent.itemid;
                ItemDefinition currencyDef = ItemManager.FindItemDefinition(sellOrder.currencyID);
                if (IsValidCustomItemDefinition(currencyDef))
                    sellOrder.currencyID = currencyDef.Parent.itemid;
            }

            public static void ToClientSide(List<ProtoBuf.WeaponRackItem> weaponRackItems)
            {
                foreach (ProtoBuf.WeaponRackItem weaponRackItem in weaponRackItems)
                    ToClientSide(weaponRackItem);
            }

            public static void ToClientSide(ProtoBuf.WeaponRackItem rackItem)
            {
                ItemDefinition itemDef = ItemManager.FindItemDefinition(rackItem.itemID);
                if (IsValidCustomItemDefinition(itemDef))
                {
                    rackItem.itemID = itemDef.Parent.itemid;
                    if (rackItem.skinid == 0)
                    {
                        DefaultProperties defaultProperties = itemDef.GetComponent<DefaultProperties>();
                        if (defaultProperties != null && defaultProperties.skinId != 0UL)
                            rackItem.skinid = defaultProperties.skinId;
                    }
                }
            }

            public static void ToClientSide(List<ProtoBuf.IndustrialConveyor.ItemFilter> filters)
            {
                foreach (ProtoBuf.IndustrialConveyor.ItemFilter filter in filters)
                    ToClientSide(filter);
            }

            public static void ToClientSide(ProtoBuf.IndustrialConveyor.ItemFilter filter)
            {
                ItemDefinition itemDef = ItemManager.FindItemDefinition(filter.itemDef);
                if (IsValidCustomItemDefinition(itemDef))
                    filter.itemDef = itemDef.Parent.itemid;
            }

            public static void ToClientSide(List<ProtoBuf.IndustrialConveyorTransfer.ItemTransfer> transfers)
            {
                for (int i = 0; i < transfers.Count; i++)
                {
                    ProtoBuf.IndustrialConveyorTransfer.ItemTransfer transfer = transfers[i];
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(transfers[i].itemId);
                    if (IsValidCustomItemDefinition(itemDef))
                        transfers[i] = new ProtoBuf.IndustrialConveyorTransfer.ItemTransfer { itemId = itemDef.Parent.itemid, amount = transfer.amount };
                }
            }

            public static void ToClientSide(List<ProtoBuf.ItemCrafter.Task> tasks)
            {
                foreach (ProtoBuf.ItemCrafter.Task task in tasks)
                    ToClientSide(tasks);
            }

            public static void ToClientSide(ProtoBuf.ItemCrafter.Task task)
            {
                ItemDefinition itemDef = ItemManager.FindItemDefinition(task.itemID);
                if (IsValidCustomItemDefinition(itemDef))
                {
                    task.itemID = itemDef.Parent.itemid;
                    if (task.skinID == 0)
                    {
                        DefaultProperties defaultProperties = itemDef.GetComponent<DefaultProperties>();
                        if (defaultProperties != null && defaultProperties.skinId != 0UL)
                            task.skinID = (int)defaultProperties.skinId;
                    }
                }
            }

            public static void ToClientSide(ProtoBuf.ItemAmountList itemAmountList)
            {
                if (itemAmountList.itemID == null)
                    return;

                for (int i = 0; i < itemAmountList.itemID.Count; i++)
                {
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(itemAmountList.itemID[i]);
                    if (IsValidCustomItemDefinition(itemDef))
                        itemAmountList.itemID[i] = itemDef.Parent.itemid;
                }
            }

            public static void ToClientSide(List<ProtoBuf.AppEntityPayload.Item> items)
            {
                foreach (ProtoBuf.AppEntityPayload.Item item in items)
                    ToClientSide(item);
            }

            public static void ToClientSide(ProtoBuf.AppEntityPayload.Item item)
            {
                ItemDefinition itemDef = ItemManager.FindItemDefinition(item.itemId);
                if (IsValidCustomItemDefinition(itemDef))
                {
                    item.itemId = itemDef.Parent.itemid;
                }
            }

            public static void ToClientSide(List<ProtoBuf.AppMarker.SellOrder> sellOrders)
            {
                foreach (ProtoBuf.AppMarker.SellOrder sellOrder in sellOrders)
                    ToClientSide(sellOrder);
            }

            public static void ToClientSide(ProtoBuf.AppMarker.SellOrder sellOrder)
            {
                ItemDefinition itemDef = ItemManager.FindItemDefinition(sellOrder.itemId);
                if (IsValidCustomItemDefinition(itemDef))
                    sellOrder.itemId = itemDef.Parent.itemid;
            }

            public static void ToClientSide(ProtoBuf.FrankensteinTable frankensteinTable)
            {
                if (frankensteinTable.itemIds == null)
                    return;

                for (int i = 0; i < frankensteinTable.itemIds.Count; i++)
                {
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(frankensteinTable.itemIds[i]);
                    if (IsValidCustomItemDefinition(itemDef))
                        frankensteinTable.itemIds[i] = itemDef.Parent.itemid;
                }
            }

            public static void ToClientSide(List<ProtoBuf.ItemContainer> containers)
            {
                foreach (ProtoBuf.ItemContainer container in containers)
                    ToClientSide(container);
            }

            public static void ToClientSide(ProtoBuf.ItemContainer container)
            {
                if (container.contents != null)
                    ToClientSide(container.contents);
            }

            public static void ToClientSide(List<ProtoBuf.Item> items)
            {
                foreach (ProtoBuf.Item item in items)
                    ToClientSide(item);
            }

            public static void ToClientSide(ProtoBuf.Item protoItem)
            {
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(protoItem.itemid);
                if (itemDefinition == null)
                {
                    protoItem.itemid = FallbackItemDefinition.itemid;
                    return;
                }

                if (protoItem.contents != null)
                    ToClientSide(protoItem.contents);


                bool IsBlueprint = itemDefinition == ItemManager.blueprintBaseDef;

                if (IsBlueprint && protoItem.instanceData != null)
                {
                    ItemDefinition blueprintItemDef = ItemManager.FindItemDefinition(protoItem.instanceData.blueprintTarget);
                    if (blueprintItemDef == null)
                        return;
                    itemDefinition = blueprintItemDef;
                }

                if (!IsValidCustomItemDefinition(itemDefinition))
                    return;

                if (!IsBlueprint)
                {
                    protoItem.itemid = itemDefinition.Parent.itemid;

                    DefaultProperties defaultProperties = itemDefinition.GetComponent<DefaultProperties>();
                    if (protoItem.name == null && defaultProperties.finalName != null)
                        protoItem.name = defaultProperties.finalName;
                    if (protoItem.skinid == 0UL && defaultProperties.skinId != 0UL)
                        protoItem.skinid = defaultProperties.skinId;
                }
                else if (protoItem.instanceData != null)
                {
                    protoItem.instanceData = new ProtoBuf.Item.InstanceData() { ShouldPool = true };
                    protoItem.instanceData.blueprintTarget = FakeBlueprintItemDefinition.itemid;
                }
            }
        }
        #endregion

        #region Methods
        private object Register(object @object, Plugin plugin)
        {
            if (@object is IEnumerable enumerable)
            {
                List<CustomItemDefinition> list = new List<CustomItemDefinition>();
                foreach (object item in enumerable)
                    list.Add(CustomItemDefinition.FromObject(item));

                return RegisterPluginItemDefinitions(list, plugin);
            }
            else
            {
                return RegisterPluginItemDefinition(CustomItemDefinition.FromObject(@object), plugin);
            }
        }

        private static void CacheAlreadyCreated()
        {
            if (AlreadyCreatedItemDefinitions == null)
            {
                AlreadyCreatedItemDefinitions = new Dictionary<int, ItemDefinition>();
                if (ServerMgr.Instance != null)
                {
                    foreach (GameObject gameObject in ServerMgr.Instance.gameObject.scene.GetRootGameObjects())
                    {
                        if (!gameObject.name.IsNumeric())
                            continue;

                        if (gameObject.TryGetComponent(out ItemDefinition itemDefinition))
                            AlreadyCreatedItemDefinitions.Add(itemDefinition.itemid, itemDefinition);
                    }
                }
            }
        }

        public static HashSet<ItemDefinition> RegisterPluginItemDefinitions(IEnumerable<CustomItemDefinition> definitions, Plugin plugin)
        {
            HashSet<ItemDefinition> hashSet = new HashSet<ItemDefinition>();
            foreach (var definition in definitions)
            {
                ItemDefinition resultItemDefinition = RegisterPluginItemDefinition(definition, plugin);
                if (resultItemDefinition != null)
                    hashSet.Add(resultItemDefinition);
            };
            return hashSet;
        }

        public static ItemDefinition RegisterPluginItemDefinition(CustomItemDefinition definition, Plugin plugin)
        {
            HashSet<CustomItemDefinition> pluginHashSet = null;
            if (!CustomDefinitions.TryGetValue(plugin, out pluginHashSet))
                pluginHashSet = (CustomDefinitions[plugin] = new HashSet<CustomItemDefinition>());

            if (pluginHashSet.Contains(definition))
            {
                PluginInstance.PrintError("Error by the plugin \"{0}\": The provided CustomItemDefinition is already contained", plugin.Name);
                return null;
            }


            if (string.IsNullOrEmpty(definition.shortname) || definition.parentItemId == 0)
            {
                PluginInstance.PrintError("Error of incorrect data provided by the plugin \"{0}\": The fields shortname, parentItemId is required", plugin.Name);
                return null;
            }

            if (!definition.itemId.HasValue)
                definition.itemId = definition.shortname.GetHashCode();


            else if (ItemManager.FindItemDefinition(definition.shortname) != null)
            {
                PluginInstance.PrintError("Error of incorrect data provided by the plugin \"{0}\": Shortname must be unique! The provided shortname - \"{1}\"", plugin.Name, definition.shortname);
                return null;
            }
            else if (ItemManager.FindItemDefinition(definition.itemId.Value) != null)
            {
                PluginInstance.PrintError("Error of incorrect data provided by the plugin \"{0}\": ItemId must be unique! The provided itemId - \"{1}\"", plugin.Name, definition.itemId);
                return null;
            }

            ItemDefinition parentItemDefinition = ItemManager.FindItemDefinition(definition.parentItemId);
            if (parentItemDefinition == null)
            {
                PluginInstance.PrintError("Error of incorrect data provided by the plugin \"{0}\": ItemDefinition by parentItemId not found! The provided parentItemId - \"{1}\"", plugin.Name, definition.parentItemId);
                return null;
            }
            if (parentItemDefinition.gameObject.scene.isLoaded)
            {
                PluginInstance.PrintError("Error by the plugin \"{0}\": You cannot use a custom ItemDefinition as a parent!", plugin.Name);
                return null;
            }

            ItemDefinition newItemDefinition;
            if (!AlreadyCreatedItemDefinitions.TryGetValue(definition.itemId.Value, out newItemDefinition))
            {
                if (newItemDefinition == null)
                {
                    newItemDefinition = CloneItemDefinition(parentItemDefinition);
                    newItemDefinition.name = definition.itemId.ToString();
                }
                AlreadyCreatedItemDefinitions[definition.itemId.Value] = newItemDefinition;
            }

            if (newItemDefinition == null)
            {
                PluginInstance.PrintError("Error by the plugin \"{0}\": Failure to create or search for a new item definition ({1})!", plugin.Name, definition.shortname);
                return null;
            }

            newItemDefinition.shortname = definition.shortname;
            newItemDefinition.itemid = definition.itemId.Value;
            if (definition.defaultName != null)
                newItemDefinition.displayName = definition.defaultName;
            if (definition.defaultDescription != null)
                newItemDefinition.displayDescription = definition.defaultDescription;
            newItemDefinition.Parent = parentItemDefinition;
            newItemDefinition.flags = CUSTOM_DEFINITION_FLAG;
            newItemDefinition.flags |= definition.flags;
            if (definition.category.HasValue)
                newItemDefinition.category = definition.category.Value;
            if (definition.maxStackSize.HasValue)
                newItemDefinition.stackable = definition.maxStackSize.Value;
            newItemDefinition.condition.repairable = definition.repairable;
            newItemDefinition.itemMods = null;
            foreach (ItemMod mod in newItemDefinition.GetComponentsInChildren<ItemMod>(true))
                UnityEngine.Object.DestroyImmediate(mod);


            if (!newItemDefinition.TryGetComponent(out ItemBlueprint itemBlueprint) && (definition.craftable || definition.repairable))
                itemBlueprint = newItemDefinition.gameObject.AddComponent<ItemBlueprint>();

            if (itemBlueprint)
            {
                itemBlueprint.defaultBlueprint = false;
                itemBlueprint.userCraftable = definition.craftable;
                itemBlueprint.isResearchable = definition.craftable || definition.repairable;
                itemBlueprint.workbenchLevelRequired = definition.workbenchLevelRequired;
                itemBlueprint.ingredients = definition.blueprintIngredients != null && definition.blueprintIngredients.Count > 0 ? definition.blueprintIngredients : parentItemDefinition.Blueprint?.ingredients ?? new List<ItemAmount>();
            }

            DefaultProperties defaultProperties = newItemDefinition.gameObject.AddComponent<DefaultProperties>();
            defaultProperties.name = definition.defaultName;
            defaultProperties.description = definition.defaultDescription;
            defaultProperties.finalName = FormatNameWithDescription(definition.defaultName, definition.defaultDescription);
            defaultProperties.skinId = definition.defaultSkinId;
            if (definition.itemMods != null)
            {
                foreach (ItemMod mod in definition.itemMods)
                {
                    if ((mod as object) == null)
                        continue;

                    Type type = mod.GetType();
                    Component component = newItemDefinition.gameObject.AddComponent(type);
                    mod.CopyFields(component as ItemMod);
                }
            }
            newItemDefinition.Initialize(ItemManager.itemList);
            ItemManager.itemDictionary[definition.itemId.Value] = newItemDefinition;
            ItemManager.itemDictionaryByName[definition.shortname] = newItemDefinition;
            if (!ItemManager.itemList.Contains(newItemDefinition))
                ItemManager.itemList.Add(newItemDefinition);

            pluginHashSet.Add(definition);
            return newItemDefinition;
        }

        public static void UnloadPluginItemDefinitions(Plugin plugin)
        {
            if (CustomDefinitions.TryGetValue(plugin, out HashSet<CustomItemDefinition> definitions))
            {
                foreach (var definition in definitions)
                    UnloadCustomItemDefinition(definition);
                definitions.Clear();
            }
                
        }
        public static void UnloadPluginItemDefinition(Plugin plugin, CustomItemDefinition definition)
        {
            if (CustomDefinitions.TryGetValue(plugin, out HashSet<CustomItemDefinition> definitions))
            {
                if (definitions.Contains(definition))
                {
                    UnloadCustomItemDefinition(definition);
                    definitions.Remove(definition);
                }
            }
        }

        private static void UnloadCustomItemDefinition(CustomItemDefinition definition)
        {
            if (ItemManager.itemDictionaryByName.TryGetValue(definition.shortname, out ItemDefinition itemDefinition))
            {
                itemDefinition.itemMods = null;
                foreach (ItemMod mod in itemDefinition.GetComponentsInChildren<ItemMod>(true))
                    UnityEngine.Object.DestroyImmediate(mod);
                itemDefinition.Initialize(ItemManager.itemList);
                ItemManager.itemDictionary.Remove(definition.itemId.Value);
                ItemManager.itemDictionaryByName.Remove(definition.shortname);
                ItemManager.itemList.Remove(itemDefinition);
            }
        }

        private static ItemDefinition CloneItemDefinition(ItemDefinition itemDefinition)
        {
            if (itemDefinition == null)
                return null;
            GameObject clone = UnityEngine.Object.Instantiate(itemDefinition.gameObject);
            UnityEngine.Object.DontDestroyOnLoad(clone);
            return clone.GetComponent<ItemDefinition>();
        }

        private static ItemDefinition CloneItemDefinition(string shortname) => CloneItemDefinition(ItemManager.FindItemDefinition(shortname));
        private static ItemDefinition CloneItemDefinition(int itemId) => CloneItemDefinition(ItemManager.FindItemDefinition(itemId));

        private static bool IsValidCustomItemDefinition(ItemDefinition itemDefinition)
        {
            if (itemDefinition == null)
                return false;

            return itemDefinition.HasFlag(CUSTOM_DEFINITION_FLAG) && itemDefinition.Parent != null;
        }

        private static void SendSnapshotWithUnlockedBlueprint(BasePlayer player, int itemId)
        {
            if (player == null || player.net == null || !player.IsConnected)
                return;

            Network.Connection connection = player.net.connection;
            try
            {
                NetWrite netWrite = Network.Net.sv.StartWrite();
                global::BaseNetworkable.SaveInfo saveInfo = new global::BaseNetworkable.SaveInfo
                {
                    forConnection = player.net.connection,
                    forDisk = false
                };
                netWrite.PacketID(Message.Type.Entities);
                netWrite.UInt32(connection.validate.entityUpdates + 1U);
                using (saveInfo.msg = Facepunch.Pool.Get<ProtoBuf.Entity>())
                {
                    player.Save(saveInfo);
                    ProtoBuf.PersistantPlayer persistantData = saveInfo.msg.basePlayer.persistantData;
                    if (persistantData != null && persistantData.unlockedItems != null && !persistantData.unlockedItems.Contains(itemId))
                        persistantData.unlockedItems.Add(itemId);
                    saveInfo.msg.ToProto(netWrite);
                }
                netWrite.Send(new SendInfo(player.net.connection));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
            finally
            {
                connection.validate.entityUpdates++;
            }
        }

        private static void SendFakeUnlockedBlueprint(BasePlayer player, int itemId)
        {
            SendSnapshotWithUnlockedBlueprint(player, itemId);
            player.ClientRPCPlayer<int>(null, player, "UnlockedBlueprint", 0); // [Clientside RPC] Update blueprints
        }

        private static string FormatNameWithDescription(string name, string description)
        {
            if (!string.IsNullOrEmpty(description))
                return $"{string.Concat(Enumerable.Repeat("\n", description.Count(c => c == '\n')))}\n{name}\n\t\t\t\t\t\t\t\t\t\t\t\t{description.Replace("\n", "\n\t\t\t\t\t\t\t\t\t\t\t\t")}";
            return name;
        }
        #endregion

        #region Default Mods
        private class DefaultProperties : ItemMod
        {
            public string name;
            public string description;
            public string finalName;
            public ulong skinId;
        }
        #endregion

        #region Converters
        public class ItemAmountConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }
                ItemAmount itemAmount = (ItemAmount)value;
                writer.WriteStartObject();
                writer.WritePropertyName("ShortName");
                writer.WriteValue(itemAmount.itemDef?.shortname);
                writer.WritePropertyName("Amount");
                writer.WriteValue(itemAmount.amount);
                writer.WriteEndObject();
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(ItemAmount);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                    return null;

                JObject jobject = JObject.Load(reader);
                ItemDefinition itemDefinition;
                string shortname = (string)jobject["ShortName"];
                if (string.IsNullOrEmpty(shortname))
                    itemDefinition = null;
                else
                    itemDefinition = ItemManager.FindItemDefinition(shortname);
                return new ItemAmount(itemDefinition, (float)jobject["Amount"]);
            }

            public override bool CanRead
            {
                get
                {
                    return true;
                }
            }
        }
        #endregion

        #region Harmony Patches
        internal static class Patches
        {
  
#if CARBON
            internal static void UnloadPlugin(RustPlugin plugin)
            {
                if (CustomDefinitions.ContainsKey(plugin))
                    UnloadPluginItemDefinitions(plugin);
            }
#else
            internal static void UnloadPlugin(string name)
            {
                Plugin plugin = Oxide.Core.Interface.Oxide.RootPluginManager.GetPlugin(name);
                if (plugin == null || (plugin.IsCorePlugin && !Oxide.Core.Interface.Oxide.IsShuttingDown))
                    return;
                if (CustomDefinitions.ContainsKey(plugin))
                    UnloadPluginItemDefinitions(plugin);
            }
#endif

            internal static void Item_Load1(Item __instance, ProtoBuf.Item __0)
            {
                if (__0.itemid == FallbackItemDefinition.itemid &&
                    __0.text != null &&
                    int.TryParse(__0.text, out int restoreItemId) &&
                    ItemManager.itemDictionary.ContainsKey(restoreItemId))
                    __0.itemid = restoreItemId;
            }

            internal static void Item_Load2(Item __instance, ProtoBuf.Item __0)
            {
                if (__instance.info == null)
                    OnItemDefinitionBroken(__instance, __0);
            }

            internal static void VendingMachineMapMarker_GetAppMarkerData(ref ProtoBuf.AppMarker __result)
            {
                if (__result.sellOrders != null)
                    Mutate.ToClientSide(__result.sellOrders);
            }

            internal static void StorageMonitor_FillEntityPayload(ref ProtoBuf.AppEntityPayload payload)
            {
                if (payload.items != null)
                    Mutate.ToClientSide(payload.items);
            }

            internal static bool PlayerUpdateLoot_WriteToStream(ProtoBuf.PlayerUpdateLoot __instance)
            {
                if (__instance.containers != null)
                    Mutate.ToClientSide(__instance.containers);
                return true;
            }

            internal static bool UpdateItem_WriteToStream(ProtoBuf.UpdateItem __instance)
            {
                if (__instance.item != null)
                    Mutate.ToClientSide(__instance.item);
                return true;
            }

            internal static bool UpdateItemContainer_WriteToStream(ProtoBuf.UpdateItemContainer __instance)
            {
                if (__instance.container != null)
                    Mutate.ToClientSide(__instance.container);
                return true;
            }

            internal static bool SellOrderContainer_WriteToStream(ref ProtoBuf.VendingMachine.SellOrderContainer __instance)
            {
                if (__instance.sellOrders != null)
                {
                    __instance = __instance.Copy();
                    Mutate.ToClientSide(__instance.sellOrders);
                }
                return true;
            }

            internal static bool ItemAmountList_WriteToStream(ProtoBuf.ItemAmountList __instance)
            {
                if (__instance.itemID != null)
                    Mutate.ToClientSide(__instance);
                return true;
            }

            internal static bool ItemFilterList_WriteToStream(ProtoBuf.IndustrialConveyor.ItemFilterList __instance)
            {
                if (__instance.filters != null)
                    Mutate.ToClientSide(__instance.filters);
                return true;
            }

            internal static bool IndustrialConveyorTransfer_WriteToStream(ProtoBuf.IndustrialConveyorTransfer __instance)
            {
                if (__instance.ItemTransfers != null)
                    Mutate.ToClientSide(__instance.ItemTransfers);
                return true;
            }

            internal static void CuiItemIconPatch(CuiImageComponent __instance, ref int __result)
            {
                ItemDefinition itemDef = ItemManager.FindItemDefinition(__result);
                if (itemDef == null)
                    return;

                if (IsValidCustomItemDefinition(itemDef) && itemDef.TryGetComponent(out DefaultProperties defaultProperties))
                {
                    __result = itemDef.Parent.itemid;
                    if (__instance.SkinId == 0)
                        __instance.SkinId = defaultProperties.skinId;
                }
            }
        }
        #endregion
    }
}

#region Extensions
namespace Oxide.Plugins.CustomItemDefinitionExtensions
{
    public static class ItemModExtensions
    {
        public static void CopyFields(this ItemMod from, ItemMod to)
        {
            if ((from as object) == null || (to as object) == null)
                return;

            Type type = from.GetType();
            do
            {
                foreach (FieldInfo field in from.GetType().GetFields(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetField))
                    field.SetValue(to, field.GetValue(from));

                if (type == typeof(ItemMod))
                    break;

                type = type.BaseType;
            }
            while (type != null);
        }
    }
}
#endregion