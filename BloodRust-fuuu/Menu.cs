using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using UnityEngine.XR;
using WebSocketSharp;

namespace Oxide.Plugins
{
    [Info("Menu", "TopPlugin.ru", "3.0.0")]
    public class Menu : RustPlugin
    {
        #region eNums

        private enum State
        {
            None,
            Closed,
            Opened
        }

        #endregion
        
        #region Classess
 
        private class Configuration
        {
            public string ServerID = "1";
            public string ServerName = "БАТЯ ✘ MAX 5";
            
            public Dictionary<float, string> Images = new Dictionary<float, string>();

            public static Configuration Generate()
            {
                return new Configuration
                {
                    Images = new Dictionary<float, string>
                    {
                        [0f] = "https://ic.wampi.ru/2021/04/20/star_5cd1b24dedbd4bc70.png",
                        [0.05f] = "https://ic.wampi.ru/2021/04/20/star_6297f4499a942cea9.png",
                        [0.1f] = "https://ic.wampi.ru/2021/04/20/star_7e3d1a733037c24ca.png",
                        [0.15f] = "https://ic.wampi.ru/2021/04/20/star_831f42d0849450fe3.png",
                        [0.2f] = "https://ic.wampi.ru/2021/04/20/star_963105e3116ff4d96.png",
                        [0.25f] = "https://ic.wampi.ru/2021/04/20/star_100c39452d1dbeedb9.png",
                        [0.3f] = "https://ic.wampi.ru/2021/04/20/star_11ddc299d3f047b859.png",
                        [0.35f] = "https://ic.wampi.ru/2021/04/20/star_12bab753dbda836ef3.png",
                        [0.4f] = "https://ic.wampi.ru/2021/04/20/star_1352b0a4c991f9545c.png",
                        [0.45f] = "https://ic.wampi.ru/2021/04/20/star_14977af47947206953.png",
                        [0.5f] = "https://ic.wampi.ru/2021/04/20/star_15b6f76f0047938ed9.png",
                        [0.55f] = "https://ic.wampi.ru/2021/04/20/star_16d0860a3bbad25c34.png",
                        [0.6f] = "https://ic.wampi.ru/2021/04/20/star_17b635f2e49f797a22.png",
                        [0.65f] = "https://ic.wampi.ru/2021/04/20/star_1832b34b7dc5ca166c.png",
                        [0.7f] = "https://ic.wampi.ru/2021/04/20/star_1832b34b7dc5ca166c.png",
                        [0.75f] = "https://ic.wampi.ru/2021/04/20/star_19ffda2cd71c78190a.png",
                        [0.8f] = "https://ic.wampi.ru/2021/04/20/star_19ffda2cd71c78190a.png",
                        [0.85f] = "https://ic.wampi.ru/2021/04/20/star_19ffda2cd71c78190a.png",
                        [0.9f] = "https://ic.wampi.ru/2021/04/20/star_19ffda2cd71c78190a.png",
                        [0.95f] = "https://ic.wampi.ru/2021/04/20/star_19ffda2cd71c78190a.png",
                        [1.0f] = "https://ic.wampi.ru/2021/04/20/star_19ffda2cd71c78190a.png"
                    }
                };
            }
        }

        private class MenuPoint
        {
            public string ImageURL;
            public string ImageID;

            public string Command;
            public bool Active;
        }

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

        private class MenuPlayer : MonoBehaviour
        {
            private BasePlayer Player;
            private State      State;
			private int LastOnline = 0;
            private string Layer = "UI_MenuInitial2";
            public List<MenuPoint> Points = new List<MenuPoint>();

            public List<Notification> Notifications = new List<Notification>
            {
                new Notification
                {
                    Title = "ДОБРО ПОЖАЛОВАТЬ",
                    Information = Settings.ServerName,
                    
                    Color = "1 1 1 0.6",
                    Duration = 15,
                    SoundEffect = "",
                    ImageID = ""
                }
            };

            public void Awake()
            {
                Player = GetComponent<BasePlayer>();
                Points = Handler.Points.ToList();
                Notifications.AddRange(Handler.Defaults);
                
                Initialize();
            }

