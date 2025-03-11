using Oxide.Core;
using System;
using Oxide.Core.Libraries.Covalence;
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
    [Info("Rocke", "sdapro", "1.0.0")]
    public class Rocke : CovalencePlugin
    {
        int bigRocketCount = 1;
        int littleRocketCount = 1;

        bool javalinBlowUp = false;


        int xSize = 1;
        int ySize = 1;
        int maxRange = 200;

        int scatterSize = 3;

        ulong javalin = 2656578790;

        int instaHealAmount = 15;
        int passiveHealAmount = 50;

        bool spawnFlares = true;
        int stunDuration = 4;

        HashSet<ulong> skins = new HashSet<ulong>();
        #region Config

        private void Init()
        {
			permission.RegisterPermission("rocke.rq", this);
            javalinBlowUp = (bool)Config["javalinDestroyOnShoot"];
			javalin = ulong.Parse(Config["javalinSkin"].ToString());


            if (Config["SpawnFlares"] == null)
            {
                Config["SpawnFlares"] = true;
                SaveConfig();

            }

            
            spawnFlares = (bool)Config["SpawnFlares"];

            skins.Add(javalin);

        }


        protected override void LoadDefaultConfig()
        {
            Config["javalinDestroyOnShoot"] = false;
			Config["javalinSkin"] = 2656578790;
        }

        #endregion



        #region Commands
        [Command("jar"), Permission("rocke.jav")]
        private void jar(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            player.inventory.GiveItem(ItemManager.CreateByName("multiplegrenadelauncher", 1, javalin),
                          player.inventory.containerBelt);
        }
        #endregion

        #region Hooks
        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            HeldEntity item = player.GetHeldEntity();
            Item invItem = item.GetItem();
            if(invItem.skin == javalin)
			if(permission.UserHasPermission(player.userID.ToString(), "rocke.rq"))
            {
                entity.Kill();
                JavalinRocket(player.IPlayer);
                if(javalinBlowUp)
                    invItem.DoRemove();
            }

            return;    
        }
        #endregion
private void JavalinRocket(IPlayer iplayer)
{
    BasePlayer player = iplayer.Object as BasePlayer;
    Vector3 firingDir = player.GetNetworkRotation() * Vector3.forward;

    RaycastHit hitInfo;
    Vector3 firingPos = player.eyes.transform.position + Vector3.up;
    Vector3 target;
    if (UnityEngine.Physics.Raycast(firingPos, firingDir, out hitInfo, maxRange, 1236478737))
    {
        target = hitInfo.point;
    }
    else
    {
        target = firingPos + (firingDir.normalized * maxRange);
    }

    BaseEntity entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/rocket_heli.prefab", firingPos + firingDir);
    ServerProjectile projectile = entity.GetComponent<ServerProjectile>();
    Vector3 vector3 = projectile.initialVelocity + firingDir * 1;
    projectile.gravityModifier = 2;
    projectile.InitializeVelocity(vector3);

    entity.creatorEntity = (BaseEntity)player;
    entity.OwnerID = player.userID;
    entity.Spawn();

    timer.Once(0.1f, () => // Start up motion
    {
        if (entity == null || projectile == null || entity.IsDestroyed) return;
        projectile.gravityModifier = 0f;
        Vector3 newVel = projectile.initialVelocity + firingDir * 100;
        projectile.InitializeVelocity(newVel);
    });

    timer.Once(0.01f, () =>
    {
        TimedExplosive newNade = entity as TimedExplosive;
        if (newNade != null)
        {
            newNade.SetFuse(30.0f);
        }
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