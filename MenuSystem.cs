using System.Collections.Generic;
using System.Linq;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("MenuSystem", "Sempai#3239", "1.0.0")]
    class MenuSystem : RustPlugin
    {
        #region Вар
        string Layer = "Menu";

        [PluginReference] Plugin ImageLibrary, OurServers, KitSystem, ShopSystem, WipeSchedule, Leaderboard;

        string Logo = "https://media.discordapp.net/attachments/845902962471075870/999489494044659803/unknown.png?width=676&height=676";
        string Banner = "https://media.discordapp.net/attachments/845902962471075870/999489783644553297/unknown.png";
        #endregion

        #region Кнопки
        Dictionary<string, string> ButtonMenu = new Dictionary<string, string>()
        {
            ["SERVERS"] = "server",
            ["KITS"] = "kit",
            ["SHOP"] = "shop",
            ["WIPE SCHEDULE"] = "wipe",
            ["LEADBOARD"] = "stat"
        };
        #endregion

        #region Хуки
        void OnServerInitialized()
        {
            ImageLibrary.Call("AddImage", Logo, "LogoImage");
            ImageLibrary.Call("AddImage", Banner, "WelcomeBanner");
            ImageLibrary.Call("AddImage", "https://imgur.com/bn9qREd.png", "123");

            foreach (var check in BasePlayer.activePlayerList)
                OnPlayerConnected(check);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player));
                return;
            }
            MenuUI(player, "welcome");
        }

        void Unload()
        {
            foreach (var check in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(check, Layer);
        }
        #endregion

        #region Команды
        [ChatCommand("menu")]
        void ChatMenu(BasePlayer player) => MenuUI(player);

        [ChatCommand("server")]
        void ChatServer(BasePlayer player) => MenuUI(player, "server");

        [ChatCommand("kit")]
        void ChatKit(BasePlayer player) => MenuUI(player, "kit");

        [ChatCommand("shop")]
        void ChatShop(BasePlayer player) => MenuUI(player, "shop");

        [ChatCommand("wipe")]
        void ChatWipe(BasePlayer player) => MenuUI(player, "wipe");

        [ChatCommand("stat")]
        void ChatStat(BasePlayer player) => MenuUI(player, "stat");

        [ConsoleCommand("menu")]
        void ConsoleMenu(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            UI(player, args.Args[0]);
        }
        #endregion

        #region Интерфейс
        void MenuUI(BasePlayer player, string name = "")
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9", Material = "assets/content/ui/uibackgroundblur.mat" },
            }, "Overlay", Layer);

            

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.108 0.795", AnchorMax = $"0.185 0.92", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Command = "menu welcome" },
                Text = { Text = "" }
            }, Layer, "Logo");

            container.Add(new CuiElement
            {
                Parent = "Logo",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "LogoImage"), FadeIn = 0.5f },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.031 0.7", AnchorMax = $"0.26 0.8", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = $"<color=#ee3e61><size=30>TOP PLUGINS</size></color>\n{BasePlayer.activePlayerList.Count()}/{ConVar.Server.maxplayers}", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = $"0.031 0.045", AnchorMax = $"0.26 0.14", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0.5", Close = Layer },
                Text = { Text = "CLOSE", Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize = 30, Font = "robotocondensed-bold.ttf" }
            }, Layer);

            float width = 0.2f, height = 0.09f, startxBox = 0.044f, startyBox = 0.655f - height, xmin = startxBox, ymin = startyBox;
            foreach (var check in ButtonMenu)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"{xmin} {ymin}", AnchorMax = $"{xmin + width} {ymin + height * 1}", OffsetMin = "2 2", OffsetMax = "-2 -2" },
                    Button = { Color = "1 1 1 0", Command = $"menu {check.Value}" },
                    Text = { Text = check.Key, Color = "1 1 1 0.5", Align = TextAnchor.MiddleCenter, FontSize =30, Font = "robotocondensed-bold.ttf" }
                }, Layer);

                xmin += width;
                if (xmin + width >= 0)
                {
                    xmin = startxBox;
                    ymin -= height;
                }
            }

            CuiHelper.AddUi(player, container);
            UI(player, name);
        }

        void UI(BasePlayer player, string name)
        {
            DestroyUI(player);
            if (name == "welcome")
                WelcomeUI(player);
            if (name == "server")
                OurServers?.Call("UI_DrawInterface", player);
            if (name == "kit")
                KitSystem?.Call("KitUI", player);
            if (name == "shop")
                ShopSystem?.Call("ShopUI", player);
            if (name == "wipe")
                WipeSchedule?.Call("WipeUI", player);
            if (name == "stat")
                Leaderboard?.Call("LeaderboardUI", player);
        }

        void WelcomeUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Welcome");
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = {  AnchorMin = "0.284 0", AnchorMax = "0.952 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.6" },
            }, "Menu", "Welcome");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0.65", AnchorMax = $"1 0.83", OffsetMax = "0 0" },
                Text = { Text = $"WELCOME TO <color=#db8c5a>RUST XYITA</color>\n5X NO BPS SERVER", Align = TextAnchor.MiddleCenter, FontSize = 50, Font = "robotocondensed-regular.ttf" }
            }, "Welcome");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.15 0.4", AnchorMax = $"0.49 0.6", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, "Welcome", "Shop");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.35 0.33", AnchorMax = $"0.65 0.9", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Shop", "ImageShop");

            container.Add(new CuiElement
            {
                Parent = "ImageShop",
                Components =
                {
                    new CuiImageComponent { Sprite = "assets/icons/open.png", FadeIn = 0.5f, Color = "0.86 0.55 0.35 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 10", OffsetMax = "-10 -10" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0.1", AnchorMax = $"1 0.3", OffsetMax = "0 0" },
                Text = { Text = $"SHOP/XYITA", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Shop");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.51 0.4", AnchorMax = $"0.85 0.6", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, "Welcome", "Ds");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.35 0.33", AnchorMax = $"0.65 0.9", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Ds", "ImageDs");

            container.Add(new CuiElement
            {
                Parent = "ImageDs",
                Components =
                {
                    new CuiImageComponent { Sprite = "assets/icons/discord 1.png", FadeIn = 0.5f, Color = "0.86 0.55 0.35 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "10 10", OffsetMax = "-10 -10" }
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = $"0 0.1", AnchorMax = $"1 0.3", OffsetMax = "0 0" },
                Text = { Text = $"DISCORD/XYITA", Align = TextAnchor.MiddleCenter, FontSize = 20, Font = "robotocondensed-regular.ttf" }
            }, "Ds");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0.03 0.2", AnchorMax = $"0.97 0.35", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.5" }
            }, "Welcome", "Banner");

            container.Add(new CuiElement
            {
                Parent = "Banner",
                Components =
                {
                    new CuiRawImageComponent { Png = (string) ImageLibrary.Call("GetImage", "WelcomeBanner"), FadeIn = 0.5f },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Хелпер
        void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Welcome");
            CuiHelper.DestroyUi(player, "UI_OurServersLayer");
            CuiHelper.DestroyUi(player, "Kit_UI");
            CuiHelper.DestroyUi(player, "Shop_UI");
            CuiHelper.DestroyUi(player, "Wipe_UI");
            CuiHelper.DestroyUi(player, "Leaderboard_UI");
        }
        #endregion
    }
}