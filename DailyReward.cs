using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Oxide.Core.Database;

namespace Oxide.Plugins
{
    [Info("DailyReward", "Hougan", "1.0.0")]
    [Description("Награды за ежедневный вход на сервер")]
    public class DailyReward : RustPlugin
    {
        #region Classes
        
        Core.MySql.Libraries.MySql Sql = Interface.GetMod().GetLibrary<Core.MySql.Libraries.MySql>();
        Connection Sql_conn;

        private class DailyPlayer
        {
            [JsonProperty("Отображаемое имя игрока")]
            public string DisplayName;
            [JsonProperty("Отображаемый ID игрока")]
            public ulong UserID;
            
            [JsonProperty("Дни в которых игрок получил награду")]
            public Dictionary<string, bool> Joins = new Dictionary<string,bool>();
        }
        private Dictionary<ulong, DailyPlayer> dailyPlayers = new Dictionary<ulong, DailyPlayer>();

        #endregion

        #region Variables

        [JsonProperty("Название слоя с ГУИ")]
        private string Layer = "UI.DailyBonus";
        [JsonProperty("Начальный бонус в рублях")]
        private int DefaultMoney = 1;
        [JsonProperty("Максимальное пополнение за раз")]
        private int MaxDeposit = 10;

        [JsonProperty("Ключ магазина")] 
        private string APIKey;
        [JsonProperty("ID Сервера")] 
        private string ServerID;

        [JsonProperty("Использовать MySQL?")]
        private bool MySQL_Use = true;
        [JsonProperty("MySQL. IP БД")] 
        private string MySQL_IP = "localhost";
        [JsonProperty("MySQL. Port")]
        private int MySQL_Port = 3306;
        [JsonProperty("MySQL. Название БД")] 
        private string MySQL_DBName = "joins";
        [JsonProperty("MySQL. Название таблицы")] 
        private string MySQL_TBName = "playerjoin";
        [JsonProperty("MySQL. Имя пользователя")]
        private string MySQL_Name = "root";
        [JsonProperty("MySQL. Пароль пользователя")]
        private string MySQL_Password = "root1234";

        #endregion

        #region Commands

        [ConsoleCommand("db.get")]
        private void cmdConsoleGet(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null)
            {
                PrintWarning("Команда только для игроков!");
                return;
            }

            DailyPlayer dailyPlayer = dailyPlayers[player.userID];
            if (dailyPlayer.Joins[DateTime.Now.Day.ToString()])
            {
                player.ChatMessage(lang.GetMessage("TODAY.GOT", this, player.UserIDString));
                return;
            }
            
            if (MySQL_Use)
                TodayPlayer(player);
            else
            {
                dailyPlayer.Joins[DateTime.Now.Day.ToString()] = true;

                int current = 0;
                int i = DateTime.Now.Day;
                do
                {
                    if (!dailyPlayer.Joins[i.ToString()])
                        break;
                
                    current++;
                    i--;
                } while (dailyPlayer.Joins.ContainsKey(i.ToString()));

                if (current * DefaultMoney > MaxDeposit)
                {
                    player.ChatMessage(lang.GetMessage("ERROR", this, player.UserIDString).Replace("{0}", (current * DefaultMoney).ToString()));
                    return;
                }
            
                player.ChatMessage(lang.GetMessage("TODAY.SUCCESS", this, player.UserIDString).Replace("{0}", (current * DefaultMoney).ToString()).Replace("{1}", ((current + 1) * DefaultMoney).ToString()));
            
                LogToFile("DailyBonus", $"{player.displayName} [{player.userID}] получил {current * DefaultMoney} рублей", this);
            
                AddMoney(player.userID, current * DefaultMoney);
            
                DailyGUI(player);
            }
        }

        #endregion

        #region MySQL
        
