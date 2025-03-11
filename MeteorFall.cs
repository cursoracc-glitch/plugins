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
    [Info("MeteorFall", "EcoSmile (RustPlugin.ru)", "1.0.14")]
    class MeteorFall : RustPlugin
    {
        [PluginReference]
        Plugin EcoMap, RustMap, LustyMap, Map;
        
        #region Config
        private PluginConfig config;
        private class PluginConfig
        {
            [JsonProperty("Общие Настройки")]
            public Options _Options { get; set; }
            public class Options
            {
                [JsonProperty("Отключать стандартую радиацию?")]
                public bool OffStandartRad { get; set; }
                [JsonProperty("Включить автозапуск ивента?")]
                public bool EnableAutomaticEvents { get; set; }
                [JsonProperty("Настройка автозапуска")]
                public Timers EventTimers { get; set; }
                [JsonProperty("Сообщать о начале ивента в чат?")]
                public bool NotifyEvent { get; set; }
                [JsonProperty("Включить эффект тряски земли от падения метеорита?")]
                public bool Earthquake { get; set; }
                [JsonProperty("Минимальное количество игроков для запуска ивента")]
                public int MinPlayer { get; set; }

            }
            [JsonProperty("Настройки UI")]
            public UiSettings uiSettings { get; set; }
            public class UiSettings
            {
                [JsonProperty("Включить UI?")]
                public bool useUi { get; set; }
                [JsonProperty("Ссылка на картинку")]
                public string IconImage { get; set; }
                [JsonProperty("Положение UI")]
                public UiTransform uiTransform { get; set; }
                public class UiTransform
                {
                    [JsonProperty("Координата Х Мин")]
                    public string AnchorXMin { get; set; }
                    [JsonProperty("Координата Х Мax")]
                    public string AnchorXMax { get; set; }
                    [JsonProperty("Координата Y Мин")]
                    public string AnchorYMin { get; set; }
                    [JsonProperty("Координата Y Мax")]
                    public string AnchorYMax { get; set; }
                }
            }
            [JsonProperty("Настройки радиации")]
            public Radiations _Radiations { get; set; }
            public class Radiations
            {
                [JsonProperty("Включить создание радиации возле метеорита?")]
                public bool EnableRadZone { get; set; }
                [JsonProperty("Радиус зоны")]
                public float Radius { get; set; }
                [JsonProperty("Сила радиации")]
                public float Strange { get; set; }
            }
            public class Timers
            {
                [JsonProperty("Интервал ивента (Минуты) (Если выключен рандом)")]
                public int EventInterval { get; set; }
                [JsonProperty("Включить рандомное время?")]
                public bool UseRandomTimer { get; set; }
                [JsonProperty("Минимальный интервал (Минуты)")]
                public int RandomTimerMin { get; set; }
                [JsonProperty("Максимальный интервал (Минуты)")]
                public int RandomTimerMax { get; set; }
            }
            [JsonProperty("Настройка ивента")]
            public Intensity Settings { get; set; }
            public class Intensity
            {
                [JsonProperty("Шанс распространения огня от малого метеорита")]
                public int FireRocketChance { get; set; }
                [JsonProperty("Радиус на котором проходит метеоритопад")]
                public float Radius { get; set; }
                [JsonProperty("Количество падающих метеоритов (малых)")]
                public int RocketAmount { get; set; }
                [JsonProperty("Длительность падения малых метеоритов")]
                public int Duration { get; set; }
                [JsonProperty("Множитель урона от попадания по Enemy")]
                public float DamageMultiplier { get; set; }
                [JsonProperty("Настройка выпадающих ресурсов после попадания метеорита по земле")]
                public Drop ItemDropControl { get; set; }
                public class Drop
                {
                    [JsonProperty("Включить дроп ресурсов после метеорита?")]
                    public bool EnableItemDrop { get; set; }
                    [JsonProperty("Настройка выпадаемых ресурсов")]
                    public ItemDrops[] ItemsToDrop { get; set; }
                }
                [JsonProperty("Количество NPC возле главного метеорита")]
                public int NpcAmount { get; set; }
                [JsonProperty("Включить спавн NPC возле метеорита?")]
                public bool NpcSpawn { get; set; }
                [JsonProperty("Время которое будет остывать метеорит (Минуты)")]
                public float FireTime { get; set; }
                [JsonProperty("Время через которое метеорит изчезнет после остывания (Минуты)")]
                public float DespawnTime { get; set; }
            }

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
                    },
                    NotifyEvent = true,
                    Earthquake = true, 
                },
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
                },
                _Radiations = new PluginConfig.Radiations
                {
                    EnableRadZone = true,
                    Radius = 10f,
                    Strange = 5f,
                },
                Settings = new PluginConfig.Intensity
                {

                    Duration = 180,
                    FireRocketChance = 20,
                    Radius = 50,
                    RocketAmount = 90,
                    DamageMultiplier = 0.4f,
                    NpcAmount = 5,
                    NpcSpawn = true,
                    FireTime = 5,
                    DespawnTime = 10,
                    ItemDropControl = new PluginConfig.Intensity.Drop
                    {
                        EnableItemDrop = true,
                        ItemsToDrop = new ItemDrops[]
                            {
                             new ItemDrops
                             {
                                Maximum = 150,
                                Minimum = 80,
                                Shortname = "stones"
                             },
                             new ItemDrops
                             {
                                Maximum = 100,
                                Minimum = 50,
                                Shortname = "metal.ore"
                             },
                             new ItemDrops
                             {
                                Maximum = 90,
                                Minimum = 40,
                                Shortname = "sulfur"
                             },
                            }
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
        #endregion

        #region Oxide Hoock
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
            if (objects != null)
                foreach (var gameObj in objects)
                    UnityEngine.Object.Destroy(gameObj);
            Despawn();
            if (EventTimer != null)
                EventTimer.Destroy();
            foreach (var check in radiationZone)
                UnityEngine.Object.Destroy(check);
            radiationZone.Clear();
            if (!rads) ConVar.Server.radiation = false;

            if (mapMarker != null && !mapMarker.IsDestroyed)
                mapMarker.Kill();

            if (MarkerT != null && !MarkerT.IsDestroyed)
                MarkerT.Kill();
        }
        #endregion

        #region FireMeteor 
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
                if (carriedItems == null)
                    return;

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
            if (rockettimer != null)
                rockettimer?.Destroy();
            if (AlertTimer != null)
                AlertTimer?.Destroy();
            if (EventTimer != null)
                EventTimer?.Destroy();
            if (mystimer != null)
                mystimer?.Destroy();
            if (mystimer1 != null)
                mystimer1?.Destroy();
            if (mystimer2 != null)
                mystimer2?.Destroy();
            if (mystimer3 != null)
                mystimer3?.Destroy();
            if (mystimer4 != null)
                mystimer4?.Destroy();
            if (mystimer5 != null)
                mystimer5?.Destroy();
        }

        private void StartRandomOnMap()
        {
            if (config._Options.NotifyEvent)
            {
                foreach (var pl in BasePlayer.activePlayerList)
                    SendToChat(pl, GetMsg("incoming", pl.userID));
            }
            mystimer2 = timer.In(10f, () => CollisionAtm());
        }

        private Timer rockettimer, effecttimer, timerfireball;

        List<uint> MeteorList = new List<uint>();
        List<uint> MeteorFound = new List<uint>();
        List<uint> NpcList = new List<uint>();
        List<uint> FireBall = new List<uint>();

        public List<string> prefabOsn = new List<string>()
        {
            "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab",
            "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab",
            "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab"
        };

        Quaternion qTo;
        BaseEntity met;
        private Timer mystimer, mystimer3, mystimer4, mystimer5;
        private Timer mystimer1;
        private Timer mystimer2;
        void CollisionAtm()
        {
            if (BasePlayer.activePlayerList.Count < config._Options.MinPlayer)
            {
                foreach (var pl in BasePlayer.activePlayerList)
                    SendToChat(pl, GetMsg("EventCancel", pl.userID));
                StartEventTimer();
                return;
            }
            if (!ConVar.Server.radiation)
            {
                rads = false;
                if (config._Options.OffStandartRad)
                    OnServerRadiation();
                ConVar.Server.radiation = true;
            }
            else rads = true;
            mystimer2 = timer.Once(10f, () =>
            {
                if (config._Options.NotifyEvent)
                    foreach (var pl in BasePlayer.activePlayerList)
                        SendToChat(pl, GetMsg("InAtm", pl.userID));
            });
            var callAt = GetEventPosition();
            int coldown = 20;
            mystimer1 = mystimer = timer.Repeat(1.1f, coldown, () =>
            {
                if (coldown == 1)
                {
                    timer.In(1f, () => StartRainOfFire(callAt));
                }
                if (coldown <= 5 && coldown != 0)
                {
                    if (config._Options.NotifyEvent)
                        foreach (var pl in BasePlayer.activePlayerList)
                            SendToChat(pl, GetMsg("Dropped", pl.userID).Replace("{cooldown}", $"{coldown}"));
                }
                if (coldown != 0)
                    coldown--;
            });
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
            string pref = null;

            for (int i = 0; i < 5; i++)
            {
                qTo = Quaternion.Euler(new Vector3(UnityEngine.Random.Range(-30, 30), UnityEngine.Random.Range(-150, -50), 0));
                if (i == 0)
                {
                    pref = prefabOsn[rnd.Next(prefabOsn.Count)];
                    met = GameManager.server.CreateEntity(pref, location, qTo, true);
                    EcoMap?.Call("AddMapMarker", "meteor", false, 0.0300f, met.transform, "Метеорит");
                    RustMap?.Call("AddTemporaryMarker", "meteor", false, 0.05f, 0.95f, met.transform, "Метеорит");
                    LustyMap?.Call("AddMarker", location.x, location.z, "Метеорит", "https://i.imgur.com/BFCdsOx.png");
                    Map?.Call("ApiAddPointUrl", "https://i.imgur.com/BFCdsOx.png", "Метеорит", met.transform.position);
                    CreatePrivateMap(met.transform.position);
                    met.Spawn();
                    MeteorList.Add(met.net.ID);
                    MeteorFound.Add(met.net.ID);
                }
                else
                {
                    posy = i - (i - UnityEngine.Random.Range(0.3f, 0.6f));
                    posx = i - (i - UnityEngine.Random.Range(-1f, +1f));
                    posz = i - (i - UnityEngine.Random.Range(-1f, +1f));
                    pref = prefabOsn[rnd.Next(prefabOsn.Count)];
                    location.y = location.y + posy;
                    location.x = location.x + posx;
                    met = GameManager.server.CreateEntity(pref, (Vector3)location, qTo, true);
                    met.Spawn();
                    MeteorList.Add(met.net.ID);
                }
            }
            AlertTimer = timer.In(10f, () =>
            {
                if (config._Options.NotifyEvent)
                {
                    foreach (var pl in BasePlayer.activePlayerList)
                        SendToChat(pl, GetMsg("colding", pl.userID).Replace("{time}", $"{FormatTime(TimeSpan.FromSeconds(config.Settings.FireTime * 60))}"));
                }
            });
            mystimer3 = timer.In(20f, () =>
            {
                if (config._Options.NotifyEvent)
                {
                    foreach (var pl in BasePlayer.activePlayerList)
                        SendToChat(pl, GetMsg("Warning", pl.userID).Replace("{radius}", $"{config.Settings.Radius}"));
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
                        DrawUI(player, $"Метеорит остынет через: {FormatTime(TimeSpan.FromSeconds(cooldown2))}");
                if (cooldown2 != 0)
                    cooldown2--;
            });

            CreateFireBall(location, new Vector3(0, 2, 0));
            CreateFireBall(location, new Vector3(2, 0, 0));
            CreateFireBall(location, new Vector3(-2, 0, 0));
            CreateFireBall(location, new Vector3(0, 0, 2));
            CreateFireBall(location, new Vector3(0, 0, -2));
            timerfireball = timer.Once(120, () =>
            {
                CreateFireBall(location, new Vector3(0, 2, 0));
                CreateFireBall(location, new Vector3(2, 0, 0));
                CreateFireBall(location, new Vector3(-2, 0, 0));
                CreateFireBall(location, new Vector3(0, 0, 2));
                CreateFireBall(location, new Vector3(0, 0, -2));
            });
            Effect.server.Run("assets/prefabs/npc/m2bradley/effects/bradley_explosion.prefab", location);
            Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab", location);
            timer.Once(1f, () =>
            {
                Effect.server.Run("assets/prefabs/npc/m2bradley/effects/bradley_explosion.prefab", location);
                Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab", location);
            });
            if (config._Options.Earthquake)
                foreach (var pl in BasePlayer.activePlayerList)
                    Screen(pl);

            if (config._Radiations.EnableRadZone)
                CreateZone(location);
            if (config.Settings.NpcSpawn)
                CreateNps(location, config.Settings.NpcAmount);

        }

        private MapMarkerGenericRadius mapMarker;
        private VendingMachineMapMarker MarkerT;

        private UnityEngine.Color ConvertToColor(string color)
        {
            if (color.StartsWith("#"))
                color = color.Substring(1);
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
            mapMarker.radius = 4f;
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
            timerfireball.Destroy();
            foreach (var check in FireBall)
            {
                if (BaseNetworkable.serverEntities.Find(check) != null && !BaseNetworkable.serverEntities.Find(check).IsDestroyed)
                    BaseNetworkable.serverEntities.Find(check).Kill();
            }
            FireBall.Clear();

            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, "CuiElementMt");

            if (config._Options.NotifyEvent)
            {
                foreach (var pl in BasePlayer.activePlayerList)
                    SendToChat(pl, GetMsg("coldingFinish", pl.userID));
            }

            AlertTimer = timer.Once(60f, () =>
            {
                if (config._Options.NotifyEvent)
                {
                    foreach (var pl in BasePlayer.activePlayerList)
                        SendToChat(pl, GetMsg("Despawn", pl.userID).Replace("{time}", $"{FormatTime(TimeSpan.FromSeconds(config.Settings.DespawnTime * 60))}"));
                }
            });
            int desptime = Convert.ToInt32((config.Settings.DespawnTime * 60) + 60f);
            mystimer5 = timer.Repeat(1f, desptime, () =>
            {
                if (desptime == 1)
                {
                    foreach (var player in BasePlayer.activePlayerList)
                        CuiHelper.DestroyUi(player, "CuiElementMt");
                    Despawn();
                }
                if (1 < desptime && desptime <= config.Settings.DespawnTime * 60)
                {
                    foreach (var player in BasePlayer.activePlayerList)
                        DrawUI(player, $"Метеорит исчезнет через: {FormatTime(TimeSpan.FromSeconds(desptime))}");
                }
                if (desptime != 0)
                    desptime--;
            });
        }
        private void CreateNps(Vector3 position, int amount)
        {
            for (int i = 0; i < amount; i++)
                GameManager.server.CreateEntity("assets/prefabs/npc/scientist/scientist.prefab", RandomCircle(position, 10)).Spawn();
        }

        private void CreateFireBall(Vector3 position, Vector3 additional)
        {
            BaseEntity x = null;
            x = GameManager.server.CreateEntity("assets/bundled/prefabs/oilfireballsmall.prefab", position + additional);
            x.Spawn();

            FireBall.Add(x.net.ID);
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is NPCPlayer)
                NpcList.Add(entity.net.ID);
        }

        private void Despawn()
        {
            foreach (var check in MeteorList)
            {
                if (BaseNetworkable.serverEntities.Find(check) != null && !BaseNetworkable.serverEntities.Find(check).IsDestroyed)
                    BaseNetworkable.serverEntities.Find(check).Kill();
            }

            foreach (var check in NpcList)
            {
                if (BaseNetworkable.serverEntities.Find(check) != null && !BaseNetworkable.serverEntities.Find(check).IsDestroyed)
                    BaseNetworkable.serverEntities.Find(check).Kill();
            }

            foreach (var check in FireBall)
            {
                if (BaseNetworkable.serverEntities.Find(check) != null && !BaseNetworkable.serverEntities.Find(check).IsDestroyed)
                    BaseNetworkable.serverEntities.Find(check).Kill();
            }

            foreach (var check in radiationZone)
                UnityEngine.Object.Destroy(check);
            radiationZone.Clear();
            MeteorList.Clear();
            FireBall.Clear();
            NpcList.Clear();
            StopTimer();
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, "CuiElementMt");
            EcoMap?.Call("RemoveMapMarker", "Метеорит");
            RustMap?.Call("RemoveTemporaryMarkerByName", "Метеорит");
            LustyMap?.Call("RemoveMarker", "Метеорит");
            Map?.Call("ApiRemovePointUrl", "https://i.imgur.com/BFCdsOx.png", "Метеорит", met.transform.position);
            if (mapMarker != null && !mapMarker.IsDestroyed)
                mapMarker.Kill();
            if (MarkerT != null && !MarkerT.IsDestroyed)
                MarkerT.Kill();
            timer.In(10f, () => StartEventTimer());
        }

        private void RandomRocket(Vector3 origin, float radius)
        {
            bool isFireRocket = false;
            Vector2 rand = UnityEngine.Random.insideUnitCircle;
            Vector3 offset = new Vector3(rand.x * radius, 0, rand.y * radius);
            Random rnd = new Random();
            Vector3 direction = (Vector3.up * -launchStraightness + Vector3.right).normalized;
            Vector3 launchPos = origin + offset - direction * launchHeight;
            if (UnityEngine.Random.Range(1, config.Settings.FireRocketChance) == 1)
                isFireRocket = true;
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

            if (isFireRocket)
                projectileItem = GetFireRocket();
            else
                projectileItem = GetRocket();

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
            for (int i = 0; i < damageTypes.Count; i++)
                damageTypes[i].amount *= scale;
        }

        private ItemDefinition GetRocket() => ItemManager.FindItemDefinition("ammo.rocket.hv");
        private ItemDefinition GetFireRocket() => ItemManager.FindItemDefinition("ammo.rocket.fire");
        #endregion

        #region Command
        [ConsoleCommand("meteor")]
        void ConsoleStart(ConsoleSystem.Arg args)
        {
            if (args.Connection != null)
                if (!args.IsConnectionAdmin) return;
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
        void ChatCmdControll(BasePlayer player, string command, string[] args)
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
                    if (EventTimer != null)
                        EventTimer.Destroy();

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
        #endregion

        #region UI
        void OnEntityKill(BaseNetworkable entity)
        {
            if (MeteorFound.Contains(entity.net.ID))
            {
                if (MeteorFound.Count == 1)
                {
                    StopTimer();
                    foreach (var player in BasePlayer.activePlayerList)
                        CuiHelper.DestroyUi(player, "CuiElementMt");
                    EcoMap?.Call("RemoveMapMarker", "Метеорит");
                    RustMap?.Call("RemoveTemporaryMarkerByName", "Метеорит");
                    LustyMap?.Call("RemoveMarker", "Метеорит");
                    Map?.Call("ApiRemovePointUrl", "https://i.imgur.com/BFCdsOx.png", "Метеорит", met.transform.position);
                    if (mapMarker != null && !mapMarker.IsDestroyed)
                        mapMarker.Kill();
                    if (MarkerT != null && !MarkerT.IsDestroyed)
                        MarkerT.Kill();
                    MeteorFound.Remove(entity.net.ID);
                }
                MeteorFound.Remove(entity.net.ID);
            }
        }

        void DrawUI(BasePlayer player, string msg)
        {
            if (!config.uiSettings.useUi) return;
            if (!string.IsNullOrEmpty(config.uiSettings.uiTransform.AnchorXMax) && !string.IsNullOrEmpty(config.uiSettings.uiTransform.AnchorYMax) && !string.IsNullOrEmpty(config.uiSettings.uiTransform.AnchorXMin) && !string.IsNullOrEmpty(config.uiSettings.uiTransform.AnchorYMin))
            {
                var reply = 671;
                if (reply == 0) { }
                string anchormax = $"{config.uiSettings.uiTransform.AnchorXMax} {config.uiSettings.uiTransform.AnchorYMax}";
                string anchormin = $"{config.uiSettings.uiTransform.AnchorXMin} {config.uiSettings.uiTransform.AnchorYMin}";
                CuiHelper.DestroyUi(player, "CuiElementMt");
                CuiHelper.AddUi(player, ui
                    .Replace("{anchormax}", anchormax)
                    .Replace("{anchormin}", anchormin)
                      .Replace("{Icon}", Images)
                      .Replace("{Text}", msg));
            }
            else
            { 
                string anchormax = "0.6414062 0.2083333";
                string anchormin = "0.34375 0.1097223";
                CuiHelper.DestroyUi(player, "CuiElementMt");
                CuiHelper.AddUi(player, ui
                    .Replace("{anchormax}", anchormax)
                    .Replace("{anchormin}", anchormin)
                      .Replace("{Icon}", Images)
                      .Replace("{Text}", msg));
            }
        }

        string ui = "[{\"name\":\"CuiElementMt\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"0 0 0 0.2394277\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\"},{\"type\":\"RectTransform\",\"anchormin\":\"{anchormin}\",\"anchormax\":\"{anchormax}\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"CuiElementMt\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"0 0 0 0.434163\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.007874042 0.09859085\",\"anchormax\":\"0.1758531 0.915493\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"CuiElementMt\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"png\":\"{Icon}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.01049873 0.07042181\",\"anchormax\":\"0.1758531 0.915493\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"CuiElementMt\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"color\":\"0 0 0 0.3316553\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2020998 0.1236111\",\"anchormax\":\"0.9658796 0.8450704\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"CuiElementMt\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{Text}\",\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0.7238988 0.3568805 0.1592568 1\",\"distance\":\"1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2047244 0.1236111\",\"anchormax\":\"0.9580055 0.8309858\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";

        #endregion

        #region LoadImages
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
                files[name] = new FileInfo() { Url = url };
                needed++;
                yield return StartCoroutine(LoadImageCoroutine(name, url, size));
                ins.Puts($"{name} {url}");

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
        #endregion
        #region Rad
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

            public void InitializeRadiationZone(Vector3 position, float radius, float amount)
            {
                this.radius = radius;
                this.amount = amount;

                transform.position = position;
                transform.rotation = new Quaternion();
                UpdateCollider();

                rads = gameObject.AddComponent<TriggerRadiation>();
                rads.RadiationAmountOverride = amount;
                rads.radiationSize = radius;
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
        #endregion

        #region spawn
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

        #region Chat
        private void SendToChat(BasePlayer Player, string Message)
        {
            PrintToChat(Player, Message);
        }

        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID?.ToString());
        #endregion

        #region localization 
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
                ["CmdEventCurrent"] = "The MeteorFall event is already running, use: / meteor stop"
            }, this);
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
                ["CmdEventCurrent"] = "Ивент MeteorFall уже запущен, используйте: /meteor stop"
            }, this, "ru");
        }
        #endregion

        #region Time
        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "дней", "дня", "день")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "часов", "часа", "час")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";

            if (time.Seconds != 0)
                result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";

            return result;
        }
        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }
        #endregion 
    }
}                                                                                                    
