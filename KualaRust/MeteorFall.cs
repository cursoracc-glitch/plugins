using System;
using Random = System.Random;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Rust;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Collections;
using System.Globalization;
namespace Oxide.Plugins
{
    [Info("MeteorFall", "EcoSmile (RustPlugin.ru)", "2.0.5")]
    class MeteorFall : RustPlugin
    {
        [PluginReference] Plugin EcoMap, RustMap, LustyMap, Map;
        private PluginConfig config;
        private class PluginConfig
        {
            [JsonProperty("Общие Настройки")]
            //[JsonProperty("General settings")]
            public Options _Options;

            public class Options
            {
                [JsonProperty("Отключать стандартную радиацию?")]
                //[JsonProperty("Disable standard radiation?")]
                public bool OffStandartRad;
                [JsonProperty("Включить автозапуск ивента?")]
                //[JsonProperty("Enable autorun event?")]
                public bool EnableAutomaticEvents;
                [JsonProperty("Настройка автозапуска")]
                //[JsonProperty("Autostart setup")]
                public Timers EventTimers;
                [JsonProperty("Сообщать о начале ивента в чат?")]
                //[JsonProperty("Report event start to chat?")]
                public bool NotifyEvent;
                [JsonProperty("Включить эффект тряски земли от падения метеорита?")]
                //[JsonProperty("Enable earthquake effect?")]
                public bool Earthquake { get; set; }
                [JsonProperty("Минимальное количество игроков для запуска ивента")]
                //[JsonProperty("Minimum number of players to run an event")]
                public int MinPlayer;
            }
            [JsonProperty("Настройки UI")]
            //[JsonProperty("UI Settings")]
            public UiSettings uiSettings;
            public class UiSettings
            {
                [JsonProperty("Включить UI?")]
                //[JsonProperty("Enable UI?")]
                public bool useUi;
                [JsonProperty("Ссылка на картинку")]
                //[JsonProperty("Image link")]
                public string IconImage;
                [JsonProperty("Положение UI")]
                //[JsonProperty("UI Position")]
                public UiTransform uiTransform;
                public class UiTransform
                {
                    [JsonProperty("Координата Х Мин")]
                    //[JsonProperty("Coordinate X Min")]
                    public string AnchorXMin;
                    [JsonProperty("Координата Х Мax")]
                    //[JsonProperty("Coordinate X Мax")]
                    public string AnchorXMax;
                    [JsonProperty("Координата Y Мин")]
                    //[JsonProperty("Coordinate Y Min")]
                    public string AnchorYMin;
                    [JsonProperty("Координата Y Мax")]
                    //[JsonProperty("Coordinate Y Мax")]
                    public string AnchorYMax;
                }
            }
            [JsonProperty("Настройки радиации")]
            //[JsonProperty("Radiation settings")]
            public Radiations _Radiations;
            public class Radiations
            {
                [JsonProperty("Радиус зоны")]
                //[JsonProperty("Zone radius")]
                public float Radius;
                [JsonProperty("Сила радиации")]
                //[JsonProperty("Radiation strength")]
                public float Strange;
            }
            public class Timers
            {
                [JsonProperty("Интервал ивента (Минуты) (Если выключен рандом)")]
                //[JsonProperty("Event Interval (Minutes) (If Random time is Off)")]
                public int EventInterval;
                [JsonProperty("Включить рандомное время?")]
                //[JsonProperty("Enable random time?")]
                public bool UseRandomTimer;
                [JsonProperty("Минимальный интервал (Минуты)")]
                //[JsonProperty("Minimum Interval (Minutes)")]
                public int RandomTimerMin;
                [JsonProperty("Максимальный интервал (Минуты)")]
                //[JsonProperty("Maximum Interval (Minutes)")]
                public int RandomTimerMax;
            }
            [JsonProperty("Настройка ивента")]
            //[JsonProperty("Event Setting")]
            public Intensity Settings;
            public class Intensity
            {
                [JsonProperty("Шанс распространения огня от малого метеорита")]
                //[JsonProperty("Chance of a small meteorite to spread fire")]
                public int FireRocketChance;
                [JsonProperty("Радиус на котором проходит метеоритопад")]
                //[JsonProperty("The radius of the meteorite shower")]
                public float Radius;
                [JsonProperty("Количество падающих метеоритов (малых)")]
                //[JsonProperty("The number of falling meteorites (small)")]
                public int RocketAmount;
                [JsonProperty("Длительность падения малых метеоритов")]
                //[JsonProperty("The duration of the fall of small meteorites")]
                public int Duration;
                [JsonProperty("Множитель урона от попадания по Enemy")]
                //[JsonProperty("Enemy Hit Damage Multiplier")]
                public float DamageMultiplier;
                [JsonProperty("Настройка выпадающих ресурсов после попадания метеорита по земле")]
                //[JsonProperty("Resource setting after meteor exlosions")]
                public Drop ItemDropControl;
                public class Drop
                {
                    [JsonProperty("Включить дроп ресурсов после метеорита?")]
                    //[JsonProperty("Enable resource drop after small meteorite?")]
                    public bool EnableItemDrop;
                    [JsonProperty("Настройка выпадаемых ресурсов")]
                    //[JsonProperty("Resources settings")]
                    public ItemDrops[] ItemsToDrop;
                }
                [JsonProperty("Количество NPC возле главного метеорита")]
                //[JsonProperty("Number of NPC near the main meteorite")]
                public int NpcAmount;
                [JsonProperty("Включить спавн NPC возле метеорита?")]
                //[JsonProperty("Enable NPC spawn?")]
                public bool NpcSpawn;
                [JsonProperty("Количество HP у ученых")]
                //[JsonProperty("Sceintist HP")]
                public float NpcHealth;
                [JsonProperty("Время которое будет остывать метеорит (Минуты)")]
                //[JsonProperty("The time that the meteorite will cool (Minutes)")]
                public float FireTime;
                [JsonProperty("Время через которое метеорит исчезнет после остывания (Минуты)")]
                //[JsonProperty("Time after which the meteorite disappears after cooling (Minutes)")]
                public float DespawnTime;
            }
            [JsonProperty("Настройки метеорита")]
            //[JsonProperty("Meteorite settings")]
            public MeteorSetting meteorSetting;
            public class MeteorSetting
            {
                [JsonProperty("Время до падения метеорита")]
                //[JsonProperty("Time to meteorite fall")]
                public int MeteorTime;
                [JsonProperty("Шанс того, что метеорит будет радиоактивен (0-отключить)")]
                //[JsonProperty("The chance that the meteorite will be radioactive (0-disable)")]
                public float RadChacnce;
                [JsonProperty("Запускать волну радиации после приземления если метеорит радиоактивный?")]
                //[JsonProperty("Run a radiation wave after landing if a meteorite is radioactive?")]
                public bool RadWave;
                [JsonProperty("HP серной руды (стандартно 500)")]
                //[JsonProperty("HP Sulfur Ore (500 standard)")]
                public float SulfurHealth;
                [JsonProperty("HP металлической руды (стандартно 500)")]
                //[JsonProperty("HP metal ore (500 standard)")]
                public float MetalHealth;
                [JsonProperty("Наносить дамаг по области?")]
                //[JsonProperty("Do damage by area?")]
                public bool SplashDamage;
                [JsonProperty("Радиус области")]
                //[JsonProperty("Area radius")]
                public float splashRadius;
                [JsonProperty("Наносимый дамаг (Урон наносится всем строительным блокам) ")]
                //[JsonProperty("Damage amount (Damage dealt to all building blocks) ")]
                public float DamageAmount;
            }