            public void Initialize()
            { 
                if (Player.IsReceivingSnapshot || Player.IsSleeping()) 
                {
                    Invoke(nameof(Initialize), 1f);
                    return;
                }

                State = State.Closed;
                InitializeLayers();
                InitializeMenu();
                
                DoNotification(false);
            }

            public void CloseNotification()
            {
                DoNotification(false); 
            }

            public void DoNotification(bool @new)
            {
                if (@new && IsInvoking(nameof(CloseNotification)))
                {
                    return;
                }
                
                var notify = Notifications.FirstOrDefault();
                if (notify == null)
                {
                    for (int i = 0; i < 5; i++)
                        CuiHelper.DestroyUi(Player, Layer + $".Notify{i}");
                    
                    if (IsInvoking(nameof(CloseNotification)))
                        CancelInvoke(nameof(CloseNotification));
                    
                    StopAllCoroutines();
                    return;
                }
                
                DestroyNotification(notify);
                
                if (IsInvoking(nameof(CloseNotification)))
                    CancelInvoke(nameof(CloseNotification));
                
                Invoke(nameof(CloseNotification), notify.Duration);
                
                Notifications.Remove(notify);
            }

            public void DestroyNotification(Notification notification)
            {
                for (int i = 0; i < 5; i++)
                    CuiHelper.DestroyUi(Player, Layer + $".Notify{i}");

                StopAllCoroutines();
                StartCoroutine(DrawNotification(notification));
            }

            public void InitializeMenu(bool fromMenu = false) 
            {
                InitializeLayers();
                if (fromMenu && Notifications.All(p => !p.Title.ToLower().Contains("добро")))
                {
                    Notifications.Clear();
                    DoNotification(false);  
                    
                    Notifications.Add(new Notification
                    {
                        Title = Settings.ServerName, 
                        Information = $"Игроков онлайн: {BasePlayer.activePlayerList.ToList().Count + ServerMgr.Instance.connectionQueue.joining.Count} {(ServerMgr.Instance.connectionQueue.queue.Count > 0 ? ("(" + ServerMgr.Instance.connectionQueue.queue.Count +  " в очереди" + ")") : (ServerMgr.Instance.connectionQueue.joining.Count > 0 ? ("(" + ServerMgr.Instance.connectionQueue.joining.Count +  " присоединяется" + ")") : ""))}",
                        Color = "1 1 1 0.5",
                        Duration = 360,
                        SoundEffect = "",
                        ImageID = ""
                    });
                    DoNotification(true);
                }
                CuiElementContainer container = new CuiElementContainer();
                
                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 0", OffsetMax = "0 0"},
                    Image         = {Color     = "0 0 0 0"}
                }, "Overlay4", Layer); 

                CuiHelper.AddUi(Player, container);

                CancelInvoke(nameof(UpdateOnline));
                
                UpdateOnline(true);
                InvokeRepeating(nameof(UpdateOnline), 5f, 5f);
            }

