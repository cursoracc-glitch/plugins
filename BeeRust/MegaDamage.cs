using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("MegaDamage", "Koks", "1.0.2")]
    [Description("MegaDamage")]
    public class MegaDamage : RustPlugin
    {
        private static PluginConfig cfg = new PluginConfig();
        private class PluginConfig
        {
            [JsonProperty("Пермишены : На сколько увеличить?")]
            public Dictionary<string, float>perm=new Dictionary<string, float>()
            {
                { "megadamage.x2", 2},
                { "megadamage.x3", 3}
            };

            public static PluginConfig GetNewPluginConfig()
            {
                return new PluginConfig();
            }
        }
        List<ulong>offdamage = new List<ulong>();
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                cfg = Config.ReadObject<PluginConfig>();
                if (cfg == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Создаем новую конфигурацию!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => cfg = PluginConfig.GetNewPluginConfig();
        protected override void SaveConfig() => Config.WriteObject(cfg);

        private void OnServerInitialized()
        {
            foreach(var perm in cfg.perm)
            {
                if (!permission.PermissionExists(perm.Key, this))permission.RegisterPermission(perm.Key, this);
            }
        }
        [ChatCommand("damage")]
        void CommandDamage(BasePlayer player)
        {
            if (offdamage.Contains(player.userID))
            {
                offdamage.Remove(player.userID);
                player.ChatMessage("Вы включили увеличенный урон");

            }
            else
            {
                offdamage.Add(player.userID);
                player.ChatMessage("Вы выключили увеличенный урон");
            }
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var initiatorPlayer = info.InitiatorPlayer;
            if (initiatorPlayer == null) return;
            if (offdamage.Contains(initiatorPlayer.userID)) return;
            float scale = 1;
            foreach (var perm in cfg.perm)
            {
                if(permission.UserHasPermission(initiatorPlayer.UserIDString, perm.Key)) scale = perm.Value;
            }
            info.damageTypes.ScaleAll(scale);
        }
    }

}