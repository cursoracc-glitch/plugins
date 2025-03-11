using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SoPass", "Xoloproshka", "1.2.0")]
    public class SoPass : RustPlugin
    { 
         /*
         * ТИПЫ ЗАДАЧ
         * 1. Добыть
         * 2. Убить
         * 3. Скрафтить
         * 4. Изучить
         * 5. Залутать
         * 6. Поставить
         * 7. Починить
         * 8. Собрать с земли
         * 9. Улучшиить постройку(Например Оюъект задачи-foundation, Тогда в оружие или инструмент-дерево,камень,металл или мвк)
         * 10. Использовать карточку доступа
         * 11. Купить в магазине
         */
        #region CFG+DATA

        private Dictionary<ulong, PlayerData> _playerData = new Dictionary<ulong, PlayerData>();

        private ConfigData cfg { get; set; }


        internal class Reward
        {
            [JsonProperty("Шортнейм(Шортнейм предмета или название команды или название набора)")]
            public string ShortName = "";

            [JsonProperty("Кол-во")] public int Amount;
            [JsonProperty("Скинайди")] public ulong SkinId;
            [JsonProperty("Команда(Если надо)")] public string command = "";
            [JsonProperty("Использовать набор?")] public bool nabor = false;

            [JsonProperty("Картинка(Если команда или набор)")]
            public string URL = "";

            [JsonProperty(
                "Набор: Список предметов и команд(Если используете набор все параметры кроме \"Картинка\" и \"Шортнейм\" оставить пустыми и поставить использовать набор на true)")]
            public List<Items> itemList;

            internal class Items
            {
                [JsonProperty("Шортнейм")] public string ShortName = "";
                [JsonProperty("Кол-во")] public int Amount;
                [JsonProperty("Скинайди")] public ulong SkinId;
                [JsonProperty("Команда(Если надо)")] public string command = "";
            }
        }

        private class ConfigData
        {
            [JsonProperty("Список задач для классов(\"Название класса\":{ Список задач)}")]
            public Dictionary<string, List<Quest>> _listQuest;

            [JsonProperty("Список классов")] public List<ClassPlayer> _classList;

            internal class ClassPlayer
            {
                [JsonProperty("Название")] public string Name = "";
                [JsonProperty("Картинка")] public string URL = "";
                [JsonProperty("Пермищен")] public string Perm = "";
                [JsonProperty("Описание")] public string Text = "";
            }

            public static ConfigData GetNewConf()
            {
                var newConfig = new ConfigData();
                newConfig._listQuest = new Dictionary<string, List<Quest>>()
                {
                    ["Солдат"] = new List<Quest>()
                    {
                        new Quest()
                        {
                            DisplayName = "Начальный квест",
                            Lvl = 1,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "stones",
                                    URL = "",
                                    Amount = 1000,
                                    command = "",
                                    nabor = false,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "Набор дерева",
                                    URL = "https://www.pngkey.com/png/full/78-786188_shop-icon-icon-ca-hng.png",
                                    Amount = 0,
                                    command = "",
                                    nabor = true,
                                    itemList = new List<Reward.Items>()
                                    {
                                        new Reward.Items()
                                        {
                                            ShortName = "wood",
                                            Amount = 1000,
                                            command = "",
                                            SkinId = 0
                                        }
                                    }
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить лук",
                                    amount = 1,
                                    type = 3,
                                    need = "bow.hunting"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить стрелы",
                                    amount = 15,
                                    type = 3,
                                    need = "arrow.wooden"
                                }
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Подняться выше",
                            Lvl = 2,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "metal.fragments",
                                    nabor = false,
                                    Amount = 2500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.refined",
                                    nabor = false,
                                    Amount = 100,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "furnace",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить арбалет",
                                    amount = 1,
                                    type = 3,
                                    need = "crossbow"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить скоростных стрел",
                                    amount = 21,
                                    type = 3,
                                    need = "arrow.hv"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить костеной нож",
                                    amount = 1,
                                    type = 3,
                                    need = "knife.bone"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить медведя",
                                    amount = 1,
                                    type = 2,
                                    need = "bear"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть кожу",
                                    amount = 100,
                                    type = 1,
                                    need = "leather"
                                }
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Ну ты красавчик",
                            Lvl = 3,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "sulfur.ore",
                                    nabor = false,
                                    Amount = 2500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.ore",
                                    nabor = false,
                                    Amount = 3500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "wood",
                                    nabor = false,
                                    Amount = 5000,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить печку",
                                    amount = 1,
                                    type = 3,
                                    need = "crossbow"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Поставить печку",
                                    amount = 1,
                                    type = 6,
                                    need = "arrow.hv"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Изучить железный топор",
                                    amount = 1,
                                    type = 4,
                                    need = "hatchet"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть дерево",
                                    amount = 1,
                                    type = 1,
                                    need = "wood"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Убийственные цели",
                            Lvl = 4,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "syringe.medical",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "scrap",
                                    nabor = false,
                                    Amount = 450,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "lowgradefuel",
                                    nabor = false,
                                    Amount = 250,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить томсон",
                                    amount = 1,
                                    type = 3,
                                    need = "smg.thompson"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Поставить пистолетный патрон",
                                    amount = 100,
                                    type = 6,
                                    need = "ammo.pistol"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Изучить ледоруб",
                                    amount = 1,
                                    type = 4,
                                    need = "icepick.salvaged"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть серы",
                                    amount = 1,
                                    type = 1,
                                    need = "sulfur.ore"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Подготовка к жоскому финалу",
                            Lvl = 5,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "syringe.medical",
                                    nabor = false,
                                    Amount = 15,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "scrap",
                                    nabor = false,
                                    Amount = 1000,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "rifle.bolt",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Поставить верстак 3 уровня",
                                    amount = 1,
                                    type = 6,
                                    need = "workbench3.deployed"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить калаш",
                                    amount = 1,
                                    type = 3,
                                    need = "rifle.ak"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить патроны 5.56",
                                    amount = 100,
                                    type = 3,
                                    need = "ammo.rifle"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Финальная битва",
                            Lvl = 6,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "sulfur.ore",
                                    nabor = false,
                                    Amount = 7000,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.refined",
                                    nabor = false,
                                    Amount = 500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "rifle.l96",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Взорвать танк с помощью С4",
                                    amount = 1,
                                    type = 2,
                                    need = "bradleyapc",
                                    Weapon = "explosive.timed.deployed"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Сбить вертолет с калаша",
                                    amount = 1,
                                    type = 2,
                                    need = "patrolhelicopter",
                                    Weapon = "rifle.ak"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить HeavyNPC",
                                    amount = 10,
                                    type = 2,
                                    need = "heavyscientist"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить игроков с калаша",
                                    amount = 15,
                                    type = 2,
                                    need = "player",
                                    Weapon = "rifle.ak"
                                },
                            }
                        }
                    },
                    ["Фармер"] = new List<Quest>()
                    {
                        new Quest()
                        {
                            DisplayName = "Начальный квест",
                            Lvl = 1,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "stones",
                                    URL = "",
                                    Amount = 1000,
                                    command = "",
                                    nabor = false,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "wood",
                                    URL = "",
                                    Amount = 1000,
                                    command = "",
                                    nabor = false,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить лук",
                                    amount = 1,
                                    type = 3,
                                    need = "bow.hunting"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить стрелы",
                                    amount = 15,
                                    type = 3,
                                    need = "arrow.wooden"
                                }
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Подняться выше",
                            Lvl = 2,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "metal.fragments",
                                    nabor = false,
                                    Amount = 2500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.refined",
                                    nabor = false,
                                    Amount = 100,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "furnace",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить арбалет",
                                    amount = 1,
                                    type = 3,
                                    need = "crossbow"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить скоростных стрел",
                                    amount = 21,
                                    type = 3,
                                    need = "arrow.hv"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить костеной нож",
                                    amount = 1,
                                    type = 3,
                                    need = "knife.bone"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить медведя",
                                    amount = 1,
                                    type = 2,
                                    need = "bear"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть кожу",
                                    amount = 100,
                                    type = 1,
                                    need = "leather"
                                }
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Ну ты красавчик",
                            Lvl = 3,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "sulfur.ore",
                                    nabor = false,
                                    Amount = 2500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.ore",
                                    nabor = false,
                                    Amount = 3500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "wood",
                                    nabor = false,
                                    Amount = 5000,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить печку",
                                    amount = 1,
                                    type = 3,
                                    need = "crossbow"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Поставить печку",
                                    amount = 1,
                                    type = 6,
                                    need = "arrow.hv"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Изучить железный топор",
                                    amount = 1,
                                    type = 4,
                                    need = "hatchet"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть дерево",
                                    amount = 1,
                                    type = 1,
                                    need = "wood"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Убийственные цели",
                            Lvl = 4,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "syringe.medical",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "scrap",
                                    nabor = false,
                                    Amount = 450,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "lowgradefuel",
                                    nabor = false,
                                    Amount = 250,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить томсон",
                                    amount = 1,
                                    type = 3,
                                    need = "smg.thompson"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Поставить пистолетный патрон",
                                    amount = 100,
                                    type = 6,
                                    need = "ammo.pistol"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Изучить ледоруб",
                                    amount = 1,
                                    type = 4,
                                    need = "icepick.salvaged"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть серы",
                                    amount = 1,
                                    type = 1,
                                    need = "sulfur.ore"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Подготовка к жоскому финалу",
                            Lvl = 5,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "syringe.medical",
                                    nabor = false,
                                    Amount = 15,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "scrap",
                                    nabor = false,
                                    Amount = 1000,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "rifle.bolt",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Поставить верстак 3 уровня",
                                    amount = 1,
                                    type = 6,
                                    need = "workbench3.deployed"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить калаш",
                                    amount = 1,
                                    type = 3,
                                    need = "rifle.ak"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить патроны 5.56",
                                    amount = 100,
                                    type = 3,
                                    need = "ammo.rifle"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Финальная битва",
                            Lvl = 6,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "sulfur.ore",
                                    nabor = false,
                                    Amount = 7000,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.refined",
                                    nabor = false,
                                    Amount = 500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "rifle.l96",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Взорвать танк с помощью С4",
                                    amount = 1,
                                    type = 2,
                                    need = "bradleyapc",
                                    Weapon = "explosive.timed.deployed"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Сбить вертолет с калаша",
                                    amount = 1,
                                    type = 2,
                                    need = "patrolhelicopter",
                                    Weapon = "rifle.ak"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить HeavyNPC",
                                    amount = 10,
                                    type = 2,
                                    need = "heavyscientist"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить игроков с калаша",
                                    amount = 15,
                                    type = 2,
                                    need = "player",
                                    Weapon = "rifle.ak"
                                },
                            }
                        }
                    },
                    ["Строитель"] = new List<Quest>()
                    {
                        new Quest()
                        {
                            DisplayName = "Начальный квест",
                            Lvl = 1,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "stones",
                                    URL = "",
                                    Amount = 1000,
                                    command = "",
                                    nabor = false,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "wood",
                                    URL = "",
                                    Amount = 1000,
                                    command = "",
                                    nabor = false,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить лук",
                                    amount = 1,
                                    type = 3,
                                    need = "bow.hunting"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить стрелы",
                                    amount = 15,
                                    type = 3,
                                    need = "arrow.wooden"
                                }
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Подняться выше",
                            Lvl = 2,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "metal.fragments",
                                    nabor = false,
                                    Amount = 2500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.refined",
                                    nabor = false,
                                    Amount = 100,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "furnace",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить арбалет",
                                    amount = 1,
                                    type = 3,
                                    need = "crossbow"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить скоростных стрел",
                                    amount = 21,
                                    type = 3,
                                    need = "arrow.hv"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить костеной нож",
                                    amount = 1,
                                    type = 3,
                                    need = "knife.bone"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить медведя",
                                    amount = 1,
                                    type = 2,
                                    need = "bear"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть кожу",
                                    amount = 100,
                                    type = 1,
                                    need = "leather"
                                }
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Ну ты красавчик",
                            Lvl = 3,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "sulfur.ore",
                                    nabor = false,
                                    Amount = 2500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.ore",
                                    nabor = false,
                                    Amount = 3500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "wood",
                                    nabor = false,
                                    Amount = 5000,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить печку",
                                    amount = 1,
                                    type = 3,
                                    need = "crossbow"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Поставить печку",
                                    amount = 1,
                                    type = 6,
                                    need = "arrow.hv"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Изучить железный топор",
                                    amount = 1,
                                    type = 4,
                                    need = "hatchet"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть дерево",
                                    amount = 1,
                                    type = 1,
                                    need = "wood"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Убийственные цели",
                            Lvl = 4,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "syringe.medical",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "scrap",
                                    nabor = false,
                                    Amount = 450,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "lowgradefuel",
                                    nabor = false,
                                    Amount = 250,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить томсон",
                                    amount = 1,
                                    type = 3,
                                    need = "smg.thompson"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Поставить пистолетный патрон",
                                    amount = 100,
                                    type = 6,
                                    need = "ammo.pistol"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Изучить ледоруб",
                                    amount = 1,
                                    type = 4,
                                    need = "icepick.salvaged"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть серы",
                                    amount = 1,
                                    type = 1,
                                    need = "sulfur.ore"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Подготовка к жоскому финалу",
                            Lvl = 5,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "syringe.medical",
                                    nabor = false,
                                    Amount = 15,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "scrap",
                                    nabor = false,
                                    Amount = 1000,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "rifle.bolt",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Поставить верстак 3 уровня",
                                    amount = 1,
                                    type = 6,
                                    need = "workbench3.deployed"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить калаш",
                                    amount = 1,
                                    type = 3,
                                    need = "rifle.ak"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить патроны 5.56",
                                    amount = 100,
                                    type = 3,
                                    need = "ammo.rifle"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Финальная битва",
                            Lvl = 6,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "sulfur.ore",
                                    nabor = false,
                                    Amount = 7000,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.refined",
                                    nabor = false,
                                    Amount = 500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "rifle.l96",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Взорвать танк с помощью С4",
                                    amount = 1,
                                    type = 2,
                                    need = "bradleyapc",
                                    Weapon = "explosive.timed.deployed"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Сбить вертолет с калаша",
                                    amount = 1,
                                    type = 2,
                                    need = "patrolhelicopter",
                                    Weapon = "rifle.ak"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить HeavyNPC",
                                    amount = 10,
                                    type = 2,
                                    need = "heavyscientist"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить игроков с калаша",
                                    amount = 15,
                                    type = 2,
                                    need = "player",
                                    Weapon = "rifle.ak"
                                },
                            }
                        }
                    },
                    ["Донатер"] = new List<Quest>()
                    {
                        new Quest()
                        {
                            DisplayName = "Начальный квест",
                            Lvl = 1,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "stones",
                                    URL = "",
                                    Amount = 1000,
                                    command = "",
                                    nabor = false,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "wood",
                                    URL = "",
                                    Amount = 1000,
                                    command = "",
                                    nabor = false,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить лук",
                                    amount = 1,
                                    type = 3,
                                    need = "bow.hunting"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить стрелы",
                                    amount = 15,
                                    type = 3,
                                    need = "arrow.wooden"
                                }
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Подняться выше",
                            Lvl = 2,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "metal.fragments",
                                    nabor = false,
                                    Amount = 2500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.refined",
                                    nabor = false,
                                    Amount = 100,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "furnace",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить арбалет",
                                    amount = 1,
                                    type = 3,
                                    need = "crossbow"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить скоростных стрел",
                                    amount = 21,
                                    type = 3,
                                    need = "arrow.hv"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить костеной нож",
                                    amount = 1,
                                    type = 3,
                                    need = "knife.bone"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить медведя",
                                    amount = 1,
                                    type = 2,
                                    need = "bear"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть кожу",
                                    amount = 100,
                                    type = 1,
                                    need = "leather"
                                }
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Ну ты красавчик",
                            Lvl = 3,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "sulfur.ore",
                                    nabor = false,
                                    Amount = 2500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.ore",
                                    nabor = false,
                                    Amount = 3500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "wood",
                                    nabor = false,
                                    Amount = 5000,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить печку",
                                    amount = 1,
                                    type = 3,
                                    need = "crossbow"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Поставить печку",
                                    amount = 1,
                                    type = 6,
                                    need = "arrow.hv"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Изучить железный топор",
                                    amount = 1,
                                    type = 4,
                                    need = "hatchet"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть дерево",
                                    amount = 1,
                                    type = 1,
                                    need = "wood"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Убийственные цели",
                            Lvl = 4,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "syringe.medical",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "scrap",
                                    nabor = false,
                                    Amount = 450,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "lowgradefuel",
                                    nabor = false,
                                    Amount = 250,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить томсон",
                                    amount = 1,
                                    type = 3,
                                    need = "smg.thompson"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Поставить пистолетный патрон",
                                    amount = 100,
                                    type = 6,
                                    need = "ammo.pistol"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Изучить ледоруб",
                                    amount = 1,
                                    type = 4,
                                    need = "icepick.salvaged"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Добыть серы",
                                    amount = 1,
                                    type = 1,
                                    need = "sulfur.ore"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Подготовка к жоскому финалу",
                            Lvl = 5,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "syringe.medical",
                                    nabor = false,
                                    Amount = 15,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "scrap",
                                    nabor = false,
                                    Amount = 1000,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "rifle.bolt",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Поставить верстак 3 уровня",
                                    amount = 1,
                                    type = 6,
                                    need = "workbench3.deployed"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить калаш",
                                    amount = 1,
                                    type = 3,
                                    need = "rifle.ak"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Скрафтить патроны 5.56",
                                    amount = 100,
                                    type = 3,
                                    need = "ammo.rifle"
                                },
                            }
                        },
                        new Quest()
                        {
                            DisplayName = "Финальная битва",
                            Lvl = 6,
                            _listReward = new List<Reward>()
                            {
                                new Reward()
                                {
                                    ShortName = "sulfur.ore",
                                    nabor = false,
                                    Amount = 7000,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "metal.refined",
                                    nabor = false,
                                    Amount = 500,
                                    itemList = new List<Reward.Items>()
                                },
                                new Reward()
                                {
                                    ShortName = "rifle.l96",
                                    nabor = false,
                                    Amount = 1,
                                    itemList = new List<Reward.Items>()
                                }
                            },
                            _listZadach = new List<Zadachi>()
                            {
                                new Zadachi()
                                {
                                    DisplayName = "Взорвать танк с помощью С4",
                                    amount = 1,
                                    type = 2,
                                    need = "bradleyapc",
                                    Weapon = "explosive.timed.deployed"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Сбить вертолет с калаша",
                                    amount = 1,
                                    type = 2,
                                    need = "patrolhelicopter",
                                    Weapon = "rifle.ak"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить HeavyNPC",
                                    amount = 10,
                                    type = 2,
                                    need = "heavyscientist"
                                },
                                new Zadachi()
                                {
                                    DisplayName = "Убить игроков с калаша",
                                    amount = 15,
                                    type = 2,
                                    need = "player",
                                    Weapon = "rifle.ak"
                                },
                            }
                        }
                    }
                };
                newConfig._classList = new List<ClassPlayer>()
                {
                    new ClassPlayer()
                    {
                        Name = "Солдат",
                        Text = "-Ты можешь перестрелять макросника?\n-Решаешь споры 1 на 1?\n-Тогда это твой путь!",
                        URL = "https://i.imgur.com/HAmL1so.png",
                        Perm = "sopass.default"
                    },
                    new ClassPlayer()
                    {
                        Name = "Фармер",
                        Text = "-Ты лютый фармер?\n-Ты боишься стрелять?\n-Тогда это твой выбор!",
                        URL = "https://i.imgur.com/GOk1rqK.png",
                        Perm = "sopass.default"
                    },
                    new ClassPlayer()
                    {
                        Name = "Строитель",
                        Text = "-Любишь строить?\n-Хочешь получать плюшки за это?\n-Тогда тебе сюда!",
                        URL = "https://i.imgur.com/9ouiF2V.png",
                        Perm = "sopass.default"
                    },
                    new ClassPlayer()
                    {
                        Name = "Донатер",
                        Text = "-Только для донатеров!",
                        URL = "https://imgur.com/hLPnK7C.png",
                        Perm = "sopass.default"
                    },
                };
                return newConfig;
            }
        }

        protected override void LoadDefaultConfig()
        {
            cfg = ConfigData.GetNewConf();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(cfg);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        class Zadachi
        {
            [JsonProperty("Текст задачи")] public string DisplayName = "";

            [JsonProperty(
                "Тип задачи(1-Добыть, 2-Убить, 3-Скрафтить, 4-Изучить,5-Залутать, 6-Поставить,7-Починить,8-Собрать с земли)")]
            public int type = 1;

            [JsonProperty("Задача закончен(Оставлять false)")]
            public bool IsFinished = false;

            [JsonProperty("Объект задачи(Тип Калаш-rifle.ak, Игрок-player")]
            public string need = "";

            [JsonProperty("Кол-во")] public int amount = 0;

            [JsonProperty("Оружие или инструмент(Например задача убить с калаша, тогда сюда rifle.ak)")]
            public string Weapon = "";
        }

        class Quest
        {
            [JsonProperty("Название уровня")] public string DisplayName;
            [JsonProperty("Какой лвл")] public int Lvl;

            [JsonProperty(
                "Список наград(Если используете набор все параметры кроме \"Картинка\" и \"Шортнейм\" оставить пустыми и поставить использовать набор на true))")]
            public List<Reward> _listReward = new List<Reward>();

            [JsonProperty("Список задач")] public List<Zadachi> _listZadach = new List<Zadachi>();
        }

        class PlayerData
        {
            [JsonProperty("НикНейм")] public string NickName;
            [JsonProperty("Класс")] public string Klass;
            [JsonProperty("Лвл")] public int Lvl;

            [JsonProperty("Список активных заданий")]
            public List<Zadachi> listZadachi;

            [JsonProperty("Список ревардов")] public List<Reward> ListRewards;
        }

        #endregion
        #region ui  

        private static string Layer = "SoPassUI";
        private static string LayerMain = "SoPassUIMAIN";
        private string Hud = "Hud";
        private string Overlay = "Overlay";
        private string regular = "robotocondensed-regular.ttf";
        private static string Sharp = "assets/content/ui/ui.background.tile.psd";
        private static string Blur = "assets/content/ui/uibackgroundblur.mat";
        private string radial = "assets/content/ui/ui.background.transparent.radial.psd";

        private CuiPanel _fon = new CuiPanel()
        {
            RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
            CursorEnabled = true,
            Image = {Color = "0 0 0 0.87", Material = Blur}
        };

        private CuiPanel _mainFon = new CuiPanel()
        {
            RectTransform =
                {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-1920 -1080", OffsetMax = "1920 1080"},
            Image = {Color = "0.123 0.2312312 0.312312312 0"}
        };

        [ChatCommand("pass")]
        private void Start(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var cont = new CuiElementContainer();
            cont.Add(_fon, Overlay, Layer);
            CuiHelper.AddUi(player, cont);
            if (_playerData.ContainsKey(player.userID))
                LoadZadach(player, 1);
            else
            {
                StartUI(player, 0);
            }
        }

        void StartUI(BasePlayer player, int num)
        {
            CuiHelper.DestroyUi(player, LayerMain);
            var cont = new CuiElementContainer();
            cont.Add(_mainFon, Layer, LayerMain);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Button = {Close = Layer, Color = "0 0 0 0"},
                Text = {Text = ""}
            }, LayerMain);
            if (num > cfg._classList.Count - 1) num = 0;
            int q1 = num;
            int q2 = q1 + 1;
            int q3 = q1 + 2;
            if (q1 < 0) q1 = cfg._classList.Count - 1;
            if (q1 > cfg._classList.Count - 1) q1 = 0;
            if (q2 < 0)
            {
                if (q3 < 0)
                {
                    q3 = cfg._classList.Count + num + 2;
                    q2 = cfg._classList.Count + num + 1;
                    q1 = cfg._classList.Count + num;
                    if (num == 1 - cfg._classList.Count) num = 1;
                }
                else
                {
                    q2 = cfg._classList.Count - 1;
                    q1 = cfg._classList.Count - 2;
                    q3 = 0;
                }
            }

            if (q3 > cfg._classList.Count - 1) q3 = 0;

            if (q2 > cfg._classList.Count - 1)
            {
                q2 = 0;
                q3 = 1;
            }

            cont.Add(new CuiElement()
            {
                Parent = LayerMain,
                Name = LayerMain + 1,
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0.65", Material = "assets/content/ui/uibackgroundblur.mat"},
                    new CuiRectTransformComponent() {AnchorMin = "0.4126734 0.4549383", AnchorMax = "0.461979 0.592284"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = LayerMain,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = "BATTLEPASS - ВЫБЕРИ СВОЙ КЛАСС", Align = TextAnchor.MiddleCenter, FontSize = 25
                    },
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.178819 0.6364198", AnchorMax = "0.8179512 0.6660494"}
                }
            });
            if (permission.UserHasPermission(player.UserIDString, cfg._classList[q1].Perm))
            {
                cont.Add(new CuiButton()
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "0.995 0.09754422"
                    },
                    Button =
                    {
                        Color = HexToRustFormat("#E103945A"), Command = $"uisopass check {cfg._classList[q1].Name}"
                    },
                    Text =
                    {
                        Text = cfg._classList[q1].Name.ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 18,
                        Color = "0.64 0.64 0.64 1"
                    }
                }, LayerMain + 1);
            }
            else
            {
                cont.Add(new CuiButton()
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "0.995 0.09754422"
                    },
                    Button =
                    {
                        Color = HexToRustFormat("#E103947A"), Command = $""
                    },
                    Text =
                    {
                        Text = "НЕДОСТУПНО", Align = TextAnchor.MiddleCenter, FontSize = 18,
                        Color = "0.64 0.64 0.64 1"
                    }
                }, LayerMain + 1);
            }

            cont.Add(new CuiElement()
            {
                Parent = LayerMain + 1,
                Components =
                {
                    new CuiImageComponent() {Color = "1 1 1 1", Png = GetImage(cfg._classList[q1].URL)},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.1478906 0.532584", AnchorMax = "0.8274679 0.9460669"}
                }
            });

            cont.Add(new CuiElement()
            {
                Parent = LayerMain + 1,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = cfg._classList[q1].Text, Align = TextAnchor.MiddleCenter, Font = regular,
                        Color = "0.64 0.64 0.64 0.86"
                    },
                    new CuiRectTransformComponent() {AnchorMin = "0 0.1056177", AnchorMax = "0.995 0.3752807"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = LayerMain,
                Name = LayerMain + 2,
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0.65", Material = "assets/content/ui/uibackgroundblur.mat"},
                    new CuiRectTransformComponent() {AnchorMin = "0.4716969 0.4549383", AnchorMax = "0.520999 0.592284"}
                }
            });
            if (permission.UserHasPermission(player.UserIDString, cfg._classList[q2].Perm))
            {
                cont.Add(new CuiButton()
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "0.995 0.09754422"
                    },
                    Button =
                    {
                        Color = HexToRustFormat("#E103945A"), Command = $"uisopass check {cfg._classList[q2].Name}"
                    },
                    Text =
                    {
                        Text = cfg._classList[q2].Name.ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 18,
                        Color = "0.64 0.64 0.64 1"
                    }
                }, LayerMain + 2);
            }
            else
            {
                cont.Add(new CuiButton()
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "0.995 0.09754422"
                    },
                    Button =
                    {
                        Color = HexToRustFormat("#E103947A"), Command = $""
                    },
                    Text =
                    {
                        Text = "НЕДОСТУПНО", Align = TextAnchor.MiddleCenter, FontSize = 18,
                        Color = "0.64 0.64 0.64 1"
                    }
                }, LayerMain + 2);
            }

            cont.Add(new CuiElement()
            {
                Parent = LayerMain + 2,
                Components =
                {
                    new CuiImageComponent() {Color = "1 1 1 1", Png = GetImage(cfg._classList[q2].URL)},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.1478906 0.532584", AnchorMax = "0.8274679 0.9460669"}
                }
            });

            cont.Add(new CuiElement()
            {
                Parent = LayerMain + 2,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = cfg._classList[q2].Text, Align = TextAnchor.MiddleCenter, Font = regular,
                        Color = "0.64 0.64 0.64 0.86"
                    },
                    new CuiRectTransformComponent() {AnchorMin = "0 0.1056177", AnchorMax = "0.995 0.3752807"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = LayerMain,
                Name = LayerMain + 3,
                Components =
                {
                    new CuiImageComponent() {Color = "0 0 0 0.65", Material = "assets/content/ui/uibackgroundblur.mat"},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.5307257 0.4549383", AnchorMax = "0.5800229 0.592284"}
                }
            });
            if (permission.UserHasPermission(player.UserIDString, cfg._classList[q3].Perm))
            {
                cont.Add(new CuiButton()
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "0.995 0.09754422"
                    },
                    Button =
                    {
                        Color = HexToRustFormat("#E103945A"), Command = $"uisopass check {cfg._classList[q3].Name}"
                    },
                    Text =
                    {
                        Text = cfg._classList[q3].Name.ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 18,
                        Color = "0.64 0.64 0.64 1"
                    }
                }, LayerMain + 3);
            }
            else
            {
                cont.Add(new CuiButton()
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "0.995 0.09754422"
                    },
                    Button =
                    {
                        Color = HexToRustFormat("#E103947A"), Command = $""
                    },
                    Text =
                    {
                        Text = "НЕДОСТУПНО", Align = TextAnchor.MiddleCenter, FontSize = 18,
                        Color = "0.64 0.64 0.64 1"
                    }
                }, LayerMain + 3);
            }

            cont.Add(new CuiElement()
            {
                Parent = LayerMain + 3,
                Components =
                {
                    new CuiImageComponent() {Color = "1 1 1 1", Png = GetImage(cfg._classList[q3].URL)},
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.1478906 0.532584", AnchorMax = "0.8274679 0.9460669"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = LayerMain + 3,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = cfg._classList[q3].Text, Align = TextAnchor.MiddleCenter, Font = regular,
                        Color = "0.64 0.64 0.64 0.86"
                    },
                    new CuiRectTransformComponent() {AnchorMin = "0 0.1056177", AnchorMax = "0.995 0.3752807"}
                }
            });
            if (cfg._classList.Count > 3)
            {
                cont.Add(new CuiButton()
                {
                    RectTransform =
                    {
                        AnchorMin = "0.4015632 0.4549383", AnchorMax = "0.4093743 0.592284"
                    },
                    Button = {Color = "0.64 0.64 0.64 0", Command = $"UISoPass page-- {num}"},
                    Text = {Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 25}
                }, LayerMain);
                cont.Add(new CuiButton()
                {
                    RectTransform =
                    {
                        AnchorMin = "0.5836824 0.4549383", AnchorMax = "0.5914922 0.592284"
                    },
                    Button = {Color = "0.64 0.64 0.64 0", Command = $"UISoPass page++ {num}"},
                    Text = {Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 25}
                }, LayerMain);
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.4716969 0.4351849", AnchorMax = "0.520999 0.4503129"},
                    Button = {Command = "uiopeninv", Color = "0.64 0.64 0.64 0.35"},
                    Text = {Text = "ОТКРЫТЬ ИНВЕТАРЬ", Align = TextAnchor.MiddleCenter, Color = "0.64 0.64 0.64 0.66"}
                }, LayerMain);
            }

            CuiHelper.AddUi(player, cont);
        }

        private void LoadZadach(BasePlayer player, int page)
        {
            PlayerData f;
            if (!_playerData.TryGetValue(player.userID, out f)) return;
            var cont = new CuiElementContainer();
            CuiHelper.DestroyUi(player, LayerMain);
            cont.Add(_mainFon, Layer, LayerMain);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Button = {Close = Layer, Color = "0 0 0 0"},
                Text = {Text = ""}
            }, LayerMain);
            cont.Add(new CuiElement()
            {
                Parent = LayerMain,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = f.Klass.ToUpper(), Align = TextAnchor.MiddleCenter, FontSize = 30
                    },
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.4678819 0.6364198", AnchorMax = "0.5279512 0.6660494"}
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = LayerMain,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Text = $"Ваш уровень: {f.Lvl}", Align = TextAnchor.MiddleCenter, FontSize = 12
                    },
                    new CuiRectTransformComponent()
                        {AnchorMin = "0.4678819 0.6364198", AnchorMax = "0.5279512 0.6460494"}
                }
            });
            if (page <= cfg._listQuest[f.Klass].Count - 5 * page)
            {
                cont.Add(new CuiButton()
                { 
                    RectTransform = {AnchorMin = "0.6519127 0.3333333", AnchorMax = "0.6666672 0.6666666"},
                    Button = {Command = $"UISoPass page {page + 1}", Color = "0.461376 0.312312 0.31231 0"},
                    Text = {Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 30}
                }, LayerMain);
            }
  
            if (page > 1) 
            {
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.3333333 0.3333333", AnchorMax = "0.3480903 0.6666666"},
                    Button = {Command = $"UISoPass page {page - 1}", Color = "0 0 0 0"},
                    Text = {Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 30}
                }, LayerMain);
            }

            var findZadah = cfg._listQuest[f.Klass];
            if (findZadah == null)
            {
                Puts("Проблема в конфиге");
                return;
            }

            foreach (var quest in findZadah.Select((i, t) => new {A = i, B = t - (page - 1) * 5}).Skip((page - 1) * 5)
                .Take(5))
            {
                cont.Add(new CuiElement()
                {
                    Parent = LayerMain,
                    Name = Layer + quest.B,
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0.12 0.12 0.12 0.64",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"{0.3472222} {0.5799382 - Math.Floor((double) quest.B / 1) * 0.058}",
                            AnchorMax = $"{0.6531252} {0.6345679 - Math.Floor((double) quest.B / 1) * 0.058}"
                        }
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + quest.B,
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = quest.A.DisplayName.ToUpper(), Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.007815376 0.8079098", AnchorMax = "0.1708284 0.9661027"}
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + quest.B,
                    Components =
                    {
                        new CuiTextComponent()
                        {
                            Text = "Уровень: " + quest.A.Lvl, Align = TextAnchor.MiddleCenter, Font = regular,
                            FontSize = 10
                        },
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.007815376 0.6836164", AnchorMax = "0.1708284 0.8022601"}
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + quest.B,
                    Components =
                    {
                        new CuiImageComponent() {Color = "1 1 1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.007815376 0.668419", AnchorMax = "0.186152 0.6779668"}
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + quest.B,
                    Components =
                    {
                        new CuiImageComponent() {Color = "1 1 1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.1853143 0.04519862", AnchorMax = "0.1861517 0.9378539"}
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + quest.B,
                    Components =
                    {
                        new CuiImageComponent() {Color = "1 1 1 1"},
                        new CuiRectTransformComponent()
                            {AnchorMin = "0.852277 0.04519862", AnchorMax = "0.8530146 0.9378539"}
                    }
                });
                float i = 0;
                foreach (var zadachi in quest.A._listZadach)
                {
                    var find = f.listZadachi.Find(p => p.DisplayName == zadachi.DisplayName);
                    if (find != null && f.Lvl == quest.A.Lvl)
                    {
                        if (find.IsFinished)
                        {
                            cont.Add(new CuiElement()
                            {
                                Parent = Layer + quest.B,
                                Components =
                                {
                                    new CuiTextComponent()
                                    {
                                        Text = "ВЫПОЛНЕНО", Align = TextAnchor.MiddleCenter, FontSize = 12,
                                        Color = HexToRustFormat("#E10394")
                                    },
                                    new CuiRectTransformComponent()
                                    {
                                        AnchorMin = $"0.007815376 {0.485876 - i}",
                                        AnchorMax = $"0.1708284 {0.6327676 - i}"
                                    }
                                }
                            });
                        }
                        else
                        {
                            cont.Add(new CuiElement()
                            {
                                Parent = Layer + quest.B,
                                Components =
                                {
                                    new CuiTextComponent()
                                    {
                                        Text = $"{zadachi.DisplayName.ToUpper()}: {find.amount}",
                                        Align = TextAnchor.MiddleCenter, FontSize = 10,
                                        Color = HexToRustFormat("#ff4d4d8A")
                                    },
                                    new CuiRectTransformComponent()
                                    {
                                        AnchorMin = $"0.007815376 {0.485876 - i}",
                                        AnchorMax = $"0.1708284 {0.6327676 - i}"
                                    }
                                }
                            });
                        }
                    }
                    else
                    {
                        cont.Add(new CuiElement()
                        {
                            Parent = Layer + quest.B,
                            Components =
                            {
                                new CuiTextComponent()
                                {
                                    Text = $"{zadachi.DisplayName.ToUpper()}", Align = TextAnchor.MiddleCenter,
                                    FontSize = 10
                                },
                                new CuiRectTransformComponent()
                                {
                                    AnchorMin = $"0.007815376 {0.485876 - i}", AnchorMax = $"0.1708284 {0.6327676 - i}"
                                }
                            }
                        });
                    }

                    i += 0.0952f;
                }

                i = 0;
                foreach (var zadReward in quest.A._listReward)
                { 
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + quest.B,
                        Components =
                        {
                            new CuiImageComponent() {Color = "0 0 0 0", Material = Blur},
                            new CuiOutlineComponent() {Distance = "0 1", Color = "1 1 1 1"},
                            new CuiRectTransformComponent()
                                {AnchorMin = $"{0.2037455 + i} 0.1807913", AnchorMax = $"{0.2763903 + i} 0.8531086"}
                        }
                    });
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + quest.B,
                        Name = Layer + quest.B + "ok",
                        Components =
                        {
                            new CuiRawImageComponent() {Color = "1 1 1 1", Png = GetImage(zadReward.ShortName)},
                            new CuiRectTransformComponent()
                                {AnchorMin = $"{0.2037455 + i} 0.1807913", AnchorMax = $"{0.2763903 + i} 0.8531086"}
                        }
                    });
                    if (zadReward.Amount > 0)
                    {
                        cont.Add(new CuiElement()
                        {
                            Parent = Layer + quest.B + "ok",
                            Components =
                            {
                                new CuiTextComponent()
                                {
                                    Text = $"x{zadReward.Amount}", Align = TextAnchor.LowerRight, FontSize = 10
                                },
                                new CuiRectTransformComponent() {AnchorMin = $"0 0.05", AnchorMax = $"0.95 1"}
                            }
                        });
                    }

                    i += 0.0752f;
                }

                if (quest.A.Lvl > f.Lvl)
                {
                    cont.Add(new CuiButton()
                    {
                        RectTransform = {AnchorMin = $"0.8615204 0.40", AnchorMax = $"0.9886485 0.60"},
                        Button = {Command = "", Color = HexToRustFormat("#E103945A")},
                        Text = {Text = "НЕДОСТУПНО", Color = "0.64 0.64 0.64 0.64", Align = TextAnchor.MiddleCenter}
                    }, Layer + quest.B);
                }
                else if (f.Lvl == quest.A.Lvl && f.listZadachi.All(p => p.IsFinished) && f.listZadachi.Count > 0)
                {
                    cont.Add(new CuiButton()
                    {
                        RectTransform = {AnchorMin = $"0.8615204 0.40", AnchorMax = $"0.9886485 0.60"},
                        Button = {Command = $"UISoPass takereward {page}", Color = HexToRustFormat("#66a4908A")},
                        Text =
                        {
                            Text = "ЗАБРАТЬ НАГРАДУ", Color = "0.85 0.85 0.85 1", Align = TextAnchor.MiddleCenter
                        }
                    }, Layer + quest.B, Layer + "ACCEPT");
                }
                else if (f.Lvl == quest.A.Lvl && f.listZadachi.Count > 0)
                {
                    cont.Add(new CuiButton()
                    {
                        RectTransform = {AnchorMin = $"0.8615204 0.40", AnchorMax = $"0.9886485 0.60"},
                        Button = {Command = "", Color = HexToRustFormat("#ff4d4d5A")},
                        Text =
                        {
                            Text = "ВЫПОЛНЯЕТСЯ", Color = "0.64 0.64 0.64 0.64", Align = TextAnchor.MiddleCenter
                        }
                    }, Layer + quest.B, Layer + "ACCEPT");
                }
                else if (f.Lvl == quest.A.Lvl)
                {
                    cont.Add(new CuiButton()
                    {
                        RectTransform = {AnchorMin = $"0.8615204 0.40", AnchorMax = $"0.9886485 0.60"},
                        Button =
                        {
                            Command = $"UISoPass start {page}", Color = HexToRustFormat("#66a4909a")
                        },
                        Text = {Text = "ВЗЯТЬ ЗАДАНИЕ", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter}
                    }, Layer + quest.B, Layer + "ACCEPT");
                }
                else
                {
                    cont.Add(new CuiButton()
                    {
                        RectTransform = {AnchorMin = $"0.8615204 0.40", AnchorMax = $"0.9886485 0.60"},
                        Button =
                        {
                            Command = $"", Color = HexToRustFormat("#ff4d4d3A")
                        },
                        Text = {Text = "ЗАВЕРШЕНО", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter}
                    }, Layer + quest.B, Layer + "ACCEPT");
                }
            }

            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.6086459 0.6376544", AnchorMax = "0.6526042 0.6518518"},
                Button = {Command = "uiopeninv", Color = "0.64 0.64 0.64 0.35"},
                Text = {Text = "ОТКРЫТЬ ИНВЕТАРЬ", Align = TextAnchor.MiddleCenter, Color = "0.64 0.64 0.64 0.66"}
            }, LayerMain);
            CuiHelper.AddUi(player, cont);
        }

        [ConsoleCommand("uiopeninv")]
        void OpenInv(ConsoleSystem.Arg arg)
        {
            LoadInv(arg.Player(), 1);
        }


        private void LoadPanelNagrads(BasePlayer player, int page, string klass = "Солдат")
        {
            CuiHelper.DestroyUi(player, LayerMain);
            var cont = new CuiElementContainer();
            cont.Add(_mainFon, Layer, LayerMain);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Button = {Close = Layer, Color = "0 0 0 0"},
                Text = {Text = ""}
            }, LayerMain);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.4315972 0.6185169", AnchorMax = "0.569618 0.6654304"},
                Button = {Close = Layer, Color = "0 0 0 0"},
                Text = {Text = $"НАГРАДЫ ДЛЯ КЛАССА {klass.ToUpper()}", Align = TextAnchor.MiddleCenter, FontSize = 25}
            }, LayerMain);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.4979166 0.3694502", AnchorMax = "0.5470486 0.3824074"},
                Button = {Command = $"uisopass class {klass}", Color = HexToRustFormat("#66a4908A")},
                Text = {Text = "ВЫБРАТЬ КЛАСС", Align = TextAnchor.MiddleCenter}
            }, LayerMain);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.4479202 0.3694502", AnchorMax = "0.4970603 0.3824074"},
                Button = {Command = "chat.say /pass", Color = HexToRustFormat("#ff4d4d5A")},
                Text = {Text = "ВЕРНУТЬСЯ К ВЫБОРУ", Align = TextAnchor.MiddleCenter}
            }, LayerMain);
            for (int i = 0; i < 36; i++)
            {
                cont.Add(new CuiElement()
                {
                    Parent = LayerMain,
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0.25 0.25 0.25 0.64", Material = Blur, Sprite = radial
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin =
                                $"{0.3604166 + i * 0.030 - Math.Floor((double) i / 9) * 9 * 0.030} {0.566358 - Math.Floor((double) i / 9) * 0.05}",
                            AnchorMax =
                                $"{0.3881944 + i * 0.030 - Math.Floor((double) i / 9) * 9 * 0.030} {0.6132715 - Math.Floor((double) i / 9) * 0.05}"
                        }
                    }
                });
            }

            CuiHelper.AddUi(player, cont);
            LoadNagrads(player, page, klass);
        }

        private void LoadNagrads(BasePlayer player, int page, string klass)
        {
            var cont = new CuiElementContainer();
            Dictionary<string, int> nameList = new Dictionary<string, int>();
            foreach (var reawrd in from quest in cfg._listQuest[klass]
                from reawrd in quest._listReward
                where !nameList.ContainsKey(reawrd.ShortName)
                select reawrd)
            {
                nameList.Add(reawrd.ShortName, reawrd.Amount);
            }

            if (page <= nameList.Count - 36 * page)
            {
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.6519127 0.3333333", AnchorMax = "0.6666672 0.6666666"},
                    Button = {Command = $"UISoPass next {page + 1} {klass}", Color = "0.1 0.312312 0.31231 0"},
                    Text = {Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 30}
                }, LayerMain);
            }

            if (page > 1)
            {
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.3333333 0.3333333", AnchorMax = "0.3480903 0.6666666"},
                    Button = {Command = $"UISoPass next {page - 1} {klass}", Color = "0 0 0 0"},
                    Text = {Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 30}
                }, LayerMain);
            }

            foreach (var reward in nameList.Select((i, t) => new {A = i, B = t - (page - 1) * 36}).Skip((page - 1) * 36)
                .Take(36))
            {
                cont.Add(new CuiElement()
                {
                    Parent = LayerMain,
                    Name = Layer + reward.B,
                    Components =
                    {
                        new CuiRawImageComponent()
                        {
                            Color = "1 1 1 1", Png = GetImage(reward.A.Key)
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin =
                                $"{0.3604166 + reward.B * 0.030 - Math.Floor((double) reward.B / 9) * 9 * 0.030} {0.566358 - Math.Floor((double) reward.B / 9) * 0.05}",
                            AnchorMax =
                                $"{0.3881944 + reward.B * 0.030 - Math.Floor((double) reward.B / 9) * 9 * 0.030} {0.6132715 - Math.Floor((double) reward.B / 9) * 0.05}"
                        }
                    }
                });
                if (reward.A.Value > 0)
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + reward.B,
                        Components =
                        {
                            new CuiTextComponent()
                            {
                                Text = $"x{reward.A.Value} ", Align = TextAnchor.LowerRight, Font = regular,
                                FontSize = 14,
                                Color = "0.85 0.85 0.85 0.85"
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            }
                        }
                    });
                }
            }

            CuiHelper.AddUi(player, cont);
        }

        private void LoadInv(BasePlayer player, int page)
        {
            PlayerData f;
            if (!_playerData.TryGetValue(player.userID, out f)) return;
            CuiHelper.DestroyUi(player, LayerMain);
            var cont = new CuiElementContainer();
            cont.Add(_mainFon, Layer, LayerMain);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Button = {Close = Layer, Color = "0 0 0 0"},
                Text = {Text = ""}
            }, LayerMain);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.4788195 0.6283951", AnchorMax = "0.5209987 0.6425911"},
                Button = {Command = "chat.say /pass", Color = "0.64 0.64 0.64 0.35"},
                Text = {Text = "ВЕРНУТЬСЯ", Align = TextAnchor.MiddleCenter, Color = "0.64 0.64 0.64 0.66"}
            }, LayerMain);
            if (page > 1)
            {
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.4015673 0.4870409", AnchorMax = "0.4157873 0.5117263"},
                    Button =
                    {
                        Color = "0.64 0.64 0.64 0",
                        Command = $"uisopass nextpage {page - 1}"
                    },
                    Text =
                    {
                        Text = "<", Align = TextAnchor.MiddleCenter, FontSize = 25
                    }
                }, LayerMain, Layer + "NextPage-");
            }

            if (page <= f.ListRewards.Count - 20 * page)
            {
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.5868118 0.4870409", AnchorMax = "0.6010293 0.5117263"},
                    Button =
                    {
                        Color = "0.64 0.64 0.64 0",
                        Command = $"uisopass nextpage {page + 1}"
                    },
                    Text = {Text = ">", Align = TextAnchor.MiddleCenter, FontSize = 25}
                }, LayerMain, Layer + "NextPage+");
            }

            foreach (var key in f.ListRewards.Select((i, t) => new {A = i, B = t - (page - 1) * 20})
                .Skip((page - 1) * 20)
                .Take(20))
            {
                cont.Add(new CuiElement()
                {
                    Parent = LayerMain,
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0.64 0.64 0.64 0", Material = Blur, Sprite = radial
                        },
                        new CuiOutlineComponent() {Color = "1 1 1 1", Distance = "0 1"},
                        new CuiRectTransformComponent()
                        {
                            AnchorMin =
                                $"{0.4290492 + key.B * 0.030 - Math.Floor((double) key.B / 5) * 5 * 0.030} {0.5478395 - Math.Floor((double) key.B / 5) * 0.05}",
                            AnchorMax =
                                $"{0.4565972 + key.B * 0.030 - Math.Floor((double) key.B / 5) * 5 * 0.030} {0.5935185 - Math.Floor((double) key.B / 5) * 0.05}"
                        }
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = LayerMain,
                    Name = Layer + key.B,
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "0.64 0.64 0.64 0", Material = Blur, Sprite = radial
                        },
                        new CuiOutlineComponent() {Color = "1 1 1 1", Distance = "0 1"},
                        new CuiRectTransformComponent()
                        {
                            AnchorMin =
                                $"{0.4290492 + key.B * 0.030 - Math.Floor((double) key.B / 5) * 5 * 0.030} {0.5478395 - Math.Floor((double) key.B / 5) * 0.05}",
                            AnchorMax =
                                $"{0.4565972 + key.B * 0.030 - Math.Floor((double) key.B / 5) * 5 * 0.030} {0.5935185 - Math.Floor((double) key.B / 5) * 0.05}"
                        }
                    }
                });
                cont.Add(new CuiElement()
                {
                    Parent = Layer + key.B,
                    Components =
                    {
                        new CuiRawImageComponent()
                        {
                            Color = "1 1 1 1", Png = GetImage(key.A.ShortName)
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                });
                if (key.A.Amount > 0)
                {
                    cont.Add(new CuiElement()
                    {
                        Parent = Layer + key.B,
                        Components =
                        {
                            new CuiTextComponent()
                            {
                                Text = $"x{key.A.Amount} ", Align = TextAnchor.LowerRight, Font = regular,
                                FontSize = 14,
                                Color = "0.85 0.85 0.85 0.85"
                            },
                            new CuiRectTransformComponent()
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1"
                            }
                        }
                    });
                }

                cont.Add(new CuiButton()
                {
                    Button = {Command = $"UISoPass takeinv {page} {key.B}", Color = "0 0 0 0"},
                    Text = {Text = $""},
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"}
                }, Layer + key.B);
            }

            CuiHelper.AddUi(player, cont);
        }

        [ConsoleCommand("UISoPass")]
        void SoPassCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            PlayerData f;
            switch (arg.Args[0])
            {
                case "page++":
                    StartUI(arg.Player(), arg.Args[1].ToInt() + 1);
                    break;
                case "check":
                    LoadPanelNagrads(arg.Player(), 1, arg.Args[1]);
                    break;
                case "next":
                    LoadPanelNagrads(arg.Player(), arg.Args[1].ToInt(), arg.Args[2]);
                    break;
                case "page":
                    LoadZadach(player, arg.Args[1].ToInt());
                    break;
                case "takeinv":
                    if (!_playerData.TryGetValue(player.userID, out f)) return;
                    var t = f.ListRewards[arg.Args[2].ToInt()];
                    f.ListRewards.RemoveAt(arg.Args[2].ToInt());
                    LoadInv(player, arg.Args[1].ToInt());
                    if (t.nabor)
                    {
                        foreach (var itemse in t.itemList)
                        {

                            if (string.IsNullOrEmpty(itemse.command))
                            {
                                var item = ItemManager.CreateByName(itemse.ShortName, itemse.Amount, itemse.SkinId);
                                if (!arg.Player().inventory.GiveItem(item))
                                    item.Drop(player.inventory.containerMain.dropPosition,
                                        player.inventory.containerMain.dropVelocity);
                            }
                            else
                            {
                                rust.RunServerCommand(string.Format(itemse.command, player.userID));
                            }
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(t.command))
                        {
                            var item = ItemManager.CreateByName(t.ShortName, t.Amount, t.SkinId);
                            if (!arg.Player().inventory.GiveItem(item))
                                item.Drop(player.inventory.containerMain.dropPosition,
                                    player.inventory.containerMain.dropVelocity);
                        }
                        else
                        {
                            rust.RunServerCommand(string.Format(t.command, player.userID));
                        }
                        
                    }
                    break;
                case "nextpage":
                    LoadInv(player, arg.Args[1].ToInt());
                    break;
                case "page--":
                    StartUI(arg.Player(), arg.Args[1].ToInt() - 1);
                    break;
                case "class":
                    if (_playerData.TryGetValue(player.userID, out f)) return;

                    _playerData.Add(player.userID, new PlayerData()
                    { 
                        Lvl = 1,
                        NickName = player.displayName,
                        Klass = string.Join(" ", arg.Args.Skip(1).ToArray()),
                        listZadachi = new List<Zadachi>(),
                        ListRewards = new List<Reward>()
                    });
                    LoadZadach(player, 1);
                    break;
                case "start":
                    if (!_playerData.TryGetValue(player.userID, out f)) return;
                    var klass = f.Klass;
                    var findQuest = cfg._listQuest[klass].Find(p => p.Lvl == f.Lvl);
                    foreach (var zadachi in findQuest._listZadach)
                    {
                       f.listZadachi.Add(new Zadachi()
                       {
                           amount = zadachi.amount,
                           DisplayName = zadachi.DisplayName,
                           IsFinished = false,
                           need = zadachi.need,
                           type = zadachi.type,
                           Weapon = zadachi.Weapon
                       });
                    }
                    LoadZadach(player, arg.Args[1].ToInt());
                    break;
                case "takereward": 
                    if (!_playerData.TryGetValue(player.userID, out f)) return;
                    klass = f.Klass;
                    findQuest = cfg._listQuest[klass].Find(p => p.Lvl == f.Lvl);
                    foreach (var reward in findQuest._listReward)
                    {
                        f.ListRewards.Add(reward);
                    }

                    f.listZadachi.Clear();
                    f.Lvl += 1;
                    LoadZadach(player, arg.Args[1].ToInt());
                    break;
            }
        }

        #endregion
        #region Hooks
        private object OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || player == null || item == null) return null;

            NextTick(() =>
            {
                PlayerData f;
                if (!_playerData.TryGetValue(player.userID, out f)) return;
                var findZadah = f.listZadachi.FindAll(p => p.type == 1)
                    ?.Find(p => p.need == item.info.shortname);
                if (findZadah == null) return;
                if (player.GetActiveItem() == null)
                    Check(player, findZadah, item.amount, f);
                else
                    Check(player, findZadah, item.amount, f, player.GetActiveItem().info.shortname);
            });
            return null;
        }

        private object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item) =>
            OnDispenserGather(dispenser, player, item);

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            var _weapon = info?.Weapon?.GetItem()?.info?.shortname ?? info?.WeaponPrefab?.name;
            if (entity == null || info == null || !(info.Initiator as BasePlayer)) return;
            var attacker = info.InitiatorPlayer;
            PlayerData f;
            if (entity is BasePlayer)
            {
                if (IsFriends(entity.ToPlayer().userID, attacker.userID)) return;
            }
            else
            {
                if (entity.OwnerID == attacker.userID || IsFriends(entity.OwnerID, attacker.userID)) return;
            }
            if (!_playerData.TryGetValue(attacker.userID, out f)) return;
            var findZadah = f.listZadachi.FindAll(p => p.type == 2)?.Find(p => p.need.Contains(entity.ShortPrefabName));
            if (findZadah == null) return;  
            Check(attacker, findZadah, 1, f, _weapon);
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        { 
            PlayerData f;
            if (!_playerData.TryGetValue(task.owner.userID, out f)) return;
            var findZadah = f.listZadachi.FindAll(p => p.type == 3)?.Find(p => p.need.Contains(item.info.shortname));
            if (findZadah == null) return;
            Check(task.owner, findZadah, item.amount, f);
        }

        private void OnItemResearch(ResearchTable table, Item item, BasePlayer player)
        {
            PlayerData f;
            if (!_playerData.TryGetValue(player.userID, out f)) return;
            var findZadah = f.listZadachi.FindAll(p => p.type == 4)
                ?.Find(p => p.need.Contains(item.info.shortname));
            if (findZadah == null) return;
            Check(player, findZadah, 1, f);
        }  

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {  
            if (entity == null || player == null || entity.OwnerID != 0) return;
            PlayerData f;
            if (!_playerData.TryGetValue(player.userID, out f)) return;
            var findZadah = f.listZadachi.FindAll(p => p.type == 5)
                ?.Find(p => p.need.Contains(entity.ShortPrefabName));
            if (findZadah == null) return;
            entity.OwnerID = player.userID;
            Check(player, findZadah, 1, f);
        }

        private void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null) return;
            var player = BasePlayer.FindByID(entity.OwnerID);
            if (player == null) return;
            PlayerData f;
            if (!_playerData.TryGetValue(player.userID, out f)) return;
            var findZadah = f.listZadachi.FindAll(p => p.type == 6)
                ?.Find(p => p.need.Contains(entity.ShortPrefabName));
            if (findZadah == null) return;

            Check(player, findZadah, 1, f);
        } 

        private void OnItemRepair(BasePlayer player, Item item)
        {
            PlayerData f;
            if (!_playerData.TryGetValue(player.userID, out f)) return;
            var findZadah = f.listZadachi.FindAll(p => p.type == 7)
                ?.Find(p => p.need.Contains(item.info.shortname));
            if (findZadah == null) return;
            Check(player, findZadah, 1, f);
        }

        private object OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            if (entity == null || player == null || item == null) return null;

            NextTick(() =>
            {
                PlayerData f;
                if (!_playerData.TryGetValue(player.userID, out f)) return;
                var findZadah = f.listZadachi.FindAll(p => p.type == 8)
                    ?.Find(p => p.need.Contains(item.info.shortname));
                if (findZadah == null) return;
                if (player.GetActiveItem() == null)
                    Check(player, findZadah, item.amount, f);
                else
                    Check(player, findZadah, item.amount, f, player.GetActiveItem().info.shortname);
            });
            return null;
        }

        private Dictionary<int, string> _gradeList = new Dictionary<int, string>()
        {
            [1] = "дерево",
            [2] = "камень",
            [3] = "металл",
            [4] = "мвк",
        };

        private object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            PlayerData f;
            string res;
            if (!_playerData.TryGetValue(player.userID, out f)) return null;
            if (!_gradeList.TryGetValue((int) grade, out res)) return null;
            var findZadah = f.listZadachi.FindAll(p => p.type == 9)?.Find(p => p.need.Contains(entity.ShortPrefabName));
            if (findZadah == null) return null;
            if (findZadah.Weapon != res) return null;
            Check(player, findZadah, 1, f);
            return null;
        }     

        private object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            PlayerData f;
            if (cardReader.accessLevel != card.accessLevel) return null;
            if (!_playerData.TryGetValue(player.userID, out f)) return null;
            var findZadah = f.listZadachi.FindAll(p => p.type == 10)
                ?.Find(p => p.need.Contains(card.GetItem().info.shortname));
            if (findZadah == null) return null;
            Check(player, findZadah, 1, f);
            return null;
        }

        private object OnBuyVendingItem(VendingMachine machine, BasePlayer player, int sellOrderId, int numberOfTransactions)
        {
            PlayerData f;
            if (!_playerData.TryGetValue(player.userID, out f)) return null;
            var item = ItemManager.FindItemDefinition(machine.sellOrders.sellOrders[sellOrderId].itemToSellID);
            if (item == null) return null;
            var findZadah = f.listZadachi.FindAll(p => p.type == 11)?.Find(p => p.need.Contains(item.shortname));
            if (findZadah == null) return null;
            Check(player, findZadah, machine.sellOrders.sellOrders[sellOrderId].itemToSellAmount, f);
            return null;
        } 

        private void OnServerInitialized()
        {
            foreach (var reward in from q in cfg._listQuest
                from quest in q.Value
                from reward in quest._listReward
                select reward)
            {
                AddImage(reward.URL, reward.ShortName);
            }

            foreach (var classPlayer in cfg._classList)
            {
                AddImage(classPlayer.URL, classPlayer.URL);
                if (!permission.PermissionExists(classPlayer.Perm))
                    permission.RegisterPermission(classPlayer.Perm, this);
            }

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("SoPass"))
                _playerData =
                    Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>("SoPass");
        }

        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("SoPass", _playerData);
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(basePlayer, Layer);
            }
        }

        #endregion
        #region Mettods

        void Check(BasePlayer player, Zadachi findZadah, int amount, PlayerData data, string weapon = "")
        {
            if (findZadah.IsFinished) return;
            if (!string.IsNullOrEmpty(findZadah.Weapon) && !findZadah.Weapon.Contains(weapon)) return;
            
            findZadah.amount -= amount;
            if (findZadah.amount <= 0)
            {
                findZadah.IsFinished = true;
                player.SendConsoleCommand($"note.inv {ItemManager.FindItemDefinition("rifle.ak").itemid} 1 \"ЗАДАЧА\"");
                if (data.listZadachi.All(p => p.IsFinished))
                {
                    ReplySend(player, $"Все задачи выполнены заберите награду /pass");
                }
            }        
        }

        private bool IsFriends(ulong owner, ulong player)
        {
            if (SoFriends)
                return (bool) SoFriends.CallHook("IsFriend", player, owner);
            if (Friends)
                return (bool) Friends.CallHook("IsFriend", player, owner);
            return false;
        }

        #endregion
        #region Help

        [PluginReference] private Plugin ImageLibrary;

        public string GetImage(string shortname, ulong skin = 0) =>
            (string) ImageLibrary.Call("GetImage", shortname, skin);

        public bool AddImage(string url, string shortname, ulong skin = 0) =>
            (bool) ImageLibrary.Call("AddImage", url, shortname, skin);

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex)) hex = "#FFFFFFFF";
            var str = hex.Trim('#');
            if (str.Length == 6) str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }

        private void ReplySend(BasePlayer player, string message) => player.SendConsoleCommand("chat.add 0",
            new object[2]
                {76561199015371818, $"<size=18><color=purple>SoPass</color></size>\n{message}"});

        [PluginReference] private Plugin SoFriends, Friends;

        #endregion
    }
}
