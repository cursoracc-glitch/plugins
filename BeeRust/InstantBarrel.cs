/*
 * Exotic Plugins © 2023
 * File can not be copied, modified and/or distributed without the express permission from Tryhard
 * For support join our discord - https://discord.gg/YnbYaugRMh
 */

using System;
using CompanionServer.Handlers;
using Newtonsoft.Json;
using Oxide.Core;
using Rust;
using UnityEngine; 

namespace Oxide.Plugins
{
    [Info("Instant Barrel", "Tryhard", "1.1.5")]
    [Description("Makes barrels and road signs 1 hp and instantly spawns loot in player inventory")]
    public class InstantBarrel : RustPlugin
    {
        private const string onPermission = "InstantBarrel.on";

        private readonly string[] lootBarrelsNames = { "loot_barrel_1", "loot_barrel_2", "loot-barrel-1", "loot-barrel-2", "oil_barrel", "roadsign1", "roadsign2", "roadsign3", "roadsign4", "roadsign5", "roadsign6", "roadsign7", "roadsign8", "roadsign9" };

        private void OnServerInitialized() => permission.RegisterPermission(onPermission, this);

        private object OnEntityTakeDamage(LootContainer lootContainer, HitInfo hitInfo)
        {
            if (lootContainer == null || hitInfo == null || hitInfo.ProjectileDistance > config.maxDistance)
                return null;

            if (!config.oneShot && hitInfo.damageTypes.Total() < lootContainer.health) 
                return null;

            var lootContainerName = lootContainer.ShortPrefabName;

            if (lootContainerName == null || !lootBarrelsNames.Contains(lootContainerName))
                return null;

            var player = lootContainer.lastAttacker as BasePlayer ?? hitInfo.InitiatorPlayer;

            if (player == null || !permission.UserHasPermission(player.UserIDString, onPermission))
                return null;

            var itemContainer = lootContainer?.inventory;

            if (itemContainer == null) return null;

            if ((int)Vector2.Distance(player.transform.position, lootContainer.transform.position) > config.maxDistance) return null;

            if (!config.eWeapon && hitInfo.IsProjectile()) return null;

            for (int i = itemContainer.itemList.Count - 1; i >= 0; i--)
                player.GiveItem(itemContainer.itemList[i], BaseEntity.GiveItemReason.PickedUp);

            if (itemContainer.itemList == null || itemContainer.itemList.Count <= 0)
            {
                NextTick(() =>
                {
                    Interface.CallHook("OnEntityDeath", lootContainer, hitInfo);

                    if (config.gibs) lootContainer?.Kill(BaseNetworkable.DestroyMode.Gib);

                    else lootContainer?.Kill();
                });
            }

            return false;
        }

        #region Config
        static Configuration config;
        public class Configuration
        {
            [JsonProperty("Enable farming with weapons")]
            public bool eWeapon = true;

            [JsonProperty("Max farming distance")]
            public float maxDistance = 3f;

            [JsonProperty("Make barrels 1 hit to kill")]
            public bool oneShot = true;

            [JsonProperty("Enable barrel gibs")]
            public bool gibs = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = new Configuration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion
    }
}