using System.Net;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core;
using System.Linq;
using System;
using Network;
using Newtonsoft.Json.Linq;
using ConVar;

namespace Oxide.Plugins
{
    [Info("TPReportSystem", "Sempai#3239", "5.0.0")]
    class TPReportSystem : RustPlugin
    {
        #region Вар
        [PluginReference] Plugin ImageLibrary;

        public string Layer = "lay";

        Dictionary<ulong, string> name = new Dictionary<ulong, string>();
        #endregion

        #region Вар
        Dictionary<ulong, DataBase> DB;
        public class DataBase 
        {
            [JsonProperty("Ник игрока")] public string DisplayName;
            [JsonProperty("SteamID игрока")] public ulong SteamID;
            [JsonProperty("Кол-во проверок у игрока")] public int Count;
            [JsonProperty("Дискорд игрока")] public string DS;
            [JsonProperty("SteamID проверяющего")] public ulong SteamID2;
            [JsonProperty("Вызван ли игрок на проверку")] public bool Enable;
            [JsonProperty("Список с жалобами и их кол-вом")] public Dictionary<string, int> Res = new Dictionary<string, int>() {
                ["Reason_1"] = 0,
                ["Reason_2"] = 0,
                ["Reason_3"] = 0
            };
        }
        #endregion

