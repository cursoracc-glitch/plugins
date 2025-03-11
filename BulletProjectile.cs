using System;
using System.Collections.Generic;
using UnityEngine;

using Rust;
using ProtoBuf;
using Oxide.Core.Plugins;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("BulletProjectile", "Karuza", "01.0.00")]
    public class BulletProjectile : RustPlugin
    {
        [HookMethod("ShootProjectile")]
        public void ShootProjectile(BasePlayer owner, Vector3 muzzlePos, Vector3 velocity, List<DamageTypeEntry> damageTypeOverrides, string ammoPrefabPathOverride = "")
        {
            ProjectileSystem projectile = new GameObject().AddComponent<ProjectileSystem>();
            projectile.owner = owner;

            if (!string.IsNullOrEmpty(ammoPrefabPathOverride))
                projectile.projectilePrefabPath = ammoPrefabPathOverride;

            projectile.InitializeBullet(muzzlePos);

            if (damageTypeOverrides.Any())
                projectile.damageTypes = damageTypeOverrides;

            projectile.InitializeVelocity(velocity);
            projectile.Launch();
        }

        #region ProjectileSystem
        public class ProjectileSystem : MonoBehaviour
        {
            public float gravityModifier = 1f;
            public float penetrationPower = 1f;
            public MinMax damageDistances = new MinMax(10f, 100f);
            public MinMax damageMultipliers = new MinMax(1f, 0.8f);
            public List<DamageTypeEntry> damageTypes = new List<DamageTypeEntry>();
            [NonSerialized]
            public float integrity = 1f;
            [NonSerialized]
            public float maxDistance = float.PositiveInfinity;
            [NonSerialized]
            public Projectile.Modifier modifier = Projectile.Modifier.Default;
            [Header("Attributes")]
            public Vector3 initialVelocity;
            [Tooltip("This projectile will raycast for this many units, and then become a projectile. This is typically done for bullets.")]
            public float initialDistance;
            [Header("Impact Rules")]
            [Range(0.0f, 1f)]
            public float ricochetChance = 0.1f;
            [Header("Rendering")]
            public ScaleRenderer rendererToScale = null;
            public ScaleRenderer firstPersonRenderer = null;
            [Header("Tumble")]
            public float tumbleSpeed;
            [NonSerialized]
            public BasePlayer owner;
            public string projectilePrefabPath { get; set; } = "assets/prefabs/npc/autoturret/sentrybullet.prefab";
            [NonSerialized]
            public uint seed = 0;
            [NonSerialized]
            public bool clientsideEffect;
            [NonSerialized]
            public bool clientsideAttack;
            [NonSerialized]
            public bool invisible;
            private Vector3 currentVelocity;
            private Vector3 currentPosition;
            private float traveledDistance;
            private float traveledTime;
            private Vector3 sentPosition;
            private Vector3 previousPosition;
            private float previousTraveledTime;
            private bool isRetiring;
            private HitTest hitTest;
            protected static Effect reusableInstance = new Effect();

            public void InitializeBullet(Vector3 location)
            {
                this.initialDistance = 0;
                this.transform.position = this.currentPosition = location;
                invisible = false;
                clientsideEffect = true;
                clientsideAttack = true;
                integrity = 2;
                maxDistance = float.PositiveInfinity;
                damageTypes.Add(new DamageTypeEntry() { type = DamageType.Bullet, amount = 25 });
            }

            public void CalculateDamage(HitInfo info, Projectile.Modifier mod, float scale)
            {
                if (info == null)
                    return;

                float num1 = this.damageMultipliers.Lerp(mod.distanceOffset + mod.distanceScale * this.damageDistances.x, mod.distanceOffset + mod.distanceScale * this.damageDistances.y, info.ProjectileDistance);
                float num2 = scale * (mod.damageOffset + mod.damageScale * num1);

                foreach (DamageTypeEntry damageType in this.damageTypes)
                    info.damageTypes.Add(damageType.type, damageType.amount * num2);
            }

            public bool isAuthoritative
            {
                get
                {
                    if (this.owner != null && this.owner.IsConnected)
                        return true;
                    return false;
                }
            }

            private bool isAlive
            {
                get
                {
                    if (this.integrity > 1.0 / 1000.0 && this.traveledDistance < this.maxDistance)
                        return this.traveledTime < 8.0;
                    return false;
                }
            }

            private void Retire()
            {
                if (this.isRetiring)
                    return;

                this.isRetiring = true;
                this.Cleanup();
            }

            private void Cleanup()
            {
                this.gameObject.BroadcastOnParentDestroying();
                new GameManager(false, true).Retire(this.gameObject);
            }

            public void AdjustVelocity(Vector3 delta)
            {
                this.currentVelocity += delta;
            }

            public void InitializeVelocity(Vector3 overrideVel)
            {
                this.initialVelocity = overrideVel;
                this.currentVelocity = this.initialVelocity;
            }

            protected void OnDisable()
            {
                this.currentVelocity = Vector3.zero;
                this.currentPosition = Vector3.zero;
                this.traveledDistance = 0.0f;
                this.traveledTime = 0.0f;
                this.sentPosition = Vector3.zero;
                this.previousPosition = Vector3.zero;
                this.previousTraveledTime = 0.0f;
                this.isRetiring = false;
                this.owner = null;
                this.seed = 0;
                this.clientsideEffect = false;
                this.clientsideAttack = false;
                this.integrity = 1f;
                this.maxDistance = float.PositiveInfinity;
                this.modifier = Projectile.Modifier.Default;
                this.invisible = false;
            }

            protected void FixedUpdate()
            {
                if (!this.isAlive)
                    return;
                this.UpdateVelocity(Time.fixedDeltaTime);
            }

            protected void Update()
            {
                if (!this.isAlive)
                    this.Retire();
            }

            private void UpdateVelocity(float deltaTime)
            {
                if (this.traveledTime != 0.0f)
                {
                    this.previousPosition = this.currentPosition;
                    this.previousTraveledTime = this.traveledTime;
                }
                this.currentPosition = this.transform.position;
                if (this.traveledTime == 0.0f)
                    this.sentPosition = this.previousPosition = this.currentPosition;

                deltaTime *= Time.timeScale;
                this.DoMovement(deltaTime);
                this.DoVelocityUpdate(deltaTime);

                this.transform.position = this.currentPosition;
                if (this.tumbleSpeed > 0.0f)
                    this.transform.Rotate(Vector3.right, this.tumbleSpeed * deltaTime);
                else
                    this.transform.rotation = Quaternion.LookRotation(this.currentVelocity);
            }

            private void DoVelocityUpdate(float deltaTime)
            {
                this.currentVelocity += Physics.gravity * this.gravityModifier * deltaTime;
                if (!this.isAuthoritative || GamePhysics.LineOfSight(this.sentPosition, this.currentPosition, 2162688, 0.0f))
                    return;
                using (PlayerProjectileUpdate update = Facepunch.Pool.Get<PlayerProjectileUpdate>())
                {
                    update.curPosition = this.previousPosition;
                    update.travelTime = this.previousTraveledTime;

                    this.owner.ClientRPC<PlayerProjectileUpdate>(this.owner.Connection, "OnProjectileUpdate", update);
                    this.sentPosition = this.previousPosition;
                }
            }

            private void DoMovement(float deltaTime)
            {
                Vector3 vector3_1 = this.currentVelocity * deltaTime;
                float magnitude1 = vector3_1.magnitude;
                float num1 = 1f / magnitude1;
                Vector3 direction = vector3_1 * num1;
                bool flag = false;
                Vector3 vPosB = this.currentPosition + direction * magnitude1;

                float num2 = this.traveledTime + deltaTime;
                if (this.hitTest == null)
                    this.hitTest = new HitTest();
                else
                    this.hitTest.Clear();
                this.hitTest.AttackRay = new Ray(this.currentPosition, direction);
                this.hitTest.MaxDistance = magnitude1;
                this.hitTest.ignoreEntity = (BaseEntity)this.owner;
                this.hitTest.Radius = 0.0f;
                this.hitTest.Forgiveness = 1;
                this.hitTest.type = this.isAuthoritative ? HitTest.Type.Projectile : HitTest.Type.ProjectileEffect;

                List<TraceInfo> list = Facepunch.Pool.GetList<TraceInfo>();
                GameTrace.TraceAll(this.hitTest, list);
                
                for (int index = 0; index < list.Count && this.isAlive && !flag; index++)
                {
                    if (list[index].valid)
                    {
                        list[index].UpdateHitTest(this.hitTest);
                        Vector3 vector3_2 = this.hitTest.HitPointWorld();
                        Vector3 normal = this.hitTest.HitNormalWorld();

                        float magnitude2 = (vector3_2 - this.currentPosition).magnitude;
                        float num3 = magnitude2 * num1 * deltaTime;
                        this.traveledDistance += magnitude2;
                        this.traveledTime += num3;

                        this.currentPosition = vector3_2;
                        if (this.DoRicochet(this.hitTest, vector3_2, normal) || this.DoHit(this.hitTest, vector3_2, normal))
                            flag = true;
                    }
                }

                Facepunch.Pool.FreeList<TraceInfo>(ref list);
                if (!this.isAlive)
                    return;
                if (flag && this.traveledTime < num2)
                {
                    this.DoMovement(num2 - this.traveledTime);
                }
                else
                {
                    float magnitude2 = (vPosB - this.currentPosition).magnitude;
                    this.traveledDistance += magnitude2;
                    this.traveledTime += magnitude2 * num1 * deltaTime;
                    this.currentPosition = vPosB;
                }
            }

            private bool DoWaterHit(ref HitTest test, Vector3 targetPosition)
            {
                float height = TerrainMeta.WaterMap.GetHeight(targetPosition);
                if ((double)height < (double)targetPosition.y)
                    return false;
                Vector3 point = targetPosition;
                point.y = height;
                Vector3 normal = TerrainMeta.WaterMap.GetNormal(targetPosition);
                test.DidHit = true;
                test.HitEntity = null;
                test.HitDistance = Vector3.Distance(test.AttackRay.origin, targetPosition);
                test.HitMaterial = "Water";
                test.HitPart = 0U;
                test.HitTransform = null;
                test.HitPoint = point;
                test.HitNormal = normal;
                test.collider = null;
                test.gameObject = null;
                this.DoHit(test, point, normal);
                this.integrity = 0.0f;
                return true;
            }

            private bool DoRicochet(HitTest test, Vector3 point, Vector3 normal)
            {
                Vector3 currentPosition = this.currentPosition;
                Vector3 currentVelocity = this.currentVelocity;
                bool flag = false;
                if ((!(test.HitEntity != null) || !(test.HitEntity is BaseCombatEntity)) && (this.ricochetChance > 0.0f && UnityEngine.Random.Range(1, int.MaxValue) <= this.ricochetChance) && !Projectile.IsWaterMaterial(test.HitMaterial))
                    flag = this.Reflect(ref this.seed, point, normal);
                if (flag)
                {
                    if (this.isAuthoritative)
                    {
                        using (PlayerProjectileRicochet ricochet = Facepunch.Pool.Get<PlayerProjectileRicochet>())
                        {
                            ricochet.hitPosition = currentPosition;
                            ricochet.inVelocity = currentVelocity;
                            ricochet.outVelocity = this.currentVelocity;
                            ricochet.hitNormal = normal;
                            ricochet.travelTime = this.traveledTime;

                            this.owner.ClientRPC<PlayerProjectileRicochet>(this.owner.Connection, "OnProjectileRicochet", ricochet);
                            this.sentPosition = this.currentPosition;
                        }
                    }
                }
                return flag;
            }

            public static Attack BuildAttackMessage(HitTest test)
            {
                uint hitBone = 0;
                uint hitMaterialId = 0;
                if (test.HitTransform && test.HitEntity && test.HitEntity.transform != test.HitTransform)
                {
                    hitBone = StringPool.Get(test.HitTransform.name);
                }
                if (!string.IsNullOrEmpty(test.HitMaterial))
                {
                    hitMaterialId = StringPool.Get(test.HitMaterial);
                }
                Attack attack = Facepunch.Pool.Get<Attack>();
                attack.pointStart = test.AttackRay.origin;
                attack.pointEnd = test.AttackRay.origin + test.AttackRay.direction * test.MaxDistance;
                attack.hitMaterialID = hitMaterialId;
                if (test.DidHit)
                {
                    if (test.HitEntity.IsValid())
                    {
                        Transform transform = test.HitTransform;
                        if (!transform)
                            transform = test.HitEntity.transform;
                        attack.hitID = test.HitEntity.net.ID;
                        attack.hitBone = hitBone;
                        attack.hitPartID = test.HitPart;
                        attack.hitPositionWorld = transform.localToWorldMatrix.MultiplyPoint(test.HitPoint);
                        attack.hitPositionLocal = test.HitPoint;
                        attack.hitNormalWorld = transform.localToWorldMatrix.MultiplyVector(test.HitNormal);
                        attack.hitNormalLocal = test.HitNormal;
                    }
                    else
                    {
                        attack.hitID = 0U;
                        attack.hitBone = 0U;
                        attack.hitPositionWorld = test.HitPoint;
                        attack.hitPositionLocal = test.HitPoint;
                        attack.hitNormalWorld = test.HitNormal;
                        attack.hitNormalLocal = test.HitNormal;
                    }
                }
                return attack;
            }


            private bool DoHit(HitTest test, Vector3 point, Vector3 normal)
            {
                bool flag = false;
                using (PlayerProjectileAttack attack = Facepunch.Pool.Get<PlayerProjectileAttack>())
                {
                    attack.playerAttack = Facepunch.Pool.Get<PlayerAttack>();
                    attack.playerAttack.attack = BuildAttackMessage(test);
                    HitInfo info = new HitInfo();
                    LoadFromAttack(info, attack.playerAttack.attack, false);
                    info.Initiator = this.owner;
                    info.ProjectileDistance = this.traveledDistance;
                    info.ProjectileVelocity = this.currentVelocity;
                    info.IsPredicting = true;
                    info.DoDecals = true;
                    this.CalculateDamage(info, this.modifier, this.integrity);
                    if (object.ReferenceEquals(info.HitEntity, null) && Projectile.IsWaterMaterial(test.HitMaterial))
                    {
                        this.currentVelocity *= 0.1f;
                        this.currentPosition += this.currentVelocity.normalized * (1f / 1000f);
                        this.integrity = Mathf.Clamp01(this.integrity - 0.1f);
                        flag = true;
                    }
                    else if (this.penetrationPower <= 0.0f || object.ReferenceEquals(info.HitEntity, null))
                    {
                        this.integrity = 0.0f;
                    }
                    else
                    {
                        float resistance = info.HitEntity.PenetrationResistance(info) / this.penetrationPower;
                        flag = this.Refract(ref this.seed, point, normal, resistance);
                        this.integrity = Mathf.Clamp01(this.integrity - resistance);
                        if (info.HitEntity is BasePlayer)
                            info.HitMaterial = StringPool.Get("Flesh");
                    }
                    if (this.isAuthoritative)
                    {
                        attack.hitVelocity = this.currentVelocity;
                        attack.hitDistance = this.traveledDistance;
                        attack.travelTime = this.traveledTime;

                        this.owner.ClientRPC<PlayerProjectileAttack>(this.owner.Connection, "OnProjectileAttack", attack);
                        this.sentPosition = this.currentPosition;
                    }
                    if (this.clientsideAttack && info.HitEntity != null)
                        info.HitEntity.OnAttacked(info);
                    if (this.clientsideEffect)
                        Effect.server.ImpactEffect(info);
                }

                return flag;
            }

            public static void LoadFromAttack(HitInfo info, Attack attack, bool serverSide)
            {
                info.HitEntity = null;
                info.PointStart = attack.pointStart;
                info.PointEnd = attack.pointEnd;
                if (attack.hitID > 0U)
                {
                    info.DidHit = true;
                    if (!serverSide)
                        info.HitEntity = BaseNetworkable.serverEntities.Find(attack.hitID) as BaseEntity;
                    if (info.HitEntity != null)
                    {
                        info.HitBone = attack.hitBone;
                        info.HitPart = attack.hitPartID;
                    }
                }
                info.DidHit = true;
                info.HitPositionLocal = attack.hitPositionLocal;
                info.HitPositionWorld = attack.hitPositionWorld;
                info.HitNormalLocal = attack.hitNormalLocal.normalized;
                info.HitNormalWorld = attack.hitNormalWorld.normalized;
                info.HitMaterial = attack.hitMaterialID;
            }

            private bool Reflect(ref uint seed, Vector3 point, Vector3 normal)
            {
                bool flag = false;
                if (this.currentVelocity.magnitude > 50.0f)
                {
                    float velocityMod = Mathf.Clamp01((float)(1.0 - (90f - Vector3.Angle(-this.currentVelocity, normal)) / 30.0)) * 0.8f;
                    if (velocityMod > 0.0f)
                    {
                        this.currentVelocity = Vector3.Reflect(this.currentVelocity, (Quaternion.LookRotation(normal) * this.RandomRotation(ref seed, 10f) * Vector3.forward).normalized) * velocityMod;
                        this.currentPosition += this.currentVelocity.normalized * (1f / 1000f);
                        flag = true;
                    }
                }
                return flag;
            }

            private bool Refract(ref uint seed, Vector3 point, Vector3 normal, float resistance)
            {
                bool flag = false;
                if ((double)resistance < 1.0)
                {
                    float num = Mathf.Lerp(1f, 0.5f, resistance);
                    if (num > 0.0f)
                    {
                        this.currentVelocity = this.Refract(this.currentVelocity, (Quaternion.LookRotation(normal) * this.RandomRotation(ref seed, 10f) * Vector3.forward).normalized, resistance) * num;
                        this.currentPosition += this.currentVelocity.normalized * (1f / 1000f);
                        flag = true;
                    }
                }
                return flag;
            }

            private Vector3 Refract(Vector3 v, Vector3 n, float f)
            {
                float magnitude = v.magnitude;
                return Vector3.Slerp(v / magnitude, -n, f) * magnitude;
            }

            private Quaternion RandomRotation(ref uint seed, float range)
            {
                Xorshift(ref seed);
                float x = seed * (-range - range);
                Xorshift(ref seed);
                float y = seed * (-range - range);
                Xorshift(ref seed);
                float z = seed * (-range - range);
                return Quaternion.Euler(x, y, z);
            }

            public static uint Xorshift(ref uint x)
            {
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                return x;
            }

            internal void Launch()
            {
                Effect reusableInstance = ProjectileSystem.reusableInstance;
                reusableInstance.Clear();
                reusableInstance.Init(Effect.Type.Projectile, this.currentPosition, this.currentVelocity, null);
                reusableInstance.scale = 5f;
                reusableInstance.pooledString = projectilePrefabPath;
                EffectNetwork.Send(reusableInstance);

                while (this.isAlive && (this.traveledDistance < this.initialDistance || this.traveledTime < 0.100000001490116f))
                    this.UpdateVelocity(Time.fixedDeltaTime);
            }
        }

        public static class GameTrace
        {
            public static void TraceAll(HitTest test, List<TraceInfo> traces, int layerMask = -5)
            {
                List<RaycastHit> list = Facepunch.Pool.GetList<RaycastHit>();
                Vector3 origin = test.AttackRay.origin;
                Vector3 direction = test.AttackRay.direction;
                float maxDistance = test.MaxDistance;
                float radius = test.Radius;
                if ((layerMask & 16384) != 0)
                {
                    layerMask &= -16385;
                    GamePhysics.TraceAllUnordered(new Ray(origin - direction * 5f, direction), radius, list, maxDistance + 5f, 16384, QueryTriggerInteraction.UseGlobal);
                    for (int index = 0; index < list.Count; index++)
                    {
                        RaycastHit raycastHit = list[index];
                        raycastHit.distance -= 5f;
                        list[index] = raycastHit;
                    }
                }
                GamePhysics.TraceAllUnordered(new Ray(origin, direction), radius, list, maxDistance, layerMask, QueryTriggerInteraction.UseGlobal);
                for (int index = 0; index < list.Count; index++)
                {
                    RaycastHit hit = list[index];
                    Collider collider = hit.GetCollider();

                    if (!collider.isTrigger)
                    {
                        ColliderInfo component1 = collider.GetComponent<ColliderInfo>();
                        if (object.ReferenceEquals(component1, null) || component1.Filter(test))
                        {
                            if (hit.distance > 0.0f)
                            {
                                TraceInfo traceInfo = new TraceInfo()
                                {
                                    valid = true,
                                    distance = hit.distance,
                                    partID = 0,
                                    point = hit.point,
                                    normal = hit.normal,
                                    collider = collider,
                                    material = collider.GetMaterialAt(hit.point),
                                    entity = collider.gameObject.ToBaseEntity()
                                };
                                traceInfo.bone = GetTransform(collider.transform, hit.point, traceInfo.entity);

                                if (object.ReferenceEquals(traceInfo.entity, null) || traceInfo.entity != test.ignoreEntity)
                                    traces.Add(traceInfo);
                            }
                        }
                    }
                }
                traces.Sort((a, b) => a.distance.CompareTo(b.distance));
                Facepunch.Pool.FreeList<RaycastHit>(ref list);
            }

            public static Transform GetTransform(Transform bone, Vector3 position, BaseEntity entity)
            {
                if (bone.gameObject.GetComponentInParent<Model>() != null)
                    return bone;
                if (object.ReferenceEquals(entity, null))
                    return null;
                if (entity.model && entity.model.rootBone)
                    return entity.model.FindClosestBone(position);
                return entity.transform;
            }
        }

        public struct TraceInfo
        {
            public bool valid;
            public float distance;
            public BaseEntity entity;
            public Vector3 point;
            public Vector3 normal;
            public Transform bone;
            public PhysicMaterial material;
            public uint partID;
            public Collider collider;
            
            public void UpdateHitTest(HitTest test)
            {
                test.DidHit = true;
                test.HitEntity = this.entity;
                test.HitDistance = this.distance;
                test.HitMaterial = this.material != null ? this.material.GetName() : "generic";
                test.HitPart = this.partID;
                test.HitTransform = this.bone;
                test.HitPoint = this.point;
                test.HitNormal = this.normal;
                test.collider = this.collider;
                test.gameObject = this.collider ? this.collider.gameObject : test.HitTransform.gameObject;
                if (test.HitTransform == null)
                    return;
                test.HitPoint = test.HitTransform.InverseTransformPoint(this.point);
                test.HitNormal = test.HitTransform.InverseTransformDirection(this.normal);
            }
        }
        #endregion

    }
}
