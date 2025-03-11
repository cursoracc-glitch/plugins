using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Ext.ChaosNPC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("RustTanic", "k1lly0u", "2.0.10")]
    class RustTanic : RustPlugin, IChaosNPCPlugin
    {
        #region Fields
        private static RustTanic Instance;

        private static RaycastHit RaycastHit;

        private const string HELI_EXPLOSION = "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab";
        private const string C4_EXPLOSION = "assets/prefabs/tools/c4/effects/c4_explosion.prefab";
               
        private const string FLOATING_CRATE_PREFAB = "assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab";

        private const string ICEBERG_NAME = "iceberg";

        private const string PERMISSION_ADMIN = "rusttanic.admin";
        #endregion

        #region Oxide Hooks  
        private void OnServerInitialized()
        {
            Instance = this;

            permission.RegisterPermission(PERMISSION_ADMIN, this);

            WorldSize = Mathf.Clamp(TerrainMeta.Size.x * 1.5f, 1000f, 6000f);
            BottomLeft = new Vector3(-WorldSize, 0f, -WorldSize) * 0.5f;
            TopLeft = new Vector3(-WorldSize, 0f, WorldSize) * 0.5f;
            BottomRight = new Vector3(WorldSize, 0f, -WorldSize) * 0.5f;
            TopRight = new Vector3(WorldSize, 0f, WorldSize) * 0.5f;

            Iceberg.FindAll();  
            
            if (!Iceberg.IsValid)
            {
                Debug.LogError($"[RustTanic] - No valid icebergs were found on the map. Unable to run this event");
                return;
            }

            AutomationCycle();
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Notification.Spawned"] = "A <color=#ce422b>hijacked cargo ship</color> has been spotted off the coast and is on a collision course with an iceberg\n<color=#ce422b>ETA ~ {0}s</color>",
                ["Notification.Collided"] = "The <color=#ce422b>hijacked cargo ship</color> has collided with an icerberg and is sinking!\nGet to the crash site at <color=#ce422b>{0}</color> to secure the loot",
            }, this);
        }

        private void OnEntityEnter(TriggerParent triggerParent, BasePlayer player)
        {
            Titanic titanic = triggerParent?.GetComponentInParent<Titanic>();
            if (titanic != null && player != null)
                player.PauseFlyHackDetection(1.1f);
        }

        private void Unload()
        {
            Titanic[] t = UnityEngine.Object.FindObjectsOfType<Titanic>();
            for (int i = 0; i < t.Length; i++)
            {
                Titanic titanic = t[i];
                if (titanic && !titanic.IsDestroyed)
                    titanic.Kill(BaseNetworkable.DestroyMode.None);
            }
            
            Instance = null;
            Configuration = null;
        }
        #endregion

        #region Spawning
        private void AutomationCycle()
        {
            if (!Configuration.Automation.Enabled)
                return;

            timer.In(Mathf.Max(Configuration.Automation.RandomTime, 60), () =>
            {
                if (BasePlayer.activePlayerList.Count >= Configuration.Automation.RequiredPlayers)
                {
                    string failReason = null;

                    for (int i = 0; i < 3; i++)
                    {
                        if (SpawnCargoShip(Vector3.zero, out failReason))
                            goto TRIGGER_NEXT_CYCLE;
                    }

                    if (!string.IsNullOrEmpty(failReason))
                    {
                        Debug.Log($"[RustTanic] - Failed to run automated event : {failReason}");
                    }
                }

                TRIGGER_NEXT_CYCLE:
                AutomationCycle();
            });
        }

        private bool SpawnCargoShip(Vector3 nearestTo, out string failReason)
        {
            Iceberg iceberg = nearestTo == Vector3.zero ? Iceberg.GetRandom() : Iceberg.FindClosestIceberg(nearestTo);

            if (iceberg == null)
            {
                failReason = "Failed to find a iceberg that meets requirements";
                return false;
            }

            if (CalculateSpawnPosition2D(iceberg, out Vector3 intersection))
            {                
                Vector3 destination = iceberg.Transform.position.XZ3D();

                int attempts = 0;

                RECAST_RAY:
                if (Physics.Raycast(intersection, (destination - intersection).normalized, out RaycastHit, Vector3.Distance(destination, intersection), 1 << 23 | 1 << 29, QueryTriggerInteraction.Collide))
                {     
                    intersection = RotateAroundPoint(intersection, destination, 10f);
                    attempts++;

                    if (attempts > 3)
                    {
                        failReason = RaycastHit.collider is TerrainCollider ? "Terrain is blocking the path" : "Failed to get a clear path to the selected iceberg";
                        return false;
                    }

                    goto RECAST_RAY;
                }

                SpawnCargoShip(intersection, iceberg);

                failReason = string.Empty;
                return true;
            }

            failReason = "Failed to calculate an appropriate spawn position";
            return false;
        }

        private void SpawnCargoShip(Vector3 spawnPosition, Iceberg iceberg)
        {
            const string CARGOSHIP_PREFAB = "assets/content/vehicles/boats/cargoship/cargoshiptest.prefab";

            Debug.Log(spawnPosition);
            CargoShip cargoShip = GameManager.server.CreateEntity(CARGOSHIP_PREFAB, spawnPosition, Quaternion.identity) as CargoShip;

            Titanic titanic = cargoShip.gameObject.AddComponent<Titanic>();
            titanic.Iceberg = iceberg;

            CopySerializeableFields<CargoShip>(cargoShip, titanic);
            
            titanic.enableSaving = false;

            UnityEngine.Object.DestroyImmediate(cargoShip, true);

            titanic.Spawn();
        }

        private static void CopySerializeableFields<T>(T src, T dst)
        {
            FieldInfo[] srcFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in srcFields)
            {
                object value = field.GetValue(src);
                field.SetValue(dst, value);
            }
        }
        #endregion

        #region CustomNPC
        public bool InitializeStates(BaseAIBrain customNPCBrain) => false;

        public bool WantsToPopulateLoot(CustomScientistNPC customNpc, NPCPlayerCorpse npcplayerCorpse) => false;
        
        public byte[] GetCustomDesign() => null;
        #endregion

        #region Helpers
        private Vector3 RotateAroundPoint(Vector3 point, Vector3 origin, float degrees) => Quaternion.Euler(Vector3.up * degrees) * (point - origin) + origin;

        private static Quaternion RandomUpRotation() => Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        private static void ReleaseFreeableLootContainer(FreeableLootContainer freeableLootContainer)
        {
            if (freeableLootContainer.IsTiedDown())
            {
                freeableLootContainer.GetRB().isKinematic = false;
                freeableLootContainer.buoyancy.enabled = true;
                freeableLootContainer.buoyancy.buoyancyScale = 1f;
                freeableLootContainer.SetFlag(BaseEntity.Flags.Reserved8, false, false, true);
            }
        }

        private static void Broadcast(string key, params object[] args)
        {
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                if (args != null && args.Length > 0)
                    player.ChatMessage(string.Format(Instance.lang.GetMessage(key, Instance, player.UserIDString), args));
                else player.ChatMessage(Instance.lang.GetMessage(key, Instance, player.UserIDString));
            }
        }        
        #endregion

        #region Map Edge Spawn Point Calculation
        private float WorldSize;

        private Vector3 BottomLeft;
        private Vector3 TopLeft;
        private Vector3 BottomRight;
        private Vector3 TopRight;

        private bool CalculateSpawnPosition2D(Iceberg iceberg, out Vector3 intersection)
        {
            Vector3 targetPosition = iceberg.Transform.position.XZ3D();

            Vector3 direction = targetPosition.normalized;

            Vector3 projectedPosition = targetPosition + (direction * WorldSize);

            if (IntersectsLine(targetPosition, projectedPosition, TopLeft, BottomLeft, out intersection))
                return true;

            if (IntersectsLine(targetPosition, projectedPosition, BottomLeft, BottomRight, out intersection))
                return true;

            if (IntersectsLine(targetPosition, projectedPosition, BottomRight, TopRight, out intersection))
                return true;

            if (IntersectsLine(targetPosition, projectedPosition, TopRight, TopLeft, out intersection))
                return true;

            return false;
        }

        private bool IntersectsLine(Vector3 from, Vector3 to, Vector3 lineStart, Vector3 lineEnd, out Vector3 intersection)
        {
            intersection = Vector3.zero;

            float d = (to.x - from.x) * (lineEnd.z - lineStart.z) - (to.z - from.z) * (lineEnd.x - lineStart.x);

            if (d == 0f)            
                return false;

            float u = ((lineStart.x - from.x) * (lineEnd.z - lineStart.z) - (lineStart.z - from.z) * (lineEnd.x - lineStart.x)) / d;
            float v = ((lineStart.x - from.x) * (to.z - from.z) - (lineStart.z - from.z) * (to.x - from.x)) / d;

            if (u < 0f || u > 1f || v < 0f || v > 1f)            
                return false;
            
            intersection.x = from.x + u * (to.x - from.x);
            intersection.z = from.z + u * (to.z - from.z);

            return true;
        }    
        #endregion

        #region Icebergs       
        private partial class Iceberg
        {
            public Transform Transform { get; private set; }

            public Collider Collider { get; private set; }

            public bool HasBuildingBlocks { get; private set; }

            public bool HasToolCupboard { get; private set; }


            private float validationExpireTime;


            public Iceberg(Transform transform)
            {
                Transform = transform;
                Collider = transform.GetComponent<Collider>();

                Validate();
            }

            public void Validate()
            {
                validationExpireTime = Time.time + 1800;

                HasBuildingBlocks = false;
                HasToolCupboard = false;

                int hits = Physics.BoxCastNonAlloc(Collider.bounds.center, Collider.bounds.extents, Transform.up, buffer, Transform.rotation, 1f, 1 << (int)Rust.Layer.Construction);
                for (int i = 0; i < hits; i++)
                {
                    RaycastHit raycastHit = buffer[i];

                    BaseEntity baseEntity = raycastHit.GetEntity();
                    if (baseEntity != null)
                    {
                        if (baseEntity is BuildingBlock)
                        {
                            HasBuildingBlocks = true;

                            if ((baseEntity as BuildingBlock).GetBuildingPrivilege() != null)
                            {
                                HasToolCupboard = true;
                                break;
                            }
                        }
                    }
                }                
            }
        }

        private partial class Iceberg
        {            
            private static readonly RaycastHit[] buffer = new RaycastHit[512];

            private static readonly List<Iceberg> icebergs = new List<Iceberg>();

            public static bool IsValid => icebergs.Count > 0;

            public static void FindAll()
            {
                icebergs.Clear();

                Transform root = HierarchyUtil.GetRoot("Decor").transform;

                const string ICEBERG_PATH = "assets/bundled/prefabs/autospawn/decor/iceberg/";

                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);

                    if (child.name.StartsWith(ICEBERG_PATH))
                    {
                        for (int y = 0; y < child.childCount; y++)
                        {
                            Transform c = child.GetChild(y);
                            if (c.name.StartsWith(ICEBERG_NAME))
                            {
                                Iceberg iceberg = new Iceberg(c);
                                icebergs.Add(iceberg);
                            }
                        }
                    }
                }

                Debug.Log($"[RustTanic] Found {icebergs.Count} Icebergs");
            }

            public static Iceberg FindClosestIceberg(Vector3 position)
            {
                Iceberg closest = null;
                float distance = float.MaxValue;

                for (int i = 0; i < icebergs.Count; i++)
                {
                    Iceberg iceberg = icebergs[i];

                    if (iceberg.validationExpireTime < Time.time)
                        iceberg.Validate();

                    if ((Configuration.Iceberg.DisableWithBases && iceberg.HasBuildingBlocks) || (Configuration.Iceberg.DisableWithCupboards && iceberg.HasToolCupboard))
                        continue;

                    float d = Vector3.Distance(position, iceberg.Transform.position);
                    if (d < distance)
                    {
                        closest = iceberg;
                        distance = d;
                    }
                }

                return closest;
            }

            public static Iceberg GetRandom()
            {
                Iceberg closest = null;

                List<Iceberg> list = Facepunch.Pool.Get<List<Iceberg>>();
                list.AddRange(icebergs);

                for (int i = 0; i < icebergs.Count; i++)
                {
                    Iceberg iceberg = list.GetRandom();

                    if (iceberg.validationExpireTime < Time.time)
                        iceberg.Validate();

                    if ((Configuration.Iceberg.DisableWithBases && iceberg.HasBuildingBlocks) || (Configuration.Iceberg.DisableWithCupboards && iceberg.HasToolCupboard))
                        continue;

                    closest = iceberg;
                    break;
                }

                Facepunch.Pool.FreeUnmanaged(ref list);
                return closest;
            }
        }
        #endregion

        #region Component
        private class Titanic : CargoShip
        {
            private Transform Transform;

            internal Iceberg Iceberg;

            private Vector3 destination;

            private bool isSinking;

            private float timeToTake;
            private float timeTaken;

            private SinkingPhase Phase = SinkingPhase.Phase1;

            private SinkPhase Phase0;
            private SinkPhase Phase1;
            private SinkPhase Phase2;
            private SinkPhase Phase3;

            private bool isWithinProximity;

            private const float HALF_LENGTH = 80f;

            private enum SinkingPhase { Phase1, Phase2, Phase3 }

            public override void ServerInit()
            {
                Transform = transform;

                serverEntities.RegisterID(this);
                if (net != null)                
                    net.handler = this;
                                
                if (flags != 0)                
                    OnFlagsChanged(0, flags);
                
                if (syncPosition && PositionTickRate >= 0f)
                {
                    if (!PositionTickFixedTime)                    
                        InvokeRandomized(NetworkPositionTick, PositionTickRate, PositionTickRate - PositionTickRate * 0.05f, PositionTickRate * 0.05f);                    
                    else InvokeRepeatingFixedTime(NetworkPositionTick);
                }

                IcebergDetectionTrigger.Create(this);

                Query.Server.Add(this);
               
                InvokeRepeating(BuildingCheck, 1f, 5f);

                Invoke(SpawnInitialLoot, 10f);

                float waterlineOffset = WaterLevel.GetWaterSurface(Transform.position, false, false) - Transform.InverseTransformPoint(waterLine.transform.position).z;

                destination = Iceberg.Transform.position;
                destination.y = waterlineOffset;

                Transform.position = new Vector3(Transform.position.x, waterlineOffset, Transform.position.z);
                Transform.rotation = Quaternion.LookRotation(Vector3Ex.Direction2D(destination, Transform.position), Vector3.up);

                Debug.Log($"[RustTanic] - Spawned ship at {Transform.position} with destination {destination}");

                Broadcast("Notification.Spawned", Mathf.RoundToInt((Vector3.Distance(Transform.position, destination) - HALF_LENGTH) / Configuration.Boat.Speed));
               
                CreateMapMarker();
            }

            public override void DestroyShared()
            {
                if (explosionMarker != null && !explosionMarker.IsDestroyed)
                    explosionMarker.Kill();

                base.DestroyShared();
            }

            private new void FixedUpdate()
            {
                if (isSinking)
                {
                    timeTaken += Time.fixedDeltaTime;

                    if (timeTaken >= timeToTake)
                    {
                        if (Phase < SinkingPhase.Phase3)
                        {
                            Phase = (SinkingPhase)((int)Phase + 1);
                            timeTaken = 0;

                            if (Phase == SinkingPhase.Phase2)
                            {
                                timeToTake = Configuration.Boat.SinkTime2;

                                PlayHorn();
                                ExecuteSmallExplosion();
                            }

                            if (Phase == SinkingPhase.Phase3)
                            {
                                timeToTake = Configuration.Boat.SinkTime3;
                                base.SetFlag(Flags.Reserved8, false);
                            }
                        }
                        else 
                        {
                            CreateGibs();

                            enabled = false;

                            ReleaseLootContainers();

                            Invoke(DelayedDestroy, Configuration.Boat.DespawnTime);

                            CancelInvoke(PauseFlyhackLoop);
                            return;
                        }
                    }


                    float delta = Mathf.InverseLerp(0, timeToTake, timeTaken);

                    SinkPhase from = Phase == SinkingPhase.Phase1 ? Phase0 : Phase == SinkingPhase.Phase2 ? Phase1 : Phase2;
                    SinkPhase to = Phase == SinkingPhase.Phase1 ? Phase1 : Phase == SinkingPhase.Phase2 ? Phase2 : Phase3;

                    Transform.position = Vector3.Lerp(from.position, to.position, delta);
                    Transform.localEulerAngles = Vector3.Lerp(from.euler, to.euler, delta);                    
                }
                else
                {
                    Vector3 direction = (destination - Transform.position).normalized;

                    float dotForward = Vector3.Dot(transform.forward, direction);

                    float targetSpeedDelta = Mathf.InverseLerp(0f, 1f, dotForward);

                    currentThrottle = Mathf.Lerp(currentThrottle, targetSpeedDelta, Time.deltaTime * 0.2f);
                    currentVelocity = Transform.forward * (Configuration.Boat.Speed * currentThrottle);
                    Transform.position += currentVelocity * Time.deltaTime;

                    if (!isWithinProximity && Vector3.Distance(Transform.position, Iceberg.Transform.position) < Configuration.Boat.Effects.ProximityDistance + HALF_LENGTH)
                        ProximityTriggerEvents();
                }
            }

            #region Sinking
            private static readonly Transform[] EMPTY_SCIENTIST_SPAWNS = new Transform[0];

            private BaseEntity explosionMarker;

            private void ProximityTriggerEvents()
            {
                isWithinProximity = true;

                if (Configuration.Boat.Effects.AlarmProximity)
                    base.SetFlag(Flags.Reserved8, true);

                if (Configuration.Boat.Effects.NapalmLaunchers)
                    ServerMgr.Instance.StartCoroutine(ExecuteNapalmLaunchers());

                PlayHorn();
            }

            internal void OnStruckIceberg()
            {
                Broadcast("Notification.Collided", PhoneController.PositionToGridCoord(Iceberg.Transform.position));

                CreateExplosionMarker();

                StartSinking();

                PauseFlyhackLoop();

                ServerMgr.Instance.StartCoroutine(ProcessChildren());

                ServerMgr.Instance.StartCoroutine(SpawnLoot());
                ServerMgr.Instance.StartCoroutine(SpawnNPCs());

                if (Configuration.Boat.Effects.LargeExplosion)
                    ServerMgr.Instance.StartCoroutine(ExecuteLargeExplosions());
                else ExecuteSmallExplosion();
            }

            private void StartSinking()
            {
                SetupSinkingPhases();
               
                isSinking = true;
                timeToTake = Configuration.Boat.SinkTime1;

                CancelInvoke(PlayHorn);

                scientistSpawnPoints = EMPTY_SCIENTIST_SPAWNS;
            }

            private void SetupSinkingPhases()
            {
                Phase0 = new SinkPhase
                {
                    position = Transform.position,
                    euler = Transform.localEulerAngles
                };

                Phase1 = new SinkPhase
                {
                    position = Transform.position - (Vector3.up * 2),
                    euler = Transform.localEulerAngles + new Vector3(5f, 0f, 0f)
                };

                Phase2 = new SinkPhase
                {
                    position = Transform.position - (Vector3.up * 10),
                    euler = Transform.localEulerAngles + new Vector3(10f, 0f, 15f)
                };

                Vector3 finalPosition = Transform.position;
                finalPosition.y = TerrainMeta.HeightMap.GetHeight(finalPosition) + 15f;
                Phase3 = new SinkPhase
                {
                    position = finalPosition,
                    euler = new Vector3(0f, 0f, 90f)
                };
            }

            private void CreateExplosionMarker()
            {
                const string MARKER_PREFAB = "assets/prefabs/tools/map/explosionmarker.prefab";

                explosionMarker = GameManager.server.CreateEntity(MARKER_PREFAB, Transform.position);
                explosionMarker.enableSaving = false;
                explosionMarker.Spawn();
            }

            private IEnumerator ProcessChildren()
            {
                TriggerParent parent = GetComponentInChildren<TriggerParent>();
                parent.interestLayers.value = 1 << (int)Rust.Layer.Player_Server;
                parent.ParentNPCPlayers = false;

                for (int i = children.Count - 1; i >= 0; i--)
                {
                    BaseEntity entity = children[i];

                    if (entity is LootContainer && (entity as LootContainer).inventory != null)                    
                        ProcessParentedLootContainer(entity as LootContainer);                        
                    
                    else if (entity is BasePlayer && (entity as BasePlayer).IsNpc)
                        ProcessParentedNPC(entity as BasePlayer);

                    yield return null;
                }
            }

            private void PauseFlyhackLoop()
            {
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    BasePlayer player = children[i] as BasePlayer;
                    if (!player || player.IsNpc)
                        continue;

                    player.PauseFlyHackDetection(1.1f);
                }

                Invoke(PauseFlyhackLoop, 1f);
            }

            private struct SinkPhase
            {
                public Vector3 position;
                public Vector3 euler;
            }
            #endregion

            #region Effects           
            private void ExecuteSmallExplosion()
            {
                InvokedEffect(HELI_EXPLOSION, new Vector3(0f, 7f, 78f), 0.1f);

                InvokedEffect(C4_EXPLOSION, new Vector3(9.8f, 6.5f, -20.9f), Random.Range(0.2f, 0.8f));

                InvokedEffect(HELI_EXPLOSION, new Vector3(0.0f, 9.7f, 61.7f), Random.Range(0.9f, 1.2f));

                InvokedEffect(C4_EXPLOSION, new Vector3(-9.6f, 6.5f, 41.8f), Random.Range(1.3f, 1.8f));

                InvokedEffect(HELI_EXPLOSION, new Vector3(0.0f, 9.7f, 61.7f), Random.Range(1.9f, 2.2f));
            }

            private IEnumerator ExecuteLargeExplosions()
            { 
                const string FIREBALL_PREFAB = "assets/bundled/prefabs/oilfireballsmall.prefab";

                foreach (Vector3 position in DECK_EXPLOSION_POSITIONS)
                {
                    Vector3 worldPosition = Transform.TransformPoint(position);

                    float r = Random.value;

                    Effect.server.Run(r < 0.5f ? HELI_EXPLOSION : C4_EXPLOSION, worldPosition);

                    for (int i = 0; i < 2; i++)
                    {
                        if (Random.Range(0f, 1f) > 0.4f)
                        {
                            CreateAndLaunchFireball(FIREBALL_PREFAB, worldPosition);
                            yield return CoroutineEx.waitForSeconds(0.1f);
                        }
                    }

                    yield return CoroutineEx.waitForSeconds(0.1f);
                }
            }

            private IEnumerator ExecuteNapalmLaunchers()
            {
                const string NAPALM_PREFAB = "assets/bundled/prefabs/napalm.prefab";
                
                float waterMapHeight = WaterLevel.GetWaterSurface(Transform.TransformPoint(-2.5f, 32.4f, -53.8f), false, false);

                while (true)
                {
                    Vector3 position = Transform.TransformPoint(-2.5f, 32.4f, -53.8f);

                    if (position.y <= waterMapHeight)
                        yield break;

                    CreateAndLaunchFireball(NAPALM_PREFAB, position);

                    yield return new WaitForSeconds(Random.Range(0.4f, 0.8f));
                }
            }
                       
            private void CreateAndLaunchFireball(string prefab, Vector3 position)
            {
                BaseEntity baseEntity = GameManager.server.CreateEntity(prefab, position);
                baseEntity.enableSaving = false;
                baseEntity.Spawn();

                baseEntity.GetComponent<Rigidbody>().AddForce(Random.Range(-4, 4), Random.Range(3, 6), Random.Range(-4, 4), ForceMode.Impulse);
            }

            private void InvokedEffect(string effect, Vector3 localOffset, float time) => Invoke(() => Effect.server.Run(effect, this, 0, localOffset, Vector3.up, null, true), time);

            private static readonly List<Vector3> DECK_EXPLOSION_POSITIONS = new List<Vector3> 
            {
                new Vector3(-1.9f, 9.5f, 75f),
                new Vector3(3.6f, 9.5f, 67.4f),
                new Vector3(-5.8f, 9.5f, 68.7f),
                new Vector3(-1.2f, 10.8f, 59.7f),
                new Vector3(7.4f, 9.5f, 58.3f),
                new Vector3(-1.2f, 9.5f, 56.7f),
                new Vector3(-7.4f, 9.5f, 58.5f),
                new Vector3(-1.7f, 9.5f, 46.4f),
                new Vector3(-9.3f, 6.5f, 49.7f),
                new Vector3(7.0f, 6.5f, 50.4f),
                new Vector3(2.1f, 9.5f, 32.7f),
                new Vector3(-9.9f, 6.5f, 24.8f),
                new Vector3(-1.7f, 9.5f, 24.4f),
                new Vector3(5.8f, 6.5f, 22.8f),
                new Vector3(-0.9f, 9.5f, 16.5f),
                new Vector3(-0.4f, 9.5f, 4.8f),
                new Vector3(6.1f, 6.5f, -1.7f),
                new Vector3(-8.6f, 6.5f, -3.0f),
                new Vector3(-2.6f, 9.5f, -9.6f),
                new Vector3(-2.9f, 9.5f, -20.9f),
                new Vector3(-1.6f, 9.5f, -27.2f),
                new Vector3(-8.6f, 6.5f, -28.4f),
                new Vector3(5.6f, 6.5f, -28.3f),
                new Vector3(4.4f, 9.5f, -34.9f),
                new Vector3(-6.9f, 9.5f, -34.7f),
                new Vector3(5.5f, 9.5f, -55.3f),
                new Vector3(-10.3f, 9.8f, -54.5f),
                new Vector3(-9.0f, 12.1f, -49.6f),
                new Vector3(6.2f, 12.5f, -49.6f),
                new Vector3(8.7f, 24.5f, -45.5f),
                new Vector3(-12.0f, 25.6f, -45.2f),
                new Vector3(-7.5f, 27.5f, -42.8f),
                new Vector3(3.8f, 27.5f, -44.2f),
                new Vector3(-1.2f, 31.2f, -45.4f),
                new Vector3(-1.5f, 36.5f, -45.6f)
            };
            #endregion

            #region Loot
            private List<LootContainer> shipLootContainers;

            private void SpawnInitialLoot()
            {
                InvokeRepeating(PlayHorn, 0f, 8f);

                if (Configuration.Loot.ShipLoot == ConfigData.LootSettings.ShipLootMode.Despawn)
                    return;

                SpawnCrate(lockedCratePrefab.resourcePath);
                SpawnCrate(eliteCratePrefab.resourcePath);

                for (int i = 0; i < 4; i++)
                    SpawnCrate(militaryCratePrefab.resourcePath);

                for (int j = 0; j < 4; j++)
                    SpawnCrate(junkCratePrefab.resourcePath);
            }
                        
            private void ProcessParentedLootContainer(LootContainer lootContainer)
            {
                if (Configuration.Loot.ShipLoot == ConfigData.LootSettings.ShipLootMode.FloatToSurface)
                {
                    if (shipLootContainers == null)
                        shipLootContainers = Pool.Get<List<LootContainer>>();

                    shipLootContainers.Add(lootContainer);
                }                
            }

            private void ReleaseLootContainers()
            {
                if (shipLootContainers == null || shipLootContainers.Count == 0)
                    return;

                for (int i = 0; i < shipLootContainers.Count; i++)
                {
                    LootContainer lootContainer = shipLootContainers[i];
                    if (!lootContainer || lootContainer.IsDestroyed)
                        continue;
                    
                    Vector3 position = lootContainer.transform.position;
                    position.x += Random.Range(-10f, 10f);
                    position.y = WaterLevel.GetWaterSurface(lootContainer.transform.position, false, false) - 1f;
                    position.z += Random.Range(-10f, 10f);
                    
                    FreeableLootContainer freeableLootContainer = GameManager.server.CreateEntity(FLOATING_CRATE_PREFAB, position, lootContainer.transform.rotation, true) as FreeableLootContainer;

                    if (freeableLootContainer)
                    {
                        freeableLootContainer.enableSaving = false;
                        freeableLootContainer.inventorySlots = lootContainer.inventory.itemList.Count;
                        freeableLootContainer.initialLootSpawn = false;
                        freeableLootContainer.Spawn();

                        for (int y = lootContainer.inventory.itemList.Count - 1; y >= 0; y--)
                        {
                            Item item = lootContainer.inventory.itemList[y];
                            if (!item.MoveToContainer(freeableLootContainer.inventory, -1, true, false))
                                item.Remove();
                        }

                        freeableLootContainer.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                        freeableLootContainer.Invoke(freeableLootContainer.RemoveMe, 1800f);
                    }

                    lootContainer.inventory.Clear();
                    lootContainer.Kill(DestroyMode.None);
                }

                Pool.FreeUnmanaged(ref shipLootContainers);
            }

            private IEnumerator SpawnLoot()
            {
                if (Configuration.Loot.Floating.Enabled)                
                    yield return ServerMgr.Instance.StartCoroutine(Configuration.Loot.Floating.SpawnInWater(Transform.position, 40f));
                
                if (Configuration.Loot.Iceberg.Enabled)
                    yield return ServerMgr.Instance.StartCoroutine(Configuration.Loot.Iceberg.SpawnOnIceberg(Iceberg));
            }

            private void CreateGibs()
            {
                if (Configuration.Loot.GibsToSpawn <= 0)
                    return;

                const string GIB_RESOURCE_PATH = "assets/prefabs/npc/m2bradley/servergibs_bradley.prefab";

                GameObject gibSource = GameManager.server.FindPrefab(GIB_RESOURCE_PATH).GetComponent<ServerGib>()._gibSource;

                for (int i = 0; i < Configuration.Loot.GibsToSpawn; i++)
                {
                    Vector3 position = Transform.position;
                    position.x += Random.Range(-40, 40);
                    position.z += Random.Range(-40, 40);

                    CreateGibs(GIB_RESOURCE_PATH, gibSource, position, Configuration.Loot.GibsSpread);
                }
            }

            private void CreateGibs(string resourcePath, GameObject source, Vector3 position, float spread)
            {
                List<ServerGib> serverGibs = Pool.Get<List<ServerGib>>();

                foreach (MeshRenderer meshRenderer in source.GetComponentsInChildren<MeshRenderer>(true))
                {
                    MeshFilter component = meshRenderer.GetComponent<MeshFilter>();                    
                    BaseEntity baseEntity = GameManager.server.CreateEntity(resourcePath, position, meshRenderer.transform.localRotation, true);
                    if (baseEntity)
                    {
                        ServerGib serverGib = baseEntity.GetComponent<ServerGib>();                        
                        serverGib._gibName = meshRenderer.name;

                        MeshCollider component3 = meshRenderer.GetComponent<MeshCollider>();
                        Mesh physicsMesh = (component3 != null) ? component3.sharedMesh : component.sharedMesh;
                        serverGib.PhysicsInit(physicsMesh);

                        Vector3 velocity = meshRenderer.transform.localPosition.normalized * spread;

                        Rigidbody rb = serverGib.GetComponent<Rigidbody>();
                        rb.velocity = velocity;
                        rb.angularVelocity = Vector3Ex.Range(-1f, 1f).normalized * 1f;
                        rb.WakeUp();

                        serverGib.Spawn();
                        serverGibs.Add(serverGib);
                    }
                }

                foreach (ServerGib serverGib in serverGibs)
                {
                    foreach (ServerGib serverGib2 in serverGibs)
                    {
                        if (!(serverGib == serverGib2))
                        {
                            Physics.IgnoreCollision(serverGib2.GetCollider(), serverGib.GetCollider(), true);
                        }
                    }
                }
                Pool.FreeUnmanaged(ref serverGibs);
            }
            #endregion

            #region NPCs 
            private static readonly LootContainer.LootSpawnSlot[] EMPTY_LOOT_SPAWNS = new LootContainer.LootSpawnSlot[0];

            private void ProcessParentedNPC(BasePlayer player)
            {
                if (!Configuration.NPC.ShipNPCs.ShipNPCDropLoot)
                    (player as ScientistNPC).LootSpawnSlots = EMPTY_LOOT_SPAWNS;

                if (Configuration.NPC.ShipNPCs.Kill == ConfigData.NPCSettings.ShipNPCSettings.KillMode.KillInstantly)
                {
                    if (Configuration.NPC.ShipNPCs.Corpse == ConfigData.NPCSettings.ShipNPCSettings.CorpseMode.DropCorpse)                    
                        player.Die(new HitInfo(player, player, Rust.DamageType.Drowned, 1000f));                    
                    else
                    {
                        player.EnableSaving(false);
                        player.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }
                else player.gameObject.AddComponent<DrownWhenUnderWater>();
            }

            private IEnumerator SpawnNPCs()
            {
                yield return CoroutineEx.waitForSeconds(5f);
                Vector3 position = Iceberg.Collider.bounds.center + (Vector3.up * Iceberg.Collider.bounds.extents.y);

                for (int i = 0; i < Configuration.NPC.AmountToSpawn; i++)
                {
                    ChaosNPC.SpawnNPC(Instance, position, Configuration.NPC.Settings);

                    yield return CoroutineEx.waitForSeconds(0.5f);
                }
            }
            #endregion
        }

        private class IcebergDetectionTrigger : MonoBehaviour
        {
            private Titanic titanic;

            private BoxCollider boxCollider;

            private void Awake()
            {
                gameObject.name = "CollisionTrigger";
                gameObject.transform.localRotation = Quaternion.Euler(24f, 0f, 0f);
                gameObject.layer = (int)Rust.Layer.Reserved1;

                Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = new Vector3(0f, 33f, 66f);
                boxCollider.size = new Vector3(10f, 10f, 5);
                boxCollider.isTrigger = true;
            }

            private void OnTriggerEnter(Collider col)
            {
                if (col.gameObject.name.Contains(ICEBERG_NAME))
                {
                    titanic.OnStruckIceberg();
                    Destroy(gameObject);
                }
            }

            internal static void Create(Titanic titanic) => titanic.gameObject.CreateChild().AddComponent<IcebergDetectionTrigger>().titanic = titanic;
        }

        private class DrownWhenUnderWater : MonoBehaviour
        {
            private BasePlayer player;

            private void Awake()
            {
                enabled = false;
                player = GetComponent<BasePlayer>();
                InvokeHandler.Invoke(player, CheckWaterLevel, Random.Range(0.2f, 1f));
            }

            private void CheckWaterLevel()
            {
                if (player.eyes.position.y < 0)
                {
                    if (Configuration.NPC.ShipNPCs.Corpse == ConfigData.NPCSettings.ShipNPCSettings.CorpseMode.DropCorpse)                    
                        player.Die(new HitInfo(player, player, Rust.DamageType.Drowned, 1000f));                    
                    else
                    {
                        player.EnableSaving(false);
                        player.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }
                else InvokeHandler.Invoke(player, CheckWaterLevel, Random.Range(0.2f, 1f));
            }
        }
        #endregion

        #region Commands
        [ChatCommand("titanic")]
        private void SpawnTitanicCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PERMISSION_ADMIN))
            {
                player.ChatMessage("You do not have permission to run this command");
                return;
            }

            if (!Iceberg.IsValid)
            {
                player.ChatMessage("No valid icebergs were found on the map. Unable to run this event");
                return;
            }

            if (args.Length < 1)
            {
                player.ChatMessage("/titanic random - Spawn a titanic cargo ship targeting a random iceberg");
                player.ChatMessage("/titanic closest - Spawn a titanic cargo ship targeting the nearest iceberg to you");
                return;
            }

            string failed;
            switch (args[0].ToLower())
            {
                case "random":

                    if (!SpawnCargoShip(Vector3.zero, out failed))
                        player.ChatMessage($"Failed to run event : {failed}");
                    else player.ChatMessage("Event triggered successfully");
                    break;

                case "closest":

                    if (!SpawnCargoShip(player.transform.position, out failed))
                        player.ChatMessage($"Failed to run event : {failed}");
                    else player.ChatMessage("Event triggered successfully");
                    break;

                default:
                    player.ChatMessage("Invalid syntax. Type /titanic to view available commands");
                    break;
            }
        }

        [ConsoleCommand("titanic")]
        private void SpawnTitanicConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                if (!permission.UserHasPermission(arg.Connection.userid.ToString(), PERMISSION_ADMIN))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (!Iceberg.IsValid)
            {
                SendReply(arg, "No valid icebergs were found on the map. Unable to run this event");
                return;
            }

            if (!SpawnCargoShip(Vector3.zero, out string failed))
                SendReply(arg, $"Failed to run event : {failed}");
            else SendReply(arg, "Event triggered successfully");
        }
        #endregion

        #region Config        
        private static ConfigData Configuration;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Event Automation Settings")]
            public AutomationSettings Automation { get; set; }

            [JsonProperty(PropertyName = "Iceberg Selection")]
            public IcebergSelection Iceberg { get; set; }

            [JsonProperty(PropertyName = "Boat Settings")]
            public BoatSettings Boat { get; set; }

            [JsonProperty(PropertyName = "NPC Settings")]
            public NPCSettings NPC { get; set; }

            [JsonProperty(PropertyName = "Loot Options")]
            public LootSettings Loot { get; set; }

            public class AutomationSettings
            {
                [JsonProperty(PropertyName = "Enable event automation")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Minimum time between events (seconds)")]
                public int Minimum { get; set; }

                [JsonProperty(PropertyName = "Maximum time between events (seconds)")]
                public int Maximum { get; set; }
                
                [JsonProperty(PropertyName = "Minimum required players to trigger the event")]
                public int RequiredPlayers { get; set; }

                [JsonIgnore]
                public int RandomTime => Random.Range(Minimum, Maximum);
            }

            public class BoatSettings
            {
                [JsonProperty(PropertyName = "Movement speed")]
                public float Speed { get; set; }

                [JsonProperty(PropertyName = "Amount of time it takes to sink - Phase 1 (seconds)")]
                public float SinkTime1 { get; set; }

                [JsonProperty(PropertyName = "Amount of time it takes to sink - Phase 2 (seconds)")]
                public float SinkTime2 { get; set; }

                [JsonProperty(PropertyName = "Amount of time it takes to sink - Phase 3 (seconds)")]
                public float SinkTime3 { get; set; }

                [JsonProperty(PropertyName = "Cargo ship despawn time once sunk (seconds)")]
                public int DespawnTime { get; set; }

                [JsonProperty(PropertyName = "Effect Settings")]
                public EffectSettings Effects { get; set; }
                
                public class EffectSettings
                {
                    [JsonProperty(PropertyName = "Large explosion enabled")]
                    public bool LargeExplosion { get; set; }

                    [JsonProperty(PropertyName = "Napalm launchers enabled")]
                    public bool NapalmLaunchers { get; set; }

                    [JsonProperty(PropertyName = "Alarm sound on proximity enabled")]
                    public bool AlarmProximity { get; set; }

                    [JsonProperty(PropertyName = "Proximity distance to trigger effects")]
                    public float ProximityDistance { get; set; }
                }
            }

            public class IcebergSelection
            {
                [JsonProperty(PropertyName = "Don't target icebergs that have player construction on them")]
                public bool DisableWithBases { get; set; }

                [JsonProperty(PropertyName = "Don't target icebergs that have tool cupboards on them")]
                public bool DisableWithCupboards { get; set; }
            }

            public class NPCSettings
            {
                [JsonProperty(PropertyName = "Cargo Ship NPC Options")]
                public ShipNPCSettings ShipNPCs { get; set; }

                [JsonProperty(PropertyName = "Amount of NPCs to spawn on the iceberg")]
                public int AmountToSpawn { get; set; }

                [JsonProperty(PropertyName = "NPC Options")]
                public Oxide.Ext.ChaosNPC.NPCSettings Settings { get; set; }               

                public class ShipNPCSettings
                {                    
                    [JsonProperty(PropertyName = "Cargo Ship NPCs drop loot")]
                    public bool ShipNPCDropLoot { get; set; }

                    [JsonProperty(PropertyName = "Cargo Ship NPC kill mode ( KillInstantly, DieByDrowning )")]
                    public KillMode Kill { get; set; }

                    [JsonProperty(PropertyName = "Cargo Ship corpse mode ( NoCorpse, DropCorpse )")]
                    public CorpseMode Corpse { get; set; }

                    [JsonConverter(typeof(StringEnumConverter))]
                    public enum KillMode { KillInstantly, DieByDrowning }

                    [JsonConverter(typeof(StringEnumConverter))]
                    public enum CorpseMode { NoCorpse, DropCorpse }                    
                }
            }

            public class LootSettings
            {
                [JsonProperty(PropertyName = "Cargo ship loot mode ( Despawn, LeaveOnShip, FloatToSurface )")]
                public ShipLootMode ShipLoot { get; set; }

                [JsonProperty(PropertyName = "Amount of mine-able debris to spawn")]
                public int GibsToSpawn { get; set; }

                [JsonProperty(PropertyName = "Debris spread amount")]
                public float GibsSpread { get; set; }

                [JsonProperty(PropertyName = "Iceberg loot crates")]
                public LootTable Iceberg { get; set; }

                [JsonProperty(PropertyName = "Floating loot crates")]
                public LootTable Floating { get; set; }

                public class LootTable
                {
                    [JsonProperty(PropertyName = "Drop loot crates")]
                    public bool Enabled { get; set; }

                    [JsonProperty(PropertyName = "Amount of crates to spawn")]
                    public int Amount { get; set; }

                    [JsonProperty(PropertyName = "Use custom loot table")]
                    public bool UseCustomLoot { get; set; }

                    [JsonProperty(PropertyName = "Minimum number of items per crate")]
                    public int Minimum { get; set; }

                    [JsonProperty(PropertyName = "Maximum number of items per crate")]
                    public int Maximum { get; set; }

                    [JsonProperty(PropertyName = "Despawn time (seconds)")]
                    public int DespawnTime { get; set; }

                    public List<LootItem> Table { get; set; }

                    public class LootItem
                    {
                        public string Shortname { get; set; }

                        public int Minimum { get; set; }

                        public int Maximum { get; set; }

                        public ulong Skin { get; set; }

                        [JsonProperty(PropertyName = "Minimum condition (0.0 - 1.0)")]
                        public float MinCondition { get; set; } = 1f;

                        [JsonProperty(PropertyName = "Maximum condition (0.0 - 1.0)")]
                        public float MaxCondition { get; set; } = 1f;

                        public float Probability { get; set; }

                        public LootItem() { }

                        public LootItem(string shortname, int minimum, int maximum, float probability)
                        {
                            Shortname = shortname;
                            Minimum = minimum;
                            Maximum = maximum;
                            Probability = probability;
                            MinCondition = MaxCondition = 1f;
                        }

                        private int GetAmount()
                        {
                            if (Maximum <= 0f || Maximum <= Minimum)
                                return Minimum;

                            return Random.Range(Minimum, Maximum);
                        }

                        public void Create(ItemContainer container)
                        {
                            Item item = ItemManager.CreateByName(Shortname, GetAmount(), Skin);
                            if (item != null)
                            {
                                item.conditionNormalized = Random.Range(Mathf.Clamp01(MinCondition), Mathf.Clamp01(MaxCondition));

                                item.OnVirginSpawn();

                                if (!item.MoveToContainer(container, -1, true))
                                    item.Remove(0f);
                            }
                        }
                    }

                    public IEnumerator SpawnInWater(Vector3 position, float radius)
                    {
                        for (int i = 0; i < Amount; i++)
                        {
                            const int SPAWN_ATTEMPTS = 5;
                            for (int y = 0; y < SPAWN_ATTEMPTS; y++)
                            {
                                Vector2 random = Random.insideUnitCircle * radius;
                                position.x += random.x;
                                position.z += random.y;

                                if (Physics.Raycast(position + (Vector3.up * 100f), Vector3.down, out RaycastHit, 150f, 1 << (int)Rust.Layer.Water | 1 << (int)Rust.Layer.World))
                                {
                                    if (RaycastHit.IsOnLayer(Rust.Layer.Water))
                                    {
                                        FreeableLootContainer freeableLootContainer = SpawnContainer(FLOATING_CRATE_PREFAB, RaycastHit.point, RandomUpRotation()) as FreeableLootContainer;
                                        ReleaseFreeableLootContainer(freeableLootContainer);

                                        break;
                                    }
                                }

                                yield return null;
                            }

                            yield return null;
                        }
                    }

                    public IEnumerator SpawnOnIceberg(Iceberg iceberg)
                    {
                        const string ICESHEET_NAME = "ice_sheet";
                        const string ELITE_CRATE_PREFAB = "assets/bundled/prefabs/radtown/crate_elite.prefab";

                        Bounds bounds = iceberg.Collider.bounds;
                        Vector3 localCenter = iceberg.Transform.InverseTransformPoint(bounds.center);
                        for (int i = 0; i < Amount; i++)
                        {
                            const int SPAWN_ATTEMPTS = 5;
                            for (int y = 0; y < SPAWN_ATTEMPTS; y++)
                            {
                                Vector3 random = new Vector3(Random.Range(-bounds.extents.x, bounds.extents.x), bounds.extents.y, Random.Range(-bounds.extents.z, bounds.extents.z)); 
                                Vector3 position = iceberg.Transform.TransformPoint(localCenter + random);

                                if (Physics.Raycast(position + (Vector3.up * 50f), Vector3.down, out RaycastHit, 150f, 1 << (int)Rust.Layer.Water | 1 << (int)Rust.Layer.World))
                                {
                                    if (RaycastHit.transform.name.Contains(ICEBERG_NAME) || RaycastHit.transform.name.Contains(ICESHEET_NAME))
                                    {
                                        SpawnContainer(ELITE_CRATE_PREFAB, RaycastHit.point, Quaternion.LookRotation(Vector3.forward, RaycastHit.normal) * RandomUpRotation());
                                        break;
                                    }
                                }

                                yield return null;
                            }

                            yield return null;
                        }
                    }

                    private LootContainer SpawnContainer(string prefabPath, Vector3 position, Quaternion rotation)
                    {
                        LootContainer lootContainer = GameManager.server.CreateEntity(prefabPath, position, rotation) as LootContainer;
                        lootContainer.enableSaving = false;
                        lootContainer.Spawn();

                        if (UseCustomLoot)
                            PopulateLoot(lootContainer);

                        if (DespawnTime > 0)
                            lootContainer.Invoke(lootContainer.RemoveMe, DespawnTime);

                        return lootContainer;
                    }

                    private void PopulateLoot(LootContainer lootContainer)
                    {
                        lootContainer.inventory.Clear();

                        int count = Random.Range(Minimum, Maximum);

                        int spawnedCount = 0;
                        int loopCount = 0;

                        while (true)
                        {
                            loopCount++;

                            if (loopCount > 3)
                                return;

                            float probability = Random.Range(0f, 1f);

                            List<LootItem> definitions = Pool.Get<List<LootItem>>();
                            definitions.AddRange(Table);

                            for (int i = 0; i < Table.Count; i++)
                            {
                                LootItem lootItem = definitions.GetRandom();

                                definitions.Remove(lootItem);

                                if (lootItem.Probability >= probability)
                                {
                                    lootItem.Create(lootContainer.inventory);

                                    spawnedCount++;

                                    if (spawnedCount >= count)
                                        return;
                                }
                            }
                        }
                    }
                }

                [JsonConverter(typeof(StringEnumConverter))]
                public enum ShipLootMode { Despawn, LeaveOnShip, FloatToSurface }
            }
                        
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Configuration = Config.ReadObject<ConfigData>();

            if (Configuration.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(Configuration, true);
        }

        protected override void LoadDefaultConfig() => Configuration = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Automation = new ConfigData.AutomationSettings
                {
                    Enabled = true,
                    Minimum = 7200,
                    Maximum = 10800,
                    RequiredPlayers = 1
                },
                Boat = new ConfigData.BoatSettings()
                {
                    Speed = 8f,
                    SinkTime1 = 90,
                    SinkTime2 = 90,
                    SinkTime3 = 60,
                    DespawnTime = 900,
                    Effects = new ConfigData.BoatSettings.EffectSettings
                    {
                        LargeExplosion = true,
                        NapalmLaunchers = true,
                        AlarmProximity = true,
                        ProximityDistance = 200f
                    }
                },
                Iceberg = new ConfigData.IcebergSelection
                {
                    DisableWithBases = false,
                    DisableWithCupboards = true
                },
                NPC = new ConfigData.NPCSettings
                {                    
                    ShipNPCs = new ConfigData.NPCSettings.ShipNPCSettings
                    {
                        ShipNPCDropLoot = false,
                        Kill = ConfigData.NPCSettings.ShipNPCSettings.KillMode.DieByDrowning,
                        Corpse = ConfigData.NPCSettings.ShipNPCSettings.CorpseMode.NoCorpse
                    },
                    AmountToSpawn = 5,
                    Settings = new Oxide.Ext.ChaosNPC.NPCSettings
                    {
                        DisplayNames = new string[] { "Pirate" },
                        Types = new NPCType[] { NPCType.TunnelDweller }, 
                        WoundedRecoveryChance = 80,
                        WoundedChance = 15,
                        RoamRange = 30                        
                    }
                },
                Loot = new ConfigData.LootSettings
                {
                    ShipLoot = ConfigData.LootSettings.ShipLootMode.FloatToSurface,
                    GibsToSpawn = 3,
                    GibsSpread = 10f,
                    Floating = new ConfigData.LootSettings.LootTable
                    {
                        Enabled = false,
                        Amount = 7,
                        Minimum = 3,
                        Maximum = 6,
                        UseCustomLoot = false,
                        DespawnTime = 1800,
                        Table = new List<ConfigData.LootSettings.LootTable.LootItem>
                        {
                            new ConfigData.LootSettings.LootTable.LootItem("rope", 2, 4, 1f),
                            new ConfigData.LootSettings.LootTable.LootItem("gears", 1, 2, 1f),
                            new ConfigData.LootSettings.LootTable.LootItem("scrap", 20, 25, 1f),
                            new ConfigData.LootSettings.LootTable.LootItem("metalblade", 1, 3, 1f),
                            new ConfigData.LootSettings.LootTable.LootItem("metalspring", 1, 2, 1f),
                            new ConfigData.LootSettings.LootTable.LootItem("sheetmetal", 1, 2, 1f),
                            new ConfigData.LootSettings.LootTable.LootItem("propanetank", 1, 1, 1f),
                        }
                    },
                    Iceberg = new ConfigData.LootSettings.LootTable
                    {
                        Enabled = true,
                        Amount = 3,
                        Minimum = 3,
                        Maximum = 5,
                        UseCustomLoot = false,
                        DespawnTime = 1800,
                        Table = new List<ConfigData.LootSettings.LootTable.LootItem>
                        {
                            new ConfigData.LootSettings.LootTable.LootItem("targeting.computer", 1, 1, 0.7f),
                            new ConfigData.LootSettings.LootTable.LootItem("techparts", 1, 2, 0.8f),
                            new ConfigData.LootSettings.LootTable.LootItem("scrap", 20, 25, 1f),
                            new ConfigData.LootSettings.LootTable.LootItem("metal.refined", 10, 20, 0.8f),
                            new ConfigData.LootSettings.LootTable.LootItem("supply.signal", 1, 1, 0.7f),
                            new ConfigData.LootSettings.LootTable.LootItem("flamethrower", 1, 1, 0.5f),
                            new ConfigData.LootSettings.LootTable.LootItem("shotgun.double", 1, 1, 0.5f),
                        }
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(Configuration, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (Configuration.Version < new Core.VersionNumber(2, 0, 0))
                Configuration = baseConfig;

            Configuration.Version = Version;
            PrintWarning("Config update completed!");
        }
        #endregion
    }
}