        #region Конфиг
        public Configuration config;
        public class Configuration
        {
            [JsonProperty("Пермишен для модераторов")] public string Perm = "tpreportsystem.use";
            [JsonProperty("Информация плагина")] public string Info = "Ахуенный плагин скилов всем советую, а кто не купит, тот гомосек";
            [JsonProperty("Оповещание")] public string Title = "Предоставте свой дискорд или скайп для проверки.\nВведите команду /contact\nЕсли Вы покинете сервер, Вы будете забанены на проекте FEDOT RUST.\nУ вас есть 5 минут!";
            [JsonProperty("Webhook")] public String WebhookNotify;
			[JsonProperty("Цвет сообщения в Discord (Можно найти на сайте - https://old.message.style/dashboard в разделе JSON)")] public Int32 Color; 
			[JsonProperty("Заголовок сообщения")] public String AuthorName;
			[JsonProperty("Ссылка на иконку для аватарки сообщения")] public String IconURL;
            [JsonProperty("Список жалоб")] public Dictionary<string, string> Reasons;
            [JsonProperty("Список причин бана")] public List<string> Ban;
            public static Configuration GetNewCong()
            {
                return new Configuration
                {
                    Reasons = new Dictionary<string, string>()
                    {
                        ["Reason_1"] = "Багоюз игрового процеса",
                        ["Reason_2"] = "Использование макросов",
                        ["Reason_3"] = "Игра с читами"
                    },
                    Ban = new List<string>()
                    {
                        "багоюз",
                        "читы",
                        "макросы"
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
                if (config?.Reasons == null) LoadDefaultConfig();
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

        #region Хуки
        Dictionary<ulong, int> gg = new Dictionary<ulong, int>();
        void OnServerInitialized() {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("Report/Player"))
                DB = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, DataBase>>("Report/Player");
            else
                DB = new Dictionary<ulong, DataBase>();

            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/report_back.png", "b6wyl5i");
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/report_border.png", "lamC17G");
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/report_window.png", "HoGCxnN");
            ImageLibrary.Call("AddImage", "https://rustage.su/img/server/ui/report_btn_active.png", "DA7OETZ");
            ImageLibrary.Call("AddImage", "https://imgur.com/aAm4ZHw.png", "aAm4ZHw");
            ImageLibrary.Call("AddImage", "https://imgur.com/xclemZi.png", "xclemZi");
            ImageLibrary.Call("AddImage", "https://media.discordapp.net/attachments/1138899641472131165/1145112601072775279/5543435453453543.png", "fonDescription");

            permission.RegisterPermission(config.Perm, this);

            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        void OnPlayerConnected(BasePlayer player) {
            if (!DB.ContainsKey(player.userID))
                DB.Add(player.userID, new DataBase());
        }

        void SaveDataBase() => Interface.Oxide.DataFileSystem.WriteObject("Report/Player", DB);

        void OnPlayerDisconnected(BasePlayer player, string reason) => SaveDataBase();

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Layer);

            SaveDataBase();
        }
        #endregion

        #region Команды
        [ChatCommand("contact")]
        void ChatContact(BasePlayer player, string command, string[] args) {
            var db = DB[player.userID];
            if (db.Enable == true) {
                if (args.Length < 1)
                {
                    SendReply(player, "Вы ничего не ввели!\n<size=12>Введите /contact [дискорд или скайп]</size>");
                }
                else
                {
                    var name = "";
                    for(int z = 0; z < args.Length; z++) 
                        name += args[z] + " ";
                    
                    db.DS = name;
                    var target = BasePlayer.activePlayerList.FirstOrDefault(z => z.userID == db.SteamID2);
                    TargetUI(target, db.SteamID);
                    List<Fields> fields = DT_PlayerSendContact(player, name);
		            SendDiscord(config.WebhookNotify, fields, new Authors(player.displayName, "", "", ""), config.Color);
                    SendReply(player, "Вы успешно отправили свой дискорд, ожидайте звонка!");
                }
            }
        }

        [ConsoleCommand("report")]
        void ConsoleReport(ConsoleSystem.Arg args) {
            var player = args.Player();
            if (player != null && args.HasArgs(1))
            {
                if (args.Args[0] == "skip")
                {
                    UI(player, "", int.Parse(args.Args[1]));
                }
                if (args.Args[0] == "skips")
                {
                    ModerUI(player, "", int.Parse(args.Args[1]));
                }
                if (args.Args[0] == "name")
                {
                    if (!args.HasArgs(2)) {
                        name[player.userID] = "";
                        UI(player, name[player.userID]);
                        return;
                    }
                    name[player.userID] = args.Args[1];
                }
                if (args.Args[0] == "search")
                {
                    if (name[player.userID] != "")
                        UI(player, name[player.userID]);
                }
                if (args.Args[0] == "target")
                {
                    InfoUI(player, ulong.Parse(args.Args[1]));
                }
                if (args.Args[0] == "reason") {
                    var target = BasePlayer.activePlayerList.FirstOrDefault(z => z.userID == ulong.Parse(args.Args[1]));
                    if (player.userID == target.userID) {
                        SendReply(player, "Вы не можете отправить жалобу на самого себя!");
                        return;
                    }
                    DB[target.userID].DisplayName = target.displayName;
                    DB[target.userID].SteamID = target.userID;
                    DB[target.userID].Res[args.Args[2]] += 1;
                    List<Fields> fields = DT_PlayerSendReport(player, target.userID, config.Reasons[args.Args[2]]);
		            SendDiscord(config.WebhookNotify, fields, new Authors(player.displayName, "", "", ""), config.Color);
                    SendReply(player, "Вы успешно отправили жалобу на игрока!");
                    CuiHelper.DestroyUi(player, "Menu_UI");
                }
                if (args.Args[0] == "back") 
                {
                    ReportUI(player);
                }
                if (args.Args[0] == "moder") 
                {
                    if (!permission.UserHasPermission(player.UserIDString, config.Perm)) return;
                    ModerUI(player);
                }
                if (args.Args[0] == "player")
                {
                    if (!permission.UserHasPermission(player.UserIDString, config.Perm)) return;
                    TargetUI(player, ulong.Parse(args.Args[1]));
                }
                if (args.Args[0] == "check")
                {
                    if (!permission.UserHasPermission(player.UserIDString, config.Perm)) return;
                    var target = BasePlayer.activePlayerList.FirstOrDefault(z => z.userID == ulong.Parse(args.Args[1]));
                    var db = DB[target.userID];
                    if (db.Enable == false)
                    {
                        CuiHelper.DestroyUi(player, "Menu_UI");
                        db.SteamID2 = player.userID;
                        db.Enable = true;
                        db.Count += 1;
                        TargetUI(player, target.userID);
                        List<Fields> fields = DT_PlayerCheck(player, target.userID);
		                SendDiscord(config.WebhookNotify, fields, new Authors(player.displayName, "", "", ""), config.Color);
                        CheckUI(target);
                    }
                    else
                    {
                        CuiHelper.DestroyUi(player, LayerCheck);
                        db.DisplayName = null;
                        db.SteamID = 0;
                        db.SteamID2 = 0;
                        db.Enable = false;
                        db.DS = null;
                        List<Fields> fields = DT_PlayerCheckRemove(player, target.userID);
		                SendDiscord(config.WebhookNotify, fields, new Authors(player.displayName, "", "", ""), config.Color);
                        CuiHelper.DestroyUi(target, "Check_UI");
                    }
                }
                if (args.Args[0] == "ban")
                {
                    if (!permission.UserHasPermission(player.UserIDString, config.Perm)) return;
                    var target = BasePlayer.activePlayerList.FirstOrDefault(z => z.userID == ulong.Parse(args.Args[1]));
                    var db = DB[target.userID];
                    db.DisplayName = null;
                    db.SteamID = 0;
                    db.SteamID2 = 0;
                    db.Enable = false;
                    db.DS = null;
                    CuiHelper.DestroyUi(player, LayerCheck);
                    Server.Command($"ban {target.userID} {args.Args[2]}");
                }
            }
        }
        #endregion

        #region Интерфейс
        void ReportUI(BasePlayer player) {
            name[player.userID] = "";
            var container = new CuiElementContainer();
            
            container.Add(new CuiElement
            {
                Name = Layer + ".Main",
                Parent = ".Mains",
                Components = 
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "b6wyl5i") },
                    new CuiRectTransformComponent { AnchorMin = "-0.315 -0.27", AnchorMax = "1.3 1.275", OffsetMax = "0 0" },
                }
            });  

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8 0.804", AnchorMax = "0.817 0.832" },
                Button = { Close = "Menu_UI", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.78 0.805", AnchorMax = "0.795 0.833", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = "reportdesc" },
                Text = { Text = "?", Color = "1 1 1 0.7", Align = TextAnchor.MiddleCenter, FontSize = 16, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".Main");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.6 0.662", AnchorMax = "0.685 0.699", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0" },
                Text = { Text = "", Color = "1 1 1 0.4", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".Main", "Nik");

