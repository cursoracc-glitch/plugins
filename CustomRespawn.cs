// Custom Respawn скачан с сайта Server-rust.ru Сотни новых бесплатных плагинов уже на нашем сайте! 
// Присоеденяйся к нам! Server-rust.ru
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Respawn", "Server-rust.ru", "0.1.91")]
    [Description("Выбор снаряжения во время возраждения.")]
    {
        #region ENums

        private enum ItemType
        {
            ОСНОВНОЕ_ОРУЖИЕ,
            ДОПОЛНИТЕЛЬНОЕ_ОРУЖИЕ,
            
            МЕДИЦИНСКИЙ_ПРЕПАРАТ,
            ПРИПАСЫ
        }
        
        #endregion
        
        #region Classes

        #region Perks

        private class PlayerPerk : MonoBehaviour
        {
            [JsonProperty("Отображаемое имя перка")]
            public string DisplayName;
            [JsonProperty("Ссылка на изображение")]
            public string PictureURL;
            [JsonProperty("Описание")]
            public string Description;

            [JsonProperty("Цена данного перка")]
            public int Price;
        }

        private class HealPerk : PlayerPerk
        {
            private void Awake()
            {
                Interface.Oxide.LogWarning("Условно будем здесь хилить игрока");
                GetComponent<BasePlayer>().Heal(100);
            }
        }

        private class KillPerk : PlayerPerk
        {
            private void Awake()
            {
                Interface.Oxide.LogWarning("Условно будем здесь убивать игрока");
                // TODO: GetComponent<BasePlayer>().Die();
            }
        }

        #endregion
        
        #region LoadOut
        
        private class CustomItem
        {
            [JsonProperty("Отображаемое имя")]
            public string DisplayName;
            [JsonProperty("Короткое имя предмета")]
            public string ShortName;
            [JsonProperty("Номер скина (ID)")]
            public ulong SkinID;
            [JsonProperty("Количество предмета")]
            public int Amount;
            [JsonProperty("Доп. изображение предмета")]
            public string PictureURL;
            [JsonProperty("Разрешить только по пермишену")]
            public string Permission;
            [JsonProperty("Дополнительные предметы")]
            public Dictionary<string, int> AdditionalItems = new Dictionary<string, int>();

            [JsonProperty("Цена предмета")]
            public int Price;

            public void CreateItem(BasePlayer player)
            {
                if (string.IsNullOrEmpty(ShortName))
                    return;
                
                Item createItem = ItemManager.CreateByPartialName(ShortName, Amount);
                createItem.skin = SkinID;
                createItem.name = DisplayName;
                createItem.MoveToContainer(player.inventory.containerBelt);
                
                foreach (var check in AdditionalItems)
                {
                    createItem = ItemManager.CreateByPartialName(check.Key, check.Value);
                    createItem.skin = SkinID;
                    createItem.name = DisplayName;
                    createItem.MoveToContainer(player.inventory.containerMain);
                }
            }
        }
        
        private class PlayerPack
        {
            [JsonProperty("Список предметов игрока")]
            public Dictionary<ItemType, CustomItem> CustomItems = new Dictionary<ItemType, CustomItem>();
            [JsonProperty("Улучшения которые можно будет докупать")]
            public List<PlayerPerk> PerkList = new List<PlayerPerk>();

            public bool AddPerk(PlayerPerk addPerk)
            {
                if (PerkList.Contains(addPerk))
                    return false;
                
                PerkList.Add(addPerk);
                return true;
            }
            
            public int BuyPrice()
            {
                int resultPrice = CustomItems.Sum(p => p.Value.Price) + PerkList.Sum(p => p.Price);
                return resultPrice;
            }

            public bool IsDefault()
            {
                foreach (var check in CustomItems)
                {
                    if (check.Value != config.ListOfItems[check.Key].First().Value)
                        return false;
                }

                return true;
            }
        }
        
        #endregion

        #region Configuration

        private class Configuration
        {
            #region Side Section

            internal class Design
            {
                [JsonProperty("Ссылка на изображение заднего плана (1920х1080)")]
                public string CONF_BackgroundImage = "https://i.imgur.com/1wfjYCc.jpg";
                [JsonProperty("Ссылка на изображение человека (270х686)")]
                public string CONF_ManImage = "https://i.imgur.com/CuD4xv4.png";
            }
            
            internal class Balance
            {
                [JsonProperty("Использовать баланс от Server Rewards")]
                public bool CONF_UseSR;
                [JsonProperty("Использовать баланс от Economics")]
                public bool CONF_UseEconomics;
                [JsonProperty("Использовать баланс от RustShop")]
                public bool CONF_UseRustShop;
                [JsonProperty("Использовать баланс от NShop")]
                public bool CONF_UseNShop;
                [JsonProperty("Использовать внутренний баланс (сихронизация с донат-магазинами)")]
                public bool CONF_UseCustom;
                [JsonProperty("Стартовый баланс при использовании внутреннего баланса")]
                public int CONF_CustomStartBalance;
            }

            internal class Plugin
            {
                [JsonProperty("Показывать меню выбора снаряжения для игроков с привилегией")]
                public string CONF_ShowPermission;
                [JsonProperty("Выдавать помимо закупленных предметов - автоКит")]
                public bool CONF_GiveAutoKit;
                [JsonProperty("Настройки отображения информации об убийце")]
                public KillInfo CONF_KillInfo = new KillInfo();
            }

            internal class KillInfo
            {
                internal class Button
                {
                    [JsonProperty("Отображаемое имя")]
                    public string DisplayName;
                    [JsonProperty("Команда")]
                    public string Command;

                    [JsonProperty("Цвет кнопки")]
                    public string Color;
                }
                [JsonProperty("Показывать информацию об убийце")]
                public bool CONF_ShowKillerInfo;
                [JsonProperty("Кнопки под информацией")]
                public List<Button> CONF_KillerInfoButton = new List<Button>();
            }

            #endregion
            
            [JsonProperty("Настройки дизайна плагина", Order = 0)]
            public Design DesignSetting = new Design();
            [JsonProperty("Настройки баланса игроков", Order = 1)]
            public Balance BalanceSettings = new Balance();
            [JsonProperty("Настройки плагина", Order = 2)]
            public Plugin PluginSettings = new Plugin();
            [JsonProperty("Предметы доступные для покупки", Order = 3)]
            public Dictionary<ItemType, Dictionary<string, CustomItem>> ListOfItems;

            #region Default Methods

            public PlayerPack GetDefaultPack()
            {
                PlayerPack newPack = new PlayerPack();
                
                foreach (var check in ListOfItems.Where(p => p.Value.ContainsKey("default")))
                    newPack.CustomItems.Add(check.Key, check.Value["default"]);
                
                return newPack;
            }

            public static Configuration GetNewCong()
            {
                return new Configuration
                {
                    DesignSetting = new Design
                    {
                        CONF_BackgroundImage = "https://i.imgur.com/1wfjYCc.jpg",
                        CONF_ManImage = "https://i.imgur.com/CuD4xv4.png",
                    },
                    BalanceSettings = new Balance(),
                    PluginSettings = new Plugin
                    {
                        CONF_ShowPermission = "",
                        CONF_KillInfo = new KillInfo
                        {
                            CONF_ShowKillerInfo = true,
                            CONF_KillerInfoButton = new List<KillInfo.Button>
                            {
                                new KillInfo.Button
                                {
                                    DisplayName = "<color=#ABA7A7>ПОЖАЛОВАТЬСЯ</color>",
                                    Command = "report %STEAMID%",
                                    Color = "#575757AC"
                                }
                            }
                        }
                    },
                    ListOfItems = new Dictionary<ItemType, Dictionary<string, CustomItem>>
                    {
                        [ItemType.ОСНОВНОЕ_ОРУЖИЕ] = new Dictionary<string, CustomItem>
                        {
                            ["default"] = new CustomItem
                            {
                                DisplayName = "ПУСТО",
                                ShortName = "",
                                Amount = 0,
                                SkinID = 0UL,
                                Price = 0,
                                PictureURL = "",
                                Permission = ""
                            },
                            ["1"] = new CustomItem
                            {
                                DisplayName = GetColor("Лук Лары", 1),
                                ShortName = "bow.hunting",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                AdditionalItems = new Dictionary<string, int>
                                {
                                    ["arrow.wooden"] = 16,
                                },
    
                                Price = 35,
                                Permission = ""
                            },
                            ["2"] = new CustomItem
                            {
                                DisplayName = GetColor("Арбалет Дэрила", 2),
                                ShortName = "crossbow",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                AdditionalItems = new Dictionary<string, int>
                                {
                                    ["arrow.wooden"] = 32,
                                },
                                Price = 50,
                                Permission = ""
                            },
                            ["3"] = new CustomItem
                            {
                                DisplayName = GetColor("Лучший чиркаш", 3),
                                ShortName = "pistol.eoka",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                AdditionalItems = new Dictionary<string, int>
                                {
                                    ["ammo.handmade.shell"] = 3,
                                },
                                Permission = "CustomRespawn.VIP",
    
                                Price = 75
                            },
                        },
                        [ItemType.ДОПОЛНИТЕЛЬНОЕ_ОРУЖИЕ] = new Dictionary<string, CustomItem>
                        {
                            ["default"] = new CustomItem
                            {
                                DisplayName = GetColor("Просто камень", 0),
                                ShortName = "rock",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "",
    
                                Price = 0
                            },
                            ["1"] = new CustomItem
                            {
                                DisplayName = GetColor("Топорик новичка", 1),
                                ShortName = "stonehatchet",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "",
    
                                Price = 10
                            },
                            ["2"] = new CustomItem
                            {
                                DisplayName = GetColor("Мачете Джо", 2),
                                ShortName = "machete",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "",
    
                                Price = 15
                            },
                            ["3"] = new CustomItem
                            {
                                DisplayName = GetColor("Копьё Македонского", 3),
                                ShortName = "spear.wooden",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "CustomRespawn.VIP",
    
                                Price = 25
                            },
                        },
                        [ItemType.ПРИПАСЫ] = new Dictionary<string, CustomItem>
                        {
                            ["default"] = new CustomItem
                            {
                                DisplayName = GetColor("Яблоко просвещения", 0),
                                ShortName = "apple",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "",
    
                                Price = 0
                            },
                            ["1"] = new CustomItem
                            {
                                DisplayName = GetColor("Мясо священого оленя", 1),
                                ShortName = "deermeat.cooked",
                                Amount = 2,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "",
    
                                Price = 15
                            },
                            ["2"] = new CustomItem
                            {
                                DisplayName = GetColor("Мясо медведя", 2),
                                ShortName = "bearmeat.cooked",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "",
    
                                Price = 30
                            },
                            ["3"] = new CustomItem
                            {
                                DisplayName = GetColor("Волшебные ягодки", 3),
                                ShortName = "blueberries",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "CustomRespawn.VIP",
    
                                Price = 50
                            },
                        },
                        [ItemType.МЕДИЦИНСКИЙ_ПРЕПАРАТ] = new Dictionary<string, CustomItem>
                        {
                            ["default"] = new CustomItem
                            {
                                DisplayName = "",
                                ShortName = "",
                                Amount = 0,
                                SkinID = 0UL,
                                Price = 0,
                                PictureURL = "",
                                Permission = "",
                            },
                            ["1"] = new CustomItem
                            {
                                DisplayName = GetColor("Бинт", 1),
                                ShortName = "bandage",
                                Amount = 2,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "",
    
                                Price = 15
                            },
                            ["2"] = new CustomItem
                            {
                                DisplayName = GetColor("Шприц", 2),
                                ShortName = "syringe.medical",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "",
    
                                Price = 30
                            },
                            ["3"] = new CustomItem
                            {
                                DisplayName = GetColor("Аптечка", 3),
                                ShortName = "largemedkit",
                                Amount = 1,
                                SkinID = 0UL,
                                PictureURL = "",
                                Permission = "CustomRespawn.VIP",
    
                                Price = 50
                            },
                        },
                    }
                };
            }

            #endregion
        }

        #endregion
        
        #endregion

        #region Variables

        private static bool Initialized = false;
        [PluginReference] private Plugin ImageLibrary;
        private static Configuration config = new Configuration();
        
        [JsonProperty("Список предметов у игроков")]
        private static Dictionary<ulong, PlayerPack> playerPacks = new Dictionary<ulong, PlayerPack>();
        
        #endregion

        #region Initialization
        
        private void OnServerInitialized()
        {
     		if (config.DesignSetting.CONF_BackgroundImage != "")
                ImageLibrary.Call("AddImage", config.DesignSetting.CONF_BackgroundImage, "CR_MainBackground");
            if (config.DesignSetting.CONF_ManImage != "")
                ImageLibrary.Call("AddImage", config.DesignSetting.CONF_ManImage, "CR_MainMan");

            foreach (var check in ItemManager.itemList)
                ImageLibrary.Call("AddImage", $"https://rustlabs.com/img/items180/{check.shortname}.png", check.shortname);
            if (config.PluginSettings.CONF_ShowPermission != "")
                permission.RegisterPermission(config.PluginSettings.CONF_ShowPermission, this);
            
            foreach (var check in config.ListOfItems)
            {
                if (check.Value.Count > 4)
                {
                    PrintWarning($"Внимание, в группе {check.Key.ToString()} более 4-ёх предметов!");
                }
            }
            
            int checkSettings = Convert.ToInt32(config.BalanceSettings.CONF_UseSR) + 
                                Convert.ToInt32(config.BalanceSettings.CONF_UseEconomics) + 
                                Convert.ToInt32(config.BalanceSettings.CONF_UseRustShop) + 
                                Convert.ToInt32(config.BalanceSettings.CONF_UseCustom) + 
                                Convert.ToInt32(config.BalanceSettings.CONF_UseNShop);
            
            if (checkSettings >= 2)
            {
                PrintError("Вы используете несколько балансов одновременно, проверьте настройки!");
                return;
            }

            if (checkSettings == 0)
            {
                PrintError("Вы не выбрали ни одной системы баланса в качестве используемой, проверьте настройки!");
                return;
            }

            if (config.BalanceSettings.CONF_UseSR && !ServerRewards)
            {
                PrintError("Плагин планировал использовать баланс от СерверРевардс, но плагин выгружен!");
                return;
            }

            if (config.BalanceSettings.CONF_UseEconomics && !Economics)
            {
                PrintError("Плагин планировал использовать баланс от Экономики, но плагин выгружен!");
                return;
            }

            if (config.BalanceSettings.CONF_UseRustShop && !RustShop)
            {
                PrintError("Плагин планировал использовать баланс от Экономики, но плагин выгружен!");
                return;
            }

            if (config.BalanceSettings.CONF_UseNShop && !NShop)
            {
                PrintError("Плагин планировал использовать баланс от Экономики, но плагин выгружен!");
                return;
            }

            if (config.BalanceSettings.CONF_UseCustom)
            {
                playerBalance = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>("CustomRespawn/Balance");
            }

            foreach (var check in config.ListOfItems
                                        .SelectMany(p => p.Value)
                                        .Where(p => p.Value.Permission != "")
                                        .Where(p => !permission.PermissionExists(p.Value.Permission, this)))
            {
                permission.RegisterPermission(check.Value.Permission, this);
            }

            timer.Every(1, () =>
            {
                foreach (var check in BasePlayer.activePlayerList.Where(p => p.IsDead()))
                    UI_UpdateRespawn(check, 0);
            });

            foreach (var check in config.ListOfItems.Where(p => !p.Value.ContainsKey("default")))
            {
                check.Value.Add("default", new CustomItem
                {
                    DisplayName = "ПУСТО",
                    ShortName = "",
                    Amount = 0,
                    SkinID = 0UL,
                    Price = 0,
                    PictureURL = "https://i.imgur.com/sAL1r8f.png"
                });
                
                PrintWarning($"Добавлена не найденная стандартная конфигурация для {check.Key.ToString()}");
            }
            
            SaveData();
            SaveConfig();
            Initialized = true;
            
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
        }

        private void Unload() => SaveData();

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("CustomRespawn/Balance", playerBalance);
            timer.Once(60, SaveData);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
            
            if (!playerPacks.ContainsKey(player.userID))
                playerPacks.Add(player.userID, config.GetDefaultPack());
            
            if (config.BalanceSettings.CONF_UseCustom && !playerBalance.ContainsKey(player.userID))
                playerBalance.Add(player.userID, config.BalanceSettings.CONF_CustomStartBalance);
            
            if (player.IsDead())
            {
                UI_DrawChoose(player, null);
            }
            
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config?.ListOfItems == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Hooks

        private void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (!Initialized || player.GetComponent<NPCPlayer>() != null || player.IsNpc || !player.IsConnected)
                return;

            if (!playerPacks.ContainsKey(player.userID))
            {
                PrintError("PLEASE! If u see this - send me screen! PLEASE!");
                PrintWarning($"OPD: {player.userID} -> {player.displayName}");
                OnPlayerInit(player);
                return;
            }
            
            if (playerPacks[player.userID] == null)
                playerPacks[player.userID] = config.GetDefaultPack();
            else
                playerPacks[player.userID] = config.GetDefaultPack();
            
            if (config.PluginSettings.CONF_ShowPermission == "" || permission.UserHasPermission(player.UserIDString, config.PluginSettings.CONF_ShowPermission))
            {
                if (info == null || info.Initiator == null || !(info.Initiator is BasePlayer) || info.Initiator.GetComponent<NPCPlayer>() != null || info.InitiatorPlayer == null || info.InitiatorPlayer.userID == player.userID || info.InitiatorPlayer.userID.ToString().Length != 17)
                {
                    UI_DrawChoose(player, null);
                    return;
                }
                
                UI_DrawChoose(player, info);
            }
        }
        
        private void OnPlayerRespawn(BasePlayer player)
        {
            if (!Initialized)
                return;
            CuiHelper.DestroyUi(player, Layer);
        }
        
        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerRespawned(player));
                return;
            }
            
            CuiHelper.DestroyUi(player, Layer);
            if (!Initialized)
                return;
            
            if (config.PluginSettings.CONF_ShowPermission == "" || permission.UserHasPermission(player.UserIDString, config.PluginSettings.CONF_ShowPermission))
            {
                if (!playerPacks.ContainsKey(player.userID))
                {
                    PrintError("PLEASE! If u see this - send me screen! PLEASE!");
                    PrintWarning($"OPR: {player.userID} -> {player.displayName}");
                    return;
                }
            
                if (playerPacks[player.userID] == null)
                    return;

                var playerInfo = playerPacks[player.userID];
                if (!config.PluginSettings.CONF_GiveAutoKit)
                {
                    player.inventory.Strip();
                }

                foreach (var check in playerInfo.CustomItems)
                    check.Value.CreateItem(player);
            }
        }
        
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (!Initialized)
                return null;
            
            if (arg.cmd.FullName.ToLower() == "global.respawn")
            {
                BasePlayer player = arg.Player();
                if (player != null)
                {
                    if (!playerPacks.ContainsKey(player.userID))
                    {
                        PrintError("PLEASE! If u see this - send me screen! PLEASE!");
                        PrintWarning($"OSC: {player.userID} -> {player.displayName}");
                        OnPlayerInit(player);
                    }
                    
                    playerPacks[player.userID] = config.GetDefaultPack();
                    if (player.IsDead())
                        player.Respawn();
                }
            }
            
            return null;
        }

        #endregion

        #region Commands

        [ConsoleCommand("cr.balance")]
        private void cmdManageBalance(ConsoleSystem.Arg args)
        {
            if (args.Player() != null || !args.HasArgs(1) || !config.BalanceSettings.CONF_UseCustom)
                return;

            ulong userId;
            if (!ulong.TryParse(args.Args[0], out userId))
            {
                args.ReplyWithObject($"cr.balance <userId> <amount>");
                return;
            }

            int amount;
            if (!int.TryParse(args.Args[1], out amount))
            {
                args.ReplyWithObject("cr.balance <userId> <amount>");
                return;
            }

            if (!playerBalance.ContainsKey(userId))
            {
                args.ReplyWithObject("No user with same ID!");
                return;
            }

            playerBalance[userId] += amount;
            args.ReplyWithObject($"We gave {amount} to {userId}");
        }
        
        [ConsoleCommand("UI_CR_Complect")]
        private void cmdConsoleComlect(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null || !args.HasArgs(1))
                return;

            if (!playerPacks.ContainsKey(player.userID))
            {
                PrintError("PLEASE! If u see this - send me screen! PLEASE!");
                PrintWarning($"CCC1: {player.userID} -> {player.displayName}");
                OnPlayerInit(player);
            }
            var playerInfo = playerPacks[player.userID];

            if (args.Args[0] == "default")
            {
                playerPacks[player.userID] = config.GetDefaultPack();
                UI_UpdateRespawn(player, 0);
                UI_UpdateRight(player, 0);
                return;
            }
            
            if (args.Args[0] == "customrespawn")
            {
                if (!player.IsDead())
                {
                    CuiHelper.DestroyUi(player, Layer);
                    return;
                }

                if (API_CheckBalance(player) < playerInfo.BuyPrice())
                    return;
                
                if (args.HasArgs(2))
                {
                    bool spawned = SleepingBag.SpawnPlayer(player, uint.Parse(args.Args[1]));
                    if (!spawned)
                    {
                        UI_UpdateRespawn(player, 0);
                        return;
                    }

                    if (playerInfo.BuyPrice() > 0)
                    {
                        API_TakeBalance(player, playerInfo.BuyPrice());
                        player.ChatMessage($"Вы потратили <color=#4286f4><b>{playerInfo.BuyPrice()} руб.</b></color> на экипировку!");
                    }
                    return;
                }

                if (playerInfo.BuyPrice() > 0)
                {
                    API_TakeBalance(player, playerInfo.BuyPrice());
                    player.ChatMessage($"Вы потратили <color=#4286f4><b>{playerInfo.BuyPrice()} руб.</b></color> на экипировку!");
                }
                player.Respawn();
                return;
            }

            if (!args.HasArgs(2))
                return;
            
            ItemType itemType;
            if (Enum.TryParse(args.Args[1], out itemType))
            {
                int currentBalance = 100;
                
                if (!config.ListOfItems.ContainsKey(itemType) || !config.ListOfItems[itemType].ContainsKey(args.Args[2]))
                {
                    PrintError("PLEASE! If u see this - send me screen! PLEASE!");
                    PrintWarning($"CCC2: {itemType} -> {args.Args[2]}");
                    return;
                }
                
                if (!permission.UserHasPermission(player.UserIDString, config.ListOfItems[itemType][args.Args[2]].Permission) && config.ListOfItems[itemType][args.Args[2]].Permission != "")
                    return;
                
                if (!playerInfo.CustomItems.ContainsKey(itemType))
                {
                    PrintError("PLEASE! If u see this - send me screen! PLEASE!");
                    PrintWarning($"CCC3: {player.userID} -> {itemType}");
                    return;
                }
                playerInfo.CustomItems[itemType] = config.ListOfItems[itemType][args.Args[2]];
                UI_UpdateRight(player, 1, 0);
                UI_UpdateRespawn(player);
            }
        }

        #endregion

        #region Interface

        private static string Layer = "UI_CR_Main";
        private void UI_DrawChoose(BasePlayer player, HitInfo info)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();
            if (!playerPacks.ContainsKey(player.userID))
            {
                OnPlayerInit(player);
                
                NextTick(() => UI_DrawChoose(player, info));
                return;
            }
            var playerInfo = playerPacks[player.userID];
            
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", Layer);

            if (config.DesignSetting.CONF_BackgroundImage != "")
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiRawImageComponent { FadeIn = 4f, Png = (string) ImageLibrary.Call("GetImage", "CR_MainBackground") },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-960 -540", OffsetMax = "960 540" }
                    }
                });
            }
            
            if (config.DesignSetting.CONF_ManImage != "")
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiRawImageComponent { FadeIn = 3f, Png = (string) ImageLibrary.Call("GetImage", "CR_MainMan") },
                        new CuiRectTransformComponent { AnchorMin = "0.4989583 0.5675924", AnchorMax = "0.4989583 0.5675924", OffsetMin = "-90 -228.5", OffsetMax = "90 228.5" }
                    }
                });
            }
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3796874 0.1818519", AnchorMax = "0.6203126 0.21", OffsetMax = "0 0" },
                Text = { FadeIn = 4f, Text = $"БАЛАНС: {API_CheckBalance(player)} РУБ.", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16 }
            }, Layer);

            if (config.PluginSettings.CONF_KillInfo.CONF_ShowKillerInfo && info != null)
            {
                UI_DrawKiller(player, container, info);
            }
            
            CuiHelper.AddUi(player, container);
            
            UI_UpdateRight(player, 4);
            UI_UpdateRespawn(player, 4);
        }

        private void UI_DrawKiller(BasePlayer player, CuiElementContainer container, HitInfo info)
        {
            BasePlayer killer = info.InitiatorPlayer;
            
            container.Add(new CuiElement
            {
                Name = Layer + ".KillHolder",
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#707070A6") },
                    new CuiRectTransformComponent { AnchorMin = "0.0177083 0.8092593", AnchorMax = "0.2947917 0.9648148", OffsetMax = "0 0" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer + ".KillHolder",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", killer.UserIDString) },
                    new CuiRectTransformComponent { AnchorMin = "0.01248179 0.03023276", AnchorMax = "0.306168 0.9604649", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiLabel
            {
                Text = { Text = $"<size=24><b>УБИЙЦА</b></size>\n" + killer.displayName.ToUpper(), Font = "robotocondensed-bold.ttf", FontSize = 30, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0.3120302 0", AnchorMax = "1 1", OffsetMax = "0 0" }
            }, Layer + ".KillHolder");
            
            container.Add(new CuiElement
            {
                Name = Layer + ".KillHolder.Weapon",
                Parent = Layer + ".KillHolder",
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#595959AC") },
                    new CuiRectTransformComponent { AnchorMin = "1 0", AnchorMax = "1.304285 1", OffsetMax = "0 0" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer + ".KillHolder.Weapon",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", info.Weapon.GetItem().info.shortname) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Sprite = "assets/content/ui/ui.background.tiletex.psd", Color = "0 0 0 0.3" },
                Text = { Text = info.ProjectileDistance.ToString("F0") + " M", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 40 }
            }, Layer + ".KillHolder.Weapon");

            float width = (float) 1.304285f / config.PluginSettings.CONF_KillInfo.CONF_KillerInfoButton.Count;
            foreach (var check in config.PluginSettings.CONF_KillInfo.CONF_KillerInfoButton.Select((i,t) => new { A = i, B = t }))
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{0 + width * check.B} -0.2", AnchorMax = $"{width + width * check.B} 0", OffsetMax = "0 0" },
                    Button = { Command = check.A.Command.Replace("%STEAMID%", killer.UserIDString), Sprite = "assets/content/ui/ui.background.tiletex.psd", Color = HexToRustFormat(check.A.Color) },
                    Text = { Text = check.A.DisplayName, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 14 }
                }, Layer + ".KillHolder");
            }
        }
 
        private void UI_UpdateRight(BasePlayer player, int fadeIn = 0, int textFadeIn = 4)
        {
            if (!playerPacks.ContainsKey(player.userID))
            {
                OnPlayerInit(player);
                
                NextTick(() => UI_UpdateRight(player));
                return;
            }
            CuiElementContainer container = new CuiElementContainer();
            var playerInfo = playerPacks[player.userID];
            
            foreach (var category in config.ListOfItems.Select((i,t) => new { A = i, B = t }))
            {
                CuiHelper.DestroyUi(player, Layer + $".{category.A.Key}");
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0.6394791 {0.875557 - category.B * 0.2}", AnchorMax = $"0.956041 {0.945 - category.B * 0.2}", OffsetMax = "0 0" },
                    Text = { FadeIn = textFadeIn, Text = $"ВЫБЕРИТЕ {category.A.Key.ToString().Replace("_", " ")}", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 18 }
                }, Layer, Layer + $".{category.A.Key}");

                foreach (var item in category.A.Value.Select((i,t) => new { A = i, B = t }))
                {
                    CuiHelper.DestroyUi(player, Layer + $".{category.A.Key}.{item.A.Key}");
                    string color = playerInfo.CustomItems[category.A.Key].DisplayName == item.A.Value.DisplayName  ? "#5F8C58FF" : "#707070A6";
                    if (!permission.UserHasPermission(player.UserIDString, item.A.Value.Permission) && item.A.Value.Permission  != "")
                        color = "#ccbd3fFF";
                    
                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = $"{0.6394791 + item.B * 0.083} {0.7416681 - category.B * 0.2}", AnchorMax = $"{0.7176041 + item.B * 0.083} {0.880557 - category.B * 0.2}"},
                        Button = { FadeIn = fadeIn, Color = HexToRustFormat(color), Command = $"UI_CR_Complect switch main_weapon {item.A.Key}"},
                        Text = { Text = "" }
                    }, Layer, Layer + $".{category.A.Key}.{item.A.Key}");

                    if (item.A.Value.ShortName != "")
                    {
                        container.Add(new CuiElement
                        {
                            Parent = Layer + $".{category.A.Key}.{item.A.Key}",
                            Components =
                            {
                                new CuiRawImageComponent { FadeIn = fadeIn, Png = (string) ImageLibrary.Call("GetImage", item.A.Value.ShortName) },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                            }
                        });
                    }

                    string niceText = playerInfo.CustomItems[category.A.Key].DisplayName == item.A.Value.DisplayName
                        ? "ВЫБРАНО\n" +
                          $"{item.A.Value.Price} РУБ."
                        : $"ВЫБРАТЬ\n" +
                          $"{item.A.Value.Price} РУБ.";
                    if (!permission.UserHasPermission(player.UserIDString, item.A.Value.Permission) && item.A.Value.Permission  != "")
                        niceText = "ТОЛЬКО ДЛЯ\n<b>VIP</b>";

                    string colorBlur = "0 0 0 0.3";
                    if (playerInfo.CustomItems[category.A.Key].DisplayName == item.A.Value.DisplayName)
                        colorBlur = "0 0 0 0";
                    container.Add(new CuiButton
                    { 
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { FadeIn = fadeIn, Sprite = "assets/content/ui/ui.background.tiletex.psd", Material = "assets/content/ui/uibackgroundblur.mat", Color = colorBlur, Command = $"UI_CR_Complect switch {category.A.Key} {item.A.Key}"},
                        Text = { FadeIn = fadeIn, Text = niceText, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf"}
                    }, Layer + $".{category.A.Key}.{item.A.Key}");
                }
            }
            
            CuiHelper.AddUi(player, container);
        }

        private void UI_UpdateRespawn(BasePlayer player, int fadeIn = 0)
        {
            if (!playerPacks.ContainsKey(player.userID))
            {
                OnPlayerInit(player);
                
                NextTick(() => UI_UpdateRespawn(player));
                return;
            }
            CuiElementContainer container = new CuiElementContainer();
            var playerInfo = playerPacks[player.userID];
            int finalPrice = playerInfo.BuyPrice();
            int currentBalance = (int) API_CheckBalance(player);

            CuiHelper.DestroyUi(player, Layer + ".Default");
            string btnText = currentBalance >= finalPrice ? "ПОЯВИТЬСЯ НА ПЛЯЖЕ" : "НЕДОСТАТОЧНО СРЕДСТВ";
            
            CuiHelper.DestroyUi(player, Layer + $".MainRespawnButton");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3796874 0.0888881", AnchorMax = "0.6203126 0.1518519", OffsetMax = "0 0" },
                Button = { FadeIn = fadeIn, Color = HexToRustFormat("#707070A6"), Material = "assets/content/ui/ui.background.transparent.radial.psd", Command = "UI_CR_Complect customrespawn" },
                Text = { FadeIn = fadeIn, Text = btnText, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 26 }
            }, Layer, Layer + ".MainRespawnButton");
            
            CuiHelper.DestroyUi(player, Layer + $".MainPriceText");
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3796874 0.1518519", AnchorMax = "0.6203126 0.1918519", OffsetMax = "0 0" },
                Text = { FadeIn = fadeIn, Text = $"ЦЕНА ЭКИПИРОВКИ: <b>{finalPrice} РУБ.</b>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 18 }
            }, Layer, Layer + ".MainPriceText");

            if (currentBalance < finalPrice)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.0094791 0.0188881", AnchorMax = $"0.237041 0.0818519", OffsetMax = "0 0" },
                    Button = { Color = HexToRustFormat("#8C5858FF"), Material = "assets/content/ui/ui.background.transparent.radial.psd", Command = "UI_CR_Complect default" },
                    Text = { Text = $"ВЕРНУТЬ К СТАНДАРТНЫМ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20 }
                }, Layer, Layer + $".Default");
            }

            var foundBags = SleepingBag.FindForPlayer(player.userID, true);

            var homeWidth = 0.1;
            var homeMargin = 0.002;
            
            var leftPosition = 0.5f - (float) foundBags.Length / 2 * homeWidth - ((float) foundBags.Length - 1) / 2 * homeMargin;
            
            for (int i = 0; i < foundBags.Length; i++)
            {
                CuiHelper.DestroyUi(player, Layer + $".Zone.{i}");
                var cBag = foundBags.ElementAt(i);

                string textCd = cBag.unlockSeconds > 0 ? $"CD: {(int) cBag.unlockSeconds}S\n{cBag.niceName}" : $"RESPAWN\n{cBag.niceName}";
                if (currentBalance < finalPrice)
                    textCd = "НЕТ ДЕНЕГ";
                
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{leftPosition + i * homeWidth + i * homeMargin} 0.0188881", AnchorMax = $"{leftPosition + homeWidth + i * homeWidth + i * homeMargin} 0.0818519", OffsetMax = "0 0" },
                    Button = { FadeIn = fadeIn, Color = HexToRustFormat("#707070A6"), Material = "assets/content/ui/ui.background.transparent.radial.psd", Command = $"UI_CR_Complect customrespawn {foundBags.ElementAt(i).net.ID}" },
                    Text = { FadeIn = fadeIn, Text = textCd, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16 }
                }, Layer, Layer + $".Zone.{i}");
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Utils

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

        [JsonProperty("Цвета для обозначения рекдости предмета")]
        public static List<string> RareType = new List<string>
        {
            "#2387a0",
            "#8b23a0",
            "#ba3535",
            "#eacf48"
        };
        
        private static Dictionary<ulong, int> playerBalance = new Dictionary<ulong, int>();
        
        private static string GetColor(string text, int rarity) => $"<color={RareType[rarity]}>{text}</color>";

        #endregion

        #region API

        [PluginReference] private Plugin Economics, ServerRewards, RustShop, NShop;
        
        private int API_CheckBalance(BasePlayer player)
        {
            if (config.BalanceSettings.CONF_UseEconomics)
            {
                double result = (double) Economics.Call("Balance", player.userID);
                return (int) result;
            }
            if (config.BalanceSettings.CONF_UseSR)
            {
                object result = (object) ServerRewards.Call("CheckPoints", player.userID);

                return Convert.ToInt32(result);
            }
            if (config.BalanceSettings.CONF_UseRustShop)
            {
                return (int) RustShop.Call("GetBalance", player.userID);
            }
            if (config.BalanceSettings.CONF_UseNShop)
            {
                return (int) ((float) NShop.Call("Balance", player.userID));
            }

            if (config.BalanceSettings.CONF_UseCustom)
            {
                return playerBalance[player.userID];
            }

            return 0;
        }
        
        private void API_TakeBalance(BasePlayer player, int amount)
        {
            if (config.BalanceSettings.CONF_UseEconomics)
            {
                Economics.Call("Withdraw", player.userID, (double) amount);
                return;
            }
            if (config.BalanceSettings.CONF_UseSR)
            {
                ServerRewards.Call("TakePoints", player.userID, amount);
                return;
            }
            if (config.BalanceSettings.CONF_UseRustShop)
            {
                RustShop.Call("RemoveBalance", player.userID, amount);
            }
            if (config.BalanceSettings.CONF_UseNShop)
            {
                NShop.Call("ChangeBalance", player.userID, amount);
            }

            if (config.BalanceSettings.CONF_UseCustom)
            {
                playerBalance[player.userID] -= amount;
            }

            return;
        }

        #endregion
    }
}