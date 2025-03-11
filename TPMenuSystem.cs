using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("TPMenuSystem", "Sempai#3239", "5.0.0")]
    class TPMenuSystem : RustPlugin
    {
        #region Вар
        [PluginReference] Plugin ImageLibrary, TPRulesSystem, TPSkillSystem, TPWipeBlock, TPChat, TPKits, TPCases, TPStatsSystem, TPTeleportation, TPReportSystem, TPSkinMenu, TPBattlePass, GameStoresRUST, TPLotterySystem;

        public string Layer = "Menu_UI";

        Dictionary<ulong, bool> hidden = new Dictionary<ulong, bool>();
        Dictionary<ulong, string> activeButton = new Dictionary<ulong, string>();


        public class Settings {
            [JsonProperty("Название отображаемое в меню")] public string DisplayName;
            [JsonProperty("Выполняемая команда в меню")] public string Command;
            [JsonProperty("Изображение которое будет отображаться на кнопке")] public string Url;
        }
        #endregion

        #region Конфиг
        Configuration config;
        class Configuration 
        {
            [JsonProperty("Изображение беннера в окне с информацией")] public string BannerURL = "https://imgur.com/QqPL0jW.png";
            [JsonProperty("Первый заголовок в окне с информацией")] public string Title1 = "ДОБРО ПОЖАЛОВАТЬ НА RUSTFUN";
            [JsonProperty("Второй заголовок в окне с информацией")] public string Title2 = "ВНИМАНИЕ!";
            [JsonProperty("Первый текст с информацией")] public string Text1 = "Текст заполнитель - это текст, который имеет некоторые характеристики реального письменного текста, но является случайным набором слов или сгенерирован иным образом. Его можно использовать для отображения образца шрифтов, создание текста для тестирования или обхода.";
            [JsonProperty("Второй текст с информацией")] public string Text2 = "Есть много вариантов Lorem Ipsum, но большинство из них имеет не всегда приемлемые модификации, например, юмористические вставки или слова, которые даже отдалённо не напоминают латынь. Если вам нужны Lorem Ipsum для серьёзного проекта, вы наверняка не хотите кокой-нибудь шутки.";
            [JsonProperty("Заголовок донат магазина в окне с информацией")] public string ShopTitle = "ДОНАТ МАГАЗИН";
            [JsonProperty("Ссылка донат магазина в окне с информацией")] public string ShopText = "RUST.GOVNOSTORE.COM";
            [JsonProperty("QRCode изображение донат магазина в окне с информацией")] public string ShopQR = "https://imgur.com/2MruV7D.png";
            [JsonProperty("Заголовок дискорда в окне с информацией")] public string DSTitle = "НАШ ДИСКОРД";
            [JsonProperty("Ссылка на группу в дискорде в окне с информацией")] public string DSText = "RUST.GOVNOSTORE.COM";
            [JsonProperty("QRCode изображение дискорда в окне с информацией")] public string DSQR = "https://imgur.com/2MruV7D.png";
            [JsonProperty("Настройки навигации меню")] public List<Settings> settings;
            public static Configuration GetNewConfig() 
            {
                return new Configuration
                {
                    settings = new List<Settings>()
                    {
                        new Settings {
                            DisplayName = "Правила",
                            Command = "rules",
                            Url = "https://imgur.com/RRJLrbU.png"
                        },
                        new Settings {
                            DisplayName = "Скилы",
                            Command = "skill",
                            Url = "https://imgur.com/RRJLrbU.png"
                        },
                        new Settings {
                            DisplayName = "Вайп блок",
                            Command = "block",
                            Url = "https://imgur.com/Y1ic8fH.png"
                        },
                        new Settings {
                            DisplayName = "Наборы",
                            Command = "kit",
                            Url = "https://imgur.com/X06Pvj9.png"
                        },
                        new Settings {
                            DisplayName = "Кейсы",
                            Command = "case1",
                            Url = "https://imgur.com/IAW5SM6.png"
                        },
                        new Settings {
                            DisplayName = "Статистика",
                            Command = "stat",
                            Url = "https://imgur.com/ONdAIJ4.png"
                        },
                        new Settings {
                            DisplayName = "Телепортация",
                            Command = "teleport",
                            Url = "https://imgur.com/ONdAIJ4.png"
                        },
                        new Settings {
                            DisplayName = "Репорты",
                            Command = "report",
                            Url = "https://imgur.com/ONdAIJ4.png"
                        },
                        new Settings {
                            DisplayName = "Скины",
                            Command = "skin",
                            Url = "https://imgur.com/ONdAIJ4.png"
                        },
                        new Settings {
                            DisplayName = "Лотерея",
                            Command = "lot",
                            Url = "https://imgur.com/ONdAIJ4.png"
                        },
                        new Settings {
                            DisplayName = "Чаты",
                            Command = "Chat",
                            Url = "https://imgur.com/ONdAIJ4.png"
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
                if (config?.settings == null) LoadDefaultConfig();
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
        Dictionary<string, string> imageMenu = new Dictionary<string, string>() {
            ["backgroundhidden"] = "https://rustage.su/img/server/ui/menu_bg_hidden.png",
            ["backgroundshow"] = "https://rustage.su/img/server/ui/menu_bg.png",
            ["backgroundbutton"] = "https://rustage.su/img/server/ui/menu_bg_btn_small.png",
            ["backgroundtextbutton"] = "https://rustage.su/img/server/ui/menu_bg_text.png",
            ["hiddenarrow"] = "https://rustage.su/img/server/ui/hide-arrow.png",
            ["showarrow"] = "https://rustage.su/img/server/ui/show-arrow.png",
            ["textshow"] = "https://rustage.su/img/server/ui/menu_text_show.png",
            ["activeButton"] = "https://rustage.su/img/server/ui/menu_active_button.png",
            ["foninfo"] = "https://rustage.su/img/server/ui/mainscreen.png",
            ["commandinfo"] = "https://rustage.su/img/server/ui/modal_window.png",
        };
        void OnServerInitialized()
        {
            foreach (var check in imageMenu) {
                ImageLibrary.Call("AddImage", check.Value, check.Value);
            }
            foreach (var check in config.settings)
                ImageLibrary.Call("AddImage", check.Url, check.Url);

            ImageLibrary.Call("AddImage", config.BannerURL, "banner");
            ImageLibrary.Call("AddImage", config.ShopQR, "shopqr");
            ImageLibrary.Call("AddImage", config.DSQR, "dsqr");

            foreach (var check in BasePlayer.activePlayerList) 
                OnPlayerConnected(check);
        }

        void OnPlayerConnected(BasePlayer player) {
            if (!hidden.ContainsKey(player.userID))
                hidden[player.userID] = true;

            if (!activeButton.ContainsKey(player.userID))
                activeButton[player.userID] = "info";
        }
        #endregion

        #region Команды
        [ChatCommand("menu")]
        void ChatMenu(BasePlayer player) => MenuUI(player);

        [ChatCommand("info")]
        void ChatInfo(BasePlayer player) => MenuUI(player, "info");

        [ChatCommand("skill")]
        void ChatSkill(BasePlayer player) => MenuUI(player, "skill");

        [ChatCommand("block")]
        void ChatBlock(BasePlayer player) => MenuUI(player, "block");

        [ChatCommand("kits")]
        void ChatKit(BasePlayer player) => MenuUI(player, "kit");
                
        [ChatCommand("case")]
        void ChatCase(BasePlayer player) => MenuUI(player, "case");

        [ChatCommand("stat")]
        void ChatStat(BasePlayer player) => MenuUI(player, "stat");

        [ChatCommand("tpmenu")]
        void ChatTeleport(BasePlayer player) => MenuUI(player, "teleport");

        [ChatCommand("report")]
        void ChatReport(BasePlayer player) => MenuUI(player, "report");

        [ChatCommand("Chat")]
        void ChatChat(BasePlayer player) => MenuUI(player, "Chat");

        [ChatCommand("skin")]
        void ChatSkin(BasePlayer player) => MenuUI(player, "skin");
        
        [ChatCommand("lot")]
        void ChatLot(BasePlayer player) => MenuUI(player, "lot");

        [ConsoleCommand("hidden_menu")]
        void Hidden(ConsoleSystem.Arg args) {
            var player = args.Player();
            var hide = hidden[player.userID] == true ? false : true;
            hidden[player.userID] = hide;
            ButtonUI(player);
            MenuUI(player);
        }

        [ConsoleCommand("menu")]
        void ConsoleMenu(ConsoleSystem.Arg args) {
            var player = args.Player();
            activeButton[player.userID] = args.Args[0];
            ButtonUI(player);
            UI(player, args.Args[0]);
        }
        #endregion

        #region Интерфейс
        void MenuUI(BasePlayer player, string name = "")
        {
            if (name != "")
                activeButton[player.userID] = name;
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0.1", Material = "assets/content/ui/uibackgroundblur.mat", Sprite = "assets/content/ui/ui.background.transparent.radial.psd" }
            }, "Overlay", Layer);

            var anchormin = hidden[player.userID] == true ? "0.233" : "0.283";
            var anchormax = hidden[player.userID] == true ? "0.8" : "0.85";
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"{anchormin} 0.2", AnchorMax = $"{anchormax} 0.8", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" }
            }, Layer, ".Mains");

            CuiHelper.AddUi(player, container);
            ButtonUI(player);

            var command = name != "" ? name : activeButton[player.userID];
            UI(player, command);
        }

        void UI(BasePlayer player, string name) {
            CuiHelper.DestroyUi(player, "lay" + ".Main");
            CuiHelper.DestroyUi(player, "Rules_UI" + ".Main");
            CuiHelper.DestroyUi(player, "MainStats" + ".Main");
            CuiHelper.DestroyUi(player, "ui.kits" + ".Main");
            CuiHelper.DestroyUi(player, "TPMENULAYER");
            CuiHelper.DestroyUi(player, "TPMENULAYER1");
            CuiHelper.DestroyUi(player, "TPMENULAYER2");
            if (name == "info") {
                InfoUI(player);
            }
            if (name == "rules") {
                TPRulesSystem?.Call("RulesUI", player);
            }
            if (name == "skill") {
                TPSkillSystem?.Call("ShowMainUI", player);
            }
            if (name == "block") {
                TPWipeBlock?.Call("BlockUi", player);
            }
            if (name == "kit") {
                TPKits?.Call("InitilizeUI", player);
            }
            if (name == "case") {
                TPCases?.Call("DrawGui", player);
            }
            if (name == "stat") {
                TPStatsSystem?.Call("PlayerTopInfo", player, player.userID);
            }
            if (name == "teleport") {
                TPTeleportation?.Call("DDrawMenu", player);
            }
            if (name == "report") {
                TPReportSystem?.Call("ReportUI", player);
            }
            if (name == "Chat") {
                TPChat?.Call("InitializeInterface", player);
            }

            if (name == "skin") {
                TPSkinMenu?.Call("GUI", player);
            }
            if (name == "pass") {
                TPBattlePass?.Call("ShowUIMain", player, 0);
            }
            if (name == "store") {
                GameStoresRUST?.Call("InitializeStore", player, 0);
            }
            if (name == "lot") {
                TPLotterySystem?.Call("LotteryUI", player, 0);
            }
        }

        void ButtonUI(BasePlayer player) {
            CuiHelper.DestroyUi(player, Layer + ".Main");
            var container = new CuiElementContainer();

            var imagehidden = hidden[player.userID] == true ? imageMenu["backgroundhidden"] : imageMenu["backgroundshow"];
            var anchorhiddenmin = hidden[player.userID] == true ? "0.19" : "0.135";
            var anchorhiddenmax = hidden[player.userID] == true ? "0.22" : "0.27";
            container.Add(new CuiElement
            {
                Name = Layer + ".Main",
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", imagehidden) },
                    new CuiRectTransformComponent { AnchorMin = $"{anchorhiddenmin} 0.19", AnchorMax = $"{anchorhiddenmax} 0.814", OffsetMax = "0 0" }
                }
            });

            var imagebgarrow = hidden[player.userID] == true ? imageMenu["backgroundbutton"] : imageMenu["textshow"];
            var anchorbgarrowmin = hidden[player.userID] == true ? "0.15" : "0.04";
            var anchorbgarrowmax = hidden[player.userID] == true ? "0.85" : "0.96";
            container.Add(new CuiElement
            {
                Name = "Hidden",
                Parent = Layer + ".Main",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", imagebgarrow) },
                    new CuiRectTransformComponent { AnchorMin = $"{anchorbgarrowmin} 0.015", AnchorMax = $"{anchorbgarrowmax} 0.075", OffsetMax = "0 0" }
                }
            });

            var imagearrow = hidden[player.userID] == true ? imageMenu["hiddenarrow"] : imageMenu["showarrow"];
            var anchorarrow = hidden[player.userID] == true ? "1" : "0.18";
            container.Add(new CuiElement
            {
                Parent = "Hidden",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", imagearrow)},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"{anchorarrow} 1", OffsetMin = "9.2 9.2", OffsetMax = "-9.2 -9.2" }
                }
            });

            if (hidden[player.userID] == false) {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.19 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = "Свернуть", Color = "1 1 1 0.4", FontSize = 12, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf" }
                }, "Hidden");
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Command = "hidden_menu", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "Hidden");

            var anchorbutton = hidden[player.userID] == true ? 1f : 0.225f;
            float width = anchorbutton, height = 0.075f, startxBox = 0.005f, startyBox = 0.99f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in config.settings)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = xmin + " " + ymin, AnchorMax = (xmin + width) + " " + (ymin + height * 1), OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0", Command = $"menu {check.Command}" },
                    Text = { Text = "" }
                }, Layer + ".Main", "Button");

                container.Add(new CuiElement
                {
                    Name = "ButtonImage",
                    Parent = "Button",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", imageMenu["backgroundbutton"]) },
                        new CuiRectTransformComponent { AnchorMin = "0.15 0.1", AnchorMax = "0.85 0.9", OffsetMax = "0 0" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = "ButtonImage",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", check.Url) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "9.2 9.2", OffsetMax = "-9.2 -9.2" }
                    }
                });

                var color = activeButton[player.userID] == check.Command ? "1 1 1 1" : "0 0 0 0";
                container.Add(new CuiElement
                {
                    Parent = "ButtonImage",
                    Components =
                    {
                        new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", imageMenu["activeButton"]), Color = color },
                        new CuiRectTransformComponent { AnchorMin = "-0.15 0.2", AnchorMax = "-0.06 0.8", OffsetMax = "0 0" }
                    }
                });

                if (hidden[player.userID] == false) {
                    container.Add(new CuiElement
                    {
                        Name = "Button" + "Text",
                        Parent = "Button",
                        Components =
                        {
                            new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", imageMenu["backgroundtextbutton"]) },
                            new CuiRectTransformComponent { AnchorMin = "0.7 0", AnchorMax = "4.45 1", OffsetMin = "9.2 3.9", OffsetMax = "-9.2 -3.9" }
                        }
                    });

                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "0.08 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = $"menu {check.Command}" },
                        Text = { Text = check.DisplayName, Color = "1 1 1 0.4", FontSize = 12, Align = TextAnchor.MiddleLeft, Font = "robotocondensed-regular.ttf" }
                    }, "Button" + "Text");
                }

                xmin += width;
                if (xmin + width >= 0)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        void InfoUI(BasePlayer player) {
            var container = new CuiElementContainer();

            container.Add(new CuiElement
            {
                Name = "lay" + ".Main",
                Parent = ".Mains",
                Components = 
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", imageMenu["foninfo"]) },
                    new CuiRectTransformComponent { AnchorMin = "-0.315 -0.27", AnchorMax = "1.3 1.275", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8 0.804", AnchorMax = "0.817 0.832" },
                Button = { Close = "Menu_UI", Color = "0 0 0 0" },
                Text = { Text = "" }
            }, "lay" + ".Main");

            container.Add(new CuiElement
            {
                Parent = "lay" + ".Main",
                Components = 
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "banner") },
                    new CuiRectTransformComponent { AnchorMin = "0.244 0.615", AnchorMax = "0.76 0.79", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.406 0.554", AnchorMax = "0.5967 0.592", OffsetMax = "0 0" },
                Text = { Text = config.Title1, Color = "1 1 1 0.4", FontSize = 13, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" }
            }, "lay" + ".Main");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.25 0.47", AnchorMax = "0.755 0.55", OffsetMax = "0 0" },
                Text = { Text = config.Text1, Color = "1 1 1 0.3", FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
            }, "lay" + ".Main");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.45 0.43", AnchorMax = "0.5505 0.468", OffsetMax = "0 0" },
                Text = { Text = config.Title2, Color = "1 1 1 0.4", FontSize = 13, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" }
            }, "lay" + ".Main");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.25 0.35", AnchorMax = "0.755 0.425", OffsetMax = "0 0" },
                Text = { Text = config.Text2, Color = "1 1 1 0.4", FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf" }
            }, "lay" + ".Main");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.3285 0.27", AnchorMax = "0.429 0.308", OffsetMax = "0 0" },
                Text = { Text = config.ShopTitle, Color = "1 1 1 0.4", FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" }
            }, "lay" + ".Main");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3285 0.19", AnchorMax = "0.45 0.265", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0" }
            }, "lay" + ".Main", "ShopText");

            container.Add(new CuiElement
            {
                Parent = "ShopText",
                Components =
                {
                    new CuiInputFieldComponent { Text = config.ShopText, Color = "1 1 1 0.3", Align = TextAnchor.UpperCenter, FontSize = 12, Font = "robotocondensed-bold.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "lay" + ".Main",
                Components = 
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "shopqr") },
                    new CuiRectTransformComponent { AnchorMin = "0.259 0.203", AnchorMax = "0.3148 0.299", OffsetMax = "0 0" },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.59 0.27", AnchorMax = "0.69 0.308", OffsetMax = "0 0" },
                Text = { Text = config.DSTitle, Color = "1 1 1 0.4", FontSize = 12, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" }
            }, "lay" + ".Main");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.59 0.19", AnchorMax = "0.708 0.265", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0" }
            }, "lay" + ".Main", "DSText");

            container.Add(new CuiElement
            {
                Parent = "DSText",
                Components =
                {
                    new CuiInputFieldComponent { Text = config.DSText, Color = "1 1 1 0.3", Align = TextAnchor.UpperCenter, FontSize = 12, Font = "robotocondensed-bold.ttf"},
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

            container.Add(new CuiElement
            {
                Parent = "lay" + ".Main",
                Components = 
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.Call("GetImage", "dsqr") },
                    new CuiRectTransformComponent { AnchorMin = "0.521 0.203", AnchorMax = "0.576 0.299", OffsetMax = "0 0" },
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion 
    }
}