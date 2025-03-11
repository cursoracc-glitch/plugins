using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("IQBreakingTools", "Mercury", "0.0.4")]
    [Description("Что этот Mercury себе позволяет,он уже заебал клепать хуйню")]
    class IQBreakingTools : RustPlugin
    {
        #region Vars
        string IQBreakingToolsPermission = "IQBreakingTools.use".ToLower();
        string IQWeapon = "IQBreakingTools.weapon".ToLower();
        string IQTools = "IQBreakingTools.tools".ToLower();
        string IQAttire = "IQBreakingTools.attire".ToLower();
        #endregion
    
        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Список предметов,которые не будут ломаться (shortname)")]
            public List<string> ToolsList = new List<string>();
            [JsonProperty("Список исключенных SkinID(Вещи с этим SkinID будут ломаться! Для кастомных предметов)")]
            public List<ulong> BlackList = new List<ulong>();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    ToolsList = new List<string>
                    {
                        "rifle.ak",
                        "jackhammer",
                        "hatchet"
                    },
                    BlackList = new List<ulong>
                    {
                        1337228,
                        2281337
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #1345" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        void RegisteredPermissions()
        {         
            permission.RegisterPermission(IQBreakingToolsPermission, this);
            permission.RegisterPermission(IQTools, this);
            permission.RegisterPermission(IQWeapon, this);
            permission.RegisterPermission(IQAttire, this);
            PrintWarning("Permissions - completed");
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            RegisteredPermissions();
        }
        void OnLoseCondition(Item item, ref float amount)
        {
            if (item == null) return;
            BasePlayer player = item.GetOwnerPlayer();
            if (player == null) return;
            if (config.BlackList.Contains(item.skin)) return;
            var ItemCategory = ItemManager.FindItemDefinition(item.info.itemid).category;
            if (ItemCategory == ItemCategory.Weapon && permission.UserHasPermission(player.UserIDString, IQWeapon)
            || ItemCategory == ItemCategory.Attire && permission.UserHasPermission(player.UserIDString, IQAttire)
            || ItemCategory == ItemCategory.Tool && permission.UserHasPermission(player.UserIDString, IQTools))
                amount = 0;
            else if (permission.UserHasPermission(player.UserIDString, IQBreakingToolsPermission))
                if (config.ToolsList.Contains(item.info.shortname))
                    amount = 0;
        }
        #endregion
    }
}
