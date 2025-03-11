using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEngine;
using Oxide.Core.Plugins;
using Random = System.Random;
using Oxide.Core;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("BestLoot", "Admin", "1.0", ResourceId = 0)]
    [Description("Настройка лута в различных бочках и ящиках.")]

    public class BestLoot : RustPlugin
    {
        public const string LOOTTABLES_DATA = "BestLoot\\LootTables";

        // Привилегии
        public const string permReloadLoot = "bestloot.reloadloot";
        public const string permResetConfig = "bestloot.resetconfig";

        public Random rnd = new Random();

        bool initialized = false;

        private ConfigData configData;
        private Dictionary<string, LootTableEntry> lootTables = new Dictionary<string, LootTableEntry>();

        class ConfigData
        {
            public Dictionary<string, LootTableMap> tables { get; set; }
            public List<string> includeammo_items { get; set; }
        }

        // Потому что это не .NET 4.0 ...
        class LootTuple
        {
            public string item { get; set; }
            public int amount { get; set; }
            public string subitem { get; set; }
            public int subamount { get; set; }
            public bool nostack { get; set; }
            public float condition { get; set; }

            public LootTuple(string i, int a, string si = null, int sa = 0, bool ns = false, float c = 100)
            {
                item = i;
                amount = a;
                subitem = si;
                subamount = sa;
                nostack = ns;
                condition = c;
            }
        }

        // Отображает таблицу добычи в контейнерах и настройки параметров.
        class LootTableMap
        {
            public List<string> containers { get; set; }

            // Эти два параметра предназначены для простого распределения таблицы.
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public string table { get; set; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public int amount { get; set; }

            // Используйте это, если вы хотите иметь несколько таблиц со случайным шансом выпадения.
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, TableEntry> tables { get; set; }
        }

        class LootTableEntry
        {
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public bool includeammo { get; set; }

            public Dictionary<string, ItemEntry> items { get; set; } = new Dictionary<string, ItemEntry>();
        }

        class TableEntry
        {
            public string table { get; set; }
            public int chance { get; set; }
            public int amount { get; set; }
        }

        class ItemEntry
        {
            // Сумма, которая должна быть равна 0, приведет к использованию min/max.
            public int amount { get; set; }

            // Шанс выпадения в от 1-100%.
            public int chance { get; set ; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public bool nostack { get; set; }

            // Минимальная сумма предмета.
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public int min { get; set; }

            // Максимальная сумма предмета.
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public int max { get; set ; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public float? condition { get; set ; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public float? conditionmin { get; set ; }

            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public float? conditionmax { get; set ; }

            // Имеется ли в элементе подтип (вода в бутылках, топливо в шляпах)
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public string subitem { get; set; }

            // Какую часть этого подпункта включить?
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate, NullValueHandling = NullValueHandling.Ignore)]
            public int subamount { get; set; }
        }


        void Init()
        {
            #if !RUST
            throw new NotSupportedException("Плагин поддерживается только в RUST.");
            #endif

            permission.RegisterPermission(permReloadLoot, this);
            permission.RegisterPermission(permResetConfig, this);
        }

        void OnServerInitialized()
        {
            LoadVariables();

            if (initialized)
                return;

            // Список еще не заполнен, повторный обратный вызов.
            var itemList = ItemManager.itemList;
            if (itemList == null || itemList.Count == 0) {
                NextTick(OnServerInitialized);
                return;
            }

            // Удалить все существующие контейнеры.
            timer.Once(0.1f, () =>  {
                foreach (var container in UnityEngine.Object.FindObjectsOfType<LootContainer>()) {
                    FillContainer(container);
                }

                initialized = true;
            });
        }

        private void LoadVariables()
        {
            LoadConfigVariables();
            LoadLootTables();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
             configData = new ConfigData {
                includeammo_items = new List<string> {
                    "bow.hunting",
                    "crossbow",
                    "pistol.eoka"
                },
                tables = new Dictionary<string, LootTableMap> {
                    { "scrap", new LootTableMap {
                        containers = new List<string> { "trash-pile", "foodbox", "loot_barrel", "loot-barrel", "crate_normal_2.prefab" },
                        table = "ScrapTable",
                        amount = 1 }
                    },
                    { "foodboxes", new LootTableMap {
                        containers = new List<string> { "trash-pile", "foodbox" },
                        table = "FoodTable",
                        amount = 3 }
                    },
                    { "foodcrate", new LootTableMap {
                        containers = new List<string> { "crate_normal_2_food.prefab" },
                        table = "FoodTable",
                        amount = 5 }
                    },
                    { "oilbarrels", new LootTableMap {
                        containers = new List<string> { "oil_barrel" },
                        table = "FuelTable",
                        amount = 2 }
                    },
                    { "barrels", new LootTableMap {
                        containers = new List<string> { "loot_barrel", "loot-barrel" },
                        table = "BarrelTable",
                        amount = 2 }
                    },
                    { "normalcrates", new LootTableMap {
                        containers = new List<string> { "crate_normal_2.prefab" },
                        table = "NormalCrateTable",
                        amount = 2 }
                    },
                    { "greencrates", new LootTableMap {
                        containers = new List<string> { "crate_normal.prefab" },
                    //    table = "GreenCrateTable",
                    //    amount = 3 
                          tables = new Dictionary<string, TableEntry> {
                              { "GreenCrateTable", new TableEntry {
                                    chance = 50,
                                    amount = 1 } },
                              { "BarrelTable", new TableEntry {
                                   chance = 50,
                                   amount = 2 } } 
                          } }
                    },
                    { "medicalcrates", new LootTableMap {
                        containers = new List<string> { "crate_normal_2_medical.prefab" },
                        table = "MedicalCrateTable",
                        amount = 3 }
                    },
                    { "toolcrates", new LootTableMap {
                        containers = new List<string> { "crate_tools.prefab" },
                        table = "ToolsCrateTable",
                        amount = 3 }
                    },
                    { "bradleycrates", new LootTableMap {
                        containers = new List<string> { "bradley_crate" },
                        table = "BradleyCrateTable",
                        amount = 4 }
                    },
                    { "helicrates", new LootTableMap {
                        containers = new List<string> { "heli_crate" },
                        table = "HeliCrateTable",
                        amount = 4 }
                    },
                    { "bow.hunting", new LootTableMap {
                        containers = new List<string>(),
                        table = "ArrowsTable",
                        amount = 2 }
                    },
                    { "crossbow", new LootTableMap {
                        containers = new List<string>(),
                        table = "ArrowsTable",
                        amount = 2 }
                    },
                    { "pistol.eoka", new LootTableMap {
                        containers = new List<string>(),
                        table = "HandmadeShellTable",
                        amount = 1 }
                    },
                    { "shotgun.waterpipe", new LootTableMap {
                        containers = new List<string>(),
                        table = "HandmadeShellTable",
                        amount = 1 }
                    }
                }
            };

            SaveConfig(configData);
        }

        void LoadLootTables()
        {
            lootTables = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, LootTableEntry>>(LOOTTABLES_DATA);
            if (lootTables.Count == 0) {
                Puts("Empty loot table, populating with defaults.");
                CreateDefaultLootTables();
            }
        }

        void CreateDefaultLootTables()
        {
            lootTables = new Dictionary<string, LootTableEntry> {
                { "ScrapTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "scrap",    new ItemEntry { chance = 100, min = 1, max = 10 } }
                        }
                    }
                },
                { "HandmadeShellTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "ammo.handmade.shell", new ItemEntry { chance = 100, min = 5, max = 15 } }
                        }
                    }
                },
                { "FoodTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "apple",            new ItemEntry { chance = 10, min = 1, max = 10 } },
                            { "chocholate",       new ItemEntry { chance = 15, min = 1, max = 3 } },
                            { "granolabar",       new ItemEntry { chance = 15, min = 1, max = 3 } },
                            { "can.beans",        new ItemEntry { chance = 15, min = 1, max = 3 } },
                            { "can.tuna",         new ItemEntry { chance = 15, min = 1, max = 3 } },
                            { "smallwaterbottle", new ItemEntry { chance = 10, amount = 1, subitem = "water", subamount = 250, nostack = true } },
                            { "waterjug",         new ItemEntry { chance = 5, amount = 1, subitem = "water", subamount = 3000, nostack = true } },
                            { "candycane",        new ItemEntry { chance = 1, amount = 1 } },
                            { "pumpkin",          new ItemEntry { chance = 7, min = 1, max = 2, nostack = true } },
                            { "corn",             new ItemEntry { chance = 7, min = 1, max = 2, nostack = true } }
                        }
                    }
                },
                { "FuelTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "crude.oil",    new ItemEntry { chance = 50, min = 5, max = 15 } },
                            { "lowgradefuel", new ItemEntry { chance = 50, min = 10, max = 30 } }
                        }
                    }
                },
                { "BarrelTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "rope",            new ItemEntry { chance = 10, min = 1, max = 4 } },
                            { "sewingkit",       new ItemEntry { chance = 10, min = 1, max = 4 } },
                            { "tarp",            new ItemEntry { chance = 7, min = 1, max = 3 } },
                            { "roadsigns",       new ItemEntry { chance = 10, min = 1, max = 3 } },
                            { "metalpipe",       new ItemEntry { chance = 5, min = 1, max = 2 } },
                            { "metalspring",     new ItemEntry { chance = 8, min = 1, max = 2 } },
                            { "metalblade",      new ItemEntry { chance = 10, min = 1, max = 3 } },
                            { "gears",           new ItemEntry { chance = 10, min = 1, max = 3 } },
                            { "semibody",        new ItemEntry { chance = 5, amount = 1 } },
                            { "propanetank",     new ItemEntry { chance = 5, amount = 1 } },
                            { "sheetmetal",      new ItemEntry { chance = 12, min = 1, max = 2 } },
                            { "syringe.medical", new ItemEntry { chance = 5, min = 1, max = 2 } },
                            { "ammo.pistol",     new ItemEntry { chance = 3, min = 10, max = 30 } }
                        }
                    }
                },
                { "ToolsCrateTable", new LootTableEntry {
                        includeammo = true,
                        items = new Dictionary<string, ItemEntry> {
                            { "grenade.beancan",   new ItemEntry { chance = 1, min = 1, max = 2 } },
                            { "grenade.f1",        new ItemEntry { chance = 1, amount = 1 } },
                            { "coffeecan.helmet",  new ItemEntry { chance = 1, amount = 1, nostack = true } },
                            { "jacket.snow",       new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "jacket",            new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "shirt.collared",    new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "machete",           new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "bow.hunting",       new ItemEntry { chance = 10, amount = 1, nostack = true } },
                            { "crossbow",          new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "shotgun.waterpipe", new ItemEntry { chance = 4, amount = 1, nostack = true } },
                            { "pistol.eoka",       new ItemEntry { chance = 7, amount = 1, nostack = true } },
                            { "pants",             new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "hoodie",            new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "hat.wolf",          new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "pickaxe",           new ItemEntry { chance = 8, amount = 1, nostack = true } },
                            { "mace",              new ItemEntry { chance = 7, amount = 1, nostack = true } },
                            { "longsword",         new ItemEntry { chance = 4, amount = 1, nostack = true } },
                            { "hatchet",           new ItemEntry { chance = 7, amount = 1, nostack = true } },
                            { "axe.salvaged",      new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "icepick.salvaged",  new ItemEntry { chance = 5, amount = 1, nostack = true } }
                        }
                    }
                },
                { "MedicalCrateTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "jacket.snow",       new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "syringe.medical",   new ItemEntry { chance = 20, min = 1, max = 5 } },
                            { "antiradpills",      new ItemEntry { chance = 25, min = 1, max = 3 } },
                            { "bandage",           new ItemEntry { chance = 25, min = 3, max = 9 } },
                            { "black.raspberries", new ItemEntry { chance = 15, min = 1, max = 4 } },
                            { "largemedkit",       new ItemEntry { chance = 10, min = 1, max = 3 } }
                        }
                    }
                },
                { "NormalCrateTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "hazmatsuit",         new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "cctv.camera",        new ItemEntry { chance = 5, amount = 1 } },
                            { "tool.binoculars",    new ItemEntry { chance = 5, amount = 1 } },
                            { "tool.camera",        new ItemEntry { chance = 2, amount = 1 } },
                            { "targeting.computer", new ItemEntry { chance = 5, amount = 1 } },
                            { "gears",              new ItemEntry { chance = 15, min = 2, max = 5 } },
                            { "metalpipe",          new ItemEntry { chance = 10, min = 2, max = 4 } },
                            { "smgbody",            new ItemEntry { chance = 8, min = 1, max = 2 } },
                            { "semibody",           new ItemEntry { chance = 12, min = 1, max = 2 } },
                            { "riflebody",          new ItemEntry { chance = 1, amount = 1 } },
                            { "roadsigns",          new ItemEntry { chance = 12, min = 2, max = 5 } },
                            { "sewingkit",          new ItemEntry { chance = 15, min = 2, max = 6 } },
                            { "ammo.rifle",         new ItemEntry { chance = 5, min = 10, max = 30 } }

                        }
                    }
                },
                { "HeliCrateTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "rifle.ak",             new ItemEntry { chance = 8, amount = 1, nostack = true } },
                            { "rocket.launcher",      new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "pistol.m92",           new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "lmg.m249",             new ItemEntry { chance = 4, amount = 1, nostack = true } },
                            { "metal.refined",        new ItemEntry { chance = 13, min = 100, max = 200 } },
                            { "techparts",            new ItemEntry { chance = 10, min = 10, max = 20 } },
                            { "explosive.timed",      new ItemEntry { chance = 5, min = 5, max = 10 } },
                            { "riflebody",            new ItemEntry { chance = 5, min = 4, max = 8 } },
                            { "ammo.rocket.basic",    new ItemEntry { chance = 5, min = 6, max = 15 } },
                            { "ammo.rocket.fire",     new ItemEntry { chance = 5, min = 12, max = 18 } },
                            { "ammo.rocket.hv",       new ItemEntry { chance = 5, min = 12, max = 18 } },
                            { "ammo.pistol.hv",       new ItemEntry { chance = 10, min = 100, max = 300 } },
                            { "ammo.rifle.explosive", new ItemEntry { chance = 10, min = 100, max = 300 } },
                            { "ammo.rifle.hv",        new ItemEntry { chance = 10, min = 100, max = 300 } }

                        }
                    }
                },
                { "BradleyCrateTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "rifle.ak",             new ItemEntry { chance = 8, amount = 1, nostack = true } },
                            { "rocket.launcher",      new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "lmg.m249",             new ItemEntry { chance = 2, amount = 1, nostack = true } },
                            { "pistol.m92",           new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "smg.mp5",              new ItemEntry { chance = 5, amount = 1, nostack = true } },
                            { "metal.refined",        new ItemEntry { chance = 15, min = 100, max = 200 } },
                            { "techparts",            new ItemEntry { chance = 10, min = 10, max = 20 } },
                            { "explosive.timed",      new ItemEntry { chance = 5, min = 5, max = 10 } },
                            { "riflebody",            new ItemEntry { chance = 10, min = 4, max = 8 } },
                            { "ammo.rocket.basic",    new ItemEntry { chance = 5, min = 6, max = 15 } },
                            { "ammo.pistol.hv",       new ItemEntry { chance = 10, min = 100, max = 300 } },
                            { "ammo.rifle.explosive", new ItemEntry { chance = 10, min = 100, max = 300 } },
                            { "ammo.rifle.hv",        new ItemEntry { chance = 10, min = 100, max = 300 } }

                        }
                    }
                },
                { "GreenCrateTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "rifle.ak",           new ItemEntry { chance = 1, amount = 1, nostack = true, condition = 50 } },
                            { "smg.thompson",       new ItemEntry { chance = 1, amount = 1, nostack = true, conditionmin = 1, conditionmax = 20 } },
                            { "smg.mp5",            new ItemEntry { chance = 1, amount = 1, nostack = true } },
                            { "metal.refined",      new ItemEntry { chance = 14, min = 5, max = 10 } },
                            { "cctv.camera",        new ItemEntry { chance = 10, amount = 1 } },
                            { "targeting.computer", new ItemEntry { chance = 10, amount = 1 } },
                            { "techparts",          new ItemEntry { chance = 15, min = 1, max = 4 } },
                            { "metalpipe",          new ItemEntry { chance = 12, min = 4, max = 7 } },
                            { "smgbody",            new ItemEntry { chance = 10, min = 1, max = 2 } },
                            { "riflebody",          new ItemEntry { chance = 10, min = 1, max = 2 } },
                            { "roadsigns",          new ItemEntry { chance = 12, min = 2, max = 5 } },
                            { "ammo.pistol.hv",     new ItemEntry { chance = 2, min = 10, max = 30 } },
                            { "ammo.rifle.hv",      new ItemEntry { chance = 2, min = 10, max = 30 } }

                        }
                    }
                },
                { "ArrowsTable", new LootTableEntry {
                        items = new Dictionary<string, ItemEntry> {
                            { "arrow.hv",     new ItemEntry { chance = 50, min = 5, max = 15 } },
                            { "arrow.wooden", new ItemEntry { chance = 50, min = 5, max = 15 } }
                        }
                    }
                }
            };

            SaveLootTables();
        }

        void SaveLootTables() => Interface.GetMod().DataFileSystem.WriteObject(LOOTTABLES_DATA, lootTables);
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!initialized || entity == null)
                return;


            NextTick(() => {
                if (entity == null)
                    return;

                var container = entity as LootContainer;
                if (container == null)
                    return;

                FillContainer(container);
            });

        }

        // Для отладки прочности.
        void OnLoseCondition(Item item, ref float amount)
        {
            Puts("item condition is " + item.condition + " with a loss of " + amount);
        }


        void Unload()
        {
            var lootContainers = Resources.FindObjectsOfTypeAll<LootContainer>().Where(c => c.isActiveAndEnabled && !c.IsInvoking("SpawnLoot")).ToList();
            foreach (var container in lootContainers) {
                try {
                    container.Invoke("SpawnLoot", UnityEngine.Random.Range(container.minSecondsBetweenRefresh, container.maxSecondsBetweenRefresh));
                } catch {}
            }
        }

        [ConsoleCommand("reloadloot")]
        void ReloadLootConsoleCmd(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin) {
                return;
            }

            arg.ReplyWith("[BestLoot] Перезагрузка контейнеров");
            ReloadLoot();
        }

        [ChatCommand("reloadloot")]
        void ReloadLootCmd(BasePlayer player)
        {
            if (!HasPerm(player.UserIDString, permReloadLoot)) {
                return;
            }

            Puts("Перезагрузка контейнеров");
            PrintToChat(player, "[BestLoot] Перезагрузка контейнеров");
            ReloadLoot();
        }

        [ChatCommand("resetconfig")]
        void ResetConfigCmd(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !arg.Player().IsAdmin) {
                return;
            }

            arg.ReplyWith("[BestLoot] Сброс настроек по умолчанию.");
            ResetConfig();
        }

        void ResetConfigCmd(BasePlayer player)
        {
            if (!HasPerm(player.UserIDString, permResetConfig)) {
                return;
            }

            Puts("Сброс настроек loot обратно по умолчанию.");
            PrintToChat(player, "[BestLoot] Сброс конфигурации по умолчанию");
            ResetConfig();
        }

        private void ResetConfig()
        {
            LoadDefaultConfig();
            CreateDefaultLootTables();
        }

        private void ReloadLoot()
        {
            timer.Once(0.1f, () =>  {
                foreach (var container in UnityEngine.Object.FindObjectsOfType<LootContainer>()) {
                    FillContainer(container);
                }
            });
        }


        string PickLookTable(LootTableMap lootmap)
        {
            int cumulativeP = 0;
            int diceRoll = rnd.Next(1, 101);

            foreach (KeyValuePair<string, TableEntry> te in lootmap.tables) {
                cumulativeP += te.Value.chance;
                if (diceRoll < cumulativeP) {
                    return te.Key;
                }
            }

            return "";
        }

        /**
         * This is the meat of the loot table lookup. It uses a cumulative
         * distribution to pick items randomly based on their defined
         * percentages. This means if your loot tables do not add up to 100%
         * in chance, whatever percentage is left over is the percentage this
         * function will not drop loot. This is a 'feature' not a bug.
         *
         * Additionally, this function will cross reference the the lookup
         * table for ammo if includeammo is true.
         */
        List<LootTuple> PickItems(LootTableMap lootmap, bool ammolookup = true)
        {
            List<LootTuple> items = new List<LootTuple>();
            LootTableEntry tableEntry;

            int itemamount = 0;

            if (lootmap.table != null && !lootmap.table.Equals("")) {
                tableEntry = lootTables[lootmap.table];
                itemamount = lootmap.amount;
            } else if (lootmap.tables != null) {
                string table = PickLookTable(lootmap);

                if (table.Equals("")) {
                    return items;
                }

                tableEntry = lootTables[table];
                itemamount = lootmap.tables[table].amount;
            } else {
                Puts("Нет таблицы или таблиц определения.");                
                return items;
            }

            for (int c = 0; c < itemamount; c++) {
                int cumulativeP = 0;
                int diceRoll = rnd.Next(1, 101);

                foreach (KeyValuePair<string, ItemEntry> ie in tableEntry.items) {
                    cumulativeP += ie.Value.chance;
                    if (diceRoll < cumulativeP) {
                        int amount = ie.Value.amount;
                        if (amount == null || amount == 0) {
                            if (ie.Value.min == null || ie.Value.max == null) {
                                // No amounts set? Use 1.
                                amount = 1;
                            } else {
                                // Use min/max instead:
                                amount = rnd.Next(ie.Value.min, ie.Value.max + 1);
                            }
                        }

                        float condition = 100;
                        if (ie.Value.condition != null) {
                            condition = (float)ie.Value.condition;
                        } else if (ie.Value.conditionmin != null && ie.Value.conditionmax != null) {
                            condition = (float)rnd.Next((int)ie.Value.conditionmin, (int)ie.Value.conditionmax);
                        }

                        items.Add(new LootTuple(
                            ie.Key,
                            amount,
                            ie.Value.subitem,
                            ie.Value.subamount,
                            ie.Value.nostack,
                            condition
                        ));

                        if (configData.includeammo_items.Contains(ie.Key) && ammolookup && tableEntry.includeammo) {
                            if (configData.tables.ContainsKey(ie.Key)) {
                                items.AddRange(PickItems(configData.tables[ie.Key], false));
                            } else {
                                Puts("[BestLoot] WARNING: боеприпасы включены, но таблица боеприпасов не найдена для " + ie.Key);
                            }

                        }
                        break;
                    }
                }
            }

            return items;
        }

        // I think this is whats causing older loot tables to appear, this is
        // different than despawning/respawning, but actually just changes loot.
        void DisableSpawnLoot(LootContainer container)
        {
            container.minSecondsBetweenRefresh = -1;
            container.maxSecondsBetweenRefresh = 0;

            container.CancelInvoke("SpawnLoot");
        }

        void FillContainer(LootContainer container)
        {
            if (container.inventory == null) {
                container.inventory = new ItemContainer();
                container.inventory.ServerInitialize(
                    null, container.inventorySlots);
                container.inventory.GiveUID();
            }

            string containerName = container.gameObject.name.ToLower();

            Dictionary<string, LootTuple>rolledLoot = new Dictionary<string, LootTuple>();

            foreach (KeyValuePair<string, LootTableMap> tables in configData.tables) {
                var containers = tables.Value.containers;
                if (containers == null)
                    continue;

                foreach (var c in containers) {
                    if (containerName.Contains(c)) {
                        DisableSpawnLoot(container);
                        EmptyContainer(container);

                        IEnumerable<LootTuple> pickeditems = PickItems(tables.Value);
                        foreach (var i in pickeditems) {
                            // Consolidate duplicate items from tables so we're
                            // not having two slots filled with the same items.
                            if (rolledLoot.ContainsKey(i.item)) {
                                rolledLoot[i.item].amount += i.amount;
                            } else {
                                rolledLoot.Add(i.item, i);
                            }
                        }
                    }
                }
            }

            if (rolledLoot.Count <= 0)
                return;

            // Make sure there are enough available slots for fill with the
            // rolled loots.

            int containerSize = 0;
            foreach (KeyValuePair<string, LootTuple> loots in rolledLoot) {
                if (loots.Value.nostack)
                    containerSize += loots.Value.amount;
                else
                    containerSize += 1;
            }

            if (container.inventory.capacity < containerSize) {
                if (containerSize <= 36) {
                    container.inventory.capacity = containerSize;
                    container.inventorySlots = containerSize;
                } else {
                    Puts("[BestLoot] WARNING: Container " + containerName + " rolled more than 36 loot items, truncating.");
                    container.inventory.capacity = 36;
                    container.inventorySlots = 36;
                }
            }

            foreach (KeyValuePair<string, LootTuple> loots in rolledLoot) {
                if (loots.Value.nostack) {
                    for (int x = 0; x < loots.Value.amount; x++) {
                        var item = ItemManager.CreateByName(loots.Key, 1);

                        item.condition = loots.Value.condition;

                        if (item == null) {
                            Puts("[BestLoot] WARNING: No item for " + loots.Key + " found.");
                            break;
                        }

                        if (loots.Value.subitem != null && !loots.Value.subitem.Equals("")) {
                            item.contents.AddItem(ItemManager.FindItemDefinition(loots.Value.subitem), loots.Value.subamount);
                        }

                        item.MoveToContainer(container.inventory, -1, false);
                    }
                } else {
                    var item = ItemManager.CreateByName(loots.Key, loots.Value.amount);

                    item.condition = loots.Value.condition;

                    if (item == null) {
                        Puts("[BestLoot] WARNING:  No item for " + loots.Key + " found.");
                        continue;
                    }

                    if (loots.Value.subitem != null && !loots.Value.subitem.Equals("")) {
                        item.contents.AddItem(ItemManager.FindItemDefinition(loots.Value.subitem), loots.Value.subamount);
                    }

                    item.MoveToContainer(container.inventory, -1, false);
                }
            }
            container.inventory.MarkDirty();
        }

        void EmptyContainer(LootContainer container)
        {
            while (container.inventory.itemList.Count > 0) {
                var item = container.inventory.itemList[0];
                item.RemoveFromContainer();
                item.Remove(0);
            }
        }
    }
}