﻿using System;
using System.Collections.Generic;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;
using Console = ConVar.Console;
using Layer = Rust.Layer;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("BattlePass", "LAGZYA", "1.1.1")]
    public class BattlePass : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;
        private ConfigData cfg { get; set; }

        class ConfigData
        {
            [JsonProperty("Название при выборе класса")]
            public string Lable;

            [JsonProperty("Текст под названием")] public string UnderLable;

            [JsonProperty("Название класс 1")] public string name1;
            [JsonProperty("Название класс 2")] public string name2;
            [JsonProperty("Название класс 3")] public string name3;
            [JsonProperty("Текст класс 1")] public string textname1;
            [JsonProperty("Текст класс 2")] public string textname2;
            [JsonProperty("Текст класс 3")] public string textname3;
            [JsonProperty("Картинка класс 1")] public string iconname1;
            [JsonProperty("Картинка класс 2")] public string iconname2;
            [JsonProperty("Картинка класс 3")] public string iconname3;
            [JsonProperty("После прохода ветки давать возможность пройти еще раз или выбрать другую?")] public bool newlvl;
            [JsonProperty("Текст после прохождения ветки, если включен выбор веток после прохождения ")] public string newlvltext;

            public static ConfigData GetNewConf()
            {
                ConfigData newConfig = new ConfigData();
                newConfig.Lable = "BATTLE PASS";
                newConfig.UnderLable = "Вы можете выбрать один из классов";
                newConfig.name1 = "ФАРМЕР";
                newConfig.name2 = "СОЛДАТ";
                newConfig.name3 = "СТРОИТЕЛЬ";
                newConfig.textname1 = "-Ты лютый фармер?\n-Ты боишься стрелять?\n-Тогда это твой выбор!";
                newConfig.textname2 =
                    "-Ты можешь перестрелять макросника?\n-Решаешь споры 1 на 1?\n-Тогда это твой путь!";
                newConfig.textname3 = "-Любишь строить?\n-Хочешь получать плюшки за это?\n-Тогда тебе сюда!";
                newConfig.iconname1 = "https://i.imgur.com/GOk1rqK.png";
                newConfig.iconname2 = "https://i.imgur.com/HAmL1so.png";
                newConfig.iconname3 = "https://i.imgur.com/9ouiF2V.png";
                newConfig.newlvl = false;
                newConfig.newlvltext =
                    "<color=#9e0000>[BPASS]</color> Вы закончили ветку,теперь вы можете пройти ее повторно или выбрать другую ветку, написав /bpass";
                return newConfig;
            }
        }


        protected override void LoadDefaultConfig() => cfg = ConfigData.GetNewConf();
        protected override void SaveConfig() => Config.WriteObject(cfg);

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

        public string GetImage(string shortname, ulong skin = 0) =>
            (string) ImageLibrary.Call("GetImage", shortname, skin);

        public bool AddImage(string url, string shortname, ulong skin = 0) =>
            (bool) ImageLibrary?.Call("AddImage", url, shortname, skin);

        public string Layer = "BattlePassUI";
        public List<BattleData> BattlePasss = new List<BattleData>();
        public List<BattleData2> BattlePassLevels = new List<BattleData2>();
        public Dictionary<string, bool> Bb = new Dictionary<string, bool>();
        public int NeedCount;
        public ulong LastDamagePlayer;

        public class BattleData
        {
            [JsonProperty("Ник")] public string Name { get; set; }
            [JsonProperty("СтимАйди")] public ulong SteamID { get; set; }
            [JsonProperty("Класс")] public string Class { get; set; }
            [JsonProperty("Уровень")] public int Lvl { get; set; }
            [JsonProperty("Убийств")] public int Kills { get; set; }
            [JsonProperty("Добыто камня")] public int GatherS { get; set; }
            [JsonProperty("Добыто дерева")] public int GatherW { get; set; }
            [JsonProperty("Добыто железа")] public int GatherM { get; set; }
            [JsonProperty("Добыто серы")] public int GatherWSulfur { get; set; }
            [JsonProperty("Добыто мвк")] public int GatherMVK { get; set; }
            [JsonProperty("Добыто ткани")] public int GatherCloth { get; set; }
            [JsonProperty("Добыто жира")] public int GatherFat { get; set; }
            [JsonProperty("Добыто кожи")] public int GatherKoja { get; set; }
            [JsonProperty("Собранно ткани")] public int GatherHemp { get; set; }
            [JsonProperty("Убито кабанов")] public int KillKob { get; set; }
            [JsonProperty("Убито медведей")] public int KillBear { get; set; }
            [JsonProperty("Убито лошадей")] public int KillHorse { get; set; }
            [JsonProperty("Убито куриц")] public int KillChik { get; set; }
            [JsonProperty("Убито оленей")] public int KillStag { get; set; }
            [JsonProperty("Убито волков")] public int KillVolk { get; set; }
            [JsonProperty("Убито NPC")] public int Killnpc { get; set; }
            [JsonProperty("Убито с рокетницы")] public int KillRocket { get; set; }
            [JsonProperty("Убито c томсона")] public int Kolltomson { get; set; }
            [JsonProperty("Убито с лука")] public int KillLuk { get; set; }
            [JsonProperty("Убито с калаша")] public int KillAk { get; set; }
            [JsonProperty("Убито c пешки")] public int KollPesh { get; set; }
            [JsonProperty("Убито с берданки")] public int KillBer { get; set; }
            [JsonProperty("Взорванно такнов")] public int KillTank { get; set; }
            [JsonProperty("Сбито вертолетов")] public int KillHeli { get; set; }
            [JsonProperty("Скрафченно томсонов")] public int Crafttomson { get; set; }
            [JsonProperty("Скрафченно калашей")] public int CraftAk { get; set; }

            [JsonProperty("Скрафченно железных дверей")]
            public int CraftMetalDoor { get; set; }

            [JsonProperty("Скрафченно мвк дверей")]
            public int CraftMVKDoor { get; set; }

            [JsonProperty("Скрафченно верстаков 3 лвл")]
            public int CraftWb3 { get; set; }

            [JsonProperty("Скрафченно ветряков")] public int CraftWeter { get; set; }
            [JsonProperty("Скрафченно турелей")] public int CraftTurret { get; set; }
            [JsonProperty("Скрафченно топливо")] public int CraftFuel { get; set; }

            [JsonProperty("Скрафченно разрывной 5.56")]
            public int CraftAmmo { get; set; }

            [JsonProperty("Скрафченно рокетниц")] public int CraftRocketL { get; set; }
            [JsonProperty("Скрафченно рокет")] public int CraftRocket { get; set; }

            [JsonProperty("Скрафченно нагрудников из знаков")]
            public int CraftRoad { get; set; }

            [JsonProperty("Скрафченно кофейников")]
            public int CraftCof { get; set; }

            [JsonProperty("Скрафченно каменных патрон")]
            public int CraftAmHand { get; set; }

            [JsonProperty("Скрафченно столов изучения")]
            public int CraftResea { get; set; }

            [JsonProperty("Скрафченно ремонтных верстаков")]
            public int CraftRep { get; set; }

            [JsonProperty("Скрафченно гаражек")] public int CraftGarage { get; set; }

            [JsonProperty("Скрафченно каменных стен")]
            public int CraftStoneW { get; set; }

            [JsonProperty("Скрафченно камменых ворот")]
            public int CraftStoneGates { get; set; }

            [JsonProperty("Скрафченно люков")] public int CraftLuk { get; set; }

            [JsonProperty("Скрафченно револьверов")]
            public int CraftRev { get; set; }

            [JsonProperty("Скрафченно пистолетных патрон")]
            public int CraftPammo { get; set; }

            [JsonProperty("Скрафченно 5.56")] public int Craft5 { get; set; }
            [JsonProperty("Скрафченно берданок")] public int CraftBerd { get; set; }
            [JsonProperty("Скрафченно ледорубов")] public int CraftIce { get; set; }

            [JsonProperty("Скрафченно самодельных топоров")]
            public int CraftAxe { get; set; }

            [JsonProperty("Скрафченно пешок")] public int CraftPesh { get; set; }
            [JsonProperty("Скрафченно хазматов")] public int CraftHaz { get; set; }

            [JsonProperty("Улучшенно стен в камень")]
            public int UpgradeStoneWall { get; set; }

            [JsonProperty("Улучшенно стен в железо")]
            public int UpgradeMetalWall { get; set; }

            [JsonProperty("Улучшенно стен в мвк")] public int UpgradeMvkWall { get; set; }

            [JsonProperty("Улучшенно поталков в камень")]
            public int UpgradeStoneFloor { get; set; }

            [JsonProperty("Улучшенно поталков в железо")]
            public int UpgradeMetalFloor { get; set; }

            [JsonProperty("Улучшенно поталков в мвк")]
            public int UpgradeMvkFloor { get; set; }

            [JsonProperty("Улучшенно фундаментов в камень")]
            public int UpgradeStoneFound { get; set; }

            [JsonProperty("Улучшенно фундаментов в железо")]
            public int UpgradeMetalFound { get; set; }

            [JsonProperty("Улучшенно фундаментов в мвк")]
            public int UpgradeMvkFound { get; set; }

            [JsonProperty("Улучшенно дверных проемов в камень")]
            public int UpgradeStoneDoor { get; set; }

            [JsonProperty("Улучшенно дверных проемов в железо")]
            public int UpgradeMetalDoor { get; set; }

            [JsonProperty("Улучшенно дверных проемов в мвк")]
            public int UpgradeMvkDoor { get; set; }

            [JsonProperty("Сломанно бочек любых")] public int KillBarrel { get; set; }

            [JsonProperty("Залутанно ящиков (обычных)")]
            public int LootCrateNoraml2 { get; set; }

            [JsonProperty("Залутанно ящиков (оружейных)")]
            public int LootCrateNoraml { get; set; }

            [JsonProperty("Залутанно ящиков (элитных)")]
            public int LootCrateElite { get; set; }

            [JsonProperty("Залутанно чинуков")] public int LootCrateLock { get; set; }
            [JsonProperty("Сломанно потолков")] public int DesFloor { get; set; }
            [JsonProperty("Сломанно стен")] public int DesWall { get; set; }
            [JsonProperty("Сломанно фундаментов")] public int DesFound { get; set; }

            [JsonProperty("Сломанно металлических дверей")]
            public int DesMetalDoor { get; set; }

            [JsonProperty("Сломанно мвк дверей")] public int DesMvkDoor { get; set; }
            [JsonProperty("Сломанно турелей")] public int DesTurret { get; set; }

            [JsonProperty("Сломанно каменных стен")]
            public int DesStoneW { get; set; }
        }

        public class BattleData2
        {
            [JsonProperty("Класс")] public string Class { get; set; }
            [JsonProperty("Уровни")] public List<Levels> Level { get; set; }
        }

        public class Levels
        {
            [JsonProperty("Уровень")] public int NumLvl { get; set; }
            [JsonProperty("Задачи(Не больше 5)")] public List<Zadachis> Zadachi { get; set; }

            [JsonProperty("Что получит за выполнение уровня(Не больше 4)")]
            public List<LevelItems> LevelItem { get; set; }
        }

        public class Zadachis
        {
            [JsonProperty("Список задач")] public List<Farmer> farmers { get; set; }
        }

        public class LevelItems
        {
            [JsonProperty("Shortname предмета(Или если привилегия то ее название)")]
            public string ShortName;

            [JsonProperty("Команда(если надо)")] public string command;

            [JsonProperty("Картинка(URL)(если есть команда)")]
            public string image;

            [JsonProperty("Кол-во")] public int Amount;
        }

        public class Farmer
        {
            [JsonProperty("Задача(Сюда вставлять задачу из списка)")]
            public string ShortName;

            [JsonProperty("Кол-во")] public int Amount;
        }

        void SaveData()
        {
            BattlePassLevels = Interface.Oxide.DataFileSystem.ReadObject<List<BattleData2>>("BattlePass/Levels");
            Interface.Oxide.DataFileSystem.WriteObject("BattlePass/Players", BattlePasss);
        }

        void OnServerInitialized()
        {
            BattleData2 data2 = BattlePassLevels.Find(x => x.Level != null);
            foreach (var check2 in data2.Level.Select((i, t) => new {A = i, B = t}))
            {
                foreach (var icon in check2.A.LevelItem.Select((i, t) => new {A = i, B = t}))
                {
                    AddImage(icon.A.image, icon.A.ShortName);
                }
            }

            AddImage(cfg.iconname1, "farmer");
            AddImage(cfg.iconname2, "pvp");
            AddImage(cfg.iconname3, "stroitel");
            BattlePasss = Interface.Oxide.DataFileSystem.ReadObject<List<BattleData>>("BattlePass/Players");
            BattlePassLevels = Interface.Oxide.DataFileSystem.ReadObject<List<BattleData2>>("BattlePass/Levels");
        }

        protected void LoadDefaultData()
        {
            BattlePassLevels = Interface.Oxide.DataFileSystem.ReadObject<List<BattleData2>>("BattlePass/Levels");
            BattleData2 data = BattlePassLevels.Find(x => x.Class == "farmer");
            BattleData2 data2 = BattlePassLevels.Find(x => x.Class == "pvp");
            BattleData2 data3 = BattlePassLevels.Find(x => x.Class == "builder");
            if (data == null || data2 == null || data3 == null)
            {
                data = new BattleData2()
                {
                    Class = "farmer",
                    Level = new List<Levels>
                    {
                        new Levels()
                        {
                            NumLvl = 1,
                            Zadachi = new List<Zadachis>()
                            {
                                new Zadachis()
                                {
                                    farmers = new List<Farmer>()
                                    {
                                        new Farmer()
                                        {
                                            ShortName = "Добыть камня",
                                            Amount = 5000
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Добыть серы",
                                            Amount = 3000
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Добыть ткани",
                                            Amount = 150
                                        },
                                    }
                                }
                            },
                            LevelItem = new List<LevelItems>
                            {
                                new LevelItems()
                                {
                                    ShortName = "wood",
                                    command = "",
                                    image = "",
                                    Amount = 3500
                                },
                                new LevelItems()
                                {
                                    ShortName = "pickaxe",
                                    command = "",
                                    image = "",
                                    Amount = 1
                                }
                            }
                        },
                        new Levels()
                        {
                            NumLvl = 2,
                            Zadachi = new List<Zadachis>()
                            {
                                new Zadachis()
                                {
                                    farmers = new List<Farmer>()
                                    {
                                        new Farmer()
                                        {
                                            ShortName = "Добыть камня",
                                            Amount = 1000
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Убить медведей",
                                            Amount = 5
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Добыть кожи",
                                            Amount = 300
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Собрать ткани",
                                            Amount = 250
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Убить оленей",
                                            Amount = 1
                                        },
                                    }
                                }
                            },
                            LevelItem = new List<LevelItems>
                            {
                                new LevelItems()
                                {
                                    ShortName = "furnace",
                                    command = "",
                                    image = "",
                                    Amount = 1
                                },
                                new LevelItems()
                                {
                                    ShortName = "lowgradefuel",
                                    command = "",
                                    image = "",
                                    Amount = 150
                                }
                            }
                        }
                    }
                };
                data2 = new BattleData2()
                {
                    Class = "pvp",
                    Level = new List<Levels>
                    {
                        new Levels()
                        {
                            NumLvl = 1,
                            Zadachi = new List<Zadachis>()
                            {
                                new Zadachis()
                                {
                                    farmers = new List<Farmer>()
                                    {
                                        new Farmer()
                                        {
                                            ShortName = "Убить игроков",
                                            Amount = 5,
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Убить лошадей",
                                            Amount = 10,
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Сбить вертолетов",
                                            Amount = 1,
                                        }
                                    }
                                }
                            },
                            LevelItem = new List<LevelItems>
                            {
                                new LevelItems()
                                {
                                    ShortName = "scrap",
                                    command = "",
                                    image = "",
                                    Amount = 1000
                                }
                            }
                        },
                        new Levels()
                        {
                            NumLvl = 2,
                            Zadachi = new List<Zadachis>()
                            {
                                new Zadachis()
                                {
                                    farmers = new List<Farmer>()
                                    {
                                        new Farmer()
                                        {
                                            ShortName = "Убить NPC",
                                            Amount = 5,
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Убить медведей",
                                            Amount = 1,
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Залутать обычных ящиков",
                                            Amount = 5,
                                        }
                                    }
                                }
                            },
                            LevelItem = new List<LevelItems>
                            {
                                new LevelItems()
                                {
                                    ShortName = "sulfur",
                                    command = "",
                                    image = "",
                                    Amount = 2500
                                },
                                new LevelItems()
                                {
                                    ShortName = "metal.fragments",
                                    command = "",
                                    image = "",
                                    Amount = 1500
                                }
                            }
                        }
                    }
                };
                data3 = new BattleData2()
                {
                    Class = "builder",
                    Level = new List<Levels>
                    {
                        new Levels()
                        {
                            NumLvl = 1,
                            Zadachi = new List<Zadachis>()
                            {
                                new Zadachis()
                                {
                                    farmers = new List<Farmer>()
                                    {
                                        new Farmer()
                                        {
                                            ShortName = "Улучшить стен в камень",
                                            Amount = 10,
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Скрафтить металлических дверей",
                                            Amount = 1,
                                        },
                                    }
                                }
                            },
                            LevelItem = new List<LevelItems>
                            {
                                new LevelItems()
                                {
                                    ShortName = "wood",
                                    command = "",
                                    image = "",
                                    Amount = 5000
                                },
                                new LevelItems()
                                {
                                    ShortName = "stones",
                                    command = "",
                                    image = "",
                                    Amount = 5000
                                },
                                new LevelItems()
                                {
                                    ShortName = "metal.fragments",
                                    command = "",
                                    image = "",
                                    Amount = 2500
                                }
                            }
                        },
                        new Levels()
                        {
                            NumLvl = 2,
                            Zadachi = new List<Zadachis>()
                            {
                                new Zadachis()
                                {
                                    farmers = new List<Farmer>()
                                    {
                                        new Farmer()
                                        {
                                            ShortName = "Улучшить стен в металл",
                                            Amount = 15,
                                        },
                                        new Farmer()
                                        {
                                            ShortName = "Добыть металла",
                                            Amount = 5000,
                                        },
                                    }
                                }
                            },
                            LevelItem = new List<LevelItems>
                            {
                                new LevelItems()
                                {
                                    ShortName = "wood",
                                    command = "",
                                    image = "",
                                    Amount = 5000
                                },
                                new LevelItems()
                                {
                                    ShortName = "stones",
                                    command = "",
                                    image = "",
                                    Amount = 5000
                                },
                                new LevelItems()
                                {
                                    ShortName = "metal.fragments",
                                    command = "",
                                    image = "",
                                    Amount = 2500
                                }
                            }
                        }
                    }
                };
                BattlePassLevels.Add(data);
                BattlePassLevels.Add(data2);
                BattlePassLevels.Add(data3);
                Interface.Oxide.DataFileSystem.WriteObject("BattlePass/Levels", BattlePassLevels);
            }
        }

        void Init() => LoadDefaultData();

        void MainUi(BasePlayer player)
        {
            CuiElementContainer cont = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);
            BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
            if (data != null)
            {
                BattleData2 data2 = BattlePassLevels.FindLast(x => x.Class == data.Class);
                if (!cfg.newlvl || data.Lvl <= data2.Level.Count)
                {
                    fon(player, 0);
                    return;
                }
            }

            cont.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Image =
                {
                    Color = "0 0 0 0.669929", Sprite = "",
                    Material = ""
                },
                FadeOut = 0.1f,
            }, "Overlay", Layer);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Button = {Color = "0 0 0 0", Command = "Destroy_UI_BattlePass"},
                Text = {Text = ""}
            }, Layer);
            cont.Add(new CuiLabel()
            {
                Text = {Text = cfg.Lable, Align = TextAnchor.MiddleCenter, FontSize = 50},
                RectTransform = {AnchorMin = "0.3192708 0.9037015", AnchorMax = "0.6677083 1.007393"}
            }, Layer);
            cont.Add(new CuiLabel()
            {
                Text = {Text = cfg.UnderLable, Align = TextAnchor.UpperCenter, FontSize = 14},
                RectTransform = {AnchorMin = "0.3713542 0.825", AnchorMax = "0.6171875 0.9111111"}
            }, Layer);
            //Право
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0 0 0 0.3712924"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.5802084 0.3638888",
                        AnchorMax = "0.7192656 0.7481496"
                    }
                }
            });
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.5802036 0.312963", AnchorMax = "0.7192661 0.3611111"},
                Button = {Color = "0 1 0.1529412 0.4734576", Command = "UI_check_battlepass builder"},
                Text = {Text = cfg.name3, Align = TextAnchor.MiddleCenter, FontSize = 15}
            }, Layer);
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "1 1 1 1",
                        Png = GetImage("stroitel")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.606766 0.5416667",
                        AnchorMax = "0.6958286 0.7101852"
                    }
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Color = "1 1 1 1",
                        Text = cfg.textname3,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.5843726 0.3638898",
                        AnchorMax = "00.7151018 0.5324126"
                    }
                }
            });
            //
            //Лево
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0 0 0 0.3712924"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2666672 0.3638888",
                        AnchorMax = "0.405728 0.7481496"
                    }
                }
            });
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.2666667 0.312963", AnchorMax = "0.4057291 0.3611111"},
                Button = {Color = "0 1 0.1529412 0.4734576", Command = "UI_check_battlepass farmer"},
                Text = {Text = cfg.name1, Align = TextAnchor.MiddleCenter, FontSize = 15}
            }, Layer);
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "1 1 1 1",
                        Png = GetImage("farmer")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2911458 0.5416667",
                        AnchorMax = "0.3802083 0.7101852"
                    }
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Color = "1 1 1 1",
                        Text = cfg.textname1,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.2708357 0.3638898",
                        AnchorMax = "0.401565 0.5324126"
                    }
                }
            });
            //
            //Центр
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "0 0 0 0.3712924"
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.4234396 0.3638888",
                        AnchorMax = "0.5624974 0.7481496"
                    }
                }
            });
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.4234351 0.312963", AnchorMax = "0.5624976 0.3611111"},
                Button = {Color = "0 1 0.1529412 0.4734576", Command = "UI_check_battlepass pvp"},
                Text = {Text = cfg.name2, Align = TextAnchor.MiddleCenter, FontSize = 15}
            }, Layer);
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent()
                    {
                        Color = "1 1 1 1",
                        Png = GetImage("pvp")
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.4484351 0.5416667",
                        AnchorMax = "0.5374976 0.7101852"
                    }
                }
            });
            cont.Add(new CuiElement()
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent()
                    {
                        Color = "1 1 1 1",
                        Text = cfg.textname2,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent()
                    {
                        AnchorMin = "0.4276042 0.3638898",
                        AnchorMax = "0.5583334 0.5324126"
                    }
                }
            });
            CuiHelper.AddUi(player, cont);
        }

        [ConsoleCommand("nextpage_UI_BattlePass")]
        void nextpages(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            LevelsUi(player, Convert.ToInt32(arg.Args[0]));
        }

        [ConsoleCommand("Destroy_UI_BattlePass")]
        void Destroyui(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            CuiHelper.DestroyUi(player, Layer);
        }

        [ConsoleCommand("UI_check_battlepass")]
        void CheckClass(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer cont = new CuiElementContainer();
            cont.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Image =
                {
                    Color = "0 0 0 0.669929", Sprite = "",
                    Material = ""
                },
                FadeOut = 0.1f,
            }, "Overlay", Layer);
            cont.Add(new CuiLabel()
            {
                Text = {Text = "BATTLE PASS", Align = TextAnchor.MiddleCenter, FontSize = 50},
                RectTransform = {AnchorMin = "0.3192708 0.9037015", AnchorMax = "0.6677083 1.007393"},
                FadeOut = 0.1f,
            }, Layer);
            cont.Add(new CuiLabel()
            {
                Text = {Text = "Вы уверены?", Align = TextAnchor.MiddleCenter, FontSize = 14},
                RectTransform = {AnchorMin = "0.3062498 0.54352", AnchorMax = "0.6833329 0.6472236"},
                FadeOut = 0.1f,
            }, Layer);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0.4937468 0.4453717", AnchorMax = "0.6796842 0.5379643"},
                Button = {Color = "0.9764706 0 0 0.3005632", Command = "chat.say /bpass"},
                Text = {Text = "НЕТ", Align = TextAnchor.MiddleCenter, FontSize = 15},
                FadeOut = 0.1f,
            }, Layer);
            if (arg.Args[0] == "farmer")
            {
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.3067705 0.4453717", AnchorMax = "0.4927079 0.5379643"},
                    Button = {Color = "0 1 0.1213166 0.2219746", Command = "UI_Choose_Class_battlepass farmer"},
                    Text = {Text = "ДА", Align = TextAnchor.MiddleCenter, FontSize = 15},
                    FadeOut = 0.1f,
                }, Layer);
            }

            if (arg.Args[0] == "pvp")
            {
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.3067705 0.4453717", AnchorMax = "0.4927079 0.5379643"},
                    Button = {Color = "0 1 0.1213166 0.2219746", Command = "UI_Choose_Class_battlepass pvp"},
                    Text = {Text = "ДА", Align = TextAnchor.MiddleCenter, FontSize = 15},
                    FadeOut = 0.1f,
                }, Layer);
            }

            if (arg.Args[0] == "builder")
            {
                cont.Add(new CuiButton()
                {
                    RectTransform = {AnchorMin = "0.3067705 0.4453717", AnchorMax = "0.4927079 0.5379643"},
                    Button = {Color = "0 1 0.1213166 0.2219746", Command = "UI_Choose_Class_battlepass builder"},
                    Text = {Text = "ДА", Align = TextAnchor.MiddleCenter, FontSize = 15},
                    FadeOut = 0.1f,
                }, Layer);
            }

            CuiHelper.AddUi(player, cont);
        }

        [ConsoleCommand("UI_choose_Class_battlepass")]
        void chooseclass(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            CuiHelper.DestroyUi(player, Layer);
            BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
            if (data == null)
            {
                data = new BattleData()
                {
                    Name = player.displayName,
                    SteamID = player.userID,
                    Class = arg.Args[0],
                    Lvl = 1
                };
                BattlePasss.Add(data);
                SaveData();
            }
            else
            {
                ClearStats(player);
                data.Class = arg.Args[0];
                data.Lvl = 1;
            }
        }

        public bool secc41;

        void fon(BasePlayer player, int page)
        {
            BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
            CuiElementContainer cont = new CuiElementContainer();
            cont.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Image =
                {
                    Color = "0 0 0 0.3", Sprite = "",
                    Material = "assets/content/ui/uibackgroundblur.mat"
                },
            }, "Overlay", Layer);
            cont.Add(new CuiButton()
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Button = {Color = "0 0 0 0", Command = "Destroy_UI_BattlePass", FadeIn = 0.5f},
                Text = {Text = ""}
            }, Layer);
            if (data.Class == "pvp")
            {
                cont.Add(new CuiLabel()
                {
                    Text = {Text = cfg.name2, Align = TextAnchor.MiddleCenter, FontSize = 50},
                    RectTransform = {AnchorMin = "0.3192708 0.9037015", AnchorMax = "0.6677083 1.007393"}
                }, Layer);
                cont.Add(new CuiLabel()
                {
                    Text =
                    {
                        Text = "Выполняй задания и получай бонусы", Align = TextAnchor.UpperCenter,
                        FontSize = 14
                    },
                    RectTransform = {AnchorMin = "0.3713542 0.825", AnchorMax = "0.6171875 0.9111111"}
                }, Layer);
            }

            if (data.Class == "farmer")
            {
                cont.Add(new CuiLabel()
                {
                    Text = {Text = cfg.name1, Align = TextAnchor.MiddleCenter, FontSize = 50},
                    RectTransform = {AnchorMin = "0.3192708 0.9037015", AnchorMax = "0.6677083 1.007393"}
                }, Layer);
                cont.Add(new CuiLabel()
                {
                    Text =
                    {
                        Text = "Выполняй задания и получай бонусы", Align = TextAnchor.UpperCenter,
                        FontSize = 14
                    },
                    RectTransform = {AnchorMin = "0.3713542 0.825", AnchorMax = "0.6171875 0.9111111"}
                }, Layer);
            }

            if (data.Class == "builder")
            {
                cont.Add(new CuiLabel()
                {
                    Text = {Text = cfg.name3, Align = TextAnchor.MiddleCenter, FontSize = 50},
                    RectTransform = {AnchorMin = "0.3192708 0.9037015", AnchorMax = "0.6677083 1.007393"}
                }, Layer);
                cont.Add(new CuiLabel()
                {
                    Text =
                    {
                        Text = "Выполняй задания и получай бонусы", Align = TextAnchor.UpperCenter,
                        FontSize = 14
                    },
                    RectTransform = {AnchorMin = "0.3713542 0.825", AnchorMax = "0.6171875 0.9111111"}
                }, Layer);
            }

            CuiHelper.AddUi(player, cont);
            LevelsUi(player, 0);
        }

        void LevelsUi(BasePlayer player, int page)
        {
            secc41 = true;
            BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
            BattleData2 data2 = BattlePassLevels.Find(x => data.Class == x.Class);
            CuiElementContainer cont = new CuiElementContainer();
            foreach (var check2 in data2.Level.Select((i, t) => new {A = i, B = t - (page - 0) * 3})
                .Skip((page - 0) * 3).Take(3))
            {
                CuiHelper.DestroyUi(player, Layer + $".{check2.B}3");
                CuiHelper.DestroyUi(player, Layer + $".{check2.B}2");
                cont.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin =
                                $"{0.05416666 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.6435185 - Math.Floor((double) check2.B) * 0.20}",
                            AnchorMax =
                                $"{0.940625 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.8185186 - Math.Floor((double) check2.B) * 0.20}",
                            OffsetMax = "0 0"
                        },
                        Button =
                        {
                            Color = "0 0 0 0.4027277",
                            Material = "",
                            Command = $""
                        },
                        Text =
                        {
                            Text = "", Align = TextAnchor.LowerRight,
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 15
                        },
                        FadeOut = 0.02f,
                    }, Layer, Layer + $".{check2.B}3");
                cont.Add(new CuiElement
                {
                    Parent = Layer + $".{check2.B}3",
                    Components =
                    {
                        new CuiImageComponent()
                        {
                            Color = "1 1 1 1"
                        },
                        new CuiRectTransformComponent()
                        {
                            AnchorMin =
                                $"{0.2191534 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.08333292 - Math.Floor((double) check2.B) * 0.01}",
                            AnchorMax =
                                $"{0.2215037 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.9423069 - Math.Floor((double) check2.B) * 0.01}",
                            OffsetMax = "0 0"
                        }
                    }
                });
                if (page > 0)
                {
                    cont.Add(new CuiButton
                    {
                        RectTransform =
                            {AnchorMin = "0.006249997 0.4490741", AnchorMax = "0.03020833 0.5555555"},
                        Button = {Color = "0 0 0 0", Command = $"nextpage_UI_Battlepass {page - 1}"},
                        Text = {Text = "<", FontSize = 50},
                        FadeOut = 1f,
                    }, Layer);
                }

                if (data2.Level.Count > 3 * (1 + page))
                {
                    cont.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0.9734374 0.4620373", AnchorMax = "0.9963542 0.5564818"},
                        Button = {Color = "0 0 0 0", Command = $"nextpage_UI_Battlepass {page + 1}"},
                        Text = {Text = ">", FontSize = 50},
                        FadeOut = 1f,
                    }, Layer);
                }

                cont.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin =
                                $"{0.06562501 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.6648148 - Math.Floor((double) check2.B) * 0.20}",
                            AnchorMax =
                                $"{0.2296875 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.7935185 - Math.Floor((double) check2.B) * 0.20}",
                            OffsetMax = "0 0"
                        },
                        Button =
                        {
                            Color = "0 0 0 0",
                            Material = "",
                            Command = $""
                        },
                        Text =
                        {
                            Text = $"Уровень {check2.A.NumLvl} ", Align = TextAnchor.UpperCenter,
                            Font = "robotocondensed-regular.ttf", FontSize = 20
                        },

                        FadeOut = 0.01f,
                    }, Layer, Layer + $".{check2.B}2");
                foreach (var check in check2.A.Zadachi.Select((i, t) => new {A = i, B = t}))
                {
                    foreach (var chec in check.A.farmers.Select((i, t) => new {A = i, B = t}))
                    {
                        if (data.Lvl == check2.A.NumLvl)
                        {
                            if (chec.A.ShortName.Contains("Добыть камня"))
                            {
                                if (data.GatherS < chec.A.Amount)
                                {
                                    NeedCount = data.GatherS;
                                }
                                else
                                {
                                    NeedCount = data.GatherS;
                                }
                            }

                            if (chec.A.ShortName.Contains("Добыть серы"))
                            {
                                if (data.GatherWSulfur < chec.A.Amount)
                                {
                                    NeedCount = data.GatherWSulfur;
                                }
                                else
                                {
                                    NeedCount = data.GatherWSulfur;
                                }
                            }

                            if (chec.A.ShortName.Contains("Добыть металла"))
                            {
                                if (data.GatherM < chec.A.Amount)
                                {
                                    NeedCount = data.GatherM;
                                }
                                else
                                {
                                    NeedCount = data.GatherM;
                                }
                            }

                            if (chec.A.ShortName.Contains("Добыть дерево"))
                            {
                                if (data.GatherW < chec.A.Amount)
                                {
                                    NeedCount = data.GatherW;
                                }
                                else
                                {
                                    NeedCount = data.GatherW;
                                }
                            }

                            if (chec.A.ShortName.Contains("Добыть мвк"))
                            {
                                if (data.GatherMVK < chec.A.Amount)
                                {
                                    NeedCount = data.GatherMVK;
                                }
                                else
                                {
                                    NeedCount = data.GatherMVK;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить игроков"))
                            {
                                if (data.Kills < chec.A.Amount)
                                {
                                    NeedCount = data.Kills;
                                }
                                else
                                {
                                    NeedCount = data.Kills;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить NPC"))
                            {
                                if (data.Killnpc < chec.A.Amount)
                                {
                                    NeedCount = data.Killnpc;
                                }
                                else
                                {
                                    NeedCount = data.Killnpc;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить медведей"))
                            {
                                if (data.KillBear < chec.A.Amount)
                                {
                                    NeedCount = data.KillBear;
                                }
                                else
                                {
                                    NeedCount = data.KillBear;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить кабанов"))
                            {
                                if (data.KillKob < chec.A.Amount)
                                {
                                    NeedCount = data.KillKob;
                                }
                                else
                                {
                                    NeedCount = data.KillKob;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить волков"))
                            {
                                if (data.KillVolk < chec.A.Amount)
                                {
                                    NeedCount = data.KillVolk;
                                }
                                else
                                {
                                    NeedCount = data.KillVolk;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить куриц"))
                            {
                                if (data.KillChik < chec.A.Amount)
                                {
                                    NeedCount = data.KillChik;
                                }
                                else
                                {
                                    NeedCount = data.KillChik;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить лошадей"))
                            {
                                if (data.KillHorse < chec.A.Amount)
                                {
                                    NeedCount = data.KillHorse;
                                }
                                else
                                {
                                    NeedCount = data.KillHorse;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить оленей"))
                            {
                                if (data.KillStag < chec.A.Amount)
                                {
                                    NeedCount = data.KillStag;
                                }
                                else
                                {
                                    NeedCount = data.KillStag;
                                }
                            }

                            if (chec.A.ShortName.Contains("Взорвать танков"))
                            {
                                if (data.KillTank < chec.A.Amount)
                                {
                                    NeedCount = data.KillTank;
                                }
                                else
                                {
                                    NeedCount = data.KillTank;
                                }
                            }

                            if (chec.A.ShortName.Contains("Сбить вертолетов"))
                            {
                                if (data.KillHeli < chec.A.Amount)
                                {
                                    NeedCount = data.KillHeli;
                                }
                                else
                                {
                                    NeedCount = data.KillHeli;
                                }
                            }

                            if (chec.A.ShortName.Contains("Сломать бочек"))
                            {
                                if (data.KillBarrel < chec.A.Amount)
                                {
                                    NeedCount = data.KillBarrel;
                                }
                                else
                                {
                                    NeedCount = data.KillBarrel;
                                }
                            }

                            if (chec.A.ShortName.Contains("Сломать каменных стен"))
                            {
                                if (data.DesStoneW < chec.A.Amount)
                                {
                                    NeedCount = data.DesStoneW;
                                }
                                else
                                {
                                    NeedCount = data.DesStoneW;
                                }
                            }

                            if (chec.A.ShortName.Contains("Собрать ткани"))
                            {
                                if (data.GatherHemp < chec.A.Amount)
                                {
                                    NeedCount = data.GatherHemp;
                                }
                                else
                                {
                                    NeedCount = data.GatherHemp;
                                }
                            }

                            if (chec.A.ShortName.Contains("Добыть ткани"))
                            {
                                if (data.GatherCloth < chec.A.Amount)
                                {
                                    NeedCount = data.GatherCloth;
                                }
                                else
                                {
                                    NeedCount = data.GatherCloth;
                                }
                            }

                            if (chec.A.ShortName.Contains("Добыть жира"))
                            {
                                if (data.GatherFat < chec.A.Amount)
                                {
                                    NeedCount = data.GatherFat;
                                }
                                else
                                {
                                    NeedCount = data.GatherFat;
                                }
                            }

                            if (chec.A.ShortName.Contains("Добыть кожи"))
                            {
                                if (data.GatherKoja < chec.A.Amount)
                                {
                                    NeedCount = data.GatherKoja;
                                }
                                else
                                {
                                    NeedCount = data.GatherKoja;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить калашей"))
                            {
                                if (data.CraftAk < chec.A.Amount)
                                {
                                    NeedCount = data.CraftAk;
                                }
                                else
                                {
                                    NeedCount = data.CraftAk;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить томсонов"))
                            {
                                if (data.Crafttomson < chec.A.Amount)
                                {
                                    NeedCount = data.Crafttomson;
                                }
                                else
                                {
                                    NeedCount = data.Crafttomson;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить металлических дверей"))
                            {
                                if (data.CraftMetalDoor < chec.A.Amount)
                                {
                                    NeedCount = data.CraftMetalDoor;
                                }
                                else
                                {
                                    NeedCount = data.CraftMetalDoor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить мвк дверей"))
                            {
                                if (data.CraftMVKDoor < chec.A.Amount)
                                {
                                    NeedCount = data.CraftMVKDoor;
                                }
                                else
                                {
                                    NeedCount = data.CraftMVKDoor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить ветряков"))
                            {
                                if (data.CraftWeter < chec.A.Amount)
                                {
                                    NeedCount = data.CraftWeter;
                                }
                                else
                                {
                                    NeedCount = data.CraftWeter;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить топливо"))
                            {
                                if (data.CraftFuel < chec.A.Amount)
                                {
                                    NeedCount = data.CraftFuel;
                                }
                                else
                                {
                                    NeedCount = data.CraftFuel;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить рокет"))
                            {
                                if (data.CraftRocket < chec.A.Amount)
                                {
                                    NeedCount = data.CraftRocket;
                                }
                                else
                                {
                                    NeedCount = data.CraftRocket;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить 5.56 разрывной"))
                            {
                                if (data.CraftAmmo < chec.A.Amount)
                                {
                                    NeedCount = data.CraftAmmo;
                                }
                                else
                                {
                                    NeedCount = data.CraftAmmo;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить рокетниц"))
                            {
                                if (data.CraftRocketL < chec.A.Amount)
                                {
                                    NeedCount = data.CraftRocketL;
                                }
                                else
                                {
                                    NeedCount = data.CraftRocketL;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить брони из знаков"))
                            {
                                if (data.CraftRoad < chec.A.Amount)
                                {
                                    NeedCount = data.CraftRoad;
                                }
                                else
                                {
                                    NeedCount = data.CraftRoad;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить кофейников"))
                            {
                                if (data.CraftCof < chec.A.Amount)
                                {
                                    NeedCount = data.CraftCof;
                                }
                                else
                                {
                                    NeedCount = data.CraftCof;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить каменных патрон"))
                            {
                                if (data.CraftAmHand < chec.A.Amount)
                                {
                                    NeedCount = data.CraftAmHand;
                                }
                                else
                                {
                                    NeedCount = data.CraftAmHand;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить столов изучения"))
                            {
                                if (data.CraftResea < chec.A.Amount)
                                {
                                    NeedCount = data.CraftResea;
                                }
                                else
                                {
                                    NeedCount = data.CraftResea;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить ремонтных верстаков"))
                            {
                                if (data.CraftRep < chec.A.Amount)
                                {
                                    NeedCount = data.CraftRep;
                                }
                                else
                                {
                                    NeedCount = data.CraftRep;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить гаражек"))
                            {
                                if (data.CraftGarage < chec.A.Amount)
                                {
                                    NeedCount = data.CraftGarage;
                                }
                                else
                                {
                                    NeedCount = data.CraftGarage;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить каменных ворот"))
                            {
                                if (data.CraftStoneGates < chec.A.Amount)
                                {
                                    NeedCount = data.CraftStoneGates;
                                }
                                else
                                {
                                    NeedCount = data.CraftStoneGates;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить каменных стен"))
                            {
                                if (data.CraftStoneW < chec.A.Amount)
                                {
                                    NeedCount = data.CraftStoneW;
                                }
                                else
                                {
                                    NeedCount = data.CraftStoneW;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить люков"))
                            {
                                if (data.CraftLuk < chec.A.Amount)
                                {
                                    NeedCount = data.CraftLuk;
                                }
                                else
                                {
                                    NeedCount = data.CraftLuk;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить револьверов"))
                            {
                                if (data.CraftRev < chec.A.Amount)
                                {
                                    NeedCount = data.CraftRev;
                                }
                                else
                                {
                                    NeedCount = data.CraftRev;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить пистолетных патрон"))
                            {
                                if (data.CraftPammo < chec.A.Amount)
                                {
                                    NeedCount = data.CraftPammo;
                                }
                                else
                                {
                                    NeedCount = data.CraftPammo;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить 5.56 патрон"))
                            {
                                if (data.Craft5 < chec.A.Amount)
                                {
                                    NeedCount = data.Craft5;
                                }
                                else
                                {
                                    NeedCount = data.Craft5;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить берданок"))
                            {
                                if (data.CraftBerd < chec.A.Amount)
                                {
                                    NeedCount = data.CraftBerd;
                                }
                                else
                                {
                                    NeedCount = data.CraftBerd;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить ледорубов"))
                            {
                                if (data.CraftIce < chec.A.Amount)
                                {
                                    NeedCount = data.CraftIce;
                                }
                                else
                                {
                                    NeedCount = data.CraftIce;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить самодельных топоров"))
                            {
                                if (data.CraftAxe < chec.A.Amount)
                                {
                                    NeedCount = data.CraftAxe;
                                }
                                else
                                {
                                    NeedCount = data.CraftAxe;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить пешек"))
                            {
                                if (data.CraftPesh < chec.A.Amount)
                                {
                                    NeedCount = data.CraftPesh;
                                }
                                else
                                {
                                    NeedCount = data.CraftPesh;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить хазматов"))
                            {
                                if (data.CraftHaz < chec.A.Amount)
                                {
                                    NeedCount = data.CraftHaz;
                                }
                                else
                                {
                                    NeedCount = data.CraftHaz;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить турель"))
                            {
                                if (data.CraftTurret < chec.A.Amount)
                                {
                                    NeedCount = data.CraftTurret;
                                }
                                else
                                {
                                    NeedCount = data.CraftTurret;
                                }
                            }

                            if (chec.A.ShortName.Contains("Скрафтить верстак 3 уровня"))
                            {
                                if (data.CraftWb3 < chec.A.Amount)
                                {
                                    NeedCount = data.CraftWb3;
                                }
                                else
                                {
                                    NeedCount = data.CraftWb3;
                                }
                            }

                            if (chec.A.ShortName.Contains("Залутать обычных ящиков"))
                            {
                                if (data.LootCrateNoraml2 < chec.A.Amount)
                                {
                                    NeedCount = data.LootCrateNoraml2;
                                }
                                else
                                {
                                    NeedCount = data.LootCrateNoraml2;
                                }
                            }

                            if (chec.A.ShortName.Contains("Залутать оружейных ящиков"))
                            {
                                if (data.LootCrateNoraml < chec.A.Amount)
                                {
                                    NeedCount = data.LootCrateNoraml;
                                }
                                else
                                {
                                    NeedCount = data.LootCrateNoraml;
                                }
                            }

                            if (chec.A.ShortName.Contains("Залутать элитных ящиков"))
                            {
                                if (data.LootCrateElite < chec.A.Amount)
                                {
                                    NeedCount = data.LootCrateElite;
                                }
                                else
                                {
                                    NeedCount = data.LootCrateElite;
                                }
                            }

                            if (chec.A.ShortName.Contains("Залутать чинуков"))
                            {
                                if (data.LootCrateLock < chec.A.Amount)
                                {
                                    NeedCount = data.LootCrateLock;
                                }
                                else
                                {
                                    NeedCount = data.LootCrateLock;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить стен в камень"))
                            {
                                if (data.UpgradeStoneWall < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeStoneWall;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeStoneWall;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить стен в металл"))
                            {
                                if (data.UpgradeMetalWall < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeMetalWall;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeMetalWall;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить стен в мвк"))
                            {
                                if (data.UpgradeMvkWall < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeMvkWall;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeMvkWall;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить фундаментов в камень"))
                            {
                                if (data.UpgradeStoneFound < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeStoneFound;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeStoneFound;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить фундаментов в металл"))
                            {
                                if (data.UpgradeMetalFound < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeMetalFound;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeMetalFound;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить фундаментов в мвк"))
                            {
                                if (data.UpgradeMvkFound < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeMvkFound;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeMvkFound;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить потолков в камень"))
                            {
                                if (data.UpgradeStoneFloor < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeStoneFloor;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeStoneFloor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить потолков в металл"))
                            {
                                if (data.UpgradeMetalFound < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeMetalFloor;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeMetalFloor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить потолков в мвк"))
                            {
                                if (data.UpgradeMvkFloor < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeMvkFloor;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeMvkFloor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить дверных проемом в камень"))
                            {
                                if (data.UpgradeStoneDoor < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeStoneDoor;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeStoneDoor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить дверных проемом в металл"))
                            {
                                if (data.UpgradeMetalDoor < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeMetalDoor;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeMetalDoor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Улучшить дверных проемом в мвк"))
                            {
                                if (data.UpgradeMvkDoor < chec.A.Amount)
                                {
                                    NeedCount = data.UpgradeMvkDoor;
                                }
                                else
                                {
                                    NeedCount = data.UpgradeMvkDoor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Сломать фундаментов"))
                            {
                                if (data.DesFound < chec.A.Amount)
                                {
                                    NeedCount = data.DesFound;
                                }
                                else
                                {
                                    NeedCount = data.DesFound;
                                }
                            }

                            if (chec.A.ShortName.Contains("Сломать потолков"))
                            {
                                if (data.UpgradeMvkDoor < chec.A.Amount)
                                {
                                    NeedCount = data.DesFloor;
                                }
                                else
                                {
                                    NeedCount = data.DesFloor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Сломать стен"))
                            {
                                if (data.UpgradeMvkDoor < chec.A.Amount)
                                {
                                    NeedCount = data.DesWall;
                                }
                                else
                                {
                                    NeedCount = data.DesWall;
                                }
                            }

                            if (chec.A.ShortName.Contains("Сломать металлических дверей"))
                            {
                                if (data.DesMetalDoor < chec.A.Amount)
                                {
                                    NeedCount = data.DesMetalDoor;
                                }
                                else
                                {
                                    NeedCount = data.DesMetalDoor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Сломать мвк дверей"))
                            {
                                if (data.DesMvkDoor < chec.A.Amount)
                                {
                                    NeedCount = data.DesMvkDoor;
                                }
                                else
                                {
                                    NeedCount = data.DesMvkDoor;
                                }
                            }

                            if (chec.A.ShortName.Contains("Сломать турелей"))
                            {
                                if (data.DesTurret < chec.A.Amount)
                                {
                                    NeedCount = data.DesTurret;
                                }
                                else
                                {
                                    NeedCount = data.DesTurret;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить с калаша"))
                            {
                                if (data.KillAk < chec.A.Amount)
                                {
                                    NeedCount = data.KillAk;
                                }
                                else
                                {
                                    NeedCount = data.KillAk;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить с лука"))
                            {
                                if (data.KillLuk < chec.A.Amount)
                                {
                                    NeedCount = data.KillLuk;
                                }
                                else
                                {
                                    NeedCount = data.KillLuk;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить с пешки"))
                            {
                                if (data.KollPesh < chec.A.Amount)
                                {
                                    NeedCount = data.KollPesh;
                                }
                                else
                                {
                                    NeedCount = data.KollPesh;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить с берданки"))
                            {
                                if (data.KillBer < chec.A.Amount)
                                {
                                    NeedCount = data.KillBer;
                                }
                                else
                                {
                                    NeedCount = data.KillBer;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить с томсона"))
                            {
                                if (data.Kolltomson < chec.A.Amount)
                                {
                                    NeedCount = data.Kolltomson;
                                }
                                else
                                {
                                    NeedCount = data.Kolltomson;
                                }
                            }

                            if (chec.A.ShortName.Contains("Убить с ракетницы"))
                            {
                                if (data.KillRocket < chec.A.Amount)
                                {
                                    NeedCount = data.KillRocket;
                                }
                                else
                                {
                                    NeedCount = data.KillRocket;
                                }
                            }

                            if (NeedCount < chec.A.Amount)
                            {
                                cont.Add(new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin =
                                                $"{0.1 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.2169312 - Math.Floor((double) chec.B / 1) * 0.15}",
                                            AnchorMax =
                                                $"{0.9 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.9936525 - Math.Floor((double) chec.B / 1) * 0.15}",
                                            OffsetMax = "0 0"
                                        },
                                        Button =
                                        {
                                            Color = "0 0 0 0.0",
                                            Material = "",
                                            Command = $""
                                        },
                                        Text =
                                        {
                                            Color = "0.58 0.35 1.00 0.98",
                                            Text = $"{NeedCount}/{chec.A.Amount}",
                                            Align = TextAnchor.MiddleRight,
                                            Font = "robotocondensed-bold.ttf", FontSize = 10
                                        },
                                    }, Layer + $".{check2.B}2", "Ktrk");
                                cont.Add(new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin =
                                                $"{0.1 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.2169312 - Math.Floor((double) chec.B / 1) * 0.15}",
                                            AnchorMax =
                                                $"{0.9 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.9936525 - Math.Floor((double) chec.B / 1) * 0.15}",
                                            OffsetMax = "0 0"
                                        },
                                        Button =
                                        {
                                            Color = "0 0 0 0.0",
                                            Material = "",
                                            Command = $""
                                        },
                                        Text =
                                        {
                                            Text = $"{chec.A.ShortName}", Align = TextAnchor.MiddleLeft,
                                            Font = "robotocondensed-bold.ttf", FontSize = 10
                                        },
                                    }, Layer + $".{check2.B}2", "Ktrk");
                                secc41 = false;
                            }
                            else
                            {
                                cont.Add(new CuiButton
                                    {
                                        RectTransform =
                                        {
                                            AnchorMin =
                                                $"{0.1 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.2169312 - Math.Floor((double) chec.B / 1) * 0.15}",
                                            AnchorMax =
                                                $"{0.9 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.9936525 - Math.Floor((double) chec.B / 1) * 0.15}",
                                            OffsetMax = "0 0"
                                        },
                                        Button =
                                        {
                                            Color = "0 0 0 0.0",
                                            Material = "",
                                            Command = $""
                                        },
                                        Text =
                                        {
                                            Color = "0.58 0.35 1.00 0.98",
                                            Text = $"ВЫПОЛНЕНО!",
                                            Align = TextAnchor.MiddleCenter,
                                            Font = "robotocondensed-bold.ttf", FontSize = 15
                                        },
                                    }, Layer + $".{check2.B}2", "Ktrk");
                                if (!secc41)
                                {
                                    secc41 = false;
                                }
                                else
                                {
                                    secc41 = true;
                                }
                            }
                        }
                        //
                        else
                        {
                            cont.Add(new CuiButton
                                {
                                    RectTransform =
                                    {
                                        AnchorMin =
                                            $"{0.1 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.2169312 - Math.Floor((double) chec.B / 1) * 0.15}",
                                        AnchorMax =
                                            $"{0.9 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.9936525 - Math.Floor((double) chec.B / 1) * 0.15}",
                                        OffsetMax = "0 0"
                                    },
                                    Button =
                                    {
                                        Color = "0 0 0 0.0",
                                        Material = "",
                                        Command = $""
                                    },
                                    Text =
                                    {
                                        Text = $"x{chec.A.Amount}",
                                        Align = TextAnchor.MiddleRight,
                                        Font = "robotocondensed-bold.ttf", FontSize = 10
                                    },
                                }, Layer + $".{check2.B}2", "Ktrk");
                            cont.Add(new CuiButton
                                {
                                    RectTransform =
                                    {
                                        AnchorMin =
                                            $"{0.1 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.2169312 - Math.Floor((double) chec.B / 1) * 0.15}",
                                        AnchorMax =
                                            $"{0.9 + chec.B * 0.119 - Math.Floor((double) chec.B / 1) * 1 * 0.119} {0.9936525 - Math.Floor((double) chec.B / 1) * 0.15}",
                                        OffsetMax = "0 0"
                                    },
                                    Button =
                                    {
                                        Color = "0 0 0 0.0",
                                        Material = "",
                                        Command = $""
                                    },
                                    Text =
                                    {
                                        Text = $"{chec.A.ShortName}", Align = TextAnchor.MiddleLeft,
                                        Font = "robotocondensed-bold.ttf", FontSize = 10
                                    },
                                }, Layer + $".{check2.B}2", "Ktrk");
                        }
                    }
                }

                foreach (var check in check2.A.LevelItem.Select((i, t) => new {A = i, B = t}))
                {
                    if (check.A.command == "")
                    {
                        cont.Add(new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin =
                                        $"{0.2755581 + check.B * 0.119 - Math.Floor((double) check.B / 4) * 4 * 0.119} {0.2169312 - Math.Floor((double) check.B / 4) * 0.20}",
                                    AnchorMax =
                                        $"{0.347826 + check.B * 0.119 - Math.Floor((double) check.B / 4) * 4 * 0.119} {0.7936525 - Math.Floor((double) check.B / 4) * 0.20}",
                                    OffsetMax = "0 0"
                                },
                                Button =
                                {
                                    Color = "0 0 0 0",
                                    Material = "",
                                    Command = $""
                                },
                                Text =
                                {
                                    Text = "", Align = TextAnchor.LowerRight,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 15
                                },
                                FadeOut = 0.05f
                            }, Layer + $".{check2.B}3", Layer + $".{check.B}");
                        cont.Add(new CuiElement
                        {
                            Parent = Layer + $".{check.B}",
                            Name = Layer + $".{check.B}",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) ImageLibrary.Call("GetImage", check.A.ShortName)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1",
                                    OffsetMax = "-5 -1",
                                }
                            }
                        });
                        cont.Add(new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin =
                                        $"{0.2755581 + check.B * 0.119 - Math.Floor((double) check.B / 4) * 4 * 0.119} {0.2169312 - Math.Floor((double) check.B / 4) * 0.20}",
                                    AnchorMax =
                                        $"{0.347826 + check.B * 0.119 - Math.Floor((double) check.B / 4) * 4 * 0.119} {0.5936525 - Math.Floor((double) check.B / 4) * 0.20}",
                                    OffsetMax = "0 0"
                                },
                                Button =
                                {
                                    Color = "0 0 0 0.0",
                                    Material = "",
                                    Command = $""
                                },
                                Text =
                                {
                                    Text = "x" + check.A.Amount + " ", Align = TextAnchor.LowerRight,
                                    Font = "robotocondensed-regular.ttf", FontSize = 15
                                },
                            }, Layer + $".{check2.B}3", Layer + $".{check.B}");
                    }
                    else
                    {
                        cont.Add(new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin =
                                        $"{0.2755581 + check.B * 0.119 - Math.Floor((double) check.B / 4) * 4 * 0.119} {0.2169312 - Math.Floor((double) check.B / 4) * 0.20}",
                                    AnchorMax =
                                        $"{0.347826 + check.B * 0.119 - Math.Floor((double) check.B / 4) * 4 * 0.119} {0.7936525 - Math.Floor((double) check.B / 4) * 0.20}",
                                    OffsetMax = "0 0"
                                },
                                Button =
                                {
                                    Color = "0 0 0 0",
                                    Material = "",
                                    Command = $""
                                },
                                Text =
                                {
                                    Text = "", Align = TextAnchor.LowerRight,
                                    Font = "robotocondensed-regular.ttf",
                                    FontSize = 15
                                },
                            }, Layer + $".{check2.B}3", Layer + $".{check.B}");
                        cont.Add(new CuiElement
                        {
                            Parent = Layer + $".{check.B}",
                            Name = Layer + $".{check.B}",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = GetImage(check.A.ShortName)
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 1",
                                    OffsetMax = "-5 -1"
                                }
                            }
                        });
                        cont.Add(new CuiButton
                            {
                                RectTransform =
                                {
                                    AnchorMin =
                                        $"{0.2755581 + check.B * 0.119 - Math.Floor((double) check.B / 4) * 4 * 0.119} {0.2169312 - Math.Floor((double) check.B / 4) * 0.20}",
                                    AnchorMax =
                                        $"{0.347826 + check.B * 0.119 - Math.Floor((double) check.B / 4) * 4 * 0.119} {0.5936525 - Math.Floor((double) check.B / 4) * 0.20}",
                                    OffsetMax = "0 0"
                                },
                                Button =
                                {
                                    Color = "0 0 0 0.0",
                                    Material = "",
                                    Command = $""
                                },
                                Text =
                                {
                                    Text = "x" + check.A.Amount + " ", Align = TextAnchor.LowerRight,
                                    Font = "robotocondensed-regular.ttf", FontSize = 15
                                },
                            }, Layer + $".{check2.B}3", Layer + $".{check.B}");
                    }
                }

                if (secc41 && data.Lvl == check2.A.NumLvl)
                {
                    cont.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin =
                                    $"{0.7602819 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.3439152 - Math.Floor((double) check2.B) * 0.01}",
                                AnchorMax =
                                    $"{0.9447708 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.6349199 - Math.Floor((double) check2.B) * 0.01}",
                                OffsetMax = "0 0"
                            },
                            Button =
                            {
                                Color = "0.00 0.50 0.00 0.53",
                                Material = "",
                                Command = "UI_bpass2_LevelsUI"
                            },
                            Text =
                            {
                                Text = "Получить награду", Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 15
                            }
                        }, Layer + $".{check2.B}3");
                }
                else if (data.Lvl > check2.A.NumLvl)
                {
                    cont.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin =
                                    $"{0.7602819 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.3439152 - Math.Floor((double) check2.B) * 0.01}",
                                AnchorMax =
                                    $"{0.9447708 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.6349199 - Math.Floor((double) check2.B) * 0.01}",
                                OffsetMax = "0 0"
                            },
                            Button =
                            {
                                Color = "0.92 0.00 0.00 0.53",
                                Material = "",
                                Command = ""
                            },
                            Text =
                            {
                                Text = "√", Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 15
                            }
                        }, Layer + $".{check2.B}3");
                }
                else
                {
                    cont.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin =
                                    $"{0.7602819 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.3439152 - Math.Floor((double) check2.B) * 0.01}",
                                AnchorMax =
                                    $"{0.9447708 + check2.B * 0.119 - Math.Floor((double) check2.B / 1) * 0.119} {0.6349199 - Math.Floor((double) check2.B) * 0.01}",
                                OffsetMax = "0 0"
                            },
                            Button =
                            {
                                Color = "0.00 0.00 0.00 0.76",
                                Material = "",
                                Command = ""
                            },
                            Text =
                            {
                                Text = "Получить награду", Align = TextAnchor.MiddleCenter,
                                Font = "robotocondensed-regular.ttf",
                                FontSize = 15
                            }
                        }, Layer + $".{check2.B}3");
                }
            }
            CuiHelper.AddUi(player, cont);
        }

        [ChatCommand("bp")]
        void Battlepassopen(BasePlayer player)
        {
            MainUi(player);
        }

        [ConsoleCommand("UI_bpass2_LevelsUI")]
        void Battlepassopens(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            CuiHelper.DestroyUi(player, Layer);
            if (player == null) return;
            BattleData data2 = BattlePasss.Find(x => x.SteamID == player.userID);
            if (data2.Class == null) return;
            BattleData2 data1 = BattlePassLevels.Find(x => data2.Class == x.Class);
            if (data1 == null) return;
            var t = data1.Level.Find(x => data2.Lvl == x.NumLvl);
            foreach (var set in t.LevelItem.Select((i, f) => new {A = i, B = f}))
            {
                if (set.A.command == "")
                {
                    Item n = ItemManager.CreateByName(set.A.ShortName, set.A.Amount);
                    if (!player.inventory.GiveItem(n))
                    {
                        n.Drop(player.ServerPosition, Vector3.down * 3f);
                    }
                }
            }

            foreach (var command in t.LevelItem.Select((i, f) => new {A = i, B = f}))
            {
                if (command.A.command != "")
                {
                    rust.RunServerCommand(String.Format(command.A.command, args.Player().userID));
                }
            }

            ClearStats(player);
            data2.Lvl += 1;
            if (cfg.newlvl && data2.Lvl > data1.Level.Count)
            {
                SendReply(player, cfg.newlvltext);
            }
        }

        void ClearStats(BasePlayer player)
        {
            if (player == null) return;
            BattleData data2 = BattlePasss.Find(x => x.SteamID == player.userID);
            if (data2.Class == null) return;
            BattleData2 data1 = BattlePassLevels.Find(x => data2.Class == x.Class);
            if (data1 == null) return;
            data2.Crafttomson = 0;
            data2.Killnpc = 0;
            data2.Kills = 0;
            data2.KillKob = 0;
            data2.KillBear = 0;
            data2.KillChik = 0;
            data2.KillHorse = 0;
            data2.KillStag = 0;
            data2.KillVolk = 0;
            data2.KillBarrel = 0;
            data2.KillHeli = 0;
            data2.KillTank = 0;
            data2.KillLuk = 0;
            data2.KillBer = 0;
            data2.KillRocket = 0;
            data2.KillStag = 0;
            data2.KillAk = 0;
            data2.Kolltomson = 0;
            data2.KollPesh = 0;
            data2.GatherS = 0;
            data2.GatherM = 0;
            data2.GatherW = 0;
            data2.GatherWSulfur = 0;
            data2.GatherMVK = 0;
            data2.GatherFat = 0;
            data2.GatherCloth = 0;
            data2.GatherHemp = 0;
            data2.GatherKoja = 0;
            data2.GatherMVK = 0;
            data2.CraftAk = 0;
            data2.CraftTurret = 0;
            data2.CraftWb3 = 0;
            data2.CraftWeter = 0;
            data2.CraftMetalDoor = 0;
            data2.CraftMVKDoor = 0;
            data2.Crafttomson = 0;
            data2.CraftHaz = 0;
            data2.CraftRocket = 0;
            data2.CraftRocketL = 0;
            data2.Craft5 = 0;
            data2.CraftAmmo = 0;
            data2.CraftBerd = 0;
            data2.CraftAxe = 0;
            data2.CraftCof = 0;
            data2.CraftFuel = 0;
            data2.CraftGarage = 0;
            data2.CraftIce = 0;
            data2.CraftLuk = 0;
            data2.CraftPammo = 0;
            data2.CraftPesh = 0;
            data2.CraftRep = 0;
            data2.CraftResea = 0;
            data2.CraftRev = 0;
            data2.CraftRoad = 0;
            data2.CraftAmHand = 0;
            data2.CraftStoneGates = 0;
            data2.CraftStoneW = 0;
            data2.LootCrateElite = 0;
            data2.LootCrateNoraml2 = 0;
            data2.LootCrateNoraml = 0;
            data2.LootCrateLock = 0;
            data2.UpgradeMetalFound = 0;
            data2.UpgradeMetalDoor = 0;
            data2.UpgradeMetalWall = 0;
            data2.UpgradeMetalFloor = 0;
            data2.UpgradeStoneWall = 0;
            data2.UpgradeStoneDoor = 0;
            data2.UpgradeStoneFloor = 0;
            data2.UpgradeStoneFound = 0;
            data2.UpgradeMvkFloor = 0;
            data2.UpgradeMvkFound = 0;
            data2.UpgradeMvkWall = 0;
            data2.UpgradeMvkDoor = 0;
            data2.DesStoneW = 0;
            data2.DesFloor = 0;
            data2.DesFound = 0;
            data2.DesTurret = 0;
            data2.DesWall = 0;
            data2.DesMetalDoor = 0;
            data2.DesMvkDoor = 0;
            
        }
        void OnServerSave()
        {
            SaveData();
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
            SaveData();
        }

        //Для выполнение уровней
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity?.ToPlayer();
            BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
            if (data == null) return;
            if (item.info.shortname == "wood")
            {
                timer.Once(0.01f, () => { data.GatherW += item.amount; });
            }

            if (item.info.shortname == "stones")
            {
                timer.Once(0.01f, () => { data.GatherS += item.amount; });
            }

            if (item.info.shortname == "sulfur.ore")
            {
                timer.Once(0.01f, () => { data.GatherWSulfur +=  item.amount; });
            }

            if (item.info.shortname == "metal.ore")
            {
                timer.Once(0.01f, () => { data.GatherM += item.amount; });
            }

            if (item.info.shortname == "hq.metal.ore")
            {
                timer.Once(0.01f, () => { data.GatherMVK += item.amount; });
            }

            if (item.info.shortname == "cloth")
            {
                timer.Once(0.01f, () => { data.GatherCloth += item.amount; });
            }

            if (item.info.shortname == "leather")
            {
                timer.Once(0.01f, () => { data.GatherKoja += item.amount; });
            }

            if (item.info.shortname == "fat.animal")
            {
                timer.Once(0.01f, () => { data.GatherFat += item.amount; });
            }
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) =>
            OnDispenserGather(dispenser, entity, item);

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
            if (data == null) return;
            if (item.info.shortname == "wood")
            {
                timer.Once(0.01f, () => { data.GatherW += item.amount; });
            }

            if (item.info.shortname == "stones")
            {
                timer.Once(0.01f, () => { data.GatherS += item.amount; });
            }

            if (item.info.shortname == "sulfur.ore")
            {
                timer.Once(0.01f, () => { data.GatherWSulfur += item.amount; });
            }

            if (item.info.shortname == "metal.ore")
            {
                timer.Once(0.01f, () => { data.GatherM += item.amount; });
            }

            if (item.info.shortname == "cloth")
            {
                timer.Once(0.01f, () => { data.GatherHemp += item.amount; });
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (hitinfo?.Initiator is BasePlayer)
            {
                var player = hitinfo.Initiator as BasePlayer;
                if (!player.userID.IsSteamId()) return;
                BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
                if (data == null) return;
                if (player.userID.IsSteamId() && !(player is NPCPlayer))
                {
                    if (entity.name.Contains("agents/"))
                        switch (entity.ShortPrefabName)
                        {
                            case "bear":
                                data.KillBear++;
                                break;
                            case "boar":
                                data.KillKob++;
                                break;
                            case "chicken":
                                data.KillChik++;
                                break;
                            case "horse":
                                data.KillHorse++;
                                break;
                            case "stag":
                                data.KillStag++;
                                break;
                            case "wolf":
                                data.KillVolk++;
                                break;
                            case "scientistnpc":
                                data.Killnpc++;
                                break;
                            case "heavyscientist":
                                data.Killnpc++;
                                break;
                        }

                    if (entity.name.Contains("htn/"))
                    {
                        data.Killnpc++;
                        return;
                    }
                    if (entity.ShortPrefabName == "loot-barrel-1" || entity.ShortPrefabName == "loot-barrel-2" ||
                        entity.ShortPrefabName == "loot_barrel_1" || entity.ShortPrefabName == "loot_barrel_2")
                    {
                        data.KillBarrel++;
                    }

                    if (entity.PrefabName == "assets/rust.ai/nextai/testridablehorse.prefab")
                    {
                        data.KillHorse++;
                    }

                    if (entity.ShortPrefabName == "foundation" && entity.OwnerID != player.userID ||
                        entity.ShortPrefabName == "foundation.triangle" && entity.OwnerID != player.userID)
                    {
                        data.DesFound++;
                    }

                    if (entity.ShortPrefabName == "wall" && entity.OwnerID != player.userID ||
                        entity.ShortPrefabName == "wall.half" && entity.OwnerID != player.userID ||
                        entity.ShortPrefabName == "wall.low" && entity.OwnerID != player.userID)
                    {
                        data.DesWall++;
                    }

                    if (entity.ShortPrefabName == "floor" && entity.OwnerID != player.userID ||
                        entity.ShortPrefabName == "floor.triangle" && entity.OwnerID != player.userID)
                    {
                        data.DesFloor++;
                    }

                    if (entity.ShortPrefabName == "door.hinged.metal" && entity.OwnerID != player.userID ||
                        entity.ShortPrefabName == "door.double.hinged.metal" && entity.OwnerID != player.userID)
                    {
                        data.DesMetalDoor++;
                    }

                    if (entity.ShortPrefabName == "door.hinged.toptier" && entity.OwnerID != player.userID ||
                        entity.ShortPrefabName == "door.double.hinged.toptier" && entity.OwnerID != player.userID)
                    {
                        data.DesMvkDoor++;
                    }

                    if (entity.ShortPrefabName == "autoturret_deployed" && entity.OwnerID != player.userID)
                    {
                        data.DesTurret++;
                    }

                    if (entity.ShortPrefabName == "wall.external.high.stone" && entity.OwnerID != player.userID)
                    {
                        data.DesStoneW++;
                    }
                }
            }

            if (entity is BaseHelicopter)
            {
                BattleData data1 = BattlePasss.Find(x => x.SteamID == LastDamagePlayer);
                if (data1 == null) return;
                data1.KillHeli++;
            }

            if (entity is BradleyAPC)
            {
                BasePlayer player2;
                player2 = BasePlayer.FindByID(LastDamagePlayer);
                BattleData data1 = BattlePasss.Find(x => x.SteamID == player2.userID);
                if (data1 == null) return;
                data1.KillTank++;
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                LastDamagePlayer = info.Initiator.ToPlayer().userID;
            if (entity is BradleyAPC && info.Initiator is BasePlayer)
                LastDamagePlayer = info.Initiator.ToPlayer().userID;
        }

        private void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (info == null || player == null || player.IsNpc)
                return;
            var attacker = info.InitiatorPlayer;
            if (attacker == null || attacker.IsNpc)
                return;
            BattleData dataattacker = BattlePasss.Find(x => x.SteamID == attacker.userID);
            if (dataattacker == null) return;
            if (attacker.userID == player.userID) return;
            if (!player.userID.IsSteamId()) return;
            if(!attacker.userID.IsSteamId()) return;
            if (info.WeaponPrefab == null || info.Weapon == null)
            {
                dataattacker.Kills++;
                return;
            }
            if (info.damageTypes.GetMajorityDamageType() == DamageType.Blunt ||
                info.damageTypes.GetMajorityDamageType() == DamageType.Stab ||
                info.damageTypes.GetMajorityDamageType() == DamageType.Heat)
            {
                dataattacker.Kills++;
                return;
            }
            if (info.damageTypes.GetMajorityDamageType() == DamageType.Explosion)
            {
                switch (info.WeaponPrefab.ShortPrefabName)
                {
                    case "rocket_basic":
                        dataattacker.Kills++;
                        dataattacker.KillRocket++;
                        return;
                    case "rocket_fire":
                        dataattacker.Kills++;
                        dataattacker.KillRocket++;
                        return;
                    case "rocket_hv":
                        dataattacker.Kills++;
                        dataattacker.KillRocket++;
                        return;
                }
               dataattacker.Kills++;
                return;
            }
            switch (info.WeaponPrefab.ShortPrefabName)
            {
                case "rocket_basic":
                    dataattacker.Kills++;
                    dataattacker.KillRocket++;
                    return;
                case "rocket_fire":
                    dataattacker.Kills++;
                    dataattacker.KillRocket++;
                    return;
                case "rocket_hv":
                    dataattacker.Kills++;
                    dataattacker.KillRocket++;
                    return;
                case "flamethrower.entity":
                    dataattacker.Kills++;
                    return;
                default:
                    switch (info.Weapon.ShortPrefabName)
                    {
                        case "ak47u.entity":
                            dataattacker.Kills++;
                            dataattacker.KillAk++;
                            return;
                        case "pistol_semiauto.entity":
                            dataattacker.Kills++;
                            dataattacker.KollPesh++;
                            return;
                        case "thompson.entity":
                            dataattacker.Kills++;
                            dataattacker.Kolltomson++;
                            return;
                        case "semi_auto_rifle.entity":
                            dataattacker.Kills++;
                            dataattacker.KillBer++;
                            return;
                        case "bow_hunting.entity":
                            dataattacker.Kills++;
                            dataattacker.KillLuk++;
                            return;
                        default:
                            dataattacker.Kills++;
                            break;
                    }
                    break;
            }
        }

        object OnItemCraft(ItemCraftTask task, BasePlayer player, Item item)
        {
            BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
            if (data == null) return null;
            if (task.blueprint.targetItem.shortname == "rifle.ak")
            {
                if (task.cancelled)
                {
                    data.CraftAk += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "smg.thompson")
            {
                if (task.cancelled)
                {
                    data.Crafttomson += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "autoturret")
            {
                if (task.cancelled)
                {
                    data.CraftTurret += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "workbench3")
            {
                if (task.cancelled)
                {
                    data.CraftWb3 += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "door.hinged.metal" ||
                task.blueprint.targetItem.shortname == "door.double.hinged.metal")
            {
                if (task.cancelled)
                {
                    data.CraftMetalDoor += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "door.hinged.toptier" ||
                task.blueprint.targetItem.shortname == "door.double.hinged.toptier")
            {
                if (task.cancelled)
                {
                    data.CraftMVKDoor += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "generator.wind.scrap")
            {
                if (task.cancelled)
                {
                    data.CraftWeter += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "lowgradefuel")
            {
                if (task.cancelled)
                {
                    data.CraftFuel += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "ammo.rocket.basic")
            {
                if (task.cancelled)
                {
                    data.CraftRocket += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "ammo.rifle.explosive")
            {
                if (task.cancelled)
                {
                    data.CraftAmmo += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "rocket.launcher")
            {
                if (task.cancelled)
                {
                    data.CraftRocketL += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "roadsign.jacket")
            {
                if (task.cancelled)
                {
                    data.CraftRoad += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "coffeecan.helmet")
            {
                if (task.cancelled)
                {
                    data.CraftCof += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "research.table")
            {
                if (task.cancelled)
                {
                    data.CraftResea += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "box.repair.bench")
            {
                if (task.cancelled)
                {
                    data.CraftRep += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "wall.frame.garagedoor")
            {
                if (task.cancelled)
                {
                    data.CraftGarage += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "gates.external.high.stone")
            {
                if (task.cancelled)
                {
                    data.CraftStoneGates += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "wall.external.high.stone")
            {
                if (task.cancelled)
                {
                    data.CraftStoneW += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "floor.ladder.hatch")
            {
                if (task.cancelled)
                {
                    data.CraftLuk += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "pistol.revolver")
            {
                if (task.cancelled)
                {
                    data.CraftRev += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "ammo.pistol")
            {
                if (task.cancelled)
                {
                    data.CraftPammo += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "ammo.rifle")
            {
                if (task.cancelled)
                {
                    data.Craft5 += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "rifle.semiauto")
            {
                if (task.cancelled)
                {
                    data.CraftBerd += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "icepick.salvaged")
            {
                if (task.cancelled)
                {
                    data.CraftIce += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "axe.salvaged")
            {
                if (task.cancelled)
                {
                    data.CraftAxe += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "hazmatsuit")
            {
                if (task.cancelled)
                {
                    data.CraftHaz += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "ammo.handmade.shell")
            {
                if (task.cancelled)
                {
                    data.CraftAmHand += task.blueprint.amountToCreate;
                }
            }

            if (task.blueprint.targetItem.shortname == "pistol.semiauto")
            {
                if (task.cancelled)
                {
                    data.CraftPesh += task.blueprint.amountToCreate;
                }
            }

            return null;
        }

        object OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
            if (data == null) return null;
            if (entity.ShortPrefabName == "foundation" || entity.ShortPrefabName == "foundation.triangle")
            {
                if (grade == BuildingGrade.Enum.Stone)
                {
                    data.UpgradeStoneFound++;
                }

                if (grade == BuildingGrade.Enum.Metal)
                {
                    data.UpgradeMetalFound++;
                }

                if (grade == BuildingGrade.Enum.TopTier)
                {
                    data.UpgradeMvkFound++;
                }
            }

            if (entity.ShortPrefabName == "wall" || entity.ShortPrefabName == "wall.half" ||
                entity.ShortPrefabName == "wall.low")
            {
                if (grade == BuildingGrade.Enum.Stone)
                {
                    data.UpgradeStoneWall++;
                }

                if (grade == BuildingGrade.Enum.Metal)
                {
                    data.UpgradeMetalWall++;
                }

                if (grade == BuildingGrade.Enum.TopTier)
                {
                    data.UpgradeMvkWall++;
                }
            }

            if (entity.ShortPrefabName == "floor" || entity.ShortPrefabName == "floor.triangle")
            {
                if (grade == BuildingGrade.Enum.Stone)
                {
                    data.UpgradeStoneFloor++;
                }

                if (grade == BuildingGrade.Enum.Metal)
                {
                    data.UpgradeMetalFloor++;
                }

                if (grade == BuildingGrade.Enum.TopTier)
                {
                    data.UpgradeMvkFloor++;
                }
            }

            if (entity.ShortPrefabName == "wall.door" || entity.ShortPrefabName == "wall.frame")
            {
                if (grade == BuildingGrade.Enum.Stone)
                {
                    data.UpgradeStoneDoor++;
                }

                if (grade == BuildingGrade.Enum.Metal)
                {
                    data.UpgradeMetalDoor++;
                }

                if (grade == BuildingGrade.Enum.TopTier)
                {
                    data.UpgradeMetalDoor++;
                }
            }

            return null;
        }

        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            BattleData data = BattlePasss.Find(x => x.SteamID == player.userID);
            if (data == null) return;

            if (entity.ShortPrefabName == "crate_normal_2" && entity.OwnerID == 0)
            {
                entity.OwnerID = player.userID;
                data.LootCrateNoraml2++;
            }

            if (entity.ShortPrefabName == "codelockedhackablecrate" && entity.OwnerID == 0)
            {
                entity.OwnerID = player.userID;
                data.LootCrateLock++;
            }

            if (entity.ShortPrefabName == "crate_normal" && entity.OwnerID == 0)
            {
                entity.OwnerID = player.userID;
                data.LootCrateNoraml++;
            }

            if (entity.ShortPrefabName == "crate_elite" && entity.OwnerID == 0)
            {
                entity.OwnerID = player.userID;
                data.LootCrateElite++;
            }

            if (entity.ShortPrefabName == "crate_elite" && entity.OwnerID == 0)
            {
                entity.OwnerID = player.userID;
                data.LootCrateElite++;
            }
        }
    }
}