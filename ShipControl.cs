using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ShipControl", "Hougan", "0.0.2")]
    public class ShipControl : RustPlugin
    {
        #region Classes

        private class AdditionalContainer
        {
            [JsonProperty("Отображаемое имя")]
            public string DisplayName;
            [JsonProperty("Название префаба")]
            public string PrefabName;

            [JsonProperty("Локальная позиция")]
            public string LocalPosition;
            [JsonProperty("Возможные предметы")]
            public List<Dictionary<string, int>> RandomItems = new List<Dictionary<string, int>>();
        }

        private class Configuration
        {
            [JsonProperty("Отключить появление корабля на совсем")]
            public bool DisableEvent = false;

            [JsonProperty("Время плавания корабля")]
            public float ActiveTime = 40f;
            [JsonProperty("Время уплывания корабля")]
            public float DeActivateTime = 10f;

            [JsonProperty("Оповещение о прибытии корабля")]
            public string SpawnedAlert = "К берегу приблежается грузовой корабль!";
            
            [JsonProperty("Отключить появление лодки на борту")]
            public bool DisableRHIB = true;
            [JsonProperty("Изменить количество топлива в лодке на борту")]
            public int RHIB_Fuel = 150;

            [JsonProperty("Отключить появление стандартных контейнеров")]
            public bool DisableDefaultContainers = true;
            
            [JsonProperty("Дополнительные ящики с предметами")]
            public List<AdditionalContainer> CustomContainers = new List<AdditionalContainer>();

            public static Configuration GetNewConf()
            {
                return new Configuration();
            }
        }

        #endregion
        
        #region Variables

        public bool EventEnabled = false;
        public float EventDuration = 40f;
        public float EgressDuration = 10f;


        public float currentRadition;

        private List<AdditionalContainer> PossibleContainers = new List<AdditionalContainer>
        {
            new AdditionalContainer
            {
                PrefabName = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                DisplayName = "Элитный ящик",
                
                LocalPosition = ""
            },
            new AdditionalContainer
            {
                PrefabName = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                DisplayName = "Оружейный ящик",
                
                LocalPosition = ""
            },
            new AdditionalContainer
            {
                PrefabName = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                DisplayName = "Обычный ящик",
                
                LocalPosition = ""
            },
            new AdditionalContainer
            {
                PrefabName = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab",
                DisplayName = "Закрытый ящик",
                
                LocalPosition = ""
            }
        };

        private static Configuration config;


        #endregion

        #region Initialization

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.CustomContainers == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConf();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion
        


        [ChatCommand("add.crate")]
        private void cmdChatAdmin(BasePlayer player)
        {
            if (!player.IsAdmin)
                return;
            
            RaycastHit hitInfo;
            if (!Physics.Raycast(player.transform.position, Vector3.down, out hitInfo) || !(hitInfo.GetEntity() is CargoShip))
            {
                player.ChatMessage("Вы находитесь не на корабле!");
                return;
            }

            UI_DrawChooseType(player);
        }

        [ConsoleCommand("UI_SC")]
        private void cmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player != null && args.HasArgs(2))
            {
                RaycastHit hitInfo;
                if (!Physics.Raycast(player.transform.position, Vector3.down, out hitInfo) || !(hitInfo.GetEntity() is CargoShip))
                {
                    player.ChatMessage("Вы находитесь не на корабле!");
                    return;
                }
                CargoShip cargoShip = hitInfo.GetEntity() as CargoShip;
                
                switch (args.Args[0].ToLower())
                {
                    case "addcrate":
                    {
                        AdditionalContainer newContainer = PossibleContainers.Find(p => p.PrefabName == args.Args[1]);
                        if (newContainer != null)
                        {
                            config.CustomContainers.Add(new AdditionalContainer
                            {
                                DisplayName = newContainer.DisplayName,
                                PrefabName = newContainer.PrefabName,
                                LocalPosition = cargoShip.transform.InverseTransformPoint(player.transform.position).ToString(),
                                RandomItems = new List<Dictionary<string, int>>
                                {
                                    new Dictionary<string, int>
                                    {
                                        ["rifle.ak"] = 1
                                    }
                                }
                            });

                            SpawnCustomContainer(config.CustomContainers.Last(), cargoShip);
                            
                            player.ChatMessage($"Вы успешно добавили {newContainer.DisplayName}, не забудьте настроить выпадающий лут!");
                            CuiHelper.DestroyUi(player, Layer);
                            SaveConfig();
                        }
                        break;
                    }
                }
            }
        }


        #region Interface

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        
        
        private const string Layer = "UI_SC";

        private void UI_DrawChooseType(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-10000 -10000", OffsetMax = "10000 10000" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            var topPosition = 0.5f + (double) PossibleContainers.Count / 2 * 40 + (double) (PossibleContainers.Count - 1) / 2 * 5;
            foreach (var check in PossibleContainers)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = $"-300 {topPosition - 40}", OffsetMax = $"300 {topPosition}" },
                    Button = { Color = HexToRustFormat("#7C7D7A51"), Command = $"UI_SC AddCrate {check.PrefabName}" },
                    Text = { Text = check.DisplayName, Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, Layer);

                topPosition -= 40;
                topPosition -= 5;
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion
        
        private void OnServerInitialized()
        {
            if (EventEnabled)
            {
                Server.Command($"cargoship.event_duration_minutes {config.ActiveTime}");
                Server.Command($"cargoship.egress_duration_minute {config.DeActivateTime}");
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is CargoShip)
            {
                if (config.DisableEvent)
                {
                    entity.Kill();
                    return;
                }

                if (config.SpawnedAlert != "")
                {
                    Server.Broadcast(config.SpawnedAlert);
                }
                
                var cargoShip = entity as CargoShip;
                var cargoRhib = cargoShip.children.Find(p => p is RHIB) as RHIB;

                if (config.DisableDefaultContainers)
                {
                    var containerList = cargoShip.children.Where(p => p is StorageContainer).ToArray();
                    foreach (var check in containerList)
                    {
                        check.Kill();
                    }
                }
                
                if (config.CustomContainers.Count > 0)
                {
                    timer.Once(120, () =>
                    {
                        if (cargoShip != null)
                        {
                            foreach (var check in config.CustomContainers)
                            {
                                SpawnCustomContainer(check, cargoShip);
                            }
                        }
                    });
                }

                if (config.DisableRHIB && cargoRhib != null)
                {
                    cargoRhib.Kill();
                }
                else if (cargoRhib != null)
                {
                    BaseEntity baseEntity = cargoRhib.fuelStorageInstance.Get(true);
                    if (!(bool) ((UnityEngine.Object) baseEntity))
                        return;
                    
                    baseEntity.GetComponent<StorageContainer>().inventory.Clear();
                    NextTick(() =>
                    {
                        baseEntity.GetComponent<StorageContainer>().inventory.AddItem(ItemManager.FindItemDefinition("lowgradefuel"), config.RHIB_Fuel);
                    });
                }
            }
        }

        private void SpawnCustomContainer(AdditionalContainer customContainer, CargoShip ship)
        {
            Vector3 position = customContainer.LocalPosition.ToVector3() + ship.transform.position;
            Quaternion rotation = default(Quaternion);
            BaseEntity entity = GameManager.server.CreateEntity(customContainer.PrefabName, position, rotation, true);
            if ((bool) ((UnityEngine.Object) entity))
            {
                entity.enableSaving = false;
                entity.SendMessage("SetWasDropped");
                entity.Spawn();
                entity.SetParent((BaseEntity) ship, true, false);
                
                if (entity.GetComponent<Rigidbody>() != null)
                    entity.GetComponent<Rigidbody>().isKinematic = true;

                var itemContainer = entity.GetComponent<StorageContainer>();
                if (customContainer.RandomItems.Count > 0)
                {
                    itemContainer.inventory.Clear();
                    foreach (var check in customContainer.RandomItems.GetRandom())
                    {
                        Item x = ItemManager.CreateByPartialName(check.Key, check.Value);
                        x.MoveToContainer(itemContainer.inventory);
                    }
                }
            }
        }
        
    }
}