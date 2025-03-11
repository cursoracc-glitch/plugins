using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WelcomeHelp", "Empty", "0.0.41")]
    public class WelcomeHelp : RustPlugin
    {
        #region Classes

        private class Configs
        {
            public class Interface
            {
                [JsonProperty("Текст заголовка (над выделенным блоком), советую оставить пустым")]
                public string HeaderText = "Text";
                [JsonProperty("Текст в разделителе")]
                public string DelimiterText = "Привет, мы подготивили тебе несколько статей, пожалуйста удели им внимание\n" +
                                              "Некоторые из них являются обязательными для чтения (<color=#DC143C><b>X</b></color>), без них ты <b>не сможешь начать играть</b>";

                [JsonProperty("Надпись в левом нижнем углу")]
                public string LeftDownText = "ГРУППА\n" +
                                             "<b>vk.com/huntpub</b>";
                [JsonProperty("Напись в правом нижнем углу")]
                public string RightDownText = "МАГАЗИН\n" +
                                              "<b>rusthuntstores.gamestores.ru</b>";

                [JsonProperty("Надпись на кнопке")]
                public string ButtonText = "ВЕРНУТЬСЯ В ИГРУ";
            }

            [JsonProperty("Показывает при входе игрока на сервер")]
            public bool ShowOnJoin = true;
            [JsonProperty("Настройки дизайна")]
            public Interface InterfaceSettings = new Interface();
            [JsonProperty("Список возможных страниц")]
            public List<Page> Pages = new List<Page>();

            public static Configs GetNewConf()
            {
                return new Configs
                {
                    Pages = new List<Page>
                    {
                        new Page
                        {
                            DisplayName = "Система BattlePass",
                            Text =
                                "У нас новая, уникальная система BattlePass. <b>Зачем?</b> - спросите вы. Это помогает вам получить больше ресурсов!\n" +
                                "Есть много разных видов фракций для который есть отдельные мисиии! Выполни получи награду! <b>/bp</b>!\n\n" +
                                "Сменить можно в любое время любую фракцию!.",
                            Important = true,
                            ReadTime = 10
                        },
                        new Page
                        {
                            DisplayName = "Другая информация",
                            Text =
                                "В нашей группе много чего нового зайди. <b>https://vk.com/huntpub</b> - Чтобы узнавать больше информации!!\n",
                            Important = false,
                            ReadTime = 0
                        },
                    }
                };
            }
        }

        private class Page
        {
            [JsonProperty("Короткое название в главном меню")]
            public string DisplayName;
            [JsonProperty("Текст в вложенном окне")]
            public string Text;
            [JsonProperty("Обязательно для чтения")]
            public bool Important;
            [JsonProperty("Время обязательного чтения (нельзя свернуть пункт)")]
            public int ReadTime = 10;

            public void Draw(BasePlayer player)
            {
                CuiElementContainer container = new CuiElementContainer();
                CuiHelper.DestroyUi(player, Layer + ".Text");

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" },
                    Image = { FadeIn = 0.3f, Color = "0 0 0 0.9", Material = "assets/content/ui/uibackgroundblur.mat" }
                }, Layer, Layer + ".Text");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                    Text = { FadeIn = 0.3f, Text = $"<size=34>{DisplayName}</size>\n" + Text, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 18 }
                }, Layer + ".Text");

                if (!Important || PlayerInfos[player.userID].ReadArticles.Contains(DisplayName) || ReadTime <= 0)
                {
                    container.Add(new CuiButton
                    {
                        RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100", OffsetMax = "0 0" },
                        Button = { Color = "0 0 0 0", Command = "chat.say /info" },
                        Text = { Text = "" }
                    }, Layer + ".Text");

                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2", OffsetMax = "0 0" },
                        Text = { Text = $"Нажмите в любом месте, чтобы закрыть статью", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 22, Color = "1 1 1 0.4" }
                    }, Layer + ".Text", Layer + ".Notify");
                }
                else ServerMgr.Instance.StartCoroutine(DrawCounter(player));

                if (!PlayerInfos[player.userID].ReadArticles.Contains(DisplayName))
                    PlayerInfos[player.userID].ReadArticles.Add(DisplayName);

                CuiHelper.AddUi(player, container);
            }

            public IEnumerator DrawCounter(BasePlayer player)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Unused2, true);

                CuiElementContainer container = new CuiElementContainer();
                for (int i = 0; i < ReadTime; i++)
                {
                    if (player == null || !player.IsConnected)
                    {
                        player.SetPlayerFlag(BasePlayer.PlayerFlags.Unused2, false);
                        yield return 0;
                    }

                    container.Clear();

                    CuiHelper.DestroyUi(player, Layer + ".Notify");
                    container.Add(new CuiLabel
                    {
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2", OffsetMax = "0 0" },
                        Text = { Text = $"Пожалуйста, уделите этой статье ещё: <b>{ReadTime - i} сек.</b>", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 22, Color = "1 1 1 0.4" }
                    }, Layer + ".Text", Layer + ".Notify");

                    CuiHelper.AddUi(player, container);
                    yield return new WaitForSeconds(1);
                }
                container.Clear();

                CuiHelper.DestroyUi(player, Layer + ".Notify");
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.2", OffsetMax = "0 0" },
                    Text = { Text = $"Нажмите в любом месте, чтобы закрыть статью", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 22, Color = "1 1 1 0.4" }
                }, Layer + ".Text", Layer + ".Notify");

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100", OffsetMax = "0 0" },
                    Button = { Color = "0 0 0 0", Command = "chat.say /info" },
                    Text = { Text = "" }
                }, Layer + ".Text");

                CuiHelper.AddUi(player, container);

                player.SetPlayerFlag(BasePlayer.PlayerFlags.Unused2, false);
                yield return 0;
            }
        }

        private class PlayerInfo
        {
            [JsonProperty("Прочитанные статьи")]
            public HashSet<string> ReadArticles = new HashSet<string>();
        }

        #endregion

        #region Variables

        private static Hash<ulong, PlayerInfo> PlayerInfos = new Hash<ulong, PlayerInfo>();
        private Configs Configuration = new Configs();

        #endregion

        #region Commands

        [ConsoleCommand("UI_WelcomeHelpHandler")]
        private void CmdChatInfoMenu(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null || !args.HasArgs(1)) return;

            switch (args.Args[0].ToLower())
            {
                /*case "switch":
                {
                    if (SkipPlayers.Contains(player.userID)) SkipPlayers.Remove(player.userID);
                    else SkipPlayers.Add(player.userID);

                    CuiHelper.DestroyUi(player, Layer + ".CurrentSwitch");
                    CuiElementContainer container = new CuiElementContainer();

                    if (SkipPlayers.Contains(player.userID))
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 25" },
                            Button = { FadeIn = 1f, Color = "0 0 0 0.107", Material = "", Command = "UI_WelcomeHelpHandler switch" },
                            Text = { FadeIn = 1f, Text = "<color=#DC143C><b>X</b></color> Не показывать при входе в игру", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = "1 1 1 0.4" }
                        }, Layer, Layer + ".CurrentSwitch");
                    }
                    else
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 25" },
                            Button = { FadeIn = 1f, Color = "0 0 0 0.107", Material = "", Command = "UI_WelcomeHelpHandler switch" },
                            Text = { FadeIn = 1f, Text = "<color=#14dc6e>✓</color> Показывать каждый раз при входе в игру", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = "1 1 1 0.4"}
                        }, Layer, Layer + ".CurrentSwitch");
                    }
                    CuiHelper.AddUi(player, container);
                    break; 
                }*/
                case "read":
                    {
                        if (!args.HasArgs(2)) return;
                        int index = 0;
                        if (!int.TryParse(args.Args[1], out index)) return;

                        var cPage = Configuration.Pages.ElementAtOrDefault(index);
                        if (cPage == null) return;

                        if (player.HasPlayerFlag(BasePlayer.PlayerFlags.Unused2)) return;
                        cPage.Draw(player);
                        break;
                    }
            }
        }

        [ChatCommand("info")]
        private void CmdChatInfo(BasePlayer player, string command, string[] args) => UI_DrawInterface(player, false);

        #endregion

        #region Initialization

        private void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/Data"))
                PlayerInfos = Interface.Oxide.DataFileSystem.ReadObject<Hash<ulong, PlayerInfo>>($"{Name}/Data");

            BasePlayer.activePlayerList.ToList().ForEach(p =>
            {
                if (!PlayerInfos.ContainsKey(p.userID))
                    PlayerInfos.Add(p.userID, new PlayerInfo());
            });

            timer.Every(60, SaveData);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/Data", PlayerInfos);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Configuration = Config.ReadObject<Configs>();
                if (Configuration?.InterfaceSettings == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Error reading config, creating one new config!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig()
        {
            Configuration = Configs.GetNewConf();
        }

        protected override void SaveConfig() => Config.WriteObject(Configuration);

        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
            if (!PlayerInfos.ContainsKey(player.userID))
                PlayerInfos.Add(player.userID, new PlayerInfo());

            if (Configuration.ShowOnJoin) UI_DrawInterface(player);
        }

        #endregion

        #region Interface

        private void Unload()
        {
            SaveData();
            BasePlayer.activePlayerList.ToList().ForEach(p =>
            {
                CuiHelper.DestroyUi(p, Layer);
            });
        }

        private const string Layer = "UI_LayerInfo";
        private void UI_DrawInterface(BasePlayer player, bool onJoin = true)
        {
            var playerInfo = PlayerInfos[player.userID];

            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, Layer);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" }
            }, "Overlay", Layer);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.895", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = Configuration.InterfaceSettings.HeaderText, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 38, FadeIn = 1f, Color = "1 1 1 0.1" }
            }, Layer);


            // Верхний делимитер
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.895", AnchorMax = "1 0.895", OffsetMin = "0 -1", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.2" }
            }, Layer);

            // Текст в делимитере
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.755", AnchorMax = "1 0.895", OffsetMax = "0 0" },
                Text = { Text = Configuration.InterfaceSettings.DelimiterText, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 26, Color = "1 1 1 0.8" }
            }, Layer);

            // Нижний делимитер
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0.755", AnchorMax = "1 0.755", OffsetMin = "0 -1", OffsetMax = "0 0" },
                Image = { Color = "1 1 1 0.2" }
            }, Layer);

            // Левый нижний угол
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMax = "400 100" },
                Text = { Text = Configuration.InterfaceSettings.LeftDownText, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 22, Color = "1 1 1 0.3" }
            }, Layer);

            // Правый нижний угол
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-400 0", OffsetMax = "0 100" },
                Text = { Text = Configuration.InterfaceSettings.RightDownText, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 22, Color = "1 1 1 0.3" }
            }, Layer);

            bool shouldRead = false;
            float topPos = (float)Configuration.Pages.Count / 2 * 50;

            foreach (var check in Configuration.Pages.Select((i, t) => new { A = i, B = t }))
            {
                string helpText = "<color=#DC143C><b>X</b></color> ";
                if (playerInfo.ReadArticles.Contains(check.A.DisplayName) || !check.A.Important) helpText = $"<color=#14dc6e>✓</color>";
                else shouldRead = true;

                topPos -= 50;
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.35 0.45", AnchorMax = "1 0.45", OffsetMin = $"0 {topPos}", OffsetMax = $"0 {topPos + 50}" },
                    Button = { Color = $"0 0 0 {0}", Command = $"UI_WelcomeHelpHandler read {check.B}" },
                    Text = { Text = check.A.DisplayName.ToUpper(), Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.MiddleLeft }
                }, Layer);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0.45", AnchorMax = "0.345 0.45", OffsetMin = $"0 {topPos}", OffsetMax = $"0 {topPos + 50}" },
                    Button = { Color = $"0 0 0 {0}", Command = $"UI_WelcomeHelpHandler read {check.B}" },
                    Text = { Text = helpText, Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.MiddleRight }
                }, Layer);
            }

            if (!shouldRead && onJoin && Configuration.Pages.Any(p => p.Important)) return;

            if (shouldRead)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 25" },
                    Button = { Color = "0 0 0 0", Material = "", Command = "UI_WelcomeHelpHandler switch" },
                    Text = { Text = "<color=#DC143C><b>X</b></color> Прочитайте обязательные статьи (помечены крестом)", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = "1 1 1 0.4" }
                }, Layer, Layer + ".CurrentSwitch");
            }
            else
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-200 25", OffsetMax = "200 75" },
                    Button = { Color = "0.968627453107 0.921568632 0.882352948 0.02529412", Material = "", Close = Layer },
                    Text = { Text = Configuration.InterfaceSettings.ButtonText, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 28 }
                }, Layer);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 0", OffsetMax = "0 25" },
                    Button = { Color = "0 0 0 0", Material = "", Command = "UI_WelcomeHelpHandler switch" },
                    Text = { Text = "<color=#14dc6e>✓</color> Вы успешно прочитали статьи, и можете начать игру", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 15, Color = "1 1 1 0.4" }
                }, Layer, Layer + ".CurrentSwitch");
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion
    }
}