using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("TPLotterySystem", "Sempai#3239", "5.0.0")]
    class TPLotterySystem : RustPlugin
    {
        #region Вар
        private string Layer = "MainStats" + ".Main";
        private string Inventory = "INVENTORY_UI";

        [PluginReference] Plugin ImageLibrary;
        private Hash<ulong, PlayersSettings> Settings = new Hash<ulong, PlayersSettings>();
        #endregion

        #region Класс
        public class LotterySettings
        {
            [JsonProperty("ID предмета")] public string ID;
            [JsonProperty("Название предмета")] public string DisplayName;
            [JsonProperty("Короткое название предмета")] public string ShortName;
            [JsonProperty("SkinID предмета")] public ulong SkinID;
            [JsonProperty("Дополнительная команда")] public string Command;
            [JsonProperty("Сколько нужно одинаковых предметов, чтобы забрать из инвентаря?")] public int Amount;
            [JsonProperty("Количество")] public int Count;
            [JsonProperty("Изображение")] public string Url;
        }

        public class PlayersSettings
        {
            [JsonProperty("Сколько игрок открыл ячеек")] public int Count;
            [JsonProperty("Откат")] public double Time;
            [JsonProperty("Список предметов")] public Dictionary<string, InventorySettings> Inventory = new Dictionary<string, InventorySettings>();
        }

        public class InventorySettings
        {
            [JsonProperty("ID предмета")] public string ID;
            [JsonProperty("Название предмета")] public string DisplayName;
            [JsonProperty("Короткое название предмета")] public string ShortName;
            [JsonProperty("SkinID предмета")] public ulong SkinID;
            [JsonProperty("Дополнительная команда")] public string Command;
            [JsonProperty("Собранно одинаковых предметов")] public int Amount;
            [JsonProperty("Количество")] public int Count;
            [JsonProperty("Изображение")] public string Url;
        }
        #endregion

        #region Конфиг
        public Configuration config;
        public class Configuration
        {
            [JsonProperty("Название")] public string Name = "<b><size=30>ЕЖЕДНЕВНАЯ ЛОТЕРЕЯ</size></b>\nКаждые 24 часа у вас есть возможность забрать три вещи!";
            [JsonProperty("Откат на открытие ячеек(в секундах)")] public int Time = 1;
            [JsonProperty("Список призов")] public List<LotterySettings> Settings;
            public static Configuration GetNewConfig()
            {
                return new Configuration
                {
                    Settings  = new List<LotterySettings>
                    {
                        new LotterySettings
                        {
                            ID = "1",
                            DisplayName = "Дерево",
                            ShortName = "wood",
                            SkinID = 0,
                            Command = null,
                            Amount = 1,
                            Count = 1000,
                            Url = null
                        },  
                        new LotterySettings
                        {
                            ID = "2",
                            DisplayName = "Вип",
                            ShortName = null,
                            SkinID = 0,
                            Command = "123",
                            Amount = 50,
                            Count = 1,
                            Url = ""
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
                if (config?.Settings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Хуки
        private void OnServerInitialized()
        {
            PrintWarning("\n-----------------------------\n " +" Author - Sempai#3239\n " +" VK - https://vk.com/rustnastroika\n " +" Forum - https://topplugin.ru\n " +" Discord - https://discord.gg/5DPTsRmd3G\n" +"-----------------------------"); 
            foreach (var check in config.Settings)
            {
                ImageLibrary.Call("AddImage", check.Url, check.Url);
            }

            ImageLibrary.Call("AddImage", $"https://rustage.su/img/server/ui/lottery_bg.png", "d118a4862771842a");

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            var Data = Interface.Oxide.DataFileSystem.ReadObject<PlayersSettings>($"TPLotterySystem/{player.userID}");

            if (!Settings.ContainsKey(player.userID))
                Settings.Add(player.userID, new PlayersSettings());

            Settings[player.userID] = Data ?? new PlayersSettings();
        }

        private void SaveData(BasePlayer player) => SaveData(player.userID);
        private void SaveData(ulong userID)
        {
            Interface.Oxide.DataFileSystem.WriteObject($"TPLotterySystem/{userID}", Settings[userID]);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SaveData(player);
        }

        private InventorySettings GetItem(ulong userID, string name)
        {
            if (!Settings.ContainsKey(userID))
                Settings[userID].Inventory = new Dictionary<string, InventorySettings>();

            if (!Settings[userID].Inventory.ContainsKey(name))
                Settings[userID].Inventory[name] = new InventorySettings();

            return Settings[userID].Inventory[name];
        }

        private void AddItem(BasePlayer player, LotterySettings settings)
        {
            var data = GetItem(player.userID, settings.ID);
            data.ID = settings.ID;
            data.DisplayName = settings.DisplayName;
            data.ShortName = settings.ShortName;
            data.SkinID = settings.SkinID;
            data.Command = settings.Command;
            data.Amount += 1;
            data.Count = settings.Count;
            data.Url = settings.Url;
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
                CuiHelper.DestroyUi(player, Inventory);
                SaveData(player);
            }
        }
        #endregion

        #region Команды
        [ConsoleCommand("lottery")]
        private void ConsoleLottery(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "random")
                {
                    var time = CurrentTime();
                    if (Settings[player.userID].Time < time)
                    {
                        Settings[player.userID].Count += 1;
                        if (Settings[player.userID].Count <= 3)
                        {
                            var random = config.Settings.ToList().GetRandom();
                            PrizUI(player, args.Args[1], random);
                            AddItem(player, random);
                        }

                        if (Settings[player.userID].Count == 3)
                        {
                            Settings[player.userID].Time = time + config.Time;
                            Settings[player.userID].Count = 0;

                            CuiHelper.DestroyUi(player, "Time");
                            var container = new CuiElementContainer();

                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0.44 0.19", AnchorMax = "0.56 0.22", OffsetMax = "0 0" },
                                Button = { Color = "0 0 0 0" },
                                Text = { Text = $"ПОДОЖДИТЕ {FormatShortTime(TimeSpan.FromSeconds(Settings[player.userID].Time - time))}", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                            }, Layer, "Time");

                            CuiHelper.AddUi(player, container);
                        }
                    }
                }
                if (args.Args[0] == "take")
                {
                    var item = GetItem(player.userID, Settings[player.userID].Inventory.ElementAt(int.Parse(args.Args[1])).Key);
                    var check = config.Settings.FirstOrDefault(p => p.ID == item.ID);
                    if (item.Amount >= check.Amount)
                    {
                        if (!string.IsNullOrEmpty(item.Command))
                        {
                            Server.Command(item.Command.Replace("%STEAMID%", player.UserIDString));
                            SendReply(player, $"Вы получили услугу <color=#ee3e61>{item.DisplayName}</color>");
                        }
                        if (!string.IsNullOrEmpty(item.ShortName))
                        {
                            var items = ItemManager.CreateByName(item.ShortName, check.Count);
                            items.skin = item.SkinID;
                            player.inventory.GiveItem(items);
                            SendReply(player, $"Вы получили <color=#ee3e61>{item.DisplayName}</color>\nВ размере <color=#ee3e61>{check.Count}</color>");
                        }
                        item.Amount -= check.Amount;
                        if (item.Amount == 0)
                        {
                            Settings[player.userID].Inventory.Remove(check.ID);
                        }
                        InventoryUI(player);
                    }
                }
                if (args.Args[0] == "ui")
                {
                    LotteryUI(player, 0);
                }
                if (args.Args[0] == "inventory")
                {
                    InventoryUI(player);
                }
                if (args.Args[0] == "page")
                {
                    LotteryUI(player, int.Parse(args.Args[1]));
                }
            }
        }
        #endregion 

        #region Интерфейс
        private void LotteryUI(BasePlayer player, int page = 0)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = Layer,
                Parent = ".Mains",
                Components = 
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "d118a4862771842a"), Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "-0.315 -0.27", AnchorMax = "1.3 1.275", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8 0.804", AnchorMax = "0.817 0.832" },
                Button = { Close = "Menu_UI", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.24", AnchorMax = "0.502 0.72", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.3" },
                Text = { Text = "", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.23 0.73", AnchorMax = "0.44 0.78", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "Ежедневная <b>ЛОТЕРЕЯ</b> Каждые n времени у\nвас есть возможность забрать три вещи!", Color = "1 1 1 0.5", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.57 0.73", AnchorMax = "0.76 0.78", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "СПИСОК ВОЗМОЖНЫХ ПРИЗОВ!", Color = "1 1 1 0.5", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.505 0.19", AnchorMax = "0.76 0.715", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
            }, Layer, "Items");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.676 0.24", AnchorMax = "0.757 0.28" },
                Button = { Color = "1 1 1 0.1", Command = "lottery inventory", Close = Layer },
                Text = { Text = "ИНВЕНТАРЬ", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.23 0.19", AnchorMax = "0.497 0.72", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer, "Priz");

            if (Settings[player.userID].Time >= CurrentTime())
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.44 0.19", AnchorMax = "0.56 0.22", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = $"ПОДОЖДИТЕ {FormatShortTime(TimeSpan.FromSeconds(Settings[player.userID].Time - CurrentTime()))}", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, Layer, "Time");
            }

            float gap = 0f, width = 0.332f, height = 0.3f, startxBox = 0.003f, startyBox = 1f - height, xmin = startxBox, ymin = startyBox;
            for (int z = 0; z < 9; z++)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "3 3", OffsetMax = "-3 -3" },
                    Button = { Color = "1 1 1 0.1" },
                    Text = { Text = $"" }
                }, "Priz", $"Button.{z}");
                xmin += width + gap;
                if (xmin + width >= 1)
                {
                    xmin = startxBox;
                    ymin -= height + gap;
                }

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"lottery random {z}" },
                    Text = { Text = $"✔", Color = "1 1 1 0.3", Align = TextAnchor.MiddleCenter, FontSize = 50, Font = "robotocondensed-bold.ttf" }
                }, $"Button.{z}", $"Gal.{z}");
            }

            float gap1 = 0f, width1 = 0.332f, height1 = 0.27f, startxBox1 = 0f, startyBox1 = 1f - height1, xmin1 = startxBox1, ymin1 = startyBox1;
            foreach (var check in config.Settings.Skip(page * 9).Take(9))
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin1} {ymin1}", AnchorMax = $"{xmin1 + width1} {ymin1 + height1 * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Button = { Color = "1 1 1 0.1" },
                    Text = { Text = $"", Color = "1 1 1 0.3", Align = TextAnchor.MiddleCenter, FontSize = 50, Font = "robotocondensed-bold.ttf" }
                }, "Items", "Image");
                xmin1 += width1 + gap1;
                if (xmin1 + width1 >= 1)
                {
                    xmin1 = startxBox1;
                    ymin1 -= height1 + gap1;
                }

                var image = check.Url != null ? check.Url : check.ShortName;
                container.Add(new CuiElement
                {
                    Parent = "Image",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", image), FadeIn = 0.5f },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = $"{check.Count}x ", Color = "1 1 1 0.3", Align = TextAnchor.LowerRight, FontSize = 16, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
                }, "Image");
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.507 0.24", AnchorMax = "0.587 0.28" },
                Button = { Color = "1 1 1 0.1", Command = page >= 1 ? $"lottery page {page - 1}" : "", FadeIn = 0.5f},
                Text = { Text = $"-", Color = "1 1 1 0.65", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" }
            }, Layer, $"Items.page");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5905 0.24", AnchorMax = "0.6725 0.28" },
                Button = { Color = "1 1 1 0.1", Command = config.Settings.Count() > (page + 1) * 9 ? $"lottery page {page + 1}" : "", FadeIn = 0.5f},
                Text = { Text = $"+", Color = "1 1 1 0.65", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf" }
            }, Layer, $"Items.page");

            CuiHelper.AddUi(player, container);
        }

        private void PrizUI(BasePlayer player, string z, LotterySettings settings)
        {
            CuiHelper.DestroyUi(player, $"Gal.{z}");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0.46 0.58 0.44 0.3" },
                Text = { Text = "" }
            }, $"Button.{z}", "Layers");

            var image = settings.Url != null ? settings.Url : settings.ShortName;
            container.Add(new CuiElement
            {
                Parent = "Layers",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", image), FadeIn = 2f},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        private void InventoryUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Inventory);
            CuiElementContainer container = new CuiElementContainer();
            int ItemCount = Settings[player.userID].Inventory.Count(), CountItem = 0, Count = 5;
            float Position = 0.5f, Width = 0.08f, Height = 0.13f, Margin = 0.005f, MinHeight = 0.5f;

            if (ItemCount >= Count) Position = 0.5f - Count / 2f * Width - (Count - 1) / 2f * Margin;
            else Position = 0.5f - ItemCount / 2f * Width - (ItemCount - 1) / 2f * Margin;
            ItemCount -= Count;

            container.Add(new CuiElement
            {
                Name = Inventory,
                Parent = ".Mains",
                Components = 
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "d118a4862771842a"), Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "-0.315 -0.27", AnchorMax = "1.3 1.275", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8 0.804", AnchorMax = "0.817 0.832" },
                Button = { Close = "Menu_UI", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Inventory);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.44 0.65", AnchorMax = "0.56 0.7", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0.1", Command = "lottery ui", Close = Inventory },
                Text = { Text = "ОТКРЫТЬ ЛОТЕРЕЮ", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
            }, Inventory);

            var list = Settings[player.userID].Inventory;
            for (int z = 0; z < list.Count(); z++)
            {
                var data = GetItem(player.userID, list.ElementAt(z).Key);
                var check = config.Settings.FirstOrDefault(x => x.ID == list.ElementAt(z).Key);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{Position} {MinHeight}", AnchorMax = $"{Position + Width} {MinHeight + Height}", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.1" },
                    Text = { Text = "" }
                }, Inventory, $"{z}");

                var image = data.Url != null ? data.Url : data.ShortName;
                container.Add(new CuiElement
                {
                    Parent = $"{z}",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", image) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = $"{data.Amount}/{check.Amount} ", Color = "1 1 1 0.5", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.UpperRight }
                }, $"{z}");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"lottery take {z}" },
                    Text = { Text = $"X{data.Count} ", Color = "1 1 1 0.5", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.LowerRight }
                }, $"{z}");

                CountItem += 1;
                if (CountItem % Count == 0)
                {
                    if (ItemCount > Count)
                    {
                        Position = 0.5f - Count / 2f * Width - (Count - 1) / 2f * Margin;
                        ItemCount -= Count;
                    }
                    else
                    {
                        Position = 0.5f - ItemCount / 2f * Width - (ItemCount - 1) / 2f * Margin;
                    }
                    MinHeight -= ((Margin * 2) + Height);
                }
                else
                {
                    Position += (Width + Margin);
                }
            }

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Хелпер
        static double CurrentTime() => new TimeSpan(DateTime.UtcNow.Ticks).TotalSeconds;
        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            result += $"{time.Hours.ToString("00")}:";
            result += $"{time.Minutes.ToString("00")}";
            return result;
        }
        #endregion
    }
}