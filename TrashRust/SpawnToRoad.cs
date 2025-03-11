using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SpawnToRoad", "Mercury", "0.0.1")]
    [Description("Модальное меню")]
    class SpawnToRoad : RustPlugin
    {
        public List<float> RandomShift = new List<float>
        {
            9f,
            -9f,
            -9.5f,
            9.5f,
            8f,
            -8f,
            10f,
            -10f
        };
        public List<Vector3> SpawnPoints = new List<Vector3>();

        #region Hooks
        private void OnServerInitialized() => NextTick(() => { GetRoads(); Puts($"Найдено позиций : {SpawnPoints.Count}"); });
        object OnPlayerRespawn(BasePlayer player)
        {
            Vector3 NewPositionSpawn = SpawnPoints.GetRandom();
            BasePlayer.SpawnPoint spawnPoint = new BasePlayer.SpawnPoint();
            spawnPoint.pos = NewPositionSpawn;
            return spawnPoint;
        }
        #endregion

        #region Metods

        bool GetCheckPositiont(Vector3 Position)
        {
            if (Physics.CheckSphere(Position, 15f, LayerMask.GetMask("Construction", "Default", "Deployed", "Trigger")))
                return false;
            return true;
        }

        void GetRoads()
        {
            foreach (PathList x in TerrainMeta.Path.Roads)
                foreach (var point in x.Path.Points.Where(p => GetCheckPositiont(p)))
                {
                    Vector3 SpawnPosition = new Vector3(point.x + RandomShift.GetRandom(), point.y, point.z + RandomShift.GetRandom());
                    SpawnPosition.y = TerrainMeta.HeightMap.GetHeight(SpawnPosition);
                    SpawnPoints.Add(SpawnPosition);
                }
        }

        #endregion
    }
}
