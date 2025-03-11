

using System;
using System.Collections.Generic;
using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("StormLogo", "Ryamkk", "2.2.0")]
    public class LogoStorm : RustPlugin
    {
        [PluginReference] private Plugin ImageLibrary;

         string logo = "https://imgur.com/Ao6JTUA.png";
         string open = "https://i.imgur.com/kVda4YB.png";
       
         public static string Commnad1 = "/stat";
         public static string Commnad2 = "/store";
         public static string Commnad3 = "/block";
         public static string Commnad4 = "/report";
         
        
        #region ImageLibrary
        
        
        private string GetImage(string fileName, ulong skin = 0)
        {
            var imageId = (string)plugins.Find("ImageLibrary").CallHook("GetImage", fileName, skin);
            if (!string.IsNullOrEmpty(imageId))
                return imageId;
            return string.Empty;
        }
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        
        

        void LoadImage()
        {
            AddImage(logo, "logo");
            AddImage(open, "open");
        }
        
        #endregion
        
        private const string Layer = "UI.Menu";
        
        List<string> MSGList = new List<string>
        {
            "Максимум человек в команде - <color=#8e6874>[ 4 ]</color>", 
            "Наша группа ВК - <color=#8e6874>vk.com/storm.rust</color>", 
            "Магазин/сайт сервера - <color=#8e6874>stormrust.store</color>",
            "Включи оповощение о рейде! - <color=#8e6874>/raid</color>",    
            "Отобразить статистику игроков <color=#8e6874>/stat</color>", 
            "<color=#8e6874>/report</color> - отправить жалобу на игрока"
        };

        private PluginData _data;

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
        }

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (_data == null) _data = new PluginData();
        }

        private class PluginData
        {
            [JsonProperty(PropertyName = "Hided Players", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> HidedPlayers = new List<ulong>();

            public bool IsHided(BasePlayer player)
            {
                return HidedPlayers.Contains(player.userID);
            }

            public bool ChangeStatus(BasePlayer player)
            {
                if (player == null) return false;

                if (IsHided(player))
                {
                    HidedPlayers.Remove(player.userID);
                    return false;
                }

                HidedPlayers.Add(player.userID);
                return true;
            }
        }

        void Loaded()
        {

        }

        #region command

        [ChatCommand("online")]
        void CommanndOnline(BasePlayer sender, string command, string[] args) => CommandPlayers(sender, command, args);

        [ChatCommand("player")]
        void CommandPlayer(BasePlayer sender, string command, string[] args) => CommandPlayers(sender, command, args);

        [ChatCommand("players")]
        void CommandPlayers(BasePlayer sender, string command, string[] args)
        {
            string playerList = ("<color=#8e6874>Онлайн игроки:</color>");

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                playerList += $"\n- {player.displayName}";

            if (BasePlayer.activePlayerList.Count < 20)
                PrintToChat(sender, playerList);
            else
                PrintToChat(sender, "Список игроков слишком длинный для чата. \n Нажмите F1, затем снова введите /players в чате, затем F1, чтобы посмотреть полный список игроков.");
            sender.Command("echo", $"{playerList}");
        }

        #endregion

        private void OnServerInitialized()
        {
            LoadData();
            LoadImage();
            timer.Every(20f, () =>
            {
                foreach(var player in BasePlayer.activePlayerList)
                {
                    DrawLower(player);
                }
            });


            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }

            AddCovalenceCommand("hide", nameof(CmdMenuHide));
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
            
            SaveData();
        }

        void OnClientAuth(Connection connection)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                timer.In(0.21f, RefreshOnline);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;

            if (player.IsReceivingSnapshot)
            {
                timer.Once(2, () => OnPlayerConnected(player));
                return;
            }

            MainUi(player);
            DrawLower(player);
            RefreshOnline();
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            timer.In(0.21f, RefreshOnline);
        }

        private void CmdMenuHide(IPlayer cov, string command, string[] args)
        {
            var player = cov?.Object as BasePlayer;
            if (player == null) return;

            _data.ChangeStatus(player);

            MainUi(player);
        }

        private void MainUi(BasePlayer player)
        {
            var container = new CuiElementContainer();
            

            var SleepingPlayer = BasePlayer.sleepingPlayerList.Count;
            var OnlinePlayer = BasePlayer.activePlayerList.Count;
            var JoiningPlayer = ServerMgr.Instance.connectionQueue.Joining;
            
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -68", OffsetMax = "190 -1" }
            }, "Hud", Layer);

            if (!_data.IsHided(player))
            {
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name = Layer + ".Image",
                    FadeOut = 1f,
                    Components =
                    {
                        new CuiImageComponent {Png = GetImage("open") ,  Material = "assets/icons/greyout.mat" },
                        new CuiRectTransformComponent { AnchorMin = "0.2894738 0.175", AnchorMax = "0.9421054 0.8532338"}
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.1732883 0.5392601", AnchorMax = "0.2969442 0.8725935" },
                    Text = 
                    {
                        Text = $"{JoiningPlayer + OnlinePlayer}", Color = HexToRustFormat("#9e5167FF"), FontSize = 9, 
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" 
                    }
                }, Layer + ".Image", Layer + ".Online");
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.7324276 0.5537528", AnchorMax = "0.8560835 0.8870862" },
                    Text = { Text = $"{SleepingPlayer}", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" }
                }, Layer + ".Image", Layer + ".Sleepers");
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.4474816 0.5537528", AnchorMax = "0.5711375 0.8870862" },
                    Text = { Text = $"{JoiningPlayer}", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" }
                }, Layer + ".Image", Layer + ".Joining");
                
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"chat.say {Commnad1}" },
                    RectTransform = { AnchorMin = "0.188908 0.1773738", AnchorMax = "0.2856822 0.4382434" },
                    Text = { Text = "" }
                }, Layer + ".Image", Layer + ".Image.1");
            
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"chat.say {Commnad2}" },
                    RectTransform = { AnchorMin = "0.3609509 0.1773738", AnchorMax = "0.457725 0.4382434" },
                    Text = { Text = "" }
                }, Layer + ".Image", Layer + ".Image.2");
            
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"chat.say {Commnad3}" },
                    RectTransform = { AnchorMin = "0.5329937 0.1773738", AnchorMax = "0.6297679 0.4382434" },
                    Text = { Text = "" }
                }, Layer + ".Image", Layer + ".Image.3");
            
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = $"chat.say {Commnad4}" },
                    RectTransform = { AnchorMin = "0.7050366 0.1918665", AnchorMax = "0.8018107 0.4527361" },
                    Text = { Text = "" }
                }, Layer + ".Image", Layer + ".Image.4");
            }

            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".SummerImage",
                Components =
                {
                    new CuiImageComponent { Png = GetImage("logo") , Material = "assets/icons/greyout.mat" },
                    new CuiRectTransformComponent { AnchorMin = "-0.03859668 -0.08706465", AnchorMax = "0.3578931 1.186567" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.96 1" },
                Text = { Text = "" },
                Button = { Color = "0 0 0 0", Command = "hide" }
            }, Layer + ".SummerImage");

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);

        }

        private void RefreshOnline()
        {
            var container = new CuiElementContainer();
                
            var SleepingPlayer = BasePlayer.sleepingPlayerList.Count;
            var OnlinePlayer = BasePlayer.activePlayerList.Count;
            var JoiningPlayer = ServerMgr.Instance.connectionQueue.Joining;

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.1732883 0.5392601", AnchorMax = "0.2969442 0.8725935" },
                    Text = 
                    {
                        Text = $"{JoiningPlayer + OnlinePlayer}", Color = HexToRustFormat("#9e5167FF"), FontSize = 9, 
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" 
                    }
                }, Layer + ".Image", Layer + ".Online");
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.7324276 0.5537528", AnchorMax = "0.8560835 0.8870862" },
                    Text = { Text = $"{SleepingPlayer}", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" }
                }, Layer + ".Image", Layer + ".Sleepers");
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.4474816 0.5537528", AnchorMax = "0.5711375 0.8870862" },
                    Text = { Text = $"{JoiningPlayer}", FontSize = 9, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" }
                }, Layer + ".Image", Layer + ".Joining");

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (_data.IsHided(player)) continue;

                CuiHelper.DestroyUi(player, Layer + ".Online");
                CuiHelper.DestroyUi(player, Layer + ".Sleepers");
                CuiHelper.DestroyUi(player, Layer + ".Joining");
                CuiHelper.AddUi(player, container);
            }
        }

        private void DrawLower(BasePlayer player)
        {
            var container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                Image = { Color = HexToRustFormat("#00000000") },
                RectTransform = { AnchorMin = "0.3462664 0.003906242", AnchorMax = "0.6449488 0.02473958" },
                CursorEnabled = false,
            }, "Under", "DVLower");
            
            container.Add(new CuiElement
            {
                Parent = "DVLower",
                Components =
                {
                    new CuiTextComponent { Text = MSGList.GetRandom(), Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "RobotoCondensed-regular.ttf"},
                    new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.5 0.5" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            CuiHelper.DestroyUi(player, "DVLower");
            CuiHelper.AddUi(player, container);
        }

        private static string HexToRustFormat(string hex)
        { 
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
    }
}