using Oxide.Core;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Helicopter Instant Takeoff", "bsdinis", "0.0.9")]
    [Description("Allows helicopters to takeoff instantly when the engine starts.")]
    class HelicopterInstantTakeoff : RustPlugin
    {
        void Init()
        {
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null)
                {
                    throw new Exception();
                }
                else
                {
                    SaveConfig();
                }
            }
            catch
            {
                PrintError("CONFIG FILE IS INVALID!\nCheck config file and reload HelicopterInstantTakeoff.");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            if (!string.IsNullOrWhiteSpace(config.AirPermission) && !permission.PermissionExists(config.AirPermission))
            {
                permission.RegisterPermission(config.AirPermission, this);
            }
            if (!string.IsNullOrWhiteSpace(config.GroundPermission) && !permission.PermissionExists(config.GroundPermission))
            {
                permission.RegisterPermission(config.GroundPermission, this);
            }
        }

        ConfigData config;
        class ConfigData
        {
            public string AirPermission;
            public string GroundPermission;
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData
            {
                AirPermission = "helicopterinstanttakeoff.air",
                GroundPermission = "helicopterinstanttakeoff.ground"
            };
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        bool DriverHasPermission(PlayerHelicopter heli, string perm)
        {
            List<BaseVehicle.MountPointInfo> mountPoints = heli.mountPoints;
            if (mountPoints == null || mountPoints.Count < 1)
            {
                return false;
            }
            BaseMountable mountable = mountPoints[0].mountable;
            if (mountable == null)
            {
                return false;
            }
            BasePlayer player = mountable._mounted;
            if (player == null || (!string.IsNullOrWhiteSpace(perm) && !permission.UserHasPermission(player.UserIDString, perm)))
            {
                return false;
            }
            return true;
        }

        object OnEngineStart(PlayerHelicopter heli)
        {
            if (!DriverHasPermission(heli, (!heli.Grounded() ? config.AirPermission : config.GroundPermission)))
            {
                return null;
            }
            heli.engineController.FinishStartingEngine();
            return false;
        }
    }
}