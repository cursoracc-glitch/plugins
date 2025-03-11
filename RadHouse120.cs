using Facepunch;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("RadHouse120", "RustPlugin.ru", "1.2.0")]

    class RadHouse120 : RustPlugin
    {
        // Other needed functions and vars
        #region SomeParameters and plugin's load
        [PluginReference]
        Plugin RandomSpawns;
        [PluginReference]
        Plugin RustMap;
        [PluginReference]
        Plugin Map;
        [PluginReference]
        Plugin LustyMap;

        static RadHouse120 ins;

        private List<ZoneList> RadiationZones = new List<ZoneList>();
        private static readonly int playerLayer = LayerMask.GetMask("Player (Server)");
        private static readonly Collider[] colBuffer = Vis.colBuffer;
        private ZoneList RadHouseZone;
        private BaseEntity LootBox;


        public List<BaseEntity> BaseEntityList = new List<BaseEntity>();
        public List<ulong> PlayerAuth = new List<ulong>();

        private DateTime DateOfWipe;
        private string DateOfWipeStr;

        public bool CanLoot = false;
        public bool NowLooted = false;
        public Timer mytimer;
        public Timer mytimer2;
        public Timer mytimer3;
        public Timer mytimer4;
        public Timer mytimer5;
        public int timercallbackdelay = 0;

        #region CFG var's
        public class Amount
        {
            public object ShortName;
            public object Min;
            public object Max;
        }

        public class DataStorage
        {
            public Dictionary<string, Amount>[] Common = new Dictionary<string, Amount>[]
            {
            new Dictionary<string, Amount>()
            {
                ["Wood"] = new Amount() { ShortName = "wood", Min = 3000, Max = 10000 },
                ["Stone"] = new Amount() { ShortName = "stones", Min = 5000, Max = 9000 },
                ["Metall"] = new Amount() { ShortName = "metal.fragments", Min = 1000, Max = 5000 },
                ["Charcoal"] = new Amount() { ShortName = "charcoal", Min = 2500, Max = 10000 },
                ["Fuel"] = new Amount() { ShortName = "lowgradefuel", Min = 100, Max = 500 }
            },
            new Dictionary<string, Amount>()
            {
                ["Wood"] = new Amount() { ShortName = "wood", Min = 5000, Max = 15000 },
                ["Stone"] = new Amount() { ShortName = "stones", Min = 9000, Max = 20000 },
                ["Metall"] = new Amount() { ShortName = "metal.fragments", Min = 2500, Max = 10000 },
                ["Charcoal"] = new Amount() { ShortName = "charcoal", Min = 5000, Max = 15000 },
                ["Fuel"] = new Amount() { ShortName = "lowgradefuel", Min = 250, Max = 900 },
                ["HQMetall"] = new Amount() { ShortName = "metal.refined", Min = 50, Max = 150 }
            },
            new Dictionary<string, Amount>()
            {
                ["Wood"] = new Amount() { ShortName = "wood", Min = 8000, Max = 25000 },
                ["Stone"] = new Amount() { ShortName = "stones", Min = 15000, Max = 32000 },
                ["Metall"] = new Amount() { ShortName = "metal.fragments", Min = 5000, Max = 15000 },
                ["Charcoal"] = new Amount() { ShortName = "charcoal", Min = 7000, Max = 23000 },
                ["Fuel"] = new Amount() { ShortName = "lowgradefuel", Min = 350, Max = 1200 },
                ["HQMetall"] = new Amount() { ShortName = "metal.refined", Min = 150, Max = 400 },
                ["Sulfur"] = new Amount() { ShortName = "sulfur", Min = 1000, Max = 3500 }
            },
            new Dictionary<string, Amount>()
            {
                ["Wood"] = new Amount() { ShortName = "wood", Min = 15000, Max = 35000 },
                ["Stone"] = new Amount() { ShortName = "stones", Min = 40000, Max = 70000 },
                ["Metall"] = new Amount() { ShortName = "metal.fragments", Min = 10000, Max = 25000 },
                ["Charcoal"] = new Amount() { ShortName = "charcoal", Min = 20000, Max = 40000 },
                ["Fuel"] = new Amount() { ShortName = "lowgradefuel", Min = 500, Max = 1500 },
                ["HQMetall"] = new Amount() { ShortName = "metal.refined", Min = 250, Max = 600 },
                ["Sulfur"] = new Amount() { ShortName = "sulfur", Min = 2500, Max = 6000 },
                ["GunPow"] = new Amount() { ShortName = "gunpowder", Min = 1000, Max = 3000 }
            },
            new Dictionary<string, Amount>()
            {
                ["Wood"] = new Amount() { ShortName = "wood", Min = 25000, Max = 50000 },
                ["Stone"] = new Amount() { ShortName = "stones", Min = 50000, Max = 80000 },
                ["Metall"] = new Amount() { ShortName = "metal.fragments", Min = 15000, Max = 35000 },
                ["Charcoal"] = new Amount() { ShortName = "charcoal", Min = 30000, Max = 50000 },
                ["Fuel"] = new Amount() { ShortName = "lowgradefuel", Min = 750, Max = 1750 },
                ["HQMetall"] = new Amount() { ShortName = "metal.refined", Min = 350, Max = 700 },
                ["Sulfur"] = new Amount() { ShortName = "sulfur", Min = 3500, Max = 7000 },
                ["GunPow"] = new Amount() { ShortName = "gunpowder", Min = 2000, Max = 5000 },
                ["Explosives"] = new Amount() { ShortName = "explosives", Min = 70, Max = 150 }
            },
            new Dictionary<string, Amount>()
            {
                ["Wood"] = new Amount() { ShortName = "wood", Min = 35000, Max = 70000 },
                ["Stone"] = new Amount() { ShortName = "stones", Min = 70000, Max = 100000 },
                ["Metall"] = new Amount() { ShortName = "metal.fragments", Min = 20000, Max = 40000 },
                ["Charcoal"] = new Amount() { ShortName = "charcoal", Min = 40000, Max = 60000 },
                ["Fuel"] = new Amount() { ShortName = "lowgradefuel", Min = 1000, Max = 2000 },
                ["HQMetall"] = new Amount() { ShortName = "metal.refined", Min = 500, Max = 900 },
                ["Sulfur"] = new Amount() { ShortName = "sulfur", Min = 5000, Max = 10000 },
                ["GunPow"] = new Amount() { ShortName = "gunpowder", Min = 4000, Max = 7000 },
                ["Explosives"] = new Amount() { ShortName = "explosives", Min = 100, Max = 250 }
            },
            new Dictionary<string, Amount>()
            {
                ["Wood"] = new Amount() { ShortName = "wood", Min = 50000, Max = 90000 },
                ["Stone"] = new Amount() { ShortName = "stones", Min = 90000, Max = 130000 },
                ["Metall"] = new Amount() { ShortName = "metal.fragments", Min = 25000, Max = 47000 },
                ["Charcoal"] = new Amount() { ShortName = "charcoal", Min = 50000, Max = 70000 },
                ["Fuel"] = new Amount() { ShortName = "lowgradefuel", Min = 1300, Max = 2500 },
                ["HQMetall"] = new Amount() { ShortName = "metal.refined", Min = 700, Max = 1200 },
                ["Sulfur"] = new Amount() { ShortName = "sulfur", Min = 10000, Max = 20000 },
                ["GunPow"] = new Amount() { ShortName = "gunpowder", Min = 7000, Max = 15000 },
                ["Explosives"] = new Amount() { ShortName = "explosives", Min = 250, Max = 400 }
            }
            };
            public Dictionary<string, Amount>[] Rare = new Dictionary<string, Amount>[]
            {
            new Dictionary<string, Amount>()
            {
                ["WoodGates"] = new Amount() { ShortName = "gates.external.high.wood", Min = 1, Max = 1 },
                ["WoodWall"] = new Amount() { ShortName = "wall.external.high", Min = 2, Max = 3 },
                ["MetallBarricade"] = new Amount() { ShortName = "barricade.metal", Min = 2, Max = 3 }
            },
            new Dictionary<string, Amount>()
            {
                ["StoneWall"] = new Amount() { ShortName = "wall.external.high.stone", Min = 2, Max = 3 },
                ["StoneGate"] = new Amount() { ShortName = "gates.external.high.stone", Min = 1, Max = 1 },
                ["P250"] = new Amount() { ShortName = "pistol.semiauto", Min = 1, Max = 1 },
                ["Python"] = new Amount() { ShortName = "pistol.python", Min = 1, Max = 1 }
            },
            new Dictionary<string, Amount>()
            {
                ["GunPow"] = new Amount() { ShortName = "gunpowder", Min = 500, Max = 2000 },
                ["Explosives"] = new Amount() { ShortName = "explosives", Min = 10, Max = 40 },
                ["Smg"] = new Amount() { ShortName = "smg.2", Min = 1, Max = 1 },
                ["SmgMp5"] = new Amount() { ShortName = "smg.mp5", Min = 1, Max = 1 }
            },
            new Dictionary<string, Amount>()
            {
                ["Explosives"] = new Amount() { ShortName = "explosives", Min = 40, Max = 100 },
                ["Thompson"] = new Amount() { ShortName = "smg.thompson", Min = 1, Max = 1 },
                ["Bolt"] = new Amount() { ShortName = "rifle.bolt", Min = 1, Max = 1 },
                ["B4"] = new Amount() { ShortName = "explosive.satchel", Min = 4, Max = 11 }
            },
            new Dictionary<string, Amount>()
            {
                ["AmmoRifle"] = new Amount() { ShortName = "ammo.rifle", Min = 90, Max = 150 },
                ["Bolt"] = new Amount() { ShortName = "rifle.bolt", Min = 1, Max = 1 },
                ["LR300"] = new Amount() { ShortName = "rifle.lr300", Min = 1, Max = 1 },
                ["Ak"] = new Amount() { ShortName = "rifle.ak", Min = 1, Max = 1 },
                ["Mask"] = new Amount() { ShortName = "metal.facemask", Min = 1, Max = 1 },
                ["B4"] = new Amount() { ShortName = "explosive.satchel", Min = 8, Max = 17 }
            },
            new Dictionary<string, Amount>()
            {
                ["AmmoRifle"] = new Amount() { ShortName = "ammo.rifle", Min = 60, Max = 120 },
                ["Bolt"] = new Amount() { ShortName = "rifle.bolt", Min = 1, Max = 1 },
                ["LR300"] = new Amount() { ShortName = "rifle.lr300", Min = 1, Max = 1 },
                ["Ak"] = new Amount() { ShortName = "rifle.ak", Min = 1, Max = 1 },
                ["C4"] = new Amount() { ShortName = "explosive.timed", Min = 2, Max = 4 },
                ["B4"] = new Amount() { ShortName = "explosive.satchel", Min = 6, Max = 13 }
            },
            new Dictionary<string, Amount>()
            {
                ["AmmoRifle"] = new Amount() { ShortName = "ammo.rifle", Min = 150, Max = 240 },
                ["Bolt"] = new Amount() { ShortName = "rifle.bolt", Min = 1, Max = 1 },
                ["LR300"] = new Amount() { ShortName = "rifle.lr300", Min = 1, Max = 1 },
                ["Ak"] = new Amount() { ShortName = "rifle.ak", Min = 1, Max = 1 },
                ["Launcher"] = new Amount() { ShortName = "rocket.launcher", Min = 1, Max = 1 },
                ["M249"] = new Amount() { ShortName = "lmg.m249", Min = 1, Max = 1 }
            }
            };
            public Dictionary<string, Amount>[] Top = new Dictionary<string, Amount>[]
            {
            new Dictionary<string, Amount>()
            {
                ["DoorHQ"] = new Amount() { ShortName = "door.hinged.toptier", Min = 1, Max = 1 },
                ["DdoorHQ"] = new Amount() { ShortName = "door.double.hinged.toptier", Min = 1, Max = 2 },
                ["p250"] = new Amount() { ShortName = "pistol.semiauto", Min = 1, Max = 1 }
            },
            new Dictionary<string, Amount>()
            {
                ["Pomp"] = new Amount() { ShortName = "shotgun.pump", Min = 1, Max = 1 },
                ["B4"] = new Amount() { ShortName = "explosive.satchel", Min = 1, Max = 4 },
                ["m92"] = new Amount() { ShortName = "pistol.m92", Min = 1, Max = 1 }
            },
            new Dictionary<string, Amount>()
            {
                ["Thompson"] = new Amount() { ShortName = "smg.thompson", Min = 1, Max = 1 },
                ["Ak"] = new Amount() { ShortName = "rifle.ak", Min = 1, Max = 1 },
                ["B4"] = new Amount() { ShortName = "explosive.satchel", Min = 3, Max = 9 }
            },
            new Dictionary<string, Amount>()
            {
                ["C4"] = new Amount() { ShortName = "explosive.timed", Min = 1, Max = 3 },
                ["LR300"] = new Amount() { ShortName = "rifle.lr300", Min = 1, Max = 1 },
                ["Plate"] = new Amount() { ShortName = "metal.plate.torso", Min = 1, Max = 1 }
            },
            new Dictionary<string, Amount>()
            {
                ["C4"] = new Amount() { ShortName = "explosive.timed", Min = 3, Max = 5 },
                ["Launcher"] = new Amount() { ShortName = "rocket.launcher", Min = 1, Max = 1 },
                ["M249"] = new Amount() { ShortName = "lmg.m249", Min = 1, Max = 1 }
            },
            new Dictionary<string, Amount>()
            {
                ["C4"] = new Amount() { ShortName = "explosive.timed", Min = 7, Max = 10 },
                ["LauncherRocket"] = new Amount() { ShortName = "ammo.rocket.basic", Min = 4, Max = 11 },
                ["M249"] = new Amount() { ShortName = "lmg.m249", Min = 1, Max = 1 }
            },
            new Dictionary<string, Amount>()
            {
                ["C4"] = new Amount() { ShortName = "explosive.timed", Min = 10, Max = 15 },
                ["LauncherRocket"] = new Amount() { ShortName = "ammo.rocket.basic", Min = 15, Max = 35 },
                ["B4"] = new Amount() { ShortName = "explosive.satchel", Min = 19, Max = 31 }
            }
            };

            public Dictionary<string, float>[] RadiationRadius = new Dictionary<string, float>[]
            {
                new Dictionary<string, float>()
                {
                    ["Радиус радиации в первый день"] = 10,
                    ["Радиус радиации во второй день"] = 12,
                    ["Радиус радиации в третий день"] = 14,
                    ["Радиус радиации в четвертый день"] = 16,
                    ["Радиус радиации в пятый день"] = 18,
                    ["Радиус радиации в шестой день"] = 20,
                    ["Радиус радиации в седьмой день"] = 20,
                }
            };

            public Dictionary<string, float>[] RadiationIntensity = new Dictionary<string, float>[]
            {
                new Dictionary<string, float>()
                {
                    ["Радиация в первый день"] = 10,
                    ["Радиация во второй день"] = 15,
                    ["Радиация в третий день"] = 20,
                    ["Радиация в четвертый день"] = 25,
                    ["Радиация в пятый день"] = 30,
                    ["Радиация в шестой день"] = 35,
                    ["Радиация в седьмой день"] = 40,
                }
            };
            public DataStorage() { }
        }

        DataStorage data;
        private DynamicConfigFile RadData;

        public bool GuiOn = true;
        public string AnchorMinCfg = "0.3445 0.16075";
        public string AnchorMaxCfg = "0.6405 0.20075";
        public string ColorCfg = "1 1 1 0.1";
        public string TextGUI = "Radiation House:";
        public bool RadiationTrue = false;
        public string ChatPrefix = "<color=#ffe100>Radiation House:</color>";
        public int TimerSpawnHouse = 3600;
        public int TimerDestroyHouse = 60;
        public int TimerLoot = 300;
        public int TimeToRemove = 300;
        public int GradeNum = 1;
        public int MinPlayers = 10;

        public bool EnabledNPC = true;
        public int AmountNPC = 5;
        public bool LootNPC = true;

        #endregion


        protected override void LoadDefaultConfig()
        {
            LoadConfigValues();
        }

        private void LoadConfigValues()
        {
            DateOfWipe = DateTime.Now;
            DateOfWipeStr = DateOfWipe.ToString();
            GetConfig("[GUI]", "Включить GUI", ref GuiOn);
            GetConfig("[GUI]", "Anchor Min", ref AnchorMinCfg);
            GetConfig("[GUI]", "Anchor Max", ref AnchorMaxCfg);
            GetConfig("[GUI]", "Цвет фона", ref ColorCfg);
            GetConfig("[GUI]", "Текст в GUI окне", ref TextGUI);
            GetConfig("[Основное]", "Дата вайпа", ref DateOfWipeStr);
            GetConfig("[Основное]", "Префикс чата", ref ChatPrefix);
            GetConfig("[Основное]", "Минимальный онлайн для запуска ивента", ref MinPlayers);
            GetConfig("[Основное]", "Материал дома (0 - солома, 4 - мвк)", ref GradeNum);
            GetConfig("[Радиация]", "Отключить стандартную радиацию", ref RadiationTrue);
            GetConfig("[Основное]", "Время спавна дома", ref TimerSpawnHouse);
            GetConfig("[Основное]", "Задержка перед лутанием ящика", ref TimerLoot);
            GetConfig("[Основное]", "Задержка перед удалением дома", ref TimerDestroyHouse);
            GetConfig("[Основное]", "Время удаления дома если в течение N секунд никто не авторизовался в шкафу", ref TimeToRemove);

            GetConfig("[NPC]", "Включить создание NPC возле радиационного дома", ref EnabledNPC);
            GetConfig("[NPC]", "Количество созданых NPC", ref AmountNPC);
            GetConfig("[NPC]", "Удалять тело, и рюкзак NPC после его смерти", ref LootNPC);
            SaveConfig();
        }

        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }

        void OnServerInitialized()
        {
            RadData = Interface.Oxide.DataFileSystem.GetFile("RadHouseLoot");
            LoadData();
            LoadDefaultConfig();
            mytimer4 = timer.Once(TimerSpawnHouse, () =>
            {
                if (mytimer4 != null) mytimer4.Destroy();
                try
                {
                    if (BaseEntityList.Count > 0)
                    {
                        DestroyRadHouse();
                    }
                    CreateRadHouse(false);
                }
                catch (Exception ex) { Puts(ex.ToString()); }
            });

        }

        void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<DataStorage>("RadHouseLoot");
            }

            catch
            {
                data = new DataStorage();
            }
        }

        void Unload()
        {

            if (BaseEntityList != null) DestroyRadHouse();

            if (mytimer != null) timer.Destroy(ref mytimer);
            if (mytimer2 != null) timer.Destroy(ref mytimer2);
            if (mytimer3 != null) timer.Destroy(ref mytimer3);
            if (mytimer4 != null) timer.Destroy(ref mytimer4);
        }

        void OnNewSave(string filename)
        {
            DateOfWipe = DateTime.Now;
            string DateOfWipeStr = DateOfWipe.ToString();
            Config["[Основное]", "Дата вайпа"] = DateOfWipeStr;
            SaveConfig();
            PrintWarning($"Wipe detect. Дата установлена на {DateOfWipeStr}");
        }
        #endregion

        #region CreateAndDestroyRadHouse
        public object success;

        [ConsoleCommand("rh")]
        void CreateRadHouseConsoleCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (arg == null || arg.FullString.Length == 0 && arg.FullString != "start" && arg.FullString != "cancel")
            {
                SendReply(player, $"{ChatPrefix} Используйте /rh start или /rh cancel");
                return;
            }
            switch (arg.Args[0])
            {
                case "start":
                    SendReply(player, $"{ChatPrefix} Вы в ручную запустили ивент");
                    CreateRadHouse(true);
                    return;
                case "cancel":
                    SendReply(player, $"{ChatPrefix} Ивент остановлен");
                    DestroyRadHouse();
                    return;
            }
        }

        [ChatCommand("rh")]
        void CreateRadHouseCommand(BasePlayer player, string cmd, string[] Args)
        {
            if (player == null) return;
            if (!player.IsAdmin)
            {
                SendReply(player, $"{ChatPrefix} Команда доступна только администраторам");
                return;
            }
            if (Args == null || Args.Length == 0 || Args[0] != "start" && Args[0] != "cancel")
            {
                SendReply(player, $"{ChatPrefix} Используйте /rh start или /rh cancel");
                return;
            }
            switch (Args[0])
            {
                case "start":
                    SendReply(player, $"{ChatPrefix} Вы в ручную запустили ивент");
                    CreateRadHouse(true);
                    return;
                case "cancel":
                    SendReply(player, $"{ChatPrefix} Ивент остановлен");
                    DestroyRadHouse();
                    return;
            }

        }

        private void OnServerRadiation()
        {
            var allobjects = UnityEngine.Object.FindObjectsOfType<TriggerRadiation>();
            for (int i = 0; i < allobjects.Length; i++)
            {
                UnityEngine.Object.Destroy(allobjects[i]);
            }
        }

        Vector3 RadPosition;
        void CreateRadHouse(bool IsAdminCreate)
        {
            if (!IsAdminCreate && BasePlayer.activePlayerList.Count < MinPlayers)
            {
                PrintWarning("Не хватает игроков для запуска ивента");
                mytimer4 = timer.Once(TimerSpawnHouse, () =>
                {
                    if (mytimer4 != null) mytimer4.Destroy();
                    try
                    {
                        if (BaseEntityList.Count > 0)
                        {
                            DestroyRadHouse();
                        }
                        CreateRadHouse(false);
                    }
                    catch (Exception ex) { Puts(ex.ToString()); }
                });
                return;
            }
            if (BaseEntityList.Count > 0) DestroyRadHouse();
            Vector3 pos;
            pos.x = 0;
            pos.y = 0;
            pos.z = 0;
            success = GetEventPosition();
            pos = (Vector3)success;
            Puts(pos.ToString());
            RadPosition = pos;
            pos.x = pos.x + 0f; pos.y = pos.y + 1f; pos.z = pos.z + 0f;
            BaseEntity Foundation = GameManager.server.CreateEntity("assets/prefabs/building core/foundation/foundation.prefab", pos, new Quaternion(), true);

            pos.x = pos.x - 1.5f;
            BaseEntity Wall = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);
            Wall.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 0f; pos.y = pos.y + 1f; pos.z = pos.z + 3f;
            BaseEntity Foundation2 = GameManager.server.CreateEntity("assets/prefabs/building core/foundation/foundation.prefab", pos, new Quaternion(), true);

            pos.x = pos.x - 1.5f;
            BaseEntity Wall2 = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);
            Wall2.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

            //
            pos = (Vector3)success; pos.x = pos.x + 4.5f; pos.y = pos.y + 4f; pos.z = pos.z + 3f;
            BaseEntity Wall5 = GameManager.server.CreateEntity("assets/prefabs/building core/wall.window/wall.window.prefab", pos, new Quaternion(), true);
            Wall5.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 4.5f; pos.y = pos.y + 4f; pos.z = pos.z + 0f;
            BaseEntity Wall6 = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);
            Wall6.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

            pos = (Vector3)success; pos.x = pos.x - 1.5f; pos.y = pos.y + 4f; pos.z = pos.z + 0f;
            BaseEntity Wall7 = GameManager.server.CreateEntity("assets/prefabs/building core/wall.window/wall.window.prefab", pos, new Quaternion(), true);
            Wall7.transform.localEulerAngles = new Vector3(0f, 180f, 0f);


            pos = (Vector3)success; pos.x = pos.x - 1.5f; pos.y = pos.y + 4f; pos.z = pos.z + 3f;
            BaseEntity Wall8 = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);
            Wall8.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            //

            pos = (Vector3)success; pos.x = pos.x + 3f; pos.y = pos.y + 1f; pos.z = pos.z + 0f;
            BaseEntity Foundation3 = GameManager.server.CreateEntity("assets/prefabs/building core/foundation/foundation.prefab", pos, new Quaternion(), true);

            pos.x = pos.x + 1.5f;
            BaseEntity Wall3 = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);

            pos = (Vector3)success; pos.x = pos.x + 3f; pos.y = pos.y + 1f; pos.z = pos.z + 3f;
            BaseEntity Foundation4 = GameManager.server.CreateEntity("assets/prefabs/building core/foundation/foundation.prefab", pos, new Quaternion(), true);

            pos.x = pos.x + 1.5f;
            BaseEntity Wall4 = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);

            pos = (Vector3)success; pos.z = pos.z - 1.5f; pos.y = pos.y + 1f;
            BaseEntity DoorWay = GameManager.server.CreateEntity("assets/prefabs/building core/wall.doorway/wall.doorway.prefab", pos, new Quaternion(), true);
            DoorWay.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 3f; pos.y = pos.y + 1f; pos.z = pos.z + 4.5f;
            BaseEntity DoorWay2 = GameManager.server.CreateEntity("assets/prefabs/building core/wall.doorway/wall.doorway.prefab", pos, new Quaternion(), true);
            DoorWay2.transform.localEulerAngles = new Vector3(0f, 270f, 0f);

            pos = (Vector3)success; pos.z = pos.z - 1.5f; pos.y = pos.y + 1f; pos.x = pos.x + 3f;
            BaseEntity WindowWall = GameManager.server.CreateEntity("assets/prefabs/building core/wall.window/wall.window.prefab", pos, new Quaternion(), true);
            WindowWall.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 0f; pos.y = pos.y + 1f; pos.z = pos.z + 4.5f;
            BaseEntity WindowWall2 = GameManager.server.CreateEntity("assets/prefabs/building core/wall.window/wall.window.prefab", pos, new Quaternion(), true);
            WindowWall2.transform.localEulerAngles = new Vector3(0f, 270f, 0f);

            //yes
            pos = (Vector3)success; pos.x = pos.x + 0f; pos.y = pos.y + 4f; pos.z = pos.z + 4.5f;
            BaseEntity wall3 = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);
            wall3.transform.localEulerAngles = new Vector3(0f, 270f, 0f);
            //
            //yes
            pos = (Vector3)success; pos.x = pos.x + 3f; pos.y = pos.y + 4f; pos.z = pos.z + 4.5f;
            BaseEntity wall4 = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);
            wall4.transform.localEulerAngles = new Vector3(0f, 270f, 0f);
            //
            //yes
            pos = (Vector3)success; pos.z = pos.z - 1.5f; pos.y = pos.y + 4f; pos.x = pos.x + 3f;
            BaseEntity wall5 = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);
            wall5.transform.localEulerAngles = new Vector3(0f, 90f, 0f);
            //yes
            pos = (Vector3)success; pos.z = pos.z - 1.5f; pos.y = pos.y + 4f;
            BaseEntity wall6 = GameManager.server.CreateEntity("assets/prefabs/building core/wall/wall.prefab", pos, new Quaternion(), true);
            wall6.transform.localEulerAngles = new Vector3(0f, 90f, 0f);


            pos = (Vector3)success; pos.x = pos.x + 0f; pos.y = pos.y + 4f; pos.z = pos.z + 0f;
            BaseEntity Roof = GameManager.server.CreateEntity("assets/prefabs/building core/floor/floor.prefab", pos, new Quaternion(), true);
            Roof.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 3f; pos.y = pos.y + 1f; pos.z = pos.z + 0f;
            BaseEntity block = GameManager.server.CreateEntity("assets/prefabs/building core/stairs.l/block.stair.lshape.prefab", pos, new Quaternion(), true);
            block.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 0f; pos.y = pos.y + 4f; pos.z = pos.z + 3f;
            BaseEntity Roof1 = GameManager.server.CreateEntity("assets/prefabs/building core/floor/floor.prefab", pos, new Quaternion(), true);
            Roof1.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 3f; pos.y = pos.y + 4f; pos.z = pos.z + 3f;
            BaseEntity Roof2 = GameManager.server.CreateEntity("assets/prefabs/building core/floor/floor.prefab", pos, new Quaternion(), true);
            Roof2.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 0f; pos.y = pos.y + 7f; pos.z = pos.z + 0f;
            BaseEntity Roof3 = GameManager.server.CreateEntity("assets/prefabs/building core/floor/floor.prefab", pos, new Quaternion(), true);
            Roof3.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 3f; pos.y = pos.y + 7f; pos.z = pos.z + 0f;
            BaseEntity Roof4 = GameManager.server.CreateEntity("assets/prefabs/building core/floor/floor.prefab", pos, new Quaternion(), true);
            Roof4.transform.localEulerAngles = new Vector3(0f, 180f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 0f; pos.y = pos.y + 7f; pos.z = pos.z + 3f;
            BaseEntity Roof5 = GameManager.server.CreateEntity("assets/prefabs/building core/floor/floor.prefab", pos, new Quaternion(), true);
            Roof5.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 3f; pos.y = pos.y + 7f; pos.z = pos.z + 3f;
            BaseEntity Roof6 = GameManager.server.CreateEntity("assets/prefabs/building core/floor/floor.prefab", pos, new Quaternion(), true);
            Roof6.transform.localEulerAngles = new Vector3(0f, 0f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 4.0f; pos.y = pos.y + 4f; pos.z = pos.z + 4f;
            BaseEntity CupBoard = GameManager.server.CreateEntity("assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab", pos, new Quaternion(), true);
            CupBoard.transform.localEulerAngles = new Vector3(0f, 270f, 0f);

            pos = (Vector3)success; pos.x = pos.x - 0.7f; pos.y = pos.y + 4f; pos.z = pos.z - 0f;
            BaseEntity Bed = GameManager.server.CreateEntity("assets/prefabs/deployable/bed/bed_deployed.prefab", pos, new Quaternion(), true);
            Bed.transform.localEulerAngles = new Vector3(0f, 270f, 0f);

            pos = (Vector3)success; pos.x = pos.x - 0.85f; pos.y = pos.y + 4.01f; pos.z = pos.z + 3.45f;
            BaseEntity Box = GameManager.server.CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", pos, new Quaternion(), true);
            Box.skinID = 942917320;
            Box.SetFlag(BaseEntity.Flags.Locked, true);
            Box.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

            pos = (Vector3)success; pos.x = pos.x + 3; pos.y = pos.y - 0.5f; pos.z = pos.z + 7.5f;
            BaseEntity FSteps = GameManager.server.CreateEntity("assets/prefabs/building core/foundation.steps/foundation.steps.prefab", pos, new Quaternion(), true);
            FSteps.transform.localEulerAngles = new Vector3(0f, 90f, 0f);

            pos = (Vector3)success; pos.x = pos.x - 0f; pos.y = pos.y - 0.5f; pos.z = pos.z - 4.5f;
            BaseEntity FSteps2 = GameManager.server.CreateEntity("assets/prefabs/building core/foundation.steps/foundation.steps.prefab", pos, new Quaternion(), true);
            FSteps2.transform.localEulerAngles = new Vector3(0f, 270f, 0f);
            LootBox = Box;
            Foundation.Spawn();
            Wall.Spawn();
            Foundation2.Spawn();
            Wall2.Spawn();
            Foundation3.Spawn();
            Wall3.Spawn();
            Foundation4.Spawn();
            Wall4.Spawn();
            DoorWay.Spawn();
            DoorWay2.Spawn();
            WindowWall.Spawn();
            WindowWall2.Spawn();
            wall3.Spawn();
            Roof.Spawn();
            Roof1.Spawn();

            Roof3.Spawn();
            Roof4.Spawn();
            Roof5.Spawn();
            Roof6.Spawn();

            block.Spawn();
            Roof2.Spawn();
            wall4.Spawn();
            wall5.Spawn();
            wall6.Spawn();
            Wall5.Spawn();
            Wall6.Spawn();
            Wall7.Spawn();
            Wall8.Spawn();
            FSteps.Spawn();
            FSteps2.Spawn();
            CupBoard.Spawn();
            Box.Spawn();
            Bed.Spawn();

            BaseEntityList.Add(Foundation);
            BaseEntityList.Add(Roof);
            BaseEntityList.Add(block);
            BaseEntityList.Add(Roof1);
            BaseEntityList.Add(Roof2);

            BaseEntityList.Add(Roof3);
            BaseEntityList.Add(Roof4);
            BaseEntityList.Add(Roof5);
            BaseEntityList.Add(Roof6);

            BaseEntityList.Add(Foundation2);
            BaseEntityList.Add(Foundation3);
            BaseEntityList.Add(Foundation4);
            BaseEntityList.Add(Wall);
            BaseEntityList.Add(Wall2);
            BaseEntityList.Add(Wall3);
            BaseEntityList.Add(Wall4);
            BaseEntityList.Add(Wall7);
            BaseEntityList.Add(Wall8);
            BaseEntityList.Add(Wall6);
            BaseEntityList.Add(wall3);
            BaseEntityList.Add(wall4);
            BaseEntityList.Add(wall5);
            BaseEntityList.Add(wall6);
            BaseEntityList.Add(Wall5);
            BaseEntityList.Add(DoorWay);
            BaseEntityList.Add(DoorWay2);
            BaseEntityList.Add(WindowWall);
            BaseEntityList.Add(WindowWall2);
            BaseEntityList.Add(FSteps);
            BaseEntityList.Add(FSteps2);
            BaseEntityList.Add(CupBoard);
            BaseEntityList.Add(Box);
            BaseEntityList.Add(Bed);
            StorageContainer Container = Box.GetComponent<StorageContainer>();
            CreateLoot(Container, Box);
            var reply = 486;
            if (reply == 0) { };
            var buildingID = BuildingManager.server.NewBuildingID();
            try
            {
                foreach (var entity in BaseEntityList)
                {
                    DecayEntity decayEntity = entity.GetComponentInParent<DecayEntity>();
                    decayEntity.AttachToBuilding(buildingID);
                    if (entity.name.Contains("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab") && entity.name.Contains("assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab") && entity.name.Contains("assets/prefabs/building/wall.window.bars/wall.window.bars.metal.prefab")) break;
                    BuildingBlock buildingBlock = entity.GetComponent<BuildingBlock>();
                    buildingBlock.SetGrade((BuildingGrade.Enum)GradeNum);
                    buildingBlock.UpdateSkin();
                    buildingBlock.SetHealthToMax();
                    if (!entity.name.Contains("assets/prefabs/building core/foundation/foundation.prefab") && !entity.name.Contains("assets/prefabs/building core/foundation.steps/foundation.steps.prefab")) buildingBlock.grounded = true;
                    buildingBlock.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }
            }
            catch { }
            Server.Broadcast($"{ChatPrefix} Радиактивный дом появился, координаты: {pos.ToString()}\nЕсли никто не успеет авторизоваться за {TimeToRemove} секунд, он пропадет");
            mytimer5 = timer.Once(TimeToRemove, () =>
                {
                    if (BaseEntityList.Count > 0)
                    {
                        if (PlayerAuth.Count == 0)
                        {
                            DestroyRadHouse();
                            Server.Broadcast($"{ChatPrefix} Радиактивный дом удалился, никто не успел авторизоваться в шкафу");
                        }
                    }
                });

            foreach (var player in BasePlayer.activePlayerList)
            {
                CreateGui(player);
            }
            if (EnabledNPC) CreateNps(pos, AmountNPC);
            AddMapMarker();
            CanLoot = false;
            NowLooted = false;
            timercallbackdelay = 0;
        }

        private void CreateNps(Vector3 position, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                BaseEntity npc = GameManager.server.CreateEntity("assets/prefabs/npc/scientist/scientist.prefab", RandomCircle(position, 10), new Quaternion(), true);
                npc.Spawn();
                BaseEntityList.Add(npc);
            }
        }

        private void AddMapMarker()
        {
            LustyMap?.Call("AddMarker", LootBox.transform.position.x, LootBox.transform.position.z, "RadIcon", "https://i.imgur.com/TxUxuN7.png", 0);
            Map?.Call("ApiAddPointUrl", "https://i.imgur.com/TxUxuN7.png", "Радиактивный дом", LootBox.transform.position);
            RustMap?.Call("AddTemporaryMarker", "rad", false, 0.04f, 0.99f, LootBox.transform, "RadHouseMap");
        }

        private void RemoveMapMarker()
        {
            LustyMap?.Call("RemoveMarker", "RadIcon");
            Map?.Call("ApiRemovePointUrl", "https://i.imgur.com/TxUxuN7.png", "Радиактивный дом", LootBox.transform.position);
            RustMap?.Call("RemoveTemporaryMarkerByName", "RadHouseMap");
        }

        void DestroyRadHouse()
        {
            if (BaseEntityList != null)
            {
                foreach (BaseEntity entity in BaseEntityList)
                {
                    if (!entity.IsDestroyed)
                    entity.Kill();
                }
                DestroyZone(RadHouseZone);
                RemoveMapMarker();
                BaseEntityList.Clear();
                PlayerAuth.Clear();
                timer.Destroy(ref mytimer5);

            }
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyGui(player);
            }
            mytimer4 = timer.Once(TimerSpawnHouse, () =>
            {
                if (mytimer4 != null) mytimer4.Destroy();
                CreateRadHouse(false);
            });
            RadPosition = Vector3.zero;
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (BaseEntityList != null)
                {
                    foreach (BaseEntity entityInList in BaseEntityList)
                    {

                        if (entityInList.net.ID == entity.net.ID)
                        {
                            if (entityInList.name == "assets/prefabs/npc/scientist/scientist.prefab") return null;
                            return false;
                        }
                    }
                }
            }
            catch { return null; }
            return null;
        }

        void CreateLoot(StorageContainer Container, BaseEntity Box)
        {
            int Day = data.Common.Length - 1;
            DateTime DateOfWipeParse;
            DateTime.TryParse(DateOfWipeStr, out DateOfWipeParse);
            for (int i = 0; i <= data.Common.Length; i++)
            {
                if (DateOfWipeParse.AddDays(i) >= DateTime.Now)
                {
                    Day = i - 1;
                    break;
                }
            }
            ItemContainer inven = Container.inventory;
            if (Container != null)
            {
                var CommonList = data.Common[Day].Values.ToList();
                var RareList = data.Rare[Day].Values.ToList();
                var TopList = data.Top[Day].Values.ToList();
                for (var i = 0; i < CommonList.Count; i++)
                {
                    int j = UnityEngine.Random.Range(1, 10);
                    var item = ItemManager.CreateByName(CommonList[i].ShortName.ToString(), UnityEngine.Random.Range(Convert.ToInt32(CommonList[i].Min), Convert.ToInt32(CommonList[i].Max)));
                    if (j > 3)
                    {
                        item.MoveToContainer(Container.inventory, -1, false);
                    }
                }
                for (var i = 0; i < RareList.Count; i++)
                {
                    int j = UnityEngine.Random.Range(1, 10);
                    var item = ItemManager.CreateByName(RareList[i].ShortName.ToString(), UnityEngine.Random.Range(Convert.ToInt32(RareList[i].Min), Convert.ToInt32(RareList[i].Max)));
                    if (j > 5)
                    {
                        item.MoveToContainer(Container.inventory, -1, false);
                    }
                }
                for (var i = 0; i < TopList.Count; i++)
                {
                    int j = UnityEngine.Random.Range(1, 10);
                    var item = ItemManager.CreateByName(TopList[i].ShortName.ToString(), UnityEngine.Random.Range(Convert.ToInt32(TopList[i].Min), Convert.ToInt32(TopList[i].Max)));
                    if (j > 7)
                    {
                        item.MoveToContainer(Container.inventory, -1, false);
                    }
                }

                var Intensity = data.RadiationIntensity[0].Values.ToList();
                var Radius = data.RadiationRadius[0].Values.ToList();

                InitializeZone(Box.transform.position, Intensity[Day], Radius[Day], 2145);
            }
        }
        #endregion

        #region LootBox
        void CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (player == null) return;
            if (container == null && container?.net?.ID == null) return;
            if (BaseEntityList != null)
            {
                BaseEntity box = BaseEntityList.Find(p => p == container);
                if (box == null) return;
                if (box.net.ID == container.net.ID)
                {
                    if (box.name == "assets/prefabs/npc/scientist/scientist.prefab") return;
                    if (!CanLoot && PlayerAuth.Contains(player.userID))
                    {
                        SendReply(player, $"{ChatPrefix} Вы сможете залутать ящик, через: {mytimer.Delay - timercallbackdelay} секунд");
                        return;
                    }
                    else if (!PlayerAuth.Contains(player.userID))
                    {
                        SendReply(player, $"{ChatPrefix} Вы должны быть авторизованы в шкафу для лута ящика");
                    }
                }
            }
        }

        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null) return;
            if (entity?.net?.ID == null) return;
            if (!LootNPC) return;
            RemoveEntity(entity);
        }

        void RemoveEntity(BaseEntity entity)
        {
            var corpse = entity as NPCPlayerCorpse;
            if (RadPosition == Vector3.zero) return;
                if (corpse != null)
            {
                if (IsRadZone(corpse.transform.position))
                {
                    corpse.ResetRemovalTime(0.1f);
                }
            }
            if (entity is NPCPlayerCorpse || entity.name.Contains("item_drop_backpack"))
            {
                if (IsRadZone(entity.transform.position))
                {
                    NextTick(() =>
                    {
                        if (entity != null && !entity.IsDestroyed)
                        {
                            entity.Kill();
                        }
                    });
                }
            }
        }

        bool IsRadZone(Vector3 pos)
        {
            if (RadPosition != Vector3.zero)
            if (Vector3.Distance(RadPosition, pos) < 20)
                return true;
            return false;
        }


        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null) return;
            if (entity == null || entity?.net?.ID == null) return;
            if (BaseEntityList != null)
            {
                BaseEntity box = BaseEntityList.Find(p => p == entity);
                if (box == null) return;
                if (box.name == "assets/prefabs/npc/scientist/scientist.prefab")
                {
                    var corpse = box as BaseCorpse;
                    if (corpse != null)
                    {
                        Puts(corpse.ToString());
                    }
                }
                if (box == null) return;
                if (box.net.ID == entity.net.ID)
                {
                    BaseEntityList.Remove(entity);
                    return;
                }
            }
        }
        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (player == null) return;
            if (entity == null && entity?.net?.ID == null) return;
            if (BaseEntityList != null)
            {
                BaseEntity box = BaseEntityList.Find(p => p == entity);
                if (box == null) return;
                if (box.net.ID == entity.net.ID)
                {
                    if (box.name == "assets/prefabs/npc/scientist/scientist.prefab") return;
                    if (CanLoot)
                    {
                        if (PlayerAuth.Contains(player.userID))
                        {
                            if (!NowLooted)
                            {
                                NowLooted = true;
                                Server.Broadcast($"{ChatPrefix} Игрок {player.displayName} залутал ящик в радиактивном доме. \nДом самоуничтожится через {TimerDestroyHouse} секунд");
                                mytimer3 = timer.Once(TimerDestroyHouse, () =>
                                    {
                                        DestroyRadHouse();
                                    });
                            }
                        }
                    }

                }
            }
        }


        object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            var Cupboard = privilege as BuildingPrivlidge;
            var entity = privilege as BaseEntity;
            if (BaseEntityList != null)
            {
                foreach (BaseEntity entityInList in BaseEntityList)
                {
                    if (entityInList.net.ID == entity.net.ID)
                    {
                        if (PlayerAuth.Contains(player.userID))
                        {
                            SendReply(player, $"{ChatPrefix} Вы уже авторизованы");
                            return false;
                        }
                        foreach (var authPlayer in BasePlayer.activePlayerList)
                        {
                            if (PlayerAuth.Contains(authPlayer.userID))
                            {
                                SendReply(authPlayer, $"{ChatPrefix} Вас выписал из шкафа игрок {player.displayName}");
                            }
                        }
                        CanLoot = false;
                        PlayerAuth.Clear();
                        timer.Destroy(ref mytimer);
                        timer.Destroy(ref mytimer2);
                        if (mytimer5 != null) timer.Destroy(ref mytimer5);
                        timercallbackdelay = 0;
                        mytimer = timer.Once(TimerLoot, () =>
                        {
                            CanLoot = true;
                            LootBox.SetFlag(BaseEntity.Flags.Locked, false);
                            foreach (var authPlayer in BasePlayer.activePlayerList)
                            {
                                if (PlayerAuth.Contains(authPlayer.userID))
                                {
                                    SendReply(authPlayer, $"{ChatPrefix} Вы можете залутать ящик");
                                }
                            }
                        });
                        mytimer2 = timer.Repeat(1f, 0, () =>
                        {
                            if (timercallbackdelay >= TimerLoot)
                            {
                                timercallbackdelay = 0;
                                timer.Destroy(ref mytimer2);
                            }
                            else
                            {
                                timercallbackdelay = timercallbackdelay + 1;
                            }
                        });
                        PlayerAuth.Add(player.userID);
                        SendReply(player, $"{ChatPrefix} Через {TimerLoot} секунд вы сможете залутать ящик радиационного дома");
                        return false;
                    }
                }
            }
            return null;
        }
        #endregion

        #region Spawn
        SpawnFilter filter = new SpawnFilter();
        List<Vector3> monuments = new List<Vector3>();

        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" })) && !hit.collider.name.Contains("rock_cliff"))
                return Mathf.Max(hit.point.y, y);
            return y;
        }

        public Vector3 RandomDropPosition()
        {
            var vector = Vector3.zero;
            float num = 1000f, x = TerrainMeta.Size.x / 3;

            do
            {
                vector = Vector3Ex.Range(-x, x);
            }
            while (filter.GetFactor(vector) == 0f && (num -= 1f) > 0f);
            float max = TerrainMeta.Size.x / 2;
            float height = TerrainMeta.HeightMap.GetHeight(vector);
            vector.y = height;
            return vector;
        }

        List<int> BlockedLayers = new List<int> { (int)Layer.Water, (int)Layer.Construction, (int)Layer.Trigger, (int)Layer.Prevent_Building, (int)Layer.Deployed, (int)Layer.Tree };
        static int blockedMask = LayerMask.GetMask(new[] { "Player (Server)", "Trigger", "Prevent Building" });

        public Vector3 GetSafeDropPosition(Vector3 position)
        {
            RaycastHit hit;
            position.y += 200f;

            if (Physics.Raycast(position, Vector3.down, out hit))
            {
                if (hit.collider?.gameObject == null)
                    return Vector3.zero;
                string ColName = hit.collider.name;

                if (!BlockedLayers.Contains(hit.collider.gameObject.layer) && ColName != "MeshColliderBatch" && ColName != "iceberg_3" && ColName != "iceberg_2" && !ColName.Contains("rock_cliff"))
                {
                    position.y = Mathf.Max(hit.point.y, TerrainMeta.HeightMap.GetHeight(position));
                    var colliders = Pool.GetList<Collider>();
                    Vis.Colliders(position, 1, colliders, blockedMask, QueryTriggerInteraction.Collide);
                    bool blocked = colliders.Count > 0;
                    Pool.FreeList<Collider>(ref colliders);
                    if (!blocked)
                        return position;
                }
            }

            return Vector3.zero;
        }

        public Vector3 GetEventPosition()
        {
            var eventPos = Vector3.zero;
            int maxRetries = 100;
            monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>().Select(monument => monument.transform.position).ToList();
            do
            {
                eventPos = GetSafeDropPosition(RandomDropPosition());

                foreach (var monument in monuments)
                {
                    if (Vector3.Distance(eventPos, monument) < 150f)
                    {
                        eventPos = Vector3.zero;
                        break;
                    }
                }
            } while (eventPos == Vector3.zero && --maxRetries > 0);

            return eventPos;
        }

        Vector3 RandomCircle(Vector3 center, float radius = 2)
        {
            float ang = UnityEngine.Random.value * 360;
            Vector3 pos;
            pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
            pos.y = center.y;
            pos.y = GetGroundPosition(pos);
            return pos;
        }
        #endregion

        #region GUI
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (BaseEntityList.Count > 0)
            {
                if (GuiOn)
                {
                    DestroyGui(player);
                    CreateGui(player);
                }
            }

        }

        void CreateGui(BasePlayer player)
        {
            if (GuiOn)
            {
                Vector3 pos = (Vector3)success;
                CuiElementContainer Container = new CuiElementContainer();
                CuiElement RadUI = new CuiElement
                {
                    Name = "RadUI",
                    Components = {
                        new CuiImageComponent {
                            Color = ColorCfg
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = AnchorMinCfg,
                            AnchorMax = AnchorMaxCfg
                        }
                    }
                };
                CuiElement RadText = new CuiElement
                {
                    Name = "RadText",
                    Parent = "RadUI",
                    Components = {
                        new CuiTextComponent {
                            Text = $"{TextGUI} {pos.ToString()}",
                            Align = TextAnchor.MiddleCenter
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                };

                Container.Add(RadUI);
                Container.Add(RadText);
                CuiHelper.AddUi(player, Container);
            }
        }

        void DestroyGui(BasePlayer player)
        {
            if (GuiOn)
            {
                CuiHelper.DestroyUi(player, "RadUI");
            }
        }
        #endregion

        // Create radiation
        #region Radiation Control
        private void InitializeZone(Vector3 Location, float intensity, float radius, int ZoneID)
        {
            if (!ConVar.Server.radiation)
                ConVar.Server.radiation = true;
            if (RadiationTrue)
            {
                OnServerRadiation();
            }
            var newZone = new GameObject().AddComponent<RadZones>();
            newZone.Activate(Location, radius, intensity, ZoneID);

            ZoneList listEntry = new ZoneList { zone = newZone };
            RadHouseZone = listEntry;
            RadiationZones.Add(listEntry);
        }
        private void DestroyZone(ZoneList zone)
        {
            if (RadiationZones.Contains(zone))
            {
                var index = RadiationZones.FindIndex(a => a.zone == zone.zone);
                UnityEngine.Object.Destroy(RadiationZones[index].zone);
                RadiationZones.Remove(zone);
            }
        }
        public class ZoneList
        {
            public RadZones zone;
        }

        public class RadZones : MonoBehaviour
        {
            private int ID;
            private Vector3 Position;
            private float ZoneRadius;
            private float RadiationAmount;

            private List<BasePlayer> InZone;

            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "NukeZone";

                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
            }

            public void Activate(Vector3 pos, float radius, float amount, int ZoneID)
            {
                ID = ZoneID;
                Position = pos;
                ZoneRadius = radius;
                RadiationAmount = amount;

                gameObject.name = $"RadHouse{ID}";
                transform.position = Position;
                transform.rotation = new Quaternion();
                UpdateCollider();
                gameObject.SetActive(true);
                enabled = true;

                var Rads = gameObject.GetComponent<TriggerRadiation>();
                Rads = Rads ?? gameObject.AddComponent<TriggerRadiation>();
                Rads.RadiationAmountOverride = RadiationAmount;
                Rads.radiationSize = ZoneRadius;
                Rads.interestLayers = playerLayer;
                Rads.enabled = true;

                if (IsInvoking("UpdateTrigger")) CancelInvoke("UpdateTrigger");
                InvokeRepeating("UpdateTrigger", 5f, 5f);
            }

            private void OnDestroy()
            {
                CancelInvoke("UpdateTrigger");
                Destroy(gameObject);
            }

            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>();
                {
                    if (sphereCollider == null)
                    {
                        sphereCollider = gameObject.AddComponent<SphereCollider>();
                        sphereCollider.isTrigger = true;
                    }
                    sphereCollider.radius = ZoneRadius;
                }
            }
            private void UpdateTrigger()
            {
                InZone = new List<BasePlayer>();
                int entities = Physics.OverlapSphereNonAlloc(Position, ZoneRadius, colBuffer, playerLayer);
                for (var i = 0; i < entities; i++)
                {
                    var player = colBuffer[i].GetComponentInParent<BasePlayer>();
                    if (player != null)
                        InZone.Add(player);
                }
            }
        }
        #endregion
    }
}
                              