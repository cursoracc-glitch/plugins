#region Using
using Oxide.Core;
using System.Collections.Generic;
using System;
#endregion

namespace Oxide.Plugins
{
    [Info("CustomItems", "rdapplehappy", "1.3")]

    class CustomItems : RustPlugin
    {
        #region Variables

        List<Ore> Ores = new List<Ore>();
        private string MessageTake = "Вы получили драгоценный камень";
        private bool MessageEnabled = false, ItemRecycle = true;

        private List<ulong> Skins = new List<ulong>();

        #endregion

        #region Classes
        class Ore
        {
            public string name; // Имя
            public ulong id; // Skindid
            public string Description; // Описание
            public bool Recycled;
            public int MinCount; // Мин.количество из одного ящика/бочки и.т.д
            public int Maxcount; // Макс.количество из одного ящика/бочки и.т.д
            public Dictionary<string, int> itemsdrop = new Dictionary<string, int>(); //Выпадение ресурсов при переработке
            public int ItemId; // id предмета, который будет заменяться скином.
            public int ChanceStone; // Шанс выпадения из камня 
            public int ChanceSulfur; // Шанс выпадения из серы
            public int ChanceMetall; // Шанс выпадения из металла
            public int ChanceBarrel; // Шанс выпадения из бочки
            public int ChanceCrate_underwater; // Шанс выпадения из подводного ящика
            public int StandartCrateChance; // Шанс выпадения из обычного ящика
            public int GreenCrateChance;  // Шанс выпадения из зелён.ящика 
            public int EliteCrateChance; //  Шанс выпадения из элитного ящика
        }
        #endregion

        #region Config

        private void LoadDefaultConfig()
        {
            GetConfig("Оповещение", "Текст оповещения", ref MessageTake);
            GetConfig("Оповещение", "Включить оповещения при добыче", ref MessageEnabled);
            SaveConfig();
        }

        #endregion

        #region Hooks

        //Проверка загрузки даты
        void OnServerInitialized() { LoadData(); NextTick(() => { if (CheckData()) PrintWarning("Ores by DYXA - succesful load"); }); NextTick(() => GetSkinsId()); LoadDefaultConfig(); }

        //Замена скина. - Fix rustSkins
        Item OnItemSplit(Item item, int amount)
        {
            ulong skin = item.skin;
            if (Skins.Contains(skin))
            {
                item.amount -= amount;
                item.Initialize(item.info);
                return AddItem(item.GetOwnerPlayer(), item.name, skin, item.info.shortname, amount);

            }
            return null;
        }
        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            Item item1 = item.GetItem(); Item item2 = targetItem.GetItem();
            if (Skins.Contains(item1.skin)) if (!item2.skin.Equals(item1.skin)) return false; 
            if(Skins.Contains(item2.skin)) if (!item1.skin.Equals(item2.skin))  return false; 

            return null;
        }

        //Ores - drop
        void OnDispenserGather(ResourceDispenser dis, BaseEntity ent, Item it)
        {
            OresName name = GetOreName(it.info.name.Replace(".item", "")); if (name == OresName.Nn) return;
            Ore ore = RandomGather(name); if (ore == null) return;
            if (OreRandom(ore, name)) GiveItems(ore, ent.ToPlayer()); 
        }
        void OnLootSpawn(LootContainer container)
        {
            Crates crate = Crates.Nn;
            if (container.ShortPrefabName.Equals(Crates.crate_normal.ToString())) crate = Crates.crate_normal; 
            else if (container.ShortPrefabName.Equals(Crates.crate_normal_2.ToString())) crate = Crates.crate_normal_2; 
            else if (container.ShortPrefabName.Equals(Crates.crate_elite.ToString()))  crate = Crates.crate_elite; 
            else if (container.ShortPrefabName.Equals(Crates.loot_barrel_1.ToString()))  crate = Crates.loot_barrel_1; 
            else if (container.ShortPrefabName.Equals(Crates.loot_barrel_2.ToString()))  crate = Crates.loot_barrel_2;
            else if (container.ShortPrefabName.Equals("loot-barrel-1")) crate = Crates.lootbarrel1;
            else if (container.ShortPrefabName.Equals("loot-barrel-2")) crate = Crates.lootbarrel2;
            else if (container.ShortPrefabName.Equals(Crates.crate_underwater_advanced.ToString())) crate = Crates.crate_underwater_advanced; 
            else if (container.ShortPrefabName.Equals(Crates.crate_underwater_basic.ToString())) crate = Crates.crate_underwater_basic;

            BoxDrop(container, crate);
        }

