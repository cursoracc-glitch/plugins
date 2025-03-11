using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("N1KTO COMPANY - Cobalt Laboratory", "RustPlugin", "1.0.0")]
    [Description("Автоматическое событие с ботами и кастомным зданием от N1KTO COMPANY")]
    public class CobaltLaboratory : RustPlugin
    {
        #region Fields
        private Configuration config;
        private const string PermissionUse = "cobaltlaboratory.use";
        private const string PermissionAdmin = "cobaltlaboratory.admin";
        private Timer eventTimer;
        private Timer eventDurationTimer;
        private Timer radiationTimer;
        private Timer autoStartCheckTimer;
        private Vector3 eventPosition;
        private bool isEventActive;
        private List<BasePlayer> activeNPCs = new List<BasePlayer>();
        private List<BaseEntity> spawnedEntities = new List<BaseEntity>();
        private StorageContainer lootBox;
        private BaseEntity radiationEntity;
        private float eventEndTime;
        private string mapMarkerID;
        #endregion

        #region Configuration
        class Configuration
        {
            [JsonProperty("Setting up and stopping an event")]
            public EventSettings Event { get; set; }

            [JsonProperty("Configuring notifications")]
            public NotificationSettings Notifications { get; set; }

            [JsonProperty("Setting up radiation in the event area")]
            public RadiationSettings Radiation { get; set; }

            [JsonProperty("Event display on maps")]
            public MapSettings Map { get; set; }

            [JsonProperty("Bot settings")]
            public BotSettings Bots { get; set; }

            [JsonProperty("UI settings")]
            public UISettings UI { get; set; }

            [JsonProperty("Loot settings")]
            public LootSettings Loot { get; set; }

            [JsonProperty("Command settings")]
            public CommandSettings Commands { get; set; }

            public Configuration()
            {
                Event = new EventSettings();
                Notifications = new NotificationSettings();
                Radiation = new RadiationSettings();
                Map = new MapSettings();
                Bots = new BotSettings();
                UI = new UISettings();
                Loot = new LootSettings();
                Commands = new CommandSettings();
            }
        }

        class EventSettings
        {
            [JsonProperty("The minimum number of players to start an event")]
            public int MinPlayers { get; set; }

            [JsonProperty("Time before the start of the event (Minimum in seconds)")]
            public int MinStartTime { get; set; }

            [JsonProperty("Time before the start of the event (Maximum in seconds)")]
            public int MaxStartTime { get; set; }

            [JsonProperty("Enable auto-start at specific time")]
            public bool EnableAutoStart { get; set; }

            [JsonProperty("Auto-start time in seconds")]
            public int AutoStartTime { get; set; }
        }

        class NotificationSettings
        {
            [JsonProperty("Discord WebHook")]
            public string DiscordWebHook { get; set; }

            [JsonProperty("Enable UI Notifications?")]
            public bool EnableUINotifications { get; set; }

            [JsonProperty("Auto hide UI notifications?")]
            public bool AutoHideUI { get; set; }

            [JsonProperty("How long after the show will it hide? (sec)")]
            public float HideDelay { get; set; }
        }

        class RadiationSettings
        {
            [JsonProperty("Turn on radiation?")]
            public bool EnableRadiation { get; set; }

            [JsonProperty("Number of radiation particles")]
            public int RadiationParticles { get; set; }
        }

        class MapSettings
        {
            [JsonProperty("Mark the event on the G card")]
            public bool ShowOnMap { get; set; }

            [JsonProperty("Text for map G")]
            public string MapText { get; set; }
        }

        class BotSettings
        {
            [JsonProperty("Bot types")]
            public List<BotType> Types { get; set; } = new List<BotType>();

            [JsonProperty("Enable night vision")]
            public bool EnableNightVision { get; set; }

            [JsonProperty("Enable flashlights at night")]
            public bool EnableFlashlights { get; set; }

            [JsonProperty("Bot behavior settings")]
            public BotBehavior Behavior { get; set; }

            public BotSettings()
            {
                Types = new List<BotType>
                {
                    new BotType
                    {
                        Name = "Штурмовик",
                        Health = 150,
                        Accuracy = 0.7f,
                        RoamRange = 30,
                        ChaseRange = 50,
                        Equipment = new Equipment
                        {
                            Weapons = new List<string> { "rifle.ak", "pistol.python" },
                            Armor = new List<string> { "metal.facemask", "metal.plate.torso" }
                        }
                    },
                    new BotType
                    {
                        Name = "Снайпер",
                        Health = 100,
                        Accuracy = 0.9f,
                        RoamRange = 50,
                        ChaseRange = 100,
                        Equipment = new Equipment
                        {
                            Weapons = new List<string> { "rifle.bolt", "pistol.revolver" },
                            Armor = new List<string> { "metal.facemask", "roadsign.jacket" }
                        }
                    },
                    new BotType
                    {
                        Name = "Медик",
                        Health = 120,
                        Accuracy = 0.6f,
                        RoamRange = 20,
                        ChaseRange = 30,
                        Equipment = new Equipment
                        {
                            Weapons = new List<string> { "smg.mp5", "pistol.semiauto" },
                            Armor = new List<string> { "coffeecan.helmet", "roadsign.jacket" },
                            Items = new List<string> { "syringe.medical", "bandage" }
                        }
                    }
                };
                Behavior = new BotBehavior();
            }
        }

        class BotType
        {
            [JsonProperty("Bot name")]
            public string Name { get; set; }

            [JsonProperty("Health")]
            public float Health { get; set; }

            [JsonProperty("Accuracy (0.0-1.0)")]
            public float Accuracy { get; set; }

            [JsonProperty("Roam range")]
            public float RoamRange { get; set; }

            [JsonProperty("Chase range")]
            public float ChaseRange { get; set; }

            [JsonProperty("Equipment")]
            public Equipment Equipment { get; set; } = new Equipment();
        }

        class Equipment
        {
            [JsonProperty("Weapons")]
            public List<string> Weapons { get; set; } = new List<string>();

            [JsonProperty("Armor")]
            public List<string> Armor { get; set; } = new List<string>();

            [JsonProperty("Items")]
            public List<string> Items { get; set; } = new List<string>();
        }

        class BotBehavior
        {
            [JsonProperty("Use cover")]
            public bool UseCover { get; set; } = true;

            [JsonProperty("Help wounded allies")]
            public bool HelpAllies { get; set; } = true;

            [JsonProperty("Retreat when low health")]
            public bool RetreatWhenLowHealth { get; set; } = true;

            [JsonProperty("Low health threshold")]
            public float LowHealthThreshold { get; set; } = 0.3f;
        }

        class UISettings
        {
            [JsonProperty("Event timer color")]
            public string TimerColor { get; set; } = "1 1 1 1";

            [JsonProperty("Event notification color")]
            public string NotificationColor { get; set; } = "0.7 0.3 0.3 1";

            [JsonProperty("Show event timer")]
            public bool ShowEventTimer { get; set; } = true;

            [JsonProperty("Show kill feed")]
            public bool ShowKillFeed { get; set; } = true;

            [JsonProperty("Show minimap")]
            public bool ShowMinimap { get; set; } = true;
        }

        class LootSettings
        {
            [JsonProperty("Настройки ящиков")]
            public BoxSettings BoxSettings { get; set; } = new BoxSettings();

            [JsonProperty("Категории лута")]
            public List<LootCategory> Categories { get; set; }

            [JsonProperty("Минимум предметов в ящике")]
            public int MinItemsPerBox { get; set; } = 6;

            [JsonProperty("Максимум предметов в ящике")]
            public int MaxItemsPerBox { get; set; } = 12;

            public LootSettings()
            {
                BoxSettings = new BoxSettings();
                Categories = new List<LootCategory>
                {
                    new LootCategory
                    {
                        Name = "Оружие",
                        Weight = 30,
                        Items = new List<LootItem>
                        {
                            new LootItem { ShortName = "rifle.ak", MinAmount = 1, MaxAmount = 1, Chance = 20, Blueprint = true },
                            new LootItem { ShortName = "rifle.bolt", MinAmount = 1, MaxAmount = 1, Chance = 15, Blueprint = true },
                            new LootItem { ShortName = "rifle.l96", MinAmount = 1, MaxAmount = 1, Chance = 10, Blueprint = true },
                            new LootItem { ShortName = "rifle.lr300", MinAmount = 1, MaxAmount = 1, Chance = 20 },
                            new LootItem { ShortName = "smg.mp5", MinAmount = 1, MaxAmount = 1, Chance = 25 },
                            new LootItem { ShortName = "lmg.m249", MinAmount = 1, MaxAmount = 1, Chance = 5 },
                            new LootItem { ShortName = "rocket.launcher", MinAmount = 1, MaxAmount = 1, Chance = 8, Blueprint = true }
                        }
                    },
                    new LootCategory
                    {
                        Name = "Взрывчатка",
                        Weight = 20,
                        Items = new List<LootItem>
                        {
                            new LootItem { ShortName = "explosive.timed", MinAmount = 1, MaxAmount = 10, Chance = 15 },
                            new LootItem { ShortName = "ammo.rocket.basic", MinAmount = 5, MaxAmount = 15, Chance = 20 },
                            new LootItem { ShortName = "ammo.rocket.hv", MinAmount = 5, MaxAmount = 15, Chance = 15 },
                            new LootItem { ShortName = "ammo.rocket.fire", MinAmount = 5, MaxAmount = 15, Chance = 10 },
                            new LootItem { ShortName = "rocket.launcher", MinAmount = 1, MaxAmount = 1, Chance = 8, Blueprint = true }
                        }
                    },
                    new LootCategory
                    {
                        Name = "Боеприпасы",
                        Weight = 50,
                        Items = new List<LootItem>
                        {
                            new LootItem { ShortName = "ammo.rifle", MinAmount = 120, MaxAmount = 240, Chance = 50 },
                            new LootItem { ShortName = "ammo.rifle.hv", MinAmount = 60, MaxAmount = 120, Chance = 30 },
                            new LootItem { ShortName = "ammo.rifle.explosive", MinAmount = 20, MaxAmount = 40, Chance = 15 },
                            new LootItem { ShortName = "ammo.rocket.basic", MinAmount = 2, MaxAmount = 4, Chance = 10 },
                            new LootItem { ShortName = "ammo.rocket.hv", MinAmount = 2, MaxAmount = 4, Chance = 8 }
                        }
                    },
                    new LootCategory
                    {
                        Name = "Компоненты",
                        Weight = 40,
                        Items = new List<LootItem>
                        {
                            new LootItem { ShortName = "explosives", MinAmount = 20, MaxAmount = 50, Chance = 30 },
                            new LootItem { ShortName = "targeting.computer", MinAmount = 1, MaxAmount = 2, Chance = 20 },
                            new LootItem { ShortName = "rifle.body", MinAmount = 1, MaxAmount = 3, Chance = 40 },
                            new LootItem { ShortName = "tech.trash", MinAmount = 5, MaxAmount = 10, Chance = 50 }
                        }
                    },
                    new LootCategory
                    {
                        Name = "Ресурсы",
                        Weight = 70,
                        Items = new List<LootItem>
                        {
                            new LootItem { ShortName = "sulfur", MinAmount = 2000, MaxAmount = 4000, Chance = 60 },
                            new LootItem { ShortName = "metal.refined", MinAmount = 200, MaxAmount = 500, Chance = 40 },
                            new LootItem { ShortName = "gunpowder", MinAmount = 1000, MaxAmount = 2000, Chance = 50 },
                            new LootItem { ShortName = "metal.fragments", MinAmount = 2000, MaxAmount = 4000, Chance = 60 },
                            new LootItem { ShortName = "charcoal", MinAmount = 2000, MaxAmount = 4000, Chance = 70 }
                        }
                    }
                };
            }
        }

        class BoxSettings
        {
            [JsonProperty("Тип ящика (large.wooden/elite/military/...)")]
            public string BoxType { get; set; } = "box.wooden.large";

            [JsonProperty("Высота спавна ящика над землей")]
            public float BoxHeight { get; set; } = 1.0f;

            [JsonProperty("Можно ли подбирать ящик")]
            public bool IsPickupable { get; set; } = false;

            [JsonProperty("Время жизни ящика (в минутах, 0 = бесконечно)")]
            public float BoxLifetime { get; set; } = 0f;

            [JsonProperty("Защита ящика (0-1000)")]
            public float BoxHealth { get; set; } = 500f;

            [JsonProperty("Создавать несколько ящиков")]
            public bool EnableMultipleBoxes { get; set; } = false;

            [JsonProperty("Минимум ящиков")]
            public int MinBoxes { get; set; } = 1;

            [JsonProperty("Максимум ящиков")]
            public int MaxBoxes { get; set; } = 3;

            [JsonProperty("Радиус спавна ящиков")]
            public float BoxSpawnRadius { get; set; } = 10f;
        }

        class LootCategory
        {
            [JsonProperty("Category name")]
            public string Name { get; set; }

            [JsonProperty("Category weight (higher = more common)")]
            public int Weight { get; set; }

            [JsonProperty("Items in category")]
            public List<LootItem> Items { get; set; } = new List<LootItem>();
        }

        class LootItem
        {
            [JsonProperty("Item shortname")]
            public string ShortName { get; set; }

            [JsonProperty("Minimum amount")]
            public int MinAmount { get; set; }

            [JsonProperty("Maximum amount")]
            public int MaxAmount { get; set; }

            [JsonProperty("Drop chance (0-100)")]
            public float Chance { get; set; }

            [JsonProperty("Is blueprint")]
            public bool Blueprint { get; set; }

            [JsonProperty("Custom skin ID")]
            public ulong SkinID { get; set; }
        }

        class CommandSettings
        {
            [JsonProperty("Основная команда")]
            public string MainCommand { get; set; } = "cobaltlab";

            [JsonProperty("Команда старта")]
            public string StartCommand { get; set; } = "start";

            [JsonProperty("Команда остановки")]
            public string StopCommand { get; set; } = "stop";

            [JsonProperty("Команда статуса")]
            public string StatusCommand { get; set; } = "status";

            [JsonProperty("Команда настройки времени")]
            public string TimeCommand { get; set; } = "time";

            [JsonProperty("Команда настройки вебхука")]
            public string WebhookCommand { get; set; } = "webhook";

            [JsonProperty("Команда автостарта")]
            public string AutoStartCommand { get; set; } = "autostart";
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
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration
            {
                Event = new EventSettings
                {
                    MinPlayers = 0,
                    MinStartTime = 3000,
                    MaxStartTime = 7200
                },
                Notifications = new NotificationSettings
                {
                    DiscordWebHook = "",
                    EnableUINotifications = true,
                    AutoHideUI = true,
                    HideDelay = 15.0f
                },
                Radiation = new RadiationSettings
                {
                    EnableRadiation = true,
                    RadiationParticles = 20
                },
                Map = new MapSettings
                {
                    ShowOnMap = true,
                    MapText = "Cobalt lab"
                },
                Bots = new BotSettings
                {
                    Types = new List<BotType>
                    {
                        new BotType
                        {
                            Name = "Штурмовик",
                            Health = 150,
                            Accuracy = 0.7f,
                            RoamRange = 30,
                            ChaseRange = 50,
                            Equipment = new Equipment
                            {
                                Weapons = new List<string> { "rifle.ak", "pistol.python" },
                                Armor = new List<string> { "metal.facemask", "metal.plate.torso" }
                            }
                        },
                        new BotType
                        {
                            Name = "Снайпер",
                            Health = 100,
                            Accuracy = 0.9f,
                            RoamRange = 50,
                            ChaseRange = 100,
                            Equipment = new Equipment
                            {
                                Weapons = new List<string> { "rifle.bolt", "pistol.revolver" },
                                Armor = new List<string> { "metal.facemask", "roadsign.jacket" }
                            }
                        },
                        new BotType
                        {
                            Name = "Медик",
                            Health = 120,
                            Accuracy = 0.6f,
                            RoamRange = 20,
                            ChaseRange = 30,
                            Equipment = new Equipment
                            {
                                Weapons = new List<string> { "smg.mp5", "pistol.semiauto" },
                                Armor = new List<string> { "coffeecan.helmet", "roadsign.jacket" },
                                Items = new List<string> { "syringe.medical", "bandage" }
                            }
                        }
                    },
                    Behavior = new BotBehavior
                    {
                        UseCover = true,
                        HelpAllies = true,
                        RetreatWhenLowHealth = true,
                        LowHealthThreshold = 0.3f
                    }
                },
                UI = new UISettings
                {
                    TimerColor = "1 1 1 1",
                    NotificationColor = "0.7 0.3 0.3 1",
                    ShowEventTimer = true,
                    ShowKillFeed = true,
                    ShowMinimap = true
                },
                Loot = new LootSettings
                {
                    BoxSettings = new BoxSettings(),
                    Categories = new List<LootCategory>
                    {
                        new LootCategory
                        {
                            Name = "Оружие",
                            Weight = 30,
                            Items = new List<LootItem>
                            {
                                new LootItem { ShortName = "rifle.ak", MinAmount = 1, MaxAmount = 1, Chance = 20, Blueprint = true },
                                new LootItem { ShortName = "rifle.bolt", MinAmount = 1, MaxAmount = 1, Chance = 15, Blueprint = true },
                                new LootItem { ShortName = "rifle.l96", MinAmount = 1, MaxAmount = 1, Chance = 10, Blueprint = true },
                                new LootItem { ShortName = "rifle.lr300", MinAmount = 1, MaxAmount = 1, Chance = 20 },
                                new LootItem { ShortName = "smg.mp5", MinAmount = 1, MaxAmount = 1, Chance = 25 },
                                new LootItem { ShortName = "lmg.m249", MinAmount = 1, MaxAmount = 1, Chance = 5 },
                                new LootItem { ShortName = "rocket.launcher", MinAmount = 1, MaxAmount = 1, Chance = 8, Blueprint = true }
                            }
                        },
                        new LootCategory
                        {
                            Name = "Взрывчатка",
                            Weight = 20,
                            Items = new List<LootItem>
                            {
                                new LootItem { ShortName = "explosive.timed", MinAmount = 1, MaxAmount = 10, Chance = 15 },
                                new LootItem { ShortName = "ammo.rocket.basic", MinAmount = 5, MaxAmount = 15, Chance = 20 },
                                new LootItem { ShortName = "ammo.rocket.hv", MinAmount = 5, MaxAmount = 15, Chance = 15 },
                                new LootItem { ShortName = "ammo.rocket.fire", MinAmount = 5, MaxAmount = 15, Chance = 10 },
                                new LootItem { ShortName = "rocket.launcher", MinAmount = 1, MaxAmount = 1, Chance = 8, Blueprint = true }
                            }
                        },
                        new LootCategory
                        {
                            Name = "Боеприпасы",
                            Weight = 50,
                            Items = new List<LootItem>
                            {
                                new LootItem { ShortName = "ammo.rifle", MinAmount = 120, MaxAmount = 240, Chance = 50 },
                                new LootItem { ShortName = "ammo.rifle.hv", MinAmount = 60, MaxAmount = 120, Chance = 30 },
                                new LootItem { ShortName = "ammo.rifle.explosive", MinAmount = 20, MaxAmount = 40, Chance = 15 },
                                new LootItem { ShortName = "ammo.rocket.basic", MinAmount = 2, MaxAmount = 4, Chance = 10 },
                                new LootItem { ShortName = "ammo.rocket.hv", MinAmount = 2, MaxAmount = 4, Chance = 8 }
                            }
                        },
                        new LootCategory
                        {
                            Name = "Компоненты",
                            Weight = 40,
                            Items = new List<LootItem>
                            {
                                new LootItem { ShortName = "explosives", MinAmount = 20, MaxAmount = 50, Chance = 30 },
                                new LootItem { ShortName = "targeting.computer", MinAmount = 1, MaxAmount = 2, Chance = 20 },
                                new LootItem { ShortName = "rifle.body", MinAmount = 1, MaxAmount = 3, Chance = 40 },
                                new LootItem { ShortName = "tech.trash", MinAmount = 5, MaxAmount = 10, Chance = 50 }
                            }
                        },
                        new LootCategory
                        {
                            Name = "Ресурсы",
                            Weight = 70,
                            Items = new List<LootItem>
                            {
                                new LootItem { ShortName = "sulfur", MinAmount = 2000, MaxAmount = 4000, Chance = 60 },
                                new LootItem { ShortName = "metal.refined", MinAmount = 200, MaxAmount = 500, Chance = 40 },
                                new LootItem { ShortName = "gunpowder", MinAmount = 1000, MaxAmount = 2000, Chance = 50 },
                                new LootItem { ShortName = "metal.fragments", MinAmount = 2000, MaxAmount = 4000, Chance = 60 },
                                new LootItem { ShortName = "charcoal", MinAmount = 2000, MaxAmount = 4000, Chance = 70 }
                            }
                        }
                    }
                },
                Commands = new CommandSettings
                {
                    MainCommand = "cobaltlab",
                    StartCommand = "start",
                    StopCommand = "stop",
                    StatusCommand = "status",
                    TimeCommand = "time",
                    WebhookCommand = "webhook",
                    AutoStartCommand = "autostart"
                }
            };
        }

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Oxide Hooks
        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            permission.RegisterPermission(PermissionAdmin, this);
            cmd.AddChatCommand(config.Commands.MainCommand, this, nameof(CmdCobaltLab));

            // Запускаем таймер для проверки автозапуска
            if (config.Event.EnableAutoStart)
            {
                autoStartCheckTimer = timer.Every(60f, CheckAutoStart);
            }
        }

        private void OnServerInitialized(bool initial)
        {
            if (initial)
                ScheduleNextEvent();
        }

        void Unload()
        {
            // Принудительно останавливаем событие перед выгрузкой
            isEventActive = false;
            
            // Очищаем все таймеры
            eventTimer?.Destroy();
            eventDurationTimer?.Destroy();
            radiationTimer?.Destroy();
            autoStartCheckTimer?.Destroy();

            // Очищаем все сущности
            foreach (var npc in activeNPCs.ToList())
            {
                if (npc != null && !npc.IsDestroyed)
                    npc.Kill();
            }
            activeNPCs.Clear();

            foreach (var entity in spawnedEntities.ToList())
            {
                if (entity != null && !entity.IsDestroyed)
                    entity.Kill();
            }
            spawnedEntities.Clear();

            // Очищаем UI у всех игроков
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player != null && player.IsConnected)
                    CuiHelper.DestroyUi(player, "CobaltLabNotification");
            }

            // Очищаем маркер на карте
            RemoveMapMarker();
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            var npc = entity as BasePlayer;
            if (npc == null || !activeNPCs.Contains(npc)) return;

            activeNPCs.Remove(npc);
            
            if (info?.InitiatorPlayer != null)
            {
                var killer = info.InitiatorPlayer;
                BroadcastKill(killer, npc.displayName);
            }

            // Если все боты мертвы, завершаем событие
            if (activeNPCs.Count == 0)
            {
                timer.Once(30f, () => StopEvent());
            }
        }
        #endregion

        #region Commands
        [Command("cobaltlab")]
        private void CmdCobaltLab(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin))
            {
                SendMessage(player, "У вас нет прав на использование этой команды");
                return;
            }

            if (args.Length == 0)
            {
                SendMessage(player, $"Используйте: /{config.Commands.MainCommand} {config.Commands.StartCommand}|{config.Commands.StopCommand}|{config.Commands.StatusCommand}|{config.Commands.TimeCommand}|{config.Commands.WebhookCommand}|{config.Commands.AutoStartCommand}");
                SendMessage(player, $"{config.Commands.StartCommand} [время_в_секундах] - Запустить событие");
                SendMessage(player, $"{config.Commands.StopCommand} - Остановить текущее событие");
                SendMessage(player, $"{config.Commands.StatusCommand} - Показать статус события");
                SendMessage(player, $"{config.Commands.TimeCommand} <min> <max> - Установить интервал появления (в секундах)");
                SendMessage(player, $"{config.Commands.WebhookCommand} <url> - Установить Discord webhook URL");
                SendMessage(player, $"{config.Commands.AutoStartCommand} <время_в_секундах> - Установить время автозапуска");
                SendMessage(player, $"{config.Commands.AutoStartCommand} disable - Отключить автозапуск");
                return;
            }

            switch (args[0].ToLower())
            {
                case var cmd when cmd == config.Commands.StartCommand.ToLower():
                    if (args.Length > 1 && int.TryParse(args[1], out int delay))
                    {
                        if (eventTimer != null) eventTimer.Destroy();
                        eventTimer = timer.Once(delay, StartEvent);
                        SendMessage(player, $"Событие запустится через {delay} секунд");
                    }
                    else
                    {
                        StartEvent();
                    }
                    break;

                case var cmd when cmd == config.Commands.StopCommand.ToLower():
                    StopEvent();
                    SendMessage(player, "Событие остановлено");
                    break;

                case var cmd when cmd == config.Commands.StatusCommand.ToLower():
                    ShowEventStatus(player);
                    if (eventTimer != null)
                    {
                        var nextEvent = config.Event.MinStartTime;
                        SendMessage(player, $"Следующее событие через: {nextEvent} секунд");
                    }
                    break;

                case var cmd when cmd == config.Commands.TimeCommand.ToLower():
                    if (args.Length >= 3 && int.TryParse(args[1], out int min) && int.TryParse(args[2], out int max))
                    {
                        if (min > max)
                        {
                            SendMessage(player, "Минимальное время не может быть больше максимального");
                            return;
                        }

                        config.Event.MinStartTime = min;
                        config.Event.MaxStartTime = max;
                        SaveConfig();
                        
                        SendMessage(player, $"Интервал появления установлен: {min}-{max} секунд");
                        
                        ScheduleNextEvent();
                    }
                    else
                    {
                        SendMessage(player, $"Используйте: /{config.Commands.MainCommand} {config.Commands.TimeCommand} <min> <max>");
                    }
                    break;

                case var cmd when cmd == config.Commands.WebhookCommand.ToLower():
                    if (args.Length >= 2)
                    {
                        string url = args[1];
                        config.Notifications.DiscordWebHook = url;
                        SaveConfig();
                        
                        SendDiscordMessage(":white_check_mark: **Webhook успешно настроен!**");
                        SendMessage(player, "Discord webhook URL обновлен и протестирован");
                    }
                    else
                    {
                        SendMessage(player, $"Используйте: /{config.Commands.MainCommand} {config.Commands.WebhookCommand} <url>");
                    }
                    break;

                case var cmd when cmd == config.Commands.AutoStartCommand.ToLower():
                    if (args.Length >= 2)
                    {
                        if (args[1].ToLower() == "disable")
                        {
                            config.Event.EnableAutoStart = false;
                            config.Event.AutoStartTime = 0;
                            SaveConfig();
                            SendMessage(player, "Автозапуск отключен");
                        }
                        else if (int.TryParse(args[1], out int autoStartTime))
                        {
                            config.Event.EnableAutoStart = true;
                            config.Event.AutoStartTime = autoStartTime;
                            SaveConfig();
                            SendMessage(player, $"Время автозапуска установлено на {autoStartTime} секунд");
                        }
                        else
                        {
                            SendMessage(player, $"Используйте: /{config.Commands.MainCommand} {config.Commands.AutoStartCommand} <время_в_секундах> или disable");
                        }
                    }
                    else
                    {
                        SendMessage(player, $"Используйте: /{config.Commands.MainCommand} {config.Commands.AutoStartCommand} <время_в_секундах> или disable");
                    }
                    break;

                default:
                    SendMessage(player, $"Неизвестная команда. Используйте: /{config.Commands.MainCommand} {config.Commands.StartCommand}|{config.Commands.StopCommand}|{config.Commands.StatusCommand}|{config.Commands.TimeCommand}|{config.Commands.WebhookCommand}|{config.Commands.AutoStartCommand}");
                    break;
            }
        }
        #endregion

        #region Core Methods
        private void ScheduleNextEvent()
        {
            if (eventTimer != null) eventTimer.Destroy();
            
            float delay = UnityEngine.Random.Range(config.Event.MinStartTime, config.Event.MaxStartTime);
            eventTimer = timer.Once(delay, StartEvent);
        }

        private void StartEvent()
        {
            if (isEventActive) return;
            if (BasePlayer.activePlayerList.Count < config.Event.MinPlayers) return;

            isEventActive = true;
            eventEndTime = Time.time + 900f; // 15 минут на событие
            
            FindEventPosition();
            SpawnBuilding();
            SpawnNPCs();
            CreateLootBox();
            
            if (config.Radiation.EnableRadiation)
                CreateRadiation();

            if (config.Map.ShowOnMap)
                CreateMapMarker();

            // Запускаем таймер события
            eventDurationTimer = timer.Once(900f, () => StopEvent());

            BroadcastEventStart();
        }

        private void StopEvent()
        {
            if (!isEventActive) return;

            // Отправляем сообщение в Discord
            var message = ":stop_sign: **Событие Cobalt Laboratory завершено!**";
            if (activeNPCs.Count == 0)
                message += "\nВсе боты были уничтожены!";
            SendDiscordMessage(message);

            CleanupEvent();
            ScheduleNextEvent();
        }

        private void CleanupEvent()
        {
            isEventActive = false;
            
            // Очищаем таймеры безопасно
            if (radiationTimer != null && !radiationTimer.Destroyed)
            {
                radiationTimer.Destroy();
                radiationTimer = null;
            }

            if (eventDurationTimer != null && !eventDurationTimer.Destroyed)
            {
                eventDurationTimer.Destroy();
                eventDurationTimer = null;
            }

            if (eventTimer != null && !eventTimer.Destroyed)
            {
                eventTimer.Destroy();
                eventTimer = null;
            }

            // Очищаем сущности с проверками
            if (activeNPCs != null)
            {
                foreach (var npc in activeNPCs.ToList())
                {
                    try
                    {
                        if (npc != null && !npc.IsDestroyed)
                        {
                            npc.Kill();
                            PrintWarning($"Удален бот: {npc.displayName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintError($"Ошибка при удалении бота: {ex.Message}");
                    }
                }
                activeNPCs.Clear();
            }

            if (spawnedEntities != null)
            {
                foreach (var entity in spawnedEntities.ToList())
                {
                    try
                    {
                        if (entity != null && !entity.IsDestroyed)
                        {
                            entity.Kill();
                            PrintWarning($"Удалена сущность: {entity.ShortPrefabName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintError($"Ошибка при удалении сущности: {ex.Message}");
                    }
                }
                spawnedEntities.Clear();
            }

            // Очищаем UI у всех игроков безопасно
            foreach (var player in BasePlayer.activePlayerList.ToList())
            {
                try
                {
                    if (player != null && player.IsConnected)
                    {
                        CuiHelper.DestroyUi(player, "CobaltLabNotification");
                        CuiHelper.DestroyUi(player, "CobaltLabMinimap");
                        CuiHelper.DestroyUi(player, "CobaltLabCompass");
                    }
                }
                catch (Exception ex)
                {
                    PrintError($"Ошибка при очистке UI у игрока {player?.displayName}: {ex.Message}");
                }
            }

            // Очищаем маркер на карте
            RemoveMapMarker();

            // Сбрасываем переменные
            eventPosition = Vector3.zero;
            lootBox = null;
            radiationEntity = null;
            eventEndTime = 0f;
            
            PrintWarning("Событие успешно очищено");
        }

        private void FindEventPosition()
        {
            var attempts = 0;
            const int maxAttempts = 100;
            const float minDistanceFromMonument = 50f;
            const float maxDistanceFromMonument = 150f;
            
            while (attempts < maxAttempts)
            {
                // Получаем случайную позицию на карте (исключая края)
                var randomPos = TerrainMeta.Position + new Vector3(
                    UnityEngine.Random.Range(TerrainMeta.Size.x * 0.2f, TerrainMeta.Size.x * 0.8f),
                    0f,
                    UnityEngine.Random.Range(TerrainMeta.Size.z * 0.2f, TerrainMeta.Size.z * 0.8f)
                );

                // Получаем высоту в этой точке
                var height = TerrainMeta.HeightMap.GetHeight(randomPos);
                randomPos.y = height;

                // Проверяем, что позиция не в воде
                if (WaterLevel.GetWaterDepth(randomPos, true, true, null) > 0.1f)
                {
                    attempts++;
                    continue;
                }

                // Проверяем уклон поверхности
                if (TerrainMeta.HeightMap.GetSlope(randomPos) > 40f)
                {
                    attempts++;
                    continue;
                }

                // Проверяем, что рядом нет построек
                var entities = Physics.OverlapSphere(randomPos, 20f, LayerMask.GetMask("Construction", "Deployed"));
                if (entities.Length > 0)
                {
                    attempts++;
                    continue;
                }

                // Проверяем расстояние до монументов
                bool validPosition = true;
                foreach (var monument in TerrainMeta.Path.Monuments)
                {
                    float distance = Vector3.Distance(randomPos, monument.transform.position);
                    if (distance < minDistanceFromMonument || distance > maxDistanceFromMonument)
                    {
                        validPosition = false;
                        break;
                    }
                }

                if (!validPosition)
                {
                    attempts++;
                    continue;
                }

                // Проверяем, что на этом месте можно строить
                var ray = new Ray(randomPos + new Vector3(0f, 5f, 0f), Vector3.down);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 10f, LayerMask.GetMask("Terrain", "World")))
                {
                    if (hit.GetEntity() == null)
                    {
                        eventPosition = hit.point;
                        PrintWarning($"Найдена подходящая позиция: {eventPosition}");
                        return;
                    }
                }

                attempts++;
            }

            // Если не нашли позицию, используем центр карты
            eventPosition = TerrainMeta.Position + TerrainMeta.Size / 2f;
            eventPosition.y = TerrainMeta.HeightMap.GetHeight(eventPosition);
            PrintWarning($"Используем центр карты как позицию: {eventPosition}");
        }

        private void SpawnBuilding()
        {
            var building = GameManager.server.CreateEntity("assets/prefabs/building core/foundation/foundation.prefab", eventPosition);
            if (building == null) return;
            
            building.Spawn();
            spawnedEntities.Add(building);
        }

        private void SpawnNPCs()
        {
            if (config?.Bots?.Types == null)
            {
                PrintError("Ошибка: Конфигурация ботов отсутствует");
                return;
            }

            foreach (var botType in config.Bots.Types)
            {
                try
                {
                    if (string.IsNullOrEmpty(botType?.Name))
                    {
                        PrintError("Ошибка: Имя бота не задано");
                        continue;
                    }

                    // Создаем НПС с правильным префабом
                    var npcPlayer = GameManager.server.CreateEntity("assets/prefabs/npc/scientist/scientistspawn.prefab", 
                        eventPosition + new Vector3(UnityEngine.Random.Range(-5f, 5f), 1f, UnityEngine.Random.Range(-5f, 5f))) as NPCPlayer;
                    
                    if (npcPlayer == null)
                    {
                        PrintError($"Не удалось создать бота {botType.Name}");
                        continue;
                    }

                    // Настраиваем базовые параметры с проверками
                    npcPlayer.displayName = $"Cobalt {botType.Name}";
                    npcPlayer.startHealth = Mathf.Max(1f, botType.Health);
                    npcPlayer.health = npcPlayer.startHealth;
                    npcPlayer._maxHealth = npcPlayer.startHealth;
                    npcPlayer.InitializeHealth(npcPlayer.startHealth, npcPlayer.startHealth);

                    // Спавним НПС
                    npcPlayer.Spawn();

                    // Проверяем валидность инвентаря
                    if (npcPlayer.inventory == null || 
                        npcPlayer.inventory.containerBelt == null || 
                        npcPlayer.inventory.containerWear == null)
                    {
                        PrintError($"Ошибка: Невалидный инвентарь у бота {botType.Name}");
                        npcPlayer.Kill();
                        continue;
                    }

                    // Экипировка бота с проверками
                    if (botType.Equipment?.Weapons != null)
                    {
                        foreach (var weapon in botType.Equipment.Weapons)
                        {
                            if (string.IsNullOrEmpty(weapon)) continue;

                            var item = ItemManager.CreateByName(weapon);
                            if (item != null)
                            {
                                if (!item.MoveToContainer(npcPlayer.inventory.containerBelt))
                                {
                                    PrintError($"Не удалось экипировать оружие {weapon} боту {botType.Name}");
                                    item.Remove();
                                    continue;
                                }

                                // Устанавливаем первое оружие как активное
                                if (npcPlayer.inventory.containerBelt.itemList.Count == 1)
                                {
                                    npcPlayer.UpdateActiveItem(item.uid);
                                }
                            }
                        }
                    }

                    if (botType.Equipment?.Armor != null)
                    {
                        foreach (var armor in botType.Equipment.Armor)
                        {
                            if (string.IsNullOrEmpty(armor)) continue;

                            var item = ItemManager.CreateByName(armor);
                            if (item != null)
                            {
                                if (!item.MoveToContainer(npcPlayer.inventory.containerWear))
                                {
                                    PrintError($"Не удалось экипировать броню {armor} боту {botType.Name}");
                                    item.Remove();
                                }
                            }
                        }
                    }

                    // Включаем фонарик ночью с проверками
                    if (config.Bots.EnableFlashlights && TOD_Sky.Instance != null && TOD_Sky.Instance.IsNight)
                    {
                        var flashlight = ItemManager.CreateByName("flashlight.held");
                        if (flashlight != null)
                        {
                            if (!flashlight.MoveToContainer(npcPlayer.inventory.containerBelt))
                            {
                                PrintError($"Не удалось экипировать фонарик боту {botType.Name}");
                                flashlight.Remove();
                            }
                        }
                    }

                    activeNPCs.Add(npcPlayer);
                    PrintWarning($"Создан бот {botType.Name} в позиции {npcPlayer.transform.position}");
                }
                catch (Exception ex)
                {
                    PrintError($"Ошибка при создании бота {botType?.Name}: {ex.Message}");
                }
            }
        }

        private void CreateLootBox()
        {
            try
            {
                // Определяем количество ящиков
                int boxCount = 1;
                if (config.Loot.BoxSettings.EnableMultipleBoxes)
                {
                    boxCount = UnityEngine.Random.Range(
                        config.Loot.BoxSettings.MinBoxes,
                        config.Loot.BoxSettings.MaxBoxes + 1
                    );
                }

                for (int i = 0; i < boxCount; i++)
                {
                    // Вычисляем позицию для ящика
                    Vector3 boxPosition = eventPosition;
                    if (boxCount > 1)
                    {
                        float angle = (360f / boxCount) * i;
                        float radius = config.Loot.BoxSettings.BoxSpawnRadius;
                        boxPosition += new Vector3(
                            Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                            config.Loot.BoxSettings.BoxHeight,
                            Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                        );
                    }
                    else
                    {
                        boxPosition += new Vector3(0, config.Loot.BoxSettings.BoxHeight, 0);
                    }

                    // Создаем ящик
                    var box = GameManager.server.CreateEntity(
                        $"assets/prefabs/deployable/{config.Loot.BoxSettings.BoxType}/{config.Loot.BoxSettings.BoxType}.prefab",
                        boxPosition
                    ) as StorageContainer;

                    if (box == null)
                    {
                        PrintError($"Не удалось создать ящик с лутом #{i + 1}");
                        continue;
                    }

                    // Настраиваем параметры ящика
                    box.OwnerID = 0;
                    box.pickup.enabled = config.Loot.BoxSettings.IsPickupable;
                    box.health = config.Loot.BoxSettings.BoxHealth;

                    box.Spawn();
                    spawnedEntities.Add(box);

                    // Если это первый ящик, сохраняем его как основной
                    if (i == 0) lootBox = box;

                    // Устанавливаем время жизни ящика
                    if (config.Loot.BoxSettings.BoxLifetime > 0)
                    {
                        timer.Once(config.Loot.BoxSettings.BoxLifetime * 60f, () =>
                        {
                            if (box != null && !box.IsDestroyed)
                            {
                                box.Kill();
                                SendDiscordMessage($":boom: **Ящик с лутом #{i + 1} исчез!**");
                            }
                        });
                    }

                    // Заполняем ящик лутом
                    timer.Once(0.5f, () =>
                    {
                        if (box != null && !box.IsDestroyed)
                        {
                            PopulateLootBox(box);
                        }
                    });
                }

                // Отправляем сообщение о появлении ящиков
                string boxMessage = boxCount == 1
                    ? ":package: **Появился ящик с лутом!**"
                    : $":package: **Появилось {boxCount} ящиков с лутом!**";
                SendDiscordMessage($"{boxMessage}\nСодержимое будет доступно после уничтожения всех ботов.");
            }
            catch (Exception ex)
            {
                PrintError($"Ошибка при создании ящиков с лутом: {ex.Message}");
            }
        }

        private void PopulateLootBox(StorageContainer box)
        {
            if (box == null || box.inventory == null) return;

            try
            {
                // Очищаем инвентарь ящика
                box.inventory.Clear();

                // Определяем количество предметов
                int itemCount = UnityEngine.Random.Range(config.Loot.MinItemsPerBox, config.Loot.MaxItemsPerBox + 1);

                for (int i = 0; i < itemCount; i++)
                {
                    // Выбираем категорию
                    var category = SelectRandomCategory();
                    if (category == null) continue;

                    // Выбираем предмет из категории
                    var item = SelectRandomItem(category);
                    if (item == null) continue;

                    // Определяем количество
                    int amount = UnityEngine.Random.Range(item.MinAmount, item.MaxAmount + 1);

                    // Создаем предмет
                    Item newItem;
                    if (item.Blueprint)
                    {
                        var itemDef = ItemManager.FindItemDefinition(item.ShortName);
                        if (itemDef != null)
                        {
                            newItem = ItemManager.Create(ItemManager.blueprintBaseDef, 1, 0UL);
                            if (newItem != null)
                            {
                                newItem.blueprintTarget = itemDef.itemid;
                                newItem.MoveToContainer(box.inventory);
                            }
                        }
                    }
                    else
                    {
                        newItem = ItemManager.CreateByName(item.ShortName, amount);
                        if (newItem != null)
                        {
                            if (item.SkinID > 0)
                            {
                                newItem.skin = item.SkinID;
                            }
                            newItem.MoveToContainer(box.inventory);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError($"Ошибка при заполнении ящика лутом: {ex.Message}");
            }
        }

        private LootCategory SelectRandomCategory()
        {
            var totalWeight = config.Loot.Categories.Sum(c => c.Weight);
            var random = UnityEngine.Random.Range(0, totalWeight);
            var currentWeight = 0;

            foreach (var category in config.Loot.Categories)
            {
                currentWeight += category.Weight;
                if (random < currentWeight)
                    return category;
            }

            return config.Loot.Categories.FirstOrDefault();
        }

        private LootItem SelectRandomItem(LootCategory category)
        {
            foreach (var item in category.Items.OrderBy(x => UnityEngine.Random.value))
            {
                if (UnityEngine.Random.Range(0f, 100f) <= item.Chance)
                    return item;
            }

            return category.Items.FirstOrDefault();
        }

        private void CreateRadiation()
        {
            if (radiationEntity != null && !radiationEntity.IsDestroyed)
            {
                radiationEntity.Kill();
                radiationEntity = null;
            }

            // Создаем сферу радиации
            var sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", eventPosition);
            if (sphere == null) return;

            sphere.Spawn();
            radiationEntity = sphere;
            spawnedEntities.Add(sphere);

            // Запускаем таймер для радиации
            if (radiationTimer != null)
                radiationTimer.Destroy();

            radiationTimer = timer.Every(1f, () =>
            {
                if (!isEventActive || radiationEntity == null)
                {
                    if (radiationTimer != null)
                    {
                        radiationTimer.Destroy();
                        radiationTimer = null;
                    }
                    return;
                }

                var radiationRange = 20f;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player == null || player.IsDestroyed || !player.IsConnected) continue;

                    if (Vector3.Distance(player.transform.position, eventPosition) <= radiationRange)
                    {
                        // Проверяем защиту от радиации
                        float protection = 0f;
                        var clothingMoveSpeed = player.clothingMoveSpeedReduction;
                        if (clothingMoveSpeed > 0)
                        {
                            // Чем больше брони, тем больше защита от радиации
                            protection = Mathf.Clamp01(clothingMoveSpeed / 0.5f);
                        }

                        // Применяем радиацию с учетом защиты
                        float radiationAmount = config.Radiation.RadiationParticles * (1f - protection);
                        if (radiationAmount > 0)
                        {
                            player.metabolism.radiation_poison.Add(radiationAmount);
                        }
                    }
                }
            });
        }

        private void CreateMapMarker()
        {
            if (!string.IsNullOrEmpty(mapMarkerID))
                RemoveMapMarker();

            mapMarkerID = $"cobaltlab_{eventPosition.x}_{eventPosition.z}";

            // Создаем маркер на карте
            rust.RunServerCommand($"marker.add {mapMarkerID} {eventPosition.x} {eventPosition.y} {eventPosition.z} 1 {config.Map.MapText}");
            PrintWarning($"Создан маркер на карте: {mapMarkerID} в позиции {eventPosition}");
        }

        private void RemoveMapMarker()
        {
            if (!string.IsNullOrEmpty(mapMarkerID))
            {
                rust.RunServerCommand($"marker.remove {mapMarkerID}");
                mapMarkerID = null;
            }
        }

        private void BroadcastEventStart()
        {
            var message = "Событие Cobalt Laboratory началось!";
            
            // Отправляем в чат
            foreach (var player in BasePlayer.activePlayerList)
                SendMessage(player, message);

            // Отправляем в Discord с эмбедом
            var discordMessage = $":radioactive: **{message}**\n\nПриходите за лутом и сразитесь с ботами!\nСобытие будет активно 15 минут.";
            SendDiscordMessage(discordMessage);
        }

        private void ShowEventStatus(BasePlayer player)
        {
            string status = isEventActive ? "активно" : "неактивно";
            SendMessage(player, $"Событие сейчас {status}");
        }

        private void SendMessage(BasePlayer player, string message)
        {
            if (config.Notifications.EnableUINotifications)
                CreateUI(player, message);
            else
                player.ChatMessage(message);
        }

        private void CreateUI(BasePlayer player, string message)
        {
            if (player == null || !player.IsConnected) return;

            try
            {
                CuiHelper.DestroyUi(player, "CobaltLabNotification");
                CuiHelper.DestroyUi(player, "CobaltLabMinimap");
                CuiHelper.DestroyUi(player, "CobaltLabCompass");

                var elements = new CuiElementContainer();
                
                // Основная панель
                elements.Add(new CuiElement
                {
                    Parent = "Hud",
                    Components = 
                    {
                        new CuiImageComponent { Color = config.UI.NotificationColor },
                        new CuiRectTransformComponent { AnchorMin = "0.3 0.8", AnchorMax = "0.7 0.85" }
                    },
                    Name = "CobaltLabNotification"
                });

                // Текст сообщения
                elements.Add(new CuiElement
                {
                    Parent = "CobaltLabNotification",
                    Components = 
                    {
                        new CuiTextComponent 
                        { 
                            Text = message, 
                            FontSize = 14, 
                            Align = TextAnchor.MiddleCenter,
                            Font = "robotocondensed-bold.ttf"
                        },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                // Компас с направлением
                if (config.UI.ShowMinimap && isEventActive)
                {
                    var direction = GetDirectionToEvent(player.transform.position);
                    elements.Add(new CuiElement
                    {
                        Parent = "Hud",
                        Components = 
                        {
                            new CuiTextComponent 
                            { 
                                Text = $"↑ Событие: {direction}м", 
                                FontSize = 12,
                                Align = TextAnchor.MiddleCenter,
                                Color = "1 1 1 1"
                            },
                            new CuiRectTransformComponent { AnchorMin = "0.45 0.95", AnchorMax = "0.55 0.98" }
                        },
                        Name = "CobaltLabCompass"
                    });
                }

                CuiHelper.AddUi(player, elements);
                
                if (config.Notifications.AutoHideUI)
                {
                    timer.Once(config.Notifications.HideDelay, () =>
                    {
                        if (player != null && player.IsConnected)
                        {
                            CuiHelper.DestroyUi(player, "CobaltLabNotification");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                PrintError($"Ошибка при создании UI для игрока {player.displayName}: {ex.Message}");
            }
        }

        private string GetDirectionToEvent(Vector3 playerPos)
        {
            var delta = eventPosition - playerPos;
            var distance = Mathf.Round(delta.magnitude);
            return $"{distance}";
        }

        private void BroadcastKill(BasePlayer killer, string victimName)
        {
            if (!config.UI.ShowKillFeed) return;

            var message = $"{killer.displayName} уничтожил {victimName}!";

            // Отправляем в чат
            foreach (var player in BasePlayer.activePlayerList)
            {
                SendMessage(player, message);
            }

            // Отправляем в Discord
            var discordMessage = $":skull: **{message}**";
            SendDiscordMessage(discordMessage);
        }

        private void SendDiscordMessage(string message)
        {
            if (string.IsNullOrEmpty(config?.Notifications?.DiscordWebHook))
            {
                PrintWarning("Discord webhook не настроен");
                return;
            }

            try
            {
                var description = $"{message}\n\n";

                if (eventPosition != Vector3.zero)
                {
                    description += $"📍 **Координаты:** X: {Math.Round(eventPosition.x)}, Y: {Math.Round(eventPosition.y)}, Z: {Math.Round(eventPosition.z)}\n" +
                                 $"🏛️ **Ближайший монумент:** {GetNearestMonument()}\n";
                }

                if (message.Contains("Ящик с лутом") && config?.Loot?.Categories != null)
                {
                    description += "\n💎 **Возможный лут:**\n";
                    foreach (var category in config.Loot.Categories)
                    {
                        if (category?.Items == null) continue;

                        var topItems = category.Items
                            .Where(x => x != null)
                            .OrderByDescending(x => x.Chance)
                            .Take(3)
                            .Select(x => $"• {x.ShortName}" + (x.Blueprint ? " (BP)" : ""));

                        description += $"**{category.Name}:** {string.Join(", ", topItems)}\n";
                    }
                }

                // Ограничиваем длину сообщения
                if (description.Length > 2000)
                {
                    description = description.Substring(0, 1997) + "...";
                }

                var payload = new Dictionary<string, object>
                {
                    ["username"] = "N1KTO COMPANY - Cobalt Laboratory",
                    ["embeds"] = new List<Dictionary<string, object>>
                    {
                        new Dictionary<string, object>
                        {
                            ["title"] = "N1KTO COMPANY - Cobalt Laboratory",
                            ["description"] = description,
                            ["color"] = 16711680,
                            ["footer"] = new Dictionary<string, string>
                            {
                                ["text"] = $"Cobalt Laboratory Event • {DateTime.Now:dd.MM.yyyy HH:mm}"
                            }
                        }
                    }
                };

                webrequest.Enqueue(config.Notifications.DiscordWebHook, 
                    JsonConvert.SerializeObject(payload), 
                    (code, response) => 
                    {
                        if (code != 200 && code != 204)
                        {
                            PrintError($"Ошибка отправки в Discord: {code} {response}");
                        }
                        else
                        {
                            PrintWarning("Сообщение успешно отправлено в Discord");
                        }
                    }, 
                    this,
                    Core.Libraries.RequestMethod.POST,
                    new Dictionary<string, string>
                    {
                        ["Content-Type"] = "application/json"
                    });
            }
            catch (Exception ex)
            {
                PrintError($"Ошибка при отправке сообщения в Discord: {ex.Message}");
            }
        }

        private string GetNearestMonument()
        {
            var nearestMonument = "Неизвестно";
            var shortestDistance = float.MaxValue;

            foreach (var monument in TerrainMeta.Path.Monuments)
            {
                var distance = Vector3.Distance(eventPosition, monument.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestMonument = monument.name;
                }
            }

            return $"{nearestMonument} ({Math.Round(shortestDistance)}m)";
        }

        private void CheckAutoStart()
        {
            if (!config.Event.EnableAutoStart || isEventActive) return;

            var currentTime = DateTime.Now;
            var autoStartTime = TimeSpan.FromSeconds(config.Event.AutoStartTime);
            var currentTimeOfDay = currentTime.TimeOfDay;

            // Если текущее время совпадает с временем автозапуска (с погрешностью в 1 минуту)
            if (Math.Abs((currentTimeOfDay - autoStartTime).TotalMinutes) < 1)
            {
                StartEvent();
            }
        }
        #endregion
    }
} 