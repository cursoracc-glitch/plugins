using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using ConVar;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("XDGoldenskull", "Sempai#3239", "1.0.9")]
    public class XDGoldenskull : RustPlugin
    {


        
        
        [ChatCommand("g.give")]
        private void cmdChatEmerald(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            config.CreateItem(player, Vector3.zero, 10);
        }
        [PluginReference] Plugin IQChat;
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, "");
            else
                player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        object CanBeRecycled(Item item, Recycler recycler)
        {
            if (item == null)
                return false;
            if (item.info.shortname == ReplaceShortName && item.skin == config.ReplaceID)
                return true;
            return null;
        }
        private class Configuration
        {

            public void CreateItem(BasePlayer player, Vector3 position, int amount)
            {
                Item x = ItemManager.CreateByPartialName(ReplaceShortName, amount);
                x.skin = ReplaceID;
                x.name = DisplayName;
                x.info.stackable = StackItem;

                if (player != null)
                {
                    if (player.inventory.containerMain.itemList.Count < 24)
                        x.MoveToContainer(player.inventory.containerMain);
                    else
                        x.Drop(player.transform.position, Vector3.zero);
                    return;
                }

                if (position != Vector3.zero)
                {
                    x.Drop(position, Vector3.down);
                    return;
                }
            }
            [JsonProperty("Стак предмета")]
            public int StackItem;
            [JsonProperty("Отображаемое имя")]
            public string DisplayName;
            [JsonProperty("Из каких бочек будет падать и процент выпадения")]
            public Dictionary<string, int> barellList = new Dictionary<string, int>();
            [JsonProperty("Призы за переработку")]
            public List<string> itemsrec = new List<string>();
            [JsonProperty("Призы за потрошения")]
            public List<string> itempot = new List<string>();

            public int GetItemId() => ItemManager.FindItemDefinition(ReplaceShortName).itemid;
            [JsonProperty("Скин ID черепа")]
            public ulong ReplaceID;
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    DisplayName = "Золотой череп",
                    StackItem = 5,
                    ReplaceID = 1683645276,
                    barellList = new Dictionary<string, int>
                    {
                        ["loot-barrel-1"] = 50,
                        ["loot-barrel-2"] = 20,
                    },
                    cratelList = new Dictionary<string, int>
                    {
                        ["bradley_crate"] = 50,
                        ["codelockedhackablecrate_oilrig"] = 20,
                        ["crate_elite"] = 20,
                    },
                    itemsrec = new List<string>
                    {
                        "weapon.mod.small.scope",
                        "rifle.ak",
                        "rifle.l96",
                        "smg.thompson",
                        "rifle.semiauto",
                        "pistol.revolver",
                        "rifle.lr300",
                    },
                    itempot = new List<string>
                    {
                        "shotgun.double",
                        "grenade.f1",
                        "smg.2",
                        "shotgun.pump",
                        "pistol.semiauto",
                        "pistol.python",
                        "weapon.mod.lasersight",
                        "weapon.mod.muzzlebrake",
                    },
                };
            }
            [JsonProperty("Из каких ящиков будет падать и процент выпадения")]
            public Dictionary<string, int> cratelList = new Dictionary<string, int>();

            public Item Copy(int amount = 1)
            {
                Item x = ItemManager.CreateByPartialName(ReplaceShortName, amount);
                x.skin = ReplaceID;
                x.name = DisplayName;
                x.info.stackable = StackItem;

                return x;
            }
        }
        private const string ReplaceShortName = "skull.human";

        
               
        void OnServerInitialized() => lootContainerList = config.cratelList.Concat(config.barellList);

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
                PrintWarning("Ошибка #495" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        
                private Item CreateItem()
        {
            return config.Copy(1);
        }

        private static System.Random random = new System.Random();
        private IEnumerable<KeyValuePair<string, int>> lootContainerList = null;
        private void OnLootSpawn(LootContainer container)
        {
            if (container == null || lootContainerList == null)
                return;
		   		 		  						  	   		  		 			  	  			  						  		  
            foreach (var crate in lootContainerList)
            {
                if (container.PrefabName.Contains(crate.Key))
                {
                    if (random.Next(0, 100) >= (100 - crate.Value))
                    {
                        InvokeHandler.Instance.Invoke(() =>
                        {
                            if (container.inventory.capacity <= container.inventory.itemList.Count)
                            {
                                container.inventory.capacity = container.inventory.itemList.Count + 1;
                            }
                            Item item = (Item)CreateItem();
                            item?.MoveToContainer(container.inventory);
                        }, 0.21f);
                    }
                }
            }
        }

        object OnRecycleItem(Recycler recycler, Item item)
        {
            if (item.info.shortname == ReplaceShortName && item.skin == config.ReplaceID)
            {
                item.UseItem(1);
                int RandomItem = random.Next(config.itemsrec.Count);
                recycler.MoveItemToOutput(ItemManager.CreateByName(config.itemsrec[RandomItem], 1));
                return true;
            }
            return null;
        }
		   		 		  						  	   		  		 			  	  			  						  		  
        [ConsoleCommand("goldenskul")]
        void FishCommand(ConsoleSystem.Arg arg)
        {

            BasePlayer player = BasePlayer.Find(arg.Args[0]);
            if (player == null || !player.IsConnected)
            {
                Puts("Игрок не найден");
                return;
            }
            int count = int.Parse(arg.Args[1]);
            config.CreateItem(player, Vector3.zero, count);
            SendChat(player, $"Вы успешно получили {config.DisplayName}");
            Puts($"Игроку выдана {config.DisplayName}");
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (action == "crush" && item.skin == config.ReplaceID)
            {
                Item itemS = ItemManager.CreateByName(config.itempot[random.Next(config.itempot.Count)], 1, 0);
                player.GiveItem(itemS, BaseEntity.GiveItemReason.PickedUp);
                ItemRemovalThink(item, player, 1);
                Interface.CallHook("OnSkullOpen", player);
                return false;
            }
            return null;
        }
        object CanRecycle(Recycler recycler, Item item)
        {
            if (item.info.shortname == ReplaceShortName && item.skin == config.ReplaceID)
                return true;
            return null;
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

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();

        
        private static Configuration config = new Configuration();
        protected override void SaveConfig() => Config.WriteObject(config);
            }
}
