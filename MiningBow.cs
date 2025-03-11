using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("MiningBow", "Sempai#3239", "1.0.1")]
    public class MiningBow : RustPlugin
    {
        #region Config

        private PluginConfig cfg;

        public class PluginConfig
        {
            [JsonProperty("Настройки лука")]
            public bowsettings Bow = new bowsettings();
            [JsonProperty("Настройки дропа с лука")]
            public drop Drop = new drop();
        }

        public class bowdrop
        {
            [JsonProperty("Включить спавн лука в ящиках?")]
            public bool enabledropfromcrates = true;
            [JsonProperty("Shortprefabname ящика, шанс спавна")]
            public Dictionary<string, float> drop = new Dictionary<string, float>
            {
                ["crate_normal"] = 100f,
                ["crate_normal_2"] = 100f,
                ["crate_elite"] = 100f,
                ["crate_basic"] = 100f,
                ["crate_tools"] = 100f,
            };
        }

        public class bowsettings
        {
            [JsonProperty("SkinID лука")]
            public ulong skinid = 1731189052;
            [JsonProperty("Название лука")]
            public string name = "Mining Bow";
            [JsonProperty("Настройки спавна лука")]
            public bowdrop drop = new bowdrop();
        }

        public class drop
        {
            [JsonProperty("Shortprefabname предмета на который будет действовать лук")]
            public Dictionary<string, List<dropsettings>> bowdrop = new Dictionary<string, List<dropsettings>>
            {
                ["sulfur-ore"] = new List<dropsettings> { new dropsettings { shortname = "sulfur", skinid = 0, minamount = 50, maxamount = 100 }, new dropsettings { shortname = "stones", skinid = 0, minamount = 10, maxamount = 50 }, },
                ["metal-ore"] = new List<dropsettings> { new dropsettings { shortname = "metal.fragments", skinid = 0, minamount = 300, maxamount = 300 }, new dropsettings { shortname = "stones", skinid = 0, minamount = 10, maxamount = 50 }, },
                ["loot-barrel-1"] = new List<dropsettings> { new dropsettings { shortname = "scrap", skinid = 0, minamount = 50, maxamount = 100 }, new dropsettings { shortname = "hq.metal.ore", skinid = 0, minamount = 5, maxamount = 10 }, },
            };
        }

        public class dropsettings
        {
            [JsonProperty("Shortname создаваемого предмета после разрушения обьекта")]
            public string shortname;
            [JsonProperty("SkinID создаваемого предмета после разрушения обьекта")]
            public ulong skinid;
            [JsonProperty("Минимальное количество предмета для создания")]
            public int minamount;
            [JsonProperty("Максимальное количество предмета для создания")]
            public int maxamount;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<PluginConfig>();
                if (cfg == null) throw new Exception();
            }
            catch
            {
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => cfg = new PluginConfig();

        protected override void SaveConfig() => Config.WriteObject(cfg);

        #endregion

        #region Hooks

        void OnLootSpawn(LootContainer container)
        {
            if (container == null || container.inventory == null) return;
            NextTick(() =>
            {
                if (container.ShortPrefabName == "stocking_large_deployed" ||
                container.ShortPrefabName == "stocking_small_deployed") return;
                if (!cfg.Bow.drop.enabledropfromcrates) return;
                foreach (var c in cfg.Bow.drop.drop)
                {
                    if (c.Key.Contains(container.ShortPrefabName))
                    {
                        if (UnityEngine.Random.Range(0f, 100f) < c.Value)
                        {
                            if (container.inventory.itemList.Count == container.inventory.capacity) container.inventory.capacity++;

                            Item x = ItemManager.CreateByName("bow.hunting", 1, cfg.Bow.skinid);
                            if (x == null) return;
                            x.name = cfg.Bow.name;
                            x.MoveToContainer(container.inventory);
                        }
                    }
                }
            });
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (attacker == null || info == null || info.Initiator == null || info.InitiatorPlayer == null
                || info.InitiatorPlayer.IsNpc || info.Weapon == null) return;

            var entity = info.HitEntity;
            if (entity == null || info.Weapon.skinID != cfg.Bow.skinid) return;

            foreach (var item in cfg.Drop.bowdrop)
            {
                if (item.Key.Contains(entity.ShortPrefabName))
                {
                    NextTick(() =>
                    {
                        if (entity.IsDestroyed) return;
                        entity.Kill();
                    });
                    foreach (var drop in item.Value)
                    {
                        if (drop == null) return;
                        var random = UnityEngine.Random.Range(drop.minamount, drop.maxamount);
                        Item x = ItemManager.CreateByName(drop.shortname, random, drop.skinid);
                        if (x == null) return;
                        info.InitiatorPlayer.GiveItem(x);
                    }
                }
            }
        }

        #endregion

        #region Commands

        [ConsoleCommand("GiveMiningBow")]
        private void GiveMiningBow(ConsoleSystem.Arg arg)
        {
            var connection = arg.Connection;
            if (connection != null) return;
            if (!arg.HasArgs(1))
            {
                Puts(" [MiningBow callback] Command syntax error: GiveMiningBow SteamID");
                return;
            }
            else
            {
                var target = BasePlayer.FindByID(ulong.Parse(arg.Args[0]));
                if (target == null || ulong.Parse(arg.Args[0]) == 0)
                {
                    Puts(" [MiningBow callback] Player not found");
                    return;
                }
                Item x = ItemManager.CreateByName("bow.hunting", 1, cfg.Bow.skinid);
                if (x == null) return;
                x.name = cfg.Bow.name;
                target.GiveItem(x);
                Puts($" [MiningBow callback] Successful give 1x MiningBow to {target.displayName}/{target.UserIDString}");
            }
        }

        #endregion
    }
}