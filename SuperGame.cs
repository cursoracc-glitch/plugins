using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SuperGame", "Chibubrik", "1.3.0")]
    class SuperGame : RustPlugin
    {
        #region Вар
        private string Layer = "Game_UI";

        [PluginReference] private Plugin ImageLibrary;
        public Dictionary<ulong, Gather> gather;
        #endregion

        #region Класс
        public class Settings
        {
            [JsonProperty("Название панельки")] public string Name;
            [JsonProperty("Сбрасывать ли прогресс при достижении 200 баллов?")] public bool Progress;
            [JsonProperty("Информация")] public string Info;
            [JsonProperty("Баллы")] public string Ball;
        }

        public class GameSettings
        {
            [JsonProperty("Название руды")] public string Name;
            [JsonProperty("ShortName руды")] public string ShortName;
            [JsonProperty("Сколько нужно набрать очков")] public int Count;
        }

        public class Gather
        {
            [JsonProperty("Общее кол - во баллов")] public int Amount;
            [JsonProperty("Список добываемых ресурсов")] public Dictionary<string, PlayerGather> Res = new Dictionary<string, PlayerGather>();
        }

        public class PlayerGather
        {
            [JsonProperty("Количество очков у игрока")] public int Count;
        }
        #endregion

        #region Конфиг
        public Configuration config;
        public class Configuration
        {
            [JsonProperty("Настройки")] public Settings settings = new Settings();
            [JsonProperty("Ресурсы")] public List<GameSettings> game;
            [JsonProperty("Список наград (выбирается случайно)")] public Dictionary<string, string> RewardList;
            public static Configuration GetNewCong()
            {
                return new Configuration
                {
                    settings = new Settings
                    {
                        Name = "СУПЕРИГРА",
                        Progress = false,
                        Info = "Суперигра в которой ты можешь выиграть случайную привилегию.\nПри добычи Дерева, Камня, Железной и Серной руды, вы будете получать от 1 до 3 баллов.\nСобрав 200 баллов, вы выполните одно задание. Если число получится более 200, выши баллы сгорают и придется начинать сначала. Набрав 800 баллов, нажмите на кнопку забрать, чтобы получить случайную привилегию: Premium, Wars, Elite, King, Grand на 7 дней.",
                        Ball = "Собери все 800 баллов и получи приз!"
                    },
                    game = new List<GameSettings>
                    {
                        new GameSettings
                        {
                            Name = "Дерево",
                            ShortName = "wood",
                            Count = 200
                        },
                        new GameSettings
                        {
                            Name = "Камень",
                            ShortName = "stones",
                            Count = 200
                        },
                        new GameSettings
                        {
                            Name = "Железная руда",
                            ShortName = "metal.ore",
                            Count = 200
                        },
                        new GameSettings
                        {
                            Name = "Серная руда",
                            ShortName = "sulfur.ore",
                            Count = 200
                        }
                    },
                    RewardList = new Dictionary<string, string>
                    {
                        ["o.grant user %STEAMID% king"] = "Кинг на 7 дней",
                        ["o.grant user %STEAMID% HitAdvance.line"] = "HitAdvance на 7 дней"
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
                if (config?.RewardList == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewCong();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Команды
        [ChatCommand("game")]
        private void cmdGame(BasePlayer player, string command, string[] args)
        {
            GameUI(player);
        }

        [ConsoleCommand("game.priz")]
        private void cmdConsoleGame(ConsoleSystem.Arg args)
        {
            var player = args.Player();
			int sum = config.game.Sum(p => p.Count);
            if (gather[player.userID].Amount >= sum)
            {
                var name = config.RewardList.ToList().GetRandom();
                var item = name.Key.Split('+')[0];
                Server.Command(item.Replace("%STEAMID%", player.UserIDString));
                SendReply(player, $"<color=#7e53d4><size=16>СУПЕРИГРА</size></color>\nВы получили {name.Value}");
                CuiHelper.DestroyUi(player, Layer);
                gather.Clear();
                SaveData();
            }
            else
            {
                SendReply(player, $"<size=16>СУПЕРИГРА</size>\nВы не собрали нужное количество очков!\n У вас {gather[player.userID].Amount} очков");
            }
        }
        #endregion

        #region Хуки
        private void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("SuperGame/Player"))
            {
                gather = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Gather>>("SuperGame/Player");
            }
            else
            {
                gather = new Dictionary<ulong, Gather>();
            }

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }

        private PlayerGather GatherData(ulong userID, string name)
        {
            if (!gather.ContainsKey(userID)){
                gather.Add(userID, new Gather());
				gather[userID].Res = new Dictionary<string, PlayerGather>();
			}

            if (!gather[userID].Res.ContainsKey(name))
                gather[userID].Res[name] = new PlayerGather();

            return gather[userID].Res[name];
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!gather.ContainsKey(player.userID))
            {
                gather.Add(player.userID, new Gather());
            }
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
            SaveData();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SaveData();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("SuperGame/Player", gather);
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
			if (item==null || player==null) return;
            var check = config.game.FirstOrDefault(x => x.ShortName == item.info.shortname);
            if (check == null) return;
            
			if (check.ShortName != item.info.shortname) return;
			
			PlayerGather data = GatherData(player.userID, check.ShortName);
			int amount = UnityEngine.Random.Range(1, 3);
			data.Count += amount;
			gather[player.userID].Amount += amount;			
			if (config.settings.Progress)
			{
				
				if (data.Count > check.Count)
				{
					gather[player.userID].Amount -= data.Count;
					data.Count = 0;
					SendReply(player, "<color=#7e53d4><size=16>СУПЕРИГРА</size></color>\nВы перефармили, <color=#7e53d4>прогресс</color> сброшен!");
				}
			}
			
        }
        #endregion

        #region Интерфейс
        private void GameUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" },
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-2 -2", AnchorMax = "2 2", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.9", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3 0.27", AnchorMax = "0.7 0.73", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1" },
                Text = { Text = "" }
            }, Layer, "Game");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0.86", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1" },
                Text = { Text = config.settings.Name, Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
            }, "Game");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.01 0.15", AnchorMax = "0.99 0.5", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1" },
                Text = { Text = config.settings.Info, Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
            }, "Game");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.14", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1" },
                Text = { Text = config.settings.Ball, Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
            }, "Game", "Give");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8 0.1", AnchorMax = "0.99 0.9", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1", Command = $"game.priz" },
                Text = { Text = $"ЗАБРАТЬ", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
            }, "Give");

            float width = 0.2485f, height = 0.32f, xmin = 0.002f, ymin = 0.84f - height;
            foreach (var check in config.game)
            {
                var data = GatherData(player.userID, check.ShortName);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMin = "4 0", OffsetMax = "-4 0" },
                    Image = { Color = "0 0 0 0" }
                }, "Game", "Items");
                xmin += width;

                container.Add(new CuiElement
                {
                    Parent = "Items",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.ShortName) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.1" },
                    Text = { Text = $"{data.Count / 2}%", Color = "1 1 1 0.6", Align = TextAnchor.MiddleCenter, FontSize = 40, Font = "robotocondensed-bold.ttf" }
                }, "Items");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = $"{check.Name}", Color = "1 1 1 0.5", Align = TextAnchor.LowerCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, "Items");
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}