        private void TodayPlayer(BasePlayer player)
        {
            if (Sql_conn == null)
                Sql_conn = Sql.OpenDb(MySQL_IP, MySQL_Port, MySQL_DBName, MySQL_Name, MySQL_Password, this);
            if (Sql_conn?.Con == null)
            {
                if (Sql_conn != null)
                    Puts("Ошибка соединения с БД: " + Sql_conn.Con?.State.ToString());
                else
                    Puts("Ошибка соединения с БД не определена!");

                return;
            }
            Sql.Query(Core.Database.Sql.Builder.Append($"SELECT * FROM `{MySQL_DBName}`.`{MySQL_TBName}` WHERE `userid`={player.userID};"), Sql_conn, lists =>
            {
                if (lists.Count == 0)
                {
                    AddPrize(player, true);
                    return;
                }

                foreach (var check in lists)
                {
                    if (check.Values.ToList()[1].ToString() != DateTime.Now.Day.ToString())
                    {
                        AddPrize(player, false);
                        return;
                    }
                    player.ChatMessage(lang.GetMessage("TODAY.GOT", this, player.UserIDString));
                    return;
                }
            });
            
            Sql_conn.Con.Close();
        }

        private void AddPrize(BasePlayer player, bool newPlayer = false)
        {
            if (!newPlayer)
            {
                Sql.Query(Core.Database.Sql.Builder.Append($"UPDATE `{MySQL_DBName}`.`{MySQL_TBName}` SET `join`= '{DateTime.Now.Day}' WHERE `userid`= {player.userID};"), Sql_conn, lists =>
                {
                    DailyPlayer dailyPlayer = dailyPlayers[player.userID];
                    dailyPlayer.Joins[DateTime.Now.Day.ToString()] = true;

                    int current = 0;
                    int i = DateTime.Now.Day;
                    do
                    {
                        if (!dailyPlayer.Joins[i.ToString()])
                            break;
                
                        current++;
                        i--;
                    } while (dailyPlayer.Joins.ContainsKey(i.ToString()));

                    if (current * DefaultMoney > MaxDeposit)
                    {
                        player.ChatMessage(lang.GetMessage("ERROR", this, player.UserIDString).Replace("{0}", (current * DefaultMoney).ToString()));
                        return;
                    }
            
                    player.ChatMessage(lang.GetMessage("TODAY.SUCCESS", this, player.UserIDString).Replace("{0}", (current * DefaultMoney).ToString()).Replace("{1}", ((current + 1) * DefaultMoney).ToString()));
            
                    LogToFile("DailyBonus", $"{player.displayName} [{player.userID}] получил {current * DefaultMoney} рублей", this);
            
                    AddMoney(player.userID, current * DefaultMoney);
            
                    DailyGUI(player);
                });
            }
            else
            {
                Sql.Query(Core.Database.Sql.Builder.Append(string.Format($"INSERT INTO `{MySQL_DBName}`.`{MySQL_TBName}` (`userid`, `join`) VALUES ('{player.userID}', '{DateTime.Now.Day}');")), Sql_conn, lists =>
                {
                    DailyPlayer dailyPlayer = dailyPlayers[player.userID];
                    dailyPlayer.Joins[DateTime.Now.Day.ToString()] = true;

                    int current = 0;
                    int i = DateTime.Now.Day;
                    do
                    {
                        if (!dailyPlayer.Joins[i.ToString()])
                            break;
                
                        current++;
                        i--;
                    } while (dailyPlayer.Joins.ContainsKey(i.ToString()));

                    if (current * DefaultMoney > MaxDeposit)
                    {
                        player.ChatMessage(lang.GetMessage("ERROR", this, player.UserIDString).Replace("{0}", (current * DefaultMoney).ToString()));
                        return;
                    }
            
                    player.ChatMessage(lang.GetMessage("TODAY.SUCCESS", this, player.UserIDString).Replace("{0}", (current * DefaultMoney).ToString()).Replace("{1}", ((current + 1) * DefaultMoney).ToString()));
            
                    LogToFile("DailyBonus", $"{player.displayName} [{player.userID}] получил {current * DefaultMoney} рублей", this);
            
                    AddMoney(player.userID, current * DefaultMoney);
            
                    DailyGUI(player);
                });
            }
        }

        #endregion

        #region Functions

