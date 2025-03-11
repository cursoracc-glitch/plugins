using System.Collections.Generic;
using System;
using ConVar;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using Rust;

namespace Oxide.Plugins
{
    [Info("XDWipeReward", "TopPlugin.ru", "1.2.3")]
    [Description("Награда первым N игрокам после вайпа")]
    public class XDWipeReward : RustPlugin
    {

        private void LoadPlayerData() => playersInfo = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, bool>>(Name);

        void OnNewSave(string filename)
        {
            playersInfo.Clear();
            Wipe = true;
        }



                public static string WipeR = "WipeR_CUI";

        public class Setings
        {

            [JsonProperty("Команда для выдачи приза (если не нужно то оставить поля пустым)")]
            public string CommandPrize;

            [JsonProperty("У вас магазин ОВХ?")]
            public bool OVHStore;
            [JsonProperty("API KEY Магазина(GameStore. Если OVH оставить пустым)")]
            public string Store_Key;
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
            [JsonProperty("Бонус в виде баланса GameStores или OVH (если не нужно оставить пустым)")]
            public string GameStoreBonus;

            [JsonProperty("Id Магазина(GameStore. Если OVH оставить пустым)")]
            public string Store_Id;
            [JsonProperty("Количество игроков")]
            public int PlayersIntConnect;

            [JsonProperty("Лог сообщения(Показывается в магазине после выдачи в истории. Если OVH оставить пустым)")]
            public string GameStoreMSG;
        }
                public Configuration config;
        private void Unload()
        {
            SavePlayerData();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, WipeR);
        }
        bool Wipe = false;
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
        
                private Dictionary<ulong, bool> playersInfo = new Dictionary<ulong, bool>();
        
        
        public void WipeRewardGui(BasePlayer p)
        {
            CuiHelper.DestroyUi(p, WipeR);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-175 -340", OffsetMax = "-1 -280" },
                Image = { Color = "0 0 0 0.4", Material = "assets/content/ui/uibackgroundblur.mat", Sprite = "assets/content/ui/ui.background.tiletex.psd" }
            }, "Overlay", WipeR);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.1973175 0.011111457", AnchorMax = "0.7988508 0.3999999" },
                Button = { Command = "giveprize", Color = HexToRustFormat("#71FF9A9A") },
                Text = { Text = "Забрать награду", Align = TextAnchor.MiddleCenter, FontSize = 13 }
            }, WipeR);
		   		 		  						  	   		  		 			  	 	 		  	 	 		  	  	
                        container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.4444442", AnchorMax = "1 1" },
                Text = { Text = $"Вы {Players.Count} из {config.setings.PlayersIntConnect}\n Поэтому получаете награду", FontSize = 13, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }

            }, WipeR);

            
            CuiHelper.AddUi(p, container);
        }

        
        
        List<ulong> Players = new List<ulong>();

        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, "");
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (Wipe)
            {
                if (playersInfo.Count >= config.setings.PlayersIntConnect)
                {
                    Wipe = false;
                    return;
                }
                if (!playersInfo.ContainsKey(player.userID))
                {
                    playersInfo.Add(player.userID, false);
                    WipeRewardGui(player);
                }
            }
        }

        void GiveReward(ulong ID)
        {
            if (!config.setings.OVHStore)
            {
                string url = $"https://gamestores.ru/api?shop_id={config.setings.Store_Id}&secret={config.setings.Store_Key}&action=moneys&type=plus&steam_id={ID}&amount={config.setings.GameStoreBonus}&mess={config.setings.GameStoreMSG}";
                webrequest.Enqueue(url, null, (i, s) =>
                {
                    if (i != 200) { }
                    if (s.Contains("success"))
                    {
                        PrintWarning($"Игрок [{ID}] зашел 1 из первых, и получил бонус в нашем магазине. В виде [{config.setings.GameStoreBonus} руб]");
                    }
                    else
                    {
                        PrintWarning($"Игрок {ID} проголосовал за сервер, но не авторизован в магазине.");
                    }
                }, this);
            }
            else
            {
                plugins.Find("RustStore").CallHook("APIChangeUserBalance", ID, config.setings.GameStoreBonus, new Action<string>((result) =>
                {
                    if (result == "SUCCESS")
                    {
                        PrintWarning($"Игрок [{ID}] зашел 1 из первых, и получил бонус в нашем магазине. В виде [{config.setings.GameStoreBonus} руб]");
                        return;
                    }
                    PrintWarning($"Игрок {ID} проголосовал за сервер, но не авторизован в магазине. Ошибка: {result}");
                }));
            }
        }
        
        private void Init() => LoadPlayerData();
        [PluginReference] Plugin IQChat;


        protected override void LoadDefaultConfig()
        {
            config = new Configuration()
            {
                setings = new Setings
                {
                    PlayersIntConnect = 100,
                    CommandPrize = "say %STEAMID%",
                    OVHStore = false,
                    GameStoreBonus = "",
                    GameStoreMSG = "За заход после вайпа:3",
                    Store_Id = "ID",
                    Store_Key = "KEY"

                }
            };
            SaveConfig(config);
        }

        [ConsoleCommand("giveprize")]
        void GivePrize(ConsoleSystem.Arg arg)
        {
            BasePlayer p = arg.Player();
            CuiHelper.DestroyUi(p, WipeR);
            if (!playersInfo.ContainsKey(p.userID))
                return;
            if (playersInfo[p.userID] == false)
            {
                if (!string.IsNullOrEmpty(config.setings.CommandPrize))
                {
                    Server.Command(config.setings.CommandPrize.Replace("%STEAMID%", p.UserIDString));
                }
                if (!string.IsNullOrEmpty(config.setings.GameStoreBonus))
                {
                    GiveReward(p.userID);
                }
                playersInfo[p.userID] = true;
                SendChat(p, "Вы успешно <color=#A1FF919A>забрали награду</color>!");
            }          
        }

        public class Configuration
        {
            [JsonProperty("Настройки")]
            public Setings setings;
        }

        public void LoadConfigVars()
        {
            config = Config.ReadObject<Configuration>();
            Config.WriteObject(config, true);
        }
        private void SavePlayerData() => Interface.GetMod().DataFileSystem.WriteObject(this.Name, playersInfo);
        private void OnServerInitialized()
        {
            LoadConfigVars();

            if (!string.IsNullOrEmpty(config.setings.GameStoreBonus) && !config.setings.OVHStore)
            {
                if (config.setings.Store_Id == "ID" || config.setings.Store_Key == "KEY")
                {
                    NextTick(() =>
                    {
                        PrintError("Вы не настроили ID И KEY от магазина GameStores");
                        Interface.Oxide.UnloadPlugin(Name);
                    });
                    return;
                }
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        void SaveConfig(Configuration config)
        {
            Config.WriteObject(config, true);
            SaveConfig();
        }
            }
}
