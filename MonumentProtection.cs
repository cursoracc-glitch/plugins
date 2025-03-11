using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;
using static Oxide.Plugins.MonumentProtectionEx.MonumentProtectionEx;
using System.Linq;

namespace Oxide.Plugins
{
    [Info( "Monument Protection", "Waggy", "1.0.4", ResourceId = 154 )]
    [Description( "" )]

    public class MonumentProtection : RustPlugin
    {
        private static MonumentProtection PluginInstance;
        private List<SamSite> SamSites = new List<SamSite>();

        public Dictionary<Vector3, MonumentInfo> SamPositions = new Dictionary<Vector3, MonumentInfo>();

        private Timer RespawnTimer;

        //[ChatCommand( "localpos" )]
        //void LocalPosition( BasePlayer player, string command, string[] args )
        //{
        //    var samSite = RaycastLookDirection<SamSite>( player );
        //    if ( samSite != null )
        //    {
        //        float lowestDist = float.MaxValue;
        //        MonumentInfo closest = null;
        //        foreach ( var monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>()/*.Where(x => x.GetMonumentName() == MonumentName.LaunchSite) */)
        //        {
        //            float dist = Vector3.Distance( player.transform.position, monument.transform.position );
        //            if ( dist < lowestDist )
        //            {
        //                lowestDist = dist;
        //                closest = monument;
        //            }
        //        }

        //        var localPos = closest.transform.InverseTransformPoint( samSite.transform.position );
        //        var rotation = samSite.transform.rotation;
        //        PrintToChat( $"Pos: {localPos.ToString( "F4" )} Rotation: {rotation.ToString( "F4" )}" );
        //    }
        //}

        #region Entity Raycast

        int layer = LayerMask.GetMask( "AI", "Construction", "Deployed", "Default", "Debris", "Ragdoll", "Tree", "Vehicle Movement" );

        private T RaycastLookDirection<T>( BasePlayer player, string prefabName = "" ) where T : BaseEntity
        {
            RaycastHit hit;
            if ( !Physics.Raycast( player.eyes.BodyRay(), out hit, 5f, layer ) )
            {
                return null;
            }
            T entity = hit.GetEntity() as T;
            return entity;
        }

        #endregion

