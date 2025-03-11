using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NoEscape", "Hougan", "0.1.4")]
    public class NoEscape : RustPlugin
    {
        private static string Layer = "UI_NoEscapeLayer";
        private static List<Blocker> BlockerList = new List<Blocker>();
        
        #region Class

        private class PlayerBlocked : MonoBehaviour
        {
            private BasePlayer Player;
            public Blocker CurrentBlocker;
            private float LastUpdate = 0f;

            private void Awake()
            {
                Player = GetComponent<BasePlayer>(); 
            }
 
            private void Update()
            {
                LastUpdate += Time.deltaTime;
                if (LastUpdate > 1)
                {
                    LastUpdate = 0f;
                    ControllerUpdate();
                }
            }

            private void ControllerUpdate()
            {
                if (CurrentBlocker == null)
                {
                    UnblockPlayer(); 
                    if (Settings.BlockSettings.ShouldBlockEnter)
                    {
                        var blocker = BlockerList.FirstOrDefault(p => p.IsInBlocker(Player));
                        if (!blocker) return;
                    
                        BlockPlayer(blocker); 
                    }
                }
                else
                {
                    UpdateUI();
                }
            }

            private void BlockPlayer(Blocker blocker, bool justCreated = false)
            {
                CurrentBlocker = blocker;
            
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    RectTransform = { AnchorMin = "0.3447913 0.112037", AnchorMax = "0.640625 0.1398148", OffsetMax = "0 0" },
                    Image = { Color = "0.968627453 0.921568632 0.882352948 0.03529412" }
                }, "Hud", Layer);
            
                CuiHelper.AddUi(Player, container);
                UpdateUI();
            }
              
            private const string UpdateLayer = "UI_MagicLayerUpdate";

            public void UpdateUI() 
            {
                CuiHelper.DestroyUi(Player, UpdateLayer);
                CuiHelper.DestroyUi(Player, Layer + ".Info");

                CuiElementContainer container = new CuiElementContainer();
            
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = UpdateLayer, 
                    Components =
                    {//0.441568628 0.609607848 0.327451 1
                        new CuiImageComponent { Color = Settings.NotificationSettings.InterfaceColor },
                        new CuiRectTransformComponent {AnchorMin = $"0 0", AnchorMax = $"{(float) (CurrentBlocker.CurrentTime / CurrentBlocker.TotalTime)} 1", OffsetMin = "1 1", OffsetMax = "-2 -1"},
                    }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = $"БЛОКИРОВКА: {(CurrentBlocker.TotalTime - CurrentBlocker.CurrentTime + 1):F0} СЕК", Font = "robotocondensed-regular.ttf", Color = "0.8 0.8 0.8 0.9", FontSize = 16, Align = TextAnchor.MiddleCenter }
                }, Layer, Layer + ".Info"); 
                
                CuiHelper.AddUi(Player, container);
            }

            private void UnblockPlayer()
            {
                CuiHelper.DestroyUi(Player, Layer);
            }

            private void OnDestroy() => UnblockPlayer();
        }

        private class Blocker : MonoBehaviour
        {
            public double CurrentTime = 0;
            public double TotalTime = Settings.BlockSettings.BlockLength;
            
            public BuildingPrivlidge Cupboard;
            public BaseEntity Alarm;
            public BaseEntity Light;

            public void Awake()
            {
                Cupboard = GetComponent<BuildingPrivlidge>();

                if (Settings.BlockSettings.CreateAlarm)
                {
                    Alarm = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/alarms/audioalarm.prefab", Cupboard.transform.position, default(Quaternion), true);
                     
                    Alarm.Spawn();
                    
                    Alarm.SetFlag(BaseEntity.Flags.Reserved8, true);
                }
                if (Settings.BlockSettings.CreateLighter)
                {
                    Light = GameManager.server.CreateEntity("assets/prefabs/deployable/playerioents/lights/sirenlight/electric.sirenlight.deployed.prefab", Cupboard.transform.position, Quaternion.identity, false);
                
                    
                    Light.enableSaving = true;
                    Light.Spawn();
                    Light.SetParent(Cupboard);
                    Light.transform.localPosition = new Vector3(0, 1.7f, 0);
                    Light.transform.hasChanged = true;
                    Light.SendNetworkUpdate(); 
                    
                    Light.SetFlag(BaseEntity.Flags.Reserved8, true);
                }

                NotifyOwners(Cupboard);
            }

            public void Update()
            {
                CurrentTime += Time.deltaTime;
                if (CurrentTime > TotalTime)
                {
                    if (BlockerList.Contains(this)) 
                        BlockerList.Remove(this);
                    
                    UnityEngine.Object.Destroy(this);
                }
            }

            public void OnDestroy()
            {
                if (Settings.BlockSettings.CreateAlarm)
                {
                    Alarm?.Kill();
                }
                if (Settings.BlockSettings.CreateLighter)
                {
                    Light?.Kill();
                }

                if (BlockerList.Contains(this)) 
                    BlockerList.Remove(this);
            }
            
            public bool IsOwner(ulong userId) => Cupboard.authorizedPlayers.FirstOrDefault(p => p.userid == userId) != null;
            public bool IsInBlocker(BasePlayer player) => Vector3.Distance(player.transform.position, Cupboard.transform.position) < Settings.BlockSettings.BlockerDistance;
        }
        
        #endregion


        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
            
            if (player.GetComponent<PlayerBlocked>() == null)
                player.gameObject.AddComponent<PlayerBlocked>();

            if (player.GetComponent<PlayerBlocked>().CurrentBlocker != null)
            {
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    RectTransform = { AnchorMin = "0.3447913 0.112037", AnchorMax = "0.640625 0.1398148", OffsetMax = "0 0" },
                    Image = { Color = "0.968627453 0.921568632 0.882352948 0.03529412" }
                }, "Hud", Layer);
            
                CuiHelper.AddUi(player, container);
            }
        }
 
        private void Unload()
        {
            UnityEngine.Object.FindObjectsOfType<PlayerBlocked>().ToList().ForEach(UnityEngine.Object.Destroy);
            UnityEngine.Object.FindObjectsOfType<Blocker>().ToList().ForEach(UnityEngine.Object.Destroy);
        }

        private class Configuration
        {
            internal class BlockSetting
            {
                internal class BlockLimit
                {
                    [JsonProperty("Можно ли строить? (фундаменты, стены (из соломы))")] 
                    public bool CanBuild = true;
                    [JsonProperty("Можно ли размещать объекты? (спальники, двери и т.д.)")]
                    public bool CanPlaceObjects = true; 
                    [JsonProperty("Можно ли чинить объекты (фундаменты, двери и т.д.)")]
                    public bool CanRepair = false;
                    [JsonProperty("Можно ли улучшать объекты (фундаменты, стены и т.д.)")]
                    public bool CanUpgrade = false;
                    [JsonProperty("Список запрещенных для использования команд")]
                    public List<string> BlockedCommands = new List<string>();
                }
                
                [JsonProperty("Дистанция действия блокировки (в метрах)")]
                public float BlockerDistance = 150;
                [JsonProperty("Время блокировки в секундах")]
                public float BlockLength = 150;
                [JsonProperty("Создавать сигнальный фонарь при рейде")]
                public bool CreateLighter = true;
                [JsonProperty("Создавать звуковое оповещение при рейде (колонка)")]
                public bool CreateAlarm = true;
                [JsonProperty("Блокировать хозяев построек, даже если они вне зоны рейда")]
                public bool BlockOwnersIfNotInZone = false;
                [JsonProperty("Блокировать игрока, который вошёл в активную зону блокировки")]
                public bool ShouldBlockEnter = true;
                [JsonProperty("Пускать игроков без очереди, если игрока рейдят")]
                public bool ShouldByPassQueue = true;
                [JsonProperty("Разрешить игрокам ломать шкаф (сразу после разрушения шкафа рейд-блок спадёт)")]
                public bool CanDestroyCupBoard = false;
                
                [JsonProperty("Настройки ограничений игроков в рейд-блоке")]
                public BlockLimit BlockLimits = new BlockLimit();
            }

            internal class NotificationSetting
            {
                [JsonProperty("Цвет полосы в интерфейсе у РБ игроков")]
                public string InterfaceColor = "0.121568628 0.419607848 0.627451 0.784313738";
                [JsonProperty("Привилегия, игроки с которой могут установить оповещение о рейде")]
                public string PermissionToInstall = "NoEscape.Install";
                [JsonProperty("Текст уведомления о рейде для отправки")]
                public string NotificationText = "Ваша постройка подверглась нападению!\n" +
                                                 "Скорее заходите и защитесь!";
            }
            
            [JsonProperty("Настройка работы блокировки")]
            public BlockSetting BlockSettings = new BlockSetting();
            [JsonProperty("Настройка оповещения о рейде")]
            public NotificationSetting NotificationSettings = new NotificationSetting();

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    BlockSettings = new BlockSetting
                    {
                        BlockLimits = new BlockSetting.BlockLimit
                        {
                            BlockedCommands = new List<string>
                            {
                                "/shop",
                                "/s",
                                "/chat",
                                "god",
                                "backpack.open" 
                            }
                        }
                    }
                };
            }
        }

        #region Variables
        
        private static Configuration Settings = new Configuration();
        
        #endregion
        
        #region Hooks
        
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
                if (Settings?.BlockSettings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => Settings = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(Settings);

        private bool? OnServerCommand(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return null;
            if (!args.HasArgs(1)) return null;

            if (!IsBlocked(player)) return null;
            
            foreach (var check in Settings.BlockSettings.BlockLimits.BlockedCommands)
            {
                if (args.Args[0].ToLower().Contains(check.ToLower())) 
                {
                    player.ChatMessage("Вам запрещено использовать эту команду во время РБ!");
                    return false;
                }
            }

            return null;
        }
        
        private bool? OnPlayerCommand(ConsoleSystem.Arg args, BasePlayer player)
        {
            if (!args.HasArgs(1)) return null;

            BasePlayer initiator = args.Player();
            if (initiator == null) return null; 
            
            if (!IsBlocked(initiator)) return null;
            string cmd = args.Args[0];

            if (Settings.BlockSettings.BlockLimits.BlockedCommands.Contains(cmd.ToLower()))  
            {
                initiator.ChatMessage("Вам запрещено использовать эту команду во время РБ!");
                return false;
            }

            return null;
        }
        
        private string CupLayer = "UI_CupLayer";
        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!(entity is BuildingPrivlidge)) return;
            if (!VKBot) return;
            
            CuiHelper.DestroyUi(player, CupLayer);
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "192 491", OffsetMax = "573 556" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", CupLayer);

            bool isOn = entity.HasFlag(BaseEntity.Flags.Reserved4);
            string text = isOn ? "ОПОВЕЩЕНИЯ АКТИВИРОВНЫ" : "ОПОВЕЩЕНИЯ ОТКЛЮЧЕНЫ";
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = isOn ? "0.5 0.6 0.5 1" : "0.7 0.5 0.5 1", Command = "UI_NoEscape switch" },
                Text = { Text = "", Align  = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 24 }
            }, CupLayer);

            if (!permission.UserHasPermission(player.UserIDString, Settings.NotificationSettings.PermissionToInstall) && !isOn)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.15", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = isOn ? "0.501486 0.606000789 0.504524 0" : "0.70465 0.50488 0.50655 0", Command = "UI_NoEscape switch" },
                    Text = { Text = isOn ? "ОПОВЕЩЕНИЯ АКТИВИРОВНЫ" : "ОПОВЕЩЕНИЯ ОТКЛЮЧЕНЫ", Align  = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 24 }
                }, CupLayer);
                
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.55", OffsetMax = "0 0" },
                    Button = { Color = isOn ? "0.501486 0.606000789 0.504524 0" : "0.70465 0.50488 0.50655 0", Command = "UI_NoEscape switch" },
                    Text = { Text = "У вас нет доступа к активации оповещений о рейде", Align  = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                }, CupLayer);
            } 
            else
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = isOn ? "0.501486 0.606000789 0.504524 0" : "0.70465 0.50488 0.50655 0", Command = "UI_NoEscape switch" },
                    Text = { Text = isOn ? "ОПОВЕЩЕНИЯ АКТИВИРОВНЫ" : "ОПОВЕЩЕНИЯ ОТКЛЮЧЕНЫ", Align  = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 24 }
                }, CupLayer);
            }
            
            CuiHelper.AddUi(player, container); 
        }

        [ConsoleCommand("UI_NoEscape")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (!args.HasArgs(1) || player == null) return;
            switch (args.Args[0].ToLower())
            {
                case "switch":
                {
                    if (!permission.UserHasPermission(player.UserIDString, Settings.NotificationSettings.PermissionToInstall)) return;
                    string receiver = (string) instance.VKBot.Call("GetUserVKId", player.userID);
                    if (string.IsNullOrEmpty(receiver))
                    {
                        player.ChatMessage($"У вас не подтвержден профиль ВК! Используйте VKBot чтобы сделать это!");
                        return;
                    }
                    
                    var loot = player.inventory.loot.entitySource;
                    if (loot != null && loot is BuildingPrivlidge)
                        loot.SetFlag(BaseEntity.Flags.Reserved4, !loot.HasFlag(BaseEntity.Flags.Reserved4));
                    
                    OnLootEntity(player, loot);
                    break;
                }
            }
        }
        
        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity) => CuiHelper.DestroyUi(player, CupLayer);
        
        private bool? OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BuildingPrivlidge && entity.GetComponent<Blocker>())
            {
                if (info?.InitiatorPlayer != null) info?.InitiatorPlayer.ChatMessage($"Вы не можете разрушить шкаф во время блокировки!");
                info?.damageTypes.ScaleAll(0);
                return false;
            }
            return null;
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is StabilityEntity)) return;
            if (entity.GetBuildingPrivilege() == null) return;
            if (entity is BuildingBlock && (entity as BuildingBlock).currentGrade.gradeBase.type == BuildingGrade.Enum.Twigs) return;
            var owner = info?.InitiatorPlayer;
            if (owner != null && owner.IsBuildingAuthed()) return;
            if (info?.damageTypes.GetMajorityDamageType() == DamageType.Decay) return;
            
            var buildingPrivlidge = entity.GetBuildingPrivilege();

            var alreadyBlock = BlockerList.FirstOrDefault(p => p.Cupboard == buildingPrivlidge);
            if (alreadyBlock) alreadyBlock.CurrentTime = 0;
            else
            {
                var block = buildingPrivlidge.gameObject.AddComponent<Blocker>();
                BlockerList.Add(block);
            }
        }
        private bool? CanBypassQueue(Network.Connection connection) => BlockerList.FirstOrDefault(p => p.IsOwner(connection.userid));
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BasePlayer player = plan.GetOwnerPlayer(); 
            BaseEntity entity = go.ToBaseEntity();
            
            if (entity is BuildingPrivlidge && !permission.UserHasPermission(player.UserIDString, Settings.NotificationSettings.PermissionToInstall))
                entity.SetFlag(BaseEntity.Flags.Reserved4, false);
            
            if (player == null || !IsRaidBlocked(player)) return;

            bool shouldDestroy = false;
            if (!Settings.BlockSettings.BlockLimits.CanBuild && entity is StabilityEntity) shouldDestroy = true;
            else if (!Settings.BlockSettings.BlockLimits.CanPlaceObjects && entity is DecayEntity) shouldDestroy = true;
            else return;

            if (shouldDestroy)
            {
                if (entity is BuildingBlock)
                {
                    (entity as StabilityEntity).BuildCost().ForEach(p =>
                    {
                        var item = ItemManager.Create(p.itemDef, (int) p.amount);
                        if (!player.inventory.GiveItem(item)) item.Drop(player.transform.position, Vector3.down);
                    });
                }
                else if (entity is DecayEntity)
                {
                    var item = ItemManager.GetItemDefinitions().FirstOrDefault(p => p.GetComponent<ItemModDeployable>()?.entityPrefab.resourcePath == entity.PrefabName);
                    if (item == null) return;

                    if (!player.inventory.GiveItem(ItemManager.Create(item)))
                        ItemManager.Create(item).Drop(player.transform.position, Vector3.down);
                }
            
                player.ChatMessage($"Вам запрещено совершать это действие во время РБ!");
                entity.Kill();
            }
        }
        
        private bool? CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (!IsRaidBlocked(player)) return null;
            
            player.ChatMessage($"Вам запрещено улучшать объекты во время РБ!"); 
            return false;
        }
        
        private bool? OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            if (!IsRaidBlocked(player)) return null;

            player.ChatMessage($"Вам запрещено чинить объекты во время РБ!"); 
            return false;
        }
        
        #endregion

        #region Functions

        [PluginReference] private Plugin VKBot;
        private static void NotifyOwners(BuildingPrivlidge building)
        {
            foreach (var check in building.authorizedPlayers)
            {
                string receiver = (string) instance.VKBot?.Call("GetUserVKId", check.userid) ?? "";
                if (string.IsNullOrEmpty(receiver)) continue;

                instance.VKBot?.Call("SendVkMessage", receiver, Settings.NotificationSettings.NotificationText);
            }   
        }

        #endregion

        private static NoEscape instance; 
        private void OnServerInitialized()
        {
            instance = this;
            timer.Once(1, () =>
            {
                BasePlayer.activePlayerList.ForEach(OnPlayerInit);
                PrintError("NoEscape initialized (HOUGAN)");
            });
            
            permission.RegisterPermission(Settings.NotificationSettings.PermissionToInstall, this);
            
            if (Settings.BlockSettings.CanDestroyCupBoard) Unsubscribe(nameof(OnEntityTakeDamage));
            if (Settings.BlockSettings.BlockLimits.CanRepair) Unsubscribe(nameof(OnStructureRepair));
            if (Settings.BlockSettings.BlockLimits.CanUpgrade) Unsubscribe(nameof(CanAffordUpgrade));
            if (!Settings.BlockSettings.ShouldByPassQueue) Unsubscribe(nameof(CanBypassQueue));
        }


        #region API

        private bool IsBlocked(BasePlayer player) => IsRaidBlocked(player);
        private bool IsRaidBlock(ulong userId) => IsRaidBlocked(userId.ToString());
        private bool IsRaidBlocked(BasePlayer player) => player.GetComponent<PlayerBlocked>()?.CurrentBlocker != null;
        private bool IsRaidBlocked(string player)
        {
            BasePlayer target = BasePlayer.Find(player);
            if (!target || !target.IsConnected) return false;

            return IsRaidBlocked(target);
        }
        private int ApiGetTime(ulong userId)
        {
            if (!IsRaidBlocked(userId.ToString())) return 0;
            var targetBlock = BasePlayer.Find(userId.ToString()).GetComponent<PlayerBlocked>(); 
            return (int) (targetBlock.CurrentBlocker.TotalTime - targetBlock.CurrentBlocker.CurrentTime);
        }

        private string CanTeleport(BasePlayer player)
        {
            if (IsBlocked(player)) return "Вы не можете телепортироваться в зоне рейда!";
            return null;
        }

        private string CanTrade(BasePlayer player)
        {
            if (IsBlocked(player)) return "Вы не можете обмениваться ресурсами во время рейда!";
            return null; 
        }

        #endregion
    }
}