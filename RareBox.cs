using System;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using System.Linq;
using Rust;
using Facepunch;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("RareBox", "Sparkless", "0.1.1")]
    public class RareBox : RustPlugin
    {

        #region Map

        void RemoveMap()
        {
            Map?.Call("ApiRemovePointUrl",_config.Icons, "Ящик с ресурсами!", BoxLoot?.transform.position);
            RustMap?.Call("RemoveTemporaryMarkerByName", _config.Icons, "Ящик с ресурсами!");
            PrintWarning($"Метка на карте удалена!");
        }
        void AddMap()
        {
            RustMap?.Call("AddTemporaryMarker",_config.Icons, false, 0.05f, 0.95f, BoxLoot?.transform.position, "Ящик с ресурсами!");
            Map?.Call("ApiAddPointUrl", _config.Icons, "Ящик с ресурсами!", BoxLoot?.transform.position);
            PrintWarning($"Метка на карте установлена!");
        }

            #endregion

        #region Random
        
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
        #endregion
        
        #endregion

        [PluginReference] private Plugin RustMap;
        [PluginReference] private Plugin Map;
        public List<BaseEntity> BaseEntityList = new List<BaseEntity>();
        private ConfigData _config;
        public Timer mytimer;
        public Timer mytimer2;
        public Timer mytimer3;
        private BaseEntity BoxLoot;
        public bool CanLoot = false;



        public class Itemss
        {
            [JsonProperty("Предмет из игры(shortname)")] public string ShortName;
            [JsonProperty("мин кол-во предмета")] public int MinDrop;
            [JsonProperty("макс кол-во предмета")] public int MaxDrop;
            [JsonProperty("Шанс добавление предмета(0 - отключить выпадение)")] public int Chance;
        }

        class ConfigData
        {
            [JsonProperty("Иконка на карте")]
            public string Icons = "https://i.imgur.com/C0n44en.png";
            [JsonProperty("Каждое n секунд будет запускаться ивент!")]
            public int CheckTimeForStart = 15000;

            [JsonProperty("Пермишенс для команды /rarebox")]
            public string CheckPermission = "RareBoxCommands.use";

            [JsonProperty("Сколько будет надо будет времени подождать, дабы открыть сундук?(в секундах)")]
            public int CheckTime = 5;

            [JsonProperty("Через сколько секунд после открытия ящика он удалится(в секундах)")]
            public int CheckTimeForRemove = 60;
            
            [JsonProperty("skinID на ящик!(0 - дефолт)")]
            public ulong skinID = 1766238308;
            
            [JsonProperty("Размер иконки на игровой карте")]
            public float Radius = 5f;

            [JsonProperty("Сколько ресурсов ложить в ящик")]
            public int capacity = 4;

            [JsonProperty("Вещи, которые могут попаться именно в ящике")]
            public List<Itemss> ListDrop { get; set; }

            public static ConfigData GetNewCong()
            {
                ConfigData newConfig = new ConfigData();

                newConfig.ListDrop = new List<Itemss>
                {
                    new Itemss()
                    {
                        ShortName = "rifle.ak",
                        MinDrop = 1,
                        MaxDrop = 2,
                        Chance = 50,
                    },
                    new Itemss()
                    {
                        ShortName = "wood",
                        MinDrop = 1,
                        MaxDrop = 2,
                        Chance = 50,
                    }
                };
                return newConfig;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<ConfigData>();
                if (_config?.ListDrop == null) LoadDefaultConfig();

            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => _config = ConfigData.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(_config);
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        void OnServerInitialized()
        {
            permission.RegisterPermission(_config.CheckPermission, this);
            StartEvents();
        }

        void StartEvents()
        {
            mytimer = timer.Once(_config.CheckTimeForStart,  () =>
            {
                if (BaseEntityList.Count > 0)
                {
                    DestroyTownLoot();
                }
                var callAt = GetEventPosition();
                CreateRareTownLoot(callAt);
            });
        }
        
        public Dictionary<ulong, double> Time = new Dictionary<ulong, double>();
        void CreateRareTownLoot(Vector3 vector)
        {
            var location = vector;
            location.y = GetGroundPosition(location);
            BaseEntity Box = GameManager.server.CreateEntity("assets/prefabs/deployable/large wood storage/box.wooden.large.prefab", location);
            Box.skinID = _config.skinID;
            Box.OwnerID = 9596;
            BoxLoot = Box;
            Box.Spawn();
            StorageContainer container = Box.GetComponent<StorageContainer>();
            AddLoot(container, Box);
            BaseEntityList.Add(Box);
            Box.SendNetworkUpdate();
            CanLoot = false;
                Server.Broadcast($"<color=#edb8b8><size=20>На карте появился сундук с редкими ресурсами, координаты {location}, так же метка где находится ящик отмечена на карте!</size></color>");
            AddMap();
            SpawnMapMarkers();
            Time.Add(1337, CurrentTime() + _config.CheckTime);
            mytimer2 = timer.Once(_config.CheckTime, () =>
            {
                TownLootIsOpen();
            });
        }
        void TownLootIsOpen()
        {
            CanLoot = true;
            Server.Broadcast($"<color=#efb5a5><size=15>Ящик с редкими ресурсами открылся!, бегом к нему, через {_config.CheckTimeForRemove} секунд он удалится!</size></color>");
            mytimer3 = timer.Once(_config.CheckTimeForRemove, () =>
            {
                StartEvents();
                Server.Broadcast($"<color=#b88a8a><size=15>Ящик с редкими ресурсами был удален</size></color>");
                DestroyTownLoot();
            });
        }
        void DestroyTownLoot()
        {
            if (BaseEntityList != null)
            {
                mapmarker?.Kill();
                MarkerName?.Kill();
                RemoveMap();
                foreach (BaseEntity entity in BaseEntityList)
                {
                    NextTick(() =>
                    {
                        entity.Kill(); 
                    });
                }
                if (mytimer2 != null) timer.Destroy(ref mytimer2);
                if (mytimer3 != null) timer.Destroy(ref mytimer3);
                BaseEntityList?.Clear();
                Time?.Clear();
            }
        }
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (BaseEntityList != null)
                {
                    foreach (BaseEntity entityIn in BaseEntityList)
                    {
                        if (entityIn.net.ID == entity.net.ID)
                        {
                            return false;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
            return null;
        }
        object CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container == null || player.IsNpc || player == null) return null;

            if (container.OwnerID == 9596)
            {
                if (!CanLoot)
                {
                    var check = Time[1337] - CurrentTime();
                    var timecheck = TimeSpan.FromSeconds(check).ToShortString();
                    SendReply(player, "<color=#7b49d1>Вы не можете открыть ящик, так как он заблокирован!</color>" + $"\n<color=#7b49d1> Подождите</color> <color=#efacac>{timecheck}</color>");
                    return false;
                }
            }
            return null;
        }
        void Unload()
        {
            if (mytimer != null) timer.Destroy(ref mytimer);
            if (mytimer2 != null) timer.Destroy(ref mytimer2);
            if (mytimer3 != null) timer.Destroy(ref mytimer3);
            if (BaseEntityList != null) DestroyTownLoot();
        }
        [ChatCommand("rarebox")]
        void CmdStartTownLoot(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, _config.CheckPermission)) return;
            if (args.Length == 0)
            {
                SendReply(player, "/rarebox start - запустить ивент!\n/rarebox cancel - отменить ивент!");
            }
            switch (args[0])
            {
                case "start":
                    if (BaseEntityList.Count > 0)
                    {
                        SendReply(player, "Ивент уже запущен!");
                        return;
                    }
                    var callAt = GetEventPosition();
                    CreateRareTownLoot(callAt);
                    SendReply(player, "Вы успешно запустили ивент!");
                    break;
                case "cancel":
                {
                    if (BaseEntityList.Count == 0)
                    {
                        SendReply(player, "Ивент не запущен!");
                        return;
                    }
                    SendReply(player, "Вы остановили ивент!");
                    DestroyTownLoot();
                    break;
                }
            }
        }
        void AddLoot(StorageContainer container, BaseEntity Box)
        {
            ItemContainer inventorContainer = container.inventory;
            List<Itemss> Listing = new List<Itemss>();

                if (container != null)
                {
                    for (int i = 0; i < _config.ListDrop.Count; i++)
                    {
                        var it = _config.ListDrop.GetRandom();
                        if (UnityEngine.Random.Range(1, 100) < it.Chance && !Listing.Contains(it))
                        {
                            Listing.Add(it);
                        }
                        else
                        {
                            i--;
                        }
                        if (Listing.Count == _config.capacity)
                        {
                            foreach (var key in Listing)
                            {
                                var item = ItemManager.CreateByName(key.ShortName,
                                    UnityEngine.Random.Range(key.MinDrop, key.MaxDrop));
                                item.MoveToContainer(container.inventory);     
                            }
                        }
                    }
                }
            }
        #region Map

        MapMarkerGenericRadius mapmarker; 
        VendingMachineMapMarker MarkerName; 
        public void SpawnMapMarkers() 
        { 
            MarkerName = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", BoxLoot.transform.position, Quaternion.identity, true) as VendingMachineMapMarker; 
            MarkerName.markerShopName = "RARE BOX"; 
            MarkerName.Spawn(); 
            mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", BoxLoot.transform.position, Quaternion.identity, true) as MapMarkerGenericRadius; 
            mapmarker.Spawn();
            mapmarker.radius = _config.Radius;
            mapmarker.color1 = new Color(1f, 0.56f, 0.21f);
            mapmarker.color2 = new Color(1f, 0.56f, 0.21f);
            mapmarker.SendUpdate();
        }
        #endregion
    }
}