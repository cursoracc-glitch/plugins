using System;
using System.Collections;
using Network;
using System.Collections.Generic;
using UnityEngine;
using Facepunch;
using Rust;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("SharkBait", "Colon Blow", "1.0.4")]
    class SharkBait : RustPlugin
    {
        // Fix for shark parts now spawning to see them

        #region Load

        void Loaded()
        {
            LoadVariables();
            LoadMessages();
            permission.RegisterPermission("sharkbait.admin", this);
        }

        void OnServerInitialized()
        {
            timer.In(10, RespawnAllSharks);
        }

        #endregion

        #region Configuration

        static ulong GWFin = 1407588505;
        static ulong GWFront = 1407587906;
        static ulong GWBack = 1407587156;

        static ulong HHFin = 1403745229;
        static ulong HHFront = 1403174634;
        static ulong HHBack = 1403180514;

        bool EnableAutoSpawn = true;
        bool EnableSpawnAtRockFormations = true;
        bool EnableSpawnAtDiveSites = true;
        bool EnableSpawnAtFloatingLoot = true;

        static float SpawnActivationRadius = 50f;
        static float SharkAggroRadius = 20f;
        static float SharkBiteRange = 2f;
        static float SharkDespawnTime = 500f;
        static float SharkSwimSpeed = 6f;
        static float SharkDamageToPlayer = 20f;
        int SharkSpawnChance = 30;
        static bool SharksCanAttackBoats = true;
        static bool SharksCanAttackPlayers = true;

        bool Changed;

        void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        void LoadConfigVariables()
        {
            CheckCfgUlong("Skin ID - Great White Front", ref GWFront);
            CheckCfgUlong("Skin ID - Great White Back", ref GWBack);
            CheckCfgUlong("Skin ID - Great White Fin", ref GWFin);

            CheckCfgUlong("Skin ID - HammerHead Front", ref HHFront);
            CheckCfgUlong("Skin ID - HammerHead Back", ref HHBack);
            CheckCfgUlong("Skin ID - HammerHead Fin", ref HHFin);

            CheckCfg("Chance Roll - chances a loot or dive site will have a shark spawner : ", ref SharkSpawnChance);

            CheckCfg("Global - Sharks can attack Boats ? ", ref SharksCanAttackBoats);
            CheckCfg("Global - Sharks can attack Players ? ", ref SharksCanAttackPlayers);

            CheckCfg("Autospawn - Enable the Autospawn feature ? ", ref EnableAutoSpawn);
            CheckCfg("Autospawn - Enable autospawn at Rock Formations ? ", ref EnableSpawnAtRockFormations);
            CheckCfg("Autospawn - Enable autospawn at Dive Sites ? ", ref EnableSpawnAtDiveSites);
            CheckCfg("Autospawn - Enable autospawn at Floating Water Loot Sites ? ", ref EnableSpawnAtFloatingLoot);

            CheckCfgFloat("Shark Spawner - player detection radius : ", ref SpawnActivationRadius);

            CheckCfgFloat("Shark - aggro radius of shark to players or boats : ", ref SharkAggroRadius);
            CheckCfgFloat("Shark - bite range radius of shark : ", ref SharkBiteRange);
            CheckCfgFloat("Shark - despawn time of shark after spawn with no interaction : ", ref SharkDespawnTime);
            CheckCfgFloat("Shark - swim speed of shark : ", ref SharkSwimSpeed);
            CheckCfgFloat("Shark - damage done to player when shark attacks : ", ref SharkDamageToPlayer);
        }

        void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = System.Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }

        void CheckCfgUlong(string Key, ref ulong var)
        {

            if (Config[Key] != null)
                var = Convert.ToUInt64(Config[Key]);
            else
                Config[Key] = var;
        }

        #endregion

        #region Localization

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["noperms"] = "You don't have permission to use this command.",
                ["sharkhasspawned"] = "There is a shark in the water !!!!",
                ["sharkhasdied"] = "You have killed a shark.. good job !!",
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("sharkbait")]
        void chatSharkBait(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "sharkbait.admin"))
                AddSharkEntity(player.transform.position);
            else
                PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
        }

        [ChatCommand("sharkbait.respawn")]
        void chatSharkBaitRespawn(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "sharkbait.admin"))
                RespawnAllSharks();
            else
                PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
        }

        [ConsoleCommand("sharkbait.respawn")]
        void cmdConsoleSharkBaitRespawn(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player == null) { RespawnAllSharks(); return; }
            if (player != null)
            {
                if (permission.UserHasPermission(player.UserIDString, "sharkbait.admin"))
                    RespawnAllSharks();
                else
                    PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
            }
        }

        [ChatCommand("sharkbait.killall")]
        void chatSharkBaitKillAll(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "sharkbait.admin"))
                RemoveAllSharks();
            else
                PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
        }

        #endregion

        #region Hooks

        private void RemoveAllSharks()
        {
            DestroyAll<SharkEntity>();
            DestroyAll<SharkSpawnController>();
            List<BaseEntity> findspawns = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(new Vector3(0f, 0f, 0f), ((ConVar.Server.worldsize) / 2) + 1000f, findspawns);
            foreach (BaseEntity obj in findspawns)
            {
                if (obj.name.Contains("rug/rug.deployed"))
                {
                    if (obj.skinID == GWFront || obj.skinID == HHFront || obj.skinID == GWFin || obj.skinID == HHFin || obj.skinID == GWFront || obj.skinID == HHFront)
                    {
                        if (obj != null && !obj.IsDestroyed)
                        {
                            obj.gameObject.ToBaseEntity().Kill(BaseNetworkable.DestroyMode.None);
                        }
                    }
                }
            }
        }

        void OnEntitySpawned(BaseEntity entity, UnityEngine.GameObject gameObject)
        {
            if (!EnableAutoSpawn) return;
            if (gameObject == null) return;

            if (gameObject.name.Contains("rockformation_underwater") && EnableSpawnAtRockFormations)
            {
                RespawnHandler(gameObject);
            }
            if (gameObject.name.Contains("junkpile_water") && EnableSpawnAtFloatingLoot)
            {
                RespawnHandler(gameObject);
            }
            if (gameObject.name.Contains("divesite") && EnableSpawnAtDiveSites)
            {
                RespawnHandler(gameObject);
            }
        }

        void RespawnAllSharks()
        {
            if (!EnableAutoSpawn) return;
            RemoveAllSharks();

            var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var gobject in allobjects)
            {
                if (gobject.name.Contains("rockformation_underwater") && EnableSpawnAtRockFormations)
                {
                    RespawnHandler(gobject);
                }
                if (gobject.name.Contains("junkpile_water") && EnableSpawnAtFloatingLoot)
                {
                    RespawnHandler(gobject);
                }
                if (gobject.name.Contains("divesite") && EnableSpawnAtDiveSites)
                {
                    RespawnHandler(gobject);
                }
            }
            PrintWarning("Respawn of Shark Population has been completed.");
        }

        public void RespawnHandler(GameObject gobject)
        {
            var pos = gobject.transform.position;

            int roll = UnityEngine.Random.Range(0, 100);
            if (roll < SharkSpawnChance)
            {
                var hascontroller = gobject.GetComponent<SharkSpawnController>() ?? null;
                if (hascontroller != null) GameObject.Destroy(hascontroller);

                gobject.AddComponent<SharkSpawnController>();
            }
        }

        public void AddSharkEntity(Vector3 pos)
        {

            List<BaseEntity> nearshark = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(pos, 30f, nearshark);
            foreach (BaseEntity shark in nearshark)
            {
                if (shark.GetComponentInParent<SharkEntity>())
                {
                    return;
                }
            }
            SpawnShark(pos);
        }

        void SpawnShark(Vector3 pos)
        {
            string prefabsharkfin = "assets/prefabs/misc/orebonus/orebonus_generic.prefab";
            var groundy = TerrainMeta.HeightMap.GetHeight(pos);
            if (pos.y < groundy) pos.y = groundy + 2f;
            if (pos.y > -0.5f) pos.y = -0.5f;
            var sharkent = GameManager.server.CreateEntity(prefabsharkfin, new Vector3(pos.x, pos.y, pos.z), Quaternion.identity, true);
            sharkent.enableSaving = false;
            sharkent.Spawn();
            var addentity = sharkent.gameObject.AddComponent<SharkEntity>();
        }

        void Unload()
        {
            RemoveAllSharks();
        }

        void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                {
                    GameObject.Destroy(gameObj);
                }
        }

        #endregion

        #region Shark Spawn Controller

        class SharkSpawnController : MonoBehaviour
        {
            SharkBait _instance;
            SphereCollider detectionradius;
            Vector3 spawnlocation;
            bool doactivation;
            Timer mytimer;

            void Awake()
            {
                _instance = new SharkBait();
                detectionradius = gameObject.AddComponent<SphereCollider>();
                detectionradius.gameObject.layer = (int)Layer.Reserved1;
                detectionradius.isTrigger = true;
                detectionradius.radius = SpawnActivationRadius;
                spawnlocation = detectionradius.transform.position;
            }

            private void OnTriggerEnter(Collider col)
            {
                if (col == null || doactivation) return;
                var target = col.GetComponentInParent<BasePlayer>() ?? null;
                if (target != null)
                {
                    doactivation = true;
                    _instance.AddSharkEntity(spawnlocation);
                    ToggleTrigger();
                }
            }

            void ToggleTrigger()
            {
                mytimer = _instance.timer.Once(System.Convert.ToSingle(SharkDespawnTime), () =>
                {
                    if (this == null) { mytimer.Destroy(); return; }
                    doactivation = false;
                });
            }

            void OnDestroy()
            {
                GameObject.Destroy(detectionradius);
                GameObject.Destroy(this);
            }
        }

        #endregion

        #region SharkEntity

        class SharkEntity : BaseEntity
        {
            SharkBait instance;
            BaseEntity sharkentity;
            BaseEntity sharkfront;
            BaseEntity sharkback;
            BaseEntity sharkfin;
            BaseEntity sharkbait;
            int counter;
            SphereCollider aggroradius;
            Rigidbody rigidbody;
            Vector3 initialspawn;
            Vector3 spawnlocation;
            Vector3 position;
            Vector3 attackpos;
            Vector3 offset;
            private float _angle;
            float speed;
            float despawntimer;
            float despawntimelimit;
            int attackcounter;
            bool didattack;
            bool reversemovment;
            bool moveback;

            void Awake()
            {
                instance = new SharkBait();
                sharkentity = GetComponentInParent<BaseEntity>();
                sharkbait = null;
                initialspawn = sharkentity.transform.position;
                spawnlocation = sharkentity.transform.position;
                counter = 0;
                speed = SharkSwimSpeed;
                attackcounter = 0;
                didattack = false;
                reversemovment = false;
                moveback = false;
                despawntimer = 0f;
                despawntimelimit = (SharkDespawnTime * 10);

                aggroradius = sharkentity.gameObject.AddComponent<SphereCollider>();
                aggroradius.gameObject.layer = (int)Layer.Reserved1;
                aggroradius.isTrigger = true;
                aggroradius.radius = SharkAggroRadius;
                MovementRoll();
                SpawnSharky();
            }

            void MovementRoll()
            {
                int moveroll = UnityEngine.Random.Range(0, 2);
                if (moveroll == 1) reversemovment = true;
            }

            void SpawnSharky()
            {
                bool spawnhammerhead = false;
                int typeroll = UnityEngine.Random.Range(0, 2);
                if (typeroll == 1) spawnhammerhead = true;

                ulong frontskinid = GWFront;
                ulong backskinid = GWBack;
                ulong finskinid = GWFin;

                if (spawnhammerhead)
                {
                    frontskinid = HHFront;
                    backskinid = HHBack;
                    finskinid = HHFin;
                }

                string prefabsharkfin = "assets/prefabs/deployable/rug/rug.deployed.prefab";
                sharkfront = GameManager.server.CreateEntity(prefabsharkfin, sharkentity.transform.position, sharkentity.transform.rotation, true);
                sharkfront.GetComponent<BaseNetworkable>()._limitedNetworking = false;
                sharkfront.enableSaving = false;
                sharkfront.skinID = frontskinid;
                var sfstab = sharkfront.GetComponent<StabilityEntity>();
                if (sfstab) sfstab.grounded = true;
                sharkfront.Spawn();
                sharkfront.SetParent(sharkentity, true, false);

                sharkfin = GameManager.server.CreateEntity(prefabsharkfin, sharkentity.transform.position, sharkentity.transform.rotation, true);
                sharkfin.GetComponent<BaseNetworkable>()._limitedNetworking = false;
                sharkfin.enableSaving = false;
                sharkfin.skinID = finskinid;
                var sfinstab = sharkfront.GetComponent<StabilityEntity>();
                if (sfinstab) sfstab.grounded = true;
                sharkfin.Spawn();
                sharkfin.SetParent(sharkentity, true, false);
                sharkfin.transform.localPosition = new Vector3(0f, 0.8f, -1f);
                sharkfin.transform.localEulerAngles = new Vector3(0, 180, 90);

                sharkback = GameManager.server.CreateEntity(prefabsharkfin, sharkentity.transform.position, sharkentity.transform.rotation, true);
                sharkback.GetComponent<BaseNetworkable>()._limitedNetworking = false;
                sharkback.enableSaving = false;
                sharkback.skinID = backskinid;
                var sbstab = sharkfront.GetComponent<StabilityEntity>();
                if (sbstab) sfstab.grounded = true;
                sharkback.Spawn();
                sharkback.SetParent(sharkentity, true, false);
                sharkback.transform.localPosition = new Vector3(0f, 0f, -2.9f);
                sharkback.transform.localEulerAngles = new Vector3(0, 0, 0);
            }

            private void OnTriggerStay(Collider col)
            {
                if (col == null) return;
                var target = col.GetComponentInParent<BasePlayer>();
                if (SharksCanAttackPlayers && target != null && target.WaterFactor() > 0.25f && target.Health() > 0)
                {
                    sharkbait = target;
                    return;
                }
                var boat = col.GetComponentInParent<BaseBoat>();
                if (SharksCanAttackBoats && boat != null && boat.Health() > 0f)
                {
                    sharkbait = boat;
                    return;
                }
                var corpse = col.GetComponentInParent<BaseCorpse>();
                if (corpse != null)
                {
                    sharkbait = corpse;
                    return;
                }
            }

            void MovementSplash()
            {
                Vector3 currentpos = sharkentity.transform.position;
                if (currentpos.y >= -1f) Effect.server.Run("assets/content/vehicles/boats/effects/splashloop.prefab", sharkentity.transform.position + new Vector3(0f, -0.6f, 0f));
            }

            void NearSharkBait()
            {
                if (moveback || didattack) return;
                List<BaseEntity> nearplayer = new List<BaseEntity>();
                Vis.Entities<BaseEntity>(sharkentity.transform.position, SharkBiteRange, nearplayer);
                foreach (BaseEntity ply in nearplayer)
                {
                    var iscorpse = ply.GetComponent<BaseCorpse>();
                    if (iscorpse)
                    {
                        iscorpse.Hurt(SharkDamageToPlayer);
                        didattack = true;
                        return;
                    }
                    var isplayer = ply.GetComponent<BasePlayer>();
                    if (isplayer)
                    {
                        isplayer.Hurt(SharkDamageToPlayer);
                        Effect.server.Run("assets/bundled/prefabs/fx/headshot.prefab", isplayer.transform.position);
                        Effect.server.Run("assets/bundled/prefabs/fx/explosions/water_bomb.prefab", isplayer.transform.position);
                        didattack = true;
                        return;
                    }
                    var isboat = ply.GetComponent<BaseBoat>();
                    if (isboat)
                    {
                        Effect.server.Run("assets/bundled/prefabs/fx/impacts/blunt/wood/wood1.prefab", isboat.transform.position);
                        Effect.server.Run("assets/bundled/prefabs/fx/explosions/water_bomb.prefab", isboat.transform.position);
                        isboat.myRigidBody.AddRelativeTorque(Vector3.forward * 10f, ForceMode.VelocityChange);
                        isboat.Hurt(999f);
                        didattack = true;
                        return;
                    }
                }
                didattack = false;
                return;
            }

            public bool IsWaterDeepEnough(Vector3 position)
            {
                var waterdepth = (TerrainMeta.WaterMap.GetHeight(position) - TerrainMeta.HeightMap.GetHeight(position));
                if (waterdepth >= 3f) return true;
                return false;
            }

            Vector3 GetNewPos()
            {
                if (!IsWaterDeepEnough(sharkentity.transform.position) && !moveback)
                {
                    sharkbait = null;
                    didattack = false;
                    moveback = true;
                    return initialspawn;
                }
                if (moveback)
                {
                    sharkbait = null;
                    didattack = false;
                    var checkpos = sharkentity.transform.position;
                    if (checkpos == initialspawn) { moveback = false; return initialspawn; }
                    return initialspawn;
                }

                if (didattack)
                {
                    despawntimer = 0f;
                    if (attackcounter == 1)
                    {
                        attackpos = new Vector3(sharkentity.transform.position.x, sharkentity.transform.position.y, sharkentity.transform.position.z);
                        position = new Vector3(UnityEngine.Random.Range(-5000f, 5000f), sharkentity.transform.position.y, UnityEngine.Random.Range(-5000f, 5000f));
                    }
                    attackcounter = attackcounter + 1;
                    if (attackcounter >= 50 && attackcounter < 100) { position = attackpos; }
                    if (attackcounter >= 100) { attackcounter = 0; MovementRoll(); sharkbait = null; didattack = false; }
                    return position;
                }
                if (sharkbait != null)
                {
                    despawntimer = 0f;
                    position = sharkbait.transform.position;
                    float adjustedy = position.y;
                    if (adjustedy > -0.5f) adjustedy = -0.5f;
                    position = new Vector3(position.x, adjustedy, position.z);
                    return position;
                }

                _angle += 0.5f * Time.deltaTime;
                offset = new Vector3(Mathf.Sin(_angle), 0f, Mathf.Cos(_angle)) * 15f;
                if (reversemovment) offset = new Vector3(Mathf.Sin(-_angle), 0f, Mathf.Cos(-_angle)) * 15f;
                var currentpos = sharkentity.transform.position;
                if (currentpos.y < -0.5f) currentpos.y = currentpos.y + 0.1f;
                position = currentpos + offset;
                didattack = false;
                return position;
            }

            void FixedUpdate()
            {
                if (sharkback == null || sharkfin == null || sharkfront == null) { OnDestroy(); return; }
                if (!didattack || !sharkbait) despawntimer = despawntimer + 1f;
                if (despawntimer >= despawntimelimit) { OnDestroy(); return; }
                Vector3 currentpos = sharkentity.transform.position;
                if (counter == 3)
                {
                    if (currentpos.y >= -1f) MovementSplash();
                    if (!didattack) NearSharkBait();
                    counter = 0;
                }

                var newpos = GetNewPos();

                var targetDir = newpos - sharkentity.transform.position;
                Vector3 newDir = Vector3.RotateTowards(sharkentity.transform.forward, targetDir, speed * Time.deltaTime, 0.0F);

                sharkentity.transform.rotation = Quaternion.LookRotation(newDir);
                sharkentity.transform.position = Vector3.MoveTowards(transform.position, newpos, (speed) * Time.deltaTime);
                if (currentpos.y > -0.5f) currentpos.y = -0.5f;
                spawnlocation = new Vector3(currentpos.x, currentpos.y, currentpos.z);

                sharkentity.transform.hasChanged = true;
                sharkentity.SendNetworkUpdateImmediate();
                sharkentity.UpdateNetworkGroup();

                counter = counter + 1;
            }

            void OnDestroy()
            {
                if (sharkentity != null && !sharkentity.IsDestroyed) { sharkentity.Invoke("KillMessage", 0.1f); }
            }
        }

        #endregion

    }
}