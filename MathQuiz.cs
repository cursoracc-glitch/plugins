using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("MathQuiz", "FREDWAY", "1.0.1")]
    [Description("Mathematical quiz with rewards")]

    class MathQuiz : CovalencePlugin
    {
        private bool QuizInProgress = false;
        private string Task = "";
        private string Answer = "";
        private string Winner = "";
        private string Reward = "";
        private Timer Reminder;

        #region Config Setip
        private string Prefix = "[MathQuiz]";
        private string PrefixColor = "#42dff4";
        private string SteamIDIcon = "";
        //private Dictionary<string, int> Rewards = new Dictionary<string, int>();
        private Dictionary<ItemDefinition, int> Rewards = new Dictionary<ItemDefinition, int>();
        private Dictionary<int, string> Tasks = new Dictionary<int, string>()
        {
            [0] = "{X} * {Y} + {Z}",
            [1] = "{X} * ({Y} - {Z})",
            [2] = "{X} * ({Y} + {Z})",
            [3] = "{X} + {Y} * {Z}",
        };
        private float QuizFreq = 300f;

        private class ConfigLocalization
        {
            public string Prefix { get; set; }
            public string PrefixColor { get; set; }
            public string SteamIDIcon { get; set; }
            public string Rewards { get; set; }
            public string Tasks { get; set; }
            public string QuizFreq { get; set; }
        }
        private static ConfigLocalization ConfigRus = new ConfigLocalization
        {
            Prefix = "Префикс в чате",
            PrefixColor = "Цвет префикса в чате",
            SteamIDIcon = "Steam ID сообщений в чате",
            Rewards = "Возможные награды",
            Tasks = "Варианты задач",
            QuizFreq = "Частота викторин(в секундах)"
        };
        private static ConfigLocalization ConfigEn = new ConfigLocalization
        {
            Prefix = "Chat Prefix",
            PrefixColor = "Color of the Chat Prefix",
            SteamIDIcon = "Plugin Icon SteamID",
            Rewards = "Possible rewards",
            Tasks = "Tasks",
            QuizFreq = "Frequency of the Quiz(in seconds)"

        };
        #endregion
        
        #region Loading config
        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");
        private new void LoadConfig()
        {
            Dictionary<string, object> rewards = new Dictionary<string, object>()
            {
                ["ammo.pistol"] = 100,
                ["ammo.pistol.fire"] = 50,
                ["ammo.pistol.hv"] = 50,
                ["ammo.rifle"] = 100,
                ["ammo.rifle.explosive"] = 50,
                ["ammo.rifle.hv"] = 50,
                ["ammo.rifle.incendiary"] = 50,
                ["rifle.ak"] = 1,
                ["smg.2"] = 1,
                ["smg.thompson"] = 1
            };
            GetConfig(ConfigRus.Prefix, ref Prefix);
            GetConfig(ConfigRus.PrefixColor, ref PrefixColor);
            GetConfig(ConfigRus.SteamIDIcon, ref SteamIDIcon);
            GetConfig(ConfigRus.QuizFreq, ref QuizFreq);
            GetConfig(ConfigRus.Rewards, ref rewards);
            SaveConfig();

            foreach(var item in rewards)
            {
                int count;
                bool done = false;
                if(!int.TryParse(item.Value.ToString(), out count))
                {
                    PrintWarning($"Error while adding item \"{item.Key}\" to the rewards list. Check it's value, must be integer.");
                    continue;
                }
                var itemdefs = ItemManager.GetItemDefinitions();
                foreach(var def in itemdefs)
                {
                    if(def.shortname == item.Key)
                    {
                        Rewards.Add(def, count);
                        done = true;
                        break;
                    }
                    if(def.itemid.ToString() == item.Key)
                    {
                        Rewards.Add(def, count);
                        done = true;
                        break;
                    }
                    if(def.displayName.english == item.Key)
                    {
                        Rewards.Add(def, count);
                        done = true;
                        break;
                    }
                }
                if (!done)
                {
                    PrintWarning($"Error while adding item \"{item.Key}\" to the rewards list. Item not found. Check your config.");
                    continue;
                }
            }

        }
        #endregion

        #region Localization
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Quiz started"] = "The Math Quiz has began! Todays task is {0}\nThe first player who would type the right anwer to the chat will be rewarded!",
                ["Quiz ended"] = "The Quiz has ended. The winner is {0}! His rewards is {1} X{2}"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Quiz started"] = "Математическая виктроина началась! Задание на сегодня: {0}\nПервый игрок, написавий верный ответ в чат получит награду!",
                ["Quiz ended"] = "Викторина окончена. Победитель {0}! В качестве награды он получает {1}"
            }, this,"ru");
        }
        #endregion

        #region Initialization
        void Loaded()
        {
            LoadConfig();
            LoadMessages();
        }
        private void OnServerInitialized()
        {
            timer.Once(QuizFreq, () => { StartQuiz(); });
        }
        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!QuizInProgress) return;
            QuizInformToChat(true);
        }
        #endregion

        #region Quiz
        private void StartQuiz()
        {
            QuizInProgress = true;

            int num = Core.Random.Range(0, 4);
            Task = Tasks[num];
            int X = Core.Random.Range(2, 15);
            int Y = Core.Random.Range(2, 15);
            int Z = Core.Random.Range(2, 15);
            Task = Task.Replace("{X}", X.ToString()).Replace("{Y}", Y.ToString()).Replace("{Z}", Z.ToString());
            Answer = GetAnswer(X, Y, Z, num);
            if (Answer == "")
            {
                PrintWarning($"Something goes wrong! Please contact the developer. And give him this info:\nNum = {num}; X = {X}; Y = {Y}; Z = {Z}");
                StartQuiz();
            }
            QuizInformToChat(true);
            Log($"The quiz has begun! Task: {Task}. Correct answer: {Answer}");
            Reminder = timer.Repeat(120f, 0, () => { QuizInformToChat(true); });

        }
        private string GetAnswer(int X, int Y, int Z, int num)
        {
            switch (num)
            {
                case 0:
                    return $"{X * Y + Z}";
                case 1:
                    return $"{X * (Y - Z)}";
                case 2:
                    return $"{X * (Y + Z)}";
                case 3:
                    return $"{X + Y * Z}";
                default:
                    return "";
            }
        }
        private void EndQuiz(IPlayer winner)
        {
            if (Reminder != null && !Reminder.Destroyed) Reminder.Destroy();
            QuizInProgress = false;
            Winner = winner.Name;
            var reward = Rewards.ElementAt(Core.Random.Range(0, Rewards.Count));
            BasePlayer player = BasePlayer.FindByID(ulong.Parse(winner.Id));
            Reward = $"{reward.Key.displayName.english} x{reward.Value}";
            player.GiveItem(ItemManager.Create(reward.Key, reward.Value), BaseEntity.GiveItemReason.PickedUp);
            QuizInformToChat(false);
            timer.Once(QuizFreq, () => { StartQuiz(); });
            Log($"The quiz is over. Winner is {Winner} and he got {Reward}");

        }
        #endregion

        #region OnUserChat
        void OnUserChat(IPlayer player, string message)
        {
            if (!QuizInProgress) return;
            if (message.Contains(Answer))
            {
                EndQuiz(player);
            }
        }
        #endregion

        #region Helpers
        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }
        private void QuizInformToChat(bool start)
        {
            foreach(IPlayer player in players.Connected)
            {
                string Message;
                if (start)
                {
                    Message = GetMsg("Quiz started", player.Id);
                    Message = string.Format(Message, Task);
                }else
                {
                    Message = GetMsg("Quiz ended", player.Id);
                    Message = string.Format(Message, Winner, Reward);
                }
                player.Command("chat.add", new object[] { SteamIDIcon, "<color=" + PrefixColor + ">" + Prefix + "</color> " + Message });
            }
        }
        
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        #endregion
    }
}