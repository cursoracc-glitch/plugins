using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CheckPlayers", "SkiTles", "0.2")]
    class CheckPlayers : RustPlugin
    {
        //Данный плагин принадлежит группе vk.com/vkbotrust
        //Данный плагин предоставляется в существующей форме,
        //"как есть", без каких бы то ни было явных или
        //подразумеваемых гарантий, разработчик не несет
        //ответственность в случае его неправильного использования.

        #region Vars
        [PluginReference]
        Plugin VKBot;
        private Dictionary<BasePlayer, BasePlayer> PlayersCheckList = new Dictionary<BasePlayer, BasePlayer>(); //moder, target
        private BasePlayer CheckCMDPlayer = null;
        #endregion

        #region Config
        private ConfigData config;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Основные настройки")]
            public MainSettings mainSet { get; set; }
            public class MainSettings
            {
                [JsonProperty(PropertyName = "Привилегия для команд вызова на проверку/завершения проверки")]
                [DefaultValue("vkbot.checkplayers")]
                public string PlCheckPerm { get; set; } = "checkplayers.use";
                [JsonProperty(PropertyName = "Текст уведомления")]
                [DefaultValue("<color=#990404>Модератор вызвал вас на проверку.</color> \nНапишите свой скайп с помощью команды <color=#990404>/skype <НИК в СКАЙПЕ>.</color>\nЕсли вы покините сервер, Вы будете забанены на нашем проекте серверов.")]
                public string PlCheckText { get; set; } = "<color=#990404>Модератор вызвал вас на проверку.</color> \nНапишите свой скайп с помощью команды <color=#990404>/skype <НИК в СКАЙПЕ>.</color>\nЕсли вы покините сервер, Вы будете забанены на нашем проекте серверов.";
                [JsonProperty(PropertyName = "Бан игрока при выходе с сервера во время проверки")]
                [DefaultValue(false)]
                public bool AutoBan { get; set; } = false;
                [JsonProperty(PropertyName = "Кастомная команда для автобана (оставить none если не нужно). Пример: banid {steamid} {reason} 4d")]
                [DefaultValue("none")]
                public string BanCmd { get; set; } = "none";
                [JsonProperty(PropertyName = "Позиция GUI AnchorMin (дефолт 0 0.826)")]
                [DefaultValue("0 0.826")]
                public string GUIAnchorMin { get; set; } = "0 0.826";
                [JsonProperty(PropertyName = "Позиция GUI AnchorMax (дефолт 1 0.965)")]
                [DefaultValue("1 0.965")]
                public string GUIAnchorMax { get; set; } = "1 0.965";
                [JsonProperty(PropertyName = "Команда вызова игрока на проверку")]
                [DefaultValue("alert")]
                public string CMDalert { get; set; } = "alert";
                [JsonProperty(PropertyName = "Команда завершения проверки игрока")]
                [DefaultValue("unalert")]
                public string CMDunalert { get; set; } = "unalert";
                [JsonProperty(PropertyName = "Команда отправки скайпа модератору")]
                [DefaultValue("skype")]
                public string CMDskype { get; set; } = "skype";
                [JsonProperty(PropertyName = "Отправка скайпа админу в ВК (при вызове на проверку консольной командой)")]
                [DefaultValue(false)]
                public bool admMsg { get; set; } = false;
                [JsonProperty(PropertyName = "Отправка скайпа в беседу ВК (при вызове на проверку консольной командой)")]
                [DefaultValue(false)]
                public bool admChat { get; set; } = false;
            }
        }
        private void LoadVariables()
        {
            bool changed = false;
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            config = Config.ReadObject<ConfigData>();
            if (config.mainSet == null) { config.mainSet = new ConfigData.MainSettings(); changed = true; }
            Config.WriteObject(config, true);
            if (changed) PrintWarning("Конфигурационный файл обновлен.");
        }
        protected override void LoadDefaultConfig()
        {
            var configData = new ConfigData { mainSet = new ConfigData.MainSettings() };
            Config.WriteObject(configData, true);
            PrintWarning("Поддержи разработчика! Вступи в группу vk.com/vkbotrust");
        }
        #endregion

        #region OxideHooks
        void OnServerInitialized()
        {
            LoadVariables();
            if (!permission.PermissionExists(config.mainSet.PlCheckPerm)) permission.RegisterPermission(config.mainSet.PlCheckPerm, this);
            cmd.AddChatCommand(config.mainSet.CMDalert, this, "CheckPlayerChat");
            cmd.AddChatCommand(config.mainSet.CMDunalert, this, "UnCheckPlayerChat");
            cmd.AddChatCommand(config.mainSet.CMDskype, this, "SendSkype");
            cmd.AddConsoleCommand(config.mainSet.CMDalert, this, "CheckPlayerCMD");
            cmd.AddConsoleCommand(config.mainSet.CMDunalert, this, "StopCheckPlayerCMD");
        }
        void Loaded() => LoadMessages();
        void Unload() => UnloadAllGUI();
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (CheckCMDPlayer == player)
            {
                CheckCMDPlayer = null;
                PrintWarning($"Игрок {player.displayName} отключился во время проверки. Причина: {reason}.");
                if (config.mainSet.AutoBan && reason == "Disconnected")
                {
                    if (config.mainSet.BanCmd == "none") { player.IPlayer.Ban("Отказ от проверки"); }
                    else
                    {
                        string cmd = config.mainSet.BanCmd.Replace("{steamid}", player.UserIDString);
                        cmd = cmd.Replace("{reason}", "ОтказОтПроверки");
                        rust.RunServerCommand(cmd);
                    }
                    if (player.IsConnected) { player.Kick("banned"); }
                }
            }
            if (PlayersCheckList.ContainsValue(player))
            {
                CuiHelper.DestroyUi(player, "AlertGUI");
                foreach (var check in PlayersCheckList.Keys)
                {
                    if (PlayersCheckList[check] == player)
                    {
                        PlayersCheckList.Remove(check);
                        if (BasePlayer.activePlayerList.Contains(check)) { PrintToChat(check, string.Format(GetMsg("ИгрокОтключился", player))); }
                    }
                }
                if (config.mainSet.AutoBan && reason == "Disconnected")
                {
                    if (config.mainSet.BanCmd == "none") { player.IPlayer.Ban("Отказ от проверки"); }
                    else
                    {
                        string cmd = config.mainSet.BanCmd.Replace("{steamid}", player.UserIDString);
                        cmd = cmd.Replace("{reason}", "ОтказОтПроверки");
                        rust.RunServerCommand(cmd);
                    }
                    if (player.IsConnected) { player.Kick("banned"); }
                }
            }
            if (PlayersCheckList.ContainsKey(player))
            {
                var target = PlayersCheckList[player];
                if (BasePlayer.activePlayerList.Contains(target)) { CuiHelper.DestroyUi(target, "AlertGUI"); PrintToChat(target, string.Format(GetMsg("МодераторОтключился", player))); }
                PlayersCheckList.Remove(player);
            }
        }
        #endregion

        #region Commands
        private void CheckPlayerChat(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), config.mainSet.PlCheckPerm)) { PrintToChat(player, string.Format(GetMsg("НетПрав", player))); return; }
            if (!VKBot) { PrintToChat(player, "Плагин VKBot не установлен!"); return; }
            if (PlayersCheckList.ContainsKey(player)) { PrintToChat(player, string.Format(GetMsg("ПроверкаНеЗакончена", player), config.mainSet.CMDunalert)); return; }
            if (PlayersCheckList.ContainsValue(player)) { PrintToChat(player, string.Format(GetMsg("ВыНаПроверке", player))); return; }
            if (CheckCMDPlayer == player) { PrintToChat(player, string.Format(GetMsg("ВыНаПроверке", player))); return; }
            PListUI(player);
        }
        private void UnCheckPlayerChat(BasePlayer player, string cmd, string[] args)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), config.mainSet.PlCheckPerm)) { PrintToChat(player, string.Format(GetMsg("НетПрав", player))); return; }
            if (!PlayersCheckList.ContainsKey(player)) { PrintToChat(player, string.Format(GetMsg("НетПроверок", player))); return; }
            var target = PlayersCheckList[player];
            if (!BasePlayer.activePlayerList.Contains(target)) return;
            CuiHelper.DestroyUi(target, "AlertGUI");
            PrintToChat(player, string.Format(GetMsg("ПроверкаЗакончена", player), target.displayName));
            PlayersCheckList.Remove(player);
        }
        private void SendSkype(BasePlayer player, string cmd, string[] args)
        {
            if (CheckCMDPlayer != player && !PlayersCheckList.ContainsValue(player)) { PrintToChat(player, string.Format(GetMsg("НеНаПроверке", player))); return; }
            if (args.Length < 1) { PrintToChat(player, string.Format(GetMsg("КомандаСкайп", player), config.mainSet.CMDskype)); return; }
            string contact = args[0];
            string text = string.Format(GetMsg("СообщениеМодератору", player), player.displayName, player.UserIDString, contact);
            if (CheckCMDPlayer == player)
            {
                PrintWarning(text);
                if (config.mainSet.admChat) VKBot?.Call("VKAPIChatMsg", text);
                if (config.mainSet.admMsg)
                {
                    var reciverID = (string)VKBot?.Call("AdminVkID");
                    if (reciverID != null) VKBot?.Call("VKAPIMsg", text, "", reciverID, false);
                }
            }
            if (PlayersCheckList.ContainsValue(player))
            {
                ulong moderid = 0;
                foreach (var moder in PlayersCheckList.Keys)
                {
                    if (PlayersCheckList[moder] == player) moderid = moder.userID;
                }
                if (moderid == 0) return;
                var userVK = (string)VKBot?.Call("GetUserVKId", moderid);
                if (userVK == null) return;
                VKBot?.Call("VKAPIMsg", text, "", userVK, false);
            }
        }
        private void CheckPlayerCMD(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            if (CheckCMDPlayer != null) { PrintWarning($"Сначала закончите предыдущую проверку командой /{config.mainSet.CMDunalert}"); return; }
            if (arg.Args.Length < 1) { PrintWarning($"Команда вызова на проверку: {config.mainSet.CMDalert} steamid/name"); return; }
            var targets = FindPlayersOnline(arg.Args[0]);
            if (targets.Count <= 0) { PrintWarning($"Не удалось найти игрока {arg.Args[0]}"); return; }
            if (targets.Count > 1) { PrintWarning($"Найдено несколько игроков:"); Puts(string.Join(", ", targets.ConvertAll(p => p.displayName).ToArray())); return; }
            var target = targets[0];
            if (PlayersCheckList.ContainsValue(target)) { PrintWarning("Игрок уже на проверке."); return; }
            if (PlayersCheckList.ContainsKey(target)) { PrintWarning("Этот игрок выполняет проверку, вы не можете сейчас его вызвать."); return; }
            CheckCMDPlayer = target;
            StartGUI(target);
            PrintWarning($"Вы вызвали игрока {target.displayName} на проверку.");
        }
        private void StopCheckPlayerCMD(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            if (CheckCMDPlayer == null) { PrintWarning("Нет игроков на проверке."); return; }
            if (BasePlayer.activePlayerList.Contains(CheckCMDPlayer)) { CuiHelper.DestroyUi(CheckCMDPlayer, "AlertGUI"); PrintWarning($"Вы закончили проверку игрока {CheckCMDPlayer.displayName}"); CheckCMDPlayer = null; }
        }
        [ConsoleCommand("checkplayers.gui")]
        private void PListCMD(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            if (!permission.UserHasPermission(player.userID.ToString(), config.mainSet.PlCheckPerm)) { PrintToChat(player, string.Format(GetMsg("НетПрав", player))); return; }
            if (arg.Args == null) return;
            switch (arg.Args[0])
            {
                case "close":
                    CuiHelper.DestroyUi(player, "PListGUI");
                    break;
                case "alert":
                    ulong targetid = 0;
                    if (!ulong.TryParse(arg.Args[1], out targetid)) { PrintToChat(player, string.Format(GetMsg("ИгрокНеНайден", player))); return; }
                    var target = BasePlayer.FindByID(targetid);
                    if (target == null) { PrintToChat(player, string.Format(GetMsg("ИгрокНеНайден", player))); return; }
                    StartCheck(player, target);
                    break;
                case "page":
                    int page;
                    if (arg.Args.Length < 3) return;
                    if (!Int32.TryParse(arg.Args[1], out page)) return;
                    GUIManager.Get(player).Page = page;
                    CuiHelper.DestroyUi(player, "PListGUI");
                    PListUI(player);
                    break;
            }
        }
        #endregion

        #region GUIBuilder
        private CuiElement Panel(string name, string color, string anMin, string anMax, string parent = "Hud", bool cursor = false)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiImageComponent { Color = color },
                    new CuiRectTransformComponent { AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            if (cursor)
            {
                Element.Components.Add(new CuiNeedsCursorComponent());
            }
            return Element;
        }
        private CuiElement Text(string parent, string color, string text, TextAnchor pos, int fsize, string anMin = "0 0", string anMax = "1 1", string fname = "robotocondensed-bold.ttf")
        {
            var Element = new CuiElement()
            {
                Parent = parent,
                Components =
                {
                    new CuiTextComponent() { Color = color, Text = text, Align = pos, Font = fname, FontSize = fsize },
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private CuiElement Button(string name, string parent, string command, string color, string anMin, string anMax)
        {
            var Element = new CuiElement()
            {
                Name = name,
                Parent = parent,
                Components =
                {
                    new CuiButtonComponent { Command = command, Color = color},
                    new CuiRectTransformComponent{ AnchorMin = anMin, AnchorMax = anMax }
                }
            };
            return Element;
        }
        private void UnloadAllGUI()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "PListGUI");
                CuiHelper.DestroyUi(player, "AlertGUI");
            }
        }
        #endregion

        #region AlertPlayerGUI
        private void StartGUI(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            container.Add(Panel("AlertGUI", "0.0 0.0 0 0.35", config.mainSet.GUIAnchorMin, config.mainSet.GUIAnchorMax));
            container.Add(Text("AlertGUI", "1 1 1 1", String.Format(config.mainSet.PlCheckText), TextAnchor.MiddleCenter, 23));
            CuiHelper.AddUi(player, container);
        }
        private void StopGui(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "AlertGUI");
        }
        #endregion

        #region PlayersListGUI        
        private void PListUI(BasePlayer player)
        {
            string text = "Выберите игрока которого хотите вызвать на проверку.";
            List<BasePlayer> players = new List<BasePlayer>();
            foreach (var pl in BasePlayer.activePlayerList)
            {
                if (pl == player) continue;
                players.Add(pl);
            }
            if (players.Count == 0) { PrintToChat(player, "На сервере нет никого кроме вас."); return; }
            players = players.OrderBy(x => x.displayName).ToList();
            int maxPages = CalculatePages(players.Count);
            string pageNum = (maxPages > 1) ? $" - {GUIManager.Get(player).Page}" : "";
            CuiElementContainer container = new CuiElementContainer();
            container.Add(Panel("PListGUI", "0 0 0 0.75", "0.2 0.125", "0.8 0.9", "Hud", true));
            container.Add(Panel("header", "0 0 0 0.75", "0 0.93", "1 1", "PListGUI"));
            if (maxPages != 1) text = text + " Страница " + pageNum.ToString();
            container.Add(Text("header", "1 1 1 1", text, TextAnchor.MiddleCenter, 20));
            container.Add(Button("close", "header", "checkplayers.gui close", "1 0 0 1", "0.94 0.01", "1.0 0.98"));
            container.Add(Text("close", "1 1 1 1", "X", TextAnchor.MiddleCenter, 20));
            container.Add(Panel("playerslist", "0 0 0 0.75", "0 0", "1 0.9", "PListGUI"));
            var page = GUIManager.Get(player).Page;
            int playerCount = (page * 100) - 100;
            for (int j = 0; j < 20; j++)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (players.ToArray().Length <= playerCount) continue;
                    string AnchorMin = (0.2f * i).ToString() + " " + (1f - (0.05f * j) - 0.05f).ToString();
                    string AnchorMax = ((0.2f * i) + 0.2f).ToString() + " " + (1f - (0.05f * j)).ToString();
                    string playerName = players.ToArray()[playerCount].displayName;
                    string id = players.ToArray()[playerCount].UserIDString;
                    container.Add(Panel($"pn{id}", "0 0 0 0", AnchorMin, AnchorMax, "playerslist"));
                    container.Add(Button($"plbtn{id}", $"pn{id}", $"checkplayers.gui alert {id}", "0 0 0 0.85", "0.05 0.05", "0.95 0.95"));
                    container.Add(Text($"plbtn{id}", "1 1 1 1", UserName(playerName), TextAnchor.MiddleCenter, 18));
                    playerCount++;
                }
            }
            if (page < maxPages)
            {
                container.Add(Button("npg", "PListGUI", $"checkplayers.gui page {(page + 1).ToString()}", "0 0 0 0.75", "1.025 0.575", "1.1 0.675"));
                container.Add(Text("npg", "1 1 1 1", ">>", TextAnchor.MiddleCenter, 16));
            }
            if (page > 1)
            {
                container.Add(Button("ppg", "PListGUI", $"checkplayers.gui page {(page - 1).ToString()}", "0 0 0 0.75", "1.025 0.45", "1.1 0.55"));
                container.Add(Text("ppg", "1 1 1 1", ">>", TextAnchor.MiddleCenter, 16));
            }
            CuiHelper.AddUi(player, container);
        }
        int CalculatePages(int value) => (int)Math.Ceiling(value / 100d);
        class GUIManager
        {
            public static Dictionary<BasePlayer, GUIManager> Players = new Dictionary<BasePlayer, GUIManager>();
            public int Page = 1;
            public static GUIManager Get(BasePlayer player)
            {
                if (Players.ContainsKey(player)) return Players[player];
                Players.Add(player, new GUIManager());
                return Players[player];
            }
        }
        #endregion

        #region Main        
        private void StartCheck(BasePlayer moder, BasePlayer target)
        {
            if (PlayersCheckList.ContainsValue(target)) { PrintToChat(moder, string.Format(GetMsg("ИгрокУжеНаПроверке", moder))); return; }
            if (PlayersCheckList.ContainsKey(target)) { PrintToChat(moder, string.Format(GetMsg("ИгрокВыполняетПроверку", moder))); return; }
            if (CheckCMDPlayer == target) { PrintToChat(moder, string.Format(GetMsg("ИгрокУжеНаПроверке", moder))); return; }
            var userVK = (string)VKBot?.Call("GetUserVKId", moder.userID);
            if (userVK == null) { PrintToChat(moder, string.Format(GetMsg("НетВК", moder))); return; }
            PlayersCheckList.Add(moder, target);
            StartGUI(target);
            PrintToChat(moder, string.Format(GetMsg("ИгрокВызванНаПроверку", moder), target.displayName));
        }
        #endregion

        #region Helpers
        private string UserName(string name)
        {
            if (name.Length > 15)
            {
                name = name.Remove(12) + "...";
            }
            return name;
        }
        private static List<BasePlayer> FindPlayersOnline(string nameOrIdOrIp)
        {
            var players = new List<BasePlayer>();
            if (string.IsNullOrEmpty(nameOrIdOrIp)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            return players;
        }
        #endregion

        #region Langs
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"ИгрокОтключился", "<size=17>Игрок вызванный на проверку покинул сервер. Причина: <color=#049906>{0}</color>.</size>"},
                {"МодераторОтключился", "<size=17>Модератор отключился от сервера, ожидайте следующей проверки.</size>"},
                {"НетПрав", "<size=17>У вас нет прав для использования данной команды</size>"},
                {"НетВК", "<size=17>Сначала добавьте и подтвердите свой профиль ВК командой /vk</size>" },
                {"ИгрокНеНайден", "<size=17>Игрок не найден</size>"},
                {"ВыНаПроверке", "<size=17>Вас вызвали на проверку, вы не можете использовать эту команду сейчас.</size>"},
                {"ПроверкаЗакончена", "<size=17>Вы закончили проверку игрока <color=#049906>{0}</color></size>"},
                {"ИгрокУжеНаПроверке", "<size=17>Этого игрока уже вызвали на проверку</size>"},
                {"НетПроверок", "<size=17>У вас нет активных проверок.</size>"},
                {"ИгрокВыполняетПроверку", "<size=17>Этот игрок выполняет проверку, вы не можете сейчас его вызвать.</size>"},
                {"ПроверкаНеЗакончена", "<size=17>Сначала закончите предыдущую проверку командой /{0}</size>"},
                {"ИгрокВызванНаПроверку", "<size=17>Вы вызвали игрока <color=#049906>{0}</color> на проверку</size>"},
                {"НеНаПроверке", "<size=17>Вас не вызывали на проверку. Ваш скайп нам не нужен <color=#049906>:)</color></size>"},
                {"СкайпОтправлен", "<size=17>Ваш скайп был отправлен модератору. Ожидайте звонка.</size>"},
                {"КомандаСкайп", "<size=17>Команда <color=#049906>/{0} НИК в СКАЙПЕ</color></size>"},
                {"СообщениеМодератору", "[CheckPlayers] Игрок {0} (steamcommunity.com/profiles/{1}/) предоставил скайп для проверки: {2}"}
            }, this);
        }
        string GetMsg(string key, BasePlayer player = null) => GetMsg(key, player.UserIDString);
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        #endregion
    }
}