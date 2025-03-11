using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("RandomSpawns", "k1lly0u", "0.2.0", ResourceId = 0)]
    class RandomSpawns : RustPlugin
    {
        #region Fields
        private bool isDisabled;
        private int spawnCount;
        private Hash<BiomeType, List<Vector3>> spawnPoints = new Hash<BiomeType, List<Vector3>>();

        private int minPlayerReq = 1;
        System.Random random = new System.Random();
        #endregion

        #region Oxide Hooks
        [ChatCommand("showspawns")]
        private void cmdShowSpawns(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;           
            SendReply(player, "Total spawn count: " + spawnCount);
            foreach (var list in spawnPoints.Values)
            {
                foreach (var position in list)
                    player.SendConsoleCommand("ddraw.box", 30f, Color.magenta, position, 1f);
            }
        }

        private void OnServerInitialized()
        {           
            isDisabled = true;
            LoadVariables();
            CalculateMinimumPlayers();
            GenerateSpawnpoints();
        }
        private object OnPlayerRespawn(BasePlayer player)
        {
            if (isDisabled || BasePlayer.activePlayerList.Count < minPlayerReq)
                return null;
                        
            var targetpos = GetSpawnPoint(false);
            if (targetpos == null)
                return null;

            BasePlayer.SpawnPoint spawnPoint1 = new BasePlayer.SpawnPoint();
            spawnPoint1.pos = (Vector3)targetpos;
            spawnPoint1.rot = new Quaternion(0f, 0f, 0f, 1f);
            return spawnPoint1;
        }
        private BiomeType GetMajorityBiome(Vector3 position)
        {
            Dictionary<BiomeType, float> biomes = new Dictionary<BiomeType, float>
            {
                {BiomeType.Arctic, TerrainMeta.BiomeMap.GetBiome(position, TerrainBiome.ARCTIC) },
                {BiomeType.Arid, TerrainMeta.BiomeMap.GetBiome(position, TerrainBiome.ARID) },
                {BiomeType.Temperate, TerrainMeta.BiomeMap.GetBiome(position, TerrainBiome.TEMPERATE) },
                {BiomeType.Tundra, TerrainMeta.BiomeMap.GetBiome(position, TerrainBiome.TUNDRA) }
            };
            return biomes.OrderByDescending(x => x.Value).ToArray()[0].Key;           
        }
        #endregion

        #region Functions 
        private void CalculateMinimumPlayers()
        {
            minPlayerReq = 1000;

            foreach(var biome in configData.SpawnAreas)
            {
                if (biome.Value.Players < minPlayerReq)
                    minPlayerReq = biome.Value.Players;
            }
        }

        private object GetSpawnPoint(bool ignorePlayerRestriction = true)
        {
            BiomeType biomeType = spawnPoints.ElementAt(UnityEngine.Random.Range(0, spawnPoints.Count - 1)).Key;

            if (!ignorePlayerRestriction)            
            {
                List<BiomeType> availableTypes = new List<BiomeType>();
                int onlinePlayers = BasePlayer.activePlayerList.Count;

                foreach(var biome in configData.SpawnAreas)
                {
                    if (biome.Value.Enabled && onlinePlayers >= biome.Value.Players)                    
                        availableTypes.Add(biome.Key);                    
                }

                biomeType = availableTypes.GetRandom();                
                availableTypes.Clear();
            }

            Vector3 targetPos = spawnPoints[biomeType].GetRandom();
            if (targetPos == Vector3.zero)
                return null;

            List<BaseEntity> entities = Facepunch.Pool.GetList<BaseEntity>();
            Vis.Entities(targetPos, 15f, entities, LayerMask.GetMask("Construction", "Deployable"));
            int count = entities.Count;
            Facepunch.Pool.FreeList(ref entities);
            if (count > 0)
            {
                spawnPoints[biomeType].Remove(targetPos);
                -- spawnCount;
                if (spawnCount < 10)
                {
                    PrintWarning("All current spawnpoints have been overrun by buildings and such. Disabling random spawnpoints");
                    isDisabled = true;
                    return null;
                }
                return GetSpawnPoint(false);
            }
            return targetPos;
        }

        private void GenerateSpawnpoints()
        {
            PrintWarning("Generating spawnpoints. This may take a moment");
            for (int i = 0; i < 1500; i++)
            {
                float max = TerrainMeta.Size.x / 2;
                var success = FindNewPosition(new Vector3(0,0,0), max);
                if (success is Vector3)
                {
                    Vector3 spawnPoint = (Vector3)success;
                    float height = TerrainMeta.HeightMap.GetHeight(spawnPoint);
                    if (spawnPoint.y >= height && !(spawnPoint.y - height > 1))
                    {
                        BiomeType biome = GetMajorityBiome(spawnPoint);
                        if (configData.SpawnAreas[biome].Enabled)
                        {
                            if (!spawnPoints.ContainsKey(biome))
                                spawnPoints.Add(biome, new List<Vector3>());
                            spawnPoints[biome].Add(spawnPoint);
                        }
                    }
                }
            }            
            foreach (var biome in spawnPoints)
                spawnCount += biome.Value.Count;

            PrintWarning($"{spawnCount} spawn points generated!");
            isDisabled = false;
        }

        private Vector3 CalculatePoint(Vector3 position, float max)
        {
            var angle = Math.PI * 2.0f * random.NextDouble();
            var radius = Math.Sqrt(random.NextDouble()) * max;
            var x = position.x + radius * Math.Cos(angle);
            var y = position.y + radius * Math.Sin(angle);
            return new Vector3((float)x, 300, (float)y);
        }

        private object FindNewPosition(Vector3 position, float max, bool failed = false)
        {
            var targetPos = UnityEngine.Random.insideUnitCircle * max;
            var sourcePos = new Vector3(position.x + targetPos.x, 300, position.z + targetPos.y);
            var hitInfo = RayPosition(sourcePos);
            var success = ProcessRay(hitInfo);
            if (success == null)
            {
                if (failed) return null;
                else return FindNewPosition(position, max, true);
            }
            else if (success is Vector3)
            {
                if (failed) return null;
                else return FindNewPosition(new Vector3(sourcePos.x, ((Vector3)success).y, sourcePos.y), max, true);
            }
            else
            {
                sourcePos.y = Mathf.Max((float)success, TerrainMeta.HeightMap.GetHeight(sourcePos));
                return sourcePos;
            }
        }

        private object ProcessRay(RaycastHit hitInfo)
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider?.gameObject.layer == LayerMask.NameToLayer("Water"))
                    return null;
                if (hitInfo.collider?.gameObject.layer == LayerMask.NameToLayer("Prevent Building"))
                    return null;
                if (hitInfo.GetEntity() != null)
                {
                    return hitInfo.point.y;
                }
                if (hitInfo.collider?.name == "areaTrigger")
                    return null;
                if (hitInfo.collider?.GetComponentInParent<SphereCollider>() || hitInfo.collider?.GetComponentInParent<BoxCollider>())
                {
                    return hitInfo.collider.transform.position + new Vector3(0, -1, 0);
                }               
            }
            return hitInfo.point.y;
        }

        private RaycastHit RayPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;
            Physics.Raycast(sourcePos, Vector3.down, out hitInfo);//, LayerMask.GetMask("Terrain", "World", "Construction"));

            return hitInfo;
        }
        #endregion

        #region Config    
        enum BiomeType { Arid, Arctic, Temperate, Tundra }
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Biome Options")]
            public Dictionary<BiomeType, BiomeOptions> SpawnAreas { get; set; }

            public class BiomeOptions
            {
                [JsonProperty(PropertyName = "Enable spawn points to be generated in this biome")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "Minimum required online players before spawns from this biome will be selected")]
                public int Players { get; set; }
            }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                SpawnAreas = new Dictionary<BiomeType, ConfigData.BiomeOptions>
                {
                    [BiomeType.Temperate] = new ConfigData.BiomeOptions { Enabled = true, Players = 1 },
                    [BiomeType.Arid] = new ConfigData.BiomeOptions { Enabled = true, Players = 10 },
                    [BiomeType.Tundra] = new ConfigData.BiomeOptions { Enabled = true, Players = 20 },
                    [BiomeType.Arctic] = new ConfigData.BiomeOptions { Enabled = true, Players = 30 }
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}
