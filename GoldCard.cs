
using ConVar;
using System;
using Oxide.Core.Plugins;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("GoldCard", "Sempai#3239", "1.0.4")]
    [Description("Самый лучший пионер Sempai#3239")]
    class GoldCard : RustPlugin
    {
        
        
        private Item OnItemSplit(Item item, int amount)
        {
            if (plugins.Find("Stacks") || plugins.Find("CustomSkinsStacksFix") || plugins.Find("SkinBox")) return null;
            if (item.skin == config.SkinID)
            {
                Item x = ItemManager.CreateByPartialName(Shortname, amount);
                x.name = config.DisplayName;
                x.skin = config.SkinID;
                x.amount = amount;
                item.amount -= amount;
                return x;
            }
            return null;
        }

        
                void CreateItem(BasePlayer player)
        {
            Item item = ItemManager.CreateByName(Shortname, 1, config.SkinID);
            item.name = config.DisplayName;
            player.GiveItem(item);
        }

        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, "");
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        private class Configuration
        {
            [JsonProperty("SkinID для предмета")]
            public ulong SkinID;
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    SkinID = 1977450795,
                    DisplayName = "Карта общего доступа",
                    condition = 0.5f,
                };
            }
            [JsonProperty("DisplayName для предмета")]
            public string DisplayName;
            [JsonProperty("Как быстро будет ломаться предмет ? (1.0 стандарт)")]
            public float condition;
        }
        object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin != targetItem.skin) return false;
            return null;
        }

        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.GetItem().skin != targetItem.GetItem().skin) return false;
            return null;
        }

        
                [ConsoleCommand("card")]
        void CardCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = BasePlayer.FindByID(ulong.Parse(arg.Args[0]));
            if (player == null) return;
            CreateItem(player);
            SendChat($"Вы успешно получили {config.DisplayName}", player);
            Puts("Игроку успешно выдана карта!");
        }
		   		 		  						  	   		  		 			   					  	   		   			
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        
        
        private static Configuration config = new Configuration();
                private const string Shortname = "keycard_red";
        protected override void SaveConfig() => Config.WriteObject(config);
		   		 		  						  	   		  		 			   					  	   		   			
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
                PrintWarning("Ошибка #1" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            if (config.condition == 0.0)
                config.condition = 0.5f;
            NextTick(SaveConfig);
        }
        
                [PluginReference] Plugin IQChat;

        private object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            if (card.accessLevel != cardReader.accessLevel && card.skinID == config.SkinID)
            {
                var cards = card.GetItem();
                if (cards == null || cards.conditionNormalized <= 0.0)
                    return null;

                cardReader.Invoke(new Action(cardReader.GrantCard), 0.5f);
                cards.LoseCondition(config.condition);
                SendChat("Вы успешно получили доступ к двери", player);
                return true;
            }
            return null;
        }
            }
}
