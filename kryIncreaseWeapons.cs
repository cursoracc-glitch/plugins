using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("kryIncreaseWeapons", "xkrystalll", "1.0.2")]
    class kryIncreaseWeapons : RustPlugin
    {
        #region Configuration
        public static ConfigData cfg;
        public class ConfigData
        {
            [JsonProperty("Улучшенное оружие")]
            public Dictionary<string, WeaponsInfo> weapons = new Dictionary<string, WeaponsInfo>();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData()
            {
                weapons = 
                {
                    {"aktest", new WeaponsInfo("АК-47", 2323019876, 10f, 0f, "rifle.ak", true, false, true)},
                    {"lrtest", new WeaponsInfo("M4", 2319796265, 0.1f, 10f, "rifle.lr300", false, true, false)}
                }
            };
            SaveConfig(config);
        } 
        void LoadConfig() 
        {
            cfg = Config.ReadObject<ConfigData>();
            SaveConfig(cfg);
        }
        void SaveConfig(object config) => Config.WriteObject(config, true);
        #endregion

        #region Fields
        public List<WeaponsInfo> weaponsVal;
        public List<string> weaponsKeys;
        public class WeaponsInfo
        {
            public WeaponsInfo(string name, ulong skin, float dmulti, float cmulti, string shortname, bool maybeRepair, bool deleteOnBreak, bool entityMultiplier)
            {
                this.shortname = shortname;
                this.name = name;
                this.skin = skin;
                this.damageMultiplier = dmulti;
                this.conditionMultiplier = cmulti;
                this.deleteOnBreak = deleteOnBreak;
                this.maybeRepair = maybeRepair;
                this.entityMultiplier = entityMultiplier;
            }
            [JsonProperty("ShortName оружия")]
            public string shortname;
            [JsonProperty("Имя оружия")]
            public string name;
            [JsonProperty("Скин оружия")]
            public ulong skin;
            [JsonProperty("Множитель урона оружия")]
            public float damageMultiplier;
            [JsonProperty("Множитель ломания оружия")]
            public float conditionMultiplier;
            [JsonProperty("Можно ли починить?")]
            public bool maybeRepair;
            [JsonProperty("Удалять оружие при поломке?")]
            public bool deleteOnBreak;
            [JsonProperty("Умножать урон по строениям?")]
            public bool entityMultiplier;
        }
        #endregion
    
        #region Hooks
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var p = info.InitiatorPlayer;
            if (p == null) { return null; }
            WeaponsInfo weapon;
            try { weapon = cfg.weapons.Values.ToList().First(x => info.Weapon.GetItem().name == x.name && x.skin == info.Weapon.skinID); } catch { return null; }
            if (!weapon.entityMultiplier)
            {
                if (entity.ToPlayer() == null || entity.IsNpc) { return null; }
            }
            if (entity.IsDead()) { return null; }
            info.damageTypes.ScaleAll(weapon.damageMultiplier); 
            return null;
        }

        object OnItemRepair(BasePlayer player, Item item)
        {
            if (item == null) return null;
            WeaponsInfo weapon;
            try{ weapon = weaponsVal.First(x => x.name == item.name && x.skin == item.skin); } catch { return null; }
            if (!weapon.maybeRepair) 
            {
                player.ChatMessage($"Вы <color=red>не можете</color> починить {item.name}!");
                return false;
            }
            return null;
        }
        private void ChangeItemSkin(Item item, ulong targetSkin)
        {           
            item.skin = targetSkin;
            item.MarkDirty();

            BaseEntity heldEntity = item.GetHeldEntity();
            if (heldEntity != null)
            {
                heldEntity.skinID = targetSkin;
                heldEntity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }           
        }
        void OnLoseCondition(Item item, ref float amount)
        {
            if (item == null) { return; }
            WeaponsInfo weapon; 
            try { weapon = weaponsVal.First(x => x.name == item.name && x.skin == item.skin); } catch { return; }
            float ConditionBefore = item.condition;
            BasePlayer p = item.GetOwnerPlayer();

            NextTick(() => 
            {
                float ConditionAfter = item.condition;
                item.condition += 0.25f;
                float SetCondition = ((ConditionBefore - ConditionAfter) * weapon.conditionMultiplier);
                item.condition -= SetCondition;
                if (item.condition == 0 || item.isBroken) 
                {
                    if (weapon.deleteOnBreak) { item.UseItem(1); }
                    return;
                }
            });
            
        }
        void OnServerInitialized()
        {
            LoadConfig();
            weaponsVal = cfg.weapons.Values.ToList();
            weaponsKeys = cfg.weapons.Keys.ToList();
        }
        #endregion

        #region commands
        [ConsoleCommand("giveweapon")]
        void GiveWeapon(ConsoleSystem.Arg args)
        {
            if (args.Connection.player != null) 
            { 
                BasePlayer p = args.Connection.player as BasePlayer;
                if (!p.IsAdmin) { return; }
            }
            if (args.Args.Count() < 2) { return; }

            List<string> argg = args.Args.ToList();
            BasePlayer target = BasePlayer.Find(argg[0]);
            int index = weaponsKeys.FindIndex(x => x == argg[1]);
            if (index.Equals(-1)) { return; }

            Item itemToGive = ItemManager.Create(ItemManager.FindItemDefinition(cfg.weapons.Values.ToList()[index].shortname), 1);
            itemToGive.name = weaponsVal.ToList()[index].name;
            target?.inventory.GiveItem(itemToGive);
            ChangeItemSkin(itemToGive, weaponsVal.ToList()[index].skin);
        }
        #endregion
    }
}