        public void SpawnSAMSites()
        {
            foreach ( var monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>() )
            {
                bool canSpawn = false;
                var monumentName = monument.GetMonumentName();
                if ( !config.MonumentWithSAMs.TryGetValue( monumentName, out canSpawn ) || !canSpawn )
                    continue;

                switch ( monumentName )
                {
                    default: continue;
                    case MonumentName.LaunchSite:
                        {
                            SamPositions.Add( new Vector3( 222.5791f, 20.7363f, 24.2286f ), monument );
                            SamPositions.Add( new Vector3( 226.3754f, 20.7363f, -23.9451f ), monument );
                            SamPositions.Add( new Vector3( 117.1534f, 12.0363f, 31.9013f ), monument );

                            break;
                        }
                    case MonumentName.Trainyard:
                        {
                            SamPositions.Add( new Vector3( 58.4138f, 30.2905f, 12.3804f ), monument );
                            SamPositions.Add( new Vector3( 49.1168f, 30.2773f, 18.5964f ), monument );
                                                                                        
                            break;                                                      
                        }                                                               
                    case MonumentName.Dome:                                             
                        {                                                               
                            SamPositions.Add( new Vector3( 35.9377f, 18.0663f, 41.2695f ), monument );
                            SamPositions.Add( new Vector3( 19.5758f, 59.4494f, -7.3288f ), monument );
                            SamPositions.Add( new Vector3( -24.5651f, 59.4994f, -8.7703f ), monument );
                            SamPositions.Add( new Vector3( -19.7446f, 3.1654f, -34.9776f ), monument );
                            SamPositions.Add( new Vector3( 20.3374f, 3.4097f, 50.4880f ), monument );
                                                                                         
                            break;                                                       
                        }                                                                
                    case MonumentName.Powerplant:                                        
                        {                                                                
                            SamPositions.Add( new Vector3( -38.0946f, 22.7686f, 8.9574f ), monument );
                            SamPositions.Add( new Vector3( -43.0134f, 18.2407f, 18.9424f ), monument );
                            SamPositions.Add( new Vector3( -27.4102f, 21.2407f, 12.9824f ), monument );
                            SamPositions.Add( new Vector3( -28.3916f, 21.3069f, 6.2423f ), monument );

                            break;
                        }
                    case MonumentName.WaterTreatment:
                        {
                            SamPositions.Add( new Vector3( 68.9818f, 12.1657f, 22.3747f ), monument );
                            SamPositions.Add( new Vector3( 71.332f, 12.2664f, -20.8726f ), monument );

                            break;
                        }
                    case MonumentName.OilRig_Small:
                        {
                            SamPositions.Add( new Vector3( 38.7308f, 31.4891f, 0.8049f ), monument );
                            SamPositions.Add( new Vector3( 38.5506f, 31.4891f, -21.4670f ), monument );
                            SamPositions.Add( new Vector3( 10.2068f, 30, -32.2483f ), monument );
                            SamPositions.Add( new Vector3( 6.2874f, 30, 2.1386f ), monument );
                            SamPositions.Add( new Vector3( -4.6217f, 0.9817f, -28.4416f ), monument );
                            SamPositions.Add( new Vector3( -4.4660f, 0.9220f, -6.0966f ), monument );
                            SamPositions.Add( new Vector3( 30.7479f, 2.0713f, -25.8844f ), monument );
                            SamPositions.Add( new Vector3( 16.7402f, 0.1f, 4.6281f ), monument );

                            break;
                        }
                    case MonumentName.OilRig_Large:
                        {
                            SamPositions.Add( new Vector3( 27.4640f, 45.1916f, 16.0521f ), monument );
                            SamPositions.Add( new Vector3( 27.8638f, 45.1916f, -6.1706f ), monument );
                            SamPositions.Add( new Vector3( 7.1513f, 42.1259f, 36.2357f ), monument );
                            SamPositions.Add( new Vector3( 12.1727f, 39.1474f, -25.9371f ), monument );
                            SamPositions.Add( new Vector3( -16.6521f, 40.5907f, 1.6809f ), monument );
                            SamPositions.Add( new Vector3( -0.3493f, 39.1000f, -38.5986f ), monument );
                            SamPositions.Add( new Vector3( 16.4913f, 2.7f, -10.6589f ), monument );
                            SamPositions.Add( new Vector3( 16.3649f, 3f, 10.6897f ), monument );
                            SamPositions.Add( new Vector3( -15.16f, 2.8703f, -32.5268f ), monument );
                            SamPositions.Add( new Vector3( -15.0637f, 2.8798f, 32.5015f ), monument );
                            SamPositions.Add( new Vector3( 15.0416f, 2.9619f, 32.4463f ), monument );
                            SamPositions.Add( new Vector3( 14.9828f, 2.8417f, -32.4670f ), monument );

                            break;
                        }
                    case MonumentName.Airfield:
                        {
                            SamPositions.Add( new Vector3( -46.9147f, 21.4142f, -87.9877f ), monument );
                            SamPositions.Add( new Vector3( -29.0119f, 9.4142f, -90.7702f ), monument );
                            SamPositions.Add( new Vector3( -3.6652f, 9.4142f, -102.1889f ), monument );
                            SamPositions.Add( new Vector3( -73.6783f, 3.1142f, -87.7945f ), monument );
                            SamPositions.Add( new Vector3( -117.6489f, 18.0142f, 50.3876f ), monument );
                            SamPositions.Add( new Vector3( 9.2417f, 13.4142f, 39.8607f ), monument );
                            SamPositions.Add( new Vector3( 48.8095f, 1.9142f, -64.6002f ), monument );

                            break;
                        }
                }
            }

            foreach ( var monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>() )
            {
                foreach ( var position in SamPositions )
                {
                    if ( position.Value == monument )
                    {
                        var samPosition = monument.transform.TransformPoint( position.Key );
                        var samSite = GameManager.server.CreateEntity( "assets/prefabs/npc/sam_site_turret/sam_static.prefab", samPosition ) as SamSite;
                        samSite.lifestate = BaseCombatEntity.LifeState.Alive;
                        samSite.health = samSite._maxHealth;
                        samSite.SetFlag( BaseEntity.Flags.Reserved1, false, true );
                        samSite.Spawn();
                        samSite.SendNetworkUpdateImmediate();

                        SamSites.Add( samSite );

                        //Puts( $"Spawning one of {monument.GetMonumentName().ToString()}'s SAMs at {samSite.transform.position.ToString()}" );
                    }
                }
            }
        }

        public void RespawnSamSites()
        {
            foreach ( var sam in SamSites )
            {
                sam?.Kill();
            }

            SamSites.Clear();

            SpawnSAMSites();
        }

        #region Hooks

        private void Init()
        {
            config = Config.ReadObject<ConfigData>();
            PluginInstance = this;
        }

        [HookMethod( "OnServerInitialized" )]
        void OnServerInitialized()
        {
            SpawnSAMSites();

            if ( !config.InvincibleSams )
                RespawnTimer = timer.Every( config.RespawnTimer * 60, RespawnSamSites );
        }

        [HookMethod( "Unload" )]
        void Unload()
        {
            foreach ( var sam in SamSites )
            {
                sam?.Kill();
            }

            SamSites.Clear();

            RespawnTimer?.Destroy();

            PluginInstance = null;
        }

        object OnEntityTakeDamage( BaseCombatEntity entity, HitInfo info )
        {
            if ( config.InvincibleSams && entity.ShortPrefabName == "sam_static" )
                return false;

            return null;
        }

        #endregion

        #region Configuration

        private ConfigData config;

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject( new ConfigData(), true );
        }