        [PluginReference] 
        private Plugin RustStore;
        private bool Moscow = false;

        private void AddMoney(ulong userId, int amount)
        {
            ExecuteApiRequest(new Dictionary<string, string>()
            {
                { "action", "moneys" },
                { "type", "plus" },
                { "steam_id", userId.ToString() },
                { "amount", amount.ToString() },
                { "mess", "Ежедневная награда! Спасибо что играете у нас!"}
            }, Moscow);
        }
        
        void ExecuteApiRequest(Dictionary<string, string> args, bool Moscow)
        {
            if (!Moscow)
            {
                string url = $"http://panel.gamestores.ru/api?shop_id={ServerID}&secret={APIKey}" +
                             $"{string.Join("",args.Select(arg => $"&{arg.Key}={arg.Value}").ToArray())}";
                LogToFile("test", url, this);
                string moscowUrl = "https://store-api.moscow.ovh/index.php";
                webrequest.EnqueueGet(url, (i, s) =>
                {
                    if (i != 200)
                    {
                        PrintError($"Ошибка соединения с сайтом GS!");
                    }
                    else
                    {
                        JObject jObject = JObject.Parse(s);
                        if (jObject["result"].ToString() == "fail")
                        {
                            PrintError($"Ошибка пополнения баланса для {args["steam_id"]}!");
                            PrintError($"Причина: {jObject["message"].ToString()}");
                        }
                        else
                            PrintWarning($"Игрок {args["steam_id"]} успешно получил {args["amount"]} рублей");
                    }
                }, this);
            }
            else
            {
                RustStore.Call("APIChangeUserBalance", Convert.ToUInt64(args["steam_id"]), Convert.ToInt32(args["amount"]), null);
            }
            
        }
        
        #endregion

        #region Initialization

        private void MySQL_Initialize()
        {
            if (Sql_conn == null)
            {
                // Открываем новое соединение
                Sql_conn = Sql.OpenDb(MySQL_IP, MySQL_Port, "", MySQL_Name, MySQL_Password, this);
            }
            
            if (Sql_conn?.Con == null)
            {
                if (Sql_conn != null)
                    Puts("Ошибка соединения с БД: " + Sql_conn.Con?.State.ToString());
                else
                    Puts("Ошибка соединения с БД не определена!");

                PrintError("Ошибка инициализации БД, плагин выгружается!");
                Interface.Oxide.UnloadPlugin(this.Name);
                return;
            }
            
            Sql.Query(Core.Database.Sql.Builder.Append($"CREATE DATABASE IF NOT EXISTS {MySQL_DBName};"), Sql_conn, lists =>
            {
                PrintWarning("База данных создана или обновлена!");
            });
            
            
            Sql.Query(Core.Database.Sql.Builder.Append($"CREATE TABLE IF NOT EXISTS `{MySQL_DBName}`.`{MySQL_TBName}` (`userid` VARCHAR(18) NOT NULL, `join` VARCHAR(3) NULL, PRIMARY KEY (`userid`));"), Sql_conn, lists =>
            {
                PrintWarning("Таблица данных создана или обновлена!");
            });
            
            Sql.Query(Core.Database.Sql.Builder.Append($"SELECT * FROM `{MySQL_DBName}`.`{MySQL_TBName}` WHERE `userid`={123456789};"), Sql_conn, lists =>
            {
                if (lists.Count == 0)
                {
                    PrintWarning("ТЕСТ №1 - Пройден успешно");
                    return;
                }
            });
            
            Sql.Query(Core.Database.Sql.Builder.Append(string.Format($"INSERT IGNORE INTO `{MySQL_DBName}`.`{MySQL_TBName}` (`userid`, `join`) VALUES ('{123456789}', '{DateTime.Now.AddDays(1).Day}');")), Sql_conn, lists =>
            {
            });
            
            Sql.Query(Core.Database.Sql.Builder.Append($"SELECT * FROM `{MySQL_DBName}`.`{MySQL_TBName}` WHERE `userid`={123456789};"), Sql_conn, lists =>
            {
                if (lists.Count == 1)
                {
                    PrintWarning("ТЕСТ №2 - Пройден успешно");
                    return;
                }
                PrintError("ТЕСТ №3 - Не пройден!");
            });
            
            Sql.Query(Core.Database.Sql.Builder.Append($"UPDATE `{MySQL_DBName}`.`{MySQL_TBName}` SET `join`= '{DateTime.Now.Day}' WHERE `userid`= {123456789};"), Sql_conn, lists =>
            {
                
            });
            
            Sql.Query(Core.Database.Sql.Builder.Append($"SELECT * FROM `{MySQL_DBName}`.`{MySQL_TBName}` WHERE `userid`={123456789};"), Sql_conn, lists =>
            {
                if (lists.Count == 1)
                {
                    foreach (var check in lists)
                    {
                        if (check.Values.ToList()[1].ToString() == DateTime.Now.Day.ToString())
                        {
                            PrintWarning("Тест №3 - Пройден успешно");
                            PrintWarning("База данных успешно работает!");
                            return;
                        }
                        PrintError("ТЕСТ №3 - Не пройден!");
                        return;
                    }
                    PrintError("ТЕСТ №3 - Не пройден!");
                    return;
                }
                PrintError("ТЕСТ №3 - Не пройден!");
            });
            
            Sql.Query(Core.Database.Sql.Builder.Append($"DELETE FROM `{MySQL_DBName}`.`{MySQL_TBName}` WHERE `userid`='123456789';"), Sql_conn,
            lists =>
            {
                
            });
        }
        
