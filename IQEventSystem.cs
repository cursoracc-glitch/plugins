using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQEventSystem", "Mercury", "0.0.2")]
    [Description("Не услышанный пионер Mercury")]
    class IQEventSystem : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin IQChat;
        #endregion

        #region Vars
        public bool EventStatus = false;
        public EventType EventLocal;
        public int LocalIndexEvent = 0;
        public enum EventType
        {
            Gather,
            PickUp,
            Search,
            Kills
        }
        public Dictionary<BasePlayer, int> EventPlayerList = new Dictionary<BasePlayer, int>();
        #endregion

        #region Configuration
        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка ивентов")]
            public List<Events> EventList = new List<Events>();
            [JsonProperty("Настройка интерфейса")]
            public InterfaceSetting InterfaceSettings = new InterfaceSetting();
            [JsonProperty("Через сколько запуск случайный ивент(в секундах)")]
            public float TimeToStartEvent;
            [JsonProperty("Время ожидания регистрации игроков на ивент")]
            public float TimerVotesWait;
            [JsonProperty("Префикс в чате(IQChat)")]
            public string PrefixForChat;
            [JsonProperty("Минимум игроков для запуска ивента")]
            public int MinimumOnline;

            internal class Events
            {
                [JsonProperty("Тип ивента : 0 - Добыча, 1 - Поднять с пола, 2 - Найти в ящике, 3 - Убийство")]
                public EventType EventTypes;
                [JsonProperty("Время ивента ( в секундах )")]
                public float TimerEvent;
                [JsonProperty("Отображаемое имя")]
                public string DisplayName;
                [JsonProperty("Описание ивента")]
                public string Description;
                [JsonProperty("Цель ивента : Shortname")]
                public string Shortname;
                [JsonProperty("Цель ивента : SkinID (если не требуется,оставляйте 0)")]
                public ulong SkinID;
                [JsonProperty("Награда за победу в ивенте")]
                public List<Reward> Rewards = new List<Reward>();

                internal class Reward
                {
                    [JsonProperty("Shortname")]
                    public string Shortname;
                    [JsonProperty("Команда")]
                    public string Command;
                    [JsonProperty("SkinID")]
                    public ulong SkinID;
                    [JsonProperty("Минимальное количество")]
                    public int MinAmount;
                    [JsonProperty("Максимальное количество")]
                    public int MaxAmount;
                }
            }

            internal class InterfaceSetting
            {
                [JsonProperty("AnchorMin всей панели")]
                public string AnchorMin;
                [JsonProperty("AnchorMax всей панели")]
                public string AnchorMax;
                [JsonProperty("AnchorMin кнопки для участия")]
                public string AnchorMinVote;
                [JsonProperty("AnchorMax кнопки для участия")]
                public string AnchorMaxVote;
                [JsonProperty("Основной цвет")]
                public string MainColor;
                [JsonProperty("Дополнительный цвет")]
                public string TwoMainColor;
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    TimeToStartEvent = 1600,
                    TimerVotesWait = 20,
                    MinimumOnline = 5,
                    PrefixForChat = "<color=#007FFF><b>[МЕРОПРИЯТИЕ]</b></color>",
                    InterfaceSettings = new InterfaceSetting
                    {
                        AnchorMin = "0.003645837 0.6509259",
                        AnchorMax = "0.1661458 0.9916667",
                        AnchorMinVote = "0.3078125 0.112963",
                        AnchorMaxVote = "0.6765625 0.162963",
                        MainColor = "#6B803EFF",
                        TwoMainColor = "#566B2BFF",
                    },
                    EventList = new List<Events>
                    {
                       new Events
                       {
                           EventTypes = EventType.Gather,
                           DisplayName = "<b>Каменьщик</b>",
                           Description = "<b><size=12>Добудьте КАМНЯ больше всех и получите приз</size></b>",
                           TimerEvent = 600,
                           Shortname = "stones",
                           SkinID = 0,
                           Rewards = new List<Events.Reward>
                           {
                               new Events.Reward
                               {
                                   Shortname = "wrappingpaper",
                                   Command = "",
                                   SkinID = 0,
                                   MinAmount = 1,
                                   MaxAmount = 5
                               }
                           }
                       },
                       new Events
                       {
                           EventTypes = EventType.Kills,
                           DisplayName = "<b>Убийца животных</b>",
                           Description = "<b><size=12>Убейте КАБАНОВ больше всех и получите приз</size></b>",
                           TimerEvent = 600,
                           Shortname = "boar",
                           SkinID = 0,
                           Rewards = new List<Events.Reward>
                           {
                               new Events.Reward
                               {
                                   Shortname = "wrappingpaper",
                                   Command = "",
                                   SkinID = 0,
                                   MinAmount = 1,
                                   MaxAmount = 5
                               },
                               new Events.Reward
                               {
                                   Shortname = "piano",
                                   Command = "say %STEAMID%",
                                   SkinID = 0,
                                   MinAmount = 1,
                                   MaxAmount = 5
                               },
                           }
                       },
                       new Events
                       {
                           EventTypes = EventType.PickUp,
                           DisplayName = "<b>Грибник</b>",
                           Description = "<b><size=12>Найдите грибов больше всех и получите приз</size></b>",
                           TimerEvent = 600,
                           Shortname = "mushroom",
                           SkinID = 0,
                           Rewards = new List<Events.Reward>
                           {
                               new Events.Reward
                               {
                                   Shortname = "wrappingpaper",
                                   Command = "",
                                   SkinID = 0,
                                   MinAmount = 1,
                                   MaxAmount = 5
                               },
                               new Events.Reward
                               {
                                   Shortname = "piano",
                                   Command = "say %STEAMID%",
                                   SkinID = 0,
                                   MinAmount = 1,
                                   MaxAmount = 5
                               },
                           }
                       },
                       new Events
                       {
                           EventTypes = EventType.Search,
                           DisplayName = "<b>Искатель</b>",
                           Description = "<b><size=12>Найдите скрапа больше всех и получите приз</size></b>",
                           TimerEvent = 100,
                           Shortname = "scrap",
                           SkinID = 0,
                           Rewards = new List<Events.Reward>
                           {
                               new Events.Reward
                               {
                                   Shortname = "wrappingpaper",
                                   Command = "",
                                   SkinID = 0,
                                   MinAmount = 1,
                                   MaxAmount = 5
                               },
                               new Events.Reward
                               {
                                   Shortname = "piano",
                                   Command = "say %STEAMID%",
                                   SkinID = 0,
                                   MinAmount = 1,
                                   MaxAmount = 5
                               },
                           }
                       },
                    }
                };
            }
        }

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
                PrintWarning("Ошибка #132167" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region UI

        public static string QIEVENT_PARENT = "IQEVENT_PARENTS";
        public static string QIEVENT_VOTE_PARENT = "IQEVENTVOTE_PARENTS";

        public void UIEventVotes(int IndexEvent)
        {
            if (EventStatus) return;
            EventPlayerList.Clear();

            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                var player = BasePlayer.activePlayerList[i];

                CuiElementContainer container = new CuiElementContainer();
                CuiHelper.DestroyUi(player, QIEVENT_VOTE_PARENT);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = config.InterfaceSettings.AnchorMinVote, AnchorMax = config.InterfaceSettings.AnchorMaxVote },
                    Button = { Close = QIEVENT_VOTE_PARENT, Command = "iqe vote", Color = HexToRustFormat(config.InterfaceSettings.MainColor), Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { FadeIn = 0.8f, Text = lang.GetMessage("EVENT_VOTES_BTN", this, player.UserIDString), Align = TextAnchor.MiddleCenter }
                }, "Overlay", QIEVENT_VOTE_PARENT);

                CuiHelper.AddUi(player, container);
            };

            timer.Once(config.TimerVotesWait, () =>
            {
                foreach (var MembersEvent in EventPlayerList)
                    UIEvent(MembersEvent.Key, IndexEvent);

                for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    var player = BasePlayer.activePlayerList[i];
                    CuiHelper.DestroyUi(player, QIEVENT_VOTE_PARENT);
                }
            });
        }

        public void UIEvent(BasePlayer player, int IndexEvent)
        {
            var Event = config.EventList[IndexEvent];
            var Interface = config.InterfaceSettings;
            EventLocal = Event.EventTypes; 
            EventStatus = true;

            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, QIEVENT_PARENT);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = Interface.AnchorMin, AnchorMax = Interface.AnchorMax },
                Image = { FadeIn = 0.15f, Color = "0 0 0 0" }
            }, "Overlay", QIEVENT_PARENT);

            #region TitlePanel

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.8940217", AnchorMax = "1 1" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(Interface.MainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            },  QIEVENT_PARENT, "TITLE_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.03205127 0", AnchorMax = "0.7211539 1" },
                Text = { Text = Event.DisplayName, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft, FadeIn = 0.3f }
            }, "TITLE_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.775641 0", AnchorMax = "1 0.9074167" },
                Text = { Text = $"<b>{FormatTime(TimeSpan.FromSeconds(Event.TimerEvent))}</b>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TITLE_PANEL", "TIMER");

            #endregion

            #region MainPanel

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.8858696" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(Interface.MainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, QIEVENT_PARENT, "MAIN_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9049079", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("LIST_MEMBERS",this,player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "MAIN_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8527607", AnchorMax = "1 0.9233128" },
                Text = { Text = lang.GetMessage("ONE_DESCTIPTION", this,player.UserIDString), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "MAIN_PANEL");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.1441717" },
                Text = { Text = Event.Description, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "MAIN_PANEL");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.02 0.1503068", AnchorMax = "0.98 0.8036808" },
                Image = { FadeIn = 0.15f, Color = HexToRustFormat(config.InterfaceSettings.TwoMainColor), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "MAIN_PANEL", "PLAYERS_PANEL");

            #endregion

            CuiHelper.AddUi(player, container);

            timer.Once(1, () => { RefreshTimer(player, (float)Event.TimerEvent, IndexEvent); });
        }

        public void RefreshTimer(BasePlayer player, float Timer,int IndexEvent)
        {
            CuiHelper.DestroyUi(player, "TIMER");
            if (!EventStatus) return;
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.775641 0", AnchorMax = "1 0.9074075" },
                Text = { Text = $"<b>{FormatTime(TimeSpan.FromSeconds(Timer))}</b>", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
            }, "TITLE_PANEL", "TIMER");

            var TopEvent = EventPlayerList.OrderByDescending(x => x.Value).Take(8).ToDictionary(x => x.Key, x => x.Value);

            for(int i = 0; i < TopEvent.Count; i++)
            {
                CuiHelper.DestroyUi(player, $"PLAYER_COUNT_{i}");
                var ElementTop = TopEvent.ElementAt(i);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = $"0 {0.8779345 - (i * 0.13)}", AnchorMax = $"1 {1 - (i * 0.13)}" },
                    Text = { Text = $"{ElementTop.Key.displayName} : {ElementTop.Value}шт", Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter, FadeIn = 0.3f }
                }, "PLAYERS_PANEL", $"PLAYER_COUNT_{i}");
            }

            CuiHelper.AddUi(player, container);

            if(Timer <= 0)
            {
                EventStatus = false;
                GiveReward(IndexEvent);
                foreach (var Eventers in EventPlayerList)
                    CuiHelper.DestroyUi(Eventers.Key, QIEVENT_PARENT);
                return;
            }
            Timer--;
            timer.Once(1, () => { RefreshTimer(player, (float)Timer, IndexEvent); });
        }

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LIST_MEMBERS"] = "<size=14><b>Entry</b></size>",
                ["ONE_DESCTIPTION"] = "<size=10><b>Outrun everyone and hold first place</b></size>",
                ["NON_ADMIN_CHAT_COMMAND"] = "You are not an Administrator",
                ["NON_CORRECT_CHAT_COMMAND"] = "You are using the command incorrectly",
                ["EVENT_VOTES_BTN"] = "<size=14><b>The event is about to begin!\n To take part, click on this button</b></size>",
                ["EVENT_VOTES_BTN_ACCESS"] = "You have successfully registered for participation",
                ["EVENT_WINNER_ALERT"] = "You received a reward for winning the event, congratulations!",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LIST_MEMBERS"] = "<size=14><b>Список участников</b></size>",
                ["ONE_DESCTIPTION"] = "<size=10><b>Обгони всех и удержи первое место</b></size>",
                ["NON_ADMIN_CHAT_COMMAND"] = "Вы не являетесь Администратором",
                ["NON_CORRECT_CHAT_COMMAND"] = "Вы некорректно используете команду",
                ["EVENT_VOTES_BTN"] = "<size=14><b>Achtung!!! Сейчас начнется Ивент!\nЧтобы принять участие,нажмите на эту кнопку</b></size>",
                ["EVENT_VOTES_BTN_ACCESS"] = "Вы успешно зарегистрировались на участие",
                ["EVENT_WINNER_ALERT"] = "Вы получили награду за победу в ивенте,поздравляем!",

            }, this, "ru");
            PrintWarning("Языковой файл загружен успешно");
        }
        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            EventAutoStart();
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!EventStatus) return;
            if (dispenser == null || entity == null || item == null) return;
            if (EventLocal != EventType.Gather) return;
            if (item.info.shortname != config.EventList[LocalIndexEvent].Shortname) return;
            BasePlayer player = (BasePlayer)entity;
            WriteResult(player, item.amount);
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (!EventStatus) return;
            if (dispenser == null || player == null || item == null) return;
            if (EventLocal != EventType.Gather) return;
            if (item.info.shortname != config.EventList[LocalIndexEvent].Shortname) return;
            WriteResult(player, item.amount);
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!EventStatus) return;
            if (info?.InitiatorPlayer == null || info == null || entity == null) return;
            if (EventLocal != EventType.Kills) return;
            if (entity.ShortPrefabName != config.EventList[LocalIndexEvent].Shortname) return;
            var player = info?.InitiatorPlayer;
            WriteResult(player, 1);
        }

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (!EventStatus) return;
            if (item == null || player == null) return;
            if (EventLocal != EventType.PickUp) return;
            if (item.info.shortname != config.EventList[LocalIndexEvent].Shortname) return;
            WriteResult(player, item.amount);
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!EventStatus) return;
            if (entity == null || player == null) return;
            if (entity.GetComponent<StorageContainer>() == null) return;
            if (entity.OwnerID >= 7656000000) return;
            if (EventLocal != EventType.Search) return;

            foreach (var content in entity.GetComponent<StorageContainer>().inventory.itemList)
            {
                if (content.info.shortname == config.EventList[LocalIndexEvent].Shortname)
                    WriteResult(player, content.amount);
            }
        }
        #endregion

        #region Metods

        void EventAutoStart()
        {
            timer.Every(config.TimeToStartEvent, () =>
             {
                 if (EventStatus)
                 {
                     PrintError("Слишком маленькое время для автоматического старта! Прошлый ивент не успел закончится!\nНовый ивент перенесен!");
                     return;
                 }
                 if(BasePlayer.activePlayerList.Count < config.MinimumOnline)
                 {
                     PrintWarning("Автоматический запуск ивента отменен! Недостаточно игроков на сервере!\nНовый ивент перенесен!");
                     return;
                 }
                 LocalIndexEvent = GetRandomEvent();
                 UIEventVotes(LocalIndexEvent);
             });
        }

        public int GetRandomEvent()
        {
            int IndexEvent = UnityEngine.Random.Range(0, config.EventList.Count());
            return IndexEvent;
        }

        public void WriteResult(BasePlayer player,int Amount)
        {
            if (!EventPlayerList.ContainsKey(player)) return;
            EventPlayerList[player] += Amount;
        }

        public void GiveReward(int IndexEvent)
        {
            var Winner = EventPlayerList.OrderByDescending(x => x.Value).Take(1).ToDictionary(x => x.Key, x => x.Value);
            var Rewards = config.EventList[IndexEvent].Rewards;
            foreach (var player in Winner)
            {
                for (int i = 0; i < Rewards.Count; i++)
                {
                    if (String.IsNullOrEmpty(Rewards[i].Command))
                    {
                        var RandomAmount = UnityEngine.Random.Range(Rewards[i].MinAmount, Rewards[i].MaxAmount);
                        Item item = ItemManager.CreateByName(Rewards[i].Shortname, RandomAmount, Rewards[i].SkinID);
                        player.Key.GiveItem(item);
                    }
                    else Server.Command(Rewards[i].Command.Replace("%STEAMID%", player.Key.UserIDString));
                }
                SendChat(lang.GetMessage("EVENT_WINNER_ALERT", this, player.Key.UserIDString), player.Key);
            }
        }
        #endregion

        #region Command

        [ChatCommand("iqe")]
        void IQEventCommand(BasePlayer player, string cmd, string[] arg)
        {
            if (!player.IsAdmin)
            {
                SendChat(lang.GetMessage("NON_ADMIN_CHAT_COMMAND", this, player.UserIDString), player);
                return;
            }
            if (arg == null || arg.Length < 1 || arg[0].Length < 0)
            {
                SendChat(lang.GetMessage("NON_CORRECT_CHAT_COMMAND", this, player.UserIDString), player);
                return;
            }
            switch(arg[0].ToLower())
            {
                case "start":
                    {
                        LocalIndexEvent = GetRandomEvent();
                        UIEventVotes(LocalIndexEvent);
                        SendChat("Вы успешно запустили ивент вручную", player);
                        break;
                    }
                case "stop":
                    {
                        for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
                        {
                            var p = BasePlayer.activePlayerList[i];
                            CuiHelper.DestroyUi(p, QIEVENT_PARENT);
                            CuiHelper.DestroyUi(p, QIEVENT_VOTE_PARENT);
                        }

                        EventStatus = false;
                        SendChat("Вы успешно остановили ивент вручную", player);
                        break;
                    }
            }
        }

        [ConsoleCommand("iqe")]
        void IQECommandConsole(ConsoleSystem.Arg arg)
        {
            switch(arg.Args[0])
            {
                case "vote":
                    {
                        BasePlayer player = arg.Player();
                        EventPlayerList.Add(player, 0);
                        SendChat(lang.GetMessage("EVENT_VOTES_BTN_ACCESS", this, player.UserIDString), player);
                        break;
                    }
            }
        }

        #endregion

        #region Helps
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

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "д", "д", "д")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "ч", "ч", "ч")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "м", "м", "м")} ";

            if (time.Seconds != 0)
                result += $"{Format(time.Seconds, "с", "с", "с")} ";

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

        public void SendChat(string Message, BasePlayer player, Chat.ChatChannel channel = Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message, config.PrefixForChat);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion
    }
}