        private new void SaveConfig()
        {
            Config.WriteObject( config, true );
        }

        public class ConfigData
        {
            [JsonProperty( "Monuments with SAMs" )]
            public Dictionary<MonumentName, bool> MonumentWithSAMs = new Dictionary<MonumentName, bool>()
            {
                { MonumentName.Trainyard, true },
                { MonumentName.Dome, true },
                { MonumentName.Powerplant, true },
                { MonumentName.WaterTreatment, true },
                { MonumentName.OilRig_Small, true },
                { MonumentName.OilRig_Large, true },
                { MonumentName.Airfield, true },
                { MonumentName.LaunchSite, true },
            };

            [JsonProperty( "Monument SAM Site Indestructible" )]
            public bool InvincibleSams = true;

            [JsonProperty( "Respawn Timer for Sams" )]
            public float RespawnTimer = 30;
        }

        #endregion
    }

    namespace MonumentProtectionEx
    {
        public static class MonumentProtectionEx
        {
            public enum MonumentName
            {
                Unknown = 0,
                Lighthouse,
                MiningOutpost,
                Dome,
                SatelliteDish,
                SewerBranch,
                Powerplant,
                Trainyard,
                Airfield,
                MilitaryTunnel,
                WaterTreatment,
                SulfurQuarry,
                StoneQuarry,
                HqmQuarry,
                GasStation,
                Supermarket,
                LaunchSite,
                Outpost,
                BanditCamp,
                Harbor_A,
                Harbor_B,
                Junkyard,
                OilRig_Small,
                OilRig_Large,
            }

            private static Dictionary<string, MonumentName> MonumentToName = new Dictionary<string, MonumentName>()
            {
                { "assets/bundled/prefabs/autospawn/monument/small/warehouse.prefab", MonumentName.MiningOutpost },
                { "assets/bundled/prefabs/autospawn/monument/lighthouse/lighthouse.prefab", MonumentName.Lighthouse },
                { "assets/bundled/prefabs/autospawn/monument/small/satellite_dish.prefab", MonumentName.SatelliteDish },
                { "assets/bundled/prefabs/autospawn/monument/small/sphere_tank.prefab", MonumentName.Dome },
                { "assets/bundled/prefabs/autospawn/monument/harbor/harbor_1.prefab", MonumentName.Harbor_A },
                { "assets/bundled/prefabs/autospawn/monument/harbor/harbor_2.prefab", MonumentName.Harbor_B },
                { "assets/bundled/prefabs/autospawn/monument/large/airfield_1.prefab", MonumentName.Airfield },
                { "assets/bundled/prefabs/autospawn/monument/large/junkyard_1.prefab", MonumentName.Junkyard },
                { "assets/bundled/prefabs/autospawn/monument/large/launch_site_1.prefab", MonumentName.LaunchSite },
                { "assets/bundled/prefabs/autospawn/monument/large/military_tunnel_1.prefab", MonumentName.MilitaryTunnel },
                { "assets/bundled/prefabs/autospawn/monument/large/powerplant_1.prefab", MonumentName.Powerplant },
                { "assets/bundled/prefabs/autospawn/monument/large/trainyard_1.prefab", MonumentName.Trainyard },
                { "assets/bundled/prefabs/autospawn/monument/large/water_treatment_plant_1.prefab", MonumentName.WaterTreatment },
                { "assets/bundled/prefabs/autospawn/monument/medium/bandit_town.prefab", MonumentName.BanditCamp },
                { "assets/bundled/prefabs/autospawn/monument/medium/compound.prefab", MonumentName.Outpost },
                { "assets/bundled/prefabs/autospawn/monument/medium/radtown_small_3.prefab", MonumentName.SewerBranch },
                { "assets/bundled/prefabs/autospawn/monument/small/gas_station_1.prefab", MonumentName.GasStation },
                { "assets/bundled/prefabs/autospawn/monument/small/mining_quarry_a.prefab", MonumentName.SulfurQuarry },
                { "assets/bundled/prefabs/autospawn/monument/small/mining_quarry_b.prefab", MonumentName.StoneQuarry },
                { "assets/bundled/prefabs/autospawn/monument/small/mining_quarry_c.prefab", MonumentName.HqmQuarry },
                { "assets/bundled/prefabs/autospawn/monument/offshore/oilrig_2.prefab", MonumentName.OilRig_Small },
                { "assets/bundled/prefabs/autospawn/monument/offshore/oilrig_1.prefab", MonumentName.OilRig_Large },
            };

            public static MonumentName GetMonumentName( this MonumentInfo monument )
            {
                MonumentName name;

                var gameObject = monument.gameObject;

                while ( gameObject.name.StartsWith( "assets/" ) == false && gameObject.transform.parent != null )
                {
                    gameObject = gameObject.transform.parent.gameObject;
                }

                MonumentToName.TryGetValue( gameObject.name, out name );

                return name;
            }
        }
    }
}