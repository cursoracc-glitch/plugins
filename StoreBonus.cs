using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("StoreBonus", "OxideBro", "1.0.1")]
    class StoreBonus : RustPlugin
    {
        List<Counts> logs = new List<Counts>();
        public int timercallbackdelay = 0;
        class Counts
        {
            [JsonProperty("time")]
            public string time;
            public Counts(BasePlayer player)
            {
                this.time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        #region Testing
        Dictionary<BasePlayer, int> timers = new Dictionary<BasePlayer, int>();
        List<ulong> activePlayers = new List<ulong>();


        #region UI



        string HandleArgs(string json, params object[] args)
        {
            var reply = 793;
            for (int i = 0; i < args.Length; i++)
                json = json.Replace("{" + i + "}", args[i].ToString());
            return json;
        }
        private double GetCurrentTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;

        [ConsoleCommand("getbonus")]
        void CmdGetBonus(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (data.StoreData[player.userID].EnabledBonus == 0)
            {
                SendReply(player, "У Вас нету бонусов");
                return;
            }
            if (data.StoreData[player.userID].EnabledBonus >= 1)
            {
                if (!EnableGUIPlayer)
                {
                    CuiHelper.DestroyUi(player, "UIPlayer");
                }
                data.StoreData[player.userID].EnabledBonus = data.StoreData[player.userID].EnabledBonus = 0;
                data.StoreData[player.userID].Bonus = data.StoreData[player.userID].Bonus + 1;
                SaveData();
                CuiHelper.DestroyUi(player, "GetBonus");
                UpdateTimer(player);
                SendReply(player, "Вы получили свой заслуженный <color=#ECBE13>1 бонус</color>. Что бы проверить баланс, введите <color=#ECBE13>/bonus</color>");
            }

        }
        public BasePlayer FindBasePlayer(string nameOrUserId)
        {
            nameOrUserId = nameOrUserId.ToLower();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId)
                    return player;
            }
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (player.displayName.ToLower().Contains(nameOrUserId) || player.UserIDString == nameOrUserId)
                    return player;
            }
            return default(BasePlayer);
        }



        void GradeTimerHandler()
        {
            foreach (var player in timers.Keys.ToList())
            {
                var seconds = --timers[player];
                if (seconds <= 0)
                {
                    var resetTime = (GameActive * 60);
                    var TimerBonus = FormatTime(TimeSpan.FromSeconds(resetTime));
                    timers.Remove(player);
                    data.StoreData[player.userID].EnabledBonus = data.StoreData[player.userID].EnabledBonus + 1;
                    SaveData();
                    if (EnableMsg)
                    {
                        SendReply(player, ChatSms, TimerBonus, GetShop);
                    }
                    CuiHelper.DestroyUi(player, "OpenBonus");
                    DrawUIGetBonus(player);
                    continue;
                }
                if (data.StoreData[player.userID].EnabledBonus == 0)
                {
                    if (EnableGUIPlayer)
                    {
                        DrawUIBalance(player, seconds);
                    }
                }
            }
        }




        void UpdateTimer(BasePlayer player)
        {
            if (player == null) return;

            var resetTime = (GameActive * 60);
            timers[player] = resetTime;

            DrawUIBalance(player, timers[player]);
        }

        void DeactivateTimer(BasePlayer player)
        {
            ulong userId;
            if (activePlayers.Contains(player.userID))
            {
                activePlayers.Remove(player.userID);

                timers.Remove(player);
            }
        }

        void ActivateTimer(ulong userId)
        {
            if (!activePlayers.Contains(userId))
            {
                activePlayers.Add(userId);
            }
        }


        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "дней", "дня", "день")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "часов", "часа", "час")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";

            if (time.Seconds != 0)
                result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";

            return result;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        private string GetFormatTime(int seconds)
        {
            var time = seconds;
            double minutes = Math.Floor((double)(time / 60));
            time -= (int)(minutes * 60);
            return string.Format("{0:00}:{1:00}", minutes, time);
        }
        #endregion

        #endregion

        #region CONFIGURATION
        public Timer mytimer;
        public Timer mytimer2;
        string secret = "123456789";
        private string ChatSms = "<size=15>Спасибо что провели на сервере <color=#A6FFAC>{0}</color>, за это Вам подарок 1 бонус!</size>\n<size=14>Бонусы вы сможете обменять на рубли в игровом магазине <color=#A6FFAC>{1}</color>\nПолучить бонус вы сможете нажав кнопку ниже <color=#A6FFAC>ЗАБРАТЬ БОНУС</color></size>";
        string shopId = "134";
        int amount1 = 1;
        bool EnableMsg = true;
        bool LogsPlayer = true;
        public int GameActive = 10;
        bool MoscowStore = false;
        bool EnableGUIPlayer = true;
        string EnableGUIPlayerMin = "0.645 0.051";
        string EnableGUIPlayerMax = "0.844 0.111";
        string GUIEnabledColor = "0.44 0.55 0.26 0.70";
        string GUIEnabledText = "<size=18>ЗАБРАТЬ БОНУС</size>";
        public string GetShop = "shop.gamestores.ru";

        private void LoadDefaultConfig()
        {
            GetConfig("Настройки", "Секретный ключ магазина (SECRET.KEY GameStores)", ref secret);
            GetConfig("Настройки", "ID магазина (SHOP.ID GameStores)", ref shopId);
            GetConfig("Настройки", "Курс 1 бонуса (руб)", ref amount1);
            GetConfig("Настройки", "Время активности на сервере за какое выдаеться бонус (минуты)", ref GameActive);
            GetConfig("Настройки", "Название магазина", ref GetShop);
            GetConfig("Настройки", "Включить логирование обмена средств", ref LogsPlayer);
            GetConfig("Настройки", "У Вас магазин Moscow.ovh (true = да, false = GameStores)", ref MoscowStore);

            GetConfig("Сообщения", "Включить сообщение о выдаче бонуса игроку в чат", ref EnableMsg);

            GetConfig("GUI Баланс", "Включить панель баланса", ref EnableGUIPlayer);
            GetConfig("GUI Баланс", "Anchor Min", ref EnableGUIPlayerMin);
            GetConfig("GUI Баланс", "Anchor Max", ref EnableGUIPlayerMax);
            SaveConfig();
        }

        private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }
            Config[menu, Key] = var;
        }

        #endregion

        #region COMMANDS
        [ChatCommand("bonus")]
        void cmdChatBonus(BasePlayer player, string command, string[] args)
        {
            if (data.StoreData[player.userID].Bonus == 0)
            {

                var resetTime = (GameActive * 60);
                var TimerBonus = (resetTime / 60);
                SendReply(player, $"<size=15>У вас нету бонусов =(\nИграйте и получайте их только у нас!\nЗа <color=#A6FFAC>{TimerBonus} мин. </color>активной игры вы получаете <color=#A6FFAC>1 бонус</color>.</size>");
                return;
            }
            if (args.Count() == 0)
            {
                DrawUI(player);
                return;
            }
            if (args.Count() == 1)
            {
                SendReply(player, "Вы не правильно ввели команду.\nИспользуйте /bonus или /bonus get Кол-ство или /bonus get all");
                return;
            }
            if (args[0] == "get")
            {

                if (args[1] == "all")
                {
                    int amount2 = (data.StoreData[player.userID].Bonus * amount1);
                    if (MoscowStore)
                    {
                        APIChangeUserBalance(player.userID, amount2, null);
                    }
                    if (!MoscowStore)
                    {
                        MoneyPlus(player.userID, amount2);
                    }
                    SendReply(player, $"<size=15>Вы обменяли {data.StoreData[player.userID].Bonus} бон. на <color=RED>{amount2}</color> руб..\nДеньги зачислены на Ваш игровой счет в магазине</size>");
                    data.StoreData[player.userID].Bonus = data.StoreData[player.userID].Bonus = 0;
                    return;
                }

                int amounts;
                if (!int.TryParse(args[1], out amounts))
                {
                    SendReply(player, "Необходимо ввести число!");
                    return;
                }
                if (amounts <= 0)
                {
                    SendReply(player, "Необходимо ввести положительное число больше 0!");
                    return;
                }
                int amounts1 = (amounts * amount1);
                if (data.StoreData[player.userID].Bonus < amounts)
                {
                    SendReply(player, $"<size=15>У вас не хватает бонусов. На балансе: {data.StoreData[player.userID].Bonus}</size>");
                    return;
                }
                if (MoscowStore)
                {
                    APIChangeUserBalance(player.userID, amounts1, null);
                }
                if (!MoscowStore)
                {
                    MoneyPlus(player.userID, amounts1);
                }
                data.StoreData[player.userID].Bonus = data.StoreData[player.userID].Bonus - amounts;
                SendReply(player, $"<size=15>Вы обменяли {amounts} бон. на {amounts1} руб. Осталось: {data.StoreData[player.userID].Bonus}.\nДеньги зачислены на Ваш игровой счет в магазине</size>");
                if (LogsPlayer)
                {
                    logs.Add(new Counts(player));
                    if (LogsPlayer)
                    {
                        LogToFile("log", $"ID:{logs.Count - 1} ({DateTime.Now.ToShortTimeString()}) {player.displayName} ({player.userID}) обменял  {amounts} бон. на {amounts1} руб", this);
                    }
                }
            }
        }

        [ConsoleCommand("bonus.plus")]
        void cmdStoreBonusAdd(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (arg.Connection != null) return;

            if (arg.Args.Length != 2)
            {
                SendReply(arg, $"YOU NEED WRITE 2 PARAMS");
                return;
            }

            ulong userid;
            if (!ulong.TryParse(arg.Args[0], out userid))
            {
                SendReply(arg, $"FIRST PARAM NEED BE AS STEAM_ID");
                return;
            }
            int amount;
            if (!int.TryParse(arg.Args[1], out amount))
            {
                SendReply(arg, $"SECOND PARAM NEED BE AS AMOUNT");
                return;
            }
            data.StoreData[userid].Bonus = data.StoreData[userid].Bonus + amount;
            Puts($"Игроку {userid} выдано бонусов: {amount}. Текущий баланс: {data.StoreData[userid].Bonus}");
            LogToFile("logConsoleBonus", $"({DateTime.Now.ToShortTimeString()}) Игроку {userid} выдано бонусов: {amount}. Текущий баланс: {data.StoreData[userid].Bonus}", this);
            SaveData();
        }

        [ConsoleCommand("bonus.minus")]
        void cmdStoreBonusRemove(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;

            if (arg.Args.Length != 2)
            {
                SendReply(arg, $"YOU NEED WRITE 2 PARAMS");
                return;
            }

            ulong userid;
            if (!ulong.TryParse(arg.Args[0], out userid))
            {
                SendReply(arg, $"FIRST PARAM NEED BE AS STEAM_ID");
                return;
            }
            int amount;
            if (!int.TryParse(arg.Args[1], out amount))
            {
                SendReply(arg, $"SECOND PARAM NEED BE AS AMOUNT");
                return;
            }
            if (data.StoreData[userid].Bonus < amount)
            {
                Puts("У игрока нету столько бонусов, у него: " + data.StoreData[userid].Bonus);
                return;
            }
            data.StoreData[userid].Bonus = data.StoreData[userid].Bonus - amount;
            Puts($"Игроку {userid} удалено бонусов: {amount}. Текущий баланс: {data.StoreData[userid].Bonus}");
            LogToFile("logConsoleBonus", $"({DateTime.Now.ToShortTimeString()}) Игроку {userid} удалено бонусов: {amount}. Текущий баланс: {data.StoreData[userid].Bonus}", this);
            SaveData();
        }

        [ConsoleCommand("money.plus")]
        void cmdStoreMoneyAdd(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            if (arg.Args.Length != 2)
            {
                SendReply(arg, $"YOU NEED WRITE 2 PARAMS");
                return;
            }
            ulong userid;
            if (!ulong.TryParse(arg.Args[0], out userid))
            {
                SendReply(arg, $"FIRST PARAM NEED BE AS STEAM_ID");
                return;
            }
            int amount;
            if (!int.TryParse(arg.Args[1], out amount))
            {
                SendReply(arg, $"SECOND PARAM NEED BE AS AMOUNT");
                return;
            }
            if (MoscowStore)
            {
                APIChangeUserBalance(userid, amount, null);
            }
            if (!MoscowStore)
            {
                MoneyPlus(userid, amount);
            }
            Puts($"Игроку {userid} выдано рублей: {amount}");
            LogToFile("logConsoleMoney", $"({DateTime.Now.ToShortTimeString()}) Игроку {userid} выдано рублей: {amount}", this);

        }

        [ConsoleCommand("money.minus")]
        void cmdStoreMoneyRemove(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;

            if (arg.Args.Length != 2)
            {
                SendReply(arg, $"YOU NEED WRITE 2 PARAMS");
                return;
            }
            var reply = 0;
            ulong userid;
            if (!ulong.TryParse(arg.Args[0], out userid))
            {
                SendReply(arg, $"FIRST PARAM NEED BE AS STEAM_ID");
                return;
            }
            int amount;
            if (!int.TryParse(arg.Args[1], out amount))
            {
                SendReply(arg, $"SECOND PARAM NEED BE AS AMOUNT");
                return;
            }
            if (MoscowStore)
            {
                APIChangeUserBalance(userid, amount, null);
            }
            if (!MoscowStore)
            {
                MoneyMinus(userid, amount);
            }
            Puts($"Игроку {userid} снято рублей: {amount}");
            LogToFile("logConsoleMoney", $"({DateTime.Now.ToShortTimeString()}) Игроку {userid} снято рублей: {amount}", this);
        }

        [ConsoleCommand("bonusclose")]
        void CmdDestroyGui(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            DestroyUI(player);
        }
        [ConsoleCommand("drawui")]
        void DrawUIPlayer(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            DrawUI(player);
        }
        [ConsoleCommand("acceptclose")]
        void CmdDestroyGuiAccept(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            DestroyUIAccept(player);
        }


        [ConsoleCommand("bonus.accept")]
        void CmdBonusGetAllAccept(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            DestroyUIAccept(player);
            DrawUIAccept(player);
        }

        [ConsoleCommand("bonus.getall")]
        void CmdBonusGetAll(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (!player)
            {
                SendReply(arg, "Может быть вызвана только в игре!");
                return;
            }

            var resetTime = (GameActive * 60);
            var TimerBonus = (resetTime / 60);
            if (data.StoreData[player.userID].Bonus == 0)
            {
                SendReply(player, $"<size=15>У Вас нету бонусов =(\nИграйте и получайте их только у нас!\nЗа <color=#A6FFAC>{TimerBonus} мин. </color>активной игры вы получаете <color=#A6FFAC>1 бонус</color>.</size>");
                return;
            }
            int amount2 = (data.StoreData[player.userID].Bonus * amount1);
            if (MoscowStore)
            {
                APIChangeUserBalance(player.userID, amount2, null);
            }
            if (!MoscowStore)
            {
                MoneyPlus(player.userID, amount2);
            }
            SendReply(player, $"<size=15>Вы обменяли {data.StoreData[player.userID].Bonus} бон. на {amount2} руб.\nДеньги зачислены на Ваш игровой счет в магазине</size>");
            DestroyUI(player);
            if (LogsPlayer)
            {
                int amount3 = (data.StoreData[player.userID].Bonus * amount1);
                logs.Add(new Counts(player));
                if (LogsPlayer)
                {
                    LogToFile("log", $"ID:{logs.Count - 1} ({DateTime.Now.ToShortTimeString()}) {player.displayName} ({player.userID}) обменял {data.StoreData[player.userID].Bonus} бон. на {amount2} руб", this);
                }
            }
            data.StoreData[player.userID].Bonus = data.StoreData[player.userID].Bonus = 0;
        }
        #endregion

        #region UI
        void DrawUIBalance(BasePlayer player, int seconds)
        {
            CuiHelper.DestroyUi(player, "OpenBonus");

            int Balance = (data.StoreData[player.userID].Bonus);
            CuiElementContainer Container = new CuiElementContainer();
            CuiElement OpenBonus = new CuiElement
            {
                Name = "OpenBonus",
                Parent = "UIPlayer",
                Components =
                {
                    new CuiTextComponent {
                        Text = $"<size=15><color=#D3D3D3>Баланс бонусов: <color=#ECBE13>{Balance}</color></color></size>\n<size=14><color=#D3D3D3>Следующий бонус через <color=#ECBE13>{GetFormatTime(seconds)}</color></color></size>",
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1",
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.13 0",
                        AnchorMax = "1 1"
                    },
                        new CuiOutlineComponent {
                            Color = "0 0 0 0.5",
                            Distance = "1.0 -0.5"
                        }
                }
            };
            Container.Add(OpenBonus);
            CuiHelper.AddUi(player, Container);

        }

        void DrawUIGetBonus(BasePlayer player)
        {
            DrawUIPlayer(player);
            CuiHelper.DestroyUi(player, "OpenBonus");
            CuiElementContainer Container = new CuiElementContainer();

            CuiElement GetBonus = new CuiElement
            {
                Name = "GetBonus",
                Parent = "UIPlayer",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "getbonus",
                        Color = GUIEnabledColor,
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                }
            };
            CuiElement TextGetBonus = new CuiElement
            {
                Name = "TextGetBonus",
                Parent = "GetBonus",
                Components =
                {
                    new CuiTextComponent {
                        Text = GUIEnabledText,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                        new CuiOutlineComponent {
                            Color = "0 0 0 0.5", Distance = "1.0 -0.5"

                        }
                }
            };
            Container.Add(GetBonus);
            Container.Add(TextGetBonus);
            CuiHelper.AddUi(player, Container);

        }

        void DrawUIPlayer(BasePlayer player)
        {

            DestroyUIPlayer(player);
            CuiElementContainer Container = new CuiElementContainer();
            CuiElement BonusIcon = new CuiElement
            {
                Name = "BonusIcon",
                Parent = "UIPlayer",
                Components = {
                        new CuiRawImageComponent {
                            Url = "http://rustplugin.ru/info/iconUI.png",
                            Color = "0.75 0.75 0.75 1.00"
                        },
                        new CuiRectTransformComponent {
                        AnchorMin = "0.02 0.2",
                        AnchorMax = "0.095 0.75"
                        }
                }
            };
            CuiElement BPUI = new CuiElement
            {
                Name = "BPUI",
                Parent = "UIPlayer",
                Components = {
                        new CuiImageComponent {
                            Color = "0 0 0 0.8"
                        },
                        new CuiRectTransformComponent {
                             AnchorMin = "0 0",
                        AnchorMax = "0.12 0.98"
                        }
                    }
            };
            CuiElement UIPlayer = new CuiElement
            {
                Name = "UIPlayer",
                Parent = "Hud",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "drawui",
                        Color = "0.75 0.75 0.75 0.2",
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = EnableGUIPlayerMin,
                        AnchorMax = EnableGUIPlayerMax
                    }
                }
            };

            Container.Add(UIPlayer);
            Container.Add(BPUI);
            Container.Add(BonusIcon);
            CuiHelper.AddUi(player, Container);

            var resetTime = (GameActive * 60);
            if (EnableGUIPlayer)
            {
                DrawUIBalance(player, resetTime);
            }

        }

        void DrawUIAccept(BasePlayer player)
        {
            int amount2 = (data.StoreData[player.userID].Bonus * amount1);
            CuiElementContainer Container = new CuiElementContainer();
            CuiElement PlayerUI1 = new CuiElement
            {
                Name = "PlayerUI1",
                Parent = "ContainerUI",
                Components = {
                        new CuiImageComponent {
                            Color = "0.75 0.75 0.75 0.2"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.343 0.204",
                            AnchorMax = "0.635 0.725"
                        }
                    }

            };
            CuiElement BackGroundAccept = new CuiElement
            {
                Name = "BackgroundAccept",
                Parent = "Overlay",
                Components = {
                        new CuiImageComponent {
                            Color = "0 0 0 0.95"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.300 0.400",
                            AnchorMax = "0.700 0.550"
                        }
                    }
            };
            CuiElement BackgroundAcceptText = new CuiElement
            {
                Name = "BackgroundAcceptText",
                Parent = "BackgroundAccept",
                Components =
                {
                    new CuiTextComponent {
                        Text = $"<size=18>Вы действительно хотите обменять <color=#ECBE13>{data.StoreData[player.userID].Bonus}</color> бон. на <color=#ECBE13>{amount2}</color> руб ?</size>",
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0.6",
                        AnchorMax = "1 1"
                    },
                        new CuiOutlineComponent {
                            Color = "0 0 0 0.5", Distance = "1.0 -0.5"

                        }
                }
            };
            CuiElement CloseButton1 = new CuiElement
            {
                Name = "CloseButton1",
                Parent = "BackgroundAccept",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "acceptclose",
                        Color = "1.00 0.09 0.20 0.7",
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.55 0.1",
                        AnchorMax = "0.8 0.5"
                    },
                    new CuiNeedsCursorComponent()
                }
            };
            CuiElement CloseButtonText1 = new CuiElement
            {
                Name = "CloseButtonText1",
                Parent = "CloseButton1",
                Components =
                {
                    new CuiTextComponent {
                        Text = $"<size=18>Отменить</size>",
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                        new CuiOutlineComponent {
                            Color = "0 0 0 0.5", Distance = "1.0 -0.5"

                        }
                }
            };
            CuiElement AcceptButton = new CuiElement
            {
                Name = "AcceptButton",
                Parent = "BackgroundAccept",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "bonus.getall",
                        Color = "0.00 1.00 0.10 0.7",
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.2 0.1",
                        AnchorMax = "0.5 0.5"
                    },
                    new CuiNeedsCursorComponent()
                }
            };
            CuiElement AcceptButtonText = new CuiElement
            {
                Name = "AcceptButtonText",
                Parent = "AcceptButton",
                Components =
                {
                    new CuiTextComponent {
                        Text = $"<size=18>Подтвердить</size>",
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                        new CuiOutlineComponent {
                            Color = "0 0 0 0.5", Distance = "1.0 -0.5"

                        }
                }
            };
            Container.Add(PlayerUI1);
            Container.Add(BackGroundAccept);
            Container.Add(BackgroundAcceptText);
            Container.Add(CloseButton1);
            Container.Add(CloseButtonText1);
            Container.Add(AcceptButton);
            Container.Add(AcceptButtonText);
            CuiHelper.AddUi(player, Container);
        }

        void DrawUI(BasePlayer player)
        {
            int Hours = (data.StoreData[player.userID].GameTime * 60);
            int amount2 = (data.StoreData[player.userID].Bonus * amount1);
            CuiElementContainer Container = new CuiElementContainer();
            CuiElement ContainerUI = new CuiElement
            {
                Name = "ContainerUI",
                Parent = "Hud",
                Components = {
                        new CuiImageComponent {
                            Color = "0 0 0 0"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        new CuiNeedsCursorComponent()
                    }
            };
            CuiElement PlayerUI = new CuiElement
            {
                Name = "PlayerUI",
                Parent = "ContainerUI",
                Components = {
                        new CuiImageComponent {
                            Color = "0 0 0 0.75"
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0.343 0.204",
                            AnchorMax = "0.635 0.725"
                        }
                    }
            };

            CuiElement PlayerName = new CuiElement
            {
                Name = "PlayerName",
                Parent = "PlayerUI",
                Components = {
                        new CuiTextComponent {
                            Text = $"<color=#ECBE13><size=20>СИСТЕМА БОНУСОВ</size></color>\n\n<size=24>{player.displayName}</size>\nВремя на сервере: <color=#ECBE13>{FormatTime(TimeSpan.FromSeconds(Hours))}</color>\n\n<size=17>Бонусов на балансе: <color=#ECBE13>{data.StoreData[player.userID].Bonus}</color></size>\n<size=12>(Вы сможете получить по текущему курсу <color=#ECBE13>{amount2} руб.</color>)</size>\n\nКурс бонусов на сегодня: <color=#ECBE13>1 бонус = {amount1} руб.</color>\nЧто бы обменять бонусы, нажмите ОБМЕНЯТЬ!\n\n\n\n\n\n\n\nЧто бы обменять определенное количество введите в чате:\n<color=#ECBE13>/bonus get Количество</color>",
                            Align = TextAnchor.UpperCenter
                        },
                        new CuiRectTransformComponent {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0.95"
                        },
                        new CuiOutlineComponent {
                            Color = "0 0 0 0.5", Distance = "1.0 -0.5"
                        }
                    }
            };
            CuiElement CloseButton = new CuiElement
            {
                Name = "CloseButton",
                Parent = "PlayerUI",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "bonusclose",
                        Color = "0 0 0 0.5",
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.868 0.87",
                        AnchorMax = "0.998 0.998"
                    },
                }
            };
            CuiElement CloseButtonText = new CuiElement
            {
                Name = "CloseButtonText",
                Parent = "CloseButton",
                Components =
                {
                    new CuiTextComponent {
                        Text = $"<size=18>X</size>",
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                        new CuiOutlineComponent {
                            Color = "0 0 0 0.5", Distance = "1.0 -0.5"

                        }
                }
            };
            CuiElement Button = new CuiElement
            {
                Name = "Button",
                Parent = "PlayerUI",
                Components =
                {
                    new CuiButtonComponent
                    {
                        Command = "bonus.accept",
                        Color = "1.00 1.00 1.00 0.2",
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.02 0.23",
                        AnchorMax = "0.98 0.4"
                    },

                }
            };
            CuiElement ButtonText = new CuiElement
            {
                Name = "ButtonText",
                Parent = "Button",
                Components =
                {
                    new CuiTextComponent {
                        Text = $"<size=19>Обменять все бонусы на <color=#ECBE13>{amount2} руб.</color></size>",
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    },
                        new CuiOutlineComponent {
                            Color = "0 0 0 0.7", Distance = "1.0 -0.5"

                        }
                }
            };

            Container.Add(ContainerUI);
            Container.Add(PlayerUI);
            Container.Add(PlayerName);
            Container.Add(CloseButton);
            Container.Add(CloseButtonText);
            Container.Add(Button);
            Container.Add(ButtonText);
            CuiHelper.AddUi(player, Container);
        }

        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ContainerUI");
            CuiHelper.DestroyUi(player, "BackgroundAccept");
        }
        void DestroyUIPlayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "UIPlayer");
        }
        void DestroyUIAccept(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BackgroundAccept");
            CuiHelper.DestroyUi(player, "PlayerUI1");
        }
        #endregion

        #region OXIDE HOOKS

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
                DestroyUIPlayer(player);
                DeactivateTimer(player);
            }
            SaveData();
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            DeactivateTimer(player);
        }


        void OnServerInitialized()
        {
            LoadDefaultConfig();
            StoreData = Interface.Oxide.DataFileSystem.GetFile("StoreBonus/StorePlayerData");
            LoadData();
            timer.Every(1f, GradeTimerHandler);
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerInit(player);
            }

            mytimer = timer.Repeat(60f, 0, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    data.StoreData[player.userID].GameTime = data.StoreData[player.userID].GameTime + 1;
                }
            });

            var resetTime = (GameActive * 60);
            var TimerBonus = (resetTime / 60);
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!player.IsSleeping())
                {
                    SaveData();
                    CuiHelper.DestroyUi(player, "OpenBonus");
                    if (data.StoreData[player.userID].EnabledBonus == 0)
                    {
                        if (EnableGUIPlayer)
                        {
                            DrawUIBalance(player, resetTime);
                        }
                    }
                }
            }
        }

        void LoadData()
        {
            logs = logsFile.ReadObject<List<Counts>>();

            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<DataStorage>("StoreBonus/StorePlayerData");
            }

            catch
            {
                data = new DataStorage();
            }
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (data.StoreData.ContainsKey(player.userID))
            {
                if (EnableGUIPlayer)
                {
                    DrawUIPlayer(player);
                }
                if (data.StoreData[player.userID].EnabledBonus != 0)
                {
                    DrawUIGetBonus(player);
                    return;
                }

            }
            else
            {
                OnPlayerInit(player);
            }
        }


        void SaveData()
        {
            logsFile.WriteObject(logs);
            StoreData.WriteObject(data);
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player == null) return;

            if (!data.StoreData.ContainsKey(player.userID))
            {
                data.StoreData.Add(player.userID, new STOREDATA()
                {
                    Name = player.displayName,
                    EnabledBonus = 0,
                    Bonus = 0,
                    GameTime = 0,
                });
                SaveData();
            }
            if (data.StoreData.ContainsKey(player.userID))
            {
                if (EnableGUIPlayer)
                {
                    DrawUIPlayer(player);
                }
            }
            if (data.StoreData[player.userID].Bonus > 0)
            {
                SendReply(player, "У вас есть не использованые бонусы!\nЧтобы их проверить наберите команду <color=#fee3b4>/bonus</color>");
            }
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                data.StoreData[player.userID].Name = player.displayName.ToString();
            }

            if (data.StoreData[player.userID].EnabledBonus == 0)
            {
                UpdateTimer(player);
                ActivateTimer(player.userID);
            }
            else
            {
                DrawUIGetBonus(player);
            }

        }
        #endregion

        #region Money
        void APIChangeUserBalance(ulong steam, int balanceChange, Action<string> callback)
        {
            plugins.Find("RustStore").CallHook("APIChangeUserBalance", steam, balanceChange, new Action<string>((result) =>
            {
                if (result == "SUCCESS")
                {
                    if (LogsPlayer)
                    {
                        LogToFile("logWEB", $"ID:{logs.Count - 1} ({DateTime.Now.ToShortTimeString()}): Отправлен запрос пользователем {steam} на пополнение баланса в размере: {balanceChange}", this);
                    }
                    return;
                }
                if (LogsPlayer) { LogToFile("logError", $"ID:{logs.Count - 1} ({DateTime.Now.ToShortTimeString()}): Баланс не был изменен, ошибка: {result}", this); }
            }));
        }

        void MoneyPlus(ulong userId, int amount)
        {
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                { "action", "moneys" },
                { "type", "plus" },
                { "steam_id", userId.ToString() },
                { "amount", amount.ToString() }
            });
        }

        void MoneyMinus(ulong userId, int amount)
        {
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                { "action", "moneys" },
                { "type", "minus" },
                { "steam_id", userId.ToString() },
                { "amount", amount.ToString() }
            });
        }

        void ExecuteApiRequest(Dictionary<string, string> args)
        {
            string url = $"http://panel.gamestores.ru/api?shop_id={shopId}&secret={secret}" +
                     $"{string.Join("", args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
            webrequest.EnqueueGet(url, (i, s) =>
            {
                if (i != 200)
                {
                    if (LogsPlayer)
                    {
                        LogToFile("logError", $"ID:{logs.Count - 1} ({DateTime.Now.ToShortTimeString()}): {url}\nCODE {i}: {s}", this);
                    }
                }
                else
                {
                    if (LogsPlayer)
                    {
                        LogToFile("logWEB", $"ID:{logs.Count - 1} ({DateTime.Now.ToShortTimeString()}): {s}", this);
                    }
                }
            }, this);
        }

        #endregion

        #region DATA

        class DataStorage
        {
            public Dictionary<ulong, STOREDATA> StoreData = new Dictionary<ulong, STOREDATA>();
            public DataStorage() { }
        }

        class STOREDATA
        {
            public string Name;
            public int EnabledBonus;
            public int Bonus;
            public int GameTime;
        }

        DataStorage data;

        private DynamicConfigFile StoreData;
        DynamicConfigFile logsFile = Interface.Oxide.DataFileSystem.GetFile("StoreBonus/StoreLogs");

        #endregion

    }
}
                    