using System;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using System.Globalization;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Help", "vk.com/rust_fans", "1.0.0")]
    class Help : RustPlugin
    {
        #region Variables
        [JsonProperty("Системный слой")] private string Layer = "UI_Help";
        [PluginReference] private Plugin ImageLibrary;
        #endregion

        #region Command
        [ChatCommand("help")]
        void cmdChatOnes(BasePlayer player, string command, string[] args) => InfoUI(player);

        #region Command
        [ChatCommand("1")]
        void cmdChatOne(BasePlayer player, string command, string[] args) => OneUI(player);

        [ChatCommand("2")]
        void cmdChatTwo(BasePlayer player, string command, string[] args) => TwoUI(player);

        [ChatCommand("3")]
        void cmdChatThree(BasePlayer player, string command, string[] args) => ThreeUI(player);

        [ChatCommand("4")]
        void cmdChatFour(BasePlayer player, string command, string[] args) => FourUI(player);

        [ChatCommand("5")]
        void cmdChatFive(BasePlayer player, string command, string[] args) => FiveUI(player);
        
        [ChatCommand("6")]
        void cmdChatSix(BasePlayer player, string command, string[] args) => SixUI(player);

        [ChatCommand("7")]
        void cmdChatSeven(BasePlayer player, string command, string[] args) => SevenUI(player);

        [ConsoleCommand("10")]
        void cmdConsoleShop(ConsoleSystem.Arg args)
        {
            InfoUI(args.Player());
        }

        #endregion

        void OnServerInitialized()
        {
			ImageLibrary.Call("AddImage", "https://gspics.org/images/2020/08/20/xgAGe.png", "Logo");
        }

        #region Ui
        private void InfoUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.97", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", FadeIn = 0.7f },
                FadeOut = 0.7f
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.28 0.21", AnchorMax = $"1 0.7", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "                 ДОБРО ПОЖАЛОВАТЬ <color=#32C8C8><b>" + player.displayName.ToUpper() + "</b></color>\n\n<color=#ffa987><b>ОПИСАНИЕ СЕРВЕРА:</b></color>\n\n• Администрация 20+\n• Стартовые Киты\n• Рейты X2 на ресурсы и на компоненты X2\n• Временный блок предметов после вайпа\n• Бесплатные оповещение о рейдах\n• Система телепортов, дуэлей и трейды\n• Апгрейд, ремув и рейдблок\n• Карта(/Map) \n• Ивенты и удобнейшее Меню сервера(/Menu) \n• Радиоактивная руда при добыче ресурсов\n• СВО не стреляют в зоне дейсвия Вашего шкафа", Font = "robotocondensed-regular.ttf", FontSize = 20, Color = HexToCuiColor("#e4e4e4ba"), Align = TextAnchor.MiddleLeft, FadeIn = 0.7f }
            }, Layer);


            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = $"0.2 0.85", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0.2", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say 0 /help", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ff8484>О СЕРВЕРЕ</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.7", AnchorMax = $"0.2 0.75", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /1", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ПРАВИЛА", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.6", AnchorMax = $"0.2 0.65", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /2", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОМАНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.5", AnchorMax = $"0.2 0.55", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /3", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "БИНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = $"0.2 0.45", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /4", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "НАБОРЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.38 0.12", AnchorMax = $"0.62 0.17", OffsetMax = "0 0" },
                Button = { Color = "1.00 0.00 1.00 0.05", Material = "assets/content/ui/uibackgroundblur.mat", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ffa987>••• Выход •••</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.3", AnchorMax = $"0.2 0.35", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /5", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ВАЙПЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = $"0.2 0.25", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /6", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОНТАКТЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Logo") },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0.71", AnchorMax = "0.8 0.95", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        #region OneUi
        private void OneUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.97", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", FadeIn = 0.7f },
                FadeOut = 0.7f
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.28 0.05", AnchorMax = $"0.8 0.9", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "                 ДОБРО ПОЖАЛОВАТЬ <color=#32C8C8><b>" + player.displayName.ToUpper() + "</b></color>\n\n<color=#ffa987><b>ПРАВИЛА СЕРВЕРА:</b></color>\n\n1. Запрещено использовать посторонний софт для игры\n2. Запрещено критиковать, обсуждать действия администрации\n3. Запрещено хейтерство, спам, реклама в любом виде\n4. Администрация вправе проверить любого игрока на читы, а также запретить доступ к серверу без объяснения причины\n5. Запрещены баги и обучение им других игроков\n6. Все добровольные пожертвования (донат) направляются на развитие проекта и не подлежат возврату\n7. Полный свод правил в группе сервера ВК и Discord, незнание правил не освобождает от банов на проекте ", Font = "robotocondensed-regular.ttf", FontSize = 20, Color = HexToCuiColor("#FFFFFF5A"), Align = TextAnchor.MiddleLeft, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = $"0.2 0.85", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /help", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "О СЕРВЕРЕ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.7", AnchorMax = $"0.2 0.75", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0.2", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /1", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ff8484>ПРАВИЛА</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.6", AnchorMax = $"0.2 0.65", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /2", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОМАНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.5", AnchorMax = $"0.2 0.55", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /3", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "БИНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = $"0.2 0.45", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /4", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "НАБОРЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.3", AnchorMax = $"0.2 0.35", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /5", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ВАЙПЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = $"0.2 0.25", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /6", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОНТАКТЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.38 0.12", AnchorMax = $"0.62 0.17", OffsetMax = "0 0" },
                Button = { Color = "1.00 0.00 1.00 0.05", Material = "assets/content/ui/uibackgroundblur.mat", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ffa987>••• Выход •••</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Logo") },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0.71", AnchorMax = "0.8 0.95", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        #region TwoUi
        private void TwoUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.97", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", FadeIn = 0.7f },
                FadeOut = 0.7f
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.25 0.47", AnchorMax = $"1 0.8", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "                         ДОБРО ПОЖАЛОВАТЬ <color=#32C8C8><b>" + player.displayName.ToUpper() + "</b></color>\n\n        <color=#ffa987><b>КОМАНДЫ СЕРВЕРА:</b></color>\n", Font = "robotocondensed-regular.ttf", FontSize = 20, Color = HexToCuiColor("#e4e4e4ba"), Align = TextAnchor.MiddleLeft, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
				RectTransform = { AnchorMin = "0.28 0.24", AnchorMax = $"0.5 0.84", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "\n\n\n\n\n\n\n<color=orange>/battle</color> - вызвать на поединок\n<color=orange>/kit</color> - доступные наборы\n<color=orange>/map</color> - карта сервера\n<color=orange>/remove</color> - удаление построек\n<color=orange>/up</color> - улучшение построек\n<color=orange>/store</color> - корзина магазина\n<color=orange>/al</color> - настройка авторизации в замках\n<color=orange>/craft</color> - крафт уникальных предметов", Font = "robotocondensed-regular.ttf", FontSize = 21, Align = TextAnchor.MiddleLeft, Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.24", AnchorMax = $"0.91 0.84", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "\n\n\n\n\n\n\n<color=orange>/rn</color> - уведомления о рейдах\n<color=orange>/trade</color> - обмен вещами\n<color=orange>/tpr</color> - телепорт к игроку\n<color=orange>/pinfo</color> - просмотр своих привилегий\n<color=orange>/home</color> - телепорты домой\n<color=orange>/skin</color> - изменение скинов\n<color=orange>/block</color> - блок предметов\n<color=orange>/hitmarker</color> - вкл/выкл хитмаркер\n<color=orange>/friend</color> - настройка друзей\n<color=orange>/chat</color> - настройка чата", Font = "robotocondensed-regular.ttf", FontSize = 21, Align = TextAnchor.MiddleLeft, Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = $"0.2 0.85", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /help", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "О СЕРВЕРЕ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.7", AnchorMax = $"0.2 0.75", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /1", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ПРАВИЛА", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.6", AnchorMax = $"0.2 0.65", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0.2", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /2", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ff8484>КОМАНДЫ</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.5", AnchorMax = $"0.2 0.55", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /3", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "БИНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = $"0.2 0.45", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /4", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "НАБОРЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.3", AnchorMax = $"0.2 0.35", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /5", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ВАЙПЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = $"0.2 0.25", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /6", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОНТАКТЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.38 0.12", AnchorMax = $"0.62 0.17", OffsetMax = "0 0" },
                Button = { Color = "1.00 0.00 1.00 0.05", Material = "assets/content/ui/uibackgroundblur.mat", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ffa987>••• Выход •••</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Logo") },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0.71", AnchorMax = "0.8 0.95", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        #region ThreeUi
        private void ThreeUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.97", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", FadeIn = 0.7f },
                FadeOut = 0.7f
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.28 0.14", AnchorMax = $"0.8 0.9", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "                ДОБРО ПОЖАЛОВАТЬ <color=#32C8C8><b>" + player.displayName.ToUpper() + "</b></color>\n\n<color=#ffa987><b>БИНДЫ СЕРВЕРА: Команды ввводить в консоль (F1)</b></color>" + "\n\n<color=orange>bind m chat.say /map</color> - открытие карты на М (англ)\n<color=orange>bind x chat.say /menu</color> - открывает на клавишу X меню сервера\n<color=orange>bind z menu.tp</color> - открытие меню тп на клавишу Z (англ)\n<color=orange>bind c menu.friend</color> - открытие меню тп к друзьям на клавишу С (англ)\n<color=orange>bind v menu.friendset</color> - открытие меню настройки друзей на клавишу V (англ)\n<color=orange>bind u menu.up</color> - открытие меню апгрейда на клавишу U (англ)\n<color=orange>bind t menu.trade</color> - открытие меню трейда на клавишу T (англ)", Font = "robotocondensed-regular.ttf", FontSize = 21, Color = HexToCuiColor("#e4e4e4ba"), Align = TextAnchor.MiddleLeft, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = $"1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = $"0.2 0.85", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /help", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "О СЕРВЕРЕ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.7", AnchorMax = $"0.2 0.75", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /1", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ПРАВИЛА", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.6", AnchorMax = $"0.2 0.65", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /2", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОМАНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.5", AnchorMax = $"0.2 0.55", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0.2", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /3", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ff8484>БИНДЫ</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = $"0.2 0.45", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /4", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "НАБОРЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.3", AnchorMax = $"0.2 0.35", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /5", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ВАЙПЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = $"0.2 0.25", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /6", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОНТАКТЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.38 0.12", AnchorMax = $"0.62 0.17", OffsetMax = "0 0" },
                Button = { Color = "1.00 0.00 1.00 0.05", Material = "assets/content/ui/uibackgroundblur.mat", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ffa987>••• Выход •••</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Logo") },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0.71", AnchorMax = "0.8 0.95", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        #region FourUi
        private void FourUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.97", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", FadeIn = 0.7f },
                FadeOut = 0.7f
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.28 0.14", AnchorMax = $"1 0.8", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "                ДОБРО ПОЖАЛОВАТЬ <color=#32C8C8><b>" + player.displayName.ToUpper() + "</b></color>\n\n<color=#ffa987><b>НАБОРЫ СЕРВЕРА:</b></color>\n" + "\n\n<color=orange>/kit start</color> - стартовый набор, поможет в развитии\n<color=orange>/kit hunt</color> - набор охотника\n<color=orange>/kit med</color> - набор первой помощи\n<color=orange>/kit work</color> - набор инструментов для добычи\n<color=orange>/kit food</color> - набор еды\n\n\n<color=#ffa987>Также Вы можете приобрести донат-киты на нашем сайте kuala store link не забыть вставить</color>\n<color=#ffa987>Подробнее (/kit)</color>", Font = "robotocondensed-regular.ttf", FontSize = 21, Color = HexToCuiColor("#e4e4e4ba"), Align = TextAnchor.MiddleLeft, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = $"0.2 0.85", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /help", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "О СЕРВЕРЕ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.7", AnchorMax = $"0.2 0.75", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /1", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ПРАВИЛА", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.6", AnchorMax = $"0.2 0.65", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /2", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОМАНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.5", AnchorMax = $"0.2 0.55", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /3", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "БИНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = $"0.2 0.45", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0.2", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /4", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ff8484>НАБОРЫ</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.3", AnchorMax = $"0.2 0.35", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /5", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ВАЙПЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = $"0.2 0.25", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /6", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОНТАКТЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.38 0.12", AnchorMax = $"0.62 0.17", OffsetMax = "0 0" },
                Button = { Color = "1.00 0.00 1.00 0.05", Material = "assets/content/ui/uibackgroundblur.mat", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ffa987>••• Выход •••</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Logo") },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0.71", AnchorMax = "0.8 0.95", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #region FiveUi
        private void FiveUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.97", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", FadeIn = 0.7f },
                FadeOut = 0.7f
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.28 0.267", AnchorMax = $"1 0.8", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "                ДОБРО ПОЖАЛОВАТЬ <color=#32C8C8><b>" + player.displayName.ToUpper() + "</b></color>\n\n\n\n<color=#ffa987><b>ВАЙПЫ СЕРВЕРА:</b></color>\n" + "\nВайп на сервере каждые 5 дней в 14:00 по МСК. Подробнее (/wipe)\n\nКаждый второй вайп - глобальный (с удалением чертежей)", Font = "robotocondensed-regular.ttf", FontSize = 21, Color = HexToCuiColor("#e4e4e4ba"), Align = TextAnchor.MiddleLeft, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = $"0.2 0.85", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /help", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "О СЕРВЕРЕ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.7", AnchorMax = $"0.2 0.75", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /1", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ПРАВИЛА", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.6", AnchorMax = $"0.2 0.65", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /2", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОМАНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.5", AnchorMax = $"0.2 0.55", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /3", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "БИНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = $"0.2 0.45", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /4", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "НАБОРЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.3", AnchorMax = $"0.2 0.35", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0.2", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /5", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ff8484>ВАЙПЫ</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = $"0.2 0.25", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /6", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОНТАКТЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = $"0.2 0.15", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "", Command = "chat.say /7", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ff8484>ПАСХАЛКА</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 2, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.38 0.12", AnchorMax = $"0.62 0.17", OffsetMax = "0 0" },
                Button = { Color = "1.00 0.00 1.00 0.05", Material = "assets/content/ui/uibackgroundblur.mat", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ffa987>••• Выход •••</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Logo") },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0.71", AnchorMax = "0.8 0.95", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        #region SixUi
        private void SixUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.97", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", FadeIn = 0.7f },
                FadeOut = 0.7f
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.28 0.245", AnchorMax = $"1 0.8", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "                ДОБРО ПОЖАЛОВАТЬ <color=#32C8C8><b>" + player.displayName.ToUpper() + "</b></color>\n\n<color=#ffa987><b>Контакты:</b></color>\n" + "\n<color=orange>Just-Rust.ru</color> - основной сайт там можно купить донат и найти другие полезные ссылки\n<color=orange>vk.com/justrustx</color> - группа вконтакте, новости промокоды и т.п\n<color=orange>tglink.ru/justrust_ru</color> - телеграм канал. Более подробные новости, промокоды и др\n<color=orange>discord.gg/grBSFPW</color> - дискорд канал где вы можете общаться\n<color=orange>vk.com/@justrustx-rules</color> - правила сервера\n<color=orange>37.230.228.87:22222</color> - ip сервера\n<color=orange>ТехПоддержка</color> - по всем вопросам пишите в сообщения группы", Font = "robotocondensed-regular.ttf", FontSize = 21, Color = HexToCuiColor("#e4e4e4ba"), Align = TextAnchor.MiddleLeft, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = $"0.2 0.85", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /help", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "О СЕРВЕРЕ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.7", AnchorMax = $"0.2 0.75", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /1", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ПРАВИЛА", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.6", AnchorMax = $"0.2 0.65", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /2", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОМАНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.5", AnchorMax = $"0.2 0.55", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /3", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "БИНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = $"0.2 0.45", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /4", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "НАБОРЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.3", AnchorMax = $"0.2 0.35", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /5", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ВАЙПЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = $"0.2 0.25", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0.2", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /6", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ff8484>КОНТАКТЫ</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.38 0.12", AnchorMax = $"0.62 0.17", OffsetMax = "0 0" },
                Button = { Color = "1.00 0.00 1.00 0.05", Material = "assets/content/ui/uibackgroundblur.mat", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ffa987>••• Выход •••</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Logo") },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0.71", AnchorMax = "0.8 0.95", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }

        #region SevenUi
        private void SevenUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.97", Sprite = "Assets/Content/UI/UI.Background.Tile.psd", FadeIn = 0.7f },
                FadeOut = 0.7f
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.28 0.2", AnchorMax = $"1 0.7", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "                         ДОБРО ПОЖАЛОВАТЬ <color=#00c1fd85><b>" + player.displayName.ToUpper() + "</b></color>\n\n<color=#ffa987><b>ТЫ НАШЁЛ СЕКРЕТ:</b></color>\n" + "\n Промокод: OlegAndDimaPidor\n \n \n \n <color=#ffa987>HL3 ?</color>", Font = "robotocondensed-regular.ttf", FontSize = 23, Color = HexToCuiColor("#e4e4e4ba"), Align = TextAnchor.MiddleLeft, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.8", AnchorMax = $"0.2 0.85", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /help", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "О СЕРВЕРЕ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.7", AnchorMax = $"0.2 0.75", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /1", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ПРАВИЛА", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.6", AnchorMax = $"0.2 0.65", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /2", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОМАНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.5", AnchorMax = $"0.2 0.55", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /3", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "БИНДЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = $"0.2 0.45", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /4", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "НАБОРЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.3", AnchorMax = $"0.2 0.35", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /5", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "ВАЙПЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.2", AnchorMax = $"0.2 0.25", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /6", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "КОНТАКТЫ", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = $"0.2 0.15", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.40 0.40 0.2", Material = "assets/content/ui/uibackgroundblur.mat", Command = "chat.say /7", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ff8484>Пасхалка</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.38 0.12", AnchorMax = $"0.62 0.17", OffsetMax = "0 0" },
                Button = { Color = "1.00 0.00 1.00 0.05", Material = "assets/content/ui/uibackgroundblur.mat", Close = Layer, FadeIn = 0.7f },
                Text = { Text = "<color=#ffa987>••• Выход •••</color>", Font = "RobotoCondensed-Regular.ttf", FontSize = 25, Align = TextAnchor.MiddleCenter, FadeIn = 0.7f }
            }, Layer);

            container.Add(new CuiElement
            {
                Parent = Layer,
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "Logo") },
                    new CuiRectTransformComponent { AnchorMin = "0.2 0.71", AnchorMax = "0.8 0.95", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion
        #endregion
        #endregion
        #endregion
        #endregion
        #endregion
        #endregion
        #endregion
        #endregion

        #region Oxide
        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }
        #endregion

        #region Helpers
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
        #endregion
    }
}