using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using Rust;
using Facepunch;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("Advanced Airstrike", "k1lly0u", "0.1.75")]
    class AdvAirstrike : RustPlugin
    {
        #region Fields
        [PluginReference]
        Plugin Economics, ServerRewards, Spawns, NoEscape;

        StoredData storedData;
        private DynamicConfigFile data;

        private Dictionary<ulong, StrikeType> toggleList = new Dictionary<ulong, StrikeType>();

        private Dictionary<string, int> shortnameToId = new Dictionary<string, int>();
        private Dictionary<string, string> shortnameToDn = new Dictionary<string, string>();
        private Hash<ulong, double> playerCooldowns = new Hash<ulong, double>();

        private List<StrikeType> availableTypes = new List<StrikeType>();
        private List<RadiationEntry> radiationZones = new List<RadiationEntry>();

        private List<BaseEntity> strikeSignals = new List<BaseEntity>();

        private static AdvAirstrike ins;
                
        const string cargoPlanePrefab = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
        const string basicRocket = "assets/prefabs/npc/patrol helicopter/rocket_heli.prefab";
        const string fireRocket = "assets/prefabs/npc/patrol helicopter/rocket_heli_napalm.prefab";
        const string tankShell = "assets/prefabs/npc/m2bradley/maincannonshell.prefab";
        const string heliExplosion = "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab";
        const string fireball = "assets/bundled/prefabs/oilfireballsmall.prefab";
        const string c4Explosion = "assets/prefabs/tools/c4/effects/c4_explosion.prefab";
        const string debris = "assets/prefabs/npc/patrol helicopter/damage_effect_debris.prefab";
        const string bulletExp = "assets/bundled/prefabs/fx/impacts/additive/explosion.prefab";
        const string fireCannonEffect = "assets/prefabs/npc/m2bradley/effects/maincannonattack.prefab";

        static int layerMaskExpl = LayerMask.GetMask("Deployed", "AI", "Vehicle_Movement", "Player_Server", "Construction");

        enum StrikeType { None, Strike, Squad, Napalm, Nuke, Spectre }
        enum FireType { Rocket, Bullet, Cannon, Combined }
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            data = Interface.Oxide.DataFileSystem.GetFile("advairstrike_data");

            lang.RegisterMessages(Messages, this);

            foreach(var type in Enum.GetValues(typeof(StrikeType)))
            {
                permission.RegisterPermission($"advairstrike.signal.{type.ToString().ToLower()}", this);
                permission.RegisterPermission($"advairstrike.purchase.{type.ToString().ToLower()}", this);
                permission.RegisterPermission($"advairstrike.chat.{type.ToString().ToLower()}", this);
            }            
            permission.RegisterPermission("advairstrike.ignorecooldown", this);
        }

        private void OnServerInitialized()
        {
            ins = this;
            LoadData();

            shortnameToId = ItemManager.itemList.ToDictionary(x => x.shortname, y => y.itemid);
            shortnameToDn = ItemManager.itemList.ToDictionary(x => x.shortname, y => y.displayName.translated);

            PrepareRandomStrikes();
        }

        private void Unload()
        {
            ins = null;

            BasePlane[] objects = UnityEngine.Object.FindObjectsOfType<BasePlane>();
            if (objects != null)
            {
                foreach (BasePlane obj in objects)
                    UnityEngine.Object.Destroy(obj);
            }
            RocketMonitor[] monitors = UnityEngine.Object.FindObjectsOfType<RocketMonitor>();
            if (monitors != null)
            {
                foreach (RocketMonitor obj in monitors)
                    UnityEngine.Object.Destroy(obj);
            }

            for (int i = 0; i < radiationZones.Count; i++)
            {
                radiationZones[i].time.Destroy();
                UnityEngine.Object.Destroy(radiationZones[i].zone);
            }
            radiationZones.Clear();
        }

        private void OnServerSave() => SaveData();

        private void OnExplosiveDropped(BasePlayer player, BaseEntity entity) => OnExplosiveThrown(player, entity);

        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (toggleList.ContainsKey(player.userID) && entity is SupplySignal)
            {
                strikeSignals.Add(entity);

                StrikeType type = toggleList[player.userID];
                toggleList.Remove(player.userID);
                AddCooldownData(player, type);

                entity.CancelInvoke((entity as SupplySignal).Explode);
                entity.Invoke(() =>
                {
                    strikeSignals.Remove(entity);
                    entity.KillMessage();
                }, 30f);

                timer.Once(3, () =>
                {
                    Effect.server.Run("assets/bundled/prefabs/fx/smoke_signal_full.prefab", entity, 0, new Vector3(), new Vector3());
                    SendStrike(player, type, entity.transform.position);
                });
            }
        }
        #endregion

        #region Plane Control
        private class NapalmPlane : BasePlane
        {
            private void Awake()
            {
                entity = GetComponent<CargoPlane>();
                entity.enabled = false;
                rocketOptions = ins.configData.Rocket;
                fireDistance = ins.configData.Plane.Distance;

                forcedType = fireRocket;
                enabled = false;
            }
            public override void LaunchRocket()
            {
                if (projectilesFired >= rocketOptions.Amount)
                {
                    InvokeHandler.CancelInvoke(this, LaunchRocket);
                    return;
                }
                                
                BaseEntity rocket = FireRocket();
                rocket.gameObject.AddComponent<RocketMonitor>().SetType(StrikeType.Napalm);
                ++projectilesFired;
            }
        }

        private class NukePlane : BasePlane
        {
            private void Awake()
            {
                entity = GetComponent<CargoPlane>();
                entity.enabled = false;
                rocketOptions = ins.configData.Rocket;

                forcedType = basicRocket;
                enabled = false;
            }
            public override void LaunchRocket()
            {
                InvokeHandler.CancelInvoke(this, LaunchRocket);
                
                BaseEntity rocket = FireRocket();
                rocket.gameObject.AddComponent<RocketMonitor>().SetType(StrikeType.Nuke);
                ++projectilesFired;
            }
        }

        private class SpectrePlane : BasePlane
        {
            private bool inOrbit;
            private float flightRadius;
            private Vector3 beginOrbit;
            private float distance;
            private float speed;
            private int rotations;
            private bool isLeaning;

            private void Awake()
            {
                entity = GetComponent<CargoPlane>();
                entity.enabled = false;
                rocketOptions = ins.configData.Rocket;

                this.flightRadius = ins.configData.Plane.FlightRadius;
                distance = Convert.ToSingle(Math.PI * (flightRadius * 2));
                speed = ins.configData.Plane.Speed;
                enabled = false;
            }
            
            public override void InitializeFlightPath(Vector3 targetPos, StrikeType strikeType = StrikeType.Spectre)
            {
                CheckFireType(strikeType);
                this.strikeType = strikeType;
                this.targetPos = targetPos;

                float size = TerrainMeta.Size.x;
                float highestPoint = 170f;

                startPos = Vector3Ex.Range(-1f, 1f);
                startPos.y = 0f;
                startPos.Normalize();
                startPos = startPos * (size * 2f);
                startPos.y = highestPoint;

                endPos = startPos * -1f;
                endPos.y = startPos.y;
                startPos = startPos + targetPos;
                endPos = endPos + targetPos;

                secondsToTake = (Vector3.Distance(startPos, endPos) / ins.configData.Plane.Speed) * UnityEngine.Random.Range(0.95f, 1.05f);
                                
                entity.transform.rotation = Quaternion.LookRotation(endPos - startPos);

                startPos = startPos + (-entity.transform.right * flightRadius);
                endPos = endPos + (-entity.transform.right * flightRadius);
                beginOrbit = targetPos + (-entity.transform.right * flightRadius);
                beginOrbit.y = startPos.y;

                entity.transform.position = startPos;

                enabled = true;
            }
            public override void MovePlane()
            {                
                if (!inOrbit)
                {
                    Rotate();

                    secondsTaken += Time.deltaTime;
                    float single = Mathf.InverseLerp(0f, secondsToTake, secondsTaken);

                    entity.transform.position = Vector3.Lerp(startPos, endPos, single);
                    entity.transform.hasChanged = true;
                    if (single >= 1f)
                        entity.Kill(BaseNetworkable.DestroyMode.None);
                }
                else
                {
                    Rotate();
                    entity.transform.RotateAround(targetPos, Vector3.up, distance / speed * Time.deltaTime);
                }
            }            
            private void Rotate()
            {
                if (rotations < 600 && isLeaning)
                {
                    entity.transform.rotation = Quaternion.Euler(new Vector3(entity.transform.rotation.eulerAngles.x, entity.transform.rotation.eulerAngles.y, entity.transform.rotation.eulerAngles.z - 0.05f));
                    ++rotations;
                }
                if (rotations > 0 && !isLeaning)
                {
                    entity.transform.rotation = Quaternion.Euler(new Vector3(entity.transform.rotation.eulerAngles.x, entity.transform.rotation.eulerAngles.y, entity.transform.rotation.eulerAngles.z + 0.05f));
                    --rotations;
                }
            }
            
            public override void CanFireRockets()
            {
                if (!isFiring && Vector3.Distance(entity.transform.position, beginOrbit) < 2)
                {
                    entity.transform.rotation = Quaternion.Euler(new Vector3(entity.transform.rotation.eulerAngles.x, entity.transform.rotation.eulerAngles.y, 359.95f));
                    isLeaning = true;
                    inOrbit = true;
                    isFiring = true;
                    TryFireWeapons();                    
                }
            }
            public override void OnAmmunitionDepleted()
            {
                startPos = entity.transform.position;
                endPos = entity.transform.forward * Vector3.Distance(entity.transform.position, endPos);
                secondsToTake /= 2;
                secondsTaken = 0;
                isLeaning = false;
                inOrbit = false;                
            }
        }

        private class BasePlane : MonoBehaviour
        {
            internal CargoPlane entity;
            internal RocketOptions rocketOptions;
            internal CannonOptions cannonOptions;
            internal GunOptions gunOptions;
            internal FireType fireType = FireType.Rocket;
            internal StrikeType strikeType = StrikeType.None;

            internal Vector3 startPos;
            internal Vector3 endPos;
            internal Vector3 targetPos;

            internal float secondsToTake;
            internal float secondsTaken;
            internal bool isFiring;
            internal int projectilesFired;
            internal int projectilesFiredGun;
            internal string forcedType;
            
            internal float fireDistance;

            private AutoTurret autoTurret;           

            private void Awake()
            {
                entity = GetComponent<CargoPlane>();
                entity.enabled = false;                
                rocketOptions = ins.configData.Rocket;                
                fireDistance = ins.configData.Plane.Distance;

                enabled = false;
            }
            private void Update()
            {
                CanFireRockets();
                MovePlane();
            }
            private void OnDestroy()
            {
                InvokeHandler.CancelInvoke(this, FireGun);
                InvokeHandler.CancelInvoke(this, LaunchRocket);
                if (autoTurret != null && !autoTurret.IsDestroyed)
                    autoTurret.Kill();
                if (entity != null && !entity.IsDestroyed)
                    entity.Kill();
            }

            public virtual void InitializeFlightPath(Vector3 targetPos, StrikeType strikeType)
            {
                CheckFireType(strikeType);
                this.strikeType = strikeType;
                this.targetPos = targetPos;

                float size = TerrainMeta.Size.x;
                float highestPoint = 170f;

                startPos = Vector3Ex.Range(-1f, 1f);
                startPos.y = 0f;
                startPos.Normalize();
                startPos = startPos * (size * 2f);
                startPos.y = highestPoint;

                endPos = startPos * -1f;
                endPos.y = startPos.y;
                startPos = startPos + targetPos;
                endPos = endPos + targetPos;

                secondsToTake = (Vector3.Distance(startPos, endPos) / ins.configData.Plane.Speed) * UnityEngine.Random.Range(0.95f, 1.05f);

                entity.transform.position = startPos;
                entity.transform.rotation = Quaternion.LookRotation(endPos - startPos);

                if (fireType == FireType.Bullet && fireDistance > 250)
                    fireDistance = 250;

                if (strikeType == StrikeType.Nuke)                
                    fireDistance = (entity.transform.position.y - targetPos.y) + 10f;                
                
                enabled = true;
            }
            internal void CheckFireType(StrikeType strikeType)
            {
                if (ins.configData.Types.ContainsKey(strikeType))                                    
                    fireType = ins.ParseType<FireType>(ins.configData.Types[strikeType]);

                if (fireType == FireType.Bullet || fireType == FireType.Combined)
                {
                    cannonOptions = ins.configData.Cannon;
                    gunOptions = ins.configData.Gun[strikeType];
                    autoTurret = (AutoTurret)GameManager.server.CreateEntity("assets/prefabs/npc/autoturret/autoturret_deployed.prefab", transform.position);
                    autoTurret.Spawn();
                    autoTurret.enableSaving = false;
                    autoTurret.SetParent(entity, 0);
                    autoTurret.transform.localEulerAngles = new Vector3(0, 0, 180);
                    autoTurret.transform.localPosition = new Vector3();

                    (autoTurret as BaseCombatEntity).baseProtection = Resources.FindObjectsOfTypeAll<ProtectionProperties>().ToList().FirstOrDefault(p => p.name == "Immortal");
                }      
                else if (fireType == FireType.Cannon)
                    cannonOptions = ins.configData.Cannon;
            }
            public virtual void CanFireRockets()
            {
                if (!isFiring && Vector3.Distance(transform.position, targetPos) <= fireDistance)
                {
                    isFiring = true;
                    TryFireWeapons();
                }
            }
            public virtual void MovePlane()
            {
                secondsTaken += Time.deltaTime;
                float single = Mathf.InverseLerp(0f, secondsToTake, secondsTaken);
                
                entity.transform.position = Vector3.Lerp(startPos, endPos, single);
                entity.transform.hasChanged = true;
                if (single >= 1f)                
                    entity.Kill(BaseNetworkable.DestroyMode.None);                
            }
            public void GetFlightData(out Vector3 startPos, out Vector3 endPos, out float secondsToTake)
            {
                startPos = this.startPos;
                endPos = this.endPos;
                secondsToTake = this.secondsToTake;
            }
            public void SetFlightData(Vector3 startPos, Vector3 endPos, Vector3 targetPos, float secondsToTake)
            {
                strikeType = StrikeType.Squad;
                CheckFireType(strikeType);

                this.startPos = startPos;
                this.endPos = endPos;
                this.targetPos = targetPos;
                this.secondsToTake = secondsToTake;

                entity.transform.position = startPos;
                entity.transform.rotation = Quaternion.LookRotation(endPos - startPos);                

                enabled = true;
            }
            internal void TryFireWeapons()
            {
                if (fireType == FireType.Bullet)
                    InvokeHandler.InvokeRepeating(this, FireGun, 0, gunOptions.FireRate);
                else if (fireType == FireType.Combined)
                {
                    InvokeHandler.InvokeRepeating(this, FireGun, 0, gunOptions.FireRate);
                    InvokeHandler.InvokeRepeating(this, LaunchRocket, 0, strikeType == StrikeType.Spectre ? cannonOptions.IntervalSpectre : cannonOptions.Interval);
                }
                else InvokeHandler.InvokeRepeating(this, LaunchRocket, 0, strikeType == StrikeType.Spectre ? (fireType == FireType.Rocket ? rocketOptions.IntervalSpectre : cannonOptions.IntervalSpectre) : (fireType == FireType.Rocket ? rocketOptions.Interval : cannonOptions.Interval));
            }
                       
            public virtual void LaunchRocket()
            {
                if (projectilesFired >= (fireType == FireType.Rocket ? rocketOptions.Amount : cannonOptions.Amount))
                {
                    if (fireType != FireType.Combined || (fireType == FireType.Combined && !InvokeHandler.IsInvoking(this, FireGun)))
                        OnAmmunitionDepleted();
                    
                    InvokeHandler.CancelInvoke(this, LaunchRocket);
                    return;
                }

                if (fireType == FireType.Combined || fireType == FireType.Cannon)
                    FireCannon();
                else FireRocket();
                ++projectilesFired;

            }

            public virtual void OnAmmunitionDepleted()
            {                
            }

            internal BaseEntity FireCannon()
            {
                Effect.server.Run(fireCannonEffect, entity, 0u, Vector3.zero, Vector3.zero, null, false);
               
                float accuracy = GetComponent<SpectrePlane>() ? cannonOptions.SpectreAccuracy : cannonOptions.Accuracy;
                Vector3 launchPos = entity.transform.position;
                Vector3 newTarget = Quaternion.Euler(GetRandom(accuracy), GetRandom(accuracy), GetRandom(accuracy)) * targetPos;

                BaseEntity rocket = GameManager.server.CreateEntity(tankShell, launchPos, new Quaternion(), true);

                TimedExplosive rocketExplosion = rocket.GetComponent<TimedExplosive>();
                               
                for (int i = 0; i < rocketExplosion.damageTypes.Count; i++)
                    rocketExplosion.damageTypes[i].amount *= cannonOptions.Damage;

                Vector3 newDirection = (newTarget - launchPos);

                rocket.SendMessage("InitializeVelocity", newDirection);
                rocket.Spawn();
                return rocket;
            }

            internal BaseEntity FireRocket()
            {
                string rocketType = forcedType;
                if (string.IsNullOrEmpty(forcedType))
                {
                    rocketType = rocketOptions.Type == "Normal" ? basicRocket : fireRocket;
                    if (rocketOptions.Mixed && UnityEngine.Random.Range(1, rocketOptions.FireChance) == 1)
                        rocketType = fireRocket;
                }
               
                float accuracy = GetComponent<SpectrePlane>() ? rocketOptions.SpectreAccuracy : GetComponent<NukePlane>() ? 0.5f : rocketOptions.Accuracy;
                Vector3 launchPos = entity.transform.position;
                Vector3 newTarget = Quaternion.Euler(GetRandom(accuracy), GetRandom(accuracy), GetRandom(accuracy)) * targetPos;

                Effect.server.Run("assets/prefabs/npc/m2bradley/effects/maincannonattack.prefab", entity, 0u, Vector3.zero, Vector3.zero, null, false);
                BaseEntity rocket = GameManager.server.CreateEntity(rocketType, launchPos, new Quaternion(), true);
                
                TimedExplosive rocketExplosion = rocket.GetComponent<TimedExplosive>();
                ServerProjectile rocketProjectile = rocket.GetComponent<ServerProjectile>();

                rocketProjectile.speed = rocketOptions.Speed;
                rocketProjectile.gravityModifier = 0;
                rocketExplosion.timerAmountMin = 60;
                rocketExplosion.timerAmountMax = 60;

                for (int i = 0; i < rocketExplosion.damageTypes.Count; i++)
                    rocketExplosion.damageTypes[i].amount *= rocketOptions.Damage;

                Vector3 newDirection = (newTarget - launchPos);

                rocket.SendMessage("InitializeVelocity", newDirection);
                rocket.Spawn();
               
                return rocket;
            }

            internal void FireGun()
            {
                if (projectilesFiredGun >= gunOptions.Amount)
                {
                    if (fireType == FireType.Bullet || (fireType == FireType.Combined && !InvokeHandler.IsInvoking(this, LaunchRocket)))
                        OnAmmunitionDepleted();
                    InvokeHandler.CancelInvoke(this, FireGun);
                    return;
                }

                ++projectilesFiredGun;

                UpdateFacingToTarget();

                Vector3 centerMuzzle = autoTurret.GetCenterMuzzle().transform.position;
                Vector3 modifiedAimConeDir = AimConeUtil.GetModifiedAimConeDirection(gunOptions.Accuracy * 0.2f, targetPos - centerMuzzle, true);
                Vector3 newTargetPosition = centerMuzzle + (modifiedAimConeDir * 400f);
                
                List<RaycastHit> list = Pool.GetList<RaycastHit>();
                GamePhysics.TraceAll(new Ray(centerMuzzle, modifiedAimConeDir), 0f, list, 400f, 1219701521, QueryTriggerInteraction.UseGlobal);
                for (int i = 0; i < list.Count; i++)
                {
                    RaycastHit hit = list[i];
                    BaseEntity entity = hit.GetEntity();
                    if ((!(entity != null) || (!(entity == this) && !entity.EqualNetID(autoTurret))) && (!(entity != null) || !(entity.GetComponent<BasePlayer>() != null)))
                    {
                        BaseCombatEntity baseCombatEntity = entity as BaseCombatEntity;
                        if (baseCombatEntity != null)
                        {
                            HitInfo info = new HitInfo(autoTurret, entity, DamageType.Bullet, gunOptions.Damage, hit.point);
                            entity.OnAttacked(info);
                            if (entity is BasePlayer || entity is BaseNpc)
                            {
                                Effect.server.ImpactEffect(new HitInfo
                                {
                                    HitPositionWorld = hit.point,
                                    HitNormalWorld = -modifiedAimConeDir,
                                    HitMaterial = StringPool.Get("Flesh")
                                });
                            }                            
                        }
                        if (!(entity != null) || entity.ShouldBlockProjectiles())
                        {
                            newTargetPosition = hit.point;
                            break;
                        }
                        else newTargetPosition = hit.point;
                    }
                }

                if (gunOptions.IsExplosive)
                {
                    DamageUtil.RadiusDamage(autoTurret, null, newTargetPosition, 0.5f, 0.5f, new List<DamageTypeEntry> { new DamageTypeEntry { amount = 5f, type = DamageType.Explosion } }, layerMaskExpl, false);
                    Effect.server.Run(bulletExp, newTargetPosition);
                }

                autoTurret.ClientRPC(null, "CLIENT_FireGun", StringPool.Get(autoTurret.muzzlePos.gameObject.name), newTargetPosition);

                Pool.FreeList(ref list);
            }

            private void UpdateFacingToTarget()
            {
                Vector3 a = targetPos;
                Vector3 b = autoTurret.gun_pitch.transform.InverseTransformPoint(autoTurret.muzzlePos.transform.position);
                b.z = (b.x = 0f);
                Vector3 vector = a - (autoTurret.gun_pitch.position + b);
                autoTurret.aimDir = vector;
                autoTurret.UpdateAiming();
            }

            internal float GetRandom(float accuracy) => UnityEngine.Random.Range(-accuracy * 0.2f, accuracy * 0.2f);
        }
        #endregion

        #region Rockets and Radiation
        private class RocketMonitor : MonoBehaviour
        {
            StrikeType type;
            public void SetType(StrikeType type)
            {
                this.type = type;
            }

            private void OnDestroy()
            {
                if (type == StrikeType.Napalm)
                {
                    SpawnFireball(transform.position);
                }
                if (type == StrikeType.Nuke)
                {
                    new NukeExplosion(transform.position);
                    InitializeZone(transform.position);
                }
            }

            FireBall SpawnFireball(Vector3 position)
            {
                FireBall fireBall = GameManager.server.CreateEntity(fireball, position, new Quaternion(), true) as FireBall;
                if (fireBall)
                {
                    fireBall.GetComponent<Rigidbody>().isKinematic = true;
                    fireBall.GetComponent<Collider>().enabled = false;                    
                    fireBall.Spawn();
                    fireBall.CancelInvoke("Extinguish");
                    fireBall.Invoke("Extinguish", ins.configData.Rocket.FireLife);
                    return fireBall;
                }
                return null;
            }      
            
            private void InitializeZone(Vector3 location)
            {
                if (!ConVar.Server.radiation)
                    ConVar.Server.radiation = true;
                RadiationZone newZone = new GameObject("RadiationZone").AddComponent<RadiationZone>();
                newZone.transform.position = location;
                newZone.Activate(ins.configData.Rocket.RadRadius, ins.configData.Rocket.RadIntensity);

                RadiationEntry listEntry = new RadiationEntry { zone = newZone };
                listEntry.time = ins.timer.Once(ins.configData.Rocket.RadDuration, () => ins.DestroyZone(listEntry));

                ins.radiationZones.Add(listEntry);

                if (ins.configData.Rocket.NukeAmount > 0)
                {
                    float radius = ins.configData.Rocket.NukeRadius;
                                        
                    List<BaseCombatEntity> entities = Pool.GetList<BaseCombatEntity>();
                    Vis.Entities<BaseCombatEntity>(location, radius, entities);

                    if (entities.Count > 0)
                    {
                        for (int i = entities.Count - 1; i >= 0; i--)
                        {
                            BaseCombatEntity entity = entities[i];
                            if (entity == null)
                                continue;
                            
                            float distance = Vector3.Distance(location, entity.transform.position);
                            entity.Hurt(ins.configData.Rocket.NukeAmount * (1 - (distance / radius)), DamageType.Explosion, null);
                        }
                    }
                    Pool.FreeList(ref entities);
                }
            }
        }

        public class NukeExplosion
        {
            private int count;
            private Vector3 position;

            private List<KeyValuePair<int, int>> blastRadius = new List<KeyValuePair<int, int>>
            {
                new KeyValuePair<int, int> (25, 18),
                new KeyValuePair<int, int> (15, 12),
                new KeyValuePair<int, int> (10, 10),
                new KeyValuePair<int, int> (5, 5),
                new KeyValuePair<int, int> (5, 5),
                new KeyValuePair<int, int> (5, 5),
                new KeyValuePair<int, int> (5, 5),
                new KeyValuePair<int, int> (5, 5),
                new KeyValuePair<int, int> (5, 5),
                new KeyValuePair<int, int> (10, 10),
                new KeyValuePair<int, int> (25, 18),
                new KeyValuePair<int, int> (25, 18),
                new KeyValuePair<int, int> (20, 16),
                new KeyValuePair<int, int> (20, 16)
            };

            public NukeExplosion() { }
            public NukeExplosion(Vector3 position)
            {
                this.position = position;
                BeginExplosion();
            }

            private void BeginExplosion()
            {
                Effect.server.Run(heliExplosion, position, Vector3.up, null, true);
                Next();
            }
            private void Next() => ServerMgr.Instance.StartCoroutine(CreateRing());
            IEnumerator CreateRing()
            {
                if (count == blastRadius.Count - 1)
                    yield break;
                yield return new WaitForSeconds(0.1f);
                ExplosionRing(position + ((Vector3.up * 3) * count), blastRadius[count].Key, blastRadius[count].Value);
                ++count;
                Next();
            }
            private void ExplosionRing(Vector3 position, float radius, int amount)
            {                
                int angle = 360 / amount;
                for (int i = 0; i < amount; i++)
                {
                    int a = i * angle;
                    Vector3 pos = RandomCircle(position, radius, a);
                    Effect.server.Run(c4Explosion, pos, Vector3.up, null, true);
                }
            }
            private Vector3 RandomCircle(Vector3 center, float radius, int angle)
            {
                Vector3 pos;
                pos.x = center.x + radius * Mathf.Sin(angle * Mathf.Deg2Rad);
                pos.z = center.z + radius * Mathf.Cos(angle * Mathf.Deg2Rad);
                pos.y = center.y;
                return pos;
            }
        }
        public class RadiationEntry
        {
            public RadiationZone zone;
            public Timer time;
        }
        public class RadiationZone : MonoBehaviour
        {
            SphereCollider collider;

            private void Awake()
            {
                gameObject.layer = (int)Layer.Reserved1;

                Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                collider = gameObject.AddComponent<SphereCollider>();
                collider.isTrigger = true;
            }

            public void Activate(float radius, float amount)
            {
                collider.radius = radius;

                TriggerRadiation radiation = gameObject.AddComponent<TriggerRadiation>();
                radiation.RadiationAmountOverride = amount;
                radiation.interestLayers = LayerMask.GetMask("Player (Server)");
                radiation.enabled = true;
            }

            private void OnDestroy()
            {
                Destroy(gameObject);
            }
        }

        private void DestroyZone(RadiationEntry zone)
        {
            if (radiationZones.Contains(zone))
            {
                var index = radiationZones.FindIndex(a => a.zone == zone.zone);
                radiationZones[index].time.Destroy();
                UnityEngine.Object.Destroy(radiationZones[index].zone);
                radiationZones.Remove(zone);
            }
        }
        #endregion

        #region Functions
        private void SendStrike(BasePlayer player, StrikeType type, Vector3 position, string targetName = "")
        {
            switch (type)
            {
                case StrikeType.Strike:
                    CallStrike(position, player.displayName);
                    SendReply(player, string.IsNullOrEmpty(targetName) ? string.Format(msg("strikeConfirmed", player.UserIDString), position) : string.Format(msg("strikePlayer", player.UserIDString), targetName));
                    break;
                case StrikeType.Squad:
                    CallSquad(position, player.displayName);
                    SendReply(player, string.IsNullOrEmpty(targetName) ? string.Format(msg("squadConfirmed", player.UserIDString), position) : string.Format(msg("squadPlayer", player.UserIDString), targetName));
                    break;
                case StrikeType.Napalm:
                    CallNapalm(position, player.displayName);
                    SendReply(player, string.IsNullOrEmpty(targetName) ? string.Format(msg("napalmConfirmed", player.UserIDString), position) : string.Format(msg("napalmPlayer", player.UserIDString), targetName));
                    break;
                case StrikeType.Nuke:
                    CallNuke(position, player.displayName);
                    SendReply(player, string.IsNullOrEmpty(targetName) ? string.Format(msg("nukeConfirmed", player.UserIDString), position) : string.Format(msg("nukePlayer", player.UserIDString), targetName));
                    break;
                case StrikeType.Spectre:
                    CallSpectre(position, player.displayName);
                    SendReply(player, string.IsNullOrEmpty(targetName) ? string.Format(msg("spectreConfirmed", player.UserIDString), position) : string.Format(msg("spectrePlayer", player.UserIDString), targetName));
                    break;
                default: return;
            }
        }

        private void PrepareRandomStrikes()
        {
            if (configData.Other.RandomStrikes.Count == 0) return;
            foreach (var type in configData.Other.RandomStrikes)
                availableTypes.Add(ParseType<StrikeType>(type));
            CallRandomStrike();
        }
        private void CallRandomStrike()
        {   
            timer.In(UnityEngine.Random.Range(configData.Other.RandomTimer[0], configData.Other.RandomTimer[1]), () =>
            {
                StrikeType type = availableTypes.GetRandom();               
                Vector3 position = GetRandomPosition();

                if (!string.IsNullOrEmpty(configData.Other.RandomStrikeLocations) && Spawns)
                {
                    object success = Spawns.Call("GetRandomSpawn", configData.Other.RandomStrikeLocations);
                    if (success is string)
                        PrintError($"Unable to retrieve a position from the designated spawnfile. Error: {(string)success}");
                    else position = (Vector3)success;
                }

                switch (type)
                {
                    case StrikeType.Strike:
                        CallStrike(position, string.Empty);
                        break;
                    case StrikeType.Squad:
                        CallSquad(position, string.Empty);
                        break;
                    case StrikeType.Napalm:
                        CallNapalm(position, string.Empty);
                        break;
                    case StrikeType.Nuke:
                        CallNuke(position, string.Empty);
                        break;
                    case StrikeType.Spectre:
                        CallSpectre(position, string.Empty);
                        break;
                    default: break;
                }                

                CallRandomStrike();
            });
        }
        private void CallStrike(Vector3 position, string ownerName)
        {
            CargoPlane entity = CreatePlane();
            entity.Spawn();

            BasePlane plane = entity.gameObject.AddComponent<BasePlane>();
            plane.InitializeFlightPath(position, StrikeType.Strike);

            if (configData.Other.Broadcast)
            {
                if (configData.Other.BroadcastNames && !string.IsNullOrEmpty(ownerName))
                    PrintToChat(string.Format(msg("strikeInboundName"), ownerName));
                else PrintToChat(msg("strikeInbound"));
            }
        }
        private void CallSquad(Vector3 position, string ownerName)
        {
            CargoPlane leaderEnt = CreatePlane();
            leaderEnt.Spawn();

            BasePlane leaderPlane = leaderEnt.gameObject.AddComponent<BasePlane>();
            leaderPlane.InitializeFlightPath(position, StrikeType.Squad);

            Vector3 startPos;
            Vector3 endPos;
            float secondsToTake;
            leaderPlane.GetFlightData(out startPos, out endPos, out secondsToTake);

            CargoPlane leftEnt = CreatePlane();
            leftEnt.Spawn();
            BasePlane leftPlane = leftEnt.gameObject.AddComponent<BasePlane>();
            Vector3 leftOffset = (leaderEnt.transform.right * 70) + (-leaderEnt.transform.forward * 80);
            leftPlane.SetFlightData(startPos + leftOffset, endPos + leftOffset, position + (leftOffset / 4), secondsToTake);

            CargoPlane rightEnt = CreatePlane();
            rightEnt.Spawn();
            BasePlane rightPlane = rightEnt.gameObject.AddComponent<BasePlane>();
            Vector3 rightOffset = (-leaderEnt.transform.right * 70) + (-leaderEnt.transform.forward * 80);
            rightPlane.SetFlightData(startPos + rightOffset, endPos + rightOffset, position + (rightOffset / 4), secondsToTake);

            if (configData.Other.Broadcast)
            {
                if (configData.Other.BroadcastNames && !string.IsNullOrEmpty(ownerName))
                    PrintToChat(string.Format(msg("squadInboundName"), ownerName));
                else PrintToChat(msg("squadInbound"));
            }
        }
        private void CallNuke(Vector3 position, string ownerName)
        {
            CargoPlane entity = CreatePlane();
            entity.Spawn();

            NukePlane plane = entity.gameObject.AddComponent<NukePlane>();
            plane.InitializeFlightPath(position, StrikeType.Nuke);

            if (configData.Other.Broadcast)
            {
                if (configData.Other.BroadcastNames && !string.IsNullOrEmpty(ownerName))
                    PrintToChat(string.Format(msg("nukeInboundName"), ownerName));
                else PrintToChat(msg("nukeInbound"));
            }
        }
        private void CallNapalm(Vector3 position, string ownerName)
        {
            CargoPlane entity = CreatePlane();
            entity.Spawn();

            NapalmPlane plane = entity.gameObject.AddComponent<NapalmPlane>();
            plane.InitializeFlightPath(position, StrikeType.Napalm);

            if (configData.Other.Broadcast)
            {
                if (configData.Other.BroadcastNames && !string.IsNullOrEmpty(ownerName))
                    PrintToChat(string.Format(msg("napalmInboundName"), ownerName));
                else PrintToChat(msg("napalmInbound"));
            }
        }
        private void CallSpectre(Vector3 position, string ownerName)
        {
            CargoPlane entity = CreatePlane();
            entity.Spawn();

            SpectrePlane plane = entity.gameObject.AddComponent<SpectrePlane>();
            plane.InitializeFlightPath(position, StrikeType.Spectre);

            if (configData.Other.Broadcast)
            {
                if (configData.Other.BroadcastNames && !string.IsNullOrEmpty(ownerName))
                    PrintToChat(string.Format(msg("spectreInboundName"), ownerName));
                else PrintToChat(msg("spectreInbound"));
            }
        }

        private bool CanBuyStrike(BasePlayer player, StrikeType type)
        {
            Dictionary<string, int> costToBuy = null;
            switch (type)
            {
                case StrikeType.None:
                    return false;
                case StrikeType.Strike:
                    costToBuy = configData.Buy.StrikeCost;
                    break;
                case StrikeType.Squad:
                    costToBuy = configData.Buy.SquadCost;
                    break;
                case StrikeType.Napalm:
                    costToBuy = configData.Buy.NapalmCost;
                    break;
                case StrikeType.Nuke:
                    costToBuy = configData.Buy.NukeCost;
                    break;
                case StrikeType.Spectre:
                    costToBuy = configData.Buy.SpectreCost;
                    break;               
            }

            foreach (var item in costToBuy)
            {
                if (item.Key == "RP")
                {
                    if (ServerRewards)
                    {
                        if ((int)ServerRewards.Call("CheckPoints", player.userID) < item.Value)
                        {
                            SendReply(player, string.Format(msg("buyItem", player.UserIDString), item.Value, item.Key));
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                            return false;
                        }
                    }
                }
                if (item.Key == "Economics")
                {
                    if (Economics)
                    {
                        if ((double)Economics.Call("Balance", player.userID) < item.Value)
                        {
                            SendReply(player, string.Format(msg("buyItem", player.UserIDString), item.Value, item.Key));
                            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                            return false;
                        }
                    }
                }
                if (shortnameToId.ContainsKey(item.Key))
                {
                    if (player.inventory.GetAmount(shortnameToId[item.Key]) < item.Value)
                    {
                        SendReply(player, string.Format(msg("buyItem", player.UserIDString), item.Value, shortnameToDn[item.Key]));
                        Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player.transform.position);
                        return false;
                    }
                }
            }
            Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", player.transform.position);
            return true;
        }

        private void BuyStrike(BasePlayer player, StrikeType type, Vector3 targetPosition, string targetName = "")
        {
            Dictionary<string, int> costToBuy = null;
            switch (type)
            {
                case StrikeType.None:
                    return;
                case StrikeType.Strike:
                    costToBuy = configData.Buy.StrikeCost;
                    break;
                case StrikeType.Squad:
                    costToBuy = configData.Buy.SquadCost;
                    break;
                case StrikeType.Napalm:
                    costToBuy = configData.Buy.NapalmCost;
                    break;
                case StrikeType.Nuke:
                    costToBuy = configData.Buy.NukeCost;
                    break;
                case StrikeType.Spectre:
                    costToBuy = configData.Buy.SpectreCost;
                    break;
            }

            foreach (var item in costToBuy)
            {
                if (item.Key == "RP")
                {
                    if (ServerRewards)
                        ServerRewards.Call("TakePoints", player.userID, item.Value);
                }
                if (item.Key == "Economics")
                {
                    if (Economics)
                        Economics.Call("Withdraw", player.userID, (double)item.Value);
                }
                if (shortnameToId.ContainsKey(item.Key))
                    player.inventory.Take(null, shortnameToId[item.Key], item.Value);
            }
            SendStrike(player, type, targetPosition, targetName);
        }
        #endregion

        #region NoEscape
        private bool CanCallStrike(BasePlayer player)
        {
            if (player.IsAdmin)
                return true;

            if (NoEscape)
            {
                if (configData.Other.FromRB)
                {
                    bool success = NoEscape.Call<bool>("IsRaidBlocked", player);
                    if (success)
                    {
                        SendReply(player, msg("playerRaidBlocked", player.UserIDString));
                        return false;
                    }
                }
                if (configData.Other.FromCB)
                {
                    bool success = NoEscape.Call<bool>("IsCombatBlocked", player);
                    if (success)
                    {
                        SendReply(player, msg("playerCombatBlocked", player.UserIDString));
                        return false;
                    }
                }
            }
            return true;
        }

        private bool CanStrikeTarget(BasePlayer player, BasePlayer target)
        {
            if (player.IsAdmin)
                return true;

            if (NoEscape)
            {
                if (configData.Other.AgainstRB)
                {
                    bool success = NoEscape.Call<bool>("IsRaidBlocked", target);
                    if (success)
                    {
                        SendReply(player, msg("targetRaidBlocked", player.UserIDString));
                        return false;
                    }
                }
                if (configData.Other.AgainstCB)
                {
                    bool success = NoEscape.Call<bool>("IsCombatBlocked", target);
                    if (success)
                    {
                        SendReply(player, msg("targetCombatBlocked", player.UserIDString));
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion

        #region Helpers
        private double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        private CargoPlane CreatePlane() => (CargoPlane)GameManager.server.CreateEntity(cargoPlanePrefab, new Vector3(), new Quaternion(), true);
        private bool isStrikePlane(CargoPlane plane) => plane.GetComponent<BasePlane>();
        private bool isStrikeSignal(BaseEntity entity) => strikeSignals.Contains(entity);
        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        private Vector3 GetRandomPosition()
        {
            float mapSize = (TerrainMeta.Size.x / 2) - 600f;

            float randomX = UnityEngine.Random.Range(-mapSize, mapSize);
            float randomY = UnityEngine.Random.Range(-mapSize, mapSize);

            return new Vector3(randomX, 0f, randomY);
        }

        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
        }

        private void AddCooldownData(BasePlayer player, StrikeType type)
        {
            if (!configData.Cooldown.Enabled) return;

            if (!storedData.cooldowns.ContainsKey(player.userID))
                storedData.cooldowns.Add(player.userID, new CooldownData());

            if (configData.Cooldown.Combine)
                storedData.cooldowns[player.userID].globalCd = GrabCurrentTime() + configData.Cooldown.Time;
            else
            {
                switch (type)
                {
                    case StrikeType.None:
                        return;
                    case StrikeType.Strike:
                        storedData.cooldowns[player.userID].strikeCd = GrabCurrentTime() + configData.Cooldown.Times[type.ToString()];
                        return;
                    case StrikeType.Squad:
                        storedData.cooldowns[player.userID].squadCd = GrabCurrentTime() + configData.Cooldown.Times[type.ToString()];
                        return;
                    case StrikeType.Napalm:
                        storedData.cooldowns[player.userID].napalmCd = GrabCurrentTime() + configData.Cooldown.Times[type.ToString()];
                        return;
                    case StrikeType.Nuke:
                        storedData.cooldowns[player.userID].nukeCd = GrabCurrentTime() + configData.Cooldown.Times[type.ToString()];
                        return;
                    case StrikeType.Spectre:
                        storedData.cooldowns[player.userID].spectreCd = GrabCurrentTime() + configData.Cooldown.Times[type.ToString()];
                        return;
                }
            }            
        }

        private List<BasePlayer> FindPlayer(string arg)
        {
            List<BasePlayer> foundPlayers = new List<BasePlayer>();

            ulong steamid;
            ulong.TryParse(arg, out steamid);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (steamid != 0L)
                {
                    if (player.userID == steamid)
                    {
                        foundPlayers.Clear();
                        foundPlayers.Add(player);
                        return foundPlayers;
                    }
                }
                if (player.displayName.ToLower().Contains(arg.ToLower()))                
                    foundPlayers.Add(player);                
            }
            return foundPlayers;
        }

        private T ParseType<T>(string type)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), type, true);
            }
            catch
            {
                PrintError($"INVALID OPTION! The value \"{type}\" is an incorrect selection.\nAvailable options are: {Enum.GetNames(typeof(T)).ToSentence()}");
                return default(T);
            }
        }
        #endregion

        #region Commands
        [ChatCommand("strike")]
        void cmdAirstrike(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                SendReply(player, string.Format("<color=#ce422b>Advanced Airstrike  </color><color=#939393>v.</color><color=#ce422b>{0} </color><color=#939393>-- </color><color=#ce422b>k1lly0u @ chaoscode.io</color>", Version));
                SendReply(player, msg("help1", player.UserIDString));
                SendReply(player, msg("help2", player.UserIDString));
                SendReply(player, msg("help6", player.UserIDString));
                if (player.IsAdmin || configData.Other.AllowCoords)
                    SendReply(player, msg("help7", player.UserIDString));
                SendReply(player, msg("help3", player.UserIDString));
                SendReply(player, msg("help4", player.UserIDString));
                if (player.IsAdmin || configData.Other.AllowCoords)
                    SendReply(player, msg("help5", player.UserIDString));
                return;
            }
            if (args.Length >= 2)
            {
                var time = GrabCurrentTime();
                StrikeType type = ParseType<StrikeType>(args[1]);
                if (type == StrikeType.None)
                {
                    SendReply(player, msg("invalidType", player.UserIDString));
                    return;
                }

                if (!HasPermission(player, "advairstrike.ignorecooldown"))
                {
                    if (configData.Cooldown.Enabled)
                    {
                        CooldownData data;
                        if (storedData.cooldowns.TryGetValue(player.userID, out data))
                        {
                            double nextUse = configData.Cooldown.Combine ? data.globalCd : type == StrikeType.Strike ? data.strikeCd : type == StrikeType.Squad ? data.squadCd : type == StrikeType.Napalm ? data.napalmCd : type == StrikeType.Nuke ? data.nukeCd : data.spectreCd;
                            if (nextUse > time)
                            {
                                double remaining = nextUse - time;
                                SendReply(player, string.Format(msg("onCooldown", player.UserIDString), FormatTime(remaining)));
                                return;
                            }
                        }
                    }
                }

                if (!CanCallStrike(player))
                    return;

                switch (args[0].ToLower())
                {
                    case "signal":
                        if ((type == StrikeType.Strike && configData.Other.SignalStrike) || (type == StrikeType.Squad && configData.Other.SignalSquad) || (type == StrikeType.Napalm && configData.Other.SignalNapalm) || (type == StrikeType.Nuke && configData.Other.SignalNuke) || (type == StrikeType.Spectre && configData.Other.SignalSpectre))
                        {
                            if (!HasPermission(player, $"advairstrike.signal.{type.ToString().ToLower()}"))
                            {
                                SendReply(player, msg("noPerms", player.UserIDString));
                                return;
                            }

                            if (toggleList.ContainsKey(player.userID))
                                toggleList[player.userID] = type;
                            else toggleList.Add(player.userID, type);
                            SendReply(player, msg("signalReady", player.UserIDString));
                        }
                        else SendReply(player, msg("typeDisabledSignal", player.UserIDString));
                        return;
                    case "buy":
                        if ((type == StrikeType.Strike && configData.Buy.PermissionStrike) || (type == StrikeType.Squad && configData.Buy.PermissionSquad) || (type == StrikeType.Napalm && configData.Buy.PermissionNapalm) || (type == StrikeType.Nuke && configData.Buy.PermissionNuke) || (type == StrikeType.Spectre && configData.Buy.PermissionSpectre))
                        {
                            if (!HasPermission(player, $"advairstrike.purchase.{type.ToString().ToLower()}"))
                            {
                                SendReply(player, msg("noPerms", player.UserIDString));
                                return;
                            }

                            if (CanBuyStrike(player, type))
                            {
                                Vector3 position = new Vector3();
                                string targetName = string.Empty;
                                if (args.Length == 4)
                                {
                                    if (player.IsAdmin || configData.Other.AllowCoords)
                                    {
                                        float x, z;
                                        if (!float.TryParse(args[2], out x) || !float.TryParse(args[3], out z))
                                        {
                                            SendReply(player, msg("invCoords", player.UserIDString));
                                            return;
                                        }
                                        else
                                        {
                                            position = new Vector3(x, 0, z);
                                            position.y = TerrainMeta.HeightMap.GetHeight(position);
                                        }
                                    }
                                    else
                                    {
                                        SendReply(player, msg("invalidSyntax", player.UserIDString));
                                        return;
                                    }
                                }
                                else if (args.Length == 3)
                                {
                                    var players = FindPlayer(args[2]);
                                    if (players.Count > 1)
                                    {
                                        SendReply(player, msg("multiplePlayers", player.UserIDString));
                                        return;
                                    }
                                    else if (players.Count == 0)
                                    {
                                        SendReply(player, msg("noPlayers", player.UserIDString));
                                        return;
                                    }
                                    else
                                    {
                                        BasePlayer targetPlayer = players[0];

                                        if (!CanStrikeTarget(player, targetPlayer))
                                            return;

                                        if (configData.Cooldown.PlayerTime > 0)
                                        {
                                            if (playerCooldowns.ContainsKey(targetPlayer.userID))
                                            {
                                                if (time < playerCooldowns[targetPlayer.userID])
                                                {
                                                    SendReply(player, msg("playerCooldown", player.UserIDString));
                                                    return;
                                                }
                                            }
                                            playerCooldowns[targetPlayer.userID] = time + configData.Cooldown.PlayerTime;
                                        }
                                        position = players[0].transform.position;
                                        targetName = targetPlayer.displayName;
                                    }
                                }
                                else position = player.transform.position;

                                BuyStrike(player, type, position, targetName);
                                AddCooldownData(player, type);
                            }
                        }
                        else SendReply(player, msg("typeDisabledBuy", player.UserIDString));
                        return;
                    case "call":
                        if (HasPermission(player, $"advairstrike.chat.{type.ToString().ToLower()}"))
                        {
                            Vector3 position = new Vector3();
                            string targetName = string.Empty;
                            if (args.Length == 4)
                            {
                                if (player.IsAdmin || configData.Other.AllowCoords)
                                {
                                    float x, z;
                                    if (!float.TryParse(args[2], out x) || !float.TryParse(args[3], out z))
                                    {
                                        SendReply(player, msg("invCoords", player.UserIDString));
                                        return;
                                    }
                                    else position = new Vector3(x, 0, z);
                                }
                                else
                                {
                                    SendReply(player, msg("invalidSyntax", player.UserIDString));
                                    return;
                                }
                            }
                            else if (args.Length == 3)
                            {
                                List<BasePlayer> players = FindPlayer(args[2]);
                                if (players.Count > 1)
                                {
                                    SendReply(player, msg("multiplePlayers", player.UserIDString));
                                    return;
                                }
                                else if (players.Count == 0)
                                {
                                    SendReply(player, msg("noPlayers", player.UserIDString));
                                    return;
                                }
                                else
                                {
                                    BasePlayer targetPlayer = players[0];

                                    if (!CanStrikeTarget(player, targetPlayer))
                                        return;

                                    if (configData.Cooldown.PlayerTime > 0)
                                    {
                                        if (playerCooldowns.ContainsKey(targetPlayer.userID))
                                        {
                                            if (time < playerCooldowns[targetPlayer.userID])
                                            {
                                                SendReply(player, msg("playerCooldown", player.UserIDString));
                                                return;
                                            }
                                        }
                                        playerCooldowns[targetPlayer.userID] = time + configData.Cooldown.PlayerTime;
                                    }
                                    position = players[0].transform.position;
                                    targetName = targetPlayer.displayName;
                                }
                            }
                            else if (args.Length == 2)
                            {
                                position = player.transform.position;
                            }
                            else
                            {
                                SendReply(player, msg("invalidSyntax", player.UserIDString));
                                return;
                            }

                            SendStrike(player, type, position, targetName);
                            AddCooldownData(player, type);
                        }
                        else SendReply(player, msg("noPerms", player.UserIDString));
                        return;
                    default:
                        break;
                }
            }
        }     

        [ConsoleCommand("strike")]
        void ccmdAirstrike(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;
            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "strike <strike|squad|napalm|nuke|spectre> <x> <z> - Call the specified airstrike type to the target position");
                SendReply(arg, "strike <strike|squad|napalm|nuke|spectre> <playername> - Call the specified airstrike type to the target player");
                SendReply(arg, "strike <strike|squad|napalm|nuke|spectre> random - Call a random airstrike of the specified type");
                return;
            }

            StrikeType type = ParseType<StrikeType>(arg.Args[0]);
            if (type == StrikeType.None)
            {
                SendReply(arg, msg("invalidType"));
                return;
            }

            Vector3 position = Vector3.zero;

            if (arg.Args[1].ToLower() == "random")
                position = GetRandomPosition();
            else if (arg.Args.Length == 3)
            {
                float x, z;
                if (!float.TryParse(arg.Args[1], out x) || !float.TryParse(arg.Args[2], out z))
                {
                    SendReply(arg, "Invalid co-ordinates set. You must enter number values for X and Z");
                    return;
                }
                else position = new Vector3(x, 0, z);
            }
            else if (arg.Args.Length == 2)
            {
                var players = FindPlayer(arg.Args[1]);
                if (players.Count > 1)
                {
                    SendReply(arg, "Multiple players found");
                    return;
                }
                else if (players.Count == 0)
                {
                    SendReply(arg, "No players found");
                    return;
                }
                else position = players[0].transform.position;
            }

            switch (type)
            {
                case StrikeType.Strike:
                    CallStrike(position, string.Empty);
                    SendReply(arg, string.Format("Airstrike confirmed at co-ordinates: {0}", position));
                    break;
                case StrikeType.Squad:
                    CallSquad(position, string.Empty);
                    SendReply(arg, string.Format("Squadstrike confirmed at co-ordinates: {0}", position));
                    break;
                case StrikeType.Napalm:
                    CallNapalm(position, string.Empty);
                    SendReply(arg, string.Format("Napalm strike confirmed at co-ordinates: {0}", position));
                    break;
                case StrikeType.Nuke:
                    CallNuke(position, string.Empty);
                    SendReply(arg, string.Format("Nuke strike confirmed at co-ordinates: {0}", position));
                    break;
                case StrikeType.Spectre:
                    CallSpectre(position, string.Empty);
                    SendReply(arg, string.Format("Spectre strike confirmed at co-ordinates: {0}", position));
                    break;
                default: return;
            }
        }
        #endregion

        #region Config 
        private ConfigData configData;
        class RocketOptions
        {
            [JsonProperty(PropertyName = "Speed of the rocket")]
            public float Speed { get; set; }
            [JsonProperty(PropertyName = "Damage modifier")]
            public float Damage { get; set; }
            [JsonProperty(PropertyName = "Accuracy of rocket (a lower number is more accurate)")]
            public float Accuracy { get; set; }
            [JsonProperty(PropertyName = "Accuracy of spectre rocket (a lower number is more accurate)")]
            public float SpectreAccuracy { get; set; }
            [JsonProperty(PropertyName = "Interval between rockets (seconds)")]
            public float Interval { get; set; }
            [JsonProperty(PropertyName = "Interval between rockets (seconds) (Spectre Strike)")]
            public float IntervalSpectre { get; set; }
            [JsonProperty(PropertyName = "Type of rocket (Normal, Napalm)")]
            public string Type { get; set; }
            [JsonProperty(PropertyName = "Use both rocket types")]
            public bool Mixed { get; set; }
            [JsonProperty(PropertyName = "Chance of a fire rocket (when using both types)")]
            public int FireChance { get; set; }
            [JsonProperty(PropertyName = "Amount of rockets to fire")]
            public int Amount { get; set; }
            [JsonProperty(PropertyName = "Napalm lifetime (seconds) (Napalm Strike)")]
            public float FireLife { get; set; }
            [JsonProperty(PropertyName = "Radiation lifetime (seconds) (Nuke Strike)")]
            public float RadDuration { get; set; }
            [JsonProperty(PropertyName = "Radiation intensity (Nuke Strike)")]
            public float RadIntensity { get; set; }
            [JsonProperty(PropertyName = "Radiation radius (Nuke Strike)")]
            public float RadRadius { get; set; }
            [JsonProperty(PropertyName = "Damage radius (Nuke Strike)")]
            public float NukeRadius { get; set; }           
            [JsonProperty(PropertyName = "Damage amount (Nuke Strike)")]
            public float NukeAmount { get; set; }
        }
        class CannonOptions
        {            
            [JsonProperty(PropertyName = "Damage modifier")]
            public float Damage { get; set; }
            [JsonProperty(PropertyName = "Accuracy of shell (a lower number is more accurate)")]
            public float Accuracy { get; set; }
            [JsonProperty(PropertyName = "Accuracy of spectre shells (a lower number is more accurate)")]
            public float SpectreAccuracy { get; set; }
            [JsonProperty(PropertyName = "Interval between shells (seconds)")]
            public float Interval { get; set; }
            [JsonProperty(PropertyName = "Interval between shells (seconds) (Spectre Strike)")]
            public float IntervalSpectre { get; set; }       
            [JsonProperty(PropertyName = "Amount of shells to fire")]
            public int Amount { get; set; }           
        }
        class GunOptions
        {
            [JsonProperty(PropertyName = "Amount to fire")]
            public int Amount { get; set; }
            [JsonProperty(PropertyName = "Bullet fire rate")]
            public float FireRate { get; set; }
            [JsonProperty(PropertyName = "Is explosive ammunition")]
            public bool IsExplosive { get; set; }
            [JsonProperty(PropertyName = "Accuracy of bullet spread")]
            public float Accuracy { get; set; }
            [JsonProperty(PropertyName = "Damage of bullets")]
            public float Damage { get; set; }
        }
        class CooldownOptions
        {
            [JsonProperty(PropertyName = "Use cooldown timers")]
            public bool Enabled { get; set; }
            [JsonProperty(PropertyName = "Use a global cooldown for each type")]
            public bool Combine { get; set; }
            [JsonProperty(PropertyName = "Global cooldown time")]
            public int Time { get; set; }
            [JsonProperty(PropertyName = "Cooldown time that commands can be used on other players")]
            public int PlayerTime { get; set; }
            [JsonProperty(PropertyName = "Strike cooldown times (seconds)")]
            public Dictionary<string, int> Times { get; set; }            
        }
        class PlaneOptions
        {
            [JsonProperty(PropertyName = "Flight speed (meters per second)")]
            public float Speed { get; set; }
            [JsonProperty(PropertyName = "Distance from target to engage")]
            public float Distance { get; set; }
            [JsonProperty(PropertyName = "Flight radius (Spectre Strike)")]
            public float FlightRadius { get; set; }
        }
        class BuyOptions
        {
            [JsonProperty(PropertyName = "Can purchase standard strike")]
            public bool StrikeEnabled { get; set; }
            [JsonProperty(PropertyName = "Can purchase squad strike")]
            public bool SquadEnabled { get; set; }
            [JsonProperty(PropertyName = "Can purchase napalm strike")]
            public bool NapalmEnabled { get; set; }
            [JsonProperty(PropertyName = "Can purchase nuke strike")]
            public bool NukeEnabled { get; set; }
            [JsonProperty(PropertyName = "Can purchase spectre strike")]
            public bool SpectreEnabled { get; set; }

            [JsonProperty(PropertyName = "Require permission to purchase strike")]
            public bool PermissionStrike { get; set; }
            [JsonProperty(PropertyName = "Require permission to purchase squad strike")]
            public bool PermissionSquad { get; set; }
            [JsonProperty(PropertyName = "Require permission to purchase napalm strike")]
            public bool PermissionNapalm { get; set; }
            [JsonProperty(PropertyName = "Require permission to purchase nuke strike")]
            public bool PermissionNuke { get; set; }
            [JsonProperty(PropertyName = "Require permission to purchase spectre strike")]
            public bool PermissionSpectre { get; set; }

            [JsonProperty(PropertyName = "Cost to purchase a standard strike (shortname, amount)")]
            public Dictionary<string, int> StrikeCost { get; set; }
            [JsonProperty(PropertyName = "Cost to purchase a squad strike (shortname, amount)")]
            public Dictionary<string, int> SquadCost { get; set; }
            [JsonProperty(PropertyName = "Cost to purchase a napalm strike (shortname, amount)")]
            public Dictionary<string, int> NapalmCost { get; set; }
            [JsonProperty(PropertyName = "Cost to purchase a nuke strike (shortname, amount)")]
            public Dictionary<string, int> NukeCost { get; set; }
            [JsonProperty(PropertyName = "Cost to purchase a spectre strike (shortname, amount)")]
            public Dictionary<string, int> SpectreCost { get; set; }
        }
        class OtherOptions
        {
            [JsonProperty(PropertyName = "Allow players to call strikes using co-ordinates")]
            public bool AllowCoords { get; set; }

            [JsonProperty(PropertyName = "Broadcast strikes to chat")]
            public bool Broadcast { get; set; }
            [JsonProperty(PropertyName = "Broadcast player names when a strike is called")]
            public bool BroadcastNames { get; set; }

            [JsonProperty(PropertyName = "Can call standard strikes using a supply signal")]
            public bool SignalStrike { get; set; }
            [JsonProperty(PropertyName = "Can call squad strikes using a supply signal")]
            public bool SignalSquad { get; set; }
            [JsonProperty(PropertyName = "Can call napalm strikes using a supply signal")]
            public bool SignalNapalm { get; set; }
            [JsonProperty(PropertyName = "Can call nuke strikes using a supply signal")]
            public bool SignalNuke { get; set; }
            [JsonProperty(PropertyName = "Can call spectre strikes using a supply signal")]
            public bool SignalSpectre { get; set; }

            [JsonProperty(PropertyName = "Random airstrike locations (Spawnfile name)")]
            public string RandomStrikeLocations { get; set; }

            [JsonProperty(PropertyName = "Random airstrike types (Strike, Squad, Napalm, Nuke, Spectre)")]
            public List<string> RandomStrikes { get; set; }  

            [JsonProperty(PropertyName = "Random timer (minimum, maximum. In seconds)")]
            public int[] RandomTimer { get; set; }

            [JsonProperty(PropertyName = "NoEscape - Prevent strikes against RaidBlocked players")]
            public bool AgainstRB { get; set; }
            [JsonProperty(PropertyName = "NoEscape - Prevent strikes from RaidBlocked players")]
            public bool FromRB { get; set; }
            [JsonProperty(PropertyName = "NoEscape - Prevent strikes against CombatBlocked players")]
            public bool AgainstCB { get; set; }
            [JsonProperty(PropertyName = "NoEscape - Prevent strikes from CombatBlocked players")]
            public bool FromCB { get; set; }
        }
        class ConfigData
        {
            [JsonProperty(PropertyName = "Strike Weapon Types (Rocket, Bullet, Cannon, Combined)")]
            public Dictionary<StrikeType, string> Types { get; set; }
            [JsonProperty(PropertyName = "Rocket Options")]
            public RocketOptions Rocket { get; set; }
            [JsonProperty(PropertyName = "Cannon Options")]
            public CannonOptions Cannon { get; set; }
            [JsonProperty(PropertyName = "Strike Gun Options")]
            public Dictionary<StrikeType, GunOptions> Gun { get; set; }
            [JsonProperty(PropertyName = "Cooldown Options")]
            public CooldownOptions Cooldown { get; set; }
            [JsonProperty(PropertyName = "Plane Options")]
            public PlaneOptions Plane { get; set; }
            [JsonProperty(PropertyName = "Purchase Options")]
            public BuyOptions Buy { get; set; }
            [JsonProperty(PropertyName = "Other Options")]
            public OtherOptions Other { get; set; }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Buy = new BuyOptions
                {
                    SquadCost = new Dictionary<string, int>
                    {
                        ["metal.refined"] = 100,
                        ["techparts"] = 50,
                        ["targeting.computer"] = 1
                    },
                    SquadEnabled = true,
                    PermissionSquad = true,
                    StrikeCost = new Dictionary<string, int>
                    {
                        ["metal.refined"] = 50,
                        ["targeting.computer"] = 1
                    },
                    StrikeEnabled = true,
                    PermissionStrike = true,
                    SpectreCost = new Dictionary<string, int>
                    {
                        ["metal.refined"] = 150,
                        ["techparts"] = 100,
                        ["targeting.computer"] = 2
                    },
                    SpectreEnabled = true,
                    PermissionSpectre = true,
                    NapalmCost = new Dictionary<string, int>
                    {
                        ["lowgradefuel"] = 500,
                        ["metal.refined"] = 100,
                        ["targeting.computer"] = 1
                    },
                    NapalmEnabled = true,
                    PermissionNapalm = true,
                    NukeCost = new Dictionary<string, int>
                    {
                        ["explosives"] = 20,
                        ["metal.refined"] = 200,
                        ["targeting.computer"] = 2
                    },
                    NukeEnabled = true,
                    PermissionNuke = true
                },
                Cooldown = new CooldownOptions
                {
                    Combine = false,
                    Enabled = true,
                    Time = 3600,
                    Times = new Dictionary<string, int>
                    {
                        [StrikeType.Strike.ToString()] = 3600,
                        [StrikeType.Squad.ToString()] = 3600,
                        [StrikeType.Napalm.ToString()] = 3600,
                        [StrikeType.Nuke.ToString()] = 3600,
                        [StrikeType.Spectre.ToString()] = 3600
                    },
                    PlayerTime = 600
                },
                Other = new OtherOptions
                {
                    AllowCoords = true,
                    Broadcast = true,
                    BroadcastNames = false,
                    SignalSquad = true,
                    SignalStrike = true,
                    SignalNapalm = true,
                    SignalNuke = true,
                    SignalSpectre = true,
                    RandomStrikes = new List<string>() { StrikeType.Strike.ToString(), StrikeType.Squad.ToString(), StrikeType.Napalm.ToString(), StrikeType.Nuke.ToString(), StrikeType.Spectre.ToString() },
                    RandomStrikeLocations = string.Empty,
                    RandomTimer = new int[] { 1800, 3600 },
                    AgainstCB = false,
                    AgainstRB = false,
                    FromCB = false,
                    FromRB = false
                },
                Plane = new PlaneOptions
                {
                    Distance = 900,
                    Speed = 105,
                    FlightRadius = 300
                },
                Rocket = new RocketOptions
                {
                    Accuracy = 1.5f,
                    Amount = 15,
                    Damage = 1.0f,
                    FireChance = 4,
                    Interval = 0.6f,
                    Mixed = true,
                    Speed = 110f,
                    Type = "Normal",
                    FireLife = 60f,
                    IntervalSpectre = 1.5f,
                    NukeAmount = 100,
                    NukeRadius = 20,
                    SpectreAccuracy = 7f,
                    RadDuration = 30,
                    RadIntensity = 30,
                    RadRadius = 30
                },
                Cannon = new CannonOptions
                {
                    Accuracy = 1.5f,
                    Amount = 15,
                    Damage = 1.0f,
                    Interval = 0.6f,
                    IntervalSpectre = 1.5f,
                    SpectreAccuracy = 7f
                },
                Types = new Dictionary<StrikeType, string>
                {
                    [StrikeType.Strike] = FireType.Bullet.ToString(),
                    [StrikeType.Squad] = FireType.Rocket.ToString(),
                    [StrikeType.Spectre] = FireType.Combined.ToString()
                },
                Gun = new Dictionary<StrikeType, GunOptions>
                {
                    [StrikeType.Strike] = new GunOptions
                    {
                        Accuracy = 15f,
                        Amount = 100,
                        Damage = 15f,
                        FireRate = 0.07f,
                        IsExplosive = false
                    },
                    [StrikeType.Squad] = new GunOptions
                    {
                        Accuracy = 15f,
                        Amount = 50,
                        Damage = 15f,
                        FireRate = 0.2f,
                        IsExplosive = true
                    },
                    [StrikeType.Spectre] = new GunOptions
                    {
                        Accuracy = 15f,
                        Amount = 300,
                        Damage = 15f,
                        FireRate = 0.115f,
                        IsExplosive = true
                    },
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(0, 1, 55))
                configData.Types = baseConfig.Types;

            if (configData.Version < new VersionNumber(0, 1, 60))
                configData.Cannon = baseConfig.Cannon;

            if (configData.Version < new VersionNumber(0, 1, 65))
                configData.Cooldown.PlayerTime = baseConfig.Cooldown.PlayerTime;

            if (configData.Version < new VersionNumber(0, 1, 70))
                configData.Other.BroadcastNames = baseConfig.Other.BroadcastNames;

            if (configData.Version < new VersionNumber(0, 1, 73))
            {
                configData.Other.AllowCoords = true;
                configData.Other.AgainstCB = false;
                configData.Other.AgainstRB = false;
                configData.Other.FromCB = false;
                configData.Other.FromRB = false;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }        
        #endregion

        #region Data Management
        private void SaveData() => data.WriteObject(storedData);

        private void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }

            if (storedData.cooldowns == null)
                storedData.cooldowns = new Dictionary<ulong, CooldownData>();
        }

        private void ClearData()
        {
            storedData = new StoredData();
            SaveData();
        }

        private class StoredData
        {
            public Dictionary<ulong, CooldownData> cooldowns = new Dictionary<ulong, CooldownData>();
        }

        private class CooldownData
        {
            public double strikeCd, squadCd, napalmCd, nukeCd, spectreCd, globalCd;
        }
        #endregion

        #region Localization
        private string msg(string key, string playerId = null) => lang.GetMessage(key, this, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["strikeConfirmed"] = "<color=#939393>Airstrike confirmed at co-ordinates: </color><color=#ce422b>{0}</color>",
            ["squadConfirmed"] = "<color=#939393>Squadstrike confirmed at co-ordinates: </color><color=#ce422b>{0}</color>",
            ["napalmConfirmed"] = "<color=#939393>Napalm strike confirmed at co-ordinates: </color><color=#ce422b>{0}</color>",
            ["nukeConfirmed"] = "<color=#939393>Nuke strike confirmed at co-ordinates: </color><color=#ce422b>{0}</color>",
            ["spectreConfirmed"] = "<color=#939393>Spectre strike confirmed at co-ordinates: </color><color=#ce422b>{0}</color>",
            ["strikePlayer"] = "<color=#939393>Airstrike confirmed at <color=#ce422b>{0}</color>'s co-ordinates</color>",
            ["squadPlayer"] = "<color=#939393>Squadstrike confirmed at <color=#ce422b>{0}</color>'s co-ordinates</color>",
            ["napalmPlayer"] = "<color=#939393>Napalm strike confirmed at <color=#ce422b>{0}</color>'s co-ordinates</color>",
            ["nukePlayer"] = "<color=#939393>Nuke strike confirmed at <color=#ce422b>{0}</color>'s co-ordinates</color>",
            ["spectrePlayer"] = "<color=#939393>Spectre strike confirmed at <color=#ce422b>{0}</color>'s co-ordinates</color>",
            ["strikeInbound"] = "<color=#ce422b>Air strike inbound!</color>",
            ["squadInbound"] = "<color=#ce422b>Squad strike inbound!</color>",
            ["napalmInbound"] = "<color=#ce422b>Napalm strike inbound!</color>",
            ["nukeInbound"] = "<color=#ce422b>Nuke strike inbound!</color>",
            ["spectreInbound"] = "<color=#ce422b>Spectre strike inbound!</color>",
            ["strikeInboundName"] = "<color=#ce422b>{0} called a air strike!</color>",
            ["squadInboundName"] = "<color=#ce422b>{0} called a squad strike!</color>",
            ["napalmInboundName"] = "<color=#ce422b>{0} called a napalm strike!</color>",
            ["nukeInboundName"] = "<color=#ce422b>{0} called a nuke strike!</color>",
            ["spectreInboundName"] = "<color=#ce422b>{0} called a spectre strike!</color>",
            ["buyItem"] = "<color=#939393>You need another </color><color=#ce422b>{0} {1}</color><color=#939393> to buy this strike</color>",
            ["help1"] = "<color=#ce422b>/strike signal <strike|squad|napalm|nuke|spectre></color><color=#939393> - Use a supply signal to mark a airstrike position</color>",
            ["help2"] = "<color=#ce422b>/strike buy <strike|squad|napalm|nuke|spectre></color><color=#939393> - Purchase a airstrike on your position</color>",
            ["help6"] = "<color=#ce422b>/strike buy <strike|squad|napalm|nuke|spectre> \"player name\"</color><color=#939393> - Purchase a airstrike on the target player</color>",
            ["help7"] = "<color=#ce422b>/strike buy <strike|squad|napalm|nuke|spectre> \"x\" \"z\"</color><color=#939393> - Purchase a airstrike on your position</color>",
            ["help3"] = "<color=#ce422b>/strike call <strike|squad|napalm|nuke|spectre></color><color=#939393> - Call a airstrike on target position</color>",
            ["help4"] = "<color=#ce422b>/strike call <strike|squad|napalm|nuke|spectre> \"player name\" </color><color=#939393>- Call a airstrike to the target player</color>",
            ["help5"] = "<color=#ce422b>/strike call <strike|squad|napalm|nuke|spectre> \"x\" \"z\" </color><color=#939393>- Call a airstrike to the target position</color>",
            ["onCooldown"] = "<color=#939393>You must wait another </color><color=#ce422b>{0}</color><color=#939393> before calling this type again</color>",
            ["noPerms"] = "<color=#939393>You do not have permission to use that strike type</color>",
            ["signalReady"] = "<color=#939393>Throw a supply signal to call a strike</color>",
            ["typeDisabledSignal"] = "<color=#939393>That strike type can not be called with supply signals</color>",
            ["typeDisabledBuy"] = "<color=#939393>That strike type can not be purchased</color>",
            ["invCoords"] = "<color=#939393>Invalid co-ordinates set. You must enter number values for X and Z</color>",
            ["multiplePlayers"] = "<color=#939393>Multiple players found</color>",
            ["noPlayers"] = "<color=#939393>No players found</color>",
            ["invalidType"] = "<color=#939393>Invalid strike type selected</color>",
            ["invalidSyntax"] = "<color=#939393>Invalid command syntax!</color>",
            ["playerCooldown"] = "<color=#939393>The specified player had a strike launched at them recently. Try again later!</color>",
            ["playerRaidBlocked"] = "<color=#939393>You can not call a strike when you are <color=#ce422b>Raid Blocked</color></color>",
            ["playerCombatBlocked"] = "<color=#939393>You can not call a strike when you are <color=#ce422b>Combat Blocked</color></color>",
            ["targetRaidBlocked"] = "<color=#939393>You can not call a strike on a <color=#ce422b>Raid Blocked</color> player</color>",
            ["targetCombatBlocked"] = "<color=#939393>You can not call a strike on a <color=#ce422b>Combat Blocked</color> player</color>",
        };
        #endregion
    }
}