            [JsonProperty("Настройка семечки")]
            //[JsonProperty("Seed Setting")]
            public SeedSettings seedSettings;

            public class SeedSettings
            {
                [JsonProperty("Включить выпадение семечки?")]
                //[JsonProperty("Enable seed drop?")]
                public bool IsEnable;
                [JsonProperty("Cажать семечку только на грядки?")]
                //[JsonProperty("Plant a seed only on the beds?")]
                public bool PlantOnly;
                [JsonProperty("Скин семенчки")]
                //[JsonProperty("Seed skinID")]
                public ulong SeedSkinID;
                [JsonProperty("Название семечки")]
                //[JsonProperty("Custom seed name")]
                public string SeedName;
                [JsonProperty("Время через которое обьект вырастет (секунды)")]
                //[JsonProperty("The time it takes for the object to grow (seconds)")]
                public float TimeToRelise;
                [JsonProperty("Прифаб обьекта - Настройка")]
                //[JsonProperty("Prifab object - Settings")]
                public Dictionary<string, ObjectSetting> PrefabSetting;
            }
        }

        public class ObjectSetting
        {
            [JsonProperty("Максимальное ХП обьекта")]
            //[JsonProperty("Maximum object HP")]
            public float MaxHealth;
            [JsonProperty("Шанс спавна обьекта")]
            //[JsonProperty("Object spawn chance")]
            public float SpawnChance;
        }
        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig()
            {
                _Options = new PluginConfig.Options
                {
                    OffStandartRad = false,
                    EnableAutomaticEvents = true,
                    MinPlayer = 5,
                    EventTimers = new PluginConfig.Timers
                    {
                        EventInterval = 60,
                        UseRandomTimer = true,
                        RandomTimerMin = 20,
                        RandomTimerMax = 30
                    }
                    ,
                    NotifyEvent = true,
                    Earthquake = true,
                }
                ,
                uiSettings = new PluginConfig.UiSettings
                {
                    useUi = true,
                    IconImage = "https://i.imgur.com/1PSiC85.png",
                    uiTransform = new PluginConfig.UiSettings.UiTransform
                    {
                        AnchorXMax = "0.6414062",
                        AnchorXMin = "0.34375",
                        AnchorYMax = "0.2083333",
                        AnchorYMin = "0.1097223"
                    }
                }
                ,
                _Radiations = new PluginConfig.Radiations
                {
                    Radius = 10f,
                    Strange = 5f,
                }
                ,
                Settings = new PluginConfig.Intensity
                {
                    Duration = 180,
                    FireRocketChance = 20,
                    Radius = 50,
                    RocketAmount = 90,
                    DamageMultiplier = 0.4f,
                    NpcAmount = 5,
                    NpcSpawn = false,
                    NpcHealth = 300f,
                    FireTime = 5,
                    DespawnTime = 10,
                    ItemDropControl = new PluginConfig.Intensity.Drop
                    {
                        EnableItemDrop = true,
                        ItemsToDrop = new ItemDrops[] {
                            new ItemDrops {
                                Maximum=150, Minimum=80, Shortname="stones"
                            }
                            , new ItemDrops {
                                Maximum=100, Minimum=50, Shortname="metal.ore"
                            }
                            , new ItemDrops {
                                Maximum=90, Minimum=40, Shortname="sulfur"
                            }
                            ,
                        }
                    }
                }
                ,
                meteorSetting = new PluginConfig.MeteorSetting
                {
                    MeteorTime = 30,
                    RadChacnce = 70,
                    RadWave = true,
                    SulfurHealth = 1000,
                    MetalHealth = 1500,
                    SplashDamage = false,
                    splashRadius = 100,
                    DamageAmount = 250
                },
                seedSettings = new PluginConfig.SeedSettings()
                {
                    IsEnable = true,
                    PlantOnly = true,
                    TimeToRelise = 30f,
                    SeedSkinID = 2131201310,
                    SeedName = "Метеоритное семя",
                    PrefabSetting = new Dictionary<string, ObjectSetting>()
                    {
                        ["assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab"] = new ObjectSetting
                        {
                            MaxHealth = 500,
                            SpawnChance = 33,
                        },
                        ["assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab"] = new ObjectSetting
                        {
                            MaxHealth = 500,
                            SpawnChance = 33,
                        },
                        ["assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab"] = new ObjectSetting
                        {
                            MaxHealth = 500,
                            SpawnChance = 33,
                        },

                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        class ItemDrops
        {
            public string Shortname;
            public int Minimum;
            public int Maximum;
        }

        static MeteorFall ins;
        private bool rads;
        void OnServerInitialized()
        {
            ins = this;
            LoadConfig();
            LoadMessages();
            StartEventTimer();
            InitFileManager();
            ServerMgr.Instance.StartCoroutine(LoadImages());

        }

        private void OnServerRadiation()
        {
            var allobjects = UnityEngine.Object.FindObjectsOfType<TriggerRadiation>();
            for (int i = 0; i < allobjects.Length; i++)
            {
                UnityEngine.Object.Destroy(allobjects[i]);
            }
        }

        void Unload()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<ItemCarrier>();
            if (objects != null) foreach (var gameObj in objects) UnityEngine.Object.Destroy(gameObj);

            var objectsSeed = UnityEngine.Object.FindObjectsOfType<MeteorSeed>();
            foreach (var gameObj in objectsSeed) UnityEngine.Object.Destroy(gameObj);

            Despawn();
            if (EventTimer != null) EventTimer.Destroy();
            foreach (var check in radiationZone) UnityEngine.Object.Destroy(check);
            radiationZone.Clear();
            if (!rads) ConVar.Server.radiation = false;
            if (mapMarker != null && !mapMarker.IsDestroyed) mapMarker.Kill();
            if (MarkerT != null && !MarkerT.IsDestroyed) MarkerT.Kill();
        }

        private Timer EventTimer, AlertTimer;
        private float launchStraightness = 2.0f;
        private float launchHeight = 150f;
        private float MapSize() => TerrainMeta.Size.x / 2;
        private float projectileSpeed = 250f;
        private float gravityModifier = 0.2f;
        private float detonationTime = 60f;

        class ItemCarrier : MonoBehaviour
        {
            private ItemDrops[] carriedItems = null;
            private float multiplier;
            public void SetCarriedItems(ItemDrops[] carriedItems) => this.carriedItems = carriedItems;
            public void SetDropMultiplier(float multiplier) => this.multiplier = 1.0f;
            private void OnDestroy()
            {
                if (carriedItems == null) return;
                int amount;
                for (int i = 0; i < carriedItems.Length; i++)
                {
                    if ((amount = (int)(UnityEngine.Random.Range(carriedItems[i].Minimum, carriedItems[i].Maximum) * 1.0f)) > 0)
                        ItemManager.CreateByName(carriedItems[i].Shortname, amount).Drop(gameObject.transform.position, Vector3.up);
                }
            }
        }

        private void StartEventTimer()
        {
            if (config._Options.EnableAutomaticEvents)
            {
                if (config._Options.EventTimers.UseRandomTimer)
                {
                    var random = UnityEngine.Random.Range(config._Options.EventTimers.RandomTimerMin, config._Options.EventTimers.RandomTimerMax);
                    EventTimer = timer.Once(random * 60, () => StartRandomOnMap());
                }
                else EventTimer = timer.Once(config._Options.EventTimers.EventInterval * 60, () => StartRandomOnMap());
            }
        }
        private void StopTimer()
        {
            if (rockettimer != null) rockettimer?.Destroy();
            if (AlertTimer != null) AlertTimer?.Destroy();
            if (EventTimer != null) EventTimer?.Destroy();
            if (npcTimer != null) timer.Destroy(ref npcTimer);
            npcTimer?.Destroy();
            if (mystimer != null) mystimer?.Destroy();
            if (mystimer1 != null) mystimer1?.Destroy();
            if (mystimer2 != null) mystimer2?.Destroy();
            if (mystimer3 != null) mystimer3?.Destroy();
            if (mystimer4 != null) mystimer4?.Destroy();
            if (mystimer5 != null) mystimer5?.Destroy();
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, "CuiElementMt");
        }
        private void StartRandomOnMap()
        {
            if (config._Options.NotifyEvent)
            {
                foreach (var pl in BasePlayer.activePlayerList) SendToChat(pl, GetMsg("incoming", pl.userID));
            }
            mystimer2 = timer.In(10f, () => CollisionAtm());
        }
        private Timer rockettimer, effecttimer, timerfireball;
        List<uint> MeteorList = new List<uint>();
        List<uint> MeteorFound = new List<uint>();
        List<NPCPlayer> NpcList = new List<NPCPlayer>();
        List<uint> FireBall = new List<uint>();
        public List<string> prefabOsn = new List<string>() {
            "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab", "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab", "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab"
        }
        ;
        Quaternion qTo;
        BaseEntity met;
        private Timer mystimer, mystimer3, mystimer4, mystimer5;
        private Timer mystimer1;
        private Timer mystimer2;
        void CollisionAtm()
        {
            PrintWarning($"Event Started!!!!!!!!!");
            //if (BasePlayer.activePlayerList.Count < config._Options.MinPlayer)
            //{
            //    foreach (var pl in BasePlayer.activePlayerList) SendToChat(pl, GetMsg("EventCancel", pl.userID));
            //    StartEventTimer();
            //    return;
            //}
            if (!ConVar.Server.radiation)
            {
                rads = false;
                if (config._Options.OffStandartRad) OnServerRadiation();
                ConVar.Server.radiation = true;
            }
            else rads = true;
            mystimer2 = timer.Once(10f, () =>
            {
                if (config._Options.NotifyEvent) foreach (var pl in BasePlayer.activePlayerList) SendToChat(pl, GetMsg("InAtm", pl.userID));
            }
            );
            var callAt = GetEventPosition();
            int cooldown = config.meteorSetting.MeteorTime >= 5 ? config.meteorSetting.MeteorTime : 5;
            mystimer1 = mystimer = timer.Repeat(1.1f, cooldown, () =>
            {
                if (cooldown == 1)
                {
                    timer.In(1f, () => StartRainOfFire(callAt));
                }
                if (cooldown <= 5 && cooldown != 0)
                {
                    if (config._Options.NotifyEvent) foreach (var pl in BasePlayer.activePlayerList) SendToChat(pl, GetMsg("Dropped", pl.userID).Replace("{cooldown}", $"{cooldown}"));
                }
                if (cooldown >= 0)
                {
                    foreach (var player in BasePlayer.activePlayerList)
                        DrawUI(player, GetMsg("TFallUI", player.userID).Replace("{time}", $"{FormatTime(TimeSpan.FromSeconds(cooldown))}"));
                    cooldown--;
                }
                if (cooldown <= 0)
                {
                    mystimer1.Destroy();
                    foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, "CuiElementMt");
                }
            }
            );
        }
        Vector3 spawnpos;
        private Vector3 MeteorSpawnPos()
        {
            return spawnpos;
        }

