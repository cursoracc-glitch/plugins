using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AirMarker", "Fierrov", "1.0.0")]
    internal class AirMarker : RustPlugin
    {
       
        #region OxideHooks

        private void OnServerInitialized()
        {
            PrintWarning(
                $"Was changed Fierrov");
            LoadConfig();
        }

        private void Unload()
        {
            var marker = GameObject.FindObjectsOfType<Marker>();
            foreach (var check in marker)
            {
                check?.Kill();
            }
        }
        void OnEntitySpawned(SupplyDrop air)
        {
            if (air == null) return;
            air.gameObject.AddComponent<Marker>();
        }

        void OnLootEntity(BasePlayer player, SupplyDrop air)
        {
            if (air == null) return;
            var comp = air.GetComponent<Marker>();
            if (comp == null) return;
            comp.Kill();
        }

        #endregion

        class Marker : MonoBehaviour
        {
            private BaseEntity entity;
            MapMarkerGenericRadius mapmarker;

            private void Awake()
            {
                entity = GetComponent<BaseEntity>();
                InstallMarker();
            }

            private void InstallMarker()
            {
                mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab") as MapMarkerGenericRadius;
                mapmarker.enableSaving = false;
                mapmarker.SetParent(entity);
                mapmarker.Spawn();
                mapmarker.radius = 0.1f;
                mapmarker.alpha = 1f;
                UnityEngine.Color color = new UnityEngine.Color(0.23f, 0.48f, 0.18f, 0.70f);
                UnityEngine.Color color2 = new UnityEngine.Color(0, 0, 0, 0);
                mapmarker.color1 = color;
                mapmarker.color2 = color2;
                mapmarker.SendUpdate();
            }


            public void Kill()
            {
                mapmarker?.Kill();
                Destroy(this);
            }
        }
    }
}