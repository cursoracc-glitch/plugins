﻿using System.Collections.Generic;
using Facepunch.Extend;
using Newtonsoft.Json;
 using Oxide.Core.Plugins;
 using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DropCars", "TopPlugin.ru", "1.0.4")]
    public class DropCars : RustPlugin
    {
        #region CFG
        private ConfigData cfg { get; set; }

        private class ConfigData
        {
            [JsonProperty("Список флаеров призыва")] public List<FlareList> listFlare = new List<FlareList>();
            [JsonProperty("Высота полёта")] public float height = 150;
            [JsonProperty("Скорость самолета")] public float speed = 250;
            [JsonProperty("Заменить дефолтный вызов на вызов с самолета(Будут работать ток те которые есть во флаерах)")] public bool defaultSpawn = false; 
            public static ConfigData GetNewConf() 
            {
                var newConfig = new ConfigData();
                newConfig.listFlare = new List<FlareList>()
                {
                    new FlareList()
                    {
                        SkinId = 2112250209,
                        DisplayName = "Вызов миникоптера",
                        PrefabName = "assets/content/vehicles/minicopter/minicopter.entity.prefab",
                        _crateList = new Dictionary<string, float>()
                        {
                            ["crate_normal"] = 30,
                            ["crate_elite"] = 70,
                        }
                    },
                    new FlareList()
                    {
                        SkinId = 2112252048,
                        DisplayName = "Вызов большого коптера",
                        PrefabName = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab",
                        _crateList = new Dictionary<string, float>()
                        {
                            ["crate_normal"] = 30,
                            ["crate_elite"] = 70,
                        }
                    },
                    new FlareList()
                    {
                        SkinId = 2112251924,
                        DisplayName = "Вызов rhib",
                        PrefabName = "assets/content/vehicles/boats/rhib/rhib.prefab",
                        _crateList = new Dictionary<string, float>()
                        {
                            ["crate_normal"] = 30,
                            ["crate_elite"] = 70,
                        }
                    }
                };
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
        #endregion
        #region Command
        [ConsoleCommand("give.flare")]
        void ConsCommad(ConsoleSystem.Arg arg)
        {
            if(arg?.Args == null || arg.Args.Length < 3) return;
            if(arg.Player() != null && !arg.Player().IsAdmin) return;
            var player = BasePlayer.FindByID(ulong.Parse(arg.Args[0]));
            if(player == null) return;
            var findPrefab = cfg.listFlare.FindLast(p => p.SkinId == ulong.Parse(arg.Args[1]));
            if(findPrefab == null) return;
            var item = ItemManager.CreateByName("flare", arg.Args[2].ToInt(), findPrefab.SkinId);
            item.name = findPrefab.DisplayName;
            if (!player.inventory.GiveItem(item))
                item.Drop(player.inventory.containerMain.dropPosition, player.inventory.containerMain.dropVelocity);
        }

        #endregion
        #region Class
        class FlareList
        {
            [JsonProperty("СкинАйди")]
            public ulong SkinId = 0;
            [JsonProperty("Название в инвентаре")]
            public string DisplayName= "";
            [JsonProperty("Префаб")]
            public string PrefabName = "";
            [JsonProperty("Ящики в который будет появляться(Ящик и шанс)")]
            public Dictionary<string, float> _crateList = new Dictionary<string, float>();
            [JsonProperty("Минимум выпадает")]
            public int Min = 1;
            [JsonProperty("Максимум выпадает")]
            public int Max = 2;
        }
        public static DropCars ins;
        #endregion
        #region Hooks
        void Init()
        {
            ins = this;
        } 
        [PluginReference] private Plugin CustomSkinsStacksFix;
        private Item OnItemSplit(Item item, int amount)
        {
            if (CustomSkinsStacksFix != null) return null;
            if (amount <= 0) return null;
            if (cfg.listFlare.Find(p => p.SkinId == item.skin) == null) return null;
            item.amount -= amount;
            var newItem = ItemManager.Create(item.info, amount, item.skin);
            newItem.name = item.name;
            newItem.skin = item.skin;
            newItem.amount = amount;
            return newItem;
        } 
        private object CanCombineDroppedItem(WorldItem first, WorldItem second)
        {
            return CanStackItem(first.item, second.item);
        } 
        object CanStackItem(Item item, Item targetItem)
        {
            var findSkin = cfg.listFlare.Find(p => p.SkinId == item.skin);
            if (findSkin == null) return null;
            if (item.skin == findSkin.SkinId && targetItem.skin == findSkin.SkinId) return true;
            return null;
        }
        object OnLootSpawn(LootContainer container)
        {
            if (container == null) return null;
            NextTick((() =>
            {
                var f = cfg.listFlare.FindAll(p => p._crateList.ContainsKey(container.ShortPrefabName));
                foreach (var flareList in f)
                {
                    if(flareList._crateList[container.ShortPrefabName] < Core.Random.Range(0f, 100f)) return;
                    container.GetComponent<StorageContainer>().inventory.capacity += 1;
                    var item = ItemManager.CreateByName("flare", Random.Range(flareList.Min, flareList.Max), flareList.SkinId);
                    item.name = flareList.DisplayName;
                    ItemContainer component1 = container.GetComponent<StorageContainer>().inventory;
                    item.MoveToContainer(component1);
                }
            }));
            
            return null;
        }
        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.cmd.Name != "spawn" || !cfg.defaultSpawn) return null;
            var findPrefab = cfg.listFlare.FindLast(p => p.PrefabName.Contains(arg.Args[0]));
            if(findPrefab == null) return null;
            GameObject entity  = new GameObject();
            entity.AddComponent<BaseEntity>();
            entity.transform.position = arg.Player().transform.position;
            entity.gameObject.AddComponent<DropCar>().PrefabName = findPrefab.PrefabName;
            return false;  
        }
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity, ThrownWeapon item)
        {
            if(player == null || entity == null || item == null) return;
            var findPrefab = cfg.listFlare.FindLast(p => p.SkinId == item.skinID);
            if(findPrefab == null) return;
            entity.gameObject.AddComponent<DropCar>().PrefabName = findPrefab.PrefabName;
        }

        void OnExplosiveDropped(BasePlayer player, BaseEntity entity, ThrownWeapon item) =>
            OnExplosiveThrown(player, entity, item);
        #endregion
        #region ElseClass 
        
        class Parachute : BaseEntity
        {   
            private BaseEntity _entity;
            private BaseEntity parachute;

            private void Awake() 
            {
                _entity = GetComponent<BaseEntity>();
                parachute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", new Vector3(), new Quaternion(), true);
                parachute.SetParent(_entity, "parachute_attach");
                parachute.Spawn();
                var bodyRHIB = _entity.GetComponent<RHIB>();
                var bodyRow = _entity.GetComponent<MotorRowboat>();
                if (bodyRHIB != null) bodyRHIB.landDrag = 1.5f;
                else if (bodyRow != null) bodyRow.landDrag = 1.5f;
                else _entity.GetComponent<Rigidbody>().drag = 1.5f;

                var collider = parachute.gameObject.AddComponent<SphereCollider>();
                collider.gameObject.layer = (int) Layer.Reserved1;
                collider.radius = 0.25f;
                collider.isTrigger = true; 
            } 

            public void RemoveParachute() 
            {
                if (!parachute)
                    return;
                parachute.Kill();
                parachute =null;
                Destroy(this);
                return;
            }

            private void OnTriggerEnter(Collider other)
            {
                if (other.gameObject.GetComponent<Terrain>() == null&& !other.gameObject.name.Contains("building core") && !other.name.Contains("rock_cliff") && other.gameObject.GetComponent<BaseEntity>() == null && other.gameObject.GetComponent<BuildingPrivlidge>() == null && !other.gameObject.name.Contains("flare")) return;
                var bodyRHIB = _entity.GetComponent<RHIB>();
                var bodyRow = _entity.GetComponent<MotorRowboat>();
                if (bodyRHIB != null)
                {
                    bodyRHIB.landDrag = 0.2f; 
                }
                else if (bodyRow != null)
                {
                    bodyRow.landDrag = 0.2f;
                }
                else 
                {
                    _entity.GetComponent<Rigidbody>().drag = 0.3f;   
                }
                RemoveParachute();
            } 
        }
        class DropCar : MonoBehaviour
        {
            public string PrefabName;
            private CargoPlane _cargoPlane;
            private BaseEntity parachute;
            private BaseEntity _entity;
            private void Awake() 
            {
                _entity = GetComponent<BaseEntity>();
                if(_entity.ShortPrefabName !=null && _entity.ShortPrefabName.Contains("flare")) _entity.GetComponent<Rigidbody>().freezeRotation = true;
                _cargoPlane = GameManager.server.CreateEntity("assets/prefabs/npc/cargo plane/cargo_plane.prefab", Vector3.up) as CargoPlane;
                _cargoPlane.Spawn();
                _cargoPlane.UpdateDropPosition(_entity.transform.position);
                _cargoPlane.startPos.y = TerrainMeta.HighestPoint.y + ins.cfg.height;
                _cargoPlane.endPos.y = _cargoPlane.startPos.y;
                _cargoPlane.secondsToTake = Vector3.Distance(_cargoPlane.startPos, _cargoPlane.endPos) / (int) ins.cfg.speed;
                _cargoPlane.SendNetworkUpdateImmediate(true);
                _cargoPlane.dropped = true;
            }

            private void OnDestroy()
            {
                if(_entity == null)_cargoPlane.Kill();
                Destroy(this);
            }

            private void Update()
            {
                if(_entity != null) _cargoPlane.dropPosition = _entity.transform.position;
                float t = Mathf.InverseLerp(0.0f, _cargoPlane.secondsToTake, _cargoPlane.secondsTaken);
                
                if (_cargoPlane.dropped && (double) t >= 0.5)
                {
                    BaseEntity entity = GameManager.server.CreateEntity(PrefabName, new Vector3(_cargoPlane.dropPosition.x, _cargoPlane.startPos.y, _cargoPlane.dropPosition.z), new Quaternion(), true);
                    if (entity)
                    {
                        entity.Spawn();
                        entity.gameObject.AddComponent<Parachute>();
                    }
                    Destroy(this);
                }
            }
        }

        #endregion 
    }
}