            container.Add(new CuiElement
            {
                Parent = "Nik",
                Components =
                {
                    new CuiInputFieldComponent { Command = "report name ", Text = "", Color = "1 1 1 0.3", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.69 0.67", AnchorMax = "0.736 0.69", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0", Command = "report search" },
                Text = { Text = "", Color = "1 1 1 0.4", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".Main");

            if (permission.UserHasPermission(player.UserIDString, config.Perm)) {
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.44 0.66", AnchorMax = "0.57 0.7", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0"}
            }, Layer + ".Main", "Moder");

            container.Add(new CuiElement
            {
                Parent = "Moder",
                Components = {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "DA7OETZ"), Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0", Command = "report moder" },
                Text = { Text = "Модерация", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Moder");
            }

            CuiHelper.AddUi(player, container);
            UI(player);
        }

        [ConsoleCommand("reportdesc")]
        void DescUI(ConsoleSystem.Arg args) {
            var player = args.Player();
            CuiHelper.DestroyUi(player, Layer + ".Main" + ".Description");
            var container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = Layer + ".Main" + ".Description",
                Parent = Layer + ".Main",
                Components = {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "fonDescription") },
                    new CuiRectTransformComponent { AnchorMin = $"0.58 0.6", AnchorMax = $"0.8 0.8" },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = "0.9 1" },
                Text = { Text = $"Описание репортов", Color = "1 1 1 0.65",FontSize = 14, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
            }, Layer + ".Main" + ".Description");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.05 0", AnchorMax = "1 0.7" },
                Text = { Text = $"{config.Info}", Color = "1 1 1 0.65",FontSize = 12, Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperLeft }
            }, Layer + ".Main" + ".Description");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.9 0.82", AnchorMax = "0.98 0.98" },
                Button = { Close = Layer + ".Main" + ".Description", Color = "1 1 1 0" },
                Text = { Text = "" }
            }, Layer + ".Main" + ".Description");

            CuiHelper.AddUi(player, container);
        }

        void UI(BasePlayer player,string name = "", int page = 0)
        {
            CuiHelper.DestroyUi(player, "Reports");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.65" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Main", "Reports");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.033 0.38", AnchorMax = "0.085 0.645", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = page != 0 ? $"report skip {page - 1}" : "" },
                Text = { Text = "", Color = "0 0 0 0", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Reports");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.91 0.38", AnchorMax = "0.962 0.645", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = BasePlayer.activePlayerList.Count() > (page + 1) * 18 ? $"report skip {page + 1}" : "" },
                Text = { Text = "", Color = "0 0 0 0", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Reports");

            var target = BasePlayer.activePlayerList.Skip(page * 18).Take(18);
            float width = 0.132f, height = 0.31f, startxBox = 0.101f, startyBox = 0.985f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in target)
            {
                if (check.displayName.Contains(name)) {
                    container.Add(new CuiButton
                    {  
                        RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMin = "4 4", OffsetMax = "-4 -4" },
                        Button = { Color = "1 1 1 0", Command = $"report target {check.userID}" },
                        Text = { Text = "", Color = "1 1 1 0.6", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                    }, "Reports", "Players");

                    container.Add(new CuiElement
                    {
                        Parent = "Players",
                        Components = {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.UserIDString) },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMin = "6 6", OffsetMax = "-6 -6" },
                        }
                    });

                    container.Add(new CuiElement
                    {
                        Parent = "Players",
                        Components = {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "lamC17G") },
                            new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                        }
                    });

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.29", OffsetMax = "0 0" },
                        Text = { Text = check.displayName, Color = "1 1 1 0.6", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                    }, "Players");

                    xmin += width + 0.0003f;
                    if (xmin + width >= 1)
                    {
                        xmin = startxBox;
                        ymin -= height + 0.008f;
                    }
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void ModerUI(BasePlayer player, string name = "", int page = 0) {
            CuiHelper.DestroyUi(player, "Reports");
            CuiHelper.DestroyUi(player, "Moder");
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.65" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Main", "Reports");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.57 0.66", AnchorMax = "0.74 0.7", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0"}
            }, Layer + ".Main", "Moder");

            container.Add(new CuiElement
            {
                Parent = "Moder",
                Components = {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "DA7OETZ"), Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "1 1 1 0", Command = "report back" },
                Text = { Text = "Назад", Color = "1 1 1 0.8", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
            }, "Moder");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.033 0.38", AnchorMax = "0.085 0.645", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = page != 0 ? $"report skips {page - 1}" : "" },
                Text = { Text = "", Color = "0 0 0 0", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Reports");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.91 0.38", AnchorMax = "0.962 0.645", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = DB.Count() > (page + 1) * 18 ? $"report skips {page + 1}" : "" },
                Text = { Text = "", Color = "0 0 0 0", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Reports");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.91 0.38", AnchorMax = "0.962 0.645", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = DB.Count() > (page + 1) * 18 ? $"report skip {page + 1}" : "" },
                Text = { Text = "", Color = "0 0 0 0", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Reports");

            var target = DB.Skip(page * 18).Take(18);
            float width = 0.132f, height = 0.31f, startxBox = 0.101f, startyBox = 0.985f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in target)
            {
                if (check.Value.SteamID != 0) {
                    if (check.Value.DisplayName.Contains(name)) {
                        container.Add(new CuiButton
                        {  
                            RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMin = "4 4", OffsetMax = "-4 -4" },
                            Button = { Color = "1 1 1 0", Command = $"report player {check.Key}" },
                            Text = { Text = "", Color = "1 1 1 0.6", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                        }, "Reports", "Players");

                        container.Add(new CuiElement
                        {
                            Parent = "Players",
                            Components = {
                                new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.Key.ToString()) },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMin = "6 6", OffsetMax = "-6 -6" },
                            }
                            
                        });

                        container.Add(new CuiElement
                        {
                            Parent = "Players",
                            Components = {
                                new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "lamC17G") },
                                new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                            }
                        });

                        container.Add(new CuiLabel
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.29", OffsetMax = "0 0" },
                            Text = { Text = check.Value.DisplayName, Color = "1 1 1 0.6", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                        }, "Players");

                        xmin += width + 0.0003f;
                        if (xmin + width >= 1)
                        {
                            xmin = startxBox;
                            ymin -= height + 0.008f;
                        }
                    }
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void InfoUI(BasePlayer player, ulong id) {
            var container = new CuiElementContainer();

            var target = BasePlayer.activePlayerList.FirstOrDefault(z => z.userID == id);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.28 0.22", AnchorMax = "0.72 0.8", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Reports", "Report");

            container.Add(new CuiElement
            {
                Parent = "Report",
                Components = {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "HoGCxnN"), Color = "1 1 1 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.08 0.775", AnchorMax = "0.87 0.9", OffsetMax = "0 0" },
                Text = { Text = "Жалоба на игрока", Color = "1 1 1 0.4", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, "Report");

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.88 0.775", AnchorMax = "0.945 0.9", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = "Report" },
                Text = { Text = "", Color = "0 0 0 0", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Report");

            container.Add(new CuiElement
            {
                Parent = "Report",
                Components = {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", target.UserIDString) },
                    new CuiRectTransformComponent { AnchorMin = "0.075 0.15", AnchorMax = $"0.375 0.7", OffsetMin = "6 6", OffsetMax = "-6 -6" },
                }
            });

            container.Add(new CuiElement
            {
                Parent = "Report",
                Components = {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "lamC17G") },
                    new CuiRectTransformComponent { AnchorMin = "0.075 0.15", AnchorMax = $"0.375 0.7", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.075 0.15", AnchorMax = "0.375 0.3", OffsetMax = "0 0" },
                Text = { Text = target.displayName, Color = "1 1 1 0.6", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
            }, "Report");

            float width = 0.55f, height = 0.195f, startxBox = 0.38f, startyBox = 0.72f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in config.Reasons)
            {
                container.Add(new CuiButton
                {  
                    RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMin = "4 4", OffsetMax = "-4 -4" },
                    Button = { Color = "1 1 1 0", Command = $"report reason {target.userID} {check.Key}" },
                    Text = { Text = $"     {check.Value}", Color = "1 1 1 0.4", Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                }, "Report");

                xmin += width;
                if (xmin + width >= 0)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        string LayerCheck = "Target";
        void TargetUI(BasePlayer player, ulong id) {
            CuiHelper.DestroyUi(player, LayerCheck);
            var container = new CuiElementContainer();

            var target = DB.FirstOrDefault(z => z.Key == id);

            var amin = target.Value.SteamID2 == player.userID ? "0.64 0" : "0.38 0.25";
            var amax = target.Value.SteamID2 == player.userID ? "0.84 0.225" : "0.62 0.6";
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = amin, AnchorMax = amax, OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", LayerCheck);

            if (target.Value.SteamID2 != player.userID) {
                container.Add(new CuiElement
                {
                    Parent = LayerCheck,
                    Components = {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "aAm4ZHw"), Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.08 0.86", AnchorMax = "0.87 0.94", OffsetMax = "0 0" },
                    Text = { Text = "Проверка игрока", Color = "1 1 1 0.4", Align = TextAnchor.MiddleLeft, FontSize = 14, Font = "robotocondensed-regular.ttf" }
                }, LayerCheck);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.88 0.775", AnchorMax = "0.945 0.9", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Close = LayerCheck },
                    Text = { Text = "", Color = "0 0 0 0", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
                }, LayerCheck);

                container.Add(new CuiElement
                {
                    Parent = LayerCheck,
                    Components = {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", target.Key.ToString()) },
                        new CuiRectTransformComponent { AnchorMin = "0.075 0.45", AnchorMax = $"0.375 0.81", OffsetMin = "6 6", OffsetMax = "-6 -6" },
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = LayerCheck,
                    Components = {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "lamC17G") },
                        new CuiRectTransformComponent { AnchorMin = "0.075 0.45", AnchorMax = $"0.375 0.81", OffsetMax = "0 0" },
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.075 0.4", AnchorMax = "0.375 0.55", OffsetMax = "0 0" },
                    Text = { Text = target.Value.DisplayName, Color = "1 1 1 0.6", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                }, LayerCheck);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.41 0.6", AnchorMax = "0.915 0.81", OffsetMax = "0 0" },
                    Text = { Text = $"Читы жалоб: {target.Value.Res["Reason_1"]}\nБагоюз жалоб: {target.Value.Res["Reason_2"]}\nМакрос жалоб: {target.Value.Res["Reason_3"]}\nПроверок: {target.Value.Count}", Color = "1 1 1 0.4", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, LayerCheck);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.41 0.5", AnchorMax = "0.915 0.575", OffsetMax = "0 0" },
                    Text = { Text = $"steamid: {target.Key}", Color = "1 1 1 0.4", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, LayerCheck);

                var status = BasePlayer.Find(target.Key.ToString()).IsConnected == true ? "Online" : "Offline";
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.44 0.21", AnchorMax = "0.915 0.48", OffsetMax = "0 0" },
                    Text = { Text = $"Discord: не указан\nСтатус: {status}", Color = "1 1 1 0.4", Align = TextAnchor.MiddleLeft, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, LayerCheck);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.41 0.09", AnchorMax = "0.915 0.2", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"report check {target.Key}" },
                    Text = { Text = "Вызвать на проверку", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "robotocondensed-regular.ttf" }
                }, LayerCheck);

                float width = 0.31f, height = 0.115f, startxBox = 0.07f, startyBox = 0.44f - height, xmin = startxBox, ymin = startyBox;
                foreach (var check in config.Ban)
                {
                    container.Add(new CuiButton
                    {  
                        RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMin = "4 4", OffsetMax = "-4 -4" },
                        Button = { Color = "1 1 1 0", Command = $"report ban {target.Key} {check}" },
                        Text = { Text = $"Бан {check}", Color = "1 1 1 0.4", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                    }, LayerCheck);

                    xmin += width;
                    if (xmin + width >= 0)
                    {
                        xmin = startxBox;
                        ymin -= height + 0.003f;
                    }
                }
            }
            else {
                container.Add(new CuiElement
                {
                    Parent = LayerCheck,
                    Components = {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "xclemZi"), Color = "1 1 1 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.06 0.195", AnchorMax = "0.39 0.81", OffsetMax = "0 0" },
                    Text = { Text = $"Читы жалоб: {target.Value.Res["Reason_1"]}\n\nБагоюз жалоб: {target.Value.Res["Reason_2"]}\n\nМакрос жалоб: {target.Value.Res["Reason_3"]}", Color = "1 1 1 0.4", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, LayerCheck);

                var ds = target.Value.DS != null ? target.Value.DS : "не указан";
                var status = BasePlayer.Find(target.Key.ToString()).IsConnected == true ? "Online" : "Offline";
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.44 0.21", AnchorMax = "0.915 0.8", OffsetMax = "0 0" },
                    Text = { Text = $"Никнейм: {target.Value.DisplayName}\nSteamID: {target.Value.SteamID}\nСтатус: {status}\nDiscord: {ds}\nПроверок: {target.Value.Count}", Color = "1 1 1 0.4", Align = TextAnchor.MiddleLeft, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, LayerCheck);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.05 0.12", AnchorMax = "0.95 0.23", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = $"report check {target.Key}" },
                    Text = { Text = "Снять с проверки", Color = "1 1 1 1", Align = TextAnchor.MiddleCenter, FontSize = 10, Font = "robotocondensed-regular.ttf" }
                }, LayerCheck);

                float width = 0.307f, height = 0.15f, startxBox = 0.04f, startyBox = 0.9f - height, xmin = startxBox, ymin = startyBox;
                foreach (var check in config.Ban)
                {
                    container.Add(new CuiButton
                    {  
                        RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMin = "4 4", OffsetMax = "-4 -4" },
                        Button = { Color = "1 1 1 0", Command = $"report ban {target.Key} {check}" },
                        Text = { Text = $"Бан {check}", Color = "1 1 1 0.4", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 12 }
                    }, LayerCheck);

                    xmin += width;
                    if (xmin + width >= 1)
                    {
                        xmin = startxBox;
                        ymin -= height + 0.004f;
                    }
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void CheckUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Check_UI");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0 0.75", AnchorMax = "1 0.9", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" },
            }, "Overlay", "Check_UI");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"<b><size=20>{player.displayName.ToUpper()}, ВАС ВЫЗВАЛИ НА ПРОВЕРКУ</size></b>\n{config.Title}", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, "Check_UI");

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Дискорд
        List<Fields> DT_PlayerSendReport(BasePlayer Sender, UInt64 TargetID, String Reason)
        {
	        List<Fields> fields = new List<Fields>
	        {
		        new Fields("Получена новая жалоба :", "", false),
		        new Fields("", "", false),
		        new Fields("Информация об отправителе :", "", false),
		        new Fields("", "", false),
		        new Fields("Ник", $"{Sender.displayName}", true),
		        new Fields("Steam64ID", $"[{Sender.userID}](https://steamcommunity.com/profiles/{Sender.userID})", true),
		        new Fields("", "", false),
		        new Fields("Информация о подозреваемом :", "", false),
		        new Fields("", "", false),
		        new Fields("Ник", $"{covalence.Players.FindPlayerById(TargetID.ToString()).Name ?? "EMPTY"}", true),
		        new Fields("Steam64ID", $"[{TargetID}](https://steamcommunity.com/profiles/{TargetID})", true),
		        new Fields("Причина жалобы :", Reason, false),
	        };
	        
	        return fields;
        }
            
        List<Fields> DT_PlayerSendContact(BasePlayer Sender, String Contact)
        {
	        List<Fields> fields = new List<Fields>
	        {
		        new Fields("Информация об отправителе :", "", false),
		        new Fields("", "", false),
		        new Fields("Ник", $"{Sender.displayName}", true),
		        new Fields("Steam64ID", $"[{Sender.userID}](https://steamcommunity.com/profiles/{Sender.userID})", true),
		        new Fields("Контакты для связи :", Contact, false),
	        };

	        return fields;
        }

        List<Fields> DT_PlayerCheck(BasePlayer Sender, UInt64 TargetID)
        {
	        List<Fields> fields = new List<Fields>
	        {
		        new Fields($"Модератор {Sender.displayName} начал проверку:", "", false),
		        new Fields("", "", false),
		        new Fields("Ник проверяемого", $"{covalence.Players.FindPlayerById(TargetID.ToString()).Name ?? "EMPTY"}", true),
		        new Fields("Steam64ID", $"[{TargetID}](https://steamcommunity.com/profiles/{TargetID})", true),
	        };

	        return fields;
        }

        List<Fields> DT_PlayerCheckRemove(BasePlayer Sender, UInt64 TargetID)
        {
	        List<Fields> fields = new List<Fields>
	        {
		        new Fields($"Модератор {Sender.displayName} закончил проверку:", "", false),
		        new Fields("", "", false),
		        new Fields("Ник проверяемого", $"{covalence.Players.FindPlayerById(TargetID.ToString()).Name ?? "EMPTY"}", true),
		        new Fields("Steam64ID", $"[{TargetID}](https://steamcommunity.com/profiles/{TargetID})", true),
	        };

	        return fields;
        }

        void SendDiscord(String Webhook, List<Fields> fields, Authors Authors, Int32 Color)
        {
	        if (Webhook == null || String.IsNullOrWhiteSpace(Webhook)) return;
	        FancyMessage newMessage = new FancyMessage(null, false, new FancyMessage.Embeds[1] { new FancyMessage.Embeds(null, Color, fields, Authors) });

	        Request($"{Webhook}", newMessage.toJSON());
        }

        void Request(String url, String payload, Action<Int32> callback = null)
        {
            Dictionary<String, String> header = new Dictionary<String, String>();
            header.Add("Content-Type", "application/json");
            webrequest.Enqueue(url, payload, (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    if (response != null)
                    {
                        try
                        {
                            JObject json = JObject.Parse(response);
                            if (code == 429)
                            {
                                Single seconds = Single.Parse(Math.Ceiling((Double)(Int32)json["retry_after"] / 1000).ToString());
                            }
                            else
                            {
                                PrintWarning($" Discord rejected that payload! Responded with \"{json["message"].ToString()}\" Code: {code}");
                            }
                        }
                        catch
                        {
                            PrintWarning($"Failed to get a valid response from discord! Error: \"{response}\" Code: {code}");
                        }
                    }
                    else
                    {
                        PrintWarning($"Discord didn't respond (down?) Code: {code}");
                    }
                }
                try
                {
                    callback?.Invoke(code);
                }
                catch (Exception ex) { }

            }, this, Core.Libraries.RequestMethod.POST, header, timeout: 10F);
        }

        public class Fields
        {
            public String name { get; set; }
            public String value { get; set; }
            public bool inline { get; set; }
            public Fields(String name, String value, bool inline)
            {
                this.name = name;
                this.value = value;
                this.inline = inline;
            }
        }

        public class Authors
        {
            public String name { get; set; }
            public String url { get; set; }
            public String icon_url { get; set; }
            public String proxy_icon_url { get; set; }
            public Authors(String name, String url, String icon_url, String proxy_icon_url)
            {
                this.name = name;
                this.url = url;
                this.icon_url = icon_url;
                this.proxy_icon_url = proxy_icon_url;
            }
        }

        public class FancyMessage
        {
            public String content { get; set; }
            public Boolean tts { get; set; }
            public Embeds[] embeds { get; set; }

            public class Embeds
            {
                public String title { get; set; }
                public Int32 color { get; set; }
                public List<Fields> fields { get; set; }
                public Authors author { get; set; }

                public Embeds(String title, Int32 color, List<Fields> fields, Authors author)
                {
                    this.title = title;
                    this.color = color;
                    this.fields = fields;
                    this.author = author;

                }
            }

            public FancyMessage(String content, bool tts, Embeds[] embeds)
            {
                this.content = content;
                this.tts = tts;
                this.embeds = embeds;
            }

            public String toJSON() => JsonConvert.SerializeObject(this);
        }
        #endregion
    }
}