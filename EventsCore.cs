using System;
using System.Collections.Generic;
using Oxide.Core.Configuration;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using Oxide;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EventsCore", "BadMandarin, Ryamkk", "1.2.1")]
    [Description("EventsCore")]
    class EventsCore : RustPlugin
    {
		#region References
		
        [PluginReference] Plugin UniversalShop;
		[PluginReference] Plugin ServerRewards;
		[PluginReference] Plugin RustShop;

		#endregion
		
		#region Configuration
		
		private void LoadDefaultConfig()
        {
			GetConfig("Основные настройки ивента: › Царь горы ‹", "Названия ивента", ref EventName_1);
			GetConfig("Основные настройки ивента: › Царь горы ‹", "Цвет ивента для UI панели", ref EventColor_1);
			GetConfig("Основные настройки ивента: › Царь горы ‹", "Основная цель ивента для UI панели", ref EventText_1);
			GetConfig("Основные настройки ивента: › Царь горы ‹", "Время для определение победителя ивента", ref EventTime_1);
			
			GetConfig("Основные настройки ивента: › Рудо-Копатель ‹", "Названия ивента", ref EventName_2);
			GetConfig("Основные настройки ивента: › Рудо-Копатель ‹", "Цвет ивента для UI панели", ref EventColor_2);
			GetConfig("Основные настройки ивента: › Рудо-Копатель ‹", "Основная цель ивента для UI панели", ref EventText_2);
			GetConfig("Основные настройки ивента: › Рудо-Копатель ‹", "Время для определение победителя ивента", ref EventTime_2);
			
			GetConfig("Общие настройки ивентов", "Интервал запуска ивента (секунды)", ref EventAutoStart);
			GetConfig("Общие настройки ивентов", "Минимальный онлайн для запуска ивента", ref EventMaxPlayer);
			
			GetConfig("Общие настройки бонуса", "Использовать выдачу бонуса балансом от Server Rewards", ref Use_ServerRewards);
			GetConfig("Общие настройки бонуса", "Использовать выдачу бонуса балансом от Rust Shop", ref Use_RustShop);
			GetConfig("Общие настройки бонуса", "Использовать выдачу бонуса балансом от Universal Shop", ref Use_UniversalShop);
			GetConfig("Общие настройки бонуса", "Сумма бонуса за победу в ивенте", ref BalanceAmount);
			
            SaveConfig();
        }

		#endregion
		
        #region Variables
		
		private string EventName_1 = "Царь горы";
		private string EventColor_1 = "#9999E2FF";
		private string EventText_1 = "Будь выше всех чтобы победить!";
		private int EventTime_1 = 300;
		
		private string EventName_2 = "Рудо-Копатель";
		private string EventColor_2 = "#CAB366FF";
		private string EventText_2 = "Добывай как можно больше ресурсов!";
		private int EventTime_2 = 300;
		
		private float EventAutoStart = 7200f;
		
		private int EventMaxPlayer = 5;
		
		private bool Use_RustShop = true;
		private bool Use_ServerRewards = false;
		private bool Use_UniversalShop = false;
		
		private int BalanceAmount = 1000;
		
        private Timer EventsTimer;
        private Timer AutoETimer;
        private int curEvent;
        private int eTimeLeft = 0;
        private bool eActive = false;
        List<eInfo> EventsList;
        Dictionary<ulong, EventsScores> eUsersScore;
        BasePlayer eLastLeader;
		
        #endregion

        #region Oxide
		
        private void Init()
        {
            permission.RegisterPermission("eventscore.admin", this);
			LoadDefaultConfig();
            eUsersScore = new Dictionary<ulong, EventsScores>();
            EventsList = new List<eInfo>();
        }

        void OnServerInitialized()
        {
            EventsList.Add(new eInfo { eName = EventName_1, eBackground = EventColor_1, eLeaders = "null", eText = EventText_1, eTime = EventTime_1 });
            EventsList.Add(new eInfo { eName = EventName_2, eBackground = EventColor_2, eLeaders = "null", eText = EventText_2, eTime = EventTime_2 });

            AutoETimer = timer.Once(EventAutoStart, AutoStart);
			
			if (Use_ServerRewards && !ServerRewards)
            {
                PrintError("Плагин планировал использовать баланс для выдачи бонуса за ивент, но плагин был выгружен!");
                return;
            }

            if (Use_UniversalShop && !UniversalShop)
            {
                PrintError("Плагин планировал использовать баланс для выдачи бонуса за ивент, но плагин был выгружен!");
                return;
            }

            if (Use_RustShop && !RustShop)
            {
                PrintError("Плагин планировал использовать баланс для выдачи бонуса за ивент, но плагин был выгружен!");
                return;
            }
        }
		
		private void Unload() 
        {
		BasePlayer.activePlayerList.ToList().ForEach(p => CuiHelper.DestroyUi(p, NotifyLayer));
        } 
		
        #endregion

        #region Core
		
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (curEvent != 1 || !eActive) return null;

            var player = entity as BasePlayer;
            if (!eUsersScore.ContainsKey(player.userID)) eUsersScore.Add(player.userID, new EventsScores { UserName = player.displayName, UserScore = item.amount });
            else
            {
                eUsersScore[player.userID].UserScore += item.amount;
            }
            return null;
        }
		
        #endregion

        #region GUI Interface
		
        private string Layer = "UI_EventGui";
        private void Draw_UIEvents(BasePlayer player)
        {
            if (player == null)
            {
                return;
            }

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.1 0.86", AnchorMax = "0.1 0.86", OffsetMin = "-130 -55", OffsetMax = "110 55" },
                Image = { Color = HexToRustFormat("#000000"), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" }
            }, "Hud", Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat(EventsList[curEvent].eBackground), Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent {  Text = EventsList[curEvent].eName + "\n" +
                                            EventsList[curEvent].eText + "\n" +
                                            "Ваш счёт: " + Math.Round(eUsersScore[player.userID].UserScore) + "\n" +
                                            "Лидер: " + GetLeader(), Align = TextAnchor.UpperCenter, FontSize = 15, Font = "robotocondensed-bold.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    new CuiOutlineComponent { Distance = "0.2 0.2", Color = "0 0 0 0" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiTextComponent {  Text = $"Осталось: {eTimeLeft} с", Align = TextAnchor.LowerCenter, FontSize = 15, Font = "robotocondensed-bold.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    new CuiOutlineComponent { Distance = "0.1 0.1", Color = "0 0 0 0" }
                }
            });

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }
		
		private string NotifyLayer = "UI_NotifyLayer";
		private void ShowNotify(BasePlayer player, string text)
        {
            timer.Once(5f, () => CuiHelper.DestroyUi(player, NotifyLayer));
            CuiElementContainer container = new CuiElementContainer();
            
			container.Add(new CuiButton
            {
				RectTransform = { AnchorMin = "0 0.85", AnchorMax = "1 0.93", OffsetMax = "0 0" }, 
                Button = { Color = HexToRustFormat(EventsList[curEvent].eBackground) }, 
                Text = { FadeIn = 2f, Text = text, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 16 }
            }, "Overlay", NotifyLayer);

            CuiHelper.AddUi(player, container);
        }
		
        #endregion

        #region Utils
		
        private void AutoStart()
        {
            if(BasePlayer.activePlayerList.Count < EventMaxPlayer)
            {
                if (AutoETimer.Destroyed)
                {
                    AutoETimer = timer.Once(EventAutoStart, AutoStart);
                    //AutoETimer.Destroy();
                }
                else
                {
                    AutoETimer.Destroy();
                    AutoETimer = timer.Once(EventAutoStart, AutoStart);
                }
                PrintWarning("Недостаточно игроков для запуска ивента!");
                return;
            }

            System.Random rnd = new System.Random();
            EventStart(rnd.Next(0, EventsList.Count - 1));
        }

        private string GetLeader()
        {
            if (!eActive) return "null";
            if (eUsersScore == null) return "null";

            ulong bestplrid = 0;
            double bestscore = -1;
            if (eUsersScore.Count > 0) { 
                foreach (var best in eUsersScore)
                {
                    if (best.Value.UserScore > bestscore)
                    {
                        bestscore = best.Value.UserScore;
                        bestplrid = best.Key;
                    }
                }
            } else {
                return "Нету";
            }
            eLastLeader = BasePlayer.FindByID(bestplrid);
            string name = eLastLeader == null ? "" : eLastLeader.displayName;
            return $"{name}   ({Math.Round(bestscore)})";
        }
		
		private void EventStart(int eventId)
        {
            if (!AutoETimer.Destroyed) AutoETimer.Destroy();
            curEvent = eventId;
            switch (eventId)
            {
                case 0:
                    {
                        eActive = true;
                        EventsTimer = timer.Every(1f, UpdateEventKing);
                        break;
                    }
                case 1:
                    {
                        eActive = true;
                        EventsTimer = timer.Every(1f, UpdateEventFarm);
                        break;
                    }
                default: break;
            }
        }

        private void EventEnd()
        {
            eActive = false;
            if(!EventsTimer.Destroyed) EventsTimer.Destroy();
            if (AutoETimer.Destroyed)
            {
                AutoETimer = timer.Once(EventAutoStart, AutoStart);
                //AutoETimer.Destroy();
            }
            else
            {
                AutoETimer.Destroy();
                AutoETimer = timer.Once(EventAutoStart, AutoStart);
            }
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
            eUsersScore.Clear();
        }
		
        #region Commands
        
        [ChatCommand("ev"), Permission("eventscore.admin")]
        void CMD_StartEvent(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.userID.ToString(), "eventscore.admin"))
            {
                if (args.Length > 0)
                {
                    if (args[0] == "end")
                    {
                        if (!eActive)
                        {
                            SendReply(player, "Нечего останавливать!");
                            return;
                        }
                        EventEnd();
                        SendReply(player, "Вы остановили ивент!");
                        return;
                    }

                    if (eActive)
                    {
                        SendReply(player, "Уже существует активный ивент!");
                        return;
                    }

                    if (args[0] == "king")
                    {
                        
                        EventStart(0);
                        SendReply(player, "Вы запустили ивент!");
                    }
                    else if (args[0] == "farm")
                    {
                        EventStart(1);
                        SendReply(player, "Вы запустили ивент!");
                    }
                    else
                    {
                        SendReply(player, "Неизвесный Ивент!");
                    }
                    
                }
            }
            else
            {
                SendReply(player, "В доступе отказано!");
            }
        }
		
        #endregion

        #region EVENT_KING
        private void UpdateEventKing()
        {
            if (eTimeLeft == 0 && eActive)
            {
                eTimeLeft = EventsList[0].eTime;
                if (eUsersScore == null) return;
                foreach(var player in BasePlayer.activePlayerList)
                {
                    if (!eUsersScore.ContainsKey(player.userID)) eUsersScore.Add(player.userID, new EventsScores { UserName = player.displayName, UserScore = 0 });
                    else
                    {
                        eUsersScore[player.userID].UserScore = 0;
                    }
                    Draw_UIEvents(player);
                }
            }
            else if(eTimeLeft == 0 && !eActive)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, Layer);
                }
            }
            else
            {
                if (eTimeLeft > 0) eTimeLeft--;
                else eTimeLeft = 0;
                if (eTimeLeft < 2)
                {
                    eActive = false;
                    if(!EventsTimer.Destroyed) EventsTimer.Destroy();
                    if (AutoETimer.Destroyed)
                    {
                        AutoETimer = timer.Once(EventAutoStart, AutoStart);
                        //AutoETimer.Destroy();
                    }
                    else
                    {
                        AutoETimer.Destroy();
                        AutoETimer = timer.Once(EventAutoStart, AutoStart);
                    }
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(player, Layer);
					    ShowNotify(player, $"Ивент › {EventsList[curEvent].eName} ‹ окончен!\n" +
						                   $"Победитель: › {eLastLeader.displayName} ‹\n" +
						                   $"Он получает: {BalanceAmount} бонусов на свой баланс мини-магазина!");
                    }
                    eUsersScore.Clear();
					
                    if(Use_UniversalShop) 
					{
						UniversalShop?.Call("API_ShopAddBalance", eLastLeader.userID, BalanceAmount);
					}
					
					if(Use_RustShop)
					{
						RustShop?.Call("AddBalance", eLastLeader.userID, BalanceAmount);
					}
					
					if(Use_ServerRewards)
					{
						ServerRewards?.Call("AddPoints", eLastLeader.userID, BalanceAmount);
					}
                }
                else
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        var y = player.transform.position.y;
                        if (!eUsersScore.ContainsKey(player.userID)) eUsersScore.Add(player.userID, new EventsScores { UserName = player.displayName, UserScore = y });
                        else
                        {
                            eUsersScore[player.userID].UserScore = y;
                        }
                        Draw_UIEvents(player);
                    }
                }
            }
        }

        #endregion

        #region EVENT_FARM
        private void UpdateEventFarm()
        {
            if (eTimeLeft == 0 && eActive)
            {
                eTimeLeft = EventsList[1].eTime;
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (!eUsersScore.ContainsKey(player.userID)) eUsersScore.Add(player.userID, new EventsScores { UserName = player.displayName, UserScore = 0 });
                    else
                    {
                        eUsersScore[player.userID].UserScore = 0;
                    }
                    Draw_UIEvents(player);
                }
            }
            else if (eTimeLeft == 0 && !eActive)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CuiHelper.DestroyUi(player, Layer);
                }
            }
            else
            {
                if (eTimeLeft > 0) eTimeLeft--;
                else eTimeLeft = 0;
                if (eTimeLeft < 2)
                {
                    eActive = false;
                    if (!EventsTimer.Destroyed) EventsTimer.Destroy();
                    if (AutoETimer.Destroyed)
                    {
                        AutoETimer = timer.Once(EventAutoStart, AutoStart);
                        //AutoETimer.Destroy();
                    }
                    else
                    {
                        AutoETimer.Destroy();
                        AutoETimer = timer.Once(EventAutoStart, AutoStart);
                    }
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        CuiHelper.DestroyUi(player, Layer);
					    ShowNotify(player, $"Ивент › {EventsList[curEvent].eName} ‹ окончен!\n" +
						                   $"Победитель: › {eLastLeader.displayName} ‹\n" +
						                   $"Он получает: {BalanceAmount} бонусов на свой баланс мини-магазина!");
                    }
                    eUsersScore.Clear();
					
                    if(Use_UniversalShop) 
					{
						UniversalShop?.Call("API_ShopAddBalance", eLastLeader.userID, BalanceAmount);
					}
					
					if(Use_RustShop)
					{
						RustShop?.Call("AddBalance", eLastLeader.userID, BalanceAmount);
					}
					
					if(Use_ServerRewards)
					{
						ServerRewards?.Call("AddPoints", eLastLeader.userID, BalanceAmount);
					}
                }
                else
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (!eUsersScore.ContainsKey(player.userID)) eUsersScore.Add(player.userID, new EventsScores { UserName = player.displayName, UserScore = 0 });
                        Draw_UIEvents(player);
                    }
                }
            }
        }

        #endregion

        #region Others
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }
		
		private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }

        public struct eInfo
        {
            public string eName;
            public string eBackground;
            public string eLeaders;
            public string eText;
            public int eTime;
        }

        class EventsScores
        {
            public string UserName;
            public double UserScore;
        }
        #endregion

        #endregion
    }
}