            public void UpdateOnline(bool update=false)
            {
				int online = BasePlayer.activePlayerList.ToList().Count;
				if (!update) if (LastOnline==online) return;
				LastOnline = online;
                CuiHelper.DestroyUi(Player, Layer + ".Online");
                CuiElementContainer container = new CuiElementContainer();

                float imageId = 0f;
                //Выстраиваем прогресс от онлайна
				imageId = (float)online/(float)ConVar.Server.maxplayers;
                
                if (imageId % 0.05f != 0)
                    imageId += 0.05f - imageId % 0.05f; 
                
                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Name   = Layer + ".Online", 
                    Components =
                    {
                        new CuiRawImageComponent { Png = Handler.PrepareImages[imageId.ToString()], Color = "1 1 1 0.9",FadeIn=update==true?3f:0f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 -64", OffsetMax = "54 -10" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax   = "1 1", OffsetMin = "0 0", OffsetMax = "0 0"},
                    Button        = {Color     = "0 0 0 0", Command = "chat.say /menu"},
                    Text          = {Text      = ""}
                }, Layer + ".Online");

                if (State == State.Opened)
                {
                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax                                   = "1 0", OffsetMin                        = "-2 -15", OffsetMax = "0 5"},
                        Text          = { Text      = online.ToString(), Font = "robotocondensed-regular.ttf", FontSize = 15, Align = TextAnchor.MiddleCenter, Color = "0.9 0.1 0.2 1" }
                    }, Layer + ".Online", Layer + ".VisualUpdate");
                }

                CuiHelper.AddUi(Player, container);
            }

            public IEnumerator DrawNotification(Notification notification)
            {
                yield return new WaitForSeconds(1f);
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    FadeOut = 1f,
                    Name = Layer + ".Notify4",
                    Parent = Layer + ".Notify",
                    Components =
                    {
                        new CuiRawImageComponent {FadeIn         = 1f, Png          = Handler.NotificationImage, Color = notification.Color, Material = ""},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin                 = "0 0", OffsetMax = "0 1" } 
                    } 
                });

                if (!notification.ImageID.IsNullOrEmpty())
                {
                    container.Add(new CuiElement
                    {
                        FadeOut = 1f,
                        Name = Layer + ".Notify3",
                        Parent = Layer + ".Notify",
                        Components =
                        {
                            new CuiRawImageComponent {FadeIn         = 1f, Png          = notification.ImageID, Color = "1 1 1 0.5" },
                            new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin            = "0 0", OffsetMax = "0 0" } 
                        }
                    }); 
                }
                 
                container.Add(new CuiLabel
                {
                    FadeOut = 1f,
                    RectTransform = {AnchorMin = "0.13 0.46", AnchorMax        = "4 1", OffsetMax              = "0 0"},
                    Text          = {FadeIn = 1f,Text      = notification.Title, Align = TextAnchor.LowerLeft, Font = "robotocondensed-bold.ttf", FontSize = 16}
                }, Layer + ".Notify", Layer + ".Notify1");
                
                container.Add(new CuiLabel 
                {
                    FadeOut = 1f,
                    RectTransform = {AnchorMin = "0.13 0", AnchorMax        = "4 0.46", OffsetMax              = "0 0"},
                    Text          = {FadeIn = 1f,Text      = notification.Information, Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf", FontSize = 10}
                }, Layer + ".Notify", Layer + ".Notify2");

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0.08 0", AnchorMax = "0.8 1", OffsetMax = "0 0"},
                    Button        = {Color     = "0 0 0 0", Command = $"UI_MenuHandler skip " + notification.Command },
                    Text          = {Text      = ""}
                }, Layer + ".Notify");
                
				CuiHelper.AddUi(Player, container);

                if (!notification.SoundEffect.IsNullOrEmpty())
                {
                    while (true)
                    {
                        Effect effect = new Effect(notification.SoundEffect, Player.transform.position, Vector3.zero);
                        EffectNetwork.Send(effect, Player.net.connection);

                        yield return new WaitForSeconds(5);
                    }
                }
                yield return 0;
            }

            public void OpenMenu()
            {
                CuiHelper.DestroyUi(Player, Layer + ".Menu");
                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiElement
                {
                    Name   = Layer + ".Menu",
                    Parent = Layer,
                    Components =
                    {
                        new CuiRawImageComponent { FadeIn = 1f, Png           = Handler.MenuHeaderImage, Color = "1 1 1 0.9"},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax               = "1 0", OffsetMin = "-1.5 -86", OffsetMax = "52.5 -40" }
                    }
                });

                float topPosition = 0f;

                float counter = 0;
                
                /*CuiHelper.AddUi(Player, container);
                foreach (var check in Handler.Points)
                {
                    InitializePoint(check, false, true); 
                }  
                 
                if (IsInvoking(nameof(UpdateOnline)))
                    CancelInvoke(nameof(UpdateOnline));*/
                
                //UpdateOnline();
                //InvokeRepeating(nameof(UpdateOnline), 5f, 5f);
            }

            public void InitializePoint(MenuPoint point, bool active, bool initial)
            {  
                point.Active = active;
                
                int index = Handler.Points.IndexOf(point); 
                string layerName = Layer + $"MP.{index}";
                CuiHelper.DestroyUi(Player, layerName);

                CuiElementContainer container = new CuiElementContainer();
                
                float topPosition = -38f * index;
                float counter = 0.5f * index;
                
                /*container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = $"9 {topPosition - 36 + 4}", OffsetMax = $"-9 {topPosition + 4}"},
                    Image         = {FadeIn = 1f + counter,Color     = "0 0 0 0"}
                }, Layer + ".Menu", layerName);
                
                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Image         = {Color     = "0 0 0 0"}
                }, layerName, layerName + ".Inactive");
                
                container.Add(new CuiPanel
                { 
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Image         = {Color     = "0 0 0 0"}
                }, layerName, layerName + ".Active");

                if (active)
                {
                    container.Add(new CuiElement
                    {
                        Parent = layerName + ".Inactive",
                        Components =
                        {
                            new CuiRawImageComponent() { Png            = Handler.ActiveImage },
                            new CuiRectTransformComponent() {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"}
                        }
                    });
                }
                
                container.Add(new CuiElement
                {
                    Parent = layerName + ".Active",
                    Components =
                    {
                        new CuiRawImageComponent() {FadeIn = initial ? 1f + counter : 0f, Png            = point.ImageID, Color = active ? "1 1 1 1" : "1 1 1 0.5" },
                        new CuiRectTransformComponent() {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"}
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                    Button        = {Color     = "0 0 0 0", Command = $"UI_MenuHandler rebuild {index} {(active ? "" : "123")}"},
                    Text          = {Text      = ""}
                }, layerName + ".Active");*/

                CuiHelper.AddUi(Player, container);
            }

            public void CloseMenu()
            {
                CuiHelper.DestroyUi(Player, Layer + ".VisualUpdate");
                for (int i = 10; i >= 0; i--)
                    CuiHelper.DestroyUi(Player, Layer + i);
                
                CuiHelper.DestroyUi(Player, Layer + ".Menu");
            }

            public void SwitchState()
            {
                State = State == State.Opened ? State.Closed : State.Opened;

                if (State == State.Opened)
                {
                    OpenMenu();
                    //UpdateOnline();
                }
                else
                {
                    CloseMenu();
                }
            }

            public void InitializeLayers()
            {
                CuiElementContainer container = new CuiElementContainer();

                CuiHelper.DestroyUi(Player, Layer);
                CuiHelper.DestroyUi(Player, "Overlay1");
                CuiHelper.DestroyUi(Player, "Overlay2");
                CuiHelper.DestroyUi(Player, "Overlay3");
                CuiHelper.DestroyUi(Player, "Overlay4");
                

                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMax = "0 0"},
                    Image         = {Color     = "0 0 0 0"}
                }, "Overlay", "Overlay1");
                
                container.Add(new CuiPanel 
                {
                    RectTransform = {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMax = "0 0"},
                    Image         = {Color     = "0 0 0 0"}
                }, "Overlay", "Overlay2");
                
                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "0 0"},
                    Image         = {Color     = "0 0 0 0"}
                }, "Overlay", "Overlay3");
                 
                container.Add(new CuiElement
                {
                    Name   = Layer + ".Notify",
                    Parent = "Overlay3",
                    Components =  
                    {
                        new CuiRawImageComponent {FadeIn         = 1f, Color = "1 1 1 0" },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin                 = "24 -65", OffsetMax = "290 -9" } 
                    }
                });
                
                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = "0 1", AnchorMax = "0 1", OffsetMax = "0 0"},
                    Image         = {Color     = "0 0 0 0"}
                }, "Overlay", "Overlay4");

                CuiHelper.AddUi(Player, container);
            }

            public void DestroyLayers()
            {
                CuiHelper.DestroyUi(Player, "Overlay1");
                CuiHelper.DestroyUi(Player, "Overlay2");
                CuiHelper.DestroyUi(Player, "Overlay3");
                CuiHelper.DestroyUi(Player, "Overlay4");
            }

            public void OnDestroy()
            {
                DestroyLayers();
            }
        }  
        
        private class MenuHandler : MonoBehaviour
        {
            public Dictionary<float, string> RawImages = new Dictionary<float, string>();

            public List<Notification> Defaults = new List<Notification>
            {
            };
            public List<MenuPoint> Points = new List<MenuPoint>
            {
                new MenuPoint
                { 
                    ImageURL = "https://i.imgur.com/Nou392v.png",
                    ImageID  = "",
                    Command  = "chat.say /store"
                },
                new MenuPoint
                {
                    ImageURL = "https://i.imgur.com/CXBatkm.png",
                    ImageID  = "",
                    Command  = "chat.say /report"
                },
                new MenuPoint
                {
                    ImageURL = "https://i.imgur.com/eFakSsx.png",
                    ImageID  = "",
                    Command  = "chat.say /help"
                },  
                new MenuPoint  
                {  
                    ImageURL = "https://i.imgur.com/JDsmlwV.png",
                    ImageID  = "",
                    Command  = "chat.say /wipe"
                }, 
                new MenuPoint
                {
                    ImageURL = "https://i.imgur.com/vhQCtI2.png",
                    ImageID  = "",
                    Command  = "chat.say /block"
                },
                new MenuPoint
                {
                    ImageURL = "https://i.imgur.com/DSiRg5v.png",
                    ImageID = "",
                    Command = "chat.say /vip"
                }, 
            };
            public Dictionary<string, string> PrepareImages = new Dictionary<string, string>();
            public string ActiveImage = "";
            public string MenuHeaderImage = "";
            public string NotificationImage = "";
            public string RaidNotification = "";
            public string BoarNotification = "";

            public void Awake()
            {
                RawImages = Settings.Images;
                Interface.Oxide.LogWarning($"Initializing menu components [Loading images]");

                _.ImageLibrary.Call("AddImage", "https://i.imgur.com/OQQbkzE.png", "ActiveImage");
                _.ImageLibrary.Call("AddImage", "https://ic.wampi.ru/2021/04/21/menu_2_2.png", "NotificationImage");
                _.ImageLibrary.Call("AddImage", "https://i.imgur.com/HiEEg07.png", "MenuHeaderImage");
                _.ImageLibrary.Call("AddImage", "https://ic.wampi.ru/2021/04/20/menu_1_7.png", "RaidNotification");
                _.ImageLibrary.Call("AddImage", "", "BoarNotification");

                foreach (var check in Defaults.Where(p => !p.ImageID.IsNullOrEmpty())) 
                {
                    _.ImageLibrary.Call("AddImage", check.ImageID, check.Title.Replace(" ", ""));
                }
                foreach (var check in RawImages)
                {  
                    _.ImageLibrary.Call("AddImage", check.Value, $"I.{check.Key}");
                    if (RawImages.ToList().IndexOf(check) % 5 == 0)
                    {
                        Interface.Oxide.LogDebug($"Loading: {(((float) RawImages.ToList().IndexOf(check) / RawImages.Count) * 100):F0}%");
                    }
                }

                foreach (var check in Points)
                {
                    _.ImageLibrary.Call("AddImage", check.ImageURL, $"M.{Points.IndexOf(check)}");
                }
                
                Interface.Oxide.LogWarning($"Parsing image components [Parsing images in 15 sec]");
                Invoke(nameof(ParseImages), 1f);
            }

            public void ParseImages()
            {
                foreach (var check in RawImages)
                {
                    var result = _.ImageLibrary.Call("GetImage", $"I.{check.Key}");
                    PrepareImages.Add(check.Key.ToString(), result.ToString());
                }
                foreach (var check in Points)
                { 
                    check.ImageID = (string) _.ImageLibrary.Call("GetImage", $"M.{Points.IndexOf(check)}");
                }
                foreach (var check in Defaults.Where(p => !p.ImageID.IsNullOrEmpty())) 
                {
                    check.ImageID = (string) _.ImageLibrary.Call("GetImage", check.Title.Replace(" ", ""));
                }
                ActiveImage = (string) _.ImageLibrary.Call("GetImage", $"ActiveImage");
                MenuHeaderImage = (string) _.ImageLibrary.Call("GetImage", $"MenuHeaderImage");
                NotificationImage = (string) _.ImageLibrary.Call("GetImage", $"NotificationImage");
                RaidNotification = (string) _.ImageLibrary.Call("GetImage", $"RaidNotification");
                BoarNotification = (string) _.ImageLibrary.Call("GetImage", $"BoarNotification");
                
                Interface.Oxide.LogError($"Saving images complete, giving components...");
                GiveComponents();
            }

            public void GiveComponents()
            {
                _.Subscribe(nameof(_.OnPlayerConnected));
                BasePlayer.activePlayerList.ToList().ForEach(p =>
                {
                    p.gameObject.AddComponent<MenuPlayer>();
                });   
            }
        }

        #endregion

        #region Variables

        private static Menu _;
        private static MenuHandler Handler;
        
        [PluginReference] private Plugin ImageLibrary;
        
        
        private static Configuration Settings = null;
        
        #endregion

        #region Initialization

        private void CloseMenu(BasePlayer player)
        {
            var obj = player.GetComponent<MenuPlayer>();
            
            
            obj.Notifications.Clear();
            obj.DoNotification(false);  
        }
        
        private void ShowMenu(BasePlayer player)
        {
            player.GetComponent<MenuPlayer>().InitializeMenu(true);
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintWarning($"Error reading config, creating one new config!");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => Settings = Configuration.Generate();
        protected override void SaveConfig()        => Config.WriteObject(Settings);

        private void OnServerInitialized()
        {
            _ = this;

            Unsubscribe(nameof(_.OnPlayerConnected));
            Handler = ServerMgr.Instance.gameObject.AddComponent<MenuHandler>(); 
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            var obj = player.GetComponent<MenuPlayer>();
            if (obj != null) return;

            player.gameObject.AddComponent<MenuPlayer>();
        }
        
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var obj = player.GetComponent<MenuPlayer>();
            if (obj != null) UnityEngine.Object.Destroy(obj); 
        }
        
        private void Unload()
        {
            DestroyAll<MenuHandler>();
            DestroyAll<MenuPlayer>();
        }

        #endregion

        #region Commands

        [ConsoleCommand("UI_MenuHandler")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;

            var obj = player.GetComponent<MenuPlayer>();
            if (obj == null) return;
            
            switch (args.Args[0].ToLower())
            {
                case "switch":
                {
                    obj.SwitchState();
                    break;
                }
                case "rebuild":
                {
                    CmdConsoleClose(args);
                    
                    int index = int.Parse(args.Args[1]);
                    var elem = Handler.Points.ElementAt(index);
                    
                    obj.InitializePoint(elem, args.HasArgs(3), false); 
                    
                    if (args.HasArgs(3))
                        player.SendConsoleCommand(elem.Command);
                    break;
                }
                case "skip":
                {
                    string result = "";
                    if (args.HasArgs(2))
                    {
                        for (int i = 1; i < args.Args.Length; i++)
                            result += args.Args[i] + " ";
                    }
                    
                    if (!result.IsNullOrEmpty())
                        player.SendConsoleCommand(result); 
                        
                    obj.DoNotification(false);
                    break;
                }
            }
        }

        [ConsoleCommand("closemenu")]
        private void CmdConsoleClose(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;

            args.Player().SendConsoleCommand("UI_BReport closec");
            CuiHelper.DestroyUi(player, "Overlay2");
            CuiHelper.AddUi(player, new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                        Image         = {Color     = "0 0 0 0"}
                    },
                    "Overlay1", "Overlay2"
                }
            });

            var obj = player.GetComponent<MenuPlayer>();
            if (obj == null) return;
            
            foreach (var check in obj.Points)
                obj.InitializePoint(check, false, false); 
        }

        #endregion

        #region API

        [HookMethod("AddNotificationFromMenu")] 
        private void AddNotificationFromMenu(BasePlayer player, string title, string desc, string color, int duration, string sound, string imageId, string command = "")
        {
            var obj = player.GetComponent<MenuPlayer>();
            if (obj == null) return; 
            
            obj.Notifications.Clear();
            obj.Notifications.Add(new Notification
            {
                Title = title,
                Information = desc,
                Color = color,
                Duration = duration,
                SoundEffect = sound,
                ImageID = imageId,
                Command = command 
            });
            
            obj.DoNotification(false);   
        }

        [HookMethod("AddNotification")] 
        private void AddNotification(BasePlayer player, string title, string desc, string color, int duration, string sound, string imageId, string command = "")
        {
            if (player == null)
            {
                return;
            }
            
            var obj = player.GetComponent<MenuPlayer>();
            if (obj == null) return;
            
            obj.Notifications.Add(new Notification
            {
                Title = title,
                Information = desc,
                Color = color,
                Duration = duration,
                SoundEffect = sound,
                ImageID = imageId,
                Command = command 
            });
            
            obj.DoNotification(true);  
        }

        #endregion

        #region Utils

        private void DestroyAll<T>()
        {
            var objects = UnityEngine.Object.FindObjectsOfType(typeof(T));
            objects?.ToList().ForEach(UnityEngine.Object.Destroy); 
        }

        #endregion
    }
}