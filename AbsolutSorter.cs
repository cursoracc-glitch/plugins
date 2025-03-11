using Facepunch;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AbsolutSorter", "k1lly0u", "2.0.3")]
    [Description("Sort items from your inventory into designated storage containers with the click of a button")]
    class AbsolutSorter : RustPlugin
    {
        #region Fields
        private StoredData storedData;
        private DynamicConfigFile data;

        [PluginReference] Plugin NoEscape, SkinBox;
        
        private ItemCategory[] itemCategories;

        private bool wipeDetected = false;

        private List<ulong> hiddenPlayers = new List<ulong>();

        private const string SORTING_UI = "asui.sorting";

        private const string PERMISSION_ALLOW = "absolutsorter.allow";
        private const string PERMISSION_LOOTALL = "absolutsorter.lootall";
        private const string PERMISSION_ALLOWDUMP = "absolutsorter.dumpall";

        private const string BG_COLOR = "0 0 0 0";
        private const string BUTTON_COLOR = "1 1 1 0.2";
        private const string BUTTON_SELECTED_COLOR = "0.2 0.8 0.2 0.9";
        private const string BUTTON_EXIT_COLOR = "0.807 0.258 0.168 0.9";

        private const int ALLOWED_CATEGORIES = 79871;

        private enum SortMode { Arrange, This, Nearby, DumpAll, LootAll }
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("absolutsorter");

            permission.RegisterPermission(PERMISSION_ALLOW, this);
            permission.RegisterPermission(PERMISSION_ALLOWDUMP, this);
            permission.RegisterPermission(PERMISSION_LOOTALL, this);

            lang.RegisterMessages(Messages, this);

            itemCategories = (ItemCategory[])Enum.GetValues(typeof(ItemCategory));
        }

        private void OnServerInitialized()
        {
            LoadData();

            if (wipeDetected)
            {
                storedData.boxes.Clear();
                SaveData();
            }

            BroadcastInformation();
        }

        private void OnNewSave(string filename) => wipeDetected = true;

        private void OnServerSave() => SaveData();

        private void OnPlayerDisconnected(BasePlayer player) => CuiHelper.DestroyUi(player, SORTING_UI);

        private void OnPlayerRespawned(BasePlayer player) => CuiHelper.DestroyUi(player, SORTING_UI);

        private void OnLootEntity(BasePlayer player, StorageContainer container)
        {
            if (player == null || !container.IsValid())
                return;

            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ALLOW))
                return;

            if (!configData.AllowedBoxes.Contains(container.ShortPrefabName))
                return;

            if (configData.RequirePrivilege && !player.IsBuildingAuthed())
                return;
            
            if (NoEscape && (bool)NoEscape.Call("IsRaidBlocked", player))
                return;

            NextTick(() =>
            {
                if (SkinBox)
                {
                    object isSkinBoxPlayer = SkinBox.Call("IsSkinBoxPlayer", player.userID);
                    if (isSkinBoxPlayer is bool && (bool)isSkinBoxPlayer)
                        return;
                }

                CreateSortingPanel(player, container.net.ID);
            });
        }

        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity) => CuiHelper.DestroyUi(player, SORTING_UI);        

        private void Unload()
        {
            if (!ServerMgr.Instance.Restarting)
                SaveData();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, SORTING_UI);
        }
        #endregion

        #region Functions
        private void BroadcastInformation()
        {
            if (configData.NotificationInterval == 0)
                return;

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                SendReply(player, msg("Global.Notification", player.userID));

            timer.In(configData.NotificationInterval, BroadcastInformation);
        }

        private void ArrangeSort(BasePlayer player, uint id)
        {
            StorageContainer container = player.inventory?.loot?.entitySource as StorageContainer;
            if (container == null)
                return;

            StoredData.BoxData data;
            if (storedData.IsSortingBox(id, out data))
            {
                List<Item> items = Pool.GetList<Item>();
                items.AddRange(container.inventory.itemList);

                for (int i = 0; i < items.Count; i++)
                    items[i].RemoveFromContainer();

                container.inventory.itemList.Clear();

                items.Sort(delegate (Item a, Item b)
                {
                    bool hasCategoryA = data.HasCategory(a.info.category);
                    bool hasCatagoryB = data.HasCategory(b.info.category);

                    if (hasCategoryA && !hasCatagoryB)
                        return -1;
                    if (!hasCategoryA && hasCatagoryB)
                        return 1;
                    if (hasCategoryA && hasCatagoryB)
                        return 0;
                    return 1;
                });

                for (int i = items.Count - 1; i >= 0; i--)
                    items[i].MoveToContainer(container.inventory, i);

                Pool.FreeList(ref items);
            }
        }

        private void ThisSort(BasePlayer player, uint id)
        {
            StorageContainer container = player.inventory?.loot?.entitySource as StorageContainer;
            if (container == null)
                return;

            if (container.inventory.itemList.Count == container.inventory.capacity)
                return;

            List<Item> items = GetPlayerItems(player);
            if (items.Count > 0)
            {
                StoredData.BoxData data;
                if (storedData.IsSortingBox(container.net.ID, out data))
                {
                    for (int y = items.Count - 1; y >= 0; y--)
                    {
                        Item item = items[y];

                        if (data.AcceptsItem(item.info) && item.MoveToContainer(container.inventory))
                            items.Remove(item);
                    }
                }

            }
            Pool.FreeList(ref items);
        }

        private void NearbySort(BasePlayer player, uint id)
        {
            List<Item> items = GetPlayerItems(player);

            if (items.Count > 0)
            {
                List<StorageContainer> list = Pool.GetList<StorageContainer>();

                Vis.Entities(player.transform.position, configData.SortingRadius, list);

                list.Sort(delegate (StorageContainer a, StorageContainer b)
                {
                    return Vector3Ex.Distance2D(player.transform.position, a.transform.position).CompareTo(Vector3Ex.Distance2D(player.transform.position, b.transform.position));
                }); 

                for (int i = 0; i < list.Count; i++)
                {
                    StorageContainer container = list[i];
                   
                    if (container.inventory.itemList.Count == container.inventory.capacity)
                        continue;

                    StoredData.BoxData data;
                    if (storedData.IsSortingBox(container.net.ID, out data))
                    {                        
                        for (int y = items.Count - 1; y >= 0; y--)
                        {
                            Item item = items[y];

                            if (data.AcceptsItem(item.info) && item.MoveToContainer(container.inventory))
                                items.Remove(item);
                        }
                    }

                    if (items.Count == 0)
                        break;
                }
                Pool.FreeList(ref list);
            }
            Pool.FreeList(ref items);
        }

        private void DumpAll(BasePlayer player)
        {
            List<Item> items = GetPlayerItems(player);
            if (items.Count > 0)
            {
                StorageContainer container = player.inventory?.loot?.entitySource as StorageContainer;
                if (container != null)
                {
                    for (int i = items.Count - 1; i >= 0; i--)
                    {
                        items[i].MoveToContainer(container.inventory);
                    }
                }
            }
            Pool.FreeList(ref items);
        }

        private void LootAll(BasePlayer player)
        {
            StorageContainer container = player.inventory?.loot?.entitySource as StorageContainer;
            if (container != null)
            {
                List<Item> items = container.inventory.itemList;

                for (int i = items.Count - 1; i >= 0; i--)
                {
                    Item item = items[i];
                    if (!item.MoveToContainer(player.inventory.containerMain))
                    {
                        if (!item.MoveToContainer(player.inventory.containerBelt))
                        {
                            item.MoveToContainer(player.inventory.containerWear);
                        }
                    }
                }
            }
        }

        private List<Item> GetPlayerItems(BasePlayer player)
        {
            List<Item> items = Pool.GetList<Item>();

            items.AddRange(player.inventory.containerMain.itemList);

            if (configData.IncludeBelt)
                items.AddRange(player.inventory.containerBelt.itemList);

            return items;
        }

        private bool IsHidden(BasePlayer player) => hiddenPlayers.Contains(player.userID);
        #endregion

        #region UI         
        public static class UI
        {
            public static CuiElementContainer Container(string panelName, string color, UI4 dimensions, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName.ToString()
                    }
                };
                return container;
            }

            public static void Panel(ref CuiElementContainer container, string panel, string color, UI4 dimensions, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    CursorEnabled = cursor
                },
                panel.ToString());
            }

            public static void Label(ref CuiElementContainer container, string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel.ToString());

            }

            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel.ToString());
            }
            
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }

        public class UI4
        {
            public float xMin, yMin, xMax, yMax;

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

        #region UI Creation
        private void CreateSortingPanel(BasePlayer player, uint id)
        {
            CuiElementContainer container = null;

            if (!IsHidden(player))
            {
                container = UI.Container(SORTING_UI, BG_COLOR, new UI4(0.646f, 0.01f, 0.831f, 0.14f), true);

                StoredData.BoxData data;
                storedData.boxes.TryGetValue(id, out data);

                UI.Button(ref container, SORTING_UI, BUTTON_COLOR, msg("UI.SelectCategories", player.userID), 10, new UI4(0f, 0.824f, 0.49f, 1f), $"asui.opencategories {id}");

                if (data?.HasAnyCategory() ?? false)
                    UI.Button(ref container, SORTING_UI, BUTTON_COLOR, msg("UI.SelectItems", player.userID), 10, new UI4(0.51f, 0.824f, 1f, 1f), $"asui.openitems {id}");

                UI.Label(ref container, SORTING_UI, msg("UI.SortingOptions", player.userID), 10, new UI4(0f, 0.624f, 1f, 0.8f), TextAnchor.MiddleLeft);

                UI.Button(ref container, SORTING_UI, BUTTON_COLOR, msg("UI.Sort.This", player.userID), 10, new UI4(0f, 0.448f, 0.32f, 0.624f), $"asui.sort 1 {id}");
                UI.Button(ref container, SORTING_UI, BUTTON_COLOR, msg("UI.Sort.Nearby", player.userID), 10, new UI4(0.34f, 0.448f, 0.66f, 0.624f), $"asui.sort 2 {id}");

                if (data != null)
                    UI.Button(ref container, SORTING_UI, BUTTON_COLOR, msg("UI.Sort.Arrange", player.userID), 10, new UI4(0.68f, 0.448f, 1f, 0.624f), $"asui.sort 0 {id}");

                if (permission.UserHasPermission(player.UserIDString, PERMISSION_ALLOWDUMP))
                    UI.Button(ref container, SORTING_UI, BUTTON_COLOR, msg("UI.Sort.Dump", player.userID), 10, new UI4(0f, 0.234f, 0.32f, 0.41f), $"asui.sort 3 {id}");

                if (permission.UserHasPermission(player.UserIDString, PERMISSION_LOOTALL) && (player.inventory?.loot?.entitySource as StorageContainer)?.inventory?.itemList?.Count > 0)
                    UI.Button(ref container, SORTING_UI, BUTTON_COLOR, msg("UI.Sort.Take", player.userID), 10, new UI4(0.34f, 0.234f, 0.66f, 0.41f), $"asui.sort 4 {id}");

                UI.Button(ref container, SORTING_UI, BUTTON_SELECTED_COLOR, msg("UI.Sort.Help", player.userID), 10, new UI4(0.68f, 0.234f, 1f, 0.41f), "asui.help");

                if (data != null)
                    UI.Button(ref container, SORTING_UI, BUTTON_EXIT_COLOR, msg("UI.Sort.Remove", player.userID), 10, new UI4(0f, 0.02f, 0.66f, 0.196f), $"asui.destroy {id}");

                UI.Button(ref container, SORTING_UI, BUTTON_EXIT_COLOR, msg("UI.Sort.Hide", player.userID), 10, new UI4(0.68f, 0.02f, 1f, 0.196f), $"asui.hide {id}");
            }
            else
            {
                container = UI.Container(SORTING_UI, BG_COLOR, new UI4(0.7718f, 0.0126f, 0.831f, 0.03548f), true);

                UI.Button(ref container, SORTING_UI, BUTTON_EXIT_COLOR, msg("UI.Sort.Show", player.userID), 10, new UI4(0, 0, 1, 1), $"asui.hide {id}");
            }

            CuiHelper.DestroyUi(player, SORTING_UI);
            CuiHelper.AddUi(player, container);
        }

        private void SelectCategories(BasePlayer player, uint id)
        {
            CuiHelper.DestroyUi(player, SORTING_UI);

            StoredData.BoxData data;
            storedData.IsSortingBox(id, out data);

            CuiElementContainer container = UI.Container(SORTING_UI, BG_COLOR, new UI4(0.646f, 0.01f, 0.831f, 0.14f), true);

            int count = 0;

            for (int i = 0; i < itemCategories.Length; i++)
            {
                ItemCategory category = itemCategories[i];

                if ((ALLOWED_CATEGORIES & (1 << (int)category)) == 0)
                    continue;

                UI.Button(ref container, SORTING_UI, data?.HasCategory(category) ?? false ? BUTTON_SELECTED_COLOR : BUTTON_COLOR, category.ToString(), 10, GetPosition(count), $"asui.selectcategory {id} {(int)category}");
                count++;
            }

            UI.Button(ref container, SORTING_UI, BUTTON_EXIT_COLOR, msg("UI.Return", player.userID), 10, GetPosition(14), $"asui.mainmenu {id}");

            CuiHelper.AddUi(player, container);
        }

        private void SelectItems(BasePlayer player, uint id, int page = 0)
        {
            CuiHelper.DestroyUi(player, SORTING_UI);

            StoredData.BoxData data;
            if (storedData.IsSortingBox(id, out data))
            {
                CuiElementContainer container = UI.Container(SORTING_UI, BG_COLOR, new UI4(0.646f, 0.01f, 0.831f, 0.14f), true);

                ItemDefinition[] definitions = ItemManager.itemList.Where(x => data.HasCategory(x.category)).ToArray();

                int index = page * 14;

                int count = 0;

                for (int i = index; i < Mathf.Min(index + 14, definitions.Length); i++)
                {
                    ItemDefinition itemDefinition = definitions[i];

                    UI.Button(ref container, SORTING_UI, data.HasItem(itemDefinition.shortname) ? BUTTON_SELECTED_COLOR : BUTTON_COLOR, itemDefinition.displayName.english, 10, GetPosition(count), $"asui.selectitem {id} {itemDefinition.shortname} {page}");
                    count++;
                }

                UI4 ui4 = GetPosition(14);
                float width = (ui4.xMax - ui4.xMin) / 3f;
                if (page > 0)
                    UI.Button(ref container, SORTING_UI, BUTTON_COLOR, "<<", 10, new UI4(ui4.xMin, ui4.yMin, ui4.xMin + width - 0.015f, ui4.yMax), $"asui.selectitempage {id} {page - 1}");
                if (index + 14 < definitions.Length - 1)
                    UI.Button(ref container, SORTING_UI, BUTTON_COLOR, ">>", 10, new UI4(ui4.xMin + width + 0.005f, ui4.yMin, ui4.xMin + (width * 2 - 0.005f), ui4.yMax), $"asui.selectitempage {id} {page + 1}");

                UI.Button(ref container, SORTING_UI, BUTTON_EXIT_COLOR, "X", 10, new UI4(ui4.xMin + (width * 2f) + 0.015f, ui4.yMin, ui4.xMax, ui4.yMax), $"asui.mainmenu {id}");

                CuiHelper.AddUi(player, container);
            }
        }
        #endregion

        #region UI Functions
        private UI4 GetPosition(int number, int columns = 5, float xOffset = 0f, float width = 0.32f, float yOffset = 1f, float height = 0.175f, float horizontalSpacing = 0.02f, float verticalSpacing = 0.03125f)
        {
            int columnNumber = number == 0 ? 0 : ColumnNumber(columns, number);
            int rowNumber = number - (columnNumber * columns);

            float offsetX = xOffset + (width * columnNumber) + (horizontalSpacing * columnNumber);

            float offsetY = (yOffset - (rowNumber * height) - (verticalSpacing * rowNumber));

            return new UI4(offsetX, offsetY - height, offsetX + width, offsetY);
        }

        private int ColumnNumber(int max, int count) => Mathf.FloorToInt(count / max);
        #endregion

        #region UI Commands
        [ConsoleCommand("asui.mainmenu")]
        private void ccmdMainmenu(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            uint id = arg.GetUInt(0);

            CreateSortingPanel(player, id);
        }

        [ConsoleCommand("asui.opencategories")]
        private void ccmdOpenCategories(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            uint id = arg.GetUInt(0);

            SelectCategories(player, id);
        }

        [ConsoleCommand("asui.openitems")]
        private void ccmdOpenItems(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            uint id = arg.GetUInt(0);

            SelectItems(player, id);
        }

        [ConsoleCommand("asui.selectcategory")]
        private void ccmdSelectCategory(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            uint id = arg.GetUInt(0);

            ItemCategory category = (ItemCategory)arg.GetInt(1);

            StoredData.BoxData data;

            if (!storedData.IsSortingBox(id, out data))
                storedData.boxes[id] = data = new StoredData.BoxData();

            if (data.HasCategory(category))
                data.RemoveCategory(category);
            else data.AddCategory(category);

            SelectCategories(player, id);
        }

        [ConsoleCommand("asui.selectitem")]
        private void ccmdSelectItem(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            uint id = arg.GetUInt(0);

            string shortname = arg.GetString(1);

            int page = arg.GetInt(2);

            StoredData.BoxData data;

            if (storedData.IsSortingBox(id, out data))
            {
                if (data.HasItem(shortname))
                    data.RemoveItem(shortname);
                else data.AddItem(shortname);

                SelectItems(player, id, page);
            }
        }

        [ConsoleCommand("asui.selectitempage")]
        private void ccmdSelectItemPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            uint id = arg.GetUInt(0);

            int page = arg.GetInt(1);

            SelectItems(player, id, page);
        }
        
        [ConsoleCommand("asui.sort")]
        private void ccmdSort(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            SortMode sortMode = (SortMode)arg.GetInt(0);

            uint id = arg.GetUInt(1);
            switch (sortMode)
            {
                case SortMode.Arrange:
                    ArrangeSort(player, id);
                    break;
                case SortMode.This:
                    ThisSort(player, id);
                    break;
                case SortMode.Nearby:
                    NearbySort(player, id);
                    break;
                case SortMode.DumpAll:
                    DumpAll(player);
                    break;
                case SortMode.LootAll:
                    LootAll(player);
                    break;
                default:
                    break;
            }

            CreateSortingPanel(player, id);
        }

        [ConsoleCommand("asui.destroy")]
        private void ccmdDestroy(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            uint id = arg.GetUInt(0);

            storedData.RemoveSortingBox(id);

            CreateSortingPanel(player, id);
        }

        [ConsoleCommand("asui.hide")]
        private void ccmdHide(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            if (IsHidden(player))
                hiddenPlayers.Remove(player.userID);
            else hiddenPlayers.Add(player.userID);

            uint id = arg.GetUInt(0);

            CreateSortingPanel(player, id);
        }

        [ConsoleCommand("asui.help")]
        private void ccmdHelp(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            player.inventory.loot.Clear();

            SendHelpText(player);
        }
        #endregion

        #region Commands
        [ChatCommand("sorthelp")]
        private void cmdSortHelp(BasePlayer player, string command, string[] args) => SendHelpText(player);

        private void SendHelpText(BasePlayer player)
        {
            SendReply(player, $"<color=#ce422b>{Title}</color> v<color=#ce422b>{Version}</color>\nYou can view this help at anytime by typing <color=#ce422b>/sorthelp</color>");

            string str = msg("Help.Options.1", player.userID);
            str += msg("Help.Options.2", player.userID);
            str += msg("Help.Options.3", player.userID);
            str += msg("Help.Options.4", player.userID);
            str += msg("Help.Options.5", player.userID);
            SendReply(player, str);

            str = msg("Help.Sorting.1", player.userID);
            str += msg("Help.Sorting.2", player.userID);
            str += string.Format(msg("Help.Sorting.3", player.userID), configData.SortingRadius);
            str += msg("Help.Sorting.4", player.userID);
            str += msg("Help.Sorting.5", player.userID);
            str += msg("Help.Sorting.6", player.userID);
            str += msg("Help.Sorting.7", player.userID);
            SendReply(player, str);
        }
        #endregion
        
        #region Config        
        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Allowed containers (short prefab name)")]
            public string[] AllowedBoxes { get; set; }

            [JsonProperty(PropertyName = "Sorting radius")]
            public float SortingRadius { get; set; }

            [JsonProperty(PropertyName = "Include hotbar items when sorting")]
            public bool IncludeBelt { get; set; }

            [JsonProperty(PropertyName = "Require building privilege to use sorting")]
            public bool RequirePrivilege { get; set; }

            [JsonProperty(PropertyName = "Help notification interval (seconds, set to 0 to disable)")]
            public int NotificationInterval { get; set; }

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
                AllowedBoxes = new string[]
                {
                    "campfire",
                    "furnace",
                    "woodbox_deployed",
                    "box.wooden.large",
                    "small_stash_deployed",
                    "fridge.deployed",
                    "coffinstorage"
                },
                IncludeBelt = false,
                RequirePrivilege = true,
                SortingRadius = 30f,
                NotificationInterval = 1800,
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(2, 0, 0))
                configData = baseConfig;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveData() => data.WriteObject(storedData);

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
        }

        internal class StoredData
        {
            public Hash<uint, BoxData> boxes = new Hash<uint, BoxData>();

            public BoxData CreateSortingBox(uint id)
            {
                BoxData data = new BoxData();

                boxes.Add(id, data);

                return data;
            }

            public bool IsSortingBox(uint id, out BoxData data) => boxes.TryGetValue(id, out data);
            
            public void RemoveSortingBox(uint id) => boxes.Remove(id);

            public class BoxData
            {
                public int categories;

                public List<string> allowedItems = new List<string>();

                public void AddCategory(ItemCategory category)
                {
                    categories |= (1 << (int)category);
                }

                public void RemoveCategory(ItemCategory category)
                {
                    categories &= ~(1 << (int)category);

                    if (allowedItems.Count > 0)
                    {
                        for (int i = allowedItems.Count - 1; i >= 0; i--)
                        {
                            ItemDefinition itemDefinition = ItemManager.FindItemDefinition(allowedItems[i]);
                            if (itemDefinition != null && itemDefinition.category == category)
                                allowedItems.RemoveAt(i);
                        }
                    }
                }

                public bool HasCategory(ItemCategory category)
                {
                    return ((categories & (1 << (int)category)) != 0);
                }

                public bool HasAnyCategory() => categories != 0;

                public bool AcceptsItem(ItemDefinition itemDefinition)
                {
                    if (allowedItems.Contains(itemDefinition.shortname) && HasCategory(itemDefinition.category))
                        return true;

                    if (allowedItems.Count == 0 && HasCategory(itemDefinition.category))
                        return true;

                    return false;
                }

                public void AddItem(string shortname) => allowedItems.Add(shortname);

                public bool HasItem(string shortname) => allowedItems.Contains(shortname);

                public void RemoveItem(string shortname) => allowedItems.Remove(shortname);
            }
        }
        #endregion

        #region Localization
        private string msg(string key, ulong playerId = 0UL) => lang.GetMessage(key, this, playerId != 0UL ? playerId.ToString() : null);

        private Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Help.Options.1"] = "<color=#ce422b>Sorting Preferences</color>;\n\n",
            ["Help.Options.2"] = "To begin, press the 'Select Categories' button to select which item categories are allowed in this box\n\n",
            ["Help.Options.3"] = "By selecting categories only all items in those categories will be accepted\n\n",
            ["Help.Options.4"] = "To pick individual items press the 'Select Items' button and pick the items from the list\n\n",
            ["Help.Options.5"] = "When using the 'This' or 'Nearby' functions the boxes will only take items that meet these requirements",
            ["Help.Sorting.1"] = "<color=#ce422b>Sorting Options</color>;\n\n",
            ["Help.Sorting.2"] = "<color=#ce422b>This</color> - Will move items from your inventory to the box if the item meets the set requirements\n\n",
            ["Help.Sorting.3"] = "<color=#ce422b>Nearby</color> - Will move items from your inventory to boxes to boxes within a {0} radius if the item meets the set requirements\n\n",
            ["Help.Sorting.4"] = "<color=#ce422b>Arrange Box</color> - Will arrange all items in the box with the selected categories/items listed first\n\n",
            ["Help.Sorting.5"] = "<color=#ce422b>Dump All</color> - Empty your inventory into the box regardless of the accepted items\n\n",
            ["Help.Sorting.6"] = "<color=#ce422b>Loot All</color> - Empty the box into your inventory\n\n",
            ["Help.Sorting.7"] = "<color=#ce422b>Remove sorter from container</color> - Removes all sorting settings from this box",
            ["UI.SelectCategories"] = "Select Categories",
            ["UI.SelectItems"] = "Select Items",
            ["UI.SortingOptions"] = "Sorting Options",
            ["UI.Sort.This"] = "This",
            ["UI.Sort.Nearby"] = "Nearby",
            ["UI.Sort.Arrange"] = "Arrange Box",
            ["UI.Sort.Dump"] = "Dump All",
            ["UI.Sort.Take"] = "Loot All",
            ["UI.Sort.Help"] = "Help",
            ["UI.Sort.Remove"] = "Remove Sorter From Container",
            ["UI.Sort.Hide"] = "Hide",
            ["UI.Sort.Show"] = "Show",
            ["UI.Return"] = "Return",
            ["Global.Notification"] = "This server is running <color=#ce422b>AbsolutSorter</color>! Type <color=#ce422b>/sorthelp</color> for information on how to use it",
        };
        #endregion
    }
}