        private void StartRainOfFire(Vector3 origin)
        {
            float radius = config.Settings.Radius;
            int numberOfRockets = config.Settings.RocketAmount;
            float duration = config.Settings.Duration;
            bool dropsItems = config.Settings.ItemDropControl.EnableItemDrop;
            ItemDrops[] itemDrops = config.Settings.ItemDropControl.ItemsToDrop;
            float intervals = duration / numberOfRockets;
            rockettimer = timer.Repeat(intervals, numberOfRockets, () => RandomRocket(origin, radius));
            Random rnd = new Random();
            var location = origin;
            location.y = GetGroundPosition(location);
            float posy;
            float posx;
            float posz;
            string pref = "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab";
            int meteorcount = 0;

            meteorcount = UnityEngine.Random.Range(10, 20);
            timer.In(2f, () =>
            {
                foreach (var pl in BasePlayer.activePlayerList)
                    SendToChat(pl, GetMsg("MajorMeteor", pl.userID));
            });

            for (int i = 0; i < (meteorcount > 2 ? meteorcount : 2); i++)
            {
                qTo = Quaternion.Euler(new Vector3(UnityEngine.Random.Range(-30, 30), UnityEngine.Random.Range(-150, -50), 0));

                if (i % 2 == 0) pref = "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab";
                else pref = "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab";
                posy = i - (i - UnityEngine.Random.Range(0.3f, 0.6f));
                posx = i - (i - UnityEngine.Random.Range(-1f, +1f));
                posz = i - (i - UnityEngine.Random.Range(-1f, +1f));
                location.y = location.y + posy;
                location.x = location.x + posx;
                met = GameManager.server.CreateEntity(pref, (Vector3)location, qTo, true);
                met.Spawn();
                if (i == 2)
                    RustMap?.Call("AddTemporaryMarker", "meteor", false, 0.05f, 0.95f, met.transform, "Метеорит");
                OreResourceEntity ore = met.GetComponent<OreResourceEntity>();
                if (met.ShortPrefabName.Contains("sulfur"))
                {
                    foreach (var it in ore.resourceDispenser.containedItems)
                    {
                        it.startAmount = it.startAmount * config.meteorSetting.SulfurHealth / 500;
                        it.amount = it.amount * config.meteorSetting.SulfurHealth / 500;
                    }
                }
                else
                {
                    foreach (var it in ore.resourceDispenser.containedItems)
                    {
                        it.startAmount = it.startAmount * config.meteorSetting.MetalHealth / 500;
                        it.amount = it.amount * config.meteorSetting.MetalHealth / 500;
                    }
                }
                MeteorList.Add(met.net.ID);
                MeteorFound.Add(met.net.ID);
            }

            AlertTimer = timer.In(10f, () =>
            {
                if (config._Options.NotifyEvent)
                {
                    foreach (var pl in BasePlayer.activePlayerList) SendToChat(pl, GetMsg("colding", pl.userID).Replace("{time}", $"{FormatTime(TimeSpan.FromSeconds(config.Settings.FireTime * 60))}"));
                }
            });

            mystimer3 = timer.In(20f, () =>
            {
                if (config._Options.NotifyEvent)
                {
                    foreach (var pl in BasePlayer.activePlayerList) SendToChat(pl, GetMsg("Warning", pl.userID).Replace("{radius}", $"{config.Settings.Radius}"));
                }
            });

            int cooldown2 = Convert.ToInt32((config.Settings.FireTime * 60) + 10);
            mystimer4 = timer.Repeat(1f, cooldown2, () =>
            {
                if (cooldown2 == 1)
                {
                    ColdMeteorite();
                }
                if (1 < cooldown2 && cooldown2 <= config.Settings.FireTime * 60)
                    foreach (var player in BasePlayer.activePlayerList)
                        DrawUI(player, GetMsg("TCoolUI", player.userID).Replace("{time}", $"{FormatTime(TimeSpan.FromSeconds(cooldown2))}"));
                if (cooldown2 != 0) cooldown2--;
            }
            );
            CreateFireBall(location, new Vector3(0, 2, 0));
            CreateFireBall(location, new Vector3(2, 0, 0));
            CreateFireBall(location, new Vector3(-2, 0, 0));
            CreateFireBall(location, new Vector3(0, 0, 2));
            CreateFireBall(location, new Vector3(0, 0, -2));
            Effect.server.Run("assets/prefabs/npc/m2bradley/effects/bradley_explosion.prefab", location);
            Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab", location);
            timer.Once(1f, () =>
            {
                Effect.server.Run("assets/prefabs/npc/m2bradley/effects/bradley_explosion.prefab", location);
                Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab", location);
            }
            );
            if (config._Options.Earthquake) foreach (var pl in BasePlayer.activePlayerList) Screen(pl);
            var radchance = UnityEngine.Random.Range(0f, 100f);
            if (radchance < config.meteorSetting.RadChacnce)
            {
                if (config.meteorSetting.RadWave) CrateWave();
                CreateZone(location);
            }
            if (config.Settings.NpcSpawn) CreateNpc(location, config.Settings.NpcAmount);
            EcoMap?.Call("AddMapMarker", "meteor", false, 0.0300f, location, "Метеорит");
            LustyMap?.Call("AddMarker", location.x, location.z, "Метеорит", "https://i.imgur.com/BFCdsOx.png");
            Map?.Call("ApiAddPoint", location, "https://i.imgur.com/BFCdsOx.png");
            CreatePrivateMap(location);
            if (config.meteorSetting.SplashDamage)
                DamageObjects(location);
        }

