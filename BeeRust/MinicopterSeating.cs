using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Minicopter Seating", "Bazz3l", "1.1.6")]
    [Description("Spawn extra seats on each side of the minicopter.")]
    public class MinicopterSeating : CovalencePlugin
    {
        #region Fields

        private readonly GameObjectRef _gameObjectRef = new GameObjectRef { guid = "dc329880dec7ab343bc454fd969d5709" };
        private readonly Vector3 _seat1 = new Vector3(0.6f, 0.2f, -0.3f);
        private readonly Vector3 _seat2 = new Vector3(-0.6f, 0.2f, -0.3f);
	private readonly Vector3 _seat3 = new Vector3(0.0f, 0.5f, -2.0f);

        #endregion

        #region Oxide

        private void OnEntitySpawned(BaseVehicle mini)
        {
            if (mini.mountPoints.Count < 4 && mini.ShortPrefabName == "minicopter.entity")
                SetupSeating(mini);
        }

        #endregion

        #region Core

        private void SetupSeating(BaseVehicle vehicle)
        {
            vehicle.mountPoints.Add(CreateMount(vehicle.mountPoints, _seat1));
            vehicle.mountPoints.Add(CreateMount(vehicle.mountPoints, _seat2));
	    vehicle.mountPoints.Add(CreateMount(vehicle.mountPoints, _seat3));
        }

        private BaseVehicle.MountPointInfo CreateMount(List<BaseVehicle.MountPointInfo> mountPoints, Vector3 position)
        {
            return new BaseVehicle.MountPointInfo
            {
                pos = position,
                rot = mountPoints[1].rot,
                prefab = _gameObjectRef,
                mountable = mountPoints[1].mountable,
            };
        }

        #endregion
    }
}