﻿using System;
 using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
 using ProtoBuf;
 using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Rust Menu", "Hougan", "0.0.1")]
    public class RustMenu : RustPlugin
    {
        #region Variables

        private Dictionary<BasePlayer, MenuPoint> OpenMenu = new Dictionary<BasePlayer, MenuPoint>();

        #endregion
        
        #region Classes

        private class Notification
        {
            public string Title;
            public string Information;
            public string Color;

            public int Duration; 
            public string SoundEffect;
            public string ImageID;
            public string Command;
        }
        
        private static class Interface
        {
            public static string External = "UI_RustMenu_External";
            public static string Internal = "UI_RustMenu_Internal";
            public static string InterInternal = "InterInternal";
            public static string PrivPoint = External + ".PrivPoints";
            public static string MenuPoint = External + ".MenuPoints";
            public static string Header = External + ".Header";
            public static string NotificationImage = "124";
            
            public static void DrawPrivileges(BasePlayer player)
            {
                CuiElementContainer container = new CuiElementContainer();

                float marginTop = -70;
                
                float originalHeight = 30;
                float freeHeight = 20;
                
                float padding = 5; 

                var result = (object) _.plugins.Find("Grant").Call("GetGroups", player.userID);
                if (result == null) return;

                var fetch = (Dictionary<string, int>) result; 
                  
                foreach (var point in fetch.Where(p => Settings.GroupImages.ContainsKey(p.Key)))
                {
                    CuiHelper.DestroyUi(player, PrivPoint + point.Key);

                    float elementHeight = originalHeight;
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"10 {marginTop - elementHeight}", OffsetMax = $"200 {marginTop}" }, 
                        Image = { Color = "1 1 1 0" }
                    }, PrivPoint, PrivPoint + point.Key);

                    container.Add(new CuiElement
                    {
                        Parent = PrivPoint + point.Key,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) _.ImageLibrary.Call("GetImage", "H.Image"), Material = "", Color = TimeSpan.FromSeconds(point.Value).Days < 2 ? "1 0.5 0.5 0.2" : "1 1 1 0.2"},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "15 0", OffsetMax = "0 0"}
                        }
                    });

                    container.Add(new CuiElement
                    {
                        Parent = PrivPoint + point.Key,
                        Components =
                        {
                            new CuiRawImageComponent {Png = (string) _.ImageLibrary.Call("GetImage", "G" + point.Key)},
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "0 1", OffsetMax = "30 0"}
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "35 0", OffsetMax = "0 0"},
                        Text = {Text = $"ОСТАЛОСЬ: {GetTimeFromSecs(point.Value)}", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 14, Color = TimeSpan.FromSeconds(point.Value).Days < 2 ? "0.75686 0.392156 0.35686 1" : "0.81 0.77 0.74 1"}
                    }, PrivPoint + point.Key);

                    marginTop -= elementHeight + padding;
                }

                CuiHelper.AddUi(player, container);
            }

            private static string GetTimeFromSecs(int sec)
            {
                var span = TimeSpan.FromSeconds(sec);
                var result = $"{(span.Days > 0 ? span.Days + "д" + " " + span.Hours + "ч" : (span.Hours > 0 ? span.Hours + "ч" + " " + span.Minutes + "м" : span.Minutes + " мин"))}";
                
                return result;
            }
            public static void DrawMenuWithoutPoints(BasePlayer player)
            {
                CuiElementContainer container = new CuiElementContainer();

                float marginTop = -250;
                
                float originalHeight = 35;
                float freeHeight = 20;
                
                float padding = 5;
                 
                foreach (var point in Settings.Points)
                {
                    CuiHelper.DestroyUi(player, MenuPoint + Settings.Points.IndexOf(point) + ".OverflowText");

                    float elementHeight = point.DisplayName.Length > 0 ? originalHeight : freeHeight;

                    marginTop -= elementHeight + padding;
                    string text = point.TextMethod; 
                    if (!string.IsNullOrEmpty(point.TextMethod)) 
                    { 
                        if (text.StartsWith("call"))
                        {
                            if (text.Contains("report"))
                            {
                                text = (string) _.plugins.Find("ReportManager").Call("GetCooldown", player);
                                if (text == "")
                                {
                                    CuiHelper.DestroyUi(player, MenuPoint + Settings.Points.IndexOf(point) + ".Overflow");
                                    continue;
                                }

                                text = text.Substring(4, 4);
                            } 
                            if (text.Contains("store"))
                            {
                                text = (string) _.plugins.Find("RustStore").Call("GetAmount", player);
                                if (text == "")
                                {
                                    CuiHelper.DestroyUi(player, MenuPoint + Settings.Points.IndexOf(point) + ".Overflow");
                                    continue;
                                }
                            }
                        } 
                        
                        container.Add(new CuiLabel
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                Text = { Text = text, Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf", Color = "1 1 1 0.7"}
                            }, MenuPoint + Settings.Points.IndexOf(point) + ".Overflow" ,MenuPoint + Settings.Points.IndexOf(point) + ".OverflowText"); 
                    }  
                }

                CuiHelper.AddUi(player, container);
            }
            
            public static void DrawMenuPoints(BasePlayer player, MenuPoint choosed = null)
            {
                player.EndLooting();
                CuiElementContainer container = new CuiElementContainer();

                float marginTop = -250;
                
                float originalHeight = 35;
                float freeHeight = 20;
                
                float padding = 5;
                  
                foreach (var point in Settings.Points)
                {
                    CuiHelper.DestroyUi(player, MenuPoint + Settings.Points.IndexOf(point));

                    string color = point == choosed ? "0.929 0.882 0.847 1" : "0.929 0.882 0.847 0.2";
                    if (point.DrawMethod?.Contains("report") ?? false)
                    {
                        string text = (string) _.plugins.Find("ReportManager").Call("GetCooldown", player);
                        if (text != "") color = "0.81 0.77 0.74 0.3";
                    }
                    float elementHeight = point.DisplayName.Length > 0 ? originalHeight : freeHeight;
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = $"70 {marginTop - elementHeight}", OffsetMax = $"300 {marginTop}" }, 
                        Button = { Command = $"UI_RM_Handler choose {Settings.Points.IndexOf(point)}", Color = "0 0 0 0" },
                        Text = { Text = point.DisplayName, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-bold.ttf", FontSize = 26, Color = color }
                    }, MenuPoint, MenuPoint + Settings.Points.IndexOf(point));


                    marginTop -= elementHeight + padding;
                    if (!string.IsNullOrEmpty(point.TextMethod)) 
                    { 
                        string text = point.TextMethod;
                        if (text.StartsWith("call"))
                        {
                            if (text.Contains("report"))
                            {
                                text = (string) _.plugins.Find("ReportManager").Call("GetCooldown", player);
                                if (text == "") continue;

                                text = text.Substring(4, 4);
                            } 
                            if (text.Contains("store"))
                            {
                                text = (string) _.plugins.Find("RustStore").Call("GetAmount", player);
                                if (text == "") continue;
                            }
                        }
                        
                        container.Add(new CuiPanel
                        {
                            RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = $"{-11 + point.TextMargin} -11", OffsetMax = $"{11 + point.TextMargin} 1" },
                            Image = { Color = "0.73 0.3 0.27 1" } 
                        }, MenuPoint + Settings.Points.IndexOf(point), MenuPoint + Settings.Points.IndexOf(point) + ".Overflow");
                        
                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                            Text = { Text = text, Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-bold.ttf", Color = "1 1 1 0.8"}
                        }, MenuPoint + Settings.Points.IndexOf(point) + ".Overflow" ,MenuPoint + Settings.Points.IndexOf(point) + ".OverflowText"); 
                    }  
                }

                CuiHelper.AddUi(player, container);
            }
            
            public static void DrawExternalLayer(BasePlayer player)
            {
                CuiElementContainer container = new CuiElementContainer();
                CuiHelper.DestroyUi(player, External);
                CuiHelper.DestroyUi(player, Internal);
                CuiHelper.DestroyUi(player, InterInternal);

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Image = { Color = "0.235 0.227 0.180 0.90" } 
                }, "Overlay", External);
                
                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Image = { Color = "0 0 0 0.2" } 
                }, External);

                container.Add(new CuiPanel
                { 
                    CursorEnabled = true,
                    RectTransform = {AnchorMin = "0.3 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Image = { Color = "0 0 0 0" } 
                }, "Overlay", Internal);

                container.Add(new CuiPanel
                { 
                    CursorEnabled = true,
                    RectTransform = {AnchorMin = "0.95 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Image = { Color = "0 0 0 0" } 
                }, "Overlay", InterInternal);
                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Image = {Color = "0.141 0.137 0.109 1", Sprite = "assets/content/ui/ui.background.transparent.radial.psd"}
                }, External, MenuPoint);

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = {AnchorMin = "0 0.8", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Image = {Color = "0 0 0 0", Sprite = "assets/content/ui/ui.background.transparent.radial.psd"}
                }, External, PrivPoint);

                container.Add(new CuiButton 
                {
                    RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = $"-35 -35", OffsetMax = "-10 -10" },
                    Button = { Color = "0.929 0.882 0.847 0.6", Command = "UI_RM_Handler close", Sprite = "assets/icons/close.png", Close = Interface.InterInternal }, 
                    Text = { Text = "" }
                }, InterInternal);
                
                CuiHelper.AddUi(player, container);
            }

            public static void OpenMenu(BasePlayer player)
            {
                if (!_.OpenMenu.ContainsKey(player))
                    _.OpenMenu.Add(player, Settings.Points.FirstOrDefault(p => p.DisplayName.ToLower() == "календарь")); 
                
                DrawExternalLayer(player); 
                DrawMenuPoints(player);
                DrawPrivileges(player); 
                
                _.plugins.Find("Menu").Call("ShowMenu", player); 
                //DrawTopImage(player); 
            }
            
            
        }

        private class MenuPoint
        {
            public string DisplayName;
            public string DrawMethod;

            public float TextMargin;
            public string TextMethod;
            public bool NewInfo;
        }

        private class Configuration
        {
            [JsonProperty("Название сервера")]
            public string ServerName;
            [JsonProperty("Список доступных разделов меню")]
            public List<MenuPoint> Points = new List<MenuPoint>();

            public Dictionary<string, string> GroupImages = new Dictionary<string, string>();
            public Dictionary<float, string> RawImages = new Dictionary<float, string>();
            

            public static Configuration Generate()
            {
                return new Configuration
                {
                    ServerName = "BLOOD RED MAX2",
                    GroupImages = new Dictionary<string, string>
                    {
                        ["kabanchik"] = "https://i.imgur.com/8p7yiVh.png",
                        ["joker"] = "https://i.imgur.com/TH9qtZ7.png",  
                        ["masnik"] = "https://i.imgur.com/XIQQXSo.png",
						["smert"] = "https://i.imgur.com/oqfU7RT.png",  
                    },
                    RawImages = new Dictionary<float, string>
                    {
                        
                    },
                    Points = new List<MenuPoint>
                    {
                        new MenuPoint
                        {
                            DisplayName = "КОРЗИНА",
                            TextMethod = "call:store",  
                            TextMargin = -120,
                            DrawMethod = "storeSecret"
                        },
                        new MenuPoint
                        {
                            DisplayName = "৑"
                        },
                        new MenuPoint
                        {
                            DisplayName = "НАЁМНИКИ",
                            DrawMethod = "chat.say /furySecret"
                        },
                        new MenuPoint
                        { 
                            DisplayName = "КАЛЕНДАРЬ", 
                            DrawMethod = "chat.say /wipe" 
                        },
                        new MenuPoint 
                        {
                            DisplayName = "РЕПОРТ",
                            DrawMethod = "chat.say /reportSecret",
                            TextMethod = "call:report",
                            TextMargin = -140
                        },
                        new MenuPoint
                        {
                            DisplayName = "ВАЙПБЛОК",
                            DrawMethod = "chat.say /block" 
                        },
                        new MenuPoint
                        {
                            DisplayName = "ИНФОРМАЦИЯ",
                            DrawMethod = "chat.say /help",
                        },
                        new MenuPoint
                        {
                            DisplayName = ""
                        },
                        new MenuPoint
                        {
                            DisplayName = "НАСТРОЙКИ",
                            DrawMethod = "chat.say /settings",
                        },
                        new MenuPoint
                        {
                            DisplayName = ""
                        },
                        new MenuPoint
                        {
                            DisplayName = "ЗАКРЫТЬ",
                            DrawMethod = "UI_RM_Handler close"
                        },
                    }
                };
            }
        }
        
        #endregion

        #region Variables

        [PluginReference] private Plugin ImageLibrary;
        private static RustMenu _;
        private static Configuration Settings = Configuration.Generate();

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            _ = this;

            foreach (var check in Settings.GroupImages)
            {
                ImageLibrary.Call("AddImage", check.Value, "G" + check.Key);
            }
            
            timer.Every(1, () =>
            {
                foreach (var check in OpenMenu)
                {
                    if (check.Key.IsConnected)
                    {
                        Interface.DrawPrivileges(check.Key);
                        Interface.DrawMenuWithoutPoints(check.Key);
                    }
                }
                
            });
            
            //foreach (var check in FileSystem.FindAll("", "psd"))
            //    LogToFile("SAfd", check, this);  
            
            foreach (var check in Settings.RawImages)
            {
                ImageLibrary.Call("AddImage", check.Value, $"I.{check.Key}");
            }
 
            ImageLibrary.Call("AddImage", "https://i.imgur.com/c3DqDdw.png", $"H.Image");
            Interface.NotificationImage = (string) ImageLibrary.Call("GetImage", "NotificationImage");
        } 
        
        #endregion

        #region Commands

        [ConsoleCommand("UI_RM_Handler")]
        private void CmdConsoleRustMenu(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null || !args.HasArgs(1)) return;

            switch (args.Args[0].ToLower())
            {
                case "close":
                { 
                    if (OpenMenu.ContainsKey(player))
                        OpenMenu.Remove(player);
                    
                    CuiHelper.DestroyUi(player, Interface.External);
                    CuiHelper.DestroyUi(player, Interface.Internal);
                    CuiHelper.DestroyUi(player, Interface.InterInternal);

                    plugins.Find("Menu").Call("CloseMenu", player);
                    break;
                }
                case "choose":
                {
                    int chooseIndex = -1;
                    if (!int.TryParse(args.Args[1], out chooseIndex)) return;

                    var chooseElement = Settings.Points.ElementAtOrDefault(chooseIndex);
                    if (chooseElement == null) return;

                    CuiHelper.DestroyUi(player, Interface.Internal); 
                    CuiHelper.DestroyUi(player, Interface.InterInternal); 
                    CuiElementContainer container = new CuiElementContainer();
                    container.Add(new CuiPanel
                    { 
                        CursorEnabled = true,
                        RectTransform = {AnchorMin = "0.3 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                        Image = { Color = "0 0 0 0" } 
                    }, "Overlay", Interface.Internal);

                    /*container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = $"-35 -35", OffsetMax = "-10 -10" },
                        Button = { Color = "1 1 1 0.6", Command = "UI_RM_Handler close", Sprite = "assets/icons/close.png" }, 
                        Text = { Text = "" }
                    }, Interface.Internal);*/
                    
                    CuiHelper.AddUi(player, container);
                    
                    player.SendConsoleCommand(chooseElement.DrawMethod);
                    
                    Interface.DrawPrivileges(player);  
                    Interface.DrawMenuPoints(player, chooseElement);
                    
                    //Effect effect = new Effect("", player, 0, new Vector3(), new Vector3());
                    //EffectNetwork.Send(effect, player.Connection);

                    OpenMenu[player] = chooseElement;
 
                    container.Clear();
                    container.Add(new CuiPanel
                    { 
                        CursorEnabled = true,
                        RectTransform = {AnchorMin = "0.95 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                        Image = { Color = "0 0 0 0" } 
                    }, "Overlay", Interface.InterInternal);
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = $"-35 -35", OffsetMax = "-10 -10" },
                        Button = { Color = "0.929 0.882 0.847 0.6", Command = "UI_RM_Handler close", Sprite = "assets/icons/close.png", Close = Interface.InterInternal}, 
                        Text = { Text = "" } 
                    }, Interface.InterInternal);
                    
                    CuiHelper.AddUi(player, container); 
                    break;
                }
            }
        }

        #endregion

        #region Unload

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        [ChatCommand("menuSecret")]
        private void CmdChatXs(BasePlayer player)
        {
            Interface.OpenMenu(player);

            Interface.DrawPrivileges(player); 
        } 
        [ChatCommand("menu")]
        private void CmdChatX(BasePlayer player)
        {
            Interface.OpenMenu(player);

            var first = Settings.Points.FirstOrDefault(p => p.DisplayName.Contains("৑"));
            OpenMenu[player] = Settings.Points.FirstOrDefault(p => p.DisplayName.ToLower() == "৑");
            Interface.DrawMenuPoints(player, first);
            Interface.DrawPrivileges(player); 
            player.SendConsoleCommand(first.DrawMethod);
        }

        private void Unload()
        {
            BasePlayer.activePlayerList.ToList().ToList().ForEach(p =>
            {
                CuiHelper.DestroyUi(p, Interface.External);
                CuiHelper.DestroyUi(p, Interface.Internal);
                CuiHelper.DestroyUi(p, Interface.InterInternal);
            });
        }

        #endregion
    }
}