using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Collections;

namespace Oxide.Plugins
{
    [Info("IQDailyStep", "SkuliDropek", "0.0.5")]
    [Description("Ежедневные награды")]
    class IQDailyStep : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin IQChat, IQEconomic, IQCases, ImageLibrary, RustStore;
        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.GeneralSetting.ChatSettings;
            if (IQChat)
                if (Chat.UIAlertUse)
                    IQChat?.Call("API_ALERT_PLAYER_UI", player, Message);
                else IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public void AddAllImage()
        {
            var Images = config.InterfaceSetting;
            if (string.IsNullOrEmpty(Images.BackgroundURL))
                PrintError("Не найдена ссылка на задний фон!");
            if (!ImageLibrary)
            {
                PrintError("Не установлен плагин ImageLibrary!");
                return;
            }
            AddImage(Images.BackgroundURL, "BACKGROUND_IMG");
            for (int Day = 0; Day < config.Days.Count; Day++)
            {
                AddImage(config.Days[Day].ImageLink, $"DAY_{Day}");
                AddImage(config.Days[Day].ImageLinkAccess, $"DAY_ACCESS_{Day}");
                for (int Item = 0; Item < config.Days[Day].Rewards.Count; Item++)
                {
                    var Reward = config.Days[Day].Rewards[Item];
                    if (Reward.UseCommand)
                        AddImage(Reward.CommandSettings.CommandPNG, $"DAY_{Day}_COMMAND_{Item}");
                    if (Reward.UseItem)
                        AddImage(Reward.PNGItemRust, $"DAY_{Day}_ITEM_{Item}");
                    if (Reward.UseStores)
                        AddImage(Reward.PNGStoreBalance, $"DAY_{Day}_BALANCE_{Item}");
                }
            }
            Puts("Изображения загружены успешно");
        }
        public string GetImageReward(int Day, int ItemIndex)
        {
            var Item = config.Days[Day].Rewards[ItemIndex];
            if (Item.UseCommand)
                return $"DAY_{Day}_COMMAND_{ItemIndex}";
            if (Item.UseItem)
                return $"DAY_{Day}_ITEM_{ItemIndex}";
            if (Item.UseStores)
                return $"DAY_{Day}_BALANCE_{ItemIndex}";
            if (Item.UseIQCases)
                if (IQCASE_EXIST(Item.IQCasesSettings.KeyCase))
                    return Item.IQCasesSettings.KeyCase;
            if (Item.UseIQEconomic)
                return IQEconomicGetIL();
            return "null";
        }
        string IQEconomicGetIL() { 
            return (string)IQEconomic?.Call("API_GET_MONEY_IL"); }
        void IQCasesGiveCase(ulong userID, string CaseKey, int Amount)
        {
            if (!IQCases)
            {
                PrintError("У вас установлена выдача кейса IQCases,но не установлен плагин!\nВыдача награды будетн неккоректна!");
                return;
            }
            if (!IQCASE_EXIST(CaseKey))
            {
                PrintError($"{CaseKey} - данного кейса не найдено в IQCases.Проверьте введенные вами данные в плагине!");
                return;
            }
            IQCases?.Call("API_GIVE_CASE", userID, CaseKey, Amount);
        }
        bool IQCASE_EXIST(string CaseKey)
        {
            if (!(bool)IQCases?.Call("API_IS_CASE_EXIST", CaseKey))
                return false;
            else 
                return true;
        }
        void IQEconomicBalanceSet(ulong userID,int Balance)
        {
            if(!IQEconomic)
            {
                PrintError("У вас установлена выдача баланса IQEconomic,но не установлен плагин!\nВыдача награды будетн неккоректна!");
                return;
            }
            IQEconomic?.Call("API_SET_BALANCE", userID, Balance);
        }
        public void MoscovOVHBalanceSet(ulong userID, int Balance)
        {
            if (!RustStore)
            {
                PrintWarning("У вас не установлен магазин MoscovOVH");
                return;
            }
            plugins.Find("RustStore").CallHook("APIChangeUserBalance", userID, Balance, new Action<string>((result) =>
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (result == "SUCCESS") return;
                Puts($"Пользователь {userID} не авторизован в магазине");
            }));
        }
        public void GameStoresBalanceSet(ulong userID, int Balance)
        {
            var GameStores = config.GeneralSetting.GameStoresSettings;
            if (String.IsNullOrEmpty(GameStores.GameStoresAPIStore) || String.IsNullOrEmpty(GameStores.GameStoresIDStore))
            {
                PrintWarning("Магазин GameStores не настроен! Невозможно выдать баланс пользователю");
                return;
            }
            webrequest.Enqueue($"https://gamestores.ru/api?shop_id={GameStores.GameStoresIDStore}&secret={GameStores.GameStoresAPIStore}&action=moneys&type=plus&steam_id={userID}&amount={Balance}&mess={GameStores.GameStoresMessage}", null, (i, s) =>
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (i != 200) { }
                if (s.Contains("success")) return;
                if (s.Contains("fail"))
                {
                    Puts($"Пользователь {userID} не авторизован в магазине");
                    return;
                }
            }, this);
        }

        #endregion

        #region Configuration
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка дней")]
            public List<DaySetting> Days = new List<DaySetting>();
            [JsonProperty("Настройка плагина")]
            public GeneraldSettings GeneralSetting = new GeneraldSettings();
            [JsonProperty("Настройка интерфейса")]
            public InterfaceSettings InterfaceSetting = new InterfaceSettings();

            #region GeneralSetting
            public class GeneraldSettings
            {
                [JsonProperty("MoscovOVH : Включить использование магазина(Должен быть включен обмен валют)")]
                public bool MoscovOvhUse;
                [JsonProperty("GameStores : Включить использование магазина(Должен быть включен обмен валют)")]
                public bool GameStoreshUse;
                [JsonProperty("GameStores : Настройки магазина GameStores")]
                public GameStores GameStoresSettings = new GameStores();
                [JsonProperty("IQChat : Настройки чата")]
                public ChatSetting ChatSettings = new ChatSetting();
                internal class ChatSetting
                {
                    [JsonProperty("IQChat : Кастомный префикс в чате")]
                    public string CustomPrefix;
                    [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
                    public string CustomAvatar;
                    [JsonProperty("IQChat : Использовать UI уведомления")]
                    public bool UIAlertUse;
                }
                internal class GameStores
                {
                    [JsonProperty("API Магазина(GameStores)")]
                    public string GameStoresAPIStore;
                    [JsonProperty("ID Магазина(GameStores)")]
                    public string GameStoresIDStore;
                    [JsonProperty("Сообщение в магазин при выдаче баланса(GameStores)")]
                    public string GameStoresMessage;
                }
            }

            #endregion

            #region DaysSettings
            internal class DaySetting
            {
                [JsonProperty("Настройка награды")]
                public List<RewardSetting> Rewards = new List<RewardSetting>();
                [JsonProperty("Ссылка на картинку")]
                public string ImageLink;
                [JsonProperty("Ссылка на картинку в случае,если награда взята(Галочка)")]
                public string ImageLinkAccess;
                [JsonProperty("День")]
                public string Day;
                internal class RewardSetting
                {
                    [JsonProperty("Использовать предметы RUST'a")]
                    public bool UseItem;
                    [JsonProperty("Использовать команды")]
                    public bool UseCommand;
                    [JsonProperty("Использовать баланс на магазин")]
                    public bool UseStores;
                    [JsonProperty("Использовать валюты с IQEconomic.Картинка будет подставляться с плагина сама(Если есть плагин IQEconomic)")]
                    public bool UseIQEconomic;
                    [JsonProperty("Использовать кейсы с IQCases.Картинка будет подставляться с плагина сама(Если есть)")]
                    public bool UseIQCases;
                    [JsonProperty("Настройка предметов RUST'a")]
                    public List<ItemSetting> ItemSettings = new List<ItemSetting>();
                    [JsonProperty("Ссылка на картинку для набора предметов RUST'a")]
                    public string PNGItemRust;
                    [JsonProperty("Сколько баланса выдавать в магазин")]
                    public int CountMoneyStore;
                    [JsonProperty("Ссылка на картинку для иконки баланса с магазина")]
                    public string PNGStoreBalance;
                    [JsonProperty("Настройка IQCases(Если такой плагин имеется)")]
                    public IQCasesSetting IQCasesSettings = new IQCasesSetting();
                    [JsonProperty("Сколько монет выдавать IQEconomic(Если такой плагин имеется)")]
                    public int AmountEconomic;
                    [JsonProperty("Настройка команды")]
                    public CommandSetting CommandSettings = new CommandSetting();
                    internal class ItemSetting
                    {
                        [JsonProperty("DisplayName (Если требуется для кастомного предмета,в ином случае оставляйте пустым)")]
                        public string DisplayName;
                        [JsonProperty("Shortname")]
                        public string Shortname;
                        [JsonProperty("SkinID")]
                        public ulong SkinID;
                        [JsonProperty("Количество")]
                        public int Amount;
                    }
                    internal class IQCasesSetting
                    {
                        [JsonProperty("Ключ от кейса(к примеру freecase)")]
                        public string KeyCase;
                        [JsonProperty("Количество")]
                        public int Amount;
                    }
                    internal class CommandSetting
                    {
                        [JsonProperty("Команда")]
                        public string Command;
                        [JsonProperty("Картинка для команды")]
                        public string CommandPNG;
                    }
                }
            }
            #endregion

            #region InterfaceSettings
            internal class InterfaceSettings
            {
                [JsonProperty("Ссылка на задний фон")]
                public string BackgroundURL;
                [JsonProperty("Включить задний фон")]
                public bool UseBackground;
            }
            #endregion

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    #region GeneralSettings
                    GeneralSetting = new GeneraldSettings
                    {
                        GameStoreshUse = false,
                        MoscovOvhUse = true,
                        GameStoresSettings = new GeneraldSettings.GameStores
                        {
                            GameStoresAPIStore = "",
                            GameStoresIDStore = "",
                            GameStoresMessage = "",
                        },
                        ChatSettings = new GeneraldSettings.ChatSetting
                        {
                            UIAlertUse = false,
                            CustomAvatar = "",
                            CustomPrefix = ""
                        }
                    },
                    #endregion

                    #region RewardSettings

                    Days = new List<DaySetting>
                    {
                        new DaySetting
                        {
                            Day = "1 ДЕНЬ",
                            ImageLinkAccess = "https://i.imgur.com/8A7OQ4X.png",
                            ImageLink = "https://i.imgur.com/6zWWX2N.png",
                            Rewards = new List<DaySetting.RewardSetting>
                            {
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = true,
                                    UseItem = false,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = false,
                                    UseStores = false,
                                    UseIQCases = false,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {

                                    }
                                },
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = false,
                                    UseItem = true,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = false,
                                    UseStores = false,
                                    UseIQCases = false,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {
                                       new DaySetting.RewardSetting.ItemSetting
                                       {
                                           DisplayName = "",
                                           Shortname = "rifle.ak",
                                           Amount = 1,
                                           SkinID = 0,
                                       },
                                        new DaySetting.RewardSetting.ItemSetting
                                        {
                                            DisplayName = "",
                                            Shortname = "scrap",
                                            Amount = 100,
                                            SkinID = 0,
                                        },
                                         new DaySetting.RewardSetting.ItemSetting
                                         {
                                             DisplayName = "Калаш убийца",
                                             Shortname = "rifle.ak",
                                             Amount = 1,
                                             SkinID = 1337,
                                         },
                                    }
                                },
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = false,
                                    UseItem = false,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = true,
                                    UseStores = false,
                                    UseIQCases = false,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {

                                    }
                                },
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = false,
                                    UseItem = false,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = false,
                                    UseStores = false,
                                    UseIQCases = true,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {

                                    }
                                }
                            }
                        },
                        new DaySetting
                        {
                            Day = "2 ДЕНЬ",
                            ImageLink = "https://i.imgur.com/OqPXvRh.png",
                            ImageLinkAccess = "https://i.imgur.com/66fsczj.png",
                            Rewards = new List<DaySetting.RewardSetting>
                            {
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = true,
                                    UseItem = false,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = false,
                                    UseStores = true,
                                    UseIQCases = false,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {
                                       new DaySetting.RewardSetting.ItemSetting
                                       {
                                           DisplayName = "",
                                           Shortname = "rifle.ak",
                                           Amount = 1,
                                           SkinID = 0,
                                       },
                                        new DaySetting.RewardSetting.ItemSetting
                                        {
                                            DisplayName = "",
                                            Shortname = "scrap",
                                            Amount = 100,
                                            SkinID = 0,
                                        },
                                         new DaySetting.RewardSetting.ItemSetting
                                         {
                                             DisplayName = "Калаш убийца",
                                             Shortname = "rifle.ak",
                                             Amount = 1,
                                             SkinID = 1337,
                                         },
                                    }
                                }
                            }
                        },
                        new DaySetting
                        {
                            Day = "3 ДЕНЬ",
                            ImageLink = "https://i.imgur.com/DJR1xPh.png",
                            ImageLinkAccess = "https://i.imgur.com/mCXhiap.png",
                            Rewards = new List<DaySetting.RewardSetting>
                            {
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = true,
                                    UseItem = true,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = true,
                                    UseStores = true,
                                    UseIQCases = true,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {
                                       new DaySetting.RewardSetting.ItemSetting
                                       {
                                           DisplayName = "",
                                           Shortname = "rifle.ak",
                                           Amount = 1,
                                           SkinID = 0,
                                       },
                                        new DaySetting.RewardSetting.ItemSetting
                                        {
                                            DisplayName = "",
                                            Shortname = "scrap",
                                            Amount = 100,
                                            SkinID = 0,
                                        },
                                         new DaySetting.RewardSetting.ItemSetting
                                         {
                                             DisplayName = "Калаш убийца",
                                             Shortname = "rifle.ak",
                                             Amount = 1,
                                             SkinID = 1337,
                                         },
                                    }
                                }
                            }
                        },
                        new DaySetting
                        {
                            Day = "4 ДЕНЬ",
                            ImageLink = "https://i.imgur.com/p2Xgdog.png",
                            ImageLinkAccess = "https://i.imgur.com/ffdLwcr.png",
                            Rewards = new List<DaySetting.RewardSetting>
                            {
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = true,
                                    UseItem = true,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = true,
                                    UseStores = true,
                                    UseIQCases = true,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {
                                       new DaySetting.RewardSetting.ItemSetting
                                       {
                                           DisplayName = "",
                                           Shortname = "rifle.ak",
                                           Amount = 1,
                                           SkinID = 0,
                                       },
                                        new DaySetting.RewardSetting.ItemSetting
                                        {
                                            DisplayName = "",
                                            Shortname = "scrap",
                                            Amount = 100,
                                            SkinID = 0,
                                        },
                                         new DaySetting.RewardSetting.ItemSetting
                                         {
                                             DisplayName = "Калаш убийца",
                                             Shortname = "rifle.ak",
                                             Amount = 1,
                                             SkinID = 1337,
                                         },
                                    }
                                }
                            }
                        },
                        new DaySetting
                        {
                            Day = "5 ДЕНЬ",
                            ImageLink = "https://i.imgur.com/XO3xVoZ.png",
                            ImageLinkAccess = "https://i.imgur.com/Kp90WoJ.png",
                            Rewards = new List<DaySetting.RewardSetting>
                            {
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = true,
                                    UseItem = true,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = true,
                                    UseStores = true,
                                    UseIQCases = true,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {
                                       new DaySetting.RewardSetting.ItemSetting
                                       {
                                           DisplayName = "",
                                           Shortname = "rifle.ak",
                                           Amount = 1,
                                           SkinID = 0,
                                       },
                                        new DaySetting.RewardSetting.ItemSetting
                                        {
                                            DisplayName = "",
                                            Shortname = "scrap",
                                            Amount = 100,
                                            SkinID = 0,
                                        },
                                         new DaySetting.RewardSetting.ItemSetting
                                         {
                                             DisplayName = "Калаш убийца",
                                             Shortname = "rifle.ak",
                                             Amount = 1,
                                             SkinID = 1337,
                                         },
                                    }
                                }
                            }
                        },
                        new DaySetting
                        {
                            Day = "6 ДЕНЬ",
                            ImageLink = "https://i.imgur.com/L9GrCmr.png",
                            ImageLinkAccess = "https://i.imgur.com/t9kh9No.png",
                            Rewards = new List<DaySetting.RewardSetting>
                            {
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = true,
                                    UseItem = true,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = true,
                                    UseStores = true,
                                    UseIQCases = true,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {
                                       new DaySetting.RewardSetting.ItemSetting
                                       {
                                           DisplayName = "",
                                           Shortname = "rifle.ak",
                                           Amount = 1,
                                           SkinID = 0,
                                       },
                                        new DaySetting.RewardSetting.ItemSetting
                                        {
                                            DisplayName = "",
                                            Shortname = "scrap",
                                            Amount = 100,
                                            SkinID = 0,
                                        },
                                         new DaySetting.RewardSetting.ItemSetting
                                         {
                                             DisplayName = "Калаш убийца",
                                             Shortname = "rifle.ak",
                                             Amount = 1,
                                             SkinID = 1337,
                                         },
                                    }
                                }
                            }
                        },
                        new DaySetting
                        {
                            Day = "7 ДЕНЬ",
                            ImageLink = "https://i.imgur.com/iehQzIo.png",
                            ImageLinkAccess = "https://i.imgur.com/LoV71NL.png",
                            Rewards = new List<DaySetting.RewardSetting>
                            {
                                new DaySetting.RewardSetting
                                {
                                    UseCommand = true,
                                    UseItem = false,
                                    PNGItemRust = "https://i.imgur.com/4valKsh.png",
                                    PNGStoreBalance = "https://i.imgur.com/Pe2VmPZ.png",
                                    UseIQEconomic = false,
                                    UseStores = false,
                                    UseIQCases = false,
                                    AmountEconomic = 10,
                                    CountMoneyStore = 10,
                                    CommandSettings = new DaySetting.RewardSetting.CommandSetting
                                    {
                                        Command = "say 123 %STEAMID%",
                                        CommandPNG = "https://i.imgur.com/n6Pyc5F.png",
                                    },
                                    IQCasesSettings = new DaySetting.RewardSetting.IQCasesSetting
                                    {
                                        KeyCase = "freecase",
                                        Amount = 1,
                                    },
                                    ItemSettings = new List<DaySetting.RewardSetting.ItemSetting>
                                    {
                                       new DaySetting.RewardSetting.ItemSetting
                                       {
                                           DisplayName = "",
                                           Shortname = "rifle.ak",
                                           Amount = 1,
                                           SkinID = 0,
                                       },
                                        new DaySetting.RewardSetting.ItemSetting
                                        {
                                            DisplayName = "",
                                            Shortname = "scrap",
                                            Amount = 100,
                                            SkinID = 0,
                                        },
                                         new DaySetting.RewardSetting.ItemSetting
                                         {
                                             DisplayName = "Калаш убийца",
                                             Shortname = "rifle.ak",
                                             Amount = 1,
                                             SkinID = 1337,
                                         },
                                    }
                                }
                            }
                        },
                    },

                    #endregion
                        
                    #region InterfaceSetting
                    InterfaceSetting = new InterfaceSettings
                    {
                        BackgroundURL = "https://i.imgur.com/VfYWeo4.png",
                        UseBackground = true,
                    }
                    #endregion
                };
            }
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
                PrintWarning($"Ошибка чтения #57 конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Data
        [JsonProperty("Дата с днями игроков")]
        public Dictionary<ulong, Dictionary<int, bool>> DataPlayer = new Dictionary<ulong, Dictionary<int, bool>>();

        void ReadData()
        {
            DataPlayer = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<int, bool>>>("IQDailyStep/IQUser");
        }
        void WriteData() {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQDailyStep/IQUser", DataPlayer);
        }
        void RegisteredDataUser(BasePlayer player)
        {
            if (!DataPlayer.ContainsKey(player.userID))
                DataPlayer.Add(player.userID, new Dictionary<int, bool>
                {
                    [DateTime.Now.Day] = false,
                    [DateTime.Now.AddDays(1).Day] = false,
                    [DateTime.Now.AddDays(2).Day] = false,
                    [DateTime.Now.AddDays(3).Day] = false,
                    [DateTime.Now.AddDays(4).Day] = false,
                    [DateTime.Now.AddDays(5).Day] = false,
                    [DateTime.Now.AddDays(6).Day] = false,
                });
            else
            {
                if (!DataPlayer[player.userID].ContainsKey(DateTime.Now.Day))
                {
                    DataPlayer.Remove(player.userID);
                    DataPlayer.Add(player.userID, new Dictionary<int, bool>
                    {
                        [DateTime.Now.Day] = false,
                        [DateTime.Now.AddDays(1).Day] = false,
                        [DateTime.Now.AddDays(2).Day] = false,
                        [DateTime.Now.AddDays(3).Day] = false,
                        [DateTime.Now.AddDays(4).Day] = false,
                        [DateTime.Now.AddDays(5).Day] = false,
                        [DateTime.Now.AddDays(6).Day] = false,
                    });
                    Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQDailyStep/IQUser", DataPlayer);

                    return;
                }

                if (DataPlayer[player.userID].Count(x => x.Value == true) >= 1)
                {
                    int BackDay = DateTime.Now.Day - 1;
                    if (DataPlayer[player.userID].ContainsKey(BackDay))// ?????????????????
                        if (DataPlayer[player.userID][BackDay] == false)
                        {
                            DataPlayer.Remove(player.userID);
                            return;
                        }
                }

                if (DataPlayer[player.userID][DateTime.Now.Day])
                    return;
            }
        }

        #endregion

        #region Command

        [ChatCommand("day")]
        void DayilyMenuOpen(BasePlayer player)
        {
            Interface_Daily(player);
        }

        List<ulong> StopSpam = new List<ulong>();
        [ConsoleCommand("daily")]
        void ConsoleCommandDayTake(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            switch(arg.Args[0].ToLower())
            {
                case "take":
                    {
                        if (StopSpam.Contains(player.userID)) return;
                        int Day = Convert.ToInt32(arg.Args[1]);
                        int ThisDay = Convert.ToInt32(arg.Args[2]);
                        Interface_Days_Take(player, Day, ThisDay);
                        StopSpam.Add(player.userID);
                        break;
                    }
            }
        }

        #endregion
            
        #region Hooks
        void OnPlayerConnected(BasePlayer player)
        {
            RegisteredDataUser(player);
            Interface_Daily(player);
        }

        private void OnServerInitialized()
        {
            ReadData();
            //foreach (var p in BasePlayer.activePlayerList)
            //    OnPlayerConnected(p);
            WriteData();
            AddAllImage();
        }

        void Unload()
        {
            foreach (var p in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(p, UI_MAIN_UI_STEP);

            UI_MAIN_UI_STEP = null;
            UI_MAIN_UI_STEP_TAKE = null;
            WriteData();
        }
        #endregion

        #region Metods

        void GiveReward(BasePlayer player,int Day)
        {
            var DaysReward = config.Days[Day].Rewards;
            for(int i = 0; i < DaysReward.Count; i++)
            {
                var Reward = DaysReward[i];

                if(Reward.UseCommand)
                    rust.RunServerCommand(Reward.CommandSettings.Command.Replace("%STEAMID%", player.UserIDString));
                if(Reward.UseItem)
                {
                    for(int j = 0; j < Reward.ItemSettings.Count; j++)
                    {
                        var RewardItem = Reward.ItemSettings[j];
                        var ItemReward = ItemManager.CreateByName(RewardItem.Shortname, RewardItem.Amount, RewardItem.SkinID);
                        if (!string.IsNullOrEmpty(RewardItem.DisplayName))
                            ItemReward.name = RewardItem.DisplayName;
                        player.GiveItem(ItemReward);
                    }
                }
                if (Reward.UseIQEconomic)
                    IQEconomicBalanceSet(player.userID, Reward.AmountEconomic);
                if(Reward.UseIQCases)
                {
                    var IQCases = Reward.IQCasesSettings;
                    IQCasesGiveCase(player.userID, IQCases.KeyCase, IQCases.Amount);
                }
                if (Reward.UseStores)
                {
                    var General = config.GeneralSetting;
                    if(General.GameStoreshUse)
                        GameStoresBalanceSet(player.userID, Reward.CountMoneyStore);
                    if(General.MoscovOvhUse)
                        MoscovOVHBalanceSet(player.userID, Reward.CountMoneyStore);
                }
            }
        }
        #endregion

        #region UI
        public static string UI_MAIN_UI_STEP = "UI_MAIN_UI_STEP";
        public static string UI_MAIN_UI_STEP_TAKE = "UI_MAIN_UI_STEP_TAKE";

        #region InterfaceDaily
        void Interface_Daily(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_MAIN_UI_STEP);
            if(!DataPlayer.ContainsKey(player.userID))
            {
                RegisteredDataUser(player);
                return;
            }

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                FadeOut = 0.15f,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = "0 0 0 0" }
            }, "Overlay", UI_MAIN_UI_STEP);

            if (config.InterfaceSetting.UseBackground)
            {
                container.Add(new CuiElement
                {
                    Parent = UI_MAIN_UI_STEP,
                    Components =
                {
                    new CuiRawImageComponent { Png = GetImage("BACKGROUND_IMG") },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = $"1 1"},
                }
                });
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Button = { Close = UI_MAIN_UI_STEP, Color = "0 0 0 0" },
                Text = { Text = "" }
            },  UI_MAIN_UI_STEP);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.90371398", AnchorMax = "1 1" }, //
                Text = { Text = lang.GetMessage("UI_TITLE", this, player.UserIDString), Color = HexToRustFormat("#FFFFFF99"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_MAIN_UI_STEP);

            container.Add(new CuiLabel  
            {
                RectTransform = { AnchorMin = "0 0.8824", AnchorMax = "1 0.9251398" },//
                Text = { Text = lang.GetMessage("UI_DESCRIPTION", this, player.UserIDString), Color = HexToRustFormat("#FFFFFF99"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_MAIN_UI_STEP);

            #region CenterFunc

            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.413646f - 0.18f; /// Ширина
            float itemMargin = 0.409895f - 0.41f; /// Расстояние между 
            int itemCount = 7;
            float itemMinHeight = 0.45f; // Сдвиг по вертикали
            float itemHeight = 0.44f; /// Высота
            int ItemTarget = 4;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            #endregion

            int Day = 0;
            //for(int Day = 0; Day < 7; Day++)
            foreach(var Data in DataPlayer[player.userID])
            {
                //       var Data = DataPlayer[player.userID].ElementAt(Day);
                //if (Data == null) continue;
                int ThisDay = DateTime.Now.Day;

                string HEX = ThisDay == Data.Key ? "#FFFFFFFF" : "#FFFFFF99";

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = {  Color = "0 0 0 0" }
                },  UI_MAIN_UI_STEP , $"DAY_{Day}");

                container.Add(new CuiElement
                {
                    Parent = $"DAY_{Day}",
                    Components =
                        {       
                        new CuiRawImageComponent { Png = GetImage($"DAY_{Day}"), Color = HexToRustFormat(HEX) },
                        new CuiRectTransformComponent{  AnchorMin = $"0 0", AnchorMax = $"1 1" },
                        }
                });

                if (Data.Value)
                {
                    container.Add(new CuiElement
                    {
                        Parent = $"DAY_{Day}",
                        Components =
                        {
                        new CuiRawImageComponent { Png = GetImage($"DAY_ACCESS_{Day}") },
                        new CuiRectTransformComponent{ AnchorMin = "0.3 0.35", AnchorMax = "0.7 0.7"  },
                        }
                    });
                }
                                
                if (ThisDay == Data.Key && !Data.Value)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.88" },
                        Button = { Command = $"daily take {Day} {ThisDay}", Color = "0 0 0 0" },
                        Text = { Text = "" }
                    }, $"DAY_{Day}");
                }

                #region CenterFunc

                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }

                #endregion

                Day++;
                if (Day >= 7)
                    break;
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8817708 0.9416667", AnchorMax = "1 1" },
                Button = { Close = UI_MAIN_UI_STEP, Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("UI_CLOSE", this, player.UserIDString) }
            }, UI_MAIN_UI_STEP);

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region InterfaceDaysTake
        void Interface_Days_Take(BasePlayer player,int Day, int ThisDay)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_MAIN_UI_STEP_TAKE);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                FadeOut = 0.15f,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#282721E6"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            },  UI_MAIN_UI_STEP, UI_MAIN_UI_STEP_TAKE);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9037037", AnchorMax = "1 1" },
                Text = { Text = String.Format(lang.GetMessage("UI_PRIZES", this, player.UserIDString), config.Days[Day].Day), Color = HexToRustFormat("#FFFFFF99"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            },  UI_MAIN_UI_STEP_TAKE);

            CuiHelper.AddUi(player, container);

            GiveReward(player, Day);
            DataPlayer[player.userID][ThisDay] = true;
            RunEffect(player, "assets/bundled/prefabs/fx/impacts/blunt/cloth/cloth1.prefab");
            ServerMgr.Instance.StartCoroutine(AnimationItems(player, Day));
            if (StopSpam.Contains(player.userID))
                StopSpam.Remove(player.userID);
        }

        #endregion

        #region AnimationItems

        public IEnumerator AnimationItems(BasePlayer player, int Day)
        {
            var Items = config.Days[Day].Rewards;

            #region CenterFunc

            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.413646f - 0.18f; /// Ширина
            float itemMargin = 0.409895f - 0.38f; /// Расстояние между 
            int itemCount = Items.Count;
            float itemMinHeight = 0.5f; // Сдвиг по вертикали
            float itemHeight = 0.4f; /// Высота
            int ItemTarget = 3;

            if (itemCount > ItemTarget)
            {
                itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                itemCount -= ItemTarget;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            #endregion

            for (int i = 0; i < Items.Count; i++)
            {
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    FadeOut = 0.15f,
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = { FadeIn = 0.15f, Color = HexToRustFormat("#282721E6"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
                }, UI_MAIN_UI_STEP_TAKE, $"ITEM_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"ITEM_{i}",
                    Components =
                        {
                        new CuiRawImageComponent { Png = GetImage(GetImageReward(Day, i))},
                        new CuiRectTransformComponent{  AnchorMin = "0.05263162 0.03859329", AnchorMax = "0.9473684 0.9333" }, //1398
                        }
                });

                #region CenterFunc

                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % ItemTarget == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));
                    if (itemCount > ItemTarget)
                    {
                        itemMinPosition = 0.5f - ItemTarget / 2f * itemWidth - (ItemTarget - 1) / 2f * itemMargin;
                        itemCount -= ItemTarget;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }

                #endregion

                CuiHelper.AddUi(player, container);
                yield return new WaitForSeconds(0.5f);
            }

            timer.Once(3f, () => { 
                Interface_Daily(player);
            });
        }

        #endregion

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TITLE"] = "<size=30><b>DAILY AWARDS OF THE COOL SERVER</b></size>",
                ["UI_DESCRIPTION"] = "<size=18>IF YOU MISS ONE DAY - EVERYTHING WILL START FROM THE BEGINNING</size>",
                ["UI_PRIZES"] = "<size=30><b>YOU HAVE SUCCESSFULLY TAKEN {0}</b></size>",
                ["UI_CLOSE"] = "<size=30><b>CLOSE</b></size>",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TITLE"] = "<size=30><b>ЕЖЕДНЕВНЫЕ НАГРАДЫ КРУТОГО СЕРВЕРА</b></size>",
                ["UI_DESCRIPTION"] = "<size=18>ЕСЛИ ВЫ ПРОПУСТИТЕ ОДИН ДЕНЬ - ВСЕ НАЧНЕТСЯ С НАЧАЛА</size>",
                ["UI_PRIZES"] = "<size=30><b>ВЫ УСПЕШНО ЗАБРАЛИ {0}</b></size>",
                ["UI_CLOSE"] = "<size=30><b>ЗАКРЫТЬ</b></size>",
            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion

        #region Helps
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
        void RunEffect(BasePlayer player, string path)
        {
            Effect effect = new Effect();
            effect.Init(Effect.Type.Generic, player.transform.position, player.transform.forward, (Network.Connection)null);
            effect.pooledString = path; EffectNetwork.Send(effect, player.net.connection);
        }
        #endregion

        #region API

        public bool IsDay(ulong userID, int Day)
        {
            if (DataPlayer.ContainsKey(userID))
                if (DataPlayer[userID].ContainsKey(Day))
                    if (DataPlayer[userID][Day])
                        return true;
                    else return false;
                else return false;
            else return false;
        }

        Int32 GetPlayerDays(UInt64 userID)
        {
            Int32 Days = 0;
            if (!DataPlayer.ContainsKey(userID))
                return Days;
            Days = DataPlayer[userID].Count(x => x.Value);
            return Days;
        }

        #endregion
    }
}
