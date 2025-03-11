using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("HomeRecycler", "wazzzup", "1.3.2")]
    [Description("Allows to have Recycler at home")]
    class HomeRecycler : RustPlugin
    {
        [PluginReference]
        Plugin Friends;

        Dictionary<uint, ulong> startedRecyclers = new Dictionary<uint, ulong>();
        public Dictionary<int, KeyValuePair<string, int>> itemsNeededToCraft = new Dictionary<int, KeyValuePair<string, int>>();
        public static HomeRecycler Instance;

        private ItemBlueprint bp;

        private class RecyclerEntity : MonoBehaviour
        {
            private DestroyOnGroundMissing desGround;
            private GroundWatch groundWatch;
            public ulong OwnerID;

            void Awake()
            {
                OwnerID = GetComponent<BaseEntity>().OwnerID;
                desGround = GetComponent<DestroyOnGroundMissing>();
                if (!desGround) gameObject.AddComponent<DestroyOnGroundMissing>();
                groundWatch = GetComponent<GroundWatch>();
                if (!groundWatch) gameObject.AddComponent<GroundWatch>();
            }
        }

        PluginData pluginData;
        class PluginData
        {
            public Dictionary<ulong, double> userCooldowns = new Dictionary<ulong, double>();
            public Dictionary<ulong, int> userSpawned = new Dictionary<ulong, int>();
            public Dictionary<ulong, double> userCooldownsCraft = new Dictionary<ulong, double>();
            public Dictionary<ulong, int> userCrafted = new Dictionary<ulong, int>();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(this.Title, pluginData);
        }

        ConfigData configData;
        class ConfigData
        {
            public string chatCommand = "rec";
            public string craftCommand = "craftrecycler";
            public bool restrictUseByCupboard = true;
            public bool adminSpawnsPublicRecycler = true;
            public bool useSpawning = false;
            public bool useCrafting = false;
            public bool useSpawnCooldown = true;
            public bool useCraftCooldown = true;
            public bool useSpawnLimit = false;
            public bool useCraftLimit = false;
            public bool allowDeployOnGround = false;
            public bool allowPickupByHammerHit = true;
            public bool pickupOnlyOwnerFriends = true;
            public bool spawnInLoot = false;
            public Rates DefaultRates = new Rates();
            public Dictionary<string, int> itemsNeededToCraft = new Dictionary<string, int>()
            {
                { "scrap", 750 },
                { "gears", 25 },
                { "metalspring", 25 },
            };
            public Dictionary<string, Rates> PermissionsRates = new Dictionary<string, Rates>();
            public List<Loot> Loot = new List<Loot>();
        }

        public class Loot
        {
            public string containerName;
            public int probability = 0;
        }

        class Rates
        {
            public int Priority = 1;
            public float spawnCooldown = 86400f;
            public float craftCooldown = 86400f;
            public int craftLimit = 1;
            public int spawnLimit = 1;
            public float Ratio = 0.5f;
            public float RatioScrap = 1f;
            public float Speed = 5f;
            public float percentOfMaxStackToTake = 0.1f;
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData()
            {
                PermissionsRates = new Dictionary<string, Rates>()
                {
                    {  "viptest", new Rates() },
                    {  "viptest2", new Rates(){ Priority =2, Ratio = 0.7f, Speed =3f } }
                }
            };
            SaveConfig(configData);
            PrintWarning("New configuration file created.");
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"Title", "Recycler:" },
                {"badCommand", "you can get recycler in kit home" },
                {"buldingBlocked", "you need building privilege" },
                {"cooldown", "Сooldown, wait {0} seconds" },
                {"cooldown craft", "Сooldown, wait {0} seconds" },
                {"recycler crafted", "You have crafted a recycler" },
                {"recycler got", "You have got a recycler" },
                {"cannot craft", "Sorry, you can't craft a recycler" },
                {"not enough ingredient", "You should have {0} x{1}" },
                {"inventory full", "You should have space in inventory" },
                {"limit", "You have reached the limit of {0} recyclers" },
                {"place on construction", "You can't place it on ground" },
                {"cant pick", "You can pickup only your own or friend recycler" },
            }, this, "en");
            lang.RegisterMessages(new Dictionary<string, string>()
            {
                {"Title", "Переработчик:" },
                {"badCommand", "ты можешь получить его в kit home" },
                {"buldingBlocked", "нужна авторизация в шкафу" },
                {"cooldown", "Подождите еще {0} секунд" },
                {"cooldown craft", "Подождите еще {0} секунд" },
                {"recycler crafted", "Ты скрафтил переработчик" },
                {"recycler got", "Ты получил переработчик" },
                {"cannot craft", "Ты не можешь крафтить переработчик" },
                {"not enough ingredient", "Тебе нужно {0} x{1}" },
                {"inventory full", "Нет места в инвентаре" },
                {"limit", "Достигнут лимит в {0} переработчика" },
                {"place on construction", "Нельзя ставить на землю" },
                {"cant pick", "Ты можешь поднять только свой переработчик или друга" },
            }, this, "ru");
        }
        void Init()
        {
            Instance = this;
            configData = Config.ReadObject<ConfigData>();
            configData.PermissionsRates = configData.PermissionsRates.OrderBy(i => -i.Value.Priority).ToDictionary(x => x.Key, x => x.Value);
            SaveConfig(configData);
            Unsubscribe(nameof(OnLootSpawn));
            if (!configData.allowPickupByHammerHit) Unsubscribe(nameof(OnHammerHit));
            try
            {
                pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(this.Title);
            }
            catch
            {
                pluginData = new PluginData();
            }
            foreach (var perm in configData.PermissionsRates)
            {
                permission.RegisterPermission("homerecycler." + perm.Key, this);
            }
            permission.RegisterPermission("homerecycler.canget", this);
            permission.RegisterPermission("homerecycler.cancraft", this);
            permission.RegisterPermission("homerecycler.ignorecooldown", this);
            permission.RegisterPermission("homerecycler.ignorecraftcooldown", this);

            if (configData.useSpawning)
            {
                cmd.AddChatCommand(configData.chatCommand, this, "cmdRec");
            }
            if (configData.useCrafting) {
                if (configData.itemsNeededToCraft.Count < 1)
                {
                    PrintWarning("no items set to craft, check config");
                }
                else
                {
                    cmd.AddChatCommand(configData.craftCommand, this, "cmdCraft");
                }
            }
        }
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);

        void OnNewSave()
        {
            pluginData = new PluginData();
            SaveData();
        }

        void OnServerInitialized()
        {
            var allobjects = UnityEngine.Object.FindObjectsOfType<Recycler>();
            foreach(var r in allobjects)
            {
                if (r.OwnerID!=0 && r.gameObject.GetComponent<RecyclerEntity>()==null)
                    r.gameObject.AddComponent<RecyclerEntity>();
            }
            var ingredients = new List<ItemAmount>();
            foreach (var i in configData.itemsNeededToCraft)
            {
                var def = ItemManager.FindItemDefinition(i.Key);
                if (def == null)
                {
                    PrintWarning($"cannot find item {i.Key} for crafting, check config");
                    continue;
                }
                itemsNeededToCraft.Add(def.itemid, i);
            }
            if (configData.spawnInLoot)
            {
                Subscribe(nameof(OnLootSpawn));
                foreach (var container in UnityEngine.Object.FindObjectsOfType<LootContainer>())
                {
                    if (configData.Loot.FirstOrDefault(c => c.containerName == container.ShortPrefabName) == null)
                    {
                        configData.Loot.Add(new Loot() { containerName = container.ShortPrefabName });
                    }
                    container.SpawnLoot();
                }
            }
            SaveConfig(configData);
        }

        void OnLootSpawn(LootContainer container)
        {
            timer.In(1f, () =>
            {
                if (container == null) return;
                Loot cont = configData.Loot.FirstOrDefault(c => c.containerName == container.ShortPrefabName);
                if (cont == null || cont.probability < 1) return;
                int current = UnityEngine.Random.Range(0, 100);
                if (current <= cont.probability)
                {
                    container.inventorySlots = container.inventory.itemList.Count() + 5;
                    container.inventory.capacity = container.inventory.itemList.Count() + 5;
                    container.SendNetworkUpdateImmediate();
                    GiveRecycler(container.inventory);
                    //PlaceComponent(container, cont.itemid, cont.skinid, cont.name, cont.minCount, cont.maxCount);
                }
            });
        }

        void Unload()
        {
            var allobjects = UnityEngine.Object.FindObjectsOfType<RecyclerEntity>();
            foreach (var r in allobjects)
            {
                GameObject.Destroy(r);
            }
        }

        object CanStackItem(Item item, Item anotherItem)
        {
            if (item.info.itemid == 833533164 && item.skin == 1321253094)
                return false;
            return null;
        }

        float GetCraftLimit(BasePlayer player)
        {
            foreach (var perm in configData.PermissionsRates)
            {
                if (permission.UserHasPermission(player.UserIDString, "homerecycler." + perm.Key))
                {
                    return perm.Value.craftLimit;
                }
            }
            return configData.DefaultRates.craftLimit;
        }

        float GetSpawnLimit(BasePlayer player)
        {
            foreach (var perm in configData.PermissionsRates)
            {
                if (permission.UserHasPermission(player.UserIDString, "homerecycler." + perm.Key))
                {
                    return perm.Value.spawnLimit;
                }
            }
            return configData.DefaultRates.spawnLimit;
        }

        float GetCraftCooldown(BasePlayer player)
        {
            foreach (var perm in configData.PermissionsRates)
            {
                if (permission.UserHasPermission(player.UserIDString, "homerecycler." + perm.Key))
                {
                    return perm.Value.craftCooldown;
                }
            }
            return configData.DefaultRates.craftCooldown;
        }

        float GetSpawnCooldown(BasePlayer player)
        {
            foreach (var perm in configData.PermissionsRates)
            {
                if (permission.UserHasPermission(player.UserIDString, "homerecycler." + perm.Key))
                {
                    return perm.Value.spawnCooldown;
                }
            }
            return configData.DefaultRates.spawnCooldown;
        }

        void cmdRec(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "homerecycler.canget"))
            {
                SendMsg(player, "badCommand");
                return;
            }
            if (configData.useSpawnCooldown)
            {
                if (!permission.UserHasPermission(player.UserIDString, "homerecycler.ignorecooldown"))
                {
                    double time = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                    float spawnCooldown = GetSpawnCooldown(player);
                    if (!pluginData.userCooldowns.ContainsKey(player.userID))
                        pluginData.userCooldowns.Add(player.userID, time + spawnCooldown);
                    else
                    {
                        double nextUseTime = pluginData.userCooldowns[player.userID];
                        if (nextUseTime > time)
                        {
                            SendMsg(player, "cooldown", true, new string[] { ((int)(nextUseTime - time)).ToString() });
                            return;
                        }
                        else pluginData.userCooldowns[player.userID] = time + spawnCooldown;
                    }
                    SaveData();
                }
            }
            if (configData.useSpawnLimit)
            {
                if (!pluginData.userSpawned.ContainsKey(player.userID))
                    pluginData.userSpawned.Add(player.userID, 0);
                float spawnLimit = GetSpawnLimit(player);
                if (pluginData.userSpawned[player.userID]>=spawnLimit)
                {
                    SendMsg(player, "limit", true, new string[] { spawnLimit.ToString() });
                    return;
                }
                pluginData.userSpawned[player.userID]++;
                SaveData();
            }
            if (GiveRecycler(player))
            {
                SendMsg(player, "recycler got");
            }
            else
            {
                SendMsg(player, "inventory full");
            }
        }

        void cmdCraft(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "homerecycler.cancraft"))
            {
                SendMsg(player, "cannot craft");
                return;
            }
            string mess = "";
            bool enough = true;
            foreach(var item in itemsNeededToCraft)
            {
                var haveCount = player.inventory.GetAmount(item.Key);
                if (haveCount >= item.Value.Value) continue;
                mess += String.Format(msg("not enough ingredient", player)+"\n", item.Value.Key, item.Value.Value);
                enough = false;
            }
            if (!enough)
            {
                SendReply(player, mess);
                return;
            }

            if (configData.useCraftCooldown)
            {
                if (!permission.UserHasPermission(player.UserIDString, "homerecycler.ignorecraftcooldown"))
                {
                    double time = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
                    float craftCooldown = GetCraftCooldown(player);
                    if (!pluginData.userCooldownsCraft.ContainsKey(player.userID))
                        pluginData.userCooldownsCraft.Add(player.userID, time + craftCooldown);
                    else
                    {
                        double nextUseTime = pluginData.userCooldownsCraft[player.userID];
                        if (nextUseTime > time)
                        {
                            SendMsg(player, "cooldown craft", true, new string[] { ((int)(nextUseTime - time)).ToString() });
                            return;
                        }
                        else pluginData.userCooldownsCraft[player.userID] = time + craftCooldown;
                    }
                    SaveData();
                }
            }
            if (configData.useCraftLimit)
            {
                if (!pluginData.userCrafted.ContainsKey(player.userID))
                    pluginData.userCrafted.Add(player.userID, 0);
                float craftLimit = GetCraftLimit(player);
                if (pluginData.userCrafted[player.userID] >= craftLimit)
                {
                    SendMsg(player, "limit", true, new string[] { craftLimit.ToString() });
                    return;
                }
                pluginData.userCrafted[player.userID]++;
                SaveData();
            }
            foreach (var item in itemsNeededToCraft)
            {
                player.inventory.Take(null, item.Key, item.Value.Value);
            }
            if (GiveRecycler(player))
            {
                SendMsg(player, "recycler crafted");
            }
            else
            {
                SendMsg(player, "inventory full");
            }            
        }

        private string msg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player?.UserIDString);
        private void SendMsg(BasePlayer player, string langkey, bool title = true, params string[] args)
        {
            string message = $"<color=white>{String.Format(msg(langkey, player), args)}</color>";
            if (title) message = $"<color=orange>{msg("Title", player)}</color> " + message;
            SendReply(player, message);
        }

        [ConsoleCommand("giverecycler")]
        void cmdGiveRecycler(ConsoleSystem.Arg arg)
        {
            var player = arg?.Player() ?? null;
            if (player?.net.connection.authLevel < 2) return;
            if (arg.Args == null || arg.Args.Length<1)
            {
                SendReply(arg, "bad syntax");
                return;
            }
            BasePlayer targetPlayer = BasePlayer.Find(arg.Args[0]);
            if (targetPlayer == null)
            {
                SendReply(arg, "error player not found for give");
                return;
            }
            if (GiveRecycler(targetPlayer))
            {
                SendReply(targetPlayer, msg("recycler got",targetPlayer));
            }
            else
            {
                SendReply(targetPlayer, msg("inventory full", targetPlayer));
            }
        }

        bool GiveRecycler(ItemContainer container)
        {
            var item = ItemManager.CreateByItemID(833533164, 1, 1321253094);
            item.name = "Recycler";
            return item.MoveToContainer(container,-1, false);
        }

        bool GiveRecycler(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(833533164, 1, 1321253094);
            item.name = "Recycler";
            if (!player.inventory.GiveItem(item))
            {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return false;
            }
            return true;
        }

        bool Check(BaseEntity entity)
        {
            GroundWatch component = entity.gameObject.GetComponent<GroundWatch>();
            List<Collider> list = Facepunch.Pool.GetList<Collider>();
            Vis.Colliders<Collider>(entity.transform.TransformPoint(component.groundPosition), component.radius, list, component.layers, QueryTriggerInteraction.Collide);
            foreach (Collider collider in list)
            {
                if (!(collider.transform.root == entity.gameObject.transform.root))
                {
                    BaseEntity baseEntity = collider.gameObject.ToBaseEntity();
                    if ((!(bool)(baseEntity) || !baseEntity.IsDestroyed && !baseEntity.isClient) && baseEntity is BuildingBlock)
                    {
                        Facepunch.Pool.FreeList<Collider>(ref list);
                        return true;
                    }
                }
            }
            Facepunch.Pool.FreeList<Collider>(ref list);
            return false;
        }


        void OnEntityBuilt(Planner plan, GameObject obj)
        {
            var entity = obj.GetComponent<BaseEntity>();
            if (entity != null && entity.ShortPrefabName == "box.wooden.large" && entity.skinID == 1321253094L)
            {
                BasePlayer player = plan.GetOwnerPlayer();
                if (!configData.allowDeployOnGround && player.net.connection.authLevel < 2)
                {
                    if (!Check(entity))
                    {
                        GiveRecycler(player);
                        SendMsg(player, "place on construction");
                        entity.Kill();
                        return;
                    }
                }
                Recycler recycler = GameManager.server.CreateEntity("assets/bundled/prefabs/static/recycler_static.prefab", entity.transform.position, entity.transform.rotation) as Recycler;
                if (configData.adminSpawnsPublicRecycler && player.net.connection.authLevel == 2) recycler.OwnerID = 0;
                else recycler.OwnerID = player.userID;
                recycler.Spawn();
                entity.Kill();
                recycler.gameObject.AddComponent<RecyclerEntity>();
            }
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null || info.HitEntity == null)
                return;
            RecyclerEntity rec = info.HitEntity.GetComponent<RecyclerEntity>();
            if (rec != null && rec.OwnerID != 0)
            {
                if ((player.IsBuildingBlocked() || player.GetBuildingPrivilege() == null) && !player.IsBuildingAuthed())
                {
                    SendMsg(player, "buldingBlocked");
                    return;
                }
                if (configData.pickupOnlyOwnerFriends && !(rec.OwnerID==player.userID || (bool)(Friends?.Call("AreFriends", rec.OwnerID, player.userID) ?? false)))
                {
                    SendMsg(player, "cant pick");
                    return;
                }
                if (GiveRecycler(player))
                {
                    SendMsg(player, "recycler got");
                }
                else
                {
                    SendMsg(player, "inventory full");
                }
                Recycler rec2 = rec.gameObject.GetComponent<Recycler>();
                if (rec2.inventory.itemList.Count > 0)
                {
                    rec2.inventory.Drop("assets/prefabs/misc/item drop/item_drop.prefab", rec2.transform.position + new Vector3(0f, 1f, 0f), rec2.transform.rotation);
                }
                info.HitEntity.Kill();
            }
        }

        void OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            if (recycler.IsOn() || recycler.OwnerID == 0) return;
            if (configData.restrictUseByCupboard && recycler.OwnerID!=0 && (player.IsBuildingBlocked() || player.GetBuildingPrivilege() == null) && !player.IsBuildingAuthed())
            {
                NextTick(() =>
                {
                    SendMsg(player, "buldingBlocked");
                    recycler.StopRecycling();
                });
                return;
            }
            startedRecyclers[recycler.net.ID] = player.userID;
            NextTick(() =>
            {
                foreach (var perm in configData.PermissionsRates)
                {
                    if (permission.UserHasPermission(player.UserIDString, "homerecycler."+perm.Key))
                    {
                        recycler.CancelInvoke(new Action(recycler.RecycleThink));
                        recycler.InvokeRepeating(new Action(recycler.RecycleThink), perm.Value.Speed, perm.Value.Speed);
                        return;
                    }
                }
                if (configData.DefaultRates.Speed!=5f)
                {
                    recycler.CancelInvoke(new Action(recycler.RecycleThink));
                    recycler.InvokeRepeating(new Action(recycler.RecycleThink), configData.DefaultRates.Speed, configData.DefaultRates.Speed);
                }
            });
        }

        object OnRecycleItem(Recycler recycler, Item item)
        {
            if (recycler.OwnerID == 0) return null;
            if (item.info.Blueprint == null)
            {
                return false;
            }
            bool flag = false;
            float num = configData.DefaultRates.Ratio;
            float percentToTake = configData.DefaultRates.percentOfMaxStackToTake;
            if (startedRecyclers.ContainsKey(recycler.net.ID))
            {
                foreach (var perm in configData.PermissionsRates)
                {
                    if (permission.UserHasPermission(startedRecyclers[recycler.net.ID].ToString(), "homerecycler." + perm.Key))
                    {
                        num = perm.Value.Ratio;
                        percentToTake = perm.Value.percentOfMaxStackToTake;
                        break;
                    }
                }
            }
            //PrintWarning($"ratio is {num}");

            if (item.hasCondition)
            {
                num = Mathf.Clamp01(num * Mathf.Clamp(item.conditionNormalized * item.maxConditionNormalized, 0.1f, 1f));
                //PrintWarning($"corrected ratio is {num}");
            }
            int num2 = 1;
            if (item.amount > 1)
            {
                num2 = Mathf.CeilToInt(Mathf.Min((float)item.amount, (float)item.info.stackable * percentToTake));
            }
            //PrintWarning($"amount is {num2}");
            if (item.info.Blueprint.scrapFromRecycle > 0)
            {
                float ratioScrap = configData.DefaultRates.RatioScrap;
                if (startedRecyclers.ContainsKey(recycler.net.ID)) {
                    foreach (var perm in configData.PermissionsRates)
                    {
                        if (permission.UserHasPermission(startedRecyclers[recycler.net.ID].ToString(), "homerecycler." + perm.Key))
                        {
                            ratioScrap = perm.Value.RatioScrap;
                            break;
                        }
                    }
                }
                Item newItem = ItemManager.CreateByName("scrap", (int)(item.info.Blueprint.scrapFromRecycle * num2 * ratioScrap), 0uL);
                recycler.MoveItemToOutput(newItem);
            }
            item.UseItem(num2);
            foreach (ItemAmount ingredient in item.info.Blueprint.ingredients)
            {
                if (!(ingredient.itemDef.shortname == "scrap"))
                {
                    float num3 = ingredient.amount / (float)item.info.Blueprint.amountToCreate;
                    int num4 = 0;
                    if (num3 <= 1f)
                    {
                        for (int j = 0; j < num2; j++)
                        {
                            if (UnityEngine.Random.Range(0f, 1f) <= num)
                            {
                                num4++;
                            }
                        }
                    }
                    else
                    {
                        num4 = Mathf.CeilToInt(Mathf.Clamp(num3 * num * UnityEngine.Random.Range(1f, 1f), 0f, ingredient.amount) * (float)num2);
                    }
                    if (num4 > 0)
                    {
                        int num5 = Mathf.CeilToInt((float)num4 / (float)ingredient.itemDef.stackable);
                        for (int k = 0; k < num5; k++)
                        {
                            int num6 = (num4 <= ingredient.itemDef.stackable) ? num4 : ingredient.itemDef.stackable;
                            //PrintWarning($"num6 = {num6}");
                            Item newItem2 = ItemManager.Create(ingredient.itemDef, num6, 0uL);
                            if (!recycler.MoveItemToOutput(newItem2))
                            {
                                flag = true;
                            }
                            num4 -= num6;
                            if (num4 <= 0)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            if (flag) recycler.StopRecycling();
            return false;
        }

    }
}