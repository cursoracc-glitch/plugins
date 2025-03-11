using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PassSystem", "TopPlugin.ru", "3.0.0")]
    class PassSystem : RustPlugin
    {
        #region Вар
        private string Layer = "Pass_UI";

        [PluginReference] Plugin ImageLibrary;
        #endregion

        #region Класс
        public class Settings
        {
            [JsonProperty("Основное название")] public string DisplayName;
            [JsonProperty("Название сервера")] public string ServerName;
            [JsonProperty("Последний уровень")] public int Level;
        }

        public class PassSettings
        {
            [JsonProperty("Уровень")] public int Level;
            [JsonProperty("Сколько максимум игрок может получить вещей из списка?")] public int Count;
            [JsonProperty("Список заданий")] public List<MainSettings> mains;
            [JsonProperty("Список наград")] public List<ItemsList> items;
        }

        public class MainSettings
        {
            [JsonProperty("Название задания")] public string DisplayName;
            [JsonProperty("Короткое название задачи")] public string ShortName;
            [JsonProperty("Количество предмета")] public int Amount;
        }

        public class ItemsList
        {
            [JsonProperty("Короткое название предмета")] public string ShortName;
            [JsonProperty("Шанс выпадения предмета")] public int DropChance;
            [JsonProperty("Минимальное количество при выпадени")] public int AmountMin;
            [JsonProperty("Максимальное Количество при выпадени")] public int AmountMax;
        }

        private Dictionary<ulong, PlayerTasks> ProgressPass;
        private class PlayerTasks
        {
            [JsonProperty("Уровень игрока")] public int Level;
            [JsonProperty("Список выполняемых заданий")] public Dictionary<string, PlayerProgress> Progress = new Dictionary<string, PlayerProgress>();
        }

        private class PlayerProgress
        {
            [JsonProperty("Количество")] public int Amount;
        }
        #endregion

        #region Конфиг
        public Configuration config;
        public class Configuration
        {
            [JsonProperty("Основные настройки")] public Settings settings = new Settings();
            [JsonProperty("Список")] public List<PassSettings> passSettings;
            public static Configuration GetNewConfig()
            {
                return new Configuration
                {
                    settings = new Settings
                    {
                        DisplayName = "SERVERNAME PASS",
                        ServerName = "SERVERNAME",
                        Level = 3
                    },
                    passSettings = new List<PassSettings>
                    {
                        new PassSettings
                        {
                            Level = 1,
                            Count = 3,
                            mains = new List<MainSettings>
                            {
                                new MainSettings
                                {
                                    DisplayName = "Собери ткань",
                                    ShortName = "cloth",
                                    Amount = 500,
                                },       
                                new MainSettings
                                {
                                    DisplayName = "Убить игроков",
                                    ShortName = "player",
                                    Amount = 5,
                                },   
                                new MainSettings
                                {
                                    DisplayName = "Добыть дерево",
                                    ShortName = "wood",
                                    Amount = 6000,
                                },  
                                new MainSettings
                                {
                                    DisplayName = "Добыть камень",
                                    ShortName = "stones",
                                    Amount = 6000,
                                },
                            },
                            items = new List<ItemsList>
                            {
                                new ItemsList
                                {
                                    ShortName = "wood",
                                    AmountMin = 1000,
                                    AmountMax = 5000,
                                    DropChance = 100
                                },       
                                new ItemsList
                                {
                                    ShortName = "stones",
                                    AmountMin = 1000,
                                    AmountMax = 5000,
                                    DropChance = 100
                                },
                            }
                        },
                        new PassSettings
                        {
                            Level = 2,
                            Count = 3,
                            mains = new List<MainSettings>
                            {
                                new MainSettings
                                {
                                    DisplayName = "Уничтожить танк",
                                    ShortName = "bradleyapc",
                                    Amount = 1,
                                },
                                new MainSettings
                                {
                                    DisplayName = "Сера",
                                    ShortName = "sulfur.ore",
                                    Amount = 500,
                                }
                            },
                            items = new List<ItemsList>
                            {
                                new ItemsList
                                {
                                    ShortName = "stones",
                                    AmountMin = 1000,
                                    AmountMax = 5000,
                                    DropChance = 70
                                }, 
                                new ItemsList
                                {
                                    ShortName = "rifle.ak",
                                    AmountMin = 1,
                                    AmountMax = 1,
                                    DropChance = 70
                                },
                            }
                        }
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
                if (config?.passSettings == null) LoadDefaultConfig();
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
            PrintWarning("\n-----------------------------\n" +
            "     Author - https://topplugin.ru/\n" +
            "     VK - https://vk.com/rustnastroika\n" +
            "     Discord - https://discord.com/invite/5DPTsRmd3G\n" +
            "-----------------------------");
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("PassSystem/PlayerList"))
            {
                ProgressPass = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerTasks>>("PassSystem/PlayerList");
            }
            else
            {
                ProgressPass = new Dictionary<ulong, PlayerTasks>();
            }
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!ProgressPass.ContainsKey(player.userID))
            {
                ProgressPass.Add(player.userID, new PlayerTasks());
            }
            SaveData();
        }

        private PlayerProgress AddPlayersData(ulong userID, string name)
        {
            if (!ProgressPass.ContainsKey(userID))
                ProgressPass[userID].Progress = new Dictionary<string, PlayerProgress>();

            if (!ProgressPass[userID].Progress.ContainsKey(name))
                ProgressPass[userID].Progress[name] = new PlayerProgress();

            return ProgressPass[userID].Progress[name];
        }

        private void Progress(BasePlayer player, string ShortName, int Count)
        {
            foreach (var check in config.passSettings)
            {
                var name = check.mains.FirstOrDefault(x => x.ShortName == ShortName);
                if (name != null)
                {
                    var data = AddPlayersData(player.userID, name.ShortName);
                    if (ProgressPass[player.userID].Level == check.Level)
                    {
                        if (data.Amount <= name.Amount)
                        {
                            if (data.Amount < name.Amount)
                            {
                                data.Amount += Count;
                                SaveData();
                            }
                            else return;
                            if (name.Amount <= data.Amount)
                            {
                                SendReply(player, $"Вы успешно выполнили задание: <color=#ee3e61>{name.DisplayName}</color>!");
                                data.Amount = name.Amount;
                            }
                            else return;
                        }
                        else return;
                    }
                    else return;
                }
            }
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            NextTick(() => Progress(player, item.info.shortname, item.amount));
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            NextTick(() => Progress(player, item.info.shortname, item.amount));
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            Progress(task.owner, item.info.shortname, item.amount);
        }

        public ulong lastDamageName;
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is BradleyAPC && info.Initiator is BasePlayer)
                lastDamageName = info.Initiator.ToPlayer().userID;
            if (entity is BaseHelicopter && info.Initiator is BasePlayer)
                lastDamageName = info.Initiator.ToPlayer().userID;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            BasePlayer player = null;

            if (info.InitiatorPlayer != null) player = info.InitiatorPlayer;
            else if (entity is BradleyAPC || entity is BaseHelicopter) player = BasePlayer.FindByID(lastDamageName);

            if (player == null) return;
            if (entity.ToPlayer() != null && entity as BasePlayer == player) return;
            Progress(player, entity?.ShortPrefabName, 1);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("PassSystem/PlayerList", ProgressPass);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SaveData();
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }
        #endregion

        #region Команды
        private void ChatPass(BasePlayer player)
        {
            if (ProgressPass[player.userID].Level == config.settings.Level)
            {
                SendReply(player, "Вы уже <color=#ee3e61>выполнили</color> все задания!");
            }
            else PassUI(player);
        }

        [ConsoleCommand("pass")]
        private void CmdCase(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "take")
                {
                    bool enable = true;
                    foreach (var check in config.passSettings)
                    {
                        foreach (var pass in check.mains)
                        {
                            if (ProgressPass[player.userID].Level == check.Level)
                            {
                                var data = AddPlayersData(player.userID, pass.ShortName);
                                if (data.Amount >= pass.Amount) continue;
                                enable = false;
                            }
                        }
                    }
                    if (!enable)
                    {
                        SendReply(player, "Вы еще не <color=#ee3e61>выполнили</color> все задания");
                    }
                    else
                    {
                        foreach (var check in config.passSettings)
                        {
                            int count = 0;
                            foreach (var item in check.items)
                            {
                                if (UnityEngine.Random.Range(0, 100) > item.DropChance) continue;
                                int Amount = Core.Random.Range(item.AmountMin, item.AmountMax);
                                if (ProgressPass[player.userID].Level == check.Level)
                                {
                                    if (count >= check.Count) break;
                                    player.inventory.GiveItem(ItemManager.CreateByName(item.ShortName, Amount));
                                    SendReply(player, $"Вы получили: <color=#ee3e61>{item.ShortName}</color>\nВ размере: <color=#ee3e61>{Amount}</color>");
                                    count++;
                                }
                                ProgressPass[player.userID].Progress.Clear();
                                
                            }
                        }
                        ProgressPass[player.userID].Level += 1;
                        CuiHelper.DestroyUi(player, Layer);
                        SaveData();
                    }
                }
                if (args.Args[0] == "start")
                {
                    ProgressPass[player.userID].Level += 1;
                    PassUI(player);
                }
            }
        }
        #endregion

        #region Интерфейс
        private void PassUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Pass");
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.276 0", AnchorMax = "0.945 1", OffsetMax = "0 0" },
                Image = { Color = "0.117 0.121 0.109 0.95" }
            }, "Menu_UI", "Pass");

            if (ProgressPass[player.userID].Level == 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.15", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0" },
                    Text = { Text = $"<b><size=40>{config.settings.ServerName}</size></b>\n\nПривет, <color=#1E88E5><b>{player.displayName.ToUpper()}</b></color>\n\nТы зашел на проект <b></b>, а значит, ты готов тащить и развиваться, в соло или с <b>кривыми</b> тимейтами, но это не важно. Важно лишь то, что мы готовы <b>облегчить</b> твои страдания, но платой за это будет выполнение <b>наших</b> заданий!\n\n<b><color=#1E88E5>Правила</color></b>\n<b><color=#1E88E5>1</color></b>. Выполняешь <b><color=#1E88E5>задания</color></b>\n<b><color=#1E88E5>2</color></b>. Забираешь <b><color=#1E88E5>награду</color></b>\n<b><color=#1E88E5>3</color></b>. Выполняешь <b><color=#1E88E5>новое</color></b> задание\n\nКак видишь, <b>правила просты</b>, скорее приступай!\nА мы желаем <b>тебе</b> удачи и прекрасного вайпа!", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
                }, "Pass");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.4 0.31", AnchorMax = "0.6 0.37", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.1", Command = "pass start" },
                    Text = { Text = $"<color=#1E88E5>ПРИСТУПИТЬ</color>", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
                }, "Pass");
            }
            else
            {
                foreach (var check in config.passSettings)
                {
                    if (ProgressPass[player.userID].Level == check.Level)
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0.643", AnchorMax = "1 0.75", OffsetMax = "0 0" },
                            Button = { Color = "1 1 1 0" },
                            Text = { Text = $"<b><size=35>{config.settings.DisplayName}</size></b>\nУровень: {ProgressPass[player.userID].Level}", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
                        }, "Pass");

                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0.35 0.17", AnchorMax = "0.65 0.22", OffsetMax = "0 0" },
                            Button = { Color = "1 1 1 0.1", Command = $"pass take" },
                            Text = { Text = $"ЗАБРАТЬ НАГРАДУ", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 18, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
                        }, "Pass");

                        float gap1 = 0f, width1 = 0.13f, height1 = 0.15f, xmin1 = 0.1f, ymin1 = 0.63f - height1;
                        for (int i = 0; i < 6; i++)
                        {
                            container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = xmin1 + " " + ymin1, AnchorMax = (xmin1 + width1) + " " + (ymin1 + height1 * 1), OffsetMin = "2 0", OffsetMax = "-2 0" },
                                Image = { Color = "1 1 1 0.1" }
                            }, "Pass");
                            xmin1 += width1;
                        }

                        float width2 = 0.13f, height2 = 0.15f, xmin2 = 0.1f, ymin2 = 0.63f - height2;
                        foreach (var item in check.items)
                        {
                            container.Add(new CuiPanel
                            {
                                RectTransform = { AnchorMin = xmin2 + " " + ymin2, AnchorMax = (xmin2 + width2) + " " + (ymin2 + height2 * 1), OffsetMin = "2 0", OffsetMax = "-2 0" },
                                Image = { Color = "1 1 1 0" }
                            }, "Pass", "Set");

                            container.Add(new CuiElement
                            {
                                Parent = "Set",
                                Components =
                                {
                                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", item.ShortName) },
                                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                                }
                            });

                            var textAmount = item.AmountMin != item.AmountMax ? $"{item.AmountMin}-{item.AmountMax}" : $"{item.AmountMax}";
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                                Button = { Color = "0 0 0 0" },
                                Text = { Text = $"{textAmount}x ", Color = "1 1 1 0.5", Align = TextAnchor.LowerRight, FontSize = 14, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
                            }, "Set");
                            xmin2 += width2;
                        }

                        float width = 0.7f, height = 0.05f, startxBox = 0.145f, startyBox = 0.45f - height, xmin = startxBox, ymin = startyBox;
                        foreach (var pass in check.mains)
                        {
                            var data = AddPlayersData(player.userID, pass.ShortName);
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMin = "2 2", OffsetMax = "-2 -2" },
                                Button = { Color = "1 1 1 0" },
                                Text = { Text = $"" }
                            }, "Pass", "Task");
                            xmin += width;
                            if (xmin + width >= 1)
                            {
                                xmin = startxBox;
                                ymin -= height;
                            }

                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0.35", AnchorMax = "0.5 0.9", OffsetMax = "0 0" },
                                Button = { Color = "1 1 1 0" },
                                Text = { Text = $" {pass.DisplayName}", Color = "1 1 1 0.5", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
                            }, "Task");

                            var text = data.Amount >= pass.Amount ? "выполнено" : $"{data.Amount}/{pass.Amount}";
                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0.5 0.35", AnchorMax = "1 0.9", OffsetMax = "0 0" },
                                Button = { Color = "1 1 1 0" },
                                Text = { Text = $"{text} ", Color = "1 1 1 0.5", Align = TextAnchor.MiddleRight, FontSize = 14, Font = "robotocondensed-regular.ttf", FadeIn = 0.5f }
                            }, "Task");

                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.3", OffsetMax = "0 0" },
                                Button = { Color = "1 1 1 0.12" },
                                Text = { Text = "", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
                            }, "Task", "Progress");

                            container.Add(new CuiButton
                            {
                                RectTransform = { AnchorMin = "0 0", AnchorMax = $"{(float)data.Amount / pass.Amount} 1", OffsetMax = "0 0" },
                                Button = { Color = "0.46 0.73 0.43 0.5" },
                                Text = { Text = "", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-bold.ttf", FadeIn = 0.5f }
                            }, "Progress");
                        }
                    }
                }
            }
            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}