using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("NoEscape", "OxideBro", "2.3.2")]
    public class NoEscape : RustPlugin
    {
        #region Class
        private static List<SphereComponent> BlockerList = new List<SphereComponent>();

        private class PlayerBlockStatus : FacepunchBehaviour
        {
            private BasePlayer Player;
            public SphereComponent CurrentBlocker;
            public double CurrentTime = config.BlockSettings.BlockLength;

            public static PlayerBlockStatus Get(BasePlayer player)
            {
                return player.GetComponent<PlayerBlockStatus>() ?? player.gameObject.AddComponent<PlayerBlockStatus>();
            }

            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
            }

            private void ControllerUpdate()
            {
                if (CurrentBlocker != null)
                    UpdateUI();
                else
                    UnblockPlayer();
            }

            public void CreateUI()
            {
                CuiHelper.DestroyUi(Player, "NoEscape");
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = config.UISettings.AnchorMin, AnchorMax = config.UISettings.AnchorMax, OffsetMax = "0 0" },
                    Image = { Color = config.UISettings.InterfaceColorBP }
                }, "Hud", "NoEscape");
                CuiHelper.AddUi(Player, container);
                if (CurrentBlocker != null) UpdateUI();
            }

            public void BlockPlayer(SphereComponent blocker, bool justCreated)
            {
                if (ins.permission.UserHasPermission(Player.UserIDString, config.BlockSettings.PermissionToIgnore))
                {
                    UnblockPlayer();
                    return;
                }
                if (justCreated)
                    Player.ChatMessage(string.Format(ins.Messages["blockactiveAttacker"], NumericalFormatter.FormatTime(config.BlockSettings.BlockLength)));
                CurrentBlocker = blocker;
                CurrentTime = CurrentBlocker.CurrentTime;
                CreateUI();
                InvokeRepeating(ControllerUpdate, 1f, 1f);
            }

            public void UpdateUI()
            {
                CurrentTime++;
                CuiHelper.DestroyUi(Player, "NoEscape_update");
                CuiHelper.DestroyUi(Player, "NoEscape" + ".Info");

                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Parent = "NoEscape",
                    Name = "NoEscape_update",
                    Components =
                    {
                        new CuiImageComponent { Color = config.UISettings.InterfaceColor },
                        new CuiRectTransformComponent {AnchorMin = $"0 0", AnchorMax = $"{(float) (CurrentBlocker.TotalTime - CurrentBlocker.CurrentTime) / CurrentBlocker.TotalTime} 1", OffsetMin = "0 0", OffsetMax = "0 0"},
                    }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = string.Format(ins.Messages["guitimertext"], ins.GetFormatTime(TimeSpan.FromSeconds(CurrentBlocker.TotalTime - CurrentBlocker.CurrentTime))), Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.9", FontSize = 8, Align = TextAnchor.MiddleCenter }
                }, "NoEscape", "NoEscape" + ".Info");

                CuiHelper.AddUi(Player, container);
                if (CurrentTime >= config.BlockSettings.BlockLength)
                    UnblockPlayer();
            }

            public void UnblockPlayer()
            {
                if (Player == null)
                {
                    Destroy(this);
                    return;
                }
                Player.ChatMessage(ins.Messages["blocksuccess"]);
                CancelInvoke(ControllerUpdate);
                CuiHelper.DestroyUi(Player, "NoEscape");
                CurrentBlocker = null;
            }
            private void OnDestroy()
            {
                CuiHelper.DestroyUi(Player, "NoEscape");
                Destroy(this);
            }
        }

        public class SphereComponent : FacepunchBehaviour
        {
            SphereCollider sphereCollider;
            public BasePlayer initPlayer;
            public List<ulong> Privilage = null;
            public ulong OwnerID;
            public double CurrentTime = 0;
            public double TotalTime = config.BlockSettings.BlockLength;
            void Awake()
            {
                var reply = 288;
                if (reply == 0) { }
                gameObject.layer = (int)Layer.Reserved1;
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = config.BlockSettings.BlockerDistance;
            }

            public void Init(BasePlayer player, ulong owner, List<ulong> privilage)
            {
                initPlayer = player;
                OwnerID = owner;
                Privilage = privilage;
            }

            private void OnTriggerEnter(Collider other)
            {
                var target = other.GetComponentInParent<BasePlayer>();
                if (target == null) return;

                if (PlayerBlockStatus.Get(target).CurrentBlocker != null && PlayerBlockStatus.Get(target).CurrentBlocker == this && PlayerBlockStatus.Get(target).CurrentTime > CurrentTime)
                {
                    PlayerBlockStatus.Get(target).CurrentTime = CurrentTime;
                    return;
                }
                if (PlayerBlockStatus.Get(target).CurrentBlocker != null && PlayerBlockStatus.Get(target).CurrentBlocker != this && PlayerBlockStatus.Get(target).CurrentTime > CurrentTime)
                {
                    target.ChatMessage(string.Format(ins.Messages["enterRaidZone"], NumericalFormatter.FormatTime(config.BlockSettings.BlockLength - CurrentTime)));
                    PlayerBlockStatus.Get(target).CurrentTime = CurrentTime;
                    PlayerBlockStatus.Get(target).CurrentBlocker = this;
                    return;
                }
                if (config.BlockSettings.ShouldBlockEnter && (PlayerBlockStatus.Get(target).CurrentBlocker == null || PlayerBlockStatus.Get(target).CurrentBlocker != this))
                {
                    PlayerBlockStatus.Get(target).BlockPlayer(this, false);
                    target.ChatMessage(string.Format(ins.Messages["enterRaidZone"], NumericalFormatter.FormatTime(config.BlockSettings.BlockLength - CurrentTime)));
                    return;
                }
            }

            private void OnTriggerExit(Collider other)
            {
                if (!config.BlockSettings.UnBlockExit) return;
                var target = other.GetComponentInParent<BasePlayer>();
                if (target != null && target.userID.IsSteamId() && PlayerBlockStatus.Get(target).CurrentBlocker == this)
                    PlayerBlockStatus.Get(target).UnblockPlayer();
            }

            public void FixedUpdate()
            {
                CurrentTime += Time.deltaTime;
                if (CurrentTime > TotalTime)
                {
                    if (BlockerList.Contains(this))
                        BlockerList.Remove(this);
                    Destroy(this);
                }
            }

            public void OnDestroy()
            {
                Destroy(this);
            }

            public bool IsInBlocker(BaseEntity player) => Vector3.Distance(player.transform.position, transform.position) < config.BlockSettings.BlockerDistance;
        }
        #endregion

        #region Variables

        static PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за покупку плагина на сайте RustPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        void Loaded()
        {
            if (!config.PlayerBlockSettings.CanRepair) Unsubscribe(nameof(OnStructureRepair));
            else Subscribe(nameof(OnStructureRepair));
            if (!config.PlayerBlockSettings.CanUpgrade) Unsubscribe(nameof(CanAffordUpgrade));
            else Subscribe(nameof(CanAffordUpgrade));
            if (!config.PlayerBlockSettings.CanDefaultremove) Unsubscribe(nameof(OnStructureDemolish));
            else Subscribe(nameof(OnStructureDemolish));
            if (!config.PlayerBlockSettings.CanBuild && !config.PlayerBlockSettings.CanPlaceObjects) Unsubscribe(nameof(CanBuild));
            else Subscribe(nameof(CanBuild));
            permission.RegisterPermission(config.BlockSettings.PermissionToIgnore, this);
            permission.RegisterPermission(config.VkBotMessages.VkPrivilage, this);
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                PrintWarning("Config update detected! Updating config values...");

                if (config.PluginVersion < new VersionNumber(2, 2, 0))
                {
                    config.BlockSettings.WriteListDestroyEntity = new List<string>()
                    {
                        "barricade.metal",
                         "bed_deployed"
                    };
                    PrintWarning("Added Write List entity");
                }
                if (config.PluginVersion < new VersionNumber(2, 3, 1))
                {
                    config.PlayerBlockSettings.BlackListCommands = new List<string>()
                    {
                        "/bp",
                        "backpack.open",
                        "/trade"
                    };

                    PrintWarning("Added Black List commands");
                }
                PrintWarning("Config update completed!");
                config.PluginVersion = Version;
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class UISettings
        {
            [JsonProperty("Цвет полосы активный полосы")]
            public string InterfaceColor = "0.121568628 0.419607848 0.627451 0.784313738";

            [JsonProperty("Цвет фона")]
            public string InterfaceColorBP = "1 1 1 0.3";

            [JsonProperty("Позиция AnchorMin")]
            public string AnchorMin = "0.3447913 0.112037";

            [JsonProperty("Позиция AnchorMax")]
            public string AnchorMax = "0.640625 0.1398148";
        }

        public class BlockSettings
        {
            [JsonProperty("Радиус зоны блокировки")]
            public float BlockerDistance = 150;

            [JsonProperty("Общее время блокировки в секундах")]
            public float BlockLength = 150;

            [JsonProperty("Блокировать создателя объекта какой разрушили, даже если он вне зоны рейда")]
            public bool BlockOwnersIfNotInZone = true;

            [JsonProperty("Блокировать игрока, который вошёл в активную зону блокировки")]
            public bool ShouldBlockEnter = true;

            [JsonProperty("Снимать блокировку с игрока если он вышел из зоны блокировки?")]
            public bool UnBlockExit = false;

            [JsonProperty("Не создавать блокировку если разрушенный объект не в зоне шкафа (Нету билды)")]
            public bool EnabledBuildingBlock = false;

            [JsonProperty("Блокировать всех игроков какие авторизаваны в шкафу (Если шкаф существует, и авторизованный игрок на сервере)")]
            public bool EnabledBlockAutCupboard = false;

            [JsonProperty("Привилегия, игроки с которой игнорируются РБ (на них он не действует")]
            public string PermissionToIgnore = "noescape.ignore";

            [JsonProperty("Белый список entity при разрушении каких не действует блокировка")]
            public List<string> WriteListDestroyEntity = new List<string>();
        }

        public class VkBotMessages
        {
            [JsonProperty("Включить отправку сообщения в ВК оффлайн игроку через VkBot")]
            public bool EnabledVkBOT = false;
            [JsonProperty("Сообщение какое будет отправлено игроку ({0} - Имя атакуещего, {1} - Квадрат на карте)")]
            public string Messages = "Внимание! Игрок {0} начал рейд вашего строения в квадрате {1} на сервере SERVERNAME.";
            [JsonProperty("Привилегия на использование оффлайн уведомления")]
            public string VkPrivilage = "noescape.vknotification";
        }

        public class PlayerBlockSettings
        {
            [JsonProperty("Блокировать использование китов")]
            public bool CanUseKits = true;

            [JsonProperty("Блокировать обмен между игроками (Trade)")]
            public bool CanUseTrade = true;

            [JsonProperty("Блокировать телепорты")]
            public bool CanTeleport = true;

            [JsonProperty("Блокировать удаление построек (CanRemove)")]
            public bool CanRemove = true;

            [JsonProperty("Блокировать улучшение построек (Upgrade, BuildingUpgrade и прочее)")]
            public bool CanBGrade = true;

            [JsonProperty("Блокировать удаление построек (стандартное)")]
            public bool CanDefaultremove = true;

            [JsonProperty("Блокировать строительство")]
            public bool CanBuild = true;

            [JsonProperty("Блокировать установку объектов")]
            public bool CanPlaceObjects = true;

            [JsonProperty("Блокировать ремонт построек (стандартный)")]
            public bool CanRepair = true;

            [JsonProperty("Блокировать улучшение построек (стандартное)")]
            public bool CanUpgrade = true;

            [JsonProperty("Белый список предметов какие можно строить при блокировке")]
            public List<string> WriteListBuildEntity = new List<string>();

            [JsonProperty("Черный список команд какие запрещены при рейд блоке (Чатовые и консольные)")]
            public List<string> BlackListCommands = new List<string>();

        }

        private class PluginConfig
        {
            [JsonProperty("Настройка UI")]
            public UISettings UISettings = new UISettings();

            [JsonProperty("Общая настройка блокировки")]
            public BlockSettings BlockSettings = new BlockSettings();

            [JsonProperty("Настройка запретов для игрока")]
            public PlayerBlockSettings PlayerBlockSettings = new PlayerBlockSettings();

            [JsonProperty("Настройка VkBOT")]
            public VkBotMessages VkBotMessages = new VkBotMessages();

            [JsonProperty("Версия конфигурации")]
            public VersionNumber PluginVersion = new VersionNumber();

            [JsonIgnore]
            [JsonProperty("Инициализация плагина")]
            public bool Init = false;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig
                {
                    BlockSettings = new BlockSettings()
                    {
                        BlockerDistance = 150,
                        BlockLength = 150,
                        BlockOwnersIfNotInZone = true,
                        ShouldBlockEnter = true,
                        UnBlockExit = false,
                        EnabledBuildingBlock = false,
                        EnabledBlockAutCupboard = false,
                        PermissionToIgnore = "noescape.ignore",
                        WriteListDestroyEntity = new List<string>()
                        {
                            "barricade.metal",
                            "bed_deployed"
                        }
                    },
                    PlayerBlockSettings = new PlayerBlockSettings()
                    {
                        CanUseKits = true,
                        CanUseTrade = true,
                        CanTeleport = true,
                        CanRemove = true,
                        CanBGrade = true,
                        CanDefaultremove = true,
                        CanBuild = true,
                        CanPlaceObjects = true,
                        CanRepair = true,
                        CanUpgrade = true,
                        WriteListBuildEntity = new List<string>()
                        {
                             "wall.external.high.stone",
                             "barricade.metal"
                        }
                    },
                    UISettings = new UISettings()
                    {
                        InterfaceColor = "0.12 0.41 0.62 0.78",
                        InterfaceColorBP = "1 1 1 0.3",
                        AnchorMin = "0.3447913 0.112037",
                        AnchorMax = "0.640625 0.1398148",
                    },
                    VkBotMessages = new VkBotMessages()
                    {
                        EnabledVkBOT = false,
                        Messages = "Внимание! Игрок {0} начал рейд вашего строения в квадрате {1} на сервере SERVERNAME.",
                        VkPrivilage = "noescape.vknotification",
                    },
                    PluginVersion = new VersionNumber(),
                };
            }
        }

        #endregion

        #region Oxide⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        private static NoEscape ins;
        private void OnServerInitialized()
        {
            ins = this;
            config.Init = true;
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            SphereComponent ActiveRaidZone = GetRaidZone(player.transform.position);
            if (ActiveRaidZone == null)
            {
                if (config.BlockSettings.UnBlockExit)
                {
                    if (PlayerBlockStatus.Get(player).CurrentBlocker != null)
                        PlayerBlockStatus.Get(player).UnblockPlayer();
                }
                return;
            }

            if (PlayerBlockStatus.Get(player).CurrentBlocker != null)
            {

                if (PlayerBlockStatus.Get(player).CurrentBlocker != ActiveRaidZone)
                    PlayerBlockStatus.Get(player).BlockPlayer(ActiveRaidZone, false);
            }
            else
            {
                player.ChatMessage(string.Format(Messages["enterRaidZone"], NumericalFormatter.FormatTime(config.BlockSettings.BlockLength - ActiveRaidZone.CurrentTime)));
                PlayerBlockStatus.Get(player)?.BlockPlayer(ActiveRaidZone, false);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                timer.In(1f, () => OnPlayerConnected(player));
                return;
            }
            if (PlayerBlockStatus.Get(player).CurrentBlocker != null)
                PlayerBlockStatus.Get(player).CreateUI();
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                if (PlayerBlockStatus.Get(player) != null)
                    UnityEngine.Object.Destroy(PlayerBlockStatus.Get(player));
            BlockerList.RemoveAll(x =>
            {
                UnityEngine.Object.Destroy(x);
                return true;
            });
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!config.Init) return;
            if (entity == null || info == null || info.InitiatorPlayer == null || !(entity is StabilityEntity || entity is ShopFront || entity is BuildingPrivlidge)
                || config.BlockSettings.EnabledBuildingBlock && entity.GetBuildingPrivilege() == null || entity.OwnerID == 0) return;
            if (entity is BuildingBlock && (entity as BuildingBlock).currentGrade.gradeBase.type == BuildingGrade.Enum.Twigs
                || info?.damageTypes.GetMajorityDamageType() == DamageType.Decay || config.BlockSettings.WriteListDestroyEntity.Contains(entity.ShortPrefabName)) return;
            var alreadyBlock = BlockerList.FirstOrDefault(p => Vector3.Distance(entity.transform.position, p.transform.position) < (config.BlockSettings.BlockerDistance / 2));
            if (alreadyBlock)
            {
                alreadyBlock.CurrentTime = 0;
                if (config.BlockSettings.BlockOwnersIfNotInZone)
                {
                    var OwnerPlayer = BasePlayer.FindByID(entity.OwnerID);
                    if (OwnerPlayer != null)
                        PlayerBlockStatus.Get(OwnerPlayer).BlockPlayer(alreadyBlock, false);
                }
                PlayerBlockStatus.Get(info.InitiatorPlayer).BlockPlayer(alreadyBlock, false);
                if (entity.GetBuildingPrivilege() != null && config.BlockSettings.EnabledBlockAutCupboard)
                {
                    foreach (var aplayer in entity.GetBuildingPrivilege().authorizedPlayers)
                    {
                        var AuthPlayer = BasePlayer.Find(aplayer.userid.ToString());
                        if (AuthPlayer != null && AuthPlayer != info.InitiatorPlayer && AuthPlayer.IsConnected)
                            PlayerBlockStatus.Get(AuthPlayer).BlockPlayer(alreadyBlock, false);
                        else if (AuthPlayer == null || !AuthPlayer.IsConnected) SendOfflineMessages(entity.transform.position, info.InitiatorPlayer.displayName, aplayer.userid);
                    }
                }
                var col = Vis.colBuffer;
                var count = Physics.OverlapSphereNonAlloc(alreadyBlock.transform.position, config.BlockSettings.BlockerDistance, col, LayerMask.GetMask("Player (Server)"));
                for (int i = 0; i < count; i++)
                {
                    var player = col[i].ToBaseEntity() as BasePlayer;
                    if (player == null) continue;
                    PlayerBlockStatus.Get(player).BlockPlayer(alreadyBlock, false);
                }
            }
            else
            {
                var obj = new GameObject();
                obj.transform.position = entity.transform.position;
                var sphere = obj.AddComponent<SphereComponent>();
                sphere.GetComponent<SphereComponent>().Init(info.InitiatorPlayer, entity.OwnerID, entity.GetBuildingPrivilege() != null ? entity.GetBuildingPrivilege().authorizedPlayers.Select(p => p.userid).ToList() : null);
                BlockerList.Add(sphere);
                PlayerBlockStatus.Get(info.InitiatorPlayer).BlockPlayer(sphere, true);
                var OwnerPlayer = BasePlayer.FindByID(entity.OwnerID);
                if (OwnerPlayer == null || !OwnerPlayer.IsConnected)
                {
                    SendOfflineMessages(entity.transform.position, info.InitiatorPlayer.displayName, entity.OwnerID);
                    return;
                }
                else if (OwnerPlayer != null && OwnerPlayer != info.InitiatorPlayer)
                {
                    if (config.BlockSettings.BlockOwnersIfNotInZone)
                    {
                        PlayerBlockStatus.Get(OwnerPlayer)?.BlockPlayer(sphere, false);
                        if (OwnerPlayer != info?.InitiatorPlayer) OwnerPlayer.ChatMessage(string.Format(Messages["blockactive"], GetGridString(entity.transform.position), NumericalFormatter.FormatTime(config.BlockSettings.BlockLength)));
                    }
                    else
                        OwnerPlayer.ChatMessage(string.Format(Messages["blockactiveOwner"], GetGridString(entity.transform.position)));
                }
                var col = Vis.colBuffer;
                var count = Physics.OverlapSphereNonAlloc(sphere.transform.position, config.BlockSettings.BlockerDistance, col, LayerMask.GetMask("Player (Server)"));
                for (int i = 0; i < count; i++)
                {
                    var player = col[i].ToBaseEntity() as BasePlayer;
                    if (player == null || !player.IsConnected) continue;
                    PlayerBlockStatus.Get(player).BlockPlayer(sphere, false);
                }

                if (entity.GetBuildingPrivilege() != null && config.BlockSettings.EnabledBlockAutCupboard)
                {
                    foreach (var aplayer in entity.GetBuildingPrivilege().authorizedPlayers)
                    {
                        var AuthPlayer = BasePlayer.Find(aplayer.userid.ToString());
                        if (AuthPlayer != null && AuthPlayer != info.InitiatorPlayer)
                            PlayerBlockStatus.Get(AuthPlayer).BlockPlayer(sphere, false);
                        else SendOfflineMessages(entity.transform.position, info.InitiatorPlayer.displayName, aplayer.userid);
                    }
                }
            }
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            var player = planner.GetOwnerPlayer();
            if (player == null || !IsRaidBlocked(player)) return null;
            var shortname = prefab.hierachyName.Substring(prefab.hierachyName.IndexOf("/") + 1);
            if (config.PlayerBlockSettings.WriteListBuildEntity.Contains(shortname))
                return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null || component.CurrentBlocker == null) return null;
            player.ChatMessage(string.Format(Messages["blockbuld"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return false;
        }

        private object OnUserCommand(IPlayer ipl, string command, string[] args)
        {
            if (ipl == null || !ipl.IsConnected) return null;
            var player = ipl.Object as BasePlayer;
            command = command.Insert(0, "/");
            if (player == null || !IsRaidBlocked(player)) return null;
            if (config.PlayerBlockSettings.BlackListCommands.Contains(command.ToLower()))
            {
                var component = PlayerBlockStatus.Get(player);
                if (component == null || component.CurrentBlocker == null) return null;
                player.ChatMessage(string.Format(Messages["commandBlock"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
                return false;
            }
            return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            var connection = arg.Connection;
            if (connection == null || string.IsNullOrEmpty(arg.cmd?.FullName)) return null;
            var player = arg.Player();
            if (player == null || !IsRaidBlocked(player)) return null;
            if (config.PlayerBlockSettings.BlackListCommands.Contains(arg.cmd.Name.ToLower()) || config.PlayerBlockSettings.BlackListCommands.Contains(arg.cmd.FullName.ToLower()))
            {
                var component = PlayerBlockStatus.Get(player);
                if (component == null || component.CurrentBlocker == null) return null;
                player.ChatMessage(string.Format(Messages["commandBlock"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
                return false;
            }
            return null;
        }

        #endregion

        #region Functions
        private string GetGridString(Vector3 pos)
        {
            char letter = 'A';
            var x = Mathf.Floor((pos.x + (ConVar.Server.worldsize / 2)) / 146.3f) % 26;
            var z = (Mathf.Floor(ConVar.Server.worldsize / 146.3f) - 1) - Mathf.Floor((pos.z + (ConVar.Server.worldsize / 2)) / 146.3f);
            letter = (char)(((int)letter) + x);
            return $"{letter}{z}";
        }

        private string NumberToString(int number)
        {
            bool a = number > 26;
            Char c = (Char)(65 + (a ? number - 26 : number));
            return a ? "A" + c : c.ToString();
        }

        [PluginReference] private Plugin RaidNotice;
        private static void SendOfflineMessages(Vector3 pos, string name, ulong playerid = 294912)
        {
            if (!ins.permission.UserHasPermission(playerid.ToString(), config.VkBotMessages.VkPrivilage)) return;
            ins.RaidNotice?.Call("SendOfflineMessage", config.VkBotMessages.Messages.Replace("{0}", name).Replace("{1}", ins.GetGridString(pos)), playerid);
        }

        private string GetFormatTime(TimeSpan timespan)
        {
            return string.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, System.Math.Floor(timespan.TotalHours));
        }

        private static class NumericalFormatter
        {
            private static string GetNumEndings(int origNum, string[] forms)
            {
                string result;
                var num = origNum % 100;
                if (num >= 11 && num <= 19)
                {
                    result = forms[2];
                }
                else
                {
                    num = num % 10;
                    switch (num)
                    {
                        case 1: result = forms[0]; break;
                        case 2:
                        case 3:
                        case 4:
                            result = forms[1]; break;
                        default:
                            result = forms[2]; break;
                    }
                }
                return string.Format("{0} {1} ", origNum, result);
            }

            private static string FormatSeconds(int seconds) =>
                GetNumEndings(seconds, new[] { "секунду", "секунды", "секунд" });
            private static string FormatMinutes(int minutes) =>
                GetNumEndings(minutes, new[] { "минуту", "минуты", "минут" });
            private static string FormatHours(int hours) =>
                GetNumEndings(hours, new[] { "час", "часа", "часов" });
            private static string FormatDays(int days) =>
                GetNumEndings(days, new[] { "день", "дня", "дней" });
            private static string FormatTime(TimeSpan timeSpan)
            {
                string result = string.Empty;
                if (timeSpan.Days > 0)
                    result += FormatDays(timeSpan.Days);
                if (timeSpan.Hours > 0)
                    result += FormatHours(timeSpan.Hours);
                if (timeSpan.Minutes > 0)
                    result += FormatMinutes(timeSpan.Minutes);
                if (timeSpan.Seconds > 0)
                    result += FormatSeconds(timeSpan.Seconds).TrimEnd(' ');
                return result;
            }

            public static string FormatTime(int seconds) => FormatTime(new TimeSpan(0, 0, seconds));
            public static string FormatTime(float seconds) => FormatTime((int)Math.Round(seconds));
            public static string FormatTime(double seconds) => FormatTime((int)Math.Round(seconds));
        }
        #endregion

        #region API

        private bool IsBlocked(BasePlayer player) => IsRaidBlocked(player);

        private List<Vector3> ApiGetOwnerRaidZones(ulong playerid)
        {
            var OwnerList = BlockerList.Where(p => p.OwnerID == playerid || p.Privilage != null && p.Privilage.Contains(playerid)).Select(p => p.transform.position).ToList();
            return OwnerList;
        }

        private List<Vector3> ApiGetAllRaidZones()
          => BlockerList.Select(p => p.transform.position).ToList();

        private bool IsRaidBlock(ulong userId) => IsRaidBlocked(userId.ToString());

        private bool IsRaidBlocked(BasePlayer player) => PlayerBlockStatus.Get(player)?.CurrentBlocker != null;

        private bool IsRaidBlocked(string player)
        {
            BasePlayer target = BasePlayer.Find(player);
            if (target == null) return false;

            return IsRaidBlocked(target);
        }

        private int ApiGetTime(ulong userId)
        {
            if (!IsRaidBlocked(userId.ToString())) return 0;
            var targetBlock = PlayerBlockStatus.Get(BasePlayer.Find(userId.ToString()));
            return (int)(targetBlock.CurrentBlocker.TotalTime - targetBlock.CurrentTime);
        }

        private string CanTeleport(BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanTeleport) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blocktp"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        private int? CanBGrade(BasePlayer player, int grade, BuildingBlock block, Planner plan)
        {
            if (!config.PlayerBlockSettings.CanBGrade) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            player.ChatMessage(string.Format(Messages["blockupgrade"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return 0;
        }

        private string CanTrade(BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanUseTrade) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blocktrade"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        private string canRemove(BasePlayer player)
        {

            if (!config.PlayerBlockSettings.CanRemove) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blockremove"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        private string canTeleport(BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanTeleport) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blocktp"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        object canRedeemKit(BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanUseKits) return null;

            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            return string.Format(Messages["blockKits"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime));
        }

        private bool? CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            player.ChatMessage(string.Format(Messages["blockupgrade"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return false;
        }

        private bool? OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanRepair) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            player.ChatMessage(string.Format(Messages["blockrepair"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return false;
        }

        object OnStructureDemolish(BaseCombatEntity entity, BasePlayer player)
        {
            if (!config.PlayerBlockSettings.CanDefaultremove) return null;
            if (player == null) return null;
            if (!IsRaidBlocked(player)) return null;
            var component = PlayerBlockStatus.Get(player);
            if (component == null) return null;
            player.ChatMessage(string.Format(Messages["blockremove"], NumericalFormatter.FormatTime(component.CurrentBlocker.TotalTime - component.CurrentTime)));
            return null;
        }

        private SphereComponent GetRaidZone(Vector3 pos) =>
             BlockerList.Where(p => Vector3.Distance(p.transform.position, pos) < config.BlockSettings.BlockerDistance).FirstOrDefault();

        #endregion

        #region Messages⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

        Dictionary<string, string> Messages = new Dictionary<string, string>() {
                {
                "blocksuccess", "Блок деактивирован. Функции разблокированы"
            }
            , {
                "guitimertext", "<b>Блокировка:</b> Осталось {0}"
            }
            , {
                "blockactive", "Ваше строение в квадрате <color=#ECBE13>{0}</color> разрушено, активирован рейд блок на <color=#ECBE13>{1}</color>\nНекоторые функции временно недоступны."
            }
             , {
                "blockactiveOwner", "Внимание! Ваше строение в квадрате <color=#ECBE13>{0}</color> разрушено."
            }
             , {
                "enterRaidZone", "Внимание! Вы вошли в зону рейд блока, активирован блок на <color=#ECBE13>{0}</color>\nНекоторые функции временно недоступны."
            }
             , {
                "blockactiveAuthCup", "Внимание! Строение в каком вы проживаете в квадрате <color=#ECBE13>{0}</color> было разрушено, активирован рейд блок на <color=#ECBE13>{1}</color>\nНекоторые функции временно недоступны."
            }
            , {
                "blockactiveAttacker", "Вы уничтожили чужой объект, активирован рейд блок на <color=#ECBE13>{0}</color>\nНекоторые функции временно недоступны."
            }
            , {
                "blockrepair", "Вы не можете ремонтировать строения во время рейда, подождите {0}"
            }
            , {
                "blocktp", "Вы не можете использовать телепорт во время рейда, подождите {0}"
            }
            , {
                "blockremove", "Вы не можете удалить постройки во время рейда, подождите {0}"
            }
            , {
                "blockupgrade", "Вы не можете использовать улучшение построек во время рейда, подождите {0}"
            }
            , {
                "blockKits", "Вы не можете использовать киты во время рейда, подождите {0}"
            }
            , {
                "blockbuld", "Вы не можете строить во время рейда, подождите {0}"
            },
            {
                "raidremove", "Вы не можете удалять обьекты во время рейда, подождите {0}"
            },
            {
                "blocktrade", "Вы не можете использовать обмен во время рейда, подождите {0} "
            },
            {
                "commandBlock", "Вы не можете использовать данную команду во время рейда, подождите {0}"
            },
        };
        #endregion
    }
}