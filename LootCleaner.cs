using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Loot Cleaner", "walkinrey", "1.0.2")]
    class LootCleaner : RustPlugin 
    {
        Configuration config;
        class Configuration 
        {
            [JsonProperty("Через сколько секунд удалять ящик, если его не до конца облутал игрок?")] 
            public float seconds = 5f;
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        protected override void LoadConfig()
        {
            base.LoadConfig(); 
            try 
            {
                config = Config.ReadObject<Configuration>();
            } 
            catch 
            {
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => config = new Configuration();
        void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity) 
        {
            try {
                if(((LootContainer)entity) == null) return;
                LootContainer container = (LootContainer)entity;
                if(container.inventory.itemList.Count != 0) {
                    timer.Once(config.seconds, () =>
                    {
                        if(container != null) container.RemoveMe();
                    });
                }
            }
            catch {}
        }
    }
}