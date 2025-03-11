using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("UItems","Baks","1.2")]
    public class UItems:RustPlugin
    {
        #region var

        private string pluginPrefix = "uitems";

        #endregion

        #region config

        class PluginConfig
        {
            [JsonProperty("Привилегия для Инструментов")]
            public string ToolsPerm;
            [JsonProperty("Привилегия для Оружия")]
            public string WeaponPerm;
            [JsonProperty("Привилегия для Одежды")]
            public string AttirePerm;
            [JsonProperty("Привилегия для Патронов")]
            public string AmmunitionPerm;
            [JsonProperty("Привилегия для Ракет")]
            public string RocketPerm;
        }

        private PluginConfig config;
        
        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig
            {
                ToolsPerm = ".tools",
                AttirePerm = ".attire",
                WeaponPerm = ".weapon",
                AmmunitionPerm = ".ammo",
                RocketPerm = ".rocket"
            }, true);
            
        }

        #endregion

        #region hooks

        void OnServerInitialized()
        {
            LoadConfig();
            permission.RegisterPermission(pluginPrefix+config.AmmunitionPerm,this);
            permission.RegisterPermission(pluginPrefix+config.ToolsPerm,this);
            permission.RegisterPermission(pluginPrefix+config.AttirePerm,this);
            permission.RegisterPermission(pluginPrefix+config.RocketPerm,this);
            permission.RegisterPermission(pluginPrefix+config.WeaponPerm,this);
            
        }

        void OnLoseCondition(Item item, ref float amount)
        {
            if (item == null) return;
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null) return;
                switch (item.info.category)
            {
                case ItemCategory.Tool:
                    if (permission.UserHasPermission(player.UserIDString,pluginPrefix+config.ToolsPerm)) item.RepairCondition(amount);
                    break;
                case  ItemCategory.Attire:
                    if (permission.UserHasPermission(player.UserIDString,pluginPrefix+config.AttirePerm)) item.RepairCondition(amount);
                    break;
                case  ItemCategory.Weapon:
                    if (permission.UserHasPermission(player.UserIDString,pluginPrefix+config.WeaponPerm)) item.RepairCondition(amount);
                    break;
            }
        }
        object OnAmmoUnload(BaseProjectile projectile, Item item, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString,pluginPrefix+config.AmmunitionPerm)) return null;
            return false;
        }

        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString,pluginPrefix+config.AmmunitionPerm)) return;
            projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
            projectile.SendNetworkUpdateImmediate();
        }

        private void OnRocketLaunched(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString,pluginPrefix+config.RocketPerm)) return;
            var heldItem = player.GetActiveItem();
            if (heldItem == null) return;
            var weapon = heldItem.GetHeldEntity() as BaseProjectile;
            if (weapon == null)
            {
                return;
            }
            weapon.primaryMagazine.contents = weapon.primaryMagazine.capacity;
            weapon.SendNetworkUpdateImmediate();
        }

        #endregion
        
    }
}