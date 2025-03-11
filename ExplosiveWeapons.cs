using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ConVar;
using System.IO;
using System.Text;
using Network;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("ExplosiveWeapons", "Cameron", "1.0.8")]
    [Description("Custom rocket launchers and weapon to make sutff go boom boom, flash flash and owow")]
    public class ExplosiveWeapons : CovalencePlugin
    {
        int bigRocketCount = 20;
        int littleRocketCount = 12;

        bool jerichoBlowUp = true;
        bool heatSeekerBlowUp = false;
        bool followBlowUp = false;
        bool javalinBlowUp = false;


        int xSize = 100;
        int ySize = 1;
        int maxRange = 250;

        int scatterSize = 3;

        float flashDuration = 4.0f;

        ulong jericoSkin = 2656578790;
        ulong seekerSkin = 2657299588;
        ulong trackSkin = 2657312965;
        ulong javalin = 2657315108;
        ulong multiGrenadeSkin = 2657418159;
        ulong molatoveGrenade = 2657408625;
        ulong flashBangSkin = 2657412999;
        ulong healingSkin = 2657414983;
        ulong impactNadeSkin = 2657418820;
        ulong stunGrenadeSkin = 2657417547;

        int instaHealAmount = 15;
        int passiveHealAmount = 50;

        bool spawnFlares = true;
        int stunDuration = 4;

        HashSet<ulong> skins = new HashSet<ulong>();
        #region Config

        private void Init()
        {
            permission.RegisterPermission("explosiveweapons.notarget", this);

            bigRocketCount = int.Parse(Config["jerichoBigRocketCount"].ToString());
            littleRocketCount = int.Parse(Config["jerichoSmallRocketCount"].ToString());
            xSize = int.Parse(Config["jerichoXSize"].ToString());
            ySize = int.Parse(Config["jerichoYSize"].ToString());
            maxRange = int.Parse(Config["maxRangeForJerichoAndJavalin"].ToString());
            scatterSize = int.Parse(Config["scatterSize"].ToString());
            flashDuration = int.Parse(Config["flashDuration"].ToString());

            jerichoBlowUp = (bool)Config["jericoDestroyOnShoot"];
            heatSeekerBlowUp = (bool)Config["heatSeekerDestroyOnShoot"];
            followBlowUp = (bool)Config["followDestroyOnShoot"];
            javalinBlowUp = (bool)Config["javalinDestroyOnShoot"];

            instaHealAmount = int.Parse(Config["instaHealAmount"].ToString());
            passiveHealAmount = int.Parse(Config["passiveHealAmount"].ToString());

            stunDuration = int.Parse(Config["stunDuration"].ToString());
            //skins

            jericoSkin = ulong.Parse(Config["jericoSkin"].ToString());
            seekerSkin = ulong.Parse(Config["seekerSkin"].ToString());
            trackSkin = ulong.Parse(Config["heatSeekSkin"].ToString());
            javalin = ulong.Parse(Config["javalinSkin"].ToString());
            multiGrenadeSkin = ulong.Parse(Config["scatterNadeSkin"].ToString());
            molatoveGrenade = ulong.Parse(Config["molatoveSkin"].ToString());
            flashBangSkin = ulong.Parse(Config["flashBangSkin"].ToString());
            healingSkin = ulong.Parse(Config["healingSkin"].ToString());
            impactNadeSkin = ulong.Parse(Config["impactNadeSkin"].ToString());
            stunGrenadeSkin = ulong.Parse(Config["stunGrenadeSkin"].ToString());


            if (Config["SpawnFlares"] == null)
            {
                Config["SpawnFlares"] = true;
                SaveConfig();

            }

            
            spawnFlares = (bool)Config["SpawnFlares"];

            skins.Add(jericoSkin);
            skins.Add(seekerSkin);
            skins.Add(trackSkin);
            skins.Add(javalin);
            skins.Add(multiGrenadeSkin);
            skins.Add(molatoveGrenade);
            skins.Add(flashBangSkin);
            skins.Add(healingSkin);
            skins.Add(impactNadeSkin);
            skins.Add(stunGrenadeSkin);

        }


        protected override void LoadDefaultConfig()
        {
            Config["jerichoBigRocketCount"] = 20;
            Config["jerichoSmallRocketCount"] = 12;
            Config["jerichoXSize"] = 100;
            Config["jerichoYSize"] = 1;
            Config["maxRangeForJerichoAndJavalin"] = 250;
            Config["scatterSize"] = 3;
            Config["flashDuration"] = 4.0;

            Config["jericoDestroyOnShoot"] = true;
            Config["heatSeekerDestroyOnShoot"] = false;
            Config["followDestroyOnShoot"] = false;
            Config["javalinDestroyOnShoot"] = false;
            //skins
            Config["instaHealAmount"] = 15;
            Config["passiveHealAmount"] = 50;

            Config["stunDuration"] = 4;

            Config["jericoSkin"] = 2656578790;
            Config["seekerSkin"] = 2657299588;
            Config["heatSeekSkin"] = 2657312965;
            Config["javalinSkin"] = 2657315108;
            Config["scatterNadeSkin"] = 2657418159;
            Config["molatoveSkin"] = 2657408625;
            Config["flashBangSkin"] = 2657412999;
            Config["healingSkin"] = 2657414983;
            Config["impactNadeSkin"] = 2657418820;
            Config["stunGrenadeSkin"] = 2657417547;
        }

        #endregion



        #region Commands
        [Command("GiveJerico"),Permission("explosiveweapons.admin")]
        private void JericoGive(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("rocket.launcher", 1, jericoSkin),
                          player.inventory.containerBelt);
        }
        [Command("GiveSeeker"), Permission("explosiveweapons.admin")]
        private void GiveSeeker(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("rocket.launcher", 1, seekerSkin),
                          player.inventory.containerBelt);
        }
        [Command("GiveFollow"), Permission("explosiveweapons.admin")]
        private void GiveFollow(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("rocket.launcher", 1, trackSkin),
                          player.inventory.containerBelt);
        }
        [Command("GiveJav"), Permission("explosiveweapons.admin")]
        private void GiveJav(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("rocket.launcher", 1, javalin),
                          player.inventory.containerBelt);
        }
        [Command("givemulti"), Permission("explosiveweapons.admin")]
        private void givemulti(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, multiGrenadeSkin),
                          player.inventory.containerBelt);
        }
        [Command("givemoly"), Permission("explosiveweapons.admin")]
        private void GiveMolly(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("grenade.beancan", 1, molatoveGrenade),
                          player.inventory.containerBelt);
        }
        [Command("giveflash"), Permission("explosiveweapons.admin")]
        private void giveflash(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, flashBangSkin),
                          player.inventory.containerBelt);
        }
        [Command("giveimpact"), Permission("explosiveweapons.admin")]
        private void giveimpact(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, impactNadeSkin),
                          player.inventory.containerBelt);
        }
        [Command("givestun"), Permission("explosiveweapons.admin")]
        private void givestun(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, stunGrenadeSkin),
                          player.inventory.containerBelt);
        }
        [Command("giveshealing"), Permission("explosiveweapons.admin")]
        private void givehealing(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, healingSkin),
                          player.inventory.containerBelt);
        }
        [Command("giveall"), Permission("explosiveweapons.admin")]
        private void giveallnades(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, stunGrenadeSkin),
                           player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, impactNadeSkin),
                          player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, flashBangSkin),
                          player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("grenade.beancan", 1, molatoveGrenade),
                          player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, multiGrenadeSkin),
                          player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("rocket.launcher", 1, javalin),
                          player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("rocket.launcher", 1, trackSkin),
                          player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("rocket.launcher", 1, seekerSkin),
                          player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("rocket.launcher", 1, jericoSkin),
                         player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("grenade.f1", 1, healingSkin),
                          player.inventory.containerBelt);
        }
        #endregion

        #region Hooks

        
        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            HeldEntity item = player.GetHeldEntity();
            Item invItem = item.GetItem();

            if(invItem.skin == jericoSkin) // jerico
            {
                entity.Kill();
                PlayEffect("assets/bundled/prefabs/fx/invite_notice.prefab", player);
                Jerico(player.IPlayer);
                if(jerichoBlowUp)
                    invItem.Remove();
                return;
            }
            else if(invItem.skin == seekerSkin)
            {
                entity.Kill();
                Vector3 firingDir = player.GetNetworkRotation() * Vector3.forward;

                Collider[] colliders = UnityEngine.Physics.OverlapCapsule(player.transform.position, player.transform.position + (firingDir * 100), 30);

                foreach (Collider col in colliders)
                {
                    BaseEntity ent = col.ToBaseEntity();
                    
                    if (ent.IsValid())
                    {
                        
                        if (ent is BasePlayer && ent != player )
                        {
                            
                            BasePlayer target = ent as BasePlayer;
                            if (target.IPlayer.HasPermission("explosiveweapons.notarget"))
                            {
                                player.ChatMessage($"No Target Found!");
                                return;
                            }
                            player.ChatMessage($"Target aquired! {target.displayName}");
                           
                            TracerRocket(player.transform.position, player.GetNetworkRotation(), ent as BasePlayer,player);
                            
                            if(heatSeekerBlowUp)
                                invItem.Remove();
                            return;
                        }
                    }

                }
                player.ChatMessage($"No Target Found!");

            }
            else if(invItem.skin == trackSkin)
            {
                entity.Kill();
                CursorRocket(player.transform.position,player.GetNetworkRotation(),player);
                if(followBlowUp)
                    invItem.DoRemove();
            }
            else if(invItem.skin == javalin)
            {
                entity.Kill();
                JavalinRocket(player.IPlayer);
                if(javalinBlowUp)
                    invItem.DoRemove();
            }

            return;    
        }
        private object OnExplosiveFuseSet(TimedExplosive ent, float fuseLength)
        {
            if(ent.skinID == molatoveGrenade)
            {
                OnCollision col = ent.gameObject.AddComponent<OnCollision>();
                col.ent = ent;
            }
            else if(ent.skinID == flashBangSkin)
            {
                timer.Once(fuseLength - 0.05f, () =>
                {

                    List<BasePlayer> playersNear = FindAllPlayersNear(ent.transform.position, 10);

                    foreach (BasePlayer item in playersNear)
                    {
                        if (item == null || item.gestureList == null) continue;

                        PlayEffect("assets/bundled/prefabs/fx/gestures/cameratakescreenshot.prefab", item);
                    }
                    PlayEffect("assets/bundled/prefabs/fx/survey_explosion.prefab", ent);
                    if (ent != null && !ent.IsDestroyed)
                        ent.Kill();
                    timer.Once(0.2f, () =>
                    {
                        foreach (BasePlayer item in playersNear)
                        {
                            if (item == null || item.gestureList == null) continue;
                            MakeUi(item);
                        }

                    });
                    timer.Once(flashDuration, () =>
                    {
                        foreach (BasePlayer item in playersNear)
                        {
                            if (item == null || item.gestureList == null) continue;
                            CuiHelper.DestroyUi(item, "Flashbang");
                        }

                    });
                }); 
            }
            else if(ent.skinID == healingSkin)
            {
                timer.Once(fuseLength - 0.05f, () =>
                {

                    List<BasePlayer> playersNear = FindAllPlayersNear(ent.transform.position, 10);

                    foreach (BasePlayer item in playersNear)
                    {
                        if (item == null) continue;
                        HealPlayer(item);
                    }
                    if(ent != null && !ent.IsDestroyed)
                        ent.Kill();

                });
            }
            else if(ent.skinID == impactNadeSkin)
            {
                OnCollisionExplode col = ent.gameObject.AddComponent<OnCollisionExplode>();
                col.ent = ent;
            }
            else if(ent.skinID == multiGrenadeSkin)
            {
                ulong ownerId = ent.OwnerID;
                timer.Once(fuseLength - 0.05f, () =>
                {
                    BasePlayer player = ent.creatorEntity as BasePlayer; // Added by ZEODE (ty)

                    Vector3 location = ent.transform.position;
                    Vector3 entityRight = ent.transform.right;
                    Vector3 entityForward = ent.transform.forward;
                    Quaternion rotation = ent.transform.rotation;
                    //grenade in a grenade
                    timer.Repeat(0.1f, 5, () => {


                        int randomRight = UnityEngine.Random.Range(scatterSize * -1, scatterSize);
                        int radonomForward = UnityEngine.Random.Range(scatterSize * -1, scatterSize);
                        BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/weapons/f1 grenade/grenade.f1.deployed.prefab", location + (Vector3.right * randomRight) + (Vector3.forward * radonomForward) + (Vector3.up * 2), rotation);
                        TimedExplosive newNade = entity as TimedExplosive;
                        entity.OwnerID = ownerId;
                        entity.creatorEntity = (BaseEntity)player;
                        entity.Spawn();
                        newNade.SetFuse(0.5f);

                    });



                });
            }
            else if(ent.skinID == stunGrenadeSkin)
            {
                timer.Once(fuseLength - 0.05f, () =>
                {

                    List<BasePlayer> playersNear = FindAllPlayersNear(ent.transform.position, 10);

                    foreach (BasePlayer seeker in playersNear)
                    {
                        if (seeker.IsWounded()) continue;
                        seeker.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, true);
                        seeker.SendNetworkUpdateImmediate();
                        PlayEffect("assets/prefabs/locks/keypad/effects/lock.code.shock.prefab", seeker);
                    }
                    //PlayEffect("assets/prefabs/locks/keypad/effects/lock.code.shock.prefab", seeker);
                    timer.Once(4, () =>
                    {
                        foreach (BasePlayer seeker in playersNear)
                        {
                            seeker.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
                            seeker.SendNetworkUpdateImmediate();
                        }

                    });
                    if (ent != null && !ent.IsDestroyed)
                        ent.Kill();

                });
            }
            
            return null;
        }
        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (skins.Contains(targetItem.item.skin) || skins.Contains(item.item.skin) && item.item.skin == targetItem.item.skin)
            {
                return false;
            }
            return null;
        }
        object CanStackItem(Item item, Item targetItem)
        {
            if ((skins.Contains(targetItem.skin) || skins.Contains(item.skin)) && item.skin != targetItem.skin)
            {
                return false;
            }
            return null;
        }
        #endregion
        private void JavalinRocket(IPlayer iplayer)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            Vector3 firingDir = player.GetNetworkRotation() * Vector3.forward;

            RaycastHit hitInfo;
            Vector3 firingPos = player.eyes.transform.position + Vector3.up + Vector3.up;
            Vector3 target;
            if (UnityEngine.Physics.Raycast(firingPos, firingDir, out hitInfo, maxRange, 1236478737))
            {
                target = hitInfo.point;
            }
            else
            {
                target = firingPos + (firingDir.normalized * maxRange);

            }
            if (spawnFlares)
            {
                BaseEntity flare = GameManager.server.CreateEntity("assets/prefabs/tools/flareold/flare.deployed.prefab", target);
                flare.Spawn();
            }
            
            BaseEntity entity = GameManager.server.CreateEntity("assets/content/vehicles/mlrs/rocket_mlrs.prefab", firingPos + firingDir);
            ServerProjectile projectile = entity.GetComponent<ServerProjectile>();
            Vector3 vector3 = projectile.initialVelocity + firingDir * 10;
            //projectile.speed
            projectile.gravityModifier = 0;
            projectile.InitializeVelocity(vector3);

            entity.creatorEntity = (BaseEntity)player;
            entity.OwnerID = player.userID;
            entity.Spawn();

            timer.Once(0.25f, () => // Start up motion
            {
                if (entity == null || projectile == null || entity.IsDestroyed) return;
                PlayEffect("assets/content/vehicles/mlrs/effects/pfx_mlrs_backfire.prefab", entity);
                projectile.gravityModifier = -2.5f;
                Vector3 newVel = projectile.initialVelocity + firingDir * 5;
                projectile.InitializeVelocity(newVel);
            });

            timer.Once(4, () => //Start flattening out
            {
                if (entity == null || projectile == null || entity.IsDestroyed) return;
                projectile.gravityModifier = 4;//4
                Vector3 newVel = projectile.CurrentVelocity + firingDir;
                projectile.InitializeVelocity(newVel);
            });
            timer.Once(7f, () => // Rain rain rain
            {
                if (entity == null || projectile == null || entity.IsDestroyed) return;
                Vector3 direction = target - projectile.transform.position;
                projectile.gravityModifier = 0;
                projectile.InitializeVelocity(direction.normalized * 75);


            });
        }
        private void CursorRocket(Vector3 firePoint, Quaternion rot, BasePlayer player)
        {
            Vector3 firingDir = rot * Vector3.forward;
            BaseEntity entity = GameManager.server.CreateEntity("assets/content/vehicles/mlrs/rocket_mlrs.prefab", firePoint + (firingDir.normalized * 2), rot);
            ServerProjectile projectile = entity.GetComponent<ServerProjectile>();
            Vector3 vector3 = (rot * Vector3.forward).normalized;
            //projectile.speed
            projectile.gravityModifier = 0;
            projectile.InitializeVelocity(vector3);

            entity.creatorEntity = (BaseEntity)player;
            entity.OwnerID = player.userID;
            entity.Spawn();

            timer.Repeat(0.1f, 0, () =>
            {
                if (entity == null || projectile == null || entity.IsDestroyed) return;
                Vector3 currentDir = entity.transform.rotation * Vector3.forward;
                // Vector3 newDirection = (player.transform.position - entity.transform.position).normalized * 10; 
                Vector3 velocity = Vector3.Lerp(currentDir.normalized, (player.GetNetworkRotation() * Vector3.forward).normalized, UnityEngine.Time.deltaTime * 100);
                projectile.InitializeVelocity(velocity * 20);

            });
        }
        private void TracerRocket(Vector3 firePoint,Quaternion rot, BaseCombatEntity target, BasePlayer owner)
        {
            BaseCombatEntity player = target;
            Vector3 firingDir = rot * Vector3.forward;
            BaseEntity entity = GameManager.server.CreateEntity("assets/content/vehicles/mlrs/rocket_mlrs.prefab", firePoint + (firingDir.normalized * 2), rot);
            ServerProjectile projectile = entity.GetComponent<ServerProjectile>();
            Vector3 vector3 = (rot * Vector3.forward).normalized;
            //projectile.speed
            projectile.gravityModifier = 0;
            projectile.InitializeVelocity(vector3);
            entity.creatorEntity = (BaseEntity)owner;
            entity.OwnerID = owner.userID;
            entity.Spawn();
            
            timer.Repeat(0.1f, 0, () =>
            {
                if (entity == null || projectile == null || entity.IsDestroyed) return;
                Vector3 currentDir = entity.transform.rotation * Vector3.forward;
                // Vector3 newDirection = (player.transform.position - entity.transform.position).normalized * 10; 
                Vector3 velocity = Vector3.Lerp(currentDir.normalized, (player.transform.position + (Vector3.up)- entity.transform.position).normalized, UnityEngine.Time.deltaTime * 40);
                projectile.InitializeVelocity(velocity * 20);
                
            });
        }

        
        private List<BasePlayer> FindAllPlayersNear(Vector3 pos, float radius)
        {
            Collider[] cast = UnityEngine.Physics.OverlapSphere(pos, radius);
            List<BasePlayer> ents = new List<BasePlayer>();
            foreach (Collider item in cast)
            {
                BaseEntity entity = item.gameObject.ToBaseEntity();
                if (entity.IsValid() && entity is BasePlayer && entity is ScientistNPC == false && entity.IsVisible(pos))
                {
                    ents.Add(entity as BasePlayer);
                }
            }
            return ents;
        }
        private void HealPlayer(BasePlayer player)
        {
           
            PlayEffect("assets/prefabs/deployable/mixingtable/effects/mixing-table-deploy.prefab", player);
            player.ChatMessage("Healed!");
            player.CancelInvoke(new Action(player.WoundingTick));
            player.RecoverFromWounded();
            player.Heal(instaHealAmount);
            player.metabolism.ApplyChange(MetabolismAttribute.Type.HealthOverTime, passiveHealAmount, 20);
           
        }
        
        private void Jerico(IPlayer iplayer)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            Vector3 firingDir = player.GetNetworkRotation() * Vector3.forward;

            RaycastHit hitInfo;
            Vector3 firingPos = player.eyes.transform.position + Vector3.up + Vector3.up;
            Vector3 target;
            if (UnityEngine.Physics.Raycast(firingPos, firingDir, out hitInfo, maxRange, 1236478737))
            {
                target = hitInfo.point;
            }
            else
            {
                target = firingPos + (firingDir.normalized * maxRange);

            }

            if (spawnFlares)
            {
                for (int i = 0; i < 6; i++)
                {
                    BaseEntity flare = GameManager.server.CreateEntity("assets/prefabs/tools/flareold/flare.deployed.prefab", target + (Vector3.up * 20) + (Vector3.right * UnityEngine.Random.Range(-10, 10)) + (Vector3.forward * UnityEngine.Random.Range(-10, 10)));
                    flare.Spawn();
                }
            }
            BaseEntity entity = GameManager.server.CreateEntity("assets/content/vehicles/mlrs/rocket_mlrs.prefab", firingPos + firingDir);
            ServerProjectile projectile = entity.GetComponent<ServerProjectile>();
            Vector3 vector3 = projectile.initialVelocity + firingDir * 10;
            //projectile.speed
            projectile.gravityModifier = 0;
            projectile.InitializeVelocity(vector3);

            entity.creatorEntity = (BaseEntity)player;
            entity.OwnerID = player.userID;

            entity.Spawn();

            timer.Once(0.25f, () => // Start up motion
            {
                if (entity == null || projectile == null || entity.IsDestroyed) return;
                PlayEffect("assets/content/vehicles/mlrs/effects/pfx_mlrs_backfire.prefab", entity);
                projectile.gravityModifier = -2.5f;
                Vector3 newVel = projectile.initialVelocity + firingDir * 5;
                projectile.InitializeVelocity(newVel);
            });

            timer.Once(4, () => //Start flattening out
            {
                if (entity == null || projectile == null || entity.IsDestroyed) return;
                projectile.gravityModifier = 4;//4
                Vector3 newVel = projectile.CurrentVelocity + firingDir;
                projectile.InitializeVelocity(newVel);
            });

            timer.Once(6f, () => // Rain rain rain
            {
                if (entity == null || projectile == null || entity.IsDestroyed) return;
                Vector3 direction = target - projectile.transform.position;
                Vector3 location = entity.transform.position;
                Vector3 entityRight = entity.transform.right;
                Vector3 entityForward = entity.transform.forward;
                Quaternion rotation = entity.transform.rotation;
                timer.Repeat(0.05f, bigRocketCount, () =>
                {
                    if (entity == null || projectile == null || entity.IsDestroyed) return;
                    int randomRight = UnityEngine.Random.Range(xSize * -1, xSize);
                    int radonomForward = UnityEngine.Random.Range(ySize * -1, ySize);
                    PlayEffect("assets/content/vehicles/mlrs/effects/pfx_mlrs_backfire.prefab", entity);
                    SpawnRocket(location + (entityRight * randomRight) + (entityForward * radonomForward), firingDir, player, rotation, direction);
                });
                projectile.gravityModifier = 0;
                projectile.InitializeVelocity(direction.normalized * 75);


            });
        }
        private void SpawnRocket(Vector3 pos, Vector3 target, BasePlayer player, Quaternion rotation, Vector3 direction)
        {
            BaseEntity entity = GameManager.server.CreateEntity("assets/content/vehicles/mlrs/rocket_mlrs.prefab", pos + target, rotation);
            ServerProjectile projectile = entity.GetComponent<ServerProjectile>();

            Vector3 vector3 = direction.normalized * 75;
            projectile.gravityModifier = 0;
            projectile.InitializeVelocity(vector3);

            entity.creatorEntity = (BaseEntity)player;
            entity.OwnerID = player.userID;
            timer.Once(UnityEngine.Random.Range(0.0f, 3.0f), () =>
            {
                entity.OwnerID = player.userID;
                entity.Spawn();
                timer.Once(2f, () =>
                {
                    if (entity == null || projectile == null || entity.IsDestroyed) return;
                    PlayEffect("assets/content/vehicles/mlrs/effects/pfx_mlrs_backfire.prefab", entity);

                    timer.Repeat(0.05f, littleRocketCount, () =>
                    {

                        if (entity == null || projectile == null || entity.IsDestroyed) return;
                        int randomUp = UnityEngine.Random.Range(-20, 20);
                        int radonomForward = UnityEngine.Random.Range(-20, 20);
                        int radonom = UnityEngine.Random.Range(-20, 20);

                        SpawnSmallRocket(entity.transform.position + (Vector3.left * randomUp) + (Vector3.forward * radonomForward), target, player, projectile, direction);
                    });

                });
            });


        }
        private void SpawnSmallRocket(Vector3 pos, Vector3 target, BasePlayer player, ServerProjectile parentProjectile, Vector3 direction)
        {
            BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/ammo/rocket/rocket_basic.prefab", pos + target, parentProjectile.transform.rotation);
            ServerProjectile projectile = entity.GetComponent<ServerProjectile>();
            //projectile.speed
            projectile.gravityModifier = 0;

            Vector3 vector3 = direction.normalized * 75;
            projectile.gravityModifier = 0;
            projectile.InitializeVelocity(vector3);

            entity.creatorEntity = (BaseEntity)player;
            entity.OwnerID = player.userID;

            entity.Spawn();
        }


        private static void PlayEffect(string effect, BaseEntity entity)
        {
            BaseEntity playerEntity = entity;
            Effect reusableInstance = new Effect();
            reusableInstance.Clear();

            reusableInstance.Init(Effect.Type.Generic, playerEntity, 0, new Vector3(0, 0, 0), new Vector3(0, 0, 0), null);
            reusableInstance.scale = false ? 0.0f : 1f;


            reusableInstance.pooledString = effect;
            EffectNetwork.Send(reusableInstance);
        }
        private void MakeUi(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1.0 1.0 1.0 1.0" },
                FadeOut = 1,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            }, "Overlay", "Flashbang");
            CuiHelper.DestroyUi(player, "Flashbang");
            CuiHelper.AddUi(player, container);
        }
        public class OnCollision : MonoBehaviour
        {
            public BaseEntity ent { get; set; }
            

            void OnCollisionEnter(Collision collision)
            {

                PlayEffect("assets/bundled/prefabs/fx/impacts/stab/glass/glass1.prefab", ent);

                for (int i = 0; i < 15; i++)
                {
                    int randomRight = UnityEngine.Random.Range(-300, 300);
                    int radonomForward = UnityEngine.Random.Range(-300, 300);
                    BaseEntity entity = GameManager.server.CreateEntity("assets/bundled/prefabs/fireball.prefab", ent.transform.position + (Vector3.right * (randomRight / 100)) + (Vector3.forward * (radonomForward / 100)), new Quaternion(), true) as BaseEntity;
                    entity.creatorEntity = (BaseEntity)ent.creatorEntity;
                    entity.OwnerID = ent.OwnerID;
                    entity.Spawn();
                }
                ent.Kill();
                Destroy(this);
            }
        }
        public class OnCollisionExplode : MonoBehaviour
        {
            public TimedExplosive ent { get; set; }


            void OnCollisionEnter(Collision collision)
            {
                ent.Explode();
                Destroy(this);
            }
        }
    }
}