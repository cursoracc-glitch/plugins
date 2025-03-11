using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using System.Collections;
using Oxide.Core.Plugins;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("NoEscape", "https://topplugin.ru/", "3.0.0")]
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
                        new CuiRectTransformComponent {AnchorMin = $"0 0", AnchorMax = $"{(float) (CurrentBlocker.TotalTime - CurrentTime) / CurrentBlocker.TotalTime} 1", OffsetMin = "0 0", OffsetMax = "0 0"},
                    }
                });
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { Text = string.Format(ins.Messages["guitimertext"], ins.GetFormatTime(TimeSpan.FromSeconds(CurrentBlocker.TotalTime - CurrentTime))), Font = "robotocondensed-regular.ttf", Color = "1 1 1 0.9", FontSize = 16, Align = TextAnchor.MiddleCenter }
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
                var reply = 4018;
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
            PrintWarning("Благодарим за покупку плагина на сайте TopPlugin.ru. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
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
            //permission.RegisterPermission(config.VkBotMessages.VkPrivilage, this);
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
			LoadVKData();
        }

        public void LoadVKData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Vk/Data"))
            {
                baza = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>("Vk/Data");
            }
            else
            {
                PrintWarning($"Error reading config, creating one new data!");
                baza = new Dictionary<ulong, string>();
            }

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Vk/Names"))
            {
                _PlayerNicknames = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>("Vk/Names");
            }
            else
                _PlayerNicknames = new Dictionary<ulong, string>();

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
        public class SenderConfig
        {
            [JsonProperty("Настройки отправки сообщений в VK")]
            public VkSettings VK = new VkSettings();
            [JsonProperty("Сообщение какое будет отправлено игроку ({0} - Имя атакуещего, {1} - Квадрат на карте)")]
            public string Message = "Внимание! Игрок {0} начал рейд вашего строения в квадрате {1} на сервере SERVERNAME.";
        }

        public class VkSettings
        {
            [JsonProperty("Включить отправку сообщения в ВК оффлайн игроку")]
            public bool EnabledVk = false;
            [JsonProperty("Access токен группы ВК с правом отправки сообщений")]
            public string VKAccess = "Вставьте сюда токен для отправки сообщений в вк";
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

            [JsonProperty("Настройка отправки сообщений")]
            public SenderConfig Sender = new SenderConfig();

            [JsonProperty("Версия конфигурации")]
            public VersionNumber PluginVersion = new VersionNumber();

            [JsonIgnore]
            [JsonProperty("Инициализация плагина⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠")]
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
                    Sender = new SenderConfig()
                    {
						VK = new VkSettings(){
							EnabledVk = false,
							VKAccess = "Вставьте сюда токен для отправки сообщений в вк"
						},
                        Message = "Внимание! Игрок {0} начал рейд вашего строения в квадрате {1} на сервере SERVERNAME."
                    },
                    PluginVersion = new VersionNumber(),
                };
            }
        }

        #endregion

        #region Oxide
        private static NoEscape ins;
        [PluginReference] Plugin ImageLibrary;
        private void OnServerInitialized()
        {
            ins = this;
            config.Init = true;
            ImageLibrary.Call("AddImage", "https://imgur.com/4jcrNyN.png", "4jcrNyN");
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            SphereComponent ActiveRaidZone = GetRaidZone(player.transform.position);
            if (ActiveRaidZone == null) return;
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
            foreach (var player in BasePlayer.activePlayerList){
                if (PlayerBlockStatus.Get(player) != null)
                    UnityEngine.Object.Destroy(PlayerBlockStatus.Get(player));
			}
            BlockerList.RemoveAll(x =>
            {
                UnityEngine.Object.Destroy(x);
                return true;
            });
			Interface.Oxide.DataFileSystem.WriteObject("Vk/Data", baza);
            Interface.Oxide.DataFileSystem.WriteObject("Vk/Names", _PlayerNicknames);
        }

        string GetGridLocation(Vector3 pos)
        {
            string gridLocation = "";
            int numx = Convert.ToInt32(pos.x);
            int numz = Convert.ToInt32(pos.z);

            float offset = (ConVar.Server.worldsize) / 2;
            float step = (ConVar.Server.worldsize) / (0.0066666666666667f * (ConVar.Server.worldsize));
            string start = "";

            int diff = Convert.ToInt32(step);
            int absoluteDifference = diff;

            char letter = 'A';
            int number = 0;
            for (float xx = -offset; xx < offset; xx += step)
            {
                for (float zz = offset; zz > -offset; zz -= step)
                {
                    if (Math.Abs(numx - xx) <= diff && Math.Abs(numz - zz) <= diff)
                    {
                        gridLocation = $"{start}{letter}{number}";
                        break;
                    }
                    number++;
                }
                number = 0;
                if (letter.ToString().ToUpper() == "Z")
                {
                    start = "A";
                    letter = 'A';
                }
                else
                {
                    letter = (char)(((int)letter) + 1);
                }
                if (Math.Abs(numx - xx) <= diff)
                {
                    break;
                }
            }
            return gridLocation;
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!config.Init) return;
            if (entity == null || info == null || info.InitiatorPlayer == null || !(entity is StabilityEntity || entity is ShopFront || entity is BuildingPrivlidge)
                || config.BlockSettings.EnabledBuildingBlock && entity.GetBuildingPrivilege() == null || entity.OwnerID == 0) return;
            if (entity is BuildingBlock && (entity as BuildingBlock).currentGrade.gradeBase.type == BuildingGrade.Enum.Twigs
                || info?.damageTypes.GetMajorityDamageType() == DamageType.Decay || config.BlockSettings.WriteListDestroyEntity.Contains(entity.ShortPrefabName)) return;
            var alreadyBlock = BlockerList.FirstOrDefault(p => Vector3.Distance(entity.transform.position, p.transform.position) < (config.BlockSettings.BlockerDistance / 2));
            var position = GetGridLocation(entity.transform.position);
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
                        else if (AuthPlayer == null || !AuthPlayer.IsConnected) ALERTPLAYER(aplayer.userid, info.InitiatorPlayer.displayName, position);
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
                    ALERTPLAYER(entity.OwnerID, info.InitiatorPlayer.displayName, position);
                    return;
                }
                else if (OwnerPlayer != null && OwnerPlayer != info.InitiatorPlayer)
                {
                    if (config.BlockSettings.BlockOwnersIfNotInZone)
                    {
                        PlayerBlockStatus.Get(OwnerPlayer)?.BlockPlayer(sphere, false);
                        if (OwnerPlayer != info?.InitiatorPlayer) OwnerPlayer.ChatMessage(string.Format(Messages["blockactive"], GetNameGrid(entity.transform.position), NumericalFormatter.FormatTime(config.BlockSettings.BlockLength)));
                    }
                    else
                        OwnerPlayer.ChatMessage(string.Format(Messages["blockactiveOwner"], GetNameGrid(entity.transform.position)));
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
                        else ALERTPLAYER(aplayer.userid, info.InitiatorPlayer.displayName, position);
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

        private bool IsRaidBlocked(BasePlayer player) {
			var targetBlock = PlayerBlockStatus.Get(player);
			if (targetBlock==null) return false;
			if (targetBlock.CurrentBlocker == null) return false;
			
			return true;
		}

        private bool IsRaidBlocked(string player)
        {
            BasePlayer target = BasePlayer.Find(player);
            if (target == null) return false;

            return IsRaidBlocked(target);
        }

        private int ApiGetTime(ulong userId)
        {
            if (!IsRaidBlocked(userId.ToString())) 
				return 0;
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

		#region VkAPI
			Dictionary<ulong, string> _PlayerNicknames = new Dictionary<ulong, string>();

			public Dictionary<ulong, string> baza;

        private void ALERTPLAYER(ulong ID, string name, string pos)
        {
            ALERT alert;
            if(!alerts.TryGetValue(ID, out alert))
            {
                alerts.Add(ID, new ALERT());
                alert = alerts[ID];
            }

            #region ОПОВЕЩЕНИЕ В ВК
            if (alert.vkcooldown < DateTime.Now)
            {
                string vkid;
                if (baza.TryGetValue(ID, out vkid))
                {
                    GetRequest(vkid, $"Внимание! Игрок {name} начал рейд вашего строения в квадрате {pos} на сервере SERVERNAME.");
                    alert.vkcooldown = DateTime.Now.AddSeconds(1200);
                }
            }
            #endregion
        }

        private static Dictionary<string, Vector3> Grids = new Dictionary<string, Vector3>();
        private void CreateSpawnGrid()
        {
            Grids.Clear();
            var worldSize = (ConVar.Server.worldsize);
            float offset = worldSize / 2;
            var gridWidth = (0.0066666666666667f * worldSize);
            float step = worldSize / gridWidth;

            string start = "";

            char letter = 'A';
            int number = 0;

            for (float zz = offset; zz > -offset; zz -= step)
            {
                for (float xx = -offset; xx < offset; xx += step)
                {
                    Grids.Add($"{start}{letter}{number}", new Vector3(xx - 55f, 0, zz + 20f));
                    if (letter.ToString().ToUpper() == "Z")
                    {
                        start = "A";
                        letter = 'A';
                    }
                    else
                    {
                        letter = (char)(((int)letter) + 1);
                    }


                }
                number++;
                start = "";
                letter = 'A';
            }
        }

        private string GetNameGrid(Vector3 pos)
        {
            return Grids.Where(x => x.Value.x < pos.x && x.Value.x + 150f > pos.x && x.Value.z > pos.z && x.Value.z - 150f < pos.z).FirstOrDefault().Key;
        }

			private static string HexToRustFormat(string hex)
			{
				if (string.IsNullOrEmpty(hex))
				{
					hex = "#FFFFFFFF";
				}
				var str = hex.Trim('#');
				if (str.Length == 6) str += "FF";
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
			private string GetOfflineName(ulong id)
			{
				string name = "";
				if (_PlayerNicknames.ContainsKey(id))
					name = _PlayerNicknames[id];

				return name;
			}
			bool IsOnline(ulong id)
			{
				foreach (BasePlayer active in BasePlayer.activePlayerList)
				{
					if (active.userID == id) return true;
				}

				return false;
			}
		#endregion

        #region Хуита
        class ALERT
        {
            public DateTime gamecooldown;
            public DateTime discordcooldown;
            public DateTime vkcooldown;
            public DateTime vkcodecooldown;
        }

        class CODE
        {
            public string id;
            public ulong gameid;
        }

        private static Dictionary<string, CODE> VKCODES = new Dictionary<string, CODE>();
        
        private static Dictionary<ulong, ALERT> alerts = new Dictionary<ulong, ALERT>();
        private string RANDOMNUM() => Random.Range(1000, 99999).ToString();

        [ChatCommand("vk")]
        void ChatVk(BasePlayer player)
        {
            string vkid;
            if (!baza.TryGetValue(player.userID, out vkid))
            {
                player.Command("vk add");
            }
            else
            {
                VkUI(player, "<color=#b0b0b0>ПОДТВЕРЖДЕНО</color>", "0.51 0.85 0.59 0.4", "", "Теперь вам будут приходить оповещение о рейде в ЛС\n<b>НЕ ЗАПРЕЩАЙТЕ СООБЩЕНИЕ ОТ СООБЩЕСТВА</b>"); 
            }
        }

        [ConsoleCommand("vk")]
        void ConsolePM(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                Puts(args.Args[0]);
                if (args.Args[0] == "add")
                {
                    if(args == null || args.Args.Length == 1)
                    {
                        string vkid;
                        if (!baza.TryGetValue(player.userID, out vkid))
                        {
                            VkUI(player, "Укажите свой вк", "0 0 0 0.6", "vk add ", "Чтобы подключить оповещение о рейде\nдобавте выше <b>id вашего аккаунта</b>"); 
                        }
                        else
                        {
                            VkUI(player, "<color=#b0b0b0>ПОДТВЕРЖДЕНО</color>", "0.51 0.85 0.59 0.4", "", "Теперь вам будут приходить оповещение о рейде в ЛС\n<b>НЕ ЗАПРЕЩАЙТЕ СООБЩЕНИЕ ОТ СООБЩЕСТВА</b>"); 
                        }
                        return;
                    }
                    ALERT aLERT;
                    if (alerts.TryGetValue(player.userID, out aLERT) && aLERT.vkcodecooldown > DateTime.Now)
                    {
                        player.ChatMessage($"Отправить новый код вы сможете через {FormatTime(aLERT.vkcodecooldown - DateTime.Now).ToLower()}");
                        return;
                    }

                    string id = args.Args[1].ToLower().Replace("vk.com/", "").Replace("https://", "").Replace("http://", "");
                    string num = RANDOMNUM();
                    GetRequest(id, $"Код подтверждения {num} аккаунта.", player, num);
                }
                if (args.Args[0] == "accept")
                {
                    if(args == null || args.Args.Length == 1)
                    {
                        VkUI(player, "Укажите код из сообщения", "0 0 0 0.6", "vk accept ", "Вы не указали <b>код</b>!"); 
                        return;
                    }

                    CODE cODE;
                    if (VKCODES.TryGetValue(args.Args[1], out cODE) && cODE.gameid == player.userID)
                    {
                        string vkid;
                        if(baza.TryGetValue(player.userID, out vkid))
                        {
                            vkid = cODE.id;
                        }
                        else
                        {
                            baza.Add(player.userID, cODE.id);
                        }
                        VKCODES.Remove(args.Args[1]);
                        VkUI(player, "<color=#b0b0b0>ПОДТВЕРЖДЕНО</color>", "0.51 0.85 0.59 0.4", "", "Теперь вам будут приходить оповещение о рейде в ЛС\n<b>НЕ ЗАПРЕЩАЙТЕ СООБЩЕНИЕ ОТ СООБЩЕСТВА</b>"); 
                        Interface.Oxide.DataFileSystem.WriteObject("Vk/Data", baza);
                    }
                    else
                    {
                        VkUI(player, "Укажите код из сообщения", "0 0 0 0.6", "vk accept ", "Не верный <b>код</b>!"); 
                    }
                }
                if (args.Args[0] == "delete")
                {
                    if (baza.ContainsKey(player.userID))
                    {
                        baza.Remove(player.userID);
                        VkUI(player, "Укажите свой вк", "0 0 0 0.6", "vk add ", "Чтобы подключить оповещение о рейде\nдобавте выше <b>id вашего аккаунта</b>"); 
                    }
                }
            }
        }

        private void GetRequest(string reciverID, string msg, BasePlayer player = null, string num = null) => webrequest.Enqueue("https://api.vk.com/method/messages.send?domain=" + reciverID + "&message=" + msg.Replace("#", "%23") + "&v=5.86&access_token=" + config.Sender.VK.VKAccess, null, (code2, response2) => ServerMgr.Instance.StartCoroutine(GetCallback(code2, response2, reciverID, player, num)), this);
        
        private IEnumerator GetCallback(int code, string response, string id, BasePlayer player = null, string num = null)
        {
            if (player == null) yield break;
            if (response == null || code != 200)
            {
                ALERT alert;
                if (alerts.TryGetValue(player.userID, out alert)) alert.vkcooldown = DateTime.Now;
                Debug.Log("НЕ ПОЛУЧИЛОСЬ ОТПРАВИТЬ СООБЩЕНИЕ В ВК! => обнулили кд на отправку");
                yield break;
            }
            yield return new WaitForEndOfFrame();
            if (!response.Contains("error"))
            {
                ALERT aLERT;
                if (alerts.TryGetValue(player.userID, out aLERT))
                {
                    aLERT.vkcodecooldown = DateTime.Now.AddMinutes(10);
                }
                else
                {
                    alerts.Add(player.userID, new ALERT {vkcodecooldown = DateTime.Now.AddMinutes(10) });
                }
                if (VKCODES.ContainsKey(num)) VKCODES.Remove(num);
                VKCODES.Add(num, new CODE { gameid = player.userID, id = id });
                VkUI(player, "Укажите код из сообщения", "0 0 0 0.6", "vk accept ", $"Вы указали VK: <b>{id}</b>. Вам в <b>VK</b> отправлено сообщение с кодом.\nВставте <b>код</b> выше, чтобы подтвердить авторизацию");
            }
            else if (response.Contains("PrivateMessage"))
            {
                VkUI(player, "Укажите свой вк", "0 0 0 0.6", "vk add ", $"Ваши настройки приватности не позволяют отправить вам\nсообщение <b>{id}</b>");
            }
            else if(response.Contains("ErrorSend"))
            {
                VkUI(player, "Укажите свой вк", "0 0 0 0.6", "vk add ", $"Невозможно отправить сообщение.Проверьте правильность ссылки <b>{id}</b>\nили повторите попытку позже.");
            }
            else if(response.Contains("BlackList"))
            {
                VkUI(player, "Укажите свой вк", "0 0 0 0.6", "vk add ", "Невозможно отправить сообщение. Вы добавили группу в черный список или не подписаны на нее, если это не так,\nто просто напишите в группу сервера любое сообщение и попробуйте еще раз.");
            }
            else
            {
                VkUI(player, "Укажите свой вк", "0 0 0 0.6", "vk add ", $"Вы указали неверный <b>VK ID {id}</b>, если это не так,\nто просто напишите в группу сервера любое сообщение и попробуйте еще раз."); 
            }
            yield break;
        }

        string Layers = "Vk_UI";

        void VkUI(BasePlayer player, string vk = "", string color = "", string command = "", string text = "")
        {
            CuiHelper.DestroyUi(player, Layers);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.288 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Info", Layers);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0 0.8", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.2" },
            }, Layers);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1 0.8", AnchorMax = $"0.9 1", OffsetMax = "0 0" },
                Text = { Text = "<b><size=40>ОПОВЕЩЕНИЕ</size></b>\nУслуга оповещения о рейде предоставляется бесплатно для наших игроков. Подключив оповещения вы будете автоматически проинформированы в соц-сетях когда один из строительных объектов будет сломан.", Color = "1 1 1 0.6", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layers);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.05 0.45", AnchorMax = $"0.95 0.77", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.2" },
            }, Layers, "Vk");

            container.Add(new CuiElement
            {
                Parent = "Vk",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "4jcrNyN") },
                    new CuiRectTransformComponent { AnchorMin = "0.45 0.7", AnchorMax = "0.55 0.9", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3 0.63", AnchorMax = $"0.7 0.69", OffsetMax = "0 0" },
                Text = { Text = "ЭТО ВАШ АККАУНТ", Color = "1 1 1 0.3", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, Layers);

            var anchorMax = command != "" ? "0.93 0.6" : "0.86 0.6";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.07 0.55", AnchorMax = anchorMax, OffsetMax = "0 0" },
                Image = { Color = color }
            }, Layers, "Enter");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.04 0", AnchorMax = $"0.96 1", OffsetMax = "0 0" },
                Text = { Text = vk, Color = "1 1 1 0.05", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Enter");

            if (command != "")
            {
                container.Add(new CuiElement
                {
                    Parent = "Enter",
                    Components =
                    {
                        new CuiInputFieldComponent { Text = "ХУЙ", FontSize = 14, Align = TextAnchor.MiddleCenter, Command = command, Color = "1 1 1 0.6", CharsLimit = 40},
                        new CuiRectTransformComponent { AnchorMin = "0.04 0", AnchorMax = "0.96 1" }
                    }
                });
            }

            if (command == "")
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.86 0.55", AnchorMax = $"0.93 0.6", OffsetMax = "0 0" },
                    Button = { Color = "0.76 0.35 0.35 0.4", Command = "vk delete" },
                    Text = { Text = "✖", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-bold.ttf" }
                }, Layers);
            }

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = $"1 0.55", OffsetMax = "0 0" },
                Text = { Text = text, Color = "1 1 1 0.3", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, Layers);

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region ВРЕМЯ
        private static string m0 = "МИНУТ";
        private static string m1 = "МИНУТЫ";
        private static string m2 = "МИНУТУ";

        private static string s0 = "СЕКУНД";
        private static string s1 = "СЕКУНДЫ";
        private static string s2 = "СЕКУНДУ";

        private static string FormatTime(TimeSpan time)
        => (time.Minutes == 0 ? string.Empty : FormatMinutes(time.Minutes)) + ((time.Seconds == 0) ? string.Empty : FormatSeconds(time.Seconds));

        private static string FormatMinutes(int minutes) => FormatUnits(minutes, m0, m1, m2);

        private static string FormatSeconds(int seconds) => FormatUnits(seconds, s0, s1, s2);

        private static string FormatUnits(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9 || tmp == 0)
                return $"{units} {form1} ";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2} ";

            return $"{units} {form3} ";
        }
        #endregion



        #region Messages

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
            {"VkExit", "У вас уже есть страница!" },
            {"VkVremExit", "У вас уже есть активный запрос на подтверждение!"},
            {"VkCodeError", "Неправильный код !"},
            {"VkSendError", "Ошибка при отправке проверочного кода" },
            {"VkSendError2", "Ошибка при отправке проверочного кода\nОтправьте сообщение в группу и попробуй еще раз" },
            {"VkCodeSend", "Код подтверждения отправлен!" },
            {"VkAdded", "Страница успешно добавлена" }
        };
        #endregion
    }
}