using System;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Oxide.Core.Plugins;
using ConVar;
using Oxide.Core.Libraries.Covalence;
using System.Linq;
//using Oxide.Plugins.BossMonsterExtensionMethods;

namespace Oxide.Plugins
{
    [Info("XDWipeReward", "DezLife / Redesign by Deversive", "0.1.0")]
    [Description("Награда первым N игрокам после вайпа")]
    public class XDWipeReward : RustPlugin
    {
        [PluginReference] Plugin ImageLibrary;
        #region Config
        public Configuration config;

        public class Setings
        {
            [JsonProperty("Количество игроков")]
            public int PlayersIntConnect;

            [JsonProperty("Бонус в виде баланса GameStores или OVH (если не нужно оставить пустым)")]
            public string GameStoreBonus;

            [JsonProperty("Лог сообщения(Показывается в магазине после выдачи в истории. Если OVH оставить пустым)")]
            public string GameStoreMSG;

            [JsonProperty("Id Магазина(GameStore. Если OVH оставить пустым)")]
            public string Store_Id;
            [JsonProperty("API KEY Магазина(GameStore. Если OVH оставить пустым)")]
            public string Store_Key;
        }

        public class Configuration
        {
            [JsonProperty("Настройки")]
            public Setings setings;
        }


        protected override void LoadDefaultConfig()
        {
            config = new Configuration()
            {
                setings = new Setings
                {
                    PlayersIntConnect = 15,
                    GameStoreBonus = "15",
                    GameStoreMSG = "За заход после вайпа:3",
                    Store_Id = "39288",
                    Store_Key = "b8472007708c7b866d66ab77f4cbee1a"

                }
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

        
        #region Data

        
        
        #endregion
        
        
        #region ImageLibrary

        [ChatCommand("testui123123")]
        void testuiWipe(BasePlayer player)
        {
            Puts("1");
            WipeRewardGUIZ(player);
        }
        
        
        /*private string mainui1 = "https://imgur.com/r0cZ12a.png";
        private string close = "https://imgur.com/MFS0gCS.png";*/
        
        
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);

        private string nagradifon = "https://imgur.com/jPgI9Rg.png";
        private string buttonzab = "https://imgur.com/gtAJYX1.png";

        void LoadImage()
        {
            AddImage(nagradifon, "nagradifon");
            AddImage(buttonzab, "buttonzab");
            /*AddImage(mainui1, "fon");
            AddImage(close, "close");*/
        }
        
        #endregion
        
        
        private void OnServerInitialized()
        {
            LoadConfigVars();
            LoadImage();
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerConnected(BasePlayer.activePlayerList[i]);
            }

            if (!string.IsNullOrEmpty(config.setings.GameStoreBonus))
            {
                if (config.setings.Store_Id == "ID" || config.setings.Store_Key == "KEY")
                {
                    PrintError("Вы не настроили ID И KEY от магазина GameStores");
                    return;
                }
            }
        }

        void OnNewSave(string filename)
        {
            Wipe = true;
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }

            if (Wipe)
            {
                if (Players.Count >= config.setings.PlayersIntConnect)
                {
                    Wipe = false;
                    return;
                }
                if (!Players.Contains(player.userID))
                {
                    Players.Add(player.userID);

                    WipeRewardGUIZ(player);
                }
            }
        }

