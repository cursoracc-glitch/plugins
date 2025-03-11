using System.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("BHelp", "https://devplugins.ru/", "1.0.0")]
    public class BHelp : RustPlugin
    {
        #region [Vars]
        Dictionary<string, string> Buttons = new Dictionary<string, string>();

        private string Layer = "BHelp.Layer";
        #endregion

        #region [Oxide]
	    private void OnServerInitialized()
	    {
            PrintWarning("\n-----------------------------\n" +
            "     Author - https://devplugins.ru/\n" +
            "     VK - https://vk.com/dev.plugin\n" +
            "     Discord - https://discord.gg/eHXBY8hyUJ\n" +
            "-----------------------------");
            cmd.AddChatCommand("help", this, "MainUI");
            foreach (var key in config._SettingsHelp)
				Buttons.Add(key.Key, key.Value._Name);
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Layer);
        }
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                config.PluginVersion = Version;
                if (Version == new VersionNumber(1, 0, 0))
                {
                    //
                }

                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        public class SettingsHelp
        {
            [JsonProperty("Название страницы")]
            public string _Name;

            [JsonProperty("Титл страницы")]
            public string _Title;

            [JsonProperty("Текст страницы")]
            public string _Text;
        }

        private class PluginConfig
        {
		    [JsonProperty("Настройка кнопок")]
		    public Dictionary<string, SettingsHelp> _SettingsHelp = new Dictionary<string, SettingsHelp>();

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    _SettingsHelp = new Dictionary<string, SettingsHelp>()
                    {
                        ["server_info"] = new SettingsHelp
                        {
                            _Name = "О сервере",
                            _Title = "Информация о сервере",
                            _Text = "<color=#b7d092>BOLOTO MAX3</color> Это сервер для комфортной игры с командой до 3 игроков\nУ нас не нужно много фармить и убивать на это время! Файты и рейды - главное\nнаправление нашего сервера\n\nВайп сервера происходит каждую пятницу и вторник в <color=#b7d092>16:00</color> по МСК\n\nРейты на добычу ресурсов и компонентов <color=#b7d092>Х5</color>, с наилучшей привилегией <color=#b7d092>Х10</color>.\nКомпоненты улучшены до идеала, никакого мусора в бочках/ящиках.\n\nАктивная администрация/модерация всегда поможет вам в решение каких-либо\nвозникших проблем.\n\nНа сервере установлены уникальные плагин, такие как <color=#b7d092>FIGHTZONE, CARGOZONE,\nМЕГАЯЩИК, Апгрейд карьеров</color>\nБолее подробно о каждом из плагинов вы можете узнать тут - <color=#b7d092>vk.com/bolotorust</color>",
                        },
                        ["rules"] = new SettingsHelp
                        {
                            _Name = "Правила",
                            _Title = "Правила",
                            _Text = "Основные правила игры\n\n<color=#b7d092>1.</color> Запрещено использовать любые виды макросов/читов и всего, что дает\nпреимущество над игроками\n\n<color=#b7d092>2.</color> Нельзя обманывать игроков на любые виды ресурсов.\n\n<color=#b7d092>3.</color> Запрещено использовать баги игры/сервера в любом их проявлении.\n\n<color=#b7d092>4.</color> При нарушении лимита игроков вы получите бан на сервере длительностью от 7\nдней.\n\n<color=#b7d092>5.</color> Запрещено использовать в никах теги чужих серверов.\n\n<color=#b7d092>6.</color> Не знания правил не освобождает вас от ответственности!\n\n<color=#b7d092>7.</color> Играя на нашем сервере вы автоматически соглашаетесь с нашими правилами!\n\nБолее подробно с нашими правилами вы можете ознакомится в нашем дискорде -\n<color=#b7d092>discord.gg/eQnHwZNqrj</color>.",
                        },
                        ["command"] = new SettingsHelp
                        {
                            _Name = "Команды",
                            _Title = "Команды",
                            _Text = "<color=#b7d092>/ad</color> - Автозакрытие дверей\n<color=#b7d092>/rec</color> - Карманный переработчик\n<color=#b7d092>/backpack</color> - Открытие рюкзака\n<color=#b7d092>/fz</color> - Статистика FIGHTZONE\n<color=#b7d092>/tpmenu</color> - Меню телепортации\n<color=#b7d092>/friend</color> - Управление друзьями\n<color=#b7d092>/kit</color> - Открытие меню китов\n<color=#b7d092>/map</color> - Открытие внутриигровой карты\n<color=#b7d092>/remove</color> - Удаление построек\n<color=#b7d092>/up</color> - Автоапгрейд построек\n<color=#b7d092>/hair</color> - Меню прицелов\n<color=#b7d092>/block</color> - Блокировка предметов после вайпа\n<color=#b7d092>/skin</color> - Меню скинов\n<color=#b7d092>/skinentity</color> - Перекрасить скин на уже поставленном предмете (Чтобы сработало, нужно\nсмотреть на установленную, например дверь)\n<color=#b7d092>/raid</color> - Оповещение о рейде в ВК\n<color=#b7d092>/craft</color> - Крафт предметов\n<color=#b7d092>/chat</color> - Настройка чата\n<color=#b7d092>/pinfo</color> - Информация о ваших привилегиях\n<color=#b7d092>/trade</color> - Обмен с другими игроками\n<color=#b7d092>/bps</color> - Проверка ночной защиты\n<color=#b7d092>/report</color> - Жалобы на игроков\n<color=#b7d092>/top</color> - Топ игроков сервера",
                        },
                        ["binds"] = new SettingsHelp
                        {
                            _Name = "Бинды",
                            _Title = "Бинды",
                            _Text = "<color=#b7d092>bind кнопка tp.menu</color> - Меню телепортации\n<color=#b7d092>bind кнопка chat.say /kit</color> - Меню китов\n<color=#b7d092>bind кнопка upgrade.use</color> - Автоапгрейд построек\n<color=#b7d092>bind кнопка remove.use</color> - Ремув построек\n<color=#b7d092>bind кнопка chat.say /map</color> - Открытие внутриигровой карты",
                        },
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion

        #region [ConsoleCommand]
        [ConsoleCommand("UI_HELP")]
        private void cmdBHelp(ConsoleSystem.Arg args)
        {
		    BasePlayer player = args.Player();
		    if (player == null) return;

            switch (args.Args[0])
            {
			    case "open.help":
			    {
				    var _config = config._SettingsHelp[args.Args[1]];
                    if (_config == null) return;

                    MenuButtons(player, args.Args[1]);
                    TextUI(player, _config);
				    break;
			    }
            }
        }
        #endregion

        #region [UI]
        private void MainUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            string FirtHelp = Buttons.FirstOrDefault().Key;
            var _config = config._SettingsHelp[FirtHelp];

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Material = "assets/content/ui/uibackgroundblur.mat", Color = "0 0 0 0.77" },
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0.36 0.33 0.28 0.3", Material = "assets/icons/greyout.mat", Close = Layer }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-360 -230", OffsetMax = "362.5 250" },
                Image = { Color = "0.3773585 0.3755785 0.3755785 0.3407843", Material = "assets/icons/greyout.mat" }
            }, Layer, Layer + ".Menu");

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
            MenuButtons(player, FirtHelp);
            TextUI(player, _config);
        }

        private void MenuButtons(BasePlayer player, string Button = "")
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.2 1" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Menu", Layer + ".Menu" + ".Button");

            int y = 0;
            foreach (var key in Buttons)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.1 {0.85 - y * 0.088}", AnchorMax = $"0.96 {0.92 - y * 0.088}" },
                    Button = { Color = "0 0 0 0.60", Command = $"UI_HELP open.help {key.Key}" },
                    Text = { Text = $"" }
                }, Layer + ".Menu" + ".Button", Layer + ".Menu" + $".Button{y}");

                container.Add(new CuiLabel
                {   
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = $"{key.Value}", Color = "1 1 1 0.85", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 16 }
                }, Layer + ".Menu" + $".Button{y}");

                if (Button == key.Key)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.99 0.03" },
                        Image = { Color = "0.00 0.84 0.47 1.00" }
                    }, Layer + ".Menu" + $".Button{y}");
                }

                y++;
            }

            CuiHelper.DestroyUi(player, Layer + ".Menu" + ".Button");
            CuiHelper.AddUi(player, container);
        }

        private void TextUI(BasePlayer player, SettingsHelp _config)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, Layer + ".Menu", Layer + ".Menu" + ".Text");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.03 0.925", AnchorMax = "0.96 1", OffsetMax = "0 0" },
                Text = { Text = $"{_config._Title}", Color = "1 1 1 0.65", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 22 }
            }, Layer + ".Menu" + ".Text");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.045 0", AnchorMax = "0.96 0.928", OffsetMax = "0 0" },
                Text = { Text = $"{_config._Text}", Color = "1 1 1 0.8", Align = TextAnchor.UpperLeft, FontSize = 14, Font = "robotocondensed-regular.ttf" }
            }, Layer + ".Menu" + ".Text");

            CuiHelper.DestroyUi(player, Layer + ".Menu" + ".Text");
            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}