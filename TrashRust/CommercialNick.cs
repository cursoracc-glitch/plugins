using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using ConVar;

namespace Oxide.Plugins
{
    [Info("CommercialNick+", "DezLife", "2.5.13")]
    [Description("Плагин позволяющий давать награду за приставку в нике , например название вашего сервера")]

    class CommercialNick : RustPlugin
    {
        [PluginReference] Plugin IQChat;

        #region config
        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Наградить за что то в нике:")]
            public List<string> CONF_BlockedParts;
            [JsonProperty("Настройки")]
            public setings seting;
        }

        public class setings
        {
            [JsonProperty("Использовать выдачу баланса ?")]
            public bool GameStore;
            [JsonProperty("Бонус в виде баланса GameStores или OVH (если не нужно оставить пустым)")]
            public string GameStoreBonus;
            [JsonProperty("У вас магазин ОВХ?")]
            public bool OVHStore;
            [JsonProperty("Store id")]
            public string storeid;
            [JsonProperty("Store key")]
            public string storekey;
            [JsonProperty("Лог сообщения(Показывается в магазине после выдачи в истории. Если OVH оставить пустым)")]
            public string GameStoreMSG;
            [JsonProperty("Использовать выдачу привилегии")]
            public bool commands;
            [JsonProperty("Команда для выдачи")]
            public string commandsgo;
            [JsonProperty("Названия того что он получит от команды")]
            public string commandprize;
            [JsonProperty("Время которое нужно отыграть игроку с приставкой в нике что бы получить награду (секунды)")]
            public int timeplay;
            [JsonProperty("Разршить после вайпа получить приз заново")]
            public bool wipeclear;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration()
            {
                CONF_BlockedParts = new List<string>()
                {
                   "PONYLAND",
                   "PONY LAND",
                   "pony land",
                   "ponyland",
                   "DURACHOCK",
                   "Pony Land",
                },
                seting = new setings
                {
                    GameStore = false,
                    GameStoreBonus = "",
                    OVHStore = false,
                    storeid = "storeid",
                    storekey = "storekey",
                    GameStoreMSG = "За заход после вайпа:3",
                    commands = true,
                    commandsgo = "addgroup %STEAMID% vip 226d",
                    commandprize = "vip",
                    timeplay = 500,
                    wipeclear = true,
                },
            };
            SaveConfig(config);
        }

        void SaveConfig(Configuration config)
        {
            Config.WriteObject(config, true);
            SaveConfig();
        }

        public void LoadConfigVars()
        {
            config = Config.ReadObject<Configuration>();
            Config.WriteObject(config, true);
        }

        #endregion

        #region data
        private Dictionary<ulong, bool> ConnectedPlayers = new Dictionary<ulong, bool>();
        #endregion

        void OnNewSave(string filename)
        {
            if (config.seting.wipeclear)
            {
                ConnectedPlayers.Clear();
                PrintWarning("Обнаружен WIPE . Дата игроков сброшена");
            }
        }

        private void OnServerInitialized()
        {
            #region Load Config / Lang
            LoadConfigVars();
            #endregion

            if (Interface.Oxide.DataFileSystem.ExistsDatafile("CommercialNick/PlayerInfo"))
                ConnectedPlayers = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>("CommercialNick/PlayerInfo");

            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerConnected(BasePlayer.activePlayerList[i]);
            }

            #region info   

            if (!string.IsNullOrEmpty(config.seting.GameStoreBonus) && !config.seting.OVHStore)
            {
                if (config.seting.storeid == "storeid" || config.seting.storekey == "storekey")
                {
                    PrintError("Вы не настроили ID И KEY от магазина GameStores");
                }
            }
            #endregion
        }

        void OnServerSave()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("CommercialNick/PlayerInfo", ConnectedPlayers);

        }
        private void Unload()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("CommercialNick/PlayerInfo", ConnectedPlayers);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (!ConnectedPlayers.ContainsKey(player.userID))
            {
                ConnectedPlayers.Add(player.userID, false);
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("CommercialNick/PlayerInfo", ConnectedPlayers);
            }

            NextTick(() => {
                NickName(player);
            });
        }

        void NickName(BasePlayer player)
        {
            if (!ConnectedPlayers.ContainsKey(player.userID))
            {
                OnPlayerConnected(player);
                return;
            }
            if (ContainsAny(player.displayName, config.CONF_BlockedParts) && ConnectedPlayers[player.userID] == false)
            {
                timer.Once(config.seting.timeplay, () =>
                {
                    PrizeGive(player.userID);
                });
            }
        }

        public void PrizeGive(ulong id)
        {
            if (ConnectedPlayers[id])
                return;

            BasePlayer player = BasePlayer.FindByID(id);
            if (config.seting.commands)
            {
                ConnectedPlayers[player.userID] = true;
                Server.Command(config.seting.commandsgo.Replace("%STEAMID%", player.userID.ToString()));
                LogToFile("ConnectPlayer", $" [{player.userID}] получил {config.seting.commandsgo}", this);
                SendChat($"Вы получили награду в виде {config.seting.commandprize}. Награда за приставку в нике", player);
            }
            try
            {
                if (!config.seting.OVHStore)
                {
                    webrequest.Enqueue($"https://gamestores.ru/api?shop_id={config.seting.storeid}&secret={config.seting.storekey}&action=moneys&type=plus&steam_id={id}&amount={config.seting.GameStoreBonus}&mess={config.seting.GameStoreMSG}", null, (i, s) =>
                    {
                        if (i != 200)
                        {
                        }
                        if (s.Contains("success"))
                        {
                            ConnectedPlayers[id] = true;
                            LogToFile("ConnectPlayer", $" [{id}] получил {config.seting.GameStoreBonus} рублей", this);
                            SendChat($"Вы получили награду в виде {config.seting.GameStoreBonus} рублей. Награда за приставку в нике", player);
                        }
                        if (s.Contains("fail"))
                        {
                            SendChat("Вы не получили приз за приставку в ники т.к не авторизованы в магазине.", player);
                            return;
                        }
                    }, this);
                }
                else if (config.seting.OVHStore)
                {
                    plugins.Find("RustStore").CallHook("APIChangeUserBalance", id, config.seting.GameStoreBonus, new Action<string>((result) =>
                    {
                        if (result == "SUCCESS")
                        {
                            ConnectedPlayers[id] = true;
                            LogToFile("ConnectPlayer", $" [{id}] получил {config.seting.GameStoreBonus} рублей", this);
                            SendChat($"Вы получили награду в виде {config.seting.GameStoreBonus} рублей. Награда за приставку в нике", player);
                        }
                        else
                        {
                            SendChat("Вы не получили приз за приставку в ники т.к не авторизованы в магазине.", player);
                            return;
                        }
                    }));
                }
            }
            catch(Exception ex)
            {
                LogToFile(Title, ex.ToString(), this);
            }
            
        }


        #region helpers

        #region ReplyMSG

        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion

        private bool ContainsAny(string input, List<string> check)
        {
            foreach (var str in check)
            {
                var word = str.ToLower();
                if (input.ToLower().Contains(word))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
