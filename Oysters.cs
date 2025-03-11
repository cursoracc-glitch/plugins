using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("Oysters", "OxideBro", "0.1.1")]
    class Oysters : RustPlugin
    {
        #region Configuration
        private PluginConfig config;

        protected override void LoadDefaultConfig() => config = PluginConfig.DefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < new VersionNumber(0, 1, 0))
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }
            config.PluginVersion = Version;
        }

        public class DefaultItems
        {
            [JsonProperty("Shortname предмета")]
            public string ShortName;
            [JsonProperty("Минимальное количество")]
            public int MinAmount;
            [JsonProperty("Максимальное количество")]
            public int MaxAmount;
            [JsonProperty("SkinID предмета")]
            public ulong SkinID;
            [JsonProperty("Имя предмета при создании (Оставте поле постым чтобы использовать стандартное название итема)")]
            public string Name;
            [JsonProperty("Это чертеж")]
            public bool IsBlueprnt;
        }

        class OysterSetting
        {
            [JsonProperty("SkinID устрицы")]
            public ulong SkinID;
            [JsonProperty("Имя предмета устрицы")]
            public string Name;
            [JsonProperty("Shortname предмета для устрицы")]
            public string ShortName;
            [JsonProperty("Выдаваемое количество")]
            public int Amount;
            [JsonProperty("Шанс что игроку даст данную открытую устрицу")]
            public int Change;
            [JsonProperty("Список предметов какие даёт при открытии")]
            public List<DefaultItems> Items;
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        class PluginConfig
        {
            [JsonProperty("Configuration Version")]
            public VersionNumber PluginVersion = new VersionNumber();

            [JsonProperty("SkinID закрытой устрицы")]
            public ulong SkinID;
            [JsonProperty("Имя предмета закрытой устрицы ")]
            public string Name;
            [JsonProperty("Shortname предмета для закрытой устрицы")]
            public string ShortName;
            [JsonProperty("Выдаваемое количество")]
            public int Amount;
            [JsonProperty("Шанс что игроку даст данную закрытую устрицу")]
            public int Change;

            [JsonProperty("Засчитывать очки фермера друзьям")]
            public bool EnableFriendsFarm;

            [JsonProperty("Настройки открытых устриц")]
            public List<OysterSetting> OytersList;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginVersion = new VersionNumber(),
                    Amount = 1,
                    Change = 10,
                    Name = "Устрица",
                    ShortName = "fish.troutsmall",
                    SkinID = 1916873496,
                    EnableFriendsFarm = false,

                    OytersList = new List<OysterSetting>()
                    {
                        new OysterSetting()
                        {
                            Amount = 1,
                            Change = 5,
                            Items = new List<DefaultItems>(),
                            Name = "Раковина с черной жемчуженой",
                            ShortName = "sticks",
                            SkinID = 1916874386,
                        },
                        new OysterSetting()
                        {
                              Amount = 1,
                              Change = 45,
                              Items = new List<DefaultItems>(),
                              Name = "Раковина с белой жемчуженой",
                              ShortName = "sticks",
                              SkinID = 1916875142,
                        },
                        new OysterSetting()
                        {
                            Amount = 1,
                            Change = 50,
                            Items = new List<DefaultItems>(),
                            Name = "Пустая раковина",
                            ShortName = "sticks",
                            SkinID = 1916875604,
                        },
                    }
                };
            }
        }
        #endregion

        #region Init & Reference        

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null || go == null) return;
            if (go.ToBaseEntity() != null && go.ToBaseEntity()?.GetComponent<WildlifeTrap>() != null)
                SetPreferences(go.ToBaseEntity()?.GetComponent<WildlifeTrap>());
        }

        void SetPreferences(WildlifeTrap trap)
        {
            if (trap == null) return;
            trap.CancelInvoke(trap.TrapThink);
            trap.gameObject.AddComponent<CustomTraps>();
        }

        public static Oysters ins;

        void Init() => LoadData();

        void OnServerSave() => SaveData();

        void OnServerInitialized()
        {
            ins = this;
            var traps = UnityEngine.Object.FindObjectsOfType<WildlifeTrap>();
            if (traps != null)
            {
                foreach (var trap in traps)
                {
                    if (trap.GetComponent<CustomTraps>() == null)
                        trap.gameObject.AddComponent<CustomTraps>();
                }
            }
        }

        void Unload()
        {
            SaveData();
            var traps = UnityEngine.Object.FindObjectsOfType<CustomTraps>();
            if (traps != null)
                foreach (var trap in traps)
                {
                    if (trap.trap != null)
                        trap.trap.SetTrapActive(true);

                    UnityEngine.Object.Destroy(trap);
                }
        }

        private void OnNewSave()
        {
            PlayersList.Clear();
            SaveData();
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item == null || player == null || player.GetComponent<NPCPlayer>() != null) return null;
            if (action == "Gut" && item.skin == config.SkinID)
            {
                if (item.amount > 1)
                {
                    item.amount--;
                    item.MarkDirty();
                }
                else item.RemoveFromContainer();
                var random = UnityEngine.Random.RandomRange(3, 100);
                var oyster = config.OytersList.Find(p => p.Change == Find(random));
                var item1 = ItemManager.CreateByName(oyster.ShortName, oyster.Amount, oyster.SkinID);
                item1.name = oyster.Name;
                GiveItem(player, item1, BaseEntity.GiveItemReason.ResourceHarvested);

                if (!PlayersList.ContainsKey(player.userID)) PlayersList.Add(player.userID, new PlayerInfo());

                switch (oyster.SkinID)
                {
                    case 1916874386:
                        PlayersList[player.userID].Black++;
                        break;
                    case 1916875142:
                        PlayersList[player.userID].Write++;
                        break;
                }
                PlayersList[player.userID].Opened++;

                EffectNetwork.Send(new Effect("assets/bundled/prefabs/fx/gestures/cut_meat.prefab", player, 0, Vector3.up, Vector3.zero)
                {
                    scale = UnityEngine.Random.Range(0f, 1f),
                }
              );
                return true;
            }

            if (action == "Gut" && config.OytersList.Find(p => p.SkinID == item.skin) != null)
                return true;
            return null;
        }

        private int? Find(int x)
        {
            var num = (from number in config.OytersList.Select(p => p.Change)
                       let difference = Math.Abs(number - x)
                       orderby difference, Math.Abs(number), number descending
                       select number)
                .FirstOrDefault();
            return num;
        }

        [ChatCommand("g")]
        void cmdGive(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            var item = ItemManager.CreateByName(ins.config.ShortName, 100);
            item.skin = ins.config.SkinID;
            item.name = ins.config.Name;
            GiveItem(player, item);
        }

        Dictionary<BasePlayer, bool> CreateLoot = new Dictionary<BasePlayer, bool>();

        [ChatCommand("oyloot")]
        void cmdOystersLoot(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (CreateLoot.ContainsKey(player))
                CreateLoot.Remove(player);
            if (args.Length < 1) return;

            switch (args[0])
            {
                case "write":
                    CreateLoot.Add(player, true);
                    SendReply(player, "Режим создания лута для белой жемчужены включен, ударьте по ящику с лутом чтобы скопировать его содержимое.");
                    break;
                case "black":
                    CreateLoot.Add(player, false);
                    SendReply(player, "Режим создания лута для черной жемчужены  включен, ударьте по ящику с лутом чтобы скопировать его содержимое.");
                    break;
            }
        }

        object OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (!CreateLoot.ContainsKey(player)) return null;
            if (player == null || info == null) return null;

            var container = info.HitEntity.GetComponent<StorageContainer>();
            if (!player.IsAdmin) return null;
            if (container == null) return null;
            if (CreateLoot[player])
            {
                config.OytersList.Find(p => p.Name.Contains("белой")).Items.Clear();
                var items = container.inventory.itemList;
                foreach (var item in items)
                {
                    config.OytersList.Find(p => p.Name.Contains("белой")).Items.Add(new DefaultItems()
                    {
                        IsBlueprnt = item.IsBlueprint(),
                        MaxAmount = item.amount,
                        MinAmount = item.amount == 1 ? item.amount : item.amount / 2,
                        Name = "",
                        ShortName = item.info.shortname,
                        SkinID = item.skin,
                    });
                }
                SendReply(player, $"Лут успешно обновлен. Добавлено новых предметов: {items.Count}");
            }
            else
            {
                config.OytersList.Find(p => p.Name.Contains("черной")).Items.Clear();
                var items = container.inventory.itemList;
                foreach (var item in items)
                {
                    config.OytersList.Find(p => p.Name.Contains("черной")).Items.Add(new DefaultItems()
                    {
                        IsBlueprnt = item.IsBlueprint(),
                        MaxAmount = item.amount,
                        MinAmount = item.amount == 1 ? item.amount : item.amount / 2,
                        Name = "",
                        ShortName = item.info.shortname,
                        SkinID = item.skin,
                    });


                }
                SendReply(player, $"Лут успешно обновлен. Добавлено новых предметов: {items.Count}");
            }

            SaveConfig();
            return null;
        }

        object OnItemSplit(Item item, int split_Amount)
        {
            if (config.OytersList.FirstOrDefault(p => p.SkinID == item.skin) != null)
            {
                Item byItemId = ItemManager.CreateByItemID(item.info.itemid, 1, item.skin);
                item.amount -= split_Amount;
                byItemId.amount = split_Amount;
                byItemId.name = item.name;
                byItemId.text = item.text;
                item.MarkDirty();
                return byItemId;
            }

            if (config.SkinID == item.skin)
            {
                Item byItemId = ItemManager.CreateByItemID(item.info.itemid, 1, item.skin);
                item.amount -= split_Amount;
                byItemId.amount = split_Amount;
                byItemId.name = item.name;
                byItemId.text = item.text;
                item.MarkDirty();
                return byItemId;
            }
            return null;
        }

        object CanCombineDroppedItem(DroppedItem drItem, DroppedItem anotherDrItem)
        {
            if (config.OytersList.FirstOrDefault(p => p.SkinID == drItem.item.skin) == null && config.OytersList.FirstOrDefault(p => p.SkinID == anotherDrItem.item.skin) == null &&
                config.SkinID != drItem.item.skin && config.SkinID != anotherDrItem.item.skin) return null;

            if (drItem.item.skin != anotherDrItem.item.skin || !IsTextEqual(drItem.item.text, anotherDrItem.item.text)) return false;
            return null;
        }

        object CanStackItem(Item item, Item anotherItem)
        {
            if (config.OytersList.FirstOrDefault(p => p.SkinID == item.skin) == null && config.OytersList.FirstOrDefault(p => p.SkinID == anotherItem.skin) == null &&
                config.SkinID != item.skin && config.SkinID != anotherItem.skin) return null;

            if (item.info.itemid == anotherItem.info.itemid && (item.skin != anotherItem.skin || !IsTextEqual(item.text, anotherItem.text))) return false;
            return null;
        }

        private object OnItemRecycle(Item item, Recycler recycler)
        {
            if (recycler == null || item == null) return null;
            if (config.OytersList.Find(p => p.SkinID == item.skin) != null && config.OytersList.Find(p => p.ShortName == item.info.shortname) != null)
            {
                var itemRecycler = config.OytersList.Find(p => p.SkinID == item.skin);
                item.UseItem(1);
                var currentItem = itemRecycler.Items.GetRandom();
                if (currentItem == null) return false;
                Item NewItem = ItemManager.CreateByName(currentItem.ShortName, UnityEngine.Random.Range(currentItem.MinAmount, currentItem.MaxAmount), currentItem.SkinID);
                if (!string.IsNullOrEmpty(currentItem.Name))
                    NewItem.name = currentItem.Name;
                if (NewItem == null)
                {
                    PrintError($"Shortname error: {currentItem.ShortName}");
                    return null;
                }
                recycler.MoveItemToOutput(NewItem);

                return true;
            }
            return null;
        }

        private static bool IsTextEqual(string var1, string var2)
        {
            if (string.IsNullOrEmpty(var1) && string.IsNullOrEmpty(var2))
                return true;

            if (string.IsNullOrEmpty(var1) || string.IsNullOrEmpty(var2))
                return false;

            return var1 == var2;
        }

        private static ulong GetUL(string userID)
        {
            if (string.IsNullOrEmpty(userID))
                return 0;

            ulong result = 0;
            try { result = (ulong)Convert.ToUInt64(userID); } catch { result = 0; }
            return result;
        }

        private class CustomTraps : BaseEntity
        {
            public WildlifeTrap trap;

            private void Awake()
            {
                trap = GetComponent<WildlifeTrap>();
                var time = UnityEngine.Random.Range(240, 300);
                InvokeHandler.InvokeRepeating(this, RandomTrap, time, time); 
            }

            private void OnDestroy() => InvokeHandler.CancelInvoke(this, RandomTrap);

            public virtual void RandomTrap()
            {
                int baitCalories = trap.GetBaitCalories();
                if (baitCalories <= 0)
                    return;
                TrappableWildlife randomWildlife = trap.GetRandomWildlife();
                if (baitCalories >= randomWildlife.caloriesForInterest && UnityEngine.Random.Range(0f, 1f) <= randomWildlife.successRate)
                {
                    UseBaitCalories(randomWildlife.caloriesForInterest);
                    if (UnityEngine.Random.Range(0f, 1f) <= trap.trapSuccessRate)
                        GiveItems(randomWildlife);
                }
            }

            private void UseBaitCalories(int numToUse)
            {
                foreach (Item item in trap.inventory.itemList)
                {
                    var component = item.info.GetComponent<ItemModConsumable>();
                    if (component == null || trap.ignoreBait.Contains(item.info))
                        continue;

                    int itemCalories = trap.GetItemCalories(item);
                    if (itemCalories <= 0) continue;

                    numToUse -= itemCalories;
                    item.UseItem(1);
                    if (numToUse > 0) continue;

                    return;
                }
            }

            public void GiveItems(TrappableWildlife life)
            {
                Item item = ItemManager.Create(life.inventoryObject, UnityEngine.Random.Range(life.minToCatch, life.maxToCatch + 1), 0UL);

                if (!MoveToContainer(item, trap.inventory, -1, true))
                    item.Remove(0f);
                else
                    SetFlag(BaseEntity.Flags.Reserved1, true, false, true);

                TryGetOyster();

                trap.Hurt(trap.StartMaxHealth() * 0.1f, Rust.DamageType.Decay, null, false);
            }

            private void TryGetOyster()
            {
                Item item = null;

                bool random = UnityEngine.Random.Range(0, 100) < ins.config.Change;
                if (random)
                {
                    if (!ins.PlayersList.ContainsKey(trap.OwnerID)) ins.PlayersList.Add(trap.OwnerID, new PlayerInfo());

                    item = ItemManager.CreateByName(ins.config.ShortName, ins.config.Amount);
                    item.skin = ins.config.SkinID;
                    item.name = ins.config.Name;
                    item.text = trap.OwnerID.ToString();
                    ins.PlayersList[trap.OwnerID].Caught++;
                }

                if (item != null)
                {
                    if (!MoveToContainer(item, trap.inventory, -1, true))
                        item.Remove(0f);
                    else
                        SetFlag(BaseEntity.Flags.Reserved1, true, false, true);
                }
            }

            public void PlayerStoppedLooting(global::BasePlayer player)
            {
                trap.CancelInvoke(trap.TrapThink);
                return;
            }
        }
        #endregion

        private static bool MoveToContainer(Item itemBase, ItemContainer newcontainer, int iTargetPos = -1, bool allowStack = true)
        {
            bool container;
            Quaternion quaternion;
            using (TimeWarning timeWarning = TimeWarning.New("MoveToContainer", 0))
            {
                var itemContainer = itemBase.parent;
                if (!itemBase.CanMoveTo(newcontainer, iTargetPos))
                    container = false;
                else
                    if (iTargetPos >= 0 && newcontainer.SlotTaken(itemBase, iTargetPos))
                {
                    Item slot = newcontainer.GetSlot(iTargetPos);

                    if (allowStack)
                    {
                        int num = slot.MaxStackable();
                        if (slot.CanStack(itemBase))
                        {
                            if (slot.amount < num)
                            {
                                slot.amount += itemBase.amount;
                                slot.MarkDirty();
                                itemBase.RemoveFromWorld();
                                itemBase.RemoveFromContainer();
                                itemBase.Remove(0f);
                                int num1 = slot.amount - num;
                                if (num1 > 0)
                                {
                                    Item item = slot.SplitItem(num1);
                                    if (item != null && !MoveToContainer(item, newcontainer, -1, false) && (itemContainer == null || !MoveToContainer(item, itemContainer, -1, true)))
                                    {
                                        Vector3 vector3 = newcontainer.dropPosition;
                                        Vector3 vector31 = newcontainer.dropVelocity;
                                        quaternion = new Quaternion();
                                        item.Drop(vector3, vector31, quaternion);
                                    }
                                    slot.amount = num;
                                }
                                container = true;
                                return container;
                            }
                            else
                            {
                                container = false;
                                return container;
                            }
                        }
                    }

                    if (itemBase.parent == null)
                        container = false;
                    else
                    {
                        ItemContainer itemContainer1 = itemBase.parent;
                        int num2 = itemBase.position;
                        if (slot.CanMoveTo(itemContainer1, num2))
                        {
                            itemBase.RemoveFromContainer();
                            slot.RemoveFromContainer();
                            MoveToContainer(slot, itemContainer1, num2, true);
                            container = MoveToContainer(itemBase, newcontainer, iTargetPos, true);
                        }
                        else
                            container = false;
                    }
                }
                else
                        if (itemBase.parent != newcontainer)
                {
                    if (iTargetPos == -1 & allowStack && itemBase.info.stackable > 1)
                    {
                        var item1 = newcontainer.itemList.Where(x => x != null && x.info.itemid == itemBase.info.itemid && x.skin == itemBase.skin).OrderBy(x => x.amount).FirstOrDefault();

                        if (item1 != null && item1.CanStack(itemBase))
                        {
                            int num3 = item1.MaxStackable();
                            if (item1.amount < num3)
                            {
                                var total = item1.amount + itemBase.amount;
                                if (total <= num3)
                                {
                                    item1.amount += itemBase.amount;
                                    item1.MarkDirty();
                                    itemBase.RemoveFromWorld();
                                    itemBase.RemoveFromContainer();
                                    itemBase.Remove(0f);
                                    container = true;
                                    return container;
                                }
                                else
                                {
                                    item1.amount = item1.MaxStackable();
                                    item1.MarkDirty();
                                    itemBase.amount = total - item1.MaxStackable();
                                    itemBase.MarkDirty();
                                    container = MoveToContainer(itemBase, newcontainer, iTargetPos, allowStack);
                                    return container;
                                }
                            }
                        }
                    }

                    if (newcontainer.maxStackSize > 0 && newcontainer.maxStackSize < itemBase.amount)
                    {
                        Item item2 = itemBase.SplitItem(newcontainer.maxStackSize);
                        if (item2 != null && !MoveToContainer(item2, newcontainer, iTargetPos, false) && (itemContainer == null || !MoveToContainer(item2, itemContainer, -1, true)))
                        {
                            Vector3 vector32 = newcontainer.dropPosition;
                            Vector3 vector33 = newcontainer.dropVelocity;
                            quaternion = new Quaternion();
                            item2.Drop(vector32, vector33, quaternion);
                        }
                        container = true;
                    }
                    else
                        if (newcontainer.CanAccept(itemBase))
                    {
                        itemBase.RemoveFromContainer();
                        itemBase.RemoveFromWorld();
                        itemBase.position = iTargetPos;
                        itemBase.SetParent(newcontainer);
                        container = true;
                    }
                    else
                        container = false;
                }
                else
                            if (iTargetPos < 0 || iTargetPos == itemBase.position || itemBase.parent.SlotTaken(itemBase, iTargetPos))
                    container = false;
                else
                {
                    itemBase.position = iTargetPos;
                    itemBase.MarkDirty();
                    container = true;
                }
            }

            return container;
        }

        private void GiveItem(BasePlayer player, Item item, BaseEntity.GiveItemReason reason = 0)
        {
            if (reason == BaseEntity.GiveItemReason.ResourceHarvested)
                player.stats.Add(string.Format("harvest.{0}", item.info.shortname), item.amount, Stats.Server | Stats.Life);

            int num = item.amount;
            if (!GiveItem(player.inventory, item, null))
            {
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity, new Quaternion());
                return;
            }

            if (string.IsNullOrEmpty(item.name))
            {
                player.Command("note.inv", new object[] { item.info.itemid, num, string.Empty, (int)reason });
                return;
            }

            player.Command("note.inv", new object[] { item.info.itemid, num, item.name, (int)reason });
        }

        private bool GiveItem(PlayerInventory inv, Item item, ItemContainer container = null)
        {
            if (item == null)
                return false;

            int num = -1;
            GetIdealPickupContainer(inv, item, ref container, ref num);
            if (container != null && MoveToContainer(item, container, num, true))
                return true;

            if (MoveToContainer(item, inv.containerMain, -1, true))
                return true;

            if (MoveToContainer(item, inv.containerBelt, -1, true))
                return true;

            return false;
        }

        private void GetIdealPickupContainer(PlayerInventory inv, Item item, ref ItemContainer container, ref int position)
        {
            if (item.info.stackable > 1)
            {
                if (inv.containerBelt != null && inv.containerBelt.FindItemByItemID(item.info.itemid) != null)
                {
                    container = inv.containerBelt;
                    return;
                }

                if (inv.containerMain != null && inv.containerMain.FindItemByItemID(item.info.itemid) != null)
                {
                    container = inv.containerMain;
                    return;
                }
            }

            if (!item.info.isUsable || item.info.HasFlag(ItemDefinition.Flag.NotStraightToBelt))
                return;

            container = inv.containerBelt;
        }

        public class PlayerInfo
        {
            public int Opened;
            public int Caught;
            public int Write;
            public int Black;
        }

        [ChatCommand("oysters")]
        void cmdTopOysters2(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;

            int i = 1;
            var top = PlayersList.Where(x => x.Value != null && covalence.Players.FindPlayerById(x.Key.ToString()) != null).ToList().OrderByDescending(p => p.Value.Opened).
                        Select(p => $"{i++}. <color=#8ABB50>{covalence.Players.FindPlayerById(p.Key.ToString()).Name}</color>: всего {p.Value.Opened}, белых {p.Value.Write}, черных {p.Value.Black}").Take(10);
            SendReply(player, $"<size=15>ТОП Ловцов устриц за текущий вайп:\n{string.Join("\n", top)}</size>");
        }

        public Dictionary<ulong, PlayerInfo> PlayersList = new Dictionary<ulong, PlayerInfo>();

        void LoadData()
        {
            try
            {
                PlayersList = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerInfo>>($"{Name}_Players");
            }
            catch
            {
                PlayersList = new Dictionary<ulong, PlayerInfo>();
            }
        }

        void SaveData()
        {
            if (PlayersList != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}_Players", PlayersList);
        }
    }
}