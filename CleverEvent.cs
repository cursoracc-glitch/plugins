using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Apex;
using ConVar;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CleverEvent", "Hougan, Ryamkk", "1.0.4")]
    public class CleverEvent : RustPlugin
    {
		[PluginReference] private Plugin StoreHandler;
		[PluginReference] private Plugin ImageLibrary;
		
		public ulong senderID = 76561198236355670;
		
		public string header = "Clever Event";
		public string prefixcolor = "#81B67A";
		public string prefixsize = "16";
		
		public int QuestionAmount = 12;
        public int TimeToAnswer = 12;
        public int AnnounceTime = 300;
        public int FinishPrize = 100;
        public int MinimalPlayers = 15;
		
		private void LoadDefaultConfig()
        {
            GetConfig("Настройки сообщений", "Иконка для сообщений в чате (необходимо указать id профиля steam)", ref senderID);
            GetConfig("Настройки сообщений", "Названия префикса", ref header);
			GetConfig("Настройки сообщений", "Цвет префикса", ref prefixcolor);
			GetConfig("Настройки сообщений", "Размер префиса", ref prefixsize);
			
			GetConfig("Основные настройки", "Стандартное количество вопросов", ref QuestionAmount);
			GetConfig("Основные настройки", "Стандартное время на ответ", ref TimeToAnswer);
			GetConfig("Основные настройки", "Время перед началом игры", ref AnnounceTime);
			GetConfig("Основные настройки", "Стандартный приз", ref FinishPrize);
			GetConfig("Основные настройки", "Минимальное количество игроков", ref MinimalPlayers);
            SaveConfig();
        }
		
        private class Player
        {
            [JsonProperty("Текущий баланс игрока")]
            public int Balance;
        }

		[JsonProperty("Список игроков и их баланса")]
        private Dictionary<ulong, Player> playerBalance = new Dictionary<ulong, Player>();
		[JsonProperty("Список открытых клеверов")]
        private List<ulong> OpenList = new List<ulong>();
        [JsonProperty("Текущая игра")]
        private Game currentGame = null;
		
        private class Game
        {
            [JsonProperty("Время начала игры")] 
			public double StartTimeStamp;
            
            [JsonProperty("Текущий номер вопроса")] 
			public int CurrentQuestionIndex = 0;
            [JsonProperty("Текущий призовой фонд")] 
			public int FinishPrize = 0;
            [JsonProperty("Время на ответ")] 
			public int TimeToAnswer = 0;

            [JsonProperty("Текущий таймер")]
            public int NextTimerAmount = 10;
            
            [JsonProperty("Сфомированный список вопросов")]
            public List<Question> Questions = new List<Question>();
            [JsonProperty("Правильные ответы на вопросы")]
            public Dictionary<int, int> PercentsAnswers = new Dictionary<int, int>
            {
                [0] = 0,
                [1] = 0,
                [2] = 0
            };
            [JsonProperty("Ответы игроков на каждый вопрос")]
            public List<Dictionary<BasePlayer, bool>> PlayerAnswers = new List<Dictionary<BasePlayer, bool>>();

            public Game(double startTime, int finishPrize, int timeToAnswer, List<Question> questionList)
            {
                this.StartTimeStamp = startTime + GetTimeStamp();
                this.CurrentQuestionIndex = 0;
                this.FinishPrize = finishPrize;
                this.TimeToAnswer = timeToAnswer;

                this.Questions = questionList;
                this.PlayerAnswers = new List<Dictionary<BasePlayer, bool>>();
                
                Interface.Oxide.LogWarning("Создана новая игра:", "Clever");
                Interface.Oxide.LogWarning($" - Приз на игру: {finishPrize} руб.", "Clever");
                Interface.Oxide.LogWarning($" - Кол-во вопросов: {questionList.Count} шт.", "Clever");
            }

            public Question CurrentQuestion()
            {
                if (CurrentQuestionIndex == 0)
                    return null;
                return Questions[CurrentQuestionIndex];
            }

            public bool IsLoose(BasePlayer player)
            {
                if (!PlayerAnswers[0].ContainsKey(player))
                    return true;
                if (!PlayerAnswers[CurrentQuestionIndex - 1].ContainsKey(player))
                    return true;
                if (!PlayerAnswers[CurrentQuestionIndex - 1][player])
                    return true;

                return false;
            }

            public double LeftTime() => StartTimeStamp - GetTimeStamp();
            public string LeftTimeString() => FormatTime(TimeSpan.FromSeconds((int) LeftTime()));
            public string LeftTimeShortString() => TimeSpan.FromSeconds((int) LeftTime()).Minutes + ":" + TimeSpan.FromSeconds((int) LeftTime()).Seconds;
        }

		private static DateTime Epoch = new DateTime(1970, 1, 1);
		public static long GetTimeStamp()
		{
			return (long)DateTime.Now.Subtract(Epoch).TotalSeconds;
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
        
		private class Question
        {
            [JsonProperty("Текст вопроса")]
            public string Text;
            [JsonProperty("Варианты ответов")]
            public Dictionary<string, bool> Answers = new Dictionary<string, bool>();
        }
		
        [JsonProperty("Список вопросов составляющих игру")]
        private List<Question> questionList = new List<Question>
        {
            new Question
            {
                Text = "От какого излучения спасает солнечнозащитный крем?",
                Answers = new Dictionary<string, bool>
                {
                    ["Электромагнитное"] = false,
                    ["Ультрафиолетовое"] = true,
                    ["Гамма-излучение"] = false
                }
            },
            new Question
            {
                Text = "Как называются ночи в Санкт-Петербурге, когда светло, как днем?",
                Answers = new Dictionary<string, bool>
                {
                    ["Белые"] = true,
                    ["Дневные"] = false,
                    ["Скипнутые"] = false
                }
            },
            new Question
            {
                Text = "Какая из этих планет наиболее удалена от солнца?",
                Answers = new Dictionary<string, bool>
                {
                    ["Уран"] = false,
                    ["Юпитер"] = false,
                    ["Нептун"] = true
                }
            },
            new Question
            {
                Text = "Что кричат фанаты ф/к Балтика на матчах своей команды?",
                Answers = new Dictionary<string, bool>
                {
                    ["Вперед, Балтийцы!"] = true,
                    ["Боже, Царя храни!"] = false,
                    ["Не бейте, лучше обоссыте!"] = false
                }
            },
            new Question
            {
                Text = "Кто является главными героями мультфильма Мадагаскар?",
                Answers = new Dictionary<string, bool>
                {
                    ["Люди"] = false,
                    ["Животные"] = true,
                    ["Реперы"] = false
                }
            },
            new Question
            {
                Text = "Как с английского языка переводится расшифровка аббервиатуры PC?",
                Answers = new Dictionary<string, bool>
                {
                    ["Постконцептуализм"] = false,
                    ["Персональный копмьютер"] = true,
                    ["Параллельная вселенаная"] = false
                }
            },
            new Question
            {
                Text = "Какой город находится на реке Майн?",
                Answers = new Dictionary<string, bool>
                {
                    ["Франкфурт-на-Одере"] = false,
                    ["Майнкрафт"] = false,
                    ["Франкфурт-на-Майне"] = true
                }
            },
            new Question
            {
                Text = "Кризис рядом с берегами Кубы 1962 года",
                Answers = new Dictionary<string, bool>
                {
                    ["Кубинский"] = false,
                    ["Карибский"] = true,
                    ["Советский"] = false
                }
            },
            new Question
            {
                Text = "Назовите столицу Белиза?",
                Answers = new Dictionary<string, bool>
                {
                    ["Бельмопан"] = true,
                    ["Порт-о-Пренс"] = false,
                    ["Ватикан"] = false
                }
            },
            new Question
            {
                Text = "Как назывался роман о человеке, сшитом из разных частей?",
                Answers = new Dictionary<string, bool>
                {
                    ["Робинзон Крузо"] = false,
                    ["Франкенштейн"] = true,
                    ["Мастер и Маргарита"] = false
                }
            },
            new Question
            {
                Text = "Во что вы сейчас играете?",
                Answers = new Dictionary<string, bool>
                {
                    ["Дота 2"] = false,
                    ["Дота первой версии"] = false,
                    ["Клевер"] = true
                }
            },
            new Question
            {
                Text = "Кто такая золовка?",
                Answers = new Dictionary<string, bool>
                {
                    ["Мышь"] = false,
                    ["Птица"] = false,
                    ["Сестра мужа"] = true
                }
            },
        };
		
        // System
        private string MenuLayer = "UI.Clever.MainMenu";
        private string AcceptMenu = "UI.Clever.Close";
		
		private void OnServerInitialized()
        {
			ImageLibrary.Call("AddImage", "https://i.imgur.com/8XP9Fdb.png", "iPhone_Clever");
			ImageLibrary.Call("AddImage", "https://i.imgur.com/Xdlzzk4.png", "iPhone_Menu");
            
			if (Interface.Oxide.DataFileSystem.ExistsDatafile("Clever/Balance"))
            {
                playerBalance = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Player>>("Clever/Balance");
            }
			
			if (!Interface.Oxide.DataFileSystem.ExistsDatafile("Clever/Question"))
            {
                Interface.Oxide.DataFileSystem.WriteObject("Clever/Question", questionList);
                return;
            }
            else
                questionList = Interface.Oxide.DataFileSystem.ReadObject<List<Question>>("Clever/Question");
            
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (!playerBalance.ContainsKey(player.userID))
                playerBalance.Add(player.userID, new Player { Balance = 0 });
        }

        private string PrepareGame(int questionAmount, int finishPrize)
        {
            Puts("Начинаем подготавливать игру!");
            if (currentGame != null)
                return "Произошла ошибка. Код: №1";

            List<Question> questions = new List<Question>() {new Question()};
            CreateQuestions(questions, questionAmount);
        
            if (questions.Count < questionAmount)
                return $"Мы смогли сгенерировать {questions.Count} вопросов из {questionAmount}";
            
            Puts($"Подготовлено {questions.Count} вопросов!");

            if (BasePlayer.activePlayerList.Count < MinimalPlayers)
                return $"Не достаточно игроков для начала игры!";

            currentGame = new Game(AnnounceTime, finishPrize, TimeToAnswer, questions);
            
            for (int i = 0; i < questionAmount + 1; i++)
                currentGame.PlayerAnswers.Add(new Dictionary<BasePlayer, bool>());
            
            timer.Once((int) (currentGame.LeftTime()), StartGame);
            timer.Repeat(0.99f, AnnounceTime, () =>
            {
                foreach (var check in OpenList)
                {
                    BasePlayer player = BasePlayer.FindByID(check);
                    if (player != null && player.IsConnected)
                        UpdateTimerStart(player);
                }
            });
            foreach (var check in OpenList)
            {
                BasePlayer player = BasePlayer.FindByID(check);
                if (player != null && player.IsConnected)
                    player.SendConsoleCommand("chat.say /clever");
            }
			
            foreach (var check in BasePlayer.activePlayerList)
                ReplyWithHelper(check, "Начинается регистрация на игру \"Клевер\", в ней вы можете получить деньги, отвечая на вопросы.\n  <color=#81B67A>Зарегистрироваться</color>: /clever");
            
            timer.Once(10, AnnounceClever);

            return "";
        }

        private void StartGame()
        {
            foreach (var check in OpenList)
            {
                BasePlayer player = BasePlayer.FindByID(check);
                if (player != null && player.IsConnected)
                    CuiHelper.DestroyUi(player, MenuLayer + ".SpecialText");
            }
            
            foreach (var check in currentGame.PlayerAnswers[0])
            {
                if (!OpenList.Contains(check.Key.userID))
                    OpenList.Add(check.Key.userID);
                
                DrawMenu(check.Key);
            }
            
            PrintWarning("Игра Clever началась!");
            PrintWarning($"Количество игроков: {currentGame.PlayerAnswers[0].Count}");

            SwitchQuestion();
        }

        private void SwitchQuestion()
        {
            currentGame.PercentsAnswers = new Dictionary<int, int>
            {
                [0] = 0,
                [1] = 0,
                [2] = 0
            };
            currentGame.NextTimerAmount = currentGame.TimeToAnswer;
            currentGame.CurrentQuestionIndex++;
            
            if (currentGame.Questions.Count == currentGame.CurrentQuestionIndex)
            {
                int eachPrize = currentGame.FinishPrize / currentGame.PlayerAnswers.Last().Count(p => p.Value);
                foreach (var check in currentGame.PlayerAnswers.Last().Where(p => p.Value))
                {
                    ReplyWithHelper(check.Key, $"Вы, и <color=#81B67A>{currentGame.PlayerAnswers.Last().Count(p => p.Value)}</color> чел. получили по <color=#81B67A>{eachPrize}</color> рублей на баланс!");
                    playerBalance[check.Key.userID].Balance += eachPrize;
                    DrawMenu(check.Key);
                }
                
                foreach (var check in BasePlayer.activePlayerList.Where(p => !currentGame.PlayerAnswers.Last().ContainsKey(p)))
                    ReplyWithHelper(check, $"Игра закончена, победители получили по <color=#81B67A>{eachPrize}</color> рублей!");
                
                currentGame = null;
                return;
            }
            
            PrintWarning($"Задаём игрокам вопрос #{currentGame.CurrentQuestionIndex}");
            
            foreach (var check in currentGame.PlayerAnswers[0])
                DrawQuestion(check.Key);

            timer.Repeat(1, currentGame.TimeToAnswer, () =>
            {
                PrintWarning($"Обновляем таймер до окончания ответа");
                
                foreach (var check in OpenList)
                {
                    BasePlayer player = BasePlayer.FindByID(check);
                    if (player != null && player.IsConnected)
                        UpdatetimerNext(player);
                }
                currentGame.NextTimerAmount--;
            }).Callback();
            
            timer.Once(currentGame.TimeToAnswer, CountResults);
        }

        private void CountResults()
        {
            PrintWarning("Результаты ответов на вопрос:");
            PrintWarning($" - Правильно ответило: {currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex].Count(p => p.Value)}");
            PrintWarning($" - Неправильно ответило: {currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex].Count(p => !p.Value)}");
            PrintWarning($" - Не ответило на вопрос: {currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex - 1].Count(p => p.Value) - currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex].Count()}");

            foreach (var check in currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex - 1])
            {
                if (!currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex].ContainsKey(check.Key) && check.Value)
                {
                    if (check.Key != null && check.Key.IsConnected)
                        ReplyWithHelper(check.Key, "Вы не ответили на вопрос, вы проиграли!");
                }
                else if (currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex].ContainsKey(check.Key))
                {
                    var currentPlayer = currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex][check.Key];
                    string effect = currentPlayer ? "assets/bundled/prefabs/fx/invite_notice.prefab" : "assets/bundled/prefabs/fx/player/beartrap_scream.prefab";
                    Effect.server.Run(effect, check.Key.transform.position);
                    if (check.Key != null && check.Key.IsConnected)
                        ReplyWithHelper(check.Key, (currentPlayer ? "Вы ответили на вопрос правильно!" : "Вы ответили на вопрос не правильно!"));
                }
                if (check.Key != null && check.Key.IsConnected)
                    ShowResult(check.Key);
            }
            
            if (currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex].Count(p => p.Value) == 0)
            {
                foreach (var check in BasePlayer.activePlayerList)
                    ReplyWithHelper(check, "К сожалению все игроки проиграли до окончания игры!");
                currentGame = null;
                return;
            }

            currentGame.NextTimerAmount = 3;
            timer.Repeat(1, 3, () =>
            {  
                foreach (var check in OpenList)
                {
                    BasePlayer player = BasePlayer.FindByID(check);
                    if (player != null && player.IsConnected)
                        UpdatetimerNext(player);
                }
                currentGame.NextTimerAmount--;
            }).Callback();
            timer.Once(3, SwitchQuestion);
        }

        private void AnnounceClever()
        {
            foreach (var check in BasePlayer.activePlayerList)
                ReplyWithHelper(check, $"Продолжается регистрация на игру \"Клевер\", в ней вы можете получить деньги, отвечая на вопросы.\n  <color=#81B67A>Зарегистрироваться</color>: /clever\nОсталось: <color=#81B67A>{currentGame.LeftTimeString()}</color>");

            if (currentGame.LeftTime() > 10)
                timer.Once(10, AnnounceClever);
        }
		
        private void ShowResult(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            for (int i = 0; i < currentGame.CurrentQuestion().Answers.Count; i++)
            {
                string text = "";
                for (int t = 0; t < currentGame.CurrentQuestion().Answers.ElementAt(i).Key.Length + 1; t++)
                    text += "_";
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.1", AnchorMax = "1 1", OffsetMax = "0 0 " },
                    Text = { Text = $"{(currentGame.CurrentQuestion().Answers.ElementAt(i).Value ? "" : text)}", Color = HexToCuiColor("#333333FF"), FontSize = 20, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter}
                }, MenuLayer + $".{i}");

                if (!currentGame.CurrentQuestion().Answers.ElementAt(i).Value)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.3 0.45", AnchorMax = "0.7 0.50", OffsetMax = "0 0" },
                        Button = { Color = HexToCuiColor("#A05D5DFF") },
                        Text = { Text = "" }
                    }, MenuLayer + $".{i}");
                }
                

                string color = currentGame.CurrentQuestion().Answers.ElementAt(i).Value
                    ? HexToCuiColor("#8BAE95FF")
                    : HexToCuiColor("#A85252FF");
                
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.1 0.98", AnchorMax = "0.9 1.02", OffsetMax = "0 0" },
                    Button = { Color = color },
                    Text = { Text = "" }
                }, MenuLayer + $".{i}");
                
                container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.1 -0.01", AnchorMax = "0.9 0.03", OffsetMax = "0 0" },
                        Button = { Color = color },
                        Text = { Text = "" }
                    }, MenuLayer + $".{i}");
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 2.05", OffsetMax = "0 0 " },
                    Text = { Text = $"<b>-------------------------</b>", Color = color, FontSize = 20, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter}
                }, MenuLayer + $".{i}");
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 -0.95", AnchorMax = "1 1", OffsetMax = "0 0 " },
                    Text = { Text = $"<b>-------------------------</b>", Color = color, FontSize = 20, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter}
                }, MenuLayer + $".{i}");
                
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "0.95 1", OffsetMax = "0 0 " },
                    Text = { Text = $"{currentGame.PercentsAnswers[i]}", Color = HexToCuiColor("#333333FF"), FontSize = 20, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight}
                }, MenuLayer + $".{i}");
            }
            CuiHelper.AddUi(player, container);
        }
        
        private void DrawQuestion(BasePlayer player)
        {
            Question currentQuestion = currentGame.CurrentQuestion();
            CuiHelper.DestroyUi(player, MenuLayer + $".CurrentQuestion");
            CuiHelper.DestroyUi(player, MenuLayer + $".QuestionAnswer");
            CuiHelper.DestroyUi(player, MenuLayer + ".SpecialText");
            CuiHelper.DestroyUi(player, MenuLayer + $".QuestionNumber");
            
            for (int i = 0; i < currentQuestion.Answers.Count; i++)
                CuiHelper.DestroyUi(player, MenuLayer + $".{i}");

            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiElement
            {
                Parent = MenuLayer,
                Name = MenuLayer + ".QuestionAnswer",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "iPhone_Menu") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1124998 0.4815856", AnchorMax = "0.8815633 0.5514984", OffsetMax = "0 0" },
                Text = { FadeIn = 1f, Text = "Выберите один из вариантов, предложенных ниже", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#333333FF") }
            }, MenuLayer, MenuLayer + ".HelpText");
            

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.06249967 0.7275281", AnchorMax = "0.4465642 0.7833956", OffsetMax = "0 0" },
                Text = { FadeIn = 1f, Text = $"<color=white>Вопрос {currentGame.CurrentQuestionIndex} из {currentGame.Questions.Count - 1}</color>", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#333333FF") }
            }, MenuLayer, MenuLayer + ".QuestionNumber");

            if (currentGame.IsLoose(player))
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.5399994 0.7275281", AnchorMax = "0.9240639 0.7833956", OffsetMax = "0 0" },
                    Text = { FadeIn = 1f, Text = $"Вы проиграли", Font = "robotocondensed-regular.ttf", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#FFFFFFFF") }
                }, MenuLayer, MenuLayer + ".Loose");
            }
            
            container.Add(new CuiLabel
            {
                FadeOut = 1f,
                RectTransform = { AnchorMin = "0.1124998 0.567727", AnchorMax = "0.8815633 0.7434447", OffsetMax = "0 0" },
                Text = { FadeIn = 1f, Text = currentQuestion.Text, Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#333333FF") }
            }, MenuLayer, MenuLayer + ".CurrentQuestion");
            
            for (int i = 0; i < currentQuestion.Answers.Count; i++)
            {
                container.Add(new CuiLabel
                {
                    FadeOut = 0.1f,
                    RectTransform = { AnchorMin = $"0.1124998 {0.351748 - i * 0.079}", AnchorMax = $"0.8815633 {0.4200998 - i * 0.079}", OffsetMax = "0 0" },
                    Text = { FadeIn = 0.1f, Text = currentQuestion.Answers.ElementAt(i).Key, Font = "robotocondensed-bold.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = HexToCuiColor("#333333FF") }
                }, MenuLayer, MenuLayer + $".{i}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"UI_Clever answer {i}" },
                    Text = { Text = "" }
                }, MenuLayer + $".{i}", MenuLayer + $".{i}.Button");
            }

            CuiHelper.DestroyUi(player, MenuLayer + ".SpecialText");
            CuiHelper.AddUi(player, container);
        }

        private void UpdatetimerNext(BasePlayer player)
        {
            if (!OpenList.Contains(player.userID))
                return;
            
            CuiHelper.DestroyUi(player, MenuLayer + $".SpecialTimer");
            
            CuiElementContainer container = new CuiElementContainer();
            string text = currentGame.NextTimerAmount == 0 ? "ВРЕМЯ ВЫШЛО" : currentGame.NextTimerAmount.ToString();
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.4208799", AnchorMax = "1 0.4817404", OffsetMax = "0 0" },
                Text = { Text = text, Align = TextAnchor.MiddleCenter, FontSize = 28, Font = "robotocondensed-bold.ttf" }
            }, MenuLayer, MenuLayer + ".SpecialTimer");

            Effect.server.Run("assets/bundled/prefabs/fx/notice/item.select.fx.prefab", player.transform.position);
            
            CuiHelper.AddUi(player, container);
        }

        private void UpdateTimerStart(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MenuLayer + $".SpecialText");
            
            CuiElementContainer container = new CuiElementContainer();
            
            string text = "";
            if (currentGame == null || currentGame.LeftTime() < 0)
            {
                text = $"<size=25>ОЖИДАЕМ НАЧАЛА</size>\n<size=75>ИГРЫ</size>";
            }
            else if (currentGame.LeftTime() > 60)
            {
                text = $"<size=80>{currentGame.LeftTimeShortString()}</size>";
            }
            else
            {
                text = $"<size=100>{currentGame.LeftTimeShortString().Split(':')[1]}</size>";
            }
                
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.04 0.0720976", AnchorMax = "0.95 0.9360176", OffsetMax = "0 0" },
                Text = { Text = text, Align = TextAnchor.MiddleCenter }
            }, MenuLayer, MenuLayer + ".SpecialText");

            CuiHelper.AddUi(player, container);
        }

        private void DrawMenu(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MenuLayer);
            CuiElementContainer container = new CuiElementContainer();
            
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.3876952 0.1428239", AnchorMax = "0.3876952 0.1428239", OffsetMin = "0 0", OffsetMax = "280 560" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", MenuLayer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100", OffsetMax = "0 0" },
                Button = { Command = "UI_Clever close", Color = "0 0 0 0"},
                Text = { Text = "" }
            }, MenuLayer);
            
            container.Add(new CuiElement
            {
                Parent = MenuLayer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage",  "iPhone_Clever") },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.08 0.02", AnchorMax = "0.92 0.11", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = "UI_Clever getbalance" },
                Text = { Text = $"БАЛАНС: {playerBalance[player.userID].Balance} РУБ.", Align = TextAnchor.MiddleCenter, FontSize = 28, Font = "robotocondensed-bold.ttf", Color = HexToCuiColor("#9A1111FF") }
            }, MenuLayer);
            
            
            
            CuiHelper.AddUi(player, container);
            
            UpdateTimerStart(player);
        }

        private void CreateQuestions(List<Question> questions, int amount = 12)
        {
            //questionList.Shuffle((uint) Core.Random.Range(0, 1000));
            
            //PrintWarning($"Отобрано {amount} случайных вопросов:");
            for (int i = 0; i < Math.Min(questionList.Count, amount); i++)
            {
               questions.Add(questionList[i]);
                //PrintWarning($"[{i+1}] {question.Text}");
                
                //for (int t = 0; t < question.Answers.Count; t++)
                    //PrintWarning($"{(question.Answers.ElementAt(t).Value ? "+ " : "- ")} {question.Answers.ElementAt(t)}");
            }
        }

        [ChatCommand("clever")]
        private void cmdChatClever(BasePlayer player)
        {
            if (!OpenList.Contains(player.userID))
                OpenList.Add(player.userID);
            if (currentGame != null)
            {
                if (currentGame.LeftTime() < 3)
                {
                    ReplyWithHelper(player, "Вы не можете присоединиться к игре, она уже началась!");
                    return;
                }

                if (!currentGame.PlayerAnswers[0].ContainsKey(player))
                {
                    ReplyWithHelper(player, "Вы успешно зарегистрировались на игру! Закрыть телефон вы можете нажав справа от телефона, а затем на чёлку!");
                    currentGame.PlayerAnswers[0].Add(player, true);
                    PrintWarning($"Игрок {player} зарегистрировался");
                }
            }
            DrawMenu(player);
        }
        
        [ConsoleCommand("UI_Clever")]
        private void cmdConsoleHandler(ConsoleSystem.Arg args)
        {
            if (args.Player() == null)
                return;

            if (!args.HasArgs(1))
                return;

            BasePlayer player = args.Player();

            if (args.Args[0].ToLower() == "close")
            {
                CuiHelper.DestroyUi(player, MenuLayer + ".Close");
                CuiElementContainer container = new CuiElementContainer();
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.2499996 0.9335207", AnchorMax = "0.7540634 0.9694132", OffsetMax = "0 0" },
                    Button = { Close = MenuLayer, Color = "0 0 0 0", Command = "UI_Clever acceptclose" },
                    Text = { Text = "" }
                }, MenuLayer, MenuLayer + ".Close");
                ReplyWithHelper(player, "Для того чтобы закрыть телефон, нажмите на камеру (чёлку) телефона!");
                CuiHelper.AddUi(player, container);
            }

            if (args.Args[0].ToLower() == "getbalance")
            {
                if (playerBalance[player.userID].Balance < 10)
                {
                    ReplyWithHelper(player, "Вывод баланса возможен от <color=#FF5733>10 рублей</color>");
                    return;
                }

                StoreHandler.Call("HasRegistered", player.userID, (Action<bool>) ((b) =>
                {
                    if (!b)
                    {
                        ReplyWithHelper(player, "Для вывода средств авторизуйтесь в магазине!");
                    }
                    else
                    {
                        StoreHandler.Call("AddMoney", player.userID, (float) playerBalance[player.userID].Balance,
                            "Клевер: вывод средств", (Action<bool>) ((a) =>
                            {
                                if (!a)
                                {
                                    ReplyWithHelper(player, "Неизвестная ошибка #3!");
                                }
                                else
                                {
                                    LogToFile("Withdraw", $"Игрок {player.userID} вывел {playerBalance[player.userID].Balance} рублей.", this);
                                    ReplyWithHelper(player, $"Вы успешно вывели: <color=#81B67A>{playerBalance[player.userID].Balance} рублей.</color>");
                                    playerBalance[player.userID].Balance = 0;
                                    DrawMenu(player);
                                }
                            }));
                    }
                }));
            }

            if (args.Args[0].ToLower() == "acceptclose")
            {
                if (OpenList.Contains(player.userID))
                {
                    OpenList.Remove(player.userID);
                }
            }

            if (currentGame == null || currentGame.LeftTime() > 0)
                return;
            
            if (args.Args[0].ToLower() == "answer")
            {
                if (currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex].ContainsKey(player))
                    return;
                
                if (!args.HasArgs(2))
                    return;

                if (!currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex - 1].ContainsKey(player) || (currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex - 1].ContainsKey(player) && !currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex - 1][player]))
                {
                    ReplyWithHelper(player, "Вы не можете отвечать на вопросы, вы <color=#FF5733>проиграли</color>!");
                    return;
                }

                int answer;
                if (!Int32.TryParse(args.Args[1], out answer))
                    return;

                currentGame.PercentsAnswers[answer]++;
                bool isRight = currentGame.CurrentQuestion().Answers.ElementAt(answer).Value;
                currentGame.PlayerAnswers[currentGame.CurrentQuestionIndex].Add(player, isRight);
                PrintWarning($"Получен ответ от {player.userID} - {(isRight ? "ВЕРНО" : "НЕВЕРНО")}");
                
                Effect.server.Run("assets/bundled/prefabs/fx/build/repair_full.prefab", player.transform.position);
                ReplyWithHelper(player, $"Вы успешно ответили на вопрос!\n<color=#81B67A>Ответ</color>: {currentGame.CurrentQuestion().Answers.ElementAt(answer).Key}");
            }
        }

        [ConsoleCommand("clever")]
        private void cmdControlClever(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
                return;

            if (!args.HasArgs(0))
            {
                PrintWarning("Список возможных команд:");
                PrintWarning("clever start 'qAmount' 'pAmount' - запустить игру");
                PrintWarning("clever stop - остановить игру");
                return;
            }

            switch (args.Args[0].ToLower())
            {
                case "start":
                {
                    if (currentGame != null)
                    {
                        PrintWarning("Игра уже запущена, сначала остановите её!");
                        return;
                    }

                    int questionAmount = QuestionAmount;
                    int finishPrize = FinishPrize;

                    if (args.HasArgs(2))
                    {
                        if (!Int32.TryParse(args.Args[1], out questionAmount))
                        {
                            PrintError("Было введено не стандартное число вопросов, однако мы не смогли преобразовать его в число!");
                            return;
                        }
                    }

                    if (args.HasArgs(3))
                    {
                        if (!Int32.TryParse(args.Args[2], out finishPrize))
                        {
                            PrintError("Было введен не стандартный приз, но мы не смогли преобразовать его в число!");
                            return;
                        }
                    }
                    
                    string result = PrepareGame(questionAmount, finishPrize);
                    if (result != "")
                    {
                        PrintWarning(result);
                    }
                    return;
                }
                case "stop":
                {
                    if (currentGame == null)
                    {
                        PrintWarning("Игра уже остановлена, сначала запустите её!");
                        return;
                    }
                    return;
                }
            }
        }
		
		private static string HexToCuiColor(string hex)
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
 
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
		
		public void ReplyWithHelper(BasePlayer player, string message, string[] args = null)
        {
            if (args != null)
	        {
                message = string.Format(message, args);
	        }
			
	        player.SendConsoleCommand("chat.add", senderID, string.Format("<size=prefixsize><color=prefixcolor>{0}</color>:</size>\n{1}", header, message));
        }
		
		private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }
    }
}