        //Recycle item
        void CanRecycle(Recycler recycler, Item item)
        {
            foreach (var ore in Ores)
                if (item.info.itemid.Equals(ore.ItemId) && item.skin.Equals(ore.id))
                { if (!ore.Recycled) { recycler.MoveItemToOutput(item); return; }; recycler.StartRecycling(); return; }
            return;
        }
        object OnRecycleItem(Recycler recycler, Item item)
        {
            foreach (var ore in Ores)
            {
                if (item.info.itemid.Equals(ore.ItemId) && item.skin.Equals(ore.id))
                {
                    if(!ore.Recycled) { recycler.MoveItemToOutput(item);  NextTick(() => { recycler.StartRecycling(); }); return false; }
                    foreach (var items in ore.itemsdrop)
                    {
                        Item itemcreate = ItemManager.CreateByName(items.Key, items.Value * item.amount);
                        recycler.MoveItemToOutput(itemcreate);
                    }
                    item.RemoveFromWorld(); item.RemoveFromContainer();
                    return false;
                }
            }
            return null;
        }

        #endregion

        #region Functions

        private Ore FindOre(string shortname, string itemName, ulong skin)
        {
            foreach(Ore ore in Ores)
            {
                if (ore.ItemId.Equals(ItemManager.CreateByName(shortname).info.itemid) && ore.name.Contains(itemName) && ore.id.Equals(skin)){
                    return ore;
                }
            }
            return null;
        }

        private void CreateItem(string shortName, string itemName, ulong skin)
        {
            int id = ItemManager.CreateByName(shortName).info.itemid;

            Ore ore = new Ore()
            {
                name = itemName,
                id = skin,
                ItemId = id,
                Maxcount = 5,
                MinCount = 1,
                Recycled = false,
                ChanceBarrel = 10,
                ChanceMetall = 0,
                ChanceStone = 15,
                ChanceSulfur = 20,
                GreenCrateChance = 30,
                StandartCrateChance = 5,
                EliteCrateChance = 12,
                ChanceCrate_underwater = 0,
                itemsdrop = null,
                Description = ""
            };

            Ores.Add(ore);
            SaveData();

            PrintWarning("Generate new item " + itemName + ". can be changed in data/Ores.json");
        }

        #endregion

        #region Data

        protected void LoadData() => Ores = Interface.Oxide.DataFileSystem.ReadObject<List<Ore>>("Ores");
        protected void SaveData() => Interface.Oxide.DataFileSystem.WriteObject("Ores", Ores);

        protected void GenerateData()
        {
            Dictionary<string, int> Drops = new Dictionary<string, int>();
            Drops.Add("sulfur.ore", 1500); Drops.Add("metal.ore", 1250); Drops.Add("hq.metal.ore", 100);

            Ore ore = new Ore()
            {
                name = "Золото",
                Maxcount = 10,
                MinCount = 1,
                ItemId = 609049394,
                Recycled = true,
                ChanceBarrel = 10,
                ChanceMetall = 0,
                ChanceStone = 15,
                ChanceSulfur = 20,
                GreenCrateChance = 30,
                StandartCrateChance = 5,
                EliteCrateChance = 12,
                ChanceCrate_underwater = 0,
                id = 609049394,
                itemsdrop = Drops,
                Description = "<size=18>█████████████████</size>\n Золото - <size=14>дорогой камень,\n переработав который, вы получите много ресурсов!</size>"
            };

            Ores.Add(ore); SaveData(); PrintWarning("Data successfully created!");
        }

        protected bool CheckData()
        {
            if (Ores.Count == 0) { PrintWarning("Start to generate data..."); GenerateData(); return false; }
            else return true;
        }

        #endregion

        #region Api

        bool isGenerated(string shortName, string itemName, ulong skin)
        {
            if (FindOre(shortName, itemName, skin) != null) return true;
            return false;
        }

        void itemGenerate(string shortName, string itemName, ulong skin)
        {
            if (isGenerated(shortName, itemName, skin)) return;
            CreateItem(shortName, itemName, skin);
        }

