using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins 
{
    [Info("AutoKit", "walkinrey", "1.0.2")]
    public class AutoKit : RustPlugin 
    {
        #region Конфигурация

        private Configuration _config;

        public class Configuration 
        {
            [JsonProperty("Биомы")] 
            public Biomes biomes = new Biomes();

            public class Biomes
            {
                [JsonProperty("Тундра")] 
                public Biome tundra = new Biome();

                [JsonProperty("Пустыня")] 
                public Biome dust = new Biome();

                [JsonProperty("Зима")] 
                public Biome winter = new Biome();

                [JsonProperty("Умеренный")] 
                public Biome normal = new Biome();
            }

            public class Biome
            {
                [JsonProperty("Разрешение для использования")] 
                public string permission = "autokit.normal";

                [JsonProperty("Выдавать набор в этом биоме?")] 
                public bool recive = true;

                [JsonProperty("Очищать инвентарь перед выдачей?")]
                public bool strip = true;

                [JsonProperty("Предметы к выдаче")] 
                public List<ItemConfig> items = new List<ItemConfig>();

                public class ItemConfig
                {
                    [JsonProperty("Отображаемое название")] 
                    public string name = "Штаны";

                    [JsonProperty("Скин ID предмета")] 
                    public ulong id = 0;

                    [JsonProperty("Shortname предмета")] 
                    public string shortname = "pants";

                    [JsonProperty("Количество")] 
                    public int amount = 1;

                    [JsonProperty("В какой контейнер помещать? (wear, main, belt)")] 
                    public string container = "wear";

                    public ItemConfig(ulong sourceID = 0, string sourceShortname = "", int sourceAmount = 0, string sourceContainer = "")
                    {
                        id = sourceID;
                        shortname = sourceShortname;
                        amount = sourceAmount;
                        container = sourceContainer;
                    }
                }
            
                public Biome(string defaultPermission = "", bool fillDefaultItem = false)
                {
                    if(fillDefaultItem) items.Add(new ItemConfig(0, "pants", 1, "wear"));
                    permission = defaultPermission;
                }
            }
        
            public static Configuration GetDefault()
            {
                Configuration _config = new Configuration();

                _config.biomes.tundra = new Biome("autokit.tundra", true);
                _config.biomes.dust = new Biome("autokit.dust", true);
                _config.biomes.winter = new Biome("autokit.winter", true);
                _config.biomes.normal = new Biome("autokit.normal", true);

                return _config;
            }
        }

        protected override void LoadDefaultConfig() => _config = Configuration.GetDefault();
        protected override void SaveConfig() => Config.WriteObject(_config);
        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<Configuration>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        
        #endregion

        #region Хуки

        private void Loaded()
        {
            permission.RegisterPermission(_config.biomes.tundra.permission, this);
            permission.RegisterPermission(_config.biomes.dust.permission, this);
            permission.RegisterPermission(_config.biomes.winter.permission, this);
            permission.RegisterPermission(_config.biomes.normal.permission, this);

            permission.RegisterPermission("autokit.admin", this);
        }

        private void OnDefaultItemsReceived(PlayerInventory inventory) 
        {
            var player = inventory.containerMain?.GetOwnerPlayer();

            if(player == null) return;

            switch(GetBiome(player.transform.position))
            {
                case "Dust":
                    ReciveItems(player, _config.biomes.dust);
                    break;

                case "Normal":
                    ReciveItems(player, _config.biomes.normal);
                    break;

                case "Tundra":
                    ReciveItems(player, _config.biomes.tundra);
                    break;

                case "Winter":
                    ReciveItems(player, _config.biomes.winter);
                    break;

                case "Undefined":
                    break;
            }
        }

        #endregion

        #region Методы

        [ChatCommand("autokit")]
        private void chatCmd(BasePlayer player, string command, string[] args) 
        {
            if(!permission.UserHasPermission(player.UserIDString, "autokit.admin")) return;

            if(args == null || args?.Length == 0) player.ChatMessage("Не введен аргумент! (dust, tundra, winter, normal)");

            switch(args[0].ToLower())
            {
                case "dust":
                    SetupNewItems(player, "dust");
                    return;

                case "tundra":
                    SetupNewItems(player, "tundra");
                    return;
                    
                case "winter":
                    SetupNewItems(player, "winter");
                    return;

                case "normal":
                    SetupNewItems(player, "normal");
                    return;
            }

            player.ChatMessage("Указан неизвестный аргумент!");
        }
        
        private void SetupNewItems(BasePlayer player, string caseStr) 
        {
            if(caseStr == "dust") 
            {
                _config.biomes.dust.items = new List<Configuration.Biome.ItemConfig>();
                
                foreach(var itemInventory in player.inventory.containerMain.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "main");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.dust.items.Add(item);
                }

                foreach(var itemInventory in player.inventory.containerBelt.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "belt");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.dust.items.Add(item);
                }

                foreach(var itemInventory in player.inventory.containerWear.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "wear");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.dust.items.Add(item);
                }
            }

            if(caseStr == "tundra") 
            {
                _config.biomes.tundra.items = new List<Configuration.Biome.ItemConfig>();

                foreach(var itemInventory in player.inventory.containerMain.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "main");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.tundra.items.Add(item);
                }

                foreach(var itemInventory in player.inventory.containerBelt.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "belt");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.tundra.items.Add(item);
                }

                foreach(var itemInventory in player.inventory.containerWear.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "wear");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.tundra.items.Add(item);
                }
            }

            if(caseStr == "winter") 
            {
                _config.biomes.winter.items = new List<Configuration.Biome.ItemConfig>();

                foreach(var itemInventory in player.inventory.containerMain.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "main");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.winter.items.Add(item);
                }

                foreach(var itemInventory in player.inventory.containerBelt.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "belt");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.winter.items.Add(item);
                }

                foreach(var itemInventory in player.inventory.containerWear.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "wear");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.winter.items.Add(item);
                }
            }

            if(caseStr == "normal") 
            {
                _config.biomes.normal.items = new List<Configuration.Biome.ItemConfig>();

                foreach(var itemInventory in player.inventory.containerMain.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "main");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.normal.items.Add(item);
                }

                foreach(var itemInventory in player.inventory.containerBelt.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "belt");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.normal.items.Add(item);
                }

                foreach(var itemInventory in player.inventory.containerWear.itemList) 
                {
                    Configuration.Biome.ItemConfig item = new Configuration.Biome.ItemConfig(itemInventory.skin, itemInventory.info.shortname, itemInventory.amount, "wear");

                    if(!string.IsNullOrEmpty(itemInventory.name)) item.name = itemInventory.name;
                    _config.biomes.normal.items.Add(item);
                }
            }

            NextTick(SaveConfig);
            player.ChatMessage("Автокит был успешно изменен!");
        }
        
        private static ItemContainer GetContainer(BasePlayer player, string container) 
        {   
            switch(container) 
            {
                case "wear":
                    return player.inventory.containerWear;

                case "belt":
                    return player.inventory.containerBelt;

                case "main":
                    return player.inventory.containerMain;
            }

            return null;
        }
        
        private void ReciveItems(BasePlayer player, Configuration.Biome biome) 
        {
            if(biome.recive && permission.UserHasPermission(player.UserIDString, biome.permission))
            {
                if(biome.strip) player.inventory.Strip();

                foreach(var itemConf in biome.items) 
                {
                    Item item = ItemManager.CreateByName(itemConf.shortname, itemConf.amount, itemConf.id);

                    if(!string.IsNullOrEmpty(itemConf.name)) item.name = itemConf.name;
                    item.MoveToContainer(GetContainer(player, itemConf.container));
                }
            }
        }

        private static string GetBiome(Vector3 pos)
        {
            if (TerrainMeta.BiomeMap.GetBiome(pos, 1) > 0.5f ) return "Dust";
            if (TerrainMeta.BiomeMap.GetBiome(pos, 2) > 0.5f ) return "Normal";
            if (TerrainMeta.BiomeMap.GetBiome(pos, 4) > 0.5f ) return "Tundra";
            if (TerrainMeta.BiomeMap.GetBiome(pos, 8) > 0.5f ) return "Winter";
            
            return "Undefined";
        }

        #endregion
    }
}