        private void OnServerInitialized() // xy
        {
            LoadDefaultConfig();

            if (APIKey == "Сюда АПИ ключ")
            {
                PrintError("Введите АПИ ключ магазина!");
                Interface.Oxide.UnloadPlugin(this.Name);
                return;
            }
            if (ServerID == "Сюда ID сервера")
            {
                PrintError("Введите ID сервера!");
                Interface.Oxide.UnloadPlugin(this.Name);
                return;
            }
            
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("DailyBonus/Players"))
                dailyPlayers = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, DailyPlayer>>("DailyBonus/Players");
            
            
            PrintWarning("Плагин - 'Ежедневная награда' загружен!");
            PrintWarning("Разработчик - HOUGAN!");
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TODAY.GOT"] = "<size=16><color=#FF5733>Ежедневный</color> бонус</size>" +
                                "\n" +
                                "\nВы уже <color=#FF5733>получили</color> награду за сегодня!",
                ["TODAY.SUCCESS"] = "<size=16><color=#FF5733>Ежедневный</color> бонус</size>" +
                                    "\n" +
                                    "\nВы <color=#FF5733>успешно</color> получили <color=#FF5733>{0}</color> рублей!" +
                                    "\n<size=10>Заходите <color=#FF5733>завтра</color> и получите <color=#FF5733>{1}</color> рублей</size>",
                ["ERROR"] = "Ошибка сервера, напишите в группу! [{0}]"
            }, this);
            if (MySQL_Use)
            {
                PrintWarning("Включен режим эксперта. Могут быть задержки в выполнении функций плагина!");
                MySQL_Initialize();
            }
            BasePlayer.activePlayerList.ForEach(OnPlayerInit);
        }
        
        protected override void LoadDefaultConfig()
        {
            Config["GS. API Ключ"] = APIKey = GetConfig("GS. API Ключ", "Сюда АПИ ключ");
            Config["GS. ID Магазина"] = ServerID = GetConfig("GS. ID Магазина", "Сюда ID магазина");
            Config["Moscow. У вас магазин ОВХ?"] = Moscow = GetConfig("Moscow. У вас магазин ОВХ?", false);
            Config["Награда - стартовый баланс за первый день"] = DefaultMoney = GetConfig("Награда - стартовый баланс за первый день", 1);
            Config["Защита от слишком большого пополнения"] = MaxDeposit = GetConfig("Защита от слишком большого пополнения", 10);
            
            
            Config["MySQL. Использовать MySQL (TRUE только если знаете что делаете!)"] = MySQL_Use = GetConfig("MySQL. Использовать MySQL (TRUE только если знаете что делаете!)", false);
            Config["MySQL. IP Сервера с БД"] = MySQL_IP = GetConfig("MySQL. IP Сервера с БД", "localhost");
            Config["MySQL. Порт сервера с БД"] = MySQL_Port = GetConfig("MySQL. Порт сервера с БД", 3306);
            Config["MySQL. Имя базы данных"] = MySQL_DBName = GetConfig("MySQL. Имя базы данных", "dailyreward");
            Config["MySQL. Название таблицы"] = MySQL_TBName = GetConfig("MySQL. Название таблицы", "jointable");
            Config["MySQL. Имя пользователя"] = MySQL_Name = GetConfig("MySQL. Имя пользователя", "root");
            Config["MySQL. Пароль пользователя"] = MySQL_Password = GetConfig("MySQL. Пароль пользователя", "root1234");
            
            SaveConfig();
        }

        private void Unload() => Interface.Oxide.DataFileSystem.WriteObject("DailyBonus/Players", dailyPlayers);

        private void OnPlayerInit(BasePlayer player)
        {
            if (!dailyPlayers.ContainsKey(player.userID))
            {
                dailyPlayers.Add(player.userID, new DailyPlayer
                {
                    DisplayName = player.displayName,
                    UserID = player.userID,

                    Joins = new Dictionary<string, bool>()
                });

                
                if (SaveRestore.SaveCreatedTime.Day - DateTime.Now.Day > 7)
                {
                    dailyPlayers[player.userID].Joins = new Dictionary<string, bool>
                    {
                        [SaveRestore.SaveCreatedTime.Day.ToString()] = false,
                        [SaveRestore.SaveCreatedTime.AddDays(1).Day.ToString()] = false,
                        [SaveRestore.SaveCreatedTime.AddDays(2).Day.ToString()] = false,
                        [SaveRestore.SaveCreatedTime.AddDays(3).Day.ToString()] = false,
                        [SaveRestore.SaveCreatedTime.AddDays(4).Day.ToString()] = false,
                        [SaveRestore.SaveCreatedTime.AddDays(5).Day.ToString()] = false,
                        [SaveRestore.SaveCreatedTime.AddDays(6).Day.ToString()] = false,
                    };
                }
                else
                {
                    dailyPlayers[player.userID].Joins = new Dictionary<string, bool>
                    {
                        [DateTime.Now.Day.ToString()] = false,
                        [DateTime.Now.AddDays(1).Day.ToString()] = false,
                        [DateTime.Now.AddDays(2).Day.ToString()] = false,
                        [DateTime.Now.AddDays(3).Day.ToString()] = false,
                        [DateTime.Now.AddDays(4).Day.ToString()] = false,
                        [DateTime.Now.AddDays(5).Day.ToString()] = false,
                        [DateTime.Now.AddDays(6).Day.ToString()] = false,
                    }; 
                }
            }

            if (!dailyPlayers[player.userID].Joins.ContainsKey(DateTime.Now.Day.ToString()))
            {
                dailyPlayers.Remove(player.userID);
                NextTick(() => OnPlayerInit(player));
                return;
            }

            if (dailyPlayers[player.userID].Joins[DateTime.Now.Day.ToString()])
                return;

            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerInit(player);
                    return;
                });
            }

            DailyGUI(player);
        }

        #endregion

        #region GUI
        
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

        [ChatCommand("daily")]
        private void DailyGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.3265625 0.4030093", AnchorMax = "0.6734375 0.5969907" },
                Image = { Color = HexToRustFormat("#A1A1A13F") }
            }, "Hud", Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Close = Layer, Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer, Layer + ".Close");
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Header.BG",
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#D6D6D6FF") },
                    new CuiRectTransformComponent { AnchorMin = "0 0.7147974", AnchorMax = "0.997 1" },
                    new CuiOutlineComponent { Color = HexToRustFormat("#3434347D"), Distance = "0 3" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer + ".Header.BG",
                Name = Layer + ".Header.HEADER",
                Components =
                {
                    new CuiTextComponent { Text = "ЕЖЕДНЕВНАЯ НАГРАДА", Color = HexToRustFormat("#343434FF"), FontSize = 24, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0 0.2845186", AnchorMax = "1 1" },
                    new CuiOutlineComponent { Color = HexToRustFormat("#3434347D"), Distance = "0.155 0.155" }
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer + ".Header.BG",
                Name = Layer + ".Header.HELP",
                Components =
                {
                    new CuiTextComponent { Text = "Заходите каждый день, чтобы получить увеличенный бонус!", Color = HexToRustFormat("#343434FF"), FontSize = 14, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 0.4184099" },
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Container",
                Components =
                {
                    new CuiImageComponent() { Color = "0 0 0 0" },
                    new CuiRectTransformComponent { AnchorMin = "0 0.2261503", AnchorMax = "1 0.6843842" },
                }
            });
            
            int money = 0;
            int current = 0;
            foreach (var check in dailyPlayers[player.userID].Joins)
            {
                container.Add(new CuiElement
                {
                    Parent = Layer + ".Container",
                    Name = Layer + $".Container.{check.Key}",
                    Components =
                    {
                        new CuiImageComponent { Color = HexToRustFormat(check.Value ? "#3BA75BFF" : DateTime.Now.Day < Convert.ToInt32(check.Key) ? "#9A9A9AFF" : DateTime.Now.Day != Convert.ToInt32(check.Key) ? "#DC4444FF" : "#2B81B4FF") },
                        new CuiRectTransformComponent { AnchorMin = $"{0.007704161 + 0.1418 * current} 0.05208336", AnchorMax = $"{0.1377565 + 0.1418 * current} 0.9479166" },
                        new CuiOutlineComponent { Color = HexToRustFormat("#3434347D"), Distance = "1.5 1.5" }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = Layer + $".Container.{check.Key}",
                    Components =
                    {
                        new CuiTextComponent { Text = check.Key, Color = HexToRustFormat(DateTime.Now.Day == Convert.ToInt32(check.Key) ? "#343434FF" : "#FFFFFFFF"), FontSize = 35, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = $"0 0.2325586", AnchorMax = $"1 0.9069777" },
                        new CuiOutlineComponent { Color = HexToRustFormat("#3434347D"), Distance = DateTime.Now.Day == Convert.ToInt32(check.Key) ? "0.5 0.5" : "0 0" }
                    }
                });
                string status = !check.Value ? "ПРОПУСК" : "ПОЛУЧЕНО";
                if (Convert.ToInt32(check.Key) >= DateTime.Now.Day)
                {
                    money += 5;
                    if (!check.Value)
                        status = money + " РУБЛЕЙ";
                }
                
                container.Add(new CuiElement
                {
                    Parent = Layer + $".Container.{check.Key}",
                    Components =
                    {
                        new CuiTextComponent { Text = status, Color = "1 1 1 1", FontSize = 9, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                        new CuiRectTransformComponent { AnchorMin = $"0 0.05813904", AnchorMax = $"1 0.3837207" }
                    }
                });

                current++;
            }
            
            container.Add(new CuiElement
            {
                Parent = Layer,
                Name = Layer + ".Get",
                Components =
                {
                    new CuiImageComponent { Color = HexToRustFormat("#D6D6D6FF") },
                    new CuiOutlineComponent { Color = HexToRustFormat("#3434347D"), Distance = "2 2" },
                    new CuiRectTransformComponent { AnchorMin = "0.289039 0.03699267", AnchorMax = "0.710961 0.2040572" },
                }
            });
            container.Add(new CuiElement
            {
                Parent = Layer + ".Get",
                Components =
                {
                    new CuiTextComponent { Text = "ЗАБРАТЬ ПРИЗ", Color = HexToRustFormat("#343434FF"), FontSize = 20, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter},
                    new CuiRectTransformComponent { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                }
            });
            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 0", Command = "db.get", Close = Layer },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Text = { Text = "" }
            }, Layer + ".Get");

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));

        #endregion
    }
}