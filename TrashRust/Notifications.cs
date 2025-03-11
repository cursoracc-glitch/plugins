using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Notifications", "VooDoo", "1.0.0")]
    [Description("Notifications Api")]
    public class Notifications : RustPlugin
    {
        class UINotification
        {
            public string text { get; set; }

            public UINotification(string text)
            {
                this.text = text;
            }
        }

        private static Dictionary<ulong, DateTime> hideList = new Dictionary<ulong, DateTime>();

        #region API
        bool AddInHideList(ulong userID)
        {
            hideList[userID] = DateTime.Now;

            BasePlayer player = BasePlayer.FindByID(userID);
            return true;
        }

        bool RemoveFromHideList(ulong userID)
        {
            hideList.Remove(userID);
            BasePlayer player = BasePlayer.FindByID(userID);

            return true;
        }

        bool ContainsInHideList(ulong userID)
        {
            if (hideList.ContainsKey(userID))
                return true;
            return false;
        }
        #endregion

        Dictionary<BasePlayer, Timer> UITimers = new Dictionary<BasePlayer, Timer>();
        Dictionary<ulong, List<UINotification>> UIUsers = new Dictionary<ulong, List<UINotification>>();

        void API_AddUINote(ulong userID, string message)
        {
            if (hideList.ContainsKey(userID))
                return;

            UIUsers[userID].Insert(0, new UINotification(message));

            if (UIUsers[userID].Count > 5)
                UIUsers[userID].Remove(UIUsers[userID].LastOrDefault());

            BasePlayer player = BasePlayer.FindByID(userID);
            Effect effect = new Effect("assets/bundled/prefabs/fx/notice/item.select.fx.prefab", player, 0, Vector3.zero, Vector3.forward);
            EffectNetwork.Send(effect, player.net.connection);

            API_UpdateUINote(BasePlayer.FindByID(userID));
            UpdateTimer(player);
            player.SendConsoleCommand("echo " + message);
        }

        void OnServerInitialized()
        {
            hideList = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, DateTime>>("Notification");
            
            List<ulong> cacheList = new List<ulong>();
            foreach (var ignore in hideList)
            {
                if (hideList[ignore.Key].AddDays(14) < DateTime.Now)
                    cacheList.Add(ignore.Key);
            }

            foreach (var userID in cacheList)
                hideList.Remove(userID);

            foreach(var player in BasePlayer.activePlayerList)
				OnPlayerConnected(player);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2, () => OnPlayerConnected(player));
                return;
            }

            if (hideList.ContainsKey(player.userID))
                hideList[player.userID] = DateTime.Now;


            UIUsers[player.userID] = new List<UINotification>();
        }

        void UpdateTimer(BasePlayer player)
        {
            if (UITimers.ContainsKey(player) && UITimers[player] != null)
                UITimers[player].Destroy();

            UITimers[player] = timer.In(5f, () => 
            {
                if (player == null) UITimers[player].Destroy();

                if (UIUsers[player.userID].Count > 0)
                {
                    UIUsers[player.userID].Remove(UIUsers[player.userID].LastOrDefault());
                    API_UpdateUINote(player);
                    UpdateTimer(player);
                }
                else
                {
                    UITimers[player].Destroy();
                }
            });
        }

        #region UI
        public const string UILayer = "Notification";
        private void API_UpdateUINote(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UILayer + $".{UIUsers[player.userID].Count}");
            for (int i = 0; i < UIUsers[player.userID].Count; i++)
            {
                CuiHelper.DestroyUi(player, UILayer + $".{i}");
                CuiElementContainer Container = new CuiElementContainer();
                Container.Add(new CuiElement
                {
                    Name = UILayer + $".{i}",
                    Parent = "Overlay",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                        }
                    }
                });
                Container.Add(new CuiElement
                {
                    Name = UILayer + $".{i}.Type",
                    Parent = UILayer + $".{i}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "0 0 0 0.5",
                            Sprite = "assets/content/materials/highlight.png",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"6 {-85 - i * 40}",
                            OffsetMax = $"36 {-55- i * 40}",
                        }
                    }
                });
                Container.Add(new CuiElement
                {
                    Name = UILayer + $".{i}.Type",
                    Parent = UILayer + $".{i}.Type",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Material = "assets/icons/iconmaterial.mat",
                            Sprite = "assets/icons/info.png",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.2 0.2",
                            AnchorMax = "0.8 0.8",
                        }
                    }
                });
                Container.Add(new CuiElement
                {
                    Name = UILayer + $".{i}.Text",
                    Parent = UILayer + $".{i}",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 0.1",
                            Sprite = "assets/content/materials/highlight.png",
                            Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"{36} {-85 - i * 40}",
                            OffsetMax = $"{41 + GetStringWidth(UIUsers[player.userID].ElementAt(i).text, "Roboto Condensed", 9)} {-55 - i * 40}",
                        }
                    }
                });
                Container.Add(new CuiElement
                {
                    Name = UILayer + $".{i}.Text",
                    Parent = UILayer + $".{i}.Text",
                    Components =
                        {
                            new CuiTextComponent
                            {
                                Text = $"<color=#fff9f9>{UIUsers[player.userID].ElementAt(i).text}</color>",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = 9,
                                Font = "robotocondensed-regular.ttf",
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1",
                                OffsetMin = "5 0",
                                OffsetMax = "5 0",
                            },
                            new CuiOutlineComponent
                            {
                                 Color = "0 0 0 1",
                                 Distance = "-0.5 0.5"
                            }
                        }
                });
                CuiHelper.AddUi(player, Container);
            }
        }
        #endregion

        #region GetStringWidth
        public double GetStringWidth(string message, string font, int fontSize)
        {
            System.Drawing.Font stringFont = new System.Drawing.Font(font, fontSize, System.Drawing.FontStyle.Regular);
            using (System.Drawing.Bitmap tempImage = new System.Drawing.Bitmap(200, 200))
            {
                System.Drawing.SizeF stringSize = System.Drawing.Graphics.FromImage(tempImage).MeasureString(message, stringFont);
                return stringSize.Width * 0.65;
            }
        }
        #endregion
    }
}
