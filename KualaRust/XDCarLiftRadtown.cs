using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("XDCarLiftRadtown", "DezLife", "0.1.3")]
    [Description("Лифты в супермаркетах и на заправках")]
    class XDCarLiftRadtown : RustPlugin
    {
        #region Var
        List<uint> modularCars = new List<uint>();
        #endregion

        #region Hooks
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (modularCars != null)
                {
                    for(int i = 0; i < modularCars.Count; i++)
                    {
                        if (modularCars[i] == entity.net.ID) return false;
                    }
                }
            }
            catch (NullReferenceException) { }
            return null;
        }
        private void OnServerInitialized()
        {        
            foreach (var mount in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (mount.name.Contains("gas_station_1") && config.pluginSettings.gasstation)
                {
                    var pos = mount.transform.position + mount.transform.rotation * new Vector3(4.2f, 0f, -0.5f);
                    pos.y = TerrainMeta.HeightMap.GetHeight(pos);
                    CrateLift(pos, mount.transform.rotation);
                }
                else if (mount.name.Contains("supermarket_1") && config.pluginSettings.supermarket)
                {
                    var pos = mount.transform.position + mount.transform.rotation * new Vector3(0.2f, 0f, 17.5f);
                    pos.y = TerrainMeta.HeightMap.GetHeight(pos);
                    CrateLift(pos, mount.transform.rotation);
                }
            }
        }
        #endregion

        #region Metods
        private void CrateLift(Vector3 pos, Quaternion quaternion)
        {
            RaycastHit[] rHit = Physics.SphereCastAll(pos, 5f, Vector3.one);
            
            for(int i = 0; i < rHit.Length; i++)
            {
                var ent = rHit[i].GetEntity();
                if (ent != null && (ent is ModularCarGarage))
                {
                    ent.SetFlag(BaseEntity.Flags.On, true);
                    modularCars.Add(ent.net.ID);
                    return;
                }
            }
           
            ModularCarGarage modularCar = GameManager.server.CreateEntity("assets/prefabs/deployable/modular car lift/electrical.modularcarlift.deployed.prefab", pos, quaternion) as ModularCarGarage;
            modularCar.Spawn();
            modularCar.OwnerID = 23423423;
            modularCar.SetFlag(BaseEntity.Flags.On, true);
            modularCars.Add(modularCar.net.ID);
        }

        #endregion

        #region Configuration

        public static Configuration config = new Configuration();
        public class Configuration
        {
            public class PluginSettings
            {
                [JsonProperty("Спавнить у супермаркетов ?")]
                public bool supermarket;
                [JsonProperty("Спавнить у заправок ?")]
                public bool gasstation;
            }

            [JsonProperty("Настройки спавна")]
            public PluginSettings pluginSettings;

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    pluginSettings = new PluginSettings
                    {
                        gasstation = true,
                        supermarket = true
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка#skykey чтения конфигурации 'oxide/config/', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config, true);

        #endregion
    }
}