        void DamageObjects(Vector3 pos)
        {
            List<BuildingBlock> list = Pool.GetList<BuildingBlock>();
            Vis.Entities(pos, config.meteorSetting.splashRadius, list);
            if (list.Count > 0)
                foreach (var obj in list)
                    obj.Hurt(config.meteorSetting.DamageAmount);
            Pool.FreeList(ref list);
        }

        private MapMarkerGenericRadius mapMarker;
        private VendingMachineMapMarker MarkerT;
        private UnityEngine.Color ConvertToColor(string color)
        {
            if (color.StartsWith("#")) color = color.Substring(1);
            int red = int.Parse(color.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            int green = int.Parse(color.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            int blue = int.Parse(color.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            return new UnityEngine.Color((float)red / 255, (float)green / 255, (float)blue / 255);
        }
        private void CreatePrivateMap(Vector3 pos)
        {
            mapMarker = (MapMarkerGenericRadius)GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", pos, new Quaternion());
            MarkerT = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", pos).GetComponent<VendingMachineMapMarker>();
            mapMarker.enableSaving = false;
            MarkerT.markerShopName = "<size=18>Метеорит</size>";
            MarkerT.enableSaving = false;
            MarkerT.Spawn();
            MarkerT.enabled = false;
            mapMarker.Spawn();
            mapMarker.radius = 0.2f;
            mapMarker.alpha = 1f;
            UnityEngine.Color color = ConvertToColor("#932e1d");
            UnityEngine.Color color2 = new UnityEngine.Color(0, 0, 0, 0);
            mapMarker.color1 = color;
            mapMarker.color2 = color2;
            mapMarker.SendUpdate();
        }
        List<string> prefabscreen = new List<string>()
        {
            "assets/prefabs/weapons/bone knife/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/hatchet/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/pickaxe/effects/strike_screenshake.prefab",
            "assets/prefabs/weapons/salvaged_axe/effects/strike_screenshake.prefab",
            "assets/bundled/prefabs/fx/screen_jump.prefab"
        };
        Random rnd = new Random();
        void Screen(BasePlayer pl)
        {
            effecttimer = timer.Repeat(0.2f, 21, () =>
            {
                string screanp = prefabscreen[rnd.Next(prefabscreen.Count)];
                Effect.server.Run(screanp, pl.transform.position);
            });
        }
        private void ColdMeteorite()
        {
            timerfireball?.Destroy();
            foreach (var check in FireBall)
            {
                if (BaseNetworkable.serverEntities.Find(check) != null && !BaseNetworkable.serverEntities.Find(check).IsDestroyed) BaseNetworkable.serverEntities.Find(check).Kill();
            }
            FireBall.Clear();
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, "CuiElementMt");
            if (config._Options.NotifyEvent)
            {
                foreach (var pl in BasePlayer.activePlayerList) SendToChat(pl, GetMsg("coldingFinish", pl.userID));
            }
            AlertTimer = timer.Once(60f, () =>
            {
                if (config._Options.NotifyEvent)
                {
                    foreach (var pl in BasePlayer.activePlayerList) SendToChat(pl, GetMsg("Despawn", pl.userID).Replace("{time}", $"{FormatTime(TimeSpan.FromSeconds(config.Settings.DespawnTime * 60))}"));
                }
            }
            );
            int desptime = Convert.ToInt32((config.Settings.DespawnTime * 60) + 60f);
            mystimer5 = timer.Every(1f, () =>
            {
                if (MeteorFound.Count > 0)
                {
                    if (desptime == 1)
                    {
                        foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, "CuiElementMt");
                        Despawn();
                    }
                    if (1 < desptime && desptime <= config.Settings.DespawnTime * 60)
                    {
                        foreach (var player in BasePlayer.activePlayerList)
                            DrawUI(player, GetMsg("TDownUI", player.userID).Replace("{time}", $"{FormatTime(TimeSpan.FromSeconds(desptime))}"));
                        //DrawUI(player, $"Метеорит исчезнет через: {FormatTime(TimeSpan.FromSeconds(desptime))}");
                    }
                    if (desptime != 0) desptime--;
                }
                else
                {
                    if (mystimer5 != null) timer.Destroy(ref mystimer5);
                    mystimer5?.Destroy();
                }
            }
            );
        }
        Dictionary<Scientist, Vector3> npcPos = new Dictionary<Scientist, Vector3>();
        private void CreateNpc(Vector3 position, int amount = 0)
        {
            for (int i = 0; i < amount; i++)
            {
                var pos = RandomCircle(position, 10);
                Scientist scientist = GameManager.server.CreateEntity("assets/prefabs/npc/scientist/scientist.prefab", pos) as Scientist;
                scientist.Spawn();
                npcPos.Add(scientist, pos);
                NpcList.Add(scientist);
            }
            ToSpawnPoint();
        }
        private Timer npcTimer;
        void ToSpawnPoint()
        {
            npcTimer = timer.Every(5f, () =>
            {
                foreach (var npc in npcPos.Keys.ToList())
                    if (npc != null && npcPos.ContainsKey(npc) && npc?.AttackTarget == null)
                        npc?.SetDestination(npcPos[npc]);
            });
        }

        private void CreateFireBall(Vector3 position, Vector3 additional)
        {
            FireBall fireball_ = null;
            fireball_ = GameManager.server.CreateEntity("assets/bundled/prefabs/oilfireballsmall.prefab", position + additional) as FireBall;
            fireball_.lifeTimeMin = config.Settings.FireTime * 60 + 10;
            fireball_.lifeTimeMax = config.Settings.FireTime * 60 + 10;
            fireball_.Spawn();
            FireBall.Add(fireball_.net.ID);
        }

        private void Despawn()
        {
            foreach (var check in MeteorList)
            {
                var metent = BaseNetworkable.serverEntities.Find(check);
                if (metent != null && !metent.IsDestroyed)
                    metent.Kill();
            }
            foreach (var check in NpcList)
            {
                if (check != null && !check.IsDestroyed && !check.IsDead())
                    check.Kill();
            }
            foreach (var check in FireBall)
            {
                var Ball = BaseNetworkable.serverEntities.Find(check);
                if (Ball != null && !Ball.IsDestroyed)
                    Ball.Kill();
            }
            foreach (var check in radiationZone) if (check != null) UnityEngine.Object.Destroy(check);
            radiationZone.Clear();

            if (mapMarker != null && !mapMarker.IsDestroyed) mapMarker.Kill();
            if (MarkerT != null && !MarkerT.IsDestroyed) MarkerT.Kill();

            MeteorList.Clear();
            MeteorFound.Clear();
            FireBall.Clear();
            NpcList.Clear();
            StopTimer();
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, "CuiElementMt");
            EcoMap?.Call("RemoveMapMarker", "Метеорит");
            RustMap?.Call("RemoveTemporaryMarkerByName", "Метеорит");
            LustyMap?.Call("RemoveMarker", "Метеорит");
            if (met != null)
                Map?.Call("ApiRemovePoint", met);
            StartEventTimer();
            PrintWarning($"Event Stoped!!!!!!!!!!");
        }
        private void RandomRocket(Vector3 origin, float radius)
        {
            bool isFireRocket = false;
            Vector2 rand = UnityEngine.Random.insideUnitCircle;
            Vector3 offset = new Vector3(rand.x * radius, 0, rand.y * radius);
            Random rnd = new Random();
            Vector3 direction = (Vector3.up * -launchStraightness + Vector3.right).normalized;
            Vector3 launchPos = origin + offset - direction * launchHeight;
            if (UnityEngine.Random.Range(1, config.Settings.FireRocketChance) == 1) isFireRocket = true;
            BaseEntity rocket = CreateRocket(launchPos, direction, isFireRocket);
            if (config.Settings.ItemDropControl.EnableItemDrop)
            {
                var comp = rocket.gameObject.AddComponent<ItemCarrier>();
                comp.SetCarriedItems(config.Settings.ItemDropControl.ItemsToDrop);
                comp.SetDropMultiplier(1.0f);
            }
        }
        private BaseEntity CreateRocket(Vector3 startPoint, Vector3 direction, bool isFireRocket)
        {
            ItemDefinition projectileItem;
            if (isFireRocket) projectileItem = GetFireRocket();
            else projectileItem = GetRocket();
            ItemModProjectile component = projectileItem.GetComponent<ItemModProjectile>();
            BaseEntity entity = GameManager.server.CreateEntity(component.projectileObject.resourcePath, startPoint, new Quaternion(), true);
            TimedExplosive timedExplosive = entity.GetComponent<TimedExplosive>();
            ServerProjectile serverProjectile = entity.GetComponent<ServerProjectile>();
            serverProjectile.gravityModifier = gravityModifier;
            serverProjectile.speed = projectileSpeed;
            timedExplosive.timerAmountMin = detonationTime;
            timedExplosive.timerAmountMax = detonationTime;
            ScaleAllDamage(timedExplosive.damageTypes, config.Settings.DamageMultiplier);
            entity.SendMessage("InitializeVelocity", (object)(direction * 30f));
            entity.Spawn();
            return entity;
        }
        private void ScaleAllDamage(List<DamageTypeEntry> damageTypes, float scale)
        {
            for (int i = 0;
            i < damageTypes.Count;
            i++) damageTypes[i].amount *= scale;
        }
        private ItemDefinition GetRocket() => ItemManager.FindItemDefinition("ammo.rocket.hv");
        private ItemDefinition GetFireRocket() => ItemManager.FindItemDefinition("ammo.rocket.fire");
        [ConsoleCommand("meteor")]
        void ConsoleStart(ConsoleSystem.Arg args)
        {
            if (args.Connection != null) if (!args.IsConnectionAdmin) return;
            if (args.Args == null || args.Args.Length < 0)
            {
                Puts("MeteorFall by EcoSmile (RustPlugin.ru)\nКоманды:\n\tmeteor start - Ручной запуск ивента\n\tmeteor stop - Остановить текущий запущенный ивент");
                return;
            }
            switch (args.Args[0])
            {
                case "start":
                    foreach (var check in MeteorList)
                    {
                        if (BaseNetworkable.serverEntities.Find(check) != null && !BaseNetworkable.serverEntities.Find(check).IsDestroyed)
                        {
                            PrintWarning("Ивент MeteorFall уже запущен, используйте: meteor stop");
                            return;
                        }
                    }
                    Despawn();
                    PrintWarning("Вы запустили Ивент MeteorFall в ручном режиме");
                    StartRandomOnMap();
                    break;
                case "stop":
                    StopTimer();
                    Despawn();
                    PrintWarning("Ивент MeteorFall остановлен");
                    break;
            }
        }
        [ChatCommand("meteor")]
        void ChatCmdControll(BasePlayer player, string command, string[] args, ulong playerid = 533504)
        {
            if (player == null) return;
            if (!player.IsAdmin) return;
            if (args == null || args.Length <= 0)
            {
                SendToChat(player, GetMsg("CmdHelp", player.userID));
                return;
            }
            switch (args[0])
            {
                case "start":
                    foreach (var check in MeteorList)
                    {
                        if (BaseNetworkable.serverEntities.Find(check) != null && !BaseNetworkable.serverEntities.Find(check).IsDestroyed)
                        {
                            SendToChat(player, GetMsg("CmdEventCurrent", player.userID));
                            return;
                        }
                    }
                    if (EventTimer != null) EventTimer.Destroy();
                    SendToChat(player, GetMsg("CmdEventStart", player.userID));
                    StartRandomOnMap();
                    break;
                case "stop":
                    StopTimer();
                    Despawn();
                    SendToChat(player, GetMsg("CmdEventStop", player.userID));
                    break;
            }
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return;
            var ID = dispenser.GetComponent<BaseEntity>().net.ID;
            if (ID != 0 && MeteorFound.Contains(ID))
            {
                if (config.seedSettings.IsEnable)
                {
                    GiveSeed(player);
                }
            }
        }

        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser.GetComponent<MeteorSeed>() != null)
            {
                var player = entity.ToPlayer();
                SendToChat(player, GetMsg("SeedCrash", player.userID));
                UnityEngine.Object.Destroy(dispenser.GetComponent<MeteorSeed>());
                dispenser.GetComponent<OreResourceEntity>().UpdateNetworkStage();
            }
            return null;
        }

        void OnEntityBuilt(Planner planner, GameObject gameobject, Vector3 Pos)
        {
            if (planner == null || gameobject == null) return;
            var player = planner.GetOwnerPlayer();
            BaseEntity entity = gameobject.ToBaseEntity();
            if (entity == null) return;
            if (entity.skinID == config.seedSettings.SeedSkinID)
            {
                NextTick(() =>
                {
                    if (entity != null && !entity.IsDestroyed)
                    {
                        if (config.seedSettings.PlantOnly && entity.GetParentEntity() == null)
                        {
                            entity.Kill();
                            GiveSeed(player);
                            SendToChat(player, GetMsg("OnlyPlant", player.userID));
                            return;
                        }

                        SetFirstStage(entity.transform.position);
                        entity.Kill();
                    }

                });
            }
        }

        void GiveSeed(BasePlayer player, int amount = 1)
        {
            Item seed = ItemManager.CreateByName("seed.hemp", amount, config.seedSettings.SeedSkinID);
            if (!string.IsNullOrEmpty(config.seedSettings.SeedName))
                seed.name = config.seedSettings.SeedName;
            player.GiveItem(seed);
        }

        [ChatCommand("mseed")]
        void mSeed_cmd(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (arg.Length == 0)
            {
                SendReply(player, $"/mseed amount");
                return;
            }
            int amount = int.Parse(arg[0]);
            GiveSeed(player, amount);
        }


        void SetFirstStage(Vector3 pos)
        {
            OreResourceEntity ore = GameManager.server.CreateEntity(OrePrefab(), pos) as OreResourceEntity;
            ore.stage = 3;
            ore.Spawn();
            ore.gameObject.AddComponent<MeteorSeed>();
        }

        string OrePrefab()
        {
            float chance = UnityEngine.Random.Range(0, 100);
            var prefab = "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab";
            int maxtry = 100;

            do
            {
                prefab = ins.config.seedSettings.PrefabSetting.Keys.ToList().GetRandom();
                chance = UnityEngine.Random.Range(0, 100);
                if (maxtry > 0)
                    maxtry--;
                else
                    return prefab;
            }
            while (ins.config.seedSettings.PrefabSetting[prefab].SpawnChance < chance);
            
            return prefab;
        }

        public class MeteorSeed : FacepunchBehaviour
        {
            OreResourceEntity ore;
            StagedResourceEntity stage;

            void Awake()
            {
                ore = GetComponent<OreResourceEntity>();
                InvokeRepeating(OreProgress, ins.config.seedSettings.TimeToRelise / 4, ins.config.seedSettings.TimeToRelise / 4);
            }

            void OreProgress()
            {
                if (ore.stage > 0)
                    ore.stage--;
                GroundWatch.PhysicsChanged(ore.GetComponent<StagedResourceEntity>().gameObject);
                ore.GetComponent<StagedResourceEntity>().SendNetworkUpdate();
                ore.SendNetworkUpdate();
                if (ore.stage == 0)
                {
                    foreach (var it in ore.resourceDispenser.containedItems)
                    {
                        it.startAmount = it.startAmount * ins.config.seedSettings.PrefabSetting[ore.PrefabName].MaxHealth / 500;
                        it.amount = it.amount * ins.config.seedSettings.PrefabSetting[ore.PrefabName].MaxHealth / 500;
                    }
                    CancelInvoke(OreProgress);
                    Destroy(this);
                    return;
                }
            }

            void OnDestroy()
            {
                CancelInvoke(OreProgress);
                Destroy(this);
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (entity.net == null) return;
            if (MeteorFound.Contains(entity.net.ID))
            {
                if (MeteorFound.Count == 1)
                {
                    StopTimer();

                    if (mapMarker != null && !mapMarker.IsDestroyed) mapMarker?.Kill();
                    if (MarkerT != null && !MarkerT.IsDestroyed) MarkerT?.Kill();

                    foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, "CuiElementMt");
                    EcoMap?.Call("RemoveMapMarker", "Метеорит");
                    RustMap?.Call("RemoveTemporaryMarkerByName", "Метеорит");
                    LustyMap?.Call("RemoveMarker", "Метеорит");
                    MeteorFound.Clear();
                    if (met != null)
                        Map?.Call("ApiRemovePoint", met, "https://i.imgur.com/BFCdsOx.png");
                    StartEventTimer();
                    return;
                }
                MeteorFound.Remove(entity.net.ID);
            }
        }

        void DrawUI(BasePlayer player, string msg)
        {
            if (!config.uiSettings.useUi) return;
            if (!string.IsNullOrEmpty(config.uiSettings.uiTransform.AnchorXMax) && !string.IsNullOrEmpty(config.uiSettings.uiTransform.AnchorYMax) && !string.IsNullOrEmpty(config.uiSettings.uiTransform.AnchorXMin) && !string.IsNullOrEmpty(config.uiSettings.uiTransform.AnchorYMin))
            {
                string anchormax_ = $"{config.uiSettings.uiTransform.AnchorXMax} {config.uiSettings.uiTransform.AnchorYMax}";
                string anchormin_ = $"{config.uiSettings.uiTransform.AnchorXMin} {config.uiSettings.uiTransform.AnchorYMin}";
                CuiHelper.DestroyUi(player, "CuiElementMt");
                CuiHelper.AddUi(player, ui.Replace("{anchormax}", anchormax_).Replace("{anchormin}", anchormin_).Replace("{Icon}", Images).Replace("{Text}", msg));
            }
            else
            {
                string anchormax_ = "0.6414062 0.2083333";
                string anchormin_ = "0.34375 0.1097223";
                CuiHelper.DestroyUi(player, "CuiElementMt");
                CuiHelper.AddUi(player, ui.Replace("{anchormax}", anchormax_).Replace("{anchormin}", anchormin_).Replace("{Icon}", Images).Replace("{Text}", msg));
            }
        }
        string ui = "[{\"name\":\"CuiElementMt\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"0 0 0 0.2394277\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\"},{\"type\":\"RectTransform\",\"anchormin\":\"{anchormin}\",\"anchormax\":\"{anchormax}\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"CuiElementMt\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"0 0 0 0.434163\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.007874042 0.09859085\",\"anchormax\":\"0.1758531 0.915493\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"CuiElementMt\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{Icon}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01049873 0.07042181\",\"anchormax\":\"0.1758531 0.915493\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"CuiElementMt\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"0 0 0 0.3316553\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2020998 0.1236111\",\"anchormax\":\"0.9658796 0.8450704\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"CuiElementMt\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Text}\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.7238988 0.3568805 0.1592568 1\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2047244 0.1236111\",\"anchormax\":\"0.9580055 0.8309858\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";
        bool init;
        private GameObject FileManagerObject;
        private FileManager m_FileManager;
        private string Images;
        IEnumerator LoadImages()
        {
            if (!string.IsNullOrEmpty(config.uiSettings.IconImage))
            {
                Images = config.uiSettings.IconImage;
                yield return m_FileManager.StartCoroutine(m_FileManager.LoadFile(Images, Images));
                Images = m_FileManager.GetPng(Images);
            }
            else
            {
                Images = "https://i.imgur.com/1PSiC85.png";
                yield return m_FileManager.StartCoroutine(m_FileManager.LoadFile(Images, Images));
                Images = m_FileManager.GetPng(Images);
            }
        }
        void InitFileManager()
        {
            FileManagerObject = new GameObject("FileManagerObject");
            m_FileManager = FileManagerObject.AddComponent<FileManager>();
        }
        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;
            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();
            private class FileInfo
            {
                public string Url;
                public string Png;
            }
            public string GetPng(string name) => files[name].Png;
            public IEnumerator LoadFile(string name, string url, int size = -1)
            {
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo()
                {
                    Url = url
                }
                ;
                needed++;
                yield return StartCoroutine(LoadImageCoroutine(name, url, size));
            }
            IEnumerator LoadImageCoroutine(string name, string url, int size = -1)
            {
                using (WWW www = new WWW(url))
                {
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        var bytes = size == -1 ? www.bytes : Resize(www.bytes, size);
                        var entityId = CommunityEntity.ServerInstance.net.ID;
                        var crc32 = FileStorage.server.Store(bytes, FileStorage.Type.png, entityId).ToString();
                        files[name].Png = crc32;
                    }
                }
                ins.init = true;
                loaded++;
            }
            static byte[] Resize(byte[] bytes, int size)
            {
                Image img = (Bitmap)(new ImageConverter().ConvertFrom(bytes));
                Bitmap cutPiece = new Bitmap(size, size);
                System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(cutPiece);
                graphic.DrawImage(img, new Rectangle(0, 0, size, size), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);
                graphic.Dispose();
                MemoryStream ms = new MemoryStream();
                cutPiece.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
        private List<RadiationZone> radiationZone = new List<RadiationZone>();
        private class RadiationZone : MonoBehaviour
        {
            private TriggerRadiation rads;
            public float radius;
            public float amount;
            private void Awake()
            {
                gameObject.layer = (int)Rust.Layer.Reserved1;
                enabled = false;
            }
            private void OnDestroy() => Destroy(gameObject);
            public void DestroyZone()
            {
                try
                {
                    Destroy(this);
                }
                catch (Exception ex)
                {
                    ins.LogToFile("MeteorLog", $"Инфа о ошибке DestroyZone: {ex.Message} {Environment.NewLine} {ex.StackTrace}", ins, true);
                }
            }
            public void InitializeRadiationZone(Vector3 position, float radius, float amount)
            {
                this.radius = radius;
                this.amount = amount;
                transform.position = position;
                transform.rotation = new Quaternion();
                UpdateCollider();
                rads = gameObject.AddComponent<TriggerRadiation>();
                rads.RadiationAmountOverride = amount;
                //rads.radiationSize = radius;
                rads.interestLayers = LayerMask.GetMask("Player (Server)");
                rads.enabled = true;
            }
            public void Deactivate() => rads.gameObject.SetActive(false);
            public void Reactivate() => rads.gameObject.SetActive(true);
            public void AmountChange(float amount)
            {
                this.amount = amount;
                rads.RadiationAmountOverride = amount;
            }
            private void UpdateCollider()
            {
                var sphereCollider = gameObject.GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = radius;
            }
        }
        private void CreateZone(Vector3 pos)
        {
            var newZone = new GameObject().AddComponent<RadiationZone>();
            newZone.InitializeRadiationZone(pos, config._Radiations.Radius, config._Radiations.Strange);
            radiationZone.Add(newZone);
        }
        private void CrateWave()
        {
            var newZone = new GameObject().AddComponent<RadiationZone>();
            newZone.InitializeRadiationZone(Vector3.zero, ConVar.Server.worldsize, config._Radiations.Strange);
            radiationZone.Add(newZone);
            timer.In(10f, () => newZone.DestroyZone());
        }
        SpawnFilter filter = new SpawnFilter();
        List<Vector3> monuments = new List<Vector3>();
        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity,
                LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" })) && !hit.collider.name.Contains("rock_cliff")) return Mathf.Max(hit.point.y, y);
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
        List<int> BlockedLayers = new List<int> {
            (int)Layer.Water, (int)Layer.Construction, (int)Layer.Trigger, (int)Layer.Prevent_Building, (int)Layer.Deployed, (int)Layer.Tree
        }
        ;
        static int blockedMask = LayerMask.GetMask(new[] {
            "Player (Server)", "Trigger", "Prevent Building"
        }
        );
        public Vector3 GetSafeDropPosition(Vector3 position)
        {
            RaycastHit hit;
            position.y += 200f;
            if (Physics.Raycast(position, Vector3.down, out hit))
            {
                if (hit.collider?.gameObject == null) return Vector3.zero;
                string ColName = hit.collider.name;
                if (!BlockedLayers.Contains(hit.collider.gameObject.layer) && ColName != "MeshColliderBatch" && ColName != "iceberg_3" && ColName != "iceberg_2" && !ColName.Contains("rock_cliff"))
                {
                    position.y = Mathf.Max(hit.point.y, TerrainMeta.HeightMap.GetHeight(position));
                    var colliders = Pool.GetList<Collider>();
                    Vis.Colliders(position, 1, colliders, blockedMask, QueryTriggerInteraction.Collide);
                    bool blocked = colliders.Count > 0;
                    Pool.FreeList<Collider>(ref colliders);
                    if (!blocked) return position;
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
            }
            while (eventPos == Vector3.zero && --maxRetries > 0);
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
        private void SendToChat(BasePlayer Player, string Message)
        {
            PrintToChat(Player, Message);
        }
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID?.ToString());
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["incoming"] = "<size=16><color=red>Attention!</color> \nAn island falling <color=#FF4500><b>METEORITE</b></color>! \nCollision is inevitable!</size>",
                ["colding"] = "<size=14>Scientists have discovered a <color=#FF4500><b>METEORITE</b></color>, its location is marked on the map! \nThe meteorite cools through {time}</size>",
                ["InAtm"] = "<size=14><color=red>Attention!</color> \n<color=#FF4500><b>Meteorite</b></color> entered the atmosphere of the planet</size>",
                ["Dropped"] = "<size=14><color=red>Collision through {cooldown}...</color></size>",
                ["Warning"] = "<size=14>Careful! At the site of the fall of the meteorite <b>fragments</b> continue to fall within a radius of {radius} meters.</size>",
                ["coldingFinish"] = "<size=14>The temperature of the <color=#FF4500><b>METEOR</b></color> has reached the planetary temperatur!</size>",
                ["Despawn"] = "<size=14><color=#FF4500><b>The METEORITE</b></color> began to disintegrate under the influence of oxygen. \nEstimated time of disappearance {time}</size>",
                ["EventCancel"] = "<size=14><color=#FF4500><b>The METEORITE</b></color> flew past the planet! The astronomers made a mistake.</size>\n<size=10>Not enough players to start the event.</size>",
                ["CmdHelp"] = "<size=16>MeteorFall by EcoSmile (RustPlugin.ru)</size>\nCommands:\n\t/meteor start - Start event\n\t/meteor stop - Stop current event",
                ["CmdEventStart"] = "You ran the MeteorFall Event manually",
                ["CmdEventStop"] = "Event MeteorFall stopped",
                ["CmdEventCurrent"] = "The MeteorFall event is already running, use: / meteor stop",
                ["MajorMeteor"] = "The meteorite at the entrance to the atmosphere is almost not damaged and has retained its gigantic size!",
                ["MinorMeteor"] = "The meteorite at the entrance to the atmosphere slightly damaged but its size is still frightening!",
                ["SmalMeteor"] = "The meteorite at the entrance to the atmosphere is severely damaged to the earth will reach only a small fragment.",
                ["OnlyPlant"] = "Meteor seed can only be planted in the Planter Box",
                ["SeedCrash"] = "You have disturbed the growth of the seed and now it will not be so fruitful.",
                ["TDownUI"] = "The meteorite will disappear in: {time}",
                ["TCoolUI"] = "The meteorite will cool down in a minute: {time}",
                ["TFallUI"] = "The meteorite will fall through: {time}",
            }
            , this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["incoming"] = "<size=16><color=red>Внимание!</color> \nНа остров падает <color=#FF4500><b>МЕТЕОРИТ</b></color>! \nСтолкновение неизбежно!</size>",
                ["colding"] = "<size=14>Ученые обнаружили <color=#FF4500><b>МЕТЕОРИТ</b></color>, его местоположение отмечено на карте! \nМетеорит остынет через {time}</size>",
                ["InAtm"] = "<size=14><color=red>ВНИМАНИЕ!</color> \n<color=#FF4500><b>Метеорит</b></color> вошел в атмосферу!</size>",
                ["Dropped"] = "<size=14><color=red>Столкновние через {cooldown}...</color></size>",
                ["Warning"] = "<size=14>Осторожно! На месте падения метеорита продолжают падать <b>осколки</b> в радиусе {radius} метров.</size>",
                ["coldingFinish"] = "<size=14>Температура <color=#FF4500><b>МЕТЕОРА</b></color> достигла планетной температуры!</size>",
                ["Despawn"] = "<size=14><color=#FF4500><b>МЕТЕОРИТ</b></color> начал распадаться под действием кислорода. \nОриентировочное время исчезновения {time}</size>",
                ["EventCancel"] = "<size=14><color=#FF4500><b>МЕТЕОРИТ</b></color> прошел мимо планеты! \nАстраномы ошиблись.</size>\n<size=10>Недостаточно игроков для запуска ивента.</size>",
                ["CmdHelp"] = "<size=16>MeteorFall by EcoSmile (RustPlugin.ru)</size>\nКоманды:\n\t/meteor start - Ручной запуск ивента\n\t/meteor stop - Остановить текущий запущенный ивент",
                ["CmdEventStart"] = "Вы запустили Ивент MeteorFall в ручном режиме",
                ["CmdEventStop"] = "Ивент MeteorFall остановлен",
                ["CmdEventCurrent"] = "Ивент MeteorFall уже запущен, используйте: /meteor stop",
                ["MajorMeteor"] = "Метеорит при входе в атмосферу почти не повредился и сохранил свои гигантские размеры!",
                ["MinorMeteor"] = "Метеорит при входе в атмосферу слегка повредился но его размеры все еще пугают!",
                ["SmalMeteor"] = "Метеорит при входе в атмосферу сильно повредился до земли долетит лишь не большой обломок.",
                ["OnlyPlant"] = "Семечко можно посадить только в грядки",
                ["SeedCrash"] = "Вы нарушили помешали росту семени теперь оно не будет так блодоносно.",
                ["TDownUI"] = "Метеорит исчезнет через: {time}",
                ["TCoolUI"] = "Метеорит остынет через: {time}",
                ["TFallUI"] = "Метеорит упадет через: {time}"
            }
            , this, "ru");
        }
        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0) result += $"{Format(time.Days, "д.", "д.", "д.")} ";
            if (time.Hours != 0) result += $"{Format(time.Hours, "ч.", "ч.", "ч.")} ";
            if (time.Minutes != 0) result += $"{Format(time.Minutes, "мин", "мин", "мин")} ";
            if (time.Seconds != 0) result += $"{Format(time.Seconds, "сек", "сек", "сек")} ";
            return result;
        }
        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;
            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9) return $"{units} {form1}";
            if (tmp >= 2 && tmp <= 4) return $"{units} {form2}";
            return $"{units} {form3}";
        }
    }
}