        [ChatCommand("rewardclose")]
        void closeRewardUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, mainui1);
            CuiHelper.DestroyUi(player, "mainui6");
        }
        
        [ConsoleCommand("bxckjaklsdjaslkxzcasdxzcjasdxzcjasdzxckjasdjzxcasdzxc")]
        void GivePrize(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (!string.IsNullOrEmpty(config.setings.GameStoreBonus))
            {
                GiveReward(player);
            }
        }
        

        void GiveReward(BasePlayer player)
        {
           
                string url = $"https://gamestores.app/api?shop_id={config.setings.Store_Id}&secret={config.setings.Store_Key}&action=moneys&type=plus&steam_id={player.userID}&amount={config.setings.GameStoreBonus}&mess={config.setings.GameStoreMSG}";
                webrequest.Enqueue(url, null, (i, s) =>
                {
                    if (i != 200) { }
                    if (s.Contains("success"))
                    {
                        CuiHelper.DestroyUi(player, mainui1);
                        CuiHelper.DestroyUi(player, "mainui6");
                        SendChat(player, "Вы успешно забрали награду, удачной игры на нашем сервере");
                        PrintWarning($"Игрок [{player.userID}] зашел 1 из первых, и получил бонус в нашем магазине. В виде [{config.setings.GameStoreBonus} руб]");
                    }
                    else
                    {
                        SendChat(player, "Вы не авторизированы в магазине, чтобы забрать приз авторизуйтесь в магазине, у вас есть 5 минут");
                        PrintWarning($"Игрок {player.userID} проголосовал за сервер, но не авторизован в магазине.");
                    }
                }, this);
        }

       void Unload(BasePlayer player)
       {
           CuiHelper.DestroyUi(player, "mainui6");
           CuiHelper.DestroyUi(player, mainui1);
       }

        #region Parent
        public static string mainui1 = "mainui6";
        #endregion

        #region GUI

        void WipeRewardGUIZ(BasePlayer player)
        {

            timer.Once(300f, () => { CuiHelper.DestroyUi(player, mainui1); });
            timer.Once(300f, () => { CuiHelper.DestroyUi(player, "mainui6"); });
            
            CuiElementContainer container = new CuiElementContainer();
                
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.3222221", AnchorMax = "0.1723957 0.4333335" },
                Image = { FadeIn = 1f, Color = "0 0 0 0", }
            },  "Hud", mainui1);
            
            container.Add(new CuiElement
            {
                Parent = "mainui6",
                FadeOut = 1f,
                //Name = mainui + "mainui6",
                Components =
                {
                    new CuiImageComponent { Png = GetImage("nagradifon") , Material = "assets/icons/greyout.mat", },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "mainui6",
                FadeOut = 1f,
                //Name = mainui + "mainui6",
                Components =
                {
                    new CuiTextComponent { Text = "ОДИН ИЗ ПЕРВЫХ!", Color = HexToRustFormat("#CAD5DF"),  Align = TextAnchor.UpperLeft, FontSize = 12, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.07902735 0.5499993", AnchorMax = "0.8267483 0.833331" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "mainui6",
                FadeOut = 1f,
                //Name = mainui + "mainui6",
                Components =
                {
                    new CuiTextComponent { Text = $"Вы {Players.Count} из {config.setings.PlayersIntConnect} зашедших игроков на сервер и поэтому получаете бонус в виде {config.setings.GameStoreBonus} рублей на баланс магазина", Color = HexToRustFormat("#8E8E8E") ,Align = TextAnchor.UpperLeft, FontSize = 9, Font = "robotocondensed-regular.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0.07902735 0.2250011", AnchorMax = "0.7234047 0.6416651" }
                }
            });
            
            
            container.Add(new CuiElement
            {
                Parent = "mainui6",
                //Name = mainui + "mainui6",
                Components =
                {
                    new CuiImageComponent { Png = GetImage("buttonzab") , Material = "assets/icons/greyout.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0.3383688 0.08333409", AnchorMax = "0.5770397 0.2499999" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3383688 0.08333409", AnchorMax = "0.5770397 0.2499999" },
                Button = { Command = "bxckjaklsdjaslkxzcasdxzcjasdxzcjasdzxckjasdjzxcasdzxc", Color = HexToRustFormat("#CAD5DF00") },
                Text = { Text = "Забрать", Align = TextAnchor.MiddleCenter, FontSize = 10 }
            }, "mainui6");
            
            
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Help

        List<ulong> Players = new List<ulong>();
        bool Wipe = false;
        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        public void SendChat(BasePlayer player, string Message)
        {
            SendReply(player, Message);
        }

        #endregion

    }
}
