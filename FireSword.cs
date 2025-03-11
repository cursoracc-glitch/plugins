using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("FireSword", "ColonBlow", "1.1.8")]
    public class FireSword : RustPlugin
    {
        //fix for sword skin id not working from config

        #region Load and Data

        void Loaded()
        {
            permission.RegisterPermission("firesword.blacksmith", this);
            lang.RegisterMessages(messagesFA, this);
            LoadVariables();
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyFireOnData(player);
            }
        }

        bool Changed;
        bool isAllowed(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        //This allows tracking Melee Weapons that are on fire when thrown to toggle fire effects on impact
        static Dictionary<ulong, ToggleFireData> FireOn = new Dictionary<ulong, ToggleFireData>();

        class ToggleFireData
        {
            public BasePlayer player;
        }

        #endregion

        #region Commands

        [ChatCommand("firesword")]
        void chatFireSword(BasePlayer player, string command)
        {
            AddFireSword(player);
        }

        [ConsoleCommand("firesword")]
        void cmdConsoleFireSword(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            AddFireSword(player);
        }

        #endregion

        #region Configuration

        static float FSChance = 50f;
        static float FSHeatDamage = 25f;
        static float FSExplosionDamage = 100f;
        static float DamageRadius = 1f;
        static bool UseProt = true;
        static bool LootAndUse = true;
        static bool DamageConditionOnThrow = true;
        static ulong CustomSkinID = 813766930;

        static bool MatsToBuild = true;
        static int AmountToBuild = 100;
        static int ReqBuildItemID = -946369541;

        static bool UseMats = true;
        static int AmountReq1 = 5;
        static int Req1ItemID = -946369541;  //Default ID is low grade fuel
        static int AmountReq2 = 20;
        static int Req2ItemID = -265876753; //Default ID is gunpowder

        private void LoadVariables()
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

        private void LoadConfigVariables()
        {
            CheckCfgFloat("Damage - Chance - Likelyhood of Fireball spawn on Melee Fire Weapon Strike : (percentage) : ", ref FSChance);
            CheckCfgFloat("Damage - Heat - Amount of Heat Damage added with Melee/Throw attacks : ", ref FSHeatDamage);
            CheckCfgFloat("Damage - Explosive - Amount added when Fire Weapon is Thrown : ", ref FSExplosionDamage);
            CheckCfgFloat("Damage - Radius - Strike/Throw damage radius : ", ref DamageRadius);
            CheckCfg("Damage - Reduction - Use Victims Protection Values when damaging : ", ref UseProt);

            CheckCfg("Materials - Make Sword - Require Materials to Make Fire Sword : ", ref MatsToBuild);
            CheckCfg("Materials - Make Sword - ID of item needed to make Fire Sword : ", ref ReqBuildItemID);
            CheckCfg("Materials - Make Sword - Amount of Materials needed to Make Fire Sword : ", ref AmountToBuild);

            CheckCfg("Usage - Found/Looted Fire Weapons can be used by Anyone : (no perms needed) : ", ref LootAndUse);
            CheckCfg("Durability - Deal random condition loss when Fire Weapon is Thrown : ", ref DamageConditionOnThrow);
            CheckCfgUlong("Skin - Fire Weapon custom steam skin ID : (Set to 0 for default) : ", ref CustomSkinID);

            CheckCfg("Materials - Require materials to use Fire Sword : ", ref UseMats);
            CheckCfg("Materials - Flame - Material ID needed to fuel flames", ref Req1ItemID);
            CheckCfg("Materials - Flame - Amount PER TICK needed for flames : ", ref AmountReq1);
            CheckCfg("Materials - Explosion - Material ID needed for Explosion on Weapon Throw : ", ref Req2ItemID);
            CheckCfg("Materials - Explosion - Amount needed for Explosion : ", ref AmountReq2);
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        private void CheckCfgFloat(string Key, ref float var)
        {

            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
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

        Dictionary<string, string> messagesFA = new Dictionary<string, string>()
                {
                    {"fireweapondenied", "You are not worthy of this yet !!"},
                    {"fireweapondestroyed", "You have destroyed your Fire Weapon !!"},
                    {"fireweaponcreationerror", "No room in your inventory for Fire Weapon !!"},
                    {"fireweaponnomats1", "You need Low Grade Fuel (" + AmountReq1 + ") to Toggle Fire Weapon !!"},
                    {"fireweaponnomats2", "You need Low Grade Fuel (" + AmountReq1 + ") and Explosives (" + AmountReq2 + ") to have Fire Weapon Throw Explosion !!"},
            {"fireweaponcreated", "You have created a Fire Weapon !!"}
                };

        #endregion

        #region Flame Weapon

        class FlameWeapon : MonoBehaviour
        {
            BasePlayer player;
            BaseEntity flame;
            BaseEntity playerweapon;
            FireBall fireball;
            Vector3 pos;
            Quaternion rot;
            string prefab;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                playerweapon = player.GetHeldEntity();
                pos = new Vector3(-0.1f, -0.1f, -0.6f);
                rot = Quaternion.identity;
                prefab = "assets/bundled/prefabs/fireball_small.prefab";
            }

            void FixedUpdate()
            {
                if (!UsingFireWeapon(player))
                {
                    if (fireball == null) return;
                    fireball.Kill(BaseNetworkable.DestroyMode.None);
                    return;
                }
                if (fireball != null)
                {
                    fireball.transform.localPosition = new Vector3(-0.1f, -0.1f, -0.6f);
                    fireball.transform.hasChanged = true;
                    fireball.SendNetworkUpdateImmediate();
                    return;

                }
                if (fireball == null)
                {
                    SpawnFireEffects();
                }
            }
            void SpawnFireEffects()
            {
                if (UseMats)
                {
                    if (!HasItem1Mats(player))
                    {
                        GameObject.Destroy(this);
                        return;
                    }
                    else
                    if (TakeItem1Mats(player)) ;
                }

                flame = GameManager.server.CreateEntity(prefab, pos, rot, true);
                fireball = flame.GetComponent<FireBall>();
                fireball.generation = 0.1f;
                fireball.radius = 0.1f;
                fireball.tickRate = 0.1f;
                fireball.SetParent(playerweapon, 0);
                flame?.Spawn();
            }

            bool HasItem1Mats(BasePlayer player)
            {
                int HasReq1 = player.inventory.GetAmount(Req1ItemID);

                if (HasReq1 >= AmountReq1) return true;
                return false;
            }


            bool TakeItem1Mats(BasePlayer player)
            {
                int HasReq1 = player.inventory.GetAmount(Req1ItemID);

                if (HasReq1 >= AmountReq1)
                {
                    player.inventory.Take(null, Req1ItemID, AmountReq1);
                    player.Command("note.inv", Req1ItemID, -AmountReq1);
                    return true;
                }
                return false;
            }

            void OnDestroy()
            {
                if (fireball == null) return;
                fireball.Kill(BaseNetworkable.DestroyMode.None);
            }
        }

        #endregion

        #region Hooks

        void AddFireOn(BasePlayer player)
        {
            if (ThrowWeaponHasFireOn(player)) return;
            FireOn.Add(player.userID, new ToggleFireData { player = player, });
        }

        void RemoveFireOn(BasePlayer player)
        {
            if (!ThrowWeaponHasFireOn(player)) return;
            FireOn.Remove(player.userID);
        }

        bool ThrowWeaponHasFireOn(BasePlayer player)
        {
            if (FireOn.ContainsKey(player.userID)) return true;
            return false;
        }

        public void SpawnFireSword(ItemContainer itemContainer)
        {
            int roll = UnityEngine.Random.Range(0, 100);
            if (roll >= 75) return;
            Item sword = ItemManager.CreateByItemID(-388967316, 1, 813766930);
            sword.MoveToContainer(itemContainer, -1, false);
            sword.SetFlag(global::Item.Flag.OnFire, true);
            sword.MarkDirty();
        }

        void OnMeleeThrown(BasePlayer player, Item item)
        {
            if (item == null) return;
            if (player == null) return;
            if (!item.HasFlag(global::Item.Flag.OnFire)) { RemoveFireOn(player); return; }
            if (item.HasFlag(global::Item.Flag.OnFire))
            {
                var flameweapon = player.GetComponent<FlameWeapon>();
                if (flameweapon == null) return;
                AddFireOn(player);
                ThrowWeaponCondition(player, item);
            }
        }

        void OnPlayerAttack(BasePlayer player, HitInfo hitInfo)
        {
            if (hitInfo == null) return;
            if (player == null) return;

            //Checks to make sure weapon has Fire toggled on or off
            var flameweapon = player.GetComponent<FlameWeapon>() ?? null;
            if (flameweapon == null) return;

            Vector3 pos = hitInfo.HitPositionWorld;

            //OnMeleeThrown check to make sure weapon thrown is a fire weapon
            if ((ThrowWeaponHasFireOn(player)) && (flameweapon != null))
            {
                WeaponStrikeFX(player, pos, hitInfo);
                WeaponThrowFX(player, pos, hitInfo);
                RemoveFireOn(player);
                GameObject.Destroy(flameweapon);
                return;
            }

            //Check melee attack to make sure using fire weapon
            if (!UsingFireWeapon(player)) return;
            if ((UsingFireWeapon(player)) && (flameweapon != null))
            {
                WeaponStrikeFX(player, pos, hitInfo);
                RemoveFireOn(player);
                return;
            }
        }

        bool UsingSwordWeapon(BasePlayer player)
        {
            Item activeItem = player.GetActiveItem();
            if (activeItem != null && activeItem.info.shortname == "salvaged.sword") return true;
            return false;
        }
        static bool UsingFireWeapon(BasePlayer player)
        {
            Item activeItem = player.GetActiveItem();
            if (activeItem != null && activeItem.HasFlag(global::Item.Flag.OnFire)) return true;
            return false;
        }

        void WeaponThrowFX(BasePlayer player, Vector3 pos, HitInfo hitInfo)
        {
            if (UseMats)
            {
                if (HasItem2Mats(player))
                {
                    Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_01.prefab", pos);
                    AddSwordDamage(player, FSExplosionDamage, Rust.DamageType.Explosion, hitInfo);
                    AddSwordDamage(player, FSHeatDamage, Rust.DamageType.Heat, hitInfo);
                    return;
                }
                else
                    SendReply(player, lang.GetMessage("fireweaponnomats2", this));
                return;

            }
            else
                Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_01.prefab", pos);
            AddSwordDamage(player, FSExplosionDamage, Rust.DamageType.Explosion, hitInfo);
            AddSwordDamage(player, FSHeatDamage, Rust.DamageType.Heat, hitInfo);
            return;
        }

        void WeaponStrikeFX(BasePlayer player, Vector3 pos, HitInfo hitInfo)
        {
            AddSwordDamage(player, FSHeatDamage, Rust.DamageType.Heat, hitInfo);
            float chanceforstrike = UnityEngine.Random.Range(0f, 99f);
            if (chanceforstrike <= FSChance)
            {
                Quaternion rot = new Quaternion();
                string prefab = "assets/bundled/prefabs/fireball.prefab";
                BaseEntity flame = GameManager.server.CreateEntity(prefab, pos, rot, true);
                FireBall fireball = flame.GetComponent<FireBall>();
                fireball.damagePerSecond = 1f;
                fireball.radius = 1f;
                fireball.lifeTimeMin = 5f;
                fireball.lifeTimeMin = 5f;
                fireball.generation = 10f;
                fireball.Spawn();
                return;
            }
        }

        void AddSwordDamage(BasePlayer player, float damageamount, Rust.DamageType damagetype, HitInfo hitInfo)
        {
            List<BaseCombatEntity> entitylist = new List<BaseCombatEntity>();
            Vis.Entities<BaseCombatEntity>(hitInfo.HitPositionWorld, DamageRadius, entitylist);

            foreach (BaseCombatEntity entity in entitylist)
            {
                if (!(entity is BuildingPrivlidge))
                {
                    if (entity is BasePlayer)
                    {
                        var attacker = (BasePlayer)entity;
                        if (attacker.userID == player.userID) return;
                    }
                    entity.Hurt(damageamount, damagetype, player, UseProt);
                }
            }
        }

        void ThrowWeaponCondition(BasePlayer player, Item item)
        {
            if (DamageConditionOnThrow)
            {
                float currentcond = item.condition;
                float randomcond = UnityEngine.Random.Range(10f, currentcond + 10f);
                item.condition = item.condition - randomcond;
                if (item.condition <= 0)
                {
                    SendReply(player, lang.GetMessage("fireweapondestroyed", this));
                }
                return;
            }
            else
                return;
        }

        bool HasItem1Mats(BasePlayer player)
        {
            int HasReq1 = player.inventory.GetAmount(Req1ItemID);

            if (HasReq1 >= AmountReq1) return true;
            return false;
        }

        bool HasItem2Mats(BasePlayer player)
        {
            int HasReq2 = player.inventory.GetAmount(Req2ItemID);

            if (HasReq2 >= AmountReq2)
            {
                player.inventory.Take(null, Req2ItemID, AmountReq2);
                player.Command("note.inv", Req2ItemID, -AmountReq2);
                return true;
            }
            return false;
        }

        void AddFireSword(BasePlayer player)
        {
            if (!UsingSwordWeapon(player)) return;
            var flameweapon = player.GetComponent<FlameWeapon>();

            //toggles fire off if already on
            if (flameweapon != null)
            {
                GameObject.Destroy(flameweapon);
                RemoveFireOn(player);
                return;
            }

            //toggles fire on if player is holding fire weapon and LootAndUse is turned on. or has permission to make a fire sword.
            if (flameweapon == null)
            {
                if ((UsingFireWeapon(player)) && (LootAndUse))
                {
                    if (UseMats)
                    {
                        if (!HasItem1Mats(player))
                        {
                            SendReply(player, lang.GetMessage("fireweaponnomats1", this));
                            return;
                        }
                    }
                    player.gameObject.AddComponent<FlameWeapon>();
                    return;
                }
                if (!UsingFireWeapon(player))
                {
                    if (isAllowed(player, "firesword.blacksmith"))
                    {
                        ActivateWeapon(player);
                        return;
                    }
                    SendReply(player, lang.GetMessage("fireweapondenied", this));
                    return;
                }
            }
        }

        void ActivateWeapon(BasePlayer player)
        {
            Item playerweaponitem = player.GetActiveItem();
            if (playerweaponitem.skin != CustomSkinID)
            {
                ReplaceWithFireSword(player, playerweaponitem);
                return;
            }
            if (playerweaponitem.skin == CustomSkinID)
            {
                ActivateFireSword(player, playerweaponitem);
                return;
            }
        }

        void ReplaceWithFireSword(BasePlayer player, Item playerweaponitem)
        {
            if (MatsToBuild)
            {
                if (!CheckUpgradeMats(player, ReqBuildItemID, AmountToBuild, "Fire Sword")) return;
            }
            playerweaponitem.Remove(0f);

            Item sword = ItemManager.CreateByItemID(1326180354, 1, CustomSkinID);
            if (!player.inventory.GiveItem(sword, null))
            {
                sword.Remove(0f);
                SendReply(player, lang.GetMessage("fireweaponcreationerror", this));
                return;
            }
            SendReply(player, lang.GetMessage("fireweaponcreated", this));
            ActivateFireSword(player, sword);
        }

        bool CheckUpgradeMats(BasePlayer player, int itemID, int amount, string str)
        {
            int HasReq = player.inventory.GetAmount(itemID);
            if (HasReq >= amount)
            {
                player.inventory.Take(null, itemID, amount);
                player.Command("note.inv", itemID, -amount);
                return true;
            }
            ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemID);

            SendReply(player, "You need " + amount + " " + itemDefinition.shortname + " to build " + str);
            return false;
        }

        void ActivateFireSword(BasePlayer player, Item playerweaponitem)
        {
            playerweaponitem.SetFlag(global::Item.Flag.OnFire, true);
            playerweaponitem.MarkDirty();
        }

        void Unload()
        {
            DestroyAll<FlameWeapon>();
        }

        void DestroyFireOnData(BasePlayer player)
        {
            if (FireOn.ContainsKey(player.userID))
            {
                FireOn.Remove(player.userID);
            }
            else
                return;
        }

        static void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                    GameObject.Destroy(gameObj);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var flameweapon = player.GetComponent<FlameWeapon>();
            if (flameweapon == null) return;
            if (flameweapon != null)
            {
                GameObject.Destroy(flameweapon);
            }
            DestroyFireOnData(player);
            return;
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            var flameweapon = player.GetComponent<FlameWeapon>();
            if (flameweapon == null) return;
            if (flameweapon != null)
            {
                GameObject.Destroy(flameweapon);
            }
            DestroyFireOnData(player);
            return;
        }

        #endregion
    }
}
