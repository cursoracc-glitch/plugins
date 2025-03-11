using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SnowMobile","megargan","0.0.3")]
    class SnowMoped : RustPlugin
    {
        #region Configuration
        private static Configuration _config = new Configuration();
        public class Configuration
        {
            [JsonProperty("Скин ID обычного снегохода")]
            public ulong SkinIDSimple { get; set; } = 2742584081;
            
            [JsonProperty("Скин ID снегохода tomaha")]
            public ulong SkinIDTomaha { get; set; } = 2745113546;

            [JsonProperty("Скин ID снегохода ultra")]
            public ulong SkinIDUltra { get; set; } = 2750417732;

            [JsonProperty("Количество топлива внутри снегохода")]
            public int FuelAmount { get; set; } = 100;

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                };
            }
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
            }
            catch
            {
                Puts("!!!!ОШИБКА КОНФИГУРАЦИИ!!!! создаем новую");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        private void OnServerInitialized()
        {
            if (_config.SkinIDTomaha == 0 || _config.SkinIDSimple == 0 || _config.SkinIDUltra == 0)
            {
                Puts("Значение skinID в конфиге не может быть 0");
                Interface.Oxide.UnloadPlugin(Name);
            }
        }
        protected override void LoadDefaultConfig() => _config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner == null || prefab == null || target.player == null) return null;
            if (planner.skinID != _config.SkinIDTomaha && planner.skinID != _config.SkinIDSimple) return null;
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(target.position.x, target.position.y + 1, target.position.z),Vector3.down, out hit, 2f,
                    LayerMask.GetMask(new string[] {"Terrain", "Construction" })) && !hit.collider.name.Contains("building core") && !hit.collider.name.Contains("rock_cliff") && hit.GetEntity() == null) return null;
            SendReply(target.player, "Нельзя ставить на скалах или постройках!");
            return false;
        }
        private void OnEntitySpawned(StorageContainer entity)
        {
            if (entity.skinID == _config.SkinIDSimple || entity.skinID == _config.SkinIDTomaha || entity.skinID == _config.SkinIDUltra)
            {
                var transform = entity.transform;
                Vector3 ePos = transform.position;
                Snowmobile snowmobil = null;
                if (entity.skinID == _config.SkinIDSimple)
                {
                    snowmobil = GameManager.server.CreateEntity(
                        "assets/content/vehicles/snowmobiles/snowmobile.prefab", ePos,
                        transform.rotation * new Quaternion(0, 1f, 0, 1f), true) as Snowmobile;
                }
                else if (entity.skinID == _config.SkinIDTomaha)
                {
                    snowmobil = GameManager.server.CreateEntity(
                        "assets/content/vehicles/snowmobiles/tomahasnowmobile.prefab", ePos,
                        transform.rotation * new Quaternion(0, 1f, 0, 1f), true) as Snowmobile;
                } else if (entity.skinID == _config.SkinIDUltra)
                {
                    snowmobil = GameManager.server.CreateEntity(
                        "assets/content/vehicles/snowmobiles/tomahasnowmobile.prefab", ePos,
                        transform.rotation * new Quaternion(0, 1f, 0, 1f), true) as Snowmobile;
                    snowmobil.engineKW = 90000;
                    snowmobil.badTerrainDrag = 0f;
                    snowmobil.airControlStability = 10000f;
                    snowmobil.hurtTriggerMinSpeed = 10000f;
                } else return;
                snowmobil.Spawn();
                if (_config.FuelAmount > 0)
                    snowmobil.GetFuelSystem().GetFuelContainer().inventory.AddItem(ItemManager.FindItemDefinition("lowgradefuel"), _config.FuelAmount);
                
                NextTick(() => entity.Kill());
            } else return;
        }

        [ChatCommand("givemobile")]
        private void MobileAddChat(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (args == null || args.Length == 0)
            {
                player.ChatMessage("snowmobile.add [nick/id] [1-3] 1 - обычный 2 - tomaha 3 - ultra");
                return;
            }

            BasePlayer target = BasePlayer.Find(args[0]);
            int snowtype = 0;
            if (!int.TryParse(args[1], out snowtype))
            {
                player.ChatMessage("snowmobile.add [nick/id] [1-3] 1 - обычный 2 - tomaha 3 - ultra");
                return;
            }
            CreateMobile(target, snowtype);
        }
        [ConsoleCommand("snowmobile.add")]
        private void MobileAdd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if(!arg.IsAdmin) return;
            if (!arg.HasArgs())
            {
                PrintWarning("snowmobile.add [nick/id] [1-3] 1 - обычный 2 - tomaha 3 - ultra");
                if(player != null) player.ChatMessage("snowmobile.add [nick/id] [1-3] 1 - обычный 2 - tomaha 3 - ultra");
                return;
            }
            BasePlayer target = BasePlayer.Find(arg.Args[0]);
            if (target == null)
            {
                PrintWarning("Игрок не найден!");
                if(player != null) player.ChatMessage("Игрок не найден!");
            }
            int snowtype = 0;
            if (!int.TryParse(arg.Args[1], out snowtype))
            {
                PrintWarning("snowmobile.add [nick/id] [1-3] 1 - обычный 2 - tomaha 3 - ultra");
                if(player != null) player.ChatMessage("snowmobile.add [nick/id] [1-3] 1 - обычный 2 - tomaha 3 - ultra");
                return;
            }
            CreateMobile(target, snowtype);
        }

        private void CreateMobile(BasePlayer target, int snowtype)
        {
            Item snow = null;
            switch (snowtype)
            {
                case 1:
                {
                    snow = ItemManager.CreateByName("coffin.storage", 1, _config.SkinIDSimple);
                    snow.name = "SNOW MOBILE";
                    break;
                }
                case 2:
                {
                    snow = ItemManager.CreateByName("coffin.storage", 1, _config.SkinIDTomaha);
                    snow.name = "SNOW MOBILE TOMAHA";
                    break;
                }
                case 3:
                {
                    snow = ItemManager.CreateByName("coffin.storage", 1, _config.SkinIDUltra);
                    snow.name = "SNOW MOBILE TOMAHA-ULTRA";
                    break;
                }
            }
            if (snow != null)
            {
                
                if (!target.inventory.GiveItem(snow))
                    snow.Drop(target.inventory.containerMain.dropPosition, target.inventory.containerMain.dropVelocity);
            }
        }
    }
}