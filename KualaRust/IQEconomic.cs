using System;
using System.Collections.Generic;
using System.Globalization;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQEconomic", "Mercury", "0.1.1")]
    [Description("Экономика на ваш сервер")]
    class IQEconomic : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin IQChat, Friends, Clans, Battles, Duel, RustStore, ImageLibrary;
        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.ReferenceSettings.ChatSettings;
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
            var Images = config.TransferSettings;
            if (string.IsNullOrEmpty(Images.URLMoney) || string.IsNullOrEmpty(Images.URLStores)) return;
            if (!ImageLibrary)
            {
                PrintError("Не установлен плагин ImageLibrary!");
                return;
            }

            AddImage(Images.URLMoney, "URLMoney");
            AddImage(Images.URLStores, "URLStores");
        }

        public bool IsFriends(ulong userID, ulong targetID)
        {
            if (Friends)
                return (bool)Friends?.Call("HasFriend", userID, targetID);
            else return false;
        }
        public bool IsClans(ulong userID, ulong targetID)
        {
            if (Clans)
                return (bool)Clans?.Call("HasFriend", userID, targetID);
            else return false;
        }
        public bool IsDuel(ulong userID)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel) return (bool)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else return false;
        }
        public void MoscovOVHBalanceSet(ulong userID, int Balance, int MoneyTake)
        {
            if (!RustStore)
            {
                PrintWarning("У вас не установлен магазин MoscovOVH");
                return;
            }
            plugins.Find("RustStore").CallHook("APIChangeUserBalance", userID, Balance, new Action<string>((result) =>
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (result == "SUCCESS")
                {
                    Puts($"Пользователю {userID} успешно зачислен баланс - {Balance}");
                    RemoveBalance(userID, MoneyTake);
                    if (player == null) return;
                    SendChat(lang.GetMessage("CHAT_STORE_SUCCESS", this, player.UserIDString), player);
                    return;
                }
                Puts($"Пользователь {userID} не авторизован в магазине");
                if (player == null) return;
                SendChat(lang.GetMessage("CHAT_NO_AUTH_STORE", this, player.UserIDString), player);
            }));
        }
        public void GameStoresBalanceSet(ulong userID, int Balance,int MoneyTake)
        {
            var GameStores = config.ReferenceSettings.GameStoresSettings;
            if (String.IsNullOrEmpty(GameStores.GameStoresAPIStore) || String.IsNullOrEmpty(GameStores.GameStoresIDStore))
            {
                PrintWarning("Магазин GameStores не настроен! Невозможно выдать баланс пользователю");
                return;
            }
            webrequest.Enqueue($"https://gamestores.ru/api?shop_id={GameStores.GameStoresIDStore}&secret={GameStores.GameStoresAPIStore}&action=moneys&type=plus&steam_id={userID}&amount={Balance}&mess={GameStores.GameStoresMessage}", null, (i, s) =>
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (i != 200) { }
                if (s.Contains("success"))
                {
                    Puts($"Пользователю {userID} успешно зачислен баланс - {Balance}");
                    RemoveBalance(userID, MoneyTake);
                    if (player == null) return;
                    SendChat(lang.GetMessage("CHAT_STORE_SUCCESS", this, player.UserIDString), player);
                    return;
                }
                if (s.Contains("fail"))
                {
                    Puts($"Пользователь {userID} не авторизован в магазине");
                    if (player == null) return;
                    SendChat(lang.GetMessage("CHAT_NO_AUTH_STORE", this, player.UserIDString),player);
                }
            }, this);
        }
        #endregion

        #region Configuration
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Монетки будут и гроков на руках - false / Иначе будет чат или интерфейсе - true")]
            public bool UseUI;
            [JsonProperty("Использовать UI интерфейс для отображения баланса(true - да(Если вы не поставили , чтобы монетки были на руках))/false - информация будет в чате по команде и при входе)")]
            public bool UseUIMoney;
            [JsonProperty("Включить обмен валюты на баланс в магазине(GameStores/MoscovOVH)")]
            public bool TransferStoreUse;
            [JsonProperty("Отображение интерфейса с балансом(true - отображает/false - скрывает)")]
            public bool ShowUI;
            [JsonProperty("Настройки обмена валют на баланс в магазине")]
            public TransferSetting TransferSettings = new TransferSetting();
            [JsonProperty("Основные настройки")]
            public GeneralSettings GeneralSetting = new GeneralSettings();
            [JsonProperty("Настройки валюты(Если вид экономикик - false)")]
            public CustomMoney CustomMoneySetting = new CustomMoney();
            [JsonProperty("Настройки совместной работы с другими плагинами")]
            public ReferenceSetting ReferenceSettings = new ReferenceSetting();
            internal class CustomMoney
            {
                [JsonProperty("Название валюты")]
                public string DisplayName;
                [JsonProperty("Shortname монетки")]
                public string Shortname;
                [JsonProperty("SkinID монетки")]
                public ulong SkinID;
            }
                
            internal class GeneralSettings
            {
                [JsonProperty("Получение валюты за убийство игроков")]
                public bool BPlayerKillUse;
                [JsonProperty("Получение валюты за убийство животных")]
                public bool BPlayerAnimalUse;
                [JsonProperty("Получение валюты за убийство NPC")]
                public bool BPlayerNPCUse;
                [JsonProperty("Получение валюты за добычу ресурсов")]
                public bool BPlayerGatherUse;
                [JsonProperty("Получение валюты за уничтожение танка")]
                public bool BPlayerBradleyUse;
                [JsonProperty("Получение валюты за уничтожение вертолета")]
                public bool BPlayerHelicopterUse;
                [JsonProperty("Получение валюты за уничтожение бочек")]
                public bool BPLayerBarrelUse;
                [JsonProperty("Получение валюты проведенное время на сервере")]
                public bool BPLayerOnlineUse;
                [JsonProperty("Сколько нужно провести времени,чтобы выдали награду")]
                public int BPlayerOnlineTime;
                [JsonProperty("Сколько начислять валюты за проведенное время на сервере")]
                public int BPlayerOnlineGive;
                [JsonProperty("Настройка зачисления баланса за убийство игроков")]
                public AdvancedSetting BPlayerKillGive = new AdvancedSetting();
                [JsonProperty("Сколько начислять валюты за убийство животных")]
                public AdvancedSetting BPlayerAnimalGive = new AdvancedSetting();
                [JsonProperty("Сколько начислять валюты за убийство NPC")]
                public AdvancedSetting BPlayerNPCGive = new AdvancedSetting();
                [JsonProperty("Сколько начислять валюты за уничтожение танка")]
                public AdvancedSetting BPlayerBradleyGive = new AdvancedSetting();
                [JsonProperty("Сколько начислять валюты за уничтожение вертолета")]
                public AdvancedSetting BPlayerHelicopterGive = new AdvancedSetting();
                [JsonProperty("Сколько начислять валюты за уничтожение бочек")]
                public AdvancedSetting BPlayerBarrelGive = new AdvancedSetting();
                [JsonProperty("Сколько начислять валюты за добычу ресурсов ( [за какой ресурс давать] = { остальная настройка }")]
                public Dictionary<string,AdvancedSetting> BPlayerGatherGive = new Dictionary<string, AdvancedSetting>();

                internal class AdvancedSetting
                {
                    [JsonProperty("Шанс получить валюту")]
                    public int Rare;
                    [JsonProperty("Сколько выдавать валюты")]
                    public int BPlayerGive;
                }
            }
            internal class TransferSetting
            {
                [JsonProperty("URL вашей монеты")]
                public string URLMoney;
                [JsonProperty("URL валюты для магазина")]
                public string URLStores;
                [JsonProperty("Сколько монет требуется для обмена")]
                public int MoneyCount;
                [JsonProperty("Сколько баланса получит игрок после обмена")]
                public int StoresMoneyCount;
            }
            internal class ReferenceSetting
            {
                [JsonProperty("Friends : Запретить получение монет за убийство друзей")]
                public bool FriendsBlockUse;
                [JsonProperty("Clans : Запретить получение монет за убийство сокланов")]
                public bool ClansBlockUse;
                [JsonProperty("Duel/Battles : Запретить получение монет за убийство на дуэлях")]
                public bool DuelBlockUse;
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

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    UseUI = true,
                    ShowUI = true,
                    UseUIMoney = true,
                    TransferStoreUse = true,
                    TransferSettings = new TransferSetting
                    {
                        MoneyCount = 3,
                        StoresMoneyCount = 1,
                        URLMoney = "https://i.imgur.com/1dXVda4.png",
                        URLStores = "https://i.imgur.com/vPjkizs.png",
                    },
                    GeneralSetting = new GeneralSettings
                    {
                        BPLayerOnlineUse = true,
                        BPLayerBarrelUse = true,
                        BPlayerKillUse = true,
                        BPlayerAnimalUse = true,
                        BPlayerNPCUse = true,
                        BPlayerGatherUse = true,
                        BPlayerBradleyUse = true,
                        BPlayerHelicopterUse = true,
                        BPlayerOnlineTime = 60,
                        BPlayerOnlineGive = 5,
                        BPlayerAnimalGive = new GeneralSettings.AdvancedSetting { Rare = 53, BPlayerGive = 3 },
                        BPlayerKillGive = new GeneralSettings.AdvancedSetting { Rare = 40, BPlayerGive = 5 },
                        BPlayerNPCGive = new GeneralSettings.AdvancedSetting { Rare = 20, BPlayerGive = 2 },
                        BPlayerBradleyGive = new GeneralSettings.AdvancedSetting { Rare = 100 , BPlayerGive = 15},
                        BPlayerHelicopterGive = new GeneralSettings.AdvancedSetting { Rare = 90, BPlayerGive = 10 },
                        BPlayerBarrelGive = new GeneralSettings.AdvancedSetting { Rare = 10, BPlayerGive = 3 },
                        BPlayerGatherGive = new Dictionary<string, GeneralSettings.AdvancedSetting>
                        {
                            ["sulfur.ore"] = new GeneralSettings.AdvancedSetting
                            {
                                BPlayerGive = 10,
                                Rare = 10,
                            },
                            ["stones"] = new GeneralSettings.AdvancedSetting
                            {
                                Rare = 20,
                                BPlayerGive = 1,
                            }
                        },
                    },
                    CustomMoneySetting = new CustomMoney
                    {
                        DisplayName = "Монета удачи",
                        Shortname = "bleach",
                        SkinID = 1337228,
                    },
                    ReferenceSettings = new ReferenceSetting
                    {
                        FriendsBlockUse = true,
                        ClansBlockUse = true,
                        DuelBlockUse = true,
                        MoscovOvhUse = true,
                        GameStoreshUse = false,
                        GameStoresSettings = new ReferenceSetting.GameStores
                        {
                            GameStoresAPIStore = "",
                            GameStoresIDStore = "",
                            GameStoresMessage = "Успешный обмен"
                        },
                        ChatSettings = new ReferenceSetting.ChatSetting
                        {
                            CustomAvatar = "",
                            CustomPrefix = "",
                            UIAlertUse = true,
                        }
                    }

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
                PrintWarning("Ошибка #49" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию! #33");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Data
        [JsonProperty("Система экономики")] public Dictionary<ulong, InformationData> DataEconomics = new Dictionary<ulong, InformationData>();
        void ReadData() { 
            DataEconomics = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, InformationData>>("IQEconomic/DataEconomics");
        }
        void WriteData() {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQEconomic/DataEconomics", DataEconomics);
        }
        void RegisteredDataUser(ulong player)
        {
            if (!DataEconomics.ContainsKey(player))
                DataEconomics.Add(player, new InformationData { Balance = 0, Time = 0 });
        }
        public class InformationData
        {
            [JsonProperty("Баланс игрока")]
            public int Balance;
            [JsonProperty("Счетчик времени")]
            public int Time;
        }
        #endregion

        #region Command

        [ChatCommand("transfer")]
        void ChatCommandTransfer(BasePlayer player, string cmd, string[] arg)
        {
            if (player == null) return;
            if (!config.TransferStoreUse && arg.Length == 0 || arg == null)
            {
                SendChat(lang.GetMessage("TRANSFER_COMMAND_NO_ARGS", this, player.UserIDString),player);
                return;
            }
            else if(config.TransferStoreUse && arg.Length == 0 || arg == null)
            {
                Interface_Changer(player);
                return;
            }
            BasePlayer transferPlayer = FindPlayer(arg[0]);
            if (transferPlayer == null)
            {
                SendChat(lang.GetMessage("BALANCE_CUSTOM_MONEY_NOT_PLAYER", this, player.UserIDString), player);
                return;
            }
            if(transferPlayer.IsDead())
            {
                SendChat(lang.GetMessage("BALANCE_TRANSFER_TRANSFERPLAYER_DIE", this, player.UserIDString), player);
                return;
            }

            int TransferBalance = Convert.ToInt32(arg[1]);
            TransferPlayer(player.userID, transferPlayer.userID, TransferBalance);
        }
        
        [ConsoleCommand("transfer")]
        void ConsoleCommandTransfer(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null) return;
            int Balance = GetBalance(player.userID);
            var Reference = config.ReferenceSettings;

            if (!IsTransfer(Balance)) return;
            var Transfer = config.TransferSettings;

            if (Reference.GameStoreshUse)
                GameStoresBalanceSet(player.userID, Transfer.StoresMoneyCount, Transfer.MoneyCount);

            if(Reference.MoscovOvhUse)
                MoscovOVHBalanceSet(player.userID, Transfer.StoresMoneyCount, Transfer.MoneyCount);

            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "MY_INFO_TRANSFERS");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-130 5", OffsetMax = "60 50" },
                Text = { Text = String.Format(lang.GetMessage("UI_CHANGER_MY_INFO", this, player.UserIDString), GetBalance(player.userID) - Transfer.MoneyCount), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, UI_CHANGER_PARENT, "MY_INFO_TRANSFERS");

            CuiHelper.AddUi(player, container);

        }

        [ConsoleCommand("migration")]
        void MigrationDataFile(ConsoleSystem.Arg args)
        {
            PrintWarning("Начинаем миграцию..Ищем файл . . .");
            string path = $"IQEconomic/DataEconomicsOLD";
            var data = Interface.GetMod().DataFileSystem.GetDatafile(path);
            if(data == null)
            {
                PrintError("Старый файл не найден!");
                return;
            }
            Dictionary<ulong,int> OldStructure = new Dictionary<ulong, int>();
            OldStructure = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, int>>(path);
            PrintWarning("Считали старую структуру....");
            foreach(var Old in OldStructure)
            {
                if (!DataEconomics.ContainsKey(Old.Key))
                    DataEconomics.Add(Old.Key, new InformationData { Balance = Old.Value, Time = 0 });
                else DataEconomics[Old.Key].Balance += Old.Value;
            }
            WriteData();
            PrintWarning("Миграция прошла успешно, выгрузите плагин и удалите старый файл");
        }

        [ConsoleCommand("iq.eco")]
        void IQEconomicCommandsAdmin(ConsoleSystem.Arg arg)
        {
            switch(arg.Args[0])
            {
                case "give":
                    {
                        ulong userID = ulong.Parse(arg.Args[1]);
                        int Balance = Convert.ToInt32(arg.Args[2]);
                        SetBalance(userID, Balance);
                        Puts($"Игроку {userID} успешно зачислено {Balance} монет");
                        break;
                    }
                case "remove":
                    {
                        ulong userID = ulong.Parse(arg.Args[1]);
                        int Balance = Convert.ToInt32(arg.Args[2]);
                        RemoveBalance(userID, Balance);
                        Puts($"Игроку {userID} успешно снято {Balance} монет");
                        break;
                    }
            }
        }

        #endregion

        #region Metods
        public void TrackerTime()
        {
            foreach (var player in BasePlayer.activePlayerList)
                if (DataEconomics[player.userID].Time <= CurrentTime())
                {
                    int SetTime = Convert.ToInt32(config.GeneralSetting.BPlayerOnlineTime + CurrentTime());
                    SetBalance(player.userID, config.GeneralSetting.BPlayerOnlineGive);
                    DataEconomics[player.userID].Time = SetTime;
                }
        }

        public void ConnectedPlayer(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => ConnectedPlayer(player));
                return;
            }

            RegisteredDataUser(player.userID);

            if (config.UseUI)
                if (config.UseUIMoney)
                {
                    if (config.ShowUI)
                        Interface_Balance(player);
                }
                else SendChat(String.Format(lang.GetMessage("CHAT_MY_BALANCE", this), GetBalance(player.userID)), player);
        }
        public void TransferPlayer(ulong userID, ulong transferUserID, int Balance )
        {
            BasePlayer player = BasePlayer.FindByID(userID);
            BasePlayer transferPlayer = BasePlayer.FindByID(transferUserID);
            if (player == null) return;
            if (transferPlayer == null) return;

            if (!IsRemoveBalance(player.userID, Balance))
            {
                SendChat(lang.GetMessage("BALANCE_TRANSFER_NO_BALANCE", this, player.UserIDString), player);
                return;
            }

            RemoveBalance(player.userID, Balance);
            SetBalance(transferPlayer.userID, Balance);
            SendChat(String.Format(lang.GetMessage("BALANCE_TRANSFER_PLAYER", this, player.UserIDString), transferPlayer.displayName, Balance),player);
            SendChat(String.Format(lang.GetMessage("BALANCE_TRANSFER_TRANSFERPLAYER", this, transferPlayer.UserIDString), Balance, player.displayName),transferPlayer);
        }
        public void SetBalance(ulong userID, int SetBalance, ItemContainer cont = null)
        {
            if(!config.UseUI)
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (player == null)
                {
                    PrintWarning(lang.GetMessage("BALANCE_CUSTOM_MONEY_NOT_PLAYER",this));
                    return;
                }
                Item Money = CreateCustomMoney(SetBalance);
                ItemContainer itemContainer = cont == null ? player.inventory.containerMain : cont;

                if (player.inventory.containerMain.itemList.Count == 24)
                {
                    SendChat(lang.GetMessage("BALANCE_CUSTOM_MONEY_INVENTORY_FULL", this, player.UserIDString), player);
                    Money.Drop(player.transform.position, Vector3.zero);
                    return;
                }
                Money.MoveToContainer(itemContainer);
                SendChat(String.Format(lang.GetMessage("BALANCE_SET", this), SetBalance), player);
            }
            else
            {
                if (IsData(userID))
                {
                    DataEconomics[userID].Balance += SetBalance;
                    BasePlayer player = BasePlayer.FindByID(userID);
                    if (player == null) return;
                    SendChat(String.Format(lang.GetMessage("BALANCE_SET", this), SetBalance), player);
                    if (config.ShowUI)
                        Interface_Balance(player);
                }
                else RegisteredDataUser(userID);
                WriteData();
            }
            Interface.Oxide.CallHook("SET_BALANCE_USER", userID, SetBalance);
        }
        public void RemoveBalance(ulong userID, int RemoveBalance)
        {
            if (!config.UseUI)
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (player == null)
                {
                    PrintWarning(lang.GetMessage("BALANCE_CUSTOM_MONEY_NOT_PLAYER", this));
                    return;
                }
                if(!IsRemoveBalance(userID,RemoveBalance))
                {
                    PrintWarning(lang.GetMessage("BALANCE_CUSTOM_MONEY_NO_COUNT_TAKE", this));
                    return;
                }
                player.inventory.Take(null, ItemManager.FindItemDefinition(config.CustomMoneySetting.Shortname).itemid, RemoveBalance);
            }
            else
            {
                if (IsData(userID))
                {
                    DataEconomics[userID].Balance -= RemoveBalance;
                    BasePlayer player = BasePlayer.FindByID(userID);
                    if (player == null) return;
                    if (config.ShowUI)
                        Interface_Balance(player);
                }
                else RegisteredDataUser(userID);
                WriteData();
            }
        }

        #region ReturnMetods

        public bool IsRare(int Rare)
        {
            if (Rare >= UnityEngine.Random.Range(0, 100))
                return true;
            else return false;
        }
        public bool IsData(ulong userID)
        {
            if (DataEconomics.ContainsKey(userID))
                return true;
            else return false;
        }
        public int GetBalance(ulong userID)
        {
            if (config.UseUI)
            {
                if (IsData(userID))
                    return DataEconomics[userID].Balance;
                else return 0;
            }
            else
            {
                BasePlayer player = BasePlayer.FindByID(userID);
                if (player == null)
                {
                    PrintWarning(lang.GetMessage("BALANCE_CUSTOM_MONEY_NOT_PLAYER", this));
                    return 0;
                }
                var PMoney = player.inventory.GetAmount(ItemManager.FindItemDefinition(config.CustomMoneySetting.Shortname).itemid);
                return PMoney;
            }
        }
        Item CreateCustomMoney(int Amount)
        {
            var CustomMoney = config.CustomMoneySetting;
            Item Money = ItemManager.CreateByName(CustomMoney.Shortname, Amount, CustomMoney.SkinID);
            Money.name = CustomMoney.DisplayName;
            return Money;
        }

        public bool IsRemoveBalance(ulong userID,int Amount)
        {
            if (GetBalance(userID) >= Amount)
                return true;
            else return false;
        }

        private BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var check in BasePlayer.activePlayerList)
            {
                if (check.displayName.ToLower().Contains(nameOrId.ToLower()) || check.userID.ToString() == nameOrId)
                    return check;
            }

            return null;
        }

        public bool IsTransfer(int Balance)
        {
            var TransferCurse = config.TransferSettings;
            if (Balance < TransferCurse.MoneyCount) return false;
            else return true;
        }
        #endregion

        #endregion

        #region Hooks
        void Unload()
        {
            WriteData();
            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, UI_BALANCE_PARENT);
                CuiHelper.DestroyUi(player, UI_CHANGER_PARENT);
            }
        }
        private void OnServerInitialized()
        {
            ReadData();
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
            WriteData();
            AddAllImage();
            if (config.GeneralSetting.BPLayerOnlineUse)
                timer.Every(120f, () => TrackerTime());
        }
        void OnPlayerConnected(BasePlayer player) => ConnectedPlayer(player);
        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null) return;
            if (player == null) return;
            if (item == null) return;
            var General = config.GeneralSetting;
            if (!General.BPlayerGatherUse) return;
            var GatherGeneral = config.GeneralSetting.BPlayerGatherGive;
            if (!GatherGeneral.ContainsKey(item.info.shortname)) return;
            var Gather = GatherGeneral[item.info.shortname];
            if (!IsRare(Gather.Rare)) return;

            SetBalance(player.userID, Gather.BPlayerGive);
        }
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            BasePlayer player = info.InitiatorPlayer;
            if (info.InitiatorPlayer != null)
                player = info.InitiatorPlayer;
            else if (entity.GetComponent<BaseHelicopter>() != null)
                player = BasePlayer.FindByID(GetLastAttacker(entity.net.ID));
            if (player == null) return;

            var General = config.GeneralSetting;
            var ReferenceGeneral = config.ReferenceSettings;

            if ((bool)(entity as NPCPlayer) || (bool)(entity as NPCMurderer))
                if (General.BPlayerNPCUse)
                {
                    var Setting = General.BPlayerNPCGive;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.BPlayerGive);
                }
            if ((bool)(entity as BasePlayer))
            {
                if ((bool)(entity as NPCPlayer) || (bool)(entity as NPCMurderer)) return;

                BasePlayer targetPlayer = entity.ToPlayer();
                if (targetPlayer == null) return;
                if (targetPlayer.userID != player.userID)
                    if (General.BPlayerKillUse)
                    {
                        if (ReferenceGeneral.FriendsBlockUse)
                            if (IsFriends(player.userID, targetPlayer.userID)) return;
                        if (ReferenceGeneral.ClansBlockUse)
                            if (IsClans(player.userID, targetPlayer.userID)) return;
                        if (ReferenceGeneral.DuelBlockUse)
                            if (IsDuel(player.userID)) return;

                        var Setting = General.BPlayerKillGive;
                        if (!IsRare(Setting.Rare)) return;
                        SetBalance(player.userID, Setting.BPlayerGive);
                    }
            }
            if ((bool)(entity as BaseAnimalNPC))
                if (General.BPlayerAnimalUse)
                {
                    var Setting = General.BPlayerAnimalGive;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.BPlayerGive);
                }
            if ((bool)(entity as BaseHelicopter))
                if (General.BPlayerHelicopterUse)
                {
                    var Setting = General.BPlayerHelicopterGive;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.BPlayerGive);
                }
            if ((bool)(entity as BradleyAPC))
                if (General.BPlayerBradleyUse)
                {
                    var Setting = General.BPlayerBradleyGive;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.BPlayerGive);
                }
            if(entity.PrefabName.Contains("barrel"))
                if(General.BPLayerBarrelUse)
                {
                    var Setting = General.BPlayerBarrelGive;
                    if (!IsRare(Setting.Rare)) return;
                    SetBalance(player.userID, Setting.BPlayerGive);
                }
        }

        private Dictionary<uint, Dictionary<ulong, int>> HeliAttackers = new Dictionary<uint, Dictionary<ulong, int>>();
        void OnEntityTakeDamage(BaseCombatEntity victim, HitInfo info)
        {
            if (victim.GetComponent<BaseHelicopter>() != null && info?.Initiator?.ToPlayer() != null)
            {
                var heli = victim.GetComponent<BaseHelicopter>();
                var player = info.Initiator.ToPlayer();
                NextTick(() =>
                {
                    if (heli == null) return;
                    if (!HeliAttackers.ContainsKey(heli.net.ID))
                        HeliAttackers.Add(heli.net.ID, new Dictionary<ulong, int>());
                    if (!HeliAttackers[heli.net.ID].ContainsKey(player.userID))
                        HeliAttackers[heli.net.ID].Add(player.userID, 0);
                    HeliAttackers[heli.net.ID][player.userID]++;
                });
            }
        }

        private ulong GetLastAttacker(uint id)
        {
            int hits = 0;
            ulong majorityPlayer = 0U;
            if (HeliAttackers.ContainsKey(id))
            {
                foreach (var score in HeliAttackers[id])
                {
                    if (score.Value > hits)
                        majorityPlayer = score.Key;
                }
            }
            return majorityPlayer;
        }

        #region HelpHooks
        private Item OnItemSplit(Item item, int amount)
        {
            if (plugins.Find("Stacks") || plugins.Find("CustomSkinsStacksFix") || plugins.Find("SkinBox")) return null; 
            var CustomMoney = config.CustomMoneySetting;
            if (CustomMoney.SkinID == 0) return null;
            if (item.skin == CustomMoney.SkinID)
            {
                Item x = ItemManager.CreateByPartialName(CustomMoney.Shortname, amount);
                x.name = CustomMoney.DisplayName;
                x.skin = CustomMoney.SkinID;
                x.amount = amount;
                item.amount -= amount;
                return x;
            }
            return null;
        }
        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.GetItem().skin != targetItem.GetItem().skin) return false;

            return null;
        }
        object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin != targetItem.skin) return false;

            return null;
        }
        #endregion

        #endregion

        #region Interface

        public static string UI_BALANCE_PARENT = "BALANCE_PLAYER_PARENT";
        public static string UI_CHANGER_PARENT = "CHANGER_PLAYER_PARENT";
        public void Interface_Balance(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_BALANCE_PARENT);

            var Balance = GetBalance(player.userID);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"1 0", AnchorMax = $"1 0", OffsetMin = "-396 70", OffsetMax = "-218 97" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#FFFFFF05"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "Hud", UI_BALANCE_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.01932367 0.04938272", AnchorMax = $"0.1541551 0.9382206" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#b1b1b1"), Sprite = "assets/icons/favourite_servers.png" }
            }, UI_BALANCE_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.1835208 0.1358024", AnchorMax = $"0.9625472 0.8518519" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#FF800080"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, UI_BALANCE_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.22 0", AnchorMax = "1 1" },
                Text = { Text = String.Format(lang.GetMessage("UI_MY_BALANCE", this), Balance), Color = HexToRustFormat("#FEFFDDFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft, FadeIn = 0.3f }
            }, UI_BALANCE_PARENT); 

            CuiHelper.AddUi(player, container); 
        }

        public void Interface_Changer(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_CHANGER_PARENT);
            var Balance = GetBalance(player.userID);
            var Transfer = config.TransferSettings;

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-150 -280", OffsetMax = "20 -230" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat("#292A2140"), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", UI_CHANGER_PARENT);

            container.Add(new CuiButton
            {
                FadeOut = 0.2f,
                RectTransform = { AnchorMin = $"1 1", AnchorMax = $"1 1", OffsetMin = "2 -50", OffsetMax = "161 0" },
                Button = { Command = $"transfer", Color = HexToRustFormat("#FF800080"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                Text = { Text = lang.GetMessage("UI_CHANGER_TRANSFER", this, player.UserIDString), Color = HexToRustFormat("#0B5CF4"), Align = TextAnchor.MiddleCenter }
            },  UI_CHANGER_PARENT);

            container.Add(new CuiButton
            {
                FadeOut = 0.2f,
                RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0 0", OffsetMin = "-50 0", OffsetMax = "-2 50" },
                Button = { Close = UI_CHANGER_PARENT, Color = HexToRustFormat("#b0382580"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                Text = { Text = "<b>✖</b>", Color = HexToRustFormat("#c8a097"),FontSize = 30, Align = TextAnchor.MiddleCenter }
            }, UI_CHANGER_PARENT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-130 5", OffsetMax = "60 50" },
                Text = { Text = String.Format(lang.GetMessage("UI_CHANGER_MY_INFO", this,player.UserIDString),Balance), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, UI_CHANGER_PARENT,"MY_INFO_TRANSFERS");

            #region TransferInterface

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.01866667 0.06666667", AnchorMax = $"0.2696471 0.92" },
                Image = { Color = HexToRustFormat("#8A847E3D"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, UI_CHANGER_PARENT, "MY_MONEY");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.7351995 0.06666667", AnchorMax = $"0.9861803 0.92" },
                Image = { Color = HexToRustFormat("#8A847E3D"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, UI_CHANGER_PARENT, "TRANSFER_MONEY");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2151515 0.1200001", AnchorMax = "0.7757577 0.8800001" },
                Text = { Text = $"<b>{Transfer.MoneyCount} > {Transfer.StoresMoneyCount} </b>", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            },  UI_CHANGER_PARENT);

            container.Add(new CuiElement
            {
                Parent = "MY_MONEY",
                Components =
                        {
                        new CuiRawImageComponent { Png = GetImage("URLMoney") },
                        new CuiRectTransformComponent{  AnchorMin = $"0.125 0.125", AnchorMax = $"0.875 0.875" },
                        }
            });

            container.Add(new CuiElement
            {
                Parent = "TRANSFER_MONEY",
                Components =
                        {
                        new CuiRawImageComponent { Png = GetImage("URLStores") },
                        new CuiRectTransformComponent{  AnchorMin = $"0.125 0.125", AnchorMax = $"0.875 0.875" },
                        }
            });

            #endregion

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MY_BALANCE"] = "<b><size=12>Balance : {0}</size></b>",

                ["CHAT_MY_BALANCE"] = "Your Balance : <color=yellow>{0}</color>",
                ["BALANCE_CUSTOM_MONEY_NOT_PLAYER"] = "Player not found",
                ["BALANCE_CUSTOM_MONEY_INVENTORY_FULL"] = "Your inventory is full, coins fell to the floor",

                ["BALANCE_SET"] = "You have successfully received : {0} money",
                ["BALANCE_TRANSFER_NO_BALANCE"] = "You do not have so many coins to transfer",
                ["BALANCE_TRANSFER_PLAYER"] = "You have successfully submitted {0} {1} money",
                ["BALANCE_TRANSFER_TRANSFERPLAYER"] = "You have successfully received {0} money from {1}",
                ["BALANCE_CUSTOM_MONEY_NO_COUNT_TAKE"] = "The player does not have as many coins available",
                ["BALANCE_TRANSFER_TRANSFERPLAYER_DIE"] = "The player is dead, you can’t give him coins",

                ["TRANSFER_COMMAND_NO_ARGS"] = "Invalid Command\nEnter the correct transfer command transfer [Nick] [Amount Money]",

                ["UI_CHANGER_TRANSFER"] = "<b><size=20>TRANSFER</size>  </b>",
                ["UI_CHANGER_MY_INFO"] = "<b><size=16>TRANSFERS\nYour Balance : {0}</size></b>",

                ["CHAT_NO_AUTH_STORE"] = "You no auth stores",
                ["CHAT_STORE_SUCCESS"] = "You succes transfers",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_MY_BALANCE"] = "<b><size=12>Ваш баланс : {0}</size></b>",

                ["CHAT_MY_BALANCE"] = "Ваш баланс на данный момент : <color=yellow>{0}</color>",

                ["BALANCE_SET"] = "Вы успешно получили : {0} монет",
                ["BALANCE_CUSTOM_MONEY_NOT_PLAYER"] = "Такого игрока нет",
                ["BALANCE_CUSTOM_MONEY_INVENTORY_FULL"] = "Ваш инвентарь полон, монеты выпали на пол",
                ["BALANCE_CUSTOM_MONEY_NO_COUNT_TAKE"] = "У игрока нет столько монет в наличии",

                ["BALANCE_TRANSFER_NO_BALANCE"] = "У вас нет столько монет для передачи",
                ["BALANCE_TRANSFER_PLAYER"] = "Вы успешно передали {0} {1} монет(ы)",
                ["BALANCE_TRANSFER_TRANSFERPLAYER"] = "Вы успешно получили {0} монет(ы) от {1}",
                ["BALANCE_TRANSFER_TRANSFERPLAYER_DIE"] = "Игрок мертв,вы не можете передать ему монеты",

                ["TRANSFER_COMMAND_NO_ARGS"] = "Неверная команда\nВведите корректную команду transfer [Ник] [Количество монет]",

                ["UI_CHANGER_TRANSFER"] = "<b><size=20>ОБМЕНЯТЬ</size></b>",
                ["UI_CHANGER_MY_INFO"] = "<b><size=16>ОБМЕННИК\nВАШ БАЛАНС : {0}</size></b>",

                ["CHAT_NO_AUTH_STORE"] = "Вы не аваторизованы в магазине",
                ["CHAT_STORE_SUCCESS"] = "Вы успешно обменяли валюту",
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
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        #endregion

        #region API
        bool API_IS_USER(ulong userID) => IsData(userID);
        bool API_IS_REMOVED_BALANCE(ulong userID, int Amount) => IsRemoveBalance(userID, Amount);
        int API_GET_BALANCE(ulong userID) => GetBalance(userID);
        Item API_GET_ITEM(int Amount) => CreateCustomMoney(Amount);
        string API_GET_MONEY_IL() { return "URLMoney"; }
        string API_GET_STORES_IL() { return "URLStores"; }
        void API_SET_BALANCE(ulong userID, int Balance, ItemContainer itemContainer = null)
        {
            SetBalance(userID, Balance, itemContainer);
            Puts("Успешно зачислен баланс");
        }
        void API_REMOVE_BALANCE(ulong userID, int Balance)
        {
            RemoveBalance(userID, Balance);
            Puts("Успешно списан баланс");
        }
        void API_TRANSFERS(ulong userID, ulong trasferUserID, int Balance) => TransferPlayer(userID, trasferUserID, Balance);
        bool API_MONEY_TYPE()
        {
            if (!config.UseUI)
                return true;
            else return false;
        }
        #endregion
    }
}
