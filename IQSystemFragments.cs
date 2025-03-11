using System;
using System.Collections.Generic;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQSystemFragments", "xуй", "0.0.4")]
    [Description("Система фрагментов")]
    class IQSystemFragments : RustPlugin
    {
        #region Vars
        public string ReplaceShortname = "skull.human";
        public string ReplaceShortnameFull = "skull.wolf";
        public enum TypeReward
        {
            Command,
            ItemList,
            IQEconomic,
            CommandList,
        }
        public enum TypeItem
        {
            Fragment,
            Full

        }
        #endregion

        #region Reference
        [PluginReference] Plugin IQChat, IQEconomic;
        public void SetBalance(ulong userID, int Balance) => IQEconomic?.Call("API_SET_BALANCE", userID, Balance);
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            var Chat = config.GeneralSetting.ChatSetting;
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, Chat.CustomPrefix, Chat.CustomAvatar);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка плагина")]
            public GeneralSettings GeneralSetting = new GeneralSettings();
            [JsonProperty("Настройка системы фрагментов")]
            public Dictionary<string, ItemSettings> ItemSetting = new Dictionary<string, ItemSettings>();

            internal class GeneralSettings
            {
                [JsonProperty("Настройки IQChat")]
                public ChatSettings ChatSetting = new ChatSettings();
                [JsonProperty("Настройки IQPlagueSkill")]
                public IQPlagueSkills IQPlagueSkill = new IQPlagueSkills();

                internal class ChatSettings
                {
                    [JsonProperty("IQChat : Кастомный префикс в чате")]
                    public string CustomPrefix;
                    [JsonProperty("IQChat : Кастомный аватар в чате(Если требуется)")]
                    public string CustomAvatar;
                }
                internal class IQPlagueSkills
                {
                    [JsonProperty("На сколько увеличить шанс выпадения фрагментов")]
                    public int RareUpFragments;
                    [JsonProperty("На сколько увеличить шанс выпадения полных частей")]
                    public int RareUpFull;
                }
            }

            internal class ItemSettings
            {
                [JsonProperty("Отображаемое имя привилегии")]
                public string DisplayName;
                [JsonProperty("Сколько требуется фрагментов на получение награды")]
                public int AmountFragmets;
                [JsonProperty("SkinID для полного комлпекта")]
                public ulong SkinID;
                [JsonProperty("Настройка фрагментов")]
                public FragmentSettings FragmentSetting = new FragmentSettings();
                [JsonProperty("Настройка награды")]
                public RewardSettings RewardSetting = new RewardSettings();
                [JsonProperty("Настройка выпадения целого фрагмента.[откуда] = шанс")]
                public Dictionary<string, int> DropListRareFull = new Dictionary<string, int>();
                internal class FragmentSettings
                {
                    [JsonProperty("Отображаемое имя фрагмента")]
                    public string DisplayName;
                    [JsonProperty("SkinID фрагмента")]
                    public ulong SkinID;
                    [JsonProperty("Настройка выпадения.[откуда] = шанс")]
                    public Dictionary<string, int> DropListRare = new Dictionary<string, int>();
                }
                internal class RewardSettings
                {
                    [JsonProperty("ТИП ПРИЗА : 0 - Команда  , 1 - Лист предметов, 2 - IQEconomic монеты, 3 - Список команд")]
                    public TypeReward typeReward;
                    [JsonProperty("Команда,которая отыграется,когда игрок заберет приз(ТИП ПРИЗА - 0) %USERID% - заменится на ID игрока")]
                    public string Command;
                    [JsonProperty("Список команд,которые отыграются,когда игрок заберет приз(ТИП ПРИЗА - 3) %USERID% - заменится на ID игрока")]
                    public List<string> CommandList;
                    [JsonProperty("Количество выдаваемых монет(ТИП ПРИЗА - 2)")]
                    public int Balance;
                    [JsonProperty("Настройка предметов из RUST'a(ТИП ПРИЗА - 1)")]
                    public List<ItemRewardSettings> ItemRewardSetting = new List<ItemRewardSettings>();

                    internal class ItemRewardSettings
                    {
                        [JsonProperty("Отображаемое имя(если это не кастомный предмет,оставьте пустым)")]
                        public string DisplayName;
                        [JsonProperty("Shortname предмета")]
                        public string Shortname;
                        [JsonProperty("SkinID предмета")]
                        public ulong SkinID;
                        [JsonProperty("Количество для выдачи")]
                        public int Amount;
                    }
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    GeneralSetting = new GeneralSettings
                    {
                        ChatSetting = new GeneralSettings.ChatSettings
                        {
                            CustomAvatar = "",
                            CustomPrefix = "[IQSystemFragments]",
                        },
                    },
                    ItemSetting = new Dictionary<string, ItemSettings>
                    {
                        ["vip"] = new ItemSettings
                        {
                            DisplayName = "ПРИВИЛЕГИЯ VIP",
                            AmountFragmets = 10,
                            SkinID = 2101501999,
                            FragmentSetting = new ItemSettings.FragmentSettings
                            {
                                DisplayName = "Фрагмент VIP",
                                SkinID = 2101501999,
                                DropListRare = new Dictionary<string, int>
                                {
                                    ["crate_normal"] = 50,
                                    ["crate_elite"] = 90,
                                }
                            },
                            DropListRareFull = new Dictionary<string, int>
                            {
                                ["crate_normal"] = 50,
                                ["crate_elite"] = 90,
                            },
                            RewardSetting = new ItemSettings.RewardSettings
                            {
                                typeReward = TypeReward.Command,
                                Command = "say GIVE VIP DURAKU",
                                CommandList = new List<string> { },
                                Balance = 0,
                                ItemRewardSetting = new List<ItemSettings.RewardSettings.ItemRewardSettings> { }
                            }
                        },
                        ["vipPrem"] = new ItemSettings
                        {
                            DisplayName = "НАБОР ПРИВИЛЕГИЙ",
                            AmountFragmets = 10,
                            SkinID = 2101501014,
                            FragmentSetting = new ItemSettings.FragmentSettings
                            {
                                DisplayName = "НАБОР ПРИВИЛЕГИЙ",
                                SkinID = 2101056280,
                                DropListRare = new Dictionary<string, int>
                                {
                                    ["crate_normal"] = 50,
                                    ["crate_elite"] = 90,
                                }
                            },
                            DropListRareFull = new Dictionary<string, int>
                            {
                                ["crate_normal"] = 50,
                                ["crate_elite"] = 90,
                            },
                            RewardSetting = new ItemSettings.RewardSettings
                            {
                                typeReward = TypeReward.CommandList,
                                Command = "",
                                CommandList = new List<string>
                                {
                                    "say 1",
                                    "say 2",
                                    "say 3",
                                },
                                Balance = 0,
                                ItemRewardSetting = new List<ItemSettings.RewardSettings.ItemRewardSettings> { }
                            }
                        },
                        ["recycle"] = new ItemSettings
                        {
                            DisplayName = "ПЕРЕРАБОТЧИК",
                            AmountFragmets = 20,
                            SkinID = 2101500645,
                            FragmentSetting = new ItemSettings.FragmentSettings
                            {
                                DisplayName = "Фрагмент ПЕРЕРАБОТЧИКА",
                                SkinID = 2101057099,
                                DropListRare = new Dictionary<string, int>
                                {
                                    ["crate_normal"] = 50,
                                    ["crate_elite"] = 90,
                                }
                            },
                            DropListRareFull = new Dictionary<string, int>
                            {
                                ["crate_normal"] = 50,
                                ["crate_elite"] = 90,
                            },
                            RewardSetting = new ItemSettings.RewardSettings
                            {
                                typeReward = TypeReward.Command,
                                Command = "say %STEAMID% give ПЕРЕРАБОТЧИК",
                                Balance = 0,
                                CommandList = new List<string> { },
                                ItemRewardSetting = new List<ItemSettings.RewardSettings.ItemRewardSettings> { }
                            }
                        },
                        ["weapons"] = new ItemSettings
                        {
                            DisplayName = "Оружейник",
                            AmountFragmets = 30,
                            SkinID = 2101500148,
                            FragmentSetting = new ItemSettings.FragmentSettings
                            {
                                DisplayName = "Фрагмент Оружейника",
                                SkinID = 2101057796,
                                DropListRare = new Dictionary<string, int>
                                {
                                    ["crate_normal"] = 50,
                                    ["crate_elite"] = 90,
                                }
                            },
                            DropListRareFull = new Dictionary<string, int>
                            {
                                ["crate_normal"] = 50,
                                ["crate_elite"] = 90,
                            },
                            RewardSetting = new ItemSettings.RewardSettings
                            {
                                typeReward = TypeReward.ItemList,
                                Command = "",
                                CommandList = new List<string> { },
                                Balance = 0,
                                ItemRewardSetting = new List<ItemSettings.RewardSettings.ItemRewardSettings>
                                {
                                   new ItemSettings.RewardSettings.ItemRewardSettings
                                   {
                                       DisplayName = "",
                                       Shortname = "rifle.ak",
                                       SkinID = 0,
                                       Amount = 1
                                   },
                                   new ItemSettings.RewardSettings.ItemRewardSettings
                                   {
                                       DisplayName = "",
                                       Shortname = "scrap",
                                       SkinID = 0,
                                       Amount = 1000
                                   },
                                }
                            }
                        },
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
                PrintWarning("Ошибка " + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Data
        [JsonProperty("Фрагменты игроков")] public Dictionary<ulong, Dictionary<string, int>> DataFragments = new Dictionary<ulong, Dictionary<string, int>>();
        void ReadData() => DataFragments = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, int>>>("IQSystemFragments/DataFragments");
        void WriteData() => timer.Every(200f, () => { Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("IQSystemFragments/DataFragments", DataFragments); });
        void RegisteredDataUser(ulong userID)
        {
            if (!DataFragments.ContainsKey(userID))
                DataFragments.Add(userID, new Dictionary<string, int> { });
        }
        void SendData(ulong userID, string Key, int Amount)
        {
            var Fragment = config.ItemSetting[Key];
            if (DataFragments[userID].ContainsKey(Key))
                DataFragments[userID][Key] += Amount;
            else DataFragments[userID].Add(Key, Amount);

            BasePlayer player = BasePlayer.FindByID(userID);
            if (player == null) return;
            SendChat(player, String.Format(lang.GetMessage("NEW_FRAGMENT_USE", this, userID.ToString()), Fragment.DisplayName, (Fragment.AmountFragmets - DataFragments[userID][Key])));
            if (DataFragments[userID][Key] >= Fragment.AmountFragmets)
                FragmentGoToFull(player, Key);
        }
        #endregion

        #region Hooks
        void OnEntitySpawned(BaseNetworkable entity)
        {
            System.Random rnd = new System.Random();
            var values = Enum.GetValues(typeof(TypeItem));
            var myEnumRandom = (TypeItem)values.GetValue(rnd.Next(values.Length));
            SpawnMetods(myEnumRandom, entity);
        }
        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item == null || action == null || action != "crush")
                return null;
            if (player == null)
                return null;

            if (item.info.shortname == ReplaceShortname)
            {
                var Fragment = config.ItemSetting.FirstOrDefault(x => x.Value.FragmentSetting.SkinID == item.skin);
                if (Fragment.Value == null)
                {
                    PrintError("Сообщите разработчику");
                    return false;
                }
                SendData(player.userID, Fragment.Key, 1);
            }
            else if (item.info.shortname == ReplaceShortnameFull)
            {
                var Fragment = config.ItemSetting.FirstOrDefault(x => x.Value.SkinID == item.skin);
                if(Fragment.Value == null)
                {
                    PrintError("Сообщите разработчику");
                    return false;
                }
                UnwrapFragmentFull(player, Fragment.Key);
            }
            NextTick(() => { player.inventory.Take(null, ItemManager.FindItemDefinition("bone.fragments").itemid, 20); });
            ItemRemovalThink(item, player, 1);

            return false;
        }
        private static void ItemRemovalThink(Item item, BasePlayer player, int itemsToTake)
        {
            if (item.amount == itemsToTake)
            {
                item.RemoveFromContainer();
                item.Remove();
            }
            else
            {
                item.amount = item.amount - itemsToTake;
                player.inventory.SendSnapshot();
            }
        }
        private void OnServerInitialized()
        {
            ReadData();
            foreach (var p in BasePlayer.activePlayerList)
                OnPlayerConnected(p);
            WriteData();
        }
        void OnPlayerConnected(BasePlayer player) => RegisteredDataUser(player.userID);
        #endregion

        #region Commands

        [ConsoleCommand("iqsf")]
        void ConsoleCommandIQSF(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length == 0)
            {
                PrintWarning("Ошибка синтаксиса#439");
                PrintToConsole("Ошибка синтаксиса");
                return;
            }
            if (String.IsNullOrEmpty(arg.Args[0]) || arg.Args[0] == null || arg.Args[0].Length == 0)
            {
                PrintWarning("Ошибка синтаксиса.Используйте - iqsf give STEAMID fragments/complete KEY");
                return;
            }
            switch (arg.Args[0].ToLower())
            {
                case "give":
                    {
                        ulong SteamID = ulong.Parse(arg.Args[1]);
                        string CaseKey = arg.Args[2];
                        string Key = arg.Args[3];
                        int Amount = Convert.ToInt32(arg.Args[4]);
                        switch (CaseKey)
                        {
                            case "fragments":
                                {
                                    BasePlayer player = BasePlayer.FindByID(SteamID);
                                    if (player == null)
                                    {
                                        PrintWarning("Игрок не в сети!");
                                        return;
                                    }
                                    var Fragment = config.ItemSetting[Key];
                                    var Fragments = (Item)CreateFragment(Fragment.FragmentSetting.DisplayName, Fragment.FragmentSetting.SkinID, ReplaceShortname, Amount);
                                    player.GiveItem(Fragments);

                                    break;
                                }
                            case "complete":
                                {
                                    BasePlayer player = BasePlayer.FindByID(SteamID);
                                    if (player == null)
                                    {
                                        PrintWarning("Игрок не в сети!");
                                        return;
                                    }
                                    var Fragment = config.ItemSetting[Key];
                                    var FragmentsFull = (Item)CreateFragment(Fragment.DisplayName, Fragment.SkinID, ReplaceShortnameFull, Amount);
                                    player.GiveItem(FragmentsFull);
                                    break;
                                }
                        }
                        break;
                    }
                case "debug":
                    {
                        BasePlayer player = arg.Player();
                        if (player == null)
                        {
                            PrintWarning("Данную команду нужно прописывать в игровой консоли");
                            return;
                        }
                        foreach (var Fragment in config.ItemSetting)
                        {
                            var Fragments = (Item)CreateFragment(Fragment.Value.FragmentSetting.DisplayName, Fragment.Value.FragmentSetting.SkinID, ReplaceShortname, 10);
                            player.GiveItem(Fragments);
                            var FragmentsFull = (Item)CreateFragment(Fragment.Value.DisplayName, Fragment.Value.SkinID, ReplaceShortnameFull, 1);
                            player.GiveItem(FragmentsFull);
                        }
                        break;
                    }
            }
        }

        #endregion

        #region Metods
        void SpawnMetods(TypeItem Types, BaseNetworkable entity)
        {
            int RandomIndex = UnityEngine.Random.Range(0, config.ItemSetting.Count);
            var RandomElement = config.ItemSetting.ElementAt(RandomIndex).Value;
            var DropList = Types == TypeItem.Fragment ? RandomElement.FragmentSetting.DropListRare : RandomElement.DropListRareFull;
            if (DropList == null) return;
            if (!DropList.ContainsKey(entity.ShortPrefabName)) return;
            if (!IsRandom(DropList[entity.ShortPrefabName])) return;

            var Item = Types == TypeItem.Fragment ? (Item)CreateFragment(RandomElement.FragmentSetting.DisplayName, RandomElement.FragmentSetting.SkinID, ReplaceShortname, 1)
                                                  : (Item)CreateFragment(RandomElement.DisplayName, RandomElement.SkinID, ReplaceShortnameFull, 1);
            Item?.MoveToContainer(entity.GetComponent<LootContainer>().inventory);
        }
        void FragmentGoToFull(BasePlayer player, string Key)
        {
            var FragmentFull = config.ItemSetting[Key];
            var FragmentsFull = (Item)CreateFragment(FragmentFull.DisplayName, FragmentFull.SkinID, ReplaceShortnameFull);
            player.GiveItem(FragmentsFull);

            if (DataFragments[player.userID].ContainsKey(Key))
                DataFragments[player.userID].Remove(Key);
            SendChat(player, String.Format(lang.GetMessage("NEW_FRAGMENT_FULL", this, player.UserIDString), FragmentFull.DisplayName));
        }
        void UnwrapFragmentFull(BasePlayer player, string Key)
        {
            var Reward = config.ItemSetting[Key].RewardSetting;
            
            switch(Reward.typeReward)
            {
                case TypeReward.Command:
                    {
                        rust.RunServerCommand(Reward.Command.Replace("%USERID%", player.userID.ToString()));
                        break;
                    }
                case TypeReward.ItemList:
                    {
                        foreach (var Item in Reward.ItemRewardSetting)
                        {
                            Item itemS = ItemManager.CreateByName(Item.Shortname, Item.Amount, Item.SkinID);
                            if (!String.IsNullOrEmpty(Item.DisplayName))
                                itemS.name = Item.DisplayName;

                            if (player.inventory.containerMain.itemList.Count < 24)
                                player.GiveItem(itemS);
                            else itemS.Drop(player.transform.position, Vector3.zero);
                        }
                        break;
                    }
                case TypeReward.IQEconomic:
                    {
                        if (!IQEconomic)
                        {
                            PrintWarning("У вас не установлен IQEconomic,плагин работает неккоректно");
                            return;
                        }
                        SetBalance(player.userID, Reward.Balance);
                        break;
                    }
                case TypeReward.CommandList:
                    {
                        foreach (var Command in Reward.CommandList)
                            rust.RunServerCommand(Command.Replace("%USERID%", player.userID.ToString()));
                        break;
                    }
            }
        }

        #region HelpMetods
        public bool IsRandom(int Rare)
        {
            if (Oxide.Core.Random.Range(0, 100) >= (100 - Rare))
                return true;
            else return false;
        }
        private Item CreateFragment(string DisplayName,ulong SkinID, string Shortname, int Amount = 1)
        {
            Item itemS = ItemManager.CreateByName(Shortname, Amount, SkinID);
            itemS.name = DisplayName;
            itemS.info.stackable = 1;
            return itemS;
        }
        private Item OnItemSplit(Item item, int amount)
        {
            if (plugins.Find("Stacks") || plugins.Find("CustomSkinsStacksFix") || plugins.Find("SkinBox")) return null;
            var Item = config.ItemSetting;
            if (item.info.shortname == ReplaceShortname)
            {
                var Fragment = Item.FirstOrDefault(x => x.Value.FragmentSetting.SkinID == item.skin);
                    if (item.skin == Fragment.Value.FragmentSetting.SkinID)
                    {
                        Item x = ItemManager.CreateByPartialName(ReplaceShortname, amount);
                        x.name = Fragment.Value.FragmentSetting.DisplayName;
                        x.skin = Fragment.Value.FragmentSetting.SkinID;
                        x.amount = amount;
                        item.amount -= amount;
                        return x;
                    }
            }
            else if (item.info.shortname == ReplaceShortnameFull)
            {
                var Fragment = Item.FirstOrDefault(x => x.Value.SkinID == item.skin);

                if (item.skin == Fragment.Value.SkinID)
                    {
                        Item x = ItemManager.CreateByPartialName(ReplaceShortnameFull, amount);
                        x.name = Fragment.Value.DisplayName;
                        x.skin = Fragment.Value.SkinID;
                        x.amount = amount;
                        item.amount -= amount;
                        return x;
                    }
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

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NEW_FRAGMENT_USE"] = "You have successfully used the fragment {0}\nTo complete the set you need {1} more fragments",
                ["NEW_FRAGMENT_FULL"] = "You have successfully assembled the kit {0}",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NEW_FRAGMENT_USE"] = "Вы успешно использовали фрагмент {0}\nДля полного комплекта вам нужно еще {1} фрагментов",
                ["NEW_FRAGMENT_FULL"] = "Вы успешно собрали комплект {0}",

            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion
    }
}