        private bool OreRandom(Ore ore, OresName name)
        {
            int chance = 0;

            if (name == OresName.Barrel) chance = ore.ChanceBarrel;
            if (name == OresName.Metall) chance = ore.ChanceMetall;
            if (name == OresName.Stone) chance = ore.ChanceStone;
            if (name == OresName.EliteBox) chance = ore.EliteCrateChance;
            if (name == OresName.GreenBox) chance = ore.GreenCrateChance;
            if (name == OresName.StandartBox) chance = ore.StandartCrateChance;
            if (name == OresName.Sulfur) chance = ore.ChanceSulfur;
            if (name == OresName.WaterBox) chance = ore.ChanceCrate_underwater;

            if (chance > 100) chance = 100; if (chance < 0) chance = 0;

            if (Oxide.Core.Random.Range(1, 100) <= chance) return true;

            return false;
        }
        private OresName GetOreName(string name)
        {
            if (name.Equals("sulfur_ore")) return OresName.Sulfur;
            else if (name.Equals("metal_ore")) return OresName.Metall;
            else if (name.Equals("stones")) return OresName.Stone;
            return OresName.Nn;
        }
        private OresName GetCrateName(Crates crates)
        {
            OresName name = OresName.Nn;
            if (crates == Crates.crate_elite) name = OresName.EliteBox;
            else if (crates == Crates.crate_normal) name = OresName.GreenBox;
            else if (crates == Crates.crate_normal_2) name = OresName.StandartBox;
            else if (crates == Crates.loot_barrel_1 || crates == Crates.loot_barrel_2 || crates == Crates.lootbarrel1 || crates == Crates.lootbarrel2) name = OresName.Barrel;
            else if (crates == Crates.crate_underwater_advanced || crates == Crates.crate_underwater_basic) name = OresName.WaterBox;

            return name;
        }
        
        //LoadSkinsandIds
        private void GetSkinsId() { foreach (Ore ore in Ores) { Skins.Add(ore.id); } }

        //Cfg
        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }
            Config[menu, Key] = var;
			var СfgFiles = 8915;
        }


        //ItemGive
        private Item AddItem(BasePlayer p, string name, ulong skin, string shortname,int count)
        {
            Item winitem = ItemManager.CreateByName(shortname, count, skin);
            winitem.name = name;
            return winitem;
        }
        private void GiveItems(Ore ore, BasePlayer player)
        {
              Item winitem = ItemManager.CreateByItemID(ore.ItemId, Oxide.Core.Random.Range(ore.MinCount, ore.Maxcount + 1), ore.id);
              winitem.name = ore.name +  "\n" + ore.Description;
            if (!player.inventory.containerMain.IsFull() || !player.inventory.containerBelt.IsFull()) player.GiveItem(winitem);
            else winitem.Drop(player.transform.position, new UnityEngine.Vector3(), new UnityEngine.Quaternion());
            if (MessageEnabled) MessageSend(MessageTake, player);
        }
        private void GivetoBox(Ore ore, LootContainer cont)
        {
            Item winitem = ItemManager.CreateByItemID(ore.ItemId, Oxide.Core.Random.Range(ore.MinCount, ore.Maxcount + 1), ore.id);
            winitem.name = ore.name + "\n" + ore.Description;
            NextTick(() => winitem.MoveToContainer(cont.inventory));
        }


        enum OresName { Sulfur, Metall, Stone, Barrel, WaterBox, StandartBox, GreenBox, EliteBox, Nn }
        enum Crates { crate_normal_2, crate_elite, crate_normal, loot_barrel_1, loot_barrel_2, lootbarrel1, lootbarrel2, crate_underwater_basic, crate_underwater_advanced, Nn}

        private void MessageSend(string message, BasePlayer player)
        {
            player.SendConsoleCommand("gametip.showgametip", message);
            timer.Once(4f, () => player.SendConsoleCommand("gametip.hidegametip"));
        }

        private Ore RandomGather(OresName name)
        {
            List<Ore> OresList = new List<Ore>();
            foreach(var Ors in Ores)
            {
                if (Ors.ChanceMetall > 0 && name == OresName.Metall) OresList.Add(Ors);
                else if (Ors.ChanceStone > 0 && name == OresName.Stone) OresList.Add(Ors);
                else if (Ors.ChanceSulfur > 0 && name == OresName.Sulfur) OresList.Add(Ors);
                else if (Ors.ChanceBarrel > 0 && name == OresName.Barrel) OresList.Add(Ors);
                else if (Ors.EliteCrateChance > 0 && name == OresName.EliteBox) OresList.Add(Ors);
                else if (Ors.GreenCrateChance > 0 && name == OresName.GreenBox) OresList.Add(Ors);
                else if (Ors.ChanceCrate_underwater > 0 && name == OresName.WaterBox) OresList.Add(Ors);
                else if (Ors.StandartCrateChance > 0 && name == OresName.StandartBox) OresList.Add(Ors);
            }

            if (OresList.Count == 0) return null;
            return OresList[Oxide.Core.Random.Range(0, OresList.Count)];
        }

        private void BoxDrop(LootContainer cont, Crates crate)
        {
           OresName name = GetCrateName(crate); if (name == OresName.Nn) return;
            Ore ore = RandomGather(name); if (ore == null) return;
            if (OreRandom(ore, name)) GivetoBox(ore, cont);
        }

        #endregion
    }
}