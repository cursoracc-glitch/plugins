using Facepunch;
using Rust;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
namespace Oxide.Plugins
{
    [Info("CopterSpawns", "OxideBro", "0.0.1")]
    public class CopterSpawns : RustPlugin
    {
        #region Oxide
        string copter = "assets/content/vehicles/minicopter/minicopter.entity.prefab";

        private void OnServerInitialized()
        {
            if (ConVar.Server.level == "Barren")
            {
                PrintWarning($"Server size map: {TerrainMeta.Size.x}, Minicopter population {MiniCopter.population}, Loaded {UnityEngine.Object.FindObjectsOfType<MiniCopter>().Count()} MiniCopters");
                StartSpawn();
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || entity?.net?.ID == null) return;
            try
            {
                if (entity is MiniCopter) NextTick(() => StartSpawn());
            }
            catch (NullReferenceException) { }
        }
        #endregion

        #region Spawn
        void StartSpawn()
        {
            var ents = UnityEngine.Object.FindObjectsOfType<MiniCopter>();
            if (ents != null)
            {
                var entcount = ents.Count();
                var count = MiniCopter.population * TerrainMeta.Size.x / 1000;
                if (count - entcount > 0) PrintWarning($"At the moment we will create {count - entcount} minicopter");
                for (int i = 0; i < count - entcount; i++)
                {
                    Vector3 vector = GetEventPosition();


                    BaseEntity copter = GameManager.server.CreateEntity(this.copter, vector, new Quaternion(), true);
                    copter.enableSaving = true;
                    copter.Spawn();
                }
            }
        }

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
            float num = 1000f, x = TerrainMeta.Size.x / 5;

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

            eventPos.y = GetGroundPosition(eventPos);

            if (eventPos.y < 0)
                GetEventPosition();
            return eventPos;
        }
        #endregion
    }
}