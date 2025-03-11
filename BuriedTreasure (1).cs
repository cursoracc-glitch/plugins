using System;
using Rust;
using Oxide.Core;
using Newtonsoft.Json;
using Oxide.Core.Configuration;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("BuriedTreasure", "Colon Blow", "1.0.10")]
	[Description("Куплено на Oxide Russia")]
	
    class BuriedTreasure : RustPlugin
    {

        // Test fix for possible conflict with plugins using the CanNetworkTo

        #region Load

        [PluginReference] Plugin ServerRewards;
        [PluginReference] Plugin Economics;

        void Loaded()
        {
            permission.RegisterPermission("buriedtreasure.admin", this);
        }

        #endregion

        #region Configuration

        private static PluginConfig config;

        private class PluginConfig
        {
            public GlobalSettings globalSettings { get; set; }

            public class GlobalSettings
            {
                [JsonProperty(PropertyName = "Gold - Enable gold to be sold for Server Reward Points ? ")] public bool UseServerRewards { get; set; }
                [JsonProperty(PropertyName = "Gold - Enable gold to be sold for Economics Bucks ? ")] public bool UseEconomics { get; set; }
                [JsonProperty(PropertyName = "Gold - Player will get this many Server Reward Points when selling 1 gold : ")] public int ServerRewardsGoldExhcange { get; set; }
                [JsonProperty(PropertyName = "Gold - Player will get this many Economics Bucks when selling 1 gold : ")] public int EconomicsGoldExchange { get; set; }

                [JsonProperty(PropertyName = "AutoLoot - Automatically turn in gold coins for rewards when looting ? ")] public bool EnableAutoGoldRewardOnLoot { get; set; }
                [JsonProperty(PropertyName = "AutoLoot - Automatically mark treasure maps when they are looted ? ")] public bool EnableAutoReadMapOnLoot { get; set; }

                [JsonProperty(PropertyName = "Standard Loot - Enable chance for random treasure map in standard loot crates ? ")] public bool EnableMapsInStandardLoot { get; set; }
                [JsonProperty(PropertyName = "Standard Loot - Enable chance for gold to spawn in standard loot crates ? ")] public bool EnableGoldInStandardLoot { get; set; }
                [JsonProperty(PropertyName = "Standard Loot - Random Treasure Map chance (if enabled) : ")] public int StandardLootAddMapChance { get; set; }
                [JsonProperty(PropertyName = "Standard Loot - Gold spawn chance (if enabled) : ")] public int StandardLootAddGoldChance { get; set; }

                [JsonProperty(PropertyName = "Treasure - Spawn - Only spawn Treasure up to this far from players current postion : ")] public float LocalTreasureMaxDistance { get; set; }
                [JsonProperty(PropertyName = "Treasure - Spawn - Use whole map (instead of distance from player) to get random spawn point ? ")] public bool UseWholeMapSpawn { get; set; }
                [JsonProperty(PropertyName = "Treasure - Spawn - When whole map size is used, reduce spawn area by this much offset (closer to land) : ")] public float WholeMapOffset { get; set; }
                [JsonProperty(PropertyName = "Treasure - Despawn - Approx Seconds the Treasure Marker and Location will despawn if not found : ")] public float DespawnTime { get; set; }
                [JsonProperty(PropertyName = "Treasure - Despawn - Approx Seconds the Spawned Chest will despawn if not looted : ")] public float TreasureDespawnTime { get; set; }
                [JsonProperty(PropertyName = "Treasure - Location - When player gets within this distance, treasure will spawn nearby : ")] public float LootDetectionRadius { get; set; }

                [JsonProperty(PropertyName = "Treasure - Chance - to add a Random Map to Treasure Chest : ")] public int AddMapChance { get; set; }
                [JsonProperty(PropertyName = "Treasure - Chance - to add a Gold to Treasure Chest : ")] public int AddGoldChance { get; set; }

                [JsonProperty(PropertyName = "Treasure - Chance - When a random map is added to chest or spawned, chance it will be a Basic Map: ")] public int BasicMapChance { get; set; }
                [JsonProperty(PropertyName = "Treasure - Chance - When a random map is added to chest or spawned, chance it will be a UnCommon Map: ")] public int UnCommonMapChance { get; set; }
                [JsonProperty(PropertyName = "Treasure - Chance - When a random map is added to chest or spawned, chance it will be a Rare Map: ")] public int RareMapChance { get; set; }
                [JsonProperty(PropertyName = "Treasure - Chance - When a random map is added to chest or spawned, chance it will be a Elite Map: ")] public int EliteMapChance { get; set; }

                [JsonProperty(PropertyName = "Map Marker - Prefab - Treasure Chest Map marker prefab (default explosion marker) : ")] public string MapMarkerPrefab { get; set; }
                [JsonProperty(PropertyName = "Treasure - Prefab - Basic Treasure Chest prefab : ")] public string BasicTreasurePrefab { get; set; }
                [JsonProperty(PropertyName = "Treasure - Prefab - UnCommon Treasure Chest prefab : ")] public string UnCommonTreasurePrefab { get; set; }
                [JsonProperty(PropertyName = "Treasure - Prefab - Rare Treasure Chest prefab : ")] public string RareTreasurePrefab { get; set; }
                [JsonProperty(PropertyName = "Treasure - Prefab - Elite Treasure Chest prefab : ")] public string EliteTreasurePrefab { get; set; }

                [JsonProperty(PropertyName = "Text - Basic Map name when inspecting map in inventory")] public string BasicMapTitle { get; set; }
                [JsonProperty(PropertyName = "Text - Uncommon Map name when inspecting map in inventory")] public string UncommonMapTitle { get; set; }
                [JsonProperty(PropertyName = "Text - Rare Map name when inspecting map in inventory")] public string RareMapTitle { get; set; }
                [JsonProperty(PropertyName = "Text - Elite Map name when inspecting map in inventory")] public string EliteMapTitle { get; set; }
                [JsonProperty(PropertyName = "Text - Notes to player when inspecting map in inventory")] public string MapInfomation { get; set; }

                [JsonProperty(PropertyName = "Loot Table - Only Use Loot Table Items ? ")] public bool UseOnlyLootTable { get; set; }
                [JsonProperty(PropertyName = "Loot Table - Basic Treasure Chest")] public Dictionary<int, int> BasicLootTable { get; set; }
                [JsonProperty(PropertyName = "Loot Table - UnCommon Treasure Chest")] public Dictionary<int, int> UnCommonLootTable { get; set; }
                [JsonProperty(PropertyName = "Loot Table - Rare Treasure Chest")] public Dictionary<int, int> RareLootTable { get; set; }
                [JsonProperty(PropertyName = "Loot Table - Elite Treasure Chest")] public Dictionary<int, int> EliteLootTable { get; set; }
            }

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                globalSettings = new PluginConfig.GlobalSettings
                {
                    UseServerRewards = true,
                    UseEconomics = true,
                    ServerRewardsGoldExhcange = 100,
                    EconomicsGoldExchange = 100,
                    EnableMapsInStandardLoot = false,
                    EnableGoldInStandardLoot = false,
                    EnableAutoGoldRewardOnLoot = false,
                    EnableAutoReadMapOnLoot = false,
                    StandardLootAddMapChance = 1,
                    StandardLootAddGoldChance = 1,
                    LocalTreasureMaxDistance = 100,
                    UseWholeMapSpawn = false,
                    WholeMapOffset = 500f,
                    DespawnTime = 3600f,
                    TreasureDespawnTime = 3600f,
                    LootDetectionRadius = 8f,
                    AddMapChance = 5,
                    AddGoldChance = 5,
                    BasicMapChance = 50,
                    UnCommonMapChance = 30,
                    RareMapChance = 15,
                    EliteMapChance = 5,
                    MapMarkerPrefab = "assets/prefabs/tools/map/cratemarker.prefab",
                    BasicTreasurePrefab = "assets/bundled/prefabs/radtown/crate_basic.prefab",
                    UnCommonTreasurePrefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                    RareTreasurePrefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                    EliteTreasurePrefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",

                    BasicMapTitle = "Basic Map",
                    UncommonMapTitle = "Uncommon Map",
                    RareMapTitle = "Rare Map",
                    EliteMapTitle = "Elite Map",

                    MapInfomation = "Place map in Quick Slot, then right click on it to mark location.",

                    UseOnlyLootTable = false,
                    BasicLootTable = new Dictionary<int, int>() { { -700591459, 1 }, { 1655979682, 5 } },
                    UnCommonLootTable = new Dictionary<int, int>() { { -1941646328, 1 }, { -1557377697, 5 } },
                    RareLootTable = new Dictionary<int, int>() { { -1848736516, 1 }, { 1973684065, 2 }, { -1440987069, 3 } },
                    EliteLootTable = new Dictionary<int, int>() { { 1545779598, 1 }, { -2139580305, 2 }, { 1099314009, 3 } }
                }
            };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Commands

        [ConsoleCommand("buymap")]
        void cmdConsoleBuyMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ConsoleCommand("buyuncommonmap")]
        void cmdConsoleBuyUnCommonMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveUnCommonTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveUnCommonTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ConsoleCommand("buyraremap")]
        void cmdConsoleBuyRareMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveRareTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveRareTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ConsoleCommand("givegold")]
        void cmdConsoleGiveGold(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveGold(player);
                return;
            }
        }

        [ConsoleCommand("buyelitemap")]
        void cmdConsoleBuyEliteMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveEliteTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveEliteTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ConsoleCommand("buyrandommap")]
        void cmdConsoleBuyRandomMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveRandomTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveRandomTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ChatCommand("markmap")]
        void cmdMarkMap(BasePlayer player, string command, string[] args)
        {
            if (!HoldingMap(player, player.GetActiveItem()))
            {
                SendReply(player, "You are not holding a Treasure Map !!");
            }
        }

        [ChatCommand("treasurehelp")]
        void cmdTreasureHelp(BasePlayer player, string command, string[] args)
        {
            string help1 = "/markmap - while holding a treasure map, will mark the location on ingame map.";
            string help2 = "/sellgold - while holding gold, will sell gold for RP or Economics Bucks.";
            string help3 = "Note: You do have to be holding map or gold to use the commands.";

            SendReply(player, " Treasure Map Commands : \n " + help1 + " \n " + help2 + " \n " + help3);
        }

        [ChatCommand("sellgold")]
        void cmdSellGold(BasePlayer player, string command, string[] args)
        {
            SellGold(player);
        }

        #endregion

        #region Hooks

        object CanStackItem(Item item, Item targetItem)
        {
            if (item == null || targetItem == null) return null;
            if (item.skin == 1376561963) return false;
            if (item.skin == 1389950043) return false;
            if (item.skin == 1390209788) return false;
            if (item.skin == 1390210901) return false;
            if (item.skin == 1390211736) return false;
            return null;
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null) return;
            if (input.IsDown(BUTTON.FIRE_SECONDARY))
            {
                if (!HoldingMap(player))
                    SellGold(player);
            }
        }

        void CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot)
        {
            if (item == null || playerLoot == null || targetContainer == null || targetSlot == null) return;

            var thplayer = playerLoot.GetComponentInParent<BasePlayer>() as BasePlayer;
            if (thplayer == null) return;
            if (config.globalSettings.EnableAutoGoldRewardOnLoot && item.skin == 1376561963) { SellGold(thplayer, item); return; }
            if (config.globalSettings.EnableAutoReadMapOnLoot && HoldingMap(thplayer, item)) return;

            if (targetSlot != -1) return;

            var container = playerLoot.FindContainer(targetContainer) ?? null;
            if (container == null || container != playerLoot.containerMain) return;

            if (HoldingMap(thplayer, item)) return;
            SellGold(thplayer, item);
        }

        bool HoldingMap(BasePlayer player, Item item = null)
        {
            Item activeItem;
            if (item != null) activeItem = item;
            else activeItem = player.GetActiveItem();

            if (activeItem != null)
            {
                if (activeItem.skin == 1389950043)
                {
                    activeItem.Remove(0f);
                    BuryTheTreasure(player, 1);
                    return true;
                }
                if (activeItem.skin == 1390209788)
                {
                    activeItem.Remove(0f);
                    BuryTheTreasure(player, 2);
                    return true;
                }
                if (activeItem.skin == 1390210901)
                {
                    activeItem.Remove(0f);
                    BuryTheTreasure(player, 3);
                    return true;
                }
                if (activeItem.skin == 1390211736)
                {
                    activeItem.Remove(0f);
                    BuryTheTreasure(player, 4);
                    return true;
                }
            }
            return false;
        }

        void GiveTreasureMap(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1389950043);
            item.name = config.globalSettings.BasicMapTitle;
            item.text = config.globalSettings.MapInfomation;
            player.inventory.GiveItem(item);
        }

        void GiveUnCommonTreasureMap(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1390209788);
            item.name = config.globalSettings.UncommonMapTitle;
            item.text = config.globalSettings.MapInfomation;
            player.inventory.GiveItem(item);
        }

        void GiveRareTreasureMap(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1390210901);
            item.name = config.globalSettings.RareMapTitle;
            item.text = config.globalSettings.MapInfomation;
            player.inventory.GiveItem(item);
        }

        void GiveEliteTreasureMap(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1390211736);
            item.name = config.globalSettings.EliteMapTitle;
            item.text = config.globalSettings.MapInfomation;
            player.inventory.GiveItem(item);
        }

        void GiveRandomTreasureMap(BasePlayer player)
        {
            ulong skinid = 1389950043;
            var randomroll = UnityEngine.Random.Range(0, (config.globalSettings.BasicMapChance + config.globalSettings.UnCommonMapChance + config.globalSettings.RareMapChance + config.globalSettings.EliteMapChance));
            if (randomroll >= 0 && randomroll <= config.globalSettings.BasicMapChance) skinid = 1389950043;
            if (randomroll >= (config.globalSettings.BasicMapChance + 1) && randomroll <= (config.globalSettings.BasicMapChance + config.globalSettings.UnCommonMapChance)) skinid = 1390209788;
            if (randomroll >= (config.globalSettings.UnCommonMapChance + 1) && randomroll <= (config.globalSettings.UnCommonMapChance + config.globalSettings.RareMapChance)) skinid = 1390210901;
            if (randomroll >= (config.globalSettings.RareMapChance + 1) && randomroll <= (config.globalSettings.RareMapChance + config.globalSettings.EliteMapChance)) skinid = 1390211736;
            var item = ItemManager.CreateByItemID(1414245162, 1, skinid);
            player.inventory.GiveItem(item);
        }

        void GiveContainerRandomTreasureMap(LootContainer container)
        {
            ulong skinid = 1389950043;
            var randomroll = UnityEngine.Random.Range(0, (config.globalSettings.BasicMapChance + config.globalSettings.UnCommonMapChance + config.globalSettings.RareMapChance + config.globalSettings.EliteMapChance));
            if (randomroll >= 0 && randomroll <= config.globalSettings.BasicMapChance) skinid = 1389950043;
            if (randomroll >= (config.globalSettings.BasicMapChance + 1) && randomroll <= (config.globalSettings.BasicMapChance + config.globalSettings.UnCommonMapChance)) skinid = 1390209788;
            if (randomroll >= (config.globalSettings.UnCommonMapChance + 1) && randomroll <= (config.globalSettings.UnCommonMapChance + config.globalSettings.RareMapChance)) skinid = 1390210901;
            if (randomroll >= (config.globalSettings.RareMapChance + 1) && randomroll <= (config.globalSettings.RareMapChance + config.globalSettings.EliteMapChance)) skinid = 1390211736;

            ItemContainer component1 = container.GetComponent<StorageContainer>().inventory;
            Item item = ItemManager.CreateByItemID(1414245162, 1, skinid);
            component1.itemList.Add(item);
            item.parent = component1;
            item.MarkDirty();
        }

        void GiveGold(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1376561963);
            player.inventory.GiveItem(item);
        }

        void GiveContainerGold(LootContainer container)
        {
            ItemContainer component1 = container.GetComponent<StorageContainer>().inventory;
            Item item = ItemManager.CreateByItemID(1414245162, 1, 1376561963);
            component1.itemList.Add(item);
            item.parent = component1;
            item.MarkDirty();
        }

        void SellGold(BasePlayer player, Item item = null)
        {
            Item activeItem = new Item();
            if (item != null) activeItem = item;
            else activeItem = player.GetActiveItem();

            if (activeItem != null)
            {
                if (activeItem.skin == 1376561963)
                {
                    if (config.globalSettings.UseServerRewards && ServerRewards != null)
                    {
                        ServerRewards?.Call("AddPoints", new object[] { player.userID, config.globalSettings.ServerRewardsGoldExhcange });
                        SendReply(player, "You Just sold your gold for " + config.globalSettings.ServerRewardsGoldExhcange.ToString() + " Rewards Points !!!");
                    }
                    if (config.globalSettings.UseEconomics && Economics != null)
                    {
                        Economics?.Call("Deposit", new object[] { player.userID, config.globalSettings.EconomicsGoldExchange });
                        SendReply(player, "You Just sold your gold for " + config.globalSettings.EconomicsGoldExchange.ToString() + " Economic Bucks !!!");
                    }
                    activeItem.Remove(0f);
                    return;
                }
            }
        }

        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, UnityEngine.LayerMask.GetMask("World", "Construction", "Default")))
                return Mathf.Max(hit.point.y, y);

            return y;
        }

        Vector3 GetSpawnLocation(BasePlayer player)
        {
            Vector3 targetPos = new Vector3();
            RaycastHit hitInfo;
            Vector3 randomizer = new Vector3(UnityEngine.Random.Range(-config.globalSettings.LocalTreasureMaxDistance, config.globalSettings.LocalTreasureMaxDistance), 0f, UnityEngine.Random.Range(-config.globalSettings.LocalTreasureMaxDistance, config.globalSettings.LocalTreasureMaxDistance));
            Vector3 newp = (player.transform.position + randomizer);
            var groundy = GetGroundPosition(newp);
            targetPos = new Vector3(newp.x, groundy, newp.z);
            return targetPos;
        }

        Vector3 FindGlobalSpawnPoint()
        {
            Vector3 spawnpoint = new Vector3();
            float mapoffset = config.globalSettings.WholeMapOffset;
            float mapsize = ((ConVar.Server.worldsize) / 2) - mapoffset;
            Vector3 randomizer = new Vector3(UnityEngine.Random.Range(-mapsize, mapsize), 0f, UnityEngine.Random.Range(-mapsize, mapsize));
            Vector3 newp = randomizer;
            var groundy = GetGroundPosition(newp);
            spawnpoint = new Vector3(randomizer.x, groundy, randomizer.z);
            return spawnpoint;
        }

        void BuryTheTreasure(BasePlayer player, int maprarity = 1)
        {
            Vector3 position = GetSpawnLocation(player);
            if (config.globalSettings.UseWholeMapSpawn) position = FindGlobalSpawnPoint();
            GameObject newTreasure = new GameObject();
            newTreasure.transform.position = position;
            var stash = newTreasure.gameObject.AddComponent<BaseEntity>();
            stash.OwnerID = player.userID;
            var addmarker = stash.gameObject.AddComponent<TreasureMarker>();
            addmarker.rarity = maprarity;
            SendReply(player, "Treasure is now marked on ingame map at grid : " + GetGridLocation(position));
        }

        void OnLootSpawn(LootContainer container)
        {
            var getobj = container.GetComponentInParent<BaseEntity>() ?? null;
            if (getobj != null && getobj.skinID == 111) return;
            if (config.globalSettings.EnableMapsInStandardLoot)
            {
                int randomlootroll = UnityEngine.Random.Range(0, 100);
                if (randomlootroll <= config.globalSettings.StandardLootAddMapChance) GiveContainerRandomTreasureMap(container);
            }
            if (config.globalSettings.EnableGoldInStandardLoot)
            {
                int randomgoldlootroll = UnityEngine.Random.Range(0, 100);
                if (randomgoldlootroll <= config.globalSettings.StandardLootAddGoldChance) GiveContainerGold(container);
            }
        }

        string GetGridLocation(Vector3 position)
        {
            Vector2 offsetPos = new Vector2((World.Size / 2 - 6) + position.x, (World.Size / 2 - 56) - position.z);
            string gridstring = $"{Convert.ToChar(65 + (int)offsetPos.x / 146)}{(int)(offsetPos.y / 146)}";
            return gridstring;
        }

        void Unload()
        {
            DestroyAll<TreasureMarker>();
        }

        static void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        #endregion

        #region TreasureMarker 

        object CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            if (entity is MapMarker && entity.name == "Treasure Marker")
            {
                MapMarker getMarker = entity.GetComponent<MapMarker>();
                if (getMarker)
                {
                    if (target.userID == getMarker.OwnerID) return null;
                    else return false;
                }
            }
            return null;
        }

        class TreasureMarker : BaseEntity
        {
            BaseEntity lootbox;
            BaseEntity treasurechest;
            MapMarker mapmarker;
            SphereCollider sphereCollider;
            public ulong playerid;
            BuriedTreasure instance;
            public int rarity;
            string prefabtreasure;
            Dictionary<int, int> loottable;
            bool isvisible;
            bool didspawnchest;
            float despawncounter;
            float detectionradius;

            void Awake()
            {
                instance = new BuriedTreasure();
                lootbox = GetComponentInParent<BaseEntity>();
                playerid = lootbox.OwnerID;
                rarity = 1;
                despawncounter = 0f;
                isvisible = false;
                didspawnchest = false;
                detectionradius = config.globalSettings.LootDetectionRadius;
                string prefabmarker = config.globalSettings.MapMarkerPrefab;

                mapmarker = GameManager.server.CreateEntity(prefabmarker, lootbox.transform.position, Quaternion.identity, true) as MapMarker;
                mapmarker.OwnerID = lootbox.OwnerID;
                mapmarker.name = "Treasure Marker";
                mapmarker.Spawn();

                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.gameObject.layer = (int)Layer.Reserved1;
                sphereCollider.isTrigger = true;
                sphereCollider.radius = detectionradius;
            }

            private void OnTriggerEnter(Collider col)
            {
                if (didspawnchest) return;
                var target = col.GetComponentInParent<BasePlayer>();
                if (target != null)
                {
                    if (target.userID == lootbox.OwnerID)
                    {
                        SpawnTreasureChest();
                        didspawnchest = true;
                        instance.SendReply(target, "The Treasure is very close !!!!");
                    }
                }
            }

            void SpawnTreasureChest()
            {
                if (rarity == 1) prefabtreasure = config.globalSettings.BasicTreasurePrefab;
                if (rarity == 2) prefabtreasure = config.globalSettings.UnCommonTreasurePrefab;
                if (rarity == 3) prefabtreasure = config.globalSettings.RareTreasurePrefab;
                if (rarity == 4) prefabtreasure = config.globalSettings.EliteTreasurePrefab;
                treasurechest = GameManager.server.CreateEntity(prefabtreasure, lootbox.transform.position, Quaternion.identity, true);
                treasurechest.skinID = 111;
                treasurechest.OwnerID = lootbox.OwnerID;
                treasurechest.Spawn();
                treasurechest.gameObject.AddComponent<TreasureDespawner>();

                ItemContainer storageCont = treasurechest.GetComponent<StorageContainer>().inventory;
                storageCont.capacity = 36;
                if (config.globalSettings.UseOnlyLootTable) storageCont.Clear();

                AddLootTableItems(treasurechest);
                CheckForExtras(treasurechest);
                CheckSpawnVisibility(treasurechest);

                lootbox.Invoke("KillMessage", 0.2f);
            }

            void AddLootTableItems(BaseEntity treasurebox)
            {
                if (rarity == 1) loottable = config.globalSettings.BasicLootTable;
                if (rarity == 2) loottable = config.globalSettings.UnCommonLootTable;
                if (rarity == 3) loottable = config.globalSettings.RareLootTable;
                if (rarity == 4) loottable = config.globalSettings.EliteLootTable;

                foreach (KeyValuePair<int, int> lootlist in loottable)
                {
                    int itemqty = lootlist.Value;
                    int itemid = lootlist.Key;
                    ItemContainer component1 = treasurebox.GetComponent<StorageContainer>().inventory;
                    Item item = ItemManager.CreateByItemID(itemid, itemqty, 0);
                    component1.itemList.Add(item);
                    item.parent = component1;
                    item.MarkDirty();
                }
            }

            void CheckSpawnVisibility(BaseEntity entitybox)
            {
                if (isvisible) return;
                if (entitybox.IsOutside()) { isvisible = true; return; }
                entitybox.transform.position = entitybox.transform.position + new Vector3(0f, 0.2f, 0f);
                entitybox.transform.hasChanged = true;
                entitybox.SendNetworkUpdateImmediate();
                CheckSpawnVisibility(entitybox);
            }

            void CheckForExtras(BaseEntity entitybox)
            {
                int randommaproll = UnityEngine.Random.Range(0, 100);
                if (rarity == 2) randommaproll = randommaproll - 2;
                if (rarity == 3) randommaproll = randommaproll - 4;
                if (rarity == 4) randommaproll = randommaproll - 6;
                if (randommaproll > 100) randommaproll = 100;
                if (randommaproll < 0) randommaproll = 0;
                if (randommaproll <= config.globalSettings.AddMapChance) AddRandomMap(entitybox);

                AddRandomGold(entitybox);
            }

            void AddRandomGold(BaseEntity entitybox)
            {
                int randomgoldroll = UnityEngine.Random.Range(0, 100);
                if (rarity == 2) randomgoldroll = randomgoldroll - 2;
                if (rarity == 3) randomgoldroll = randomgoldroll - 4;
                if (rarity == 4) randomgoldroll = randomgoldroll - 6;
                if (randomgoldroll > 100) randomgoldroll = 100;
                if (randomgoldroll < 0) randomgoldroll = 0;
                if (randomgoldroll <= config.globalSettings.AddGoldChance)
                {
                    ItemContainer component1 = entitybox.GetComponent<StorageContainer>().inventory;
                    Item item = ItemManager.CreateByItemID(1414245162, 1, 1376561963);
                    component1.itemList.Add(item);
                    item.parent = component1;
                    item.MarkDirty();
                }
            }

            void AddRandomMap(BaseEntity entitybox)
            {
                ulong skinid = 1389950043;
                var randomroll = UnityEngine.Random.Range(0, (config.globalSettings.BasicMapChance + config.globalSettings.UnCommonMapChance + config.globalSettings.RareMapChance + config.globalSettings.EliteMapChance));
                if (randomroll >= 0 && randomroll <= config.globalSettings.BasicMapChance) skinid = 1389950043;
                if (randomroll >= (config.globalSettings.BasicMapChance + 1) && randomroll <= (config.globalSettings.BasicMapChance + config.globalSettings.UnCommonMapChance)) skinid = 1390209788;
                if (randomroll >= (config.globalSettings.UnCommonMapChance + 1) && randomroll <= (config.globalSettings.UnCommonMapChance + config.globalSettings.RareMapChance)) skinid = 1390210901;
                if (randomroll >= (config.globalSettings.RareMapChance + 1) && randomroll <= (config.globalSettings.RareMapChance + config.globalSettings.EliteMapChance)) skinid = 1390211736;
                ItemContainer component1 = entitybox.GetComponent<StorageContainer>().inventory;
                Item item = ItemManager.CreateByItemID(1414245162, 1, skinid);
                component1.itemList.Add(item);
                item.parent = component1;
                item.MarkDirty();
            }

            void FixedUpdate()
            {
                if (despawncounter >= (config.globalSettings.DespawnTime * 15) && lootbox != null) { lootbox.Invoke("KillMessage", 0.1f); return; }
                despawncounter = despawncounter + 1f;
            }

            void OnDestroy()
            {
                if (mapmarker != null) mapmarker.Invoke("KillMessage", 0.1f);
                if (lootbox != null) lootbox.Invoke("KillMessage", 0.1f);
            }
        }

        #endregion

        #region TreasureDespawner 

        class TreasureDespawner : BaseEntity
        {
            BaseEntity treasure;
            BuriedTreasure instance;
            float despawncounter;

            void Awake()
            {
                instance = new BuriedTreasure();
                treasure = GetComponentInParent<BaseEntity>();
                despawncounter = 0f;
            }

            void FixedUpdate()
            {
                if (despawncounter >= (config.globalSettings.TreasureDespawnTime * 15) && treasure != null) { treasure.Invoke("KillMessage", 0.1f); return; }
                despawncounter = despawncounter + 1f;
            }

            void OnDestroy()
            {
                if (treasure != null) treasure.Invoke("KillMessage", 0.1f);
            }
        }

        #endregion
    }
}